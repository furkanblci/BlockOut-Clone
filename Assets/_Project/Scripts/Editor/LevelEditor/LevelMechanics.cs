using BlockOut.Core;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Bir bölümün HANGİ mekanikleri kullandığını veriden çıkarır.
    ///
    /// DERS (mekanik tanıtım sırası bir TASARIM kararıdır): Oyun her yeni
    /// mekaniği ilk göründüğü bölümde tanıtıyor (`NewItemPanel`). Bu, sıranın
    /// içerik olduğu anlamına gelir: perde 12. bölümde çıkıyorsa oyuncu onu
    /// orada öğrenir, 40. bölümde çıkıyorsa oraya kadar hiç görmez. Ama bu sıra
    /// hiçbir yerde YAZILI değil — 50 JSON'un içine gömülü. Araç onu okuyup
    /// gösterirse sıra denetlenebilir hâle gelir; okumazsa kimse fark etmeden
    /// bozulur.
    ///
    /// DERS (türetilmiş veriyi elle yazma): Bu bayrakları bölüm dosyasına bir
    /// alan olarak eklemek cazip görünür ama iki gerçek kaynağı olur ve
    /// kaçınılmaz olarak ayrışırlar. Her zaman veriden HESAPLA.
    /// </summary>
    [System.Flags]
    public enum Mechanic
    {
        None        = 0,
        Layers      = 1 << 0,
        IceBlock    = 1 << 1,
        IceGate     = 1 << 2,
        GateQueue   = 1 << 3,
        Curtain     = 1 << 4,
        Generator   = 1 << 5,
        Directional = 1 << 6,
        Polyomino   = 1 << 7,
        Wall        = 1 << 8,
        ShapedBoard = 1 << 9
    }

    public static class LevelMechanics
    {
        /// <summary>Rozet harfi kısa olmalı — kartta 12 piksele sığıyor.</summary>
        public static readonly (Mechanic Flag, string Badge, string Label, Color Tint)[] All =
        {
            (Mechanic.Layers,      "K", "Katmanlı blok",      new Color(0.62f, 0.45f, 0.90f)),
            (Mechanic.IceBlock,    "B", "Buzlu blok",         new Color(0.55f, 0.82f, 0.98f)),
            (Mechanic.IceGate,     "BK", "Buzlu kapı",        new Color(0.42f, 0.70f, 0.92f)),
            (Mechanic.GateQueue,   "Q", "Kapı renk kuyruğu",  new Color(0.95f, 0.60f, 0.35f)),
            (Mechanic.Curtain,     "P", "Perde",              new Color(0.90f, 0.72f, 0.28f)),
            (Mechanic.Generator,   "Ü", "Blok üreteci",       new Color(0.45f, 0.85f, 0.70f)),
            (Mechanic.Directional, "↔", "Yönlü blok",         new Color(0.95f, 0.45f, 0.60f)),
            (Mechanic.Polyomino,   "L", "Polyomino şekil",    new Color(0.70f, 0.75f, 0.85f)),
            (Mechanic.Wall,        "D", "İç duvar",           new Color(0.55f, 0.50f, 0.75f)),
            (Mechanic.ShapedBoard, "S", "Şekilli tahta",      new Color(0.50f, 0.55f, 0.62f))
        };

        public static Mechanic Of(LevelData data)
        {
            if (data == null) return Mechanic.None;
            var found = Mechanic.None;

            foreach (var block in data.Blocks) found |= OfBlock(block);

            foreach (var gate in data.Gates)
            {
                if (gate.Ice > 0) found |= Mechanic.IceGate;
                if (gate.Colors.Count > 1) found |= Mechanic.GateQueue;
            }

            foreach (var obstacle in data.Obstacles)
            {
                if (obstacle.Type == "curtain")
                {
                    found |= Mechanic.Curtain;
                    // Perdenin ARDINDAKİ bloklar da bölümün mekaniğidir —
                    // gizli olmaları saymamak için sebep değil.
                    foreach (var hidden in LevelEditorIO.GetContents(obstacle))
                        found |= OfBlock(hidden);
                }
                else if (obstacle.Type == "generator")
                {
                    found |= Mechanic.Generator;
                    foreach (var queued in LevelEditorIO.GetQueue(obstacle))
                        found |= OfBlock(queued);
                }
            }

            if (data.Board != null)
            {
                if (data.Board.Walls.Count > 0) found |= Mechanic.Wall;
                foreach (var row in data.Board.Rows)
                    if (row.IndexOf('.') >= 0) { found |= Mechanic.ShapedBoard; break; }
            }

            return found;
        }

        static Mechanic OfBlock(BlockData block)
        {
            var found = Mechanic.None;
            if (block.Layers.Count > 1) found |= Mechanic.Layers;
            if (block.Ice > 0) found |= Mechanic.IceBlock;
            if (!string.IsNullOrEmpty(block.Axis)) found |= Mechanic.Directional;
            if (block.Cells != null && block.Cells.Count > 0) found |= Mechanic.Polyomino;
            return found;
        }
    }
}
