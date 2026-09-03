using System.Collections.Generic;
using System.IO;
using GameKit.UI;
using UnityEditor;
using UnityEngine;

namespace GameKit.Editor.UiDesign
{
    /// <summary>
    /// Bir arayüz öğesinin KARŞILAŞTIRILABİLİR hâli.
    ///
    /// Yalnız <see cref="UiOverride"/>'ın taşıyabildiği alanları tutuyor:
    /// taşımadığı bir alanı ölçmek, kaydedilemeyecek bir farkı "değişti" diye
    /// göstermek olurdu.
    /// </summary>
    public struct UiNodeSnapshot
    {
        public bool isRect;
        public Vector2 anchorMin, anchorMax, offsetMin, offsetMax, pivot;
        public Vector3 scale;
        public bool hasGraphic; public Color color;
        public bool hasText;    public float fontSize;

        /// <summary>
        /// Punto OTOMATİK mi hesaplanıyor?
        ///
        /// DERS (hesaplanan değer, yazılan değer değildir): TMP otomatik
        /// küçültme açıkken `fontSize` alanı bir AYAR değil, o karede
        /// hesaplanmış SONUÇTUR — mesh her yenilendiğinde değişebilir. Bu
        /// bayrak olmadan araç, hiç dokunulmamış yazılarda "punto değişti"
        /// diye sahte farklar üretiyordu: tek bir kutuyu kaydırmak beş kayıt
        /// yazdırıyordu ve o kayıtlar hesaplanmış bir puntoyu DONDURUYORDU,
        /// yani metin uzayınca artık küçülemeyecekti.
        /// </summary>
        public bool autoSize;
        public bool active;
    }

    /// <summary>
    /// Kod ile ELLE YAPILAN arasındaki farkı ölçen ve <c>UiLayout.asset</c>'e
    /// yazan ortak kat.
    ///
    /// NEDEN AYRI DOSYA: Bu hesabı iki ayrı akış kullanıyor — sahne kurup
    /// düzenleyen "Paneller" sekmesi ve play modunda çalışan "Canlı" sekmesi.
    /// İkisi farkı ayrı ayrı hesaplasaydı, birinin kaydettiğini diğeri fark
    /// olarak görmezdi. Ölçüm tek yerde.
    ///
    /// DERS (fark, TAM YENİDEN HESAPLANIR): Eski akış farkı mevcut kaydın
    /// ÜSTÜNE ekliyordu; bu, bir ayarı geri almayı imkânsız kılıyordu — bir
    /// değeri kodun varsayılanına geri çektiğinde kayıt hâlâ eski override'ı
    /// taşıyordu. Burada kapsam içindeki her yol sıfırdan hesaplanıyor:
    /// varsayılana dönen öğenin kaydı SİLİNİYOR. Kapsam dışındaki yollara
    /// (o an açık olmayan panellerin ayarlarına) dokunulmuyor.
    /// </summary>
    public static class UiTweakDiff
    {
        public const string AssetDir  = "Assets/_Project/Resources";
        public const string AssetPath = AssetDir + "/UiLayout.asset";

        // ---------------------------------------------------------- varlık

        public static UiLayoutAsset Find() =>
            AssetDatabase.LoadAssetAtPath<UiLayoutAsset>(AssetPath);

        public static UiLayoutAsset LoadOrCreate()
        {
            var asset = Find();
            if (asset != null) return asset;

            Directory.CreateDirectory(AssetDir);
            asset = ScriptableObject.CreateInstance<UiLayoutAsset>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        // ---------------------------------------------------------- okuma

        public static UiNodeSnapshot Read(Transform node)
        {
            var snap = new UiNodeSnapshot
            {
                scale = node.localScale,
                active = node.gameObject.activeSelf
            };

            if (node is RectTransform rect)
            {
                snap.isRect = true;
                snap.anchorMin = rect.anchorMin; snap.anchorMax = rect.anchorMax;
                snap.offsetMin = rect.offsetMin; snap.offsetMax = rect.offsetMax;
                snap.pivot = rect.pivot;
            }

            var graphic = node.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null) { snap.hasGraphic = true; snap.color = graphic.color; }

            var text = node.GetComponent<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                snap.hasText = true;
                snap.fontSize = text.fontSize;
                snap.autoSize = text.enableAutoSizing;
            }

            return snap;
        }

        /// <summary>Kökten aşağı bütün ağacı yola göre toplar.</summary>
        public static void Collect(Transform node, string path,
                                   Dictionary<string, UiNodeSnapshot> into)
        {
            into[path] = Read(node);
            for (int i = 0; i < node.childCount; i++)
            {
                var child = node.GetChild(i);
                Collect(child, path + "/" + child.name, into);
            }
        }

        // ---------------------------------------------------------- fark

