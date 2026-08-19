using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Bir polyomino'nun DIŞ ÇEVRE ÇİZGİSİNİ üretir: hücre maskesinden başlar,
    /// kenarları içeri/dışarı kaydırır ve köşeleri yuvarlatır.
    ///
    /// DERS (aynı renkten iki blok neden tek parça görünüyordu?): Tuğlalar
    /// hücre hücre kutu örülerek kuruluyordu; silüet KESKİN bir dikdörtgendi
    /// ve `brickInset` ayarı 0'a çekilmişti. Yan yana duran aynı renkten iki
    /// blok arasında ne boşluk ne de kırılan bir hat kalıyordu — göz onları
    /// tek bir kütle olarak okuyordu (4. tur H27, KRİTİK).
    ///
    /// Referansta (Levels 1-20 yürüyüşü, 00:12 karesi) her blok yuvarlak
    /// köşeli plastik bir parça: köşe yayları komşusuyla arasında dört küçük
    /// çentik açıyor, ince bir boşluk da o çentikleri birleştiriyor. Yani
    /// ayrımı yapan şey boşluğun BÜYÜKLÜĞÜ değil, silüetin BİÇİMİ.
    ///
    /// DERS (ölçekle büyütmek kontur üretmez): Tutma konturu bloğun silüetini
    /// `localScale = 1.04` ile büyüterek kuruluyordu. Ölçek MERKEZDEN çalışır:
    /// 2 hücrelik blokta 0,04 hücre taşarken 6 hücrelik blokta 0,12 hücre
    /// taşıyor ve komşu bloğun içine giriyordu (4. tur H29). Kontur bir
    /// KALINLIKTIR, bir orantı değil — burada dışa doğru SABİT bir kaydırma
    /// olarak üretiliyor.
    /// </summary>
    public static class BrickSilhouette
    {
        /// <summary>Bir köşe yayına kaç parça — 4 zaten pürüzsüz görünüyor.</summary>
        public const int CornerSegments = 4;

        /// <summary>
        /// Hücre maskesinden yuvarlak köşeli çevre çizgisi.
        /// </summary>
        /// <param name="cells">Bloğun dolu hücreleri (hücre uzayı, y AŞAĞI artar).</param>
        /// <param name="w">Bloğun sınırlayıcı kutusunun genişliği.</param>
        /// <param name="h">Yüksekliği.</param>
        /// <param name="inset">İçeri kaydırma; NEGATİF vermek dışa şişirir (kontur kabuğu).</param>
        /// <param name="radius">Köşe yarıçapı (hücre biriminde).</param>
        /// <returns>Yerel XZ düzleminde kapalı halka; yön garanti edilmez.</returns>
        public static List<Vector2> Build(
            IReadOnlyList<Vector2Int> cells, int w, int h, float inset, float radius)
        {
            var loop = TraceOuterLoop(cells, w, h);
            if (loop == null || loop.Count < 4) return null;

            RemoveCollinear(loop);
            if (Mathf.Abs(inset) > 1e-5f) Offset(loop, inset);
            return radius > 0.0005f ? Fillet(loop, radius) : loop;
        }

        // ------------------------------------------------------------------
        // 1. Rektilineer çevre çizgisini bul
        // ------------------------------------------------------------------

        /// <summary>
        /// Kenar takibi: yalnız KOMŞUSU OLMAYAN hücre kenarları sınırdır.
        ///
        /// DERS (iç kenarları hiç üretmemek): "Her hücreye kutu çiz, sonra
        /// birleştir" yaklaşımı iç yüzleri de kurar; hem israftır hem de
        /// üst üste binen yüzeylerde titreşen çizgi (z-fighting) doğurur.
        /// Sınırı doğrudan izlemek bu sorunu var olmadan bitiriyor.
        /// </summary>
        static List<Vector2> TraceOuterLoop(IReadOnlyList<Vector2Int> cells, int w, int h)
        {
            var filled = new HashSet<Vector2Int>(cells);
            if (filled.Count == 0) return null;

            // Kafes köşesi (cx, cy) -> yerel XZ. Hücre uzayında y aşağı artar,
            // dünyada Z yukarı: eşleme y'yi ters çeviriyor.
            Vector2 ToLocal(Vector2Int p) => new Vector2(p.x - w * 0.5f, h * 0.5f - p.y);

            // Yönlü kenarlar: iç bölge daima SOLDA kalacak biçimde.
            var next = new Dictionary<Vector2Int, List<Vector2Int>>();
            void Add(Vector2Int from, Vector2Int to)
            {
                if (!next.TryGetValue(from, out var list))
                    next[from] = list = new List<Vector2Int>(2);
                list.Add(to);
            }

            foreach (var cell in filled)
            {
                var a = new Vector2Int(cell.x, cell.y + 1);       // XZ'de alt-sol
                var b = new Vector2Int(cell.x + 1, cell.y + 1);   // alt-sag
                var c = new Vector2Int(cell.x + 1, cell.y);       // ust-sag
                var d = new Vector2Int(cell.x, cell.y);           // ust-sol

                if (!filled.Contains(new Vector2Int(cell.x, cell.y + 1))) Add(a, b);
                if (!filled.Contains(new Vector2Int(cell.x + 1, cell.y))) Add(b, c);
                if (!filled.Contains(new Vector2Int(cell.x, cell.y - 1))) Add(c, d);
                if (!filled.Contains(new Vector2Int(cell.x - 1, cell.y))) Add(d, a);
            }

            if (next.Count == 0) return null;

            // Baslangic: en kucuk (x, y) kafes noktasi daima DIS halkadadir.
            var start = default(Vector2Int);
            bool first = true;
            foreach (var key in next.Keys)
            {
                if (first || key.x < start.x || (key.x == start.x && key.y < start.y))
                {
                    start = key;
                    first = false;
                }
            }

            var loop = new List<Vector2>(next.Count + 4);
            var current = start;
            var incoming = Vector2Int.zero;

            // Guvenlik siniri: bozuk bir maske sonsuz donguye sokmasin.
            int guard = next.Count * 4 + 16;
            while (guard-- > 0)
            {
                loop.Add(ToLocal(current));

                if (!next.TryGetValue(current, out var options) || options.Count == 0)
                    return null;

                Vector2Int chosen = options[0];
                if (options.Count > 1)
                {
                    // KISTIRMA NOKTASI: iki hucre yalniz koseden degiyor ve bu
                    // kafes noktasindan iki kenar cikiyor. Dis halkada kalmanin
                    // kurali SAGA donmeyi tercih etmektir; sola donmek deligin
                    // icine sapar.
                    int best = int.MinValue;
                    foreach (var option in options)
                    {
                        int rank = TurnRank(incoming, option - current);
                        if (rank > best) { best = rank; chosen = option; }
                    }
                }

                options.Remove(chosen);
                if (options.Count == 0) next.Remove(current);

                incoming = chosen - current;
                current = chosen;
                if (current == start) break;
            }

            return loop.Count >= 4 ? loop : null;
        }

        /// <summary>Saga donus en yuksek, geri donus en dusuk puani alir.</summary>
        static int TurnRank(Vector2Int incoming, Vector2Int outgoing)
        {
            if (incoming == Vector2Int.zero) return 0;
            // Kafes uzayinda y ASAGI artiyor; ekrandaki "saga donus" bu yuzden
            // capraz carpimin pozitif isareti.
            int cross = incoming.x * outgoing.y - incoming.y * outgoing.x;
            if (cross > 0) return 3;                    // saga
            if (incoming == outgoing) return 2;         // duz
            if (cross < 0) return 1;                    // sola
            return 0;                                   // geri
        }

        static void RemoveCollinear(List<Vector2> loop)
        {
            for (int i = loop.Count - 1; i >= 0 && loop.Count > 4; i--)
            {
                var prev = loop[(i - 1 + loop.Count) % loop.Count];
                var cur = loop[i];
                var nxt = loop[(i + 1) % loop.Count];
                var a = cur - prev;
                var b = nxt - cur;
                if (Mathf.Abs(a.x * b.y - a.y * b.x) < 1e-5f) loop.RemoveAt(i);
            }
        }

        // ------------------------------------------------------------------
        // 2. Kenarlari kaydir
        // ------------------------------------------------------------------

        /// <summary>
        /// Her kenari ic normali yonunde <paramref name="amount"/> kadar tasir.
        ///
        /// Ardisik kenarlar dik oldugu icin yeni kose, iki kaydirmanin
        /// TOPLAMIDIR; ayrica kesisim hesabi gerekmiyor.
        /// </summary>
        static void Offset(List<Vector2> loop, float amount)
        {
            int count = loop.Count;
            // Yon garanti degil: halka saat yonundeyse ic normal ters doner ve
            // "iceri" dedigimiz sey disari kayar. Isareti alandan okuyoruz.
            float sign = SignedArea(loop) >= 0f ? 1f : -1f;

            var moved = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                var prev = loop[(i - 1 + count) % count];
                var cur = loop[i];
                var nxt = loop[(i + 1) % count];
                var n1 = LeftNormal(cur - prev) * sign;
                var n2 = LeftNormal(nxt - cur) * sign;
                moved[i] = cur + (n1 + n2) * amount;
            }
            for (int i = 0; i < count; i++) loop[i] = moved[i];
        }

        /// <summary>Saat yonunun tersine halkada ic bolge SOLDADIR.</summary>
        static Vector2 LeftNormal(Vector2 direction)
        {
            direction = direction.normalized;
            return new Vector2(-direction.y, direction.x);
        }

        // ------------------------------------------------------------------
        // 3. Koseleri yuvarla
        // ------------------------------------------------------------------

        /// <summary>
        /// Her 90 derecelik koseyi yay ile degistirir.
        ///
        /// DERS (tek formul iki kose tipini de cozer): Disbukey koseyi kesmek
        /// ile icbukey koseyi doldurmak ayri islermis gibi gorunur; oysa yay
        /// merkezi her ikisinde de V + (b - a) * r. Donusun YONU zaten yayin
        /// hangi tarafa sistigini belirliyor. Iki ayri dal yazmak, ayni
        /// geometrik gercegi iki kez (ve er gec tutarsiz bicimde) anlatmak olur.
        /// </summary>
        static List<Vector2> Fillet(List<Vector2> loop, float radius)
        {
            int count = loop.Count;
            var result = new List<Vector2>(count * (CornerSegments + 1));

            for (int i = 0; i < count; i++)
            {
                var prev = loop[(i - 1 + count) % count];
                var cur = loop[i];
                var nxt = loop[(i + 1) % count];

                var inDir = cur - prev;
                var outDir = nxt - cur;
                float inLen = inDir.magnitude;
                float outLen = outDir.magnitude;
                if (inLen < 1e-5f || outLen < 1e-5f) { result.Add(cur); continue; }

                var a = inDir / inLen;
                var b = outDir / outLen;

                // Yaricap komsu kenarlarin YARISINI asamaz; asarsa iki yay
                // birbirinin icine girer ve siluet dugumlenir.
                float r = Mathf.Min(radius, Mathf.Min(inLen, outLen) * 0.5f);
                if (r < 1e-4f) { result.Add(cur); continue; }

                var p1 = cur - a * r;
                var p2 = cur + b * r;
                var center = cur + (b - a) * r;

                float angle1 = Mathf.Atan2(p1.y - center.y, p1.x - center.x);
                float angle2 = Mathf.Atan2(p2.y - center.y, p2.x - center.x);
                float delta = Mathf.DeltaAngle(angle1 * Mathf.Rad2Deg, angle2 * Mathf.Rad2Deg)
                              * Mathf.Deg2Rad;

                for (int s = 0; s <= CornerSegments; s++)
                {
                    float t = angle1 + delta * (s / (float)CornerSegments);
                    result.Add(center + new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r);
                }
            }

            return result;
        }

        // ------------------------------------------------------------------
        // 4. Ust yuzu ucgenle
        // ------------------------------------------------------------------

        /// <summary>
        /// Kulak kirpma (ear clipping) ile basit cokgen ucgenlemesi.
        ///
        /// DERS (yelpaze ucgenleme icbukey sekilde yalan soyler): Merkezden
        /// yelpaze acmak yalniz DISBUKEY cokgenlerde dogrudur; L seklindeki
        /// bir blokta ucgenler seklin disina tasar ve blok "hayalet bir
        /// koseyle" gorunur. Kulak kirpma yavas ama dogru; sekil basina bir
        /// kez calisip onbellege girdigi icin maliyeti tek seferliktir.
        ///
        /// DERS (ters sarim sessizce kaybeder): Unity SOL ELLİ bir dunyada,
        /// ekranda SAAT YONUNDE sirali ucgeni on yuz sayar. XZ duzleminde saat
        /// yonunun TERSINE dizilmis bir cokgen, tepeden bakildiginda ekranda
        /// saat yonunun tersine gorunur; yani kulak kirpmanin dogal cikti sirasi
        /// ARKA yuzdur ve yuzey hicbir hata vermeden GORUNMEZ olur. Tahtanin
        /// zemini tam bu yuzden aylarca cizilmemisti (2026-08-17 bulgusu).
        /// <paramref name="faceUp"/> true iken sira ters cevriliyor.
        /// </summary>
        public static void Triangulate(List<Vector2> polygon, List<int> triangles, int baseIndex,
            bool faceUp = true)
        {
            int count = polygon.Count;
            if (count < 3) return;

            var indices = new List<int>(count);
            bool clockwise = SignedArea(polygon) < 0f;
            for (int i = 0; i < count; i++) indices.Add(clockwise ? count - 1 - i : i);

            int guard = count * count + 32;
            while (indices.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < indices.Count; i++)
                {
                    int i0 = indices[(i - 1 + indices.Count) % indices.Count];
                    int i1 = indices[i];
                    int i2 = indices[(i + 1) % indices.Count];

                    var a = polygon[i0];
                    var b = polygon[i1];
                    var c = polygon[i2];

                    // Kulak olabilmesi icin kose DISBUKEY olmali...
                    if (Cross(b - a, c - b) <= 0f) continue;

                    // ...ve ucgenin icinde baska kose kalmamali.
                    bool contains = false;
                    foreach (int other in indices)
                    {
                        if (other == i0 || other == i1 || other == i2) continue;
                        if (PointInTriangle(polygon[other], a, b, c)) { contains = true; break; }
                    }
                    if (contains) continue;

                    Emit(triangles, baseIndex, i0, i1, i2, faceUp);
                    indices.RemoveAt(i);
                    clipped = true;
                    break;
                }

                // Kirpilacak kulak kalmadiysa (sayisal olarak bozuk cokgen)
                // kalani yelpazeyle kapat: hic kapatmamaktan iyidir.
                if (!clipped) break;
            }

            for (int i = 1; i + 1 < indices.Count; i++)
                Emit(triangles, baseIndex, indices[0], indices[i], indices[i + 1], faceUp);
        }

        static void Emit(List<int> triangles, int baseIndex, int i0, int i1, int i2, bool faceUp)
        {
            triangles.Add(baseIndex + i0);
            triangles.Add(baseIndex + (faceUp ? i2 : i1));
            triangles.Add(baseIndex + (faceUp ? i1 : i2));
        }

        public static float SignedArea(List<Vector2> polygon)
        {
            float sum = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                sum += a.x * b.y - b.x * a.y;
            }
            return sum * 0.5f;
        }

        /// <summary>Halkayi saat yonunun TERSINE cevirir (ust yuz normali icin).</summary>
        public static void MakeCounterClockwise(List<Vector2> polygon)
        {
            if (SignedArea(polygon) < 0f) polygon.Reverse();
        }

        /// <summary>
        /// Noktanın halkanın İÇİNDE ne kadar derinde olduğu.
        ///
        /// Dışarıdaysa negatif döner. Kullanım yeri: bir saplamanın gövdeye
        /// sığıp sığmadığına karar vermek — gövde kenarına saplama
        /// yarıçapından yakın bir saplama havada asılı kalır.
        ///
        /// Halka birkaç düzine noktalı, mesh de önbelleklendiği için bu kaba
        /// (nokta-kenar) hesap fazlasıyla yeterli.
        /// </summary>
        public static float DistanceInside(List<Vector2> polygon, Vector2 point)
        {
            if (polygon == null || polygon.Count < 3) return 0f;

            float best = float.MaxValue;
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[j], b = polygon[i];

                // Kenara uzaklık
                Vector2 edge = b - a;
                float lengthSq = edge.sqrMagnitude;
                float t = lengthSq > 1e-8f
                    ? Mathf.Clamp01(Vector2.Dot(point - a, edge) / lengthSq) : 0f;
                best = Mathf.Min(best, Vector2.Distance(point, a + edge * t));

                // Işın testi: yatay ışın kenarı kesiyor mu?
                if (a.y > point.y != b.y > point.y &&
                    point.x < a.x + (point.y - a.y) / (b.y - a.y) * (b.x - a.x))
                    inside = !inside;
            }
            return inside ? best : -best;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(b - a, p - a);
            float d2 = Cross(c - b, p - b);
            float d3 = Cross(a - c, p - c);
            bool negative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool positive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(negative && positive);
        }
    }
}
