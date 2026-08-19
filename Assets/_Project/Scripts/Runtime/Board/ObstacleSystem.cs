using System.Collections.Generic;
using UnityEngine;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.View;

namespace BlockOut.Runtime.Board
{
    /// <summary>
    /// "Bir blok tahtadan çıktı" olayının zincirleme etkileri: TÜM buz
    /// sayaçları (blok + kapı) ve perde sayaçları 1 azalır (video kuralı);
    /// sıfırlananlar kırılır/açılır. GateSystem her emilmeden sonra çağırır.
    /// </summary>
    public sealed class ObstacleSystem
    {
        readonly LevelModel _level;
        readonly BoardViews _views;
        readonly ColorPaletteSO _palette;
        readonly BoardEvents _events;
        readonly BoardSpace _space;

        public ObstacleSystem(
            LevelModel level, BoardViews views, ColorPaletteSO palette,
            BoardEvents events, BoardSpace space)
        {
            _level = level;
            _views = views;
            _palette = palette;
            _events = events;
            _space = space;
        }

        /// <summary>
        /// Bölüm başında bir kez çağrılır: üreteçler ilk bloklarını iter.
        /// (Görselli kurulumda BoardBuilder, görselsiz doğrulamada araç çağırır.)
        /// </summary>
        public void Start() => PumpGenerators();

        public void NotifyBlockExit()
        {
            // Blok buzları
            foreach (var block in _level.Blocks)
            {
                if (block.IceCount <= 0) continue;
                block.IceCount--;

                _views.Blocks.TryGetValue(block, out var view);
                if (block.IceCount == 0)
                {
                    if (view != null) view.ShatterIce();
                    _events.RaiseIceShattered(block);
                }
                else
                {
                    if (view != null) view.UpdateIceCount();
                    _events.RaiseIceDecremented(block);
                }
            }

            // Kapı buzları
            foreach (var gate in _level.Gates)
            {
                if (gate.IceCount <= 0) continue;
                gate.IceCount--;

                _views.Gates.TryGetValue(gate, out var view);
                if (gate.IceCount == 0)
                {
                    if (view != null) view.RevealColor();
                    _events.RaiseGateIceShattered(gate);
                }
                else
                {
                    if (view != null) view.UpdateIceCount();
                    _events.RaiseGateIceDecremented(gate);
                }
            }

            // Perdeler ve gelecekteki diğer engeller
            foreach (var obstacle in _level.Obstacles)
            {
                if (!(obstacle is CurtainModel curtain))
                {
                    obstacle.OnBlockExit();
                    continue;
                }

                if (!curtain.OnBlockExit()) continue;

                if (curtain.IsOpen)
                    OpenCurtain(curtain);
                else
                {
                    if (_views.Curtains.TryGetValue(curtain, out var view))
                        view.UpdateCount();
                    _events.RaiseCurtainDecremented(curtain);
                }
            }

            // Yer açıldı: üreteçler sıradaki bloklarını itebilir.
            PumpGenerators();
        }

        /// <summary>
        /// Yeri olan her üretece sıradaki bloğu ittirir. Bir blok doğunca
        /// başka bir üretecin girişi de açılmış olabileceği için değişiklik
        /// kalmayana kadar döner (küçük sayıda üreteç, ucuz döngü).
        /// </summary>
        public void PumpGenerators()
        {
            bool changed;
            int guard = 0;
            do
            {
                changed = false;
                foreach (var obstacle in _level.Obstacles)
                    if (obstacle is GeneratorModel generator && TrySpawn(generator))
                        changed = true;
            }
            while (changed && ++guard < 64);

            RefreshGeneratorLamps();
        }

        /// <summary>
        /// Her makinenin KIRMIZI/YEŞİL lambasını tazeler (4. tur, L43).
        ///
        /// Kullanıcı makinede "kırmızı/yeşil yanma göstergesi" istedi;
        /// referansta makinenin başında küçük bir ışık var.
        ///
        /// DERS (durumu hesaplayan taraf, göstermesi gereken taraftır):
        /// Görsel katman "sıradaki blok sığıyor mu" sorusunu cevaplayamaz —
        /// o bilgi tahtanın doluluğunda. Bu yüzden lambayı kuran değil,
        /// itmeyi DENEYEN sistem yakıyor. Aynı `IsAreaFree` çağrısı hem itme
        /// kararını hem lambayı besliyor; iki ayrı hesap yapılsaydı biri
        /// güncellenip diğeri unutulurdu.
        /// </summary>
        void RefreshGeneratorLamps()
        {
            if (_views == null) return;

            foreach (var obstacle in _level.Obstacles)
            {
                if (!(obstacle is GeneratorModel generator)) continue;
                if (!_views.Generators.TryGetValue(generator, out var view) || view == null)
                    continue;

                bool ready = false;
                if (!generator.IsEmpty)
                {
                    var block = generator.Queue[0];
                    var saved = block.Position;
                    block.Position = EntryPosition(generator, block);
                    ready = IsAreaFree(block);
                    block.Position = saved;
                }
                view.SetReady(ready);
            }
        }

