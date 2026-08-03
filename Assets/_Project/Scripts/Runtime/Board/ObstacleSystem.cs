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
        }

        void OpenCurtain(CurtainModel curtain)
        {
            if (_views.Curtains.TryGetValue(curtain, out var view))
            {
                _views.Curtains.Remove(curtain);
                view.Open();
            }

            // Gizli içerik tahtaya doğar — artık normal (gerekirse buzlu) bloklar.
            foreach (var block in curtain.Contents)
            {
                _level.Blocks.Add(block);
                _views.Blocks[block] = BlockView.Create(
                    _views.BlockRoot, block, _space,
                    BoardBuilder.GetBlockMaterial(_palette, block.CurrentColor));
            }
            curtain.Contents.Clear();

            _events.RaiseCurtainOpened(curtain);
        }
    }
}
