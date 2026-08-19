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
    /// DERS (gövde artık hücre hücre değil, TEK SİLÜETTEN örülüyor — 4. tur):
    /// Eski hâl her hücre için ayrı bir kutu çiziyor, yalnız komşusuz kenarlara
    /// duvar örüyordu. Sonuç KESKİN köşeli bir dikdörtgendi ve `brickInset`
    /// sıfırlandığında yan yana iki blok tek kütle gibi görünüyordu (H27).
    /// Şimdi <see cref="BrickSilhouette"/> bloğun dış çevre çizgisini yuvarlak
    /// köşeli tek bir halka olarak veriyor; üst yüz o halkanın üçgenlenmesi,
    /// yan duvar ise halkanın aşağı doğru süpürülmesi. Saplamalar hücre hücre
    /// kalıyor, çünkü onların ızgarası gerçekten hücreye bağlı.
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
            // YÖNLÜ BLOKTA SAPLAMA YOK (4. tur, H31).
            //
            // Referans (41-50 yürüyüşü, 50. bölüm): eksen kısıtlı bloklar
            // PÜRÜZSÜZ karolar; üstlerinde saplama değil, aynı renkten
            // kabartma bir çift yönlü ok var. Saplama ızgarası okun altında
            // kalınca ok "çıkartma" gibi duruyordu.
            bool studs = block.Axis == BlockOut.Core.MoveAxis.Free;

            int key = ShapeKey(block) ^ (studs ? 0 : unchecked((int)0x7A3C0000));
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var mesh = Build(block.W, block.H, block.Cells, withStuds: studs, grow: 0f);
            Cache[key] = mesh;
            return mesh;
        }

        /// <summary>
        /// Aynı şeklin SAPLAMASIZ hâli — iç katman paneli ve kontur kabuğu için.
        ///
        /// DERS (kontur kabuğu, gövdenin AYNISI olmamalı): Kontur ilk denemede
        /// tuğlanın kendi mesh'ini %4 büyüterek kuruldu. Silüet doğru çıktı ama
        /// SAPLAMALAR da büyüdü ve her saplamanın üstünde beyaz bir hilal
        /// belirdi — blok "beyaz benekli" göründü. Kabuğun işi silüeti
        /// çizmek; yüzeydeki ayrıntıyı taşımasına gerek yok, taşırsa da her
        /// ayrıntı kendi konturunu üretiyor.
        ///
        /// Ayrı önbellek anahtarı: aynı şeklin iki farklı mesh'i var.
        /// </summary>
        public static Mesh GetSilhouette(BlockOut.Core.BlockModel block)
        {
            int key = ShapeKey(block) ^ unchecked((int)0x5D1E0000);
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var mesh = Build(block.W, block.H, block.Cells, withStuds: false, grow: 0f);
            mesh.name += "_Silhouette";
            Cache[key] = mesh;
            return mesh;
        }

        /// <summary>
        /// Tutma konturunun kabuğu: silüetin dışa doğru SABİT kalınlıkta
        /// şişirilmiş hâli.
        ///
        /// DERS (kontur bir kalınlıktır, bir oran değil — 4. tur H29): Kabuk
        /// eskiden `localScale = 1.04` ile üretiliyordu. Ölçek merkezden
        /// çalıştığı için taşma miktarı bloğun boyuna bağlıydı: 6 hücrelik bir
        /// blokta 0,12 hücre taşıyor ve KOMŞU BLOĞUN İÇİNE giriyordu. Burada
        /// silüet dışa doğru `outlineWidth` kadar kaydırılıyor; kalınlık her
        /// blokta aynı ve komşuya taşma, boşluk payı kadar sınırlı.
        /// </summary>
        /// <summary>
        /// Seçili bloğun beyaz konturu: bloğun EN GENİŞ olduğu yükseklikte,
        /// silüetin hemen dışına oturan DÜZ bir halka.
        ///
        /// DERS (ters kabuk her kamerada çalışmaz): Kontur eskiden "inverted
        /// hull" ile çiziliyordu — blok büyütülüp ön yüzleri atılıyor, arka
        /// yüzleri beyaz görünüyordu. Bu teknik nesnenin ETRAFINI değil,
        /// kabuğun kameraya arkasını dönen kısmını boyar. Tepeden eğik bakan
        /// bir kamerada bu yalnız iki kenara denk geliyor; ölçtüm, referansta
        /// kontur DÖRT kenarda da 2-3 piksel (hücrenin ~%3,7'si).
        ///
        /// Halka çözümü açıdan bağımsız: iç kenarı bloğun gerçek silüeti, dış
        /// kenarı onun `outlineWidth` kadar dışa kaydırılmış hâli. İkisi de
        /// aynı `BrickSilhouette.Build` çağrısından geldiği için nokta
        /// sayıları ve sıraları birebir eşleşiyor — aralarını şerit olarak
        /// örmek yetiyor.
        ///
        /// Yükseklik neden `height - chamfer`? Blok orada tam genişliğinde.
        /// Halkayı bloğun ÜST yüzüne koysaydık, pah kadar yukarıda kalır ve
        /// eğik kamerada blokla halka arasında ince bir boşluk açılırdı.
        /// </summary>
        public static Mesh GetOutlineRing(BlockOut.Core.BlockModel block)
        {
            int key = ShapeKey(block) ^ unchecked((int)0x51170000);
            if (Cache.TryGetValue(key, out var ringCached) && ringCached != null) return ringCached;

            var cfg = VisualSettings.Current;
            float width = cfg != null ? cfg.outlineWidth : 0.05f;
            float height = cfg != null ? cfg.brickHeight : 0.40f;
            float inset = cfg != null ? cfg.brickInset : 0.055f;
            float chamfer = cfg != null ? cfg.brickChamfer : 0.06f;
            float corner = cfg != null ? cfg.brickCornerRadius : 0.15f;

            var inner = BrickSilhouette.Build(block.Cells, block.W, block.H, inset, corner);
            var outer = BrickSilhouette.Build(
                block.Cells, block.W, block.H, inset - width, corner + width);
            if (inner == null || outer == null || inner.Count < 3 || inner.Count != outer.Count)
                return new Mesh { name = "Brick_OutlineRing_Empty" };

            BrickSilhouette.MakeCounterClockwise(inner);
            BrickSilhouette.MakeCounterClockwise(outer);

            float y = Mathf.Max(0f, height - chamfer);
            int ringCount = inner.Count;
            var ringVerts = new Vector3[ringCount * 2];
            var ringNormals = new Vector3[ringCount * 2];
            for (int i = 0; i < ringCount; i++)
            {
                ringVerts[i * 2] = new Vector3(inner[i].x, y, inner[i].y);
                ringVerts[i * 2 + 1] = new Vector3(outer[i].x, y, outer[i].y);
                ringNormals[i * 2] = Vector3.up;
                ringNormals[i * 2 + 1] = Vector3.up;
            }

            var ringTris = new List<int>(ringCount * 6);
            RingQuads(ringTris, 0, ringCount);

            var ring = new Mesh { name = "Brick_OutlineRing" };
            ring.SetVertices(new List<Vector3>(ringVerts));
            ring.SetNormals(new List<Vector3>(ringNormals));
            ring.SetTriangles(ringTris, 0);
            ring.RecalculateBounds();
            Cache[key] = ring;
            return ring;
        }

        public static Mesh GetOutlineShell(BlockOut.Core.BlockModel block)
        {
            int key = ShapeKey(block) ^ unchecked((int)0x2B770000);
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var cfg = VisualSettings.Current;
            float width = cfg != null ? cfg.outlineWidth : 0.05f;
            var mesh = Build(block.W, block.H, block.Cells, withStuds: false, grow: width);
            mesh.name += "_Outline";
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

        /// <param name="grow">Silüeti dışa şişirme miktarı (kontur kabuğu için).</param>
        static Mesh Build(int w, int h, List<Vector2Int> cells, bool withStuds, float grow)
        {
            var cfg = VisualSettings.Current;
            float height = cfg != null ? cfg.brickHeight : 0.40f;
            float inset = cfg != null ? cfg.brickInset : 0.055f;
            float chamfer = cfg != null ? cfg.brickChamfer : 0.06f;
            float corner = cfg != null ? cfg.brickCornerRadius : 0.15f;
            int perCell = cfg != null ? cfg.studsPerCell : 2;

            // Kontur kabuğunda köşe yarıçapı da büyümeli: dışa kaydırılan bir
            // yayın yarıçapı, kaydırma kadar artar. Aksi hâlde kabuk köşelerde
            // gövdeden DAHA KESKİN kalır ve dört köşede beyaz sivri uçlar
            // görünür.
            float outline = Mathf.Max(0f, grow);
            var loop = BrickSilhouette.Build(cells, w, h, inset - outline, corner + outline);
            if (loop == null || loop.Count < 3)
                return new Mesh { name = "Brick_Empty" };

            BrickSilhouette.MakeCounterClockwise(loop);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            var colors = new List<Color>();

            float shoulder = Mathf.Max(0f, height - chamfer);

            Color bottom = Tone(cfg != null ? cfg.toneBodyBottom : 0.48f);
            Color side = Tone(cfg != null ? cfg.toneBodySide : 0.86f);
            Color face = Tone(cfg != null ? cfg.toneFaceTop : 0.72f);
            Color studFoot = Tone(cfg != null ? cfg.toneStudFoot : 0.42f);
            Color studTop = Tone(cfg != null ? cfg.toneStudTop : 1f);

            int count = loop.Count;

            // --- Yan duvar: tabandan omuza --------------------------------
            //
            // Halkanın dış normali, kenar teğetinin dik izdüşümü. Köşe
            // noktalarında komşu iki kenarın ortalaması alınıyor ki yay
            // boyunca ışık pürüzsüz kaysın.
            var outward = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var prev = loop[(i - 1 + count) % count];
                var next = loop[(i + 1) % count];
                var tangent = (next - prev).normalized;
                outward[i] = new Vector3(tangent.y, 0f, -tangent.x);
            }

            int wallStart = verts.Count;
            for (int i = 0; i < count; i++)
            {
                var p = loop[i];
                verts.Add(new Vector3(p.x, 0f, p.y));
                verts.Add(new Vector3(p.x, shoulder, p.y));
                normals.Add(outward[i]); normals.Add(outward[i]);
                colors.Add(bottom); colors.Add(side);
            }
            RingQuads(tris, wallStart, count);

            // --- Pah bandı: omuzdan üst yüze ------------------------------
            //
            // DERS (pahı GEOMETRİ ile yapmak): Eski hâl pahı yalnız eğik bir
            // NORMAL ile taklit ediyordu; silüet keskin kalıyor ve blok
            // kenarından bakınca "kesilmiş karton" gibi görünüyordu (H28).
            // Gerçek bir bant, tepeden bakan kamerada bile bloğun etrafında
            // ince bir açık şerit üretir — 3B kenarı okutan şey budur.
            float chamferInset = Mathf.Min(chamfer, 0.5f);
            int bevelStart = verts.Count;
            for (int i = 0; i < count; i++)
            {
                var p = loop[i];
                var n = outward[i];
                var top = new Vector3(p.x, height, p.y) - n * chamferInset;
                var bevelNormal = (n + Vector3.up * 1.1f).normalized;

                verts.Add(new Vector3(p.x, shoulder, p.y));
                verts.Add(top);
                normals.Add(bevelNormal); normals.Add(bevelNormal);
                colors.Add(side); colors.Add(face);
            }
            RingQuads(tris, bevelStart, count);

            // --- Üst yüz ---------------------------------------------------
            var topRing = new List<Vector2>(count);
            for (int i = 0; i < count; i++)
            {
                var p = loop[i];
                var n = outward[i];
                topRing.Add(new Vector2(p.x - n.x * chamferInset, p.y - n.z * chamferInset));
            }

            int faceStart = verts.Count;
            for (int i = 0; i < count; i++)
            {
                verts.Add(new Vector3(topRing[i].x, height, topRing[i].y));
                normals.Add(Vector3.up);
                colors.Add(face);
            }
            BrickSilhouette.Triangulate(topRing, tris, faceStart, faceUp: true);

            // --- Alt yüz ---------------------------------------------------
            //
            // Tepeden bakan kamerada hiç görünmez ama emilme animasyonunda
            // blok eğilebiliyor; açık kalan bir taban orada delik gibi durur.
            int bottomStart = verts.Count;
            for (int i = 0; i < count; i++)
            {
                verts.Add(new Vector3(loop[i].x, 0f, loop[i].y));
                normals.Add(Vector3.down);
                colors.Add(bottom);
            }
            BrickSilhouette.Triangulate(loop, tris, bottomStart, faceUp: false);

            // --- Saplamalar -------------------------------------------------
            if (withStuds)
            {
                float step = 1f / perCell;
                float first = step * 0.5f;
                foreach (var cell in cells)
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
