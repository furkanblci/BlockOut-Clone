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
        static readonly Color CardFace = new Color(0.541f, 0.518f, 0.965f);
        static readonly Color SlotDark = new Color(0.220f, 0.184f, 0.463f);

        sealed class Toggle
        {
            public System.Func<bool> Get;
            public Image OffFace, OnFace;
            public TextMeshProUGUI OffText, OnText;
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
            const float rowH = 196f;
            const float cardTop = 316f;
            float cardH = rowH * 4f + 72f;

            var card = MenuPage.Row("Card", root, cardTop, cardH, 0.045f, 0.955f);
            var face = UiKit.CreateRoundedPanel("Face", card, CardFace);
            face.pixelsPerUnitMultiplier = 0.28f;
            UiKit.Place(face, 0f, 0f, 1f, 1f);

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
            var legal = MenuPage.Row("Legal", root, cardTop + cardH + 330f, 130f, 0.075f, 0.925f);
            var terms = MenuPage.PillButton("Terms", legal, "Terms", MenuPage.Blue, 46,
                () => Application.OpenURL("https://example.com/terms"));
            UiKit.Place(terms, 0f, 0f, 0.47f, 1f);
            var privacy = MenuPage.PillButton("Privacy", legal, "Privacy", MenuPage.Blue, 46,
                () => Application.OpenURL("https://example.com/privacy"));
            UiKit.Place(privacy, 0.53f, 0f, 1f, 1f);

            // --- Soluk hesap silme ---
            var danger = MenuPage.Row("Delete", root, 1770f, 88f, 0.28f, 0.72f);
            var deleteFace = MenuPage.Capsule("Face", danger, new Color(0.44f, 0.42f, 0.78f));
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
            screen._playerId = UiKit.CreateLabel("PlayerId", root, "", 24,
                new Color(1f, 1f, 1f, 0.38f));
            UiKit.Place(screen._playerId, 0.05f, 0.02f, 0.95f, 0.055f);

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

            // İkili anahtar: koyu bir yuva, içinde iki yarı.
            var slot = MenuPage.Capsule("Slot", row, SlotDark);
            UiKit.Place(slot, 0.600f, 0.14f, 0.958f, 0.86f);

            var toggle = new Toggle { Get = get };

            toggle.OffFace = MenuPage.Capsule("Off", slot.transform,
                SlotDark);
            UiKit.Place(toggle.OffFace, 0.045f, 0.08f, 0.495f, 0.92f);
            toggle.OffText = UiKit.CreateTitle("OffText", toggle.OffFace.transform, "Off", 32,
                MenuPage.InkSoft, new Color(0.12f, 0.10f, 0.28f));
            UiKit.Place(toggle.OffText, 0.04f, 0.06f, 0.96f, 0.94f);

            toggle.OnFace = MenuPage.Capsule("On", slot.transform, MenuPage.Green);
            UiKit.Place(toggle.OnFace, 0.505f, 0.08f, 0.955f, 0.92f);
            toggle.OnText = UiKit.CreateTitle("OnText", toggle.OnFace.transform, "On", 36,
                MenuPage.Ink, new Color(0.05f, 0.26f, 0.03f));
            UiKit.Place(toggle.OnText, 0.04f, 0.06f, 0.96f, 0.94f);

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
            AddHalfClick(toggle.OffFace, false, set);
            AddHalfClick(toggle.OnFace, true, set);
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
            {
                bool on = toggle.Get();

                // AKTİF olan yarı renkli, diğeri yuvayla aynı tonda kaybolur.
                toggle.OnFace.color = on ? MenuPage.Green : SlotDark;
                toggle.OffFace.color = on ? SlotDark : new Color(0.36f, 0.32f, 0.60f);
                toggle.OnText.color = on ? MenuPage.Ink : MenuPage.InkSoft;
                toggle.OffText.color = on ? MenuPage.InkSoft : MenuPage.Ink;
            }
        }
    }
}
