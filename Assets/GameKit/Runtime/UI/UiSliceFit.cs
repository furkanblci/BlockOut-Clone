using UnityEngine;
using UnityEngine.UI;

namespace GameKit.UI
{
    /// <summary>
    /// 9-dilim kenar paylarını kutunun boyuna göre küçültür.
    ///
    /// DERS (9-dilim payı SABİTTİR, kutu değildir): Bir düğme görselinin kenar
    /// payları piksel cinsinden sabittir — bizim düğme sprite'ımız 512×246 ve
    /// dikey payları 68 + 88 = 156 piksel. Kutuyu bundan alçak çizersen üst ve
    /// alt paylar çakışır; Unity ikisini birden sıkıştırır ve düğmenin baskılı
    /// 3B pahı EZİLİR. Ekranda "basık, bozuk" bir düğme görünür ve suç görselde
    /// sanılır — oysa görsel doğru, ona verilen kutu yanlış.
    ///
    /// Bu tam olarak "büyük düğmeler güzel, küçükler bozuk" tablosunu üretir:
    /// Yolculuk ekranındaki "Üst"/"Alt" düğmeleri ~100 piksel yüksekliğindeydi,
    /// yani payların altında kaldı.
    ///
    /// DERS (çözüm her çağrı yerine değil, TEK yere): Alternatif her küçük
    /// düğmeye elle `pixelsPerUnitMultiplier` yazmaktı — otuz çağrı yeri, ve
    /// biri ölçüsünü değiştirdiğinde sessizce yeniden bozulur. Bu bileşen
    /// kutunun boyu her değiştiğinde kendini yeniden hesaplıyor, dolayısıyla
    /// yerleşim değişse de doğru kalıyor.
    ///
    /// Büyük kutularda çarpan 1'de kalır: hiçbir şey değişmez, maliyet yok.
    /// </summary>
    [RequireComponent(typeof(Image))]
    [DisallowMultipleComponent]
    public sealed class UiSliceFit : MonoBehaviour
    {
        /// <summary>
        /// Kenar paylarının kutunun en fazla ne kadarını kaplayabileceği.
        /// %85: içeride esneyecek bir şerit kalsın ki köşeler birbirine değmesin.
        /// </summary>
        const float MaxBorderShare = 0.85f;

        Image _image;
        RectTransform _rect;

        void Awake()
        {
            _image = GetComponent<Image>();
            _rect = (RectTransform)transform;
        }

        void OnEnable() => Fit();

        /// <summary>RectTransform'un ölçüsü her değiştiğinde Unity bunu çağırır.</summary>
        void OnRectTransformDimensionsChange() => Fit();

        void Fit()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_rect == null) _rect = transform as RectTransform;
            if (_image == null || _rect == null) return;

            var sprite = _image.sprite;
            if (sprite == null || _image.type != Image.Type.Sliced) return;

            Vector4 border = sprite.border;
            if (border == Vector4.zero) return;

            float width = _rect.rect.width;
            float height = _rect.rect.height;
            if (width <= 1f || height <= 1f) return;

            // border = (sol, alt, sağ, üst) — SPRITE PİKSELİ cinsinden, kutu
            // ise KANVAS BİRİMİ. İkisi ancak sprite'ın piksel/birim oranı
            // kanvasınkiyle (100) aynıysa doğrudan karşılaştırılabilir.
            // Yüksek çözünürlüklü sprite'lar (bkz. UiSprites.PanelPixelsPerUnit)
            // 200 kullanıyor; oranı düşmeden pay iki katı sanılır ve bileşen
            // sığan bir kutuyu sığmıyor sanıp köşeleri gereksiz yere ezerdi.
            float perUnit = sprite.pixelsPerUnit > 0f ? 100f / sprite.pixelsPerUnit : 1f;
            float needX = (border.x + border.z) * perUnit;
            float needY = (border.y + border.w) * perUnit;

            // Çarpan payları BÖLER: 2 vermek payları yarıya indirir.
            float mulX = needX > 0f ? needX / (width * MaxBorderShare) : 1f;
            float mulY = needY > 0f ? needY / (height * MaxBorderShare) : 1f;

            // 1'in altına inmiyoruz — payları BÜYÜTMEK istemiyoruz, yalnız
            // sığmadıklarında küçültmek.
            float multiplier = Mathf.Max(1f, Mathf.Max(mulX, mulY));

            if (!Mathf.Approximately(_image.pixelsPerUnitMultiplier, multiplier))
                _image.pixelsPerUnitMultiplier = multiplier;
        }
    }
}
