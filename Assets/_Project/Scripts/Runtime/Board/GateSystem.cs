using BlockOut.Core;
using BlockOut.Runtime.Config;
using UnityEngine;

namespace BlockOut.Runtime.Board
{
    /// <summary>
    /// Kapı temas kararı: sürüklenen blok bir kapıya değiyor mu, rengi uyuyor
    /// mu, aralığa sığıyor mu? Uyuyorsa bloğu modelden düşürür, emilme
    /// animasyonunu tetikler ve olayları yayınlar.
    ///
    /// Videodan doğrulanan kural: emilme SÜRÜKLEME SIRASINDA da gerçekleşir —
    /// bloğu bırakmak gerekmez. Bu yüzden DragController her karede sorar.
    /// Katman soyma kararı (ResolveGateContact'ın M2 hali) da buraya gelecek.
    /// </summary>
    public sealed class GateSystem
    {
        readonly LevelModel _level;
        readonly BoardViews _views;
        readonly GameConfigSO _config;
        readonly BoardEvents _events;

        public GateSystem(LevelModel level, BoardViews views, GameConfigSO config, BoardEvents events)
        {
            _level = level;
            _views = views;
            _config = config;
            _events = events;
        }

        /// <summary>Blok bir kapıya temas ediyorsa emer ve true döner.</summary>
        public bool TryAbsorb(BlockModel block)
        {
            foreach (var gate in _level.Gates)
            {
                if (block.CurrentColor != gate.ActiveColor) continue;
                if (!IsTouching(block, gate)) continue;

                Absorb(block, gate);
                return true;
            }
            return false;
        }

        bool IsTouching(BlockModel block, GateModel gate)
        {
            // Kenara dik eksende: bloğun kapıya bakan kenarı, kapı çizgisine
            // gateContactGap'ten yakın olmalı. DragSolver tam kenetlendiği için
            // duvara dayalı blokta bu mesafe fiilen 0'dır.
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

            // Kenar boyunca: bloğun o kenarı kapı aralığının İÇİNE sığmalı
            // (küçük tolerans payıyla — his ayarı).
            return spanStart >= gate.SpanMin - _config.gateSpanTolerance &&
                   spanStart + spanSize <= gate.SpanMax + _config.gateSpanTolerance;
        }

        void Absorb(BlockModel block, GateModel gate)
        {
            _level.RemoveBlock(block);

            if (_views.Blocks.TryGetValue(block, out var view))
            {
                _views.Blocks.Remove(block);
                view.SetHighlight(false);

                // Hücre-uzayı "dışarı" yönünü dünya yönüne çevir (y ekseni Z'ye ters).
                Vector3 dir = gate.EdgeHorizontal
                    ? new Vector3(0f, 0f, -gate.OutwardSign)
                    : new Vector3(gate.OutwardSign, 0f, 0f);
                view.PlayAbsorb(dir, _config.absorbDuration);
            }

            _events.RaiseBlockAbsorbed(block, gate);
            if (_level.Blocks.Count == 0)
                _events.RaiseBoardCleared();
        }
    }
}
