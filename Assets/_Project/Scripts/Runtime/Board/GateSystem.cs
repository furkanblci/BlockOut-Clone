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

        /// <summary>
        /// Blok kapıya değiyor mu?
        ///
        /// İki koşul birlikte aranır:
        /// 1) TEMAS — bloğun EN AZ BİR hücresinin dışa bakan kenarı kapı
        ///    çizgisine yapışmış olmalı.
        /// 2) SIĞMA — bloğun kenar boyunca TOPLAM genişliği kapı açıklığına
        ///    sığmalı; 2 hücrelik kapıdan 3 hücre genişliğinde blok geçemez.
        ///
        /// DERS (polyomino): Dikdörtgende bu iki koşul aynı hesaptan çıkıyordu.
        /// L şeklinde ayrışıyor: bloğun bir hücresi kapıya değiyor olabilir ama
        /// başka bir hücresi açıklığın dışında kalabilir — o zaman blok geçemez.
        /// Bu yüzden temas hücre hücre, sığma sınırlayıcı kutu üzerinden bakılır.
        /// </summary>
        bool IsTouching(BlockModel block, GateModel gate)
        {
            bool horizontal = gate.EdgeHorizontal;
            bool towardNegative = gate.OutwardSign < 0;

            bool anyCellTouches = false;
            foreach (var cell in block.Cells)
            {
                float cellX = block.Position.x + cell.x;
                float cellY = block.Position.y + cell.y;

                float cellEdge = horizontal
                    ? (towardNegative ? cellY : cellY + 1f)
                    : (towardNegative ? cellX : cellX + 1f);

                if (Mathf.Abs(cellEdge - gate.EdgeCoord) > _config.gateContactGap) continue;

                // Değen hücrenin kendisi de açıklığın içinde olmalı.
                float cellSpan = horizontal ? cellX : cellY;
                if (cellSpan < gate.SpanMin - _config.gateSpanTolerance) continue;
                if (cellSpan + 1f > gate.SpanMax + _config.gateSpanTolerance) continue;

                anyCellTouches = true;
                break;
            }
            if (!anyCellTouches) return false;

            float spanStart = horizontal ? block.Position.x : block.Position.y;
            float spanSize = horizontal ? block.W : block.H;
            return spanStart >= gate.SpanMin - _config.gateSpanTolerance &&
                   spanStart + spanSize <= gate.SpanMax + _config.gateSpanTolerance;
        }

        /// <summary>
        /// Katman soyma (video kuralı): dış katman kapıda "emilir", blok İÇ
        /// rengiyle tahtada kalır ve sürükleme sona erer.
        ///
        /// DÜZELTME (bölüm 21 analizi): Sayaçlar blok ÇIKIŞINI değil, kapıdaki
        /// EMİLİM olayını sayar — soyulma da sayar. Referansta bölüm 21'in
        /// perde sayaçları (6/13/15) yalnız tam çıkışlarla asla dolmuyordu;
        /// soyulmaları da sayınca birebir tutuyor. Katmanlı blokların bulunduğu
        /// her bölümün buz/perde bütçesi bu kurala bağlı.
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
            _obstacles.NotifyBlockExit();          // emilim sayılır: buz erir, perde sayar
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

                // Bloğun merkezi kapı çizgisine tam oturana kadar ilerlesin;
                // daha fazlası duvarın üstünden geçmesine yol açar.
                float travel = (gate.EdgeHorizontal ? block.H : block.W) * 0.5f + 0.2f;
                view.PlayAbsorb(dir, travel, _config.absorbDuration);
            }

            _events.RaiseBlockAbsorbed(block, gate);

            // Çıkış zinciri: buzlar erir, perdeler sayar (belki içerik doğar)...
            _obstacles.NotifyBlockExit();
            // ...renk mevcudiyeti değişti — ghost/kuyruk durumlarını tazele.
            RecomputeGateStates();

            CheckCleared();
        }

        /// <summary>
        /// Tahta boşaldı mı? Boşaldıysa zafer olayını tetikler.
        ///
        /// DERS (bitiş koşulu TEK yerde ve HER yoldan kontrol edilmeli):
        /// Bu kontrol yalnız <see cref="Absorb"/> içindeydi, yani zafer sadece
        /// blok KAPIDAN çıkarsa görülüyordu. Son bloğu roketle ya da UFO ile
        /// silen oyuncu tahtayı boşaltıyor ama bölüm bitmiyordu — süre dolana
        /// kadar boş tahtaya bakıyordu. Yardımcılar oyunun meşru bir parçası;
        /// bitişi yalnız bir yola bağlamak, diğer yolları sessizce bozuyor.
        ///
        /// Bir kez tetiklenir: aynı karede iki blok birden silinirse olay
        /// ikinci kez atılmasın.
        /// </summary>
        public void CheckCleared()
        {
            if (_cleared) return;
            if (_level.Blocks.Count > 0 || _level.HasPendingContent()) return;

            _cleared = true;
            _events.RaiseBoardCleared();
        }

        bool _cleared;

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
