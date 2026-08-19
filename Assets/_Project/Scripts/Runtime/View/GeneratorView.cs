using System.Collections;
using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Blok üretecinin görseli — REFERANSTAN YENİDEN KURULDU (4. tur, L43).
    ///
    /// Kullanıcı: "Yeni gelen makine feature'ının orijinal oyunla alakası yok.
    /// Birebir yeniden yapılacak: gerekli assetler üretilecek, çalışma şekli,
    /// animasyonu, kırmızı/yeşil yanma göstergesi, bittiğinde modelin
    /// parçalanma efekti."
    ///
    /// REFERANS ÇÖZÜMLENDİ (41-50 yürüyüşü, 49. bölüm, 12:16 karesi; makine
    /// tahtanın sol kenarında, tam çözünürlükte büyütüldü). Makine dört
    /// parçadan oluşuyor ve hepsi bir bilgi taşıyor:
    ///
    ///   1) GÖVDE       — kenar boyunca uzanan, yuvarlak köşeli magenta kutu
    ///   2) SAYAÇ BAŞI  — dış uçta koyu bir plaka, içinde kalan blok sayısı
    ///   3) LAMBA       — sayaç plakasının yanında küçük bir ışık
    ///   4) PENCERE     — koyu lacivert bir oyuk; içinde SIRADAKİ BLOĞUN
    ///                    kendi şekli, kendi renginde duruyor
    ///
    /// Bizdeki hâli iki düz küptü: gövde ve "sıradaki rengin" düz bir kutusu.
    /// Yani şekil bilgisi hiç yoktu (oyuncu 1×3 mü 2×2 mi geleceğini
    /// bilmiyordu) ve makinenin çalışıp çalışmadığı da görünmüyordu.
    ///
    /// DERS (bir makineyi makine yapan şey, DURUMUNU göstermesidir): Referansta
    /// lamba kırmızıyken "girişim kapalı", yeşilken "yer açılırsa iterim"
    /// diyor. O tek nokta olmadan makine bir dekor; oyuncu bloğun neden
    /// gelmediğini anlamıyor ve tahtayı yanlış okuyor.
    /// </summary>
    public sealed class GeneratorView : MonoBehaviour
    {
        /// <summary>Kenardan dışa taşma (hücre).</summary>
        const float Depth = 0.92f;

        /// <summary>Kenar boyunca uzunluk (hücre).</summary>
        const float Along = 1.85f;

        const float BodyHeight = 0.52f;

        static readonly Color Shell     = new Color(0.706f, 0.235f, 0.780f);
        static readonly Color ShellDark = new Color(0.435f, 0.129f, 0.514f);
        static readonly Color WellInk   = new Color(0.106f, 0.086f, 0.290f);
        static readonly Color LampOn    = new Color(0.278f, 0.937f, 0.310f);
        static readonly Color LampOff   = new Color(0.937f, 0.180f, 0.180f);

        GeneratorModel _model;
        TMPro.TextMeshPro _counter;
        Transform _pieces;
        MeshRenderer _lamp;
        MeshRenderer _preview;
        Material _lampMaterial;
        BoardSpace _space;
        bool _lampLit;

        public static GeneratorView Create(
            Transform parent, GeneratorModel model, BoardSpace space, Material nextMaterial)
        {
            var go = new GameObject($"Generator_{model.Side}_{model.X}_{model.Y}");
            go.transform.SetParent(parent, worldPositionStays: false);

            var view = go.AddComponent<GeneratorView>();
            view._model = model;
            view._space = space;

            bool horizontalEdge = model.Side == Side.North || model.Side == Side.South;
            float sizeX = horizontalEdge ? Along : Depth;
            float sizeZ = horizontalEdge ? Depth : Along;

            go.transform.position = view.WorldCenter(space);

            var pieces = new GameObject("Pieces").transform;
            pieces.SetParent(go.transform, worldPositionStays: false);
            view._pieces = pieces;

            // --- 1. gövde -------------------------------------------------
            Piece(pieces, "Shell", sizeX, sizeZ, BodyHeight, ShellDark, 0f);
            Piece(pieces, "Face", sizeX - 0.11f, sizeZ - 0.11f, BodyHeight + 0.03f, Shell, 0f);

            // Yön vektörleri: makinenin İÇ ucu tahtaya, DIŞ ucu dışarı bakar.
            Vector3 outward = view.Outward();
            Vector3 alongAxis = horizontalEdge ? Vector3.right : Vector3.forward;

            // --- 4. pencere (tahtaya bakan uçta) --------------------------
            //
            // Pencere İÇ uçta: blok oradan çıkıyor, yani oyuncunun bakacağı yer
            // makinenin tahtaya değen tarafı.
            float windowSpan = Along * 0.52f;
            var wellSize = horizontalEdge
                ? new Vector2(windowSpan, Depth - 0.28f)
                : new Vector2(Depth - 0.28f, windowSpan);
            var well = Piece(pieces, "Well", wellSize.x, wellSize.y,
                BodyHeight + 0.05f, WellInk, 0f);

            // Makinenin parçaları KENAR BOYUNCA diziliyor (referansta da öyle):
            // bir uçta sayaç başı ve lamba, öbür uçta pencere. `outward` bu
            // dizilime karışmıyor; o yalnız geri tepme ve parçalanma yönü.
            well.transform.localPosition = AlongOffset(alongAxis,
                -(Along * 0.5f - windowSpan * 0.5f - 0.08f));

            // Sıradaki bloğun KENDİ ŞEKLİ, kendi renginde.
            view.BuildPreview(well.transform, nextMaterial, wellSize);

            // --- 2. sayaç başı (dış uçta) ---------------------------------
            var head = Piece(pieces, "Head",
                horizontalEdge ? Along * 0.40f : Depth - 0.20f,
                horizontalEdge ? Depth - 0.20f : Along * 0.40f,
                BodyHeight + 0.07f, ShellDark, 0f);
            head.transform.localPosition = AlongOffset(alongAxis, Along * 0.5f - Along * 0.20f - 0.06f);

            // --- 3. lamba --------------------------------------------------
            var lamp = Piece(pieces, "Lamp", 0.20f, 0.20f, BodyHeight + 0.12f, LampOff, 0.09f);
            lamp.transform.localPosition =
                AlongOffset(alongAxis, Along * 0.5f - 0.16f)
                + Perp(alongAxis) * (horizontalEdge ? Depth * 0.28f : Depth * 0.28f);
            view._lamp = lamp.GetComponent<MeshRenderer>();
            view._lampMaterial = ViewKit.Solid("GeneratorLamp", LampOff);
            view._lamp.sharedMaterial = view._lampMaterial;

            // Sayaç lambanın yanında, makinenin dış ucunda.
            Vector3 headWorld = go.transform.position + head.transform.localPosition;
            view._counter = ViewKit.CreateCounter(
                parent, headWorld + Vector3.up * (BodyHeight + 0.16f),
                model.Remaining, ViewKit.CounterStyle.Gold);

            return view;
        }

        /// <summary>Eksen boyunca kaydırma — hangi eksen olduğunu çağıran bilir.</summary>
        static Vector3 AlongOffset(Vector3 axis, float amount) => axis * amount;

        /// <summary>Eksene dik yatay yön.</summary>
        static Vector3 Perp(Vector3 axis) => new Vector3(-axis.z, 0f, axis.x);

        static GameObject Piece(Transform parent, string name, float sizeX, float sizeZ,
            float height, Color color, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.AddComponent<MeshFilter>().sharedMesh = PrismMeshBuilder.Build(
                Mathf.Max(0.05f, sizeX), Mathf.Max(0.05f, sizeZ), height,
                radius > 0f ? radius : -1f, -1f, name);

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ViewKit.Solid("Gen_" + name, color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        /// <summary>
        /// Penceredeki önizleme: sıradaki bloğun GERÇEK silüeti.
        ///
        /// DERS (renk yetmez, ŞEKİL de bir plandır): Eski pencere yalnız
        /// sıradaki rengi gösteren düz bir kutuydu. Oyuncu "mavi gelecek"
        /// biliyor ama "1×3 mü, 2×2 mi" bilmiyordu — oysa yer açma kararı
        /// tamamen şekle bağlı. Aynı mesh'i (saplamasız silüet) küçültüp
        /// pencereye koymak o bilgiyi bedavaya veriyor.
        /// </summary>
        void BuildPreview(Transform well, Material nextMaterial, Vector2 wellSize)
        {
            if (_model.IsEmpty) return;
            var block = _model.Queue[0];

            var go = new GameObject("Next");
            go.transform.SetParent(well, worldPositionStays: false);
            go.AddComponent<MeshFilter>().sharedMesh = BrickMeshBuilder.GetSilhouette(block);

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = nextMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _preview = renderer;

            // Silüet blok boyutunda; pencereye SIĞACAK kadar küçültülüyor.
            float fit = Mathf.Min(wellSize.x / Mathf.Max(1, block.W),
                                  wellSize.y / Mathf.Max(1, block.H)) * 0.74f;
            go.transform.localScale = new Vector3(fit, 0.32f, fit);
            go.transform.localPosition = new Vector3(0f, 0.06f, 0f);
        }

        /// <summary>Tahtadan dışa bakan yön.</summary>
        Vector3 Outward()
        {
            switch (_model.Side)
            {
                case Side.West:  return Vector3.left;
                case Side.East:  return Vector3.right;
                case Side.North: return Vector3.forward;
                default:         return Vector3.back;
            }
        }

        /// <summary>Edit modunda Destroy yasak; araç/doğrulama yollarında da çalışsın.</summary>
        static void Kill(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        Vector3 WorldCenter(BoardSpace space)
        {
            // Makine kenarın DIŞINDA durur; girişin tam karşısına hizalanır.
            float half = Depth * 0.5f + 0.14f;
            switch (_model.Side)
            {
                case Side.West:
                    return space.CornerToWorld(0f, _model.Y + 0.5f, 0f) + Vector3.left * half;
                case Side.East:
                    return space.CornerToWorld(space.Width, _model.Y + 0.5f, 0f) + Vector3.right * half;
                case Side.North:
                    return space.CornerToWorld(_model.X + 0.5f, 0f, 0f) + Vector3.forward * half;
                default:
                    return space.CornerToWorld(_model.X + 0.5f, space.Height, 0f) + Vector3.back * half;
            }
        }

        /// <summary>
        /// Lambayı yakar/söndürür. Sistem her hamleden sonra çağırıyor.
        ///
        /// Yeşil = "sıradaki blok tahtaya sığıyor, ilk fırsatta itilecek".
        /// Kırmızı = "giriş kapalı, önce yer aç".
        /// </summary>
        public void SetReady(bool ready)
        {
            if (_lampLit == ready && _lampMaterial != null) return;
            _lampLit = ready;
            if (_lampMaterial == null) return;

            var color = ready ? LampOn : LampOff;
            _lampMaterial.color = color;
            if (_lampMaterial.HasProperty("_BaseColor"))
                _lampMaterial.SetColor("_BaseColor", color);
        }

        /// <summary>
        /// Bir blok itildi: sayaç ve pencere tazelenir, makine geri teper,
        /// sıra bitince PARÇALANIR.
        /// </summary>
        public void UpdateQueue()
        {
            if (_counter != null) _counter.text = _model.Remaining.ToString();

            if (!_model.IsEmpty)
            {
                RefreshPreview();
                if (isActiveAndEnabled) StartCoroutine(Recoil());
                return;
            }

            if (_counter != null) Kill(_counter.gameObject);
            _counter = null;

            if (Application.isPlaying && isActiveAndEnabled) StartCoroutine(Shatter());
            else Kill(gameObject);
        }

        void RefreshPreview()
        {
            if (_preview == null || _model.IsEmpty) return;
            _preview.GetComponent<MeshFilter>().sharedMesh =
                BrickMeshBuilder.GetSilhouette(_model.Queue[0]);
            // Renk paletten değil, bloğun kendi materyalinden gelir; sistem
            // burayı `SetNextMaterial` ile besliyor.
        }

        /// <summary>Kuyruk ilerledi: pencere yeni rengin materyalini alır.</summary>
        public void SetNextMaterial(Material material)
        {
            if (_preview != null && material != null) _preview.sharedMaterial = material;
        }

        /// <summary>
        /// Blok çıkarken makine dışa doğru bir tık geri teper.
        ///
        /// DERS (kuvvet çift yönlüdür): Blok makineden fırlarken makine
        /// kımıldamıyordu; blok "makinenin içinden geçip gitmiş" gibi
        /// görünüyordu. Küçük bir geri tepme, ittiren tarafın da var olduğunu
        /// söylüyor.
        /// </summary>
        IEnumerator Recoil()
        {
            if (_pieces == null) yield break;

            Vector3 home = _pieces.localPosition;
            Vector3 kick = Outward() * 0.16f;
            const float duration = 0.22f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (_pieces == null) yield break;
                float k = t / duration;
                // Hızlı çık, yavaş dön.
                float amount = k < 0.30f ? k / 0.30f : 1f - (k - 0.30f) / 0.70f;
                _pieces.localPosition = home + kick * amount;
                yield return null;
            }
            if (_pieces != null) _pieces.localPosition = home;
        }

        /// <summary>
        /// Sıra bitince makine PARÇALANIR (4. tur, L43).
        ///
        /// DERS (biten bir şey yok olmaz, BİTER): Makine kuyruğu boşalınca
        /// `Destroy` ile bir karede kayboluyordu; oyuncu tahtanın kenarında
        /// bir şeyin eksildiğini ancak sonradan fark ediyordu. Referansta
        /// makine kırılıp dağılıyor — bu hem "bu makine bir daha çalışmayacak"
        /// bilgisini veriyor hem de o anı ödüllendiriyor.
        ///
        /// Parçalar makinenin KENDİ parçalarından üretiliyor: her parça
        /// rastgele bir yöne fırlıyor, dönüyor ve küçülerek sönüyor. Ayrı bir
        /// parçacık sistemi kurmaya gerek yok; ekranda uçan şeyler tam olarak
        /// az önce orada duran şeyler.
        /// </summary>
        IEnumerator Shatter()
        {
            GameKit.FX.CameraShake.Add(0.18f);
            GameKit.Services.Haptics.Tap(GameKit.Services.HapticStrength.Medium);

            var chunks = new List<(Transform piece, Vector3 velocity, Vector3 spin)>();
            if (_pieces != null)
            {
                foreach (Transform piece in _pieces)
                {
                    // Yön: dışa doğru bir koni. Tam rastgele yön parçaları
                    // tahtanın üstüne savuruyordu.
                    Vector3 dir = (Outward() * 1.4f
                                   + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f))
                                   ).normalized;
                    chunks.Add((piece,
                        dir * Random.Range(1.1f, 2.3f) + Vector3.up * Random.Range(2.0f, 3.4f),
                        new Vector3(Random.Range(-360f, 360f), Random.Range(-360f, 360f),
                                    Random.Range(-360f, 360f))));
                }
            }

            const float life = 0.62f;
            for (float t = 0f; t < life; t += Time.deltaTime)
            {
                float dt = Time.deltaTime;
                float k = t / life;
                for (int i = 0; i < chunks.Count; i++)
                {
                    var (piece, velocity, spin) = chunks[i];
                    if (piece == null) continue;

                    velocity += Vector3.down * 9.5f * dt;
                    piece.localPosition += velocity * dt;
                    piece.Rotate(spin * dt, Space.Self);
                    piece.localScale = Vector3.one * Mathf.Max(0.02f, 1f - k);
                    chunks[i] = (piece, velocity, spin);
                }
                yield return null;
            }

            Kill(gameObject);
        }
    }
}
