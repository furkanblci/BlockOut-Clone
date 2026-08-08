using System.Text;
using BlockOut.Core;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Oyun içi arayüz: üstte bölüm ve süre, altta yardımcılar, sonda sonuç paneli.
    ///
    /// DERS (HUD tahtayı YEMEMELİ): Bulmaca oyununda ekranın ortası kutsaldır —
    /// blokların sürükleneceği alan orasıdır. Bu yüzden arayüz yalnızca üst ve
    /// alt şeritte yaşıyor, ortası tamamen boş bırakılıyor. Sonuç paneli bu
    /// kuralın tek istisnası ve zaten oyun bittiğinde çıkıyor.
    ///
    /// DERS (IMGUI'den uGUI'ye niçin geçildi): Önceki HUD OnGUI ile çiziliyordu.
    /// Çalışıyordu ama üç bedeli vardı: (1) kare başına yeniden düzen hesabı ve
    /// çöp üretimi, (2) sprite/9-dilim/TMP kullanılamaması — yani oyunun geri
    /// kalanıyla aynı görsel dili konuşamaması, (3) dokunma alanlarının
    /// EventSystem'den geçmemesi. uGUI üçünü de çözüyor; karşılığında kurulum
    /// kodu uzuyor, o da bir kez yazılıyor.
    /// </summary>
    public sealed class GameplayScreen : MonoBehaviour
    {
        static readonly Color Periwinkle = new Color(0.353f, 0.322f, 0.784f);
        static readonly Color Ink        = new Color(1f, 0.98f, 0.94f);
        static readonly Color Warning    = new Color(1f, 0.36f, 0.30f);
        static readonly Color Good       = new Color(0.30f, 0.88f, 0.36f);

        GameSession _session;

        TextMeshProUGUI _levelLabel, _timerLabel, _livesLabel, _coinLabel, _hintLabel;
        RectTransform _resultPanel, _pausePanel;
        Image _resultCard, _pauseCard;
        readonly Image[] _stars = new Image[3];
        TextMeshProUGUI _perfectBadge;
        TextMeshProUGUI _resultTitle, _resultReward;
        Button _resultPrimary, _adButton, _homeButton;
        TextMeshProUGUI _resultPrimaryLabel, _adLabel;

        readonly (Button button, Image face, TextMeshProUGUI badge)[] _powerButtons =
            new (Button, Image, TextMeshProUGUI)[3];

        readonly StringBuilder _scratch = new StringBuilder(48);
        int _shownSeconds = -1, _shownLevel = -1, _shownLives = -1, _shownCoins = -1;
        string _hint = "";
        float _hintUntil;
        GameState _shownState = GameState.Intro;
        int _lastPulse = -1;
        float _nextSlowTick;
        GameObject _freezeVignette;

        public void Init(GameSession session)
        {
            _session = session;
            if (_session.PowerUps != null)
                _session.PowerUps.Message += text =>
                {
                    _hint = text;
                    _hintUntil = Time.unscaledTime + 2.5f;
                };

            BuildUi();
            Refresh();
        }

        // ------------------------------------------------------------------ kurulum

        void BuildUi()
        {
            var canvas = UiKit.CreateCanvas("GameplayCanvas");
            canvas.transform.SetParent(transform, worldPositionStays: false);
            canvas.sortingOrder = 5;
            var root = UiKit.CreateSafeArea(canvas);

            BuildTopBar(root);
            BuildPowerUpBar(root);
            BuildPausePanel(root);
            BuildResultPanel(root);
        }

        void BuildTopBar(Transform root)
        {
            // Duraklat: sol üstte, tahtadan uzakta.
            var pause = UiKit.CreateSpriteButton("Pause", root, UiSkin.Get(Art.PanelDark),
                null, 0, Ink);
            UiKit.Place(pause, 0.030f, 0.918f, 0.165f, 0.988f);
            if (pause.targetGraphic is Image pauseFace) pauseFace.color = Periwinkle;
            var pauseIcon = UiKit.CreateIcon("Icon", pause.transform, UiSkin.Get(Art.Home));
            UiKit.Place(pauseIcon, 0.16f, 0.16f, 0.84f, 0.84f);
            pause.onClick.AddListener(() => SetPaused(true));

            // Bölüm + süre: ortada, tek kapsülde.
            var capsule = UiKit.CreateSlicedPanel("Status", root, UiSkin.Get(Art.PanelDark));
            UiKit.Place(capsule, 0.195f, 0.918f, 0.640f, 0.988f);

            _levelLabel = UiKit.CreateLabel("Level", capsule.transform, "", 26,
                new Color(0.84f, 0.86f, 1f));
            UiKit.Place(_levelLabel, 0.04f, 0.48f, 0.96f, 0.94f);

            _timerLabel = UiKit.CreateTitle("Timer", capsule.transform, "", 46, Ink,
                new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(_timerLabel, 0.04f, 0.04f, 0.96f, 0.52f);

            // Can ve jeton: sağda, küçük.
            var meta = UiKit.CreateSlicedPanel("Meta", root, UiSkin.Get(Art.PanelDark));
            UiKit.Place(meta, 0.665f, 0.918f, 0.985f, 0.988f);

            var heart = UiKit.CreateIcon("Heart", meta.transform, UiSkin.Get(Art.Heart));
            UiKit.Place(heart, 0.02f, 0.10f, 0.26f, 0.90f);
            _livesLabel = UiKit.CreateTitle("Lives", meta.transform, "", 32, Ink,
                new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(_livesLabel, 0.26f, 0.08f, 0.48f, 0.92f);

            var coin = UiKit.CreateIcon("Coin", meta.transform, UiSkin.Get(Art.Coin));
            UiKit.Place(coin, 0.50f, 0.10f, 0.72f, 0.90f);
            _coinLabel = UiKit.CreateTitle("Coins", meta.transform, "", 30, Ink,
                new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(_coinLabel, 0.71f, 0.08f, 0.99f, 0.92f);
        }

        /// <summary>
        /// Alt yardımcı çubuğu: çalar saat / roket / UFO.
        ///
        /// DERS (durumu düğmenin üstünde göster): Oyuncu "bu bana kaça mal
        /// olacak" sorusunu düğmeye BASMADAN görebilmeli. Elde varsa adet,
        /// yoksa fiyat yazıyor; fiyatı gizleyip basınca almak, oyuncunun
        /// kendini kandırılmış hissetmesinin en kısa yolu.
        /// </summary>
        void BuildPowerUpBar(Transform root)
        {
            string[] icons = { Art.Clock, Art.Rocket, Art.Ufo };

            _hintLabel = UiKit.CreateTitle("Hint", root, "", 28, Ink,
                new Color(0.10f, 0.07f, 0.24f));
            UiKit.Place(_hintLabel, 0.06f, 0.155f, 0.94f, 0.205f);

            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.155f + i * 0.245f;

                var button = UiKit.CreateSpriteButton($"Power_{i}", root,
                    UiSkin.Get(Art.PanelDark), null, 0, Ink);
                UiKit.Place(button, x0, 0.030f, x0 + 0.195f, 0.150f);
                var face = button.targetGraphic as Image;
                if (face != null) face.color = Periwinkle;

                var icon = UiKit.CreateIcon("Icon", button.transform, UiSkin.Get(icons[i]));
                UiKit.Place(icon, 0.14f, 0.28f, 0.86f, 0.98f);

                var badge = UiKit.CreateTitle("Badge", button.transform, "", 26, Ink,
                    new Color(0.12f, 0.09f, 0.30f));
                UiKit.Place(badge, 0.02f, 0.02f, 0.98f, 0.30f);

                var kind = (PowerUpKind)i;
                button.onClick.AddListener(() => _session.PowerUps?.Use(kind));
                _powerButtons[i] = (button, face, badge);
            }
        }

        /// <summary>
        /// Sonuç paneli: yıldızlar, PERFECT rozeti, ödül ve iki düğme.
        ///
        /// DERS (kutlama BEDAVA elde tutma): Bölümü bitirmek zaten ödül; ama
        /// ekran "kazandın" yazıp geçerse o an hiçbir şey hissettirmez. Yıldızın
        /// tek tek oturması, konfetinin patlaması ve jetonun sayaca uçması —
        /// üçü birlikte yarım saniyelik bir tören yapıyor. Yeni içerik yazmadan
        /// oyuncunun bir sonraki bölüme geçme isteğini artıran en ucuz yol budur.
        /// </summary>
        /// <summary>
        /// Duraklat paneli.
        ///
        /// DERS (geri dönüşü olmayan çıkışı SORMADAN yapma): Bu düğme önce
        /// doğrudan ana ekrana atıyordu. Bölüme girerken can zaten harcanmış
        /// oluyor; yanlışlıkla basan oyuncu hem bölümü hem canı kaybediyor ve
        /// bunun neden olduğunu anlamıyordu. Sessiz veri kaybı, arayüzün
        /// yapabileceği en pahalı hatadır. Panel üç seçenek sunuyor ve
        /// "Devam Et" en büyüğü — kazara açılan bir menüden çıkış kolay olmalı.
        /// </summary>
        void BuildPausePanel(Transform root)
        {
            _pausePanel = UiKit.CreateRect("Pause", root);
            UiKit.Place(_pausePanel, 0f, 0f, 1f, 1f);

            var scrim = UiKit.CreatePanel("Scrim", _pausePanel, new Color(0.05f, 0.03f, 0.14f, 0.82f));
            scrim.raycastTarget = true;

            _pauseCard = UiKit.CreateSlicedPanel("Card", _pausePanel, UiSkin.Get(Art.PanelCard));
            UiKit.Place(_pauseCard, 0.13f, 0.33f, 0.87f, 0.67f);

            var title = UiKit.CreateTitle("Title", _pauseCard.transform, "DURAKLATILDI", 44,
                new Color(0.30f, 0.16f, 0.05f), new Color(1f, 0.93f, 0.80f));
            UiKit.Place(title, 0.06f, 0.78f, 0.94f, 0.95f);

            var resume = UiKit.CreateTintedButton("Resume", _pauseCard.transform,
                UiSkin.Get(Art.PanelCard), Good, "Devam Et", 32, Ink);
            UiKit.Place(resume, 0.10f, 0.52f, 0.90f, 0.74f);
            resume.onClick.AddListener(() => SetPaused(false));

            var restart = UiKit.CreateTintedButton("Restart", _pauseCard.transform,
                UiSkin.Get(Art.PanelCard), Periwinkle, "Yeniden Başla", 28, Ink);
            UiKit.Place(restart, 0.10f, 0.29f, 0.90f, 0.48f);
            restart.onClick.AddListener(() =>
            {
                SetPaused(false);
                _session.Restart();
            });

            // Çıkış EN KÜÇÜK ve en altta: kaybı olan seçenek, kolay basılan
            // yerde durmamalı.
            var quit = UiKit.CreateTintedButton("Quit", _pauseCard.transform,
                UiSkin.Get(Art.PanelCard), new Color(0.925f, 0.255f, 0.176f),
                "Bölümden Çık", 24, Ink);
            UiKit.Place(quit, 0.22f, 0.07f, 0.78f, 0.25f);
            quit.onClick.AddListener(AppRouter.GoHome);

            _pausePanel.gameObject.SetActive(false);
        }

        void SetPaused(bool paused)
        {
            if (_session == null) return;
            if (paused && _session.State != GameState.Playing) return;

            _session.SetPaused(paused);
            _pausePanel.gameObject.SetActive(paused);
            if (paused)
                GameKit.FX.Juice.Replace(_pauseCard,
                    GameKit.FX.Juice.PopIn(_pauseCard.transform, 0.30f));
        }

        void BuildResultPanel(Transform root)
        {
            _resultPanel = UiKit.CreateRect("Result", root);
            UiKit.Place(_resultPanel, 0f, 0f, 1f, 1f);

            // Perde: altındaki tahtaya dokunmayı da yutar.
            var scrim = UiKit.CreatePanel("Scrim", _resultPanel, new Color(0.05f, 0.03f, 0.14f, 0.84f));
            scrim.raycastTarget = true;

            _resultCard = UiKit.CreateSlicedPanel("Card", _resultPanel, UiSkin.Get(Art.PanelCard));
            UiKit.Place(_resultCard, 0.10f, 0.30f, 0.90f, 0.72f);

            _resultTitle = UiKit.CreateTitle("Title", _resultCard.transform, "", 50,
                new Color(0.30f, 0.16f, 0.05f), new Color(1f, 0.93f, 0.80f));
            UiKit.Place(_resultTitle, 0.06f, 0.76f, 0.94f, 0.94f);
            _resultTitle.textWrappingMode = TextWrappingModes.Normal;

            // Üç yıldız: kazanınca sırayla yaylanarak oturur.
            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.20f + i * 0.21f;
                var star = UiKit.CreateIcon($"Star_{i}", _resultCard.transform, UiSkin.Get(Art.Star));
                // Ortadaki yıldız biraz yukarıda: düz sıra "üç ikon" gibi durur,
                // kavisli dizilim madalya gibi.
                float lift = i == 1 ? 0.04f : 0f;
                UiKit.Place(star, x0, 0.52f + lift, x0 + 0.19f, 0.76f + lift);
                _stars[i] = star;
            }

            _perfectBadge = UiKit.CreateTitle("Perfect", _resultCard.transform, "PERFECT", 34,
                new Color(1f, 0.98f, 0.94f), new Color(0.90f, 0.42f, 0.03f));
            UiKit.Place(_perfectBadge, 0.28f, 0.44f, 0.72f, 0.55f);

            _resultReward = UiKit.CreateTitle("Reward", _resultCard.transform, "", 36,
                new Color(0.85f, 0.55f, 0.05f), new Color(1f, 0.95f, 0.85f));
            UiKit.Place(_resultReward, 0.06f, 0.33f, 0.94f, 0.45f);

            // Reklam düğmesi: kazanınca "ödülü ikiye katla", kaybedince
            // "+30 sn ile devam et". İkisi de aynı yerde durur ki oyuncu
            // nereye bakacağını öğrensin.
            _adButton = UiKit.CreateTintedButton("Ad", _resultCard.transform,
                UiSkin.Get(Art.PanelCard), new Color(0.94f, 0.62f, 0.06f), "", 26, Ink);
            UiKit.Place(_adButton, 0.10f, 0.175f, 0.90f, 0.315f);
            _adLabel = _adButton.GetComponentInChildren<TextMeshProUGUI>();
            _adButton.onClick.AddListener(OnWatchAd);

            _resultPrimary = UiKit.CreateTintedButton("Primary", _resultCard.transform,
                UiSkin.Get(Art.PanelCard), Good, "", 30, Ink);
            UiKit.Place(_resultPrimary, 0.10f, 0.025f, 0.90f, 0.165f);
            _resultPrimaryLabel = _resultPrimary.GetComponentInChildren<TextMeshProUGUI>();
            _resultPrimary.onClick.AddListener(OnPrimary);

            // Ana ekrana dönüş kartın DIŞINDA, küçük: birincil eylem "devam
            // et"tir, çıkış onunla aynı ağırlıkta görünmemeli.
            _homeButton = UiKit.CreateTintedButton("Home", _resultPanel,
                UiSkin.Get(Art.PanelCard), Periwinkle, "Ana Ekran", 24, Ink);
            UiKit.Place(_homeButton, 0.34f, 0.215f, 0.66f, 0.275f);
            _homeButton.onClick.AddListener(AppRouter.GoHome);

            _resultPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Konfeti: kartın üstünden saçılan küçük renkli kareler.
        ///
        /// Kanvas parçacığı yerine düz Image kullanılıyor — arayüz katmanında
        /// ParticleSystem çizim sırasına karışıyor ve kartın altında kalıyor.
        /// Otuz küçük Image bir kerelik kutlama için ucuz.
        /// </summary>
        void BurstConfetti()
        {
            var sheet = UiSkin.Get(Art.Confetti);
            var colors = new[]
            {
                new Color(0.18f, 0.80f, 0.05f), new Color(1f, 0.78f, 0.10f),
                new Color(0.95f, 0.30f, 0.45f), new Color(0.25f, 0.62f, 0.98f),
                new Color(0.66f, 0.35f, 0.92f)
            };

            for (int i = 0; i < 30; i++)
            {
                var piece = sheet != null
                    ? UiKit.CreateIcon($"Confetti_{i}", _resultPanel, sheet, colors[i % colors.Length])
                    : UiKit.CreateRoundedPanel($"Confetti_{i}", _resultPanel, colors[i % colors.Length]);
                piece.raycastTarget = false;

                var rect = piece.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.72f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(Random.Range(18f, 34f), Random.Range(24f, 42f));
                rect.anchoredPosition = Vector2.zero;

                float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                float speed = Random.Range(700f, 1500f);
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                float spin = Random.Range(-540f, 540f);

                GameKit.FX.Juice.Run(ConfettiFlight(rect, velocity, spin));
            }
        }

        static System.Collections.IEnumerator ConfettiFlight(RectTransform piece,
            Vector2 velocity, float spin)
        {
            const float gravity = -2600f;
            const float life = 1.6f;
            var position = Vector2.zero;
            float angle = 0f;

            for (float t = 0f; t < life; t += Time.unscaledDeltaTime)
            {
                if (piece == null) yield break;

                velocity.y += gravity * Time.unscaledDeltaTime;
                position += velocity * Time.unscaledDeltaTime;
                angle += spin * Time.unscaledDeltaTime;

                piece.anchoredPosition = position;
                piece.localRotation = Quaternion.Euler(0f, 0f, angle);

                // Son üçte birde solarak kaybolur; birden yok olmak göze çarpar.
                var image = piece.GetComponent<Image>();
                if (image != null && t > life * 0.66f)
                {
                    var color = image.color;
                    color.a = 1f - (t - life * 0.66f) / (life * 0.34f);
                    image.color = color;
                }
                yield return null;
            }

            if (piece != null) Destroy(piece.gameObject);
        }

        /// <summary>
        /// Ödüllü reklam: kazandıysa ödülü katlar, kaybettiyse süre ekler.
        ///
        /// DERS (ödül SONUÇ geldiğinde verilir): Reklamı açıp ödülü hemen
        /// vermek en sık yapılan hata. Burada ödül yalnız Completed dönerse
        /// veriliyor; oyuncu atlarsa hiçbir şey değişmiyor.
        /// </summary>
        void OnWatchAd()
        {
            bool won = _session.State == GameState.Won;
            string placement = won ? "double_reward" : "continue_level";

            _adButton.interactable = false;
            Services.FakeAdScreen.Instance.ShowRewarded(placement, outcome =>
            {
                _adButton.interactable = true;
                if (outcome != GameKit.Services.RewardedResult.Completed) return;

                if (won)
                {
                    _session.MultiplyReward(2);
                    _resultReward.text = _scratch.Clear()
                        .Append('+').Append(_session.LastReward).Append(" jeton").ToString();
                    GameKit.FX.Juice.Run(GameKit.FX.Juice.PunchScale(_resultReward.transform, 0.34f));
                    _adButton.gameObject.SetActive(false);   // bir kez katlanır
                }
                else
                {
                    _session.ContinueWithExtraTime(ExtraSeconds);
                    _shownState = GameState.Playing;
                    _resultPanel.gameObject.SetActive(false);
                }
            });
        }

        const int ExtraSeconds = 30;

        void OnPrimary()
        {
            if (_session == null) return;
            if (_session.State == GameState.Won && _session.HasNextLevel) _session.NextLevel();
            else _session.Restart();
        }

        // ----------------------------------------------------------------- tazeleme

        void Update()
        {
            if (_session == null) return;
            Refresh();
        }

        void Refresh()
        {
            RefreshTimer();
            RefreshPowerUps();

            // Can/jeton saniyede bir yeter; her karede kurmak savurganlık.
            if (Time.unscaledTime >= _nextSlowTick)
            {
                _nextSlowTick = Time.unscaledTime + 1f;
                if (MetaServices.Ready) MetaServices.Lives.Refresh();
                RefreshMeta();
            }

            if (_session.State != _shownState)
            {
                _shownState = _session.State;
                RefreshResult();
            }
        }

        void RefreshTimer()
        {
            int total = Mathf.CeilToInt(_session.Timer.Remaining);
            if (total == _shownSeconds && _session.DisplayNumber == _shownLevel) return;

            _shownSeconds = total;
            _shownLevel = _session.DisplayNumber;

            _levelLabel.text = _scratch.Clear().Append("Bölüm ").Append(_shownLevel).ToString();
            _timerLabel.text = _scratch.Clear()
                .Append(total / 60).Append(':')
                .Append((total % 60) / 10).Append(total % 10).ToString();

            bool warning = total <= _session.WarningSeconds;
            _timerLabel.color = warning ? Warning : Ink;

            // DERS (uyarı SÜREKLİ değil ANLIK olmalı): Sayacı kırmızıya boyayıp
            // bırakmak ilk saniyede fark edilir, sonra göz alışır ve uyarı
            // görünmez olur. Her saniyede bir atan nabız, kalan her saniyeyi
            // yeniden duyurur — panik hissi de buradan gelir.
            if (warning && total != _lastPulse)
            {
                _lastPulse = total;
                GameKit.FX.Juice.Replace(_timerLabel,
                    GameKit.FX.Juice.PunchScale(_timerLabel.transform, 0.26f, 0.32f));
            }
        }

        void RefreshMeta()
        {
            if (!MetaServices.Ready) return;

            int lives = MetaServices.Lives.Current;
            if (lives != _shownLives)
            {
                _shownLives = lives;
                _livesLabel.text = lives.ToString();
            }

            int coins = MetaServices.Progress.Coins;
            if (coins != _shownCoins)
            {
                _shownCoins = coins;
                _coinLabel.text = coins.ToString();
            }
        }

        void RefreshPowerUps()
        {
            var power = _session.PowerUps;
            if (power == null) return;

            for (int i = 0; i < 3; i++)
            {
                var kind = (PowerUpKind)i;
                var (_, face, badge) = _powerButtons[i];

                int owned = power.Owned(kind);
                badge.text = owned > 0
                    ? _scratch.Clear().Append('x').Append(owned).ToString()
                    : _scratch.Clear().Append(PowerUpInfo.Price(kind)).Append(" J").ToString();

                // Seçili yardımcı yeşile döner: "şimdi bir blok seç" durumu
                // başka türlü görünmüyor.
                if (face != null)
                    face.color = power.Pending == kind ? Good : Periwinkle;
            }

            // Süre donmuşken ekranın kenarında buzlu bir çerçeve dursun:
            // sayaç durdu bilgisi HUD yazısıyla verilirse gözden kaçıyor.
            bool frozen = power.IsTimeFrozen;
            if (frozen && _freezeVignette == null)
                _freezeVignette = FX.PowerUpFX.FreezeVignette(transform);
            else if (!frozen && _freezeVignette != null)
            {
                Destroy(_freezeVignette);
                _freezeVignette = null;
            }

            if (Time.unscaledTime < _hintUntil)
                _hintLabel.text = _hint;
            else if (power.IsTimeFrozen)
                _hintLabel.text = _scratch.Clear()
                    .Append("Süre donduruldu: ")
                    .Append(Mathf.CeilToInt(power.FreezeRemaining)).Append(" sn").ToString();
            else
                _hintLabel.text = "";
        }

        void RefreshResult()
        {
            bool finished = _shownState == GameState.Won || _shownState == GameState.Lost;
            _resultPanel.gameObject.SetActive(finished);
            if (!finished) return;

            bool won = _shownState == GameState.Won;
            _resultTitle.text = won ? "BÖLÜM TAMAMLANDI!" : "SÜRE DOLDU";
            _resultTitle.color = won ? new Color(0.15f, 0.45f, 0.10f) : new Color(0.55f, 0.12f, 0.06f);

            bool hasReward = won && _session.LastReward > 0;
            _resultReward.gameObject.SetActive(hasReward);
            if (hasReward)
                _resultReward.text = _scratch.Clear()
                    .Append('+').Append(_session.LastReward).Append(" jeton").ToString();

            bool advance = won && _session.HasNextLevel;
            _resultPrimaryLabel.text = advance ? "Sonraki Bölüm" : won ? "Tekrar Oyna" : "Tekrar Dene";

            if (_resultPrimary.targetGraphic is Image face)
                face.color = won ? Good : new Color(0.925f, 0.255f, 0.176f);

            _perfectBadge.gameObject.SetActive(won && _session.LastPerfect);

            // Kart yüksekliği içeriğe göre: kaybedince yıldız ve ödül satırı
            // yok, sabit yükseklik ortada koca bir boşluk bırakıyordu.
            UiKit.Place(_resultCard, 0.10f, won ? 0.30f : 0.375f, 0.90f, won ? 0.72f : 0.655f);
            UiKit.Place(_resultTitle, 0.06f, won ? 0.76f : 0.70f, 0.94f, won ? 0.94f : 0.93f);
            UiKit.Place(_adButton, 0.10f, won ? 0.175f : 0.375f, 0.90f, won ? 0.315f : 0.585f);
            UiKit.Place(_resultPrimary, 0.10f, won ? 0.025f : 0.06f, 0.90f, won ? 0.165f : 0.31f);
            UiKit.Place(_homeButton, 0.34f, won ? 0.215f : 0.295f, 0.66f, won ? 0.275f : 0.352f);

            // Reklam düğmesi: kazanınca katlama (yalnız ödül varsa), kaybedince
            // devam etme. Ödül yoksa katlanacak bir şey de yok, düğme gizlenir.
            bool adUseful = won ? hasReward : true;
            _adButton.gameObject.SetActive(adUseful);
            _adButton.interactable = true;
            if (adUseful)
                _adLabel.text = won ? "Reklam izle · Ödülü 2 kat"
                                    : $"Reklam izle · +{ExtraSeconds} sn devam";

            // Yıldızlar: kaybedince hiç yok, kazanınca kazanılanlar dolu,
            // kalanlar soluk. Soluk yıldızı GÖSTERMEK önemli — "üç tane var,
            // ikisini aldın" bilgisi tekrar oynama sebebidir.
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i].gameObject.SetActive(won);
                if (!won) continue;
                bool earned = i < _session.LastStars;
                _stars[i].color = earned ? Color.white : new Color(0.55f, 0.52f, 0.48f, 0.55f);
                _stars[i].transform.localScale = Vector3.one;
            }

            GameKit.FX.Juice.Replace(_resultCard,
                GameKit.FX.Juice.PopIn(_resultCard.transform, 0.34f));

            if (won) GameKit.FX.Juice.Run(CelebrateRoutine());
        }

        /// <summary>
        /// Kazanma töreni: kart oturur, yıldızlar tek tek düşer, konfeti patlar.
        ///
        /// DERS (aynı anda değil SIRAYLA): Hepsini birlikte oynatmak görsel
        /// gürültü yapar ve hiçbiri fark edilmez. Aralarına 120 milisaniye
        /// koymak, gözün her birini ayrı ayrı görmesini sağlıyor — toplam süre
        /// yarım saniyeyi geçmediği için de oyuncuyu bekletmiyor.
        /// </summary>
        System.Collections.IEnumerator CelebrateRoutine()
        {
            yield return new WaitForSecondsRealtime(0.18f);

            for (int i = 0; i < _session.LastStars && i < _stars.Length; i++)
            {
                var star = _stars[i];
                if (star == null) continue;
                GameKit.FX.Juice.Run(GameKit.FX.Juice.PopIn(star.transform, 0.30f));
                Services.AudioService.Star();
                yield return new WaitForSecondsRealtime(0.12f);
            }

            if (_session.LastPerfect && _perfectBadge != null)
                GameKit.FX.Juice.Run(GameKit.FX.Juice.PopIn(_perfectBadge.transform, 0.32f));

            BurstConfetti();

            if (_resultReward != null && _resultReward.gameObject.activeSelf)
            {
                yield return new WaitForSecondsRealtime(0.14f);
                GameKit.FX.Juice.Run(GameKit.FX.Juice.PunchScale(_resultReward.transform, 0.24f));
                Services.AudioService.Coin();
            }
        }
    }
}
