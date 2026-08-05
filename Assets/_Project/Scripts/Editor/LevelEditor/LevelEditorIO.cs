using System.Collections.Generic;
using System.IO;
using BlockOut.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Editörün disk ve oyun ile teması: JSON okuma/yazma, yeni bölüm şablonu,
    /// perde (curtain) yardımcıları ve Play Test.
    ///
    /// DERS (editör ↔ runtime veri paylaşımı): Editör kendi veri modelini
    /// UYDURMAZ — oyunun yüklediği `LevelData` DTO'larını doğrudan düzenler.
    /// Böylece "editörde çalışıyor ama oyunda bozuk" sınıfı hatalar
    /// yapısal olarak imkânsız hale gelir.
    /// </summary>
    public static class LevelEditorIO
    {
        public const string LevelDir = "Assets/_Project/Levels";
        public const string PlaytestPath = LevelDir + "/__playtest.json";
        public const string PlaytestKey = "BlockOut.PlaytestLevel";
        const string GameplayScene = "Assets/_Project/Scenes/Gameplay.unity";

        public static LevelData NewLevel(int width = 6, int height = 8)
        {
            var data = new LevelData
            {
                Id = "level_new",
                DisplayNumber = 1,
                Difficulty = "normal",
                TimeSeconds = 120,
                Board = new BoardData { Width = width, Height = height }
            };
            for (int y = 0; y < height; y++)
                data.Board.Rows.Add(new string('X', width));
            return data;
        }

        public static string ToJson(LevelData data) =>
            JsonConvert.SerializeObject(data, Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

        /// <summary>
        /// JSON → LevelData; okurken şema göçünü de uygular. Böylece eski
        /// dosyalar editörde açıldığı anda güncel şemaya taşınır.
        /// </summary>
        public static LevelData FromJson(string json)
        {
            var data = JsonConvert.DeserializeObject<LevelData>(json);
            if (data == null) throw new JsonException("Level JSON çözümlenemedi.");
            if (LevelMigration.IsFromFuture(data))
                throw new JsonException($"Bu bölüm sürüm {data.Version} ile yazılmış; " +
                                        $"araç en fazla {LevelMigration.CurrentVersion} destekliyor.");
            LevelMigration.Upgrade(data, null);

            // Elle yazılmış dosyalarda "cells" ile w/h çelişebilir; şeklin tek
            // gerçeği maske, w/h ondan tazelenir.
            foreach (var block in data.Blocks) BlockShape.Normalize(block);
            foreach (var obstacle in data.Obstacles)
            {
                var contents = GetContents(obstacle);
                if (contents.Count == 0) continue;
                foreach (var block in contents) BlockShape.Normalize(block);
                SetContents(obstacle, contents);
            }
            return data;
        }

        /// <summary>
        /// Levels klasöründeki en büyük numaradan sonraki dosya yolunu üretir
        /// (level_003 varsa level_004). "Sonraki bölüm olarak kaydet" için.
        /// </summary>
        public static string NextLevelPath(out int number)
        {
            number = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LevelDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!name.StartsWith("level_")) continue;
                if (int.TryParse(name.Substring(6), out int value) && value > number)
                    number = value;
            }
            number++;
            return $"{LevelDir}/level_{number:000}.json";
        }

        // ---------- otomatik kurtarma ----------
        // DERS: Domain reload'ı [SerializeField] ile atlatıyoruz ama Unity
        // ÇÖKERSE o da gider. Çalışma kopyasını proje dışına (Library) yazmak
        // ucuz bir sigortadır: repoyu kirletmez, sonraki açılışta sorulur.

        static string RecoveryPath =>
            System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath) ?? ".",
                "Library", "BlockOut_LevelRecovery.json");

        public static void WriteRecovery(LevelData data)
        {
            try { File.WriteAllText(RecoveryPath, ToJson(data)); }
            catch { /* kurtarma yazılamadıysa sessiz geç — asıl iş engellenmemeli */ }
        }

        public static void ClearRecovery()
        {
            try { if (File.Exists(RecoveryPath)) File.Delete(RecoveryPath); }
            catch { }
        }

        public static bool TryReadRecovery(out LevelData data)
        {
            data = null;
            try
            {
                if (!File.Exists(RecoveryPath)) return false;
                data = FromJson(File.ReadAllText(RecoveryPath));
                return data != null;
            }
            catch { return false; }
        }

        /// <summary>Kaydeder ve asset veritabanına bildirir. Yol proje içindeyse import edilir.</summary>
        public static void Save(LevelData data, string assetPath)
        {
            File.WriteAllText(assetPath, ToJson(data));
            AssetDatabase.ImportAsset(assetPath);
        }

        public static string AskSavePath(string suggestedName)
        {
            string abs = EditorUtility.SaveFilePanel(
                "Bölümü Kaydet", LevelDir, suggestedName + ".json", "json");
            return string.IsNullOrEmpty(abs) ? null : ToProjectPath(abs);
        }

        public static string AskLoadPath()
        {
            string abs = EditorUtility.OpenFilePanel("Bölüm Aç", LevelDir, "json");
            return string.IsNullOrEmpty(abs) ? null : ToProjectPath(abs);
        }

        static string ToProjectPath(string absolute)
        {
            absolute = absolute.Replace('\\', '/');
            string root = Application.dataPath.Replace('\\', '/');
            return absolute.StartsWith(root) ? "Assets" + absolute.Substring(root.Length) : absolute;
        }

        // ---------- Play Test ----------

        /// <summary>
        /// Düzenlenen bölümü geçici dosyaya yazar, SessionState'e işaretler ve
        /// Gameplay sahnesinde Play'e girer. GameSession bu işareti görürse
        /// kendi level dizisi yerine test bölümünü yükler.
        ///
        /// SessionState seçildi çünkü domain reload'ı (Play'e giriş) hayatta
        /// kalır ama editör kapanınca temizlenir — kalıcı ayar kirliliği olmaz.
        /// </summary>
        public static void PlayTest(LevelData data)
        {
            Save(data, PlaytestPath);
            SessionState.SetString(PlaytestKey, PlaytestPath);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        /// <summary>Play modundan çıkınca test işaretini temizler — sonraki Play normal akışa döner.</summary>
        [InitializeOnLoadMethod]
        static void HookPlayModeExit()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    SessionState.EraseString(PlaytestKey);
            };
        }

        // ---------- perde (curtain) yardımcıları ----------
        // ObstacleData polimorfiktir: tür-özel alanlar Extra sözlüğünde ham
        // JSON olarak durur. Editör bu alanları okurken/yazarken tek yerden geçer.

        public static ObstacleData NewCurtain(int x, int y, int w, int h, int count)
        {
            var data = new ObstacleData
            {
                Type = "curtain",
                Extra = new Dictionary<string, JToken>()
            };
            SetInt(data, "x", x); SetInt(data, "y", y);
            SetInt(data, "w", w); SetInt(data, "h", h);
            SetInt(data, "count", count);
            data.Extra["contents"] = new JArray();
            return data;
        }

        public static int GetInt(ObstacleData data, string key, int fallback = 0) =>
            data.Extra != null && data.Extra.TryGetValue(key, out var token)
                ? token.Value<int>() : fallback;

        public static void SetInt(ObstacleData data, string key, int value)
        {
            data.Extra ??= new Dictionary<string, JToken>();
            data.Extra[key] = value;
        }

        public static List<BlockData> GetContents(ObstacleData data)
        {
            if (data.Extra != null && data.Extra.TryGetValue("contents", out var token))
                return token.ToObject<List<BlockData>>() ?? new List<BlockData>();
            return new List<BlockData>();
        }

        public static void SetContents(ObstacleData data, List<BlockData> blocks)
        {
            data.Extra ??= new Dictionary<string, JToken>();
            data.Extra["contents"] = JArray.FromObject(blocks);
        }
    }
}
