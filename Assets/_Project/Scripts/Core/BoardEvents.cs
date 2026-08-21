using System;

namespace BlockOut.Core
{
    /// <summary>
    /// Tahta olaylarının merkezi.
    ///
    /// DERS (olay merkezli gevşek bağlılık): GateSystem "blok emildi" der ve
    /// KİMİN dinlediğini bilmez; GameSession kazanma kontrolü için, M4'te ses
    /// ve partikül servisleri efekt için AYNI olaya abone olur. Yeni dinleyici
    /// eklemek mevcut kodu değiştirmez. Statik event yerine örnek (instance)
    /// kullanıyoruz: her bölüm kurulumunda taze bir kopya yaratılır, böylece
    /// "önceki oyundan kalma bayat abone" sınıfı hatalar kökten yok olur.
    ///
    /// M2'de eklenecek: LayerPeeled, IceDecremented, IceShattered, GateAdvanced.
    /// </summary>
    public sealed class BoardEvents
    {
        public event Action<BlockModel, GateModel> BlockAbsorbed;
        public event Action<BlockModel, GateModel> LayerPeeled;
        public event Action<BlockModel> IceDecremented;
        public event Action<BlockModel> IceShattered;
        public event Action<GateModel> GateIceDecremented;
        public event Action<GateModel> GateIceShattered;
        public event Action<GateModel> GateGhosted;
        public event Action<GateModel> GateAdvanced;
        public event Action<CurtainModel> CurtainDecremented;
        public event Action<CurtainModel> CurtainOpened;
        /// <summary>
        /// Tahta boşaldı. Parametre, zaferin GÖSTERİLMESİ için beklenmesi
        /// gereken süre: son blok kapıdan geçerken ve kapı solarken paneli
        /// açmak, oyuncuya son hamlesini hiç göstermiyordu.
        /// </summary>
        public event Action<float> BoardCleared;

        public void RaiseBlockAbsorbed(BlockModel block, GateModel gate) =>
            BlockAbsorbed?.Invoke(block, gate);

        public void RaiseLayerPeeled(BlockModel block, GateModel gate) =>
            LayerPeeled?.Invoke(block, gate);

        public void RaiseIceDecremented(BlockModel block) => IceDecremented?.Invoke(block);

        public void RaiseIceShattered(BlockModel block) => IceShattered?.Invoke(block);

        public void RaiseGateIceDecremented(GateModel gate) => GateIceDecremented?.Invoke(gate);

        public void RaiseGateIceShattered(GateModel gate) => GateIceShattered?.Invoke(gate);

        public void RaiseGateGhosted(GateModel gate) => GateGhosted?.Invoke(gate);

        public void RaiseGateAdvanced(GateModel gate) => GateAdvanced?.Invoke(gate);

        public void RaiseCurtainDecremented(CurtainModel curtain) =>
            CurtainDecremented?.Invoke(curtain);

        public void RaiseCurtainOpened(CurtainModel curtain) => CurtainOpened?.Invoke(curtain);

        /// <summary>Üreteç tahtaya yeni bir blok itti.</summary>
        public event System.Action<BlockModel> BlockSpawned;

        public void RaiseBlockSpawned(BlockModel block) => BlockSpawned?.Invoke(block);

        public void RaiseBoardCleared(float showDelay = 0f) =>
            BoardCleared?.Invoke(showDelay);
    }
}
