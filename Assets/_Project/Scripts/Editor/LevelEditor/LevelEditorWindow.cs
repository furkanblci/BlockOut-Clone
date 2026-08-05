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
    /// şartları: geri alınabilir olmalı, seçilen nesne düzenlenebilmeli, hata
    /// anında görünmeli, işi asla kaybettirmemeli.
    ///
    /// Araç, oyunun yüklediği <see cref="LevelData"/> DTO'larını doğrudan
    /// düzenler — ayrı bir editör modeli YOKTUR.
    ///
    /// Sınıf üç parçaya bölünmüştür: bu dosya (durum, yaşam döngüsü, çizim),
    /// <c>.Panels</c> (yan panel arayüzü), <c>.Input</c> (etkileşim ve düzenleme).
    /// </summary>
    public sealed partial class LevelEditorWindow : EditorWindow
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

            public bool Same(Selection other) =>
                Kind == other.Kind && Index == other.Index && Sub == other.Sub;
        }

        static readonly (string label, string tip)[] ToolInfo =
        {
            ("Seç",   "Nesne seç ve sürükleyerek taşı · boş alanda sürükle = kutu seçim · Alt+sürükle = kopyala (1)"),
            ("Şekil", "Hücreleri aç/kapa — sürükleyerek boya (2)"),
            ("Blok",  "Blok yerleştir; perde içine koyarsan gizli içerik olur (3)"),
            ("Kapı",  "En yakın kenara kapı koy (4)"),
            ("Duvar", "İç duvar çiz — sürükleyerek uzat (5)"),
            ("Perde", "Sürükleyerek perde bölgesi seç (6)")
        };

        static readonly string[] Difficulties = { "normal", "hard", "superhard" };
        /// <summary>
        /// Şekil paleti. Her ön ayar bir hücre maskesidir ('X' dolu, '.' boş) —
        /// dikdörtgenler maskenin tamamen dolu olduğu özel hâl. Referans oyunda
        /// L ve T parçaları bol kullanıldığı için palette hazır dururlar.
        /// </summary>
        static readonly (string Label, string[] Rows)[] ShapePresets =
        {
            ("1×1",  new[] { "X" }),
            ("2×1",  new[] { "XX" }),
            ("1×2",  new[] { "X", "X" }),
            ("2×2",  new[] { "XX", "XX" }),
            ("3×1",  new[] { "XXX" }),
            ("1×3",  new[] { "X", "X", "X" }),
            ("3×2",  new[] { "XXX", "XXX" }),
            ("2×3",  new[] { "XX", "XX", "XX" }),
            ("3×3",  new[] { "XXX", "XXX", "XXX" }),
            ("L",    new[] { "X.", "XX" }),
            ("J",    new[] { ".X", "XX" }),
            ("L uzun", new[] { "X.", "X.", "XX" }),
            ("T",    new[] { "XXX", ".X." }),
            ("T ters", new[] { ".X.", "XXX" }),
            ("S",    new[] { ".XX", "XX." }),
            ("Z",    new[] { "XX.", ".XX" }),
            ("U",    new[] { "X.X", "XXX" }),
            ("Artı", new[] { ".X.", "XXX", ".X." })
        };

        // ---- domain reload'ı aşan durum ----
        // DERS: EditorWindow'un düz C# alanları Play'e girerken (domain reload)
        // SIFIRLANIR. Düzenlenen bölümü kaybetmemek için durumu [SerializeField]
        // bir JSON dizesine yazıp geri okuyoruz.
        [SerializeField] string _serializedData;
        [SerializeField] string _path;
        [SerializeField] Tool _tool = Tool.Select;
        [SerializeField] List<string> _undoStack = new List<string>();
        [SerializeField] List<string> _redoStack = new List<string>();
        [SerializeField] List<Selection> _selections = new List<Selection>();
        [SerializeField] bool _autoValidate = true;
        [SerializeField] int _blockW = 1, _blockH = 1, _blockIce;

        /// <summary>Fırçanın hücre maskesi; boş/null ise blok _blockW×_blockH dikdörtgen.</summary>
        [SerializeField] List<string> _blockMask;
        [SerializeField] List<BlockColor> _layers = new List<BlockColor> { BlockColor.Red };
        [SerializeField] int _activeLayer;
        [SerializeField] BlockColor _gateColor = BlockColor.Red;
        [SerializeField] int _gateLength = 2, _gateIce;
        [SerializeField] int _curtainCount = 3;
        [SerializeField] float _zoom = 1f;
        [SerializeField] Vector2 _pan;
        [SerializeField] LevelReferenceOverlay _reference = new LevelReferenceOverlay();
        [SerializeField] bool _showBrowser, _showReference, _showMetrics = true;
        [SerializeField] int _playbackStep = -1;
        [SerializeField] bool _showSolution;
        [SerializeField] string _stampName = "";

        LevelData _data;
        bool _dirty;
        readonly LevelCanvasDrawer _canvas = new LevelCanvasDrawer();
        ColorPaletteSO _palette;
        GameConfigSO _config;

        LevelReport _report;
        bool _validationStale = true;
        Vector2 _reportScroll, _browserScroll, _panelScroll;
        double _lastRecoveryWrite;
        bool _recoveryAvailable;

        Vector2Int? _regionStart;
        Vector2Int _dragGrabOffset;
        bool _movingSelection, _moveRecorded, _boxSelecting;
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
            window.minSize = new Vector2(980, 620);
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
        /// DERS: [OnOpenAsset], Unity'nin "bu dosyayı benim aracım açsın" kancası.
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

            if (_data == null)
            {
                _recoveryAvailable = LevelEditorIO.TryReadRecovery(out _);
                _data = LevelEditorIO.NewLevel();
            }

            _canvas.Zoom = _zoom;
            _canvas.Pan = _pan;
        }

        void OnDisable() => StashState();

        void StashState()
        {
            if (_data == null) return;
            _serializedData = LevelEditorIO.ToJson(_data);
            _zoom = _canvas.Zoom;
            _pan = _canvas.Pan;
        }

        void OnGUI()
        {
            HandleShortcuts();
            DrawToolbar();
            if (_recoveryAvailable) DrawRecoveryBanner();

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

            AutoSaveRecovery();
            if (Event.current.type == EventType.MouseMove) Repaint();
        }

        /// <summary>Unity çökerse iş kaybolmasın: çalışma kopyasını periyodik yaz.</summary>
        void AutoSaveRecovery()
        {
            if (!_dirty) return;
            if (EditorApplication.timeSinceStartup - _lastRecoveryWrite < 10.0) return;
            _lastRecoveryWrite = EditorApplication.timeSinceStartup;
            LevelEditorIO.WriteRecovery(_data);
        }

        void DrawRecoveryBanner()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "Önceki oturumdan kurtarılabilir bir çalışma bulundu.",
                    EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Yükle", GUILayout.Width(60)))
                {
                    if (LevelEditorIO.TryReadRecovery(out var recovered))
                    {
                        Record();
                        _data = recovered;
                        _path = null;
                        AfterChange();
                    }
                    _recoveryAvailable = false;
                }
                if (GUILayout.Button("Yoksay", GUILayout.Width(60)))
                {
                    LevelEditorIO.ClearRecovery();
                    _recoveryAvailable = false;
                }
            }
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
            _selections.Clear();
            AfterChange();
        }

        void Redo()
        {
            if (_redoStack.Count == 0) return;
            _undoStack.Add(LevelEditorIO.ToJson(_data));
            _data = LevelEditorIO.FromJson(_redoStack[_redoStack.Count - 1]);
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _selections.Clear();
            AfterChange();
        }

        void AfterChange()
        {
            _dirty = true;
            _validationStale = true;
            _playbackStep = -1;
            StashState();
            Repaint();
        }

        // ---------------- dosya ----------------

        /// <summary>Kaydedilmemiş iş varsa sorar. Devam edilebilirse true.</summary>
        bool ConfirmDiscard()
        {
            if (!_dirty) return true;
            int choice = EditorUtility.DisplayDialogComplex(
                "Kaydedilmemiş değişiklikler",
                "Bu bölümde kaydedilmemiş değişiklikler var.",
                "Kaydet", "İptal", "Kaydetme");

            if (choice == 1) return false;
            if (choice == 0)
            {
                string path = _path ?? LevelEditorIO.AskSavePath(_data.Id);
                if (path == null) return false;
                SaveTo(path);
            }
            return true;
        }

        void LoadFrom(string path)
        {
            try
            {
                _data = LevelEditorIO.FromJson(System.IO.File.ReadAllText(path));
                _path = path;
                _selections.Clear();
                _undoStack.Clear(); _redoStack.Clear();
                _canvas.ResetView();
                AfterChange(); _dirty = false;
                LevelEditorIO.ClearRecovery();
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
            LevelEditorIO.ClearRecovery();
            ShowNotification(new GUIContent("Kaydedildi"));
        }

        void RunValidation()
        {
            _validationStale = false;
            _problemCells.Clear();

            if (_palette == null || _config == null) { _report = null; return; }

            _report = LevelValidationTool.ValidateData(_data, _palette, _config);
            CollectProblemCells();
        }

        /// <summary>Tuvalde kırmızı gösterilecek sorunlu hücreler (çakışma / taşma).</summary>
        void CollectProblemCells()
        {
            var seen = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < _data.Blocks.Count; i++)
            {
                var block = _data.Blocks[i];
                for (int x = block.X; x < block.X + block.W; x++)
                    for (int y = block.Y; y < block.Y + block.H; y++)
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

        // ---------------- tuval çizimi ----------------

        void DrawCanvas()
        {
            var area = GUILayoutUtility.GetRect(200, 200,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(area, new Color(0.13f, 0.11f, 0.20f));

            _canvas.Layout(area, _data.Board.Width, _data.Board.Height);

            if (_reference.Behind) _reference.Draw(_canvas.BoardRect);
            DrawCells();
            DrawWalls();
            DrawCurtains();
            DrawBlocks();
            DrawGates();
            if (!_reference.Behind) _reference.Draw(_canvas.BoardRect);

            DrawProblems();
            DrawSolutionBadges();
            DrawSelectionOutlines();
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

        // DrawBlock her karede çağrılır; her seferinde liste ayırmamak için paylaşılır.
        static readonly List<Vector2Int> BlockCellBuffer = new List<Vector2Int>();

        void DrawBlock(BlockData block, float alpha)
        {
            BlockShape.LocalCells(block, BlockCellBuffer);
            if (BlockCellBuffer.Count == 0) return;

            var color = ColorOf(block.Layers.Count > 0 ? block.Layers[0] : "red");
            color.a = alpha;

            // Polyomino: hücre hücre boyanır. Komşusu olan kenarda boşluk
            // bırakılmaz, böylece L parçası tek gövde gibi görünür.
            foreach (var cell in BlockCellBuffer)
            {
                var cellRect = _canvas.RectFor(block.X + cell.x, block.Y + cell.y, 1, 1);
                float left   = BlockShape.Covers(block, block.X + cell.x - 1, block.Y + cell.y) ? 0f : 2f;
                float right  = BlockShape.Covers(block, block.X + cell.x + 1, block.Y + cell.y) ? 0f : 2f;
                float top    = BlockShape.Covers(block, block.X + cell.x, block.Y + cell.y - 1) ? 0f : 2f;
                float bottom = BlockShape.Covers(block, block.X + cell.x, block.Y + cell.y + 1) ? 0f : 2f;

                LevelCanvasDrawer.Fill(new Rect(
                    cellRect.x + left, cellRect.y + top,
                    cellRect.width - left - right, cellRect.height - top - bottom), color);

                if (block.Layers.Count > 1)
                {
                    float inset = _canvas.CellSize * 0.26f;
                    var innerColor = ColorOf(block.Layers[1]);
                    innerColor.a = alpha;
                    LevelCanvasDrawer.Fill(new Rect(
                        cellRect.x + inset, cellRect.y + inset,
                        cellRect.width - inset * 2f, cellRect.height - inset * 2f), innerColor);
                }

                if (block.Ice > 0)
                    LevelCanvasDrawer.Fill(cellRect, new Color(0.62f, 0.85f, 1f, 0.55f * alpha));
            }

            if (block.Ice > 0)
            {
                var first = BlockCellBuffer[0];
                LevelCanvasDrawer.Label(
                    _canvas.RectFor(block.X + first.x, block.Y + first.y, 1, 1), block.Ice.ToString(),
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
                else LevelCanvasDrawer.Fill(rect, ColorOf(gate.Colors.Count > 0 ? gate.Colors[0] : "red"));
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

        /// <summary>Çözüm hamlelerini sıra numaralı rozetler olarak gösterir.</summary>
        void DrawSolutionBadges()
        {
            if (!_showSolution || _report?.Solution == null) return;

            var moves = _report.Solution.Moves;
            for (int i = 0; i < moves.Count; i++)
            {
                if (_playbackStep >= 0 && i > _playbackStep) break;

                var move = moves[i];
                var rect = _canvas.RectFor(move.X, move.Y, move.W, move.H);
                bool current = _playbackStep == i;

                var tint = ColorOf(move.Color);
                tint.a = current ? 0.75f : 0.28f;
                LevelCanvasDrawer.Fill(rect, tint);
                LevelCanvasDrawer.Outline(rect, current ? Color.white : new Color(1f, 1f, 1f, 0.5f),
                    current ? 3f : 1f);
                LevelCanvasDrawer.Label(rect, (i + 1).ToString(),
                    current ? Color.white : new Color(1f, 1f, 1f, 0.85f),
                    Mathf.RoundToInt(_canvas.CellSize * (current ? 0.45f : 0.34f)));
            }
        }

        void DrawSelectionOutlines()
        {
            var accent = new Color(1f, 0.85f, 0.2f);
            foreach (var selection in _selections)
            {
                switch (selection.Kind)
                {
                    case SelKind.Block:
                    case SelKind.Content:
                    {
                        var block = BlockOf(selection);
                        if (block != null)
                            LevelCanvasDrawer.Highlight(
                                _canvas.RectFor(block.X, block.Y, block.W, block.H), accent);
                        break;
                    }
                    case SelKind.Gate:
                        if (selection.Index < _data.Gates.Count)
                        {
                            var gate = _data.Gates[selection.Index];
                            if (SideUtil.TryParse(gate.Side, out var side))
                                LevelCanvasDrawer.Highlight(
                                    _canvas.GateRect(gate.X, gate.Y, side, gate.Length), accent);
                        }
                        break;
                    case SelKind.Curtain:
                        if (selection.Index < _data.Obstacles.Count)
                            LevelCanvasDrawer.Highlight(CurtainRect(_data.Obstacles[selection.Index]), accent);
                        break;
                }
            }
        }

        void DrawHover()
        {
            var mouse = Event.current.mousePosition;

            if (_boxSelecting && _regionStart.HasValue &&
                _canvas.TryCell(mouse, _data.Board.Width, _data.Board.Height, out var boxCell))
            {
                var region = RegionRect(_regionStart.Value, boxCell);
                var rect = _canvas.RectFor(region.x, region.y, region.width, region.height);
                LevelCanvasDrawer.Fill(rect, new Color(1f, 0.85f, 0.2f, 0.15f));
                LevelCanvasDrawer.Outline(rect, new Color(1f, 0.85f, 0.2f), 2f);
                return;
            }

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

            if (_tool == Tool.Curtain && _regionStart.HasValue)
            {
                var region = RegionRect(_regionStart.Value, cell);
                LevelCanvasDrawer.Outline(_canvas.RectFor(region.x, region.y, region.width, region.height),
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
                    ? $"({cell.x},{cell.y})" : "—";
                GUILayout.Label(cellText, EditorStyles.miniLabel, GUILayout.Width(64));
                GUILayout.Label($"{_data.Blocks.Count} blok · {_data.Gates.Count} kapı · " +
                                $"{_data.Obstacles.Count} engel · seçili {_selections.Count}",
                    EditorStyles.miniLabel);
                GUILayout.Label($"%{_canvas.Zoom * 100f:0}", EditorStyles.miniLabel, GUILayout.Width(46));
                if (GUILayout.Button("Görünümü sıfırla", EditorStyles.toolbarButton, GUILayout.Width(110)))
                    _canvas.ResetView();
                GUILayout.FlexibleSpace();
                GUILayout.Label("1-6 araç · Ctrl+Z geri · Del sil · Ctrl+D çoğalt · R döndür · " +
                                "tekerlek yakınlaştır · orta tuş kaydır", EditorStyles.miniLabel);
            }
        }

        // ---------------- ortak yardımcılar ----------------

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

        /// <summary>Seçim kaydından blok verisine erişim (perde içeriği dahil).</summary>
        BlockData BlockOf(Selection selection)
        {
            if (selection.Kind == SelKind.Block)
                return selection.Index < _data.Blocks.Count ? _data.Blocks[selection.Index] : null;

            if (selection.Kind == SelKind.Content && selection.Index < _data.Obstacles.Count)
            {
                var contents = LevelEditorIO.GetContents(_data.Obstacles[selection.Index]);
                return selection.Sub < contents.Count ? contents[selection.Sub] : null;
            }
            return null;
        }

        Selection Primary => _selections.Count > 0 ? _selections[0] : Selection.None;

        /// <summary>
        /// Perde içeriği JSON'dan KOPYA olarak okunur; düzenlemenin kalıcı
        /// olması için perdeye geri yazılmalı.
        /// </summary>
        void CommitContentEdit(Selection selection, BlockData edited)
        {
            if (selection.Kind != SelKind.Content) return;
            if (selection.Index >= _data.Obstacles.Count) return;

            var curtain = _data.Obstacles[selection.Index];
            var contents = LevelEditorIO.GetContents(curtain);
            if (selection.Sub < contents.Count) contents[selection.Sub] = edited;
            LevelEditorIO.SetContents(curtain, contents);
        }
    }
}
