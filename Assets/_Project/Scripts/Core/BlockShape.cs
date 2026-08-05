using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Core
{
    /// <summary>
    /// BlockData'nın (JSON DTO) kapladığı hücreleri çözen tek doğruluk kaynağı.
    ///
    /// DERS (tek kural, üç tüketici): "cells maskesi varsa ondan, yoksa w×h
    /// dikdörtgeninden" kuralını runtime yükleyici, doğrulayıcı ve level editörü
    /// ayrı ayrı yazsaydı, üçü ilk şema değişikliğinde ayrışırdı. Kural burada
    /// bir kez yazılır; hepsi buraya sorar.
    ///
    /// Yerel hücre uzayı: (0,0) = maskenin sol-üst hücresi, x sağa, y aşağıya
    /// (tahtanın "rows" dizisiyle aynı yön).
    /// </summary>
    public static class BlockShape
    {
        /// <summary>Bloğun yerel hücreleri (blok köşesine göre).</summary>
        public static void LocalCells(BlockData b, List<Vector2Int> output)
        {
            output.Clear();

            if (b.Cells == null || b.Cells.Count == 0)
            {
                int w = Mathf.Max(1, b.W), h = Mathf.Max(1, b.H);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        output.Add(new Vector2Int(x, y));
                return;
            }

            for (int row = 0; row < b.Cells.Count; row++)
            {
                string line = b.Cells[row];
                if (string.IsNullOrEmpty(line)) continue;
                for (int col = 0; col < line.Length; col++)
                    if (char.ToUpperInvariant(line[col]) == 'X')
                        output.Add(new Vector2Int(col, row));
            }
        }

        /// <summary>Tahta uzayındaki (x,y) hücresi bu bloğun üstünde mi?</summary>
        public static bool Covers(BlockData b, int x, int y)
        {
            int lx = x - b.X, ly = y - b.Y;
            if (lx < 0 || ly < 0) return false;

            if (b.Cells == null || b.Cells.Count == 0)
                return lx < Mathf.Max(1, b.W) && ly < Mathf.Max(1, b.H);

            if (ly >= b.Cells.Count) return false;
            string line = b.Cells[ly];
            if (string.IsNullOrEmpty(line) || lx >= line.Length) return false;
            return char.ToUpperInvariant(line[lx]) == 'X';
        }

        /// <summary>
        /// Yerel hücrelerden maske satırları üretir ve bloğun x/y/w/h alanlarını
        /// buna göre tazeler. Dikdörtgen çıkarsa maske temizlenir — JSON gereksiz
        /// yere şişmesin, eski araçlar da okuyabilsin.
        /// </summary>
        public static void ApplyCells(BlockData b, IEnumerable<Vector2Int> boardCells)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            var list = new List<Vector2Int>();

            foreach (var cell in boardCells)
            {
                list.Add(cell);
                if (cell.x < minX) minX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y > maxY) maxY = cell.y;
            }
            if (list.Count == 0) return;

            b.X = minX;
            b.Y = minY;
            b.W = maxX - minX + 1;
            b.H = maxY - minY + 1;

            if (list.Count == b.W * b.H) { b.Cells = null; return; }

            var rows = new List<string>(b.H);
            var buffer = new char[b.W];
            for (int y = 0; y < b.H; y++)
            {
                for (int x = 0; x < b.W; x++) buffer[x] = '.';
                foreach (var cell in list)
                    if (cell.y - minY == y) buffer[cell.x - minX] = 'X';
                rows.Add(new string(buffer));
            }
            b.Cells = rows;
        }

        /// <summary>
        /// Yerel hücrelerin sınırlayıcı kutusu. DERS: b.W/b.H'ye GÜVENİLMEZ —
        /// elle yazılmış ya da eski bir JSON'da maske ile alanlar uyuşmayabilir.
        /// Şeklin tek gerçeği maskenin kendisidir.
        /// </summary>
        static Vector2Int Extent(List<Vector2Int> local)
        {
            int w = 0, h = 0;
            foreach (var cell in local)
            {
                if (cell.x + 1 > w) w = cell.x + 1;
                if (cell.y + 1 > h) h = cell.y + 1;
            }
            return new Vector2Int(Mathf.Max(1, w), Mathf.Max(1, h));
        }

        /// <summary>
        /// w/h alanlarını maskeyle eşitler ve tamamen dolu maskeleri temizler.
        /// Elle yazılmış JSON'lar için: şekil doğru olsa bile w/h yanlışsa
        /// editördeki sürükleme sınırları kayar.
        /// </summary>
        public static void Normalize(BlockData b)
        {
            var local = new List<Vector2Int>();
            LocalCells(b, local);
            if (local.Count == 0) return;

            for (int i = 0; i < local.Count; i++)
                local[i] += new Vector2Int(b.X, b.Y);
            ApplyCells(b, local);
        }

        /// <summary>Maskeyi 90° saat yönünde döndürür (editörde R kısayolu).</summary>
        public static void RotateCw(BlockData b)
        {
            var local = new List<Vector2Int>();
            LocalCells(b, local);
            int h = Extent(local).y;

            for (int i = 0; i < local.Count; i++)
                local[i] = new Vector2Int(h - 1 - local[i].y + b.X, local[i].x + b.Y); // (x,y) → (h-1-y, x)

            ApplyCells(b, local);
        }

        /// <summary>Maskeyi yatay aynalar (editörde F kısayolu).</summary>
        public static void FlipHorizontal(BlockData b)
        {
            var local = new List<Vector2Int>();
            LocalCells(b, local);
            int w = Extent(local).x;

            for (int i = 0; i < local.Count; i++)
                local[i] = new Vector2Int(w - 1 - local[i].x + b.X, local[i].y + b.Y);
            ApplyCells(b, local);
        }
    }
}
