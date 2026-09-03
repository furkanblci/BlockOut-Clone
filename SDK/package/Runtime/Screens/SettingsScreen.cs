using System.Collections.Generic;
using GameKit.Meta;
using GameKit.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;
using UiSprites = GameKit.UI.UiSprites;

namespace GameKit.Screens
{
    /// <summary>
    /// Ayarlar — referanstan ölçülerek yeniden kuruldu.
    /// Referans kare: `menus,powerups,vs.mp4`, 54-56. saniyeler.
    ///
    /// YAPI: degrade başlık + sağ üstte kırmızı çarpı; tek büyük açık mor
    /// kart içinde DÖRT satır (simge · ad · Kapalı|Açık ikili anahtarı);
    /// altında yeşil "Support", yanyana iki mavi düğme, en altta soluk
    /// "Delete My Account".
    ///
    /// DERS (ikili anahtar iki DURUM gösterir, bir tane değil): İlk hâlde tek
    /// bir "Açık/Kapalı" yazısı vardı ve dokununca değişiyordu. Referans ikisini
    /// de yan yana gösterip AKTİF OLANI yeşile boyuyor. Fark küçük görünür ama
    /// tek yazıda oyuncu "burada yazan şey mevcut durum mu, basınca olacak şey
    /// mi?" diye duraksar; iki durumu birden göstermek o soruyu ortadan kaldırır.
    ///
    /// DERS (ayar = anında etki + kalıcı kayıt): Anahtar değiştiği anda hem
    /// ilgili servis kapanır hem de kayda yazılır. "Kaydet" düğmesi koymak
    /// mobilde bir hata kaynağıdır — oyuncu ayarı değiştirip geri tuşuna basar
    /// ve değişiklik kaybolur.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        // Referanstan ölçüldü: kart #8C7DFE, anahtar yuvası #342B7E.
        static readonly Color CardFace = new Color(0.549f, 0.490f, 0.996f);
        // ANAHTAR RENKLERİ REFERANSTAN ÖLÇÜLDÜ (2026-08-18, `settings.jpg`).
        static readonly Color SlotDark = new Color(0.204f, 0.169f, 0.494f);  // #342B7E
        static readonly Color SlotRim  = new Color(0.565f, 0.506f, 0.996f);  // #9081FE
        static readonly Color OnGreen  = new Color(0.224f, 0.835f, 0.063f);  // #39D510
        static readonly Color OnGloss  = new Color(0.710f, 0.988f, 0.376f, 0.85f); // #B5FC60
        static readonly Color OnEdge   = new Color(0.075f, 0.400f, 0.020f);  // koyu kontur
        static readonly Color OnInk    = new Color(0.055f, 0.239f, 0.020f);  // "On" KOYU yeşil

        /// <summary>
        /// Bir anahtarın çalışma anında değişen parçaları.
        ///
        /// DERS (katman eklerken TAZELEME kodunu da güncelle): "Açık" çipi üç
        /// katmana çıkarıldı (koyu kontur → yüzey → üst parlaklık) ama
        /// <see cref="Apply"/> hâlâ tek bir `OnFace.color` yazıyordu — ve o
        /// alan artık KONTUR katmanını gösteriyordu. Sonuç: kurulumda doğru
        /// görünen çip, ilk tazelemede düz yeşile geri dönüyordu. Görünüm
        /// kurulumda değil, tazelemeden SONRA doğrulanmalı.
        /// </summary>
        sealed class Toggle
        {
            public System.Func<bool> Get;
            public MenuPage.SwitchView View;
        }

        readonly List<Toggle> _toggles = new List<Toggle>();
        TextMeshProUGUI _playerId;
        bool _built;

        Image _deleteFace;
        TextMeshProUGUI _deleteLabel;
        bool _deleteArmed;
        float _deleteArmedUntil;

        static readonly Color DangerIdle  = new Color(0.44f, 0.42f, 0.78f);
        static readonly Color DangerArmed = new Color(0.855f, 0.145f, 0.180f);

        /// <summary>
        /// Ses/titreşim servisleri artık uygulama açılışında <see cref="App.AppRoot"/>
        /// altında yaşıyor; menü de onları kullanıyor. Yoksa (sahne doğrudan
        /// açılmışsa) null geçilir, <see cref="SettingsBinder"/> buna dayanıklı.
        /// </summary>
        static AudioService Audio =>
            App.AppRoot.Current != null ? App.AppRoot.Current.Audio : null;

