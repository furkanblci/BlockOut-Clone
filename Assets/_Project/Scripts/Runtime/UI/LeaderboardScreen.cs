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

        /// <summary>
        /// Oyuncunun puanı: bitirilen bölüm, yıldız ve mükemmel geçişlerden.
        /// Yalnız "kaçıncı bölümdeyim" saymak, aynı bölümü daha iyi oynamayı
        /// ödüllendirmezdi.
        /// </summary>
        static int Score(int level, Core.Save.ProgressService progress)
        {
            int score = level * 12;
            if (progress == null) return score;

            for (int i = 0; i < BlockOut.Runtime.Config.LevelCatalog.Count; i++)
            {
                var record = progress.Record(BlockOut.Runtime.Config.LevelCatalog.IdAt(i));
                if (record.Cleared) score += 8;
                if (record.Perfect) score += 14;
            }
            return score;
        }

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
        TextMeshProUGUI _rankLabel;

        public static RectTransform Build(Transform parent)
        {
            var root = MenuShell.Screen(parent, "LeaderboardScreen");
            var screen = root.gameObject.AddComponent<LeaderboardScreen>();
            MenuShell.Header(root, "Liderlik Panosu");

            screen._rankLabel = UiKit.CreateLabel("Rank", root, "", 28,
                new Color(1f, 1f, 1f, 0.85f));
            UiKit.Place(screen._rankLabel, 0.05f, 0.877f, 0.95f, 0.920f);

            // Sekmeler
            for (int i = 0; i < TabNames.Length; i++)
            {
                float x0 = 0.05f + i * 0.31f;
                var button = UiKit.CreateTintedButton($"Tab_{i}", root,
                    UiSkin.Get(Art.PanelCard), i == 0 ? new Color(0.176f, 0.800f, 0.047f) : new Color(0.420f, 0.310f, 0.878f),
                    TabNames[i], 26, UiKit.Ink);
                UiKit.Place(button, x0, 0.815f, x0 + 0.29f, 0.872f);

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
                var card = UiKit.CreateSlicedPanel($"Podium_{i}", root, UiSkin.Get(Art.PanelDark));
                UiKit.Place(card, x0, 0.625f, x0 + 0.26f, 0.625f + podiumHeight[i]);

                var label = UiKit.CreateTitle($"P{i}", card.transform, podiumRank[i], 44, UiKit.Coin, UiKit.PanelDark);
                UiKit.Place(label, 0f, 0.45f, 1f, 0.95f);

                var who = UiKit.CreateLabel($"Who{i}", card.transform, "", 22, UiKit.Ink);
                UiKit.Place(who, 0.05f, 0.05f, 0.95f, 0.45f);
                screen._rowLabels.Add(who);
            }

            // Sıralı satırlar
            for (int i = 0; i < 7; i++)
            {
                float y1 = 0.595f - i * 0.082f, y0 = y1 - 0.070f;
                var row = UiKit.CreateSlicedPanel($"Row_{i}", root, UiSkin.Get(Art.PanelDark));
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
                if (face != null)
                    face.color = i == index ? new Color(0.176f, 0.800f, 0.047f) : new Color(0.420f, 0.310f, 0.878f);
            Refresh();
        }

        void Refresh()
        {
            if (!_built) return;

            // Oyuncu kendi seviyesiyle listeye katılır; sekme yalnız puanı ölçekler
            // (haftalık < ülke < dünya) — sunucu gelene kadar yerleşimi denemek için.
            var progress = MetaServices.Ready ? MetaServices.Progress : null;
            int myLevel = progress != null ? progress.HighestUnlockedIndex + 1 : 1;
            float scale = _activeTab == 0 ? 1f : _activeTab == 1 ? 1.6f : 1.2f;

            // DERS (sahte rakip AMA gerçek yarış): Sunucu yok, rakipler sabit.
            // Ama oyuncunun puanı GERÇEK ilerlemesinden geliyor; bölüm
            // ilerledikçe rakipleri tek tek geçiyor ve sıralaması gözle görülür
            // biçimde yükseliyor. "Sahte veri" olduğu için sıralamayı rastgele
            // kımıldatmak kolay olurdu — ama o zaman oyuncunun emeği ile ekran
            // arasındaki bağ kopardı ve tablo anlamsızlaşırdı.
            int myScore = Score(myLevel, progress);

            var rows = new List<(string name, int score, int level)>();
            foreach (var r in Rivals)
                rows.Add((r.name, Mathf.RoundToInt(r.score * scale), r.level));
            rows.Add(("Sen", Mathf.RoundToInt(myScore * scale), myLevel));
            rows.Sort((a, b) => b.score.CompareTo(a.score));

            // Kaçıncı sıradayım ve bir üsttekine ne kadar kaldı.
            int myRank = rows.FindIndex(r => r.name == "Sen");
            _rankLabel.text = myRank == 0
                ? $"1. sıradasın · {myScore} puan"
                : $"{myRank + 1}. sıradasın · {rows[myRank - 1].score - rows[myRank].score} puan geride";

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
