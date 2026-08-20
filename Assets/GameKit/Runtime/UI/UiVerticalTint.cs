using UnityEngine;
using UnityEngine.UI;

namespace GameKit.UI
{
    /// <summary>
    /// Bir arayüz grafiğini YUKARIDAN AŞAĞIYA renk geçişiyle boyar.
    ///
    /// DERS (degrade AYRI BİR KATMAN DEĞİLDİR): Bu projede kabartma hissi
    /// bugüne kadar üst üste konan yamalarla veriliyordu — yüzün üstüne açık
    /// bir dikdörtgen, altına koyu bir dikdörtgen. İkisi de DİKDÖRTGEN olduğu
    /// için yuvarlak köşeyi takip etmiyordu: düğmenin üst köşelerinde açık
    /// bandın dik kenarı, alt köşelerinde koyu bandın çıkıntısı görünüyordu.
    /// Kullanıcının "piksel bozukluğu, fazla piksel olan yerler" dediği şeyin
    /// bir bölümü buydu — bozuk piksel değil, YANLIŞ ŞEKİLLİ katman.
    ///
    /// Bu bileşen degradeyi ayrı bir yüzey olarak değil, grafiğin KENDİ
    /// köşe noktalarının rengi olarak yazıyor. Silüet neyse degrade de o:
    /// yuvarlak köşede yuvarlak, kesik köşede kesik. Ek çizim çağrısı da yok.
    ///
    /// DERS (9-dilim ile neden çalışıyor?): Dokuz dilimli bir görüntünün ağı
    /// dört yatay köşe sırasından oluşur. Degradeyi köşe noktasının GERÇEK
    /// y'sine göre hesaplamak, bu dört sıranın nerede olduğundan bağımsız
    /// olarak her yerde doğrusal bir geçiş verir; bant oluşmaz.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    [AddComponentMenu("GameKit/UI/Dikey Renk Geçişi")]
    public sealed class UiVerticalTint : BaseMeshEffect
    {
        [SerializeField] Color top = Color.white;
        [SerializeField] Color bottom = Color.white;

        /// <summary>
        /// Üstteki ilk <c>capShare</c> oranlık bant için AYRI bir renk
        /// (referansın yüzeyindeki parlak kaymak çizgisi). Sıfır ise yok.
        /// </summary>
        [SerializeField, Range(0f, 0.5f)] float capShare;
        [SerializeField] Color cap = Color.white;

        public Color Top { get => top; set { top = value; Refresh(); } }
        public Color Bottom { get => bottom; set { bottom = value; Refresh(); } }

        /// <summary>Üst bandı ayarlar; <paramref name="share"/> 0 ise kapatır.</summary>
        public void SetCap(Color color, float share)
        {
            cap = color;
            capShare = Mathf.Clamp(share, 0f, 0.5f);
            Refresh();
        }

        public void Set(Color topColor, Color bottomColor)
        {
            top = topColor;
            bottom = bottomColor;
            Refresh();
        }

        void Refresh()
        {
            if (graphic != null) graphic.SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Refresh();
        }

        /// <summary>
        /// Kutu ölçüsü değişince ağ yeniden kurulur ama Unity bunu bize
        /// haber vermez; degrade oranlı olduğu için yine de doğru kalır —
        /// yine de köşe noktaları yeniden yazılsın diye tetikliyoruz.
        /// </summary>
        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            Refresh();
        }

        public override void ModifyMesh(VertexHelper helper)
        {
            if (!IsActive() || helper.currentVertCount == 0) return;

            var rect = ((RectTransform)transform).rect;
            float height = rect.height;
            if (height <= 0.001f) return;

            var vertex = new UIVertex();
            for (int i = 0; i < helper.currentVertCount; i++)
            {
                helper.PopulateUIVertex(ref vertex, i);

                // Kutunun ALTI 0, ÜSTÜ 1.
                float t = Mathf.Clamp01((vertex.position.y - rect.yMin) / height);
                var tint = Mathf.Approximately(capShare, 0f) || t <= 1f - capShare
                    ? Color.Lerp(bottom, top, Mathf.Approximately(capShare, 0f)
                        ? t
                        : t / Mathf.Max(0.0001f, 1f - capShare))
                    : Color.Lerp(top, cap, (t - (1f - capShare)) / Mathf.Max(0.0001f, capShare));

                // ÇARPIYORUZ, ATAMIYORUZ: grafiğin kendi rengi (ve düğmenin
                // basılı/sönük geçişleri) böylece yaşamaya devam ediyor.
                var source = vertex.color;
                vertex.color = new Color32(
                    (byte)(source.r * tint.r),
                    (byte)(source.g * tint.g),
                    (byte)(source.b * tint.b),
                    (byte)(source.a * tint.a));

                helper.SetUIVertex(vertex, i);
            }
        }
    }
}
