using UnityEngine;

namespace BlockOut.Runtime.Flow
{
    /// <summary>
    /// Ekranlar arası geçişin tek adresi. Artık sahne YÜKLEMEZ — tek sahnelik
    /// yapıda <see cref="AppRoot"/>'a devreder.
    ///
    /// DERS (statik cephe, örnek gövde): Çağıran taraflar (ana ekran düğmesi,
    /// HUD, bölüm sonu ekranı) bir referans taşımak zorunda kalmasın diye giriş
    /// noktası statik. Ama işi yapan, sahnedeki gerçek nesne. Böylece hem
    /// çağırmak kolay hem de mantık test edilebilir bir bileşende duruyor.
    /// </summary>
    public static class AppRouter
    {
        public const string BootScene = "Boot";
        public const string MainScene = "Main";

        /// <summary>Kaç numaralı bölüm oynanıyor (bitiş ekranı için).</summary>
        public static int LastPlayedLevelIndex { get; private set; } = -1;

        public static void GoHome()
        {
            if (AppRoot.Current == null) { Missing(); return; }
            AppRoot.Current.ShowMenu();
        }

        public static void PlayLevel(int levelIndex)
        {
            if (AppRoot.Current == null) { Missing(); return; }
            LastPlayedLevelIndex = Mathf.Max(0, levelIndex);
            AppRoot.Current.PlayLevel(LastPlayedLevelIndex);
        }

        public static bool SceneExists(string sceneName) =>
            GameKit.Flow.SceneRouter.SceneExists(sceneName);

        static void Missing() => Debug.LogError(
            "[AppRouter] Sahnede AppRoot yok. Tools > Block Out > Kurulumu Şimdi Çalıştır.");
    }
}
