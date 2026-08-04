using System.Collections.Generic;

namespace BlockOut.Core
{
    /// <summary>
    /// Level JSON şemasının sürüm geçişleri.
    ///
    /// DERS (şema evrimi): Veri odaklı bir oyunda içerik koddan uzun yaşar.
    /// Şema değiştiğinde eski bölümleri elle düzeltmek ölçeklenmez; bunun
    /// yerine "yükle → gerekiyorsa yükselt → kullan" zinciri kurulur.
    /// Aynı desen M5'teki kayıt dosyası (save.json) için de kullanılacak.
    ///
    /// Kural: yükseltme İLERİ yönlüdür ve kayıpsızdır. Daha yeni sürümle
    /// yazılmış bir dosya reddedilir — sessizce yanlış yorumlamaktansa
    /// açıkça hata vermek yeğdir.
    /// </summary>
    public static class LevelMigration
    {
        public const int CurrentVersion = 1;

        /// <summary>
        /// Veriyi güncel şemaya taşır. Değişiklik yapıldıysa true döner ve
        /// yapılanları <paramref name="notes"/> listesine yazar.
        /// </summary>
        public static bool Upgrade(LevelData data, List<string> notes)
        {
            bool changed = false;

            // v0 = "version" alanı olmayan ilk elle yazılmış dosyalar.
            if (data.Version <= 0)
            {
                data.Version = 1;
                if (data.TimeSeconds <= 0) data.TimeSeconds = 120;
                if (string.IsNullOrEmpty(data.Difficulty)) data.Difficulty = "normal";
                notes?.Add("Sürüm 0 → 1: eksik metadata varsayılanlarla dolduruldu.");
                changed = true;
            }

            // Gelecek geçişler buraya EKLENİR, öncekiler silinmez:
            // if (data.Version == 1) { ...; data.Version = 2; changed = true; }

            // Boş listeler null gelmiş olabilir (elle düzenlenmiş dosyalar).
            if (data.Blocks == null) { data.Blocks = new List<BlockData>(); changed = true; }
            if (data.Gates == null) { data.Gates = new List<GateData>(); changed = true; }
            if (data.Obstacles == null) { data.Obstacles = new List<ObstacleData>(); changed = true; }
            if (data.Board != null && data.Board.Walls == null)
            {
                data.Board.Walls = new List<WallData>();
                changed = true;
            }

            return changed;
        }

        /// <summary>Dosya bu yapıdan daha yeni bir sürümle mi yazılmış?</summary>
        public static bool IsFromFuture(LevelData data) => data.Version > CurrentVersion;
    }
}