        /// <summary>
        /// İki hâl arasındaki farkı bir override'a çevirir.
        /// Hiçbir alan değişmediyse <c>false</c> döner — o yolun kaydı SİLİNİR.
        /// </summary>
        public static bool TryMakeOverride(string path,
            in UiNodeSnapshot was, in UiNodeSnapshot now, out UiOverride over)
        {
            over = new UiOverride { path = path };
            bool touched = false;

            if (was.isRect && now.isRect &&
                (now.anchorMin != was.anchorMin || now.anchorMax != was.anchorMax ||
                 now.offsetMin != was.offsetMin || now.offsetMax != was.offsetMax ||
                 now.pivot != was.pivot))
            {
                over.hasRect = true;
                over.anchorMin = now.anchorMin; over.anchorMax = now.anchorMax;
                over.offsetMin = now.offsetMin; over.offsetMax = now.offsetMax;
                over.pivot = now.pivot;
                touched = true;
            }

            if (now.scale != was.scale)
            {
                over.hasScale = true; over.scale = now.scale; touched = true;
            }

            if (now.hasGraphic && was.hasGraphic && now.color != was.color)
            {
                over.hasColor = true; over.color = now.color; touched = true;
            }

            // Otomatik puntolu yazı ATLANIYOR: aradaki fark elle yapılmış bir
            // ayar değil, motorun o karedeki hesabı. Elle punto verildiğinde
            // otomatik küçültme zaten kapanıyor (denetçi kapatıyor), yani
            // gerçek ayarlar bu kapıdan geçmeye devam ediyor.
            if (now.hasText && was.hasText && !now.autoSize &&
                !Mathf.Approximately(now.fontSize, was.fontSize))
            {
                over.hasFont = true; over.fontSize = now.fontSize; touched = true;
            }

            if (now.active != was.active)
            {
                over.hasActive = true; over.active = now.active; touched = true;
            }

            return touched;
        }

        /// <summary>Bu öğede kayda değer bir fark var mı?</summary>
        public static bool Differs(in UiNodeSnapshot was, in UiNodeSnapshot now) =>
            TryMakeOverride(string.Empty, was, now, out _);

        // ---------------------------------------------------------- yazma

        /// <summary>
        /// Kapsam içindeki yolları yeniden hesaplayıp varlığa yazar.
        /// Kapsam = <paramref name="baseline"/>'ın anahtarları; yani o an
        /// kurulmuş olan panelin ağacı. Geriye yazılan/silinen kayıt sayısı döner.
        /// </summary>
        public static int Save(IDictionary<string, UiNodeSnapshot> baseline,
                               IDictionary<string, UiNodeSnapshot> current)
        {
            var asset = LoadOrCreate();

            var merged = new Dictionary<string, UiOverride>();
            foreach (var old in asset.Entries)
                if (!string.IsNullOrEmpty(old.path)) merged[old.path] = old;

            int changed = 0;
            foreach (var pair in baseline)
            {
                // Kurulumdan sonra doğan/ölen nesne: elimizde iki ucu da olmayan
                // bir fark var demektir, dokunmuyoruz.
                if (!current.TryGetValue(pair.Key, out var now)) continue;

                bool had = merged.ContainsKey(pair.Key);
                if (TryMakeOverride(pair.Key, pair.Value, now, out var over))
                {
                    if (!had || !Same(merged[pair.Key], over)) changed++;
                    merged[pair.Key] = over;
                }
                else if (had)
                {
                    merged.Remove(pair.Key);
                    changed++;
                }
            }

            var list = new List<UiOverride>(merged.Count);
            foreach (var pair in merged) list.Add(pair.Value);
            list.Sort((a, b) => string.CompareOrdinal(a.path, b.path));

            asset.SetEntries(list.ToArray());
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            UiTweak.Invalidate();
            return changed;
        }

        /// <summary>Verilen yolların kayıtlarını siler (öğeyi/paneli sıfırla).</summary>
        public static int Clear(IEnumerable<string> paths)
        {
            var asset = Find();
            if (asset == null) return 0;

            var keep = new List<UiOverride>();
            var drop = new HashSet<string>(paths);
            int removed = 0;

            foreach (var entry in asset.Entries)
            {
                if (drop.Contains(entry.path)) { removed++; continue; }
                keep.Add(entry);
            }

            if (removed == 0) return 0;

            asset.SetEntries(keep.ToArray());
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            UiTweak.Invalidate();
            return removed;
        }

        static bool Same(in UiOverride a, in UiOverride b) =>
            a.hasRect == b.hasRect && a.anchorMin == b.anchorMin && a.anchorMax == b.anchorMax &&
            a.offsetMin == b.offsetMin && a.offsetMax == b.offsetMax && a.pivot == b.pivot &&
            a.hasScale == b.hasScale && a.scale == b.scale &&
            a.hasColor == b.hasColor && a.color == b.color &&
            a.hasFont == b.hasFont && Mathf.Approximately(a.fontSize, b.fontSize) &&
            a.hasActive == b.hasActive && a.active == b.active;
    }
}
