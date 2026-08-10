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
        RectTransform _comboBadge;
        TextMeshProUGUI _comboLabel;
        int _shownChain = -1;

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
            BuildComboBadge(root);
            BuildPowerUpBar(root);
            BuildPausePanel(root);
            BuildResultPanel(root);
        }

        /// <summary>
        /// Üst bar — REFERANS VİDEODAN ölçülerek yeniden kuruldu.
        ///
        /// İki satır:
        ///   1) solda jeton sayacı, sağda "Bölüm N" kapsülü
        ///   2) solda yeniden başlat, ortada süre, sağda duraklat
        ///
        /// DERS (oynanışta CAN gösterilmez): Bizim ilk sürümümüz üst bara can
        /// ve jetonu birlikte koyuyordu. Referans oynanış sırasında canı hiç
        /// göstermiyor — çünkü can bölüme GİRERKEN harcanır, oyun içinde
        /// değişmez. Değişmeyen bir sayacı ekranda tutmak yer kaplar ve
        /// oyuncunun gözünü boş yere çeker.
        ///
        /// DERS (yeniden başlat DURAKLATTAN ayrı): Bulmacada en sık istenen
        /// eylem "baştan al"dır. Referans onu tek dokunuşluk ayrı bir düğme
        /// yapmış; duraklat menüsünün içine gömmek her seferinde iki dokunuş
        /// demek olurdu.
        /// </summary>
        void BuildTopBar(Transform root)
        {
            // --- 1. satır: jeton | bölüm ---
            var coin = UiKit.CreateIcon("Coin", root, UiSkin.Get(Art.Coin));
            UiKit.Place(coin, 0.035f, 0.944f, 0.115f, 0.984f);

            _coinLabel = UiKit.CreateTitle("Coins", root, "", 34, Ink,
                new Color(0.10f, 0.07f, 0.24f));
            UiKit.Place(_coinLabel, 0.125f, 0.944f, 0.420f, 0.984f);
            _coinLabel.alignment = TextAlignmentOptions.Left;

            var levelPill = UiKit.CreateSlicedPanel("LevelPill", root,
                UiSkin.Get(Art.PanelDark), Periwinkle);
            UiKit.Place(levelPill, 0.700f, 0.946f, 0.965f, 0.986f);

            _levelLabel = UiKit.CreateTitle("Level", levelPill.transform, "", 28, Ink,
                new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(_levelLabel, 0.05f, 0.06f, 0.95f, 0.94f);

            // --- 2. satır: yeniden başlat | süre | duraklat ---
            var restart = SquareButton(root, "Restart", 0.035f, 0.165f, 0.878f, 0.936f);
            BuildRestartGlyph(restart.transform);
            restart.onClick.AddListener(() => _session.Restart());

            var timer = UiKit.CreateSlicedPanel("TimerPill", root,
                UiSkin.Get(Art.PanelDark), new Color(0.18f, 0.15f, 0.38f));
            UiKit.Place(timer, 0.300f, 0.878f, 0.700f, 0.936f);

            var clock = UiKit.CreateIcon("Clock", timer.transform, UiSkin.Get(Art.Clock));
            UiKit.Place(clock, 0.05f, 0.14f, 0.24f, 0.86f);

            _timerLabel = UiKit.CreateTitle("Timer", timer.transform, "", 38, Ink,
                new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(_timerLabel, 0.26f, 0.06f, 0.94f, 0.94f);

            var pause = SquareButton(root, "Pause", 0.835f, 0.965f, 0.878f, 0.936f);
            // Duraklat simgesi de çiziliyor: "II" yazıyla da olurdu ama yazı
            // tipinin harf aralığı iki çubuğu eşit yapmıyor ve simge eğri duruyor.
            for (int bar = 0; bar < 2; bar++)
            {
                var stripe = UiKit.CreateRoundedPanel($"Bar_{bar}", pause.transform, Ink);
                stripe.pixelsPerUnitMultiplier = 1.4f;
                stripe.raycastTarget = false;
                float x0 = bar == 0 ? 0.30f : 0.56f;
                UiKit.Place(stripe, x0, 0.26f, x0 + 0.14f, 0.74f);
            }
            pause.onClick.AddListener(() => SetPaused(true));

            // Can oynanışta gösterilmiyor; alan boş kalmasın diye değil,
            // GEREKMEDİĞİ için. Alanı bloklar kullanıyor.
            _livesLabel = null;
        }

        /// <summary>
        /// Yeniden başlat simgesi: halka + ok başı, ÇİZİLEREK kuruluyor.
        ///
        /// DERS (yazı tipinde olmayan karakter kutu olarak çıkar): Simgeyi
        /// "↺" karakteriyle yazmıştım; Baloo 2'de o karakter yok ve TMP onu
        /// boş kutuya çeviriyor. Bu tuzağa bu projede dördüncü düşüşüm — ders
        /// artık net: ARAYÜZ SİMGESİ YAZI DEĞİLDİR. Yazı tipi bir dil taşır,
        /// simgeler ayrı bir varlıktır ve ya sprite ya çizim olmalı.
        ///
        /// Halka için "RoundedOutline" sprite'ı kullanılıyor: köşe yarıçapı
        /// sonuna kadar açılınca kare çerçeve daireye dönüşüyor.
        /// </summary>
        static void BuildRestartGlyph(Transform parent)
        {
            var ring = UiKit.CreateRect("Glyph", parent);
            UiKit.Place(ring, 0.20f, 0.20f, 0.80f, 0.80f);

            var circle = ring.gameObject.AddComponent<Image>();
            circle.sprite = GameKit.UI.UiSprites.RoundedOutline;
            circle.type = Image.Type.Sliced;
            circle.pixelsPerUnitMultiplier = 0.10f;
            circle.color = Ink;
            circle.raycastTarget = false;

            // Ok başı: halkanın sağ üstünde küçük bir üçgen. Döndürülmüş bir
            // kare, üçgen sprite'ı üretmeden aynı okumayı veriyor.
            var head = UiKit.CreateRoundedPanel("Head", ring, Ink);
            head.pixelsPerUnitMultiplier = 1.2f;
            head.raycastTarget = false;
            UiKit.Place(head, 0.56f, 0.62f, 1.02f, 1.08f);
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        /// <summary>Üst bardaki kare düğme: mor yüzey, ortada simge.</summary>
        static Button SquareButton(Transform root, string name,
            float x0, float x1, float y0, float y1)
        {
            var square = UiSkin.Get(Art.ButtonSquare);
            var button = square != null
                ? UiKit.CreateIconButton(name, root, square)
                : UiKit.CreateSpriteButton(name, root, UiSkin.Get(Art.PanelDark), null, 0, Ink);
            if (square == null && button.targetGraphic is Image face) face.color = Periwinkle;
            UiKit.Place(button, x0, y0, x1, y1);
            return button;
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
            UiKit.Place(_hintLabel, 0.06f, 0.130f, 0.94f, 0.178f);

            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.235f + i * 0.180f;

                // REFERANS: yardımcılar YEŞİL kare düğme, adet sağ altta
                // KIRMIZI yuvarlak rozette. Bizde koyu mor düğmenin altında
                // yazıyla duruyordu — hem referanstan uzaktı hem de adet
                // düğmenin parçası değil, altına iliştirilmiş bir not gibi
                // görünüyordu. Rozet, sayıyı düğmenin ÜSTÜNE taşıyıp
                // "bu düğmenin üç hakkı var" cümlesini tek bakışta veriyor.
                // DERS (boyama ÇARPMADIR — üçüncü kez): Yeşil düğme için önce
                // krem kart boyandı (alt bandı koyu yeşil şeride döndü), sonra
                // MOR kare düğme boyandı — mor × yeşil = koyu haki, hiç yeşil
                // değil. Doğru cevap boyamak değil ZATEN YEŞİL olan görseli
                // kullanmak: btn_green.
                //
                // btn_green 3:1 orana çizildi ve kare alanda 9-dilim payları
                // (yanlarda 90px) genişliği aşıyordu. pixelsPerUnitMultiplier
                // payı yarıya indiriyor; kare oranda da bozulmadan duruyor.
                // btn_green de denendi: 3:1 orana çizildiği için kare alanda
                // dikey payları (üst 88 + alt 68) yüksekliğin neredeyse tamamını
                // yiyor ve ekranda yalnız koyu alt kenarı görünüyordu.
                // Krem kart doğru cevap: neredeyse beyaz olduğu için parlak
                // yeşil veriyor, kendi alt bandı da düğmenin 3B kalınlığı gibi
                // okunuyor — küçük bir düğmede bu istenen şey.
                var button = UiKit.CreateSpriteButton($"Power_{i}", root,
                    UiSkin.Get(Art.PanelCard), null, 0, Ink);
                UiKit.Place(button, x0, 0.028f, x0 + 0.150f, 0.122f);
                var face = button.targetGraphic as Image;
                if (face != null)
                {
                    face.color = PowerGreen;
                    face.pixelsPerUnitMultiplier = 1.5f;
                }

                var icon = UiKit.CreateIcon("Icon", button.transform, UiSkin.Get(icons[i]));
                UiKit.Place(icon, 0.12f, 0.18f, 0.88f, 0.94f);

                // Rozet düğmenin sağ alt köşesinden TAŞAR — referanstaki gibi.
                var badge = UiKit.CreateRoundedPanel("Badge", button.transform,
                    new Color(0.91f, 0.16f, 0.16f));
                badge.pixelsPerUnitMultiplier = 0.10f;
                UiKit.Place(badge, 0.62f, -0.06f, 1.04f, 0.34f);

                var count = UiKit.CreateTitle("Count", badge.transform, "", 24, Ink,
                    new Color(0.40f, 0.03f, 0.03f));
                UiKit.Place(count, 0.04f, 0.06f, 0.96f, 0.94f);

                var kind = (PowerUpKind)i;
                button.onClick.AddListener(() => _session.PowerUps?.Use(kind));
                _powerButtons[i] = (button, face, count);
            }
        }

        static readonly Color PowerGreen = new Color(0.365f, 0.878f, 0.259f);

        /// <summary>
        /// Combo rozeti: tahtanın sağ üstünde, zincir varken beliren sayaç.
        ///
        /// DERS (geri bildirim ELİN OLDUĞU yerde olmalı): Combo yazısını ekranın
        /// ortasına koymak blokların önünü kapatır; en alta koymak ise parmağın
        /// altında kalır. Sağ üst köşe, sürükleme sırasında gözün doğal olarak
        /// uğradığı ama parmağın örtmediği yer.
        /// </summary>
        void BuildComboBadge(Transform root)
        {
            _comboBadge = UiKit.CreateRect("Combo", root);
            UiKit.Place(_comboBadge, 0.66f, 0.820f, 0.985f, 0.900f);

            var back = UiKit.CreateRoundedPanel("Back", _comboBadge,
                new Color(0.96f, 0.53f, 0.05f));
            back.pixelsPerUnitMultiplier = 0.16f;
            back.raycastTarget = false;
            UiKit.Place(back, 0f, 0f, 1f, 1f);

            _comboLabel = UiKit.CreateTitle("Text", _comboBadge, "", 32, Ink,
                new Color(0.42f, 0.18f, 0.01f));
            UiKit.Place(_comboLabel, 0.06f, 0.08f, 0.94f, 0.92f);

            _comboBadge.gameObject.SetActive(false);
        }

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
            RefreshCombo();
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

        void RefreshCombo()
        {
            int chain = _session.Combo.Chain;
            if (chain == _shownChain) return;
            _shownChain = chain;

            bool show = chain >= ComboTracker.MinimumChain;
            _comboBadge.gameObject.SetActive(show);
            if (!show) return;

            _comboLabel.text = _scratch.Clear().Append("COMBO x").Append(chain).ToString();

            // Her yeni halkada vuruş: sayının değiştiğini sessizce yazmak
            // zinciri fark ettirmiyordu.
            GameKit.FX.Juice.Replace(_comboBadge,
                GameKit.FX.Juice.PunchScale(_comboBadge, 0.30f, 0.26f));
            Services.AudioService.Star();
        }

        void RefreshMeta()
        {
            if (!MetaServices.Ready) return;

            // Can oynanışta gösterilmiyor (referans da göstermiyor: can bölüme
            // GİRERKEN harcanır, oyun içinde değişmez). Etiket yok, o yüzden
            // yazmadan önce varlığı kontrol ediliyor.
            if (_livesLabel != null)
            {
                int lives = MetaServices.Lives.Current;
                if (lives != _shownLives)
                {
                    _shownLives = lives;
                    _livesLabel.text = lives.ToString();
                }
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

                // Rozet yalnız ADET gösteriyor. Fiyat, elde hiç yokken düğmenin
                // ALTINDAKİ ipucu satırına düşüyor: kırmızı yuvarlak rozete
                // "300 J" sığmıyor ve sığdırmaya çalışmak yazıyı okunmaz
                // yapıyordu. Referans da rozette yalnız sayı gösteriyor.
                badge.text = owned > 0
                    ? owned.ToString()
                    : _scratch.Clear().Append(PowerUpInfo.Price(kind)).ToString();
                badge.fontSize = owned > 0 ? 24 : 18;

                // DERS (kurulumu değiştirmek yetmez, TAZELEMEYİ de değiştir):
                // Düğmenin yeşili kurulumda ayarlanıyordu ama bu satır her
                // karede rengi mora geri yazıyordu. Ekranda hiç yeşil
                // görmediğim için önce sprite'ı, sonra 9-dilim payını suçladım;
                // ikisi de doğruydu. Bir değeri HER KARE yazan kod varsa,
                // kurulumdaki değer görünmez.
                //
                // Boşta yeşil, seçiliyken ALTIN: seçim durumu artık yeşilden
                // ayrışmak zorunda.
                if (face != null)
                    face.color = power.Pending == kind
                        ? new Color(1f, 0.82f, 0.15f)
                        : PowerGreen;
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
