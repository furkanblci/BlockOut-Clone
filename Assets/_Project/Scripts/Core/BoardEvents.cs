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
        public event Action BoardCleared;

        public void RaiseBlockAbsorbed(BlockModel block, GateModel gate) =>
            BlockAbsorbed?.Invoke(block, gate);

        public void RaiseBoardCleared() => BoardCleared?.Invoke();
    }
}
