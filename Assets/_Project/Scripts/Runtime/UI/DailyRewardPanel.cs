using System.Collections.Generic;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;
using UiSprites = GameKit.UI.UiSprites;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Günlük ödül penceresi: yedi günlük dizi, bugünkü hediye vurgulu.
    ///
    /// DERS (ödülü ALMADAN ÖNCE göster): Panel yalnız "işte 50 jeton" deyip
    /// kapansaydı, oyuncu yarın ne kazanacağını bilmezdi. Yedi günü birlikte
    /// göstermek, 7. günün büyük ödülünü BUGÜNDEN görünür kılıyor — zinciri
    /// sürdürmenin sebebi o.
    ///
    /// DERS (kendiliğinden açılır ama zorlamaz): Ana ekran açılınca ödül
    /// varsa panel geliyor. Kapatma düğmesi var ve ödül kaybolmuyor; oyuncu
    /// isterse sonra alır. Kapatılamayan bir pencere, oyuna girer girmez
    /// karşılaşılan ilk düşmandır.
    ///
    /// GÖRSEL DİL — REFERANSSIZ (2026-08-17, 4. APK bulgusu). Kullanıcı bu
    /// ekran için elimizdeki referansın BAŞKA bir oyundan olduğunu söyledi;
    /// panel oyunun kendi diliyle kuruldu: koyu kenarlı mor kart, üst kenarına
    /// binen kapsül başlık, içinde koyu bir oyuk ve oyuğun içinde yedi kutu.
    /// Aynı parçalar Yolculuk, Mağaza ve "Continue?" panelinde de var.
    ///
    /// DERS (bir kutu KAÇ ödül gösteriyor?): Eski panel her gün için TEK simge
    /// çiziyor ve altına HER ZAMAN jeton sayısını yazıyordu. 5. gün "kalp +
    /// 200" görünüyordu; 200 kalp değil jetondu, kalp ise 1 taneydi. Yani
    /// panel oyuncuya yanlış bilgi veriyordu. Artık bir günün hediyeleri
    /// ÇİP olarak yan yana diziliyor: her çipin kendi simgesi ve kendi sayısı
    /// var. 7. gün üç hediye taşıdığı için kutusu da iki kat geniş.
    ///
    /// DERS (yazı KURULUMDA da dolu olmalı): "CLAIM" etiketi yalnız
    /// <see cref="Refresh"/> içinde yazılıyordu; panel Refresh'siz açıldığında
    /// (editörde elle, ileride başka bir çağrı yerinden) düğme BOŞ yeşil bir
    /// çubuk olarak duruyordu. Sonradan doldurulacak her yazının kurulumda da
    /// bir varsayılanı olmalı.
    /// </summary>
    public sealed class DailyRewardPanel : MonoBehaviour
    {
        // --- Kart bantları (Continue? paneliyle aynı aile) ---
        static readonly Color CardRim   = new Color(0.172f, 0.043f, 0.368f);
        static readonly Color CardFace  = new Color(0.380f, 0.133f, 0.737f);
        static readonly Color Well      = new Color(0.192f, 0.071f, 0.396f);
        static readonly Color BannerTop = new Color(0.439f, 0.208f, 0.827f);
        static readonly Color BannerLow = new Color(0.231f, 0.078f, 0.478f);

        // --- Kutu durumları ---
        static readonly Color TileFuture  = new Color(0.290f, 0.118f, 0.588f);
        static readonly Color TileToday   = new Color(0.502f, 0.263f, 0.910f);
        static readonly Color TileClaimed = new Color(0.145f, 0.055f, 0.290f);
        static readonly Color Gold        = new Color(1f, 0.765f, 0.227f);
        static readonly Color GoldInk     = new Color(0.243f, 0.129f, 0.020f);

        static readonly Color Ink      = new Color(1f, 0.98f, 0.94f);
        static readonly Color InkSoft  = new Color(0.702f, 0.639f, 0.925f);
        static readonly Color InkEdge  = new Color(0.114f, 0.031f, 0.259f);

        /// <summary>Bir gün kutusunun durumu değişen parçaları.</summary>
        struct Tile
        {
            public int Day;
            public RectTransform Root;
            public Image Glow, Rim, Face, Tab, Dim, Check;
            public TextMeshProUGUI TabLabel;
        }

        readonly List<Tile> _tiles = new List<Tile>(7);
        RectTransform _root, _card;
        Button _claim;
        TextMeshProUGUI _claimLabel, _subtitle;

        public static DailyRewardPanel Build(Transform parent)
        {
            var holder = UiKit.CreateRect("DailyReward", parent);
            UiKit.Place(holder, 0f, 0f, 1f, 1f);
            var panel = holder.gameObject.AddComponent<DailyRewardPanel>();
            panel._root = holder;

            // Perde neredeyse mat: bu pencere açıkken altındaki ana ekranda
            // yapılacak bir şey yok, okunmasına da gerek yok.
            var scrim = UiKit.CreatePanel("Scrim", holder, new Color(0.020f, 0.012f, 0.055f, 0.90f));
            scrim.raycastTarget = true;

            panel._card = UiKit.CreateRect("Card", holder);
            UiKit.Place(panel._card, 0.045f, 0.230f, 0.955f, 0.788f);

            var rim = UiKit.CreateRoundedPanel("Rim", panel._card, CardRim);
            UiKit.SetSliceScale(rim, 0.34f);
            UiKit.Place(rim, 0f, 0f, 1f, 1f);

            var face = UiKit.CreateRoundedPanel("Face", panel._card, CardFace);
            UiKit.SetSliceScale(face, 0.36f);
            face.raycastTarget = false;
            UiKit.Place(face, 0f, 0f, 1f, 1f, padding: 9f);

            var well = UiKit.CreateRoundedPanel("Well", panel._card, Well);
            UiKit.SetSliceScale(well, 0.45f);
            well.raycastTarget = false;
            UiKit.Place(well, 0.042f, 0.215f, 0.958f, 0.800f);

            panel.BuildTiles(well.rectTransform);

            // --- Alt satır: yeşil "CLAIM" ---
            // Oyunun standart düğmesi (8. tur): bu üç düğme —"CLAIM", teklif
            // ekranındaki "Buy" ve ana ekrandaki reklam düğmesi— görselden
            // geliyordu, gerisi koddan. Aynı ekranda iki farklı düğme stili
            // olması, kullanıcının "bütün butonlar tarz olarak alakasız"
            // geri bildiriminin doğrudan sebebiydi.
            panel._claim = MenuPage.PillButton("Claim", panel._card,
                "CLAIM", MenuPage.Green, 54, null);
            UiKit.Place(panel._claim, 0.200f, 0.040f, 0.800f, 0.180f);
            panel._claimLabel = panel._claim.GetComponentInChildren<TextMeshProUGUI>();
            panel._claim.onClick.AddListener(panel.OnClaim);

            // --- Alt başlık: panelin NE OLDUĞUNU söyleyen tek satır ---
            panel._subtitle = UiKit.CreateLabel("Subtitle", panel._card,
                "Come back every day for a bigger prize!", 34, InkSoft);
            UiKit.Place(panel._subtitle, 0.050f, 0.812f, 0.950f, 0.866f);

            // --- Kapsül başlık: kartın ÜST KENARINA biner ---
            // Sıra önemli: başlık ve çarpı en son kuruluyor ki kartın
            // üstünde kalsınlar (kardeş sırası = çizim sırası).
            BuildBanner(panel._card);
            BuildClose(panel._card, panel.Hide);

            holder.gameObject.SetActive(false);
            return panel;
        }

        /// <summary>
        /// Kapsül başlık. Altında bir tık kaydırılmış koyu kopyası var —
        /// oyunun her düğmesine kalınlık veren aynı numara.
        /// </summary>
        static void BuildBanner(Transform card)
        {
            var banner = UiKit.CreateRect("Banner", card);
            UiKit.Place(banner, 0.100f, 0.872f, 0.900f, 1.048f);

            var shadow = MenuPage.Capsule("Shadow", banner, BannerLow);
            UiKit.Place(shadow, 0f, 0f, 1f, 1f);

            var top = MenuPage.Capsule("Face", banner, BannerTop);
            UiKit.Place(top, 0.010f, 0.130f, 0.990f, 1f);

            var title = UiKit.CreateTitle("Title", top.transform, "DAILY REWARD", 66,
                Ink, InkEdge);
            UiKit.Place(title, 0.05f, 0.08f, 0.95f, 0.92f);

            // KONTUR — paylaşılan başlık materyali `CreateTitle`'a verilen
            // rengi sessizce yok sayıyor (bu projede yedinci tuzak).
            UiKit.SetOutline(title, InkEdge);
        }

        static void BuildClose(Transform card, UnityEngine.Events.UnityAction onClick)
        {
            // Çarpı kapsülün SAĞ UCUNA biniyor ama çoğu kartın dışında kalıyor:
            // fazla içeri girerse başlık kapsülünün yuvarlak ucunu kesip
            // "eksik çizilmiş" gibi gösteriyor.
            var root = UiKit.CreateRect("Close", card);
            UiKit.Place(root, 0.880f, 0.908f, 1.018f, 1.018f);

            // GÖRSEL ORİJİNALDEN (13. tur, G3). Bu daireyi üç prosedürel
            // katmanla kuruyorduk; kullanıcı *"klasik kapat X işareti
            // orijinalden alınacak, bizdeki çok kötü"* dedi. Ortak yardımcı
            // `MenuPage.CloseGlyph` görseli kullanıyor, görsel yoksa eski
            // katmanlara düşüyor.
            var ringImage = MenuPage.CloseGlyph(root);

            UiKit.MakeClickable(root.gameObject, ringImage, onClick);
        }

        // ------------------------------------------------------------ kutular

        /// <summary>
        /// Yedi kutu: üstte 1-4, altta 5-6 ve ÇİFT GENİŞ 7.
        ///
        /// DERS (ızgara eşit olmak zorunda değil): İlk düzen 4+3 eşit kutuydu
        /// ve alt satır sola yaslanıp sağda boşluk bırakıyordu. 7. günün üç
        /// hediyesi de o dar kutuya sığmıyordu. İki hücre birden vermek hem
        /// boşluğu kapatıyor hem de haftanın büyük ödülünü BOYUYLA anlatıyor —
        /// oyuncu okumadan önce görüyor.
        /// </summary>
        void BuildTiles(RectTransform well)
        {
            const float padX = 0.028f;
            const float unit = (1f - padX * 2f) / 4f;   // dört sütun
            const float inset = 0.010f;                 // kutular arası pay

            for (int i = 0; i < DailyRewardService.Week.Length; i++)
            {
                bool topRow = i < 4;
                int column = topRow ? i : i - 4;
                bool grand = i == 6;

                float x0 = padX + column * unit + inset;
                float x1 = padX + (column + (grand ? 2 : 1)) * unit - inset;
                float y0 = topRow ? 0.520f : 0.040f;
                float y1 = topRow ? 0.960f : 0.480f;

                _tiles.Add(BuildTile(well, i, x0, y0, x1, y1));
            }
        }

        Tile BuildTile(RectTransform well, int index, float x0, float y0, float x1, float y1)
        {
            var gift = DailyRewardService.Week[index];

            var root = UiKit.CreateRect($"Day_{index + 1}", well);
            UiKit.Place(root, x0, y0, x1, y1);

            // Işık huzmesi kutunun ARKASINDA ve dışına taşıyor: "buradan
            // parlıyor" hissini veren şey taşma. Rengi Refresh belirliyor.
            var glow = UiKit.CreateIcon("Glow", root, UiSprites.Burst, Color.clear);
            UiKit.Place(glow, -0.30f, -0.24f, 1.30f, 1.24f);
            glow.enabled = false;

            var rim = UiKit.CreateRoundedPanel("Rim", root, MenuPage.Darken(TileFuture, 0.45f));
            UiKit.SetSliceScale(rim, 0.8f);
            rim.raycastTarget = false;
            UiKit.Place(rim, 0f, 0f, 1f, 1f);

            var face = UiKit.CreateRoundedPanel("Face", root, TileFuture);
            UiKit.SetSliceScale(face, 0.85f);
            face.raycastTarget = false;
            UiKit.Place(face, 0f, 0f, 1f, 1f, padding: 6f);

            var chips = UiKit.CreateRect("Gifts", root);
            UiKit.Place(chips, 0.04f, 0.075f, 0.96f, 0.775f);
            BuildChips(chips, gift);

            // Alınmış günün üstüne inen perde + yeşil tik.
            var dim = UiKit.CreateRoundedPanel("Dim", root, new Color(0.02f, 0.01f, 0.06f, 0.55f));
            UiKit.SetSliceScale(dim, 0.85f);
            dim.raycastTarget = false;
            UiKit.Place(dim, 0f, 0f, 1f, 1f, padding: 6f);
            dim.enabled = false;

            // TİK KÖŞEYE, ÖDÜLÜN ÜSTÜNE DEĞİL.
            //
            // DERS (işaret, işaretlediği şeyi gizlememeli): Tik önce kutunun
            // ortasındaydı ve alınmış günlerin jetonunu tamamen örtüyordu —
            // oyuncu dün ne kazandığını göremiyordu. Oysa yedi günü birlikte
            // göstermenin tek sebebi zincirin GÖRÜNMESİ. Rozet köşeye çekilip
            // kenardan biraz taşınca hem "alındı" diyor hem ödülü bırakıyor.
            //
            // ALT köşe de yanlıştı: orada MİKTAR yazısı var ve tik "50"yi
            // "5C" yapıyordu. Kutuda örtülmeye en uygun yer simgenin sağ üst
            // köşesi — bir jetonun kenarı kapansa da jeton hâlâ jeton, ama
            // rakamın yarısı kapanınca sayı okunmaz oluyor.
            var check = UiKit.CreateIcon("Check", root, UiSkin.Get(Art.Check));
            UiKit.Place(check, 0.60f, 0.46f, 1.00f, 0.78f);
            check.enabled = false;

            // GÜN ETİKETİ KUTUNUN ÜST BANDINDA, kendi kapsülünde ve EN ÜSTTE.
            //
            // DERS (kontrast yoksa punto da yetmez): Eski panelde "Day 1"
            // 18 punto ve %65 saydam beyazdı; koyu lacivert kutunun üstünde
            // neredeyse görünmüyordu (kullanıcının "day yazısı anlaşılır
            // değil" dediği şey). Punto 30'a çıktı ve yazı kendi koyu
            // kapsülünün üstüne oturdu — bugünkü günde kapsül altın olunca
            // aynı yazı bu kez KOYU mürekkeple yazılıyor.
            //
            // Perdeden SONRA kuruluyor: alınmış günün kutusu kararıyor ama
            // gün numarası okunur kalıyor. Kararan bir tabloda hangi günün
            // alındığını göremezsen perdenin anlattığı şey yarım kalır.
            var tab = MenuPage.Capsule("DayTab", root, MenuPage.Darken(TileFuture, 0.42f));
            UiKit.Place(tab, 0.08f, 0.800f, 0.92f, 0.985f);

            var tabLabel = UiKit.CreateLabel("Text", tab.transform, $"DAY {index + 1}", 30, Ink);
            UiKit.Place(tabLabel, 0.04f, 0.06f, 0.96f, 0.94f);

            return new Tile
            {
                Day = index + 1,
                Root = root,
                Glow = glow,
                Rim = rim,
                Face = face,
                Tab = tab,
                Dim = dim,
                Check = check,
                TabLabel = tabLabel
            };
        }

        /// <summary>
        /// Bir günün hediyeleri yan yana çipler hâlinde: her çipin kendi
        /// simgesi ve kendi sayısı. Punto çip sayısına göre küçülüyor, yoksa
        /// üç çipe bölünen kutuda yazılar birbirine giriyor.
        /// </summary>
        static void BuildChips(RectTransform host, DailyGift gift)
        {
            var chips = new List<(string Icon, string Amount)>(3);
            if (gift.Coins > 0) chips.Add((Art.Coin, gift.Coins.ToString()));
            if (gift.Lives > 0) chips.Add((Art.Heart, "x" + gift.Lives));
            if (!string.IsNullOrEmpty(gift.PowerUp))
                chips.Add((PowerUpIcon(gift.PowerUp), "x1"));
            if (chips.Count == 0) return;

            float slot = 1f / chips.Count;
            int size = chips.Count == 1 ? 42 : chips.Count == 2 ? 32 : 30;

            for (int i = 0; i < chips.Count; i++)
            {
                var cell = UiKit.CreateRect("Chip_" + i, host);
                UiKit.Place(cell, i * slot, 0f, (i + 1) * slot, 1f);

                // `CreateIcon` en-boy oranını koruyor: dar bir hücrede simge
                // eziliyor değil, küçülüyor.
                var icon = UiKit.CreateIcon("Icon", cell, UiSkin.Get(chips[i].Icon));
                UiKit.Place(icon, 0.08f, 0.34f, 0.92f, 1f);

                var amount = UiKit.CreateTitle("Amount", cell, chips[i].Amount, size,
                    Ink, InkEdge);
                UiKit.Place(amount, 0f, 0.01f, 1f, 0.32f);
            }
        }

        /// <summary>Yardımcının kendi simgesi; tanınmayan ad sandığa düşer.</summary>
        static string PowerUpIcon(string kind)
        {
            if (kind == nameof(BlockOut.Core.PowerUpKind.Clock))  return Art.Clock;
            if (kind == nameof(BlockOut.Core.PowerUpKind.Rocket)) return Art.Rocket;
            if (kind == nameof(BlockOut.Core.PowerUpKind.Ufo))    return Art.Ufo;
            return Art.Chest;
        }

        // ----------------------------------------------------------- davranış

        /// <summary>Ödül varsa paneli açar; yoksa hiçbir şey yapmaz.</summary>
        public void ShowIfAvailable()
        {
            if (!MetaServices.Ready || MetaServices.Daily == null) return;
            if (!MetaServices.Daily.Available) return;

            Refresh();
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            GameKit.FX.Juice.Run(GameKit.FX.Juice.PopIn(_card, 0.34f));
        }

        public void Hide() => _root.gameObject.SetActive(false);

        void Refresh()
        {
            if (!MetaServices.Ready || MetaServices.Daily == null) return;

            // `PendingDay` İKİ FARKLI ŞEY SÖYLÜYOR.
            //
            // DERS (bir özelliğin anlamı duruma göre değişiyorsa, durumu da
            // sor): Ödül BEKLERKEN `PendingDay` bugün alınacak günü verir —
            // ondan öncekiler alınmıştır. Ödül ALINDIKTAN sonra ise aynı
            // özellik `DailyStreak`'e düşer, yani AZ ÖNCE ALINAN günü verir.
            // "alınmış = gün < pending" kuralı bu ikinci durumda yanlış: ödül
            // alındıktan sonra 3. gün hâlâ altın "bugün" kutusu olarak
            // kalıyor, tik hiç gelmiyordu. Oyuncu CLAIM'e basıp hiçbir şeyin
            // değişmediğini görüyor — panelin anlattığı tek hikâye kırılıyor.
            var daily = MetaServices.Daily;
            int pending = daily.PendingDay;
            bool waiting = daily.Available;

            int claimedThrough = waiting ? pending - 1 : pending;
            int highlight = waiting ? pending : 0;      // alındıysa vurgu yok

            foreach (var tile in _tiles)
            {
                bool claimed = tile.Day <= claimedThrough;
                bool today   = tile.Day == highlight;
                bool grand   = tile.Day == DailyRewardService.Week.Length;

                var faceColor = today ? TileToday : claimed ? TileClaimed : TileFuture;
                tile.Face.color = faceColor;
                tile.Rim.color  = today ? Gold : MenuPage.Darken(faceColor, 0.45f);

                tile.Tab.color = today ? Gold : MenuPage.Darken(faceColor, 0.42f);
                tile.TabLabel.color = today ? GoldInk : claimed ? InkSoft : Ink;

                tile.Dim.enabled = claimed;
                tile.Check.enabled = claimed;

                // Huzme yalnız BUGÜN parlak; 7. gün alınmadığı sürece soluk
                // bir parıltı taşıyor — haftanın sonundaki ödül hep görünür
                // kalsın diye.
                float glow = today ? 0.55f : grand && !claimed ? 0.20f : 0f;
                tile.Glow.color = new Color(1f, 0.82f, 0.34f, glow);
                tile.Glow.enabled = glow > 0.001f;

                tile.Root.localScale = Vector3.one * (today ? 1.05f : 1f);
            }

            _claim.interactable = true;
            _claimLabel.text = "CLAIM";
            _subtitle.text = "Come back every day for a bigger prize!";
        }

        void OnClaim()
        {
            if (!MetaServices.Ready) return;

            int day = MetaServices.Daily.PendingDay;
            if (!MetaServices.Daily.Claim(MetaServices.Progress, MetaServices.Lives)) return;

            // Jeton sesi tek başına "bir şey aldın" der; hediye açılışının
            // kendi sesi olayı ödül gibi okutuyor.
            AudioService.RewardClaim();
            if (_tiles.Count >= day)
                GameKit.FX.Juice.Run(GameKit.FX.Juice.PunchScale(_tiles[day - 1].Root, 0.32f));

            Refresh();

            // ANA EKRAN DA TAZELENMELİ.
            //
            // DERS (bu projede beşinci tekrar): Kaydı değiştirmek yetmiyor.
            // Panel açıkken arkadaki jeton ve can sayaçları duruyor; oyuncu
            // "CLAIM" diyip 50 jeton alıyor ama üstteki çubukta sayı
            // kıpırdamıyordu. Zaten AÇIK olan bir ekran kendiliğinden
            // yenilenmez — açan taraf söylemek zorunda.
            HomeScreen.Instance?.Refresh();

            _claim.interactable = false;
            _claimLabel.text = "SEE YOU TOMORROW";

            // ZİNCİRİ SÖYLE. Alınan hediye zaten ekranda; oyuncunun yarın
            // dönmesini sağlayan bilgi hediye değil, KAÇ GÜNDÜR sürdüğü.
            // Kaybedilecek bir sayı olması, dönmek için tek başına sebeptir.
            _subtitle.text = day == 1
                ? "Streak started — day 2 is bigger!"
                : $"{day} day streak! Come back tomorrow.";
            GameKit.FX.Juice.Run(CloseSoon());
        }

        System.Collections.IEnumerator CloseSoon()
        {
            yield return new WaitForSecondsRealtime(1.4f);
            Hide();
            _claim.interactable = true;
        }
    }
}
