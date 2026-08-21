using BlockOut.Core;
using BlockOut.Runtime.Board;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.View;
using UnityEngine;

namespace BlockOut.Runtime.FX
{
    /// <summary>
    /// Tahta olaylarını parçacık patlamalarına çevirir: tuğla kırıntısı, buz kırılması.
    ///
    /// DERS (neden ayrı bir havuz YOK?): Object pooling'in amacı sık yaratılıp
    /// yok edilen nesnelerin GC baskısını kaldırmaktır. ParticleSystem'in kendisi
    /// ZATEN bir havuzdur — sabit bir parçacık dizisi tutar ve tekrar kullanır.
    /// Bu yüzden her patlama için ayrı sistem yaratmak yerine TEK sistem kurup
    /// <see cref="ParticleSystem.EmitParams"/> ile her patlamanın rengini ayrı
    /// veriyoruz. Doğru araç zaten havuzluysa üstüne havuz yazmak israftır.
    ///
    /// Servis <see cref="BoardEvents"/>'e abonedir; oyun mantığı FX'in varlığından
    /// habersizdir (M1'de kurduğumuz gevşek bağlılığın karşılığı).
    /// </summary>
    public sealed class FXService : MonoBehaviour
    {
        ParticleSystem _crumbs;
        ParticleSystem _gateCubes;
        ParticleSystem _shafts;
        ParticleSystem _sparks;
        ParticleSystem _mouthGlow;
        BoardEvents _events;
        BoardSpace _space;
        ColorPaletteSO _palette;

        public static FXService Create(Transform parent, ColorPaletteSO palette)
        {
            var go = new GameObject("FX");
            go.transform.SetParent(parent, worldPositionStays: false);
            var service = go.AddComponent<FXService>();
            service._palette = palette;
            service._crumbs = service.BuildCrumbSystem(go.transform);
            service._gateCubes = service.BuildGateCubeSystem(go.transform);
            service._shafts = service.BuildShaftSystem(go.transform);
            service._sparks = service.BuildSparkSystem(go.transform);
            service._mouthGlow = service.BuildMouthGlowSystem(go.transform);
            return service;
        }

        /// <summary>Bölüm yeniden kurulduğunda yeni olay merkezine bağlanır.</summary>
        public void Bind(BoardEvents events, BoardSpace space)
        {
            Unbind();
            _events = events;
            _space = space;

            _events.BlockAbsorbed += OnBlockAbsorbed;
            _events.LayerPeeled += OnLayerPeeled;
            _events.IceShattered += OnIceShattered;
            _events.GateIceShattered += OnGateIceShattered;

            // HER HAMLEDE ÇATLAMA (5. tur, kullanıcı geri bildirimi).
            //
            // Kullanıcı: "buz kırılıyorken her hamle yaptığımızda buz
            // parçalanma efekti gelsin demiştim, çalışmıyor şu anda."
            //
            // BULUNAN SEBEP: `IceDecremented` olayına yalnız titreşim ve ses
            // bağlıydı; GÖRSEL hiçbir şey yoktu. Parçacıklar sadece buz
            // TAMAMEN kırılınca (`IceShattered`) çıkıyordu. Yani sayaç 5'ten
            // 1'e inerken ekranda hiçbir şey olmuyor, oyuncu ilerlediğini
            // göremiyordu.
            //
            // DERS (bir olayın DUYULMASI görülmesi demek değil): Ses ve
            // titreşim bağlanmış olduğu için olay "yapıldı" sayılmıştı.
            // Sessiz oynayan ya da titreşimi kapalı bir oyuncu için o hamle
            // hiç gerçekleşmemiş gibiydi.
            _events.IceDecremented += OnIceDecremented;
            _events.GateIceDecremented += OnGateIceDecremented;
            _events.CurtainOpened += OnCurtainOpened;
        }

        void Unbind()
        {
            if (_events == null) return;
            _events.BlockAbsorbed -= OnBlockAbsorbed;
            _events.LayerPeeled -= OnLayerPeeled;
            _events.IceShattered -= OnIceShattered;
            _events.GateIceShattered -= OnGateIceShattered;
            _events.IceDecremented -= OnIceDecremented;
            _events.GateIceDecremented -= OnGateIceDecremented;
            _events.CurtainOpened -= OnCurtainOpened;
            _events = null;
        }

        void OnDestroy() => Unbind();

        // ---------------- olaylar ----------------

        // ================= KAPIDAN GECISIN KOREOGRAFISI =================
        //
        // Kullanici: "blok kapidan giriyor, geriye gitme animasyonu basliyor,
        // ayni anda kapidan iceri isik yansimasi da oluyor ve bununla beraber
        // senkron -- ne kadar iceri girdikce arkasinda donusturdugu kup
        // parcaciklari ve isik suzmeleri geliyor. Daha girmeden isik
        // parcalari da geliyor."
        //
        // OLCUM (`Levels`, 59,47 fps; 4. bolumun mavi 2x2 blogu guney
        // kapisindan gecerken -- temas kare 1732, bitis kare 1746):
        //
        //   kare 1721  ilk sonuk isin        temas - 0,185 sn
        //   kare 1732  TEMAS                 0
        //   kare 1736  ilk kup               temas + 0,067 sn
        //   kare 1746  blok bitti            temas + 0,235 sn
        //   kare 1756  kup emisyonu bitti    temas + 0,40 sn
        //   kare 1768  kupler durdu          temas + 0,60 sn
        //   kare 1794  kupler sondu          temas + 1,04 sn
        //
        // DERS (tek patlama bir OLAYI anlatamaz): Eski hal butun kirintilari
        // TEK karede saciyordu. O yuzden ekranda "blok kapiya girdi" degil
        // "blok patladi" gorunuyordu: parcalar blok daha yari yoldayken
        // coktan dagilmis oluyordu. Referansta emisyon blogun ilerlemesiyle
        // SURUYOR -- parcacigin sayisi degil, ZAMANA YAYILMASI olayi
        // anlatan sey.
        void OnBlockAbsorbed(BlockModel block, GateModel gate)
        {
            int depth = gate.EdgeHorizontal ? block.H : block.W;
            StartCoroutine(AbsorbRoutine(gate, ColorOf(block.CurrentColor),
                                         AbsorbTiming.Duration(depth),
                                         block.Cells.Count));

            // Sarsinti blogun BUYUKLUGUNE bagli: 1x1 bir parcanin emilmesiyle
            // 2x4'luk bir kutlenin emilmesi ayni agirlikta hissedilmemeli.
            GameKit.FX.CameraShake.Add(0.10f + block.Cells.Count * 0.012f);
        }

