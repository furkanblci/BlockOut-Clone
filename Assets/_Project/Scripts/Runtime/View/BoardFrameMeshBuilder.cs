using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Tahtayı çevreleyen YUVARLAK KÖŞELİ, pahlı çerçeve mesh'i.
    ///
    /// DERS (neden dört kutu yetmiyor?): Dört ayrı kutu köşelerde keskin bir
    /// birleşim ve birbirine giren yüzeyler bırakır — hem referanstaki yumuşak
    /// görünümü vermez hem de çakışan yüzeylerde z-fighting (titreşen çizgi)
    /// üretir. Tek parça, kapalı bir şerit mesh'i ikisini de çözer.
    ///
    /// Şerit iki yuvarlatılmış dikdörtgen çevre çizgisinden (dış ve iç) oluşur;
    /// aralarına üst yüz, pah bandı ve yan duvarlar örülür. Köşe renkleri sahte
    /// AO taşır: üst aydınlık, yan yüzler koyu — derinlik hissi buradan gelir.
    /// </summary>
    public static class BoardFrameMeshBuilder
    {
        /// <summary>
        /// DİKDÖRTGEN OLMAYAN tahtalar için çerçeve.
        ///
        /// Kullanıcı (5. tur): "bazı levellerde boşluk olmalı, kullanılmayan
        /// kısımlar kesilmeli... örneğin level 8'de sol alt kısmın olmaması
        /// gerekiyor."
        ///
        /// Referansta (Levels 1-20 yürüyüşü, 8., 9., 11., 12., 13. bölümler)
        /// tahta çoğu zaman dikdörtgen DEĞİL: kullanılmayan bölge tahtadan
        /// tamamen kesilmiş ve duvar o çıkıntının çevresini dolanıyor. Bizde
        /// zemin maskeyi zaten destekliyordu (`BoardModel.IsPlayable`), ama
        /// çerçeve `RoundedRect` ile ÜRETİLİYORDU — yani her tahta dikdörtgen
        /// bir duvarla çevriliyordu ve kesik bölge "duvarın içinde boşluk"
        /// gibi görünüyordu.
        ///
        /// DERS (bir kısıtın nerede saklandığını bilmek): "Tahtalar
        /// dikdörtgen" kuralı hiçbir yerde yazmıyordu; tek bir yardımcı
        /// fonksiyonun (`RoundedRect`) içinde saklıydı. Veri modeli çoktan
        /// serbestti. Bir özelliği eklemek çoğu zaman yeni kod yazmak değil,
        /// eski bir varsayımın nerede kaldığını bulmak.
        ///
        /// Halkalar <see cref="BrickSilhouette"/> ile üretiliyor — blok
        /// silüetini üreten aynı kod. Dört halka da aynı çağrının farklı
        /// `inset`/`radius` değerleriyle üretildiği için nokta sayıları ve
        /// sıraları birebir eşleşiyor; şerit örücü değişmeden çalışıyor.
        /// </summary>
        public static Mesh Build(
            System.Collections.Generic.IReadOnlyList<Vector2Int> playableCells,
            int boardW, int boardH,
            float thickness, float height, float cornerRadius, float bevel)
        {
            cornerRadius = Mathf.Max(0.001f, cornerRadius);
            bevel = Mathf.Clamp(bevel, 0f, Mathf.Min(thickness * 0.45f, height * 0.5f));

            var outer = BrickSilhouette.Build(
                playableCells, boardW, boardH, -thickness, cornerRadius + thickness);
            var outerTop = BrickSilhouette.Build(
                playableCells, boardW, boardH, -thickness + bevel,
                Mathf.Max(0.01f, cornerRadius + thickness - bevel));
            var innerTop = BrickSilhouette.Build(
                playableCells, boardW, boardH, -bevel, cornerRadius + bevel);
            var inner = BrickSilhouette.Build(
                playableCells, boardW, boardH, 0f, cornerRadius);

            if (outer == null || outerTop == null || innerTop == null || inner == null ||
                outer.Count != inner.Count || outer.Count != outerTop.Count ||
                outer.Count != innerTop.Count)
            {
                // Maske çözülemezse eski dikdörtgen yola dön: tahta duvarsız
                // kalmasın.
                return Build(boardW, boardH, thickness, height, cornerRadius, bevel);
            }

            BrickSilhouette.MakeCounterClockwise(outer);
            BrickSilhouette.MakeCounterClockwise(outerTop);
            BrickSilhouette.MakeCounterClockwise(innerTop);
            BrickSilhouette.MakeCounterClockwise(inner);

            return Weave(outer, outerTop, innerTop, inner, height, bevel);
        }

        /// <param name="innerW">Tahtanın genişliği (hücre).</param>
        /// <param name="innerH">Tahtanın yüksekliği (hücre).</param>
        public static Mesh Build(float innerW, float innerH,
            float thickness, float height, float cornerRadius, float bevel)
        {
            cornerRadius = Mathf.Max(0.001f, cornerRadius);
            bevel = Mathf.Clamp(bevel, 0f, Mathf.Min(thickness * 0.45f, height * 0.5f));

            const int cornerSegments = 6;
            float outerHalfW = innerW * 0.5f + thickness;
            float outerHalfH = innerH * 0.5f + thickness;
            float innerHalfW = innerW * 0.5f;
            float innerHalfH = innerH * 0.5f;

            // Dış yarıçap ayarın kendisidir; içeriye doğru kalınlık kadar
            // küçülür. Önceden dışa +kalınlık/2 ekleniyordu ve köşeler
            // referanstan çok daha yuvarlak çıkıyordu.
            float outerRadius = Mathf.Min(cornerRadius, Mathf.Min(outerHalfW, outerHalfH) * 0.6f);
            float innerRadius = Mathf.Max(0.02f, outerRadius - thickness * 0.7f);

            var outer = RoundedRect(outerHalfW, outerHalfH, outerRadius, cornerSegments);
            var inner = RoundedRect(innerHalfW, innerHalfH,
                Mathf.Min(innerRadius, Mathf.Min(innerHalfW, innerHalfH) * 0.6f), cornerSegments);

            // Dikdörtgen yolda pah halkaları merkeze göre ölçekle üretiliyor;
            // dört halkalı ortak örücü ikisini de aynı koda bağlıyor.
            int rectCount = outer.Count;
            var outerTopRect = new List<Vector2>(rectCount);
            var innerTopRect = new List<Vector2>(rectCount);
            for (int i = 0; i < rectCount; i++)
            {
                outerTopRect.Add(Shrink(outer[i], bevel));
                innerTopRect.Add(Grow(inner[i], bevel));
            }

            return Weave(outer, outerTopRect, innerTopRect, inner, height, bevel);
        }

        /// <summary>
        /// Dört halkayı (dış → dış pah → iç pah → iç) beş şerit hâlinde örer.
        /// Renkler sahte AO taşır: üst aydınlık, yan yüzler koyu.
        /// </summary>
        static Mesh Weave(List<Vector2> outer, List<Vector2> outerTop,
            List<Vector2> innerTop, List<Vector2> inner, float height, float bevel)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            Color topTone = Color.white;
            Color bevelTone = new Color(0.82f, 0.82f, 0.82f);
            Color sideTone = new Color(0.52f, 0.52f, 0.52f);
            Color innerTone = new Color(0.38f, 0.38f, 0.38f); // iç duvar en koyu

            int count = outer.Count;
            float shoulder = height - bevel;

            // Dış yan duvar (aşağıdan omuza)
            Strip(verts, normals, colors, tris, count,
                i => Lift(outer[i], 0f), i => Lift(outer[i], shoulder),
                i => OutNormal(outer, i), sideTone, bevelTone);

            // Dış pah (omuzdan üste, içeri kaçarak)
            Strip(verts, normals, colors, tris, count,
                i => Lift(outer[i], shoulder),
                i => Lift(outerTop[i], height),
                i => (OutNormal(outer, i) + Vector3.up).normalized, bevelTone, topTone);

            // Üst yüz (dış pah kenarından iç pah kenarına)
            Strip(verts, normals, colors, tris, count,
                i => Lift(outerTop[i], height),
                i => Lift(innerTop[i], height),
                i => Vector3.up, topTone, topTone);

            // İç pah (üstten iç omuza)
            Strip(verts, normals, colors, tris, count,
                i => Lift(innerTop[i], height),
                i => Lift(inner[i], shoulder),
                i => (-OutNormal(inner, i) + Vector3.up).normalized, topTone, bevelTone);

            // İç duvar (omuzdan zemine)
            Strip(verts, normals, colors, tris, count,
                i => Lift(inner[i], shoulder), i => Lift(inner[i], 0f),
                i => -OutNormal(inner, i), bevelTone, innerTone);

            var mesh = new Mesh { name = "BoardFrame" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        static Vector3 Lift(Vector2 point, float y) => new Vector3(point.x, y, point.y);
        static Vector2 Shrink(Vector2 point, float amount) => point - point.normalized * amount;
        static Vector2 Grow(Vector2 point, float amount) => point + point.normalized * amount;

        static Vector3 OutNormal(List<Vector2> ring, int i)
        {
            Vector2 prev = ring[(i - 1 + ring.Count) % ring.Count];
            Vector2 next = ring[(i + 1) % ring.Count];
            Vector2 tangent = (next - prev).normalized;
            return new Vector3(tangent.y, 0f, -tangent.x);
        }

        /// <summary>İki çevre çizgisi arasına kapalı bir üçgen şeridi örer.</summary>
        static void Strip(List<Vector3> verts, List<Vector3> normals, List<Color> colors,
            List<int> tris, int count,
            System.Func<int, Vector3> lower, System.Func<int, Vector3> upper,
            System.Func<int, Vector3> normal, Color lowTone, Color highTone)
        {
            int start = verts.Count;
            for (int i = 0; i < count; i++)
            {
                var n = normal(i);
                verts.Add(lower(i)); verts.Add(upper(i));
                normals.Add(n); normals.Add(n);
                colors.Add(lowTone); colors.Add(highTone);
            }

            for (int i = 0; i < count; i++)
            {
                int a = start + i * 2;
                int b = start + ((i + 1) % count) * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(b);
                tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
            }
        }

        /// <summary>Yuvarlatılmış dikdörtgenin çevre noktaları (saat yönünde).</summary>
        static List<Vector2> RoundedRect(float halfW, float halfH, float radius, int cornerSegments)
        {
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(halfW, halfH));
            var points = new List<Vector2>();

            // Dört köşe merkezi: sağ-üst, sol-üst, sol-alt, sağ-alt.
            var centers = new[]
            {
                new Vector2(halfW - radius, halfH - radius),
                new Vector2(-halfW + radius, halfH - radius),
                new Vector2(-halfW + radius, -halfH + radius),
                new Vector2(halfW - radius, -halfH + radius)
            };
            float[] startAngles = { 0f, 90f, 180f, 270f };

            for (int c = 0; c < 4; c++)
            {
                for (int s = 0; s <= cornerSegments; s++)
                {
                    float angle = (startAngles[c] + s / (float)cornerSegments * 90f) * Mathf.Deg2Rad;
                    points.Add(centers[c] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            }
            return points;
        }
    }
}
