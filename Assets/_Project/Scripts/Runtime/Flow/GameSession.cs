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

        LevelModel _level;
        BoardEvents _events;
        DragController _drag;
        Camera _camera;

        // Cila servisleri: bir kez kurulur, her bölümde yeni olay merkezine bağlanır.
        FX.FXService _fx;
        Services.AudioService _audio;
        Services.HapticsService _haptics;

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
            _haptics = Services.HapticsService.Create(transform);

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

        TextAsset NormalLevelAsset =>
            levelSequence != null && levelSequence.Length > 0
                ? levelSequence[Mathf.Clamp(_levelIndex, 0, levelSequence.Length - 1)]
                : levelJson;

        public bool HasNextLevel =>
            levelSequence != null && _levelIndex + 1 < levelSequence.Length;

        /// <summary>Dizideki bölüm sayısı (test seçicisi için).</summary>
        public int LevelCount => levelSequence != null ? levelSequence.Length : 0;

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

        void BuildAndStart()
        {
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
            _haptics?.Bind(_events);

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

            _fitWidth = boardWidth;
            _fitHeight = boardHeight;
            _lastAspect = cam.aspect;

            var rotation = Quaternion.Euler(68f, 0f, 0f);
            Vector3 forward = rotation * Vector3.forward;
            cam.fieldOfView = 33f;

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

        void OnBoardCleared()
        {
            State = GameState.Won;
            Timer.Stop();
        }

        void OnTimeExpired()
        {
            if (State != GameState.Playing) return;
            State = GameState.Lost;
            _audio?.PlayLose();
            _haptics?.Pulse();
        }
    }
}
