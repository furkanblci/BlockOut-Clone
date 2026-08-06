using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.View;
using UnityEngine;

namespace BlockOut.Runtime.Board
{
    /// <summary>Model → view eşlemeleri. Sistemler (drag, gate, obstacle) view'lara buradan ulaşır.</summary>
    public sealed class BoardViews
    {
        public readonly Dictionary<BlockModel, BlockView> Blocks =
            new Dictionary<BlockModel, BlockView>();

        public readonly Dictionary<GateModel, GateView> Gates =
            new Dictionary<GateModel, GateView>();

        public readonly Dictionary<CurtainModel, CurtainView> Curtains =
            new Dictionary<CurtainModel, CurtainView>();

        public readonly Dictionary<GeneratorModel, GeneratorView> Generators =
            new Dictionary<GeneratorModel, GeneratorView>();

        /// <summary>Perde açılınca doğan blok view'ları da buraya eklenir.</summary>
        public Transform BlockRoot;
    }
}
