using System.Collections.Generic;
using BlockOut.Core;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Tahta sekmesinin sol kolonu: araç seçimi, fırça ayarları, seçili nesne
    /// denetçisi ve bölüm bilgileri.
    ///
    /// DERS (denetçi BAĞLAMA duyarlı olmalı): Panel "hiçbir şey seçili değilken
    /// fırçayı", "bir şey seçiliyken o nesneyi" gösterir. İkisini birden
    /// göstermek kullanıcıyı her seferinde "şimdi hangisini değiştiriyorum?"
    /// diye düşündürür.
    /// </summary>
    public sealed partial class LevelEditorWindow
    {
        void DrawSidePanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(PanelWidth)))
            using (var scroll = new EditorGUILayout.ScrollViewScope(_panelScroll))
            {
                _panelScroll = scroll.scrollPosition;

                DrawToolButtons();
                EditorGUILayout.Space(6);

                if (_tool == Tool.Select && _selections.Count > 0) DrawSelectionInspector();
                else DrawToolOptions();

                EditorGUILayout.Space(8);
                DrawLevelSettings();
            }
        }

        void DrawToolButtons()
        {
            LevelEditorSkin.SectionHeader("Araçlar");

            const int perRow = 4;
            int rows = (ToolInfo.Length + perRow - 1) / perRow;
            for (int row = 0; row < rows; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = row * perRow; i < Mathf.Min(row * perRow + perRow, ToolInfo.Length); i++)
                    {
                        bool on = (int)_tool == i;
                        if (LevelEditorSkin.Button(ToolInfo[i].label, LevelEditorSkin.Neutral,
                                62f, 28f, ToolInfo[i].tip, on) && !on)
                            RequestTool((Tool)i);
                    }

                    // Düğmeler SABİT genişlikte; eksik kalan yer esnek boşlukla
                    // doldurulur. Aksi hâlde 7 araç 4'e bölünmediği için son
                    // satırdaki üç düğme diğerlerinden geniş çıkardı.
                    GUILayout.FlexibleSpace();
                }
            }
            // Savunma sınırı: araç sayısı değiştiğinde diskte SERİLEŞMİŞ eski
            // _tool değeri diziyi aşabilir (Unity o değeri int olarak saklar).
            EditorGUILayout.LabelField(
                ToolInfo[Mathf.Clamp((int)_tool, 0, ToolInfo.Length - 1)].tip,
                LevelEditorSkin.SectionBody);
        }

        void DrawToolOptions()
        {
            switch (_tool)
            {
                case Tool.Blocks:
                    LevelEditorSkin.SectionHeader("Blok Şekli");
                    DrawSizePalette();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Özel", GUILayout.Width(34));
                        // Elle boyut girmek "düz dikdörtgen" demektir — maske düşer.
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            _blockW = Mathf.Clamp(EditorGUILayout.IntField(_blockW, GUILayout.Width(34)), 1, 6);
                            EditorGUILayout.LabelField("×", GUILayout.Width(12));
                            _blockH = Mathf.Clamp(EditorGUILayout.IntField(_blockH, GUILayout.Width(34)), 1, 6);
                            if (check.changed) _blockMask = null;
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("Buz", GUILayout.Width(26));
                        _blockIce = Mathf.Max(0, EditorGUILayout.IntField(_blockIce, GUILayout.Width(34)));
                    }

                    DrawAxisPicker();

                    EditorGUILayout.Space(4);
                    LevelEditorSkin.SectionHeader("Katmanlar", "Dıştan içe. Üst katman soyulunca altındaki renk kalır.");
                    DrawLayerChips();
                    DrawColorGrid(_layers[Mathf.Clamp(_activeLayer, 0, _layers.Count - 1)],
                        c => _layers[Mathf.Clamp(_activeLayer, 0, _layers.Count - 1)] = c);
                    DrawStampSection();
                    break;

                case Tool.Gates:
                    LevelEditorSkin.SectionHeader("Kapı Rengi");
                    DrawColorGrid(_gateColor, c => _gateColor = c);
                    _gateLength = LevelEditorSkin.SliderRow("Uzunluk", _gateLength, 1, 5);
                    _gateIce = Mathf.Max(0, LevelEditorSkin.IntRow("Buz kaplaması", _gateIce));
                    break;

                case Tool.Curtain:
                    _curtainCount = LevelEditorSkin.SliderRow("Sayaç", _curtainCount, 1, 20);
                    LevelEditorSkin.Note(
                        "Perde koyduktan sonra Blok aracıyla içine blok yerleştir — gizli içerik olurlar.");
                    break;

                case Tool.Generator:
                    LevelEditorSkin.SectionHeader("Sıradaki Blok", "Üreteç kondugunda sıraya bu blok girer.");
                    DrawSizePalette();
                    DrawColorGrid(_layers[Mathf.Clamp(_activeLayer, 0, _layers.Count - 1)],
                        c => _layers[Mathf.Clamp(_activeLayer, 0, _layers.Count - 1)] = c);
                    LevelEditorSkin.Note(
                        "Kenara tıkla — o girişe üreteç kondu ve fırçadaki blok sıraya girdi. " +
                        "Sıranın kalanını alt şeritteki üreteç kutusundan doldur (+ düğmesi). " +
                        "Sağ tık: üreteci sil.\n\n" +
                        "Üreteç zamanla değil YER AÇILINCA üretir; sıradaki bloklar da " +
                        "'oyunda olan renk' sayılır — o rengin kapısı olmalı.");
                    break;

                case Tool.Select:
                    LevelEditorSkin.Note(
                        "Nesneye tıkla, sürükleyerek taşı. Boş alanda sürükleyerek kutu seçim yap, " +
                        "Ctrl+tık ile seçime ekle. Alt+sürükle kopyalar.");
                    DrawStampSection();
                    break;
            }
        }

        /// <summary>
        /// Hareket kısıtı seçici. Referans oyunda bloğun üstündeki çift yönlü
        /// ok bunu gösterir: blok yalnızca o eksende sürüklenebilir.
        /// </summary>
        void DrawAxisPicker()
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Hareket", GUILayout.Width(52));
                DrawAxisButton("Serbest", "");
                DrawAxisButton("↔ yatay", "h");
                DrawAxisButton("↕ dikey", "v");
            }
        }

        /// <summary>Seçili bloğun eksenini değiştirir (fırçadan bağımsız).</summary>
        void DrawBlockAxisButton(BlockData block, string label, string value)
        {
            bool on = string.IsNullOrEmpty(block.Axis) ? value == null : block.Axis == value;
            var previous = GUI.backgroundColor;
            if (on) GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
            if (GUILayout.Button(label, GUILayout.Height(22)) && !on)
            {
                Record();
                block.Axis = value;
                AfterChange();
            }
            GUI.backgroundColor = previous;
        }

        void DrawAxisButton(string label, string value)
        {
            bool on = _blockAxis == value;
            var previous = GUI.backgroundColor;
            if (on) GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
            if (GUILayout.Button(label, GUILayout.Height(24))) _blockAxis = value;
            GUI.backgroundColor = previous;
        }

        /// <summary>Fırçanın maskesi; dikdörtgense null (JSON'a "cells" yazılmaz).</summary>
        List<string> BrushMask()
        {
            if (_blockMask == null || _blockMask.Count == 0) return null;
            return new List<string>(_blockMask);
        }

        /// <summary>Fırçayı bir ön ayara ayarlar; dolu maske dikdörtgene indirgenir.</summary>
        void SetBrushShape(string[] rows)
        {
            int w = 0, filled = 0;
            foreach (var row in rows)
            {
                w = Mathf.Max(w, row.Length);
                foreach (char c in row) if (char.ToUpperInvariant(c) == 'X') filled++;
            }
            _blockW = w;
            _blockH = rows.Length;
            _blockMask = filled == w * rows.Length ? null : new List<string>(rows);
        }

        static bool BrushMatches(List<string> mask, int w, int h, string[] rows)
        {
            bool presetIsRect = true;
            int presetW = 0;
            foreach (var row in rows)
            {
                presetW = Mathf.Max(presetW, row.Length);
                foreach (char c in row) if (char.ToUpperInvariant(c) != 'X') presetIsRect = false;
            }

            if (presetIsRect)
                return (mask == null || mask.Count == 0) && w == presetW && h == rows.Length;

            if (mask == null || mask.Count != rows.Length) return false;
            for (int i = 0; i < rows.Length; i++)
                if (!string.Equals(mask[i], rows[i], System.StringComparison.OrdinalIgnoreCase))
                    return false;
            return true;
        }

        /// <summary>Görsel şekil paleti — sayı girmek yerine şekle tıklanır.</summary>
        void DrawSizePalette()
        {
            const int perRow = 6;
            var brushColor = ColorOf(_layers.Count > 0 ? _layers[0] : BlockColor.Red);
            bool rowOpen = false;

            for (int i = 0; i < ShapePresets.Length; i++)
            {
                if (i % perRow == 0) { EditorGUILayout.BeginHorizontal(); rowOpen = true; }

                var preset = ShapePresets[i];
                bool selected = BrushMatches(_blockMask, _blockW, _blockH, preset.Rows);
                var rect = GUILayoutUtility.GetRect(42, 42, GUILayout.Width(42), GUILayout.Height(42));

                if (GUI.Button(rect, new GUIContent("", preset.Label)))
                    SetBrushShape(preset.Rows);

                DrawShapeIcon(rect, preset.Rows, brushColor);
                if (selected) LevelCanvasDrawer.Outline(rect, Color.white, 2f);

                if (i % perRow == perRow - 1) { EditorGUILayout.EndHorizontal(); rowOpen = false; }
            }

            if (rowOpen) EditorGUILayout.EndHorizontal();
        }

        /// <summary>Maskeyi küçük hücre kareleri olarak çizer (palet ikonu).</summary>
        static void DrawShapeIcon(Rect box, IReadOnlyList<string> rows, Color color)
        {
            int w = 0;
            for (int y = 0; y < rows.Count; y++) w = Mathf.Max(w, rows[y].Length);
            if (w == 0) return;

            float unit = Mathf.Min(30f / Mathf.Max(w, rows.Count), 9f);
            float originX = box.center.x - w * unit * 0.5f;
            float originY = box.center.y - rows.Count * unit * 0.5f;

            for (int y = 0; y < rows.Count; y++)
                for (int x = 0; x < rows[y].Length; x++)
                {
                    if (char.ToUpperInvariant(rows[y][x]) != 'X') continue;
                    LevelCanvasDrawer.Fill(
                        new Rect(originX + x * unit, originY + y * unit, unit - 1f, unit - 1f), color);
                }
        }

        void DrawLayerChips()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < _layers.Count; i++)
                {
                    var rect = GUILayoutUtility.GetRect(34, 26, GUILayout.Width(34), GUILayout.Height(26));
                    if (GUI.Button(rect, GUIContent.none)) _activeLayer = i;
                    LevelCanvasDrawer.Fill(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6),
                        ColorOf(_layers[i]));
                    if (i == _activeLayer) LevelCanvasDrawer.Outline(rect, Color.white, 2f);
                }

                if (_layers.Count < 3 && GUILayout.Button("+", GUILayout.Width(24), GUILayout.Height(26)))
                {
                    _layers.Add(BlockColor.Blue);
                    _activeLayer = _layers.Count - 1;
                }
                if (_layers.Count > 1 && GUILayout.Button("−", GUILayout.Width(24), GUILayout.Height(26)))
                {
                    _layers.RemoveAt(_layers.Count - 1);
                    _activeLayer = Mathf.Min(_activeLayer, _layers.Count - 1);
                }
                GUILayout.FlexibleSpace();
            }
        }

        /// <summary>
        /// Renk swatch ızgarası — açılır menü yerine tek tıkla renk.
        ///
        /// DERS (satır kapatmayı SAYIYA bağlama): Bu metot eskiden satırı
        /// `i % 4 == 3` olunca kapatıyordu; palet 8 renkken bu her zaman
        /// tutuyordu. Palet 10 renge çıkınca (mor + camgöbeği) son satır
        /// (9. ve 10. renk) HİÇ kapanmadı ve editör her karede
        /// "Invalid GUILayout state ... Begin/End calls match" hatası bastı.
        /// Kural: açılan grubu döngü bitiminde bir BAYRAKLA kapat, eleman
        /// sayısının bölünebilirliğine güvenme.
        /// </summary>
        void DrawColorGrid(BlockColor current, System.Action<BlockColor> onPick)
        {
            const int perRow = 4;
            var colors = (BlockColor[])System.Enum.GetValues(typeof(BlockColor));
            bool rowOpen = false;

            for (int i = 0; i < colors.Length; i++)
            {
                if (i % perRow == 0) { EditorGUILayout.BeginHorizontal(); rowOpen = true; }

                var rect = GUILayoutUtility.GetRect(56, 26, GUILayout.Height(26));
                if (GUI.Button(rect, new GUIContent("", colors[i].ToString()))) onPick(colors[i]);
                LevelCanvasDrawer.Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4),
                    ColorOf(colors[i]));
                if (colors[i] == current) LevelCanvasDrawer.Outline(rect, Color.white, 2f);

                if (i % perRow == perRow - 1) { EditorGUILayout.EndHorizontal(); rowOpen = false; }
            }

            // Son satır tam dolmadıysa burada kapanır.
            if (rowOpen) EditorGUILayout.EndHorizontal();
        }

        void DrawStampSection()
        {
            EditorGUILayout.Space(4);
            LevelEditorSkin.SectionHeader("Pano & Damgalar");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_selections.Count == 0))
                    if (GUILayout.Button(new GUIContent("Kopyala", "Ctrl+C"))) CopySelection();
                using (new EditorGUI.DisabledScope(!LevelEditorClipboard.HasContent))
                    if (GUILayout.Button(new GUIContent("Yapıştır", "Ctrl+V"))) PasteClipboard();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _stampName = EditorGUILayout.TextField(_stampName);
                using (new EditorGUI.DisabledScope(_selections.Count == 0 || string.IsNullOrWhiteSpace(_stampName)))
                    if (GUILayout.Button("Damga kaydet", GUILayout.Width(100)))
                    {
                        LevelEditorClipboard.SaveStamp(_stampName, SelectedBlocks());
                        ShowNotification(new GUIContent("Damga kaydedildi"));
                    }
            }

            var stamps = LevelEditorClipboard.StampNames();
            if (stamps.Length == 0) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (EditorGUILayout.DropdownButton(new GUIContent("Damga bas"), FocusType.Passive))
                {
                    var menu = new GenericMenu();
                    foreach (var name in stamps)
                    {
                        string captured = name;
                        menu.AddItem(new GUIContent(captured), false, () => StampAtCenter(captured));
                        menu.AddItem(new GUIContent("Sil/" + captured), false,
                            () => LevelEditorClipboard.DeleteStamp(captured));
                    }
                    menu.ShowAsContext();
                }
            }
        }

        void DrawSelectionInspector()
        {
            LevelEditorSkin.SectionHeader(_selections.Count > 1
                ? $"Seçili {_selections.Count} Nesne" : "Seçili Nesne");

            var primary = Primary;
            EditorGUI.BeginChangeCheck();

            switch (primary.Kind)
            {
                case SelKind.Block:
                case SelKind.Content:
                {
                    var block = BlockOf(primary);
                    if (block == null) { _selections.Clear(); return; }

                    EditorGUILayout.LabelField(primary.Kind == SelKind.Content
                        ? "Perde içeriği (gizli blok)" : "Blok", LevelEditorSkin.RowLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool shaped = block.Cells != null && block.Cells.Count > 0;
                        EditorGUILayout.LabelField(shaped ? "Şekil" : "Boyut", GUILayout.Width(42));
                        if (shaped)
                        {
                            // Maskeli blokta w×h yazmak anlamsız — şekli göster,
                            // değişiklik döndür/aynala ile yapılsın.
                            var icon = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28), GUILayout.Height(28));
                            DrawShapeIcon(icon, block.Cells, ColorOf(block.Layers.Count > 0 ? block.Layers[0] : "red"));
                            if (GUILayout.Button(new GUIContent("□", "Dikdörtgene çevir"), GUILayout.Width(24)))
                                block.Cells = null;
                        }
                        else
                        {
                            block.W = Mathf.Clamp(EditorGUILayout.IntField(block.W, GUILayout.Width(34)), 1, 6);
                            EditorGUILayout.LabelField("×", GUILayout.Width(12));
                            block.H = Mathf.Clamp(EditorGUILayout.IntField(block.H, GUILayout.Width(34)), 1, 6);
                        }
                        if (GUILayout.Button(new GUIContent("⟳", "Döndür (R)"), GUILayout.Width(26)))
                            RotateSelection();
                        if (GUILayout.Button(new GUIContent("⇄", "Aynala (F)"), GUILayout.Width(26)))
                            FlipSelection();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("Buz", GUILayout.Width(26));
                        block.Ice = Mathf.Max(0, EditorGUILayout.IntField(block.Ice, GUILayout.Width(34)));
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Hareket", GUILayout.Width(52));
                        DrawBlockAxisButton(block, "Serbest", null);
                        DrawBlockAxisButton(block, "↔ yatay", "h");
                        DrawBlockAxisButton(block, "↕ dikey", "v");
                    }

                    EditorGUILayout.LabelField("Katmanlar", LevelEditorSkin.RowLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int i = 0; i < block.Layers.Count; i++)
                        {
                            var r = GUILayoutUtility.GetRect(34, 24, GUILayout.Width(34), GUILayout.Height(24));
                            if (GUI.Button(r, GUIContent.none)) _activeLayer = i;
                            BlockColorUtil.TryParse(block.Layers[i], out var layerColor);
                            LevelCanvasDrawer.Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6),
                                ColorOf(layerColor));
                            if (i == Mathf.Clamp(_activeLayer, 0, block.Layers.Count - 1))
                                LevelCanvasDrawer.Outline(r, Color.white, 2f);
                        }
                        if (block.Layers.Count < 3 && GUILayout.Button("+", GUILayout.Width(24), GUILayout.Height(24)))
                            block.Layers.Add(BlockColor.Blue.ToId());
                        if (block.Layers.Count > 1 && GUILayout.Button("−", GUILayout.Width(24), GUILayout.Height(24)))
                            block.Layers.RemoveAt(block.Layers.Count - 1);
                        GUILayout.FlexibleSpace();
                    }

                    int layerIndex = Mathf.Clamp(_activeLayer, 0, block.Layers.Count - 1);
                    BlockColorUtil.TryParse(block.Layers[layerIndex], out var currentColor);
                    DrawColorGrid(currentColor, c => ApplyColorToSelection(c, layerIndex));

                    CommitContentEdit(primary, block);
                    break;
                }

                case SelKind.Gate:
                {
                    if (primary.Index >= _data.Gates.Count) { _selections.Clear(); return; }
                    var gate = _data.Gates[primary.Index];
                    EditorGUILayout.LabelField($"Kapı — {gate.Side} kenarı", LevelEditorSkin.RowLabel);
                    gate.Length = LevelEditorSkin.SliderRow("Uzunluk", gate.Length, 1, 5);
                    gate.Ice = Mathf.Max(0, LevelEditorSkin.IntRow("Buz kaplaması", gate.Ice));
                    BlockColorUtil.TryParse(gate.Colors.Count > 0 ? gate.Colors[0] : "red", out var gateColor);
                    DrawColorGrid(gateColor, c =>
                    {
                        if (gate.Colors.Count == 0) gate.Colors.Add(c.ToId());
                        else gate.Colors[0] = c.ToId();
                        AfterChange();
                    });
                    break;
                }

                case SelKind.Generator:
                case SelKind.Queued:
                {
                    if (primary.Index >= _data.Obstacles.Count) { _selections.Clear(); return; }

                    // DERS (indeks TÜR garantisi vermez): Engeller tek listede
                    // duruyor — perde ve üreteç yan yana. Seçim yalnız indeks
                    // tuttuğu için, liste değişince "üreteç" sanılan kayıt bir
                    // PERDEYE denk gelebiliyordu. O hâlde üreteç denetçisi
                    // perdenin üstünde çalışır ve "sıraya ekle" dediğinde
                    // perdeye `queue` alanı yazar — sessiz veri bozulması.
                    var obstacle = _data.Obstacles[primary.Index];
                    if (obstacle.Type != "generator") { _selections.Clear(); return; }

                    DrawGeneratorInspector(obstacle);
                    break;
                }

                case SelKind.Curtain:
                {
                    if (primary.Index >= _data.Obstacles.Count) { _selections.Clear(); return; }
                    var curtain = _data.Obstacles[primary.Index];
                    EditorGUILayout.LabelField("Perde", LevelEditorSkin.RowLabel);
                    int count = LevelEditorSkin.SliderRow("Sayaç",
                        LevelEditorIO.GetInt(curtain, "count", 1), 1, 20);
                    LevelEditorIO.SetInt(curtain, "count", count);
                    EditorGUILayout.LabelField(
                        $"Gizli içerik: {LevelEditorIO.GetContents(curtain).Count} blok",
                        LevelEditorSkin.RowLabel);
                    break;
                }
            }

            if (EditorGUI.EndChangeCheck()) AfterChange();

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Çoğalt")) DuplicateSelection();
                if (GUILayout.Button("Sil")) DeleteSelection();
            }
            DrawStampSection();
        }

        /// <summary>
        /// Seçili üretecin denetçisi: kenar, şerit ve sıranın tamamı.
        ///
        /// Sıra burada SATIR SATIR düzenlenir (alt şeritte yalnız renk kutuları
        /// var); şekil ve buz gibi alanlar ancak burada görünür.
        /// </summary>
        void DrawGeneratorInspector(ObstacleData generator)
        {
            EditorGUILayout.LabelField("Blok Üreteci", LevelEditorSkin.RowLabel);

            SideUtil.TryParse(LevelEditorIO.GetString(generator, "side", "N"), out var side);
            int lane = LevelEditorIO.GeneratorLane(generator);
            bool horizontal = side == Side.North || side == Side.South;
            int laneMax = (horizontal ? _data.Board.Width : _data.Board.Height) - 1;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Kenar", GUILayout.Width(42));
                foreach (var candidate in new[] { Side.North, Side.East, Side.South, Side.West })
                {
                    bool on = candidate == side;
                    var previous = GUI.backgroundColor;
                    if (on) GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
                    if (GUILayout.Button(candidate.ToId(), GUILayout.Height(22)) && !on)
                    {
                        Record();
                        LevelEditorIO.SetString(generator, "side", candidate.ToId());
                        // Kenar değişince şerit yeni kenarın uzunluğuna sığmalı.
                        bool nowHorizontal = candidate == Side.North || candidate == Side.South;
                        int limit = (nowHorizontal ? _data.Board.Width : _data.Board.Height) - 1;
                        LevelEditorIO.SetGeneratorLane(generator, Mathf.Clamp(lane, 0, limit));
                        AfterChange();
                    }
                    GUI.backgroundColor = previous;
                }
            }

            EditorGUI.BeginChangeCheck();
            int newLane = LevelEditorSkin.SliderRow(
                horizontal ? "Kolon" : "Satır", Mathf.Clamp(lane, 0, laneMax), 0, Mathf.Max(0, laneMax));
            if (EditorGUI.EndChangeCheck())
            {
                Record();
                LevelEditorIO.SetGeneratorLane(generator, newLane);
                AfterChange();
            }

            var queue = LevelEditorIO.GetQueue(generator);
            EditorGUILayout.LabelField($"Sıra — {queue.Count} blok (ilk sırada olan sıradaki)",
                LevelEditorSkin.Value);

            if (queue.Count == 0)
                LevelEditorSkin.Note("Sıra boş — bu makine hiç blok üretmez.",
                    LevelEditorSkin.NoteKind.Warning);

            bool changed = false;
            for (int i = 0; i < queue.Count; i++)
            {
                var block = queue[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label((i + 1) + ".", LevelEditorSkin.RowLabel, GUILayout.Width(20));

                    var swatch = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
                    LevelCanvasDrawer.Fill(swatch, ColorOf(block.Layers.Count > 0 ? block.Layers[0] : "red"));
                    // IndexOf -1 dönebilir (nesne listeden çıkmışsa); menüye
                    // geçersiz indeks göndermek tıklama anında istisna atardı.
                    int obstacleIndex = _data.Obstacles.IndexOf(generator);
                    if (GUI.Button(swatch, GUIContent.none, GUIStyle.none) && obstacleIndex >= 0)
                        ShowQueueMenu(obstacleIndex, i);

                    bool shaped = block.Cells != null && block.Cells.Count > 0;
                    if (shaped)
                    {
                        var icon = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
                        DrawShapeIcon(icon, block.Cells,
                            ColorOf(block.Layers.Count > 0 ? block.Layers[0] : "red"));
                    }
                    else
                    {
                        int w = Mathf.Clamp(EditorGUILayout.IntField(block.W, GUILayout.Width(28)), 1, 6);
                        GUILayout.Label("×", GUILayout.Width(10));
                        int h = Mathf.Clamp(EditorGUILayout.IntField(block.H, GUILayout.Width(28)), 1, 6);
                        if (w != block.W || h != block.H) { block.W = w; block.H = h; changed = true; }
                    }

                    GUILayout.Label("buz", LevelEditorSkin.RowLabel, GUILayout.Width(24));
                    int ice = Mathf.Max(0, EditorGUILayout.IntField(block.Ice, GUILayout.Width(28)));
                    if (ice != block.Ice) { block.Ice = ice; changed = true; }

                    // DERS: Listeyi çizim sırasının ORTASINDA kısaltmak, o
                    // geçişte Layout'ta ölçülenden az kontrol çizmek demektir.
                    // GUIUtility.ExitGUI() bu geçişi temiz biçimde iptal eder —
                    // `using` kapsamları Dispose edilir, layout grupları kapanır,
                    // Unity bir sonraki karede güncel listeyle baştan çizer.
                    using (new EditorGUI.DisabledScope(i == 0))
                        if (GUILayout.Button(new GUIContent("▲", "öne al"), GUILayout.Width(22)))
                        {
                            MoveInQueue(generator, i, -1);
                            GUIUtility.ExitGUI();
                        }

                    if (GUILayout.Button(new GUIContent("✕", "sıradan çıkar"), GUILayout.Width(22)))
                    {
                        Record();
                        queue.RemoveAt(i);
                        LevelEditorIO.SetQueue(generator, queue);
                        AfterChange();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (changed)
            {
                Record();
                LevelEditorIO.SetQueue(generator, queue);
                AfterChange();
            }

            if (GUILayout.Button("Fırçadaki bloğu sıraya ekle"))
            {
                Record();
                queue.Add(BrushBlock(0, 0));
                LevelEditorIO.SetQueue(generator, queue);
                AfterChange();
            }
        }

        void DrawLevelSettings()
        {
            LevelEditorSkin.SectionHeader("Bölüm Bilgileri");
            EditorGUI.BeginChangeCheck();
            _data.Id = LevelEditorSkin.TextRow("Kimlik", _data.Id);
            _data.DisplayNumber = LevelEditorSkin.IntRow("Bölüm No", _data.DisplayNumber);
            int diff = Mathf.Max(0, System.Array.IndexOf(Difficulties, _data.Difficulty));
            _data.Difficulty = Difficulties[LevelEditorSkin.PopupRow("Zorluk", diff, Difficulties)];
            _data.TimeSeconds = LevelEditorSkin.IntRow("Süre (sn)", _data.TimeSeconds);

            int w = LevelEditorSkin.SliderRow("Genişlik", _data.Board.Width, 3, 12);
            int h = LevelEditorSkin.SliderRow("Yükseklik", _data.Board.Height, 3, 14);
            if (EditorGUI.EndChangeCheck())
            {
                if (w != _data.Board.Width || h != _data.Board.Height) ResizeBoard(w, h);
                AfterChange();
            }
        }

        // Referans bindirmesi, bölüm tarayıcısı ve doğrulama raporu artık kendi
        // sekmelerinde: bkz. .Tabs (Referans / Doğrula / Çözüm) ve .Board (tarayıcı doku).

        /// <summary>
        /// Rengin tahtadaki katman sayısı ile o rengin kapı sayısını yan yana
        /// koyar. Kapısı olmayan renk = çözülemeyen bölüm; bu tablo o hatayı
        /// çözücüyü beklemeden gösterir.
        /// </summary>
        void DrawColorSummary()
        {
            if (_report.BlockCounts.Count == 0) return;

            EditorGUILayout.LabelField("Renk dağılımı", LevelEditorSkin.Value);
            foreach (var pair in _report.BlockCounts)
            {
                _report.GateCounts.TryGetValue(pair.Key, out int gates);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var swatch = GUILayoutUtility.GetRect(16, 14, GUILayout.Width(16), GUILayout.Height(14));
                    LevelCanvasDrawer.Fill(swatch, ColorOf(pair.Key));
                    EditorGUILayout.LabelField(
                        $"{pair.Key}: {pair.Value} katman · {gates} kapı" + (gates == 0 ? "  ⚠" : ""),
                        LevelEditorSkin.RowLabel);
                }
            }
        }
    }
}
