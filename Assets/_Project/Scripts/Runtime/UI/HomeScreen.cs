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
        Image _livesInfinity;
        TextMeshProUGUI _livesTimer;
        TextMeshProUGUI _levelLabel;
        TextMeshProUGUI _difficultyLabel;
        TextMeshProUGUI _avatarInitial;
        RectTransform _rewardRibbon;
        TextMeshProUGUI _rewardLabel;
        PillTint _playPill;
        Button _playButton;
        GameObject _adRow;
        TextMeshProUGUI _adOfferSub;

        readonly StringBuilder _scratch = new StringBuilder(32);
        int _shownLives = -1, _shownCoins = -1, _shownRefill = -2, _shownNext = -1;

        float _nextTick;

        /// <summary>Can göstergesinin nabzı için (37. madde).</summary>
        RectTransform _heartRect;
        RectTransform _livesTrackRect;
        PrimeTween.Tween _heartPulse;

        /// <summary>Sayaçların dokunma yüzeyleri — teşhis ve testte aranıyor.</summary>
        Image[] _coinHitAreas, _livesHitAreas;

        RectTransform _livesPopup;
        TextMeshProUGUI _livesPopupCount, _livesPopupTimer;

        DailyRewardPanel _daily;

        RectTransform _slideRoot, _slideCover;

        /// <summary>Sekme geçişi için erişim (MenuShell kullanıyor).</summary>
        public static HomeScreen Instance { get; private set; }

        void Awake() => Instance = this;

        /// <summary>
        /// Ana ekranı yatayda kaydırır — sekme geçişinde menü ekranlarıyla
        /// birlikte hareket etsin diye. Manzara ve arayüz ayrı ebeveynlerde
        /// olduğu için İKİSİ de kaydırılıyor.
        /// </summary>
        public void Slide(float fromX, float toX, float duration)
        {
            if (_slideRoot != null)
                GameKit.FX.Juice.Replace(_slideRoot,
                    GameKit.FX.Juice.SlideX(_slideRoot, fromX, toX, duration));

            if (_slideCover != null)
                GameKit.FX.Juice.Replace(_slideCover,
                    GameKit.FX.Juice.SlideX(_slideCover, fromX, toX, duration));
        }

        void Start()
        {
            BuildUi();
            Refresh();
            _daily?.ShowIfAvailable();
        }

        void OnEnable()
        {
            // Menüye her dönüşte değil, GÜN değiştiyse açılır — servisin
            // kendisi "bugün alındı mı" kontrolünü yapıyor.
            if (_daily != null) _daily.ShowIfAvailable();
        }

        void Update()
        {
            if (Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 1f;

            if (MetaServices.Ready) MetaServices.Lives.Refresh();
            Refresh();

            // Baloncuk açıkken geri sayım da her saniye yenilenmeli; yoksa
            // panel açıldığı andaki süreyi donmuş gibi gösterir.
            if (_livesPopup != null && _livesPopup.gameObject.activeSelf)
                RefreshLivesPopup();
        }

        void BuildUi()
        {
            var canvas = UiKit.CreateCanvas("HomeCanvas");
            canvas.transform.SetParent(transform, worldPositionStays: false);
            var root = UiKit.CreateSafeArea(canvas);
            GameKit.UI.UiTweak.Mark(root, "HomeScreen");

            // Manzara, güvenli alanın değil EKRANIN tamamını kaplamalı; çentiğin
            // altında zemin rengi görünmesin diye kanvasa doğrudan bağlanıyor.
            var cover = UiKit.CreateCover("Background", canvas.transform,
                UiSkin.Get(Art.MenuBack), UiKit.Background);
            cover.transform.SetAsFirstSibling();

            // Sekme geçişinde ana ekran da kayıyor (bkz. Slide). Manzara ile
            // arayüz AYRI ebeveynlerde olduğu için ikisini de tutmak gerekiyor;
            // yalnız birini kaydırmak zemini yerinde bırakır ve geçiş "arayüz
            // kaydı ama dünya durdu" gibi görünür.
            _slideRoot = root;
            _slideCover = cover.rectTransform;

            BuildCharacters(root);
            BuildTopBar(root);
            BuildPlayButton(root);
            BuildAdOffer(root);
            BuildLivesPopup(root);

            // Günlük ödül: ana ekran açılınca hediye varsa kendiliğinden gelir.
            _daily = DailyRewardPanel.Build(canvas.transform);
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
            UiKit.Place(group, 0.03f, 0.248f, 0.97f, 0.571f);

            // Hafif nefes alma: tamamen hareketsiz bir görsel, arkasındaki
            // manzaranın parçası sanılıyor.
            //
            // DERS (nefes GÖRÜNMEMELİ, hissedilmeli): Genlik %2'yi geçtiğinde göz
            // hareketi fark eder ve karakterler "titriyor" görünür. Amaç ekranın
            // ölü olmadığını çevresel görüşe söylemek, dikkat çekmek değil.
            GameKit.FX.Juice.Breathe(group.transform, amount: 0.018f, period: 3.6f);
        }

        // ---------------------------------------------------------------- üst bar

        /// <summary>
        /// Üst bar — X oranları `ana ekran.jpeg`'ten (946×2048) BİREBİR alındı.
        ///
        /// DERS (iki kap değil, TEK kap): Burası eskiden jeton ve can için iki
        /// AYRI koyu kapsül kuruyordu ve aralarında bir boşluk kalıyordu.
        /// Referansta tek bir kapsül var (X 0.303-0.866); jeton ikonu onun SOL
        /// UCUNDAN taşıyor, kalp de içinde duruyor. Fark küçük görünür ama üst
        /// bar bir "kaynak çubuğu" olarak okunuyor — ikiye bölünce iki ayrı
        /// gösterge gibi duruyor ve ekranın üstü dağınık görünüyordu.
        ///
        /// Ölçülen yerleşim (ekran genişliğine oran):
        ///   avatar 0.058-0.201 · kapsül 0.303-0.866
        ///   jeton ikonu 0.220-0.308 · sayı 0.345-0.419 · artı 0.457-0.511
        ///   kalp 0.529-0.628 · durum 0.650-0.752 · artı 0.772-0.827
        ///   dişli 0.872-0.962
        /// </summary>
        void BuildTopBar(Transform root)
        {
            const float top = 0.976f, bottom = 0.936f;

            BuildAvatar(root, 0.058f, 0.201f, bottom - 0.018f, top + 0.018f);

            // İKİ AYRI ÇUBUK, kapsül değil DİKDÖRTGEN.
            //
            // DERS (referansı GERÇEKTEN oku, gevşek eşikle değil): Önce bu iki
            // çubuğu TEK bir uzun kapsüle birleştirmiştim. Sebep bir ölçüm
            // hatasıydı: koyu bölgeyi ararken kullandığım eşik jeton ve kalp
            // ikonlarının GÖLGELERİNİ de "koyu" saydı ve iki çubuğu tek parça
            // gibi gösterdi. Referansı yakınlaştırıp yazıların ALTINDAN geçen
            // bir satırı taradığımda gerçek çıktı: iki ayrı çubuk var
            // (X 0.296-0.511 ve 0.616-0.825), üstelik köşeleri kapsül gibi
            // değil, hafif yuvarlatılmış DİKDÖRTGEN.
            //
            // Kullanıcı bunu ekrana bakar bakmaz gördü; ben ölçtüğümü sanıp
            // yanlış ölçmüştüm. Ölçmek, doğru yeri ölçmek demek.
            Image ResourceBar(string name, float x0, float x1)
            {
                var panel = UiKit.CreateRoundedPanel(name, root,
                    new Color(0.055f, 0.035f, 0.145f, 0.50f));
                UiKit.Place(panel, x0, bottom, x1, top);
                // ppu BÜYÜDÜKÇE köşe KÜÇÜLÜR: 0.16 kapsül yapıyordu,
                // 1.4 referanstaki dikdörtgen köşeyi veriyor.
                UiKit.SetSliceScale(panel, 1.4f);
                panel.raycastTarget = false;
                return panel;
            }

            // ÇUBUK İKONUN ALTINDAN BAŞLAR, YANINDAN DEĞİL.
            //
            // Kullanıcı: "o transparan arka planının başlangıç noktasını
            // biraz daha kalbe yaklaştır, sola doğru; kalbin üstünde gibi
            // olsun. Aynı şekilde coin için de — kalp ile koyu arka plan
            // arasında boşluk olmasın."
            //
            // BULUNAN SEBEP — SİLUET İLE KUTU AYNI ŞEY DEĞİL. Çubuklar
            // ikonların KUTUSUNUN içinden başlıyordu (kalp 0.529-0.628,
            // çubuk 0.616) — kâğıt üzerinde 13 birimlik bir binişme var.
            // Ama kalp dikdörtgen değil: en geniş yeri ÜST kısmı, ortada ise
            // iki yana doğru daralıyor. Çubuk dikeyde ortada durduğu için
            // tam da kalbin daraldığı hizaya denk geliyor ve orada siluet
            // 0.616'ya çoktan varmamış oluyor — arada zemin görünüyor.
            //
            // DERS (bir ikonun kutusu, kaplayacağı alanı söylemez):
            // `preserveAspect` görseli kutuya sığdırır ama şeklin NEREDE
            // olduğu şeklin kendisine bağlıdır. İki öğeyi birleştirirken
            // kutuların değil, o hizadaki SİLUETLERİN değmesi gerekiyor.
            // Güvenli kural: çubuk ikonun ORTASINDAN başlasın — siluet o
            // noktada her zaman dolu.
            //
            // Kalbin ortası (0.529+0.628)/2 = 0.579, jetonunki 0.264.
            // Çubuklar ikonlardan ÖNCE kuruluyor, yani ikon üstünü örtüyor.
            var coinTrack = ResourceBar("Track_Coin", 0.262f, 0.511f);
            var livesTrack = ResourceBar("Track_Lives", 0.574f, 0.825f);

            // TÜM SAYAÇ DOKUNULABİLİR, yalnız artı değil.
            //
            // DERS (küçük bir hedefe basmak zorunda bırakma): Mağazaya giden
            // tek yol 54 birim genişliğindeki artı düğmesiydi. Oyuncu jeton
            // sayısına ya da simgeye basıyor, hiçbir şey olmuyor ve arayüzü
            // "tepkisiz" buluyor — kullanıcının "paraya tıklarsak direkt
            // ikona veya para yazısına mağazaya yönlendirsin" bulgusu.
            // Referansta sayacın TAMAMI bir düğme; artı yalnız oraya ne
            // olacağını söyleyen bir işaret.
            //
            // Çubuk şeridin ARKASINDA duruyor ve simge/yazı onun üstünde;
            // ikisi de raycast almadığı için dokunuş çubuğa düşüyor.
            UiKit.MakeClickable(coinTrack, () => MenuShell.Instance?.ShowStepped("store"));
            UiKit.MakeClickable(livesTrack, ShowLivesPopup);

            // Simge ve yazı çubuğun DIŞINDA kalan kısımlarda da çalışsın diye
            // ayrıca tıklanabilir yapılıyor (jeton simgesi çubuğun soluna
            // taşıyor, kalp de öyle).
            _coinHitAreas = new[] { coinTrack };
            _livesHitAreas = new[] { livesTrack };
            _livesTrackRect = livesTrack.rectTransform;

            // --- jeton ---
            var coinIcon = UiKit.CreateIcon("Icon_Coin", root, UiSkin.Get(Art.Coin));
            UiKit.Place(coinIcon, 0.220f, bottom - 0.004f, 0.308f, top + 0.004f);
            UiKit.MakeClickable(coinIcon, () => MenuShell.Instance?.ShowStepped("store"));

            _coinLabel = UiKit.CreateTitle("Value_Coin", root, "", 40, CoinInk,
                new Color(0.10f, 0.07f, 0.24f));
            UiKit.Place(_coinLabel, 0.330f, bottom + 0.003f, 0.445f, top - 0.003f);

            var coinPlus = UiKit.CreateIconButton("Plus_Coin", root, UiSkin.Get(Art.Plus));
            UiKit.Place(coinPlus, 0.457f, bottom + 0.002f, 0.511f, top - 0.002f);
            coinPlus.onClick.AddListener(() => MenuShell.Instance?.ShowStepped("store"));

            // --- can ---
            var heart = UiKit.CreateIcon("Icon_Heart", root, UiSkin.Get(Art.Heart));
            UiKit.Place(heart, 0.529f, bottom - 0.005f, 0.628f, top + 0.005f);
            UiKit.MakeClickable(heart, ShowLivesPopup);
            _heartRect = heart.rectTransform;

            // CAN GÖSTERGESİ NABIZ ATIYOR (kullanıcı: "orası küçülüp büyüsün,
            // minik hareket ediyor gibi olsun, orijinal oyunda var").
            //
            // DERS (nabız DAVET eder, nefes yalnız canlı tutar): Karakterlerdeki
            // `Breathe` %1.8 genlikle "ekran ölü değil" diyor. Buradaki hareket
            // başka bir iş yapıyor — dokunulabilir olduğunu söylüyor — o yüzden
            // biraz daha belirgin (%5) ve daha yavaş. %10'u geçerse sayacın
            // rakamı okunmaz hâle geliyor, denendi.
            _heartPulse = PrimeTween.Tween.Scale(_heartRect, 1f, 1.05f, 0.95f,
                PrimeTween.Ease.InOutSine, cycles: -1,
                cycleMode: PrimeTween.CycleMode.Yoyo, useUnscaledTime: true);

            // Sayı kalbin ÜSTÜNDE ortalanır — referansta da öyle.
            _livesLabel = UiKit.CreateTitle("Value_Heart", heart.transform, "", 46,
                CoinInk, new Color(0.42f, 0.03f, 0.03f));
            UiKit.Place(_livesLabel, 0f, 0.04f, 1f, 0.92f);

            _livesTimer = UiKit.CreateLabel("Status_Heart", root, "", 39, CoinInk);
            UiKit.Place(_livesTimer, 0.640f, bottom + 0.003f, 0.762f, top - 0.003f);

            var lifePlus = UiKit.CreateIconButton("Plus_Heart", root, UiSkin.Get(Art.Plus));
            UiKit.Place(lifePlus, 0.772f, bottom + 0.002f, 0.827f, top - 0.002f);
            lifePlus.onClick.AddListener(() => MenuShell.Instance?.ShowStepped("store"));

            // Sınırsız can hakkı sürerken sayının yerini ∞ GÖRSELİ alır.
            //
            // DERS (simge YAZI DEĞİLDİR): "∞" karakteri Baloo 2'de yok, TMP
            // onun yerine boş kutu çizer — bu projede beşinci tekrar. Mağazada
            // zaten kullandığımız `icon_infinite` sprite'ı kullanılıyor.
            _livesInfinity = UiKit.CreateIcon("Infinite", _livesLabel.transform.parent,
                UiSkin.Get(Art.Infinite) ?? MenuSprites.Infinity);
            var number = _livesLabel.rectTransform;
            var badge = _livesInfinity.rectTransform;
            badge.anchorMin = number.anchorMin;
            badge.anchorMax = number.anchorMax;
            badge.offsetMin = number.offsetMin;
            badge.offsetMax = number.offsetMax;
            _livesInfinity.enabled = false;

            var gear = SquareButton(root, "Gear", Art.Gear, 0.872f, 0.962f, bottom, top);
            gear.onClick.AddListener(() => MenuShell.Instance?.Show("settings"));
        }

        void BuildAvatar(Transform root, float x0, float x1, float y0, float y1)
        {
            // DERS (katman sırası = çizim sırası): Çerçeve düğmenin KENDİ
            // grafiğiydi ve portre onun ÇOCUĞU; uGUI çocukları sonra çizdiği
            // için portre çerçevenin üstüne biniyor, hem taşıyor hem çerçeveyi
            // örtüyordu. Doğru sıra: zemin → portre → çerçeve. Böylece çerçeve
            // portrenin taşan kısmını kapatıyor ve gerçek bir "çerçeve içinde
            // resim" oluyor.
            var button = UiKit.CreateRect("Avatar", root);
            UiKit.Place(button, x0, y0, x1, y1);

            // Zemin: çerçevenin ortası oyuk, arkasında bir şey olmazsa portre
            // boşlukta duruyor.
            var back = UiKit.CreateRoundedPanel("Back", button, new Color(0.20f, 0.62f, 0.78f));
            UiKit.Place(back, 0.13f, 0.13f, 0.87f, 0.87f);
            UiKit.SetSliceScale(back, 0.35f);

            var portrait = UiSkin.Get(Art.Avatar);
            if (portrait != null)
            {
                var face = UiKit.CreateIcon("Portrait", button, portrait);
                UiKit.Place(face, 0.10f, 0.06f, 0.90f, 0.94f);
            }
            else
            {
                _avatarInitial = UiKit.CreateTitle("Initial", button, "?", 46,
                    CoinInk, new Color(0.12f, 0.09f, 0.28f));
                UiKit.Place(_avatarInitial, 0f, 0f, 1f, 1f);
            }

            var frameSprite = UiSkin.Get(Art.AvatarFrame);
            var frame = frameSprite != null
                ? UiKit.CreateIcon("Frame", button, frameSprite)
                : UiKit.CreateRoundedPanel("Frame", button, new Color(1f, 1f, 1f, 0f));
            UiKit.Place(frame, 0f, 0f, 1f, 1f);

            // Tıklama en son: dokunuşu yakalayan saydam yüzey en üstte olmalı.
            var hit = UiKit.CreatePanel("Hit", button, new Color(1f, 1f, 1f, 0f));
            var click = hit.gameObject.AddComponent<Button>();
            click.targetGraphic = hit;
            click.transition = Selectable.Transition.None;
            hit.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            click.onClick.AddListener(() => MenuShell.Instance?.Show("profile"));
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

        // --- OYNA düğmesindeki iki yazının dikey yerleşimi (7. tur, P55) ---
        //
        // Düğmenin alt %34'ü 3B dudak; yazı yüzeyi 0.34-1.00, ortası 0.67.
        //   zorluk VARSA : blok 0.375-0.955, ortası 0.665
        //   zorluk YOKSA : blok 0.385-0.955, ortası 0.670
        const float LevelTop = 0.955f;
        const float LevelH = 0.350f;                     // seviye yazısının kutusu
        const float LevelBottomWithDifficulty = LevelTop - LevelH;   // 0.605
        const float LevelBottomAlone = 0.385f;

        void BuildPlayButton(Transform root)
        {
            // GÖVDE ARTIK MENÜLERİN DÜĞMESİYLE AYNI REÇETEDEN (8. tur).
            //
            // Kullanıcı: "ana menüdeki oynama butonu... tarz olarak alakasız
            // kalmış." Sebebi ölçüldü: bu düğme `btn_green.png` görselinden,
            // menülerin düğmesi ise koddan geliyordu. Referansta ikisi de aynı
            // ışık profiline sahip — ana ekran düğmesinin yan duvarı da
            // ölçüldü ve pauseâ€™daki Resume ile aynı çıktı (yüz (133,28,252),
            // duvar (95,20,200) = ×0,79, dipte koyu etek (61,11,128)).
            var body = MenuPage.PillBody("Play", root, MenuPage.Green,
                                         out _, out _playPill);
            _playButton = body.gameObject.AddComponent<Button>();
            _playButton.transition = Selectable.Transition.None;
            body.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            // DERS (düğme ekranı yemez): İlk hâli genişliğin %68'i ve
            // yüksekliğin %13'ü kadardı; ekranın altını kaplıyor ve manzarayı
            // eziyordu. Referanstaki düğme genişliğin ~%54'ü, yüksekliğin
            // ~%7'si. Küçültünce hem manzara nefes alıyor hem düğme daha
            // "basılası" duruyor — büyük düğme güçlü değil, hantal görünüyor.
            // Genişlik %54 → %44, yükseklik %7.2 → %10.5. İlk küçültmede yatayda
            // yeterince daraltmayıp dikeyde fazla kısaltmıştım: yazı iki kenara
            // dayanıyor, düğme de yassı bir şerit gibi duruyordu. Bir düğmenin
            // OKUNAKLI olması için yazının iki yanında nefes payı kalmalı.
            // DERS (dokunma hedefleri BİRBİRİNE DEĞMEZ): Düğmenin alt kenarı
            // sekme çubuğunun seçili kartına dayanıyordu; ikisi tek bir yığın
            // gibi görünüyor ve parmak yanlış olana basıyordu. Sekme kartı
            // çubuğun üstüne %8 kadar taşıyor, düğme %17'den başlamalı ki
            // arada gerçek bir nefes payı kalsın.
            UiKit.Place(_playButton, 0.280f, 0.158f, 0.720f, 0.263f);
            _playButton.onClick.AddListener(PlayCurrent);

            // Yazılar YÜZÜN üstüne: dipteki koyu etek yazının altında kalsın.
            var face = _playButton.transform.Find("Face");

            // ÖLÇÜ REFERANSTAN (2026-08-17, `ana ekran.jpeg` 946×2048):
            // "Seviye 54" yazısının BÜYÜK harf yüksekliği 67 piksel, yani
            // ekran yüksekliğinin %3.27'si. Bizdeki 44 punto %1.65 veriyordu —
            // TAM İKİ KATI küçük. Kullanıcının "level yazısı çok küçük"
            // dediği şey buydu (7. APK bulgusu).
            //
            // DERS (oranı NEYE göre ölçtüğüne dikkat): Düğmenin İÇİNDEKİ
            // orana bakmak yanıltıyor — bizim düğmemiz referanstan %50 daha
            // yüksek, o yüzden "yazı/düğme" oranı iki tarafta farklı şeyi
            // anlatıyor. Doğru ölçü EKRANA göre olan: oyuncunun gözü düğmeyi
            // değil yazıyı okuyor.
            //
            // YAZI ÇİFTİ DÜĞMENİN GÖRÜNEN YÜZÜNE ORTALANDI (7. tur, P55).
            //
            // Kullanıcı: "Ana ekrandaki level yazıları biraz daha ortalanmış
            // olacak, taşma olmayacak."
            //
            // ÖLÇÜM: `btn_*` görselinin alt 9-dilim payı 68 piksel; 201,6
            // birimlik düğmede alt %34 3B DUDAK, yani yazının oturabileceği
            // yüz 0.34-1.00 ve ortası 0.67. İki yazının kapladığı blok ise
            // 0.355-0.94, ortası 0.6475 idi — %2,3 aşağıda. Üstelik iki kutu
            // 0.58-0.60 arasında ÜST ÜSTE biniyordu.
            //
            // DERS (ortalamanın ölçüsü kutu değil, GÖRÜNEN yüzeydir): Bu tur
            // aynı hata mağazadaki teklif bandında da çıktı (N49). Bir
            // yüzeyin bir kısmını başka bir katman (orada sanat paneli,
            // burada düğmenin dudağı) yiyorsa ortalama o payı düşmeli.
            //
            // Taşma tarafı artık yapısal olarak kapalı: `UiKit.CreateLabel`
            // her etikete <see cref="GameKit.UI.UiTextFit"/> takıyor.
            _levelLabel = UiKit.CreateTitle("Level", face, "", 84, CoinInk,
                new Color(0.10f, 0.06f, 0.22f));
            UiKit.Place(_levelLabel, 0.05f, LevelTop - LevelH, 0.95f, LevelTop);

            // DERS (yazı butonun YÜZÜNE oturur, kenarına değil): "Zor Seviye"
            // düğmenin alt %34'lük 3B kenarına denk geliyordu ve gölgenin
            // üstünde yüzüyormuş gibi duruyordu — "model yok" hissi oradan
            // geliyor. btn_purple'ın alt payı 68px, yani 202px yükseklikte
            // alt %34 kenardır; iki yazı da bunun üstünde kalmalı.
            // Zorluk da aynı oranda büyüdü (24 → 48). Referansta bu bir AYRI
            // rozet değil, düğmenin İÇİNDE ikinci satır — açık lavanta tonda.
            // Kullanıcı "zorluk kısmı için daha iyi buton olacak" derken
            // gördüğü şey, yazının o kadar küçük olması yüzünden düğmeye ait
            // görünmemesiydi.
            _difficultyLabel = UiKit.CreateTitle("Difficulty", face, "", 48,
                new Color(0.86f, 0.88f, 1f), new Color(0.10f, 0.06f, 0.22f));
            UiKit.Place(_difficultyLabel, 0.06f, LevelBottomWithDifficulty - 0.230f,
                                          0.94f, LevelBottomWithDifficulty);

            BuildRewardRibbon();
        }

        /// <summary>
        /// "Ödüller x3" şeridi — referanstan ölçülerek yeniden kuruldu.
        /// Referans: `ana ekran.jpeg` (946×2048), plaka x 338-606 / y 1482-1542,
        /// düğme x 250-700 / y 1536-1720.
        ///
        /// DERS (hazır görsel BAŞKA BİR ŞEYSE, kullanma): Şerit `badge_reward`
        /// görselini kullanıyordu. O görsel KARE bir çerçeve: turuncu kenarlık
        /// ve ortasında MOR bir pencere — bir avatar/ikon çerçevesi. 1.9:1 bir
        /// şeride gerdirilince kenarlık inceliyor, ortadaki mor pencere de
        /// yazının zemini oluyordu; beyaz yazı açık mor üstünde kayboluyordu.
        /// Kullanıcının "yazı okunmuyor" dediği şey buydu. Referansta bu şerit
        /// bir çerçeve değil DOLU turuncu bir plaka ve yazısı KOYU KAHVE.
        ///
        /// DERS (etiket düğmenin ARKASINA girer): Şerit düğme kökünün son
        /// çocuğuydu, yani düğmenin ÜSTÜNE çiziliyordu ve "üstüne yapıştırılmış"
        /// duruyordu. Referansta plakanın alt kenarı düğmenin arkasında kayboluyor
        /// — bu yüzden ilk kardeş yapılıyor. Tek satırlık fark, "yapıştırılmış"
        /// ile "takılmış" arasındaki fark.
        ///
        /// ÖLÇÜ: plaka düğme genişliğinin %59.5'i, düğmenin üstünden düğme
        /// yüksekliğinin %29'u kadar çıkıyor. Yazının büyük harf yüksekliği
        /// ekranın %1.46'sı (bizdeki 26 punto %0.95 veriyordu).
        /// </summary>
        void BuildRewardRibbon()
        {
            var ribbonRoot = UiKit.CreateRect("Ribbon", _playButton.transform);
            UiKit.Place(ribbonRoot, 0.196f, 0.780f, 0.791f, 1.290f);
            ribbonRoot.SetAsFirstSibling();      // düğmenin ARKASINDA kalsın
            _rewardRibbon = ribbonRoot;

            // Koyu kahve kenarlık + turuncu yüzey + üstte açık bir ışık.
            // Renkler referanstan örneklendi: kenar #5B2A08, yüzey #FBA40A,
            // üst ışık #FFC93C.
            var edge = UiKit.CreateRoundedPanel("Edge", ribbonRoot,
                new Color(0.357f, 0.165f, 0.031f));
            // Köşe yarıçapı referanstan ~20 birim. `pixelsPerUnitMultiplier`
            // TERS çalışır (büyük değer = küçük köşe) ve 0.5 ≈ 40 birim
            // verdiğine göre 1.0 ≈ 20 birim.
            UiKit.SetSliceScale(edge, 0.95f);
            edge.raycastTarget = false;
            UiKit.Place(edge, 0f, 0f, 1f, 1f);

            var faceFill = UiKit.CreateRoundedPanel("Face", ribbonRoot,
                new Color(0.984f, 0.643f, 0.039f));
            UiKit.SetSliceScale(faceFill, 1.05f);
            faceFill.raycastTarget = false;
            UiKit.Place(faceFill, 0f, 0f, 1f, 1f, padding: 7f);

            // Işık üstte toplanır. `FadeDown` altta opak olduğu için 180°
            // çevriliyor — aynı dokuyu ters yönde kullanmak, ikinci bir doku
            // üretmekten ucuz.
            var sheen = UiKit.CreateRect("Sheen", faceFill.transform);
            var sheenImage = sheen.gameObject.AddComponent<Image>();
            sheenImage.sprite = MenuSprites.FadeDown;
            sheenImage.type = Image.Type.Sliced;
            sheenImage.color = new Color(1f, 0.788f, 0.235f, 0.85f);
            sheenImage.raycastTarget = false;
            UiKit.Place(sheen, 0.02f, 0.10f, 0.98f, 0.94f);
            sheen.localRotation = Quaternion.Euler(0f, 0f, 180f);

            // KOYU KAHVE YAZI + KREM KONTUR. Referansta yazı plakadan daha
            // koyu; beyaz yazı turuncu üstünde yeterince ayrışmıyor.
            _rewardLabel = UiKit.CreateTitle("RibbonText", ribbonRoot, "", 40,
                new Color(0.290f, 0.114f, 0.008f), new Color(1f, 0.953f, 0.839f));
            UiKit.Place(_rewardLabel, 0.07f, 0.36f, 0.93f, 0.97f);
            UiKit.SetOutline(_rewardLabel, new Color(1f, 0.965f, 0.878f));

            // İngilizce "Rewards x3" Türkçe "Ödüller x3"ten uzun; punto üst
            // sınır, gerekirse küçülür. `enableAutoSizing` TEK BAŞINA çalışmaz
            // — kısıtlayıcı bir `overflowMode` olmadan TMP taşırır.
            _rewardLabel.enableAutoSizing = true;
            _rewardLabel.fontSizeMax = 40;
            _rewardLabel.fontSizeMin = 28;
            _rewardLabel.overflowMode = TextOverflowModes.Truncate;
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
        // ---- Can baloncuğu (2. tur, 37. madde) -----------------------------

        /// <summary>
        /// Cana dokununca çıkan küçük bilgi baloncuğu.
        ///
        /// Kullanıcı: "Cana tıkladığımızda artıya değil, direkt cana ve 'dolu'
        /// yazan kısma, örneğin popup gibi olsun."
        ///
        /// DERS (artı düğmesi bir KISAYOL, tek yol değil): Can sayacına
        /// dokunmanın tek sonucu mağazaya atlamaktı ve o da yalnız 55 birimlik
        /// artı düğmesinden. Oyuncunun sorduğu soru "nasıl can alırım" değil,
        /// "canım ne zaman dolacak" — mağazaya atmak sorunun cevabı değil,
        /// konuyu değiştirmek. Baloncuk önce CEVABI veriyor (kaç can var, ne
        /// zaman dolacak), satın alma yolunu ikincil bırakıyor.
        /// </summary>
        void BuildLivesPopup(Transform root)
        {
            _livesPopup = UiKit.CreateRect("LivesPopup", root);
            UiKit.Place(_livesPopup, 0f, 0f, 1f, 1f);

            var scrim = UiKit.CreatePanel("Scrim", _livesPopup,
                new Color(0.03f, 0.02f, 0.08f, 0.62f));
            scrim.raycastTarget = true;
            UiKit.MakeClickable(scrim, HideLivesPopup);

            var card = UiKit.CreateOutlinedBox("Card", _livesPopup,
                MenuPage.Panel, new Color(0.204f, 0.145f, 0.573f), borderInset: 0f);
            UiKit.Place(card, 0.13f, 0.545f, 0.87f, 0.795f);

            var heart = UiKit.CreateIcon("Heart", card.transform, UiSkin.Get(Art.Heart));
            UiKit.Place(heart, 0.36f, 0.50f, 0.64f, 0.94f);

            _livesPopupCount = UiKit.CreateTitle("Count", heart.transform, "", 52,
                CoinInk, new Color(0.42f, 0.03f, 0.03f));
            UiKit.Place(_livesPopupCount, 0f, 0.06f, 1f, 0.92f);

            _livesPopupTimer = UiKit.CreateTitle("Timer", card.transform, "", 36,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(_livesPopupTimer, 0.06f, 0.28f, 0.94f, 0.46f);

            var shop = MenuPage.PillButton("Shop", card.transform, "Get More",
                MenuPage.Green, 38, () => { HideLivesPopup(); MenuShell.Instance?.ShowStepped("store"); });
            UiKit.Place(shop, 0.20f, 0.06f, 0.80f, 0.26f);

            var close = UiKit.CreateIconButton("Close", card.transform,
                GameKit.UI.UiSprites.Circle, MenuPage.CloseRed);
            UiKit.Place(close, 0.845f, 0.80f, 1.015f, 1.09f);
            close.onClick.AddListener(HideLivesPopup);
            var mark = UiKit.CreateIcon("Mark", close.transform,
                GameKit.UI.UiSprites.Cross, CoinInk);
            mark.raycastTarget = false;
            UiKit.Place(mark, 0.26f, 0.26f, 0.74f, 0.74f);

            _livesPopup.gameObject.SetActive(false);
        }

        /// <summary>
        /// Can sayacına dokunuş.
        ///
        /// CAN DOLUYKEN PANEL AÇILMIYOR (4. tur, A5).
        ///
        /// Kullanıcı: "Can doluyken tıklayınca panel açılıyor; orijinalde yok.
        /// Can kısmı arka plan paneliyle birlikte küçük popup gibi davranmalı,
        /// tıklandıkça hafif büyüyüp küçülen animasyon oynatmalı."
        ///
        /// DERS (bir panel, SÖYLEYECEK ŞEYİ olduğunda açılır): Panel "kaç can
        /// var, bir sonraki ne zaman dolacak, nasıl can alınır" diyor. Canlar
        /// zaten doluyken bu üç sorunun da cevabı yok — panel açılıyor,
        /// "Lives are full!" yazıyor ve kapatılmayı bekliyor. Yani oyuncuya
        /// bildiği bir şeyi söylemek için bir dokunuş daha yaptırıyor.
        /// Referans doluyken hiç açmıyor; yalnız sayaç kısa bir nefes alıyor,
        /// bu da "dokunuşunu aldım ama yapacak bir şey yok" demenin en kısa
        /// yolu.
        /// </summary>
        void ShowLivesPopup()
        {
            if (_livesPopup == null) return;

            bool full = MetaServices.Ready && MetaServices.Lives.IsFull
                        && !MetaServices.Progress.HasInfiniteLives;
            if (full)
            {
                PulseLives();
                return;
            }

            RefreshLivesPopup();
            _livesPopup.gameObject.SetActive(true);
            GameKit.FX.Juice.Replace(_livesPopup,
                GameKit.FX.Juice.CardEntrance(_livesPopup.GetChild(1)));
            Services.AudioService.PanelOpen();
        }

        /// <summary>
        /// Can sayacının kendisi ve arka plan plakası birlikte kısa bir
        /// büyü-küçül yapıyor.
        ///
        /// İkisi ayrı ayrı tween'leniyor çünkü hiyerarşide kardeşler: kalp
        /// simgesi ve zaman yazısı plakanın ÜSTÜNDE değil YANINDA duruyor
        /// (plaka raycast almıyor, dokunuşu o topluyor). Tek bir taşıyıcıya
        /// almak düzeni yeniden yazmak demekti; iki tween aynı süre ve aynı
        /// eğriyle çalıştığı için ekranda tek bir hareket olarak okunuyor.
        /// </summary>
        void PulseLives()
        {
            Services.AudioService.Click();
            GameKit.Services.Haptics.Tap(GameKit.Services.HapticStrength.Light);

            if (_livesTrackRect != null)
                GameKit.FX.Juice.Replace(_livesTrackRect,
                    GameKit.FX.Juice.PunchScale(_livesTrackRect, 0.10f, 0.28f));

            if (_heartRect != null)
                GameKit.FX.Juice.PunchScale(_heartRect, 0.14f, 0.28f);
        }

        void HideLivesPopup()
        {
            if (_livesPopup == null) return;
            _livesPopup.gameObject.SetActive(false);
            Services.AudioService.PanelClose();
        }

        void RefreshLivesPopup()
        {
            if (_livesPopupCount == null || !MetaServices.Ready) return;

            var lives = MetaServices.Lives;
            _livesPopupCount.text = lives.Current.ToString();

            // Sınırsız can hakkı varken geri sayım yanıltıcı olur: can zaten
            // eksilmiyor. O durumda hakkın kalan süresi yazılıyor.
            if (MetaServices.Progress.HasInfiniteLives)
            {
                var left = MetaServices.Progress.InfiniteLivesLeft;
                _livesPopupTimer.text = left.TotalHours >= 1d
                    ? $"Unlimited for {(int)left.TotalHours}h {left.Minutes}m"
                    : $"Unlimited for {left.Minutes}m {left.Seconds}s";
                return;
            }

            var refill = lives.TimeToNextLife;
            _livesPopupTimer.text = lives.IsFull
                ? "Lives are full!"
                : $"Next life in {refill.Minutes:00}:{refill.Seconds:00}";
        }

        void BuildAdOffer(Transform root)
        {
            _adRow = UiKit.CreateRect("AdOffer", root).gameObject;
            UiKit.Place((RectTransform)_adRow.transform, 0.255f, 0.158f, 0.745f, 0.263f);

            // OYNA düğmesiyle AYNI reçete: bu ikisi ekranda aynı yerde,
            // sırayla görünüyor (can varken OYNA, yokken reklam). İki farklı
            // düğme stilinin yan yana en çok göze battığı yer burasıydı.
            var body = MenuPage.PillBody("WatchAd", _adRow.transform, PlayPurple,
                                         out _, out _);
            UiKit.Place(body, 0f, 0f, 1f, 1f);
            var button = body.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            body.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();

            var face = body.Find("Face");
            var title = UiKit.CreateTitle("Title", face, "WATCH AD", 34, CoinInk,
                new Color(0.16f, 0.06f, 0.30f));
            UiKit.Place(title, 0.06f, 0.44f, 0.94f, 0.94f);

            _adOfferSub = UiKit.CreateLabel("Sub", face, "Earn +1 life", 24,
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

        /// <summary>
        /// Üst bar, oyna düğmesi ve avatar baş harfi. Dışarıdan da çağrılabilir:
        /// Profil'de ad değişince ana ekran zaten açık olmadığı için OnEnable
        /// beklemek yetmiyor (bkz. <see cref="ProfileScreen"/>).
        /// </summary>
        public void Refresh()
        {
            if (!MetaServices.Ready)
            {
                if (_levelLabel != null) _levelLabel.text = "PLAY";
                return;
            }

            var lives = MetaServices.Lives;
            var progress = MetaServices.Progress;

            if (progress.Coins != _shownCoins)
            {
                _shownCoins = progress.Coins;
                // Mağazayla AYNI biçim: binlik ayıracı boşluk ("1 720").
                // Değer yalnız değiştiğinde yazıldığı için tahsis kare başına
                // değil, olay başına.
                _coinLabel.text = MenuPage.Amount(_shownCoins);
            }

            // Sınırsız can hakkı varsa sayaç yerini ∞'a ve KALAN SÜREYE bırakır.
            // Süre her saniye değiştiği için buradaki dal önbelleğe takılmıyor.
            bool infinite = progress.HasInfiniteLives;
            if (_livesInfinity != null) _livesInfinity.enabled = infinite;
            _livesLabel.enabled = !infinite;

            if (infinite)
            {
                var left = progress.InfiniteLivesLeft;
                _livesTimer.text = left.TotalHours >= 1d
                    ? _scratch.Clear().Append((int)left.TotalHours).Append('h')
                        .Append(' ').Append(left.Minutes).Append('m').ToString()
                    : _scratch.Clear().Append(left.Minutes).Append('m')
                        .Append(' ').Append(left.Seconds).Append('s').ToString();
                _shownLives = -1;      // hak bitince normal sayaç yeniden yazılsın
                _shownRefill = int.MinValue;
            }
            else
            {
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
            }

            if (_avatarInitial != null)
                _avatarInitial.text = string.IsNullOrEmpty(MetaServices.PlayerName)
                    ? "?" : MetaServices.PlayerName.Substring(0, 1).ToUpperInvariant();

            int next = Mathf.Clamp(progress.HighestUnlockedIndex, 0,
                Mathf.Max(0, LevelCatalog.Count - 1));
            // Sınırsız can hakkı, can sayacı sıfır olsa bile oynatır — hakkın
            // tamamı zaten "can derdi olmasın" demek. Bu satır atlanınca paketi
            // alan oyuncu 0 canla "Oyna"ya basamıyordu.
            bool canPlay = (lives.HasLife || progress.HasInfiniteLives) && LevelCatalog.Count > 0;

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

            // CANSIZ DURUM GERİ ALINABİLİR OLMALI.
            //
            // BULUNAN HATA (kullanıcı: "reklam izledikten sonra canım olmasına
            // rağmen butonda can yok yazıyor"): Bu blok düğmeyi griye boyayıp
            // yazısını değiştiriyordu ama TERSİNİ yapan hiçbir satır yoktu.
            // Yazıyı geri koyan tek yer `ApplyDifficulty` ve o da yalnız
            // BÖLÜM DEĞİŞTİĞİNDE çağrılıyor (`next != _shownNext`). Reklam
            // izlenip can alındığında bölüm aynı kaldığı için düğme "CAN YOK"
            // yazmaya ve gri kalmaya devam ediyordu — üstelik `interactable`
            // true olduğu için BASILABİLİYORDU. Yani ekran yalan söylüyordu.
            //
            // DERS (bir durumu boyayan her satırın geri dönüşü de olmalı):
            // "Şu koşulda şunu yaz" biçiminde yazılmış her görünüm kuralı,
            // koşul kalktığında ne olacağını da söylemek zorunda. Tazeleme
            // fonksiyonları idempotent görünür ama tek yönlü yazılırsa
            // değildir; hata ancak koşul GERİ DÖNDÜĞÜNDE ortaya çıkar ve
            // testte kolayca atlanır.
            //
            // Ayrıca yazı TÜRKÇEYDİ. Arayüz 2026-08-10'da tamamen İngilizceye
            // geçti; bu satır o taramada gözden kaçmış ve İngilizce bir
            // düğmenin ortasında tek başına duruyordu.
            if (!canPlay)
            {
                _levelLabel.text = "NO LIVES";
                _difficultyLabel.gameObject.SetActive(true);
                _difficultyLabel.text = "waiting for lives";
                if (_playPill != null) _playPill.Color = new Color(0.62f, 0.62f, 0.66f);
                _playBlocked = true;
            }
            else if (_playBlocked)
            {
                // Can geri geldi: düğmenin yazısını, zorluğunu ve görselini
                // bölüm hiç değişmemiş olsa bile yeniden kur.
                _playBlocked = false;
                ApplyDifficulty(LevelCatalog.DifficultyAt(next), next);
            }
        }

        /// <summary>
        /// OYNA düğmesi şu an "can yok" görünümünde mi? Can geri geldiğinde
        /// görünümü yeniden kurmak için gerekiyor (bkz. <see cref="Refresh"/>).
        /// </summary>
        bool _playBlocked;

        /// <summary>ÖLÇÜM: `ana ekran.jpeg`, "Seviye 54" düğmesinin yüzü (133,28,252).</summary>
        static readonly Color PlayPurple = new Color(0.522f, 0.110f, 0.988f);

        void ApplyDifficulty(LevelDifficulty difficulty, int index)
        {
            _levelLabel.text = _scratch.Clear().Append("Level ").Append(index + 1).ToString();

            string label = LevelDifficultyRule.Label(difficulty);
            _difficultyLabel.text = label;
            _difficultyLabel.gameObject.SetActive(!string.IsNullOrEmpty(label));

            // Zorluk arttıkça düğmenin rengi değişir: yeşil → mor → kırmızı.
            //
            // Eskiden üç ayrı PNG arasında geçiş yapılıyordu; ışık profili
            // artık çarpanla türetildiği için tek bir renk yetiyor.
            // Mor, referansın ana ekran düğmesinden ölçüldü: (133,28,252).
            if (_playPill != null)
                _playPill.Color = difficulty == LevelDifficulty.SuperHard ? MenuPage.Red
                                : difficulty == LevelDifficulty.Hard ? PlayPurple
                                : MenuPage.Green;

            int multiplier = LevelDifficultyRule.RewardMultiplier(difficulty);
            bool showRibbon = multiplier > 1;
            _rewardRibbon.gameObject.SetActive(showRibbon);
            // "x3" ÇARPANI DAHA KÜÇÜK. Referansta "Ödüller" büyük, "x3" onun
            // yaklaşık dörtte üçü kadar — okuyan önce NE olduğunu, sonra KAÇ
            // katı olduğunu görüyor. Tek puntoda yazınca şerit tek bir uzun
            // kelime gibi okunuyordu.
            if (showRibbon)
                _rewardLabel.text = _scratch.Clear()
                    .Append("Rewards <size=76%>x").Append(multiplier).Append("</size>")
                    .ToString();

            // Yazı yerleşimi içeriğe göre. Zorluk etiketi yoksa (normal bölüm)
            // seviye yazısı düğmenin ORTASINA oturur; alt satır boş kaldığında
            // tek satırın yukarıda asılı durması dengesiz görünüyordu.
            bool hasDifficulty = !string.IsNullOrEmpty(label);
            float bottom = hasDifficulty ? LevelBottomWithDifficulty : LevelBottomAlone;
            UiKit.Place(_levelLabel, 0.05f, bottom, 0.95f, LevelTop);
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
