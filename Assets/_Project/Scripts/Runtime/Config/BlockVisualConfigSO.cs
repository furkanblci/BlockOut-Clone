using UnityEngine;

namespace BlockOut.Runtime.Config
{
    /// <summary>
    /// Oyunun TÜM görsel ayarları tek asset'te.
    ///
    /// DERS (sabitleri veriye çevirmek): Görünüm ayarları koda gömülüyken her
    /// deneme için kod değiştirip derleme beklemek gerekir — bu, sanat yönü
    /// bulmanın en yavaş yoludur. Ayarlar bir ScriptableObject'e taşınınca
    /// tasarımcı (ya da sen) kaydırıcıları oynatıp sonucu ANINDA görür.
    /// Stüdyolarda "tuning pass" denen aşamanın tamamı böyle çalışır.
    ///
    /// Değerleri <c>Tools > Block Out > Görünüm Ayarları</c> penceresinden
    /// canlı olarak denemek en pratiği.
    /// </summary>
    [CreateAssetMenu(menuName = "Block Out/Block Visual Config", fileName = "BlockVisualConfig")]
    public sealed class BlockVisualConfigSO : ScriptableObject
    {
        [Header("Tuğla gövdesi")]
        [Tooltip("Tuğlanın yüksekliği (hücre biriminde).")]
        [Range(0.15f, 0.8f)] public float brickHeight = 0.40f;

        [Tooltip("Komşu bloklarla arasındaki görsel boşluk.")]
        [Range(0f, 0.15f)] public float brickInset = 0.055f;

        [Tooltip("Üst kenar pahı — büyüdükçe tuğla yuvarlak hisseder.")]
        [Range(0f, 0.2f)] public float brickChamfer = 0.06f;

        [Header("Saplamalar")]
        [Tooltip("Hücre başına saplama sayısı (referans oyunda 2 = 2×2).")]
        [Range(1, 3)] public int studsPerCell = 2;

        [Range(0.05f, 0.24f)] public float studRadius = 0.168f;
        [Range(0.02f, 0.25f)] public float studHeight = 0.115f;

        [Tooltip("Saplamanın üst kenar pahı — ışığı yakalayan parlak halka.")]
        [Range(0f, 0.08f)] public float studBevel = 0.035f;

        [Range(6, 20)] public int studSegments = 14;

        [Header("Sahte gölgelendirme (vertex AO)")]
        [Tooltip("Tuğlanın alt kenarı — koyu olması bloğu zeminden ayırır.")]
        [Range(0f, 1f)] public float toneBodyBottom = 0.48f;

        [Tooltip("Yan yüzlerin üst kısmı.")]
        [Range(0f, 1f)] public float toneBodySide = 0.86f;

        [Tooltip("Tuğlanın üst yüzeyi. Saplama tepesinden KOYU olmalı.")]
        [Range(0f, 1f)] public float toneFaceTop = 0.72f;

        [Tooltip("Saplama dibi — gölge halkası. En koyu ton.")]
        [Range(0f, 1f)] public float toneStudFoot = 0.42f;

        [Tooltip("Saplama tepesi. En parlak ton.")]
        [Range(0f, 1f)] public float toneStudTop = 1f;

        [Header("Işık ve malzeme")]
        [Tooltip("Işığın geldiği yön. Y bileşenini düşürmek derinliği artırır.")]
        public Vector3 lightDirection = new Vector3(0.45f, 0.8f, -0.4f);

        [Range(0f, 1f)] public float ambient = 0.5f;
        [Range(0f, 2f)] public float specular = 0.5f;
        [Range(4f, 128f)] public float gloss = 64f;
        [Range(0f, 1f)] public float rim = 0.12f;
        [Range(1f, 2f)] public float saturation = 1.18f;

        [Header("Temas gölgesi")]
        [Tooltip("Blokların altına yumuşak gölge — 'havada duruyor' hissini kırar.")]
        public bool contactShadow = true;

        [Range(0f, 1f)] public float shadowOpacity = 0.42f;
        [Range(0.6f, 1.6f)] public float shadowScale = 1.02f;
        public Vector2 shadowOffset = new Vector2(0.06f, -0.06f);

        [Header("Tahta")]
        public Color floorColorA = new Color(0.17f, 0.15f, 0.31f);
        public Color floorColorB = new Color(0.14f, 0.12f, 0.27f);
        public Color wallColor = new Color(0.36f, 0.32f, 0.62f);

        [Tooltip("Tahtanın dış çerçevesi.")]
        public Color frameColor = new Color(0.30f, 0.26f, 0.58f);

        [Range(0f, 1.2f)] public float frameThickness = 0.55f;
        [Range(0.05f, 0.8f)] public float frameHeight = 0.34f;
        [Range(0f, 0.5f)] public float wallHeight = 0.42f;
        [Range(0.05f, 0.5f)] public float wallThickness = 0.18f;

        [Header("Kapılar")]
        [Range(0.1f, 0.8f)] public float gateBarHeight = 0.40f;
        [Range(0.1f, 0.8f)] public float gateBarDepth = 0.44f;
        [Range(0f, 0.4f)] public float gateOutwardOffset = 0.16f;
        [Range(0.05f, 0.4f)] public float arrowSize = 0.20f;
        [Range(0f, 0.2f)] public float arrowRise = 0.055f;

        [Tooltip("Ok köşelerinin yuvarlaklığı — referans oyunda köşeler yumuşak.")]
        [Range(0f, 0.5f)] public float arrowCornerRadius = 0.3f;

        /// <summary>Ayar değiştiğinde görselleri tazelemek isteyenler buna abone olur.</summary>
        public event System.Action Changed;

        public void RaiseChanged() => Changed?.Invoke();

        void OnValidate() => RaiseChanged();
    }
}
