using System.Collections;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Bir bloğun sahnedeki görseli. Model → dünya yansıtmasından, vurgulama
    /// ve emilme animasyonundan sorumludur; OYUN KARARI VERMEZ.
    /// M1'de geçici küp primitifi; M4'te prosedürel LEGO tuğla mesh'i gelecek.
    /// </summary>
    public sealed class BlockView : MonoBehaviour
    {
        const float BlockHeight = 0.5f;
        const float EdgeInset = 0.08f;   // komşu bloklarla görsel ayrım için küçük boşluk
        const float DragLift = 0.1f;     // sürüklerken hafif kaldırma — "tuttum" hissi

        BlockModel _model;
        BoardSpace _space;
        Vector3 _baseScale;
        bool _highlighted;
        MeshRenderer _renderer;
        GameObject _iceShell;
        TextMesh _iceCounter;

        public static BlockView Create(Transform parent, BlockModel model, BoardSpace space, Material material)
        {
            // Primitif küp collider ile doğar; fizik kullanmıyoruz — söküyoruz.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Block_{model.Id}_{model.CurrentColor}";
            go.transform.SetParent(parent, worldPositionStays: false);
            Destroy(go.GetComponent<Collider>());

            var view = go.AddComponent<BlockView>();
            view._renderer = go.GetComponent<MeshRenderer>();
            view._renderer.sharedMaterial = material; // paylaşımlı — SRP Batcher dostu
            view._model = model;
            view._space = space;
            view._baseScale = new Vector3(model.W - EdgeInset, BlockHeight, model.H - EdgeInset);
            go.transform.localScale = view._baseScale;
            view.SyncFromModel();

            if (model.IsFrozen)
                view.BuildIceShell(parent);

            return view;
        }

        /// <summary>
        /// Buz kabuğu + sayaç. Bloğun ÇOCUĞU değil, kardeşi: bloğun eşit olmayan
        /// ölçeği (w≠h) çocukları da ezerdi. Donmuş blok zaten hareket edemez,
        /// dünya konumuna sabitlemek güvenli.
        /// </summary>
        void BuildIceShell(Transform parent)
        {
            _iceShell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _iceShell.name = $"Ice_{_model.Id}";
            _iceShell.transform.SetParent(parent, worldPositionStays: false);
            Destroy(_iceShell.GetComponent<Collider>());
            _iceShell.GetComponent<MeshRenderer>().sharedMaterial = ViewKit.Ice;

            Vector3 center = _space.RectCenterToWorld(
                _model.Position, _model.W, _model.H, BlockHeight * 0.5f + 0.06f);
            _iceShell.transform.position = center;
            _iceShell.transform.localScale = new Vector3(
                _model.W - 0.02f, BlockHeight + 0.16f, _model.H - 0.02f);

            _iceCounter = ViewKit.CreateCounter(
                parent, center + Vector3.up * (BlockHeight * 0.5f + 0.16f), _model.IceCount);
        }

        public void UpdateIceCount()
        {
            if (_iceCounter != null)
                _iceCounter.text = _model.IceCount.ToString();
        }

        /// <summary>Buz kırıldı: kabuk ve sayaç gider, blok normal bloğa döner. Parçacıklar M4'te.</summary>
        public void ShatterIce()
        {
            if (_iceShell != null) Destroy(_iceShell);
            if (_iceCounter != null) Destroy(_iceCounter.gameObject);
            _iceShell = null;
            _iceCounter = null;
        }

        /// <summary>Katman soyulunca dış rengin materyali değişir.</summary>
        public void SetLayerMaterial(Material material) => _renderer.sharedMaterial = material;

        /// <summary>Modelin hücre-uzayı konumunu dünyaya yansıtır. Sürükleme sırasında her kare çağrılır.</summary>
        public void SyncFromModel()
        {
            float lift = _highlighted ? DragLift : 0f;
            transform.position = _space.RectCenterToWorld(
                _model.Position, _model.W, _model.H, BlockHeight * 0.5f + lift);
        }

        public void SetHighlight(bool on)
        {
            if (_highlighted == on) return;
            _highlighted = on;
            transform.localScale = on ? _baseScale * 1.05f : _baseScale;
            SyncFromModel();
        }

        /// <summary>Kapıdan emilme: dışarı doğru kayarken küçülür, sonra yok edilir (M4'te havuza dönecek).</summary>
        public void PlayAbsorb(Vector3 outwardWorldDir, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(AbsorbRoutine(outwardWorldDir, duration));
        }

        IEnumerator AbsorbRoutine(Vector3 dir, float duration)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + dir * 0.9f;
            Vector3 startScale = transform.localScale;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                k = k * k; // ease-in: önce yavaş, sonra hızla emilir
                transform.position = Vector3.Lerp(startPos, endPos, k);
                transform.localScale = Vector3.Lerp(startScale, startScale * 0.1f, k);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
