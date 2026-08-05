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
            var frontier = new MinHeap();
            frontier.Push(startState, Heuristic(level, movers, startState));

            var stepReachable = new HashSet<Vector2>();
            var stepQueue = new Queue<Vector2>();
            int expanded = 0;

            while (frontier.Count > 0)
            {
                var state = frontier.Pop();
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
                                frontier.Push(next, Heuristic(level, movers, next));
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

        /// <summary>
        /// "Bu düzen hedefe ne kadar yakın?" — herhangi bir bloğun kendi kapısına
        /// olan en kısa mesafesi. Arama kör BFS yerine bunu izleyince (best-first)
        /// gereken kaydırma dizisini onlarca kat daha az düğümde buluyor.
        ///
        /// DERS (sezgisel = ucuz tahmin): Değerin DOĞRU olması gerekmiyor, sadece
        /// "daha iyi düzen daha küçük sayı" sıralamasını kabaca vermesi yeterli.
        /// Duvarları ve diğer blokları yok sayıyoruz; hesap birkaç çıkarma işlemi.
        /// </summary>
        static float Heuristic(LevelModel level, List<BlockModel> movers, Vector2[] state)
        {
            float best = float.MaxValue;

            for (int i = 0; i < movers.Count; i++)
            {
                var block = movers[i];
                if (block.IsFrozen) continue;
                var pos = state[i];

                foreach (var gate in level.Gates)
                {
                    if (gate.IsIced || gate.IsGhost) continue;
                    if (block.CurrentColor != gate.ActiveColor) continue;

                    // Kapıya dik uzaklık: bloğun kapıya bakan kenarı ile kapı çizgisi.
                    float lead = gate.EdgeHorizontal
                        ? (gate.OutwardSign < 0 ? pos.y : pos.y + block.H)
                        : (gate.OutwardSign < 0 ? pos.x : pos.x + block.W);
                    float perpendicular = Mathf.Abs(lead - gate.EdgeCoord);

                    // Kenar boyunca kayma: bloğun açıklığa oturması için gereken kayma.
                    float spanStart = gate.EdgeHorizontal ? pos.x : pos.y;
                    float spanSize = gate.EdgeHorizontal ? block.W : block.H;
                    float slide = 0f;
                    if (spanStart < gate.SpanMin) slide = gate.SpanMin - spanStart;
                    else if (spanStart + spanSize > gate.SpanMax) slide = spanStart + spanSize - gate.SpanMax;

                    float total = perpendicular + slide;
                    if (total < best) best = total;
                }
            }

            return best == float.MaxValue ? 0f : best;
        }

        /// <summary>
        /// En küçük öncelikli ikili yığın. Sıralı bir liste yerine yığın kullanmak,
        /// on binlerce düğümde ekleme/çıkarmayı O(log n)'de tutuyor.
        /// </summary>
        sealed class MinHeap
        {
            readonly List<Vector2[]> _items = new List<Vector2[]>();
            readonly List<float> _keys = new List<float>();

            public int Count => _items.Count;

            public void Push(Vector2[] item, float key)
            {
                _items.Add(item);
                _keys.Add(key);

                int child = _items.Count - 1;
                while (child > 0)
                {
                    int parent = (child - 1) / 2;
                    if (_keys[parent] <= _keys[child]) break;
                    Swap(parent, child);
                    child = parent;
                }
            }

            public Vector2[] Pop()
            {
                var top = _items[0];
                int last = _items.Count - 1;
                _items[0] = _items[last];
                _keys[0] = _keys[last];
                _items.RemoveAt(last);
                _keys.RemoveAt(last);

                int parent = 0;
                while (true)
                {
                    int left = parent * 2 + 1, right = left + 1, smallest = parent;
                    if (left < _items.Count && _keys[left] < _keys[smallest]) smallest = left;
                    if (right < _items.Count && _keys[right] < _keys[smallest]) smallest = right;
                    if (smallest == parent) break;
                    Swap(parent, smallest);
                    parent = smallest;
                }
                return top;
            }

            void Swap(int a, int b)
            {
                (_items[a], _items[b]) = (_items[b], _items[a]);
                (_keys[a], _keys[b]) = (_keys[b], _keys[a]);
            }
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
