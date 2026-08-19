using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Perdenin görseli (L20): bölgeyi kaplayan koyu panel + altın çerçeve +
    /// sayaç rozeti. Sayaç 0'a inince panel kalkar; içerik bloklarını
    /// ObstacleSystem doğurur. Açılma animasyonu M4'te.
    /// </summary>
    public sealed class CurtainView : MonoBehaviour
    {
        const float PanelHeight = 0.5f;

        TMPro.TextMeshPro _counter;
        CurtainModel _model;

        public static CurtainView Create(Transform parent, CurtainModel model, BoardSpace space)
        {
            var root = new GameObject($"Curtain_{model.X}_{model.Y}");
            root.transform.SetParent(parent, worldPositionStays: false);

            Vector3 center = space.RectCenterToWorld(
                new Vector2(model.X, model.Y), model.W, model.H, PanelHeight * 0.5f);

            // PERDE ARTIK YUVARLAK KÖŞELİ (4. tur, I36).
            //
            // Kullanıcı: "Perde detay görseli birebir orijinaliyle aynı olacak."
            //
            // REFERANS (41-50 yürüyüşü, 49. bölüm — üç perde bir arada):
            // panel yuvarlak köşeli, çevresinde KALIN altın bir çerçeve, yüzeyde
            // ince yatay tırtıllar ve ortada altın çerçeveli bir sayaç rozeti.
            // Bizim panelimiz keskin köşeli iki küptü; tahtanın geri kalanı
            // (bloklar, kapılar, çerçeve) yuvarlakken perde tek keskin nesneydi
            // ve "arayüzden kalma bir kutu" gibi duruyordu.
            //
            // DERS (bir sahnede TEK keskin nesne, en çok göze batan nesnedir):
            // Yuvarlaklık burada süs değil, aidiyet. `PrismMeshBuilder` kapı
            // barı ve makine ile aynı yarıçapı verdiği için perde de aynı
            // malzemeden dökülmüş görünüyor.
            var frame = new GameObject("Frame");
            frame.transform.SetParent(root.transform, false);
            frame.AddComponent<MeshFilter>().sharedMesh = PrismMeshBuilder.Build(
                model.W, model.H, PanelHeight, 0.34f, 0.05f, "CurtainFrame");
            var frameRenderer = frame.AddComponent<MeshRenderer>();
            frameRenderer.sharedMaterial = ViewKit.CurtainFrame;
            frameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            frameRenderer.receiveShadows = false;
            // ÇERÇEVE DIŞA TAŞMAZ, PANEL İÇERİ ÇEKİLİR.
            //
            // Kalınlık ölçüldü: referansta (22. bölümün büyük perdesi) altın
            // kenar ~10 piksel, hücre ~60 piksel — yani hücrenin %17'si.
            //
            // DERS (büyüterek kenar yapılmaz): Çerçeve önce perdeden BÜYÜK bir
            // kutuydu ve altına konuyordu; taşan pay kenar olarak görünsün
            // diye. Kalınlığı 0.05'ten 0.17 hücreye çıkardığımda da hiçbir şey
            // değişmedi. Sebep ölçünce çıktı: 22. bölümün perdesi tahtanın
            // neredeyse tamamını kaplıyor (5.96 / 6.00) ve taşan çerçeve
            // tahtanın KENDİ kenarının altında kalıyor — o kenar daha yüksek
            // (0.80 > 0.44). Dışarı taşan hiçbir pay, dışarısı doluyken
            // görünmez. Doğrusu çerçeveyi bölgenin tam boyutunda tutup PANELİ
            // içeri çekmek: kenar her zaman kendi alanının içinde kalıyor.
            const float Border = 0.17f;              // hücre payı, her kenarda
            // Prizma mesh'i TABANINDAN başlıyor; merkez hesabı yarım yükseklik
            // içerdiği için taban konumu geri alınıyor.
            frame.transform.position = center - Vector3.up * (PanelHeight * 0.5f);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform, false);
            panel.AddComponent<MeshFilter>().sharedMesh = PrismMeshBuilder.Build(
                model.W - Border * 2f, model.H - Border * 2f, PanelHeight, 0.26f, 0.05f,
                "CurtainPanel");
            var panelRenderer = panel.AddComponent<MeshRenderer>();
            panelRenderer.sharedMaterial = ViewKit.CurtainPanel;
            panelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            panelRenderer.receiveShadows = false;
            // Panel çerçeveden bir tık YÜKSEK: aynı hizada olsalar iki yüzey
            // aynı derinlikte çakışır ve kenar yer yer kayboldu-göründü olur.
            panel.transform.position = center - Vector3.up * (PanelHeight * 0.5f - 0.02f);

            // SÜSLER PANELİN GERÇEK TEPESİNDEN ÖLÇÜLÜR.
            //
            // DERS (aynı hata, üçüncü kez): Tırtıllar ve parıltılar önce
            // `PanelHeight * 0.5f + 0.004f` ile konumlanıyordu — yani panelin
            // sabit varsayılan tepesinden. Sonra paneli çerçeveden ayırmak için
            // 0.02 yükselttim ve süsler panelin İÇİNDE kaldı; ekranda hiçbiri
            // görünmedi. Sayaçlarda da, iç katmanda da aynı şey olmuştu.
            // Bir yüzeyin üstüne konan her şey, o yüzeyin ÖLÇÜLEN tepesine
            // bağlanmalı; hesapla varsayılan tepe, ilk taşımada yalan olur.
            float panelTop = panelRenderer.bounds.max.y;
            AddSlats(root.transform, center, panelTop, model, Border);
            AddSparkles(root.transform, center, panelTop, model, Border);

            var view = root.AddComponent<CurtainView>();
            view._model = model;

            // Yükseklik perdenin GERÇEK tepesinden; sabit hesap rakamı panelin
            // içine gömüyordu (bkz. BlockView'daki aynı ders).
            float curtainTop = center.y + PanelHeight * 0.5f;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                if (r.bounds.max.y > curtainTop) curtainTop = r.bounds.max.y;

            AddBadge(root.transform, new Vector3(center.x, curtainTop, center.z));

            // Rakam ROZETİN İÇİNDE ve BEYAZ: referansta perdenin sayacı yüzeye
            // yazılmış bir rakam değil, altın çerçeveli koyu bir rozetin içinde
            // duruyor. Altın rakamı doğrudan mor panele yazmak, sayacı yüzeyin
            // bir parçası gibi gösteriyordu — oysa o bir ETİKET.
            view._counter = ViewKit.CreateCounter(
                root.transform, new Vector3(center.x, curtainTop + 0.10f, center.z), model.Count,
                ViewKit.CounterStyle.Badge);

            return view;
        }

        /// <summary>
        /// Yüzeydeki YATAY TIRTIL ÇİZGİLERİ.
        ///
        /// DERS (rastgelelik doku değildir): Burada önce rastgele açılı,
        /// rastgele boyda "cam çizikleri" vardı. Referansta ise çizgiler
        /// DÜZENLİ: eşit aralıklı, perdenin tamamı boyunca uzanan yatay
        /// tırtıllar. Rastgele çizik "hasar" anlatıyor, düzenli tırtıl
        /// "malzeme" anlatıyor — ikisi bambaşka şeyler söylüyor.
        ///
        /// Aralık hücre başına sabit; böylece küçük perde de büyük perde de
        /// aynı dokuya sahip oluyor, çizgi SAYISI perdeyle birlikte büyüyor.
        /// </summary>
        static void AddSlats(Transform parent, Vector3 center, float panelTop,
                             CurtainModel model, float border)
        {
            // ÖLÇÜM: referansta çizgiler 20 piksel arayla, hücre ~60 piksel —
            // yani hücrenin üçte biri.
            const float Spacing = 0.33f;
            float inner = model.H - border * 2f;
            int count = Mathf.Max(1, Mathf.FloorToInt(inner / Spacing) - 1);
            float step = inner / (count + 1);

            for (int i = 1; i <= count; i++)
            {
                var slat = ViewKit.CreateShape(PrimitiveType.Cube, "Shape");
                slat.name = "Slat";
                slat.transform.SetParent(parent, worldPositionStays: false);
                var renderer = slat.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = ViewKit.CurtainStreak;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                slat.transform.position = new Vector3(
                    center.x, panelTop + 0.004f,
                    center.z - inner * 0.5f + i * step);
                slat.transform.localScale = new Vector3(
                    model.W - border * 2f - 0.06f, 0.02f, 0.035f);
            }
        }

        /// <summary>
        /// Serpilmiş parıltılar: yüzeye derinlik veren küçük eşkenar dörtgenler.
        ///
        /// Konumlar perdenin KOORDİNATINDAN türetilen sabit bir tohumla
        /// üretiliyor — aynı bölüm her açılışta aynı görünsün diye. Kare başına
        /// değişen bir parıltı deseni, göz onu "titreme" olarak okur.
        /// </summary>
        static void AddSparkles(Transform parent, Vector3 center, float panelTop,
                                CurtainModel model, float border)
        {
            // ---- 7. tur, V69 ----
            //
            // Kullanıcı: "Perde tasarımı daha iyi hale getirilecek. Parıltılar
            // tek bir yerde toplanmak yerine eşit şekilde dağıtılacak."
            //
            // ÜÇ AYRI HATA VARDI, üçü de referansla ölçülerek bulundu
            // (`…Levels 1-20 Walkthrough.mp4` 11:01, 20. bölümün perdesi):
            //
            // 1) DAĞILIM. Konumlar düz `Range(-half, half)` ile atılıyordu.
            //    Düzgün rastgelelik KÜMELENİR — bu iyi bilinen bir şeydir ama
            //    göz onu "rastgele" değil "bir yere yığılmış" diye okur.
            //    Çözüm katmanlı (stratified) örnekleme: yüzey ızgaraya
            //    bölünüyor, her hücreye BİR parıltı ve hücre içinde rastgele
            //    bir sapma. Hem eşit dağılım hem organik görünüm.
            //
            // 2) PARLAKLIK. Referansta parıltı ile zemin arasındaki fark
            //    NEREDEYSE YOK: zemin (57,36,187), parıltı (64,44,183) — yedi
            //    birim. Bizimki ekranda (206,201,201) ölçüldü, yani BEYAZ.
            //    Sebep: parıltı bir KÜP idi; saydam bir küpün ön ve arka
            //    yüzleri üst üste harmanlanıyor, üstelik kümelenen parıltılar
            //    birbirinin üstüne biniyordu — %10'luk alfa ekranda %75'e
            //    çıkıyordu. Tek yüzlü bir levha bu birikmeyi ortadan kaldırıyor.
            //
            // 3) BİÇİM. 45° döndürülmüş küp bir EŞKENAR DÖRTGEN verir;
            //    referanstaki ise içbükey kollu DÖRT UÇLU YILDIZ. Elmas
            //    "mücevher", yıldız "parıltı" anlatıyor.
            //
            // DERS (saydamlık ÜST ÜSTE BİNMEYİ affetmez): Bir yüzeyin
            // görünürlüğünü alfa ile ayarlarken o yüzeyin kaç kez çizildiğini
            // de hesaba katmak gerekiyor. Aynı alfa, tek katmanda görünmez,
            // altı katmanda bembeyaz olur.
            float halfW = (model.W - border * 2f) * 0.44f;
            float halfH = (model.H - border * 2f) * 0.44f;

            // Izgara oranı yüzeyin en-boyunu izliyor: kare hücreler, yani
            // yatayda ve dikeyde AYNI yoğunluk.
            int cols = Mathf.Clamp(Mathf.RoundToInt(halfW * 2f / 0.95f), 2, 7);
            int rows = Mathf.Clamp(Mathf.RoundToInt(halfH * 2f / 0.95f), 2, 7);

            var random = new System.Random(model.X * 73856093 ^ model.Y * 19349663);
            float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);

            float cellW = halfW * 2f / cols;
            float cellH = halfH * 2f / rows;

            // Sayaç rozeti perdenin ORTASINDA duruyor; oradaki hücreler
            // atlanıyor, yoksa parıltı rakamın arkasında kalıp kirletiyor.
            float guardX = Mathf.Min(halfW * 0.42f, 0.55f);
            float guardZ = Mathf.Min(halfH * 0.42f, 0.55f);

            for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                float x = -halfW + (col + 0.5f) * cellW + Range(-cellW * 0.30f, cellW * 0.30f);
                float z = -halfH + (row + 0.5f) * cellH + Range(-cellH * 0.30f, cellH * 0.30f);
                if (Mathf.Abs(x) < guardX && Mathf.Abs(z) < guardZ) continue;

                var spark = new GameObject("Sparkle");
                spark.transform.SetParent(parent, worldPositionStays: false);
                spark.AddComponent<MeshFilter>().sharedMesh = StarMesh;
                var renderer = spark.AddComponent<MeshRenderer>();
                // Parıltı AÇIK, tırtıl çizgisi KOYU — aynı materyali
                // paylaşamazlar (bkz. ViewKit.CurtainSparkle).
                renderer.sharedMaterial = ViewKit.CurtainSparkle;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                // Boy hücreden hücreye değişiyor: eşit dağılım ile eşit BOYUT
                // ayrı şeyler; ikincisi deseni ızgara gibi gösterirdi.
                // Referansta yıldızlar ~18 piksel, hücre ~60 → hücrenin %30'u.
                float size = Range(0.22f, 0.40f);
                spark.transform.position =
                    new Vector3(center.x + x, panelTop + 0.006f, center.z + z);
                spark.transform.localScale = new Vector3(size, 1f, size);
                spark.transform.rotation = Quaternion.Euler(0f, Range(-14f, 14f), 0f);
            }
        }

        static Mesh _starMesh;

        /// <summary>
        /// Dört uçlu parıltı yıldızı — TEK YÜZLÜ yatay levha.
        ///
        /// Kollar 0/90/180/270 derecede dış yarıçapta, aralar 45 derecede iç
        /// yarıçapta. İç yarıçap dışın %22'si: referanstaki gibi ince, uzun
        /// kollu bir parıltı. (%50 olsaydı sekizgen, %70 olsaydı daire olurdu.)
        /// </summary>
        static Mesh StarMesh
        {
            get
            {
                if (_starMesh != null) return _starMesh;

                const int arms = 4;
                const float inner = 0.22f;
                var verts = new Vector3[arms * 2 + 1];
                var normals = new Vector3[verts.Length];
                var tris = new int[arms * 2 * 3];

                verts[0] = Vector3.zero;                       // merkez
                for (int i = 0; i < arms * 2; i++)
                {
                    float angle = Mathf.PI * 2f * i / (arms * 2f);
                    float r = (i % 2 == 0) ? 0.5f : 0.5f * inner;
                    verts[i + 1] = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                }
                for (int i = 0; i < verts.Length; i++) normals[i] = Vector3.up;

                for (int i = 0; i < arms * 2; i++)
                {
                    // Yukarıdan bakıldığında ön yüz: saat yönünde sıralı.
                    tris[i * 3 + 0] = 0;
                    tris[i * 3 + 1] = 1 + (i + 1) % (arms * 2);
                    tris[i * 3 + 2] = 1 + i;
                }

                _starMesh = new Mesh { name = "CurtainSparkle" };
                _starMesh.vertices = verts;
                _starMesh.normals = normals;
                _starMesh.triangles = tris;
                _starMesh.RecalculateBounds();
                return _starMesh;
            }
        }

        /// <summary>
        /// Sayacın altındaki rozet: altın çerçeve + koyu iç.
        ///
        /// İki kutu yetiyor — dıştaki çerçeve, içindeki biraz küçük ve biraz
        /// yüksek olan koyu yüz. Yükseklik farkı, çerçevenin bir kabartma gibi
        /// okunmasını sağlıyor.
        /// </summary>
        static void AddBadge(Transform parent, Vector3 top)
        {
            // Ölçüm: rozet referansta ~46 piksel, hücre ~60 — yani 0,77 hücre.
            const float RimSize = 0.76f, FaceSize = 0.60f;

            var rim = ViewKit.CreateShape(PrimitiveType.Cube, "Shape");
            rim.name = "BadgeRim";
            rim.transform.SetParent(parent, worldPositionStays: false);
            rim.GetComponent<MeshRenderer>().sharedMaterial = ViewKit.BadgeRim;
            rim.transform.position = top + Vector3.up * 0.03f;
            rim.transform.localScale = new Vector3(RimSize, 0.06f, RimSize);

            var face = ViewKit.CreateShape(PrimitiveType.Cube, "Shape");
            face.name = "BadgeFace";
            face.transform.SetParent(parent, worldPositionStays: false);
            face.GetComponent<MeshRenderer>().sharedMaterial = ViewKit.BadgeFace;
            face.transform.position = top + Vector3.up * 0.05f;
            face.transform.localScale = new Vector3(FaceSize, 0.06f, FaceSize);
        }

        public void UpdateCount()
        {
            if (_counter != null)
                _counter.text = _model.Count.ToString();
        }

        /// <summary>
        /// PERDE KALKMA ANIMASYONU (4. tur, I36).
        ///
        /// Kullanıcı: "Perde kalkma animasyonu ve blokların geliş şekli birebir
        /// yapılacak."
        ///
        /// Eskiden `Destroy(gameObject)` idi: perde bir karede yok oluyor ve
        /// altındaki bloklar aynı karede beliriyordu. Ekranda görülen tek şey
        /// "renkler değişti" oluyordu; hangi perdenin açıldığı, altından ne
        /// çıktığı okunmuyordu.
        ///
        /// Referansta perde YUKARI KALKIYOR ve saydamlaşıyor — bir kapak gibi.
        /// Burada üç şey birlikte yürüyor: yükselme, büyüme ve sönme. Üçünün
        /// aynı eğriyi paylaşması hareketi tek bir olay gibi okutuyor.
        ///
        /// DERS (yok etmeden ÖNCE anlat): Bir nesneyi silmek bedava; ama
        /// oyuncunun o silmeyi FARK ETMESİ için nesnenin gidişini görmesi
        /// gerekir. Yarım saniye, bir bulmacada altın değerinde bir yarım
        /// saniyedir.
        /// </summary>
        public void Open()
        {
            if (!Application.isPlaying) { DestroyImmediate(gameObject); return; }
            StartCoroutine(OpenRoutine());
        }

        System.Collections.IEnumerator OpenRoutine()
        {
            // Sayaç ve rozet perdeyle birlikte gitmeli; rozet ayrı bir kökün
            // çocuğu olduğu için elle kapatılıyor.
            if (_counter != null) _counter.gameObject.SetActive(false);

            var renderers = GetComponentsInChildren<MeshRenderer>();
            var materials = new System.Collections.Generic.List<Material>(renderers.Length);
            foreach (var renderer in renderers)
            {
                // Kendi kopyası: paylaşılan materyali söndürmek AYNI renkteki
                // bütün perdeleri söndürürdü.
                var fading = ViewKit.Translucent(ReadColor(renderer.sharedMaterial));
                renderer.sharedMaterial = fading;
                materials.Add(fading);
            }

            Vector3 home = transform.position;
            const float duration = 0.42f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / duration);
                float eased = k * k;                       // hızlanarak kalkar

                transform.position = home + Vector3.up * (eased * 1.6f);
                transform.localScale = Vector3.one * (1f + eased * 0.14f);

                for (int i = 0; i < materials.Count; i++)
                {
                    var color = ReadColor(materials[i]);
                    color.a = 1f - eased;
                    Paint(materials[i], color);
                }
                yield return null;
            }

            foreach (var material in materials) if (material != null) Destroy(material);
            Destroy(gameObject);
        }

        static Color ReadColor(Material material)
        {
            if (material == null) return Color.white;
            if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
            if (material.HasProperty("_Color")) return material.color;
            return Color.white;
        }

        static void Paint(Material material, Color color)
        {
            if (material == null) return;
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        }
    }
}
