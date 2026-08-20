using BlockOut.Core;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Kabuğun üst şeridi: kimlik, gezinme ve dosya eylemleri.
    ///
    /// DERS (kabuk her sekmede AYNI kalmalı): Kaydet, Geri al ve Play test
    /// sekmeye bağlı değildir — hangi ekranda olursan ol aynı yerdedirler.
    /// Bir araçta en sık kullanılan eylemler yer değiştirirse kullanıcı her
    /// seferinde onları ARAR; sabit şerit bu aramayı sıfıra indirir.
    ///
    /// DERS (yıkıcı olmayan eylem yeşil, geri alınamaz olan turuncu):
    /// Kaydet ve Play yeşil, önbellek temizleme turuncu. Renk bir süs değil,
    /// eylemin sonucunun ne kadar kalıcı olduğunun göstergesi.
    /// </summary>
    public sealed partial class LevelEditorWindow
    {
        void DrawTopBar()
        {
            const float height = 52f;
            var band = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            LevelEditorSkin.Fill(band, LevelEditorSkin.Bar);
            LevelEditorSkin.Fill(new Rect(band.x, band.yMax - 1f, band.width, 1f),
                LevelEditorSkin.Hairline);

            DrawBrand(band);

            float actionsWidth = DrawActions(band, measureOnly: true);
            float actionsX = band.xMax - actionsWidth - 8f;
            DrawActions(new Rect(actionsX, band.y, actionsWidth, band.height), measureOnly: false);

            float dotsWidth = _palette != null ? PaletteDotsWidth() : 0f;
            float dotsX = actionsX - dotsWidth - 14f;
            if (dotsWidth > 0f && dotsX > 210f)
                DrawPaletteDots(new Rect(dotsX, band.y, dotsWidth, band.height));

            float identityX = 210f;
            float identityWidth = Mathf.Min(320f, (dotsX > identityX ? dotsX : actionsX) - identityX - 14f);
            if (identityWidth > 170f)
                DrawIdentity(new Rect(identityX, band.y, identityWidth, band.height));
        }

        void DrawBrand(Rect band)
        {
            var title = new GUIStyle(LevelEditorSkin.SectionTitle) { fontSize = 15 };
            LevelEditorSkin.TrackedLabel(
                new Rect(band.x + 14f, band.y + 7f, 200f, 20f), "LEVEL EDİTÖR", title, 1.8f);

            GUI.Label(new Rect(band.x + 14f, band.y + 27f, 200f, 16f),
                LevelLibrary.Count + " bölüm · block out", LevelEditorSkin.SectionBody);
        }

        /// <summary>Ortadaki kimlik alanı: bölüm adı üstte, düzenlenebilir kimlik altta.</summary>
        void DrawIdentity(Rect area)
        {
            string label = _path != null
                ? System.IO.Path.GetFileNameWithoutExtension(_path)
                : "(kaydedilmemiş)";

            int index = LevelLibrary.IndexOf(_path);
            if (index >= 0) label += $"   —   sıra {index + 1}/{LevelLibrary.Count}";
            if (_dirty) label += "  •";

            var nameStyle = new GUIStyle(LevelEditorSkin.Value)
            { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            nameStyle.normal.textColor = _dirty ? LevelEditorSkin.Warning : LevelEditorSkin.Text;
            GUI.Label(new Rect(area.x, area.y + 6f, area.width, 18f), label, nameStyle);

            var row = new Rect(area.x, area.y + 26f, area.width, 20f);
            EditorGUI.BeginChangeCheck();
            float idWidth = row.width - 66f;
            string id = EditorGUI.TextField(new Rect(row.x, row.y, idWidth, row.height), _data.Id);
            int number = EditorGUI.IntField(
                new Rect(row.x + idWidth + 6f, row.y, 60f, row.height), _data.DisplayNumber);
            if (EditorGUI.EndChangeCheck())
            {
                _data.Id = id;
                _data.DisplayNumber = number;
                AfterChange();
            }
        }

        // ---------------- palet noktaları ----------------

        static readonly BlockColor[] AllColors = (BlockColor[])System.Enum.GetValues(typeof(BlockColor));
        const float DotSize = 14f, DotGap = 4f;

        static float PaletteDotsWidth() => AllColors.Length * (DotSize + DotGap);

        /// <summary>
        /// Bu bölümde HANGİ renkler kullanılıyor — kullanılan parlak, kullanılmayan
        /// soluk. Kapısı olmayan bir renk kırmızı kenarla işaretlenir: o renk
        /// tahtada var ama çıkışı yok, yani bölüm çözülemez.
        /// </summary>
        void DrawPaletteDots(Rect area)
        {
            var blockColors = new System.Collections.Generic.HashSet<BlockColor>();
            var gateColors = new System.Collections.Generic.HashSet<BlockColor>();

            foreach (var block in _data.Blocks)
                foreach (var layer in block.Layers)
                    if (BlockColorUtil.TryParse(layer, out var parsed)) blockColors.Add(parsed);

            foreach (var obstacle in _data.Obstacles)
                foreach (var hidden in LevelEditorIO.GetContents(obstacle))
                    foreach (var layer in hidden.Layers)
                        if (BlockColorUtil.TryParse(layer, out var parsed)) blockColors.Add(parsed);

            foreach (var gate in _data.Gates)
                foreach (var id in gate.Colors)
                    if (BlockColorUtil.TryParse(id, out var parsed)) gateColors.Add(parsed);

            float x = area.x;
            float y = area.center.y - DotSize * 0.5f;

            foreach (var color in AllColors)
            {
                bool used = blockColors.Contains(color);
                var box = new Rect(x, y, DotSize, DotSize);

                var tint = ColorOf(color);
                if (!used) tint = new Color(tint.r, tint.g, tint.b, 0.22f);
                LevelCanvasDrawer.Fill(box, tint);

                if (used && !gateColors.Contains(color))
                    LevelCanvasDrawer.Outline(box, new Color(1f, 0.3f, 0.3f), 2f);

                if (GUI.Button(box, new GUIContent("", used
                        ? color + (gateColors.Contains(color) ? " — kapısı var" : " — KAPISI YOK")
                        : color + " — kullanılmıyor"), GUIStyle.none))
                {
                    _gateColor = color;
                    _layers[Mathf.Clamp(_activeLayer, 0, _layers.Count - 1)] = color;
                    Say(color + " rengi fırçaya alındı");
                }

                x += DotSize + DotGap;
            }
        }

        // ---------------- eylem düğmeleri ----------------

        /// <summary>
        /// Düğme sırasını çizer. <paramref name="measureOnly"/> ile aynı kod
        /// hem genişliği ölçer hem çizer — iki ayrı listede sıra tutmak, birini
        /// güncellemeyi unutmak demektir.
        /// </summary>
        float DrawActions(Rect area, bool measureOnly)
        {
            float x = area.x;
            float y = area.center.y - 12f;
            const float h = 24f, gap = 4f;

            bool Button(string label, string tip, float width, Color? tint = null, bool enabled = true)
            {
                var rect = new Rect(x, y, width, h);
                x += width + gap;
                return !measureOnly && LevelEditorSkin.Button(
                    rect, label, tint ?? LevelEditorSkin.Neutral, tip, false, enabled);
            }

            bool Dropdown(string label, float width)
            {
                var rect = new Rect(x, y, width, h);
                x += width + gap;
                if (measureOnly) return false;

                bool clicked = LevelEditorSkin.Button(rect, label + "  ▾", LevelEditorSkin.Neutral);
                return clicked;
            }

            if (Button("Yeni", "Boş bölüm", 44f))
            {
                if (ConfirmDiscard())
                {
                    _data = LevelEditorIO.NewLevel();
                    _path = null; _selections.Clear();
                    _undoStack.Clear(); _redoStack.Clear();
                    AfterChange(); _dirty = false;
                    Say("Yeni bölüm");
                }
            }

            if (Dropdown("Aç", 54f)) ShowLevelMenu();

            x += 6f;
            if (Button("◀ Önceki", "Listede bir önceki bölüm (Ctrl+PageUp)", 68f,
                    enabled: LevelLibrary.Neighbour(_path, -1) != null))
                GoToNeighbour(-1);
            if (Button("Sonraki ▶", "Listede bir sonraki bölüm (Ctrl+PageDown)", 74f,
                    enabled: LevelLibrary.Neighbour(_path, 1) != null))
                GoToNeighbour(1);

            x += 6f;
            if (Button("↶", "Geri al (Ctrl+Z)", 26f, enabled: _undoStack.Count > 0)) Undo();
            if (Button("↷", "İleri al (Ctrl+Y)", 26f, enabled: _redoStack.Count > 0)) Redo();

            x += 6f;
            if (Button("Kaydet", "Ctrl+S", 58f, LevelEditorSkin.Positive, _path != null && _dirty))
                SaveTo(_path);
            if (Dropdown("Kaydet", 72f)) ShowSaveMenu();

            x += 6f;
            if (Button("▶ Play", "Bu bölümü hemen oyna", 60f, LevelEditorSkin.Positive))
            {
                StashState();
                LevelEditorIO.PlayTest(_data);
            }

            if (Button("Önbellek", "Minyatür ve doğrulama önbelleğini temizle", 70f,
                    LevelEditorSkin.Warning))
            {
                LevelLibrary.Invalidate();
                Say("Önbellek temizlendi");
            }

            return x - area.x;
        }

        // ---------------- dosya menüleri ----------------

        void ShowLevelMenu()
        {
            var menu = new GenericMenu();
            foreach (var entry in LevelLibrary.Entries)
            {
                string captured = entry.Path;
                menu.AddItem(new GUIContent(entry.Name), captured == _path,
                    () => { if (ConfirmDiscard()) LoadFrom(captured); });
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Dosyadan aç…"), false, () =>
            {
                if (!ConfirmDiscard()) return;
                string path = LevelEditorIO.AskLoadPath();
                if (path != null) LoadFrom(path);
            });
            menu.ShowAsContext();
        }

        void ShowSaveMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Farklı kaydet…"), false, () =>
            {
                string path = LevelEditorIO.AskSavePath(_data.Id);
                if (path != null) SaveTo(path);
            });
            menu.AddItem(new GUIContent("Sonraki bölüm olarak kaydet"), false, () =>
            {
                string path = LevelEditorIO.NextLevelPath(out int number);
                _data.Id = $"level_{number:000}";
                _data.DisplayNumber = number;
                SaveTo(path);
            });
            menu.ShowAsContext();
        }
    }
}
