using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Editor.ProjectSetup;
using BlockOut.Runtime.Config;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Bölüm tasarım aracı — JSON'u elle yazmayı bitirir.
    ///
    /// DERS (iç araçlar): Stüdyolarda içerik üretim hızını belirleyen şey
    /// motorun kendisi değil, tasarımcının kullandığı araçtır. İyi bir editörün
    /// şartları: geri alınabilir olmalı, seçilen nesne düzenlenebilmeli, hata
    /// anında görünmeli, işi asla kaybettirmemeli.
    ///
    /// DERS (araç MİMARİSİ — neden sekmeli kabuk): Bu pencere tek bir yan panele
    /// her şeyi yığmak yerine SEKMELERE bölünmüştür. Sebebi estetik değil:
    /// bir bölüm tasarlamak dört ayrı işten oluşur ve her biri farklı bir
    /// ekran düzeni ister —
    ///   • tek bölümü ÇİZMEK          → geniş tuval + dar denetçi (Tahta)
    ///   • setin BÜTÜNÜNÜ görmek      → sayfa boyu minyatür ızgarası (Galeri)
    ///   • setin SAĞLIĞINI görmek     → sıralanabilir liste (Doğrula)
    ///   • bölümü ANLAMAK             → hamle hamle oynatma (Çözüm)
    /// Aynı ekrana sığdırılmaya çalışıldığında dördü de yarım kalır. Kabuk
    /// (üst şerit, sekme çubuğu, durum çubuğu) her sekmede AYNI durur; değişen
    /// yalnızca ortadaki gövdedir. Bu düzen bir sonraki oyunda da kullanılabilir:
    /// oyuna özel olan tek şey Tahta sekmesinin içidir.
    ///
    /// Araç, oyunun yüklediği <see cref="LevelData"/> DTO'larını doğrudan
    /// düzenler — ayrı bir editör modeli YOKTUR.
    ///
    /// Sınıf parçalara bölünmüştür: bu dosya (durum, yaşam döngüsü, kabuk),
    /// <c>.TopBar</c> (üst şerit ve dosya menüleri), <c>.Board</c> (tuval ve
    /// Tahta sekmesi), <c>.Panels</c> (denetçi arayüzü), <c>.Tabs</c> (diğer
    /// sekmeler), <c>.Input</c> (etkileşim ve düzenleme).
    /// </summary>
    public sealed partial class LevelEditorWindow : EditorWindow
    {
        /// <summary>Kabuk sekmeleri. Sıra ekranda göründüğü sıradır.</summary>
        enum Tab { Dashboard, Board, Gallery, Preview, Validate, Solution, Tools, Reference, Guide }

        enum Tool { Select, Shape, Blocks, Gates, Walls, Curtain, Generator }
        enum SelKind { None, Block, Gate, Curtain, Content, Generator, Queued }

        [System.Serializable]
        struct Selection
        {
            public SelKind Kind;
            public int Index;   // blok / kapı / perde dizini
            public int Sub;     // perde içeriği dizini
            public static Selection None => new Selection { Kind = SelKind.None };

            public bool Same(Selection other) =>
                Kind == other.Kind && Index == other.Index && Sub == other.Sub;
        }

        static readonly (Tab Id, string Label, string Tip)[] TabInfo =
        {
            (Tab.Dashboard, "Pano",     "Setin özeti ve hızlı eylemler"),
            (Tab.Board,     "Tahta",    "Bölümü çiz — tuval, denetçi, kapı şeridi"),
            (Tab.Gallery,   "Galeri",   "Bütün bölümler minyatür olarak"),
            (Tab.Preview,   "3D Önizleme", "Bölümü oyunun gerçek mesh ve materyalleriyle gör"),
            (Tab.Validate,  "Doğrula",  "Toplu doğrulama ve sağlık listesi"),
            (Tab.Solution,  "Çözüm",    "Çözümü hamle hamle oynat"),
            (Tab.Tools,     "Araçlar",  "Tahta dönüşümleri ve toplu işlemler"),
            (Tab.Reference, "Referans", "Videodan kare al, tuvale bindir"),
            (Tab.Guide,     "Kılavuz",  "Kısayollar ve mekanik notları")
        };

        static readonly (string label, string tip)[] ToolInfo =
        {
            ("Seç",   "Nesne seç ve sürükleyerek taşı · boş alanda sürükle = kutu seçim · Alt+sürükle = kopyala (1)"),
            ("Şekil", "Hücreleri aç/kapa — sürükleyerek boya (2)"),
            ("Blok",  "Blok yerleştir; perde içine koyarsan gizli içerik olur (3)"),
            ("Kapı",  "En yakın kenara kapı koy (4)"),
            ("Duvar", "İç duvar çiz — sürükleyerek uzat (5)"),
            ("Perde", "Sürükleyerek perde bölgesi seç (6)"),
            ("Üreteç", "En yakın kenara blok üreteci koy; sırayı alt şeritten doldur (7)")
        };

        static readonly string[] Difficulties = { "normal", "hard", "superhard" };

        /// <summary>
        /// Şekil paleti. Her ön ayar bir hücre maskesidir ('X' dolu, '.' boş) —
        /// dikdörtgenler maskenin tamamen dolu olduğu özel hâl. Referans oyunda
        /// L ve T parçaları bol kullanıldığı için palette hazır dururlar.
        /// </summary>
        static readonly (string Label, string[] Rows)[] ShapePresets =
        {
            ("1×1",  new[] { "X" }),
            ("2×1",  new[] { "XX" }),
            ("1×2",  new[] { "X", "X" }),
            ("2×2",  new[] { "XX", "XX" }),
            ("3×1",  new[] { "XXX" }),
            ("1×3",  new[] { "X", "X", "X" }),
            ("3×2",  new[] { "XXX", "XXX" }),
            ("2×3",  new[] { "XX", "XX", "XX" }),
            ("3×3",  new[] { "XXX", "XXX", "XXX" }),
            ("L",    new[] { "X.", "XX" }),
            ("J",    new[] { ".X", "XX" }),
            ("L uzun", new[] { "X.", "X.", "XX" }),
            ("T",    new[] { "XXX", ".X." }),
            ("T ters", new[] { ".X.", "XXX" }),
            ("S",    new[] { ".XX", "XX." }),
            ("Z",    new[] { "XX.", ".XX" }),
            ("U",    new[] { "X.X", "XXX" }),
            ("Artı", new[] { ".X.", "XXX", ".X." })
        };

        // ---- domain reload'ı aşan durum ----
        // DERS: EditorWindow'un düz C# alanları Play'e girerken (domain reload)
        // SIFIRLANIR. Düzenlenen bölümü kaybetmemek için durumu [SerializeField]
        // bir JSON dizesine yazıp geri okuyoruz.
        [SerializeField] string _serializedData;
        [SerializeField] string _path;
        [SerializeField] Tab _tab = Tab.Board;
        [SerializeField] Tool _tool = Tool.Select;
        [SerializeField] List<string> _undoStack = new List<string>();
        [SerializeField] List<string> _redoStack = new List<string>();
        [SerializeField] List<Selection> _selections = new List<Selection>();
        [SerializeField] bool _autoValidate = true;
        [SerializeField] int _blockW = 1, _blockH = 1, _blockIce;

        /// <summary>Fırçanın hücre maskesi; boş/null ise blok _blockW×_blockH dikdörtgen.</summary>
        [SerializeField] List<string> _blockMask;

        /// <summary>Fırçanın hareket kısıtı: "" serbest, "h" yatay, "v" dikey.</summary>
        [SerializeField] string _blockAxis = "";
        [SerializeField] List<BlockColor> _layers = new List<BlockColor> { BlockColor.Red };
        [SerializeField] int _activeLayer;
        [SerializeField] BlockColor _gateColor = BlockColor.Red;
        [SerializeField] int _gateLength = 2, _gateIce;
        [SerializeField] int _curtainCount = 3;
        [SerializeField] float _zoom = 1f;
        [SerializeField] Vector2 _pan;
        [SerializeField] LevelReferenceOverlay _reference = new LevelReferenceOverlay();
        [SerializeField] bool _showLibrary = true, _showRulers = true, _showGrid = true;

        /// <summary>
        /// Tuvali oyunun gerçek mesh ve materyalleriyle çiz. Kapalıysa hızlı 2B
        /// tuğla çizimine düşer — perde içeriği gibi GİZLİ şeyleri incelemek
        /// gerektiğinde ya da çok büyük tahtalarda kullanışlı.
        /// </summary>
        [SerializeField] bool _realVisuals = true;
        [SerializeField] int _playbackStep = -1;
        [SerializeField] bool _showSolution;
        [SerializeField] string _stampName = "";

        // ---- sekmelere ait görünüm durumu ----
        [SerializeField] float _galleryScale = 1f;
        [SerializeField] string _gallerySearch = "";
        [SerializeField] int _galleryStatusFilter;         // 0 hepsi, 1 ok, 2 uyarı, 3 bozuk, 4 doğrulanmadı
        [SerializeField] int _galleryMechanicFilter;       // 0 hepsi, 1+ LevelMechanics.All dizini
        [SerializeField] string _librarySearch = "";
        [SerializeField] int _validateSort;                // 0 sıra, 1 durum, 2 hamle

        // Bekleyen mod değişiklikleri; -1 = yok. Bkz. ApplyPendingMode.
        [SerializeField] int _pendingTab = -1;
        [SerializeField] int _pendingTool = -1;

        LevelData _data;
        bool _dirty;
        readonly LevelCanvasDrawer _canvas = new LevelCanvasDrawer();
        ColorPaletteSO _palette;
        GameConfigSO _config;

        LevelReport _report;
        bool _validationStale = true;
        Vector2 _reportScroll, _libraryScroll, _panelScroll, _galleryScroll, _validateScroll,
                _dashboardScroll, _toolsScroll, _guideScroll, _solutionScroll, _gateStripScroll;
        double _lastRecoveryWrite;
        bool _recoveryAvailable;

        /// <summary>Durum çubuğunun sol tarafındaki son eylem bildirimi.</summary>
        string _status = "hazır";

        Vector2Int? _regionStart;
        Vector2Int _dragGrabOffset;
        bool _movingSelection, _moveRecorded, _boxSelecting;
        string _preMoveSnapshot;
        bool _shapePaintValue;
        readonly HashSet<EdgeId> _strokeEdges = new HashSet<EdgeId>();
        readonly HashSet<Vector2Int> _problemCells = new HashSet<Vector2Int>();

        /// <summary>Açık bölümün kimliği (pencere başlığı ve araçlar için).</summary>
        public string CurrentLevelId => _data?.Id;

        [MenuItem("Tools/Block Out/Level Editör")]
        public static LevelEditorWindow Open()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editör");
            window.minSize = new Vector2(1040, 680);
            return window;
        }

        /// <summary>Belirtilen bölümü editörde açar.</summary>
        public static LevelEditorWindow OpenLevel(string assetPath)
        {
            var window = Open();
            window.LoadFrom(assetPath);
            return window;
        }

        /// <summary>
        /// Project penceresinde bir level JSON'una çift tıklamak editörü açar.
        /// DERS: [OnOpenAsset], Unity'nin "bu dosyayı benim aracım açsın" kancası.
        /// </summary>
        [UnityEditor.Callbacks.OnOpenAsset]
        static bool OnOpenLevelAsset(int instanceId, int line)
        {
            // DERS (kullanımdan kalkan API'yi görmezden gelme): Unity 6.3'te
            // `GetAssetPath(int)` obsolete oldu — instanceId yerine EntityId
            // isteniyor. Nesne üzerinden giden aşırı yükleme her sürümde
            // geçerli ve kancanın imzasından bağımsız; uyarı da böylece
            // konsolu kirletmiyor.
            var asset = EditorUtility.InstanceIDToObject(instanceId);
            if (asset == null) return false;

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".json")) return false;
            if (!path.Replace('\\', '/').StartsWith(LevelEditorIO.LevelDir)) return false;

            OpenLevel(path);
            return true; // olayı tükettik; Unity dosyayı metin editöründe açmasın
        }

        void OnEnable()
        {
            wantsMouseMove = true;
            _palette = AssetDatabase.LoadAssetAtPath<ColorPaletteSO>(
                "Assets/_Project/ScriptableObjects/ColorPalette.asset");
            _config = AssetDatabase.LoadAssetAtPath<GameConfigSO>(
                "Assets/_Project/ScriptableObjects/GameConfig.asset");

            // Görsel ayarı editörde de yükle: tuval blokları OYUNUN ölçüleriyle
            // çiziyor (kenar payı, pah, saplama, zemin ve çerçeve renkleri) ve
            // 3D önizleme aynı asset'ten mesh üretiyor. Yüklenmezse ikisi de
            // koddaki varsayılanlara düşer ve oyundan sapar.
            var visual = AssetDatabase.LoadAssetAtPath<BlockVisualConfigSO>(
                "Assets/_Project/ScriptableObjects/BlockVisualConfig.asset");
            if (visual != null && !ReferenceEquals(BlockOut.Runtime.View.VisualSettings.Current, visual))
                BlockOut.Runtime.View.VisualSettings.Apply(visual);

            if (!string.IsNullOrEmpty(_serializedData))
            {
                try { _data = LevelEditorIO.FromJson(_serializedData); }
                catch { _data = null; }
            }

            if (_data == null)
            {
                _recoveryAvailable = LevelEditorIO.TryReadRecovery(out _);
                _data = LevelEditorIO.NewLevel();
            }

            _canvas.Zoom = _zoom;
            _canvas.Pan = _pan;
        }

        void OnDisable()
        {
            StashState();
            CleanupPreview();
        }

        void OnDestroy() => CleanupPreview();

        void StashState()
        {
            if (_data == null) return;
            _serializedData = LevelEditorIO.ToJson(_data);
            _zoom = _canvas.Zoom;
            _pan = _canvas.Pan;
        }

        // ---------------- kabuk ----------------

        /// <summary>
        /// Sekme ve araç değişimini KARENİN BAŞINDA uygular.
        ///
        /// DERS (IMGUI'de en sinsi hata): OnGUI tek karede birden çok kez koşar —
        /// önce <c>Layout</c>, sonra gerçek olay (MouseUp), sonra <c>Repaint</c>.
        /// GUILayout, kontrol yerleşimini Layout geçişinde ÖLÇER ve sonraki
        /// geçişlerde o ölçümü kullanır. Sekmeye basıldığı anda (MouseUp geçişi)
        /// gövdeyi değiştirirsen, o geçiş Layout'ta ölçülenden farklı sayıda
        /// kontrol çizer ve Unity "Getting control N's position in a group with
        /// only M controls" diye patlar.
        ///
        /// Çözüm: tıklama isteği kaydedilir, mod yalnızca Layout geçişinin
        /// başında değişir. Böylece o karenin üç geçişi de AYNI gövdeyi görür.
        /// </summary>
        void ApplyPendingMode()
        {
            if (Event.current.type != EventType.Layout) return;

            if (_pendingTab >= 0)
            {
                _tab = (Tab)_pendingTab;
                _pendingTab = -1;
                _selections.Clear();
                GUI.FocusControl(null);
            }

            if (_pendingTool >= 0)
            {
                _tool = (Tool)_pendingTool;
                _pendingTool = -1;
                if (_tool != Tool.Select) _selections.Clear();
            }
        }

        void OnGUI()
        {
            // Pencerenin TAMAMINI boya. Unity'nin varsayılan pencere zemini
            // #383838 klasik grisidir; boyanmayan her boşlukta o gri sızar ve
            // referansın iki tonlu siyahını bozar. Tek bir dolgu, bütün
            // sekmelerin altını halleder.
            LevelEditorSkin.Fill(new Rect(0f, 0f, position.width, position.height),
                LevelEditorSkin.Window);

            ApplyPendingMode();
            HandleShortcuts();
            DrawTopBar();
            DrawTabBar();
            if (_recoveryAvailable) DrawRecoveryBanner();

            switch (_tab)
            {
                case Tab.Dashboard: DrawDashboardTab(); break;
                case Tab.Board:     DrawBoardTab(); break;
                case Tab.Gallery:   DrawGalleryTab(); break;
                case Tab.Preview:   DrawPreviewTab(); break;
                case Tab.Validate:  DrawValidateTab(); break;
                case Tab.Solution:  DrawSolutionTab(); break;
                case Tab.Tools:     DrawToolsTab(); break;
                case Tab.Reference: DrawReferenceTab(); break;
                case Tab.Guide:     DrawGuideTab(); break;
            }

            DrawStatusBar();

            // Doğrulama YALNIZ Repaint'te koşar: Layout ve Repaint aynı karede
            // iki kez geçer, iki kez çözücü koşturmak aracı yarı hıza düşürürdü.
            if (_autoValidate && _validationStale && Event.current.type == EventType.Repaint)
                RunValidation();

            AutoSaveRecovery();
            if (Event.current.type == EventType.MouseMove) Repaint();
        }

        /// <summary>
        /// Sekme çubuğu. Seçili sekme altında vurgu çizgisiyle işaretlenir —
        /// düğme kabartması yerine çizgi, çünkü sekmeler bir SEÇİM kümesidir,
        /// altı ayrı eylem değil.
        /// </summary>
        void DrawTabBar()
        {
            var row = GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true));
            LevelEditorSkin.Fill(row, LevelEditorSkin.Window);
            LevelEditorSkin.Fill(new Rect(row.x, row.yMax - 1, row.width, 1), LevelEditorSkin.Hairline);

            float x = row.x + 10f;
            var style = new GUIStyle(LevelEditorSkin.RowLabel)
            { alignment = TextAnchor.MiddleCenter, fontSize = 11 };

            foreach (var info in TabInfo)
            {
                var content = new GUIContent(info.Label, info.Tip);
                float width = style.CalcSize(content).x + 26f;
                var slot = new Rect(x, row.y, width, row.height);
                bool active = _tab == info.Id;
                bool hover = slot.Contains(Event.current.mousePosition);

                if (active)
                    LevelEditorSkin.RoundedRect(
                        new Rect(slot.x + 3f, slot.y + 4f, slot.width - 6f, slot.height - 9f),
                        new Color(LevelEditorSkin.Accent.r, LevelEditorSkin.Accent.g,
                            LevelEditorSkin.Accent.b, 0.16f), LevelEditorSkin.Round6);

                if (GUI.Button(slot, GUIContent.none, GUIStyle.none) && !active)
                    RequestTab(info.Id);

                style.normal.textColor = active
                    ? Color.white
                    : hover ? LevelEditorSkin.Text : LevelEditorSkin.TextMuted;
                GUI.Label(slot, content, style);

                // Seçili sekmenin altındaki vurgu çizgisi: sekmeler bir SEÇİM
                // kümesi, altı ayrı eylem değil — düğme kabartması yerine çizgi.
                if (active)
                    LevelEditorSkin.Fill(
                        new Rect(slot.x + 8f, slot.yMax - 2f, slot.width - 16f, 2f),
                        LevelEditorSkin.Accent);

                x += width;
            }
        }

        /// <summary>Sekme değişimini kuyruğa alır — bkz. <see cref="ApplyPendingMode"/>.</summary>
        void RequestTab(Tab tab)
        {
            _pendingTab = (int)tab;
            Repaint();
        }

        /// <summary>Araç değişimini kuyruğa alır — yan panelin içeriği araca göre değişiyor.</summary>
        void RequestTool(Tool tool)
        {
            _pendingTool = (int)tool;
            Repaint();
        }

        void DrawStatusBar()
        {
            using (LevelEditorSkin.BarScope())
            {
                var tint = _report == null ? new Color(0.7f, 0.7f, 0.75f)
                    : _report.Ok ? LevelLibrary.ColorFor(LevelLibrary.Status.Ok)
                    : LevelLibrary.ColorFor(LevelLibrary.Status.Broken);

                var style = new GUIStyle(LevelEditorSkin.RowLabel);
                style.normal.textColor = tint;
                GUILayout.Label(_status, style);

                GUILayout.FlexibleSpace();

                GUILayout.Label(
                    $"{_data.Id}  │  {_data.Board.Width}×{_data.Board.Height}  │  " +
                    $"{_data.Blocks.Count} blok  │  {_data.Gates.Count} kapı  │  " +
                    $"{_data.Obstacles.Count} engel  │  seçili {_selections.Count}",
                    LevelEditorSkin.RowLabel);
            }
        }

        /// <summary>Durum çubuğuna yazar; bildirim baloncuğundan farklı olarak KALICI.</summary>
        void Say(string message)
        {
            _status = message;
            Repaint();
        }

        /// <summary>Unity çökerse iş kaybolmasın: çalışma kopyasını periyodik yaz.</summary>
        void AutoSaveRecovery()
        {
            if (!_dirty) return;
            if (EditorApplication.timeSinceStartup - _lastRecoveryWrite < 10.0) return;
            _lastRecoveryWrite = EditorApplication.timeSinceStartup;
            LevelEditorIO.WriteRecovery(_data);
        }

        void DrawRecoveryBanner()
        {
            using (LevelEditorSkin.BarScope(LevelEditorSkin.Card))
            {
                var style = new GUIStyle(LevelEditorSkin.SectionBody);
                style.normal.textColor = LevelEditorSkin.WarningBright;
                GUILayout.Label("Önceki oturumdan kurtarılabilir bir çalışma bulundu.", style);
                GUILayout.FlexibleSpace();
                if (LevelEditorSkin.BarButton("Yükle", 60f, null, LevelEditorSkin.Positive))
                {
                    if (LevelEditorIO.TryReadRecovery(out var recovered))
                    {
                        Record();
                        _data = recovered;
                        _path = null;
                        AfterChange();
                        Say("Kurtarılan çalışma yüklendi");
                    }
                    _recoveryAvailable = false;
                }
                if (LevelEditorSkin.BarButton("Yoksay", 60f))
                {
                    LevelEditorIO.ClearRecovery();
                    _recoveryAvailable = false;
                }
            }
        }

        // ---------------- geri alma ----------------

        void Record()
        {
            _undoStack.Add(LevelEditorIO.ToJson(_data));
            if (_undoStack.Count > 60) _undoStack.RemoveAt(0);
            _redoStack.Clear();
        }

        void Undo()
        {
            if (_undoStack.Count == 0) return;
            _redoStack.Add(LevelEditorIO.ToJson(_data));
            _data = LevelEditorIO.FromJson(_undoStack[_undoStack.Count - 1]);
            _undoStack.RemoveAt(_undoStack.Count - 1);
            _selections.Clear();
            AfterChange();
            Say("Geri alındı");
        }

        void Redo()
        {
            if (_redoStack.Count == 0) return;
            _undoStack.Add(LevelEditorIO.ToJson(_data));
            _data = LevelEditorIO.FromJson(_redoStack[_redoStack.Count - 1]);
            _redoStack.RemoveAt(_redoStack.Count - 1);
            _selections.Clear();
            AfterChange();
            Say("İleri alındı");
        }

        /// <summary>
        /// Bölümün YAPISAL sürümü. Her değişiklikte artar.
        ///
        /// DERS (imza olarak JSON kullanma): Önizleme "veri değişti mi?"
        /// sorusunu bölümün tamamını JSON'a çevirip karşılaştırarak
        /// cevaplıyordu — ve bunu her OnGUI geçişinde yapıyordu (Layout,
        /// olay, Repaint, üstelik her fare hareketinde). 50 bloklu bir
        /// bölümde bu saniyede onlarca tam serileştirme demek. Tek bir
        /// tamsayı aynı işi bedelsiz görüyor.
        /// </summary>
        int _revision;

        /// <summary>
        /// Seçimi bırakır. YAPISAL her silme/ekleme sonrası çağrılmalı.
        ///
        /// DERS (indeksle tutulan seçim, liste değişince YALAN söyler):
        /// Seçim kaydı "Blocks listesinin 3. elemanı" gibi bir indeks tutuyor.
        /// Listeden 1. eleman silinince 3. eleman artık BAŞKA bir bloktur;
        /// denetçi hiçbir hata vermeden yanlış nesneyi düzenler. Sağ tıkla
        /// silme yollarının hiçbiri seçimi temizlemiyordu. İndeksleri
        /// kaydırarak düzeltmek mümkün ama hataya açık; seçimi bırakmak
        /// KANITLANABİLİR biçimde doğru ve kullanıcıya maliyeti bir tık.
        /// </summary>
        void DropSelection()
        {
            if (_selections.Count > 0) _selections.Clear();
        }

        void AfterChange()
        {
            _dirty = true;
            _revision++;
            _validationStale = true;
            _playbackStep = -1;
            StashState();
            Repaint();
        }

        // ---------------- dosya ----------------

        /// <summary>Kaydedilmemiş iş varsa sorar. Devam edilebilirse true.</summary>
        bool ConfirmDiscard()
        {
            if (!_dirty) return true;
            int choice = EditorUtility.DisplayDialogComplex(
                "Kaydedilmemiş değişiklikler",
                "Bu bölümde kaydedilmemiş değişiklikler var.",
                "Kaydet", "İptal", "Kaydetme");

            if (choice == 1) return false;
            if (choice == 0)
            {
                string path = _path ?? LevelEditorIO.AskSavePath(_data.Id);
                if (path == null) return false;
                SaveTo(path);
            }
            return true;
        }

        void LoadFrom(string path)
        {
            try
            {
                _data = LevelEditorIO.FromJson(System.IO.File.ReadAllText(path));
                _path = path;
                _selections.Clear();
                _undoStack.Clear(); _redoStack.Clear();
                _canvas.ResetView();
                AfterChange(); _dirty = false;
                LevelEditorIO.ClearRecovery();
                Say($"✓ {System.IO.Path.GetFileNameWithoutExtension(path)} açıldı " +
                    $"({_data.Board.Width}×{_data.Board.Height})");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Açılamadı", e.Message, "Tamam");
                Say("⚠ açılamadı: " + e.Message);
            }
        }

        void SaveTo(string path)
        {
            LevelEditorIO.Save(_data, path);
            _path = path; _dirty = false;
            LevelEditorIO.ClearRecovery();

            // Galeri ve doğrulama listesi bu dosyanın ESKİ hâlini önbellekte
            // tutuyor; kaydettikten sonra atılmalı, yoksa araç kendi yazdığı
            // değişikliği göstermez.
            LevelLibrary.Invalidate(path);
            Say($"✓ {System.IO.Path.GetFileName(path)} kaydedildi");
        }

        /// <summary>
        /// Bölüm açmayı KAREN SONUNA erteler.
        ///
        /// DERS (OnGUI'nin ortasında modal pencere açma): ConfirmDiscard bir
        /// dialog açar; dialog IMGUI'nin olay döngüsünü keser ve o karede
        /// başlatılmış layout grupları kapanmadan kalır — Unity bunu
        /// "EndLayoutGroup: BeginLayoutGroup must be called first" diye
        /// bağırır. Ayrıca yükleme listeyi ve seçimi değiştirir; aynı karede
        /// çizilmeye devam eden satırlar artık var olmayan veriye bakar.
        /// Çözüm: isteği kuyruğa al, kare bitince koştur.
        /// </summary>
        void RequestOpen(string path)
        {
            if (string.IsNullOrEmpty(path) || path == _path) return;
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                if (ConfirmDiscard()) LoadFrom(path);
            };
        }

        /// <summary>
        /// Bölümü onay alarak siler ve açıkta kalmamak için komşusuna geçer.
        /// Silinen bölüm editörde açıksa "kaydedilmemiş değişiklik" sorusu
        /// SORULMAZ — dosya artık yok, soru anlamsız olurdu.
        /// </summary>
        void DeleteLevel(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (!EditorUtility.DisplayDialog("Bölümü sil",
                    System.IO.Path.GetFileNameWithoutExtension(path) +
                    " kalıcı olarak silinsin mi?\n\nDosya ve katalog girdisi birlikte silinir.",
                    "Sil", "Vazgeç"))
                return;

            bool wasOpen = path == _path;
            string next = LevelLibrary.Neighbour(path, 1) ?? LevelLibrary.Neighbour(path, -1);
            if (next == path) next = null;

            LevelLibrary.Delete(path);

            if (wasOpen)
            {
                _dirty = false;
                if (next != null) LoadFrom(next);
                else
                {
                    _data = LevelEditorIO.NewLevel();
                    _path = null;
                    AfterChange();
                    _dirty = false;
                }
            }
            Say("Bölüm silindi: " + System.IO.Path.GetFileNameWithoutExtension(path));
        }

        /// <summary>
        /// Sıra değişince açık bölümün numarası DİSKTE güncellenmiş olabilir;
        /// bellekteki kopyayı yalnız o alanda eşitler.
        ///
        /// DERS (bellekteki kopya bayatlar): Sıra değiştirmek katalogla birlikte
        /// dosyaların `displayNumber` alanını da yeniliyor. Pencere ise bölümün
        /// KENDİ kopyasını tutuyor; eşitlenmezse bir sonraki kaydetme eski
        /// numarayı geri yazıp yapılan düzeltmeyi sessizce iptal ederdi.
        /// Dosyanın tamamını okumak yanlış olurdu — kaydedilmemiş düzenlemeler
        /// gider; değişen tek alanı almak yeterli.
        /// </summary>
        void SyncDisplayNumberFromDisk()
        {
            if (_path == null || !System.IO.File.Exists(_path)) return;
            try
            {
                var onDisk = LevelEditorIO.FromJson(System.IO.File.ReadAllText(_path));
                if (onDisk.DisplayNumber == _data.DisplayNumber) return;

                _data.DisplayNumber = onDisk.DisplayNumber;
                StashState();
                Repaint();
            }
            catch { /* dosya okunamadıysa numara eski kalsın — veri kaybından iyidir */ }
        }

        /// <summary>Listedeki komşu bölüme geçer (üst şeritteki Önceki/Sonraki).</summary>
        void GoToNeighbour(int step)
        {
            string target = LevelLibrary.Neighbour(_path, step);
            if (target == null) { Say("Setin sonundasın"); return; }
            RequestOpen(target);
        }

        void RunValidation()
        {
            _validationStale = false;
            _problemCells.Clear();

            if (_palette == null || _config == null) { _report = null; return; }

            _report = LevelValidationTool.ValidateData(_data, _palette, _config);
            CollectProblemCells();

            // Kaydedilmiş bir bölümü düzenliyorsak listedeki durumu da tazele —
            // galeriye geçince eski rozeti görmemek için.
            if (_path != null) LevelLibrary.Apply(LevelLibrary.Find(_path), _report);
        }

        /// <summary>Tuvalde kırmızı gösterilecek sorunlu hücreler (çakışma / taşma).</summary>
        void CollectProblemCells()
        {
            var seen = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < _data.Blocks.Count; i++)
            {
                var block = _data.Blocks[i];
                for (int x = block.X; x < block.X + block.W; x++)
                    for (int y = block.Y; y < block.Y + block.H; y++)
                    {
                        var cell = new Vector2Int(x, y);
                        if (!Playable(x, y)) _problemCells.Add(cell);
                        else if (seen.ContainsKey(cell)) _problemCells.Add(cell);
                        else seen[cell] = i;
                    }
            }

            foreach (var gate in _data.Gates)
            {
                if (!SideUtil.TryParse(gate.Side, out var side)) continue;
                bool horizontal = side == Side.North || side == Side.South;
                for (int j = 0; j < gate.Length; j++)
                {
                    int cx = horizontal ? gate.X + j : gate.X;
                    int cy = horizontal ? gate.Y : gate.Y + j;
                    if (!Playable(cx, cy)) _problemCells.Add(new Vector2Int(cx, cy));
                }
            }
        }

        void ResizeBoard(int width, int height)
        {
            var rows = new List<string>(height);
            for (int y = 0; y < height; y++)
            {
                string old = y < _data.Board.Rows.Count ? _data.Board.Rows[y] : "";
                var sb = new System.Text.StringBuilder(width);
                for (int x = 0; x < width; x++) sb.Append(x < old.Length ? old[x] : 'X');
                rows.Add(sb.ToString());
            }
            _data.Board.Rows = rows;
            _data.Board.Width = width;
            _data.Board.Height = height;
        }

        // ---------------- ortak yardımcılar ----------------

        /// <summary>
        /// Hücre oynanabilir mi?
        ///
        /// DERS (dış veriye asla güvenme): Satır uzunluğu Width'e EŞİT
        /// varsayılıyordu. Elle düzenlenmiş ya da yarım kalmış bir JSON'da
        /// kısa bir satır olursa bu ifade her karede IndexOutOfRange atar ve
        /// editör açılamaz hâle gelir. Uzunluk kontrolü, bozuk dosyayı
        /// çökme yerine "kapalı hücre" olarak göstermeyi sağlıyor —
        /// doğrulama da onu ayrıca uyarı olarak bildiriyor.
        /// </summary>
        bool Playable(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _data.Board.Width || y >= _data.Board.Height) return false;
            if (y >= _data.Board.Rows.Count) return false;

            string row = _data.Board.Rows[y];
            return x < row.Length && char.ToUpperInvariant(row[x]) == 'X';
        }

        Rect CurtainRect(ObstacleData curtain) => _canvas.RectFor(
            LevelEditorIO.GetInt(curtain, "x"), LevelEditorIO.GetInt(curtain, "y"),
            LevelEditorIO.GetInt(curtain, "w", 1), LevelEditorIO.GetInt(curtain, "h", 1));

        static RectInt RegionRect(Vector2Int a, Vector2Int b) => new RectInt(
            Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
            Mathf.Abs(a.x - b.x) + 1, Mathf.Abs(a.y - b.y) + 1);

        Color ColorOf(string id) =>
            BlockColorUtil.TryParse(id, out var color) ? ColorOf(color) : Color.magenta;

        Color ColorOf(BlockColor color)
        {
            var entry = _palette != null ? _palette.Get(color) : null;
            return entry != null ? entry.uiColor : Color.magenta;
        }

        /// <summary>Seçim kaydından blok verisine erişim (perde içeriği dahil).</summary>
        BlockData BlockOf(Selection selection)
        {
            if (selection.Kind == SelKind.Block)
                return selection.Index < _data.Blocks.Count ? _data.Blocks[selection.Index] : null;

            if (selection.Kind == SelKind.Content && selection.Index < _data.Obstacles.Count)
            {
                var contents = LevelEditorIO.GetContents(_data.Obstacles[selection.Index]);
                return selection.Sub < contents.Count ? contents[selection.Sub] : null;
            }
            return null;
        }

        Selection Primary => _selections.Count > 0 ? _selections[0] : Selection.None;

        /// <summary>
        /// Perde içeriği JSON'dan KOPYA olarak okunur; düzenlemenin kalıcı
        /// olması için perdeye geri yazılmalı.
        /// </summary>
        void CommitContentEdit(Selection selection, BlockData edited)
        {
            if (selection.Kind != SelKind.Content) return;
            if (selection.Index >= _data.Obstacles.Count) return;

            var curtain = _data.Obstacles[selection.Index];
            var contents = LevelEditorIO.GetContents(curtain);
            if (selection.Sub < contents.Count) contents[selection.Sub] = edited;
            LevelEditorIO.SetContents(curtain, contents);
        }

        // ---------------- küçük çizim yardımcıları (sekmelerin paylaştığı) ----------------

        /// <summary>Bölüm başlıklarını her sekmede aynı görünümde yazar.</summary>
        static void SectionHeader(string title, string hint = null) =>
            LevelEditorSkin.SectionHeader(title, hint);

        /// <summary>Durum noktası — galeri ve listelerde aynı dili konuşsun diye tek yerde.</summary>
        static void StatusDot(Rect rect, LevelLibrary.Status status)
        {
            var color = LevelLibrary.ColorFor(status);
            float size = Mathf.Min(rect.width, rect.height);
            var box = new Rect(rect.x, rect.center.y - size * 0.5f, size, size);
            LevelCanvasDrawer.Fill(box, color);
        }
    }
}
