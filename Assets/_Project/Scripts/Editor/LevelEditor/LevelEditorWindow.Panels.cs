using System.Collections.Generic;
using BlockOut.Core;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>Level editörünün arayüz katmanı: üst şerit ve yan panel bölümleri.</summary>
    public sealed partial class LevelEditorWindow
    {
        // ---------------- üst şerit ----------------

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Yeni", EditorStyles.toolbarButton, GUILayout.Width(48)))
                {
                    if (ConfirmDiscard())
                    {
                        _data = LevelEditorIO.NewLevel();
                        _path = null; _selections.Clear();
                        _undoStack.Clear(); _redoStack.Clear();
                        AfterChange(); _dirty = false;
                    }
                }

                if (EditorGUILayout.DropdownButton(new GUIContent("Aç"),
                        FocusType.Passive, EditorStyles.toolbarDropDown, GUILayout.Width(46)))
                    ShowLevelMenu();

                using (new EditorGUI.DisabledScope(_path == null))
                    if (GUILayout.Button(new GUIContent("Kaydet", "Ctrl+S"),
                            EditorStyles.toolbarButton, GUILayout.Width(58)))
                        SaveTo(_path);

                if (EditorGUILayout.DropdownButton(new GUIContent("Kaydet…"),
                        FocusType.Passive, EditorStyles.toolbarDropDown, GUILayout.Width(66)))
                    ShowSaveMenu();

                GUILayout.Space(6);
                using (new EditorGUI.DisabledScope(_undoStack.Count == 0))
                    if (GUILayout.Button(new GUIContent("↶", "Geri al (Ctrl+Z)"),
                            EditorStyles.toolbarButton, GUILayout.Width(26))) Undo();
                using (new EditorGUI.DisabledScope(_redoStack.Count == 0))
                    if (GUILayout.Button(new GUIContent("↷", "İleri al (Ctrl+Y)"),
                            EditorStyles.toolbarButton, GUILayout.Width(26))) Redo();

                GUILayout.Space(6);
                if (EditorGUILayout.DropdownButton(new GUIContent("Bölüm"),
                        FocusType.Passive, EditorStyles.toolbarDropDown, GUILayout.Width(62)))
                    ShowLevelTransformMenu();

                GUILayout.Space(6);
                if (GUILayout.Button("Doğrula", EditorStyles.toolbarButton, GUILayout.Width(58)))
                    RunValidation();
                _autoValidate = GUILayout.Toggle(_autoValidate,
                    new GUIContent("Otomatik", "Her değişiklikten sonra doğrula"),
                    EditorStyles.toolbarButton, GUILayout.Width(62));

                if (GUILayout.Button(new GUIContent("▶ Play Test", "Bu bölümü hemen oyna"),
                        EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    StashState();
                    LevelEditorIO.PlayTest(_data);
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(_path == null
                    ? "(kaydedilmemiş)"
                    : System.IO.Path.GetFileName(_path) + (_dirty ? " •" : ""), EditorStyles.miniLabel);
            }
        }

        void ShowLevelMenu()
        {
            var menu = new GenericMenu();
            foreach (var path in LevelPaths())
                menu.AddItem(new GUIContent(System.IO.Path.GetFileName(path)), path == _path,
                    () => { if (ConfirmDiscard()) LoadFrom(path); });

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

        /// <summary>Bölüm geneli dönüşümler — varyant üretmenin en hızlı yolu.</summary>
        void ShowLevelTransformMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Yatay aynala"), false,
                () => { Record(); LevelTransform.MirrorHorizontal(_data); _selections.Clear(); AfterChange(); });
            menu.AddItem(new GUIContent("Dikey aynala"), false,
                () => { Record(); LevelTransform.MirrorVertical(_data); _selections.Clear(); AfterChange(); });
            menu.AddItem(new GUIContent("180° döndür"), false,
                () => { Record(); LevelTransform.Rotate180(_data); _selections.Clear(); AfterChange(); });
            menu.ShowAsContext();
        }

        static List<string> LevelPaths()
        {
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LevelEditorIO.LevelDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".json") && !path.Contains("__playtest")) paths.Add(path);
            }
            paths.Sort();
            return paths;
        }

        // ---------------- yan panel ----------------

        void DrawSidePanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(290)))
            using (var scroll = new EditorGUILayout.ScrollViewScope(_panelScroll))
            {
                _panelScroll = scroll.scrollPosition;

                DrawToolButtons();
                EditorGUILayout.Space(6);

                if (_tool == Tool.Select && _selections.Count > 0) DrawSelectionInspector();
                else DrawToolOptions();

                EditorGUILayout.Space(8);
                DrawLevelSettings();
                EditorGUILayout.Space(6);
                DrawReferenceSection();
                EditorGUILayout.Space(6);
                DrawBrowserSection();
                EditorGUILayout.Space(6);
                DrawReportSection();
            }
        }

        void DrawToolButtons()
        {
            EditorGUILayout.LabelField("Araçlar", EditorStyles.boldLabel);
            for (int row = 0; row < 2; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = row * 3; i < Mathf.Min(row * 3 + 3, ToolInfo.Length); i++)
                    {
                        bool on = (int)_tool == i;
                        var content = new GUIContent(ToolInfo[i].label, ToolInfo[i].tip);
                        if (GUILayout.Toggle(on, content, EditorStyles.miniButton, GUILayout.Height(26)) && !on)
                        {
                            _tool = (Tool)i;
                            if (_tool != Tool.Select) _selections.Clear();
                        }
                    }
                }
            }
            EditorGUILayout.LabelField(ToolInfo[(int)_tool].tip, EditorStyles.wordWrappedMiniLabel);
        }

        void DrawToolOptions()
        {
            switch (_tool)
            {
                case Tool.Blocks:
                    EditorGUILayout.LabelField("Blok Şekli", EditorStyles.boldLabel);
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

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Katmanlar (dıştan içe)", EditorStyles.boldLabel);
                    DrawLayerChips();
                    DrawColorGrid(_layers[Mathf.Clamp(_activeLayer, 0, _layers.Count - 1)],
                        c => _layers[Mathf.Clamp(_activeLayer, 0, _layers.Count - 1)] = c);
                    DrawStampSection();
                    break;

                case Tool.Gates:
                    EditorGUILayout.LabelField("Kapı Rengi", EditorStyles.boldLabel);
                    DrawColorGrid(_gateColor, c => _gateColor = c);
                    _gateLength = EditorGUILayout.IntSlider("Uzunluk", _gateLength, 1, 5);
                    _gateIce = Mathf.Max(0, EditorGUILayout.IntField("Buz kaplaması", _gateIce));
                    break;

                case Tool.Curtain:
                    _curtainCount = EditorGUILayout.IntSlider("Sayaç", _curtainCount, 1, 20);
                    EditorGUILayout.HelpBox(
                        "Perde koyduktan sonra Blok aracıyla içine blok yerleştir — gizli içerik olurlar.",
                        MessageType.Info);
                    break;

                case Tool.Select:
                    EditorGUILayout.HelpBox(
                        "Nesneye tıkla, sürükleyerek taşı. Boş alanda sürükleyerek kutu seçim yap, " +
                        "Ctrl+tık ile seçime ekle. Alt+sürükle kopyalar.", MessageType.Info);
                    DrawStampSection();
                    break;
            }
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
            var brushColor = ColorOf(_layers[0]);

            for (int i = 0; i < ShapePresets.Length; i++)
            {
                if (i % perRow == 0) EditorGUILayout.BeginHorizontal();

                var preset = ShapePresets[i];
                bool selected = BrushMatches(_blockMask, _blockW, _blockH, preset.Rows);
                var rect = GUILayoutUtility.GetRect(42, 42, GUILayout.Width(42), GUILayout.Height(42));

                if (GUI.Button(rect, new GUIContent("", preset.Label)))
                    SetBrushShape(preset.Rows);

                DrawShapeIcon(rect, preset.Rows, brushColor);
                if (selected) LevelCanvasDrawer.Outline(rect, Color.white, 2f);

                if (i % perRow == perRow - 1 || i == ShapePresets.Length - 1)
                    EditorGUILayout.EndHorizontal();
            }
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

        /// <summary>8 rengin swatch ızgarası — açılır menü yerine tek tıkla renk.</summary>
        void DrawColorGrid(BlockColor current, System.Action<BlockColor> onPick)
        {
            var colors = (BlockColor[])System.Enum.GetValues(typeof(BlockColor));
            for (int i = 0; i < colors.Length; i++)
            {
                if (i % 4 == 0) EditorGUILayout.BeginHorizontal();

                var rect = GUILayoutUtility.GetRect(56, 26, GUILayout.Height(26));
                if (GUI.Button(rect, new GUIContent("", colors[i].ToString()))) onPick(colors[i]);
                LevelCanvasDrawer.Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4),
                    ColorOf(colors[i]));
                if (colors[i] == current) LevelCanvasDrawer.Outline(rect, Color.white, 2f);

                if (i % 4 == 3) EditorGUILayout.EndHorizontal();
            }
        }

        void DrawStampSection()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Pano & Damgalar", EditorStyles.boldLabel);
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
            EditorGUILayout.LabelField(_selections.Count > 1
                ? $"Seçili {_selections.Count} nesne" : "Seçili Nesne", EditorStyles.boldLabel);

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
                        ? "Perde içeriği (gizli blok)" : "Blok", EditorStyles.miniLabel);

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

                    EditorGUILayout.LabelField("Katmanlar", EditorStyles.miniLabel);
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
                    EditorGUILayout.LabelField($"Kapı — {gate.Side} kenarı", EditorStyles.miniLabel);
                    gate.Length = EditorGUILayout.IntSlider("Uzunluk", gate.Length, 1, 5);
                    gate.Ice = Mathf.Max(0, EditorGUILayout.IntField("Buz kaplaması", gate.Ice));
                    BlockColorUtil.TryParse(gate.Colors.Count > 0 ? gate.Colors[0] : "red", out var gateColor);
                    DrawColorGrid(gateColor, c =>
                    {
                        if (gate.Colors.Count == 0) gate.Colors.Add(c.ToId());
                        else gate.Colors[0] = c.ToId();
                        AfterChange();
                    });
                    break;
                }

                case SelKind.Curtain:
                {
                    if (primary.Index >= _data.Obstacles.Count) { _selections.Clear(); return; }
                    var curtain = _data.Obstacles[primary.Index];
                    EditorGUILayout.LabelField("Perde", EditorStyles.miniLabel);
                    int count = EditorGUILayout.IntSlider("Sayaç",
                        LevelEditorIO.GetInt(curtain, "count", 1), 1, 20);
                    LevelEditorIO.SetInt(curtain, "count", count);
                    EditorGUILayout.LabelField(
                        $"Gizli içerik: {LevelEditorIO.GetContents(curtain).Count} blok",
                        EditorStyles.miniLabel);
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

        void DrawLevelSettings()
        {
            EditorGUILayout.LabelField("Bölüm Bilgileri", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _data.Id = EditorGUILayout.TextField("Kimlik", _data.Id);
            _data.DisplayNumber = EditorGUILayout.IntField("Bölüm No", _data.DisplayNumber);
            int diff = Mathf.Max(0, System.Array.IndexOf(Difficulties, _data.Difficulty));
            _data.Difficulty = Difficulties[EditorGUILayout.Popup("Zorluk", diff, Difficulties)];
            _data.TimeSeconds = EditorGUILayout.IntField("Süre (sn)", _data.TimeSeconds);

            int w = EditorGUILayout.IntSlider("Genişlik", _data.Board.Width, 3, 12);
            int h = EditorGUILayout.IntSlider("Yükseklik", _data.Board.Height, 3, 14);
            if (EditorGUI.EndChangeCheck())
            {
                if (w != _data.Board.Width || h != _data.Board.Height) ResizeBoard(w, h);
                AfterChange();
            }
        }

        // ---------------- referans görsel ----------------

        void DrawReferenceSection()
        {
            _showReference = EditorGUILayout.Foldout(_showReference, "Referans Görsel", true);
            if (!_showReference) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _reference.Visible = EditorGUILayout.ToggleLeft("Göster", _reference.Visible,
                        GUILayout.Width(70));
                    _reference.Behind = EditorGUILayout.ToggleLeft(
                        new GUIContent("Arkada", "Kapalıysa ızgaranın üstünde çizilir"),
                        _reference.Behind, GUILayout.Width(70));
                    if (GUILayout.Button("Görsel seç…"))
                    {
                        string path = EditorUtility.OpenFilePanel("Referans görsel", "", "png,jpg,jpeg");
                        if (!string.IsNullOrEmpty(path)) _reference.SetImage(path);
                    }
                }

                _reference.Opacity = EditorGUILayout.Slider("Opaklık", _reference.Opacity, 0f, 1f);
                _reference.Scale = EditorGUILayout.Slider("Ölçek", _reference.Scale, 0.2f, 3f);
                _reference.Offset = EditorGUILayout.Vector2Field("Kaydırma", _reference.Offset);

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Videodan kare çıkar", EditorStyles.miniBoldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("ffmpeg", GUILayout.Width(48));
                    EditorGUILayout.LabelField(
                        string.IsNullOrEmpty(LevelReferenceOverlay.FfmpegPath)
                            ? "(ayarlı değil)" : System.IO.Path.GetFileName(LevelReferenceOverlay.FfmpegPath),
                        EditorStyles.miniLabel);
                    if (GUILayout.Button("Seç…", GUILayout.Width(48)))
                    {
                        string path = EditorUtility.OpenFilePanel("ffmpeg çalıştırılabiliri", "", "exe");
                        if (!string.IsNullOrEmpty(path)) LevelReferenceOverlay.FfmpegPath = path;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Video", GUILayout.Width(48));
                    EditorGUILayout.LabelField(
                        string.IsNullOrEmpty(LevelReferenceOverlay.VideoPath)
                            ? "(ayarlı değil)" : System.IO.Path.GetFileName(LevelReferenceOverlay.VideoPath),
                        EditorStyles.miniLabel);
                    if (GUILayout.Button("Seç…", GUILayout.Width(48)))
                    {
                        string path = EditorUtility.OpenFilePanel("Referans video", "", "mp4,mov,mkv");
                        if (!string.IsNullOrEmpty(path)) LevelReferenceOverlay.VideoPath = path;
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _reference.Timestamp = EditorGUILayout.TextField("Zaman", _reference.Timestamp);
                    if (GUILayout.Button("Kare al", GUILayout.Width(70)))
                    {
                        if (!_reference.ExtractFrame(out string error))
                            EditorUtility.DisplayDialog("Kare alınamadı", error, "Tamam");
                    }
                }
            }
        }

        // ---------------- bölüm tarayıcısı ----------------

        void DrawBrowserSection()
        {
            _showBrowser = EditorGUILayout.Foldout(_showBrowser, "Bölümler", true);
            if (!_showBrowser) return;

            using (var scroll = new EditorGUILayout.ScrollViewScope(_browserScroll, GUILayout.Height(110)))
            {
                _browserScroll = scroll.scrollPosition;
                foreach (var path in LevelPaths())
                {
                    bool current = path == _path;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Toggle(current, System.IO.Path.GetFileNameWithoutExtension(path),
                                EditorStyles.miniButton) && !current)
                        {
                            if (ConfirmDiscard()) LoadFrom(path);
                        }
                    }
                }
            }
        }

        // ---------------- rapor & çözüm ----------------

        void DrawReportSection()
        {
            _showMetrics = EditorGUILayout.Foldout(_showMetrics, "Doğrulama & Tasarım", true);
            if (!_showMetrics || _report == null) return;

            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = _report.Ok
                ? new Color(0.4f, 0.85f, 0.45f) : new Color(1f, 0.5f, 0.4f);

            var solution = _report.Solution;
            EditorGUILayout.LabelField(_report.Ok
                ? $"✓ Oynanabilir — {solution.Moves.Count} hamle"
                : "⚠ Sorunlu bölüm", style);

            if (_report.Ok && solution != null)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"Açılış seçeneği: {solution.InitialOptions}   " +
                        $"Zorunlu hamle: {solution.ForcedSteps}/{solution.Moves.Count}",
                        EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(
                        $"Ortalama seçenek: {solution.AverageOptions:0.0}   " +
                        $"Tahmini süre: ~{_report.EstimatedSeconds} sn (verilen {_data.TimeSeconds})",
                        EditorStyles.miniLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _showSolution = EditorGUILayout.ToggleLeft("Çözümü göster", _showSolution,
                            GUILayout.Width(110));
                        using (new EditorGUI.DisabledScope(!_showSolution))
                        {
                            if (GUILayout.Button("◀", GUILayout.Width(26)))
                                _playbackStep = Mathf.Max(-1, _playbackStep - 1);
                            if (GUILayout.Button("▶", GUILayout.Width(26)))
                                _playbackStep = Mathf.Min(solution.Moves.Count - 1, _playbackStep + 1);
                            _playbackStep = Mathf.RoundToInt(GUILayout.HorizontalSlider(
                                _playbackStep, -1, solution.Moves.Count - 1));
                            GUILayout.Label(_playbackStep < 0 ? "tümü" : $"{_playbackStep + 1}",
                                EditorStyles.miniLabel, GUILayout.Width(34));
                        }
                    }

                    if (_showSolution && _playbackStep >= 0 && _playbackStep < solution.Moves.Count)
                    {
                        var move = solution.Moves[_playbackStep];
                        EditorGUILayout.LabelField(
                            $"{_playbackStep + 1}. {move.Color} → ({move.X},{move.Y}) " +
                            $"{move.Outcome} · o anda {move.Options} seçenek",
                            EditorStyles.miniLabel);
                    }
                }

                DrawColorSummary();
            }

            var messages = new List<string>(_report.AllMessages);
            if (messages.Count == 0) return;

            using (var scroll = new EditorGUILayout.ScrollViewScope(_reportScroll, GUILayout.Height(90)))
            {
                _reportScroll = scroll.scrollPosition;
                foreach (var line in messages)
                    EditorGUILayout.LabelField("• " + line, EditorStyles.wordWrappedMiniLabel);
            }
        }

        void DrawColorSummary()
        {
            if (_report.BlockCounts.Count == 0) return;

            EditorGUILayout.LabelField("Renk dağılımı", EditorStyles.miniBoldLabel);
            foreach (var pair in _report.BlockCounts)
            {
                _report.GateCounts.TryGetValue(pair.Key, out int gates);
                using (new EditorGUILayout.HorizontalScope())
                {
                    var swatch = GUILayoutUtility.GetRect(16, 14, GUILayout.Width(16), GUILayout.Height(14));
                    LevelCanvasDrawer.Fill(swatch, ColorOf(pair.Key));
                    EditorGUILayout.LabelField(
                        $"{pair.Key}: {pair.Value} katman · {gates} kapı" + (gates == 0 ? "  ⚠" : ""),
                        EditorStyles.miniLabel);
                }
            }
        }
    }
}
