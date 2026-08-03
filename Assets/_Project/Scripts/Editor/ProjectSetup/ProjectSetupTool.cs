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
    /// Kurulum mantığı: config asset'leri ve Gameplay sahnesi.
    ///
    /// Buradaki metodlar İDEMPOTENT'tir (varsa dokunmaz, yoksa oluşturur) —
    /// bu sayede <see cref="ProjectBootstrap"/> her domain reload'da güvenle
    /// çağırabilir. Menü komutları yalnızca elle tetiklemek isteyenler için.
    ///
    /// DERS (Editor scripting): Sahne, açık sahneyi bozmamak için ADDITIVE
    /// modda arka planda kurulur, kaydedilir ve kapatılır. "Single" mod
    /// kullansaydık kullanıcının o an açık sahnesini kapatırdık.
    /// </summary>
    public static class ProjectSetupTool
    {
        const string SoDir = "Assets/_Project/ScriptableObjects";
        public const string ScenePath = "Assets/_Project/Scenes/Gameplay.unity";

        // ---------- Menü komutları (elle tetikleme) ----------

        [MenuItem("Tools/Block Out/Kurulumu Şimdi Çalıştır")]
        public static void RunSetupNow()
        {
            bool a = EnsureConfigAssets();
            bool b = EnsureGameplayScene();
            Debug.Log(a || b
                ? "[Setup] Eksikler tamamlandı."
                : "[Setup] Her şey zaten kuruluydu, değişiklik yok.");
        }

        // ---------- İdempotent kurulum adımları ----------

        /// <returns>Bir şey oluşturulduysa true.</returns>
        public static bool EnsureConfigAssets()
        {
            bool created = false;

            created |= CreateAssetIfMissing<GameConfigSO>($"{SoDir}/GameConfig.asset") != null;

            created |= CreateAssetIfMissing<ColorPaletteSO>($"{SoDir}/ColorPalette.asset", palette =>
            {
                // Videodan göz kararı alınan başlangıç paleti — M4'te orijinale
                // yaklaştırılacak. Materyaller M1'de üretilip buraya bağlanacak.
                palette.EditorSetEntries(new[]
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
                EditorUtility.SetDirty(palette);
            }) != null;

            if (created) AssetDatabase.SaveAssets();
            return created;
        }

        /// <summary>
        /// Gameplay sahnesi yoksa AÇIK SAHNEYE DOKUNMADAN arka planda oluşturur.
        /// </summary>
        /// <returns>Sahne oluşturulduysa true.</returns>
        public static bool EnsureGameplayScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                return false; // zaten var

            var previousActive = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            // new GameObject() aktif sahneye doğar; bu yüzden geçici olarak
            // yeni sahneyi aktif yapıp işimiz bitince eskisini geri getiriyoruz.
            SceneManager.SetActiveScene(scene);
            try
            {
                PopulateGameplayScene();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (previousActive.IsValid())
                    SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }

            Debug.Log("[Setup] Gameplay sahnesi arka planda kuruldu: " + ScenePath);
            return true;
        }

        // ---------- Sahne içeriği ----------

        static void PopulateGameplayScene()
        {
            // Kamera: dikey telefon görünümü, tahtaya yukarıdan hafif eğik bakış.
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

            // Işık: gölgesiz tek yönlü ışık (mobil bütçe).
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.None;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(55f, -25f, 0f);

            // Kök nesneler: sistemlerin yaşayacağı iskelet.
            new GameObject("Board");                       // BoardBuilder buraya kuracak (M1)
            var services = new GameObject("Services");
            services.AddComponent<PointerInputService>();  // M0 doğrulaması: Console'da Down/Up logları

            // Zemin referansı: tahta düzlemini görmek için geçici quad.
            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = "BoardPlane_TEMP";
            Object.DestroyImmediate(plane.GetComponent<Collider>()); // fizik kullanmıyoruz!
            plane.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
            plane.transform.localScale = new Vector3(6f, 8f, 1f);    // ~6x8 hücrelik alan hissi
        }

        // ---------- Yardımcılar ----------

        static T CreateAssetIfMissing<T>(string path, System.Action<T> onCreated = null)
            where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
                return null; // zaten vardı — "yeni oluşturulmadı" bilgisi için null

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
