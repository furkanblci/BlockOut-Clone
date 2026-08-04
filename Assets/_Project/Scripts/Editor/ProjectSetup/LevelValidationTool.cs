using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Level;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Tüm level JSON'larını şema + oynanabilirlik açısından denetler.
    ///
    /// Stüdyo alışkanlığı: içerik doğrulaması ELLE test edilmez, araç koşar.
    /// M3'te level editörü bu aracı canlı uyarı paneli olarak kullanacak;
    /// ileride CI'da da koşturulabilir (bozuk level merge edilemez).
    /// </summary>
    public static class LevelValidationTool
    {
        const string LevelDir = "Assets/_Project/Levels";

        [MenuItem("Tools/Block Out/Tüm Bölümleri Doğrula")]
        public static void ValidateAll()
        {
            var palette = AssetDatabase.LoadAssetAtPath<ColorPaletteSO>(
                "Assets/_Project/ScriptableObjects/ColorPalette.asset");
            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(
                "Assets/_Project/ScriptableObjects/GameConfig.asset");
            if (palette == null || config == null)
            {
                Debug.LogError("[Validate] Config asset'leri bulunamadı.");
                return;
            }

            int ok = 0, failed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LevelDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".json")) continue;

                if (ValidateOne(path, palette, config)) ok++;
                else failed++;
            }

            string summary = $"[Validate] {ok} bölüm geçti, {failed} bölüm sorunlu.";
            if (failed > 0) Debug.LogError(summary);
            else Debug.Log(summary);
        }

        static bool ValidateOne(string path, ColorPaletteSO palette, GameConfigSO config)
        {
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            LevelData data;
            try
            {
                data = LevelLoader.Parse(asset.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Validate] {name}: parse hatası — {e.Message}", asset);
                return false;
            }

            var errors = new List<string>();
            if (!LevelLoader.Validate(data, errors))
            {
                foreach (var err in errors)
                    Debug.LogError($"[Validate] {name}: {err}", asset);
                return false;
            }

            // Oynanabilirlik: GÖRSELSİZ kurulum. Sistemler boş BoardViews ile
            // çalışır (view sözlükleri boş kalır, TryGetValue sessizce atlar) —
            // edit modunda sahne nesnesi yaratmadığımız için hem hızlı hem güvenli.
            var level = LevelModel.Build(data);
            var space = new BoardSpace(data.Board.Width, data.Board.Height);
            var views = new BoardViews();
            var events = new BoardEvents();
            var obstacles = new ObstacleSystem(level, views, palette, events, space);
            var gates = new GateSystem(level, views, config, events, obstacles, palette);
            gates.RecomputeGateStates();

            var result = LevelSolver.Solve(
                level,
                (BlockModel block, out string outcome) =>
                {
                    var contact = gates.ResolveContact(block);
                    outcome = contact.ToString();
                    return contact != GateContactResult.None;
                },
                config.dragSubstep, config.collisionEpsilon);

            if (!result.Solved)
            {
                Debug.LogError(
                    $"[Validate] {name}: ÇÖZÜLEMEZ — {result.RemainingBlocks} blok kaldı" +
                    (result.PendingCurtainContent ? ", perde açılmadı" : "") +
                    $". Yapılabilen hamleler: {result.Moves.Count}", asset);
                return false;
            }

            Debug.Log($"[Validate] {name}: OK ({result.Moves.Count} hamlede çözülebilir)", asset);
            return true;
        }
    }
}
