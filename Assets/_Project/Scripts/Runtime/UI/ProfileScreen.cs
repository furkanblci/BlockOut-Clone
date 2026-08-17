using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Profil — referanstan ölçülerek yeniden kuruldu.
    /// Referans kare: `menus,powerups,vs.mp4`, 57-60. saniyeler.
    ///
    /// YAPI: degrade başlık + kırmızı çarpı; üstte açık mor bir kart (solda
    /// avatar çerçevesi + üstüne binen yeşil kalem rozeti + altında ad
    /// kapsülü, dikey ayraç, sağda "Level / N"); altında "General Stats"
    /// başlığı ve ince ayraç; sonra 2 sütun × 4 satır koyu kutu — her kutunun
    /// ÜST KENARINA binen bir simge, içinde etiket ve değer.
    ///
    /// DERS (simge kutunun İÇİNDE değil ÜSTÜNDE): Simgeler önce kutunun içine
    /// konmuştu; kutu hem simgeyi hem iki satır yazıyı taşıyınca her şey
    /// küçüldü ve okunmaz oldu. Referans simgeyi kutunun üst kenarına
    /// bindiriyor: simge tam boy kalıyor, kutu yalnız yazıyı taşıyor ve
    /// ikisi arasındaki bağ örtüşmeden doğuyor.
    /// </summary>
    public sealed class ProfileScreen : MonoBehaviour
    {
        /// <summary>Referanstaki sekiz sayaç, aynı sırayla.</summary>
        static readonly (string label, string icon)[] Stats =
        {
            ("First Try Wins",      Art.Trophy),
            ("Sky Jump Wins",       Art.Ufo),
            ("Streak Race Wins",    Art.Rocket),
            ("Block League Wins",   Art.RewardBadge),
            ("Star Blast Cleared",  Art.Star),
            ("Weekly Cup Wins",     Art.Trophy),
            ("Rocket Race Wins",    Art.Rocket),
            ("Max Ufo Climb",       Art.Ufo)
        };

        static readonly Color CardFace = new Color(0.541f, 0.518f, 0.965f);
        static readonly Color BoxFace  = new Color(0.129f, 0.110f, 0.325f);

        TextMeshProUGUI _name, _level;
        readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[Stats.Length];
        NamePanel _namePanel;
        bool _built;

        public static RectTransform Build(Transform parent)
        {
            var root = MenuPage.Screen(parent, "ProfileScreen");
            var screen = root.gameObject.AddComponent<ProfileScreen>();

            // --- Kimlik kartı ---
            var card = MenuPage.Row("Card", root, 330f, 340f, 0.055f, 0.945f);
            var face = UiKit.CreateRoundedPanel("Face", card, CardFace);
            face.pixelsPerUnitMultiplier = 0.30f;
            UiKit.Place(face, 0f, 0f, 1f, 1f);

            // Avatar çerçevesi karttan YUKARI taşar (referansta da öyle).
            var frame = UiKit.CreateRect("Avatar", card);
            UiKit.Place(frame, 0.105f, 0.36f, 0.405f, 1.46f);
            var frameSprite = UiSkin.Get(Art.AvatarFrame);
            if (frameSprite != null)
            {
                var frameImage = UiKit.CreateIcon("Frame", frame, frameSprite);
                UiKit.Place(frameImage, 0f, 0f, 1f, 1f);
            }
            var portrait = UiKit.CreateIcon("Portrait", frame, UiSkin.Get(Art.Avatar));
            UiKit.Place(portrait, 0.13f, 0.13f, 0.87f, 0.87f);

            // Yeşil kalem rozeti: çerçevenin sağ alt köşesine biner.
            //
            // DERS (düğme gibi duran şey BİR ŞEY YAPMALI): Burası uzun süre
            // yalnız yeşil bir daire + eğik beyaz kapsüldü — her oyuncunun
            // "adımı buradan değiştiririm" diye okuyacağı bir işaret, ama
            // altında Button bile yoktu. Mekanik denetimde çıkan dördüncü ölü
            // kontrol. Referansta rozet VAR, yani kaldırmak görünüşü bozardı;
            // doğru çözüm ona gerçek işini vermek.
            var pencil = UiKit.CreateRect("Edit", frame);
            UiKit.Place(pencil, 0.66f, -0.04f, 1.06f, 0.36f);
            var pencilFace = pencil.gameObject.AddComponent<Image>();
            pencilFace.sprite = GameKit.UI.UiSprites.Circle;
            pencilFace.color = MenuPage.Green;
            var pencilButton = pencil.gameObject.AddComponent<Button>();
            pencilButton.targetGraphic = pencilFace;
            pencilButton.transition = Selectable.Transition.None;
            pencil.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();
            pencilButton.onClick.AddListener(screen.OpenNamePanel);
            // Kalem bir YAZI DEĞİL: "✎" Baloo 2'de yok, TMP boş kutu çizer
            // (bu projede beşinci tekrar). Eğik beyaz bir kapsül, o boyutta
            // kalem olarak okunuyor ve her cihazda aynı çıkıyor.
            var pencilMark = MenuPage.Capsule("Mark", pencil, MenuPage.Ink);
            UiKit.Place(pencilMark, 0.26f, 0.42f, 0.74f, 0.58f);
            pencilMark.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);

            var namePlate = MenuPage.Capsule("NamePlate", card, new Color(0.318f, 0.290f, 0.694f));
            UiKit.Place(namePlate, 0.050f, 0.10f, 0.560f, 0.40f);
            screen._name = UiKit.CreateTitle("Name", namePlate.transform, "", 48,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(screen._name, 0.05f, 0.06f, 0.95f, 0.94f);

            var divider = UiKit.CreatePanel("Divider", card, new Color(1f, 1f, 1f, 0.30f));
            UiKit.Place(divider, 0.596f, 0.16f, 0.601f, 0.84f);

            var caption = UiKit.CreateTitle("LevelCaption", card, "Level", 44,
                MenuPage.InkDark, new Color(0.70f, 0.68f, 0.98f));
            UiKit.Place(caption, 0.62f, 0.50f, 0.97f, 0.86f);

            screen._level = UiKit.CreateTitle("Level", card, "1", 64,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(screen._level, 0.62f, 0.12f, 0.97f, 0.52f);

            // --- Başlık + ayraç ---
            var heading = MenuPage.Row("StatsTitle", root, 764f, 92f, 0.05f, 0.95f);
            var headingText = UiKit.CreateTitle("Text", heading, "General Stats", 50,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(headingText, 0f, 0f, 1f, 1f);

            var rule = MenuPage.Row("Rule", root, 866f, 4f, 0.055f, 0.945f);
            var ruleImage = rule.gameObject.AddComponent<Image>();
            ruleImage.color = new Color(1f, 1f, 1f, 0.18f);
            ruleImage.raycastTarget = false;

            // --- 2 × 4 sayaç ızgarası ---
            const float boxH = 214f, gapY = 30f, top = 902f;
            for (int i = 0; i < Stats.Length; i++)
            {
                int col = i % 2, rowIndex = i / 2;
                float x0 = col == 0 ? 0.055f : 0.525f;
                float x1 = col == 0 ? 0.475f : 0.945f;

                var box = MenuPage.Row("Stat_" + i, root, top + rowIndex * (boxH + gapY),
                    boxH, x0, x1);
                var boxFace = UiKit.CreateRoundedPanel("Face", box, BoxFace);
                boxFace.pixelsPerUnitMultiplier = 0.34f;
                UiKit.Place(boxFace, 0f, 0f, 1f, 1f);

                var icon = UiKit.CreateIcon("Icon", box, UiSkin.Get(Stats[i].icon));
                UiKit.Place(icon, 0.33f, 0.66f, 0.67f, 1.34f);

                var label = UiKit.CreateLabel("Label", boxFace.transform, Stats[i].label, 30,
                    new Color(0.639f, 0.647f, 0.918f));
                UiKit.Place(label, 0.04f, 0.40f, 0.96f, 0.74f);

                screen._values[i] = UiKit.CreateTitle("Value", boxFace.transform, "-", 50,
                    MenuPage.Ink, MenuPage.InkDark);
                UiKit.Place(screen._values[i], 0.04f, 0.04f, 0.96f, 0.40f);
            }

            var band = MenuPage.Header(root, "Profile");
            MenuPage.Close(band, () => MenuShell.Instance?.Show("home"));

            // Ad penceresi EN SON kuruluyor: başlık bandının da üstünde kalmalı.
            screen._namePanel = NamePanel.Build(root, screen.OnNameSaved);

            screen._built = true;
            return root;
        }

        void OpenNamePanel() => _namePanel?.Show();

        /// <summary>
        /// Ad değişti: bu ekranın yanı sıra ana ekrandaki avatar baş harfi ve
        /// liderlik satırı da aynı adı okuyor.
        ///
        /// DERS (kurulumu değiştirmek yetmez, TAZELEMEYİ de değiştir): Bu
        /// projede dört kez düşülen tuzak. Adı yalnız kayda yazıp bırakmak,
        /// oyuncunun ana ekrana döndüğünde hâlâ "?" görmesi demek olurdu —
        /// o ekranlar kendi Refresh'lerini açılışta çalıştırıyor ama zaten
        /// AÇIK olan ekran kendiliğinden yenilenmez.
        /// </summary>
        void OnNameSaved()
        {
            Refresh();
            HomeScreen.Instance?.Refresh();
        }

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (!_built || !MetaServices.Ready) return;

            var progress = MetaServices.Progress;
            string playerName = MetaServices.Save.Data.PlayerName;
            _name.text = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
            _level.text = (progress.HighestUnlockedIndex + 1).ToString();

            // Yalnız GERÇEKTEN ölçtüğümüz sayaç dolduruluyor; kalanlar "-".
            //
            // DERS (sahte sayı sahte oyundur): Referansta sekiz sayaç var ama
            // yedisi bizde henüz olmayan etkinliklere ait (Gökyüzü Atlayışı,
            // Blok Ligi, Haftalık Kupa...). Onlara rastgele sayı yazmak ekranı
            // "dolu" gösterir ama ilk bakan kişi bunun uydurma olduğunu anlar.
            // Referans da kazanılmamış sayacı "-" ile gösteriyor.
            _values[0].text = progress.FirstTryClears.ToString();
            for (int i = 1; i < _values.Length; i++) _values[i].text = "-";
        }
    }
}
