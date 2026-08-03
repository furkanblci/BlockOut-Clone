using System;

namespace BlockOut.Core
{
    /// <summary>
    /// Hücre kenarı yönleri. JSON'da "N/E/S/W" olarak yazılır.
    /// Hücre uzayı sözleşmemiz: x sağa, y AŞAĞI artar (JSON rows[0] = en üst satır).
    /// North = -y yönü (üst), South = +y (alt), West = -x (sol), East = +x (sağ).
    /// </summary>
    public enum Side
    {
        North,
        East,
        South,
        West
    }

    public static class SideUtil
    {
        public static bool TryParse(string id, out Side side)
        {
            switch (id)
            {
                case "N": case "n": side = Side.North; return true;
                case "E": case "e": side = Side.East;  return true;
                case "S": case "s": side = Side.South; return true;
                case "W": case "w": side = Side.West;  return true;
                default: side = default; return false;
            }
        }

        public static string ToId(this Side side)
        {
            switch (side)
            {
                case Side.North: return "N";
                case Side.East:  return "E";
                case Side.South: return "S";
                default:         return "W";
            }
        }
    }

    /// <summary>
    /// Eksen hizalı sınırlayıcı kutu (axis-aligned bounding box), hücre uzayında.
    /// DragSolver'ın tek çarpışma ilkeli budur — fizik motoru YOK, saf matematik.
    /// Duvarlar sıfır kalınlıklı AABB olarak temsil edilir (Min == Max tek eksende);
    /// epsilon'lu örtüşme testi sayesinde bitişik durmak çarpışma sayılmaz.
    /// </summary>
    public readonly struct Aabb
    {
        public readonly float MinX, MinY, MaxX, MaxY;

        public Aabb(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY;
        }

        public static Aabb FromCell(int x, int y) => new Aabb(x, y, x + 1, y + 1);

        public static Aabb FromRect(float x, float y, float w, float h) => new Aabb(x, y, x + w, y + h);

        /// <summary>Bu kutu (her yandan eps kadar küçültülmüş) diğeriyle örtüşüyor mu?</summary>
        public bool Overlaps(in Aabb o, float eps) =>
            MinX + eps < o.MaxX && MaxX - eps > o.MinX &&
            MinY + eps < o.MaxY && MaxY - eps > o.MinY;
    }

    /// <summary>
    /// Izgara kenarının normalize kimliği, köşe-uzayında:
    /// Horizontal=true  → (X,Y)-(X+1,Y) yatay segmenti; (X,Y-1) ile (X,Y) hücrelerini ayırır.
    /// Horizontal=false → (X,Y)-(X,Y+1) dikey segmenti; (X-1,Y) ile (X,Y) hücrelerini ayırır.
    /// Normalizasyon sayesinde "hücre (2,3)'ün kuzeyi" ile "hücre (2,2)'nin güneyi"
    /// AYNI EdgeId'ye düşer — HashSet aramaları güvenilir olur.
    /// </summary>
    public readonly struct EdgeId : IEquatable<EdgeId>
    {
        public readonly int X, Y;
        public readonly bool Horizontal;

        public EdgeId(int x, int y, bool horizontal)
        {
            X = x; Y = y; Horizontal = horizontal;
        }

        public static EdgeId OfCellSide(int cellX, int cellY, Side side)
        {
            switch (side)
            {
                case Side.North: return new EdgeId(cellX, cellY, true);
                case Side.South: return new EdgeId(cellX, cellY + 1, true);
                case Side.West:  return new EdgeId(cellX, cellY, false);
                default:         return new EdgeId(cellX + 1, cellY, false);
            }
        }

        public bool Equals(EdgeId other) =>
            X == other.X && Y == other.Y && Horizontal == other.Horizontal;

        public override bool Equals(object obj) => obj is EdgeId e && Equals(e);

        public override int GetHashCode() =>
            (X * 397) ^ (Y * 31) ^ (Horizontal ? 1 : 0);
    }
}
