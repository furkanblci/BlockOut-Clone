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
            var dark  = new Color(0.000f, 0.424f, 0.914f);   // ekranda ≈ #0066DC
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
            float depth = AwningH * 0.140f;
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
                    float cx = (stripe + 0.5f) * stripeW;
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                               new Vector2(cx, scallopR));

                    float alpha = y >= depth ? 1f : 1f - Step(scallopR - 1.2f, scallopR, d);

                    // Alt kenarda ince koyu bir çizgi: kumaşa kalınlık verir.
                    float rim = y >= depth ? 1f
                        : Mathf.Clamp01((scallopR - d) / 5f);

                    var c = baseColor * shade * Mathf.Lerp(0.62f, 1f, rim);
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
        }
    }
}
