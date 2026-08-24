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
        /// Kullanıcının "arkaplan çok sade kalmış, orada doku filan var"
        /// dediği şey bu (11. APK bulgusu).
        ///
        /// DERS (doku RENK DEĞİL, IŞIKTIR): Deseni renkli çizip Image'ı
        /// boyamak, boyama çarpma olduğu için deseni de renklendirirdi ve her
        /// bölümde farklı bir ton çıkardı. Bunun yerine doku BEYAZ üstüne
        /// yalnız parlaklık farkı olarak çiziliyor; bölüm rengi tint ile
        /// veriliyor. Böylece tek doku üç bölümde de doğru çalışıyor.
        ///
        /// DERS (ayrıştırılabilir dalga EŞKENAR DÖRTGEN VERMEZ — 14. tur,
        /// M4h): Bu doku bir tur `cos(x+y) + cos(x-y)` ile yazılmıştı, "iki
        /// köşegen dalganın toplamı eşkenar dörtgen ızgara verir" diye. Ama
        /// toplama formülü gereği bu ifade `2·cos(x)·cos(y)`ye eşit — yani
        /// ÇARPANLARINA AYRILIYOR ve x ile y'de ayrı ayrı periyodik, eksen
        /// hizalı KARE bir ızgara çiziyor. Ekranda da yuvarlak köşeli kareler
        /// çıkıyordu; kullanıcı "aralarındaki kahverengi arkaplanın deseni
        /// orijinaldeki gibi olmalı" derken bunu gördü. 45°'lik bir kafes,
        /// dalga toplamıyla değil, `x+y` ve `x-y` doğrularına olan DİK
        /// MESAFEYLE kurulur.
        ///
        /// ÖLÇÜM (`market_ref.png`, y 940..1015 — kartların arasındaki
        /// kahverengi bölge):
        ///   • Kafes 45°: bir çizgi y 960->1010 arasında x 43->91'e kayıyor,
        ///     yani eğim 1.
        ///   • Yatay adım 222 referans piksel (çizgi merkezleri 72, 294, 516,
        ///     737). 946 piksel genişlik 1080 kanvas birimine geldiği için
        ///     ekranda 253 birim.
        ///   • Çizgi merkezi 59, zemin medyanı 100, yüzeylerin en açığı 116,
        ///     en koyusu 70 (3. ve 97. yüzdelik).
        ///   • Çizginin ÜST-SOL yanında ince bir parlama var (x=66'da 108,
        ///     merkezden 6 piksel ötede).
        /// </summary>
        public static Sprite Quilt => _quilt != null ? _quilt : (_quilt = BuildQuilt());

        static Sprite BuildQuilt()
        {
            // Döşeme karesinin kenarı, kafesin YATAY ADIMIYLA aynı olmalı:
            // `x+y` ailesinin ardışık iki doğrusu arasındaki yatay mesafe s,
            // `x-y` ailesininki de s. Kare doku o yüzden kusursuz döşeniyor.
            const int s = 256;
            var tex = NewTexture("ShopQuilt", s, s);
            tex.wrapMode = TextureWrapMode.Repeat;

            // Referans ölçüsü doku pikseline çevrildi: 253 kanvas birimi = 256
            // doku pikseli, yani bire bir sayılabilir. Çizginin dik yarı
            // kalınlığı yatayda 7 piksel ölçüldü; 45°'de dik mesafe bunun
            // 1/√2'si.
            const float SeamYari  = 7f / 1.41421f;   // ~4.95
            const float ParlaBas  = SeamYari;        // parlama dikişin hemen dışında
            const float ParlaBit  = SeamYari + 4.5f;

            var pixels = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                // İki doğru ailesine DİK mesafe. Doğrular: x+y = k·s ve
                // x-y = k·s. Dik mesafe |x±y - k·s| / √2.
                float a = Mathf.Repeat(x + y, s);        // 0..s
                float b = Mathf.Repeat(x - y, s);
                float d1 = Mathf.Min(a, s - a) * 0.70711f;
                float d2 = Mathf.Min(b, s - b) * 0.70711f;
                float d  = Mathf.Min(d1, d2);            // en yakın dikişe uzaklık

                // Dikiş: merkezde en koyu, SeamYari'de zemine dönüyor.
                float seam = 1f - Step(0f, SeamYari, d);

                // Dikişin dışındaki ince parlama (referansta merkezden 6
                // piksel ötede 108, zemin 100).
                float parla = Step(ParlaBas, (ParlaBas + ParlaBit) * 0.5f, d)
                            * (1f - Step((ParlaBas + ParlaBit) * 0.5f, ParlaBit, d));

                // YÜZEY IŞIĞI: her eşkenar dörtgen kendi içinde üstten alta
                // hafifçe koyulaşıyor (referansta 102 -> 93). Hücre içindeki
                // konum a ve b'den çıkıyor; a veya b sarmaladığı yerde ışık
                // sıçrıyor ama orası zaten dikişin koyu şeridi, görünmüyor.
                float t = (a / s + b / s) * 0.5f;        // 0..1
                float yuzey = 1.06f - 0.13f * t;

                float light = yuzey * (1f - 0.41f * seam) + 0.085f * parla;
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

        // ---- Paket kartinin yuzu ve bandi (14. tur, M4f) -------------------

        static Sprite _packFace, _packBand;

        /// <summary>
        /// Paket kartının KREM YÜZÜ — üstte krem, altta rafın kumu, en altta
        /// rafın ön kenarı.
        ///
        /// Kullanıcı: *"o panelin starter pack kısmına gelmeden o aradaki
        /// çizgi görünüm geçişi daha iyi hale getirilmeli."*
        ///
        /// Bizimki ÜÇ DÜZ LEVHAYDI ve ölçüm bunu net gösteriyor — kartın
        /// dikey kesitinde renk hiç değişmiyor, sonra bir anda sıçrıyor:
        ///     krem (251,241,226) ... sabit
        ///     raf  (229,208,183) ... sabit
        ///     mor  (120,17,169)  ... sabit
        /// Referansın aynı kesiti ise sürekli:
        ///     %0..%26   (250,241,226) düz krem
        ///     %26..%80  krem yavaşça (245,215,177) kumuna dönüyor
        ///     %84..%89  (237,199,156) — nesnelerin rafa düşen gölgesi
        ///     %94..%95  (250,228,204) — rafın ışık alan burnu
        ///     %96..%100 (211,148,87)  — rafın ÖN KENARI, kalınlığı
        ///
        /// Yani referansta "geçiş" diye bir çizgi yok; bir RAF var ve rafın
        /// kalınlığı görünüyor. Kullanıcının "çizgi" dediği şey de o kalınlığın
        /// eksikliği: iki düz levha yan yana gelince aradaki sınır çizgi gibi
        /// okunuyor.
        ///
        /// DERS (bir sınırı yumuşatmanın yolu onu BULANIKLAŞTIRMAK değil,
        /// KALINLIK vermektir): İlk refleks iki levha arasına yumuşak bir
        /// geçiş koymak olurdu; o da sınırı siler ve yüzey sünger gibi durur.
        /// Referans tam tersini yapıyor — sınırı KESKİN bırakıyor ama önüne
        /// bir ışık burnu, arkasına bir gövde kalınlığı koyuyor. Göz o zaman
        /// "iki renk" değil "bir raf" görüyor.
        ///
        /// Üst köşeler yuvarlak, alt köşeler kare: yüz bandın üstüne oturuyor.
        /// </summary>
        public static Sprite PackFace => _packFace != null ? _packFace : (_packFace = BuildPackFace());

        static Sprite BuildPackFace()
        {
            const int W = 128, H = 317;        // yükseklik = referans yüzü, birim birim
            const float R = 30f;               // üst köşe yarıçapı

            var duraklar = new[]
            {
                (0.000f, new Color32(211, 148,  87, 255)),   // en alt: rafın ön kenarı
                (0.020f, new Color32(219, 165, 108, 255)),
                (0.040f, new Color32(250, 228, 204, 255)),   // rafın ışık alan burnu
                (0.062f, new Color32(243, 208, 166, 255)),
                (0.110f, new Color32(237, 199, 156, 255)),   // nesne gölgesi
                (0.160f, new Color32(245, 215, 177, 255)),
                (0.200f, new Color32(245, 215, 177, 255)),   // rafın kumu
                (0.450f, new Color32(246, 229, 209, 255)),
                (0.740f, new Color32(248, 239, 224, 255)),
                (1.000f, new Color32(250, 241, 226, 255)),
            };

            var tex = NewTexture("PackFace", W, H);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                Color c = Duraktan(duraklar, y / (float)(H - 1));
                for (int x = 0; x < W; x++)
                {
                    // Yalnız ÜST köşeler yuvarlak: alt kenar bandın üstünde
                    // duruyor, orada yuvarlaklık bir boşluk açardı.
                    float d = UstKose(x + 0.5f, y + 0.5f, W, H, R);
                    var k = c; k.a = Mathf.Clamp01(0.7f - d);
                    px[y * W + x] = k;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            // 9-dilim yalnız YATAYDA: profil dikey olduğu için orta satırlar
            // gerilemez, ama kart genişliği ekrana göre değiştiğinden yatayda
            // gerilmesi şart.
            _packFace = Sprite.Create(tex, new Rect(0f, 0f, W, H), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(R + 2f, 0f, R + 2f, 0f));
            _packFace.name = "PackFace";
            _packFace.hideFlags = HideFlags.HideAndDontSave;
            return _packFace;
        }

        /// <summary>
        /// Paket kartının MOR BANDI — üstte kendi kalınlığının koyu çizgisi,
        /// hemen altında ışık alan parlak şerit, sonra aşağı doğru sönen gövde.
        ///
        /// ÖLÇÜM (`market_ref.png`, x=200, bant y 1456..1607):
        ///     %0    (102,17,134)  koyu üst kenar
        ///     %8    (219,74,255)  parlak şerit — ışığın vurduğu yer
        ///     %11   (171,26,219)  gövdenin üstü
        ///     %70   (146,22,206)  gövdenin altı (sönüyor)
        ///     %93   (124,16,161)
        ///     %100  ( 72, 3,100)  alt kenar
        ///
        /// Bizimki tek düz mordu (141,23,198) ve ayrıca alt %5.5'e elle
        /// koyduğumuz bir "dudak" vardı. Referansta dudak ALTTA DEĞİL ÜSTTE:
        /// bandın üstünde görünen şey, yüzün altındaki kalınlık.
        ///
        /// Üst köşeler kare, alt köşeler yuvarlak.
        /// </summary>
        public static Sprite PackBand => _packBand != null ? _packBand : (_packBand = BuildPackBand());

        static Sprite BuildPackBand()
        {
            const int W = 128, H = 174;
            const float R = 30f;

            var duraklar = new[]
            {
                (0.000f, new Color32( 72,   3, 100, 255)),   // en alt
                (0.030f, new Color32(124,  16, 161, 255)),
                (0.070f, new Color32(140,  20, 198, 255)),
                (0.300f, new Color32(146,  22, 206, 255)),
                (0.700f, new Color32(168,  25, 215, 255)),
                (0.890f, new Color32(171,  26, 219, 255)),
                (0.921f, new Color32(219,  74, 255, 255)),   // parlak şerit
                (0.947f, new Color32(183,  39, 225, 255)),
                (0.974f, new Color32(129,  15, 191, 255)),
                (1.000f, new Color32(102,  17, 134, 255)),   // koyu üst kenar
            };

            var tex = NewTexture("PackBand", W, H);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                Color c = Duraktan(duraklar, y / (float)(H - 1));
                for (int x = 0; x < W; x++)
                {
                    float d = AltKose(x + 0.5f, y + 0.5f, W, H, R);
                    var k = c; k.a = Mathf.Clamp01(0.7f - d);
                    px[y * W + x] = k;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            _packBand = Sprite.Create(tex, new Rect(0f, 0f, W, H), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(R + 2f, 0f, R + 2f, 0f));
            _packBand.name = "PackBand";
            _packBand.hideFlags = HideFlags.HideAndDontSave;
            return _packBand;
        }

        static Sprite _offerFace, _offerBand;

        /// <summary>
        /// TEKLİF kartının turuncu yüzü — <see cref="PackFace"/> ile aynı raf
        /// mantığı, kartın kendi paletinde (M4f).
        ///
        /// Kullanıcının şikâyeti asıl BU kart içindi: *"o panelin starter pack
        /// kısmına gelmeden o aradaki çizgi görünüm geçişi daha iyi hale
        /// getirilmeli."* Turuncu sanat alanı ile "Starter Pack" bandı iki düz
        /// levhaydı ve aradaki sınır çizgi gibi okunuyordu.
        ///
        /// Paket kartında referanstan ÖLÇÜLEN raf profili şu üç öge:
        /// nesnelerin düştüğü gölge, rafın ışık alan burnu, rafın ön kenarı.
        /// Referansta teklif kartının turuncu hâli yok (oradaki kart bambaşka
        /// bir premium paket), o yüzden burada ÖLÇÜM DEĞİL ORAN taşındı:
        /// aynı üç öge, taban rengin çevresinde aynı açıklık/koyuluk
        /// çarpanlarıyla.
        /// </summary>
        public static Sprite OfferFace => _offerFace != null ? _offerFace : (_offerFace = BuildOfferFace());

        static Sprite BuildOfferFace()
        {
            const int W = 128, H = 418;
            const float R = 30f;

            var duraklar = new[]
            {
                (0.000f, new Color32(176,  78,   8, 255)),   // rafın ön kenarı
                (0.018f, new Color32(198,  96,  12, 255)),
                (0.036f, new Color32(255, 190,  96, 255)),   // rafın ışık alan burnu
                (0.056f, new Color32(250, 160,  45, 255)),
                (0.100f, new Color32(226, 122,  18, 255)),   // nesne gölgesi
                (0.150f, new Color32(245, 137,  20, 255)),
                (0.450f, new Color32(249, 160,  12, 255)),
                (1.000f, new Color32(252, 186,   5, 255)),
            };

            var tex = NewTexture("OfferFace", W, H);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                Color c = Duraktan(duraklar, y / (float)(H - 1));
                for (int x = 0; x < W; x++)
                {
                    float d = UstKose(x + 0.5f, y + 0.5f, W, H, R);
                    var k = c; k.a = Mathf.Clamp01(0.7f - d);
                    px[y * W + x] = k;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            _offerFace = Sprite.Create(tex, new Rect(0f, 0f, W, H), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(R + 2f, 0f, R + 2f, 0f));
            _offerFace.name = "OfferFace";
            _offerFace.hideFlags = HideFlags.HideAndDontSave;
            return _offerFace;
        }

        /// <summary>
        /// Teklif kartının adı ve fiyatı taşıyan bandı. Yüksekliğe kartın
        /// ALT KALINLIĞI da dahil: eskiden bu iki ayrı paneldi (koyu bir
        /// "Body" ve üstünde bandın kendisi) ve aralarındaki sınır yine bir
        /// çizgi bırakıyordu. Kalınlık artık bandın kendi profilinin altı.
        /// </summary>
        public static Sprite OfferBand => _offerBand != null ? _offerBand : (_offerBand = BuildOfferBand());

        static Sprite BuildOfferBand()
        {
            const int W = 128, H = 204;        // 172 bant + 32 alt kalınlık
            const float R = 30f;

            var duraklar = new[]
            {
                (0.000f, new Color32(110,  36,   4, 255)),   // alt kenar
                (0.060f, new Color32(140,  48,   6, 255)),
                (0.140f, new Color32(176,  62,   9, 255)),
                (0.157f, new Color32(214,  82,  12, 255)),   // kalınlık biter, gövde başlar
                (0.400f, new Color32(226,  88,  14, 255)),
                (0.760f, new Color32(237,  96,  17, 255)),
                (0.905f, new Color32(243, 104,  22, 255)),
                (0.930f, new Color32(255, 158,  92, 255)),   // parlak şerit
                (0.955f, new Color32(250, 122,  40, 255)),
                (0.978f, new Color32(196,  66,   8, 255)),
                (1.000f, new Color32(150,  44,   4, 255)),   // koyu üst kenar
            };

            var tex = NewTexture("OfferBand", W, H);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                Color c = Duraktan(duraklar, y / (float)(H - 1));
                for (int x = 0; x < W; x++)
                {
                    float d = AltKose(x + 0.5f, y + 0.5f, W, H, R);
                    var k = c; k.a = Mathf.Clamp01(0.7f - d);
                    px[y * W + x] = k;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            _offerBand = Sprite.Create(tex, new Rect(0f, 0f, W, H), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(R + 2f, 0f, R + 2f, 0f));
            _offerBand.name = "OfferBand";
            _offerBand.hideFlags = HideFlags.HideAndDontSave;
            return _offerBand;
        }

        static Sprite _tabCard;

        /// <summary>
        /// Alt çubuktaki SEÇİLİ sekme kartı — dudağı kenar mesafesine göre
        /// boyanmış tek görsel (N1).
        ///
        /// ÖLÇÜM (8 kare ortalaması, kartın SOL kenarından yatay kesit;
        /// 1 kare pikseli = 2 kanvas birimi):
        ///     0-4    ( 28,  5, 131)  koyu dış kenar
        ///     8-12   ( 56, 33, 164)  ince ışık
        ///     12-16  ( 38, 17, 146)  KOYU OYUK
        ///     20-24  ( 64, 45, 180)
        ///     28-32  (113, 94, 246)  parlak iç rim
        ///     32+    ( 93, 70, 245)  YÜZ — düz, gradyansız
        ///
        /// Yani kartın dudağı, çubuğun üst dudağının AYNISI: aynı dört bant,
        /// yalnız 90° dönmüş ve kartın dört kenarını da dolanıyor.
        ///
        /// Eskiden üç katmandı: koyu rim + açık yüz + yüzün üstünde
        /// `FadeDown` ışığı (ve ışığı kırpmak için yüze bir `Mask`).
        /// İki hata vardı:
        ///   • Yüz (107,101,249) idi, referans (93,70,245) — yeşil kanal 31
        ///     fazla, yani mor lavantaya kaçıyordu (aynı hata çubuğun
        ///     gövdesinde de vardı, bkz. N2).
        ///   • Referansta yüzün üstünde ışık YOK; kesitte üst (91,72,243),
        ///     alt (94,70,246) — düz. Bizim eklediğimiz `Sheen` kartı
        ///     referansta olmayan bir parlaklıkla yıkıyordu.
        ///
        /// 9-dilim payı yarıçap + 2: dudak köşe bölgesinde, orta yalnız düz
        /// yüz olduğu için serbestçe geriliyor.
        ///
        /// DERS (bir dudağı ÜÇ katmanla değil, mesafeyle boya): Koyu bant +
        /// açık yüz iki dikdörtgendir; köşede bandın kalınlığı kaçınılmaz
        /// olarak değişir ve yarıçapları eşitlemek uğraşına düşülür (7. ve
        /// 8. turların ikisi de bu yüzden harcandı). Renk kenar mesafesinden
        /// geliyorsa bant her yerde, köşede de, tam ölçüldüğü kalınlıkta.
        /// </summary>
        public static Sprite TabCard => _tabCard != null ? _tabCard : (_tabCard = BuildTabCard());

        static Sprite BuildTabCard()
        {
            const int W = 160, H = 160;
            const float R = 44f;

            // Kenardan içeri mesafeye göre renk (birim = piksel).
            var duraklar = new[]
            {
                ( 0f, new Color32( 28,   5, 131, 255)),
                ( 4f, new Color32( 41,  20, 132, 255)),
                ( 8f, new Color32( 56,  33, 164, 255)),
                (12f, new Color32( 38,  17, 146, 255)),
                (16f, new Color32( 50,  31, 155, 255)),
                (20f, new Color32( 64,  45, 180, 255)),
                (24f, new Color32( 96,  77, 223, 255)),
                (28f, new Color32(113,  94, 246, 255)),
                (32f, new Color32( 93,  70, 245, 255)),
            };

            var tex = NewTexture("TabCard", W, H);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float d = Kose(x + 0.5f, y + 0.5f, W, H, R);   // negatif = içeride
                float ic = -d;                                  // kenardan içeri
                Color c;
                if (ic >= 32f) c = duraklar[duraklar.Length - 1].Item2;
                else
                {
                    int i = 1;
                    while (i < duraklar.Length && ic > duraklar[i].Item1) i++;
                    if (i >= duraklar.Length) i = duraklar.Length - 1;
                    float k = (ic - duraklar[i - 1].Item1)
                            / Mathf.Max(1e-5f, duraklar[i].Item1 - duraklar[i - 1].Item1);
                    c = Color.Lerp(duraklar[i - 1].Item2, duraklar[i].Item2, Mathf.Clamp01(k));
                }
                c.a = Mathf.Clamp01(0.7f - d);
                px[y * W + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            _tabCard = Sprite.Create(tex, new Rect(0f, 0f, W, H), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect,
                new Vector4(R + 2f, R + 2f, R + 2f, R + 2f));
            _tabCard.name = "TabCard";
            _tabCard.hideFlags = HideFlags.HideAndDontSave;
            return _tabCard;
        }

        /// <summary>
        /// Yuvarlak köşeli dikdörtgene işaretli mesafe; negatif = içeride.
        /// Standart kutu-SDF: köşe bölgesinde Öklid, kenarlarda dik mesafe.
        /// </summary>
        static float Kose(float x, float y, float w, float h, float r)
        {
            float qx = Mathf.Max(r - x, x - (w - r));
            float qy = Mathf.Max(r - y, y - (h - r));
            float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static Sprite _tabRim;

        /// <summary>
        /// Alt sekme çubuğunun ÜST DUDAĞI — 40 birimlik dikey profil (N1).
        ///
        /// Kullanıcı: *"alt menü daha iyi oldu okey, ama arkaplan orjinal
        /// oyundakinin birebir aynısı alınabilir; bizdekinin dış kenarları
        /// görünümü şuan kötü, birebir orjinalini alalım."*
        ///
        /// Bizimki DÖRT DÜZ ŞERİTTİ (koyu kenar 15, ışık 4, oyuk 11,
        /// parlaklık 9 birim). Referansın aynı bölgesi ise sürekli bir
        /// profil ve toplam 30 birim — bizimki hem %30 kalın hem basamaklı
        /// duruyordu; "dış kenarları kötü" denen şey o basamaklar.
        ///
        /// ÖLÇÜM: 52 menü karesinden aynı durumu gösteren 8'i ortalandı
        /// (kareler arası std 3.5 -> ortalama sonrası kodek gürültüsü yok).
        /// Kare 443x960, kanvas 1920 birim yüksek: 1 kare pikseli = 2 birim.
        /// Çubuğun üstü ekranın altından 186 birimde. Profil (üstten aşağı,
        /// birim):
        ///     0-2    (34,26,94)   koyu dış kenar
        ///     4-8    (58,37,164)  ince ışık şeridi
        ///     10-13  (32,12,134)  KOYU OYUK
        ///     14-20  (60,34,180)  yükselen
        ///     22-25  (101,74,235) ÜST PARLAKLIK (tepe)
        ///     26-29  (73,44,219)  gövdeye iniş
        ///     30+    (73,44,219)  gövde
        ///
        /// DERS (düz şerit yığını, gradyanın ucuz taklidi değildir): Dört
        /// şerit doğru RENKLERİ taşıyordu ama aralarındaki sıçramalar
        /// telefonda çizgi olarak görünüyor. Profil ölçülebiliyorsa doku
        /// olarak çizilmeli; şerit ancak profil GERÇEKTEN basamaklıysa
        /// doğrudur.
        /// </summary>
        public static Sprite TabRim => _tabRim != null ? _tabRim : (_tabRim = BuildTabRim());

        static Sprite BuildTabRim()
        {
            // Üstten aşağı, birim birim ölçülen profil.
            var profil = new Color32[]
            {
                new Color32( 34,  26,  94, 255), new Color32( 37,  25, 115, 255),
                new Color32( 39,  24, 136, 255), new Color32( 44,  28, 150, 255),
                new Color32( 50,  32, 163, 255), new Color32( 54,  35, 164, 255),
                new Color32( 58,  37, 164, 255), new Color32( 56,  36, 161, 255),
                new Color32( 55,  35, 158, 255), new Color32( 44,  24, 146, 255),
                new Color32( 32,  12, 134, 255), new Color32( 32,  11, 136, 255),
                new Color32( 33,  11, 138, 255), new Color32( 41,  18, 150, 255),
                new Color32( 50,  26, 163, 255), new Color32( 55,  30, 172, 255),
                new Color32( 60,  34, 180, 255), new Color32( 59,  33, 182, 255),
                new Color32( 58,  32, 184, 255), new Color32( 62,  36, 190, 255),
                new Color32( 67,  40, 196, 255), new Color32( 80,  54, 211, 255),
                new Color32( 94,  68, 226, 255), new Color32( 98,  71, 230, 255),
                new Color32(101,  74, 235, 255), new Color32( 90,  62, 226, 255),
                new Color32( 79,  51, 218, 255), new Color32( 74,  46, 215, 255),
                new Color32( 70,  41, 212, 255), new Color32( 71,  42, 215, 255),
                new Color32( 73,  44, 219, 255), new Color32( 73,  44, 219, 255),
                new Color32( 73,  44, 219, 255), new Color32( 73,  44, 219, 255),
                new Color32( 73,  44, 219, 255), new Color32( 73,  44, 219, 255),
                new Color32( 73,  44, 219, 255), new Color32( 73,  44, 219, 255),
                new Color32( 73,  44, 219, 255), new Color32( 73,  44, 219, 255),
            };

            // GÖVDE DE AYNI DOKUDA (14. tur). Önce dudak ayrı görsel, gövde
            // ayrı bir `FadeDown` katmanıydı; sönüm kutusu çubuğun EKRAN
            // DIŞINA taşan payını da kapsadığı için görünen alanda hiç
            // uygulanmıyordu — ölçümde gövde baştan sona (73,42,219) çıktı,
            // referansta ise altta (60,34,180). Profil artık çubuğun görünen
            // 186 biriminin TAMAMI; taşan pay çubuğun kendi düz rengiyle
            // doluyor.
            const int W = 8;
            const int H = 186;
            var tex = NewTexture("TabBarFace", W, H);
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                int ust = H - 1 - y;                 // doku y=0 ALTTIR
                Color32 c;
                if (ust < profil.Length) c = profil[ust];
                else
                {
                    // Gövde: (73,44,219) -> (60,34,180), ölçülen sönüm.
                    float t = (ust - profil.Length) / (float)(H - 1 - profil.Length);
                    c = Color32.Lerp(new Color32(73, 44, 219, 255),
                                     new Color32(60, 34, 180, 255), t);
                }
                for (int x = 0; x < W; x++) px[y * W + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            _tabRim = Sprite.Create(tex, new Rect(0f, 0f, W, H), new Vector2(0.5f, 0.5f), 100f);
            _tabRim.name = "TabBarFace";
            _tabRim.hideFlags = HideFlags.HideAndDontSave;
            return _tabRim;
        }

        static Sprite _softGlow;

        /// <summary>
        /// Kartın yüzünde, sanat eserinin ARKASINDAKİ yumuşak hale (M5).
        ///
        /// Kullanıcı: *"bu paketlerin arkaplanı beyaz kısmı, orada bi kağıt
        /// dokuluymuş gibi, düz beyazdan ziyade öyle ya; onu yapabiliyorsak
        /// yapalım."*
        ///
        /// İlk varsayım "kâğıt taneciği ekle" idi. ÖLÇÜM bunu çürüttü:
        /// referansın kreminde satır/sütun gradyanı çıkarıldığında kalan
        /// sapma std 0,1 — yani yüzey pürüzsüz, tanecik YOK. Mavi kanalı
        /// ±8 birimlik pencereye gerip baktım, ışın deseni de yok.
        ///
        /// Gerçekte olan şey tek bir şey: kesenin arkasında YUMUŞAK BİR HALE.
        ///     kesenin yanı (x=160)  (255,248,240)
        ///     uzağı      (x=460)    (248,239,224)
        /// Kırmızı doygun, yeşil +9, mavi +16 — yani SOĞUK beyaz bir ışık.
        /// Beyazı %55 alfayla eklemek tam bu farkı veriyor.
        ///
        /// DERS ("doku" sözcüğü tanecik demek değildir): Kullanıcı düz bir
        /// yüzeyin cansız durduğunu söylüyor; çözümün gürültü olduğunu
        /// varsaymak kolay. Referansı ölçünce derinliği verenin gürültü
        /// değil, tek bir ışık kaynağı olduğu çıktı. Gürültü eklemek hem
        /// yanlış olurdu hem de ASTC'yi bozardı.
        /// </summary>
        public static Sprite SoftGlow => _softGlow != null ? _softGlow : (_softGlow = BuildSoftGlow());

        static Sprite BuildSoftGlow()
        {
            const int S = 128;
            var tex = NewTexture("SoftGlow", S, S);
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S * 2f - 1f;
                float dy = (y + 0.5f) / S * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                // Karesi alınmış sönüm: referansta hale merkeze yakın hızlı,
                // dışta yavaş kapanıyor.
                float a = Mathf.Clamp01(1f - r); a *= a;
                px[y * S + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            _softGlow = Sprite.Create(tex, new Rect(0f, 0f, S, S), new Vector2(0.5f, 0.5f), 100f);
            _softGlow.name = "SoftGlow";
            _softGlow.hideFlags = HideFlags.HideAndDontSave;
            return _softGlow;
        }

        /// <summary>Duraklı renk rampası; t alttan üste 0..1.</summary>
        static Color Duraktan((float t, Color32 c)[] duraklar, float t)
        {
            if (t <= duraklar[0].t) return duraklar[0].c;
            for (int i = 1; i < duraklar.Length; i++)
            {
                if (t > duraklar[i].t) continue;
                float k = (t - duraklar[i - 1].t) / Mathf.Max(1e-5f, duraklar[i].t - duraklar[i - 1].t);
                return Color.Lerp(duraklar[i - 1].c, duraklar[i].c, k);
            }
            return duraklar[duraklar.Length - 1].c;
        }

        /// <summary>
        /// Yalnız ÜST köşeleri yuvarlak dikdörtgene işaretli mesafe.
        /// Negatif = içeride. Doku koordinatında y=0 ALTTIR.
        /// </summary>
        static float UstKose(float x, float y, float w, float h, float r)
        {
            float cx = Mathf.Max(Mathf.Max(r - x, 0f), Mathf.Max(x - (w - r), 0f));
            float cy = Mathf.Max(y - (h - r), 0f);
            float dis = Mathf.Sqrt(cx * cx + cy * cy) - r;
            return Mathf.Max(dis, Mathf.Max(Mathf.Max(-x, x - w), -y));
        }

        /// <summary>Yalnız ALT köşeleri yuvarlak dikdörtgene işaretli mesafe.</summary>
        static float AltKose(float x, float y, float w, float h, float r)
        {
            float cx = Mathf.Max(Mathf.Max(r - x, 0f), Mathf.Max(x - (w - r), 0f));
            float cy = Mathf.Max(r - y, 0f);
            float dis = Mathf.Sqrt(cx * cx + cy * cy) - r;
            return Mathf.Max(dis, Mathf.Max(Mathf.Max(-x, x - w), y - h));
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
            _packFace = _packBand = _offerFace = _offerBand = _softGlow = null;
            _tabRim = _tabCard = null;
        }
    }
}
