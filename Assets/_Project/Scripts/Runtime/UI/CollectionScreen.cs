using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Koleksiyon — referanstan kuruldu.
    /// Referans kare: `menus,powerups,vs.mp4`, 43-45. saniyeler.
    ///
    /// DERS (referans BOŞ diyorsa boş bırak): Bu ekran uzun süre bir tahmin
    /// üzerine kuruluydu — ızgara dolusu rozet, ilerleme yüzdesi, filtreler.
    /// Referansta 53. seviyedeki bir oyuncuda ekranın TAMAMI şu: ortada bir
    /// kitap/blok görseli ve altında tek satır, "Unlock Collection at Level 95!".
    /// İçerik 95. seviyeye kadar hiç açılmıyor. Var olmayan bir özelliğin
    /// arayüzünü uydurmak, portfolyoda "yapabiliyor" değil "anlamamış" der.
    ///
    /// Seviye 95'e gelindiğinde burası gerçek koleksiyonla doldurulacak; o
    /// hâlin referansı henüz elimizde YOK.
    /// </summary>
    public sealed class CollectionScreen : MonoBehaviour
    {
        /// <summary>Referanstaki eşik.</summary>
        const int UnlockLevel = 95;

        TextMeshProUGUI _hint;
        bool _built;

        public static RectTransform Build(Transform parent)
        {
            var root = MenuPage.Screen(parent, "CollectionScreen");
            var screen = root.gameObject.AddComponent<CollectionScreen>();

            // Ortada duran görsel: elimizde kitap yok, sandık en yakını.
            var art = UiKit.CreateRect("Art", root);
            UiKit.Place(art, 0.24f, 0.42f, 0.76f, 0.70f);
            var icon = UiKit.CreateIcon("Icon", art, UiSkin.Get(Art.Chest));
            UiKit.Place(icon, 0f, 0f, 1f, 1f);

            screen._hint = UiKit.CreateTitle("Hint", root, "", 40,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(screen._hint, 0.06f, 0.33f, 0.94f, 0.40f);

            MenuPage.Header(root, "Collection");

            screen._built = true;
            return root;
        }

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (!_built) return;

            int reached = MetaServices.Ready
                ? MetaServices.Progress.HighestUnlockedIndex + 1
                : 1;

            _hint.text = reached >= UnlockLevel
                ? "Collection unlocked!"
                : $"Unlock Collection at Level {UnlockLevel}!";
        }
    }
}
