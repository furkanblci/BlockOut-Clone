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
        static readonly (string label, string key)[] Tabs =
        {
            ("Mağaza",   "store"),
            ("Liderlik", "board"),
            ("Ana Ekran", "home"),
            ("Yolculuk", "journey"),
            ("Profil",   "profile")
        };

        readonly Dictionary<string, RectTransform> _screens = new Dictionary<string, RectTransform>();
        readonly List<(Button button, Image face, string key)> _tabButtons =
            new List<(Button, Image, string)>();

        string _active = "home";

        /// <summary>Ana ekrandaki dişli düğmesi buraya bağlanır.</summary>
        public static MenuShell Instance { get; private set; }

        void Awake() => Instance = this;

        void Start()
        {
            var canvas = UiKit.CreateCanvas("MenuCanvas");
            canvas.sortingOrder = 10;                 // ana ekranın üstünde
            var root = UiKit.CreateSafeArea(canvas);

            // İçerik alanı: sekme çubuğunun üstünde kalan her şey.
            var content = UiKit.CreateRect("Content", root);
            UiKit.Place(content, 0f, 0.085f, 1f, 1f);

            _screens["store"]   = StoreScreen.Build(content);
            _screens["board"]   = LeaderboardScreen.Build(content);
            _screens["journey"] = JourneyScreen.Build(content);
            _screens["profile"] = ProfileScreen.Build(content);
            _screens["settings"] = (RectTransform)SettingsScreen.Build(content).transform;

            BuildTabBar(root);
            Show("home");
        }

        void BuildTabBar(Transform root)
        {
            var bar = UiKit.CreateRoundedPanel("TabBar", root, UiKit.PanelDark);
            UiKit.Place(bar, 0f, 0f, 1f, 0.085f);

            float slot = 1f / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                var (label, key) = Tabs[i];
                var button = UiKit.CreateButton($"Tab_{key}", bar.transform, label, 30,
                    key == "home" ? UiKit.Accent : UiKit.Panel, UiKit.Ink);
                UiKit.Place(button, i * slot + 0.008f, 0.12f, (i + 1) * slot - 0.008f, 0.88f);

                string captured = key;
                button.onClick.AddListener(() => Show(captured));

                // Düğmenin yüzü (renk değişimi için) gövdenin ikinci çocuğu.
                var face = button.targetGraphic as Image;
                _tabButtons.Add((button, face, key));
            }
        }

        /// <summary>Sekmeyi değiştirir; aynı sekmeye basmak ana ekrana döner.</summary>
        public void Show(string key)
        {
            if (_active == key && key != "home") key = "home";
            _active = key;

            foreach (var pair in _screens)
                pair.Value.gameObject.SetActive(pair.Key == key);

            foreach (var (_, face, tabKey) in _tabButtons)
                if (face != null) face.color = tabKey == key ? UiKit.Accent : UiKit.Panel;

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
            var bar = UiKit.CreateRoundedPanel("Header", parent, UiKit.PanelDark);
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
            UiKit.CreatePanel("Bg", root, UiKit.Background);
            return root;
        }
    }
}
