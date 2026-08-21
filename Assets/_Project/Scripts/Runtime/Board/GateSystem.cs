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

            if (_views.Gates.TryGetValue(gate, out var peelGateView) && peelGateView != null)
                peelGateView.PlayAbsorbFlash();

            _events.RaiseLayerPeeled(block, gate);
            _obstacles.NotifyBlockExit();          // emilim sayılır: buz erir, perde sayar
            // Soyulmada da kapı hemen solmasın: soyulma da bir emilim
            // gösterisi (kupler, ışınlar) oynatıyor.
            RecomputeGateStates(FX.AbsorbTiming.GhostDelay);
        }

        void Absorb(BlockModel block, GateModel gate)
        {
            _level.RemoveBlock(block);

            // EMILIMIN SURESI BLOGUN DERINLIGINDEN GELIR.
            //
            // OLCUM (`Levels`, 4. bolum, 2 hucrelik mavi blok): temastan
            // bitise 14 kare = 0,235 sn -> hucre basina 0,118 sn. Eski hal
            // `absorbDuration` sabitini (0,25) her bloga uyguluyordu; 1x1 bir
            // parca da 2x4'luk bir kutle de ayni surede yutuluyordu ve kucuk
            // blok agir cekim gibi duruyordu.
            int depth = gate.EdgeHorizontal ? block.H : block.W;
            float duration = FX.AbsorbTiming.Duration(depth);

            _views.Gates.TryGetValue(gate, out var gateView);

            if (_views.Blocks.TryGetValue(block, out var view))
            {
                _views.Blocks.Remove(block);
                view.SetHighlight(false);

                Vector3 dir = gate.EdgeHorizontal
                    ? new Vector3(0f, 0f, -gate.OutwardSign)
                    : new Vector3(gate.OutwardSign, 0f, 0f);

                // Blok, gorunen son parcasi da duvarin arkasinda kalana kadar
                // ilerliyor; silueti zaten kapinin ic cizgisinde kirpiliyor,
                // yani "fazla" yol goze gorunmuyor.
                float travel = depth + 0.35f;

                // Kirpma cizgisi kapinin KENDI geometrisinden geliyor. Kapi
                // gorunumu yoksa (buzlu/ghost yolundan gelen bir durum)
                // kenarin kendisi kullaniliyor.
                Vector3 clipPoint = gateView != null
                    ? gateView.MouthWorldPoint
                    : view.transform.position + dir * (depth * 0.5f);

                view.PlayAbsorb(dir, clipPoint, travel, duration,
                                FX.AbsorbTiming.PreRoll);
            }

            // Kapi da tepki versin: yutma iki tarafli bir olay (4. tur G23).
            // Geri tepme blok ICERI GIRDIGI SURECE suruyor, o yuzden tepeye
            // cikis suresi emilimin suresi; blok girmeden onceki on-yukleme
            // kadar da gecikiyor ki kapi bloktan once kimildamasin.
            if (gateView != null)
                gateView.PlayAbsorbFlash(duration, FX.AbsorbTiming.PreRoll);

            _events.RaiseBlockAbsorbed(block, gate);

            // Çıkış zinciri: buzlar erir, perdeler sayar (belki içerik doğar)...
            _obstacles.NotifyBlockExit();
            // ...renk mevcudiyeti değişti — ghost/kuyruk durumlarını tazele.
            RecomputeGateStates(
                FX.AbsorbTiming.PreRoll + duration + FX.AbsorbTiming.GhostDelay);

            // Zafer, koreografi bitip kapı da solduktan sonra gösteriliyor.
            // Pay: sönme rutini `t < duration` ile bittiği için son kare tam
            // sınırda kalabiliyor; panel, kapı EKRANDAN GİTTİKTEN sonra
            // açılsın diye bir kare fazlası bekleniyor.
            CheckCleared(FX.AbsorbTiming.PreRoll + duration +
                         FX.AbsorbTiming.GhostDelay + FX.AbsorbTiming.GhostFade + 0.06f);
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
        public void CheckCleared() => CheckCleared(0f);

        /// <param name="showDelay">
        /// Zafer panelinin açılması için beklenecek süre.
        ///
        /// Kullanıcı: "son kapıya soktuğun bloğu görmeden level bitiyor."
        ///
        /// BULUNAN SEBEP: `_level.RemoveBlock` emilimin BAŞINDA çağrılıyor
        /// (mantık için doğru: blok o an tahtadan çıkmış sayılır), yani son
        /// blok daha kapıya girerken tahta mantıksal olarak boşalıyor ve
        /// panel açılıyordu. Blok kapıdan geçerken ekranda panel vardı.
        ///
        /// ÖLÇÜM (referans, 4. bölümün son mavi bloğu): blok kare 1746'da
        /// bitti, bölüm geçişi kare 1796'da başladı = 50 kare = 0,84 sn.
        /// Aradaki sürede kapı soluyor (1775-1794) — yani referans, zaferi
        /// KAPI SOLDUKTAN SONRA gösteriyor.
        ///
        /// DERS (mantığın bittiği yer, gösterinin bittiği yer değildir):
        /// Model "tahta boş" dediğinde ekranda hâlâ oynanacak yarım saniye
        /// var. İkisini aynı karede bağlamak, oyuncunun kendi son hamlesini
        /// görmesini engelliyordu.
        /// </param>
        public void CheckCleared(float showDelay)
        {
            if (_cleared) return;
            if (_level.Blocks.Count > 0 || _level.HasPendingContent()) return;

            _cleared = true;
            _events.RaiseBoardCleared(showDelay);
        }

        bool _cleared;

        /// <summary>
        /// Her kapı için: aktif renk oyunda (gizli katmanlar ve perde içerikleri
        /// dahil) kalmadıysa kuyruk varsa ilerler, yoksa kapı kalıcı ghost olur.
        /// Buzlu kapılar atlanır — buz kırılınca zaten yeniden hesaplanır.
        /// </summary>
        public void RecomputeGateStates() => RecomputeGateStates(0f);

        /// <param name="ghostDelay">
        /// Rengi tükenen kapının solmaya başlamadan önce bekleyeceği süre.
        /// Emilimden çağrıldığında koreografinin bitişine kadar bekliyor;
        /// bölüm kurulurken ya da yardımcılardan çağrıldığında beklemiyor
        /// (orada gösterilecek bir emilim yok).
        /// </summary>
        public void RecomputeGateStates(float ghostDelay)
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
                        view.SetGhost(ViewKit.GhostFor(_palette, gate.ActiveColor),
                                      ghostDelay);
                    _events.RaiseGateGhosted(gate);
                    break;
                }
            }
        }
    }
}
