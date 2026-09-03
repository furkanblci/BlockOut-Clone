using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;
using UiCornerFit = GameKit.UI.UiCornerFit;

namespace GameKit.Screens
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
        // BANT REFERANSTAN ÖLÇÜLDÜ (12. tur, G1/N2 — `m_051.jpg`).
        //
        // Eski değerler (70,55,226) / (55,44,178) idi. Referansın bandı
        // (66,39,196), üst kenarından alta doğru (60,35,179)'a iniyor.
        // Fark küçük görünüyor ama SONUCU BOZUYORDU: referansın başlık
        // halesi (98,71,228), yani bizim ESKİ BANDIMIZ kadar açıktı.
        // Banttan açık bir hale çizmeye yer kalmıyordu; hale eklendiğinde
        // bant içinde kayboluyordu.
        //
        // DERS (bir rengi tek başına doğrulama, KOMŞUSUYLA doğrula): Bandın
        // rengi tek başına bakınca makuldü. Yanlış olan şey, üstüne
        // çizilecek olan halenin ondan AÇIK olması gerektiğiydi — iki renk
        // arasındaki İLİŞKİ ölçülmeden hiçbiri "doğru" sayılamaz.
        public static readonly Color HeaderTop  = new Color(0.259f, 0.153f, 0.769f);
        public static readonly Color HeaderLow  = new Color(0.216f, 0.129f, 0.639f);
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
        public static readonly Color TitleEdge  = new Color(0.450f, 0.420f, 0.920f);

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
        /// <summary>
        /// Ana ekranın ve menü kabuğunun arkasındaki manzara (J1, 14. tur).
        ///
        /// Oyuncu yolculukta bir bölgeyi tamamlayınca o bölgenin görselini
        /// arka plan olarak SEÇEBİLİYOR; seçim kayıtta duruyor. Seçilmemişse
        /// ya da seçilen görsel eksikse varsayılan manzaraya düşülüyor —
        /// eksik bir varlık yüzünden ekranın boş kalması kabul edilemez.
        ///
        /// İKİ ÇAĞIRAN, TEK KAYNAK: `HomeScreen` ve `MenuShell` aynı manzarayı
        /// çiziyor. İkisinde ayrı ayrı `UiSkin.Get(Art.MenuBack)` yazılıydı;
        /// seçim özelliği eklenirken birini güncelleyip diğerini unutmak
        /// işten değildi. Tek metot, iki yerin ayrı düşmesini imkânsız kılıyor.
        /// </summary>
        public static Sprite SeciliManzara() => SeciliManzara(out _);

        /// <param name="yakinlik">
        /// Görselin ne kadar büyütüleceği. Bölge görselleri DAİRE çizilmiş
        /// (yolculuk diskleri için); tam ekran arka plan yapılınca köşelerde
        /// dairenin kavisi görünüyor. Dairenin İÇİNE SIĞAN KARE ekranı
        /// doldursun diye 1.45 ile büyütülüyorlar (kabaca √2). Varsayılan
        /// manzara zaten dikdörtgen, o 1.0 kalıyor.
        ///
        /// DERS (bir varlığı yeni bir yerde kullanmak, onu yeniden ölçmek
        /// demektir): Bölge görselleri arka plan olarak kullanılabilsin diye
        /// açıldı ve kod doğru çalıştı — ama görselin BİÇİMİ o iş için
        /// çizilmemişti. Özelliği ekledikten sonra ekranda görmeden
        /// "bitti" demek, yuvarlak kenarlı bir ana ekran yollamak olurdu.
        /// </param>
        public static Sprite SeciliManzara(out float yakinlik)
        {
            yakinlik = 1f;
            int secim = GameKit.Meta.MetaServices.Ready
                ? GameKit.Meta.MetaServices.Progress.Background : 0;
            if (secim > 0)
            {
                var bolge = UiSkin.Get(Art.Region(secim));
                if (bolge != null) { yakinlik = 1.45f; return bolge; }
            }
            return UiSkin.Get(Art.MenuBack);
        }

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
            // PUNTO 72 -> 104 (14. tur, G5). Kullanıcı: *"kabartmalı
            // başlıkların boyutu ve konumu bazı ekranlarda ayarlanacak."*
            //
            // ÖLÇÜM iki bağımsız yoldan aynı çarpanı verdi:
            //   • BÜYÜK HARF YÜKSEKLİĞİ — referansın kısa başlığı "Ayarlar"
            //     ekran yüksekliğinin %4,09'u; bizim alt uzantısız en kısa
            //     başlığımız "Collection" %2,71. Oran 1,51.
            //   • GENİŞLİK — referansın uzun başlığı "Liderlik Panosu" ekran
            //     genişliğinin %62,5'i; bizim "Leaderboard" %43. 72 punto
            //     104'e çıkınca %65 oluyor, yani aynı yere düşüyor.
            // İki farklı ölçünün aynı çarpanda buluşması, sayının tek bir
            // karenin gürültüsü olmadığını söylüyor.
            //
            // Ayrıca bu, Mağaza başlığıyla (zaten 104) sayfa başlıklarını
            // EŞİTLİYOR — G5'in "bazı ekranlarda" dediği tutarsızlık buydu:
            // Mağaza %6,09, diğer sayfalar %2,50-3,02'ydi.
            var label = UiKit.CreateTitle("Title", band, title, 104, Ink, TitleEdge);
            UiKit.Place(label, 0.12f, 0.28f, 0.88f, 0.88f);

            // KONTUR — paylaşılan başlık materyali `CreateTitle`'a verilen
            // kontur rengini sessizce yok sayıyor (bu projede yedinci tuzak).
            // Aynı eksik Yolculuk ve Mağaza başlıklarında da vardı — burada
            // düzeltmek Ayarlar, Profil, Liderlik ve Koleksiyon'u BİRDEN
            // düzeltiyor.
            //
            // 12. TUR (G1): kontur rengi #322192 idi ve YÖNÜ YANLIŞTI.
            // Ölçüm (`m_051.jpg`, "Profil" başlığı):
            //     bandın rengi        (66, 39, 196)
            //     referans halesi     (98, 71, 228)  -> BANTTAN AÇIK
            //     bizim konturumuz    (50, 33, 146)  -> BANTTAN KOYU
            // Referans harfin çevresine IŞIK koyuyor, biz gölge koyuyorduk;
            // bu yüzden bizimki "ince karanlık çizgi", referanstaki "kalın
            // parlak hale" gibi okunuyordu. Üstelik tek katman yetmiyor:
            // referansta harfin DİBİNDE ince koyu bir kenar da var.
            //
            // İkisini birden `UiTitleEmboss` veriyor (arkaya şişirilmiş bir
            // kopya + öne ince koyu kenar). Kullanıcının "shader mı yazarsın
            // üst üste text mi koyarsın" sorusunun cevabı: üst üste text.
            GameKit.UI.UiTitleEmboss.Apply(label,
                halo: new Color(0.384f, 0.278f, 0.894f),   // (98,71,228)
                shadow: new Color(0.137f, 0.071f, 0.420f));
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
            // KUTU %21 BÜYÜDÜ (13. tur, G3). Görsele geçince koyu halka artık
            // KUTUNUN İÇİNDE — eskiden ayrı bir katmandı ve kutuyu doldurup
            // taşabiliyordu. Aynı kutuda görünen kırmızı %20 küçüldü.
            //
            // ÖLÇÜM EKRAN YÜZDESİYLE DEĞİL, BANT YÜKSEKLİĞİYLE yapıldı:
            // ekran yüzdesi ×1,51, ekran yüksekliği ×1,17 diyordu — çelişki
            // referans karesinin (384x832) bizim tuvalden (1080x1920) farklı
            // en-boyda olmasından geliyor. İki bandın yüksekliği neredeyse
            // aynı (%18,6 / %19,3), o yüzden "çarpı / bant" oranı ikisinden
            // de bağımsız tek doğru ölçü:
            //     referans %25,2   bizim %20,8   ->  x1,21
            //
            // Yatay merkez de düzeltildi: referans %86,2, bizimki %86,85'ti.
            //
            // DERS (iki ekran farklı en-boydaysa, ORTAK BİR ÜÇÜNCÜ ŞEYE
            // oranla): Ekranın kendisi ortak değil; ama iki tasarımda da olan
            // ve boyu tutan bir öge (burada başlık bandı) ortak cetvel olur.
            var root = UiKit.CreateRect("Close", band);
            UiKit.Place(root, 0.8075f, 0.0733f, 0.9165f, 0.5727f);

            // GÖLGE YOK (15. tur). Kapat çarpısına da yuva gölgesi eklenmişti;
            // kullanıcı: *"settingste kapatma butonunun üst kısmı çok mor
            // olmuş."* Sebep: çarpı hazır bir GÖRSEL ve kendi gölgesi zaten
            // içinde; üstüne konan yarı saydam yuva, altındaki mor panelle
            // karışıp daire çevresinde mor bir hâle bırakıyor.
            //
            // DERS (kendi gölgesi olan bir görsele gölge ekleme): Prosedürel
            // katmanlarla kurulan düğmelerde yuva doğru; hazır PNG'lerde
            // gölge zaten baskılı.

            var ringImage = CloseGlyph(root);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = ringImage;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            button.onClick.AddListener(onClick);
            return button;
        }

        /// <summary>
        /// Kapat çarpısının GÖRÜNEN yüzü — verilen kökü tamamen doldurur ve
        /// ışın hedefi olarak döndürülür.
        ///
        /// NEDEN TEK YERDE (13. tur, G3): Bu çarpı altı ayrı yerde ayrı ayrı
        /// kuruluyordu (menü başlığı, duraklat, sonuç, yeniden dene, günlük
        /// ödül, yeni eşya) ve hepsi aynı üç prosedürel katmanı tekrar
        /// yazıyordu. Kullanıcı *"klasik kapat X işareti orijinalden
        /// alınacak, bizdeki çok kötü"* deyince altı yeri birden değiştirmek
        /// gerekti — o yüzden önce tek yere indirildi.
        ///
        /// Görsel varsa o kullanılıyor; yoksa eski üç katman çiziliyor, yani
        /// varlık eksikse ekran boş kalmıyor.
        ///
        /// DERS (aynı kontrolü altı yere kopyalamak, altı kat değişiklik
        /// demek): Tekrar eden bir görsel parçayı ilk kopyalarken değil,
        /// ilk DEĞİŞTİRİRKEN pahalıya alıyorsun.
        /// </summary>
        /// <summary>
        /// Kapat çarpısının görseli; yoksa null (çağıran prosedürel çizime
        /// düşer). `CreateIconButton` kullanan yerler kökü kendisi kurduğu
        /// için <see cref="CloseGlyph"/> yerine bunu kullanıyor.
        /// </summary>
        public static Sprite CloseArt => UiSkin.Get(Art.Close);

        public static Image CloseGlyph(Transform root)
        {
            var sprite = UiSkin.Get(Art.Close);
            if (sprite != null)
            {
                var img = UiKit.CreateIcon("Ring", root, sprite);
                img.preserveAspect = true;
                img.raycastTarget = true;
                UiKit.Place(img, 0f, 0f, 1f, 1f);
                return img;
            }

            // YEDEK: görsel yoksa eski prosedürel çarpı.
            //
            // DERS (bu projede aynı hata ÜÇÜNCÜ kez): Düz bir `Image`
            // sprite'ı kutuya GERER; `CreateIcon` en-boy oranını KORUR.
            // Daire çizen her yerde ikincisi kullanılmalı.
            var ringImage = UiKit.CreateIcon("Ring", root, GameKit.UI.UiSprites.Circle,
                new Color(0.42f, 0.05f, 0.09f));
            ringImage.raycastTarget = true;
            UiKit.Place(ringImage, 0f, 0f, 1f, 1f);

            var faceImage = UiKit.CreateIcon("Face", root, GameKit.UI.UiSprites.Circle, CloseRed);
            UiKit.Place(faceImage, 0.07f, 0.09f, 0.93f, 0.95f);

            var cross = UiKit.CreateIcon("Cross", faceImage.transform, GameKit.UI.UiSprites.Cross);
            UiKit.Place(cross, 0.26f, 0.26f, 0.74f, 0.74f);
            return ringImage;
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
        /// <param name="maxRadius">
        /// Yarıçap TAVANI. Varsayılan <see cref="UiCornerFit.MaxRadius"/> (34
        /// birim) panel ve kartlar için ölçülmüştü; büyük bir kapsülde o tavan
        /// devreye girip şekli köşeli bırakıyor. Yolculuk'un kilometre taşı
        /// kapsülleri gibi bilerek çok yuvarlak olan yüzeyler tavanı kaldırır.
        /// Tavanı GENEL olarak yükseltmek yanlış olur — bkz. UiCornerFit'teki
        /// "kapsam hatası" dersi.
        /// </param>
        public static Image Capsule(string name, Transform parent, Color color,
                                    float cornerShare = UiCornerFit.HouseShare,
                                    float maxRadius = UiCornerFit.MaxRadius)
        {
            var image = UiKit.CreateRoundedPanel(name, parent, color, cornerShare, maxRadius);
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
        /// <summary>Yazının konturu — koyu kalmalı, okunaklılık ondan geliyor.</summary>
        public const float OutlineTone = 0.15f;

        /// <summary>
        /// Düğmenin DIŞ ÇİZGİSİ. Yazının konturundan AYRI (15. tur).
        ///
        /// Kullanıcı: *"basılı tutmadan normal hâlinde dış çizgileri siyah,
        /// yumuşak değil, o yüzden referanstakine benzemiyor."* Ölçüm onayladı:
        ///
        /// ÖLÇÜM (referans `btn_050`, yatay orta hat, dıştan içe):
        ///     gölge (parlaklık 33-42, 5 px)
        ///   → KOYU YEŞİL kenar (15,146,22) ≈ 93, 15 px
        ///   → kaymak çizgisi (79,220,42) ≈ 158
        ///   → yüz (43,195,15) ≈ 129
        /// Yani referansta SİYAH bir halka HİÇ YOK; en koyu şey gölgenin
        /// kendisi. Bizde ise yüz × 0,15 = parlaklık 24'lük, yani neredeyse
        /// siyah bir halka vardı ve düğmeyi zeminden "kesiyordu".
        ///
        /// 0,15 → 0,40 denendi: halka koyu YEŞİLE dönüyordu ve kullanıcı bunu
        /// reddetti — *"koyu yeşil fikrini sevmedim, siyah olacak ama aşırı
        /// siyah değil, yumuşatılmış."* Yani istenen şey rengin değişmesi değil,
        /// SERTLİĞİN azalması. 0,24: hâlâ siyah okunuyor (parlaklık ≈38) ama
        /// yüzün rengini bir tık taşıdığı için kenar "kesik" değil "dökümlü"
        /// duruyor. Zeminden ayırma işini zaten dışındaki yuva gölgesi yapıyor
        /// (bkz. <see cref="AttachShadow"/>).
        ///
        /// DERS (bir sabit iki işi birden yapıyorsa, biri yanlıştır): Aynı sayı
        /// hem yazının konturunu hem düğmenin kenarını besliyordu. Yazının
        /// konturu KOYU olmalı (okunaklılık), düğmenin kenarı olmamalı
        /// (yumuşaklık). Ayrılmadan ikisi birden doğru olamazdı.
        /// </summary>
        public const float EdgeTone = 0.24f;
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
            // Hedef grafik ADIYLA alınıyor: "ilk çocuk Image" varsayımı 15.
            // turda kırıldı, çünkü artık ilk çocuk DIŞ GÖLGE.
            var outlineImage = root.Find("Outline");
            button.targetGraphic = outlineImage != null
                ? outlineImage.GetComponent<Image>()
                : root.GetComponentInChildren<Image>();
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

            // 0) DIŞ GÖLGE — düğmenin OTURDUĞU zemin.
            //
            // NEDEN VAR (15. tur): Kullanıcı — *"buton basılı değilken
            // kenarlarının dışında transparan gölge gibi bir şey var; basılı
            // tutunca buton daha da içeri geçiyor ve dışarıdaki o boşluk
            // artıyor."*
            //
            // ÖLÇÜM (referans videosu, ana ekranın "Level 10" düğmesi,
            // 281×110 px): serbest hâlde düğmenin ALTINDA 17 px, YANLARINDA
            // 2 px koyu alan var ve bu alan düğme basılıp 273×107'ye
            // küçülürken YERİNDE KALIYOR — yani boşluk büyüyor. Bizde gölge
            // hiç yoktu, o yüzden %3'lük küçülme gözle fark edilmiyordu:
            // düğme küçülüyor ama arkasından ARKA PLAN çıkıyordu, gölge değil.
            //
            // Oranlar yükseklikten türetiliyor (17/110 ≈ %15, 2/281 ≈ %0,8),
            // böylece HUD'un küçük kare düğmesinde de kapsül düğmede de aynı
            // görünüyor.
            //
            // DERS (bir hareketi GÖRÜNÜR yapan şey, hareket eden değil DURAN
            // katmandır): Basış ölçeği referansla aynıydı ama her katman
            // birlikte küçüldüğü için kıyaslanacak sabit bir kenar yoktu.
            // Gölge sabit kalınca aynı %3 anında okunuyor.
            // Gölge düğmenin KENDİ rengini taşıyor (nötr siyah, mor düğmenin
            // altında gri bir hâle bırakıyordu).
            AttachShadow(root, cornerShare, color);

            // 1) DIŞ ÇİZGİ — düğmeyi zeminden kesen çok koyu halka.
            // Dokunmayı da bu yakalıyor: en dıştaki katman, tıklanabilir
            // alanın gerçek sınırı.
            var outline = UiKit.CreateRoundedPanel("Outline", root,
                Darken(color, EdgeTone), cornerShare, NoRadiusCap);
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

        /// <summary>
        /// Tek görselden ikon düğmesi — yüzü <see cref="MenuSprites.PlastikKare"/>
        /// (H1/H2/H3, 14. tur).
        ///
        /// <see cref="IconButton"/> yüzü halka + yüz katmanlarıyla kuruyor ve
        /// düz renk veriyor; referansın üst çubuk düğmelerinde ise hacim var:
        /// ince koyu kenar, üstte dar bir parlaklık, aşağı sönen gövde, altta
        /// kalın koyu bir kalınlık. Bunların hepsi tek dokuda.
        /// </summary>
        public static Button PlateIconButton(string name, Transform parent, Sprite icon,
                                             Sprite plate, float iconInset = 0.08f,
                                             Color? iconTint = null)
        {
            var root = UiKit.CreateRect(name, parent);

            // Gölge kapsül düğmedekiyle aynı reçeteden (15. tur): basışta
            // yerinde kalır, düğme onun içine gömülür.
            AttachShadow(root, SquareCornerShare);

            var yuz = UiKit.CreateIcon("Plate", root, plate);
            yuz.type = Image.Type.Sliced;
            yuz.preserveAspect = false;
            yuz.raycastTarget = true;                 // dokunuşu bu yakalıyor
            UiKit.Place(yuz, 0f, 0f, 1f, 1f);

            if (icon != null)
            {
                var glyph = UiKit.CreateIcon("Icon", root, icon, iconTint ?? Color.white);
                glyph.raycastTarget = false;
                UiKit.Place(glyph, iconInset, iconInset, 1f - iconInset, 1f - iconInset);
            }

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = yuz;
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            return button;
        }

        /// <summary>
        /// Düğmenin ALTINA serilen yumuşak gölge — ve basışta yerinde kalan
        /// tek katman (<see cref="GameKit.UI.UiButtonFeel"/> "Shadow" adını
        /// arayıp ters ölçekliyor).
        ///
        /// ÖLÇÜM (referans, 281×110 px'lik "Level 10" düğmesi): gölge ALTTA
        /// 17 px (yüksekliğin %15'i), YANLARDA yalnız 2 px (genişliğin
        /// %0,7'si), ÜSTTE yok. Yani bu bir çerçeve değil, düğmenin altına
        /// düşen bir gölge.
        ///
        /// DERS (yanlardan taşan gölge KONTURA dönüşür): İlk denemede yanlara
        /// genişliğin %1,8'i verilmişti; kullanıcı ekran görüntüsünde bunu
        /// "kalın siyah çerçeve" olarak gördü. Referansta yan pay neredeyse
        /// yok — gölgenin yönü var, ışık yukarıdan geliyor.
        ///
        /// Üç katman, dıştan içe koyulaşıyor: keskin kenarlı tek panel şerit
        /// gibi okunuyor, üç kademe bulanıklığı taklit ediyor.
        /// </summary>
        /// <param name="tint">
        /// Gölgenin RENGİ (alfası yok sayılır). Boşsa siyah.
        ///
        /// NEDEN (15. tur): Saf siyah gölge, mor düğmenin altında GRİ bir hâle
        /// bırakıyordu — kullanıcı "watch ad kısmında hâlâ problem var" derken
        /// gördüğü şey buydu. Referansta gölge nötr değil: yeşil düğmenin
        /// altındaki koyu alan (3,81,22), yani düğmenin kendi yeşilinin çok
        /// koyu hâli.
        ///
        /// DERS (gölge renksiz değildir): Işığı kesen yüzey, altındakinin
        /// rengini de taşır. Nötr siyah bir yığın, doygun renkli bir düğmenin
        /// çevresini kirli griye çeviriyor.
        /// </param>
        public static RectTransform AttachShadow(RectTransform root,
                                                 float cornerShare = ButtonCornerShare,
                                                 Color? tint = null)
        {
            Color ink = tint.HasValue ? Darken(tint.Value, 0.12f) : Color.black;
            var shadow = UiKit.CreateRect("Shadow", root);
            UiKit.Place(shadow, 0f, 0f, 1f, 1f);
            shadow.SetAsFirstSibling();
            var fit = shadow.gameObject.AddComponent<GameKit.UI.UiRingLayout>();

            // --- 1) DÜŞEN GÖLGE (en arkada) — yalnız AŞAĞI uzar -----------
            // ÖLÇÜM: referansta düğmenin altındaki koyu alan 17 px / 110 px
            // yükseklik = %15. Yanlara taşma yok, bu yüzden yatayda düğmeyle
            // aynı genişlikte kalıyor.
            Drop("Drop3", -0.150f, 0.070f);
            Drop("Drop2", -0.100f, 0.080f);
            Drop("Drop1", -0.058f, 0.095f);

            // --- 2) YUVA KONTÜRÜ — dört kenarı SARAN koyuluk ---------------
            //
            // Kullanıcı (15. tur, üçüncü geçiş): *"sabit koyu renkte bir dış
            // çizgi var; basılı tutunca buton içe giriyor ve dış çizgiyle
            // buton arasında, butonun etrafını saran bir koyuluk oluşuyor,
            // kontür gibi."*
            //
            // ÖLÇÜM (referans, `btn_050`/`btn_060`, düğme 281×110 px):
            //   yatay orta hat, SOL kenar
            //     serbest : x 135-139 koyu (parlaklık 33-42), sonra yeşil kenar
            //     basılı  : x 135-143 koyu (parlaklık 23-40), yani bant
            //               5 px'ten 9 px'e ÇIKIYOR ve KOYULAŞIYOR
            //   sağ kenarda aynı: 5 px → 9 px, parlaklık 33-48 → 24-36
            //   ÜST kenarda da var (y 53-57), yani bu bir alt gölge değil,
            //   düğmeyi çepeçevre saran bir YUVA.
            //   Arka planın parlaklığı ~105-130; bandın dibi ~33 → %70 koyuluk.
            //
            // Yani düğme, zemine oyulmuş koyu bir yuvanın içinde duruyor.
            // Basınca küçülüyor, yuva yerinde kalıyor ve aradaki koyuluk
            // görünür oluyor. Kullanıcının tarif ettiği şey tam olarak bu.
            //
            // DERS (gölge ≠ yuva): Düşen gölge ışığın yönünü anlatır ve
            // AŞAĞI düşer; yuva ise derinliği anlatır ve DÖRT KENARI sarar.
            // İkisi ayrı katman: yalnız düşen gölge koyunca düğme zeminin
            // üstünde duran bir etiket gibi kalıyordu.
            //
            // Paylar `UiRingLayout` üzerinden NEGATİF veriliyor: dört kenara
            // da yüksekliğin oranı kadar AYNI BİRİM ekleniyor. Oranla
            // verilseydi 3:1 bir düğmede yan kontür üç kat kalın çıkardı —
            // ilk denemedeki "kalın siyah çerçeve" hatası tam olarak buydu.
            // ALFALAR ÖLÇÜMLE AYARLANDI, HESAPLA DEĞİL: proje LINEAR renk
            // uzayında karıştırıyor, yani %30 siyahın ekrandaki koyultması
            // sRGB aritmetiğinin söylediğinden az. İlk deneme (0,15-0,30)
            // kâğıtta %65 koyultma veriyordu, ekranda ölçülen %47 çıktı.
            // Bu değerler ekran görüntüsünden ölçülerek referansın bandına
            // (%65-70) oturtuldu.
            // BİR TIK AÇILDI (kullanıcı: "iyi olmuş ama çok azıcık
            // düşürebiliriz"): yığın ~%15 hafifledi, geometri aynı kaldı.
            Socket("Socket4", -0.052f, 0.145f);
            Socket("Socket3", -0.035f, 0.200f);
            Socket("Socket2", -0.021f, 0.310f);
            Socket("Socket1", -0.010f, 0.440f);
            return shadow;

            void Drop(string layerName, float dropShare, float alpha)
            {
                var image = Panel(layerName, alpha);
                UiKit.Place(image, 0f, dropShare, 1f, 1f + dropShare * 0.10f);
            }

            void Socket(string layerName, float outset, float alpha)
            {
                var image = Panel(layerName, alpha);
                fit.Add(image.rectTransform, outset);
            }

            Image Panel(string layerName, float alpha)
            {
                var image = UiKit.CreateRoundedPanel(layerName, shadow,
                    new Color(ink.r, ink.g, ink.b, alpha), cornerShare, NoRadiusCap);
                image.raycastTarget = false;
                return image;
            }
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

        // 13. TURDA VİDEODAN YENİDEN ÖLÇÜLDÜ. Yukarıdaki değerler tek bir
        // JPEG ekran görüntüsünden alınmıştı; `Block Out! menus,powerups,vs.mp4`
        // 53,6-55,4 sn arası 109 karenin ORTALAMASI (`_Reference/notes/
        // ayarlar_avg.png`) kodek gürültüsünden arınmış bir "Ayarlar" ekranı
        // verdi ve dört renk de kaydı.

        /// <summary>Yuvanın dış bileziği (#9081FE) ve içi — ÖLÇÜM (39,30,105).</summary>
        static readonly Color SwitchRim  = new Color(0.565f, 0.506f, 0.996f);
        static readonly Color SwitchDark = new Color(0.153f, 0.118f, 0.412f);
        /// <summary>Çipin yüzeyi — ÖLÇÜM: gövde medyanı (52,169,20).</summary>
        static readonly Color SwitchOn   = new Color(0.204f, 0.663f, 0.078f);

        /// <summary>
        /// KAPALI çipin yüzeyi (A1). Referansta kapalı bir satır YOK — video
        /// dört ayarı da açık gösteriyor — ama kullanıcı açıkça istedi:
        /// *"On/Off anahtarı: ON iken yeşil, OFF iken KIRMIZI düğme"*.
        /// Oyunun kendi kırmızısı kullanılıyor ki panel içinde yabancı
        /// durmasın (aynı kırmızı "Quit" düğmesinde de var).
        /// </summary>
        static readonly Color SwitchOff  = Red;

        /// <summary>Çipin üstündeki yazı — ÖLÇÜM (5,67,1), koyu yeşil.</summary>
        static readonly Color SwitchOnInk = new Color(0.020f, 0.263f, 0.004f);
        /// <summary>KAPALI çipin üstündeki yazı: kırmızının koyusu.</summary>
        static readonly Color SwitchOffInk = new Color(0.259f, 0.020f, 0.012f);

        /// <summary>
        /// Sönük yarının yazısı — ÖLÇÜM (124,112,226).
        ///
        /// A2 (kullanıcı: *"'on / off' yazıları fazla koyu görünüyor"*).
        /// Eski değer (71,57,208) koyu yuvanın üstünde zar zor okunuyordu;
        /// referansta bu yazı belirgin biçimde daha AÇIK bir leylak.
        /// Eski ölçüm tek bir JPEG karesinden alınmıştı ve kodek, koyu
        /// zemindeki ince harfleri zemine doğru çekmişti.
        ///
        /// DERS (koyu zemindeki ince yazıyı TEK kareden ölçme): Sıkıştırma
        /// en çok düşük kontrastlı ince ayrıntıyı bozar. Sabit bir sahnede
        /// yüzlerce kareyi ortalamak bu yazıyı 53 birim açığa çıkardı.
        /// </summary>
        /// 15. TUR: kullanıcı hâlâ okuyamıyor — *"on/off yazıları bir tık daha
        /// aynı tonda ama daha açık olsun, koyuluktan okunmuyor."* Aynı leylak
        /// ailesi, %22 daha açık: (124,112,226) -> (158,146,242).
        static readonly Color SwitchIdleInk = new Color(0.620f, 0.573f, 0.949f);

        /// <summary>Kurulan anahtarın parçaları; durumu <see cref="SetOn"/> çevirir.</summary>
        public sealed class SwitchView
        {
            public RectTransform Root;
            /// <summary>Sol yarının dokunma yüzeyi (görsel olarak yuvanın kendisi).</summary>
            public Image OffFace;
            /// <summary>Sağ yarının dokunma yüzeyi.</summary>
            public Image OnFace;
            public PillTint Chip;
            public RectTransform ChipRect;
            public TMPro.TextMeshProUGUI OffText, OnText;

            /// <summary>
            /// Çip AÇIKKEN sağda yeşil, KAPALIYKEN solda KIRMIZI (A1).
            ///
            /// Eskiden çip hep sağda duruyor ve kapanınca yuvanın koyusuna
            /// düşüyordu — yani "sönmüş" bir düğme. Kullanıcı bunun yerine
            /// KIRMIZI istedi. Kırmızı bir düğme sağda, "On" yazısının
            /// üstünde durursa yazının söylediğinin tersini gösterir; bu
            /// yüzden çip yalnız renk değil YER de değiştiriyor.
            /// </summary>
            public void SetOn(bool on)
            {
                UiKit.Place(ChipRect,
                    on ? ChipRightX0 : ChipLeftX0, ChipY0,
                    on ? ChipRightX1 : ChipLeftX1, ChipY1);
                Chip.Color   = on ? SwitchOn      : SwitchOff;
                OnText.color = on ? SwitchOnInk   : SwitchIdleInk;
                OffText.color= on ? SwitchIdleInk : SwitchOffInk;
            }
        }

        // ÇİPİN İKİ DURAĞI — 13. turda videodan yeniden ölçüldü.
        //
        // Eski değerler (0,60..1,065) çipi yuvanın sağ ucundan TAŞIRIYORDU;
        // yorum bunu "referanstaki kabartma" diye açıklıyordu ve kaynak tek
        // bir JPEG'di. `ayarlar_avg.png` üzerinde anahtar satırının yatay
        // kesiti alınınca gerçek şu çıktı:
        //
        //     yuva (ray)  x %63,8..%92,2   ->  genişlik %28,4
        //     yeşil çip   x %78,9..%91,7   ->  genişlik %12,8
        //
        // Yani çip yuvanın İÇİNDE ve sağ ucuna DAYALI (fark %0,5), taşmıyor.
        // Yuvaya göre normalize edilince: %53..%100.
        // Dikeyde de içeride: yuva y %74,28..%79,33, çip %75,0..%79,1 —
        // yani yuva yüksekliğinin %81'i, ortalanmış.
        //
        // DERS (tek karelik bir JPEG'e dayanan "ölçüm" bir tahmindir):
        // Taşma iddiası ölçülmüş gibi yazılmıştı ama kaynağı sıkıştırılmış
        // tek bir kareydi; çipin parlak kenarı zemine taşınca dışarı çıkmış
        // gibi görünüyor. Sabit sahnenin kare ortalaması bunu çözdü.
        const float ChipRightX0 = 0.53f, ChipRightX1 = 1.00f;
        const float ChipLeftX0  = 0.00f, ChipLeftX1  = 0.47f;
        const float ChipY0 = 0.095f, ChipY1 = 0.905f;

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

            // İKİ DOKUNMA YÜZEYİ ALTTA, ÇİP ÜSTTE (13. tur).
            //
            // Eskiden sağ yarının dokunma yüzeyi ÇİPİN dış konturuydu. Çip
            // artık yer değiştirdiği için (A1) o kurgu çöküyordu: çip sola
            // gidince sağ yarı ölü alan kalıyordu. Şimdi iki yarı da yuvanın
            // içinde SABİT birer yüzey; çip ve yazılar onların üstünde
            // duruyor ve hiçbiri ışın hedefi değil, yani dokunuş altlarındaki
            // yarıya geçiyor.
            //
            // DERS (hareket eden bir parçayı DOKUNMA HEDEFİ yapma): Tıklama
            // alanı kontrolün SABİT yarısına ait; görsel gösterge onun
            // üstünde gezinen ayrı bir katman.
            view.OffFace = Capsule("Off", slot.transform, SwitchDark);
            view.OffFace.raycastTarget = true;
            UiKit.Place(view.OffFace, 0.02f, 0.06f, 0.50f, 0.94f);

            view.OnFace = Capsule("On", slot.transform, SwitchDark);
            view.OnFace.raycastTarget = true;
            UiKit.Place(view.OnFace, 0.50f, 0.06f, 0.98f, 0.94f);

            // Çip yuvayı yanda VE dikeyde AŞIYOR: referanstaki kabartma.
            // İçine gömülü bir çip, anahtarı "iki renkli düz bir şerit"
            // gibi gösteriyordu. Konumunu SetOn koyuyor.
            var chip = PillBody("Chip", slot.transform, SwitchOn, out var layout, out view.Chip);
            view.ChipRect = chip;
            UiKit.Place(chip, ChipRightX0, ChipY0, ChipRightX1, ChipY1);
            foreach (var g in chip.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;

            // Yazılar EN SON ve çipten BAĞIMSIZ: çip altlarında kayıyor,
            // "Off" hep solda "On" hep sağda kalıyor.
            //
            // Kutular ÇİP DURAKLARIYLA AYNI MERKEZDE. İlk denemede sabit
            // yarılara (0,04-0,46 / 0,54-0,96) konmuşlardı ve yazı çipin
            // merkezinden %8 kayıyordu — kırmızı çipin üstünde "Off" sağa
            // yaslanmış duruyordu. Referansta "Kapalı" da ray içinde
            // %5,3..%41,2 aralığında, yani sol durağın merkezinde.
            view.OffText = UiKit.CreateTitle("OffText", slot.transform, "Off",
                fontSize, SwitchIdleInk, new Color(0.12f, 0.10f, 0.28f));
            UiKit.Place(view.OffText, ChipLeftX0 + 0.03f, 0.06f, ChipLeftX1 - 0.03f, 0.94f);
            view.OffText.characterSpacing = LabelTracking;
            view.OffText.raycastTarget = false;

            view.OnText = UiKit.CreateTitle("OnText", slot.transform, "On", fontSize,
                SwitchOnInk, new Color(0.63f, 1f, 0.45f));
            UiKit.Place(view.OnText, ChipRightX0 + 0.03f, 0.06f, ChipRightX1 - 0.03f, 0.94f);
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
