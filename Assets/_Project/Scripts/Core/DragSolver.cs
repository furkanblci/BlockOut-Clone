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
    ///    hesaplar (binary search yok) — blok duvara mikron boşluksuz dayanır,
    ///    kapı temas testi de bu sayede güvenilirdir.
    /// </summary>
    public static class DragSolver
    {
        /// <summary>
        /// Bloğu mevcut konumundan hedefe doğru, engellere çarpa çarpa taşır.
        /// Dönen değer ulaşılabilen en ileri konumdur.
        /// </summary>
        public static Vector2 Solve(
            Vector2 position, Vector2 target, int w, int h,
            IReadOnlyList<Aabb> obstacles, float substep, float eps)
        {
            Vector2 delta = target - position;
            float maxAxis = Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
            if (maxAxis < 1e-6f) return position;

            int steps = Mathf.CeilToInt(maxAxis / substep);
            Vector2 stepDelta = delta / steps;

            for (int i = 0; i < steps; i++)
            {
                position.x += SweepX(position, w, h, stepDelta.x, obstacles, eps);
                position.y += SweepY(position, w, h, stepDelta.y, obstacles, eps);
            }
            return position;
        }

        /// <summary>X ekseninde izin verilen gerçek yer değiştirmeyi döndürür.</summary>
        static float SweepX(Vector2 pos, int w, int h, float dx,
            IReadOnlyList<Aabb> obstacles, float eps)
        {
            if (dx == 0f) return 0f;

            // Dikey örtüşme testi eps kadar küçültülmüş blokla yapılır:
            // duvara/komşuya TAM bitişik blok, kenar boyunca takılmadan kayar.
            float minY = pos.y + eps, maxY = pos.y + h - eps;

            if (dx > 0f)
            {
                float lead = pos.x + w; // öndeki (sağ) kenar
                float allowed = dx;
                for (int i = 0; i < obstacles.Count; i++)
                {
                    var o = obstacles[i];
                    if (o.MinY >= maxY || o.MaxY <= minY) continue; // dikeyde kesişmiyor
                    if (o.MinX >= lead - eps)                       // önümüzde
                        allowed = Mathf.Min(allowed, o.MinX - lead);
                }
                return Mathf.Max(0f, allowed);
            }
            else
            {
                float lead = pos.x; // öndeki (sol) kenar
                float allowed = dx;
                for (int i = 0; i < obstacles.Count; i++)
                {
                    var o = obstacles[i];
                    if (o.MinY >= maxY || o.MaxY <= minY) continue;
                    if (o.MaxX <= lead + eps)
                        allowed = Mathf.Max(allowed, o.MaxX - lead);
                }
                return Mathf.Min(0f, allowed);
            }
        }

        /// <summary>Y ekseninde izin verilen gerçek yer değiştirmeyi döndürür (SweepX'in simetriği).</summary>
        static float SweepY(Vector2 pos, int w, int h, float dy,
            IReadOnlyList<Aabb> obstacles, float eps)
        {
            if (dy == 0f) return 0f;

            float minX = pos.x + eps, maxX = pos.x + w - eps;

            if (dy > 0f)
            {
                float lead = pos.y + h;
                float allowed = dy;
                for (int i = 0; i < obstacles.Count; i++)
                {
                    var o = obstacles[i];
                    if (o.MinX >= maxX || o.MaxX <= minX) continue;
                    if (o.MinY >= lead - eps)
                        allowed = Mathf.Min(allowed, o.MinY - lead);
                }
                return Mathf.Max(0f, allowed);
            }
            else
            {
                float lead = pos.y;
                float allowed = dy;
                for (int i = 0; i < obstacles.Count; i++)
                {
                    var o = obstacles[i];
                    if (o.MinX >= maxX || o.MaxX <= minX) continue;
                    if (o.MaxY <= lead + eps)
                        allowed = Mathf.Max(allowed, o.MaxY - lead);
                }
                return Mathf.Min(0f, allowed);
            }
        }

        /// <summary>
        /// Bırakılan bloğu en yakın GEÇERLİ tam sayı hücreye oturtur.
        /// Adaylar: konumun floor/ceil kombinasyonları (en fazla 4), yakından
        /// uzağa denenir; çarpışanlar elenir. Sürükleme çarpışmasız ilerlediği
        /// için pratikte her zaman geçerli bir aday bulunur.
        /// </summary>
        public static Vector2 SnapToGrid(
            Vector2 pos, int w, int h, IReadOnlyList<Aabb> obstacles, float eps)
        {
            int fx = Mathf.FloorToInt(pos.x);
            int fy = Mathf.FloorToInt(pos.y);

            Vector2 best = pos;
            float bestDist = float.MaxValue;

            for (int dx = 0; dx <= 1; dx++)
            {
                for (int dy = 0; dy <= 1; dy++)
                {
                    var cand = new Vector2(fx + dx, fy + dy);
                    float dist = (cand - pos).sqrMagnitude;
                    if (dist >= bestDist) continue;
                    if (Collides(cand, w, h, obstacles, eps)) continue;
                    best = cand;
                    bestDist = dist;
                }
            }
            return best;
        }

        static bool Collides(Vector2 p, int w, int h, IReadOnlyList<Aabb> obstacles, float eps)
        {
            var rect = Aabb.FromRect(p.x, p.y, w, h);
            for (int i = 0; i < obstacles.Count; i++)
                if (rect.Overlaps(obstacles[i], eps))
                    return true;
            return false;
        }
    }
}
