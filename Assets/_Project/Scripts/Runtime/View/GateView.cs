using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Kapının sahnedeki görseli: kenarın hemen dışında, duvar yüksekliğinde
    /// renkli bar + üstünde çıkış yönünü gösteren kabartmalı ok.
    ///
    /// Ölçüler referans oyuna göre: bar duvarla aynı yükseklikte ve ondan
    /// KALIN — ince bir şerit tepeden bakıldığında kaybolur ve "buradan
    /// çıkılıyor" mesajını vermez.
    /// </summary>
    public sealed class GateView : MonoBehaviour
    {

        // Kapı ölçüleri: "çerçeveyle aynı" seçiliyse kapı ÇERÇEVE BANDINI
        // birebir doldurur — referans oyunda kapı, çerçevenin renkli bir
        // parçası gibi görünür; ayrı bir çıkıntı yoktur.
        //
        // Konumlandırma: tahta kenarı ile çerçevenin dış kenarı arasındaki
        // bandın ORTASINA oturur, yani dışa kaydırma = kalınlığın yarısı.
        static bool MatchesFrame => VisualSettings.Current == null ||
                                    VisualSettings.Current.gateMatchesWall;

        /// <summary>
        /// Kapının çerçevenin dışına taşma payı — ARTIK SIFIR (10. tur).
        ///
        /// Kullanıcı: "bir tık fazla gibi, duvarın tam bitişiğine eşit denk
        /// olması lazım" (referans görseli ekledi). ÖLÇÜM: kapının görünen
        /// dış kenarı 4,182, çerçevenin görünen dış kenarı 4,161 — tam
        /// 0,02 fark, yani bu payın kendisi.
        ///
        /// Pay z-çakışmasını önlemek için konmuştu ve o gün haklıydı: kapı
        /// çerçeveyle AYNI bantta, aynı yükseklikteydi. Kapı 8. turda
        /// blokların üstüne (y≈1,12) çıkınca çerçeveyle (y 0…0,8) ortak
        /// yüzeyi kalmadı; çakışacak bir şey yokken hâlâ ödenen bir pay
        /// olarak kaldı.
        ///
        /// DERS (bir düzeltmenin gerekçesi ortadan kalkabilir): Kod
        /// doğruydu, yorumu doğruydu, sebebi artık yoktu. Bir sabiti
        /// okurken "bu neden var" kadar "bu hâlâ geçerli mi" de sorulmalı.
        /// </summary>
        const float FrameOverlapBias = 0f;

        /// <summary>
        /// Kapı çerçeveden BİR TIK YÜKSEK durur.
        ///
        /// DERS (aynı hizadaki iki yüzey tek parça okunur — 4. tur): Kapı
        /// çerçeveyle tıpatıp aynı yükseklik ve derinlikteydi; sonuç, çerçeveye
        /// SÜRÜLMÜŞ renkli bir şerit gibi görünmesiydi. Referansta kapı ayrı
        /// bir parça: çerçevenin üstünden biraz taşıyor ve dışa doğru
        /// çıkıntı yapıyor. Yükseklik farkı, "bu bir kapak" bilgisini
        /// tek başına taşıyor.
        /// </summary>
        static float BarHeight => VisualSettings.Current == null ? 0.34f
            : MatchesFrame ? BlockTop + PlateLift
                           : VisualSettings.Current.gateBarHeight;

        /// <summary>
        /// Blokların EN TEPESİ: gövde + çıtçıt. Ölçüldü (14. bölüm, gerçek
        /// mesh sınırları): 0,80 + 0,25 = 1,05.
        /// </summary>
        static float BlockTop => VisualSettings.Current == null ? 0.515f
            : VisualSettings.Current.brickHeight + VisualSettings.Current.studHeight;

        /// <summary>
        /// Kapı plakasının blokların tepesinden ne kadar yükseği.
        ///
        /// DURGUN blokların üstünde, TUTULAN bloğun altında.
        ///
        /// Bir tur önce kapı tutulan bloğun da üstüne çıkarılmıştı; kullanıcı
        /// tam tersini istedi: "blok önde gözükmeli basılı tuttuğumuzda."
        /// Doğrusu da bu — tutulan blok oyuncunun elinde, tahtadan KALKMIŞ
        /// durumda; onu bir kapının arkasına gömmek "kaldırdım" hissini
        /// bozuyor.
        ///
        /// Sıralama artık üç katlı ve her katın bir anlamı var:
        ///   durgun blok (1,05) < kapı (1,07) < tutulan blok (1,15)
        /// Yani blok tahtadayken kapının ALTINA giriyor (referanstaki gibi),
        /// elde ise her şeyin ÜSTÜNDE.
        /// </summary>
        const float PlateLift = 0.02f;

        /// <summary>Bloğun tutulduğu andaki ölçeği — tepesi bu kadar yükseliyor.</summary>
        static float DragScale => VisualSettings.Current == null ? 1.05f
            : Mathf.Max(1f, VisualSettings.Current.dragScale);

        /// <summary>
        /// Blok kenarının hücre sınırından içeri payı — kapının bloğu ne kadar
        /// örttüğü buna göre hesaplanıyor.
        /// </summary>
        static float BlockInset => VisualSettings.Current == null ? 0.055f
            : VisualSettings.Current.brickInset;

        /// <summary>
        /// Barın kenara dik derinliği — çerçeve bandından KIL PAYI dar.
        ///
        /// BULUNAN HATA: Derinlik bandın tam kalınlığıydı, yani barın iç yüzü
        /// çerçevenin iç duvarıyla TAM ÇAKIŞIYORDU. İki yüzey aynı düzlemde
        /// olunca derinlik tamponu hangisinin önde olduğuna karar veremiyor ve
        /// kapının altında ince, titreyen renkli çizgiler beliriyordu
        /// (z-fighting). Ekranda "kapının altından sızan çizgiler" gibi
        /// görünüyordu.
        ///
        /// DERS (çakışan yüzey, kararsız yüzeydir): "Tam oturması" istenen iki
        /// yüzey asla aynı düzleme konmaz; birine görünmeyecek kadar küçük bir
        /// pay verilir.
        ///
        /// KAPI DUVARIN YERİNİ TAMAMEN ALIR (5. tur, kullanıcı geri bildirimi).
        ///
        /// Kullanıcı: "kapı komple o duvarın yerini almalı, altında bir duvar
        /// parçası kalmamalı; ama oraya blok sokabiliyoruz, bu da oyunu
        /// mantıksızlaştırıyor."
        ///
        /// ÖLÇÜM — referans (Levels 1-20, 00:12, üstteki kırmızı kapı):
        /// kapı 158x55 piksel, yanındaki duvar bandı 47 piksel. Kapının ÜST
        /// kenarı duvarın üst kenarıyla aynı (1 piksel fark), ALT kenarı ise
        /// duvarın iç kenarından **7 piksel İÇERİDE** — yani kapı bandı
        /// doldurmakla kalmıyor, oyun alanına bir tık taşıyor. Hücre 79
        /// piksel → taşma hücrenin %9'u.
        ///
        /// ÖLÇÜM — bizde (düzeltmeden önce): kapı 74 piksel, duvar bandı 106
        /// piksel; kapının alt kenarı duvarın iç kenarından **33 piksel
        /// YUKARIDA**. Aradaki mor şerit ekranda duruyordu ve blok oraya
        /// giriyormuş gibi görünüyordu.
        ///
        /// DERS (bir kapı, kapattığı şeyin TAMAMINI kaplamalı): Derinlik
        /// bandın %94'üydü ve bar bandın ORTASINA konuyordu; kâğıt üzerinde
        /// "bandı dolduruyor" gibi görünse de çerçevenin iç pahı (üst yüz iç
        /// kenardan `bevelInset` kadar geride başlar) barın altında kalıyordu.
        /// Geometriyi "ortala ve biraz küçült" diye kurmak, kenar detaylarını
        /// hesaba katmıyor. Doğrusu iki UÇTAN tanımlamak: dış kenar
        /// çerçevenin dış kenarı, iç kenar oyun alanının bir tık içi.
        /// </summary>
        static float BarDepth(GateModel model) => VisualSettings.Current == null ? 0.55f
            : MatchesFrame
                ? VisualSettings.Current.frameThickness + InwardOverhang(model) + FrameOverlapBias
                : VisualSettings.Current.gateBarDepth;

        /// <summary>
        /// Kapının oyun alanına doğru taşma payı.
        ///
        /// 0.09 → 0.235 (7. tur, T65). Kullanıcı: "Üste konulan kapıların
        /// altında dış duvarın bir kısmı görünüyor. Kapı çok azıcık aşağı
        /// çekilerek alt duvar payı tamamen gizlenecek."
        ///
        /// SEBEP — PARALAKS, yerleşim değil. Kamera tepeye yakın ama TAM
        /// tepede değil (80° eğim, yani dikeyden 10°). Yerden `h` kadar
        /// yüksekteki bir yüzey ekranda `h·tan(10°) = 0,176h` kadar KUZEYE
        /// (yukarı) kaymış görünür. Çerçevemiz 0,80 hücre yüksekliğinde:
        ///   çerçevenin üst yüzü   → 0,141 hücre yukarı kayıyor
        ///   kapı (0,82 yükseklik) → 0,145 hücre yukarı kayıyor
        /// Kapı düz bir PLAKA (6. turda prizma olmaktan çıktı), yani yan yüzü
        /// yok; altındaki pah bandı 0,141 hücrelik bir şerit olarak açıkta
        /// kalıyor. ÖLÇÜLDÜ: kendi yakalamamızda 11 piksel, hücre 122 piksel
        /// → 0,09 hücre.
        ///
        /// DERS (üç boyutlu bir sahnede "üst üste" ekranda üst üste demek
        /// değildir): Kapının iç kenarı dünya koordinatında duvarın iç
        /// kenarının ÖTESİNDEYDİ; kâğıt üzerinde örtüyordu. Örtmediği yer
        /// ekranın kendisi. Bu tür bir kusuru sayı hesabıyla değil, ancak
        /// KAREYİ ÖLÇEREK bulabilirsin.
        ///
        /// KÖK SEBEP 8. TURDA BULUNDU — PAY DEĞİL, YÜKSEKLİK.
        ///
        /// Kullanıcı: "üst kısımdaki kapı kırmızı bloğun üstünde kalmış,
        /// böyle şeyler hiçbir levelde olmamalı."
        ///
        /// ÖLÇÜM (14. bölüm, gerçek mesh sınırları):
        ///   kapı plakası   y 0,820 … 0,824   (frameHeight + 0,02)
        ///   blok gövdesi   y 0 … 0,800
        ///   blok çıtçıtları        … 1,050
        ///
        /// Yani plaka, bloğun GÖVDESİ ile ÇITÇITLARININ ARASINDA kalıyordu.
        /// Komşu bloğun çıtçıtları plakanın üstüne taşıyor ve kapının iç
        /// kenarını örtüyordu; ekranda kapı kısalmış, blok da doğrudan
        /// kapıya girmiş gibi görünüyordu (araya çerçeve şeridi girmiyordu).
        ///
        /// Bu, alt/üst kapılarda ayrı ayrı denenen bütün pay değerlerinin
        /// neden yalnız bir kenarı düzelttiğini de açıklıyor: sorun payda
        /// değil, iki yüzeyin FARKLI YÜKSEKLİKTE olmasındaydı. Farklı
        /// yükseklikteki iki yüzey paralakstan farklı etkileniyor, yani
        /// aralarındaki mesafe kenara göre değişiyor.
        ///
        /// Plaka blokların tepesine çıkarılınca ikisi aynı kadar kayıyor,
        /// pay dört kenarda da aynı olabiliyor ve kapı her zaman bloğun
        /// ÜSTÜNDE çiziliyor — referanstaki gibi, blok kapının altına
        /// giriyor.
        ///
        /// DERS (bir kusuru dört kez düzeltiyorsan, düzelttiğin şey kusur
        /// değildir): Pay üç turda üç kez değiştirildi ve her seferinde
        /// bir kenar düzelip öteki bozuldu. Kenara göre değişen bir
        /// düzeltmeye ihtiyaç duyman, ölçtüğün büyüklüğün yanlış olduğunun
        /// işareti.
        ///
        /// --- 7. turun (artık geçersiz) gerekçesi aşağıda ---
        ///
        /// BULUNAN HATA: Yukarıdaki hesap doğruydu ama YALNIZ ÜST kenar için.
        /// Paralaks her zaman KUZEYE (yukarı) kaydırıyor; "içeri" yönü ise
        /// kenara göre değişiyor:
        ///   • ÜST kenarda içeri = güney → kayma payı YER, telafi için pay
        ///     BÜYÜMELİ  →  0.09 + 0.145 = 0.235
        ///   • ALT kenarda içeri = kuzey → kayma payı EKLER, pay KÜÇÜLMELİ
        ///     →  0.09 − 0.145 = −0.055 (kapı tahtanın dışında bitiyor ama
        ///        ekranda 0.09 hücre içeride görünüyor)
        ///   • YAN kenarlarda içeri = X ekseni; paralaks Z'de olduğu için
        ///     ikisi birbirine karışmıyor → düz 0.09
        ///
        /// 0.235'i her kenara vermek alt kapıları tahtanın 0,38 hücre içine
        /// sokuyordu (0,235 pay + 0,145 kayma) ve kapı plakası bloğun alt
        /// saplama sırasının üstünü örtüyordu. Eski 0,09 değerinde bile alt
        /// kapılar 0,235 hücre örtüyordu — yani bu hata KISMEN zaten vardı,
        /// 7. turdaki değişiklik onu görünür eşiğin üstüne çıkardı.
        ///
        /// DERS (bir düzeltmenin YÖNÜ vardır): Ölçüm tek bir kenarda
        /// yapıldığında bulunan sayı o kenara özeldir. Simetrik görünen bir
        /// geometride bile, kameranın kırdığı simetriyi hesaba katmadan
        /// değeri dört kenara birden uygulamak bir kenarı düzeltirken
        /// karşısındakini iki katı bozuyor.
        /// </summary>
        static float InwardOverhang(GateModel model)
        {
            // Referansta kapı oyun alanına hücrenin %9'u kadar taşıyor.
            const float visible = 0.09f;

            // Pay artık kenara göre DEĞİŞMİYOR — çünkü plaka blokla aynı
            // yükseklikte. İki yüzey aynı yükseklikteyse paralaks ikisini de
            // aynı kadar kaydırıyor, yani aradaki mesafe ekranda da aynı
            // kalıyor. Kalan tek düzeltme bloğun kendi kenar payı.
            //
            // (0,02 birimlik yükseklik farkının payı 0,0035 hücre; ölçüm
            // hassasiyetinin altında, o yüzden hesaba katılmıyor.)
            // PAY YALNIZ KUZEY KENARDA (10. tur).
            //
            // Kullanıcı: "alttaki kapı bir tık daha önde duruyor, duvarın
            // başlangıcına doğru geri gitmeli, aynı hizada başlayıp aynı
            // hizada bitmeli."
            //
            // ÖLÇÜM: alt kapı 897..948, çerçeve bandı 905..949 — kapı bandın
            // 8 piksel ÜSTÜNDEN başlıyordu. 0,122 hücre × 77 = 9,4 piksel,
            // yani payın kendisi.
            //
            // O pay kuzey kenarda GEREKLİ (orada duvarın iç yüzü kameraya
            // dönük ve örtülmesi gerekiyor), diğer üç kenarda GEREKSİZ:
            // o yüzler kameradan kaçık ya da profilden, örtülecek bir şey
            // yok. Geriye yalnız bloğun kendi kenar payı kalıyor.
            // PAY DÖRT KENARDA DA ÇERÇEVENİN PAHI (10. tur, son).
            //
            // Kullanıcı iki şey söyledi ve ikisi aynı yere çıktı:
            //   • "alttaki kapının arkasında çok ufak bir boşluk var"
            //   • "üst kapının alt tarafı bloğun üstünde kalmış, bunu
            //      istemiyoruz"
            //
            // `BoardFrameMeshBuilder` üst halkayı pah kadar kaydırıyor, yani
            // çerçevenin ÜST YÜZÜ oyun alanına 0,09 hücre giriyor. Kapının iç
            // kenarı tam oraya oturmalı: daha az olursa çerçevenin üst yüzü
            // açıkta kalıyor (alttaki boşluk), daha çok olursa kapı blokların
            // üstüne taşıyor (üstteki sorun).
            //
            // KUZEYDEKİ FAZLADAN PAY KALDIRILDI. Bir tur önce oraya duvarın
            // iç yüzünü örtsün diye `frameHeight × skew` eklenmişti ve duvar
            // gerçekten kapanmıştı — ama o iç yüz, ekranda üst sıradaki
            // bloğun çıtçıtlarının düştüğü bölgenin TA KENDİSİ. Aynı pikselleri
            // hem duvarla hem blokla paylaşmak mümkün değil; biri örtülünce
            // öteki örtülüyor.
            //
            // Yükseklikle ayırmak da mümkün değil: çerçeve 0,80, bloğun
            // gövdesi de 0,80. Aralarında kapıyı sokacak bir kat yok.
            //
            // Referans da bu tarafı seçmiş: `f0001` karesinde üst kapının
            // altında duvar bandı GÖRÜNÜYOR (21 piksel). Yani duvarın bir
            // kısmının görünmesi kusur değil, tasarımın kendisi.
            //
            // DERS (iki istek aynı pikseli paylaşıyorsa biri seçilmek
            // zorunda): "Duvarı kapla" ile "bloğun üstüne binme" bağımsız
            // iki ayar gibi duruyordu; ekranda aynı şeridi işaret ettikleri
            // anlaşılınca seçim kaçınılmaz oldu. Böyle bir çakışmayı erken
            // görmek, iki turluk gidip gelmeyi baştan keserdi.
            return VisualSettings.Current != null
                ? VisualSettings.Current.frameBevel : visible;
        }


        /// <summary>
        /// Kameranın dikeyden sapması: `tan(90° − 80°)`. Yerden `h` yükseklikteki
        /// bir yüzey ekranda `h · CameraSkew` kadar kuzeye kaymış görünür.
        /// Eğim <c>GameSession.FitCamera</c>'da 80° olarak sabit.
        /// </summary>
        const float CameraSkew = 0.1763f;

        /// <summary>
        /// Barın merkezi, tahta kenarından DIŞA doğru bu kadar uzakta:
        /// çerçeve bandının TAM ORTASI.
        ///
        /// ÖLÇÜM (Levels 1-20 yürüyüşü, 00:12 karesi): üstteki kırmızı kapının
        /// dış kenarı çerçevenin dış kenarıyla AYNI hizada, iç kenarı da tahta
        /// zemininin başladığı yerde. Yani kapı bandı doldurur, dışarı TAŞMAZ.
        /// Kapıyı dışarı çıkarma denemesi (kalınlığın %66'sı) onu çerçeveye
        /// yapıştırılmış bir dil gibi gösterdi; ayrımı yapan şey konum değil,
        /// YÜKSEKLİK farkı.
        ///
        /// Merkez artık iki UÇTAN hesaplanıyor (bkz. <see cref="BarDepth"/>):
        /// iç kenar `-InwardOverhang`, dış kenar `frameThickness + bias`;
        /// merkez ikisinin ortası.
        /// </summary>
        static float OutwardOffset(GateModel model) => VisualSettings.Current == null ? 0.275f
            : MatchesFrame
                ? (VisualSettings.Current.frameThickness + FrameOverlapBias
                   - InwardOverhang(model)) * 0.5f
                : VisualSettings.Current.gateOutwardOffset;

        MeshRenderer _renderer;
        Material _colorMaterial;
        TMPro.TextMeshPro _iceCounter;
        GateModel _model;
        GameObject _arrow;

        /// <summary>Temas çizgisindeki parlama şeridi (bkz. PlayAbsorbFlash).</summary>
        MeshRenderer _mouthLight;
        Material _mouthMaterial;
        Coroutine _mouthFade;

        public static GateView Create(
            Transform parent, GateModel model, BoardSpace space, Material colorMaterial,
            BlockOut.Runtime.Config.ColorPaletteSO palette = null)
        {
            var go = new GameObject($"Gate_{model.ActiveColor}_{model.Side.ToId()}");
            go.transform.SetParent(parent, worldPositionStays: false);
            // Kapı barının kenar boyunca kapladığı aralık — açıklığın tamamı.
            ResolveSpan(model, out float barMin, out float barMax);

            float spanCenter = (barMin + barMax) * 0.5f;
            float barLength = Mathf.Max(0.25f, barMax - barMin);
            float offCoord = model.EdgeCoord + model.OutwardSign * OutwardOffset(model);

            Vector3 center;
            float alongX;   // barın X eksenindeki uzunluğu
            float alongZ;
            if (model.EdgeHorizontal)
            {
                center = space.CornerToWorld(spanCenter, offCoord, 0f);
                alongX = barLength; alongZ = BarDepth(model);
            }
            else
            {
                center = space.CornerToWorld(offCoord, spanCenter, 0f);
                alongX = BarDepth(model); alongZ = barLength;
            }

            // KAPI ARTIK KESKİN BİR KÜP DEĞİL (4. tur, G20/G26).
            //
            // Kullanıcı: "Kapılar köşelerden taşıyor... Kapılar duvarla iç içe
            // geçmeyecek."
            //
            // DERS (keskin bir kutu, yuvarlak bir çerçevenin içine sığmaz):
            // Bar `PrimitiveType.Cube` idi. Tahtanın köşesine dayanan bir kapı
            // (1., 2., 4., 5. bölümler) çerçevenin yuvarlak köşesinin DIŞINA
            // taşıyor ve sivri bir dilim olarak sırıtıyordu. Referansta her
            // kapı yuvarlak köşeli bir plastik parça; köşeye dayandığında
            // yayı çerçevenin yayını izliyor. Silüeti düzeltmek, barı
            // kısaltmaktan (yani açıklığı hakkında yalan söylemekten) iyi.
            Vector3 outwardDir = model.EdgeHorizontal
                ? new Vector3(0f, 0f, -model.OutwardSign)
                : new Vector3(model.OutwardSign, 0f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh =
                BuildBarMesh(alongX, alongZ, BarHeight, outwardDir);
            go.AddComponent<MeshRenderer>();

            // PARALAKS TELAFİSİ — TEK YERDE, TEK YÖNDE (9. tur).
            //
            // Kapı plakası blokların tepesinde, y = 1,07'de duruyor. Kamera
            // dikeyden 10° eğik olduğu için yerden `h` yükseklikteki bir yüzey
            // ekranda `h · tan(10°)` kadar KUZEYE kaymış görünür — bizim
            // plakada 1,07 × 0,1763 = **0,189 hücre**.
            //
            // ÖLÇÜM (`level_003` gerçek mesh sınırları): kapının dünya
            // koordinatındaki iç kenarı z=3,378, yani oyun alanının 0,122
            // İÇİNDE. Ekranda ise 0,18 hücre DIŞINDA görünüyordu. Aradaki
            // 0,3 hücrenin tamamı bu kayma.
            //
            // DERS (bir kusuru dört kez düzeltiyorsan, düzelttiğin şey kusur
            // değildir — bu dosyanın kendi notu): Önceki üç tur bunu
            // `InwardOverhang`'i kenara göre büyütüp küçülterek kapatmaya
            // çalıştı ve her seferinde bir kenar düzelip öteki bozuldu.
            // Sebebi şu: kayma DÜNYADA her kenarda aynı yöne (+z) oluyor,
            // ama "içeri" yönü üst kenarda −z, alt kenarda +z. Payla telafi
            // etmek, tek bir çevirmeyi iki ayrı işaretle taklit etmeye
            // çalışmak demekti. Doğrusu kaymayı olduğu yerde, yani KONUMDA
            // ve tek yönde geri almak.
            //
            // TELAFİ KAPININ TAM YÜKSEKLİĞİ DEĞİL, ÇERÇEVEYLE ARASINDAKİ FARK.
            //
            // İlk deneme `BarHeight × skew` (0,189) kullandı ve üst kapıyı
            // düzeltirken alt kapıları çerçevenin dışına taşırdı — yani üç
            // turdur yaşanan "birini düzelt, öbürünü boz" tam olarak
            // tekrarlandı. Sebebi: ÇERÇEVE DE kayıyor. Kapı çerçeveye göre
            // hizalanacaksa telafi edilmesi gereken şey mutlak kayma değil,
            // İKİSİ ARASINDAKİ FARK:
            //
            //   (1,07 − 0,80) × 0,1763 = 0,048 hücre
            //
            // Bu değer iki kenarda da doğru sonuç veriyor, çünkü hem kapı hem
            // çerçeve aynı yöne kayıyor ve geriye yalnız yükseklik farkı
            // kalıyor. Hesap (üst kenar / alt kenar, görünen konumlar):
            //   kapı dış kenarı  4,181 / −3,899   çerçeve dışı  4,161 / −3,879
            //   kapı iç kenarı   3,519 / −3,237   çerçeve içi   3,641 / −3,359
            // yani kapı iki kenarda da çerçeveden 0,02 taşıyor ve oyun
            // alanına 0,122 hücre giriyor. Referansta ölçülen 0,27 × duvar =
            // 0,14 hücre; aradaki fark ölçüm hassasiyetinin içinde.
            //
            // DERS (paralaksı telafi ederken NEYE göre hizaladığını sor):
            // Kayma mutlak bir büyüklük değil; iki nesne aynı kadar kayıyorsa
            // aralarındaki mesafe hiç değişmiyor. Telafi edilecek olan farktır.
            center.z -= (BarHeight - VisualSettings.Current.frameHeight) * CameraSkew;
            go.transform.position = center;

            // KOYU KENAR KALDIRILDI — ÖLÇÜM ONU YALANLADI (4. tur, G26 revizyon).
            //
            // G26 için barın bir tık büyütülmüş KOYU kopyası eklenmişti; amaç
            // kapı ile çerçeve arasındaki sınırı çizmekti. İki şeyi birden
            // bozdu:
            //   1) Aktif kapının çevresinde ekranda GRİ bir hale bıraktı.
            //   2) Kapı sönüp saydamlaşırken o kopya kapanmıyordu, yani rengi
            //      tükenen kapının yerinde GRİ bir dikdörtgen kalıyordu.
            //
            // ÖLÇÜM (Levels 1-20, 00:12 karesi, kırmızı kapının sol kenarında
            // yatay tarama): çerçeve #4238A3 → kenarında iki piksel açık mor
            // (#5348B8, çerçevenin kendi ışığı) → **2 PİKSEL** koyu mor
            // (#241C70) → kapının koyu kırmızısı → kapı. Yani referanstaki
            // ayrım 2 piksel; benim kopyam her kenarda 0,045 hücre = ~6 piksel
            // ve neredeyse siyahtı.
            //
            // DERS (bir sınırı GÖRÜNÜR kılmak ile KALIN yapmak aynı şey
            // değil): Referansta o 2 pikseli üreten şey ayrı bir katman değil,
            // kapının KENDİ yan yüzü — kapı çerçeveden %14 yüksek olduğu için
            // kendi gölgesini düşürüyor. Yükseklik farkı zaten bizde de var;
            // eklenen kopya gereksizdi ve zararlıydı.
            var view = go.AddComponent<GateView>();
            view._model = model;
            view._renderer = go.GetComponent<MeshRenderer>();
            view._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view._colorMaterial = colorMaterial;
            view._arrow = CreateArrow(parent, model, center, palette);
            view.BuildMouthLight(barLength, model, outwardDir, palette);
            view.BuildWallFace(space, model, barMin, barMax, colorMaterial);

            if (model.IsIced)
            {
                // Buz rengi GİZLER (video kuralı) — bar buz materyaliyle başlar.
                view._renderer.sharedMaterial = ViewKit.GateIce;
                // Donmuş KAPININ sayacı krem: kapının buzu bloğunkinden çok
                // daha soluk, rakam da onun açık tonunda.
                //
                // Yükseklik çizicinin KENDİ sınırından; sabit hesap barın
                // gerçek tepesini ıskalayıp rakamı buzun içine gömüyordu
                // (bkz. BlockView'daki aynı ders).
                // DONMUŞ KAPIDA OK YOK, SAYI VAR.
                //
                // Referans (`menus,powerups,vs.mp4` 01:25 ve 01:47): buzlu
                // kapıların üstünde yalnız sayaç duruyor; ok ancak buz
                // kırılınca beliriyor. Bizde ikisi birden çiziliyordu ve ok
                // sayacın ÜSTÜNDE kalıyordu (ölçüm: ok y=0.97, sayaç y=0.90),
                // yani rakamın yarısını kapatıyordu.
                //
                // DERS (aynı yere iki şey koyma): Buz zaten "bu kapı şu an
                // kullanılamaz" diyor; ok ise "buradan çıkabilirsin" diyor.
                // İkisi aynı anda doğru olamaz. `BlockView` donmuş blokta
                // eksen okunu zaten gizliyordu; kapıda bu adım atlanmıştı.
                if (view._arrow != null) view._arrow.SetActive(false);

                float barTop = view._renderer.bounds.max.y;
                view._iceCounter = ViewKit.CreateCounter(
                    parent, new Vector3(center.x, barTop + 0.08f, center.z), model.IceCount,
                    ViewKit.CounterStyle.Frost);
            }
            else
            {
                view._renderer.sharedMaterial = colorMaterial;
            }

            return view;
        }

        /// <summary>
        /// Kapı barının mesh'i: yuvarlak köşeli, üst kenarı pahlı alçak prizma.
        ///
        /// Ölçüler bloklarla AYNI dilden geliyor: köşe yarıçapı ve pah,
        /// <see cref="Config.BlockVisualConfigSO"/>'daki tuğla değerlerinin
        /// aynısı. Referansta kapı ile blok aynı malzemeden dökülmüş gibi
        /// duruyor; iki ayrı yuvarlaklık kullanmak o birliği bozardı.
        /// </summary>
        /// <summary>
        /// Kapı barı — TEK PARÇA, dikey degradeli.
        ///
        /// Kullanıcı (6. tur): "Kapılarda ciddi problem var, son hali çok
        /// kötüye gitmiş."
        ///
        /// TEŞHİS (ölçümle): Referans kapının dikey profili KESİNTİSİZ bir
        /// degrade —
        ///   üst kenar  (144, 17, 35)   koyu hat
        ///   parlama    (255,112,102)   ince açık şerit
        ///   gövde üstü (255, 43, 45)
        ///   gövde altı (246, 26, 27)   yavaş koyulaşma
        ///   alt kenar  (119,  5,  6)   koyu hat
        /// Bizimki ise ÜÇ DÜZ BANT ve aralarında sert basamaklar:
        ///   (255,0,0) → (248,0,0) → (211,0,0)
        ///
        /// SEBEP: Bar mesh'i hiç KÖŞE RENGİ yazmıyordu. Malzeme
        /// `BlockOut/Brick`, köşe rengini çarpan olarak kullanıyor; renk
        /// olmayınca Unity beyaz varsayıyor ve tüm yüzeyler aynı tonda
        /// çıkıyor. Ekrandaki üç bandı ayıran tek şey NORMAL'lerin ışığı
        /// farklı açıyla alması — o da düz basamaklar üretiyor.
        ///
        /// DERS (bir malzemenin beklediği veriyi vermezsen sessizce düzleşir):
        /// Tuğla mesh'i bu köşe renklerini baştan beri yazıyordu ve bloklar
        /// bu yüzden hacimli görünüyordu. Kapı aynı malzemeyi kullanıyor ama
        /// aynı veriyi vermiyordu; eksik olan shader değil, mesh'in kendisiydi.
        ///
        /// <paramref name="outward"/> barın DIŞA bakan yönü: degrade o yöne
        /// doğru açılıyor, çünkü referansta parlama dış kenarda.
        /// </summary>
        /// <summary>
        /// Kapı barı — DÜZ İKİ KATLI PLAKA (6. tur, yeniden yazıldı).
        ///
        /// Kullanıcı iki görsel gönderdi: bizimki ve olması gereken. Referans
        /// kapı bir prizma gibi okunmuyor; düz bir plaka: çepeçevre ince KOYU
        /// bir kenar, içinde dikey degradeli parlak bir yüz.
        ///
        /// ÜÇ DENEME, ÜÇ BAŞARISIZLIK:
        ///   1. Prizmaya köşe rengi verildi → üç düz bant, sert basamaklar.
        ///   2. Pah genişletilip ton rampası kuruldu → basamak yumuşadı ama
        ///      "ikinci kütle" görüntüsü kaldı.
        ///   3. Üst kapak halka + kapak diye ikiye bölündü → halka, yuvarlak
        ///      köşelerde kendi kendini kesti ve dört köşede ok biçimli
        ///      kırıklar çıktı (dışa doğru kaydırma, yarıçaptan büyük olunca
        ///      komşu noktalar birbirinin üstünden geçiyor).
        ///
        /// DERS (biçimi taklit etmek yerine YAPIYI kur): Üç denemenin ortak
        /// hatası, referansa benzemeyen bir gövdeyi (prizma) gölgelendirerek
        /// benzetmeye çalışmaktı. Referansın gerçekte yaptığı şey basit: iki
        /// düz katman. Kamera zaten tepeden bakıyor, üçüncü boyut kimseye
        /// görünmüyor.
        ///
        /// Katmanlar AYRI ÜÇGENLENİYOR (aralarında şerit yok): kaydırma
        /// yarıçaptan büyük olsa bile kendi kendini kesme ihtimali kalmıyor.
        /// </summary>
        static Mesh BuildBarMesh(float sizeX, float sizeZ, float height, Vector3 outwardDir)
        {
            var cfg = VisualSettings.Current;
            float radius = cfg != null ? cfg.brickCornerRadius : 0.16f;

            // ÖLÇÜM (Levels 1-20, 00:12): kapının sol ucu y 350..401 boyunca
            // SABİT x=338 — uç düz, köşedeki kıvrım 4-5 piksel / 80 piksellik
            // hücre = ~%6.
            radius = Mathf.Min(radius * 0.45f, Mathf.Min(sizeX, sizeZ) * 0.22f);

            // ÖLÇÜM: kenar kalınlığı ~2 piksel / 79 piksellik hücre = %2,5.
            float border = Mathf.Min(0.026f, Mathf.Min(sizeX, sizeZ) * 0.10f);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            // Referans profilinden: yüzün dış kenarı (255,43,45), iç kenarı
            // (246,26,27), çepeçevre kenar (119,5,6).
            const float ToneEdge = 0.52f;
            const float ToneFaceOuter = 1.06f;
            const float ToneFaceInner = 0.90f;

            float span = Mathf.Max(0.001f,
                Mathf.Abs(outwardDir.x) * sizeX + Mathf.Abs(outwardDir.z) * sizeZ);

            void Plate(float halfX, float halfZ, float r, float y,
                       System.Func<Vector2, Color> tone)
            {
                var loop = RoundedRect(halfX, halfZ, r);
                int start = verts.Count;
                foreach (var q in loop)
                {
                    verts.Add(new Vector3(q.x, y, q.y));
                    normals.Add(Vector3.up);
                    colors.Add(tone(q));
                }
                BrickSilhouette.Triangulate(loop, tris, start, faceUp: true);
            }

            Color Tone(float t) => new Color(t, t, t, 1f);

            // 1) KOYU KENAR: tam boy plaka.
            Plate(sizeX * 0.5f, sizeZ * 0.5f, radius, height, _ => Tone(ToneEdge));

            // 2) PARLAK YÜZ: kenar kadar içeride, bir tık yukarıda
            //    (z-fighting olmasın), dikey degradeli.
            Plate(sizeX * 0.5f - border, sizeZ * 0.5f - border,
                  Mathf.Max(0.01f, radius - border * 0.5f), height + 0.004f,
                  q =>
                  {
                      float t = Mathf.Clamp01(
                          0.5f + (q.x * outwardDir.x + q.y * outwardDir.z) / span);
                      return Tone(Mathf.Lerp(ToneFaceInner, ToneFaceOuter, t));
                  });

            var mesh = new Mesh { name = "GateBar" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }
        static List<Vector2> RoundedRect(float halfX, float halfZ, float radius)
        {
            const int segments = 4;
            var points = new List<Vector2>((segments + 1) * 4);
            var centers = new[]
            {
                new Vector2(halfX - radius, -halfZ + radius),
                new Vector2(halfX - radius, halfZ - radius),
                new Vector2(-halfX + radius, halfZ - radius),
                new Vector2(-halfX + radius, -halfZ + radius)
            };
            float[] start = { -90f, 0f, 90f, 180f };

            for (int c = 0; c < 4; c++)
                for (int s = 0; s <= segments; s++)
                {
                    float angle = (start[c] + s / (float)segments * 90f) * Mathf.Deg2Rad;
                    points.Add(centers[c] +
                        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
            return points;
        }

        /// <summary>
        /// Barın kenar boyunca kaplayacağı aralığı verir: açıklığın TAMAMI,
        /// iki ucundan yalnız kıl payı çıkarılmış hâli.
        ///
        /// Eski hâli köşeye dayanan ucu köşe yarıçapı kadar içeri çekiyordu;
        /// gerekçesi barın çerçevenin yuvarlak köşesine girmesiydi. Ölçüm o
        /// çözümün bedelini gösterdi (aşağıya bakın): kapı açıklığından %25
        /// dar çiziliyordu. Kozmetik kusur geri geldi, işlevsel yalan gitti.
        /// </summary>
        static void ResolveSpan(GateModel model, out float min, out float max)
        {
            min = model.SpanMin;
            max = model.SpanMax;

            // KAPI TAM AÇIKLIĞINI KAPLAR — kırpılmaz.
            //
            // Eskiden tahta köşesine dayanan uç `clearance` kadar içeri
            // alınıyordu ve o pay `min(köşeYarıçapı * 0.75, açıklık * 0.35)`
            // idi. 2 hücrelik bir kapıda bu **0.45 hücre** ediyor: kapı
            // ekranda 2 değil 1.5 hücre çiziliyordu.
            //
            // DERS (kapının genişliği bir SÖZDÜR): Kapının açıklığı oyuncuya
            // "bu genişlikte bir blok buradan geçer" der. Barı kısaltmak o
            // sözü bozuyor — 2 hücrelik blok 2 hücrelik kapının önünde
            // "sığmayacak" gibi görünüyordu ve oyuncu hizalamayı yanlış
            // sanıyordu. Referans ölçüldü (`menus,powerups,vs.mp4` 01:25):
            // 2 hücrelik kapı 95 piksel, hücre 46,4 piksel — tam 2 hücre,
            // hiç kırpma yok.
            //
            // Kırpma KOZMETİK bir sorunu çözmek için konmuştu: köşeye dayanan
            // ucun çerçevenin yuvarlak köşesinin içine girmesi. Ama kozmetik
            // bir kusuru, işlevsel bir yalanla değiştirmek kötü bir takas.
            // Köşeye dayanan kapı çerçeve yayına değebilir; bu, kısa bir
            // bardan çok daha küçük bir günah.
            //
            // Kalan pay yalnız KOMŞU KAPILARI ayırmak için, kıl payı:
            // yan yana iki kapı aynı renkteyse tek parça gibi okunuyordu.
            const float SeamInset = 0.02f;
            min += SeamInset;
            max -= SeamInset;
        }

        /// <summary>
        /// Kapının üstündeki ok: düz üçgen değil ALÇAK PRİZMA. Düz üçgen tek
        /// renk kalır ve "detaysız" görünür; prizmanın yan yüzleri ışığı farklı
        /// açıyla aldığı için kenarları belirginleşir.
        /// </summary>
        static GameObject CreateArrow(Transform parent, GateModel model, Vector3 barCenter,
                                      BlockOut.Runtime.Config.ColorPaletteSO palette)
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(parent, worldPositionStays: false);

            var cfg = VisualSettings.Current;
            float size = cfg != null ? cfg.arrowSize : 0.20f;
            float rise = cfg != null ? cfg.arrowRise : 0.055f;
            float cornerRadius = cfg != null ? cfg.arrowCornerRadius : 0.3f;

            // Dışarı yönü: yatay kapılarda ±Z, dikey kapılarda ±X.
            Vector3 forward = model.EdgeHorizontal
                ? new Vector3(0f, 0f, -model.OutwardSign)
                : new Vector3(model.OutwardSign, 0f, 0f);
            Vector3 across = new Vector3(-forward.z, 0f, forward.x);

            // OK BASIK, KARE DEĞİL (5. tur, kullanıcı geri bildirimi).
            //
            // Kullanıcı: "kapıların üzerindeki ok işareti kapıdan taşmış gibi
            // gözüküyor; ok kapıyı tam ortalasın, biraz daha küçük olsun."
            //
            // ÖLÇÜM (Levels 1-20, 00:12, üstteki kırmızı kapının beyaz oku):
            // ok 27x15 piksel, hücre 79 piksel → GENİŞLİK hücrenin %34'ü,
            // DERİNLİK %19'u. Oran 1,8 — yani ok geniş ve BASIK.
            //
            // Eski katsayılarla (tip 1,0 / taban 0,48) toplam derinlik
            // 1,48·size, genişlik 1,56·size idi: oran 1,05, neredeyse eşkenar.
            // 0,30'luk `arrowSize` ile derinlik 0,44 hücre çıkıyordu — kapının
            // kendi derinliğinin (0,49) neredeyse tamamı. Taşmış görünmesinin
            // sebebi buydu.
            //
            // Yeni katsayılar: genişlik 1,56·size, derinlik 0,87·size.
            // `arrowSize = 0.218` ile genişlik 0,34 ve derinlik 0,19 —
            // referansın ölçülen oranları.
            //
            // DERS (bir şekli tek sayı ile küçültmek şeklini düzeltmez):
            // "Ok büyük" denince ilk refleks `arrowSize`ı kısmak. Ama sorun
            // boyut değil ORANDI; küçültmek oku kapıya sığdırırdı ama yine
            // eşkenar, yani referanstakinden başka bir şekil kalırdı.
            Vector3 tip = forward * size * 0.59f;
            Vector3 left = across * size * 0.95f - forward * size * 0.28f;
            Vector3 right = -across * size * 0.95f - forward * size * 0.28f;

            // Referans oyunda okun köşeleri YUMUŞAK; keskin üçgen sert ve
            // "vektör klibi" gibi duruyor. Her köşeyi küçük bir yay ile
            // yuvarlıyoruz (köşe kesme + ara noktalar).
            var outline = RoundedTriangle(tip, left, right, cornerRadius);

            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            float top = rise;

            // Üst yüz: merkezden yelpaze.
            Vector3 center = (tip + left + right) / 3f;
            int capCenter = verts.Count;
            verts.Add(center + Vector3.up * top);
            normals.Add(Vector3.up);
            colors.Add(Color.white);

            int capStart = verts.Count;
            foreach (var point in outline)
            {
                verts.Add(point + Vector3.up * top);
                normals.Add(Vector3.up);
                colors.Add(Color.white);
            }
            for (int i = 0; i < outline.Count; i++)
            {
                int a = capStart + i;
                int b = capStart + (i + 1) % outline.Count;
                tris.Add(capCenter); tris.Add(b); tris.Add(a);
            }

            // Yan yüzler: kabartma hissi (düz üçgen tek renk kalıyordu).
            for (int i = 0; i < outline.Count; i++)
            {
                Vector3 a = outline[i];
                Vector3 b = outline[(i + 1) % outline.Count];
                Vector3 edge = (b - a).normalized;
                Vector3 outward = Vector3.Cross(Vector3.up, edge).normalized;

                int start = verts.Count;
                verts.Add(a); verts.Add(b);
                verts.Add(b + Vector3.up * top); verts.Add(a + Vector3.up * top);
                for (int n = 0; n < 4; n++) normals.Add(outward);
                colors.Add(new Color(0.66f, 0.66f, 0.66f));
                colors.Add(new Color(0.66f, 0.66f, 0.66f));
                colors.Add(Color.white); colors.Add(Color.white);

                tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
                tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
            }

            var mesh = new Mesh { name = "GateArrow" };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            // OKU SARAN KOYU HALKA (6. tur).
            //
            // Kullanıcının gönderdiği referansta ok, kapının koyu tonundan
            // bir halkanın içinde oturuyor — kapağa GÖMÜLMÜŞ bir düğme gibi.
            // Bizde ok doğrudan kırmızının üstündeydi ve "yapıştırılmış"
            // duruyordu.
            //
            // ÖLÇÜM (00:12): halka ~2,4 piksel / 79 piksellik hücre = %3.
            // DİKKAT: `outline` bir List<Vector3> ve noktalar XZ düzleminde
            // (y = 0). Vector2'ye kopyalamak Z'yi düşürüp halkayı tek bir
            // çizgiye indirdi — ölçüm: halkanın Z uzanımı 0,00 çıkıyordu ve
            // ekranda hiç görünmüyordu.
            //
            // DERS (boyut düşürmek sessiz bir veri kaybıdır): `Vector3` →
            // `Vector2` dönüşümü derleyicide hata vermez, çalışma anında da
            // patlamaz; yalnız mesh boş çıkar. Aynı düzlemde çalışan iki tip
            // arasında gidip gelirken hangi eksenin düştüğünü bilmek gerekiyor.
            var ringOutline = new List<Vector2>(outline.Count);
            for (int i = 0; i < outline.Count; i++)
            {
                var d = new Vector2(outline[i].x, outline[i].z);
                float len = d.magnitude;
                ringOutline.Add(len > 1e-4f ? d + d / len * (size * 0.30f) : d);
            }
            BrickSilhouette.MakeCounterClockwise(ringOutline);

            var ringGo = new GameObject("ArrowRing");
            ringGo.transform.SetParent(go.transform, worldPositionStays: false);
            ringGo.transform.localPosition = new Vector3(0f, -0.005f, 0f);
            var ringVerts = new List<Vector3>(ringOutline.Count);
            var ringNormals = new List<Vector3>(ringOutline.Count);
            foreach (var q in ringOutline)
            {
                ringVerts.Add(new Vector3(q.x, 0f, q.y));
                ringNormals.Add(Vector3.up);
            }
            var ringTris = new List<int>();
            BrickSilhouette.Triangulate(ringOutline, ringTris, 0, faceUp: true);
            var ringMesh = new Mesh { name = "ArrowRing" };
            ringMesh.SetVertices(ringVerts);
            ringMesh.SetNormals(ringNormals);
            ringMesh.SetTriangles(ringTris, 0);
            ringMesh.RecalculateBounds();
            ringGo.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            var ringRenderer = ringGo.AddComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = ViewKit.GateArrowRing(palette, model.ActiveColor);
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ViewKit.ArrowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Bar artık TABANINDAN konumlanıyor (mesh y=0'dan başlıyor), bu
            // yüzden ok barın TAM YÜKSEKLİĞİ kadar kaldırılıyor; eskiden küpün
            // merkezine göre yarım yükseklikti.
            // Ok, parlak yüzün (BarHeight + 0,004) ÜSTÜNDE olmalı; yoksa
            // altındaki koyu halka yüzün içinde kalıp hiç görünmüyor.
            go.transform.position = barCenter + Vector3.up * (BarHeight + 0.012f);
            return go;
        }

        /// <summary>
        /// Üçgenin köşelerini yuvarlatıp kapalı bir dış çizgi noktası listesi verir.
        /// <paramref name="radius01"/> 0 = keskin üçgen, 1 = maksimum yumuşama.
        /// </summary>
        static List<Vector3> RoundedTriangle(Vector3 a, Vector3 b, Vector3 c, float radius01)
        {
            var corners = new[] { a, b, c };
            var outline = new List<Vector3>();

            if (radius01 <= 0.001f)
            {
                outline.AddRange(corners);
                return outline;
            }

            const int arcSegments = 4;
            for (int i = 0; i < 3; i++)
            {
                Vector3 prev = corners[(i + 2) % 3];
                Vector3 corner = corners[i];
                Vector3 next = corners[(i + 1) % 3];

                // Köşeye komşu kenarlar boyunca içeri kaçış noktaları.
                float cut = Mathf.Clamp01(radius01) * 0.42f;
                Vector3 from = Vector3.Lerp(corner, prev, cut);
                Vector3 to = Vector3.Lerp(corner, next, cut);

                for (int s = 0; s <= arcSegments; s++)
                {
                    float t = s / (float)arcSegments;
                    // Köşeyi kontrol noktası kabul eden ikinci derece Bézier:
                    // yay gibi yumuşak bir geçiş verir, trigonometri gerekmez.
                    Vector3 p = Vector3.Lerp(
                        Vector3.Lerp(from, corner, t),
                        Vector3.Lerp(corner, to, t), t);
                    outline.Add(p);
                }
            }
            return outline;
        }

        /// <summary>
        /// Kapı bir blok yuttuğunda ne olur? — ÖLÇÜLDÜ: HİÇBİR ŞEY.
        ///
        /// Kullanıcı (6. tur): "Blok kapıdan içeri girince o beyaz parlama
        /// çok büyük ve alakasız, kötü bir görünüm; şu an bizdeki bug gibi
        /// gözüküyor."
        ///
        /// REFERANS KARE KARE İNCELENDİ (Levels 1-20, 00:24 civarı, 20 fps'te
        /// çıkarılan 280 kare; turuncu kapıya turuncu blok giriyor):
        ///   • Blok kapıya yaklaşır — kapı DEĞİŞMEZ.
        ///   • Blok kapının çizgisine değer — kapı DEĞİŞMEZ.
        ///   • Blok, kendi renginde İRİ PARÇALARA ayrılıp kapının dışına
        ///     savrulur — kapı yine DEĞİŞMEZ.
        /// Dokuz karenin hiçbirinde kapıda beyazlama, büyüme ya da hale yok.
        ///
        /// Bizde ise kapı beyaza patlıyor, %14 büyüyor ve etrafına kendi
        /// boyunun üç katı bir katkılı hale yayıyordu. Bu 4. turda "kapı da
        /// tepki versin" diye eklenmişti — ölçmeden.
        ///
        /// DERS (bir tepki EKLEMEK, tepkiyi iyileştirmek değildir): "Olay iki
        /// taraflı olmalı" mantıklı bir cümle ve o yüzden sorgulanmadı. Ama
        /// referansın anlatımı başka: olayın öznesi BLOK, kapı yalnızca bir
        /// kapı. Kapıyı da oynatmak, sahnede iki şey birden hareket ettiği
        /// için gözü böler ve asıl olayı — bloğun parçalanmasını — gölgeler.
        ///
        /// AMA IŞIK VAR — YANLIŞ YERE BAKILMIŞ (7. tur düzeltmesi).
        ///
        /// Kullanıcı: "kapıdan blok geçerken geçtiği taraftan parıltı
        /// gelmiyor, o ışık olayını hâlâ yapamadık."
        ///
        /// YENİDEN ÖLÇÜM (`…Levels 1-20 Walkthrough.mp4` 01:44,6 karesi,
        /// 592×1280; kırmızı blok sağdaki kırmızı kapıya giriyor): bloğun
        /// kapıya DEĞEN kenarında 6 piksellik (hücrenin %8'i) bir bant var ve
        /// rengi (255,178,179) — kapının kırmızısından (250,35,37) çok daha
        /// açık, neredeyse beyaz. Bir kare sonrasında blok iri parçalara
        /// ayrılıp dışarı savruluyor ve bant kayboluyor.
        ///
        /// DERS (doğru soruyu sorup yanlış yere bakmak): 6. turda "kapı
        /// yutarken değişiyor mu" diye 280 kare tarandı ve cevap doğru
        /// çıktı — KAPI değişmiyor. Ama ışık kapının üstünde değil, kapı ile
        /// bloğun TEMAS ÇİZGİSİNDE. Ölçüm "yok" dedi çünkü aranan şey
        /// oradaydı da bakılan yer orası değildi. Bir ölçümün kapsamı,
        /// sonucunun geçerlilik alanıdır.
        /// </summary>
        public void PlayAbsorbFlash()
        {
            if (_mouthLight == null) return;
            if (_mouthFade != null) StopCoroutine(_mouthFade);
            _mouthFade = StartCoroutine(FlashMouth());
        }

        /// <summary>
        /// Temas çizgisindeki şerit: kapının İÇ kenarında, açıklığın boyunca.
        ///
        /// Kapının kendisinden daha yüksekte duruyor — kamera tepeye yakın
        /// olduğu için bloğun üst yüzeyiyle kapı arasındaki dar şeridi ancak
        /// ikisinin de üstünde çizilen bir katman gösterebiliyor.
        /// </summary>
        /// <summary>
        /// DUVARIN İÇ YÜZÜ, KAPININ AÇIKLIĞI BOYUNCA KAPI RENGİNDE (10. tur).
        ///
        /// Kullanıcı: "kapının altı boşken o küçük alanda da kapının modeli
        /// olması gerekiyor... ama blok geldiğinde blok önde gözükecek."
        ///
        /// Yani aynı şerit iki şey olmalı: BOŞKEN kapı, DOLUYKEN bloğun
        /// arkası. Bunu yükseklikle çözmek mümkün değil — çerçeve 0,80,
        /// bloğun gövdesi de 0,80; araya bir kat sığmıyor. Kapı barını
        /// oraya uzatmak da olmuyor, çünkü bar bloktan yüksek olmak zorunda
        /// (yoksa duran blok kapıyı örter) ve o zaman şeridi de örtüyor.
        ///
        /// Çözüm şeridi kapının değil DUVARIN parçası yapmak: duvarın iç
        /// yüzüne, tam kapının açıklığı boyunca, kapı renginde ince bir
        /// yüzey. Yüksekliği duvarınkiyle aynı (0…frameHeight), yani:
        ///   • boşken görünüyor — arkasında yalnız duvar var,
        ///   • blok gelince blok DAHA YAKIN (z küçük) olduğu için örtüyor.
        /// Derinlik testi işi kendiliğinden yapıyor, sıraya karışmak
        /// gerekmiyor.
        ///
        /// DERS (bir şeyin iki farklı davranması gerekiyorsa, onu doğru
        /// NESNENİN parçası yap): Şerit "kapının uzantısı" diye
        /// düşünüldüğü sürece kapının kurallarına (bloktan yüksek olmak)
        /// tabiydi ve istenen davranış imkânsızdı. Duvarın parçası olunca
        /// duvarın kurallarına tabi oluyor ve istenen davranış bedava
        /// geliyor.
        /// </summary>
        void BuildWallFace(BoardSpace space, GateModel model,
                           float barMin, float barMax, Material colorMaterial)
        {
            // Yalnız KUZEY duvarın iç yüzü kameraya dönük; diğer üçünde
            // görünecek bir yüzey yok (bkz. paylardaki aynı gerekçe).
            if (model.Side != Side.North || VisualSettings.Current == null) return;

            float height = VisualSettings.Current.frameHeight;
            var face = ViewKit.CreateShape(PrimitiveType.Cube, "WallFace");
            face.transform.SetParent(transform.parent, worldPositionStays: false);

            float spanCenter = (barMin + barMax) * 0.5f;
            float length = Mathf.Max(0.25f, barMax - barMin);

            // Yüzey oyun alanının sınırında, duvarın içine KIL PAYI gömülü:
            // duvarla aynı düzlemde olursa derinlik tamponu titrer.
            const float sink = 0.004f;
            var center = space.CornerToWorld(spanCenter, model.EdgeCoord, 0f);
            center.y = height * 0.5f;
            center.z += model.OutwardSign * sink;

            face.transform.position = center;
            face.transform.localScale = new Vector3(length, height, 0.02f);

            var renderer = face.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = colorMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _wallFace = renderer;
        }

        /// <summary>Duvarın iç yüzündeki kapı rengi şerit; renk değişince o da değişir.</summary>
        MeshRenderer _wallFace;

        void BuildMouthLight(float barLength, GateModel model, Vector3 outwardDir,
                             BlockOut.Runtime.Config.ColorPaletteSO palette)
        {
            // Ölçülen bant hücrenin %8'i; bir tık geniş tutuluyor çünkü bandın
            // yarısı bloğun, yarısı kapının üstüne düşüyor.
            const float bandDepth = 0.10f;
            const float bandHeight = 0.02f;

            var light = ViewKit.CreateShape(PrimitiveType.Cube, "MouthLight");
            light.transform.SetParent(transform, worldPositionStays: false);

            float alongX = model.EdgeHorizontal ? barLength * 0.97f : bandDepth;
            float alongZ = model.EdgeHorizontal ? bandDepth : barLength * 0.97f;
            light.transform.localScale = new Vector3(alongX, bandHeight, alongZ);

            // Barın İÇ kenarı: merkezden içeri doğru derinliğin yarısı.
            float half = BarDepth(model) * 0.5f;
            light.transform.localPosition =
                -outwardDir * half + Vector3.up * (BarHeight + 0.03f);

            _mouthLight = light.GetComponent<MeshRenderer>();

            // KENDİ MATERYAL KOPYASI. Renk başına paylaşılan bir materyalin
            // alfasını söndürmek, aynı renkteki BÜTÜN kapıları birlikte
            // söndürürdü — bir bölümde aynı renkten üç kapı olabiliyor.
            // Bölüm başına birkaç kapı var; kopya maliyeti yok denecek kadar az.
            _mouthMaterial = new Material(ViewKit.GateMouthLight(palette, model.ActiveColor));
            _mouthLight.sharedMaterial = _mouthMaterial;
            _mouthLight.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mouthLight.receiveShadows = false;
            light.SetActive(false);
        }

        /// <summary>
        /// Şerit BİR ANDA yanar, sonra söner. Referansta bant tek karede tam
        /// parlaklıkta beliriyor ve iki-üç karede kayboluyor (20 fps'te ~0,15
        /// saniye); yükselerek gelen bir ışık "yumuşak" durur ve emilimin
        /// ANİLİĞİNİ anlatmaz.
        /// </summary>
        System.Collections.IEnumerator FlashMouth()
        {
            const float life = 0.17f;
            var go = _mouthLight.gameObject;
            go.SetActive(true);

            Color color = _mouthMaterial.color;
            for (float t = 0f; t < life; t += Time.deltaTime)
            {
                // Kare kare sönüş: doğrusal, çünkü referansta ışık kalıcı bir
                // hale bırakmıyor — yanıp bitiyor.
                color.a = 1f - t / life;
                _mouthMaterial.color = color;
                if (_mouthMaterial.HasProperty("_BaseColor"))
                    _mouthMaterial.SetColor("_BaseColor", color);
                yield return null;
            }

            go.SetActive(false);
            _mouthFade = null;
        }

        void OnDestroy()
        {
            if (_mouthMaterial != null) Destroy(_mouthMaterial);
        }

        public void UpdateIceCount()
        {
            if (_iceCounter != null)
                _iceCounter.text = _model.IceCount.ToString();
        }

        /// <summary>Buz kırıldı: gizli renk ve ok ortaya çıkar.</summary>
        public void RevealColor()
        {
            if (_iceCounter != null) Destroy(_iceCounter.gameObject);
            _iceCounter = null;
            _renderer.sharedMaterial = _colorMaterial;

            // Ok buz boyunca gizliydi; kapı artık kullanılabilir olduğuna göre
            // "buradan çıkabilirsin" işareti geri gelmeli.
            if (_arrow != null) _arrow.SetActive(true);

            // Buz kırıldığı an kapı BİR KEZ parlıyor (4. tur, G25).
            //
            // DERS (durum değişimini kutlamak, onu görünür kılar): Kapının
            // materyali tek karede buzdan renge geçiyordu; parçacıklar
            // saçılırken kapının kendisi hiçbir şey yapmıyor ve "artık
            // kullanılabilir" bilgisi kırılma gürültüsünde kayboluyordu.
            // Aynı parlama emilmede de kullanılıyor; iki olayın aynı dili
            // konuşması, oyuncuya "kapıda bir şey oldu" demenin tek yolu.
            PlayAbsorbFlash();
        }

        /// <summary>Kuyruk ilerledi: yeni aktif rengin materyali (L21+ olasılığı).</summary>
        public void SetColorMaterial(Material material)
        {
            _colorMaterial = material;
            if (!_model.IsIced) _renderer.sharedMaterial = material;
        }

        /// <summary>
        /// Rengi tükendi: bar soluklaşır, ok da soluk materyale geçer.
        ///
        /// Kapı GİZLENMEZ — referans oyunda da kapı yerinde durup solar.
        /// Gizlemek duvarda boşluk bırakıyordu (kapı kenarına duvar örülmez).
        /// </summary>
        public void SetGhost(Material ghostMaterial)
        {
            // GEÇİŞ ANİDEN DEĞİL, SÖNEREK.
            //
            // DERS (durum değişimi bir OLAYDIR): Kapı işini bitirdiğinde
            // materyali tek karede ghost'a çevriliyordu; ekranda renk "pat"
            // diye değişiyordu ve oyuncu ne olduğunu anlamıyordu — hatta
            // kapının bozulduğunu sanıyordu. Referansta kapı saydamlaşarak
            // siliniyor; sönme, "bu kapının işi bitti" cümlesinin ta kendisi.
            // Bir kare süren değişim bilgi taşımaz, yalnız şaşırtır.
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeToGhost(ghostMaterial));
        }

        Coroutine _fade;

        /// <summary>
        /// Barı ve oku ghost görünümüne doğru söndürür.
        ///
        /// Renk ARADA geçiyor: hedef materyale hemen geçip yalnız alfayı
        /// indirmek, kapının bir an tam renkli sonra yarı saydam görünmesine
        /// yol açıyordu. Kaynak rengiyle hedef rengi arasında yürümek geçişi
        /// tek bir hareket gibi gösteriyor.
        /// </summary>
        System.Collections.IEnumerator FadeToGhost(Material ghostMaterial)
        {
            // ÖLÇÜM (Levels 1-20, 8 fps ile çıkarılan kareler): kapı 8. karede
            // tam kırmızı (238,45,46), 9'da yarı yolda (150,48,95), 10'da
            // neredeyse bitmiş (95,53,135), 11'de tam çerçeve rengi
            // (66,55,158). Üç kare = 0,375 saniye.
            const float duration = 0.375f;

            // RENK DEĞİŞMİYOR, YALNIZ ALFA İNİYOR.
            //
            // ÖLÇÜM DOĞRULADI: referansta ara kare (150,48,95); kapının
            // kırmızısı (238,45,46) ile çerçevenin moru (66,55,158) arasında
            // %51'lik DÜZ bir karışım (hesap: R 150 → t=0.51, o t ile
            // G=50 ölçülen 48, B=103 ölçülen 95). Yani referans kapıyı başka
            // bir renge boyamıyor; sadece saydamlaştırıyor ve altındaki
            // çerçeve kendiliğinden görünüyor.
            //
            // DERS (bir kaybolmayı iki değişkenle anlatmaya çalışma): Renk ve
            // alfa birlikte yürüyünce ara karelerde kapı ne kendi rengi ne de
            // çerçeve oluyor — "solmuş" değil "kirlenmiş" görünüyordu.
            // `ghostMaterial` artık yalnız çağrı uyumluluğu için duruyor.
            // SAYDAMLIK YOK — RENK ÇERÇEVEYE DOĞRU YÜRÜYOR (5. tur).
            //
            // Kullanıcı: "bazen kapı kaybolurken üzerinde uzun çizgi
            // işaretleri görüyoruz."
            //
            // TEŞHİS: Sönme `ViewKit.Translucent` kullanıyordu; o materyal
            // derinliğe YAZMAZ (`_ZWrite = 0`, saydamların olması gerektiği
            // gibi). Kapı yarı saydamken prizmanın üst kapağı, pahı ve yan
            // duvarı ekranda üst üste harmanlanıyor; iki kez boyanan yerler
            // daha koyu çıkıyor ve mesh'in iç kenarları UZUN ÇİZGİLER olarak
            // görünüyor.
            //
            // DERS (saydamlık, nesnenin KENDİ içini de gösterir): "Yavaşça
            // kaybolsun" denince ilk akla gelen alfayı indirmek. Ama alfa,
            // nesnenin arkasındakini gösterirken kendi arka yüzeylerini de
            // gösterir. İçi dolu bir cismin yarı saydam hâli, cismin
            // topolojisini ele verir.
            //
            // ÇÖZÜM ölçümden geliyor: referansın ara karesi (150,48,95),
            // kapı kırmızısı ile çerçeve moru arasında %51'lik düz bir
            // karışım. Kapının ARKASINDA zaten çerçeve var; dolayısıyla
            // "alfayı sıfıra indirmek" ile "rengi çerçeve rengine yürütmek"
            // ekranda AYNI pikselleri üretiyor — ama ikincisi opak, yani
            // çizgi üretmiyor.
            Color from = ReadColor(_renderer.sharedMaterial);
            var visualCfg = VisualSettings.Current;
            Color to = visualCfg != null
                ? visualCfg.frameColor
                : new Color(0.30f, 0.26f, 0.58f);
            to.a = from.a;

            // Kendi örneğimizde çalışıyoruz: paylaşılan materyali boyamak
            // aynı renkteki BÜTÜN kapıları söndürürdü.
            var fading = ViewKit.CopyFor(_renderer.sharedMaterial, "GateFade");
            _renderer.sharedMaterial = fading;

            var arrowRenderer = _arrow != null ? _arrow.GetComponent<MeshRenderer>() : null;
            Material arrowFading = null;
            Color arrowFrom = default, arrowTo = default;
            if (arrowRenderer != null)
            {
                arrowFrom = ReadColor(arrowRenderer.sharedMaterial);
                arrowTo = to;
                arrowTo.a = arrowFrom.a;
                arrowFading = ViewKit.CopyFor(arrowRenderer.sharedMaterial, "ArrowFade");
                arrowRenderer.sharedMaterial = arrowFading;
            }

            // OKUN KOYU HALKASI DA SÖNMELİ (6. tur, kullanıcı: "kapının
            // gidişi, kayboluşu daha smooth olsun; bizde bir bozulma var").
            //
            // Halka `_arrow`ın çocuğu; sönme onu boyamıyordu. Bar ve ok
            // çerçeve rengine yürürken halka koyu kalıyor, sonunda hepsi bir
            // anda kapanıyordu — geçişin son karesinde ekranda ok biçiminde
            // koyu bir leke beliriyordu. Kullanıcının gördüğü "bozulma" buydu.
            var ringRenderers = new List<MeshRenderer>();
            var ringMaterials = new List<Material>();
            var ringFrom = new List<Color>();
            if (_arrow != null)
                foreach (var r in _arrow.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (arrowRenderer != null && r == arrowRenderer) continue;
                    var copy = ViewKit.CopyFor(r.sharedMaterial, "RingFade");
                    ringFrom.Add(ReadColor(r.sharedMaterial));
                    r.sharedMaterial = copy;
                    ringRenderers.Add(r);
                    ringMaterials.Add(copy);
                }

            // KAPI TAMAMEN SAYDAMLAŞIYOR (4. tur, G22).
            //
            // Kullanıcı: "Kapı kaybolma efekti — bizde soluklaşıyor;
            // orijinalde tamamen transparan olarak kayboluyor."
            //
            // DERS (yarım kalmış bir geçiş, geçiş değil ARIZA gibi okunur):
            // Kapı ghost rengine solup ORADA KALIYORDU. Ekranda "rengi
            // atmış bir kapı" duruyor ve oyuncu onu hâlâ kullanılabilir
            // sanıyordu. Kapının işi bittiyse ekranda yeri de bitmeli.
            //
            // Gizlemenin eskiden kaçınılan bedeli "duvarda boşluk kalması"ydı;
            // bu doğru değil: `BoardFrameMeshBuilder` çerçeveyi KESİNTİSİZ bir
            // halka olarak örüyor, kapı yalnız onun üstünde duruyor. Kapı
            // saydamlaşınca altından çerçevenin kendisi çıkıyor — referansta
            // da görünen bu.
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                // DOĞRUSAL, `SmoothStep` DEĞİL: ölçülen ara kare tam ortada
                // %51'de. SmoothStep aynı anda %50 verirdi ama uçlarda
                // yavaşlar; referansın üç karesi eşit aralıklı.
                float k = Mathf.Clamp01(t / duration);
                // Hafif yavaşlayan bir eğri: doğrusal geçişte son kare "kesik"
                // hissettiriyordu. Ölçülen ara kare hâlâ ~%51'de kalıyor.
                float e = k * k * (3f - 2f * k) * 0.35f + k * 0.65f;
                Paint(fading, Color.Lerp(from, to, e));
                if (arrowFading != null)
                    Paint(arrowFading, Color.Lerp(arrowFrom, arrowTo, e));
                for (int i = 0; i < ringMaterials.Count; i++)
                    Paint(ringMaterials[i], Color.Lerp(ringFrom[i], to, e));
                yield return null;
            }

            // KAPININ ALTINDAKİ HER ŞEY KAPANIYOR, YALNIZ BAR DEĞİL.
            //
            // Eskiden yalnız `_renderer` ve ok kapatılıyordu. Kapıya sonradan
            // eklenen bir çocuk (bkz. kaldırılan koyu kenar) o listede
            // olmadığı için sönmüyor ve ekranda gri bir dikdörtgen olarak
            // kalıyordu.
            //
            // DERS (ada göre değil, AĞACA göre kapat): "Şu iki nesneyi
            // gizle" diyen kod, üçüncü nesne eklendiği gün sessizce yanlış
            // olur. `GetComponentsInChildren` o listeyi kendi tutuyor.
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
                renderer.enabled = false;
            // Ok ve ONUN ÇOCUKLARI (koyu halka) kapının ağacında değil —
            // `CreateArrow` onları tahta köküne bağlıyor. Ada göre değil,
            // OKUN ağacına göre kapatılıyorlar.
            if (_arrow != null)
                foreach (var renderer in _arrow.GetComponentsInChildren<MeshRenderer>(true))
                    renderer.enabled = false;

            if (fading != null) Destroy(fading);
            if (arrowFading != null) Destroy(arrowFading);
            foreach (var m in ringMaterials) if (m != null) Destroy(m);
            _fade = null;
        }

        static void Paint(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        }

        /// <summary>
        /// Materyalin rengini GÜVENLİ okur.
        ///
        /// DERS (`Material.color` her gölgecide yok): `.color` aslında
        /// `_Color` özelliğini okuyor. Okun materyali `BlockOut/Brick`
        /// gölgecisini kullanıyor ve onda `_Color` YOK — sönmeyi yazdığımda
        /// konsola iki satır hata düştü ("doesn't have a color property
        /// '_Color'"). Unity bunu istisnaya çevirmiyor, sessizce siyah
        /// döndürüyor; yani hata görülmese geçiş siyahtan başlardı.
        /// URP'de doğru ad `_BaseColor`; ikisini de denemek gerekiyor.
        /// </summary>
        static Color ReadColor(Material material)
        {
            if (material == null) return Color.white;
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color")) return material.color;
            return Color.white;
        }
    }
}
