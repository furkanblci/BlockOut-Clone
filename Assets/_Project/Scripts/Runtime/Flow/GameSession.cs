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

        /// <summary>Bölüm içi yardımcılar (çalar saat / roket / UFO).</summary>
        public PowerUpSystem PowerUps { get; private set; }

        /// <summary>Arka arkaya emilim zinciri.</summary>
        public ComboTracker Combo { get; } = new ComboTracker();
        public int DisplayNumber { get; private set; }
        public int WarningSeconds => config.warningSeconds;

        /// <summary>Bu bölümün kayıt anahtarı (level_003 gibi). Meta servisleri kullanır.</summary>
        public string LevelId { get; private set; } = "";

        /// <summary>Son kazanışta verilen coin — bitiş ekranı gösterir.</summary>
        public int LastReward { get; private set; }

        /// <summary>
        /// Bu bölüm kazanılsaydı verilecek coin. Kaybetme paneli bunu
        /// "kaçırdığın ödül" olarak üstünde kırmızı çarpıyla gösteriyor.
        /// </summary>
        public int PendingReward =>
            Services.MetaServices.Ready ? Services.MetaServices.Progress.PreviewReward(LevelId) : 0;

        /// <summary>Son kazanış PERFECT miydi? Bitiş ekranı rozeti buna bakar.</summary>
        public bool LastPerfect { get; private set; }

        /// <summary>
        /// Son kazanışın yıldızı (1-3). Kalan süreye göre.
        ///
        /// DERS (yıldız neden 3'lü): Tek bir "bitti" bilgisi oyuncuya SONRAKİ
        /// hedefi vermez. Üç kademe, aynı bölümü tekrar oynamak için ücretsiz
        /// bir sebep üretir — hem de yeni içerik yazmadan.
        /// </summary>
        public int LastStars { get; private set; }

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
        /// Ayar ekranlarının servislere ulaşabilmesi için.
        /// (Duraklat panelindeki Sounds/Musics/Haptics anahtarları bunları
        /// <see cref="Services.SettingsBinder"/> üzerinden kullanıyor.)
        /// </summary>
        public Services.AudioService Audio => _audio;
        public GameKit.Services.Haptics Haptics => _haptics;

        /// <summary>
        /// Kamerayı tembel çözer. Restart/NextLevel dışarıdan (HUD, editör
        /// aracı) Start'tan önce çağrılabildiği için doğrudan alana güvenmiyoruz.
        /// </summary>
        Camera Cam => _camera != null ? _camera : (_camera = Camera.main);

        bool _servicesReady;

        void Start() => EnsureServices();

        /// <summary>
        /// Bir kereye mahsus kurulum. Tek sahnelik yapıda oynanış kökü açılıp
        /// kapandığı için Start güvenilmez bir kancadır — kurulum buraya alındı
        /// ve her giriş noktası (PlayLevel, Restart) önce bunu çağırır.
        /// </summary>
        void EnsureServices()
        {
            if (_servicesReady) return;
            _servicesReady = true;

            _camera = Camera.main;
            GameKit.FX.CameraShake.Ensure(_camera);
            gameObject.AddComponent<UI.GameplayScreen>().Init(this);

            // Menü AppRoot'ta da kuruluyor; burası oynanış kökünün AppRoot
            // olmadan (ör. sahneyi doğrudan açarak) test edildiği durumu
            // kapsıyor. Ensure zaten varsa hiçbir şey yapmıyor.
            DevTools.DevMenu.Ensure();

            // Cila servisleri KALICI kökte yaşar: menüye dönünce ses kesilmesin,
            // ayarlar ekranı ses servisini bulabilsin.
            var host = AppRoot.Current != null ? AppRoot.Current.PersistentRoot : transform;
            _fx = FX.FXService.Create(host, palette);

            // Ses ve titreşim artık AppRoot'ta, uygulama açılışında kuruluyor
            // (menü düğmeleri de ses çıkarsın diye). Burada YALNIZCA devralınıyor;
            // ikinci bir örnek kurmak, ayar ekranının bir örneği kapatıp
            // oyunun bir başkasını çalması demek olurdu. AppRoot yoksa
            // (oynanış sahnesi doğrudan açılmışsa) kendi örneğimizi kurarız.
            if (AppRoot.Current != null)
            {
                _audio = AppRoot.Current.Audio;
                _haptics = AppRoot.Current.Haptics;
            }
            else
            {
                _audio = Services.AudioService.Create(host);
                _haptics = GameKit.Services.Haptics.Create(host);
            }

            // Oyuncunun kayıtlı ses/titreşim tercihleri hemen geçerli olsun.
            if (Services.MetaServices.Ready)
                Services.SettingsBinder.Apply(
                    Services.MetaServices.Save.Data.Settings, _audio, _haptics);

            Timer.Expired += OnTimeExpired;
        }

        /// <summary>Menüden gelen "Oyna": istenen bölümü kurar ve başlatır.</summary>
        public void PlayLevel(int levelIndex)
        {
            EnsureServices();

            if (LevelCount > 0)
                _levelIndex = Mathf.Clamp(levelIndex, 0, LevelCount - 1);

            Restart();
        }

        /// <summary>
        /// Menüye dönerken tahtayı söker. Sahne yüklemediğimiz için temizliği
        /// artık biz yapmak zorundayız — bırakılan bir tahta, bir sonraki
        /// bölümde hayalet bloklar demek.
        /// </summary>
        public void StopLevel()
        {
            // Hedef bekleyen yardımcı ASILI KALMASIN. Menüye dönerken
            // `Pending` "Rocket" olarak duruyordu; tahta sökülüp gittiği için
            // görünür bir zararı yoktu ama oyunun durumu yalan söylüyordu ve
            // bu tür kalıntılar er geç bir yerde patlar.
            PowerUps?.Cancel();

            _drag?.Dispose();
            _drag = null;
            _events = null;
            Timer.Stop();
            State = GameState.Intro;

            if (boardRoot != null)
                for (int i = boardRoot.childCount - 1; i >= 0; i--)
                    Destroy(boardRoot.GetChild(i).gameObject);
        }

        void Update()
        {
            if (State == GameState.Playing)
            {
                Combo.Tick(Time.time);
                PowerUps?.Tick(Time.deltaTime);
                // Çalar saat açıkken sayaç durur — süre baskısı geçici olarak kalkar.
                if (PowerUps == null || !PowerUps.IsTimeFrozen)
                    Timer.Tick(Time.deltaTime);
            }

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

        /// <summary>
        /// Bölümün zorluğu. HUD'daki uyarı satırı ve ödül çarpanı bunu kullanır.
        ///
        /// DERS (aynı bilgiyi İKİ yerden hesaplama): Bu önce GameSession'da
        /// ayrı bir bool olarak eklendi ve JSON'daki "difficulty" dizesini
        /// okuyordu. Oysa projede zaten LevelDifficultyRule vardı ve ana ekran
        /// onu kullanıyordu — üstelik o kural zorluğu tahtanın İÇERİĞİNDEN
        /// hesaplıyor, JSON etiketinden değil. İki kaynak bir süre aynı cevabı
        /// verir, sonra sessizce ayrışır. Tek kaynak: kural.
        /// </summary>
        public Core.LevelDifficulty Difficulty { get; private set; }

        /// <summary>Sıradaki bölüm sayısı (Home ekranı ve test seçicisi için).</summary>
        public int LevelCount =>
            Config.LevelCatalog.Count > 0
                ? Config.LevelCatalog.Count
                : (levelSequence != null ? levelSequence.Length : 0);

        public int LevelIndex => _levelIndex;

        public void NextLevel()
        {
            // Perde arkasında geçiyoruz: oyuncu iki tahtayı birlikte görmesin.
            // Perde yoksa (sahne doğrudan açıldıysa) doğrudan devam eder.
            var intro = UI.LevelIntro.Ensure(
                AppRoot.Current != null ? AppRoot.Current.PersistentRoot : transform);

            if (intro == null) { AdvanceNow(); return; }
            intro.Play(AdvanceNow);
        }

        void AdvanceNow()
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

        /// <summary>
        /// Bölüm yüklenemedi — sebebini EKRANDA söyle.
        ///
        /// DERS (cihazda `Debug.LogError` hiçbir yere gitmez): 2026-08-17
        /// APK testinde 1. bölüm açılmadı, tahta boş kaldı ve oyuncu bunun
        /// SEBEBİNİ hiçbir yerde göremedi — konsol yok, logcat için kablo
        /// gerekiyor. Sessiz boş ekran, teşhisi saatlerce geciktirdi.
        /// Yükleme hatası oyuncunun/testçinin GÖRECEĞİ bir yere yazılmalı;
        /// hata metnini gizlemek hatayı yok etmiyor, yalnız bulunmasını
        /// zorlaştırıyor.
        /// </summary>
        void ReportLoadFailure(string title, string detail)
        {
            Debug.LogError($"[GameSession] {title}: {detail}", this);
            State = GameState.Intro;
            LoadFailure = $"{title}\n{detail}";
        }

        /// <summary>
        /// Son bölüm yükleme hatası; başarılı kurulumda temizlenir.
        /// HUD bunu okuyup ekrana basıyor (bkz. <see cref="UI.GameplayScreen"/>).
        /// </summary>
        public string LoadFailure { get; private set; }

        void BuildAndStart()
        {
            LoadFailure = null;

            if (visuals != null) View.VisualSettings.Apply(visuals);

            LevelData data;
            try
            {
                var asset = ActiveLevelAsset;
                if (asset == null)
                    throw new System.Exception(
                        $"Bölüm dosyası bulunamadı (sıra {_levelIndex + 1}, katalogda " +
                        $"{Config.LevelCatalog.Count} bölüm var).");

                data = Level.LevelLoader.Parse(asset.text);
            }
            catch (System.Exception e)
            {
                ReportLoadFailure("Level yüklenemedi", e.Message);
                return;
            }

            // BURADAN AŞAĞISININ TAMAMI KORUMALI.
            //
            // DERS (yalnız ŞÜPHELENDİĞİN satırı korumak, körlüğü taşımaktır):
            // Eskiden yalnız `Parse` try/catch içindeydi. Doğrulama, model
            // kurulumu, tahta inşası, sistemler ve sürükleme denetleyicisi —
            // hepsi korumasızdı. Oradan fırlayan bir hata `BuildAndStart`'ı
            // yarıda kesiyor, `LoadFailure` HİÇ yazılmıyor ve oyuncu boş bir
            // tahtaya, hiçbir açıklama olmadan bakıyor. 2026-08-17 APK
            // testindeki "bölümler gelmiyor" belirtisi tam olarak bu:
            // ekranda sebebi söyleyecek mekanizma vardı ama hatanın geçtiği
            // yol onun DIŞINDAYDI.
            //
            // Cihazda konsol yok; bir hatanın nereye düşeceğini tahmin etmek
            // yerine bütün kurulumu tek bir ağa almak doğrusu.
            try
            {
                var errors = new List<string>();
                if (!Level.LevelLoader.Validate(data, errors))
                {
                    foreach (var err in errors)
                        Debug.LogError($"[GameSession] Level doğrulama hatası: {err}", this);
                    ReportLoadFailure("Level doğrulanamadı",
                        errors.Count > 0 ? errors[0] : "bilinmeyen hata");
                    return;
                }

                BuildBoard(data);
            }
            catch (System.Exception e)
            {
                // Tür adı da yazılıyor: cihazda "ExecutionEngineException"
                // görmek doğrudan IL2CPP/AOT'u işaret eder, "NullReference"
                // ise bağlanmamış bir alanı.
                ReportLoadFailure("Level kurulamadı", $"{e.GetType().Name}: {e.Message}");
            }
        }

        /// <summary>
        /// Doğrulanmış veriden tahtayı ve sistemleri kurar.
        /// <see cref="BuildAndStart"/> bunu try/catch içinde çağırır.
        /// </summary>
        void BuildBoard(LevelData data)
        {
            DisplayNumber = data.DisplayNumber;
            LevelId = string.IsNullOrEmpty(data.Id) ? ActiveLevelAsset.name : data.Id;

            _level = LevelModel.Build(data);

            // Zorluk tahtanın İÇERİĞİNDEN hesaplanıyor (LevelDifficultyRule);
            // JSON'daki "difficulty" alanı yalnız yazarın notu. Bu satır
            // _level kurulduktan SONRA gelmek zorunda.
            Difficulty = Core.LevelDifficultyRule.Of(_level);
            _events = new BoardEvents();
            _events.BoardCleared += OnBoardCleared;

            var space = new BoardSpace(data.Board.Width, data.Board.Height);
            FitCamera(data.Board.Width, data.Board.Height);
            var views = BoardBuilder.Build(boardRoot, _level, space, palette);
            var obstacles = new ObstacleSystem(_level, views, palette, _events, space);
            var gates = new GateSystem(_level, views, config, _events, obstacles, palette);
            gates.RecomputeGateStates(); // baştan rengi olmayan kapı hemen ghost görünsün
            obstacles.Start();           // üreteçler ilk bloklarını tahtaya itsin
            _drag = new DragController(
                input, Cam, _level, views, space, config, gates,
                () => State == GameState.Playing);

            PowerUps = new PowerUpSystem(
                _level, views, gates, obstacles, Services.MetaServices.Progress, space);
            _drag.BlockTapped = PowerUps.HandleBlockTap;

            // Combo: her emilimde zincir uzar. Yeni bölümde sıfırdan başlar.
            Combo.Reset();
            _events.BlockAbsorbed += (b, g) => Combo.NoteAbsorb(Time.time);

            // Cila servisleri taze olay merkezine bağlanır.
            _fx?.Bind(_events, space);
            _audio?.Bind(_events);
            BindHaptics(_events);

            PlayBoardIntro(views);

            // CAN EN SON HARCANIR.
            //
            // Niyet zaten buydu ("bölüm kurulurken değil oynanmaya başlarken")
            // ama çağrı kurulumun BAŞINDAYDI; kurulum ortada patlarsa oyuncu
            // hiç oynamadığı bir bölüm için can kaybediyordu. Artık tahta
            // gerçekten ayakta olduğunda harcanıyor.
            SpendLifeForAttempt();

            Timer.StartCountdown(data.TimeSeconds);
            State = GameState.Playing;

            // Öğretici yalnız ilk bölümde ve yalnız bir kez çıkar; kendisi
            // karar veriyor, buradan koşul yazmaya gerek yok.
            UI.TutorialOverlay.TryShow(this, _level, space);

            // Yeni mekanik tanıtımı da kendi kararını kendi veriyor: bu bölümde
            // ilk kez görülen bir şey varsa spot ışığıyla tanıtır, yoksa hiç
            // görünmez.
            UI.NewItemPanel.TryShow(this, _level);
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
            EnsureServices();
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

            // SINIRSIZ CAN: hak sürerken can HARCANMAZ.
            //
            // DERS (yazılan ama HİÇ OKUNMAYAN alan): Mağazadaki sınırsız can
            // paketi `InfiniteLivesUntilUtc`'yi kayda düzgünce yazıyordu ama
            // projede o alanı okuyan TEK BİR YER YOKTU — ne burası, ne ana
            // ekranın can sayacı. Yani paket alınıyor, para gidiyor, hiçbir
            // şey değişmiyordu (2026-08-17 cihaz testinde raporlandı).
            // Bir alanı yazmak onu bir ÖZELLİK yapmaz; okuyan taraf yoksa
            // özellik de yoktur.
            if (!Services.MetaServices.Progress.HasInfiniteLives)
                Services.MetaServices.Lives.TrySpend();

            Services.MetaServices.Progress.NoteAttempt(LevelId);

            GameKit.Services.Analytics.LevelStarted(
                _levelIndex, Services.MetaServices.Progress.Record(LevelId).Attempts);
        }

        /// <summary>
        /// Bölümü çözmüş gibi bitirir — YALNIZCA gizli geliştirici menüsü için.
        ///
        /// DERS (test kapısı gerçek yolu KULLANMALI): Sonuç panelini görmek için
        /// paneli elle açan bir kestirme yazmak cazip; ama o zaman test ettiğin
        /// şey gerçek akış olmaz — ödül hesabı, PERFECT ölçüsü, can iadesi
        /// çalışmadan panel açılır ve "doğru görünen ama yanlış veriyle dolu"
        /// bir ekran doğrularsın. Bu metot normal bitiş yolunun ta kendisini
        /// çağırıyor; tek farkı tetiğin nereden çekildiği.
        /// </summary>
        public void DebugForceWin()
        {
            if (State != GameState.Playing && State != GameState.Intro) return;
            OnBoardCleared();
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

                LastPerfect = perfect;
                LastStars = perfect ? 3 : remaining * 4 >= Timer.Total ? 2 : 1;

                LastReward = Services.MetaServices.Progress.NoteCleared(
                    LevelId, _levelIndex, remaining, perfect);

                // Zincir ödülü: bölüm boyunca yakalanan EN UZUN zincir ödülü
                // çarpar. Anlık zincire bakmak, son hamlenin şansına bağlı bir
                // ödül olurdu; en uzun zincir oyuncunun gerçekten yaptığı işi
                // ölçüyor.
                float multiplier = ComboTracker.Multiplier(Combo.BestChain);
                if (multiplier > 1f)
                {
                    int bonus = Mathf.RoundToInt(LastReward * (multiplier - 1f));
                    if (bonus > 0)
                    {
                        LastReward += bonus;
                        Services.MetaServices.Progress.GrantCoins(bonus);
                        GameKit.Services.Analytics.CurrencyEarned("coin", bonus, "combo");
                    }
                }

                // Kazanan oyuncu canını geri alır — videoda can yalnız kaybedince
                // eksiliyor. Harcamayı girişte yapıp kazanınca iade etmek, "çıkıp
                // geri girme" istismarını da kapatıyor.
                Services.MetaServices.Lives.Grant(1);

                var record = Services.MetaServices.Progress.Record(LevelId);
                GameKit.Services.Analytics.LevelCompleted(
                    _levelIndex, record.Attempts, remaining, perfect);
                GameKit.Services.Analytics.CurrencyEarned("coin", LastReward, "level_clear");
            }
            else
            {
                // DERS (sessiz kayıp en kötü hata türüdür): Meta katmanı hazır
                // değilken bölüm bitirilirse buradaki hiçbir şey çalışmaz —
                // oyuncu jetonunu, canını, bölüm açılışını ve kaydını kaybeder.
                // Eskiden bu durum HİÇBİR iz bırakmıyordu; sonuç paneli sadece
                // ödülsüz açılıyor ve "tasarım böyle" gibi görünüyordu. Bir
                // gün gerçek cihazda olursa sebebi aranabilsin diye bağırıyor.
                LastReward = 0;
                LastStars = 0;
                LastPerfect = false;
                Debug.LogError("[GameSession] Bölüm bitti ama MetaServices hazır değil — " +
                               "ödül, can iadesi ve bölüm açılışı YAZILMADI.");
            }
        }

        /// <summary>
        /// Duraklat / devam et.
        ///
        /// DERS (duraklatma zaman ölçeğiyle YAPILMAZ): `Time.timeScale = 0`
        /// en kolay yol gibi görünür ama arayüz animasyonlarını da durdurur —
        /// duraklat paneli donmuş bir resim gibi açılır. Burada oyun durumu
        /// değişiyor: Update zaten yalnız Playing'ken sayaç işletiyor ve
        /// sürükleme yalnız Playing'ken kabul ediliyor. Arayüz ölçeklenmemiş
        /// zamanla çalıştığı için akmaya devam ediyor.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (paused && State == GameState.Playing) State = GameState.Paused;
            else if (!paused && State == GameState.Paused) State = GameState.Playing;
        }

        /// <summary>
        /// Reklam ödülüyle bölüme devam: süre eklenir, oyun kaldığı yerden akar.
        /// Tahta hiç bozulmadığı için oyuncu tam bıraktığı yerden devam eder.
        /// </summary>
        public void ContinueWithExtraTime(int seconds)
        {
            if (State != GameState.Lost) return;
            State = GameState.Playing;
            Timer.AddTime(seconds);
        }

        /// <summary>Kazanılan ödülü katlar — "reklam izle, ödülü ikiye katla".</summary>
        public void MultiplyReward(int multiplier)
        {
            if (State != GameState.Won || multiplier <= 1) return;

            int bonus = LastReward * (multiplier - 1);
            LastReward *= multiplier;

            if (Services.MetaServices.Ready)
            {
                Services.MetaServices.Progress.GrantCoins(bonus);
                GameKit.Services.Analytics.CurrencyEarned("coin", bonus, "rewarded_ad");
            }
        }

        /// <summary>
        /// Oyuncu BİLİNÇLİ olarak pes etti (yeniden başlat onayı verildi).
        ///
        /// DERS (yeniden başlatmak ücretsiz DEĞİLDİR): Üst bardaki yeniden
        /// başlat düğmesi doğrudan <see cref="Restart"/> çağırıyordu ve bölüm
        /// anında baştan kuruluyordu. Oyuncunun bakış açısından bu "bedava
        /// geri al" demek; oysa `Restart` zaten `_lifeSpent`'ı sıfırlayıp yeni
        /// bir can harcıyor. Yani bedel ÖDENİYOR ama oyuncuya hiç sorulmuyor
        /// ve hiç gösterilmiyordu — parmağın kayması bir can yakıyordu.
        ///
        /// Referans oyun bunu kaybetme akışına bağlıyor: pes etmek "kaybettim"
        /// ile aynı şeydir, aynı paneli açar ve canı orada kaybedersin.
        /// Bu yüzden burada yeni bir durum icat etmiyoruz, mevcut Lost
        /// durumuna giriyoruz — panel, ses, analitik, hepsi zaten bağlı.
        /// </summary>
        public void GiveUp()
        {
            if (State != GameState.Playing && State != GameState.Paused) return;

            SetPaused(false);
            State = GameState.Lost;
            _audio?.PlayLose();
            _haptics?.Play(GameKit.Services.HapticStrength.Heavy);

            if (Services.MetaServices.Ready)
                GameKit.Services.Analytics.LevelFailed(
                    _levelIndex,
                    Services.MetaServices.Progress.Record(LevelId).Attempts,
                    "restart");
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
