using System.Text;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Ana ekran: köy manzarası, üstte can/coin şeridi, altta tek büyük "OYNA".
    ///
    /// DERS (ana ekran BİR şey söyler): Önceki hâlinde burada 50 bölümlük bir
    /// ızgara vardı. Oyuncuya elli seçenek sunmak, hiçbir şey sunmamakla aynı
    /// kapıya çıkıyor: ekran kalabalık, asıl eylem (oynamaya devam et)
    /// kayboluyordu. Referans oyunda ana ekranda TEK bir yeşil düğme var; bölüm
    /// seçimi Yolculuk sekmesine ait. Izgarayı silmek özellik kaybı değil,
    /// özelliğin ait olduğu yere taşınması.
    ///
    /// DERS (ekran = durum + yansıtma): Bu sınıf hiçbir KURAL bilmiyor. Kaç can
    /// var, bölüm açık mı, coin kaç — hepsini meta servislerine soruyor ve
    /// yalnızca ekrana çiziyor.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        TextMeshProUGUI _coinLabel;
        TextMeshProUGUI _livesLabel;
        TextMeshProUGUI _timerLabel;
        TextMeshProUGUI _playLabel;
        TextMeshProUGUI _playSubLabel;
        Button _playButton;

        // Çöp üretmeyen metin: değer değişmedikçe yeni dize kurulmaz.
        readonly StringBuilder _scratch = new StringBuilder(32);
        int _shownLives = -1, _shownCoins = -1, _shownRefill = -2, _shownNext = -1;

        float _nextTick;

        void Start()
        {
            BuildUi();
            Refresh();
        }

        void Update()
        {
            // Can geri sayımı saniyede bir yeter; her karede kurmak savurganlık.
            if (Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 1f;

            if (MetaServices.Ready) MetaServices.Lives.Refresh();
            Refresh();
        }

        void BuildUi()
        {
            var canvas = UiKit.CreateCanvas("HomeCanvas");
            // Kanvası sahibinin altına al: menü kökü kapatılınca ekrandan da gitsin.
            canvas.transform.SetParent(transform, worldPositionStays: false);
            var root = UiKit.CreateSafeArea(canvas);

            // Manzara, GÜVENLİ ALANIN değil ekranın tamamını kaplamalı; çentiğin
            // altında zemin rengi görünmesin diye kanvasa doğrudan bağlanıyor.
            var cover = UiKit.CreateCover("Background", canvas.transform,
                UiSkin.Get(Art.MenuBack), UiKit.Background);
            cover.transform.SetAsFirstSibling();

            BuildTopBar(root);

            var title = UiKit.CreateTitle("Title", root, "BLOCK OUT!", 120,
                UiKit.Ink, new Color(0.18f, 0.12f, 0.42f));
            UiKit.Place(title, 0f, 0.80f, 1f, 0.90f);

            BuildPlayButton(root);
        }

        void BuildTopBar(Transform root)
        {
            var lives = Chip("LivesChip", root, 0.03f, 0.395f, Art.Heart);
            _livesLabel = UiKit.CreateTitle("Lives", lives, "", 46, UiKit.Ink, UiKit.PanelDark);
            UiKit.Place(_livesLabel, 0.30f, 0.34f, 0.98f, 1f);
            _timerLabel = UiKit.CreateLabel("Timer", lives, "", 26,
                new Color(1f, 1f, 1f, 0.75f));
            UiKit.Place(_timerLabel, 0.30f, 0.02f, 0.98f, 0.36f);

            // Kapsül 0.76'da bitiyor: artı düğmesi kapsülün sağından TAŞTIĞI için
            // daha geniş bırakılırsa dişliyle üst üste biniyor.
            var coins = Chip("CoinChip", root, 0.425f, 0.755f, Art.Coin);
            _coinLabel = UiKit.CreateTitle("Coins", coins, "", 46, UiKit.Ink, UiKit.PanelDark);
            UiKit.Place(_coinLabel, 0.28f, 0.05f, 0.80f, 0.95f);

            // Artı: mağazanın coin sekmesine götürür.
            var plus = UiKit.CreateSpriteButton("Plus", coins, UiSkin.Get(Art.Plus),
                null, 0, UiKit.Ink);
            UiKit.Place(plus, 0.78f, 0.02f, 1.16f, 0.98f);
            plus.onClick.AddListener(() => MenuShell.Instance?.Show("store"));

            var gear = UiKit.CreateSpriteButton("Gear", root, UiSkin.Get(Art.Gear),
                null, 0, UiKit.Ink);
            UiKit.Place(gear, 0.845f, 0.917f, 0.985f, 0.987f);
            gear.onClick.AddListener(() => MenuShell.Instance?.Show("settings"));
        }

        /// <summary>Üst bardaki kapsül: koyu panel + solda ikon.</summary>
        static Transform Chip(string name, Transform parent, float x0, float x1, string icon)
        {
            var chip = UiKit.CreateSlicedPanel(name, parent, UiSkin.Get(Art.PanelDark));
            UiKit.Place(chip, x0, 0.917f, x1, 0.987f);

            // İkon kapsülün SOL KENARINDAN taşar — referanstaki gibi, kapsüle
            // takılmış bir madalyon hissi verir.
            var badge = UiKit.CreateIcon("Icon", chip.transform, UiSkin.Get(icon));
            UiKit.Place(badge, -0.06f, -0.12f, 0.30f, 1.12f);

            return chip.transform;
        }

        void BuildPlayButton(Transform root)
        {
            _playButton = UiKit.CreateSpriteButton("Play", root, UiSkin.Get(Art.ButtonGreen),
                null, 0, UiKit.Ink);
            UiKit.Place(_playButton, 0.14f, 0.115f, 0.86f, 0.235f);
            _playButton.onClick.AddListener(PlayCurrent);

            // Yazılar butonun YÜZÜNE oturmalı. Sprite'ın alt kalınlığı yüksekliğin
            // ~%27'si; oradan aşağısı gölge sayılır ve yazı oraya taşarsa
            // "düğmeden düşmüş" görünür.
            var face = _playButton.transform.GetChild(0);
            _playLabel = UiKit.CreateTitle("PlayLabel", face, "OYNA", 62, UiKit.Ink,
                new Color(0.05f, 0.30f, 0.03f));
            UiKit.Place(_playLabel, 0.06f, 0.50f, 0.94f, 0.95f);

            _playSubLabel = UiKit.CreateLabel("PlaySub", face, "", 32,
                new Color(1f, 1f, 1f, 0.85f));
            UiKit.Place(_playSubLabel, 0.06f, 0.28f, 0.94f, 0.52f);
        }

        void Refresh()
        {
            if (!MetaServices.Ready)
            {
                if (_playLabel != null) _playLabel.text = "OYNA";
                return;
            }

            var lives = MetaServices.Lives;
            var progress = MetaServices.Progress;

            if (progress.Coins != _shownCoins)
            {
                _shownCoins = progress.Coins;
                _coinLabel.text = _scratch.Clear().Append(_shownCoins).ToString();
            }

            var refill = lives.TimeToNextLife;
            int refillSeconds = lives.IsFull ? -1 : Mathf.CeilToInt((float)refill.TotalSeconds);
            if (lives.Current != _shownLives || refillSeconds != _shownRefill)
            {
                _shownLives = lives.Current;
                _shownRefill = refillSeconds;

                _livesLabel.text = _scratch.Clear().Append(_shownLives).ToString();
                _timerLabel.text = refillSeconds < 0
                    ? "dolu"
                    : _scratch.Clear()
                        .Append(refill.Minutes / 10).Append(refill.Minutes % 10).Append(':')
                        .Append(refill.Seconds / 10).Append(refill.Seconds % 10).ToString();
            }

            int next = Mathf.Clamp(progress.HighestUnlockedIndex, 0,
                Mathf.Max(0, LevelCatalog.Count - 1));
            bool canPlay = lives.HasLife && LevelCatalog.Count > 0;
            if (next != _shownNext || canPlay != _playButton.interactable)
            {
                _shownNext = next;
                _playLabel.text = lives.HasLife ? "OYNA" : "CAN YOK";
                _playSubLabel.text = lives.HasLife
                    ? _scratch.Clear().Append("Bölüm ").Append(next + 1).ToString()
                    : "canın dolmasını bekle";
            }

            _playButton.interactable = canPlay;
            // Düğme kapalıyken sprite'ı soldur: renk geçişi kapalı olduğu için
            // etkileşimsizlik başka türlü anlaşılmıyor.
            if (_playButton.targetGraphic is Image face)
                face.color = canPlay ? Color.white : new Color(0.62f, 0.62f, 0.66f);
        }

        void PlayCurrent()
        {
            if (!MetaServices.Ready) { Play(0); return; }
            Play(MetaServices.Progress.HighestUnlockedIndex);
        }

        static void Play(int index)
        {
            if (MetaServices.Ready && !MetaServices.Lives.HasLife) return;
            AppRouter.PlayLevel(index);
        }
    }
}
