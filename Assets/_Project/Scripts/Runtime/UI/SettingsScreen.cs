using System.Collections.Generic;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;
using UiSprites = GameKit.UI.UiSprites;

namespace BlockOut.Runtime.UI
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
        /// Ses/titreşim servisleri artık uygulama açılışında <see cref="Flow.AppRoot"/>
        /// altında yaşıyor; menü de onları kullanıyor. Yoksa (sahne doğrudan
        /// açılmışsa) null geçilir, <see cref="SettingsBinder"/> buna dayanıklı.
        /// </summary>
        static AudioService Audio =>
            Flow.AppRoot.Current != null ? Flow.AppRoot.Current.Audio : null;

        static GameKit.Services.Haptics Haptics =>
            Flow.AppRoot.Current != null ? Flow.AppRoot.Current.Haptics : null;

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
            const float rowH = 147f;
            const float cardTop = 363f;
            const float cardH = 604f;

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
                v => { if (MetaServices.Ready) MetaServices.Save.Mutate(d => d.Settings.Notifications = v); });

            screen.AddRow(card, 1, rowH, cardH, UiSprites.Speaker, "Sounds",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Sounds,
                v => SettingsBinder.SetSounds(v, Audio, Haptics));

            screen.AddRow(card, 2, rowH, cardH, UiSprites.MusicNote, "Music",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Music,
                v => SettingsBinder.SetMusic(v, Audio, Haptics));

            screen.AddRow(card, 3, rowH, cardH, UiSprites.Haptics, "Haptics",
                () => MetaServices.Ready && MetaServices.Save.Data.Settings.Haptics,
                v => SettingsBinder.SetHaptics(v, Audio, Haptics));

            // --- Yeşil Support ---
            var support = MenuPage.Row("Support", root, cardTop + cardH + 132f, 156f, 0.24f, 0.76f);
            MenuPage.PillButton("Button", support, "Support", MenuPage.Green, 52,
                () => Application.OpenURL("https://example.com/support"));

            // --- İki mavi düğme ---
            // Referansta iki mavi düğme yeşil "Destek" ile aynı yükseklikte
            // (149 birim); bizde 130'du ve daha cılız duruyordu.
            var legal = MenuPage.Row("Legal", root, cardTop + cardH + 324f, 148f, 0.084f, 0.914f);
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
            var danger = MenuPage.Row("Delete", root, 1672f, 92f, 0.28f, 0.72f);
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
        void AddRow(Transform card, int index, float rowH, float cardH, Sprite icon,
                    string title, System.Func<bool> get, System.Action<bool> set)
        {
            float pad = 28f;
            float y1 = 1f - (pad + index * rowH) / cardH;
            float y0 = 1f - (pad + (index + 1) * rowH) / cardH;

            var row = UiKit.CreateRect("Row_" + title, card);
            UiKit.Place(row, 0f, y0, 1f, y1);

            if (icon != null)
            {
                var glyph = UiKit.CreateIcon("Icon", row, icon, MenuPage.InkDark);
                UiKit.Place(glyph, 0.045f, 0.14f, 0.185f, 0.86f);
            }

            var label = UiKit.CreateLabel("Label", row, title + ":", 46, MenuPage.InkDark,
                TextAlignmentOptions.Left);
            label.fontStyle = FontStyles.Bold;
            UiKit.Place(label, 0.23f, 0.16f, 0.60f, 0.84f);

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
            AddHalfClick(toggle.View.OffFace, false, set);
            // Tıklanan yüzey EN DIŞTAKİ katman (koyu kontur): çip artık üç
            // katman ve dokunmayı en dıştaki yakalamalı, yoksa konturun
            // taşan 6 birimlik şeridi ölü alan olurdu.
            AddHalfClick(toggle.View.OnFace, true, set);
        }

        /// <summary>Anahtarın bir yarısı: basınca o duruma GEÇER, ters çevirmez.</summary>
        void AddHalfClick(Image face, bool value, System.Action<bool> set)
        {
            face.raycastTarget = true;
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            face.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
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
