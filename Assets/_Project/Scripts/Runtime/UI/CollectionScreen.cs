using System.Collections.Generic;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Koleksiyon: oyun boyunca toplanan parçalar ve ilerleme yüzdesi.
    ///
    /// DERS (koleksiyon = ilerlemenin İKİNCİ ekseni): Bölüm sayısı tek eksendir
    /// ve bir noktada oyuncu "kaçıncı bölümdeyim" sorusundan sıkılır. Koleksiyon,
    /// aynı oynanıştan İKİNCİ bir ilerleme çıkarır: "42 parçadan 17'sini
    /// topladım". Yeni oynanış yazmadan oyuna uzunluk ekler ve tamamlanmamış
    /// bir set, tamamlanmış bir setten daha güçlü bir geri dönme sebebidir.
    ///
    /// DERS (kilitli olanı GÖSTER): Toplanmamış parçayı gizlemek "bilmediğim
    /// şeyi özleyemem" demektir. Silüetini göstermek hedefi görünür kılar;
    /// referans oyunlar bu yüzden kilitli parçayı karartıp yerinde bırakır.
    ///
    /// NOT: Referans oyunun koleksiyon ekranının birebir düzeni henüz elimizde
    /// yok. Bu ilk sürüm mantığı ve akışı kuruyor; ekran görüntüsü gelince
    /// yerleşim ona göre düzeltilecek.
    /// </summary>
    public sealed class CollectionScreen : MonoBehaviour
    {
        /// <summary>
        /// Bir set: adı, parça ikonları ve hangi bölümde açıldığı.
        /// Parçalar bölüm ilerlemesiyle açılıyor — ayrı bir düşürme sistemi
        /// kurmadan çalışan en basit kural.
        /// </summary>
        static readonly (string name, string[] icons, int[] levels)[] Sets =
        {
            ("Yolculuk Hatıraları",
                new[] { Art.Coin, Art.Star, Art.Heart, Art.Trophy },
                new[] { 2, 5, 9, 14 }),
            ("Yardımcı Kutusu",
                new[] { Art.Clock, Art.Rocket, Art.Ufo, Art.Chest },
                new[] { 18, 24, 31, 38 }),
            ("Nadir Parçalar",
                new[] { Art.Globe, Art.Shop, Art.Home, Art.Gear },
                new[] { 42, 45, 48, 50 })
        };

        readonly List<(Image icon, Image frame, TextMeshProUGUI label, int level)> _slots =
            new List<(Image, Image, TextMeshProUGUI, int)>();
        TextMeshProUGUI _progressLabel;
        bool _built;

        static readonly Color Periwinkle = new Color(0.353f, 0.322f, 0.784f);
        static readonly Color Locked     = new Color(0.22f, 0.20f, 0.34f);

        public static RectTransform Build(Transform parent)
        {
            var root = MenuShell.Screen(parent, "CollectionScreen");
            var screen = root.gameObject.AddComponent<CollectionScreen>();
            MenuShell.Header(root, "Koleksiyon");

            screen._progressLabel = UiKit.CreateLabel("Progress", root, "", 30,
                new Color(1f, 1f, 1f, 0.85f));
            UiKit.Place(screen._progressLabel, 0.05f, 0.872f, 0.95f, 0.915f);

            float y = 0.845f;
            foreach (var (name, icons, levels) in Sets)
            {
                var title = UiKit.CreateTitle($"Set_{name}", root, name, 34,
                    UiKit.Ink, new Color(0.12f, 0.09f, 0.30f));
                UiKit.Place(title, 0.06f, y - 0.045f, 0.94f, y);
                y -= 0.055f;

                for (int i = 0; i < icons.Length; i++)
                {
                    float x0 = 0.055f + i * 0.2325f;

                    var frame = UiKit.CreateSlicedPanel($"Slot_{name}_{i}", root,
                        UiSkin.Get(Art.PanelDark), Periwinkle);
                    UiKit.Place(frame, x0, y - 0.135f, x0 + 0.205f, y);

                    var icon = UiKit.CreateIcon("Icon", frame.transform, UiSkin.Get(icons[i]));
                    UiKit.Place(icon, 0.14f, 0.24f, 0.86f, 0.94f);

                    var label = UiKit.CreateLabel("Level", frame.transform, "", 20,
                        new Color(1f, 1f, 1f, 0.8f));
                    UiKit.Place(label, 0.04f, 0.03f, 0.96f, 0.24f);

                    screen._slots.Add((icon, frame, label, levels[i]));
                }
                y -= 0.165f;
            }

            screen._built = true;
            return root;
        }

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (!_built || !MetaServices.Ready) return;

            int reached = MetaServices.Progress.HighestUnlockedIndex + 1;
            int owned = 0;

            foreach (var (icon, frame, label, level) in _slots)
            {
                bool unlocked = reached >= level;
                if (unlocked) owned++;

                // Kilitli parça GİZLENMİYOR, karartılıyor: hedef görünür kalsın.
                icon.color = unlocked ? Color.white : new Color(0f, 0f, 0f, 0.55f);
                frame.color = unlocked ? Periwinkle : Locked;
                label.text = unlocked ? "" : $"sv {level}";
            }

            _progressLabel.text =
                $"{owned} / {_slots.Count} parça  ·  %{Mathf.RoundToInt(100f * owned / _slots.Count)}";
        }
    }
}
