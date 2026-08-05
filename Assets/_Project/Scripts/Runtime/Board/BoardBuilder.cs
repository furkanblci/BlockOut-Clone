using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.View;
using UnityEngine;

namespace BlockOut.Runtime.Board
{
    /// <summary>
    /// LevelModel'i sahnedeki görsellere çevirir: zemin karoları, duvarlar,
    /// bloklar ve kapı barları. Tersi YOKTUR — görseller asla modele yazmaz.
    ///
    /// Blok/kapı materyalleri palet asset'inden gelir (8 paylaşımlı materyal);
    /// zemin/duvar gibi kozmetik materyaller M1'de çalışma anında üretilir,
    /// M4'te asset'e taşınacak.
    /// </summary>
    public static class BoardBuilder
    {
        static float WallHeight => VisualSettings.Current != null
            ? VisualSettings.Current.wallHeight : 0.42f;
        static float WallThickness => VisualSettings.Current != null
            ? VisualSettings.Current.wallThickness : 0.18f;

        public static BoardViews Build(
            Transform root, LevelModel level, BoardSpace space, ColorPaletteSO palette)
        {
            // Yeniden başlatmada eski tahtayı temizle. DestroyImmediate bilinçli:
            // Destroy kare SONUNA ertelenir; yık-yeniden-kur geçişinde eski ve
            // yeni tahta bir karelik üst üste görünürdü. Oyun içi tekil yok
            // etmeler (emilme animasyonu) ertelenmiş Destroy kullanmaya devam eder.
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            var views = new BoardViews();
            var board = level.Board;

            // Renkler görsel ayar asset'inden gelir; Görünüm Ayarları penceresinden
            // canlı değiştirilebilir.
            var cfg = VisualSettings.Current;
            Material floorLight = MakeMat("Floor_Light",
                cfg != null ? cfg.floorColorA : new Color(0.17f, 0.15f, 0.31f));
            Material floorDark = MakeMat("Floor_Dark",
                cfg != null ? cfg.floorColorB : new Color(0.14f, 0.12f, 0.27f));
            Material wallMat = MakeMat("Wall",
                cfg != null ? cfg.wallColor : new Color(0.36f, 0.32f, 0.62f));
            Material frameMat = MakeMat("Frame",
                cfg != null ? cfg.frameColor : new Color(0.30f, 0.26f, 0.58f));

            // --- Zemin: her oynanabilir hücreye bir karo (satranç deseni) ---
            var floorRoot = new GameObject("Floor").transform;
            floorRoot.SetParent(root, false);
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (!board.IsPlayable(x, y)) continue;
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    quad.name = $"Floor_{x}_{y}";
                    quad.transform.SetParent(floorRoot, false);
                    Object.Destroy(quad.GetComponent<Collider>());
                    quad.transform.SetPositionAndRotation(
                        space.CornerToWorld(x + 0.5f, y + 0.5f, 0f),
                        Quaternion.Euler(90f, 0f, 0f));
                    quad.GetComponent<MeshRenderer>().sharedMaterial =
                        (x + y) % 2 == 0 ? floorLight : floorDark;
                }
            }

            // --- Dış çerçeve: tahtayı çevreleyen kalın bordür ---
            // Referans oyunda tahta, kalın yuvarlatılmış bir çerçeve içinde
            // oturur; bu hem sınırı netleştirir hem de tahtaya kalınlık hissi verir.
            float frameThickness = cfg != null ? cfg.frameThickness : 0.55f;
            float frameHeight = cfg != null ? cfg.frameHeight : 0.34f;
            if (frameThickness > 0.01f)
            {
                var frameRoot = new GameObject("Frame").transform;
                frameRoot.SetParent(root, false);

                float halfW = board.Width * 0.5f, halfH = board.Height * 0.5f;
                float outerW = board.Width + frameThickness * 2f;
                float outerH = board.Height + frameThickness * 2f;

                AddFrameBar(frameRoot, frameMat,
                    new Vector3(0f, frameHeight * 0.5f, halfH + frameThickness * 0.5f),
                    new Vector3(outerW, frameHeight, frameThickness));
                AddFrameBar(frameRoot, frameMat,
                    new Vector3(0f, frameHeight * 0.5f, -halfH - frameThickness * 0.5f),
                    new Vector3(outerW, frameHeight, frameThickness));
                AddFrameBar(frameRoot, frameMat,
                    new Vector3(-halfW - frameThickness * 0.5f, frameHeight * 0.5f, 0f),
                    new Vector3(frameThickness, frameHeight, board.Height));
                AddFrameBar(frameRoot, frameMat,
                    new Vector3(halfW + frameThickness * 0.5f, frameHeight * 0.5f, 0f),
                    new Vector3(frameThickness, frameHeight, board.Height));
            }

