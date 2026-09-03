using UnityEditor;

namespace GameKit.Editor.Setup
{
    /// <summary>
    /// Düzenleyicide kare yakalayan HER aracın önce çağırması gereken hazırlık.
    ///
    /// Bir kez kur-unut değil: yakalayan araçların statik kurucusundan VE her
    /// <c>Camera.Render()</c> çağrısından önce çağrılır (ayar domain reload'da
    /// varsayılana döner).
    /// </summary>
    public static class EditorCapture
    {
        /// <summary>
        /// Shader'ları senkron derlet — yoksa yakalanan kare MAGENTA çıkar.
        ///
        /// BULUNAN HATA: Bir domain reload'dan sonraki ilk yakalamalarda
        /// tahtanın ızgara çizgileri MAGENTA (212,0,212) çıkıyordu. Materyal
        /// doğruydu (`Universal Render Pipeline/Unlit`, renk 0,0,0,0.34,
        /// kuyruk 2990) ve konsolda tek hata yoktu. Sebep: Unity düzenleyicide
        /// bir shader VARYANTI henüz derlenmemişse o nesneyi magenta "bekliyor"
        /// rengiyle çiziyor; `Camera.Render()` derlemeyi beklemiyor.
        ///
        /// ÖLÇÜLDÜ: aynı sahne art arda üç kez yakalandı, üçünde de tam
        /// 18 476 magenta piksel — yani geçici değil, TAKILI kalmış bir durum.
        /// Ayar ayrı bir çağrıda kapatılınca 0'a düştü.
        ///
        /// Ayar geri AÇILMIYOR: kapatıp aynı çağrının sonunda geri açmak bir
        /// sonraki yakalamayı yeniden bozuyor (ilk kare yine varyantı bekliyor).
        /// Bu bir doğrulama aracı; senkron derleme birkaç yüz milisaniye
        /// yavaşlatır, karşılığında kare her zaman doğru.
        ///
        /// DERS (doğrulama aracının kendi yalanı en tehlikelisidir): Kare
        /// "ızgara bozuk, blokların üstüne çiziliyor" diyordu ve oyunda öyle
        /// bir hata yok. Ölçüm aracı, ölçtüğü şeyin durumunu değil KENDİ
        /// durumunu gösterebiliyorsa önce onu sabitlemek gerekir.
        /// </summary>
        public static void EnsureSynchronousShaders()
        {
            if (ShaderUtil.allowAsyncCompilation)
                ShaderUtil.allowAsyncCompilation = false;
        }
    }
}
