using System.Collections.Generic;

namespace BlockOut.Core
{
    public enum LevelDifficulty
    {
        Normal,
        Hard,
        SuperHard
    }

    /// <summary>
    /// Bir bölümün zorluğunu İÇERİĞİNDEN çıkarır.
    ///
    /// DERS (elle etiket yerine türetilen etiket): Zorluğu her bölüm dosyasına
    /// elle yazmak iki sorun doğurur — elli dosyada tutarlılık kalmaz, ve bir
    /// bölümü düzenleyip zorlaştırdığında etiketi güncellemeyi unutursun.
    /// Zorluk, bölümün zaten sahip olduğu şeylerden (renk sayısı, engel sayısı,
    /// çok hücreli blok sayısı, kapı sayacı) hesaplanırsa kendi kendini günceller.
    ///
    /// Ağırlıklar oyunun kendi mantığından geliyor: bir renk eklemek bölümü
    /// belirgin biçimde zorlaştırır (planlanacak sıra artar), buz/perde bir
    /// hamle sınırı getirir, üretici makinesi tahtayı sürekli yeniden doldurur.
    /// Tek hücreli fazladan bir blok ise neredeyse hiçbir şey değiştirmez.
    /// </summary>
    public static class LevelDifficultyRule
    {
        // Eşikler GÖZ KARARI DEĞİL, mevcut 50 bölümün puan dağılımından seçildi:
        // min 5, %25 → 14, medyan 20, %70 → 23, %88 → 27, max 32.
        // İlk denemede 9 ve 15 verilmişti; sonuç 50 bölümün 37'sinin "Çok Zor"
        // çıkması oldu. Her şey zorsa etiket bilgi taşımaz — zorluk etiketi
        // ancak AZINLIKTA olduğunda bir şey söyler. Bu değerler kabaca
        // %70 normal / %18 zor / %12 çok zor veriyor.
        //
        // Bölüm seti karakter değiştirirse (ör. üretici makinesi yaygınlaşırsa)
        // dağılım yeniden ölçülüp bu iki sayı güncellenmeli.
        public const int HardThreshold = 23;
        public const int SuperHardThreshold = 28;

        public static LevelDifficulty Of(LevelModel level)
        {
            int score = Score(level);
            if (score >= SuperHardThreshold) return LevelDifficulty.SuperHard;
            if (score >= HardThreshold) return LevelDifficulty.Hard;
            return LevelDifficulty.Normal;
        }

        public static int Score(LevelModel level)
        {
            if (level == null) return 0;

            var colors = new HashSet<BlockColor>();
            int layered = 0, polyomino = 0, iced = 0;

            foreach (var block in level.Blocks)
            {
                if (block.Layers != null && block.Layers.Count > 0)
                {
                    foreach (var layer in block.Layers) colors.Add(layer);
                    if (block.Layers.Count > 1) layered++;
                }
                if (block.Cells != null && block.Cells.Count > 1) polyomino++;
                if (block.IceCount > 0) iced++;
            }

            int curtains = 0, generators = 0, gateIce = 0;
            foreach (var obstacle in level.Obstacles)
            {
                if (obstacle is CurtainModel) curtains++;
                else if (obstacle is GeneratorModel) generators++;
            }
            foreach (var gate in level.Gates)
                if (gate.IceCount > 0) gateIce++;

            return colors.Count * 2
                 + layered
                 + polyomino / 2
                 + iced
                 + gateIce
                 + curtains * 2
                 + generators * 3;
        }

        public static string Label(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:      return "Zor Seviye";
                case LevelDifficulty.SuperHard: return "Çok Zor Seviye";
                default:                        return "";
            }
        }

        /// <summary>Zor bölümler daha çok ödül verir — referanstaki "Ödüller x3".</summary>
        public static int RewardMultiplier(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard:      return 2;
                case LevelDifficulty.SuperHard: return 3;
                default:                        return 1;
            }
        }
    }
}
