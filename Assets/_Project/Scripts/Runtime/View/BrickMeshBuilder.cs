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
    /// DERS (vertex AO): Gölge hesaplamak mobilde pahalıdır. Bunun yerine
    /// köşe renklerine gömülü koyuluk (ambient occlusion) kullanıyoruz —
    /// bedava, çünkü zaten gönderilen vertex verisinin bir parçası. Shader
    /// vertex rengini çarpan olarak kullanır.
    ///
    /// Mesh'ler (w,h) başına BİR kez üretilir; aynı boyuttaki tüm bloklar aynı
    /// mesh'i paylaşır (paylaşımlı materyalle birlikte SRP Batcher dostu).
    /// </summary>
    public static class BrickMeshBuilder
    {
        public const float Height = 0.42f;

        const float Inset = 0.04f;       // komşu bloklarla görsel ayrım
        const float Chamfer = 0.07f;     // üst kenar pahı
        const float StudRadius = 0.27f;
        const float StudHeight = 0.14f;
        const int StudSegments = 12;

        static readonly Dictionary<int, Mesh> Cache = new Dictionary<int, Mesh>();

        /// <summary>Verilen hücre boyutu için tuğla mesh'i (önbellekten).</summary>
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
            var tris = new List<int>();
            var colors = new List<Color>();

            float hx = w * 0.5f - Inset;
            float hz = h * 0.5f - Inset;
            float shoulder = Height - Chamfer;

            // Renk çarpanları: alt karanlık, üst aydınlık (sahte AO).
            Color bottom = new Color(0.62f, 0.62f, 0.62f);
            Color mid = new Color(0.86f, 0.86f, 0.86f);
            Color top = Color.white;

            // --- yan duvarlar (0 → shoulder) ---
            AddQuad(verts, tris, colors,
                new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz),
                new Vector3(hx, shoulder, -hz), new Vector3(-hx, shoulder, -hz), bottom, mid);
            AddQuad(verts, tris, colors,
                new Vector3(hx, 0, hz), new Vector3(-hx, 0, hz),
                new Vector3(-hx, shoulder, hz), new Vector3(hx, shoulder, hz), bottom, mid);
            AddQuad(verts, tris, colors,
                new Vector3(hx, 0, -hz), new Vector3(hx, 0, hz),
                new Vector3(hx, shoulder, hz), new Vector3(hx, shoulder, -hz), bottom, mid);
            AddQuad(verts, tris, colors,
                new Vector3(-hx, 0, hz), new Vector3(-hx, 0, -hz),
                new Vector3(-hx, shoulder, -hz), new Vector3(-hx, shoulder, hz), bottom, mid);

            // --- pah bandı (shoulder → Height, içe doğru daralır) ---
            float ix = hx - Chamfer, iz = hz - Chamfer;
            AddQuad(verts, tris, colors,
                new Vector3(-hx, shoulder, -hz), new Vector3(hx, shoulder, -hz),
                new Vector3(ix, Height, -iz), new Vector3(-ix, Height, -iz), mid, top);
            AddQuad(verts, tris, colors,
                new Vector3(hx, shoulder, hz), new Vector3(-hx, shoulder, hz),
                new Vector3(-ix, Height, iz), new Vector3(ix, Height, iz), mid, top);
            AddQuad(verts, tris, colors,
                new Vector3(hx, shoulder, -hz), new Vector3(hx, shoulder, hz),
                new Vector3(ix, Height, iz), new Vector3(ix, Height, -iz), mid, top);
            AddQuad(verts, tris, colors,
                new Vector3(-hx, shoulder, hz), new Vector3(-hx, shoulder, -hz),
                new Vector3(-ix, Height, -iz), new Vector3(-ix, Height, iz), mid, top);

            // --- üst yüz ---
            AddQuad(verts, tris, colors,
                new Vector3(-ix, Height, -iz), new Vector3(ix, Height, -iz),
                new Vector3(ix, Height, iz), new Vector3(-ix, Height, iz), top, top);

            // --- alt yüz (kamera görmez ama emilme dönerken boşluk kalmasın) ---
            AddQuad(verts, tris, colors,
                new Vector3(-hx, 0, iz), new Vector3(hx, 0, iz),
                new Vector3(hx, 0, -iz), new Vector3(-hx, 0, -iz), bottom, bottom);

            // --- saplamalar: her hücrenin merkezine bir silindir ---
            for (int cx = 0; cx < w; cx++)
                for (int cz = 0; cz < h; cz++)
                    AddStud(verts, tris, colors,
                        new Vector3(-w * 0.5f + cx + 0.5f, Height, -h * 0.5f + cz + 0.5f), mid, top);

            var mesh = new Mesh { name = $"Brick_{w}x{h}" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true); // GPU'ya gönder, CPU kopyasını bırak
            return mesh;
        }

        /// <summary>Dört köşeli yüzey ekler; alt kenar <paramref name="c0"/>, üst kenar <paramref name="c1"/> renginde.</summary>
        static void AddQuad(List<Vector3> verts, List<int> tris, List<Color> colors,
            Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color c0, Color c1)
        {
            int start = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            colors.Add(c0); colors.Add(c0); colors.Add(c1); colors.Add(c1);
            tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
            tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
        }

        static void AddStud(List<Vector3> verts, List<int> tris, List<Color> colors,
            Vector3 center, Color side, Color cap)
        {
            int ringStart = verts.Count;
            float topY = center.y + StudHeight;

            // Yan yüzey halkası
            for (int i = 0; i < StudSegments; i++)
            {
                float angle = i / (float)StudSegments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * StudRadius;
                float z = Mathf.Sin(angle) * StudRadius;
                verts.Add(new Vector3(center.x + x, center.y, center.z + z));
                verts.Add(new Vector3(center.x + x, topY, center.z + z));
                colors.Add(side); colors.Add(cap);
            }

            for (int i = 0; i < StudSegments; i++)
            {
                int a = ringStart + i * 2;
                int b = ringStart + ((i + 1) % StudSegments) * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(b);
                tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
            }

            // Üst kapak (yelpaze). Sarım YÖNÜ önemli: Unity saat yönündeki
            // üçgeni ön yüz sayar. (merkez, a, b) sırası normali AŞAĞI çevirip
            // kapağı görünmez yapıyordu — saplamalar hilal gibi görünüyordu.
            int capCenter = verts.Count;
            verts.Add(new Vector3(center.x, topY, center.z));
            colors.Add(cap);
            for (int i = 0; i < StudSegments; i++)
            {
                int a = ringStart + i * 2 + 1;
                int b = ringStart + ((i + 1) % StudSegments) * 2 + 1;
                tris.Add(capCenter); tris.Add(b); tris.Add(a);
            }
        }
    }
}
