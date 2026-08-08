using BlockOut.Core;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Mağaza: jeton paketleri (gerçek para) ve yardımcı paketleri (jeton).
    ///
    /// DERS (ödeme kancası TAKILI ama BAĞLI DEĞİL): Jeton paketleri gerçek
    /// bir mağaza SDK'sı ister; burada düğmeler duruyor ama satın alma
    /// <see cref="OnPurchaseRequested"/> olayına bırakılıyor. Böylece ekran
    /// bugün çalışır (yardımcı satın alma gerçekten işler), ödeme entegrasyonu
    /// geldiğinde tek bir yere bağlanır ve UI'a hiç dokunulmaz.
    /// </summary>
    public sealed class StoreScreen : MonoBehaviour
    {
        /// <summary>Referans oyundaki jeton paketleri ve TL fiyatları.</summary>
        static readonly (int coins, string price)[] CoinPacks =
        {
            (1000,   "99,99 TL"),
            (5000,   "389,99 TL"),
            (10000,  "799,99 TL"),
            (20000,  "1.499,99 TL"),
            (60000,  "2.499,99 TL"),
            (100000, "4.999,99 TL")
        };

        /// <summary>Gerçek ödeme akışı buraya bağlanır (SDK entegrasyonu).</summary>
        public event System.Action<int, string> OnPurchaseRequested;

        TextMeshProUGUI _status;
        readonly System.Collections.Generic.List<UnityEngine.UI.Button> _purchaseButtons =
            new System.Collections.Generic.List<UnityEngine.UI.Button>();

        public static RectTransform Build(Transform parent)
        {
            var root = MenuShell.Screen(parent, "StoreScreen");
            var screen = root.gameObject.AddComponent<StoreScreen>();
            MenuShell.Header(root, "Mağaza");

            screen._status = UiKit.CreateLabel("Status", root, "", 26, UiKit.Coin);
            UiKit.Place(screen._status, 0.04f, 0.885f, 0.96f, 0.925f);

            screen.BuildSpecialOffer(root);

            // --- Yardımcı paketleri: jetonla, ANINDA çalışır ---
            var helpersTitle = UiKit.CreateLabel("HelpersTitle", root, "Yardımcılar", 32, UiKit.Ink);
            UiKit.Place(helpersTitle, 0.05f, 0.700f, 0.95f, 0.744f);

            // Yardımcı kartları: krem kart + ürünün kendi ikonu + yeşil satın al.
            string[] helperIcons = { Art.Clock, Art.Rocket, Art.Ufo };
            for (int i = 0; i < 3; i++)
            {
                var kind = (PowerUpKind)i;
                float x0 = 0.05f + i * 0.315f;

                var card = UiKit.CreateSlicedPanel($"Helper_{i}", root, UiSkin.Get(Art.PanelCard));
                UiKit.Place(card, x0, 0.490f, x0 + 0.29f, 0.690f);

                var icon = UiKit.CreateIcon("Icon", card.transform, UiSkin.Get(helperIcons[i]));
                UiKit.Place(icon, 0.16f, 0.42f, 0.84f, 0.94f);

                // Krem kartın üstünde beyaz yazı okunmaz; koyu kahve kullanılıyor.
                var name = UiKit.CreateLabel("Name", card.transform,
                    PowerUpInfo.Label(kind), 22, new Color(0.32f, 0.17f, 0.05f));
                UiKit.Place(name, 0.04f, 0.28f, 0.96f, 0.44f);

                var buy = UiKit.CreateTintedButton($"Buy_{i}", card.transform,
                    UiSkin.Get(Art.PanelCard), new Color(0.176f, 0.800f, 0.047f),
                    PowerUpInfo.Price(kind) + " J", 24, UiKit.Ink);
                UiKit.Place(buy, 0.06f, 0.05f, 0.94f, 0.27f);

                var captured = kind;
                buy.onClick.AddListener(() => screen.BuyHelper(captured));
            }

            // --- Jeton paketleri: gerçek para, SDK bekliyor ---
            var coinsTitle = UiKit.CreateLabel("CoinsTitle", root, "Jetonlar", 32, UiKit.Ink);
            UiKit.Place(coinsTitle, 0.05f, 0.428f, 0.95f, 0.474f);

            for (int i = 0; i < CoinPacks.Length; i++)
            {
                var (coins, price) = CoinPacks[i];
                int col = i % 2, row = i / 2;
                float x0 = 0.05f + col * 0.46f;
                float y1 = 0.420f - row * 0.128f, y0 = y1 - 0.116f;

                var card = UiKit.CreateSlicedPanel($"Pack_{i}", root, UiSkin.Get(Art.PanelCard));
                UiKit.Place(card, x0, y0, x0 + 0.44f, y1);

                // Paket büyüdükçe jeton yığını da büyür: fiyat farkını okumadan
                // önce göz hangisinin daha çok verdiğini görsün.
                var stack = UiKit.CreateIcon("Coins", card.transform, UiSkin.Get(Art.Coin));
                float grow = 0.30f + i * 0.025f;
                UiKit.Place(stack, 0.5f - grow * 0.5f, 0.46f, 0.5f + grow * 0.5f, 0.46f + grow * 1.45f);

                var amount = UiKit.CreateTitle("Amount", card.transform,
                    coins.ToString("N0"), 30, new Color(0.36f, 0.19f, 0.02f),
                    new Color(1f, 0.93f, 0.80f));
                UiKit.Place(amount, 0.05f, 0.30f, 0.95f, 0.48f);

                var buy = UiKit.CreateTintedButton($"Pay_{i}", card.transform,
                    UiSkin.Get(Art.PanelCard), new Color(0.420f, 0.310f, 0.878f), price, 22, UiKit.Ink);
                UiKit.Place(buy, 0.07f, 0.05f, 0.93f, 0.28f);

                int capturedCoins = coins;
                string capturedPrice = price;
                buy.onClick.AddListener(() => screen.RequestPurchase(capturedCoins, capturedPrice));
                screen._purchaseButtons.Add(buy);
            }

            var restore = UiKit.CreateTintedButton("Restore", root, UiSkin.Get(Art.PanelCard),
                new Color(0.420f, 0.310f, 0.878f), "Satın Alımları Geri Yükle", 24, UiKit.Ink);
            UiKit.Place(restore, 0.15f, 0.015f, 0.85f, 0.075f);
            restore.onClick.AddListener(() =>
            {
                screen._status.color = UiKit.Ink;
                screen._status.text = "Satın alımlar sorgulanıyor…";
                PurchaseService.Instance.Restore(message => screen._status.text = message);
            });

            return root;
        }

        void BuyHelper(PowerUpKind kind)
        {
            if (!MetaServices.Ready) return;

            int price = PowerUpInfo.Price(kind);
            var progress = MetaServices.Progress;
            if (!progress.TrySpendCoins(price))
            {
                _status.text = "Yeterli jeton yok.";
                return;
            }

            string id = kind.ToString().ToLowerInvariant();
            progress.SetPowerUpCount(id, progress.PowerUpCount(id) + 1);
            _status.text = PowerUpInfo.Label(kind) + " alındı! (" + progress.Coins + " J kaldı)";
        }

        /// <summary>
        /// Satın alma: onay → işlem → sonuç. Gerçek para geçmiyor ama akışın
        /// tamamı gerçek; ödeme SDK'sı bağlanınca değişecek tek yer
        /// <see cref="PurchaseService"/> olacak.
        /// </summary>
        /// <summary>
        /// Süreli özel teklif: normalden fazla jeton, aynı fiyata.
        ///
        /// DERS (kıtlık satın alma sebebidir): Aynı paketler her zaman orada
        /// duruyorsa alma kararı hep ertelenebilir. Süreli bir teklif kararı
        /// BUGÜNE çeker. Geri sayım gerçek: gün sonunda kayboluyor ve ertesi
        /// gün yenisi geliyor; sahte bir "acele et" yazısı oyuncunun güvenini
        /// bir kez kaybettirir.
        /// </summary>
        void BuildSpecialOffer(Transform root)
        {
            var card = UiKit.CreateSlicedPanel("Offer", root, UiSkin.Get(Art.PanelCard));
            UiKit.Place(card, 0.05f, 0.755f, 0.95f, 0.885f);

            var badge = UiKit.CreateRoundedPanel("Badge", card.transform,
                new Color(0.96f, 0.24f, 0.20f));
            badge.pixelsPerUnitMultiplier = 0.16f;
            UiKit.Place(badge, 0.02f, 0.74f, 0.34f, 1.06f);

            var badgeText = UiKit.CreateTitle("BadgeText", badge.transform, "%60 EK", 22,
                UiKit.Ink, new Color(0.40f, 0.05f, 0.03f));
            UiKit.Place(badgeText, 0.04f, 0.08f, 0.96f, 0.92f);

            var icon = UiKit.CreateIcon("Icon", card.transform, UiSkin.Get(Art.Chest));
            UiKit.Place(icon, 0.04f, 0.10f, 0.30f, 0.74f);

            var title = UiKit.CreateTitle("Title", card.transform, "Günün Fırsatı", 30,
                new Color(0.30f, 0.16f, 0.05f), new Color(1f, 0.94f, 0.84f));
            UiKit.Place(title, 0.32f, 0.52f, 0.70f, 0.92f);

            _offerTimer = UiKit.CreateLabel("Timer", card.transform, "", 22,
                new Color(0.55f, 0.30f, 0.10f));
            UiKit.Place(_offerTimer, 0.32f, 0.10f, 0.70f, 0.48f);

            var buy = UiKit.CreateTintedButton("Buy", card.transform,
                UiSkin.Get(Art.PanelCard), new Color(0.176f, 0.800f, 0.047f),
                "16.000  ·  99,99 TL", 22, UiKit.Ink);
            UiKit.Place(buy, 0.72f, 0.12f, 0.96f, 0.88f);
            buy.onClick.AddListener(() => RequestPurchase(16000, "99,99 TL"));
            _purchaseButtons.Add(buy);
        }

        TextMeshProUGUI _offerTimer;

        void Update()
        {
            if (_offerTimer == null) return;
            // Gün sonuna kalan süre — teklifin kıtlığı gerçek olmalı.
            var left = System.DateTime.Today.AddDays(1) - System.DateTime.Now;
            _offerTimer.text = $"bitişine {left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}";
        }

        void RequestPurchase(int coins, string price)
        {
            var purchases = PurchaseService.Instance;
            if (purchases.IsBusy) return;                 // çift tıklama koruması

            string productId = "coins_" + coins;
            SetBusy(true);
            _status.color = UiKit.Ink;
            _status.text = "Mağazaya bağlanılıyor…";

            purchases.Buy(productId, coins, price, outcome =>
            {
                SetBusy(false);
                switch (outcome)
                {
                    case PurchaseResult.Purchased:
                        _status.color = new Color(0.35f, 0.92f, 0.42f);
                        _status.text = $"{coins:N0} jeton hesabına eklendi!";
                        GameKit.FX.Juice.Run(GameKit.FX.Juice.PunchScale(_status.transform, 0.30f));
                        break;
                    case PurchaseResult.Failed:
                        _status.color = new Color(1f, 0.42f, 0.36f);
                        _status.text = "Ödeme tamamlanamadı. Tekrar deneyebilirsin.";
                        break;
                    default:
                        _status.color = UiKit.Ink;
                        _status.text = "Satın alma iptal edildi.";
                        break;
                }
            });

            OnPurchaseRequested?.Invoke(coins, price);
        }

        /// <summary>İşlem sürerken bütün satın alma düğmeleri kapanır.</summary>
        void SetBusy(bool busy)
        {
            foreach (var button in _purchaseButtons)
                if (button != null) button.interactable = !busy;
        }
    }
}
