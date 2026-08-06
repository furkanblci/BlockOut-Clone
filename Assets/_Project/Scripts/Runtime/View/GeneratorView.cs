using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Blok üretecinin görseli: tahtanın kenarına yapışık bir makine gövdesi,
    /// üstünde kalan blok sayısı, ortasında SIRADAKİ bloğun rengini gösteren
    /// bir pencere.
    ///
    /// DERS (bilgiyi nesnenin üstünde göster): Oyuncunun "bu makineden daha
    /// kaç blok gelecek ve sıradaki ne renk" sorusuna bakmadan cevap verebilmesi
    /// gerekir; aksi halde yer açma kararını körlemesine verir. Referans oyun da
    /// bu iki bilgiyi makinenin üstüne basıyor.
    /// </summary>
    public sealed class GeneratorView : MonoBehaviour
    {
        const float Depth = 0.55f;   // kenardan dışarı taşma
        const float Body = 0.86f;    // kenar boyunca kalınlık

        GeneratorModel _model;
        TextMesh _counter;
        MeshRenderer _window;

        public static GeneratorView Create(
            Transform parent, GeneratorModel model, BoardSpace space, Material nextMaterial)
        {
            var go = new GameObject($"Generator_{model.Side}_{model.X}_{model.Y}");
            go.transform.SetParent(parent, worldPositionStays: false);

            var view = go.AddComponent<GeneratorView>();
            view._model = model;

            bool horizontalEdge = model.Side == Side.North || model.Side == Side.South;

            // Gövde: kenarın dışında duran kutu.
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(go.transform, worldPositionStays: false);
            body.GetComponent<MeshRenderer>().sharedMaterial = ViewKit.GeneratorBody;
            Kill(body.GetComponent<Collider>());

            // Pencere: sıradaki bloğun rengi.
            var window = GameObject.CreatePrimitive(PrimitiveType.Cube);
            window.name = "Window";
            window.transform.SetParent(go.transform, worldPositionStays: false);
            view._window = window.GetComponent<MeshRenderer>();
            view._window.sharedMaterial = nextMaterial;
            Kill(window.GetComponent<Collider>());

            Vector3 center = view.WorldCenter(space, horizontalEdge);
            go.transform.position = center;

            body.transform.localScale = horizontalEdge
                ? new Vector3(Body, 0.34f, Depth)
                : new Vector3(Depth, 0.34f, Body);
            body.transform.localPosition = Vector3.zero;

            window.transform.localScale = new Vector3(0.4f, 0.36f, 0.4f);
            window.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            // Sayaç makinenin DIŞ kenarına konur: kamera tepeden baktığı için
            // "yukarı" kaydırmak onu pencerenin üstüne bindiriyordu.
            view._counter = ViewKit.CreateCounter(
                parent, center + view.Outward() * 0.42f + Vector3.up * 0.3f, model.Remaining);

            return view;
        }

        /// <summary>Tahtadan dışa bakan yön — sayaç bu tarafa kaydırılır.</summary>
        Vector3 Outward()
        {
            switch (_model.Side)
            {
                case Side.West:  return Vector3.left;
                case Side.East:  return Vector3.right;
                case Side.North: return Vector3.forward;
                default:         return Vector3.back;
            }
        }

        /// <summary>Edit modunda Destroy yasak; araç/doğrulama yollarında da çalışsın.</summary>
        static void Kill(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        Vector3 WorldCenter(BoardSpace space, bool horizontalEdge)
        {
            // Makine kenarın DIŞINDA durur; girişin tam karşısına hizalanır.
            float half = Depth * 0.5f + 0.18f;
            switch (_model.Side)
            {
                case Side.West:  return space.CornerToWorld(0f, _model.Y + 0.5f, 0.18f) + Vector3.left * half;
                case Side.East:  return space.CornerToWorld(space.Width, _model.Y + 0.5f, 0.18f) + Vector3.right * half;
                case Side.North: return space.CornerToWorld(_model.X + 0.5f, 0f, 0.18f) + Vector3.forward * half;
                default:         return space.CornerToWorld(_model.X + 0.5f, space.Height, 0.18f) + Vector3.back * half;
            }
        }

        /// <summary>Bir blok itildi: sayaç ve pencere tazelenir, sıra bitince makine gider.</summary>
        public void UpdateQueue()
        {
            if (_counter != null) _counter.text = _model.Remaining.ToString();

            if (!_model.IsEmpty) return;

            if (_counter != null) Kill(_counter.gameObject);
            Kill(gameObject);
        }
    }
}
