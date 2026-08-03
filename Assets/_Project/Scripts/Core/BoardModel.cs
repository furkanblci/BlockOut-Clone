using System.Collections.Generic;

namespace BlockOut.Core
{
    /// <summary>
    /// Tahtanın statik durumu: hangi hücreler oynanabilir, hangi kenarlar duvarlı.
    /// Dikdörtgen olmayan tahtalar (L şekli, merdiven...) "sınır kutusu içinde
    /// oynanamaz hücreler" olarak temsil edilir — rows'taki '.' karakterleri.
    /// </summary>
    public sealed class BoardModel
    {
        public int Width;
        public int Height;

        /// <summary>İç duvar kenarları (normalize EdgeId'ler).</summary>
        public readonly HashSet<EdgeId> Walls = new HashSet<EdgeId>();

        bool[] _playable; // index = y * Width + x

        public BoardModel(int width, int height)
        {
            Width = width;
            Height = height;
            _playable = new bool[width * height];
        }

        public void SetPlayable(int x, int y, bool value) => _playable[y * Width + x] = value;

        /// <summary>Sınır kutusu dışı da "oynanamaz" sayılır — çağıran taşma derdi yaşamaz.</summary>
        public bool IsPlayable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return false;
            return _playable[y * Width + x];
        }

        /// <summary>
        /// Hareket etmeyen çarpışma kutularını listeye ekler:
        /// dış çerçeve (4 büyük AABB), oynanamaz hücreler ve iç duvarlar.
        /// Duvarlar SIFIR kalınlıklı AABB'dir — hücrelerin içine taşmadıkları
        /// için duvara bitişik blok kenar boyunca serbestçe kayabilir
        /// (epsilon'lu örtüşme testi bitişikliği çarpışma saymaz).
        /// </summary>
        public void CollectStaticColliders(List<Aabb> output)
        {
            const float m = 4f; // çerçeve kalınlığı — ekran dışına kaçışı da engeller

            output.Add(new Aabb(-m, -m, 0, Height + m));                 // sol
            output.Add(new Aabb(Width, -m, Width + m, Height + m));      // sağ
            output.Add(new Aabb(-m, -m, Width + m, 0));                  // üst
            output.Add(new Aabb(-m, Height, Width + m, Height + m));     // alt

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (!_playable[y * Width + x])
                        output.Add(Aabb.FromCell(x, y));

            foreach (var e in Walls)
            {
                output.Add(e.Horizontal
                    ? new Aabb(e.X, e.Y, e.X + 1, e.Y)      // yatay segment, kalınlık 0
                    : new Aabb(e.X, e.Y, e.X, e.Y + 1));    // dikey segment, kalınlık 0
            }
        }
    }
}