            // --- Kapı kenarlarını topla: o kenarlara duvar örülmeyecek ---
            var gateEdges = new HashSet<EdgeId>();
            foreach (var gate in level.Gates)
            {
                for (int j = 0; j < gate.Length; j++)
                {
                    int cx = gate.EdgeHorizontal ? gate.X + j : gate.X;
                    int cy = gate.EdgeHorizontal ? gate.Y : gate.Y + j;
                    gateEdges.Add(EdgeId.OfCellSide(cx, cy, gate.Side));
                }
            }

            // --- Duvarlar: tahta sınırı (kapısız kenarlar) + iç duvarlar ---
            var wallRoot = new GameObject("Walls").transform;
            wallRoot.SetParent(root, false);
            var builtEdges = new HashSet<EdgeId>();

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (!board.IsPlayable(x, y)) continue;
                    TryBuildBoundaryWall(x, y, Side.North, x, y - 1);
                    TryBuildBoundaryWall(x, y, Side.South, x, y + 1);
                    TryBuildBoundaryWall(x, y, Side.West, x - 1, y);
                    TryBuildBoundaryWall(x, y, Side.East, x + 1, y);
                }
            }

            void TryBuildBoundaryWall(int cx, int cy, Side side, int nx, int ny)
            {
                if (board.IsPlayable(nx, ny)) return; // sınır değil
                var edge = EdgeId.OfCellSide(cx, cy, side);
                if (gateEdges.Contains(edge) || !builtEdges.Add(edge)) return;
                BuildWallSegment(edge);
            }

            foreach (var edge in board.Walls)
                if (!gateEdges.Contains(edge) && builtEdges.Add(edge))
                    BuildWallSegment(edge);

            void BuildWallSegment(EdgeId edge)
            {
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"Wall_{edge.X}_{edge.Y}_{(edge.Horizontal ? "H" : "V")}";
                seg.transform.SetParent(wallRoot, false);
                Object.Destroy(seg.GetComponent<Collider>());
                seg.GetComponent<MeshRenderer>().sharedMaterial = wallMat;

                seg.transform.position = edge.Horizontal
                    ? space.CornerToWorld(edge.X + 0.5f, edge.Y, WallHeight * 0.5f)
                    : space.CornerToWorld(edge.X, edge.Y + 0.5f, WallHeight * 0.5f);
                seg.transform.localScale = edge.Horizontal
                    ? new Vector3(1f + WallThickness, WallHeight, WallThickness)
                    : new Vector3(WallThickness, WallHeight, 1f + WallThickness);
            }

            // --- Bloklar, kapılar ve engeller ---
            var blockRoot = new GameObject("Blocks").transform;
            blockRoot.SetParent(root, false);
            views.BlockRoot = blockRoot; // perde açılınca doğan bloklar da buraya
            foreach (var block in level.Blocks)
                views.Blocks[block] = BlockView.Create(
                    blockRoot, block, space, GetBlockMaterial(palette, block.CurrentColor));

            var gateRoot = new GameObject("Gates").transform;
            gateRoot.SetParent(root, false);
            foreach (var gate in level.Gates)
                views.Gates[gate] = GateView.Create(
                    gateRoot, gate, space, GetBlockMaterial(palette, gate.ActiveColor));

            var obstacleRoot = new GameObject("Obstacles").transform;
            obstacleRoot.SetParent(root, false);
            foreach (var obstacle in level.Obstacles)
                if (obstacle is CurtainModel curtain)
                    views.Curtains[curtain] = CurtainView.Create(obstacleRoot, curtain, space);

            return views;
        }

        public static Material GetBlockMaterial(ColorPaletteSO palette, BlockColor color)
        {
            var entry = palette.Get(color);
            if (entry != null && entry.blockMaterial != null)
                return entry.blockMaterial;

            // Kurulum aracı materyalleri henüz üretmediyse görünür kal: geçici materyal.
            Debug.LogWarning($"[BoardBuilder] '{color}' için palet materyali yok — geçici materyal üretildi.");
            return MakeMat($"Fallback_{color}", entry?.uiColor ?? Color.magenta);
        }

        static void AddFrameBar(Transform parent, Material material, Vector3 center, Vector3 size)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "FrameBar";
            bar.transform.SetParent(parent, false);
            Object.Destroy(bar.GetComponent<Collider>());
            var renderer = bar.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bar.transform.localPosition = center;
            bar.transform.localScale = size;
        }

        static Material MakeMat(string name, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", color);
            return mat;
        }
    }
}
