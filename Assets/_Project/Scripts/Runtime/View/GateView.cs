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
        const float EndInset = 0.10f;

        // Kapı ölçüleri: "çerçeveyle aynı" seçiliyse kapı ÇERÇEVE BANDINI
        // birebir doldurur — referans oyunda kapı, çerçevenin renkli bir
        // parçası gibi görünür; ayrı bir çıkıntı yoktur.
        //
        // Konumlandırma: tahta kenarı ile çerçevenin dış kenarı arasındaki
        // bandın ORTASINA oturur, yani dışa kaydırma = kalınlığın yarısı.
        static bool MatchesFrame => VisualSettings.Current == null ||
                                    VisualSettings.Current.gateMatchesWall;

        // Z-FIGHTING: kapı ile çerçeve tam olarak aynı hacmi kaplarsa yüzeyler
        // çakışır ve kamera açısına göre titreyen tırtıklı kenarlar oluşur.
        // Kapıyı bir tık büyük yapmak çakışmayı kaldırır; fark gözle görülmez.
        const float FrameOverlapBias = 0.02f;

        static float BarHeight => VisualSettings.Current == null ? 0.34f
            : MatchesFrame ? VisualSettings.Current.frameHeight + FrameOverlapBias
                           : VisualSettings.Current.gateBarHeight;

        static float BarDepth => VisualSettings.Current == null ? 0.55f
            : MatchesFrame ? VisualSettings.Current.frameThickness + FrameOverlapBias
                           : VisualSettings.Current.gateBarDepth;

        static float OutwardOffset => VisualSettings.Current == null ? 0.275f
            : MatchesFrame ? VisualSettings.Current.frameThickness * 0.5f
                           : VisualSettings.Current.gateOutwardOffset;   // kabartma yüksekliği

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

            // Kapı barının kenar boyunca kapladığı aralık. Tahtanın KÖŞESİNE
            // dayanan uç, çerçevenin yuvarlatılmış köşesinin içine girip
            // "içinden geçmiş" gibi görünüyordu — o ucu köşeden uzaklaştırıyoruz.
            ResolveSpan(model, space, out float barMin, out float barMax);

            float spanCenter = (barMin + barMax) * 0.5f;
            float barLength = Mathf.Max(0.25f, barMax - barMin);
            float offCoord = model.EdgeCoord + model.OutwardSign * OutwardOffset;

            Vector3 center;
            Vector3 scale;
            if (model.EdgeHorizontal)
            {
                center = space.CornerToWorld(spanCenter, offCoord, BarHeight * 0.5f);
                scale = new Vector3(barLength, BarHeight, BarDepth);
            }
            else
            {
                center = space.CornerToWorld(offCoord, spanCenter, BarHeight * 0.5f);
                scale = new Vector3(BarDepth, BarHeight, barLength);
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
        /// Barın kenar boyunca kaplayacağı aralığı verir.
        ///
        /// SORUN: Kapı, tahtanın köşesine dayandığında (örneğin üst kenarın en
        /// solundaki kapı) bar, çerçevenin YUVARLATILMIŞ köşesinin içine giriyor
        /// ve tepeden bakınca "çerçevenin içinden geçmiş" gibi görünüyordu.
        ///
        /// ÇÖZÜM: Serbest uçlar eskisi gibi küçük bir payla kısalır; ama tahta
        /// KÖŞESİNE dayanan uç, köşe yarıçapı kadar içeri çekilir. Kapı böylece
        /// köşenin yuvarlak kısmına hiç girmez.
        ///
        /// DERS (neden kısaltmak yerine kaydırmıyoruz?): Barı olduğu gibi içeri
        /// ötelemek onu kapının GERÇEK açıklığından kaydırırdı — oyuncu bloğu
        /// barın hizasına getirir ama kapı orada değildir. Uzunluğu kısaltmak
        /// görseli düzeltirken açıklığın konumunu bozmuyor.
        /// </summary>
        static void ResolveSpan(GateModel model, BoardSpace space, out float min, out float max)
        {
            min = model.SpanMin;
            max = model.SpanMax;

            float cfgRadius = VisualSettings.Current != null
                ? VisualSettings.Current.frameCornerRadius : 0.6f;
            // Köşeden kaçınma payı: yarıçapın biraz altı yeter, tamamı kadar
            // çekmek kısa kapılarda barı gereksiz kırpıyor.
            float clearance = Mathf.Min(cfgRadius * 0.75f, (max - min) * 0.35f);

            float boardSpan = model.EdgeHorizontal ? space.Width : space.Height;

            // Tahta köşesine dayanan uç: köşe payı kadar içeri.
            // Serbest uç: yalnızca komşu kapıdan ayrışsın diye küçük pay.
            min += min <= 0.001f ? clearance : EndInset * 0.5f;
            max -= max >= boardSpan - 0.001f ? clearance : EndInset * 0.5f;
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

            var cfg = VisualSettings.Current;
            float size = cfg != null ? cfg.arrowSize : 0.20f;
            float rise = cfg != null ? cfg.arrowRise : 0.055f;
            float cornerRadius = cfg != null ? cfg.arrowCornerRadius : 0.3f;

            // Dışarı yönü: yatay kapılarda ±Z, dikey kapılarda ±X.
            Vector3 forward = model.EdgeHorizontal
                ? new Vector3(0f, 0f, -model.OutwardSign)
                : new Vector3(model.OutwardSign, 0f, 0f);
            Vector3 across = new Vector3(-forward.z, 0f, forward.x);

            Vector3 tip = forward * size;
            Vector3 left = across * size * 0.78f - forward * size * 0.48f;
            Vector3 right = -across * size * 0.78f - forward * size * 0.48f;

            // Referans oyunda okun köşeleri YUMUŞAK; keskin üçgen sert ve
            // "vektör klibi" gibi duruyor. Her köşeyi küçük bir yay ile
            // yuvarlıyoruz (köşe kesme + ara noktalar).
            var outline = RoundedTriangle(tip, left, right, cornerRadius);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            float top = rise;

            // Üst yüz: merkezden yelpaze.
            Vector3 center = (tip + left + right) / 3f;
            int capCenter = verts.Count;
            verts.Add(center + Vector3.up * top);
            normals.Add(Vector3.up);
            colors.Add(Color.white);

            int capStart = verts.Count;
            foreach (var point in outline)
            {
                verts.Add(point + Vector3.up * top);
                normals.Add(Vector3.up);
                colors.Add(Color.white);
            }
            for (int i = 0; i < outline.Count; i++)
            {
                int a = capStart + i;
                int b = capStart + (i + 1) % outline.Count;
                tris.Add(capCenter); tris.Add(b); tris.Add(a);
            }

            // Yan yüzler: kabartma hissi (düz üçgen tek renk kalıyordu).
            for (int i = 0; i < outline.Count; i++)
            {
                Vector3 a = outline[i];
                Vector3 b = outline[(i + 1) % outline.Count];
                Vector3 edge = (b - a).normalized;
                Vector3 outward = Vector3.Cross(Vector3.up, edge).normalized;

                int start = verts.Count;
                verts.Add(a); verts.Add(b);
                verts.Add(b + Vector3.up * top); verts.Add(a + Vector3.up * top);
                for (int n = 0; n < 4; n++) normals.Add(outward);
                colors.Add(new Color(0.66f, 0.66f, 0.66f));
                colors.Add(new Color(0.66f, 0.66f, 0.66f));
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

        /// <summary>
        /// Üçgenin köşelerini yuvarlatıp kapalı bir dış çizgi noktası listesi verir.
        /// <paramref name="radius01"/> 0 = keskin üçgen, 1 = maksimum yumuşama.
        /// </summary>
        static List<Vector3> RoundedTriangle(Vector3 a, Vector3 b, Vector3 c, float radius01)
        {
            var corners = new[] { a, b, c };
            var outline = new List<Vector3>();

            if (radius01 <= 0.001f)
            {
                outline.AddRange(corners);
                return outline;
            }

            const int arcSegments = 4;
            for (int i = 0; i < 3; i++)
            {
                Vector3 prev = corners[(i + 2) % 3];
                Vector3 corner = corners[i];
                Vector3 next = corners[(i + 1) % 3];

                // Köşeye komşu kenarlar boyunca içeri kaçış noktaları.
                float cut = Mathf.Clamp01(radius01) * 0.42f;
                Vector3 from = Vector3.Lerp(corner, prev, cut);
                Vector3 to = Vector3.Lerp(corner, next, cut);

                for (int s = 0; s <= arcSegments; s++)
                {
                    float t = s / (float)arcSegments;
                    // Köşeyi kontrol noktası kabul eden ikinci derece Bézier:
                    // yay gibi yumuşak bir geçiş verir, trigonometri gerekmez.
                    Vector3 p = Vector3.Lerp(
                        Vector3.Lerp(from, corner, t),
                        Vector3.Lerp(corner, to, t), t);
                    outline.Add(p);
                }
            }
            return outline;
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

        /// <summary>
        /// Rengi tükendi: bar soluklaşır, ok da soluk materyale geçer.
        ///
        /// Kapı GİZLENMEZ — referans oyunda da kapı yerinde durup solar.
        /// Gizlemek duvarda boşluk bırakıyordu (kapı kenarına duvar örülmez).
        /// </summary>
        public void SetGhost(Material ghostMaterial)
        {
            _renderer.sharedMaterial = ghostMaterial;
            if (_arrow == null) return;

            var arrowRenderer = _arrow.GetComponent<MeshRenderer>();
            if (arrowRenderer != null) arrowRenderer.sharedMaterial = ViewKit.ArrowGhostMaterial;
        }
    }
}
