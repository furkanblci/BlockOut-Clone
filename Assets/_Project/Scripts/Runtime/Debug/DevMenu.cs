using BlockOut.Runtime.Config;
using BlockOut.Runtime.Flow;
using GameKit.DevTools;
using UnityEngine;

namespace BlockOut.Runtime.DevTools
{
    /// <summary>
    /// Bu OYUNUN geliştirici konsolu kurulumu: kitin kabuğunu kurar, oyunun
    /// sayfalarını kaydeder ve duraklatma / girdi kilidi / başlık bağlamı
    /// isteklerini karşılar.
    ///
    /// DERS (kabuk kite, içerik oyuna): Konsolun kendisi artık
    /// <see cref="DevConsole"/> — gizli açılış, panel boyutu, siyah tema, log
    /// akışı, cihaz künyesi orada ve HİÇBİRİ bu oyunu tanımıyor. Bir sonraki
    /// oyun aynı kiti alır, kendi sayfalarını yazar ve aracın kabuğunu yeniden
    /// yazmaz. Buradaki dosya o iki dünyanın birleştiği tek nokta: on satırlık
    /// bir kurulum ve üç kancanın karşılığı.
    ///
    /// DERS (bir kancayı boş bırakmak = özelliği kapatmak): Kit, oyunu nasıl
    /// duraklatacağını ya da girdisini nasıl keseceğini bilemez; bunları
    /// <see cref="IDevConsoleHost"/> üzerinden sorar. Bu üç metot yazılmazsa
    /// konsol yine açılır ama arkasındaki oyun akmaya ve dokunuşları almaya
    /// devam eder — yani araç ölçtüğü şeyi bozar.
    /// </summary>
    public sealed class DevMenu : IDevConsoleHost
    {
        static DevMenu _host;
        static GameSession _session;
        static float _probeAt;

        /// <summary>Konsolun duraklattığı bir oturum var mı? (Sayfalar gösteriyor.)</summary>
        public static bool PausedByConsole { get; private set; }

        /// <summary>
        /// Oynanan oturum; menüdeyken null.
        ///
        /// Sahne taraması yarım saniyede bir yapılıyor: oturum her bölümde
        /// yeniden kuruluyor, her karede aramak ise boşa iş.
        /// </summary>
        public static GameSession Session
        {
            get
            {
                if (_session != null && Time.unscaledTime < _probeAt) return _session;
                _probeAt = Time.unscaledTime + 0.5f;
                _session = Object.FindFirstObjectByType<GameSession>();
                return _session;
            }
        }

        /// <summary>Konsolu kurar ve sekmelerini kaydeder. Tekrar çağrılması zararsız.</summary>
        public static void Ensure()
        {
            if (_host != null) return;
            _host = new DevMenu();

            DevConsole.Ensure();
            DevConsole.Host = _host;

            // Sıra sekme sırasıdır: en çok kullanılan solda.
            DevConsole.Register(new DevLevelsPage());
            DevConsole.Register(new DevPlayPage());
            DevConsole.Register(new DevSavePage());
            DevConsole.Register(new DevDataPage());
            DevConsole.Register(new DevLogPage());     // kitten
            DevConsole.Register(new DevSystemPage());  // kitten
        }

        /// <summary>Editör araçları ve testler için: konsolu aç/kapat.</summary>
        public static void SetVisible(bool value)
        {
            Ensure();
            DevConsole.SetVisible(value);
        }

        /// <summary>Editör araçları ve testler için: sekme seç (0 = BÖLÜM).</summary>
        public static void SelectTab(int index) => DevConsole.SelectTab(index);

        public static bool IsOpen => DevConsole.IsOpen;

        // ------------------------------------------------------------ IDevConsoleHost

        /// <summary>Başlıktaki tek satır: konsolun NEREDE açıldığını söyler.</summary>
        public string Context
        {
            get
            {
                var session = Session;
                bool inGame = session != null && AppRoot.Current != null && AppRoot.Current.InGame;

                // Başlık şeridi dar (panel ekranın yarısı olabilir): uzun metin
                // kırpılır ve kırpılmış bilgi bilgi değildir. Üç kısa parça.
                if (!inGame)
                    return DevUi.Tint("MENÜ", DevInk.Muted) + " · " + LevelCatalog.Count + " bölüm";

                Color stateColor = session.State == GameState.Won ? DevInk.Accent2
                                 : session.State == GameState.Lost ? DevInk.Danger
                                 : session.State == GameState.Paused ? DevInk.Warn
                                 : DevInk.Accent;

                return DevUi.Tint("L" + (session.LevelIndex + 1), DevInk.Text) + " " +
                       DevUi.Tint(session.State.ToString().ToUpperInvariant(), stateColor) + " " +
                       DevUi.Clock(session.Timer.Remaining);
            }
        }

        /// <summary>
        /// Konsol açıkken bölümü duraklatır, kapanınca kaldığı yerden sürdürür.
        ///
        /// Yalnız KENDİ duraklattığını geri açıyor: oyuncu duraklat düğmesine
        /// basmışken konsolu açıp kapatmak, oyunu haberi olmadan başlatmasın.
        /// </summary>
        public void SetPaused(bool paused)
        {
            var session = Session;
            if (session == null) { PausedByConsole = false; return; }

            if (paused)
            {
                if (PausedByConsole || session.State != GameState.Playing) return;
                session.SetPaused(true);
                PausedByConsole = true;
            }
            else
            {
                if (!PausedByConsole) return;
                PausedByConsole = false;
                session.SetPaused(false);
            }
        }

        /// <summary>
        /// Oynanış girdisini keser.
        ///
        /// DERS (IMGUI hiçbir şeyi engellemez): OnGUI ile çizilen panel yalnızca
        /// ÜSTE ÇİZER; altındaki oyun dünyası dokunuşları almaya devam eder.
        /// Konsol açıkken panele basan parmak aynı anda arkadaki bloğu da
        /// sürüklüyordu — yani test aracının kendisi test edilen durumu
        /// bozuyordu.
        /// </summary>
        public void SetInputBlocked(bool blocked) => Input.PointerInputService.Blocked = blocked;
    }
}