        System.Collections.IEnumerator AbsorbRoutine(
            GateModel gate, Color color, float duration, int cells)
        {
            // 1) BLOK DAHA GIRMEDEN: kapinin agzinda birkac sonuk isin.
            //    Referansta blok kapiya 1,5 hucre uzaktayken ilk isinlar
            //    goruluyor; bizde blok komsu hucreye oturdugu anda emilim
            //    basladigi icin ayni bekleyis one alinmis bir on-yukleme
            //    olarak yasiyor.
            for (float t = 0f; t < AbsorbTiming.PreRoll; t += Time.deltaTime)
            {
                EmitShafts(gate, color, 2, 0.45f);
                yield return null;
            }

            // 2) BLOK ICERI GIRIYOR: isin ve kup emisyonu ilerlemeyle suruyor.
            //
            // Kapinin agzindan tahtaya vuran hale TEK parcacik: omru emilimin
            // suresi kadar ve zarfini (dogus - tutus - sonus) sistemin
            // `colorOverLifetime` egrisi veriyor. Her karede yeniden emitmek
            // ust uste binen katkili kopyalar yuzunden titriyordu.
            EmitMouthGlow(gate, color, duration + AbsorbTiming.MouthFade);

            //
            // Kup sayisi OLCULDU: kare 1758'de kapinin disinda ayni anda ~60
            // kup var ve blok 4 hucreydi -> hucre basina ~14. Eski sabit
            // "12 + hucre x 3" (4 hucre icin 24) referansin yarisiydi ve
            // parcalar da iri oldugu icin kume "birkac iri kalinti" gibi
            // duruyordu, "blok kuplere donustu" gibi degil.
            int cubeTarget = Mathf.Max(10, cells * AbsorbTiming.CubesPerCell);
            float cubeSpan = duration + AbsorbTiming.CubeTail - AbsorbTiming.CubeDelay;
            float emitted = 0f;

            float elapsed = 0f;
            float total = duration + AbsorbTiming.CubeTail;
            while (elapsed < total)
            {
                elapsed += Time.deltaTime;

                // Isinlar yalniz blok iceri girerken; blok bitince tek bir
                // patlamayla kapaniyorlar (asagida).
                if (elapsed <= duration)
                    EmitShafts(gate, color, Random.value < 0.6f ? 6 : 5, 1f);

                if (elapsed >= AbsorbTiming.CubeDelay)
                {
                    // Karede kac kup: hedefi sureye BOLUP kesirli borcu
                    // biriktiriyoruz -- kare hizi dustugunde toplam sayi
                    // korunsun diye.
                    emitted += cubeTarget * Time.deltaTime / Mathf.Max(0.01f, cubeSpan);
                    int now = Mathf.FloorToInt(emitted);
                    if (now > 0)
                    {
                        EmitGateCubes(gate, color, now);
                        EmitSparks(gate, color, now >= 2 ? 1 : 0);
                        emitted -= now;
                    }
                }
                yield return null;
            }

            // 3) BITIS VURUSU: blok tamamen gectigi karede isinlar bir kez
            //    genis bir yelpaze halinde patliyor (referans kare 1747).
            EmitShafts(gate, color, 22, 1.35f);
            EmitSparks(gate, color, 8);
        }

        void OnLayerPeeled(BlockModel block, GateModel gate)
        {
            // Soyulan katmanın rengi zaten listeden çıktı; kapının rengi doğru olan.
            // Soyulma da kapıda olur — kırıntı orada doğmalı, blok yerinde kalır.
            // Soyulma emilimin KUCUK kardesi: ayni kupler, ayni isinlar --
            // ama tek atisluk. Ayri bir gorunum kullanmak, oyuncuya iki
            // olayin akrabaligini kaybettirirdi.
            Color peeled = ColorOf(gate.ActiveColor);
            EmitGateCubes(gate, peeled, 14);
            EmitShafts(gate, peeled, 8, 1f);
            EmitSparks(gate, peeled, 4);
        }

        void OnIceShattered(BlockModel block)
        {
            Vector3 at = _space.RectCenterToWorld(
                block.Position, block.W, block.H, BrickHeightHalf);
            IceBurst(at, block.Cells.Count);
            GameKit.FX.CameraShake.Add(0.30f);      // buz kırılması sert bir an
        }

