using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;
using UiSprites = GameKit.UI.UiSprites;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Kaybetme akışının İLK İKİ AŞAMASI — referanstan ölçülerek kuruldu.
    /// Referans: `OneDrive/Masaüstü/Block Out! Videos/Game over .mp4`,
    /// 1. aşama 7-11. saniye, 2. aşama 12-16. saniye (384x832).
    ///
    /// DERS (kaybetmek TEK panel değil, bir AKIŞ): Bizde süre bitince doğrudan
    /// "FAILED" kartı açılıyordu. Referans araya iki teklif koyuyor ve ikisi de
    /// aynı soruyu farklı fiyatla soruyor: "bırakma, devam et". Tek kart
    /// oyuncuya yalnız kaybettiğini söyler; akış ona geri dönmesi için iki ayrı
    /// kapı açar. Kaybetme ekranı, oyunun en çok terk edilen anıdır — oradaki
    /// her kapı gerçek bir tekrar oynama sebebidir.
    ///
    /// AŞAMA 1 "Time's Up!" — kart YOK. Sahne neredeyse siyaha kadar karartılıp
    /// (referansta tahta ~%9'a iniyor) içerik doğrudan perdenin üstüne
    /// konuyor: altın kronometre, "+30 seconds", yeşil "Add Time 900".
    /// HUD gizleniyor ama JETON SAYACI parlak kalıyor — 900 jetonu olup
    /// olmadığını göremeyen oyuncuya fiyat göstermenin anlamı yok.
    ///
    /// AŞAMA 2 "Continue?" — burada gerçek bir panel var: ekranı bir uçtan
    /// diğerine geçen mor sayfa, üstte başlık bandı, ortada KOYU bir oyuk
    /// (kırık kalp orada), altta "You'll lose 1 life!" ve yeşil "Play 900".
    ///
    /// REFERANSTAN ALINMAYAN İKİ ŞEY:
    /// - 2. aşamanın altındaki "Blok Bileti" promosyon kartı KURULMADI: bizde
    ///   öyle bir ürün yok, olmayan bir şeyi satan bir kart uydurma olurdu.
    /// - Referansta jetonu yetmeyen oyuncu bu panellerden MAĞAZAYA gidebiliyor
    ///   (4. ve 10. saniyeler). Bizde mağaza menü kabuğunda ve menüye dönmek
    ///   tahtayı söküyor (<c>AppRoot.ShowMenu</c> → <c>StopLevel</c>), yani
    ///   "devam et" imkânsız hâle gelirdi. Jeton yetmeyince fiyat kapsülü
    ///   sarsılıyor; panel açık kalıyor.
    /// </summary>
    public sealed class ContinueOffer : MonoBehaviour
    {
        /// <summary>Referanstaki fiyat: iki teklifte de 900 jeton.</summary>
        public const int Price = 900;

        /// <summary>Referanstaki süre: "+30 saniye".</summary>
        public const int ExtraSeconds = 30;

        static readonly Color Ink        = new Color(1f, 0.98f, 0.94f);
        static readonly Color TitleEdge  = new Color(0.16f, 0.10f, 0.32f);
        static readonly Color TitleCream = new Color(0.988f, 0.945f, 0.922f);
        static readonly Color TitleRed   = new Color(0.796f, 0.106f, 0.145f);
        static readonly Color CoinGold   = new Color(1f, 0.820f, 0.320f);

        // Aşama 2 panelinin dört bandı — hepsi referans karesinden örneklendi.
        static readonly Color PanelRim    = new Color(0.172f, 0.043f, 0.368f);
        static readonly Color PanelHeader = new Color(0.439f, 0.208f, 0.827f);
        static readonly Color PanelWell   = new Color(0.192f, 0.071f, 0.396f);
        static readonly Color PanelBody   = new Color(0.380f, 0.133f, 0.737f);
        static readonly Color PanelFoot   = new Color(0.306f, 0.106f, 0.612f);

        RectTransform _root, _stage1, _stage2;
        TextMeshProUGUI _coins1, _coins2;
        RectTransform _price1, _price2;
        System.Action _onContinue, _onDeclined;
        int _stage;

        /// <summary>Teklif ekranda mı? HUD karartmasını çağıran buna bakıyor.</summary>
        public bool Visible => _root != null && _root.gameObject.activeSelf;

        public static ContinueOffer Build(Transform parent)
        {
            var holder = UiKit.CreateRect("ContinueOffer", parent);
            UiKit.Place(holder, 0f, 0f, 1f, 1f);
            var offer = holder.gameObject.AddComponent<ContinueOffer>();
            offer._root = holder;

            // Perde neredeyse mat: referansta tahtanın parlaklığı normalin
            // ~%9'una iniyor. Yardımcı isteminin perdesi (%47) buna göre
            // aydınlık kalır — orada oyuncunun tahtayı OKUMASI gerekiyordu,
            // burada tahta yalnızca bir hatıra.
            var scrim = UiKit.CreatePanel("Scrim", holder, new Color(0.020f, 0.012f, 0.055f, 0.94f));
            scrim.raycastTarget = true;

            offer._stage1 = offer.BuildStage1(holder);
            offer._stage2 = offer.BuildStage2(holder);

            holder.gameObject.SetActive(false);
            return offer;
        }

        // ------------------------------------------------------------ aşama 1

        RectTransform BuildStage1(Transform holder)
        {
            var page = UiKit.CreateRect("Stage1_TimesUp", holder);
            UiKit.Place(page, 0f, 0f, 1f, 1f);

            // AŞAMA 1'DE PANEL YOK — REFERANS BÖYLE (12. tur, O1).
            //
            // Kullanıcı *"bak bu kısmı da patlamış, ne kadar kötü gözüküyor
            // ikon burada"* dediğinde, arkadan sızan tahtayı panel eksikliği
            // sandım ve aşama 2'nin bantlarını buraya da kurdum. YANLIŞTI.
            //
            // `Game over .mp4` 8. saniye tam çözünürlükte çıkarıldı: ekranda
            // kart YOK, içerik doğrudan koyu perdenin üstünde duruyor ve
            // tahta da hafifçe görünüyor — yani sızma kusur değil, tasarım.
            // Eski ölçülerimiz de zaten tutuyordu:
            //
            //     öge      bizim          referans
            //     saat     0,416-0,675    0,405-0,675
            //     düğme    0,162-0,250    0,159-0,249
            //     başlık   0,742-0,805    ~0,778 (orta)
            //     çarpı    0,894-0,942    ~0,916
            //     jeton    0,944-0,984    ~0,934
            //
            // Kullanıcının gördüğü kusur İKONDU: referansta ALTIN BİR
            // KRONOMETRE var, bizde süre yardımcısının yeşil çalar saati
            // kutusuna gerilmiş hâlde duruyordu.
            //
            // DERS (şikâyetin işaret ettiği yere değil, SEBEBİNE bak):
            // "Panel patlamış" cümlesini panelin kendisiyle ilgili sandım ve
            // olmayan bir paneli inşa ettim. Kullanıcı kusuru gördüğü yerden
            // tarif eder; hangi öğenin kusurlu olduğunu ölçüm söyler.
            BuildCoinReadout(page, out _coins1, 0.919f, 0.948f);   // hapın aralığı (referans)
            BuildClose(page, 0.894f, 0.942f, () => ShowStage(2));

            // Başlık krem dolgulu, KIRMIZI konturlu. Paylaşılan başlık materyali
            // tek tek kontur ayarını yok saydığı için SetOutline şart
            // (bu projede yedinci tuzak).
            // PUNTO VE KONTUR REFERANSTAN (12. tur). Ölçüm:
            //     başlık dolgusu   referans %4,33   bizim %3,65   -> punto 82->98
            //     kontur dahil     referans %6,37   bizim %4,06
            // Yani referansın kırmızı konturu bizimkinden BEŞ KAT kalın:
            // her yandan ekranın %1,02'si (1920'de ~20 birim), bizde %0,2.
            //
            // Tek `SetOutline` bunu veremiyor — TMP konturu harfin
            // kenarından İÇERİ büyüyor, kalınlaştırınca dolgu kararıyor
            // (menü başlıklarında öğrenilen ders). Kalınlık arkaya konan
            // kaydırılmış kopyalardan geliyor.
            var title = UiKit.CreateTitle("Title", page, "Time's Up!", 98,
                TitleCream, TitleRed);
            UiKit.Place(title, 0.146f, 0.735f, 0.859f, 0.812f);
            var emboss = GameKit.UI.UiTitleEmboss.Apply(title,
                halo: TitleRed,
                shadow: new Color(0.451f, 0.043f, 0.071f),
                haloThickness: 19f, shadowDrop: 5f);
            // KONTUR YUMUŞATILDI (kullanıcı: *"times up dış kontürü kırmızı
            // fazla keskin, kesilmiş gibi"*). Hale sekiz yöne kaydırılmış
            // kopyadan oluşuyordu; sekiz kopyanın birleşimi çember yerine
            // SEKİZGEN veriyor ve kenarda tarak gibi çentikler bırakıyor.
            // Yön sayısı 24'e çıkarıldı — birleşim çembere yaklaşıyor.
            // (8 -> 16 belirgin düzeltti ama tarak izi kalıyordu; kalınlık
            // 19 birim olduğu için 16 yönde komşu kopyalar arası kiriş hâlâ
            // ~7 birim. 24'te ~5'e iniyor ve kopyaların kendi şişmesi
            // (`HaloDilate`) kalan boşluğu kapatıyor.)
            //
            // Maliyet: başlık başına 24 ek yazı nesnesi. Bu panel ender
            // açılan bir kip penceresi olduğu için kabul edilebilir; menü
            // başlıkları varsayılan 8'de kalıyor.
            emboss.HaloSteps = 24;
            emboss.Sync();

            // ALTIN KRONOMETRE (12. tur). Eskiden süre yardımcısının YEŞİL
            // çalar saati kullanılıyordu — "elimizdeki en yakın görsel" diye.
            // Referansla yan yana konunca yakın değil, yanlış nesneydi.
            // `_Reference/draw_stopwatch.py` ile çizildi.
            //
            // `preserveAspect` ŞART: `UiKit.CreateIcon` onu kurmuyor ve
            // kronometre kare değil; kutusuna gerilince eziliyordu.
            var clock = UiKit.CreateIcon("Clock", page,
                UiSkin.Get(Art.Stopwatch) ?? UiSkin.Get(Art.Clock));
            clock.preserveAspect = true;
            // KUTU REFERANSTAN ÖLÇÜLDÜ (384x832 kare, kronometrenin dolu
            // dikdörtgeni x 88-285, y 265-500):
            //     x %22,9..%74,2      y %39,9..%68,1
            // Eski kutu %41,6-%67,5 idi, yani 46 birim alçaktı ve
            // `preserveAspect` yüzünden kronometre o kadar küçülüyordu.
            UiKit.Place(clock, 0.229f, 0.399f, 0.742f, 0.681f);

            // PUNTO REFERANSTAN (12. tur). Kullanıcı: *"yazıyı buton boyutunu
            // da aynı yap."* Ölçüm (`Game over .mp4`, 132 karenin ortalaması,
            // 384x832): "+30 saniye" beyaz dolgusunun yüksekliği 38 piksel,
            // yani ekran yüksekliğinin %4,57'si -> 1920'de ~88 birim.
            // Baloo2'de kapak yüksekliği punto x 0,72 olduğuna göre gereken
            // punto ~122. Bizdeki 56, yarısından azdı.
            //
            // 110 seçildi: İngilizce "+30 seconds" Türkçe "+30 saniye"den
            // uzun, 122'de kutuyu taşıyor ve `UiTextFit` zaten küçültüyor —
            // yazılan puntonun ekranda geçerli olması için sığan değer alındı
            // (bu projede "kutuya sığmayan punto, yazılmamış puntodur" dersi).
            //
            // KÜÇÜK HARF (13. tur). Yazı "+30 SECONDS" diye TAMAMI BÜYÜK
            // yazılmıştı; ölçüm bunu ele verdi: bizimki referanstan hem %19
            // DAHA GENİŞ hem %24 DAHA KISA çıkıyordu. Aynı yazı aynı puntoda
            // iki yönde birden sapamaz — sapıyorsa ölçülen ŞEY farklıdır.
            // Kırpıp bakınca görüldü: referansta "+30 saniye" küçük harf,
            // bizde büyük. Büyük harf hem geniş hem alt uzantısız, yani tam
            // olarak "geniş ama kısa" kutuyu veriyor. Kendi başlığımız
            // ("Time's Up!") da karışık harfli — büyük harf zaten kendi
            // içinde de tutarsızdı.
            //
            // DERS (iki yönde birden sapan ölçüm, ÖLÇÜMÜ değil VARSAYIMI
            // yalanlar): Bir kutu referanstan hem daha geniş hem daha kısa
            // çıkıyorsa yerleşimi kurcalamadan önce iki görüntüyü yan yana
            // kırp — karşılaştırdığın şeyler aynı şey olmayabilir.
            var amount = UiKit.CreateTitle("Amount", page, "+30 seconds", 110, Ink, TitleEdge);
            UiKit.Place(amount, 0.214f, 0.326f, 0.789f, 0.371f);

            // DÜĞME ÖLÇÜLMÜŞ YERİNE DÖNDÜ. Panel denemesinden kalan
            // 0,352-0,440 değerleri "+30 seconds" yazısının ÜSTÜNE biniyordu.
            // Referans (`Game over .mp4` 8. sn): düğme y %15,9-%24,9.
            // DÜĞME REFERANSTAN BÜTÜN OLARAK ÖLÇEKLENDİ (12. tur).
            //
            // Kullanıcı: *"butonu ve texti ayarla, boyutunu ölçüsünü, butonu
            // referanstakine benzet."* Ölçüm (132 karenin ortalaması):
            //
            //     referans dugme  255 x 74 px   en-boy 3,45
            //     bizim           703 x 139     en-boy 5,06   <- YASSI
            //
            // Yüzdeleri birebir kopyalamak yanlış: referans ekranı 384x832
            // (oran 0,46), bizimki 1080x1920 (0,56). Aynı yüzde genişlik,
            // BİZDE fiziksel olarak daha geniş bir düğme demek; yükseklik
            // yüzdesi sabit kalınca düğme yassılaşıyor.
            //
            // Doğrusu düğmeyi BÜTÜN olarak ölçeklemek: genişlik oranı korunup
            // (255/384 = %66,4) ölçek 717/255 = 2,81 çıkıyor, yükseklik de
            // 74 x 2,81 = 208 birim oluyor. Kutu bundan biraz büyük çünkü
            // `PillBody` altına gölge koyuyor (yeşil, kutunun %82'si):
            // 208 / 0,82 = 254 birim = ekranın %13,2'si.
            //
            // DERS (iki ekranın ORANI farklıysa yüzde kopyalanamaz): Bir
            // öğenin kendi en-boy oranı, ekrandaki yüzdesinden daha çok şey
            // söylüyor. Yüzde kopyalamak farklı oranlı bir ekranda öğenin
            // biçimini bozuyor.
            _price1 = BuildPriceButton(page, "Add Time", 0.168f, 0.141f, 0.832f, 0.273f);
            return page;
        }

        // ------------------------------------------------------------ aşama 2

        RectTransform BuildStage2(Transform holder)
        {
            var page = UiKit.CreateRect("Stage2_Continue", holder);
            UiKit.Place(page, 0f, 0f, 1f, 1f);

            BuildCoinReadout(page, out _coins2, 0.818f, 0.847f);   // aynı yükseklik, aşama 2 hizası

            // Sayfa ekranı bir uçtan diğerine geçiyor — referansta yanlarda
            // boşluk YOK. Bantlar üstten alta: kenar, başlık, koyu oyuk, gövde,
            // ayak. Hepsi ayrı dikdörtgen; tek bir kart görseli bu kademeli
            // renkleri veremezdi.
            Band(page, "Rim",    PanelRim,    0.768f, 0.792f);
            Band(page, "Header", PanelHeader, 0.716f, 0.768f);
            Band(page, "Well",   PanelWell,   0.502f, 0.716f);
            Band(page, "Body",   PanelBody,   0.320f, 0.502f);
            Band(page, "Foot",   PanelFoot,   0.298f, 0.320f);

            var title = UiKit.CreateTitle("Title", page, "Continue?", 60, Ink, TitleEdge);
            UiKit.Place(title, 0.10f, 0.722f, 0.90f, 0.762f);

            var heart = UiKit.CreateIcon("Heart", page, UiSkin.Get(Art.HeartBroken));
            UiKit.Place(heart, 0.302f, 0.543f, 0.698f, 0.683f);

            var warn = UiKit.CreateTitle("Warning", page, "You'll lose 1 life!", 40, Ink, TitleEdge);
            UiKit.Place(warn, 0.198f, 0.462f, 0.805f, 0.492f);

            _price2 = BuildPriceButton(page, "Play", 0.203f, 0.352f, 0.802f, 0.440f);

            // Çarpı panelin ÜST KENARINA biniyor, HUD hizasında değil.
            BuildClose(page, 0.757f, 0.803f, () => { Hide(); _onDeclined?.Invoke(); });
            return page;
        }

        static void Band(Transform page, string name, Color color, float y0, float y1)
        {
            var band = UiKit.CreatePanel(name, page, color);
            band.raycastTarget = true;      // panelin altındaki tahtaya dokunma geçmesin
            UiKit.Place(band, 0f, y0, 1f, y1);
        }

        // ------------------------------------------------------------ parçalar

        /// <summary>
        /// Panelin KENDİ jeton sayacı.
        ///
        /// DERS (fiyat gösteren ekran cüzdanı da göstermeli): HUD bu panellerde
        /// gizleniyor. Sayaç da onunla birlikte kaybolsaydı oyuncuya "900
        /// jeton" yazıp parasını göstermemiş olurduk — referans tam da bu yüzden
        /// sayacı panelin üstünde ayrıca çiziyor.
        /// </summary>
        /// <summary>
        /// Jeton sayacı: krem hap + şeftali alt dudak + rakam + jeton.
        ///
        /// <paramref name="y0"/>/<paramref name="y1"/> artık HAPIN aralığı
        /// (eskiden jetonun kutusuydu). Jeton bu aralıktan TÜRETİLİYOR çünkü
        /// referansta jeton hapın iki katından biraz büyük ve hapla aynı
        /// yatay eksende duruyor.
        ///
        /// ÖLÇÜM (`Game over .mp4`, 132 karenin ortalaması; oran = ekranın
        /// yüzdesi, y alttan):
        ///
        ///     jeton   x %4,2..%12,2   y %91,3..%95,1   merkez %93,21
        ///     hap     x %10,9..%27,9  y %91,9..%94,8   merkez %93,39
        ///
        /// Yani jetonun merkezi hapın merkezinden %0,18 AŞAĞIDA ve jetonun
        /// yüksekliği hapın 2,03 katı.
        ///
        /// ÖNCEKİ HÂLİN İKİ HATASI (kullanıcı: *"coini de ortala, yukarıda
        /// kalmış"*): bütün grup %3,5 yukarıdaydı (jeton %95,2..%98,2) ve
        /// jeton %33 küçüktü (%5,4 genişlik, olması gereken %8,0).
        ///
        /// DERS (bir grubu ölçerken ÖGE ÖGE karşılaştır): "Jeton yukarıda
        /// kalmış" tek bir öğe sorunu gibi duruyordu; altı ögenin kutusu
        /// referansla yan yana yazılınca hem grubun tamamının kaydığı hem de
        /// jetonun küçüklüğü aynı anda göründü. Tek tek bakmak, her turda
        /// bir kusur bulup diğerini kaçırmak demek.
        /// </summary>
        void BuildCoinReadout(Transform page, out TextMeshProUGUI label, float y0, float y1)
        {
            const float px0 = 0.062f, px1 = 0.280f;   // hap: sol uç jetonun ALTINDA
            float h = y1 - y0;
            // -0,0058 KUTU merkezini degil GORUNEN altin icerigin merkezini
            // hizaliyor: `icon_coin.png`in saydam dolgusu asimetrik, altin
            // icerik kutunun merkezinden 10 birim YUKARIDA kaliyor. Kutuyu
            // referans merkezine (%93,27) koymak yetmedi, olculen fark kadar
            // daha asagi itmek gerekti.
            //
            // DERS (kutuyu degil, GORUNENI hizala): preserveAspect sprite'i
            // kutuya ortalar ama sprite'in saydam dolgusu dengesizse gorunen
            // sekil ortalanmaz. Hizalamayi her zaman piksel olcumuyle kapat.
            float merkez = (y0 + y1) * 0.5f - 0.0058f;
            float jetonYari = h * 2.03f * 0.5f;

            // ŞEFTALİ ALT DUDAK — AYNI ŞEKLİN AŞAĞI KAYDIRILMIŞ KOPYASI.
            //
            // Referansın dikey kesitinde hapın alt kenarında iki piksellik
            // sıcak bir şerit var: (255,202,183), hap yüksekliğinin ~%7'si.
            // Bu şerit olmadan hap düz bir krem dikdörtgen gibi duruyor.
            //
            // İLK DENEME YANLIŞTI: dudağı ince ve DÜZ bir dikdörtgen olarak
            // koydum. Yüksekliği 4 birim olunca yuvarlaklığı da 2 birime
            // düşüyor ve hapın yuvarlak köşelerinin DIŞINA taşıyor — altta
            // iki yandan çıkan düz bir uç bırakıyor.
            //
            // DERS (bir kenar şeridi, ŞEKLİN kendisinden türetilmeli):
            // Yuvarlak bir forma düz bir şerit eklemek, formun dışına taşan
            // bir uç bırakır. Aynı şekli kaydırmak hem ucuz hem şekil ne
            // olursa olsun doğru.
            var lip = UiKit.CreateRoundedPanel("PillLip", page,
                new Color(1f, 0.792f, 0.718f), 0.5f);
            lip.raycastTarget = false;
            UiKit.Place(lip, px0, y0 - 0.0022f, px1, y1 - 0.0022f);

            var pill = UiKit.CreateRoundedPanel("CoinPill", page,
                new Color(0.969f, 0.941f, 0.925f), 0.5f);
            pill.raycastTarget = false;
            UiKit.Place(pill, px0, y0, px1, y1);

            // KONTURSUZ. Bir sürümde `CreateTitle`a dolgu rengiyle AYNI
            // konturu verdim; kontur harfin kenarından dışa büyüdüğü için
            // rakamlar şişip hantallaştı — kullanıcı: *"sayı yazan font fazla
            // kalın"*. `CreateLabel` kontur eklemiyor.
            // Punto: referansta sayı hapın yüksekliğinin %52'si.
            label = UiKit.CreateLabel("Coins", page, "",
                48, new Color(0.129f, 0.125f, 0.231f));
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            UiKit.Place(label, 0.126f, y0, 0.278f, y1);

            // Jeton EN SON: hapın sol ucuna binmesi için üstte çizilmeli.
            var icon = UiKit.CreateIcon("Coin", page, UiSkin.Get(Art.Coin), CoinGold);
            icon.preserveAspect = true;
            UiKit.Place(icon, 0.026f, merkez - jetonYari, 0.138f, merkez + jetonYari);
        }

        void BuildClose(Transform page, float y0, float y1, UnityEngine.Events.UnityAction onClick)
        {
            var root = UiKit.CreateRect("Close", page);
            UiKit.Place(root, 0.854f, y0, 0.958f, y1);

            // DAİRE `CreateIcon` İLE: düz bir `Image` sprite'ı kutuya gerer ve
            // kutu kare olmadığı anda daire elipse döner (bkz. MenuPage.Close).
            // RENKLER REFERANSTAN (12. tur — kullanıcı: *"referans görselin
            // kapat butonu daha iyi"*). Ölçüm (y=67 yatay kesiti):
            //     koyu halka  (163, 0, 0)     bizde (107, 13, 23)  <- ÇOK KOYU
            //     parlak yüz  (245, 45, 50)   bizde (218, 37, 46)
            //     çarpı       (253,248,240) krem, saf beyaz değil
            // Bizimki hem koyu hem donuktu; referansın halkası kırmızının
            // KOYU tonu, siyaha kaçanı değil.
            var ringImage = UiKit.CreateIcon("Ring", root, UiSprites.Circle,
                new Color(0.639f, 0f, 0f));
            UiKit.Place(ringImage, 0f, 0f, 1f, 1f);

            var faceImage = UiKit.CreateIcon("Face", root, UiSprites.Circle,
                new Color(0.961f, 0.176f, 0.196f));
            UiKit.Place(faceImage, 0.07f, 0.09f, 0.93f, 0.95f);

            var cross = UiKit.CreateIcon("Cross", faceImage.transform, UiSprites.Cross,
                new Color(0.992f, 0.973f, 0.941f));
            UiKit.Place(cross, 0.26f, 0.26f, 0.74f, 0.74f);

            UiKit.MakeClickable(root.gameObject, ringImage, onClick);
        }

        /// <summary>
        /// Yeşil düğme: "&lt;eylem&gt; · jeton · 900".
        ///
        /// DERS (rolünün görselini seç, rengini BOYAMA): `btn_green`'in alt
        /// kenarında baskılı bir gölge var; boyama çarpma olduğu için onu
        /// başka bir renge çevirmek düğmenin altında olmayan bir renk şeridi
        /// bırakır. Yeşil isteniyorsa yeşil sprite kullanılır.
        /// </summary>
        RectTransform BuildPriceButton(Transform page, string action,
                                       float x0, float y0, float x1, float y1)
        {
            // Oyunun standart düğmesi (8. tur) — görselden değil koddan.
            var body = MenuPage.PillBody("Buy", page, MenuPage.Green, out _, out _);
            UiKit.Place(body, x0, y0, x1, y1);
            var button = body.gameObject.AddComponent<Button>();
            GameKit.UI.UiPressFeedback.Attach(button);   // 12. tur, H7
            button.transition = Selectable.Transition.None;
            body.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            button.onClick.AddListener(Buy);

            var face = body.Find("Face");

            // PUNTO DÜĞMEYLE BİRLİKTE ÖLÇEKLENDİ (12. tur). Referansta yazı
            // 19 piksel ve düğme 74 piksel — yani düğme yüksekliğinin
            // %25,7'si. Düğme 2,81 kat büyütülünce yazı da 19 x 2,81 = 53
            // birim olmalı: punto 46 -> 66 -> 78 -> 89 (ölçülerek yakınsandı:
            // 78 punto yazıyı düğmenin %22,5'inde bırakıyordu, hedef %25,7).
            //
            // Yazıyı düğmeden BAĞIMSIZ ölçeklemek, düğmenin içinde farklı
            // oranda duran bir yazı bırakıyordu; ikisi tek bir görsel.
            //
            // 89 -> 77 (13. tur). Yukarıdaki %25,7 hedefi PUNTODAN
            // hesaplanmıştı; çizilen sonuç ölçülünce yazı düğme yüksekliğinin
            // %29,9'unu ve GENİŞLİĞİNİN %95,7'sini kaplıyordu. Referansta bu
            // iki oran %26,0 ve %81,1 — yani bizim yazı düğmenin kenarlarını
            // yiyor, referanstaki ise iki yanında eşit %9,4 boşluk bırakıyor.
            // 89 x (26,0/29,9) = 77.
            //
            // DERS (oranı PUNTODAN değil ÇİZİLENDEN hesapla): "punto x kapak
            // oranı" tahmini bu düğmede %16 saptı; `UiTextFit` küçültmesi,
            // kontur kalınlığı ve harf aralığı araya giriyor. Hedef bir ORANSA
            // (yazı/düğme), oranı ölçüp puntoyu o oranla ölçekle.
            var label = UiKit.CreateTitle("Action", face, action, 77, Ink, new Color(0.04f, 0.24f, 0.02f));
            label.alignment = TextAlignmentOptions.Right;
            // KUTU GENİŞLETİLDİ (12. tur). Punto 89 yazılmıştı ama ekranda 63
            // çıkıyordu: `UiTextFit` "Add Time"i 299 birimlik kutuya
            // sığdırmak için küçültüyordu (çizilen 302). Bu projede tekrarlayan
            // ders — "kutuya sığmayan punto, yazılmamış puntodur".
            // 0,47 -> 0,555 (yaklaşık 388 birim), böylece yazılan punto
            // gerçekten ekrana gidiyor.
            UiKit.Place(label, 0.045f, 0.20f, 0.600f, 0.80f);

            var coin = UiKit.CreateIcon("Coin", face, UiSkin.Get(Art.Coin), CoinGold);
            coin.preserveAspect = true;
            UiKit.Place(coin, 0.618f, 0.24f, 0.722f, 0.76f);

            var price = UiKit.CreateTitle("Price", face, Price.ToString(), 77, Ink,
                new Color(0.04f, 0.24f, 0.02f));
            price.alignment = TextAlignmentOptions.Left;
            UiKit.Place(price, 0.742f, 0.20f, 0.965f, 0.80f);

            return (RectTransform)button.transform;
        }

        // ------------------------------------------------------------ davranış

        /// <summary>
        /// Teklifi açar. <paramref name="onContinue"/> jeton ödenip bölüme
        /// dönülünce, <paramref name="onDeclined"/> ikinci aşama da kapatılınca
        /// çağrılır — asıl "FAILED" kartını açan odur.
        /// </summary>
        public void Show(System.Action onContinue, System.Action onDeclined)
        {
            _onContinue = onContinue;
            _onDeclined = onDeclined;
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            ShowStage(1);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
            _stage = 0;
        }

        void ShowStage(int stage)
        {
            _stage = stage;
            _stage1.gameObject.SetActive(stage == 1);
            _stage2.gameObject.SetActive(stage == 2);

            RefreshCoins();
            var card = stage == 1 ? _stage1 : _stage2;
            GameKit.FX.Juice.Run(GameKit.FX.Juice.PopIn(card, 0.28f));
        }

        void RefreshCoins()
        {
            string text = MetaServices.Ready ? MetaServices.Progress.Coins.ToString() : "0";
            if (_coins1 != null) _coins1.text = text;
            if (_coins2 != null) _coins2.text = text;
        }

        /// <summary>
        /// Jetonla devam. Parası yetmezse HİÇBİR ŞEY satın alınmıyor; fiyat
        /// kapsülü sarsılıyor ve panel açık kalıyor.
        ///
        /// DERS (yetersiz bakiye bir HATA DEĞİL, bir cevaptır): Düğmeyi
        /// tıklanamaz yapmak ya da sessizce yutmak, oyuncuya "bozuk" dedirtir.
        /// Sarsılan fiyat, sayacın hemen üstünde durduğu için sebebi de
        /// gösteriyor.
        /// </summary>
        void Buy()
        {
            if (!MetaServices.Ready) return;

            if (!MetaServices.Progress.TrySpendCoins(Price))
            {
                var target = _stage == 1 ? _price1 : _price2;
                if (target != null)
                    GameKit.FX.Juice.Replace(target,
                        GameKit.FX.Juice.PunchScale(target, 0.18f));
                AudioService.Refuse();
                RefreshCoins();
                return;
            }

            GameKit.Services.Analytics.CurrencySpent("coin", Price,
                _stage == 1 ? "continue_time" : "continue_life");

            Hide();
            _onContinue?.Invoke();
        }
    }
}
