using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Perdenin görseli (L20): bölgeyi kaplayan koyu panel + altın çerçeve +
    /// sayaç rozeti. Sayaç 0'a inince panel kalkar; içerik bloklarını
    /// ObstacleSystem doğurur. Açılma animasyonu M4'te.
    /// </summary>
    public sealed class CurtainView : MonoBehaviour
    {
        const float PanelHeight = 0.5f;

        TextMesh _counter;
        CurtainModel _model;

        public static CurtainView Create(Transform parent, CurtainModel model, BoardSpace space)
        {
            var root = new GameObject($"Curtain_{model.X}_{model.Y}");
            root.transform.SetParent(parent, worldPositionStays: false);

            Vector3 center = space.RectCenterToWorld(
                new Vector2(model.X, model.Y), model.W, model.H, PanelHeight * 0.5f);

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Frame";
            frame.transform.SetParent(root.transform, false);
            ViewKit.StripCollider(frame);
            frame.GetComponent<MeshRenderer>().sharedMaterial = ViewKit.CurtainFrame;
            frame.transform.position = center + Vector3.down * 0.03f;
            frame.transform.localScale = new Vector3(model.W + 0.1f, PanelHeight - 0.06f, model.H + 0.1f);

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(root.transform, false);
            ViewKit.StripCollider(panel);
            panel.GetComponent<MeshRenderer>().sharedMaterial = ViewKit.CurtainPanel;
            panel.transform.position = center;
            panel.transform.localScale = new Vector3(model.W - 0.04f, PanelHeight, model.H - 0.04f);

            // DERS (düz yüzey MALZEME hissi vermez): Perde tek düz bir gri
            // kutuydu; tahtanın üstünde "boş alan" gibi duruyor, altında blok
            // olduğunu düşündürmüyordu. Buzlu cam iki şeyle okunur: kenarda
            // parlayan bir çerçeve ve yüzeyde ışığın kırıldığı düzensiz
            // lekeler. İkisi de birkaç ek kutuyla veriliyor.
            AddGlassStreaks(root.transform, center, model);

            var view = root.AddComponent<CurtainView>();
            view._model = model;
            view._counter = ViewKit.CreateCounter(
                root.transform, center + Vector3.up * (PanelHeight * 0.5f + 0.12f), model.Count);
            view._counter.color = new Color(1f, 0.9f, 0.55f); // altın rozet hissi

            return view;
        }

        /// <summary>
        /// Buzlu camın üstündeki ışık çizgileri. Perdenin boyutuna göre
        /// ölçekleniyor; küçük perde iki, geniş perde beş çizgi alıyor.
        /// </summary>
        static void AddGlassStreaks(Transform parent, Vector3 center, CurtainModel model)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(model.W * model.H * 0.5f), 2, 5);
            var random = new System.Random(model.X * 73856093 ^ model.Y * 19349663);
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);

            for (int i = 0; i < count; i++)
            {
                var streak = GameObject.CreatePrimitive(PrimitiveType.Cube);
                streak.name = "Streak";
                streak.transform.SetParent(parent, worldPositionStays: false);
                ViewKit.StripCollider(streak);

                var renderer = streak.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = ViewKit.CurtainStreak;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                float length = Range(0.35f, 0.8f) * Mathf.Min(model.W, model.H);
                streak.transform.position = center + new Vector3(
                    Range(-model.W * 0.32f, model.W * 0.32f),
                    PanelHeight * 0.5f + 0.005f,
                    Range(-model.H * 0.32f, model.H * 0.32f));
                streak.transform.localScale = new Vector3(length, 0.02f, Range(0.06f, 0.14f));
                streak.transform.rotation = Quaternion.Euler(0f, Range(-40f, 40f), 0f);
            }
        }

        public void UpdateCount()
        {
            if (_counter != null)
                _counter.text = _model.Count.ToString();
        }

        public void Open() => Destroy(gameObject);
    }
}
