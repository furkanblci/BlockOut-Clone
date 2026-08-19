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

            BuildGrid(root, board, space);

            // --- Dış çerçeve: tahtayı çevreleyen kalın bordür ---
            // Referans oyunda tahta, kalın yuvarlatılmış bir çerçeve içinde
            // oturur; bu hem sınırı netleştirir hem de tahtaya kalınlık hissi verir.
            float frameThickness = cfg != null ? cfg.frameThickness : 0.55f;
            float frameHeight = cfg != null ? cfg.frameHeight : 0.34f;
            if (frameThickness > 0.01f)
            {
                var frameGo = new GameObject("Frame");
                frameGo.transform.SetParent(root, false);
                // Çerçeve OYNANABİLİR HÜCRELERİN silüetini izler, sınır
                // kutusunu değil: kesilmiş bölgelerde duvar o çıkıntının
                // çevresini dolanır (bkz. BoardFrameMeshBuilder).
                var playable = new List<Vector2Int>(board.Width * board.Height);
                for (int y = 0; y < board.Height; y++)
                    for (int x = 0; x < board.Width; x++)
                        if (board.IsPlayable(x, y)) playable.Add(new Vector2Int(x, y));

                frameGo.AddComponent<MeshFilter>().sharedMesh = BoardFrameMeshBuilder.Build(
                    playable, board.Width, board.Height, frameThickness, frameHeight,
                    cfg != null ? cfg.frameCornerRadius : 0.6f,
                    cfg != null ? cfg.frameBevel : 0.09f);

                var frameRenderer = frameGo.AddComponent<MeshRenderer>();
                frameRenderer.sharedMaterial = frameMat;
                frameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                frameRenderer.receiveShadows = false;
            }

            // --- Oynanamaz bölgeler: çerçevenin bir parçası gibi DOLDURULUR ---
            BuildDeadZones(root, board, space, frameMat, cfg);

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

            // İç duvarlar çerçeveyle aynı stilde: ince gri çizgi yerine tahtanın
            // yapısal bir parçası gibi görünsün (referansta böyle).
            // NOT: yerel fonksiyonlar bu değişkenleri yakaladığı için ÇAĞRIDAN
            // ÖNCE atanmaları gerekir — aksi halde "unassigned local" hatası.
            bool wallLikeFrame = cfg == null || cfg.wallMatchesFrame;
            Material wallMaterial = wallLikeFrame ? frameMat : wallMat;
            float wallH = wallLikeFrame ? frameHeight : WallHeight;
            float wallT = wallLikeFrame
                ? Mathf.Max(WallThickness, frameThickness * 0.6f) : WallThickness;

            // OYNANAMAZ BÖLGELERİN ÇEVRESİNE ARTIK DUVAR ÇUBUĞU ÖRÜLMÜYOR.
            //
            // DERS (aynı sınırı iki kez anlatma): Buradaki döngü, oynanabilir
            // bir hücrenin oynanamaz komşusuna baktığı her yere ince bir çubuk
            // koyuyordu. `BuildDeadZones` o bölgeleri artık kabartma bir kütle
            // olarak DOLDURDUĞU için sınır zaten çiziliyor; çubuk üstüne
            // binince oynanabilir alana 0,15 hücrelik ikinci bir raf taşıyor
            // ve yuvarlak köşelerin ucunu kesiyordu. Sınırı bir kere çizen
            // taraf, o sınırın SAHİBİ olan taraf olmalı.
            //
            // Şemadaki AÇIK iç duvarlar (board.Walls) hâlâ çubuk: onlar bir
            // bölge sınırı değil, iki oynanabilir hücre arasındaki engel.
            foreach (var edge in board.Walls)
                if (!gateEdges.Contains(edge) && builtEdges.Add(edge))
                    BuildWallSegment(edge);

            void BuildWallSegment(EdgeId edge)
            {
                var seg = ViewKit.CreateShape(PrimitiveType.Cube, "Shape");
                seg.name = $"Wall_{edge.X}_{edge.Y}_{(edge.Horizontal ? "H" : "V")}";
                seg.transform.SetParent(wallRoot, false);
                var wallRenderer = seg.GetComponent<MeshRenderer>();
                wallRenderer.sharedMaterial = wallMaterial;
                wallRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                seg.transform.position = edge.Horizontal
                    ? space.CornerToWorld(edge.X + 0.5f, edge.Y, wallH * 0.5f)
                    : space.CornerToWorld(edge.X, edge.Y + 0.5f, wallH * 0.5f);
                seg.transform.localScale = edge.Horizontal
                    ? new Vector3(1f + wallT, wallH, wallT)
                    : new Vector3(wallT, wallH, 1f + wallT);
            }

            // --- Bloklar, kapılar ve engeller ---
            var blockRoot = new GameObject("Blocks").transform;
            blockRoot.SetParent(root, false);
            views.BlockRoot = blockRoot; // perde açılınca doğan bloklar da buraya
            foreach (var block in level.Blocks)
                views.Blocks[block] = BlockView.Create(
                    blockRoot, block, space, GetBlockMaterial(palette, block.CurrentColor),
                    palette);

            var gateRoot = new GameObject("Gates").transform;
            gateRoot.SetParent(root, false);
            foreach (var gate in level.Gates)
                views.Gates[gate] = GateView.Create(
                    gateRoot, gate, space, GetBlockMaterial(palette, gate.ActiveColor));

            var obstacleRoot = new GameObject("Obstacles").transform;
            obstacleRoot.SetParent(root, false);
            foreach (var obstacle in level.Obstacles)
            {
                if (obstacle is CurtainModel curtain)
                    views.Curtains[curtain] = CurtainView.Create(obstacleRoot, curtain, space);
                else if (obstacle is GeneratorModel generator && !generator.IsEmpty)
                    views.Generators[generator] = GeneratorView.Create(
                        obstacleRoot, generator, space,
                        GetBlockMaterial(palette, generator.Queue[0].CurrentColor));
            }

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

        /// <summary>
        /// ŞEKİLLİ TAHTALARDA oynanamaz hücreleri, çerçeveyle aynı malzemeden
        /// KABARTMA bir kütle olarak doldurur (4. tur I32/I33/I34).
        ///
        /// Kullanıcı: "Level 8 — '1' sayısına benzer tasarım; sol alt kısım
        /// orijinalde boş ama bizde kapatılmış... boşluklu olan tüm levellerde
        /// boş alanlar bizde görünüyor."
        ///
        /// DERS (bir boşluğu göstermemek, orayı boş bırakmak DEĞİLDİR): Kod
        /// zaten doğru şeyi yapıyordu — zemin yalnız oynanabilir hücrelere
        /// örülüyor, oynanamaz hücrelere zemin çizilmiyordu. Ama ekranda kalan
        /// şey ARKA PLAN oluyordu: ızgara çizgileri o bölgenin üstünden geçmeye
        /// devam ediyor, sınırına ince bir duvar çubuğu düşüyor ve bölge
        /// "tahtada açılmış bir delik" gibi görünüyordu. Referansta (41-50
        /// yürüyüşü, 48-49. bölümler) o alanlar DOLU: çerçeveyle aynı açık
        /// mor, aynı yükseklikte, yuvarlak köşeli bir kütle — yani tahta
        /// oradan hiç başlamamış gibi. Şekli okutan şey deliğin kendisi değil,
        /// çevresindeki DOLULUKTUR.
        ///
        /// DERS (kenardaki yuvarlaklık çerçevenin ALTINDA kalmalı): Bölge
        /// tahtanın kenarına dayanıyorsa köşe yayı orada çentik açar ve
        /// çerçeveyle arasında arka plan sızar. Bu yüzden bölge önce tahtanın
        /// DIŞINA bir hücre taşırılıyor, sonra çerçevenin dış yüzüne
        /// KIRPILIYOR: yay çerçevenin altında kalıyor, görünen kenar düz.
        /// </summary>
        static void BuildDeadZones(Transform root, BoardModel board, BoardSpace space,
            Material frameMat, BlockVisualConfigSO cfg)
        {
            var dead = new HashSet<Vector2Int>();
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                    if (!board.IsPlayable(x, y)) dead.Add(new Vector2Int(x, y));

            if (dead.Count == 0) return;

            float height = cfg != null ? cfg.frameHeight : 0.34f;
            float bevel = cfg != null ? cfg.frameBevel : 0.09f;
            float thickness = cfg != null ? cfg.frameThickness : 0.55f;
            float radius = cfg != null ? cfg.frameCornerRadius : 0.6f;

            // Çerçeveyle üst yüzde z-fighting olmasın: bölge saç teli kadar alçak.
            height = Mathf.Max(0.02f, height - 0.006f);
            float limitX = board.Width * 0.5f + thickness * 0.85f;
            float limitZ = board.Height * 0.5f + thickness * 0.85f;

            var holder = new GameObject("DeadZones").transform;
            holder.SetParent(root, worldPositionStays: false);

            var remaining = new HashSet<Vector2Int>(dead);
            var queue = new Queue<Vector2Int>();
            int index = 0;

            while (remaining.Count > 0)
            {
                // Bağlı bileşen: ayrı boşluklar ayrı kütlelerdir, tek mesh'e
                // basmak aralarında hayalet bağlantı üretirdi.
                var component = new List<Vector2Int>();
                var seed = default(Vector2Int);
                foreach (var cell in remaining) { seed = cell; break; }

                queue.Clear();
                queue.Enqueue(seed);
                remaining.Remove(seed);
                while (queue.Count > 0)
                {
                    var cell = queue.Dequeue();
                    component.Add(cell);
                    TryTake(cell + Vector2Int.left);
                    TryTake(cell + Vector2Int.right);
                    TryTake(cell + Vector2Int.up);
                    TryTake(cell + Vector2Int.down);
                }

                void TryTake(Vector2Int cell)
                {
                    if (!remaining.Remove(cell)) return;
                    queue.Enqueue(cell);
                }

                // KENARA DAYANAN BOŞLUK ARTIK ÇİZİLMİYOR (5. tur).
                //
                // Kullanıcı: "bazı levellerde boşluk olmalı, kullanılmayan
                // kısımlar kesilmeli... hatta ekstra doldurmuşsun oraları."
                //
                // Referansta (8., 9., 11., 12., 13. bölümler) kullanılmayan
                // bölge tahtanın DIŞINDA: duvar o çıkıntının çevresini
                // dolanıyor ve ötesinde arka plan var. Bizde her oynanamaz
                // hücre çerçeve renginde kabarık bir kütle olarak
                // çiziliyordu — yani "kesilmiş" değil "doldurulmuş"
                // görünüyordu; kullanıcının gördüğü buydu.
                //
                // Artık çerçeve maskeyi izlediği (bkz. BoardFrameMeshBuilder)
                // için kenara dayanan boşluğun zaten dışarısı olduğu
                // biliniyor. Yalnız İÇ DELİKLER — çepeçevre oynanabilir
                // hücreyle sarılı adacıklar — kütle olarak kalıyor; onları
                // silüet halkası kapsamıyor.
                //
                // DERS (aynı şeyi iki yerde anlatmak, ikisini de bozar):
                // Boşluk hem çerçevenin silüetinde hem de ayrı bir kütlede
                // temsil edilirse, ikisi ilk fırsatta çelişir. Sınır bir kez
                // çizilir.
                bool touchesBorder = false;
                foreach (var cell in component)
                    if (cell.x == 0 || cell.y == 0 ||
                        cell.x == board.Width - 1 || cell.y == board.Height - 1)
                    { touchesBorder = true; break; }

                if (!touchesBorder) BuildDeadZone(component, index++);
            }

            void BuildDeadZone(List<Vector2Int> component, int id)
            {
                // Kenara dayanan hücreleri tahtanın DIŞINA taşır — köşegenler
                // dahil, yoksa köşede üçgen bir boşluk kalır.
                var cells = new HashSet<Vector2Int>(component);
                foreach (var cell in component)
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            var neighbour = new Vector2Int(cell.x + dx, cell.y + dy);
                            bool outside = neighbour.x < 0 || neighbour.y < 0 ||
                                           neighbour.x >= board.Width || neighbour.y >= board.Height;
                            if (outside) cells.Add(neighbour);
                        }

                var list = new List<Vector2Int>(cells);
                var loop = View.BrickSilhouette.Build(
                    list, board.Width, board.Height, -0.01f,
                    Mathf.Min(radius, 0.5f));
                if (loop == null || loop.Count < 3) return;

                View.BrickSilhouette.MakeCounterClockwise(loop);
                for (int i = 0; i < loop.Count; i++)
                    loop[i] = new Vector2(
                        Mathf.Clamp(loop[i].x, -limitX, limitX),
                        Mathf.Clamp(loop[i].y, -limitZ, limitZ));

                var go = new GameObject($"DeadZone_{id}");
                go.transform.SetParent(holder, worldPositionStays: false);
                go.AddComponent<MeshFilter>().sharedMesh = ExtrudeCapped(loop, height, bevel);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = frameMat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        /// <summary>Kapalı bir halkayı yan duvar + pah + üst kapak olarak kabartır.</summary>
        static Mesh ExtrudeCapped(List<Vector2> loop, float height, float bevel)
        {
            int count = loop.Count;
            bevel = Mathf.Clamp(bevel, 0f, height * 0.5f);
            float shoulder = height - bevel;

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var tris = new List<int>();

            var outward = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var prev = loop[(i - 1 + count) % count];
                var next = loop[(i + 1) % count];
                var tangent = (next - prev).normalized;
                outward[i] = new Vector3(tangent.y, 0f, -tangent.x);
            }

            void Ring(System.Func<int, Vector3> lower, System.Func<int, Vector3> upper,
                      System.Func<int, Vector3> normal)
            {
                int start = verts.Count;
                for (int i = 0; i < count; i++)
                {
                    verts.Add(lower(i)); verts.Add(upper(i));
                    var n = normal(i);
                    normals.Add(n); normals.Add(n);
                }
                for (int i = 0; i < count; i++)
                {
                    int a = start + i * 2;
                    int b = start + ((i + 1) % count) * 2;
                    tris.Add(a); tris.Add(a + 1); tris.Add(b);
                    tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
                }
            }

            Ring(i => new Vector3(loop[i].x, 0f, loop[i].y),
                 i => new Vector3(loop[i].x, shoulder, loop[i].y),
                 i => outward[i]);

            Ring(i => new Vector3(loop[i].x, shoulder, loop[i].y),
                 i => new Vector3(loop[i].x, height, loop[i].y) - outward[i] * bevel,
                 i => (outward[i] + Vector3.up).normalized);

            var top = new List<Vector2>(count);
            for (int i = 0; i < count; i++)
                top.Add(new Vector2(loop[i].x - outward[i].x * bevel,
                                    loop[i].y - outward[i].z * bevel));

            int capStart = verts.Count;
            foreach (var p in top)
            {
                verts.Add(new Vector3(p.x, height, p.y));
                normals.Add(Vector3.up);
            }
            View.BrickSilhouette.Triangulate(top, tris, capStart, faceUp: true);

            var mesh = new Mesh { name = "DeadZone" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        /// <summary>Oynanabilir hücrelerin tamamını tek mesh'e örer; her hücre 0-1 UV alır.</summary>
        /// <summary>
        /// Zemine ince hücre ızgarası çizer.
        ///
        /// DERS (ızgara OKUNABİLİRLİK, süs değil): Zemin düz koyu bir yüzeydi.
        /// Blok sürüklenirken oyuncu "bir hücre mi iki hücre mi kaydım" sorusunu
        /// ancak bloğun kendisine bakarak cevaplayabiliyordu. Izgara o soruyu
        /// zeminden cevaplıyor ve kaydırmanın hücreye oturduğunu gösteriyor.
        ///
        /// Çizgiler ÇOK soluk (%7): amaç sınırı sezdirmek, dikkat çekmek değil.
        /// Belirgin bir ızgara bloklarla yarışır ve tahtayı gürültülü yapar.
        /// </summary>
        static void BuildGrid(Transform root, BoardModel board, BoardSpace space)
        {
            var holder = new GameObject("Grid");
            holder.transform.SetParent(root, worldPositionStays: false);

            const float thickness = 0.035f;
            var material = ViewKit.GridLine;

            void Line(Vector3 center, Vector3 scale)
            {
                var line = ViewKit.CreateShape(PrimitiveType.Cube, "GridLine");
                line.transform.SetParent(holder.transform, worldPositionStays: false);
                var renderer = line.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                line.transform.position = center;
                line.transform.localScale = scale;
            }

            // IZGARA MASKEYİ İZLER (5. tur).
            //
            // Çizgiler tahtanın SINIR KUTUSU boyunca uçtan uca çiziliyordu.
            // Tahta dikdörtgenken sorun yoktu; kesilmiş tahtalarda ise
            // çizgiler oyun alanının dışına, boş bölgeye taşıyor ve orada
            // hayalet bir ızgara bırakıyordu.
            //
            // Bir iç çizgi parçası yalnız İKİ YANI DA oynanabilir olan hücre
            // sınırlarında çiziliyor: tek yanı oynanabilirse orası tahtanın
            // kenarıdır ve zaten çerçeve tarafından belirtiliyor.
            //
            // Bitişik parçalar tek bir çubuğa birleştiriliyor: hücre başına
            // ayrı nesne, 6x8 bir tahtada bile gereksiz yüzlerce çizim çağrısı
            // demek.
            for (int x = 1; x < board.Width; x++)
            {
                int runStart = -1;
                for (int y = 0; y <= board.Height; y++)
                {
                    bool seam = y < board.Height &&
                                board.IsPlayable(x - 1, y) && board.IsPlayable(x, y);
                    if (seam && runStart < 0) runStart = y;
                    else if (!seam && runStart >= 0)
                    {
                        float length = y - runStart;
                        Line(space.CornerToWorld(x, runStart + length * 0.5f, 0.012f),
                             new Vector3(thickness, 0.02f, length));
                        runStart = -1;
                    }
                }
            }

            for (int y = 1; y < board.Height; y++)
            {
                int runStart = -1;
                for (int x = 0; x <= board.Width; x++)
                {
                    bool seam = x < board.Width &&
                                board.IsPlayable(x, y - 1) && board.IsPlayable(x, y);
                    if (seam && runStart < 0) runStart = x;
                    else if (!seam && runStart >= 0)
                    {
                        float length = x - runStart;
                        Line(space.CornerToWorld(runStart + length * 0.5f, y, 0.012f),
                             new Vector3(length, 0.02f, thickness));
                        runStart = -1;
                    }
                }
            }

            // KESİŞİM NOKTALARI — referansta her iç kesişimde küçük koyu bir
            // nokta var (oynanış videosu, 00:28 karesi). Tek başına küçük bir
            // ayrıntı ama tahtayı "çizilmiş ızgara" olmaktan çıkarıp
            // "dökülmüş bir kalıp" gibi gösteren şey o: göz noktaları hücre
            // köşelerinin perçini olarak okuyor.
            const float dot = thickness * 2.6f;
            for (int x = 1; x < board.Width; x++)
                for (int y = 1; y < board.Height; y++)
                {
                    // Perçin yalnız DÖRT hücrenin de oynanabilir olduğu
                    // köşede: tahtanın kenarındaki bir köşede nokta,
                    // çerçevenin üstünde asılı kalırdı.
                    if (!board.IsPlayable(x - 1, y - 1) || !board.IsPlayable(x, y - 1) ||
                        !board.IsPlayable(x - 1, y) || !board.IsPlayable(x, y)) continue;
                    Line(space.CornerToWorld(x, y, 0.013f),
                         new Vector3(dot, 0.02f, dot));
                }
        }

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

                    // HATA (level editörünün 3D önizlemesi ortaya çıkardı):
                    // Bu iki üçgen TERS sarılmıştı ve zemin ön yüzü AŞAĞI
                    // bakıyordu — yani tahtanın zemini kamera tepeden baktığı
                    // için arka yüz eleme (backface culling) ile HİÇ
                    // ÇİZİLMİYORDU. Oyunda "zemin" sanılan şey arka plan +
                    // ızgara çizgileri + çerçeveydi; floorColorA/B ayarlarının
                    // hiçbir etkisi olmuyordu.
                    //
                    // DERS (sarım yönü sessizce kaybeder): Ters sarılmış bir
                    // yüzey hata vermez, sadece görünmez olur. Kıyas noktası
                    // aynı dosyadaki gölge mesh'i: köşeleri ALTTAN üste
                    // sıralıyor ve doğru çalışıyor. Buradaki köşeler ÜSTTEN
                    // alta sıralanıyor (CornerToWorld'de y arttıkça z AZALIR),
                    // dolayısıyla üçgen sırası da tersine dönmek zorundaydı.
                    tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                    tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
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
