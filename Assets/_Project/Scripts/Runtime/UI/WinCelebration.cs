using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Bölüm kazanıldığında ödül kartından ÖNCE oynayan kutlama.
    ///
    /// REFERANS (`menus,powerups,vs.mp4` 01:56–02:01, 4 fps'te çıkarıldı):
    /// dizilim dört adım. (1) Tahta boşalır, ekran siyaha döner. (2) "BLOCK
    /// OUT!" logosu siyahın üstünde yaylanarak oturur — önce "BLOCK", hemen
    /// ardından "OUT!". (3) Yaklaşık 4,5 saniye boyunca arkada havai fişekler
    /// patlar, önde yoğun renkli konfeti düşer; logo ortada durur. (4) Ancak
    /// bundan sonra "MÜKEMMEL!" ödül kartı açılır.
    ///
    /// Bizde 2. ve 3. adım hiç yoktu: tahta boşalıyor ve kart doğrudan
    /// açılıyordu. Kullanıcının "kazanma ekranında çıkan efektler" dediği şey
    /// tam olarak bu eksik iki adım (2. tur, 50. madde).
    ///
    /// DERS (kutlama REFERANSTAN KISA olmalı): Referans burada 4,5 saniye
    /// harcıyor. Elli bölüm oynayan biri için bu, elli kez beklemek demek.
    /// Bizim süremiz 2,2 saniye ve DOKUNULUNCA KESİLİYOR — gösteriyi izlemek
    /// isteyen izler, acelesi olan geçer. Atlanamayan kutlama, kutlama değil
    /// vergidir.
    /// </summary>
    public sealed class WinCelebration : MonoBehaviour
    {
        // ÖLÇÜM (Levels 1-20, 59,47 fps): tahta kararması 10 kare = 0,17 sn.
        const float FadeIn = 0.17f;

        // Logonun kurulması artık 1,24 saniye sürüyor (harf harf). Referansın
        // toplam kutlaması 3,53 saniye; buradaki 1,6 ile toplam 3,3 ediyor —
        // yani hâlâ referanstan kısa, üstelik dokunuşla kesiliyor.
        // SÜRE REFERANSIN DİZİLİMİNE GÖRE.
        //
        // Son patlama ilk harften 2,741 saniye sonra; efektler `ShowStarts`
        // (0,94) anında başladığına göre çizelgenin tamamı 1,80 saniye
        // sürüyor. Buna patlamanın açılması için 0,4 eklenince 2,2 çıkıyor.
        // Referansın toplam kutlaması 3,53 sn, bizimki 0,17 + 0,94 + 2,2 +
        // 0,3 = 3,61 — yani artık neredeyse aynı. (Daha önce 1,6 idi ve
        // son üç patlama hiç görünmeden ekran kararıyordu.)
        const float Show = 2.50f;
        const float FadeOut = 0.30f;

        Canvas _canvas;
        RectTransform _root;
        RectTransform _logo;
        Image _curtain;
        TextMeshProUGUI _logoTop, _logoBottom;
        Image _logoImage;
        CanvasGroup _logoGroup;
        FX.CelebrationStage _stage;
        Coroutine _running;
        bool _skip;

        /// <summary>
        /// Zaman çarpanı: 1 normal, ilk dokunuştan sonra <see cref="TapSpeed"/>.
        ///
        /// NEDEN İKİ KADEME (12. tur, W3): Kullanıcı — *"üst üste
        /// tıkladığımızda hızlandırıp skipleyebilelim, her oyunda olan
        /// özellik."* Eskiden TEK dokunuş doğrudan sona atlıyordu; yani
        /// "biraz hızlansın" diyen oyuncu kutlamayı hiç görmeden kaybediyordu.
        ///
        /// DERS (atlamak ile hızlandırmak aynı istek değildir): Sabırsız
        /// oyuncu genelde "bitsin" demiyor, "bekletme" diyor. Tek kademeli
        /// atlama, ikisini de aynı düğmeye sıkıştırıp ilkini imkânsız
        /// kılıyor.
        /// </summary>
        float _speed = 1f;

        /// <summary>İlk dokunuştan sonraki hız çarpanı.</summary>
        const float TapSpeed = 3.2f;

        /// <summary>
        /// Perdeye dokunma: önce hızlandır, sonra atla.
        /// </summary>
        void OnTap()
        {
            if (_speed < TapSpeed) _speed = TapSpeed;
            else _skip = true;
        }

        public static WinCelebration Create(Transform parent)
        {
            var holder = new GameObject("WinCelebration");
            holder.transform.SetParent(parent, worldPositionStays: false);
            var celebration = holder.AddComponent<WinCelebration>();
            celebration.Build();
            return celebration;
        }

        /// <summary>
        /// EDİTÖR ÖNİZLEMESİ: kutlamayı KANVAS KURMADAN kurar (12. tur, W1).
        ///
        /// `Create` yolunda `UiKit.CreateCanvas` var, o da `DontDestroyOnLoad`
        /// çağırıyor — düzenleyici kipinde yasak (bu projede KURAL 0). Sonuç
        /// ve duraklat önizlemelerinde çözülen aynı sorun: kanvası dışarıdan
        /// almak.
        ///
        /// Harfler `localScale = 0` ile kuruluyor (giriş animasyonu için);
        /// önizleme onları 1'e çekiyor ki logo durağan hâliyle ölçülebilsin.
        /// </summary>
        public static WinCelebration CreateLogoPreview(Transform parent)
        {
            // KÖK DÜZ TRANSFORM DEĞİL, RECTTRANSFORM OLMALI.
            //
            // İlk denemede `new GameObject("WinPreview")` düz bir Transform
            // veriyordu ve yakalama BOMBOŞ çıktı. Arayüz çocukları çapalarını
            // ÜST DİKDÖRTGENE göre çözüyor; düz bir Transform'un altında
            // yerleşim sessizce çöküyor, hata da vermiyor.
            //
            // Bu tuzak `CreateResultPreview`de zaten belgelenmişti — aynı
            // dosyada okumuş olmama rağmen yeni önizlemede tekrarladım.
            var host = UiKit.CreateRect("WinPreview", parent);
            UiKit.Place(host, 0f, 0f, 1f, 1f);
            var c = host.gameObject.AddComponent<WinCelebration>();
            c.Build(host);
            var holder = host.gameObject;

            // `Build` sonunda kök KAPATILIYOR (kutlama ancak kazanınca açılır).
            // Önizlemenin görebilmesi için açılıyor; konfeti sahnesi kapalı
            // kalıyor — burada ölçülen şey LOGO.
            c._root.gameObject.SetActive(true);

            foreach (var rt in holder.GetComponentsInChildren<RectTransform>(true))
                if (rt.localScale.x < 0.02f || rt.localScale.y < 0.02f)
                    rt.localScale = Vector3.one;
            if (c._logoGroup != null) c._logoGroup.alpha = 1f;
            return c;
        }

        void Build() => Build(null);

        void Build(Transform hostOverride)
        {
            Transform host;
            if (hostOverride != null)
            {
                // Önizleme yolu: kanvas yok, doğrudan verilen köke kuruluyor.
                host = hostOverride;
            }
            else
            {
                _canvas = UiKit.CreateCanvas("WinCanvas");
                _canvas.transform.SetParent(transform, worldPositionStays: false);
                _canvas.sortingOrder = 210;          // sonuç kartının da üstünde
                host = _canvas.transform;
            }

            _root = UiKit.CreateRect("Root", host);
            UiKit.Place(_root, 0f, 0f, 1f, 1f);

            // Referansta zemin TAM SİYAH, oyunun moru değil: fişeklerin
            // parlaklığı ancak siyahın üstünde okunuyor. Mor zeminde aynı
            // kıvılcımlar soluk kalırdı.
            _curtain = UiKit.CreatePanel("Curtain", _root, new Color(0f, 0f, 0f, 1f));
            _curtain.raycastTarget = true;       // dokunuş kutlamayı keser

            var skip = _curtain.gameObject.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(OnTap);

            // GERÇEK LOGO GÖRSELİ (49. madde).
            //
            // ÖLÇÜM (`menus,powerups,vs.mp4` kazanma dizilimi, 384x832 kare):
            // logonun mor halesi x 54..321 — yani ekran genişliğinin %69,5'i;
            // dikey merkezi 0.496, tam ortada.
            //
            // DERS (harf biçimi TARİF EDİLEMEZ): Logo önce iki TMP satırıyla
            // kuruluyordu, sonra yapay zekâya "aynı tarzda" ürettirildi;
            // ikisi de referansa benzemedi. Bir markanın harfleri onun
            // kimliği — "kalın, yuvarlak, altın" diye tarif edilince ortaya
            // hep başka bir şey çıkıyor. Doğru yol referans karesinden
            // kesmekti; medyan alma hilesi de arkadaki konfetiyi sildi.
            // Dikdörtgen, görselin en-boy oranından DAHA UZUN tutuluyor.
            //
            // DERS (`preserveAspect` hangi kenara sığdırır?): Önce 0.695 x 0.20
            // yazdım — genişlik doğru olsun diye. Ölçtüğümde logo 0.589 çıktı:
            // dikdörtgen (1.95) görselden (1.66) daha YASSI olduğu için
            // `preserveAspect` YÜKSEKLİĞE sığdırdı ve genişlik kendiliğinden
            // küçüldü. İstenen kenarın bağlayıcı olması için diğer kenar bol
            // bırakılmalı.
            _logo = UiKit.CreateRect("Logo", _root);
            UiKit.Place(_logo, 0.1525f, 0.366f, 0.8475f, 0.626f);   // %69,5 genişlik

            // HARF HARF GELİŞ GERİ GELDİ — ÇÜNKÜ ARTIK VARLIK VAR (8. tur).
            //
            // 5. ve 6. turda bu iki kez denendi, iki kez de "yapboz" göründü
            // ve terk edildi. Sebebi doğru teşhis edilmişti: logo TEK bir PNG
            // ve harfler ORTAK bir mor konturla birbirine bağlı. Bir dilimi
            // küçültünce ekranda görünen şey harf değil, KÜÇÜLTÜLMÜŞ BİR
            // DİKDÖRTGEN oluyordu — iki yanında dümdüz kesik kenarlar.
            //
            // O turlarda "harf başına ayrı görsel elimizde yok" denip
            // vazgeçilmişti. Yanlış olan bu son adımdı: görsel elimizde
            // YOKTU ama ÜRETİLEBİLİRDİ. `tools/slice_logo.py` harfleri
            // renklerinden buluyor, kalan her pikseli en yakın harfe
            // veriyor ve her parçaya kendi mor konturunu geri büyütüyor.
            // Altı PNG çıkıyor; hepsi açıkken sonuç piksel piksel logonun
            // kendisi (betik bunu sayarak doğruluyor).
            //
            // DERS (eksik olan animasyon değil VARLIK ise, varlığı üret):
            // Üç tur boyunca eğri, süre ve alfa ile oynandı; sorun hiçbirinde
            // değildi. "Bu varlıkla yapılamaz" doğru bir teşhis, ama tek
            // başına bir karar değil — sıradaki soru "o varlığı biz
            // üretebilir miyiz?" olmalıydı.
            var art = UiSkin.Get(Art.GameLogo);
            if (BuildLetters())
            {
                _logoGroup = _logo.gameObject.AddComponent<CanvasGroup>();
                _logoGroup.blocksRaycasts = false;
            }
            else if (art != null)
            {
                _logoImage = UiKit.CreateIcon("Mark", _logo, art);
                UiKit.Place(_logoImage, 0f, 0f, 1f, 1f);
                _logoImage.preserveAspect = true;
            }
            else
            {
                // Görsel yoksa eski iki satırlık yazıya düşülür; ekran boş
                // kalmasın diye duruyor, tercih edilen yol değil.
                _logoTop = UiKit.CreateTitle("Top", _logo, "BLOCK", 96,
                    new Color(0.98f, 0.31f, 0.29f), new Color(0.24f, 0.06f, 0.30f));
                UiKit.Place(_logoTop, 0f, 0.50f, 1f, 1f);

                _logoBottom = UiKit.CreateTitle("Bottom", _logo, "OUT!", 96,
                    new Color(1f, 0.79f, 0.13f), new Color(0.24f, 0.06f, 0.30f));
                UiKit.Place(_logoBottom, 0f, 0f, 1f, 0.50f);
            }

            // PARÇACIK SAHNESİ EN SON: kardeş sırası çizim sırası, yani
            // konfeti ve fişekler LOGONUN ÖNÜNDEN geçiyor. Referansta da
            // konfeti harflerin üstünden akıyor.
            _stage = FX.CelebrationStage.Create(_root);
            _stage.SetActive(false);

            _root.gameObject.SetActive(false);
        }

        readonly System.Collections.Generic.List<RectTransform> _letters =
            new System.Collections.Generic.List<RectTransform>();

        /// <summary>Harflerin mor zeminleri — hepsi harflerin ALTINDA çizilir.</summary>
        readonly System.Collections.Generic.List<RectTransform> _backs =
            new System.Collections.Generic.List<RectTransform>();

        /// <summary>Bir harf parçasının logo görseli içindeki yeri (0-1).</summary>
        readonly struct LetterRect
        {
            public readonly string Name;
            public readonly float X0, Y0, X1, Y1;
            public LetterRect(string name, float x0, float y0, float x1, float y1)
            { Name = name; X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
        }

        /// <summary>
        /// Harf parçalarının yerleri — `tools/slice_logo.py` BASIYOR, elle
        /// yazılmıyor. Kaynak görsel 662x399; sayılar oranlanmış hâlleri.
        /// Görselin y ekseni üstten, arayüzünki alttan olduğu için betik
        /// çeviriyi de kendi yapıyor.
        /// </summary>
        static readonly LetterRect[] LetterRects =
        {
            new LetterRect("logo_b",   0.0015f, 0.3784f, 0.2870f, 0.8697f),
            new LetterRect("logo_l",   0.1571f, 0.4461f, 0.3837f, 0.8997f),
            new LetterRect("logo_o",   0.2704f, 0.4486f, 0.6511f, 0.9975f),
            new LetterRect("logo_c",   0.5317f, 0.4486f, 0.8353f, 0.9123f),
            new LetterRect("logo_k",   0.7069f, 0.3960f, 0.9985f, 0.8872f),
            new LetterRect("logo_out", 0.0619f, 0.0000f, 0.9502f, 0.5764f),
        };

        /// <summary>Kaynak logonun en-boy oranı (662/399).</summary>
        const float LogoAspect = 662f / 399f;

        /// <summary>
        /// Logoyu HARF BAŞINA İKİ görselden kurar: önce bütün mor zeminler,
        /// sonra bütün harfler. Parçalardan biri bile eksikse hiçbirini
        /// kurmaz ve <c>false</c> döner — yarım bir logo, tek parça olandan
        /// kötüdür.
        ///
        /// NEDEN İKİ KATMAN (kullanıcı: "çok kesik kesik, fazla alınmış
        /// yerler, bazı yerler eksik alınmış"):
        ///
        /// İlk sürüm logoyu bir BÖLÜNTÜYE çeviriyordu — her piksel tek bir
        /// harfe gidiyordu. Birleşik görüntü kusursuzdu ama harfler tek
        /// başına bozuktu: bir harfin mor zemini komşusunun piksellerini
        /// İÇEREMEZ, çünkü onlar komşuya ait. Sonuç, her zeminde komşusu
        /// şeklinde bir ısırık ve mesafeye göre çizilmiş tırtıklı sınırlar.
        ///
        /// Referanstaki harflerin her birinin KENDİ kapalı zemini var ve
        /// üst üste biniyorlar. Zeminler ayrı bir katmana alınınca ikisi
        /// birden mümkün oluyor: zeminler serbestçe örtüşüyor (hepsi aynı
        /// moru taşıdığı için fark etmiyor), harfler en üstte sırayla
        /// çiziliyor ve birleşim aslından ayırt edilemiyor.
        ///
        /// DERS (bölüntü mü, katman mı?): "Parçalar birleşince aslını
        /// versin" şartı tek başına yetmiyor. Parçanın TEK BAŞINA da doğru
        /// görünmesi gerekiyorsa bölüntü yanlış araç; üst üste binmeye izin
        /// vermek iki şartı aynı anda sağlıyor.
        /// </summary>
        bool BuildLetters()
        {
            var backArt = new Sprite[LetterRects.Length];
            var letterArt = new Sprite[LetterRects.Length];
            for (int i = 0; i < LetterRects.Length; i++)
            {
                backArt[i] = UiSkin.Get(LetterRects[i].Name + "_back");
                letterArt[i] = UiSkin.Get(LetterRects[i].Name);
                if (backArt[i] == null || letterArt[i] == null) return false;
            }

            // ÇERÇEVE, `preserveAspect`in YERİNİ TUTUYOR.
            //
            // Tek parça logo `preserveAspect = true` ile çiziliyordu: görsel
            // dikdörtgene sığdırılıyor, artan yer boşluk kalıyordu. Harfleri
            // doğrudan `_logo` içine oranlarıyla koysaydım hepsi dikdörtgene
            // GERİLİRDİ ve logo yassılaşırdı.
            //
            // `AspectRatioFitter.FitInParent` tam olarak `preserveAspect`in
            // hesabını yapıyor: bu çerçeve, `_logo` içinde 662/399 oranını
            // koruyan en büyük dikdörtgen.
            var frame = UiKit.CreateRect("Frame", _logo);
            UiKit.Place(frame, 0f, 0f, 1f, 1f);
            var fitter = frame.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = LogoAspect;

            // İKİ KAP: kardeş sırası çizim sırası olduğu için bütün zeminler
            // bütün harflerin ALTINDA kalıyor.
            var backLayer = UiKit.CreateRect("Backs", frame);
            UiKit.Place(backLayer, 0f, 0f, 1f, 1f);
            var letterLayer = UiKit.CreateRect("Letters", frame);
            UiKit.Place(letterLayer, 0f, 0f, 1f, 1f);

            for (int i = 0; i < LetterRects.Length; i++)
            {
                var slot = LetterRects[i];

                // Zemin ve harf AYNI dikdörtgeni paylaşıyor. Şart: ikisi
                // farklı kaplarda olduğu için tek bir dönüşümle
                // ölçeklenemiyorlar; aynı kutuya ve aynı pivota oturunca
                // aynı çarpan ikisini de birebir aynı hareket ettiriyor.
                var back = Piece(backLayer, "Back_" + slot.Name, backArt[i], slot);
                var letter = Piece(letterLayer, "Letter_" + slot.Name, letterArt[i], slot);

                _backs.Add(back);
                _letters.Add(letter);
            }
            return true;

            RectTransform Piece(RectTransform parent, string name, Sprite art, LetterRect slot)
            {
                var image = UiKit.CreateIcon(name, parent, art);
                image.raycastTarget = false;
                var rect = image.rectTransform;
                UiKit.Place(rect, slot.X0, slot.Y0, slot.X1, slot.Y1);

                // "OUT!" KENDİ ALT KENARINDAN BÜYÜYOR, ötekiler ortasından.
                //
                // ÖLÇÜM (Levels 1-20, iki ayrı kutlama, 59,47 fps): "OUT!"
                // büyürken en yoğun altın satırı 696 → 590 → 648 pikselde
                // geziniyor, yani şişerken YUKARI çıkıp geri iniyor. Ortadan
                // ölçeklenen bir şey bunu yapmaz; dönme noktası kelimenin
                // altında.
                rect.pivot = slot.Name == "logo_out"
                    ? new Vector2(0.5f, 0.20f)
                    : new Vector2(0.5f, 0.5f);

                rect.localScale = Vector3.zero;
                return rect;
            }
        }

        // ÖLÇÜM (`…Levels 1-20 Walkthrough.mp4`, 59,47 fps, İKİ ayrı kutlama
        // — 05:16 ve 04:01 — bağımsız ölçülüp aynı çıktı):
        //
        //   harf       başlangıç   tepe ölçek   oturma
        //   B          0,000 sn
        //   L          0,105 sn    ~1,40        ~0,38 sn sonra
        //   O (tuğla)  0,210 sn
        //   C          0,315 sn
        //   K          0,420 sn
        //   OUT!       0,525 sn    1,65         0,72 sn sonra
        //   konfeti    0,940 sn
        //
        // Harfler doygunluklarıyla sayıldı (B kırmızı, C yeşil, K camgöbeği);
        // aralık her seferinde 6-7 kare, yani 0,10-0,12 saniye çıktı.
        const float LetterStep = 0.105f;
        const float LetterPop = 0.38f;
        const float LetterOvershoot = 1.40f;

        /// <summary>
        /// "OUT!" tepe ölçeği. Referansta kelime son boyunun 1,42 katına
        /// kadar şişip geri iniyor — o sırada "BLOCK" satırını tamamen
        /// örtüyor. İki ayrı kutlamada aynı çıktı. Logonun kalan beş harfi
        /// bu sırada KIPIRDAMIYOR: kırmızı B'nin piksel sayısı 8,2 binde
        /// sabit kalıyor. Yani büyüyen şey logo değil, yalnız alt satır.
        ///
        /// DERS (ölçtüğün şey, ölçmek istediğin şey mi?): Önce 1,65 yazdım.
        /// O sayı "altın piksel sayısı en yüksek olan SATIRIN genişliği"nden
        /// geliyordu; kelime büyüdükçe o satır harfin başka bir yerine denk
        /// düştüğü için oran şişiyordu. Kelimenin gerçek KUTUSU ölçülünce
        /// 537/379 = 1,42 çıkıyor. Fark masum değildi: 1,65 ile bizim
        /// "OUT!" ekran genişliğinin %103'üne çıkıp iki yandan kesilecekti.
        /// </summary>
        const float OutOvershoot = 1.42f;
        const float OutPop = 0.72f;

        /// <summary>
        /// Konfeti ve fişeklerin başladığı an (ilk harften itibaren).
        ///
        /// 0,94 -> 0,55 (12. tur, W2). Kullanıcı: *"konfetiler havai fişekler
        /// sırası uyumu orjinaldekiyle aynı değil."*
        ///
        /// YENİDEN ÖLÇÜLDÜ. `menus,powerups,vs.mp4` ve walkthrough videoları
        /// bir ara "depoda yok" sanılmıştı; OneDrive'da duruyorlarmış.
        /// Kutlama 20 fps'te 120 kareye ayrıldı, t=0 siyaha geçiş karesi:
        ///
        ///     t(sn)   konfeti(alt)%   fişek(üst)%   logo%
        ///     0,15        0,1            0,9         25,4
        ///     0,35        0,0            3,6         28,7
        ///     0,55        3,2            4,0         49,6   <- konfeti BAŞLIYOR
        ///     0,75        3,9            2,7         59,4
        ///     1,15        5,1            3,4          —
        ///     1,55        7,1            3,7          —
        ///     1,75        8,7            6,5          —     <- konfeti doyuyor
        ///     2,15        8,7           10,2          —     <- fişek tepesi
        ///     2,55        8,5            4,9          —
        ///
        /// Yani konfeti logo HENÜZ OTURURKEN başlıyor (t=0,55'te logo %49,6,
        /// yani harflerin yarısı yeni gelmiş). Bizde 0,94 idi: logo tamamen
        /// yerleştikten sonra, arada gözle görülür bir ölü an bırakarak.
        /// Fişek tablosu (`ReferenceBursts`, 1,614-2,741) ölçülen 1,75-2,15
        /// penceresiyle örtüşüyor; ona dokunulmadı.
        ///
        /// DERS (bir kutlamada üst üste binme, sıralamadan iyidir): İki olayı
        /// arka arkaya dizmek "önce şu bitsin, sonra bu başlasın" diye
        /// düşünmenin doğal sonucu ama referans onları BİNDİRİYOR — konfeti
        /// logo otururken patlıyor ve iki hareket birbirini itiyor. Aradaki
        /// 0,39 saniyelik boşluk, kutlamayı iki ayrı gösteriye bölüyordu.
        /// </summary>
        const float ShowStarts = 0.55f;

        /// <summary>
        /// Harfleri soldan sağa, ölçülen aralıklarla getirir.
        ///
        /// DERS (grup ölçeği DEĞİL, harf ölçeği): Eski sürüm harfleri
        /// açarken bütün logoyu da 0,42'den 1,12'ye büyütüyordu — "logo
        /// kuruluyor" hissi versin diye. Referansta böyle bir şey yok:
        /// logonun çerçevesi hiç oynamıyor, her harf KENDİ yerinde
        /// sıfırdan şişip oturuyor. Grup ölçeği eklendiğinde daha önce
        /// yerleşmiş harfler de kaydığı için göz "yapboz" görüyordu.
        /// </summary>
        IEnumerator RevealLetters()
        {
            for (int i = 0; i < _letters.Count; i++)
            {
                bool last = i == _letters.Count - 1;
                GameKit.FX.Juice.Run(PopLetter(_backs[i], _letters[i],
                    last ? OutPop : LetterPop,
                    last ? OutOvershoot : LetterOvershoot));

                if (last) break;
                for (float t = 0f; t < LetterStep && !_skip; t += Time.unscaledDeltaTime * _speed)
                    yield return null;
                if (_skip) break;
            }

            if (_skip)
            {
                foreach (var letter in _letters)
                    if (letter != null) letter.localScale = Vector3.one;
                foreach (var back in _backs)
                    if (back != null) back.localScale = Vector3.one;
                yield break;
            }

            // Konfeti "OUT!" tepe noktasını geçtikten sonra başlıyor; burada
            // beklenen süre son harfin başlangıcından konfetiye kadar olan
            // fark. Kalan oturma hareketi konfetinin altında sürüyor —
            // referansta da öyle.
            float wait = ShowStarts - LetterStep * (_letters.Count - 1);
            for (float t = 0f; t < wait && !_skip; t += Time.unscaledDeltaTime * _speed)
                yield return null;
        }

        /// <summary>
        /// Tek bir harfi getirir: sıfırdan tepe ölçeğe, oradan son boyuna.
        ///
        /// ÖLÇÜM (tuğla "O" harfinin yüksekliği, 59,47 fps): 45 → 156 → 113
        /// piksel. Yani harf son boyunu %38-40 AŞIYOR ve geri iniyor;
        /// büyüme yolun ilk yarısında, geri oturma ikinci yarısında.
        ///
        /// DERS (aşma tek yönlü bir "back" eğrisi DEĞİL): `EaseOutBack` sona
        /// doğru bir tık aşıp döner — aşma oranı %10 civarında kalır ve göz
        /// bunu "yaylandı" diye okur. Referanstaki hareket bambaşka: harf
        /// önce belirgin biçimde BÜYÜK geliyor, sonra küçülüyor. İkisi aynı
        /// kelimeyle ("overshoot") anılsa da farklı şeyler.
        /// </summary>
        /// <remarks>
        /// STATIC DEĞİL (14. tur, W3): Harf açılışı tek yerde hızdan
        /// bağımsız kalmıştı. `static` olduğu için <c>_speed</c> ve
        /// <c>_skip</c> alanlarını göremiyordu; ekranda perdeye dokunup
        /// kutlamayı 3,2 katına çıkaran oyuncu, harflerin ESKİ hızda
        /// açıldığını görüyordu — beklemeler kısalıyor ama harfler
        /// kısalmıyor, ikisi birbirinden kopuyordu.
        ///
        /// DERS (bir hız çarpanı, zamanı okuyan HER yerde geçerli olmalı):
        /// Hızlandırmayı beklemelere uygulayıp animasyonlara uygulamamak,
        /// "hızlandı" değil "senkron bozuldu" hissi veriyor. Zamanı okuyan
        /// yeni bir döngü yazarken çarpanı da yazmak gerekiyor.
        /// </remarks>
        IEnumerator PopLetter(RectTransform back, RectTransform letter,
                              float duration, float peak)
        {
            if (letter == null) yield break;

            float rise = duration * 0.5f;
            for (float t = 0f; t < duration && !_skip; t += Time.unscaledDeltaTime * _speed)
            {
                if (letter == null) yield break;

                float scale;
                if (t < rise)
                {
                    float k = t / rise;
                    scale = Mathf.Lerp(0f, peak, 1f - (1f - k) * (1f - k));   // hızlı çıkış
                }
                else
                {
                    float k = Mathf.Clamp01((t - rise) / (duration - rise));
                    scale = Mathf.Lerp(peak, 1f, k * k * (3f - 2f * k));      // yumuşak iniş
                }
                // Zemin ve harf AYNI çarpanla: aynı kutuyu ve aynı pivotu
                // paylaştıkları için birlikte hareket ediyorlar.
                letter.localScale = Vector3.one * scale;
                if (back != null) back.localScale = Vector3.one * scale;
                yield return null;
            }
            if (letter != null) letter.localScale = Vector3.one;
            if (back != null) back.localScale = Vector3.one;
        }

        /// <summary>
        /// SAHNE, KUTLAMAYLA BİRLİKTE ÖLMELİ.
        ///
        /// <see cref="FX.CelebrationStage"/> bir KÖK nesne — çocuk olamıyor,
        /// çünkü dünyada oyundan uzakta durması gerekiyor. Bu yüzden kutlama
        /// yok edildiğinde otomatik olarak yok olmuyordu: geriye her karede
        /// boş bir dokuya çizen bir kamera kalıyor, üstelik yüzeyi (kutlama
        /// kanvasının çocuğu) ölmüş olduğu için ona dokunan her satır
        /// `MissingReferenceException` atıyordu.
        ///
        /// DERS (bağı olmayanın ömrünü sen taşırsın): Unity'de ömür,
        /// hiyerarşiyle geliyor. Bir nesneyi bilerek hiyerarşinin dışına
        /// koyduysan, onu kapatma sorumluluğunu da almışsındır.
        /// </summary>
        void OnDestroy()
        {
            if (_stage != null) Destroy(_stage.gameObject);
        }

        /// <summary>Kutlamayı oynatır; bittiğinde <paramref name="done"/> çağrılır.</summary>
        public void Play(System.Action done)
        {
            if (_running != null) GameKit.FX.Juice.Stop(_running);
            _running = GameKit.FX.Juice.Run(Routine(done));
        }

        IEnumerator Routine(System.Action done)
        {
            _skip = false;
            _speed = 1f;
            _root.gameObject.SetActive(true);
            SetAlpha(0f);

            // İKİNCİ BÖLÜMDE LOGO HAZIR GELİYORDU (kullanıcı bildirdi).
            //
            // `WinCelebration` bir kez kuruluyor ve her bölümde YENİDEN
            // KULLANILIYOR. Bir önceki kutlama bittiğinde harflerin ölçeği
            // 1'de kalıyor; ikinci kutlamada `RevealLetters` sıfırlamayı
            // harfin KENDİ rutinine bırakıyordu ve o rutin ilk karesini
            // ancak sırası gelince çalıştırıyor. Yani ilk yarım saniye
            // boyunca bütün harfler tam boyda ekranda duruyor, sonra tek tek
            // "yeniden" beliriyorlardı — animasyon ilk bölümde doğru,
            // sonrakilerde bozuk görünüyordu.
            //
            // DERS (bu turda ikinci kez): Gecikmeli bir animasyonun
            // BAŞLANGIÇ DURUMU gecikemez. Aynı hata PERFECT kartında da
            // vardı (bkz. GameplayScreen.CelebrateRoutine) — ortak sebep,
            // "sıfırla" işini animasyonun kendisine bırakmak.
            foreach (var letter in _letters)
                if (letter != null) letter.localScale = Vector3.zero;
            foreach (var back in _backs)
                if (back != null) back.localScale = Vector3.zero;

            yield return GameKit.FX.Juice.Tween(FadeIn, SetAlpha);

            // LOGO ÖNCE KURULUR, KUTLAMA SONRA BAŞLAR (4. tur, J37/J38).
            //
            // DERS (sıra bir anlatımdır): Konfeti ve fişek eskiden logoyla
            // AYNI anda başlıyordu; ekran ilk kareden itibaren dolu olduğu
            // için logonun kurulduğu görülmüyordu. Referansta dizilim net:
            // önce harfler siyahın üstünde tek tek diziliyor, logo
            // tamamlandığında ekran patlıyor. Aynı öğeler, farklı sıra,
            // bambaşka bir his.
            // DERS (yedek yol da BİR YOLDUR — çökmemeli): Buradaki üçüncü dal
            // "görsel hiç yoksa yazıyla göster" diyordu ve `_logoTop`u null
            // KONTROL ETMEDEN kullanıyordu. Bir kez o dala düşünce
            // NullReferenceException coroutine'i öldürdü; `done` hiç
            // çağrılmadı ve PERFECT kartı ASLA AÇILMADI. Yani küçük bir
            // yedek yol hatası, oyunun bitiş akışını tamamen durdurdu.
            //
            // Kutlama bir SÜS; hiçbir koşulda oyunun akışını kesmemeli.
            // Bu yüzden her dal null'a dayanıklı ve hiçbiri "hiç bitmeme"
            // ihtimali taşımıyor.
            if (_letters.Count > 0)
            {
                yield return RevealLetters();
            }
            else if (_logoImage != null)
            {
                // BEKLENİYOR, fırlatılıp unutulmuyor: referansta logo
                // TAMAMLANDIKTAN sonra fişekler başlıyor. Eskiden `Run(...)`
                // ile başlatılıp hemen devam ediliyordu ve konfeti logonun
                // üstüne biniyordu.
                yield return LogoEntrance();
            }
            else
            {
                if (_logoTop != null) GameKit.FX.Juice.Run(DropIn(_logoTop.rectTransform, 0f));
                if (_logoBottom != null)
                    GameKit.FX.Juice.Run(DropIn(_logoBottom.rectTransform, 0.10f));
            }

            // LOGO VURUŞU KALDIRILDI (harf harf geliş varken).
            //
            // Referansta logo tamamlandıktan sonra bütünüyle bir daha
            // oynamıyor; "işte bu" anını zaten "OUT!"un 1,65 katına şişip
            // geri oturması veriyor. Üstüne bir de tüm logoyu vurmak, o
            // hareketin hemen ardına ikinci ve daha zayıf bir hareket
            // koyuyordu — vurgu ikiye bölününce ikisi de vurgu olmuyor.
            //
            // Yedek yollarda (tek parça görsel ya da yazı) hâlâ gerekli:
            // orada logoyu canlandıran başka bir şey yok.
            if (_letters.Count == 0) GameKit.FX.Juice.Run(Punch(_logo));

            // ------------------------------------------------ efektler
            //
            // KENDİ ÇİZDİĞİMİZ KONFETİ VE FİŞEKLER YERİNİ EPIC TOON FX'E
            // BIRAKTI (9. tur).
            //
            // Eskisi arayüz dikdörtgenlerinden kuruluyordu: her konfeti
            // parçası bir `Image`, her kıvılcım bir başka `Image`. Ölçülerek
            // ayarlanmıştı (300 parça, ölçülen renk paleti) ama ARAYÜZ
            // dikdörtgeni bir parçacık değil — dönemiyor, çarpışmıyor,
            // yerçekimi eğrisi yok, dokusu yok. Ekranda "kâğıt parçaları
            // düşüyor" değil "renkli dikdörtgenler kayıyor" gibi okunuyordu.
            //
            // Paket bunları hazır ve çok daha zengin veriyor. Kurulum
            // maliyeti tek bir yerde toplandı: parçacıklar üst katman
            // kanvasın üstüne çizilemediği için ayrı bir kameraya ve dokuya
            // ihtiyaç var (bkz. CelebrationStage).
            //
            // YEDEK YOL DURUYOR: paket projeden çıkarılırsa `FxSkin` boş
            // döner ve aşağıdaki eski çizim devreye girer. Kutlama bir süs;
            // bir varlığın yokluğu oyunun akışını durdurmamalı.
            if (PlayPackageEffects())
            {
                // paket efektleri oynuyor
            }
            else
            {
                GameKit.FX.Juice.Run(FX.CelebrationFX.Rockets(_root, count: 11, interval: 0.13f));
                GameKit.FX.Juice.Run(FX.CelebrationFX.Show(
                    _root, bursts: 4, interval: 0.34f, sparks: 48));
                GameKit.FX.Juice.Run(FX.CelebrationFX.Rain(
                    _root, count: 300, duration: Show * 0.55f));
            }

            Services.AudioService.Star();

            for (float t = 0f; t < Show && !_skip; t += Time.unscaledDeltaTime * _speed)
                yield return null;

            yield return GameKit.FX.Juice.Tween(FadeOut, t => SetAlpha(1f - t));

            // Sahne kapanıyor: açık kalan bir kamera, kutlama olmadığı
            // anlarda da her karede boş bir dokuya çizerdi.
            if (_stage != null) { _stage.Clear(); _stage.SetActive(false); }

            _root.gameObject.SetActive(false);
            _running = null;
            done?.Invoke();
        }

        /// <summary>
        /// Paket efektlerini sahneye dizer. Paket yoksa <c>false</c> döner ve
        /// çağıran eski çizime düşer.
        ///
        /// DİZİLİM referanstan (Levels 1-20, kutlama kareleri):
        ///   • Konfeti ekranın ÜSTÜNDEN yağıyor ve bütün genişliği kaplıyor.
        ///   • İki alt köşeden yukarı doğru birer patlama atılıyor.
        ///   • Fişekler ekranın üst yarısına, aralıklarla ve farklı
        ///     renklerde. Referansta aynı anda en az iki patlama var, bu
        ///     yüzden aralık patlamanın ömründen kısa.
        /// </summary>
        bool PlayPackageEffects()
        {
            if (_stage == null) return false;
            if (FX.FxSkin.Get(FX.Fx.ConfettiUp) == null &&
                FX.FxSkin.Get(FX.Fx.Fireworks[0]) == null) return false;

            _stage.SetActive(true);
            GameKit.FX.Juice.Run(ReferenceSequence());
            return true;
        }

        // ================= REFERANS DİZİLİMİ =================
        //
        // Kullanıcı: "konfetilerin havai fişeklerin patladığı yeri iyi
        // dikkatlice incele, sıralama birebir aynı olmalı, konumları filan
        // tamamen aynı olmalı."
        //
        // ÖLÇÜM (`…Levels 1-20 Walkthrough.mp4`, 59,47 fps ham kareler;
        // sıfır anı ilk harfin belirdiği kare). Yöntem: her karede beyaz
        // kıvılcım maskesi bağlı bileşenlere ayrıldı, 2500 pikselden büyük
        // YENİ bir bileşen "patlama" sayıldı ve ağırlık merkezi ekran
        // oranına çevrildi. Konfeti içinse renkli piksellerin dikey dağılımı
        // izlendi.
        //
        // İLK BULGU — KONFETİ YUKARIDAN DEĞİL AŞAĞIDAN GELİYOR.
        // t=0,99'da renkli piksellerin %99,8'i ekranın ALT yarısında; en üst
        // konfeti 0,358'de. 0,27 saniye içinde tepe 0,100'e çıkıyor. Yani
        // tek bir YUKARI FIRLATMA, sonra yağmur. Bizde yağmur yukarıdan
        // dökülüyordu — dizilim ters başlıyordu.
        //
        // DERS (bir efektin YÖNÜ, yoğunluğundan daha çok şey anlatır):
        // Yukarıdan dökülen konfeti "kutlama sürüyor" der; aşağıdan fırlayan
        // konfeti "AZ ÖNCE bir şey oldu" der. İkisi de aynı kâğıt parçaları.
        const float ConfettiAt = 0.98f;

        /// <summary>
        /// Konfeti topları ekranın alt kenarında, genişliğe yayılı.
        /// Referansta konfeti ilk karede bütün genişliği kaplıyor; tek bir
        /// koni bunu veremiyor (koni açısı 12°).
        /// </summary>
        static readonly float[] ConfettiCannons =
            { 0.06f, 0.22f, 0.36f, 0.50f, 0.64f, 0.78f, 0.94f };

        /// <summary>
        /// Ölçülen patlamalar: (an, x, y). x soldan, y alttan — ekran oranı.
        ///
        /// Sıra ve konum BİREBİR referanstan; ara süreler 0,118 ile 0,252
        /// arasında değişiyor ve düzenli değil, o yüzden sabit bir aralık
        /// yerine tablo tutuluyor.
        /// </summary>
        static readonly Vector3[] ReferenceBursts =
        {
            new Vector3(1.614f, 0.806f, 0.679f),
            new Vector3(1.799f, 0.152f, 0.677f),
            new Vector3(2.001f, 0.479f, 0.804f),
            new Vector3(2.119f, 0.686f, 0.805f),
            new Vector3(2.371f, 0.699f, 0.352f),
            new Vector3(2.489f, 0.692f, 0.753f),
            new Vector3(2.623f, 0.339f, 0.635f),
            new Vector3(2.741f, 0.866f, 0.693f),
        };

        /// <summary>
        /// Roket izinin patlamadan ne kadar önce çıktığı. ÖLÇÜM: ilk iz
        /// t=1,278'de görünüyor, ona ait patlama t=1,614'te — 0,336 saniye.
        /// </summary>
        const float RocketRise = 0.33f;

        // ÖLÇÜM (renkli piksellerin ekrana oranı, logo hariç):
        //   referans  tepe %4,98   (t=2,4 civarı)
        //   bizim ilk hâl  tepe %1,06
        // Yani beş kat daha az konfeti vardı. Yedi topa çıkarıldı ve her
        // topun parça sayısı üçe katlandı (7×3 ≈ 4,2 kat).
        // ÖLÇÜM (referans, konfetinin en üst noktası): fırlatmadan 0,28
        // saniye sonra tepe y=0,10'a, yani ekranın ÜSTÜNE ulaşıyor.
        //
        // İlk denemede ölçek 1,15'ti ve parçalar ekranın alt üçte birinden
        // yukarı çıkamıyordu: yoğunluk (%4,79) referansla eşitti ama hepsi
        // dipte bir duvar hâlinde toplanıyordu. `localScale` parçacık
        // sisteminde HIZI da çarptığı için ölçeği büyütmek menzili
        // uzatıyor; sayı da o oranda düşürülüyor ki toplam yoğunluk
        // bozulmasın.
        //
        // DERS (aynı sayı, farklı dağılım): "Ekranın %5'i konfeti" ölçütü
        // tek başına yetmiyor — o %5'in NEREDE olduğu da ölçülmeli.
        const float ConfettiScale = 2.2f;
        const float ConfettiCount = 0.75f;

        /// <summary>İki top arasındaki süre — dalga hâlinde açılsın diye.</summary>
        const float ConfettiStep = 0.085f;

        /// <summary>Konfeti topunun ağzındaki duman ve parıltı; referansta yok.</summary>
        static readonly string[] ConfettiMute = { "Clouds", "Glow" };

        // ÖLÇÜM (patlamanın kapladığı alanın ekrana oranı):
        //   referans %1,58   bizim %0,40  → alan 3,9 kat, yani ÇAP 2 kat.
        // Yan yana konunca fark açıktı: referansın patlaması ekran
        // genişliğinin beşte birini kaplıyor, bizimki onda birini.
        const float BurstScale = 2.0f;

        /// <summary>Patlamanın rengi — referanstan örneklendi (229,219,234).</summary>
        static readonly Color BurstTint = new Color(0.898f, 0.859f, 0.918f);

        /// <summary>
        /// Bütün kutlama efektleri TEK bir zaman çizelgesinden sürülüyor.
        ///
        /// Ayrı ayrı rutinler (biri konfeti, biri roket, biri fişek) yazmak
        /// daha derli toplu görünüyordu ama sıralamayı okunamaz yapıyordu:
        /// hangi olayın hangisinden önce geldiğini görmek için üç ayrı
        /// gecikme zincirini kafada toplamak gerekiyordu. Tek çizelgede
        /// tablo neyse ekranda o oluyor.
        /// </summary>
        IEnumerator ReferenceSequence()
        {
            // Çizelge ilk harfe göre yazılı; bu rutin harfler dizildikten
            // SONRA başlıyor, yani saat zaten `ShowStarts` kadar ilerlemiş.
            float clock = ShowStarts;
            int next = 0;

            var confetti = FX.FxSkin.Get(FX.Fx.ConfettiUp);
            int confettiSent = 0;

            float last = ReferenceBursts[ReferenceBursts.Length - 1].x;
            while (clock < last + 0.05f && !_skip)
            {
                // Toplar SIRAYLA ateşleniyor, hepsi birden değil.
                //
                // ÖLÇÜM (referansın konfeti oranı): t=1,72'de %1,5 → t=2,05'te
                // %3,3 → t=2,56'da %6,6. Yani ekran yavaş yavaş doluyor.
                // Hepsini tek karede atınca bizde t=1,15'te zaten %8,8 vardı
                // ve 1,79'da %12,7'ye çıkıyordu: aynı toplam kâğıt, ama bir
                // anda gelip bir anda bitiyordu — patlayan bir kese gibi.
                // Sıraya sokmak hem tepe yoğunluğu düşürüyor hem de referansın
                // "dolmakta olan ekran" hissini veriyor.
                if (confettiSent < ConfettiCannons.Length &&
                    clock >= ConfettiAt + confettiSent * ConfettiStep)
                {
                    if (confetti != null)
                        _stage.Spawn(confetti,
                                     new Vector2(ConfettiCannons[confettiSent], -0.02f),
                                     ConfettiScale, ConfettiCount, null, ConfettiMute);
                    if (confettiSent == 0) Services.AudioService.Star();
                    confettiSent++;
                }

                // Roket izi: patlamadan `RocketRise` önce, patlama noktasına
                // doğru yükseliyor.
                while (next < ReferenceBursts.Length &&
                       clock >= ReferenceBursts[next].x - RocketRise)
                {
                    var beat = ReferenceBursts[next];
                    FX.CelebrationFX.RocketTo(_root, beat.y, beat.z, RocketRise);
                    GameKit.FX.Juice.Run(BurstAt(beat, next));
                    next++;
                }

                clock += Time.unscaledDeltaTime * _speed;   // 12. tur, W3
                yield return null;
            }
        }

        /// <summary>Roketin ucunda, ölçülen anda ve ölçülen noktada patlama.</summary>
        IEnumerator BurstAt(Vector3 beat, int index)
        {
            for (float t = 0f; t < RocketRise && !_skip; t += Time.unscaledDeltaTime * _speed)
                yield return null;
            if (_skip || _stage == null) yield break;

            // Hep AYNI prefab, hep AYNI renk: referansta patlamalar
            // birbirinin aynısı ve beyaz. Renk çeşitliliği konfetide.
            var prefab = FX.FxSkin.Get(FX.Fx.Fireworks[0]);
            if (prefab != null)
                _stage.Spawn(prefab, new Vector2(beat.y, beat.z), BurstScale, 1f, BurstTint);
        }

        /// <summary>
        /// Perdeyi ve logoyu birlikte söndürür.
        ///
        /// DERS (kıvılcımlar bu alfaya KATILMAZ): Alfa yalnız perde ve logo
        /// üstünde geziniyor; fişek ve konfeti kendi ömürlerini kendileri
        /// söndürüyor. Hepsini tek alfaya bağlamak, her karede onlarca
        /// görselin rengini yeniden yazmak demekti — üstelik yeni doğan bir
        /// kıvılcım o an sönmüş olarak başlardı.
        /// </summary>
        void SetAlpha(float alpha)
        {
            // HER PARÇA AYRI AYRI KONTROL EDİLİYOR.
            //
            // Kutlama bir SÜS ve ömrü kendi sahibinden bağımsız: hareketler
            // ortak bir çalıştırıcıda yaşıyor, dolayısıyla ekran kapanıp
            // nesneler yok edildikten sonra da bir-iki kare çalışmaya devam
            // edebiliyorlar. O anda `_curtain.color` okumak
            // `MissingReferenceException` atıyor ve rutin ölüyor — daha önce
            // bu tam olarak PERFECT kartının hiç açılmamasına yol açmıştı.
            //
            // Unity'de yok edilmiş bir nesne `== null` döndürdüğü için
            // kontrol basit; pahalı olan, yazılmadığında.
            if (_curtain != null)
            {
                var color = _curtain.color;
                color.a = alpha;
                _curtain.color = color;
            }

            if (_logoImage != null)
            {
                var ic = _logoImage.color;
                ic.a = alpha;
                _logoImage.color = ic;
            }
            // PARÇACIK YÜZEYİ DE SÖNÜYOR.
            //
            // Yüzey bir `RawImage`; perde ve logo söndüğünde o olduğu gibi
            // kalıyordu ve kutlamanın son yarım saniyesinde konfeti ANA
            // EKRANIN üstünde asılı görünüyordu. Kıvılcımlar kendi ömürlerini
            // kendileri söndürüyor ama YÜZEY onların kabı — kap sönmezse
            // içindekiler de sönmüş sayılmaz.
            if (_stage != null && _stage.Surface != null)
            {
                var sc = _stage.Surface.color;
                sc.a = alpha;
                _stage.Surface.color = sc;
            }

            // Harf harf kurulan logoda tek tek altı görselin rengini yazmak
            // yerine ortak bir grup: sönüş sırasında harfler arasında alfa
            // farkı oluşmuyor ve üst üste binen mor konturlar birbirinin
            // içinden görünmüyor.
            if (_logoGroup != null) _logoGroup.alpha = alpha;
            SetTextAlpha(_logoTop, alpha);
            SetTextAlpha(_logoBottom, alpha);
        }

        static void SetTextAlpha(TextMeshProUGUI text, float alpha)
        {
            if (text == null) return;
            var c = text.color;
            c.a = alpha;
            text.color = c;
        }

        /// <summary>
        /// Tamamlanan logoya kısa bir ölçek vuruşu. STATIC DEĞİL: hız
        /// çarpanını görmesi gerekiyor (bkz. <see cref="PopLetter"/>).
        /// </summary>
        IEnumerator Punch(RectTransform target)
        {
            const float duration = 0.26f;
            for (float t = 0f; t < duration && !_skip; t += Time.unscaledDeltaTime * _speed)
            {
                if (target == null) yield break;
                float k = t / duration;
                target.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        /// <summary>Satırın yukarıdan düşüp yaylanarak oturması.</summary>
        /// <summary>
        /// Logonun gelişi: yalnız ÖLÇEK ve ALFA — kayma yok.
        ///
        /// Kullanıcı (6. tur): "Direkt düz bir şekilde gelsin, o açılma
        /// muhabbeti olmasın."
        ///
        /// Yukarıdan düşürmek (eski `DropIn`) siyah perdede logonun nereden
        /// geldiğini belirsiz bırakıyordu; referanstaki hareket zaten
        /// merkezden BÜYÜME. Küçükten başlayıp son boyunu bir tık aşıp
        /// oturuyor — tek bir hareket, kesik kenar yok, dilim yok.
        /// </summary>
        IEnumerator LogoEntrance()
        {
            var group = _logo.GetComponent<CanvasGroup>();
            if (group == null) group = _logo.gameObject.AddComponent<CanvasGroup>();

            const float duration = 0.36f;
            for (float t = 0f; t < duration && !_skip; t += Time.unscaledDeltaTime * _speed)
            {
                float k = Mathf.Clamp01(t / duration);
                _logo.localScale = Vector3.one *
                    Mathf.Lerp(0.62f, 1f, GameKit.FX.Juice.EaseOutBack(k, 2.2f));
                group.alpha = Mathf.Clamp01(k * 2.2f);
                yield return null;
            }
            _logo.localScale = Vector3.one;
            group.alpha = 1f;
        }

        /// <remarks>
        /// STATIC DEĞİL (14. tur, W3): <see cref="PopLetter"/> ile aynı
        /// sebep — hız çarpanını göremediği için logonun iki satırı,
        /// kutlama hızlandırılsa bile eski hızda düşüyordu. Gecikme de
        /// `WaitForSecondsRealtime` ile sabitti; çarpanla bölünüyor.
        /// </remarks>
        IEnumerator DropIn(RectTransform line, float delay)
        {
            if (line == null) yield break;

            Vector2 target = line.anchoredPosition;
            for (float t = 0f; t < delay && !_skip; t += Time.unscaledDeltaTime * _speed)
                yield return null;

            const float duration = 0.34f;
            for (float t = 0f; t < duration && !_skip; t += Time.unscaledDeltaTime * _speed)
            {
                if (line == null) yield break;
                float k = GameKit.FX.Juice.EaseOutBack(t / duration, 2.4f);
                line.anchoredPosition = target + new Vector2(0f, (1f - k) * 420f);
                line.localScale = Vector3.one * (0.65f + 0.35f * k);
                yield return null;
            }

            line.anchoredPosition = target;
            line.localScale = Vector3.one;
        }
    }
}
