using System.Text;
using BlockOut.Core;
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
    /// Ana ekran: köy manzarası, üstte tek bir kaynak şeridi, altta seviye düğmesi.
    ///
    /// DERS (ana ekran BİR şey söyler): Burada eskiden 50 bölümlük bir ızgara
    /// vardı. Referans oyunda ana ekranda TEK bir düğme var ve üstünde oynanacak
    /// seviyenin numarası yazıyor — oyuncuya "nereye geldin" ve "sıradaki ne"
    /// bilgisini aynı anda verir. Elli seçenek sunmak, hiçbir şey sunmamakla
    /// aynı kapıya çıkıyordu.
    ///
    /// DERS (üst bar niçin TEK kapsül): Her sayacı ayrı bir koyu kutuya koymak,
    /// ekranın üstünü birbirinden bağımsız lekelerle doldurur. Referansta jeton
    /// ve can TEK bir koyu kapsülün içinde; avatar ve dişli o kapsülün dışında
    /// kendi düğmeleri olarak durur. Böylece "kaynaklarım" ve "düğmelerim"
    /// görsel olarak ayrışır.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        static readonly Color Periwinkle = new Color(0.353f, 0.322f, 0.784f);
        static readonly Color CoinInk    = new Color(1f, 0.98f, 0.94f);

        TextMeshProUGUI _coinLabel;
        TextMeshProUGUI _livesLabel;
        TextMeshProUGUI _livesTimer;
        TextMeshProUGUI _levelLabel;
        TextMeshProUGUI _difficultyLabel;
        TextMeshProUGUI _avatarInitial;
        RectTransform _rewardRibbon;
        TextMeshProUGUI _rewardLabel;
        Image _playFace;
        Button _playButton;
        GameObject _adRow;
        TextMeshProUGUI _adOfferSub;

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
            if (Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 1f;

            if (MetaServices.Ready) MetaServices.Lives.Refresh();
            Refresh();
        }

        void BuildUi()
        {
            var canvas = UiKit.CreateCanvas("HomeCanvas");
            canvas.transform.SetParent(transform, worldPositionStays: false);
            var root = UiKit.CreateSafeArea(canvas);

            // Manzara, güvenli alanın değil EKRANIN tamamını kaplamalı; çentiğin
            // altında zemin rengi görünmesin diye kanvasa doğrudan bağlanıyor.
            var cover = UiKit.CreateCover("Background", canvas.transform,
                UiSkin.Get(Art.MenuBack), UiKit.Background);
            cover.transform.SetAsFirstSibling();

            BuildCharacters(root);
            BuildTopBar(root);
            BuildPlayButton(root);
            BuildAdOffer(root);
        }

        /// <summary>
        /// Ana ekranın ortasındaki karakterler.
        ///
        /// DERS (boş orta alan ölü alandır): Manzara güzel ama üstünde hiçbir
        /// şey olmayan bir ekran "yükleniyor" gibi durur. Referans oyunda o
        /// alanda karakterler var ve bütün ekranın kimliğini onlar taşıyor.
        /// Karakterler oyuna kural eklemez; ekranın SAHİPLİ görünmesini sağlar.
        /// </summary>
        void BuildCharacters(Transform root)
        {
            var art = UiSkin.Get(Art.Characters);
            if (art == null) return;

            var group = UiKit.CreateIcon("Characters", root, art);
            // Alt kenarı OYNA düğmesinin hemen üstünde: karakterler yolun
            // üzerinde duruyormuş gibi görünsün, havada asılı değil.
            UiKit.Place(group, 0.04f, 0.255f, 0.96f, 0.56f);

            // Hafif nefes alma: tamamen hareketsiz bir görsel, arkasındaki
            // manzaranın parçası sanılıyor.
            GameKit.FX.Juice.Run(Breathe(group.transform));
        }

        static System.Collections.IEnumerator Breathe(Transform target)
        {
            Vector3 baseScale = target.localScale;
            float time = 0f;
            while (target != null)
            {
                time += Time.unscaledDeltaTime;
                float pulse = Mathf.Sin(time * 1.4f) * 0.012f;
                target.localScale = baseScale * (1f + pulse);
                yield return null;
            }
        }

        // ---------------------------------------------------------------- üst bar

        void BuildTopBar(Transform root)
        {
            const float top = 0.988f, bottom = 0.918f;

            BuildAvatar(root, 0.028f, 0.190f, bottom, top);

            // Jeton ve can TEK kapsülde.
            var pill = UiKit.CreateSlicedPanel("ResourcePill", root, UiSkin.Get(Art.PanelDark));
            UiKit.Place(pill, 0.205f, bottom, 0.815f, top);

            _coinLabel = Counter(pill.transform, Art.Coin, 0.015f, 0.475f, out var coinPlus);
            coinPlus.onClick.AddListener(() => MenuShell.Instance?.Show("store"));

            _livesLabel = Counter(pill.transform, Art.Heart, 0.500f, 0.985f, out var lifePlus);
            lifePlus.onClick.AddListener(() => MenuShell.Instance?.Show("store"));

            // Can durumu ("Dolu" / geri sayım) SAYININ ALTINA yazılır. Yanına
            // koymayı denedim; jeton sayısı üç haneye çıkınca artı düğmesinin
            // altına giriyordu.
            _livesTimer = UiKit.CreateLabel("LivesTimer", pill.transform, "", 24,
                new Color(1f, 1f, 1f, 0.8f));
            UiKit.Place(_livesTimer, 0.615f, 0.04f, 0.845f, 0.42f);

            var gear = SquareButton(root, "Gear", Art.Gear, 0.832f, 0.985f, bottom, top);
            gear.onClick.AddListener(() => MenuShell.Instance?.Show("settings"));
        }

        /// <summary>
        /// Kapsül içindeki bir sayaç: ikon + sayı + yeşil artı düğmesi.
        /// Artı düğmesi <paramref name="plus"/> ile dışarı verilir; ne yapacağı
        /// çağırana ait.
        /// </summary>
        TextMeshProUGUI Counter(Transform pill, string icon, float x0, float x1, out Button plus)
        {
            float width = x1 - x0;

            var badge = UiKit.CreateIcon($"Icon_{icon}", pill, UiSkin.Get(icon));
            UiKit.Place(badge, x0, -0.14f, x0 + width * 0.26f, 1.14f);

            var label = UiKit.CreateTitle($"Value_{icon}", pill, "", 40, CoinInk,
                new Color(0.12f, 0.09f, 0.28f));
            UiKit.Place(label, x0 + width * 0.23f, 0.30f, x0 + width * 0.72f, 0.98f);

            plus = UiKit.CreateSpriteButton($"Plus_{icon}", pill, UiSkin.Get(Art.Plus),
                null, 0, CoinInk);
            UiKit.Place(plus, x0 + width * 0.70f, -0.06f, x0 + width, 1.06f);

            return label;
        }

        /// <summary>
        /// Avatar: çerçeveli, tıklanınca profile götüren kare düğme.
        ///
        /// Şimdilik oyuncunun baş harfi gösteriliyor — kendi karakter görselimiz
        /// üretilene kadar boş bir kare bırakmaktansa anlamlı bir şey durur.
        /// </summary>
        void BuildAvatar(Transform root, float x0, float x1, float y0, float y1)
        {
            var frame = UiKit.CreateSpriteButton("Avatar", root, UiSkin.Get(Art.PanelDark),
                null, 0, CoinInk);
            UiKit.Place(frame, x0, y0, x1, y1);
            if (frame.targetGraphic is Image face) face.color = Periwinkle;

            var inner = UiKit.CreateSlicedPanel("Inner", frame.transform,
                UiSkin.Get(Art.PanelDark), new Color(0.16f, 0.13f, 0.40f));
            UiKit.Place(inner, 0.12f, 0.12f, 0.88f, 0.88f);

            // Portre varsa o, yoksa oyuncunun baş harfi.
            var portrait = UiSkin.Get(Art.Avatar);
            if (portrait != null)
            {
                var face2 = UiKit.CreateIcon("Portrait", inner.transform, portrait);
                // Çerçeveden biraz TAŞAR: bust görseli aşağıdan kesik olduğu
                // için tam oturtulunca kafası küçük kalıyor.
                UiKit.Place(face2, -0.06f, -0.02f, 1.06f, 1.24f);
            }
            else
            {
                _avatarInitial = UiKit.CreateTitle("Initial", inner.transform, "?", 52,
                    CoinInk, new Color(0.12f, 0.09f, 0.28f));
                UiKit.Place(_avatarInitial, 0f, 0f, 1f, 1f);
            }

            frame.onClick.AddListener(() => MenuShell.Instance?.Show("profile"));
        }

        /// <summary>
        /// Kare düğme: koyu panel sprite'ı mor tonlanıp üstüne ikon konur.
        ///
        /// Buton sprite'ları (btn_*) 3:1 orana göre çizildi; kareye sıkıştırınca
        /// 9-dilim payları (yanlarda 90px) genişliğin tamamını yiyip yüzü
        /// eziyor. Kare düğmeler için payları eşit olan panel sprite'ı doğru.
        /// </summary>
        static Button SquareButton(Transform root, string name, string icon,
            float x0, float x1, float y0, float y1)
        {
            var button = UiKit.CreateSpriteButton(name, root, UiSkin.Get(Art.PanelDark),
                null, 0, CoinInk);
            UiKit.Place(button, x0, y0, x1, y1);
            if (button.targetGraphic is Image face) face.color = Periwinkle;

            var glyph = UiKit.CreateIcon("Icon", button.transform, UiSkin.Get(icon));
            UiKit.Place(glyph, 0.14f, 0.14f, 0.86f, 0.86f);
            return button;
        }

        // ------------------------------------------------------------ oyna düğmesi

        void BuildPlayButton(Transform root)
        {
            _playButton = UiKit.CreateSpriteButton("Play", root, UiSkin.Get(Art.ButtonGreen),
                null, 0, CoinInk);
            UiKit.Place(_playButton, 0.16f, 0.125f, 0.84f, 0.255f);
            _playButton.onClick.AddListener(PlayCurrent);
            _playFace = _playButton.targetGraphic as Image;

            var face = _playButton.transform.GetChild(0);

            _levelLabel = UiKit.CreateTitle("Level", face, "", 68, CoinInk,
                new Color(0.10f, 0.06f, 0.22f));
            UiKit.Place(_levelLabel, 0.06f, 0.44f, 0.94f, 0.94f);

            _difficultyLabel = UiKit.CreateTitle("Difficulty", face, "", 34,
                new Color(0.88f, 0.90f, 1f), new Color(0.10f, 0.06f, 0.22f));
            UiKit.Place(_difficultyLabel, 0.06f, 0.22f, 0.94f, 0.46f);

            // Ödül şeridi düğmenin ÜST KENARINA binerek durur; ayrı bir kutu
            // gibi değil, düğmeye takılmış bir etiket gibi okunsun.
            var ribbonSprite = UiSkin.Get(Art.Ribbon);
            var ribbon = ribbonSprite != null
                ? UiKit.CreateSlicedPanel("Ribbon", _playButton.transform, ribbonSprite)
                : UiKit.CreateSlicedPanel("Ribbon", _playButton.transform,
                    UiSkin.Get(Art.PanelDark), new Color(0.94f, 0.55f, 0.10f));
            UiKit.Place(ribbon, 0.26f, 0.80f, 0.74f, 1.28f);
            _rewardRibbon = ribbon.rectTransform;

            _rewardLabel = UiKit.CreateTitle("RibbonText", ribbon.transform, "", 30,
                CoinInk, new Color(0.45f, 0.22f, 0.02f));
            UiKit.Place(_rewardLabel, 0.04f, 0.08f, 0.96f, 0.92f);
        }

        /// <summary>
        /// "Can yok" durumunda çıkan ödüllü reklam teklifi.
        ///
        /// DERS (reklam RAHATSIZ ETMEZ, KURTARIR): Reklamı oyuncunun keyfi
        /// yerindeyken önüne koymak onu kaçırır. Aynı reklamı "oynayamıyorum"
        /// dediği anda sunmak ise bir çözüm gibi görünür — hem izlenme oranı
        /// yükselir hem oyuncu rahatsız olmaz. Bu yüzden teklif yalnızca can
        /// bittiğinde ve tam OYNA düğmesinin yerinde beliriyor.
        /// </summary>
        void BuildAdOffer(Transform root)
        {
            _adRow = UiKit.CreateRect("AdOffer", root).gameObject;
            UiKit.Place((RectTransform)_adRow.transform, 0.16f, 0.125f, 0.84f, 0.255f);

            var button = UiKit.CreateSpriteButton("WatchAd", _adRow.transform,
                UiSkin.Get(Art.ButtonPurple), null, 0, CoinInk);
            UiKit.Place(button, 0f, 0f, 1f, 1f);

            var face = button.transform.GetChild(0);
            var title = UiKit.CreateTitle("Title", face, "REKLAM İZLE", 44, CoinInk,
                new Color(0.16f, 0.06f, 0.30f));
            UiKit.Place(title, 0.06f, 0.44f, 0.94f, 0.94f);

            _adOfferSub = UiKit.CreateLabel("Sub", face, "+1 can kazan", 30,
                new Color(1f, 1f, 1f, 0.85f));
            UiKit.Place(_adOfferSub, 0.06f, 0.22f, 0.94f, 0.46f);

            button.onClick.AddListener(() =>
            {
                button.interactable = false;
                Services.FakeAdScreen.Instance.ShowRewarded("free_life", outcome =>
                {
                    button.interactable = true;
                    if (outcome != GameKit.Services.RewardedResult.Completed) return;
                    if (!MetaServices.Ready) return;

                    MetaServices.Lives.Grant(1);
                    _shownLives = -1;              // sayaç hemen tazelensin
                    Refresh();
                });
            });

            _adRow.SetActive(false);
        }

        // ---------------------------------------------------------------- tazeleme

        void Refresh()
        {
            if (!MetaServices.Ready)
            {
                if (_levelLabel != null) _levelLabel.text = "OYNA";
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
                _livesTimer.text = refillSeconds < 0
                    ? "Dolu"
                    : _scratch.Clear()
                        .Append(refill.Minutes / 10).Append(refill.Minutes % 10).Append(':')
                        .Append(refill.Seconds / 10).Append(refill.Seconds % 10).ToString();
            }

            if (_avatarInitial != null)
                _avatarInitial.text = string.IsNullOrEmpty(MetaServices.PlayerName)
                    ? "?" : MetaServices.PlayerName.Substring(0, 1).ToUpperInvariant();

            int next = Mathf.Clamp(progress.HighestUnlockedIndex, 0,
                Mathf.Max(0, LevelCatalog.Count - 1));
            bool canPlay = lives.HasLife && LevelCatalog.Count > 0;

            if (next != _shownNext)
            {
                _shownNext = next;
                ApplyDifficulty(LevelCatalog.DifficultyAt(next), next);
            }

            _playButton.interactable = canPlay;

            // Can bittiğinde OYNA düğmesi ölü bir tuşa dönmemeli: aynı yerde
            // "reklam izle, can al" teklifi çıkıyor. Oyuncunun oturumu burada
            // biter ya da devam eder; boş bir düğme bırakmak bitmesini seçmektir.
            _adRow.SetActive(!canPlay && LevelCatalog.Count > 0);
            if (!canPlay)
            {
                _levelLabel.text = "CAN YOK";
                _difficultyLabel.text = "canın dolmasını bekle";
                if (_playFace != null) _playFace.color = new Color(0.62f, 0.62f, 0.66f);
            }
        }

        void ApplyDifficulty(LevelDifficulty difficulty, int index)
        {
            _levelLabel.text = _scratch.Clear().Append("Seviye ").Append(index + 1).ToString();

            string label = LevelDifficultyRule.Label(difficulty);
            _difficultyLabel.text = label;
            _difficultyLabel.gameObject.SetActive(!string.IsNullOrEmpty(label));

            // Zorluk arttıkça düğmenin görseli değişir: yeşil → mor → kırmızı.
            // Renk tonlamak yerine SPRITE değiştiriliyor; parlak plastik yüzeyin
            // ışık lekesi de o rengin tonunda olsun diye.
            string sprite = difficulty == LevelDifficulty.SuperHard ? Art.ButtonRed
                          : difficulty == LevelDifficulty.Hard ? Art.ButtonPurple
                          : Art.ButtonGreen;
            if (_playFace != null)
            {
                _playFace.sprite = UiSkin.Get(sprite);
                _playFace.color = Color.white;
            }

            int multiplier = LevelDifficultyRule.RewardMultiplier(difficulty);
            bool showRibbon = multiplier > 1;
            _rewardRibbon.gameObject.SetActive(showRibbon);
            if (showRibbon)
                _rewardLabel.text = _scratch.Clear().Append("Ödüller x").Append(multiplier).ToString();

            // Yazı yerleşimi içeriğe göre. Zorluk etiketi yoksa (normal bölüm)
            // seviye yazısı düğmenin ORTASINA oturur; alt satır boş kaldığında
            // tek satırın yukarıda asılı durması dengesiz görünüyordu.
            bool hasDifficulty = !string.IsNullOrEmpty(label);
            float top = showRibbon ? 0.86f : 0.92f;
            float bottom = hasDifficulty ? (showRibbon ? 0.40f : 0.44f) : 0.24f;
            UiKit.Place(_levelLabel, 0.06f, bottom, 0.94f, top);
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
