using System.Collections.Generic;
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
        GameObject _arrow;

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
            view._arrow = CreateArrow(go.transform, model, center, scale);

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

        /// <summary>
        /// Kapının üstündeki beyaz ok — referans oyunda çıkış yönünü gösterir.
        /// Üç köşeli düz bir mesh; ayrı asset gerektirmez.
        /// </summary>
        static GameObject CreateArrow(Transform parent, GateModel model, Vector3 center, Vector3 scale)
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(parent, worldPositionStays: false);

            // Ok, barın DAR kenarına sığmalı: toplam derinlik ≈ size*1.45,
            // bar derinliği 0.3 hücre. Daha büyüğü barın dışına taşıyor.
            const float size = 0.16f;
            var mesh = new Mesh { name = "GateArrow" };

            // Ok, dışa doğru bakar: hücre uzayında dışarı yönü dünyada
            // yatay kapılarda -Z/+Z, dikey kapılarda ±X'e karşılık gelir.
            Vector2 outward = model.EdgeHorizontal
                ? new Vector2(0f, -model.OutwardSign)
                : new Vector2(model.OutwardSign, 0f);
            Vector2 side = new Vector2(-outward.y, outward.x);

            Vector3 forward = new Vector3(outward.x, 0f, outward.y);
            Vector3 across = new Vector3(side.x, 0f, side.y);

            Vector3 tip = forward * size;
            Vector3 left = across * size * 0.75f - forward * size * 0.45f;
            Vector3 right = -across * size * 0.75f - forward * size * 0.45f;

            mesh.SetVertices(new List<Vector3> { tip, left, right });
            mesh.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up });
            // Sarım yönü: (0,1,2) normali AŞAĞI çevirip oku görünmez yapıyordu.
            mesh.SetTriangles(new[] { 0, 2, 1 }, 0);
            mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ViewKit.ArrowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            go.transform.position = center + Vector3.up * (scale.y * 0.5f + 0.01f);
            return go;
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
