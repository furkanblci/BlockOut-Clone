using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Flow;
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
        const string MatDir = "Assets/_Project/Art/Materials";
        const string LevelJsonPath = "Assets/_Project/Levels/level_001.json";
        /// <summary>
        /// Oyun sırası. Levels klasöründeki level_NNN dosyaları numara sırasına
        /// göre dizilir — yeni bölüm eklemek için burayı düzenlemek gerekmez.
        /// (M5'te bu liste LevelDatabaseSO'ya taşınacak.)
        /// </summary>
        const string LevelDir = "Assets/_Project/Levels";

        static string[] LevelSequencePaths()
        {
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LevelDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (path.EndsWith(".json") && name.StartsWith("level_")) paths.Add(path);
            }
            paths.Sort(System.StringComparer.Ordinal);
            return paths.ToArray();
        }
        public const string ScenePath = "Assets/_Project/Scenes/Gameplay.unity";

        // ---------- Menü komutları (elle tetikleme) ----------

        [MenuItem("Tools/Block Out/Kurulumu Şimdi Çalıştır")]
        public static void RunSetupNow()
        {
            bool a = EnsureConfigAssets();
            bool b = EnsureGameplayScene();
            bool c = EnsureBlockMaterials();
            bool d = EnsureGameplayWiring();
            bool e = EnsureBuildScenes();
            Debug.Log(a || b || c || d || e
                ? "[Setup] Eksikler tamamlandı."
                : "[Setup] Her şey zaten kuruluydu, değişiklik yok.");
        }

        // ---------- İdempotent kurulum adımları ----------

        /// <returns>Bir şey oluşturulduysa true.</returns>
        public static bool EnsureConfigAssets()
        {
            bool created = false;

            created |= CreateAssetIfMissing<GameConfigSO>($"{SoDir}/GameConfig.asset") != null;
            created |= CreateAssetIfMissing<BlockVisualConfigSO>(
                $"{SoDir}/BlockVisualConfig.asset") != null;

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

        /// <summary>
        /// 8 blok rengine karşılık 8 paylaşımlı URP materyali üretir ve palet
        /// asset'ine bağlar. Paylaşımlı materyal = SRP Batcher dostu (M0 dersi).
        /// </summary>
        /// <returns>Bir şey üretildi ya da bağlandıysa true.</returns>
        public static bool EnsureBlockMaterials()
        {
            var palette = AssetDatabase.LoadAssetAtPath<ColorPaletteSO>($"{SoDir}/ColorPalette.asset");
            if (palette == null) return false; // EnsureConfigAssets henüz koşmadı

            EnsureFolder(MatDir);
            // M4: bloklar artık prosedürel tuğla mesh'i + vertex AO kullanıyor;
            // materyaller bunu okuyan özel shader'a taşınır.
            var shader = Shader.Find("BlockOut/Brick") ??
                         Shader.Find("Universal Render Pipeline/Lit");
            bool changed = false;

            foreach (var entry in palette.EditorEntries)
            {
                string path = $"{MatDir}/Block_{entry.color}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(shader);
                    mat.SetColor("_BaseColor", entry.uiColor);
                    AssetDatabase.CreateAsset(mat, path);
                    changed = true;
                }
                else if (mat.shader != shader && shader != null)
                {
                    // Shader değiştiyse mevcut asset'i taşı (renk korunur).
                    mat.shader = shader;
                    mat.SetColor("_BaseColor", entry.uiColor);
                    EditorUtility.SetDirty(mat);
                    changed = true;
                }

                // Parlaklık ayarları görsel ayar asset'inden gelir; mevcut
                // materyallerde de güncellenir (shader varsayılanı yalnızca
                // YENİ materyale uygulanır, eskiler serileşmiş değeri taşır).
                var visuals = LoadVisualConfig();
                if (visuals != null && mat.HasProperty("_Specular"))
                {
                    ApplyVisualsTo(mat, visuals);
                    EditorUtility.SetDirty(mat);
                }
                if (entry.blockMaterial != mat)
                {
                    entry.blockMaterial = mat;
                    EditorUtility.SetDirty(palette);
                    changed = true;
                }
            }

            if (changed) AssetDatabase.SaveAssets();
            return changed;
        }

        /// <summary>
        /// Gameplay sahnesine M1 bağlantılarını kurar: Game nesnesi + GameSession
        /// ve serileşen alan referansları. M0 kalıntılarını da temizler
        /// (geçici zemin quad'ı, input konsol logları).
        ///
        /// DERS (SerializedObject): private [SerializeField] alanlara editörden
        /// yazmanın resmi yolu budur — alanı public yapmak yerine Unity'nin
        /// serileştirme katmanından geçilir; undo/dirty işaretleme doğru işler.
        /// </summary>
        /// <returns>Sahnede bir şey değiştiyse true.</returns>
        public static bool EnsureGameplayWiring()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                return false; // sahne henüz yok

            // Play modunda sahne diske YAZILAMAZ (Unity yasaklar) ve zaten
            // yazılmamalı: oynanış sırasındaki geçici durum kalıcı olmamalı.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;

            var scene = SceneManager.GetSceneByPath(ScenePath);
            bool wasOpen = scene.IsValid() && scene.isLoaded;
            if (!wasOpen)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                bool changed = WireGameplayScene(scene);
                if (changed) EditorSceneManager.SaveScene(scene);
                return changed;
            }
            finally
            {
                if (!wasOpen) EditorSceneManager.CloseScene(scene, removeScene: true);
            }
        }

        static bool WireGameplayScene(Scene scene)
        {
            bool changed = false;
            GameObject boardGo = null, servicesGo = null, gameGo = null, tempPlane = null;

            foreach (var root in scene.GetRootGameObjects())
            {
                switch (root.name)
                {
                    case "Board": boardGo = root; break;
                    case "Services": servicesGo = root; break;
                    case "Game": gameGo = root; break;
                    case "BoardPlane_TEMP": tempPlane = root; break;
                }
            }

            GameObject NewRoot(string rootName)
            {
                var go = new GameObject(rootName);
                SceneManager.MoveGameObjectToScene(go, scene);
                changed = true;
                return go;
            }

            if (boardGo == null) boardGo = NewRoot("Board");
            if (servicesGo == null) servicesGo = NewRoot("Services");
            if (gameGo == null) gameGo = NewRoot("Game");

            var inputService = servicesGo.GetComponent<PointerInputService>();
            if (inputService == null)
            {
                inputService = servicesGo.AddComponent<PointerInputService>();
                changed = true;
            }

            var session = gameGo.GetComponent<GameSession>();
            if (session == null)
            {
                session = gameGo.AddComponent<GameSession>();
                changed = true;
            }

            // GameSession'ın private [SerializeField] alanlarını bağla.
            var so = new SerializedObject(session);
            changed |= SetReference(so, "config",
                AssetDatabase.LoadAssetAtPath<GameConfigSO>($"{SoDir}/GameConfig.asset"));
            changed |= SetReference(so, "palette",
                AssetDatabase.LoadAssetAtPath<ColorPaletteSO>($"{SoDir}/ColorPalette.asset"));
            changed |= SetReference(so, "visuals", LoadVisualConfig());
            changed |= SetReference(so, "levelJson",
                AssetDatabase.LoadAssetAtPath<TextAsset>(LevelJsonPath));
            changed |= SetReference(so, "input", inputService);
            changed |= SetReference(so, "boardRoot", boardGo.transform);

            // Level sırası (M2): dizi elemanları SerializedProperty ile bağlanır.
            var seq = so.FindProperty("levelSequence");
            if (seq != null)
            {
                var levelPaths = LevelSequencePaths();
                if (seq.arraySize != levelPaths.Length)
                {
                    seq.arraySize = levelPaths.Length;
                    changed = true;
                }
                for (int i = 0; i < levelPaths.Length; i++)
                {
                    var element = seq.GetArrayElementAtIndex(i);
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(levelPaths[i]);
                    if (element.objectReferenceValue != asset)
                    {
                        element.objectReferenceValue = asset;
                        changed = true;
                    }
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // M0 kalıntıları: geçici zemin görseli ve input konsol logları.
            if (tempPlane != null)
            {
                Object.DestroyImmediate(tempPlane);
                changed = true;
            }
            var inputSo = new SerializedObject(inputService);
            var logProp = inputSo.FindProperty("logEvents");
            if (logProp != null && logProp.boolValue)
            {
                logProp.boolValue = false;
                inputSo.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            return changed;
        }

        static bool SetReference(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"[Setup] GameSession'da '{propertyName}' alanı bulunamadı — alan adı mı değişti?");
                return false;
            }
            if (prop.objectReferenceValue == value) return false;
            prop.objectReferenceValue = value;
            return true;
        }

        public static BlockVisualConfigSO LoadVisualConfig() =>
            AssetDatabase.LoadAssetAtPath<BlockVisualConfigSO>($"{SoDir}/BlockVisualConfig.asset");

        /// <summary>Görsel ayarları 8 blok materyaline yazar (ayar penceresi çağırır).</summary>
        public static void PushVisualsToMaterials(BlockVisualConfigSO visuals)
        {
            if (visuals == null) return;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { MatDir }))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat != null && mat.HasProperty("_Specular"))
                {
                    ApplyVisualsTo(mat, visuals);
                    EditorUtility.SetDirty(mat);
                }
            }
            AssetDatabase.SaveAssets();
        }

        static void ApplyVisualsTo(Material mat, BlockVisualConfigSO visuals)
        {
            mat.SetVector("_LightDir", visuals.lightDirection.normalized);
            mat.SetFloat("_Ambient", visuals.ambient);
            mat.SetFloat("_Specular", visuals.specular);
            mat.SetFloat("_Gloss", visuals.gloss);
            mat.SetFloat("_RimStrength", visuals.rim);
            mat.SetFloat("_Saturation", visuals.saturation);
        }

        /// <summary>
        /// Build sahne listesini Gameplay sahnesine ayarlar. Unity'nin varsayılan
        /// SampleScene'i listede kalırsa cihazda BOŞ EKRAN gelir — bu, mobil
        /// duman testinde en sık düşülen tuzaktır.
        /// </summary>
        public static bool EnsureBuildScenes()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) return false;

            var scenes = EditorBuildSettings.scenes;
            bool correct = scenes.Length == 1 && scenes[0].path == ScenePath && scenes[0].enabled;
            if (correct) return false;

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            return true;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
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
