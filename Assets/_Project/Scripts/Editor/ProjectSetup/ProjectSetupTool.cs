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
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        public const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";
        public const string HomeScenePath = "Assets/_Project/Scenes/Home.unity";

        /// <summary>Bölüm kataloğu burada durur; Resources = her sahneden erişilebilir.</summary>
        const string ResourcesDir = "Assets/_Project/Resources";

        // ---------- Menü komutları (elle tetikleme) ----------

        [MenuItem("Tools/Block Out/Kurulumu Şimdi Çalıştır")]
        public static void RunSetupNow()
        {
            bool a = EnsureConfigAssets();
            bool b = EnsureGameplayScene();
            bool c = EnsureBlockMaterials();
            bool d = EnsureGameplayWiring();
            bool e = EnsureLevelCatalog();
            bool f = EnsureMetaScenes();
            bool g = EnsureBuildScenes();
            Debug.Log(a || b || c || d || e || f || g
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
                    Entry(BlockColor.Orange, new Color(1.00f, 0.55f, 0.10f)),
                    Entry(BlockColor.Purple, new Color(0.60f, 0.28f, 0.90f)),
                    Entry(BlockColor.Cyan,   new Color(0.30f, 0.78f, 0.95f))
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
            GameObject tempPlane = null;

            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "BoardPlane_TEMP") tempPlane = root;

            GameObject NewRoot(string rootName)
            {
                var go = new GameObject(rootName);
                SceneManager.MoveGameObjectToScene(go, scene);
                changed = true;
                return go;
            }

            // --- Tek sahnelik iskelet: App / Menu / Gameplay ---
            // Bunlar her zaman kök kalır, kök taraması yeterli.
            GameObject appGo = null, menuGo = null, playGo = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "App") appGo = root;
                else if (root.name == "Menu") menuGo = root;
                else if (root.name == "Gameplay") playGo = root;
            }

            if (appGo == null) appGo = NewRoot("App");
            if (menuGo == null) menuGo = NewRoot("Menu");
            if (playGo == null) playGo = NewRoot("Gameplay");

            // Oynanışa ait üç nesne Gameplay'in altına toplanır ki menüye
            // dönerken tek kökü kapatmak yetsin.
            var boardGo = Adopt(scene, playGo, "Board", ref changed);
            var servicesGo = Adopt(scene, playGo, "Services", ref changed);
            var gameGo = Adopt(scene, playGo, "Game", ref changed);

            changed |= EnsureChild<BlockOut.Runtime.UI.HomeScreen>(menuGo, "Home");
            changed |= EnsureChild<BlockOut.Runtime.UI.MenuShell>(menuGo, "MenuShell");

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

            // AppRoot: menü ve oynanış köklerini tanısın.
            var appRoot = appGo.GetComponent<BlockOut.Runtime.Flow.AppRoot>();
            if (appRoot == null)
            {
                appRoot = appGo.AddComponent<BlockOut.Runtime.Flow.AppRoot>();
                changed = true;
            }
            var appSo = new SerializedObject(appRoot);
            bool appChanged = SetReference(appSo, "menuRoot", menuGo);
            appChanged |= SetReference(appSo, "gameRoot", playGo);
            appChanged |= SetReference(appSo, "session", session);
            if (appChanged) { appSo.ApplyModifiedPropertiesWithoutUndo(); changed = true; }

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
            // Sıra ÖNEMLİ: derleme listesinin ilk sahnesi uygulamanın açılışıdır.
            // Boot en başta, Main hemen ardından — tek sahnelik yapıda hepsi bu.
            var wanted = new List<string>();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath) != null) wanted.Add(BootScenePath);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) wanted.Add(ScenePath);
            if (wanted.Count == 0) return false;

            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == wanted.Count)
            {
                bool same = true;
                for (int i = 0; i < wanted.Count; i++)
                    if (scenes[i].path != wanted[i] || !scenes[i].enabled) { same = false; break; }
                if (same) return false;
            }

            var next = new EditorBuildSettingsScene[wanted.Count];
            for (int i = 0; i < wanted.Count; i++)
                next[i] = new EditorBuildSettingsScene(wanted[i], true);
            EditorBuildSettings.scenes = next;
            return true;
        }

        /// <summary>
        /// Boot ve Home sahnelerini üretir. İkisi de neredeyse boştur — içerikleri
        /// çalışma anında koddan kurulur (UiKit), böylece sahne dosyaları git'te
        /// çakışma üretmez.
        /// </summary>
        public static bool EnsureMetaScenes()
        {
            bool created = false;
            created |= CreateSceneIfMissing(BootScenePath, () =>
            {
                new GameObject("Boot", typeof(BlockOut.Runtime.Flow.BootLoader));
                // Boş sahnede kamera yoksa Unity uyarı basar; ucuz bir tane koyalım.
                var cam = new GameObject("Main Camera", typeof(Camera));
                cam.tag = "MainCamera";
                cam.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
                cam.GetComponent<Camera>().backgroundColor = new Color(0.13f, 0.10f, 0.28f);
            });

            // Home artık ayrı bir sahne DEĞİL: menü de oynanış da Main içinde
            // yaşıyor (bkz. AppRoot). Eski Home.unity varsa kurulum silmez —
            // kullanıcı dosyasını silmek aracın işi değil, uyarı yeter.
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HomeScenePath) != null)
                Debug.LogWarning("[Setup] Artık kullanılmayan Home.unity duruyor; silebilirsin.");

            return created;
        }

        static bool CreateSceneIfMissing(string path, System.Action populate)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) return false;

            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));

            var previousActive = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                populate();
                EditorSceneManager.SaveScene(scene, path);
            }
            finally
            {
                if (previousActive.IsValid()) SceneManager.SetActiveScene(previousActive);
                EditorSceneManager.CloseScene(scene, removeScene: true);
            }

            Debug.Log("[Setup] Sahne kuruldu: " + path);
            return true;
        }

        /// <summary>
        /// Bölüm kataloğunu Levels klasöründen tazeler. Home ekranı ve Gameplay
        /// aynı listeyi buradan okur; yeni bölüm eklemek için kod düzenlemek
        /// gerekmez.
        /// </summary>
        public static bool EnsureLevelCatalog()
        {
            EnsureFolder(ResourcesDir);
            string path = $"{ResourcesDir}/LevelCatalog.asset";

            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogSO>(path);
            bool created = false;
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LevelCatalogSO>();
                AssetDatabase.CreateAsset(catalog, path);
                created = true;
            }

            var paths = LevelSequencePaths();
            var assets = new TextAsset[paths.Length];
            for (int i = 0; i < paths.Length; i++)
                assets[i] = AssetDatabase.LoadAssetAtPath<TextAsset>(paths[i]);

            bool changed = created || catalog.levels == null || catalog.levels.Length != assets.Length;
            if (!changed)
                for (int i = 0; i < assets.Length; i++)
                    if (catalog.levels[i] != assets[i]) { changed = true; break; }

            if (!changed) return false;

            catalog.levels = assets;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Bölüm kataloğu tazelendi: {assets.Length} bölüm.");
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

        /// <summary>
        /// Adı geçen nesneyi SAHNENİN TAMAMINDA arar, tekini bırakıp gerisini siler
        /// ve kalanı verilen kökün altına taşır. Yoksa yeni oluşturur.
        ///
        /// DERS (kurulum aracı yeniden çalıştırılabilir olmalı): Bu arama önce
        /// yalnız KÖKLERE bakıyordu. İlk çalıştırma Board/Services/Game'i
        /// Gameplay altına taşıdığı için ikinci çalıştırma onları "yok" sanıp
        /// yenilerini yaratıyordu — sahnede üç Board, üç Services birikti ve
        /// AppRoot en sonuncuya bağlandığı için oyun boş bir tahtayla açıldı.
        /// Bir kurulum aracı her zaman KENDİ ÇIKTISININ ÜSTÜNE tekrar
        /// çalıştırılabilmeli; aksi hâlde yalnız bir kez doğrudur.
        /// </summary>
        static GameObject Adopt(Scene scene, GameObject parent, string name, ref bool changed)
        {
            var found = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(includeInactive: true))
                    if (t.name == name) found.Add(t.gameObject);

            if (found.Count == 0)
            {
                var created = new GameObject(name);
                SceneManager.MoveGameObjectToScene(created, scene);
                created.transform.SetParent(parent.transform, worldPositionStays: false);
                changed = true;
                return created;
            }

            // Hangisi asıl? En çok bileşeni olan — kopyalar boş kabuk oluyor.
            var keep = found[0];
            foreach (var candidate in found)
                if (candidate.GetComponents<Component>().Length > keep.GetComponents<Component>().Length ||
                    candidate.transform.childCount > keep.transform.childCount)
                    keep = candidate;

            foreach (var duplicate in found)
                if (duplicate != keep)
                {
                    Debug.LogWarning($"[Setup] Yinelenen '{name}' silindi.");
                    Object.DestroyImmediate(duplicate);
                    changed = true;
                }

            if (keep.transform.parent != parent.transform)
            {
                keep.transform.SetParent(parent.transform, worldPositionStays: true);
                changed = true;
            }
            return keep;
        }

        /// <summary>Verilen kökün altında adı geçen çocuğu (bileşeniyle) garanti eder.</summary>
        static bool EnsureChild<T>(GameObject parent, string childName) where T : Component
        {
            foreach (Transform existing in parent.transform)
                if (existing.name == childName)
                {
                    if (existing.GetComponent<T>() != null) return false;
                    existing.gameObject.AddComponent<T>();
                    return true;
                }

            var go = new GameObject(childName, typeof(T));
            go.transform.SetParent(parent.transform, worldPositionStays: false);
            return true;
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
