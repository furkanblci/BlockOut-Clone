using System.Collections.Generic;
using BlockOut.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace BlockOut.Runtime.Level
{
    /// <summary>
    /// JSON metni → doğrulanmış LevelData.
    ///
    /// DERS (parse ve validate'i ayır): Parse yalnızca "geçerli JSON mu ve
    /// şemaya uyuyor mu" der; Validate ise OYUN kurallarını denetler (taşan
    /// blok, üst üste blok, boşluğa kapı...). Ayrım sayesinde level editörü
    /// (M3) aynı Validate'i canlı uyarı panelinde kullanabilecek.
    /// </summary>
    public static class LevelLoader
    {
        public static LevelData Parse(string json)
        {
            var data = JsonConvert.DeserializeObject<LevelData>(json);
            if (data == null || data.Board == null)
                throw new JsonException("Level JSON boş ya da 'board' alanı eksik.");
            return data;
        }

        /// <summary>Kural hatalarını listeye toplar; hata yoksa true.</summary>
        public static bool Validate(LevelData d, List<string> errors)
        {
            int start = errors.Count;
            var board = d.Board;

            if (board.Rows.Count != board.Height)
                errors.Add($"rows sayısı ({board.Rows.Count}) height ({board.Height}) ile uyuşmuyor.");
            else
                for (int y = 0; y < board.Height; y++)
                    if (board.Rows[y].Length != board.Width)
                        errors.Add($"rows[{y}] uzunluğu ({board.Rows[y].Length}) width ({board.Width}) ile uyuşmuyor.");

            if (errors.Count > start)
                return false; // ızgara bozuksa gerisini denetlemek anlamsız

            bool Playable(int x, int y) =>
                x >= 0 && y >= 0 && x < board.Width && y < board.Height &&
                char.ToUpperInvariant(board.Rows[y][x]) == 'X';

            // Bloklar: sınır içinde, oynanabilir hücrelerde ve çakışmasız.
            var occupied = new Dictionary<(int x, int y), int>();
            for (int i = 0; i < d.Blocks.Count; i++)
            {
                var b = d.Blocks[i];
                if (b.Layers.Count == 0)
                    errors.Add($"blocks[{i}] için layers boş.");
                for (int x = b.X; x < b.X + b.W; x++)
                {
                    for (int y = b.Y; y < b.Y + b.H; y++)
                    {
                        if (!Playable(x, y))
                        {
                            errors.Add($"blocks[{i}] oynanamaz hücreye taşıyor: ({x},{y}).");
                            continue;
                        }
                        if (occupied.TryGetValue((x, y), out int other))
                            errors.Add($"blocks[{i}] ile blocks[{other}] çakışıyor: ({x},{y}).");
                        else
                            occupied[(x, y)] = i;
                    }
                }
            }

            // Kapılar: kapladığı hücreler oynanabilir olmalı.
            for (int i = 0; i < d.Gates.Count; i++)
            {
                var g = d.Gates[i];
                if (g.Colors.Count == 0)
                    errors.Add($"gates[{i}] için colors boş.");
                if (!SideUtil.TryParse(g.Side, out var side))
                {
                    errors.Add($"gates[{i}] kenarı geçersiz: '{g.Side}'.");
                    continue;
                }
                bool horizontal = side == Side.North || side == Side.South;
                for (int j = 0; j < g.Length; j++)
                {
                    int cx = horizontal ? g.X + j : g.X;
                    int cy = horizontal ? g.Y : g.Y + j;
                    if (!Playable(cx, cy))
                        errors.Add($"gates[{i}] oynanamaz hücreye dayalı: ({cx},{cy}).");
                }
            }

            // Kapısı olmayan renk: hata değil, tasarım uyarısı.
            var gateColors = new HashSet<string>();
            foreach (var g in d.Gates)
                foreach (var c in g.Colors)
                    gateColors.Add(c.ToLowerInvariant());
            foreach (var b in d.Blocks)
                foreach (var layer in b.Layers)
                    if (!gateColors.Contains(layer.ToLowerInvariant()))
                        Debug.LogWarning($"[LevelLoader] '{layer}' renginde blok var ama kapısı yok — bölüm bitirilemez olabilir ({d.Id}).");

            return errors.Count == start;
        }
    }
}
