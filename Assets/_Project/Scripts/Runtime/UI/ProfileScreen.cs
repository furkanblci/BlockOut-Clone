using System.Collections.Generic;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Profil: avatar, ad, seviye ve "Genel İstatistikler" — referans oyundaki
    /// sekiz sayaç.
    ///
    /// DERS (istatistik = oynanışın hafızası): Sayaçların çoğu henüz var olmayan
    /// etkinliklere ait (Blok Ligi, Roket Yarışı...). Yine de ekrana konur ve
    /// "-" gösterir: oyuncuya oyunun ilerideki genişliğini anlatır, bizim için
    /// de o etkinlikler geldiğinde bağlanacak hazır bir yer olur. Boş kutuyu
    /// SONRA eklemek, ekranın yerleşimini yeniden düşünmek demektir.
    /// </summary>
    public sealed class ProfileScreen : MonoBehaviour
    {
        static readonly string[] StatNames =
        {
            "İlk Denemede Kazanıldı",
            "Gökyüzü Atlayışı Zaferleri",
            "Seri Yarışı Galibiyetleri",
            "Blok Ligi Galibiyetleri",
            "Yıldız Patlaması Tamamlandı",
            "Haftalık Kupa Zaferleri",
            "Roket Yarışı Zaferleri",
            "Maksimum Ufo Yükselişi"
        };

        readonly List<TextMeshProUGUI> _statValues = new List<TextMeshProUGUI>();
        TextMeshProUGUI _nameLabel, _levelLabel;

        public static RectTransform Build(Transform parent)
        {
            var root = MenuShell.Screen(parent, "ProfileScreen");
            var screen = root.gameObject.AddComponent<ProfileScreen>();
            MenuShell.Header(root, "Profil");

            // Kimlik kartı: avatar + ad + seviye.
            var card = UiKit.CreateRoundedPanel("Identity", root, UiKit.PanelDark);
            UiKit.Place(card, 0.05f, 0.78f, 0.95f, 0.915f);

            var avatar = UiKit.CreateRoundedPanel("Avatar", card.transform, UiKit.Panel);
            UiKit.Place(avatar, 0.03f, 0.12f, 0.24f, 0.88f);
            var face = UiKit.CreateLabel("Face", avatar.transform, ":)", 54, UiKit.Ink);
            UiKit.Place(face, 0f, 0f, 1f, 1f);

            screen._nameLabel = UiKit.CreateTitle("Name", card.transform, "", 40, UiKit.Ink, UiKit.PanelDark);
            UiKit.Place(screen._nameLabel, 0.27f, 0.45f, 0.72f, 0.9f);

            screen._levelLabel = UiKit.CreateLabel("Level", card.transform, "", 32, UiKit.Ink);
            UiKit.Place(screen._levelLabel, 0.27f, 0.1f, 0.72f, 0.45f);

            var statsTitle = UiKit.CreateLabel("StatsHeading", root, "Genel İstatistikler", 34, UiKit.Ink);
            UiKit.Place(statsTitle, 0.05f, 0.73f, 0.95f, 0.775f);

            // Sekiz sayaç, iki sütun hâlinde.
            for (int i = 0; i < StatNames.Length; i++)
            {
                int col = i % 2, row = i / 2;
                float x0 = 0.05f + col * 0.46f, x1 = x0 + 0.44f;
                float y1 = 0.71f - row * 0.16f, y0 = y1 - 0.14f;

                var cell = UiKit.CreateRoundedPanel($"Stat_{i}", root, UiKit.PanelDark);
                UiKit.Place(cell, x0, y0, x1, y1);

                var name = UiKit.CreateLabel("Name", cell.transform, StatNames[i], 22, UiKit.Ink);
                UiKit.Place(name, 0.06f, 0.42f, 0.94f, 0.95f);

                var value = UiKit.CreateTitle("Value", cell.transform, "-", 40, UiKit.Coin, UiKit.PanelDark);
                UiKit.Place(value, 0.06f, 0.05f, 0.94f, 0.42f);
                screen._statValues.Add(value);
            }

            return root;
        }

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (!MetaServices.Ready) return;

            var progress = MetaServices.Progress;
            _nameLabel.text = string.IsNullOrEmpty(MetaServices.PlayerName)
                ? "Oyuncu" : MetaServices.PlayerName;
            _levelLabel.text = "Seviye " + (progress.HighestUnlockedIndex + 1);

            // Şimdilik yalnız ilk sayaç gerçek veriden besleniyor: ilk denemede
            // bitirilen bölüm sayısı. Diğerleri ilgili etkinlik geldiğinde bağlanır.
            _statValues[0].text = progress.FirstTryClears.ToString();
        }
    }
}
