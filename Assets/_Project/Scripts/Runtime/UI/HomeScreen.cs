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
            UiKit.Place(group, 0.03f, 0.215f, 0.97f, 0.545f);

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

        /// <summary>
        /// Üst bar: avatar · kaynak kapsülü · ayarlar.
        ///
        /// DERS (oranlar referanstan ÖLÇÜLDÜ): İlk hâlde her parça göz kararı
        /// yerleştirilmişti ve üst şerit "uyumsuz" görünüyordu. Referans karesi
        /// 946 piksel genişliğinde; oradaki ölçüler oranlanarak alındı:
        /// avatar 45-165 (%4.8-17.4), kapsül 190-775 (%20-82), dişli 795-885
        /// (%84-93.5). Avatar kapsülden BİRAZ DAHA UZUN — altına ve üstüne
        /// taşar; bu, referanstaki "madalyon" hissini veren şey.
        /// </summary>
        void BuildTopBar(Transform root)
        {
            const float top = 0.982f, bottom = 0.926f;   // kapsül şeridi

            BuildAvatar(root, 0.040f, 0.174f, bottom - 0.012f, top + 0.012f);

            var pill = UiKit.CreateSlicedPanel("ResourcePill", root, UiSkin.Get(Art.PanelDark));
            UiKit.Place(pill, 0.200f, bottom, 0.820f, top);
            // Kapsül yüksekliği 108 piksel; panel görselinin 9-dilim payı her
            // kenarda 60 piksel olduğu için dikeyde 120 pay 108'e sıkışıp
            // yüzeyi eziyordu. Payı küçültmek kapsülü düzgün bırakıyor.
            pill.pixelsPerUnitMultiplier = 1.6f;

            _coinLabel = Counter(pill.transform, Art.Coin, 0.020f, 0.480f, out var coinPlus);
            coinPlus.onClick.AddListener(() => MenuShell.Instance?.Show("store"));

            _livesLabel = Counter(pill.transform, Art.Heart, 0.510f, 0.975f, out var lifePlus);
            lifePlus.onClick.AddListener(() => MenuShell.Instance?.Show("store"));

            // Can durumu sayının ALTINDA: yanına koymak, jeton üç haneye
            // çıkınca artı düğmesinin altına giriyordu.
            _livesTimer = UiKit.CreateLabel("LivesTimer", pill.transform, "", 22,
                new Color(1f, 1f, 1f, 0.78f));
            UiKit.Place(_livesTimer, 0.615f, 0.02f, 0.860f, 0.40f);

            var gear = SquareButton(root, "Gear", Art.Gear, 0.846f, 0.960f, bottom, top);
            gear.onClick.AddListener(() => MenuShell.Instance?.Show("settings"));
        }

        /// <summary>
        /// Kapsül içindeki bir sayaç: ikon + sayı + yeşil artı düğmesi.
        ///
        /// DERS (ikon gerilmez): Artı düğmesi önce normal düğme olarak
        /// kuruluyordu; yüzeyi 9-dilim gibi gerilip YUMURTAYA dönüyordu. İkonun
        /// kendisi düğme yüzeyi olduğunda en-boy oranı korunmalı.
        /// </summary>
        TextMeshProUGUI Counter(Transform pill, string icon, float x0, float x1, out Button plus)
        {
            float width = x1 - x0;

            // İkon kapsülün üstüne ve altına hafifçe taşar — referanstaki gibi
            // kapsüle takılmış bir madalyon.
            var badge = UiKit.CreateIcon($"Icon_{icon}", pill, UiSkin.Get(icon));
            UiKit.Place(badge, x0, -0.10f, x0 + width * 0.235f, 1.10f);

            var label = UiKit.CreateTitle($"Value_{icon}", pill, "", 38, CoinInk,
                new Color(0.12f, 0.09f, 0.28f));
            UiKit.Place(label, x0 + width * 0.235f, 0.28f, x0 + width * 0.760f, 0.96f);

            plus = UiKit.CreateIconButton($"Plus_{icon}", pill, UiSkin.Get(Art.Plus));
            UiKit.Place(plus, x0 + width * 0.760f, -0.02f, x0 + width, 1.02f);

            return label;
        }

        /// <summary>
        /// Avatar: çerçeveli, tıklanınca profile götüren kare düğme.
        ///
        /// Çerçeve için ayrı bir görsel (frame_avatar) beklenirken koyu panel
        /// mor tonlanıyor. Görsel gelince yalnız bu satır değişecek.
        /// </summary>
        void BuildAvatar(Transform root, float x0, float x1, float y0, float y1)
        {
            var frameSprite = UiSkin.Get(Art.AvatarFrame);

            var frame = frameSprite != null
                ? UiKit.CreateIconButton("Avatar", root, frameSprite)
                : UiKit.CreateSpriteButton("Avatar", root, UiSkin.Get(Art.PanelDark),
                    null, 0, CoinInk);
            UiKit.Place(frame, x0, y0, x1, y1);

            Transform host = frame.transform;
            if (frameSprite == null)
            {
                if (frame.targetGraphic is Image face) face.color = Periwinkle;
                var inner = UiKit.CreateSlicedPanel("Inner", frame.transform,
                    UiSkin.Get(Art.PanelDark), new Color(0.16f, 0.13f, 0.40f));
                UiKit.Place(inner, 0.12f, 0.12f, 0.88f, 0.88f);
                host = inner.transform;
            }

            var portrait = UiSkin.Get(Art.Avatar);
            if (portrait != null)
            {
                var face2 = UiKit.CreateIcon("Portrait", host, portrait);
                // Bust görseli aşağıdan kesik; tam oturtulunca kafa küçük kalıyor.
                UiKit.Place(face2, 0.02f, 0.02f, 0.98f, 1.18f);
            }
            else
            {
                _avatarInitial = UiKit.CreateTitle("Initial", host, "?", 46,
                    CoinInk, new Color(0.12f, 0.09f, 0.28f));
                UiKit.Place(_avatarInitial, 0f, 0f, 1f, 1f);
            }

            frame.onClick.AddListener(() => MenuShell.Instance?.Show("profile"));
        }

        /// <summary>
        /// Kare düğme. Kendi görseli (btn_square) varsa o, yoksa panel tonlanır.
        /// Buton görselleri (btn_*) 3:1 orana çizildi; kareye sıkıştırınca
        /// 9-dilim payları yüzü eziyor.
        /// </summary>
        static Button SquareButton(Transform root, string name, string icon,
            float x0, float x1, float y0, float y1)
        {
            var squareSprite = UiSkin.Get(Art.ButtonSquare);

            Button button;
            Transform host;
            if (squareSprite != null)
            {
                button = UiKit.CreateIconButton(name, root, squareSprite);
                host = button.transform;
            }
            else
            {
                button = UiKit.CreateSpriteButton(name, root, UiSkin.Get(Art.PanelDark),
                    null, 0, CoinInk);
                if (button.targetGraphic is Image face) face.color = Periwinkle;
                host = button.transform;
            }
            UiKit.Place(button, x0, y0, x1, y1);

            var glyph = UiKit.CreateIcon("Icon", host, UiSkin.Get(icon));
            UiKit.Place(glyph, 0.18f, 0.18f, 0.82f, 0.82f);
            return button;
        }

        // ------------------------------------------------------------ oyna düğmesi

        void BuildPlayButton(Transform root)
        {
            _playButton = UiKit.CreateSpriteButton("Play", root, UiSkin.Get(Art.ButtonGreen),
                null, 0, CoinInk);
            // DERS (düğme ekranı yemez): İlk hâli genişliğin %68'i ve
            // yüksekliğin %13'ü kadardı; ekranın altını kaplıyor ve manzarayı
            // eziyordu. Referanstaki düğme genişliğin ~%54'ü, yüksekliğin
            // ~%7'si. Küçültünce hem manzara nefes alıyor hem düğme daha
            // "basılası" duruyor — büyük düğme güçlü değil, hantal görünüyor.
            UiKit.Place(_playButton, 0.230f, 0.140f, 0.770f, 0.212f);
            _playButton.onClick.AddListener(PlayCurrent);
            _playFace = _playButton.targetGraphic as Image;

            var face = _playButton.transform.GetChild(0);

            _levelLabel = UiKit.CreateTitle("Level", face, "", 48, CoinInk,
                new Color(0.10f, 0.06f, 0.22f));
            UiKit.Place(_levelLabel, 0.06f, 0.44f, 0.94f, 0.94f);

            _difficultyLabel = UiKit.CreateTitle("Difficulty", face, "", 26,
                new Color(0.88f, 0.90f, 1f), new Color(0.10f, 0.06f, 0.22f));
            UiKit.Place(_difficultyLabel, 0.06f, 0.18f, 0.94f, 0.44f);

            // Ödül şeridi düğmenin ÜST KENARINA binerek durur; ayrı bir kutu
            // gibi değil, düğmeye takılmış bir etiket gibi okunsun.
            var ribbonSprite = UiSkin.Get(Art.Ribbon);
            var ribbon = ribbonSprite != null
                ? UiKit.CreateSlicedPanel("Ribbon", _playButton.transform, ribbonSprite)
                : UiKit.CreateSlicedPanel("Ribbon", _playButton.transform,
                    UiSkin.Get(Art.PanelDark), new Color(0.94f, 0.55f, 0.10f));
            UiKit.Place(ribbon, 0.24f, 0.74f, 0.76f, 1.34f);
            _rewardRibbon = ribbon.rectTransform;

            _rewardLabel = UiKit.CreateTitle("RibbonText", ribbon.transform, "", 24,
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
            UiKit.Place((RectTransform)_adRow.transform, 0.210f, 0.132f, 0.790f, 0.220f);

            var button = UiKit.CreateSpriteButton("WatchAd", _adRow.transform,
                UiSkin.Get(Art.ButtonPurple), null, 0, CoinInk);
            UiKit.Place(button, 0f, 0f, 1f, 1f);

            var face = button.transform.GetChild(0);
            var title = UiKit.CreateTitle("Title", face, "REKLAM İZLE", 34, CoinInk,
                new Color(0.16f, 0.06f, 0.30f));
            UiKit.Place(title, 0.06f, 0.44f, 0.94f, 0.94f);

            _adOfferSub = UiKit.CreateLabel("Sub", face, "+1 can kazan", 24,
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
