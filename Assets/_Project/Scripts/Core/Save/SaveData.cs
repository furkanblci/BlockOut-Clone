using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BlockOut.Core.Save
{
    /// <summary>
    /// Oyuncunun diske yazılan tüm durumu.
    ///
    /// DERS (kayıt da bir şemadır): LevelData gibi bu da sürümlü bir DTO'dur.
    /// Level şeması bozulursa en fazla bir bölüm açılmaz; KAYIT şeması bozulursa
    /// oyuncunun aylarca biriktirdiği ilerleme gider. Bu yüzden üç kural:
    /// (1) her alanın makul bir varsayılanı olur — eksik alan çökme sebebi değil,
    /// (2) sürüm numarası taşınır ve göç kodu tek yerdedir,
    /// (3) gelecekten gelen kayıt ASLA ezilmez (oyuncu yeni sürümde oynayıp eski
    ///     sürüme dönmüş olabilir).
    ///
    /// DERS (neden Newtonsoft, neden PlayerPrefs değil?): PlayerPrefs anahtar-değer
    /// bir kutudur; iç içe veri (bölüm başına kayıt) tutamaz, atomik yazmaz ve
    /// platforma göre registry/plist'e dağılır. Tek bir JSON dosyası hem
    /// okunabilir hem yedeklenebilir hem de tek seferde atomik yazılabilir.
    /// </summary>
    public sealed class SaveData
    {
        /// <summary>Şema sürümü; SaveMigration bunu okur.</summary>
        [JsonProperty("version")] public int Version = SaveMigration.CurrentVersion;

        /// <summary>Oyuncunun adı (videodaki "Enter Name" ekranı). Boşsa henüz sorulmadı.</summary>
        [JsonProperty("playerName")] public string PlayerName = "";

        [JsonProperty("coins")] public int Coins;

        /// <summary>Kalan can. Dolum mantığı LivesService'te.</summary>
        [JsonProperty("lives")] public int Lives = -1;   // -1 = "henüz kurulmadı", servis doldurur

        /// <summary>
        /// Bir sonraki canın dolacağı an (UTC, ISO-8601). Can dolu ise anlamsızdır.
        /// DERS: Zamanı SÜRE olarak değil AN olarak saklıyoruz — oyun kapalıyken
        /// geçen zamanı ancak "şu ana kadar ne kadar oldu" diye sorabiliriz.
        /// </summary>
        [JsonProperty("nextLifeAtUtc")] public string NextLifeAtUtc = "";

        /// <summary>Bölüm kayıtları; anahtar = bölüm id'si (level_003 gibi).</summary>
        [JsonProperty("levels")] public Dictionary<string, LevelRecord> Levels
            = new Dictionary<string, LevelRecord>();

        /// <summary>Açılan en yüksek bölüm sırası (0 tabanlı). -1 = hiç oynanmadı.</summary>
        [JsonProperty("highestUnlockedIndex")] public int HighestUnlockedIndex;

        [JsonProperty("settings")] public SettingsData Settings = new SettingsData();

        /// <summary>Son kaydın yazıldığı an — teşhis ve saat kurcalama tespiti için.</summary>
        [JsonProperty("savedAtUtc")] public string SavedAtUtc = "";
    }

    /// <summary>Tek bir bölümün oyuncu kaydı.</summary>
    public sealed class LevelRecord
    {
        [JsonProperty("cleared")] public bool Cleared;

        /// <summary>Bitirirken kalan en yüksek süre (saniye) — "en iyi" ölçüsü.</summary>
        [JsonProperty("bestRemainingSeconds")] public int BestRemainingSeconds;

        /// <summary>Videodaki PERFECT rozetini bir kez bile aldı mı?</summary>
        [JsonProperty("perfect")] public bool Perfect;

        [JsonProperty("attempts")] public int Attempts;
    }

    /// <summary>Videodaki Pause menüsünün üç anahtarı.</summary>
    public sealed class SettingsData
    {
        [JsonProperty("sounds")]  public bool Sounds = true;
        [JsonProperty("music")]   public bool Music = true;
        [JsonProperty("haptics")] public bool Haptics = true;
    }

    /// <summary>
    /// Kayıt şeması göçü. LevelMigration ile aynı desen: sürüm sürüm yükselt,
    /// gelecekten gelen kaydı reddet.
    /// </summary>
    public static class SaveMigration
    {
        public const int CurrentVersion = 1;

        /// <summary>Kayıt bu yapımdan YENİ bir sürümle yazılmış mı?</summary>
        public static bool IsFromFuture(SaveData data) => data.Version > CurrentVersion;

        /// <summary>Eski kaydı güncel şemaya taşır; yapılan işleri döndürür.</summary>
        public static List<string> Upgrade(SaveData data)
        {
            var steps = new List<string>();
            if (data.Version >= CurrentVersion) return steps;

            // v0 -> v1: ilk sürüm; alanların hepsi varsayılanlı olduğu için
            // yapılacak bir şey yok. Sonraki sürümler buraya adım ekleyecek.
            data.Version = CurrentVersion;
            steps.Add("v1'e taşındı");
            return steps;
        }

        /// <summary>ISO-8601 UTC metnini okur; bozuksa null.</summary>
        public static DateTime? ParseUtc(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            if (!DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal |
                    System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
                return null;
            return parsed;
        }

        /// <summary>DateTime'ı kayıt formatına çevirir (UTC, ISO-8601, kültürden bağımsız).</summary>
        public static string FormatUtc(DateTime utc) =>
            utc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
    }
}
