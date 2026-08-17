namespace BlockOut.Core
{
    /// <summary>
    /// Oyuncunun bölüm içinde harcayabildiği yardımcılar (referans oyunun alt
    /// çubuğu). Fiyatlar jeton cinsindendir ve mağazadan alınabilir.
    ///
    /// DERS (yardımcı = kontrollü kaçış kapısı): Süreli bir bulmacada oyuncu
    /// tıkandığında oyunu bırakmasın diye üç farklı "kurtarma" sunulur; her biri
    /// FARKLI bir sıkışıklığı çözer — zaman baskısı (saat), tek bir tıkaç
    /// (roket), tüm bir rengin kilidi (UFO). Aynı işi yapan iki yardımcı
    /// olsaydı biri ölü içerik olurdu.
    /// </summary>
    public enum PowerUpKind
    {
        /// <summary>Çalar saat: süreyi bir süreliğine dondurur.</summary>
        Clock,

        /// <summary>Roket: seçilen TEK bloğu tahtadan siler.</summary>
        Rocket,

        /// <summary>UFO: seçilen rengin TÜM bloklarını siler.</summary>
        Ufo
    }

    public static class PowerUpInfo
    {
        /// <summary>Referans oyundaki jeton fiyatları.</summary>
        public static int Price(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Clock:  return 300;
                case PowerUpKind.Rocket: return 600;
                default:                 return 1200;
            }
        }

        /// <summary>Alt çubuktaki ad (referans oyun Türkçe).</summary>
        public static string Label(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Clock:  return "Alarm Clock";
                case PowerUpKind.Rocket: return "Rocket";
                default:                 return "UFO";
            }
        }

        /// <summary>Kullanınca ekranda çıkan yönerge; hedef seçmeyen için boş.</summary>
        public static string Prompt(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Rocket: return "Pick a block to remove.";
                case PowerUpKind.Ufo:    return "Pick a color to clear.";
                default:                 return "";
            }
        }

        /// <summary>Çalar saatin süreyi dondurduğu saniye.</summary>
        public const float ClockFreezeSeconds = 15f;
    }
}
