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
    /// DERS (EditorWindow): Unity editörü kendi arayüzünü de IMGUI ile çizer;
    /// aynı sistemle kendi araçlarımızı yazabiliriz. Stüdyolarda içerik
    /// üretimini hızlandıran bu "iç araçlar" katmanı, oyunun kendisi kadar
    /// önemlidir: tasarımcı kod bilmeden bölüm üretebilmelidir.
    ///
    /// Araç, oyunun yüklediği <see cref="LevelData"/> DTO'larını doğrudan
    /// düzenler — ayrı bir editör modeli YOKTUR.
    /// </summary>
    public sealed class LevelEditorWindow : EditorWindow
    {
        enum Tool { Shape, Blocks, Gates, Walls, Curtain }

        static readonly string[] ToolLabels = { "Şekil", "Bloklar", "Kapılar", "Duvarlar", "Perde" };
        static readonly string[] Difficulties = { "normal", "hard", "superhard" };

        LevelData _data;
        string _path;
        bool _dirty;

        Tool _tool = Tool.Shape;
        readonly LevelCanvasDrawer _canvas = new LevelCanvasDrawer();

        // Araç seçenekleri
        int _blockW = 1, _blockH = 1, _blockIce;
        readonly List<BlockColor> _layers = new List<BlockColor> { BlockColor.Red };
        BlockColor _gateColor = BlockColor.Red;
        int _gateLength = 2, _gateIce;
        int _curtainCount = 3;
        Vector2Int? _curtainDragStart;

        ColorPaletteSO _palette;
        GameConfigSO _config;
        readonly List<string> _report = new List<string>();
        bool _reportOk;
        Vector2 _reportScroll;
        bool _shapePaintValue;

        [MenuItem("Tools/Block Out/Level Editör")]
        public static void Open()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editör");
            window.minSize = new Vector2(760, 520);
        }

        void OnEnable()
        {
            wantsMouseMove = true;
            _palette = AssetDatabase.LoadAssetAtPath<ColorPaletteSO>(
                "Assets/_Project/ScriptableObjects/ColorPalette.asset");
            _config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(
                "Assets/_Project/ScriptableObjects/GameConfig.asset");
            _data ??= LevelEditorIO.NewLevel();
        }

        void OnGUI()
        {
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSidePanel();
                DrawCanvas();
            }

            if (Event.current.type == EventType.MouseMove) Repaint();
        }

        // ---------------- üst şerit ----------------

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Yeni", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    _data = LevelEditorIO.NewLevel();
                    _path = null; _dirty = false; _report.Clear();
                }
                if (GUILayout.Button("Aç…", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    LoadFromDisk();
                using (new EditorGUI.DisabledScope(_path == null))
                    if (GUILayout.Button("Kaydet", EditorStyles.toolbarButton, GUILayout.Width(70)))
                        SaveTo(_path);
                if (GUILayout.Button("Farklı Kaydet…", EditorStyles.toolbarButton, GUILayout.Width(110)))
                {
                    string path = LevelEditorIO.AskSavePath(_data.Id);
                    if (path != null) SaveTo(path);
                }

                GUILayout.Space(12);
                if (GUILayout.Button("Doğrula", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    RunValidation();
                if (GUILayout.Button("▶ Play Test", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    LevelEditorIO.PlayTest(_data);

                GUILayout.FlexibleSpace();
                GUILayout.Label(_path == null ? "(kaydedilmemiş)" : _path + (_dirty ? " *" : ""),
                    EditorStyles.miniLabel);
            }
        }

        void LoadFromDisk()
        {
            string path = LevelEditorIO.AskLoadPath();
            if (path == null) return;
            try
            {
                _data = LevelEditorIO.FromJson(System.IO.File.ReadAllText(path));
                _path = path; _dirty = false; _report.Clear();
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
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(260)))
            {
                EditorGUILayout.LabelField("Bölüm Bilgileri", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                _data.Id = EditorGUILayout.TextField("Kimlik", _data.Id);
                _data.DisplayNumber = EditorGUILayout.IntField("Bölüm No", _data.DisplayNumber);
                int diff = Mathf.Max(0, System.Array.IndexOf(Difficulties, _data.Difficulty));
                diff = EditorGUILayout.Popup("Zorluk", diff, Difficulties);
                _data.Difficulty = Difficulties[diff];
                _data.TimeSeconds = EditorGUILayout.IntField("Süre (sn)", _data.TimeSeconds);

                int w = EditorGUILayout.IntSlider("Genişlik", _data.Board.Width, 3, 12);
                int h = EditorGUILayout.IntSlider("Yükseklik", _data.Board.Height, 3, 14);
                if (EditorGUI.EndChangeCheck())
                {
                    if (w != _data.Board.Width || h != _data.Board.Height) ResizeBoard(w, h);
                    _dirty = true;
                }

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("Araç", EditorStyles.boldLabel);
                _tool = (Tool)GUILayout.SelectionGrid((int)_tool, ToolLabels, 3);
                EditorGUILayout.HelpBox(ToolHelp(), MessageType.None);

                EditorGUILayout.Space(4);
                DrawToolOptions();

                EditorGUILayout.Space(8);
                DrawReport();
            }
        }

        string ToolHelp()
        {
            switch (_tool)
            {
                case Tool.Shape:   return "Sol tık: hücreyi aç/kapa (sürükleyerek boyayabilirsin).";
                case Tool.Blocks:  return "Sol tık: blok koy. Sağ tık: sil. Perde içine koyarsan gizli içerik olur.";
                case Tool.Gates:   return "Sol tık: en yakın kenara kapı koy. Sağ tık: sil.";
                case Tool.Walls:   return "Sol tık: iç duvar aç/kapa. Sağ tık: sil.";
                default:           return "Sürükle: perde bölgesi seç. Sağ tık: sil.";
            }
        }

        void DrawToolOptions()
        {
            switch (_tool)
            {
                case Tool.Blocks:
                    _blockW = EditorGUILayout.IntSlider("Genişlik", _blockW, 1, 5);
                    _blockH = EditorGUILayout.IntSlider("Yükseklik", _blockH, 1, 5);
                    _blockIce = EditorGUILayout.IntField("Buz sayacı", Mathf.Max(0, _blockIce));
                    EditorGUILayout.LabelField("Katmanlar (0 = dış)");
                    for (int i = 0; i < _layers.Count; i++)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            _layers[i] = (BlockColor)EditorGUILayout.EnumPopup($"  {i}", _layers[i]);
                            using (new EditorGUI.DisabledScope(_layers.Count <= 1))
                                if (GUILayout.Button("−", GUILayout.Width(22)))
                                { _layers.RemoveAt(i); break; }
                        }
                    }
                    if (_layers.Count < 3 && GUILayout.Button("Katman ekle"))
                        _layers.Add(BlockColor.Blue);
                    break;

                case Tool.Gates:
                    _gateColor = (BlockColor)EditorGUILayout.EnumPopup("Renk", _gateColor);
                    _gateLength = EditorGUILayout.IntSlider("Uzunluk", _gateLength, 1, 5);
                    _gateIce = EditorGUILayout.IntField("Buz kaplaması", Mathf.Max(0, _gateIce));
                    break;

                case Tool.Curtain:
                    _curtainCount = EditorGUILayout.IntSlider("Sayaç", _curtainCount, 1, 20);
                    break;
            }
        }

        void DrawReport()
        {
            if (_report.Count == 0) return;

            EditorGUILayout.LabelField(_reportOk ? "Doğrulama: TEMİZ" : "Doğrulama: SORUNLU",
                EditorStyles.boldLabel);
            using (var scroll = new EditorGUILayout.ScrollViewScope(_reportScroll, GUILayout.Height(120)))
            {
                _reportScroll = scroll.scrollPosition;
                foreach (var line in _report)
                    EditorGUILayout.LabelField("• " + line, EditorStyles.wordWrappedMiniLabel);
            }
        }

        void RunValidation()
        {
            _report.Clear();
            if (_palette == null || _config == null)
            {
                _reportOk = false;
                _report.Add("ColorPalette / GameConfig asset'i bulunamadı.");
                return;
            }
            _reportOk = LevelValidationTool.ValidateData(_data, _palette, _config, _report, out int moves);
            if (_reportOk) _report.Add($"Bölüm {moves} hamlede çözülebilir.");
        }

        void ResizeBoard(int width, int height)
        {
            var rows = new List<string>(height);
            for (int y = 0; y < height; y++)
            {
                string old = y < _data.Board.Rows.Count ? _data.Board.Rows[y] : "";
                var sb = new System.Text.StringBuilder(width);
                for (int x = 0; x < width; x++)
                    sb.Append(x < old.Length ? old[x] : 'X');
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

            var board = _data.Board;
            _canvas.Layout(area, board.Width, board.Height);

            DrawCells();
            DrawWalls();
            DrawCurtains();
            DrawBlocks();
            DrawGates();
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
            var empty = new Color(0.18f, 0.16f, 0.26f);

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
                for (int i = 0; i < Mathf.Max(1, wall.Length); i++)
                {
                    var edge = EdgeId.OfCellSide(wall.X, wall.Y, side);
                    var actual = edge.Horizontal
                        ? EdgeId.OfCellSide(wall.X + i, wall.Y, side)
                        : EdgeId.OfCellSide(wall.X, wall.Y + i, side);
                    LevelCanvasDrawer.Fill(_canvas.EdgeRect(actual), color);
                }
            }
        }

        void DrawBlocks()
        {
            foreach (var block in _data.Blocks)
                DrawBlock(block, 1f);
        }

        void DrawBlock(BlockData block, float alpha)
        {
            var rect = _canvas.RectFor(block.X, block.Y, block.W, block.H);
            rect = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);

            var color = ColorOf(block.Layers.Count > 0 ? block.Layers[0] : "red");
            color.a = alpha;
            LevelCanvasDrawer.Fill(rect, color);

            // İç katman varsa çerçeve içinde ikinci renk göster (video görünümü).
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
                LevelCanvasDrawer.Fill(rect, new Color(0.62f, 0.85f, 1f, 0.55f));
                LevelCanvasDrawer.Label(rect, block.Ice.ToString(),
                    new Color(0.1f, 0.2f, 0.4f), Mathf.RoundToInt(_canvas.CellSize * 0.35f));
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
                    LevelCanvasDrawer.Fill(rect, new Color(0.75f, 0.9f, 1f, 0.9f));
                    LevelCanvasDrawer.Label(rect, gate.Ice.ToString(),
                        new Color(0.1f, 0.2f, 0.4f), Mathf.RoundToInt(_canvas.CellSize * 0.3f));
                }
                else
                {
                    LevelCanvasDrawer.Fill(rect,
                        ColorOf(gate.Colors.Count > 0 ? gate.Colors[0] : "red"));
                }
            }
        }

        void DrawCurtains()
        {
            foreach (var obstacle in _data.Obstacles)
            {
                if (obstacle.Type != "curtain") continue;
                int x = LevelEditorIO.GetInt(obstacle, "x"), y = LevelEditorIO.GetInt(obstacle, "y");
                int w = LevelEditorIO.GetInt(obstacle, "w", 1), h = LevelEditorIO.GetInt(obstacle, "h", 1);
                var rect = _canvas.RectFor(x, y, w, h);

                // Gizli içerik tasarımcıya soluk gösterilir (oyuncu göremez).
                foreach (var hidden in LevelEditorIO.GetContents(obstacle))
                    DrawBlock(hidden, 0.35f);

                LevelCanvasDrawer.Fill(rect, new Color(0.22f, 0.16f, 0.38f, 0.85f));
                LevelCanvasDrawer.Outline(rect, new Color(0.85f, 0.65f, 0.2f), 3f);
                LevelCanvasDrawer.Label(rect, LevelEditorIO.GetInt(obstacle, "count").ToString(),
                    new Color(1f, 0.9f, 0.55f), Mathf.RoundToInt(_canvas.CellSize * 0.4f));
            }
        }

        void DrawHover()
        {
            var mouse = Event.current.mousePosition;
            if (!_canvas.TryCell(mouse, _data.Board.Width, _data.Board.Height, out var cell)) return;

            if (_tool == Tool.Gates || _tool == Tool.Walls)
            {
                if (_canvas.TryEdge(mouse, _data.Board.Width, _data.Board.Height, out var c, out var side))
                {
                    var rect = _tool == Tool.Gates
                        ? _canvas.GateRect(c.x, c.y, side, _gateLength)
                        : _canvas.EdgeRect(EdgeId.OfCellSide(c.x, c.y, side));
                    LevelCanvasDrawer.Fill(rect, new Color(1f, 1f, 1f, 0.45f));
                }
                return;
            }

            if (_tool == Tool.Blocks)
            {
                LevelCanvasDrawer.Outline(
                    _canvas.RectFor(cell.x, cell.y, _blockW, _blockH), Color.white, 2f);
                return;
            }

            if (_tool == Tool.Curtain && _curtainDragStart.HasValue)
            {
                var r = RegionRect(_curtainDragStart.Value, cell);
                LevelCanvasDrawer.Outline(
                    _canvas.RectFor(r.x, r.y, r.width, r.height),
                    new Color(0.85f, 0.65f, 0.2f), 2f);
                return;
            }

            LevelCanvasDrawer.Outline(_canvas.RectFor(cell.x, cell.y), Color.white, 2f);
        }

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

        // ---------------- etkileşim ----------------

        void HandleInput(Rect area)
        {
            var e = Event.current;
            if (!area.Contains(e.mousePosition)) return;

            bool erase = e.button == 1;
            bool down = e.type == EventType.MouseDown;
            bool drag = e.type == EventType.MouseDrag;
            bool up = e.type == EventType.MouseUp;
            if (!down && !drag && !up) return;

            switch (_tool)
            {
                case Tool.Shape:   HandleShape(e, down, drag); break;
                case Tool.Blocks:  if (down) HandleBlocks(e, erase); break;
                case Tool.Gates:   if (down) HandleGates(e, erase); break;
                case Tool.Walls:   if (down) HandleWalls(e, erase); break;
                case Tool.Curtain: HandleCurtain(e, down, up, erase); break;
            }
        }

        void MarkDirty(Event e)
        {
            _dirty = true;
            e.Use();
            Repaint();
        }

        void HandleShape(Event e, bool down, bool drag)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;

            // Basılı tutup sürüklerken TEK değer boyanır (yoksa hücreler
            // parmak altında yanıp söner) — ilk tıklamanın tersi alınır.
            if (down) _shapePaintValue = !Playable(cell.x, cell.y);
            else if (!drag) return;

            var row = _data.Board.Rows[cell.y].ToCharArray();
            row[cell.x] = _shapePaintValue ? 'X' : '.';
            _data.Board.Rows[cell.y] = new string(row);
            MarkDirty(e);
        }

        void HandleBlocks(Event e, bool erase)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;

            if (erase)
            {
                if (RemoveBlockAt(cell)) MarkDirty(e);
                return;
            }

            var block = new BlockData { X = cell.x, Y = cell.y, W = _blockW, H = _blockH, Ice = _blockIce };
            foreach (var layer in _layers) block.Layers.Add(layer.ToId());

            // Perde bölgesine denk geliyorsa GİZLİ İÇERİK olarak eklenir.
            var curtain = CurtainCovering(cell);
            if (curtain != null)
            {
                var contents = LevelEditorIO.GetContents(curtain);
                contents.Add(block);
                LevelEditorIO.SetContents(curtain, contents);
            }
            else
            {
                _data.Blocks.Add(block);
            }
            MarkDirty(e);
        }

        bool RemoveBlockAt(Vector2Int cell)
        {
            for (int i = _data.Blocks.Count - 1; i >= 0; i--)
                if (Covers(_data.Blocks[i], cell)) { _data.Blocks.RemoveAt(i); return true; }

            foreach (var obstacle in _data.Obstacles)
            {
                if (obstacle.Type != "curtain") continue;
                var contents = LevelEditorIO.GetContents(obstacle);
                for (int i = contents.Count - 1; i >= 0; i--)
                    if (Covers(contents[i], cell))
                    {
                        contents.RemoveAt(i);
                        LevelEditorIO.SetContents(obstacle, contents);
                        return true;
                    }
            }
            return false;
        }

        static bool Covers(BlockData block, Vector2Int cell) =>
            cell.x >= block.X && cell.x < block.X + block.W &&
            cell.y >= block.Y && cell.y < block.Y + block.H;

        ObstacleData CurtainCovering(Vector2Int cell)
        {
            foreach (var obstacle in _data.Obstacles)
            {
                if (obstacle.Type != "curtain") continue;
                int x = LevelEditorIO.GetInt(obstacle, "x"), y = LevelEditorIO.GetInt(obstacle, "y");
                int w = LevelEditorIO.GetInt(obstacle, "w", 1), h = LevelEditorIO.GetInt(obstacle, "h", 1);
                if (cell.x >= x && cell.x < x + w && cell.y >= y && cell.y < y + h)
                    return obstacle;
            }
            return null;
        }

        void HandleGates(Event e, bool erase)
        {
            if (!_canvas.TryEdge(e.mousePosition, _data.Board.Width, _data.Board.Height,
                    out var cell, out var side))
                return;

            string sideId = side.ToId();
            int existing = _data.Gates.FindIndex(g => g.X == cell.x && g.Y == cell.y && g.Side == sideId);

            if (erase)
            {
                if (existing >= 0) { _data.Gates.RemoveAt(existing); MarkDirty(e); }
                return;
            }

            if (existing >= 0) _data.Gates.RemoveAt(existing); // aynı yere koyarsan günceller
            var gate = new GateData
            {
                X = cell.x, Y = cell.y, Side = sideId, Length = _gateLength, Ice = _gateIce
            };
            gate.Colors.Add(_gateColor.ToId());
            _data.Gates.Add(gate);
            MarkDirty(e);
        }

        void HandleWalls(Event e, bool erase)
        {
            if (!_canvas.TryEdge(e.mousePosition, _data.Board.Width, _data.Board.Height,
                    out var cell, out var side))
                return;

            var target = EdgeId.OfCellSide(cell.x, cell.y, side);
            int existing = _data.Board.Walls.FindIndex(w =>
                SideUtil.TryParse(w.Side, out var s) &&
                EdgeId.OfCellSide(w.X, w.Y, s).Equals(target));

            if (existing >= 0) _data.Board.Walls.RemoveAt(existing);
            else if (!erase)
                _data.Board.Walls.Add(new WallData
                {
                    X = cell.x, Y = cell.y, Side = side.ToId(), Length = 1
                });
            MarkDirty(e);
        }

        void HandleCurtain(Event e, bool down, bool up, bool erase)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;

            if (erase)
            {
                if (down)
                {
                    var curtain = CurtainCovering(cell);
                    if (curtain != null) { _data.Obstacles.Remove(curtain); MarkDirty(e); }
                }
                return;
            }

            if (down) { _curtainDragStart = cell; e.Use(); return; }

            if (up && _curtainDragStart.HasValue)
            {
                var region = RegionRect(_curtainDragStart.Value, cell);
                _curtainDragStart = null;
                _data.Obstacles.Add(LevelEditorIO.NewCurtain(
                    region.x, region.y, region.width, region.height, _curtainCount));
                MarkDirty(e);
            }
        }
    }
}
