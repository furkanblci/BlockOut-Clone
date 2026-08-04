using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Editor.ProjectSetup;
using BlockOut.Runtime.Config;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Bölüm tasarım aracı — JSON'u elle yazmayı bitirir.
    ///
    /// DERS (iç araçlar): Stüdyolarda içerik üretim hızını belirleyen şey
    /// motorun kendisi değil, tasarımcının kullandığı araçtır. İyi bir editörün
    /// üç şartı vardır: (1) geri alınabilir olmalı, (2) seçilen nesne
    /// düzenlenebilmeli, (3) hata anında görünmeli. Üçü de burada var.
    ///
    /// Araç, oyunun yüklediği <see cref="LevelData"/> DTO'larını doğrudan
    /// düzenler — ayrı bir editör modeli YOKTUR.
    /// </summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        enum Tool { Select, Shape, Blocks, Gates, Walls, Curtain }
        enum SelKind { None, Block, Gate, Curtain, Content }

        [System.Serializable]
        struct Selection
        {
            public SelKind Kind;
            public int Index;   // blok / kapı / perde dizini
            public int Sub;     // perde içeriği dizini
            public bool IsNone => Kind == SelKind.None;
            public static Selection None => new Selection { Kind = SelKind.None };
        }

        static readonly (string label, string tip)[] Tools =
        {
            ("Seç",      "Nesneyi seç, sürükleyerek taşı, yan panelden düzenle (1)"),
            ("Şekil",    "Hücreleri aç/kapa — sürükleyerek boya (2)"),
            ("Blok",     "Blok yerleştir; perde içine koyarsan gizli içerik olur (3)"),
            ("Kapı",     "En yakın kenara kapı koy (4)"),
            ("Duvar",    "İç duvar çiz — sürükleyerek uzat (5)"),
            ("Perde",    "Sürükleyerek perde bölgesi seç (6)")
        };

        static readonly string[] Difficulties = { "normal", "hard", "superhard" };
        static readonly Vector2Int[] CommonSizes =
        {
            new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(1,2), new Vector2Int(2,2),
            new Vector2Int(3,1), new Vector2Int(1,3), new Vector2Int(3,2), new Vector2Int(2,3),
            new Vector2Int(3,3), new Vector2Int(4,1), new Vector2Int(1,4), new Vector2Int(4,2)
        };

        // ---- domain reload'ı aşan durum ----
        // DERS: EditorWindow'un düz C# alanları Play'e girerken (domain reload)
        // SIFIRLANIR. Düzenlenen bölümü kaybetmemek için durumu [SerializeField]
        // bir JSON dizesine yazıp geri okuyoruz. Bu, "Play Test'e bastım, işim
        // uçtu" hatasının kalıcı çözümü.
        [SerializeField] string _serializedData;
        [SerializeField] string _path;
        [SerializeField] Tool _tool = Tool.Select;
        [SerializeField] List<string> _undoStack = new List<string>();
        [SerializeField] List<string> _redoStack = new List<string>();
        [SerializeField] Selection _selection = Selection.None;
        [SerializeField] bool _autoValidate = true;
        [SerializeField] int _blockW = 1, _blockH = 1, _blockIce;
        [SerializeField] List<BlockColor> _layers = new List<BlockColor> { BlockColor.Red };
        [SerializeField] int _activeLayer;
        [SerializeField] BlockColor _gateColor = BlockColor.Red;
        [SerializeField] int _gateLength = 2, _gateIce;
        [SerializeField] int _curtainCount = 3;

        LevelData _data;
        bool _dirty;
        readonly LevelCanvasDrawer _canvas = new LevelCanvasDrawer();
        ColorPaletteSO _palette;
        GameConfigSO _config;

        readonly List<string> _report = new List<string>();
        bool _reportOk;
        int _reportMoves;
        Vector2 _reportScroll;
        bool _validationStale = true;

        Vector2Int? _regionStart;
        Vector2Int _dragGrabOffset;
        bool _movingSelection;
        bool _moveRecorded;
        string _preMoveSnapshot;
        bool _shapePaintValue;
        readonly HashSet<EdgeId> _strokeEdges = new HashSet<EdgeId>();
        readonly HashSet<Vector2Int> _problemCells = new HashSet<Vector2Int>();

        /// <summary>Açık bölümün kimliği (pencere başlığı ve araçlar için).</summary>
        public string CurrentLevelId => _data?.Id;

        [MenuItem("Tools/Block Out/Level Editör")]
        public static LevelEditorWindow Open()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editör");
            window.minSize = new Vector2(900, 600);
            return window;
        }

        /// <summary>Belirtilen bölümü editörde açar.</summary>
        public static LevelEditorWindow OpenLevel(string assetPath)
        {
            var window = Open();
            window.LoadFrom(assetPath);
            return window;
        }

        /// <summary>
        /// Project penceresinde bir level JSON'una çift tıklamak editörü açar.
        /// DERS: [OnOpenAsset], Unity'nin "bu dosyayı benim aracım açsın" kancası —
        /// tasarımcı dosyayı metin editöründe açıp bozmak yerine doğrudan araca düşer.
        /// </summary>
        [UnityEditor.Callbacks.OnOpenAsset]
        static bool OnOpenLevelAsset(int instanceId, int line)
        {
            string path = AssetDatabase.GetAssetPath(instanceId);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".json")) return false;
            if (!path.Replace('\\', '/').StartsWith(LevelEditorIO.LevelDir)) return false;

            OpenLevel(path);
            return true; // olayı tükettik; Unity dosyayı metin editöründe açmasın
        }

        void OnEnable()
        {
            wantsMouseMove = true;
            _palette = AssetDatabase.LoadAssetAtPath<ColorPaletteSO>(
                "Assets/_Project/ScriptableObjects/ColorPalette.asset");
            _config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(
                "Assets/_Project/ScriptableObjects/GameConfig.asset");

            if (!string.IsNullOrEmpty(_serializedData))
            {
                try { _data = LevelEditorIO.FromJson(_serializedData); }
                catch { _data = null; }
            }
            _data ??= LevelEditorIO.NewLevel();
        }

        void OnDisable() => StashState();

        void StashState()
        {
            if (_data != null) _serializedData = LevelEditorIO.ToJson(_data);
        }

        void OnGUI()
        {
            HandleShortcuts();
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSidePanel();
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawCanvas();
                    DrawStatusBar();
                }
            }

            if (_autoValidate && _validationStale && Event.current.type == EventType.Repaint)
                RunValidation();

            if (Event.current.type == EventType.MouseMove) Repaint();
        }

        // ---------------- geri alma ----------------

        void Record()
        {
            _undoStack.Add(LevelEditorIO.ToJson(_data));
            if (_undoStack.Count > 60) _undoStack.RemoveAt(0);
            _redoStack.Clear();
        }

        void Undo()
        {
            if (_undoStack.Count == 0) return;
            _redoStack.Add(LevelEditorIO.ToJson(_data));
            _data = LevelEditorIO.FromJson(_undoStack[_undoStack.Count - 1]);
            _undoStack.RemoveAt(_undoStack.Count - 1);
            AfterChange();
        }

        void Redo()
        {
            if (_redoStack.Count == 0) return;
            _undoStack.Add(LevelEditorIO.ToJson(_data));
            _data = LevelEditorIO.FromJson(_redoStack[_redoStack.Count - 1]);
            _redoStack.RemoveAt(_redoStack.Count - 1);
            AfterChange();
        }

        void AfterChange()
        {
            _dirty = true;
            _validationStale = true;
            StashState();
            Repaint();
        }

        void HandleShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            bool ctrl = e.control || e.command;
            if (ctrl && e.keyCode == KeyCode.Z) { if (e.shift) Redo(); else Undo(); e.Use(); return; }
            if (ctrl && e.keyCode == KeyCode.Y) { Redo(); e.Use(); return; }
            if (ctrl && e.keyCode == KeyCode.S) { if (_path != null) SaveTo(_path); e.Use(); return; }
            if (ctrl && e.keyCode == KeyCode.D) { DuplicateSelection(); e.Use(); return; }
            if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
            { DeleteSelection(); e.Use(); return; }

            if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha6)
            {
                _tool = (Tool)(e.keyCode - KeyCode.Alpha1);
                e.Use();
            }
        }

        // ---------------- üst şerit ----------------

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Yeni", EditorStyles.toolbarButton, GUILayout.Width(52)))
                {
                    Record();
                    _data = LevelEditorIO.NewLevel();
                    _path = null; _selection = Selection.None;
                    AfterChange(); _dirty = false;
                }

                if (EditorGUILayout.DropdownButton(new GUIContent("Aç"),
                        FocusType.Passive, EditorStyles.toolbarDropDown, GUILayout.Width(52)))
                    ShowLevelMenu();

                using (new EditorGUI.DisabledScope(_path == null))
                    if (GUILayout.Button(new GUIContent("Kaydet", "Ctrl+S"),
                            EditorStyles.toolbarButton, GUILayout.Width(62)))
                        SaveTo(_path);

                if (GUILayout.Button("Farklı Kaydet", EditorStyles.toolbarButton, GUILayout.Width(94)))
                {
                    string path = LevelEditorIO.AskSavePath(_data.Id);
                    if (path != null) SaveTo(path);
                }

                GUILayout.Space(8);
                using (new EditorGUI.DisabledScope(_undoStack.Count == 0))
                    if (GUILayout.Button(new GUIContent("↶", "Geri al (Ctrl+Z)"),
                            EditorStyles.toolbarButton, GUILayout.Width(28))) Undo();
                using (new EditorGUI.DisabledScope(_redoStack.Count == 0))
                    if (GUILayout.Button(new GUIContent("↷", "İleri al (Ctrl+Y)"),
                            EditorStyles.toolbarButton, GUILayout.Width(28))) Redo();

                GUILayout.Space(8);
                if (GUILayout.Button("Doğrula", EditorStyles.toolbarButton, GUILayout.Width(62)))
                    RunValidation();
                _autoValidate = GUILayout.Toggle(_autoValidate,
                    new GUIContent("Otomatik", "Her değişiklikten sonra doğrula"),
                    EditorStyles.toolbarButton, GUILayout.Width(64));

                if (GUILayout.Button(new GUIContent("▶ Play Test", "Bu bölümü hemen oyna"),
                        EditorStyles.toolbarButton, GUILayout.Width(84)))
                {
                    StashState();
                    LevelEditorIO.PlayTest(_data);
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(_path == null ? "(kaydedilmemiş)" : System.IO.Path.GetFileName(_path) +
                    (_dirty ? " •" : ""), EditorStyles.miniLabel);
            }
        }

        /// <summary>Levels klasöründeki bölümleri listeleyen hızlı açma menüsü.</summary>
        void ShowLevelMenu()
        {
            var menu = new GenericMenu();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LevelEditorIO.LevelDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".json") || path.Contains("__playtest")) continue;
                menu.AddItem(new GUIContent(System.IO.Path.GetFileName(path)), path == _path,
                    () => LoadFrom(path));
            }
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Dosyadan aç…"), false, () =>
            {
                string path = LevelEditorIO.AskLoadPath();
                if (path != null) LoadFrom(path);
            });
            menu.ShowAsContext();
        }

        void LoadFrom(string path)
        {
            try
            {
                _data = LevelEditorIO.FromJson(System.IO.File.ReadAllText(path));
                _path = path;
                _selection = Selection.None;
                _undoStack.Clear(); _redoStack.Clear();
                AfterChange(); _dirty = false;
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Açılamadı", e.Message, "Tamam");
            }
        }

        void SaveTo(string path)
        {
            LevelEditorIO.Save(_data, path);
            _path = path; _dirty = false;
            ShowNotification(new GUIContent("Kaydedildi"));
        }

        // ---------------- yan panel ----------------

        void DrawSidePanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
            {
                DrawToolButtons();
                EditorGUILayout.Space(6);

                if (_tool == Tool.Select && !_selection.IsNone) DrawSelectionInspector();
                else DrawToolOptions();

                EditorGUILayout.Space(8);
                DrawLevelSettings();
                EditorGUILayout.Space(8);
                DrawReport();
            }
        }

        void DrawToolButtons()
        {
            EditorGUILayout.LabelField("Araçlar", EditorStyles.boldLabel);
            for (int row = 0; row < 2; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = row * 3; i < Mathf.Min(row * 3 + 3, Tools.Length); i++)
                    {
                        bool on = (int)_tool == i;
                        var content = new GUIContent(Tools[i].label, Tools[i].tip);
                        if (GUILayout.Toggle(on, content, EditorStyles.miniButton, GUILayout.Height(26)) && !on)
                        {
                            _tool = (Tool)i;
                            if (_tool != Tool.Select) _selection = Selection.None;
                        }
                    }
                }
            }
            EditorGUILayout.LabelField(Tools[(int)_tool].tip, EditorStyles.wordWrappedMiniLabel);
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
                        _blockW = Mathf.Clamp(EditorGUILayout.IntField(_blockW, GUILayout.Width(34)), 1, 6);
                        EditorGUILayout.LabelField("×", GUILayout.Width(12));
                        _blockH = Mathf.Clamp(EditorGUILayout.IntField(_blockH, GUILayout.Width(34)), 1, 6);
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField("Buz", GUILayout.Width(26));
                        _blockIce = Mathf.Max(0, EditorGUILayout.IntField(_blockIce, GUILayout.Width(34)));
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Katmanlar (dıştan içe)", EditorStyles.boldLabel);
                    DrawLayerChips();
                    DrawColorGrid(_layers[Mathf.Clamp(_activeLayer, 0, _layers.Count - 1)],
                        c => _layers[Mathf.Clamp(_activeLayer, 0, _layers.Count - 1)] = c);
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
                        "Perde koyduktan sonra Blok aracıyla içine blok yerleştir — " +
                        "gizli içerik olurlar.", MessageType.Info);
                    break;

                case Tool.Select:
                    EditorGUILayout.HelpBox(
                        "Bir nesneye tıkla: özellikleri burada açılır. Sürükleyerek taşı, " +
                        "Delete ile sil, Ctrl+D ile çoğalt.", MessageType.Info);
                    break;
            }
        }

        /// <summary>Görsel şekil paleti — sayı girmek yerine şekle tıklanır.</summary>
        void DrawSizePalette()
        {
            const int perRow = 6;
            for (int i = 0; i < CommonSizes.Length; i++)
            {
                if (i % perRow == 0) EditorGUILayout.BeginHorizontal();

                var size = CommonSizes[i];
                bool selected = _blockW == size.x && _blockH == size.y;
                var rect = GUILayoutUtility.GetRect(42, 42, GUILayout.Width(42), GUILayout.Height(42));

                if (GUI.Button(rect, new GUIContent("", $"{size.x}×{size.y}")))
                {
                    _blockW = size.x; _blockH = size.y;
                }

                float unit = Mathf.Min(30f / Mathf.Max(size.x, size.y), 9f);
                var shape = new Rect(
                    rect.center.x - size.x * unit * 0.5f,
                    rect.center.y - size.y * unit * 0.5f,
                    size.x * unit, size.y * unit);
                LevelCanvasDrawer.Fill(shape, ColorOf(_layers[0]));
                if (selected) LevelCanvasDrawer.Outline(rect, Color.white, 2f);

                if (i % perRow == perRow - 1 || i == CommonSizes.Length - 1)
                    EditorGUILayout.EndHorizontal();
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

        void DrawSelectionInspector()
        {
            EditorGUILayout.LabelField("Seçili Nesne", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            switch (_selection.Kind)
            {
                case SelKind.Block:
                case SelKind.Content:
                {
                    var block = SelectedBlock();
                    if (block == null) { _selection = Selection.None; return; }

                    EditorGUILayout.LabelField(_selection.Kind == SelKind.Content
                        ? "Perde içeriği (gizli blok)" : "Blok", EditorStyles.miniLabel);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Boyut", GUILayout.Width(42));
                        block.W = Mathf.Clamp(EditorGUILayout.IntField(block.W, GUILayout.Width(34)), 1, 6);
                        EditorGUILayout.LabelField("×", GUILayout.Width(12));
                        block.H = Mathf.Clamp(EditorGUILayout.IntField(block.H, GUILayout.Width(34)), 1, 6);
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
                            BlockColorUtil.TryParse(block.Layers[i], out var lc);
                            LevelCanvasDrawer.Fill(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), ColorOf(lc));
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
                    DrawColorGrid(currentColor, c => { block.Layers[layerIndex] = c.ToId(); AfterChange(); });

                    // Perde içeriği JSON'dan KOPYA olarak okunur; düzenlemenin
                    // kalıcı olması için perdeye geri yazılmalı.
                    CommitContentEdit(block);
                    break;
                }

                case SelKind.Gate:
                {
                    if (_selection.Index >= _data.Gates.Count) { _selection = Selection.None; return; }
                    var gate = _data.Gates[_selection.Index];
                    EditorGUILayout.LabelField($"Kapı — {gate.Side} kenarı", EditorStyles.miniLabel);
                    gate.Length = EditorGUILayout.IntSlider("Uzunluk", gate.Length, 1, 5);
                    gate.Ice = Mathf.Max(0, EditorGUILayout.IntField("Buz kaplaması", gate.Ice));
                    BlockColorUtil.TryParse(gate.Colors.Count > 0 ? gate.Colors[0] : "red", out var gc);
                    DrawColorGrid(gc, c =>
                    {
                        if (gate.Colors.Count == 0) gate.Colors.Add(c.ToId());
                        else gate.Colors[0] = c.ToId();
                        AfterChange();
                    });
                    break;
                }

                case SelKind.Curtain:
                {
                    if (_selection.Index >= _data.Obstacles.Count) { _selection = Selection.None; return; }
                    var curtain = _data.Obstacles[_selection.Index];
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
                if (GUILayout.Button("Çoğalt (Ctrl+D)")) DuplicateSelection();
                if (GUILayout.Button("Sil (Del)")) DeleteSelection();
            }
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

        void DrawReport()
        {
            if (_report.Count == 0 && !_reportOk) return;

            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = _reportOk ? new Color(0.4f, 0.85f, 0.45f) : new Color(1f, 0.5f, 0.4f);
            EditorGUILayout.LabelField(_reportOk
                ? $"✓ Oynanabilir — {_reportMoves} hamle"
                : "⚠ Sorunlu bölüm", style);

            if (_report.Count == 0) return;
            using (var scroll = new EditorGUILayout.ScrollViewScope(_reportScroll, GUILayout.Height(90)))
            {
                _reportScroll = scroll.scrollPosition;
                foreach (var line in _report)
                    EditorGUILayout.LabelField("• " + line, EditorStyles.wordWrappedMiniLabel);
            }
        }

        void RunValidation()
        {
            _validationStale = false;
            _report.Clear();
            _problemCells.Clear();

            if (_palette == null || _config == null)
            {
                _reportOk = false;
                _report.Add("ColorPalette / GameConfig asset'i bulunamadı.");
                return;
            }

            _reportOk = LevelValidationTool.ValidateData(_data, _palette, _config, _report, out _reportMoves);
            CollectProblemCells();
        }

        /// <summary>Tuvalde kırmızı gösterilecek sorunlu hücreler (çakışma / taşma).</summary>
        void CollectProblemCells()
        {
            var seen = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < _data.Blocks.Count; i++)
            {
                var b = _data.Blocks[i];
                for (int x = b.X; x < b.X + b.W; x++)
                    for (int y = b.Y; y < b.Y + b.H; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (!Playable(x, y)) _problemCells.Add(cell);
                        else if (seen.ContainsKey(cell)) _problemCells.Add(cell);
                        else seen[cell] = i;
                    }
            }

            foreach (var gate in _data.Gates)
            {
                if (!SideUtil.TryParse(gate.Side, out var side)) continue;
                bool horizontal = side == Side.North || side == Side.South;
                for (int j = 0; j < gate.Length; j++)
                {
                    int cx = horizontal ? gate.X + j : gate.X;
                    int cy = horizontal ? gate.Y : gate.Y + j;
                    if (!Playable(cx, cy)) _problemCells.Add(new Vector2Int(cx, cy));
                }
            }
        }

        void ResizeBoard(int width, int height)
        {
            var rows = new List<string>(height);
            for (int y = 0; y < height; y++)
            {
                string old = y < _data.Board.Rows.Count ? _data.Board.Rows[y] : "";
                var sb = new System.Text.StringBuilder(width);
                for (int x = 0; x < width; x++) sb.Append(x < old.Length ? old[x] : 'X');
                rows.Add(sb.ToString());
            }
            _data.Board.Rows = rows;
            _data.Board.Width = width;
            _data.Board.Height = height;
        }

        // ---------------- tuval ----------------

        void DrawCanvas()
        {
            var area = GUILayoutUtility.GetRect(200, 200,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(area, new Color(0.13f, 0.11f, 0.20f));

            _canvas.Layout(area, _data.Board.Width, _data.Board.Height);

            DrawCells();
            DrawWalls();
            DrawCurtains();
            DrawBlocks();
            DrawGates();
            DrawProblems();
            DrawSelectionOutline();
            DrawHover();

            HandleInput(area);
        }

        bool Playable(int x, int y) =>
            x >= 0 && y >= 0 && x < _data.Board.Width && y < _data.Board.Height &&
            char.ToUpperInvariant(_data.Board.Rows[y][x]) == 'X';

        void DrawCells()
        {
            var light = new Color(0.80f, 0.78f, 0.90f);
            var dark = new Color(0.72f, 0.70f, 0.85f);
            var empty = new Color(0.17f, 0.15f, 0.25f);
            for (int y = 0; y < _data.Board.Height; y++)
                for (int x = 0; x < _data.Board.Width; x++)
                    LevelCanvasDrawer.Fill(_canvas.RectFor(x, y),
                        !Playable(x, y) ? empty : (x + y) % 2 == 0 ? light : dark);
        }

        void DrawWalls()
        {
            var color = new Color(0.33f, 0.28f, 0.52f);
            foreach (var wall in _data.Board.Walls)
            {
                if (!SideUtil.TryParse(wall.Side, out var side)) continue;
                var first = EdgeId.OfCellSide(wall.X, wall.Y, side);
                for (int i = 0; i < Mathf.Max(1, wall.Length); i++)
                    LevelCanvasDrawer.Fill(_canvas.EdgeRect(first.Horizontal
                        ? EdgeId.OfCellSide(wall.X + i, wall.Y, side)
                        : EdgeId.OfCellSide(wall.X, wall.Y + i, side)), color);
            }
        }

        void DrawBlocks()
        {
            foreach (var block in _data.Blocks) DrawBlock(block, 1f);
        }

        void DrawBlock(BlockData block, float alpha)
        {
            var rect = _canvas.RectFor(block.X, block.Y, block.W, block.H);
            rect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);

            var color = ColorOf(block.Layers.Count > 0 ? block.Layers[0] : "red");
            color.a = alpha;
            LevelCanvasDrawer.Fill(rect, color);

            if (block.Layers.Count > 1)
            {
                float inset = Mathf.Min(rect.width, rect.height) * 0.26f;
                var inner = new Rect(rect.x + inset, rect.y + inset,
                    rect.width - inset * 2f, rect.height - inset * 2f);
                var innerColor = ColorOf(block.Layers[1]);
                innerColor.a = alpha;
                LevelCanvasDrawer.Fill(inner, innerColor);
            }

            if (block.Ice > 0)
            {
                LevelCanvasDrawer.Fill(rect, new Color(0.62f, 0.85f, 1f, 0.55f * alpha));
                LevelCanvasDrawer.Label(rect, block.Ice.ToString(),
                    new Color(0.1f, 0.2f, 0.4f, alpha), Mathf.RoundToInt(_canvas.CellSize * 0.32f));
            }
        }

        void DrawGates()
        {
            foreach (var gate in _data.Gates)
            {
                if (!SideUtil.TryParse(gate.Side, out var side)) continue;
                var rect = _canvas.GateRect(gate.X, gate.Y, side, gate.Length);
                if (gate.Ice > 0)
                {
                    LevelCanvasDrawer.Fill(rect, new Color(0.75f, 0.9f, 1f, 0.92f));
                    LevelCanvasDrawer.Label(rect, gate.Ice.ToString(),
                        new Color(0.1f, 0.2f, 0.4f), Mathf.RoundToInt(_canvas.CellSize * 0.28f));
                }
                else
                {
                    LevelCanvasDrawer.Fill(rect, ColorOf(gate.Colors.Count > 0 ? gate.Colors[0] : "red"));
                }
            }
        }

        void DrawCurtains()
        {
            foreach (var obstacle in _data.Obstacles)
            {
                if (obstacle.Type != "curtain") continue;
                var rect = CurtainRect(obstacle);

                foreach (var hidden in LevelEditorIO.GetContents(obstacle))
                    DrawBlock(hidden, 0.4f);

                LevelCanvasDrawer.Fill(rect, new Color(0.22f, 0.16f, 0.38f, 0.8f));
                LevelCanvasDrawer.Outline(rect, new Color(0.85f, 0.65f, 0.2f), 3f);
                LevelCanvasDrawer.Label(rect, LevelEditorIO.GetInt(obstacle, "count").ToString(),
                    new Color(1f, 0.9f, 0.55f), Mathf.RoundToInt(_canvas.CellSize * 0.38f));
            }
        }

        void DrawProblems()
        {
            foreach (var cell in _problemCells)
                LevelCanvasDrawer.Fill(_canvas.RectFor(cell.x, cell.y), new Color(1f, 0.2f, 0.2f, 0.45f));
        }

        void DrawSelectionOutline()
        {
            if (_selection.IsNone) return;
            var accent = new Color(1f, 0.85f, 0.2f);

            switch (_selection.Kind)
            {
                case SelKind.Block:
                case SelKind.Content:
                {
                    var block = SelectedBlock();
                    if (block != null)
                        LevelCanvasDrawer.Highlight(
                            _canvas.RectFor(block.X, block.Y, block.W, block.H), accent);
                    break;
                }
                case SelKind.Gate:
                    if (_selection.Index < _data.Gates.Count)
                    {
                        var gate = _data.Gates[_selection.Index];
                        if (SideUtil.TryParse(gate.Side, out var side))
                            LevelCanvasDrawer.Highlight(
                                _canvas.GateRect(gate.X, gate.Y, side, gate.Length), accent);
                    }
                    break;
                case SelKind.Curtain:
                    if (_selection.Index < _data.Obstacles.Count)
                        LevelCanvasDrawer.Highlight(CurtainRect(_data.Obstacles[_selection.Index]), accent);
                    break;
            }
        }

        void DrawHover()
        {
            var mouse = Event.current.mousePosition;
            if (!_canvas.TryCell(mouse, _data.Board.Width, _data.Board.Height, out var cell)) return;

            if (_tool == Tool.Gates || _tool == Tool.Walls)
            {
                if (_canvas.TryEdge(mouse, _data.Board.Width, _data.Board.Height, out var c, out var side))
                    LevelCanvasDrawer.Fill(_tool == Tool.Gates
                        ? _canvas.GateRect(c.x, c.y, side, _gateLength)
                        : _canvas.EdgeRect(EdgeId.OfCellSide(c.x, c.y, side)),
                        new Color(1f, 1f, 1f, 0.5f));
                return;
            }

            if (_tool == Tool.Blocks)
            {
                var preview = _canvas.RectFor(cell.x, cell.y, _blockW, _blockH);
                var color = ColorOf(_layers[0]); color.a = 0.45f;
                LevelCanvasDrawer.Fill(preview, color);
                LevelCanvasDrawer.Outline(preview, Color.white, 2f);
                return;
            }

            if ((_tool == Tool.Curtain || _tool == Tool.Shape) && _regionStart.HasValue)
            {
                var r = RegionRect(_regionStart.Value, cell);
                LevelCanvasDrawer.Outline(_canvas.RectFor(r.x, r.y, r.width, r.height),
                    new Color(0.85f, 0.65f, 0.2f), 2f);
                return;
            }

            LevelCanvasDrawer.Outline(_canvas.RectFor(cell.x, cell.y), new Color(1f, 1f, 1f, 0.7f), 2f);
        }

        void DrawStatusBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                string cellText = _canvas.TryCell(Event.current.mousePosition,
                    _data.Board.Width, _data.Board.Height, out var cell)
                    ? $"hücre ({cell.x},{cell.y})" : "—";
                GUILayout.Label(cellText, EditorStyles.miniLabel, GUILayout.Width(110));
                GUILayout.Label($"{_data.Blocks.Count} blok · {_data.Gates.Count} kapı · " +
                                $"{_data.Obstacles.Count} engel", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label("1-6: araç · Ctrl+Z: geri · Del: sil · Ctrl+D: çoğalt",
                    EditorStyles.miniLabel);
            }
        }

        Rect CurtainRect(ObstacleData curtain) => _canvas.RectFor(
            LevelEditorIO.GetInt(curtain, "x"), LevelEditorIO.GetInt(curtain, "y"),
            LevelEditorIO.GetInt(curtain, "w", 1), LevelEditorIO.GetInt(curtain, "h", 1));

        static RectInt RegionRect(Vector2Int a, Vector2Int b) => new RectInt(
            Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
            Mathf.Abs(a.x - b.x) + 1, Mathf.Abs(a.y - b.y) + 1);

        Color ColorOf(string id) =>
            BlockColorUtil.TryParse(id, out var color) ? ColorOf(color) : Color.magenta;

        Color ColorOf(BlockColor color)
        {
            var entry = _palette != null ? _palette.Get(color) : null;
            return entry != null ? entry.uiColor : Color.magenta;
        }

        // ---------------- seçim ----------------

        BlockData SelectedBlock()
        {
            if (_selection.Kind == SelKind.Block)
                return _selection.Index < _data.Blocks.Count ? _data.Blocks[_selection.Index] : null;

            if (_selection.Kind == SelKind.Content && _selection.Index < _data.Obstacles.Count)
            {
                var contents = LevelEditorIO.GetContents(_data.Obstacles[_selection.Index]);
                return _selection.Sub < contents.Count ? contents[_selection.Sub] : null;
            }
            return null;
        }

        /// <summary>Perde içeriği referans değil kopya olarak döndüğü için yazma geri işlenir.</summary>
        void CommitContentEdit(BlockData edited)
        {
            if (_selection.Kind != SelKind.Content) return;
            var curtain = _data.Obstacles[_selection.Index];
            var contents = LevelEditorIO.GetContents(curtain);
            if (_selection.Sub < contents.Count) contents[_selection.Sub] = edited;
            LevelEditorIO.SetContents(curtain, contents);
        }

        bool TryPick(Vector2 mouse, out Selection selection)
        {
            selection = Selection.None;

            for (int i = 0; i < _data.Gates.Count; i++)
            {
                var gate = _data.Gates[i];
                if (!SideUtil.TryParse(gate.Side, out var side)) continue;
                if (_canvas.GateRect(gate.X, gate.Y, side, gate.Length).Contains(mouse))
                {
                    selection = new Selection { Kind = SelKind.Gate, Index = i };
                    return true;
                }
            }

            if (!_canvas.TryCell(mouse, _data.Board.Width, _data.Board.Height, out var cell))
                return false;

            for (int i = _data.Blocks.Count - 1; i >= 0; i--)
                if (Covers(_data.Blocks[i], cell))
                {
                    selection = new Selection { Kind = SelKind.Block, Index = i };
                    return true;
                }

            for (int i = 0; i < _data.Obstacles.Count; i++)
            {
                var obstacle = _data.Obstacles[i];
                if (obstacle.Type != "curtain") continue;
                if (!CurtainCovers(obstacle, cell)) continue;

                var contents = LevelEditorIO.GetContents(obstacle);
                for (int j = contents.Count - 1; j >= 0; j--)
                    if (Covers(contents[j], cell))
                    {
                        selection = new Selection { Kind = SelKind.Content, Index = i, Sub = j };
                        return true;
                    }

                selection = new Selection { Kind = SelKind.Curtain, Index = i };
                return true;
            }
            return false;
        }

        static bool Covers(BlockData block, Vector2Int cell) =>
            cell.x >= block.X && cell.x < block.X + block.W &&
            cell.y >= block.Y && cell.y < block.Y + block.H;

        bool CurtainCovers(ObstacleData curtain, Vector2Int cell)
        {
            int x = LevelEditorIO.GetInt(curtain, "x"), y = LevelEditorIO.GetInt(curtain, "y");
            int w = LevelEditorIO.GetInt(curtain, "w", 1), h = LevelEditorIO.GetInt(curtain, "h", 1);
            return cell.x >= x && cell.x < x + w && cell.y >= y && cell.y < y + h;
        }

        ObstacleData CurtainCovering(Vector2Int cell)
        {
            foreach (var obstacle in _data.Obstacles)
                if (obstacle.Type == "curtain" && CurtainCovers(obstacle, cell))
                    return obstacle;
            return null;
        }

        void DeleteSelection()
        {
            if (_selection.IsNone) return;
            Record();

            switch (_selection.Kind)
            {
                case SelKind.Block:
                    if (_selection.Index < _data.Blocks.Count) _data.Blocks.RemoveAt(_selection.Index);
                    break;
                case SelKind.Gate:
                    if (_selection.Index < _data.Gates.Count) _data.Gates.RemoveAt(_selection.Index);
                    break;
                case SelKind.Curtain:
                    if (_selection.Index < _data.Obstacles.Count) _data.Obstacles.RemoveAt(_selection.Index);
                    break;
                case SelKind.Content:
                {
                    var curtain = _data.Obstacles[_selection.Index];
                    var contents = LevelEditorIO.GetContents(curtain);
                    if (_selection.Sub < contents.Count) contents.RemoveAt(_selection.Sub);
                    LevelEditorIO.SetContents(curtain, contents);
                    break;
                }
            }
            _selection = Selection.None;
            AfterChange();
        }

        void DuplicateSelection()
        {
            var block = SelectedBlock();
            if (block == null) return;
            Record();

            var copy = new BlockData
            {
                X = Mathf.Min(block.X + 1, _data.Board.Width - block.W),
                Y = Mathf.Min(block.Y + 1, _data.Board.Height - block.H),
                W = block.W, H = block.H, Ice = block.Ice,
                Layers = new List<string>(block.Layers)
            };
            _data.Blocks.Add(copy);
            _selection = new Selection { Kind = SelKind.Block, Index = _data.Blocks.Count - 1 };
            AfterChange();
        }

        // ---------------- etkileşim ----------------

        void HandleInput(Rect area)
        {
            var e = Event.current;
            if (!area.Contains(e.mousePosition)) return;

            bool erase = e.button == 1;
            switch (e.type)
            {
                case EventType.MouseDown: OnMouseDown(e, erase); break;
                case EventType.MouseDrag: OnMouseDrag(e, erase); break;
                case EventType.MouseUp:   OnMouseUp(e); break;
            }
        }

        void OnMouseDown(Event e, bool erase)
        {
            _strokeEdges.Clear();

            switch (_tool)
            {
                case Tool.Select:
                {
                    if (TryPick(e.mousePosition, out var picked))
                    {
                        _selection = picked;
                        _movingSelection = true;
                        _moveRecorded = false; // geri alma kaydı ilk GERÇEK harekette alınır
                        _preMoveSnapshot = LevelEditorIO.ToJson(_data);

                        _canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell);
                        var block = SelectedBlock();
                        if (block != null)
                            _dragGrabOffset = new Vector2Int(block.X - cell.x, block.Y - cell.y);
                        else if (_selection.Kind == SelKind.Curtain)
                        {
                            var curtain = _data.Obstacles[_selection.Index];
                            _dragGrabOffset = new Vector2Int(
                                LevelEditorIO.GetInt(curtain, "x") - cell.x,
                                LevelEditorIO.GetInt(curtain, "y") - cell.y);
                        }
                    }
                    else _selection = Selection.None;
                    e.Use(); Repaint();
                    break;
                }

                case Tool.Shape:
                    Record();
                    PaintShape(e, true);
                    break;

                case Tool.Blocks:
                    if (erase) EraseAt(e);
                    else PlaceBlock(e);
                    break;

                case Tool.Gates:
                    PlaceGate(e, erase);
                    break;

                case Tool.Walls:
                    Record();
                    PaintWall(e, erase);
                    break;

                case Tool.Curtain:
                    if (erase) { EraseCurtain(e); }
                    else if (_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                    { _regionStart = cell; e.Use(); }
                    break;
            }
        }

        void OnMouseDrag(Event e, bool erase)
        {
            switch (_tool)
            {
                case Tool.Select:
                    if (_movingSelection) MoveSelection(e);
                    break;
                case Tool.Shape:
                    PaintShape(e, false);
                    break;
                case Tool.Walls:
                    PaintWall(e, erase); // sürükleyerek duvar dizisi çiz
                    break;
                case Tool.Curtain:
                    e.Use(); Repaint();
                    break;
            }
        }

        void OnMouseUp(Event e)
        {
            _movingSelection = false;

            if (_tool == Tool.Curtain && _regionStart.HasValue &&
                _canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
            {
                var region = RegionRect(_regionStart.Value, cell);
                _regionStart = null;
                Record();
                _data.Obstacles.Add(LevelEditorIO.NewCurtain(
                    region.x, region.y, region.width, region.height, _curtainCount));
                AfterChange();
                e.Use();
            }
            _regionStart = null;
        }

        void MoveSelection(Event e)
        {
            // Kapılar kenara oturur: taşırken imlecin altındaki kenar yeniden
            // hesaplanır, böylece kapı tahtanın çevresinde dolaştırılabilir.
            if (_selection.Kind == SelKind.Gate)
            {
                if (!_canvas.TryEdge(e.mousePosition, _data.Board.Width, _data.Board.Height,
                        out var gateCell, out var gateSide)) return;

                var gate = _data.Gates[_selection.Index];
                string sideId = gateSide.ToId();
                if (gate.X == gateCell.x && gate.Y == gateCell.y && gate.Side == sideId) return;

                EnsureMoveRecorded();
                gate.X = gateCell.x; gate.Y = gateCell.y; gate.Side = sideId;
                AfterChange(); e.Use();
                return;
            }

            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;

            if (_selection.Kind == SelKind.Curtain)
            {
                var curtain = _data.Obstacles[_selection.Index];
                int w = LevelEditorIO.GetInt(curtain, "w", 1), h = LevelEditorIO.GetInt(curtain, "h", 1);
                int nx = Mathf.Clamp(cell.x + _dragGrabOffset.x, 0, _data.Board.Width - w);
                int ny = Mathf.Clamp(cell.y + _dragGrabOffset.y, 0, _data.Board.Height - h);
                if (nx == LevelEditorIO.GetInt(curtain, "x") && ny == LevelEditorIO.GetInt(curtain, "y"))
                    return;

                EnsureMoveRecorded();
                LevelEditorIO.SetInt(curtain, "x", nx);
                LevelEditorIO.SetInt(curtain, "y", ny);
                AfterChange(); e.Use();
                return;
            }

            var block = SelectedBlock();
            if (block == null) return;

            int bx = Mathf.Clamp(cell.x + _dragGrabOffset.x, 0, _data.Board.Width - block.W);
            int by = Mathf.Clamp(cell.y + _dragGrabOffset.y, 0, _data.Board.Height - block.H);
            if (bx == block.X && by == block.Y) return;

            EnsureMoveRecorded();
            block.X = bx; block.Y = by;
            CommitContentEdit(block);
            AfterChange(); e.Use();
        }

        /// <summary>Sürükleme boyunca TEK geri alma kaydı tutulur (her karede değil).</summary>
        void EnsureMoveRecorded()
        {
            if (_moveRecorded) return;
            _moveRecorded = true;

            // Kayıt, hareketten ÖNCEKİ hali içermeli: mevcut durumu geri sar.
            _undoStack.Add(_preMoveSnapshot ?? LevelEditorIO.ToJson(_data));
            if (_undoStack.Count > 60) _undoStack.RemoveAt(0);
            _redoStack.Clear();
        }

        void PaintShape(Event e, bool isFirst)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;

            // Sürükleme boyunca TEK değer boyanır; yoksa hücreler parmak
            // altında yanıp söner.
            if (isFirst) _shapePaintValue = !Playable(cell.x, cell.y);

            var row = _data.Board.Rows[cell.y].ToCharArray();
            char want = _shapePaintValue ? 'X' : '.';
            if (row[cell.x] == want) { e.Use(); return; }

            row[cell.x] = want;
            _data.Board.Rows[cell.y] = new string(row);
            AfterChange(); e.Use();
        }

        void PlaceBlock(Event e)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;
            Record();

            var block = new BlockData
            {
                X = Mathf.Clamp(cell.x, 0, _data.Board.Width - _blockW),
                Y = Mathf.Clamp(cell.y, 0, _data.Board.Height - _blockH),
                W = _blockW, H = _blockH, Ice = _blockIce
            };
            foreach (var layer in _layers) block.Layers.Add(layer.ToId());

            var curtain = CurtainCovering(cell);
            if (curtain != null)
            {
                var contents = LevelEditorIO.GetContents(curtain);
                contents.Add(block);
                LevelEditorIO.SetContents(curtain, contents);
            }
            else _data.Blocks.Add(block);

            AfterChange(); e.Use();
        }

        void EraseAt(Event e)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;

            for (int i = _data.Blocks.Count - 1; i >= 0; i--)
                if (Covers(_data.Blocks[i], cell))
                {
                    Record();
                    _data.Blocks.RemoveAt(i);
                    AfterChange(); e.Use();
                    return;
                }

            foreach (var obstacle in _data.Obstacles)
            {
                if (obstacle.Type != "curtain") continue;
                var contents = LevelEditorIO.GetContents(obstacle);
                for (int i = contents.Count - 1; i >= 0; i--)
                    if (Covers(contents[i], cell))
                    {
                        Record();
                        contents.RemoveAt(i);
                        LevelEditorIO.SetContents(obstacle, contents);
                        AfterChange(); e.Use();
                        return;
                    }
            }
        }

        void PlaceGate(Event e, bool erase)
        {
            if (!_canvas.TryEdge(e.mousePosition, _data.Board.Width, _data.Board.Height,
                    out var cell, out var side))
                return;

            string sideId = side.ToId();
            int existing = _data.Gates.FindIndex(g => g.X == cell.x && g.Y == cell.y && g.Side == sideId);
            Record();

            if (erase)
            {
                if (existing >= 0) { _data.Gates.RemoveAt(existing); AfterChange(); }
                e.Use();
                return;
            }

            if (existing >= 0) _data.Gates.RemoveAt(existing);
            var gate = new GateData
            {
                X = cell.x, Y = cell.y, Side = sideId, Length = _gateLength, Ice = _gateIce
            };
            gate.Colors.Add(_gateColor.ToId());
            _data.Gates.Add(gate);
            AfterChange(); e.Use();
        }

        void PaintWall(Event e, bool erase)
        {
            if (!_canvas.TryEdge(e.mousePosition, _data.Board.Width, _data.Board.Height,
                    out var cell, out var side))
                return;

            var target = EdgeId.OfCellSide(cell.x, cell.y, side);
            if (!_strokeEdges.Add(target)) { e.Use(); return; } // aynı kenarı bir kez işle

            int existing = _data.Board.Walls.FindIndex(w =>
                SideUtil.TryParse(w.Side, out var s) &&
                EdgeId.OfCellSide(w.X, w.Y, s).Equals(target));

            if (erase)
            {
                if (existing >= 0) { _data.Board.Walls.RemoveAt(existing); AfterChange(); }
            }
            else if (existing < 0)
            {
                _data.Board.Walls.Add(new WallData
                {
                    X = cell.x, Y = cell.y, Side = side.ToId(), Length = 1
                });
                AfterChange();
            }
            e.Use();
        }

        void EraseCurtain(Event e)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;
            var curtain = CurtainCovering(cell);
            if (curtain == null) return;

            Record();
            _data.Obstacles.Remove(curtain);
            AfterChange(); e.Use();
        }
    }
}
