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

        public static LevelData FromJson(string json) =>
            JsonConvert.DeserializeObject<LevelData>(json);

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
