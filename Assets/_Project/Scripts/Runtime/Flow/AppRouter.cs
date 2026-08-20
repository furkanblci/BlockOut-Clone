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
            Services.AudioService.PlayMusic(Services.Sfx.MusicMenu);
            AppRoot.Current.ShowMenu();
        }

        public static void PlayLevel(int levelIndex)
        {
            if (AppRoot.Current == null) { Missing(); return; }
            // MÜZİĞİ KİMSE BAŞLATMIYORDU.
            //
            // BULUNAN HATA (ses dosyaları geldiğinde ortaya çıktı):
            // `AudioService.PlayMusic` yazılmış, ayarlara bağlanmış ve
            // duraklat panelindeki "Musics" anahtarına takılmıştı — ama
            // projede onu ÇAĞIRAN tek bir satır yoktu. Dosya konsa bile
            // müzik hiç çalmayacaktı; anahtar da hiçbir şeyi açıp
            // kapatmayacaktı.
            //
            // DERS (bir sistemin "hazır" olması, BAĞLI olması demek değil):
            // Oynatıcı, ayar, kütüphane ve içe aktarıcı hepsi doğruydu;
            // eksik olan tek şey zincirin ilk halkasıydı. Sessizliğin sebebi
            // "dosya yok" sanıldığı için de aranmamıştı.
            //
            // Menü ve oynanış aynı parçayı paylaşıyorsa `PlayMusicInternal`
            // erken çıkıyor — yani geçişte müzik baştan başlamıyor, akmaya
            // devam ediyor.
            Services.AudioService.PlayMusic(Services.Sfx.MusicGameplay);
            LastPlayedLevelIndex = Mathf.Max(0, levelIndex);
            AppRoot.Current.PlayLevel(LastPlayedLevelIndex);
        }

        public static bool SceneExists(string sceneName) =>
            GameKit.Flow.SceneRouter.SceneExists(sceneName);

        static void Missing() => Debug.LogError(
            "[AppRouter] Sahnede AppRoot yok. Tools > Block Out > Kurulumu Şimdi Çalıştır.");
    }
}
