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

        public static BlockView Create(Transform parent, BlockModel model, BoardSpace space, Material material)
        {
            // Primitif küp collider ile doğar; fizik kullanmıyoruz — söküyoruz.
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Block_{model.Id}_{model.CurrentColor}";
            go.transform.SetParent(parent, worldPositionStays: false);
            Destroy(go.GetComponent<Collider>());

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material; // paylaşımlı — SRP Batcher dostu

            var view = go.AddComponent<BlockView>();
            view._model = model;
            view._space = space;
            view._baseScale = new Vector3(model.W - EdgeInset, BlockHeight, model.H - EdgeInset);
            go.transform.localScale = view._baseScale;
            view.SyncFromModel();
            return view;
        }

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
