using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Arayüz sprite'larının tek kayıt yeri.
    ///
    /// DERS (Resources tuzağı): Sprite'lar `Art/UI/` altında duruyor ve orası bir
    /// Resources klasörü DEĞİL — yani çalışma anında `Resources.Load` ile
    /// bulunamazlar. İlk akla gelen çözüm hepsini Resources'a taşımak; ama
    /// Resources klasöründeki her şey, kullanılsın kullanılmasın, derlemeye
    /// girer ve açılışta indekslenir. Bunun yerine Resources'a YALNIZCA bu küçük
    /// asset konuyor; sprite'lara referans verdiği için hepsi derlemeye
    /// girer, klasör düzeni bozulmaz ve neyin kullanıldığı tek yerden görünür.
    ///
    /// Asset'i elle doldurmuyoruz: kurulum aracı klasörü tarayıp dosya adına
    /// göre yazıyor. Yeni bir sprite atmak, onu kullanılabilir kılmaya yeter.
    /// </summary>
    public sealed class UiSkin : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string name;
            public Sprite sprite;
        }

        [SerializeField] Entry[] entries = new Entry[0];

        public Entry[] Entries => entries;

        /// <summary>
        /// Kurulum aracı listeyi yeniler. Arama tablosu MUTLAKA sıfırlanır.
        ///
        /// DERS (önbellek, kaynağı değişince geçersizdir): Arama tablosu bir kez
        /// kurulup ScriptableObject örneğinde saklanıyor. Editörde o örnek
        /// düzenleme ve oyun modu arasında YAŞAMAYA DEVAM EDER; yeni sprite'lar
        /// eklendiğinde tablo eskisini tutmaya devam etti ve yeni görseller
        /// "yok" göründü — avatar çerçevesi ve kare düğme, dosyalar yerinde
        /// olduğu hâlde yedek çizime düştü. Önbelleği besleyen veri değiştiğinde
        /// önbelleği düşürmek, yazılmaması en pahalı iki satırdan biri.
        /// </summary>
        public void SetEntries(Entry[] value)
        {
            entries = value;
            _lookup = null;
        }

        static UiSkin _current;
        static bool _searched;
        Dictionary<string, Sprite> _lookup;

        /// <summary>Resources'taki tek skin asset'i; yoksa null (kod yine çalışır).</summary>
        public static UiSkin Current
        {
            get
            {
                if (!_searched)
                {
                    _searched = true;
                    _current = Resources.Load<UiSkin>("UiSkin");
                    if (_current == null)
                        Debug.LogWarning("[UiSkin] Resources/UiSkin.asset yok — arayüz " +
                                         "prosedürel sprite'lara düşecek.");
                }
                return _current;
            }
        }

        /// <summary>Adıyla sprite getirir; bulunamazsa null döner, patlamaz.</summary>
        public static Sprite Get(string spriteName)
        {
            var skin = Current;
            if (skin == null) return null;

            if (skin._lookup == null)
            {
                skin._lookup = new Dictionary<string, Sprite>(skin.entries.Length);
                foreach (var entry in skin.entries)
                    if (!string.IsNullOrEmpty(entry.name) && entry.sprite != null)
                        skin._lookup[entry.name] = entry.sprite;
            }

            return skin._lookup.TryGetValue(spriteName, out var sprite) ? sprite : null;
        }
    }

    /// <summary>
    /// Sprite adları. Dizeyi çağrı yerine yazmak yerine buradan kullan:
    /// yazım hatası derleme hatasına dönüşür, çalışma anında sessiz boşluğa değil.
    /// </summary>
    public static class Art
    {
        public const string ButtonGreen  = "btn_green";
        public const string ButtonPurple = "btn_purple";
        public const string ButtonRed    = "btn_red";
        public const string PanelCard    = "panel_card";
        public const string PanelDark    = "panel_dark";
        public const string FrameBoard   = "frame_board";
        public const string MenuBack     = "bg_menu";
        public const string LevelNode    = "node_level";
        public const string RegionBanner = "banner_region";
        public const string Confetti     = "confetti_sheet";
        public const string Characters   = "home_characters";
        public const string Avatar       = "avatar_player";

        /// <summary>
        /// Rakip avatarları. Liderlik satırları ve podyum bunları sırayla
        /// kullanıyor — hepsi aynı yüz olursa tablo tek bir kişinin
        /// tekrarı gibi okunuyor.
        ///
        /// NOT: yalnız 2, 3 ve 4 ÇERÇEVESİZ. `avatar_6..9` kendi altın
        /// çerçevesiyle geliyor ve bizim kare çerçevemizin içine konunca
        /// ÇİFT ÇERÇEVE oluyor; onlar şimdilik kullanılmıyor.
        /// </summary>
        public static string Rival(int index) => "avatar_" + (2 + index % 3);

        public const string Check        = "check_green";
        public const string Ribbon       = "ribbon_reward";
        public const string AvatarFrame  = "frame_avatar";
        public const string ButtonSquare = "btn_square";
        public const string Splash       = "splash_art";
        public const string Hand         = "icon_hand";
        public const string RewardBadge  = "badge_reward";
        public const string TabBar       = "bar_tabs";
        public const string TabCard      = "card_tab";

        /// <summary>Bölge dairesi (1 tabanlı). Yoksa null döner, çağıran düşer.</summary>
        public static string Region(int index) => "region_" + index;

        /// <summary>
        /// Mağazadaki altın yığını (1-6, küçükten büyüğe). Referansta altı
        /// jeton kutusunun her biri farklı büyüklükte bir yığın gösteriyor;
        /// fiyat okunmadan önce göz hangisinin daha çok verdiğini görüyor.
        /// </summary>
        public static string CoinPile(int index) => "coin_pile_" + index;

        /// <summary>Mağaza paketinin kabı (1-5: kese, kavanoz, sandık, lüks sandık, hazine).</summary>
        public static string PackArt(int index) => "pack_" + index;

        /// <summary>Varsa kullanılır; yoksa <see cref="MenuSprites"/> çizimine düşülür.</summary>
        public const string NoAds    = "icon_noads";
        public const string Infinite = "icon_infinite";

        /// <summary>
        /// KAPAT ÇARPISI (13. tur, G3).
        ///
        /// Kullanıcı: *"Klasik kapat X işareti orijinalden alınacak; bizdeki
        /// çok kötü."* Bizimki üç prosedürel katmandı (koyu halka + kırmızı
        /// yüz + çarpı) ve referansın hacmini vermiyordu.
        ///
        /// `WhatsApp Image 2026-08-17 ... (2).jpeg` (946x2048, videodan 2,5
        /// kat büyük) üzerinden kesildi. Düğme bir DAİRE olduğu için alfa
        /// eşikten değil YARIÇAPTAN kuruldu; kenardaki karışım da bilinen
        /// panel moruyla çözüldü.
        ///
        /// Yarıçaplar yatay kesitle ölçüldü: kırmızı 0-53, koyu indigo halka
        /// 54-60, panel 61+. İlk denemede dış yarıçapı 66 alıp panelin morunu
        /// da içeri almıştım.
        /// </summary>
        public const string Close   = "icon_close";

        /// <summary>
        /// MAĞAZA TENTESİ (13. tur, M2 — kullanıcı: *"mağaza tentesi çok
        /// kötü olmuş, yeniden yap, aynısını yap, gerekirse direkt oyundan
        /// al"*).
        ///
        /// Prosedürel tente (`MenuSprites.Awning`) üç turdur yaklaşamadı;
        /// son denemede yarım daire taraklar çizilmişti ama referansın
        /// gerçek biçimi o değil: alt köşeleri yuvarlatılmış GENİŞ DİKEY
        /// ŞERİTLER, hepsi aynı yükseklikte bitiyor ve altlarında koyu
        /// lacivert bir levha var.
        ///
        /// `market.jpeg`ten (946x2048) kesildi. Üstündeki jeton kapsülü ve
        /// "Mağaza" başlığı, TEMİZ BİR PERİYODUN (x 658..926, iki şerit)
        /// aynı fazla döşenmesiyle silindi — yani geometri referansın
        /// kendisi, yalnız kaplayan öğeler kaldırıldı.
        /// </summary>
        public const string Awning  = "awning_shop";

        /// <summary>
        /// MAĞAZANIN BÖLÜM KURDELESİ (13. tur — kullanıcı: *"özel teklifler
        /// başlığını güncelleyelim, o dış çizgisi kötü olmuş, orijinaline
        /// benzetelim"*).
        ///
        /// Prosedürel kurdele bir tur denendi ve profili doğru çizmesine
        /// rağmen kenarı tutmadı. Tentede öğrenilen kural burada doğrudan
        /// uygulandı: kes.
        ///
        /// `market.jpeg`ten: kurdele y 318..425, köşe yarıçapı 38 (ölçüldü:
        /// sol uçta kumaş x=32'de açılmaya başlıyor, x=70'te tam yüksekliğe
        /// ulaşıyor). Renk profili YAZI OLMAYAN bir sütundan (x=200)
        /// alındı; biçim ölçülen yarıçaptan kuruldu, böylece köşelerde
        /// JPEG'in kahverengi duvarı karışmıyor.
        /// </summary>
        public const string SectionRibbon = "ribbon_section";

        /// <summary>
        /// Bölüm kurdelesinin UÇLARINDAKİ DİL. Referansta düz altın değil,
        /// dikey gradyanlı: (250,246,123) -> (247,191,6) -> (199,77,0).
        /// Bizimki tek renkti ve kurdelenin yanında yassı duruyordu.
        /// `market.jpeg` y 354..392, x=12 (temiz sütun).
        /// </summary>
        public const string RibbonTail = "ribbon_tail";

        public const string Coin    = "icon_coin";
        public const string Heart   = "icon_heart";
        public const string HeartBroken = "icon_heart_broken";
        public const string Gear    = "icon_gear";
        public const string Star    = "icon_star";
        public const string Lock    = "icon_lock";
        public const string Chest   = "icon_chest";
        public const string Clock   = "icon_clock";

        /// <summary>
        /// "Süre Doldu" ekranının ALTIN KRONOMETRESİ (12. tur, O1).
        ///
        /// Orada eskiden <see cref="Clock"/> (süre yardımcısının yeşil çalar
        /// saati) kullanılıyordu — kod yorumu bunu "elimizdeki en yakın
        /// görsel" diye açıklıyordu. `Game over .mp4` 8. saniyeyle yan yana
        /// konunca yakın değil, YANLIŞ NESNE olduğu görüldü.
        /// `_Reference/draw_stopwatch.py` ile çizildi.
        /// </summary>
        public const string Stopwatch = "icon_stopwatch";
        public const string Rocket  = "icon_rocket";
        public const string Ufo     = "icon_ufo";
        public const string Plus    = "icon_plus";
        public const string Trophy  = "icon_trophy";
        public const string Shop    = "icon_shop";
        public const string Home    = "icon_home";
        public const string Globe   = "icon_globe";
        public const string Restart = "icon_restart";
        public const string Album   = "icon_album";

        // --- profil istatistikleri (11. tur) ---
        // Referansın profil ekranında sekiz kutucuğun sekizinde de FARKLI bir
        // simge var; bizde beş simge tekrar ediyordu (kupa, roket ve UFO
        // ikişer kez). Bu dördü `_Reference/draw_stat_icons.py` ile, menü
        // simgeleriyle aynı palet ve bitirişte çizildi.
        public const string Medal   = "icon_medal";    // ilk denemede kazanıldı
        public const string Flag    = "icon_flag";     // seri yarışı
        public const string League  = "icon_league";   // blok ligi
        public const string Saucer  = "icon_saucer";   // gökyüzü atlayışı

        /// <summary>
        /// Oyunun logosu ve stüdyo yazısı (49. ve 35. madde).
        ///
        /// İkisi de ÜRETİLMEDİ, referans ekran görüntülerinden kesildi:
        /// yapay zekâ ile üretilen logo referansa benzemedi ve kullanıcı
        /// birebir olanı istedi. Harf biçimi bir markanın kimliği; "aynı
        /// tarzda" üretmek burada yaklaşmıyor bile.
        /// </summary>
        public const string GameLogo   = "logo_game";
        public const string StudioLogo = "logo_studio";

        /// <summary>
        /// Logonun HARF HARF parçaları — kazanma kutlaması bunları tek tek
        /// getiriyor (bkz. WinCelebration).
        ///
        /// Bunlar elle çizilmedi, `tools/slice_logo.py` tek parça logodan
        /// üretiyor: harfler renklerinden bulunuyor, kalan her piksel en
        /// yakın harfe veriliyor ve her parçaya KENDİ mor konturu geri
        /// büyütülüyor. Sıra ÇİZİM SIRASI — soldaki harf sağdakinin altında
        /// kalıyor; hepsi açıkken sonuç piksel piksel `logo_game`.
        /// </summary>
        /// Her harf İKİ görsel: `logo_b` harfin kendisi, `logo_b_back` mor
        /// zemini. Kutlamada önce bütün zeminler, sonra bütün harfler
        /// çiziliyor (bkz. WinCelebration.BuildLetters).
        public static readonly string[] GameLogoLetters =
        {
            "logo_b", "logo_l", "logo_o", "logo_c", "logo_k", "logo_out",
            "logo_b_back", "logo_l_back", "logo_o_back",
            "logo_c_back", "logo_k_back", "logo_out_back",
        };

        /// <summary>Liderlik kürsüsü — 1., 2., 3. için ayrı kaide.</summary>
        public static string Podium(int rank) =>
            rank == 1 ? "podium_gold" : rank == 2 ? "podium_silver" : "podium_bronze";

        /// <summary>Liderlik ekranının park sahnesi (arka plan, kesilmez).</summary>
        public const string BoardScene = "board_scene";

        /// <summary>Koleksiyon ekranının ortasındaki kitap + albüm paketleri.</summary>
        public const string CollectionBook = "collection_book";
    }
}
