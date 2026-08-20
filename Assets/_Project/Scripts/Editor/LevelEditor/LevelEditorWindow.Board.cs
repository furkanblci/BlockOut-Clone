using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.View;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Tahta sekmesi: üç kolonlu düzen (denetçi · tuval · bölüm tarayıcısı) ve
    /// tuvalin kendi çizimi.
    ///
    /// DERS (kolonların ANLAMI): Solda "seçili şeyi düzenle", ortada "tahtayı
    /// gör", sağda "hangi bölümde olduğunu bil". Üçü de aynı anda görünür
    /// olmalı çünkü bölüm tasarlamak sürekli bu üçü arasında geçiş yapmaktır.
    /// Sağdaki tarayıcı kapatılabilir — dar ekranda tuval kazanır.
    /// </summary>
    public sealed partial class LevelEditorWindow
    {
        const float PanelWidth = 292f;
        const float LibraryWidth = 226f;
        const float GateStripHeight = 104f;

        void DrawBoardTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSidePanel();

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawCanvasToolbar();
                    DrawCanvas();
                    DrawGateStrip();
                }

                if (_showLibrary) DrawLibraryDock();
            }
        }

        /// <summary>Tuvalin üstündeki görünüm şeridi — yakınlaştırma ve ızgara anahtarları.</summary>
        void DrawCanvasToolbar()
        {
            using (LevelEditorSkin.BarScope())
            {
                GUILayout.Label("Yakınlaştır", LevelEditorSkin.RowLabel, GUILayout.Width(66));
                float zoom = GUILayout.HorizontalSlider(_canvas.Zoom,
                    LevelCanvasDrawer.MinZoom, LevelCanvasDrawer.MaxZoom, GUILayout.Width(120));
                if (!Mathf.Approximately(zoom, _canvas.Zoom)) { _canvas.Zoom = zoom; StashState(); }
                GUILayout.Label($"%{_canvas.Zoom * 100f:0}", LevelEditorSkin.RowLabel, GUILayout.Width(40));

                if (LevelEditorSkin.BarButton("Sığdır", 52f))
                { _canvas.ResetView(); StashState(); }

                GUILayout.Space(8);
                _realVisuals = LevelEditorSkin.BarToggle(_realVisuals, "Gerçek görsel", 92f,
                    "Açık: tahtayı oyunun kendi mesh ve materyalleri çizer (tepeden " +
                    "ortografik). Kapalı: hızlı 2B tuğla çizimi.");
                _showGrid = LevelEditorSkin.BarToggle(_showGrid, "Izgara", 56f);
                _showRulers = LevelEditorSkin.BarToggle(_showRulers, "Cetvel", 56f);
                _reference.Visible = LevelEditorSkin.BarToggle(_reference.Visible, "Referans", 68f,
                    "Referans görseli tuvale bindir");

                GUILayout.FlexibleSpace();

                // Gerçek görsel istenip kurulamadıysa SESSİZ kalmak yanlış olur:
                // kullanıcı 2B çizimi görür ve anahtarın bozuk olduğunu sanır.
                if (_realVisuals && !string.IsNullOrEmpty(_realBoardError))
                {
                    var warn = new GUIStyle(LevelEditorSkin.RowLabel);
                    warn.normal.textColor = LevelEditorSkin.WarningBright;
                    GUILayout.Label("⚠ gerçek görsel kurulamadı (2B çizime düşüldü): " +
                                    _realBoardError, warn);
                }

                if (_report != null && !_report.Ok)
                {
                    var style = new GUIStyle(LevelEditorSkin.RowLabel);
                    style.normal.textColor = LevelEditorSkin.DangerBright;
                    GUILayout.Label("⚠ " + (_report.Errors.Count > 0
                        ? _report.Errors[0]
                        : "çözülemiyor"), style);
                }

                _autoValidate = LevelEditorSkin.BarToggle(_autoValidate, "Oto doğrula", 82f,
                    "Her değişiklikten sonra çözücüyü koştur");
                _showLibrary = LevelEditorSkin.BarToggle(_showLibrary, "Tarayıcı", 66f);
            }
        }

        // ---------------- tuval ----------------

        /// <summary>
        /// Tuvali çizer.
        ///
        /// DERS (aynı görünüm, farklı YETKİ): Tuval üç sekmede kullanılıyor —
        /// Tahta'da düzenlemek, Çözüm'de hamleleri izlemek, Referans'ta kareyi
        /// hizalamak için. Etkileşim ayrımı yapılmadığı sürece Çözüm sekmesinde
        /// dalgın bir sürükleme bloğu sessizce oynatıyor ve incelenen çözümü
        /// GEÇERSİZ kılıyordu. Görünüm paylaşılabilir, yetki paylaşılamaz.
        /// </summary>
        void DrawCanvas(bool interactive = true)
        {
            var area = GUILayoutUtility.GetRect(200, 200,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            LevelEditorSkin.Fill(area, LevelEditorSkin.Inset);

            _canvas.Layout(area, _data.Board.Width, _data.Board.Height);

            if (_reference.Behind) _reference.Draw(_canvas.BoardRect);

            // "Gerçek görsel" açıkken tahtayı OYUNUN kodu çiziyor; 2B tuğla
            // çizimi yalnız yedek (ve hızlı) yol olarak kalıyor.
            bool real = _realVisuals && DrawRealBoard();
            if (!real)
            {
                DrawCells();
                DrawWalls();
                DrawCurtains();
                DrawBlocks();
                DrawGates();
                DrawGenerators();
            }
            else
            {
                // Perdenin ARDINDAKİ bloklar gerçek render'da görünmez (oyunda da
                // görünmemeli). Ama tasarımcı onları düzenliyor — bu yüzden
                // yalnız editörde, soluk biçimde üstüne bindirilir.
                DrawHiddenContents();
                if (_showGrid) DrawGridOverlay();
            }

            if (!_reference.Behind) _reference.Draw(_canvas.BoardRect);

            DrawProblems();
            DrawSolutionBadges();
            DrawSelectionOutlines();
            if (_showRulers) DrawRulers();
            DrawHover();

            if (interactive) HandleInput(area);
            else if (Event.current.type == EventType.MouseDown && area.Contains(Event.current.mousePosition))
                Say("Bu sekmede tuval salt okunur — düzenlemek için Tahta sekmesine geç");
        }

        /// <summary>
        /// Izgaranın kenarlarına satır/kolon numaraları. Referans editördeki
        /// gibi HER İKİ kenarda: geniş bir tahtada tek kenardaki numarayı
        /// karşı köşeden takip etmek gözle yapılabilir bir iş değil.
        /// </summary>
        void DrawRulers()
        {
            var color = new Color(0.62f, 0.62f, 0.72f);
            int size = Mathf.Max(8, Mathf.RoundToInt(_canvas.CellSize * 0.26f));
            var board = _canvas.BoardRect;

            for (int x = 0; x < _data.Board.Width; x++)
            {
                var cell = _canvas.RectFor(x, 0);
                LevelCanvasDrawer.Label(new Rect(cell.x, board.y - 18f, cell.width, 14f),
                    x.ToString(), color, size, FontStyle.Normal);
                LevelCanvasDrawer.Label(new Rect(cell.x, board.yMax + 4f, cell.width, 14f),
                    x.ToString(), color, size, FontStyle.Normal);
            }

            for (int y = 0; y < _data.Board.Height; y++)
            {
                var cell = _canvas.RectFor(0, y);
                LevelCanvasDrawer.Label(new Rect(board.x - 22f, cell.y, 18f, cell.height),
                    y.ToString(), color, size, FontStyle.Normal);
                LevelCanvasDrawer.Label(new Rect(board.xMax + 4f, cell.y, 18f, cell.height),
                    y.ToString(), color, size, FontStyle.Normal);
            }
        }

        /// <summary>
        /// Tahta zemini ve çerçevesi — renkler oyunun görsel ayarından gelir.
        ///
        /// DERS (editörün zemini de oyunun zemini olmalı): Eski hâl lavanta bir
        /// satranç deseniydi; oyunda zemin TEK renk koyu mor + ızgara çizgileri
        /// ve tahtayı çevreleyen kalın bir çerçeve var. Zemin yanlış olunca
        /// blokların rengi de yanlış okunuyordu — bir rengin nasıl göründüğü
        /// altındaki zemine bağlıdır.
        /// </summary>
        void DrawCells()
        {
            var cfg = VisualSettings.Current;
            var floorA = cfg != null ? cfg.floorColorA : new Color(0.17f, 0.15f, 0.31f);
            var floorB = cfg != null ? cfg.floorColorB : new Color(0.14f, 0.12f, 0.27f);
            var frame = cfg != null ? cfg.frameColor : new Color(0.30f, 0.26f, 0.58f);
            float lineDarken = cfg != null ? cfg.floorLineDarken : 0.6f;

            // Çerçeve: tahtayı çevreleyen kalın bordür (oyunda BoardFrameMesh).
            float thickness = (cfg != null ? cfg.frameThickness : 0.55f) * _canvas.CellSize;
            if (thickness > 1f)
            {
                var board = _canvas.BoardRect;
                LevelEditorSkin.RoundedRect(new Rect(
                        board.x - thickness, board.y - thickness,
                        board.width + thickness * 2f, board.height + thickness * 2f),
                    frame, LevelEditorSkin.Round6);
            }

            var line = new Color(floorA.r * lineDarken, floorA.g * lineDarken,
                floorA.b * lineDarken, 1f);

            for (int y = 0; y < _data.Board.Height; y++)
                for (int x = 0; x < _data.Board.Width; x++)
                {
                    var rect = _canvas.RectFor(x, y);
                    if (!Playable(x, y))
                    {
                        // Oynanamaz hücre tahtanın DELİĞİ: zemin yok, arka plan görünür.
                        LevelEditorSkin.Fill(rect, LevelEditorSkin.Inset);
                        continue;
                    }

                    LevelCanvasDrawer.Fill(rect, (x + y) % 2 == 0 ? floorA : floorB);
                    if (_showGrid) LevelCanvasDrawer.Outline(rect, line);
                }
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

        /// <summary>
        /// Bloğu OYUNDAKİ tuğla gibi çizer: pah bandı, üst yüz ve saplamalar.
        ///
        /// DERS (editör oyunun DİLİNİ konuşmalı): Burası eskiden düz renk
        /// dikdörtgen boyuyordu. Oyunda bloklar LEGO benzeri kabartmalı tuğlalar
        /// (<see cref="BrickMeshBuilder"/>); tasarımcı tuvalde bambaşka bir şey
        /// görüyordu ve tahta "yabancı" duruyordu. Editör oyunun görsel dilini
        /// paylaşmazsa tasarımcı kafasında sürekli çeviri yapmak zorunda kalır —
        /// ve o çeviri her seferinde biraz yanlış olur.
        ///
        /// Ölçüler UYDURULMAZ, <see cref="BlockVisualConfigSO"/>'dan okunur:
        /// aynı asset mesh'i de üretiyor, yani ayar penceresinden tuğla
        /// değiştirilince tuval de birlikte değişir.
        /// </summary>
        void DrawBlock(BlockData block, float alpha)
        {
            BlockShape.LocalCells(block, BlockCellBuffer);
            if (BlockCellBuffer.Count == 0) return;

            var cfg = VisualSettings.Current;
            float inset = (cfg != null ? cfg.brickInset : 0.055f) * _canvas.CellSize;
            float chamfer = Mathf.Max(1f, (cfg != null ? cfg.brickChamfer : 0.06f) * _canvas.CellSize * 1.6f);
            int perCell = cfg != null ? cfg.studsPerCell : 2;
            float studRadius = (cfg != null ? cfg.studRadius : 0.168f) * _canvas.CellSize;

            var baseColor = ColorOf(block.Layers.Count > 0 ? block.Layers[0] : "red");
            var face = Tone(baseColor, cfg != null ? cfg.toneFaceTop : 0.72f, alpha);
            var side = Tone(baseColor, cfg != null ? cfg.toneBodySide : 0.86f, alpha);
            var studTop = Tone(baseColor, cfg != null ? cfg.toneStudTop : 1f, alpha);
            var studFoot = Tone(baseColor, cfg != null ? cfg.toneStudFoot : 0.42f, alpha);

            bool frozen = block.Ice > 0;
            var shape = _canvas.CellSize >= 18f ? LevelEditorSkin.Round4 : LevelEditorSkin.Round3;

            // 1) Temas gölgesi — oyundaki gölge quad'ının karşılığı.
            if (cfg == null || cfg.contactShadow)
            {
                var offset = cfg != null ? cfg.shadowOffset : new Vector2(0.06f, -0.06f);
                float shadowAlpha = (cfg != null ? cfg.shadowOpacity : 0.42f) * alpha;
                foreach (var cell in BlockCellBuffer)
                    DrawBrickCell(block, cell, inset, shape,
                        new Color(0f, 0f, 0f, shadowAlpha),
                        new Vector2(offset.x * _canvas.CellSize, -offset.y * _canvas.CellSize));
            }

            // 2) Pah bandı (yan yüz tonu) ve üstüne 3) üst yüz.
            foreach (var cell in BlockCellBuffer)
            {
                DrawBrickCell(block, cell, inset, shape, frozen ? Tone(side, 0.7f, alpha) : side, Vector2.zero);
                DrawBrickCell(block, cell, inset + chamfer, shape,
                    frozen ? Tone(face, 0.7f, alpha) : face, Vector2.zero);
            }

            // 4) Saplamalar: hücre başına perCell², tam mesh'teki yerlerde.
            if (!frozen && studRadius > 1.2f)
            {
                float step = _canvas.CellSize / perCell;
                float first = step * 0.5f;

                foreach (var cell in BlockCellBuffer)
                {
                    var origin = _canvas.RectFor(block.X + cell.x, block.Y + cell.y);
                    for (int sx = 0; sx < perCell; sx++)
                        for (int sy = 0; sy < perCell; sy++)
                        {
                            var center = new Vector2(
                                origin.x + first + sx * step,
                                origin.y + first + sy * step);

                            // Ayak (koyu) + tepe (açık): kabartma hissi iki
                            // daireden geliyor, gölge dokusuna gerek yok.
                            LevelEditorSkin.DrawDisc(new Rect(
                                center.x - studRadius, center.y - studRadius + 1f,
                                studRadius * 2f, studRadius * 2f), studFoot);
                            LevelEditorSkin.DrawDisc(new Rect(
                                center.x - studRadius, center.y - studRadius,
                                studRadius * 2f, studRadius * 2f), studTop);
                        }
                }
            }

            // 5) Katman: ikinci rengin halkası. Oyunda alt katman soyulunca
            // ortaya çıkıyor; tuvalde iç kare olarak duruyor.
            if (block.Layers.Count > 1 && !frozen)
            {
                var inner = Tone(ColorOf(block.Layers[1]), cfg != null ? cfg.toneFaceTop : 0.72f, alpha);
                foreach (var cell in BlockCellBuffer)
                {
                    var rect = _canvas.RectFor(block.X + cell.x, block.Y + cell.y);
                    float pad = _canvas.CellSize * 0.30f;
                    LevelEditorSkin.RoundedRect(
                        new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, rect.height - pad * 2f),
                        inner, LevelEditorSkin.Round3);
                }
            }

            // 6) Buz kabuğu: oyunda tuğlanın SİLUETİNİ alıyor, düz levha değil.
            if (frozen)
            {
                var iceTint = new Color(0.68f, 0.88f, 1f, 0.72f * alpha);
                foreach (var cell in BlockCellBuffer)
                    DrawBrickCell(block, cell, inset, shape, iceTint, Vector2.zero);

                var first = BlockCellBuffer[0];
                var counter = _canvas.RectFor(block.X + first.x, block.Y + first.y);
                LevelCanvasDrawer.Label(counter, block.Ice.ToString(),
                    new Color(0.08f, 0.20f, 0.38f, alpha),
                    Mathf.Max(9, Mathf.RoundToInt(_canvas.CellSize * 0.34f)));
            }

            // 7) Hareket kısıtı: oyunda kabartma çift başlı ok.
            if (!string.IsNullOrEmpty(block.Axis))
            {
                var anchor = BlockCellBuffer[0];
                LevelCanvasDrawer.Label(
                    _canvas.RectFor(block.X + anchor.x, block.Y + anchor.y),
                    block.Axis == "v" ? "↕" : "↔",
                    new Color(1f, 1f, 1f, 0.92f * alpha),
                    Mathf.Max(10, Mathf.RoundToInt(_canvas.CellSize * 0.5f)));
            }
        }

        /// <summary>
        /// Tek hücrenin gövdesi. Boşluk payı YALNIZ dış kenarlara uygulanır —
        /// bitişik hücreler kusursuz birleşsin ve L parçası tek gövde görünsün.
        /// Mesh üreticisi de tam olarak bu kuralı uyguluyor.
        /// </summary>
        void DrawBrickCell(BlockData block, Vector2Int cell, float inset, Texture2D shape,
            Color color, Vector2 offset)
        {
            int cx = block.X + cell.x, cy = block.Y + cell.y;
            var rect = _canvas.RectFor(cx, cy);

            float left   = BlockShape.Covers(block, cx - 1, cy) ? 0f : inset;
            float right  = BlockShape.Covers(block, cx + 1, cy) ? 0f : inset;
            float top    = BlockShape.Covers(block, cx, cy - 1) ? 0f : inset;
            float bottom = BlockShape.Covers(block, cx, cy + 1) ? 0f : inset;

            var body = new Rect(
                rect.x + left + offset.x, rect.y + top + offset.y,
                rect.width - left - right, rect.height - top - bottom);

            // Yuvarlak köşe dokusu yalnız DIŞ köşelerde anlamlı; bitişik
            // hücrelerde kenar payı sıfır olduğu için doku zaten düz basıyor.
            LevelEditorSkin.RoundedRect(body, color, shape);
        }

        static Color Tone(Color color, float tone, float alpha) =>
            new Color(color.r * tone, color.g * tone, color.b * tone, color.a * alpha);

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

                // Renk kuyruğu: ilk rengin ardından gelenler barın yanına küçük
                // kareler olarak dizilir. Kuyruk şemada vardı ama tuvalde hiç
                // görünmüyordu — görünmeyen veri yanlış yazılır.
                if (gate.Colors.Count > 1 && gate.Ice == 0) DrawGateQueue(gate, side, rect);
            }
        }

        void DrawGateQueue(GateData gate, Side side, Rect bar)
        {
            float chip = Mathf.Max(5f, _canvas.CellSize * 0.20f);
            bool horizontal = side == Side.North || side == Side.South;

            for (int i = 1; i < gate.Colors.Count; i++)
            {
                float offset = (chip + 2f) * i;
                var box = horizontal
                    ? new Rect(bar.center.x - chip * 0.5f,
                        side == Side.North ? bar.y - offset : bar.yMax + offset - chip, chip, chip)
                    : new Rect(side == Side.West ? bar.x - offset : bar.xMax + offset - chip,
                        bar.center.y - chip * 0.5f, chip, chip);

                LevelCanvasDrawer.Fill(box, ColorOf(gate.Colors[i]));
                LevelCanvasDrawer.Outline(box, new Color(0f, 0f, 0f, 0.5f));
            }
        }

        // ---------------- üreteçler ----------------

        /// <summary>
        /// Makinenin tuvaldeki yeri: tahtanın DIŞINDA, beslediği kolonun/satırın
        /// hizasında.
        ///
        /// DERS (tuvalin payı kadar çizebilirsin): Kutu hücre boyutunda olsaydı
        /// yakınlaştırmada tuval alanının dışına taşar ve komşu arayüzün üstüne
        /// bulaşırdı — <see cref="LevelCanvasDrawer"/> tahtayı 24 piksel payla
        /// yerleştiriyor. Bu yüzden kutu o payla sınırlanır; sıranın TAMAMI
        /// tuvalde değil, alt şeritte düzenlenir. Oyunun kendisi de makinenin
        /// üstünde yalnız sayacı ve SIRADAKİ bloğu gösteriyor.
        /// </summary>
        Rect GeneratorRect(ObstacleData generator, out Side side)
        {
            SideUtil.TryParse(LevelEditorIO.GetString(generator, "side", "N"), out side);
            int lane = LevelEditorIO.GeneratorLane(generator);
            float size = Mathf.Min(_canvas.CellSize * 0.9f, 22f);
            var board = _canvas.BoardRect;

            switch (side)
            {
                case Side.North:
                {
                    var cell = _canvas.RectFor(Mathf.Clamp(lane, 0, _data.Board.Width - 1), 0);
                    return new Rect(cell.center.x - size * 0.5f, board.y - size - 1f, size, size);
                }
                case Side.South:
                {
                    var cell = _canvas.RectFor(Mathf.Clamp(lane, 0, _data.Board.Width - 1), 0);
                    return new Rect(cell.center.x - size * 0.5f, board.yMax + 1f, size, size);
                }
                case Side.West:
                {
                    var cell = _canvas.RectFor(0, Mathf.Clamp(lane, 0, _data.Board.Height - 1));
                    return new Rect(board.x - size - 1f, cell.center.y - size * 0.5f, size, size);
                }
                default:
                {
                    var cell = _canvas.RectFor(0, Mathf.Clamp(lane, 0, _data.Board.Height - 1));
                    return new Rect(board.xMax + 1f, cell.center.y - size * 0.5f, size, size);
                }
            }
        }

        void DrawGenerators()
        {
            foreach (var obstacle in _data.Obstacles)
            {
                if (obstacle.Type != "generator") continue;

                var box = GeneratorRect(obstacle, out _);
                var queue = LevelEditorIO.GetQueue(obstacle);

                LevelCanvasDrawer.Fill(box, new Color(0.16f, 0.14f, 0.22f));
                LevelCanvasDrawer.Outline(box, new Color(0.55f, 0.80f, 0.95f), 2f);

                // Pencere: sıradaki bloğun rengi. Sıra boşsa uyarı olarak kırmızı.
                var window = new Rect(box.x + 3f, box.y + 3f, box.width - 6f, box.height - 6f);
                if (queue.Count > 0)
                {
                    var next = queue[0];
                    LevelCanvasDrawer.Fill(window,
                        ColorOf(next.Layers.Count > 0 ? next.Layers[0] : "red"));
                    LevelCanvasDrawer.Label(window, queue.Count.ToString(),
                        new Color(0f, 0f, 0f, 0.75f), Mathf.Max(8, Mathf.RoundToInt(box.height * 0.5f)));
                }
                else
                {
                    LevelCanvasDrawer.Fill(window, new Color(0.9f, 0.3f, 0.3f, 0.55f));
                    LevelCanvasDrawer.Label(window, "0", Color.white,
                        Mathf.Max(8, Mathf.RoundToInt(box.height * 0.5f)));
                }
            }
        }

        /// <summary>
        /// Perde içeriğini gerçek render'ın ÜSTÜNE soluk basar — yalnız editörde.
        /// Oyunda bu bloklar perde açılana kadar görünmez ve öyle kalmalı; ama
        /// düzenlenebilmeleri için tasarımcının onları görmesi şart.
        /// </summary>
        void DrawHiddenContents()
        {
            foreach (var obstacle in _data.Obstacles)
            {
                if (obstacle.Type != "curtain") continue;
                foreach (var hidden in LevelEditorIO.GetContents(obstacle))
                    DrawBlock(hidden, 0.45f);
            }
        }

        /// <summary>Gerçek render üstüne ince hücre ızgarası — hizalamayı okumak için.</summary>
        void DrawGridOverlay()
        {
            var line = new Color(1f, 1f, 1f, 0.07f);
            for (int y = 0; y < _data.Board.Height; y++)
                for (int x = 0; x < _data.Board.Width; x++)
                    if (Playable(x, y))
                        LevelCanvasDrawer.Outline(_canvas.RectFor(x, y), line);
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
                    case SelKind.Generator:
                    case SelKind.Queued:
                        if (selection.Index < _data.Obstacles.Count &&
                            _data.Obstacles[selection.Index].Type == "generator")
                            LevelCanvasDrawer.Highlight(
                                GeneratorRect(_data.Obstacles[selection.Index], out _), accent);
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

            if (_tool == Tool.Generator)
            {
                if (_canvas.TryEdge(mouse, _data.Board.Width, _data.Board.Height, out var gc, out var gs))
                {
                    var preview = LevelEditorIO.NewGenerator(gs, 0, 0);
                    LevelEditorIO.SetGeneratorLane(preview,
                        gs == Side.North || gs == Side.South ? gc.x : gc.y);
                    var box = GeneratorRect(preview, out _);
                    LevelCanvasDrawer.Fill(box, new Color(0.55f, 0.80f, 0.95f, 0.45f));
                    LevelCanvasDrawer.Outline(box, Color.white, 2f);
                }
                return;
            }

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

        // ---------------- alt şerit: kapılar & engeller ----------------

        /// <summary>
        /// Tahtanın altındaki nesne şeridi. Referans editördeki "BOX LANES"
        /// şeridinin karşılığı.
        ///
        /// DERS (tuval bazı verileri GÖSTEREMEZ): Kapının renk KUYRUĞU tuvalde
        /// birkaç piksellik bir kare olarak durur; sırasını değiştirmek ya da
        /// dördüncü rengi eklemek orada mümkün değil. Tahtaya sığmayan her veri
        /// için ikinci bir görünüm gerekir — liste. Kuyruk şemamızda başından
        /// beri vardı ama düzenlenemediği için 50 bölümde hiç kullanılmadı.
        /// </summary>
        void DrawGateStrip()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Height(GateStripHeight)))
            {
                using (LevelEditorSkin.BarScope())
                {
                    GUILayout.Label("KAPILAR & ENGELLER", LevelEditorSkin.Value, GUILayout.Width(150));
                    GUILayout.Label($"{_data.Gates.Count} kapı · {_data.Obstacles.Count} engel",
                        LevelEditorSkin.RowLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("kutuya tıkla = seç · renk kutusuna tıkla = kuyruğu düzenle",
                        LevelEditorSkin.RowLabel);
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(_gateStripScroll))
                {
                    _gateStripScroll = scroll.scrollPosition;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        for (int i = 0; i < _data.Gates.Count; i++) DrawGateChip(i);
                        for (int i = 0; i < _data.Obstacles.Count; i++) DrawObstacleChip(i);
                        GUILayout.FlexibleSpace();
                    }
                }
            }
        }

        void DrawGateChip(int index)
        {
            var gate = _data.Gates[index];
            bool selected = IsSelected(new Selection { Kind = SelKind.Gate, Index = index });

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(96)))
            {
                var header = GUILayoutUtility.GetRect(96, 18);
                if (GUI.Button(header, GUIContent.none, GUIStyle.none))
                    SelectOnly(new Selection { Kind = SelKind.Gate, Index = index });
                LevelCanvasDrawer.Label(header, $"{gate.Side} ({gate.X},{gate.Y})",
                    selected ? new Color(1f, 0.85f, 0.2f) : new Color(0.78f, 0.78f, 0.85f),
                    9, FontStyle.Bold);

                // Kuyruk kutuları: soldan sağa çıkış sırası. Tıklamak o sıradaki
                // rengi değiştirir, sağ tıklamak sıradan çıkarır.
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int q = 0; q < gate.Colors.Count; q++)
                    {
                        var box = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
                        LevelCanvasDrawer.Fill(box, ColorOf(gate.Colors[q]));
                        if (q == 0) LevelCanvasDrawer.Outline(box, Color.white, 1f);

                        int captured = q;
                        if (GUI.Button(box, new GUIContent("",
                                q == 0 ? "aktif renk — tıkla değiştir" : "kuyrukta " + (q + 1) + ". renk"),
                                GUIStyle.none))
                            ShowGateColorMenu(gate, captured);
                    }

                    if (gate.Colors.Count < 5 &&
                        GUILayout.Button(new GUIContent("+", "kuyruğa renk ekle"),
                            EditorStyles.miniButton, GUILayout.Width(18), GUILayout.Height(18)))
                    {
                        Record();
                        gate.Colors.Add(_gateColor.ToId());
                        AfterChange();
                    }
                }

                GUILayout.Label(gate.Ice > 0 ? $"uzunluk {gate.Length} · buz {gate.Ice}"
                                             : $"uzunluk {gate.Length}",
                    LevelEditorSkin.RowLabel);
            }
            GUILayout.Space(6);
        }

        void ShowGateColorMenu(GateData gate, int queueIndex)
        {
            var menu = new GenericMenu();
            foreach (var color in AllColors)
            {
                var captured = color;
                menu.AddItem(new GUIContent(color.ToString()),
                    gate.Colors[queueIndex] == color.ToId(),
                    () => { Record(); gate.Colors[queueIndex] = captured.ToId(); AfterChange(); });
            }

            if (gate.Colors.Count > 1)
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Kuyruktan çıkar"), false,
                    () => { Record(); gate.Colors.RemoveAt(queueIndex); AfterChange(); });
            }
            menu.ShowAsContext();
        }

        void DrawObstacleChip(int index)
        {
            if (_data.Obstacles[index].Type == "generator") DrawGeneratorChip(index);
            else DrawCurtainChip(index);
        }

        void DrawCurtainChip(int index)
        {
            var obstacle = _data.Obstacles[index];
            bool selected = IsSelected(new Selection { Kind = SelKind.Curtain, Index = index });
            int contents = LevelEditorIO.GetContents(obstacle).Count;

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(96)))
            {
                var header = GUILayoutUtility.GetRect(96, 18);
                if (GUI.Button(header, GUIContent.none, GUIStyle.none))
                    SelectOnly(new Selection { Kind = SelKind.Curtain, Index = index });
                LevelCanvasDrawer.Label(header, "perde",
                    selected ? new Color(1f, 0.85f, 0.2f) : new Color(0.85f, 0.65f, 0.2f),
                    9, FontStyle.Bold);

                var body = GUILayoutUtility.GetRect(96, 18);
                LevelCanvasDrawer.Fill(body, new Color(0.22f, 0.16f, 0.38f));
                LevelCanvasDrawer.Label(body, "sayaç " + LevelEditorIO.GetInt(obstacle, "count"),
                    new Color(1f, 0.9f, 0.55f), 9, FontStyle.Normal);

                GUILayout.Label(contents > 0 ? $"{contents} gizli blok" : "içi boş ⚠",
                    LevelEditorSkin.RowLabel);
            }
            GUILayout.Space(6);
        }

        /// <summary>
        /// Üreteç kutusu: makinenin SIRASI burada düzenlenir.
        ///
        /// DERS (sıra bir LİSTEDİR, tahta değil): Üretecin kuyruğu tahtada
        /// hiçbir hücreyi kaplamaz — tuvalde gösterilecek yeri yok. Kuyruğu
        /// düzenlemenin tek makul yeri bu şerit; kutular soldan sağa üretim
        /// sırasıdır, ilki "sıradaki"dir.
        /// </summary>
        void DrawGeneratorChip(int index)
        {
            var generator = _data.Obstacles[index];
            bool selected = IsSelected(new Selection { Kind = SelKind.Generator, Index = index });
            var queue = LevelEditorIO.GetQueue(generator);
            SideUtil.TryParse(LevelEditorIO.GetString(generator, "side", "N"), out var side);
            int lane = LevelEditorIO.GeneratorLane(generator);

            float width = Mathf.Max(110f, 24f + queue.Count * 20f);

            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
            {
                var header = GUILayoutUtility.GetRect(width, 18);
                if (GUI.Button(header, new GUIContent("", "üreteci seç"), GUIStyle.none))
                    SelectOnly(new Selection { Kind = SelKind.Generator, Index = index });
                LevelCanvasDrawer.Label(header, $"üreteç {side} · şerit {lane}",
                    selected ? new Color(1f, 0.85f, 0.2f) : new Color(0.55f, 0.80f, 0.95f),
                    9, FontStyle.Bold);

                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int q = 0; q < queue.Count; q++)
                    {
                        var box = GUILayoutUtility.GetRect(18, 18, GUILayout.Width(18), GUILayout.Height(18));
                        var block = queue[q];
                        LevelCanvasDrawer.Fill(box,
                            ColorOf(block.Layers.Count > 0 ? block.Layers[0] : "red"));
                        if (q == 0) LevelCanvasDrawer.Outline(box, Color.white, 1f);
                        if (block.Ice > 0)
                            LevelCanvasDrawer.Fill(box, new Color(0.62f, 0.85f, 1f, 0.5f));

                        int captured = q;
                        if (GUI.Button(box, new GUIContent("",
                                $"{q + 1}. {block.W}×{block.H}" + (q == 0 ? " (sıradaki)" : "")),
                                GUIStyle.none))
                            ShowQueueMenu(index, captured);
                    }

                    if (GUILayout.Button(new GUIContent("+", "fırçadaki bloğu sıranın sonuna ekle"),
                            EditorStyles.miniButton, GUILayout.Width(18), GUILayout.Height(18)))
                    {
                        Record();
                        queue.Add(BrushBlock(0, 0));
                        LevelEditorIO.SetQueue(generator, queue);
                        AfterChange();
                    }
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Label(queue.Count > 0
                        ? $"{queue.Count} blok sırada"
                        : "sıra BOŞ ⚠ makine hiç çalışmaz",
                    LevelEditorSkin.RowLabel);
            }
            GUILayout.Space(6);
        }

        /// <summary>Kuyruktaki bir bloğun renk/sıra/silme menüsü.</summary>
        void ShowQueueMenu(int obstacleIndex, int queueIndex)
        {
            if (obstacleIndex < 0 || obstacleIndex >= _data.Obstacles.Count) return;

            var generator = _data.Obstacles[obstacleIndex];
            if (generator.Type != "generator") return;
            if (queueIndex < 0 || queueIndex >= LevelEditorIO.GetQueue(generator).Count) return;

            var menu = new GenericMenu();

            foreach (var color in AllColors)
            {
                var captured = color;
                var queue = LevelEditorIO.GetQueue(generator);
                bool on = queue[queueIndex].Layers.Count > 0 &&
                          queue[queueIndex].Layers[0] == captured.ToId();
                menu.AddItem(new GUIContent("Renk/" + color), on, () =>
                {
                    Record();
                    var edited = LevelEditorIO.GetQueue(generator);
                    if (edited[queueIndex].Layers.Count == 0) edited[queueIndex].Layers.Add(captured.ToId());
                    else edited[queueIndex].Layers[0] = captured.ToId();
                    LevelEditorIO.SetQueue(generator, edited);
                    AfterChange();
                });
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Fırçanın şeklini uygula"), false, () =>
            {
                Record();
                var edited = LevelEditorIO.GetQueue(generator);
                edited[queueIndex] = BrushBlock(0, 0);
                LevelEditorIO.SetQueue(generator, edited);
                AfterChange();
            });

            if (queueIndex > 0)
                menu.AddItem(new GUIContent("Sırada öne al"), false, () => MoveInQueue(generator, queueIndex, -1));
            menu.AddItem(new GUIContent("Sırada geriye al"), false, () => MoveInQueue(generator, queueIndex, 1));

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Sıradan çıkar"), false, () =>
            {
                Record();
                var edited = LevelEditorIO.GetQueue(generator);
                edited.RemoveAt(queueIndex);
                LevelEditorIO.SetQueue(generator, edited);
                AfterChange();
            });
            menu.ShowAsContext();
        }

        void MoveInQueue(ObstacleData generator, int from, int step)
        {
            var queue = LevelEditorIO.GetQueue(generator);
            int to = from + step;
            if (to < 0 || to >= queue.Count) return;

            Record();
            (queue[from], queue[to]) = (queue[to], queue[from]);
            LevelEditorIO.SetQueue(generator, queue);
            AfterChange();
        }

        /// <summary>Seçimi tek nesneye indirger ve Seç aracına döner.</summary>
        void SelectOnly(Selection selection)
        {
            RequestTool(Tool.Select);
            _selections.Clear();
            _selections.Add(selection);
        }

        // ---------------- sağ dok: bölüm tarayıcısı ----------------

        void DrawLibraryDock()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(LibraryWidth)))
            {
                using (LevelEditorSkin.BarScope())
                    GUILayout.Label("BÖLÜM TARAYICISI", LevelEditorSkin.Value);

                _librarySearch = EditorGUILayout.TextField(_librarySearch, EditorStyles.toolbarSearchField);

                using (var scroll = new EditorGUILayout.ScrollViewScope(_libraryScroll))
                {
                    _libraryScroll = scroll.scrollPosition;

                    var list = LevelLibrary.Entries;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var entry = list[i];
                        if (!Matches(entry, _librarySearch)) continue;
                        DrawLibraryRow(entry, i);
                    }
                }

                DrawLibraryActions();
            }
        }

        static bool Matches(LevelLibrary.Entry entry, string search) =>
            string.IsNullOrWhiteSpace(search) ||
            entry.Name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            (entry.Data != null && entry.Data.Difficulty.IndexOf(
                search, System.StringComparison.OrdinalIgnoreCase) >= 0);

        void DrawLibraryRow(LevelLibrary.Entry entry, int order)
        {
            bool current = entry.Path == _path;
            var row = GUILayoutUtility.GetRect(LibraryWidth - 8f, 22f);

            if (current) EditorGUI.DrawRect(row, new Color(0.28f, 0.36f, 0.52f, 0.55f));
            else if (row.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(row, new Color(1f, 1f, 1f, 0.05f));

            StatusDot(new Rect(row.x + 4f, row.y + 7f, 8f, 8f), entry.Status);

            var nameStyle = new GUIStyle(LevelEditorSkin.RowLabel);
            nameStyle.normal.textColor = current ? Color.white : new Color(0.8f, 0.8f, 0.86f);
            GUI.Label(new Rect(row.x + 18f, row.y + 3f, 108f, 16f), $"{order + 1}. {entry.Name}", nameStyle);

            var metaStyle = new GUIStyle(LevelEditorSkin.RowLabel) { alignment = TextAnchor.MiddleRight };
            metaStyle.normal.textColor = new Color(0.55f, 0.55f, 0.62f);
            GUI.Label(new Rect(row.xMax - 74f, row.y + 3f, 70f, 16f),
                entry.Data != null ? $"{entry.Width}×{entry.Height} · {entry.BlockCount}b" : "bozuk",
                metaStyle);

            if (GUI.Button(row, new GUIContent("", entry.Summary), GUIStyle.none) && !current)
                RequestOpen(entry.Path);
        }

        /// <summary>
        /// Bölümü oynanış sırasında taşır ve açık kopyanın numarasını eşitler.
        /// Taşımak bütün bölümlerin `displayNumber` alanını yeniliyor.
        /// </summary>
        void MoveInOrder(int step)
        {
            if (!LevelLibrary.Reorder(_path, step)) { Say("Katalogda taşınamadı"); return; }

            SyncDisplayNumberFromDisk();
            int index = LevelLibrary.IndexOf(_path);
            Say(index >= 0
                ? $"Sıra değişti — bu bölüm artık {index + 1}."
                : "Sıra değişti");
        }

        void DrawLibraryActions()
        {
            using (LevelEditorSkin.BarScope())
            using (new EditorGUI.DisabledScope(_path == null))
            {
                if (LevelEditorSkin.BarButton("▲", 26f, "Oynanış sırasında yukarı taşı"))
                    MoveInOrder(-1);

                if (LevelEditorSkin.BarButton("▼", 26f, "Oynanış sırasında aşağı taşı"))
                    MoveInOrder(1);

                if (LevelEditorSkin.BarButton("Çoğalt", 56f, "Kopyasını setin sonuna ekle"))
                {
                    string source = _path;
                    EditorApplication.delayCall += () =>
                    {
                        if (this == null || !ConfirmDiscard()) return;
                        LoadFrom(LevelLibrary.Duplicate(source));
                    };
                }

                if (LevelEditorSkin.BarButton("Sil", 34f, "Dosyayı ve katalog girdisini sil",
                        LevelEditorSkin.Danger))
                {
                    string doomed = _path;
                    EditorApplication.delayCall += () => DeleteLevel(doomed);
                }
            }
        }
    }
}
