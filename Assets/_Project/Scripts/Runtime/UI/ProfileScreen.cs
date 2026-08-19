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
            // `badge_reward` BURAYA UYMUYOR: o görsel içi boş turuncu bir
            // ÇERÇEVE (ortasında mor pencere), tek başına konunca "yüklenmemiş
            // ikon" gibi duruyor. Referanstaki amblem elimizde yok; küre
            // "lig" fikrine en yakın olan.
            ("Block League Wins",   Art.Globe),
            ("Star Blast Cleared",  Art.Star),
            ("Weekly Cup Wins",     Art.Trophy),
            ("Rocket Race Wins",    Art.Rocket),
            ("Max Ufo Climb",       Art.Ufo)
        };

        // RENKLER REFERANSTAN ÖLÇÜLDÜ (`profil.jpeg`, 2026-08-17):
        // kart #A79BFD, kutu dolgusu #1D1450, kutu kenarlığı #2A1E74.
        //
        // DERS (kenarlık dolgudan AÇIK, zeminden KOYU): Kutuların ince
        // kenarlığı yoktu ve koyu kutular açık mor zeminin üstünde "kesilmiş
        // delik" gibi duruyordu. Referansta 4 pikselik bir ara ton var:
        // zeminden koyu, dolgudan açık. O tek şerit kutuyu delik olmaktan
        // çıkarıp YÜZEY yapıyor.
        static readonly Color CardFace = new Color(0.655f, 0.608f, 0.992f);
        static readonly Color BoxFace  = new Color(0.114f, 0.078f, 0.314f);
        static readonly Color BoxEdge  = new Color(0.165f, 0.118f, 0.455f);

        TextMeshProUGUI _name, _level;
        readonly TextMeshProUGUI[] _values = new TextMeshProUGUI[Stats.Length];
        NamePanel _namePanel;
        bool _built;

        public static RectTransform Build(Transform parent)
        {
            // Zemin diğer menü sayfalarından AÇIK — referansta öyle ve koyu
            // istatistik kutularının ayrışması buna bağlı (bkz. MenuPage).
            var root = MenuPage.Screen(parent, "ProfileScreen", MenuPage.BodyProfile);
            var screen = root.gameObject.AddComponent<ProfileScreen>();

            // --- Kimlik kartı ---
            // ÖLÇÜ REFERANSTAN: kart y 413-702 (946×2048) → bizim tuvalde
            // üst 387, yükseklik 271; X 0.058-0.941. Eskisi 330/340'tı, yani
            // hem yukarıda başlıyor hem 69 birim fazla uzuyordu.
            var card = MenuPage.Row("Card", root, 387f, 271f, 0.058f, 0.941f);
            var face = UiKit.CreateRoundedPanel("Face", card, CardFace);
            UiKit.SetSliceScale(face, 0.30f);
            UiKit.Place(face, 0f, 0f, 1f, 1f);

            // Avatar çerçevesi karttan YUKARI taşar (referansta da öyle).
            var frame = UiKit.CreateRect("Avatar", card);
            UiKit.Place(frame, 0.173f, 0.446f, 0.492f, 1.417f);
            var frameSprite = UiSkin.Get(Art.AvatarFrame);
            if (frameSprite != null)
            {
                var frameImage = UiKit.CreateIcon("Frame", frame, frameSprite);
                UiKit.Place(frameImage, 0f, 0f, 1f, 1f);
            }
            // Portre çerçevenin İÇİNDE kalmalı: 0.13 payında karakterin başı
            // çerçevenin üst çubuğunun üstüne taşıyordu.
            var portrait = UiKit.CreateIcon("Portrait", frame, UiSkin.Get(Art.Avatar));
            UiKit.Place(portrait, 0.17f, 0.17f, 0.83f, 0.83f);

            // Yeşil kalem rozeti: çerçevenin sağ alt köşesine biner.
            //
            // DERS (düğme gibi duran şey BİR ŞEY YAPMALI): Burası uzun süre
            // yalnız yeşil bir daire + eğik beyaz kapsüldü — her oyuncunun
            // "adımı buradan değiştiririm" diye okuyacağı bir işaret, ama
            // altında Button bile yoktu. Mekanik denetimde çıkan dördüncü ölü
            // kontrol. Referansta rozet VAR, yani kaldırmak görünüşü bozardı;
            // doğru çözüm ona gerçek işini vermek.
            // ÖLÇÜ REFERANSTAN: rozet 70×66 piksel (daire), çerçevenin sağ alt
            // köşesinde. Bizimki 121×105 birimlik bir kutuydu; hem büyüktü hem
            // düz `Image` olduğu için daire ELİPSE geriliyordu.
            var pencil = UiKit.CreateRect("Edit", frame);
            UiKit.Place(pencil, 0.779f, 0.029f, 1.043f, 0.333f);
            var pencilFace = UiKit.CreateIcon("Face", pencil,
                GameKit.UI.UiSprites.Circle, MenuPage.Green);
            UiKit.Place(pencilFace, 0f, 0f, 1f, 1f);
            UiKit.MakeClickable(pencil.gameObject, pencilFace, screen.OpenNamePanel);
            // KALEM ARTIK GERÇEK BİR KALEM ŞEKLİ (4. tur, C10).
            //
            // Kullanıcı: "Player çerçevesinin sağ alt köşesindeki düzenleme
            // (edit) ikonu kötü — yeni görsel üretilecek."
            //
            // Burada eskiden 45 derece döndürülmüş beyaz bir KAPSÜL vardı.
            // Kapsülün iki ucu da yuvarlak olduğu için ekranda "beyaz bir
            // oval" okunuyordu. Yeni şekil `GameKit.UI.UiSprites.Pencil`:
            // düz kesimli gövde, arkada silgi bandı, önde GERÇEK bir sivri uç.
            //
            // DERS (bir simgeyi tanınır kılan şey oranı değil AYIRT EDİCİ
            // ayrıntısıdır): Uzunluk/kalınlık oranını doğru tutmak yetmedi;
            // sivri uç olmadan o şekil bir kalem değil, bir çubuk. Simgeyi
            // taşıyan bilgi çoğu zaman en küçük parçasında saklı.
            var pencilMark = UiKit.CreateIcon("Mark", pencil,
                GameKit.UI.UiSprites.Pencil, MenuPage.Ink);
            UiKit.Place(pencilMark, 0.14f, 0.14f, 0.86f, 0.86f);
            pencilMark.preserveAspect = true;
            pencilMark.raycastTarget = false;

            // Ucu: kalemin sivri tarafı. Küçük koyu bir üçgen yerine kısa bir
            // koyu çizgi — o boyutta ikisi aynı şeyi anlatıyor.
            var pencilTip = MenuPage.Capsule("Tip", pencil, new Color(0.20f, 0.14f, 0.05f));
            UiKit.Place(pencilTip, 0.20f, 0.455f, 0.32f, 0.545f);
            pencilTip.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);

            // Ad kapsülü avatarın ALTINDA ve ondan geniş (referansta X
            // 0.119-0.590 ekran, avatar 0.211-0.492): levha avatarın iki
            // yanından da taşıyor, yani ikisi tek bir kimlik bloğu okunuyor.
            var namePlate = MenuPage.Capsule("NamePlate", card, new Color(0.318f, 0.290f, 0.694f));
            UiKit.Place(namePlate, 0.069f, 0.089f, 0.602f, 0.380f);
            screen._name = UiKit.CreateTitle("Name", namePlate.transform, "", 48,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(screen._name, 0.05f, 0.06f, 0.95f, 0.94f);

            var divider = UiKit.CreatePanel("Divider", card, new Color(1f, 1f, 1f, 0.30f));
            UiKit.Place(divider, 0.663f, 0.16f, 0.669f, 0.84f);

            // PUNTO REFERANSTAN: "Seviye" cap yüksekliği 35 birim (→ ~52
            // punto), sayı 53 birim (→ ~74). 44/64 idi.
            var caption = UiKit.CreateTitle("LevelCaption", card, "Level", 52,
                MenuPage.InkDark, new Color(0.70f, 0.68f, 0.98f));
            UiKit.Place(caption, 0.707f, 0.531f, 0.933f, 0.727f);

            screen._level = UiKit.CreateTitle("Level", card, "1", 74,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(screen._level, 0.707f, 0.247f, 0.933f, 0.480f);

            // --- Başlık + ayraç ---
            var heading = MenuPage.Row("StatsTitle", root, 700f, 92f, 0.05f, 0.95f);
            var headingText = UiKit.CreateTitle("Text", heading, "General Stats", 50,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(headingText, 0f, 0f, 1f, 1f);

            var rule = MenuPage.Row("Rule", root, 790f, 4f, 0.055f, 0.945f);
            var ruleImage = rule.gameObject.AddComponent<Image>();
            ruleImage.color = new Color(1f, 1f, 1f, 0.18f);
            ruleImage.raycastTarget = false;

            // --- 2 × 4 sayaç ızgarası ---
            // ARALIK, İKONUN TAŞMASINI HESABA KATMALI.
            //
            // DERS (bir öğe taşıyorsa komşusunun payı da o kadar artmalı):
            // İkon kutunun ÜST kenarından %34 taşıyor (tasarım böyle, referans
            // da öyle). Ama satır aralığı 30 birimdi; yani sonraki satırın
            // taşan ikonu, önceki kutunun ALTINDAKİ DEĞER yazısının üstüne
            // biniyordu. Ekranda "0" rakamı roketin üstünde yüzüyor gibi
            // görünüyordu ve tablo düzensiz okunuyordu (21. APK bulgusu).
            // Taşma 0.34 × kutu yüksekliği kadar, aralık en az o kadar olmalı.
            //
            // SATIR ADIMI REFERANSTAN — DİKEY TARAMAYLA ÖLÇÜLDÜ.
            //
            // İlk ölçüm gözle yapılmıştı ve 152 + 80 = 232 vermişti. Kutu
            // dolgusunun koyu olduğunu bilerek yapılan dikey tarama gerçek
            // sayıları verdi: kutular orijinalde y 950-1123, 1219-1391,
            // 1495-1667, 1767-1939 → yükseklik 172, adım 272 (bizim tuvalde
            // 161 ve 255). Yani hem kutular hem aralık %6 küçüktü ve dördüncü
            // satır referanstan 40 birim yukarıda kalıyordu.
            //
            // Aralık ikonun taşmasını da karşılıyor: ikon kutunun üstünden
            // %34 taşıyor (161 × 0.34 = 55), aralık 94.
            const float boxH = 161f, gapY = 94f, top = 891f;
            for (int i = 0; i < Stats.Length; i++)
            {
                int col = i % 2, rowIndex = i / 2;
                float x0 = col == 0 ? 0.055f : 0.525f;
                float x1 = col == 0 ? 0.475f : 0.945f;

                var box = MenuPage.Row("Stat_" + i, root, top + rowIndex * (boxH + gapY),
                    boxH, x0, x1);
                var boxEdge = UiKit.CreateRoundedPanel("Edge", box, BoxEdge);
                UiKit.SetSliceScale(boxEdge, 0.34f);
                boxEdge.raycastTarget = false;
                UiKit.Place(boxEdge, 0f, 0f, 1f, 1f);

                var boxFace = UiKit.CreateRoundedPanel("Face", box, BoxFace);
                UiKit.SetSliceScale(boxFace, 0.36f);
                boxFace.raycastTarget = false;
                UiKit.Place(boxFace, 0f, 0f, 1f, 1f, padding: 5f);

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
