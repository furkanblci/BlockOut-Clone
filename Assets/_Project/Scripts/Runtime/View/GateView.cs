using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Kapının sahnedeki görseli: kenarın hemen dışında yatık bar.
    /// M2 halleri: buzlu (renk gizli + sayaç), normal (renk), ghost (soluk).
    /// Oklar ve cila M4'te.
    /// </summary>
    public sealed class GateView : MonoBehaviour
    {
        const float BarHeight = 0.22f;
        const float BarDepth = 0.3f;
        const float OutwardOffset = 0.2f;
        const float EndInset = 0.12f;

        MeshRenderer _renderer;
        Material _colorMaterial;
        TextMesh _iceCounter;
        GateModel _model;

        public static GateView Create(
            Transform parent, GateModel model, BoardSpace space, Material colorMaterial)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Gate_{model.ActiveColor}_{model.Side.ToId()}";
            go.transform.SetParent(parent, worldPositionStays: false);
            Destroy(go.GetComponent<Collider>());

            float spanCenter = (model.SpanMin + model.SpanMax) * 0.5f;
            float offCoord = model.EdgeCoord + model.OutwardSign * OutwardOffset;

            Vector3 center;
            Vector3 scale;
            if (model.EdgeHorizontal)
            {
                center = space.CornerToWorld(spanCenter, offCoord, BarHeight * 0.5f);
                scale = new Vector3(model.Length - EndInset, BarHeight, BarDepth);
            }
            else
            {
                center = space.CornerToWorld(offCoord, spanCenter, BarHeight * 0.5f);
                scale = new Vector3(BarDepth, BarHeight, model.Length - EndInset);
            }
            go.transform.position = center;
            go.transform.localScale = scale;

            var view = go.AddComponent<GateView>();
            view._model = model;
            view._renderer = go.GetComponent<MeshRenderer>();
            view._colorMaterial = colorMaterial;

            if (model.IsIced)
            {
                // Buz rengi GİZLER (video kuralı) — bar buz materyaliyle başlar.
                view._renderer.sharedMaterial = ViewKit.Ice;
                view._iceCounter = ViewKit.CreateCounter(
                    parent, center + Vector3.up * (BarHeight * 0.5f + 0.12f), model.IceCount);
            }
            else
            {
                view._renderer.sharedMaterial = colorMaterial;
            }

            return view;
        }

        public void UpdateIceCount()
        {
            if (_iceCounter != null)
                _iceCounter.text = _model.IceCount.ToString();
        }

        /// <summary>Buz kırıldı: gizli renk ortaya çıkar.</summary>
        public void RevealColor()
        {
            if (_iceCounter != null) Destroy(_iceCounter.gameObject);
            _iceCounter = null;
            _renderer.sharedMaterial = _colorMaterial;
        }

        /// <summary>Kuyruk ilerledi: yeni aktif rengin materyali (L21+ olasılığı).</summary>
        public void SetColorMaterial(Material material)
        {
            _colorMaterial = material;
            if (!_model.IsIced)
                _renderer.sharedMaterial = material;
        }

        /// <summary>Rengi tükendi: soluk (ghost) hal.</summary>
        public void SetGhost(Material ghostMaterial) =>
            _renderer.sharedMaterial = ghostMaterial;
    }
}
