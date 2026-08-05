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
    ///
    /// DERS (dikdörtgenden POLYOMINO'ya): Referans oyunda bloklar yalnızca
    /// w×h dikdörtgen değil; L ve T şekilleri de var. Bu yüzden blok artık
    /// bir HÜCRE LİSTESİ taşır. Dikdörtgen, bu listenin dolu olduğu özel bir
    /// hâlden ibarettir — yani tek bir kod yolu hem eskisini hem yenisini
    /// karşılar. Şekli "dikdörtgen + istisna" diye modellemek, her yeni şekil
    /// türünde koda dallanma eklemek demek olurdu.
    /// </summary>
    public sealed class BlockModel
    {
        public int Id;

        /// <summary>Sınırlayıcı kutu genişliği (hücre).</summary>
        public int W = 1;

        /// <summary>Sınırlayıcı kutu yüksekliği (hücre).</summary>
        public int H = 1;

        /// <summary>
        /// Bloğu oluşturan hücrelerin sınırlayıcı kutu içindeki yerel konumları.
        /// Dikdörtgen bloklarda kutunun tamamı doludur.
        /// </summary>
        public readonly List<Vector2Int> Cells = new List<Vector2Int>();

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

        /// <summary>Sınırlayıcı kutu — kaba testler (imleç bloğun üstünde mi) için.</summary>
        public Aabb Bounds => Aabb.FromRect(Position.x, Position.y, W, H);

        /// <summary>Blok tam dikdörtgen mi? (Mesh ve çizim kısayolları için.)</summary>
        public bool IsRectangle => Cells.Count == W * H;

        /// <summary>Sınırlayıcı kutuyu tamamen dolduran hücre listesi kurar.</summary>
        public void SetRectangle(int width, int height)
        {
            W = width;
            H = height;
            Cells.Clear();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    Cells.Add(new Vector2Int(x, y));
        }

        /// <summary>
        /// Hücre listesinden kurar; sınırlayıcı kutu hücrelerden hesaplanır ve
        /// hücreler sol-üst köşeye göre normalize edilir.
        /// </summary>
        public void SetCells(IEnumerable<Vector2Int> cells)
        {
            Cells.Clear();
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var cell in cells)
            {
                Cells.Add(cell);
                if (cell.x < minX) minX = cell.x;
                if (cell.y < minY) minY = cell.y;
                if (cell.x > maxX) maxX = cell.x;
                if (cell.y > maxY) maxY = cell.y;
            }

            if (Cells.Count == 0) { SetRectangle(1, 1); return; }

            for (int i = 0; i < Cells.Count; i++)
                Cells[i] -= new Vector2Int(minX, minY);

            W = maxX - minX + 1;
            H = maxY - minY + 1;
        }

        /// <summary>Bloğun ŞU ANKİ konumundaki çarpışma kutuları — hücre başına bir tane.</summary>
        public void CollectColliders(List<Aabb> output)
        {
            foreach (var cell in Cells)
                output.Add(Aabb.FromRect(Position.x + cell.x, Position.y + cell.y, 1, 1));
        }

        /// <summary>Verilen hücre (tahta uzayında) bu bloğun üstünde mi?</summary>
        public bool CoversCell(int x, int y)
        {
            int bx = Mathf.RoundToInt(Position.x);
            int by = Mathf.RoundToInt(Position.y);
            foreach (var cell in Cells)
                if (bx + cell.x == x && by + cell.y == y) return true;
            return false;
        }
    }
}
