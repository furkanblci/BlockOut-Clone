using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Core
{
    /// <summary>
    /// Bir bölümün gerçekten çözülebilir olup olmadığını kanıtlamaya çalışır.
    ///
    /// DERS (neden gerekti?): İlk M2 testlerinde blokları kapıya IŞINLAYARAK
    /// doğrulama yapmıştık; oyuncu testinde level_003'ün çözülemez olduğu
    /// çıktı çünkü kırmızı blok kapıya fiziksel olarak ULAŞAMIYORDU. Ders:
    /// "kural doğru işliyor mu" ile "bölüm oynanabilir mi" AYRI sorulardır.
    ///
    /// Yöntem: her blok için DragSolver ile BFS — bloğun tek sürüklemelerle
    /// varabileceği tüm hücreler keşfedilir, her durumda kapı teması denenir.
    /// Çıkarılabilen ilk blok çıkarılır (açgözlü) ve baştan başlanır.
    ///
    /// Açgözlü olduğu için EKSİKTİR: "çözülebilir" dediyse bölüm kesinlikle
    /// bitirilebilir (kanıt = hamle listesi); "çözemedim" dediyse bölüm zor
    /// olabilir de, gerçekten kilitli olabilir de — tasarımcı uyarısıdır.
    /// </summary>
    public static class LevelSolver
    {
        public sealed class Result
        {
            public bool Solved;
            public readonly List<string> Moves = new List<string>();
            public int RemainingBlocks;
            public bool PendingCurtainContent;
        }

        /// <summary>Kapı temasını uygulayan geri çağırım (GateSystem.ResolveContact).
        /// Core'un Runtime'a bağımlı olmaması için dışarıdan verilir.</summary>
        public delegate bool TryResolveContact(BlockModel block, out string outcome);

        public static Result Solve(
            LevelModel level, TryResolveContact resolve,
            float substep, float epsilon, int maxMoves = 200)
        {
            var result = new Result();
            var obstacles = new List<Aabb>();
            var reachable = new HashSet<Vector2>();
            var queue = new Queue<Vector2>();

            for (int move = 0; move < maxMoves; move++)
            {
                if (level.Blocks.Count == 0 && !level.HasPendingContent())
                {
                    result.Solved = true;
                    return result;
                }

                if (!TryClearOne(level, resolve, obstacles, reachable, queue,
                        substep, epsilon, result.Moves))
                    break;
            }

            result.RemainingBlocks = level.Blocks.Count;
            result.PendingCurtainContent = level.HasPendingContent();
            result.Solved = result.RemainingBlocks == 0 && !result.PendingCurtainContent;
            return result;
        }

        static bool TryClearOne(
            LevelModel level, TryResolveContact resolve,
            List<Aabb> obstacles, HashSet<Vector2> reachable, Queue<Vector2> queue,
            float substep, float epsilon, List<string> moves)
        {
            // Kopya üzerinde geziyoruz: resolve çağrısı listeyi değiştirebilir.
            var candidates = new List<BlockModel>(level.Blocks);

            foreach (var block in candidates)
            {
                if (block.IsFrozen) continue;

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

                    if (resolve(block, out string outcome))
                    {
                        moves.Add($"{block.CurrentColor} @({(int)pos.x},{(int)pos.y}) -> {outcome}");
                        return true;
                    }

                    TryStep(block, pos, Vector2.right, obstacles, reachable, queue, substep, epsilon);
                    TryStep(block, pos, Vector2.left,  obstacles, reachable, queue, substep, epsilon);
                    TryStep(block, pos, Vector2.up,    obstacles, reachable, queue, substep, epsilon);
                    TryStep(block, pos, Vector2.down,  obstacles, reachable, queue, substep, epsilon);
                }

                block.Position = start; // hiçbir kapıya ulaşamadı — yerine koy
            }
            return false;
        }

        static void TryStep(
            BlockModel block, Vector2 from, Vector2 dir, List<Aabb> obstacles,
            HashSet<Vector2> reachable, Queue<Vector2> queue, float substep, float epsilon)
        {
            var solved = DragSolver.Solve(from, from + dir, block.W, block.H,
                obstacles, substep, epsilon);
            var cell = new Vector2(Mathf.Round(solved.x), Mathf.Round(solved.y));
            if (cell != from && reachable.Add(cell))
                queue.Enqueue(cell);
        }
    }
}
