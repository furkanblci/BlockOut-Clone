using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockOut.Runtime.Flow
{
    /// <summary>
    /// Sahneler arası geçiş ve aralarında taşınan tek parça niyet: "hangi bölüm".
    ///
    /// DERS (sahneler arası veri taşıma): Yeni sahne yüklenince eski sahnenin
    /// TÜM nesneleri yok olur. Veriyi taşımanın yolları: (a) DontDestroyOnLoad
    /// bir taşıyıcı nesne — sahne listesini kirletir, çift örnek riski taşır;
    /// (b) PlayerPrefs — diske yazmak sadece "hangi bölüm" için savurganlık;
    /// (c) statik bir alan — sahne yüklemeleri statikleri sıfırlamaz.
    /// (c) en ucuzu ve burada en doğrusu: taşınan şey tek bir tam sayı ve
    /// ömrü bir sahne geçişi kadar.
    ///
    /// DERS (neden "istek" deyip geçmiyoruz?): Gameplay sahnesi tek başına da
    /// (editörde doğrudan Play'e basınca) açılabilmeli. Bu yüzden istek
    /// OKUNDUĞUNDA tüketilir ve varsayılana döner — sahne kimin çağırdığını
    /// bilmek zorunda kalmaz.
    /// </summary>
    public static class AppRouter
    {
        public const string BootScene = "Boot";
        public const string HomeScene = "Home";
        public const string GameplayScene = "Gameplay";

        /// <summary>-1 = istek yok; Gameplay kendi varsayılanını (kaldığı bölüm) kullanır.</summary>
        static int _requestedLevelIndex = -1;

        /// <summary>Bölüm bitince Home'a dönerken hangi bölümün oynandığını göstermek için.</summary>
        public static int LastPlayedLevelIndex { get; private set; } = -1;

        /// <summary>İsteği OKUR VE TÜKETİR — ikinci çağrı -1 döner.</summary>
        public static int ConsumeRequestedLevel()
        {
            int index = _requestedLevelIndex;
            _requestedLevelIndex = -1;
            if (index >= 0) LastPlayedLevelIndex = index;
            return index;
        }

        public static void GoHome() => SceneManager.LoadScene(HomeScene);

        public static void PlayLevel(int levelIndex)
        {
            _requestedLevelIndex = Mathf.Max(0, levelIndex);
            SceneManager.LoadScene(GameplayScene);
        }

        /// <summary>
        /// Sahne derleme listesinde var mı? Kurulum unutulduğunda sessiz siyah
        /// ekran yerine anlaşılır bir hata vermek için.
        /// </summary>
        public static bool SceneExists(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName) return true;
            }
            return false;
        }
    }
}
