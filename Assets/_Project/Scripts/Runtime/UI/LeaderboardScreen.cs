using System.Collections.Generic;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Liderlik Panosu: Haftalık / Dünya / Ülke sekmeleri, üç kişilik podyum ve
    /// sıralı satırlar.
    ///
    /// DERS (sunucu gelmeden ekranı bitir): Gerçek sıralama bir arka uç ister.
    /// Ekran şimdilik YEREL bir liste üzerinden çalışıyor — oyuncunun kendi
    /// seviyesi listeye giriyor, gerisi sahte rakip. Böylece yerleşim, yazı
    /// boyutları ve taşma davranışı BUGÜN test edilebiliyor; sunucu geldiğinde
    /// yalnız veri kaynağı değişecek. Boş bir ekranla beklemek, sunucu geldiği
    /// gün hem veriyi hem tasarımı aynı anda hata ayıklamak demektir.
    /// </summary>
    public sealed class LeaderboardScreen : MonoBehaviour
    {
        static readonly string[] TabNames = { "Haftalık", "Dünya", "Ülke" };

        static readonly (string name, int score, int level)[] Rivals =
        {
            ("aisha",        980, 62),
            ("Bet",          910, 58),
            ("zzz",          860, 55),
            ("dotsang",      800, 51),
            ("player_2u1hw", 760, 47),
            ("kret",         700, 44),
            ("mira",         640, 39),
            ("polat",        580, 33)
        };

        readonly List<(Button button, Image face, int index)> _tabs =
            new List<(Button, Image, int)>();
        readonly List<TextMeshProUGUI> _rowLabels = new List<TextMeshProUGUI>();
        int _activeTab;

        public static RectTransform Build(Transform parent)
        {
            var root = MenuShell.Screen(parent, "LeaderboardScreen");
            var screen = root.gameObject.AddComponent<LeaderboardScreen>();
            MenuShell.Header(root, "Liderlik Panosu");

            // Sekmeler
            for (int i = 0; i < TabNames.Length; i++)
            {
                float x0 = 0.05f + i * 0.31f;
                var button = UiKit.CreateButton($"Tab_{i}", root, TabNames[i], 28,
                    i == 0 ? UiKit.Accent : UiKit.Panel, UiKit.Ink);
                UiKit.Place(button, x0, 0.855f, x0 + 0.29f, 0.915f);

                int captured = i;
                button.onClick.AddListener(() => screen.SelectTab(captured));
                screen._tabs.Add((button, button.targetGraphic as Image, i));
            }

            // Podyum: 2. - 1. - 3. sırayla, ortadaki daha yüksek.
            float[] podiumHeight = { 0.14f, 0.19f, 0.12f };
            string[] podiumRank = { "2", "1", "3" };
            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.08f + i * 0.29f;
                var card = UiKit.CreateRoundedPanel($"Podium_{i}", root, UiKit.PanelDark);
                UiKit.Place(card, x0, 0.66f, x0 + 0.26f, 0.66f + podiumHeight[i]);

                var label = UiKit.CreateTitle($"P{i}", card.transform, podiumRank[i], 44, UiKit.Coin, UiKit.PanelDark);
                UiKit.Place(label, 0f, 0.45f, 1f, 0.95f);

                var who = UiKit.CreateLabel($"Who{i}", card.transform, "", 22, UiKit.Ink);
                UiKit.Place(who, 0.05f, 0.05f, 0.95f, 0.45f);
                screen._rowLabels.Add(who);
            }

            // Sıralı satırlar
            for (int i = 0; i < 7; i++)
            {
                float y1 = 0.63f - i * 0.085f, y0 = y1 - 0.072f;
                var row = UiKit.CreateRoundedPanel($"Row_{i}", root, UiKit.PanelDark);
                UiKit.Place(row, 0.05f, y0, 0.95f, y1);

                var label = UiKit.CreateLabel($"L{i}", row.transform, "", 26, UiKit.Ink);
                UiKit.Place(label, 0.04f, 0f, 0.96f, 1f);
                screen._rowLabels.Add(label);
            }

            screen._built = true;
            return root;
        }

        // AddComponent, Build() alanları doldurmadan ÖNCE OnEnable'ı tetikler;
        // bu bayrak olmadan ilk tazeleme null referansa çarpıyor.
        bool _built;

        void OnEnable() => Refresh();

        void SelectTab(int index)
        {
            _activeTab = index;
            foreach (var (_, face, i) in _tabs)
                if (face != null) face.color = i == index ? UiKit.Accent : UiKit.Panel;
            Refresh();
        }

        void Refresh()
        {
            if (!_built) return;

            // Oyuncu kendi seviyesiyle listeye katılır; sekme yalnız puanı ölçekler
            // (haftalık < ülke < dünya) — sunucu gelene kadar yerleşimi denemek için.
            int myLevel = MetaServices.Ready ? MetaServices.Progress.HighestUnlockedIndex + 1 : 1;
            float scale = _activeTab == 0 ? 1f : _activeTab == 1 ? 1.6f : 1.2f;

            var rows = new List<(string name, int score, int level)>();
            foreach (var r in Rivals)
                rows.Add((r.name, Mathf.RoundToInt(r.score * scale), r.level));
            rows.Add(("Sen", Mathf.RoundToInt(myLevel * 18f * scale), myLevel));
            rows.Sort((a, b) => b.score.CompareTo(a.score));

            for (int i = 0; i < _rowLabels.Count && i < rows.Count; i++)
            {
                var (name, score, level) = rows[i];
                bool podium = i < 3;

                // Podyum sırası ekranda 2-1-3 dizildiği için ilk üçü yeniden eşle.
                int slot = podium ? (i == 0 ? 1 : i == 1 ? 0 : 2) : i;
                var label = _rowLabels[slot];

                label.text = podium
                    ? $"{name}\n{score}"
                    : $"{i + 1}.  {name}          {score} puan   ·   sv {level}";
                label.color = name == "Sen" ? UiKit.Accent : UiKit.Ink;
            }
        }
    }
}
