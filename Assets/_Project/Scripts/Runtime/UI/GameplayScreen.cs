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
        // ÖLÇÜLDÜ (Levels 1-20, 02:53, PERFECT kartının iç dolgusu): (65,49,192).
        // Eskiden (77,62,237) idi — referanstan belirgin biçimde parlak ve
        // daha doygun; kart "neon" görünüyordu.
        static readonly Color CardPurple   = new Color(0.255f, 0.192f, 0.753f);

        /// <summary>Sonuç kartının koyu kenarı — referanstan (31,16,95).</summary>
        static readonly Color CardRim = new Color(0.122f, 0.063f, 0.373f);
        /// <summary>Koyu kenarın kalınlığı: referansta 18 piksel = 33 birim.</summary>
        const float CardRimWidth = 30f;

        // KARTIN KENARI TEK BANT DEĞİL, DÖRT (8. tur).
        //
        // ÖLÇÜM (ham 59,47 fps kare, kartın solundan y=600'de yatay kesit;
        // 592 piksel genişlikte ekran, dıştan içe):
        //   x 22-33  (11 px)  dış kenar, mordan parlağa       (50,32,165)
        //   x 34-39  ( 6 px)  KOYU oluk                       (30,17,110)
        //   x 40-55  (16 px)  iç kenar, yine mor              (50,32,165)
        //   x 56-59  ( 4 px)  PARLAK iç çizgi                 (90,64,233)
        //   x 60+             düz yüz                         (65,49,192)
        //
        // Bizde tek bir koyu bant vardı. Referansın kartında çıplak gözle de
        // görülen bir İÇ ÇİZGİ var — kenardan yaklaşık 20 piksel içeride
        // dolanan açık mor hat. Tek bantla o hat hiç yoktu ve kart "düz bir
        // dikdörtgen" gibi okunuyordu.
        //
        // DERS (bir kenarı "kalınlık + renk" diye özetleme): İlk ölçümde
        // kesitin yalnız en koyu değeri alınıp "kenar bu" denmişti. Kesitin
        // TAMAMINA bakınca kenarın bir profil olduğu görülüyor; oradaki
        // parlak hat, kartın kabarık durmasını sağlayan şey.
        //
        // Piksel → birim çevrimi: 18 px = 33 birim, yani 1 px ≈ 1,83 birim.
        static readonly Color CardEdge      = new Color(0.196f, 0.125f, 0.647f);
        static readonly Color CardGroove    = new Color(0.118f, 0.067f, 0.431f);
        static readonly Color CardInnerLine = new Color(0.353f, 0.251f, 0.914f);
        const float CardEdgeBand   = 20f;   // 11 px
        const float CardGrooveBand = 11f;   //  6 px
        const float CardInnerBand  = 29f;   // 16 px
        const float CardLineBand   = 7f;    //  4 px
        /// <summary>
        /// Kartın dış köşe yarıçapı (kanvas birimi). Referansın köşesi tam
        /// bir daire değil (süperelips) ama 60 piksellik bir yay — 592
        /// genişlikte, bizim tuvalde 110 birim — görsel olarak birebir
        /// oturuyor.
        /// </summary>
        const float CardCornerRadius = 110f;
        // "PERFECT!" başlığının dizgisi — hepsi referanstan ölçüldü,
        // gerekçesi BuildResultPanel'de.
        const int TitlePoint = 250;
        /// <summary>"FAILED" altı harf ve kartın DIŞINDA duruyor.</summary>
        const int LoseTitlePoint = 160;
        /// <summary>Harf arası (em/100). Referansta harfler 2-5 piksel arayla.</summary>
        const float TitleTracking = -16f;
        /// <summary>Yatay sıkıştırma: referansın tipi Baloo2'den dar.</summary>
        const float TitleCondense = 0.70f;
        /// <summary>SDF gövde şişirme — referansın harfleri belirgin kalın.</summary>
        const float TitleDilate = 0.12f;
        const float TitleOutlineWidth = 0.30f;
        /// <summary>Harfin tepesi — referansta ölçüldü (254,211,7).</summary>
        static readonly Color TitleGoldTop    = new Color(0.996f, 0.827f, 0.027f);
        /// <summary>Harfin dibi — (250,152,0). Aradaki fark yalnız YEŞİL
        /// kanalda: sarıdan turuncuya geçiş bu.</summary>
        static readonly Color TitleGoldBottom = new Color(0.980f, 0.596f, 0.000f);

        static readonly Color TitleGold    = new Color(1f, 0.760f, 0.180f);
        static readonly Color TitleOutline = new Color(0.290f, 0.160f, 0.620f);
        static readonly Color CoinGold     = new Color(1f, 0.820f, 0.320f);
        static readonly Color BadgeDark    = new Color(0.165f, 0.125f, 0.430f);

        // PERFECT kartındaki TEK plaka: ödül sayısı. (Bölüm adınınki 8. turda
        // kaldırıldı — referansta yok, bkz. BuildResultPanel.) Ödül plakası
        // ise ham karede de doğrulandı: rakamların çevresi kartın moru değil,
        // (34,21,100) koyu moru ve kenarları kesintisiz.
        /// <summary>Ödül sayısı plakasının yüzü (#221564).</summary>
        static readonly Color RewardPlateFace = new Color(0.133f, 0.082f, 0.392f);
        /// <summary>Ödül sayısı plakasının kenarı (#382978).</summary>
        static readonly Color RewardPlateRim  = new Color(0.220f, 0.161f, 0.471f);

        // ÖLÇÜLDÜ (`Game over .mp4` 18. saniye, "Ödüller x3" rozeti).
        // ÖLÇÜLDÜ (`Game over .mp4` 2. saniye, yardımcı fiyat kapsülü).
        static readonly Color PriceFill = new Color(0.353f, 0.271f, 0.945f);  // #5A45F1
        static readonly Color PriceEdge = new Color(0.196f, 0.145f, 0.596f);
        static readonly Color PriceInk  = new Color(0.075f, 0.063f, 0.275f);  // #131046

        static readonly Color TagOrange     = new Color(0.965f, 0.616f, 0.129f);
        static readonly Color TagOrangeDark = new Color(0.545f, 0.286f, 0.043f);

        // ÖLÇÜLDÜ (`Game over .mp4` 18. saniye, BAŞARISIZ panelinin dolgusu):
        // (95,32,186). Kaybetme paneli kazanma panelinden BAŞKA bir mor —
        // ikisini aynı renge boyamak "aynı sonuç" izlenimi veriyordu.
        static readonly Color CardFailViolet = new Color(0.373f, 0.125f, 0.729f);
        static readonly Color CloseRed     = new Color(1.000f, 0.157f, 0.165f);  // #FF282A
        /// <summary>Kapatma düğmesinin koyu halkası — ölçüm (212,0,4).</summary>
        static readonly Color CloseRim = new Color(0.831f, 0.000f, 0.016f);
        /// <summary>Çarpının koyu bileziği — referansta ölçüldü (#6E0000).</summary>
        static readonly Color CloseRimDark = new Color(0.431f, 0.000f, 0.000f);
        /// <summary>Çarpının kendisi saf beyaz değil krem (#F4E7D6).</summary>
        static readonly Color CloseInk     = new Color(0.957f, 0.906f, 0.839f);

        /// <summary>
        /// Duraklat panelindeki ayar simgeleri — REFERANSTA KOYU (7. tur, S64).
        ///
        /// Kullanıcı: "Ses / Müzik / Haptik ikonları koyu mor olacak (bizde
        /// beyaz)." Ölçüm (aynı duraklat karesi): hoparlör, nota ve titreşim
        /// simgelerinin en koyu pikselleri (26, 19, 74) = #1A134A. Kartın
        /// yüzeyi #4131C0 olduğuna göre simgeler yüzeyden KOYU tarafa
        /// ayrılıyor, açık tarafa değil.
        ///
        /// DERS (beyaz, "okunur"un eş anlamlısı değil): Bu ekranda beyaz zaten
        /// yazının rengi. Simgeleri de beyaz yapınca simge ile etiket aynı
        /// katmanda okunuyor ve satır tek bir uzun kelime gibi görünüyordu.
        /// Referans simgeyi koyulaştırarak ona ikincil bir rol veriyor.
        /// </summary>
        static readonly Color PauseGlyph   = new Color(0.102f, 0.075f, 0.286f);

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

        /// <summary>
        /// Süre hapındaki saat kadranı — REFERANSTAN ÖRNEKLENDİ (7. tur, U67).
        ///
        /// `…Levels 1-20 Walkthrough.mp4` 01:20 karesinde simgenin en parlak
        /// pikselleri (92, 83, 180) = #5C53B4. Duraklat çubuklarıyla aynı
        /// karede ölçüldü (onlar #DED5FF çıktı), yani bu koyuluk karenin
        /// tonlamasından değil: kadran BİLEREK sönük mor. Sayı krem beyaz
        /// olduğu için simge ondan geri planda kalıyor — hapın içinde asıl
        /// okunacak şey rakam.
        /// </summary>
        static readonly Color HudClockGlyph = new Color(0.361f, 0.325f, 0.706f);

        // YENİDEN ÖLÇÜLDÜ (6. tur; kullanıcı: "PERFECT yazısı özensiz,
        // kötü, yeri alakasız; referanstan bakıp yeri ve boyutu
        // ayarlanmalı").
        //
        // ÖLÇÜM (Levels 1-20, 02:53, 592x1280 kare; ekran oranına normalize,
        // y aşağıdan yukarı):
        //   panel        x 0.044..0.954   y 0.258..0.767
        //   "PERFECT!"   x 0.132..0.862   y 0.781..0.836
        //   kapatma X    x 0.870..0.954   y 0.736..0.775
        //   "Level 8"    x 0.321..0.693   y 0.652..0.699
        //   jeton yığını x 0.279..0.726   y 0.484..0.609
        //   sayı hapı    x 0.389..0.625   y 0.434..0.477
        //   Continue     x 0.245..0.760   y 0.316..0.398
        //
        // Bizim kartımız 0.060..0.940 / 0.300..0.755 idi: hem dar hem kısa.
        // "Continue" ise 0.12..0.88 — kartın %76'sı; referansta %55.
        // Kullanıcının "continue butonu çok uzun yatay" dediği bu.
        // KART REFERANSA GÖRE YENİDEN ÖLÇÜLDÜ (7. tur, R59).
        // `…Levels 1-20 Walkthrough.mp4` 11:31, 592×1280: kart x 32-558
        // (%5,4-%94,3), y 268-948 → alttan %25,9-%79,1. Bizimki %25,8-%76,7
        // idi; kart referanstan 44 birim (%4,5) ALÇAKTI ve "PERFECT!" başlığı
        // üst kenarına BİNMEK yerine üstünde asılı kalıyordu.
        //
        // YENİDEN ÖLÇÜLDÜ (8. tur, 59,47 fps'lik karelerden): 11:31 karesi
        // yeniden kodlanmış bir ekran görüntüsüydü; kenarları bir-iki piksel
        // içeriden okunuyordu. Ham kareden kartın mor kütlesi x 29-564,
        // y 252-949 çıkıyor → %4,9-%95,3 ve alttan %25,9-%80,3.
        const float WinCardX0 = 0.049f, WinCardX1 = 0.953f;
        const float WinCardY0 = 0.259f, WinCardY1 = 0.803f;

        GameSession _session;

        TextMeshProUGUI _levelLabel, _timerLabel, _livesLabel, _coinLabel, _hintLabel;
        TextMeshProUGUI _hardLabel;
        readonly RectTransform[] _powerBadge = new RectTransform[3];
        readonly RectTransform[] _powerPrice = new RectTransform[3];
        readonly TextMeshProUGUI[] _powerPriceText = new TextMeshProUGUI[3];
        RectTransform _resultPanel, _pausePanel, _promptPanel, _failurePanel;

        /// <summary>Süre hapı — duraklatınca yukarı kayıyor (bkz. SlideTimerAway).</summary>
        RectTransform _timerPill;
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
        Image _cardGroove, _cardInner, _cardLine, _cardFace;
        TextMeshProUGUI _resultDifficulty;
        TextMeshProUGUI _perfectBadge;

        TextMeshProUGUI _resultTitle, _resultReward;
        RectTransform _rewardArt;
        Image _rewardBadge;
        Button _resultPrimary, _adButton, _closeButton;

        /// <summary>Ses/müzik/titreşim anahtarları — üçü de aynı ortak kontrol.</summary>
        readonly MenuPage.SwitchView[] _toggles = new MenuPage.SwitchView[3];
        TextMeshProUGUI _resultPrimaryLabel, _adLabel;

        // Yalnız kaybetme panelinde görünenler (bkz. BuildFailExtras).
        Image _failHeart, _failDenied, _rewardsTag;
        TextMeshProUGUI _failDifficulty;

        // Kaybetme kartı referanstan ölçüldü: `Game over .mp4` 18. sn, 384x832.
        // YENİDEN ÖLÇÜLDÜ (5. tur; kullanıcı: "failed ekranı sıkıntılı,
        // orijinaliyle çok benzer değil, bozuk gözüküyor; scale, yapı, tarz
        // olarak birebir benzetilmesi gerekiyor").
        //
        // ÖLÇÜM (`Game over .mp4` 18. saniye, BAŞARISIZ paneli; 384x832 kare,
        // ekran oranına normalize edildi, y aşağıdan yukarı):
        //   panel        x 0.049..0.951   y 0.254..0.700
        //   yeşil düğme  x 0.266..0.734   y 0.308..0.389
        //   sayı hapı    x 0.344..0.711   y 0.422..0.483
        //   "Ödüller x3" x 0.370..0.628   y 0.393..0.418
        //   "Seviye 54"                   y 0.608..0.644
        //
        // Bizim kartımız 0.255..0.786 idi: referanstan **%19 daha uzun**.
        // Uzun kart, içindeki her şeyi de seyreltiyor; kullanıcının "bozuk"
        // dediği şey tek tek öğeler değil, aralarındaki boşluklardı.
        //
        // DERS (oranı bozan tek sayı, her şeyi bozar): İçerideki öğeler kart
        // GÖRELİ yerleştirildiği için kartın yüksekliği yanlış olunca hepsi
        // birden kayıyor. Önce kabı ölç, sonra içindekileri.
        const float LoseCardX0 = 0.049f, LoseCardY0 = 0.254f;
        const float LoseCardX1 = 0.951f, LoseCardY1 = 0.700f;

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

            // PUNTO REFERANSTAN YENİDEN ÖLÇÜLDÜ (7. tur, U68 ile birlikte).
            //
            // ÖLÇÜM (`…Levels 1-20 Walkthrough.mp4` 01:20, 592×1280):
            //   "1040"    büyük harf yüksekliği 19 piksel → ekranın %1.484'ü
            //   "Level 6" büyük harf yüksekliği 19 piksel → AYNI
            //   "01:52"   büyük harf yüksekliği 26 piksel → %2.031
            // 1920 birimlik tuvalde sırasıyla 28,5 / 28,5 / 39 birim.
            // Baloo 2'de büyük harf ≈ puntonun %67,2'si (mağaza başlığından
            // ölçüldü) → 42 / 42 / 58 punto.
            //
            // Bizde 34 / 28 / 42 vardı. Kullanıcının iki maddesi de aynı
            // ölçümden çıkıyor: süre yazısı %38, seviye yazısı %50 küçüktü.
            // Jeton yazısı da 42'ye çıkıyor — referansta jeton ve seviye
            // AYNI puntoda ve biri büyüyüp diğeri kalsaydı satır bozulurdu.
            _coinLabel = UiKit.CreateTitle("Coins", coinPlate.transform, "", 42, HudInk,
                new Color(0.10f, 0.07f, 0.24f));
            UiKit.Place(_coinLabel, 0.20f, 0.06f, 0.94f, 0.94f);
            _coinLabel.alignment = TextAlignmentOptions.Center;

            // BÖLÜM PLAKASI: hazır `panel_dark` görseli MOR ve boyandığında
            // bile referansın koyu lacivertine inmiyordu (boyama çarpma).
            // Prosedürel yuvarlak panel doğru rengi birebir veriyor.
            var levelPill = UiKit.CreateRoundedPanel("LevelPill", root, HudPlate);
            UiKit.Place(levelPill, 0.700f, 0.944f, 0.965f, 0.986f);

            // SEVİYE YAZISI 28 → 42 (7. tur, U68). Ölçüm için bkz. jeton
            // etiketindeki not: referansta jeton ile seviye AYNI puntoda.
            _levelLabel = UiKit.CreateTitle("Level", levelPill.transform, "", 42, HudInk,
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
            // SÜRE HAPI REFERANSA GÖRE YENİDEN ÖLÇÜLDÜ (7. tur, U67).
            //
            // Kullanıcı: "Süre ikonu mor olacak. Süre texti çok daha büyük
            // olacak. Tasarım orijinaliyle birebir aynı olacak."
            //
            // ÖLÇÜM (01:20 karesi, 592×1280): hap x 215-380 (%36,3-%64,2),
            // y 120-171 (yüksekliğin %3,98'i = 76 birim). Bizimki
            // 0.300-0.700 × 111 birimdi: referanstan %43 GENİŞ ve %46
            // YÜKSEK. İçindeki yazı ise küçüktü — bu yüzden hap yarı boş
            // görünüyordu. Yani "süre yazısı küçük" bulgusunun yarısı
            // punto, yarısı hapın kendisiydi.
            //
            // Hap düğmelerin ARASINDA ve onlardan alçak: referansta düğmeler
            // 72 piksel, hap 51 piksel ve hap düğme satırının ortasına
            // oturuyor. İkisi aynı yükseklikte olsaydı üç kutu tek bir şerit
            // gibi okunurdu; referanstaki hiyerarşi buradan geliyor.
            const float rowMid = (0.878f + 0.936f) * 0.5f;
            const float pillHalf = 0.0398f * 0.5f;      // 76 birim / 1920
            var timer = UiKit.CreateRoundedPanel("TimerPill", root, HudPlate);
            UiKit.Place(timer, 0.362f, rowMid - pillHalf, 0.642f, rowMid + pillHalf);
            _timerPill = timer.rectTransform;

            // SAAT SİMGESİ MOR VE İÇİ BOŞ — 3B çalar saat görseli değil.
            // Gerekçe ve ölçüm: GameKit.UI.UiSprites.ClockFace.
            var clock = UiKit.CreateIcon("Clock", timer.transform,
                GameKit.UI.UiSprites.ClockFace, HudClockGlyph);
            UiKit.Place(clock, 0.062f, 0.20f, 0.252f, 0.80f);

            _timerLabel = UiKit.CreateTitle("Timer", timer.transform, "", 58, HudClockInk,
                new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(_timerLabel, 0.30f, 0.04f, 0.94f, 0.96f);

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
                // ROZET KÜÇÜLDÜ VE KÖŞEYE OTURDU (5. tur).
                //
                // Kullanıcı: "powerupların adet göstergesi daha küçük, koyu
                // gölgeli şık bir kırmızı ile gösteriliyor."
                //
                // ÖLÇÜM (Levels 1-20, 02:32 karesi, sol yardımcı düğmesi):
                // düğme 112x89 piksel, rozet 36x34 — yani düğme genişliğinin
                // %31'i. Rozetin SAĞ kenarı düğmenin sağ kenarının bir tık
                // içinde (x %64..%95,5), gövdesinin üçte ikisi düğmenin ALT
                // kenarının altında kalıyor.
                // Bizdeki rozet düğmenin YARISI kadardı ve sağa taşıyordu.
                //
                // Renkler aynı kareden: gövde #DC1612, kenar #7C0200 (altta
                // kalınlaşan koyu bir gölge), rakam saf beyaz, üst kenarda
                // ince bir mercan parlaması (#FF5E40).
                var badgeRim = UiKit.CreateRoundedPanel("BadgeRim", button.transform,
                    new Color(0.486f, 0.008f, 0f));
                UiKit.Place(badgeRim, 0.643f, -0.247f, 0.955f, 0.124f);

                var badge = UiKit.CreateRoundedPanel("Badge", badgeRim.transform,
                    new Color(0.863f, 0.086f, 0.071f));
                UiKit.Place(badge, 0f, 0.10f, 1f, 1f, padding: 3f);

                // Üstteki mercan parlaması: rozete hacim veren tek ayrıntı.
                var badgeShine = UiKit.CreateRoundedPanel("BadgeShine", badge.transform,
                    new Color(1f, 0.369f, 0.251f, 0.85f));
                UiKit.Place(badgeShine, 0.14f, 0.62f, 0.86f, 0.94f);
                badgeShine.raycastTarget = false;

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
                // FİYAT KAPSÜLÜ YENİDEN ÖLÇÜLDÜ (5. tur; kullanıcı:
                // "powerupların para birimi yazdığı kısmın da güncellenmesi
                // lazım").
                //
                // ÖLÇÜM (`Game over .mp4` 2. saniye, üç yardımcının fiyatı):
                // hap PARLAK mavi-mor (#5A45F1), rakam ÇOK KOYU lacivert
                // (#131046) — açık zemin üstünde koyu yazı. Jeton hapın SOL
                // ucunda ve haptan BÜYÜK: dışına taşıyor, yani hap onu
                // çerçevelemiyor, jeton hapın üstüne oturuyor.
                //
                // Bizdeki hap HUD'un koyu morlarıyla boyanmıştı ve yazı
                // açıktı — referansın tam tersi. Koyu hap, koyu HUD'un
                // içinde kaybolyordu.
                var priceRim = UiKit.CreateRoundedPanel("PriceRim", button.transform,
                    PriceEdge);
                UiKit.Place(priceRim, 0.10f, -0.24f, 1.06f, 0.20f);

                var price = UiKit.CreateRoundedPanel("Price", priceRim.transform,
                    PriceFill);
                UiKit.Place(price, 0f, 0f, 1f, 1f, padding: 4f);

                var priceText = UiKit.CreateTitle("PriceText", price.transform, "", 28,
                    PriceInk, new Color(0f, 0f, 0f, 0f));
                UiKit.Place(priceText, 0.30f, 0.04f, 0.94f, 0.96f);

                // Jeton hapın DIŞINDA (kardeşi, çocuğu değil): hapın kenarını
                // aşması gerekiyor, çocuk olsaydı hapın içine sıkışırdı.
                var priceCoin = UiKit.CreateIcon("PriceCoin", priceRim.transform,
                    UiSkin.Get(Art.Coin));
                priceCoin.raycastTarget = false;
                UiKit.Place(priceCoin, -0.20f, -0.18f, 0.30f, 1.18f);

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
        // YEŞİL DÜĞME ARTIK PROSEDÜREL — HAZIR GÖRSEL HAP ŞEKLİNDEYDİ.
        //
        // ÖLÇÜM (ham 59,47 fps kare, 592×1280): düğme x 142-449, y 765-877 —
        // yani 307×112 piksel, kenar oranı 2,74. Köşe yarıçapı yüksekliğin
        // yaklaşık dörtte biri: YUVARLAK KÖŞELİ DİKDÖRTGEN, hap değil.
        //
        // Yüzeyin dikey kesiti üç bant veriyor:
        //   üst  (50,246,22)  parlak
        //   orta (52,211,18)
        //   alt  (22,176,5)   koyu
        // ve bunların çevresinde (20,144,15) bir kenar bandı.
        //
        // `btn_green` sprite'ı ise tam bir HAP (iki ucu yarım daire) ve
        // üstüne basılı bir parlaklık taşıyor. Kartın köşeleriyle aynı dilde
        // konuşmuyordu: kart yuvarlak köşeli dikdörtgen, düğme kapsül.
        //
        // DERS (bu projede yedinci kez): Boyanabilir olan, üstünde KARAR
        // basılı olmayandır. Köşe yarıçapını ayarlayabilmek gerekiyorsa
        // yüzey kodla kurulmalı.

        /// <summary>
        /// Sonuç kartının ana düğmesi ("Continue" / "Play Again" / "Try
        /// Again"). Artık menülerin düğmesiyle AYNI reçeteden geliyor.
        ///
        /// DERS (aynı işi yapan iki kod, er geç iki farklı tasarım olur):
        /// Burada düğmenin kendi dört katmanı elle kuruluyordu — dış halka,
        /// yüz, dip bandı, parlaklık — ve <c>MenuPage.PillButton</c> başka
        /// bir dörtlüyle aynı şeyi yapıyordu. İkisi aynı gün ölçülmüştü ama
        /// zamanla ayrıştı: köşe oranları 0,30/0,26'ya karşı 0,22, puntolar
        /// farklı, dip bandı birinde var birinde yok. Kullanıcıya ulaşan
        /// geri bildirim de tam buydu — "bulunan bütün butonlar tarz olarak
        /// alakasız kalmış". Alakasızlığın sebebi ikisinin de kötü olması
        /// değil, AYNI OLMAMASIYDI.
        ///
        /// Rengi artık tek yerden geliyor; ışık profili (pah, kaymak, etek)
        /// çarpanla türetildiği için ayrıca dört renk tanımlamaya gerek yok.
        /// </summary>
        static Button CreateGreenButton(string name, Transform parent) =>
            MenuPage.PillButton(name, parent, "", MenuPage.Green, 78, null);

        /// <summary>
        /// Kartın beş katmanını tek bir yüz renginden boyar.
        ///
        /// Oranlar mor kartta ÖLÇÜLDÜ ve yüze bölündü:
        ///   dış kenar  (50,32,165) / (65,49,192) = 0,77 · 0,65 · 0,86
        ///   koyu oluk  (30,17,110) / yüz         = 0,46 · 0,35 · 0,57
        ///   iç çizgi   (90,64,233) / yüz         = 1,38 · 1,31 · 1,21
        ///
        /// Çarpanla türetmenin sebebi: kart üç renkte açılıyor (mor, şarap,
        /// kızıl). Her biri için ayrı beş renk yazmak on beş sabit demekti ve
        /// biri unutulduğunda yalnız O zorlukta bozuluyordu. Çarpan, ölçülen
        /// PROFİLİ taşıyor; renk hangisi olursa olsun kenar aynı kabartıyı
        /// veriyor.
        /// </summary>
        void TintCard(Color face)
        {
            if (_resultCard != null)
                _resultCard.color = Scale(face, 0.77f, 0.65f, 0.86f);
            if (_cardGroove != null)
                _cardGroove.color = Scale(face, 0.46f, 0.35f, 0.57f);
            if (_cardInner != null)
                _cardInner.color = Scale(face, 0.77f, 0.65f, 0.86f);
            if (_cardLine != null)
                _cardLine.color = Scale(face, 1.38f, 1.31f, 1.21f);
            if (_cardFace != null) _cardFace.color = face;

            static Color Scale(Color c, float r, float g, float b) => new Color(
                Mathf.Clamp01(c.r * r), Mathf.Clamp01(c.g * g), Mathf.Clamp01(c.b * b), c.a);
        }

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

            var retry = MenuPage.PillButton("Retry", band,
                "Try Again", MenuPage.Green, 46, null);
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

            // ÇARPI: GÖLGE + KOYU KENAR + PARLAK YÜZ (7. tur, S63).
            //
            // Kullanıcı: "Kapat (X) butonu daha koyu ve gölgeli olacak."
            //
            // ÖLÇÜM (`…Levels 1-20 Walkthrough.mp4` 00:04 duraklat karesi,
            // 592×1280): dairenin ÜST kenarı #FF282A, ALT ve yan kenarları
            // #6E0000-#960000 — yani düz bir daire değil, koyu kırmızı bir
            // bileziğin içinde parlak bir yüz. Çarpı da saf beyaz değil krem
            // (#F4E7D6). Bizimki tek düz #E5252E daireydi ve kartın üstünde
            // "yapıştırılmış pul" gibi duruyordu.
            //
            // DERS (bu ekranda beşinci tekrar): Bu arayüzün her yüzeyi üç
            // katmanlı — gölge, koyu kenar, yüz. Tek katmanlı her öğe, ne
            // kadar doğru renkte olursa olsun, yüzeyden kopuk görünüyor.
            var close = UiKit.CreateRect("Close", _pausePanel);
            UiKit.Place(close, 0.861f, 0.732f, 0.963f, 0.778f);

            var closeShadow = UiKit.CreateIcon("Shadow", close, UiSprites.Circle,
                new Color(0f, 0f, 0f, 0.38f));
            UiKit.Place(closeShadow, 0.03f, -0.10f, 1.03f, 0.90f);

            var closeRim = UiKit.CreateIcon("Rim", close, UiSprites.Circle, CloseRimDark);
            UiKit.Place(closeRim, 0f, 0f, 1f, 1f);
            closeRim.raycastTarget = true;

            var closeFace = UiKit.CreateIcon("Face", close, UiSprites.Circle, CloseRed);
            UiKit.Place(closeFace, 0.115f, 0.145f, 0.885f, 0.915f);

            var closeMark = UiKit.CreateIcon("Mark", close, UiSprites.Cross, CloseInk);
            closeMark.raycastTarget = false;
            UiKit.Place(closeMark, 0.26f, 0.28f, 0.74f, 0.76f);

            UiKit.MakeClickable(close.gameObject, closeRim, () => SetPaused(false));

            // Üç ayar satırı. Referanstaki sıra: Sounds, Musics, Haptics.
            BuildSettingRow(0, UiSprites.Speaker, "Sounds:", 0.785f, 0.869f,
                on => SettingsBinder.SetSounds(on, _session.Audio, _session.Haptics));
            BuildSettingRow(1, UiSprites.MusicNote, "Musics:", 0.660f, 0.744f,
                on => SettingsBinder.SetMusic(on, _session.Audio, _session.Haptics));
            BuildSettingRow(2, UiSprites.Haptics, "Haptics:", 0.535f, 0.619f,
                on => SettingsBinder.SetHaptics(on, _session.Audio, _session.Haptics));

            // DÜĞMELER KARTIN İÇ YÜZEYİNE SIĞDIRILDI (7. tur, S62).
            //
            // Kullanıcı: "Quit butonu biraz daha yukarıda olmalı, şu an
            // taşmış."
            //
            // ÖLÇÜM (kendi yakalamamız, 1080×1920): kart 393-1425, yani 1031
            // birim; `panel_card` görselinin ALT DUDAĞI 108 birim, dolayısıyla
            // iç yüzey kart-göreli 0.121'de bitiyor. Quit ise 0.076'da
            // başlıyordu — dudağın 46 birim İÇİNE giriyor, düğmenin alt
            // kenarı kartın kabartmasının üstüne biniyordu.
            //
            // Referansta kartın dudağı 75 birim (0.073) ve Quit 0.082'de,
            // yani dudağın hemen ÜSTÜNDE. Bizim sprite'ımızın dudağı daha
            // kalın olduğu için aynı orana değil, aynı KURALA uyuluyor:
            // düğme dudağa değmiyor. Yer açmak için ayraç ve Resume da yukarı
            // kaydı; ikisi tek bir bütçeyi paylaşıyor (aynı ders Liderlik'te
            // sabit satır ile kaydırma alanı arasında da çıkmıştı).
            //
            //   ayraç  0.512-0.519   (Haptics satırı 0.535'te bitiyor)
            //   Resume 0.330-0.487
            //   Quit   0.140-0.297   (iç yüzey 0.121'de bitiyor)
            var divider = UiKit.CreatePanel("Divider", _pauseCard.transform,
                new Color(1f, 1f, 1f, 0.18f));
            UiKit.Place(divider, 0.133f, 0.512f, 0.873f, 0.519f);

            // GÖRSELDEN REÇETEYE (8. tur). Bu iki düğme `btn_green.png` /
            // `btn_red.png` görsellerinden geliyordu; oyundaki BÜTÜN diğer
            // düğmeler ise koddan. İki stil aynı anda ekranda duruyordu ve
            // kullanıcıya ulaşan geri bildirim buydu.
            //
            // Görsel ölçüldü: 512×246, köşesi referanstan belirgin daha
            // köşeli ve parlaklığı sol üste doğru asimetrik olarak PİŞİRİLMİŞ
            // — dokuz dilimle esnetilince o parlaklık da esniyor, düğme
            // genişledikçe ışık kayıyor. Dosya duruyor; yalnız buradan
            // kullanılmıyor.
            var resume = MenuPage.PillButton("Resume", _pauseCard.transform,
                "Resume", MenuPage.Green, 46, null);
            UiKit.Place(resume, 0.237f, 0.330f, 0.763f, 0.487f);
            resume.onClick.AddListener(() => SetPaused(false));

            // Referansta "Yeniden Başla" YOK — kaldırıldı. Bölümü yeniden
            // başlatmak isteyen HUD'daki geri düğmesini kullanıyor.
            var quit = MenuPage.PillButton("Quit", _pauseCard.transform,
                "Quit", MenuPage.Red, 46, null);
            UiKit.Place(quit, 0.237f, 0.140f, 0.763f, 0.297f);
            quit.onClick.AddListener(AppRouter.GoHome);

            _pausePanel.gameObject.SetActive(false);
        }


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

            // Simge KOYU MOR — gerekçe ve ölçüm için bkz. PauseGlyph (S64).
            var glyph = UiKit.CreateIcon($"Icon_{index}", card, icon, PauseGlyph);
            glyph.raycastTarget = false;
            UiKit.Place(glyph, 0.151f, y0, 0.252f, y1);

            var text = UiKit.CreateTitle($"Label_{index}", card, label, 40, Ink, TitleOutline);
            text.alignment = TextAlignmentOptions.Left;
            UiKit.Place(text, 0.279f, y0, 0.526f, y1);

            // OYUNUN TEK ANAHTARI (8. tur).
            //
            // Burada anahtarın İKİNCİ bir uygulaması vardı: düz iki yarım,
            // beyaz "On" yazısı, kabartma yok. Ayarlar ekranındaki ise
            // referanstan ölçülmüştü. Aynı kontrol iki ekranda iki türlü
            // görünüyordu — düğmelerdeki hikâyenin aynısı, aynı çözüm.
            var view = MenuPage.Switch($"Track_{index}", card, 30);
            UiKit.Place(view.Root, 0.553f, y0, 0.863f, y1);

            _toggles[index] = view;

            // Her iki yarı da tıklanabilir: oyuncu istediği duruma DOĞRUDAN
            // basıyor. Tek düğmeli "değiştir" davranışı bir fazladan adım.
            AddToggleClick(view.OffFace, index, false, apply);
            AddToggleClick(view.OnFace, index, true, apply);
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
                _toggles[i]?.SetOn(values[i]);
        }

        void SetPaused(bool paused)
        {
            if (_session == null) return;
            if (paused && _session.State != GameState.Playing) return;

            _session.SetPaused(paused);
            _pausePanel.gameObject.SetActive(paused);
            SlideTimerAway(paused);

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

        /// <summary>Süre hapının ekran dışına çıkacağı mesafe (kanvas birimi).</summary>
        const float TimerHideRise = 210f;

        /// <summary>
        /// SÜRE GÖSTERGESİ DURAKLATINCA YUKARI KAYAR (7. tur, S61).
        ///
        /// Kullanıcı: "Oyun duraklatıldığında sayaç animasyonla arka planda
        /// yukarı doğru kaymalı. Oyun başlatıldığında hızla animasyonla
        /// aşağı geri gelmeli."
        ///
        /// DOĞRULAMA: Referansın duraklat karesinde (`…Levels 1-20
        /// Walkthrough.mp4` 00:04) yeniden başlat ve duraklat düğmeleri
        /// duruyor ama SÜRE HAPI YOK — yani gizlenmesi bir süs değil,
        /// referansın davranışı. Mantığı da var: duraklatılmış bir oyunda
        /// donmuş bir sayaç, oyuncuya "süre işliyor mu" diye sordurur.
        ///
        /// DERS (kaybolmak ile ÇIKIP GİTMEK aynı şey değil): `SetActive(false)`
        /// da hapı yok ederdi ama oyuncu onun nereye gittiğini bilmezdi;
        /// yukarı kayınca "geri geleceği" de anlaşılıyor. Dönüş bilerek daha
        /// KISA (0.16 sn): gitmek bir vedadır, dönmek oyunun yeniden
        /// başlaması — beklemek istemezsin.
        /// </summary>
        void SlideTimerAway(bool hidden)
        {
            if (_timerPill == null) return;

            float from = _timerPill.anchoredPosition.y;
            float to = hidden ? TimerHideRise : 0f;
            if (Mathf.Approximately(from, to)) return;

            GameKit.FX.Juice.Replace(_timerPill, hidden
                ? GameKit.FX.Juice.SlideY(_timerPill, from, to, 0.26f,
                    PrimeTween.Ease.InCubic)
                : GameKit.FX.Juice.SlideY(_timerPill, from, to, 0.16f,
                    PrimeTween.Ease.OutBack));
        }

        /// <summary>
        /// Ödülün arkasındaki ışın çelengi: merkezden dışa açılan ince kamalar.
        ///
        /// Kamalar tam daireye eşit aralıkla yayılıyor ve her biri kendi
        /// ucundan (pivot alt uçta) döndürülüyor; böylece merkezde birleşip
        /// dışa doğru açılıyorlar. Tek renk ve düşük alfa: amaç ışık hissi,
        /// desen değil.
        /// </summary>
        /// <summary>
        /// Jeton yığınının arkasındaki parlaklık — IŞIN DEĞİL, YUMUŞAK HALE.
        ///
        /// Kullanıcı (6. tur): "Goldların arkasındaki ışık yansıması çok kötü."
        ///
        /// REFERANS (Levels 1-20, 02:53, PERFECT kartı; tam boy kırpma
        /// incelendi): yığının arkasında ÇİZGİSEL hiçbir şey yok. Yalnız
        /// merkezden dışa açılan, panelin morundan bir tık açık, kenarları
        /// tamamen yumuşak bir hale var; çevresinde de küçük yıldız
        /// kıvılcımları.
        ///
        /// İki tur boyunca ışın denendi: önce 16 ışınlı tam daire (ekranda
        /// gri bir çark), sonra 9 ışınlı yelpaze. İkisi de yanlıştı çünkü
        /// referansta IŞIN YOK.
        ///
        /// DERS (ölçmek, saymaktan ibaret değil — NE OLMADIĞINI da görmek):
        /// "Işınlar kaç tane, hangi açıda" diye ölçtüm; oysa sorulacak ilk
        /// soru "ışın var mı" idi. Bir ayrıntıyı doğru ölçmek, o ayrıntının
        /// var olduğunu varsaymayı meşrulaştırmıyor.
        /// </summary>
        /// <summary>
        /// Jeton yığınının arkasındaki hale — SICAK ALTIN, soğuk beyaz değil.
        ///
        /// ÖLÇÜM (referans PERFECT kartı, yığının çevresindeki hale): hale
        /// kartın morundan (65,49,192) sarıya doğru kayıyor, en parlak yeri
        /// (150,120,235) civarı ve çevresinde üç-dört küçük BEYAZ dört uçlu
        /// parıltı var. Bizimki (0.90, 0.88, 1) ile mavi-beyazdı; altın bir
        /// yığının arkasında soğuk bir ışık, jetonları "buzlu" gösteriyordu.
        ///
        /// DERS (ışığın rengi, aydınlattığı şeyden gelir): Bir ödül halesi
        /// ödülün rengini taşımalı; nötr beyaz, altını gümüşe çeviriyor.
        /// </summary>
        /// <summary>
        /// Ödül yığınının ışın çelengi ve çevresindeki minik yıldızlar.
        ///
        /// Yıldızların yerleri UYDURULMADI: referansın 330-362. kareleri
        /// (0,5 saniye) tarandı, 4-200 piksel² arası beyaz lekelerin ağırlık
        /// merkezleri bu dikdörtgenin oranına çevrildi ve birbirine çok yakın
        /// olanlar birleştirildi. Üçüncü sayı yıldızın boyu — alanının
        /// karekökünden geliyor, yani referansta büyük olan burada da büyük.
        /// </summary>
        static readonly Vector3[] SparkleSpots =
        {
            new Vector3(-0.102f, 0.064f, 1.18f),
            new Vector3(-0.068f, 0.694f, 1.30f),
            new Vector3(-0.060f, 0.469f, 1.22f),
            new Vector3(-0.034f, 0.652f, 1.30f),
            new Vector3(-0.005f, 0.615f, 1.22f),
            new Vector3(-0.001f, 0.246f, 1.22f),
            new Vector3( 0.097f, 0.463f, 1.14f),
            new Vector3( 0.126f, 0.504f, 1.18f),
            new Vector3( 0.203f, 0.862f, 1.26f),
            new Vector3( 0.219f, 0.810f, 1.14f),
            new Vector3( 0.255f, 0.692f, 1.38f),
            new Vector3( 0.279f, 0.760f, 1.52f),
            new Vector3( 0.279f, 0.639f, 1.14f),
            new Vector3( 0.326f, 0.760f, 1.34f),
            new Vector3( 0.374f, 0.791f, 1.14f),
            new Vector3( 0.382f, 0.948f, 1.58f),
            new Vector3( 0.388f, 0.905f, 1.22f),
            new Vector3( 0.466f, 0.880f, 1.14f),
            new Vector3( 0.521f, 0.738f, 1.52f),
            new Vector3( 0.529f, 0.927f, 1.22f),
            new Vector3( 0.536f, 0.995f, 1.58f),
            new Vector3( 0.537f, 0.882f, 1.41f),
            new Vector3( 0.552f, 0.839f, 1.52f),
            new Vector3( 0.588f, 0.882f, 1.38f),
            new Vector3( 0.601f, 0.947f, 1.38f),
            new Vector3( 0.680f, 0.890f, 1.30f),
            new Vector3( 0.692f, 0.739f, 1.76f),
            new Vector3( 0.733f, 0.619f, 1.22f),
            new Vector3( 0.735f, 0.666f, 2.00f),
            new Vector3( 0.794f, 0.710f, 1.45f),
            new Vector3( 0.811f, 0.760f, 1.45f),
            new Vector3( 0.839f, 0.661f, 1.48f),
            new Vector3( 0.841f, 0.827f, 1.45f),
            new Vector3( 0.857f, 0.870f, 1.55f),
            new Vector3( 0.933f, 0.258f, 2.00f),
            new Vector3( 0.940f, 0.185f, 1.38f),
            new Vector3( 0.941f, 0.096f, 1.14f),
            new Vector3( 0.999f, 0.154f, 1.45f),
            new Vector3( 1.017f, 0.114f, 1.38f),
            new Vector3( 1.056f, 0.578f, 1.18f),
        };

        /// <summary>
        /// Bir yıldızın taban yarı-çapı (kutu oranı olarak).
        ///
        /// ÖLÇÜM: referansta yıldız lekelerinin alan medyanı 8 piksel², yani
        /// çekirdeği ~3 piksel; uçlarıyla birlikte 8-10 piksel. Kutu 290
        /// piksel genişliğinde olduğuna göre yarı-çap ~0,016. İlk yazdığım
        /// 0,030 iki katıydı: az sayıda İRİ yıldız, referansın çok sayıda
        /// MİNİK yıldızından bambaşka bir şey.
        /// </summary>
        const float SparkleHalf = 0.017f;

        static void BuildGlow(RectTransform parent)
        {
            // IŞIN ÇELENGİ GERİ GELDİ (8. tur).
            //
            // Kodda "ışınlar tek tek döndürülmüş ince dikdörtgenler" diye
            // anlatılıyordu ama ekranda yalnız yumuşak bir yuvarlak hale
            // vardı — açıklama kalmış, ışınlar bir turda düşmüştü.
            // Referansta jetonların arkasından çıkan düz ışınlar açıkça
            // görülüyor ve yığını "parlayan bir ödül" yapan şey onlar.
            //
            // `UiSprites.Burst` tam bunu üretiyor: 12 ışın, merkezden dışa
            // sönerek. Ayrı bir görsel gerekmiyor.
            var rays = UiKit.CreateIcon("Rays", parent, UiSprites.Burst,
                new Color(1f, 1f, 1f, 0.34f));
            rays.raycastTarget = false;
            UiKit.Place(rays, -0.52f, -0.46f, 1.52f, 1.52f);

            var glow = UiKit.CreateIcon("Glow", parent, UiSprites.Radial,
                new Color(1f, 0.92f, 0.62f, 0.55f));
            glow.raycastTarget = false;
            UiKit.Place(glow, -0.42f, -0.34f, 1.42f, 1.40f);

            var stars = new Graphic[SparkleSpots.Length];
            var scales = new float[SparkleSpots.Length];
            for (int i = 0; i < SparkleSpots.Length; i++)
            {
                var spot = SparkleSpots[i];
                var star = UiKit.CreateIcon("Sparkle" + i, parent, MenuSprites.Sparkle,
                    new Color(1f, 1f, 1f, 0f));
                star.raycastTarget = false;
                UiKit.Place(star, spot.x - SparkleHalf, spot.y - SparkleHalf,
                                  spot.x + SparkleHalf, spot.y + SparkleHalf);
                stars[i] = star;
                scales[i] = spot.z;
            }

            var field = parent.gameObject.GetComponent<SparkleField>();
            if (field == null) field = parent.gameObject.AddComponent<SparkleField>();
            field.Adopt(stars, scales);
        }

        void BuildResultPanel(Transform root)
        {
            _resultPanel = UiKit.CreateRect("Result", root);
            UiKit.Place(_resultPanel, 0f, 0f, 1f, 1f);

            // Perde: altındaki tahtaya dokunmayı da yutar.
            var scrim = UiKit.CreatePanel("Scrim", _resultPanel, new Color(0.05f, 0.03f, 0.14f, 0.84f));
            scrim.raycastTarget = true;

            // KART ARTIK PROSEDÜREL — HAZIR GÖRSEL ÇİFT ÇERÇEVE ÜRETİYORDU.
            //
            // Kullanıcı (7. tur devamı): "perfect paneli daha iyi hale
            // getirilebilir."
            //
            // ÖLÇÜM (`…Levels 1-20 Walkthrough.mp4` 11:31, kartın ortasından
            // dikey kesit, 592×1280):
            //   kartın dış kenarı → 18 piksel KOYU kenar (31,16,95)
            //   ardından 15 piksellik yumuşak geçiş
            //   sonra düz yüz (65,49,192), kartın sonuna kadar TEK PARÇA
            // Yani referansın kartı: koyu bir kenar ve içinde düz bir yüz.
            //
            // Bizde `panel_card` sprite'ı kullanılıyordu ve o görselin
            // İÇİNDE basılı ikinci bir çerçeve var: ekranda kart, "çerçeve
            // içinde çerçeve" gibi okunuyor ve 9-dilim dikişi kartın
            // ortasında yatay bir iz bırakıyordu. Boyama o baskıyı
            // kaldıramaz — baskılı gölge bu projede altı kez sorun çıkardı.
            //
            // DERS (hazır görsel, İÇİNDEKİ kararları da getirir): Bir sprite
            // yalnız renk ve şekil değil, üzerine çizilmiş her ayrıntıyı da
            // dayatır. Kenar kalınlığını, iç çizgiyi ve köşe yarıçapını
            // ayarlayabilmek gerekiyorsa yüzey kodla kurulmalı.
            _resultCard = UiKit.CreateRoundedPanel("Card", _resultPanel, CardEdge,
                GameKit.UI.UiCornerFit.HouseShare, CardCornerRadius);
            UiKit.Place(_resultCard, WinCardX0, WinCardY0, WinCardX1, WinCardY1);

            // Kenar profili: dört bant, sonra yüz. Her katmanın köşe yarıçapı
            // içeri girdiği kadar küçülüyor — eş merkezli köşe kuralı (P56).
            // Aksi hâlde bantlar köşelerde birbirine paralel kalmaz, kenar
            // kalınlığı köşede incelir.
            //
            // KOYU KENAR EKRANDA HİÇ GÖRÜNMÜYORMUŞ (8. turda bulundu).
            //
            // Kart `CardRim` ile kuruluyor, çocuğu "Face" ise `CardPurple`
            // ile. Ama `RefreshResult` her açılışta `_resultCard.color`u
            // ZORLUK RENGİYLE eziyor ve normal bölümlerde o renk tam olarak
            // `CardPurple`. Yani kenar da yüz de aynı mor oluyor, 7. turda
            // ölçülüp yazılan 18 piksellik koyu kenar bir kez bile
            // çizilmiyordu.
            //
            // DERS (ölçüm doğru olabilir ama EKRANA ULAŞMAYABİLİR): Kenarın
            // rengi kaynakta duruyordu; kimse çalışma anında onu kimin
            // yazdığını sormadı. Bir sabiti tanımlamak, onun görüneceğini
            // garanti etmez — sabitin ekrandaki karşılığı ölçülmeli.
            //
            // Çözüm: zorluk rengi artık YÜZE gidiyor, kenar bantları da o
            // renkten TÜRETİLİYOR (bkz. TintCard). Mor/şarap/kızıl üç kart
            // da aynı kenar profilini koruyor.
            float inset = 0f;
            _cardGroove = Band(CardGroove,    CardEdgeBand,   "Groove");
            _cardInner  = Band(CardEdge,      CardGrooveBand, "Inner");
            _cardLine   = Band(CardInnerLine, CardInnerBand,  "Line");
            _cardFace   = Band(CardPurple,    CardLineBand,   "Face");
            var cardFace = _cardFace;

            Image Band(Color color, float step, string name)
            {
                inset += step;
                var band = UiKit.CreateRoundedPanel(name, _resultCard.transform,
                    color, GameKit.UI.UiCornerFit.HouseShare, CardCornerRadius - inset);
                UiKit.Place(band, 0f, 0f, 1f, 1f, padding: inset);
                band.raycastTarget = false;
                return band;
            }

            // "PERFECT!" kartın DIŞINDA, üst kenarına binerek duruyor. Kartın
            // içine alınırsa başlık kutunun bir satırı olur; referansta kartı
            // taşıyan bir tabela gibi davranıyor.
            // PUNTOLAR ÖLÇÜLDÜ (6. tur; kullanıcı: "PERFECT yazısı özensiz,
            // kötü; yeri ve boyutu referanstan bakıp ayarlanmalı").
            //
            // ÖLÇÜM (Levels 1-20, 02:53; harf yükseklikleri ekran
            // yüksekliğine oranla):
            //   "PERFECT!"  %5,70  →  1920'de 109 piksel
            //   "Level 8"   %3,12  →  60 piksel
            //   "20"        %2,97  →  57 piksel
            //   "Continue"  %2,66  →  51 piksel
            // Bizim puntolarımız (88 / 58 / 46 / 44) bunların kabaca
            // YARISINI veriyordu; başlık ekranda cılız kalıyordu.
            _resultTitle = UiKit.CreateTitle("Title", _resultPanel, "", TitlePoint,
                TitleGold, TitleOutline);
            UiKit.Place(_resultTitle, 0.10f, 0.772f, 0.90f, 0.856f);
            _resultTitle.textWrappingMode = TextWrappingModes.NoWrap;

            // BAŞLIK REFERANSA GÖRE YENİDEN DÖKÜLDÜ (8. tur).
            //
            // Yan yana konunca (ikisi de aynı genişliğe ölçeklenerek) fark
            // dört başlıkta toplanıyordu:
            //
            //   1. Referansın harfleri BİRBİRİNE DEĞİYOR. Ölçüldü: harf
            //      araları 2-5 piksel, harf genişliği 34-49 piksel. Bizde
            //      araların her biri harfin yarısı kadardı.
            //   2. Referansın harfleri DAHA UZUN. Aynı kelime genişliğinde
            //      (ekranın %56,2'si) referansın büyük harf yüksekliği
            //      ekranın %8,05'i, bizimki %4,7'siydi.
            //   3. Gövde kalın; bizim kontur SDF'i ince bırakıyordu.
            //   4. Altın DÜZ DEĞİL: tepede açık sarı (255,214,90), dipte
            //      koyu turuncu (233,140,10).
            //
            // 1 ve 2 aynı sebebin iki yüzü: referansın yazı tipi Baloo2'den
            // belirgin biçimde DAR. Aynı yüksekliğe getirdiğimizde kelime
            // taşıyor. Çözüm harf aralığını kısmak ve kalan farkı yatay
            // ölçekle kapatmak — sıkıştırılmış bir başlık, dizgide de
            // "condensed" diye ayrı bir kesim olarak vardır.
            //
            // DERS (yazı tipi bir ÖLÇÜ setidir, yalnız bir biçim değil):
            // "Aynı puntoyu ver" demek aynı boyu vermiyor; iki tipin büyük
            // harf yüksekliği de, genişlik/yükseklik oranı da farklı.
            // Karşılaştırma her zaman EKRANDAKİ piksel üstünden yapılmalı.
            UiKit.SetOutline(_resultTitle, TitleOutline, TitleOutlineWidth);
            // OTOMATİK KÜÇÜLTME KAPALI: punto burada BİLEREK kutudan büyük.
            // `UiTextFit` taşan etiketleri küçültüyor (M46); bu başlık ise
            // kutusunu kasten aşıyor ve yatay ölçekle geri sığıyor. Fit
            // açık kalsaydı puntoyu geri indirir, ölçüm boşa giderdi.
            UiKit.NoFit(_resultTitle);
            _resultTitle.characterSpacing = TitleTracking;
            _resultTitle.rectTransform.localScale = new Vector3(TitleCondense, 1f, 1f);

            // Rengin kendisi BEYAZ: altın artık geçişten geliyor ve ikisi
            // TMP'de çarpılıyor (bkz. RefreshResult'taki not).
            _resultTitle.color = Color.white;
            _resultTitle.enableVertexGradient = true;
            _resultTitle.colorGradient = new VertexGradient(
                TitleGoldTop, TitleGoldTop, TitleGoldBottom, TitleGoldBottom);

            // Gövdeyi şişirmek: SDF'te `_FaceDilate` harfin kenarını dışarı
            // itiyor, yani aynı yazı tipini kalınlaştırıyor. Konturu
            // kalınlaştırmak aynı şey değil — o harfi değil çevresini
            // büyütür ve harf ARASI boşluğu yer.
            var titleMaterial = _resultTitle.fontMaterial;
            titleMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, TitleDilate);

            // Kapatma çarpısı: kartın sağ üst köşesine biner. Ana ekrana dönüş
            // artık bu — referansta ayrı bir "Ana Ekran" düğmesi yok.
            // btn_red YUVARLAK DEĞİL, yuvarlak köşeli bir dikdörtgen. Referanstaki
            // kapatma düğmesi tam daire; o yüzden zemini çizimden alıyoruz.
            // KAPATMA DÜĞMESİ İKİ HALKALI (8. tur).
            //
            // ÖLÇÜM (ham kare, y=292 yatay kesiti): düğme x 513-566, yani
            // çapı 53 piksel = ekran genişliğinin %8,95'i. Kesitte iki ayrı
            // kırmızı var: dıştaki koyu (212,0,4), içteki parlak (255,47,52).
            // Çarpı kırık beyaz (249,231,211), siyah değil.
            //
            // Bizde tek düz kırmızı bir daire ve KOYU MOR bir çarpı vardı;
            // koyu zeminde çarpı okunmuyordu ve düğme kartın köşesine
            // "yapıştırılmış" gibi duruyordu. Koyu halka, düğmeye kartın
            // üstünde durduğu hissini veren şey.
            _closeButton = UiKit.CreateIconButton("Close", _resultPanel,
                UiSprites.Circle, CloseRim);
            UiKit.Place(_closeButton, 0.867f, 0.752f, 0.956f, 0.793f);
            _closeButton.onClick.AddListener(AppRouter.GoHome);

            var closeFace = UiKit.CreateIcon("Face", _closeButton.transform,
                UiSprites.Circle, CloseRed);
            closeFace.raycastTarget = false;
            UiKit.Place(closeFace, 0.10f, 0.10f, 0.90f, 0.90f);

            // Çarpı, kırmızı yuvarlağın ÜSTÜNE ayrı bir görsel olarak biniyor.
            var crossMark = UiKit.CreateIcon("Mark", _closeButton.transform,
                UiSprites.Cross, CloseInk);
            crossMark.raycastTarget = false;
            UiKit.Place(crossMark, 0.27f, 0.27f, 0.73f, 0.73f);

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

            // BÖLÜM ADININ PLAKASI KALDIRILDI — REFERANSTA YOK (8. tur).
            //
            // 7. turda "Level 20 açık mor bir plakanın içinde" diye ölçülüp
            // eklenmişti. O ölçüm YANLIŞTI ve nasıl yanlış olduğu öğretici:
            // yazının 12 piksel altındaki satır taranmış, x 177-402 arası
            // KESİNTİSİZ koyu çıkmış ve "demek plaka var" denmişti.
            //
            // Bu turda aynı kare yoğun tarandı — yazının üstünde (y=380),
            // ortasında ve altında (y=446) aynı bant BOŞLUKLU çıkıyor:
            //
            //   y=380  ...###.#############.###.....
            //   y=441  ...###########################   <- tek dolu satır
            //   y=446  ...####...#.###############..
            //
            // Bir plaka HER satırda kesintisiz olur. Boşluklar harflerin
            // arasına denk geliyor; yani görülen şey plaka değil, yazının
            // kalın MOR KONTURU. y=441 taban çizgisinin hemen altı olduğu
            // için orada bütün harflerin konturu birleşiyor.
            //
            // Kartın yüzü de bunu doğruluyor: yazının çevresindeki her
            // piksel (65,49,192), yani kartın kendi rengi. Plaka olsaydı
            // orada başka bir renk okunurdu.
            //
            // DERS (tek satır delil değildir): Bir yapıyı doğrulamak için
            // en az iki-üç kesit gerekiyor. Tek kesit, harflerin birleştiği
            // yere denk gelirse dümdüz bir dikdörtgen gibi okunur.
            _perfectBadge = UiKit.CreateTitle("LevelName", _resultCard.transform, "", 84,
                Ink, TitleOutline);
            UiKit.Place(_perfectBadge, 0.290f, 0.740f, 0.708f, 0.845f);

            // Konturu KALIN: referansta yazıyı çevreleyen mor şerit yaklaşık
            // 12 piksel — 7. turda bu şerit yanlışlıkla "plaka" sanılmıştı
            // (bkz. yukarısı). Kalın kontur, açık mor kartın üstünde beyaz
            // yazıyı ayakta tutan şey; incesiyle yazı zemine gömülüyor.
            UiKit.SetOutline(_perfectBadge, TitleOutline, 0.30f);

            // Jeton yığını: tek bir "coin_pile" görselimiz yok, bu yüzden
            // icon_coin'lerden kuruyoruz. Gerçek yığın görseli gelince burası
            // tek bir Image'a iner — düzen değişmez.
            _rewardArt = UiKit.CreateRect("RewardArt", _resultCard.transform);
            UiKit.Place(_rewardArt, 0.18f, 0.440f, 0.82f, 0.740f);

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
            BuildGlow(_rewardArt);
            BuildCoinPile(_rewardArt);

            // Ödül sayısı koyu kapsülün içinde — açık kart üstünde altın rakam
            // okunmuyor, koyu zemin onu geri getiriyor.
            //
            // HAZIR GÖRSEL BIRAKILDI (7. tur, R59): Plaka `panel_dark`
            // sprite'ının BadgeDark ile boyanmış hâliydi. Boyama çarpma
            // olduğu için koyu bir sprite'ı koyu bir renkle çarpmak
            // SİYAH veriyor; ekranda ölçüldüğünde plaka neredeyse siyah bir
            // elipsti. Referansta ise koyu MOR ve YUVARLAK KÖŞELİ bir
            // dikdörtgen: yüz (34,21,100), kenar (56,41,120).
            //
            // DERS (bu projede altıncı tekrar): "Bir yüzeyi BOYAYACAKSAN
            // baskılı gölgesi olmayanı, açık renklisini kullan." Prosedürel
            // yuvarlak panel rengi birebir veriyor ve köşe yarıçapını da
            // kutunun boyundan hesaplıyor.
            _rewardBadge = UiKit.CreateRoundedPanel("RewardBadge", _resultCard.transform,
                RewardPlateRim);
            UiKit.Place(_rewardBadge, 0.358f, 0.333f, 0.646f, 0.436f);
            _rewardBadge.raycastTarget = false;

            var rewardFace = UiKit.CreateRoundedPanel("Face", _rewardBadge.transform,
                RewardPlateFace);
            UiKit.Place(rewardFace, 0f, 0f, 1f, 1f, padding: 6f);
            rewardFace.raycastTarget = false;

            _resultReward = UiKit.CreateTitle("Reward", _rewardBadge.transform, "", 76,
                CoinGold, TitleOutline);
            UiKit.Place(_resultReward, 0.02f, 0.04f, 0.98f, 0.96f);

            // Reklam düğmesi: kazanınca "ödülü ikiye katla", kaybedince
            // "+30 sn ile devam et". İkisi de aynı yerde durur ki oyuncu
            // nereye bakacağını öğrensin.
            // Yeşil "Continue" kartın tek ve büyük eylemi. Hazır btn_green
            // sprite'ı var; boyamaya gerek yok.
            _resultPrimary = CreateGreenButton("Primary", _resultCard.transform);
            UiKit.Place(_resultPrimary, 0.221f, 0.114f, 0.787f, 0.275f);
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
            // Kalp başlığın ALTINDA. Başlık 8. turda büyüyünce kalp
            // harflerin üstüne bindi ve "FAILED"in ortasını kapattı; iki
            // öğe aynı kutuya sığmıyordu.
            UiKit.Place(_failHeart, 0.430f, 0.700f, 0.573f, 0.752f);

            // Zorluk etiketi bölüm numarasının ÜSTÜNDE, küçük ve sade.
            _failDifficulty = UiKit.CreateTitle("Difficulty", _resultCard.transform, "", 30,
                Ink, TitleOutline);
            UiKit.Place(_failDifficulty, 0.20f, 0.884f, 0.80f, 0.952f);

            // Kaçırılan ödülün üstüne binen kırmızı çarpı — rozetin SAĞ ucunda.
            _failDenied = UiKit.CreateIcon("Denied", _rewardBadge.transform, UiSprites.Cross,
                new Color(0.93f, 0.16f, 0.17f));
            UiKit.Place(_failDenied, 0.74f, -0.08f, 1.20f, 1.08f);

            // "Rewards x3" etiketi yeşil düğmenin üst kenarına oturur.
            // ÖLÇÜM (`Game over .mp4` 18. saniye): rozet TURUNCU bir hap,
            // yazısı beyaz — koyu lacivert değil. Koyu rozet yeşil düğmenin
            // üstünde bir "ikinci gölge" gibi duruyordu.
            _rewardsTag = UiKit.CreateSlicedPanel("RewardsTag", _resultCard.transform,
                UiSkin.Get(Art.PanelDark), TagOrange);
            UiKit.Place(_rewardsTag, 0.370f, 0.260f, 0.641f, 0.312f);
            var tagLabel = UiKit.CreateTitle("Label", _rewardsTag.transform, "Rewards x3", 24,
                Color.white, TagOrangeDark);
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
        /// <summary>
        /// Jeton yığını — HAZIR GÖRSEL.
        ///
        /// Kullanıcı (6. tur): "Gold görselleri kötü, daha iyi hazır goldlar
        /// vardı."
        ///
        /// Haklıydı: `Assets/_Project/Art/UI/coin_pile_1..5.png` zaten
        /// projede duruyor ve mağazada kullanılıyor. Ödül kartı ise yığını
        /// ON ADET tek jeton görselini elle istifleyerek kuruyordu — üç
        /// sütun, iki devrik jeton. O yaklaşım 4. turda "yığın görseli
        /// olmadığı için" seçilmişti; görsel o zaman da vardı, yalnız başka
        /// bir klasörde arandı.
        ///
        /// DERS (elde olanı aramak, üretmekten ucuzdur): "Bu varlık yok"
        /// varsayımı bir kez kurulunca üstüne kod yazılıyor ve varsayım
        /// kodun içinde donuyor. Yeni bir görsel eklemeden önce projedeki
        /// dosya listesine bakmak beş saniye sürüyor.
        ///
        /// Ödül miktarına göre yığın büyüyor: 20 jeton küçük bir öbek,
        /// 200 jeton bir tepe. Referansta da kutu büyüklüğü miktarla
        /// değişiyor.
        /// </summary>
        void BuildCoinPile(RectTransform holder)
        {
            _coinPile = UiKit.CreateIcon("Pile", holder, PileFor(20));
            _coinPile.raycastTarget = false;
            _coinPile.preserveAspect = true;
            UiKit.Place(_coinPile, 0f, 0f, 1f, 1f);
        }

        Image _coinPile;

        /// <summary>Miktara göre yığın görseli; yoksa bir alt kademeye düşer.</summary>
        static Sprite PileFor(int reward)
        {
            // ÖLÇÜM (referans PERFECT kartı, 20 jeton): yığın 262x170 piksel,
            // en-boy 1,54 — birkaç istif ve önünde dağınık jetonlar. Bizim
            // `coin_pile_1` tek bir dar istif (en-boy 1,14) ve referansa en
            // uzak olan; küçük ödüller için bile 2. kademeden başlıyoruz.
            int tier = reward >= 150 ? 5
                     : reward >= 80  ? 4
                     : reward >= 40  ? 3 : 2;
            for (int i = tier; i >= 1; i--)
            {
                var sprite = UiSkin.Get(Art.CoinPile(i));
                if (sprite != null) return sprite;
            }
            return UiSkin.Get(Art.Coin);
        }

        // KART KONFETİSİ KALDIRILDI (8. tur).
        //
        // Referansın PERFECT kartında konfeti YOK: 313-362. karelerin
        // hiçbirinde kartın üstünde uçuşan parça bulunmuyor. Konfeti bir
        // önceki adımda, siyah kutlama ekranında zaten var.
        //
        // DERS (zıtlık da bir tasarım aracıdır): Gürültülü bir kutlamadan
        // sonra SAKİN bir kart, ödülü öne çıkarıyor. İkisi de hareketliyse
        // göz ikinci ekranda dinlenecek yer bulamıyor ve ödüle bakmıyor.
        // Buradaki canlılığı artık yığının çarparak oturması ve yıldızların
        // sönüp yanması taşıyor (bkz. SparkleField).

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
            // DAKİKA İKİ HANE (7. tur, U67: "tasarım orijinaliyle birebir
            // aynı olacak"). Referansta sayaç "01:52" / "01:14" biçiminde;
            // bizde "1:14" idi ve rakam sayısı değiştikçe hap içindeki yazı
            // yerinden oynuyordu. Sabit genişlik, sayacın "kıpırdamadan"
            // saymasını da sağlıyor.
            int minutes = total / 60;
            _timerLabel.text = _scratch.Clear()
                .Append(minutes / 10).Append(minutes % 10).Append(':')
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


        /// <summary>
        /// Sonuç kartının YERLEŞİMİ — tek yerde.
        ///
        /// Ayrı metoda çıkarıldı ki editör önizlemesi (bkz.
        /// <c>CreateResultPreview</c>) gerçek oyun oturumu olmadan da AYNI
        /// sayıları uygulayabilsin. Aksi hâlde önizleme, yerleşimin bir
        /// KOPYASINI doğrulardı — yani hiçbir şeyi.
        ///
        /// DERS (doğrulanamayan kod, üçüncü kez bozulur): Bu panel üç turdur
        /// düzeltiliyor ve her seferinde yalnız APK'de görülebiliyordu.
        /// Ölçümü koda yazmak yetmiyor; kodun ürettiği şeyi görebilmek de
        /// gerekiyor.
        /// </summary>
#if UNITY_EDITOR
        /// <summary>
        /// EDİTÖR ÖNİZLEMESİ: oyun oturumu olmadan yalnız sonuç kartını kurar.
        ///
        /// Neden var: Sonuç kartı üç turdur düzeltiliyor ve her seferinde
        /// ancak APK alınıp oynanınca görülebiliyordu. Ölçümü koda yazmak
        /// yetmiyor — kodun ÜRETTİĞİ şeyi de görebilmek gerekiyor. Bu giriş
        /// noktası kartı editörde kurup ekran görüntüsü almayı mümkün kılıyor.
        ///
        /// Yalnızca `UNITY_EDITOR` altında derleniyor; oyuna hiçbir maliyeti
        /// yok ve çalışma anındaki akışa dokunmuyor.
        /// </summary>
        public static GameplayScreen CreateResultPreview(
            Transform parent, bool won, int reward, string levelName, string title)
        {
            // KANVASI ÇAĞIRAN VERİR. `UiKit.CreateCanvas` içeride
            // `DontDestroyOnLoad` çağıran bir EventSystem kuruyor; o da
            // yalnız oynatma modunda geçerli. Önizleme edit modunda
            // çalıştığı için kanvası dışarıdan almak zorunda.
            // Kök DÜZ Transform DEĞİL, RectTransform olmalı: arayüz
            // çocukları çapalarını üst dikdörtgene göre çözüyor. Düz bir
            // Transform'un altında yerleşim sessizce çöküyor — ilk denemede
            // kartın tamamı görünmez oldu, yalnız başlık ekranın ortasında
            // asılı kaldı.
            var host = UiKit.CreateRect("ResultPreview", parent);
            UiKit.Place(host, 0f, 0f, 1f, 1f);
            var screen = host.gameObject.AddComponent<GameplayScreen>();

            screen.BuildResultPanel(host);
            screen._resultPanel.gameObject.SetActive(true);

            screen._resultTitle.text = title;
            screen._perfectBadge.text = levelName;
            screen._resultReward.text = reward.ToString();
            screen._resultPrimaryLabel.text = won ? "Continue" : "Try Again";
            screen._resultDifficulty.gameObject.SetActive(false);
            screen._failHeart.gameObject.SetActive(!won);
            screen._rewardsTag.gameObject.SetActive(!won);
            screen._failDenied.gameObject.SetActive(!won);
            screen._failDifficulty.gameObject.SetActive(false);
            screen._adButton.gameObject.SetActive(false);
            screen._rewardArt.gameObject.SetActive(true);
            screen._rewardBadge.gameObject.SetActive(true);

            if (screen._coinPile != null)
            {
                var sprite = PileFor(reward);
                if (sprite != null) screen._coinPile.sprite = sprite;
            }
            screen.TintCard(won ? CardPurple : CardFailViolet);

            screen.ApplyResultLayout(won);
            return screen;
        }

        /// <summary>
        /// EDİTÖR ÖNİZLEMESİ: duraklat panelini oturum olmadan kurar.
        ///
        /// Sonuç kartındaki gerekçenin aynısı (7. tur, S62-S64): panelin
        /// düğme yerleşimi, çarpısı ve simge renkleri yalnız oyunu durdurup
        /// APK'de bakınca görülebiliyordu. Ayar satırlarının tıklama
        /// dinleyicileri oturum istediği için burada `_session` null
        /// kalıyor — bu yüzden anahtarlara BASILAMAZ; önizleme yalnız
        /// GÖRÜNÜM içindir.
        /// </summary>
        public static GameplayScreen CreatePausePreview(Transform parent)
        {
            var host = UiKit.CreateRect("PausePreview", parent);
            UiKit.Place(host, 0f, 0f, 1f, 1f);
            var screen = host.gameObject.AddComponent<GameplayScreen>();

            screen.BuildPausePanel(host);
            screen._pausePanel.gameObject.SetActive(true);
            return screen;
        }

        /// <summary>
        /// EDİTÖR ÖNİZLEMESİ: yalnız üst şerit (jeton · süre · bölüm).
        /// 7. turda U67/U68 için eklendi — süre hapının ölçüsü ve saat
        /// simgesi ancak oyun oynanırken görülebiliyordu.
        /// </summary>
        public static GameplayScreen CreateHudPreview(Transform parent,
            string coins = "1490", string timer = "1:14", string level = "Level 20")
        {
            var host = UiKit.CreateRect("HudPreview", parent);
            UiKit.Place(host, 0f, 0f, 1f, 1f);
            var screen = host.gameObject.AddComponent<GameplayScreen>();

            screen.BuildTopBar(host);
            screen._coinLabel.text = coins;
            screen._timerLabel.text = timer;
            screen._levelLabel.text = level;
            return screen;
        }
#endif

        void ApplyResultLayout(bool won)
        {
            UiKit.Place(_resultCard, won ? WinCardX0 : LoseCardX0, won ? WinCardY0 : LoseCardY0,
                                     won ? WinCardX1 : LoseCardX1, won ? WinCardY1 : LoseCardY1);
            // BAŞLIK KARTIN ÜST KENARINA BİNER (7. tur, R59). Referansta
            // "PERFECT!" x 100-492 (%16,9-%83,1), y 205-285 → alttan
            // %77,7-%84,0; kartın tepesi %79,1. Yani başlığın ALT yarısı
            // kartın üstünde duruyor — onu taşıyan bir tabela gibi.
            // BAŞLIK KARTIN ÜSTÜNE BİNİYOR — SANILDIĞINDAN ÇOK DAHA FAZLA.
            //
            // ÖLÇÜM (ham 59,47 fps kare): altın harfler x 129-462 (ekranın
            // %56,2'si, tam ortalı), y 224-327. Kartın üst kenarı 252'de.
            // Yani harflerin ALT DÖRTTE ÜÇÜ kartın üstünde duruyor.
            //
            // Bizde başlık kartın üst kenarına ancak DEĞİYORDU; aradaki fark
            // ekranın %5,9'u. Değen bir başlık "kartın üstündeki yazı" gibi
            // okunuyor, binen bir başlık kartı taşıyan bir TABELA gibi.
            UiKit.Place(_resultTitle, won ? 0.09f : 0.10f, won ? 0.745f : 0.756f,
                                      won ? 0.91f : 0.90f, won ? 0.836f : 0.836f);

            // PUNTO DURUMA GÖRE. Kazanma başlığı kartın üstüne binen bir
            // tabela; kaybetme başlığı kartın ÜSTÜNDE, kırık kalple birlikte
            // ayrı bir grup. İkisine aynı puntoyu vermek "FAILED"i kartın
            // yarısı kadar büyük yapıyordu.
            _resultTitle.fontSize = won ? TitlePoint : LoseTitlePoint;

            // Kapatma çarpısı KARTIN köşesine biner — kaybetme kartı daha
            // alçak olduğu için ayrı bir y gerekiyor. Eskiden ikisinde de
            // aynı yükseklikteydi ve kaybetme ekranında boşlukta duruyordu.
            UiKit.Place(_closeButton, won ? 0.867f : 0.862f, won ? 0.752f : 0.658f,
                                      won ? 0.956f : 0.967f, won ? 0.793f : 0.699f);

            // Kart İÇİ yerleşim (kart-göreli oranlar); kaybetme düzeni
            // `Game over .mp4` 18. saniyeden ölçüldü.
            // Kaybetme oranları yukarıdaki ölçümden kart-göreliye çevrildi
            // (kart x 0.049..0.951 → genişlik 0.902; y 0.254..0.700 → 0.446).
            // Bölüm adı: plakasız, doğrudan kartın üstünde (bkz.
            // BuildResultPanel — plakanın neden kaldırıldığı orada).
            UiKit.Place(_perfectBadge, won ? 0.290f : 0.212f, won ? 0.740f : 0.788f,
                                       won ? 0.708f : 0.788f, won ? 0.845f : 0.880f);
            UiKit.Place(_rewardArt, won ? 0.234f : 0.278f, won ? 0.438f : 0.513f,
                                    won ? 0.775f : 0.744f, won ? 0.700f : 0.794f);
            UiKit.Place(_rewardBadge, won ? 0.358f : 0.327f, won ? 0.333f : 0.377f,
                                      won ? 0.646f : 0.734f, won ? 0.436f : 0.513f);
            UiKit.Place(_resultPrimary, won ? 0.221f : 0.241f, won ? 0.114f : 0.121f,
                                        won ? 0.787f : 0.759f, won ? 0.275f : 0.303f);
            // "Ödüller x3" rozeti yeşil düğmenin hemen ÜSTÜNDE.
            if (_rewardsTag != null)
                UiKit.Place(_rewardsTag, 0.356f, 0.312f, 0.642f, 0.368f);
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
            // Kaybetme paneli AYRI bir mor (ölçüm: 95,32,186); kazanma
            // panelinin moruyla aynı olması "aynı sonuç" izlenimi veriyordu.
            var difficultyTint = won
                ? CardTint(_session != null ? _session.Difficulty : Core.LevelDifficulty.Normal)
                : CardFailViolet;
            TintCard(difficultyTint);
            if (_resultTitle != null)
                UiKit.SetOutline(_resultTitle, OutlineFor(difficultyTint));

            if (_resultDifficulty != null)
            {
                string label = won && _session != null
                    ? Core.LevelDifficultyRule.Label(_session.Difficulty) : "";
                _resultDifficulty.text = label;
                _resultDifficulty.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }
            // RENK BEYAZ, ÇÜNKÜ ALTIN ARTIK GEÇİŞTEN GELİYOR.
            //
            // `color` ile `colorGradient` TMP'de ÇARPILIYOR. Burada altın
            // (255,194,46) yazılı kaldığı sürece geçişin açık sarı tepesi
            // (255,214,90) ile çarpılıp (255,163,16) oluyordu — yani
            // başlığın üstü de altı da turuncu çıkıyor, geçiş görünmüyordu.
            // Ekranda "gradyan çalışmıyor" gibi duruyordu; oysa çalışıyordu,
            // üstüne ikinci bir renk biniyordu.
            //
            // DERS (iki renk kaynağı varsa biri BEYAZ olmalı): Çarpımla
            // birleşen katmanlarda nötr eleman 1'dir. Rengi taşıyan katman
            // hangisiyse öteki beyaza çekilmeli.
            _resultTitle.color = Color.white;

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

            // Yığın görseli MİKTARA göre: 20 jeton küçük bir öbek, 200 jeton
            // bir tepe. Referansta da kutu büyüklüğü miktarla değişiyor.
            if (_coinPile != null && hasReward)
            {
                var pileSprite = PileFor(shownReward);
                if (pileSprite != null) _coinPile.sprite = pileSprite;
            }

            _failHeart.gameObject.SetActive(!won);
            _rewardsTag.gameObject.SetActive(!won);

            bool advance = won && _session.HasNextLevel;
            _resultPrimaryLabel.text = advance ? "Continue" : won ? "Play Again" : "Try Again";

            // Rengi burada EZMİYORUZ. Eskiden hedef grafik düğmenin yüzüydü
            // ve kaybedince kırmızıya boyanmasın diye beyaza çekiliyordu;
            // artık hedef grafik en dıştaki KOYU ÇİZGİ, onu beyaza çekmek
            // düğmenin çerçevesini yok ederdi. Referansta "Try Again" de
            // yeşil, yani ezmeye zaten gerek yok.

            ApplyResultLayout(won);

            // REKLAM DÜĞMESİ KALDIRILDI (5. tur).
            //
            // Kullanıcı: "watch ad double reward orijinal oyunda var mı,
            // yoksa kaldır; varsa buton ve yeri kötü, bozuk duruyor."
            //
            // REFERANS TARANDI (`Game over .mp4` tamamı, 2 fps'te 50 kare;
            // Türkçe yerelleştirilmiş sürüm): kaybetme akışı üç panel —
            // "Süre Doldu" (+30 saniye, yeşil düğme "Zaman Ekle 900"),
            // "Devam Et?" (kırık kalp, "Oyna 900") ve "BAŞARISIZ"
            // ("Yeniden Dene"). PERFECT kartında da yalnız "Continue" var.
            // Hiçbirinde REKLAM düğmesi YOK; devam etmenin bedeli JETON.
            //
            // DERS (bir özellik "olmalı" diye eklenmez): Ödüllü reklam mobil
            // bulmacalarda o kadar yaygın ki referansı açıp bakmadan
            // eklenmişti. Referansın para modeli başka: oyuncuya reklam
            // değil, biriktirdiği jetonu harcama seçeneği sunuyor.
            _adButton.gameObject.SetActive(false);

            // KAZANMA KARTI ÖLÇEKLENEREK GELMİYOR — ANINDA ORADA (8. tur).
            //
            // ÖLÇÜM (ham 59,47 fps kare): siyah kutlama ekranından sonra kart
            // TEK KAREDE tam boyunda beliriyor (313. kare). Bir önceki karede
            // hiç yok, sonrakinde kenarları son yerinde. Ölçeklenerek açılan
            // bir kartın en az 8-10 karesi olurdu.
            //
            // Canlanan şey kartın kendisi değil İÇİNDEKİLER: ödül yığını iki
            // katından oturuyor, başlık kartın üst kenarından yukarı açılıyor,
            // düğme aşarak geliyor. Kart da ölçeklenirse bunların hepsi onun
            // içinde kaydığı için hiçbiri okunmuyor — üstelik kutlama zaten
            // çok hareketliydi, kartın sabit gelmesi o gürültüden sonra
            // "durduk, ödülüne bak" diyor.
            //
            // Kaybetme kartı eskisi gibi açılıyor: orada önce sessiz bir
            // ekran var, kartın kendini duyurması gerekiyor.
            if (won)
            {
                // Önceki bir açılış hareketi hâlâ yaşıyor olabilir: boş bir
                // rutinle değiştirmek hem onu kesiyor hem de kartı son
                // boyunda bırakıyor.
                GameKit.FX.Juice.Replace(_resultCard, HoldCard(_resultCard.transform));
            }
            else
            {
                // Kaybetme kartında geliş töreni yok; bir önceki KAZANIŞ
                // bunları sıfır ölçekte bırakmış olabilir, geri alınıyor.
                if (_resultTitle != null) _resultTitle.transform.localScale = Vector3.one;
                if (_resultPrimary != null) _resultPrimary.transform.localScale = Vector3.one;
                if (_rewardBadge != null) _rewardBadge.transform.localScale = Vector3.one;
                if (_perfectBadge != null) _perfectBadge.transform.localScale = Vector3.one;

                GameKit.FX.Juice.Replace(_resultCard,
                    GameKit.FX.Juice.CardEntrance(_resultCard.transform));
            }
            Services.AudioService.PanelOpen();

            if (won) GameKit.FX.Juice.Run(CelebrateRoutine());
        }

        // PERFECT KARTININ İÇ ZAMANLAMASI — KARE KARE ÖLÇÜLDÜ (8. tur).
        //
        // `…Levels 1-20 Walkthrough.mp4`, 12. bölümün kartı, 59,47 fps.
        // Sıfır anı, kartın belirdiği kare (313):
        //
        //   0,000  ödül yığını 2,00 kat boyunda, KÜÇÜLMEYE başlıyor
        //   0,135  yığın son boyunda
        //   0,151  "PERFECT!" bir noktadan açılmaya başlıyor
        //   0,404  başlık son boyunda
        //   0,252  "Continue" düğmesi açılmaya başlıyor
        //   0,521  düğme son boyunda (tepe 1,09 kat)
        //   0,639  ödül sayısı plakası açılıyor (tepe 1,24 kat)
        //   0,790  plaka son boyunda
        //
        // Bizde sıra aynıydı ama süreler bambaşkaydı: başlık 0,12'de
        // başlayıp 0,36 sürüyordu, yığın 0,36'da başlayıp 0,42 sürüyordu ve
        // düğmenin hiç geliş hareketi YOKTU. Toplamda kart "yavaş yavaş
        // dolan bir liste" gibi açılıyordu; referansta her şey ilk yarım
        // saniyede bitiyor.
        const float PileSettle = 0.135f;
        const float PileFrom = 2.00f;
        const float TitleAt = 0.151f, TitleGrow = 0.253f;
        const float ButtonAt = 0.252f, ButtonGrow = 0.269f, ButtonPeak = 1.09f;
        const float BadgeAt = 0.639f, BadgeGrow = 0.151f, BadgePeak = 1.24f;

        /// <summary>Kartı hareketsiz, tam boyunda tutar (kazanma kartı).</summary>
        static System.Collections.IEnumerator HoldCard(Transform card)
        {
            if (card != null) card.localScale = Vector3.one;
            yield break;
        }

        /// <summary>Sırası gelene kadar sıfır ölçek (bkz. CelebrateRoutine).</summary>
        static void HideForEntrance(Transform target)
        {
            if (target != null) target.localScale = Vector3.zero;
        }

        /// <summary>
        /// Ödül yığınının gelişi: İKİ KATINDAN küçülerek oturuyor.
        ///
        /// ÖLÇÜM: yığının genişliği 468 → 233 piksel, sekiz karede. Yani
        /// kart açıldığı anda yığın kartı neredeyse dolduruyor ve hızla
        /// yerine çekiliyor.
        ///
        /// DERS (küçülerek gelmek, büyüyerek gelmekten BAŞKA bir şey söyler):
        /// Eski sürüm yığını %45'ten büyütüyordu — "bir şey belirdi" demek.
        /// Referans onu %200'den küçültüyor — "bir şey ÇARPTI" demek. İkisi
        /// de ölçek animasyonu, ama biri ödülü olay yapıyor, diğeri yapmıyor.
        /// </summary>
        System.Collections.IEnumerator PileEntrance()
        {
            var pile = _coinPile != null ? _coinPile.rectTransform : null;
            if (pile == null) yield break;

            var glow = _rewardArt.Find("Glow");
            var glowImage = glow != null ? glow.GetComponent<Image>() : null;
            Color glowTarget = glowImage != null ? glowImage.color : Color.clear;

            for (float t = 0f; t < PileSettle; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / PileSettle);
                // Çıkışta yavaşlayan eğri: ilk kareler hızlı, son kareler
                // yumuşak. Ölçülen genişlikler de böyle (468→397→367→335→
                // 304→277→253→239→233).
                float e = 1f - (1f - k) * (1f - k);
                pile.localScale = Vector3.one * Mathf.Lerp(PileFrom, 1f, e);
                if (glowImage != null)
                {
                    var c = glowTarget; c.a = glowTarget.a * k;
                    glowImage.color = c;
                }
                if (glow != null) glow.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, e);
                yield return null;
            }
            pile.localScale = Vector3.one;
            if (glow != null) glow.localScale = Vector3.one;
            if (glowImage != null) glowImage.color = glowTarget;
        }

        System.Collections.IEnumerator CelebrateRoutine()
        {
            // ÖNCE SAKLA, SONRA GETİR.
            //
            // `PopScale` ilk karesinde ölçeği sıfırlıyor — ama o ilk kare
            // beklemeden SONRA geliyor. Sıfırlama burada yapılmasaydı başlık
            // 0,15 saniye (dokuz kare), düğme 0,25 saniye (on beş kare) tam
            // boyunda ekranda durur, sonra sıfıra düşüp yeniden açılırdı.
            //
            // DERS (gecikmeli bir animasyonun BAŞLANGIÇ DURUMU gecikemez):
            // Hareketin kendisi sonra başlayabilir, ama nesnenin o ana kadar
            // hangi hâlde duracağı HEMEN yazılmalı.
            HideForEntrance(_resultTitle != null ? _resultTitle.transform : null);
            HideForEntrance(_resultPrimary != null ? _resultPrimary.transform : null);
            HideForEntrance(_rewardBadge != null ? _rewardBadge.transform : null);

            // Yığın kartla BİRLİKTE geliyor, sonradan değil.
            if (_rewardArt != null && _rewardArt.gameObject.activeSelf)
                GameKit.FX.Juice.Run(PileEntrance());

            // Bölüm adının bir gelişi YOK — ilk kareden itibaren kartın
            // üstünde. Eskiden yaylanarak açılıyordu; kartla aynı anda duran
            // tek yazı olması gözün önce ödüle gitmesini sağlıyor.
            if (_perfectBadge != null) _perfectBadge.transform.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(TitleAt);

            if (_resultTitle != null)
                GameKit.FX.Juice.Run(GrowTitle(_resultTitle.rectTransform));
            Services.AudioService.Star();

            yield return new WaitForSecondsRealtime(ButtonAt - TitleAt);

            if (_resultPrimary != null)
                GameKit.FX.Juice.Run(PopScale(_resultPrimary.transform,
                    ButtonGrow, ButtonPeak));

            yield return new WaitForSecondsRealtime(BadgeAt - ButtonAt);

            if (_rewardBadge != null && _rewardBadge.gameObject.activeSelf)
            {
                GameKit.FX.Juice.Run(PopScale(_rewardBadge.transform,
                    BadgeGrow, BadgePeak));
                Services.AudioService.Coin();
            }
        }

        /// <summary>
        /// "PERFECT!" başlığının açılışı: KENDİ ORTASINDAN değil, ALTINDAKİ
        /// bir noktadan büyüyor.
        ///
        /// ÖLÇÜM (ham kare, altın piksellerin kutusu): başlık büyürken üst
        /// kenarı 292 → 190, alt kenarı 307 → 270 pikselde geziniyor. İkisi
        /// de yükseliyor ama alt kenar daha yavaş. Ölçekleme merkezini bu iki
        /// eğriden çözünce y = 314 çıkıyor — başlığın alt kenarının 44 piksel
        /// ALTI, yani kartın üst kenarının biraz içi.
        ///
        /// Görünen etki: yazı kartın tepesinden yukarı doğru AÇILIYOR, sanki
        /// kartın içinden çıkıyor. Ortadan ölçeklenen bir başlık "belirdi"
        /// der; buradaki hareket "kart onu yukarı itti" der.
        ///
        /// DERS (dönme noktası da ölçülebilir bir şeydir): Bir ölçek
        /// animasyonunu tarif etmeye süre ve oran yetmiyor; merkezin nerede
        /// olduğu hareketin ANLAMINI değiştiriyor. İki kenarı ayrı ayrı
        /// izlemek merkezi tek denklemle veriyor.
        /// </summary>
        static System.Collections.IEnumerator GrowTitle(RectTransform title)
        {
            if (title == null) yield break;

            // Kutu dört köşesinden de bağlı (esnetilmiş) olduğu için pivotu
            // değiştirmek yerini OYNATMIYOR, yalnız ölçek merkezini taşıyor.
            title.pivot = new Vector2(0.5f, -0.54f);

            for (float t = 0f; t < TitleGrow; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / TitleGrow);
                // Ölçülen eğri neredeyse doğrusal, sonunda hafifçe yavaşlıyor
                // ve %1 aşıp dönüyor — `EaseOutBack`in belirgin yaylanması
                // burada YOK.
                float e = k * k * (3f - 2f * k) * 0.35f + k * 0.65f;
                title.localScale = Vector3.one * (e * 1.01f);
                yield return null;
            }
            title.localScale = Vector3.one;
        }

        /// <summary>
        /// Sıfırdan tepe ölçeğe, oradan son boyuna. Düğme ve ödül plakası
        /// bunu kullanıyor; iki aşma oranı da referanstan ölçüldü
        /// (düğme 1,09; plaka 1,24).
        /// </summary>
        static System.Collections.IEnumerator PopScale(
            Transform target, float duration, float peak)
        {
            if (target == null) yield break;

            float rise = duration * 0.56f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (target == null) yield break;
                float scale;
                if (t < rise)
                {
                    float k = t / rise;
                    scale = Mathf.Lerp(0f, peak, 1f - (1f - k) * (1f - k));
                }
                else
                {
                    float k = Mathf.Clamp01((t - rise) / (duration - rise));
                    scale = Mathf.Lerp(peak, 1f, k * k * (3f - 2f * k));
                }
                target.localScale = Vector3.one * scale;
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }
    }
}
