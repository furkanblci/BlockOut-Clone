using System.Text;
using BlockOut.Core;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;
using UiSprites = GameKit.UI.UiSprites;

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

        // ---- Sonuç panelinin referanstan ölçülmüş renk ve yerleşimi ----
        // Kaynak: "Levels 1-20 Walkthrough" videosu, 02:52 (Level 8 PERFECT).

        /// <summary>Kart moru. panel_card krem olduğu için maviyi biraz fazla
        /// veriyoruz — çarpım kremin sarısını içeri katıyor.</summary>
        static readonly Color CardPurple   = new Color(0.300f, 0.245f, 0.930f);
        static readonly Color TitleGold    = new Color(1f, 0.760f, 0.180f);
        static readonly Color TitleOutline = new Color(0.290f, 0.160f, 0.620f);
        static readonly Color CoinGold     = new Color(1f, 0.820f, 0.320f);
        static readonly Color BadgeDark    = new Color(0.165f, 0.125f, 0.430f);
        static readonly Color CloseRed     = new Color(0.898f, 0.196f, 0.235f);

        // ---- Duraklat panelindeki Off/On anahtarları (referans: 1-20, 00:03) ----
        /// <summary>Seçili olmayan yarı: neredeyse zeminle aynı, ama biraz açık.</summary>
        static readonly Color ToggleIdle    = new Color(0.216f, 0.176f, 0.510f);
        static readonly Color ToggleActive  = new Color(0.290f, 0.780f, 0.220f);
        static readonly Color ToggleIdleInk = new Color(0.470f, 0.435f, 0.720f);

        /// <summary>Referanstaki "Hard Level" satırının pembesi.</summary>
        static readonly Color HardPink = new Color(0.910f, 0.278f, 0.608f);

        // ---- Yeniden başlat onayı, `Game over .mp4` 15. sn'den ölçüldü -----
        static readonly Color RetryViolet = new Color(0.373f, 0.125f, 0.729f);  // #5F20BA
        static readonly Color RetryWell   = new Color(0.184f, 0.078f, 0.353f);  // #2F145A
        static readonly Color RetryLip    = new Color(0.443f, 0.208f, 0.827f);  // #7135D3

        // ---- OYUN İÇİ HUD, REFERANSTAN ÖLÇÜLDÜ (2026-08-18) ----------------
        //
        // Kaynak: `Block Out Color Sort Puzzle Levels.mp4`, 00:28 karesi
        // (592×1280). Piksel örneklendi, göz kararı değil.
        //
        // DERS (beyaz sandığımız yazı beyaz DEĞİLDİ): Jeton ve "Level"
        // yazılarını `Ink` (neredeyse beyaz) ile yazıyorduk. Referansta ikisi
        // de **#9C91FF** — açık MOR. Kullanıcı "sol üstteki para yazısı mor
        // renkte olmalı" derken bunu gördü. Süre yazısı ise gerçekten krem
        // beyaz (#FFFDF6) ve belirgin biçimde DAHA BÜYÜK; yani üç yazı üç
        // ayrı rol taşıyor, hepsini beyaz yapmak o ayrımı siliyordu.
        static readonly Color HudInk    = new Color(0.612f, 0.569f, 1.000f);  // #9C91FF
        static readonly Color HudPlate  = new Color(0.094f, 0.078f, 0.220f);  // #18143 8
        static readonly Color HudClockInk = new Color(1.000f, 0.992f, 0.965f); // #FFFDF6
        static readonly Color HudGlyph  = new Color(0.816f, 0.784f, 1.000f);  // #D0C8FF

        const float WinCardX0 = 0.060f, WinCardX1 = 0.940f;
        const float WinCardY0 = 0.300f, WinCardY1 = 0.755f;

        GameSession _session;

        TextMeshProUGUI _levelLabel, _timerLabel, _livesLabel, _coinLabel, _hintLabel;
        TextMeshProUGUI _hardLabel;
        readonly RectTransform[] _powerBadge = new RectTransform[3];
        readonly RectTransform[] _powerPrice = new RectTransform[3];
        readonly TextMeshProUGUI[] _powerPriceText = new TextMeshProUGUI[3];
        RectTransform _resultPanel, _pausePanel, _promptPanel, _failurePanel;
        TextMeshProUGUI _failureText;
        ContinueOffer _offer;

        /// <summary>
        /// Bu denemede "devam et" teklifi zaten gösterildi mi? Teklif deneme
        /// başına BİR KEZ çıkar: reddedip FAILED'e düştükten sonra sayaç
        /// yeniden dolduğunda aynı iki paneli tekrar görmek oyuncuyu boğardı.
        /// Yeni deneme başlayınca sıfırlanıyor.
        /// </summary>
        bool _offerShown;
        PowerUpSystem _boundPowerUps;
        CanvasGroup _hudGroup;

        // Yardımcı istemi açıkken katmanların karartma oranları.
        // Üçü de referans karelerinden ÖLÇÜLDÜ, seçilmedi (bkz. SetPromptDim).
        const float HudDimAlpha   = 0f;
        const float PowerDimAlpha = 0.40f;

        // DERS (katsayı ekranda gördüğün oran DEĞİLDİR): Zemini %47'ye
        // indirmek için 0.47 yazmak yanlış sonuç verdi — çarpım doğrusal
        // uzayda, ölçtüğümüz PNG ise sRGB. 0.36 katsayısı ekranda 0.23 oldu.
        // Bu değer o yüzden hesapla değil, ÖLÇEREK geri çözüldü.
        const float BgDimFactor   = 0.59f;
        UnityEngine.UI.Image _promptIcon, _promptBurst;
        TMPro.TextMeshProUGUI _promptTitle, _promptText;
        Image _resultCard, _pauseCard;
        TextMeshProUGUI _resultDifficulty;
        TextMeshProUGUI _perfectBadge;
        TextMeshProUGUI _resultTitle, _resultReward;
        RectTransform _rewardArt;
        Image _rewardBadge;
        Button _resultPrimary, _adButton, _closeButton;
        TextMeshProUGUI _resultPrimaryLabel, _adLabel;

        // Yalnız kaybetme panelinde görünenler (bkz. BuildFailExtras).
        Image _failHeart, _failDenied, _rewardsTag;
        TextMeshProUGUI _failDifficulty;

        // Kaybetme kartı referanstan ölçüldü: `Game over .mp4` 18. sn, 384x832.
        const float LoseCardX0 = 0.057f, LoseCardY0 = 0.255f;
        const float LoseCardX1 = 0.943f, LoseCardY1 = 0.786f;

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
        RectTransform _freezeGroup;
        Image _freezeFill;

        // Referans ölçümü: donma çubuğunun dolu kısmı (18,163,244), izin
        // koyusu ise zemine yakın bir lacivert.
        static readonly Color FreezeIce  = new Color(0.071f, 0.639f, 0.957f);  // #12A3F4
        static readonly Color FreezeWell = new Color(0.055f, 0.114f, 0.294f);  // #0E1D4B
        RectTransform _comboBadge;
        TextMeshProUGUI _comboLabel;
        int _shownChain = -1;

        public void Init(GameSession session)
        {
            _session = session;
            BuildUi();
            BindPowerUps();
            Refresh();
        }

        /// <summary>
        /// Yardımcı olaylarına bağlanır. HER KAREDE yoklanıyor; sebebi iki tane
        /// ve ikisi de tek seferlik aboneliği sessizce öldürüyordu:
        ///
        /// DERS 1 (kurulum sırası): <see cref="GameSession.EnsureServices"/> bu
        /// ekranı kurarken <see cref="GameSession.PowerUps"/> HENÜZ YOK — o
        /// nesne bölüm kurulurken (`Restart`) doğuyor. Init içindeki
        /// `if (PowerUps != null)` bu yüzden hep false'tu ve abonelik HİÇ
        /// kurulmadı. Olayın sıfır dinleyicisi vardı: ne yardımcı istemi ne de
        /// ondan önceki ipucu yazısı çalışıyordu.
        ///
        /// DERS 2 (nesne her bölümde YENİLENİYOR): `Restart` her çağrıldığında
        /// `PowerUps = new PowerUpSystem(...)` çalışıyor. Tek seferlik bir
        /// abonelik doğru anda kurulsa bile ilk yeniden başlatmada ÖLÜ bir
        /// nesneyi dinler kalırdı — ilk bölümde çalışıp sonra susan, teşhisi
        /// zor bir hata. Örneği izlemek ikisini birden çözüyor.
        /// </summary>
        void BindPowerUps()
        {
            var current = _session != null ? _session.PowerUps : null;
            if (ReferenceEquals(current, _boundPowerUps)) return;

            if (_boundPowerUps != null)
            {
                _boundPowerUps.Message -= OnPowerUpMessage;
                _boundPowerUps.Used -= OnPowerUpUsed;
                _boundPowerUps.Refused -= OnPowerUpRefused;
            }

            _boundPowerUps = current;

            if (_boundPowerUps != null)
            {
                _boundPowerUps.Message += OnPowerUpMessage;
                _boundPowerUps.Used += OnPowerUpUsed;
                _boundPowerUps.Refused += OnPowerUpRefused;
            }
        }

        void OnPowerUpMessage(string text)
        {
            _hint = text;
            _hintUntil = Time.unscaledTime + 2.5f;
            ShowPowerUpPrompt(text);
        }

        /// <summary>Yardımcı devreye girdi: her birinin kendi sesi var.</summary>
        void OnPowerUpUsed(PowerUpKind kind) => Services.AudioService.PowerUp(kind);

        /// <summary>
        /// Jeton yetmedi: reddi GÖRÜNÜR ve DUYULUR kıl.
        ///
        /// DERS (2.5 saniyelik bir yazı "hayır" demez): Bu durumda tek geri
        /// bildirim yardımcı çubuğunun üstünde beliren küçük bir satırdı.
        /// Oyuncunun gözü o an bastığı DÜĞMEDE; ekranın başka bir yerinde
        /// beliren 28 punto bir yazıyı çoğu zaman hiç görmüyor ve "bastım,
        /// bozuk" diye okuyor. Reddi, reddedilen şeyin ÜSTÜNDE göstermek
        /// gerekiyor: sarı fiyat kapsülü sarsılıyor ve red sesi çalıyor.
        /// Sarsılan şeyin fiyat olması sebebi de anlatıyor.
        /// </summary>
        void OnPowerUpRefused(PowerUpKind kind)
        {
            Services.AudioService.Refuse();

            // Medium, Light değil: `Haptics.Threshold` varsayılan olarak
            // Medium'dur ve altındaki her şeyi sessizce yutar — Light yazmak
            // "titreşim ekledim" sanıp hiç titretmemek olurdu.
            _session?.Haptics?.Play(GameKit.Services.HapticStrength.Medium);

            int index = (int)kind;
            if (index < 0 || index >= _powerPrice.Length) return;

            var capsule = _powerPrice[index];
            if (capsule == null) return;

            GameKit.FX.Juice.Replace(capsule,
                GameKit.FX.Juice.PunchScale(capsule.transform, 0.22f));
        }

        /// <summary>
        /// İstem kipinin karartması. TAHTAYA DOKUNULMUYOR.
        ///
        /// DERS (seçtireceğin şeyi karartamazsın): İlk kurulumda bu, HUD
        /// kanvasına serilen tam ekran bir perdeydi. Kanvas
        /// <c>ScreenSpaceOverlay</c> olduğu için perde dünyadaki her şeyin —
        /// yani TAHTANIN da — üstüne biniyordu. Oysa oyuncudan tam o anda
        /// bir blok SEÇMESİ isteniyor; seçeceği şeyi karartmak hem referansa
        /// aykırı hem de kendi içinde tutarsız.
        ///
        /// Referans karesi bunu sayıyla söylüyor (menüler videosu, istemli
        /// 66-67. sn ile istemsiz 64/68/70. sn kareleri):
        ///     boş zemin      49.0 -> 23.0   (%47'ye iniyor)
        ///     TAHTA         119.5 -> 119.4  (HİÇ DEĞİŞMİYOR)
        /// Yani karartma tahtanın ÜSTÜNE değil, ETRAFINA uygulanıyor.
        ///
        /// DERS (ortalama yalan söyler, TEPE değer söylemez): HUD'ın ortalama
        /// parlaklığı 49.3 -> 23.4 düşüyor ve bu "yarı yarıya soluyor" gibi
        /// okunuyor — ilk kurulumda alfayı 0.48 yapan da buydu. Ama ortalamanın
        /// çoğu zaten koyu olan zemin. TEPE değere bakınca gerçek çıkıyor:
        /// Seviye rozeti 255 -> 43, duraklat düğmesi 255 -> 50, karartılmış
        /// zeminin tepesi ise 44. Yani HUD'dan geriye ZEMİNDEN fazlası
        /// kalmıyor: referans onu SOLDURMUYOR, tamamen GİZLİYOR.
        /// Başlığın sayaçla çakışması da bu yüzden referansta yok.
        ///
        /// Bizde tahta 3B mesh, zemin ise kameraya bağlı bir quad; ikisi de
        /// kanvasın altında. O yüzden karartma üç ayrı yerden yapılıyor:
        /// HUD grubu alfayla, zemin <see cref="View.BackgroundView.SetDim"/>
        /// ile, yardımcı düğmeleri de tek tek — seçilen parlak kalsın diye.
        /// </summary>
        void SetPromptDim(bool on, PowerUpKind? selected)
        {
            if (_hudGroup != null)
            {
                _hudGroup.alpha = on ? HudDimAlpha : 1f;

                // Gizliyken duraklat/yeniden başlat düğmeleri TIKLANMAMALI:
                // görünmeyen bir düğmeye basmak oyuncu için kaza demek.
                _hudGroup.blocksRaycasts = !on;
                _hudGroup.interactable = !on;
            }

            View.BackgroundView.SetDim(Camera.main, on ? BgDimFactor : 1f);

            // Seçilen yardımcı tam parlak, diğer ikisi soluk.
            // (PowerUpKind sırası düğme sırasıyla aynı: Clock, Rocket, Ufo.)
            for (int i = 0; i < _powerButtons.Length; i++)
            {
                var button = _powerButtons[i].button;
                if (button == null) continue;

                var group = button.GetComponent<CanvasGroup>();
                if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();
                group.alpha = !on || (int)selected.Value == i ? 1f : PowerDimAlpha;
            }
        }

        void OnDestroy()
        {
            if (_boundPowerUps != null)
            {
                _boundPowerUps.Message -= OnPowerUpMessage;
                _boundPowerUps.Used -= OnPowerUpUsed;
                _boundPowerUps.Refused -= OnPowerUpRefused;
            }

            View.BackgroundView.SetDim(Camera.main, 1f);
        }

        /// <summary>
        /// Oynanış kökü kapanırken GEÇİCİ ARAYÜZ DURUMUNU sıfırlar.
        ///
        /// DERS (doğru temizlik, YANLIŞ kanca): Bu temizliğin bir kısmı
        /// `OnDestroy`'daydı ve yorumunda "istem açıkken bölüm bırakılırsa
        /// zemin karanlık kalırdı" yazıyordu — sorun görülmüş ama çözüm hiç
        /// çalışmamış. Tek sahnelik yapıda bu ekran HİÇ YOK EDİLMİYOR;
        /// `AppRoot.ShowMenu` yalnızca oynanış kökünü kapatıyor. `OnDestroy`
        /// ancak uygulama kapanırken koşuyor, yani temizlik hiçbir zaman
        /// yapılmıyordu.
        ///
        /// Ölçülen sonuç: yardımcı istemi açıkken menüye dönüp aynı bölüme
        /// yeniden girince HUD alfası 0 kalıyordu (görünmez VE tıklanamaz üst
        /// bar), istem paneli yeni tahtanın üstünde açık duruyordu ve zemin
        /// karartması yerindeydi. Oyuncu için bölüm çalışmaz hâlde başlıyordu.
        ///
        /// `Refresh` bunu kurtaramaz: durum geçişini görmesi için bir kare
        /// koşması gerekir, oysa kök aynı karede kapanıyor.
        /// </summary>
        void OnDisable()
        {
            if (_resultDelay != null) { GameKit.FX.Juice.Stop(_resultDelay); _resultDelay = null; }

            if (_promptPanel != null) _promptPanel.gameObject.SetActive(false);
            if (_pausePanel != null) _pausePanel.gameObject.SetActive(false);
            if (_resultPanel != null) _resultPanel.gameObject.SetActive(false);
            if (_offer != null) _offer.Hide();

            _offerShown = false;
            _hint = "";
            _hintUntil = 0f;

            // Bir sonraki açılışta durum yeniden değerlendirilsin. StopLevel
            // durumu Intro'ya çektiği için gölge durumu da oraya çekiyoruz;
            // aksi halde "Lost" olarak kalır ve yeni bölümde panel açılmazdı.
            _shownState = GameState.Intro;

            // Karartmayı en son geri al: hem HUD'ı hem yardımcı düğmelerini
            // hem de zemini birlikte toparlıyor.
            SetOfferDim(false);
        }

        // ------------------------------------------------------------------ kurulum

        void BuildUi()
        {
            var canvas = UiKit.CreateCanvas("GameplayCanvas");
            canvas.transform.SetParent(transform, worldPositionStays: false);
            canvas.sortingOrder = 5;
            var root = UiKit.CreateSafeArea(canvas);
            GameKit.UI.UiTweak.Mark(root, "GameplayScreen");

            // Üst şerit ve combo rozeti KENDİ grubunda: yardımcı istemi
            // açılınca hep birlikte soluyorlar (bkz. SetPromptDim).
            //
            // Yardımcı çubuğu bilerek bu grubun DIŞINDA: orada seçilen düğme
            // parlak kalıp diğer ikisi soluyor, bunu grup alfası yapamaz —
            // iç içe CanvasGroup alfaları ÇARPILIR, bir çocuk asla
            // ebeveyninden parlak olamaz.
            var hud = UiKit.CreateRect("Hud", root);
            UiKit.Place(hud, 0f, 0f, 1f, 1f);
            _hudGroup = hud.gameObject.AddComponent<CanvasGroup>();

            BuildTopBar(hud);
            BuildComboBadge(hud);
            BuildPowerUpBar(root);
            BuildPausePanel(root);
            BuildRetryConfirmPanel(root);
            BuildPowerUpPrompt(root);
            BuildResultPanel(root);

            // Kaybetme akışının ilk iki aşaması. Sonuç kartından SONRA
            // kuruluyor: ikisi aynı anda açık olmuyor ama teklif kapanırken
            // kartın üstünde kalmalı.
            _offer = ContinueOffer.Build(root);

            BuildFailurePanel(root);

            // Oynanış ekranı uygulama açılışında DEĞİL, ilk bölümde kuruluyor;
            // bu yüzden AppRoot'un tek seferlik uygulaması onu kaçırır.
            GameKit.UI.UiTweak.ApplyAll();
        }

        /// <summary>
        /// Bölüm kurulamadığında ekranı kaplayan hata kartı.
        ///
        /// DERS (hata mesajını SIĞMAYAN bir yere yazmak, yazmamakla aynı şey):
        /// Yükleme hatası önce yardımcı çubuğunun üstündeki ipucu satırına
        /// basılıyordu. O satır 28 punto, tek satır ve `NoWrap` — yani
        /// "Level kurulamadı\nExecutionEngineException: ..." gibi iki satırlık
        /// bir metin ekrandan taşıp okunmaz oluyordu. Cihazda konsol olmadığı
        /// için teşhis yine mümkün değildi: mekanizma vardı, görünürlük yoktu.
        ///
        /// Bu kart ekranın ortasında, sarmalı açık ve otomatik küçülen bir
        /// yazıyla duruyor; hata metninin tamamı okunabiliyor.
        /// </summary>
        void BuildFailurePanel(Transform root)
        {
            _failurePanel = UiKit.CreateRect("LoadFailure", root);
            UiKit.Place(_failurePanel, 0.04f, 0.300f, 0.96f, 0.720f);

            var rim = UiKit.CreateRoundedPanel("Rim", _failurePanel,
                new Color(0.318f, 0.043f, 0.078f));
            UiKit.SetSliceScale(rim, 0.34f);
            rim.raycastTarget = true;      // altındaki tahtaya dokunma geçmesin
            UiKit.Place(rim, 0f, 0f, 1f, 1f);

            var face = UiKit.CreateRoundedPanel("Face", _failurePanel,
                new Color(0.106f, 0.055f, 0.129f));
            UiKit.SetSliceScale(face, 0.36f);
            face.raycastTarget = false;
            UiKit.Place(face, 0f, 0f, 1f, 1f, padding: 10f);

            var title = UiKit.CreateTitle("Title", _failurePanel, "LEVEL LOAD FAILED", 48,
                new Color(1f, 0.55f, 0.50f), new Color(0.25f, 0.02f, 0.05f));
            UiKit.Place(title, 0.06f, 0.800f, 0.94f, 0.940f);

            _failureText = UiKit.CreateLabel("Detail", _failurePanel, "", 34,
                new Color(1f, 0.88f, 0.86f));
            // Sarmal AÇIK ve taşma kısıtlı: `CreateLabel` ikisini de kapalı
            // kuruyor (kısa etiketler için doğru), uzun hata metni için değil.
            _failureText.textWrappingMode = TMPro.TextWrappingModes.Normal;
            _failureText.overflowMode = TMPro.TextOverflowModes.Truncate;
            _failureText.enableAutoSizing = true;
            _failureText.fontSizeMax = 34;
            _failureText.fontSizeMin = 18;
            _failureText.alignment = TMPro.TextAlignmentOptions.Top;
            UiKit.Place(_failureText, 0.07f, 0.100f, 0.93f, 0.780f);

            _failurePanel.gameObject.SetActive(false);
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
            //
            // JETON SAYACININ KENDİ PLAKASI VAR (kullanıcı bulgusu 44).
            // Bizde yazı doğrudan zeminin üstündeydi; referansta koyu lacivert,
            // yuvarlak köşeli bir plakanın içinde duruyor ve jeton simgesi
            // plakanın SOL UCUNDAN taşıyor. Plaka olmayınca sayı, arkasındaki
            // tahtanın rengine göre bazen okunmuyordu.
            var coinPlate = UiKit.CreateRoundedPanel("CoinPlate", root, HudPlate);
            UiKit.Place(coinPlate, 0.075f, 0.944f, 0.365f, 0.986f);
            coinPlate.raycastTarget = false;

            var coin = UiKit.CreateIcon("Coin", root, UiSkin.Get(Art.Coin));
            UiKit.Place(coin, 0.035f, 0.940f, 0.125f, 0.990f);

            _coinLabel = UiKit.CreateTitle("Coins", coinPlate.transform, "", 34, HudInk,
                new Color(0.10f, 0.07f, 0.24f));
            UiKit.Place(_coinLabel, 0.20f, 0.06f, 0.94f, 0.94f);
            _coinLabel.alignment = TextAlignmentOptions.Center;

            // BÖLÜM PLAKASI: hazır `panel_dark` görseli MOR ve boyandığında
            // bile referansın koyu lacivertine inmiyordu (boyama çarpma).
            // Prosedürel yuvarlak panel doğru rengi birebir veriyor.
            var levelPill = UiKit.CreateRoundedPanel("LevelPill", root, HudPlate);
            UiKit.Place(levelPill, 0.700f, 0.944f, 0.965f, 0.986f);

            _levelLabel = UiKit.CreateTitle("Level", levelPill.transform, "", 28, HudInk,
                new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(_levelLabel, 0.05f, 0.06f, 0.95f, 0.94f);

            // DONMA GÖSTERGESİ: jeton ile bölüm plakasının ARASINDA, aynı
            // satırda (2. tur, 48. madde).
            //
            // Referans ölçümü (`menus,powerups,vs.mp4` 01:25, 384x832 kare):
            // kar tanesi x=146..162, çubuk izi x=164..245, ikisi de y=55..66.
            // Jeton sayacı da aynı satırda; yani üst şerit "jeton | donma |
            // bölüm" diye okunuyor.
            //
            // DERS (süreli bir etkinin SÜRESİ görünmeli): Kalan süreyi yalnız
            // ipucu yazısına yazıyorduk ("Time frozen: 8 sn"). Yazı ekranın
            // ortasında, oyuncunun tahtaya baktığı anda okumadığı bir yerde
            // duruyordu. Boşalan bir çubuk aynı bilgiyi BAKMADAN verir.
            _freezeGroup = UiKit.CreateRect("FreezeMeter", root);
            UiKit.Place(_freezeGroup, 0.380f, 0.944f, 0.640f, 0.986f);
            _freezeGroup.gameObject.SetActive(false);

            var flake = UiKit.CreateIcon("Flake", _freezeGroup, MenuSprites.Snowflake);
            UiKit.Place(flake, 0f, 0.10f, 0.165f, 0.90f);
            flake.color = FreezeIce;

            var freezeTrack = UiKit.CreateRoundedPanel("Track", _freezeGroup, FreezeWell);
            UiKit.Place(freezeTrack, 0.21f, 0.32f, 1f, 0.70f);
            freezeTrack.raycastTarget = false;

            _freezeFill = UiKit.CreateRoundedPanel("Fill", freezeTrack.transform, FreezeIce);
            UiKit.Place(_freezeFill, 0f, 0f, 1f, 1f);
            _freezeFill.raycastTarget = false;

            // --- 2. satır: yeniden başlat | süre | duraklat ---
            var restart = SquareButton(root, "Restart", 0.035f, 0.165f, 0.878f, 0.936f);
            BuildRestartGlyph(restart.transform);
            // ARTIK DOĞRUDAN YENİDEN BAŞLATMIYOR — önce onay paneli açılıyor.
            // Gerekçe için bkz. GameSession.GiveUp ve BuildRetryConfirmPanel.
            restart.onClick.AddListener(ShowRetryConfirm);

            // SÜRE PLAKASI: jeton/bölüm plakasıyla AYNI koyu lacivert, ama
            // yazısı krem beyaz ve daha büyük — referansta üç yazı üç ayrı rol
            // taşıyor (jeton ve bölüm açık mor bilgi, süre ise ana gösterge).
            var timer = UiKit.CreateRoundedPanel("TimerPill", root, HudPlate);
            UiKit.Place(timer, 0.300f, 0.878f, 0.700f, 0.936f);

            var clock = UiKit.CreateIcon("Clock", timer.transform, UiSkin.Get(Art.Clock));
            UiKit.Place(clock, 0.05f, 0.14f, 0.24f, 0.86f);

            _timerLabel = UiKit.CreateTitle("Timer", timer.transform, "", 42, HudClockInk,
                new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(_timerLabel, 0.26f, 0.06f, 0.94f, 0.94f);

            // Zor bölüm uyarısı sayacın HEMEN ALTINDA — referansta orada.
            // Kendi kapsülü yok, doğrudan yazı; sayacın kapsülüne
            // yapışık durması "bu sürenin bir notu" gibi okunmasını sağlıyor.
            _hardLabel = UiKit.CreateTitle("Hard", root, "Hard Level", 26,
                HardPink, new Color(0.20f, 0.05f, 0.15f));
            UiKit.Place(_hardLabel, 0.330f, 0.845f, 0.670f, 0.878f);
            _hardLabel.gameObject.SetActive(false);

            var pause = SquareButton(root, "Pause", 0.835f, 0.965f, 0.878f, 0.936f);
            // Duraklat simgesi de çiziliyor: "II" yazıyla da olurdu ama yazı
            // tipinin harf aralığı iki çubuğu eşit yapmıyor ve simge eğri duruyor.
            for (int bar = 0; bar < 2; bar++)
            {
                // Çubuklar SAF BEYAZ değil: referansta #D0C8FF, yani menekşe
                // yüzeyin üstünde açık lavanta. Beyaz simge o yüzeyde fazla
                // sert kalıyor ve düğmeyi "yapıştırılmış" gösteriyordu.
                var stripe = UiKit.CreateRoundedPanel($"Bar_{bar}", pause.transform, HudGlyph);
                UiKit.SetSliceScale(stripe, 1.4f);
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
            // Gerçek ikon geldiyse (38. madde) tek görsel yeter; prosedürel
            // halka + döndürülmüş kare yalnız yedek.
            var art = UiSkin.Get(Art.Restart);
            if (art != null)
            {
                var icon = UiKit.CreateIcon("Glyph", parent, art);
                UiKit.Place(icon, 0.18f, 0.18f, 0.82f, 0.82f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                return;
            }

            var ring = UiKit.CreateRect("Glyph", parent);
            UiKit.Place(ring, 0.20f, 0.20f, 0.80f, 0.80f);

            var circle = ring.gameObject.AddComponent<Image>();
            circle.sprite = GameKit.UI.UiSprites.RoundedOutline;
            circle.type = Image.Type.Sliced;
            UiKit.SetSliceScale(circle, 0.10f);
            circle.color = HudGlyph;
            circle.raycastTarget = false;

            // Ok başı: halkanın sağ üstünde küçük bir üçgen. Döndürülmüş bir
            // kare, üçgen sprite'ı üretmeden aynı okumayı veriyor.
            var head = UiKit.CreateRoundedPanel("Head", ring, HudGlyph);
            UiKit.SetSliceScale(head, 1.2f);
            head.raycastTarget = false;
            UiKit.Place(head, 0.56f, 0.62f, 1.02f, 1.08f);
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        /// <summary>
        /// Üst bardaki kare düğme — ÜÇ KATMAN, referanstan ölçüldü (4. tur F17).
        ///
        /// Kullanıcı: "Yeniden başla ve durdur butonları daha renkli / koyu mor
        /// olmalı; beyaz kısımları kesik duruyor."
        ///
        /// DERS (hazır bir görsel, ölçülmemiş bir karardır): Düğme
        /// `btn_square` sprite'ıyla kuruluyordu. O görsel soluk bir leylak ve
        /// simge de açık lavanta olduğu için düğme ekranda neredeyse
        /// kayboluyordu — "beyaz kısımlar kesik" bulgusu tam olarak bu düşük
        /// kontrasttan geliyor. Referans ÖLÇÜLDÜ (Levels 1-20 yürüyüşü, 00:12):
        ///
        ///   dış kenar  #2A1D8C  (42, 29, 140)
        ///   gövde      #5D46FC  (93, 70, 252)   ← DOYGUN mor-mavi
        ///   simge      #D2C8FF  (210, 200, 255)
        ///
        /// Gövde ile simge arasındaki parlaklık farkı referansta 2,4 kat;
        /// bizdeki soluk leylakta 1,2 katmış. Üç katman (kenar / gövde / simge)
        /// hem o farkı hem de düğmenin kalınlığını geri getiriyor.
        /// </summary>
        static Button SquareButton(Transform root, string name,
            float x0, float x1, float y0, float y1)
        {
            var button = UiKit.CreateSpriteButton(name, root, null, null, 0, Ink);
            UiKit.Place(button, x0, y0, x1, y1);

            if (button.targetGraphic is Image face) face.color = HudButtonEdge;

            // Gövde ALT KENARDAN daha çok pay bırakıyor: koyu kenar aşağıda
            // kalınlaşınca düğme "basılabilir bir kapak" gibi okunuyor.
            var body = UiKit.CreateRoundedPanel("Body", button.transform, HudButtonFace);
            body.raycastTarget = false;
            UiKit.Place(body, 0.055f, 0.14f, 0.945f, 0.945f);

            return button;
        }

        /// REFERANS ÖLÇÜMÜ (00:12 karesi) — bkz. <see cref="SquareButton"/>.
        static readonly Color HudButtonEdge = new Color(0.165f, 0.114f, 0.549f);  // #2A1D8C
        static readonly Color HudButtonFace = new Color(0.365f, 0.275f, 0.988f);  // #5D46FC

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
                // DÜĞME ÜÇ KATMAN — referans karesi büyütülerek okundu
                // (`menus,powerups,vs` ve oynanış videosu 00:28, yardımcı
                // düğmesi 98×90 piksel).
                //
                // DERS (tek düz yeşil bir DÜĞME değil, bir LEKEDİR): Buraya
                // uzun süre tek bir yeşil yüzey konuldu ve "ikonlar güzel ama
                // arkasındaki butonlar çok kötü" bulgusu buradan geldi.
                // Referansta üç yeşil var ve üçü de iş yapıyor:
                //   1) koyu dış kenar  → düğmeye hacim verir,
                //   2) orta gövde      → asıl renk,
                //   3) açık iç kuyu    → ikonu içine oturtur, "yuva" hissi.
                // Yarıçap referansta kısa kenarın %22'si; `UiCornerFit`
                // varsayılanı zaten o.
                var button = UiKit.CreateSpriteButton($"Power_{i}", root, null, null, 0, Ink);
                UiKit.Place(button, x0, 0.028f, x0 + 0.150f, 0.122f);

                // ÜÇ YEŞİL, REFERANSTAN ÖLÇÜLDÜ (4. tur, F18).
                //
                // Kullanıcı: "Power-up ikonları daha koyu ve gölgeli olacak —
                // yeşil ikonlar daha ön planda çıkmalı."
                //
                // ÖLÇÜM (00:12 karesi, düğmenin ortasından dikey tarama):
                //   y+2   #1C7521  (28, 117, 33)   ince üst kenar
                //   y+4…  #46F037  (70, 240, 55)   ÜST alan — en parlak
                //   …     #01AB00  (1, 171, 0)     alt gövde
                //   son ~10 piksel #005506 → #003B09  KOYU alt bandı
                //
                // DERS (yeşil TEK ton değil, bir DÜŞÜŞTÜR): Bizde üç katman
                // vardı ama sıralaması tersti — iç kuyu gövdeden KOYUYDU ve
                // aşağıda hiç koyu bant yoktu. Sonuç düz, plastik bir leke:
                // ikonlar arkalarındaki parlak yeşile karışıyordu. Referansta
                // parlaklık yukarıdan aşağıya düşüyor ve en altta sert bir koyu
                // bant var; ikon o düşüşün ortasında durduğu için öne çıkıyor.
                var face = button.targetGraphic as Image;
                if (face != null) face.color = PowerEdge;

                // Gövde alt kenardan 12% pay bırakıyor: koyu bant orada doğuyor.
                var body = UiKit.CreateRoundedPanel("Body", button.transform, PowerBody);
                body.raycastTarget = false;
                UiKit.Place(body, 0.045f, 0.115f, 0.955f, 0.965f);

                // Parlak üst alan: ikonun oturduğu yuva.
                var well = UiKit.CreateRoundedPanel("Well", button.transform, PowerWell);
                well.raycastTarget = false;
                UiKit.Place(well, 0.10f, 0.30f, 0.90f, 0.925f);

                var icon = UiKit.CreateIcon("Icon", button.transform, UiSkin.Get(icons[i]));
                icon.raycastTarget = false;
                UiKit.Place(icon, 0.10f, 0.16f, 0.90f, 0.94f);

                // ADET ROZETİ: sağ alt KÖŞEDEN taşan, koyu kenarlı KIRMIZI
                // YUVARLAK KARE.
                //
                // DERS (yarıçapı elle 0.10 vermek rozeti HAPA çeviriyordu):
                // `SetSliceScale(0.10f)` köşeyi 18/0.10 = 180 piksele çıkarıyor,
                // yani kutu ne olursa olsun tamamen yuvarlanıyor. Referansta
                // rozet 45×50 ve yarıçapı 10 piksel — belirgin biçimde KARE.
                // Oransal hesabı (varsayılan %22) kapatmaya gerek yok.
                var badgeRim = UiKit.CreateRoundedPanel("BadgeRim", button.transform,
                    new Color(0.541f, 0.055f, 0.055f));
                UiKit.Place(badgeRim, 0.60f, -0.14f, 1.10f, 0.36f);

                var badge = UiKit.CreateRoundedPanel("Badge", badgeRim.transform,
                    new Color(0.937f, 0.161f, 0.161f));
                UiKit.Place(badge, 0f, 0f, 1f, 1f, padding: 5f);

                var count = UiKit.CreateTitle("Count", badge.transform, "", 26, Ink,
                    new Color(0.40f, 0.03f, 0.03f));
                UiKit.Place(count, 0.04f, 0.06f, 0.96f, 0.94f);

                // Elde hiç yokken referans KIRMIZI ROZET GÖSTERMEZ: düğmenin
                // altına jeton simgeli sarı bir fiyat kapsülü koyar.
                //
                // DERS (iki farklı bilgi, iki farklı biçim): Adet ve fiyat aynı
                // rozete yazılırsa oyuncu "3" ile "300"ü aynı şey sanır — biri
                // sahip olduğun, öbürü ödemen gereken. Referans ikisini biçimle
                // ayırıyor: adet küçük kırmızı daire, fiyat geniş sarı kapsül
                // ve içinde jeton simgesi. Simge "bu bir PARA" diyor.
                // Fiyat kapsülü de HAP değil yuvarlak kutu: aynı
                // `SetSliceScale(0.10f)` hatası buradaydı. Rozetle aynı
                // ailedenler, aynı yarıçap kuralına uymalılar.
                // FİYAT KAPSÜLÜ ARTIK MOR VE DAHA KALIN (4. tur, F19).
                //
                // Kullanıcı: "Power-upların altındaki para birimi barının arka
                // planı mor olacak (bizde sarı) ve daha kalın olacak."
                //
                // DERS (para SİMGEDEN okunur, zeminden değil): Kapsül sarıydı,
                // çünkü "para = altın" diye düşünülmüştü. Ama sarı zemin,
                // üstündeki altın jeton simgesini yutuyor ve rakam da sarı
                // üstünde okunmuyordu. Parayı anlatan şey jeton simgesi;
                // zeminin işi onu OKUNUR kılmak. Mor zemin hem HUD'un geri
                // kalanıyla aynı dili konuşuyor hem de altını öne çıkarıyor.
                var priceRim = UiKit.CreateRoundedPanel("PriceRim", button.transform,
                    HudButtonEdge);
                UiKit.Place(priceRim, -0.02f, -0.22f, 1.02f, 0.28f);

                var price = UiKit.CreateRoundedPanel("Price", priceRim.transform,
                    HudButtonFace);
                UiKit.Place(price, 0f, 0f, 1f, 1f, padding: 6f);

                var priceCoin = UiKit.CreateIcon("PriceCoin", price.transform,
                    UiSkin.Get(Art.Coin));
                priceCoin.raycastTarget = false;
                UiKit.Place(priceCoin, 0.04f, 0.10f, 0.34f, 0.90f);

                var priceText = UiKit.CreateTitle("PriceText", price.transform, "", 26,
                    Ink, new Color(0.13f, 0.08f, 0.36f));
                UiKit.Place(priceText, 0.34f, 0.06f, 0.96f, 0.94f);

                // Göster/gizle artık KENARLIĞA bakıyor: rozet ve fiyat birer
                // katman kazandı, yalnız iç yüzeyi gizlemek kenarlığı ekranda
                // bırakırdı.
                _powerBadge[i] = badgeRim.rectTransform;
                _powerPrice[i] = priceRim.rectTransform;
                _powerPriceText[i] = priceText;

                var kind = (PowerUpKind)i;
                button.onClick.AddListener(() => _session.PowerUps?.Use(kind));
                _powerButtons[i] = (button, face, count);
            }
        }

        // REFERANS ÖLÇÜMÜ — bkz. BuildPowerUpBar.
        /// <summary>Zorluğa göre sonuç kartının rengi (bkz. RefreshResult).</summary>
        static Color CardTint(Core.LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case Core.LevelDifficulty.SuperHard: return CardCrimson;
                case Core.LevelDifficulty.Hard:      return CardWine;
                default:                             return CardPurple;
            }
        }

        /// <summary>Başlık konturu kartın KOYU tonu olmalı; sabit mor yanlış kalıyordu.</summary>
        static Color OutlineFor(Color card) =>
            new Color(card.r * 0.42f, card.g * 0.36f, card.b * 0.46f, 1f);

        // ÖLÇÜM (41-50 yürüyüşü, 12:24 — "Super Hard" PERFECT kartı): #D8331F.
        static readonly Color CardCrimson = new Color(0.847f, 0.200f, 0.122f);
        static readonly Color CardWine    = new Color(0.729f, 0.239f, 0.475f);

        static readonly Color PowerEdge = new Color(0.000f, 0.290f, 0.035f);  // #004A09
        static readonly Color PowerBody = new Color(0.004f, 0.671f, 0.000f);  // #01AB00
        static readonly Color PowerWell = new Color(0.275f, 0.941f, 0.216f);  // #46F037

        /// <summary>
        /// COMBO ROZETİ EKRANDAN KALDIRILDI (4. tur, K42).
        ///
        /// Kullanıcı: "Combo yazısı — orijinal oyunda var mı kontrol edilecek.
        /// Yoksa kaldırılacak."
        ///
        /// KONTROL EDİLDİ: 1-20 ve 41-50 yürüyüşlerinden çıkarılan ~140 karenin
        /// tamamı tarandı (oynanış, kazanma, duraklama, ödül ekranları dahil).
        /// Referans oyunun HUD'ında jeton, bölüm, yeniden başlat, süre,
        /// duraklat ve üç yardımcı var — **combo göstergesi YOK.**
        ///
        /// DERS (klonlarken "iyi fikir" eklemek, klonu bozar): Combo bizim
        /// eklememizdi ve kendi başına kötü bir fikir değil; ama referansa
        /// birebir benzemek hedefken ekranda referansta OLMAYAN bir şey
        /// bulunması, hedefin kendisiyle çelişiyor. Sayaç mantığı
        /// (<see cref="ComboTracker"/>) yerinde bırakıldı: ses kademelenmesi
        /// ve bölüm sonu "en uzun zincir" istatistiği onu kullanıyor, ikisi de
        /// oyuncuya ekranda bir kutu göstermeden çalışıyor.
        /// </summary>
        void BuildComboBadge(Transform root)
        {
            _comboBadge = null;
            _comboLabel = null;
        }


        /// <summary>
        /// Yardımcı istemi — referansta bu bir BİLDİRİM DEĞİL, bir MOD.
        ///
        /// Referans kare: `menus,powerups,vs.mp4` 67. saniye. Bir yardımcıya
        /// basıldığında tahta kararıyor, sol üstte yardımcının büyük ve parlayan
        /// görseli beliriyor, ortada adı ve altında yönerge duruyor.
        ///
        /// DERS (yazı, mod değişimini anlatamaz): Bizde bu yalnız 2.5 saniyelik
        /// bir satırdı. Oyuncu roketi seçtikten sonra ekran hiç değişmediği
        /// için "bastım mı, basmadım mı?" belirsizliği kalıyordu ve yazı
        /// kaybolduktan sonra hangi kipte olduğunu hatırlatan hiçbir şey yoktu.
        /// Perde ve büyük görsel, kipin AÇIK OLDUĞUNU sürekli söylüyor.
        ///
        /// DERS (perde dokunmayı YUTMAMALI): Oyuncu bu kipte tahtaya dokunacak.
        /// Karartma katmanı `raycastTarget = false` olmalı; olmazsa referanstaki
        /// akışın tamamı çalışmaz — seçim yapılamaz ve kipten çıkılamaz.
        /// </summary>
        void BuildPowerUpPrompt(Transform root)
        {
            _promptPanel = UiKit.CreateRect("PowerUpPrompt", root);
            UiKit.Place(_promptPanel, 0f, 0f, 1f, 1f);

            // Dikey konumlar referans karesinden (menüler videosu 67. sn,
            // 384x832) doğrudan yükseklik oranı olarak alındı — yükseklik
            // oranı en-boydan BAĞIMSIZ, bu yüzden doğrudan aktarılabiliyor.
            _promptBurst = UiKit.CreateIcon("Burst", _promptPanel, UiSprites.Burst,
                new Color(1f, 0.86f, 0.35f, 0.55f));
            UiKit.Place(_promptBurst, -0.07f, 0.765f, 0.38f, 0.977f);

            _promptIcon = UiKit.CreateIcon("Icon", _promptPanel, null);
            UiKit.Place(_promptIcon, 0.026f, 0.808f, 0.286f, 0.934f);

            _promptTitle = UiKit.CreateTitle("Title", _promptPanel, "", 72, Ink, TitleOutline);
            UiKit.Place(_promptTitle, 0.30f, 0.856f, 0.98f, 0.902f);
            UiKit.SetOutline(_promptTitle, TitleOutline);

            _promptText = UiKit.CreateTitle("Text", _promptPanel, "", 34, Ink, TitleOutline);
            UiKit.Place(_promptText, 0.30f, 0.804f, 0.98f, 0.845f);

            _promptPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// İstemi açar/kapatır. Hangi yardımcının seçildiğini olayın metninden
        /// değil <see cref="PowerUpSystem.Pending"/>'den okuyor: metin
        /// çevrilebilir ama Pending oyunun gerçek durumu.
        /// </summary>
        void ShowPowerUpPrompt(string text)
        {
            if (_promptPanel == null) return;

            var pending = _session?.PowerUps?.Pending;
            if (pending == null || string.IsNullOrEmpty(text))
            {
                SetPromptDim(false, null);
                _promptPanel.gameObject.SetActive(false);
                return;
            }

            SetPromptDim(true, pending.Value);

            var kind = pending.Value;
            _promptIcon.sprite = UiSkin.Get(
                kind == PowerUpKind.Clock ? Art.Clock :
                kind == PowerUpKind.Rocket ? Art.Rocket : Art.Ufo);
            _promptTitle.text = PowerUpInfo.Label(kind);
            _promptText.text = text;
            _promptPanel.gameObject.SetActive(true);
        }

        // ---- Yeniden başlat ONAYI (2. tur, 39. madde) ----------------------

        RectTransform _retryPanel, _retryHeart;
        TextMeshProUGUI _retryTitle;
        PrimeTween.Tween _retryPulse;

        /// <summary>
        /// "Bu bölümü baştan alırsan bir can gider" onayı.
        ///
        /// REFERANS: `Game over .mp4`, 15. saniye — oyundaki "Devam Et?"
        /// paneliyle AYNI kalıp (384×832 karede ölçüldü):
        ///   • Panel TAM GENİŞLİK bir bant, y 192-581 (ekranın %30.2-%76.9'u).
        ///   • Üstte mor bir şerit: başlık.
        ///   • Ortada **KOYU GÖMME KUYU** (#2F145A) ve içinde büyük kırık kalp.
        ///     Bu kuyu detayı panelin bütün karakteri: kalp düz mor zemine
        ///     konunca "yapıştırılmış sticker" gibi duruyor.
        ///   • Altta mor şerit: uyarı yazısı + yeşil düğme.
        ///   • Sağ ÜST köşede kırmızı yuvarlak çarpı, panelin kenarına biner.
        ///
        /// Kullanıcının istediği tek fark: başlıkta "Devam Et?" yerine HANGİ
        /// SEVİYEDE olduğumuz yazıyor ve düğme "Tekrar Dene".
        ///
        /// DERS (onay paneli oyunu DURDURMALI): Panel açıkken sayaç işlemeye
        /// devam ederse oyuncu "düşüneyim" derken bölümü kaybediyor — onay
        /// istemek cezaya dönüşüyor. Panel `GameSession.SetPaused(true)` ile
        /// açılıyor, çarpı kapatınca kaldığı yerden devam ediyor.
        /// </summary>
        void BuildRetryConfirmPanel(Transform root)
        {
            _retryPanel = UiKit.CreateRect("RetryConfirm", root);
            UiKit.Place(_retryPanel, 0f, 0f, 1f, 1f);

            var scrim = UiKit.CreatePanel("Scrim", _retryPanel, new Color(0.02f, 0.02f, 0.05f, 0.72f));
            scrim.raycastTarget = true;

            // Bant tam genişlik: referansta kart değil, ekranı kesen bir şerit.
            var band = UiKit.CreateRect("Band", _retryPanel);
            UiKit.Place(band, 0f, 0.302f, 1f, 0.769f);

            var bandFill = band.gameObject.AddComponent<Image>();
            bandFill.color = RetryViolet;
            bandFill.raycastTarget = true;

            // Üst kenardaki ışık şeridi (referansta #7135D3, 4 piksel).
            var lip = UiKit.CreateRect("Lip", band);
            var lipImage = lip.gameObject.AddComponent<Image>();
            lipImage.color = RetryLip;
            lipImage.raycastTarget = false;
            lip.anchorMin = new Vector2(0f, 1f);
            lip.anchorMax = new Vector2(1f, 1f);
            lip.pivot = new Vector2(0.5f, 1f);
            lip.sizeDelta = new Vector2(0f, 9f);
            lip.anchoredPosition = Vector2.zero;

            _retryTitle = UiKit.CreateTitle("Title", band, "", 54, Ink, TitleOutline);
            UiKit.Place(_retryTitle, 0.08f, 0.855f, 0.92f, 0.995f);
            UiKit.SetOutline(_retryTitle, TitleOutline);

            // KOYU GÖMME KUYU — referansta y 236-414, yani bandın %45-%88'i.
            var well = UiKit.CreatePanel("Well", band, RetryWell);
            UiKit.Place(well, 0f, 0.428f, 1f, 0.855f);
            well.raycastTarget = false;

            _retryHeart = UiKit.CreateIcon("Heart", well.transform,
                UiSkin.Get(Art.HeartBroken)).rectTransform;
            UiKit.Place(_retryHeart, 0.34f, 0.08f, 0.66f, 0.92f);

            var warning = UiKit.CreateTitle("Warning", band, "You will lose 1 life!", 38,
                Ink, TitleOutline);
            UiKit.Place(warning, 0.06f, 0.290f, 0.94f, 0.400f);

            var retry = UiKit.CreateSpriteButton("Retry", band,
                UiSkin.Get(Art.ButtonGreen), "Try Again", 46, Ink);
            UiKit.Place(retry, 0.185f, 0.055f, 0.815f, 0.255f);
            retry.onClick.AddListener(ConfirmRetry);

            // Çarpı bandın sağ ÜST köşesine biner (referansta y=183, bandın
            // üst kenarının hemen üstü).
            var close = UiKit.CreateIconButton("Close", _retryPanel, UiSprites.Circle, CloseRed);
            UiKit.Place(close, 0.845f, 0.742f, 0.955f, 0.804f);
            close.onClick.AddListener(CancelRetry);

            var closeMark = UiKit.CreateIcon("Mark", close.transform, UiSprites.Cross, Ink);
            closeMark.raycastTarget = false;
            UiKit.Place(closeMark, 0.24f, 0.24f, 0.76f, 0.76f);

            _retryPanel.gameObject.SetActive(false);
        }

        /// <summary>Onayı açar: oyunu durdurur, kalbi nabız gibi attırır.</summary>
        void ShowRetryConfirm()
        {
            if (_session == null || _session.State != GameState.Playing) return;

            _session.SetPaused(true);
            _retryTitle.text = _scratch.Clear().Append("Level ")
                .Append(_session.DisplayNumber).ToString();
            _retryPanel.gameObject.SetActive(true);
            Services.AudioService.PanelOpen();

            // KALP BÜYÜYÜP KÜÇÜLÜYOR (kullanıcının açık isteği). Sonsuz
            // döngü: panel kapanınca durduruluyor, yoksa tween arkada yaşamaya
            // devam eder ve panel bir daha açıldığında ikinci bir tween daha
            // eklenip kalp titrer.
            if (_retryPulse.isAlive) _retryPulse.Stop();
            _retryPulse = PrimeTween.Tween.Scale(_retryHeart, 1f, 1.12f, 0.55f,
                PrimeTween.Ease.InOutSine, cycles: -1,
                cycleMode: PrimeTween.CycleMode.Yoyo, useUnscaledTime: true);
        }

        void StopRetryPulse()
        {
            if (_retryPulse.isAlive) _retryPulse.Stop();
            if (_retryHeart != null) _retryHeart.localScale = Vector3.one;
        }

        /// <summary>Çarpı: hiçbir şey olmadı, oyun kaldığı yerden devam.</summary>
        void CancelRetry()
        {
            StopRetryPulse();
            _retryPanel.gameObject.SetActive(false);
            Services.AudioService.PanelClose();
            _session?.SetPaused(false);
        }

        /// <summary>
        /// "Tekrar Dene": bölüm kaybedilmiş sayılır ve BAŞARISIZ paneli açılır.
        ///
        /// DERS (aynı sonuca iki ayrı yol yapma): Buradan doğrudan
        /// `_session.Restart()` çağırmak cazipti — kısa ve çalışıyor. Ama o
        /// zaman "kaybettim" ile "pes ettim" iki ayrı akış olurdu ve panel,
        /// ses, analitik, can harcaması dört yerde ayrı ayrı doğru tutulmak
        /// zorunda kalırdı. Pes etmek zaten bir kaybetmedir; mevcut Lost
        /// akışına giriyoruz.
        ///
        /// `_offerShown` önceden işaretleniyor: kullanıcı "Tekrar Dene dersek
        /// BU SEFER başarısız paneli çıkacak" dedi, yani süre teklifleri
        /// (Süre Doldu → Devam Et?) atlanmalı. O teklifler süre dolduğunda
        /// anlamlı; kendi isteğiyle vazgeçen oyuncuya "30 saniye ister misin"
        /// diye sormak saçma olurdu.
        /// </summary>
        void ConfirmRetry()
        {
            StopRetryPulse();
            _retryPanel.gameObject.SetActive(false);
            _offerShown = true;
            _session?.GiveUp();
        }

        void BuildPausePanel(Transform root)
        {
            // Adı "Pause" DEĞİL: HUD'daki duraklat düğmesi de öyle adlanıyor ve
            // hiyerarşide ondan önce geliyor. İsimle arayan her teşhis aracı
            // (ve bu satırı yazan) düğmeyi panel sanıp "panel açık kalmış"
            // diye yanlış sonuca varıyor. Paneller ayırt edici adlansın:
            // PowerUpPrompt, Result, ContinueOffer, PausePanel.
            _pausePanel = UiKit.CreateRect("PausePanel", root);
            UiKit.Place(_pausePanel, 0f, 0f, 1f, 1f);

            var scrim = UiKit.CreatePanel("Scrim", _pausePanel, new Color(0.05f, 0.03f, 0.14f, 0.82f));
            scrim.raycastTarget = true;

            // Kart MOR — referansta krem değil. panel_card'ın alt dudağı
            // sprite'ın kendisinde magenta; krem bırakılınca o pembe şerit
            // ekranda kalıyordu. Mor tint hem referansa uyuyor hem dudağı
            // kartın rengine katıyor.
            _pauseCard = UiKit.CreateSlicedPanel("Card", _pausePanel,
                UiSkin.Get(Art.PanelCard), CardPurple);
            UiKit.Place(_pauseCard, 0.037f, 0.258f, 0.963f, 0.795f);

            // "Pause" kartın üst kenarına biner — PERFECT panelindeki gibi.
            var title = UiKit.CreateTitle("Title", _pausePanel, "Pause", 88, Ink, TitleOutline);
            UiKit.Place(title, 0.20f, 0.762f, 0.80f, 0.832f);
            UiKit.SetOutline(title, TitleOutline);

            var close = UiKit.CreateIconButton("Close", _pausePanel, UiSprites.Circle, CloseRed);
            UiKit.Place(close, 0.861f, 0.732f, 0.963f, 0.778f);
            close.onClick.AddListener(() => SetPaused(false));

            var closeMark = UiKit.CreateIcon("Mark", close.transform, UiSprites.Cross, Ink);
            closeMark.raycastTarget = false;
            UiKit.Place(closeMark, 0.24f, 0.24f, 0.76f, 0.76f);

            // Üç ayar satırı. Referanstaki sıra: Sounds, Musics, Haptics.
            BuildSettingRow(0, UiSprites.Speaker, "Sounds:", 0.785f, 0.869f,
                on => SettingsBinder.SetSounds(on, _session.Audio, _session.Haptics));
            BuildSettingRow(1, UiSprites.MusicNote, "Musics:", 0.660f, 0.744f,
                on => SettingsBinder.SetMusic(on, _session.Audio, _session.Haptics));
            BuildSettingRow(2, UiSprites.Haptics, "Haptics:", 0.535f, 0.619f,
                on => SettingsBinder.SetHaptics(on, _session.Audio, _session.Haptics));

            // İnce ayraç: ayarları eylemlerden ayırıyor.
            var divider = UiKit.CreatePanel("Divider", _pauseCard.transform,
                new Color(1f, 1f, 1f, 0.18f));
            UiKit.Place(divider, 0.133f, 0.498f, 0.873f, 0.506f);

            var resume = UiKit.CreateSpriteButton("Resume", _pauseCard.transform,
                UiSkin.Get(Art.ButtonGreen), "Resume", 46, Ink);
            UiKit.Place(resume, 0.237f, 0.283f, 0.763f, 0.453f);
            resume.onClick.AddListener(() => SetPaused(false));

            // Referansta "Yeniden Başla" YOK — kaldırıldı. Bölümü yeniden
            // başlatmak isteyen HUD'daki geri düğmesini kullanıyor.
            var quit = UiKit.CreateSpriteButton("Quit", _pauseCard.transform,
                UiSkin.Get(Art.ButtonRed), "Quit", 46, Ink);
            UiKit.Place(quit, 0.237f, 0.076f, 0.763f, 0.240f);
            quit.onClick.AddListener(AppRouter.GoHome);

            _pausePanel.gameObject.SetActive(false);
        }

        readonly (Image off, Image on, TextMeshProUGUI offText, TextMeshProUGUI onText)[] _toggles =
            new (Image, Image, TextMeshProUGUI, TextMeshProUGUI)[3];

        /// <summary>
        /// Bir ayar satırı: simge + etiket + Off/On anahtarı.
        ///
        /// DERS (anahtar iki DURUM gösterir, bir tane değil): Tek bir "açık mı"
        /// kutusu yerine referans hem Off hem On'u yan yana gösteriyor ve
        /// aktif olanı yeşile boyuyor. Fark önemli: tek kutuda oyuncu "bu
        /// şu an açık mı, yoksa basınca mı açılacak" diye tereddüt eder.
        /// İki etiketten biri vurguluysa soru kalmaz.
        /// </summary>
        void BuildSettingRow(int index, Sprite icon, string label,
            float y0, float y1, System.Action<bool> apply)
        {
            var card = _pauseCard.transform;

            var glyph = UiKit.CreateIcon($"Icon_{index}", card, icon, Ink);
            glyph.raycastTarget = false;
            UiKit.Place(glyph, 0.151f, y0, 0.252f, y1);

            var text = UiKit.CreateTitle($"Label_{index}", card, label, 40, Ink, TitleOutline);
            text.alignment = TextAlignmentOptions.Left;
            UiKit.Place(text, 0.279f, y0, 0.526f, y1);

            // Anahtarın gövdesi: koyu kapsül, içinde iki yarı.
            var track = UiKit.CreateSlicedPanel($"Track_{index}", card,
                UiSkin.Get(Art.PanelDark), BadgeDark);
            UiKit.Place(track, 0.553f, y0, 0.863f, y1);

            var offFace = UiKit.CreateSlicedPanel("Off", track.transform,
                UiSprites.RoundedPanel, ToggleIdle);
            UiKit.Place(offFace, 0.045f, 0.12f, 0.495f, 0.88f);
            var offText = UiKit.CreateTitle("OffText", offFace.transform, "Off", 30,
                ToggleIdleInk, TitleOutline);
            UiKit.Place(offText, 0f, 0f, 1f, 1f);

            var onFace = UiKit.CreateSlicedPanel("On", track.transform,
                UiSprites.RoundedPanel, ToggleActive);
            UiKit.Place(onFace, 0.505f, 0.12f, 0.955f, 0.88f);
            var onText = UiKit.CreateTitle("OnText", onFace.transform, "On", 30,
                Ink, TitleOutline);
            UiKit.Place(onText, 0f, 0f, 1f, 1f);

            _toggles[index] = (offFace, onFace, offText, onText);

            // Her iki yarı da tıklanabilir: oyuncu istediği duruma DOĞRUDAN
            // basıyor. Tek düğmeli "değiştir" davranışı bir fazladan adım.
            AddToggleClick(offFace, index, false, apply);
            AddToggleClick(onFace, index, true, apply);
        }

        void AddToggleClick(Image face, int index, bool value, System.Action<bool> apply)
        {
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;

            // DERS (his TEK BİR bileşenden geliyorsa, onu unutmak sessizlik
            // demektir): Bu altı yarım düğme `UiButtonFeel` taşımıyordu —
            // yani basma animasyonu da, tık sesi de, titreşim de yoktu.
            // Ses burada elle çağrıldığı için eksiklik fark edilmemişti;
            // haptik denetiminde (15. APK bulgusu) 76 düğmeden bu 6'sı çıktı.
            // Elle çağrılan ses satırı da kaldırıldı: artık `UiButtonFeel`
            // hepsini birden veriyor, iki kaynak olsa ses ikiye katlanırdı.
            face.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();

            button.onClick.AddListener(() =>
            {
                apply(value);
                RefreshToggles();
            });
        }

        /// <summary>Kayıttaki değerlere göre üç anahtarı boyar.</summary>
        void RefreshToggles()
        {
            if (!MetaServices.Ready) return;
            var settings = MetaServices.Save.Data.Settings;
            bool[] values = { settings.Sounds, settings.Music, settings.Haptics };

            for (int i = 0; i < _toggles.Length; i++)
            {
                var (off, on, offText, onText) = _toggles[i];
                if (off == null || on == null) continue;

                bool isOn = values[i];
                off.color = isOn ? ToggleIdle : ToggleActive;
                on.color = isOn ? ToggleActive : ToggleIdle;
                offText.color = isOn ? ToggleIdleInk : Ink;
                onText.color = isOn ? Ink : ToggleIdleInk;
            }
        }

        void SetPaused(bool paused)
        {
            if (_session == null) return;
            if (paused && _session.State != GameState.Playing) return;

            _session.SetPaused(paused);
            _pausePanel.gameObject.SetActive(paused);

            if (paused)
            {
                // Anahtarlar açılışta kayıttan tazeleniyor: ayar başka bir
                // ekrandan (Ayarlar) değiştirilmiş olabilir.
                RefreshToggles();
                GameKit.FX.Juice.Replace(_pauseCard,
                    GameKit.FX.Juice.CardEntrance(_pauseCard.transform));
                Services.AudioService.PanelOpen();
            }
            else
            {
                Services.AudioService.PanelClose();
            }
        }

        /// <summary>
        /// Ödülün arkasındaki ışın çelengi: merkezden dışa açılan ince kamalar.
        ///
        /// Kamalar tam daireye eşit aralıkla yayılıyor ve her biri kendi
        /// ucundan (pivot alt uçta) döndürülüyor; böylece merkezde birleşip
        /// dışa doğru açılıyorlar. Tek renk ve düşük alfa: amaç ışık hissi,
        /// desen değil.
        /// </summary>
        static void BuildSunburst(RectTransform parent)
        {
            const int rays = 16;
            var holder = UiKit.CreateRect("Sunburst", parent);
            UiKit.Place(holder, -0.22f, -0.18f, 1.22f, 1.18f);

            for (int i = 0; i < rays; i++)
            {
                var ray = UiKit.CreateRect($"Ray_{i}", holder);
                ray.anchorMin = ray.anchorMax = new Vector2(0.5f, 0.5f);
                ray.pivot = new Vector2(0.5f, 0f);
                ray.sizeDelta = new Vector2(i % 2 == 0 ? 26f : 15f, 300f);
                ray.anchoredPosition = Vector2.zero;
                ray.localRotation = Quaternion.Euler(0f, 0f, i * (360f / rays));

                var image = ray.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 0.94f, 0.62f, i % 2 == 0 ? 0.20f : 0.13f);
                image.raycastTarget = false;
            }
        }

        void BuildResultPanel(Transform root)
        {
            _resultPanel = UiKit.CreateRect("Result", root);
            UiKit.Place(_resultPanel, 0f, 0f, 1f, 1f);

            // Perde: altındaki tahtaya dokunmayı da yutar.
            var scrim = UiKit.CreatePanel("Scrim", _resultPanel, new Color(0.05f, 0.03f, 0.14f, 0.84f));
            scrim.raycastTarget = true;

            // Kart MOR — referansta krem değil. panel_card açık renkli olduğu için
            // çarpımla mora boyanabiliyor; panel_dark ya da btn_square ile aynı
            // sonuç ALINAMAZ, koyu bir sprite açık renge boyanmaz.
            _resultCard = UiKit.CreateSlicedPanel("Card", _resultPanel, UiSkin.Get(Art.PanelCard),
                CardPurple);
            UiKit.Place(_resultCard, WinCardX0, WinCardY0, WinCardX1, WinCardY1);

            // "PERFECT!" kartın DIŞINDA, üst kenarına binerek duruyor. Kartın
            // içine alınırsa başlık kutunun bir satırı olur; referansta kartı
            // taşıyan bir tabela gibi davranıyor.
            _resultTitle = UiKit.CreateTitle("Title", _resultPanel, "", 88,
                TitleGold, TitleOutline);
            UiKit.Place(_resultTitle, 0.10f, 0.745f, 0.90f, 0.845f);
            _resultTitle.textWrappingMode = TextWrappingModes.NoWrap;

            // Paylaşılan başlık materyali tek tek kontur ayarı kabul etmiyor;
            // bu etiket kendi kopyasını alıyor (bkz. UiKit.SetOutline).
            UiKit.SetOutline(_resultTitle, TitleOutline);

            // Kapatma çarpısı: kartın sağ üst köşesine biner. Ana ekrana dönüş
            // artık bu — referansta ayrı bir "Ana Ekran" düğmesi yok.
            // btn_red YUVARLAK DEĞİL, yuvarlak köşeli bir dikdörtgen. Referanstaki
            // kapatma düğmesi tam daire; o yüzden zemini çizimden alıyoruz.
            _closeButton = UiKit.CreateIconButton("Close", _resultPanel,
                UiSprites.Circle, CloseRed);
            UiKit.Place(_closeButton, 0.845f, 0.715f, 0.955f, 0.777f);
            _closeButton.onClick.AddListener(AppRouter.GoHome);

            // Çarpı, kırmızı yuvarlağın ÜSTÜNE ayrı bir görsel olarak biniyor.
            var crossMark = UiKit.CreateIcon("Mark", _closeButton.transform, UiSprites.Cross, Ink);
            crossMark.raycastTarget = false;
            UiKit.Place(crossMark, 0.24f, 0.24f, 0.76f, 0.76f);

            // ZORLUK ETİKETİ KARTIN İÇİNDE, BÖLÜM ADININ ÜSTÜNDE (4. tur, J41).
            //
            // REFERANS (41-50 yürüyüşü, 12:24): 49. bölümün PERFECT kartında
            // "Level 49" yazısının hemen üstünde koyu kırmızı "Super Hard"
            // duruyor. Bizde zorluk yalnız OYUN SIRASINDA, sayacın altında
            // görünüyordu; kart açılınca kayboluyor ve "hangi bölümü
            // bitirdim" bilgisinin yarısı gidiyordu.
            //
            // DERS (bilgi, SONUÇ ekranında da anlamlıdır): Oyun içindeki
            // uyarı "dikkat, bu zor" der; sonuç ekranındaki aynı yazı
            // "zoru bitirdin" der. Aynı üç kelime, iki farklı cümle.
            _resultDifficulty = UiKit.CreateTitle("Difficulty", _resultCard.transform, "", 34,
                Ink, TitleOutline);
            UiKit.Place(_resultDifficulty, 0.08f, 0.900f, 0.92f, 0.968f);

            // Bölüm adı kartın içinde, beyaz balon yazı.
            _perfectBadge = UiKit.CreateTitle("LevelName", _resultCard.transform, "", 58,
                Ink, TitleOutline);
            UiKit.Place(_perfectBadge, 0.08f, 0.775f, 0.92f, 0.905f);

            // Jeton yığını: tek bir "coin_pile" görselimiz yok, bu yüzden
            // icon_coin'lerden kuruyoruz. Gerçek yığın görseli gelince burası
            // tek bir Image'a iner — düzen değişmez.
            _rewardArt = UiKit.CreateRect("RewardArt", _resultCard.transform);
            UiKit.Place(_rewardArt, 0.24f, 0.400f, 0.76f, 0.830f);

            // IŞIN ÇELENGİ — jeton yığınının ARKASINDA (4. tur, J39).
            //
            // Kullanıcı: "Ödül gösteren panel birebir orijinaliyle aynı olacak."
            //
            // REFERANS (`Levels 1-20` 00:35 ve 41-50 yürüyüşü 12:24): jetonların
            // arkasından merkeze doğru toplanan açık ışınlar çıkıyor. Bizde
            // yığın kartın üstünde tek başına duruyordu ve "yapıştırılmış bir
            // resim" gibi okunuyordu.
            //
            // DERS (ödülü ödül yapan şey ÇEVRESİDİR): Aynı jeton görseli,
            // arkasında ışın olduğunda "kazandığın şey", olmadığında "bir
            // resim". Işınlar tek tek döndürülmüş ince dikdörtgenler; ayrı
            // bir görsel gerekmiyor.
            BuildSunburst(_rewardArt);
            BuildCoinPile(_rewardArt);

            // Ödül sayısı koyu kapsülün içinde — açık kart üstünde altın rakam
            // okunmuyor, koyu zemin onu geri getiriyor.
            _rewardBadge = UiKit.CreateSlicedPanel("RewardBadge", _resultCard.transform,
                UiSkin.Get(Art.PanelDark), BadgeDark);
            UiKit.Place(_rewardBadge, 0.34f, 0.285f, 0.66f, 0.395f);

            _resultReward = UiKit.CreateTitle("Reward", _rewardBadge.transform, "", 46,
                CoinGold, TitleOutline);
            UiKit.Place(_resultReward, 0.02f, 0.04f, 0.98f, 0.96f);

            // Reklam düğmesi: kazanınca "ödülü ikiye katla", kaybedince
            // "+30 sn ile devam et". İkisi de aynı yerde durur ki oyuncu
            // nereye bakacağını öğrensin.
            // Yeşil "Continue" kartın tek ve büyük eylemi. Hazır btn_green
            // sprite'ı var; boyamaya gerek yok.
            _resultPrimary = UiKit.CreateTintedButton("Primary", _resultCard.transform,
                UiSkin.Get(Art.ButtonGreen), Color.white, "", 44, Ink);
            UiKit.Place(_resultPrimary, 0.12f, 0.075f, 0.88f, 0.245f);
            _resultPrimaryLabel = _resultPrimary.GetComponentInChildren<TextMeshProUGUI>();
            _resultPrimary.onClick.AddListener(OnPrimary);

            // Reklam düğmesi kartın DIŞINDA, altında. Referans kartında yok;
            // içeri alınca düzeni bozuyor, ama akış gerçek olmalı diye duruyor.
            // Dışarıda ve daha küçük olması ikincil olduğunu da söylüyor.
            // DERS (baskılı gölgeyi BOYAYAMAZSIN): Bu düğme panel_card ile
            // kuruluydu ve turuncuya boyanıyordu. O sprite'ın alt kenarında
            // baskılı bir 3B gölge var; krem üstünde doğru duran o gölge
            // turuncuyla çarpılınca kırmızıya kayıyor ve düğmenin altında
            // yanlış renkte bir dudak beliriyordu. Baskılı gölgesi olmayan
            // prosedürel panel bu sorunu ortadan kaldırıyor.
            _adButton = UiKit.CreateTintedButton("Ad", _resultPanel,
                UiSprites.RoundedPanel, new Color(0.94f, 0.62f, 0.06f), "", 26, Ink);
            UiKit.Place(_adButton, 0.16f, 0.185f, 0.84f, 0.252f);
            _adLabel = _adButton.GetComponentInChildren<TextMeshProUGUI>();
            _adButton.onClick.AddListener(OnWatchAd);

            BuildFailExtras();

            _resultPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Yalnız KAYBETME panelinde görünen parçalar.
        /// Referans kare: `Game over .mp4`, 18. saniye (384x832).
        ///
        /// DERS (kaybetmek kazanmanın soluk kopyası değildir): Bizim panel
        /// kaybedince yalnız başlığı değiştirip ödül alanını gizliyordu.
        /// Referans tam tersini yapıyor — ödülü GÖSTERİP üstüne kırmızı çarpı
        /// atıyor. "Şunu kaçırdın" demek, "burada bir şey yok" demekten çok
        /// daha güçlü bir tekrar oynama sebebi.
        /// </summary>
        void BuildFailExtras()
        {
            // Kırık kalp: başlıkla kartın kesiştiği yere biner.
            _failHeart = UiKit.CreateIcon("FailHeart", _resultPanel, UiSkin.Get(Art.HeartBroken));
            UiKit.Place(_failHeart, 0.430f, 0.742f, 0.573f, 0.792f);

            // Zorluk etiketi bölüm numarasının ÜSTÜNDE, küçük ve sade.
            _failDifficulty = UiKit.CreateTitle("Difficulty", _resultCard.transform, "", 30,
                Ink, TitleOutline);
            UiKit.Place(_failDifficulty, 0.20f, 0.780f, 0.80f, 0.822f);

            // Kaçırılan ödülün üstüne binen kırmızı çarpı — rozetin SAĞ ucunda.
            _failDenied = UiKit.CreateIcon("Denied", _rewardBadge.transform, UiSprites.Cross,
                new Color(0.93f, 0.16f, 0.17f));
            UiKit.Place(_failDenied, 0.74f, -0.08f, 1.20f, 1.08f);

            // "Rewards x3" etiketi yeşil düğmenin üst kenarına oturur.
            _rewardsTag = UiKit.CreateSlicedPanel("RewardsTag", _resultCard.transform,
                UiSkin.Get(Art.PanelDark), BadgeDark);
            UiKit.Place(_rewardsTag, 0.370f, 0.260f, 0.641f, 0.312f);
            var tagLabel = UiKit.CreateTitle("Label", _rewardsTag.transform, "Rewards x3", 24,
                CoinGold, TitleOutline);
            UiKit.Place(tagLabel, 0.04f, 0.06f, 0.96f, 0.94f);
        }

        // ---------------------------------------------------------------- ödül görseli

        /// <summary>
        /// Jeton yığınını tek tek jetonlardan kurar.
        ///
        /// DERS (eksik varlığı BEKLEMEK yerine yaklaş): Referanstaki yığın tek
        /// bir çizim. Bizde yok ve gelene kadar panelin boş kalması işi durdurur.
        /// Aynı jetonu farklı boy/konumda üst üste dizmek yeterince yakın bir
        /// siluet veriyor; gerçek görsel gelince tek satırda değişecek.
        ///
        /// Dizilim elle yazıldı, rastgele değil: rastgele yığın her açılışta
        /// başka görünür ve arayüz "oturmamış" hissettirir.
        /// </summary>
        void BuildCoinPile(RectTransform holder)
        {
            var coin = UiSkin.Get(Art.Coin);

            // Işık huzmesi jetonlardan ÖNCE ekleniyor: uGUI çizim sırası çocuk
            // sırasıdır, önce eklenen arkada kalır.
            var glow = UiKit.CreateIcon("Glow", holder, UiSprites.Burst,
                new Color(0.75f, 0.72f, 1f, 0.55f));
            glow.raycastTarget = false;
            UiKit.Place(glow, -0.35f, -0.30f, 1.35f, 1.40f);

            // DERS (yığın = SÜTUN, tümsek değil): Önceki dizilim jetonları bir
            // tümsek gibi yayıyordu ve "masaya saçılmış paralar" okunuyordu.
            // Referanstaki yığın DİKEY İSTİFLERDEN kurulu: her jeton altındakini
            // dörtte üç oranında örtüyor, üst üste binen kenarlar silindir
            // izlenimi veriyor. Yığın hissi örtüşme oranından geliyor —
            // jetonları ayırdığın anda istif dağılıyor.
            const float r = 0.150f;   // jeton yarıçapı
            const float lift = 0.052f; // istifte bir jetonun üsttekine mesafesi

            // (x merkez, taban y, kaç jeton) — arkadan öne doğru.
            var stacks = new[]
            {
                (0.50f, 0.34f, 4),   // orta istif en yüksek, en arkada
                (0.26f, 0.28f, 3),
                (0.74f, 0.28f, 3),
            };

            foreach (var (cx, baseY, count) in stacks)
                for (int i = 0; i < count; i++)
                    AddCoin(holder, coin, cx, baseY + i * lift, r);

            // Öne devrilmiş birkaç jeton: istifleri zemine bağlıyor ve yığının
            // önünü kapatarak derinliği tamamlıyor.
            AddCoin(holder, coin, 0.38f, 0.22f, r * 0.95f);
            AddCoin(holder, coin, 0.62f, 0.22f, r * 0.95f);
        }

        void AddCoin(RectTransform holder, Sprite coin, float cx, float cy, float r)
        {
            var piece = coin != null
                ? UiKit.CreateIcon("Coin", holder, coin)
                : UiKit.CreateIcon("Coin", holder, UiSprites.Circle, CoinGold);
            piece.raycastTarget = false;
            UiKit.Place(piece, cx - r, cy - r, cx + r, cy + r);
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
                        .Append('+').Append(_session.LastReward).ToString();
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
            BindPowerUps();
            Refresh();
        }

        void Refresh()
        {
            // Bölüm hiç kurulamadıysa oyuncu boş bir tahtaya bakıyor demektir;
            // sebebini EKRANIN ORTASINDA söylüyoruz (cihazda konsol yok).
            bool failed = !string.IsNullOrEmpty(_session.LoadFailure);
            if (_failurePanel != null && _failurePanel.gameObject.activeSelf != failed)
                _failurePanel.gameObject.SetActive(failed);

            if (failed)
            {
                if (_failureText != null && _failureText.text != _session.LoadFailure)
                    _failureText.text = _session.LoadFailure;
            }
            else if (_hintLabel != null && _hintLabel.color.g < 0.5f)
            {
                _hintLabel.color = Ink;
            }

            RefreshTimer();
            RefreshCombo();
            RefreshPowerUps();

            // Can/jeton saniyede bir yeter; her karede kurmak savurganlık.
            if (Time.unscaledTime >= _nextSlowTick)
            {
                _nextSlowTick = Time.unscaledTime + 1f;
                if (MetaServices.Ready) MetaServices.Lives.Refresh();
                RefreshMeta();

                // "Hard Level" bölüm başına bir kez değişir; saniyelik turda
                // güncellemek yeterli.
                if (_hardLabel != null)
                {
                    string difficulty = Core.LevelDifficultyRule.Label(_session.Difficulty);
                    _hardLabel.text = difficulty;
                    _hardLabel.gameObject.SetActive(!string.IsNullOrEmpty(difficulty));
                }
            }

            if (_session.State != _shownState)
            {
                var previous = _shownState;
                _shownState = _session.State;

                // Yeni deneme: "devam et" teklifi hakkı geri geliyor.
                //
                // DERS (ARA DURUMLARI GÖRECEĞİNİ VARSAYMA): Burası önce yalnız
                // `Intro`'ya bakıyordu ve hiç çalışmadı — `Restart()` durumu
                // AYNI KAREDE Intro'dan Playing'e taşıyor, `Refresh` ise kare
                // sonundaki hâli görüyor. Yani ara durum hiçbir zaman
                // gözlemlenmiyordu ve teklif ilk kayıptan sonra bir daha
                // açılmıyordu. Doğru soru "hangi duruma girdi" değil,
                // "NEREDEN geldi": Paused dışından Playing'e girmek yeni bir
                // denemedir. Jetonla devam eden oyuncu buraya HİÇ uğramıyor —
                // `OnOfferAccepted` gölge durumu elle eşitliyor — yani aynı
                // denemede teklif ikinci kez çıkmıyor.
                if (_shownState == GameState.Intro ||
                    (_shownState == GameState.Playing && previous != GameState.Paused))
                {
                    _offerShown = false;
                    if (_offer != null && _offer.Visible)
                    {
                        _offer.Hide();
                        SetOfferDim(false);
                    }
                }

                // DERS (oyun BİTTİĞİNDE hemen panel açma): Son blok kapıya
                // doğru kayarken model onu çoktan silmiş oluyor ve zafer olayı
                // ANINDA tetikleniyordu — panel emilme animasyonunun ortasında
                // açılıyor, oyuncu son hamlesinin sonucunu HİÇ GÖRMÜYORDU.
                // Oyun durumu hemen değişmeli (sayaç dursun, girdi kapansın)
                // ama panel, son animasyon bitene kadar beklemeli.
                if (_shownState == GameState.Won || _shownState == GameState.Lost)
                {
                    if (_resultDelay != null) GameKit.FX.Juice.Stop(_resultDelay);
                    _resultDelay = GameKit.FX.Juice.Run(ShowResultAfterBeat());
                }
                else
                {
                    RefreshResult();
                }
            }
        }

        void RefreshTimer()
        {
            int total = Mathf.CeilToInt(_session.Timer.Remaining);
            if (total == _shownSeconds && _session.DisplayNumber == _shownLevel) return;

            _shownSeconds = total;
            _shownLevel = _session.DisplayNumber;

            _levelLabel.text = _scratch.Clear().Append("Level ").Append(_shownLevel).ToString();
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

                // Nabız sessizdi. Oyuncu son saniyelerde tahtaya bakıyor,
                // sayaca değil — uyarının duyulması gerekiyor.
                Services.AudioService.TimerWarning();
            }
        }

        void RefreshCombo()
        {
            int chain = _session.Combo.Chain;
            if (chain == _shownChain) return;
            _shownChain = chain;

            if (chain < ComboTracker.MinimumChain) return;

            // DERS (tekrar eden ses zinciri ÖLDÜRÜR): Burada her halkada aynı
            // "star" sesi çalıyordu. Zincir uzadıkça ödül aynı kalınca oyuncu
            // ilerlediğini DUYMUYOR. Kademeli tizleşen ses aynı olayı
            // "gittikçe iyileşiyor" diye anlatıyor — bedeli sıfır.
            Services.AudioService.Combo(chain);
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

            // DERS (her kare çalışan kod, YOKLUĞA da dayanmalı): Yardımcı adedi
            // kayıttan okunuyor. Meta katmanı hazır değilken bu satır her karede
            // NullReference atıyordu — saniyede altmış istisna, konsol doluyor
            // ve asıl hata (kaydın hiç yüklenmemiş olması) içinde kayboluyordu.
            // RefreshMeta zaten bu korumaya sahipti; burada eksikti.
            if (!MetaServices.Ready) return;

            for (int i = 0; i < 3; i++)
            {
                var kind = (PowerUpKind)i;
                var (_, face, badge) = _powerButtons[i];

                int owned = power.Owned(kind);

                // Referans (21-30 videosu, Level 24): elde varsa küçük KIRMIZI
                // adet rozeti, hiç yoksa jeton simgeli geniş SARI fiyat kapsülü.
                // İkisi aynı anda görünmez.
                bool hasAny = owned > 0;

                if (_powerBadge[i] != null)
                    _powerBadge[i].gameObject.SetActive(hasAny);
                if (_powerPrice[i] != null)
                    _powerPrice[i].gameObject.SetActive(!hasAny);

                if (hasAny)
                {
                    badge.text = owned.ToString();
                    badge.fontSize = 24;
                }
                else if (_powerPriceText[i] != null)
                {
                    _powerPriceText[i].text =
                        _scratch.Clear().Append(PowerUpInfo.Price(kind)).ToString();
                }

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
                        : PowerEdge;
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

            // Kalan süre artık üst şeritteki çubukta; ipucu satırı serbest.
            if (_freezeGroup != null)
            {
                if (_freezeGroup.gameObject.activeSelf != frozen)
                    _freezeGroup.gameObject.SetActive(frozen);

                if (frozen && _freezeFill != null)
                {
                    float share = Mathf.Clamp01(
                        power.FreezeRemaining / PowerUpInfo.ClockFreezeSeconds);
                    var max = _freezeFill.rectTransform.anchorMax;
                    max.x = share;
                    _freezeFill.rectTransform.anchorMax = max;
                }
            }

            if (Time.unscaledTime < _hintUntil)
                _hintLabel.text = _hint;
            else
                _hintLabel.text = "";
        }

        Coroutine _resultDelay;

        WinCelebration _celebration;

        /// <summary>
        /// Son emilme animasyonu bitsin, oyuncu tahtanın boşaldığını GÖRSÜN,
        /// sonra panel açılsın. Kaybetmede bekleme daha kısa: orada görülecek
        /// bir animasyon yok, yalnız sayacın sıfırlandığı an okunsun.
        ///
        /// KAZANMADA ARAYA KUTLAMA GİRİYOR (2. tur, 50. madde): referansta
        /// ödül kartından önce siyah perde, logo ve fişek/konfeti gösterisi
        /// var. Kart ancak gösteri bitince açılıyor.
        /// </summary>
        System.Collections.IEnumerator ShowResultAfterBeat()
        {
            bool won = _shownState == GameState.Won;
            yield return new WaitForSecondsRealtime(won ? 0.75f : 0.35f);
            _resultDelay = null;

            if (!won) { RefreshResult(); yield break; }

            // Kutlama İLK KAZANIŞTA kuruluyor, ekran kurulurken değil:
            // kaybeden ya da hiç bitirmeyen bir oturumda bu kanvas ve
            // logo hiç yaratılmıyor.
            if (_celebration == null) _celebration = WinCelebration.Create(transform);
            _celebration.Play(RefreshResult);

            // GÜVENLİK AĞI: kutlama ne olursa olsun kartı açtırmalı.
            //
            // DERS (bir süsün hatası, akışı kilitlememeli): Kutlama
            // coroutine'inde bir istisna çıktığında `done` geri çağrısı hiç
            // çağrılmıyor ve PERFECT kartı hiç açılmıyordu — yani oyuncu
            // bölümü bitiriyor, ekranda siyah bir perde ve logo kalıyor,
            // devam edecek düğme hiç gelmiyordu. Bir yan etki (görsel şölen)
            // ana akışın (bölüm bitişi) tek dayanağı olmamalı.
            //
            // Kart zaten açıksa `RefreshResult` bir şey değiştirmiyor;
            // bu yüzden ağ, doğru çalışan durumda da zararsız.
            yield return new WaitForSecondsRealtime(4.2f);
            if (_shownState == GameState.Won && _resultPanel != null &&
                !_resultPanel.gameObject.activeSelf)
            {
                Debug.LogWarning("[Sonuç] Kutlama kartı açmadı; güvenlik ağı devrede.");
                RefreshResult();
            }
        }

        /// <summary>Jetonla devam alındı: süre eklenip tahtaya dönülüyor.</summary>
        void OnOfferAccepted()
        {
            SetOfferDim(false);
            _session.ContinueWithExtraTime(ContinueOffer.ExtraSeconds);

            // Durum "Playing"e döndü; bir sonraki karede Refresh bunu yeniden
            // yakalayıp paneli açmasın diye gölge durumu ELLE eşitleniyor
            // (yardımcı isteminde de aynı yol izleniyor).
            _shownState = GameState.Playing;
            _resultPanel.gameObject.SetActive(false);
            Services.AudioService.Purchase();
        }

        /// <summary>İki teklif de reddedildi: asıl FAILED kartı açılıyor.</summary>
        void OnOfferDeclined()
        {
            SetOfferDim(false);
            RefreshResult();
        }

        /// <summary>
        /// Teklif açıkken sahne kararır ve HUD gizlenir.
        ///
        /// Yardımcı istemindeki <see cref="SetPromptDim"/> ile aynı iş, ama
        /// oranı DEĞİL: orada tahta okunabilir kalmalı (%47), burada referans
        /// onu neredeyse siyaha indiriyor (%9) ve karartmanın çoğunu panelin
        /// kendi perdesi yapıyor. Zemin quad'ı kanvasın altında olmadığı için
        /// ona ayrıca dokunuluyor.
        /// </summary>
        void SetOfferDim(bool on)
        {
            if (_hudGroup != null)
            {
                _hudGroup.alpha = on ? 0f : 1f;
                _hudGroup.blocksRaycasts = !on;
                _hudGroup.interactable = !on;
            }

            foreach (var entry in _powerButtons)
            {
                if (entry.button == null) continue;
                var group = entry.button.GetComponent<CanvasGroup>();
                if (group == null) group = entry.button.gameObject.AddComponent<CanvasGroup>();
                group.alpha = on ? 0f : 1f;
                group.blocksRaycasts = !on;
            }

            View.BackgroundView.SetDim(Camera.main, on ? 0.12f : 1f);
        }

        void RefreshResult()
        {
            bool finished = _shownState == GameState.Won || _shownState == GameState.Lost;

            // Kaybetmede FAILED kartı HEMEN açılmıyor: önce referanstaki iki
            // teklif geliyor (Time's Up → Continue?). İkisi de reddedilirse
            // buraya geri dönülüyor ve kart o zaman açılıyor.
            if (finished && _shownState == GameState.Lost && !_offerShown)
            {
                _offerShown = true;
                _resultPanel.gameObject.SetActive(false);
                SetOfferDim(true);
                _offer.Show(OnOfferAccepted, OnOfferDeclined);
                return;
            }

            _resultPanel.gameObject.SetActive(finished);
            if (!finished) return;

            bool won = _shownState == GameState.Won;

            // Referansta kazanma başlığı "PERFECT!", kaybetme başlığı
            // "BAŞARISIZ" — İKİSİ DE ALTIN. Kaybetme başlığını kırmızıya
            // boyamak bizim eklememizdi; referans kaybı RENKLE değil BİÇİMLE
            // anlatıyor (kırık kalp + kaçırılan ödülün üstündeki çarpı).
            _resultTitle.text = won ? "PERFECT!" : "FAILED";

            // KART RENGİ ZORLUKTAN GELİYOR (4. tur, J41).
            //
            // Kullanıcı: "Level 20 perfect ekranı — kazandıktan sonra gelen
            // para ödül ekranı / perfect ekranı birebir aynı olacak."
            //
            // REFERANS: 4. bölümün PERFECT kartı MOR (`Levels 1-20`, 00:35),
            // 49. bölümünki KIRMIZI ve başlığın altında "Super Hard" yazıyor
            // (41-50 yürüyüşü, 12:24). Yani kart bölümün zorluğunu de
            // taşıyor; tek bir mor kart, zor bölümü bitirmenin ayrı bir şey
            // olduğunu söylemiyordu.
            //
            // DERS (aynı olay, farklı AĞIRLIK): Elli bölümün hepsi aynı kartla
            // biterse ellinci kutlama birinciyle aynı hissettirir. Referans
            // yalnız rengi değiştirerek "bu zordu" diyor — yeni bir ekran,
            // yeni bir animasyon gerekmiyor.
            var difficultyTint = won
                ? CardTint(_session != null ? _session.Difficulty : Core.LevelDifficulty.Normal)
                : CardPurple;
            if (_resultCard != null) _resultCard.color = difficultyTint;
            if (_resultTitle != null)
                UiKit.SetOutline(_resultTitle, OutlineFor(difficultyTint));

            if (_resultDifficulty != null)
            {
                string label = won && _session != null
                    ? Core.LevelDifficultyRule.Label(_session.Difficulty) : "";
                _resultDifficulty.text = label;
                _resultDifficulty.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
            _resultTitle.color = TitleGold;

            // Bölüm numarası İKİ DURUMDA DA var — referansta kaybederken
            // "Seviye 54" karttaki en büyük yazı.
            _perfectBadge.gameObject.SetActive(true);
            _perfectBadge.text = _scratch.Clear()
                .Append("Level ").Append(_session.LevelIndex + 1).ToString();

            // Zorluk etiketi yalnız kaybederken ve yalnız zor bölümlerde.
            // Referansta panelde KISA hâli var ("Zor"), HUD'daki uzun hâli
            // ("Zor Seviye") değil — aynı bilgiyi iki kez, iki farklı
            // uzunlukta yazmak kartı gereksiz kalabalıklaştırıyor.
            string difficulty = Core.LevelDifficultyRule.Label(_session.Difficulty)
                .Replace(" Level", string.Empty);
            bool showDifficulty = !won && !string.IsNullOrEmpty(difficulty);
            _failDifficulty.gameObject.SetActive(showDifficulty);
            if (showDifficulty) _failDifficulty.text = difficulty;

            // DERS (kaçırılan ödülü GÖSTER, gizleme): Eski kod kaybedince ödül
            // alanını tamamen kaldırıyordu. Referans onu GÖSTERİP üstüne
            // kırmızı çarpı atıyor. "Şunu kaçırdın" demek, "burada bir şey
            // yok" demekten çok daha güçlü bir tekrar oynama sebebi.
            int shownReward = won ? _session.LastReward : _session.PendingReward;
            bool hasReward = shownReward > 0;
            _rewardArt.gameObject.SetActive(hasReward);
            _rewardBadge.gameObject.SetActive(hasReward);
            _failDenied.gameObject.SetActive(hasReward && !won);
            if (hasReward) _resultReward.text = shownReward.ToString();

            _failHeart.gameObject.SetActive(!won);
            _rewardsTag.gameObject.SetActive(!won);

            bool advance = won && _session.HasNextLevel;
            _resultPrimaryLabel.text = advance ? "Continue" : won ? "Play Again" : "Try Again";

            // Referansta "Yeniden Dene" düğmesi de YEŞİL — kırmızı değil.
            if (_resultPrimary.targetGraphic is Image face) face.color = Color.white;

            UiKit.Place(_resultCard, won ? WinCardX0 : LoseCardX0, won ? WinCardY0 : LoseCardY0,
                                     won ? WinCardX1 : LoseCardX1, won ? WinCardY1 : LoseCardY1);
            UiKit.Place(_resultTitle, 0.10f, won ? 0.745f : 0.784f, 0.90f, won ? 0.845f : 0.848f);
            UiKit.Place(_closeButton, won ? 0.845f : 0.862f, won ? 0.715f : 0.731f,
                                      won ? 0.955f : 0.967f, won ? 0.777f : 0.780f);

            // Kart İÇİ yerleşim (kart-göreli oranlar); kaybetme düzeni
            // `Game over .mp4` 18. saniyeden ölçüldü.
            UiKit.Place(_perfectBadge, won ? 0.08f : 0.244f, won ? 0.815f : 0.655f,
                                       won ? 0.92f : 0.758f, won ? 0.955f : 0.742f);
            UiKit.Place(_rewardArt, won ? 0.24f : 0.229f, won ? 0.400f : 0.429f,
                                    won ? 0.76f : 0.744f, won ? 0.830f : 0.616f);
            UiKit.Place(_rewardBadge, won ? 0.34f : 0.355f, won ? 0.285f : 0.350f,
                                      won ? 0.66f : 0.714f, won ? 0.395f : 0.452f);
            UiKit.Place(_resultPrimary, won ? 0.12f : 0.194f, won ? 0.075f : 0.113f,
                                        won ? 0.88f : 0.802f, won ? 0.245f : 0.271f);

            UiKit.Place(_adButton, 0.16f, won ? 0.185f : 0.150f, 0.84f, won ? 0.252f : 0.215f);

            // Reklam düğmesi: kazanınca katlama (yalnız ödül varsa), kaybedince
            // devam etme. Ödül yoksa katlanacak bir şey de yok, düğme gizlenir.
            bool adUseful = won ? hasReward : true;
            _adButton.gameObject.SetActive(adUseful);
            _adButton.interactable = true;
            if (adUseful)
                _adLabel.text = won ? "Watch Ad · Double Reward"
                                    : $"Watch Ad · +{ExtraSeconds}s Continue";

            GameKit.FX.Juice.Replace(_resultCard,
                GameKit.FX.Juice.CardEntrance(_resultCard.transform));
            Services.AudioService.PanelOpen();

            if (won) GameKit.FX.Juice.Run(CelebrateRoutine());
        }

        /// <summary>
        /// Kazanma töreni: başlık iner, bölüm adı yaylanır, jetonlar dökülür.
        ///
        /// DERS (aynı anda değil SIRAYLA): Hepsini birlikte oynatmak görsel
        /// gürültü yapar ve hiçbiri fark edilmez. Aralarına 100-140 milisaniye
        /// koymak, gözün her birini ayrı ayrı görmesini sağlıyor — toplam süre
        /// yarım saniyeyi geçmediği için de oyuncuyu bekletmiyor.
        ///
        /// DERS (yığın TEK PARÇA açılmaz): Jeton yığınını tek bir PopIn ile
        /// açmak "resim belirdi" der. Jetonları 40 milisaniye arayla açmak
        /// "para döküldü" der; aynı varlıklarla tamamen başka bir olay.
        /// </summary>
        System.Collections.IEnumerator CelebrateRoutine()
        {
            yield return new WaitForSecondsRealtime(0.12f);

            if (_resultTitle != null)
                GameKit.FX.Juice.Run(GameKit.FX.Juice.PopIn(_resultTitle.transform, 0.36f));
            Services.AudioService.Star();

            yield return new WaitForSecondsRealtime(0.10f);

            if (_perfectBadge != null && _perfectBadge.gameObject.activeSelf)
                GameKit.FX.Juice.Run(GameKit.FX.Juice.PopIn(_perfectBadge.transform, 0.32f));

            BurstConfetti();

            if (_rewardArt != null && _rewardArt.gameObject.activeSelf)
            {
                yield return new WaitForSecondsRealtime(0.14f);

                // Yığındaki her jeton sırayla düşer. İlk çocuk ışık huzmesi;
                // o yerinde kalıyor, yoksa parlaklık da zıplayarak geliyor ve
                // "arka plan" olmaktan çıkıp bir nesne gibi görünüyor.
                int count = Mathf.Max(0, _rewardArt.childCount - 1);
                var coins = new Transform[count];
                for (int i = 0; i < count; i++) coins[i] = _rewardArt.GetChild(i + 1);
                GameKit.FX.Juice.PopInStaggered(0.04f, coins);

                yield return new WaitForSecondsRealtime(count * 0.04f + 0.08f);

                if (_rewardBadge != null)
                    GameKit.FX.Juice.Run(GameKit.FX.Juice.PopIn(_rewardBadge.transform, 0.30f));
                Services.AudioService.Coin();
            }
        }
    }
}
