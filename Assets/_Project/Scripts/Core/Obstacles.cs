using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BlockOut.Core
{
    /// <summary>
    /// Tahta engellerinin ortak arayüzü.
    ///
    /// DERS (open/closed ilkesi): Çekirdek sistemler (DragSolver, GateSystem)
    /// yalnızca bu arayüzü tanır. Yarın "asansör" ya da "üreteç" eklemek,
    /// yeni bir sınıf + fabrikaya bir satır demektir — mevcut kod DEĞİŞMEZ.
    /// Buz bilerek engel DEĞİL: videoda buz hep bloğun/kapının kaplaması
    /// olarak davranıyor, o yüzden BlockModel.IceCount / GateModel.IceCount
    /// olarak modellendi.
    /// </summary>
    public interface IObstacle
    {
        /// <summary>Engelin çarpışma kutularını listeye ekler (etkin değilse eklemez).</summary>
        void CollectColliders(List<Aabb> output);

        /// <summary>Bir blok tahtadan çıktığında çağrılır; engel durumu değiştiyse true.</summary>
        bool OnBlockExit();
    }

    /// <summary>
    /// Perde (L20'de görülen sayaçlı kapalı bölge): N blok çıkışına kadar
    /// bölgeyi katı duvar gibi kapatır; sayaç 0'da açılır ve içindeki gizli
    /// bloklar tahtaya doğar.
    /// </summary>
    public sealed class CurtainModel : IObstacle
    {
        public int X, Y, W, H;
        public int Count;

        /// <summary>Perde açılınca tahtaya doğacak gizli bloklar.</summary>
        public readonly List<BlockModel> Contents = new List<BlockModel>();

        public bool IsOpen => Count <= 0;

        public void CollectColliders(List<Aabb> output)
        {
            if (!IsOpen)
                output.Add(Aabb.FromRect(X, Y, W, H));
        }

        public bool OnBlockExit()
        {
            if (IsOpen) return false;
            Count--;
            return true;
        }
    }


    /// <summary>
    /// BLOK ÜRETECİ (bölüm 35'te ilk kez görülür): tahtanın kenarına bağlı,
    /// sıraya dizilmiş blokları tek tek tahtaya iten makine. Üstündeki sayaç
    /// sırada kaç blok kaldığını, penceresi ise SIRADAKİ bloğun şeklini ve
    /// rengini gösterir.
    ///
    /// DERS (üretim tetiği): Üreteç zamanla değil YER AÇILINCA çalışır —
    /// giriş hücreleri boşaldığı anda bir sonraki blok doğar. Bu, oyuncuya
    /// "önce yer aç" hedefi verir ve tahtanın tıkanmasını kuralın kendisi
    /// engeller. Perde gibi çıkış SAYMAZ; sayacı üretim azaltır.
    /// </summary>
    public sealed class GeneratorModel : IObstacle
    {
        /// <summary>Makinenin bağlı olduğu kenar; bloklar bu kenardan girer.</summary>
        public Side Side;

        /// <summary>Girişin kenar boyunca konumu (hücre).</summary>
        public int X, Y;

        /// <summary>Sırada bekleyen bloklar; ilk eleman "sıradaki"dir.</summary>
        public readonly List<BlockModel> Queue = new List<BlockModel>();

        public int Remaining => Queue.Count;
        public bool IsEmpty => Queue.Count == 0;

        /// <summary>Makine tahtanın DIŞINDA durur; hiçbir hücreyi kapatmaz.</summary>
        public void CollectColliders(List<Aabb> output) { }

        /// <summary>Çıkışlar üreteci etkilemez — sayacı üretim azaltır.</summary>
        public bool OnBlockExit() => false;
    }

    /// <summary>
    /// JSON "type" alanı → engel modeli. Tür-özel alanlar ObstacleData.Extra
    /// içinde ham JToken olarak gelir; her tür kendi alanlarını buradan okur.
    /// </summary>
    public static class ObstacleFactory
    {
        public static IObstacle Create(ObstacleData data, Func<BlockData, BlockModel> buildBlock)
        {
            switch (data.Type)
            {
                case "curtain":
                {
                    var curtain = new CurtainModel
                    {
                        X = ReadInt(data, "x"),
                        Y = ReadInt(data, "y"),
                        W = ReadInt(data, "w", 1),
                        H = ReadInt(data, "h", 1),
                        Count = ReadInt(data, "count", 1)
                    };
                    if (data.Extra != null && data.Extra.TryGetValue("contents", out var contents))
                        foreach (var blockData in contents.ToObject<List<BlockData>>())
                            curtain.Contents.Add(buildBlock(blockData));
                    return curtain;
                }
                case "generator":
                {
                    if (!SideUtil.TryParse(ReadString(data, "side"), out var side))
                        throw new FormatException($"Üreteç kenarı çözümlenemedi: '{ReadString(data, "side")}'");

                    var generator = new GeneratorModel
                    {
                        Side = side,
                        X = ReadInt(data, "x"),
                        Y = ReadInt(data, "y")
                    };
                    if (data.Extra != null && data.Extra.TryGetValue("queue", out var queue))
                        foreach (var blockData in queue.ToObject<List<BlockData>>())
                            generator.Queue.Add(buildBlock(blockData));
                    return generator;
                }
                default:
                    throw new FormatException($"Bilinmeyen engel türü: '{data.Type}'");
            }
        }

        static string ReadString(ObstacleData data, string key, string fallback = "") =>
            data.Extra != null && data.Extra.TryGetValue(key, out var token)
                ? token.Value<string>()
                : fallback;

        static int ReadInt(ObstacleData data, string key, int fallback = 0) =>
            data.Extra != null && data.Extra.TryGetValue(key, out var token)
                ? token.Value<int>()
                : fallback;
    }
}
