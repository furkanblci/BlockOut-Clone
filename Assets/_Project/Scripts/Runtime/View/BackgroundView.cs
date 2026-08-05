using BlockOut.Runtime.Config;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Referanstaki arka plan: merkeze doğru açılan koyu mor gradyan.
    ///
    /// DERS (neden kameranın çocuğu?): Arka planı sahneye sabit koyarsak
    /// kamera hareket edince kayar. Kameraya bağlayıp uzak düzleme yerleştirmek
    /// hem her zaman ekranı doldurur hem de tek quad'a mal olur. Gradyan
    /// dokusu kodla üretilir; asset gerekmez.
    /// </summary>
    public sealed class BackgroundView : MonoBehaviour
    {
        static Texture2D _gradient;
        static Material _material;

        public static void Ensure(Camera camera, BlockVisualConfigSO config)
        {
            if (camera == null) return;

            var existing = camera.GetComponentInChildren<BackgroundView>();
            if (existing != null) existing.ApplyColors(config);
            else Create(camera, config);
        }

        static void Create(Camera camera, BlockVisualConfigSO config)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Background";
            go.transform.SetParent(camera.transform, worldPositionStays: false);
            Destroy(go.GetComponent<Collider>());

            var view = go.AddComponent<BackgroundView>();
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Material(config);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // Kameranın uzak düzlemine yakın, görüş açısını tamamen kaplayacak boyutta.
            float distance = Mathf.Min(camera.farClipPlane * 0.8f, 60f);
            float height = 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * Mathf.Max(camera.aspect, 2f);

            go.transform.localPosition = new Vector3(0f, 0f, distance);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(width * 1.2f, height * 1.2f, 1f);

            view.ApplyColors(config);
        }

        void ApplyColors(BlockVisualConfigSO config)
        {
            if (config == null) return;
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null) return;

            renderer.sharedMaterial = Material(config);
            _gradient = BuildGradient(config);
            renderer.sharedMaterial.mainTexture = _gradient;
        }

        static Material Material(BlockVisualConfigSO config)
        {
            if (_material != null) return _material;

            _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = "Background"
            };
            _material.SetColor("_BaseColor", Color.white);
            _material.renderQueue = 1000; // her şeyden ÖNCE çizilsin
            _material.mainTexture = BuildGradient(config);
            return _material;
        }

        static Texture2D BuildGradient(BlockVisualConfigSO config)
        {
            const int size = 128;
            Color inner = config != null ? config.backgroundInner : new Color(0.16f, 0.12f, 0.32f);
            Color outer = config != null ? config.backgroundOuter : new Color(0.08f, 0.06f, 0.18f);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    // Merkezden uzaklaştıkça koyulaşan yumuşak vinyet.
                    float d = Mathf.Clamp01(Mathf.Sqrt(nx * nx + ny * ny) / 1.25f);
                    pixels[y * size + x] = Color.Lerp(inner, outer, d * d);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
