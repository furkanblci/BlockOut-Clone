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
        public readonly List<IObstacle> Obstacles = new List<IObstacle>();

        int _nextBlockId;

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

            foreach (var b in data.Blocks)
                level.Blocks.Add(level.BuildBlock(b));

            foreach (var g in data.Gates)
            {
                if (!SideUtil.TryParse(g.Side, out var side))
                    throw new FormatException($"Kapı kenarı çözümlenemedi: '{g.Side}'");

                var gate = new GateModel
                {
                    X = g.X, Y = g.Y, Side = side, Length = g.Length, IceCount = g.Ice
                };
                foreach (var colorId in g.Colors)
                {
                    if (!BlockColorUtil.TryParse(colorId, out var color))
                        throw new FormatException($"Kapı rengi çözümlenemedi: '{colorId}'");
                    gate.ColorQueue.Add(color);
                }
                level.Gates.Add(gate);
            }

            foreach (var o in data.Obstacles)
                level.Obstacles.Add(ObstacleFactory.Create(o, level.BuildBlock));

            return level;
        }

        /// <summary>BlockData → BlockModel. Perde içerikleri de aynı yoldan geçer;
        /// Id sayacı ortak olduğu için sonradan doğan bloklar çakışmaz.</summary>
        BlockModel BuildBlock(BlockData b)
        {
            var block = new BlockModel
            {
                Id = _nextBlockId++,
                IceCount = b.Ice,
                Position = new Vector2(b.X, b.Y)
            };

            // Şekil kuralı BlockShape'te tek yerde: maske varsa maskeden,
            // yoksa w×h dikdörtgeninden.
            var cells = new List<Vector2Int>();
            BlockShape.LocalCells(b, cells);
            if (cells.Count == 0)
                throw new FormatException($"Blok hücre maskesi boş (x:{b.X}, y:{b.Y}).");
            block.SetCells(cells);

            foreach (var layerId in b.Layers)
            {
                if (!BlockColorUtil.TryParse(layerId, out var color))
                    throw new FormatException($"Blok rengi çözümlenemedi: '{layerId}'");
                block.Layers.Add(color);
            }
            return block;
        }

        /// <summary>
        /// Renk oyunda HERHANGİ bir yerde hâlâ var mı? Görünen dış katmanlar,
        /// gizli iç katmanlar VE açılmamış perde içerikleri sayılır — bir kapı
        /// ancak rengi kalıcı olarak tükendiyse ghost olur (video kuralı).
        /// </summary>
        public bool AnyColorRemaining(BlockColor color)
        {
            foreach (var b in Blocks)
                if (b.Layers.Contains(color))
                    return true;

            foreach (var o in Obstacles)
                if (o is CurtainModel curtain && !curtain.IsOpen)
                    foreach (var b in curtain.Contents)
                        if (b.Layers.Contains(color))
                            return true;

            return false;
        }

        /// <summary>Açılmamış perdelerde gizli blok var mı? (Kazanma koşulu bunu da bekler.)</summary>
        public bool HasPendingContent()
        {
            foreach (var o in Obstacles)
                if (o is CurtainModel curtain && !curtain.IsOpen && curtain.Contents.Count > 0)
                    return true;
            return false;
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
                    b.CollectColliders(output);   // polyomino: hücre başına kutu
            foreach (var o in Obstacles)
                o.CollectColliders(output);
        }
    }
}
