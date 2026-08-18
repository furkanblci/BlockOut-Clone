using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Ad değiştirme penceresi — Profil'deki yeşil kalem rozetinin karşılığı.
    ///
    /// DERS (düğme gibi duran şey BİR ŞEY YAPMALI): Kalem rozeti referansta da
    /// var ve biz de çizmiştik, ama yalnız ÇİZMİŞTİK: altında Button bile
    /// yoktu. Üstelik kayıttaki `PlayerName` alanı zaten duruyor,
    /// ana ekrandaki avatar baş harfini ve liderlik tablosundaki oyuncu satırını
    /// besliyordu — ama oyunda o adı yazabileceğin TEK BİR YER yoktu, yani alan
    /// hep boş kalıyor ve her yerde "Player" / "?" görünüyordu. Mekanik
    /// denetimde çıkan dördüncü ölü kontrol buydu.
    ///
    /// REFERANS NOTU: Videoda oyuncunun adı "Fikret" ama kaleme HİÇ
    /// dokunulmuyor — bu pencerenin referanstaki hâli görülemedi. Bu yüzden
    /// düzen referanstan ÖLÇÜLMEDİ; projenin kendi menü dilinden (koyu perde,
    /// mor kart, üst kenara binen başlık, kırmızı daire çarpı, yeşil kapsül)
    /// kuruldu. Referans karesi bulunursa ölçüler buradan düzeltilmeli.
    /// </summary>
    public sealed class NamePanel : MonoBehaviour
    {
        /// <summary>Adın üst sınırı: liderlik satırına ve ad kapsülüne sığmalı.</summary>
        public const int MaxLength = 12;

        static readonly Color SlotDark = new Color(0.129f, 0.110f, 0.325f);

        RectTransform _root;
        Image _card;
        Image _slot;
        TMP_InputField _field;
        System.Action _onSaved;

        public static NamePanel Build(Transform parent, System.Action onSaved)
        {
            var holder = UiKit.CreateRect("NamePanel", parent);
            UiKit.Place(holder, 0f, 0f, 1f, 1f);
            var panel = holder.gameObject.AddComponent<NamePanel>();
            panel._root = holder;
            panel._onSaved = onSaved;

            // Perde: arkadaki profil ekranına dokunmayı da yutuyor.
            var scrim = UiKit.CreatePanel("Scrim", holder, new Color(0.05f, 0.03f, 0.14f, 0.84f));
            scrim.raycastTarget = true;

            panel._card = UiKit.CreateRoundedPanel("Card", holder, MenuPage.Panel);
            UiKit.SetSliceScale(panel._card, 0.30f);
            UiKit.Place(panel._card, 0.085f, 0.400f, 0.915f, 0.620f);

            // Başlık kartın ÜST KENARINA biner (bu projedeki bütün panellerde
            // olduğu gibi). Paylaşılan başlık materyali tek tek kontur ayarı
            // kabul etmediği için kontur SetOutline ile veriliyor.
            var title = UiKit.CreateTitle("Title", panel._card.transform, "Edit Name", 56,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(title, 0.06f, 0.90f, 0.94f, 1.16f);
            UiKit.SetOutline(title, MenuPage.InkDark);

            panel.BuildField(panel._card.transform);

            var save = MenuPage.PillButton("Save", panel._card.transform, "Save",
                MenuPage.Green, 46, panel.OnSave);
            UiKit.Place(save, 0.240f, 0.090f, 0.760f, 0.330f);

            var close = MenuPage.Close(panel._card.transform, panel.Hide);
            UiKit.Place(close, 0.845f, 0.760f, 0.985f, 1.075f);

            holder.gameObject.SetActive(false);
            return panel;
        }

        /// <summary>
        /// Yazı alanı. TMP_InputField'in elle kurulması üç parça ister:
        /// kırpan bir görüntü alanı (RectMask2D), içinde gerçek yazı, ve boşken
        /// görünen yer tutucu. Sıra önemli — alanlar bağlanmadan `text`
        /// yazılırsa TMP kendi içini kuramaz.
        /// </summary>
        void BuildField(Transform card)
        {
            _slot = MenuPage.Capsule("Slot", card, SlotDark);
            UiKit.Place(_slot, 0.070f, 0.400f, 0.930f, 0.720f);
            // Kapsül yüzeyleri varsayılan olarak dokunmayı GEÇİRİR (yazı ve
            // süs olarak kullanılıyorlar); burada yazı alanının kendisi o
            // yüzden dokunmayı yakalamak zorunda.
            _slot.raycastTarget = true;

            var viewport = UiKit.CreateRect("TextArea", _slot.transform);
            UiKit.Place(viewport, 0.045f, 0.10f, 0.955f, 0.90f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var text = UiKit.CreateLabel("Text", viewport, "", 48, MenuPage.Ink);
            UiKit.Place(text, 0f, 0f, 1f, 1f);

            var placeholder = UiKit.CreateLabel("Placeholder", viewport, "Your name", 48,
                new Color(1f, 1f, 1f, 0.35f));
            UiKit.Place(placeholder, 0f, 0f, 1f, 1f);

            _field = _slot.gameObject.AddComponent<TMP_InputField>();
            _field.textViewport = viewport;
            _field.textComponent = text;
            _field.placeholder = placeholder;
            _field.targetGraphic = _slot;
            _field.transition = Selectable.Transition.None;
            _field.lineType = TMP_InputField.LineType.SingleLine;
            _field.characterLimit = MaxLength;
            _field.onFocusSelectAll = true;
            _field.restoreOriginalTextOnEscape = true;

            // Klavyedeki "bitti" tuşu da kaydetsin: mobilde oyuncunun ilk
            // sezgisi kapsül düğmeye değil klavyeye basmak.
            _field.onSubmit.AddListener(_ => OnSave());
        }

        public void Show()
        {
            _field.text = MetaServices.Ready ? MetaServices.Save.Data.PlayerName : "";
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            GameKit.FX.Juice.Run(GameKit.FX.Juice.PopIn(_card.transform, 0.30f));

            // Panel açılır açılmaz imleç alanda: mobilde klavye kendiliğinden
            // gelsin, oyuncu ikinci bir dokunuş yapmasın.
            _field.Select();
            _field.ActivateInputField();
        }

        public void Hide()
        {
            _field.DeactivateInputField();
            _root.gameObject.SetActive(false);
        }

        /// <summary>
        /// Adı kaydeder. Boş bir ad KAYDEDİLMEZ: oyuncu kendi adını silip
        /// çıkarsa her yerde yeniden "Player" görürdü ve bunu bir hata sanardı.
        /// Uyarı yazısı koymak yerine alan sarsılıyor — panel açık kalıyor,
        /// yapılacak şey ortada.
        /// </summary>
        void OnSave()
        {
            string entered = (_field.text ?? "").Trim();
            if (entered.Length == 0)
            {
                GameKit.FX.Juice.Replace(_slot,
                    GameKit.FX.Juice.PunchScale(_slot.transform, 0.18f));
                _field.ActivateInputField();
                return;
            }

            if (MetaServices.Ready)
                MetaServices.Save.Mutate(d => d.PlayerName = entered);

            Hide();
            _onSaved?.Invoke();
        }
    }
}
