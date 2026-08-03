using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Core
{
    /// <summary>
    /// Bir bloğun OYUN DURUMU — görseli değil (o BlockView'ın işi).
    ///
    /// DERS (model/view ayrımı): Model saf veridir ve hücre uzayında yaşar;
    /// view onu her karede dünya uzayına yansıtır. Sürükleme, çarpışma ve
    /// kapı temas kararlarının HEPSİ model üstünde verilir — bu sayede aynı
    /// mantık görsel olmadan EditMode testleriyle doğrulanabilir.
    /// </summary>
    public sealed class BlockModel
    {
        public int Id;
        public int W = 1;
        public int H = 1;

        /// <summary>Renk katmanları; index 0 = dış (aktif) katman. Tek katman = normal blok.</summary>
        public List<BlockColor> Layers = new List<BlockColor>();

        /// <summary>
        /// Buz sayacı. 0'dan büyükse blok buz içindedir: sürüklenemez, ama
        /// çarpışmaya normal katılır. Her blok emilişinde 1 azalır (videodan
        /// doğrulanan kural); 0'a inince buz kırılır, blok serbest kalır.
        /// </summary>
        public int IceCount;

        public bool IsFrozen => IceCount > 0;

        /// <summary>
        /// Sol-üst (min) köşenin hücre-uzayı konumu. Park halindeyken tam sayı;
        /// sürükleme sırasında serbest kayan noktalı değer alır.
        /// </summary>
        public Vector2 Position;

        public BlockColor CurrentColor => Layers[0];

        public Aabb Rect => Aabb.FromRect(Position.x, Position.y, W, H);
    }
}
