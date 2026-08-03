using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Core
{
    /// <summary>
    /// Bir bölümün ÇALIŞAN oyun durumu: tahta + bloklar + kapılar.
    /// <see cref="Build"/> fabrikası, doğrulanmış LevelData DTO'sunu model
    /// nesnelerine çevirir — JSON dünyası ile oyun dünyası arasındaki tek kapı.
    /// </summary>
    public sealed class LevelModel
    {
        public BoardModel Board;
        public readonly List<BlockModel> Blocks = new List<BlockModel>();
        public readonly List<GateModel> Gates = new List<GateModel>();

        public static LevelModel Build(LevelData data)
        {
            var level = new LevelModel
            {
                Board = new BoardModel(data.Board.Width, data.Board.Height)
            };

            for (int y = 0; y < data.Board.Height; y++)
            {
                string row = data.Board.Rows[y];
                for (int x = 0; x < data.Board.Width; x++)
                    level.Board.SetPlayable(x, y, char.ToUpperInvariant(row[x]) == 'X');
            }

            foreach (var w in data.Board.Walls)
            {
                if (!SideUtil.TryParse(w.Side, out var side))
                    throw new FormatException($"Duvar kenarı çözümlenemedi: '{w.Side}'");

                // Kenar boyunca uzat: yatay kenarlar +x, dikey kenarlar +y yönünde.
                for (int i = 0; i < w.Length; i++)
                {
                    var first = EdgeId.OfCellSide(w.X, w.Y, side);
                    level.Board.Walls.Add(first.Horizontal
                        ? EdgeId.OfCellSide(w.X + i, w.Y, side)
                        : EdgeId.OfCellSide(w.X, w.Y + i, side));
                }
            }

            int nextId = 0;
            foreach (var b in data.Blocks)
            {
                var block = new BlockModel
                {
                    Id = nextId++,
                    W = b.W,
                    H = b.H,
                    Position = new Vector2(b.X, b.Y)
                };
                foreach (var layerId in b.Layers)
                {
                    if (!BlockColorUtil.TryParse(layerId, out var color))
                        throw new FormatException($"Blok rengi çözümlenemedi: '{layerId}'");
                    block.Layers.Add(color);
                }
                level.Blocks.Add(block);
            }

            foreach (var g in data.Gates)
            {
                if (!SideUtil.TryParse(g.Side, out var side))
                    throw new FormatException($"Kapı kenarı çözümlenemedi: '{g.Side}'");

                var gate = new GateModel { X = g.X, Y = g.Y, Side = side, Length = g.Length };
                foreach (var colorId in g.Colors)
                {
                    if (!BlockColorUtil.TryParse(colorId, out var color))
                        throw new FormatException($"Kapı rengi çözümlenemedi: '{colorId}'");
                    gate.ColorQueue.Add(color);
                }
                level.Gates.Add(gate);
            }

            return level;
        }

        public void RemoveBlock(BlockModel block) => Blocks.Remove(block);

        /// <summary>
        /// Sürüklenen blok için çarpışma listesi: statik dünya + park halindeki
        /// DİĞER bloklar. Liste sürükleme başında BİR KEZ kurulur (statik kalan
        /// dünyada her kare yeniden taramak israf olur — M6 GC dersinin ön hazırlığı).
        /// </summary>
        public void CollectObstacles(List<Aabb> output, BlockModel exclude)
        {
            Board.CollectStaticColliders(output);
            foreach (var b in Blocks)
                if (!ReferenceEquals(b, exclude))
                    output.Add(b.Rect);
        }
    }
}
