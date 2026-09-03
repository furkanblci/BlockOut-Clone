using System.Collections.Generic;
using GameKit.Editor;
using GameKit.UI;
using UnityEditor;
using UnityEngine;

namespace GameKit.Editor.UiDesign
{
    /// <summary>
    /// "Kayıtlı" ve "Kılavuz" sekmeleri.
    ///
    /// KAYITLI SEKMESİNİN ASIL İŞİ SAHİPSİZ KAYITLARI BULMAK.
    ///
    /// DERS (sessizce kopan bağ, en tehlikeli hata türüdür): Düzeltmeler
    /// hiyerarşi YOLUNA göre saklanıyor ("HomeScreen/Play/Ribbon"). Kodda bir
    /// nesnenin adı değişirse ya da nesne başka bir ebeveynin altına taşınırsa,
    /// o yol artık hiçbir şeye denk gelmez. Sistem HATA VERMEZ — düzeltme
    /// sadece uygulanmaz. Aylar sonra "ben bunu ayarlamıştım" denir ve sebebi
    /// bulunamaz. Bu yüzden araç, kayıttaki her yolu gerçekten kurulan
    /// panellerde ARAYABİLİYOR ve karşılığı olmayanları kırmızıyla gösteriyor.
    /// </summary>
    public sealed partial class UiDesignWindow
    {
        Vector2 _savedScroll, _guideScroll;
        HashSet<string> _knownPaths;
        readonly List<string> _scanFailures = new List<string>();

        // ------------------------------------------------------------ kayıtlı

        void DrawSavedTab()
        {
            var asset = UiTweakDiff.Find();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (EditorSkin.Button("Bütün panelleri tara", EditorSkin.Neutral,
                        170f, 24f, "Her paneli sırayla kurup kayıttaki yolların hâlâ " +
                                   "karşılığı var mı diye bakar"))
                    ScanAllPanels();

                if (EditorSkin.Button("Varlığı seç", EditorSkin.Neutral, 110f, 24f))
                {
                    var found = UiTweakDiff.Find();
                    if (found != null) Selection.activeObject = found;
                }
            }

            if (asset == null || asset.Entries == null || asset.Entries.Length == 0)
            {
                EditorSkin.Note("Henüz kaydedilmiş düzeltme yok.",
                    EditorSkin.NoteKind.Info);
                return;
            }

            if (_knownPaths == null)
                EditorSkin.Note(
                    "Sahipsiz kayıtları görmek için önce \"Bütün panelleri tara\"ya bas. " +
                    "Tarama her paneli tek tek kurar; birkaç saniye sürer.",
                    EditorSkin.NoteKind.Info);
            else if (_scanFailures.Count > 0)
                EditorSkin.Note(
                    "Şu paneller kurulamadığı için yolları doğrulanamadı: " +
                    string.Join(", ", _scanFailures) +
                    ". Onların altındaki kayıtlar \"bilinmiyor\" sayılıyor.",
                    EditorSkin.NoteKind.Warning);

            _savedScroll = EditorGUILayout.BeginScrollView(_savedScroll);

            string group = null;
            foreach (var entry in asset.Entries)
            {
                string root = RootOf(entry.path);
                if (root != group)
                {
                    group = root;
                    EditorSkin.SectionHeader(group);
                }

                DrawSavedRow(entry);
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawSavedRow(UiOverride entry)
        {
            bool orphan = _knownPaths != null && !_knownPaths.Contains(entry.path);

            var row = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));

            if (orphan)
                EditorSkin.Fill(row, new Color(EditorSkin.Danger.r,
                    EditorSkin.Danger.g, EditorSkin.Danger.b, 0.25f));

            GUI.Label(new Rect(row.x + 8f, row.y, row.width - 220f, row.height), entry.path,
                new GUIStyle(EditorSkin.RowLabel)
                {
                    fontSize = 10,
                    normal = { textColor = orphan
                        ? EditorSkin.DangerBright : EditorSkin.TextMuted }
                });

            GUI.Label(new Rect(row.xMax - 210f, row.y, 130f, row.height), FlagsOf(entry),
                new GUIStyle(EditorSkin.RowLabel)
                { fontSize = 10, alignment = TextAnchor.MiddleRight,
                  normal = { textColor = EditorSkin.TextDim } });

            if (EditorSkin.Button(new Rect(row.xMax - 70f, row.y + 1f, 60f, 17f),
                    "sil", EditorSkin.Neutral, "Bu kaydı siler"))
            {
                UiTweakDiff.Clear(new[] { entry.path });
                _status = "Silindi: " + entry.path;
                if (_stage != null) Rebuild();
            }
        }

        static string RootOf(string path)
        {
            if (string.IsNullOrEmpty(path)) return "—";
            int slash = path.IndexOf('/');
            return slash < 0 ? path : path.Substring(0, slash);
        }

        static string FlagsOf(UiOverride entry)
        {
            var parts = new List<string>(5);
            if (entry.hasRect)   parts.Add("yerleşim");
            if (entry.hasScale)  parts.Add("ölçek");
            if (entry.hasColor)  parts.Add("renk");
            if (entry.hasFont)   parts.Add("punto");
            if (entry.hasActive) parts.Add("görünürlük");
            return parts.Count == 0 ? "boş" : string.Join(" · ", parts);
        }

