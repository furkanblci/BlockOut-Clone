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
            UiKit.Place(group, 0.03f, 0.248f, 0.97f, 0.571f);

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
            const float top = 0.978f, bottom = 0.932f;   // kapsül şeridi (dikeyde inceltildi)

            BuildAvatar(root, 0.040f, 0.174f, bottom - 0.012f, top + 0.012f);

            // DERS (kapsül bir ZEMİN, bir nesne değil): Panel görselinin kendi
            // 3B kabartması ve parlaklık lekesi var; kapsül olarak gerildiğinde
            // o kabartma bozuluyor ve göz kapsülü ikonlardan önce görüyordu.
            // Kaynak şeridinin işi ikonları TAŞIMAK, dikkat çekmek değil.
            // Prosedürel yuvarlak panel + düşük opaklık: kabartma yok, ezilme
            // yok, ikonlar öne çıkıyor. Dikeyde de inceldi.
            var pill = UiKit.CreateRoundedPanel("ResourcePill", root,
                new Color(0.055f, 0.035f, 0.145f, 0.58f));
            UiKit.Place(pill, 0.205f, bottom, 0.815f, top);
            pill.pixelsPerUnitMultiplier = 0.16f;      // uçları tam yuvarlak

            // Bölümler referans karesinden ölçüldü (kapsül 274x62 kırpma):
            // jeton bölümü %0-42, can bölümü %42-100.
            _coinLabel = Section(pill.transform, Art.Coin, 0.010f, 0.420f,
                iconHeight: 0.62f, out var coinPlus, out _);
            coinPlus.onClick.AddListener(() => MenuShell.Instance?.Show("store"));

            _livesLabel = Section(pill.transform, Art.Heart, 0.430f, 0.990f,
                iconHeight: 0.78f, out var lifePlus, out _livesTimer);
            lifePlus.onClick.AddListener(() => MenuShell.Instance?.Show("store"));

            var gear = SquareButton(root, "Gear", Art.Gear, 0.846f, 0.960f, bottom, top);
            gear.onClick.AddListener(() => MenuShell.Instance?.Show("settings"));
        }

        /// <summary>
        /// Kapsülün bir bölümü: ikon · sayı · (durum) · küçük artı düğmesi.
        ///
        /// DERS (ölçüyü REFERANSTAN al, gözden değil): Bu şerit üç kez elden
        /// geçti ve her seferinde "yaklaşık doğru" kaldı. Sorun ölçülerin göz
        /// kararı olmasıydı. Referans kırpması 274x62 piksel; oradan okunan
        /// oranlar: jeton ikonu kapsül yüksekliğinin %55'i, kalp %65'i, artı
        /// düğmesi %40'ı, ve durum yazısı ("Dolu") sayının ALTINDA değil
        /// YANINDA. Benim sürümümde artı düğmeleri ikonlar kadar büyüktü ve
        /// göz önce onları görüyordu; sayaç şeridinde en büyük eleman içeriğin
        /// kendisi olmalı, düğmesi değil.
        /// </summary>
        TextMeshProUGUI Section(Transform pill, string icon, float x0, float x1,
            float iconHeight, out Button plus, out TextMeshProUGUI status)
        {
            float width = x1 - x0;
            float top = 0.5f + iconHeight * 0.5f;
            float bottom = 0.5f - iconHeight * 0.5f;

            var badge = UiKit.CreateIcon($"Icon_{icon}", pill, UiSkin.Get(icon));
            UiKit.Place(badge, x0, bottom, x0 + width * 0.175f, top);

            bool hasStatus = icon == Art.Heart;

            // Sayı ve durum yan yana: durumu alta koymak kapsülü kalınlaştırıyor
            // ve referansta da öyle değil.
            float numberEnd = hasStatus ? 0.44f : 0.70f;
            var label = UiKit.CreateTitle($"Value_{icon}", pill, "", 34, CoinInk,
                new Color(0.10f, 0.07f, 0.24f));
            UiKit.Place(label, x0 + width * 0.185f, 0.10f, x0 + width * numberEnd, 0.90f);

            status = null;
            if (hasStatus)
            {
                status = UiKit.CreateLabel($"Status_{icon}", pill, "", 26, CoinInk);
                UiKit.Place(status, x0 + width * 0.46f, 0.12f, x0 + width * 0.78f, 0.88f);
            }

            // Artı: KÜÇÜK ve kare. En-boy korunduğu için yükseklik belirleyici.
            // Referansta artı, kapsül yüksekliğinin ~%40'ı — jetondan ve
            // kalpten belirgin biçimde küçük. Aynı boyda olunca göz önce
            // düğmeyi görüyor, oysa şeridin konusu sayının kendisi.
            plus = UiKit.CreateIconButton($"Plus_{icon}", pill, UiSkin.Get(Art.Plus));
            UiKit.Place(plus, x0 + width * 0.815f, 0.26f, x0 + width * 0.995f, 0.74f);

            return label;
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
            back.pixelsPerUnitMultiplier = 0.35f;

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

        void BuildPlayButton(Transform root)
        {
            _playButton = UiKit.CreateSpriteButton("Play", root, UiSkin.Get(Art.ButtonGreen),
                null, 0, CoinInk);
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
            _playFace = _playButton.targetGraphic as Image;

            var face = _playButton.transform.GetChild(0);

            _levelLabel = UiKit.CreateTitle("Level", face, "", 44, CoinInk,
                new Color(0.10f, 0.06f, 0.22f));
            UiKit.Place(_levelLabel, 0.06f, 0.44f, 0.94f, 0.94f);

            // DERS (yazı butonun YÜZÜNE oturur, kenarına değil): "Zor Seviye"
            // düğmenin alt %34'lük 3B kenarına denk geliyordu ve gölgenin
            // üstünde yüzüyormuş gibi duruyordu — "model yok" hissi oradan
            // geliyor. btn_purple'ın alt payı 68px, yani 202px yükseklikte
            // alt %34 kenardır; iki yazı da bunun üstünde kalmalı.
            _difficultyLabel = UiKit.CreateTitle("Difficulty", face, "", 24,
                new Color(0.86f, 0.88f, 1f), new Color(0.10f, 0.06f, 0.22f));
            UiKit.Place(_difficultyLabel, 0.08f, 0.36f, 0.92f, 0.58f);

            // Ödül şeridi düğmenin ÜST KENARINA binerek durur; ayrı bir kutu
            // gibi değil, düğmeye takılmış bir etiket gibi okunsun.
            // DERS (etiket DÜZ olmalı): Bu şerit iki kez yanlış yapıldı.
            // Önce kendi sprite'ı üretildi ama gölgesi kesimde kırpılınca
            // havada duran yarım gölgeli bir etikete döndü. Sonra krem kart
            // turuncuya boyandı — kartın kendi ALT BANDI da boyanınca altta
            // kırmızı bir şerit çıktı ve etiket iki renkli bir pastile döndü.
            // Doğrusu en sade olan: düz renkli, uçları yuvarlak, tek katmanlı
            // bir etiket. Küçük bir rozette 3B kabartma zaten okunmuyor.
            var ribbon = UiKit.CreateRoundedPanel("Ribbon", _playButton.transform,
                new Color(0.96f, 0.53f, 0.05f));
            ribbon.pixelsPerUnitMultiplier = 0.16f;
            UiKit.Place(ribbon, 0.30f, 0.84f, 0.70f, 1.20f);
            _rewardRibbon = ribbon.rectTransform;

            _rewardLabel = UiKit.CreateTitle("RibbonText", ribbon.transform, "", 22,
                CoinInk, new Color(0.50f, 0.24f, 0.02f));
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
            UiKit.Place((RectTransform)_adRow.transform, 0.255f, 0.158f, 0.745f, 0.263f);

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
            float top = showRibbon ? 0.94f : 0.98f;
            float bottom = hasDifficulty ? 0.58f : 0.40f;
            UiKit.Place(_levelLabel, 0.08f, bottom, 0.92f, top);
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
