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

        void OnBlockAbsorbed(BlockModel block, GateModel gate)
        {
            // PARÇACIK KAPIDAN, ÇIKIŞ YÖNÜNE DOĞRU ÇIKAR (2. tur, 53. madde).
            //
            // Referans (`Levels.mp4`, 00:28): sarı blok KUZEY kapısından
            // emilirken kırıntılar tahtanın DIŞINDA, kapının üstünde ve
            // yukarı doğru bir koni hâlinde saçılıyor.
            //
            // DERS (parçacık nerede doğduğunu anlatır): Bizde patlama bloğun
            // MERKEZİNDE ve küresel doğuyordu; yani blok kapıya girerken
            // kırıntılar tahtanın ortasında beliriyordu ve olay "blok patladı"
            // gibi okunuyordu, "kapıdan geçti" gibi değil. Aynı sayıda
            // parçacık, doğru yerde ve doğru yönde, bambaşka bir cümle kuruyor.
            // SAYI DA REFERANSTAN: aynı karede kapının üstünde 13 ayrı parça
            // sayıldı ve bunların bir kısmı birbirine değdiği için tek küme
            // olarak ölçüldü — gerçek sayı daha yüksek. 14 + alan*4 (1x1 blok
            // için 18) yığın hissini vermiyordu.
            // SAYI YENİDEN SAYILDI (6. tur): 20 fps'te kare kare bakıldığında
            // kapının dışında aynı anda 12-16 parça var. Eskiden 30 + alan*8
            // yazıyordu (1x1 blok için 38); parçalar iri hâle gelince o sayı
            // ekranı dolduran bir bulut üretiyordu.
            //
            // DERS (iki ayarı birlikte değiştirmek): Boyut büyütülürken SAYI
            // aynı bırakılırsa toplam alan kat kat artar. Referansta değişen
            // şey parça sayısı değil, parçanın kendisiydi.
            BurstFromGate(gate, ColorOf(block.CurrentColor),
                          12 + block.Cells.Count * 3);

            // Sarsıntı bloğun BÜYÜKLÜĞÜNE bağlı: 1x1 bir parçanın emilmesiyle
            // 2x4'lük bir kütlenin emilmesi aynı ağırlıkta hissedilmemeli.
            GameKit.FX.CameraShake.Add(0.10f + block.Cells.Count * 0.012f);
        }

        void OnLayerPeeled(BlockModel block, GateModel gate)
        {
            // Soyulan katmanın rengi zaten listeden çıktı; kapının rengi doğru olan.
            // Soyulma da kapıda olur — kırıntı orada doğmalı, blok yerinde kalır.
            BurstFromGate(gate, ColorOf(gate.ActiveColor), 14);
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
        void OnIceDecremented(BlockModel block)
        {
            Vector3 at = _space.RectCenterToWorld(
                block.Position, block.W, block.H, BrickHeightHalf);
            IceCrack(at, block.Cells.Count);
            GameKit.FX.CameraShake.Add(0.10f);
        }

        void OnGateIceDecremented(GateModel gate)
        {
            IceCrack(GateWorldPoint(gate), Mathf.Max(1, gate.Length));
            GameKit.FX.CameraShake.Add(0.10f);
        }

        /// <summary>Çatlama kırıntısı: kırılmanın küçük kardeşi.</summary>
        void IceCrack(Vector3 position, int cells)
        {
            int shards = 5 + cells * 2;
            var chips = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
                startColor = new Color(0.78f, 0.94f, 1f, 1f),
                startLifetime = 0.42f,
                startSize = 0.20f
            };
            _crumbs.Emit(chips, shards);

            // Tek bir beyaz kıvılcım: "çatladı" vurgusu.
            var spark = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
                startColor = Color.white,
                startLifetime = 0.18f,
                startSize = 0.12f
            };
            _crumbs.Emit(spark, 3);
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
        void BurstFromGate(GateModel gate, Color color, int count)
        {
            // Kapının kenar üzerindeki orta noktası ve dışa bakan yönü.
            float spanCenter = (gate.SpanMin + gate.SpanMax) * 0.5f;
            float half = (gate.SpanMax - gate.SpanMin) * 0.5f;

            Vector3 outward = gate.EdgeHorizontal
                ? new Vector3(0f, 0f, -gate.OutwardSign)
                : new Vector3(gate.OutwardSign, 0f, 0f);

            // Kırıntı kapının bir tık DIŞINDA doğuyor: tam çizgide doğarsa
            // yarısı tahtanın içinde kalıyor ve çerçevenin arkasına giriyor.
            const float outset = 0.35f;

            for (int i = 0; i < count; i++)
            {
                float alongOffset = Random.Range(-half, half);
                float outOffset = Random.Range(0f, 0.25f);

                float spanPos = spanCenter + alongOffset;
                float edgePos = gate.EdgeCoord + gate.OutwardSign * (outset + outOffset);

                Vector3 at = gate.EdgeHorizontal
                    ? _space.CornerToWorld(spanPos, edgePos, BrickHeightHalf)
                    : _space.CornerToWorld(edgePos, spanPos, BrickHeightHalf);

                // Koni: ana yön dışarı, yanlara ve yukarı serpinti.
                Vector3 sideways = new Vector3(-outward.z, 0f, outward.x);
                Vector3 velocity =
                    outward * Random.Range(2.4f, 4.6f) +
                    sideways * Random.Range(-1.5f, 1.5f) +
                    Vector3.up * Random.Range(1.2f, 3.4f);

                // İRİ PARÇALAR (6. tur).
                //
                // ÖLÇÜM (Levels 1-20, 00:24, 20 fps'te kare kare): blok kapıya
                // girerken kendi renginde 12-16 adet İRİ parçaya ayrılıyor;
                // parça kenarı 10-14 piksel / 79 piksellik hücre = hücrenin
                // %13-18'i. Bizimki varsayılan boyutta ince kırıntıydı, yani
                // "toz" gibi okunuyordu.
                //
                // DERS (parçanın boyu, kırılan şeyin boyunu anlatır): Küçük
                // kırıntı "bir şey ufalandı" der; iri parça "bir BLOK kırıldı"
                // der. Referans ikincisini seçmiş — çünkü kaybolan şey bir
                // blok.
                var emit = new ParticleSystem.EmitParams
                {
                    position = at,
                    velocity = velocity,
                    applyShapeToPosition = false,
                    startColor = color,
                    startSize = Random.Range(0.26f, 0.42f),
                    startLifetime = Random.Range(0.55f, 0.80f)
                };
                _crumbs.Emit(emit, 1);
            }
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
