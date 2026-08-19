using UnityEngine;
using UnityEngine.UI;

namespace GameKit.UI
{
    /// <summary>
    /// Yuvarlak köşe yarıçapını kutunun KISA KENARININ ORANI olarak tutar.
    ///
    /// DERS (yarıçap sabit piksel DEĞİL, orandır): Referans oyunun bütün
    /// yüzeyleri ölçüldü — küçük kare düğme (64×61), duraklat düğmesi (64×60),
    /// yardımcı düğmesi (98×90), adet rozeti (45×50), Off/On anahtarı (76×50).
    /// Hepsinde yarıçap kısa kenarın **%20-23**'ü çıktı. Yani tasarımcı tek bir
    /// piksel değeri değil, tek bir ORAN kullanmış.
    ///
    /// Bizim <c>UiKit.CreateRoundedPanel</c> ise herkese SABİT ~36 piksel
    /// yarıçap veriyordu. Sonuç iki yönde birden yanlıştı:
    /// - Kısa öğelerde (jeton plakası ~73px, anahtar ~91px) 36 piksel kısa
    ///   kenarın yarısına yaklaşıyor, kutu HAP şekline dönüyordu. Kullanıcının
    ///   "çok oval, kötü duruyor" dediği şey tam olarak buydu.
    /// - Büyük panellerde 36 piksel oransal olarak KÜÇÜK kalıyor, referansın
    ///   yumuşak köşesi yerine keskin bir kutu çıkıyordu.
    ///
    /// DERS (neden bileşen, neden çağrı yerinde hesap değil?): Yarıçap kutunun
    /// boyuna bağlı, kutunun boyu ise yerleşim çalışana kadar BİLİNMİYOR.
    /// Çağrı yerinde hesaplamak, o an sıfır olan bir genişlikle hesaplamak
    /// demekti. Bu bileşen ölçü her değiştiğinde kendini yeniden hesaplıyor;
    /// ekran döndüğünde, güvenli alan değiştiğinde ve düzen aracıyla kutu elle
    /// büyütüldüğünde de doğru kalıyor.
    ///
    /// <see cref="UiSliceFit"/> ile aynı alanı (<c>pixelsPerUnitMultiplier</c>)
    /// yazdığı için ikisi AYNI nesnede bulunamaz; bu bileşen zaten payların
    /// kutuya sığmasını da garanti ettiği için ona gerek de kalmaz.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public sealed class UiCornerFit : MonoBehaviour
    {
        /// <summary>
        /// Referans oyunun ev oranı. 64×61 düğmede 13px, 98×90 yardımcıda 20px,
        /// 45×50 rozette 10px ölçüldü — üçü de %20-22 bandında.
        /// </summary>
        public const float HouseShare = 0.22f;

        /// <summary>
        /// Yarıçap TAVANI (1080 genişlik referans çözünürlüğünde piksel).
        ///
        /// Oran tek başına büyük yüzeylerde çuvallıyor: duraklat paneli 542
        /// piksel geniş, %22'si 119 piksel eder ve panel bir baloncuğa döner.
        /// Referansta öyle değil — büyük panellerde yarıçap 34-48 piksel
        /// bandında SABİTLENİYOR (mağaza teklif kartı 946 genişlikte 34px,
        /// duraklat paneli 592 genişlikte ~34-48px). Yani tasarımcı "kısa
        /// kenarın oranı" ile "ekran genişliğinin ~%3.2'si" arasında hangisi
        /// KÜÇÜKSE onu kullanmış.
        ///
        /// Bu iki kuralı birleştirmek bütün ölçümleri aynı anda açıklıyor:
        /// 61px düğme → 13 (oran bağlar), 90px yardımcı → 20 (oran bağlar),
        /// 470px kart → 34 (tavan bağlar), 542px panel → 34 (tavan bağlar).
        /// </summary>
        public const float MaxRadius = 34f;

        /// <summary>
        /// Prosedürel yuvarlak panelin doku ölçüleri (<see cref="UiSprites"/>).
        /// Köşe YAYI 18 piksel, 9-dilim payı 20 piksel — pay yaydan biraz
        /// geniş ki yayın bittiği yer dilim sınırının içinde kalsın.
        /// </summary>
        public const float SpriteArcPixels = 18f;

        [SerializeField, Range(0f, 0.5f)] float share = HouseShare;
        [SerializeField] float maxRadius = MaxRadius;

        Image _image;
        RectTransform _rect;

        /// <summary>Yarıçap oranını değiştirir ve hemen uygular.</summary>
        public float Share
        {
            get => share;
            set { share = Mathf.Clamp(value, 0f, 0.5f); Fit(); }
        }

        /// <summary>Tavanı değiştirir (referanstan sapması gereken tek tük yüzeyler için).</summary>
        public float MaxRadiusPixels
        {
            get => maxRadius;
            set { maxRadius = Mathf.Max(1f, value); Fit(); }
        }

        void Awake()
        {
            _image = GetComponent<Image>();
            _rect = (RectTransform)transform;
        }

        void OnEnable() => Fit();

        void OnRectTransformDimensionsChange() => Fit();

        void Fit()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_rect == null) _rect = transform as RectTransform;
            if (_image == null || _rect == null) return;

            var sprite = _image.sprite;
            if (sprite == null || _image.type != Image.Type.Sliced) return;
            if (sprite.border == Vector4.zero) return;

            float width = _rect.rect.width;
            float height = _rect.rect.height;
            if (width <= 1f || height <= 1f) return;

            // Yarıçap kısa kenardan, tavanla sınırlı; yarısını asla geçmesin
            // (geçerse karşılıklı köşeler çakışır ve dilim ortası negatif kalır).
            float shortSide = Mathf.Min(width, height);
            float radius = Mathf.Min(shortSide * share, maxRadius);
            radius = Mathf.Clamp(radius, 1f, shortSide * 0.5f);

            // Çarpan payı BÖLER: yay 18 piksel ise, ekranda R piksel yay için
            // çarpan 18/R olmalı.
            float multiplier = SpriteArcPixels / radius;

            if (!Mathf.Approximately(_image.pixelsPerUnitMultiplier, multiplier))
                _image.pixelsPerUnitMultiplier = multiplier;
        }
    }
}
