using System.Collections.Generic;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

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
    /// </summary>
    public sealed class DailyRewardPanel : MonoBehaviour
    {
        static readonly Color Periwinkle = new Color(0.353f, 0.322f, 0.784f);
        static readonly Color Ink        = new Color(1f, 0.98f, 0.94f);
        static readonly Color Claimed    = new Color(0.216f, 0.812f, 0.243f);

        readonly List<(Image card, Image check, int day)> _days =
            new List<(Image, Image, int)>();
        RectTransform _root;
        Image _card;
        Button _claim;
        TextMeshProUGUI _claimLabel;

        public static DailyRewardPanel Build(Transform parent)
        {
            var holder = UiKit.CreateRect("DailyReward", parent);
            UiKit.Place(holder, 0f, 0f, 1f, 1f);
            var panel = holder.gameObject.AddComponent<DailyRewardPanel>();
            panel._root = holder;

            var scrim = UiKit.CreatePanel("Scrim", holder, new Color(0.05f, 0.03f, 0.14f, 0.84f));
            scrim.raycastTarget = true;

            panel._card = UiKit.CreateSlicedPanel("Card", holder, UiSkin.Get(Art.PanelCard));
            UiKit.Place(panel._card, 0.07f, 0.28f, 0.93f, 0.74f);

            var title = UiKit.CreateTitle("Title", panel._card.transform, "DAILY REWARD", 46,
                new Color(0.30f, 0.16f, 0.05f), new Color(1f, 0.93f, 0.80f));
            UiKit.Place(title, 0.06f, 0.83f, 0.94f, 0.96f);

            // Yedi gün: ilk sıra dört, ikinci sıra üç + büyük 7. gün.
            for (int i = 0; i < 7; i++)
            {
                bool topRow = i < 4;
                int column = topRow ? i : i - 4;
                int columns = topRow ? 4 : 3;
                float width = 0.86f / columns;
                float x0 = 0.07f + column * width;
                float y1 = topRow ? 0.78f : 0.46f;

                var slot = UiKit.CreateSlicedPanel($"Day_{i + 1}", panel._card.transform,
                    UiSkin.Get(Art.PanelDark), Periwinkle);
                UiKit.Place(slot, x0 + 0.012f, y1 - 0.28f, x0 + width - 0.012f, y1);

                var gift = DailyRewardService.Week[i];
                var icon = UiKit.CreateIcon("Icon", slot.transform,
                    UiSkin.Get(gift.Lives > 0 ? Art.Heart
                             : gift.PowerUp != null ? Art.Chest : Art.Coin));
                UiKit.Place(icon, 0.18f, 0.34f, 0.82f, 0.90f);

                var amount = UiKit.CreateLabel("Amount", slot.transform,
                    gift.Coins.ToString(), 22, Ink);
                UiKit.Place(amount, 0.04f, 0.04f, 0.96f, 0.32f);

                var day = UiKit.CreateLabel("Day", slot.transform, $"Day {i + 1}", 18,
                    new Color(1f, 1f, 1f, 0.65f));
                UiKit.Place(day, 0.04f, 0.86f, 0.96f, 1.04f);

                // Alınmış günlerin üstünde yeşil tik.
                var check = UiKit.CreateIcon("Check", slot.transform,
                    UiSkin.Get(Art.Check), Color.white);
                UiKit.Place(check, 0.52f, 0.52f, 1.10f, 1.10f);
                check.enabled = false;

                panel._days.Add((slot, check, i + 1));
            }

            panel._claim = UiKit.CreateSpriteButton("Claim", panel._card.transform,
                UiSkin.Get(Art.ButtonGreen), "", 32, Ink);
            UiKit.Place(panel._claim, 0.16f, 0.05f, 0.84f, 0.17f);
            panel._claimLabel = panel._claim.GetComponentInChildren<TextMeshProUGUI>();
            panel._claim.onClick.AddListener(panel.OnClaim);

            var close = UiKit.CreateIconButton("Close", holder, UiSkin.Get(Art.Plus));
            UiKit.Place(close, 0.845f, 0.700f, 0.945f, 0.760f);
            close.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);   // artı → çarpı
            close.onClick.AddListener(panel.Hide);

            holder.gameObject.SetActive(false);
            return panel;
        }

        /// <summary>Ödül varsa paneli açar; yoksa hiçbir şey yapmaz.</summary>
        public void ShowIfAvailable()
        {
            if (!MetaServices.Ready || MetaServices.Daily == null) return;
            if (!MetaServices.Daily.Available) return;

            Refresh();
            _root.gameObject.SetActive(true);
            GameKit.FX.Juice.Run(GameKit.FX.Juice.PopIn(_card.transform, 0.34f));
        }

        public void Hide() => _root.gameObject.SetActive(false);

        void Refresh()
        {
            var daily = MetaServices.Daily;
            int pending = daily.PendingDay;

            foreach (var (card, check, day) in _days)
            {
                bool claimed = day < pending;
                bool today = day == pending;

                card.color = today ? Claimed : claimed ? new Color(0.22f, 0.20f, 0.34f)
                                                       : Periwinkle;
                check.enabled = claimed;
                card.transform.localScale = Vector3.one * (today ? 1.06f : 1f);
            }

            var gift = DailyRewardService.Week[pending - 1];
            _claimLabel.text = $"CLAIM {gift.Coins}";
        }

        void OnClaim()
        {
            if (!MetaServices.Ready) return;

            int day = MetaServices.Daily.PendingDay;
            if (!MetaServices.Daily.Claim(MetaServices.Progress, MetaServices.Lives)) return;

            // Jeton sesi tek başına "bir şey aldın" der; hediye açılışının
            // kendi sesi olayı ödül gibi okutuyor.
            AudioService.RewardClaim();
            if (_days.Count >= day)
                GameKit.FX.Juice.Run(GameKit.FX.Juice.PunchScale(
                    _days[day - 1].card.transform, 0.32f));

            Refresh();
            _claim.interactable = false;
            _claimLabel.text = "SEE YOU TOMORROW";
            GameKit.FX.Juice.Run(CloseSoon());
        }

        System.Collections.IEnumerator CloseSoon()
        {
            yield return new WaitForSecondsRealtime(1.1f);
            Hide();
            _claim.interactable = true;
        }
    }
}
