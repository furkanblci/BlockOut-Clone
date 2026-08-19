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

        // SEKME RENKLERİ REFERANSTAN YENİDEN ÖLÇÜLDÜ (2026-08-18, `sıralama.jpeg`).
        // Eskiler göz kararıydı ve üçü de referanstan koyu/soluk kalıyordu;
        // kullanıcının "Weekly/World/Country kısmı çok detaysız" bulgusunun
        // yarısı renk, yarısı katman eksikliğiydi.
        static readonly Color TabSlot   = new Color(0.212f, 0.157f, 0.631f);  // #3628A1 koyu kuyu
        static readonly Color TabRim    = new Color(0.396f, 0.325f, 0.929f);  // kuyunun dış bileziği
        static readonly Color TabIdle   = new Color(0.396f, 0.325f, 0.992f);  // #6553FD
        static readonly Color TabActive = new Color(0.000f, 0.522f, 0.996f);  // #0085FE
        static readonly Color TabGloss  = new Color(0.012f, 0.831f, 1.000f);  // #03D4FF üst ışık
        static readonly Color InfoFill  = new Color(0.400f, 0.331f, 1.000f);  // #6654FF — MOR, mavi değil
        static readonly Color BadgeLilac = new Color(0.678f, 0.635f, 0.996f); // #ADA2FE geri sayım
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
            // HAFTALIK LİSTE DOKUZ KİŞİ — oyuncunun kendi satırıyla birlikte
            // tam 10 sıra eder. Referansta haftalık sıralama 1'den 10'a kadar
            // gösteriliyor (bkz. SelfRankLabel); sekiz rakiple en fazla 9. sıra
            // oluşabiliyordu ve "ilk 10" hiç dolmuyordu.
            new[]
            {
                ("mira",         412),
                ("kret",         388),
                ("Bet",          351),
                ("nurhayat",     309),
                ("zzz",          274),
                ("polat",        236),
                ("dotsang",      198),
                ("selin",        176),
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

        /// <summary>Seçili sekmenin üst ışığı; <see cref="Refresh"/> açıp kapatıyor.</summary>
        readonly List<RectTransform> _tabGloss = new List<RectTransform>();
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

            // SAHNE EN ÖNCE KURULUYOR (4. tur, D11/D12).
            //
            // Kullanıcı: "Weekly / World / Country sekmelerinin bulunduğu alan
            // düz arka plan rengi kalmış — arka plan görseli oraya da
            // uzatılacak." ve "Oyuncu sıralamaları listesi arka plan görselinin
            // altında kalıyor, görünmüyor — katman sırası düzeltilecek."
            //
            // ÖLÇÜM (`sıralama.jpeg`, 946×2048, sol kenardan dikey tarama):
            //   y 280-430  #205DF3 → #276FF9   (sekmelerin ARKASI)
            //   y 440+     yeşil (#7CA519 vb.) (çim)
            // Yani sekmelerin arkasındaki mavi ayrı bir şerit değil, sahne
            // görselinin GÖKYÜZÜ; sahne başlığın hemen altından başlıyor ve
            // sekmeler onun üstünde duruyor. Bizde sahne 556'da başlıyor,
            // üstündeki alana ise elle düz mavi bir bant boyanıyordu.
            //
            // DERS (katman sırası bir KARAR, bir kaza değil): Sahne artık
            // ekranın İLK çocuğu; sekmeler, kürsüler, satırlar ve sabit "You"
            // satırı ondan sonra geliyor, yani hepsi kendiliğinden üstünde
            // çiziliyor. Eskiden sahne kürsü bandının içinde doğuyordu ve
            // "önce kim kurulduysa o altta" kuralı, listenin bir kısmının
            // görselin altında kalmasına yol açıyordu.
            screen.BuildScene(root);
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
            // DÜZ MAVİ BANT KALDIRILDI: artık sahnenin gökyüzü orada
            // (bkz. Build → BuildScene, 4. tur D11).
            // ÖLÇÜ REFERANSTAN (`sıralama.jpeg`): yuva X 0.107-0.803, yani
            // ekranın SOLUNA yaslı ve sağda "i" bilgi düğmesine yer bırakıyor.
            // Bizimki 0.075-0.925 ile neredeyse tam genişlikti; üç sekme
            // birbirinden kopuk üç düğme gibi duruyordu (17. APK bulgusu).
            var slotRow = MenuPage.Row("Tabs", root, 336f, 112f, 0.107f, 0.803f);

            // ÜÇ KATMAN — referansta seçici tek bir kutu değil, GÖMÜLÜ bir yuva:
            // dıştan açık mor bir bilezik, içinde koyu bir kuyu, kuyunun içinde
            // sekmeler. Bizde tek koyu kutu vardı ve sekmeler onun üstünde
            // yüzüyordu; "çok basit kalmış" bulgusunun yapısal kısmı bu.
            var rim = MenuPage.Capsule("Rim", slotRow, TabRim);
            UiKit.Place(rim, 0f, 0f, 1f, 1f);

            var slot = MenuPage.Capsule("Slot", slotRow, TabSlot);
            UiKit.Place(slot, 0f, 0f, 1f, 1f, padding: 7f);

            for (int i = 0; i < TabNames.Length; i++)
            {
                float x0 = 0.014f + i * 0.3287f;
                var face = MenuPage.Capsule("Tab" + i, slot.transform, TabIdle);
                UiKit.Place(face, x0, 0.10f, x0 + 0.3213f, 0.90f);
                face.raycastTarget = true;

                // Üstte toplanan ışık: seçili sekmeyi "cam düğme" yapan şey.
                // Seçili olmayanda da duruyor ama Refresh onu kapatıyor —
                // burada kurulması, sonradan yaratmaya göre daha ucuz.
                var gloss = UiKit.CreateRect("Gloss", face.transform);
                var glossImage = gloss.gameObject.AddComponent<Image>();
                glossImage.sprite = MenuSprites.FadeDown;
                glossImage.type = Image.Type.Sliced;
                glossImage.color = TabGloss;
                glossImage.raycastTarget = false;
                UiKit.Place(gloss, 0.03f, 0.46f, 0.97f, 0.97f);
                gloss.localRotation = Quaternion.Euler(0f, 0f, 180f);
                gloss.gameObject.SetActive(false);

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
                _tabGloss.Add(gloss);
            }

            // Geri sayım rozeti sekmelerin ALT kenarına biner; referansta
            // yuvanın SOL alt köşesinde duruyor.
            var badge = MenuPage.Row("Countdown", root, 424f, 62f, 0.107f, 0.375f);
            var badgeFace = MenuPage.Capsule("Face", badge, TabSlot);
            UiKit.Place(badgeFace, 0f, 0f, 1f, 1f);

            // Rozetin solunda küçük bir daire simge (referansta beyaz bir
            // işaret taşıyan açık mor disk). Rozet yazıdan ibaret olunca
            // "sekmelerden kopmuş bir etiket" gibi duruyordu.
            var badgeDot = UiKit.CreateIcon("Dot", badgeFace.transform,
                GameKit.UI.UiSprites.Circle, BadgeLilac);
            UiKit.Place(badgeDot, 0.03f, 0.13f, 0.19f, 0.87f);

            _countdown = UiKit.CreateTitle("Text", badgeFace.transform, "", 26,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(_countdown, 0.22f, 0.06f, 0.94f, 0.94f);

            // "i" bilgi düğmesi: referansta yuvanın SAĞINDA, aynı hizada.
            //
            // RENK REFERANSTAN ÖLÇÜLDÜ: dolgu #6654FF, yani MOR. Bizimki
            // #2C8BFE ile MAVİYDİ — kullanıcının "yuvarlak içindeki İ orijinal
            // oyunda mor, bizde mavi" bulgusu. Etrafında ayrıca bir bilezik
            // var; tek düz daire referansta olduğundan yassı kalıyordu.
            var info = MenuPage.Row("Info", root, 344f, 96f, 0.836f, 0.936f);
            var infoRim = UiKit.CreateIcon("Rim", info, GameKit.UI.UiSprites.Circle,
                MenuPage.Darken(InfoFill, 0.62f));
            UiKit.Place(infoRim, 0f, 0f, 1f, 1f);
            var infoFace = UiKit.CreateIcon("Face", info, GameKit.UI.UiSprites.Circle, InfoFill);
            UiKit.Place(infoFace, 0.08f, 0.08f, 0.92f, 0.92f);
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
        /// <summary>
        /// Gökyüzü + park sahnesi: BAŞLIĞIN hemen altından kürsü bandının
        /// altına kadar tek parça (4. tur, D11).
        ///
        /// Maske sahneyi saran çocuğa konuyor, bandın kendisine değil:
        /// `CreateCover` görseli oranını koruyarak ebeveyni ÖRTECEK kadar
        /// büyütür (fazlası taşar, bu onun tanımı) ve maskesiz bir bant
        /// sekmelerin üstüne taşardı.
        /// </summary>
        void BuildScene(Transform root)
        {
            const float top = MenuPage.HeaderH;
            const float bottom = 556f + 486f;

            var band = MenuPage.Row("Scene", root, top, bottom - top);
            var sky = band.gameObject.AddComponent<Image>();
            // Gökyüzü ölçüldü: sekme bandının hizasında #205DF3.
            sky.color = new Color(0.125f, 0.365f, 0.953f);
            sky.raycastTarget = false;

            var scene = UiSkin.Get(Art.BoardScene);
            if (scene == null) return;

            var clip = UiKit.CreateRect("SceneClip", band);
            UiKit.Place(clip, 0f, 0f, 1f, 1f);
            clip.gameObject.AddComponent<RectMask2D>();

            var cover = UiKit.CreateCover("Scene", clip, scene,
                new Color(0.125f, 0.365f, 0.953f));
            cover.raycastTarget = false;
        }

        void BuildPodium(Transform root)
        {
            var band = MenuPage.Row("Podium", root, 556f, 486f);

            // Gökyüzü ve park sahnesi artık BU BANDIN İÇİNDE DEĞİL:
            // `BuildScene` onları başlığın hemen altından başlatıp bu bandın
            // altına kadar tek parça çiziyor (4. tur, D11).
            if (UiSkin.Get(Art.BoardScene) != null)
            {
                BuildPodiumColumns(band);
                return;
            }

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
            // ÇALILIK ŞERİDİ: ağaçların dibinde, çimin üstünde. Referansta
            // kürsülerin arkası boş çim değil — budanmış bir çit var ve sahneye
            // derinlik veren şey o. Tek renk çim, kürsüleri yine "arayüz
            // öğesi" gibi bırakıyordu.
            foreach (float cx in new[] { 0.10f, 0.30f, 0.50f, 0.70f, 0.90f })
            {
                var hedge = UiKit.CreateIcon("Hedge", band, MenuSprites.Foliage,
                    new Color(0.165f, 0.451f, 0.157f));
                hedge.raycastTarget = false;
                UiKit.Place(hedge, cx - 0.16f, 0.20f, cx + 0.16f, 0.40f);
            }

            // AĞAÇLAR: taç artık DAİRE değil, tırtıklı yaprak silueti
            // (`MenuSprites.Foliage`). İki katman — koyu alt taç, açık üst taç —
            // hacim veriyor; tek düz yeşil top "ağaç" okunmuyordu.
            foreach (float cx in new[] { 0.045f, 0.955f })
            {
                const float w = 0.125f, h = 0.50f;

                var trunk = UiKit.CreatePanel("Trunk", band, new Color(0.451f, 0.278f, 0.125f));
                trunk.raycastTarget = false;
                UiKit.Place(trunk, cx - w * 0.15f, 0.16f, cx + w * 0.15f, 0.42f);

                var crownLow = UiKit.CreateIcon("CrownLow", band, MenuSprites.Foliage,
                    new Color(0.125f, 0.396f, 0.118f));
                crownLow.raycastTarget = false;
                UiKit.Place(crownLow, cx - w, 0.32f, cx + w, 0.32f + h * 0.72f);

                var crownTop = UiKit.CreateIcon("CrownTop", band, MenuSprites.Foliage,
                    new Color(0.267f, 0.639f, 0.204f));
                crownTop.raycastTarget = false;
                UiKit.Place(crownTop, cx - w * 0.80f, 0.40f, cx + w * 0.80f, 0.40f + h * 0.68f);
            }

            BuildPodiumColumns(band);
        }

        /// <summary>
        /// Üç kaide, avatarları ve ad levhalarıyla. Zeminden ayrıldı: park
        /// sahnesi ister görsel ister çizim olsun, kaideler aynı.
        /// </summary>
        void BuildPodiumColumns(RectTransform band)
        {
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

                // GERÇEK KAİDE GÖRSELİ (12. madde): oluklu krem gövde, mor
                // minder ve sıraya göre altın/gümüş/bronz kuşak tek görselde.
                // Prosedürel dört katman (kenar → gövde → kapak kenarı →
                // kapak) yalnız görsel yokken kuruluyor.
                var art = UiSkin.Get(Art.Podium(place));
                if (art != null)
                {
                    var stand = UiKit.CreateIcon("Stand", pillar, art);
                    UiKit.Place(stand, 0f, 0f, 1f, 1f);
                    stand.preserveAspect = true;
                    stand.raycastTarget = false;
                }
                else
                {
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
                }

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

                // AD LEVHASI: gölge + kenar + yüzey.
                //
                // DERS (krem üstüne krem sınır vermez): Levha tek düz krem
                // dikdörtgendi ve kürsünün gövdesi de kremdi — ikisi ekranda
                // tek kütle olarak okunuyordu. Kullanıcı "ismin kapladığı arka
                // plan detaysız ve gölgesiz gözüküyor" derken bunu gördü.
                // Altına düşen koyu bir kopya levhayı gövdeden koparıyor.
                var plateShadow = MenuPage.Capsule("NameShadow", pillar,
                    new Color(0.42f, 0.33f, 0.26f, 0.55f));
                UiKit.Place(plateShadow, 0.02f, 0.44f, 0.98f, 0.70f);

                var plateRim = MenuPage.Capsule("NameRim", pillar,
                    new Color(0.769f, 0.686f, 0.573f));
                UiKit.Place(plateRim, 0.02f, 0.46f, 0.98f, 0.72f);

                var plate = MenuPage.Capsule("Name", pillar, new Color(0.949f, 0.902f, 0.824f));
                UiKit.Place(plate, 0.02f, 0.46f, 0.98f, 0.72f, padding: 6f);
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
                // MADALYA — dişli çelenk + koyu bilezik + yüzey + parlaklık.
                //
                // DERS (iki iç içe daire madalya DEĞİLDİR): Burada iki düz
                // daire vardı; altın/gümüş/bronz rengi doğruydu ama ekranda
                // "renkli bir nokta" olarak okunuyordu ve kullanıcı
                // "madalyamız yok" dedi. Bir madalyayı madalya yapan şey
                // kenarındaki DÜZENLİ ÇIKINTILAR (taç) ve yüzeyindeki ışık.
                // `MenuSprites.Sunburst` o tacı çiziyor.
                var disc = UiKit.CreateRect("Medal", pillar);
                UiKit.Place(disc, 0.18f, 0.06f, 0.82f, 0.46f);

                var wreath = UiKit.CreateIcon("Wreath", disc, MenuSprites.Sunburst,
                    MenuPage.Darken(medal[place], 0.80f));
                UiKit.Place(wreath, 0f, 0f, 1f, 1f);

                var rimDisc = UiKit.CreateIcon("Rim", disc, GameKit.UI.UiSprites.Circle,
                    MenuPage.Darken(medal[place], 0.62f));
                UiKit.Place(rimDisc, 0.11f, 0.11f, 0.89f, 0.89f);

                var discImage = UiKit.CreateIcon("Face", disc, GameKit.UI.UiSprites.Circle,
                    medal[place]);
                UiKit.Place(discImage, 0.17f, 0.17f, 0.83f, 0.83f);

                // Sol üstte toplanan ışık: metali "parlak" yapan tek şey.
                var shine = UiKit.CreateIcon("Shine", disc, GameKit.UI.UiSprites.Circle,
                    new Color(1f, 1f, 1f, 0.38f));
                UiKit.Place(shine, 0.22f, 0.46f, 0.50f, 0.74f);

                var number = UiKit.CreateTitle("No", disc, place.ToString(), 40,
                    new Color(0.32f, 0.20f, 0.04f), new Color(1f, 0.94f, 0.72f));
                UiKit.Place(number, 0.17f, 0.17f, 0.83f, 0.83f);
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
            //
            // DÜZELTME (2026-08-18): Sabit "You" satırı 1552'den 1470'e
            // ALINDI (alt çubuğun seçili kartı 250 birime kadar yükseliyor ve
            // satırı örtüyordu). Görüntü alanı eskisi gibi 1528'de bitince bu
            // sefer liste sabit satırın ALTINDAN görünmeye başladı — üçüncü
            // sıra yeşil satırın arkasında yarı yarıya kaldı.
            // DERS: sabit bir öğeyi yukarı almak, üstündeki kaydırma alanını
            // AYNI KADAR kısaltmayı gerektirir; ikisi tek bir bütçeyi paylaşıyor.
            // KAYDIRMA, SAHNENİN İÇİNDE DEĞİL ALTINDA BAŞLAR (7. tur, O54).
            //
            // Kullanıcı: "Sıralama kaydırılırken üst kısım maskelenmiş gibi
            // kötü görünüyor… Kürsünün hemen altında düz, çerçevesiz mor bir
            // çizgi olacak. Scroll edilen alanın üstünde çerçeve olacak."
            //
            // SEBEP: Görüntü alanı 1006'da başlıyordu, park sahnesi ise
            // 1042'de bitiyor — yani listenin ilk 36 birimi ÇİMİN ÜSTÜNDE
            // duruyordu ve kaydırılan satırlar tam orada, çimenli bir
            // görselin ortasında kesiliyordu. Maske çizgisi hiçbir kenara
            // denk gelmediği için "bir şey yanlışlıkla kırpılmış" gibi
            // okunuyordu.
            //
            // DERS (kesim, BİR KENARDA olmalı): Bir maske görünmez değildir;
            // içeriği nerede bitirdiğini gösterir. Kesim çizgisi tasarımda
            // zaten var olan bir sınıra (burada sahnenin dibine) oturursa göz
            // onu kesim değil kenar olarak okur. Kesim serbest bir yerdeyse
            // hangi kenarlığı koyarsan koy rahatsız etmeye devam eder.
            const float sceneBottom = 556f + 486f;      // BuildScene ile aynı
            const float railH = 26f;                    // düz mor çizgi
            const float frameH = 7f;                    // kaydırma alanının çerçevesi
            float viewportTop = sceneBottom + railH + frameH;

            const float viewportBottom = 1446f;
            var viewport = MenuPage.Row("RowsViewport", root, viewportTop,
                viewportBottom - viewportTop, 0f, 1f);
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

            // --- Kürsü ile liste ARASI ---
            //
            // İki parça, ikisi de görüntü alanından SONRA kuruluyor (kardeş
            // sırası = çizim sırası): kaydırılan satırlar bunların ardına
            // girip kayboluyor.
            //
            // 1) DÜZ, ÇERÇEVESİZ MOR ÇİZGİ — kürsünün hemen altında. Ekranın
            //    gövde rengiyle aynı: sahne biter bitmez ekranın kendi zemini
            //    başlıyor, arada başka bir malzeme yok.
            var rail = MenuPage.Row("ListRail", root, sceneBottom, railH, 0f, 1f);
            var railImage = rail.gameObject.AddComponent<Image>();
            railImage.color = MenuPage.Body;
            railImage.raycastTarget = false;

            // 2) ÇERÇEVE — kaydırılan alanın üst kenarı. Satır genişliğinde,
            //    satırların yüzü kadar açık mor: "liste burada başlıyor"
            //    diyen tek çizgi. Kaydırma bu çizginin ALTINDA kesildiği için
            //    kesim artık bir kenarlığa denk geliyor.
            var frame = MenuPage.Row("ListFrame", root, sceneBottom + railH, frameH,
                0.035f, 0.965f);
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.color = MenuPage.Panel;
            frameImage.raycastTarget = false;
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

        /// <summary>
        /// Madalya DİSKİNİN yüzü — referanstan ölçüldü (`sıralama.jpeg` liste
        /// satırları): 1. (255,195,9), 2. (171,197,234), 3. (228,116,34).
        /// Arkasındaki yuva rengi (<see cref="MedalColors"/>) bundan biraz
        /// daha soluk; ikisi aynı olsa madalya yuvanın içinde kaybolurdu.
        /// </summary>
        static readonly Color[] MedalFaces =
        {
            default,
            new Color(1.000f, 0.765f, 0.035f),
            new Color(0.671f, 0.773f, 0.918f),
            new Color(0.894f, 0.455f, 0.133f)
        };

        /// <summary>
        /// Avatarların arkasındaki zemin renkleri.
        ///
        /// Kullanıcı "avatarların arka planı hiç yok, hiç değilse farklı renkte
        /// kullanalım" dedi. Renkler sırayla dağıtılıyor — rastgele olsaydı
        /// aynı oyuncu her açılışta başka renk alırdı ve liste tanınmaz olurdu.
        /// Doygunlukları kasten orta: portrenin kendisiyle yarışmamalılar.
        /// </summary>
        static readonly Color[] AvatarWells =
        {
            new Color(0.988f, 0.749f, 0.290f),   // amber
            new Color(0.408f, 0.729f, 0.973f),   // gök mavisi
            new Color(0.945f, 0.478f, 0.502f),   // mercan
            new Color(0.545f, 0.780f, 0.365f),   // fıstık yeşili
            new Color(0.706f, 0.573f, 0.949f),   // leylak
            new Color(0.976f, 0.612f, 0.298f),   // turuncu
            new Color(0.376f, 0.784f, 0.741f),   // camgöbeği
            new Color(0.925f, 0.573f, 0.788f),   // pembe
            new Color(0.851f, 0.788f, 0.412f)    // hardal
        };

        void BuildSelfRow(Transform root)
        {
            // ALT KENAR ÇERÇEVESİ: satırların bittiği yerle sekme çubuğu
            // arasındaki şerit çıplak koyu laciverttı ve ekran orada
            // "kesilmiş" gibi bitiyordu (kullanıcı: "en altta dış kısımları
            // boş, orijinalde çerçeve gibi gözüküyor"). Referansta liste
            // alanının altında bir kenarlık var; onu iki şeritle kuruyoruz.
            var footer = MenuPage.Row("Footer", root, 1650f, 290f, 0f, 1f);
            var footerFill = footer.gameObject.AddComponent<Image>();
            footerFill.color = new Color(0.098f, 0.086f, 0.286f);
            footerFill.raycastTarget = false;

            var footerLip = UiKit.CreateRect("Lip", footer);
            var footerLipImage = footerLip.gameObject.AddComponent<Image>();
            footerLipImage.color = new Color(0.263f, 0.220f, 0.596f);
            footerLipImage.raycastTarget = false;
            footerLip.anchorMin = new Vector2(0f, 1f);
            footerLip.anchorMax = new Vector2(1f, 1f);
            footerLip.pivot = new Vector2(0.5f, 1f);
            footerLip.sizeDelta = new Vector2(0f, 8f);
            footerLip.anchoredPosition = Vector2.zero;

            // Kaydırılan listenin ALTINDA, sekme çubuğunun ÜSTÜNDE sabit.
            //
            // YÜKSEKLİK DÜZELTİLDİ (2026-08-18): satır 1552'de duruyordu ve
            // dünya koordinatında 210-368 arasına düşüyordu; alt sekme
            // çubuğunun SEÇİLİ KARTI ~250'ye kadar yükseliyor, yani kartın
            // altında kalıyordu. Ölçüldü, 1470'e alındı (292-450).
            var row = MenuPage.Row("Self", root, 1470f, 158f, 0.035f, 0.965f);
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

            // ROZET ÜÇ KATMAN: koyu bilezik → yüzey → üst ışık.
            //
            // DERS (düz renk bir MADALYA değildir): Rozet tek düz dikdörtgendi;
            // altın rengi bile verilse ekranda "sarı bir kutu" olarak okunuyordu
            // — kullanıcının "1., 2., 3. sıralaması da kötü gözüküyor" bulgusu.
            // Metalin okunması için üç şey gerekiyor: koyu bir kenar (hacim),
            // yüzey rengi ve üstte toplanan bir ışık (parlaklık).
            var rankRim = MenuPage.Capsule("RankRim", face.transform,
                MenuPage.Darken(slotColor, 0.62f));
            UiKit.Place(rankRim, -0.005f, 0.04f, 0.155f, 0.96f);

            var rankSlot = MenuPage.Capsule("RankSlot", rankRim.transform, slotColor);
            UiKit.Place(rankSlot, 0f, 0f, 1f, 1f, padding: 6f);

            var rankGloss = UiKit.CreateRect("Gloss", rankSlot.transform);
            var rankGlossImage = rankGloss.gameObject.AddComponent<Image>();
            rankGlossImage.sprite = MenuSprites.FadeDown;
            rankGlossImage.type = Image.Type.Sliced;
            rankGlossImage.color = new Color(1f, 1f, 1f, medalled ? 0.42f : 0.18f);
            rankGlossImage.raycastTarget = false;
            UiKit.Place(rankGloss, 0.06f, 0.50f, 0.94f, 0.94f);
            rankGloss.localRotation = Quaternion.Euler(0f, 0f, 180f);

            // Madalyalı rozette yazı KOYU: altın/gümüş üstünde beyaz okunmuyor.
            var rankInk = medalled ? new Color(0.24f, 0.14f, 0.03f) : MenuPage.Ink;
            var rankEdge = medalled ? new Color(1f, 0.96f, 0.86f) : MenuPage.InkDark;

            // İLK ÜÇTE SAYI YUVANIN İÇİNDE DEĞİL, MADALYANIN İÇİNDE.
            //
            // Referans (`sıralama.jpeg` liste satırları) büyütülünce çıktı:
            // ilk üç sırada yuvanın üstünde YUVARLAK, tırtıklı kenarlı bir
            // madalya duruyor, sayı onun ortasında ve altından iki KIRMIZI
            // kurdele ucu sarkıyor. Ölçülen yüz renkleri: 1. (255,195,9),
            // 2. (171,197,234), 3. (228,116,34).
            //
            // DERS (renk vermek biçim vermez): Rozet zaten altın/gümüş/bronz
            // renkteydi ve üç katmanla metal hissi de veriliyordu — ama
            // biçimi hâlâ YUVARLAK KÖŞELİ KARE'ydi. Renk "bu birinci" der,
            // biçim "bu bir madalya" der; ikincisi eksikti.
            var textHost = rankSlot.transform;
            if (medalled)
            {
                var medal = UiKit.CreateRect("Medal", rankSlot.transform);
                UiKit.Place(medal, 0.06f, 0.10f, 0.94f, 0.90f);

                // KURDELE İKİ AYRI UÇ, tek şerit değil.
                //
                // İlk denemede iki ucu da aynı yatay aralığa (0.34-0.66)
                // koyup ±26° döndürdüm; üst üste bindikleri için ekranda iki
                // kuyruk değil tek bir kırmızı leke çıktı. Referansta uçlar
                // diskin ALTINDAN İKİ YANA açılıyor, o yüzden yatayda da
                // ayrılmaları gerekiyor.
                foreach (var (x0, x1, tilt) in new[]
                         { (0.16f, 0.44f, -20f), (0.56f, 0.84f, 20f) })
                {
                    var tail = UiKit.CreateRoundedPanel("Ribbon", medal,
                        new Color(0.851f, 0.184f, 0.184f));
                    UiKit.SetSliceScale(tail, 0.9f);
                    tail.raycastTarget = false;
                    UiKit.Place(tail, x0, -0.30f, x1, 0.26f);
                    tail.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
                }

                // Tırtıklı kenar: madalya çelengi (`Sunburst`) koyu metalde.
                var wreath = UiKit.CreateIcon("Wreath", medal, MenuSprites.Sunburst,
                    MenuPage.Darken(MedalFaces[place], 0.70f));
                wreath.raycastTarget = false;
                UiKit.Place(wreath, 0f, 0f, 1f, 1f);

                // Disk ÇELENGİ NEREDEYSE KAPATIYOR: dişler yalnız kenardan
                // görünsün. Küçük disk bıraktığımda madalya değil GÜNEŞ gibi
                // okunuyordu — referansta kenar tırtıkları ince bir detay.
                var faceDisc = UiKit.CreateIcon("Disc", medal, GameKit.UI.UiSprites.Circle,
                    MedalFaces[place]);
                faceDisc.raycastTarget = false;
                UiKit.Place(faceDisc, 0.08f, 0.08f, 0.92f, 0.92f);

                textHost = faceDisc.transform;
            }

            var rankText = UiKit.CreateTitle("Rank", textHost, rank, 36,
                rankInk, rankEdge);
            UiKit.Place(rankText, 0.05f, 0.06f, 0.95f, 0.94f);

            // Avatar KARE çerçevede (referansta da öyle); çerçevesiz portre
            // satırın içinde yüzüyor gibi duruyordu.
            // AVATAR: ÇERÇEVE + KENDİ ZEMİN RENGİ.
            //
            // DERS (aynı zemin, farklı yüz = tek kişi gibi okunur): Sekiz
            // satırın avatar zemini de aynı açık mordu ve çerçevesi yoktu;
            // portreler satırın içinde yüzüyordu. Referansta her avatarın
            // süslü bir çerçevesi ve çerçevenin içinde KENDİ zemin rengi var
            // — göz listeyi okumadan önce yüzleri birbirinden ayırıyor
            // (kullanıcı bulguları 13 ve 17).
            //
            // Renk sıradan türetiliyor, rastgele değil: aynı oyuncu her
            // açılışta aynı rengi alsın.
            var avatar = UiKit.CreateRect("Avatar", face.transform);
            UiKit.Place(avatar, 0.175f, 0.06f, 0.305f, 0.94f);

            Color wellColor = avatarIndex < 0
                ? new Color(0.298f, 0.784f, 0.667f)      // oyuncu: turkuaz, listede tek
                : AvatarWells[avatarIndex % AvatarWells.Length];

            // Dış çerçeve (koyu) → iç çerçeve (açık) → zemin → portre.
            // Üç katman, referanstaki oymalı çerçevenin ucuz ama okunur hâli.
            var frameRim = UiKit.CreateRoundedPanel("FrameRim", avatar,
                MenuPage.Darken(wellColor, 0.52f));
            UiKit.SetSliceScale(frameRim, 0.55f);
            UiKit.Place(frameRim, 0f, 0f, 1f, 1f);

            var frame = UiKit.CreateRoundedPanel("Frame", avatar,
                new Color(0.859f, 0.847f, 0.988f));
            UiKit.SetSliceScale(frame, 0.60f);
            UiKit.Place(frame, 0f, 0f, 1f, 1f, padding: 5f);

            var well = UiKit.CreateRoundedPanel("Well", frame.transform, wellColor);
            UiKit.SetSliceScale(well, 0.75f);
            UiKit.Place(well, 0.09f, 0.09f, 0.91f, 0.91f);

            // avatarIndex < 0 => OYUNCUNUN kendi satırı; kendi avatarını taşır.
            var portrait = UiKit.CreateIcon("Portrait", well.transform,
                UiSkin.Get(avatarIndex < 0 ? Art.Avatar : Art.Rival(avatarIndex)));
            UiKit.Place(portrait, 0.04f, 0.04f, 0.96f, 0.96f);

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

        /// <summary>
        /// Oyuncunun kendi satırındaki sıra etiketi.
        ///
        /// REFERANS KURALI (kullanıcı bulgusu 19): Haftalık listede sıralama
        /// 1'den 10'a kadar GERÇEK bir sayı; Dünya ve Ülke listelerinde
        /// "1000+" yazıyor. Bizde üçünde de "1000+" sabitti.
        ///
        /// Neden mantıklı: haftalık liste küçük bir havuz (hafta yeni başladı,
        /// az kişi oynadı) — oyuncu gerçekten onuncu olabilir ve bunu görmek
        /// oynatan şey. Dünya listesi milyonlarca kişilik; oradaki gerçek sıra
        /// hem hesaplanamaz hem de motive etmez, o yüzden eşiğin altı tek bir
        /// kovaya toplanıyor.
        ///
        /// Havuz sahte olduğu için sıra da rakiplerin puanlarından hesaplanıyor;
        /// sabit bir sayı yazmak, oyuncu ilerledikçe yalan söylerdi.
        /// </summary>
        string SelfRankLabel(int selfScore)
        {
            if (_activeTab != WeeklyTab) return "1000+";

            int rank = 1;
            foreach (var rival in Boards[WeeklyTab])
                if (rival.score > selfScore) rank++;

            return rank <= WeeklyVisibleRanks ? rank.ToString() : WeeklyVisibleRanks + "+";
        }

        /// <summary>Haftalık sekmenin dizideki yeri (<see cref="TabNames"/> ile aynı sıra).</summary>
        const int WeeklyTab = 0;

        /// <summary>Referansta haftalık sıralama 1..10 gösteriliyor.</summary>
        const int WeeklyVisibleRanks = 10;

        public void Refresh()
        {
            if (!_built) return;

            foreach (var (face, label, index) in _tabs)
            {
                bool on = index == _activeTab;
                face.color = on ? TabActive : TabIdle;
                label.color = on ? MenuPage.Ink : new Color(1f, 1f, 1f, 0.78f);

                // Üst ışık YALNIZ seçilide: referansta seçili sekme camdan
                // bir düğme gibi parlıyor, diğer ikisi mat kalıyor.
                if (index < _tabGloss.Count && _tabGloss[index] != null)
                    _tabGloss[index].gameObject.SetActive(on);
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
            int selfScore = Score(level, MetaServices.Progress);
            _selfScore.text = selfScore.ToString();
            _selfRank.text = SelfRankLabel(selfScore);
        }
    }
}
