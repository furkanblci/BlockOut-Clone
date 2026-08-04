using BlockOut.Runtime.Config;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Levels klasörüne giren her JSON, içe aktarılırken otomatik denetlenir.
    ///
    /// DERS (AssetPostprocessor): Unity'nin import hattına takılıp "bu dosya
    /// projeye girdi" anını yakalayabilirsin. Stüdyolarda içerik doğrulaması
    /// buraya bağlanır: bozuk asset daha repoya girer girmez konsola düşer,
    /// kimse aylar sonra "bu bölüm neden çökmüş" diye aramaz.
    ///
    /// Hatalar tıklanabilir (context = asset) olduğu için doğrudan dosyaya gider.
    /// </summary>
    public sealed class LevelAssetValidator : AssetPostprocessor
    {
        const string LevelDir = "Assets/_Project/Levels";

        static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            ColorPaletteSO palette = null;
            GameConfigSO config = null;

            foreach (var path in imported)
            {
                if (!path.EndsWith(".json")) continue;
                if (!path.Replace('\\', '/').StartsWith(LevelDir)) continue;
                if (path.Contains("__playtest")) continue; // geçici test dosyası

                // Config'ler ilk gerçek level'da yüklenir; hiç level yoksa hiç yüklenmez.
                if (palette == null)
                {
                    palette = AssetDatabase.LoadAssetAtPath<ColorPaletteSO>(
                        "Assets/_Project/ScriptableObjects/ColorPalette.asset");
                    config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(
                        "Assets/_Project/ScriptableObjects/GameConfig.asset");
                    if (palette == null || config == null) return; // kurulum henüz tamam değil
                }

                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null) continue;

                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                LevelReport report;
                try
                {
                    report = LevelValidationTool.Validate(asset.text, palette, config);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Level import] {name}: denetim çöktü — {e.Message}", asset);
                    continue;
                }

                foreach (var error in report.Errors)
                    Debug.LogError($"[Level import] {name}: {error}", asset);
                foreach (var warning in report.Warnings)
                    Debug.LogWarning($"[Level import] {name}: {warning}", asset);
            }
        }
    }
}
