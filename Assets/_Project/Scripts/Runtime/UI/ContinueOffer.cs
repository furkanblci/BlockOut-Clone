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
    /// konuyor: altın kronometre, "+30 SECONDS", yeşil "Add Time 900".
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

            BuildCoinReadout(page, out _coins1, 0.944f, 0.984f);
            BuildClose(page, 0.894f, 0.942f, () => ShowStage(2));

            // Başlık krem dolgulu, KIRMIZI konturlu. Paylaşılan başlık materyali
            // tek tek kontur ayarını yok saydığı için SetOutline şart
            // (bu projede yedinci tuzak).
            var title = UiKit.CreateTitle("Title", page, "Time's Up!", 82,
                TitleCream, TitleRed);
            UiKit.Place(title, 0.146f, 0.742f, 0.859f, 0.805f);
            UiKit.SetOutline(title, TitleRed, 0.34f);

            // Referansta ALTIN bir kronometre var; elimizdeki en yakın görsel
            // süre yardımcısının YEŞİL çalar saati. Aynı şeyi anlatıyor ve
            // oyuncu onu zaten "süre" diye tanıyor.
            //
            // DERS (boyama ÇARPMADIR): Burası önce `CoinGold` ile boyanmıştı.
            // Yeşil bir görseli altına boyayamazsın — çarpım çamurlu bir yeşil
            // veriyor ve sonuç ne referanstaki altın oluyor ne de bizim temiz
            // yeşilimiz. Boyama kaldırıldı; gerçek altın kronometre görseli
            // istendi (docs/art-prompts.md §10).
            var clock = UiKit.CreateIcon("Clock", page, UiSkin.Get(Art.Clock));
            UiKit.Place(clock, 0.219f, 0.416f, 0.740f, 0.675f);

            var amount = UiKit.CreateTitle("Amount", page, "+30 SECONDS", 56, Ink, TitleEdge);
            UiKit.Place(amount, 0.214f, 0.326f, 0.789f, 0.371f);

            _price1 = BuildPriceButton(page, "Add Time", 0.172f, 0.162f, 0.833f, 0.250f);
            return page;
        }

        // ------------------------------------------------------------ aşama 2

        RectTransform BuildStage2(Transform holder)
        {
            var page = UiKit.CreateRect("Stage2_Continue", holder);
            UiKit.Place(page, 0f, 0f, 1f, 1f);

            BuildCoinReadout(page, out _coins2, 0.815f, 0.855f);

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
        void BuildCoinReadout(Transform page, out TextMeshProUGUI label, float y0, float y1)
        {
            var icon = UiKit.CreateIcon("Coin", page, UiSkin.Get(Art.Coin), CoinGold);
            UiKit.Place(icon, 0.035f, y0, 0.115f, y1);

            label = UiKit.CreateTitle("Coins", page, "", 34, Ink, TitleEdge);
            label.alignment = TextAlignmentOptions.Left;
            UiKit.Place(label, 0.125f, y0, 0.420f, y1);
        }

        void BuildClose(Transform page, float y0, float y1, UnityEngine.Events.UnityAction onClick)
        {
            var root = UiKit.CreateRect("Close", page);
            UiKit.Place(root, 0.854f, y0, 0.958f, y1);

            // DAİRE `CreateIcon` İLE: düz bir `Image` sprite'ı kutuya gerer ve
            // kutu kare olmadığı anda daire elipse döner (bkz. MenuPage.Close).
            var ringImage = UiKit.CreateIcon("Ring", root, UiSprites.Circle,
                new Color(0.42f, 0.05f, 0.09f));
            UiKit.Place(ringImage, 0f, 0f, 1f, 1f);

            var faceImage = UiKit.CreateIcon("Face", root, UiSprites.Circle,
                new Color(0.855f, 0.145f, 0.180f));
            UiKit.Place(faceImage, 0.07f, 0.09f, 0.93f, 0.95f);

            var cross = UiKit.CreateIcon("Cross", faceImage.transform, UiSprites.Cross);
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
            var button = UiKit.CreateSpriteButton("Buy", page, UiSkin.Get(Art.ButtonGreen),
                string.Empty, 44, Ink);
            UiKit.Place(button, x0, y0, x1, y1);
            button.onClick.AddListener(Buy);

            var face = button.transform;

            var label = UiKit.CreateTitle("Action", face, action, 46, Ink, new Color(0.04f, 0.24f, 0.02f));
            label.alignment = TextAlignmentOptions.Right;
            UiKit.Place(label, 0.08f, 0.20f, 0.55f, 0.80f);

            var coin = UiKit.CreateIcon("Coin", face, UiSkin.Get(Art.Coin), CoinGold);
            UiKit.Place(coin, 0.585f, 0.26f, 0.695f, 0.74f);

            var price = UiKit.CreateTitle("Price", face, Price.ToString(), 46, Ink,
                new Color(0.04f, 0.24f, 0.02f));
            price.alignment = TextAlignmentOptions.Left;
            UiKit.Place(price, 0.715f, 0.20f, 0.95f, 0.80f);

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
