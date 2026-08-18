using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;
using UiCornerFit = GameKit.UI.UiCornerFit;

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
        // ZEMİN REFERANSTAN ÖLÇÜLDÜ (2026-08-17): Ayarlar'da #1E1858,
        // Koleksiyon'da #1B1B4F-#20205C. Bizimki #171C4E idi — biraz daha
        // YEŞİLE kaçıyordu (G kanalı R'den büyüktü), oysa referansın her
        // menü zemini mora çalıyor.
        public static readonly Color Body       = new Color(0.118f, 0.094f, 0.345f);
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
        /// <summary>
        /// Profil sayfasının zemini — diğerlerinden AÇIK.
        ///
        /// DERS (ortak kabuk, her yerde aynı renk demek değil): Referansta
        /// Ayarlar, Liderlik ve Koleksiyon koyu lacivert bir zemin kullanıyor
        /// (#1E1856 civarı) ama Profil belirgin biçimde daha açık bir mor
        /// (#302488). Biz hepsine aynı koyu zemini vermiştik ve Profil'deki
        /// KOYU istatistik kutuları zeminden hiç ayrışmıyordu — ekran
        /// "düzensiz" görünmesinin sebebi buydu (21. APK bulgusu).
        /// Kutu rengimiz zaten doğruydu; yanlış olan arkasındaki zemindi.
        /// </summary>
        public static readonly Color BodyProfile = new Color(0.188f, 0.141f, 0.533f);

        public static RectTransform Screen(Transform parent, string name,
                                           Color? bodyColor = null)
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

            var body = UiKit.CreatePanel("Body", root, bodyColor ?? Body);

            // Elle düzenlenebilir ekran olarak işaretle (Tools > Block Out >
            // Arayüz Tasarımı). Yalnız bir string alan; hiçbir karede iş yok.
            GameKit.UI.UiTweak.Mark(root, name);
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

            // BANDIN ALT DUDAĞI — iki ince şerit.
            //
            // DERS (bir bant nerede BİTTİĞİNİ söylemeli): Bant düz bir renk
            // dikdörtgeniydi ve altındaki içerikle arasında hiçbir sınır yoktu;
            // kullanıcı "en üstte yine dikdörtgen var, detaysız" derken bunu
            // gördü. Referansta bandın alt kenarında önce KOYU bir çizgi, onun
            // hemen üstünde İNCE bir ışık var — plastik bir kapağın kalınlığı.
            // Bantı bir yüzey yapan şey degrade değil, bu iki piksellik kenar.
            var lipLight = UiKit.CreateRect("LipLight", band);
            var lipLightImage = lipLight.gameObject.AddComponent<Image>();
            lipLightImage.color = new Color(0.435f, 0.376f, 0.976f, 0.55f);
            lipLightImage.raycastTarget = false;
            lipLight.anchorMin = new Vector2(0f, 0f);
            lipLight.anchorMax = new Vector2(1f, 0f);
            lipLight.pivot = new Vector2(0.5f, 0f);
            lipLight.sizeDelta = new Vector2(0f, 9f);
            lipLight.anchoredPosition = new Vector2(0f, 7f);

            var lipDark = UiKit.CreateRect("LipDark", band);
            var lipDarkImage = lipDark.gameObject.AddComponent<Image>();
            lipDarkImage.color = new Color(0.098f, 0.071f, 0.318f, 0.85f);
            lipDarkImage.raycastTarget = false;
            lipDark.anchorMin = new Vector2(0f, 0f);
            lipDark.anchorMax = new Vector2(1f, 0f);
            lipDark.pivot = new Vector2(0.5f, 0f);
            lipDark.sizeDelta = new Vector2(0f, 7f);
            lipDark.anchoredPosition = Vector2.zero;

            // PUNTO REFERANSTAN (2026-08-17): dört ekranın da başlığı ölçüldü
            // ve büyük harf yüksekliği tutarlı biçimde ekranın **%2.7**'si:
            // Yolculuk 0.0273 · Liderlik 0.0269 · Profil 0.0278.
            // 62 punto %2.32 veriyordu.
            //
            // DERS (ALT UZANTI ölçüyü şişirir): İlk ölçümde "Ayarlar" %3.47,
            // "Koleksiyon" %3.17 çıktı ve "başlıklar %50 küçük" gibi göründü.
            // İkisinde de 'y' harfi var; beyaz piksel kutusu alt uzantıyı da
            // sayıyor. Alt uzantısı olmayan üç başlık gerçeği söyledi.
            // Punto seçerken ölçülmesi gereken şey BÜYÜK HARF yüksekliği.
            var label = UiKit.CreateTitle("Title", band, title, 72, Ink, TitleEdge);
            UiKit.Place(label, 0.12f, 0.28f, 0.88f, 0.88f);

            // KONTUR — paylaşılan başlık materyali `CreateTitle`'a verilen
            // kontur rengini sessizce yok sayıyor (bu projede yedinci tuzak).
            // Referanstan örneklendi (#322192); başlığa "baskılı" görünümünü
            // veren şey bu. Aynı eksik Yolculuk ve Mağaza başlıklarında da
            // vardı — burada düzeltmek Ayarlar, Profil, Liderlik ve
            // Koleksiyon'u BİRDEN düzeltiyor.
            UiKit.SetOutline(label, new Color(0.196f, 0.129f, 0.573f));
            return band;
        }

        /// <summary>
        /// Sağ üstteki kırmızı çarpı — referansta Ayarlar ve Profil'de var,
        /// Liderlik ve Koleksiyon'da yok (onlar sekmeden açılıyor).
        /// </summary>
        public static Button Close(Transform band, UnityEngine.Events.UnityAction onClick)
        {
            // ÖLÇÜ REFERANSTAN (`profil.jpeg`): çarpı 104×105 piksel, yani
            // TAM DAİRE; ekranda X 0.803-0.913, bandın alt yarısında.
            // Bizimki 162×125 birimlik bir kutuya konuyordu ve daire o kutuya
            // gerilip ELİPSE dönüyordu — kullanıcının "çarpı işareti de çok
            // basık gibi" dediği şey buydu.
            //
            // DERS (bu projede aynı hata ÜÇÜNCÜ kez): Yeşil artı düğmesi,
            // liderlik madalyası ve şimdi kapatma çarpısı. Düz bir `Image`
            // sprite'ı kutuya GERER; `CreateIcon` en-boy oranını KORUR.
            // Daire çizen her yerde ikincisi kullanılmalı — kutu kare
            // olmadığında hatanın görünmesi için ekranın ölçülmesi gerekiyor,
            // oysa doğru bileşen seçilirse hata hiç doğmuyor.
            var root = UiKit.CreateRect("Close", band);
            UiKit.Place(root, 0.823f, 0.117f, 0.913f, 0.529f);

            var ringImage = UiKit.CreateIcon("Ring", root, GameKit.UI.UiSprites.Circle,
                new Color(0.42f, 0.05f, 0.09f));
            ringImage.raycastTarget = true;
            UiKit.Place(ringImage, 0f, 0f, 1f, 1f);

            var faceImage = UiKit.CreateIcon("Face", root, GameKit.UI.UiSprites.Circle, CloseRed);
            UiKit.Place(faceImage, 0.07f, 0.09f, 0.93f, 0.95f);
            var face = faceImage.rectTransform;

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
        /// <summary>
        /// Menülerin ortak yüzeyi: yuvarlak köşeli kutu.
        ///
        /// DERS (referansta HİÇBİR yüzey tam hap değil): Bu yardımcı adı
        /// "Capsule" olduğu için gerçekten kapsül çiziyordu —
        /// <see cref="MenuSprites.Capsule"/>'ün 9-dilim payı yarıçapın TAMAMI
        /// (<c>s/2-1</c>), yani uçlar tam yarım daire. Mağaza bantları, Off/On
        /// anahtarı, jeton plakası, liderlik sekmeleri, Yolculuk kapsülleri —
        /// hepsi buradan besleniyordu ve hepsi için "çok oval, kötü duruyor"
        /// bulgusu geldi.
        ///
        /// Referans ölçüldü: en yuvarlak yüzey bile (Resume/Quit düğmesi,
        /// 276×100) kısa kenarın **%29**'u; küçük kontroller %20-22; büyük
        /// kartlar ~34 pikselde sabitleniyor. Yani tasarımda kapsül YOK.
        /// Yüzey artık <see cref="UiCornerFit"/> ile oranlı yarıçap alıyor.
        ///
        /// Ad korundu: otuzdan fazla çağrı yeri var ve hepsi "menünün standart
        /// yüzeyi" anlamında kullanıyor — değişen şey o yüzeyin şekli.
        /// </summary>
        public static Image Capsule(string name, Transform parent, Color color,
                                    float cornerShare = UiCornerFit.HouseShare)
        {
            var image = UiKit.CreateRoundedPanel(name, parent, color, cornerShare);
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

        /// <summary>
        /// Sayıyı oyunun her yerinde AYNI biçimde yazar: binlik ayıracı BOŞLUK
        /// ("2 000"). Referans böyle yapıyor (`market.jpeg` paket tutarları).
        ///
        /// DERS (biçimlendirme KÜLTÜRE bağlıdır): `ToString("N0")` cihazın
        /// diline göre ayırır — Türkçe bir telefonda "1.720" çıkar ve oyuncu
        /// bunu ondalık sanabilir. `InvariantCulture` ile virgüle sabitleyip
        /// boşluğa çevirmek, hangi dilde açılırsa açılsın aynı sonucu verir.
        ///
        /// DERS (aynı sayı iki ekranda iki türlü yazılmamalı): Mağaza bu
        /// yardımcıyı kullanıyordu ama ana ekranın üst çubuğu düz `ToString()`
        /// ile yazıyordu; aynı 1720 jetonu bir ekranda "1 720", diğerinde
        /// "1720" görünüyordu. Ortak biçim ortak yerde durmalı.
        /// </summary>
        public static string Amount(int value) => value.ToString("N0",
            System.Globalization.CultureInfo.InvariantCulture).Replace(",", " ");

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
