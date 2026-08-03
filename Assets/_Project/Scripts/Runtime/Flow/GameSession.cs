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

        void Start()
        {
            _camera = Camera.main;
            gameObject.AddComponent<GameplayHud>().Init(this);
            Timer.Expired += OnTimeExpired;
            BuildAndStart();
        }

        void Update()
        {
            if (State == GameState.Playing)
                Timer.Tick(Time.deltaTime);
        }

        void OnDestroy()
        {
            _drag?.Dispose();
            Timer.Expired -= OnTimeExpired;
        }

        TextAsset ActiveLevelAsset =>
            levelSequence != null && levelSequence.Length > 0
                ? levelSequence[Mathf.Clamp(_levelIndex, 0, levelSequence.Length - 1)]
                : levelJson;

        public bool HasNextLevel =>
            levelSequence != null && _levelIndex + 1 < levelSequence.Length;

        public void NextLevel()
        {
            if (HasNextLevel) _levelIndex++;
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
            FitCamera(data.Board.Height);
            var views = BoardBuilder.Build(boardRoot, _level, space, palette);
            var obstacles = new ObstacleSystem(_level, views, palette, _events, space);
            var gates = new GateSystem(_level, views, config, _events, obstacles, palette);
            gates.RecomputeGateStates(); // baştan rengi olmayan kapı hemen ghost görünsün
            _drag = new DragController(
                input, _camera, _level, views, space, config, gates,
                () => State == GameState.Playing);

            Timer.StartCountdown(data.TimeSeconds);
            State = GameState.Playing;
        }

        /// <summary>
        /// Kamerayı tahta yüksekliğine göre çerçeveler — her bölümün tahtası
        /// farklı boyutta olabileceği için kamera sahneye gömülü değer taşımaz.
        /// Oranlar 68° eğim + 33° FOV için elle ayarlandı; M4'te en-boy oranına
        /// duyarlı (telefon dikey ekranı) gerçek bir fit hesabına dönüşecek.
        /// </summary>
        void FitCamera(int boardHeight)
        {
            _camera.fieldOfView = 33f;
            _camera.transform.SetPositionAndRotation(
                new Vector3(0f, boardHeight * 2f, -boardHeight * 0.8f),
                Quaternion.Euler(68f, 0f, 0f));
        }

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

        void OnBoardCleared()
        {
            State = GameState.Won;
            Timer.Stop();
        }

        void OnTimeExpired()
        {
            if (State == GameState.Playing)
                State = GameState.Lost;
        }
    }
}
