using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BlockOut.Core
{
    /// <summary>
    /// Bölüm geneli dönüşümler (aynalama, döndürme).
    ///
    /// DERS (saf fonksiyon = test edilebilirlik): Bu iş editörde bir menü
    /// komutu olarak görünse de mantığı editöre ait DEĞİL — veri üzerinde saf
    /// dönüşüm. Core'a koyunca hem test edilebilir hem de ileride başka bir
    /// araç (ör. varyant üreteci) aynı kodu kullanabilir.
    ///
    /// Kritik nokta: kenara bağlı nesnelerde (kapı, duvar) hem KONUM hem YÖN
    /// değişir. Batıdaki kapı yatay aynalamada doğuya geçer.
    /// </summary>
    public static class LevelTransform
    {
        public static void MirrorHorizontal(LevelData data)
        {
            int width = data.Board.Width;

            for (int y = 0; y < data.Board.Rows.Count; y++)
            {
                var chars = data.Board.Rows[y].ToCharArray();
                System.Array.Reverse(chars);
                data.Board.Rows[y] = new string(chars);
            }

            foreach (var block in data.Blocks) block.X = width - block.X - block.W;

            foreach (var gate in data.Gates)
            {
                if (!SideUtil.TryParse(gate.Side, out var side)) continue;
                if (side == Side.North || side == Side.South)
                    gate.X = width - gate.X - gate.Length;   // yatay açıklık kayar
                else
                {
                    gate.X = width - 1 - gate.X;             // sütun aynalanır
                    gate.Side = (side == Side.West ? Side.East : Side.West).ToId();
                }
            }

            foreach (var wall in data.Board.Walls)
            {
                if (!SideUtil.TryParse(wall.Side, out var side)) continue;
                int length = wall.Length < 1 ? 1 : wall.Length;
                if (side == Side.North || side == Side.South)
                    wall.X = width - wall.X - length;
                else
                {
                    wall.X = width - 1 - wall.X;
                    wall.Side = (side == Side.West ? Side.East : Side.West).ToId();
                }
            }

            ForEachCurtain(data, (obstacle, contents) =>
            {
                int w = ReadInt(obstacle, "w", 1);
                WriteInt(obstacle, "x", width - ReadInt(obstacle, "x") - w);
                foreach (var block in contents) block.X = width - block.X - block.W;
            });
        }

        public static void MirrorVertical(LevelData data)
        {
            int height = data.Board.Height;
            data.Board.Rows.Reverse();

            foreach (var block in data.Blocks) block.Y = height - block.Y - block.H;

            foreach (var gate in data.Gates)
            {
                if (!SideUtil.TryParse(gate.Side, out var side)) continue;
                if (side == Side.West || side == Side.East)
                    gate.Y = height - gate.Y - gate.Length;
                else
                {
                    gate.Y = height - 1 - gate.Y;
                    gate.Side = (side == Side.North ? Side.South : Side.North).ToId();
                }
            }

            foreach (var wall in data.Board.Walls)
            {
                if (!SideUtil.TryParse(wall.Side, out var side)) continue;
                int length = wall.Length < 1 ? 1 : wall.Length;
                if (side == Side.West || side == Side.East)
                    wall.Y = height - wall.Y - length;
                else
                {
                    wall.Y = height - 1 - wall.Y;
                    wall.Side = (side == Side.North ? Side.South : Side.North).ToId();
                }
            }

            ForEachCurtain(data, (obstacle, contents) =>
            {
                int h = ReadInt(obstacle, "h", 1);
                WriteInt(obstacle, "y", height - ReadInt(obstacle, "y") - h);
                foreach (var block in contents) block.Y = height - block.Y - block.H;
            });
        }

        public static void Rotate180(LevelData data)
        {
            MirrorHorizontal(data);
            MirrorVertical(data);
        }

        static void ForEachCurtain(LevelData data, System.Action<ObstacleData, List<BlockData>> action)
        {
            foreach (var obstacle in data.Obstacles)
            {
                if (obstacle.Type != "curtain") continue;

                var contents = obstacle.Extra != null &&
                               obstacle.Extra.TryGetValue("contents", out var token)
                    ? token.ToObject<List<BlockData>>() ?? new List<BlockData>()
                    : new List<BlockData>();

                action(obstacle, contents);

                if (obstacle.Extra == null)
                    obstacle.Extra = new Dictionary<string, JToken>();
                obstacle.Extra["contents"] = JArray.FromObject(contents);
            }
        }

        static int ReadInt(ObstacleData data, string key, int fallback = 0) =>
            data.Extra != null && data.Extra.TryGetValue(key, out var token)
                ? token.Value<int>() : fallback;

        static void WriteInt(ObstacleData data, string key, int value)
        {
            if (data.Extra == null) data.Extra = new Dictionary<string, JToken>();
            data.Extra[key] = value;
        }
    }
}
