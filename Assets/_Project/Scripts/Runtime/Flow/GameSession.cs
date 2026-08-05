using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Input;
using UnityEngine;

namespace BlockOut.Runtime.Flow
{
    /// <summary>Bölüm yaşam döngüsü durumları. Intro (tahta giriş animasyonu, M4) ve Paused (M5) henüz kullanılmıyor.</summary>
    public enum GameState { Intro, Playing, Paused, Won, Lost }

    /// <summary>
    /// Bölümün KOMPOZİSYON KÖKÜ (composition root): tüm parçaları kurar,
    /// birbirine bağlar ve yaşam döngüsünü yönetir. Sahnedeki tek "oyun beyni"
    /// MonoBehaviour'ı budur — diğer sistemler saf C# olarak burada yaratılır.
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] GameConfigSO config;
        [SerializeField] ColorPaletteSO palette;
        [SerializeField, Tooltip("Görsel ayarlar — Tools > Block Out > Görünüm Ayarları'ndan canlı düzenlenir.")]
        BlockVisualConfigSO visuals;
        [SerializeField] TextAsset levelJson;
        [SerializeField, Tooltip("Doluysa levelJson yerine bu sıra oynanır; kazanınca sonrakine geçilir. M5'te LevelDatabaseSO'ya evrilecek.")]
        TextAsset[] levelSequence;
        [SerializeField] PointerInputService input;
        [SerializeField] Transform boardRoot;

        int _levelIndex;

        public GameState State { get; private set; } = GameState.Intro;
        public LevelTimer Timer { get; } = new LevelTimer();
        public int DisplayNumber { get; private set; }
        public int WarningSeconds => config.warningSeconds;

        /// <summary>Bu bölümün kayıt anahtarı (level_003 gibi). Meta servisleri kullanır.</summary>
        public string LevelId { get; private set; } = "";

        /// <summary>Son kazanışta verilen coin — bitiş ekranı gösterir.</summary>
        public int LastReward { get; private set; }

        /// <summary>Bu deneme için can harcandı mı? (Aynı bölümde iki kez düşmesin.)</summary>
        bool _lifeSpent;

        LevelModel _level;
        BoardEvents _events;
        DragController _drag;
        Camera _camera;

        // Cila servisleri: bir kez kurulur, her bölümde yeni olay merkezine bağlanır.
        FX.FXService _fx;
        Services.AudioService _audio;
        GameKit.Services.Haptics _haptics;

        /// <summary>
        /// Kamerayı tembel çözer. Restart/NextLevel dışarıdan (HUD, editör
        /// aracı) Start'tan önce çağrılabildiği için doğrudan alana güvenmiyoruz.
        /// </summary>
        Camera Cam => _camera != null ? _camera : (_camera = Camera.main);

        void Start()
        {
            _camera = Camera.main;
            gameObject.AddComponent<GameplayHud>().Init(this);

            _fx = FX.FXService.Create(transform, palette);
            _audio = Services.AudioService.Create(transform);
            _haptics = GameKit.Services.Haptics.Create(transform);

            // Oyuncunun kayıtlı ses/titreşim tercihleri hemen geçerli olsun.
            if (Services.MetaServices.Ready)
                Services.SettingsBinder.Apply(
                    Services.MetaServices.Save.Data.Settings, _audio, _haptics);

            // Bölüm seçimi üç kaynaktan gelebilir, öncelik sırasıyla:
            // 1) Home ekranının isteği, 2) oyuncunun kaldığı yer, 3) bölüm 1.
            // (Gameplay sahnesi tek başına Play'e basılarak da açılabilmeli.)
            int requested = AppRouter.ConsumeRequestedLevel();
            if (LevelCount > 0)
            {
                int index = requested >= 0
                    ? requested
                    : Services.MetaServices.Ready
                        ? Services.MetaServices.Progress.HighestUnlockedIndex
                        : 0;
                _levelIndex = Mathf.Clamp(index, 0, LevelCount - 1);
            }

            Timer.Expired += OnTimeExpired;
            BuildAndStart();
        }

        void Update()
        {
            if (State == GameState.Playing)
                Timer.Tick(Time.deltaTime);

            // Game view boyutu / cihaz yönü değişirse kadrajı tazele.
            if (_fitWidth > 0 && !Mathf.Approximately(_camera.aspect, _lastAspect))
                FitCamera(_fitWidth, _fitHeight);
        }

        void OnDestroy()
        {
            _drag?.Dispose();
            Timer.Expired -= OnTimeExpired;
        }

        TextAsset ActiveLevelAsset
        {
#if UNITY_EDITOR
            get
            {
                // Level editöründeki "Play Test" düğmesi SessionState'e bir yol
                // bırakır; varsa normal sıranın yerine o bölüm oynanır.
                // SessionState domain reload'ı aşar, editör kapanınca silinir.
                string playtest = UnityEditor.SessionState.GetString(
                    "BlockOut.PlaytestLevel", string.Empty);
                if (!string.IsNullOrEmpty(playtest))
                {
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(playtest);
                    if (asset != null) return asset;
                }
                return NormalLevelAsset;
            }
#else
            get => NormalLevelAsset;
#endif
        }

        TextAsset NormalLevelAsset
        {
            get
            {
                // Katalog M5'te tek doğruluk kaynağı oldu (Home ekranı da onu
                // okuyor). Sahnedeki dizi yalnız katalog yoksa devreye girer.
                var fromCatalog = Config.LevelCatalog.AssetAt(_levelIndex);
                if (fromCatalog != null) return fromCatalog;

                return levelSequence != null && levelSequence.Length > 0
                    ? levelSequence[Mathf.Clamp(_levelIndex, 0, levelSequence.Length - 1)]
                    : levelJson;
            }
        }

        public bool HasNextLevel => _levelIndex + 1 < LevelCount;

        /// <summary>Sıradaki bölüm sayısı (Home ekranı ve test seçicisi için).</summary>
        public int LevelCount =>
            Config.LevelCatalog.Count > 0
                ? Config.LevelCatalog.Count
                : (levelSequence != null ? levelSequence.Length : 0);

        public int LevelIndex => _levelIndex;

        public void NextLevel()
        {
            if (HasNextLevel) _levelIndex++;
            Restart();
        }

        /// <summary>
        /// Doğrudan bir bölüme atlar. Cihazda test ederken bölüm seçebilmek için;
        /// gerçek bölüm haritası (Journey) M5'te bunun yerini alacak.
        /// </summary>
        public void GoToLevel(int index)
        {
            if (LevelCount == 0) return;
            _levelIndex = Mathf.Clamp(index, 0, LevelCount - 1);
            Restart();
        }

        /// <summary>
        /// Görsel ayarlar değiştiğinde tahtayı yeniden kurar (ayar penceresi çağırır).
        /// Bölüm baştan başlar — ayar denerken istenen davranış budur.
        /// </summary>
        public void RefreshVisuals()
        {
            if (visuals != null) View.VisualSettings.Apply(visuals);
            if (State != GameState.Intro) Restart();
        }

        void BuildAndStart()
        {
            if (visuals != null) View.VisualSettings.Apply(visuals);

            LevelData data;
            try
            {
                data = Level.LevelLoader.Parse(ActiveLevelAsset.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameSession] Level parse edilemedi: {e.Message}", this);
                return;
            }

            var errors = new List<string>();
            if (!Level.LevelLoader.Validate(data, errors))
            {
                foreach (var err in errors)
                    Debug.LogError($"[GameSession] Level doğrulama hatası: {err}", this);
                return;
            }

            DisplayNumber = data.DisplayNumber;
            LevelId = string.IsNullOrEmpty(data.Id) ? ActiveLevelAsset.name : data.Id;

            // Can, bölüm KURULURKEN değil OYNANMAYA BAŞLARKEN harcanır; parse
            // hatasında oyuncudan can almış olmayalım.
            SpendLifeForAttempt();

            _level = LevelModel.Build(data);
            _events = new BoardEvents();
            _events.BoardCleared += OnBoardCleared;

            var space = new BoardSpace(data.Board.Width, data.Board.Height);
            FitCamera(data.Board.Width, data.Board.Height);
            var views = BoardBuilder.Build(boardRoot, _level, space, palette);
            var obstacles = new ObstacleSystem(_level, views, palette, _events, space);
            var gates = new GateSystem(_level, views, config, _events, obstacles, palette);
            gates.RecomputeGateStates(); // baştan rengi olmayan kapı hemen ghost görünsün
            _drag = new DragController(
                input, Cam, _level, views, space, config, gates,
                () => State == GameState.Playing);

            // Cila servisleri taze olay merkezine bağlanır.
            _fx?.Bind(_events, space);
            _audio?.Bind(_events);
            BindHaptics(_events);

            PlayBoardIntro(views);
            Timer.StartCountdown(data.TimeSeconds);
            State = GameState.Playing;
        }

        /// <summary>
        /// Kamerayı tahtaya EN-BOY ORANINA DUYARLI çerçeveler: tahta köşeleri
        /// (kapı barları için kenar payıyla) görüş alanına sığana dek kamera
        /// bakış ekseni boyunca geri çekilir (ikili arama — bölüm başına bir
        /// kez, maliyeti yok). Böylece aynı bölüm 16:9 yatayda da 9:16 dikey
        /// telefonda da tam kadrajlanır.
        /// </summary>
        void FitCamera(int boardWidth, int boardHeight)
        {
            var cam = Cam;
            if (cam == null) return;

            // Arka plan: referanstaki koyu mor/lacivert. Kamera temizleme rengi
            // + geniş gradyan quad; ayar penceresinden değiştirilebilir.
            var visualCfg = View.VisualSettings.Current;
            if (visualCfg != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = visualCfg.backgroundOuter;
                View.BackgroundView.Ensure(cam, visualCfg);
            }

            _fitWidth = boardWidth;
            _fitHeight = boardHeight;
            _lastAspect = cam.aspect;

            // Referans oyun tahtaya NEREDEYSE TEPEDEN bakar: blokların yalnızca
            // üst yüzü ve ince bir yan bandı görünür. 68° fazla yatıktı, yan
            // yüzler baskın çıkıyordu. Dar FOV perspektifi yassılaştırır —
            // uzaktaki hücreler yakındakilerle aynı boyutta görünür, bulmaca
            // okunaklı olur (bulmaca oyunlarının standart kamera dili).
            var rotation = Quaternion.Euler(80f, 0f, 0f);
            Vector3 forward = rotation * Vector3.forward;
            cam.fieldOfView = 27f;

            // Kapı barları ve duvarlar tahta sınırının dışına taşar → kenar payı.
            float hw = boardWidth * 0.5f + 0.9f;
            float hh = boardHeight * 0.5f + 0.9f;
            var corners = new[]
            {
                new Vector3(-hw, 0f, -hh), new Vector3(hw, 0f, -hh),
                new Vector3(-hw, 0f,  hh), new Vector3(hw, 0f,  hh),
                new Vector3(-hw, 0.7f, -hh), new Vector3(hw, 0.7f, -hh),
                new Vector3(-hw, 0.7f,  hh), new Vector3(hw, 0.7f,  hh)
            };

            float near = 6f, far = 80f;
            for (int i = 0; i < 18; i++)
            {
                float mid = (near + far) * 0.5f;
                cam.transform.SetPositionAndRotation(-forward * mid, rotation);
                if (AllCornersVisible(corners)) far = mid;
                else near = mid;
            }
            cam.transform.SetPositionAndRotation(-forward * far, rotation);
        }

        bool AllCornersVisible(Vector3[] points)
        {
            foreach (var p in points)
            {
                var v = Cam.WorldToViewportPoint(p);
                // Üstte HUD şeridi için pay bırak (y 0.90), yanlarda küçük marj.
                if (v.z < 0f || v.x < 0.04f || v.x > 0.96f || v.y < 0.05f || v.y > 0.90f)
                    return false;
            }
            return true;
        }

        int _fitWidth, _fitHeight;
        float _lastAspect;

        /// <summary>Aynı bölümü baştan kurar. Sahne yeniden yüklemeye gerek yok —
        /// tüm durum bu sınıfın altında olduğu için yıkıp yeniden kurmak yeterli.</summary>
        public void Restart()
        {
            _drag?.Dispose();
            _drag = null;
            _events = null; // taze olay merkezi = bayat abone kalmaz
            _lifeSpent = false;
            LastReward = 0;
            State = GameState.Intro;
            BuildAndStart();
        }

        /// <summary>
        /// Bloklar yukarıdan sırayla düşerek tahtayı kurar. Gecikme köşegen
        /// sırayla artar (sol-üstten sağ-alta doğru dalga) — hepsi aynı anda
        /// düşerse kaotik görünür, dalga halinde düşerse tahta "kuruluyor" hissi verir.
        /// </summary>
        void PlayBoardIntro(BoardViews views)
        {
            const float step = 0.045f;
            const float duration = 0.34f;

            foreach (var pair in views.Blocks)
            {
                var model = pair.Key;
                float delay = (model.Position.x + model.Position.y) * step;
                pair.Value.PlayIntro(delay, duration);
            }
        }

        /// <summary>
        /// Hangi tahta olayının titreşim ürettiğine BU OYUN karar verir.
        ///
        /// DERS (kit oyunu tanımaz): GameKit'teki Haptics yalnızca "titret"
        /// biliyor; "buz kırıldı" gibi kavramlar ona sızarsa başka bir projede
        /// kullanılamaz hâle gelir. Eşleme oyunun tarafında, tek bir yerde durur.
        ///
        /// Her blok çıkışında titretmek yorucu olur; yalnızca "kazanım"
        /// anlarında geri bildirim veriyoruz.
        /// </summary>
        void BindHaptics(BoardEvents events)
        {
            if (_haptics == null || events == null) return;
            events.IceShattered     += _ => _haptics.Play(GameKit.Services.HapticStrength.Medium);
            events.GateIceShattered += _ => _haptics.Play(GameKit.Services.HapticStrength.Medium);
            events.CurtainOpened    += _ => _haptics.Play(GameKit.Services.HapticStrength.Medium);
            events.BoardCleared     += () => _haptics.Play(GameKit.Services.HapticStrength.Heavy);
        }

        /// <summary>
        /// Denemeye bir can yazar. Can yoksa bölüm yine de açılır ama işaretlenir —
        /// "can bitti" kararını Home ekranı verecek (M5 sahne akışı); oyunun
        /// ortasında oyuncuyu kilitlemek kötü bir deneyim olurdu.
        /// </summary>
        void SpendLifeForAttempt()
        {
            if (_lifeSpent || !Services.MetaServices.Ready) return;
            _lifeSpent = true;
            Services.MetaServices.Lives.TrySpend();
            Services.MetaServices.Progress.NoteAttempt(LevelId);

            GameKit.Services.Analytics.LevelStarted(
                _levelIndex, Services.MetaServices.Progress.Record(LevelId).Attempts);
        }

        void OnBoardCleared()
        {
            State = GameState.Won;
            Timer.Stop();

            if (Services.MetaServices.Ready)
            {
                // PERFECT ölçüsü (videodan): süre dolmadan, sürenin yarısından
                // fazlası kalmışken bitirmek. Kesin kriter L20+ kaydı gelince
                // netleşecek; kural tek yerde durduğu için değiştirmesi kolay.
                int remaining = Mathf.CeilToInt(Timer.Remaining);
                bool perfect = remaining * 2 >= Timer.Total;

                LastReward = Services.MetaServices.Progress.NoteCleared(
                    LevelId, _levelIndex, remaining, perfect);

                // Kazanan oyuncu canını geri alır — videoda can yalnız kaybedince
                // eksiliyor. Harcamayı girişte yapıp kazanınca iade etmek, "çıkıp
                // geri girme" istismarını da kapatıyor.
                Services.MetaServices.Lives.Grant(1);

                var record = Services.MetaServices.Progress.Record(LevelId);
                GameKit.Services.Analytics.LevelCompleted(
                    _levelIndex, record.Attempts, remaining, perfect);
                GameKit.Services.Analytics.CurrencyEarned("coin", LastReward, "level_clear");
            }
        }

        void OnTimeExpired()
        {
            if (State != GameState.Playing) return;
            State = GameState.Lost;
            _audio?.PlayLose();
            _haptics?.Play(GameKit.Services.HapticStrength.Heavy);

            if (Services.MetaServices.Ready)
                GameKit.Services.Analytics.LevelFailed(
                    _levelIndex,
                    Services.MetaServices.Progress.Record(LevelId).Attempts,
                    "timeout");
        }
    }
}
