using System.Collections;
using System.Collections.Generic;
using System.Text;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using UnityEngine;

namespace BlockOut.Runtime.DevTools
{
    /// <summary>Tek bir bölümün ARANABİLİR özeti — konsolun bölüm listesi bunu gösterir.</summary>
    public sealed class DevLevelInfo
    {
        public int Index;                 // 0 tabanlı sıra
        public string Id = "";
        public int Number => Index + 1;

        public int Width, Height, TimeSeconds;
        public int Blocks, Gates;
        public int ColorCount;
        public string Colors = "";        // "kırmızı, mavi, sarı"

        public int Ice, GateIce, Layered, Polyomino, AxisLocked, Curtains, Generators, Queued;

        public int Score;
        public LevelDifficulty Difficulty;
        public string Error;

        /// <summary>Arama için küçük harfe indirilmiş tüm metin.</summary>
        public string Haystack = "";

        /// <summary>Listede ikinci satırda görünen mekanik rozetleri.</summary>
        public string Mechanics = "";

        public bool Has(string mechanic)
        {
            switch (mechanic)
            {
                case "ice":       return Ice > 0 || GateIce > 0;
                case "curtain":   return Curtains > 0;
                case "generator": return Generators > 0;
                case "layered":   return Layered > 0;
                case "poly":      return Polyomino > 0;
                case "axis":      return AxisLocked > 0;
                default:          return false;
            }
        }
    }

    /// <summary>
    /// Bütün bölümleri bir kez okuyup ARANABİLİR bir dizine çeviren yardımcı.
    ///
    /// DERS (arama bir dizin ister): "Buzlu bölümleri göster" diyebilmek için
    /// her bölümün içeriğini BİLMEK gerekir; bu da 50 JSON dosyasını
    /// ayrıştırmak demektir. Her tuşa basışta bunu yapmak konsolu kilitlerdi.
    /// Bir kez okuyup özet çıkarmak (dizin), sonra yalnız özet üzerinde
    /// aramak — arama kutusunun arkasındaki standart çözüm budur.
    ///
    /// DERS (uzun işi KAREYE YAY): 50 bölümü tek karede ayrıştırmak cihazda
    /// gözle görülür bir donma yaratır ve "araç bozuk" hissi verir. Dizin bir
    /// eşyordamla (coroutine) kuruluyor: her karede birkaç milisaniye çalışır,
    /// arayüz akıcı kalır ve ilerleme ekranda görünür.
    /// </summary>
    public static class DevLevelIndex
    {
        public static readonly List<DevLevelInfo> All = new List<DevLevelInfo>();

        public static bool Ready { get; private set; }
        public static int Built { get; private set; }
        public static int Total { get; private set; }

        static bool _running;

        /// <summary>
        /// Dizin yoksa kurmaya başlar. Konsol her açıldığında çağrılır.
        ///
        /// Eşyordamı konsolun kendi nesnesi taşıyor: dizin sahneden bağımsız
        /// yaşamalı (bölüm değişince yeniden kurulmasın), konsol nesnesi de
        /// zaten <c>DontDestroyOnLoad</c>.
        /// </summary>
        public static void Ensure()
        {
            if (Ready || _running) return;

            var host = GameKit.DevTools.DevConsole.Ensure();
            if (host == null) return;

            _running = true;
            host.StartCoroutine(Build());
        }

        /// <summary>Bölüm dosyaları değiştiyse (editörde) yeniden taratmak için.</summary>
        public static void Invalidate()
        {
            Ready = false;
            All.Clear();
            Built = 0;
        }

        static IEnumerator Build()
        {
            All.Clear();
            Built = 0;
            Total = LevelCatalog.Count;

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            for (int i = 0; i < Total; i++)
            {
                All.Add(Read(i));
                Built = i + 1;

                // Kare başına ~4 ms bütçe: ayrıştırma sürer ama arayüz akar.
                if (watch.ElapsedMilliseconds >= 4)
                {
                    watch.Reset();
                    watch.Start();
                    yield return null;
                }
            }

            Ready = true;
            _running = false;
        }

