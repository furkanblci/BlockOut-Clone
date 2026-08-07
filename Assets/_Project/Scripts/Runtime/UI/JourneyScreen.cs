using System.Collections.Generic;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Yolculuk: ışıklı dikey bir ray üzerinde kilometre taşı ödülleri ve bölge
    /// kartları.
    ///
    /// DERS (bu ekran bölüm SEÇMEZ): Önce buraya 50 bölümlük yılankavi bir yol
    /// yapmıştım — güzel görünüyordu ama oyunun kendi mantığına aykırıydı.
    /// Referans oyunda oyuncu istediği bölüme atlayamaz; ana ekrandaki tek
    /// düğme sıradaki seviyeyi açar. Yolculuk ekranı ilerlemenin ÖDÜL tarafını
    /// gösterir: "üç bölüm sonra sınırsız can". Serbest bölüm seçimi bir oyuncu
    /// özelliği değil bir GELİŞTİRİCİ ihtiyacıdır ve yeri DevMenu'dür.
    ///
    /// DERS (arka plan sakinleşmeli): Köy manzarasını menü zemini yapmak ana
    /// ekranda doğru; ama üstünde okunacak on satır varsa manzara gürültüye
    /// döner. Referans burada düz koyu bir zemine geçiyor. "Aynı oyundayım"
    /// hissi zeminin kendisinden değil renk paletinden geliyor.
    /// </summary>
    public sealed class JourneyScreen : MonoBehaviour
    {
        /// <summary>Kilometre taşı: seviye, ödül ikonu, ödülün kısa değeri.</summary>
        static readonly (int level, string icon, string value)[] Milestones =
        {
            (10, Art.Coin,   "50"),
            (15, Art.Heart,  "30d"),
            (25, Art.Rocket, "x1"),
            (45, Art.Coin,   "100"),
            (50, Art.Heart,  "1sa"),
            (60, Art.Clock,  "x1"),
            (80, Art.Coin,   "250"),
            (90, Art.Heart,  "1gün")
        };

        static readonly (string name, int from, int to)[] Regions =
        {
            ("Görev Hazırlığı", 1, 20),
            ("Yıldız Yolculuğu", 21, 40),
            ("Penguen Kovalamacası", 41, 70),
            ("Zafer Tırmanışı", 101, 150)
        };

        // Referans çözünürlükte (1080x1920) piksel ölçüleri.
        const float RowHeight = 240f;
        const float RegionHeight = 800f;
        const float TrackWidth = 34f;
        const float EdgePad = 130f;

        static readonly Color Indigo     = new Color(0.129f, 0.106f, 0.396f);
        static readonly Color Periwinkle = new Color(0.353f, 0.322f, 0.784f);
        static readonly Color TrackGlow  = new Color(0.243f, 0.616f, 0.976f);
        static readonly Color CheckGreen = new Color(0.216f, 0.812f, 0.243f);

        readonly List<(Image check, int level)> _checks = new List<(Image, int)>();
        readonly List<(TextMeshProUGUI label, int level)> _dimmable =
            new List<(TextMeshProUGUI, int)>();
        readonly List<(TextMeshProUGUI label, int level)> _regionState =
            new List<(TextMeshProUGUI, int)>();

        TextMeshProUGUI _progressLabel;
        ScrollRect _scroll;
        RectTransform _content;
        bool _built;
        bool _centeredOnce;

        public static RectTransform Build(Transform parent)
        {
            var root = MenuShell.Screen(parent, "JourneyScreen");
            var screen = root.gameObject.AddComponent<JourneyScreen>();

            // Manzarayı tamamen kapat: bu ekran sade zemin ister.
            UiKit.CreatePanel("Solid", root, Indigo);
            screen.BuildBackdrop(root);

            screen.BuildScrollArea(root);
            screen.BuildTrack();

            MenuShell.Header(root, "Yolculuk");
            screen._progressLabel = UiKit.CreateLabel("Progress", root, "", 30,
                new Color(1f, 1f, 1f, 0.8f));
            UiKit.Place(screen._progressLabel, 0.04f, 0.876f, 0.96f, 0.918f);

            screen.BuildJumpButtons(root);

            screen._built = true;
            return root;
        }

        /// <summary>
        /// Zemin dokusu: büyük yumuşak daireler ve serpiştirilmiş yıldızlar.
        ///
        /// DERS (düz renk zemin BOŞ okunur): Bu ekranın zemini düz laciverttdi.
        /// Manzaradan iyiydi (yazı okunuyordu) ama ekran "yüklenmemiş" gibi
        /// duruyordu. Referansta da düz değil: çok düşük kontrastlı şekiller
        /// var. Kritik olan KONTRASTIN DÜŞÜK olması — %4-7 opaklıkta bir daire
        /// gözü hiç rahatsız etmiyor ama yüzeyin var olduğunu söylüyor.
        ///
        /// Şekiller sabit bir düzende: rastgele olsaydı her açılışta farklı
        /// dururdu ve "bozuk mu" hissi verirdi.
        /// </summary>
        void BuildBackdrop(Transform root)
        {
            var layer = UiKit.CreateRect("Backdrop", root);
            UiKit.Place(layer, 0f, 0f, 1f, 1f);

            // (x, y, çap, opaklık) — elle dizildi, ekranı dengeli dolduruyor.
            var blobs = new[]
            {
                (0.14f, 0.86f, 0.42f, 0.055f), (0.82f, 0.72f, 0.34f, 0.045f),
                (0.30f, 0.52f, 0.50f, 0.040f), (0.88f, 0.34f, 0.44f, 0.050f),
                (0.16f, 0.16f, 0.38f, 0.045f), (0.62f, 0.06f, 0.30f, 0.038f),
            };
            foreach (var (x, y, d, a) in blobs)
            {
                var blob = UiKit.CreateRoundedPanel("Blob", layer,
                    new Color(0.62f, 0.55f, 1f, a));
                blob.pixelsPerUnitMultiplier = 0.05f;
                blob.raycastTarget = false;
                UiKit.Place(blob, x - d * 0.5f, y - d * 0.35f, x + d * 0.5f, y + d * 0.35f);
            }

            var star = UiSkin.Get(Art.Star);
            var stars = new[]
            {
                (0.09f, 0.62f, 0.055f), (0.91f, 0.55f, 0.042f), (0.20f, 0.30f, 0.048f),
                (0.78f, 0.90f, 0.045f), (0.50f, 0.94f, 0.035f), (0.86f, 0.14f, 0.050f),
                (0.12f, 0.44f, 0.038f), (0.70f, 0.24f, 0.042f),
            };
            foreach (var (x, y, d) in stars)
            {
                var glyph = UiKit.CreateIcon("Star", layer, star,
                    new Color(1f, 0.96f, 0.78f, 0.10f));
                UiKit.Place(glyph, x - d * 0.5f, y - d * 0.28f, x + d * 0.5f, y + d * 0.28f);
            }
        }

        void BuildScrollArea(Transform root)
        {
            var viewport = UiKit.CreateRect("Viewport", root);
            UiKit.Place(viewport, 0f, 0f, 1f, 0.870f);
            viewport.gameObject.AddComponent<RectMask2D>();

            _content = UiKit.CreateRect("Track", viewport);
            _content.anchorMin = new Vector2(0f, 0f);
            _content.anchorMax = new Vector2(1f, 0f);
            _content.pivot = new Vector2(0.5f, 0f);
            _content.sizeDelta = new Vector2(0f, ContentHeight());
            _content.anchoredPosition = Vector2.zero;

            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.content = _content;
            _scroll.viewport = viewport;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.elasticity = 0.08f;
            _scroll.scrollSensitivity = 45f;
            _scroll.decelerationRate = 0.12f;
        }

        /// <summary>
        /// Ray boyunca sıralanacak girdiler, ALTTAN ÜSTE seviye sırasında.
        /// Bölge kartı, o bölgenin ilk kilometre taşından önce gelir.
        /// </summary>
        static List<(bool isRegion, int index)> Entries()
        {
            var entries = new List<(bool, int)>();
            int milestone = 0;

            for (int r = 0; r < Regions.Length; r++)
            {
                entries.Add((true, r));
                while (milestone < Milestones.Length &&
                       Milestones[milestone].level <= Regions[r].to)
                {
                    entries.Add((false, milestone));
                    milestone++;
                }
            }
            while (milestone < Milestones.Length) { entries.Add((false, milestone)); milestone++; }
            return entries;
        }

        static float ContentHeight()
        {
            float height = EdgePad * 2f;
            foreach (var (isRegion, _) in Entries())
                height += isRegion ? RegionHeight : RowHeight;
            return height;
        }

        void BuildTrack()
        {
            float total = ContentHeight();

            // Işıklı ray iki katman: geniş soluk hâle + ince parlak çekirdek.
            // Tek düz çubuk "yol" hissi vermiyor; hâle onu ışıklı bir tüpe
            // çevirip koyu zeminden ayırıyor.
            var halo = UiKit.CreateRoundedPanel("TrackGlow", _content,
                new Color(TrackGlow.r, TrackGlow.g, TrackGlow.b, 0.26f));
            Anchor(halo.rectTransform, 0f, total * 0.5f, TrackWidth * 2.6f, total);
            halo.raycastTarget = false;

            var core = UiKit.CreateRoundedPanel("TrackCore", _content, TrackGlow);
            Anchor(core.rectTransform, 0f, total * 0.5f, TrackWidth, total);
            core.raycastTarget = false;

            float y = EdgePad;
            foreach (var (isRegion, index) in Entries())
            {
                if (isRegion)
                {
                    BuildRegion(Regions[index], y + RegionHeight * 0.5f);
                    y += RegionHeight;
                }
                else
                {
                    BuildMilestone(Milestones[index], y + RowHeight * 0.5f);
                    y += RowHeight;
                }
            }
        }

        /// <summary>Kilometre taşı satırı: solda seviye kapsülü, sağda ödül.</summary>
        void BuildMilestone((int level, string icon, string value) milestone, float y)
        {
            var pill = UiKit.CreateSlicedPanel($"Level_{milestone.level}", _content,
                UiSkin.Get(Art.PanelDark), new Color(0.463f, 0.416f, 0.878f));
            Anchor(pill.rectTransform, -272f, y, 452f, 172f);

            var caption = UiKit.CreateLabel("Caption", pill.transform, "Seviye", 30,
                new Color(0.84f, 0.86f, 1f));
            UiKit.Place(caption, 0.06f, 0.50f, 0.94f, 0.92f);

            var number = UiKit.CreateTitle("Number", pill.transform,
                milestone.level.ToString(), 54, UiKit.Ink, new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(number, 0.06f, 0.06f, 0.94f, 0.54f);
            _dimmable.Add((number, milestone.level));

            // Ödül: ikon + altında kısa değeri.
            var icon = UiKit.CreateIcon($"Reward_{milestone.level}", _content,
                UiSkin.Get(milestone.icon));
            Anchor(icon.rectTransform, 238f, y + 26f, 150f, 150f);

            var value = UiKit.CreateTitle($"Value_{milestone.level}", _content,
                milestone.value, 36, UiKit.Ink, new Color(0.12f, 0.09f, 0.30f));
            Anchor(value.rectTransform, 238f, y - 68f, 240f, 60f);
            _dimmable.Add((value, milestone.level));

            // Onay işareti yalnız kazanılmış ödüllerde görünür.
            // Onay tiki artık kendi görseli; daha önce yeşile boyanmış yıldız
            // kullanılıyordu ve "bir yıldız daha mı kazandım" diye okunuyordu.
            var checkSprite = UiSkin.Get(Art.Check);
            var check = checkSprite != null
                ? UiKit.CreateIcon($"Check_{milestone.level}", _content, checkSprite)
                : UiKit.CreateIcon($"Check_{milestone.level}", _content,
                    UiSkin.Get(Art.Star), CheckGreen);
            Anchor(check.rectTransform, 416f, y + 62f, 96f, 96f);
            _checks.Add((check, milestone.level));
        }

        /// <summary>Bölge kartı: büyük daire, adı ve seviye aralığı.</summary>
        void BuildRegion((string name, int from, int to) region, float y)
        {
            // Ad, dairenin ÜSTÜNDE durur. Daire 560 yüksekliğinde ve merkezi
            // y olduğu için tepesi y+280'de; başlık 350'ye konursa araya 70
            // piksel boşluk kalır. Önce 300 verilmişti ve yazı dairenin üstüne
            // biniyordu.
            var title = UiKit.CreateTitle($"RegionName_{region.from}", _content, region.name, 48,
                UiKit.Ink, new Color(0.12f, 0.09f, 0.30f));
            Anchor(title.rectTransform, 0f, y + 350f, 980f, 90f);
            _dimmable.Add((title, region.from));

            // Bölge dairesi. Görsel DAİRESEL çizildiği için maskeye gerek yok:
            // kare bir görseli daireye kırpmak ya çalışan bir maske ya da
            // dairesel çizilmiş bir görsel ister; ikincisi hem daha ucuz hem
            // kenarları yumuşak.
            int regionIndex = System.Array.FindIndex(Regions, r => r.from == region.from) + 1;
            var art = UiSkin.Get(Art.Region(regionIndex));

            var disc = art != null
                ? UiKit.CreateIcon($"Region_{region.from}", _content, art)
                : UiKit.CreateSlicedPanel($"Region_{region.from}", _content,
                    UiSkin.Get(Art.PanelDark), Periwinkle);
            Anchor(disc.rectTransform, 0f, y, 600f, 600f);

            var tag = UiKit.CreateSlicedPanel("Tag", _content, UiSkin.Get(Art.PanelDark));
            Anchor(tag.rectTransform, 0f, y + 265f, 300f, 84f);

            var range = UiKit.CreateTitle("Range", tag.transform, $"sv {region.from} - {region.to}",
                32, UiKit.Ink, new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(range, 0.04f, 0.06f, 0.96f, 0.94f);

            // Rozet dairenin İÇİNDE değil ALTINDA: görselin ortasında
            // karakterler var, rozet tam üstlerine biniyordu.
            var state = UiKit.CreateSlicedPanel("State", _content,
                UiSkin.Get(Art.PanelCard), new Color(0.176f, 0.800f, 0.047f));
            Anchor(state.rectTransform, 0f, y - 330f, 280f, 90f);

            var stateLabel = UiKit.CreateTitle("StateText", state.transform, "", 28,
                UiKit.Ink, new Color(0.06f, 0.28f, 0.04f));
            UiKit.Place(stateLabel, 0.04f, 0.10f, 0.96f, 0.92f);
            _regionState.Add((stateLabel, region.from));
        }

        /// <summary>Rayın iki ucuna atlayan düğmeler — referanstaki "Üst"/"Alt".</summary>
        void BuildJumpButtons(Transform root)
        {
            // DERS (yardımcı düğme ana yolun ÜSTÜNDE durmaz): Bu ikisi
            // ekranın ortasındaydı ve rayın üstündeki rozetlerle çakışıyordu.
            // İşlevleri ikincil — sağ kenara, içeriğin dışına alındılar.
            var up = UiKit.CreateTintedButton("Up", root, UiSkin.Get(Art.PanelCard),
                Periwinkle, "Üst", 24, UiKit.Ink);
            UiKit.Place(up, 0.795f, 0.800f, 0.965f, 0.852f);
            up.onClick.AddListener(() =>
            { if (_scroll != null) _scroll.verticalNormalizedPosition = 1f; });

            var down = UiKit.CreateTintedButton("Down", root, UiSkin.Get(Art.PanelCard),
                Periwinkle, "Alt", 24, UiKit.Ink);
            UiKit.Place(down, 0.795f, 0.022f, 0.965f, 0.074f);
            down.onClick.AddListener(() =>
            { if (_scroll != null) _scroll.verticalNormalizedPosition = 0f; });
        }

        /// <summary>Kaydırma içeriğinde MUTLAK yerleştirme (oran değil piksel).</summary>
        static void Anchor(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
        }

        void OnEnable()
        {
            Refresh();
            _centeredOnce = false;
        }

        void LateUpdate()
        {
            if (_centeredOnce || !_built || _scroll == null) return;
            _centeredOnce = true;
            CenterOnCurrent();
        }

        /// <summary>Sıradaki ödülü ekranın ortasına getirir.</summary>
        void CenterOnCurrent()
        {
            if (!MetaServices.Ready) return;

            int reached = MetaServices.Progress.HighestUnlockedIndex + 1;

            float y = EdgePad;
            float target = EdgePad;
            foreach (var (isRegion, index) in Entries())
            {
                if (!isRegion && Milestones[index].level > reached) { target = y; break; }
                y += isRegion ? RegionHeight : RowHeight;
                target = y;
            }

            float viewport = _scroll.viewport.rect.height;
            float span = Mathf.Max(1f, _content.rect.height - viewport);
            _scroll.verticalNormalizedPosition =
                Mathf.Clamp01((target - viewport * 0.45f) / span);
        }

        public void Refresh()
        {
            if (!_built || !MetaServices.Ready) return;

            int reached = MetaServices.Progress.HighestUnlockedIndex + 1;

            int next = int.MaxValue;
            foreach (var (level, _, _) in Milestones)
                if (level > reached && level < next) next = level;

            _progressLabel.text = next == int.MaxValue
                ? $"Seviye {reached} · tüm ödüller alındı"
                : $"Seviye {reached} · sonraki ödüle {next - reached} bölüm";

            foreach (var (check, level) in _checks)
                check.enabled = reached >= level;

            foreach (var (label, level) in _dimmable)
                label.color = reached >= level ? UiKit.Ink : new Color(1f, 1f, 1f, 0.5f);

            foreach (var (label, level) in _regionState)
                label.text = reached >= level ? "Açık" : "Kilitli";
        }
    }
}