        /// <summary>
        /// Buz ÇATLIYOR (kırılmıyor): sayaç bir azaldı.
        ///
        /// Kırılmadan farkı ÖLÇEK: burada beyaz toz yok, yalnız birkaç
        /// camgöbeği kırıntı ve hafif bir sarsıntı. Her hamlede tam
        /// parçalanma efekti oynatmak, asıl kırılma anını değersizleştirirdi
        /// — büyük an, küçük anlardan AYRIŞMALI.
        /// </summary>
        /// <summary>
        /// ÇATLAMA HER HÜCREDE AYRI OLUR (7. tur düzeltmesi).
        ///
        /// Kullanıcı: "buzlu levellerde kapıdan blok girince buz blokları
        /// parçalanıyor ama yeterince belirgin değil, anlaşılmıyor;
        /// parçalanma efekti gözüksün HER BUZ PARÇASI İÇİN."
        ///
        /// BULUNAN SEBEP: Kırıntılar bloğun TEK bir noktasından —
        /// `RectCenterToWorld` ile hesaplanan merkezinden — çıkıyordu. 3×2'lik
        /// bir buz bloğunda bu, altı hücrelik bir yüzeyin ortasında beliren
        /// küçük bir tutam demek: buzun büyük kısmında hiçbir şey olmuyor ve
        /// oyuncu "bir şey oldu mu?" diye bakıyor. Üstelik merkez, L şeklindeki
        /// bloklarda BOŞ bir hücreye bile düşebiliyor.
        ///
        /// DERS (bir olayın ölçeği, olayın nesnesi kadar olmalı): Aynı sayıda
        /// parçacığı tek noktadan çıkarmak ile yüzeye yaymak, ekranda
        /// bambaşka iki olay. İlki "ufak bir kıvılcım", ikincisi "bu blok
        /// çatladı". Toplam parçacık sayısı hemen hemen aynı kaldı; değişen
        /// şey nereden çıktıkları.
        /// </summary>
        void OnIceDecremented(BlockModel block)
        {
            foreach (var cell in block.Cells)
            {
                // `Cells` sınırlayıcı kutuya göre YERELDİR; dünya konumu için
                // bloğun konumu ekleniyor ve hücrenin ortasına çekiliyor.
                Vector3 at = _space.CornerToWorld(
                    block.Position.x + cell.x + 0.5f,
                    block.Position.y + cell.y + 0.5f,
                    BrickHeightHalf);
                IceCrack(at, 1);
            }
            GameKit.FX.CameraShake.Add(0.10f);
        }

        void OnGateIceDecremented(GateModel gate)
        {
            // Kapı buzu da hücre hücre çatlar: 3 hücrelik bir kapıda tek
            // tutam yerine üç ayrı çatlama.
            int cells = Mathf.Max(1, gate.Length);
            float spanStart = gate.SpanMin;
            for (int i = 0; i < cells; i++)
            {
                float along = spanStart + i + 0.5f;
                Vector3 at = gate.EdgeHorizontal
                    ? _space.CornerToWorld(along, gate.EdgeCoord, 0.2f)
                    : _space.CornerToWorld(gate.EdgeCoord, along, 0.2f);
                IceCrack(at, 1);
            }
            GameKit.FX.CameraShake.Add(0.10f);
        }

        /// <summary>
        /// Çatlama kırıntısı: kırılmanın küçük kardeşi — ama GÖRÜNÜR olanı.
        ///
        /// Boyut 0.20 → 0.30 ve ömür 0.42 → 0.55 (7. tur). Hücre başına
        /// çağrıldığı için sayı 5+2·hücre yerine sabit 7'ye indi; 3×2'lik bir
        /// blokta toplam 11 yerine 42 kırıntı çıkıyor ve bunlar yüzeye
        /// yayılmış durumda. Beyaz kıvılcım da hücre başına iki tane.
        /// </summary>
        void IceCrack(Vector3 position, int cells)
        {
            int shards = 5 + cells * 2;
            var chips = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
                startColor = new Color(0.78f, 0.94f, 1f, 1f),
                startLifetime = 0.55f,
                startSize = 0.30f
            };
            _crumbs.Emit(chips, shards);

