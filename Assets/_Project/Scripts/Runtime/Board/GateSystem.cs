using BlockOut.Core;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.View;
using UnityEngine;

namespace BlockOut.Runtime.Board
{
    /// <summary>Kapı temasının olası sonuçları.</summary>
    public enum GateContactResult { None, Peeled, Absorbed }

    /// <summary>
    /// Kapı temas kararı (planın "ResolveGateContact" noktası — videodan
    /// doğrulanan hali): temas + renk + sığma sağlanırsa ya katman soyulur
    /// (çok katmanlı blok) ya da blok tamamen emilir. Buzlu ve ghost kapılar
    /// devre dışıdır. Her emilme, engel sayaçlarını ilerletir ve kapıların
    /// ghost durumunu tazeler.
    /// </summary>
    public sealed class GateSystem
    {
        readonly LevelModel _level;
        readonly BoardViews _views;
        readonly GameConfigSO _config;
        readonly BoardEvents _events;
        readonly ObstacleSystem _obstacles;
        readonly ColorPaletteSO _palette;

        public GateSystem(
            LevelModel level, BoardViews views, GameConfigSO config,
            BoardEvents events, ObstacleSystem obstacles, ColorPaletteSO palette)
        {
            _level = level;
            _views = views;
            _config = config;
            _events = events;
            _obstacles = obstacles;
            _palette = palette;
        }

        /// <summary>
        /// Blok şu an bir kapıdan çıkabilir mi? DURUMU DEĞİŞTİRMEZ — level
        /// çözücüsü "kaç seçenek var" sayarken bunu kullanır (ResolveContact
        /// çağırsaydı bloğu gerçekten çıkarırdı).
        /// </summary>
        public bool CanResolve(BlockModel block)
        {
            if (block.IsFrozen) return false;

            foreach (var gate in _level.Gates)
            {
                if (gate.IsIced || gate.IsGhost) continue;
                if (block.CurrentColor != gate.ActiveColor) continue;
                if (IsTouching(block, gate)) return true;
            }
            return false;
        }

        /// <summary>Blok bir kapıya değiyorsa sonucu uygular ve ne olduğunu döndürür.</summary>
        public GateContactResult ResolveContact(BlockModel block)
        {
            if (block.IsFrozen) return GateContactResult.None;

            foreach (var gate in _level.Gates)
            {
                if (gate.IsIced || gate.IsGhost) continue;
                if (block.CurrentColor != gate.ActiveColor) continue;
                if (!IsTouching(block, gate)) continue;

                if (block.Layers.Count > 1)
                {
                    Peel(block, gate);
                    return GateContactResult.Peeled;
                }

                Absorb(block, gate);
                return GateContactResult.Absorbed;
            }
            return GateContactResult.None;
        }

        bool IsTouching(BlockModel block, GateModel gate)
        {
            float blockEdge;
            float spanStart;
            float spanSize;
            if (gate.EdgeHorizontal)
            {
                blockEdge = gate.OutwardSign < 0 ? block.Position.y : block.Position.y + block.H;
                spanStart = block.Position.x;
                spanSize = block.W;
            }
            else
            {
                blockEdge = gate.OutwardSign < 0 ? block.Position.x : block.Position.x + block.W;
                spanStart = block.Position.y;
                spanSize = block.H;
            }

            if (Mathf.Abs(blockEdge - gate.EdgeCoord) > _config.gateContactGap)
                return false;

            return spanStart >= gate.SpanMin - _config.gateSpanTolerance &&
                   spanStart + spanSize <= gate.SpanMax + _config.gateSpanTolerance;
        }

        /// <summary>
        /// Katman soyma (video kuralı): dış katman kapıda "emilir", blok İÇ
        /// rengiyle tahtada kalır ve sürükleme sona erer. Soyulma bir blok
        /// ÇIKIŞI sayılmaz — buz/perde sayaçları ilerlemez.
        /// </summary>
        void Peel(BlockModel block, GateModel gate)
        {
            block.Layers.RemoveAt(0);

            if (_views.Blocks.TryGetValue(block, out var view))
            {
                view.SetHighlight(false);
                view.SetLayerMaterial(BoardBuilder.GetBlockMaterial(_palette, block.CurrentColor));
            }

            _events.RaiseLayerPeeled(block, gate);
            RecomputeGateStates(); // soyulan rengin son örneğiyse kapısı ghost olabilir
        }

        void Absorb(BlockModel block, GateModel gate)
        {
            _level.RemoveBlock(block);

            if (_views.Blocks.TryGetValue(block, out var view))
            {
                _views.Blocks.Remove(block);
                view.SetHighlight(false);

                Vector3 dir = gate.EdgeHorizontal
                    ? new Vector3(0f, 0f, -gate.OutwardSign)
                    : new Vector3(gate.OutwardSign, 0f, 0f);
                view.PlayAbsorb(dir, _config.absorbDuration);
            }

            _events.RaiseBlockAbsorbed(block, gate);

            // Çıkış zinciri: buzlar erir, perdeler sayar (belki içerik doğar)...
            _obstacles.NotifyBlockExit();
            // ...renk mevcudiyeti değişti — ghost/kuyruk durumlarını tazele.
            RecomputeGateStates();

            if (_level.Blocks.Count == 0 && !_level.HasPendingContent())
                _events.RaiseBoardCleared();
        }

        /// <summary>
        /// Her kapı için: aktif renk oyunda (gizli katmanlar ve perde içerikleri
        /// dahil) kalmadıysa kuyruk varsa ilerler, yoksa kapı kalıcı ghost olur.
        /// Buzlu kapılar atlanır — buz kırılınca zaten yeniden hesaplanır.
        /// </summary>
        public void RecomputeGateStates()
        {
            foreach (var gate in _level.Gates)
            {
                if (gate.IsIced || gate.IsGhost) continue;

                while (!_level.AnyColorRemaining(gate.ActiveColor))
                {
                    _views.Gates.TryGetValue(gate, out var view);

                    if (gate.AdvanceQueue())
                    {
                        if (view != null)
                            view.SetColorMaterial(
                                BoardBuilder.GetBlockMaterial(_palette, gate.ActiveColor));
                        _events.RaiseGateAdvanced(gate);
                        continue; // yeni rengin mevcudiyetini de denetle
                    }

                    gate.IsGhost = true;
                    if (view != null)
                        view.SetGhost(ViewKit.GhostFor(_palette, gate.ActiveColor));
                    _events.RaiseGateGhosted(gate);
                    break;
                }
            }
        }
    }
}
