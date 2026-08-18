using System.Collections.Generic;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Liderlik Panosu — referanstan ölçülerek yeniden kuruldu.
    /// Referans kare: `menus,powerups,vs.mp4`, 16-22. saniyeler.
    ///
    /// YAPI: başlık bandı; altında ÜÇ SEKME tek bir koyu yuvada (etkin olan
    /// parlak mavi); sekmelerin altına binen küçük geri sayım rozeti; sonra
    /// podyum (2-1-3 sırası, ortadaki yüksek); altında sıralı satırlar; en
    /// altta oyuncunun KENDİ satırı yeşil olarak sabit duruyor.
    ///
    /// DERS (oyuncunun satırı listede DEĞİL, listenin altında sabit): Oyuncu
    /// 1000+. sıradaysa kendi satırını bulmak için binlerce satır kaydırması
    /// gerekir. Referans bunu, kendi satırını listenin dibine yapıştırıp yeşile
    /// boyayarak çözüyor: liste nereye kayarsa kaysın oyuncu kendini görüyor.
    ///
    /// DERS (rakipler SAHTE ama tutarlı): Sunucu yok; rakip listesi sabit bir
    /// dizi. Rastgele üretilseydi her açılışta sıralama değişir ve oyuncu
    /// "puanım düştü mü?" derdi. Sabit rakipler + gerçek oyuncu puanı, sunucu
    /// gelene kadar dürüst bir yaklaşım.
    /// </summary>
    public sealed class LeaderboardScreen : MonoBehaviour
    {
        static readonly string[] TabNames = { "Weekly", "World", "Country" };

        static readonly Color TabSlot   = new Color(0.220f, 0.184f, 0.463f);
        static readonly Color TabIdle   = new Color(0.357f, 0.294f, 0.847f);
        static readonly Color TabActive = new Color(0.098f, 0.612f, 0.976f);
        static readonly Color RowFace   = new Color(0.424f, 0.361f, 0.910f);
        static readonly Color SelfFace  = new Color(0.176f, 0.800f, 0.047f);
        static readonly Color ScoreRed  = new Color(0.690f, 0.071f, 0.263f);
        static readonly Color Podium    = new Color(0.627f, 0.235f, 0.784f);

        /// <summary>
        /// Oyuncunun puanı: bitirilen bölüm, yıldız ve mükemmel geçişlerden.
        /// Yalnız "kaçıncı bölümdeyim" saymak, aynı bölümü daha iyi oynamayı
        /// ödüllendirmezdi.
        /// </summary>
        static int Score(int level, Core.Save.ProgressService progress)
        {
            int score = level * 12;
            if (progress == null) return score;

            // Peek, Record'un aksine eksik kaydı YARATMAZ; burası yalnız
            // okuyor ve her tazelemede çağrılıyor (bkz. ProgressService.Peek).
            for (int i = 0; i < BlockOut.Runtime.Config.LevelCatalog.Count; i++)
            {
                var record = progress.Peek(BlockOut.Runtime.Config.LevelCatalog.IdAt(i));
                if (record == null) continue;
                if (record.Cleared) score += 8;
                if (record.Perfect) score += 14;
            }
            return score;
        }

        /// <summary>
        /// Üç sekmenin ÜÇ AYRI listesi. Sıra: Weekly, World, Country —
        /// <see cref="TabNames"/> ile birebir aynı.
        ///
        /// DERS (sekme LİSTEYİ değiştirmeli, rengi değil): Önce tek bir
        /// `Rivals` dizisi vardı ve üç sekme de onu gösteriyordu; tıklayınca
        /// yalnız sekmenin rengi değişiyordu. Üç ayrı kapı açıp üçünü de aynı
        /// odaya çıkarmak, düğmeyi hiç koymamaktan daha kötü — oyuncu bir
        /// süre farkı aradıktan sonra oyunun bozuk olduğuna karar veriyor.
        ///
        /// Veriler UYDURMA (arka uç yok) ama sekmeler arasında tutarlı:
        /// haftalık puanlar düşük çünkü hafta yeni başladı, dünya listesi en
        /// yüksek, ülke listesi ortada.
        /// </summary>
        static readonly (string name, int score)[][] Boards =
        {
            new[]
            {
                ("mira",         412),
                ("kret",         388),
                ("Bet",          351),
                ("nurhayat",     309),
                ("zzz",          274),
                ("polat",        236),
                ("dotsang",      198),
                ("player_2u1hw", 157)
            },
            new[]
            {
                ("aisha",        980),
                ("Bet",          910),
                ("zzz",          860),
                ("dotsang",      800),
                ("player_2u1hw", 760),
                ("kret",         700),
                ("mira",         640),
                ("polat",        580)
            },
            new[]
            {
                ("polat",        744),
                ("nurhayat",     702),
                ("kret",         661),
                ("mira",         618),
                ("selin",        574),
                ("emre",         522),
                ("zzz",          486),
                ("player_2u1hw", 431)
            }
        };

        /// <summary>
        /// Haftalık ödül: yalnız ilk üç sıra alır (referanstan: 2000/1000/500).
        ///
        /// DERS (kırmızı rozet PUAN değil ÖDÜL): Bu rozet bizde rakibin
        /// puanını gösteriyordu ve SEKİZ satırın hepsinde vardı. Referansta
        /// aynı yerde bir jeton yığını + kırmızı kapsül var ve içindeki sayı
        /// oyuncunun puanı değil, o sıraya verilecek HAFTALIK ÖDÜL — bu yüzden
        /// yalnız ilk üçte duruyor. Puan ise sağdaki "Puan" sütununda.
        /// Aynı görsel iki farklı bilgiyi anlatınca liste yanlış okunuyordu:
        /// 4. sıradaki oyuncu "ödülüm yok" değil "puanım yok" gibi görünüyordu.
        /// </summary>
        static readonly int[] Prizes = { 2000, 1000, 500 };

        /// <summary>O an gösterilen liste.</summary>
        (string name, int score)[] Rivals => Boards[Mathf.Clamp(_activeTab, 0, 2)];

        readonly List<(Image face, TextMeshProUGUI label, int index)> _tabs =
            new List<(Image, TextMeshProUGUI, int)>();
        TextMeshProUGUI _selfRank, _selfName, _selfScore, _countdown;

        // Sekme değişince yenilenecek yazılar.
        readonly TextMeshProUGUI[] _podiumNames = new TextMeshProUGUI[3];
        readonly List<(TextMeshProUGUI name, TextMeshProUGUI score)> _rows =
            new List<(TextMeshProUGUI, TextMeshProUGUI)>();

        int _activeTab = 1;
        bool _built;

        public static RectTransform Build(Transform parent)
        {
            var root = MenuPage.Screen(parent, "LeaderboardScreen");
            var screen = root.gameObject.AddComponent<LeaderboardScreen>();

            screen.BuildTabs(root);
            screen.BuildPodium(root);
            screen.BuildRows(root);
            screen.BuildSelfRow(root);

            MenuPage.Header(root, "Leaderboard");

            // Bilgi balonu EN SON kuruluyor: her şeyin üstünde görünmeli.
            screen._infoToast = UiKit.CreateTitle("InfoToast", root, "", 30,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(screen._infoToast, 0.08f, 0.795f, 0.92f, 0.845f);
            screen._infoToast.gameObject.SetActive(false);

            screen._built = true;
            return root;
        }

        // ---- Sekmeler ------------------------------------------------------

        void BuildTabs(Transform root)
        {
            // SEKMELERİN ARKASI MAVİ, KOYU LACİVERT DEĞİL.
            //
            // DERS (bir ekranın zemini TEK renk olmak zorunda değil): Menü
            // kabuğu bütün sayfalara aynı koyu gövdeyi (#17194E) veriyor ve
            // bu Koleksiyon'da doğru. Ama Liderlik'te referans zemini İKİYE
            // bölüyor: başlığın altındaki sekme bandı parlak mavi (#205DF3
            // üstte, #246CF4 altta — ölçüldü), liste alanı ise koyu lacivert
            // (#1B215B). Tek renk verince sekmelerin durduğu şerit ekranda
            // siyah bir delik gibi kalıyordu — bu ancak TAM EKRAN yakalamada
            // görülüyor, çünkü `CaptureOf<LeaderboardScreen>` yalnız o
            // kanvası çiziyor ve altındaki zemini göstermiyor.
            var band = MenuPage.Row("TabBand", root, MenuPage.HeaderH, 316f);
            var bandFill = band.gameObject.AddComponent<Image>();
            bandFill.color = new Color(0.133f, 0.384f, 0.957f);
            bandFill.raycastTarget = false;

            // Alta doğru hafifçe açılıyor (referansta 0.16'da #205DF3,
            // 0.20'de #246CF4).
            var bandFade = UiKit.CreateRect("Shade", band);
            var bandFadeImage = bandFade.gameObject.AddComponent<Image>();
            bandFadeImage.sprite = MenuSprites.FadeDown;
            bandFadeImage.type = Image.Type.Sliced;
            bandFadeImage.color = new Color(0.141f, 0.424f, 0.969f, 0.85f);
            bandFadeImage.raycastTarget = false;
            UiKit.Place(bandFade, 0f, 0f, 1f, 1f);

            // ÖLÇÜ REFERANSTAN (`sıralama.jpeg`): yuva X 0.107-0.803, yani
            // ekranın SOLUNA yaslı ve sağda "i" bilgi düğmesine yer bırakıyor.
            // Bizimki 0.075-0.925 ile neredeyse tam genişlikti; üç sekme
            // birbirinden kopuk üç düğme gibi duruyordu (17. APK bulgusu).
            var slotRow = MenuPage.Row("Tabs", root, 336f, 112f, 0.107f, 0.803f);
            var slot = MenuPage.Capsule("Slot", slotRow, TabSlot);
            UiKit.Place(slot, 0f, 0f, 1f, 1f);

            for (int i = 0; i < TabNames.Length; i++)
            {
                float x0 = 0.014f + i * 0.3287f;
                var face = MenuPage.Capsule("Tab" + i, slot.transform, TabIdle);
                UiKit.Place(face, x0, 0.10f, x0 + 0.3213f, 0.90f);
                face.raycastTarget = true;

                var label = UiKit.CreateTitle("Label", face.transform, TabNames[i], 40,
                    MenuPage.Ink, MenuPage.InkDark);
                UiKit.Place(label, 0.04f, 0.06f, 0.96f, 0.94f);

                var button = face.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.transition = Selectable.Transition.None;
                face.gameObject.AddComponent<GameKit.UI.UiButtonFeel>();

                int captured = i;
                button.onClick.AddListener(() => { _activeTab = captured; Refresh(); });
                _tabs.Add((face, label, i));
            }

            // Geri sayım rozeti sekmelerin ALT kenarına biner; referansta
            // yuvanın SOL alt köşesinde duruyor.
            var badge = MenuPage.Row("Countdown", root, 424f, 62f, 0.107f, 0.375f);
            var badgeFace = MenuPage.Capsule("Face", badge, TabSlot);
            UiKit.Place(badgeFace, 0f, 0f, 1f, 1f);
            _countdown = UiKit.CreateTitle("Text", badgeFace.transform, "", 26,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(_countdown, 0.06f, 0.06f, 0.94f, 0.94f);

            // "i" bilgi düğmesi: referansta yuvanın SAĞINDA, aynı hizada.
            // Sıralamanın nasıl hesaplandığını anlatan kısa bir bilgi.
            var info = MenuPage.Row("Info", root, 344f, 96f, 0.836f, 0.936f);
            var infoFace = UiKit.CreateIcon("Face", info, GameKit.UI.UiSprites.Circle,
                new Color(0.173f, 0.545f, 0.996f));
            UiKit.Place(infoFace, 0f, 0f, 1f, 1f);
            var infoText = UiKit.CreateTitle("Text", infoFace.transform, "i", 46,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(infoText, 0f, 0f, 1f, 1f);

            UiKit.MakeClickable(info.gameObject, infoFace,
                () => ShowInfo("Scores reset every week. Play levels to climb."));
        }

        TextMeshProUGUI _infoToast;
        float _infoUntil;

        /// <summary>
        /// "i" düğmesinin kısa açıklaması.
        ///
        /// DERS (düğme gibi duran şey bir şey YAPMALI): Referansta bu düğme
        /// var; koymayıp boş bırakmak ya da koyup işlevsiz bırakmak, bu
        /// projede beş kez düşülen tuzağın altıncısı olurdu.
        /// </summary>
        void ShowInfo(string text)
        {
            if (_infoToast == null) return;
            _infoToast.text = text;
            _infoToast.gameObject.SetActive(true);
            _infoUntil = Time.unscaledTime + 3f;
        }

        void Update()
        {
            if (_infoToast != null && _infoToast.gameObject.activeSelf &&
                Time.unscaledTime >= _infoUntil)
                _infoToast.gameObject.SetActive(false);
        }

        // ---- Podyum --------------------------------------------------------

        /// <summary>
        /// İlk üç: ortada birinci ve daha yüksek, solda ikinci, sağda üçüncü.
        ///
        /// DERS (podyumda sıra 2-1-3'tür): Alfabetik ya da 1-2-3 dizmek doğal
        /// geliyor ama gerçek bir podyumda birinci ORTADA ve yüksekte durur;
        /// göz sıralamayı okumadan önce yükseklikten anlar.
        /// </summary>
        void BuildPodium(Transform root)
        {
            var band = MenuPage.Row("Podium", root, 556f, 486f);
            var sky = band.gameObject.AddComponent<Image>();
            sky.color = new Color(0.227f, 0.627f, 0.910f);

            // PARK ZEMİNİ: referansta kürsüler düz mavi bir dikdörtgenin
            // üstünde değil, ağaçlı yeşil bir sahnenin içinde duruyor.
            //
            // DERS (podyum bir SAHNEDİR): Düz mavi zemin kürsüleri "arayüz
            // öğesi" gibi gösteriyor; birkaç ağaç ve çim şeridi onları bir
            // YERE koyuyor ve kutlama hissi oradan geliyor. Kullanıcının
            // "podyum tasarımları çok zayıf" dediği farkın büyük kısmı bu
            // (16. APK bulgusu).
            //
            // Ağaçlar tek tek çizilmiyor: yuvarlak panelden üst üste iki
            // daire (koyu taç + açık taç) ve ince bir gövde. Uzakta duran
            // süs için yeterli, kendi görseline gerek yok.
            var grass = UiKit.CreateRoundedPanel("Grass", band,
                new Color(0.361f, 0.729f, 0.235f));
            UiKit.SetSliceScale(grass, 0.22f);
            grass.raycastTarget = false;
            UiKit.Place(grass, -0.04f, -0.02f, 1.04f, 0.30f);

            // Ağaçlar kürsülerin ARKASINDA kalmalı; bu yüzden kürsülerden
            // ÖNCE kuruluyorlar (kardeş sırası = çizim sırası).
            //
            // Yalnız İKİ KENARDA duruyorlar: kürsüler 0.065-0.935 arasını
            // neredeyse tamamen kaplıyor, aradaki bir ağaç hiç görünmezdi.
            // İlk denemede dört ağaç kondu ve ikisi kürsülerin arkasında
            // kayboldu — görünmeyen bir süs, olmayan bir süstür.
            foreach (float cx in new[] { 0.045f, 0.955f })
            {
                const float w = 0.115f, h = 0.46f;

                var trunk = UiKit.CreatePanel("Trunk", band, new Color(0.451f, 0.278f, 0.125f));
                trunk.raycastTarget = false;
                UiKit.Place(trunk, cx - w * 0.16f, 0.16f, cx + w * 0.16f, 0.40f);

                var crownLow = UiKit.CreateIcon("CrownLow", band, GameKit.UI.UiSprites.Circle,
                    new Color(0.192f, 0.545f, 0.180f));
                crownLow.raycastTarget = false;
                UiKit.Place(crownLow, cx - w, 0.34f, cx + w, 0.34f + h * 0.66f);

                var crownTop = UiKit.CreateIcon("CrownTop", band, GameKit.UI.UiSprites.Circle,
                    new Color(0.310f, 0.694f, 0.243f));
                crownTop.raycastTarget = false;
                UiKit.Place(crownTop, cx - w * 0.74f, 0.50f, cx + w * 0.74f, 0.50f + h * 0.62f);
            }

            // (x merkezi, yarım genişlik, yükseklik, madalya no)
            //
            // DERS (bitişik ≠ birleşik): Üç kürsü de 0.155 yarım genişlikteydi
            // ve merkezleri 0.22/0.50/0.78'di — yani komşular 0.03 kadar
            // ÜST ÜSTE biniyordu. Aynı krem rengi paylaştıkları için ekranda
            // üç kürsü değil tek bir krem kütle görünüyordu; "podyum
            // tasarımları çok zayıf" şikâyetinin ölçülebilir kısmı buydu.
            // Referansta kürsüler yan yana ama ARALARINDA boşluk var ve
            // ortadaki belirgin biçimde GENİŞ (0.34-0.66; yanlar 0.107-0.325
            // ve 0.671-0.905). Genişlik de bir sıralama işareti.
            var slots = new[]
            {
                (0.216f, 0.109f, 282f, 2),
                (0.500f, 0.160f, 364f, 1),
                (0.788f, 0.117f, 282f, 3)
            };
            var medal = new[] { default(Color),
                new Color(1f, 0.82f, 0.25f), new Color(0.78f, 0.80f, 0.85f),
                new Color(0.82f, 0.52f, 0.28f) };

            foreach (var (cx, halfWidth, height, place) in slots)
            {
                // Kürsüler ÇİMİN ÜSTÜNE oturuyor, bandın en altından değil.
                // Aksi hâlde çim şeridi tamamen kürsülerin arkasında kalıyor
                // ve sahne yine düz bir dikdörtgen gibi okunuyordu.
                var pillar = UiKit.CreateRect("Place" + place, band);
                UiKit.Place(pillar, cx - halfWidth, 0.15f, cx + halfWidth, height / 486f);

                // Kenarlık: komşusuyla arasında boşluk OLSA DA her kürsünün
                // kendi sınırı olmalı — krem üstüne krem, ışık olmadan sınır
                // vermiyor.
                var edge = UiKit.CreateRoundedPanel("Edge", pillar,
                    new Color(0.702f, 0.627f, 0.494f));
                UiKit.SetSliceScale(edge, 0.30f);
                edge.raycastTarget = false;
                UiKit.Place(edge, 0f, 0f, 1f, 0.82f);

                var body = UiKit.CreateRoundedPanel("Body", pillar,
                    new Color(0.910f, 0.851f, 0.753f));
                UiKit.SetSliceScale(body, 0.32f);
                body.raycastTarget = false;
                UiKit.Place(body, 0f, 0f, 1f, 0.82f, padding: 7f);

                // Kapak yassı bir ELİPS değil, kalın bir dilim: yarıçap kutu
                // yüksekliğinin yarısını geçince yuvarlak panel elipse dönüyor.
                var capEdge = UiKit.CreateRoundedPanel("CapEdge", pillar,
                    MenuPage.Darken(Podium, 0.72f));
                UiKit.SetSliceScale(capEdge, 0.70f);
                capEdge.raycastTarget = false;
                UiKit.Place(capEdge, 0f, 0.74f, 1f, 1f);

                var cap = UiKit.CreateRoundedPanel("Cap", pillar, Podium);
                UiKit.SetSliceScale(cap, 0.75f);
                cap.raycastTarget = false;
                UiKit.Place(cap, 0f, 0.74f, 1f, 1f, padding: 7f);

                var frame = UiKit.CreateRect("Avatar", pillar);
                UiKit.Place(frame, 0.15f, 0.97f, 0.85f, 1.46f);
                var frameArt = UiSkin.Get(Art.AvatarFrame);
                if (frameArt != null)
                {
                    var f = UiKit.CreateIcon("Frame", frame, frameArt);
                    UiKit.Place(f, 0f, 0f, 1f, 1f);
                }
                // Her basamakta FARKLI yüz: üç podyum da aynı avatarı
                // taşıdığında tablo tek kişinin tekrarı gibi okunuyordu.
                var portrait = UiKit.CreateIcon("Portrait", frame,
                    UiSkin.Get(Art.Rival(place - 1)));
                UiKit.Place(portrait, 0.14f, 0.14f, 0.86f, 0.86f);

                var plate = MenuPage.Capsule("Name", pillar, new Color(0.949f, 0.902f, 0.824f));
                UiKit.Place(plate, 0.02f, 0.46f, 0.98f, 0.72f);
                // PUNTO REFERANSTAN: ad levhasındaki yazının büyük harf
                // yüksekliği ekranın %1.27'si → ~34 punto. 24 idi.
                // Kürsüler dar olduğu için uzun adlarda küçülmesine izin var.
                var nameText = UiKit.CreateLabel("Text", plate.transform,
                    Rivals[place - 1].name, 34, new Color(0.28f, 0.20f, 0.12f));
                nameText.fontStyle = FontStyles.Bold;
                nameText.overflowMode = TextOverflowModes.Ellipsis;
                nameText.enableAutoSizing = true;
                nameText.fontSizeMin = 20f;
                nameText.fontSizeMax = 34f;
                UiKit.Place(nameText, 0.06f, 0.06f, 0.94f, 0.94f);

                // Sekme değişince ad yenilenecek; basamak sırasıyla saklıyoruz
                // (döngü 2-1-3 sırasında geziyor, dizi 0-1-2 olmalı).
                _podiumNames[place - 1] = nameText;

                // MADALYA DAİRE OLMALI.
                //
                // DERS (kutu oranı şeklin oranıdır): Madalya düz bir `Image`
                // olarak 0.32-0.68 × 0.10-0.42 kutusuna konuyordu. Kürsü 335
                // birim genişken kutu 120×90 çıkıyor ve daire YUMURTAYA
                // dönüyordu. `CreateIcon` en-boy oranını koruduğu için aynı
                // kutuda daire kalıyor — bu projede aynı hata yeşil artı
                // düğmesinde de yaşanmıştı.
                var disc = UiKit.CreateRect("Medal", pillar);
                UiKit.Place(disc, 0.20f, 0.08f, 0.80f, 0.44f);
                var rimDisc = UiKit.CreateIcon("Rim", disc, GameKit.UI.UiSprites.Circle,
                    MenuPage.Darken(medal[place], 0.70f));
                UiKit.Place(rimDisc, 0f, 0f, 1f, 1f);
                var discImage = UiKit.CreateIcon("Face", disc, GameKit.UI.UiSprites.Circle,
                    medal[place]);
                UiKit.Place(discImage, 0.09f, 0.09f, 0.91f, 0.91f);
                var number = UiKit.CreateTitle("No", disc, place.ToString(), 40,
                    new Color(0.32f, 0.20f, 0.04f), new Color(1f, 0.94f, 0.72f));
                UiKit.Place(number, 0f, 0f, 1f, 1f);
            }
        }

        // ---- Sıralı satırlar -------------------------------------------------

        /// <summary>
        /// Sıralama satırları — İLK ÜÇ DE LİSTEDE.
        ///
        /// DERS (podyum listenin yerini tutmaz): Burası kasten `Rivals[i + 3]`
        /// ile başlıyordu; gerekçe "podyumdaki yüz iki kez görünmesin"di ve
        /// mantıklı geliyordu. Referans (`sıralama.jpeg`) tam tersini yapıyor:
        /// 1 Ella, 2 Fikret, 3 KOR podyumda DA listede DE var, üstelik
        /// listedeki sıra rozetleri madalya. Sebebi şu — podyum bir kutlama,
        /// liste bir CETVELdir. Cetvelden ilk üçü çıkarınca oyuncu "ben
        /// kaçıncıyım, önümde kim var" sorusunu cevaplayamıyor; 4. sıradaki
        /// oyuncu listenin başında görünüp birinci sanılıyor.
        ///
        /// Yan etki: sıra numaraları da uydurma değil artık. Eskiden 997-1000
        /// yazıyordu ama veri puana göre sıralı, yani o satırlar aslında
        /// 4-7. sıralardı. Ekranda görünen sayı ile verinin anlattığı şey
        /// birbirini tutmuyordu.
        /// </summary>
        void BuildRows(Transform root)
        {
            const float rowH = 158f, gap = 24f, step = rowH + gap;
            const float top = 1006f;

            // KAYDIRILABİLİR LİSTE.
            //
            // DERS (liste, sığdığı kadarından ibaret değildir): Ekranda beş
            // satır görünüyordu ve o kadarı sabitti — elimizde sekiz rakip
            // olmasına rağmen kalan üçüne ulaşmanın yolu yoktu. Kullanıcı
            // "sıralamadaki insanları hareket ettiremiyoruz" derken bunu
            // gördü. Sıralama listesi doğası gereği kaydırılır; sığan kadarını
            // gösterip gerisini atmak, listeyi bir CETVEL olmaktan çıkarıp
            // vitrine çevirir.
            // GÖRÜNTÜ ALANI SEKME ÇUBUĞUNDA BİTMELİ.
            //
            // DERS (kaydırılabilir alan, ekranın SERBEST kısmı kadardır):
            // İlk kurulumda görüntü alanını beş satır yüksekliğinde yaptım ve
            // sabit "You" satırı ekranın en altına, sekme çubuğunun ARKASINA
            // düştü. Çubuk ekranın alt %9.9'unu kaplıyor; kaydırma alanı
            // oraya kadar değil, sabit satır + çubuk payı DÜŞÜLDÜKTEN sonra
            // kalan yere kadar uzayabilir.
            //
            // Çubuğun üstü 1730, sabit satır 158 + payları → görüntü alanı
            // 1528'de bitiyor.
            const float viewportBottom = 1528f;
            var viewport = MenuPage.Row("RowsViewport", root, top,
                viewportBottom - top, 0f, 1f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = UiKit.CreateRect("Rows", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, Rivals.Length * step);
            content.anchoredPosition = Vector2.zero;

            // Görünmez dokunuş yüzeyi — içeriğin İLK çocuğu. (Yolculuk'ta
            // öğrenildi: viewport'a koymak kardeş öğeleri örtüyor.)
            var catcher = UiKit.CreatePanel("TouchCatcher", content,
                new Color(0f, 0f, 0f, 0f));
            catcher.raycastTarget = true;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.scrollSensitivity = 45f;
            scroll.decelerationRate = 0.12f;

            for (int i = 0; i < Rivals.Length; i++)
            {
                var rival = Rivals[i];
                var row = MenuPage.Row("Row" + i, content, i * step, rowH,
                    0.035f, 0.965f);
                int prize = i < Prizes.Length ? Prizes[i] : 0;
                var (_, nameText, scoreText) =
                    BuildRowContent(row, (i + 1).ToString(), rival.name, prize,
                        rival.score, RowFace, i);
                _rows.Add((nameText, scoreText));
            }
        }

        /// <summary>Aynı anda görünen satır sayısı (referansta beş).</summary>
        const int RowCount = 5;

        /// <summary>Sıra rozetinin madalya renkleri (1=altın, 2=gümüş, 3=bronz).</summary>
        static readonly Color[] MedalColors =
        {
            default,
            new Color(1.000f, 0.796f, 0.180f),
            new Color(0.804f, 0.831f, 0.878f),
            new Color(0.827f, 0.518f, 0.267f)
        };

        void BuildSelfRow(Transform root)
        {
            // Kaydırılan listenin ALTINDA, sekme çubuğunun ÜSTÜNDE sabit.
            var row = MenuPage.Row("Self", root, 1552f, 158f, 0.035f, 0.965f);
            // Oyuncu ilk üçte olmadığı için ödül kapsülü YOK (prize = 0);
            // puanı ise sağdaki sütunda, herkesle aynı yerde.
            var (rankText, nameText, scoreText) =
                BuildRowContent(row, "1000+", "You", 0, 0, SelfFace);
            _selfRank = rankText;
            _selfName = nameText;
            _selfScore = scoreText;
        }

        /// <summary>Tek satır: sıra · avatar · ad · (ödül) · puan.</summary>
        (TextMeshProUGUI rank, TextMeshProUGUI name, TextMeshProUGUI score)
            BuildRowContent(Transform row, string rank, string name, int prize, int score,
                            Color faceColor, int avatarIndex = -1)
        {
            var face = MenuPage.Capsule("Face", row, faceColor);
            UiKit.Place(face, 0f, 0f, 1f, 1f);

            // Sıra numarası KENDİ kapsülünde ve satırın sol ucundan taşıyor —
            // referansta satır, sıra rozetinin etrafında bir çentik gibi
            // duruyor. Düz metin olarak bırakınca isimle aynı ağırlıkta
            // okunuyordu; oysa sıra listenin ana bilgisi.
            // İLK ÜÇ MADALYA RENGİNDE.
            //
            // DERS (sıra numarası bir DEĞER taşır): Sekiz satırın rozeti de
            // aynı koyu mordu; liste bir numaralandırmadan ibaret kalıyordu.
            // Referansta ilk üçün rozeti altın/gümüş/bronz — göz listeye
            // bakar bakmaz zirveyi buluyor. Renk burada süs değil, BİLGİ.
            bool medalled = int.TryParse(rank, out int place) && place >= 1 && place <= 3;
            var slotColor = medalled ? MedalColors[place] : MenuPage.Darken(faceColor, 0.72f);

            var rankSlot = MenuPage.Capsule("RankSlot", face.transform, slotColor);
            UiKit.Place(rankSlot, -0.005f, 0.04f, 0.155f, 0.96f);

            // Madalyalı rozette yazı KOYU: altın/gümüş üstünde beyaz okunmuyor.
            var rankInk = medalled ? new Color(0.24f, 0.14f, 0.03f) : MenuPage.Ink;
            var rankEdge = medalled ? new Color(1f, 0.96f, 0.86f) : MenuPage.InkDark;

            var rankText = UiKit.CreateTitle("Rank", rankSlot.transform, rank, 36,
                rankInk, rankEdge);
            UiKit.Place(rankText, 0.05f, 0.06f, 0.95f, 0.94f);

            // Avatar KARE çerçevede (referansta da öyle); çerçevesiz portre
            // satırın içinde yüzüyor gibi duruyordu.
            var avatar = UiKit.CreateRect("Avatar", face.transform);
            UiKit.Place(avatar, 0.175f, 0.06f, 0.305f, 0.94f);
            var frame = UiKit.CreateRoundedPanel("Frame", avatar,
                new Color(0.788f, 0.769f, 0.976f));
            UiKit.SetSliceScale(frame, 0.55f);
            UiKit.Place(frame, 0f, 0f, 1f, 1f);
            // avatarIndex < 0 => OYUNCUNUN kendi satırı; kendi avatarını taşır.
            var portrait = UiKit.CreateIcon("Portrait", frame.transform,
                UiSkin.Get(avatarIndex < 0 ? Art.Avatar : Art.Rival(avatarIndex)));
            UiKit.Place(portrait, 0.10f, 0.10f, 0.90f, 0.90f);

            var nameText = UiKit.CreateTitle("Name", face.transform, name, 44,
                MenuPage.Ink, MenuPage.InkDark);
            nameText.alignment = TextAlignmentOptions.Left;

            // İsim uzunluğu KONTROL EDİLEMEZ (oyuncu yazıyor, rakipler farklı
            // uzunlukta). Sabit punto bırakılırsa "player_2u1hw" gibi bir ad
            // puan rozetinin altına giriyor. Otomatik küçülme, alanı taşırmak
            // yerine yazıyı sığdırıyor; alt sınır okunabilirliği koruyor.
            // DERS (otomatik küçülme TAŞMA KİPİNE bağlıdır): `enableAutoSizing`
            // tek başına yetmedi — UiKit etiketleri `overflowMode = Overflow`
            // ile kuruyor, yani TMP yazının alana sığmadığını hiç "taşma"
            // saymıyor ve küçültmeye gerek duymuyor. Kısıtlayıcı bir kip
            // (Ellipsis) verilmeden otomatik boyut sessizce çalışmaz.
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 26f;
            nameText.fontSizeMax = 44f;
            // İsim kutusu puan rozetine DEĞMEDEN bitiyor. Otomatik küçülme yazıyı
            // kutuya sığdırıyor ama kutu rozetin dibinde başlıyorsa sığmış yazı
            // yine bitişik okunur; boşluk kutunun kendisinden gelmeli.
            // İsim kutusu ödül kapsülüne kadar uzayabilir (eski puan rozeti
            // daha soldaydı, isim de ona göre kısaydı).
            UiKit.Place(nameText, 0.335f, 0.14f, 0.680f, 0.86f);

            // HAFTALIK ÖDÜL — jeton yığını + kırmızı kapsül, yalnız ilk üçte.
            //
            // ÖLÇÜ REFERANSTAN (`sıralama.jpeg`, 1. satır): ödül grafiği
            // satırın X %71.9-%86'sında, kırmızı kapsül satırın alt
            // %17-%40'ında, jeton yığını onun üstünde.
            if (prize > 0)
            {
                var prizeRoot = UiKit.CreateRect("Prize", face.transform);
                UiKit.Place(prizeRoot, 0.706f, 0.10f, 0.868f, 0.96f);

                // Yığın kapsülün ARKASINDAN çıkıyor: önce kurulup altta kalıyor.
                var pile = UiKit.CreateIcon("Pile", prizeRoot,
                    UiSkin.Get(Art.CoinPile(2)));
                UiKit.Place(pile, 0.02f, 0.30f, 0.98f, 1f);

                var rim = MenuPage.Capsule("Rim", prizeRoot,
                    new Color(0.965f, 0.769f, 0.259f));
                UiKit.Place(rim, 0f, 0f, 1f, 0.36f);

                var badge = MenuPage.Capsule("Face", prizeRoot, ScoreRed);
                UiKit.Place(badge, 0.030f, 0.045f, 0.970f, 0.325f);

                var amount = UiKit.CreateTitle("Amount", badge.transform, prize.ToString(), 32,
                    MenuPage.Ink, new Color(0.34f, 0.03f, 0.10f));
                UiKit.Place(amount, 0.05f, 0.02f, 0.95f, 0.98f);
            }

            // SAĞDAKİ SÜTUN PUAN, SEVİYE DEĞİL.
            //
            // DERS (ölü kod bir SORUNUN izidir): Bu sınıfta `Score(level,
            // progress)` diye bir yardımcı var — bitirilen bölüm, yıldız ve
            // mükemmel geçişlerden puan hesaplıyor. Hiçbir yerden çağrılmıyordu,
            // çünkü ekran onun yerine seviye numarasını yazıyordu. Referansta
            // o sütunun başlığı "Puan". Yani yazılmış ama bağlanmamış bir
            // hesap, ekranda yanlış bir bilgiyle örtülmüş duruyordu.
            var caption = UiKit.CreateLabel("ScoreCaption", face.transform, "Score", 24,
                new Color(1f, 1f, 1f, 0.72f));
            UiKit.Place(caption, 0.875f, 0.50f, 0.995f, 0.86f);

            var scoreText = UiKit.CreateTitle("Score", face.transform, score.ToString(), 38,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(scoreText, 0.875f, 0.12f, 0.995f, 0.52f);

            return (rankText, nameText, scoreText);
        }

        // ---- Tazeleme --------------------------------------------------------

        void OnEnable() => Refresh();

        public void Refresh()
        {
            if (!_built) return;

            foreach (var (face, label, index) in _tabs)
            {
                bool on = index == _activeTab;
                face.color = on ? TabActive : TabIdle;
                label.color = on ? MenuPage.Ink : new Color(1f, 1f, 1f, 0.78f);
            }

            // Sekmenin ASIL işi: listeyi değiştirmek. Renk yalnız hangisinin
            // seçili olduğunu söyler.
            var board = Rivals;
            for (int i = 0; i < _podiumNames.Length && i < board.Length; i++)
                if (_podiumNames[i] != null) _podiumNames[i].text = board[i].name;

            for (int i = 0; i < _rows.Count && i < board.Length; i++)
            {
                var rival = board[i];
                var (nameText, scoreText) = _rows[i];
                if (nameText != null) nameText.text = rival.name;
                if (scoreText != null) scoreText.text = rival.score.ToString();
            }

            // Haftalık sıfırlanmaya kalan süre — sahte bir "acele et" değil,
            // gerçek hafta sonu.
            var left = System.DateTime.UtcNow.Date.AddDays(
                7 - (int)System.DateTime.UtcNow.DayOfWeek) - System.DateTime.UtcNow;
            _countdown.text = $"# {left.Days}d {left.Hours}h";

            if (!MetaServices.Ready) return;
            int level = MetaServices.Progress.HighestUnlockedIndex + 1;
            string playerName = MetaServices.Save.Data.PlayerName;

            _selfName.text = string.IsNullOrEmpty(playerName) ? "You" : playerName;
            _selfScore.text = Score(level, MetaServices.Progress).ToString();
            _selfRank.text = "1000+";
        }
    }
}