            // Beyaz kıvılcım: "çatladı" vurgusu. Kırıntılardan KISA ömürlü,
            // yani göz önce beyazı görüp sonra camgöbeği tozu okuyor.
            var spark = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
                startColor = Color.white,
                startLifetime = 0.22f,
                startSize = 0.18f
            };
            _crumbs.Emit(spark, 2);
        }

        Vector3 GateWorldPoint(GateModel gate)
        {
            float spanCenter = (gate.SpanMin + gate.SpanMax) * 0.5f;
            return gate.EdgeHorizontal
                ? _space.CornerToWorld(spanCenter, gate.EdgeCoord, 0.2f)
                : _space.CornerToWorld(gate.EdgeCoord, spanCenter, 0.2f);
        }

        void OnGateIceShattered(GateModel gate)
        {
            float spanCenter = (gate.SpanMin + gate.SpanMax) * 0.5f;
            Vector3 at = gate.EdgeHorizontal
                ? _space.CornerToWorld(spanCenter, gate.EdgeCoord, 0.2f)
                : _space.CornerToWorld(gate.EdgeCoord, spanCenter, 0.2f);
            IceBurst(at, Mathf.Max(1, gate.Length));
            GameKit.FX.CameraShake.Add(0.34f);
        }

        /// <summary>
        /// Buz parçalanması — İKİ KATMANLI (4. tur, G25/I36).
        ///
        /// Kullanıcı: "Buzlu kapı parçalanma efekti — aktif kapıdan blok
        /// sokunca oluşan parçalanma efekti (Level 6'da örneği var) birebir
        /// yapılacak." ve Level 20 için "Buz parçalanma animasyonu da aynı
        /// şekilde."
        ///
        /// Tek renkli 18 parçacık "bir şey mavi mavi dağıldı" diyordu.
        /// Referansta kırılma iki farklı şey aynı anda gösteriyor:
        ///   1) BEYAZ, hızlı ve KISA ÖMÜRLÜ bir toz bulutu — kırılmanın ANI,
        ///   2) CAMGÖBEĞİ, iri ve yavaş düşen parçalar — kırılan MADDE.
        ///
        /// DERS (bir olayı iki ÖLÇEKTE anlatmak): Aynı sayıda parçacığı tek
        /// boyutta saçmak "duman" üretir. Küçük ve kısa ömürlü beyaz katman
        /// gözü olayın merkezine çekiyor, iri ve uzun ömürlü camgöbeği katman
        /// ise neyin kırıldığını söylüyor. İki emisyon, tek sistem — maliyet
        /// aynı.
        ///
        /// DERS (`EmitParams`ta HIZ yoktur): İlk yazımda iki katmana ayrı
        /// `startSpeed` verilecekti; o alan `EmitParams` içinde yok (yalnız
        /// `velocity` var ve o TEK bir vektör, yani bütün parçacıklar aynı
        /// yöne giderdi). Hız sistemin kendi 2,2-4,2 aralığından ve küresel
        /// şeklinden geliyor; ayrım boyut ve ömürle kuruluyor.
        ///
        /// Parça sayısı kırılan şeyin BÜYÜKLÜĞÜNE bağlı: 1 hücrelik bir buz
        /// ile 6 hücrelik bir kalıp aynı miktarda cam üretemez.
        /// </summary>
        void IceBurst(Vector3 position, int cells)
        {
            int shards = 18 + cells * 6;

            // 1) Beyaz toz: hızlı, küçük, kısa ömürlü.
            var flash = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
                startColor = new Color(1f, 1f, 1f, 1f),
                startLifetime = 0.30f,
                startSize = 0.16f
            };
            _crumbs.Emit(flash, shards);

            // 2) Camgöbeği kristaller: iri, yavaş, düşerek sönen.
            var glass = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
                startColor = new Color(0.62f, 0.90f, 1f, 1f),
                startLifetime = 0.85f,
                startSize = 0.36f
            };
            _crumbs.Emit(glass, shards);
        }

        void OnCurtainOpened(CurtainModel curtain)
        {
            Vector3 at = _space.RectCenterToWorld(
                new Vector2(curtain.X, curtain.Y), curtain.W, curtain.H, 0.3f);
            Burst(at, new Color(1f, 0.85f, 0.35f), 26);
            GameKit.FX.CameraShake.Add(0.22f);
        }

        // ---------------- parçacıklar ----------------

        const float BrickHeightHalf = 0.25f;

        void Burst(Vector3 position, Color color, int count)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
                startColor = color
            };
            _crumbs.Emit(emit, count);
        }

        /// <summary>
        /// Kapıdan ÇIKIŞ YÖNÜNE saçılan kırıntı konisi.
        ///
        /// Parçacıklar kapı AÇIKLIĞI boyunca doğuyor (tek bir noktadan değil):
        /// 3 hücrelik bir kapıdan çıkan blok, kapının tamamından toz kaldırır.
        ///
        /// DERS (tek `Emit` çağrısı tek hız verir): `EmitParams.velocity` bütün
        /// partiye uygulanır, yani tek çağrıyla koni yapılamaz — hepsi aynı
        /// yöne fırlar ve "havai fişek" değil "sprey" olur. Parçacıklar tek tek
        /// yayılıyor; `EmitParams` bir struct olduğu için döngü çöp üretmiyor.
        /// </summary>
        /// <summary>
        /// KAPI KUPLERI: blok kapidan gecerken tahtanin DISINA sacilan
        /// parcalar.
        ///
        /// OLCUM (`Levels`, guney kapisi, kare 1736 -> 1794):
        ///   - Kup kenari 8-23 piksel / 74 piksellik hucre -> 0,11-0,31 hucre.
        ///   - Bulutun on ucu 14 piksel/kare ile basliyor (11,3 hucre/sn) ve
        ///     0,4 saniyede DURUYOR; toplam yol 182 piksel = 2,46 hucre.
        ///     v0/lambda = 2,46 -> surtunme katsayisi lambda ~ 4,6.
        ///   - Yanlara acilma 15 piksel: koni neredeyse yok, sacilim DUZ
        ///     disari.
        ///   - Durduktan sonra yerinde SONUYORLAR (bulutun agirlik merkezi
        ///     1760-1790 arasinda hic kipirdamiyor, yalniz piksel sayisi
        ///     dusuyor).
        ///
        /// DERS (yercekimi yerine SURTUNME): Eski halde parcalar yukari
        /// firlayip yercekimiyle dusuyordu. Referansta kuzey kapisinda da
        /// guney kapisinda da parcalar kapinin baktigi yone gidiyor ve orada
        /// duruyor -- yani hareketi belirleyen dunya yercekimi degil, kapinin
        /// yonu ve bir sonum. Yercekimi kullanmak, ayni efektin ust kapida
        /// tahtaya geri dokulmesi demekti.
        /// </summary>
        void EmitGateCubes(GateModel gate, Color color, int count)
        {
            ResolveGateFrame(gate, out Vector3 outward, out Vector3 sideways,
                             out float spanCenter, out float half);

            for (int i = 0; i < count; i++)
            {
                float spanPos = spanCenter + Random.Range(-half, half);
                float edgePos = gate.EdgeCoord + gate.OutwardSign * Random.Range(0.30f, 0.60f);

                Vector3 at = gate.EdgeHorizontal
                    ? _space.CornerToWorld(spanPos, edgePos, Random.Range(0.15f, 0.65f))
                    : _space.CornerToWorld(edgePos, spanPos, Random.Range(0.15f, 0.65f));

                Vector3 velocity =
                    outward * Random.Range(5.0f, 11.0f) +
                    sideways * Random.Range(-0.8f, 0.8f) +
                    Vector3.up * Random.Range(-0.4f, 0.6f);

                _gateCubes.Emit(new ParticleSystem.EmitParams
                {
                    position = at,
                    velocity = velocity,
                    applyShapeToPosition = false,
                    startColor = color,
                    startSize = Random.Range(0.08f, 0.20f),
                    startLifetime = Random.Range(0.85f, 1.15f)
                }, 1);
            }
        }

        /// <summary>
        /// ISIK HUZMELERI: kapinin ic cizgisinden TAHTANIN ICINE dogru acilan
        /// ince, sivri isiklar.
        ///
        /// YON OLCUMLE BULUNDU: Guney kapisinda isinlar ekranda YUKARI,
        /// kuzey kapisinda (4. bolumun turuncu kapisi, kare 1648) ASAGI
        /// gidiyor. Yani ikisinde de tahtanin ICINE -- kuplerin tam tersi
        /// yone. Isik blogun yutuldugu yerden iceri siziyor, disari degil.
        ///
        /// RENK DE OLCULDU: Mavi blokta isinlar beyaz-camgobegi, turuncu
        /// blokta turuncu goruluyor. Ikisi de ayni sey: blogun rengi KATKILI
        /// harmanlaninca mavi doyup beyaza kaciyor, turuncu turuncu kaliyor.
        /// Beyaz sabit kullanmak turuncu kapida yanlis olurdu.
        /// </summary>
        void EmitShafts(GateModel gate, Color color, int count, float scale)
        {
            if (count <= 0) return;
            ResolveGateFrame(gate, out Vector3 outward, out Vector3 sideways,
                             out float spanCenter, out float half);

            Vector3 inward = -outward;

            // ALFA 1 DEGIL: katkili harmanlamada tam alfa, dokunun yumusak
            // kenarini da doyuruyor ve igne yerine DUZ BEYAZ CUBUK cikiyor
            // (olcum: ekranda 10 piksel genisliginde, kenari 1 piksel keskin
            // beyaz seritler; referansta cekirdek 2-3 piksel ve cevresi
            // erimis). Tepe parlakligi dusunce dokunun profili gorunur hale
            // geliyor.
            Color glow = new Color(
                Mathf.Min(1f, color.r * 1.45f + 0.25f),
                Mathf.Min(1f, color.g * 1.45f + 0.25f),
                Mathf.Min(1f, color.b * 1.45f + 0.25f), 0.85f);

            for (int i = 0; i < count; i++)
            {
                float spanPos = spanCenter + Random.Range(-half, half) * 0.92f;

                // DOGUM NOKTASI TAM CIZGIDE DEGIL, BIRAZ ICERIDE: gerdirilmis
                // parcacik konumunun ETRAFINDA ciziliyor, yani yarisi geriye
                // dogru tasiyor. Tam cizgide dogan bir isinin yarisi kapinin
                // uzerine biniyor ve yelpaze tahtaya degil bara ait
                // gorunuyordu.
                float birth = gate.EdgeCoord - gate.OutwardSign * 0.26f;
                Vector3 at = gate.EdgeHorizontal
                    ? _space.CornerToWorld(spanPos, birth, 0.42f)
                    : _space.CornerToWorld(birth, spanPos, 0.42f);

                // Yelpaze +-55 derece: kare 1747'de en distaki isinlar dikeyle
                // bu aciyi yapiyor.
                float angle = Random.Range(-55f, 55f) * Mathf.Deg2Rad;
                Vector3 dir = inward * Mathf.Cos(angle) + sideways * Mathf.Sin(angle);

                // ISIN KAPININ AGZINDAN AYRILMAZ.
                //
                // Ilk hal 2-5 hucre/sn hizla 0,2 saniye yasiyordu; parcacik
                // omrunun ortasinda hucrenin ortasina varmis oluyor ve
                // ekranda kapiyla iliskisi olmayan, havada duran kibrit
                // cöpleri gibi duruyordu. Referansta HER isin temas
                // cizgisine degiyor: boyunu yol degil, gerdirme veriyor.
                _shafts.Emit(new ParticleSystem.EmitParams
                {
                    position = at,
                    velocity = dir * Random.Range(1.4f, 3.2f) * scale,
                    applyShapeToPosition = false,
                    startColor = glow,
                    startSize = Random.Range(0.026f, 0.046f),
                    startLifetime = Random.Range(0.07f, 0.13f)
                }, 1);
            }
        }

        /// <summary>
        /// KAPIDAN TAHTAYA VURAN ISIK.
        ///
        /// Kullanici: "blok kapidan giriyor ... ayni anda kapidan iceri isik
        /// yansimasi da oluyor."
        ///
        /// Referansta (kare 1648, turuncu ust kapi) temas cizgisinden
        /// tahtanin icine dogru yumusak, kapi renginde bir hale yayiliyor ve
        /// isinlar o halenin icinden cikiyor. Bizde yalniz temas cizgisindeki
        /// ince serit vardi: cizgi "iki yuzey degdi" der, hale "oradan isik
        /// geliyor" der.
        ///
        /// DERS (isigin kendisi ile isigin cizgisi ayri seylerdir): Serit
        /// kapinin agzini isaretliyor ama hicbir yeri AYDINLATMIYORDU; o
        /// yuzden emilim, karanlikta acilip kapanan bir kapak gibi
        /// duruyordu.
        ///
        /// DERS (calisan yolu birak, benzerini icat etme): Hale once kapinin
        /// altinda duran yatik bir quad olarak kuruldu -- ayni shader, ayni
        /// harmanlama ayarlari (`_SrcBlend`=SrcAlpha, `_DstBlend`=One,
        /// `_ZWrite`=0, kuyruk 3100, hepsi calisma aninda dogrulandi) ama
        /// ekranda kapinin yaninda SIYAH bir kare ve ortasinda parlak bir top
        /// ciktu: `MeshRenderer` ile cizildiginde URP'nin parcacik shader'i
        /// bu ayarlari onurlandirmiyor, yuzeyi OPAK basiyor. Ayni materyal
        /// bir `ParticleSystemRenderer` altinda kusursuz calisiyor (isik
        /// huzmeleri). Materyalin ozelliklerini dogru yazmis olmak, onun
        /// dogru cizilecegi anlamina gelmiyor.
        /// </summary>
        void EmitMouthGlow(GateModel gate, Color color, float life)
        {
            ResolveGateFrame(gate, out Vector3 outward, out Vector3 sideways,
                             out float spanCenter, out float half);

            // Hale yuvarlak; genis bir kapida tek top acikligin ucunu bos
            // birakiyor. Hucre basina bir top, acikligi bastan sona kapliyor.
            int lamps = Mathf.Max(1, gate.Length);
            Color glow = new Color(
                Mathf.Min(1f, color.r * 1.15f + 0.12f),
                Mathf.Min(1f, color.g * 1.15f + 0.12f),
                Mathf.Min(1f, color.b * 1.15f + 0.12f), 0.5f);

            for (int i = 0; i < lamps; i++)
            {
                float along = gate.SpanMin + i + 0.5f;
                // Merkezi temas cizgisinin biraz ICERISI: yumusak leke
                // simetrik oldugu icin cizgiye oturtmak yarisini duvarin
                // arkasina atardi.
                float edgePos = gate.EdgeCoord - gate.OutwardSign * 0.30f;

                Vector3 at = gate.EdgeHorizontal
                    ? _space.CornerToWorld(along, edgePos, 0.9f)
                    : _space.CornerToWorld(edgePos, along, 0.9f);

                _mouthGlow.Emit(new ParticleSystem.EmitParams
                {
                    position = at,
                    velocity = Vector3.zero,
                    applyShapeToPosition = false,
                    startColor = glow,
                    startSize = 1.3f,
                    startLifetime = life
                }, 1);
            }
        }

        ParticleSystem BuildMouthGlowSystem(Transform parent)
        {
            var go = new GameObject("MouthGlow");
            go.transform.SetParent(parent, worldPositionStays: false);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop();

            var main = system.main;
            main.duration = 2f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.4f;
            main.startSize = 1.3f;
            main.gravityModifier = 0f;
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;

            // ZARF: bir karede yanar (blok degdi), emilim boyunca guclenir,
            // sonra soner. Sureyi omur belirledigi icin egri her blok boyunda
            // ayni bicimi koruyor.
            var colorOverLife = system.colorOverLifetime;
            colorOverLife.enabled = true;
            colorOverLife.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(0.35f, 0f),
                    new GradientAlphaKey(1f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
            });

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GlowParticleMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        /// <summary>Kuplerin arasina serpilen minik beyaz pariltilar.</summary>
        void EmitSparks(GateModel gate, Color color, int count)
        {
            if (count <= 0) return;
            ResolveGateFrame(gate, out Vector3 outward, out Vector3 sideways,
                             out float spanCenter, out float half);

            for (int i = 0; i < count; i++)
            {
                float spanPos = spanCenter + Random.Range(-half, half);
                float edgePos = gate.EdgeCoord + gate.OutwardSign * Random.Range(0.1f, 0.5f);
                Vector3 at = gate.EdgeHorizontal
                    ? _space.CornerToWorld(spanPos, edgePos, Random.Range(0.2f, 0.8f))
                    : _space.CornerToWorld(edgePos, spanPos, Random.Range(0.2f, 0.8f));

                _sparks.Emit(new ParticleSystem.EmitParams
                {
                    position = at,
                    velocity = outward * Random.Range(1.5f, 4.5f)
                               + sideways * Random.Range(-1.2f, 1.2f)
                               + Vector3.up * Random.Range(0f, 1.2f),
                    applyShapeToPosition = false,
                    startColor = Color.Lerp(Color.white, color, 0.25f),
                    startSize = Random.Range(0.04f, 0.08f),
                    startLifetime = Random.Range(0.25f, 0.45f)
                }, 1);
            }
        }

        /// <summary>Kapinin kenar cercevesi: disari yonu, yan yonu, acikligi.</summary>
        void ResolveGateFrame(GateModel gate, out Vector3 outward, out Vector3 sideways,
                              out float spanCenter, out float half)
        {
            spanCenter = (gate.SpanMin + gate.SpanMax) * 0.5f;
            half = (gate.SpanMax - gate.SpanMin) * 0.5f;
            outward = gate.EdgeHorizontal
                ? new Vector3(0f, 0f, -gate.OutwardSign)
                : new Vector3(gate.OutwardSign, 0f, 0f);
            sideways = new Vector3(-outward.z, 0f, outward.x);
        }


        /// <summary>
        /// Tuğla kırıntısı sistemi: küçük küpler, yukarı saçılıp yerçekimiyle düşer.
        /// Tamamen kodla kurulur — prefab asset'i yok, ayarlar tek yerde.
        /// </summary>
        ParticleSystem BuildCrumbSystem(Transform parent)
        {
            var go = new GameObject("Crumbs");
            go.transform.SetParent(parent, worldPositionStays: false);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop();

            var main = system.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.72f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.2f);
            // PARÇA BOYU REFERANSTAN ÖLÇÜLDÜ (21-30 yürüyüşü, 21. bölümde
            // sarı blok emilirken): kapının üstündeki yığında parçaların
            // kenarı 20-35 piksel, hücre ~48 piksel — yani hücrenin
            // %40-70'i. Bizdeki 0.07-0.16 hücrenin %7-16'sıydı: ekranda
            // "toz" görünüyordu, referansta ise KOPMUŞ TUĞLA PARÇALARI var.
            //
            // DERS (kırıntı ile moloz farklı şeyler anlatır): Küçük parçacık
            // "bir şey ufalandı" der, iri parça "bir şey KIRILDI" der.
            // Referansın verdiği his ikincisi; blok kapıdan geçerken
            // paramparça oluyor ve parçalar HUD'a kadar yükseliyor.
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 2.6f;
            main.maxParticles = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.enabled = false; // yalnızca Emit() ile patlatıyoruz

            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.28f;

            var sizeOverLife = system.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(
                1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

            var rotationOverLife = system.rotationOverLifetime;
            rotationOverLife.enabled = true;
            rotationOverLife.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            // Kırıntılar kare değil KÜP: tuğladan kopmuş parça hissi verir.
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = CubeMesh();
            renderer.sharedMaterial = ViewKit.ParticleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return system;
        }

        /// <summary>
        /// KAPI KUPLERININ sistemi. Kirintidan (`_crumbs`) iki yerde ayriliyor:
        /// yercekimi YOK, surtunme VAR.
        ///
        /// DERS (ayni gorunen iki parcacik ayni fizigi paylasmak zorunda
        /// degil): Ilk refleks `_crumbs` sistemini yeniden ayarlamakti; ama
        /// buz kirilmasi gercekten DUSEN bir sey, kapi kupleri ise disari
        /// firlayip duran bir sey. Ayni sisteme iki fizik sigmiyordu ve buz
        /// efektini bozmadan kapiyi duzeltmek mumkun degildi.
        /// </summary>
        ParticleSystem BuildGateCubeSystem(Transform parent)
        {
            var go = new GameObject("GateCubes");
            go.transform.SetParent(parent, worldPositionStays: false);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop();

            var main = system.main;
            main.duration = 2f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 1f;
            // OLCULEN 0,11-0,31 hucre IZDUSUM boyuydu; donen bir kupun
            // ekrandaki kutusu kenarinin 1,4-1,7 katina cikiyor. Kenar
            // dogrudan o araliga yazilinca kupler referansin yarim kat
            // ustune ciktu (kapinin kendisi kadar iri parcalar).
            //
            // DERS (olculen sey IZDUSUM, ayarlanan sey NESNE): Ekrandan
            // olculen bir boyu dogrudan modele yazmak, aradaki izdusum
            // buyutmesini hediye etmek demek.
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0f;                // OLCUM: bulut dusmuyor
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.enabled = false;                 // yalniz Emit() ile

            var shape = system.shape;
            shape.enabled = false;

            // SURTUNME: on uc 11,3 hucre/sn ile cikip 2,46 hucrede duruyor.
            // v0 / lambda = mesafe  ->  lambda = 11,3 / 2,46 = 4,6.
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = new ParticleSystem.MinMaxCurve(100f);   // sinir yok
            limit.dampen = 0f;
            limit.drag = new ParticleSystem.MinMaxCurve(4.6f);
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;

            // Durduktan sonra YERINDE soner: alfanin son %45'te inmesi,
            // olculen "agirlik merkezi kipirdamiyor ama piksel sayisi
            // dusuyor" davranisini veriyor.
            var colorOverLife = system.colorOverLifetime;
            colorOverLife.enabled = true;
            colorOverLife.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
            });

            var sizeOverLife = system.sizeOverLifetime;
            sizeOverLife.enabled = true;
            var shrink = new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.6f, 1f), new Keyframe(1f, 0.55f));
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, shrink);

            var rotationOverLife = system.rotationOverLifetime;
            rotationOverLife.enabled = true;
            rotationOverLife.separateAxes = true;
            rotationOverLife.x = new ParticleSystem.MinMaxCurve(-4f, 4f);
            rotationOverLife.y = new ParticleSystem.MinMaxCurve(-4f, 4f);
            rotationOverLife.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = ShadedCubeMesh();
            renderer.sharedMaterial = ViewKit.ParticleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        /// <summary>
        /// ISIK HUZMELERI: hizina gore UZAYAN, katkili harmanlanan ince
        /// isiklar.
        ///
        /// DERS (yumusak bir nokta, gerilince mizrak olur): Referanstaki
        /// isinlar iki ucu da sivri ince ipliklier. Bunun icin ayri bir
        /// "isin" dokusu cizmeye gerek yok: yumusak kenarli yuvarlak bir leke
        /// `Stretch` kipinde hiz yonunde gerilince tam o silueti veriyor --
        /// ortasi parlak, iki ucu erimis.
        /// </summary>
        ParticleSystem BuildShaftSystem(Transform parent)
        {
            var go = new GameObject("Shafts");
            go.transform.SetParent(parent, worldPositionStays: false);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop();

            var main = system.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.15f;
            main.startSize = 0.06f;
            main.gravityModifier = 0f;
            main.maxParticles = 700;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;

            // Isik once uzuyor sonra sonuyor; sabit boyda bir cizgi "isin"
            // degil "cubuk" gibi duruyor.
            var sizeOverLife = system.sizeOverLifetime;
            sizeOverLife.enabled = true;
            var taper = new AnimationCurve(
                new Keyframe(0f, 0.55f), new Keyframe(0.35f, 1f), new Keyframe(1f, 0f));
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, taper);

            var colorOverLife = system.colorOverLifetime;
            colorOverLife.enabled = true;
            colorOverLife.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(0f, 1f)
                }
            });

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0f;
            // OLCUM (referans kare 1747): isin cekirdegi 2-3 piksel genis,
            // boyu 45 piksele kadar / 74 piksellik hucre -> boy/en orani
            // 15-20. Eskiden 5,5 idi ve isinlar "cubuk" gibi duruyordu.
            renderer.lengthScale = 17f;
            renderer.cameraVelocityScale = 0f;
            renderer.sharedMaterial = GlowParticleMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        /// <summary>Kuplerin arasindaki minik pariltilar -- yuvarlak, katkili.</summary>
        ParticleSystem BuildSparkSystem(Transform parent)
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(parent, worldPositionStays: false);

            var system = go.AddComponent<ParticleSystem>();
            system.Stop();

            var main = system.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.35f;
            main.startSize = 0.06f;
            main.gravityModifier = 0f;
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = system.emission;
            emission.enabled = false;
            var shape = system.shape;
            shape.enabled = false;

            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = new ParticleSystem.MinMaxCurve(100f);
            limit.dampen = 0f;
            limit.drag = new ParticleSystem.MinMaxCurve(4.6f);

            var sizeOverLife = system.sizeOverLifetime;
            sizeOverLife.enabled = true;
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(
                1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GlowParticleMaterial();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        static Material _glowParticle;

        /// <summary>
        /// Katkili parcacik materyali + yumusak yuvarlak leke dokusu.
        ///
        /// DERS (isik EKLER, boyamaz -- ViewKit.Additive'in ayni dersi):
        /// Alfa harmanlanan bir isin sonerken arkasindaki koyu zemini
        /// KARARTIYOR ve gecerken gri bir iz birakiyor. Katkili harmanlamada
        /// sifira giden bir renk hicbir sey eklemiyor; isin iz birakmadan
        /// kayboluyor.
        /// </summary>
        static Material GlowParticleMaterial()
        {
            if (_glowParticle != null && _glowParticle.mainTexture != null)
                return _glowParticle;

            _glowParticle = ViewKit.AdditiveSoft(Color.white);
            _glowParticle.name = "GateGlow_TEMP";
            return _glowParticle;
        }

        static Mesh _shadedCube;

        /// <summary>
        /// GOLGELI KUP AGI: yuzleri kose renkleriyle tonlanmis birim kup.
        ///
        /// DERS (isik parcaciga da lazim, ama isiklandirici sart degil):
        /// Referanstaki kupler duz renk degil -- ust yuzu parlak, yanlari
        /// koyu, "kopmus tugla parcasi" gibi duruyorlar. Parcacigi
        /// isiklandirilmis bir shader'a tasimak (URP/Particles/Lit) bunu
        /// verirdi ama o shader build'de elenebiliyor (bkz. ViewKit'teki ayni
        /// tuzak). Tonu AGA gomunce hem bedava hem de kanitlanmis Unlit
        /// materyaliyle calisiyor: Unity mesh'in kose rengiyle parcacigin
        /// rengini carpiyor.
        /// </summary>
        static Mesh ShadedCubeMesh()
        {
            if (_shadedCube != null) return _shadedCube;

            // Isik yonu tuglalarinkiyle ayni ailede: yukaridan, hafif saga.
            var faces = new[]
            {
                (normal: Vector3.up,      tone: 1.00f),
                (normal: Vector3.down,    tone: 0.42f),
                (normal: Vector3.right,   tone: 0.82f),
                (normal: Vector3.left,    tone: 0.62f),
                (normal: Vector3.forward, tone: 0.72f),
                (normal: Vector3.back,    tone: 0.90f)
            };

            var vertices = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var colors = new System.Collections.Generic.List<Color>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var triangles = new System.Collections.Generic.List<int>();

            foreach (var face in faces)
            {
                Vector3 n = face.normal;
                Vector3 u = Vector3.Cross(n, Mathf.Abs(n.y) > 0.5f ? Vector3.forward : Vector3.up);
                Vector3 v = Vector3.Cross(n, u);
                Vector3 c = n * 0.5f;

                int b = vertices.Count;
                vertices.Add(c - u * 0.5f - v * 0.5f);
                vertices.Add(c + u * 0.5f - v * 0.5f);
                vertices.Add(c + u * 0.5f + v * 0.5f);
                vertices.Add(c - u * 0.5f + v * 0.5f);

                var tone = new Color(face.tone, face.tone, face.tone, 1f);
                for (int i = 0; i < 4; i++)
                {
                    normals.Add(n);
                    colors.Add(tone);
                }
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));

                triangles.Add(b); triangles.Add(b + 2); triangles.Add(b + 1);
                triangles.Add(b); triangles.Add(b + 3); triangles.Add(b + 2);
            }

            var mesh = new Mesh { name = "ShadedCube", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            _shadedCube = mesh;
            return mesh;
        }

        static Mesh _cubeMesh;

        static Mesh CubeMesh()
        {
            if (_cubeMesh != null) return _cubeMesh;

            // Yalnız KÜP AĞI isteniyor: nesne yaratıp hemen silmeye gerek yok.
            // (Eskiden CreatePrimitive ile bir küp kurulup ağı alınıp
            // nesne siliniyordu — üstelik o çağrı çarpıştırıcı da ekliyordu.)
            _cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            return _cubeMesh;
        }

        Color ColorOf(BlockColor color)
        {
            var entry = _palette != null ? _palette.Get(color) : null;
            return entry != null ? entry.particleColor : Color.white;
        }
    }
}
