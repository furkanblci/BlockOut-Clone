using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Kapının sahnedeki görseli: kenarın hemen dışında, duvar yüksekliğinde
    /// renkli bar + üstünde çıkış yönünü gösteren kabartmalı ok.
    ///
    /// Ölçüler referans oyuna göre: bar duvarla aynı yükseklikte ve ondan
    /// KALIN — ince bir şerit tepeden bakıldığında kaybolur ve "buradan
    /// çıkılıyor" mesajını vermez.
    /// </summary>
    public sealed class GateView : MonoBehaviour
    {

        // Kapı ölçüleri: "çerçeveyle aynı" seçiliyse kapı ÇERÇEVE BANDINI
        // birebir doldurur — referans oyunda kapı, çerçevenin renkli bir
        // parçası gibi görünür; ayrı bir çıkıntı yoktur.
        //
        // Konumlandırma: tahta kenarı ile çerçevenin dış kenarı arasındaki
        // bandın ORTASINA oturur, yani dışa kaydırma = kalınlığın yarısı.
        static bool MatchesFrame => VisualSettings.Current == null ||
                                    VisualSettings.Current.gateMatchesWall;

        // Z-FIGHTING: kapı ile çerçeve tam olarak aynı hacmi kaplarsa yüzeyler
        // çakışır ve kamera açısına göre titreyen tırtıklı kenarlar oluşur.
        // Kapıyı bir tık büyük yapmak çakışmayı kaldırır; fark gözle görülmez.
        const float FrameOverlapBias = 0.02f;

        /// <summary>
        /// Kapı çerçeveden BİR TIK YÜKSEK durur.
        ///
        /// DERS (aynı hizadaki iki yüzey tek parça okunur — 4. tur): Kapı
        /// çerçeveyle tıpatıp aynı yükseklik ve derinlikteydi; sonuç, çerçeveye
        /// SÜRÜLMÜŞ renkli bir şerit gibi görünmesiydi. Referansta kapı ayrı
        /// bir parça: çerçevenin üstünden biraz taşıyor ve dışa doğru
        /// çıkıntı yapıyor. Yükseklik farkı, "bu bir kapak" bilgisini
        /// tek başına taşıyor.
        /// </summary>
        static float BarHeight => VisualSettings.Current == null ? 0.34f
            : MatchesFrame ? VisualSettings.Current.frameHeight * 1.14f
                           : VisualSettings.Current.gateBarHeight;

        static float BarDepth => VisualSettings.Current == null ? 0.55f
            : MatchesFrame ? VisualSettings.Current.frameThickness * 1.0f
                           : VisualSettings.Current.gateBarDepth;

        /// <summary>
        /// Barın merkezi, tahta kenarından DIŞA doğru bu kadar uzakta:
        /// çerçeve bandının TAM ORTASI.
        ///
        /// ÖLÇÜM (Levels 1-20 yürüyüşü, 00:12 karesi): üstteki kırmızı kapının
        /// dış kenarı çerçevenin dış kenarıyla AYNI hizada, iç kenarı da tahta
        /// zemininin başladığı yerde. Yani kapı bandı doldurur, dışarı TAŞMAZ.
        /// Kapıyı dışarı çıkarma denemesi (kalınlığın %66'sı) onu çerçeveye
        /// yapıştırılmış bir dil gibi gösterdi; ayrımı yapan şey konum değil,
        /// YÜKSEKLİK farkı.
        /// </summary>
        static float OutwardOffset => VisualSettings.Current == null ? 0.275f
            : MatchesFrame ? VisualSettings.Current.frameThickness * 0.5f
                           : VisualSettings.Current.gateOutwardOffset;

        MeshRenderer _renderer;
        Material _colorMaterial;
        TMPro.TextMeshPro _iceCounter;
        GateModel _model;
        GameObject _arrow;

        public static GateView Create(
            Transform parent, GateModel model, BoardSpace space, Material colorMaterial)
        {
            var go = new GameObject($"Gate_{model.ActiveColor}_{model.Side.ToId()}");
            go.transform.SetParent(parent, worldPositionStays: false);
            // Kapı barının kenar boyunca kapladığı aralık — açıklığın tamamı.
            ResolveSpan(model, out float barMin, out float barMax);

            float spanCenter = (barMin + barMax) * 0.5f;
            float barLength = Mathf.Max(0.25f, barMax - barMin);
            float offCoord = model.EdgeCoord + model.OutwardSign * OutwardOffset;

            Vector3 center;
            float alongX;   // barın X eksenindeki uzunluğu
            float alongZ;
            if (model.EdgeHorizontal)
            {
                center = space.CornerToWorld(spanCenter, offCoord, 0f);
                alongX = barLength; alongZ = BarDepth;
            }
            else
            {
                center = space.CornerToWorld(offCoord, spanCenter, 0f);
                alongX = BarDepth; alongZ = barLength;
            }

            // KAPI ARTIK KESKİN BİR KÜP DEĞİL (4. tur, G20/G26).
            //
            // Kullanıcı: "Kapılar köşelerden taşıyor... Kapılar duvarla iç içe
            // geçmeyecek."
            //
            // DERS (keskin bir kutu, yuvarlak bir çerçevenin içine sığmaz):
            // Bar `PrimitiveType.Cube` idi. Tahtanın köşesine dayanan bir kapı
            // (1., 2., 4., 5. bölümler) çerçevenin yuvarlak köşesinin DIŞINA
            // taşıyor ve sivri bir dilim olarak sırıtıyordu. Referansta her
            // kapı yuvarlak köşeli bir plastik parça; köşeye dayandığında
            // yayı çerçevenin yayını izliyor. Silüeti düzeltmek, barı
            // kısaltmaktan (yani açıklığı hakkında yalan söylemekten) iyi.
            go.AddComponent<MeshFilter>().sharedMesh = BuildBarMesh(alongX, alongZ, BarHeight);
            go.AddComponent<MeshRenderer>();
            go.transform.position = center;

            var view = go.AddComponent<GateView>();
            view._model = model;
            view._renderer = go.GetComponent<MeshRenderer>();
            view._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view._colorMaterial = colorMaterial;
            view._arrow = CreateArrow(parent, model, center);

            if (model.IsIced)
            {
                // Buz rengi GİZLER (video kuralı) — bar buz materyaliyle başlar.
                view._renderer.sharedMaterial = ViewKit.GateIce;
                // Donmuş KAPININ sayacı krem: kapının buzu bloğunkinden çok
                // daha soluk, rakam da onun açık tonunda.
                //
                // Yükseklik çizicinin KENDİ sınırından; sabit hesap barın
                // gerçek tepesini ıskalayıp rakamı buzun içine gömüyordu
                // (bkz. BlockView'daki aynı ders).
                // DONMUŞ KAPIDA OK YOK, SAYI VAR.
                //
                // Referans (`menus,powerups,vs.mp4` 01:25 ve 01:47): buzlu
                // kapıların üstünde yalnız sayaç duruyor; ok ancak buz
                // kırılınca beliriyor. Bizde ikisi birden çiziliyordu ve ok
                // sayacın ÜSTÜNDE kalıyordu (ölçüm: ok y=0.97, sayaç y=0.90),
                // yani rakamın yarısını kapatıyordu.
                //
                // DERS (aynı yere iki şey koyma): Buz zaten "bu kapı şu an
                // kullanılamaz" diyor; ok ise "buradan çıkabilirsin" diyor.
                // İkisi aynı anda doğru olamaz. `BlockView` donmuş blokta
                // eksen okunu zaten gizliyordu; kapıda bu adım atlanmıştı.
                if (view._arrow != null) view._arrow.SetActive(false);

                float barTop = view._renderer.bounds.max.y;
                view._iceCounter = ViewKit.CreateCounter(
                    parent, new Vector3(center.x, barTop + 0.08f, center.z), model.IceCount,
                    ViewKit.CounterStyle.Frost);
            }
            else
            {
                view._renderer.sharedMaterial = colorMaterial;
            }

            return view;
        }

        /// <summary>
        /// Kapı barının mesh'i: yuvarlak köşeli, üst kenarı pahlı alçak prizma.
        ///
        /// Ölçüler bloklarla AYNI dilden geliyor: köşe yarıçapı ve pah,
        /// <see cref="Config.BlockVisualConfigSO"/>'daki tuğla değerlerinin
        /// aynısı. Referansta kapı ile blok aynı malzemeden dökülmüş gibi
        /// duruyor; iki ayrı yuvarlaklık kullanmak o birliği bozardı.
        /// </summary>
        static Mesh BuildBarMesh(float sizeX, float sizeZ, float height)
        {
            var cfg = VisualSettings.Current;
            float radius = cfg != null ? cfg.brickCornerRadius : 0.16f;
            float bevel = cfg != null ? cfg.brickChamfer : 0.06f;

            // Kapının uçları BLOKTAN daha yuvarlak: uçlar çerçevenin köşe
            // yayına dayandığında keskin kalan her köşe dışarı taşıyor
            // (4. tur G20). Yarıçapı barın DAR kenarının yarısına kadar
            // açmak, ucu yarım daireye çevirip taşmayı kapatıyor.
            radius = Mathf.Max(radius, Mathf.Min(sizeX, sizeZ) * 0.44f);
            radius = Mathf.Min(radius, Mathf.Min(sizeX, sizeZ) * 0.5f);
            bevel = Mathf.Clamp(bevel, 0f, height * 0.4f);

            var loop = RoundedRect(sizeX * 0.5f, sizeZ * 0.5f, radius);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            int count = loop.Count;

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

            var mesh = new Mesh { name = "GateBar" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        /// <summary>Yuvarlatılmış dikdörtgenin çevre noktaları (saat yönünün tersine).</summary>
        static List<Vector2> RoundedRect(float halfX, float halfZ, float radius)
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

        /// <summary>
        /// Barın kenar boyunca kaplayacağı aralığı verir: açıklığın TAMAMI,
        /// iki ucundan yalnız kıl payı çıkarılmış hâli.
        ///
        /// Eski hâli köşeye dayanan ucu köşe yarıçapı kadar içeri çekiyordu;
        /// gerekçesi barın çerçevenin yuvarlak köşesine girmesiydi. Ölçüm o
        /// çözümün bedelini gösterdi (aşağıya bakın): kapı açıklığından %25
        /// dar çiziliyordu. Kozmetik kusur geri geldi, işlevsel yalan gitti.
        /// </summary>
        static void ResolveSpan(GateModel model, out float min, out float max)
        {
            min = model.SpanMin;
            max = model.SpanMax;

            // KAPI TAM AÇIKLIĞINI KAPLAR — kırpılmaz.
            //
            // Eskiden tahta köşesine dayanan uç `clearance` kadar içeri
            // alınıyordu ve o pay `min(köşeYarıçapı * 0.75, açıklık * 0.35)`
            // idi. 2 hücrelik bir kapıda bu **0.45 hücre** ediyor: kapı
            // ekranda 2 değil 1.5 hücre çiziliyordu.
            //
            // DERS (kapının genişliği bir SÖZDÜR): Kapının açıklığı oyuncuya
            // "bu genişlikte bir blok buradan geçer" der. Barı kısaltmak o
            // sözü bozuyor — 2 hücrelik blok 2 hücrelik kapının önünde
            // "sığmayacak" gibi görünüyordu ve oyuncu hizalamayı yanlış
            // sanıyordu. Referans ölçüldü (`menus,powerups,vs.mp4` 01:25):
            // 2 hücrelik kapı 95 piksel, hücre 46,4 piksel — tam 2 hücre,
            // hiç kırpma yok.
            //
            // Kırpma KOZMETİK bir sorunu çözmek için konmuştu: köşeye dayanan
            // ucun çerçevenin yuvarlak köşesinin içine girmesi. Ama kozmetik
            // bir kusuru, işlevsel bir yalanla değiştirmek kötü bir takas.
            // Köşeye dayanan kapı çerçeve yayına değebilir; bu, kısa bir
            // bardan çok daha küçük bir günah.
            //
            // Kalan pay yalnız KOMŞU KAPILARI ayırmak için, kıl payı:
            // yan yana iki kapı aynı renkteyse tek parça gibi okunuyordu.
            const float SeamInset = 0.02f;
            min += SeamInset;
            max -= SeamInset;
        }

        /// <summary>
        /// Kapının üstündeki ok: düz üçgen değil ALÇAK PRİZMA. Düz üçgen tek
        /// renk kalır ve "detaysız" görünür; prizmanın yan yüzleri ışığı farklı
        /// açıyla aldığı için kenarları belirginleşir.
        /// </summary>
        static GameObject CreateArrow(Transform parent, GateModel model, Vector3 barCenter)
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(parent, worldPositionStays: false);

            var cfg = VisualSettings.Current;
            float size = cfg != null ? cfg.arrowSize : 0.20f;
            float rise = cfg != null ? cfg.arrowRise : 0.055f;
            float cornerRadius = cfg != null ? cfg.arrowCornerRadius : 0.3f;

            // Dışarı yönü: yatay kapılarda ±Z, dikey kapılarda ±X.
            Vector3 forward = model.EdgeHorizontal
                ? new Vector3(0f, 0f, -model.OutwardSign)
                : new Vector3(model.OutwardSign, 0f, 0f);
            Vector3 across = new Vector3(-forward.z, 0f, forward.x);

            Vector3 tip = forward * size;
            Vector3 left = across * size * 0.78f - forward * size * 0.48f;
            Vector3 right = -across * size * 0.78f - forward * size * 0.48f;

            // Referans oyunda okun köşeleri YUMUŞAK; keskin üçgen sert ve
            // "vektör klibi" gibi duruyor. Her köşeyi küçük bir yay ile
            // yuvarlıyoruz (köşe kesme + ara noktalar).
            var outline = RoundedTriangle(tip, left, right, cornerRadius);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            float top = rise;

            // Üst yüz: merkezden yelpaze.
            Vector3 center = (tip + left + right) / 3f;
            int capCenter = verts.Count;
            verts.Add(center + Vector3.up * top);
            normals.Add(Vector3.up);
            colors.Add(Color.white);

            int capStart = verts.Count;
            foreach (var point in outline)
            {
                verts.Add(point + Vector3.up * top);
                normals.Add(Vector3.up);
                colors.Add(Color.white);
            }
            for (int i = 0; i < outline.Count; i++)
            {
                int a = capStart + i;
                int b = capStart + (i + 1) % outline.Count;
                tris.Add(capCenter); tris.Add(b); tris.Add(a);
            }

            // Yan yüzler: kabartma hissi (düz üçgen tek renk kalıyordu).
            for (int i = 0; i < outline.Count; i++)
            {
                Vector3 a = outline[i];
                Vector3 b = outline[(i + 1) % outline.Count];
                Vector3 edge = (b - a).normalized;
                Vector3 outward = Vector3.Cross(Vector3.up, edge).normalized;

                int start = verts.Count;
                verts.Add(a); verts.Add(b);
                verts.Add(b + Vector3.up * top); verts.Add(a + Vector3.up * top);
                for (int n = 0; n < 4; n++) normals.Add(outward);
                colors.Add(new Color(0.66f, 0.66f, 0.66f));
                colors.Add(new Color(0.66f, 0.66f, 0.66f));
                colors.Add(Color.white); colors.Add(Color.white);

                tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
                tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
            }

            var mesh = new Mesh { name = "GateArrow" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ViewKit.ArrowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Bar artık TABANINDAN konumlanıyor (mesh y=0'dan başlıyor), bu
            // yüzden ok barın TAM YÜKSEKLİĞİ kadar kaldırılıyor; eskiden küpün
            // merkezine göre yarım yükseklikti.
            go.transform.position = barCenter + Vector3.up * (BarHeight + 0.005f);
            return go;
        }

        /// <summary>
        /// Üçgenin köşelerini yuvarlatıp kapalı bir dış çizgi noktası listesi verir.
        /// <paramref name="radius01"/> 0 = keskin üçgen, 1 = maksimum yumuşama.
        /// </summary>
        static List<Vector3> RoundedTriangle(Vector3 a, Vector3 b, Vector3 c, float radius01)
        {
            var corners = new[] { a, b, c };
            var outline = new List<Vector3>();

            if (radius01 <= 0.001f)
            {
                outline.AddRange(corners);
                return outline;
            }

            const int arcSegments = 4;
            for (int i = 0; i < 3; i++)
            {
                Vector3 prev = corners[(i + 2) % 3];
                Vector3 corner = corners[i];
                Vector3 next = corners[(i + 1) % 3];

                // Köşeye komşu kenarlar boyunca içeri kaçış noktaları.
                float cut = Mathf.Clamp01(radius01) * 0.42f;
                Vector3 from = Vector3.Lerp(corner, prev, cut);
                Vector3 to = Vector3.Lerp(corner, next, cut);

                for (int s = 0; s <= arcSegments; s++)
                {
                    float t = s / (float)arcSegments;
                    // Köşeyi kontrol noktası kabul eden ikinci derece Bézier:
                    // yay gibi yumuşak bir geçiş verir, trigonometri gerekmez.
                    Vector3 p = Vector3.Lerp(
                        Vector3.Lerp(from, corner, t),
                        Vector3.Lerp(corner, to, t), t);
                    outline.Add(p);
                }
            }
            return outline;
        }

        /// <summary>
        /// Kapı bir bloğu YUTARKEN kısa bir parlama (4. tur, G23).
        ///
        /// Kullanıcı: "Blok parçalanması + kapının objeyi alırken aydınlanması
        /// efekti bizde yok."
        ///
        /// DERS (bir olayın İKİ tarafı da tepki vermeli): Emilmede yalnız blok
        /// oynuyordu — küçülüyor, beyazlıyor, kırıntıya dönüşüyordu. Kapı ise
        /// hiç kımıldamıyor; ekranda bloğun "kaybolduğu" görülüyor ama onu
        /// KİMİN aldığı görünmüyordu. Kapının bir an aydınlanması, olayı iki
        /// nesne arasındaki bir alışverişe çeviriyor.
        ///
        /// Parlama kapının KENDİ rengi üzerinden yürüyor (beyaza doğru), yani
        /// hangi kapının yuttuğu bilgisi kaybolmuyor; ayrıca bar boyunca
        /// hafifçe şişiyor — ışık tek başına küçük bir ekranda zayıf kalıyor.
        /// </summary>
        public void PlayAbsorbFlash()
        {
            if (!isActiveAndEnabled || _renderer == null || !_renderer.enabled) return;
            if (_flash != null) StopCoroutine(_flash);
            _flash = StartCoroutine(FlashRoutine());
        }

        Coroutine _flash;

        System.Collections.IEnumerator FlashRoutine()
        {
            const float duration = 0.26f;

            var shared = _renderer.sharedMaterial;
            Color baseColor = ReadColor(shared);
            // Kendi örneği: paylaşılan materyali boyamak aynı renkteki BÜTÜN
            // kapıları birlikte parlatırdı.
            var glow = ViewKit.Translucent(baseColor);
            _renderer.sharedMaterial = glow;

            Vector3 startScale = transform.localScale;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                // Hızlı yükselip yavaş inen bir vuruş: ani parlama gözü
                // yakalar, yavaş iniş "bitti" der.
                float pulse = k < 0.22f ? k / 0.22f : 1f - (k - 0.22f) / 0.78f;
                Paint(glow, Color.Lerp(baseColor, Color.white, pulse * 0.85f));
                transform.localScale = startScale * (1f + 0.09f * pulse);
                yield return null;
            }

            transform.localScale = startScale;
            // Sönmüş bir kapıya dönmüş olabiliriz; o durumda materyali geri
            // yazmak parlamayı kalıcı kılardı.
            if (_fade == null) _renderer.sharedMaterial = shared;
            if (glow != null) Destroy(glow);
            _flash = null;
        }

        public void UpdateIceCount()
        {
            if (_iceCounter != null)
                _iceCounter.text = _model.IceCount.ToString();
        }

        /// <summary>Buz kırıldı: gizli renk ve ok ortaya çıkar.</summary>
        public void RevealColor()
        {
            if (_iceCounter != null) Destroy(_iceCounter.gameObject);
            _iceCounter = null;
            _renderer.sharedMaterial = _colorMaterial;

            // Ok buz boyunca gizliydi; kapı artık kullanılabilir olduğuna göre
            // "buradan çıkabilirsin" işareti geri gelmeli.
            if (_arrow != null) _arrow.SetActive(true);
        }

        /// <summary>Kuyruk ilerledi: yeni aktif rengin materyali (L21+ olasılığı).</summary>
        public void SetColorMaterial(Material material)
        {
            _colorMaterial = material;
            if (!_model.IsIced) _renderer.sharedMaterial = material;
        }

        /// <summary>
        /// Rengi tükendi: bar soluklaşır, ok da soluk materyale geçer.
        ///
        /// Kapı GİZLENMEZ — referans oyunda da kapı yerinde durup solar.
        /// Gizlemek duvarda boşluk bırakıyordu (kapı kenarına duvar örülmez).
        /// </summary>
        public void SetGhost(Material ghostMaterial)
        {
            // GEÇİŞ ANİDEN DEĞİL, SÖNEREK.
            //
            // DERS (durum değişimi bir OLAYDIR): Kapı işini bitirdiğinde
            // materyali tek karede ghost'a çevriliyordu; ekranda renk "pat"
            // diye değişiyordu ve oyuncu ne olduğunu anlamıyordu — hatta
            // kapının bozulduğunu sanıyordu. Referansta kapı saydamlaşarak
            // siliniyor; sönme, "bu kapının işi bitti" cümlesinin ta kendisi.
            // Bir kare süren değişim bilgi taşımaz, yalnız şaşırtır.
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeToGhost(ghostMaterial));
        }

        Coroutine _fade;

        /// <summary>
        /// Barı ve oku ghost görünümüne doğru söndürür.
        ///
        /// Renk ARADA geçiyor: hedef materyale hemen geçip yalnız alfayı
        /// indirmek, kapının bir an tam renkli sonra yarı saydam görünmesine
        /// yol açıyordu. Kaynak rengiyle hedef rengi arasında yürümek geçişi
        /// tek bir hareket gibi gösteriyor.
        /// </summary>
        System.Collections.IEnumerator FadeToGhost(Material ghostMaterial)
        {
            const float duration = 0.34f;

            Color from = ReadColor(_renderer.sharedMaterial);
            Color to = ghostMaterial != null ? ReadColor(ghostMaterial) : from;

            // Kendi örneğimizde çalışıyoruz: paylaşılan materyali boyamak
            // aynı renkteki BÜTÜN kapıları söndürürdü.
            var fading = ViewKit.Translucent(from);
            _renderer.sharedMaterial = fading;

            var arrowRenderer = _arrow != null ? _arrow.GetComponent<MeshRenderer>() : null;
            Material arrowFading = null;
            Color arrowFrom = default, arrowTo = default;
            if (arrowRenderer != null)
            {
                arrowFrom = ReadColor(arrowRenderer.sharedMaterial);
                arrowTo = ViewKit.ArrowGhostMaterial != null
                    ? ReadColor(ViewKit.ArrowGhostMaterial) : arrowFrom;
                arrowFading = ViewKit.Translucent(arrowFrom);
                arrowRenderer.sharedMaterial = arrowFading;
            }

            // KAPI TAMAMEN SAYDAMLAŞIYOR (4. tur, G22).
            //
            // Kullanıcı: "Kapı kaybolma efekti — bizde soluklaşıyor;
            // orijinalde tamamen transparan olarak kayboluyor."
            //
            // DERS (yarım kalmış bir geçiş, geçiş değil ARIZA gibi okunur):
            // Kapı ghost rengine solup ORADA KALIYORDU. Ekranda "rengi
            // atmış bir kapı" duruyor ve oyuncu onu hâlâ kullanılabilir
            // sanıyordu. Kapının işi bittiyse ekranda yeri de bitmeli.
            //
            // Gizlemenin eskiden kaçınılan bedeli "duvarda boşluk kalması"ydı;
            // bu doğru değil: `BoardFrameMeshBuilder` çerçeveyi KESİNTİSİZ bir
            // halka olarak örüyor, kapı yalnız onun üstünde duruyor. Kapı
            // saydamlaşınca altından çerçevenin kendisi çıkıyor — referansta
            // da görünen bu.
            to.a = 0f;
            arrowTo.a = 0f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                Paint(fading, Color.Lerp(from, to, k));
                if (arrowFading != null)
                    Paint(arrowFading, Color.Lerp(arrowFrom, arrowTo, k));
                yield return null;
            }

            // Görünmez bir nesneyi çizmeye devam etmenin anlamı yok: renderer
            // kapatılıyor, geçiş için üretilen materyaller yok ediliyor.
            _renderer.enabled = false;
            if (arrowRenderer != null) arrowRenderer.enabled = false;

            if (fading != null) Destroy(fading);
            if (arrowFading != null) Destroy(arrowFading);
            _fade = null;
        }

        static void Paint(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        }

        /// <summary>
        /// Materyalin rengini GÜVENLİ okur.
        ///
        /// DERS (`Material.color` her gölgecide yok): `.color` aslında
        /// `_Color` özelliğini okuyor. Okun materyali `BlockOut/Brick`
        /// gölgecisini kullanıyor ve onda `_Color` YOK — sönmeyi yazdığımda
        /// konsola iki satır hata düştü ("doesn't have a color property
        /// '_Color'"). Unity bunu istisnaya çevirmiyor, sessizce siyah
        /// döndürüyor; yani hata görülmese geçiş siyahtan başlardı.
        /// URP'de doğru ad `_BaseColor`; ikisini de denemek gerekiyor.
        /// </summary>
        static Color ReadColor(Material material)
        {
            if (material == null) return Color.white;
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color")) return material.color;
            return Color.white;
        }
    }
}
