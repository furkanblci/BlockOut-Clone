using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BlockOut.Core
{
    /// <summary>
    /// Level JSON şemasının DTO'ları (Data Transfer Object).
    ///
    /// DERS (DTO deseni): Bu sınıflar SADECE veriyi taşır — davranış yok,
    /// Unity tipi yok. JSON'daki alan adlarıyla bire bir eşleşirler ve üç
    /// tüketici tarafından paylaşılırlar: runtime (LevelLoader), level editörü
    /// (M3) ve testler. Oyun mantığı bunları değil, bunlardan üretilen
    /// MODEL sınıflarını (BoardModel, BlockModel...) kullanır; böylece JSON
    /// şeması değişse bile oyun kodu tek noktadan (dönüşüm) etkilenir.
    ///
    /// DERS (neden Newtonsoft, neden JsonUtility değil?): JsonUtility
    /// polimorfik listeleri (obstacles: tipi "type" alanından anlaşılan
    /// karışık nesneler) ve Dictionary'yi desteklemez. Newtonsoft ise
    /// [JsonExtensionData] ile "şemada olmayan alanları da taşı" diyebilir —
    /// ObstacleData'ya bak.
    /// </summary>
    public sealed class LevelData
    {
        [JsonProperty("version")]       public int Version = 1;
        [JsonProperty("id")]            public string Id;
        [JsonProperty("displayNumber")] public int DisplayNumber;
        [JsonProperty("difficulty")]    public string Difficulty = "normal";
        [JsonProperty("timeSeconds")]   public int TimeSeconds = 180;
        [JsonProperty("board")]         public BoardData Board;
        [JsonProperty("blocks")]        public List<BlockData> Blocks = new List<BlockData>();
        [JsonProperty("gates")]         public List<GateData> Gates = new List<GateData>();
        [JsonProperty("obstacles")]     public List<ObstacleData> Obstacles = new List<ObstacleData>();
    }

    public sealed class BoardData
    {
        [JsonProperty("width")]  public int Width;
        [JsonProperty("height")] public int Height;

        /// <summary>Satır dizgileri, üstten alta. 'X' = oynanabilir hücre, '.' = boşluk.</summary>
        [JsonProperty("rows")]   public List<string> Rows = new List<string>();

        [JsonProperty("walls")]  public List<WallData> Walls = new List<WallData>();
    }

    /// <summary>İç duvar: (x,y) hücresinin 'side' kenarından başlar, kenar boyunca 'length' hücre uzar.</summary>
    public sealed class WallData
    {
        [JsonProperty("x")]      public int X;
        [JsonProperty("y")]      public int Y;
        [JsonProperty("side")]   public string Side;
        [JsonProperty("length")] public int Length = 1;
    }

    public sealed class BlockData
    {
        [JsonProperty("x")]      public int X;
        [JsonProperty("y")]      public int Y;
        [JsonProperty("w")]      public int W = 1;
        [JsonProperty("h")]      public int H = 1;

        /// <summary>Renk katmanları; index 0 = dış katman. Tek katman = normal blok.</summary>
        [JsonProperty("layers")] public List<string> Layers = new List<string>();
    }

    public sealed class GateData
    {
        [JsonProperty("x")]      public int X;
        [JsonProperty("y")]      public int Y;
        [JsonProperty("side")]   public string Side;
        [JsonProperty("length")] public int Length = 1;

        /// <summary>Sıralı renk kuyruğu; index 0 aktif renk. M1'de tek renk kullanılır, kuyruk M2'de.</summary>
        [JsonProperty("colors")] public List<string> Colors = new List<string>();
    }

    /// <summary>
    /// Engeller polimorfiktir: "type" alanı türü belirler (ice_block, curtain...).
    /// Tür-özel alanlar (count, gateIndex, contents...) Extra sözlüğünde ham
    /// JSON olarak taşınır; M2'deki ObstacleFactory "type"a bakıp uygun
    /// sınıfa dönüştürür. Böylece yeni engel türü eklemek ŞEMAYI BOZMAZ.
    /// </summary>
    public sealed class ObstacleData
    {
        [JsonProperty("type")] public string Type;

        [JsonExtensionData]    public IDictionary<string, JToken> Extra;
    }
}
