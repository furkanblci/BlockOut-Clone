using System.Collections.Generic;
using GameKit.UI;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.UiDesign
{
    /// <summary>
    /// ARAYÜZ TASARIM PENCERESİ — kodla kurulan ekranları ELLE düzenlemek için.
    ///
    /// AKIŞ (üç adım):
    ///   1. Play moduna gir, düzenlemek istediğin ekranı aç.
    ///   2. "Referans al" — o andaki hâl temel alınır.
    ///   3. Hiyerarşide/Sahnede normal Unity araçlarıyla oynadıktan sonra
    ///      "Değişiklikleri kaydet" — YALNIZ farklar `Resources/UiLayout.asset`
    ///      dosyasına yazılır ve bundan sonra her açılışta uygulanır.
    ///
    /// NEDEN PREFAB DEĞİL: Bu projenin arayüzü bilerek kodla kuruluyor —
    /// ölçülerin hangi referans karesinden, hangi piksel oranından geldiği
    /// kodun yanındaki yorumda duruyor. Prefab'a çevirmek o gerekçelerin
    /// tamamını çöpe atardı ve prefab YAML'ı birleştirmede çakışır.
    /// Bu pencere ikisini birden veriyor: kod hâlâ tek kaynak, senin elle
    /// yaptığın şey onun üstüne binen bir FARK listesi. Kod bir ölçüyü
    /// değiştirdiğinde elle dokunmadığın her şey yeni değeri kendiliğinden
    /// alır — donmuş bir kopya kalmaz.
    ///
    /// OPTİMİZASYON: Çalışma anında hiçbir karede iş yok. Ekran kurulurken,
    /// ekran başına bir kez hiyerarşi geziliyor; varlık boşsa ilk satırda
    /// dönülüyor. Bu pencerenin kendisi editör kodu, derlemeye hiç girmiyor.
    /// </summary>
    public sealed class UiDesignWindow : EditorWindow
    {
        const string AssetDir  = "Assets/_Project/Resources";
        const string AssetPath = AssetDir + "/UiLayout.asset";

        /// <summary>Bir öğenin karşılaştırılabilir hâli.</summary>
        struct Snapshot
        {
            public Vector2 anchorMin, anchorMax, offsetMin, offsetMax, pivot;
            public Vector3 scale;
            public bool hasGraphic; public Color color;
            public bool hasText;    public float fontSize;
            public bool active;
        }

        readonly Dictionary<string, Snapshot> _baseline = new Dictionary<string, Snapshot>();
        Vector2 _scroll;
        string _status = "Play moduna girip \"Referans al\" ile başla.";

        [MenuItem("Tools/Block Out/Arayüz Tasarımı %#u")]
        static void Open() => GetWindow<UiDesignWindow>("Arayüz Tasarımı").minSize =
            new Vector2(380f, 320f);

        void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "1) Play moduna gir ve düzenleyeceğin ekranı aç\n" +
                "2) Referans al\n" +
                "3) Hiyerarşide öğeleri sürükle/renklerini değiştir\n" +
                "4) Değişiklikleri kaydet",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("1 · Referans al", GUILayout.Height(28f))) TakeBaseline();

                using (new EditorGUI.DisabledScope(_baseline.Count == 0))
                    if (GUILayout.Button("2 · Değişiklikleri kaydet", GUILayout.Height(34f)))
                        SaveChanges();
            }

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Ekranlar yalnız play modunda kurulu olduğu için " +
                                        "düzenleme de orada yapılır.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_status, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            DrawSaved();
        }

        // ---- Referans -------------------------------------------------------

        void TakeBaseline()
        {
            _baseline.Clear();
            int screens = 0;

            foreach (var root in FindRoots())
            {
                screens++;
                Collect(root.transform, root.Key, _baseline);
            }

            _status = screens == 0
                ? "İşaretli ekran bulunamadı. (Ekran kurulumu UiTweak.Mark çağırıyor mu?)"
                : $"Referans alındı: {screens} ekran, {_baseline.Count} öğe. Şimdi düzenle.";
            Repaint();
        }

        static IEnumerable<UiTweakRoot> FindRoots()
        {
            foreach (var root in Object.FindObjectsByType<UiTweakRoot>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(root.Key)) yield return root;
        }

        static void Collect(Transform node, string path, Dictionary<string, Snapshot> into)
        {
            into[path] = Read(node);
            for (int i = 0; i < node.childCount; i++)
            {
                var child = node.GetChild(i);
                Collect(child, path + "/" + child.name, into);
            }
        }

        static Snapshot Read(Transform node)
        {
            var snap = new Snapshot { scale = node.localScale, active = node.gameObject.activeSelf };

            if (node is RectTransform rect)
            {
                snap.anchorMin = rect.anchorMin; snap.anchorMax = rect.anchorMax;
                snap.offsetMin = rect.offsetMin; snap.offsetMax = rect.offsetMax;
                snap.pivot = rect.pivot;
            }

            var graphic = node.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null) { snap.hasGraphic = true; snap.color = graphic.color; }

            var text = node.GetComponent<TMPro.TextMeshProUGUI>();
            if (text != null) { snap.hasText = true; snap.fontSize = text.fontSize; }

            return snap;
        }

        // ---- Kaydetme -------------------------------------------------------

        void SaveChanges()
        {
            var current = new Dictionary<string, Snapshot>();
            foreach (var root in FindRoots()) Collect(root.transform, root.Key, current);

            // Eskiler KORUNUR: bu turda açık olmayan ekranların düzeltmeleri
            // silinmemeli. Yeni farklar onların üstüne yazılır.
            var merged = new Dictionary<string, UiOverride>();
            var asset = LoadOrCreate();
            foreach (var old in asset.Entries)
                if (!string.IsNullOrEmpty(old.path)) merged[old.path] = old;

            int changed = 0;
            foreach (var pair in current)
            {
                if (!_baseline.TryGetValue(pair.Key, out var was)) continue;   // yeni nesne
                var now = pair.Value;

                var over = merged.TryGetValue(pair.Key, out var existing)
                    ? existing
                    : new UiOverride { path = pair.Key };
                bool touched = false;

                if (now.anchorMin != was.anchorMin || now.anchorMax != was.anchorMax ||
                    now.offsetMin != was.offsetMin || now.offsetMax != was.offsetMax ||
                    now.pivot != was.pivot)
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

                if (now.hasText && was.hasText &&
                    !Mathf.Approximately(now.fontSize, was.fontSize))
                {
                    over.hasFont = true; over.fontSize = now.fontSize; touched = true;
                }

                if (now.active != was.active)
                {
                    over.hasActive = true; over.active = now.active; touched = true;
                }

                if (!touched) continue;
                merged[pair.Key] = over;
                changed++;
            }

            if (changed == 0)
            {
                _status = "Fark bulunamadı — referans alındıktan sonra bir şey değişmemiş.";
                Repaint();
                return;
            }

            var list = new List<UiOverride>(merged.Values);
            list.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
            asset.SetEntries(list.ToArray());

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            UiTweak.Invalidate();

            _status = $"{changed} öğe kaydedildi (toplam {list.Count}). " +
                      "Bundan sonra her açılışta uygulanacak.";
            TakeBaseline();     // yeni hâl artık referans
        }

        static UiLayoutAsset LoadOrCreate()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UiLayoutAsset>(AssetPath);
            if (asset != null) return asset;

            if (!AssetDatabase.IsValidFolder(AssetDir))
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");

            asset = CreateInstance<UiLayoutAsset>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        // ---- Kayıtlı düzeltmeler --------------------------------------------

        void DrawSaved()
        {
            var asset = AssetDatabase.LoadAssetAtPath<UiLayoutAsset>(AssetPath);
            if (asset == null || asset.Entries.Length == 0)
            {
                EditorGUILayout.LabelField("Kayıtlı düzeltme yok.", EditorStyles.miniLabel);
                return;
            }

            // Ekran başına sayı: hangi ekranda ne kadar elle iş var, bir bakışta.
            var perScreen = new Dictionary<string, int>();
            foreach (var e in asset.Entries)
            {
                int slash = e.path.IndexOf('/');
                string screen = slash < 0 ? e.path : e.path.Substring(0, slash);
                perScreen.TryGetValue(screen, out int n);
                perScreen[screen] = n + 1;
            }

            EditorGUILayout.LabelField($"Kayıtlı düzeltmeler ({asset.Entries.Length})",
                EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var pair in perScreen)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(pair.Key, GUILayout.Width(200f));
                    EditorGUILayout.LabelField(pair.Value + " öğe", GUILayout.Width(70f));
                    if (GUILayout.Button("Sıfırla", GUILayout.Width(70f)))
                    {
                        ClearScreen(asset, pair.Key);
                        break;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Varlığı seç"))
                Selection.activeObject = asset;
        }

        void ClearScreen(UiLayoutAsset asset, string screen)
        {
            var kept = new List<UiOverride>();
            foreach (var e in asset.Entries)
                if (!e.path.StartsWith(screen + "/") && e.path != screen) kept.Add(e);

            asset.SetEntries(kept.ToArray());
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            UiTweak.Invalidate();

            _status = $"\"{screen}\" düzeltmeleri silindi. Kodun ürettiği hâl geri geldi " +
                      "(play modunu yeniden başlat).";
            Repaint();
        }
    }
}
