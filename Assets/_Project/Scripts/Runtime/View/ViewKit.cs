using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// M2 geçici görsel gereçleri: çalışma anında üretilen paylaşımlı
    /// materyaller (buz, perde, ghost) ve sayaç yazıları. Hepsi placeholder —
    /// M4'te asset tabanlı gerçek görsellerle değiştirilecek.
    ///
    /// Statik önbellek bilinçli: materyaller sahne yeniden kurulunca da aynı
    /// kalır (paylaşım = SRP Batcher dostu), domain reload'da sıfırlanır.
    /// </summary>
    public static class ViewKit
    {
        // `StripCollider` KALDIRILDI (2026-08-17).
        //
        // Görevi `GameObject.CreatePrimitive`'in zorla eklediği çarpıştırıcıyı
        // silmekti. Artık çarpıştırıcı hiç oluşmuyor (bkz. CreateShape), yani
        // silinecek bir şey de yok. Metodun kendisi de zararlıydı:
        // `GetComponent<Collider>()` fizik modülüne DOKUNUYOR ve bu oyunda
        // fizik yok — dokunulan her modül ya derlemede tutulmak zorunda kalır
        // ya da orada patlar.

        static readonly Dictionary<PrimitiveType, Mesh> _shapes =
            new Dictionary<PrimitiveType, Mesh>();

        /// <summary>
        /// Çarpıştırıcısız temel şekil — <c>GameObject.CreatePrimitive</c>'in yerine.
        ///
        /// NEDEN: `CreatePrimitive` nesneye HER ZAMAN bir çarpıştırıcı ekler.
        /// Bu oyunda fizik yok, o yüzden zaten hemen siliniyordu — ama Android
        /// derlemesinde `stripEngineCode` açık ve fizik modülünü kullanan kod
        /// olmadığı için Unity `MeshCollider` sınıfını derlemeden ATIYOR.
        /// Sonuç, 2026-08-17 APK'sinde ekrana düşen hata:
        ///
        ///     Can't add component because class 'MeshCollider' doesn't exist!
        ///
        /// Yani nesne HİÇ KURULAMIYOR ve tahta boş kalıyor. İstemediğimiz bir
        /// bileşeni ekleyip silmek, olmadığı ortamda çökme sebebine dönüşüyor.
        ///
        /// DERS (kullanmadığın şeyi İSTEME): Kırpıcı "kimse kullanmıyorsa at"
        /// diye çalışır. Kodun geçici olarak dokunduğu her modül, o modülü
        /// derlemede tutmak zorunda kalmak ya da orada patlamak demektir.
        /// Doğru çözüm modülü zorla korumak değil, ona hiç dokunmamaktı.
        ///
        /// Ağlar Unity'nin yerleşik kaynaklarından geliyor (fizik gerekmez) ve
        /// PAYLAŞILIYOR — her çağrıda yeni ağ üretilmiyor.
        /// </summary>
        public static GameObject CreateShape(PrimitiveType type, string name)
        {
            if (!_shapes.TryGetValue(type, out var mesh) || mesh == null)
            {
                mesh = Resources.GetBuiltinResource<Mesh>(BuiltinMeshName(type));
                _shapes[type] = mesh;
            }

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            return go;
        }

        static string BuiltinMeshName(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Quad:     return "Quad.fbx";
                case PrimitiveType.Plane:    return "Plane.fbx";
                case PrimitiveType.Sphere:   return "Sphere.fbx";
                case PrimitiveType.Capsule:  return "Capsule.fbx";
                case PrimitiveType.Cylinder: return "Cylinder.fbx";
                default:                     return "Cube.fbx";
            }
        }

        static Material _ice;
        static Material _curtainPanel;
        static Material _curtainFrame;
        static readonly Dictionary<BlockColor, Material> _ghosts =
            new Dictionary<BlockColor, Material>();

        /// <summary>
        /// Buz bloğu — OPAK.
        ///
        /// DERS (referansı doğru okumak, yarı saydamlıkla boğuşmaktan iyidir):
        /// Buz kabuğu önce yarı saydam yapılmıştı; blok buzun içinden görünsün
        /// isteniyordu. Ama iki sorun çıktı: (1) saydam nesneler derinlik yazmaz
        /// ve sıralamaya bağımlıdır, kamera bu oyunda çok geride durduğu için
        /// buz sürekli bloğun arkasına düşüyordu; (2) referans oyunda zaten
        /// ALTTAKİ RENK GÖRÜNMÜYOR — donmuş blok düz bir buz kalıbı, renk ancak
        /// buz kırılınca ortaya çıkıyor (video kuralı: "buz rengi gizler").
        ///
        /// Opak yapmak hem referansa uyuyor hem de bütün sıralama problemini
        /// ortadan kaldırıyor. Bazen doğru çözüm, yanlış soruyu sormayı bırakmak.
        /// </summary>
        public static Material Ice
        {
            get
            {
                if (_ice == null)
                {
                    // SHADER DEĞİŞTİ: URP/Lit yerine oyunun kendi tuğla
                    // shader'ı (2026-08-18).
                    //
                    // DERS (aynı mesh, yanlış shader = kaynaşan yüzeyler):
                    // Buz kabuğu referanstaki gibi DÜZ levha yapılınca
                    // (saplama yok) yan yana duran buz blokları tek bir dev
                    // camgöbeği lekeye dönüştü — 10. bölümde dokuz buz bloğu
                    // ekranda tek parça çıktı ve tahta okunmaz oldu.
                    //
                    // Sebep: URP/Lit mesh'in KÖŞE RENKLERİNİ kullanmıyor.
                    // Tuğla mesh'i kenarlarına ve pahlarına sahte AO gömüyor
                    // (`BrickMeshBuilder`), ama o bilgi URP/Lit'te yere
                    // düşüyordu; ayrımı yapan tek şey saplamaların ışığı
                    // farklı açıyla alması kalmıştı. Saplama gidince ayrım da
                    // gitti.
                    //
                    // `BlockOut/Brick` o köşe renklerini çarpan olarak
                    // kullanıyor — renkli bloklarda kenarları okunur yapan
                    // shader'ın ta kendisi. Buz da aynı dili konuşmalı.
                    var shader = Shader.Find("BlockOut/Brick")
                                 ?? Shader.Find("Universal Render Pipeline/Lit")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit");
                    _ice = new Material(shader) { name = "Ice_TEMP" };

                    // RENK YENİDEN ÖLÇÜLDÜ (2026-08-18, `Levels.mp4` 05:40,
                    // 13. bölüm — tahtada üç buz bloğu birden var).
                    // Referans gövde `#1DB3F8`, üst bandı `#1996F0`.
                    // Bizimki `#3D9EEB` idi: hem daha koyu hem daha MAVİ.
                    // Kıyas için aynı karedeki normal mavi blok `#024DFB` —
                    // yani buzun mavi bloktan belirgin biçimde AÇIK ve
                    // CAMGÖBEĞİ olması gerekiyor, yoksa "donmuş mavi blok" ile
                    // "mavi blok" aynı şey gibi okunuyor.
                    var color = new Color(0.114f, 0.702f, 0.973f);
                    if (_ice.HasProperty("_BaseColor")) _ice.SetColor("_BaseColor", color);
                    _ice.color = color;
                    // Buz parlak ve pürüzsüz: ışığı toplayınca "cam" hissi veriyor.
                    if (_ice.HasProperty("_Smoothness")) _ice.SetFloat("_Smoothness", 0.92f);
                    if (_ice.HasProperty("_Metallic")) _ice.SetFloat("_Metallic", 0f);
                }
                return _ice;
            }
        }

        static Material _gateIce;
        static Material _iceFrost;

        /// <summary>
        /// KAPI buzu — blok buzundan AYRI ve belirgin biçimde daha SOLUK
        /// (4. tur, G24/I35).
        ///
        /// Kullanıcı: "Kapılardaki buz ile blokların üstündeki buz aynı —
        /// ayrıştırılmalı."
        ///
        /// DERS (iki farklı KURAL aynı görünemez): Blok buzu "bu bloğu
        /// tutamazsın" der; kapı buzu "bu kapı henüz açılmadı" der. İkisi de
        /// aynı camgöbeği kalıpla çizilince oyuncu tahtayı okurken hangisinin
        /// hangisi olduğunu ancak konumdan çıkarabiliyordu. Referansta
        /// (`Levels.mp4` 6. bölüm) kapı buzu neredeyse BEYAZ, blok buzu ise
        /// doygun camgöbeği.
        /// </summary>
        public static Material GateIce
        {
            get
            {
                if (_gateIce == null)
                {
                    var shader = Shader.Find("BlockOut/Brick")
                                 ?? Shader.Find("Universal Render Pipeline/Lit")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit");
                    _gateIce = new Material(shader) { name = "GateIce" };
                    var color = new Color(0.70f, 0.90f, 0.98f);
                    if (_gateIce.HasProperty("_BaseColor")) _gateIce.SetColor("_BaseColor", color);
                    _gateIce.color = color;
                    if (_gateIce.HasProperty("_Smoothness")) _gateIce.SetFloat("_Smoothness", 0.95f);
                    if (_gateIce.HasProperty("_Metallic")) _gateIce.SetFloat("_Metallic", 0f);
                }
                return _gateIce;
            }
        }

        /// <summary>
        /// Buz kalıbının üstündeki AÇIK kırağı paneli.
        ///
        /// DERS (buzu buz yapan şey, tek bir mavi değil İKİ derinliktir):
        /// Kalıp tek düz camgöbeğiyken "mavi plastik levha" gibi okunuyordu.
        /// Referansta yüzeyin ortasında daha açık, neredeyse beyaz bir alan
        /// var: donmuş suyun içindeki hava. İki ton arasındaki fark, malzemeyi
        /// saydam gösteren şey.
        /// </summary>
        public static Material IceFrost
        {
            get
            {
                if (_iceFrost == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit")
                                 ?? Shader.Find("Sprites/Default");
                    _iceFrost = new Material(shader) { name = "IceFrost" };
                    var color = new Color(0.78f, 0.94f, 1f);
                    if (_iceFrost.HasProperty("_BaseColor")) _iceFrost.SetColor("_BaseColor", color);
                    if (_iceFrost.HasProperty("_Color")) _iceFrost.SetColor("_Color", color);
                }
                return _iceFrost;
            }
        }

        static readonly Dictionary<BlockColor, Material> _layerFill =
            new Dictionary<BlockColor, Material>();
        static readonly Dictionary<BlockColor, Material> _layerRim =
            new Dictionary<BlockColor, Material>();

        /// <summary>
        /// İç katmanın dolgusu — renk paletinden bağımsız, KENDİ materyali.
        ///
        /// DERS (paylaşılan blok materyalini ödünç alma): İlk düşünce
        /// `BoardBuilder.GetBlockMaterial` ile aynı materyali kullanmaktı; o
        /// zaman iç panel dış gövdeyle birebir aynı tonda çıkıyor ve "içteki
        /// blok" değil "aynı bloğun ortası" gibi okunuyordu. Referansta iç
        /// panel gömülü olduğu için bir tık KOYU — ışığı daha az alıyor.
        /// </summary>
        public static Material LayerFill(ColorPaletteSO palette, BlockColor color)
        {
            if (_layerFill.TryGetValue(color, out var cached) && cached != null)
                return cached;

            var entry = palette != null ? palette.Get(color) : null;
            Color baseColor = entry != null ? entry.uiColor : Color.gray;

            var shader = Shader.Find("BlockOut/Brick")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "LayerFill_" + color };
            // TAM DOYGUNLUK (5. tur): renk %86'ya kısılıyordu ve iç katman
            // "tozlu" görünüyordu. Referansta iç katman, o renkteki normal
            // bir bloktan ayırt edilemez — aynı renk, aynı kabartma.
            var c = baseColor;
            c.a = 1f;
            mat.SetColor("_BaseColor", c);
            _layerFill[color] = mat;
            return mat;
        }

        /// <summary>
        /// İç panelin çevresindeki ince kenar — DIŞ rengin açık tonu.
        /// Referansta sarı gövdenin ortasındaki yeşil paneli açık sarı bir
        /// çizgi çeviriyor; o çizgi iki katmanı birbirinden ayıran şey.
        /// </summary>
        public static Material LayerRim(ColorPaletteSO palette, BlockColor outerColor)
        {
            if (_layerRim.TryGetValue(outerColor, out var cached) && cached != null)
                return cached;

            var entry = palette != null ? palette.Get(outerColor) : null;
            Color baseColor = entry != null ? entry.uiColor : Color.gray;

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "LayerRim_" + outerColor };
            // Referansta hat, dış rengin AÇIK tonu — beyaza yakın değil.
            var c = Color.Lerp(baseColor, Color.white, 0.42f);
            mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            _layerRim[outerColor] = mat;
            return mat;
        }

        static Material _iceRim;

        /// <summary>
        /// Buz kalıbının KOYU KENARI.
        ///
        /// DERS (düz renkli komşular tek kütleye kaynar): Buz kabuğu düz levha
        /// yapılınca (referansta öyle, saplama yok) yan yana duran iki buzlu
        /// blok arasında hiçbir sınır kalmadı — 10. bölümde dokuz buz bloğu
        /// ekranda TEK bir dev camgöbeği leke olarak çıktı ve tahta okunmaz
        /// oldu. Sebep: `Ice` materyali URP/Lit ve mesh'in KÖŞE RENKLERİNİ
        /// kullanmıyor; eskiden ayrımı yapan şey saplamaların ışığı farklı
        /// açıyla almasıydı. Saplama gidince gölge kaynağı da gitti.
        ///
        /// Referansta her buz kalıbının kendi koyu kenarı var (gövde `#1DB3F8`,
        /// üst bandı `#1996F0`). Aynı kabuk tekniğiyle (51. maddedeki kontur)
        /// koyu bir bilezik çiziliyor: silüet biraz büyütülüp ön yüzleri
        /// kırpılıyor, geriye kalan arka yüzler kenarda ince bir çerçeve
        /// bırakıyor.
        /// </summary>
        public static Material IceRim
        {
            get
            {
                if (_iceRim == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit")
                                 ?? Shader.Find("Sprites/Default");
                    _iceRim = new Material(shader) { name = "IceRim" };
                    var color = new Color(0.055f, 0.416f, 0.686f);
                    _iceRim.SetColor("_BaseColor", color);
                    if (_iceRim.HasProperty("_Color")) _iceRim.SetColor("_Color", color);
                    if (_iceRim.HasProperty("_Cull"))
                        _iceRim.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
                }
                return _iceRim;
            }
        }

        static Material _frostShard;
        static Material _curtainStreak;
        static Material _gridLine;
        static Material _iceGloss;

        /// <summary>
        /// Buzun üstündeki kristal parçaların materyali.
        ///
        /// Kabuktan daha AÇIK ve daha parlak: parçaların işi ışığı yakalamak.
        /// Aynı materyali paylaşırlarsa siluetleri kabuğun içinde kaybolur ve
        /// eklenmiş olmaları hiçbir şey değiştirmez.
        /// </summary>
        public static Material FrostShard
        {
            get
            {
                if (_frostShard == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit");
                    _frostShard = new Material(shader) { name = "FrostShard_TEMP" };

                    var color = new Color(0.86f, 0.96f, 1f);
                    if (_frostShard.HasProperty("_BaseColor")) _frostShard.SetColor("_BaseColor", color);
                    _frostShard.color = color;
                    if (_frostShard.HasProperty("_Smoothness")) _frostShard.SetFloat("_Smoothness", 0.92f);
                    if (_frostShard.HasProperty("_Metallic")) _frostShard.SetFloat("_Metallic", 0f);
                }
                return _frostShard;
            }
        }

        /// <summary>Buzun üst yüzeyindeki parlaklık şeridi.</summary>
        public static Material IceGloss =>
            _iceGloss != null ? _iceGloss
                : (_iceGloss = CreateTransparent(
                    "IceGloss", new Color(1f, 1f, 1f, 0.42f), 3020));

        /// <summary>Zemindeki hücre ızgarası çizgisi — çok soluk.</summary>
        /// <summary>
        /// Tahtanın hücre ayraçları — BEYAZ DEĞİL, KOYU.
        ///
        /// DERS (ayracın yönü referanstan okunur, sezgiden değil): Bu çizgiler
        /// "%7 beyaz" ile kuruluydu; gerekçe "sınırı sezdir, dikkat çekme"
        /// idi ve tek başına makul. Ama referans ölçüldüğünde (oynanış videosu,
        /// 00:28 karesi) tam tersi çıktı: hücre `#1E1B50`, ayraç `#120F2F`
        /// — yani ayraç hücreden KOYU. Bizde ekranda `#4E4E59` ölçüldü, yani
        /// hem yanlış yönde hem beklenenden parlak.
        ///
        /// Neden koyu doğru: tahta zaten koyu bir kuyu; açık çizgi orada bir
        /// ÇIKINTI gibi okunuyor ve boş hücreler "ızgara kâğıdı" gibi
        /// görünüyor. Koyu çizgi ise bir OLUK — hücreler kabartma kalıyor ve
        /// bloklar oraya oturuyormuş hissi doğuyor.
        ///
        /// %34 siyah, `#1E1E53` hücrenin üstünde ekranda ~`#141436` veriyor:
        /// referansın `#120F2F`'ine yakın.
        /// </summary>
        public static Material GridLine =>
            _gridLine != null ? _gridLine
                : (_gridLine = CreateTransparent(
                    "GridLine", new Color(0f, 0f, 0f, 0.34f), 2990));

        /// <summary>Buzlu camın üstündeki ışık çizgisi — hafif parlak, saydam.</summary>
        /// <summary>
        /// Perde yüzeyindeki yatay tırtıl çizgileri — AÇIK DEĞİL, KOYU.
        ///
        /// ÖLÇÜM (22. bölümün perdesinden dikey dilim): dolgu `(58, 37, 188)`,
        /// ayırıcı çizgi `(48, 29, 152)`, tekrar aralığı 20 piksel (hücrenin
        /// üçte biri). Çizgi dolgudan KOYU; siyahın α ≈ 0.19 ile bindirilmiş
        /// hâli.
        ///
        /// DERS (AYNI HATA, İKİNCİ YÜZEY): Tahta ızgarasında da ayraçları
        /// "%7 beyaz" yapmıştık ve ölçüm tam tersini söylemişti — ayraç
        /// hücreden koyu olmalıydı. Burada aynı yanılgıya perdede düştüm:
        /// çizgiyi beyaz sandım, yalnızca alfasını düşürdüm. Büyüklüğü
        /// düzeltmek yönü düzeltmez. Açık çizgi yüzeyde ÇIKINTI, koyu çizgi
        /// OLUK okunur; perde bir levha olduğu için doğru olan oluk.
        /// </summary>
        public static Material CurtainStreak =>
            _curtainStreak != null ? _curtainStreak
                : (_curtainStreak = CreateTransparent(
                    "CurtainStreak", new Color(0f, 0f, 0f, 0.34f), 3010));

        static Material _curtainSparkle;

        /// <summary>
        /// Yüzeye serpilen parıltılar — çizgilerin tersine AÇIK.
        ///
        /// Çizgi ile parıltı aynı materyali paylaşamaz: biri oluk, diğeri
        /// ışık. Aynı yüzeyde ikisi de var ve zıt yönde çalışıyorlar.
        /// </summary>
        public static Material CurtainSparkle =>
            _curtainSparkle != null ? _curtainSparkle
                : (_curtainSparkle = CreateTransparent(
                    "CurtainSparkle", new Color(1f, 1f, 1f, 0.10f), 3011));

        /// <summary>
        /// URP için doğru kurulmuş yarı saydam materyal.
        ///
        /// DERS (doğru hat, doğru shader): Buz eskiden `Sprites/Default` ile
        /// çiziliyordu — o YERLEŞİK (built-in) render hattının shader'ı. URP'de
        /// bir şeyler çiziyor ama saydamlık/derinlik davranışı garanti değil.
        /// URP'de saydamlık, shader'ın kendisiyle değil ANAHTAR KELİMELERLE
        /// açılır: `_Surface=1`, `_SURFACE_TYPE_TRANSPARENT` ve harmanlama
        /// modu elle kurulmalı; yalnız `color.a` düşürmek yetmez, materyal
        /// yine opak hattında çizilir.
        /// </summary>
        /// <summary>Verilen renkte yeni bir saydam materyal — efektler için.</summary>
        public static Material Translucent(Color color) =>
            CreateTransparent("Beam", color, 3000);

        /// <summary>
        /// Bir materyalin tek kullanımlık OPAK kopyası.
        ///
        /// Nerede gerekiyor: bir nesnenin rengini canlandırırken saydamlık
        /// İSTEMEDİĞİMİZ yerlerde. Kapı sönerken `Translucent` kullanılıyordu;
        /// saydam materyal derinliğe yazmadığı için prizmanın üst kapağı, pahı
        /// ve yan duvarı üst üste harmanlanıp ekranda uzun çizgiler bırakıyordu
        /// (bkz. <c>GateView.FadeToGhost</c>).
        ///
        /// Kaynak materyalin KOPYASI alınıyor, yeni bir materyal
        /// kurulmuyor: gölgeci, ışıklandırma ve bütün özellikler aynı kalsın
        /// ki geçişin ilk karesinde parlaklık sıçraması olmasın.
        /// </summary>
        public static Material CopyFor(Material source, string name)
        {
            if (source == null) return new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            return new Material(source) { name = name + "_TEMP" };
        }

        /// <summary>
        /// KATKILI (additive) ışık materyali — hale, parlama, ışın için.
        ///
        /// DERS (ışık ekler, boyamaz): Alfa harmanlama arkadaki rengin
        /// ÜSTÜNE yazar; sönerken siyaha giden bir alfa katmanı arkasındaki
        /// yüzeyi KARARTIR ve "ışık" değil "leke" gibi okunur. Katkılı
        /// harmanlama arkadaki renge EKLER: siyah eklemek hiçbir şey yapmaz,
        /// yani hale sönerken iz bırakmaz. Gerçek ışık da böyle davranır.
        /// </summary>
        public static Material Additive(Color color)
        {
            var mat = CreateTransparent("Glow", color, 3100);
            if (mat.HasProperty("_SrcBlend"))
            {
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_Blend", 1f);          // URP'de 1 = additive
            }
            return mat;
        }

        static Material CreateTransparent(string name, Color color, int queue)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // en kötü ihtimalde

            var mat = new Material(shader) { name = name + "_TEMP" };

            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);                 // 0 opak, 1 saydam
                mat.SetFloat("_Blend", 0f);                   // alfa harmanlama
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);                  // saydam derinlik yazmaz
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.SetShaderPassEnabled("ShadowCaster", false);
                mat.SetShaderPassEnabled("DepthOnly", false);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.renderQueue = queue;
            return mat;
        }

        static Material _floor;
        static Texture2D _floorTexture;

        /// <summary>
        /// Zemin: TEK quad + döşenen prosedürel doku (hücre + ızgara çizgisi +
        /// kesişim noktası).
        ///
        /// DERS (çizim çağrısı = maliyet): Önceden her hücre için ayrı quad
        /// üretiyorduk — 6×8 tahtada 48 nesne, 48 çizim çağrısı ve komşu
        /// quad'ların kenarlarında z-fighting (titreyen çizgiler). Tek quad
        /// üstüne döşenen doku hem 1 çizim çağrısı hem de kusursuz kenar.
        /// </summary>
        public static Material FloorMaterial(BlockOut.Runtime.Config.BlockVisualConfigSO cfg)
        {
            if (_floor != null) return _floor;

            _floor = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = "Floor",
                mainTexture = BuildFloorTexture(cfg)
            };
            _floor.SetColor("_BaseColor", Color.white);
            _floor.mainTexture.wrapMode = TextureWrapMode.Repeat;
            return _floor;
        }

        static Texture2D BuildFloorTexture(BlockOut.Runtime.Config.BlockVisualConfigSO cfg)
        {
            const int size = 128;
            Color cell = cfg != null ? cfg.floorColorA : new Color(0.17f, 0.15f, 0.31f);
            // Çizgi/nokta renkleri hücre renginden TÜRETİLİR: zemin rengini
            // değiştirdiğinde kontrast kendiliğinden korunur.
            float lineDarken = cfg != null ? cfg.floorLineDarken : 0.6f;
            float dotDarken = cfg != null ? cfg.floorDotDarken : 0.4f;
            Color line = new Color(cell.r * lineDarken, cell.g * lineDarken, cell.b * lineDarken, 1f);
            Color dot = new Color(cell.r * dotDarken, cell.g * dotDarken, cell.b * dotDarken, 1f);
            float lineWidth = cfg != null ? cfg.floorLineWidth : 0.045f;
            float dotSize = cfg != null ? cfg.floorDotSize : 0.09f;

            _floorTexture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;

                    // Kenara olan mesafe: hücre sınırında çizgi.
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));

                    // Hücrenin ORTASI hafif aydınlık: koyu zeminlerde bile
                    // hücreler tek tek okunur (yalnızca çizgiye güvenmek,
                    // karanlık renklerde yetersiz kalıyordu).
                    float centerLift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / 0.35f));
                    Color cellShade = Color.Lerp(
                        new Color(cell.r * 0.86f, cell.g * 0.86f, cell.b * 0.86f, 1f),
                        new Color(cell.r * 1.10f, cell.g * 1.10f, cell.b * 1.10f, 1f),
                        centerLift);

                    Color color = edge < lineWidth ? line : cellShade;

                    // Köşelerdeki nokta (döşenince kesişimlerde birleşir).
                    float dx = Mathf.Min(u, 1f - u);
                    float dy = Mathf.Min(v, 1f - v);
                    if (Mathf.Sqrt(dx * dx + dy * dy) < dotSize) color = dot;

                    pixels[y * size + x] = color;
                }
            }
            _floorTexture.SetPixels32(pixels);
            _floorTexture.Apply(true, false);
            return _floorTexture;
        }

        static Material _arrowGhost;

        /// <summary>Rengi tükenmiş kapının soluk oku (kapı gizlenmez, solar).</summary>
        public static Material ArrowGhostMaterial
        {
            get
            {
                if (_arrowGhost == null)
                {
                    var shader = Shader.Find("BlockOut/Brick")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit");
                    _arrowGhost = new Material(shader) { name = "GateArrowGhost" };
                    _arrowGhost.SetColor("_BaseColor", new Color(0.42f, 0.40f, 0.50f));
                }
                return _arrowGhost;
            }
        }

        static Material _outline;

        /// <summary>
        /// Tutulan bloğun etrafındaki BEYAZ kontur (2. tur, 51. madde).
        ///
        /// Kontur, bloğun etrafını saran DÜZ bir halka
        /// (<see cref="BrickMeshBuilder.GetOutlineRing"/>) ve `BlockOut/Outline`
        /// shader'ı ile ÇİZİLDİĞİ ANDA her şeyin üstüne biniyor.
        ///
        /// DERS (denenip ELENEN yöntem — ters kabuk): İlk sürüm klasik
        /// "inverted hull" idi: aynı mesh biraz büyütülüp ön yüzleri
        /// kırpılarak (Cull Front) çizilir, geriye kalan arka yüzler ince bir
        /// çerçeve gibi görünür. Bu teknik nesnenin ETRAFINI değil, kabuğun
        /// KAMERAYA ARKASINI DÖNEN kısmını boyar. Bizim kamera tahtaya 80°
        /// eğimle, yani neredeyse tepeden bakıyor; o açıda kabuğun yalnız iki
        /// kenarı arkasını dönüyor ve kontur diğer iki kenarda hiç
        /// görünmüyordu. Referansta ise dört kenarda da 2-3 piksel var.
        ///
        /// Işıksız olmalı: kontur bir yüzey değil bir İŞARET; sahnenin
        /// ışığından etkilenirse bir yüzü parlak diğeri sönük çıkar ve
        /// "çerçeve" okuması bozulur.
        /// </summary>
        public static Material Outline
        {
            get
            {
                if (_outline == null)
                {
                    // Malzeme bir ASSET; Resources'ta duruyor.
                    //
                    // DERS (build'de elenen shader): `Shader.Find` editörde
                    // her zaman çalışır çünkü editörde bütün shader'lar
                    // yüklüdür — 18. maddede bunu bir kez yaşadık. Build'de
                    // ise hiçbir malzemenin kullanmadığı shader ELENİR.
                    // Malzemeyi asset olarak tutup Resources'tan yüklemek
                    // shader'ı o malzemenin bağımlılığı yapar; eleme riski
                    // ortadan kalkar.
                    _outline = Resources.Load<Material>("BlockOutline");

                    if (_outline == null)
                    {
                        // Asset silinirse oyun pembe bloklarla değil, biraz
                        // eksik bir konturla çalışsın.
                        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                                     ?? Shader.Find("Sprites/Default");
                        _outline = new Material(shader) { name = "BlockOutline" };
                        _outline.SetColor("_BaseColor", Color.white);
                        if (_outline.HasProperty("_Color")) _outline.SetColor("_Color", Color.white);
                        if (_outline.HasProperty("_Cull"))
                            _outline.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                        _outline.renderQueue = 3100;
                    }
                }
                return _outline;
            }
        }

        static Material _shadow;
        static Texture2D _shadowTexture;

        /// <summary>
        /// Blokların altındaki yumuşak temas gölgesi.
        ///
        /// DERS (derinlik ipuçları): Tepeden bakan bir kamerada tüm üst yüzler
        /// ışığa aynı açıyla durur, bu yüzden sahne DÜZ görünür. Gerçek gölge
        /// hesaplamak mobilde pahalıdır; blokların altına yumuşak bir leke
        /// koymak ise neredeyse bedavadır ve "nesne zeminin ÜSTÜNDE duruyor"
        /// bilgisini tek başına verir. Oyun grafiklerinde buna blob shadow denir.
        /// </summary>
        public static Material ShadowMaterial
        {
            get
            {
                if (_shadow == null)
                {
                    _shadow = new Material(Shader.Find("Sprites/Default")) { name = "BlobShadow" };
                    _shadow.mainTexture = ShadowTexture;
                }
                return _shadow;
            }
        }

        static Texture2D ShadowTexture
        {
            get
            {
                if (_shadowTexture != null) return _shadowTexture;

                // Kenarları yumuşak, köşeleri yuvarlatılmış dikdörtgen leke.
                const int size = 64;
                _shadowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp
                };

                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Merkezden kenara doğru yumuşak düşüş (superellipse).
                        float nx = (x + 0.5f) / size * 2f - 1f;
                        float ny = (y + 0.5f) / size * 2f - 1f;
                        float d = Mathf.Pow(Mathf.Abs(nx), 4f) + Mathf.Pow(Mathf.Abs(ny), 4f);
                        float alpha = Mathf.Clamp01(1f - Mathf.Pow(d, 0.75f));
                        alpha = alpha * alpha;
                        pixels[y * size + x] = new Color32(0, 0, 0, (byte)(alpha * 255));
                    }
                }
                _shadowTexture.SetPixels32(pixels);
                _shadowTexture.Apply(false, true);
                return _shadowTexture;
            }
        }

        /// <summary>Ayar değişince üretilen materyaller yeniden kurulsun.</summary>
        public static void ClearCache()
        {
            _ice = null;
            _gateIce = null;
            _iceFrost = null;
            foreach (var mat in _counterMaterials.Values)
                if (mat != null) Object.DestroyImmediate(mat);
            _counterMaterials.Clear();
            _curtainPanel = null;
            _curtainFrame = null;
            _curtainStreak = _curtainSparkle = null;
            _badgeRim = _badgeFace = null;
            _generatorBody = null;
            _solids.Clear();
            _arrow = null;
            _arrowFace.Clear();
            _arrowGroove.Clear();
            _arrowGhost = null;
            _particle = null;
            _floor = null;
            _floorTexture = null;
            _ghosts.Clear();
        }

        static Material _arrow;

        /// <summary>
        /// Kapı okları: tuğla shader'ı kullanılır ki ok da speküler parlaklık
        /// alsın ve kabartma kenarları belirginleşsin (unlit materyal düz
        /// beyaz bir leke bırakıyordu).
        /// </summary>
        public static Material ArrowMaterial
        {
            get
            {
                if (_arrow == null)
                {
                    var shader = Shader.Find("BlockOut/Brick")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit");
                    _arrow = new Material(shader) { name = "GateArrow" };
                    _arrow.SetColor("_BaseColor", new Color(1f, 0.99f, 0.96f));
                }
                return _arrow;
            }
        }

        static readonly Dictionary<BlockColor, Material> _arrowFace =
            new Dictionary<BlockColor, Material>();

        static readonly Dictionary<BlockColor, Material> _arrowGroove =
            new Dictionary<BlockColor, Material>();

        /// <summary>
        /// Yönlü blokların üstündeki okun ÜST yüzü — bloğun renginin AÇIK tonu.
        ///
        /// DERS (beyaz, renk oyununda bilgiyi siler): Ok eskiden bembeyazdı ve
        /// bloğun rengini bastırıyordu; oyuncu "hangi renk?" sorusunu okun
        /// etrafından cevaplamak zorunda kalıyordu (4. tur H31). Referansta ok
        /// bloğun KENDİ renginin açığı: okunabilir ama rengi ele geçirmiyor.
        ///
        /// Renk başına tek materyal — SRP Batcher paylaşımlı materyalleri
        /// tek çizim çağrısında toplayabiliyor.
        /// </summary>
        public static Material AxisArrowFace(ColorPaletteSO palette, BlockColor color)
        {
            if (_arrowFace.TryGetValue(color, out var cached) && cached != null) return cached;

            var entry = palette != null ? palette.Get(color) : null;
            Color baseColor = entry != null ? entry.uiColor : Color.gray;

            var shader = Shader.Find("BlockOut/Brick")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "AxisArrowFace_" + color };
            // ÖLÇÜM (41-50 yürüyüşü, 12:32): okun DOLGUSU bloğun rengiyle
            // neredeyse aynı — blok gövdesi (253,192,13), okun yüzü aynı
            // aralıkta. Ayrımı yapan şey renk değil, kenarındaki koyu oluk ve
            // kabartmanın ışığı. Beyaza %34 karıştırmak oku bloğun üstüne
            // YAPIŞTIRILMIŞ açık bir çıkartma gibi gösteriyordu.
            var c = Color.Lerp(baseColor, Color.white, 0.10f);
            c.a = 1f;
            mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            _arrowFace[color] = mat;
            return mat;
        }

        /// <summary>
        /// Okun altındaki KOYU oluk hattı — aynı rengin koyusu.
        ///
        /// DERS (kabartmayı okutan şey gölgedir): Yalnız açık tonlu bir ok,
        /// tepeden bakan kamerada düz bir leke gibi durur; kenarını çizen bir
        /// koyu hat olmadan "basılmış" değil "boyanmış" görünür.
        /// </summary>
        public static Material AxisArrowGroove(ColorPaletteSO palette, BlockColor color)
        {
            if (_arrowGroove.TryGetValue(color, out var cached) && cached != null) return cached;

            var entry = palette != null ? palette.Get(color) : null;
            Color baseColor = entry != null ? entry.uiColor : Color.gray;

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "AxisArrowGroove_" + color };
            var c = baseColor * 0.5f;
            c.a = 1f;
            mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            _arrowGroove[color] = mat;
            return mat;
        }

        static Material _particle;

        /// <summary>
        /// Parçacık materyali: vertex rengini olduğu gibi gösteren unlit yol.
        /// ParticleSystem her parçacığın rengini vertex rengiyle taşıdığı için
        /// tek materyal tüm renkler için yeterlidir.
        /// </summary>
        public static Material ParticleMaterial
        {
            get
            {
                if (_particle == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                 ?? Shader.Find("Sprites/Default");
                    _particle = new Material(shader) { name = "Crumb_TEMP" };
                }
                return _particle;
            }
        }

        /// <summary>
        /// Perde yüzeyi — BUZLU CAM DEĞİL, DOYGUN MOR PANEL.
        ///
        /// REFERANS ÖLÇÜMÜ (21-30 yürüyüşü, 22. bölümün büyük perdesi):
        /// dolgu `(58, 37, 188)`. Bizimki `(0.22, 0.16, 0.38)` idi — hem çok
        /// koyu hem gri. Ekranda "buzlu cam" değil "kirli gri kutu" gibi
        /// duruyordu.
        ///
        /// DERS (malzemeyi ADLANDIRMAK, onu görmek değildir): Kodda perde
        /// baştan beri "buzlu cam" diye anılıyordu ve bütün süsler o benzetme
        /// üzerine kuruldu — rastgele açılı ışık çizgileri, yarı saydam
        /// lekeler. Referansa bakıldığında ortada cam falan yok: perde,
        /// tahtanın kuyusuyla aynı aileden DOYGUN MOR bir levha, üstünde
        /// düzenli yatay tırtıllar ve serpilmiş parıltılar var. Benzetmeyi
        /// referanstan almak yerine kendi kafamızdan kurunca, sonraki her
        /// ayrıntı o yanlış benzetmeye hizmet etti.
        /// </summary>
        public static Material CurtainPanel
        {
            get
            {
                if (_curtainPanel == null)
                    // Materyal rengi HEDEF DEĞİL, hedefin ışık öncesi hâli:
                    // sahne ışığı yüzeyi açtığı için `#3A25BC` yazınca ekranda
                    // `(71, 58, 210)` çıkıyordu. Fark (+0.05, +0.08, +0.09)
                    // ölçülüp materyalden düşüldü.
                    _curtainPanel = MakeLit("CurtainPanel_TEMP",
                        new Color(0.176f, 0.063f, 0.650f));
                return _curtainPanel;
            }
        }

        /// <summary>
        /// Perde çerçevesi — ölçüm: üst kenar `(232, 155, 0)`, yan kenarlar
        /// ışık aldıkça koyulaşıp `(149, 52, 0)`'a iniyor. Bizimki
        /// `(0.85, 0.65, 0.2)` idi: sarıya çalıyordu, referans TURUNCU.
        /// </summary>
        public static Material CurtainFrame
        {
            get
            {
                if (_curtainFrame == null)
                    _curtainFrame = MakeLit("CurtainFrame_TEMP",
                        new Color(0.910f, 0.608f, 0f));       // #E89B00
                return _curtainFrame;
            }
        }

        static Material _badgeRim, _badgeFace;

        /// <summary>Sayaç rozetinin altın çerçevesi — ölçüm `(255, 154, 12)`.</summary>
        public static Material BadgeRim =>
            _badgeRim != null ? _badgeRim
                : (_badgeRim = MakeLit("BadgeRim_TEMP", new Color(1f, 0.604f, 0.047f)));

        /// <summary>Rozetin koyu içi — ölçüm `(133, 42, 0)`.</summary>
        public static Material BadgeFace =>
            _badgeFace != null ? _badgeFace
                : (_badgeFace = MakeLit("BadgeFace_TEMP", new Color(0.522f, 0.165f, 0f)));

        static Material _generatorBody;

        /// <summary>Blok üretecinin mor makine gövdesi.</summary>
        static readonly Dictionary<string, Material> _solids =
            new Dictionary<string, Material>();

        /// <summary>
        /// Ada göre önbelleklenen düz renkli tuğla materyali.
        ///
        /// DERS (paylaşılan materyal = tek çizim çağrısı): Makine altı ayrı
        /// parçadan oluşuyor ve bir bölümde birkaç makine olabiliyor. Her
        /// parçaya kendi materyalini üretmek onlarca ayrı çizim çağrısı
        /// demekti; ada göre paylaşınca aynı renkteki bütün parçalar tek
        /// grupta çiziliyor.
        ///
        /// Tuğla shader'ı bilinçli: makine de bloklarla aynı ışığı almalı,
        /// yoksa tahtanın kenarında başka bir dünyadan gelmiş gibi durur.
        /// </summary>
        public static Material Solid(string name, Color color)
        {
            if (_solids.TryGetValue(name, out var cached) && cached != null) return cached;

            var shader = Shader.Find("BlockOut/Brick")
                         ?? Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            _solids[name] = mat;
            return mat;
        }

        public static Material GeneratorBody
        {
            get
            {
                if (_generatorBody == null)
                    _generatorBody = MakeLit("GeneratorBody_TEMP", new Color(0.55f, 0.24f, 0.72f));
                return _generatorBody;
            }
        }

        /// <summary>
        /// Rengi tükenen kapının kapalı hali.
        ///
        /// Önceden renk açık griye doğru karıştırılıyordu ve pastel/solgun
        /// görünüyordu — "bozuk" hissi veriyordu. Referansta kapı SÖNÜK
        /// görünür: aynı renk ama koyu ve doygunluğu düşük. Karartmak
        /// "devre dışı" mesajını çok daha net veriyor.
        /// </summary>
        public static Material GhostFor(ColorPaletteSO palette, BlockColor color)
        {
            if (_ghosts.TryGetValue(color, out var mat)) return mat;

            var entry = palette.Get(color);
            Color baseColor = entry != null ? entry.uiColor : Color.gray;

            float grey = baseColor.r * 0.299f + baseColor.g * 0.587f + baseColor.b * 0.114f;
            Color desaturated = Color.Lerp(new Color(grey, grey, grey), baseColor, 0.45f);
            Color dimmed = desaturated * 0.38f;
            dimmed.a = 1f;

            mat = MakeLit($"Ghost_{color}", dimmed);
            _ghosts[color] = mat;
            return mat;
        }

        /// <summary>
        /// Sayaçların üç ayrı görünümü.
        ///
        /// REFERANS ÖLÇÜMÜ (`menus,powerups,vs.mp4` 01:25): rakam rengi TEK
        /// DEĞİL, durduğu yüzeye göre değişiyor. Buz bloğunun üstündeki rakam
        /// açık camgöbeği (111,226,255), donmuş KAPININ üstündeki ise krem
        /// (246,231,217). İkisi de kendi zemininin AÇIK tonunda; yani sayaç
        /// zeminden kopuk bir rozet değil, aynı malzemenin parlayan yüzü.
        ///
        /// DERS (tek renk iki zemine yetmez): Bizde her sayaç kremdi. Krem,
        /// kapının soluk buzu üstünde doğru; ama koyu mavi buz bloğunun
        /// üstünde yabancı bir etiket gibi duruyordu.
        /// </summary>
        public enum CounterStyle
        {
            /// <summary>Buz bloğu: açık camgöbeği, koyu mavi konturlu.</summary>
            Ice,
            /// <summary>Donmuş kapı: krem, sıcak kahve konturlu.</summary>
            Frost,
            /// <summary>Perde ve üreteç: altın rozet.</summary>
            Gold,
            /// <summary>
            /// Koyu rozetin İÇİNDE duran beyaz rakam (perde sayacı).
            ///
            /// Referansta perdenin sayacı yüzeye yazılmış bir rakam değil,
            /// altın çerçeveli koyu bir rozetin içinde duruyor ve rakam
            /// bembeyaz — ölçüm `(255, 251, 255)`. Koyu zeminin üstünde
            /// beyaz, mor panelin üstünde altından çok daha okunur.
            /// </summary>
            Badge,
        }

        /// <summary>
        /// Zemine yatık, yukarı bakan sayaç yazısı (buz/perde/kapı sayaçları).
        ///
        /// Ana nesnenin ÇOCUĞU yapılmaz: blokların eşit olmayan ölçeği yazıyı
        /// da eziyordu; bunun yerine dünya konumuna bağımsız yerleştirilir.
        ///
        /// DERS (TextMesh'in konturu yoktur). Sayaçlar Unity'nin eski
        /// <c>TextMesh</c>'iyle, gömülü LegacyRuntime (düz Arial) fontuyla
        /// çiziliyordu. Referanstaki rakamlar ise hem YUVARLAK ve şişman bir
        /// fontta hem de kalın bir KONTURLA çevrili — okunurluğu veren şey
        /// büyük ölçüde o kontur, çünkü rakam kendi zemininin açık tonu ve
        /// kontursuz kalınca eriyip gidiyor. TextMesh kontur çizemez; TMP
        /// çizer ve zaten oyunun geri kalanının fontu (Baloo2) onda.
        /// </summary>
        public static TMPro.TextMeshPro CreateCounter(
            Transform parent, Vector3 worldPos, int value,
            CounterStyle style = CounterStyle.Frost)
        {
            var go = new GameObject("Counter");
            go.transform.SetParent(parent, worldPositionStays: false);

            // YAZI KAMERAYA SIRTINI DÖNER — ve bu DOĞRUDUR.
            //
            // Sayacın ekranda düzgün okunması için iki şey aynı anda tutmalı:
            // yazının üstü ekranda yukarı (dünyada +Z, ölçüldü) ve yazının
            // sağı ekranda sağa (dünyada +X). `LookRotation` ile bunu denedim;
            // ikisi asla birlikte tutmadı. Sebep basit: `up` ipucunu +Z
            // yapınca sağ -X'e düşüyor (yazı aynalanıyor), -Z yapınca yazı
            // ters dönüyor. İki koşulu birden sağlayan tek çerçevede yazının
            // ÖNÜ aşağı bakıyor — yani eski `Euler(90,0,0)` en baştan doğruydu.
            //
            // DERS (bir hatayı iki kez "düzeltmek"): Rakamlar hiç
            // görünmeyince açıyı suçladım ve çevirdim; sonra ters çıkınca bir
            // daha çevirdim. Oysa açı hiç bozuk değildi — görünmemenin sebebi
            // yüksekliğin geometrinin içinde kalmasıydı. Ekrandaki tek bir
            // belirtiye bakıp ilk akla gelen sebebi düzeltmek, doğru olanı
            // bozmanın en kolay yolu. Yön sorusu ancak ÖLÇÜLDÜĞÜNDE
            // (ekran DX/DY işaretleri) kapandı.
            //
            // Arkadan bakılan yazının çizilmesi için yüz ayıklaması materyalde
            // kapatılıyor (bkz. CounterMaterial) — eski `TextMesh` bunu
            // gömülü gölgecisinden hazır alıyordu.
            go.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(90f, 0f, 0f));
            go.transform.localScale = Vector3.one;

            var text = go.AddComponent<TMPro.TextMeshPro>();
            text.rectTransform.sizeDelta = new Vector2(2f, 1.5f);
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.fontStyle = TMPro.FontStyles.Bold;
            text.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            text.overflowMode = TMPro.TextOverflowModes.Overflow;

            // DERS (TMP'nin fontSize'ı dünya birimi DEĞİL): Hücre 1 birim
            // olduğu için önce `fontSize = 0.72f` yazdım — "hücrenin yarısı
            // kadar" diye. Ölçüm başka söyledi: rakamın çizici sınırı
            // 0.04 x 0.06 birim çıktı, yani hücrenin %5'i. Ekranda hiçbir şey
            // görünmüyordu ama sahnede sekiz sayaç, doğru materyal ve açık
            // çizicilerle duruyordu. TMP'de fontSize font varlığının örnekleme
            // punto boyuna göredir; dünya ölçeğine çevrim yaklaşık 0.083
            // birim/punto. Yarım hücrelik rakam için gereken değer ~6.
            const float UnitsPerPoint = 0.083f;   // ölçümden: 0.06 birim / 0.72 punto
            const float TargetHeight = 0.50f;     // hücrenin yarısı
            text.fontSize = TargetHeight / UnitsPerPoint;
            text.text = value.ToString();
            text.fontSharedMaterial = CounterMaterial(style);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return text;
        }

        static readonly System.Collections.Generic.Dictionary<CounterStyle, Material>
            _counterMaterials = new System.Collections.Generic.Dictionary<CounterStyle, Material>();

        /// <summary>
        /// Sayaç stiline karşılık gelen PAYLAŞILAN TMP materyali.
        ///
        /// DERS (TMP'de kontur bir ANAHTAR ister): <c>outlineWidth</c>'i
        /// yazmak tek başına yetmez; materyalde <c>OUTLINE_ON</c> anahtarı
        /// açık değilse gölgeci kontur dalını hiç derlemez ve verdiğin
        /// genişlik sessizce yok sayılır. (Aynı tuzağa başlık gölgesinde
        /// <c>UNDERLAY_ON</c> ile de düşülmüştü.)
        ///
        /// DERS (yazı da derinlik testine girer): Sayaç buz kalıbının ÜSTÜNDE
        /// duruyor ama varsayılan sırada çizilince yüzeyle çakışıp soluk
        /// kalıyordu. Sıra yukarı çekiliyor — sayaç bir arayüz öğesi gibi
        /// davranmalı, sahnenin bir parçası gibi değil.
        /// </summary>
        static Material CounterMaterial(CounterStyle style)
        {
            if (_counterMaterials.TryGetValue(style, out var cached) && cached != null)
                return cached;

            Color fill, outline;
            switch (style)
            {
                case CounterStyle.Ice:      // ölçüm: (111,226,255) / (0,67,176)
                    fill = new Color(0.435f, 0.886f, 1f);
                    outline = new Color(0f, 0.263f, 0.690f);
                    break;
                case CounterStyle.Gold:
                    fill = new Color(1f, 0.902f, 0.549f);
                    outline = new Color(0.525f, 0.322f, 0.094f);
                    break;
                case CounterStyle.Badge:    // ölçüm: (255,251,255) koyu rozette
                    fill = Color.white;
                    outline = new Color(0.310f, 0.086f, 0.020f);
                    break;
                default:                    // ölçüm: (246,231,217) / (134,82,24)
                    fill = new Color(0.965f, 0.906f, 0.851f);
                    outline = new Color(0.525f, 0.322f, 0.094f);
                    break;
            }

            var source = TMPro.TMP_Settings.defaultFontAsset;
            if (source == null || source.material == null) return null;

            var mat = new Material(source.material)
            {
                name = "Counter_" + style,
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = 4000,
            };
            mat.SetColor(TMPro.ShaderUtilities.ID_FaceColor, fill);
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetColor(TMPro.ShaderUtilities.ID_OutlineColor, outline);
            mat.SetFloat(TMPro.ShaderUtilities.ID_OutlineWidth, 0.22f);

            // Sayaç buz kalıbının yüzeyinin bir tık üstünde duruyor; sıra
            // numarası NE ZAMAN çizileceğini söyler, derinlik testi ise
            // ÇİZİLİP çizilmeyeceğini. İkisi ayrı ayarlar.
            //
            // DERS (özellik yoksa yazmak SESSİZCE hiçbir şey yapmaz):
            // Önce koşulsuz `SetFloat("_ZTestMode", Always)` yazdım. Ölçtüğümde
            // materyalde o özelliğin HİÇ OLMADIĞI çıktı — `_ZTestMode`
            // TMP'nin masaüstü gölgecisinde var, projenin kullandığı
            // "Mobile/Distance Field" sürümünde yok. Unity böyle bir yazmayı
            // hataya çevirmez, yutar; ben de bir sorunu "çözdüm" sanıp
            // gerçek sebebi (yanlış dönme açısı) aramayı bıraktım.
            if (mat.HasProperty("_ZTestMode"))
                mat.SetFloat("_ZTestMode",
                    (float)UnityEngine.Rendering.CompareFunction.Always);

            // YÜZ AYIKLAMASI KAPALI: sayaç tahtaya yatık duruyor ve doğru
            // okunabilmesi için kameraya sırtını dönüyor (bkz. CreateCounter).
            // Eski `TextMesh` bunu Unity'nin gömülü font gölgecisinden hazır
            // alıyordu; TMP'de açıkça istemek gerekiyor.
            if (mat.HasProperty("_CullMode"))
                mat.SetFloat("_CullMode", (float)UnityEngine.Rendering.CullMode.Off);

            _counterMaterials[style] = mat;
            return mat;
        }

        static Material MakeLit(string name, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", color);
            return mat;
        }
    }
}
