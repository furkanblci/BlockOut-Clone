using System.Text;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.Services;
using UnityEngine;

namespace BlockOut.Runtime.DevTools
{
    /// <summary>
    /// Geliştirici menüsü: bölüm atlama, para/can verme, kaydı sıfırlama.
    ///
    /// DERS (test aracı görünmez olmalı): Ekranın köşesinde duran bir "bölüm
    /// seç" düğmesi, testçinin oyunu OYNAMASINI engeller — herkes o düğmeye
    /// basar ve gerçek ilerleyişi kimse denemez. Üstelik yayınlanan yapıya
    /// sızarsa oyunu bozar. Çözüm iki katmanlı:
    ///   1. Bu dosyanın tamamı YALNIZCA geliştirme yapılarında derlenir
    ///      (DEVELOPMENT_BUILD / editör). Yayın yapısında hiç yoktur.
    ///   2. Geliştirme yapısında bile görünür bir düğmesi yoktur; sol üst
    ///      köşeye ÜST ÜSTE BEŞ KEZ dokununca açılır. Kimse kazara bulmaz,
    ///      bilen bir saniyede açar.
    ///
    /// DERS (liste taşarsa araç işe yaramaz): Önceki bölüm seçici 50 bölümü
    /// tek bir ızgaraya yığıyordu; son satırlar ekranın altından taşıyor ve
    /// SEÇİLEMİYORDU — yani aracın var oluş sebebi olan "son bölümü test et"
    /// işi tam da yapılamıyordu. Buradaki liste kaydırılabilir.
    /// </summary>
    public sealed class DevMenu : MonoBehaviour
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        const int TapsToOpen = 5;
        const float TapWindow = 2.0f;
        const float CornerRatio = 0.18f;      // ekranın sol üst %18'i

        static DevMenu _instance;

        GameSession _session;
        bool _open;
        int _tapCount;
        float _firstTapTime;
        Vector2 _scroll;
        int _tab;
        string _status = "";
        float _statusUntil;

        GUIStyle _title, _button, _small, _tabStyle;
        readonly StringBuilder _text = new StringBuilder(64);

        /// <summary>Sahnede yoksa kurar. Oyun oturumu her başladığında çağrılır.</summary>
        public static void Ensure()
        {
            if (_instance != null) return;
            var holder = new GameObject("DevMenu");
            DontDestroyOnLoad(holder);
            _instance = holder.AddComponent<DevMenu>();
        }

        void Update()
        {
            if (_session == null)
                _session = FindFirstObjectByType<GameSession>();

#if UNITY_EDITOR
            // Editörde tek tuş: köşeye beş kez tıklamak fare ile zahmetli.
            // Cihazda dokunuş dizisi tek yol olduğu için o da kalıyor.
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame)
                _open = !_open;
#endif

