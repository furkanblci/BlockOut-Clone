using UnityEngine;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Menü ekranlarının KOD İLE üretilen sprite'ları.
    ///
    /// DERS (hangi görsel çizilir, hangisi istenir?): Referans mağazanın
    /// yüzeyleri iki gruba ayrılıyor. Birincisi GEOMETRİ: çizgili tente, kapsül
    /// şeritler, ∞ halkası, üstü çizili ADS rozeti — bunların hepsi birkaç
    /// formülle tam olarak çizilebilir, bir üreticiden istemek hem beklemek hem
    /// de her seferinde biraz farklı bir sonuç almak demek. İkincisi RESİM:
    /// altın yığınları, sandıklar, para keseleri — bunlar ışık, gölge ve malzeme
    /// ister; formülle yaklaşılamaz, dışarıdan gelmeli.
    ///
    /// Bu dosya yalnız birinci grubu üretir. İkinci grup <see cref="Art"/>
    /// altında adlandırıldı ve gelene kadar mevcut ikonlara düşüyor.
    ///
    /// DERS (kapsül = 9-dilim daire): Referanstaki bölüm başlıkları tam kapsül
    /// (stadyum) biçiminde. Her genişlik için ayrı görsel üretmek yerine TEK bir
    /// daire üretilip 9-dilim payı yarıçapına eşitlenir: orta sütun/satır
    /// esnerken köşeler daire kalır, yani her genişlikte kusursuz kapsül çıkar.
    /// </summary>
    public static class MenuSprites
    {
        static Sprite _capsule, _capsuleOutline, _awning, _infinity, _noAds, _fadeDown,
                      _ring;

        // ---- Kapsül -------------------------------------------------------

        const int CapsuleSize = 128;

        /// <summary>Dolu kapsül. Yatay esnetildiğinde uçları daire kalır.</summary>
        public static Sprite Capsule => _capsule != null ? _capsule
            : (_capsule = BuildCapsule("ShopCapsule", ringWidth: 0f));

        /// <summary>Yalnız çerçeve — bölüm başlığının krem konturu.</summary>
        public static Sprite CapsuleOutline => _capsuleOutline != null ? _capsuleOutline
            : (_capsuleOutline = BuildCapsule("ShopCapsuleOutline", ringWidth: 7f));

        static Sprite BuildCapsule(string name, float ringWidth)
        {
            const int s = CapsuleSize;
            var tex = NewTexture(name, s, s);
            var pixels = new Color32[s * s];
            float half = s * 0.5f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                               new Vector2(half, half));
                    float alpha = 1f - Step(half - 1.2f, half, d);

                    if (ringWidth > 0f)
                    {
                        float inner = 1f - Step(half - ringWidth - 1.2f,
                                                            half - ringWidth, d);
                        alpha = Mathf.Clamp01(alpha - inner);
                    }

                    pixels[y * s + x] = new Color(1f, 1f, 1f, alpha);
                }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            // Pay tam yarıçap: orta sütun 0 piksel kalır, iki uç daire olarak esner.
            const int border = s / 2 - 1;
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        // ---- Tente --------------------------------------------------------

        const int AwningW = 448;
        const int AwningH = 176;
        const int Scallops = 7;

        /// <summary>Referanstaki mavi çizgili tente: dikey şeritler + festonlu alt kenar.</summary>
        public static Sprite Awning => _awning != null ? _awning : (_awning = BuildAwning());

        static Sprite BuildAwning()
        {
            var tex = NewTexture("ShopAwning", AwningW, AwningH);
            var pixels = new Color32[AwningW * AwningH];

            // RENKLER YENİDEN ÖLÇÜLDÜ (2026-08-17, `market.jpeg`).
            //
            // İlk örnekleme koyu #0356FB / açık #0084FC vermişti. Ekrandaki
            // SONUÇ ölçülünce (bizim yakalamamızda #034CDF ve #0075E0) iki
            // sorun çıktı: açık şerit referanstakinden belirgin biçimde KOYU
            // ve ikisi de MORA kaçıyordu. Referansın gerçek çifti #0066DC ve
            // #01A1F5 — yani açık şerit çok daha CAMGÖBEĞİ (G kanalı 117
            // değil 161). Parlaklık oranı bizde 1.32, referansta 1.43.
            //
            // DERS (kaynağı değil SONUCU ölç): Sprite'ın taban rengi ekranda
            // göründüğü renk değil — üstüne dikey parlaklık rampası (`shade`)
            // biniyor. Referansla kıyaslanacak şey ekrandaki piksel; taban
            // renk ondan GERİ hesaplanmalı (burada ÷0.945).
            // KOYU ŞERİT AÇILDI (2026-08-22). Üstünde başlık/kapsül olmayan
            // sağ bölgeden, tentenin %55 yüksekliğinden ölçüldü:
            //     referans koyu #0375F8 / açık #019FFA  -> oran 1.11
            //     bizim    koyu #0068E1 / açık #01A4F6  -> oran 1.25
            // Açık şerit zaten tutuyordu; koyu şerit fazla koyuydu ve kontrast
            // referanstan YÜKSEK çıkıyordu (gözle tam tersini sanmıştım —
            // ölçmeden karar vermemek gerekiyor). Taban ×1.11 açıldı.
            var dark  = new Color(0.012f, 0.472f, 1.000f);   // ekranda ≈ #0375F8
            var light = new Color(0.004f, 0.667f, 1.000f);   // ekranda ≈ #01A1F5

            float stripeW = AwningW / (float)Scallops;

            // Feston geometrisi.
            //
            // DERS (sığ yay, küçük daire değildir): İlk denemede yarıçap
            // yarım şerite eşitlendi — tam yarım daire, referanstan iki kat
            // derin. Sonra yarıçap küçültülünce yay dokunun alt kenarına
            // çarpıp DÜZ kesildi ve aralarda sivri uçlar kaldı. Doğrusu
            // derinliği seçip yarıçapı ondan TÜRETMEK: kirişi 2c, sarkması v
            // olan bir yayın yarıçapı R = (c² + v²) / 2v. Böylece yay şerit
            // sınırında tam olarak kumaşın alt çizgisine değer, uç kalmaz.
            float chord = stripeW * 0.5f;
            // Sarkma oranı referanstan: bir feston periyodu boyunca alt kenar
            // 40 piksel iniyor, tentenin toplam yüksekliği 286 → %14.
            // (İlk ölçüm %23 demişti; o rakam tentenin dışındaki bir mavi
            // pikselden kirlenmişti — periyot içinde ölçmek doğrusu.)
            // DERİNLİK YENİDEN ÖLÇÜLDÜ (2026-08-22, `market.jpeg`).
            // Eski %14 değeri "bir feston periyodunda alt kenar 40 px iniyor,
            // tente 286" hesabından geliyordu. Üstünde başlık/kapsül OLMAYAN
            // sütunlardan (ekranın sağ %70-%98'i) yeniden ölçüldü:
            //     referans: tente 315 px, feston derinliği 68 px = %21.6
            //     bizim   : tente 266 px, feston derinliği 37 px = %13.9
            // Festonlarımız referansın üçte ikisi kadar sığdı; kumaş "asılı"
            // değil "kesilmiş şerit" gibi duruyordu.
            // DERİNLİK ÜÇÜNCÜ KEZ ÖLÇÜLDÜ (2026-08-22). Önceki iki ölçüm de
            // KİRLİYDİ: mavi maskesi tenteden aşağı taşıyor ve x aralığı
            // başlığa/kapsüle denk geliyordu. %14 -> %21.6 değişikliği bir
            // REGRESYONDU; kullanıcı "tente daha kötü oldu" dedi ve haklıydı.
            //
            // Doğru ölçüm: y arama sınırı tentenin hemen altında bitirilerek,
            // x aralığı sağın %55-%99'u:
            //     referans alt kenar 315..351 -> derinlik 36 px = tentenin %10.3'ü
            //     bizim (%21.6 iken)  302..357 -> 55 px = %15.4
            // Referansın festonları GENİŞ ve SIĞ; bizimkiler parmak gibi
            // sarkıyordu.
            //
            // DERS: aynı büyüklüğü üç farklı pencereyle ölçüp üç farklı sonuç
            // aldım. Ölçümün kendisi doğrulanmadan sabit değiştirilmemeli —
            // GÖZLE kontrol (kenarı büyütüp bakmak) ölçümü yakaladı.
            // DERİNLİK VE LEVHA PAYI YENİDEN ÖLÇÜLDÜ (13. tur, M2b).
            //
            // Artık çentikler levhayla dolu olduğu için yayın derinliği
            // DOĞRUDAN ölçülebiliyor: her sütunda PARLAK mavinin en alt
            // satırı = kumaşın kenarı.
            //
            //     referans  yay derinligi 41 br | kumas alti - tente dibi 29 br
            //     bizim     yay derinligi 18 br | 11 br
            //
            // Yayımız referansın %44'üydü ve kumaşın altında levha payı
            // neredeyse yoktu — bu yüzden "sarkan kumaş" değil "dalgalı
            // kenar" gibi duruyordu.
            //
            // NOT (geçmişteki geri alma): 9. turda derinlik %21,6'ya
            // çıkarılmış, kullanıcı *"tente daha kötü oldu"* demiş ve %10,3'e
            // dönülmüştü. O zaman çentikler SAYDAMDI: derin yaylar kahverengi
            // duvarın önünde uzun parmaklar gibi sarkıyordu. Levha eklendiği
            // için aynı derinlik artık referanstaki gibi okunuyor.
            //
            // DERS (bir sayı tek başına değil, KOMŞUSUYLA birlikte yanlıştı):
            // Derinlik hep doğruydu; eksik olan altındaki levhaydı. Yalnız
            // derinliği değiştirip geri almak, iki turluk bir döngüye mal
            // oldu.
            // Bir tur ölçüp düzeltildi: 0,108/0,155 ile yay 33 br ve levha
            // 39 br çıktı (hedef 41 ve 29).
            float plateH = AwningH * 0.080f;         // kumaşın altındaki levha: 29 br
            float depth = AwningH * 0.192f;          // yay: 41 br
            float scallopR = (chord * chord + depth * depth) / (2f * depth);

            for (int y = 0; y < AwningH; y++)
            {
                // Doku y=0 ALTTIR; görüntüdeki üstten oran:
                float t = 1f - y / (float)(AwningH - 1);

                // Dikey parlaklık: üst ve alt uçlar koyu, orta bant parlak.
                // (Referansta tente kumaşı üstte gölgede kalıyor.)
                float shade = t < 0.45f
                    ? Mathf.Lerp(0.66f, 1f, t / 0.45f)
                    : Mathf.Lerp(1f, 0.80f, (t - 0.45f) / 0.55f);

                for (int x = 0; x < AwningW; x++)
                {
                    int stripe = Mathf.FloorToInt(x / stripeW);
                    var baseColor = (stripe & 1) == 0 ? dark : light;

                    // Şerit kenarında 1 piksellik yumuşatma — sert kenar
                    // ölçekleyince tırtıklanıyor.
                    float inStripe = x / stripeW - stripe;
                    float edge = Mathf.Min(inStripe, 1f - inStripe) * stripeW;
                    if (edge < 1f)
                    {
                        var other = (stripe & 1) == 0 ? light : dark;
                        baseColor = Color.Lerp(Color.Lerp(baseColor, other, 0.5f),
                                               baseColor, Mathf.Clamp01(edge));
                    }

                    // Feston: alt banttaki kumaş yalnız yayın içinde var.
                    // Daire merkezi y = R'de; en alçak noktası y = 0'a değiyor.
                    // Yay merkezleri LEVHANIN ÜSTÜNDEN başlıyor: kumaşın en
                    // alçak noktası y = plateH, altındaki şerit hep levha.
                    float cx = (stripe + 0.5f) * stripeW;
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                               new Vector2(cx, plateH + scallopR));

                    // ÇENTİKLER SAYDAM DEĞİL, KOYU LACİVERT (13. tur, M2a+M2b).
                    //
                    // Bizde festonun çentikleri saydamdı ve arkadaki
                    // KAHVERENGİ duvar görünüyordu; tentenin alt kenarı bu
                    // yüzden taraklı bir siluetti. Referansta ise alt kenar
                    // DÜZ: çentikleri koyu lacivert bir levha dolduruyor ve
                    // yaylar o levhanın üstünde kumaşın sarkması olarak
                    // okunuyor. Kullanıcının *"ucu aşağıya düşmüş gibi
                    // gözüküyor, bu da derinlik katıyor"* dediği şey bu.
                    //
                    // ÖLÇÜM (`market.jpeg`, feston ortasından yukarı):
                    //     0..27 birim   (0,28,103)..(6,35,135)  KOYU LACIVERT
                    //     27 birim ustu (0,83,213)              parlak mavi
                    // Yani levha ~27 birim ve neredeyse düz bir renk.
                    //
                    // Levha DOKUYA çiziliyor, ayrı bir panel olarak değil.
                    // Bu bilerek: aynı levha 8. ve 11. turlarda ayrı bir
                    // panel olarak eklenip iki kez KALDIRILMIŞTI — çünkü
                    // ayrı panel tentenin altından taşıyor, kaydırmada
                    // sürükleniyor ve turuncu şeridin üstüne gölge
                    // düşürüyordu. Dokunun içinde ise tam olarak festonun
                    // bittiği yerde biter, taşamaz.
                    //
                    // DERS (bir öğe iki kez kaldırıldıysa, ÜÇÜNCÜSÜNDE
                    // yerini değiştir): Levhanın kendisi doğruydu, yanlış
                    // olan AYRI BİR KATMAN olmasıydı. Aynı şeyi tekrar
                    // eklerken önce "neden kaldırılmıştı" diye bakmak, aynı
                    // şikâyeti üçüncü kez almayı önlüyor.
                    float kumas = y < plateH ? 0f
                        : y >= plateH + depth ? 1f
                        : 1f - Step(scallopR - 1.2f, scallopR, d);
                    float alpha = 1f;

                    // ALT KENARDAKİ GÖLGE GÜÇLENDİ (13. tur, M2a — kullanıcı:
                    // *"orijinal oyundaki tente çok daha güzel, alt kısmına
                    // doğru bir gölge şeklinde çizgisi var"*).
                    //
                    // ÖLÇÜM (`market.jpeg`, feston ortasından dikey kesit):
                    //     referans  (1,93,226) -> (0,28,103)   ~7 px, %30'a
                    //     bizim     (1,122,184) -> hemen kahverengi duvar
                    // Yani bizde de bir koyulaşma vardı ama 5 pikselde
                    // %62'de kalıyordu; ekranda "gölge" olarak okunmuyordu.
                    // Referansta kumaşın alt kenarı belirgin biçimde
                    // KARARIYOR ve tenteyi duvardan ayıran şey bu.
                    //
                    // DERS (var olan bir efekt YETERSİZ de olabilir): "Gölge
                    // yok" diye bakınca kodda gölge bulunca "var, tamam"
                    // demek kolay. Ölçüm iki değeri yan yana koyunca farkın
                    // %62'ye karşı %30 olduğu görüldü — efekt vardı, ama
                    // görünmüyordu.
                    float rim = y >= plateH + depth ? 1f
                        : Mathf.Clamp01((scallopR - d) / 8f);

                    var c = baseColor * shade * Mathf.Lerp(0.34f, 1f, rim);

                    // Çentiğin içi: levhanın koyu laciverti.
                    var levha = new Color(0.008f, 0.122f, 0.435f);   // (2,31,111)
                    c = Color.Lerp(levha, c, kumas);

                    pixels[y * AwningW + x] = new Color(c.r, c.g, c.b, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, AwningW, AwningH),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        // ---- Halka ---------------------------------------------------------

        /// <summary>
        /// İnce daire konturu — Yolculuk'taki bölge çemberinin parlak halkası.
        ///
        /// DERS (halka için kapsül konturunu kullanma): Elimde zaten
        /// <see cref="CapsuleOutline"/> var ve kare bir alanda daire konturu
        /// veriyor. Ama onun kalınlığı yarıçapın %11'i; 640 birimlik bir
        /// çemberde 70 birimlik bir bilezik oluyor ve referanstaki ince ışık
        /// çizgisiyle alakası kalmıyor. Kalınlık oransal olduğu için "aynı
        /// sprite'ı küçültüp kullan" da işe yaramaz.
        /// </summary>
        public static Sprite Ring => _ring != null ? _ring : (_ring = BuildRing());

        static Sprite BuildRing()
        {
            const int s = 512;
            var tex = NewTexture("MenuRing", s, s);
            var pixels = new Color32[s * s];

            float half = s * 0.5f;
            float radius = half - 6f;
            const float width = 9f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                               new Vector2(half, half));
                    float alpha = 1f - Step(width * 0.5f - 1.2f, width * 0.5f,
                                            Mathf.Abs(d - radius));
                    pixels[y * s + x] = new Color(1f, 1f, 1f, alpha);
                }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
        }

        // ---- Madalya ışını -------------------------------------------------

        static Sprite _sunburst, _foliage;

        /// <summary>
        /// Madalyanın arkasındaki DİŞLİ ÇELENK — referanstaki altın madalyanın
        /// kenarındaki yapraklı taç.
        ///
        /// DERS (madalya bir DAİRE değildir): Podyumdaki madalyalar iki iç içe
        /// düz daireydi; altın rengi verilse bile ekranda "sarı bir nokta"
        /// olarak okunuyordu ve kullanıcı "madalyamız yok" dedi — teknik olarak
        /// vardı, görsel olarak yoktu. Bir madalyayı madalya yapan şey dairenin
        /// kendisi değil, kenarındaki DÜZENLİ ÇIKINTILAR: göz onları taç/kurdele
        /// olarak okuyor.
        ///
        /// Yarıçapı açıya göre dalgalandırıyoruz; 12 diş referanstaki taçla
        /// aynı sıklıkta. Ayrı bir görsele gerek yok, her boyutta temiz.
        /// </summary>
        public static Sprite Sunburst => _sunburst != null ? _sunburst
            : (_sunburst = BuildSunburst());

        static Sprite BuildSunburst()
        {
            const int s = 256;
            const int teeth = 12;
            var tex = NewTexture("MenuSunburst", s, s);
            var pixels = new Color32[s * s];

            float half = s * 0.5f;
            float baseRadius = half * 0.74f;
            float toothDepth = half * 0.22f;

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x + 0.5f - half, dy = y + 0.5f - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // Dişler yuvarlak uçlu: kosinüsün kendisi zaten yumuşak,
                    // keskin üçgen "vektör klibi" gibi duruyor.
                    float wave = 0.5f + 0.5f * Mathf.Cos(angle * teeth);
                    float radius = baseRadius + toothDepth * wave;

                    float alpha = 1f - Step(radius - 1.6f, radius, d);
                    pixels[y * s + x] = new Color(1f, 1f, 1f, alpha);
                }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
        }

        // ---- Ağaç tacı -----------------------------------------------------

        /// <summary>
        /// Yapraklı ağaç tacı — tek daire DEĞİL, birbirine geçmiş kabarcıklar.
        ///
        /// DERS (doğada düz kenar yoktur): Liderlik sahnesindeki ağaçlar iki
        /// düz daireydi ve "ağaç" değil "yeşil top" okunuyordu; kullanıcı
        /// "o ağaçları kendimiz çizdirmek yerine ağaçlık bir alan çizdirelim"
        /// dedi. Referansta taç, kenarları TIRTIKLI bir kütle: yaprak
        /// kümelerinin silueti.
        ///
        /// Yedi kabarcığın birleşimi (mesafe alanlarının minimumu) tek bir
        /// düzgün siluet veriyor; kenar hâlâ yumuşak, ama artık daire değil.
        /// </summary>
        public static Sprite Foliage => _foliage != null ? _foliage
            : (_foliage = BuildFoliage());

        static Sprite BuildFoliage()
        {
            const int s = 256;
            var tex = NewTexture("MenuFoliage", s, s);
            var pixels = new Color32[s * s];

            // (merkez x, merkez y, yarıçap) — hepsi 0-1 aralığında, dokuya göre.
            var blobs = new[]
            {
                new Vector3(0.50f, 0.62f, 0.30f),
                new Vector3(0.28f, 0.52f, 0.22f),
                new Vector3(0.72f, 0.52f, 0.22f),
                new Vector3(0.38f, 0.34f, 0.21f),
                new Vector3(0.62f, 0.34f, 0.21f),
                new Vector3(0.50f, 0.28f, 0.24f),
                new Vector3(0.50f, 0.80f, 0.20f)
            };

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float px = (x + 0.5f) / s, py = (y + 0.5f) / s;

                    // Siluete olan en KISA mesafe: kabarcıkların birleşimi.
                    float best = 1f;
                    foreach (var b in blobs)
                    {
                        float dx = px - b.x, dy = py - b.y;
                        best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dy * dy) - b.z);
                    }

                    float alpha = 1f - Step(0f, 1.6f / s, best);
                    pixels[y * s + x] = new Color(1f, 1f, 1f, alpha);
                }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
        }

        // ---- Flama --------------------------------------------------------

        static Sprite _pennant;

        /// <summary>
        /// İndirim rozetinin flaması: üstü yuvarlak köşeli, altı çentikli şerit.
        ///
        /// DERS (çentik 9-dilime GİRMEZ): Rozet yuvarlak bir panelle yapılmıştı;
        /// referanstaki asılı flama hissi çıkmıyordu. Çentiği 9-dilim bir
        /// sprite'a koymak da işe yaramaz — orta sütun esnerken çentik de
        /// esner ve rozet büyüdükçe "V" bir yamuğa döner. Rozetin boyu sabit
        /// olduğu için sprite düz (Simple) çiziliyor, çentik de doku içinde
        /// oranını koruyor.
        /// </summary>
        public static Sprite Pennant => _pennant != null ? _pennant : (_pennant = BuildPennant());

        static Sprite BuildPennant()
        {
            const int w = 160, h = 200;
            var tex = NewTexture("ShopPennant", w, h);
            var pixels = new Color32[w * h];

            const float radius = 26f;      // yalnız ÜST köşeler yuvarlak
            const float notch = 46f;       // alttaki V'nin derinliği

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;

                    // Üst köşeler: yuvarlak dikdörtgen mesafesi.
                    float dx = Mathf.Abs(px - w * 0.5f) - (w * 0.5f - radius);
                    float dyTop = (py - (h - radius));
                    float d = dx > 0f && dyTop > 0f
                        ? new Vector2(dx, dyTop).magnitude - radius
                        : Mathf.Max(dx - radius, dyTop - radius);

                    float alpha = 1f - Step(-0.75f, 0.75f, d);

                    // Alt çentik: ortadan yukarı doğru açılan V.
                    float vLine = notch * (1f - Mathf.Abs(px - w * 0.5f) / (w * 0.5f));
                    alpha *= Step(vLine - 0.9f, vLine + 0.9f, py);

                    pixels[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
        }

        // ---- Simgeler -----------------------------------------------------

        /// <summary>
        /// ∞ halkası.
        ///
        /// DERS (bu proje bunu DÖRT KEZ yaşadı): "∞" bir YAZI değil. Baloo 2'de
        /// o karakter yok; TMP boş kutu çizer. Simge istiyorsan çizeceksin.
        /// </summary>
        public static Sprite Infinity => _infinity != null ? _infinity
            : (_infinity = BuildInfinity());

        static Sprite BuildInfinity()
        {
            const int w = 160, h = 96;
            var tex = NewTexture("ShopInfinity", w, h);
            var pixels = new Color32[w * h];

            float r = 30f, ring = 11f, cy = h * 0.5f;
            var left = new Vector2(w * 0.5f - 28f, cy);
            var right = new Vector2(w * 0.5f + 28f, cy);

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float dl = Mathf.Abs(Vector2.Distance(p, left) - r);
                    float dr = Mathf.Abs(Vector2.Distance(p, right) - r);
                    float d = Mathf.Min(dl, dr);
                    float alpha = 1f - Step(ring * 0.5f - 1.2f, ring * 0.5f, d);
                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
        }

        /// <summary>Üstü çizili kırmızı halka — "reklam yok" rozetinin zemini.</summary>
        public static Sprite NoAds => _noAds != null ? _noAds : (_noAds = BuildNoAds());

        static Sprite BuildNoAds()
        {
            const int s = 160;
            var tex = NewTexture("ShopNoAds", s, s);
            var pixels = new Color32[s * s];

            float half = s * 0.5f, r = half - 10f, ring = 14f, bar = 11f;
            var c = new Vector2(half, half);
            // Çapraz çubuk: sol üstten sağ alta (referansta bu yön).
            var dir = new Vector2(0.7071f, -0.7071f);

            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f) - c;
                    float d = p.magnitude;

                    float ringA = 1f - Step(ring * 0.5f - 1.2f, ring * 0.5f,
                                                        Mathf.Abs(d - r));
                    // Çubuk: doğrultuya dik uzaklık küçük VE daire içinde.
                    float across = Mathf.Abs(p.x * dir.y - p.y * dir.x);
                    float barA = (1f - Step(bar * 0.5f - 1.2f, bar * 0.5f, across))
                               * (1f - Step(r - 1.2f, r, d));

                    pixels[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(ringA, barA)));
                }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
        }

        /// <summary>
        /// Yukarıdan aşağı saydamlaşan beyaz — düz bir panelin üstüne konup
        /// boyandığında dikey degrade verir (özel teklif kartı böyle).
        ///
        /// DERS (Image degrade çizemez): Unity'nin Image bileşeni tek renk
        /// çarpar; degrade DOKUNUN içinde olmalı. Ama degradeyi kartın taban
        /// rengine pişirirsek her renk için ayrı doku gerekir. Alfa degradesi
        /// üretip ÜSTE koymak, aynı dokuyu her renkte kullanmayı sağlar.
        /// </summary>
        public static Sprite FadeDown => _fadeDown != null ? _fadeDown
            : (_fadeDown = BuildFadeDown());

        static Sprite BuildFadeDown()
        {
            const int w = 8, h = 128;
            var tex = NewTexture("ShopFadeDown", w, h);
            var pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                // y=0 alt; alt tamamen opak, üst tamamen saydam.
                float a = 1f - y / (float)(h - 1);
                a *= a;                                  // geçiş alta doğru toplansın
                for (int x = 0; x < w; x++)
                    pixels[y * w + x] = new Color(1f, 1f, 1f, a);
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            // Yatay pay: yana esnerken degrade bozulmasın.
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(3, 0, 3, 0));
        }

        /// <summary>
        /// Eşikli yumuşak geçiş — GLSL'deki <c>smoothstep</c>.
        ///
        /// DERS (Unity'nin Mathf.SmoothStep BU DEĞİL): Unity'deki aynı adlı
        /// metot iki DEĞER arasında yumuşatılmış bir lerp yapar
        /// (<c>from + (to-from) * smooth(t)</c>) ve t'yi 0-1'e kırpar. Yani
        /// <c>Mathf.SmoothStep(62.8f, 64f, d)</c> bir maske değil, 64'e yakın
        /// bir SAYI döndürür; <c>1 - o</c> her yerde negatif çıkar ve maskenin
        /// tamamı saydam olur. Kapsül, ∞ ve tente festonu tam olarak bu yüzden
        /// hiç çizilmedi. Kenar yumuşatmasında istenen şey, x'in iki eşik
        /// arasında 0'dan 1'e geçmesi — o da bu metot.
        /// </summary>
        static float Step(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(1e-5f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        static Sprite _quilt;

        /// <summary>
        /// Mağaza bölümlerinin arkasındaki KAPİTONE doku — döşenebilir.
        ///
        /// Referansta (`market.jpeg`) bölüm zeminleri düz değil: eşkenar
        /// dörtgen bir kapitone deseni var ve her yüzeyin ışığı biraz farklı.
        /// Ölçüm: bordo bölümde kırmızı kanalı 62 ile 116 arasında salınıyor
        /// (±%25), mor bölümde daha yumuşak. Kullanıcının "arkaplan çok sade
        /// kalmış, orada doku filan var" dediği şey bu (11. APK bulgusu).
        ///
        /// DERS (doku RENK DEĞİL, IŞIKTIR): Deseni renkli çizip Image'ı
        /// boyamak, boyama çarpma olduğu için deseni de renklendirirdi ve her
        /// bölümde farklı bir ton çıkardı. Bunun yerine doku BEYAZ üstüne
        /// yalnız parlaklık farkı olarak çiziliyor; bölüm rengi tint ile
        /// veriliyor. Böylece tek doku üç bölümde de doğru çalışıyor.
        /// </summary>
        public static Sprite Quilt => _quilt != null ? _quilt : (_quilt = BuildQuilt());

        static Sprite BuildQuilt()
        {
            const int s = 128;                 // döşeme karesi
            var tex = NewTexture("ShopQuilt", s, s);
            tex.wrapMode = TextureWrapMode.Repeat;

            var pixels = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                // Eşkenar dörtgen ızgara: iki köşegen dalganın toplamı.
                float u = (x + y) / (float)s * Mathf.PI * 2f;
                float v = (x - y) / (float)s * Mathf.PI * 2f;
                float facet = (Mathf.Cos(u) + Mathf.Cos(v)) * 0.5f;      // -1..1

                // Dikişlerde ince koyu çizgi: dalganın sıfır geçişine yakın yer.
                float seam = 1f - Step(0.02f, 0.16f, Mathf.Abs(facet));

                // Genlik referans ölçüsünden: bordo bölümde kırmızı kanal 62 ile
                // 116 arasında salınıyor, yani ortalamanın ±%28'i. Dikiş
                // çizgisi ayrıca koyultuyor.
                float light = 1f + facet * 0.17f - seam * 0.20f;
                byte c = (byte)Mathf.Clamp(Mathf.RoundToInt(255f * light), 0, 255);
                pixels[y * s + x] = new Color32(c, c, c, 255);
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---- Buz (çalar saat yardımcısı) ----------------------------------

        static Sprite _frostVignette, _snowflake;

        /// <summary>
        /// Süre donduğunda ekranın DÖRT KENARINDAN içeri sönen buzlu parlama.
        ///
        /// Referans ölçümü (`menus,powerups,vs.mp4` 01:25, 384x832 kare): sol
        /// kenarda x=2'de (77,196,226), x=8'de (27,84,128), x=22'de zemin
        /// rengi. Üstte y=0'da (16,142,155), y=50'de zemin. Yani parlama
        /// yatayda 22/384, dikeyde ~50/832 — İKİSİ DE ~%6. Kenar payı
        /// piksel olarak değil ORAN olarak eşit; bu yüzden tek bir kare doku
        /// ekrana esnetildiğinde referansı birebir verir, en-boy oranından
        /// bağımsız olarak.
        ///
        /// DERS (sönüm eğrisini ÖLÇ, tahmin etme): İlk hâli dört düz şeritti;
        /// kenarlar keskin bittiği için "buz" değil "çerçeve" gibi okunuyordu.
        /// Ölçüm sönümün doğrusal olmadığını söylüyor: bandın %35'inde alfa
        /// tepe değerin %48'i (doğrusal olsa %65 olurdu). Karesi alınmış
        /// sönüm (1-t)² tam bu eğriyi veriyor.
        ///
        /// Renk TEK TON: ilk denemede en dış piksellere beyaza çalan bir şerit
        /// koymuştum, çünkü referansın SOL kenarı (77,196,226) beyazımsı
        /// görünüyor. Ama ÜST kenarı (16,142,155) saf camgöbeği — yani o
        /// beyazlık vinyetin değil, tahtanın kendi parlamasının. Şeridi
        /// koyunca bizim üst kenarımız (135,195,205) çıktı, referansın iki
        /// katı parlak. Tek tonda kalmak doğrusu.
        /// </summary>
        public static Sprite FrostVignette => _frostVignette != null ? _frostVignette
            : (_frostVignette = BuildFrostVignette());

        static Sprite BuildFrostVignette()
        {
            const int s = 128;
            const float Band = 0.06f;        // ölçüm: kenar payı ekranın %6'sı

            var tex = NewTexture("FrostVignette", s, s);
            var pixels = new Color32[s * s];

            // Referansın üst kenarından geri çözülen ton: (16,142,155) rengi
            // (24,21,51) zeminin üstüne α=0.62 ile bindirilmiş.
            var frost = new Color(0.078f, 0.843f, 0.859f);   // #14D7DB

            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                // Piksel MERKEZİNDEN en yakın kenara uzaklık, 0-1 aralığında.
                float u = (x + 0.5f) / s;
                float v = (y + 0.5f) / s;
                float d = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));

                // Sönüm DOĞRUSAL DEĞİL: ölçümde bandın %35'inde alfa tepe
                // değerin %48'i (doğrusal olsa %65 olurdu).
                float t = Mathf.Clamp01(d / Band);
                float a = (1f - t) * (1f - t);

                pixels[y * s + x] = new Color(frost.r, frost.g, frost.b, a);
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Altı kollu kar tanesi — donma göstergesinin ikonu ve serpilen
        /// parçacıklar aynı görseli kullanır.
        ///
        /// Kollar açı ile çiziliyor: her piksel en yakın kol eksenine olan
        /// dik uzaklığına bakıyor. Böylece altı kol da tek döngüde çıkıyor,
        /// altı ayrı dikdörtgen döndürmeye gerek kalmıyor.
        /// </summary>
        public static Sprite Snowflake => _snowflake != null ? _snowflake
            : (_snowflake = BuildSnowflake());

        static Sprite _sparkle;

        /// <summary>
        /// DÖRT UÇLU PARILTI — ödül halelerinin çevresindeki küçük yıldız.
        ///
        /// Referansta (PERFECT kartı, jeton yığınının çevresi) üç-dört tane
        /// var: içbükey kollu, uçları sivri, saf beyaz. Kar tanesinden farkı
        /// kol SAYISI değil BİÇİMİ — kar tanesinin kolları sabit kalınlıkta
        /// çubuk, parıltının kolları merkezden uca doğru incelen bir eğri.
        /// Bu incelme parıltıyı "ışık", sabit kalınlık ise "nesne" gösteriyor.
        ///
        /// Kollar süperelips ile: |x|^p + |y|^p = r^p, p &lt; 1 olduğunda
        /// kenarlar içeri çöküyor ve dört sivri uç çıkıyor. Tek formül, dört
        /// kol, döndürme yok.
        /// </summary>
        public static Sprite Sparkle => _sparkle != null ? _sparkle
            : (_sparkle = BuildSparkle());

        static Sprite BuildSparkle()
        {
            const int s = 96;
            var tex = NewTexture("Sparkle", s, s);
            var pixels = new Color32[s * s];

            float half = s * 0.5f;
            // p = 0.42: belirgin içbükey kol. 1 = elmas, 2 = daire.
            const float p = 0.42f;
            const float radius = 0.46f;

            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float px = (x + 0.5f - half) / s;
                float py = (y + 0.5f - half) / s;

                float d = Mathf.Pow(Mathf.Abs(px), p) + Mathf.Pow(Mathf.Abs(py), p);
                float edge = Mathf.Pow(radius, p);
                // Kenar yumuşatma: eşiğin çevresinde dar bir bant.
                float alpha = 1f - Step(edge * 0.92f, edge * 1.08f, d);
                pixels[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect);
        }

        static Sprite BuildSnowflake()
        {
            const int s = 96;
            var tex = NewTexture("Snowflake", s, s);
            var pixels = new Color32[s * s];

            float half = s * 0.5f;
            const float ArmLength = 0.46f;   // yarıçap payı
            const float ArmWidth = 0.052f;
            const float BranchAt = 0.58f;    // dalların koldaki yeri
            const float BranchLen = 0.17f;

            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float px = (x + 0.5f - half) / s;
                float py = (y + 0.5f - half) / s;

                float mask = 0f;
                for (int arm = 0; arm < 6; arm++)
                {
                    float angle = arm * Mathf.PI / 3f;
                    float ax = Mathf.Cos(angle), ay = Mathf.Sin(angle);

                    // Kolun kendi eksenine izdüşüm (along) ve dik uzaklık (side).
                    float along = px * ax + py * ay;
                    float side = Mathf.Abs(-px * ay + py * ax);
                    if (along < 0f) continue;

                    if (along <= ArmLength)
                        mask = Mathf.Max(mask, 1f - Step(ArmWidth * 0.7f, ArmWidth, side));

                    // İki yan dal: koldan 60° ayrılan kısa çubuklar.
                    float root = ArmLength * BranchAt;
                    float bx = px - ax * root, by = py - ay * root;
                    for (int sign = -1; sign <= 1; sign += 2)
                    {
                        float ba = angle + sign * Mathf.PI / 3f;
                        float dx = Mathf.Cos(ba), dy = Mathf.Sin(ba);
                        float bAlong = bx * dx + by * dy;
                        float bSide = Mathf.Abs(-bx * dy + by * dx);
                        if (bAlong < 0f || bAlong > BranchLen) continue;
                        mask = Mathf.Max(mask,
                            1f - Step(ArmWidth * 0.5f, ArmWidth * 0.78f, bSide));
                    }
                }

                // Ortadaki küçük göbek kolları birbirine bağlar.
                float r = Mathf.Sqrt(px * px + py * py);
                mask = Mathf.Max(mask, 1f - Step(ArmWidth * 1.5f, ArmWidth * 2.1f, r));

                pixels[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(mask));
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Texture2D NewTexture(string name, int w, int h) =>
            new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

        public static void ClearCache()
        {
            _capsule = _capsuleOutline = _awning = _infinity = _noAds = null;
            _fadeDown = _pennant = _ring = _sunburst = _foliage = null;
            _quilt = _frostVignette = _snowflake = _sparkle = null;
        }
    }
}
