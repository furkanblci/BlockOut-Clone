using BlockOut.Core;
using BlockOut.Runtime.Board;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.View;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Bölümü OYUNUN kendi kurulum koduyla kurup editör içinde render eder.
    /// Hem Tahta sekmesinin tuvali hem 3D Önizleme sekmesi bunu kullanır.
    ///
    /// DERS (tek gerçeklik kaynağı, iki kamera): Editörde bölümün iki farklı
    /// görüntüsü gerekir —
    ///   • TEPEDEN, ortografik: düzenlemek için. 1 hücre = 1 dünya birimi
    ///     olduğundan hücre↔piksel eşlemesi DOĞRUSAL kalır, sürükleme şaşmaz.
    ///   • OYUN KAMERASI, perspektif: oyuncunun gördüğü kadraj.
    /// İkisi de AYNI kurulmuş tahtayı render eder; nesneler bir kez kurulur.
    /// Böylece "editörde başka, oyunda başka" sınıfı hatalar yapısal olarak
    /// imkânsızlaşır — çizen kod oyunun kodudur.
    ///
    /// DERS (neden 90°, oyundaki 80° değil): Perspektif bir kamerada bloklar
    /// tahtanın kenarına doğru KAYAR; hücre ile piksel arasındaki bağ bozulur ve
    /// tıklama yanlış hücreye düşer. Ortografik tepeden bakış bu bağı korur.
    /// Oyuncunun gerçek açısı 3D Önizleme sekmesinde duruyor; ikisi birlikte
    /// hem doğru düzenleme hem doğru kadraj veriyor.
    /// </summary>
    public sealed class LevelBoardPreview : System.IDisposable
    {
        PreviewRenderUtility _utility;
        GameObject _root;

        /// <summary>Kurulu tahtanın hangi bölüm+sürümden geldiği.</summary>
        string _builtPath;
        int _builtRevision = -1;

        /// <summary>Kurulum sırasında oluşan son hata; yoksa null.</summary>
        public string Error { get; private set; }

        public bool Ready => _root != null;

        /// <summary>
        /// Tahtayı gerekiyorsa (yeniden) kurar. Veri değişmedikçe kurulum
        /// tekrarlanmaz — 60 mesh'i her karede yeniden üretmek aracı
        /// dizlerine çökertirdi.
        /// </summary>
        /// <param name="key">
        /// Bölümü tekilleştiren anahtar (dosya yolu gibi); null olabilir.
        /// </param>
        /// <param name="revision">
        /// Bölümün yapısal sürümü. DEĞİŞMEDİKÇE tahta yeniden kurulmaz.
        ///
        /// DERS (ucuz karşılaştırma): Buradaki imza eskiden bölümün TAM JSON'u
        /// idi ve her OnGUI geçişinde üretiliyordu — Layout, olay, Repaint ve
        /// her fare hareketi için ayrı ayrı. 50 bloklu bir bölümde saniyede
        /// onlarca tam serileştirme demekti. Bir tamsayı aynı işi bedelsiz
        /// görüyor; sayacı artırmayı unutmak riski ise tek bir yerde
        /// (AfterChange) toplandığı için düşük.
        /// </param>
        public bool Ensure(LevelData data, ColorPaletteSO palette, string key, int revision)
        {
            if (data == null || palette == null) { Error = "palet ya da bölüm yok"; return false; }
            if (_utility == null) _utility = new PreviewRenderUtility();

            if (_root != null && revision == _builtRevision && key == _builtPath) return true;

            Release();
            _builtPath = key;
            _builtRevision = revision;
            Error = null;

            LevelModel level;
            try
            {
                level = LevelModel.Build(data);
            }
            catch (System.Exception error)
            {
                // Yarım kalmış bölüm (rengi olmayan blok, bozuk kapı kenarı)
                // modeli kuramaz. Sessizce boş ekran göstermek yerine sebebi
                // taşıyoruz; çağıran onu kullanıcıya yazar.
                Error = error.Message;
                return false;
            }

            _root = new GameObject("LevelEditorPreview") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                BoardBuilder.Build(_root.transform,
                    level, new BoardSpace(data.Board.Width, data.Board.Height), palette);
            }
            catch (System.Exception error)
            {
                Error = error.Message;
                Release();
                return false;
            }

            // Alt nesneler BoardBuilder içinde doğuyor ve hideFlags'i
            // devralmıyorlar; sahneye sızmasınlar diye tek tek işaretlenir.
            MarkHidden(_root.transform);
            _utility.AddSingleGO(_root);
            return true;
        }

        static void MarkHidden(Transform node)
        {
            node.gameObject.hideFlags = HideFlags.HideAndDontSave;
            for (int i = 0; i < node.childCount; i++) MarkHidden(node.GetChild(i));
        }

        /// <summary>
        /// TEPEDEN ortografik render. <paramref name="marginCells"/> tahtanın
        /// dışına taşan çerçeve ve kapı barları için pay bırakır; çağıran aynı
        /// payı piksel tarafında da uygulamak zorundadır, yoksa görüntü kayar.
        /// </summary>
        public Texture RenderTopDown(Rect area, int boardWidth, int boardHeight, float marginCells)
        {
            if (!Ready || area.width < 4f || area.height < 4f) return null;

            var cam = BeginCamera(area);
            cam.orthographic = true;

            float halfHeight = boardHeight * 0.5f + marginCells;
            float halfWidth = boardWidth * 0.5f + marginCells;
            cam.orthographicSize = halfHeight;
            cam.aspect = halfWidth / halfHeight;

            // Euler(90,0,0): ileri -Y (aşağı bakar), yukarı +Z. Dünya +Z tahtanın
            // ÜST kenarı (hücre y=0) olduğu için ekran yönü tuvalle örtüşür.
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 40f;
            cam.transform.SetPositionAndRotation(
                new Vector3(0f, 20f, 0f), Quaternion.Euler(90f, 0f, 0f));

            cam.Render();
            return _utility.EndPreview();
        }

        /// <summary>
        /// Perspektif render. <paramref name="gameCamera"/> açıkken GameSession'ın
        /// kadrajı birebir tekrarlanır (80° eğim, 27° FOV, köşeler görünene kadar
        /// ikili aramayla uzaklaştırma).
        /// </summary>
        public Texture RenderPerspective(Rect area, int boardWidth, int boardHeight,
            bool gameCamera, float pitch, float yaw, float zoom)
        {
            if (!Ready || area.width < 4f || area.height < 4f) return null;

            var cam = BeginCamera(area);
            cam.orthographic = false;
            cam.fieldOfView = 27f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            cam.aspect = Mathf.Max(0.1f, area.width / Mathf.Max(1f, area.height));

            var rotation = Quaternion.Euler(gameCamera ? 80f : pitch, gameCamera ? 0f : yaw, 0f);
            Vector3 forward = rotation * Vector3.forward;

            // Kapı barları ve çerçeve tahta sınırının dışına taşar → kenar payı.
            float hw = boardWidth * 0.5f + 0.9f;
            float hh = boardHeight * 0.5f + 0.9f;
            var corners = new[]
            {
                new Vector3(-hw, 0f, -hh), new Vector3(hw, 0f, -hh),
                new Vector3(-hw, 0f,  hh), new Vector3(hw, 0f,  hh),
                new Vector3(-hw, 0.7f, -hh), new Vector3(hw, 0.7f, -hh),
                new Vector3(-hw, 0.7f,  hh), new Vector3(hw, 0.7f,  hh)
            };

            float near = 4f, far = 160f;
            for (int i = 0; i < 20; i++)
            {
                float mid = (near + far) * 0.5f;
                cam.transform.SetPositionAndRotation(-forward * mid, rotation);
                if (AllCornersVisible(cam, corners)) far = mid;
                else near = mid;
            }

            cam.transform.SetPositionAndRotation(-forward * far * zoom, rotation);
            cam.Render();
            return _utility.EndPreview();
        }

        Camera BeginCamera(Rect area)
        {
            var cfg = VisualSettings.Current;
            _utility.BeginPreview(area, GUIStyle.none);

            var cam = _utility.camera;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = cfg != null ? cfg.backgroundOuter : new Color(0.08f, 0.06f, 0.18f);

            // DERS (önizleme sahnesinde IŞIK YOKTUR): Blok materyalleri kendi
            // ışıklandırmasını yapıyor (_LightDir/_Ambient) ve doğru çıkıyor;
            // ama zemin, çerçeve ve duvarlar STANDART aydınlatılmış materyal.
            // Önizleme sahnesinde sahne ışığı/ortam ışığı olmadığı için onlar
            // neredeyse siyah render ediliyordu (ölçüm: zemin 2,1,7 — oysa
            // ayarda 30,30,84). Ortam rengi ve yönlü ışıklar bu yüzden ELLE
            // kuruluyor; aksi hâlde "gerçek görsel" yalnız blokları gerçek,
            // tahtayı kapkara gösterirdi.
            _utility.ambientColor = new Color(0.62f, 0.62f, 0.68f, 1f);

            if (_utility.lights != null && _utility.lights.Length > 0)
            {
                var dir = cfg != null ? cfg.lightDirection.normalized : new Vector3(0.45f, 0.8f, -0.4f);

                var key = _utility.lights[0];
                key.enabled = true;
                key.type = LightType.Directional;
                key.color = Color.white;
                key.intensity = 1.05f;
                key.transform.rotation = Quaternion.LookRotation(-dir);

                if (_utility.lights.Length > 1)
                {
                    var fill = _utility.lights[1];
                    fill.enabled = true;
                    fill.type = LightType.Directional;
                    fill.color = Color.white;
                    fill.intensity = 0.4f;
                    fill.transform.rotation = Quaternion.LookRotation(
                        new Vector3(-dir.x, dir.y * 0.4f, -dir.z));
                }
            }
            return cam;
        }

        static bool AllCornersVisible(Camera cam, Vector3[] points)
        {
            foreach (var point in points)
            {
                var viewport = cam.WorldToViewportPoint(point);
                if (viewport.z <= 0f) return false;
                if (viewport.x < 0.02f || viewport.x > 0.98f) return false;
                if (viewport.y < 0.02f || viewport.y > 0.98f) return false;
            }
            return true;
        }

        /// <summary>Kurulmuş tahtayı atar; bir sonraki Ensure yeniden kurar.</summary>
        public void Release()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            _root = null;
            _builtPath = null;
            _builtRevision = -1;
        }

        /// <summary>
        /// <see cref="PreviewRenderUtility"/> bir RenderTexture ve gizli bir
        /// sahne tutar; pencere kapanınca serbest bırakılmazsa editör oturumu
        /// boyunca sızar.
        /// </summary>
        public void Dispose()
        {
            Release();
            if (_utility == null) return;
            _utility.Cleanup();
            _utility = null;
        }
    }
}
