using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameKit.DevTools
{
    /// <summary>
    /// Konsolun oyuna bağlanma noktası. Kit "oyun" diye bir şey bilmediği için
    /// duraklatmayı, girdi kilidini ve başlıktaki bağlam satırını oyundan ister.
    /// </summary>
    public interface IDevConsoleHost
    {
        /// <summary>Başlıkta görünen tek satır: "BÖLÜM 10 · PLAYING · 2:57" gibi.</summary>
        string Context { get; }

        /// <summary>Konsol açıkken oyunu durdur / devam ettir.</summary>
        void SetPaused(bool paused);

        /// <summary>Oynanış girdisini kes / aç.</summary>
        void SetInputBlocked(bool blocked);
    }

    /// <summary>
    /// Gizli geliştirici konsolu — KİTİN parçası, oyundan bağımsız.
    ///
    /// DERS (test aracı görünmez olmalı): Ekranın köşesinde duran bir "bölüm
    /// seç" düğmesi, testçinin oyunu OYNAMASINI engeller — herkes o düğmeye
    /// basar ve gerçek ilerleyişi kimse denemez. Görünür düğmesi yok; ÜST
    /// köşelerden birine art arda beş kez dokununca açılır.
    ///
    /// DERS (test aracı TEST EDİLEN YAPIDA olmalı): Bu kod eskiden
    /// `#if DEVELOPMENT_BUILD || UNITY_EDITOR` ile çevriliydi, yani normal bir
    /// APK'da HİÇ YOKTU. Cihaz testinde araç "açılmıyor" diye raporlandı —
    /// aslında açılacak bir şey yoktu. Portfolyo projelerinde her yapıda
    /// derleniyor; mağazaya gidecek bir oyunda tek yapılacak, sınıfı yeniden
    /// `#if` içine almak.
    ///
    /// DERS (araç ekranı KAPLAMAMALI): İlk sürüm tam ekrandı. Telefonda test
    /// ederken "şu an tahtada ne var" ile "konsol ne diyor" AYNI ANDA
    /// görülemiyordu; araç açıkken oyun yok, oyun varken araç yok. Panel artık
    /// ekranın bir kısmını kaplıyor (½ / ¾ / tam), üste ya da alta yaslanıyor
    /// ve tek dokunuşla küçülüp kenarda bir rozete dönüşüyor. Tercih
    /// PlayerPrefs'e yazılıyor: her açılışta yeniden ayarlamak zorunda
    /// kalmazsın.
    /// </summary>
    public sealed class DevConsole : MonoBehaviour
    {
        const int TapsToOpen = 5;
        const float TapWindow = 2.0f;
        const float CornerRatio = 0.16f;

        const string SizeKey = "gamekit.devconsole.size";
        const string CornerKey = "gamekit.devconsole.corner";

        /// <summary>
        /// Panelin kapladığı alan (genişlik, yükseklik oranı) — küçükten büyüğe.
        ///
        /// Varsayılan EN KÜÇÜK: ekranın sol üst çeyreğine yakın bir kutu. Araç
        /// ilk açıldığında oyunun görünür kalması, "ekranı kapladı, ne olduğunu
        /// göremiyorum" durumundan iyidir; büyütmek tek dokunuş, küçültmek ise
        /// ancak akıl edilirse yapılır.
        /// </summary>
        // Üç ölçü de %5 büyütüldü (kullanıcı isteği): en küçük kutu telefonda
        // altı sekme ve bir liste için dardı. En büyüğü zaten ekranı
        // kaplıyordu, o yüzden 1,00'de sabit kaldı.
        static readonly Vector2[] Sizes =
        {
            new Vector2(0.59f, 0.48f),
            new Vector2(0.84f, 0.69f),
            new Vector2(1.00f, 0.97f)
        };

        static readonly string[] SizeLabels = { "S", "M", "L" };

        // Yaslanılan köşe: 0 sol üst (varsayılan) · 1 sağ üst · 2 sağ alt · 3 sol alt.
        // Düğmedeki gösterge yazı tipinden bağımsız çizilir (bkz. DrawCornerGlyph).

        static DevConsole _instance;

        public static DevConsole Instance => _instance;
        public static bool IsOpen => _instance != null && _instance._open;

        /// <summary>Panel açık VE küçültülmemiş — oyun bu durumda duraklatılır.</summary>
        public static bool IsActive => _instance != null && _instance._open && !_instance._collapsed;

        /// <summary>Oyunun bağlantı noktası; kurulumda verilir.</summary>
        public static IDevConsoleHost Host { get; set; }

        /// <summary>Konsol açıkken oyun dursun mu? (SİSTEM sekmesinden değişir.)</summary>
        public static bool PauseWhileOpen = true;

        readonly List<DevPage> _pages = new List<DevPage>();
        readonly DevUi _ui = new DevUi();
        readonly List<EventSystem> _blockedUi = new List<EventSystem>();

        bool _open, _collapsed, _active;
        int _tab;
        int _size;
        int _corner;   // 0 sol üst (varsayılan) · 1 sağ üst · 2 sağ alt · 3 sol alt

        int _tapCount;
        float _firstTapTime;

        /// <summary>Sahnede yoksa kurar; her zaman geçerli bir örnek döner.</summary>
        public static DevConsole Ensure()
        {
            if (_instance != null) return _instance;

            // DERS (statik alan, nesneden ÖNCE ölür): Oyun çalışırken kod
            // değiştirilince Unity betikleri yeniden yükler; statik alan
            // sıfırlanır ama sahnedeki nesne yaşamaya devam eder. Yalnız statik
            // alana bakan bir Ensure o anda İKİNCİ bir konsol kurar ve iki
            // panel üst üste çizilir. Önce sahneye sormak bunu kapatıyor.
            _instance = FindFirstObjectByType<DevConsole>();
            if (_instance != null) return _instance;

            var holder = new GameObject("DevConsole");
            DontDestroyOnLoad(holder);
            _instance = holder.AddComponent<DevConsole>();
            return _instance;
        }

        /// <summary>Sekme ekler. Aynı türden ikinci bir sayfa eklenmez.</summary>
        public static void Register(DevPage page)
        {
            if (page == null) return;
            var console = Ensure();

            foreach (var existing in console._pages)
                if (existing.GetType() == page.GetType()) return;

            page.Ui = console._ui;
            console._pages.Add(page);
        }

        /// <summary>Kayıtlı sayfaları temizler (sahne/oyun değiştiren kurulumlar için).</summary>
        public static void ClearPages()
        {
            if (_instance == null) return;
            _instance._pages.Clear();
            _instance._tab = 0;
        }

        /// <summary>Konsolu dışarıdan açar/kapatır — editör araçları ve testler için.</summary>
        public static void SetVisible(bool value)
        {
            if (value) Ensure();
            if (_instance != null) _instance.Open(value);
        }

        /// <summary>Sekme seçer (0 tabanlı).</summary>
        public static void SelectTab(int index)
        {
            if (_instance == null || _instance._pages.Count == 0) return;
            _instance._tab = Mathf.Clamp(index, 0, _instance._pages.Count - 1);
            _instance._pages[_instance._tab].OnOpen();
        }

        void Awake()
        {
            _instance = this;
            DevLog.Install();
            _size = Mathf.Clamp(PlayerPrefs.GetInt(SizeKey, 0), 0, Sizes.Length - 1);
            _corner = Mathf.Clamp(PlayerPrefs.GetInt(CornerKey, 0), 0, 3);
        }

        void OnDestroy()
        {
            if (_active) Deactivate();
            if (_instance == this) _instance = null;
        }

        void Update()
        {
#if UNITY_EDITOR
            // Editörde tek tuş: köşeye beş kez tıklamak fare ile zahmetli.
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f8Key.wasPressedThisFrame) Open(!_open);
                if (_open && keyboard.escapeKey.wasPressedThisFrame) Open(false);
            }
#endif
            DetectSecretTap();

            // Sahne değişince yeni bir EventSystem doğabilir; panel açıkken
            // onun da susturulması gerekir.
            if (_active) BlockUi();
        }

        /// <summary>
        /// Gizli açılış: üst köşelerden birine kısa sürede beş dokunuş.
        ///
        /// DERS (yeni Input System'de `UnityEngine.Input` YOK): Girdi işleme
        /// yalnız Input System paketine ayarlıysa eski sınıfı okumak derlenir
        /// ama çalışma anında her karede istisna atar. `Pointer.current` hem
        /// fareyi hem dokunmatiği tek arayüzle verir: aynı kod editörde ve
        /// telefonda çalışır.
        /// </summary>
        void DetectSecretTap()
        {
            if (_open) return;   // açıkken paneldeki dokunuşlar sayılmasın

            var pointer = UnityEngine.InputSystem.Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;

            var point = pointer.position.ReadValue();
            float corner = Screen.width * CornerRatio;
            bool inCorner = point.y > Screen.height - corner &&
                            (point.x < corner || point.x > Screen.width - corner);

            if (!inCorner) { _tapCount = 0; return; }

            if (Time.unscaledTime - _firstTapTime > TapWindow) _tapCount = 0;
            if (_tapCount == 0) _firstTapTime = Time.unscaledTime;

            _tapCount++;
            if (_tapCount < TapsToOpen) return;

            _tapCount = 0;
            Open(true);
        }

        void Open(bool value)
        {
            _open = value;
            _collapsed = false;
            _tapCount = 0;
            _ui.ClearArmed();

            if (value && _pages.Count > 0) _pages[Mathf.Min(_tab, _pages.Count - 1)].OnOpen();
            SyncActive();
        }

        /// <summary>Paneli kenardaki rozete indirir / geri açar (başlıktaki "—").</summary>
        public void SetCollapsed(bool value) => Collapse(value);

        void Collapse(bool value)
        {
            _collapsed = value;
            _ui.ClearArmed();
            SyncActive();
        }

        /// <summary>
        /// Panel görünürlüğü değişince oyunun durumunu eşitler.
        ///
        /// DERS (bir aracı açmak bir DURUM değişikliğidir): İlk sürüm sadece bir
        /// bool çeviriyordu. Sonuç: konsol açıkken sayaç işlemeye devam ediyordu
        /// (bölümü incelerken süre bitiyordu), panele basan parmak arkadaki
        /// bloğu da sürüklüyordu ve HUD düğmelerine yanlışlıkla basılıyordu.
        /// Açılış tek bir yerden geçmezse bu üç düzeltmenin üçü de bir gün
        /// unutulur.
        /// </summary>
        void SyncActive()
        {
            bool active = _open && !_collapsed;
            if (active == _active) return;
            _active = active;

            if (active)
            {
                Host?.SetInputBlocked(true);
                if (PauseWhileOpen) Host?.SetPaused(true);
                BlockUi();
            }
            else
            {
                Deactivate();
            }
        }

        void Deactivate()
        {
            Host?.SetInputBlocked(false);
            Host?.SetPaused(false);
            UnblockUi();
        }

        /// <summary>SİSTEM sekmesi "açıkken duraklat" anahtarını çevirince.</summary>
        public void ApplyPausePreference()
        {
            if (!_active) return;
            Host?.SetPaused(PauseWhileOpen);
        }

        public void CycleSize()
        {
            _size = (_size + 1) % Sizes.Length;
            PlayerPrefs.SetInt(SizeKey, _size);
        }

        /// <summary>Sıradaki köşeye yaslar: sol üst → sağ üst → sağ alt → sol alt.</summary>
        public void CycleCorner()
        {
            _corner = (_corner + 1) % 4;
            PlayerPrefs.SetInt(CornerKey, _corner);
        }

        void BlockUi()
        {
            var current = EventSystem.current;
            if (current == null || !current.enabled) return;
            current.enabled = false;
            if (!_blockedUi.Contains(current)) _blockedUi.Add(current);
        }

        void UnblockUi()
        {
            foreach (var system in _blockedUi)
                if (system != null) system.enabled = true;
            _blockedUi.Clear();
        }

        // ------------------------------------------------------------------ çizim

        void OnGUI()
        {
            if (!_open) return;

            _ui.Refresh();
            // Kendi skin'imiz: kalın kaydırma çubuğu buradan geliyor.
            // IMGUI her `OnGUI` çağrısında skin'i varsayılana döndürdüğü için
            // bu atama her karede yapılmalı.
            if (_ui.Skin != null) GUI.skin = _ui.Skin;
            GUI.depth = -1000;   // oyunun kendi IMGUI katmanı (FPS sondası) altta kalsın

            if (_collapsed) { DrawPill(); return; }

            float u = _ui.U;
            var panel = PanelRect();

            DevUi.Fill(panel, DevInk.Panel);
            // İnce kenarlık: siyah panel siyah oyun zemininde kaybolmasın.
            DevUi.Fill(new Rect(panel.x, panel.y, panel.width, 1f * u), DevInk.Line);
            DevUi.Fill(new Rect(panel.x, panel.yMax - 1f * u, panel.width, 1f * u), DevInk.Line);
            DevUi.Fill(new Rect(panel.x, panel.y, 1f * u, panel.height), DevInk.Line);
            DevUi.Fill(new Rect(panel.xMax - 1f * u, panel.y, 1f * u, panel.height), DevInk.Line);

            float pad = 9f * u;
            float x = panel.x + pad;
            float width = panel.width - pad * 2f;
            float headerHeight = 28f * u;
            float tabsHeight = 30f * u;
            float footerHeight = 18f * u;

            DrawHeader(new Rect(x, panel.y + pad, width, headerHeight));
            DrawTabs(new Rect(x, panel.y + pad + headerHeight + 4f * u, width, tabsHeight));

            float top = panel.y + pad + headerHeight + tabsHeight + 12f * u;
            float bottom = panel.yMax - pad - footerHeight - 4f * u;
            var content = new Rect(x, top, width, Mathf.Max(40f * u, bottom - top));

            _ui.AreaWidth = content.width;

            GUILayout.BeginArea(content);
            if (_pages.Count == 0) _ui.Empty("Kayıtlı sayfa yok. DevConsole.Register(...) çağır.");
            else _pages[Mathf.Min(_tab, _pages.Count - 1)].Draw();
            GUILayout.EndArea();

            DrawFooter(new Rect(x, bottom + 4f * u, width, footerHeight));
        }

        /// <summary>Seçili köşeye yaslanmış panel dikdörtgeni.</summary>
        Rect PanelRect()
        {
            float margin = 6f * _ui.U;
            var size = Sizes[_size];
            float width = Screen.width * size.x - margin * 2f;
            float height = Screen.height * size.y - margin * 2f;

            bool right = _corner == 1 || _corner == 2;
            bool bottom = _corner == 2 || _corner == 3;

            return new Rect(right ? Screen.width - width - margin : margin,
                            bottom ? Screen.height - height - margin : margin,
                            width, height);
        }

        /// <summary>Küçültülmüş hâl: köşede küçük bir rozet. Oyun tamamen görünür.</summary>
        void DrawPill()
        {
            float u = _ui.U;
            float width = 84f * u, height = 28f * u, margin = 6f * u;

            bool right = _corner == 1 || _corner == 2;
            bool bottom = _corner == 2 || _corner == 3;

            var pill = new Rect(right ? Screen.width - width - margin : margin,
                                bottom ? Screen.height - height - margin : margin,
                                width, height);

            string label = "DEV";
            if (DevLog.Errors > 0) label += " " + DevUi.Tint(DevLog.Errors.ToString(), DevInk.Danger);

            if (GUI.Button(pill, label, _ui.S.Btn)) Collapse(false);
        }

        void DrawHeader(Rect r)
        {
            float u = _ui.U;
            float button = 30f * u;
            float gap = 3f * u;
            float buttons = button * 4f + gap * 3f;

            GUI.Label(new Rect(r.x, r.y, 40f * u, r.height), "DEV", _ui.S.Title);
            GUI.Label(new Rect(r.x + 42f * u, r.y, r.width - 42f * u - buttons - gap, r.height),
                      Host != null ? Host.Context : "", _ui.S.Muted);

            float bx = r.xMax - buttons;
            var size = new Rect(bx, r.y + 2f * u, button, r.height - 4f * u);
            var anchor = new Rect(bx + button + gap, size.y, button, size.height);
            var collapse = new Rect(bx + (button + gap) * 2f, size.y, button, size.height);
            var close = new Rect(bx + (button + gap) * 3f, size.y, button, size.height);

            if (GUI.Button(size, SizeLabels[_size], _ui.S.Btn)) CycleSize();

            // DERS (yazı tipinde olmayan simge, simge değildir): Köşe düğmesi
            // önce "◤◥◢◣" yazıyordu; Unity'nin gömülü yazı tipinde bu karakterler
            // yok ve cihazda boş kutu ya da bambaşka bir şekil çıkıyor. Kutucuğu
            // ÇİZMEK hem her yazı tipinde aynı görünür hem de ne anlattığı
            // (panel şu köşeye yaslı) bakışta anlaşılır.
            if (GUI.Button(anchor, GUIContent.none, _ui.S.Btn)) CycleCorner();
            DrawCornerGlyph(anchor);

            if (GUI.Button(collapse, "—", _ui.S.Btn)) Collapse(true);
            if (GUI.Button(close, "✕", _ui.S.BtnDanger)) Open(false);
        }

        void DrawCornerGlyph(Rect button)
        {
            float u = _ui.U;
            float pad = 8f * u, box = 8f * u;

            bool right = _corner == 1 || _corner == 2;
            bool bottom = _corner == 2 || _corner == 3;

            var frame = new Rect(button.x + pad, button.y + pad,
                                 button.width - pad * 2f, button.height - pad * 2f);
            DevUi.Fill(frame, DevInk.Line);
            DevUi.Fill(new Rect(right ? frame.xMax - box : frame.x,
                                bottom ? frame.yMax - box : frame.y,
                                box, box), DevInk.Text);
        }

        void DrawTabs(Rect r)
        {
            if (_pages.Count == 0) return;

            float gap = 3f * _ui.U;
            float width = (r.width - gap * (_pages.Count - 1)) / _pages.Count;

            for (int i = 0; i < _pages.Count; i++)
            {
                var cell = new Rect(r.x + i * (width + gap), r.y, width, r.height);
                bool active = _tab == i;

                string label = _pages[i].Title;
                string badge = _pages[i].Badge;
                if (!string.IsNullOrEmpty(badge))
                    label += " " + DevUi.Tint(badge, active ? Color.black : DevInk.Danger);

                if (!GUI.Button(cell, label, active ? _ui.S.TabOn : _ui.S.Tab)) continue;

                _tab = i;
                _ui.ClearArmed();
                _pages[i].OnOpen();
            }
        }

        /// <summary>
        /// Alt şerit yalnız SON İŞLEMİ gösterir.
        ///
        /// Yanında bir de "üst köşeye 5 dokunuş" ipucu duruyordu; aracı zaten
        /// açmış olan birine nasıl açılacağını söylemek yer kaybıdır — o bilgi
        /// SİSTEM sekmesinde, ihtiyaç duyulan yerde duruyor.
        /// </summary>
        void DrawFooter(Rect r)
        {
            GUI.Label(r, string.IsNullOrEmpty(DevLog.LastAction) ? "" : "› " + DevLog.LastAction,
                      _ui.S.Small);
        }
    }
}
