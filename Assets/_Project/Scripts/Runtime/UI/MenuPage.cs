using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Menü sayfalarının ORTAK KABUĞU: degrade başlık bandı, koyu gövde ve
    /// (gerekiyorsa) sağ üstteki kırmızı çarpı.
    ///
    /// DERS (aynı şeyi beş kez kurmak, beş kez yanlış kurmaktır): Referansta
    /// Yolculuk, Liderlik, Koleksiyon, Ayarlar ve Profil tam olarak aynı üst
    /// bandı ve aynı koyu gövdeyi kullanıyor. Bu ekranlar ayrı ayrı yazıldığı
    /// için her biri kendi başlığını kendi ölçüsüyle kurmuştu ve beşi de
    /// birbirinden biraz farklıydı — hiçbiri de referansa uymuyordu. Kabuk tek
    /// yerde durursa bir düzeltme beş ekranı birden düzeltir.
    ///
    /// DERS (zemin sekme çubuğunun ALTINA inmeli): Kabuk ekranları 0.105'te
    /// bitiriyor, çubuk 0.086'da başlıyor. Aradaki şeritte hiçbir ekran yok ve
    /// altındaki ana ekranın manzarası sızıyor. Gövde 210 birim aşağı taşırılıp
    /// bu kapatılıyor; fazlası zaten çubuğun altında kalıyor.
    /// </summary>
    public static class MenuPage
    {
        /// <summary>Referans karelerinden örneklenen ortak renkler.</summary>
        public static readonly Color Body       = new Color(0.090f, 0.110f, 0.306f);
        public static readonly Color HeaderTop  = new Color(0.275f, 0.216f, 0.886f);
        public static readonly Color HeaderLow  = new Color(0.216f, 0.173f, 0.698f);
        public static readonly Color Panel      = new Color(0.541f, 0.518f, 0.965f);
        public static readonly Color PanelDeep  = new Color(0.129f, 0.110f, 0.325f);
        public static readonly Color Ink        = new Color(1f, 0.99f, 0.96f);
        public static readonly Color InkSoft    = new Color(0.612f, 0.620f, 0.851f);
        public static readonly Color InkDark    = new Color(0.157f, 0.129f, 0.353f);
        public static readonly Color Green      = new Color(0.176f, 0.800f, 0.047f);
        public static readonly Color Blue       = new Color(0.173f, 0.545f, 0.996f);
        public static readonly Color CloseRed   = new Color(0.855f, 0.145f, 0.180f);
        public static readonly Color TitleEdge  = new Color(0.45f, 0.42f, 0.92f);

        public const float HeaderH = 240f;

        /// <summary>Sayfa kökü + koyu gövde. Başlık EN SON kurulur (üstte kalsın).</summary>
        public static RectTransform Screen(Transform parent, string name)
        {
            var root = UiKit.CreateRect(name, parent);
            UiKit.Place(root, 0f, 0f, 1f, 1f);

            // Kök de gövde de sekme çubuğunun ALTINA iner.
            //
            // DERS (kabuk ekranı KISALTIYOR): MenuShell ekranları 0.105-1
            // aralığına koyuyor, yani her sayfa 1920 değil 1718 birim yüksek
            // kuruluyor. Referansta Ayarlar ve Profil TAM EKRAN; bizimkiler
            // aynı içeriği %10 daha kısa bir alana sığdırmaya çalıştığı için
            // her şey küçüldü ve yukarı toplandı. Kökü aşağı taşırmak, dikey
            // ölçüleri referanstan birebir almayı mümkün kılıyor.
            root.offsetMin = new Vector2(0f, -210f);

            var body = UiKit.CreatePanel("Body", root, Body);
            return root;
        }

        /// <summary>
        /// Degrade başlık bandı + ortada beyaz konturlu başlık.
        ///
        /// ÇENTİK ŞERİDİ: Bant, güvenli alanın ÜSTÜNE taşan bir boya şeridiyle
        /// birlikte kuruluyor.
        ///
        /// DERS (aynı hatayı ekranın diğer ucunda görmek): Menü ekranları
        /// `SafeArea`'nın çocuğu, ana ekranın manzarası ise TAM EKRAN. Çentikli
        /// bir telefonda menü sayfası çentiğin altında başlıyor ve üstünde
        /// kalan şeritte ana ekranın manzarası görünüyor — kullanıcının APK
        /// testinde "ana sayfa dışındaki bütün sayfaların üstünde boşluk"
        /// dediği şey buydu (2026-08-17, 6. madde).
        ///
        /// Bu sorun ALT çubukta zaten çözülmüştü (`MenuShell.BuildTabBar`,
        /// "boyası aşağı taşar") ama aynı düşünce üst kenara uygulanmamıştı.
        /// Bir kenarda bulduğun kenar hatasını ÖBÜR kenarda da ara.
        ///
        /// Taşan şey yalnız BOYA: başlık yazısı bandın kendi içinde kalıyor,
        /// yani çentiğin altına girmiyor.
        /// </summary>
        public static RectTransform Header(Transform root, string title)
        {
            // Boya şeridi bandın ÜSTÜNDE, kökün dışına taşıyor. Pivot 0
            // olduğu için köke tepeden yapışıp yukarı doğru uzuyor.
            var bleed = UiKit.CreateRect("HeaderBleed", root);
            bleed.anchorMin = new Vector2(0f, 1f);
            bleed.anchorMax = new Vector2(1f, 1f);
            bleed.pivot = new Vector2(0.5f, 0f);
            bleed.sizeDelta = new Vector2(0f, 320f);
            bleed.anchoredPosition = Vector2.zero;
            var bleedImage = bleed.gameObject.AddComponent<Image>();
            bleedImage.color = HeaderTop;
            bleedImage.raycastTarget = false;

            var band = UiKit.CreateRect("Header", root);
            band.anchorMin = new Vector2(0f, 1f);
            band.anchorMax = new Vector2(1f, 1f);
            band.pivot = new Vector2(0.5f, 1f);
            band.sizeDelta = new Vector2(0f, HeaderH);
            band.anchoredPosition = Vector2.zero;

            var fill = band.gameObject.AddComponent<Image>();
            fill.color = HeaderTop;

            var fade = UiKit.CreateRect("Fade", band);
            var fadeImage = fade.gameObject.AddComponent<Image>();
            fadeImage.sprite = MenuSprites.FadeDown;
            fadeImage.type = Image.Type.Sliced;
            fadeImage.color = HeaderLow;
            fadeImage.raycastTarget = false;
            UiKit.Place(fade, 0f, 0f, 1f, 1f);

            var label = UiKit.CreateTitle("Title", band, title, 62, Ink, TitleEdge);
            UiKit.Place(label, 0.12f, 0.30f, 0.88f, 0.86f);
            return band;
        }

        /// <summary>
        /// Sağ üstteki kırmızı çarpı — referansta Ayarlar ve Profil'de var,
        /// Liderlik ve Koleksiyon'da yok (onlar sekmeden açılıyor).
        /// </summary>
        public static Button Close(Transform band, UnityEngine.Events.UnityAction onClick)
        {
            var root = UiKit.CreateRect("Close", band);
            UiKit.Place(root, 0.795f, 0.32f, 0.945f, 0.84f);

            var ring = UiKit.CreateRect("Ring", root);
            var ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = GameKit.UI.UiSprites.Circle;
            ringImage.color = new Color(0.42f, 0.05f, 0.09f);
            UiKit.Place(ring, 0f, 0f, 1f, 1f);

            var face = UiKit.CreateRect("Face", root);
            var faceImage = face.gameObject.AddComponent<Image>();
            faceImage.sprite = GameKit.UI.UiSprites.Circle;
            faceImage.color = CloseRed;
            faceImage.raycastTarget = false;
            UiKit.Place(face, 0.07f, 0.09f, 0.93f, 0.95f);

            var cross = UiKit.CreateIcon("Cross", face.transform, GameKit.UI.UiSprites.Cross);
            UiKit.Place(cross, 0.26f, 0.26f, 0.74f, 0.74f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = ringImage;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>Tam kapsül yüzey — kabuğun her yerinde aynı biçim.</summary>
        public static Image Capsule(string name, Transform parent, Color color)
        {
            var rect = UiKit.CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = MenuSprites.Capsule;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Alt kenarı koyu, 3B kalınlıklı kapsül düğme.</summary>
        public static Button PillButton(string name, Transform parent, string text,
                                        Color color, int fontSize,
                                        UnityEngine.Events.UnityAction onClick)
        {
            var root = UiKit.CreateRect(name, parent);

            var shadow = Capsule("Shadow", root, Darken(color, 0.58f));
            UiKit.Place(shadow, 0f, 0f, 1f, 1f);
            shadow.raycastTarget = true;

            var face = Capsule("Face", root, color);
            UiKit.Place(face, 0.012f, 0.16f, 0.988f, 1f);

            var label = UiKit.CreateTitle("Label", face.transform, text, fontSize,
                Ink, Darken(color, 0.28f));
            UiKit.Place(label, 0.05f, 0.06f, 0.95f, 0.94f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = shadow;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>Rengi ALFAYA DOKUNMADAN koyultur (`color * f` alfayı da çarpar).</summary>
        public static Color Darken(Color color, float factor) =>
            new Color(color.r * factor, color.g * factor, color.b * factor, color.a);

        /// <summary>Dikeyde piksel, yatayda oran ile yerleşen satır.</summary>
        public static RectTransform Row(string name, Transform parent, float top, float height,
                                        float x0 = 0f, float x1 = 1f)
        {
            var rect = UiKit.CreateRect(name, parent);
            rect.anchorMin = new Vector2(x0, 1f);
            rect.anchorMax = new Vector2(x1, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
            rect.anchoredPosition = new Vector2(0f, -top);
            return rect;
        }
    }
}
