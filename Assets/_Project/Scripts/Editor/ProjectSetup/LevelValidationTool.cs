using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Level;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>Bir bölümün denetim sonucu: hatalar, tasarım uyarıları ve ölçümler.</summary>
    public sealed class LevelReport
    {
        public bool Ok;
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
        public LevelSolver.Result Solution;

        public readonly Dictionary<BlockColor, int> BlockCounts = new Dictionary<BlockColor, int>();
        public readonly Dictionary<BlockColor, int> GateCounts = new Dictionary<BlockColor, int>();
        public int EstimatedSeconds;

        public IEnumerable<string> AllMessages
        {
            get
            {
                foreach (var e in Errors) yield return "HATA: " + e;
                foreach (var w in Warnings) yield return w;
            }
        }
    }

    /// <summary>
    /// Bölümleri şema + oynanabilirlik + tasarım açısından denetler.
    ///
    /// Stüdyo alışkanlığı: içerik doğrulaması ELLE test edilmez, araç koşar.
    /// Aynı kod üç yerden çağrılır: menü (toplu), level editörü (canlı),
    /// CI (batch modda çıkış koduyla).
    /// </summary>
    public static class LevelValidationTool
    {
        const string LevelDir = "Assets/_Project/Levels";

        // Süre tahmini modeli: oyuncu önce tahtayı okur, sonra her hamle için
        // düşünüp sürükler. Çözücünün hamle sayısı ALT sınırdır — gerçek oyuncu
        // deneme yanılma da yapar, o yüzden hamle başına cömert bir katsayı.
        const float ReadSeconds = 15f;
        const float SecondsPerMove = 8f;

        [MenuItem("Tools/Block Out/Tüm Bölümleri Doğrula")]
        public static void ValidateAll() => RunAll(logToConsole: true);

        /// <summary>
        /// CI girişi: `Unity -batchmode -executeMethod
        /// BlockOut.Editor.ProjectSetup.LevelValidationTool.ValidateAllBatch`
        /// Bozuk bölüm varsa 1 ile çıkar; böylece PR kırmızı olur.
        /// </summary>
        public static void ValidateAllBatch()
        {
            bool ok = RunAll(logToConsole: true);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool RunAll(bool logToConsole)
        {
            var palette = AssetDatabase.LoadAssetAtPath<ColorPaletteSO>(
                "Assets/_Project/ScriptableObjects/ColorPalette.asset");
            var config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(
                "Assets/_Project/ScriptableObjects/GameConfig.asset");
            if (palette == null || config == null)
            {
                Debug.LogError("[Validate] ColorPalette / GameConfig asset'i bulunamadı.");
                return false;
            }

            int ok = 0, failed = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:TextAsset", new[] { LevelDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".json") || path.Contains("__playtest")) continue;

                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                var report = Validate(asset.text, palette, config);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);

                if (report.Ok)
                {
                    ok++;
                    if (logToConsole)
                        Debug.Log($"[Validate] {name}: OK — {report.Solution.Moves.Count} hamle, " +
                                  $"zorunlu {report.Solution.ForcedSteps}, " +
                                  $"ort. seçenek {report.Solution.AverageOptions:0.0}", asset);
                }
                else
                {
                    failed++;
                    foreach (var error in report.Errors)
                        Debug.LogError($"[Validate] {name}: {error}", asset);
                }

                if (logToConsole)
                    foreach (var warning in report.Warnings)
                        Debug.LogWarning($"[Validate] {name}: {warning}", asset);
            }

            string summary = $"[Validate] {ok} bölüm geçti, {failed} bölüm sorunlu.";
            if (failed > 0) Debug.LogError(summary); else Debug.Log(summary);
            return failed == 0;
        }

        /// <summary>JSON metninden doğrudan denetim (import ve CI yolu).</summary>
        public static LevelReport Validate(string json, ColorPaletteSO palette, GameConfigSO config)
        {
            var report = new LevelReport();
            LevelData data;
            try
            {
                data = LevelLoader.Parse(json);
            }
            catch (System.Exception e)
            {
                report.Errors.Add("parse edilemedi — " + e.Message);
                return report;
            }
            return ValidateData(data, palette, config);
        }

        /// <summary>
        /// Bellekteki bir bölümü denetler. Level editörü bunu her değişiklikten
        /// sonra çağırır (dosyaya kaydetmeden canlı rapor).
        /// </summary>
        public static LevelReport ValidateData(
            LevelData data, ColorPaletteSO palette, GameConfigSO config)
        {
            var report = new LevelReport();

            if (LevelMigration.IsFromFuture(data))
            {
                report.Errors.Add($"Bölüm sürümü {data.Version}, bu yapı en fazla " +
                                  $"{LevelMigration.CurrentVersion} destekliyor.");
                return report;
            }
            LevelMigration.Upgrade(data, null);

            if (!LevelLoader.Validate(data, report.Errors))
                return report;

            CollectColorSummary(data, report);

            LevelModel level;
            try
            {
                level = LevelModel.Build(data);
            }
            catch (System.Exception e)
            {
                report.Errors.Add("Model kurulamadı: " + e.Message);
                return report;
            }

            // GÖRSELSİZ kurulum: sistemler boş BoardViews ile çalışır (view
            // sözlükleri boş kalır, TryGetValue sessizce atlar) — edit modunda
            // sahne nesnesi yaratmadığımız için hem hızlı hem güvenli.
            var space = new BoardSpace(data.Board.Width, data.Board.Height);
            var views = new BoardViews();
            var events = new BoardEvents();
            var obstacles = new ObstacleSystem(level, views, palette, events, space);
            var gates = new GateSystem(level, views, config, events, obstacles, palette);
            gates.RecomputeGateStates();

            report.Solution = LevelSolver.Solve(
                level,
                block => gates.CanResolve(block),
                block => gates.ResolveContact(block).ToString(),
                config.dragSubstep, config.collisionEpsilon);

            if (!report.Solution.Solved)
            {
                // Bütçe dolduysa bu bir HATA değil UYARIDIR: çözücü pes etti,
                // bölüm gerçekten kilitli olmayabilir. Yanlış alarmla tasarımı
                // bozmaktansa "emin değilim" demek doğrusu.
                string detail =
                    $"{report.Solution.RemainingBlocks} blok kaldı" +
                    (report.Solution.PendingCurtainContent ? ", perde açılmadı" : "") +
                    $" (yapılan hamle: {report.Solution.Moves.Count}, " +
                    $"kaydırma: {report.Solution.ShuffleSteps})";

                if (report.Solution.Inconclusive)
                {
                    report.Warnings.Add("ÇÖZÜCÜ PES ETTİ (arama bütçesi doldu) — " + detail +
                                        ". Elle test gerekir.");
                    AddDesignWarnings(data, report);
                    report.Ok = true;
                    return report;
                }

                report.Errors.Add("ÇÖZÜLEMEZ — " + detail);
                return report;
            }

            AddDesignWarnings(data, report);
            report.Ok = true;
            return report;
        }

        static void CollectColorSummary(LevelData data, LevelReport report)
        {
            void CountBlock(BlockData block)
            {
                foreach (var id in block.Layers)
                    if (BlockColorUtil.TryParse(id, out var color))
                    {
                        report.BlockCounts.TryGetValue(color, out int count);
                        report.BlockCounts[color] = count + 1;
                    }
            }

            foreach (var block in data.Blocks) CountBlock(block);

            // Perde içindeki gizli bloklar da renk bütçesine dahildir.
            foreach (var obstacle in data.Obstacles)
            {
                if (obstacle.Type != "curtain" || obstacle.Extra == null) continue;
                if (!obstacle.Extra.TryGetValue("contents", out var token)) continue;
                var hidden = token.ToObject<List<BlockData>>();
                if (hidden == null) continue;
                foreach (var block in hidden) CountBlock(block);
            }

            foreach (var gate in data.Gates)
                foreach (var id in gate.Colors)
                    if (BlockColorUtil.TryParse(id, out var color))
                    {
                        report.GateCounts.TryGetValue(color, out int count);
                        report.GateCounts[color] = count + 1;
                    }
        }

        /// <summary>Bölüm çalışıyor ama İYİ mi? Tasarımcıya yönelik uyarılar.</summary>
        static void AddDesignWarnings(LevelData data, LevelReport report)
        {
            foreach (var pair in report.BlockCounts)
                if (!report.GateCounts.ContainsKey(pair.Key))
                    report.Warnings.Add($"'{pair.Key}' renginde {pair.Value} katman var ama kapısı yok.");

            foreach (var pair in report.GateCounts)
                if (!report.BlockCounts.ContainsKey(pair.Key))
                    report.Warnings.Add($"'{pair.Key}' kapısı var ama o renkte blok yok (kapı baştan ghost olur).");

            var solution = report.Solution;
            report.EstimatedSeconds = Mathf.CeilToInt(ReadSeconds + solution.Moves.Count * SecondsPerMove);

            if (data.TimeSeconds < report.EstimatedSeconds * 1.2f)
                report.Warnings.Add($"Süre dar olabilir: tahmini çözüm ~{report.EstimatedSeconds} sn, " +
                                    $"verilen süre {data.TimeSeconds} sn.");
            // Öğretici bölümlerde bol süre KASITLIDIR (oyuncu baskı hissetmesin),
            // o yüzden alt sınır 90 sn: kısa bölümler boşuna uyarı vermez.
            else if (data.TimeSeconds > Mathf.Max(90f, report.EstimatedSeconds * 4f))
                report.Warnings.Add($"Süre çok bol: tahmini çözüm ~{report.EstimatedSeconds} sn, " +
                                    $"verilen süre {data.TimeSeconds} sn.");

            if (solution.Moves.Count > 0 && solution.InitialOptions == 1)
                report.Warnings.Add("Açılışta tek hamle var — oyuncu deneme yapamadan doğru yolu bulmalı.");

            if (solution.Moves.Count >= 4 &&
                solution.ForcedSteps >= solution.Moves.Count * 0.8f)
                report.Warnings.Add($"Bölüm çok dar: {solution.Moves.Count} hamlenin " +
                                    $"{solution.ForcedSteps} tanesi zorunlu (tek çözüm sırası).");

            if (solution.AverageOptions > 4.5f)
                report.Warnings.Add($"Bölüm çok serbest (ort. {solution.AverageOptions:0.0} seçenek) — " +
                                    "bulmaca hissi zayıf olabilir.");
        }
    }
}
