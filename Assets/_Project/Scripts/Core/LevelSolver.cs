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

            /// <summary>Kaç kez blok kaydırma (shuffle) araması gerekti.</summary>
            public int ShuffleSteps;

            /// <summary>
            /// Arama bütçesi doldu ve karar verilemedi. Solved=false ama bu
            /// "kilitli" DEMEK DEĞİLDİR — çözücü pes etti, insan çözebilir.
            /// </summary>
            public bool Inconclusive;
        }

        /// <summary>Bloğu ÇIKARMADAN "şu an çıkarılabilir mi" testi.</summary>
        public delegate bool CanClear(BlockModel block);

        /// <summary>Bloğu gerçekten çıkarır; sonucu (Absorbed/Peeled) döndürür.</summary>
        public delegate string ApplyClear(BlockModel block);

        public static Result Solve(
            LevelModel level, CanClear canClear, ApplyClear apply,
            float substep, float epsilon, int maxMoves = 300, int shuffleNodeBudget = 60000)
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

                // Hiçbir blok kapıya varamıyor: yoğun tahtalarda bu KİLİT DEMEK
                // DEĞİL — önce başka blokları kaydırmak gerekiyor olabilir.
                // (Referans oyunun bölümleri neredeyse tıka basa dolu; tek boş
                // hücreli "15 puzzle" gibi dansları açgözlü arama göremez.)
                if (chosen == null)
                {
                    if (!TryShuffleToClearing(level, canClear, obstacles, substep, epsilon,
                            shuffleNodeBudget, out bool budgetExhausted))
                    {
                        result.Inconclusive = budgetExhausted;
                        break;
                    }

                    result.ShuffleSteps++;
                    move--;      // kaydırma bir "hamle" değil; aynı adımı tekrar dene
                    continue;
                }

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

        /// <summary>
        /// KAYDIRMA ARAMASI: hiçbir blok tek başına kapıya varamıyorsa, tahtanın
        /// TAMAMININ durumu üzerinde genişlik-öncelikli arama yapar ve bir bloğun
        /// kapıya varabildiği ilk düzene geçer.
        ///
        /// DERS (neden ayrı bir arama?): "Bu blok kapıya varabilir mi" sorusu tek
        /// bloğu oynatır; "bölüm çözülebilir mi" sorusu ise BÜTÜN tahtanın durum
        /// uzayında dolaşmayı gerektirir — 15-puzzle'daki gibi. İkisini tek
        /// aramada birleştirmek yerine, ucuz olanı önce koşturup pahalıyı yalnız
        /// gerektiğinde açmak (escalation) hem hızlı hem anlaşılır kalıyor.
        ///
        /// Bütçe dolarsa false döner ve <paramref name="budgetExhausted"/> true
        /// olur: "kilitli" ile "bulamadım" ayrı şeylerdir, tasarımcıya öyle denir.
        /// </summary>
        static bool TryShuffleToClearing(
            LevelModel level, CanClear canClear, List<Aabb> obstacles,
            float substep, float epsilon, int nodeBudget, out bool budgetExhausted)
        {
            budgetExhausted = false;

            // Yalnız oynatılabilir bloklar aranan durumun parçası; donmuş
            // bloklar sabit engel olarak kalır.
            var movers = new List<BlockModel>();
            foreach (var block in level.Blocks)
                if (!block.IsFrozen) movers.Add(block);
            if (movers.Count == 0) return false;

            var startState = Capture(movers);
            var visited = new HashSet<string> { Key(startState) };
            var frontier = new Queue<Vector2[]>();
            frontier.Enqueue(startState);

            var stepReachable = new HashSet<Vector2>();
            var stepQueue = new Queue<Vector2>();
            int expanded = 0;

            while (frontier.Count > 0)
            {
                var state = frontier.Dequeue();
                Restore(movers, state);

                for (int i = 0; i < movers.Count; i++)
                {
                    var block = movers[i];

                    // Bu bloğun bu düzende varabildiği tüm hücreler.
                    obstacles.Clear();
                    level.CollectObstacles(obstacles, block);

                    var origin = state[i];
                    stepReachable.Clear();
                    stepQueue.Clear();
                    stepReachable.Add(origin);
                    stepQueue.Enqueue(origin);

                    while (stepQueue.Count > 0)
                    {
                        var pos = stepQueue.Dequeue();
                        block.Position = pos;

                        if (pos != origin)
                        {
                            if (canClear(block))
                            {
                                // Hedef düzen bulundu: blokları oraya yerleştir
                                // ve dış döngü normal hamlesini yapsın.
                                block.Position = origin;
                                Restore(movers, state);
                                return true;
                            }

                            var next = (Vector2[])state.Clone();
                            next[i] = pos;
                            if (visited.Add(Key(next)))
                            {
                                frontier.Enqueue(next);
                                if (++expanded >= nodeBudget)
                                {
                                    block.Position = origin;
                                    Restore(movers, startState);
                                    budgetExhausted = true;
                                    return false;
                                }
                            }
                        }

                        TryStep(block, pos, Vector2.right, obstacles, stepReachable, stepQueue, substep, epsilon);
                        TryStep(block, pos, Vector2.left, obstacles, stepReachable, stepQueue, substep, epsilon);
                        TryStep(block, pos, Vector2.up, obstacles, stepReachable, stepQueue, substep, epsilon);
                        TryStep(block, pos, Vector2.down, obstacles, stepReachable, stepQueue, substep, epsilon);
                    }

                    block.Position = origin;
                }
            }

            Restore(movers, startState);
            return false;
        }

        static Vector2[] Capture(List<BlockModel> blocks)
        {
            var state = new Vector2[blocks.Count];
            for (int i = 0; i < blocks.Count; i++) state[i] = blocks[i].Position;
            return state;
        }

        static void Restore(List<BlockModel> blocks, Vector2[] state)
        {
            for (int i = 0; i < blocks.Count; i++) blocks[i].Position = state[i];
        }

        static string Key(Vector2[] state)
        {
            var sb = new System.Text.StringBuilder(state.Length * 6);
            foreach (var p in state)
            {
                sb.Append((int)p.x).Append(',').Append((int)p.y).Append(';');
            }
            return sb.ToString();
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
