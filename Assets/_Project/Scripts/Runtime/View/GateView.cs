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
        const float BarHeight = 0.40f;    // duvarla aynı seviye
        const float BarDepth = 0.44f;     // duvardan kalın: göze çarpsın
        const float OutwardOffset = 0.16f;
        const float EndInset = 0.10f;

        const float ArrowSize = 0.20f;
        const float ArrowRise = 0.055f;   // kabartma yüksekliği

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
            view._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view._colorMaterial = colorMaterial;
            view._arrow = CreateArrow(parent, model, center);

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
        /// Kapının üstündeki ok: düz üçgen değil ALÇAK PRİZMA. Düz üçgen tek
        /// renk kalır ve "detaysız" görünür; prizmanın yan yüzleri ışığı farklı
        /// açıyla aldığı için kenarları belirginleşir.
        /// </summary>
        static GameObject CreateArrow(Transform parent, GateModel model, Vector3 barCenter)
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(parent, worldPositionStays: false);

            // Dışarı yönü: yatay kapılarda ±Z, dikey kapılarda ±X.
            Vector3 forward = model.EdgeHorizontal
                ? new Vector3(0f, 0f, -model.OutwardSign)
                : new Vector3(model.OutwardSign, 0f, 0f);
            Vector3 across = new Vector3(-forward.z, 0f, forward.x);

            Vector3 tip = forward * ArrowSize;
            Vector3 left = across * ArrowSize * 0.78f - forward * ArrowSize * 0.48f;
            Vector3 right = -across * ArrowSize * 0.78f - forward * ArrowSize * 0.48f;

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            float top = ArrowRise;
            Vector3[] baseRing = { tip, left, right };

            // Üst yüz
            for (int i = 0; i < 3; i++)
            {
                verts.Add(baseRing[i] + Vector3.up * top);
                normals.Add(Vector3.up);
                colors.Add(Color.white);
            }
            tris.Add(0); tris.Add(2); tris.Add(1);

            // Yan yüzler (taban → üst)
            for (int i = 0; i < 3; i++)
            {
                Vector3 a = baseRing[i];
                Vector3 b = baseRing[(i + 1) % 3];
                Vector3 edge = (b - a).normalized;
                Vector3 outward = Vector3.Cross(Vector3.up, edge).normalized;

                int start = verts.Count;
                verts.Add(a); verts.Add(b);
                verts.Add(b + Vector3.up * top); verts.Add(a + Vector3.up * top);
                for (int n = 0; n < 4; n++) normals.Add(outward);
                colors.Add(new Color(0.72f, 0.72f, 0.72f));
                colors.Add(new Color(0.72f, 0.72f, 0.72f));
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

            go.transform.position = barCenter + Vector3.up * (BarHeight * 0.5f + 0.005f);
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
            if (!_model.IsIced) _renderer.sharedMaterial = material;
        }

        /// <summary>Rengi tükendi: soluk (ghost) hal, ok da söner.</summary>
        public void SetGhost(Material ghostMaterial)
        {
            _renderer.sharedMaterial = ghostMaterial;
            if (_arrow != null) _arrow.SetActive(false);
        }
    }
}
