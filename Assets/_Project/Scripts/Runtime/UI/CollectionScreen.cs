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

            // Ortada duran görsel: referansta AÇIK BİR KİTAP, mor bir kaidenin
            // üstünde, çevresinde sarılmış albüm paketleri (20. madde).
            // Görsel gelene kadar sandık vekillik ediyordu — "sandık alakasız
            // duruyor" bulgusu (21. madde) tam olarak o vekilliğe aitti.
            var art = UiKit.CreateRect("Art", root);
            UiKit.Place(art, 0.20f, 0.40f, 0.80f, 0.72f);
            var icon = UiKit.CreateIcon("Icon", art,
                UiSkin.Get(Art.CollectionBook) ?? UiSkin.Get(Art.Chest));
            UiKit.Place(icon, 0f, 0f, 1f, 1f);
            icon.preserveAspect = true;

            // ÖLÇÜ REFERANSTAN (`collections.jpeg`): yazı ekranın neredeyse
            // tamamını kaplıyor (X 0.044-0.957), büyük harf yüksekliği
            // ekranın %2.69'u ve Y 0.271-0.299'da duruyor. Bizdeki 40 punto
            // %1.5 veriyordu — yarı yarıya küçüktü ve ekranın ortasında
            // kaybolmuş bir alt yazı gibi duruyordu (5. APK bulgusu).
            // YAZI ÇIPLAK DEĞİL, ÇERÇEVELİ BİR PLAKANIN İÇİNDE.
            //
            // DERS (referansta "sade" olan şey BOŞ değildi): Bu ekranı
            // kurarken referansın sadeliği doğru okunmuştu — ızgara yok,
            // filtre yok — ama cümlenin ALTINDAKİ yüzey gözden kaçmıştı.
            // Referansta yazı, kendi koyu plakasının içinde duruyor ve
            // plakanın açık mor ince bir çerçevesi var. Çıplak yazı ekranın
            // ortasında "yüzüyor"; kullanıcının "Collection kısmının
            // dikdörtgeninin çerçevesi, dış kenarı, gölgesi vs. yok, kötü
            // duruyor" bulgusu buydu.
            //
            // ÖLÇÜLDÜ (`collections.jpeg`, 946×2048): plaka X 0.030-0.970,
            // Y(alttan) 0.259-0.317; dolgu #161C4C, çerçeve #52517D.
            var plate = UiKit.CreateOutlinedBox("HintPlate", root,
                new Color(0.086f, 0.110f, 0.298f),
                new Color(0.322f, 0.318f, 0.490f), borderInset: 0f);
            UiKit.Place(plate, 0.030f, 0.256f, 0.970f, 0.320f);

            screen._hint = UiKit.CreateTitle("Hint", plate.transform, "", 72,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(screen._hint, 0.022f, 0.06f, 0.978f, 0.94f);
            UiKit.SetOutline(screen._hint, new Color(0.075f, 0.055f, 0.235f));

            // İngilizce cümle Türkçesinden UZUN ("Unlock Collection at Level
            // 95!" ↔ "Koleksiyonu Seviye 95'de Aç!"), 72 puntoda sağdan
            // taşıyordu. Referans puntosunu üst sınır yapıp gerekirse
            // küçülmesine izin veriyoruz.
            //
            // DERS (bu projede daha önce de düşüldü): `enableAutoSizing` TEK
            // BAŞINA çalışmaz — `overflowMode` kısıtlayıcı olmazsa TMP yazıyı
            // küçültmek yerine kutudan taşırır.
            screen._hint.enableAutoSizing = true;
            screen._hint.fontSizeMax = 72;
            screen._hint.fontSizeMin = 46;
            screen._hint.overflowMode = TMPro.TextOverflowModes.Truncate;

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

            // SEVİYE KISMI ALTIN. Referansta cümlenin tamamı beyaz değil:
            // "Koleksiyonu **Seviye 95'de** Aç!" — ortadaki koşul altın
            // (#FCC21E, örneklendi). Tek renkte yazınca cümle bir duyuru
            // gibi okunuyor; vurgulanınca oyuncunun aradığı SAYI öne çıkıyor.
            _hint.text = reached >= UnlockLevel
                ? "Collection unlocked!"
                : $"Unlock Collection at <color=#FCC21E>Level {UnlockLevel}</color>!";
        }
    }
}
