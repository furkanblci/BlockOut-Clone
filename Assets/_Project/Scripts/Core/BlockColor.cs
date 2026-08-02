using System;
using System.Collections.Generic;

namespace BlockOut.Core
{
    /// <summary>
    /// Oyundaki 8 blok/kapı rengi. Video analizinde doğrulanan palet.
    /// JSON dosyalarında küçük harfli isimler kullanılır ("red", "blue"...);
    /// dönüşüm için <see cref="BlockColorUtil"/> kullan.
    /// </summary>
    public enum BlockColor
    {
        Red,
        Blue,
        Yellow,
        Green,
        White,
        Black,
        Pink,
        Orange
    }

    public static class BlockColorUtil
    {
        // JSON'daki string -> enum eşlemesi. Dictionary tek sefer kurulur,
        // her level yüklemesinde tekrar tekrar string karşılaştırması yapılmaz.
        static readonly Dictionary<string, BlockColor> ById =
            new Dictionary<string, BlockColor>(StringComparer.OrdinalIgnoreCase)
            {
                { "red", BlockColor.Red },
                { "blue", BlockColor.Blue },
                { "yellow", BlockColor.Yellow },
                { "green", BlockColor.Green },
                { "white", BlockColor.White },
                { "black", BlockColor.Black },
                { "pink", BlockColor.Pink },
                { "orange", BlockColor.Orange }
            };

        public static bool TryParse(string id, out BlockColor color) =>
            ById.TryGetValue(id, out color);

        /// <summary>Enum -> JSON id ("Red" değil "red" yazılır).</summary>
        public static string ToId(this BlockColor color) => color.ToString().ToLowerInvariant();
    }
}