            DetectSecretTap();
        }

        /// <summary>
        /// Gizli açılış: sol üst köşeye kısa sürede beş dokunuş.
        ///
        /// Sayaç PENCEREYLE sıfırlanır; oyuncu o köşeye zaman içinde beş kez
        /// dokunsa bile menü açılmaz, yalnız arka arkaya dokunuşlar sayılır.
        ///
        /// DERS (bu projede ESKİ Input sınıfı YOK): Player Settings'te girdi
        /// işleme yalnızca Input System paketine ayarlı. `UnityEngine.Input`
        /// okumak derlenir ama ÇALIŞMA ANINDA her karede
        /// InvalidOperationException atar — ve hata yığını girdi paketini
        /// gösterdiği için sebebi geç anlaşılır. Doğrusu `Pointer.current`:
        /// hem fareyi hem dokunmatiği tek arayüzle verir, yani aynı kod hem
        /// editörde hem telefonda çalışır.
        /// </summary>
        void DetectSecretTap()
        {
            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            // Köşe KARE bir alan: kenarı ekran genişliğinin bir oranı kadar.
            // Ekran koordinatlarında y aşağıdan yukarı arttığı için sol ÜST
            // köşe, y'nin yüksekliğe yakın olduğu yerdir.
            var point = pointer.position.ReadValue();
            float corner = Screen.width * CornerRatio;
            bool inCorner = point.x < corner && point.y > Screen.height - corner;
            if (!inCorner) { _tapCount = 0; return; }

            if (Time.unscaledTime - _firstTapTime > TapWindow) { _tapCount = 0; }
            if (_tapCount == 0) _firstTapTime = Time.unscaledTime;

            _tapCount++;
            if (_tapCount < TapsToOpen) return;

            _tapCount = 0;
            _open = !_open;
        }

        void OnGUI()
        {
            if (!_open) return;
            EnsureStyles();

            float s = Mathf.Max(1f, Screen.width / 540f);
            var panel = new Rect(Screen.width * 0.04f, Screen.height * 0.06f,
                                 Screen.width * 0.92f, Screen.height * 0.88f);

            GUI.color = new Color(0.05f, 0.04f, 0.12f, 0.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(panel.x + 12f * s, panel.y + 10f * s,
                                         panel.width - 24f * s, panel.height - 20f * s));

            GUILayout.BeginHorizontal();
            GUILayout.Label("GELİŞTİRİCİ MENÜSÜ", _title);
            if (GUILayout.Button("Kapat", _button, GUILayout.Width(110f * s))) _open = false;
            GUILayout.EndHorizontal();

            _tab = GUILayout.Toolbar(_tab, new[] { "Bölümler", "Kaynaklar", "Bilgi" }, _tabStyle,
                GUILayout.Height(46f * s));
            GUILayout.Space(8f * s);

            switch (_tab)
            {
                case 0: DrawLevels(s); break;
                case 1: DrawResources(s); break;
                default: DrawInfo(s); break;
            }

            if (Time.unscaledTime < _statusUntil)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(_status, _small);
            }

            GUILayout.EndArea();
        }

        void DrawLevels(float s)
        {
            int count = LevelCatalog.Count;
            if (count == 0) { GUILayout.Label("Bölüm bulunamadı.", _small); return; }

            int current = _session != null ? _session.LevelIndex : -1;
            GUILayout.Label(current >= 0 ? $"Şu an: Bölüm {current + 1} / {count}"
                                         : $"{count} bölüm", _small);

            // Kaydırma alanı: 50 bölüm ekrana sığmaz, taşan satırlar
            // seçilemiyordu. FlexibleSpace'siz bir ScrollView bunu çözer.
            _scroll = GUILayout.BeginScrollView(_scroll);

            const int perRow = 6;
            for (int row = 0; row * perRow < count; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < perRow; col++)
                {
                    int index = row * perRow + col;
                    if (index >= count) { GUILayout.FlexibleSpace(); continue; }

                    bool isCurrent = index == current;
                    var previous = GUI.backgroundColor;
                    if (isCurrent) GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);

                    if (GUILayout.Button((index + 1).ToString(), _button,
                            GUILayout.Height(56f * s)))
                        JumpTo(index);

                    GUI.backgroundColor = previous;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bölümü yeniden başlat", _button, GUILayout.Height(52f * s)))
            {
                _session?.Restart();
                Note("yeniden başlatıldı");
                _open = false;
            }
            if (GUILayout.Button("Hepsini aç", _button, GUILayout.Height(52f * s)))
            {
                if (MetaServices.Ready)
                {
                    // Doğrudan kayda yazılıyor: ProgressService'e yalnız hata
                    // ayıklama için bir "hepsini aç" metodu eklemek, yayına
                    // giden koda test kapısı açmak olurdu.
                    MetaServices.Save.Mutate(data => data.HighestUnlockedIndex = count - 1);
                    Note("tüm bölümler açıldı");
                }
            }
            GUILayout.EndHorizontal();
        }

        void JumpTo(int index)
        {
            _open = false;
            if (_session != null && AppRoot.Current != null && AppRoot.Current.InGame)
                _session.GoToLevel(index);
            else
                AppRouter.PlayLevel(index);
            Note($"Bölüm {index + 1}");
        }

        void DrawResources(float s)
        {
            if (!MetaServices.Ready) { GUILayout.Label("Meta servisler hazır değil.", _small); return; }

            var progress = MetaServices.Progress;
            GUILayout.Label($"Jeton: {progress.Coins}    Can: {MetaServices.Lives.Current}", _small);
            GUILayout.Space(6f * s);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1000 jeton", _button, GUILayout.Height(52f * s)))
            { progress.GrantCoins(1000); Note("+1000 jeton"); }
            if (GUILayout.Button("Canları doldur", _button, GUILayout.Height(52f * s)))
            { MetaServices.Lives.Grant(MetaServices.MaxLives); Note("canlar dolduruldu"); }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f * s);
            GUILayout.BeginHorizontal();
            foreach (PowerUpKind kind in System.Enum.GetValues(typeof(PowerUpKind)))
            {
                var captured = kind;
                if (GUILayout.Button($"+5 {PowerUpInfo.Label(captured)}", _button,
                        GUILayout.Height(52f * s)))
                {
                    string id = captured.ToString();
                    progress.SetPowerUpCount(id, progress.PowerUpCount(id) + 5);
                    Note($"+5 {PowerUpInfo.Label(captured)}");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(14f * s);
            GUI.backgroundColor = new Color(1f, 0.45f, 0.4f);
            if (GUILayout.Button("KAYDI SIFIRLA", _button, GUILayout.Height(56f * s)))
            { MetaServices.Save.Reset(); Note("kayıt sıfırlandı"); }
            GUI.backgroundColor = Color.white;
        }

        void DrawInfo(float s)
        {
            _text.Clear()
                 .Append("Sürüm: ").Append(Application.version).Append('\n')
                 .Append("Unity: ").Append(Application.unityVersion).Append('\n')
                 .Append("Cihaz: ").Append(SystemInfo.deviceModel).Append('\n')
                 .Append("Ekran: ").Append(Screen.width).Append('x').Append(Screen.height)
                 .Append(" @").Append(Screen.dpi.ToString("0")).Append("dpi\n")
                 .Append("Grafik: ").Append(SystemInfo.graphicsDeviceName).Append('\n')
                 .Append("Bellek: ").Append(SystemInfo.systemMemorySize).Append(" MB\n");

            if (MetaServices.Ready)
                _text.Append("Kayıt: ").Append(MetaServices.Save.Outcome).Append('\n');

            GUILayout.Label(_text.ToString(), _small);

            GUILayout.Space(10f * s);
            if (GUILayout.Button(GameKit.Services.PerfProbe.Visible ? "FPS sayacını kapat"
                                                                    : "FPS sayacını aç",
                    _button, GUILayout.Height(52f * s)))
                GameKit.Services.PerfProbe.Visible = !GameKit.Services.PerfProbe.Visible;
        }

        void Note(string message)
        {
            _status = message;
            _statusUntil = Time.unscaledTime + 2.5f;
        }

        void EnsureStyles()
        {
            if (_title != null) return;
            float s = Mathf.Max(1f, Screen.width / 540f);

            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(26f * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            _title.normal.textColor = new Color(0.7f, 1f, 0.75f);

            _button = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(22f * s) };
            _tabStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(22f * s) };

            _small = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(20f * s),
                wordWrap = true
            };
            _small.normal.textColor = new Color(0.85f, 0.86f, 0.95f);
        }
#else
        // Yayın yapısında hiçbir şey yapmaz; çağrı yerleri #if ile
        // kirlenmesin diye boş bir Ensure bırakılıyor.
        public static void Ensure() { }
#endif
    }
}
