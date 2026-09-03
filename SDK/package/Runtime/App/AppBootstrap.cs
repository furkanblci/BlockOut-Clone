using GameKit.Services;
using UnityEngine;

namespace GameKit.App
{
    /// <summary>
    /// Uygulama açılışında bir kez koşan cihaz ayarları.
    ///
    /// DERS ([RuntimeInitializeOnLoadMethod]): Sahneye nesne koymadan, oyunun
    /// ilk karesinden önce kod çalıştırmanın yolu. Mobilde iki ayar kritiktir:
    /// hedef kare hızı (Unity'nin mobil varsayılanı 30'dur — 60 istiyorsak
    /// açıkça söylemeliyiz) ve ekranın uyumaması.
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Configure()
        {
            // vSync açıkken targetFrameRate yok sayılır; mobilde kapatıp
            // kare hızını biz belirliyoruz.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Analitik: olaylar ilk kareden itibaren toplansın. Sağlayıcıyı
            // sonra bağlamak, açılıştaki olayları kaybetmek demek.
            Analytics = new LocalAnalytics();
            Analytics.Initialize();
            GameKit.Services.Analytics.SetProvider(Analytics);

            Application.quitting += () => Analytics.Save();
        }

        /// <summary>Geliştirici menüsü özet raporu için erişilir.</summary>
        public static LocalAnalytics Analytics { get; private set; }
    }
}
