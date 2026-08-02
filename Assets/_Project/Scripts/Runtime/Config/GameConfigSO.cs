using UnityEngine;

namespace BlockOut.Runtime.Config
{
    /// <summary>
    /// Oyunun ayar sabitleri — tek bir asset olarak yaşar.
    ///
    /// DERS (ScriptableObject nedir?): MonoBehaviour bir sahne nesnesine
    /// yapışmak zorundadır; ScriptableObject ise sahneden bağımsız, diskte
    /// .asset dosyası olarak duran bir veri kabıdır. "Ayarları koddan çıkar,
    /// asset'e taşı" deseni sayesinde bir tasarımcı (ya da sen) kodu hiç
    /// açmadan Inspector'dan değer değiştirebilir. JSON'dan farkı: SO'lar
    /// Unity asset'lerine (materyal, prefab, ses) referans tutabilir ve
    /// build'e gömülür; JSON ise saf veridir, dışarıdan bile indirilebilir.
    /// </summary>
    [CreateAssetMenu(menuName = "Block Out/Game Config", fileName = "GameConfig")]
    public sealed class GameConfigSO : ScriptableObject
    {
        [Header("Sürükleme (DragSolver)")]
        [Tooltip("Bir karede alınacak maksimum alt adım (hücre biriminde). Küçük değer = dar koridor dönüşlerinde hassasiyet.")]
        [Range(0.05f, 0.5f)] public float dragSubstep = 0.15f;

        [Tooltip("Çarpışma testlerinde bloğun küçültülme payı — kayan nokta sürtünmesini önler.")]
        public float collisionEpsilon = 0.02f;

        [Tooltip("Bloğun kapıya 'temas etti' sayılması için gereken maksimum boşluk (hücre biriminde).")]
        public float gateContactGap = 0.05f;

        [Header("Animasyon süreleri (saniye)")]
        [Tooltip("Blok kapıdan emilirken oynayan tween süresi.")]
        public float absorbDuration = 0.25f;

        [Tooltip("Bırakılan bloğun en yakın hücreye oturma süresi.")]
        public float snapDuration = 0.08f;

        [Header("Zamanlayıcı")]
        [Tooltip("Kalan süre bu saniyenin altına inince uyarı durumuna geçilir (kırmızı yanıp sönme).")]
        public int warningSeconds = 30;
    }
}
