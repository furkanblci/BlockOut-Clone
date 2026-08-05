using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Core
{
    /// <summary>
    /// SERBEST SÜRÜKLEME algoritması — oyunun çekirdek hissi burada üretilir.
    ///
    /// DERS (neden fizik motoru değil?): Rigidbody tabanlı çözüm titremeye,
    /// tünellemeye ve kare hızına bağlı davranışa açıktır; ayrıca mobilde
    /// gereksiz maliyettir. Bunun yerine deterministik, saf matematik:
    ///
    /// 1) ALT ADIM: Parmağın kare başına deltası küçük parçalara bölünür
    ///    (config.dragSubstep ~0.15 hücre). Büyük tek adım, dar koridor
    ///    köşesini "atlar"; küçük adımlar köşeyi karış karış döner.
    /// 2) EKSEN AYRIMI: Her alt adımda önce X sonra Y ekseninde ayrı süpürme
    ///    yapılır. Çapraz itilen blok duvara çarptığında serbest eksende
    ///    ilerlemeye devam eder = "duvar boyunca kayma" hissi bedavaya çıkar.
    /// 3) TAM KENETLENME: Süpürme, engelin yüzeyine olan mesafeyi TAM olarak
    ///    hesaplar (binary search yok) — blok duvara mikron boşluksuz dayanır.
    ///
    /// DERS (polyomino): Blok artık tek dikdörtgen değil, HÜCRE LİSTESİ.
    /// Süpürme her hücre için ayrı yapılır ve en kısıtlayıcı sonuç alınır —
    /// L şeklinin çıkıntısı bir yere takılırsa tüm blok orada durur. Mantık
    /// değişmedi, yalnızca "tek kutu" yerine "kutular" üzerinde dönüyor.
    /// </summary>
    public static class DragSolver
    {
        /// <summary>
        /// Bloğu mevcut konumundan hedefe doğru, engellere çarpa çarpa taşır.
        /// <paramref name="cells"/> bloğun yerel hücre konumlarıdır.
        /// </summary>
        public static Vector2 Solve(
            Vector2 position, Vector2 target, IReadOnlyList<Vector2Int> cells,
            IReadOnlyList<Aabb> obstacles, float substep, float eps)
        {
            Vector2 delta = target - position;
            float maxAxis = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
            if (maxAxis < 1e-6f) return position;

            int steps = Mathf.CeilToInt(maxAxis / substep);
            Vector2 stepDelta = delta / steps;

            for (int i = 0; i < steps; i++)
            {
                position.x += SweepX(position, cells, stepDelta.x, obstacles, eps);
                position.y += SweepY(position, cells, stepDelta.y, obstacles, eps);
            }
            return position;
        }

        /// <summary>X ekseninde izin verilen gerçek yer değiştirmeyi döndürür.</summary>
        static float SweepX(Vector2 pos, IReadOnlyList<Vector2Int> cells, float dx,
            IReadOnlyList<Aabb> obstacles, float eps)
        {
            if (dx == 0f) return 0f;
            float allowed = dx;

            for (int c = 0; c < cells.Count; c++)
            {
                float cellX = pos.x + cells[c].x;
                float cellY = pos.y + cells[c].y;

                // Dikey örtüşme testi eps kadar küçültülmüş hücreyle yapılır:
                // duvara TAM bitişik blok, kenar boyunca takılmadan kayar.
                float minY = cellY + eps, maxY = cellY + 1f - eps;

                if (dx > 0f)
                {
                    float lead = cellX + 1f;
                    for (int i = 0; i < obstacles.Count; i++)
                    {
                        var o = obstacles[i];
                        if (o.MinY >= maxY || o.MaxY <= minY) continue;
                        if (o.MinX >= lead - eps)
                            allowed = Mathf.Min(allowed, o.MinX - lead);
                    }
                }
                else
                {
                    float lead = cellX;
                    for (int i = 0; i < obstacles.Count; i++)
                    {
                        var o = obstacles[i];
                        if (o.MinY >= maxY || o.MaxY <= minY) continue;
                        if (o.MaxX <= lead + eps)
                            allowed = Mathf.Max(allowed, o.MaxX - lead);
                    }
                }
            }

            return dx > 0f ? Mathf.Max(0f, allowed) : Mathf.Min(0f, allowed);
        }

        /// <summary>Y ekseninde izin verilen gerçek yer değiştirmeyi döndürür (SweepX'in simetriği).</summary>
        static float SweepY(Vector2 pos, IReadOnlyList<Vector2Int> cells, float dy,
            IReadOnlyList<Aabb> obstacles, float eps)
        {
            if (dy == 0f) return 0f;
            float allowed = dy;

            for (int c = 0; c < cells.Count; c++)
            {
                float cellX = pos.x + cells[c].x;
                float cellY = pos.y + cells[c].y;
                float minX = cellX + eps, maxX = cellX + 1f - eps;

                if (dy > 0f)
                {
                    float lead = cellY + 1f;
                    for (int i = 0; i < obstacles.Count; i++)
                    {
                        var o = obstacles[i];
                        if (o.MinX >= maxX || o.MaxX <= minX) continue;
                        if (o.MinY >= lead - eps)
                            allowed = Mathf.Min(allowed, o.MinY - lead);
                    }
                }
                else
                {
                    float lead = cellY;
                    for (int i = 0; i < obstacles.Count; i++)
                    {
                        var o = obstacles[i];
                        if (o.MinX >= maxX || o.MaxX <= minX) continue;
                        if (o.MaxY <= lead + eps)
                            allowed = Mathf.Max(allowed, o.MaxY - lead);
                    }
                }
            }

            return dy > 0f ? Mathf.Max(0f, allowed) : Mathf.Min(0f, allowed);
        }

        /// <summary>
        /// Bırakılan bloğu en yakın GEÇERLİ tam sayı hücreye oturtur.
        /// Adaylar: konumun floor/ceil kombinasyonları (en fazla 4), yakından
        /// uzağa denenir; çarpışanlar elenir.
        /// </summary>
        public static Vector2 SnapToGrid(
            Vector2 pos, IReadOnlyList<Vector2Int> cells,
            IReadOnlyList<Aabb> obstacles, float eps)
        {
            int fx = Mathf.FloorToInt(pos.x);
            int fy = Mathf.FloorToInt(pos.y);

            Vector2 best = pos;
            float bestDist = float.MaxValue;

            for (int dx = 0; dx <= 1; dx++)
            {
                for (int dy = 0; dy <= 1; dy++)
                {
                    var candidate = new Vector2(fx + dx, fy + dy);
                    float dist = (candidate - pos).sqrMagnitude;
                    if (dist >= bestDist) continue;
                    if (Collides(candidate, cells, obstacles, eps)) continue;
                    best = candidate;
                    bestDist = dist;
                }
            }
            return best;
        }

        static bool Collides(Vector2 p, IReadOnlyList<Vector2Int> cells,
            IReadOnlyList<Aabb> obstacles, float eps)
        {
            for (int c = 0; c < cells.Count; c++)
            {
                var rect = Aabb.FromRect(p.x + cells[c].x, p.y + cells[c].y, 1, 1);
                for (int i = 0; i < obstacles.Count; i++)
                    if (rect.Overlaps(obstacles[i], eps))
                        return true;
            }
            return false;
        }
    }
}
