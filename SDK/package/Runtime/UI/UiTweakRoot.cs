using UnityEngine;

namespace GameKit.UI
{
    /// <summary>
    /// "Bu nesne elle düzenlenebilir bir ekranın köküdür" işareti.
    ///
    /// Tek bir string taşır; hiçbir metodu yoktur, hiçbir karede iş yapmaz.
    /// Var olma sebebi, editördeki Tasarım penceresinin ekranları ARAMADAN
    /// bulabilmesi ve kodun kullandığı anahtarın BİREBİR aynısını görmesi.
    ///
    /// DERS (iki taraf aynı anahtarı ayrı ayrı ÜRETMEMELİ): Düzeltmeler yola
    /// göre saklanıyor ("HomeScreen/Play/Ribbon"). Editör yolu bir kuralla,
    /// çalışma anı başka bir kuralla üretirse sistem hata vermez — sadece
    /// hiçbir düzeltme tutmaz ve sebebi görünmez. Anahtarı kod koyuyor,
    /// editör okuyor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiTweakRoot : MonoBehaviour
    {
        [Tooltip("Düzeltme yollarının başına gelen ekran anahtarı.")]
        public string Key;
    }
}
