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
        /// <summary>
        /// Jeton sayacının rakam rengi. ESKİDEN KAHVERENGİYDİ (Cocoa,
        /// 0.322/0.169/0.051 = #522B0D) — ÖLÇÜM (2026-08-22, `market.jpeg`
        /// üzerinde "142" rakamlarının koyu piksellerinin ortancası):
        /// referansta **#1B255C**, yani LACİVERT. Kahverengi bir yanlıştı.
        /// Adı da değişti: "Cocoa" artık rengi anlatmıyordu.
        /// </summary>
        static readonly Color CoinInk    = new Color32(0x1B, 0x25, 0x5C, 0xFF);
        static readonly Color TitleShade = new Color(0.086f, 0.129f, 0.365f);

        // ---- Referanstan ölçülen yükseklikler (kanvas birimi) --------------

        // Tentenin en alçak noktası referansta y=286 (946×2048) → ekranın
        // %13.96'sı → 268 birim.
        // TENTE YÜKSEKLİĞİ 268 → 360 (2026-08-22).
        // ⚠ BU GEREKÇE YANLIŞTI (9. turda bulundu). Burada "kanvas GENİŞLİKLE
        // ölçekleniyor (matchWidthOrHeight = 0)" deniyor; oysa
        // `UiKit.cs`teki kanvas `matchWidthOrHeight = 1`, yani YÜKSEKLİĞE
        // eşli — bu dosyanın kendi başındaki not da öyle diyor (satır 33).
        // Dolayısıyla ESKİ değer ("ekran yüksekliğinin %13,96'sı" = 268)
        // doğru eksendeydi ve tente 360'a, sonra 400'e çıkarılırken şişti.
        // Doğru hedef: referans tente 315 px / 2048 = %15,38 -> 1920×0,1538
        // = 295 birim. `m_001`-`m_005` kareleriyle yeniden ölçülecek.
        // (Eski, yanlış gerekçe aşağıda bırakıldı:)
        // Eski değer "ekran YÜKSEKLİĞİNİN %13.96'sı" diye hesaplanmıştı, ama
        // kanvas GENİŞLİKLE ölçekleniyor, yani
        // karşılaştırılacak oran genişliğe göre olmalı. Referans ekranı
        // 946×2048 ve tente 315 px → genişliğin %33.3'ü. Bizimki 268/1080 =
        // %24.8'di, yani dörtte bir kısa.
        // DERS (oranı DOĞRU eksene göre al): yükseklik yüzdesi iki farklı en/boy
        // oranında aynı şeyi anlatmıyor; ölçek hangi eksenden geliyorsa oran da
        // ondan alınmalı.
        // 360 -> 400 (2026-08-22). Sıkı y sınırıyla yeniden ölçüldü:
        // referansta tentenin EN DERİN noktası 351 px / 946 = genişliğin
        // %37.1'i. Önceki ölçüm 315 px demişti (arama penceresi festonun
        // dibini kesiyordu). 0.371 × 1080 = 400.
        // ⚠ 9. TUR: 400 -> 268, YANİ EN BAŞTAKİ DEĞERE GERİ DÖNÜLDÜ.
        // Yukarıdaki iki büyütme (268->360->400) de yanlış eksene dayanıyordu.
        //
        // ÖLÇÜM (`m_001` ve `m_003`, yakalama referansın en-boyunda 886×1920;
        // tentenin EN DERİN noktası tüm sütunlar taranarak bulundu):
        //     referans %13,96 ve %13,85 (yüksekliğe)  -> ortalama %13,90
        //     bizim    %20,73
        //     0,1390 × 1920 = 267 px çizim; çizim/sabit oranı 0,995 -> 268
        // "En derin nokta 351 px" iddiası da tutmuyor: sol kenar %13,75,
        // en derin nokta %13,96 — tente neredeyse düz, feston sığ.
        // 268 -> 295 (13. tur). Yukarıdaki yorum doğru hedefi zaten yazmış:
        // "referans tente 315 px / 2048 = %15,38 -> 1920x0,1538" = 295. Ama
        // konan sayı 268 ve gerekçesi bir alt satırda: "tente 315 px ->
        // GENİŞLİĞİN %33,3'ü, bizimki 268/1080". Yani hedef yükseklikten
        // hesaplanmış, sabit ise GENİŞLİKTEN — iki farklı en-boydaki ekranda
        // aynı şeyi anlatmıyorlar.
        //
        // Ölçüm: referans tente ekran yüksekliğinin %15,4'ü, bizimki %13,9.
        //
        // DERS (bu projede tekrarlayan tuzak): kanvas YÜKSEKLİĞE göre
        // ölçekleniyor; bir ölçüyü genişliğe oranlamak sessizce %10 hata
        // veriyor. Yorumda doğru sayı dururken yanlışı yazılmış olması da
        // ayrı bir uyarı — hesabı yapıp sabiti yazmamak kolay.
        const float AwningH   = 295f;
        // Tentenin hemen altı: referansta içerik festona neredeyse değiyor.
        const float PadTop    = 4f;
        // BÖLÜM BAŞLIĞI YÜKSEKLİĞİ 104 → 122 (2026-08-22).
        // Ölçüm: başlığın altın üst ve alt kenarları satır kümesi olarak
        // tespit edildi (yüzün içindeki koyu bant "sıcak renk" koşusunu
        // kırdığı için doğrudan taramak çalışmıyordu):
        //     referans altın kümeler 318-323 ve 418-424 -> başlık 107 px
        //                                                = genişliğin %11.3'ü
        //     bizim   altın kümeler 364-373 ve 458-467 -> 104 px = %9.63
        // 0.113 × 1080 = 122.
        // ⚠ 9. TUR: 122 -> 105, yine ilk değere (104) dönüş. Yukarıdaki
        // "genişliğin %11,3'ü" hesabı yanlış eksende.
        //     referans başlık %4,38 (yüksekliğe), bizim %5,10
        //     0,0438 × 1920 = 84 px çizim; çizim/sabit oranı 0,803 -> 105
        const float PillH     = 105f;
        // BÖLÜM BAŞLIĞI İLE İÇERİK ARASI (9. tur, `m_001`, yüksekliğe):
        //     referans başlık altı 193 -> kart üstü 225 = 32 px = %3,33
        //     0,0333 × 1920 = 64
        // Bizimki 54'tü (%2,81). `PackGap` ise ÖLÇÜLDÜ VE DOĞRU ÇIKTI:
        // kart adımı referans %23,33 ↔ bizim %23,39, dokunulmadı.
        const float PillGap   = 64f;
        // TEKLİF KARTI 9. TURDA ÖLÇÜLDÜ (`m_001`, kart sınırı sıcak-renk
        // bloğu olarak, oranlar YÜKSEKLİĞE):
        //     referans kart %20,62   bizim %16,77  -> bizimki %19 KÜÇÜK
        // Üç parça aynı oranla büyütüldü (bant/toplam payı zaten doğruydu:
        // referans 64/225 = 0,284, bizim 140/506 = 0,277):
        //     506 × (20,62/16,77) = 622
        const float OfferArtH = 418f;
        const float OfferBandH = 172f;
        const float OfferLipH = 32f;
        // Karusel noktaları: referansta nokta çapı 14 px = %1,46 -> 28 birim.
        // Nokta satırın 0,30-0,70 aralığını kapladığına göre satır 28/0,40 = 70.
        const float DotsH     = 70f;
        // KREM ALAN 330 → 290 (2026-08-22). ÖLÇÜM, referans ve bizim
        // yakalamamıza AYNI kod uygulanarak (krem maskesi ile kart kutusu,
        // sonra kart-göreli oranlar):
        //
        //     referans kart 885×268  -> en/boy 3.30
        //     bizim    kart 1002×346 -> en/boy 2.90
        //
        // Yardımcı ikon şeridinin yüksekliği İKİSİNDE DE 0.448 çıktı — yani
        // ikonlar karta göre doğru orandaydı. Küçük görünmelerinin sebebi
        // kartın kendisinin fazla yüksek olmasıydı: fazladan dikey boşluk
        // genişliğe kıyasla her şeyi küçültüyordu. 330 × (2.90/3.30) = 290.
        // ⚠ 9. TUR, EKSEN DÜZELTMESİ. Yukarıdaki "en/boy" hesabı iki farklı
        // en-boy oranındaki ekranda aynı şeyi anlatmıyor: kartın genişliği
        // ekran GENİŞLİĞİNE, yüksekliği ekran YÜKSEKLİĞİNE oranlı olduğu için
        // "en/boy"u eşitlemek eksenleri karıştırıyor.
        //
        // ÖLÇÜM (`m_001`, kartın sol kenarına yakın sütundan dikey tarama —
        // içerik oraya uzanmıyor; oranlar YÜKSEKLİĞE):
        //     referans  krem %13,02   bant %6,77   toplam %19,79
        //     bizim     krem %15,68   bant %5,78   toplam %21,46
        // Yani krem fazla UZUN, bant ise fazla KISAYDI — ikisi birbirini
        // kısmen gizlediği için toplamda fark küçük görünüyordu.
        //     krem: 0,1302 × 1920 = 250 px çizim; çizim/sabit 1,038 -> 241
        //     bant: 0,0677 × 1920 = 130 px çizim; çizim/sabit 0,673 -> 193
        const float PackCreamH = 241f;
        // İNCE AYAR, İKİ ÖLÇÜM NOKTASIYLA. Tek orandan tahmin iki kez şaştı
        // (0,673 sonra 0,829), çünkü bandın ölçülen yüksekliği kartlar arası
        // boşluktan etkileniyor. İki sabit ölçülüp doğrusal ilişki kuruldu:
        //     193 -> %8,33      157 -> %7,45      129 -> %5,99
        // Üç nokta ilişkinin DOĞRUSAL OLMADIĞINI gösterdi (ilk aralıkta
        // 36 birim 0,88 puan, ikincisinde 28 birim 1,46 puan). Hedef %6,77
        // iki alt nokta arasında; ara değer alındı:
        //     129 + 0,534 × 28 = 144
        const float PackBandH = 144f;
        const float PackGap   = 64f;
        // JETON KUTUCUKLARI REFERANSTAN (10. tur, `m_004`, oranlar YÜKSEKLİĞE).
        //
        // Yeşil fiyat düğmeleri çapa alındı (iki satır, güvenilir): satır adımı
        // 197 px = %20,52 -> bizim tuvalde 394 birim.
        // Krem alan kutucuğun sol kenarına yakın sütundan (jeton görselinin
        // uzanmadığı yer) ölçüldü: 118 px -> 236 birim.
        //     krem 236  +  taban 122  +  boşluk 36  =  394  ✔
        // Eskisi 320 + 176 + 50 = 546 birim, yani %39 fazla uzundu.
        const float TileCreamH = 236f;
        // KIRMIZI TABAN 132 → 176 (7. tur, N53). Kullanıcı: "en alttaki
        // kırmızı kısım çok küçük kalmış, alan bir tık aşağıya genişletilecek
        // (fiyat butonları çok büyük görünmesin)."
        //
        // ÖLÇÜLDÜ (kendi yakalamamız, 1080×1920): kremin dibi 592, kutunun
        // dibi 710 → taban 118 piksel; yeşil düğme 605-695 arası, yani 90
        // piksel. Kırmızıdan geriye düğmenin çevresinde 8-13 piksellik bir
        // çerçeve kalıyordu ve göz onu "taban" değil "kenarlık" okuyordu.
        // Taban 176'ya çıkarken düğme 90 birimde bırakıldı: aynı düğme, artık
        // altında ve üstünde nefes payı olan bir tabanın üstünde duruyor.
        // ⚠ 176 -> 122. Yukarıdaki "132 -> 176" büyütmesi de yanlış eksene
        // dayanıyordu; referans 61 px = 122 birim, yani İLK değere (132) çok
        // yakın. Kullanıcının "kırmızı kısım küçük kalmış" bulgusu gerçekti
        // ama sebebi taban değil, KREM ALANIN fazla uzun olmasıydı — oran
        // referansta 118/61 = 1,93, bizde 320/176 = 1,82 ile zaten yakındı.
        const float TileBaseH = 122f;
        const float TileGap   = 36f;   // referans 18 px = 36 birim
        const float RestoreH  = 110f;
        const float SectionEnd = 46f;

        const float MarginX   = 0.036f;

        /// <summary>
        /// İlk ve son bölüm zemininin içeriğin dışına taştığı mesafe.
        ///
        /// Esnek kaydırma parmakla en fazla görünür alanın kabaca yarısı kadar
        /// çekilebiliyor; 1920 birimlik tuvalde 700 birim, o payın belirgin
        /// biçimde üstünde. Fazlası bedava: taşan zemin hiçbir zaman ölçüye
        /// girmiyor, yalnız maskenin dışında bekliyor.
        /// </summary>
        // TAŞMA PAYI 700'DEN 260'A İNDİ (8. tur).
        //
        // Bu pay, esnek kaydırmada içeriğin uçlarda açılmasıyla ortaya çıkan
        // boşluğu zeminle doldurmak için vardı ve 700 birim seçilmişti çünkü
        // taşmanın ne kadar olacağı BİLİNMİYORDU. Artık taşma 160 birimle
        // sınırlı (bkz. UiScrollOvershoot), yani 260 fazlasıyla yetiyor.
        //
        // DERS (bilinmeyeni bol payla kapatmak, bilinmezliği KORUR): 700
        // birimlik zemin, açılmanın kendisini de gizliyordu — kullanıcı
        // "çok geliyor" derken gördüğü şey tam olarak o paydı.
        const float OverscrollPad = 260f;

        /// <summary>
        /// İçeriğin SONUNA eklenen boşluk: en aşağı kaydırıldığında son öğe
        /// (Geri Yükle düğmesi) alt sekme çubuğunun ÜSTÜNDE kalsın.
        ///
        /// DERS (görünür alanı büyütmek, içeriği görünür yapmaz): Kaydırma
        /// alanı bilerek ekranın 210 birim ALTINA uzatılıyor — bölüm zemini
        /// çubuğun altına kadar sürsün ve arada manzara sızmasın diye. Ama
        /// içeriğin dibi de o alanın dibine hizalandığı için son öğe aynı
        /// 210 birim + çubuk yüksekliği kadar aşağıda kalıyordu.
        /// ÖLÇÜLDÜ: en alta kaydırıldığında "Restore Purchases" düğmesi
        /// y[62..172] aralığındaydı; çubuk 0..165'i, seçili sekme kartı ise
        /// 250'ye kadarını kaplıyor. Yani düğme neredeyse tamamen çubuğun
        /// arkasındaydı — kullanıcının "Restore Purch bizde gözükmüyor"
        /// bulgusu.
        ///
        /// 330 = eski 70 birimlik nefes payı + 260 birimlik çubuk payı.
        /// </summary>
        const float TabBarClearance = 330f;

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

            // TAŞMA SINIRLANDI (8. tur). Unity'nin esnek kipinde taşma
            // mesafesi ayarlanamıyor; uzun bir sürüklemede liste ekran boyu
            // kadar açılıp bölüm zeminlerinin sonunu geçiyordu.
            scroll.gameObject.AddComponent<GameKit.UI.UiScrollOvershoot>().Limit = 160f;

            // Taşıyıcı dıştaki listeyi ancak o kurulduktan sonra tanıyabilir:
            // dikey jestleri ona AKTARACAK.
            if (screen._carousel != null)
                screen._carousel.Bind(scroll, screen._carouselPages, screen._carouselDots);

            // TENTE LEVHASI VE GÖLGESİ İÇERİKTEN ÇIKARILDI (8. tur).
            //
            // Kullanıcı: "yukarıda basılı tutup çektiğimizde çok fazla
            // geldiği için zemin kopuyor… bir gölge bugu var, o gözükmesin."
            //
            // İkisi de `content` altındaydı, yani içerikle birlikte
            // KAYIYORDU. Aşağı doğru esnetildiğinde koyu lacivert levha
            // tentenin altından çıkıp koca bir bant hâline geliyor, altındaki
            // 34 birimlik gölge de ayrı bir şerit gibi görünüyordu — ekran
            // görüntülerindeki "gölge bugu" tam olarak bu.
            //
            // Artık `root` altındalar ve tenteden ÖNCE kuruluyorlar: çizim
            // sırası içerik → levha → gölge → tente. Levha festonun
            // çentiklerini arkadan kapatmaya devam ediyor, gölge de tentenin
            // içeriğe düşen gölgesi olarak yerinde duruyor. Hiçbiri artık
            // kıpırdamıyor, dolayısıyla ortaya çıkamıyor.
            //
            // DERS (bir öğe neyle birlikte HAREKET ETMELİ?): "İçeriğin
            // parçası" ile "başlığın parçası" arasındaki fark, kaydırınca
            // ortaya çıkıyor. Bu levha görsel olarak tenteye ait; içerikte
            // durmasının tek sebebi orada kurulmuş olmasıydı.

            // Tente EN SON kurulur: kardeş sırası çizim sırasıdır, içeriğin
            // üstünde kalması gereken tek şey o.
            screen.BuildAwning(root);
            return root;
        }

        // ---- Tente ---------------------------------------------------------

        // TENTENİN ARKASINDAKİ LEVHA VE GÖLGESİ KALDIRILDI.
        // <summary>
        // Tentenin arkasındaki koyu lacivert levha ve altındaki yumuşak
        // gölge — BAŞLIĞIN parçası, içeriğin değil (8. turda taşındı).
        //
        // Levha festonun çentiklerini arkadan kapatıyor, gölge de tentenin
        // içeriğe düşen izini veriyor. İkisi de sabit: içerik altlarından
        // akıp gidiyor.
        // </summary>
            // LEVHA DA KALDIRILDI (11. tur).
            //
            // Kullanıcı: "mağazada brandanın altında düz mavi arka plan
            // geliyor, onu kaldır."
            //
            // Levha tentenin festonlarını ARKADAN kapatmak için vardı ve
            // `#062387` düz bir dikdörtgendi. Tentenin kendi dokusu zaten
            // opak; kapattığı tek şey festonların arasındaki çentiklerdi ve
            // ekranda göze çarpan şey o çentikler değil, levhanın kendisi
            // oldu — tentenin altında duran düz mavi bir bant.
            //
            // DERS (arkadaki katman, önündeki KADAR görünür): "Yalnız
            // çentiklerden görünür" diye eklenen bir levha, çentiklerden
            // görünenden fazlasını gösteriyorsa artık arka plan değil,
            // ekranın bir öğesidir.
            // TENTENİN GÖLGESİ KALDIRILDI (8. tur).
            //
            // Kullanıcı: "şurada gölge var, Special Offers'ın üstünde
            // kalıyor ama alakası yok, normalde olmaması gereken bir şey —
            // o mavi kısmın gölgesi, onu kaldıralım."
            //
            // 34 birimlik bu koyu düşüş, levhanın alt kenarını yumuşatmak
            // için konmuştu. Ama levha zaten tentenin ARKASINDA; görünen tek
            // yeri festonun çentikleri. Gölge ise tentenin altından taşıp
            // turuncu "Special Offers" kurdelesinin üstüne düşüyordu — yani
            // yumuşattığı kenar görünmüyor, kendisi görünüyordu.
            //
            // Referansta tentenin altında gölge yok: feston doğrudan içeriğe
            // değiyor.
            //
            // DERS (bir öğe neyi çözdüğünü GÖSTEREBİLMELİ): "Sert kesimi
            // yumuşatsın" diye eklenen bir katman, yumuşattığı kesimin
            // görünmediği bir yerde duruyorsa yalnız kendi varlığını
            // gösteriyordur.
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
            // Renk artık KESİLEN tentenin üst satırından: (0,97,230).
            // Eski değer (1,111,195) prosedürel tentenin taban renginden
            // hesaplanmıştı; görsele geçince ikisi arasında görünür bir
            // dikiş kalırdı.
            bleedImage.color = new Color(0f, 0.380f, 0.902f);
            bleedImage.raycastTarget = false;

            // TENTENIN ALTINDAKI KOYU BANT (4. tur, E15/E16).
            //
            // Kullanıcı: "Üstteki brandanın bitiş kısmında orijinalde
            // çizgi/gölge var, 3D görünüm veriyor — bizde yok" ve
            // "Brandanın hemen altındaki mavi alan... geçiş orijinaline
            // benzetilecek."
            //
            // ÖLÇÜM (`market.jpeg`, 946×2048, iki dikey tarama):
            //   feston ORTASINDA  y 250-316  #062387
            //   festonlar ARASI   y 290-316  #062387
            //   y 390+            kahverengi duvar #5F1B0C
            // Yani tentenin arkasında koyu lacivert bir levha var: feston
            // çentiklerinin içinden o görünüyor ve en derin çentikten ~40
            // piksel daha aşağı uzanıyor. Kahverengi duvar ancak ondan sonra
            // başlıyor.
            //
            // DERS (gölge, iki yüzey ARASINDAKI boşluğu anlatır): Bizde
            // tentenin festonu doğrudan kahverengi duvara değiyordu; iki
            // yüzey aynı düzlemdeymiş gibi okunuyor ve tente "duvara
            // çizilmiş" görünüyordu. Araya koyu bir levha koymak tenteyi
            // duvardan AYIRIYOR — 3B hissi mesafeden geliyor, gölgenin
            // kendisinden değil.
            // ÖLÇÜLDÜ VE KISALDI (5. tur; kullanıcı: "brandanın altındaki mavi
            // yer aşağıya kaydırınca bizimle beraber geliyor, orijinalde o
            // yer sabit").
            //
            // İki kare (`menus,powerups,vs.mp4` 10. ve 12. saniye) farklandı:
            // referansta içeriğin kaydığı ilk satır ekranın **%13,5'i**.
            // Bizde %16,4 idi — yani tentenin festonları bittikten sonra 46
            // birimlik DÜZ bir lacivert bant daha vardı ve içerik ancak ondan
            // sonra başlıyordu. Referansta öyle bir bant yok: festonun
            // kenarı koyulaşıyor ve hemen içerik geliyor.
            //
            // DERS (sabit bir katman da yanlış YERDE olabilir): Bant zaten
            // kaydırılmıyordu, yani "sabit mi" sorusunun cevabı doğruydu;
            // yanlış olan KALINLIĞIYDI. Kullanıcı hareketi değil, o bandın
            // varlığını görüyordu.
            //
            // Pay yalnız festonun çentiklerini arkadan kapatacak kadar.
            // SIFIR (6. tur). Kullanıcı bandı hâlâ görüyor: "o mavi arka plan
            // sabit kalsın, aşağı kaydırdığımızda bizimle gelmesin."
            //
            // Referansta festonun altında BAND YOK — koyu kenar bitiyor ve
            // içerik başlıyor (menüler videosu 10. ve 12. saniye farkı:
            // içeriğin kaydığı ilk satır ekranın %13,5'i, tentenin dibiyle
            // aynı yer). Levha yalnız festonun çentiklerini ARKADAN kapatmak
            // için var; tentenin altına taşmasının hiçbir gerekçesi yok.
            //
            // LEVHA ARTIK İÇERİĞİN PARÇASI (7. tur, N47). Kullanıcı: "o mavi
            // alan ve gölgesi sadece başlangıçta en üstte olmalı, scroll
            // sonrası tamamen kaybolmalı (sticky olmayacak)."
            //
            // DERS (yanlış KATMANDA duran bir süs, kendini hareketle ele
            // verir): Levha altı turdur `root` altındaydı, yani kaydırma
            // alanının DIŞINDA. Kaydırmayan bir tuvalde bunun görünür bir
            // sonucu yok; parmak listeyi yukarı ittiği anda levha kartların
            // üstünde asılı kalıyor ve "bizimle geliyor" gibi okunuyor.
            // Bir katmanın sabit mi akan mı olduğunu, DURDUĞU yere değil,
            // ekran hareket ederken ne yaptığına bakarak seçmek gerekiyor.

            var bar = Row("Awning", root, 0f, AwningH);

            // TENTE ARTIK ORİJİNALİN KENDİSİ (13. tur — kullanıcı: *"mağaza
            // tentesi çok kötü olmuş, yeniden yap, aynısını yap, gerekirse
            // direkt oyundan al"*).
            //
            // Prosedürel tente (`MenuSprites.Awning`) ÜÇ turdur yaklaşamadı
            // ve her turda başka bir sayı ölçülüp değiştirildi (feston
            // derinliği %14 -> %21,6 -> %10,3 -> %19,2). Son turda gerçek
            // biçim de yanlış anlaşılmıştı: referansı 2 kat büyütünce
            // görüldü ki yarım daire "feston" yok — alt köşeleri
            // YUVARLATILMIŞ GENİŞ DİKEY ŞERİTLER var, hepsi aynı hizada
            // bitiyor.
            //
            // `market.jpeg`ten kesildi: periyot 268 piksel (iki şerit),
            // tentenin dibi y=316. Üstündeki jeton kapsülü ve "Mağaza"
            // başlığı, TEMİZ BİR PERİYODUN (x 658..926) aynı fazla
            // döşenmesiyle silindi. Yani geometri referansın kendisi;
            // yalnız kaplayan öğeler kaldırıldı.
            //
            // DERS (üç tur yaklaşamayan bir çizim, ÇİZİLMEMELİ): Prosedürel
            // üretim ölçülebilir biçimler için doğru araç. Referansın
            // biçimini üç turda hâlâ yanlış okuyorsam sorun sayılarda değil,
            // BİÇİMİ tarif etme yeteneğimde — o noktada kesmek hem daha
            // hızlı hem birebir. (Aynı karar logo ve kronometre için de
            // verilmişti.)
            var cloth = bar.gameObject.AddComponent<Image>();
            cloth.sprite = UiSkin.Get(Art.Awning) ?? MenuSprites.Awning;
            cloth.type = Image.Type.Simple;
            cloth.raycastTarget = true;      // tenteye dokunmak içeriği kaydırmasın

            // Jeton göstergesi: krem kapsül + SOL UCUNDAN taşan jeton.
            // Ölçü referanstan (`market.jpeg`): kapsül X 0.112-0.338,
            // jeton X 0.036-0.123 — yani jeton kapsülün soluna taşıyor,
            // kapsülün altında başlamıyor. Bizimki 0.055'ten başlıyordu ve
            // jetonun tamamı kapsülün üstünde kalıyordu (12. APK bulgusu).
            // JETON KAPSÜLÜ (5. tur, kullanıcı: "gold yerinin o arka planı,
            // dış çizgisi daha fazla ve gölgeli gözükmeli, ayrıca ufak bir
            // radiusu daha fazla; bununla beraber coin ikonu o arka planının
            // BAŞLANGIÇ NOKTASINDA").
            //
            // Üç katman: koyu gölge (bir tık aşağıda), koyu kenar, krem yüz.
            // Tek katmanlı krem kapsül tentenin mavisinde "kesilmiş kâğıt"
            // gibi duruyordu; kenar ve gölge onu yüzeyden KALDIRIYOR.
            // X1 YENİDEN ÖLÇÜLDÜ (2026-08-22): yukarıdaki yorum referans için
            // 0.338 diyor, ama aynı `market.jpeg` üzerinde krem kapsülün
            // bitişik koşusu taranınca sağ kenar 0.288 çıkıyor (kapsül
            // X 0.118-0.288). Referansta üç haneli "142" varken bile o kadar;
            // bizimki tek haneli "0" ile 0.344'e kadar uzuyordu.
            // Ölçüm yöntemi: satır satır soldan ilk bitişik krem koşusu —
            // elle kutu seçmek bu turda üç kez yanlış sonuç verdi.
            // PillX0 0,088 -> 0,069: ölçüm, referansın sol kenarı %11,9,
            // bizimki %13,8 — kapsül 0,019 sağda başlıyordu.
            const float PillX0 = 0.069f, PillX1 = 0.296f;

            // TEK PROSEDÜREL YÜZEY (13. tur, M1a+M1b).
            //
            // Üç düz katman vardı: gölge + koyu kenar + krem yüz. İki kusuru
            // birden taşıyordu ve ikisi de ölçüldü:
            //
            //   DOLGU  referans (245,248,255) -> (224,226,249)  SOĞUK BEYAZ,
            //          bizim    (255,249,236) DÜZ               sıcak krem
            //   KENAR  referans (4,5,70)   koyu LACİVERT
            //          bizim    (35,19,9)  neredeyse SİYAH-KAHVE
            //
            // Kullanıcının *"çok düz"* dediği şey birincisi, *"dış rengi
            // siyah, kontürü çok"* dediği şey ikincisi. Kahveye çalan siyah
            // bir kenar tentenin mavisinde yabancı duruyordu; referansın
            // laciverti aynı renk ailesinden.
            //
            // Gölge katmanı da kalktı: `UiSprites.CoinPad` kenarı zaten
            // kapsülün içine çiziyor, ayrıca gölge koymak konturu
            // kalınlaştırıyordu — kullanıcının şikâyetinin bir kısmı buydu.
            //
            // Yükseklik 45 -> 58 birim (referans 60): kutu 0,50-0,70 yerine
            // 0,46-0,72.
            var pill = UiKit.CreateIcon("CoinPill", bar, UiSprites.CoinPad);
            pill.type = Image.Type.Simple;
            pill.preserveAspect = false;
            pill.raycastTarget = false;
            UiKit.Place(pill, PillX0, 0.46f, PillX1, 0.72f);

            // Rakam jetonun SAĞINDA: jeton artık kapsülün başlangıcında
            // duruyor, yazı da ona göre kaydı.
            // Punto 40 -> 52: kapsül 45'ten 58 birime çıktı, yazı da onunla
            // büyümeliydi. Referansta rakam kapsül yüksekliğinin ~%60'ı.
            _coinLabel = UiKit.CreateLabel("Coins", pill.transform, "0", 52, CoinInk);
            UiKit.Place(_coinLabel, 0.30f, 0.04f, 0.95f, 0.96f);

            // Jeton kapsülün BAŞLANGIÇ noktasında ve ondan büyük: sol kenarı
            // kapsülün sol kenarıyla aynı hizada başlayıp yukarı-aşağı taşıyor.
            // JETON x1,48. Ölçüm: referansta jeton 83 birim genişliğinde
            // (x %4,3..%12,0), bizimki 56. Referansta jeton kapsülden
            // belirgin biçimde TAŞIYOR — kapsülün soluna da, üstüne ve
            // altına da. Bizimki kapsülün içine sığıyordu, yani "kapsülün
            // başlangıcındaki jeton" değil "kapsüle konmuş jeton" gibi
            // duruyordu.
            var coin = UiKit.CreateIcon("Coin", bar, UiSkin.Get(Art.Coin));
            coin.preserveAspect = true;
            UiKit.Place(coin, PillX0 - 0.042f, 0.395f, PillX0 + 0.126f, 0.805f);

            // Başlık ekranın ortasında DEĞİL: referansta merkezi 0.524'te,
            // yani jeton kapsülünün sağında kalan alanın ortasında. Bizimki
            // 0.575'teydi — sağa kaçmış görünüyordu.
            // Punto referanstan: "M" harfinin yüksekliği ekranın %3.37'si;
            // 84 punto %3.15 veriyordu.
            // PUNTO 90 → 128 (2026-08-22). Eski gerekçe "M harfinin yüksekliği
            // ekranın %3.37'si" idi; ama ekran yüzdesi iki farklı en/boy
            // oranında aynı şeyi anlatmıyor. TENTEYE oranlayınca:
            //     referans başlık 90 px / tente 315 px = %28.6
            //     bizim    başlık 72 px / tente 357 px = %20.2
            // 1.42 kat büyütüldü. Kutu da 0.32 → 0.40'a açıldı, yoksa
            // UiTextFit puntoyu geri kısardı.
            // PUNTO VE KONUM ÖLÇÜLDÜ (13. tur, M2d — kullanıcı: *"tentede
            // textin olduğu yer de çok doğru, bizde yukarıda kaçıyor text;
            // o textin yeri boyutu da iyi ayarlanmalı"*).
            //
            //     baslik kapak yuksekligi   referans  84 br   bizim 103
            //     merkezi TENTE icinde      referans %43,2    bizim %57,9
            // Yani hem %23 büyüktü hem de tentenin üst yarısına kaçmıştı.
            var title = UiKit.CreateTitle("Title", bar, "Shop", 104,
                new Color(1f, 0.99f, 0.96f), TitleShade);
            UiKit.Place(title, 0.290f, 0.232f, 0.760f, 0.632f);

            // KONTUR — Yolculuk başlığındaki aynı tuzak burada da vardı:
            // `CreateTitle`'a verilen kontur rengi paylaşılan materyal
            // yüzünden sessizce yok sayılıyor, başlık düz beyaz kalıyor.
            // Referansta kalın lacivert kontur var (#0A0F55, örneklendi) ve
            // başlığa "baskılı" görünümünü veren şey o.
            // KABARTMA BURAYA UYGULANMAZ (12. tur, kullanıcı kararı):
            // *"shop kısmında o outline kabartma yok, onu kaldır shop
            // kısmından."* Referansta tentenin üstündeki başlık ince konturlu;
            // kalın mor hale menü bantlarına ait bir muamele, mağazaya değil.
            //
            // DERS (ortak bir stil, HER YERE uygulanmaz): "Nerede varsa
            // kullanalım" talimatını "her başlığa uygula" diye okudum ve
            // mağazayı da kattım. Ortak stilin sınırı da stilin parçası.
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
            // İLK bölüm yukarı taşar: üstten çekilen esnek kaydırmada göz
            // içeriğin başladığı yeri görmesin (bkz. Background yorumu).
            Background(content, sectionTop, y - sectionTop, BgOffers, "BgOffers",
                       padTop: OverscrollPad);

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
            y += RestoreH + TabBarClearance;
            // SON bölüm aşağı taşar — aynı gerekçe, ters uç.
            Background(content, sectionTop, y - sectionTop, BgCoins, "BgCoins",
                       padBottom: OverscrollPad);

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
        /// <summary>
        /// Bir bölümün döşemeli zemini.
        ///
        /// <paramref name="padTop"/> / <paramref name="padBottom"/>: zemini
        /// bölümün DIŞINA taşırma payı.
        ///
        /// DERS (esnek kaydırmada içeriğin SINIRI görünür): Bu ekranda zemin
        /// ekranın değil, KAYDIRILAN İÇERİĞİN parçası — bölüm renkleri birbirine
        /// kayarak geçsin diye böyle (referansta da öyle). Bedeli şu: liste
        /// esnek (`MovementType.Elastic`) olduğu için parmakla uca dayanıp
        /// çekince içerik kendi sınırının ötesine gidiyor ve arkasındaki boşluk
        /// ortaya çıkıyor. Kullanıcı "basılı tutup kaldırdığımızda arka plandaki
        /// zemin çıkıyor, boşluğu görebiliyoruz" derken bunu gördü.
        ///
        /// Esnekliği kapatmak (Clamped) yanlış çözüm olurdu: referansta liste
        /// AYNI ŞEKİLDE esniyor, yalnız boşluk görünmüyor. Doğru çözüm, ilk ve
        /// son bölümün zeminini taşma mesafesinden DAHA UZAĞA uzatmak — göz
        /// hiçbir zaman içeriğin bittiği yeri görmüyor.
        /// </summary>
        static void Background(Transform content, float top, float height, Color color,
                               string name, float padTop = 0f, float padBottom = 0f)
        {
            top -= padTop;
            height += padTop + padBottom;

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

        /// <summary>
        /// Bölüm başlığı: kurdele.
        ///
        /// Kullanıcı (5. tur): "packs ve special offers kısmının şeritleri var
        /// bitiş sınırı gibi orijinalinde, onu da yapalım."
        ///
        /// ÖLÇÜM (`menus,powerups,vs.mp4` 07. saniye): başlık düz bir kapsül
        /// değil KURDELE — sağ ucundan, kapsülün dışına taşan küçük bir
        /// dil çıkıyor ve o dilin kendi koyu gölgesi var. Kitap ayracı gibi:
        /// "bu bölüm burada bitiyor" diyen şey o dil.
        ///
        /// DERS (bir çizgi de bilgi taşır): Kapsül tek başına bir etiket;
        /// üzerine oturan dil onu bir BÖLÜM SINIRI yapıyor. Aynı yazı, aynı
        /// renk — fark yalnız o küçük çıkıntıda.
        /// </summary>
        static void SectionPill(Transform content, float top, string text, Color face,
                                Color? rimColor = null)
        {
            var row = Row("Pill_" + text, content, top, PillH, MarginX, 1f - MarginX);

            var rimTone = rimColor ?? PillRim;

            // KURDELENİN DİLİ — İKİ UÇTA, İÇİ DOLU, ALTIN (7. tur, N50/N51).
            //
            // Kullanıcı: "Sağdaki şerit: içi dolu, bir tık daha ince ve sarı
            // olacak. Aynı şerit solda da olacak (simetrik)." ve Paketler
            // için: "şerit tek yerde var, çift olacak. Rengi mor yapılmış,
            // dış renkle (sarı) düzeltilecek."
            //
            // Eski dil iki katmanlıydı: dışta kapsülün kontur rengi, içinde
            // BÖLÜM RENGİNİN koyusu. Yani her bölümde başka renkteydi —
            // Paketler'de mor, Teklifler'de kızıl. Referansta dil bir kumaş
            // parçası değil, kurdelenin kendisi: tek renk, kapsülün altın
            // konturuyla aynı. Renk bölüme göre değişmemeli, çünkü anlattığı
            // şey bölüm değil "bu bir kurdele".
            //
            // DERS (simetri, bir yanı kopyalamakla kurulmaz): Sağdaki dil
            // 0.93→1.045 aralığındaydı; solda aynı görüntüyü almak için
            // aralığı AYNALAMAK gerekiyor (1-x), yoksa dil ekranın içine
            // doğru büyür. `for` içindeki `mirror` çarpanı bunu yapıyor.
            const float TailIn = 0.930f, TailOut = 1.045f;
            for (int side = 0; side < 2; side++)
            {
                bool right = side == 0;
                float x0 = right ? TailIn  : 1f - TailOut;
                float x1 = right ? TailOut : 1f - TailIn;

                var tail = UiKit.CreateRect(right ? "TailRight" : "TailLeft", row);
                // İNCE: eski 0.16-0.84 (yüksekliğin %68'i) yerine %52.
                tail.anchorMin = new Vector2(x0, 0.24f);
                tail.anchorMax = new Vector2(x1, 0.76f);
                tail.offsetMin = Vector2.zero;
                tail.offsetMax = Vector2.zero;
                var tailImage = tail.gameObject.AddComponent<Image>();
                tailImage.color = PillRimGold;
                tailImage.raycastTarget = false;
            }

            // GÖVDE TEK PROSEDÜREL YÜZEY (13. tur, M3a).
            //
            // İki düz kapsül vardı: altın kontur + turuncu yüz (%10 içeride).
            // Referansın dikey kesiti ALTI bantlı ve kritik olan şu: altın
            // çerçeveyle turuncu gövde ARASINDA koyu kırmızı bir iç kenar
            // (95,0,0) var. O olmayınca altın doğrudan turuncuya değiyor,
            // ikisi tek yüzey gibi okunuyor ve kurdele "çerçevesiz"
            // duruyordu. Gövdenin kendisi de gradyanlı: (255,136,62) ->
            // (205,49,0).
            //
            // 9-dilim: profil yalnız dikey, o yüzden orta yatayda güvenle
            // esniyor; yuvarlak uçlar kenar payında korunuyor.
            var rim = UiKit.CreateIcon("Rim", row, UiSprites.SectionRibbon);
            rim.type = Image.Type.Sliced;
            rim.preserveAspect = false;
            rim.raycastTarget = false;
            UiKit.Place(rim, 0f, 0f, 1f, 1f);

            // PUNTO 56 → 70 (2026-08-22). Yazının yüksekliği ŞERİDE oranlandı:
            //     referans 52 px / 107 px şerit = %48.6
            //     bizim    48 px / 122 px şerit = %39.3
            // ×1.24. Şerit bu turda 104'ten 122'ye çıktığı için yazı ONUNLA
            // BİRLİKTE büyümemişti — sabit punto, şerit büyüyünce oransal
            // olarak küçüldü. (Punto ile kutu birlikte düşünülmeli.)
            var label = UiKit.CreateTitle("Label", row, text, 70,
                new Color(1f, 0.99f, 0.96f), new Color(0.24f, 0.05f, 0.02f));
            UiKit.Place(label, 0.06f, 0.04f, 0.94f, 0.96f);
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

            // ALTIN ÇERÇEVE GERİ ALINDI (2026-08-22, aynı gün).
            //
            // `market.jpeg`'te teklif kartının çevresi kalın altın çerçeveydi
            // ve bunu BÜTÜN teklif kartlarına uygulamıştım. YANLIŞTI:
            // videodan çıkarılan yeni bir karede ("Başlangıç Paketi", yani
            // bizim Starter Pack'in ta kendisi) kartın altın çerçevesi YOK —
            // düz turuncu, yuvarlak köşeli.
            //
            // `market.jpeg`'teki çerçeveli kart "Blok Bileti" adlı PREMIUM
            // paketti. Yani altın çerçeve teklif kartının değil, o özel
            // paketin muamelesi.
            //
            // DERS (tek örnekten kural çıkarma): bir referans karesinde
            // görülen bir muameleyi "bu kart tipinin dili" sanmak, o karenin
            // hangi İÇERİĞİ gösterdiğine bakmamaktan geliyor. İkinci bir
            // örnek görene kadar genelleme yapılmamalı.
            var inner = UiKit.CreateRect("Inner", card);
            UiKit.Place(inner, 0f, 0f, 1f, 1f);

            // Alt kalınlık + isim bandı tek panelde; üstüne turuncu sanat alanı.
            var body = UiKit.CreateRoundedPanel("Body", inner, Darken(OfferBand, 0.72f));
            UiKit.Place(body, 0f, 0f, 1f, 1f);

            var band = UiKit.CreateRoundedPanel("Band", inner, OfferBand);
            UiKit.Place(band, 0f, OfferLipH / height, 1f, (OfferLipH + OfferBandH) / height);

            // Dikey degrade: alta doğru koyulaşan turuncu.
            //
            // Eskiden yüzeyin üstüne ikinci bir DİKDÖRTGEN konuyordu ve o
            // dikdörtgen kartın yuvarlak köşesini kesiyordu — köşelerde düz
            // bir turuncu kenar görünüyordu. Geçiş artık yüzeyin kendi köşe
            // noktalarının rengi, dolayısıyla silüet neyse o (bkz. 8. tur).
            var art = UiKit.CreateRoundedPanel("Art", inner, Color.white);
            UiKit.Place(art, 0f, (OfferLipH + OfferBandH) / height - 0.04f, 1f, 1f);
            art.gameObject.AddComponent<GameKit.UI.UiVerticalTint>()
               .Set(OfferTop, OfferLow);

            var pile = UiKit.CreateIcon("Pile", art.transform,
                Tiered(Art.CoinPile, offer.Pile, Art.Coin));
            UiKit.Place(pile, 0.06f, 0.14f, 0.46f, 0.98f);

            var amount = UiKit.CreateTitle("Amount", art.transform, Amount(offer.Coins), 72,
                new Color(1f, 1f, 1f), TitleShade);
            UiKit.Place(amount, 0.16f, 0.04f, 0.56f, 0.34f);

            RewardIcons(art.transform, showNoAds: false, offer.Hours, offer.Helpers);

            // İndirim flaması — sol üst köşeden aşağı sarkar.
            //
            // DIŞ ÇİZGİ EKLENDİ (4. tur, E14): Kullanıcı "%90 OFF indirim
            // şeriti için de outline lı yeni görsel" dedi. Referansta
            // (`market.jpeg`) mağazadaki her rozet/plaka kalın bir dış
            // çizgiyle çevrili — "Blok Bileti" ve "499,99 TL" plakalarının
            // ikisinde de altın kontur var. Bizim flamamız düz kırmızıydı ve
            // altındaki sarı kartın üstünde sınırsız kalıyordu.
            //
            // DERS (kontur ayrı bir GÖRSEL gerektirmez): Aynı flama, biraz
            // büyük ve koyu renkte, arkaya konuyor. Yeni bir sprite üretmek
            // yerine katman eklemek her boyutta doğru kalınlığı da beraberinde
            // getiriyor.
            var badge = UiKit.CreateRect("Badge", art.transform);
            UiKit.Place(badge, 0.025f, 0.52f, 0.175f, 1.10f);

            var flagRim = UiKit.CreateRect("Rim", badge);
            UiKit.Place(flagRim, -0.10f, -0.045f, 1.10f, 1.03f);
            var rimImage = flagRim.gameObject.AddComponent<Image>();
            rimImage.sprite = MenuSprites.Pennant;
            rimImage.color = new Color(0.290f, 0.020f, 0.055f);
            rimImage.raycastTarget = false;

            var flagFace = UiKit.CreateRect("Face", badge);
            UiKit.Place(flagFace, 0f, 0f, 1f, 1f);
            var flag = flagFace.gameObject.AddComponent<Image>();
            flag.sprite = MenuSprites.Pennant;
            flag.color = BadgeRed;
            flag.raycastTarget = false;

            // YAZI FLAMANIN ORTASINDA (7. tur, N48). Kullanıcı: "bayraktaki
            // text bayrağa ortalanmalı (şu an üstte kalmış)."
            //
            // ÖLÇÜM: `MenuSprites.Pennant` 160×200 ve alt kenarında 46
            // piksellik bir V çentiği var — yani flamanın DOLU gövdesi
            // 46..200, normalde 0.23..1.00 ve ortası 0.615. Yazı bloğu
            // (0.28..0.92) ise 0.60'ta duruyordu; üstelik arkadaki kontur
            // katmanı flamayı aşağı doğru 0.045 daha uzatıyor, bu yüzden göz
            // ortayı daha da aşağıda arıyor. Blok 0.06 aşağı çekildi.
            //
            // DERS (çentikli bir biçimin ortası, kutusunun ortası değildir):
            // Flamayı bir dikdörtgen sanıp kutuya göre ortalamak, V'nin
            // yediği alanı da hesaba katmak demek — yazı hep yukarıda kalır.
            var percent = UiKit.CreateTitle("Percent", badge.transform, offer.Discount + "%", 44,
                new Color(1f, 1f, 1f), new Color(0.42f, 0.03f, 0.03f));
            UiKit.Place(percent, 0.02f, 0.46f, 0.98f, 0.86f);

            var word = UiKit.CreateTitle("Word", badge.transform, "OFF", 22,
                new Color(1f, 0.94f, 0.80f), new Color(0.42f, 0.03f, 0.03f));
            UiKit.Place(word, 0.02f, 0.22f, 0.98f, 0.46f);

            // BANDIN GÖRÜNEN YÜKSEKLİĞİ, BANDIN YÜKSEKLİĞİ DEĞİL (7. tur, N49).
            //
            // Kullanıcı: "'Starter Pack' yazısı biraz daha solda ve ortalanmış
            // olmalı. Fiyat butonu ortalanmalı (şu an çok üste yapışmış)."
            //
            // SEBEP: `art` paneli bandın üstüne 0.04 kart yüksekliği kadar
            // BİNİYOR (yuvarlak köşeler arada dikiş bırakmasın diye). 0.04 ×
            // 506 = 20 birim, bandın 140 biriminin %14'ü. Ad ve düğme bandın
            // TAMAMINA göre ortalandığı için ikisi de o payın yarısı kadar
            // yukarıda duruyordu — göze "üste yapışmış" diye geliyor.
            //
            // DERS (bir kutunun ortası, GÖRÜNEN kutunun ortasıdır): Üstünü
            // başka bir katman örtüyorsa ortalama hesabı örtülen payı
            // düşmeli. Aksi hâlde düzen "doğru" ama görüntü yanlış olur.
            const float ArtOverlap = 0.04f;                       // kart oranı
            float visible = 1f - ArtOverlap * height / OfferBandH; // band oranı

            var name = UiKit.CreateTitle("Name", band.transform, offer.Name, 58,
                new Color(1f, 0.99f, 0.96f), new Color(0.45f, 0.13f, 0.02f));
            UiKit.Place(name, 0.032f, 0.06f * visible, 0.600f, 0.94f * visible);

            var buy = PriceButton("Buy", band.transform, offer.Price, 44);
            UiKit.Place(buy, 0.64f, 0.14f * visible, 0.96f, 0.86f * visible);

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

                // BİR TIK BÜYÜK VE TAM ORTADA (7. tur, N52). Kullanıcı:
                // "kampanya değiştirme 3 nokta ikonu boyutu bir tık
                // büyütülecek ve biraz daha ortalanacak."
                //
                // Yarıçap 0.011 → 0.015 (çap 23,8 → 32,4 birim), aralık da
                // orantılı açıldı (0.035 → 0.044) — noktalar büyüyünce eski
                // aralıkta birbirine değiyordu.
                //
                // DİKEY: satır DotsH = 62 birim; 0.30-0.70 aralığı 24,8
                // birim veriyordu, yani nokta YATAYDA 23,8 DİKEYDE 24,8 —
                // `CreateIcon` en-boy koruduğu için daire yatay ölçüye göre
                // çiziliyor ve kutunun içinde 1 birim yukarıda kalıyordu.
                // Çapı kutuya eşitlemek onu gerçekten ortalıyor.
                const float radius = 0.015f;
                float half = radius * UiKit.ReferenceResolution.x / DotsH;
                float cx = 0.5f + (i - (count - 1) * 0.5f) * 0.044f;
                UiKit.Place(dot, cx - radius, 0.5f - half, cx + radius, 0.5f + half);
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
            // MOR KUTUNUN ALT GÖLGESİ (5. tur, kullanıcı: "mor kutuların da
            // alt kısmında yine gölgeler daha belirgin"). Kartın altına taşan
            // koyu bir katman: kart listede "yatıyor" değil "duruyor" olsun.
            var cardDrop = UiKit.CreateRoundedPanel("Drop", card,
                new Color(0f, 0f, 0f, 0.34f));
            UiKit.Place(cardDrop, 0.012f, -0.035f, 0.988f, 0.96f);
            cardDrop.raycastTarget = false;

            var band = UiKit.CreateRoundedPanel("Band", card, BandPurple);
            UiKit.Place(band, 0f, 0f, 1f, 1f);

            // Bandın alt kenarındaki koyu şerit: mor yüzeyin kendi kalınlığı.
            var bandLip = UiKit.CreateRoundedPanel("BandLip", card,
                new Color(BandPurple.r * 0.55f, BandPurple.g * 0.45f, BandPurple.b * 0.60f));
            UiKit.Place(bandLip, 0f, 0f, 1f, 0.055f);
            bandLip.raycastTarget = false;

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

            // PAKET ADI SOLA YASLI (5. tur, kullanıcı: "paket yazıları yine
            // biraz daha solda, dikdörtgenin başlangıcında").
            //
            // Kutu zaten solda başlıyordu ama yazı ORTALIYDI; kısa adlar
            // ("Brick Pack") kutunun ortasına kaçıyor, uzun adlar sola
            // dayanıyordu — yani kartlar arasında ad hizası oynuyordu.
            //
            // DERS (hizalama, konumdan daha çok belirler): Bir etiketi sola
            // taşımak yetmez; İÇİNDEKİ yazının da sola yaslı olması gerekir,
            // yoksa konum yalnız en uzun metin için doğru olur.
            var name = UiKit.CreateTitle("Name", band.transform, pack.Name, 58,
                new Color(1f, 0.99f, 0.96f), new Color(0.24f, 0.03f, 0.36f));
            UiKit.Place(name, 0.042f, 0.02f, 0.62f, PackBandH / height * 0.92f);
            name.alignment = TextAlignmentOptions.Left;

            // FİYAT DÜĞMESİ ORANI (2026-08-22). Yeşil dolgusu ölçüldü:
            //     referans 263x86 -> en/boy 3.06 (ekranın %27.8'i geniş, %9.09'u yüksek)
            //     bizim    319x83 -> en/boy 3.84 (%29.5 geniş, %7.69 yüksek)
            // Yani hem fazla geniş hem fazla alçaktı. Genişlik ×0.94
            // (0.33 -> 0.311 kart oranı), yükseklik ×1.18; dikey MERKEZ
            // korunarak açıldı, böylece bandın içindeki dengesi bozulmuyor.
            var buy = PriceButton("Buy", band.transform, pack.Price, 44);
            UiKit.Place(buy, 0.649f, PackBandH / height * 0.124f,
                             0.96f, PackBandH / height * 0.858f);

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
            // HAZIR FLAMA GÖRSELİ (2026-08-22). Havuzdaki `ribbon_popular`
            // altın kenarlı, gölgeli bir KÖŞE flaması; bizim prosedürel
            // şeridimiz düz pembe + koyu rim idi ve yanında sönük kalıyordu.
            // Görsel `ribbon_reward.png` yuvasına yazıldı (`Art.Ribbon` sabiti
            // vardı ama hiçbir yerden kullanılmıyordu — boş yuvaydı).
            //
            // Görsel VARSA prosedürel katmanlar hiç kurulmuyor: hazır görselin
            // üstüne kendi konturumuzu çizmek "AADSS" hatasının aynısı olurdu.
            var ribbonArt = UiSkin.Get(Art.Ribbon);
            if (ribbonArt != null)
            {
                const float size = 210f;                 // kart kremine göre ölçüldü
                var holder = UiKit.CreateRect("Ribbon", clip);
                holder.anchorMin = holder.anchorMax = new Vector2(0f, 1f);
                holder.pivot = new Vector2(0f, 1f);
                holder.sizeDelta = new Vector2(size, size * 204f / 186f);
                holder.anchoredPosition = Vector2.zero;

                var art = holder.gameObject.AddComponent<Image>();
                art.sprite = ribbonArt;
                art.preserveAspect = true;               // CreateIcon değil, elle
                art.raycastTarget = false;

                // Yazı bandın üstünde, bandla aynı açıda (45°, sol-alttan
                // sağ-üste). Bandın orta çizgisi holder'ın (0.38, 0.62)
                // noktasından geçiyor.
                var band = UiKit.CreateRect("Text", holder);
                band.anchorMin = band.anchorMax = new Vector2(0.38f, 0.62f);
                band.pivot = new Vector2(0.5f, 0.5f);
                band.sizeDelta = new Vector2(size * 1.15f, 46f);
                band.anchoredPosition = Vector2.zero;
                band.localRotation = Quaternion.Euler(0f, 0f, 45f);

                // Ad `label` DEĞİL: aynı metotta prosedürel yedeğin `label`ı
                // var ve C# iç içe kapsamda aynı adı kabul etmiyor (CS0136).
                var ribbonText = UiKit.CreateTitle("Label", band, text, 30,
                    new Color(1f, 0.99f, 0.96f), new Color(0.32f, 0.04f, 0.13f));
                UiKit.Place(ribbonText, 0f, 0f, 1f, 1f);
                ribbonText.raycastTarget = false;
                return;
            }

            const float inset = 88f;

            // ÜÇ KATMANLI ŞERİT (4. tur, E13).
            //
            // Kullanıcı: "Best Value / Popular şerit bannerları kötü — yeni
            // görsel üretilecek, referanstaki gibi dış çizgileri (outline)
            // olacak."
            //
            // Şerit düz pembe TEK bir dikdörtgendi; kartın renkli sanatının
            // üstünde sınırı olmadığı için "yapıştırılmış kağıt" gibi
            // duruyordu. Referanstaki bütün plakalarda kalın bir dış çizgi ve
            // içeride bir tık açık bir yüzey var — hacmi o iki çizgi veriyor.
            //
            // DERS (kontur ayrı bir GÖRSEL gerektirmez): Aynı dikdörtgeni
            // biraz büyük ve koyu renkte arkaya koymak konturun ta kendisi.
            var strip = UiKit.CreateRect("Ribbon", clip);
            strip.anchorMin = strip.anchorMax = new Vector2(0f, 1f);
            strip.pivot = new Vector2(0.5f, 0.5f);
            strip.sizeDelta = new Vector2(2f * inset * 1.414f, 54f);
            strip.anchoredPosition = new Vector2(inset, -inset);
            strip.localRotation = Quaternion.Euler(0f, 0f, 45f);

            var rim = strip.gameObject.AddComponent<Image>();
            rim.color = new Color(0.318f, 0.043f, 0.129f);      // dış çizgi
            rim.raycastTarget = false;

            var face = UiKit.CreateRect("Face", strip);
            UiKit.Place(face, 0f, 0f, 1f, 1f, padding: 5f);
            var faceImage = face.gameObject.AddComponent<Image>();
            faceImage.color = RibbonPink;
            faceImage.raycastTarget = false;

            // Üst kenarda ince bir ışık: şeridin kumaş gibi büküldüğünü
            // söyleyen tek ayrıntı.
            var sheen = UiKit.CreateRect("Sheen", face);
            UiKit.Place(sheen, 0.02f, 0.58f, 0.98f, 0.94f);
            var sheenImage = sheen.gameObject.AddComponent<Image>();
            sheenImage.color = new Color(1f, 1f, 1f, 0.16f);
            sheenImage.raycastTarget = false;

            var label = UiKit.CreateTitle("Label", face, text, 24,
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

            // ALT GÖLGE (7. tur, N53: "…ve gölge eklenecek"). Paket
            // kartındaki `Drop` ile aynı numara: kutunun altına taşan koyu
            // bir kopya. Kutu kızıl zeminin ÜSTÜNDE duruyor görünsün diye —
            // gölgesiz hâlde ızgara, zemine çizilmiş altı kare gibiydi.
            var drop = UiKit.CreateRoundedPanel("Drop", tile,
                new Color(0f, 0f, 0f, 0.34f));
            UiKit.Place(drop, 0.02f, -0.030f, 0.98f, 0.97f);
            drop.raycastTarget = false;

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

            // Düğme yükseklikte SABİT 90 birim: taban büyüdü diye düğme de
            // büyüseydi kullanıcının şikâyeti ("fiyat butonları çok büyük
            // görünmesin") aynen kalırdı. Taban içinde bir tık yukarı
            // oturuyor — altındaki 26 birim, gölgesinin düştüğü pay.
            const float BuyH = 90f, BuyBottom = 26f;
            var buy = PriceButton("Buy", tile, price, 36);
            UiKit.Place(buy, 0.085f, BuyBottom / height, 0.915f, (BuyBottom + BuyH) / height);
            buy.onClick.AddListener(() => Purchase("coins_" + coins, coins, 0, false, price));
            _purchaseButtons.Add(buy);
        }

        void BuildRestore(Transform content, float top)
        {
            var row = Row("Restore", content, top, RestoreH, 0.24f, 0.76f);

            var button = MenuPage.PillButton("Button", row, "Restore Purchases",
                                             RestoreBlue, 36, null);
            UiKit.Place(button, 0f, 0f, 1f, 1f);
            button.onClick.AddListener(() =>
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
                // AYNI DERS, İKİNCİ KEZ (bkz. aşağıdaki sonsuz kalp):
                // hazır görselin İÇİNDE zaten "ADS" yazıyorsa üstüne ikinci
                // bir yazı koyma. Eski `icon_noads` düz bir yasak halkasıydı
                // ve harfleri kod çiziyordu; yenisinde harfler görselin
                // içinde. İkisi birden çizilince ekranda "AADSS" çıktı.
                var skinned = UiSkin.Get(Art.NoAds);

                var badge = UiKit.CreateRect("NoAds", parent);
                var ring = badge.gameObject.AddComponent<Image>();
                ring.sprite = skinned ?? MenuSprites.NoAds;
                ring.preserveAspect = true;
                ring.raycastTarget = false;
                ring.color = skinned != null
                    ? Color.white : new Color(0.85f, 0.10f, 0.12f);
                // ÖLÇÜM (referans ve bizim yakalamamıza AYNI kod):
                // rozetin yüksekliği kart boyunun referansta 0.440'ı, bizde
                // 0.538'iydi; genişliği referansta 0.141, bizde 0.184.
                // Yeni kutu ikisini de referans oranına indiriyor ve X ekseni
                // referansta ölçülen 0.574-0.715 aralığına oturuyor.
                UiKit.Place(badge, 0.578f, 0.542f, 0.719f, 1.00f);

                if (skinned == null)
                {
                    // Görsel yoksa harfleri kod çizer. Referansta harfler
                    // halkanın dışına TAŞIYOR; halka bir çerçeve değil,
                    // yazının üstünü çizen bir işaret.
                    var word = UiKit.CreateTitle("Word", parent, "ADS", 40,
                        new Color(1f, 1f, 1f), new Color(0.35f, 0.03f, 0.03f));
                    UiKit.Place(word, 0.548f, 0.62f, 0.748f, 0.84f);
                }
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

            // "3s" KALBİN ALTINDA, ORTALANMIŞ. Eskiden 0.845-0.955'teydi,
            // yani kalbin sağına kaçmış ve UFO'nun yanında uçuşuyordu.
            // Referansta sonsuz kalbin tam altında ve onunla aynı eksende:
            // kalp X 0.745-0.890 → merkez 0.8175.
            var span = UiKit.CreateTitle("Span", parent, hours + "s", 30,
                new Color(1f, 1f, 1f), TitleShade);
            UiKit.Place(span, 0.762f, 0.50f, 0.873f, 0.68f);

            // Üç yardımcı, referanstaki sırayla: roket, çalar saat, ufo.
            string[] icons = { Art.Rocket, Art.Clock, Art.Ufo };
            float[] centers = { 0.606f, 0.732f, 0.883f };
            for (int i = 0; i < 3; i++)
            {
                // Yardımcılar büyütüldü: referansta ADS rozetiyle karşılaştırınca
                // belirgin biçimde daha iriler. Kutu ±0.070 → ±0.080,
                // dikeyde 0.34 → 0.39.
                // ALTTAN KESİLME DÜZELTMESİ (kullanıcı bulgusu, 2026-08-22).
                // Kutuyu 0.14 -> 0.09'a indirmek REGRESYON olmuştu: çizilen
                // ikon kartın alt maskesine (RectMask2D) taşıyor ve roketin
                // alevi / saatin kaidesi / UFO'nun altı kesiliyordu.
                // Kutu 0.14-0.50'ye alındı (yükseklik 0.36 korunuyor) ve
                // preserveAspect AÇILDI — `CreateIcon` bunu varsayılan olarak
                // kurmuyor, yani ikon kutuyu doldurmak için GERİLİYORDU.
                var icon = UiKit.CreateIcon("Helper" + i, parent, UiSkin.Get(icons[i]));
                icon.preserveAspect = true;
                UiKit.Place(icon, centers[i] - 0.080f, 0.14f, centers[i] + 0.080f, 0.50f);

                var count = UiKit.CreateTitle("Count" + i, parent, "x" + helpers, 32,
                    new Color(1f, 1f, 1f), TitleShade);
                UiKit.Place(count, centers[i] - 0.005f, 0.03f, centers[i] + 0.105f, 0.20f);
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

        /// <summary>
        /// Paketlerin yeşil fiyat düğmesi. Oyunun standart düğmesi + altına
        /// taşan yumuşak gölge.
        ///
        /// DERS (bir kalıbı düzeltmek onu ARAMAYI gerektirir): Düğmeler 8.
        /// turda tek reçeteye indirilmişti ama bu BEŞİNCİ uygulama gözden
        /// kaçmıştı — mağazada, oyunun en çok bakılan ikinci ekranında,
        /// yanındaki her şeyden farklı köşe yarıçapıyla duruyordu.
        /// Düzeltilen örnek, aranacak şeyin tarifidir.
        ///
        /// Gölge KORUNDU: 5. turda kullanıcı "yeşil butonların bir tık daha
        /// fazla gölgesi var" demişti ve referansta da var. Düğmenin ALTINA
        /// taşıyor, yani reçetenin bir parçası değil, bu ekrana özel.
        /// </summary>
        static Button PriceButton(string name, Transform parent, string text, int fontSize)
        {
            var root = UiKit.CreateRect(name, parent);

            var drop = Capsule("Drop", root, new Color(0f, 0f, 0f, 0.30f),
                               MenuPage.ButtonCornerShare);
            UiKit.Place(drop, 0.004f, -0.10f, 0.996f, 0.94f);
            drop.raycastTarget = false;

            var button = MenuPage.PillButton("Body", root, text, PriceGreen, fontSize, null);
            UiKit.Place(button, 0f, 0f, 1f, 1f);
            return button;
        }
    }
}
