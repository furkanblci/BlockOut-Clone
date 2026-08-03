using System.Collections.Generic;

namespace BlockOut.Core
{
    /// <summary>
    /// Bir kapının oyun durumu: hangi kenarda, ne uzunlukta, hangi renk(ler).
    /// Kenar geometrisi köşe-uzayı sayılarına çevrilerek sunulur ki GateSystem
    /// temas testini basit sayı karşılaştırmalarıyla yapabilsin.
    /// </summary>
    public sealed class GateModel
    {
        /// <summary>Kapının başladığı hücre ve kenarı (WallData ile aynı sözleşme).</summary>
        public int X;
        public int Y;
        public Side Side;
        public int Length = 1;

        /// <summary>
        /// Renk kuyruğu; index 0 = aktif renk. NOT (video, L1-20): oyunda kuyruk
        /// geçişi hiç gözlenmedi — kapılar tek renk taşıyor. Şema ileriye dönük
        /// (L21+) kuyruğu desteklediği için mantık yine de kuruludur.
        /// </summary>
        public List<BlockColor> ColorQueue = new List<BlockColor>();

        public BlockColor ActiveColor => ColorQueue[0];

        /// <summary>
        /// Buz kaplaması sayacı. Buzluyken kapı ÇALIŞMAZ ve rengi gizlidir
        /// (videodan: buz kırılınca renk "ortaya çıkar"). Her emilişte 1 azalır.
        /// </summary>
        public int IceCount;

        public bool IsIced => IceCount > 0;

        /// <summary>
        /// Rengi (gizli katmanlar ve perde içerikleri dahil) tükenen kapı
        /// soluklaşır ve bir daha emmez. GateSystem hesaplar.
        /// </summary>
        public bool IsGhost;

        /// <summary>Kuyrukta sıradaki renge geçer; geçilecek renk yoksa false.</summary>
        public bool AdvanceQueue()
        {
            if (ColorQueue.Count <= 1) return false;
            ColorQueue.RemoveAt(0);
            return true;
        }

        /// <summary>Kenar yatay mı (N/S) dikey mi (W/E)?</summary>
        public bool EdgeHorizontal => Side == Side.North || Side == Side.South;

        /// <summary>Kenar çizgisinin köşe-uzayı koordinatı (yataysa y, dikeyse x değeri).</summary>
        public float EdgeCoord
        {
            get
            {
                switch (Side)
                {
                    case Side.North: return Y;
                    case Side.South: return Y + 1;
                    case Side.West:  return X;
                    default:         return X + 1;
                }
            }
        }

        /// <summary>Kapı aralığının kenar boyunca başlangıcı (yataysa x, dikeyse y ekseni).</summary>
        public float SpanMin => EdgeHorizontal ? X : Y;

        public float SpanMax => SpanMin + Length;

        /// <summary>Hücre uzayında "dışarı" yönünün işareti: N ve W için -1, S ve E için +1.</summary>
        public int OutwardSign => Side == Side.North || Side == Side.West ? -1 : 1;
    }
}
