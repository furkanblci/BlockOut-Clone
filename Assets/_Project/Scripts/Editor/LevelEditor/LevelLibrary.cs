using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Editor.ProjectSetup;
using BlockOut.Runtime.Config;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Bölüm setinin tek kaynağı: sıralı liste, ölçümler, doğrulama durumu ve
    /// minyatür önbelleği. Galeri, doğrulama sekmesi ve bölüm tarayıcısı üçü de
    /// buradan okur.
    ///
    /// DERS (araçta önbellek şart): 50 bölümü doğrulamak çözücüyü 50 kez
    /// koşturmak demek — saniyeler sürer. Bunu her OnGUI karesinde yapmak aracı
    /// kilitler. Bu yüzden sonuç bir kez hesaplanır, bellekte tutulur ve
    /// yalnızca dosya değişince ya da elle temizlenince atılır. Referans
    /// editördeki "Clear Cache" düğmesi tam olarak bunun içindir.
    ///
    /// DERS (statik durum ve domain reload): Bu önbellek bilinçli olarak
    /// statiktir; pencere kapanıp açılsa da hayatta kalır. Play'e girmek
    /// (domain reload) onu siler — sorun değil, önbellek türetilmiş veridir,
    /// kaybı yalnız bir yeniden hesaplamaya mal olur.
    /// </summary>
    public static class LevelLibrary
    {
        public enum Status
        {
            /// <summary>Henüz doğrulanmadı.</summary>
            Unknown,
            /// <summary>Çözülebilir, uyarı yok.</summary>
            Ok,
            /// <summary>Çözülebilir ama tasarım uyarısı var.</summary>
            Warning,
            /// <summary>Şema hatası ya da çözülemiyor.</summary>
            Broken
        }

        /// <summary>Tek bir bölüm dosyasının araç tarafındaki görüntüsü.</summary>
        public sealed class Entry
        {
            public string Path;
            public string Name;

            /// <summary>Katalogdaki sıra (0 tabanlı); katalogda yoksa -1.</summary>
            public int CatalogIndex = -1;

            public LevelData Data;

            /// <summary>Bu bölümün kullandığı mekanikler — veriden hesaplanır.</summary>
            public Mechanic Mechanics;

            public Status Status = Status.Unknown;
            public string Summary = "doğrulanmadı";
            public int Errors, Warnings, Moves;

            /// <summary>Minyatür; ilk istendiğinde çizilir.</summary>
            public Texture2D Thumbnail;

            public int Width => Data?.Board?.Width ?? 0;
            public int Height => Data?.Board?.Height ?? 0;
            public int BlockCount => Data?.Blocks?.Count ?? 0;
            public int GateCount => Data?.Gates?.Count ?? 0;
            public string Difficulty => Data?.Difficulty ?? "?";

            /// <summary>Galeri çerçevesinin ve durum noktasının rengi.</summary>
            public Color StatusColor => ColorFor(Status);
        }

        static readonly List<Entry> _entries = new List<Entry>();
        static bool _listValid;

        /// <summary>Sıralı bölüm listesi; gerekiyorsa diskten yeniden okur.</summary>
        public static IReadOnlyList<Entry> Entries
        {
            get
            {
                if (!_listValid) Rebuild();
                return _entries;
            }
        }

        public static int Count => Entries.Count;

        public static Color ColorFor(Status status)
        {
            switch (status)
            {
                case Status.Ok:      return new Color(0.36f, 0.80f, 0.44f);
                case Status.Warning: return new Color(0.95f, 0.75f, 0.25f);
                case Status.Broken:  return new Color(0.92f, 0.35f, 0.32f);
                default:             return new Color(0.42f, 0.42f, 0.50f);
            }
        }

        // ---------------- liste ----------------

        /// <summary>
        /// Listeyi ve bütün türetilmiş verileri atar. Dosya eklendiğinde,
        /// silindiğinde ya da kaydedildiğinde çağrılır.
        /// </summary>
        public static void Invalidate()
        {
            foreach (var entry in _entries)
                if (entry.Thumbnail != null) Object.DestroyImmediate(entry.Thumbnail);

            _entries.Clear();
            _listValid = false;
        }

        /// <summary>Tek bir bölümün önbelleğini atar — o dosya kaydedildiğinde.</summary>
        public static void Invalidate(string path)
        {
            var entry = Find(path);
            if (entry == null) { Invalidate(); return; }

            if (entry.Thumbnail != null) Object.DestroyImmediate(entry.Thumbnail);
            entry.Thumbnail = null;
            entry.Data = null;
            entry.Status = Status.Unknown;
            entry.Summary = "doğrulanmadı";
            entry.Errors = entry.Warnings = entry.Moves = 0;
            Read(entry);
        }

        static void Rebuild()
        {
            _listValid = true;
            _entries.Clear();

            // Katalog sırası OYNANIŞ sırasıdır ve dosya adı sırasından farklı
            // olabilir. Araç oyuncunun gördüğü sırayı göstermeli, alfabetik
            // dizini değil — yoksa "3. bölüm zor" gibi bir geri bildirim
            // yanlış dosyaya götürür.
            var order = new Dictionary<string, int>();
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogSO>(CatalogPath);
            if (catalog != null && catalog.levels != null)
                for (int i = 0; i < catalog.levels.Length; i++)
                {
                    var asset = catalog.levels[i];
                    if (asset == null) continue;
                    order[AssetDatabase.GetAssetPath(asset)] = i;
                }

            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LevelEditorIO.LevelDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".json") || path.Contains("__playtest")) continue;

                var entry = new Entry
                {
                    Path = path,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path),
                    CatalogIndex = order.TryGetValue(path, out int index) ? index : -1
                };
                Read(entry);
                _entries.Add(entry);
            }

            // Katalogda olmayanlar (yeni yazılmış, henüz kurulum aracı
            // koşmamış) sonda ada göre sıralanır.
            _entries.Sort((a, b) =>
            {
                if (a.CatalogIndex >= 0 && b.CatalogIndex >= 0)
                    return a.CatalogIndex.CompareTo(b.CatalogIndex);
                if (a.CatalogIndex >= 0) return -1;
                if (b.CatalogIndex >= 0) return 1;
                return string.Compare(a.Name, b.Name, System.StringComparison.Ordinal);
            });
        }

        static void Read(Entry entry)
        {
            try
            {
                entry.Data = LevelEditorIO.FromJson(System.IO.File.ReadAllText(entry.Path));
                entry.Mechanics = LevelMechanics.Of(entry.Data);
            }
            catch (System.Exception error)
            {
                entry.Data = null;
                entry.Mechanics = Mechanic.None;
                entry.Status = Status.Broken;
                entry.Summary = "okunamadı: " + error.Message;
            }
        }

        public static Entry Find(string path)
        {
            foreach (var entry in Entries)
                if (entry.Path == path) return entry;
            return null;
        }

        public static int IndexOf(string path)
        {
            var list = Entries;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Path == path) return i;
            return -1;
        }

        /// <summary>Listede komşu bölümün yolu; uçlarda null.</summary>
        public static string Neighbour(string path, int step)
        {
            int index = IndexOf(path);
            if (index < 0) return Count > 0 ? Entries[0].Path : null;

            int target = index + step;
            return target >= 0 && target < Count ? Entries[target].Path : null;
        }

        // ---------------- minyatür ----------------

        public static Texture2D Thumbnail(Entry entry, ColorPaletteSO palette)
        {
            if (entry.Thumbnail != null) return entry.Thumbnail;
            if (entry.Data == null) return null;

            entry.Thumbnail = LevelThumbnail.Render(entry.Data, palette);
            return entry.Thumbnail;
        }

        // ---------------- doğrulama ----------------

        /// <summary>Tek bölümü doğrular ve sonucu girdiye yazar.</summary>
        public static void Validate(Entry entry, ColorPaletteSO palette, GameConfigSO config)
        {
            if (entry.Data == null) return;

            var report = LevelValidationTool.ValidateData(entry.Data, palette, config);
            Apply(entry, report);
        }

        /// <summary>Editörde açık olan çalışma kopyasının raporunu girdiye yansıtır.</summary>
        public static void Apply(Entry entry, LevelReport report)
        {
            if (entry == null || report == null) return;

            entry.Errors = report.Errors.Count;
            entry.Warnings = report.Warnings.Count;
            entry.Moves = report.Solution != null ? report.Solution.Moves.Count : 0;

            entry.Status = !report.Ok ? Status.Broken
                : report.Warnings.Count > 0 ? Status.Warning
                : Status.Ok;

            entry.Summary = !report.Ok
                ? (report.Errors.Count > 0 ? report.Errors[0] : "çözülemedi")
                : report.Warnings.Count > 0
                    ? $"{entry.Moves} hamle · {report.Warnings.Count} uyarı"
                    : $"{entry.Moves} hamle";
        }

        /// <summary>
        /// Bütün seti doğrular. Çözücü yavaş olduğu için ilerleme çubuğu
        /// gösterir ve iptal edilebilir — araç asla "donmuş" görünmemeli.
        /// </summary>
        public static void ValidateAll(ColorPaletteSO palette, GameConfigSO config)
        {
            var list = Entries;
            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        "Bölümler doğrulanıyor",
                        $"{list[i].Name}  ({i + 1}/{list.Count})",
                        (i + 1f) / list.Count);

                    Validate(list[i], palette, config);
                    if (cancel) break;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void Tally(out int ok, out int warning, out int broken, out int unknown)
        {
            ok = warning = broken = unknown = 0;
            foreach (var entry in Entries)
                switch (entry.Status)
                {
                    case Status.Ok: ok++; break;
                    case Status.Warning: warning++; break;
                    case Status.Broken: broken++; break;
                    default: unknown++; break;
                }
        }

        // ---------------- set üzerinde işlemler ----------------

        const string CatalogPath = "Assets/_Project/Resources/LevelCatalog.asset";

        /// <summary>
        /// Bölümü katalogda bir sıra yukarı/aşağı taşır — oynanış sırası içeriktir,
        /// dosya adını değiştirmeden düzenlenebilmeli.
        /// </summary>
        public static bool Reorder(string path, int step)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogSO>(CatalogPath);
            if (catalog == null || catalog.levels == null) return false;

            int from = System.Array.FindIndex(catalog.levels,
                asset => asset != null && AssetDatabase.GetAssetPath(asset) == path);
            int to = from + step;
            if (from < 0 || to < 0 || to >= catalog.levels.Length) return false;

            (catalog.levels[from], catalog.levels[to]) = (catalog.levels[to], catalog.levels[from]);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);

            // Sıra değişti → bölüm NUMARALARI da değişmeli.
            //
            // DERS (iki yerde duran tek gerçek): Oynanış sırası katalogda,
            // oyuncuya gösterilen numara ise her bölümün kendi JSON'undaki
            // `displayNumber` alanında. GameplayScreen "Level N" yazarken o
            // alanı okuyor. Sırayı değiştirip numarayı bırakmak, 7. sıradaki
            // bölümün ekranda "Level 12" demesine yol açardı — araç sessizce
            // tutarsız veri üretirdi.
            RenumberFromCatalog(catalog);

            Invalidate();
            return true;
        }

        /// <summary>
        /// Her bölümün <c>displayNumber</c> alanını katalogdaki sırasına eşitler.
        /// Yalnız DEĞİŞENLERİ diske yazar; dokunulmayan dosya kirletilmez.
        /// </summary>
        static int RenumberFromCatalog(LevelCatalogSO catalog)
        {
            if (catalog?.levels == null) return 0;

            int changed = 0;
            for (int i = 0; i < catalog.levels.Length; i++)
            {
                var asset = catalog.levels[i];
                if (asset == null) continue;

                string path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(path)) continue;

                try
                {
                    var data = LevelEditorIO.FromJson(System.IO.File.ReadAllText(path));
                    if (data.DisplayNumber == i + 1) continue;

                    data.DisplayNumber = i + 1;
                    LevelEditorIO.Save(data, path);
                    changed++;
                }
                catch (System.Exception error)
                {
                    Debug.LogWarning($"[LevelLibrary] {path} numaralanamadı: {error.Message}");
                }
            }
            return changed;
        }

        /// <summary>Bölümü diskten siler ve katalogdan düşürür. Çağıran onay almalı.</summary>
        public static void Delete(string path)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogSO>(CatalogPath);
            if (catalog != null && catalog.levels != null)
            {
                var kept = new List<TextAsset>();
                foreach (var asset in catalog.levels)
                    if (asset != null && AssetDatabase.GetAssetPath(asset) != path) kept.Add(asset);

                if (kept.Count != catalog.levels.Length)
                {
                    catalog.levels = kept.ToArray();
                    EditorUtility.SetDirty(catalog);
                    AssetDatabase.SaveAssetIfDirty(catalog);
                }
            }

            AssetDatabase.DeleteAsset(path);

            // Silmek de sırayı kaydırır: kalan bölümlerin numaraları bir
            // eksilmeli, yoksa 8. sıradaki bölüm oyunda "Level 9" der.
            if (catalog != null) RenumberFromCatalog(catalog);

            Invalidate();
        }

        /// <summary>Bölümün kopyasını setin sonuna yeni dosya olarak ekler.</summary>
        public static string Duplicate(string path)
        {
            var data = LevelEditorIO.FromJson(System.IO.File.ReadAllText(path));
            string target = LevelEditorIO.NextLevelPath(out int number);

            data.Id = $"level_{number:000}";
            data.DisplayNumber = number;
            LevelEditorIO.Save(data, target);

            // DERS (yarım kalan işlem, olmayan işlemden kötüdür): Kopya yalnız
            // diske yazılıyordu. Katalogda yer almadığı için oyunda AÇILMIYOR,
            // listede de en sona "sırasız" düşüyordu; kullanıcı bunu ancak
            // oynamaya çalışınca fark ederdi. Çoğaltmanın anlamı "sete yeni
            // bölüm ekle"dir, "klasöre dosya bırak" değil.
            Register(target);

            Invalidate();
            return target;
        }

        /// <summary>Bölümü oynanış sırasının SONUNA ekler (zaten varsa dokunmaz).</summary>
        public static bool Register(string path)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogSO>(CatalogPath);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (catalog == null || asset == null) return false;

            var levels = new List<TextAsset>(catalog.levels ?? new TextAsset[0]);
            if (levels.Contains(asset)) return false;

            levels.Add(asset);
            catalog.levels = levels.ToArray();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return true;
        }
    }
}
