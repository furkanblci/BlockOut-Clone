using UnityEngine;

namespace GameKit.UI
{
    /// <summary>
    /// Arayüz sprite'larını KOD İLE üretir — hiçbir görsel asset gerekmez.
    ///
    /// DERS (neden prosedürel sprite?): Yuvarlak köşeli panel, mobil casual
    /// arayüzün temel taşıdır ve normalde bir tasarımcının PNG'sini bekler.
    /// Küçük bir doku üretip 9-DİLİM (9-slice) kenar payı vermek, aynı sprite'ı
    /// her boyutta bozulmadan esnetmeyi sağlar: köşeler sabit kalır, kenarlar
    /// uzar. Böylece tek 64×64 doku, ekranın her yerinde kullanılabilir.
    ///
    /// DERS (yumuşak kenar = anti-aliasing): Yarıçapı keskin bir eşikle kesersek
    /// köşeler tırtıklı olur. Kenara olan mesafeyi 1-2 piksellik bir bantta
    /// yumuşatmak (smoothstep) kenarları temizler — GPU'da MSAA'ya gerek kalmaz.
    /// </summary>
    public static class UiSprites
    {
        static Sprite _roundedPanel;
        static Sprite _roundedOutline;
        static Sprite _circle;
        static Sprite _cross;
        static Sprite _burst;
        static Sprite _speaker;
        static Sprite _musicNote;
        static Sprite _haptics;
        static Sprite _bell;

        const int Size = 64;
        const float Radius = 18f;      // piksel; 9-dilim payı bunun biraz üstü
        const int Border = 20;

        /// <summary>Dolu, yuvarlak köşeli panel (9-dilim).</summary>
        public static Sprite RoundedPanel =>
            _roundedPanel != null ? _roundedPanel
                : (_roundedPanel = Build("UiRounded", filled: true, outlineWidth: 0f));

        /// <summary>Yalnız çerçeve — seçili/vurgulu durum için.</summary>
        public static Sprite RoundedOutline =>
            _roundedOutline != null ? _roundedOutline
                : (_roundedOutline = Build("UiRoundedOutline", filled: false, outlineWidth: 5f));

        /// <summary>Tam daire — rozet, ikon zemini, ilerleme noktası.</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle != null) return _circle;

