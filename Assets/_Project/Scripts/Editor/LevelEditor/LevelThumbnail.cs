using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Bir <see cref="LevelData"/>'yı küçük bir dokuya çizer — galeri minyatürü.
    ///
    /// DERS (neden minyatür bir araç özelliğidir): 50 bölümlük bir sette
    /// "hangi bölüm neye benziyor" sorusunun tek cevabı dosyaları tek tek
    /// açmaksa, tasarımcı setin BÜTÜNÜNÜ hiç görmez. Bütünü görmek, tekrar eden
    /// tahtaları ve zorluk sıçramalarını fark etmenin tek yoludur.
    ///
    /// DERS (neden GUI değil doku): Izgarayı OnGUI ile çizmek her karede
    /// yüzlerce DrawRect demektir; 50 minyatür × 200 hücre = 10.000 çağrı/kare.
    /// Bir kere <see cref="Texture2D"/>'ye piksel yazıp önbelleğe koymak, çizimi
    /// tek bir doku kopyalamasına indirir.
    /// </summary>
    public static class LevelThumbnail
    {
        /// <summary>Bir hücrenin minyatürdeki piksel kenarı.</summary>
        const int Cell = 12;

        /// <summary>Kapı barları tahtanın dışına taştığı için kenar payı.</summary>
        const int Pad = 4;

        // Zemin ve çerçeve renkleri OYUNUN görsel ayarından gelir; minyatür
        // "bölüm neye benziyor" sorusunu cevaplıyorsa oyunun rengini
        // göstermelidir. Ayar yüklenmemişse koddaki varsayılana düşer.
        static Color Background => Cfg != null ? Cfg.backgroundOuter : new Color(0.08f, 0.06f, 0.18f);
        static Color CellLight => Cfg != null ? Cfg.floorColorA : new Color(0.17f, 0.15f, 0.31f);
        static Color CellDark => Cfg != null ? Cfg.floorColorB : new Color(0.14f, 0.12f, 0.27f);
        static Color CellHole => Background;
        static Color WallColor => Cfg != null ? Cfg.frameColor : new Color(0.30f, 0.26f, 0.58f);

        static BlockOut.Runtime.Config.BlockVisualConfigSO Cfg =>
            BlockOut.Runtime.View.VisualSettings.Current;
        static readonly Color CurtainColor = new Color(0.24f, 0.17f, 0.40f);
        static readonly Color CurtainEdge = new Color(0.85f, 0.65f, 0.20f);
        static readonly Color IceTint = new Color(0.62f, 0.85f, 1f);

        /// <summary>
        /// Bölümü yeni bir dokuya çizer. Çağıran dokunun sahibidir —
        /// işi bitince <see cref="Object.DestroyImmediate(Object)"/> ile yok etmeli.
        /// </summary>
        public static Texture2D Render(LevelData data, ColorPaletteSO palette)
        {
            int boardW = Mathf.Max(1, data.Board.Width);
            int boardH = Mathf.Max(1, data.Board.Height);
            int width = boardW * Cell + Pad * 2;
            int height = boardH * Cell + Pad * 2;

            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Background;

            var painter = new Painter(pixels, width, height);

            DrawCells(painter, data);
            DrawWalls(painter, data);
            DrawCurtains(painter, data, palette);
            foreach (var block in data.Blocks) DrawBlock(painter, block, palette, 1f);
            DrawGates(painter, data, palette);
            DrawGenerators(painter, data, palette);

            // Nokta filtresi: minyatür büyütülünce hücreler keskin kalsın,
            // bulanık bir renk lekesine dönüşmesin.
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // ---------------- katmanlar ----------------

        static void DrawCells(Painter painter, LevelData data)
        {
            for (int y = 0; y < data.Board.Height; y++)
                for (int x = 0; x < data.Board.Width; x++)
                    painter.FillCell(x, y, 1, 1, !Playable(data, x, y)
                        ? CellHole
                        : (x + y) % 2 == 0 ? CellLight : CellDark);
        }

        static void DrawWalls(Painter painter, LevelData data)
        {
            foreach (var wall in data.Board.Walls)
            {
                if (!SideUtil.TryParse(wall.Side, out var side)) continue;
                bool horizontal = side == Side.North || side == Side.South;

                for (int i = 0; i < Mathf.Max(1, wall.Length); i++)
                {
                    int cx = horizontal ? wall.X + i : wall.X;
                    int cy = horizontal ? wall.Y : wall.Y + i;
                    painter.FillEdge(cx, cy, side, WallColor);
                }
            }
        }

        /// <summary>
        /// Üreteçler tahtanın DIŞINDA durur; minyatürde kenar payına küçük bir
        /// kutu olarak çizilir ve rengi sıradaki bloğu gösterir. Böylece
        /// galeride "bu bölümde üreteç var mı" bakışla anlaşılır.
        /// </summary>
        static void DrawGenerators(Painter painter, LevelData data, ColorPaletteSO palette)
        {
            foreach (var obstacle in data.Obstacles)
            {
                if (obstacle.Type != "generator") continue;
                if (!SideUtil.TryParse(LevelEditorIO.GetString(obstacle, "side", "N"), out var side))
                    continue;

                var queue = LevelEditorIO.GetQueue(obstacle);
                var color = queue.Count > 0
                    ? ColorOf(palette, queue[0].Layers.Count > 0 ? queue[0].Layers[0] : "red")
                    : new Color(0.9f, 0.3f, 0.3f);

                int lane = LevelEditorIO.GeneratorLane(obstacle);
                bool horizontal = side == Side.North || side == Side.South;
                int cx = horizontal ? Mathf.Clamp(lane, 0, data.Board.Width - 1)
                                    : side == Side.West ? 0 : data.Board.Width - 1;
                int cy = horizontal ? (side == Side.North ? 0 : data.Board.Height - 1)
                                    : Mathf.Clamp(lane, 0, data.Board.Height - 1);

                painter.FillMachine(cx, cy, side, color);
            }
        }

        static void DrawCurtains(Painter painter, LevelData data, ColorPaletteSO palette)
        {
            foreach (var obstacle in data.Obstacles)
            {
                if (obstacle.Type != "curtain") continue;

                int x = LevelEditorIO.GetInt(obstacle, "x");
                int y = LevelEditorIO.GetInt(obstacle, "y");
                int w = LevelEditorIO.GetInt(obstacle, "w", 1);
                int h = LevelEditorIO.GetInt(obstacle, "h", 1);

                // Gizli içerik soluk çizilir: minyatürde perdenin ARDINDA ne
                // olduğu görünsün, ama perdenin kendisi baskın kalsın.
                foreach (var hidden in LevelEditorIO.GetContents(obstacle))
                    DrawBlock(painter, hidden, palette, 0.35f);

                painter.BlendCell(x, y, w, h, CurtainColor, 0.82f);
                painter.OutlineCell(x, y, w, h, CurtainEdge);
            }
        }

        static readonly List<Vector2Int> CellBuffer = new List<Vector2Int>();

        static void DrawBlock(Painter painter, BlockData block, ColorPaletteSO palette, float alpha)
        {
            BlockShape.LocalCells(block, CellBuffer);
            if (CellBuffer.Count == 0) return;

            var outer = ColorOf(palette, block.Layers.Count > 0 ? block.Layers[0] : "red");
            bool layered = block.Layers.Count > 1;
            var inner = layered ? ColorOf(palette, block.Layers[1]) : outer;

            foreach (var cell in CellBuffer)
            {
                int cx = block.X + cell.x, cy = block.Y + cell.y;
                painter.BlendCell(cx, cy, 1, 1, outer, alpha);

                // Katmanlı blok: içine ikinci rengin küçük karesi. Ayrı bir
                // simge yerine rengin kendisi — minyatürde 3 piksel bile okunur.
                if (layered) painter.BlendInset(cx, cy, 3, inner, alpha);
                if (block.Ice > 0) painter.BlendCell(cx, cy, 1, 1, IceTint, 0.5f * alpha);
            }
        }

        static void DrawGates(Painter painter, LevelData data, ColorPaletteSO palette)
        {
            foreach (var gate in data.Gates)
            {
                if (!SideUtil.TryParse(gate.Side, out var side)) continue;
                bool horizontal = side == Side.North || side == Side.South;

                var color = gate.Ice > 0
                    ? IceTint
                    : ColorOf(palette, gate.Colors.Count > 0 ? gate.Colors[0] : "red");

                for (int i = 0; i < Mathf.Max(1, gate.Length); i++)
                {
                    int cx = horizontal ? gate.X + i : gate.X;
                    int cy = horizontal ? gate.Y : gate.Y + i;
                    painter.FillGate(cx, cy, side, color);
                }
            }
        }

        // ---------------- yardımcılar ----------------

        static bool Playable(LevelData data, int x, int y) =>
            x >= 0 && y >= 0 && x < data.Board.Width && y < data.Board.Height &&
            y < data.Board.Rows.Count && x < data.Board.Rows[y].Length &&
            char.ToUpperInvariant(data.Board.Rows[y][x]) == 'X';

        static Color ColorOf(ColorPaletteSO palette, string id)
        {
            if (!BlockColorUtil.TryParse(id, out var parsed)) return Color.magenta;
            var entry = palette != null ? palette.Get(parsed) : null;
            return entry != null ? entry.uiColor : Color.magenta;
        }

        /// <summary>
        /// Piksel dizisi üzerine hücre uzayında çizim yapan ince sarmalayıcı.
        ///
        /// Hücre uzayı sözleşmesi runtime ile aynı (y AŞAĞI artar); doku uzayı
        /// ise ters (y YUKARI artar). Çevrim tek yerde, <see cref="Put"/>'ta yapılır —
        /// böylece çizim kodu baş aşağı düşünmek zorunda kalmaz.
        /// </summary>
        readonly struct Painter
        {
            readonly Color[] _pixels;
            readonly int _width, _height;

            public Painter(Color[] pixels, int width, int height)
            {
                _pixels = pixels;
                _width = width;
                _height = height;
            }

            void Put(int px, int py, Color color, float alpha)
            {
                if (px < 0 || py < 0 || px >= _width || py >= _height) return;
                int index = (_height - 1 - py) * _width + px;
                _pixels[index] = alpha >= 1f
                    ? color
                    : Color.Lerp(_pixels[index], color, Mathf.Clamp01(alpha));
            }

            void FillRect(int px, int py, int w, int h, Color color, float alpha)
            {
                for (int y = py; y < py + h; y++)
                    for (int x = px; x < px + w; x++)
                        Put(x, y, color, alpha);
            }

            public void FillCell(int cx, int cy, int cw, int ch, Color color) =>
                BlendCell(cx, cy, cw, ch, color, 1f);

            public void BlendCell(int cx, int cy, int cw, int ch, Color color, float alpha) =>
                FillRect(Pad + cx * Cell, Pad + cy * Cell, cw * Cell, ch * Cell, color, alpha);

            /// <summary>Hücrenin içine, kenarlardan <paramref name="inset"/> piksel boşluk bırakan kare.</summary>
            public void BlendInset(int cx, int cy, int inset, Color color, float alpha) =>
                FillRect(Pad + cx * Cell + inset, Pad + cy * Cell + inset,
                    Cell - inset * 2, Cell - inset * 2, color, alpha);

            public void OutlineCell(int cx, int cy, int cw, int ch, Color color)
            {
                int px = Pad + cx * Cell, py = Pad + cy * Cell;
                int w = cw * Cell, h = ch * Cell;
                FillRect(px, py, w, 1, color, 1f);
                FillRect(px, py + h - 1, w, 1, color, 1f);
                FillRect(px, py, 1, h, color, 1f);
                FillRect(px + w - 1, py, 1, h, color, 1f);
            }

            /// <summary>Kapı barı: hücrenin dış kenarına yapışık ince şerit.</summary>
            public void FillGate(int cx, int cy, Side side, Color color)
            {
                const int t = 3;
                int px = Pad + cx * Cell, py = Pad + cy * Cell;
                switch (side)
                {
                    case Side.North: FillRect(px, py - t, Cell, t, color, 1f); break;
                    case Side.South: FillRect(px, py + Cell, Cell, t, color, 1f); break;
                    case Side.West:  FillRect(px - t, py, t, Cell, color, 1f); break;
                    default:         FillRect(px + Cell, py, t, Cell, color, 1f); break;
                }
            }

            /// <summary>Üreteç makinesi: kenar payına oturan küçük dolu kare.</summary>
            public void FillMachine(int cx, int cy, Side side, Color color)
            {
                int size = Pad;
                int px = Pad + cx * Cell, py = Pad + cy * Cell;
                int ox = px + (Cell - size) / 2, oy = py + (Cell - size) / 2;

                switch (side)
                {
                    case Side.North: FillRect(ox, py - size, size, size, color, 1f); break;
                    case Side.South: FillRect(ox, py + Cell, size, size, color, 1f); break;
                    case Side.West:  FillRect(px - size, oy, size, size, color, 1f); break;
                    default:         FillRect(px + Cell, oy, size, size, color, 1f); break;
                }
            }

            /// <summary>İç duvar: iki hücre arasındaki sınıra oturan çizgi.</summary>
            public void FillEdge(int cx, int cy, Side side, Color color)
            {
                const int t = 2;
                int px = Pad + cx * Cell, py = Pad + cy * Cell;
                switch (side)
                {
                    case Side.North: FillRect(px, py - t / 2, Cell, t, color, 1f); break;
                    case Side.South: FillRect(px, py + Cell - t / 2, Cell, t, color, 1f); break;
                    case Side.West:  FillRect(px - t / 2, py, t, Cell, color, 1f); break;
                    default:         FillRect(px + Cell - t / 2, py, t, Cell, color, 1f); break;
                }
            }
        }
    }
}