        static DevLevelInfo Read(int index)
        {
            var info = new DevLevelInfo { Index = index, Id = LevelCatalog.IdAt(index) };

            var asset = LevelCatalog.AssetAt(index);
            if (asset == null)
            {
                info.Error = "dosya yok";
                info.Haystack = info.Number + " " + info.Id;
                return info;
            }

            try
            {
                var data = Level.LevelLoader.Parse(asset.text);
                var model = LevelModel.Build(data);

                info.TimeSeconds = data.TimeSeconds;
                info.Width = model.Board.Width;
                info.Height = model.Board.Height;
                info.Blocks = model.Blocks.Count;
                info.Gates = model.Gates.Count;

                var colors = new HashSet<BlockColor>();
                foreach (var block in model.Blocks)
                {
                    if (block.Layers != null)
                    {
                        foreach (var layer in block.Layers) colors.Add(layer);
                        if (block.Layers.Count > 1) info.Layered++;
                    }
                    if (block.Cells != null && block.Cells.Count > 1 && !block.IsRectangle)
                        info.Polyomino++;
                    if (block.IceCount > 0) info.Ice++;
                    if (block.Axis != MoveAxis.Free) info.AxisLocked++;
                }

                foreach (var gate in model.Gates)
                {
                    if (gate.IceCount > 0) info.GateIce++;
                    if (gate.ColorQueue != null)
                        foreach (var color in gate.ColorQueue) colors.Add(color);
                }

                foreach (var obstacle in model.Obstacles)
                {
                    if (obstacle is CurtainModel) info.Curtains++;
                    else if (obstacle is GeneratorModel generator)
                    {
                        info.Generators++;
                        info.Queued += generator.Remaining;
                    }
                }

                info.ColorCount = colors.Count;
                info.Colors = ColorList(colors);
                info.Score = LevelDifficultyRule.Score(model);
                info.Difficulty = LevelDifficultyRule.Of(model);
            }
            catch (System.Exception error)
            {
                info.Error = error.Message;
            }

            info.Mechanics = MechanicBadges(info);
            info.Haystack = Haystack(info);
            return info;
        }

        // Arama TÜRKÇE yazılabilsin diye renkler iki dilde de dizine giriyor:
        // "mavi" de "blue" da aynı bölümü bulur.
        static readonly string[] TurkishColors =
        {
            "kırmızı", "mavi", "sarı", "yeşil", "beyaz",
            "siyah", "pembe", "turuncu", "mor", "camgöbeği"
        };

        public static string ColorName(BlockColor color)
        {
            int index = (int)color;
            return index >= 0 && index < TurkishColors.Length
                ? TurkishColors[index]
                : color.ToString();
        }

        static string ColorList(HashSet<BlockColor> colors)
        {
            var text = new StringBuilder();
            foreach (BlockColor color in System.Enum.GetValues(typeof(BlockColor)))
            {
                if (!colors.Contains(color)) continue;
                if (text.Length > 0) text.Append(", ");
                text.Append(ColorName(color));
            }
            return text.ToString();
        }

        static string MechanicBadges(DevLevelInfo info)
        {
            var text = new StringBuilder();
            if (info.Ice > 0) text.Append("BUZ ").Append(info.Ice).Append("  ");
            if (info.GateIce > 0) text.Append("KAPI-BUZ ").Append(info.GateIce).Append("  ");
            if (info.Curtains > 0) text.Append("PERDE ").Append(info.Curtains).Append("  ");
            if (info.Generators > 0) text.Append("MAKİNE ").Append(info.Generators).Append("  ");
            if (info.Layered > 0) text.Append("KATMAN ").Append(info.Layered).Append("  ");
            if (info.Polyomino > 0) text.Append("ŞEKİL ").Append(info.Polyomino).Append("  ");
            if (info.AxisLocked > 0) text.Append("EKSEN ").Append(info.AxisLocked).Append("  ");
            return text.ToString().TrimEnd();
        }

        static string Haystack(DevLevelInfo info)
        {
            var text = new StringBuilder();
            text.Append(info.Number).Append(' ').Append(info.Id).Append(' ')
                .Append(info.Colors).Append(' ');

            // İngilizce karşılıklar: "blue", "ice", "curtain" da yazılabilsin.
            if (info.Ice > 0 || info.GateIce > 0) text.Append("buz ice ");
            if (info.Curtains > 0) text.Append("perde curtain ");
            if (info.Generators > 0) text.Append("makine üretici generator ");
            if (info.Layered > 0) text.Append("katman layer içiçe ");
            if (info.Polyomino > 0) text.Append("şekil polyomino l t ");
            if (info.AxisLocked > 0) text.Append("eksen axis ok ");

            switch (info.Difficulty)
            {
                case LevelDifficulty.Hard: text.Append("zor hard "); break;
                case LevelDifficulty.SuperHard: text.Append("çok zor super hard "); break;
                default: text.Append("normal "); break;
            }

            if (!string.IsNullOrEmpty(info.Error)) text.Append("hata error bozuk ");

            return text.ToString().ToLowerInvariant();
        }
    }
}
