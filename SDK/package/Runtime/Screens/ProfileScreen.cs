using GameKit.Meta;
using GameKit.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace GameKit.Screens
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
            // İKON TEKRARI KIRILDI (9. tur). Sekiz sayaçta beş ikon vardı:
            // Trophy×2, Ufo×2, Rocket×2, Globe, Star.
            //
            // Tekrarların çoğu ADIN KENDİSİNDEN geliyor ve doğru: "Rocket
            // Race" roketle, "Max Ufo Climb" UFO ile anlatılır. Gereksiz olan
            // tek tekrar Trophy'ydi — "First Try Wins" ile "Weekly Cup Wins"
            // aynı kupayı kullanıyordu, oysa yalnız ikincisi bir kupa.
            //
            // DENENDİ VE GERİ ALINDI: `Art.Check` (yeşil onay işareti).
            // Anlamı doğruydu — "ilk denemede" bir ödül değil bir doğruluk
            // ölçütü — ama ekranda sırıttı: bu ızgaradaki diğer yedi ikon
            // parlak, hacimli oyun ikonu (UFO, roket, yıldız, küre, kupa),
            // `check_green` ise düz bir vektör tik. Yakalamada tek başına
            // "başka bir oyundan düşmüş" gibi duruyor.
            //
            // DERS (çeşitlilik için ne anlamı ne ÜSLUBU boz): "Referansta 8
            // farklı ikon var" diye sekiz ayrı görsel dağıtmak, sandığı roket
            // yarışına iliştirmek demekti. Kalan tekrarların çoğu zaten adın
            // kendisinden geliyor ve doğru: "Rocket Race" roketle, "Max Ufo
            // Climb" UFO ile anlatılır. Anlamlı tekrar, üslubu bozan
            // çeşitlilikten iyidir.
            //
            // ---- 11. TUR: DAYANAK DÜŞTÜ, KARAR DEĞİŞTİ ----
            //
            // Yukarıdaki muhakeme kendi içinde doğruydu ama bir VARSAYIMA
            // dayanıyordu: "elimizdeki simgeler bunlar". Üç tekrarın da
            // sebebi eksik varlıktı — uygun bir madalya, yarış bayrağı ve lig
            // amblemi yoktu, `check_green` de ızgaraya uymuyordu.
            //
            // O varsayım artık geçerli değil: dördü `_Reference/
            // draw_stat_icons.py` ile, menü simgeleriyle AYNI palet ve aynı
            // bitirişte (kalın lacivert kontur, çok duraklı gradyan, tepe
            // ışığı, temas gölgesi) çizildi. Yani "üslubu bozmadan
            // çeşitlendirme" seçeneği ilk kez masada.
            //
            // Anlamı adından gelen dört tekrar KORUNDU (roket, UFO, kupa,
            // yıldız kendi satırlarında kalıyor); değişen yalnız eksik
            // varlıktan doğan dördü.
            //
            // DERS (bir kararı yeniden aç, ama SEBEBİ değişince): Bu tabloyu
            // "referansta 8 farklı var" diye değiştirmek 9. turda yanlıştı ve
            // bugün de yanlış olurdu. Değişimi haklı kılan şey referans
            // değil, kısıtın kalkmış olması. Eski yorumu silmedim: hangi
            // gerekçenin ne zaman düştüğü, kararın kendisinden daha değerli.
            ("First Try Wins",      Art.Medal),    // kurdeleli "1" madalyası
            ("Sky Jump Wins",       Art.Saucer),   // pembe tabak (mavi UFO'dan ayrı)
            ("Streak Race Wins",    Art.Flag),     // damalı yarış bayrağı
            ("Block League Wins",   Art.League),   // ışınlı rozet + taş
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
            // İÇ PANO: çerçevenin deliğinden görünen açık zemin (ana ekranla
            // aynı reçete). Portremiz saydam zeminli bir karakter olduğu için
            // zemini biz veriyoruz.
            var back = UiKit.CreateRoundedPanel("Back", frame, new Color(0.722f, 0.702f, 1f));
            UiKit.Place(back, 0.15f, 0.15f, 0.85f, 0.85f);
            UiKit.SetSliceScale(back, UiKit.SliceScaleFor(20f));

            // Portre çerçevenin İÇİNDE kalmalı: 0.13 payında karakterin başı
            // çerçevenin üst çubuğunun üstüne taşıyordu.
            var portrait = UiKit.CreateIcon("Portrait", frame, UiSkin.Get(Art.Avatar));
            UiKit.Place(portrait, 0.17f, 0.17f, 0.83f, 0.83f);

            // ÇERÇEVE EN SON (15. tur): referanstan kesilen yeni çerçevenin
            // içi boş, o yüzden portrenin ÜSTÜNE çizilmesi gerekiyor —
            // eskisi altta kalıyor ve portre onun kenarına biniyordu.
            var frameSprite = UiSkin.Get(Art.AvatarFrame);
            if (frameSprite != null)
            {
                var frameImage = UiKit.CreateIcon("Frame", frame, frameSprite);
                // Düz germe (ana ekranla aynı gerekçe: 9-dilim payı kutudan
                // büyük kalıyor ve çerçeve döşenmiş gibi görünüyor).
                frameImage.type = Image.Type.Simple;
                frameImage.preserveAspect = false;
                frameImage.raycastTarget = false;
                UiKit.Place(frameImage, 0f, 0f, 1f, 1f);
            }

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

            // ROZET ÜÇ KATMAN + GÖLGE (7. tur, Q57). Kullanıcı: "Avatar isim
            // değiştirme ikonu hâlâ düzgün değil; gölgeli ve daha kaliteli
            // hale getirilecek."
            //
            // Eskiden TEK düz yeşil daireydi. Bu ekrandaki her yüzeyin (kart,
            // ad levhası, istatistik kutuları) bir koyu kenarı var; rozet
            // onlarla aynı dili konuşmadığı için "sonradan yapıştırılmış"
            // duruyordu. Sıra: gölge → koyu kenar → yüz → üst ışık.
            var pencilShadow = UiKit.CreateIcon("Shadow", pencil,
                GameKit.UI.UiSprites.Circle, new Color(0f, 0f, 0f, 0.34f));
            UiKit.Place(pencilShadow, 0.02f, -0.075f, 1.02f, 0.925f);

            var pencilRim = UiKit.CreateIcon("Rim", pencil,
                GameKit.UI.UiSprites.Circle, new Color(0.055f, 0.318f, 0.020f));
            UiKit.Place(pencilRim, 0f, 0f, 1f, 1f);

            var pencilFace = UiKit.CreateIcon("Face", pencil,
                GameKit.UI.UiSprites.Circle, MenuPage.Green);
            UiKit.Place(pencilFace, 0.085f, 0.085f, 0.915f, 0.915f);

            // Üstte toplanan ışık: rozeti düz bir disk olmaktan çıkaran şey.
            // Keskin kenarlı bir daire ikinci bir disk gibi okunuyordu;
            // `Radial` kenarına doğru sönüyor ve ışık gibi duruyor.
            var pencilSheen = UiKit.CreateIcon("Sheen", pencil,
                GameKit.UI.UiSprites.Radial, new Color(1f, 1f, 1f, 0.30f));
            UiKit.Place(pencilSheen, 0.16f, 0.42f, 0.84f, 0.94f);

            UiKit.MakeClickable(pencil.gameObject, pencilRim, screen.OpenNamePanel);
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
            // ÜÇ KATMANLI KALEM: koyu kontur, beyaz gövde, GRAFİT uç.
            //
            // DERS (aynı şekil, iki renk = tanınır simge): Tek renk beyaz
            // kalem bu boyutta (80 birimlik rozet, ~50 birimlik simge) bir
            // TİK gibi okunuyordu — kullanıcının "hâlâ düzgün değil"
            // dediği şey. Ucu ayrı bir maskeyle koyu boyamak şekli tek
            // bakışta kalem yapıyor; kontur ise onu yeşil zeminden ayırıyor.
            const float markPad = 0.155f;
            var pencilEdge = UiKit.CreateIcon("MarkEdge", pencil,
                GameKit.UI.UiSprites.Pencil, new Color(0.043f, 0.243f, 0.016f, 0.85f));
            UiKit.Place(pencilEdge, markPad - 0.035f, markPad - 0.055f,
                                    1f - markPad + 0.035f, 1f - markPad - 0.020f);
            pencilEdge.preserveAspect = true;
            pencilEdge.raycastTarget = false;

            var pencilMark = UiKit.CreateIcon("Mark", pencil,
                GameKit.UI.UiSprites.Pencil, MenuPage.Ink);
            UiKit.Place(pencilMark, markPad, markPad, 1f - markPad, 1f - markPad);
            pencilMark.preserveAspect = true;
            pencilMark.raycastTarget = false;

            // Grafit uç — gövdeyle AYNI kutuda: iki maske aynı koordinat
            // sisteminden üretildiği için uç kendiliğinden yerine oturuyor.
            var pencilLead = UiKit.CreateIcon("Lead", pencil,
                GameKit.UI.UiSprites.PencilTip, new Color(0.216f, 0.145f, 0.055f));
            UiKit.Place(pencilLead, markPad, markPad, 1f - markPad, 1f - markPad);
            pencilLead.preserveAspect = true;
            pencilLead.raycastTarget = false;

            // Ad kapsülü avatarın ALTINDA ve ondan geniş (referansta X
            // 0.119-0.590 ekran, avatar 0.211-0.492): levha avatarın iki
            // yanından da taşıyor, yani ikisi tek bir kimlik bloğu okunuyor.
            var namePlate = MenuPage.Capsule("NamePlate", card, new Color(0.318f, 0.290f, 0.694f));
            UiKit.Place(namePlate, 0.069f, 0.089f, 0.602f, 0.380f);
            screen._name = UiKit.CreateTitle("Name", namePlate.transform, "", 48,
                MenuPage.Ink, MenuPage.InkDark);
            UiKit.Place(screen._name, 0.05f, 0.06f, 0.95f, 0.94f);

            // AYRAÇ DAHA KOYU (7. tur, Q58). Kullanıcı: "İkonun yanındaki
            // seviyenin dikey ayraç çizgisi daha koyu yapılacak."
            //
            // Beyazın %30'u, kartın açık mor yüzeyinde neredeyse kayboluyordu:
            // ölçüldüğünde yüzeyle arasındaki parlaklık farkı %8'di. Ayraç
            // KOYU tarafa geçti (kartın kendi koyu tonuna doğru %55) ve bir
            // tık kalınlaştı — bir çizgi, ayırdığı iki şeyden farklı olmalı.
            var divider = UiKit.CreatePanel("Divider", card,
                new Color(0.216f, 0.161f, 0.478f, 0.85f));
            UiKit.Place(divider, 0.661f, 0.14f, 0.671f, 0.86f);

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
            // KUTUCUK ÖLÇÜLERİ YENİDEN ALINDI (2026-08-22, `profil.jpeg` 946×2048).
            // Sol sütundan dikey tarama, iki tarafa AYNI kod:
            //     referans kutucuk yüksekliği genişliğin %8.4'ü, dikey aralık %10.4
            //     bizim    %14.7 / %23.6   -> yaklaşık %75 FAZLA UZUN
            // Aynı alana referansta 8 kutucuk sığarken bize 4 sığıyordu.
            // 161 birim ekranda 159 px çiziliyordu (oran ~0.99):
            //     hedef yükseklik 0.084×1080 = 91 px  -> boxH 92
            //     hedef aralık    0.104×1080 = 112 px -> gapY 112-92 = 20
            // İNCE AYAR: kutucuk zemin RENGİ eşleştirilerek yeniden ölçüldü
            // (eşik/parlaklık yöntemleri yazı satırlarını kutucuk sanıyordu;
            // referans zemin #1E1652, ekran zemini #302488).
            //     referans yükseklik %8.2, aralık %9.9
            //     boxH 92 iken bizim %7.6 / %10.4
            // TÜRETME HATASI DÜZELTİLDİ (9. tur).
            //
            // Burada "0.082×1080 = 89 px çizim -> boxH 99 (oran ~0.90)"
            // yazıyordu. O 0.90'lık oran uydurma: aynı notun birkaç satır
            // yukarısı "161 birim ekranda 159 px" diyor, yani oran 0.99.
            //
            // ÖLÇÜM (kendi yakalamamız, kutucuk ZEMİN RENGİ eşleştirilerek —
            // koyu-koşu yöntemi kutuları etiketten ikiye bölüyordu):
            //     boxH 99 iken kutucuk ekranda 99 piksel, oran 1.00
            //     adım 107 piksel = genişliğin %9,91'i  (hedef %9,9 ✔)
            //     kutucuk %9,17                          (hedef %8,2 ✗)
            // Adım zaten tutuyordu; fazlalık kutucuğun kendisindeydi.
            //     hedef 0,082×1080 = 89  ->  boxH 89
            //     adım 107 sabit         ->  gapY 107-89 = 18
            //
            // DERS (ölçekleme oranını VARSAYMA, ölç): "birim → piksel" oranı
            // bu tuvalde 1.00; iki ayrı turda 0,99 ve 0,90 diye tahmin edildi
            // ve ikincisi kutucuğu %11 şişirdi.
            //
            // GERÇEK REFERANS BULUNDU — YUKARIDAKİ HEDEFLER DE YANLIŞMIŞ.
            //
            // Yukarıdaki bütün hesaplar artık diskte olmayan bir kaynaktan
            // (`profil.jpeg`) alınmış "%8,2 / %9,9" hedeflerine dayanıyordu.
            // `_Reference/frames/` içindeki 52 kare tek sayfada taranınca
            // İKİ profil karesi çıktı: **m_020 ve m_051**. İkisi de aynı
            // sonucu veriyor (kutucuk zemini (28,18,70) eşleştirilerek):
            //
            //     referans   kutucuk %7,90   adım %10,61
            //     bizim (99) kutucuk %9,17   adım  %9,91
            //     bizim (89) kutucuk %8,24   adım  %9,91
            //
            // Yani kutucuk hedefi %8,2 değil %7,90, adım hedefi %9,9 değil
            // %10,61. Bir önceki düzeltme doğru YÖNDEYDİ ama yeterince
            // gitmemiş, üstelik adımı sabit sanıp aralığı yanlış türetmişti.
            //     0,0790×1080 = 85  ->  boxH 85
            //     0,1061×1080 = 115 ->  gapY 115-85 = 30
            //
            // ARALIK ARTIK İKONUN TAŞMASINI KARŞILIYOR. Bu dosyanın en
            // başındaki eski ders "ikon kutunun üstünden %34 taşıyor, aralık
            // en az o kadar olmalı" diyordu; 85 × 0,34 = 29 ve aralık 30.
            // İki tur boyunca ihlal edilen kural, gerçek referansla kendini
            // doğruladı.
            //
            // DERS (kaynağı olmayan bir hedef, hedef değildir): "%8,2" iki
            // tur boyunca ölçülmüş bir gerçek gibi alıntılandı, oysa
            // dayandığı dosya çoktan silinmişti. Elde duran kareler
            // taranmadan varsayıma devam edildi.
            //
            // ...VE O REFERANS ÖLÇÜMÜ DE YANLIŞTI (aynı tur, birkaç dakika
            // sonra). Yatay tarama "kutucuk %7,90" demişti; ama referansın
            // kutucuğunun İÇİNDE ikon, etiket ve değer var ve bunlar zemini
            // yatayda kesiyor. Ölçtüğüm şey kutucuk değil, kutucuğun yazılar
            // arasında kalan en uzun kesintisiz şeridiydi.
            //
            // DOĞRU ÖLÇÜM — yazının olmadığı bir SÜTUNDA dikey tarama
            // (m_051, x = genişliğin %11,5'i):
            //     kutucuk 82 px = %18,5      adım 128 px = %28,9
            // Yani referansın kutucuğu bizimkinin İKİ BUÇUK KATI. Gözle
            // bakınca zaten öyle görünüyordu; sayı gözü doğrulamak yerine
            // iki tur boyunca yanılttı.
            //
            // DERS (bir ölçüm gözle çelişiyorsa ölçüm yanlıştır): Bu dosyada
            // üçüncü tekrar. Yatay tarama, İÇİ DOLU bir kutuyu ölçmek için
            // yanlış araç — içerik zemini böler. Kutunun kenarına yakın,
            // içeriğin uzanmadığı bir sütundan dikey taramak gerekiyor.
            //
            // BİREBİR ORAN UYGULANAMAZ — EKRAN DAHA KISA. Referans karesi
            // 443×960 (en-boy 2,167), bizim tuval 1080×1920 (1,778).
            // (NOT, 9. tur: buradaki "matchWidthOrHeight = 0" ifadesi YANLIŞTI;
            // kanvas YÜKSEKLİĞE eşli. Aşağıdaki sonuç yine de doğru çıktı ve
            // doğru eksende ölçülüp teyit edildi: kutucuk %8,54 / adım %13,85,
            // referans %8,65 / %13,54.)
            // Genişliğe göre ölçekleyen bir düzende
            // referansın ızgarası bizde 1136 birim tutuyor ve `top` 891'den
            // başlayınca 2027'ye, yani ekranın dışına taşıyor.
            //
            // Bu yüzden referansın ORANI korundu, ölçeği bütçeye sığdırıldı:
            //     referans aralık/kutucuk = 46/82 = 0,561
            //     bütçe (891 → 1859, altta 60 birim nefes payı) = 968
            //     4h + 3×0,561h = 968  ->  h = 170, aralık 96
            // Sonuç: kutucuk %15,7, adım %24,6 — referansın %18,5/%28,9'undan
            // %15 küçük ama ORAN aynı ve ekrana sığıyor.
            //
            // DERS (birebir kopyalanamayan şeyi oranıyla kopyala): Farklı
            // en-boy oranındaki bir referanstan mutlak ölçü taşınamaz;
            // taşınabilen şey öğeler ARASINDAKİ ilişkidir.
            const float boxH = 170f, gapY = 96f, top = 891f;
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

                // ORAN KORUNUYOR (11. tur). `UiKit.CreateIcon` preserveAspect
                // KURMUYOR, yani sprite kutusuna GERİLİYOR. Sekiz simgenin
                // hepsi kabaca kare olduğu sürece bu görünmüyordu; portre
                // madalya (318×378) ve yatay flama (372×324) eklenince
                // madalyanın kurdelesi ezildi, flama yayvanlaştı.
                //
                // DERS (aynı kutuya farklı oranlar): Bir ızgara "her hücre
                // aynı boyutta" der ama içine konan görseller aynı oranda
                // değilse gerilme hücre hücre farklı olur — ve bunu ancak
                // oranı FARKLI ilk varlık geldiğinde görürsün. Kutuyu
                // varlığa değil, varlığı kutuya uydurmak gerekiyor.
                icon.preserveAspect = true;
                // İkon yukarı çekildi: kutucuk büyüyünce etiket de yukarı
                // taşındı ve 0,66 ile çakışmaya başlıyordu. Taşma 0,42×170 =
                // 71 birim, aralık 96 — üstteki kutucuğa girmiyor.
                UiKit.Place(icon, 0.33f, 0.86f, 0.67f, 1.50f);

                // ETİKET 30 -> 25 PUNTO. Değer hedefe oturunca oran hâlâ
                // 1,54'te kalmıştı (referans 1,90) — fazlalık etiketteydi:
                //     referans etiket / kutucuk = 10/82 = 0,122
                //     bizim                     = 26/170 = 0,153
                // 25 punto bu oranı 0,122'ye getiriyor ve uzun İngilizce
                // etiketlerin ("Block League Wins") sığmasını da kolaylaştırıyor.
                var label = UiKit.CreateLabel("Label", boxFace.transform, Stats[i].label, 25,
                    new Color(0.639f, 0.647f, 0.918f));
                UiKit.Place(label, 0.04f, 0.57f, 0.96f, 0.84f);

                // DEĞER DİPTEN KALDIRILDI VE KUTUYA SIĞDIRILDI (9. tur).
                //
                // Eskiden 50 punto ve kutu-göreli 0,04-0,40'taydı. İki sorun:
                //   1. Alt kenar payı 0,04 × 89 = 3,6 birim — sayı kutunun
                //      dibine yapışıyordu.
                //   2. 0,36 × 89 = 32 birimlik kutuya 50 puntoluk yazı
                //      sığmıyor; `UiTextFit` her açılışta küçültüyordu, yani
                //      yazılan punto ekranda hiçbir zaman geçerli değildi.
                // Kutunun kendisi bu turda 99'dan 89'a indiği için (bkz.
                // yukarısı) pay iyice daralmıştı.
                //
                // DERS (kutuya sığmayan punto, YAZILMAMIŞ puntodur): Fit
                // açıkken büyük punto yazmak sessizce etkisiz kalıyor; sayı
                // gerçekte kaç puntoysa o yazılmalı ki bir sonraki ölçüm
                // kaynağa güvenebilsin.
                // DEĞER, ETİKETTEN BELİRGİN BÜYÜK (9. tur, gerçek referans).
                //
                // ÖLÇÜM (m_051, ilk kutucuk, harf yüksekliği / genişlik):
                //     referans  değer %4,29   etiket %2,26   -> oran 1,90
                //     bizim     değer %2,26   etiket %2,41   -> oran 0,94
                // Yani bizde etiket değerden BÜYÜKTÜ; oyuncunun aradığı sayı,
                // sayının ne olduğunu söyleyen yazının altında eziliyordu.
                // Etiketin kendisi zaten doğru boydaydı (%2,41 ≈ %2,26),
                // eksik olan yalnız değerdi: 34 → 62 punto.
                //
                // DERS (hiyerarşi, iki puntonun FARKIDIR): İki yazıyı da
                // "okunur" yapmak yetmiyor; hangisinin önce okunacağını
                // aralarındaki oran söylüyor. Referansta bu oran 1,9 ve
                // kutucuğun ne anlattığını o belirliyor.
                // İKİNCİ TUR: 62 punto yazıldı ama ekranda %3,33 ölçüldü —
                // kutu 65 birimdi ve `UiTextFit` puntoyu geri indiriyordu
                // (aynı tuzak, aynı dosyada ikinci kez). Kutu 73 birime
                // çıkarıldı ve punto gerçekten çizilebilecek değere alındı.
                //
                // HEDEF NEDEN %4,29 DEĞİL: referansın kutucuğu bizimkinden
                // %15 büyük (ekran daha uzun, bkz. yukarısı). Ölçüt olarak
                // kutucuğa göre oran alındı — referansta değerin harf
                // yüksekliği kutucuğun 0,232'si (19/82 px); bizim 170
                // birimlik kutucukta bu 39 birim, yani %3,61.
                //
                // PUNTO DOĞRUDAN ÇİZİLİYOR — FIT KÜÇÜLTMÜYOR. İki ölçüm bunu
                // kanıtladı: 62 punto %3,33, 55 punto %2,96 verdi; oran
                // 1,125, punto oranı 1,127. Yani buradaki puntolar birebir
                // ekrana gidiyor ve %3,61 için gereken 68.
                //
                // DERS (bir varsayımı iki noktayla sına): "Fit küçültüyordur"
                // diye punto düşürmek yanlış yöndü — tek ölçümle anlaşılmazdı,
                // iki farklı punto ölçülünce doğrusal ilişki ortaya çıktı.
                screen._values[i] = UiKit.CreateTitle("Value", boxFace.transform, "-", 68,
                    MenuPage.Ink, MenuPage.InkDark);
                UiKit.Place(screen._values[i], 0.04f, 0.08f, 0.96f, 0.55f);
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
            Home.Current?.Refresh();
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
