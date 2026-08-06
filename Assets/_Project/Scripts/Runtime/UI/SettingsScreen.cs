using System.Collections.Generic;
using BlockOut.Runtime.Services;
using GameKit.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Ayarlar: Bildirimler / Sesler / Müzik / Haptik ve yasal bağlantılar.
    /// Referans oyunda dişli düğmesinden açılan tam ekran bir pencere.
    ///
    /// DERS (ayar = anında etki + kalıcı kayıt): Anahtar değiştiği anda hem
    /// ilgili servis kapanır hem de kayda yazılır. "Kaydet" düğmesi koymak
    /// mobilde bir hata kaynağıdır — oyuncu ayarı değiştirip geri tuşuna
    /// basar ve değişiklik kaybolur.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        readonly List<(TextMeshProUGUI label, string title, System.Func<bool> get)> _rows =
            new List<(TextMeshProUGUI, string, System.Func<bool>)>();

        public static SettingsScreen Build(Transform parent)
        {
            var root = MenuShell.Screen(parent, "SettingsScreen");
            var screen = root.gameObject.AddComponent<SettingsScreen>();
            MenuShell.Header(root, "Ayarlar");

            screen.AddToggle(root, 0, "Bildirimler",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Notifications,
                v => { if (MetaServices.Ready) { MetaServices.Save.Data.Settings.Notifications = v; MetaServices.Save.Save(); } });

            screen.AddToggle(root, 1, "Sesler",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Sounds,
                v =>
                {
                    if (!MetaServices.Ready) return;
                    MetaServices.Save.Data.Settings.Sounds = v;
                    MetaServices.Save.Save();
                    AudioService.SetMuted(!v);
                });

            screen.AddToggle(root, 2, "Müzik",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Music,
                v => { if (MetaServices.Ready) { MetaServices.Save.Data.Settings.Music = v; MetaServices.Save.Save(); } });

            screen.AddToggle(root, 3, "Haptik",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Haptics,
                v =>
                {
                    if (!MetaServices.Ready) return;
                    MetaServices.Save.Data.Settings.Haptics = v;
                    MetaServices.Save.Save();
                    // Haptik servisi oyun sahnesinde kurulur; ayar kayıttan okunur.
                });

            // Yasal / destek bağlantıları — referanstaki dört satır.
            string[] links = { "Destek", "Koşullar", "Gizlilik", "Hesabımı Sil" };
            for (int i = 0; i < links.Length; i++)
            {
                float y1 = 0.44f - i * 0.085f;
                var button = UiKit.CreateTintedButton($"Link_{i}", root,
                    UiSkin.Get(Art.PanelCard),
                    i == links.Length - 1 ? new Color(0.925f, 0.255f, 0.176f) : new Color(0.420f, 0.310f, 0.878f),
                    links[i], 28, UiKit.Ink);
                UiKit.Place(button, 0.08f, y1 - 0.07f, 0.92f, y1);
            }

            screen._built = true;
            return screen;
        }

        void AddToggle(Transform root, int index, string title,
            System.Func<bool> get, System.Action<bool> set)
        {
            float y1 = 0.86f - index * 0.095f;
            var row = UiKit.CreateSlicedPanel($"Row_{index}", root, UiSkin.Get(Art.PanelDark));
            UiKit.Place(row, 0.06f, y1 - 0.08f, 0.94f, y1);

            var name = UiKit.CreateLabel("Name", row.transform, title, 32, UiKit.Ink);
            UiKit.Place(name, 0.05f, 0f, 0.55f, 1f);
            name.alignment = TextAlignmentOptions.Left;

            var state = UiKit.CreateLabel("State", row.transform, "", 30, UiKit.Accent);
            UiKit.Place(state, 0.55f, 0f, 0.78f, 1f);

            var button = UiKit.CreateTintedButton($"Toggle_{index}", row.transform,
                UiSkin.Get(Art.PanelCard), new Color(0.420f, 0.310f, 0.878f), "Değiştir", 22, UiKit.Ink);
            UiKit.Place(button, 0.79f, 0.15f, 0.96f, 0.85f);
            button.onClick.AddListener(() => { set(!get()); Refresh(); });

            _rows.Add((state, title, get));
        }

        // AddComponent, Build() alanları doldurmadan ÖNCE OnEnable'ı tetikler;
        // bu bayrak olmadan ilk tazeleme null referansa çarpıyor.
        bool _built;

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (!_built) return;

            foreach (var (label, _, get) in _rows)
            {
                bool on = get();
                label.text = on ? "Açık" : "Kapalı";
                label.color = on ? UiKit.Accent : UiKit.Locked;
            }
        }
    }
}
