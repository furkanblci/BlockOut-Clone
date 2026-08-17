using UnityEngine;

namespace GameKit.UI
{
    /// <summary>
    /// Kodla kurulan arayüzün üstüne ELLE yapılmış düzeltmeleri uygular.
    ///
    /// NEDEN BÖYLE: Bu projenin arayüzü bilerek prefab'sız — ölçülerin neden
    /// öyle olduğu (hangi referans karesinden, hangi piksel oranından geldiği)
    /// kodun yanındaki yorumda duruyor ve prefab YAML'ı birleştirmede
    /// çakışıyor (bkz. <see cref="UiKit"/> başındaki ders). Ama görsel
    /// düzenleme yapamamak da gerçek bir bedel.
    ///
    /// Çözüm ikisini de koruyor: kod hâlâ tek kaynak, elle yapılan değişiklik
    /// onun ÜSTÜNE binen bir fark listesi. Kod bir ölçüyü değiştirdiğinde
    /// elle dokunulmamış her şey kendiliğinden yeni değeri alır.
    ///
    /// OPTİMİZASYON: Bu kat çalışma anında HİÇBİR karede iş yapmaz. Yalnız
    /// ekran KURULURKEN, ekran başına bir kez hiyerarşi geziliyor. Varlık yoksa
    /// ya da boşsa <see cref="Apply"/> ilk satırda dönüyor — yani yayın
    /// sürümünde maliyeti tek bir null kontrolü.
    /// </summary>
    public static class UiTweak
    {
        public const string ResourcePath = "UiLayout";

        static UiLayoutAsset _asset;
        static bool _searched;

        public static UiLayoutAsset Asset
        {
            get
            {
                if (!_searched)
                {
                    _searched = true;
                    _asset = Resources.Load<UiLayoutAsset>(ResourcePath);
                }
                return _asset;
            }
        }

        /// <summary>Varlık yenilendiğinde (editörde kaydedince) çağrılır.</summary>
        public static void Invalidate()
        {
            _searched = false;
            _asset = null;
        }

        /// <summary>Uygulanacak bir şey var mı? Yoksa hiçbir iş yapılmaz.</summary>
        public static bool HasOverrides =>
            Asset != null && Asset.Entries != null && Asset.Entries.Length > 0;

        /// <summary>
        /// Bir ekranı "elle düzenlenebilir" diye işaretler ve varsa kayıtlı
        /// düzeltmelerini hemen uygular. Ekran kurulumunun EN SONUNDA çağrılır.
        ///
        /// İşaret çalışma anında da duruyor (tek bir string alan): editör
        /// penceresi ekranları böyle buluyor ve KOD İLE EDİTÖR AYNI anahtarı
        /// kullandığı için kaydedilen yol aranan yolu tutuyor. İki tarafın
        /// anahtarı ayrı üretmesi, bu tür sistemlerin sessizce bozulduğu
        /// klasik yerdir.
        /// </summary>
        /// <remarks>
        /// YALNIZ İŞARETLER, uygulamaz. Kurulumun başında çağrıldığı için o an
        /// çocuklar henüz yok; uygulama <see cref="ApplyAll"/> ile, her şey
        /// kurulduktan sonra tek seferde yapılıyor.
        /// </remarks>
        public static void Mark(Transform screenRoot, string screenKey)
        {
            if (screenRoot == null || string.IsNullOrEmpty(screenKey)) return;

            var marker = screenRoot.GetComponent<UiTweakRoot>();
            if (marker == null) marker = screenRoot.gameObject.AddComponent<UiTweakRoot>();
            marker.Key = screenKey;
        }

        /// <summary>
        /// Bir ekranın tamamına elle yapılmış düzeltmeleri uygular.
        /// </summary>
        public static void Apply(Transform screenRoot, string screenKey)
        {
            if (screenRoot == null || !HasOverrides) return;

            var builder = new System.Text.StringBuilder(64);
            Walk(screenRoot, screenKey, builder);
        }

        /// <summary>Sahnedeki bütün işaretli ekranlara yeniden uygular.</summary>
        public static void ApplyAll()
        {
            foreach (var root in Object.FindObjectsByType<UiTweakRoot>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                Apply(root.transform, root.Key);
        }

        static void Walk(Transform node, string path, System.Text.StringBuilder builder)
        {
            if (Asset.TryGet(path, out var over)) ApplyTo(node, over);

            int count = node.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = node.GetChild(i);
                builder.Clear();
                builder.Append(path).Append('/').Append(child.name);
                Walk(child, builder.ToString(), builder);
            }
        }

        static void ApplyTo(Transform node, UiOverride over)
        {
            if (over.hasRect && node is RectTransform rect)
            {
                rect.anchorMin = over.anchorMin;
                rect.anchorMax = over.anchorMax;
                rect.offsetMin = over.offsetMin;
                rect.offsetMax = over.offsetMax;
                rect.pivot = over.pivot;
            }

            if (over.hasScale) node.localScale = over.scale;

            if (over.hasColor)
            {
                var graphic = node.GetComponent<UnityEngine.UI.Graphic>();
                if (graphic != null) graphic.color = over.color;
            }

            if (over.hasFont)
            {
                var text = node.GetComponent<TMPro.TextMeshProUGUI>();
                if (text != null)
                {
                    text.enableAutoSizing = false;
                    text.fontSize = over.fontSize;
                }
            }

            if (over.hasActive && node.gameObject.activeSelf != over.active)
                node.gameObject.SetActive(over.active);
        }

    }
}
