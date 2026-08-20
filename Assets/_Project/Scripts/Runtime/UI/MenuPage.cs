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
        /// <summary>ÖLÇÜM: referans Resume düğmesinin yüzünün üst ucu (56,215,20).</summary>
        public static readonly Color Green      = new Color(0.220f, 0.843f, 0.078f);
        /// <summary>ÖLÇÜM: referans Quit düğmesinin yüzünün üst ucu (204,22,16).</summary>
        public static readonly Color Red        = new Color(0.800f, 0.086f, 0.063f);
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

        // ===================== DÜĞMENİN REÇETESİ =====================
        //
        // Kullanıcı (8. tur): "bütün butonlara herkes laf etti, o yeşil
        // kırmızı butonlara — piksel sorunu olduğunu ve tarz olarak alakasız
        // kaldığını söylediler."
        //
        // ÖLÇÜM (`pause_00-00-04.png`, referansın duraklat paneli, 592×1280).
        // Resume düğmesi 280×113 piksel. Dikey tarama (x=200) ve yatay tarama
        // (y=700) şu katmanları verdi:
        //
        //   çok koyu dış çizgi ....... 3 px    (0,84,0)      → taban × 0,20
        //   üst pah (ışık alan) ...... 8 px    (46,244,8)    → taban × 1,19
        //   açık iç kaymak ........... 6 px    (168,253,86)  → beyaza %50
        //   yüz, üst ................ ...      (56,215,20)   → taban × 1,00
        //   yüz, alt ................ ...      (16,170,3)    → taban × 0,78
        //   yan duvar (orta yükseklik) 14 px   (13,130,19)   → taban × 0,64
        //   dip eteği ................ 14 px   (0,85,15)→(5,49,8) → 0,42→0,24
        //
        // Kırmızı Quit düğmesi aynı taramayla aynı ORANLARI verdi
        // (254/204 = 1,25 · 157/204 = 0,77 · 99/204 = 0,49 · 56/204 = 0,27).
        // Yani tasarımcı her renk için ayrı bir palet seçmemiş; tek bir IŞIK
        // profili var ve renk onun altına giriyor. Bu yüzden aşağıdaki her
        // katman tabanın ÇARPANI — düğme yeşil, kırmızı, mavi ya da mor
        // olduğunda kabartma aynı kalıyor.
        //
        // DERS (kabartma, üst üste konan yamalarla yapılmaz): Eski hâlde yüzün
        // üstüne açık bir dikdörtgen, altına koyu bir dikdörtgen konuyordu.
        // İkisi de dikdörtgen olduğu için yuvarlak köşeyi takip etmiyordu ve
        // düğmenin köşelerinde dik kenarlar görünüyordu — "piksel bozukluğu"
        // diye bildirilen şeyin bir bölümü buydu. Artık geçiş ayrı bir yüzey
        // değil, yüzeyin KENDİ köşe noktalarının rengi (bkz. UiVerticalTint).

        /// <summary>Düğmenin köşe yarıçapının kısa kenara oranı — ÖLÇÜM: 40/113.</summary>
        public const float ButtonCornerShare = 0.354f;

        /// <summary>Işık profilinin çarpanları (taban = verilen renk).</summary>
        public const float ShellTop = 1.19f;      // üst pah, ışık alan kenar
        public const float ShellBottom = 0.22f;   // dip eteği, en koyu yer
        // Kaymak çizgisi DİPTE YÜZÜN DEĞİL KABUĞUN tonuna inmeli. Yüzün
        // tonuna (0,78) indirildiğinde dipte 4 piksellik açık bir çizgi
        // kalıyordu: kaymak halkası yüzden biraz daha uzun olduğu için o
        // bant yüzün altında, koyu kabuğun üstünde açıkta kalıyor.
        // Referansta düğmenin dibinde öyle bir çizgi yok.
        public const float RimBottom = 0.50f;
        public const float FaceBottom = 0.78f;    // yüzün alt ucu
        public const float OutlineTone = 0.15f;   // dış çizgi
        const float LabelTracking = -16f;  // harf aralığı, punto yüzdesi

        /// <summary>Kenar payları — hepsi düğmenin YÜKSEKLİĞİNİN oranı.</summary>
        const float ShellInset = 0.033f;   // ÖLÇÜM: 3/113 + gölge payı
        // Kaymak çizgisi yüzden yalnız ~4 piksel dışarıda: referansta üstte
        // 6, yanlarda 2 piksel görünüyor. İlk denemede aradaki fark 7
        // pikseldi ve çizgi düğmenin ÇEVRESİNDE kalın bir halka gibi
        // duruyordu — referansta öyle bir halka yok, yalnız üst kenarda bir
        // ışık var.
        const float RimInset = 0.142f;     // ÖLÇÜM: 16/113
        const float FaceInset = 0.160f;    // ÖLÇÜM: 18/113

        /// <summary>
        /// Referansın kabartmalı düğmesi. Renk dışında her şey ölçülmüş
        /// sabitlerden gelir; çağıran yalnız rengi, yazıyı ve puntoyu verir.
        ///
        /// Adı korundu: otuzdan fazla çağrı yeri var ve hepsi "oyunun standart
        /// düğmesi" anlamında kullanıyor — değişen şey o düğmenin görünüşü.
        /// </summary>
        public static Button PillButton(string name, Transform parent, string text,
                                        Color color, int fontSize,
                                        UnityEngine.Events.UnityAction onClick)
        {
            var root = PillBody(name, parent, color, out var layout, out var tint);

            // ÖLÇÜM: "Resume" harflerinin yüksekliği düğme yüksekliğinin
            // %28,3'ü, genişliği ise %58,6'sı. Bizimki %24,3 ve %63,1'di —
            // yani yazı hem KÜÇÜK hem YAYVAN. Punto artık düğmenin boyundan
            // türetiliyor (UiRingLayout.Bind); `fontSize` parametresi de
            // çağrı yerinin beklediği ALT SINIR olarak yaşamaya devam ediyor.
            //
            // Harf aralığı ölçümden: −16 (yani punto başına %16 daralma).
            // Aynı sayı PERFECT başlığında da çıkmıştı — referansın yazısı
            // Baloo 2'nin varsayılanından tutarlı biçimde daha sıkı.
            //
            // Yazı YÜZÜN DEĞİL KÖKÜN çocuğu. Yüzün içine koymak doğal
            // görünüyor ama ölçüldü: yüz düğmeden 33 piksel kısa ve ölçülen
            // punto (98) o kutuya satır yüksekliğiyle sığmıyordu — TMP
            // kendiliğinden 81'e düşürüyor, harf yüksekliği %28,3 yerine
            // %23,3 kalıyordu. Referansta yazı düğmenin ortasında.
            var label = UiKit.CreateTitle("Label", root, text, fontSize,
                Ink, Darken(color, OutlineTone));
            UiKit.Place(label, 0.04f, 0f, 0.96f, 1f);
            label.raycastTarget = false;
            label.characterSpacing = LabelTracking;
            UiKit.SetOutline(label, Darken(color, OutlineTone), 0.24f);
            layout.Bind(label, fontSize);
            tint.BindLabel(label);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponentInChildren<Image>();
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            if (onClick != null) button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>
        /// Düğmenin YALNIZ GÖVDESİ — yazısız. İçine kendi içeriğini koymak
        /// isteyenler için (ana ekrandaki OYNA düğmesinde iki satır yazı var).
        ///
        /// DERS (bir yapıyı bölmek, kopyalamaktan ucuzdur): OYNA düğmesi
        /// kendi dört katmanını kurmak yerine bunu çağırıyor; böylece ışık
        /// profili tek yerde kalıyor ve "ana ekranın düğmesi menülerinkine
        /// benzemiyor" diye bir geri bildirim bir daha gelemiyor.
        /// </summary>
        /// <param name="cornerShare">
        /// Köşe yarıçapının kısa kenara oranı. Varsayılan kapsül düğmenin
        /// ölçüsü (%35,4); HUD'un kare düğmeleri referansta biraz daha az
        /// yuvarlak — ÖLÇÜM: 138×136 düğmede yarıçap 42, yani %30,9.
        /// </param>
        public static RectTransform PillBody(string name, Transform parent, Color color,
                                             out GameKit.UI.UiRingLayout layout,
                                             out PillTint tint,
                                             float cornerShare = ButtonCornerShare)
        {
            var root = UiKit.CreateRect(name, parent);
            layout = root.gameObject.AddComponent<GameKit.UI.UiRingLayout>();

            // 1) DIŞ ÇİZGİ — düğmeyi zeminden kesen çok koyu halka.
            // Dokunmayı da bu yakalıyor: en dıştaki katman, tıklanabilir
            // alanın gerçek sınırı.
            var outline = UiKit.CreateRoundedPanel("Outline", root,
                Darken(color, OutlineTone), cornerShare, NoRadiusCap);
            UiKit.Place(outline, 0f, 0f, 1f, 1f);
            outline.raycastTarget = true;

            // 2) KABUK — üstte ışık, dipte etek. Tek katman, tek geçiş.
            var shell = Ring("Shell", root, layout, ShellInset,
                             Concentric(cornerShare, ShellInset));
            var shellTint = Tint(shell, Brighten(color, ShellTop), Darken(color, ShellBottom));

            // 3) KAYMAK — yüzün üst kenarındaki açık çizgi.
            var rim = Ring("Rim", root, layout, RimInset,
                           Concentric(cornerShare, RimInset));
            var rimTint = Tint(rim, Color.Lerp(color, Color.white, 0.5f),
                               Darken(color, RimBottom));

            // 4) YÜZ.
            var face = Ring("Face", root, layout, FaceInset,
                            Concentric(cornerShare, FaceInset));
            var faceTint = Tint(face, color, Darken(color, FaceBottom));

            tint = root.gameObject.AddComponent<PillTint>();
            tint.Bind(outline, shellTint, rimTint, faceTint);
            return root;
        }

        /// <summary>
        /// Köşe yarıçabı TAVANSIZ. Tavan (34 birim) panel ve kart için
        /// ölçülmüştü; düğmede oran %35'e çıkıyor ve tavan onu kırpıyordu.
        /// </summary>
        const float NoRadiusCap = 9999f;

        /// <summary>
        /// İÇ KATMANIN KÖŞE ORANI — eş merkezli köşe kuralı.
        ///
        /// İki yüzeyin köşesi eş merkezli olsun istiyorsan iç yarıçap, dış
        /// yarıçaptan payı KADAR küçük olmalı. Ama <see cref="UiCornerFit"/>
        /// oranı kutunun KENDİ kısa kenarına uyguluyor ve iç katmanın kısa
        /// kenarı da iki pay kadar kısalmış oluyor; ikisini birden hesaba
        /// katmak gerekiyor.
        ///
        /// Bu üç sayı eskiden elle yazılmıştı (0,346 · 0,305 · 0,285). Doğru
        /// oldukları sürece sorun yoktu — ta ki HUD düğmeleri için farklı bir
        /// dış oran (%31) gerekene kadar. Elle yazılmış bir sayı, türetilmesi
        /// gereken bir şeyin yerine geçtiğinde ikinci kullanımda sessizce
        /// yanlış olur.
        /// </summary>
        static float Concentric(float outerShare, float inset) =>
            (outerShare - inset) / (1f - 2f * inset);

        /// <summary>
        /// KARE düğmelerin köşe oranı. ÖLÇÜM (`hud_ref_top.png`, geri ve
        /// duraklat düğmeleri): 138×136 kutuda yarıçap 42 — kapsül düğmenin
        /// %35,4'ünden biraz daha az yuvarlak. Üst bardaki "+" ve dişli
        /// düğmeleri de aynı aileden.
        /// </summary>
        public const float SquareCornerShare = 0.309f;

        /// <summary>
        /// Yazı yerine SİMGE taşıyan kare düğme. Gövde kapsül düğmeyle aynı
        /// reçeteden; tek fark köşe oranı ve içine yazı değil ikon girmesi.
        ///
        /// ÖLÇÜM (referans üst barı): yeşil "+" düğmesinin profili
        /// düğmeninkiyle aynı — koyu kontur (11,70,14), parlak kaymak
        /// (174,255,94), yüz (63,218,16) → dipte koyu etek (6,55,10). Dişli
        /// düğmesi de öyle, yalnız rengi mor.
        ///
        /// DERS (simgeli düğme de bir düğmedir): Bu ikisi uzun süre "ikon"
        /// sayılıp hazır PNG'lerle kuruldu — biri komple yuvarlak yeşil bir
        /// düğme görseliydi, oysa referansta yuvarlak KARE. Bir kontrolün
        /// içinde yazı yerine simge olması, onu düğme ailesinden çıkarmıyor.
        /// </summary>
        public static Button IconButton(string name, Transform parent, Sprite icon,
                                        Color color, Color? iconTint = null,
                                        float iconInset = 0.08f)
        {
            var body = PillBody(name, parent, color, out _, out _, SquareCornerShare);

            var glyph = UiKit.CreateIcon("Icon", body.Find("Face"), icon,
                                         iconTint ?? Color.white);
            glyph.raycastTarget = false;
            UiKit.Place(glyph, iconInset, iconInset, 1f - iconInset, 1f - iconInset);

            var button = body.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            body.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            return button;
        }

        /// <summary>Dört kenardan eşit pay alan, beyaz (yani boyanmaya hazır) halka.</summary>
        static Image Ring(string name, RectTransform parent, GameKit.UI.UiRingLayout layout,
                          float inset, float cornerShare)
        {
            var image = UiKit.CreateRoundedPanel(name, parent.transform, Color.white,
                                                 cornerShare, NoRadiusCap);
            image.raycastTarget = false;
            layout.Add(image.rectTransform, inset);
            return image;
        }

        static GameKit.UI.UiVerticalTint Tint(Image image, Color top, Color bottom)
        {
            var tint = image.gameObject.AddComponent<GameKit.UI.UiVerticalTint>();
            tint.Set(top, bottom);
            return tint;
        }

        /// <summary>
        /// Rengi ALFAYA DOKUNMADAN açar. <see cref="Darken"/>'ın eşi:
        /// RGB'yi çarpmak tonu ve doygunluğu korur, yalnız PARLAKLIĞI değişir
        /// — ışık alan bir yüzeyin fizikte yaptığı da tam olarak budur.
        /// Beyaza doğru karıştırmak ise rengi soldurur, o yüzden pah için
        /// uygun değil (referansta pah daha DOYGUN yeşil, daha soluk değil).
        /// </summary>
        public static Color Brighten(Color color, float factor) =>
            new Color(Mathf.Min(1f, color.r * factor),
                      Mathf.Min(1f, color.g * factor),
                      Mathf.Min(1f, color.b * factor), color.a);

        // ===================== AÇ/KAPA ANAHTARI =====================
        //
        // Oyunda İKİ TANE vardı: ayarlar ekranınınki (referanstan ölçülmüş —
        // çip yuvayı taşıyor, üç katmanlı, yazısı koyu yeşil) ve duraklat
        // panelininki (düz iki yarım, yazısı beyaz). Aynı kontrol, iki ayrı
        // görünüş. Düğmelerdeki hikâyenin aynısı.
        //
        // ÖLÇÜM (`pause_00-00-04.png`, y=380 yatay tarama): yeşil çipin
        // profili düğmeninkiyle AYNI — koyu kontur (0,72,4), parlak bilezik
        // (75,211,48), yüz (40,191,13). Yani çip küçük bir düğme; ayrı bir
        // reçeteye gerek yok, <see cref="PillBody"/> zaten onu veriyor.
        //
        // Yazı renkleri de ölçüldü: açıkken "On" KOYU YEŞİL (28,64,25),
        // kapalıyken "Off" LEYLAK (71,57,208). Referansın kontrastı tersine
        // kurulu — parlak yeşilin üstüne beyaz değil, kendinden koyu yazı.

        /// <summary>Yuvanın dış bileziği (#9081FE) ve içi (#342B7E).</summary>
        static readonly Color SwitchRim  = new Color(0.565f, 0.506f, 0.996f);
        static readonly Color SwitchDark = new Color(0.204f, 0.169f, 0.494f);
        /// <summary>Çipin yüzeyi — ÖLÇÜM (40,191,13).</summary>
        static readonly Color SwitchOn   = new Color(0.157f, 0.749f, 0.051f);
        /// <summary>Açık çipin üstündeki yazı — ÖLÇÜM (28,64,25).</summary>
        static readonly Color SwitchOnInk = new Color(0.110f, 0.251f, 0.098f);
        /// <summary>Sönük yarının yazısı — ÖLÇÜM (71,57,208).</summary>
        static readonly Color SwitchIdleInk = new Color(0.278f, 0.224f, 0.816f);

        /// <summary>Kurulan anahtarın parçaları; durumu <see cref="SetOn"/> çevirir.</summary>
        public sealed class SwitchView
        {
            public RectTransform Root;
            public Image OffFace;
            /// <summary>Çipin dokunmayı yakalayan en dış katmanı.</summary>
            public Image OnFace;
            public PillTint Chip;
            public TMPro.TextMeshProUGUI OffText, OnText;

            public void SetOn(bool on)
            {
                // Çip AÇIKKEN yeşil, KAPALIYKEN yuvanın kendi koyusuna
                // düşüyor — yani "sönmüş" değil, yuvaya gömülmüş oluyor.
                Chip.Color = on ? SwitchOn : SwitchDark;
                OffFace.color = on ? SwitchDark : new Color(0.36f, 0.32f, 0.60f);
                OnText.color = on ? SwitchOnInk : SwitchIdleInk;
                OffText.color = on ? SwitchIdleInk : Ink;
            }
        }

        /// <summary>
        /// Oyunun tek aç/kapa anahtarı. Çağıran yalnız kökü yerleştiriyor.
        ///
        /// İki yarı da AYRI AYRI tıklanabilir olmalı (oyuncu istediği duruma
        /// DOĞRUDAN basıyor); tıklamayı çağıran bağlıyor çünkü iki ekranda
        /// iki farklı kaydetme yolu var.
        /// </summary>
        public static SwitchView Switch(string name, Transform parent, int fontSize)
        {
            var root = UiKit.CreateRect(name, parent);

            var rim = Capsule("SlotRim", root, SwitchRim);
            UiKit.Place(rim, 0f, 0f, 1f, 1f);

            var slot = Capsule("Slot", rim.transform, SwitchDark);
            UiKit.Place(slot, 0f, 0f, 1f, 1f, padding: 5f);

            var view = new SwitchView { Root = root };

            view.OffFace = Capsule("Off", slot.transform, SwitchDark);
            view.OffFace.raycastTarget = true;
            UiKit.Place(view.OffFace, 0.03f, 0.08f, 0.58f, 0.92f);
            view.OffText = UiKit.CreateTitle("OffText", view.OffFace.transform, "Off",
                fontSize, InkSoft, new Color(0.12f, 0.10f, 0.28f));
            UiKit.Place(view.OffText, 0.04f, 0.06f, 0.96f, 0.94f);
            view.OffText.raycastTarget = false;

            // Çip yuvayı SAĞDA VE DİKEYDE AŞIYOR: referanstaki kabartma.
            // İçine gömülü bir çip, anahtarı "iki renkli düz bir şerit"
            // gibi gösteriyordu.
            var chip = PillBody("On", slot.transform, SwitchOn, out var layout, out view.Chip);
            UiKit.Place(chip, 0.60f, -0.06f, 1.065f, 1.06f);
            view.OnFace = chip.Find("Outline").GetComponent<Image>();

            view.OnText = UiKit.CreateTitle("OnText", chip, "On", fontSize,
                SwitchOnInk, new Color(0.63f, 1f, 0.45f));
            UiKit.Place(view.OnText, 0.04f, 0f, 0.96f, 1f);
            view.OnText.characterSpacing = LabelTracking;
            view.OnText.raycastTarget = false;
            layout.Bind(view.OnText, fontSize);

            return view;
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