        static GameKit.Services.Haptics Haptics =>
            App.AppRoot.Current != null ? App.AppRoot.Current.Haptics : null;

        /// <summary>
        /// İlk dokunuş silmeyi KURAR, ikincisi uygular. Süre dolarsa kendiliğinden
        /// geri döner — bkz. <see cref="Update"/>.
        /// </summary>
        void OnDeleteAccount()
        {
            if (!_deleteArmed)
            {
                _deleteArmed = true;
                _deleteArmedUntil = Time.unscaledTime + 5f;
                _deleteLabel.text = "Tap again to erase everything";
                _deleteFace.color = DangerArmed;
                GameKit.FX.Juice.Replace(_deleteFace,
                    GameKit.FX.Juice.PunchScale(_deleteFace.transform, 0.20f));
                return;
            }

            DisarmDelete();
            if (!MetaServices.Ready) return;

            MetaServices.Save.Reset();
            MenuShell.Instance?.Show("home");
        }

        void DisarmDelete()
        {
            _deleteArmed = false;
            if (_deleteLabel != null) _deleteLabel.text = "Delete My Account";
            if (_deleteFace != null) _deleteFace.color = DangerIdle;
        }

        void Update()
        {
            if (_deleteArmed && Time.unscaledTime >= _deleteArmedUntil) DisarmDelete();
        }

        void OnDisable() => DisarmDelete();

