using BlockOut.Runtime.Services;
using UnityEngine;

namespace BlockOut.Runtime.Flow
{
    /// <summary>
    /// Boot sahnesinin tek işi: her şey hazır mı diye bakıp Home'a geçmek.
    ///
    /// DERS (neden ayrı bir Boot sahnesi?): Uygulamanın ilk açtığı sahne
    /// mümkün olduğunca BOŞ olmalı. Ağır bir sahne açılışta yüklenirse marka
    /// ekranı bile gösterilemeden saniyeler geçer. Boş bir Boot sahnesi anında
    /// açılır, arka planda kurulum yapılır, sonra asıl ekrana geçilir. Ayrıca
    /// "uygulama nereden başlar" sorusunun tek ve net bir cevabı olur.
    ///
    /// NOT: Servislerin kendisi [RuntimeInitializeOnLoadMethod] ile ilk kareden
    /// ÖNCE kurulur (MetaServices). Boot sahnesi onların kurulmasını beklemez;
    /// yalnızca doğrulayıp yönlendirir.
    /// </summary>
    public sealed class BootLoader : MonoBehaviour
    {
        void Start()
        {
            if (MetaServices.Ready)
                MetaServices.Lives.Refresh();   // kapalı geçen süre hemen cana dönsün

            if (!AppRouter.SceneExists(AppRouter.HomeScene))
            {
                Debug.LogError(
                    $"[Boot] '{AppRouter.HomeScene}' sahnesi derleme listesinde yok. " +
                    "Tools > Block Out > Kurulumu Şimdi Çalıştır komutunu koştur.");
                return;
            }

            AppRouter.GoHome();
        }
    }
}
