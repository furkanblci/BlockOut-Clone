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
            Destroy(frame.GetComponent<Collider>());
            frame.GetComponent<MeshRenderer>().sharedMaterial = ViewKit.CurtainFrame;
            frame.transform.position = center + Vector3.down * 0.03f;
            frame.transform.localScale = new Vector3(model.W + 0.1f, PanelHeight - 0.06f, model.H + 0.1f);

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Panel";
            panel.transform.SetParent(root.transform, false);
            Destroy(panel.GetComponent<Collider>());
            panel.GetComponent<MeshRenderer>().sharedMaterial = ViewKit.CurtainPanel;
            panel.transform.position = center;
            panel.transform.localScale = new Vector3(model.W - 0.04f, PanelHeight, model.H - 0.04f);

            var view = root.AddComponent<CurtainView>();
            view._model = model;
            view._counter = ViewKit.CreateCounter(
                root.transform, center + Vector3.up * (PanelHeight * 0.5f + 0.12f), model.Count);
            view._counter.color = new Color(1f, 0.9f, 0.55f); // altın rozet hissi

            return view;
        }

        public void UpdateCount()
        {
            if (_counter != null)
                _counter.text = _model.Count.ToString();
        }

        public void Open() => Destroy(gameObject);
    }
}
