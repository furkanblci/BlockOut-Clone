using System.Collections;
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
        const float DragLift = 0.14f;      // tutunca hafif kalkma
        const float DragScale = 1.06f;
        const float SnapDuration = 0.09f;

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
            view._filter.sharedMesh = BrickMeshBuilder.Get(model.W, model.H);

            view._renderer = go.AddComponent<MeshRenderer>();
            view._renderer.sharedMaterial = material;  // paylaşımlı — SRP Batcher dostu
            view._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view._renderer.receiveShadows = false;

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

            Vector3 center = _space.RectCenterToWorld(
                _model.Position, _model.W, _model.H, BrickMeshBuilder.Height * 0.5f + 0.05f);
            _iceShell.transform.position = center;
            _iceShell.transform.localScale = new Vector3(
                _model.W - 0.02f, BrickMeshBuilder.Height + 0.22f, _model.H - 0.02f);

            _iceCounter = ViewKit.CreateCounter(
                parent, center + Vector3.up * (BrickMeshBuilder.Height * 0.5f + 0.2f),
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
            Vector3 start = target + Vector3.up * 5f;
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
        /// Kapıdan emilme: önce kapıya doğru squash (sıkışma), sonra hızla
        /// içeri kayıp yok olma. Squash yönü hareket eksenindedir — blok
        /// gerçekten deliğe sığmak için eziliyormuş gibi görünür.
        /// </summary>
        public void PlayAbsorb(Vector3 outwardWorldDir, float duration)
        {
            StopTween();
            StartCoroutine(AbsorbRoutine(outwardWorldDir, duration));
        }

        IEnumerator AbsorbRoutine(Vector3 dir, float duration)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + dir * 1.1f;
            Vector3 startScale = Vector3.one;

            // Hareket eksenini sıkıştır, dik ekseni hafif şişir.
            Vector3 squash = new Vector3(
                Mathf.Lerp(1f, 0.25f, Mathf.Abs(dir.x)),
                0.8f,
                Mathf.Lerp(1f, 0.25f, Mathf.Abs(dir.z)));

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                transform.position = Vector3.Lerp(startPos, endPos, k * k);
                transform.localScale = Vector3.Lerp(startScale, squash, Mathf.Min(1f, k * 2f));
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
