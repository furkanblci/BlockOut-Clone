using System.Collections.Generic;
using System.Text;
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
    /// Ana ekran: üst barda can ve coin, ortada "Oyna", altında bölüm haritası.
    ///
    /// DERS (ekran = durum + yansıtma): Bu sınıf hiçbir KURAL bilmiyor. Kaç can
    /// var, bölüm açık mı, coin kaç — hepsini meta servislerine soruyor ve
    /// yalnızca ekrana çiziyor. Kuralı da UI'ı da aynı yere yazmak kısa vadede
    /// hızlıdır ama ikinci bir ekran (bölüm sonu, mağaza) eklendiğinde kural
    /// kopyalanır ve ikisi sessizce ayrışır.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        const int PerRow = 5;

        TextMeshProUGUI _coinLabel;
        TextMeshProUGUI _livesLabel;
        TextMeshProUGUI _playLabel;
        Button _playButton;
        readonly List<(Button button, TextMeshProUGUI label, int index)> _levelButtons
            = new List<(Button, TextMeshProUGUI, int)>();

        // Çöp üretmeyen metin: değer değişmedikçe yeni dize kurulmaz.
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
            // Can geri sayımı saniyede bir yeter; her karede kurmak savurganlık.
            if (Time.unscaledTime < _nextTick) return;
            _nextTick = Time.unscaledTime + 1f;

            if (MetaServices.Ready) MetaServices.Lives.Refresh();
            Refresh();
        }

        void BuildUi()
        {
            var canvas = UiKit.CreateCanvas("HomeCanvas");
            var root = UiKit.CreateSafeArea(canvas);

            UiKit.CreatePanel("Background", root, UiKit.Background);

            // --- üst bar: can | coin ---
            var topBar = UiKit.CreateRoundedPanel("TopBar", root, UiKit.PanelDark);
            UiKit.Place(topBar, 0.04f, 0.905f, 0.96f, 0.975f);

            var livesChip = UiKit.CreateRoundedPanel("LivesChip", topBar.transform, UiKit.Life);
            UiKit.Place(livesChip, 0.03f, 0.16f, 0.47f, 0.84f);
            _livesLabel = UiKit.CreateLabel("Lives", livesChip.transform, "", 42, UiKit.Ink);
            UiKit.Place(_livesLabel, 0f, 0f, 1f, 1f);

            var coinChip = UiKit.CreateRoundedPanel("CoinChip", topBar.transform, UiKit.Coin);
            UiKit.Place(coinChip, 0.53f, 0.16f, 0.97f, 0.84f);
            _coinLabel = UiKit.CreateLabel("Coins", coinChip.transform, "", 42,
                new Color(0.32f, 0.20f, 0.02f));
            UiKit.Place(_coinLabel, 0f, 0f, 1f, 1f);

            // --- başlık ---
            var title = UiKit.CreateTitle("Title", root, "BLOCK OUT!", 110,
                UiKit.Ink, new Color(0.18f, 0.12f, 0.42f));
            UiKit.Place(title, 0f, 0.775f, 1f, 0.885f);

            // --- oyna ---
            _playButton = UiKit.CreateButton("Play", root, "", 54, UiKit.Accent, UiKit.Ink);
            UiKit.Place(_playButton, 0.20f, 0.655f, 0.80f, 0.745f);
            _playLabel = _playButton.GetComponentInChildren<TextMeshProUGUI>();
            _playButton.onClick.AddListener(PlayCurrent);

            // --- bölüm haritası ---
            var mapPanel = UiKit.CreateRoundedPanel("MapPanel", root,
                new Color(0.17f, 0.14f, 0.38f));
            UiKit.Place(mapPanel, 0.04f, 0.085f, 0.96f, 0.615f);

            var mapTitle = UiKit.CreateLabel("MapTitle", mapPanel.transform, "Bölümler", 44,
                new Color(1f, 1f, 1f, 0.7f));
            UiKit.Place(mapTitle, 0f, 0.90f, 1f, 0.99f);

            var grid = UiKit.CreateRect("Levels", mapPanel.transform);
            UiKit.Place(grid, 0.03f, 0.02f, 0.97f, 0.88f);
            BuildLevelGrid(grid);

            // --- teşhis şeridi: kaydın nereden geldiği ---
            if (MetaServices.Ready)
            {
                var state = UiKit.CreateLabel("SaveState", root,
                    $"kayıt: {MetaServices.Save.Outcome}", 24,
                    new Color(1f, 1f, 1f, 0.3f));
                UiKit.Place(state, 0f, 0.02f, 1f, 0.06f);
            }
        }

        void BuildLevelGrid(RectTransform grid)
        {
            int count = LevelCatalog.Count;
            if (count == 0)
            {
                UiKit.CreateLabel("Empty", grid, "Bölüm bulunamadı", 36, UiKit.Ink);
                return;
            }

            int rows = Mathf.CeilToInt(count / (float)PerRow);
            float cellW = 1f / PerRow;
            float cellH = 1f / rows;

            for (int i = 0; i < count; i++)
            {
                int column = i % PerRow;
                int row = i / PerRow;

                var button = UiKit.CreateButton($"Level{i + 1}", grid, (i + 1).ToString(),
                    38, UiKit.Panel, UiKit.Ink);
                // Izgara yukarıdan aşağı dolar; UI'da y ekseni yukarı baktığı için
                // satırı tersine çeviriyoruz.
                UiKit.Place(button,
                    column * cellW, 1f - (row + 1) * cellH,
                    (column + 1) * cellW, 1f - row * cellH,
                    padding: 7f);

                int index = i;
                button.onClick.AddListener(() => Play(index));
                _levelButtons.Add((button, button.GetComponentInChildren<TextMeshProUGUI>(), index));
            }
        }

        void Refresh()
        {
            if (!MetaServices.Ready)
            {
                if (_playLabel != null) _playLabel.text = "OYNA";
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

                var sb = _scratch.Clear().Append(_shownLives).Append(" / ").Append(MetaServices.MaxLives);
                if (refillSeconds >= 0)
                    sb.Append("  ").Append(refill.Minutes / 10).Append(refill.Minutes % 10)
                      .Append(':').Append(refill.Seconds / 10).Append(refill.Seconds % 10);
                _livesLabel.text = sb.ToString();
            }

            int next = Mathf.Clamp(progress.HighestUnlockedIndex, 0,
                Mathf.Max(0, LevelCatalog.Count - 1));
            bool canPlay = lives.HasLife && LevelCatalog.Count > 0;
            if (next != _shownNext || canPlay != _playButton.interactable)
            {
                _shownNext = next;
                _playLabel.text = lives.HasLife
                    ? _scratch.Clear().Append("OYNA  •  Bölüm ").Append(next + 1).ToString()
                    : "CAN BEKLENİYOR";
            }
            _playButton.interactable = canPlay;

            foreach (var (button, label, index) in _levelButtons)
            {
                bool unlocked = progress.IsUnlocked(index);
                var record = progress.Record(LevelCatalog.IdAt(index));

                button.interactable = unlocked && lives.HasLife;
                var image = button.targetGraphic as Image;
                if (image != null)
                    image.color = !unlocked ? UiKit.Locked
                        : record.Perfect ? UiKit.Coin
                        : record.Cleared ? UiKit.Accent
                        : UiKit.Panel;

                if (label != null)
                    label.color = unlocked ? UiKit.Ink : new Color(1f, 1f, 1f, 0.35f);
            }
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
