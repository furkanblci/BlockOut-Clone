using System.Collections.Generic;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Yolculuk — referans oyundan ölçülerek yeniden kuruldu.
    ///
    /// Referans kareler: `Block Out! menus,powerups,vs.mp4`, 24-42. saniyeler.
    /// Renkler ve yükseklikler o karelerden piksel örnekleyerek alındı.
    ///
    /// EKRANIN YAPISI:
    ///   • Üstte mavi-mor degrade bir başlık bandı, içinde "Yolculuk".
    ///   • Zemin DÜZ ve KOYU lacivert (#171C4E) — ölçüldü, tahmin değil.
    ///   • Ortada dikey, parlak mavi ışıklı bir BORU; bölme çizgileri var.
    ///   • Kilometre taşları: TEK bir geniş kapsül; solda "Seviye / N",
    ///     sağda ödül. Boru kapsülün ÜSTÜNDEN geçer. Alınmışsa sağ üst köşede
    ///     yeşil tik.
    ///   • Bölgeler: adı üstte, altında tam DAİRE bir görsel, üst kenarına
    ///     binen "sv 1 - 20" etiketi ve içine binen yeşil düğme.
    ///   • Borunun tepesinde "Top", dibinde "Bottom" düğmeleri — kaydırmaz, sabit.
    ///
    /// DERS (bu ekran bölüm SEÇMEZ): Önce buraya 50 bölümlük yılankavi bir yol
    /// yapılmıştı — güzel görünüyordu ama oyunun mantığına aykırıydı. Referans
    /// oyunda oyuncu istediği bölüme atlayamaz; Yolculuk ilerlemenin ÖDÜL
    /// tarafını gösterir. Serbest bölüm seçimi bir oyuncu özelliği değil bir
    /// GELİŞTİRİCİ ihtiyacıdır ve yeri DevMenu'dür.
    ///
    /// DERS (kendi eklediğimiz "iyileştirme" birebirliği bozuyordu): Bu ekranda
    /// bir ilerleme çubuğu ve "sonraki ödüle 4 bölüm" satırı vardı; iyi niyetli
    /// ve tek başına doğru bir fikirdi. Referansta ikisi de YOK ve ekranın
    /// tamamı bu yüzden farklı okunuyordu. Hedef birebir benzerlikse, iyi bir
    /// fikir bile fazlalıktır — çubuk kaldırıldı, yerini borudaki mevcut seviye
    /// işareti aldı (referansta olan da bu).
    /// </summary>
    public sealed class JourneyScreen : MonoBehaviour
    {
        // ---- Referanstan örneklenen palet ---------------------------------

        static readonly Color Body        = new Color(0.090f, 0.110f, 0.306f);
        static readonly Color HeaderTop   = new Color(0.275f, 0.216f, 0.886f);
        static readonly Color HeaderLow   = new Color(0.216f, 0.173f, 0.698f);
        static readonly Color Capsule     = new Color(0.298f, 0.231f, 0.886f);
        static readonly Color CapsuleRim  = new Color(0.129f, 0.094f, 0.478f);
        static readonly Color Tube        = new Color(0.102f, 0.663f, 0.969f);
        static readonly Color TubeDark    = new Color(0.071f, 0.067f, 0.216f);
        static readonly Color LockWash    = new Color(0.647f, 0.663f, 0.937f, 0.78f);
        static readonly Color LockTag     = new Color(0.145f, 0.157f, 0.310f);
        static readonly Color TubeSeam    = new Color(0.055f, 0.400f, 0.706f);
        static readonly Color JumpFace    = new Color(0.298f, 0.318f, 0.533f);
        static readonly Color CheckGreen  = new Color(0.212f, 0.776f, 0.106f);
        static readonly Color ActionGreen = new Color(0.176f, 0.800f, 0.047f);
        static readonly Color RangeTag    = new Color(0.137f, 0.165f, 0.369f);
        static readonly Color Caption     = new Color(0.729f, 0.741f, 0.949f);
        static readonly Color Outline     = new Color(0.086f, 0.075f, 0.290f);
        static readonly Color Locked      = new Color(0.35f, 0.35f, 0.45f);

        // ---- Referanstan ölçülen boyutlar (kanvas birimi) ------------------

        const float HeaderH   = 240f;
        const float RowHeight = 268f;   // kilometre taşları arası
        const float PillH     = 175f;
        const float PillX0    = 0.050f;
        const float PillX1    = 0.950f;
        const float RegionH   = 1010f;
        const float DiscSize  = 812f;
        const float TubeW     = 32f;
        const float EdgePad   = 150f;

        /// <summary>
        /// Kilometre taşları — ONUNUN DA referans karelerinden okundu
        /// (24., 31., 40., 41., 42. saniyeler). "d" dakika, "s" saat.
        /// Aradaki 55/65/70 gibi seviyelerde de ödül olabilir; videoda o
        /// aralıklar hiç ekrana gelmedi, uydurulmadı.
        /// </summary>
        static readonly (int level, string icon, string value)[] Milestones =
        {
            (10, "coin_pile_1", "50"),
            (15, Art.Infinite,  "30d"),
            (25, Art.Infinite,  "1s"),
            (30, Art.Clock,     "x1"),
            (45, Art.Rocket,    "x1"),
            (50, Art.Infinite,  "30d"),
            (60, Art.Clock,     "x1"),
            (75, Art.Rocket,    "x1"),
            (80, Art.Clock,     "x1"),
            (90, Art.Infinite,  "1s")
        };

        /// <summary>
        /// Beş bölge. "Buz Kurtarma" (71-100) ilk kuruluşta ATLANMIŞTI; videoda
        /// yalnız kilitli hâliyle göründüğü için gözden kaçmış.
        ///
        /// Görsel indeksi açıkça yazılıyor: sıra numarasından türetmek,
        /// araya bir bölge eklendiği anda bütün görselleri kaydırır.
        /// `region_5` henüz üretilmedi (buz temalı); gelene kadar o çember
        /// düz panele düşüyor — nasılsa kilitli görünüyor.
        /// </summary>
        static readonly (string name, int from, int to, int art)[] Regions =
        {
            ("Mission Prep", 1, 20, 1),
            ("Star Voyage", 21, 40, 2),
            ("Penguin Chase", 41, 70, 3),
            ("Ice Rescue", 71, 100, 5),
            ("Victory Climb", 101, 150, 4)
        };

        readonly List<(Image check, int level)> _checks = new List<(Image, int)>();

        ScrollRect _scroll;
        RectTransform _content, _marker, _tubeLive, _tubeGlow;
        TextMeshProUGUI _markerLabel;
        bool _built, _centeredOnce;

        // ====================================================================

        public static RectTransform Build(Transform parent)
        {
            var root = UiKit.CreateRect("JourneyScreen", parent);
            UiKit.Place(root, 0f, 0f, 1f, 1f);
            var screen = root.gameObject.AddComponent<JourneyScreen>();

            // Zemin ekranın DİBİNE kadar iner: kabuk ekranları sekme çubuğunun
            // üstünde bitiriyor ve aradaki şeritten ana ekranın manzarası
            // sızıyor. (Mağazada da aynı düzeltme yapıldı.)
            var backdrop = UiKit.CreatePanel("Body", root, Body);
            backdrop.rectTransform.offsetMin = new Vector2(0f, -210f);

            screen.BuildStars(root);
            screen.BuildScrollArea(root);
            screen.BuildTrack();
            screen.BuildHeader(root);
            screen.BuildJumpButtons(root);

            screen._built = true;
            return root;
        }

        /// <summary>
        /// Zemindeki soluk yıldızlar.
        ///
        /// DERS (zemin dokusu KISILMALI, kaldırılmamalı): Eski hâlde büyük
        /// yumuşak daireler ve %10 opaklıkta yıldızlar vardı; referansın
        /// yanına konunca ekran "sisli" duruyordu. Referansta da yıldız var
        /// ama neredeyse görünmez — zemin renginden yalnız bir tık açık.
        /// Doğru cevap dokuyu silmek değil, kontrastı düşürmekti.
        /// </summary>
        void BuildStars(Transform root)
        {
            var layer = UiKit.CreateRect("Stars", root);
            UiKit.Place(layer, 0f, 0f, 1f, 1f);

            var star = UiSkin.Get(Art.Star);
            if (star == null) return;

            var spots = new[]
            {
                (0.10f, 0.62f, 0.070f), (0.90f, 0.55f, 0.055f), (0.16f, 0.31f, 0.060f),
                (0.84f, 0.79f, 0.058f), (0.12f, 0.13f, 0.050f), (0.88f, 0.22f, 0.062f),
            };
            foreach (var (x, y, d) in spots)
            {
                var glyph = UiKit.CreateIcon("Star", layer, star,
                    new Color(0.55f, 0.60f, 1f, 0.055f));
                UiKit.Place(glyph, x - d * 0.5f, y - d * 0.28f, x + d * 0.5f, y + d * 0.28f);
            }
        }

        void BuildHeader(Transform root)
        {
            // Çentik şeridi: bandın boyası güvenli alanın ÜSTÜNE taşar, yoksa
            // çentikli telefonda o şeritte ana ekranın manzarası görünüyor
            // (bkz. MenuPage.Header'daki ders — 6. APK bulgusu).
            var bleed = UiKit.CreateRect("HeaderBleed", root);
            bleed.anchorMin = new Vector2(0f, 1f);
            bleed.anchorMax = new Vector2(1f, 1f);
            bleed.pivot = new Vector2(0.5f, 0f);
            bleed.sizeDelta = new Vector2(0f, 320f);
            bleed.anchoredPosition = Vector2.zero;
            var bleedImage = bleed.gameObject.AddComponent<Image>();
            bleedImage.color = HeaderTop;
            bleedImage.raycastTarget = false;

            // Başlık bandı en son kurulur: içerik onun ALTINDAN kayar.
            var band = UiKit.CreateRect("Header", root);
            band.anchorMin = new Vector2(0f, 1f);
            band.anchorMax = new Vector2(1f, 1f);
            band.pivot = new Vector2(0.5f, 1f);
            band.sizeDelta = new Vector2(0f, HeaderH);
            band.anchoredPosition = Vector2.zero;

            var fill = band.gameObject.AddComponent<Image>();
            fill.color = HeaderTop;

            // Alta doğru koyulaşma: düz renk bant referansta yok.
            var fade = UiKit.CreateRect("Fade", band);
            var fadeImage = fade.gameObject.AddComponent<Image>();
            fadeImage.sprite = MenuSprites.FadeDown;
            fadeImage.type = Image.Type.Sliced;
            fadeImage.color = HeaderLow;
            fadeImage.raycastTarget = false;
            UiKit.Place(fade, 0f, 0f, 1f, 1f);

            var title = UiKit.CreateTitle("Title", band, "Journey", 62,
                new Color(1f, 0.99f, 0.96f), new Color(0.45f, 0.42f, 0.92f));
            UiKit.Place(title, 0.05f, 0.30f, 0.95f, 0.86f);
        }

        void BuildScrollArea(Transform root)
        {
            var viewport = UiKit.CreateRect("Viewport", root);
            UiKit.Place(viewport, 0f, 0f, 1f, 1f);
            viewport.offsetMax = new Vector2(0f, -HeaderH);
            viewport.offsetMin = new Vector2(0f, -210f);
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
        /// Bölge çemberi, o bölgenin ilk kilometre taşından önce gelir.
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
                height += isRegion ? RegionH : RowHeight;
            return height;
        }

        /// <summary>
        /// Rayı ve üstündeki her şeyi kurar.
        ///
        /// DERS (kardeş sırası ÇİZİM sırasıdır, burada üç katman var): Referansta
        /// boru kilometre kapsüllerinin ÜSTÜNDEN, bölge çemberlerinin ALTINDAN
        /// geçiyor. Tek geçişte kurulursa bu sıra tutmaz. Bu yüzden önce bütün
        /// kapsüller, sonra boru, en son bölgeler kuruluyor.
        /// </summary>
        void BuildTrack()
        {
            float total = ContentHeight();

            // 1) Kilometre taşları
            float y = EdgePad;
            var positions = new List<(bool isRegion, int index, float y)>();
            foreach (var (isRegion, index) in Entries())
            {
                float height = isRegion ? RegionH : RowHeight;
                positions.Add((isRegion, index, y + height * 0.5f));
                y += height;
            }

            foreach (var (isRegion, index, cy) in positions)
                if (!isRegion) BuildMilestone(Milestones[index], cy);

            // 2) Boru
            BuildTube(total);

            // 3) Bölgeler
            foreach (var (isRegion, index, cy) in positions)
                if (isRegion) BuildRegion(Regions[index], cy);

            // 4) Oyuncunun bulunduğu seviye işareti — borunun üstünde.
            BuildMarker();
        }

        void BuildTube(float total)
        {
            // BORU BİR İLERLEME GÖSTERGESİDİR.
            //
            // DERS (referansı sonuna kadar izle): İlk kuruluşta boru baştan
            // sona parlak maviydi ve bu "doğru" görünüyordu — çünkü elimdeki
            // kareler hep oyuncunun ULAŞTIĞI seviyelerdeydi. Videoda yukarı
            // kaydırılan kareye bakınca boru orada KOYU çıktı: oyuncunun
            // seviyesine kadar ışıklı, ötesinde sönük. Yani boru dekor değil,
            // ekranın tek ilerleme göstergesi. (Bizim eklediğimiz ayrı ilerleme
            // çubuğu tam da bu yüzden fazlalıktı.)
            var dark = UiKit.CreateRoundedPanel("TubeDark", _content, TubeDark);
            Anchor(dark.rectTransform, 0f, total * 0.5f, TubeW, total);
            dark.raycastTarget = false;

            var halo = UiKit.CreateRoundedPanel("TubeGlow", _content,
                new Color(Tube.r, Tube.g, Tube.b, 0.13f));
            halo.raycastTarget = false;
            _tubeGlow = halo.rectTransform;
            AnchorFromBottom(_tubeGlow, TubeW * 1.5f, total);

            var core = UiKit.CreateRoundedPanel("TubeLive", _content, Tube);
            core.raycastTarget = false;
            _tubeLive = core.rectTransform;
            AnchorFromBottom(_tubeLive, TubeW, total);

            // Sol tarafta ince bir ışık şeridi: boruyu düz bir çubuk olmaktan
            // çıkarıp silindir yapan tek şey bu.
            var shine = UiKit.CreatePanel("TubeShine", core.transform,
                new Color(1f, 1f, 1f, 0.30f));
            shine.raycastTarget = false;
            UiKit.Place(shine, 0.20f, 0f, 0.42f, 1f);

            // Bölme çizgileri: referansta boru düz değil, ekli parçalardan
            // oluşuyor ve bu ekler ilerleme hissini taşıyor.
            for (float seam = EdgePad; seam < total; seam += RowHeight)
            {
                var line = UiKit.CreatePanel("Seam", _content, TubeSeam);
                line.raycastTarget = false;
                Anchor(line.rectTransform, 0f, seam, TubeW, 5f);
            }
        }

        /// <summary>Kilometre taşı: tek kapsül, solda seviye, sağda ödül.</summary>
        void BuildMilestone((int level, string icon, string value) milestone, float y)
        {
            var row = UiKit.CreateRect($"Milestone_{milestone.level}", _content);
            AnchorNormalizedX(row, PillX0, PillX1, y, PillH);

            var rim = MenuCapsule("Rim", row, CapsuleRim);
            UiKit.Place(rim, 0f, 0f, 1f, 1f);

            var face = MenuCapsule("Face", row, Capsule);
            UiKit.Place(face, 0.010f, 0.06f, 0.990f, 0.94f);

            var caption = UiKit.CreateLabel("Caption", face.transform, "Level", 38, Caption);
            UiKit.Place(caption, 0.04f, 0.52f, 0.52f, 0.92f);

            var number = UiKit.CreateTitle("Number", face.transform,
                milestone.level.ToString(), 66, UiKit.Ink, Outline);
            UiKit.Place(number, 0.04f, 0.08f, 0.52f, 0.56f);

            var icon = UiKit.CreateIcon("Reward", face.transform, UiSkin.Get(milestone.icon));
            UiKit.Place(icon, 0.655f, 0.26f, 0.875f, 0.97f);

            var value = UiKit.CreateTitle("Value", face.transform, milestone.value, 38,
                UiKit.Ink, Outline);
            UiKit.Place(value, 0.60f, 0.04f, 0.92f, 0.30f);

            // Tik kapsülün sağ ÜST köşesinden taşar; kapsülün değil satırın
            // çocuğu, yoksa kapsül onu kırpar.
            var checkSprite = UiSkin.Get(Art.Check);
            var check = checkSprite != null
                ? UiKit.CreateIcon("Check", row, checkSprite)
                : UiKit.CreateIcon("Check", row, UiSkin.Get(Art.Star), CheckGreen);
            UiKit.Place(check, 0.885f, 0.66f, 1.085f, 1.34f);
            _checks.Add((check, milestone.level));
        }

        /// <summary>Bir bölge çemberinin durum değiştiren parçaları.</summary>
        sealed class RegionView
        {
            public int From;
            public Image Wash, Lock, Action, Tick;
            public RectTransform LockTag, ActionRoot;
            public TextMeshProUGUI ActionLabel, LockLabel;
        }

        readonly List<RegionView> _regions = new List<RegionView>();

        /// <summary>Bölge: ad, daire, aralık etiketi, eylem düğmesi / kilit.</summary>
        void BuildRegion((string name, int from, int to, int art) region, float y)
        {
            var view = new RegionView { From = region.from };

            var title = UiKit.CreateTitle($"RegionName_{region.from}", _content, region.name, 62,
                UiKit.Ink, new Color(0.42f, 0.38f, 0.92f));
            Anchor(title.rectTransform, 0f, y + DiscSize * 0.5f + 68f, 1040f, 100f);

            var art = UiSkin.Get(Art.Region(region.art));

            // Görsel DAİRESEL çizildiği için maskeye gerek yok. Ama alan KARE
            // olmalı: eski hâlde 600x600 verilmişti ama preserveAspect olmayan
            // bir panele düşünce elips çıkıyordu.
            var disc = art != null
                ? UiKit.CreateIcon($"Region_{region.from}", _content, art)
                : UiKit.CreateRoundedPanel($"Region_{region.from}", _content, Capsule);
            Anchor(disc.rectTransform, 0f, y, DiscSize, DiscSize);

            // Kilitli perde: görselin üstüne açık mavi-gri bir daire.
            //
            // DERS (BOYAMA doygunluğu düşüremez): Referansta kilitli bölge
            // GRİ TONLAMA. Image.color çarpım yaptığı için renkli bir görseli
            // griye boyayamazsın — yalnız karartırsın. Doygunluğu düşürmek bir
            // materyal/gölgelendirici işi. Aynı okumayı veren ucuz yol,
            // görselin üstüne yarı saydam açık gri-mavi bir daire koymak:
            // renkler o rengin içinde erir ve sonuç referanstaki soluk hâle
            // çok yaklaşır — üstelik tek bir ek çizim.
            var wash = UiKit.CreateRect($"Wash_{region.from}", _content);
            view.Wash = wash.gameObject.AddComponent<Image>();
            view.Wash.sprite = GameKit.UI.UiSprites.Circle;
            view.Wash.color = LockWash;
            view.Wash.raycastTarget = false;
            Anchor(wash, 0f, y, DiscSize, DiscSize);

            var ring = UiKit.CreateRect($"Ring_{region.from}", _content);
            var ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = MenuSprites.Ring;
            ringImage.color = new Color(0.35f, 0.72f, 1f, 0.95f);
            ringImage.raycastTarget = false;
            Anchor(ring, 0f, y, DiscSize + 14f, DiscSize + 14f);

            // Aralık etiketi dairenin ÜST kenarına biner.
            var tag = MenuCapsule("Tag", _content, RangeTag);
            Anchor(tag.rectTransform, 0f, y + DiscSize * 0.5f - 52f, 340f, 84f);

            var range = UiKit.CreateTitle("Range", tag.transform, $"lv {region.from} - {region.to}",
                32, UiKit.Ink, Outline);
            UiKit.Place(range, 0.05f, 0.08f, 0.95f, 0.92f);

            // Kilit: altın asma kilit + altında "Seviye N" etiketi.
            var padlock = UiKit.CreateRect($"Lock_{region.from}", _content);
            view.Lock = padlock.gameObject.AddComponent<Image>();
            view.Lock.sprite = UiSkin.Get(Art.Lock);
            view.Lock.preserveAspect = true;
            view.Lock.raycastTarget = false;
            // Referanstaki kilit ALTIN; elimizdeki `icon_lock` gümüş.
            // Boyama çarpım olduğu için gümüşü altına çeviremeyiz, ancak
            // tonunu sıcağa kaydırabiliriz — gerçek altın kilit görseli
            // istendi (docs/art-prompts.md).
            view.Lock.color = new Color(1f, 0.82f, 0.42f);
            Anchor(padlock, 0f, y + 64f, 230f, 230f);

            view.LockTag = MenuCapsule("LockTag", _content, LockTag).rectTransform;
            Anchor(view.LockTag, 0f, y - 132f, 380f, 92f);

            view.LockLabel = UiKit.CreateTitle("LockText", view.LockTag, "", 36,
                UiKit.Ink, Outline);
            UiKit.Place(view.LockLabel, 0.05f, 0.08f, 0.95f, 0.92f);

            // Eylem düğmesi dairenin İÇİNE, alt tarafına biner.
            var action = UiKit.CreateRect($"Action_{region.from}", _content);
            Anchor(action, 0f, y - DiscSize * 0.5f + 168f, 280f, 96f);

            var shadow = MenuCapsule("Shadow", action, Darken(ActionGreen, 0.58f));
            UiKit.Place(shadow, 0f, 0f, 1f, 1f);

            view.Action = MenuCapsule("Face", action, ActionGreen);
            UiKit.Place(view.Action, 0.02f, 0.16f, 0.98f, 1f);

            view.ActionLabel = UiKit.CreateTitle("Label", view.Action.transform, "", 34,
                UiKit.Ink, new Color(0.05f, 0.26f, 0.03f));
            UiKit.Place(view.ActionLabel, 0.06f, 0.06f, 0.94f, 0.94f);

            // Tamamlanmış bölgede yazı yerine tik duruyor (referans: "Görev
            // Hazırlığı" karesi).
            view.Tick = UiKit.CreateIcon("Tick", view.Action.transform, UiSkin.Get(Art.Check));
            UiKit.Place(view.Tick, 0.34f, 0.10f, 0.66f, 0.90f);

            // DERS (görünen her düğme BİR ŞEY YAPMALI): Bu düğme uzun süre
            // yalnız çiziliyordu — `onClick` bağlanmamıştı, dosyadaki tek
            // gerçek Button atlama düğmeleriydi. Ekranda yeşil, parlak ve
            // tıklanabilir görünen ama basınca hiçbir şey olmayan bir düğme,
            // oyuncuya "oyun bozuk" dedirtir; eksik bir özellikten beterdir.
            int from = region.from, to = region.to;
            var click = action.gameObject.AddComponent<UnityEngine.UI.Button>();
            click.targetGraphic = shadow;
            click.transition = UnityEngine.UI.Selectable.Transition.None;
            action.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            click.onClick.AddListener(() => PlayRegion(from, to));

            view.ActionRoot = action;
            _regions.Add(view);
        }

        /// <summary>
        /// Bölgeye dokununca oynanacak bölüm: oyuncunun ilerlemesi bu bölgenin
        /// aralığına KIRPILIR. Böylece içinde bulunduğu bölge "kaldığın yerden
        /// devam", tamamladığı bölge ise "o bölgenin son bölümünü tekrar oyna"
        /// olur — ikisi de tek satırla ve sürprizsiz.
        ///
        /// Kilitli bölgede düğme zaten gizli (bkz. Refresh), o yüzden burada
        /// ayrıca kontrol etmeye gerek yok; yine de kırpma onu da güvene alıyor.
        /// </summary>
        static void PlayRegion(int from, int to)
        {
            if (!MetaServices.Ready) return;

            int reached = MetaServices.Progress.HighestUnlockedIndex + 1;
            int level = Mathf.Clamp(reached, from, to);
            Flow.AppRouter.PlayLevel(level - 1);   // görünen numara 1 tabanlı
        }

        /// <summary>Oyuncunun bulunduğu seviyeyi borunun üstünde gösteren pembe rozet.</summary>
        void BuildMarker()
        {
            _marker = UiKit.CreateRect("Marker", _content);
            Anchor(_marker, 46f, 0f, 130f, 70f);

            var pill = MenuCapsule("Face", _marker, new Color(0.847f, 0.200f, 0.620f));
            UiKit.Place(pill, 0f, 0f, 1f, 1f);

            _markerLabel = UiKit.CreateTitle("Label", pill.transform, "1", 34,
                UiKit.Ink, new Color(0.35f, 0.04f, 0.22f));
            UiKit.Place(_markerLabel, 0.06f, 0.06f, 0.94f, 0.94f);
        }

        /// <summary>
        /// Rayın iki ucuna atlayan düğmeler.
        ///
        /// DERS (referans bunları BORUNUN ÜSTÜNE koyuyor): Bu ikisi bir ara sağ
        /// kenara alınmıştı çünkü ekranın ortasındayken rozetlerle çakışıyordu.
        /// Referansta çakışma yok, çünkü onlar rayın İKİ UCUNDA ve ray orada
        /// zaten boş. Çözüm düğmeyi kaçırmak değil, doğru yere koymaktı.
        /// </summary>
        void BuildJumpButtons(Transform root)
        {
            BuildJump(root, "Top", 0.842f, () => _scroll.verticalNormalizedPosition = 1f);
            BuildJump(root, "Bottom", 0.030f, () => _scroll.verticalNormalizedPosition = 0f);
        }

        void BuildJump(Transform root, string text, float cy, UnityEngine.Events.UnityAction go)
        {
            var button = UiKit.CreateRect("Jump_" + text, root);
            UiKit.Place(button, 0.405f, cy - 0.017f, 0.595f, cy + 0.017f);

            var face = MenuCapsule("Face", button, JumpFace);
            UiKit.Place(face, 0f, 0f, 1f, 1f);

            var label = UiKit.CreateTitle("Label", face.transform, text, 30,
                UiKit.Ink, new Color(0.12f, 0.13f, 0.24f));
            UiKit.Place(label, 0.06f, 0.06f, 0.94f, 0.94f);

            var click = button.gameObject.AddComponent<Button>();
            click.targetGraphic = face;
            click.transition = Selectable.Transition.None;
            button.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            click.onClick.AddListener(() => { if (_scroll != null) go(); });
        }

        // ---- Yardımcılar ----------------------------------------------------

        static Image MenuCapsule(string name, Transform parent, Color color)
        {
            var rect = UiKit.CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = MenuSprites.Capsule;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Color Darken(Color color, float factor) =>
            new Color(color.r * factor, color.g * factor, color.b * factor, color.a);

        /// <summary>Alttan yukarı büyüyen dikey çubuk (borunun ışıklı kısmı).</summary>
        static void AnchorFromBottom(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>Kaydırma içeriğinde MUTLAK yerleştirme (oran değil piksel).</summary>
        static void Anchor(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>Yatayda oran, dikeyde piksel — kapsül her ekran oranında aynı payı alsın.</summary>
        static void AnchorNormalizedX(RectTransform rect, float x0, float x1, float y, float height)
        {
            rect.anchorMin = new Vector2(x0, 0f);
            rect.anchorMax = new Vector2(x1, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = new Vector2(0f, y);
        }

        // ---- Tazeleme --------------------------------------------------------

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
                y += isRegion ? RegionH : RowHeight;
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

            // DERS (referans ULAŞILMAMIŞ satırı SOLDURMUYOR): Burada
            // ulaşılmamış kilometre taşlarının yazısı soluklaştırılıyordu —
            // "henüz senin değil" demek mantıklı geliyordu. Videoda 75/80/90
            // satırlarına bakınca kapsül ve yazı, alınmış satırlarla BİREBİR
            // aynı çıktı; tek fark yeşil tikin olmaması. Bilgiyi zaten tik
            // taşıyor, ikinci bir sinyal gürültü.
            foreach (var (check, level) in _checks)
                check.enabled = reached >= level;

            float progress = MarkerPosition(reached);

            // Boru ilerlemeyi taşır: ışıklı kısım oyuncunun bulunduğu yere
            // kadar uzanır, ötesi koyu kalır.
            if (_tubeLive != null)
            {
                _tubeLive.sizeDelta = new Vector2(_tubeLive.sizeDelta.x, progress);
                _tubeGlow.sizeDelta = new Vector2(_tubeGlow.sizeDelta.x, progress);
            }

            // Bölge çemberi üç durumda: tamamlandı (yeşil + tik), açık
            // (yeşil "Kullan"), kilitli (gri perde + asma kilit + "Seviye N").
            foreach (var view in _regions)
            {
                var region = System.Array.Find(Regions, r => r.from == view.From);
                bool open = reached >= region.from;
                bool done = reached > region.to;

                view.Wash.enabled = !open;
                view.Lock.enabled = !open;
                view.LockTag.gameObject.SetActive(!open);
                view.ActionRoot.gameObject.SetActive(open);

                view.LockLabel.text = "Level " + region.from;
                view.Tick.enabled = done;
                view.ActionLabel.text = done ? "" : "Play";
            }

            if (_marker != null)
            {
                _markerLabel.text = reached.ToString();
                _marker.anchoredPosition = new Vector2(_marker.anchoredPosition.x, progress);
            }
        }

        /// <summary>
        /// Oyuncunun ray üzerindeki yeri: bir sonraki ulaşılmamış kilometre
        /// taşının hizası. Hem pembe rozet hem borunun ışıklı boyu bunu kullanır
        /// — iki ayrı hesap yapılsaydı biri güncellenip diğeri unutulurdu.
        /// </summary>
        static float MarkerPosition(int reached)
        {
            float y = EdgePad;
            float at = EdgePad;
            foreach (var (isRegion, index) in Entries())
            {
                float height = isRegion ? RegionH : RowHeight;
                if (!isRegion && Milestones[index].level > reached)
                    return y + height * 0.5f;
                y += height;
                at = y;
            }
            return at;
        }
    }
}
