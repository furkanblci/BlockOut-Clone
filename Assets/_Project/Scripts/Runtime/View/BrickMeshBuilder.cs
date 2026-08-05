using System.Collections.Generic;
using BlockOut.Runtime.Config;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// LEGO görünümlü tuğla mesh'lerini PROSEDÜREL üretir ve önbelleğe alır.
    ///
    /// DERS (neden prosedürel?): 1×1'den 5×5'e kadar her boyut için ayrı model
    /// çizmek onlarca asset demek. Tuğla düzenli bir geometri olduğu için kodla
    /// üretmek hem dosya sayısını sıfırlar hem de "saplama yarıçapını büyüt"
    /// gibi ayarları tek kaydırıcıya bağlar.
    ///
    /// DERS (normalleri ELLE yazmak): RecalculateNormals komşu üçgenlerin
    /// normallerini ortalar; saplamalar köşeli görünür. Doğru sonuç için
    /// normaller geometriyi bilerek yazılır: kutu yüzleri DÜZ, saplama yanları
    /// MERKEZDEN DIŞA (radyal). Radyal normal silindiri pürüzsüz gösterir ve
    /// parlaklık saplamanın üstünde kayar.
    ///
    /// Tüm ölçüler <see cref="BlockVisualConfigSO"/>'dan gelir; ayar değişince
    /// <see cref="ClearCache"/> ile mesh'ler yeniden üretilir.
    /// </summary>
    public static class BrickMeshBuilder
    {
        static readonly Dictionary<int, Mesh> Cache = new Dictionary<int, Mesh>();
        static BlockVisualConfigSO _config;

        /// <summary>Geçerli ayarların yüksekliği (view'lar konumlandırmada kullanır).</summary>
        public static float Height => _config != null ? _config.brickHeight : 0.40f;

        public static void Configure(BlockVisualConfigSO config)
        {
            if (ReferenceEquals(_config, config)) return;
            _config = config;
            ClearCache();
        }

        /// <summary>Ayar değişti: önbellekteki mesh'ler artık geçersiz.</summary>
        public static void ClearCache()
        {
            foreach (var mesh in Cache.Values)
                if (mesh != null) Object.DestroyImmediate(mesh);
            Cache.Clear();
        }

        /// <summary>
        /// Blok şekli için mesh. Anahtar, şeklin kendisinden üretilir; aynı
        /// şekildeki tüm bloklar tek mesh paylaşır (dikdörtgenler dahil).
        /// </summary>
        public static Mesh Get(BlockOut.Core.BlockModel block)
        {
            int key = ShapeKey(block);
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var mesh = Build(block.W, block.H, block.Cells);
            Cache[key] = mesh;
            return mesh;
        }

        /// <summary>Şeklin sayısal parmak izi — hücre maskesini bit bit toplar.</summary>
        static int ShapeKey(BlockOut.Core.BlockModel block)
        {
            int key = block.W * 31 + block.H * 131;
            foreach (var cell in block.Cells)
                key = key * 17 + (cell.y * 8 + cell.x + 1);
            return key;
        }

        static Mesh Build(int w, int h, List<Vector2Int> cells)
        {
            var cfg = VisualSettings.Current;
            float height = cfg != null ? cfg.brickHeight : 0.40f;
            float inset = cfg != null ? cfg.brickInset : 0.055f;
            float chamfer = cfg != null ? cfg.brickChamfer : 0.06f;
            int perCell = cfg != null ? cfg.studsPerCell : 2;

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            var colors = new List<Color>();

            float shoulder = height - chamfer;

            Color bottom = Tone(cfg != null ? cfg.toneBodyBottom : 0.48f);
            Color side = Tone(cfg != null ? cfg.toneBodySide : 0.86f);
            Color face = Tone(cfg != null ? cfg.toneFaceTop : 0.72f);
            Color studFoot = Tone(cfg != null ? cfg.toneStudFoot : 0.42f);
            Color studTop = Tone(cfg != null ? cfg.toneStudTop : 1f);

            // Hücre kümesi: komşuluk sorgusu için.
            var filled = new HashSet<Vector2Int>(cells);
            bool Has(int x, int z) => filled.Contains(new Vector2Int(x, z));

            // DERS (polyomino gövdesi): Her hücre için kutu çizip birleştirmek
            // iç yüzleri de üretir — hem israf hem de saydam olmayan yüzeylerde
            // z-fighting kaynağı. Bunun yerine yalnızca KOMŞUSU OLMAYAN kenarlara
            // duvar örülür; iç kenarlar hiç var olmaz. Boşluk payı (inset) da
            // yalnızca dış kenarlara uygulanır ki bitişik hücreler kusursuz
            // birleşsin, blok dışarıdan tek parça görünsün.
            foreach (var cell in cells)
            {
                bool left = Has(cell.x - 1, cell.y);
                bool right = Has(cell.x + 1, cell.y);
                bool up = Has(cell.x, cell.y - 1);     // hücre uzayında y aşağı artar
                bool down = Has(cell.x, cell.y + 1);

                float x0 = -w * 0.5f + cell.x + (left ? 0f : inset);
                float x1 = -w * 0.5f + cell.x + 1f - (right ? 0f : inset);
                // Hücre uzayı y aşağı, dünya Z yukarı: ters çevrilir.
                float z1 = h * 0.5f - cell.y - (up ? 0f : inset);
                float z0 = h * 0.5f - cell.y - 1f + (down ? 0f : inset);

                // Üst yüz (hücre başına; bitişik hücrelerde kusursuz döşenir)
                Quad(verts, normals, tris, colors, Vector3.up,
                    new Vector3(x0, height, z0), new Vector3(x1, height, z0),
                    new Vector3(x1, height, z1), new Vector3(x0, height, z1), face, face);

                // Alt yüz
                Quad(verts, normals, tris, colors, Vector3.down,
                    new Vector3(x0, 0, z1), new Vector3(x1, 0, z1),
                    new Vector3(x1, 0, z0), new Vector3(x0, 0, z0), bottom, bottom);

                // Dış kenarlar: yan duvar + pah bandı (pah, eğik NORMAL ile
                // taklit edilir; geometriyi içeri kaçırmak polyomino köşelerinde
                // boşluk bırakırdı).
                if (!down)
                {
                    Quad(verts, normals, tris, colors, Vector3.back,
                        new Vector3(x0, 0, z0), new Vector3(x1, 0, z0),
                        new Vector3(x1, shoulder, z0), new Vector3(x0, shoulder, z0), bottom, side);
                    Quad(verts, normals, tris, colors, new Vector3(0, 0.7f, -0.7f),
                        new Vector3(x0, shoulder, z0), new Vector3(x1, shoulder, z0),
                        new Vector3(x1, height, z0), new Vector3(x0, height, z0), side, face);
                }
                if (!up)
                {
                    Quad(verts, normals, tris, colors, Vector3.forward,
                        new Vector3(x1, 0, z1), new Vector3(x0, 0, z1),
                        new Vector3(x0, shoulder, z1), new Vector3(x1, shoulder, z1), bottom, side);
                    Quad(verts, normals, tris, colors, new Vector3(0, 0.7f, 0.7f),
                        new Vector3(x1, shoulder, z1), new Vector3(x0, shoulder, z1),
                        new Vector3(x0, height, z1), new Vector3(x1, height, z1), side, face);
                }
                if (!right)
                {
                    Quad(verts, normals, tris, colors, Vector3.right,
                        new Vector3(x1, 0, z0), new Vector3(x1, 0, z1),
                        new Vector3(x1, shoulder, z1), new Vector3(x1, shoulder, z0), bottom, side);
                    Quad(verts, normals, tris, colors, new Vector3(0.7f, 0.7f, 0),
                        new Vector3(x1, shoulder, z0), new Vector3(x1, shoulder, z1),
                        new Vector3(x1, height, z1), new Vector3(x1, height, z0), side, face);
                }
                if (!left)
                {
                    Quad(verts, normals, tris, colors, Vector3.left,
                        new Vector3(x0, 0, z1), new Vector3(x0, 0, z0),
                        new Vector3(x0, shoulder, z0), new Vector3(x0, shoulder, z1), bottom, side);
                    Quad(verts, normals, tris, colors, new Vector3(-0.7f, 0.7f, 0),
                        new Vector3(x0, shoulder, z1), new Vector3(x0, shoulder, z0),
                        new Vector3(x0, height, z0), new Vector3(x0, height, z1), side, face);
                }

                // Saplamalar — hücre başına perCell²
                float step = 1f / perCell;
                float first = step * 0.5f;
                for (int sx = 0; sx < perCell; sx++)
                    for (int sz = 0; sz < perCell; sz++)
                        AddStud(verts, normals, tris, colors, new Vector3(
                            -w * 0.5f + cell.x + first + sx * step,
                            height,
                            h * 0.5f - cell.y - first - sz * step), studFoot, studTop);
            }

            var mesh = new Mesh { name = $"Brick_{w}x{h}" };
            if (verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.SetColors(colors);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        static Color Tone(float value) => new Color(value, value, value, 1f);

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

        /// <summary>
        /// Bir saplama: dik yan duvar + PAHLI üst kenar + düz kapak.
        /// Pah şart: tepeden bakan kamerada dik silindirin yanı görünmez ve
        /// saplama düz daireye döner; pahlı kenar parlak bir halka oluşturur.
        /// </summary>
        static void AddStud(List<Vector3> verts, List<Vector3> normals, List<int> tris,
            List<Color> colors, Vector3 center, Color baseTone, Color brightTone)
        {
            var cfg = VisualSettings.Current;
            float radius = cfg != null ? cfg.studRadius : 0.168f;
            float studHeight = cfg != null ? cfg.studHeight : 0.115f;
            float bevel = Mathf.Min(cfg != null ? cfg.studBevel : 0.035f, radius * 0.6f);
            int segments = cfg != null ? cfg.studSegments : 14;

            float topY = center.y + studHeight;
            float shoulderY = topY - bevel;
            float capRadius = radius - bevel;

            Color footTone = baseTone;
            Color shoulderTone = Color.Lerp(baseTone, brightTone, 0.75f);
            Color rimTone = brightTone;
            Color capTone = Color.Lerp(brightTone, shoulderTone, 0.25f);

            int sideStart = verts.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                var radial = new Vector3(cos, 0f, sin);

                verts.Add(center + radial * radius);
                verts.Add(new Vector3(center.x + cos * radius, shoulderY, center.z + sin * radius));
                normals.Add(radial); normals.Add(radial);
                colors.Add(footTone); colors.Add(shoulderTone);
            }
            RingQuads(tris, sideStart, segments);

            int bevelStart = verts.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                var bevelNormal = new Vector3(cos, 1.1f, sin).normalized;

                verts.Add(new Vector3(center.x + cos * radius, shoulderY, center.z + sin * radius));
                verts.Add(new Vector3(center.x + cos * capRadius, topY, center.z + sin * capRadius));
                normals.Add(bevelNormal); normals.Add(bevelNormal);
                colors.Add(shoulderTone); colors.Add(rimTone);
            }
            RingQuads(tris, bevelStart, segments);

            int capStart = verts.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                verts.Add(new Vector3(
                    center.x + Mathf.Cos(angle) * capRadius, topY,
                    center.z + Mathf.Sin(angle) * capRadius));
                normals.Add(Vector3.up);
                colors.Add(capTone);
            }

            int capCenter = verts.Count;
            verts.Add(new Vector3(center.x, topY, center.z));
            normals.Add(Vector3.up);
            colors.Add(capTone);

            for (int i = 0; i < segments; i++)
            {
                int a = capStart + i;
                int b = capStart + (i + 1) % segments;
                tris.Add(capCenter); tris.Add(b); tris.Add(a);
            }
        }

        static void RingQuads(List<int> tris, int ringStart, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                int a = ringStart + i * 2;
                int b = ringStart + ((i + 1) % segments) * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(b);
                tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
            }
        }
    }
}