                var tex = NewTexture("UiCircle");
                var pixels = new Color32[Size * Size];
                float half = Size * 0.5f;
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                                   new Vector2(half, half));
                        float a = 1f - Step(half - 1.5f, half, d);
                        pixels[y * Size + x] = new Color(1f, 1f, 1f, a);
                    }
                tex.SetPixels32(pixels);
                tex.Apply(false, true);

                _circle = Sprite.Create(tex, new Rect(0, 0, Size, Size),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                return _circle;
            }
        }

        /// <summary>
        /// Kalın çarpı — kapatma düğmesinin içi.
        ///
        /// DERS (simge YAZI DEĞİLDİR): İlk akla gelen çözüm etikete "✕" yazmak.
        /// Baloo 2'de o kod noktası yok; TMP boş kutu çiziyor. Bu projede dört
        /// kez aynı tuzağa düşüldü. Simge ya sprite ya çizim olmalı — bu, çizim
        /// olanı: iki köşegen parçaya olan mesafeden üretiliyor, her boyutta
        /// keskin, atlas gerektirmiyor.
        /// </summary>
        public static Sprite Cross
        {
            get
            {
                if (_cross != null) return _cross;

                var tex = NewTexture("UiCross");
                var pixels = new Color32[Size * Size];

                float half = Size * 0.5f;
                float arm = Size * 0.26f;     // merkezden kol ucuna
                float thick = Size * 0.10f;   // yarı kalınlık

                var a1 = new Vector2(-arm, -arm); var b1 = new Vector2(arm, arm);
                var a2 = new Vector2(-arm, arm);  var b2 = new Vector2(arm, -arm);

                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        var p = new Vector2(x + 0.5f - half, y + 0.5f - half);
                        float d = Mathf.Min(SegmentDistance(p, a1, b1),
                                            SegmentDistance(p, a2, b2));
                        float alpha = 1f - Step(thick - 1.5f, thick, d);
                        pixels[y * Size + x] = new Color(1f, 1f, 1f, alpha);
                    }

                tex.SetPixels32(pixels);
                tex.Apply(false, true);

                _cross = Sprite.Create(tex, new Rect(0, 0, Size, Size),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                return _cross;
            }
        }

        // ---------------------------------------------------------------- ayar simgeleri

        /// <summary>
        /// Duraklat panelindeki üç ayar simgesi: hoparlör, nota, titreşim.
        ///
        /// DERS (simge YAZI DEĞİLDİR — beşinci tekrar): Bu üçü için ilk akla
        /// gelen 🔊 🎵 📳 yazmak. Baloo 2'de bu kod noktaları yok; TMP boş kutu
        /// çiziyor. Referans oyunda da bunlar çizim, yazı değil.
        ///
        /// DERS (neden İŞARETLİ MESAFE ile?): Simgeleri elle piksel piksel
        /// boyamak her boyutta tırtıklı çıkar. Şekle olan MESAFEYİ hesaplayıp
        /// kenarda bir piksellik bant yumuşatmak, tek bir 128×128 dokudan her
        /// ölçekte temiz kenar veriyor — ve şekli değiştirmek birkaç sayı
        /// değiştirmekten ibaret oluyor.
        /// </summary>
        public static Sprite Speaker => _speaker != null ? _speaker
            : (_speaker = BuildIcon("UiSpeaker", SpeakerDistance));

        public static Sprite MusicNote => _musicNote != null ? _musicNote
            : (_musicNote = BuildIcon("UiMusicNote", MusicNoteDistance));

        public static Sprite Haptics => _haptics != null ? _haptics
            : (_haptics = BuildIcon("UiHaptics", HapticsDistance));

        /// <summary>Bildirim zili — Ayarlar'daki dördüncü simge.</summary>
        public static Sprite Bell => _bell != null ? _bell
            : (_bell = BuildIcon("UiBell", BellDistance));

        /// <summary>Verilen mesafe fonksiyonundan alfa maskesi üretir.</summary>
        static Sprite BuildIcon(string name, System.Func<Vector2, float> distance)
        {
            const int size = 128;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color32[size * size];
            const float edge = 1.4f / size;   // kenar yumuşatma bandı

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Birim kare: (-0.5, -0.5) ... (0.5, 0.5)
                    var p = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
                    float alpha = 1f - Step(-edge, edge, distance(p));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        // Hoparlör: kutu gövde + koni + iki ses dalgası.
        static float SpeakerDistance(Vector2 p)
        {
            float body = RoundedBoxDistance(p, new Vector2(-0.28f, 0f),
                new Vector2(0.085f, 0.105f), 0.03f);

            // Koni dört yarı düzlemin kesişimi; kesişim = mesafelerin EN BÜYÜĞÜ.
            float cone = Mathf.Max(Mathf.Max(
                    -(p.x + 0.21f),                       // sol sınır
                    p.x - 0.02f),                         // sağ sınır
                Mathf.Max(
                    Dot(p, new Vector2(-0.20f, -0.11f), new Vector2(-0.674f, -0.739f)),
                    Dot(p, new Vector2(-0.20f, 0.11f), new Vector2(-0.674f, 0.739f))));

            float wave1 = ArcDistance(p, new Vector2(0.05f, 0f), 0.155f, 0.028f, 0.85f);
            float wave2 = ArcDistance(p, new Vector2(0.05f, 0f), 0.275f, 0.028f, 0.85f);

            return Mathf.Min(Mathf.Min(body, cone), Mathf.Min(wave1, wave2));
        }

        // Nota: iki baş, iki sap, bir bağ çizgisi.
        static float MusicNoteDistance(Vector2 p)
        {
            float head1 = CircleDistance(p, new Vector2(-0.22f, -0.24f), 0.105f);
            float head2 = CircleDistance(p, new Vector2(0.12f, -0.32f), 0.105f);

            float stem1 = CapsuleDistance(p, new Vector2(-0.12f, -0.22f),
                new Vector2(-0.12f, 0.28f), 0.030f);
            float stem2 = CapsuleDistance(p, new Vector2(0.22f, -0.30f),
                new Vector2(0.22f, 0.20f), 0.030f);

            float beam = CapsuleDistance(p, new Vector2(-0.12f, 0.28f),
                new Vector2(0.22f, 0.20f), 0.048f);

            return Mathf.Min(Mathf.Min(head1, head2),
                   Mathf.Min(Mathf.Min(stem1, stem2), beam));
        }

        // Titreşim: telefon gövdesi + iki yanında birer titreşim yayı.
        static float HapticsDistance(Vector2 p)
        {
            float phone = RoundedBoxDistance(p, Vector2.zero,
                new Vector2(0.105f, 0.200f), 0.045f);

            // Sağ yaylar olduğu gibi, sol yaylar noktayı dikey eksende
            // aynalayarak — tek fonksiyonla iki taraf.
            var mirrored = new Vector2(-p.x, p.y);

            float right1 = ArcDistance(p, Vector2.zero, 0.215f, 0.026f, 0.55f);
            float right2 = ArcDistance(p, Vector2.zero, 0.315f, 0.026f, 0.55f);
            float left1 = ArcDistance(mirrored, Vector2.zero, 0.215f, 0.026f, 0.55f);
            float left2 = ArcDistance(mirrored, Vector2.zero, 0.315f, 0.026f, 0.55f);

            return Mathf.Min(Mathf.Min(phone, Mathf.Min(right1, right2)),
                             Mathf.Min(left1, left2));
        }

        // ---------------------------------------------------------------- mesafe araçları

        /// <summary>Yarı düzlem: normal yönünde pozitif, ters yönde negatif.</summary>
        /// <summary>
        /// Zil: üstte kubbe, altta genişleyen etek, altında düz kenar ve tokmak.
        ///
        /// Kubbe bir dairenin ALT YARISI kesilerek elde ediliyor (kesişim =
        /// mesafelerin en büyüğü); etek iki eğik yarı düzlemle daraltılıyor.
        /// </summary>
        static float BellDistance(Vector2 p)
        {
            // Kubbe: yarıçapı 0.20 daire, y > -0.02 kısmı.
            float dome = Mathf.Max(CircleDistance(p, new Vector2(0f, 0.10f), 0.20f),
                                   -(p.y + 0.02f));

            // Etek: aşağı doğru açılan gövde.
            float skirt = Mathf.Max(Mathf.Max(
                    Dot(p, new Vector2(-0.20f, -0.02f), new Vector2(-0.966f, 0.259f)),
                    Dot(p, new Vector2(0.20f, -0.02f), new Vector2(0.966f, 0.259f))),
                Mathf.Max(p.y - 0.10f, -(p.y + 0.20f)));

            // Alt kenar çubuğu ve tokmak.
            float rim = RoundedBoxDistance(p, new Vector2(0f, -0.235f),
                new Vector2(0.30f, 0.035f), 0.03f);
            float clapper = CircleDistance(p, new Vector2(0f, -0.33f), 0.062f);

            return Mathf.Min(Mathf.Min(dome, skirt), Mathf.Min(rim, clapper));
        }

        static float Dot(Vector2 p, Vector2 pointOnPlane, Vector2 normal)
            => Vector2.Dot(p - pointOnPlane, normal);

        static float CircleDistance(Vector2 p, Vector2 center, float radius)
            => Vector2.Distance(p, center) - radius;

        static float CapsuleDistance(Vector2 p, Vector2 a, Vector2 b, float radius)
            => SegmentDistance(p, a, b) - radius;

        static float BoxDistance(Vector2 p, Vector2 center, Vector2 half)
        {
            float qx = Mathf.Abs(p.x - center.x) - half.x;
            float qy = Mathf.Abs(p.y - center.y) - half.y;
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f);
        }

        static float RoundedBoxDistance(Vector2 p, Vector2 center, Vector2 half, float radius)
            => BoxDistance(p, center, half - Vector2.one * radius) - radius;

        /// <summary>
        /// Yay parçası: merkez etrafında <paramref name="radius"/> yarıçaplı,
        /// yatay eksenden ±<paramref name="spread"/> radyan açılan şerit.
        /// </summary>
        static float ArcDistance(Vector2 p, Vector2 center, float radius,
            float thickness, float spread)
        {
            Vector2 d = p - center;
            float angle = Mathf.Atan2(d.y, d.x);

            if (Mathf.Abs(angle) > spread)
            {
                // Açı aralığının dışındaysak yayın UÇLARINA olan mesafe geçerli;
                // yoksa yay tam daireye dönüşür.
                var end1 = center + new Vector2(Mathf.Cos(spread), Mathf.Sin(spread)) * radius;
                var end2 = center + new Vector2(Mathf.Cos(spread), -Mathf.Sin(spread)) * radius;
                return Mathf.Min(Vector2.Distance(p, end1), Vector2.Distance(p, end2)) - thickness;
            }

            return Mathf.Abs(d.magnitude - radius) - thickness;
        }

        /// <summary>Bir noktanın [a,b] doğru parçasına en kısa uzaklığı.</summary>
        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>
        /// Eşikli yumuşak geçiş — GLSL'deki <c>smoothstep</c>.
        ///
        /// DERS (Unity'nin Mathf.SmoothStep BU DEĞİL): Unity'deki aynı adlı
        /// metot iki DEĞER arasında yumuşatılmış bir lerp yapar ve t'yi 0-1'e
        /// kırpar — bir maske değil, bir SAYI döndürür. Burada
        /// <c>1 - Mathf.SmoothStep(-0.75f, 0.75f, d)</c> yazılmıştı; dışarıda
        /// 0 vermesi gerekirken 0.25 veriyordu, yani her yuvarlak panelin
        /// arkasında %25 saydamlıkta bir KARE hayalet duruyordu. Yuvarlak
        /// köşeler bu yüzden aylarca "biraz kirli" göründü ve sebebi
        /// aranmadı. Kenar yumuşatmasında istenen, x'in iki eşik arasında
        /// 0'dan 1'e geçmesi.
        /// </summary>
        static float Step(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / Mathf.Max(1e-5f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        static Sprite Build(string name, bool filled, float outlineWidth)
        {
            var tex = NewTexture(name);
            var pixels = new Color32[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float d = RoundedRectDistance(x + 0.5f, y + 0.5f);

                    // d < 0 içeride, d > 0 dışarıda. Kenarda 1.5 piksellik yumuşama.
                    float alpha = 1f - Step(-0.75f, 0.75f, d);

                    if (!filled)
                    {
                        // Çerçeve: dış kenardan `outlineWidth` kadar içerisi boş.
                        float inner = 1f - Step(-0.75f, 0.75f, d + outlineWidth);
                        alpha = Mathf.Clamp01(alpha - inner);
                    }

                    pixels[y * Size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            // 9-dilim: köşe payı yarıçaptan biraz büyük olmalı, yoksa esnerken
            // yuvarlaklık bozulur.
            return Sprite.Create(tex, new Rect(0, 0, Size, Size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(Border, Border, Border, Border));
        }

        /// <summary>
        /// Yuvarlak dikdörtgene işaretli mesafe. Negatif = içeride.
        /// Köşelerde yarıçaplı bir daireye, kenarlarda düz çizgiye indirgenir.
        /// </summary>
        static float RoundedRectDistance(float x, float y)
        {
            float halfW = Size * 0.5f, halfH = Size * 0.5f;
            float dx = Mathf.Abs(x - halfW) - (halfW - Radius);
            float dy = Mathf.Abs(y - halfH) - (halfH - Radius);

            float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            return outside + inside - Radius;
        }

        /// <summary>
        /// Işık huzmesi — ödülün arkasına konan "buradan parlıyor" zemini.
        ///
        /// DERS (parlaklık NESNENİN kendisinden gelmez): Jeton yığınını olduğu
        /// gibi koyunca "mor zeminde duran sarı şekiller" görünüyor. Arkasına
        /// merkezden dışa sönen bir huzme koymak yığını zeminden ayırıyor ve
        /// göz onu "değerli" okuyor. Casual oyunların ödül ekranlarında bu
        /// huzme neredeyse istisnasız var.
        ///
        /// Işınlar açıya bağlı bir kuvvet fonksiyonuyla üretiliyor: kosinüsün
        /// yüksek kuvveti dar ve keskin ışınlar verir, düşük kuvveti geniş ve
        /// yumuşak. Taban parlaklık hep var ki ışınların arası kararmasın.
        /// </summary>
        public static Sprite Burst
        {
            get
            {
                if (_burst != null) return _burst;

                const int size = 128;
                const int rayCount = 12;

                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "UiBurst",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                var pixels = new Color32[size * size];
                float half = size * 0.5f;

                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - half;
                        float dy = y + 0.5f - half;
                        float r = Mathf.Sqrt(dx * dx + dy * dy) / half;

                        // Merkezden dışa sönüm: kare alınca merkez daha yoğun,
                        // kenar daha temiz biter.
                        float fade = 1f - Mathf.Clamp01(r);
                        fade *= fade;

                        float angle = Mathf.Atan2(dy, dx);
                        float ray = Mathf.Abs(Mathf.Cos(angle * rayCount * 0.5f));
                        ray = Mathf.Pow(ray, 6f);

                        float alpha = fade * (0.35f + 0.65f * ray);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }

                tex.SetPixels32(pixels);
                tex.Apply(false, true);

                _burst = Sprite.Create(tex, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                return _burst;
            }
        }

        static Texture2D NewTexture(string name) => new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        /// <summary>Görsel ayarlar değişince yeniden üretilsin.</summary>
        public static void ClearCache()
        {
            _roundedPanel = null;
            _roundedOutline = null;
            _circle = null;
            _cross = null;
            _burst = null;
            _speaker = null;
            _bell = null;
            _musicNote = null;
            _haptics = null;
        }
    }
}
