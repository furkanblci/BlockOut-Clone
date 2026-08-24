using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Menü kabuğu: alt sekme çubuğu ve onun açtığı ekranlar (Mağaza,
    /// Liderlik Panosu, Yolculuk, Profil). "Ana Ekran" sekmesi hiçbir panel
    /// göstermez — altındaki <see cref="HomeScreen"/> görünür kalır.
    ///
    /// DERS (kabuk / içerik ayrımı): Sekme çubuğu, üst bar ve geçiş mantığı
    /// TEK yerde durur; her ekran yalnız kendi içeriğini kurar. Beş ekranın
    /// her birine ayrı bir "geri düğmesi + sekme çubuğu" kopyalamak, altıncı
    /// ekran eklendiğinde altı yerde düzeltme demekti.
    ///
    /// DERS (ayrı canvas, yüksek sıra): Kabuk kendi canvas'ını kurar ve
    /// HomeScreen'in üstünde çizilir. Böylece ana ekranın koduna hiç
    /// dokunmadan menü sistemi eklenebiliyor — mevcut çalışan bir ekranı
    /// yeniden yazmak, çalışmayan bir ekran riski demektir.
    /// </summary>
    public sealed class MenuShell : MonoBehaviour
    {
        static readonly (string label, string key, string icon)[] Tabs =
        {
            ("Shop",       "store",   Art.Shop),
            ("Leaderboard","board",   Art.Trophy),
            ("Home",       "home",    Art.Home),
            ("Journey",    "journey", Art.Globe),
            // Sekme ikonu SANDIK DEĞİL ALBÜM (22. madde): referansta koleksiyon
            // sekmesi kitabın yanındaki albüm paketlerinin küçük hâli.
            ("Collection", "collection", Art.Album)
        };

        readonly Dictionary<string, RectTransform> _screens = new Dictionary<string, RectTransform>();
        // Kart artık TEK bir Image değil (kenar + yüzey + ışık), o yüzden
        // seçim `enabled` ile değil `SetActive` ile açılıp kapanıyor.
        readonly List<(Button button, RectTransform card, RectTransform icon, TextMeshProUGUI label, string key)>
            _tabButtons = new List<(Button, RectTransform, RectTransform, TextMeshProUGUI, string)>();

        string _active = "home";

        /// <summary>
        /// Sekme görselleri (kart, yazı, ikon konumu) en az bir kez uygulandı mı?
        ///
        /// DERS (erken çıkış İLK ÇALIŞMAYI atlamamalı): 8. bulgunun düzeltmesi
        /// "aynı sekmeye basmak hiçbir şey yapmasın" diye <see cref="Show"/>
        /// başına bir koruma koydu. Ama `_active` zaten "home" ile başlıyor ve
        /// kurulum `Show("home")` ile bitiyor — yani o çağrı da korumaya takıldı
        /// ve sekme görselleri HİÇ uygulanmadı. Sonuç: beş sekmenin de kartı ve
        /// yazısı açık kaldı, hepsi seçiliymiş gibi göründü.
        /// "Durum değişmediyse çık" koruması, durumun bir kez UYGULANDIĞINI da
        /// bilmek zorunda; yoksa "değişmedi" ile "hiç yazılmadı" karışır.
        /// </summary>
        bool _applied;

        RectTransform _bar, _tabBarRoot;

        /// <summary>Çubukla birlikte gizlenecek süsler (ayraç çizgileri).</summary>
        readonly List<RectTransform> _tabDecor = new List<RectTransform>();

        /// <summary>
        /// Sekme çubuğunun GÖRÜNMEMESİ gereken ekranlar.
        ///
        /// Referansta Ayarlar ve Profil TAM EKRAN örtü sayfaları; alt çubuk
        /// orada yok, kapanışları sağ üstteki kırmızı çarpı
        /// (`WhatsApp Image ... (2).jpeg` ve `profil.jpeg` ile doğrulandı).
        ///
        /// DERS (kardeş sırası YETMEZ, kardeş OLMAK gerekir): Bu iki ekran
        /// `SetAsLastSibling()` ile "çubuktan sonra çizilsin" diye
        /// işaretlenmişti ve mantık doğruydu — ama çubuk onların kardeşi
        /// DEĞİL: ekranlar `Content`'in, çubuk ise `SafeArea`'nın çocuğu.
        /// Sıralama yalnız aynı ebeveyn altında anlam taşır, o yüzden çubuk
        /// her ekranın üstünde kalmaya devam ediyordu ve Ayarlar'ın en
        /// altındaki "Hesabımı Sil" düğmesi onun arkasında kayboluyordu
        /// (13. ve 14. APK bulguları).
        /// </summary>
        static readonly string[] FullScreenPages = { "settings", "profile" };
        RectTransform _content;

        /// <summary>Referanstan ölçüldü: geçiş ~170 ms (30 kare/sn'de ~5 kare).</summary>
        const float SlideSeconds = 0.17f;

        /// <summary>Ana ekrandaki dişli düğmesi buraya bağlanır.</summary>
        public static MenuShell Instance { get; private set; }

        /// <summary>
        /// Şu an açık olan sekmenin anahtarı.
        ///
        /// DERS (durumu HİYERARŞİDEN okuma): Kaydırma geçişini doğrularken
        /// "hangi ekran açık" sorusunu `Content`'in ilk aktif çocuğuna bakarak
        /// cevaplamıştım. Yanlış cevap verdi: <see cref="SlideSwap"/> ÇIKAN
        /// ekranı tween bitene kadar açık bırakıyor, yani geçiş sırasında iki
        /// ekran birden aktif ve hangisinin "gerçek" olduğu kardeş sırasına
        /// kalıyor. Görünür durum ile mantıksal durum aynı şey değil; ölçmek
        /// isteyen mantıksal olanı okumalı.
        /// </summary>
        public string ActiveKey => _active;

        void Awake() => Instance = this;

        void Start()
        {
            var canvas = UiKit.CreateCanvas("MenuCanvas");
            // Kanvası sahibinin altına al: menü kökü kapatılınca ekrandan da gitsin.
            canvas.transform.SetParent(transform, worldPositionStays: false);
            canvas.sortingOrder = 10;                 // ana ekranın üstünde
            var root = UiKit.CreateSafeArea(canvas);

            // İçerik alanı: sekme çubuğunun üstünde kalan her şey.
            var content = UiKit.CreateRect("Content", root);
            UiKit.Place(content, 0f, 0.105f, 1f, 1f);
            _content = content;

            _screens["store"]   = StoreScreen.Build(content);
            _screens["board"]   = LeaderboardScreen.Build(content);
            _screens["journey"] = JourneyScreen.Build(content);
            _screens["profile"] = ProfileScreen.Build(content);
            _screens["collection"] = CollectionScreen.Build(content);
            _screens["settings"] = (RectTransform)SettingsScreen.Build(content).transform;

            BuildTabBar(root);

            // Ayarlar ve Profil ÖRTÜ sayfalarıdır: referansta tam ekran ve
            // sekme çubuğunu da kapatıyorlar (kapanışları sağ üstteki kırmızı
            // çarpı). Kardeş sırası çizim sırası olduğu için çubuktan SONRAYA
            // alınıyorlar; yoksa çubuk sayfanın üstünde kalır ve "tam ekran"
            // hissi bozulur.
            _screens["settings"].SetAsLastSibling();
            _screens["profile"].SetAsLastSibling();
            _tabBarRoot = root;

            // Yatay kaydırmayla sekme geçişi. Ekranlar kurulduktan SONRA
            // ekleniyor: bileşen ilk karesinde raycast yapıyor ve hedefler
            // hazır olmalı.
            if (GetComponent<MenuSwipeNavigator>() == null)
                gameObject.AddComponent<MenuSwipeNavigator>();

            Show("home");
        }

        /// <summary>
        /// Alt sekme çubuğu.
        ///
        /// DERS (seçili sekme nasıl anlaşılır): İlk hâlde beş sekmenin de altında
        /// yazı vardı ve seçili olan yalnız biraz büyüyordu — beş etiket yan yana
        /// çubuğu kalabalıklaştırıyor, hangisinin seçili olduğu da zayıf kalıyordu.
        /// Referans oyun tek bir şey yapıyor: SEÇİLİ sekme çubuğun üstüne çıkan
        /// kendi kartına oturuyor ve YAZI YALNIZ ONDA görünüyor. Diğerleri sade
        /// ikon. Böylece hem çubuk sakinleşiyor hem de seçim tek bakışta okunuyor
        /// — üstelik renk değil KONUM ve YÜKSEKLİK farkıyla, yani renk körlüğünde
        /// de çalışır.
        /// </summary>
        void BuildTabBar(Transform root)
        {
            // Çubuk güvenli alanda kalır (düğmeler parmakla erişilebilir olmalı)
            // ama BOYASI aşağı taşar: güvenli alan ekranın altından içeri
            // girdiği için zemin orada bitiyor ve altında manzara görünüyordu.
            // Kanvasa taşımayı denedim; kanvasın çocuğu olarak güvenli alandan
            // SONRA çizilip düğmelerin üstünü kapattı.
            // ÇUBUK ARTIK PROSEDÜREL — hazır görsel YANLIŞ RENKTE.
            //
            // DERS (boyayamayacağın rengi hazır görselde arama): Çubuk
            // `bar_tabs.png`, seçili kart `card_tab.png` görselini kullanıyordu.
            // İkisi de MOR (#5B1FB8 civarı); referanstaki çubuk ise MAVİ-MOR
            // (#5140E4, ölçüldü). Boyama çarpma olduğu için moru maviye
            // çevirmek mümkün değil — çarpım her zaman daha koyu ve daha mor
            // kalır. Kullanıcının "alt menü için daha orijinale benzer bir
            // arkaplan lazım" dediği şeyin ölçülebilir kısmı buydu.
            //
            // Referansın 3B plastik dudağı DÖRT ince şeritten oluşuyor ve
            // hepsi ölçüldü (`ana ekran.jpeg`, x=60 dikey tarama):
            //   1836-1843 koyu dış kenar #2D1B87
            //   1845      ince ışık      #5C4BD8
            //   1846-1852 koyu oyuk      #231578
            //   1861-1868 üst parlaklık  #7771F9
            //   1868+     gövde          #5140E4
            // Dört dikdörtgen, tek bir "plastik" görselinin yaptığı işi
            // yapıyor — üstelik rengi istediğimiz an değişebiliyor.
            var bar = UiKit.CreatePanel("TabBar", root, BarColor);
            // YÜKSEKLİK YENİDEN ÖLÇÜLDÜ (14. tur, N1). Önceki sayı tek bir
            // ekran görüntüsünden alınmıştı; bu kez 52 menü karesinden aynı
            // durumu gösteren 8'i ortalandı (kodek gürültüsü kalmadı).
            // Kare 443x960, kanvas 1920 birim: 1 kare pikseli = 2 birim.
            // Çubuğun üst kenarı ekranın altından 186 birim = %9.69.
            UiKit.Place(bar, 0f, 0f, 1f, 0.0969f);

            // Boya HER ÜÇ KENARDAN taşar. Aşağısı güvenli alan için (eskiden
            // beri), YANLAR ise çubuk görselinin YUVARLAK UÇLARI yüzünden:
            // referansta çubuk kenardan kenara DÜZ gidiyor, bizde iki yanda
            // yuvarlanıp arkasındaki manzarayı gösteriyordu.
            bar.rectTransform.offsetMin = new Vector2(-90f, -220f);
            bar.rectTransform.offsetMax = new Vector2(90f, 0f);
            _bar = bar.rectTransform;

            // ÇUBUĞUN YÜZÜ TEK GRADYAN (14. tur, N1).
            //
            // Eskiden dört düz şerit (koyu kenar / ışık / oyuk / parlaklık) +
            // ayrı bir sönüm katmanı vardı. Şeritler doğru RENKLERİ taşıyordu
            // ama aralarındaki sıçramalar telefonda çizgi olarak görünüyordu —
            // kullanıcının "dış kenarları görünümü şu an kötü" dediği şey o
            // basamaklar. Toplam kalınlıkları da 39 birimdi, referansta 30.
            //
            // Görsel çubuğun GÖRÜNEN 186 biriminin tamamını kaplıyor ve
            // BİRE BİR oturuyor (186 doku pikseli = 186 kanvas birimi).
            // Aşağı taşan güvenli-alan payını çubuğun kendi düz rengi
            // dolduruyor; orası zaten yalnız çentikli cihazlarda görünür.
            var yuz = UiKit.CreateRect("Face", _bar);
            yuz.anchorMin = new Vector2(0f, 1f);
            yuz.anchorMax = new Vector2(1f, 1f);
            yuz.pivot = new Vector2(0.5f, 1f);
            yuz.sizeDelta = new Vector2(0f, 186f);
            yuz.anchoredPosition = Vector2.zero;
            var yuzImage = yuz.gameObject.AddComponent<Image>();
            yuzImage.sprite = MenuSprites.TabRim;
            yuzImage.type = Image.Type.Simple;
            yuzImage.raycastTarget = false;

            // Seçili kartın taşacağı alan çubuğun üstünde; bu yüzden kartlar
            // çubuğun DEĞİL kökün çocuğu, yoksa çubuk onları kırpar.
            float slot = 1f / Tabs.Length;

            // Sekmeler arasındaki ince dikey ayraçlar — referansta her komşu
            // sekme çifti arasında var. Çubuğu bölünmüş gösteriyor, yoksa beş
            // ikon tek bir şeridin üstünde yüzüyormuş gibi duruyor.
            for (int i = 1; i < Tabs.Length; i++)
            {
                var divider = UiKit.CreatePanel($"Divider_{i}", root, DividerColor);
                divider.raycastTarget = false;
                UiKit.Place(divider, i * slot - 0.0015f, 0.020f, i * slot + 0.0015f, 0.078f);

                // Çubukla birlikte gizlenmeliler: kökün çocuğu oldukları için
                // çubuğu kapatmak onları kapatmıyor ve tam ekran sayfalarda
                // ekranın altında iki dikey çizgi olarak kalıyorlardı.
                _tabDecor.Add(divider.rectTransform);
            }

            for (int i = 0; i < Tabs.Length; i++)
            {
                var (label, key, icon) = Tabs[i];

                var button = UiKit.CreateSpriteButton($"Tab_{key}", root, null,
                    null, 0, UiKit.Ink);
                UiKit.Place(button, i * slot, 0f, (i + 1) * slot, 0.095f);

                // Seçiliyken görünen kart. Çubukla aynı sebeple prosedürel:
                // `card_tab.png` mor, referanstaki kart mavi-mor (#6B65F9
                // yüzey, #291B8C kenar — ölçüldü).
                //
                // ÖLÇÜ (kart SLOTUNDAN GENİŞ): Referansta kart ekran
                // genişliğinin %28.3'ü (337-605 piksel), oysa bir sekme slotu
                // %20. Yani kart kendi slotunun 1.42 katı ve komşu slotlara
                // taşıyor — çubuğun üstünde "kabaran" hissi buradan geliyor.
                // Bizde kart slotun İÇİNE (0.05-0.95) sığdırılmıştı, yani
                // referansın yarısı kadar genişti ve seçim zayıf okunuyordu.
                // Kenar sekmelerde 24 birim ekran dışına taşıyor; çubuğun
                // boyası zaten ±90 taştığı için bu bir sorun değil.
                //
                // Kartın ÜST kenarı ekranın altından %13.48'te; düğme %9.5
                // yüksek olduğuna göre çarpan 0.1348/0.095 = 1.42. Eskisi
                // 1.73'tü — kart referanstan bir baş boyu uzundu.
                // ALT KENAR EKRANIN DIŞINA TAŞIYOR.
                //
                // DERS (bitmemesi gereken kenarı bitirme): Kart 0.06'da
                // bitiyordu ve yuvarlak alt köşeleri ekranın hemen üstünde
                // görünüyordu — çubuğun üstüne KONMUŞ ayrı bir kutu gibi.
                // Referansta kartın altı ekrandan taşıyor, yani kart çubuğun
                // İÇİNDEN çıkıyor gibi okunuyor. Görünmeyen 20 birim, kartın
                // neye ait olduğunu anlatan şey.
                //
                // GENİŞLİK (10. tur): ölçüldü, kart referansta ekranın
                // %27,3'ü (258/946), bizde %25,6 (277/1080) idi. Yayılım
                // 1,22 slottan 1,30'a çıktı. Yüksekliği ise ölçüm doğruladı:
                // referansta kartın üstü ekranın altından %13,5'te, bizde de
                // öyle — 1,42 çarpanı yerinde.
                var card = UiKit.CreateRect("Card", button.transform);
                // KUTU YENİDEN ÖLÇÜLDÜ (14. tur, N1 — 8 kare ortalamasından):
                //   • Kartın ÜSTÜ ekranın altından 230 birim. Bizimki 260'tı,
                //     yani bir baş boyu uzundu. Düğme kutusu 182,4 birim
                //     olduğuna göre çarpan 230/182,4 = 1,261.
                //   • Kart, sekme SLOTUNUN 1,524 katı geniş (referansta 270
                //     birim / 177,2 birimlik slot). Bizimki 1,30 slottu.
                //     Slot oranı olarak yazmak en-boy oranından bağımsız:
                //     dar telefonda da geniş tablette de aynı çıkıyor.
                //   • ALT KENAR EKRANIN DIŞINDA KALMALI. -0,10 (18 birim)
                //     yetmiyordu; köşe yarıçapı 44 olduğu için yuvarlak alt
                //     köşeler ekranın hemen üstünde görünüyor ve kart çubuğa
                //     KONMUŞ ayrı bir kutu gibi duruyordu. -0,252 = 46 birim,
                //     yarıçaptan büyük.
                UiKit.Place(card, -0.262f, -0.252f, 1.262f, 1.261f);

                // KÖŞELER EŞ MERKEZLİ OLMALI (7. tur, P56). Kullanıcı:
                // "Seçili butonun dış köşelerinde piksel bozulmaları var."
                //
                // Kartın koyu bandı her yerde 20 birim; öyleyse iç yüzeyin
                // köşe yarıçapı, dış yarıçaptan TAM 20 EKSİK olmalı. Bizde
                // dış 20/0.45 = 44,4 iken iç 20/0.50 = 40 idi — olması
                // gereken 24,4. İç köşe 15,6 birim FAZLA yuvarlaktı, yani
                // köşegen boyunca dış bandı yiyor, kenarlarda 20 birim olan
                // bant köşede 8-9 birime iniyordu. Göz bunu "köşe bozulmuş"
                // diye okuyor: bant aniden inceliyor, sonra yine kalınlaşıyor.
                //
                // DERS (iç içe iki yuvarlak dikdörtgen tek bir sayı paylaşır):
                // İki yüzeyin yarıçapını ayrı ayrı seçmek, aradaki bandın
                // KALINLIĞINI köşede değiştirmek demektir. Bant sabit
                // kalsın isteniyorsa iç yarıçap = dış yarıçap − bant.
                // (Aynı hata mağaza kartlarında da mümkün; orada
                // `UiCornerFit` iki yüzeye de aynı oranı verdiği için
                // yarıçaplar zaten kutu boyuyla birlikte değişiyor.)
                const float cardRadius = 44f;               // dış köşe yarıçapı
                // GÖLGE: kartın ARKASINDA, birkaç birim aşağıda.
                //
                // Kullanıcı: "güzel gölge de verelim, orijinal referanstaki
                // gibi benzesin." Referansta seçili kart çubuğun üstünde
                // DURUYOR gibi; bunu yapan şey altındaki koyu iz. Gölgesiz
                // kart, çubuğa boyanmış bir leke gibi okunuyor.
                //
                // Ayrı bir bulanık görsel gerekmiyor: aynı yuvarlak panel,
                // koyu ve yarı saydam, 10 birim aşağı kaydırılmış. Kartın
                // kendisi onun üstüne oturduğu için yalnız alt kenarda ve
                // yanlarda görünüyor.
                var cardShadow = UiKit.CreateRoundedPanel("Shadow", card,
                    new Color(0.055f, 0.031f, 0.192f, 0.55f));
                UiKit.SetSliceScale(cardShadow, UiKit.SliceScaleFor(cardRadius));
                cardShadow.raycastTarget = false;
                // Gölge karttan 7 birim DAHA GENİŞ ve 6 birim aşağıda.
                // Aynı boyda olsaydı kartın tamamen ARKASINDA kalırdı ve
                // yalnız ekranın dışına taşan alt kenarında görünürdü —
                // yani hiç görünmezdi. Gölgeyi gölge yapan şey, kaynağının
                // dışına taşan kısmı.
                UiKit.Place(cardShadow, 0f, 0f, 1f, 1f, padding: -7f);
                cardShadow.rectTransform.anchoredPosition += new Vector2(0f, -6f);

                // KART TEK GÖRSEL, DUDAĞI KENAR MESAFESİYLE (14. tur, N1).
                //
                // Eskiden üç katmandı: koyu rim + açık yüz + yüzün üstünde
                // `FadeDown` ışığı, ışığı kırpmak için de yüze bir `Mask`.
                // Ölçüm iki şeyi söyledi:
                //   • Yüzümüz (107,101,249), referans (93,70,245) — yeşil
                //     kanal 31 fazla, mor lavantaya kaçıyordu (aynı hata
                //     çubuğun gövdesinde de vardı, bkz. N2).
                //   • Referansta yüzün üstünde ışık YOK: kesitte üst
                //     (91,72,243), alt (94,70,246) — düz. Bizim eklediğimiz
                //     `Sheen` referansta olmayan bir parlaklıktı.
                // Ayrıca kartın dudağı çubuğun üst dudağının aynısı: aynı
                // dört bant, kartın dört kenarını da dolanıyor. İki
                // dikdörtgenle bunu yapmak köşede bandın kalınlığını
                // değiştiriyordu — 7. ve 8. turların ikisi de tam bu yüzden
                // harcanmıştı. Renk artık KENAR MESAFESİNDEN geliyor, bant
                // köşede de tam ölçüldüğü kalınlıkta
                // (bkz. MenuSprites.TabCard).
                var cardImage = UiKit.CreateIcon("Face", card, MenuSprites.TabCard);
                cardImage.type = Image.Type.Sliced;
                cardImage.preserveAspect = false;
                cardImage.raycastTarget = false;
                UiKit.Place(cardImage, 0f, 0f, 1f, 1f);

                // Görünmez ama dokunulabilir yüzey: sekmenin tamamı tıklanabilsin.
                if (button.targetGraphic is Image face) face.color = new Color(1f, 1f, 1f, 0f);

                var glyph = UiKit.CreateIcon("Icon", button.transform, UiSkin.Get(icon));

                // PUNTO REFERANSTAN: "Ana Ekran" yazısının büyük harf
                // yüksekliği ekranın %1.71'i, yani 24 punto değil ~46.
                // İngilizce etiketler ("Leaderboard", "Collection") Türkçe
                // karşılıklarından uzun olduğu için üst sınır 40'ta tutulup
                // küçülmesine izin veriliyor.
                // TMP OTO-BOYUT VE TRUNCATE KALDIRILDI (7. tur, M46).
                //
                // Bu ikili tam olarak dört tur önce yaşanan tuzağı taşıyordu:
                // `Truncate` eşiği bir birim aşıldığında yazının TAMAMINI
                // siliyor. Oto-boyut da onu ancak kısmen kurtarıyordu —
                // taşma denetimi "Leaderboard" için kutu 203 birim, gerekli
                // 262 birim ölçtü, yani yazı sığmadan çiziliyordu.
                //
                // Artık iş <see cref="GameKit.UI.UiTextFit"/>'te: yalnız
                // GENİŞLİĞE bakıyor, sığmayan puntoyu küçültüyor ve hiçbir
                // koşulda satırı silmiyor. `CreateLabel` onu her etikete
                // takıyor, bu yüzden burada yazılacak tek satır bile yok.
                var caption = UiKit.CreateTitle("Label", button.transform, label, 40,
                    UiKit.Ink, new Color(0.114f, 0.075f, 0.404f));

                string captured = key;
                button.onClick.AddListener(() => Show(captured));
                _tabButtons.Add((button, card, glyph.rectTransform, caption, key));
            }
        }

        /// <summary>
        /// Çubuğun ÜST kenarına yapışan, piksel yüksekliğinde ince şerit.
        /// Plastik dudağın dört katmanı bununla kuruluyor.
        /// </summary>
        // Referans karesinden örneklenen iki ton: çubuk koyu mor-lacivert,
        // seçili kart ondan belirgin AÇIK bir mor.
        // DERS (koyu zemin ikonu YALNIZ BIRAKIR): Çubuk fazla koyuydu ve
        // parlak 3B ikonlar onun üstünde oturmuyor, boşlukta yüzüyor gibi
        // duruyordu. Kontrast ne kadar sertse eleman o kadar "yapıştırılmış"
        // görünür. Çubuk ikonların tonuna yaklaştırıldı ve üst kenarına ince
        // bir ışık şeridi kondu — o çizgi, çubuğu bir YÜZEY yapan şey.
        // RENKLER REFERANSTAN ÖLÇÜLDÜ (`ana ekran.jpeg`, 2026-08-17):
        // çubuk gövdesi #5140E4. İlk ölçümde gövde ile üst ışık şeridi
        // karışmıştı (#4F3BD8 / #5340EA); dikey tarama ikisini ayırdı —
        // #5340EA aslında GÖVDENİN kendisi, ışık şeridi ondan çok daha
        // parlak (#7771F9).
        // ÖLÇÜM (14. tur, N2 — kullanıcı: "orjinal oyunda bir tık daha koyu
        // mor var, bizdeki ekstra açık olabilir"). Haklıymış ve fark asıl
        // YEŞİL kanalda: referansın gövdesi (73,44,219), bizimki (81,64,228)
        // idi. Yeşilin 64 olması moru maviye/lavantaya çekiyor, göz de bunu
        // "daha açık" diye okuyor. Gövdenin altı (60,34,180)'e sönüyor.
        static readonly Color BarColor = new Color(0.235f, 0.133f, 0.706f);

        /// <summary>Sekmeler arasındaki ince dikey ayraç — referansta var.</summary>
        /// <summary>
        /// Sekmeler arasındaki dikey çizgi.
        ///
        /// Kullanıcı: "o aralıklı çizgiler de daha koyu olsun, renk olarak
        /// koyu mor olsun, bizde açık ya." Beyaz-üstüne-alfa olduğu için
        /// çizgi çubuğun morunu AÇIYORDU; referansta çizgi zeminden koyu,
        /// yani oyulmuş gibi duruyor. Renk kartın koyu bandından alındı
        /// (#291B8C) ve alfası düşürüldü.
        /// </summary>
        static readonly Color DividerColor = new Color(0.161f, 0.106f, 0.549f, 0.75f);

        /// <summary>
        /// Sekmeyi değiştirir. ZATEN AÇIK olan sekmeye basmak hiçbir şey yapmaz.
        ///
        /// DERS (aynı yere iki kez basmak bir GEZİNME değildir): Burası eskiden
        /// açık sekmeye tekrar basınca oyuncuyu ANA EKRANA atıyordu. Niyet
        /// "geri tuşu gibi olsun" idi ama cihazda yaşanan şey şu: oyuncu
        /// mağazadayken mağaza sekmesine bir daha dokunuyor ve kendini ana
        /// ekranda buluyor — ekran "açılıp kapanıyor" gibi görünüyor.
        /// Bir sekme çubuğunda seçili sekme bir HEDEF'tir, bir düğme değil;
        /// zaten oradaysan gidilecek yer yok. Tek doğru karşılık dokunuşun
        /// alındığını hissettirmek: haptik.
        /// </summary>
        public void Show(string key)
        {
            if (_active == key && _applied)
            {
                if (Flow.AppRoot.Current != null)
                    Flow.AppRoot.Current.Haptics?.Play(GameKit.Services.HapticStrength.Medium);
                return;
            }

            string previous = _active;
            _applied = true;

            // Tam ekran örtü sayfalarında çubuk tamamen gizleniyor: hem
            // görünmesin, hem de arkasında kalan içerik (Hesabımı Sil)
            // ortaya çıksın.
            bool fullScreen = System.Array.IndexOf(FullScreenPages, key) >= 0;
            if (_bar != null) _bar.gameObject.SetActive(!fullScreen);
            foreach (var (button, _, _, _, _) in _tabButtons)
                if (button != null) button.gameObject.SetActive(!fullScreen);
            foreach (var decor in _tabDecor)
                if (decor != null) decor.gameObject.SetActive(!fullScreen);
            _active = key;

            foreach (var pair in _screens)
            {
                // Çıkan ekran hemen kapanmıyor: kayma bitince kapanacak.
                if (pair.Key == previous && previous != key) continue;
                pair.Value.gameObject.SetActive(pair.Key == key);
            }

            SlideSwap(previous, key);

            ApplyTabVisuals(key);

            if (key == "journey" && _screens.TryGetValue(key, out var journeyPage))
                journeyPage.GetComponent<JourneyScreen>()?.Refresh();
            if (key == "profile" && _screens.TryGetValue(key, out var profilePage))
                profilePage.GetComponent<ProfileScreen>()?.Refresh();
            if (key == "collection" && _screens.TryGetValue(key, out var collectionPage))
                collectionPage.GetComponent<CollectionScreen>()?.Refresh();
            if (key == "settings" && _screens.TryGetValue(key, out var settingsPage))
                settingsPage.GetComponent<SettingsScreen>()?.Refresh();
        }

        /// <summary>
        /// Seçili sekmenin görünümü: kart, ikon konumu, yazı.
        ///
        /// <see cref="Show"/>'dan ayrıldı (7. tur) — sebebi doğrulama.
        /// Sekme çubuğu YALNIZ oynatma kipinde kuruluyordu
        /// (<see cref="Start"/>), yani seçili kartın köşelerine bakmak için
        /// her seferinde oyunu başlatmak gerekiyordu. Bu yüzden P56'daki
        /// köşe bozulması altı tur boyunca ölçülmedi.
        /// <see cref="CreateTabBarPreview"/> artık aynı çubuğu düzenleyici
        /// kipinde kuruyor ve bu metodu çağırıyor.
        /// </summary>
        void ApplyTabVisuals(string key)
        {
            foreach (var (tabButton, card, icon, caption, tabKey) in _tabButtons)
            {
                bool selected = tabKey == key;

                // AÇIK sekme basınca KIMILDAMAZ (referans davranışı). Erken
                // çıkış zaten gezinmeyi engelliyordu ama his bileşeni bundan
                // habersizdi ve düğme yine küçülüp büyüyordu — oyuncu olmayan
                // bir eylemi tetikliyormuş gibi ekranı oynatabiliyordu.
                // Titreşim susmuyor: dokunuş yine "duyuldum" diyor.
                if (tabButton != null)
                {
                    var feel = tabButton.GetComponent<GameKit.UI.UiButtonFeel>();
                    if (feel != null) feel.Muted = selected;
                }

                // Kart yalnız seçilide görünür ve çubuğun üstüne taşar.
                if (card != null) card.gameObject.SetActive(selected);

                // KONUMLAR REFERANSTAN (`ana ekran.jpeg`, düğme yüksekliği
                // %9.5 ekran kabul edilerek): seçili ikon 0.70-1.40, seçili
                // olmayan 0.30-0.93, yazı 0.19-0.45. Eskiden seçili olmayan
                // ikon 0.16'dan başlıyordu — çubuğun alt kenarına yapışıp
                // ortalanmamış duruyordu.
                //
                // DERS (`preserveAspect` kutuyu DOLDURMAZ, kutuya SIĞAR):
                // İkonlar önce 0.34-0.88 kutusuna kondu; kutu 98 birim
                // yüksekti ama simge kare olduğu için 98 birim ÇİZİLDİ, oysa
                // referanstaki simge 114. Genişliği artırmak hiçbir şeyi
                // değiştirmiyor — sınırlayan kenar YÜKSEKLİK. Bir ikonu
                // büyütmek istiyorsan dar olan kenarını büyüteceksin.
                // SEÇİLİ İKON KARTIN ORTASINA OTURUYOR (4. tur, A6).
                //
                // Kullanıcı: "İkonlar butonların en üstünde kalıyor. Seçili
                // butonun orta pivotunda, dikey ortalanmış olmalı."
                //
                // ÖLÇÜM (`ana ekran.jpeg`, 946×2048; düğme yüksekliği ekranın
                // %9.5'i = 194,6 piksel, yani düğme yerel 1 birimi 194,6px):
                //   kart üstü      y=1783 → yerel 1.362
                //   ev ikonu       y=1816-1966, merkez 1891 → yerel 0.807
                //   "Ana Ekran"    y=1976-2006, merkez 1991 → yerel 0.293
                // Yani ikon kartın ÜST kenarına yapışmıyor; üstünde de altında
                // da pay var ve yazı onun altında duruyor.
                //
                // Eski değer 0.70-1.40 idi: ikonun tepesi kartın tam üst
                // kenarındaydı ve simge kartın dışına taşmış gibi duruyordu.
                //
                // DERS (bir şeyi "yukarı almak" ile "ortalamak" farklı işler):
                // Kart düğmeden yukarı taştığı için ikonu da yukarı itmek
                // doğru göründü; oysa ikon KARTIN kutusunda ortalanmalıydı,
                // düğmenin kutusunda değil. Referans ölçüldüğünde ikonun
                // merkezi kart yüksekliğinin ortasına denk geliyor.
                // SEÇİLİ OLMAYAN İKONLAR BÜYÜDÜ (10. tur).
                //
                // ÖLÇÜM (`ana ekran.jpeg` ve bizim yakalamamız, ikon
                // yüksekliği ÇUBUK yüksekliğine oranla):
                //   referans mağaza 124/211 = %58,8 · küre 112/211 = %53,1
                //   bizim ikisi de   87/199 = %43,7
                // Yani ikonlarımız dörtte bir küçüktü ve çubuk boş
                // görünüyordu. Seçili ikon ise ölçüldü ve ZATEN doğruydu
                // (referans 0,417-1,199 · bizim 0,42-1,19 düğme biriminde),
                // o yüzden ona dokunulmadı.
                //
                // DERS (bir ekranın "boş" görünmesi çoğu zaman boşluk değil,
                // KÜÇÜKLÜK sorunudur): Buradaki ilk içgüdü çubuğu inceltmek
                // olurdu; ölçüm ise çubuğun doğru, içindekilerin küçük
                // olduğunu söyledi. İkisi aynı görüntüyü verir ama biri
                // referanstan uzaklaştırırdı.
                if (icon != null)
                    UiKit.Place(icon, selected ? 0.06f : 0.12f, selected ? 0.42f : 0.25f,
                                      selected ? 0.94f : 0.88f, selected ? 1.19f : 0.90f);

                if (caption != null) caption.gameObject.SetActive(selected);

                // YAZI KUTUSU SATIR YÜKSEKLİĞİNDEN KISA OLAMAZ.
                //
                // BULUNAN HATA: Seçili sekmenin adı ("Home", "Leaderboard"…)
                // ekranda HİÇ görünmüyordu. Kutu 0.20-0.38 idi, yani düğme
                // yüksekliğinin %18'i = 32,83 birim. Baloo 2'nin 24 puntodaki
                // satır yüksekliği ~33,6 birim; `overflowMode = Truncate` ile
                // birleşince TMP satırı sığdıramayıp TAMAMEN atıyordu.
                // Ölçüm: `text.bounds` extents = (0, 0, 0) — yani hiç geometri
                // üretilmemiş. Renk, alfa, kardeş sırası, materyal hepsi
                // doğruydu.
                //
                // DERS (kırpma, "biraz eksik"i "hiç yok"a çevirir): Kesme
                // kipi taşmayı önlemek için konur ama eşiği bir birim
                // aşıldığında sonuç yarım yazı değil, SIFIR yazıdır. Bir
                // kutuyu yazının en küçük satır yüksekliğine göre ölçmek
                // zorunludur; "yaklaşık yeter" burada hiç yetmiyor.
                //
                // Kutu 0.155-0.425 (%27 = 49 birim) yapıldı; merkezi 0.29,
                // yani referansta ölçülen 0.293 ile aynı yerde kalıyor.
                if (selected && caption != null)
                    UiKit.Place(caption, 0.03f, 0.155f, 0.97f, 0.425f);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sekme çubuğunu OTURUM OLMADAN kurar — yalnız düzenleyici doğrulaması.
        /// Kanvası ÇAĞIRAN vermeli (bkz. <c>GameplayScreen.CreateResultPreview</c>:
        /// <c>UiKit.CreateCanvas</c> düzenleyici kipinde `DontDestroyOnLoad`
        /// yüzünden patlıyor).
        /// </summary>
        public static MenuShell CreateTabBarPreview(Transform canvasTransform,
                                                    string selected = "home")
        {
            var host = UiKit.CreateRect("TabBarPreview", canvasTransform);
            UiKit.Place(host, 0f, 0f, 1f, 1f);

            var shell = host.gameObject.AddComponent<MenuShell>();
            shell.BuildTabBar(host);
            shell.ApplyTabVisuals(selected);
            return shell;
        }
#endif

        /// <summary>
        /// Sekme geçişi: yeni ekran yandan girer, eski ekran karşı yönden çıkar.
        /// Yön SEKME SIRASINDAN gelir — sağdaki sekmeye geçerken yeni ekran
        /// sağdan girer. Referansta ölçülen süre ~170 ms.
        ///
        /// DERS (anlık değişim yön TAŞIMAZ): Eski hâl `SetActive` ile bir
        /// karede değişiyordu; oyuncu "ışınlandım" hissi alıyor ve iki sekme
        /// arasındaki komşuluğu öğrenemiyordu. Referans oyun bu yüzden
        /// kaydırıyor.
        ///
        /// SINIR: Ana ekran AYRI bir kanvasta (HomeCanvas) ve menü içeriğinin
        /// ALTINDA duruyor; bu yüzden ana ekrana geçerken yalnız menü ekranı
        /// kayıp altındakini açığa çıkarıyor, ana ekranın kendisi kaymıyor.
        /// Referansta ikisi birden kayıyor.
        /// </summary>
        void SlideSwap(string from, string to)
        {
            if (_content == null || from == to) return;

            float width = _content.rect.width;
            if (width < 1f) width = 1080f;

            float direction = TabOrder(to) >= TabOrder(from) ? 1f : -1f;

            if (_screens.TryGetValue(to, out var incoming))
                GameKit.FX.Juice.Replace(incoming,
                    GameKit.FX.Juice.SlideX(incoming, direction * width, 0f, SlideSeconds));
            else if (to == "home")
                // Ana ekranın kendi kanvası var; kaymayı kendisi yapıyor.
                HomeScreen.Instance?.Slide(direction * width, 0f, SlideSeconds);

            if (from == "home")
                HomeScreen.Instance?.Slide(0f, -direction * width, SlideSeconds);

            if (_screens.TryGetValue(from, out var outgoing))
            {
                var leaving = outgoing;
                GameKit.FX.Juice.Replace(leaving,
                    GameKit.FX.Juice.SlideX(leaving, 0f, -direction * width, SlideSeconds,
                        () =>
                        {
                            // Kapatmadan önce YERİNE geri koy: bir daha
                            // açıldığında ekran dışında kalmasın.
                            leaving.anchoredPosition =
                                new Vector2(0f, leaving.anchoredPosition.y);
                            leaving.gameObject.SetActive(false);
                        }));
            }
        }

        /// <summary>
        /// Komşu sekmeye geçer (+1 sağdaki, -1 soldaki). Kaydırma jesti bunu
        /// çağırıyor; <see cref="MenuSwipeNavigator"/>.
        ///
        /// DERS (uçlarda BAŞA SARMA yok): Son sekmeden sağa kaydırınca ilk
        /// sekmeye atlamak "döngüsel" hissi verir ve oyuncu nerede olduğunu
        /// kaybeder — beş sekmelik bir çubukta konum, sıranın kendisidir.
        /// Uçta hiçbir şey yapmamak, çubuğun bir ŞERİT olduğunu öğretiyor.
        ///
        /// Tam ekran örtü sayfalarında (Ayarlar, Profil) kaydırma kapalı:
        /// onlar sekme çubuğunun parçası değil, üstüne açılan sayfalar.
        /// Oradan yana kaydırmak hangi sekmeye gideceği belirsiz bir hareket.
        /// </summary>
        /// <summary>
        /// Hedefe ARADAKİ SEKMELERE UĞRAYARAK gider (4. tur, A3).
        ///
        /// Kullanıcı: "Para ikonuna tıklayınca kayarak direkt mağazaya gidiyor.
        /// Orijinalde 2 ekran değişimi var (önce sıralama, sonra mağaza)."
        ///
        /// DERS (kestirme, haritayı siler): Ana ekrandan mağazaya doğrudan
        /// kaymak teknik olarak daha "verimli" — bir geçiş yerine bir geçiş.
        /// Ama sekme çubuğu bir HARİTA ve kaydırma o haritanın komşuluk
        /// bilgisini öğretiyor: mağaza, sıralamanın solunda. Tek hamlede
        /// atlayınca oyuncu iki ekranın yan yana olduğunu hiç öğrenmiyor ve
        /// geri dönmek için sekmeyi aramak zorunda kalıyor. Referans bu yüzden
        /// aradaki ekranı da gösteriyor.
        ///
        /// Ara duraklarda ekran gerçekten kuruluyor ve kayıyor; sahte bir
        /// "araya bir kare koy" numarası değil.
        /// </summary>
        public void ShowStepped(string key)
        {
            int target = TabOrder(key);
            int current = TabOrder(_active);
            if (target >= Tabs.Length || current >= Tabs.Length ||
                Mathf.Abs(target - current) <= 1)
            {
                Show(key);
                return;
            }

            GameKit.FX.Juice.Replace(this, StepThrough(current, target));
        }

        System.Collections.IEnumerator StepThrough(int from, int to)
        {
            int step = to > from ? 1 : -1;
            for (int i = from + step; ; i += step)
            {
                Show(Tabs[i].key);
                if (i == to) yield break;

                // Ara durak TAM oturmadan bir sonrakine geçiliyor: iki geçiş
                // birbirine akıyor, iki ayrı hareket gibi durmuyor.
                yield return new WaitForSecondsRealtime(SlideSeconds * 0.72f);
            }
        }

        public void StepTab(int direction)
        {
            if (System.Array.IndexOf(FullScreenPages, _active) >= 0) return;

            int current = TabOrder(_active);
            if (current >= Tabs.Length) return;      // listede olmayan bir ekran

            int next = current + direction;
            if (next < 0 || next >= Tabs.Length) return;

            Show(Tabs[next].key);
        }

        /// <summary>Sekme çubuğundaki sıra; listede olmayan ekranlar sona sayılır.</summary>
        static int TabOrder(string key)
        {
            for (int i = 0; i < Tabs.Length; i++)
                if (Tabs[i].key == key) return i;
            return Tabs.Length;   // profile/settings: örtü sayfaları
        }

        /// <summary>Ekranların ortak başlık şeridi.</summary>
        public static TextMeshProUGUI Header(Transform parent, string title)
        {
            var bar = UiKit.CreateSlicedPanel("Header", parent, UiSkin.Get(Art.PanelDark));
            UiKit.Place(bar, 0.04f, 0.925f, 0.96f, 0.99f);
            var label = UiKit.CreateTitle("Title", bar.transform, title, 52, UiKit.Ink, UiKit.PanelDark);
            UiKit.Place(label, 0f, 0f, 1f, 1f);
            return label;
        }

        /// <summary>Ekranların ortak arka planı + kök dikdörtgeni.</summary>
        public static RectTransform Screen(Transform parent, string name)
        {
            var root = UiKit.CreateRect(name, parent);
            UiKit.Place(root, 0f, 0f, 1f, 1f);

            // Menü zemini burada da görünsün, üstüne okunurluk için koyu bir
            // perde çekilsin: manzara tamamen kaybolursa ekranlar arası geçiş
            // "başka bir oyuna girdim" hissi veriyor.
            UiKit.CreateCover("Bg", root, MenuPage.SeciliManzara(), UiKit.Background);
            UiKit.CreatePanel("Scrim", root, new Color(0.09f, 0.06f, 0.20f, 0.88f));
            return root;
        }
    }
}
