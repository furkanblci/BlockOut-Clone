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
            ("Profil",    "profile", Art.Star)
        };

        readonly Dictionary<string, RectTransform> _screens = new Dictionary<string, RectTransform>();
        readonly List<(Button button, RectTransform icon, TextMeshProUGUI label, string key)> _tabButtons =
            new List<(Button, RectTransform, TextMeshProUGUI, string)>();

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
            _screens["settings"] = (RectTransform)SettingsScreen.Build(content).transform;

            BuildTabBar(root);
            Show("home");
        }

        /// <summary>
        /// Alt sekme çubuğu: her sekme bir ikon + altında etiketi.
        ///
        /// DERS (seçili sekme nasıl anlaşılır): Yalnız rengi değiştirmek mobilde
        /// zayıf bir işaret — güneşte ya da renk körlüğünde okunmaz. Referans
        /// oyun seçili sekmenin ikonunu BÜYÜTÜR. Boyut farkı renkten bağımsız
        /// çalışır; ikisini birlikte kullanmak en sağlamı.
        /// </summary>
        void BuildTabBar(Transform root)
        {
            var bar = UiKit.CreateSlicedPanel("TabBar", root, UiSkin.Get(Art.PanelDark));
            UiKit.Place(bar, 0f, 0f, 1f, 0.105f);

            float slot = 1f / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                var (label, key, icon) = Tabs[i];

                var button = UiKit.CreateSpriteButton($"Tab_{key}", bar.transform, null,
                    null, 0, UiKit.Ink);
                UiKit.Place(button, i * slot, 0f, (i + 1) * slot, 1f);

                // Görünmez ama dokunulabilir yüzey: sekmenin tamamı tıklanabilsin.
                if (button.targetGraphic is Image face) face.color = new Color(1f, 1f, 1f, 0f);

                var glyph = UiKit.CreateIcon("Icon", button.transform, UiSkin.Get(icon));
                UiKit.Place(glyph, 0.20f, 0.34f, 0.80f, 0.94f);

                var caption = UiKit.CreateLabel("Label", button.transform, label, 24,
                    new Color(1f, 1f, 1f, 0.7f));
                UiKit.Place(caption, 0f, 0.12f, 1f, 0.34f);

                string captured = key;
                button.onClick.AddListener(() => Show(captured));
                _tabButtons.Add((button, glyph.rectTransform, caption, key));
            }
        }

        /// <summary>Sekmeyi değiştirir; aynı sekmeye basmak ana ekrana döner.</summary>
        public void Show(string key)
        {
            if (_active == key && key != "home") key = "home";
            _active = key;

            foreach (var pair in _screens)
                pair.Value.gameObject.SetActive(pair.Key == key);

            foreach (var (_, icon, caption, tabKey) in _tabButtons)
            {
                bool selected = tabKey == key;
                if (icon != null) icon.localScale = Vector3.one * (selected ? 1.18f : 0.92f);
                if (caption != null)
                    caption.color = selected ? UiKit.Ink : new Color(1f, 1f, 1f, 0.55f);
            }

            if (key == "journey" && _screens.TryGetValue(key, out var journey))
                journey.GetComponent<JourneyScreen>()?.Refresh();
            if (key == "profile" && _screens.TryGetValue(key, out var profile))
                profile.GetComponent<ProfileScreen>()?.Refresh();
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
