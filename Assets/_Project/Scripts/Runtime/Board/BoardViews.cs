using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.View;

namespace BlockOut.Runtime.Board
{
    /// <summary>Model → view eşlemeleri. Sistemler (drag, gate) view'lara buradan ulaşır.</summary>
    public sealed class BoardViews
    {
        public readonly Dictionary<BlockModel, BlockView> Blocks =
            new Dictionary<BlockModel, BlockView>();

        public readonly Dictionary<GateModel, GateView> Gates =
            new Dictionary<GateModel, GateView>();
    }
}
