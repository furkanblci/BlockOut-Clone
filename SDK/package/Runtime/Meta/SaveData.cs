using System.Collections.Generic;
using GameKit.Meta;
using GameKit.Save;
using Newtonsoft.Json;

namespace GameKit.Meta
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

        /// <summary>Günlük ödül zinciri (1-7) ve son alım tarihi (UTC).</summary>
        [JsonProperty("dailyStreak")] public int DailyStreak;
        [JsonProperty("dailyLastClaimUtc")] public string DailyLastClaimUtc;

        /// <summary>-1 = henüz kurulmadı; LivesService ilk çalıştığında doldurur.</summary>
        [JsonProperty("lives")] public int LivesField = -1;

        [JsonProperty("nextLifeAtUtc")] public string NextLifeAtUtcField = "";

        /// <summary>
        /// Üst sınırı aşıp BEKLEYEN ödül canları. Eski kayıtlarda bu alan yok;
        /// Newtonsoft eksik alanı varsayılanla (0) doldurduğu için geriye
        /// dönük uyumluluk kendiliğinden sağlanıyor — sürüm yükseltmeye gerek
        /// yok (bkz. sınıf başındaki "her alanın makul bir varsayılanı var").
        /// </summary>
        [JsonProperty("bankedLives")] public int BankedLivesField;

        [JsonIgnore]
        public int Lives { get => LivesField; set => LivesField = value; }

        [JsonIgnore]
        public string NextLifeAtUtc { get => NextLifeAtUtcField; set => NextLifeAtUtcField = value; }

        [JsonIgnore]
        public int BankedLives { get => BankedLivesField; set => BankedLivesField = value; }

        /// <summary>Bölüm kayıtları; anahtar = bölüm id'si (level_003 gibi).</summary>
        [JsonProperty("levels")] public Dictionary<string, LevelRecord> Levels
            = new Dictionary<string, LevelRecord>();

        /// <summary>Açılan en yüksek bölüm sırası (0 tabanlı).</summary>
        [JsonProperty("highestUnlockedIndex")] public int HighestUnlockedIndex;

        /// <summary>Yardımcı envanteri: "clock"/"rocket"/"ufo" -> adet.</summary>
        [JsonProperty("powerUps")] public Dictionary<string, int> PowerUps
            = new Dictionary<string, int>();

        /// <summary>
        /// Mağaza paketlerinin verdiği kalıcı "reklam yok" hakkı.
        /// Referanstaki her pakette üstü çizili ADS rozeti var ve süresi yazmıyor
        /// — süreli olanların (∞ can) yanında hep bir süre yazdığına göre bu
        /// kalıcı.
        /// </summary>
        [JsonProperty("noAds")] public bool NoAds;

        /// <summary>
        /// Yolculuk ekranında ÖDÜLÜ ALINMIŞ kilometre taşlarının seviyeleri
        /// (J1, 14. tur).
        ///
        /// NEDEN LİSTE: "en yüksek alınan seviye" tek bir sayı olarak
        /// tutulabilirdi ama kilometre taşları ileride araya eklenebiliyor.
        /// Sayıyla tutulsaydı, 30 ile 45 arasına yeni bir taş konduğunda 45'i
        /// almış oyuncu yeni taşı hiç alamazdı. Liste her taşı bağımsız
        /// izliyor.
        ///
        /// NEDEN GEREKLİ: Ödül olmadan da ekran çalışıyordu — tik çıkıyor,
        /// boru uzuyordu. Ama kullanıcının istediği şey görüntü değil işleyiş:
        /// *"belirli levellere gelince kalp coin arkaplan görseli veriyor ya,
        /// bunların hepsini açabilelim kullanabilelim çalışsın yani."*
        /// </summary>
        [JsonProperty("journeyClaimed")] public List<int> JourneyClaimed
            = new List<int>();

        /// <summary>
        /// Ana ekranda KULLANILAN arka plan (J1, 14. tur).
        /// 0 = varsayılan manzara, 1..5 = yolculuk bölgesinin görseli.
        ///
        /// Kullanıcı: *"belirli levellere gelince kalp coin ARKA PLAN GÖRSELİ
        /// veriyor ya, bunların hepsini AÇABİLELİM KULLANABİLELİM."* Ödülün
        /// açılması yetmiyor; seçilebilmesi de gerekiyor, yoksa kazanılan şey
        /// görünmez kalıyor.
        ///
        /// Kilit AYRICA saklanmıyor: bir bölgenin arka planı, o bölge
        /// tamamlandıysa açıktır. Tek kaynak `HighestUnlockedIndex`; ikinci
        /// bir liste tutmak, iki kaydın birbirinden ayrı düşmesi demekti.
        /// </summary>
        /// VARSAYILAN 0 = oyunun kendi manzarası (`bg_menu`).
        ///
        /// GERİ ALINDI (14. tur): H4 için varsayılan bir süre 2 ("mor gece")
        /// yapılmıştı. Küçük önizlemelerde iyi duruyordu ama OYUNDA tam
        /// ekranda felaketti: bölge görselleri yolculuk diskleri için DAİRE
        /// çizilmiş, tam ekran arka plan olarak konunca köşeleri kavisli
        /// kalıyor; kavisi gizlemek için uygulanan büyütme de sahneyi
        /// kaydırıp içindeki karakterleri devleştiriyordu.
        ///
        /// DERS (küçük önizleme, tam ekranın yerine geçmez): Altı seçeneği
        /// 300 piksel genişliğinde şeritler hâlinde yan yana koyup karar
        /// verdim. O ölçekte kusur görünmüyordu. Bir arka plan kararı, arka
        /// planın ÇİZİLECEĞİ boyutta verilmeli.
        [JsonProperty("background")] public int Background;

        /// <summary>Sınırsız can hakkının bitiş anı (UTC, ISO-8601). Boş = hak yok.</summary>
        [JsonProperty("infiniteLivesUntilUtc")] public string InfiniteLivesUntilUtc = "";

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
            if (data.PowerUps == null) data.PowerUps = new Dictionary<string, int>();
            if (data.PlayerName == null) data.PlayerName = "";
            if (data.NextLifeAtUtcField == null) data.NextLifeAtUtcField = "";
            if (data.InfiniteLivesUntilUtc == null) data.InfiniteLivesUntilUtc = "";
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
        [JsonProperty("notifications")] public bool Notifications = true;
        [JsonProperty("sounds")]  public bool Sounds = true;
        [JsonProperty("music")]   public bool Music = true;
        [JsonProperty("haptics")] public bool Haptics = true;
    }
}
