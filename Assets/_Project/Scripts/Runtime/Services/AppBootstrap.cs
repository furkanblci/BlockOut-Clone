using UnityEngine;

namespace BlockOut.Runtime.Services
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
        }
    }
}
