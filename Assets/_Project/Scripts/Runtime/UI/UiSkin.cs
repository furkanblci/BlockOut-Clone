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

        public const string Coin    = "icon_coin";
        public const string Heart   = "icon_heart";
        public const string HeartBroken = "icon_heart_broken";
        public const string Gear    = "icon_gear";
        public const string Star    = "icon_star";
        public const string Lock    = "icon_lock";
        public const string Chest   = "icon_chest";
        public const string Clock   = "icon_clock";
        public const string Rocket  = "icon_rocket";
        public const string Ufo     = "icon_ufo";
        public const string Plus    = "icon_plus";
        public const string Trophy  = "icon_trophy";
        public const string Shop    = "icon_shop";
        public const string Home    = "icon_home";
        public const string Globe   = "icon_globe";
        public const string Restart = "icon_restart";
        public const string Album   = "icon_album";

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

        /// <summary>Liderlik kürsüsü — 1., 2., 3. için ayrı kaide.</summary>
        public static string Podium(int rank) =>
            rank == 1 ? "podium_gold" : rank == 2 ? "podium_silver" : "podium_bronze";

        /// <summary>Liderlik ekranının park sahnesi (arka plan, kesilmez).</summary>
        public const string BoardScene = "board_scene";

        /// <summary>Koleksiyon ekranının ortasındaki kitap + albüm paketleri.</summary>
        public const string CollectionBook = "collection_book";
    }
}
