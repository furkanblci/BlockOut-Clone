using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Yuvarlak köşeli, üst kenarı pahlı alçak prizma — kapı barı, makine
    /// gövdesi ve benzeri "plastik parça" görünümlü nesnelerin ortak kalıbı.
    ///
    /// DERS (ikinci kullanıcı gelince ORTAK ET): Bu mesh önce
    /// <see cref="GateView"/> içinde özel bir metottu. Üreteç makinesi de aynı
    /// biçime ihtiyaç duyunca kopyalamak cazipti; ama kopyalanan geometri, bir
    /// gün köşe yarıçapı değiştiğinde iki farklı görünüm demek. Oyunun görsel
    /// dili tek bir yerden gelmeli — bloklar, kapılar ve makineler aynı
    /// yuvarlaklığı paylaştığı için "aynı malzemeden dökülmüş" görünüyorlar.
    /// </summary>
    public static class PrismMeshBuilder
    {
        /// <param name="sizeX">X eksenindeki boy.</param>
        /// <param name="sizeZ">Z eksenindeki boy.</param>
        /// <param name="height">Yükseklik; taban y = 0.</param>
        /// <param name="radius">Köşe yarıçapı (negatifse tuğla ayarından alınır).</param>
        /// <param name="bevel">Üst kenar pahı (negatifse tuğla ayarından alınır).</param>
        public static Mesh Build(float sizeX, float sizeZ, float height,
            float radius = -1f, float bevel = -1f, string name = "Prism")
        {
            var cfg = VisualSettings.Current;
            if (radius < 0f) radius = cfg != null ? cfg.brickCornerRadius : 0.16f;
            if (bevel < 0f) bevel = cfg != null ? cfg.brickChamfer : 0.06f;

            radius = Mathf.Min(radius, Mathf.Min(sizeX, sizeZ) * 0.5f);
            bevel = Mathf.Clamp(bevel, 0f, height * 0.45f);

            var loop = RoundedRect(sizeX * 0.5f, sizeZ * 0.5f, radius);
            int count = loop.Count;

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            var outward = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var prev = loop[(i - 1 + count) % count];
                var next = loop[(i + 1) % count];
                var tangent = (next - prev).normalized;
                outward[i] = new Vector3(tangent.y, 0f, -tangent.x);
            }

            void Ring(System.Func<int, Vector3> lower, System.Func<int, Vector3> upper,
                      System.Func<int, Vector3> normal)
            {
                int start = verts.Count;
                for (int i = 0; i < count; i++)
                {
                    verts.Add(lower(i)); verts.Add(upper(i));
                    var n = normal(i);
                    normals.Add(n); normals.Add(n);
                }
                for (int i = 0; i < count; i++)
                {
                    int a = start + i * 2;
                    int b = start + ((i + 1) % count) * 2;
                    tris.Add(a); tris.Add(a + 1); tris.Add(b);
                    tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
                }
            }

            float shoulder = height - bevel;
            Ring(i => new Vector3(loop[i].x, 0f, loop[i].y),
                 i => new Vector3(loop[i].x, shoulder, loop[i].y),
                 i => outward[i]);
            Ring(i => new Vector3(loop[i].x, shoulder, loop[i].y),
                 i => new Vector3(loop[i].x, height, loop[i].y) - outward[i] * bevel,
                 i => (outward[i] + Vector3.up).normalized);

            var top = new List<Vector2>(count);
            for (int i = 0; i < count; i++)
                top.Add(new Vector2(loop[i].x - outward[i].x * bevel,
                                    loop[i].y - outward[i].z * bevel));

            int capStart = verts.Count;
            foreach (var p in top)
            {
                verts.Add(new Vector3(p.x, height, p.y));
                normals.Add(Vector3.up);
            }
            BrickSilhouette.Triangulate(top, tris, capStart, faceUp: true);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        /// <summary>Yuvarlatılmış dikdörtgenin çevre noktaları (saat yönünün tersine).</summary>
        public static List<Vector2> RoundedRect(float halfX, float halfZ, float radius)
        {
            const int segments = 4;
            var points = new List<Vector2>((segments + 1) * 4);
            var centers = new[]
            {
                new Vector2(halfX - radius, -halfZ + radius),
                new Vector2(halfX - radius, halfZ - radius),
                new Vector2(-halfX + radius, halfZ - radius),
                new Vector2(-halfX + radius, -halfZ + radius)
            };
            float[] start = { -90f, 0f, 90f, 180f };

            for (int c = 0; c < 4; c++)
                for (int s = 0; s <= segments; s++)
                {
                    float angle = (start[c] + s / (float)segments * 90f) * Mathf.Deg2Rad;
                    points.Add(centers[c] +
                        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            return points;
        }
    }
}
