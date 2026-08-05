using System.Collections.Generic;
using BlockOut.Core;
using Newtonsoft.Json;
using UnityEditor;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Bölümler arası kopyala/yapıştır ve yeniden kullanılabilir "damga"lar.
    ///
    /// DERS (içerik hızı): Tasarımcının en sık yaptığı iş, işe yarayan bir
    /// deseni tekrar kurmaktır. Panoyu EditorPrefs'te tutmak panonun Unity
    /// yeniden başlatılsa bile yaşamasını sağlar; damgalar ise adlandırılmış
    /// kalıcı panolardır.
    ///
    /// Bloklar KÖŞEYE GÖRE saklanır (en sol-üst blok 0,0 kabul edilir), böylece
    /// yapıştırma imlecin olduğu yere göreli oturur.
    /// </summary>
    public static class LevelEditorClipboard
    {
        const string ClipboardKey = "BlockOut.LevelClipboard";
        const string StampListKey = "BlockOut.LevelStamps";
        const string StampPrefix = "BlockOut.LevelStamp.";

        public static bool HasContent => !string.IsNullOrEmpty(EditorPrefs.GetString(ClipboardKey, ""));

        public static void Copy(List<BlockData> blocks)
        {
            if (blocks == null || blocks.Count == 0) return;
            EditorPrefs.SetString(ClipboardKey, Serialize(blocks));
        }

        /// <summary>Panodaki blokları verilen hücreye göreli olarak döndürür.</summary>
        public static List<BlockData> Paste(int atX, int atY)
        {
            string json = EditorPrefs.GetString(ClipboardKey, "");
            return string.IsNullOrEmpty(json) ? null : Deserialize(json, atX, atY);
        }

        // ---------- damgalar ----------

        public static string[] StampNames()
        {
            string joined = EditorPrefs.GetString(StampListKey, "");
            return string.IsNullOrEmpty(joined) ? new string[0] : joined.Split('\n');
        }

        public static void SaveStamp(string name, List<BlockData> blocks)
        {
            if (string.IsNullOrWhiteSpace(name) || blocks == null || blocks.Count == 0) return;
            name = name.Trim();

            EditorPrefs.SetString(StampPrefix + name, Serialize(blocks));

            var names = new List<string>(StampNames());
            if (!names.Contains(name)) names.Add(name);
            EditorPrefs.SetString(StampListKey, string.Join("\n", names));
        }

        public static List<BlockData> LoadStamp(string name, int atX, int atY)
        {
            string json = EditorPrefs.GetString(StampPrefix + name, "");
            return string.IsNullOrEmpty(json) ? null : Deserialize(json, atX, atY);
        }

        public static void DeleteStamp(string name)
        {
            EditorPrefs.DeleteKey(StampPrefix + name);
            var names = new List<string>(StampNames());
            names.Remove(name);
            EditorPrefs.SetString(StampListKey, string.Join("\n", names));
        }

        // ---------- ortak ----------

        static string Serialize(List<BlockData> blocks)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var block in blocks)
            {
                if (block.X < minX) minX = block.X;
                if (block.Y < minY) minY = block.Y;
            }

            var normalized = new List<BlockData>(blocks.Count);
            foreach (var block in blocks)
                normalized.Add(new BlockData
                {
                    X = block.X - minX, Y = block.Y - minY,
                    W = block.W, H = block.H, Ice = block.Ice,
                    Cells = block.Cells == null ? null : new List<string>(block.Cells),
                    Layers = new List<string>(block.Layers)
                });

            return JsonConvert.SerializeObject(normalized);
        }

        static List<BlockData> Deserialize(string json, int atX, int atY)
        {
            var blocks = JsonConvert.DeserializeObject<List<BlockData>>(json);
            if (blocks == null) return null;

            foreach (var block in blocks)
            {
                block.X += atX;
                block.Y += atY;
            }
            return blocks;
        }
    }
}
