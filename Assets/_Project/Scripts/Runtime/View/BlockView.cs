using System.Collections;
using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;
using PT = PrimeTween;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Bir bloğun sahnedeki görseli. Model → dünya yansıtmasından, tutma/bırakma
    /// hissinden ve emilme animasyonundan sorumludur; OYUN KARARI VERMEZ.
    ///
    /// DERS (his = küçük gecikmeler): Sürükleme sırasında görsel modeli BİREBİR
    /// takip eder — burada yumuşatma yapmak "lastik" hissi verir ve dokunmatik
    /// oyunlarda anında tepkisizlik olarak algılanır. Buna karşılık BIRAKMA
    /// anında kısa bir tween iyi hisdirir, çünkü model zaten yerine oturmuştur;
    /// göz sadece yumuşak bir varış görür.
    /// </summary>
    public sealed class BlockView : MonoBehaviour
    {
        const float SnapDuration = 0.09f;

        // Kalkma yüksekliği ayardan gelir. YÜKSEK bir değer bloğu duvarın
        // üstüne çıkarır ve "duvarın içinden geçiyor" görüntüsü doğurur —
        // bu yüzden varsayılan neredeyse sıfır, his ölçekten geliyor.
        static float DragLift => VisualSettings.Current != null
            ? VisualSettings.Current.dragLift : 0.02f;

        static float DragScale => VisualSettings.Current != null
            ? VisualSettings.Current.dragScale : 1.05f;

        BlockModel _model;
        BoardSpace _space;
        BlockOut.Runtime.Config.ColorPaletteSO _palette;
        MeshRenderer _renderer;
        MeshFilter _filter;
        GameObject _iceShell;
        GameObject _axisArrow;
        TMPro.TextMeshPro _iceCounter;
        Coroutine _tween;
        PT.Sequence _motion;
        bool _highlighted;

        public static BlockView Create(
            Transform parent, BlockModel model, BoardSpace space, Material material,
            BlockOut.Runtime.Config.ColorPaletteSO palette = null)
        {
            var go = new GameObject($"Block_{model.Id}_{model.CurrentColor}");
            go.transform.SetParent(parent, worldPositionStays: false);

            var view = go.AddComponent<BlockView>();
            view._model = model;
            view._space = space;
            view._palette = palette;
            view._filter = go.AddComponent<MeshFilter>();
            view._filter.sharedMesh = BrickMeshBuilder.Get(model);

            view._renderer = go.AddComponent<MeshRenderer>();
            view._renderer.sharedMaterial = material;  // paylaşımlı — SRP Batcher dostu
            view._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view._renderer.receiveShadows = false;

            view.BuildContactShadow();
            view.BuildInnerLayer();
            if (model.Axis != MoveAxis.Free) view.BuildAxisArrow();
            view.SyncFromModel();
            if (model.IsFrozen) view.BuildIceShell(parent);
            return view;
        }

        GameObject _innerPanel, _innerRim;

        /// <summary>
        /// İÇ İÇE BLOK: alttaki katmanın rengi ÜSTTEN görünür (2. tur, 56. madde).
        ///
        /// Kullanıcı: "İç içe 2 blok feature'ın visualı çok kötü. Orijinal
        /// oyundaki gibi olması gerekiyor."
        ///
        /// DERS (görünmeyen kural, olmayan kuraldır): Görsel katman bilgisine
        /// HİÇ bakmıyordu — `CurrentColor` dışındaki katmanlar ekranda yoktu.
        /// Yani iki katmanlı bir blok, tek katmanlıdan ayırt edilemiyordu ve
        /// oyuncu ancak kapıya götürüp soyulunca "aa, altında başka renk
        /// varmış" diyordu. Bulmacada planlanamayan bir kural, kural değil
        /// sürprizdir.
        ///
        /// REFERANS (`Levels.mp4` 08:30, 17. bölüm — sarı gövde, yeşil iç):
        /// Dış renk bir ÇERÇEVE gibi kenarda kalıyor, ortada içteki rengin
        /// gömülü bir paneli duruyor ve ikisinin arasında dış rengin AÇIK
        /// tonunda ince bir kenar çizgisi var. İç panelin kendi kabartmaları
        /// da görünüyor.
        ///
        /// Üç katman kuruluyor: dış gövde (zaten var) → açık kenar → iç panel.
        /// Hepsi bloğun ÇOCUĞU: sürüklenirken birlikte gidiyor ve tutma
        /// ölçeğini paylaşıyor.
        /// </summary>
        void BuildInnerLayer()
        {
            if (_model.Layers.Count <= 1) return;

            // PANEL DIŞ SAPLAMALARIN ÜSTÜNE KALDIRILIYOR.
            //
            // DERS (aynı yükseklikte iki yüzey birbirini deler): Panel önce
            // gövdeyle aynı hizada duruyordu; dış bloğun ORTA saplamaları
            // panelin içinden çıkıp iç rengin üstünde duruyordu (mavi panelin
            // üstünde pembe saplamalar). Paneli saplama tepesinin biraz üstüne
            // almak, o bölgedeki saplamaları temiz biçimde örtüyor.
            //
            // Kaldırma miktarı ÖLÇÜLEN değerden: saplamalı mesh'in tepesi ile
            // saplamasız gövdenin tepesi arasındaki fark. Sabit yazmak, görsel
            // ayarlardan tuğla yüksekliği değişince yanlış kalırdı.
            float studdedTop = _filter != null && _filter.sharedMesh != null
                ? _filter.sharedMesh.bounds.max.y
                : BrickMeshBuilder.Height;
            var silhouette = BrickMeshBuilder.GetSilhouette(_model);
            float bodyTop = silhouette != null ? silhouette.bounds.max.y : studdedTop;
            float lift = Mathf.Max(0.012f, studdedTop - bodyTop + 0.006f);

            // Kenar çizgisi: iç panelden biraz büyük, dış rengin AÇIK tonu.
            // Panelden bir tık AŞAĞIDA ki panelin altından ince bir hat olarak
            // görünsün, onu örtmesin.
            _innerRim = new GameObject("InnerRim");
            _innerRim.transform.SetParent(transform, worldPositionStays: false);
            _innerRim.transform.localPosition = new Vector3(0f, lift - 0.004f, 0f);
            _innerRim.transform.localScale = new Vector3(RimShare, 1f, RimShare);
            _innerRim.AddComponent<MeshFilter>().sharedMesh = silhouette;
            var rimRenderer = _innerRim.AddComponent<MeshRenderer>();
            rimRenderer.sharedMaterial = ViewKit.LayerRim(_palette, _model.CurrentColor);
            rimRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rimRenderer.receiveShadows = false;

            // İç panel: sıradaki katmanın rengi, DÜZ (saplamasız) bir levha.
            //
            // DERS (iki saplama ızgarası üst üste binmez): Panel önce tuğlanın
            // KENDİ (saplamalı) mesh'iyle kuruldu. Küçültülen saplamalar dış
            // gövdenin saplamalarıyla aynı yükseklikte kalıp içlerinden geçti;
            // ekranda her saplamanın üstünde yıldız benzeri kesişme şekilleri
            // belirdi. Referansta iç panel GÖMÜLÜ ve pürüzsüz — bir pencerenin
            // içindeki başka renk gibi. Silüet mesh'i (51. madde için üretildi)
            // tam olarak bunu veriyor.
            _innerPanel = new GameObject("InnerLayer");
            _innerPanel.transform.SetParent(transform, worldPositionStays: false);
            _innerPanel.transform.localPosition = new Vector3(0f, lift, 0f);
            _innerPanel.transform.localScale = new Vector3(PanelShare, 1f, PanelShare);
            _innerPanel.AddComponent<MeshFilter>().sharedMesh = silhouette;
            var panelRenderer = _innerPanel.AddComponent<MeshRenderer>();
            panelRenderer.sharedMaterial = ViewKit.LayerFill(_palette, _model.Layers[1]);
            panelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            panelRenderer.receiveShadows = false;
        }

        /// <summary>
        /// Kenar çizgisinin ve iç panelin bloğa oranı — referanstan ölçüldü:
        /// sarı çerçeve her kenarda bloğun ~%20'si, yani iç panel ~%60.
        /// </summary>
        const float RimShare = 0.66f;
        const float PanelShare = 0.58f;

        /// <summary>
        /// Katman soyulunca iç panel de yenilenir: yeni dış renk artık eski
        /// içtekidir, ve altında BAŞKA bir katman varsa onu göstermek gerekir.
        ///
        /// DERS (durum değişince GÖRSELİ de güncelle): İlk kurulumda bu metot
        /// yoktu; soyulan blok yeni rengini alıyor ama iç paneli ESKİ rengiyle
        /// ekranda kalıyordu — üç katmanlı bir blokta ikinci soyulmadan sonra
        /// panel yalan söylemeye başlıyordu.
        /// </summary>
        void RefreshInnerLayer()
        {
            if (_model.Layers.Count > 1)
            {
                if (_innerPanel == null) { BuildInnerLayer(); return; }

                _innerPanel.SetActive(true);
                _innerRim.SetActive(true);
                _innerPanel.GetComponent<MeshRenderer>().sharedMaterial =
                    ViewKit.LayerFill(_palette, _model.Layers[1]);
                _innerRim.GetComponent<MeshRenderer>().sharedMaterial =
                    ViewKit.LayerRim(_palette, _model.CurrentColor);
                return;
            }

            if (_innerPanel != null) _innerPanel.SetActive(false);
            if (_innerRim != null) _innerRim.SetActive(false);
        }

        /// <summary>
        /// Yönlü blokların üstündeki çift başlı ok.
        ///
        /// DERS (kuralı GÖRÜNÜR kılmak): Eksen kısıtı görünmezse oyuncu bloğu
        /// çekmeye çalışır, olmaz, oyunu bozuk sanır. Bulmacada her kısıt
        /// ekranda okunabilir olmalı — referans oyun da bu yüzden oku bloğun
        /// tam ortasına, iri ve kabartmalı basıyor.
        ///
        /// DERS (ok BİR PARÇA değil, İKİ KATMANDIR — 4. tur H31): Ok eskiden
        /// düz BEYAZ bir levhaydı ve blokla hiçbir ilişkisi yoktu; ekranda
        /// "üstüne yapıştırılmış çıkartma" gibi duruyordu. Referansta
        /// (41-50 yürüyüşü, 50. bölüm) ok BLOĞUN KENDİ RENGİNDE ve üç şeyle
        /// okunuyor: altında koyu bir oluk hattı, üstünde açık tonlu alçak bir
        /// prizma, ve altındaki gövdenin SAPLAMASIZ olması (bkz.
        /// <see cref="BrickMeshBuilder.Get"/>). Rengi kimliğin kendisi olan bir
        /// oyunda beyaz bir ok, bilginin üstünü çiziyordu.
        ///
        /// Ok bloğun ÇOCUĞU: blok sürüklenirken onunla birlikte gitsin ve
        /// tutma animasyonundaki ölçeği paylaşsın.
        /// </summary>
        void BuildAxisArrow()
        {
            var go = new GameObject("AxisArrow");
            go.transform.SetParent(transform, worldPositionStays: false);
            _axisArrow = go;

            bool horizontal = _model.Axis == MoveAxis.Horizontal;
            var center = ArrowAnchor();

            // Ölçüler referans karesinden (50. bölüm, mavi 1×3 blok):
            // gövde kalınlığı kısa kenarın ~%26'sı, baş genişliği ~%62'si,
            // baş uzunluğu ~%42'si; ok, uzun kenarın neredeyse tamamını kaplıyor.
            //
            // ÜST SINIR ŞART: ok kısa kenarla orantılı büyüyor ama 2×2 bir
            // blokta orantı korunursa ok tahtanın en iri nesnesi hâline
            // geliyor ve blok "ok tutan bir levha" gibi okunuyor. Referansta
            // iri bloklardaki ok yalnız BİR TIK büyük.
            float span = Mathf.Min(Mathf.Min(_model.W, _model.H), 1.5f);
            float axisExtent = horizontal ? _model.W : _model.H;
            float half = Mathf.Max(0.16f, axisExtent * 0.5f - 0.22f);
            float headWidth = span * 0.26f;
            float thickness = span * 0.105f;
            float headLength = Mathf.Min(span * 0.33f, half * 0.5f);

            float top = _filter != null && _filter.sharedMesh != null
                ? _filter.sharedMesh.bounds.max.y
                : BrickMeshBuilder.Height;

            var polygon = ArrowPolygon(half, thickness, headWidth, headLength);
            if (!horizontal) RotateQuarter(polygon);

            // OLUK: okun bir tık büyütülmüş KOYU kopyası. Referansta okun
            // çevresinde koyu bir hat var; onsuz açık tonlu ok gövdenin
            // üstünde yüzüyormuş gibi duruyor.
            var groove = Grow(polygon, span * 0.032f);
            var grooveGo = new GameObject("AxisArrowGroove");
            grooveGo.transform.SetParent(go.transform, worldPositionStays: false);
            grooveGo.transform.localPosition = new Vector3(0f, 0.003f, 0f);
            grooveGo.AddComponent<MeshFilter>().sharedMesh = FlatShape(groove, "AxisArrowGroove");
            var grooveRenderer = grooveGo.AddComponent<MeshRenderer>();
            grooveRenderer.sharedMaterial = ViewKit.AxisArrowGroove(_palette, _model.CurrentColor);
            grooveRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            grooveRenderer.receiveShadows = false;

            // KABARTMA: alçak bir prizma. Düz bir levha tepeden bakan kamerada
            // hiçbir kenar üretmez; yan duvarı olan prizma, ışığı farklı açıyla
            // alan ince bir şerit verir ve ok "basılmış" görünür.
            go.AddComponent<MeshFilter>().sharedMesh =
                Prism(polygon, span * 0.05f, "AxisArrow");

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ViewKit.AxisArrowFace(_palette, _model.CurrentColor);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            go.transform.localPosition = new Vector3(center.x, top + 0.005f, center.y);
        }

        /// <summary>
        /// Çift başlı okun çevre çizgisi (yatay, ±X yönünde). Saat yönünün
        /// TERSİNE sıralı.
        /// </summary>
        static List<Vector2> ArrowPolygon(
            float half, float thickness, float headWidth, float headLength)
        {
            float shoulder = half - headLength;
            return new List<Vector2>
            {
                new Vector2(half, 0f),
                new Vector2(shoulder, headWidth),
                new Vector2(shoulder, thickness),
                new Vector2(-shoulder, thickness),
                new Vector2(-shoulder, headWidth),
                new Vector2(-half, 0f),
                new Vector2(-shoulder, -headWidth),
                new Vector2(-shoulder, -thickness),
                new Vector2(shoulder, -thickness),
                new Vector2(shoulder, -headWidth)
            };
        }

        /// <summary>
        /// Yatay oku dikeye çevirir.
        ///
        /// DERS (eksen TAKASI el yönünü tersine çevirir): Eski kod dikey oku
        /// (x,z) → (z,-x) ile üretiyordu; bu gerçek bir dönüş olduğu için
        /// doğruydu. (x,z) → (z,x) gibi bir TAKAS ise aynayı katardı ve
        /// çokgenin sarım yönü tersine dönüp ok görünmez olurdu.
        /// </summary>
        static void RotateQuarter(List<Vector2> polygon)
        {
            for (int i = 0; i < polygon.Count; i++)
                polygon[i] = new Vector2(-polygon[i].y, polygon[i].x);
        }

        /// <summary>
        /// Çokgeni dışa doğru gönye (miter) ile şişirir.
        ///
        /// DERS (köşede iki normalin ORTALAMASI yetmez): Ortalama almak dar
        /// açılı köşelerde kaydırmayı istenen kalınlığın altına düşürür ve okun
        /// sivri ucu körelir. Gönye uzunluğu 1/cos(yarımaçı) ile büyür; burada
        /// o, birim toplamın uzunluğuna bölmek olarak yazılı.
        /// </summary>
        static List<Vector2> Grow(List<Vector2> polygon, float amount)
        {
            int count = polygon.Count;
            var result = new List<Vector2>(count);
            for (int i = 0; i < count; i++)
            {
                var prev = polygon[(i - 1 + count) % count];
                var cur = polygon[i];
                var nxt = polygon[(i + 1) % count];
                var sum = Outward(cur - prev) + Outward(nxt - cur);
                float length = sum.magnitude;
                // Çok sivri köşede gönye sonsuza gider; 3 kat ile sınırlanıyor.
                float miter = length > 1e-4f ? Mathf.Min(2f / length, 1.8f) : 0f;
                result.Add(cur + sum.normalized * amount * miter);
            }
            return result;
        }

        /// <summary>Saat yönünün tersine halkada dış normal SAĞDADIR.</summary>
        static Vector2 Outward(Vector2 direction)
        {
            direction = direction.normalized;
            return new Vector2(direction.y, -direction.x);
        }

        /// <summary>Yatay, tek yüzlü levha.</summary>
        static Mesh FlatShape(List<Vector2> polygon, string name)
        {
            var verts = new List<Vector3>(polygon.Count);
            var normals = new List<Vector3>(polygon.Count);
            var tris = new List<int>();
            foreach (var p in polygon)
            {
                verts.Add(new Vector3(p.x, 0f, p.y));
                normals.Add(Vector3.up);
            }
            BrickSilhouette.Triangulate(polygon, tris, 0, faceUp: true);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Alçak prizma: yan duvar + üst kapak.</summary>
        static Mesh Prism(List<Vector2> polygon, float height, string name)
        {
            int count = polygon.Count;
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            for (int i = 0; i < count; i++)
            {
                var prev = polygon[(i - 1 + count) % count];
                var nxt = polygon[(i + 1) % count];
                var tangent = (nxt - prev).normalized;
                var n = new Vector3(tangent.y, 0.35f, -tangent.x).normalized;

                var p = polygon[i];
                verts.Add(new Vector3(p.x, 0f, p.y));
                verts.Add(new Vector3(p.x, height, p.y));
                normals.Add(n); normals.Add(n);
            }
            for (int i = 0; i < count; i++)
            {
                int a = i * 2;
                int b = ((i + 1) % count) * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(b);
                tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
            }

            int capStart = verts.Count;
            foreach (var p in polygon)
            {
                verts.Add(new Vector3(p.x, height, p.y));
                normals.Add(Vector3.up);
            }
            BrickSilhouette.Triangulate(polygon, tris, capStart, faceUp: true);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Okun oturacağı yer: hücrelerin AĞIRLIK MERKEZİ. Dikdörtgende bu tam
        /// olarak bloğun ortasıdır; L gibi şekillerde ise gövdenin dolu tarafına
        /// kayar. Ağırlık merkezi boş bir hücreye düşerse (U, artı gibi kimi
        /// şekiller) en yakın DOLU hücrenin ortasına çekilir — ok asla boşlukta
        /// asılı kalmaz.
        ///
        /// Mesh yerel uzayı: x = -W/2 + cx + 0.5, z = +H/2 - cy - 0.5
        /// (hücre uzayında y aşağı artar, dünyada Z yukarı).
        /// </summary>
        Vector2 ArrowAnchor()
        {
            var cells = _model.Cells;
            if (cells.Count == 0) return Vector2.zero;

            float cx = 0f, cy = 0f;
            foreach (var cell in cells) { cx += cell.x + 0.5f; cy += cell.y + 0.5f; }
            cx /= cells.Count;
            cy /= cells.Count;

            if (!_model.Cells.Contains(new Vector2Int(Mathf.FloorToInt(cx), Mathf.FloorToInt(cy))))
            {
                var best = cells[0];
                float bestDistance = float.MaxValue;
                foreach (var cell in cells)
                {
                    float dx = cell.x + 0.5f - cx, dy = cell.y + 0.5f - cy;
                    float distance = dx * dx + dy * dy;
                    if (distance >= bestDistance) continue;
                    bestDistance = distance;
                    best = cell;
                }
                cx = best.x + 0.5f;
                cy = best.y + 0.5f;
            }

            return new Vector2(-_model.W * 0.5f + cx, _model.H * 0.5f - cy);
        }

        /// <summary>Modelin hücre konumunu dünyaya yansıtır. Sürükleme sırasında her kare çağrılır.</summary>
        public void SyncFromModel()
        {
            transform.position = WorldPosition(_highlighted ? DragLift : 0f);
        }

        Vector3 WorldPosition(float lift) =>
            _space.RectCenterToWorld(_model.Position, _model.W, _model.H, lift);

        /// <summary>
        /// Tutulan bloğun BEYAZ konturu (2. tur, 51. madde).
        ///
        /// Kullanıcı: "Orijinal oyunda hangi bloku tutuyorsak onun etrafında
        /// beyaz bir outline oluyor."
        ///
        /// DERS (tutma geri bildirimi ÖLÇEKTEN ibaret değil): Burada tek geri
        /// bildirim bloğu %5 büyütmekti. Tek başına bu, kalabalık bir tahtada
        /// hangi bloğun elde olduğunu söylemiyor — özellikle aynı renkten
        /// birkaç blok yan yanayken. Kontur o soruyu tek bakışta cevaplıyor.
        ///
        /// Kabuk İLK İSTENDİĞİNDE kuruluyor: tahtada 20 blok var ve
        /// çoğu hiç tutulmayacak; her birine baştan ikinci bir mesh vermek
        /// bedava değil.
        /// </summary>
        void EnsureOutline()
        {
            if (_outline != null) return;

            _outline = new GameObject("Outline");
            _outline.transform.SetParent(transform, worldPositionStays: false);
            _outline.transform.localPosition = Vector3.zero;
            _outline.transform.localRotation = Quaternion.identity;

            // KALINLIK ÖLÇEKTEN GELMİYOR (4. tur, H29).
            //
            // DERS (oran ile kalınlık aynı şey değildir): Burada eskiden
            // `localScale = 1.04` vardı. Ölçek merkezden çalışır, yani taşma
            // bloğun BOYUYLA orantılıdır: 2 hücrelik blokta 0,04 hücre,
            // 6 hücrelik blokta 0,12 hücre. Bloklar birbirine değdiği için
            // büyük bloklarda beyaz kontur komşunun ÜSTÜNE biniyordu.
            // Kabuk artık silüetin dışa doğru SABİT kaydırılmış hâli; ölçek
            // birde kalıyor.
            _outline.transform.localScale = Vector3.one;

            // SAPLAMASIZ mesh: kabuk yalnız silüeti çizmeli. Tuğlanın kendi
            // mesh'i kullanılınca her saplama da büyüyüp kendi konturunu
            // üretiyor ve blok "beyaz benekli" görünüyordu (denendi, ölçüldü).
            _outline.AddComponent<MeshFilter>().sharedMesh =
                BrickMeshBuilder.GetOutlineShell(_model);

            var renderer = _outline.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ViewKit.Outline;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        GameObject _outline;

        public void SetHighlight(bool on)
        {
            if (_highlighted == on) return;
            _highlighted = on;

            if (on) EnsureOutline();
            if (_outline != null) _outline.SetActive(on);

            StopTween();
            if (on)
            {
                transform.localScale = Vector3.one * DragScale;
                SyncFromModel();
            }
            else
            {
                // Bırakma: model zaten hücreye oturdu, görsel oraya yumuşak varır.
                SnapToCell();
            }
        }

        /// <summary>
        /// Bırakma anında hücreye oturma.
        ///
        /// DERS (konum ve ölçek AYNI eğriyi paylaşmamalı): Eskiden ikisi de tek
        /// bir ease-out ile gidiyordu ve varış "yumuşak ama ölü" duruyordu.
        /// Konumun sertçe varması (OutQuad) "kilitlendi" der; ölçeğin hedefi
        /// hafifçe aşıp dönmesi (overshoot) o kilitlenmeye tokat sesi ekler.
        /// İkisi aynı anda çalışıyor, bu yüzden Group.
        /// </summary>
        void SnapToCell()
        {
            Vector3 toPos = WorldPosition(0f);

            _motion = PT.Sequence.Create()
                .Group(PT.Tween.Position(transform, toPos, SnapDuration, PT.Ease.OutQuad))
                .Group(PT.Tween.Scale(transform, Vector3.one, SnapDuration * 2.4f,
                    PT.Easing.Overshoot(1.8f)));
        }

        void StopTween()
        {
            if (_tween != null) StopCoroutine(_tween);
            _tween = null;

            if (_motion.isAlive) _motion.Stop();
        }

        /// <summary>
        /// Bloğun altına yumuşak temas gölgesi. Bloğun ÇOCUĞU olduğu için
        /// blokla birlikte hareket eder; ölçek animasyonlarında da doğal
        /// biçimde büzülür.
        /// </summary>
        void BuildContactShadow()
        {
            var cfg = VisualSettings.Current;
            if (cfg != null && !cfg.contactShadow) return;

            float scale = cfg != null ? cfg.shadowScale : 1.02f;
            float opacity = cfg != null ? cfg.shadowOpacity : 0.42f;
            Vector2 offset = cfg != null ? cfg.shadowOffset : new Vector2(0.06f, -0.06f);

            var quad = new GameObject("Shadow");
            quad.transform.SetParent(transform, worldPositionStays: false);
            // Gölge de bloğun ŞEKLİNİ izler: L bloğun altında dikdörtgen gölge
            // olmaz. Hücre başına quad, tek mesh'te.
            quad.AddComponent<MeshFilter>().sharedMesh = BuildShadowMesh();

            var renderer = quad.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ViewKit.ShadowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Gölge, zeminle blok arasında kalmalı: zeminden hemen sonra çizilir.
            renderer.sortingOrder = -1;

            quad.transform.localPosition = new Vector3(offset.x, 0.012f, offset.y);
            quad.transform.localScale = new Vector3(scale, 1f, scale);

            var color = renderer.sharedMaterial.color;
            color.a = opacity;
            // Paylaşımlı materyalin alfası tek yerden gelir; blok başına
            // farklı opaklık gerekmediği için materyali kopyalamıyoruz.
            renderer.sharedMaterial.color = color;
        }

        /// <summary>
        /// Bloğun SİLÜETİNİ kaplayan yatay gölge mesh'i (yerel uzayda).
        ///
        /// DERS (gölge, gövdeyle aynı silüeti taşımalı): Gölge eskiden hücre
        /// hücre quad'lardan kuruluyor ve her quad yumuşak kenarlı bir dokuyu
        /// 0-1 UV ile geriyordu. Çok hücreli bir blokta bu, HER HÜCRENİN
        /// altında ayrı bir leke demekti: bloğun içinde açık şeritler beliriyor
        /// ve gölge blok değil "birbirine yapışmış karolar" gibi okunuyordu.
        /// Ayrıca köşeler keskin kalıyor, yuvarlak köşeli gövdenin altından
        /// sivri uçlar taşıyordu.
        ///
        /// Şimdi gölge de <see cref="BrickSilhouette"/>'ten geliyor ve dışa
        /// doğru bir tık şişiriliyor — referansta blokların çevresinde görülen
        /// koyu ince hat (4. tur H27/H28) tam olarak budur.
        /// </summary>
        Mesh BuildShadowMesh()
        {
            var cfg = VisualSettings.Current;
            float inset = cfg != null ? cfg.brickInset : 0.055f;
            float corner = cfg != null ? cfg.brickCornerRadius : 0.16f;

            // Gölge gövdeden BİRAZ TAŞAR: iki yan yana blokta iki koyu hat
            // birleşip tek, net bir ayrım çizgisi oluşturuyor.
            const float spread = 0.035f;
            var loop = BrickSilhouette.Build(_model.Cells, _model.W, _model.H,
                inset - spread, corner + spread);

            var mesh = new Mesh { name = "BlockShadow" };
            if (loop == null || loop.Count < 3) return mesh;
            BrickSilhouette.MakeCounterClockwise(loop);

            var verts = new List<Vector3>(loop.Count);
            var uvs = new List<Vector2>(loop.Count);
            var normals = new List<Vector3>(loop.Count);
            var tris = new List<int>();

            foreach (var p in loop)
            {
                verts.Add(new Vector3(p.x, 0f, p.y));
                // Doku artık yalnız düz bir ton veriyor: yumuşaklık silüetin
                // kendisinden geliyor, UV geriminden değil.
                uvs.Add(new Vector2(0.5f, 0.5f));
                normals.Add(Vector3.up);
            }
            BrickSilhouette.Triangulate(loop, tris, 0, faceUp: true);

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Buz kabuğu + sayaç. Bloğun ÇOCUĞU değil kardeşi: donmuş blok zaten
        /// hareket etmez, dünya konumuna sabitlemek güvenli ve ölçekten etkilenmez.
        /// </summary>
        void BuildIceShell(Transform parent)
        {
            // DERS (buz TUĞLA ŞEKLİNDE donar): Kabuk düz bir küptü. Referansta
            // buz, altındaki tuğlanın silüetini alıyor — yuvarlatılmış kenarlar,
            // aynı kabartma. Düz bir küp o yüzden "tahtaya yapıştırılmış mavi
            // levha" gibi duruyordu. Aynı mesh'i kullanmak hem doğru silueti
            // hem de bedava kenar yumuşatmasını veriyor.
            _iceShell = new GameObject($"Ice_{_model.Id}");
            _iceShell.transform.SetParent(parent, worldPositionStays: false);
            // BUZ DÜZ BİR LEVHA — SAPLAMASIZ.
            //
            // DERS (donmuş yüzey ALTTAKİNİN dokusunu taşımaz): Kabuk tuğlanın
            // kendi mesh'ini kullanıyordu ve buzun üstünde de kabartmalar
            // çıkıyordu. Referansta (13. bölüm karesi) buz bloğu **düz ve
            // parlak bir levha**: silüet tuğlanın ama yüzey pürüzsüz. Kabartma
            // kalınca buz "mavi boyanmış tuğla" gibi okunuyor, donmuş bir kalıp
            // gibi değil.
            //
            // Aynı silüet mesh'i 51. maddedeki kontur kabuğu için de üretilmişti;
            // ikisi de "gövdenin şekli, ayrıntısı olmadan" istiyor.
            _iceShell.AddComponent<MeshFilter>().sharedMesh =
                BrickMeshBuilder.GetSilhouette(_model);

            var renderer = _iceShell.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ViewKit.Ice;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // HATA (bulundu: buz bloğun ALTINDA kalıyordu): kabuk yüksekliği
            // `BrickMeshBuilder.Height` ile hesaplanıyordu, ama tuğlanın GERÇEK
            // tepesi kabartmalar (stud) yüzünden daha yukarıda. Kabuk bloğun
            // üstünü yalnızca ~0.04 birim aşıyordu; kamera bu oyunda çok geride
            // durduğu için derinlik tamponunun hassasiyeti o farkı çözemiyor ve
            // buz pikselleri bloğun ARKASINDA sayılıp eleniyordu.
            //
            // DERS (sabit yerine ölçülen değer): Mesh'in kendi sınırlayıcı
            // kutusunu sormak, ayarlar değiştiğinde de doğru kalır. Görsel ayar
            // penceresinden tuğla yüksekliği değiştirilince buz da uyar.
            float brickTop = _filter != null && _filter.sharedMesh != null
                ? _filter.sharedMesh.bounds.max.y
                : BrickMeshBuilder.Height;

            // Buz kalıbı tuğlanın yerine geçer: aynı ayak izi, aynı yükseklik.
            // (Eski `SideInset` sabiti kaldırıldı: kabuk artık tuğla mesh'inin
            // kendisi olduğu için ayrı bir kenar payına gerek yok, kullanılmayan
            // değişken uyarısı basıyordu.)
            float shellHeight = brickTop;

            // Mesh tuğlanın kendi yerel uzayında; kabuk bloğun konumuna oturur
            // ve bir tık büyütülür ki tuğlayı tamamen örtsün (z-fighting olmasın).
            Vector3 center = _space.RectCenterToWorld(
                _model.Position, _model.W, _model.H, 0f);
            _iceShell.transform.position = center;
            // YATAYDA İÇERİ ÇEKİLİYOR — komşu buz kalıpları arasında GERÇEK
            // bir boşluk kalsın.
            //
            // DERS (aynı rengi yan yana koyarsan tek nesne olur): Kabuk
            // referanstaki gibi düz levhaya çevrilince (saplama yok) 10.
            // bölümdeki dokuz buzlu blok ekranda TEK bir dev camgöbeği leke
            // olarak çıktı. Sırayla denendi ve hiçbiri tek başına yetmedi:
            //   • koyu kenar kabuğu (`IceRim`) — kıl gibi ince kaldı,
            //   • `BlockOut/Brick` shader'ı (köşe renkleri) — bitişik ÜST
            //     yüzlerin tonu aynı olduğu için sınır yine doğmadı.
            // Sorun gölgede değil GEOMETRİDE: iki levha fiziksel olarak
            // bitişikse aralarında gösterilecek bir şey yok. %6 içeri çekmek
            // aradan tahtanın koyu zeminini geçiriyor ve sınır kendiliğinden
            // doğuyor — referansta da her buz kalıbının hücresi içinde payı var.
            //
            // Dikeydeki 1.05 z-fighting içindi, aynen kalıyor.
            _iceShell.transform.localScale = new Vector3(0.94f, 1.05f, 0.94f);

            // KOYU KENAR: komşu buz kalıpları birbirinden ayrışsın.
            // Düz levha yapılınca (referansta saplama yok) yan yana iki buz
            // arasında hiçbir sınır kalmıyordu — 10. bölümde dokuz buz bloğu
            // ekranda tek bir dev camgöbeği leke olarak çıkıyordu.
            // Kabuk tekniği (bkz. ViewKit.IceRim): silüet biraz büyütülüp ön
            // yüzleri kırpılıyor, geriye kalan arka yüzler kenarda çerçeve
            // bırakıyor.
            var rim = new GameObject("IceRim");
            rim.transform.SetParent(_iceShell.transform, worldPositionStays: false);
            rim.transform.localPosition = Vector3.zero;
            rim.transform.localScale = Vector3.one * 1.03f;
            rim.AddComponent<MeshFilter>().sharedMesh =
                BrickMeshBuilder.GetSilhouette(_model);
            var rimRenderer = rim.AddComponent<MeshRenderer>();
            rimRenderer.sharedMaterial = ViewKit.IceRim;
            rimRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rimRenderer.receiveShadows = false;

            // KIRAĞI PANELİ: kalıbın üstünde daha AÇIK, içeri çekilmiş bir alan
            // (4. tur, I35 — "daha çok buza benzemeli").
            //
            // DERS (tek renkli bir kalıp buz değil, boyanmış plastiktir):
            // Kabuk tek düz camgöbeğiydi ve tepeden bakan kamerada hiçbir iç
            // yapı üretmiyordu. Referansta (10. bölüm) her kalıbın ortasında
            // neredeyse beyaz bir alan var — donmuş suyun içindeki hava. İki
            // ton arasındaki fark, malzemeyi saydam gösteren şeyin ta kendisi.
            // Panel gövdenin ÇOCUĞU değil kardeşi değil: kabuğun çocuğu, yani
            // kabuğun %6 içeri çekilmesini de paylaşıyor.
            var frost = new GameObject("IceFrost");
            frost.transform.SetParent(_iceShell.transform, worldPositionStays: false);
            frost.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            frost.transform.localScale = new Vector3(0.72f, 1f, 0.72f);
            frost.AddComponent<MeshFilter>().sharedMesh =
                BrickMeshBuilder.GetSilhouette(_model);
            var frostRenderer = frost.AddComponent<MeshRenderer>();
            frostRenderer.sharedMaterial = ViewKit.IceFrost;
            frostRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            frostRenderer.receiveShadows = false;

            // Buz OPAK olduğu için tuğlayı çizmeye gerek yok: hem referanstaki
            // gibi renk gizleniyor hem de bir çizim çağrısı tasarruf ediyoruz.
            if (_renderer != null) _renderer.enabled = false;
            // Ok da buzun içinde kalmalı; kabuğun tepesinden dışarı taşmasın.
            if (_axisArrow != null) _axisArrow.SetActive(false);

            BuildFrostShards(center, shellHeight);

            // Buz BLOĞUNUN sayacı camgöbeği: referansta rakam kendi zemininin
            // açık tonu, kapının kremi değil (bkz. ViewKit.CounterStyle).
            //
            // YÜKSEKLİK ÖLÇÜLEREK BULUNUYOR, HESAPLANARAK DEĞİL.
            //
            // DERS (tahmini yükseklik sessizce gömülür): Sayaç önce
            // `shellHeight * 0.5f + 0.06f` ile konumlanıyordu — kabuğun yarısı
            // artı biraz. Ama kabuk mesh'i tuğlanın silueti ve dikeyde 1.05
            // ölçekli; gerçek tepesi bu hesaptan yukarıda kaldı ve rakam buzun
            // İÇİNDE doğdu. Ekranda hiçbir şey yoktu: nesne vardı, materyali
            // doğruydu, çizicisi açıktı, hatta `isVisible` bile true'ydu —
            // yalnızca derinlik testini geçemiyordu. Çizicinin KENDİ sınırını
            // sormak, mesh ya da ölçek değişse de doğru kalan tek yol.
            float iceTop = rimRenderer.bounds.max.y;
            _iceCounter = ViewKit.CreateCounter(
                parent,
                new Vector3(center.x, iceTop + 0.08f, center.z),
                _model.IceCount,
                ViewKit.CounterStyle.Ice);
        }

        /// <summary>
        /// Buz kabuğu artık tuğla mesh'inin kendisi olduğu için ek süse gerek
        /// yok. Buraya önce kristal parçalar, sonra bir parlaklık şeridi
        /// konmuştu; ikisi de referanstan UZAKLAŞTIRDI. Kristaller oyunun
        /// diline yabancıydı, şerit ise buzun üstüne yapıştırılmış bir bant
        /// gibi duruyordu. Doğru silueti mesh, parlaklığı materyal veriyor.
        /// </summary>
        void BuildFrostShards(Vector3 center, float shellHeight) { }

        readonly List<GameObject> _frost = new List<GameObject>();

        /// <summary>Kristal parçayı yukarı fırlatır, döndürür, söndürür.</summary>
        static IEnumerator FlingShard(Transform shard)
        {
            if (shard == null) yield break;

            Vector3 start = shard.position;
            var velocity = new Vector3(
                Random.Range(-2.4f, 2.4f), Random.Range(3.2f, 5.4f), Random.Range(-2.4f, 2.4f));
            var spin = new Vector3(
                Random.Range(-540f, 540f), Random.Range(-540f, 540f), Random.Range(-540f, 540f));
            Vector3 baseScale = shard.localScale;

            const float life = 0.65f;
            for (float t = 0f; t < life; t += Time.deltaTime)
            {
                if (shard == null) yield break;
                velocity.y -= 14f * Time.deltaTime;              // yerçekimi
                shard.position += velocity * Time.deltaTime;
                shard.Rotate(spin * Time.deltaTime, Space.World);
                // Son üçte birde küçülerek yok olur; birden kaybolmak göze çarpar.
                float k = Mathf.Clamp01((t - life * 0.6f) / (life * 0.4f));
                shard.localScale = baseScale * (1f - k);
                yield return null;
            }

            if (shard != null) Destroy(shard.gameObject);
        }

        public void UpdateIceCount()
        {
            if (_iceCounter != null) _iceCounter.text = _model.IceCount.ToString();
        }

        /// <summary>Buz kırıldı: kabuk ve sayaç gider, blok serbest kalır.</summary>
        /// <summary>
        /// Buz kırılır: kabuk anında yok olur, kristal parçalar SAVRULUR.
        ///
        /// DERS (yok olmak ile kırılmak farklı okunur): Kabuk ve parçalar
        /// birlikte silinince olay "blok renk değiştirdi" gibi görünüyordu.
        /// Parçaları yerinde bırakıp fırlatmak, aynı anda hem neyin kırıldığını
        /// hem de kırılmanın SERT olduğunu anlatıyor. Parçacık sistemi zaten
        /// kırıntı saçıyor; bunlar onun büyük kardeşleri, siluet taşıyorlar.
        /// </summary>
        public void ShatterIce()
        {
            if (_iceShell != null) Destroy(_iceShell);
            if (_iceCounter != null) Destroy(_iceCounter.gameObject);

            foreach (var shard in _frost)
                if (shard != null)
                    GameKit.FX.Juice.Run(FlingShard(shard.transform));
            _frost.Clear();

            _iceShell = null;
            _iceCounter = null;

            // Gizlenen renk ortaya çıkar — video kuralı.
            if (_renderer != null) _renderer.enabled = true;
            if (_axisArrow != null) _axisArrow.SetActive(true);
        }

        /// <summary>Katman soyulunca dış rengin materyali değişir.</summary>
        /// <summary>
        /// Katman soyulunca dış gövdenin materyali yenilenir — VE iç panel de.
        ///
        /// DERS (bir olayın görsel sonucu tek yerde bitmez): `GateSystem.Peel`
        /// yalnız bu metodu çağırıyordu ve o da yalnız dış rengi değiştiriyordu.
        /// İç panel eski rengiyle ekranda kalıyor, üç katmanlı bir blokta
        /// ikinci soyulmadan sonra oyuncuya yanlış renk gösteriyordu.
        /// </summary>
        public void SetLayerMaterial(Material material)
        {
            _renderer.sharedMaterial = material;
            RefreshInnerLayer();
        }

        /// <summary>
        /// "Bu blok kımıldamıyor" tepkisi: kısa, yatay bir titreme.
        ///
        /// DERS (reddedilen girdi de CEVAP ister): Donmuş bir bloğa dokunulduğunda
        /// hiçbir şey olmuyordu. Oyuncu bunu "oyun beni duymadı" diye okur ve
        /// aynı yere üst üste basar. Küçük bir titreme "duydum ama olmaz" der;
        /// buzun neden orada olduğunu da göze gösterir. Sessiz reddetme,
        /// mobil oyunlarda en sık rastlanan hayal kırıklığı kaynağıdır.
        /// </summary>
        /// DERS (reddetme SERT başlamalı): Sönümlenen bir titremenin gücü ilk
        /// salınımdadır; sonrası yalnızca "duruyor" der. enableFalloff tam olarak
        /// bunu yapıyor — ilk darbe en güçlü, arkası hızla sönüyor. Yalnız X
        /// ekseninde sallıyoruz: dikey titreme bloğun düşmek üzere olduğunu ima
        /// eder, oysa anlatmak istediğimiz "buraya sıkışmış".
        public void PlayRefusal()
        {
            if (_refusing) return;
            _refusing = true;

            PT.Tween.ShakeLocalPosition(transform,
                    strength: new Vector3(0.14f, 0f, 0f), duration: 0.28f,
                    frequency: 13f, enableFalloff: true)
                .OnComplete(this, view => view._refusing = false);
        }

        bool _refusing;

        /// <summary>
        /// Tahta girişinde blokların sırayla yerine oturması.
        ///
        /// DERS (düşen şey HIZLANIR): Eskiden iniş ease-out cubic ile, yani
        /// yavaşlayarak yapılıyordu. Bu "indirildi" der, "düştü" demez —
        /// yerçekimi altındaki bir cisim varışa doğru hızlanır. InQuad bunu
        /// verir; ardından gelen ezilme (squash) çarpmanın ağırlığını anlatır.
        ///
        /// DERS (ezilme tek başına yetmez, GERİ DÖNÜŞ de gerekir): Yalnız ezip
        /// bırakmak lastik gibi durur. Ezilmeden sonra hedefi aşarak toparlanmak
        /// cismin katı ama canlı olduğunu söyler — çizgi filmdeki squash & stretch
        /// tam olarak budur.
        /// </summary>
        public void PlayIntro(float delay, float duration)
        {
            StopTween();

            Vector3 target = WorldPosition(0f);
            Vector3 start = target + Vector3.up * 2.2f; // fazla yüksek düşüş tahtanın dışına taşıyor
            transform.position = start;
            transform.localScale = Vector3.one * 0.6f;

            _motion = PT.Sequence.Create()
                .Group(PT.Tween.Position(transform, start, target, duration,
                    PT.Ease.InQuad, startDelay: delay))
                .Group(PT.Tween.Scale(transform, Vector3.one * 0.6f, Vector3.one, duration,
                    PT.Ease.OutQuad, startDelay: delay))
                .Chain(PT.Tween.ScaleY(transform, 0.80f, 0.06f, PT.Ease.OutQuad))
                .Chain(PT.Tween.ScaleY(transform, 1f, 0.20f, PT.Easing.Overshoot(2.0f)));
        }

        /// <summary>
        /// Kapıdan emilme: blok kapı çizgisine doğru ilerlerken hareket ekseninde
        /// hızla incelir, hafifçe aşağı çöker ve yuvaya girmiş gibi kaybolur.
        ///
        /// <paramref name="travel"/> = bloğun MERKEZİNİN kapı çizgisine olan
        /// mesafesi. Sabit bir mesafe kullanmak (eski hali) bloğun duvarın
        /// ÜSTÜNDEN geçmesine yol açıyordu; artık tam yuvada duruyor.
        /// </summary>
        /// <summary>
        /// Yardımcıyla (roket/UFO) silinme: kapıya gitmediği için yön yok —
        /// yerinde küçülüp kaybolur. Emilme animasyonundan AYRI tutuluyor
        /// çünkü ikisi farklı şey anlatır: biri "kapıdan çıktı", öbürü "yok
        /// edildi".
        /// </summary>
        public void PlayVanish()
        {
            StopTween();
            if (_iceShell != null) { Destroy(_iceShell); _iceShell = null; }
            if (_iceCounter != null) { Destroy(_iceCounter.gameObject); _iceCounter = null; }
            StartCoroutine(VanishRoutine());
        }

        IEnumerator VanishRoutine()
        {
            // BEYAZ IŞIKLA PARÇALANMA (3. tur bulgusu).
            //
            // Referansta (21-30 yürüyüşü, 21. bölüm) blok kapıya girerken
            // BEYAZ bir konturla aydınlanıyor, sonra dağılıyor. Bizde yalnız
            // küçülüp kayboluyordu; kapıdan çıkan kırıntı dışında "kırıldı"
            // diyen hiçbir şey yoktu.
            //
            // DERS (ışık, olayın nerede olduğunu söyler): Blok sessizce
            // küçülünce göz onu takip etmiyor — ekranda üç yerde bir şey
            // olurken hangisinin "emilme" olduğu belirsiz kalıyor. Bir kare
            // beyazlama, gözü tam o bloğa çiviliyor.
            EnsureOutline();
            var flash = _outline != null ? _outline.GetComponent<MeshRenderer>() : null;
            Material flashMaterial = null;
            if (flash != null)
            {
                // Kontur normalde ince; parlama anında kalınlaşıyor ki
                // beyaz gerçekten GÖRÜNSÜN.
                _outline.transform.localScale = Vector3.one * 1.16f;
                flashMaterial = ViewKit.Translucent(Color.white);
                flash.sharedMaterial = flashMaterial;
            }

            const float duration = 0.18f;
            Vector3 startScale = transform.localScale;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                transform.localScale = startScale * (1f + 0.25f * k) * (1f - k);

                // Beyaz erken sönüyor: parlama bir VURUŞ, perde değil.
                if (flashMaterial != null)
                {
                    var c = Color.white;
                    c.a = 1f - Mathf.Clamp01(k * 1.6f);
                    flashMaterial.color = c;
                    if (flashMaterial.HasProperty("_BaseColor"))
                        flashMaterial.SetColor("_BaseColor", c);
                }
                yield return null;
            }

            if (flashMaterial != null) Destroy(flashMaterial);
            Destroy(gameObject);
        }

        public void PlayAbsorb(Vector3 outwardWorldDir, float travel, float duration)
        {
            StopTween();
            StartCoroutine(AbsorbRoutine(outwardWorldDir, travel, duration));
        }

        IEnumerator AbsorbRoutine(Vector3 dir, float travel, float duration)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + dir * travel;

            // Hareket eksenine göre daralma: yalnızca gidiş yönünde incelir,
            // dik eksen neredeyse korunur — "yuvaya sığmak için sıkışma" hissi.
            float axisX = Mathf.Abs(dir.x);
            float axisZ = Mathf.Abs(dir.z);

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                float eased = k * k;                       // hızlanarak girer

                transform.position = Vector3.Lerp(startPos, endPos, eased)
                                     + Vector3.down * (eased * 0.18f); // yuvaya çöker

                float shrink = Mathf.Lerp(1f, 0.06f, eased);
                float keep = Mathf.Lerp(1f, 0.82f, eased);
                transform.localScale = new Vector3(
                    Mathf.Lerp(keep, shrink, axisX),
                    Mathf.Lerp(1f, 0.55f, eased),
                    Mathf.Lerp(keep, shrink, axisZ));
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
