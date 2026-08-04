using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// LEGO görünümlü tuğla mesh'lerini PROSEDÜREL üretir ve önbelleğe alır.
    ///
    /// DERS (neden prosedürel?): 1×1'den 5×5'e kadar her boyut için ayrı model
    /// çizmek onlarca asset demek. Tuğla düzenli bir geometri olduğu için kodla
    /// üretmek hem dosya sayısını sıfırlar hem de "saplama yarıçapını 0.02
    /// büyüt" gibi ayarları tek satırda tüm oyuna uygular.
    ///
    /// DERS (normalleri ELLE yazmak): RecalculateNormals komşu üçgenlerin
    /// normallerini ortalar; kutu köşeleri yuvarlanmış, saplamalar ise fazla
    /// köşeli görünür. Doğru sonuç için normaller geometriyi bilerek yazılır:
    /// kutu yüzleri DÜZ, saplama yanları MERKEZDEN DIŞA (radyal). Radyal normal
    /// silindiri pürüzsüz gösterir ve parlaklık saplamanın üstünde kayar —
    /// referans oyundaki cilalı plastik hissinin kaynağı budur.
    ///
    /// DERS (vertex AO): Gölge hesaplamak mobilde pahalıdır. Köşe renklerine
    /// gömülü koyuluk bedava bir yaklaşımdır; shader onu çarpan olarak kullanır.
    /// </summary>
    public static class BrickMeshBuilder
    {
        public const float Height = 0.40f;

        const float Inset = 0.055f;      // komşu bloklarla görsel ayrım
        const float Chamfer = 0.06f;     // üst kenar pahı

        // Referans oyunda her HÜCREDE 2×2 saplama var (tek kocaman değil).
        const int StudsPerCell = 2;
        const float StudRadius = 0.165f;
        const float StudHeight = 0.085f;
        const int StudSegments = 12;

        static readonly Dictionary<int, Mesh> Cache = new Dictionary<int, Mesh>();

        public static Mesh Get(int w, int h)
        {
            int key = w * 100 + h;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var mesh = Build(w, h);
            Cache[key] = mesh;
            return mesh;
        }

        static Mesh Build(int w, int h)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            var colors = new List<Color>();

            float hx = w * 0.5f - Inset;
            float hz = h * 0.5f - Inset;
            float shoulder = Height - Chamfer;
            float ix = hx - Chamfer, iz = hz - Chamfer;

            // Sahte AO: alt koyu, üst aydınlık.
            Color bottom = new Color(0.55f, 0.55f, 0.55f);
            Color mid = new Color(0.88f, 0.88f, 0.88f);
            Color top = Color.white;

            // --- yan duvarlar ---
            Quad(verts, normals, tris, colors, Vector3.back,
                new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz),
                new Vector3(hx, shoulder, -hz), new Vector3(-hx, shoulder, -hz), bottom, mid);
            Quad(verts, normals, tris, colors, Vector3.forward,
                new Vector3(hx, 0, hz), new Vector3(-hx, 0, hz),
                new Vector3(-hx, shoulder, hz), new Vector3(hx, shoulder, hz), bottom, mid);
            Quad(verts, normals, tris, colors, Vector3.right,
                new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz),
                new Vector3(hx, shoulder, hz), new Vector3(hx, shoulder, -hz), bottom, mid);
            Quad(verts, normals, tris, colors, Vector3.left,
                new Vector3(-hx, 0, hz), new Vector3(-hx, 0, -hz),
                new Vector3(-hx, shoulder, -hz), new Vector3(-hx, shoulder, hz), bottom, mid);

            // --- pah bandı (yumuşak üst kenar) ---
            var bevelN = new Vector3(0, 0.7f, -0.7f);
            Quad(verts, normals, tris, colors, bevelN,
                new Vector3(-hx, shoulder, -hz), new Vector3(hx, shoulder, -hz),
                new Vector3(ix, Height, -iz), new Vector3(-ix, Height, -iz), mid, top);
            Quad(verts, normals, tris, colors, new Vector3(0, 0.7f, 0.7f),
                new Vector3(hx, shoulder, hz), new Vector3(-hx, shoulder, hz),
                new Vector3(-ix, Height, iz), new Vector3(ix, Height, iz), mid, top);
            Quad(verts, normals, tris, colors, new Vector3(0.7f, 0.7f, 0),
                new Vector3(hx, shoulder, -hz), new Vector3(hx, shoulder, hz),
                new Vector3(ix, Height, iz), new Vector3(ix, Height, -iz), mid, top);
            Quad(verts, normals, tris, colors, new Vector3(-0.7f, 0.7f, 0),
                new Vector3(-hx, shoulder, hz), new Vector3(-hx, shoulder, -hz),
                new Vector3(-ix, Height, -iz), new Vector3(-ix, Height, iz), mid, top);

            // --- üst yüz ---
            Quad(verts, normals, tris, colors, Vector3.up,
                new Vector3(-ix, Height, -iz), new Vector3(ix, Height, -iz),
                new Vector3(ix, Height, iz), new Vector3(-ix, Height, iz), top, top);

            // --- alt yüz ---
            Quad(verts, normals, tris, colors, Vector3.down,
                new Vector3(-hx, 0, iz), new Vector3(hx, 0, iz),
                new Vector3(hx, 0, -iz), new Vector3(-hx, 0, -iz), bottom, bottom);

            // --- saplamalar: her hücreye StudsPerCell² adet ---
            float step = 1f / StudsPerCell;
            float first = step * 0.5f;
            for (int cx = 0; cx < w; cx++)
                for (int cz = 0; cz < h; cz++)
                    for (int sx = 0; sx < StudsPerCell; sx++)
                        for (int sz = 0; sz < StudsPerCell; sz++)
                            AddStud(verts, normals, tris, colors, new Vector3(
                                -w * 0.5f + cx + first + sx * step,
                                Height,
                                -h * 0.5f + cz + first + sz * step), mid, top);

            var mesh = new Mesh { name = $"Brick_{w}x{h}" };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true); // GPU'ya gönder, CPU kopyasını bırak
            return mesh;
        }

        /// <summary>Dört köşeli yüzey; köşeler CCW sırada verilir, normal elle belirtilir.</summary>
        static void Quad(List<Vector3> verts, List<Vector3> normals, List<int> tris,
            List<Color> colors, Vector3 normal,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color c0, Color c1)
        {
            int start = verts.Count;
            normal = normal.normalized;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            normals.Add(normal); normals.Add(normal); normals.Add(normal); normals.Add(normal);
            colors.Add(c0); colors.Add(c0); colors.Add(c1); colors.Add(c1);
            tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
            tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
        }

        static void AddStud(List<Vector3> verts, List<Vector3> normals, List<int> tris,
            List<Color> colors, Vector3 center, Color side, Color cap)
        {
            int ringStart = verts.Count;
            float topY = center.y + StudHeight;

            for (int i = 0; i < StudSegments; i++)
            {
                float angle = i / (float)StudSegments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                var radial = new Vector3(cos, 0f, sin);

                verts.Add(center + radial * StudRadius);
                verts.Add(new Vector3(center.x + cos * StudRadius, topY, center.z + sin * StudRadius));
                // Radyal normal: silindir pürüzsüz görünür, parlaklık üzerinde kayar.
                normals.Add(radial); normals.Add(radial);
                colors.Add(side); colors.Add(cap);
            }

            for (int i = 0; i < StudSegments; i++)
            {
                int a = ringStart + i * 2;
                int b = ringStart + ((i + 1) % StudSegments) * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(b);
                tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
            }

            // Üst kapak: ayrı vertex halkası (normali YUKARI olmalı, radyal değil).
            int capStart = verts.Count;
            for (int i = 0; i < StudSegments; i++)
            {
                float angle = i / (float)StudSegments * Mathf.PI * 2f;
                verts.Add(new Vector3(
                    center.x + Mathf.Cos(angle) * StudRadius, topY,
                    center.z + Mathf.Sin(angle) * StudRadius));
                normals.Add(Vector3.up);
                colors.Add(cap);
            }

            int capCenter = verts.Count;
            verts.Add(new Vector3(center.x, topY, center.z));
            normals.Add(Vector3.up);
            colors.Add(cap);

            for (int i = 0; i < StudSegments; i++)
            {
                int a = capStart + i;
                int b = capStart + (i + 1) % StudSegments;
                tris.Add(capCenter); tris.Add(b); tris.Add(a);
            }
        }
    }
}
