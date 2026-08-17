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
        MeshRenderer _renderer;
        MeshFilter _filter;
        GameObject _iceShell;
        GameObject _axisArrow;
        TextMesh _iceCounter;
        Coroutine _tween;
        PT.Sequence _motion;
        bool _highlighted;

        public static BlockView Create(
            Transform parent, BlockModel model, BoardSpace space, Material material)
        {
            var go = new GameObject($"Block_{model.Id}_{model.CurrentColor}");
            go.transform.SetParent(parent, worldPositionStays: false);

            var view = go.AddComponent<BlockView>();
            view._model = model;
            view._space = space;
            view._filter = go.AddComponent<MeshFilter>();
            view._filter.sharedMesh = BrickMeshBuilder.Get(model);

            view._renderer = go.AddComponent<MeshRenderer>();
            view._renderer.sharedMaterial = material;  // paylaşımlı — SRP Batcher dostu
            view._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view._renderer.receiveShadows = false;

            view.BuildContactShadow();
            if (model.Axis != MoveAxis.Free) view.BuildAxisArrow();
            view.SyncFromModel();
            if (model.IsFrozen) view.BuildIceShell(parent);
            return view;
        }

        /// <summary>
        /// Yönlü blokların üstündeki çift başlı ok.
        ///
        /// DERS (kuralı GÖRÜNÜR kılmak): Eksen kısıtı görünmezse oyuncu bloğu
        /// çekmeye çalışır, olmaz, oyunu bozuk sanır. Bulmacada her kısıt
        /// ekranda okunabilir olmalı — referans oyun da bu yüzden oku bloğun
        /// tam ortasına, iri ve kabartmalı basıyor.
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
            // Ok, bloğun kısa kenarına göre ölçeklenir ki taşmasın.
            float span = Mathf.Min(_model.W, _model.H);
            float length = Mathf.Min(horizontal ? _model.W : _model.H, span * 1.6f) * 0.34f;
            float thickness = span * 0.10f;
            float head = span * 0.17f;

            var mesh = BuildDoubleArrow(horizontal, length, thickness, head);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ViewKit.AxisArrowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            float top = _filter != null && _filter.sharedMesh != null
                ? _filter.sharedMesh.bounds.max.y
                : BrickMeshBuilder.Height;
            go.transform.localPosition = new Vector3(center.x, top + 0.005f, center.y);
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

        /// <summary>
        /// Çift başlı ok: ortada gövde, iki uçta üçgen baş. Düz bir quad yerine
        /// alçak prizma olarak kurulur — yan yüzler ışığı farklı açıyla alınca
        /// ok "kabartma" gibi okunur, çıkartma gibi değil.
        ///
        /// DERS (sarım yönü / winding): Üçgenin köşe SIRASI hangi yüzün "ön"
        /// olduğunu belirler; ters sıralı üçgen backface culling ile tamamen
        /// kaybolur. Bu yüzden ok HER ZAMAN yatay kurulur, dikey isteniyorsa
        /// mesh 90° DÖNDÜRÜLÜR — eksenleri (right ↔ forward) takas etmek
        /// el yönünü tersine çevirir ve okun görünmemesine yol açardı.
        /// </summary>
        static Mesh BuildDoubleArrow(bool horizontal, float length, float thickness, float head)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            const float rise = 0.035f;

            Vector3 along = Vector3.right;
            Vector3 across = Vector3.forward;

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int s = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                for (int i = 0; i < 4; i++) normals.Add(Vector3.up);
                tris.Add(s); tris.Add(s + 2); tris.Add(s + 1);
                tris.Add(s); tris.Add(s + 3); tris.Add(s + 2);
            }

            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int s = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                for (int i = 0; i < 3; i++) normals.Add(Vector3.up);
                tris.Add(s); tris.Add(s + 2); tris.Add(s + 1);
            }

            Vector3 up = Vector3.up * rise;
            float body = length - head;

            Quad(-along * body - across * thickness + up,
                  along * body - across * thickness + up,
                  along * body + across * thickness + up,
                 -along * body + across * thickness + up);

            Triangle(along * length + up,
                     along * body + across * head + up,
                     along * body - across * head + up);
            Triangle(-along * length + up,
                     -along * body - across * head + up,
                     -along * body + across * head + up);

            // Dikey ok: sarımı bozmayan gerçek bir 90° dönüş (x,z) → (z,-x).
            if (!horizontal)
                for (int i = 0; i < verts.Count; i++)
                {
                    var v = verts[i];
                    verts[i] = new Vector3(v.z, v.y, -v.x);
                }

            var mesh = new Mesh { name = "AxisArrow" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Modelin hücre konumunu dünyaya yansıtır. Sürükleme sırasında her kare çağrılır.</summary>
        public void SyncFromModel()
        {
            transform.position = WorldPosition(_highlighted ? DragLift : 0f);
        }

        Vector3 WorldPosition(float lift) =>
            _space.RectCenterToWorld(_model.Position, _model.W, _model.H, lift);

        public void SetHighlight(bool on)
        {
            if (_highlighted == on) return;
            _highlighted = on;

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

        /// <summary>Bloğun hücrelerini kaplayan yatay gölge mesh'i (yerel uzayda).</summary>
        Mesh BuildShadowMesh()
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            float halfW = _model.W * 0.5f, halfH = _model.H * 0.5f;
            foreach (var cell in _model.Cells)
            {
                float x0 = -halfW + cell.x, x1 = x0 + 1f;
                float z1 = halfH - cell.y, z0 = z1 - 1f;

                int start = verts.Count;
                verts.Add(new Vector3(x0, 0f, z0)); verts.Add(new Vector3(x1, 0f, z0));
                verts.Add(new Vector3(x1, 0f, z1)); verts.Add(new Vector3(x0, 0f, z1));
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
                for (int n = 0; n < 4; n++) normals.Add(Vector3.up);

                tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
                tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
            }

            var mesh = new Mesh { name = "BlockShadow" };
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
            _iceShell.AddComponent<MeshFilter>().sharedMesh =
                _filter != null ? _filter.sharedMesh : BrickMeshBuilder.Get(_model);

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
            // Yatayda 1.0: 1.02 verilince komşu buzlu bloklar birbirine değip
            // tek bir kütleye dönüşüyordu ve tahta okunmaz oluyordu. Yalnız
            // DİKEYDE büyütmek z-fighting'i çözmeye yetiyor.
            _iceShell.transform.localScale = new Vector3(1f, 1.05f, 1f);

            // Buz OPAK olduğu için tuğlayı çizmeye gerek yok: hem referanstaki
            // gibi renk gizleniyor hem de bir çizim çağrısı tasarruf ediyoruz.
            if (_renderer != null) _renderer.enabled = false;
            // Ok da buzun içinde kalmalı; kabuğun tepesinden dışarı taşmasın.
            if (_axisArrow != null) _axisArrow.SetActive(false);

            BuildFrostShards(center, shellHeight);

            _iceCounter = ViewKit.CreateCounter(
                parent,
                center + Vector3.up * (shellHeight * 0.5f + 0.06f),
                _model.IceCount);
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
        public void SetLayerMaterial(Material material) => _renderer.sharedMaterial = material;

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
            const float duration = 0.18f;
            Vector3 startScale = transform.localScale;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                transform.localScale = startScale * (1f + 0.25f * k) * (1f - k);
                yield return null;
            }

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
