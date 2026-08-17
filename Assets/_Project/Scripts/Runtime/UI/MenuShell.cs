using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Menü kabuğu: alt sekme çubuğu ve onun açtığı ekranlar (Mağaza,
    /// Liderlik Panosu, Yolculuk, Profil). "Ana Ekran" sekmesi hiçbir panel
    /// göstermez — altındaki <see cref="HomeScreen"/> görünür kalır.
    ///
    /// DERS (kabuk / içerik ayrımı): Sekme çubuğu, üst bar ve geçiş mantığı
    /// TEK yerde durur; her ekran yalnız kendi içeriğini kurar. Beş ekranın
    /// her birine ayrı bir "geri düğmesi + sekme çubuğu" kopyalamak, altıncı
    /// ekran eklendiğinde altı yerde düzeltme demekti.
    ///
    /// DERS (ayrı canvas, yüksek sıra): Kabuk kendi canvas'ını kurar ve
    /// HomeScreen'in üstünde çizilir. Böylece ana ekranın koduna hiç
    /// dokunmadan menü sistemi eklenebiliyor — mevcut çalışan bir ekranı
    /// yeniden yazmak, çalışmayan bir ekran riski demektir.
    /// </summary>
    public sealed class MenuShell : MonoBehaviour
    {
        static readonly (string label, string key, string icon)[] Tabs =
        {
            ("Shop",       "store",   Art.Shop),
            ("Leaderboard","board",   Art.Trophy),
            ("Home",       "home",    Art.Home),
            ("Journey",    "journey", Art.Globe),
            ("Collection", "collection", Art.Chest)
        };

        readonly Dictionary<string, RectTransform> _screens = new Dictionary<string, RectTransform>();
        readonly List<(Button button, Image card, RectTransform icon, TextMeshProUGUI label, string key)>
            _tabButtons = new List<(Button, Image, RectTransform, TextMeshProUGUI, string)>();

        string _active = "home";

        /// <summary>
        /// Sekme görselleri (kart, yazı, ikon konumu) en az bir kez uygulandı mı?
        ///
        /// DERS (erken çıkış İLK ÇALIŞMAYI atlamamalı): 8. bulgunun düzeltmesi
        /// "aynı sekmeye basmak hiçbir şey yapmasın" diye <see cref="Show"/>
        /// başına bir koruma koydu. Ama `_active` zaten "home" ile başlıyor ve
        /// kurulum `Show("home")` ile bitiyor — yani o çağrı da korumaya takıldı
        /// ve sekme görselleri HİÇ uygulanmadı. Sonuç: beş sekmenin de kartı ve
        /// yazısı açık kaldı, hepsi seçiliymiş gibi göründü.
        /// "Durum değişmediyse çık" koruması, durumun bir kez UYGULANDIĞINI da
        /// bilmek zorunda; yoksa "değişmedi" ile "hiç yazılmadı" karışır.
        /// </summary>
        bool _applied;
        RectTransform _content;

        /// <summary>Referanstan ölçüldü: geçiş ~170 ms (30 kare/sn'de ~5 kare).</summary>
        const float SlideSeconds = 0.17f;

        /// <summary>Ana ekrandaki dişli düğmesi buraya bağlanır.</summary>
        public static MenuShell Instance { get; private set; }

        void Awake() => Instance = this;

        void Start()
        {
            var canvas = UiKit.CreateCanvas("MenuCanvas");
            // Kanvası sahibinin altına al: menü kökü kapatılınca ekrandan da gitsin.
            canvas.transform.SetParent(transform, worldPositionStays: false);
            canvas.sortingOrder = 10;                 // ana ekranın üstünde
            var root = UiKit.CreateSafeArea(canvas);

            // İçerik alanı: sekme çubuğunun üstünde kalan her şey.
            var content = UiKit.CreateRect("Content", root);
            UiKit.Place(content, 0f, 0.105f, 1f, 1f);
            _content = content;

            _screens["store"]   = StoreScreen.Build(content);
            _screens["board"]   = LeaderboardScreen.Build(content);
            _screens["journey"] = JourneyScreen.Build(content);
            _screens["profile"] = ProfileScreen.Build(content);
            _screens["collection"] = CollectionScreen.Build(content);
            _screens["settings"] = (RectTransform)SettingsScreen.Build(content).transform;

            BuildTabBar(root);

            // Ayarlar ve Profil ÖRTÜ sayfalarıdır: referansta tam ekran ve
            // sekme çubuğunu da kapatıyorlar (kapanışları sağ üstteki kırmızı
            // çarpı). Kardeş sırası çizim sırası olduğu için çubuktan SONRAYA
            // alınıyorlar; yoksa çubuk sayfanın üstünde kalır ve "tam ekran"
            // hissi bozulur.
            _screens["settings"].SetAsLastSibling();
            _screens["profile"].SetAsLastSibling();

            Show("home");
        }

        /// <summary>
        /// Alt sekme çubuğu.
        ///
        /// DERS (seçili sekme nasıl anlaşılır): İlk hâlde beş sekmenin de altında
        /// yazı vardı ve seçili olan yalnız biraz büyüyordu — beş etiket yan yana
        /// çubuğu kalabalıklaştırıyor, hangisinin seçili olduğu da zayıf kalıyordu.
        /// Referans oyun tek bir şey yapıyor: SEÇİLİ sekme çubuğun üstüne çıkan
        /// kendi kartına oturuyor ve YAZI YALNIZ ONDA görünüyor. Diğerleri sade
        /// ikon. Böylece hem çubuk sakinleşiyor hem de seçim tek bakışta okunuyor
        /// — üstelik renk değil KONUM ve YÜKSEKLİK farkıyla, yani renk körlüğünde
        /// de çalışır.
        /// </summary>
        void BuildTabBar(Transform root)
        {
            // Çubuk güvenli alanda kalır (düğmeler parmakla erişilebilir olmalı)
            // ama BOYASI aşağı taşar: güvenli alan ekranın altından içeri
            // girdiği için zemin orada bitiyor ve altında manzara görünüyordu.
            // Kanvasa taşımayı denedim; kanvasın çocuğu olarak güvenli alandan
            // SONRA çizilip düğmelerin üstünü kapattı.
            // Çubuk ve seçili kart artık kendi görselleri. Önce hazır bir kart
            // görseli boyanmıştı — o görselin pişmiş magenta taban bandı mora
            // boyanınca çubuğun altında koyu bir gölge şeridine dönüşüyordu.
            // Sonra prosedürel düz renk denendi; temizdi ama 3B plastik dilini
            // tutturamıyordu. Kendi görseli olan bir yüzey ikisini de çözüyor.
            var barSprite = UiSkin.Get(Art.TabBar);
            var bar = barSprite != null
                ? UiKit.CreateSlicedPanel("TabBar", root, barSprite)
                : UiKit.CreateRoundedPanel("TabBar", root, BarColor);
            UiKit.Place(bar, 0f, 0f, 1f, 0.086f);
            bar.rectTransform.offsetMin = new Vector2(0f, -220f);

            // Seçili kartın taşacağı alan çubuğun üstünde; bu yüzden kartlar
            // çubuğun DEĞİL kökün çocuğu, yoksa çubuk onları kırpar.
            float slot = 1f / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                var (label, key, icon) = Tabs[i];

                var button = UiKit.CreateSpriteButton($"Tab_{key}", root, null,
                    null, 0, UiKit.Ink);
                UiKit.Place(button, i * slot, 0f, (i + 1) * slot, 0.082f);

                // Seçiliyken görünen kart: normalde saydam.
                // Kart, çubuktan AÇIK bir tonda: koyu zemin üstünde koyu bir
                // kart seçimi göstermiyordu.
                var cardSprite = UiSkin.Get(Art.TabCard);
                var card = cardSprite != null
                    ? UiKit.CreateSlicedPanel("Card", button.transform, cardSprite)
                    : UiKit.CreateRoundedPanel("Card", button.transform, CardColor);
                UiKit.Place(card, 0.05f, 0.06f, 0.95f, 1.56f);

                // Görünmez ama dokunulabilir yüzey: sekmenin tamamı tıklanabilsin.
                if (button.targetGraphic is Image face) face.color = new Color(1f, 1f, 1f, 0f);

                var glyph = UiKit.CreateIcon("Icon", button.transform, UiSkin.Get(icon));

                var caption = UiKit.CreateLabel("Label", button.transform, label, 24, UiKit.Ink);

                string captured = key;
                button.onClick.AddListener(() => Show(captured));
                _tabButtons.Add((button, card, glyph.rectTransform, caption, key));
            }
        }

        // Referans karesinden örneklenen iki ton: çubuk koyu mor-lacivert,
        // seçili kart ondan belirgin AÇIK bir mor.
        // DERS (koyu zemin ikonu YALNIZ BIRAKIR): Çubuk fazla koyuydu ve
        // parlak 3B ikonlar onun üstünde oturmuyor, boşlukta yüzüyor gibi
        // duruyordu. Kontrast ne kadar sertse eleman o kadar "yapıştırılmış"
        // görünür. Çubuk ikonların tonuna yaklaştırıldı ve üst kenarına ince
        // bir ışık şeridi kondu — o çizgi, çubuğu bir YÜZEY yapan şey.
        static readonly Color BarColor  = new Color(0.318f, 0.243f, 0.612f);
        static readonly Color CardColor = new Color(0.404f, 0.278f, 0.831f);
        static readonly Color RimColor  = new Color(0.478f, 0.396f, 0.812f);

        /// <summary>
        /// Sekmeyi değiştirir. ZATEN AÇIK olan sekmeye basmak hiçbir şey yapmaz.
        ///
        /// DERS (aynı yere iki kez basmak bir GEZİNME değildir): Burası eskiden
        /// açık sekmeye tekrar basınca oyuncuyu ANA EKRANA atıyordu. Niyet
        /// "geri tuşu gibi olsun" idi ama cihazda yaşanan şey şu: oyuncu
        /// mağazadayken mağaza sekmesine bir daha dokunuyor ve kendini ana
        /// ekranda buluyor — ekran "açılıp kapanıyor" gibi görünüyor.
        /// Bir sekme çubuğunda seçili sekme bir HEDEF'tir, bir düğme değil;
        /// zaten oradaysan gidilecek yer yok. Tek doğru karşılık dokunuşun
        /// alındığını hissettirmek: haptik.
        /// </summary>
        public void Show(string key)
        {
            if (_active == key && _applied)
            {
                if (Flow.AppRoot.Current != null)
                    Flow.AppRoot.Current.Haptics?.Play(GameKit.Services.HapticStrength.Medium);
                return;
            }

            string previous = _active;
            _applied = true;
            _active = key;

            foreach (var pair in _screens)
            {
                // Çıkan ekran hemen kapanmıyor: kayma bitince kapanacak.
                if (pair.Key == previous && previous != key) continue;
                pair.Value.gameObject.SetActive(pair.Key == key);
            }

            SlideSwap(previous, key);

            foreach (var (_, card, icon, caption, tabKey) in _tabButtons)
            {
                bool selected = tabKey == key;

                // Kart yalnız seçilide görünür ve çubuğun üstüne taşar.
                if (card != null) card.enabled = selected;

                // İkon seçiliyken kartın üst yarısına çıkar, yazıya yer açar.
                if (icon != null)
                    UiKit.Place(icon, selected ? 0.17f : 0.22f, selected ? 0.56f : 0.16f,
                                      selected ? 0.83f : 0.78f, selected ? 1.38f : 0.86f);

                if (caption != null) caption.gameObject.SetActive(selected);
                if (selected && caption != null) UiKit.Place(caption, 0f, 0.10f, 1f, 0.52f);
            }

            if (key == "journey" && _screens.TryGetValue(key, out var journey))
                journey.GetComponent<JourneyScreen>()?.Refresh();
            if (key == "profile" && _screens.TryGetValue(key, out var profile))
                profile.GetComponent<ProfileScreen>()?.Refresh();
            if (key == "collection" && _screens.TryGetValue(key, out var collection))
                collection.GetComponent<CollectionScreen>()?.Refresh();
            if (key == "settings" && _screens.TryGetValue(key, out var settings))
                settings.GetComponent<SettingsScreen>()?.Refresh();
        }

        /// <summary>
        /// Sekme geçişi: yeni ekran yandan girer, eski ekran karşı yönden çıkar.
        /// Yön SEKME SIRASINDAN gelir — sağdaki sekmeye geçerken yeni ekran
        /// sağdan girer. Referansta ölçülen süre ~170 ms.
        ///
        /// DERS (anlık değişim yön TAŞIMAZ): Eski hâl `SetActive` ile bir
        /// karede değişiyordu; oyuncu "ışınlandım" hissi alıyor ve iki sekme
        /// arasındaki komşuluğu öğrenemiyordu. Referans oyun bu yüzden
        /// kaydırıyor.
        ///
        /// SINIR: Ana ekran AYRI bir kanvasta (HomeCanvas) ve menü içeriğinin
        /// ALTINDA duruyor; bu yüzden ana ekrana geçerken yalnız menü ekranı
        /// kayıp altındakini açığa çıkarıyor, ana ekranın kendisi kaymıyor.
        /// Referansta ikisi birden kayıyor.
        /// </summary>
        void SlideSwap(string from, string to)
        {
            if (_content == null || from == to) return;

            float width = _content.rect.width;
            if (width < 1f) width = 1080f;

            float direction = TabOrder(to) >= TabOrder(from) ? 1f : -1f;

            if (_screens.TryGetValue(to, out var incoming))
                GameKit.FX.Juice.Replace(incoming,
                    GameKit.FX.Juice.SlideX(incoming, direction * width, 0f, SlideSeconds));
            else if (to == "home")
                // Ana ekranın kendi kanvası var; kaymayı kendisi yapıyor.
                HomeScreen.Instance?.Slide(direction * width, 0f, SlideSeconds);

            if (from == "home")
                HomeScreen.Instance?.Slide(0f, -direction * width, SlideSeconds);

            if (_screens.TryGetValue(from, out var outgoing))
            {
                var leaving = outgoing;
                GameKit.FX.Juice.Replace(leaving,
                    GameKit.FX.Juice.SlideX(leaving, 0f, -direction * width, SlideSeconds,
                        () =>
                        {
                            // Kapatmadan önce YERİNE geri koy: bir daha
                            // açıldığında ekran dışında kalmasın.
                            leaving.anchoredPosition =
                                new Vector2(0f, leaving.anchoredPosition.y);
                            leaving.gameObject.SetActive(false);
                        }));
            }
        }

        /// <summary>Sekme çubuğundaki sıra; listede olmayan ekranlar sona sayılır.</summary>
        static int TabOrder(string key)
        {
            for (int i = 0; i < Tabs.Length; i++)
                if (Tabs[i].key == key) return i;
            return Tabs.Length;   // profile/settings: örtü sayfaları
        }

        /// <summary>Ekranların ortak başlık şeridi.</summary>
        public static TextMeshProUGUI Header(Transform parent, string title)
        {
            var bar = UiKit.CreateSlicedPanel("Header", parent, UiSkin.Get(Art.PanelDark));
            UiKit.Place(bar, 0.04f, 0.925f, 0.96f, 0.99f);
            var label = UiKit.CreateTitle("Title", bar.transform, title, 52, UiKit.Ink, UiKit.PanelDark);
            UiKit.Place(label, 0f, 0f, 1f, 1f);
            return label;
        }

        /// <summary>Ekranların ortak arka planı + kök dikdörtgeni.</summary>
        public static RectTransform Screen(Transform parent, string name)
        {
            var root = UiKit.CreateRect(name, parent);
            UiKit.Place(root, 0f, 0f, 1f, 1f);

            // Menü zemini burada da görünsün, üstüne okunurluk için koyu bir
            // perde çekilsin: manzara tamamen kaybolursa ekranlar arası geçiş
            // "başka bir oyuna girdim" hissi veriyor.
            UiKit.CreateCover("Bg", root, UiSkin.Get(Art.MenuBack), UiKit.Background);
            UiKit.CreatePanel("Scrim", root, new Color(0.09f, 0.06f, 0.20f, 0.88f));
            return root;
        }
    }
}
