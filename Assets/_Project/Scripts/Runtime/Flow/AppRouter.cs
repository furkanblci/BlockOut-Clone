using GameKit.Flow;

namespace BlockOut.Runtime.Flow
{
    /// <summary>
    /// Bu oyunun sahne adları ve geçişleri.
    ///
    /// DERS (kit ne bilir, oyun ne bilir?): Sahne yükleme ve niyet taşıma
    /// mekaniği her oyunda aynı → <see cref="SceneRouter"/>. Sahnelerin ADI ve
    /// "niyet = bölüm sırası" yorumu bu oyuna ait → burası. İnce bir katman ama
    /// kiti oyunun isimlendirmesinden temiz tutuyor.
    /// </summary>
    public static class AppRouter
    {
        public const string BootScene = "Boot";
        public const string HomeScene = "Home";
        public const string GameplayScene = "Gameplay";

        /// <summary>Home'un istediği bölüm; Gameplay bunu okuyup tüketir.</summary>
        public static int ConsumeRequestedLevel() => SceneRouter.ConsumeIntent();

        /// <summary>Son oynanan bölüm (bitiş ekranı için).</summary>
        public static int LastPlayedLevelIndex => SceneRouter.LastIntent;

        public static void GoHome() => SceneRouter.Load(HomeScene);

        public static void PlayLevel(int levelIndex) =>
            SceneRouter.Load(GameplayScene, UnityEngine.Mathf.Max(0, levelIndex));

        public static bool SceneExists(string sceneName) => SceneRouter.SceneExists(sceneName);
    }
}
