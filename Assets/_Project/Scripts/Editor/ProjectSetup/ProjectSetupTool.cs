using BlockOut.Core;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Input;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// M0 kurulum aracı: config asset'lerini ve Gameplay sahnesini tek tıkla üretir.
    ///
    /// DERS (Editor scripting): Stüdyolarda "elle 12 adım" yerine böyle menü
    /// komutları yazılır; kurulum tekrarlanabilir ve ekipteki herkeste aynı olur.
    /// [MenuItem] bir statik metodu Unity menüsüne bağlar. Bu kod BlockOut.Editor
    /// asmdef'inde olduğu için build'e ASLA girmez.
    /// </summary>
    public static class ProjectSetupTool
    {
        const string SoDir = "Assets/_Project/ScriptableObjects";
        const string ScenePath = "Assets/_Project/Scenes/Gameplay.unity";

        [MenuItem("Tools/Block Out/1. Config Asset'lerini Oluştur")]
        public static void CreateConfigAssets()
        {
            CreateAssetIfMissing<GameConfigSO>($"{SoDir}/GameConfig.asset");

            var palette = CreateAssetIfMissing<ColorPaletteSO>($"{SoDir}/ColorPalette.asset", created =>
            {
                // Videodan göz kararı alınan başlangıç paleti — M4'te orijinale
                // yaklaştırılacak. Materyaller M1'de üretilip buraya bağlanacak.
                created.EditorSetEntries(new[]
                {
                    Entry(BlockColor.Red,    new Color(0.90f, 0.15f, 0.20f)),
                    Entry(BlockColor.Blue,   new Color(0.15f, 0.45f, 0.95f)),
                    Entry(BlockColor.Yellow, new Color(1.00f, 0.75f, 0.10f)),
                    Entry(BlockColor.Green,  new Color(0.20f, 0.75f, 0.25f)),
                    Entry(BlockColor.White,  new Color(0.93f, 0.91f, 0.88f)),
                    Entry(BlockColor.Black,  new Color(0.16f, 0.16f, 0.18f)),
                    Entry(BlockColor.Pink,   new Color(0.95f, 0.25f, 0.65f)),
                    Entry(BlockColor.Orange, new Color(1.00f, 0.55f, 0.10f))
                });
                EditorUtility.SetDirty(created);
            });

            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(palette);
            Debug.Log("[Setup] Config asset'leri hazır: " + SoDir);
        }

        [MenuItem("Tools/Block Out/2. Gameplay Sahnesini Kur")]
        public static void SetupGameplayScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Kamera: dikey telefon görünümü, tahtaya yukarıdan hafif eğik bakış ---
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.13f, 0.10f, 0.25f); // koyu mor zemin
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 60f;
            camGo.transform.SetPositionAndRotation(
                new Vector3(0f, 14f, -5.5f),
                Quaternion.Euler(68f, 0f, 0f));
            // Not: URP'nin UniversalAdditionalCameraData bileşenini elle eklemiyoruz;
            // URP ihtiyaç duyduğunda kameraya kendisi ekler.

            // --- Işık: gölgesiz tek yönlü ışık (mobil bütçe) ---
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(55f, -25f, 0f);

            // --- Kök nesneler: sistemlerin yaşayacağı iskelet ---
            new GameObject("Board");                       // BoardBuilder buraya kuracak (M1)
            var services = new GameObject("Services");
            services.AddComponent<PointerInputService>();  // M0 doğrulaması: Console'da Down/Up logları

            // --- Zemin referansı: tahta düzlemini görmek için geçici quad ---
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = "BoardPlane_TEMP";
            Object.DestroyImmediate(plane.GetComponent<Collider>()); // fizik kullanmıyoruz!
            plane.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            plane.transform.localScale = new Vector3(6f, 8f, 1f);    // ~6x8 hücrelik alan hissi

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Setup] Gameplay sahnesi kuruldu: " + ScenePath +
                      "\nPlay'e bas, ekrana tıkla → Console'da [Input] Down/Up loglarını gör.");
        }

        // ---- yardımcılar ----

        static T CreateAssetIfMissing<T>(string path, System.Action<T> onCreated = null)
            where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            onCreated?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static ColorPaletteSO.Entry Entry(BlockColor c, Color ui) => new ColorPaletteSO.Entry
        {
            color = c,
            uiColor = ui,
            particleColor = ui
        };
    }
}
