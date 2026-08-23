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
        static Sprite _plus;
        static Sprite _burst;
        static Sprite _speaker;
        static Sprite _musicNote;
        static Sprite _haptics;
        static Sprite _bell;
        static Sprite _pencil;
        static Sprite _pencilTip;
        static Sprite _triangle;
        static Sprite _clockFace;

        const int Size = 64;
        const float Radius = 18f;      // piksel; 9-dilim payı bunun biraz üstü
        const int Border = 20;

        /// <summary>
        /// YUVARLAK PANELİN KENDİ ÇÖZÜNÜRLÜĞÜ — 2× (7. tur, P56).
        ///
        /// Kullanıcı: "Seçili butonun dış köşelerinde piksel bozulmaları var."
        ///
        /// ÖLÇÜM: Alt çubuktaki seçili kart 263×277 birim ve köşe yarıçapı 44
        /// birim (<c>pixelsPerUnitMultiplier = 0.45</c>). Dokudaki yay ise
        /// 18 piksel — yani yay ekrana **2,4 KAT BÜYÜTÜLEREK** çiziliyor.
        /// Yayın 1,5 piksellik yumuşatma bandı da onunla birlikte 3,6 birime
        /// çıkıyor; köşe hem bulanıklaşıyor hem dokunun kendi basamakları
        /// görünür oluyor. Ekranın en büyük yuvarlak yüzeyi olduğu için de
        /// bozulma en çok orada okunuyor.
        ///
        /// DERS (9-dilim, çözünürlükten muaf değildir): Dokuz dilim KENARLARI
        /// bozulmadan uzatır — köşeleri değil. Köşe her zaman dokudaki yayın
        /// kendisidir ve ekrandaki yarıçap dokudakinden büyükse büyütülür.
        /// Bu yüzden "9-dilim kullanıyoruz, boyut önemli değil" yanlıştır.
        ///
        /// ÇÖZÜM ÇAĞRI YERLERİNE DOKUNMADAN: Doku 128'e, yay 36 piksele,
        /// dilim payı 40 piksele çıktı — VE sprite'ın piksel/birim oranı da
        /// 100'den 200'e. Unity dilim payını
        /// <c>pay_px × referans_ppu / (sprite_ppu × çarpan)</c> ile birime
        /// çevirdiğinden 40×100/(200×m) = 20/m, yani eski değerin AYNISI.
        /// Projedeki kırk kadar elle ölçülmüş <c>SetSliceScale</c> değeri ve
        /// <see cref="UiCornerFit"/>'in hesabı olduğu gibi geçerli kalıyor;
        /// değişen tek şey köşenin keskinliği (büyütme 2,4× → 1,2×).
        /// </summary>
        /// <summary>
        /// İKİNCİ YÜKSELTME — 2× DAHA (8. tur, düğme turu).
        ///
        /// Kullanıcı: "butonların piksel sorunu olduğunu ve tarz olarak
        /// alakasız kaldığını söylediler."
        ///
        /// ÖLÇÜM (referans duraklat paneli, Resume düğmesi 280×113): köşe
        /// yarıçapı **40 piksel**, yani kısa kenarın %35'i. Bizim ev oranımız
        /// %22 ve tavanımız 34 birimdi — düğmeler hem fazla köşeliydi hem de
        /// 1080 çözünürlükte istenen 73 birimlik yarıçap 36 pikselik yaydan
        /// **4 KAT** büyütülerek çiziliyordu. "Piksel bozukluğu" tam olarak bu.
        ///
        /// Bir öncekiyle aynı numara: dört sayı da 2× oluyor, böylece
        /// <c>PanelRadius/ppu = 0,18</c> ve <c>PanelBorder/ppu = 0,20</c>
        /// oranları korunuyor. Projedeki kırk kadar elle ölçülmüş
        /// <c>SetSliceScale</c> değeri ve <see cref="UiCornerFit"/>'in hesabı
        /// hiç değişmeden geçerli kalıyor; değişen tek şey yayın çözünürlüğü.
        /// 256×256 RGBA = 256 KB, tek sprite için kabul edilebilir.
        ///
        /// DERS (bir sabiti değiştirmek yerine ORANI koru): Bu dokuyu tek
        /// başına büyütmek kırk çağrı yerini sessizce bozardı. Hangi oranın
        /// sözleşme olduğunu bilmek, aynı dosyayı ikinci kez korkusuzca
        /// büyütmeyi mümkün kıldı.
        const int PanelSize = 256;
        const float PanelRadius = 72f;
        const int PanelBorder = 80;
        const float PanelPixelsPerUnit = 400f;

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
        /// Kalın artı — "daha fazla al" düğmesinin içi.
        ///
        /// <see cref="Cross"/> ile aynı gerekçe (simge yazı değildir) ama ayrı
        /// bir çizim: artı, çarpının 45° döndürülmüş hâli DEĞİL. Döndürünce
        /// kolların uçları eğik kalıyor ve piksel ızgarasına oturmuyor;
        /// eksenlere paralel çizmek her boyutta keskin duruyor.
        /// </summary>
        public static Sprite Plus
        {
            get
            {
                if (_plus != null) return _plus;

                var tex = NewTexture("UiPlus");
                var pixels = new Color32[Size * Size];

                float half = Size * 0.5f;
                float arm = Size * 0.30f;     // merkezden kol ucuna
                float thick = Size * 0.115f;  // yarı kalınlık

                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        var p = new Vector2(x + 0.5f - half, y + 0.5f - half);

                        // İki dikdörtgenin BİRLEŞİMİ = mesafelerin en KÜÇÜĞÜ.
                        float dx = Mathf.Max(Mathf.Abs(p.x) - arm, Mathf.Abs(p.y) - thick);
                        float dy = Mathf.Max(Mathf.Abs(p.x) - thick, Mathf.Abs(p.y) - arm);
                        float d = Mathf.Min(dx, dy);

                        float alpha = 1f - Step(-1.5f, 1.5f, d);
                        pixels[y * Size + x] = new Color(1f, 1f, 1f, alpha);
                    }

                tex.SetPixels32(pixels);
                tex.Apply(false, true);

                _plus = Sprite.Create(tex, new Rect(0, 0, Size, Size),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                return _plus;
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

        /// <summary>
        /// Kalem — profildeki "adı düzenle" rozeti (4. tur, C10).
        ///
        /// Kullanıcı: "Player çerçevesinin sağ alt köşesindeki düzenleme (edit)
        /// ikonu kötü — yeni görsel üretilecek."
        ///
        /// DERS (eğik bir kapsül KALEM DEĞİLDİR): Rozetin içinde 45 derece
        /// döndürülmüş beyaz bir kapsül vardı. Kapsülün iki ucu da yuvarlak
        /// olduğu için ekranda "beyaz bir oval" okunuyordu; kalemi kalem yapan
        /// şey UÇTAKİ SİVRİLİK ve öbür uçtaki düz kesim. Şekil o iki bilgiyi
        /// taşımadığı sürece boyutu ya da açısı ne olursa olsun kalem
        /// görünmüyor. Burada gövde düz kesimli bir kutu, uç ise gerçek bir
        /// üçgen.
        /// </summary>
        public static Sprite Pencil => _pencil != null ? _pencil
            : (_pencil = BuildIcon("UiPencil", PencilDistance));

        /// <summary>
        /// Kalemin YALNIZ ucu — gövdenin üstüne KOYU renkte konur (7. tur, Q57).
        ///
        /// Kullanıcı: "Avatar isim değiştirme ikonu hâlâ düzgün değil; gölgeli
        /// ve daha kaliteli hale getirilecek."
        ///
        /// Tek renk beyaz bir kalem, 80 birimlik yeşil bir dairede KALEM değil
        /// "eğik beyaz bir işaret" okunuyordu — kullanıcının gördüğü şey buydu
        /// (ekran görüntüsünde tik sanılıyor). Bir kalemi kalem yapan iki
        /// renk vardır: gövde ve GRAFİT uç. Aynı mesafe fonksiyonunun yalnız
        /// uç parçası ayrı bir maske olarak üretiliyor; iki katman üst üste
        /// konunca şekil tek bakışta okunuyor.
        ///
        /// DERS (bir simgeyi büyütmek onu anlaşılır yapmaz): Önceki turda
        /// çözüm "sivri uç ekle" olmuştu ve şekil doğruydu — eksik olan
        /// KONTRASTTI. Tek renkli bir siluet, ayrıntısı ne kadar doğru olursa
        /// olsun küçük boyutta kendi lekesine dönüşüyor.
        /// </summary>
        public static Sprite PencilTip => _pencilTip != null ? _pencilTip
            : (_pencilTip = BuildIcon("UiPencilTip", PencilTipDistance));

        /// <summary>
        /// Yuvarlak köşeli, YUKARI bakan üçgen — ok başı ve kapı işareti.
        ///
        /// DERS (üçgen sprite ı olmayan projede üçgen çizmek): Bu projede
        /// üçgen gereken her yerde 45 derece döndürülmüş bir kare kullanıldı.
        /// Döndürülmüş kare bir üçgen DEĞİL — üç kenarı da eşit görünmüyor ve
        /// ok başı olarak konduğunda gövdeyle hizası tutmuyor. Gerçek bir
        /// üçgen maskesi bir kez üretilince her yerde doğru oturuyor.
        /// </summary>
        public static Sprite Triangle => _triangle != null ? _triangle
            : (_triangle = BuildIcon("UiTriangle", TriangleDistance));

        /// <summary>
        /// SAAT KADRANI — ince halka + iki akrep (7. tur, U67).
        ///
        /// Kullanıcı: "Süre ikonu mor olacak… tasarım orijinaliyle birebir
        /// aynı olacak."
        ///
        /// ÖLÇÜM (`…Levels 1-20 Walkthrough.mp4`, 01:20 karesi, 592×1280;
        /// süre hapı x 215-380, y 120-171 piksel): haptaki simge x 226-256,
        /// y 132-159 — yani 30×27 piksellik, İÇİ BOŞ, tek renk MOR bir kadran.
        /// Bizde onun yerine `Art.Clock` vardı: mavi-turkuaz, 3B render
        /// edilmiş bir ÇALAR SAAT. Aynı görsel mağazada yardımcı simgesi
        /// olarak da kullanılıyor; oysa referansta HUD'daki saat bir SÜS
        /// değil, sayının etiketi.
        ///
        /// DERS (aynı kavram, iki farklı rol, iki farklı görsel): "Saat"
        /// gereken her yere aynı çalar saati koymak, süre göstergesini bir
        /// yardımcı düğmesi gibi okutuyordu. Bir simgenin doğru olması
        /// yetmez; bulunduğu yerin diline ait olması gerekir.
        /// </summary>
        public static Sprite ClockFace => _clockFace != null ? _clockFace
            : (_clockFace = BuildIcon("UiClockFace", ClockFaceDistance));

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

        /// <summary>
        /// Yukarı bakan ikizkenar üçgen: üç yarı düzlemin kesişimi.
        /// Kesişim = mesafelerin EN BÜYÜĞÜ.
        /// </summary>
        static float TriangleDistance(Vector2 p)
        {
            // Tepe (0, 0.42), taban köşeleri (±0.40, -0.36).
            // Kenar normalleri: sağ (0.700, 0.714), sol (-0.700, 0.714).
            return Mathf.Max(
                -(p.y + 0.36f),
                Mathf.Max(
                    Dot(p, new Vector2(0f, 0.42f), new Vector2(0.700f, 0.714f)),
                    Dot(p, new Vector2(0f, 0.42f), new Vector2(-0.700f, 0.714f))));
        }

        /// <summary>
        /// Kalem: köşegen boyunca düz kesimli gövde + sivri uç + silgi bandı.
        ///
        /// Hesap kalemin KENDİ ekseninde yapılıyor (u = eksen boyunca,
        /// v = enine). Böylece eğik bir şekli eğik formüllerle değil, düz
        /// formüllerle tarif edebiliyoruz; açıyı değiştirmek tek bir yön
        /// vektörünü değiştirmek oluyor.
        /// </summary>
        static float PencilDistance(Vector2 p)
        {
            // 45 derece: uç sağ üstte, silgi sol altta.
            var axis = new Vector2(0.7071f, 0.7071f);
            var side = new Vector2(-0.7071f, 0.7071f);
            float u = p.x * axis.x + p.y * axis.y;
            float v = p.x * side.x + p.y * side.y;
            var local = new Vector2(u, v);

            // Gövde: uç tarafındaki kenarı KESKİN kalsın diye yarıçap küçük.
            //
            // SILGI BANDI KALDIRILDI: ayrı bir kutu olarak eklenince gövdeyle
            // arasında saç teli kadar bir boşluk kalıyor ve o boşluk ekranda
            // "kopmuş bir parça" gibi görünüyordu. 40 piksellik bir rozette
            // üç parçalık bir şekil zaten okunmuyor; kalemi kalem yapan iki
            // bilgi (düz kesilmiş arka uç + sivri ön uç) tek gövdede duruyor.
            //
            // DERS (küçük bir simgede AYRINTI GÜRÜLTÜDÜR): Silgi gerçek bir
            // kalemde vardır ama 40 pikselde onu çizmek, şekli anlaşılır
            // kılmak yerine kirletiyor.
            float body = RoundedBoxDistance(local, new Vector2(-0.09f, 0f),
                new Vector2(0.26f, 0.088f), 0.025f);

            // Uç: üç yarı düzlemin kesişimi (kesişim = mesafelerin EN BÜYÜĞÜ).
            // Taban (0.15, ±0.085), tepe (0.36, 0).
            float tip = Mathf.Max(
                -(u - 0.15f),
                Mathf.Max(
                    Dot(local, new Vector2(0.15f, 0.085f), new Vector2(0.391f, 0.920f)),
                    Dot(local, new Vector2(0.15f, -0.085f), new Vector2(0.391f, -0.920f))));

            return Mathf.Min(body, tip);
        }

        /// <summary>
        /// Saat kadranı: halka + akrep (yukarı) + yelkovan (sağ-aşağı).
        ///
        /// Ölçüler referans kareden (30×27 piksellik simge) oranlandı:
        /// halka çapı simgenin %86'sı, kalınlığı çapın %13'ü.
        /// Akreplerin ucu halkaya DEĞMİYOR — değseydi bu boyutta kadran
        /// dolu bir leke gibi okunurdu.
        /// </summary>
        static float ClockFaceDistance(Vector2 p)
        {
            const float radius = 0.365f;      // halkanın orta yarıçapı
            const float ring = 0.048f;        // halka kalınlığının yarısı
            const float hand = 0.040f;        // akrep kalınlığının yarısı

            float dial = Mathf.Abs(p.magnitude - radius) - ring;

            // 12 yönünde kısa akrep, 4 yönünde uzun yelkovan (referanstaki
            // duruş). Merkezde birleştikleri için ayrı bir göbek gerekmiyor.
            float hour = CapsuleDistance(p, Vector2.zero, new Vector2(0f, 0.185f), hand);
            float minute = CapsuleDistance(p, Vector2.zero, new Vector2(0.145f, -0.115f), hand);

            return Mathf.Min(dial, Mathf.Min(hour, minute));
        }

        /// <summary>
        /// <see cref="PencilDistance"/>'in YALNIZ uç üçgeni — aynı koordinat
        /// sisteminde, aynı sayılarla. İki maske aynı yerleşimde üst üste
        /// konduğunda uç, gövdenin tam ucuna oturuyor.
        /// </summary>
        static float PencilTipDistance(Vector2 p)
        {
            var axis = new Vector2(0.7071f, 0.7071f);
            var side = new Vector2(-0.7071f, 0.7071f);
            float u = p.x * axis.x + p.y * axis.y;
            float v = p.x * side.x + p.y * side.y;
            var local = new Vector2(u, v);

            return Mathf.Max(
                -(u - 0.15f),
                Mathf.Max(
                    Dot(local, new Vector2(0.15f, 0.085f), new Vector2(0.391f, 0.920f)),
                    Dot(local, new Vector2(0.15f, -0.085f), new Vector2(0.391f, -0.920f))));
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
            var tex = new Texture2D(PanelSize, PanelSize, TextureFormat.RGBA32, false)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[PanelSize * PanelSize];

            // Yumuşatma bandı doku ÇÖZÜNÜRLÜĞÜYLE ölçeklenmeli: doku iki
            // kat büyüdüğünde 1,5 pikselik bant ekranda yarıya iner ve köşe
            // bu sefer tırtıklı çıkar.
            const float feather = 3.0f;

            for (int y = 0; y < PanelSize; y++)
            {
                for (int x = 0; x < PanelSize; x++)
                {
                    float d = RoundedRectDistance(x + 0.5f, y + 0.5f);

                    // d < 0 içeride, d > 0 dışarıda.
                    float alpha = 1f - Step(-feather, feather, d);

                    if (!filled)
                    {
                        // Çerçeve: dış kenardan `outlineWidth` kadar içerisi boş.
                        float inner = 1f - Step(-feather, feather, d + outlineWidth * 2f);
                        alpha = Mathf.Clamp01(alpha - inner);
                    }

                    pixels[y * PanelSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            // 9-dilim: köşe payı yarıçaptan biraz büyük olmalı, yoksa esnerken
            // yuvarlaklık bozulur. Piksel/birim 200 — bkz. PanelPixelsPerUnit.
            return Sprite.Create(tex, new Rect(0, 0, PanelSize, PanelSize),
                new Vector2(0.5f, 0.5f), PanelPixelsPerUnit, 0, SpriteMeshType.FullRect,
                new Vector4(PanelBorder, PanelBorder, PanelBorder, PanelBorder));
        }

        /// <summary>
        /// Yuvarlak dikdörtgene işaretli mesafe. Negatif = içeride.
        /// Köşelerde yarıçaplı bir daireye, kenarlarda düz çizgiye indirgenir.
        /// </summary>
        static float RoundedRectDistance(float x, float y)
        {
            float halfW = PanelSize * 0.5f, halfH = PanelSize * 0.5f;
            float dx = Mathf.Abs(x - halfW) - (halfW - PanelRadius);
            float dy = Mathf.Abs(y - halfH) - (halfH - PanelRadius);

            float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            return outside + inside - PanelRadius;
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
        static Sprite _radial;

        /// <summary>
        /// IŞINSIZ yumuşak hale: merkezden dışa doğru sönen düz bir daire.
        ///
        /// <see cref="Burst"/> ile farkı, ışın olmaması. Referansta (Block
        /// Out! PERFECT kartı) jeton yığınının arkasındaki parlaklık çizgisel
        /// değil; ışın koymak ekranda gri bir çark üretiyordu.
        ///
        /// Sönüm eğrisi KÜP: karesel sönüm kenarda hâlâ görünür bir daire
        /// bırakıyor, küp alınca kenar tamamen kayboluyor ve hale "bir yerde
        /// biten" bir şekil olmaktan çıkıyor.
        /// </summary>
        /// <summary>
        /// Yardımcı (booster) düğmesinin YÜZEYİ — tek prosedürel görsel.
        ///
        /// NEDEN TEK PARÇA (13. tur, F2 — kullanıcı: *"powerupların arka
        /// butonunu güncelle, kötü duruyor"*):
        ///
        /// Bu düğme dört ayrı `RoundedPanel` katmanıyla kuruluyordu (koyu
        /// kenar, gövde, kuyu, ikinci kuyu tonu) ve referansın en belirgin
        /// özelliğini veremiyordu: yeşilin yukarıdan aşağı SÜREKLİ sönmesi.
        /// Ölçüm (referans düğmenin dikey kesiti, yeşil kanal):
        ///
        ///     216 -> 208 -> 195 -> 174 -> 115     (%47 düşüş, pürüzsüz)
        ///
        /// Düz katmanlarla bunu vermek için 6-8 bant gerekiyor ve her bant
        /// sınırı 138 birimlik bir düğmede BASAMAK olarak görünüyor. İki
        /// tonla denendi, kullanıcı haklı olarak beğenmedi.
        ///
        /// Doku olarak üretilince rampa piksel piksel çiziliyor, basamak
        /// kalmıyor; üstelik koyu kenar, kuyu ve kuyunun üst parlaklığı da
        /// aynı dokuya giriyor, yani dört katman bire iniyor.
        ///
        /// DERS (katman sayısı artıyorsa, yanlış aracı kullanıyorsun):
        /// Düz renk panelleri "iki-üç ton" için doğru araç. Sürekli bir
        /// geçiş isteniyorsa panel eklemek çözüm değil — çizim gerekiyor.
        ///
        /// Boyut sabit (düğme 194x138 birim) olduğu için 9-dilime gerek yok;
        /// doku doğrudan gerilmeden kullanılıyor.
        /// </summary>
        static Sprite _powerPad;

        public static Sprite PowerPad
        {
            get
            {
                if (_powerPad != null) return _powerPad;

                const int W = 256, H = 182;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
                {
                    name = "UiPowerPad",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                // Ölçülen duraklar (referans kesiti, üstten alta).
                var durakT = new[] { 0.00f, 0.22f, 0.46f, 0.72f, 1.00f };
                var durakC = new[]
                {
                    new Color(36f / 255f, 216f / 255f,  29f / 255f),
                    new Color(58f / 255f, 208f / 255f,  31f / 255f),
                    new Color(36f / 255f, 195f / 255f,  18f / 255f),
                    new Color(34f / 255f, 174f / 255f,  13f / 255f),
                    new Color(14f / 255f, 115f / 255f,  10f / 255f),
                };

                var kenar = new Color(0f, 54f / 255f, 0f);
                var kuyuKenar = new Color(18f / 255f, 132f / 255f, 8f / 255f);

                float rDis = H * 0.24f;          // dış köşe yarıçapı
                float kalin = H * 0.055f;        // koyu kenar kalınlığı
                float rIc = rDis - kalin;

                // Kuyu: ikonun oturduğu açık alan (y AŞAĞIDAN).
                // Kuyu ilk denemede %11/%26..%92 verildi ve düğmenin
                // neredeyse tamamını kapladı; referansta kuyunun ÇEVRESİNDE
                // görünür bir koyu yeşil bilezik var ve o bilezik düğmeye
                // derinlik veren şey.
                float kx0 = W * 0.135f, kx1 = W * 0.865f;
                float ky0 = H * 0.305f, ky1 = H * 0.875f;
                float rKuyu = (ky1 - ky0) * 0.28f;

                var px = new Color32[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float fx = x + 0.5f, fy = y + 0.5f;
                        float dDis = PadMesafe(fx, fy, 0f, 0f, W, H, rDis);
                        if (dDis > 0.7f) { px[y * W + x] = new Color32(0, 0, 0, 0); continue; }

                        // t: ÜSTTEN aşağı 0..1 (doku y'si aşağıdan yukarı)
                        float t = 1f - (float)y / (H - 1);
                        Color c = PadTon(durakT, durakC, t);

                        float dIc = PadMesafe(fx, fy, kalin, kalin, W - kalin, H - kalin, rIc);
                        if (dIc > -0.5f)
                        {
                            c = Color.Lerp(c, kenar, Mathf.Clamp01(dIc + 1f));
                        }
                        else
                        {
                            float dK = PadMesafe(fx, fy, kx0, ky0, kx1, ky1, rKuyu);
                            if (dK < 0f)
                            {
                                float ust = Mathf.Clamp01((fy - ky0) / (ky1 - ky0));
                                c = Color.Lerp(c, Color.white, 0.10f + 0.10f * ust);
                                c = Color.Lerp(c, kuyuKenar, Mathf.Clamp01(1.6f + dK) * 0.70f);
                            }
                        }

                        c.a = Mathf.Clamp01(0.7f - dDis);
                        px[y * W + x] = c;
                    }

                tex.SetPixels32(px);
                tex.Apply(false, true);
                _powerPad = Sprite.Create(tex, new Rect(0f, 0f, W, H),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                _powerPad.name = "UiPowerPad";
                _powerPad.hideFlags = HideFlags.HideAndDontSave;
                return _powerPad;
            }
        }

        /// <summary>
        /// "Rewards x3" rozetinin yüzeyi — tek prosedürel görsel.
        ///
        /// NEDEN (13. tur — kullanıcı: *"benzedi ama dil olarak sanki bir
        /// eksiklik var, renkler olarak bizimki çok basit duruyor"*):
        ///
        /// Rozet düz turuncu bir panel + düz koyu bir konturdu. Referansın
        /// dikey kesiti ise turuncuyu sürekli bir geçiş olarak veriyor:
        ///
        ///     ust kenar (72,13,0)  ->  (255,180,34)  ->  (243,158,31)
        ///     ->  (238,138,8)  ->  alt dudak (196,96,0)
        ///
        /// Bu, oyunun görsel dilinin tamamında geçerli: referansta HİÇBİR
        /// yüzey tek ton değil. Düz panellerle "yaklaşmak" mümkün ama sonuç
        /// hep sade kalıyor — kullanıcının fark ettiği şey tam olarak bu.
        ///
        /// DERS (bu projede ikinci kez, bkz. <see cref="PowerPad"/>):
        /// Sürekli bir geçiş isteniyorsa panel eklemek çözüm değil, ÇİZİM
        /// gerekiyor. Doku olarak üretmek hem basamağı siliyor hem de kontur,
        /// gradyan ve iç parlaklığı tek katmanda topluyor.
        /// </summary>
        static Sprite _rewardTag;

        public static Sprite RewardTag
        {
            get
            {
                if (_rewardTag != null) return _rewardTag;

                const int W = 320, H = 64;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
                {
                    name = "UiRewardTag",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                // Ölçülen duraklar (referans kesiti, üstten alta).
                var durakT = new[] { 0.00f, 0.30f, 0.62f, 1.00f };
                var durakC = new[]
                {
                    new Color(255f / 255f, 190f / 255f,  56f / 255f),
                    new Color(255f / 255f, 176f / 255f,  34f / 255f),
                    new Color(240f / 255f, 146f / 255f,  18f / 255f),
                    new Color(206f / 255f, 106f / 255f,   2f / 255f),
                };

                var kenar = new Color(72f / 255f, 13f / 255f, 0f);

                // Kontur 0,105 -> 0,082: ilk denemede referanstan kalın
                // çıkıyor ve rozet "çerçeveli" duruyordu.
                float rDis = H * 0.34f;
                float kalin = H * 0.082f;
                float rIc = rDis - kalin;

                var px = new Color32[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float fx = x + 0.5f, fy = y + 0.5f;
                        float dDis = PadMesafe(fx, fy, 0f, 0f, W, H, rDis);
                        if (dDis > 0.7f) { px[y * W + x] = new Color32(0, 0, 0, 0); continue; }

                        float t = 1f - (float)y / (H - 1);
                        Color c = PadTon(durakT, durakC, t);

                        float dIc = PadMesafe(fx, fy, kalin, kalin, W - kalin, H - kalin, rIc);
                        if (dIc > -0.5f)
                        {
                            c = Color.Lerp(c, kenar, Mathf.Clamp01(dIc + 1f));
                        }
                        else if (dIc > -kalin * 0.9f && t < 0.34f)
                        {
                            // Üst iç kenarda ince bir parlaklık: referansta
                            // konturun hemen altındaki açık şerit.
                            float k = 1f - Mathf.Clamp01(-dIc / (kalin * 0.9f));
                            c = Color.Lerp(c, Color.white, 0.30f * k * (1f - t / 0.34f));
                        }

                        c.a = Mathf.Clamp01(0.7f - dDis);
                        px[y * W + x] = c;
                    }

                tex.SetPixels32(px);
                tex.Apply(false, true);
                _rewardTag = Sprite.Create(tex, new Rect(0f, 0f, W, H),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                _rewardTag.name = "UiRewardTag";
                _rewardTag.hideFlags = HideFlags.HideAndDontSave;
                return _rewardTag;
            }
        }

        /// <summary>
        /// Mağazanın jeton kapsülünün yüzeyi — tek prosedürel görsel (M1).
        ///
        /// Kullanıcı: *"bizdeki çok düz ve dış rengi siyah, kontürü çok; onu
        /// düzeltelim birebir orijinal hâle getirelim."*
        ///
        /// İki ayrı kusur vardı ve ikisi de ölçümle doğrulandı
        /// (`market.jpeg` üzerinde kapsülün dikey kesiti):
        ///
        ///   DOLGU  referans (245,248,255) -> (224,226,249)   SOĞUK BEYAZ,
        ///                                                     üstten alta sönen
        ///          bizim    (255,249,236) düz                 SICAK KREM
        ///
        ///   KENAR  referans (4,5,70)      koyu LACİVERT
        ///          bizim    (35,19,9)     neredeyse SİYAH-KAHVE
        ///
        /// Kahverengiye çalan siyah bir kenar, tentenin mavisinin üstünde
        /// yabancı duruyordu; referansın laciverti aynı aileden olduğu için
        /// kapsülü kesmiyor, oturtuyor.
        ///
        /// Üç düz katman (gölge + kenar + yüz) yerine tek doku: gradyan
        /// basamaksız ve kenar kalınlığı piksel piksel ayarlanabiliyor.
        /// (Bu turda aynı çözüm <see cref="PowerPad"/> ve
        /// <see cref="RewardTag"/> için de gerekmişti.)
        /// </summary>
        static Sprite _coinPad;

        public static Sprite CoinPad
        {
            get
            {
                if (_coinPad != null) return _coinPad;

                const int W = 256, H = 72;
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false)
                {
                    name = "UiCoinPad",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };

                // İLK ÖLÇÜM YANLIŞTI, DÜZELTİLDİ.
                //
                // Kapsülün dikey kesitini x=%36'da almıştım; oysa kapsül
                // %11,9..%28,2 arasında — yani o sütun kapsülün DIŞINDAYDI
                // ve okuduğum "koyu lacivert kenar" ile "soğuk beyaz dolgu"
                // aslında tentenin bandıydı.
                //
                // Temiz bir sütundan (x=%26,5, rakamların sağı) gerçek yapı:
                //
                //     ust kenar   yumusak gecis, KOYU HALKA YOK
                //     govde       (254,243,237)  SICAK KREM, duz
                //     alt %8      (255,249,246)  bir tik acik
                //     en alt      (233,166,149) -> (125,91,89)  SICAK DUDAK
                //
                // Yani referansta kapsülün konturu hiç yok; hacmi veren şey
                // alttaki sıcak dudak.
                //
                // DERS (bir kesit almadan önce ÖGENİN NEREDE olduğunu ölç):
                // Yanlış sütundan alınan kesit yine de "makul" sayılar
                // veriyor ve insan onları öğenin kendisi sanıyor. Bu turda
                // ikinci kez oldu.
                var durakT = new[] { 0.00f, 0.86f, 0.94f, 1.00f };
                var durakC = new[]
                {
                    new Color(254f / 255f, 243f / 255f, 237f / 255f),
                    new Color(254f / 255f, 243f / 255f, 237f / 255f),
                    new Color(255f / 255f, 249f / 255f, 246f / 255f),
                    new Color(208f / 255f, 132f / 255f, 116f / 255f),
                };

                var kenar = new Color(254f / 255f, 243f / 255f, 237f / 255f);

                float rDis = H * 0.5f;            // tam kapsül
                float kalin = 0f;                 // KONTUR YOK (referansta da yok)
                float rIc = rDis;

                var px = new Color32[W * H];
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float fx = x + 0.5f, fy = y + 0.5f;
                        float dDis = PadMesafe(fx, fy, 0f, 0f, W, H, rDis);
                        if (dDis > 0.7f) { px[y * W + x] = new Color32(0, 0, 0, 0); continue; }

                        float t = 1f - (float)y / (H - 1);
                        Color c = PadTon(durakT, durakC, t);

                        float dIc = PadMesafe(fx, fy, kalin, kalin, W - kalin, H - kalin, rIc);
                        if (dIc > -0.5f)
                            c = Color.Lerp(c, kenar, Mathf.Clamp01(dIc + 1f));

                        c.a = Mathf.Clamp01(0.7f - dDis);
                        px[y * W + x] = c;
                    }

                tex.SetPixels32(px);
                tex.Apply(false, true);
                _coinPad = Sprite.Create(tex, new Rect(0f, 0f, W, H),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                _coinPad.name = "UiCoinPad";
                _coinPad.hideFlags = HideFlags.HideAndDontSave;
                return _coinPad;
            }
        }

        /// <summary>Yuvarlak dikdörtgenin imzalı mesafesi; İÇİ negatif.</summary>
        static float PadMesafe(float x, float y, float x0, float y0, float x1, float y1, float r)
        {
            float cx = Mathf.Max(Mathf.Max(x0 + r - x, 0f), Mathf.Max(x - (x1 - r), 0f));
            float cy = Mathf.Max(Mathf.Max(y0 + r - y, 0f), Mathf.Max(y - (y1 - r), 0f));
            return Mathf.Sqrt(cx * cx + cy * cy) - r;
        }

        /// <summary>Çok duraklı dikey renk rampası.</summary>
        static Color PadTon(float[] t, Color[] c, float u)
        {
            for (int i = 1; i < t.Length; i++)
            {
                if (u > t[i] && i != t.Length - 1) continue;
                float k = Mathf.InverseLerp(t[i - 1], t[i], Mathf.Clamp(u, t[i - 1], t[i]));
                return Color.Lerp(c[i - 1], c[i], k);
            }
            return c[c.Length - 1];
        }

        public static Sprite Radial
        {
            get
            {
                if (_radial != null) return _radial;

                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    name = "UiRadial",
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
                        float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / half);
                        // Sönüm KÜP değil 1,5. KÜP alınca hale merkezde bir
                        // noktaya toplanıyor ve jetonların altında kalıp
                        // ekranda hiç görünmüyordu (ölçüldü: ödül kartında
                        // parlaklık farkı fark edilmiyor). 1,5 daha geniş bir
                        // bulut veriyor, kenarı yine tamamen kayboluyor.
                        float fade = Mathf.Pow(1f - r, 1.5f);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, fade);
                    }

                tex.SetPixels32(pixels);
                tex.Apply(false, true);
                _radial = Sprite.Create(tex, new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                _radial.name = "UiRadial";
                _radial.hideFlags = HideFlags.HideAndDontSave;
                return _radial;
            }
        }

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
            _pencil = null;
            _pencilTip = null;
            _clockFace = null;
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
