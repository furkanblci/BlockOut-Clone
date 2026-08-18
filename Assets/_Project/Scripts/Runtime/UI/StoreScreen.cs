using BlockOut.Core;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;
using UiCornerFit = GameKit.UI.UiCornerFit;
using UiSprites = GameKit.UI.UiSprites;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Mağaza — referans oyundan ölçülerek yeniden kuruldu.
    ///
    /// Referans kare: `Block Out! menus,powerups,vs.mp4`, 08-14. saniyeler.
    /// Renkler ve yükseklikler o karelerden piksel örnekleyerek alındı, göz
    /// kararıyla değil.
    ///
    /// EKRANIN YAPISI (yukarıdan aşağı):
    ///   • Mavi çizgili TENTE — sabit; içerik onun ALTINDAN kayar.
    ///   • "Special Offers" — koyu bordo zemin, turuncu kapsül başlık, tek
    ///     büyük turuncu kart + taşıyıcı noktaları.
    ///   • "Packs" — koyu mor zemin, mor kapsül başlık, 5 krem kart.
    ///   • "Coins" — koyu kızıl zemin, kırmızı kapsül başlık, 3×2 kutu.
    ///   • "Restore Purchases" — mavi düğme.
    ///
    /// DERS (bölüm zemini içeriğin İÇİNDE olmalı): İlk akla gelen, ekrana tek
    /// bir zemin verip bölümleri onun üstüne dizmek. Ama referansta zemin rengi
    /// bölümle birlikte KAYIYOR — "Packs"in moru, "Coins"ın kızılına
    /// kaydırırken geçiyor. Zemin ekranın değil, kaydırılan içeriğin parçası.
    ///
    /// DERS (dikey piksel, yatay oran): Kanvas ölçekleyici YÜKSEKLİĞE kilitli
    /// (matchWidthOrHeight = 1), yani 1920 birimlik yükseklik her cihazda aynı
    /// ama GENİŞLİK ekran oranıyla değişiyor. Bu yüzden yükseklikler birim
    /// olarak sabit yazıldı, yatay yerleşim ise oranla. Tersini yapmak, geniş
    /// bir tablette kartları incecik bırakırdı.
    /// </summary>
    public sealed class StoreScreen : MonoBehaviour
    {
        // ---- Referanstan örneklenen palet ---------------------------------

        static readonly Color BgOffers   = new Color(0.314f, 0.094f, 0.063f);
        static readonly Color BgPacks    = new Color(0.200f, 0.102f, 0.349f);
        static readonly Color BgCoins    = new Color(0.325f, 0.047f, 0.122f);

        static readonly Color PillOffers = new Color(0.922f, 0.239f, 0.047f);
        static readonly Color PillPacks  = new Color(0.537f, 0.090f, 0.788f);
        static readonly Color PillCoins  = new Color(0.796f, 0.071f, 0.239f);
        static readonly Color PillRim    = new Color(1.000f, 0.937f, 0.851f);
        static readonly Color PillRimGold = new Color(0.980f, 0.741f, 0.200f);

        static readonly Color CardCream  = new Color(0.984f, 0.945f, 0.886f);
        static readonly Color CardShelf  = new Color(0.898f, 0.816f, 0.718f);
        static readonly Color BandPurple = new Color(0.553f, 0.090f, 0.776f);
        static readonly Color CoinBase   = new Color(0.827f, 0.165f, 0.341f);

        static readonly Color OfferTop   = new Color(0.988f, 0.729f, 0.020f);
        static readonly Color OfferLow   = new Color(0.961f, 0.537f, 0.078f);
        static readonly Color OfferBand  = new Color(0.929f, 0.376f, 0.067f);

        static readonly Color PriceGreen = new Color(0.247f, 0.725f, 0.153f);
        static readonly Color RestoreBlue = new Color(0.173f, 0.545f, 0.996f);
        static readonly Color RibbonPink = new Color(0.847f, 0.200f, 0.420f);
        static readonly Color BadgeRed   = new Color(0.949f, 0.200f, 0.204f);

        /// <summary>Krem kartın üstünde beyaz okunmaz; referans da koyu kahve kullanıyor.</summary>
        static readonly Color Cocoa      = new Color(0.322f, 0.169f, 0.051f);
        static readonly Color TitleShade = new Color(0.086f, 0.129f, 0.365f);

        // ---- Referanstan ölçülen yükseklikler (kanvas birimi) --------------

        // Tentenin en alçak noktası referansta y=286 (946×2048) → ekranın
        // %13.96'sı → 268 birim.
        const float AwningH   = 268f;
        const float PadTop    = 34f;
        const float PillH     = 104f;
        const float PillGap   = 54f;
        const float OfferArtH = 340f;
        const float OfferBandH = 140f;
        const float OfferLipH = 26f;
        const float DotsH     = 62f;
        const float PackCreamH = 330f;
        const float PackBandH = 165f;
        const float PackGap   = 64f;
        const float TileCreamH = 320f;
        const float TileBaseH = 132f;
        const float TileGap   = 50f;
        const float RestoreH  = 110f;
        const float SectionEnd = 46f;

        const float MarginX   = 0.036f;

        // ---- İçerik --------------------------------------------------------

        /// <summary>Referanstaki beş paket. Süreler saat; "72s" = 72 saat.</summary>
        readonly struct Pack
        {
            public readonly string Name, Price, Ribbon;
            public readonly int Coins, Hours, Helpers;

            public Pack(string name, int coins, int hours, int helpers, string price,
                        string ribbon = null)
            {
                Name = name; Coins = coins; Hours = hours;
                Helpers = helpers; Price = price; Ribbon = ribbon;
            }
        }

        static readonly Pack[] Packs =
        {
            new Pack("Brick Pack",     2000,  3,  1, "$4.99"),
            new Pack("Block Pack",     5000,  6,  3, "$9.99"),
            new Pack("Premium Pack",   8000, 12,  8, "$19.99", "Popular"),
            new Pack("Deluxe Pack",   20000, 24, 16, "$49.99"),
            new Pack("Ultimate Pack", 60000, 72, 36, "$99.99", "Best Value")
        };

        /// <summary>
        /// "Special Offers" taşıyıcısındaki üç teklif.
        ///
        /// Referansta üç nokta görünüyor ama görüntüde yalnız BİRİNCİ teklif
        /// açık (Başlangıç Paketi, %90, 1 000 jeton, 99,99 TL). İkincisi ve
        /// üçüncüsü hiç kaydedilmemiş; buradakiler aynı ailenin makul devamı
        /// olarak yazıldı. Gerçek kareler gelirse değişecek tek yer burası.
        /// </summary>
        readonly struct Offer
        {
            public readonly string Name, Price, Discount;
            public readonly int Coins, Hours, Helpers, Pile;

            public Offer(string name, int coins, int hours, int helpers, int pile,
                         string discount, string price)
            {
                Name = name; Coins = coins; Hours = hours; Helpers = helpers;
                Pile = pile; Discount = discount; Price = price;
            }
        }

        static readonly Offer[] Offers =
        {
            new Offer("Starter Pack",      1000,  1, 1, 2, "90", "$1.99"),
            new Offer("Weekly Deal",       6000, 12, 4, 4, "75", "$7.99"),
            new Offer("Mega Deal",        15000, 24, 9, 5, "60", "$15.99")
        };

        /// <summary>Referanstaki jeton kutuları (3×2).</summary>
        static readonly (int coins, string price)[] CoinPacks =
        {
            (1000,   "$1.99"),
            (5000,   "$7.99"),
            (10000,  "$15.99"),
            (25000,  "$29.99"),
            (50000,  "$59.99"),
            (100000, "$99.99")
        };

        /// <summary>Gerçek ödeme akışı buraya bağlanır (SDK entegrasyonu).</summary>
        public event System.Action<int, string> OnPurchaseRequested;

        TextMeshProUGUI _coinLabel, _toast;
        float _toastUntil;
        OfferCarousel _carousel;
        RectTransform[] _carouselPages;
        Image[] _carouselDots;
        readonly System.Collections.Generic.List<Button> _purchaseButtons =
            new System.Collections.Generic.List<Button>();

        // ====================================================================

        public static RectTransform Build(Transform parent)
        {
            var root = UiKit.CreateRect("StoreScreen", parent);
            GameKit.UI.UiTweak.Mark(root, "StoreScreen");
            UiKit.Place(root, 0f, 0f, 1f, 1f);
            var screen = root.gameObject.AddComponent<StoreScreen>();

            // Kaydırma alanı EKRANIN TAMAMI: referansta kartlar tentenin altına
            // girerek kayboluyor, tentenin altında bitmiyor.
            var viewport = UiKit.CreateRect("Viewport", root);
            UiKit.Place(viewport, 0f, 0f, 1f, 1f);

            // Görünüm alanı EKRANIN DİBİNE kadar uzar.
            //
            // DERS (kabuk ekranı sekme çubuğunun ÜSTÜNDE bitiriyor): MenuShell
            // ekranları 0.105–1 aralığına koyuyor, çubuk ise 0–0.086'da. Aradaki
            // ince şeritte HİÇBİR ekran yok ve altındaki ana ekranın manzarası
            // sızıyordu — mağazanın koyu zemini çubuğun hemen üstünde kesilip
            // yeşil bir çizgi bırakıyordu. Alanı aşağı taşırmak, bölüm zemininin
            // çubuğun altına kadar devam etmesini sağlıyor; fazlası zaten çubuk
            // tarafından örtülüyor (çubuk ekranlardan SONRA kuruluyor).
            viewport.offsetMin = new Vector2(0f, -210f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = UiKit.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            float height = screen.BuildContent(content);
            content.sizeDelta = new Vector2(0f, height);

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.scrollSensitivity = 45f;
            scroll.decelerationRate = 0.12f;

            // Taşıyıcı dıştaki listeyi ancak o kurulduktan sonra tanıyabilir:
            // dikey jestleri ona AKTARACAK.
            if (screen._carousel != null)
                screen._carousel.Bind(scroll, screen._carouselPages, screen._carouselDots);

            // Tente EN SON kurulur: kardeş sırası çizim sırasıdır, içeriğin
            // üstünde kalması gereken tek şey o.
            screen.BuildAwning(root);
            return root;
        }

        // ---- Tente ---------------------------------------------------------

        void BuildAwning(Transform root)
        {
            // Çentik şeridi: tentenin üstünde kalan güvenli alan dışı boşluk.
            // Renk referanstaki tentenin KOYU şeridi (`market.jpeg` üst kenarı,
            // #053AE8). Çizgili deseni yukarı sürdürmek yerine düz renk
            // kullanıldı: o şerit çentiğin/durum çubuğunun altında kalıyor,
            // tentenin festonunu yukarı esnetmek ise tasarımı bozardı.
            // (6. APK bulgusu — bkz. MenuPage.Header'daki ders.)
            var bleed = UiKit.CreateRect("AwningBleed", root);
            bleed.anchorMin = new Vector2(0f, 1f);
            bleed.anchorMax = new Vector2(1f, 1f);
            bleed.pivot = new Vector2(0.5f, 0f);
            bleed.sizeDelta = new Vector2(0f, 320f);
            bleed.anchoredPosition = Vector2.zero;
            var bleedImage = bleed.gameObject.AddComponent<Image>();
            // Renk, tentenin ÜST SATIRININ ortalaması olmalı; yoksa çentiksiz
            // bir telefonda ikisi arasında görünür bir dikiş kalır. Tentenin
            // üst satırı taban rengin %80'i (dikey parlaklık rampası), iki
            // şeridin ortalaması ≈ #016FC3.
            bleedImage.color = new Color(0.004f, 0.435f, 0.765f);
            bleedImage.raycastTarget = false;

            var bar = Row("Awning", root, 0f, AwningH);

            var cloth = bar.gameObject.AddComponent<Image>();
            cloth.sprite = MenuSprites.Awning;
            cloth.type = Image.Type.Simple;
            cloth.raycastTarget = true;      // tenteye dokunmak içeriği kaydırmasın

            // Jeton göstergesi: krem kapsül + SOL UCUNDAN taşan jeton.
            // Ölçü referanstan (`market.jpeg`): kapsül X 0.112-0.338,
            // jeton X 0.036-0.123 — yani jeton kapsülün soluna taşıyor,
            // kapsülün altında başlamıyor. Bizimki 0.055'ten başlıyordu ve
            // jetonun tamamı kapsülün üstünde kalıyordu (12. APK bulgusu).
            var pill = Capsule("CoinPill", bar, new Color(1f, 0.976f, 0.925f));
            UiKit.Place(pill, 0.112f, 0.50f, 0.338f, 0.70f);

            _coinLabel = UiKit.CreateLabel("Coins", pill.transform, "0", 40, Cocoa);
            UiKit.Place(_coinLabel, 0.28f, 0.04f, 0.92f, 0.96f);

            var coin = UiKit.CreateIcon("Coin", bar, UiSkin.Get(Art.Coin));
            UiKit.Place(coin, 0.030f, 0.475f, 0.135f, 0.725f);

            // Başlık ekranın ortasında DEĞİL: referansta merkezi 0.524'te,
            // yani jeton kapsülünün sağında kalan alanın ortasında. Bizimki
            // 0.575'teydi — sağa kaçmış görünüyordu.
            // Punto referanstan: "M" harfinin yüksekliği ekranın %3.37'si;
            // 84 punto %3.15 veriyordu.
            var title = UiKit.CreateTitle("Title", bar, "Shop", 90,
                new Color(1f, 0.99f, 0.96f), TitleShade);
            UiKit.Place(title, 0.300f, 0.44f, 0.748f, 0.76f);

            // KONTUR — Yolculuk başlığındaki aynı tuzak burada da vardı:
            // `CreateTitle`'a verilen kontur rengi paylaşılan materyal
            // yüzünden sessizce yok sayılıyor, başlık düz beyaz kalıyor.
            // Referansta kalın lacivert kontur var (#0A0F55, örneklendi) ve
            // başlığa "baskılı" görünümünü veren şey o.
            UiKit.SetOutline(title, new Color(0.039f, 0.059f, 0.333f));

            // Satın alma sonucu: referansta böyle bir satır yok, ama sonucu
            // hiç söylememek de yok. Kısa süre görünüp kayboluyor.
            _toast = UiKit.CreateLabel("Toast", bar, "", 30, new Color(1f, 0.95f, 0.75f));
            UiKit.Place(_toast, 0.05f, 0.05f, 0.95f, 0.28f);
            _toast.gameObject.SetActive(false);
        }

        // ---- İçerik --------------------------------------------------------

        /// <summary>Bütün bölümleri dizer ve toplam yüksekliği döndürür.</summary>
        float BuildContent(Transform content)
        {
            float y = 0f;

            // --- Özel Teklifler ---
            float sectionTop = y;
            y = AwningH + PadTop;
            // Turuncu şeridin konturu referansta krem değil ALTIN.
            SectionPill(content, y, "Special Offers", PillOffers, PillRimGold);
            y += PillH + PillGap;

            var (carousel, pages) = BuildOffers(content, y);
            y += OfferArtH + OfferBandH + OfferLipH + 10f;

            var dots = BuildDots(content, y, Offers.Length);
            y += DotsH + SectionEnd;
            _carousel = carousel;
            _carouselPages = pages;
            _carouselDots = dots;
            Background(content, sectionTop, y - sectionTop, BgOffers, "BgOffers");

            // --- Paketler ---
            sectionTop = y;
            y += PadTop;
            SectionPill(content, y, "Packs", PillPacks);
            y += PillH + PillGap;

            for (int i = 0; i < Packs.Length; i++)
            {
                BuildPack(content, y, i);
                y += PackCreamH + PackBandH;
                if (i < Packs.Length - 1) y += PackGap;
            }
            y += SectionEnd;
            Background(content, sectionTop, y - sectionTop, BgPacks, "BgPacks");

            // --- Jetonlar ---
            sectionTop = y;
            y += PadTop;
            SectionPill(content, y, "Coins", PillCoins);
            y += PillH + PillGap;

            for (int i = 0; i < CoinPacks.Length; i++)
            {
                int col = i % 3, row = i / 3;
                BuildCoinTile(content, y + row * (TileCreamH + TileBaseH + TileGap), col, i);
            }
            y += 2f * (TileCreamH + TileBaseH) + TileGap + 60f;

            BuildRestore(content, y);
            y += RestoreH + 70f;
            Background(content, sectionTop, y - sectionTop, BgCoins, "BgCoins");

            return y;
        }

        /// <summary>
        /// Bölüm zemini. Kardeş sırasında EN BAŞA alınır: sonradan eklenen
        /// zemin, üstüne dizilmiş kartların önüne geçerdi.
        /// </summary>
        /// <summary>
        /// Bölüm zemini — düz renk DEĞİL, kapitone dokulu.
        ///
        /// Referansta zemin dokulu (bkz. `MenuSprites.Quilt`); bizde düz
        /// renkti ve ekran "sade" duruyordu (11. APK bulgusu). Doku beyaz
        /// üstüne yalnız ışık farkı olarak çizildiği için bölüm rengi tint
        /// ile veriliyor ve tek doku üç bölümde de doğru çalışıyor.
        /// </summary>
        static void Background(Transform content, float top, float height, Color color, string name)
        {
            var rect = Row(name, content, top, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = MenuSprites.Quilt;
            image.type = Image.Type.Tiled;
            // Döşeme ölçüsü: desen okunsun ama gürültü olmasın. 128 piksellik
            // kare, kanvas biriminde ~190'a geliyor.
            UiKit.SetSliceScale(image, 0.68f);
            image.color = color;
            image.raycastTarget = false;
            rect.SetAsFirstSibling();
        }

        static void SectionPill(Transform content, float top, string text, Color face,
                                Color? rimColor = null)
        {
            var row = Row("Pill_" + text, content, top, PillH, MarginX, 1f - MarginX);

            var rim = Capsule("Rim", row, rimColor ?? PillRim);
            UiKit.Place(rim, 0f, 0f, 1f, 1f);

            var fill = Capsule("Face", row, face);
            UiKit.Place(fill, 0.008f, 0.10f, 0.992f, 0.90f);

            var label = UiKit.CreateTitle("Label", row, text, 56,
                new Color(1f, 0.99f, 0.96f), new Color(0.24f, 0.05f, 0.02f));
            UiKit.Place(label, 0.06f, 0.06f, 0.94f, 0.94f);
        }

        // ---- Özel teklif taşıyıcısı -----------------------------------------

        /// <summary>
        /// Üç teklifi yan yana koyar ve yatay kaydırmayı kurar.
        /// Görünüm alanı satırın kendisi; sayfaların genişliği çalışma anında
        /// <see cref="OfferCarousel"/> tarafından veriliyor.
        /// </summary>
        (OfferCarousel carousel, RectTransform[] pages) BuildOffers(Transform content, float top)
        {
            float height = OfferArtH + OfferBandH + OfferLipH;
            var viewport = Row("Offers", content, top, height);
            viewport.gameObject.AddComponent<RectMask2D>();

            // Kaydırmayı yakalayacak görünmez yüzey: Image olmadan dokunma
            // hiç gelmez, jest doğrudan alttaki listeye düşerdi.
            var surface = viewport.gameObject.AddComponent<Image>();
            surface.color = new Color(0f, 0f, 0f, 0f);

            var strip = UiKit.CreateRect("Pages", viewport);
            strip.anchorMin = new Vector2(0f, 0f);
            strip.anchorMax = new Vector2(0f, 1f);
            strip.pivot = new Vector2(0f, 0.5f);
            strip.anchoredPosition = Vector2.zero;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = strip;
            scroll.viewport = viewport;
            scroll.horizontal = false;      // sürüklemeyi OfferCarousel yürütüyor
            scroll.vertical = false;

            var pages = new RectTransform[Offers.Length];
            for (int i = 0; i < Offers.Length; i++)
            {
                pages[i] = UiKit.CreateRect("Page" + i, strip);
                BuildOfferCard(pages[i], Offers[i]);
            }

            return (viewport.gameObject.AddComponent<OfferCarousel>(), pages);
        }

        void BuildOfferCard(Transform page, Offer offer)
        {
            float height = OfferArtH + OfferBandH + OfferLipH;
            var card = UiKit.CreateRect("Card", page);
            UiKit.Place(card, MarginX, 0f, 1f - MarginX, 1f);

            // Alt kalınlık + isim bandı tek panelde; üstüne turuncu sanat alanı.
            var body = UiKit.CreateRoundedPanel("Body", card, Darken(OfferBand, 0.72f));
            UiKit.Place(body, 0f, 0f, 1f, 1f);

            var band = UiKit.CreateRoundedPanel("Band", card, OfferBand);
            UiKit.Place(band, 0f, OfferLipH / height, 1f, (OfferLipH + OfferBandH) / height);

            var art = UiKit.CreateRoundedPanel("Art", card, OfferTop);
            UiKit.Place(art, 0f, (OfferLipH + OfferBandH) / height - 0.04f, 1f, 1f);

            // Dikey degrade: alta doğru koyulaşan turuncu.
            var fade = UiKit.CreateRect("Fade", art.transform);
            var fadeImage = fade.gameObject.AddComponent<Image>();
            fadeImage.sprite = MenuSprites.FadeDown;
            fadeImage.type = Image.Type.Sliced;
            fadeImage.color = OfferLow;
            fadeImage.raycastTarget = false;
            UiKit.Place(fade, 0.01f, 0.02f, 0.99f, 0.98f);

            var pile = UiKit.CreateIcon("Pile", art.transform,
                Tiered(Art.CoinPile, offer.Pile, Art.Coin));
            UiKit.Place(pile, 0.06f, 0.14f, 0.46f, 0.98f);

            var amount = UiKit.CreateTitle("Amount", art.transform, Amount(offer.Coins), 72,
                new Color(1f, 1f, 1f), TitleShade);
            UiKit.Place(amount, 0.16f, 0.04f, 0.56f, 0.34f);

            RewardIcons(art.transform, showNoAds: false, offer.Hours, offer.Helpers);

            // İndirim flaması — sol üst köşeden aşağı sarkar.
            var badge = UiKit.CreateRect("Badge", art.transform);
            var flag = badge.gameObject.AddComponent<Image>();
            flag.sprite = MenuSprites.Pennant;
            flag.color = BadgeRed;
            flag.raycastTarget = false;
            UiKit.Place(badge, 0.025f, 0.52f, 0.175f, 1.10f);

            var percent = UiKit.CreateTitle("Percent", badge.transform, offer.Discount + "%", 44,
                new Color(1f, 1f, 1f), new Color(0.42f, 0.03f, 0.03f));
            UiKit.Place(percent, 0.02f, 0.52f, 0.98f, 0.92f);

            var word = UiKit.CreateTitle("Word", badge.transform, "OFF", 22,
                new Color(1f, 0.94f, 0.80f), new Color(0.42f, 0.03f, 0.03f));
            UiKit.Place(word, 0.02f, 0.28f, 0.98f, 0.52f);

            var name = UiKit.CreateTitle("Name", band.transform, offer.Name, 58,
                new Color(1f, 0.99f, 0.96f), new Color(0.45f, 0.13f, 0.02f));
            UiKit.Place(name, 0.05f, 0.06f, 0.62f, 0.94f);

            var buy = PriceButton("Buy", band.transform, offer.Price, 44);
            UiKit.Place(buy, 0.64f, 0.14f, 0.96f, 0.86f);

            var captured = offer;
            buy.onClick.AddListener(() => Purchase("offer_" + captured.Coins, captured.Coins,
                captured.Hours, true, captured.Price, captured.Helpers));
            _purchaseButtons.Add(buy);
        }

        /// <summary>Taşıyıcı noktaları; renkleri <see cref="OfferCarousel"/> sürüyor.</summary>
        static Image[] BuildDots(Transform content, float top, int count)
        {
            var row = Row("Dots", content, top, DotsH);
            var dots = new Image[count];

            for (int i = 0; i < count; i++)
            {
                // `CreateIcon` en-boy oranını korur: kutu kare olmasa da nokta
                // daire kalır (bkz. MenuPage.Close — bu projede üç kez elips
                // çıktı).
                var image = UiKit.CreateIcon("Dot" + i, row, UiSprites.Circle);
                var dot = image.rectTransform;
                // Başlangıç rengi KURULUMDA verilir. Taşıyıcı bunu her kare
                // güncelliyor ama ilk karede henüz çalışmamış olur; üstelik
                // düzenleyici kipinde Update hiç çalışmaz ve ekran görüntüsünde
                // üç nokta da beyaz çıkar.
                image.color = i == 0
                    ? new Color(1f, 0.78f, 0.16f)
                    : new Color(0.62f, 0.35f, 0.16f);
                dots[i] = image;

                float cx = 0.5f + (i - (count - 1) * 0.5f) * 0.035f;
                UiKit.Place(dot, cx - 0.011f, 0.30f, cx + 0.011f, 0.70f);
            }
            return dots;
        }

        // ---- Paket kartı ----------------------------------------------------

        void BuildPack(Transform content, float top, int index)
        {
            var pack = Packs[index];
            float height = PackCreamH + PackBandH;
            var card = Row("Pack_" + index, content, top, height, MarginX, 1f - MarginX);

            // Mor bant tüm kartı kaplar; krem alan onun üstüne oturur ve alt
            // köşeleri bandın arkasında kalır — böylece iki ayrı görsel
            // gerekmeden referanstaki "krem üst + mor alt" biçimi çıkar.
            var band = UiKit.CreateRoundedPanel("Band", card, BandPurple);
            UiKit.Place(band, 0f, 0f, 1f, 1f);

            var shelf = UiKit.CreateRoundedPanel("Shelf", card, CardShelf);
            UiKit.Place(shelf, 0f, (PackBandH - 16f) / height, 1f, 1f);

            var cream = UiKit.CreateRoundedPanel("Cream", card, CardCream);
            UiKit.Place(cream, 0f, (PackBandH + 6f) / height, 1f, 1f);

            var clip = UiKit.CreateRect("Clip", cream.transform);
            UiKit.Place(clip, 0f, 0f, 1f, 1f);
            clip.gameObject.AddComponent<RectMask2D>();

            var art = UiKit.CreateIcon("Art", clip, Tiered(Art.PackArt, index + 1, Art.Chest));
            UiKit.Place(art, 0.03f, 0.06f, 0.52f, 0.98f);

            var amount = UiKit.CreateTitle("Amount", clip, Amount(pack.Coins), 70,
                new Color(1f, 1f, 1f), TitleShade);
            UiKit.Place(amount, 0.14f, 0.02f, 0.55f, 0.30f);

            RewardIcons(clip, showNoAds: true, hours: pack.Hours, helpers: pack.Helpers);

            if (pack.Ribbon != null) Ribbon(clip, pack.Ribbon);

            var name = UiKit.CreateTitle("Name", band.transform, pack.Name, 58,
                new Color(1f, 0.99f, 0.96f), new Color(0.24f, 0.03f, 0.36f));
            UiKit.Place(name, 0.05f, 0.02f, 0.62f, PackBandH / height * 0.92f);

            var buy = PriceButton("Buy", band.transform, pack.Price, 44);
            UiKit.Place(buy, 0.63f, PackBandH / height * 0.18f,
                             0.96f, PackBandH / height * 0.80f);

            var captured = pack;
            buy.onClick.AddListener(() => Purchase(
                "pack_" + captured.Coins, captured.Coins, captured.Hours, true, captured.Price,
                captured.Helpers));
            _purchaseButtons.Add(buy);
        }

        /// <summary>
        /// Kartın sol üst köşesine çapraz şerit.
        ///
        /// DERS (çaprazı MASKE keser): Şeridi köşede kırpmak için ayrı bir
        /// üçgen görsel çizmek gerekmiyor. Şerit 45° döndürülüp kartın
        /// <see cref="RectMask2D"/>'i içine konursa fazlası kendiliğinden
        /// kırpılır ve şerit her kart boyutunda doğru oturur.
        /// </summary>
        static void Ribbon(Transform clip, string text)
        {
            // Köşeden uzaklık, şeridin kart içinde kalan boyunu belirler:
            // 45°'lik bir kirişin uzunluğu ≈ 2·uzaklık·√2. Yazının tamamının
            // görünmesi için şerit köşeden yeterince İÇERİ alınmalı — ilk
            // denemede 18 birim kalmıştı ve "Popüler" yazısının yalnız "pül"ü
            // görünüyordu.
            const float inset = 88f;

            var strip = UiKit.CreateRect("Ribbon", clip);
            strip.anchorMin = strip.anchorMax = new Vector2(0f, 1f);
            strip.pivot = new Vector2(0.5f, 0.5f);
            strip.sizeDelta = new Vector2(2f * inset * 1.414f, 46f);
            strip.anchoredPosition = new Vector2(inset, -inset);
            strip.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var image = strip.gameObject.AddComponent<Image>();
            image.color = RibbonPink;
            image.raycastTarget = false;

            var label = UiKit.CreateTitle("Label", strip, text, 24,
                new Color(1f, 1f, 1f), new Color(0.45f, 0.06f, 0.18f));
            UiKit.Place(label, 0f, 0f, 1f, 1f);
        }

        // ---- Jeton kutusu ---------------------------------------------------

        void BuildCoinTile(Transform content, float top, int col, int index)
        {
            var (coins, price) = CoinPacks[index];

            const float gap = 0.024f;
            float span = (1f - 2f * MarginX - 2f * gap) / 3f;
            float x0 = MarginX + col * (span + gap);

            float height = TileCreamH + TileBaseH;
            var tile = Row("Tile_" + index, content, top, height, x0, x0 + span);

            var basePlate = UiKit.CreateRoundedPanel("Base", tile, CoinBase);
            UiKit.Place(basePlate, 0f, 0f, 1f, 1f);

            var cream = UiKit.CreateRoundedPanel("Cream", tile, CardCream);
            UiKit.Place(cream, 0f, (TileBaseH - 14f) / height, 1f, 1f);

            // Yığın HER KUTUDA AYNI BÜYÜKLÜKTE.
            //
            // DERS (ölç, varsayma): Önce yığınlar kademeli büyütülmüştü —
            // "büyük paket büyük görünsün" mantıklı geliyordu. Referans
            // karesinden ölçünce altı yığının da genişliği 226-229 piksel
            // çıktı, yani hepsi kutuyu aynı biçimde dolduruyor; artan tek şey
            // JETON SAYISI (dolgu oranı %21'den %35'e çıkıyor). Mesajı taşıyan
            // şey yığının boyu değil, içindeki jeton yoğunluğu. Kademeli
            // büyütmek üstelik küçük kutuları boş bırakıp ızgarayı dağıtıyordu.
            var pile = UiKit.CreateIcon("Pile", cream.transform,
                Tiered(Art.CoinPile, index + 1, Art.Coin));
            UiKit.Place(pile, 0.02f, 0.14f, 0.98f, 1.0f);

            var amount = UiKit.CreateTitle("Amount", cream.transform, Amount(coins), 52,
                new Color(1f, 1f, 1f), TitleShade);
            UiKit.Place(amount, 0.02f, 0.06f, 0.98f, 0.32f);

            var buy = PriceButton("Buy", tile, price, 36);
            UiKit.Place(buy, 0.04f, 0.02f, 0.96f, TileBaseH / height * 0.86f);
            buy.onClick.AddListener(() => Purchase("coins_" + coins, coins, 0, false, price));
            _purchaseButtons.Add(buy);
        }

        void BuildRestore(Transform content, float top)
        {
            var row = Row("Restore", content, top, RestoreH, 0.24f, 0.76f);

            var shadow = Capsule("Shadow", row, Darken(RestoreBlue, 0.62f));
            UiKit.Place(shadow, 0f, 0f, 1f, 1f);

            var face = Capsule("Face", row, RestoreBlue);
            UiKit.Place(face, 0.01f, 0.14f, 0.99f, 1f);

            var label = UiKit.CreateLabel("Label", face.transform,
                "Restore Purchases", 36, new Color(1f, 0.99f, 0.96f));
            label.fontStyle = FontStyles.Bold;
            UiKit.Place(label, 0.04f, 0.05f, 0.96f, 0.95f);

            // `Capsule` yardımcısı süs amaçlı olduğu için raycast'i KAPALI
            // üretiyor; bu düğme de onu hedef grafik olarak kullandığı için
            // dokunuş hiç ulaşmıyordu. `MakeClickable` raycast'i açmayı
            // unutulamaz hâle getiriyor (bkz. UiKit'teki ders).
            UiKit.MakeClickable(row.gameObject, shadow, () =>
            {
                Toast("Checking purchases…");
                PurchaseService.Instance.Restore(Toast);
            });
        }

        // ---- Ödül simgeleri --------------------------------------------------

        /// <summary>
        /// Kartın sağ yarısındaki ödül bloğu: üstte reklam-yok + sınırsız can,
        /// altta üç yardımcı ve adetleri.
        /// </summary>
        void RewardIcons(Transform parent, bool showNoAds, int hours, int helpers)
        {
            if (showNoAds)
            {
                var badge = UiKit.CreateRect("NoAds", parent);
                var ring = badge.gameObject.AddComponent<Image>();
                ring.sprite = UiSkin.Get(Art.NoAds) ?? MenuSprites.NoAds;
                ring.preserveAspect = true;
                ring.raycastTarget = false;
                ring.color = UiSkin.Get(Art.NoAds) != null
                    ? Color.white : new Color(0.85f, 0.10f, 0.12f);
                UiKit.Place(badge, 0.570f, 0.46f, 0.726f, 0.98f);

                // Referansta harfler halkanın dışına TAŞIYOR; halka bir çerçeve
                // değil, yazının üstünü çizen bir işaret.
                var word = UiKit.CreateTitle("Word", parent, "ADS", 40,
                    new Color(1f, 1f, 1f), new Color(0.35f, 0.03f, 0.03f));
                UiKit.Place(word, 0.548f, 0.62f, 0.748f, 0.84f);
            }

            // Sınırsız can.
            //
            // DERS (hazır görsel geldiğinde parçaları ÜST ÜSTE KOYMA):
            // `icon_infinite` zaten ∞'lu bir kalp — düz kalbin üstüne ayrıca
            // onu koymak, kalbin ortasına küçültülmüş ikinci bir kalp basıyor.
            // Görsel varsa TEK parça, yoksa kalp + çizilmiş ∞ halkası.
            var combined = UiSkin.Get(Art.Infinite);
            if (combined != null)
            {
                var icon = UiKit.CreateIcon("InfiniteHeart", parent, combined);
                UiKit.Place(icon, 0.745f, 0.50f, 0.890f, 0.98f);
            }
            else
            {
                var heart = UiKit.CreateIcon("Heart", parent, UiSkin.Get(Art.Heart));
                UiKit.Place(heart, 0.745f, 0.50f, 0.890f, 0.98f);

                var infinite = UiKit.CreateRect("Infinity", parent);
                var glyph = infinite.gameObject.AddComponent<Image>();
                glyph.sprite = MenuSprites.Infinity;
                glyph.preserveAspect = true;
                glyph.raycastTarget = false;
                UiKit.Place(infinite, 0.772f, 0.64f, 0.864f, 0.84f);
            }

            var span = UiKit.CreateTitle("Span", parent, hours + "s", 30,
                new Color(1f, 1f, 1f), TitleShade);
            UiKit.Place(span, 0.845f, 0.44f, 0.955f, 0.62f);

            // Üç yardımcı, referanstaki sırayla: roket, çalar saat, ufo.
            string[] icons = { Art.Rocket, Art.Clock, Art.Ufo };
            float[] centers = { 0.606f, 0.732f, 0.883f };
            for (int i = 0; i < 3; i++)
            {
                var icon = UiKit.CreateIcon("Helper" + i, parent, UiSkin.Get(icons[i]));
                UiKit.Place(icon, centers[i] - 0.070f, 0.14f, centers[i] + 0.070f, 0.48f);

                var count = UiKit.CreateTitle("Count" + i, parent, "x" + helpers, 32,
                    new Color(1f, 1f, 1f), TitleShade);
                UiKit.Place(count, centers[i] - 0.005f, 0.06f, centers[i] + 0.105f, 0.24f);
            }
        }

        // ---- Satın alma ------------------------------------------------------

        /// <summary>
        /// Onay → işlem → sonuç. Gerçek para geçmiyor ama akışın tamamı gerçek;
        /// ödeme SDK'sı bağlanınca değişecek tek yer <see cref="PurchaseService"/>.
        /// </summary>
        void Purchase(string productId, int coins, int hours, bool noAds, string price,
                      int helpers = 0)
        {
            var purchases = PurchaseService.Instance;
            if (purchases.IsBusy) return;                 // çift tıklama koruması

            SetBusy(true);
            Toast("Connecting to store…");

            purchases.Buy(productId, coins, price, outcome =>
            {
                SetBusy(false);
                switch (outcome)
                {
                    case PurchaseResult.Purchased:
                        Grant(coins, hours, noAds, helpers);
                        Toast(Amount(coins) + " coins added to your account!");
                        GameKit.FX.Juice.Run(
                            GameKit.FX.Juice.PunchScale(_coinLabel.transform, 0.30f));
                        Services.AudioService.Purchase();
                        break;
                    case PurchaseResult.Failed:
                        Toast("Payment failed. You can try again.");
                        break;
                    default:
                        Toast("Purchase cancelled.");
                        break;
                }
            });

            OnPurchaseRequested?.Invoke(coins, price);
        }

        /// <summary>
        /// Paketin verdikleri.
        ///
        /// DERS (jetonu SATIN ALMA SERVİSİ ekliyor): PurchaseService başarılı
        /// alımda jetonu kendisi yazıyor; burada bir kez daha eklemek jetonu
        /// ikiye katlardı. Bu yüzden yalnız paketin FAZLADAN verdikleri —
        /// yardımcılar, reklam-yok, sınırsız can — burada işleniyor.
        /// </summary>
        void Grant(int coins, int hours, bool noAds, int helpers)
        {
            if (!MetaServices.Ready) return;
            var progress = MetaServices.Progress;

            progress.GrantPackage(0, hours, noAds);

            if (helpers > 0)
                for (int i = 0; i < 3; i++)
                {
                    string id = ((PowerUpKind)i).ToString().ToLowerInvariant();
                    progress.SetPowerUpCount(id, progress.PowerUpCount(id) + helpers);
                }

            RefreshCoins();
        }

        void SetBusy(bool busy)
        {
            foreach (var button in _purchaseButtons)
                if (button != null) button.interactable = !busy;
        }

        // ---- Tazeleme --------------------------------------------------------

        void OnEnable()
        {
            RefreshCoins();
            if (MetaServices.Ready) MetaServices.Progress.CoinsChanged += OnCoinsChanged;
        }

        void OnDisable()
        {
            if (MetaServices.Ready) MetaServices.Progress.CoinsChanged -= OnCoinsChanged;
        }

        void OnCoinsChanged(int coins)
        {
            if (_coinLabel != null) _coinLabel.text = Amount(coins);
        }

        void RefreshCoins()
        {
            if (_coinLabel == null) return;
            _coinLabel.text = MetaServices.Ready ? Amount(MetaServices.Progress.Coins) : "0";
        }

        void Update()
        {
            if (_toast != null && _toast.gameObject.activeSelf && Time.unscaledTime > _toastUntil)
                _toast.gameObject.SetActive(false);
        }

        void Toast(string message)
        {
            if (_toast == null) return;
            _toast.text = message;
            _toast.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + 3f;
        }

        // ---- Küçük yardımcılar ------------------------------------------------

        /// <summary>Ortak biçim <see cref="MenuPage.Amount"/>'ta; burası ona geçiyor.</summary>
        static string Amount(int value) => MenuPage.Amount(value);

        /// <summary>
        /// Dikeyde piksel, yatayda oran ile yerleşen satır.
        /// <paramref name="top"/> içeriğin tepesinden aşağı uzaklık.
        /// </summary>
        static RectTransform Row(string name, Transform parent, float top, float height,
                                 float x0 = 0f, float x1 = 1f)
        {
            var rect = UiKit.CreateRect(name, parent);
            rect.anchorMin = new Vector2(x0, 1f);
            rect.anchorMax = new Vector2(x1, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = new Vector2(0f, -top);
            return rect;
        }

        /// <summary>
        /// Rengi koyultur — ALFAYA DOKUNMADAN.
        ///
        /// DERS: Unity'de `color * 0.6f` alfayı da çarpar. Gölge olarak konan
        /// koyu kapsül böylece yarı saydam olur ve arkasındaki kartın rengi
        /// içinden geçer; "koyu yeşil" beklerken çamurlu bir ton çıkar.
        /// </summary>
        static Color Darken(Color color, float factor) =>
            new Color(color.r * factor, color.g * factor, color.b * factor, color.a);

        /// <summary>
        /// Kademeli görsel getirir; yoksa BİR ALT kademeye düşer.
        ///
        /// DERS (yedek, sıranın anlamını bozmamalı): Eksik bir jeton yığını
        /// için tek bir jeton ikonuna düşmek "eksik" demek değil, YANLIŞ demek:
        /// 50 000'lik kutu, 25 000'liğin yanında daha AZ veriyormuş gibi
        /// görünür. Bir alt kademeye düşmek en kötü ihtimalle iki kutuyu
        /// eşitler — sıralamayı ters çevirmez.
        /// </summary>
        static Sprite Tiered(System.Func<int, string> naming, int index, string lastResort)
        {
            for (int i = index; i >= 1; i--)
            {
                var sprite = UiSkin.Get(naming(i));
                if (sprite != null) return sprite;
            }
            return UiSkin.Get(lastResort);
        }

        /// <summary>Tam kapsül (stadyum) yüzey.</summary>
        /// <summary>
        /// Mağazanın yüzeyi. Kendi kopyası vardı ve <see cref="MenuPage.Capsule"/>
        /// ile satır satır aynıydı; şekil düzeltmesi tek yerde kalsın diye ona
        /// bağlandı. ("Özel Teklifler"/"Paketler" bantları ve fiyat düğmeleri
        /// buradan besleniyor — kullanıcının "çok oval" bulgusunun sahnesi.)
        /// </summary>
        static Image Capsule(string name, Transform parent, Color color,
                             float cornerShare = UiCornerFit.HouseShare)
            => MenuPage.Capsule(name, parent, color, cornerShare);

        /// <summary>Yeşil fiyat düğmesi: koyu bir kapsülün üstünde parlak yüz.</summary>
        static Button PriceButton(string name, Transform parent, string text, int fontSize)
        {
            var root = UiKit.CreateRect(name, parent);

            var shadow = Capsule("Shadow", root, Darken(PriceGreen, 0.58f));
            UiKit.Place(shadow, 0f, 0f, 1f, 1f);
            shadow.raycastTarget = true;

            var face = Capsule("Face", root, PriceGreen);
            UiKit.Place(face, 0.012f, 0.16f, 0.988f, 1f);

            var label = UiKit.CreateTitle("Label", face.transform, text, fontSize,
                new Color(1f, 1f, 1f), new Color(0.05f, 0.24f, 0.04f));
            UiKit.Place(label, 0.04f, 0.04f, 0.96f, 0.96f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = shadow;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            return button;
        }
    }
}
