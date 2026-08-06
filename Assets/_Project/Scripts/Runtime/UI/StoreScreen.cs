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

        public static RectTransform Build(Transform parent)
        {
            var root = MenuShell.Screen(parent, "StoreScreen");
            var screen = root.gameObject.AddComponent<StoreScreen>();
            MenuShell.Header(root, "Mağaza");

            screen._status = UiKit.CreateLabel("Status", root, "", 26, UiKit.Coin);
            UiKit.Place(screen._status, 0.04f, 0.885f, 0.96f, 0.925f);

            // --- Yardımcı paketleri: jetonla, ANINDA çalışır ---
            var helpersTitle = UiKit.CreateLabel("HelpersTitle", root, "Yardımcılar", 32, UiKit.Ink);
            UiKit.Place(helpersTitle, 0.05f, 0.80f, 0.95f, 0.85f);

            for (int i = 0; i < 3; i++)
            {
                var kind = (PowerUpKind)i;
                float x0 = 0.05f + i * 0.315f;
                var card = UiKit.CreateRoundedPanel($"Helper_{i}", root, UiKit.PanelDark);
                UiKit.Place(card, x0, 0.66f, x0 + 0.29f, 0.79f);

                var name = UiKit.CreateLabel("Name", card.transform,
                    PowerUpInfo.Label(kind), 24, UiKit.Ink);
                UiKit.Place(name, 0.05f, 0.62f, 0.95f, 0.95f);

                var buy = UiKit.CreateButton($"Buy_{i}", card.transform,
                    PowerUpInfo.Price(kind) + " J", 26, UiKit.Accent, UiKit.Ink);
                UiKit.Place(buy, 0.08f, 0.08f, 0.92f, 0.58f);

                var captured = kind;
                buy.onClick.AddListener(() => screen.BuyHelper(captured));
            }

            // --- Jeton paketleri: gerçek para, SDK bekliyor ---
            var coinsTitle = UiKit.CreateLabel("CoinsTitle", root, "Jetonlar", 32, UiKit.Ink);
            UiKit.Place(coinsTitle, 0.05f, 0.60f, 0.95f, 0.65f);

            for (int i = 0; i < CoinPacks.Length; i++)
            {
                var (coins, price) = CoinPacks[i];
                int col = i % 2, row = i / 2;
                float x0 = 0.05f + col * 0.46f;
                float y1 = 0.58f - row * 0.145f, y0 = y1 - 0.125f;

                var card = UiKit.CreateRoundedPanel($"Pack_{i}", root, UiKit.PanelDark);
                UiKit.Place(card, x0, y0, x0 + 0.44f, y1);

                var amount = UiKit.CreateTitle("Amount", card.transform,
                    coins.ToString("N0") + " J", 34, UiKit.Coin, UiKit.PanelDark);
                UiKit.Place(amount, 0.05f, 0.5f, 0.95f, 0.95f);

                var buy = UiKit.CreateButton($"Pay_{i}", card.transform, price, 24, UiKit.Panel, UiKit.Ink);
                UiKit.Place(buy, 0.08f, 0.08f, 0.92f, 0.48f);

                int capturedCoins = coins;
                string capturedPrice = price;
                buy.onClick.AddListener(() => screen.RequestPurchase(capturedCoins, capturedPrice));
            }

            var restore = UiKit.CreateButton("Restore", root, "Satın Alımları Geri Yükle", 26, UiKit.Panel, UiKit.Ink);
            UiKit.Place(restore, 0.15f, 0.015f, 0.85f, 0.075f);
            restore.onClick.AddListener(() =>
                screen._status.text = "Geri yükleme için mağaza bağlantısı gerekli.");

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

        void RequestPurchase(int coins, string price)
        {
            _status.text = "Ödeme sağlayıcısı henüz bağlı değil.";
            OnPurchaseRequested?.Invoke(coins, price);
        }
    }
}
