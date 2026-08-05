using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameKit.Flow
{
    /// <summary>
    /// Sahne geçişleri ve aralarında taşınan tek parça niyet.
    ///
    /// DERS (sahneler arası veri taşıma): Yeni sahne yüklenince eski sahnenin TÜM
    /// nesneleri yok olur. Taşımanın yolları: (a) DontDestroyOnLoad bir taşıyıcı —
    /// sahne listesini kirletir, çift örnek riski taşır; (b) PlayerPrefs — diske
    /// yazmak tek bir tam sayı için savurganlık; (c) statik alan — sahne
    /// yüklemeleri statikleri sıfırlamaz. Ömrü bir sahne geçişi kadar olan küçük
    /// bir niyet için (c) en ucuzu ve en doğrusu.
    ///
    /// DERS (istek TÜKETİLİR): Hedef sahne tek başına da açılabilmeli (editörde
    /// doğrudan Play). Bu yüzden istek okununca sıfırlanır ve sahne "beni kim
    /// çağırdı" diye sormak zorunda kalmaz — çağıran yoksa kendi varsayılanını
    /// kullanır.
    /// </summary>
    public static class SceneRouter
    {
        /// <summary>-1 = istek yok.</summary>
        static int _pendingIntent = -1;

        /// <summary>En son taşınan niyet (teşhis ve geri dönüş ekranları için).</summary>
        public static int LastIntent { get; private set; } = -1;

        /// <summary>İsteği OKUR VE TÜKETİR — ikinci çağrı -1 döner.</summary>
        public static int ConsumeIntent()
        {
            int value = _pendingIntent;
            _pendingIntent = -1;
            if (value >= 0) LastIntent = value;
            return value;
        }

        /// <summary>Niyeti taşıyarak sahne yükler.</summary>
        public static void Load(string sceneName, int intent = -1)
        {
            _pendingIntent = intent;
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Sahne derleme listesinde var mı? Kurulum unutulduğunda sessiz siyah
        /// ekran yerine anlaşılır bir hata verebilmek için.
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
