using UnityEngine;
using UnityEngine.UI;

namespace GameKit.UI
{
    /// <summary>
    /// Esnek kaydırmanın uçlarda ne kadar TAŞABİLECEĞİNİ sınırlar.
    ///
    /// Neden gerekiyor: Unity'nin <see cref="ScrollRect"/> bileşeninde
    /// `Elastic` kipin taşma mesafesi AYARLANAMAZ. `elasticity` alanı yalnız
    /// GERİ DÖNÜŞ süresini belirliyor; sürüklerken ne kadar açıldığını,
    /// motorun içine gömülü sabit bir lastik katsayısı (0,55) belirliyor.
    /// Yani uzun bir parmak hareketi listeyi ekran boyu kadar açabiliyor.
    ///
    /// Kullanıcı: "en altta basılı tutup yukarı kaldırdığımızda çok geliyor,
    /// zemin o kadar kaldıramamalıyız, bunun yarısı kadar filan gelsin."
    /// Ölçülebilir sonucu: mağazada içerik o kadar açılıyordu ki bölüm
    /// zeminlerinin sonu geçiliyor ve arkadaki menü zemini görünüyordu —
    /// "zemin kopuyor" denen şey buydu.
    ///
    /// DERS (kırpmak, tasarımı da SADELEŞTİRİR): Taşma serbestken zemine
    /// 700 birimlik bir yedek pay eklenmişti; yani sorun görselle
    /// örtülüyordu. Taşma sınırlanınca o paya da gerek kalmıyor. Bir
    /// davranışı sınırlamak, onun etrafına yığılmış telafileri de düşürüyor.
    ///
    /// `Clamped` kipe geçmek de bir seçenekti ama esnemeyi tümden
    /// öldürüyor: listenin sonuna geldiğini söyleyen şey o küçük yaylanma.
    /// Burada yaylanma duruyor, yalnız boyu kısalıyor.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class UiScrollOvershoot : MonoBehaviour
    {
        /// <summary>Uçlarda izin verilen en fazla taşma (kanvas birimi).</summary>
        [SerializeField] float limit = 160f;

        public float Limit { get => limit; set => limit = Mathf.Max(0f, value); }

        ScrollRect _scroll;

        void Awake() => _scroll = GetComponent<ScrollRect>();

        /// <summary>
        /// `LateUpdate` şart: ScrollRect konumu kendi `LateUpdate`'inde
        /// yazıyor. `Update`'te kırpmak, aynı karede üzerine yazıldığı için
        /// hiçbir şey yapmazdı.
        /// </summary>
        void LateUpdate()
        {
            if (_scroll == null || _scroll.content == null || _scroll.viewport == null) return;

            var content = _scroll.content;
            Vector2 position = content.anchoredPosition;

            if (_scroll.vertical)
            {
                float slack = content.rect.height - _scroll.viewport.rect.height;
                position.y = Clamp(position.y, slack);
            }

            if (_scroll.horizontal)
            {
                float slack = content.rect.width - _scroll.viewport.rect.width;
                position.x = -Clamp(-position.x, slack);
            }

            if (position != content.anchoredPosition) content.anchoredPosition = position;
        }

        /// <summary>
        /// İçerik görünümden KISA olduğunda kaydırılacak bir şey yok; o
        /// durumda izin verilen aralık tek bir nokta artı taşma payı.
        /// Bunu ayrıca ele almazsak negatif bir "boşluk" değeri aralığı ters
        /// çevirir ve içerik yerinde titrer.
        /// </summary>
        float Clamp(float value, float slack)
        {
            float high = Mathf.Max(0f, slack);
            return Mathf.Clamp(value, -limit, high + limit);
        }
    }
}
