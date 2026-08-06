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
            ("Mağaza",    "store",   Art.Shop),
            ("Liderlik",  "board",   Art.Trophy),
            ("Ana Ekran", "home",    Art.Home),
            ("Yolculuk",  "journey", Art.Globe),
            ("Koleksiyon", "collection", Art.Chest)
        };

        readonly Dictionary<string, RectTransform> _screens = new Dictionary<string, RectTransform>();
        readonly List<(Button button, Image card, RectTransform icon, TextMeshProUGUI label, string key)>
            _tabButtons = new List<(Button, Image, RectTransform, TextMeshProUGUI, string)>();

        string _active = "home";

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

            _screens["store"]   = StoreScreen.Build(content);
            _screens["board"]   = LeaderboardScreen.Build(content);
            _screens["journey"] = JourneyScreen.Build(content);
            _screens["profile"] = ProfileScreen.Build(content);
            _screens["collection"] = CollectionScreen.Build(content);
            _screens["settings"] = (RectTransform)SettingsScreen.Build(content).transform;

            BuildTabBar(root);
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
            // DERS (boyama CARPMADIR — çubukta da geçerli): Çubuk koyu lacivert
            // panel görselinden yapılıyordu ve üstündeki parlak ikonlarla
            // uyuşmuyordu; ikonlar boşlukta yüzüyor gibi duruyordu. Krem kart
            // görseli boyanınca gerçek rengi veriyor: referanstaki mor-lacivert
            // çubuk ve ondan AÇIK bir seçili kart. İkonlar artık aynı ailedeki
            // bir yüzeyin üstünde duruyor.
            var bar = UiKit.CreateSlicedPanel("TabBar", root, UiSkin.Get(Art.PanelCard), BarColor);
            UiKit.Place(bar, 0f, 0f, 1f, 0.082f);
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
                var card = UiKit.CreateSlicedPanel("Card", button.transform,
                    UiSkin.Get(Art.PanelCard), CardColor);
                UiKit.Place(card, 0.06f, 0.05f, 0.94f, 1.66f);

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
        static readonly Color BarColor  = new Color(0.235f, 0.176f, 0.470f);
        static readonly Color CardColor = new Color(0.427f, 0.322f, 0.812f);

        /// <summary>Sekmeyi değiştirir; aynı sekmeye basmak ana ekrana döner.</summary>
        public void Show(string key)
        {
            if (_active == key && key != "home") key = "home";
            _active = key;

            foreach (var pair in _screens)
                pair.Value.gameObject.SetActive(pair.Key == key);

            foreach (var (_, card, icon, caption, tabKey) in _tabButtons)
            {
                bool selected = tabKey == key;

                // Kart yalnız seçilide görünür ve çubuğun üstüne taşar.
                if (card != null) card.enabled = selected;

                // İkon seçiliyken kartın üst yarısına çıkar, yazıya yer açar.
                if (icon != null)
                    UiKit.Place(icon, selected ? 0.16f : 0.22f, selected ? 0.58f : 0.16f,
                                      selected ? 0.84f : 0.78f, selected ? 1.50f : 0.86f);

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
