using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Core
{
    /// <summary>
    /// Bir bölümün gerçekten çözülebilir olup olmadığını kanıtlamaya çalışır ve
    /// yol boyunca TASARIM ÖLÇÜMLERİ toplar.
    ///
    /// DERS (neden gerekti?): İlk M2 testlerinde blokları kapıya IŞINLAYARAK
    /// doğrulama yapmıştık; oyuncu testinde level_003'ün çözülemez olduğu
    /// çıktı çünkü kırmızı blok kapıya fiziksel olarak ULAŞAMIYORDU. Ders:
    /// "kural doğru işliyor mu" ile "bölüm oynanabilir mi" AYRI sorulardır.
    ///
    /// Yöntem: her blok için DragSolver ile BFS — bloğun tek sürüklemelerle
    /// varabileceği tüm hücreler keşfedilir, her durumda kapı teması denenir.
    /// Her adımda KAÇ bloğun çıkarılabildiği de sayılır; bu sayı bölümün
    /// darlığını ölçer (1 ise oyuncunun tek seçeneği vardır = zorunlu hamle).
    ///
    /// Açgözlü olduğu için EKSİKTİR: "çözülebilir" dediyse bölüm kesinlikle
    /// bitirilebilir (kanıt = hamle listesi); "çözemedim" dediyse bölüm zor
    /// olabilir de, gerçekten kilitli olabilir de — tasarımcı uyarısıdır.
    /// </summary>
    public static class LevelSolver
    {
        /// <summary>Çözümdeki tek bir hamle — editör bunu tahtada rozet olarak gösterir.</summary>
        public struct Move
        {
            public BlockColor Color;
            public int X, Y, W, H;
            public string Outcome;   // Absorbed / Peeled
            public int Options;      // bu adımda kaç blok çıkarılabilirdi
        }

        public sealed class Result
        {
            public bool Solved;
            public readonly List<Move> Moves = new List<Move>();
            public int RemainingBlocks;
            public bool PendingCurtainContent;

            /// <summary>İlk hamlede oyuncunun kaç seçeneği vardı (açıklık ölçüsü).</summary>
            public int InitialOptions;

            /// <summary>Tek seçenekli (zorunlu) hamle sayısı — yüksekse bölüm "dar"dır.</summary>
            public int ForcedSteps;

            /// <summary>Ortalama seçenek sayısı — düşükse bölüm katı, yüksekse serbesttir.</summary>
            public float AverageOptions;
        }

        /// <summary>Bloğu ÇIKARMADAN "şu an çıkarılabilir mi" testi.</summary>
        public delegate bool CanClear(BlockModel block);

        /// <summary>Bloğu gerçekten çıkarır; sonucu (Absorbed/Peeled) döndürür.</summary>
        public delegate string ApplyClear(BlockModel block);

        public static Result Solve(
            LevelModel level, CanClear canClear, ApplyClear apply,
            float substep, float epsilon, int maxMoves = 300)
        {
            var result = new Result();
            var obstacles = new List<Aabb>();
            var reachable = new HashSet<Vector2>();
            var queue = new Queue<Vector2>();
            int totalOptions = 0;

            for (int move = 0; move < maxMoves; move++)
            {
                if (level.Blocks.Count == 0 && !level.HasPendingContent())
                    break;

                // Bu adımda çıkarılabilecek TÜM blokları bul (seçenek sayısı için).
                BlockModel chosen = null;
                Vector2 chosenAt = default;
                int options = 0;

                foreach (var block in new List<BlockModel>(level.Blocks))
                {
                    if (!TryFindClearingSpot(level, block, canClear, obstacles, reachable, queue,
                            substep, epsilon, out var spot))
                        continue;

                    options++;
                    if (chosen == null) { chosen = block; chosenAt = spot; }
                }

                if (chosen == null) break;

                if (result.Moves.Count == 0) result.InitialOptions = options;
                if (options == 1) result.ForcedSteps++;
                totalOptions += options;

                chosen.Position = chosenAt;
                var record = new Move
                {
                    Color = chosen.CurrentColor,
                    X = Mathf.RoundToInt(chosenAt.x), Y = Mathf.RoundToInt(chosenAt.y),
                    W = chosen.W, H = chosen.H,
                    Options = options
                };
                record.Outcome = apply(chosen);
                result.Moves.Add(record);
            }

            result.RemainingBlocks = level.Blocks.Count;
            result.PendingCurtainContent = level.HasPendingContent();
            result.Solved = result.RemainingBlocks == 0 && !result.PendingCurtainContent;
            result.AverageOptions = result.Moves.Count > 0
                ? (float)totalOptions / result.Moves.Count : 0f;
            return result;
        }

        /// <summary>
        /// Bloğun ulaşabildiği hücreler içinde kapıya değdiği ilk konumu arar.
        /// Blok konumu test sırasında oynatılır; bulunamazsa aynen geri konur.
        /// </summary>
        static bool TryFindClearingSpot(
            LevelModel level, BlockModel block, CanClear canClear,
            List<Aabb> obstacles, HashSet<Vector2> reachable, Queue<Vector2> queue,
            float substep, float epsilon, out Vector2 spot)
        {
            spot = block.Position;
            if (block.IsFrozen) return false;

            obstacles.Clear();
            level.CollectObstacles(obstacles, block);

            var start = block.Position;
            reachable.Clear();
            queue.Clear();
            reachable.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var pos = queue.Dequeue();
                block.Position = pos;

                if (canClear(block))
                {
                    block.Position = start;
                    spot = pos;
                    return true;
                }

                TryStep(block, pos, Vector2.right, obstacles, reachable, queue, substep, epsilon);
                TryStep(block, pos, Vector2.left, obstacles, reachable, queue, substep, epsilon);
                TryStep(block, pos, Vector2.up, obstacles, reachable, queue, substep, epsilon);
                TryStep(block, pos, Vector2.down, obstacles, reachable, queue, substep, epsilon);
            }

            block.Position = start;
            return false;
        }

        static void TryStep(
            BlockModel block, Vector2 from, Vector2 dir, List<Aabb> obstacles,
            HashSet<Vector2> reachable, Queue<Vector2> queue, float substep, float epsilon)
        {
            var solved = DragSolver.Solve(from, from + dir, block.Cells,
                obstacles, substep, epsilon);
            var cell = new Vector2(Mathf.Round(solved.x), Mathf.Round(solved.y));
            if (cell != from && reachable.Add(cell))
                queue.Enqueue(cell);
        }
    }
}
