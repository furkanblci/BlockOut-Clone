using System.Collections;
using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

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
        TextMesh _iceCounter;
        Coroutine _tween;
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
            view.SyncFromModel();
            if (model.IsFrozen) view.BuildIceShell(parent);
            return view;
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
                _tween = StartCoroutine(SnapRoutine());
            }
        }

        IEnumerator SnapRoutine()
        {
            Vector3 fromPos = transform.position;
            Vector3 fromScale = transform.localScale;
            Vector3 toPos = WorldPosition(0f);

            for (float t = 0f; t < SnapDuration; t += Time.deltaTime)
            {
                float k = t / SnapDuration;
                k = 1f - (1f - k) * (1f - k); // ease-out
                transform.position = Vector3.Lerp(fromPos, toPos, k);
                transform.localScale = Vector3.Lerp(fromScale, Vector3.one, k);
                yield return null;
            }

            transform.position = toPos;
            transform.localScale = Vector3.one;
            _tween = null;
        }

        void StopTween()
        {
            if (_tween != null) StopCoroutine(_tween);
            _tween = null;
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
            _iceShell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _iceShell.name = $"Ice_{_model.Id}";
            _iceShell.transform.SetParent(parent, worldPositionStays: false);
            Destroy(_iceShell.GetComponent<Collider>());

            var renderer = _iceShell.GetComponent<MeshRenderer>();
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
            const float SideInset = 0.09f;   // tuğladaki inset ile aynı his
            float shellHeight = brickTop;

            Vector3 center = _space.RectCenterToWorld(
                _model.Position, _model.W, _model.H, shellHeight * 0.5f);
            _iceShell.transform.position = center;
            _iceShell.transform.localScale = new Vector3(
                _model.W - SideInset, shellHeight, _model.H - SideInset);

            // Buz OPAK olduğu için tuğlayı çizmeye gerek yok: hem referanstaki
            // gibi renk gizleniyor hem de bir çizim çağrısı tasarruf ediyoruz.
            if (_renderer != null) _renderer.enabled = false;

            _iceCounter = ViewKit.CreateCounter(
                parent,
                center + Vector3.up * (shellHeight * 0.5f + 0.06f),
                _model.IceCount);
        }

        public void UpdateIceCount()
        {
            if (_iceCounter != null) _iceCounter.text = _model.IceCount.ToString();
        }

        /// <summary>Buz kırıldı: kabuk ve sayaç gider, blok serbest kalır.</summary>
        public void ShatterIce()
        {
            if (_iceShell != null) Destroy(_iceShell);
            if (_iceCounter != null) Destroy(_iceCounter.gameObject);
            _iceShell = null;
            _iceCounter = null;

            // Gizlenen renk ortaya çıkar — video kuralı.
            if (_renderer != null) _renderer.enabled = true;
        }

        /// <summary>Katman soyulunca dış rengin materyali değişir.</summary>
        public void SetLayerMaterial(Material material) => _renderer.sharedMaterial = material;

        /// <summary>Tahta girişinde blokların sırayla yerine oturması.</summary>
        public void PlayIntro(float delay, float duration)
        {
            StopTween();
            _tween = StartCoroutine(IntroRoutine(delay, duration));
        }

        IEnumerator IntroRoutine(float delay, float duration)
        {
            Vector3 target = WorldPosition(0f);
            Vector3 start = target + Vector3.up * 2.2f; // fazla yüksek düşüş tahtanın dışına taşıyor
            transform.position = start;
            transform.localScale = Vector3.one * 0.6f;

            yield return new WaitForSeconds(delay);

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float eased = 1f - (1f - k) * (1f - k) * (1f - k); // ease-out cubic
                transform.position = Vector3.Lerp(start, target, eased);
                transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, eased);
                yield return null;
            }

            transform.position = target;
            transform.localScale = Vector3.one;
            _tween = null;
        }

        /// <summary>
        /// Kapıdan emilme: blok kapı çizgisine doğru ilerlerken hareket ekseninde
        /// hızla incelir, hafifçe aşağı çöker ve yuvaya girmiş gibi kaybolur.
        ///
        /// <paramref name="travel"/> = bloğun MERKEZİNİN kapı çizgisine olan
        /// mesafesi. Sabit bir mesafe kullanmak (eski hali) bloğun duvarın
        /// ÜSTÜNDEN geçmesine yol açıyordu; artık tam yuvada duruyor.
        /// </summary>
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
