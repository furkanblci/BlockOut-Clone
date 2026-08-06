using System.Collections.Generic;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Yolculuk: dikey bir yol üzerinde bölge kartları ve kilometre taşı
    /// ödülleri. Referans oyunda ödüller Seviye 10/15/25/45/50/60/80/90'da
    /// duruyor; bölgeler "Görev Hazırlığı" (1-20), "Yıldız Yolculuğu" (21-40),
    /// "Penguen Kovalamacası" (41-70) ve kilitli "Zafer Tırmanışı" (101-150).
    ///
    /// DERS (ilerleme görünür olmalı): Yolculuk ekranı oyuna yeni bir kural
    /// EKLEMEZ; yalnızca zaten var olan ilerlemeyi bir hikâyeye bağlar.
    /// "Bir sonraki ödüle 3 bölüm kaldı" hissi, oyuncunun oturumu uzatmasının
    /// en ucuz yoludur — bu yüzden bir sonraki kilometre taşı her zaman
    /// ekranın görünür yerinde durur.
    /// </summary>
    public sealed class JourneyScreen : MonoBehaviour
    {
        /// <summary>Kilometre taşı: hangi seviyede ne verilir.</summary>
        static readonly (int level, string reward)[] Milestones =
        {
            (10, "50 jeton"),
            (15, "sınırsız can · 30 dk"),
            (25, "Yardımcı x1"),
            (45, "100 jeton"),
            (50, "sınırsız can · 1 sa"),
            (60, "Yardımcı x1"),
            (80, "250 jeton"),
            (90, "sınırsız can · 1 gün")
        };

        static readonly (string name, int from, int to)[] Regions =
        {
            ("Görev Hazırlığı", 1, 20),
            ("Yıldız Yolculuğu", 21, 40),
            ("Penguen Kovalamacası", 41, 70),
            ("Zafer Tırmanışı", 101, 150)
        };

        readonly List<(TextMeshProUGUI label, int level)> _milestoneLabels =
            new List<(TextMeshProUGUI, int)>();
        readonly List<(TextMeshProUGUI label, int from)> _regionLabels =
            new List<(TextMeshProUGUI, int)>();
        TextMeshProUGUI _progressLabel;

        public static RectTransform Build(Transform parent)
        {
            var root = MenuShell.Screen(parent, "JourneyScreen");
            var screen = root.gameObject.AddComponent<JourneyScreen>();
            MenuShell.Header(root, "Yolculuk");

            // Dikey yol şeridi — kilometre taşları buna dizilir.
            var track = UiKit.CreateRoundedPanel("Track", root, UiKit.Panel);
            UiKit.Place(track, 0.47f, 0.06f, 0.53f, 0.90f);

            screen._progressLabel = UiKit.CreateLabel("Progress", root, "", 34, UiKit.Ink);
            UiKit.Place(screen._progressLabel, 0.04f, 0.885f, 0.96f, 0.925f);

            // Kilometre taşları alttan yukarı: oyuncu ilerledikçe yukarı tırmanır.
            float slot = 0.82f / Milestones.Length;
            for (int i = 0; i < Milestones.Length; i++)
            {
                var (level, reward) = Milestones[i];
                float y = 0.055f + i * slot;
                bool left = i % 2 == 0;

                var card = UiKit.CreateRoundedPanel($"Milestone_{level}", root, UiKit.PanelDark);
                UiKit.Place(card,
                    left ? 0.06f : 0.54f, y,
                    left ? 0.46f : 0.94f, y + slot * 0.78f);

                var label = UiKit.CreateLabel($"M{level}", card.transform,
                    $"Seviye {level}\n{reward}", 28, UiKit.Ink);
                UiKit.Place(label, 0.05f, 0.05f, 0.95f, 0.95f);
                screen._milestoneLabels.Add((label, level));
            }

            // Bölge kartları sağ üstte küçük rozetler hâlinde.
            for (int i = 0; i < Regions.Length; i++)
            {
                var (name, from, to) = Regions[i];
                var chip = UiKit.CreateRoundedPanel($"Region_{i}", root, UiKit.Panel);
                UiKit.Place(chip, 0.04f + i * 0.235f, 0.005f, 0.235f + i * 0.235f, 0.05f);

                var label = UiKit.CreateLabel($"R{i}", chip.transform,
                    $"{name}\nsv {from}-{to}", 20, UiKit.Ink);
                UiKit.Place(label, 0.03f, 0.03f, 0.97f, 0.97f);
                screen._regionLabels.Add((label, from));
            }

            screen._built = true;
            return root;
        }

        // AddComponent, Build() alanları doldurmadan ÖNCE OnEnable'ı tetikler;
        // bu bayrak olmadan ilk tazeleme null referansa çarpıyor.
        bool _built;

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (!_built) return;

            if (!MetaServices.Ready) return;

            // Kayıt 0 tabanlı dizin tutar; ekranda 1 tabanlı "Seviye" gösterilir.
            int reached = MetaServices.Progress.HighestUnlockedIndex + 1;
            int next = int.MaxValue;
            foreach (var (level, _) in Milestones)
                if (level > reached && level < next) next = level;

            _progressLabel.text = next == int.MaxValue
                ? $"Seviye {reached} · tüm ödüller alındı"
                : $"Seviye {reached} · sonraki ödüle {next - reached} bölüm";

            foreach (var (label, level) in _milestoneLabels)
                label.color = reached >= level ? UiKit.Accent : UiKit.Ink;

            foreach (var (label, from) in _regionLabels)
                label.color = reached >= from ? UiKit.Ink : UiKit.Locked;
        }
    }
}
