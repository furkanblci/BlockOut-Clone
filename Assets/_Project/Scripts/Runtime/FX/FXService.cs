using BlockOut.Core;
using BlockOut.Runtime.Board;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.View;
using UnityEngine;

namespace BlockOut.Runtime.FX
{
    /// <summary>
    /// Tahta olaylarını parçacık patlamalarına çevirir: tuğla kırıntısı, buz kırılması.
    ///
    /// DERS (neden ayrı bir havuz YOK?): Object pooling'in amacı sık yaratılıp
    /// yok edilen nesnelerin GC baskısını kaldırmaktır. ParticleSystem'in kendisi
    /// ZATEN bir havuzdur — sabit bir parçacık dizisi tutar ve tekrar kullanır.
    /// Bu yüzden her patlama için ayrı sistem yaratmak yerine TEK sistem kurup
    /// <see cref="ParticleSystem.EmitParams"/> ile her patlamanın rengini ayrı
    /// veriyoruz. Doğru araç zaten havuzluysa üstüne havuz yazmak israftır.
    ///
    /// Servis <see cref="BoardEvents"/>'e abonedir; oyun mantığı FX'in varlığından
    /// habersizdir (M1'de kurduğumuz gevşek bağlılığın karşılığı).
    /// </summary>
    public sealed class FXService : MonoBehaviour
    {
        ParticleSystem _crumbs;
        BoardEvents _events;
        BoardSpace _space;
        ColorPaletteSO _palette;

        public static FXService Create(Transform parent, ColorPaletteSO palette)
        {
            var go = new GameObject("FX");
            go.transform.SetParent(parent, worldPositionStays: false);
            var service = go.AddComponent<FXService>();
            service._palette = palette;
            service._crumbs = service.BuildCrumbSystem(go.transform);
            return service;
        }

        /// <summary>Bölüm yeniden kurulduğunda yeni olay merkezine bağlanır.</summary>
        public void Bind(BoardEvents events, BoardSpace space)
        {
            Unbind();
            _events = events;
            _space = space;

            _events.BlockAbsorbed += OnBlockAbsorbed;
            _events.LayerPeeled += OnLayerPeeled;
            _events.IceShattered += OnIceShattered;
            _events.GateIceShattered += OnGateIceShattered;
            _events.CurtainOpened += OnCurtainOpened;
        }

        void Unbind()
        {
            if (_events == null) return;
            _events.BlockAbsorbed -= OnBlockAbsorbed;
            _events.LayerPeeled -= OnLayerPeeled;
            _events.IceShattered -= OnIceShattered;
            _events.GateIceShattered -= OnGateIceShattered;
            _events.CurtainOpened -= OnCurtainOpened;
            _events = null;
        }

        void OnDestroy() => Unbind();

        // ---------------- olaylar ----------------

        void OnBlockAbsorbed(BlockModel block, GateModel gate)
        {
            Vector3 at = _space.RectCenterToWorld(
                block.Position, block.W, block.H, BrickHeightHalf);
            Burst(at, ColorOf(block.CurrentColor), 14 + block.W * block.H * 4);
        }

        void OnLayerPeeled(BlockModel block, GateModel gate)
        {
            // Soyulan katmanın rengi zaten listeden çıktı; kapının rengi doğru olan.
            Vector3 at = _space.RectCenterToWorld(
                block.Position, block.W, block.H, BrickHeightHalf);
            Burst(at, ColorOf(gate.ActiveColor), 12);
        }

        void OnIceShattered(BlockModel block)
        {
            Vector3 at = _space.RectCenterToWorld(
                block.Position, block.W, block.H, BrickHeightHalf);
            Burst(at, new Color(0.72f, 0.92f, 1f), 20);
        }

        void OnGateIceShattered(GateModel gate)
        {
            float spanCenter = (gate.SpanMin + gate.SpanMax) * 0.5f;
            Vector3 at = gate.EdgeHorizontal
                ? _space.CornerToWorld(spanCenter, gate.EdgeCoord, 0.2f)
                : _space.CornerToWorld(gate.EdgeCoord, spanCenter, 0.2f);
            Burst(at, new Color(0.72f, 0.92f, 1f), 18);
        }

        void OnCurtainOpened(CurtainModel curtain)
        {
            Vector3 at = _space.RectCenterToWorld(
                new Vector2(curtain.X, curtain.Y), curtain.W, curtain.H, 0.3f);
            Burst(at, new Color(1f, 0.85f, 0.35f), 26);
        }

        // ---------------- parçacıklar ----------------

        const float BrickHeightHalf = 0.25f;

        void Burst(Vector3 position, Color color, int count)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
                startColor = color
            };
            _crumbs.Emit(emit, count);
        }

        /// <summary>
        /// Tuğla kırıntısı sistemi: küçük küpler, yukarı saçılıp yerçekimiyle düşer.
        /// Tamamen kodla kurulur — prefab asset'i yok, ayarlar tek yerde.
        /// </summary>
        ParticleSystem BuildCrumbSystem(Transform parent)
        {
            var go = new GameObject("Crumbs");
            go.transform.SetParent(parent, worldPositionStays: false);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop();

            var main = system.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.55f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 2.6f;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.enabled = false; // yalnızca Emit() ile patlatıyoruz

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.28f;

            var sizeOverLife = system.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(
                1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

            var rotationOverLife = system.rotationOverLifetime;
            rotationOverLife.enabled = true;
            rotationOverLife.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            // Kırıntılar kare değil KÜP: tuğladan kopmuş parça hissi verir.
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = CubeMesh();
            renderer.sharedMaterial = ViewKit.ParticleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return system;
        }

        static Mesh _cubeMesh;

        static Mesh CubeMesh()
        {
            if (_cubeMesh != null) return _cubeMesh;

            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(temp);
            return _cubeMesh;
        }

        Color ColorOf(BlockColor color)
        {
            var entry = _palette != null ? _palette.Get(color) : null;
            return entry != null ? entry.particleColor : Color.white;
        }
    }
}
