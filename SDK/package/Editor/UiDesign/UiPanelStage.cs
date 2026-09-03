using System.Collections.Generic;
using GameKit.Editor.Setup;
using GameKit.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit.Editor.UiDesign
{
    /// <summary>
    /// Bir paneli SAHNEYE DOKUNMADAN kuran, dokuya çizen ve tıklamayı öğeye
    /// çeviren geçici tezgâh.
    ///
    /// DERS (araç sahneyi KİRLETMEMELİ — 14. turda pahalıya öğrenildi): Bir
    /// yakalama komutu istisna atınca kurduğu "OnizlemeKanvas" sahnede kaldı ve
    /// kullanıcının menüsü bozuk açıldı. Buradaki iki koruma o olayın karşılığı:
    ///   • Her nesne <see cref="HideFlags.HideAndDontSave"/> ile doğuyor —
    ///     hiyerarşide görünmüyor, sahneye KAYDEDİLEMİYOR, sahne değişince
    ///     kendiliğinden gidiyor.
    ///   • Kurulum istisna atarsa <see cref="Dispose"/> yine de koşuyor ve
    ///     yarım kalan her şeyi siliyor; hata metni panelin yerine gösteriliyor.
    ///
    /// DERS (kamera KADRAJI kadar KONUMU da önemli): Tezgâh sahnenin ta uzağında,
    /// (100000, 100000) civarında kuruluyor. Başlangıçta orijindeydi ve oyun
    /// tahtasının küpleri panelin arkasında görünüyordu — önizleme "arka planı
    /// bozuk" sanılıyordu. Katman maskesiyle çözmek mümkün değil: arayüz
    /// nesneleri Default katmanında doğuyor, yani tahtayla aynı katmanda.
    /// </summary>
    public sealed class UiPanelStage : System.IDisposable
    {
        /// <summary>Önizleme dokusunun ölçüsü — referansın yarısı.</summary>
        public const int Width  = 540;
        public const int Height = 960;

        /// <summary>Tezgâhın sahneden uzaklığı (bkz. sınıf notu).</summary>
        static readonly Vector3 FarAway = new Vector3(100000f, 100000f, -1000f);

        GameObject _root;
        Camera _camera;
        Canvas _canvas;
        RenderTexture _texture;

        readonly List<(Transform Root, string Key)> _roots = new List<(Transform, string)>();
        readonly Dictionary<string, UiNodeSnapshot> _baseline = new Dictionary<string, UiNodeSnapshot>();

        public RenderTexture Texture => _texture;
        public Camera Camera => _camera;
        public string Error { get; private set; }
        public bool Ok => Error == null && _canvas != null;

        /// <summary>Kurulduğu andaki (kodun ürettiği) hâl — farkın tabanı.</summary>
        public IReadOnlyDictionary<string, UiNodeSnapshot> Baseline => _baseline;

        /// <summary>İşaretli kökler: düzeltme yollarının başlangıç noktaları.</summary>
        public IReadOnlyList<(Transform Root, string Key)> Roots => _roots;

        // ------------------------------------------------------------- kurulum

        public static UiPanelStage Build(UiPanelCatalog.Entry entry, int variant)
        {
            var stage = new UiPanelStage();
            stage.Setup(entry, variant);
            return stage;
        }

        void Setup(UiPanelCatalog.Entry entry, int variant)
        {
            UiPanelFixture.Ensure();

            _root = new GameObject("UiPanelStage") { hideFlags = HideFlags.HideAndDontSave };
            _root.transform.position = FarAway;

            _texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            { name = "UiPanelStageRT" };

            var cameraGo = new GameObject("StageCamera") { hideFlags = HideFlags.HideAndDontSave };
            cameraGo.transform.SetParent(_root.transform, worldPositionStays: false);
            _camera = cameraGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            // Zemin, oyunun kendi arka planına yakın koyu mor: saydam bölgeleri
            // satranç deseninde değil DÜZ KOYU zeminde yargılamak gerekiyor
            // (bkz. UiCaptureTool'daki saydamlık dersi).
            _camera.backgroundColor = new Color(0.05f, 0.03f, 0.12f, 1f);
            _camera.targetTexture = _texture;
            _camera.enabled = false;              // yalnız elle Render()
            _camera.cullingMask = ~0;

            var canvasGo = new GameObject("StageCanvas", typeof(Canvas), typeof(CanvasScaler))
            { hideFlags = HideFlags.HideAndDontSave };
            canvasGo.transform.SetParent(_root.transform, worldPositionStays: false);

            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _camera;
            _canvas.planeDistance = 50f;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiKit.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // Menü sayfaları çubuğun üstündeki çerçeveye kuruluyor; önizleme de
            // aynı çerçeveyi kurmazsa sayfa gerçekte olduğundan uzun görünür.
            Transform parent = _canvas.transform;
            if (entry.ContentFrame)
            {
                var content = UiKit.CreateRect("Content", _canvas.transform);
                UiKit.Place(content, 0f, 0.105f, 1f, 1f);
                parent = content;
            }

            // SAF KOD ÇIKTISI ÖLÇÜLÜYOR: bazı paneller düzeltmelerini kendi
            // kurulumlarının sonunda uyguluyor. Askıya almazsak "varsayılan"
            // ölçümü zaten düzeltilmiş hâli gösterir ve araç hiçbir fark
            // göremez (bkz. UiTweak.Suspended).
            bool suspendedBefore = UiTweak.Suspended;
            UiTweak.Suspended = true;
            try
            {
                var marked = entry.Build(parent, variant);
                if (!string.IsNullOrEmpty(entry.Key) && marked != null)
                    UiTweak.Mark(marked, entry.Key);
            }
            catch (System.Exception exception)
            {
                Error = exception.GetType().Name + ": " + exception.Message;
                Debug.LogWarning($"[Arayüz Tasarımı] '{entry.Label}' kurulamadı — {Error}");
            }
            finally
            {
                UiTweak.Suspended = suspendedBefore;
            }

            CollectRoots();

            // TABAN, YERLEŞİM OTURDUKTAN SONRA ALINIR. Kurulum biter bitmez
            // ölçmek, henüz hesaplanmamış değerleri "kodun verdiği hâl" diye
            // kaydetmek olurdu; ilk hesaptan sonra o değerler kendiliğinden
            // değişir ve araç, kimsenin dokunmadığı öğelerde SAHTE farklar
            // görür. (Somut olarak: otomatik küçülen yazıların puntosu.)
            Canvas.ForceUpdateCanvases();

            foreach (var pair in _roots) UiTweakDiff.Collect(pair.Root, pair.Key, _baseline);

            // Taban alındıktan SONRA kayıtlı düzeltmeler biniyor.
            foreach (var pair in _roots) UiTweak.Apply(pair.Root, pair.Key);

            Render();
        }

        void CollectRoots()
        {
            if (_canvas == null) return;

            foreach (var marker in _canvas.GetComponentsInChildren<UiTweakRoot>(includeInactive: true))
                if (!string.IsNullOrEmpty(marker.Key))
                    _roots.Add((marker.transform, marker.Key));
        }

        // -------------------------------------------------------------- çizim

        public void Render()
        {
            if (_camera == null || _canvas == null) return;

            Canvas.ForceUpdateCanvases();
            // Derlenmemiş bir shader varyantı kareyi MAGENTA çizer ve hata
            // vermez — araç yalan söyler, kod suçlanır. Gerekçe:
            // EditorCapture.EnsureSynchronousShaders.
            GameKit.Editor.Setup.EditorCapture.EnsureSynchronousShaders();
            _camera.Render();
        }

        /// <summary>Kurulmuş ağacın ŞU ANKİ hâli — farkın öbür ucu.</summary>
        public Dictionary<string, UiNodeSnapshot> ReadCurrent()
        {
            var current = new Dictionary<string, UiNodeSnapshot>();
            foreach (var pair in _roots)
                if (pair.Root != null) UiTweakDiff.Collect(pair.Root, pair.Key, current);
            return current;
        }

        /// <summary>Bir öğenin düzeltme yolu — kökten aşağı adlar.</summary>
        public string PathOf(Transform node)
        {
            foreach (var pair in _roots)
            {
                if (node == pair.Root) return pair.Key;
                if (!node.IsChildOf(pair.Root)) continue;

                string path = node.name;
                var walker = node.parent;
                while (walker != null && walker != pair.Root)
                {
                    path = walker.name + "/" + path;
                    walker = walker.parent;
                }
                return pair.Key + "/" + path;
            }
            return null;
        }

        // ------------------------------------------------------------- seçim

        /// <summary>
        /// Doku üzerindeki bir noktanın altındaki öğeyi bulur.
        ///
        /// DERS (üstteki, EN DERİN olan değildir): İlk hâli "en derin çocuğu al"
        /// diyordu ve hep en alttaki perdeyi seçiyordu. uGUI'de çizim sırası
        /// KARDEŞ SIRASIDIR: sonra gelen üste çizilir. Bu yüzden aday listesi
        /// derinlik-öncelikli gezinme sırasına göre numaralanıp EN SON çizilen
        /// seçiliyor — yani oyuncunun gerçekten gördüğü.
        /// </summary>
        public Transform Pick(Vector2 texturePoint)
        {
            if (_canvas == null) return null;

            Transform best = null;
            int bestOrder = -1, order = 0;
            Walk(_canvas.transform, ref order, texturePoint, ref best, ref bestOrder);
            return best;
        }

        void Walk(Transform node, ref int order, Vector2 point,
                  ref Transform best, ref int bestOrder)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                var child = node.GetChild(i);
                order++;

                if (child.gameObject.activeInHierarchy &&
                    child is RectTransform rect &&
                    child.GetComponent<Graphic>() is Graphic graphic &&
                    graphic.enabled && graphic.color.a > 0.02f &&
                    RectTransformUtility.RectangleContainsScreenPoint(rect, point, _camera))
                {
                    if (order > bestOrder) { best = child; bestOrder = order; }
                }

                Walk(child, ref order, point, ref best, ref bestOrder);
            }
        }

        /// <summary>Bir öğenin doku üzerindeki dikdörtgeni (seçim çerçevesi için).</summary>
        public bool TryGetTextureRect(Transform node, out Rect rect)
        {
            rect = default;
            if (node is not RectTransform target || _camera == null) return false;

            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var point = RectTransformUtility.WorldToScreenPoint(_camera, corners[i]);
                minX = Mathf.Min(minX, point.x); maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y); maxY = Mathf.Max(maxY, point.y);
            }

            rect = new Rect(minX, minY, maxX - minX, maxY - minY);
            return rect.width > 0f && rect.height > 0f;
        }

        // ------------------------------------------------------------- temizlik

        public void Dispose()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (_root != null) Object.DestroyImmediate(_root);
            if (_texture != null) { _texture.Release(); Object.DestroyImmediate(_texture); }

            _root = null; _camera = null; _canvas = null; _texture = null;
            _roots.Clear();
            _baseline.Clear();
        }
    }
}