        bool TrySpawn(GeneratorModel generator)
        {
            if (generator.IsEmpty) return false;

            var block = generator.Queue[0];
            block.Position = EntryPosition(generator, block);
            if (!IsAreaFree(block)) return false;

            generator.Queue.RemoveAt(0);
            _level.Blocks.Add(block);

            if (_views.BlockRoot != null)
            {
                _views.Blocks[block] = BlockView.Create(
                    _views.BlockRoot, block, _space,
                    BoardBuilder.GetBlockMaterial(_palette, block.CurrentColor));
                if (_views.Generators.TryGetValue(generator, out var view))
                {
                    view.UpdateQueue();
                    // Pencere sıradaki bloğun rengini gösteriyor; kuyruk
                    // ilerleyince o renk de değişiyor.
                    if (!generator.IsEmpty)
                        view.SetNextMaterial(BoardBuilder.GetBlockMaterial(
                            _palette, generator.Queue[0].CurrentColor));
                }
            }

            _events.RaiseBlockSpawned(block);
            return true;
        }

        /// <summary>Bloğun kenardan girdiği ilk konum — makinenin hizasına oturur.</summary>
        Vector2 EntryPosition(GeneratorModel generator, BlockModel block)
        {
            switch (generator.Side)
            {
                case Side.West:  return new Vector2(0, generator.Y);
                case Side.East:  return new Vector2(_level.Board.Width - block.W, generator.Y);
                case Side.North: return new Vector2(generator.X, 0);
                default:         return new Vector2(generator.X, _level.Board.Height - block.H);
            }
        }

        /// <summary>Bloğun hücrelerinin hepsi boş ve oynanabilir mi?</summary>
        bool IsAreaFree(BlockModel block)
        {
            _spawnProbe.Clear();
            _level.CollectObstacles(_spawnProbe, block);

            _spawnCells.Clear();
            block.CollectColliders(_spawnCells);

            foreach (var cell in _spawnCells)
                foreach (var other in _spawnProbe)
                    if (cell.Overlaps(other, 0.001f)) return false;

            return true;
        }

        readonly List<Aabb> _spawnProbe = new List<Aabb>();
        readonly List<Aabb> _spawnCells = new List<Aabb>();

        void OpenCurtain(CurtainModel curtain)
        {
            if (_views.Curtains.TryGetValue(curtain, out var view))
            {
                _views.Curtains.Remove(curtain);
                view.Open();
            }

            // Gizli içerik tahtaya doğar — artık normal (gerekirse buzlu) bloklar.
            // BlockRoot yoksa görselsiz (headless) koşuyoruz: level doğrulama aracı
            // ve testler modeli sahne kurmadan sürer. Model/view ayrımının karşılığı.
            // BLOKLAR SIRAYLA DÜŞEREK GELİYOR (4. tur, I36).
            //
            // Kullanıcı: "Perde kalkma animasyonu ve blokların geliş şekli
            // birebir yapılacak."
            //
            // Bloklar eskiden aynı karede, tam boyda beliriyordu; perde
            // kalkarken altından çıkanlar "hep oradaymış" gibi görünüyordu.
            // Bölüm açılışındaki giriş animasyonu (`PlayIntro`) zaten tam bu
            // işi yapıyor: yukarıdan düşüp yaylanarak oturuyor. Gecikme hücre
            // konumundan türetiliyor, yani sol üstteki önce geliyor — açılışla
            // aynı ritim.
            //
            // DERS (var olan bir hareketi ikinci kez yazma): Aynı "geliş"
            // hissini burada sıfırdan kurmak, iki ayrı eğri ve iki ayrı süre
            // demekti; biri değişince öbürü unutulurdu.
            int index = 0;
            foreach (var block in curtain.Contents)
            {
                _level.Blocks.Add(block);
                if (_views.BlockRoot != null)
                {
                    var blockView = BlockView.Create(
                        _views.BlockRoot, block, _space,
                        BoardBuilder.GetBlockMaterial(_palette, block.CurrentColor));
                    _views.Blocks[block] = blockView;

                    // Perde 0,42 saniyede kalkıyor; bloklar onun ardından
                    // geliyor ki ikisi üst üste binmesin.
                    blockView.PlayIntro(0.30f + index * 0.07f, 0.22f);
                    index++;
                }
            }
            curtain.Contents.Clear();

            _events.RaiseCurtainOpened(curtain);
        }
    }
}
