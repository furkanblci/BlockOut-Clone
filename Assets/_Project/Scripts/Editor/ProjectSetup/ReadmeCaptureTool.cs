using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.Input;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// DEPO VİTRİNİ İÇİN YAKALAMA — README'deki durağan kareler ve GIF'ler.
    ///
    /// NEDEN AYRI BİR ARAÇ: <see cref="UiCaptureTool"/> yalnız kanvası,
    /// <see cref="BoardCaptureTool"/> yalnız tahtayı alıyor. Vitrin karesi
    /// ikisini BİRDEN ister: 3B tahta + üstündeki HUD.
    ///
    /// DERS (URP'de elle kompozit YAPMA): İlk hâli iki kamerayı aynı
    /// RenderTexture'a çiziyordu — <c>Camera.Render()</c> + "yalnız derinliği
    /// temizle". URP'de bu çalışmaz: her kamera hedefini kendi temizler ve
    /// oynanış karesi bembeyaz çıktı. Menüde sorun görünmedi çünkü orada
    /// çizilen tek şey kanvastı. Doğrusu ekranın BİTMİŞ karesini almak:
    /// <c>ScreenCapture.CaptureScreenshotIntoRenderTexture</c> boru hattının
    /// çıktısını olduğu gibi verir — kanvas kipiyle, kamera yığınıyla,
    /// katman maskesiyle uğraşmak gerekmez.
    ///
    /// DERS (kare sonunu beklemek zorunludur): Ekran arabelleği ancak çizim
    /// bittikten sonra doludur. Yakalama bir eşyordamda
    /// <c>WaitForEndOfFrame</c> ardından yapılır; yani <see cref="Shot"/>
    /// çağrıldığı karede değil, BİR SONRAKİ karede dosya üretir.
    /// </summary>
    public static class ReadmeCapture
    {
        /// <summary>PNG'lerin yazıldığı klasör (depo dışında, geçici).</summary>
        public static string Dir = Path.Combine(Path.GetTempPath(), "blockout-readme");

        /// <summary>
        /// Tek kare ister. <paramref name="w"/> verilmezse ekran çözünürlüğü.
        /// Dosya bir sonraki karede oluşur.
        /// </summary>
        public static void Shot(string name, int w = 0, int h = 0) =>
            ReadmeRunner.Ensure().Enqueue(name, w, h);

        /// <summary>
        /// Kare dizisi kaydeder. <paramref name="autoplay"/> açıksa oyunu aynı
        /// zamanda KENDİ OYNAR: her adımda bir bloğa çıkış yolu arayıp parmakla
        /// sürüklüyormuş gibi taşır.
        /// </summary>
        public static void Record(string prefix, int frames, int fps = 20,
            int w = 540, int h = 960, bool autoplay = true) =>
            ReadmeRunner.Ensure().BeginRecord(prefix, frames, fps, w, h, autoplay);

        public static bool Recording => ReadmeRunner.Instance != null && ReadmeRunner.Instance.IsRecording;
        public static int RecordedFrames => ReadmeRunner.Instance != null ? ReadmeRunner.Instance.Written : 0;
        public static string LastNote => ReadmeRunner.Instance != null ? ReadmeRunner.Instance.Note : "yok";
        public static void StopRecording() { if (ReadmeRunner.Instance != null) ReadmeRunner.Instance.EndRecord(); }
    }

    /// <summary>
    /// Kareleri diske yazan ve isterse oyunu kendi oynayan geçici bileşen.
    ///
    /// DERS (sahte parmak, gerçek kod yolu): Blokları doğrudan yerine koymak
    /// çok daha kolaydı ama GIF'te oyun kodunun yaptığı hiçbir şey (çarpışma
    /// kaydırması, kapı teması, emilme efekti, kombo) görünmezdi. Bunun yerine
    /// <see cref="PointerInputService"/>'in olayları yansımayla tetikleniyor:
    /// oyun bunu gerçek bir dokunuştan ayırt edemez.
    /// </summary>
    public sealed class ReadmeRunner : MonoBehaviour
    {
        public static ReadmeRunner Instance;

        struct Job { public string Name; public int W, H; }
        readonly Queue<Job> _jobs = new Queue<Job>();

        public bool IsRecording { get; private set; }
        public int Written { get; private set; }
        public string Note = "-";

        string _prefix;
        int _frames, _rw, _rh, _index;
        bool _auto;

        public static ReadmeRunner Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("__ReadmeRunner") { hideFlags = HideFlags.DontSave };
                Instance = go.AddComponent<ReadmeRunner>();
            }
            return Instance;
        }

        void Awake()
        {
            Instance = this;
            StartCoroutine(CaptureLoop());
        }

        public void Enqueue(string name, int w, int h) => _jobs.Enqueue(new Job { Name = name, W = w, H = h });

        public void BeginRecord(string prefix, int frames, int fps, int w, int h, bool autoplay)
        {
            _prefix = prefix; _frames = frames; _rw = w; _rh = h;
            _index = 0; Written = 0;
            _auto = autoplay;
            IsRecording = true;
            Time.captureFramerate = fps;   // kayıt duvar saatinden kopar
            if (_auto) Bind();
        }

        public void EndRecord()
        {
            IsRecording = false;
            Time.captureFramerate = 0;
            if (_up != null && _phase == Phase.Drag) _up(ScreenOf(_pos + _half));
            _phase = Phase.Think;
        }

        IEnumerator CaptureLoop()
        {
            var wait = new WaitForEndOfFrame();
            while (true)
            {
                yield return wait;

                while (_jobs.Count > 0)
                {
                    var job = _jobs.Dequeue();
                    Grab(job.Name, job.W, job.H);
                }

                if (IsRecording)
                {
                    Grab(string.Format("{0}_{1:0000}", _prefix, _index), _rw, _rh);
                    _index++;
                    if (_index >= _frames) EndRecord();
                }
            }
        }

        void Grab(string name, int w, int h)
        {
            int sw = Screen.width, sh = Screen.height;
            var full = RenderTexture.GetTemporary(sw, sh, 0, RenderTextureFormat.ARGB32);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(full);

            RenderTexture target = full;
            RenderTexture scaled = null;
            if (w > 0 && h > 0 && (w != sw || h != sh))
            {
                scaled = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                scaled.filterMode = FilterMode.Bilinear;
                Graphics.Blit(full, scaled);
                target = scaled;
            }

            var prev = RenderTexture.active;
            RenderTexture.active = target;
            var tex = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            // D3D'de ekran arabelleğinin başlangıcı ÜSTTEDİR; ReadPixels alttan
            // sayar. Çevirmezsek bütün kareler baş aşağı çıkar.
            if (SystemInfo.graphicsUVStartsAtTop) FlipVertically(tex);

            Directory.CreateDirectory(ReadmeCapture.Dir);
            File.WriteAllBytes(Path.Combine(ReadmeCapture.Dir, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            RenderTexture.ReleaseTemporary(full);
            if (scaled != null) RenderTexture.ReleaseTemporary(scaled);
            Written++;
        }

        static void FlipVertically(Texture2D tex)
        {
            var pixels = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            var row = new Color32[w];
            for (int y = 0; y < h / 2; y++)
            {
                int top = y * w, bottom = (h - 1 - y) * w;
                System.Array.Copy(pixels, top, row, 0, w);
                System.Array.Copy(pixels, bottom, pixels, top, w);
                System.Array.Copy(row, 0, pixels, bottom, w);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }

        // ————————————————————————————————— otomatik oynatma

        GameSession _session;
        PointerInputService _input;
        LevelModel _level;
        GateSystem _gates;
        BoardSpace _space;
        float _substep = 0.05f, _epsilon = 0.001f;

        System.Action<Vector2> _down, _held, _up;

        enum Phase { Think, Drag, Settle }
        Phase _phase = Phase.Think;
        readonly List<Vector2> _path = new List<Vector2>();
        BlockModel _block;
        Vector2 _half, _pos;
        int _leg, _settle;

        static readonly Vector2[] Dirs = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };

        void Bind()
        {
            _session = Object.FindFirstObjectByType<GameSession>();
            if (_session == null) { _auto = false; Note = "GameSession yok"; return; }

            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            var t = typeof(GameSession);
            _level = _session.ActiveLevel;
            _gates = Field(t, "_gates", F, _session) as GateSystem;
            _input = Field(t, "input", F, _session) as PointerInputService;
            var config = Field(t, "config", F, _session) as GameConfigSO;
            if (config != null) { _substep = config.dragSubstep; _epsilon = config.collisionEpsilon; }
            if (_level != null) _space = new BoardSpace(_level.Board.Width, _level.Board.Height);

            if (_input != null)
            {
                var it = typeof(PointerInputService);
                _down = Field(it, "PointerDown", F, _input) as System.Action<Vector2>;
                _held = Field(it, "PointerHeld", F, _input) as System.Action<Vector2>;
                _up = Field(it, "PointerUp", F, _input) as System.Action<Vector2>;
            }

            if (_level == null || _gates == null || _down == null)
            {
                Note = string.Format("bağlanamadı level={0} gates={1} down={2}",
                    _level != null, _gates != null, _down != null);
                _auto = false;
            }
            else Note = "bağlandı";
        }

        static object Field(System.Type type, string name, BindingFlags flags, object target)
        {
            var f = type.GetField(name, flags);
            return f == null ? null : f.GetValue(target);
        }

        void Update()
        {
            if (!_auto || !IsRecording) return;
            if (_session == null) { _auto = false; return; }
            if (_level != _session.ActiveLevel) Bind();   // sonraki bölüme geçildi

            switch (_phase)
            {
                case Phase.Think: Think(); break;
                case Phase.Drag: Drag(); break;
                case Phase.Settle: if (--_settle <= 0) _phase = Phase.Think; break;
            }
        }

        /// <summary>Çıkışa giden yolu olan bir blok seç; yoksa bekle.</summary>
        void Think()
        {
            _block = null;
            _path.Clear();
            if (_level == null || _level.Blocks.Count == 0) { _settle = 20; _phase = Phase.Settle; return; }

            var obstacles = new List<Aabb>();
            foreach (var block in _level.Blocks)
            {
                if (block.IsFrozen) continue;
                if (TryFindPath(block, obstacles, _path)) { _block = block; break; }
            }

            if (_block == null) { _settle = 20; _phase = Phase.Settle; Note = "hamle yok"; return; }

            // TUTMA NOKTASI: bloğun İLK HÜCRESİNİN ortası.
            //
            // DERS (kutunun ortası bloğun üstü değildir): Önce sınırlayıcı
            // kutunun merkezi kullanılıyordu ve iki ayrı sebeple ıskalıyordu —
            // (1) `Aabb.FromRect` MaxX'i x+W verir, yani genişlik zaten
            // MaxX−MinX'tir; "+1" parmağı bir hücre dışarı taşıyordu,
            // (2) L biçimli bir polyomino'nun kutu merkezi BOŞ olabilir.
            // Sonuç: dokunuş hiçbir bloğu tutmuyordu, tahta 300 kare boyunca
            // hiç kıpırdamadı ve hata "sürüklüyor" yazdığı için sessiz kaldı.
            var first = _block.Cells[0];
            _half = new Vector2(first.x + 0.5f, first.y + 0.5f);
            _pos = _block.Position;
            _leg = 0;
            _down(ScreenOf(_pos + _half));
            _phase = Phase.Drag;
            Note = "sürüklüyor";
        }

        void Drag()
        {
            if (_block == null || !_level.Blocks.Contains(_block) || _leg >= _path.Count)
            {
                if (_up != null) _up(ScreenOf(_pos + _half));
                _settle = 12;
                _phase = Phase.Settle;
                return;
            }

            var target = _path[_leg];
            _pos = Vector2.MoveTowards(_pos, target, 0.34f);   // hücre/kare — insan hızına yakın
            if (_held != null) _held(ScreenOf(_pos + _half));
            if (_pos == target) _leg++;
        }

        Vector2 ScreenOf(Vector2 cell)
        {
            var cam = Camera.main;
            if (cam == null) return Vector2.zero;
            return cam.WorldToScreenPoint(_space.CornerToWorld(cell.x, cell.y));
        }

        /// <summary>
        /// Bloğun kapıya değdiği ilk konuma giden ADIM ADIM yolu bulur —
        /// LevelSolver'ın erişilebilirlik aramasının yol saklayan ikizi.
        /// Blok arama sırasında oynatılır, sonunda yerine konur.
        /// </summary>
        bool TryFindPath(BlockModel block, List<Aabb> obstacles, List<Vector2> path)
        {
            obstacles.Clear();
            _level.CollectObstacles(obstacles, block);

            var start = block.Position;
            var parent = new Dictionary<Vector2, Vector2>();
            var seen = new HashSet<Vector2> { start };
            var queue = new Queue<Vector2>();
            queue.Enqueue(start);

            Vector2 found = start;
            bool ok = false;

            while (queue.Count > 0 && !ok)
            {
                var pos = queue.Dequeue();
                block.Position = pos;
                if (_gates.CanResolve(block)) { found = pos; ok = true; break; }

                for (int i = 0; i < Dirs.Length; i++)
                {
                    var solved = DragSolver.Solve(pos, pos + Dirs[i], block.Cells,
                        obstacles, _substep, _epsilon, block.Axis);
                    var cell = new Vector2(Mathf.Round(solved.x), Mathf.Round(solved.y));
                    if (cell == pos || !seen.Add(cell)) continue;
                    parent[cell] = pos;
                    queue.Enqueue(cell);
                }
            }

            block.Position = start;
            if (!ok) return false;

            path.Clear();
            for (var at = found; at != start; at = parent[at]) path.Add(at);
            path.Reverse();
            path.Add(found);   // son adımı yineleyip temasın kaydedilmesini garantiler
            return true;
        }

        void OnDestroy()
        {
            Time.captureFramerate = 0;
            if (Instance == this) Instance = null;
        }
    }
}
