using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Kendi kendini kuran proje (self-bootstrapping project).
    ///
    /// DERS: [InitializeOnLoadMethod], Unity her "domain reload"da (proje
    /// açılışı, kod derlemesi, Play'den dönüş) bu metodu çağırır. Stüdyolarda
    /// bu desen "yeni katılan kişi repoyu çeker, Unity'yi açar, HER ŞEY hazır"
    /// deneyimi için kullanılır — README'de elle kurulum adımı bırakılmaz.
    ///
    /// Kurulum adımları idempotent olduğu için her reload'da çalışması
    /// zararsızdır: eksik yoksa hiçbir şey yapmaz.
    /// </summary>
    public static class ProjectBootstrap
    {
        [InitializeOnLoadMethod]
        static void OnDomainReload()
        {
            // Asset oluşturmak import/serileştirme ortasında güvenli değildir;
            // delayCall işi ilk boş editör karesine erteler.
            EditorApplication.delayCall += RunIfNeeded;
        }

        static void RunIfNeeded()
        {
            // Play moduna girerken dokunma — oyun başlatmayı geciktirmeyelim.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            bool createdAssets = ProjectSetupTool.EnsureConfigAssets();
            bool createdScene = ProjectSetupTool.EnsureGameplayScene();
            bool createdMats = ProjectSetupTool.EnsureBlockMaterials();
            bool wiredScene = ProjectSetupTool.EnsureGameplayWiring();

            if (createdAssets || createdScene || createdMats || wiredScene)
                Debug.Log("[Bootstrap] Eksik kurulum otomatik tamamlandı: " +
                          (createdAssets ? "config asset'leri, " : "") +
                          (createdScene ? "Gameplay sahnesi, " : "") +
                          (createdMats ? "blok materyalleri, " : "") +
                          (wiredScene ? "sahne bağlantıları, " : "") +
                          "— hiçbir menüye tıklamana gerek yok.");
        }
    }
}