        /// <summary>
        /// Bütün panelleri sırayla kurup GEÇERLİ yolların listesini çıkarır.
        /// Kurulum hatası olan paneller ayrıca raporlanıyor: onların yollarını
        /// "sahipsiz" ilan etmek, var olan bir ayarı yanlışlıkla sildirirdi.
        /// </summary>
        void ScanAllPanels()
        {
            var paths = new HashSet<string>();
            _scanFailures.Clear();

            // Açık tezgâh kapatılıyor: tarama kendi tezgâhlarını kuracak ve
            // aynı anda iki kanvas kurulu kalması karışıklık yaratır.
            DropStage();

            try
            {
                int index = 0;
                foreach (var entry in UiPanelCatalog.All)
                {
                    EditorUtility.DisplayProgressBar("Arayüz Tasarımı",
                        "Taranıyor: " + entry.Label,
                        (float)index++ / UiPanelCatalog.All.Count);

                    UiPanelStage stage = null;
                    try
                    {
                        stage = UiPanelStage.Build(entry, 0);
                        if (!stage.Ok) { _scanFailures.Add(entry.Label); continue; }
                        foreach (var key in stage.Baseline.Keys) paths.Add(key);
                    }
                    finally
                    {
                        stage?.Dispose();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _knownPaths = paths;
            _status = $"Tarama bitti: {paths.Count} geçerli yol" +
                      (_scanFailures.Count > 0 ? $", {_scanFailures.Count} panel kurulamadı." : ".");
            Rebuild();
        }

        // ------------------------------------------------------------ kılavuz

        void DrawGuideTab()
        {
            _guideScroll = EditorGUILayout.BeginScrollView(_guideScroll);

            EditorSkin.SectionHeader("Nasıl çalışır");
            GUILayout.Label(
                "Bu oyunun arayüzü prefab'sız, kodla kuruluyor. Bu pencere kodu " +
                "değiştirmiyor; kodun ürettiği hâlin ÜSTÜNE binen bir fark listesi " +
                "tutuyor (Assets/_Project/Resources/UiLayout.asset). Oyun açılırken " +
                "her ekran kurulduktan sonra o liste bir kez uygulanıyor.\n\n" +
                "Sonuç: kod hâlâ tek kaynak. Bir ölçüyü kodda değiştirdiğinde elle " +
                "dokunmadığın her şey yeni değeri kendiliğinden alır — donmuş bir " +
                "kopya kalmaz.",
                EditorSkin.SectionBody);

            EditorSkin.SectionHeader("Neyi düzenleyebilir");
            GUILayout.Label(
                "· Yerleşim (çapalar, kenar payı, eksen)\n" +
                "· Ölçek\n" +
                "· Renk (Image, yazı rengi — Graphic taşıyan her şey)\n" +
                "· Punto\n" +
                "· Görünürlük (aç/kapa)",
                EditorSkin.SectionBody);

            EditorSkin.SectionHeader("Neyi düzenleyemez — ve neden");
            EditorSkin.Note(
                "Bu oyunun görselinin büyük kısmı PROSEDÜREL çiziliyor (UiSprites, " +
                "UiVerticalTint, UiTitleEmboss, UiCornerFit). Köşe yarıçapı, degrade " +
                "renkleri, kabartma derinliği gibi değerler bir RectTransform alanı " +
                "değil, kodun çizim parametresi. Onları buradan ayarlayabilmek için " +
                "fark listesinin şeması genişletilmeli — ihtiyaç çıktıkça, hepsini " +
                "birden değil.",
                EditorSkin.NoteKind.Warning);

            EditorSkin.Note(
                "Metnin KENDİSİ de düzenlenemiyor. Bilerek: yazılar oyunun durumundan " +
                "geliyor (jeton sayısı, bölüm adı, kalan süre). Bir yazıyı sabitlemek " +
                "onu oyundan koparırdı.",
                EditorSkin.NoteKind.Info);

            EditorSkin.SectionHeader("Kaydettiğin şey nereye gidiyor");
            GUILayout.Label(
                "Varlığa. Kodun yanındaki yorumlara DEĞİL. Bu projenin en değerli " +
                "tarafı ölçülerin neden öyle olduğunun kodda yazılı olması; " +
                "düzeltmeler biriktikçe gerçek koddan varlığa kayar. Denetçideki " +
                "\"Kod satırını kopyala\" düğmesi tam bunun için var: beğendiğin " +
                "ayarı kaynağa geri katlamak iki saniyelik iş olsun.",
                EditorSkin.SectionBody);

            EditorSkin.SectionHeader("Henüz listede olmayan paneller");
            foreach (var missing in UiPanelCatalog.Missing)
            {
                GUILayout.Label("· " + missing.Label,
                    new GUIStyle(EditorSkin.RowLabel) { fontSize = 11 });
                GUILayout.Label("    " + missing.Reason,
                    new GUIStyle(EditorSkin.SectionBody) { fontSize = 10 });
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
