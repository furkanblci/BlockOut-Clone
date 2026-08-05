using System.Collections.Generic;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.Services;
using UnityEngine;
using UnityEngine.UI;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Ana ekran: üst barda coin ve can, ortada "Oyna", altında bölüm haritası.
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

        Text _coinLabel;
        Text _livesLabel;
        Text _playLabel;
        Button _playButton;
        readonly List<(Button button, Text label, int index)> _levelButtons
            = new List<(Button, Text, int)>();

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
            var topBar = UiKit.CreatePanel("TopBar", root, UiKit.PanelDark);
            UiKit.Place(topBar, 0.04f, 0.90f, 0.96f, 0.97f);

            _livesLabel = UiKit.CreateLabel("Lives", topBar.transform, "", 44, UiKit.Life);
            UiKit.Place(_livesLabel, 0f, 0f, 0.5f, 1f);

            _coinLabel = UiKit.CreateLabel("Coins", topBar.transform, "", 44, UiKit.Coin);
            UiKit.Place(_coinLabel, 0.5f, 0f, 1f, 1f);

            // --- başlık ---
            var title = UiKit.CreateLabel("Title", root, "BLOCK OUT!", 96, UiKit.Ink);
            UiKit.Place(title, 0f, 0.76f, 1f, 0.88f);

            // --- oyna ---
            _playButton = UiKit.CreateButton("Play", root, "", 56, UiKit.Accent, UiKit.Ink);
            UiKit.Place(_playButton, 0.22f, 0.64f, 0.78f, 0.73f);
            _playLabel = _playButton.GetComponentInChildren<Text>();
            _playButton.onClick.AddListener(PlayCurrent);

            // --- bölüm haritası ---
            var mapTitle = UiKit.CreateLabel("MapTitle", root, "Bölümler", 46, UiKit.Ink);
            UiKit.Place(mapTitle, 0f, 0.57f, 1f, 0.62f);

            var grid = UiKit.CreateRect("Levels", root);
            UiKit.Place(grid, 0.05f, 0.10f, 0.95f, 0.56f);
            BuildLevelGrid(grid);

            // --- teşhis şeridi: kaydın nereden geldiği ---
            if (MetaServices.Ready)
            {
                var state = UiKit.CreateLabel("SaveState", root,
                    $"kayıt: {MetaServices.Save.Outcome}", 26,
                    new Color(1f, 1f, 1f, 0.35f));
                UiKit.Place(state, 0f, 0.03f, 1f, 0.07f);
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
                    40, UiKit.Panel, UiKit.Ink);
                // Izgara yukarıdan aşağı dolar; UI'da y ekseni yukarı baktığı için
                // satırı tersine çeviriyoruz.
                UiKit.Place(button,
                    column * cellW, 1f - (row + 1) * cellH,
                    (column + 1) * cellW, 1f - row * cellH,
                    padding: 8f);

                int index = i;
                button.onClick.AddListener(() => Play(index));
                _levelButtons.Add((button, button.GetComponentInChildren<Text>(), index));
            }
        }

        void Refresh()
        {
            if (!MetaServices.Ready)
            {
                if (_playLabel != null) _playLabel.text = "Oyna";
                return;
            }

            var lives = MetaServices.Lives;
            var progress = MetaServices.Progress;

            _coinLabel.text = $"◉ {progress.Coins}";
            _livesLabel.text = lives.IsFull
                ? $"♥ {lives.Current}/{MetaServices.MaxLives}"
                : $"♥ {lives.Current}/{MetaServices.MaxLives}  " +
                  $"{lives.TimeToNextLife.Minutes:00}:{lives.TimeToNextLife.Seconds:00}";

            int next = Mathf.Clamp(progress.HighestUnlockedIndex, 0,
                Mathf.Max(0, LevelCatalog.Count - 1));
            _playLabel.text = lives.HasLife ? $"OYNA  •  Bölüm {next + 1}" : "CAN BEKLENİYOR";
            _playButton.interactable = lives.HasLife && LevelCatalog.Count > 0;

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