        public static SettingsScreen Build(Transform parent)
        {
            var root = MenuPage.Screen(parent, "SettingsScreen");
            var screen = root.gameObject.AddComponent<SettingsScreen>();

            // --- Ayar kartı: dört satır tek panelde ---
            //
            // ÖLÇÜ REFERANSTAN (`WhatsApp Image ... (2).jpeg`, 946×2048):
            // kart y 387-1031 → bizim tuvalde üst **363**, yükseklik **604**;
            // X 0.034-0.966. Satır adımı 157 piksel → **147 birim**.
            //
            // Bizimki 316 / 856 / 196'ydı — yani kart %40, satırlar %33
            // fazla yüksekti. Kullanıcının "anahtarlar referansta daha küçük"
            // notunun ölçülebilir hâli: küçük olan anahtar değil, ONU TAŞIYAN
            // SATIRDI. Anahtarın satır içindeki oranı zaten doğruydu.
            // SATIR ADIMI VE KART BOYU REFERANSTAN (9. tur, `m_019`).
            //
            // Bu ekran daha önce "bitti" sayılmıştı, ama o turdaki ölçüler
            // artık diskte olmayan kaynaklara dayanıyordu. `_Reference/frames/`
            // taranınca ayarların gerçek karesi çıktı.
            //
            // ÖLÇÜM (kart zemini renk eşleştirmesiyle; oranlar GENİŞLİĞE göre —
            // BU KENAR SEÇİMİ YANLIŞTI, bkz. aşağıdaki eksen düzeltmesi):
            //     satır adımı   referans %16,70   bizim %13,61
            //     kart yüksekl. referans %70,40   bizim %54,40
            //     kart genişl.  referans %93,70   bizim %91,90   (yakın)
            //     anahtar yük.  referans  %8,80   bizim  %8,06   (yakın)
            // Yani satırlar referanstan %18 sıkışıktı; kart da onun kadar
            // kısa kalıyordu.
            //     0,1670×1080 = 180  ->  rowH 180
            //     4×180 + 28 üst + 28 alt = 776  ->  cardH 776
            //
            // DİKKAT — YÜKSEKLİĞE GÖRE BAKINCA FARK GÖRÜNMÜYOR: iki ekranı
            // aynı YÜKSEKLİĞE ölçekleyip yan yana koyunca satır adımları
            // birebir aynı çıkıyor (64 piksel). Fark yalnız genişliğe oranla
            // görünüyor, çünkü referans karesi 443×960 (en-boy 2,167), bizim
            // tuval 1080×1920 (1,778).
            //
            // DERS (karşılaştırmayı DÜZENİN ölçeklendiği eksene göre yap):
            // Bu arayüz genişliğe göre ölçekleniyor; o hâlde her oran
            // genişliğe bölünmeli. Yüksekliğe göre hazırlanmış bir yan yana
            // görsel, gerçek farkı gizliyor.
            // ---- 9. TURDA YAPILAN HATA VE DÜZELTMESİ ----
            //
            // Bu turda rowH 147'den 180'e, cardH 604'ten 776'ya çıkarılmıştı.
            // Gerekçe "referans satır adımı genişliğin %16,70'i, bizimki
            // %13,61" idi. ORAN DOĞRU ÖLÇÜLDÜ AMA YANLIŞ KENARA BÖLÜNDÜ.
            //
            // `UiKit` kanvası `matchWidthOrHeight = 1` kuruyor, yani DİKEY
            // birim sabit: kanvas her cihazda 1920 birim yüksek, genişliği
            // ise cihaza göre değişiyor. `MenuPage.Row`un `top`/`height`
            // parametreleri de birim cinsinden. Dolayısıyla bir Y ölçüsü
            // referansın EKRAN YÜKSEKLİĞİNE oranlanmalı:
            //     referans satır adımı 74 px / 960 = %7,71 -> 1920×0,0771 = 148
            //     referans kart yüks. 312 px / 960 = %32,5 -> 624
            // Eski değerler (147 ve 604) bu hedeflere zaten çok yakındı;
            // "düzeltme" %22'lik bir şişme yarattı.
            //
            // X ölçüleri ise `Place` ile normalize edildiği için ekran
            // GENİŞLİĞİNE oranlanır — bu turdaki X düzeltmeleri (Support'un
            // 0,52'den 0,47'ye daralması) doğru ve korunuyor.
            //
            // DERS (her eksen kendi kenarına bölünür): `Place` iki eksende de
            // 0-1 normalize; ama X kanvasın genişliğine, Y yüksekliğine
            // karşılık gelir. Tek bir "oranları genişliğe böl" kuralı,
            // dikey ölçüleri sessizce şişirir. Doğrulama yolu: yakalamayı
            // referansın EN-BOYUNDA al (443/960 = 0,4615 -> 886×1920); o
            // zaman iki eksen de aynı ölçeğe gelir ve fark ortaya çıkar.
            const float rowH = 148f;
            const float cardTop = 363f;
            const float cardH = 624f;

            var card = MenuPage.Row("Card", root, cardTop, cardH, 0.034f, 0.966f);

            // Kart kenarlığı: referansta kartın çevresinde ondan koyu ince bir
            // şerit var ve kartı zeminden ayıran şey o.
            var edge = UiKit.CreateRoundedPanel("Edge", card,
                new Color(0.267f, 0.216f, 0.616f));
            UiKit.SetSliceScale(edge, 0.28f);
            edge.raycastTarget = false;
            UiKit.Place(edge, 0f, 0f, 1f, 1f);

            var face = UiKit.CreateRoundedPanel("Face", card, CardFace);
            UiKit.SetSliceScale(face, 0.30f);
            UiKit.Place(face, 0f, 0f, 1f, 1f, padding: 7f);

            // DERS (ayarı KAYDA yazmak, ayarı UYGULAMAK değildir): Bu üç satır
            // eskiden doğrudan `Save.Data.Settings`'e yazıyordu. Kayıt doğru
            // oluyordu ama Müzik ve Haptik anahtarları çalışan servise hiç
            // uğramıyordu: oyuncu müziği kapatıyor, müzik çalmaya devam
            // ediyordu — ancak uygulamayı yeniden başlatınca ayar tutuyordu.
            // Duraklat panelindeki AYNI anahtarlar ise `SettingsBinder`
            // üzerinden gidiyordu, yani aynı ayar iki yerden farklı davranıyordu.
            // Tek yol var: yazma ve uygulama tek elden, `SettingsBinder`.
            screen.AddRow(card, 0, rowH, cardH, UiSprites.Bell, "Notifications",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Notifications,
                v => { if (MetaServices.Ready) MetaServices.Save.Mutate(d => d.Settings.Notifications = v); },
                // ZİL 1,30 DEĞİL 1,04. İlk çarpan hatalı bir ölçümden geldi:
                // simgeleri boşluk arayarak satırlara ayırıyordum ve zilin
                // gövdesiyle tokmağı arasındaki boşluk onu ikiye bölüp
                // yalnız üst parçayı (53 birim) ölçtürüyordu. Kartı DÖRT EŞİT
                // dilime bölünce gerçek değer 73 çıktı, hedef 76.
                //
                // DERS (ölçüm bölütlemesi, ölçtüğün şeyi bozabilir): İçinde
                // boşluk olan bir simgeyi "dolu satır" arayarak bulmak onu
                // parçalara ayırır. Bölütlemeyi verinin kendisinden değil
                // BİLİNEN düzenden (dört eşit satır) türet.
                iconScale: 1.04f);   // zil: cizilen 73 birim -> hedef 76

            screen.AddRow(card, 1, rowH, cardH, UiSprites.Speaker, "Sounds",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Sounds,
                v => SettingsBinder.SetSounds(v, Audio, Haptics),
                iconScale: 1.09f);   // hoparlor: 64 -> 68

            screen.AddRow(card, 2, rowH, cardH, UiSprites.MusicNote, "Music",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Music,
                v => SettingsBinder.SetMusic(v, Audio, Haptics),
                iconScale: 0.86f);   // nota: 80 -> 68 (tek BUYUK olan)

            screen.AddRow(card, 3, rowH, cardH, UiSprites.Haptics, "Haptics",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Haptics,
                v => SettingsBinder.SetHaptics(v, Audio, Haptics),
                iconScale: 1.68f);   // titresim: 42 -> 68 (en cok dolgusu olan)

            // --- Yeşil Support ---
            // ALT BÖLÜM REFERANSTAN (9. tur, `m_019`; oranlar GENİŞLİĞE göre).
            //
            //                 genişlik      yükseklik     üst kenar
            //   Support  ref   %47,0         %16,9         %122,6
            //            biz   %51,1         %11,7         %118,1
            //   Terms    ref   %82,6         %13,8         %149,2
            //            biz   %81,9         %13,8         %136,0
            //
            // Terms/Privacy çifti zaten birebirdi — dokunulmadı. Support ise
            // hem geniş hem alçaktı: en-boyu 4,37, referansta 2,78. Yani
            // düğme yassı bir şerit gibi duruyordu.
            //     genişlik 0,52 -> 0,47  (0,265-0,735)
            //     yükseklik: yeşil maske 156 birimi 126 piksel ölçüyor
            //                (alt gölge sayılmıyor, oran 1,238);
            //                hedef %16,9 = 183 piksel -> sabit 227
            //     üst kenar 1271 -> 1324  (offset 132 -> 185)
            // Support uzayınca Terms'in eski yeri (324) çakışıyordu; o da
            // referansın söylediği yere alındı: 1463 -> 1611 (offset 472).
            var support = MenuPage.Row("Support", root, cardTop + cardH + 99f, 186f, 0.265f, 0.735f);
            MenuPage.PillButton("Button", support, "Support", MenuPage.Green, 52,
                () => Application.OpenURL("https://example.com/support"));

            // --- İki mavi düğme ---
            // Referansta iki mavi düğme yeşil "Destek" ile aynı yükseklikte
            // (149 birim); bizde 130'du ve daha cılız duruyordu.
            //
            // 148 -> 184 (2026-08-22). Mavi maskesiyle iki tarafa da AYNI ölçüm
            // uygulandı (WhatsApp referansı 946×2048):
            //     genişlik/konum ZATEN TUTUYOR:
            //         referans X 0.089-0.463 ve 0.535-0.909, gen %37.4
            //         bizim    X 0.091-0.468 ve 0.531-0.907, gen %37.7
            //     yükseklik: referans %13.6, bizim %11.0
            // 148 birim ekranda %11.0 çiziliyordu (hap düğmenin iç payı, oran
            // 0.80). Hedef %13.6 -> 0.136×1080/0.80 = 184.
            // NOT: gözle "bizimkiler daha geniş ve kenarlara itilmiş" sanmıştım;
            // ölçüm tersini söyledi — sorun yalnız yükseklikmiş.
            // ALT BÖLÜM EKRANA SIĞMIYOR — REFERANSIN İÇ ARALIKLARI KISALDI.
            //
            // Referansın alt bölümü (Support üstünden Delete altına) genişliğe
            // oranla %88, yani bizim tuvalde 950 birim. Kartın altı 1139'da
            // bittiği için bu 2274'e, ekranın 354 birim dışına taşıyor.
            // Referans karesi 443×960 (2,167), bizim tuval 1,778 — alt bölüm
            // orada rahat, burada değil.
            //
            // Support ve Terms referansın söylediği yerde bırakıldı; fazlalık
            // Terms ile Delete arasındaki BÜYÜK boşluktan kısıldı (referansta
            // %40,6 = 438 birim). Legal 472 -> 440, Delete 1672 -> 1790.
            //
            // ÇAKIŞMA VARDI: Delete 1672'de SABİTTİ ve Legal aşağı kayınca
            // (1463 -> 1611) tam onun içine girdi; "Delete My Account" yazısı
            // Terms/Privacy düğmelerinin üstünde duruyordu. Sayılar birebir
            // tuttuğu için ölçüm bunu göstermedi — GÖZ gösterdi.
            //
            // DERS (bir öğeyi kaydırırken SABİT komşularını da hesaba kat):
            // Support ve Legal karta göre konumlanıyor, Delete ise mutlak.
            // Karışık bir yerleşimde göreli olanı oynatmak, mutlak olanın
            // üstüne biner ve hiçbir oran ölçümü bunu yakalamaz.
            var legal = MenuPage.Row("Legal", root, cardTop + cardH + 335f, 151f, 0.084f, 0.914f);
            var terms = MenuPage.PillButton("Terms", legal, "Terms", MenuPage.Blue, 46,
                () => Application.OpenURL("https://example.com/terms"));
            UiKit.Place(terms, 0f, 0f, 0.47f, 1f);
            var privacy = MenuPage.PillButton("Privacy", legal, "Privacy", MenuPage.Blue, 46,
                () => Application.OpenURL("https://example.com/privacy"));
            UiKit.Place(privacy, 0.53f, 0f, 1f, 1f);

            // --- Soluk hesap silme ---
            //
            // KONUM VE BİÇİM REFERANSTAN (2026-08-18): düğme `946×2048` karede
            // y 1855-1925, yani ekranın DİBİNDEN %6-%9,4 yukarıda. Bizimki
            // 1770'teydi ve dünya koordinatında y[62..150]'ye düşüyordu —
            // ekranın en alt şeridinde, altındaki oyuncu kimliği yazısıyla
            // ÜST ÜSTE (kimlik y[30..98]). Kullanıcının "Delete My Account...
            // çok aşağıda taşmış" bulgusu buydu.
            //
            // Biçim de yanlıştı: referansta bu düğme DOLGUSUZ — yalnız açık
            // mor ince bir çerçeve ve içinde yazı. Yıkıcı bir eylemin dolu bir
            // düğme gibi davetkâr görünmemesi bilinçli bir tasarım kararı;
            // biz onu dolu lavanta bir kutu yapınca diğer düğmelerle aynı
            // ağırlığa gelmişti.
            var danger = MenuPage.Row("Delete", root, 1804f, 92f, 0.28f, 0.72f);
            var deleteFace = UiKit.CreateOutlinedBox("Face", danger,
                new Color(0.44f, 0.42f, 0.78f, 0.16f),
                new Color(0.71f, 0.69f, 0.98f, 0.85f), borderInset: 0f);
            UiKit.Place(deleteFace, 0f, 0f, 1f, 1f);
            var deleteLabel = UiKit.CreateTitle("Label", deleteFace.transform, "Delete My Account",
                34, MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(deleteLabel, 0.04f, 0.06f, 0.96f, 0.94f);

            // DERS (düğme gibi duran şey düğme OLMALI): Burası uzun süre yalnız
            // bir kapsül + yazıydı; tıklanabilir görünüyordu ama hiçbir şey
            // yapmıyordu. Mekanik denetimde çıkan üç ölü kontrolden biriydi.
            //
            // DERS (yıkıcı işlem TEK dokunuşla olmaz): Bu, oyuncunun bütün
            // ilerlemesini siler ve GERİ ALINAMAZ. Ayrı bir onay penceresi
            // kurmak yerine düğmenin kendisi iki aşamalı: ilk dokunuş uyarıya
            // dönüşüyor, ikinci dokunuş siliyor. Beş saniye içinde onaylanmazsa
            // kendiliğinden eski hâline dönüyor — yanlışlıkla basan oyuncu
            // hiçbir şey kaybetmiyor.
            screen._deleteFace = deleteFace;
            screen._deleteLabel = deleteLabel;
            var deleteButton = deleteFace.gameObject.AddComponent<Button>();
            deleteFace.raycastTarget = true;
            deleteButton.targetGraphic = deleteFace;
            deleteButton.transition = Selectable.Transition.None;
            deleteFace.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            deleteButton.onClick.AddListener(screen.OnDeleteAccount);

            // Oyuncu kimliği: destek talebinde tek işe yarayan bilgi.
            //
            // Silme düğmesinin ALTINDA duruyordu ve ikisi çakışıyordu (kimlik
            // y[30..98], düğme y[62..150]). Artık düğmenin ÜSTÜNDE, kendi
            // şeridinde: çakışma yok ve kimlik silmeden önce okunabiliyor —
            // destek talebi zaten hesabı silmeden ÖNCE yazılır.
            screen._playerId = UiKit.CreateLabel("PlayerId", root, "", 24,
                new Color(1f, 1f, 1f, 0.38f));
            UiKit.Place(screen._playerId, 0.05f, 0.148f, 0.95f, 0.180f);

            var band = MenuPage.Header(root, "Settings");
            MenuPage.Close(band, () => MenuShell.Instance?.Show("home"));

            screen._built = true;
            return screen;
        }

        /// <summary>
        /// Bir ayar satırı: simge · ad · [Kapalı|Açık] ikili anahtarı.
        /// Satırlar kartın İÇİNDE oransal yerleşir; kart yüksekliği bilindiği
        /// için piksel oranına çevriliyor.
        /// </summary>
        /// <param name="iconScale">
        /// Simgenin kutusunu merkezinden büyüten/küçülten çarpan.
        ///
        /// NEDEN GEREKLİ (13. tur, P5): Dört satırın dördü de AYNI kutuyu
        /// kullanıyordu ama ekranda çizilen simge yükseklikleri %2,8 / %3,2 /
        /// %4,2 / %2,2 çıkıyordu — referansta dördü de %3,2-%3,7 arasında.
        /// Sebep kutuda değil SPRITE'LARDA: her birinin saydam dolgusu
        /// farklı, `preserveAspect` de opak içeriği değil dosyanın tamamını
        /// kutuya sığdırıyor. Aynı tuzak "Süre Doldu" ekranındaki jetonda da
        /// çıkmıştı.
        ///
        /// Çarpanlar ÇİZİLEN yükseklikten hesaplandı (hedef ~68 birim):
        /// zil 53 -> x1,30 · hoparlör 64 -> x1,09 · nota 80 -> x0,86 ·
        /// titreşim 42 -> x1,68.
        ///
        /// DERS (aynı kutu, aynı boyut DEMEK DEĞİL): Bir sprite kümesini tek
        /// kutuya koyup "hepsi eşit" saymak, dolguları eşitse doğru. Değilse
        /// ekranda gördüğün boyut dosyanın boş kenarına göre belirlenir —
        /// ve bu ancak ÇİZİLEN piksel ölçülünce görünür.
        /// </param>
        void AddRow(Transform card, int index, float rowH, float cardH, Sprite icon,
                    string title, System.Func<bool> get, System.Action<bool> set,
                    float iconScale = 1f)
        {
            float pad = 28f;
            float y1 = 1f - (pad + index * rowH) / cardH;
            float y0 = 1f - (pad + (index + 1) * rowH) / cardH;

            var row = UiKit.CreateRect("Row_" + title, card);
            UiKit.Place(row, 0f, y0, 1f, y1);

            if (icon != null)
            {
                var glyph = UiKit.CreateIcon("Icon", row, icon, MenuPage.InkDark);
                glyph.preserveAspect = true;   // UiKit.CreateIcon bunu kurmuyor
                const float ix0 = 0.045f, ix1 = 0.185f, iy0 = 0.14f, iy1 = 0.86f;
                float cx = (ix0 + ix1) * 0.5f, cy = (iy0 + iy1) * 0.5f;
                float hw = (ix1 - ix0) * 0.5f * iconScale, hh = (iy1 - iy0) * 0.5f * iconScale;
                UiKit.Place(glyph, cx - hw, cy - hh, cx + hw, cy + hh);
            }

            // ETİKET ORTALANIYOR, sola dayanmıyor (13. tur, A2 komşusu).
            //
            // ÖLÇÜM (`ayarlar_avg.png`): dört etiketin başlangıcı FARKLI
            // (%25,5 / %31,5 / %31,8 / %30,7) ama merkezleri AYNI (%40,6).
            // Referans bunları simgeyle anahtar arasına ORTALIYOR; bizde
            // dördü de %25,0'ten başlıyordu, yani sola dayalıydı.
            //
            // Punto 46 -> 52: ölçülen kapak yüksekliği bizde ekranın
            // %1,7'si, referansta %2,0 idi (%12 küçük).
            var label = UiKit.CreateLabel("Label", row, title + ":", 52, MenuPage.InkDark,
                TextAlignmentOptions.Center);
            label.fontStyle = FontStyles.Bold;
            UiKit.Place(label, 0.20f, 0.16f, 0.60f, 0.84f);

            // İKİLİ ANAHTAR — referanstan yeniden ölçüldü (2026-08-18).
            //
            // Ölçüm (`WhatsApp Image ... (2).jpeg`, 946×2048): koyu yuva
            // ekranın 0.617-0.894'ü, yeşil çip 0.783-0.913'ü. Yani **çip
            // yuvanın SAĞ UCUNDAN TAŞIYOR** — kabartılmış bir tuş gibi
            // duruyor, yuvanın içine gömülü değil. Bizde çip yuvanın
            // içindeydi ve anahtar "iki renkli düz bir şerit" gibi
            // okunuyordu; kullanıcının "Off/On butonları... orijinalinde
            // gölgeli, parlak, şık" notunun yapısal kısmı bu.
            //
            // Renkler: yuva içi #342B7E, yuvanın dış bileziği #9081FE,
            // çip yüzeyi #39D510, çipin üst parlaklığı #B5FC60.
            // "Açık" yazısı BEYAZ DEĞİL, koyu yeşil — parlak yeşilin üstünde
            // beyaz yazı okunuyor ama referansın kontrastı tersine kurulmuş.
            var view = MenuPage.Switch("Switch", row, 32);
            UiKit.Place(view.Root, 0.625f, 0.14f, 0.923f, 0.86f);

            var toggle = new Toggle { Get = get, View = view };

            _toggles.Add(toggle);

            // Her iki yarı da AYRI AYRI tıklanabilir: oyuncu istediği duruma
            // DOĞRUDAN basıyor.
            //
            // DERS (aynı görünen kontrol aynı davranmalı): Burası eskiden
            // yuvanın tamamını tek düğme yapıyor ve dokununca durumu TERS
            // ÇEVİRİYORDU. Yani yeşil "On" yarısına basmak — üstünde "On"
            // yazan yere — ayarı KAPATIYORDU. Duraklat panelindeki birebir
            // aynı anahtar ise doğrudan-durum çalışıyordu. Oyuncu iki ekranda
            // aynı şeye basıp farklı sonuç alınca kontrolü değil oyunu
            // suçlar; üstelik yazının söylediğinin tersini yapan bir düğme
            // tek başına da yanlış.
            AddHalfClick(toggle.View.OffFace, false, set, view.Root);
            // Tıklanan yüzey EN DIŞTAKİ katman (koyu kontur): çip artık üç
            // katman ve dokunmayı en dıştaki yakalamalı, yoksa konturun
            // taşan 6 birimlik şeridi ölü alan olurdu.
            AddHalfClick(toggle.View.OnFace, true, set, view.Root);
        }

        /// <summary>Anahtarın bir yarısı: basınca o duruma GEÇER, ters çevirmez.</summary>
        void AddHalfClick(Image face, bool value, System.Action<bool> set, Transform feel)
        {
            face.raycastTarget = true;
            var button = face.gameObject.AddComponent<Button>();
            // Geri bildirim ANAHTARIN TAMAMINA uygulanıyor (13. tur, P5).
            // Tıklanan yüzey çipin ALTINDA kalan bir kapsül; onu küçültmek
            // ekranda hiçbir şey değiştirmiyordu.
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            // Ölçek anahtarın TAMAMINA (`Body`) uygulanıyor — 15. turda
            // `UiPressFeedback.Target` buraya taşındı.
            GameKit.UI.UiButtonFeel.Attach(face, feel);
            button.onClick.AddListener(() => { set(value); Refresh(); });
        }

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (!_built) return;

            if (_playerId != null)
                _playerId.text = MetaServices.Ready ? MetaServices.PlayerId : "—";

            foreach (var toggle in _toggles)
                toggle.View.SetOn(toggle.Get());
        }
    }
}
