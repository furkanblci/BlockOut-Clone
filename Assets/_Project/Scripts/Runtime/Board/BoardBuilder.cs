using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.View;
using UnityEngine;

// Not: zemin artık tek mesh; floorLight/floorDark materyalleri yalnızca
// eski satranç deseni için vardı ve kaldırıldı.

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
            Material wallMat = MakeMat("Wall",
                cfg != null ? cfg.wallColor : new Color(0.36f, 0.32f, 0.62f));
            Material frameMat = MakeMat("Frame",
                cfg != null ? cfg.frameColor : new Color(0.30f, 0.26f, 0.58f));

            // --- Zemin: TÜM oynanabilir hücreler TEK mesh ---
            // Hücre başına ayrı quad, komşu kenarlarda z-fighting (titreyen
            // çizgiler) üretiyordu ve her hücre ayrı çizim çağrısıydı. Tek
            // mesh + döşenen doku ikisini de çözer; delikli tahtalar da çalışır
            // çünkü yalnızca oynanabilir hücreler mesh'e giriyor.
            var floorGo = new GameObject("Floor");
            floorGo.transform.SetParent(root, false);
            floorGo.AddComponent<MeshFilter>().sharedMesh = BuildFloorMesh(board, space);
            var floorRenderer = floorGo.AddComponent<MeshRenderer>();
            floorRenderer.sharedMaterial = ViewKit.FloorMaterial(cfg);
            floorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            floorRenderer.receiveShadows = false;

            // --- Dış çerçeve: tahtayı çevreleyen kalın bordür ---
            // Referans oyunda tahta, kalın yuvarlatılmış bir çerçeve içinde
            // oturur; bu hem sınırı netleştirir hem de tahtaya kalınlık hissi verir.
            float frameThickness = cfg != null ? cfg.frameThickness : 0.55f;
            float frameHeight = cfg != null ? cfg.frameHeight : 0.34f;
            if (frameThickness > 0.01f)
            {
                var frameGo = new GameObject("Frame");
                frameGo.transform.SetParent(root, false);
                frameGo.AddComponent<MeshFilter>().sharedMesh = BoardFrameMeshBuilder.Build(
                    board.Width, board.Height, frameThickness, frameHeight,
                    cfg != null ? cfg.frameCornerRadius : 0.6f,
                    cfg != null ? cfg.frameBevel : 0.09f);

                var frameRenderer = frameGo.AddComponent<MeshRenderer>();
                frameRenderer.sharedMaterial = frameMat;
                frameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                frameRenderer.receiveShadows = false;
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

        /// <summary>Oynanabilir hücrelerin tamamını tek mesh'e örer; her hücre 0-1 UV alır.</summary>
        static Mesh BuildFloorMesh(BoardModel board, BoardSpace space)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (!board.IsPlayable(x, y)) continue;

                    int start = verts.Count;
                    verts.Add(space.CornerToWorld(x, y));
                    verts.Add(space.CornerToWorld(x + 1, y));
                    verts.Add(space.CornerToWorld(x + 1, y + 1));
                    verts.Add(space.CornerToWorld(x, y + 1));

                    uvs.Add(new Vector2(0f, 1f)); uvs.Add(new Vector2(1f, 1f));
                    uvs.Add(new Vector2(1f, 0f)); uvs.Add(new Vector2(0f, 0f));
                    for (int n = 0; n < 4; n++) normals.Add(Vector3.up);

                    tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
                    tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
                }
            }

            var mesh = new Mesh { name = "Floor" };
            if (verts.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        static Material MakeMat(string name, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", color);
            return mat;
        }
    }
}
