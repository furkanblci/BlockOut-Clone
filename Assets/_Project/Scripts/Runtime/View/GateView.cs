using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Kapının sahnedeki görseli: kenarın hemen dışında, kapı renginde yatık bar.
    /// M1'de sade bar; oklar, ghost solması ve kuyruk göstergesi M2/M4'te.
    /// </summary>
    public sealed class GateView : MonoBehaviour
    {
        const float BarHeight = 0.22f;
        const float BarDepth = 0.3f;      // kenara dik kalınlık
        const float OutwardOffset = 0.2f; // kenar çizgisinden dışarı kayma
        const float EndInset = 0.12f;     // kenar boyunca uçlardan içeri çekme

        public static GateView Create(Transform parent, GateModel model, BoardSpace space, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Gate_{model.ActiveColor}_{model.Side.ToId()}";
            go.transform.SetParent(parent, worldPositionStays: false);
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = material;

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

            return go.AddComponent<GateView>();
        }
    }
}
