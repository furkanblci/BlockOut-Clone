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
        RectTransform _resultPanel;
        TextMeshProUGUI _resultTitle, _resultReward;
        Button _resultPrimary;
        TextMeshProUGUI _resultPrimaryLabel;

        readonly (Button button, Image face, TextMeshProUGUI badge)[] _powerButtons =
            new (Button, Image, TextMeshProUGUI)[3];

        readonly StringBuilder _scratch = new StringBuilder(48);
        int _shownSeconds = -1, _shownLevel = -1, _shownLives = -1, _shownCoins = -1;
        string _hint = "";
        float _hintUntil;
        GameState _shownState = GameState.Intro;
        float _nextSlowTick;

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
            pause.onClick.AddListener(AppRouter.GoHome);

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

        void BuildResultPanel(Transform root)
        {
            _resultPanel = UiKit.CreateRect("Result", root);
            UiKit.Place(_resultPanel, 0f, 0f, 1f, 1f);

            // Perde: altındaki tahtaya dokunmayı da yutar.
            var scrim = UiKit.CreatePanel("Scrim", _resultPanel, new Color(0.05f, 0.03f, 0.14f, 0.82f));
            scrim.raycastTarget = true;

            var card = UiKit.CreateSlicedPanel("Card", _resultPanel, UiSkin.Get(Art.PanelCard));
            UiKit.Place(card, 0.10f, 0.30f, 0.90f, 0.70f);

            _resultTitle = UiKit.CreateTitle("Title", card.transform, "", 54,
                new Color(0.30f, 0.16f, 0.05f), new Color(1f, 0.93f, 0.80f));
            UiKit.Place(_resultTitle, 0.06f, 0.70f, 0.94f, 0.93f);
            _resultTitle.textWrappingMode = TextWrappingModes.Normal;

            _resultReward = UiKit.CreateTitle("Reward", card.transform, "", 38,
                new Color(0.85f, 0.55f, 0.05f), new Color(1f, 0.95f, 0.85f));
            UiKit.Place(_resultReward, 0.06f, 0.52f, 0.94f, 0.70f);

            _resultPrimary = UiKit.CreateTintedButton("Primary", card.transform,
                UiSkin.Get(Art.PanelCard), Good, "", 30, Ink);
            UiKit.Place(_resultPrimary, 0.12f, 0.28f, 0.88f, 0.48f);
            _resultPrimaryLabel = _resultPrimary.GetComponentInChildren<TextMeshProUGUI>();
            _resultPrimary.onClick.AddListener(OnPrimary);

            var home = UiKit.CreateTintedButton("Home", card.transform,
                UiSkin.Get(Art.PanelCard), Periwinkle, "Ana Ekran", 28, Ink);
            UiKit.Place(home, 0.12f, 0.06f, 0.88f, 0.24f);
            home.onClick.AddListener(AppRouter.GoHome);

            _resultPanel.gameObject.SetActive(false);
        }

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
            _timerLabel.color = total <= _session.WarningSeconds ? Warning : Ink;
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
        }
    }
}
