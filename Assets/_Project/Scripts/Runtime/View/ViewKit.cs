using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// M2 geçici görsel gereçleri: çalışma anında üretilen paylaşımlı
    /// materyaller (buz, perde, ghost) ve sayaç yazıları. Hepsi placeholder —
    /// M4'te asset tabanlı gerçek görsellerle değiştirilecek.
    ///
    /// Statik önbellek bilinçli: materyaller sahne yeniden kurulunca da aynı
    /// kalır (paylaşım = SRP Batcher dostu), domain reload'da sıfırlanır.
    /// </summary>
    public static class ViewKit
    {
        static Material _ice;
        static Material _curtainPanel;
        static Material _curtainFrame;
        static Font _counterFont;
        static readonly Dictionary<BlockColor, Material> _ghosts =
            new Dictionary<BlockColor, Material>();

        /// <summary>Yarı saydam buz. Sprites/Default URP'de SRPDefaultUnlit yolundan çizilir —
        /// kod tarafında güvenilir tek saydam yerleşik şu an bu (M4'te asset olacak).</summary>
        public static Material Ice
        {
            get
            {
                if (_ice == null)
                {
                    _ice = new Material(Shader.Find("Sprites/Default")) { name = "Ice_TEMP" };
                    _ice.color = new Color(0.62f, 0.85f, 1f, 0.55f);
                }
                return _ice;
            }
        }

        static Material _arrow;

        /// <summary>Kapı oklarının düz beyaz materyali.</summary>
        public static Material ArrowMaterial
        {
            get
            {
                if (_arrow == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit")
                                 ?? Shader.Find("Sprites/Default");
                    _arrow = new Material(shader) { name = "GateArrow" };
                    _arrow.SetColor("_BaseColor", new Color(1f, 0.98f, 0.94f));
                    _arrow.color = new Color(1f, 0.98f, 0.94f);
                }
                return _arrow;
            }
        }

        static Material _particle;

        /// <summary>
        /// Parçacık materyali: vertex rengini olduğu gibi gösteren unlit yol.
        /// ParticleSystem her parçacığın rengini vertex rengiyle taşıdığı için
        /// tek materyal tüm renkler için yeterlidir.
        /// </summary>
        public static Material ParticleMaterial
        {
            get
            {
                if (_particle == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                 ?? Shader.Find("Sprites/Default");
                    _particle = new Material(shader) { name = "Crumb_TEMP" };
                }
                return _particle;
            }
        }

        public static Material CurtainPanel
        {
            get
            {
                if (_curtainPanel == null)
                    _curtainPanel = MakeLit("CurtainPanel_TEMP", new Color(0.22f, 0.16f, 0.38f));
                return _curtainPanel;
            }
        }

        public static Material CurtainFrame
        {
            get
            {
                if (_curtainFrame == null)
                    _curtainFrame = MakeLit("CurtainFrame_TEMP", new Color(0.85f, 0.65f, 0.2f));
                return _curtainFrame;
            }
        }

        /// <summary>Rengi tükenen kapının soluk hali — renk başına tek paylaşımlı materyal.</summary>
        public static Material GhostFor(ColorPaletteSO palette, BlockColor color)
        {
            if (_ghosts.TryGetValue(color, out var mat)) return mat;

            var entry = palette.Get(color);
            Color baseColor = entry != null ? entry.uiColor : Color.gray;
            Color faded = Color.Lerp(baseColor, new Color(0.35f, 0.33f, 0.45f), 0.65f);
            mat = MakeLit($"Ghost_{color}", faded);
            _ghosts[color] = mat;
            return mat;
        }

        static Font CounterFont
        {
            get
            {
                if (_counterFont == null)
                    _counterFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _counterFont;
            }
        }

        /// <summary>
        /// Zemine yatık, yukarı bakan sayaç yazısı (buz/perde sayaçları).
        /// TextMesh geçici çözüm — TMP entegrasyonu M4'te. Ana nesnenin ÇOCUĞU
        /// yapılmaz: blokların eşit olmayan ölçeği yazıyı da eziyordu; bunun
        /// yerine dünya konumuna bağımsız yerleştirilir.
        /// </summary>
        public static TextMesh CreateCounter(Transform parent, Vector3 worldPos, int value)
        {
            var go = new GameObject("Counter");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(90f, 0f, 0f));
            go.transform.localScale = Vector3.one * 0.35f;

            var text = go.AddComponent<TextMesh>();
            text.font = CounterFont;
            text.GetComponent<MeshRenderer>().sharedMaterial = CounterFont.material;
            text.fontSize = 48;
            text.characterSize = 0.1f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(0.15f, 0.25f, 0.45f);
            text.text = value.ToString();
            return text;
        }

        static Material MakeLit(string name, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", color);
            return mat;
        }
    }
}
