using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameKit.UI
{
    /// <summary>
    /// İç içe geçmiş katmanları, HER YÖNDE AYNI PİKSEL kadar içeri çeker;
    /// o piksel değeri de kutunun KENDİ YÜKSEKLİĞİNİN oranıdır.
    ///
    /// Kabartmalı düğmenin dört katmanı (dış çizgi, kabuk, kaymak, yüz) böyle
    /// diziliyor.
    ///
    /// DERS (bir halkanın kalınlığı çapraya bağlı olamaz): Duvarı çıpalarla
    /// vermek ilk akla gelen çözüm — üstten %16, soldan %6 gibi. Ama o oranlar
    /// düğmenin ENİNE ve BOYUNA ayrı ayrı vurulduğu için duvar dört kenarda
    /// dört farklı kalınlıkta çıkıyor. Referansta ölçülen değer tek bir sayı
    /// (280×113 düğmede dört kenarda da ~18 piksel), yani tasarımcı bir ORAN
    /// değil bir KALINLIK kullanmış. Kalınlığın kaynağı yükseklik: düğme
    /// uzadıkça değil, KALINLAŞTIKÇA duvarı kalınlaşıyor (18/113 = %16).
    ///
    /// DERS (bileşen KENDİ ölçüsünü okumalı, ebeveynininkini değil): Bunun
    /// ilk hâli her katmanın ÜSTÜNDE duruyor ve ebeveyninin yüksekliğini
    /// okuyordu. Çalışmadı ve sebebi öğretici: düğme kurulurken kök kutu
    /// henüz yerleştirilmemiş oluyor, kanvas boyunda (1920) görünüyor, pay
    /// 0,027 × 1920 = 52 piksel çıkıyordu. Kök sonradan 206 piksele inince
    /// hesap bir daha yapılmıyordu — ekranda kocaman siyah bir hap kalıyordu
    /// (ölçüm: kök 511×206 iken kabuk 408×102, yüz ise −102×−408).
    ///
    /// <c>ILayoutSelfController</c> ile Unity'nin yerleşim döngüsüne girmeyi
    /// denemek de işe yaramadı; sebebi <c>LayoutRebuilder</c>'ın kaynağında
    /// yazıyor: bir kutuda hiç denetleyici yoksa BÜTÜN ALT AĞAÇ atlanıyor.
    /// Düğmenin kökünde denetleyici yoktu, dolayısıyla katmanlara hiç
    /// uğranmıyordu.
    ///
    /// Çözüm hesabı KÖKE taşımak: kök kendi ölçüsünü okuyor ve
    /// <c>OnRectTransformDimensionsChange</c> ateşlendiğinde o ölçü zaten
    /// güncel. <see cref="UiCornerFit"/> yıllardır aynı mekanizmayla
    /// çalışıyor — yeni bir yol icat etmek yerine çalıştığı kanıtlanmış olanı
    /// kullanmak, bu turda kaybedilen zamanın asıl dersi.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("GameKit/UI/Halka Yerleşimi")]
    public sealed class UiRingLayout : MonoBehaviour
    {
        [System.Serializable]
        struct Ring
        {
            public RectTransform rect;
            public float share;
        }

        [SerializeField] List<Ring> rings = new List<Ring>();

        /// <summary>
        /// Yazısı da kutunun boyuna bağlı: ÖLÇÜM (referans Resume düğmesi)
        /// harf yüksekliği düğme yüksekliğinin %28,3'ü, Baloo 2'de büyük harf
        /// punto başına 0,595 geldiğine göre punto = yükseklik × 0,476.
        ///
        /// Puntoyu çağrı yerinin sabit sayısına bırakmak, aynı düğmenin
        /// ekranın iki yerinde iki farklı ORANDA görünmesi demekti; ölçüm de
        /// bunu gösterdi (bizimki %24,3, referans %28,3).
        /// </summary>
        [SerializeField] TMP_Text label;
        [SerializeField] float labelShare = 0.476f;
        [SerializeField] float labelFloor;

        RectTransform _rect;

        /// <summary>Bir katmanı kaydeder; pay kutunun yüksekliğinin oranıdır.</summary>
        public void Add(RectTransform ring, float share)
        {
            if (ring == null) return;
            rings.Add(new Ring { rect = ring, share = Mathf.Clamp(share, 0f, 0.5f) });
            Apply();
        }

        /// <summary>Yazıyı kutunun boyuna bağlar; <paramref name="floor"/> alt sınır.</summary>
        public void Bind(TMP_Text text, float floor)
        {
            label = text;
            labelFloor = floor;
            Apply();
        }

        void OnEnable() => Apply();

        void OnRectTransformDimensionsChange() => Apply();

        void Apply()
        {
            if (_rect == null) _rect = transform as RectTransform;
            if (_rect == null) return;

            float height = _rect.rect.height;
            if (height <= 1f) return;

            foreach (var ring in rings)
            {
                if (ring.rect == null) continue;

                // Çıpalar dört köşeye gerilir; pay artık saf piksel.
                ring.rect.anchorMin = Vector2.zero;
                ring.rect.anchorMax = Vector2.one;

                float pad = height * ring.share;

                // Pay kutunun yarısını geçerse katman ters dönüp yok olur.
                // Aşırı basık bir düğmede bile en azından bir çizgi kalsın.
                pad = Mathf.Min(pad, height * 0.5f - 1f);

                ring.rect.offsetMin = new Vector2(pad, pad);
                ring.rect.offsetMax = new Vector2(-pad, -pad);
            }

            if (label == null) return;

            // Uzun yazı düğmeye sığmayabilir; TMP kendiliğinden küçültsün ama
            // çağrı yerinin beklediği tabanın altına inmesin.
            float size = Mathf.Max(labelFloor, height * labelShare);
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Min(labelFloor, size);
        }
    }
}
