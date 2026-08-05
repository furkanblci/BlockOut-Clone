using System.Collections.Generic;
using GameKit.Meta;
using GameKit.Save;
using Newtonsoft.Json;

namespace BlockOut.Core.Save
{
    /// <summary>
    /// Bu OYUNUN diske yazılan durumu.
    ///
    /// DERS (kayıt da bir şemadır): LevelData gibi bu da sürümlü bir DTO'dur.
    /// Level şeması bozulursa en fazla bir bölüm açılmaz; KAYIT şeması bozulursa
    /// oyuncunun aylarca biriktirdiği ilerleme gider. Bu yüzden her alanın makul
    /// bir varsayılanı var — eksik alan çökme sebebi değil.
    ///
    /// DERS (arayüzler neden burada?): <see cref="IVersionedSave"/> ve
    /// <see cref="ILivesState"/> GameKit'ten geliyor. Kit "sürüm numarası" ve
    /// "can + dolum anı" dışında bu sınıfın hiçbir alanını bilmiyor; bu yüzden
    /// aynı kit Match-3'te de, koşu oyununda da çalışıyor. Bağımlılık oyundan
    /// kite doğru akıyor.
    /// </summary>
    public sealed class SaveData : IVersionedSave, ILivesState
    {
        public const int CurrentVersion = 1;

        [JsonProperty("version")] public int VersionField = CurrentVersion;

        [JsonIgnore]
        public int Version { get => VersionField; set => VersionField = value; }

        /// <summary>Oyuncunun adı (videodaki "Enter Name" ekranı). Boşsa henüz sorulmadı.</summary>
        [JsonProperty("playerName")] public string PlayerName = "";

        [JsonProperty("coins")] public int Coins;

        /// <summary>-1 = henüz kurulmadı; LivesService ilk çalıştığında doldurur.</summary>
        [JsonProperty("lives")] public int LivesField = -1;

        [JsonProperty("nextLifeAtUtc")] public string NextLifeAtUtcField = "";

        [JsonIgnore]
        public int Lives { get => LivesField; set => LivesField = value; }

        [JsonIgnore]
        public string NextLifeAtUtc { get => NextLifeAtUtcField; set => NextLifeAtUtcField = value; }

        /// <summary>Bölüm kayıtları; anahtar = bölüm id'si (level_003 gibi).</summary>
        [JsonProperty("levels")] public Dictionary<string, LevelRecord> Levels
            = new Dictionary<string, LevelRecord>();

        /// <summary>Açılan en yüksek bölüm sırası (0 tabanlı).</summary>
        [JsonProperty("highestUnlockedIndex")] public int HighestUnlockedIndex;

        [JsonProperty("settings")] public SettingsData Settings = new SettingsData();

        /// <summary>Son kaydın yazıldığı an — teşhis için.</summary>
        [JsonProperty("savedAtUtc")] public string SavedAtUtc = "";

        /// <summary>
        /// Eski kaydı güncel şemaya taşır. Şu an v1 tek sürüm olduğu için
        /// yapacak bir şey yok; sonraki sürümler buraya adım ekleyecek.
        /// </summary>
        public static void Upgrade(SaveData data)
        {
            if (data.Version >= CurrentVersion) return;
            data.Version = CurrentVersion;
        }

        /// <summary>Elle kurcalanmış ya da yarım kalmış kayıtları savunmaya alır.</summary>
        public static void Normalize(SaveData data)
        {
            if (data.Levels == null) data.Levels = new Dictionary<string, LevelRecord>();
            if (data.Settings == null) data.Settings = new SettingsData();
            if (data.PlayerName == null) data.PlayerName = "";
            if (data.NextLifeAtUtcField == null) data.NextLifeAtUtcField = "";
            if (data.Coins < 0) data.Coins = 0;
            if (data.HighestUnlockedIndex < 0) data.HighestUnlockedIndex = 0;
        }
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
}
