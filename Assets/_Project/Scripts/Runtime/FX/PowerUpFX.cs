using System.Collections;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using BlockOut.Runtime.View;
using UnityEngine;

namespace BlockOut.Runtime.FX
{
    /// <summary>
    /// Yardımcıların görsel karşılığı: roket izi, UFO ışını, dondurma perdesi.
    ///
    /// DERS (yardımcı PAHALI, o yüzden GÖSTERİŞLİ olmalı): Bu üç yardımcı ya
    /// jetonla alınıyor ya reklam izlenerek kazanılıyor. Oyuncu bedelini ödediği
    /// bir şeyin işe yaradığını GÖRMELİ. Blok sessizce kaybolursa "para boşa
    /// gitti" hissi doğar — oyuncu bir daha almaz. Efektin süresi kısa (yarım
    /// saniye) ama varlığı, o bedelin karşılığını verdiğini söyleyen şey.
    ///
    /// DERS (efekt oyunu BEKLETMEZ): Hepsi coroutine ve kendi başına akıyor;
    /// model tarafı hiçbir şey beklemiyor. Efekti oynanışın önüne geçirmek —
    /// "animasyon bitene kadar oynayamazsın" — casual oyunda en hızlı sıkma yolu.
    /// </summary>
    public static class PowerUpFX
    {
        /// <summary>
        /// Roket: hedefin üstüne yukarıdan alev izi bırakarak dalar, çarpar ve
        /// kıvılcım saçar.
        ///
        /// Yönü YUKARIDAN AŞAĞI: yatay gelen bir roket tahtadaki diğer blokların
        /// arkasından geçmek zorunda kalır ve yarısı görünmez.
        ///
        /// DERS (etki ANIN'da olur, yolda değil): İlk hâlde yalnız beyaz bir
        /// küp iniyordu; çarpma anında hiçbir şey olmuyordu, blok da ayrıca
        /// kendi kaybolma animasyonunu oynatıyordu. Sonuç "roket geldi" değil
        /// "ekranda bir şey geçti" gibi okunuyordu. Vuruşu okutan üç şey var:
        /// ARDINDA bıraktığı iz (nereden geldiği), ÇARPMADAKİ sarsıntı (ne
        /// kadar sert) ve saçılan kıvılcım (nereye vurduğu).
        /// </summary>
        public static void Rocket(Transform parent, Vector3 target, Color tint)
        {
            var rocket = ViewKit.CreateShape(PrimitiveType.Cube, "Shape");
            rocket.name = "RocketFX";
            rocket.transform.SetParent(parent, worldPositionStays: false);
            var renderer = rocket.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = View.ViewKit.FrostShard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            rocket.transform.localScale = new Vector3(0.22f, 0.55f, 0.22f);
            GameKit.FX.Juice.Run(Dive(rocket.transform, parent, target));
        }

        /// <summary>Roketin alevi ve kıvılcımı: ikonundaki turuncu-altın.</summary>
        static readonly Color Flame = new Color(1f, 0.62f, 0.16f);

        static Material _sparkMaterial;

        /// <summary>
        /// Kıvılcımların ORTAK materyali.
        ///
        /// DERS (materyal üreten yardımcıyı döngüde çağırma):
        /// <see cref="ViewKit.Translucent"/> her çağrıda YENİ bir materyal
        /// yaratıyor ve kimse onu silmiyor. Kıvılcım başına bir çağrı, tek bir
        /// roket için 15 materyal demekti; nesneler yok edilse bile materyaller
        /// bellekte kalırdı. Rengi sabit olan bir efekt için tek materyal
        /// yeter — ve renk gerekirse `MaterialPropertyBlock` ile verilir,
        /// yeni materyalle değil.
        /// </summary>
        static Material SparkMaterial
        {
            get
            {
                if (_sparkMaterial == null)
                {
                    _sparkMaterial = ViewKit.Translucent(Flame);
                    _sparkMaterial.name = "SparkFX";
                    _sparkMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                return _sparkMaterial;
            }
        }

        static IEnumerator Dive(Transform rocket, Transform parent, Vector3 target)
        {
            Vector3 start = target + Vector3.up * 7f;
            const float duration = 0.26f;
            float sinceTrail = 0f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (rocket == null) yield break;
                float k = t / duration;
                rocket.position = Vector3.Lerp(start, target, k * k);   // hızlanarak
                rocket.Rotate(0f, 720f * Time.deltaTime, 0f);

                // İz her karede değil, sabit ARALIKLARLA bırakılıyor: kare
                // hızına bağlarsak hızlı cihazda yoğun, yavaş cihazda seyrek
                // bir iz çıkar ve efekt donanıma göre değişir.
                sinceTrail += Time.deltaTime;
                if (sinceTrail >= 0.02f)
                {
                    sinceTrail = 0f;
                    Spark(parent, rocket.position, Vector3.up * Random.Range(0.4f, 1.1f),
                          Random.Range(0.14f, 0.24f), 0.32f, Flame);
                }
                yield return null;
            }

            if (rocket != null)
            {
                Vector3 hit = rocket.position;
                Object.Destroy(rocket.gameObject);

                // Çarpma: sarsıntı + yanlara saçılan kıvılcım çelengi.
                GameKit.FX.CameraShake.Add(0.34f);
                for (int i = 0; i < 14; i++)
                {
                    float angle = i * (Mathf.PI * 2f / 14f) + Random.Range(-0.2f, 0.2f);
                    var velocity = new Vector3(
                        Mathf.Cos(angle) * Random.Range(2.2f, 4.0f),
                        Random.Range(1.4f, 3.2f),
                        Mathf.Sin(angle) * Random.Range(2.2f, 4.0f));
                    Spark(parent, hit, velocity, Random.Range(0.10f, 0.20f), 0.42f, Flame);
                }
            }
        }

        /// <summary>
        /// Tek bir kıvılcım: verilen hızla fırlar, küçülerek kaybolur.
        ///
        /// DERS (neden ParticleSystem değil?): <see cref="FXService"/>'in
        /// kırıntı sistemi tahta olaylarına bağlı ve kendi rengini oradan
        /// alıyor; yardımcı efektleri için oraya ikinci bir giriş açmak, o
        /// sistemi iki farklı sahibi olan bir ortak kaynağa çevirirdi. On beş
        /// kısa ömürlü nesne bu bağımlılığa değmez.
        /// </summary>
        static void Spark(Transform parent, Vector3 at, Vector3 velocity,
                          float size, float life, Color color)
        {
            var spark = ViewKit.CreateShape(PrimitiveType.Cube, "Spark");
            spark.transform.SetParent(parent, worldPositionStays: false);
            spark.transform.position = at;
            spark.transform.localScale = Vector3.one * size;
            spark.transform.rotation = Random.rotation;

            var renderer = spark.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = SparkMaterial;
            if (color != Flame)
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                renderer.SetPropertyBlock(block);
            }
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            GameKit.FX.Juice.Run(FadeSpark(spark.transform, velocity, size, life));
        }

        static IEnumerator FadeSpark(Transform spark, Vector3 velocity,
                                     float size, float life)
        {
            for (float t = 0f; t < life; t += Time.deltaTime)
            {
                if (spark == null) yield break;
                float k = t / life;
                spark.position += velocity * Time.deltaTime;
                velocity += Vector3.down * (9f * Time.deltaTime);   // yerçekimi
                spark.localScale = Vector3.one * (size * (1f - k));
                yield return null;
            }
            if (spark != null) Object.Destroy(spark.gameObject);
        }

        /// <summary>
        /// UFO: tahtanın üstünde beliren geniş bir ışık sütunu.
        ///
        /// UFO bir RENGİN tamamını siliyor; tek bir bloğa değil tahtanın
        /// geneline ait bir olay. Bu yüzden efekt de tek noktada değil,
        /// silinen her bloğun üstünde aynı anda parlıyor.
        /// </summary>
        public static void Beam(Transform parent, Vector3 target, Color tint)
        {
            var beam = ViewKit.CreateShape(PrimitiveType.Cylinder, "Shape");
            beam.name = "BeamFX";
            beam.transform.SetParent(parent, worldPositionStays: false);
            var renderer = beam.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = View.ViewKit.Translucent(
                new Color(tint.r, tint.g, tint.b, 0.55f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            beam.transform.position = target + Vector3.up * 2.4f;
            GameKit.FX.Juice.Run(Collapse(beam.transform, renderer));

            // Işının TABANINDA dışa açılan bir halka: sütun tek başına
            // "yukarı" anlatıyor ama bloğun kendisine ne olduğunu anlatmıyor.
            // Yerde açılan halka, olayın o hücrede geçtiğini söylüyor.
            var ring = ViewKit.CreateShape(PrimitiveType.Cylinder, "BeamRing");
            ring.transform.SetParent(parent, worldPositionStays: false);
            ring.transform.position = target + Vector3.up * 0.05f;
            var ringRenderer = ring.GetComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = ViewKit.Translucent(
                new Color(tint.r, tint.g, tint.b, 0.45f));
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            GameKit.FX.Juice.Run(Expand(ring.transform, ringRenderer));
        }

        /// <summary>
        /// Yerde dışa açılıp sönen ince halka.
        ///
        /// DERS (`renderer.material` KOPYA çıkarır): <see cref="ViewKit.Translucent"/>
        /// zaten bu nesneye özel yeni bir materyal döndürüyor; üstüne
        /// `.material` demek ikinci bir kopya daha yaratır ve ilki hiç
        /// kullanılmadan bellekte kalır. Materyal zaten bize aitken
        /// `sharedMaterial` üzerinden yazmak doğrusu — ve iş bitince onu da
        /// elle silmek gerekiyor, nesne yok edilince materyal gitmiyor.
        /// </summary>
        static IEnumerator Expand(Transform ring, MeshRenderer renderer)
        {
            const float duration = 0.34f;
            var material = renderer.sharedMaterial;
            Color color = material.color;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (ring == null) yield break;
                float k = t / duration;
                float width = Mathf.Lerp(0.25f, 1.35f, k * (2f - k));   // hızlı açılır
                ring.localScale = new Vector3(width, 0.02f, width);

                color.a = 0.45f * (1f - k) * (1f - k);
                material.color = color;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                yield return null;
            }

            if (ring != null) Object.Destroy(ring.gameObject);
            if (material != null) Object.Destroy(material);
        }

        static IEnumerator Collapse(Transform beam, MeshRenderer renderer)
        {
            const float duration = 0.42f;
            // `Translucent` zaten bu ışına özel bir materyal verdi; `.material`
            // demek ikinci bir kopya çıkarırdı (bkz. Expand'daki ders).
            var material = renderer.sharedMaterial;
            Color color = material.color;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (beam == null) yield break;
                float k = t / duration;
                // Genişlik daralır, yükseklik korunur: ışın "emiyor" gibi okunur.
                float width = Mathf.Lerp(0.55f, 0.04f, k);
                beam.localScale = new Vector3(width, 2.4f, width);

                color.a = 0.55f * (1f - k);
                material.color = color;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                yield return null;
            }

            if (beam != null) Object.Destroy(beam.gameObject);
            if (material != null) Object.Destroy(material);
        }

        /// <summary>
        /// Çalar saat: ekranın kenarlarından içeri sönen buzlu parlama ve
        /// yavaşça düşen kar taneleri. Süre donduğu sürece durur, bittiğinde
        /// söner.
        ///
        /// Kenarlarda duruyor çünkü tahtayı kaplayan bir perde oyunu okumayı
        /// zorlaştırır. "Bir şey değişti" bilgisi çevreden de verilebilir.
        ///
        /// DERS (dört şerit ≠ vinyet): Önceki hâl dört düz panel koyuyordu ve
        /// kenarlar keskin bittiği için efekt "buz" değil "çerçeve" gibi
        /// okunuyordu. Sönüm eğrisi artık <see cref="UI.MenuSprites.FrostVignette"/>
        /// içinde referanstan ÖLÇÜLEREK pişiriliyor; burada tek bir gerdirilmiş
        /// görsel kalıyor. Dört nesne yerine bir nesne, üstelik doğru görünen.
        /// </summary>
        public static GameObject FreezeVignette(Transform parent)
        {
            var holder = new GameObject("FreezeVignette");
            holder.transform.SetParent(parent, worldPositionStays: false);

            var canvas = GameKit.UI.UiKit.CreateCanvas("FreezeCanvas");
            canvas.transform.SetParent(holder.transform, worldPositionStays: false);
            canvas.sortingOrder = 4;                   // HUD'ın ALTINDA

            // GÜVENLİ ALAN DEĞİL, TAM EKRAN: buzlanma ekranın fiziksel
            // kenarından başlar; çentiğin altında kesilirse kenar çizgi gibi
            // durur. Referansta parlama en üst piksele kadar gidiyor.
            var root = GameKit.UI.UiKit.CreatePanel("Frost", canvas.transform, Color.white);
            GameKit.UI.UiKit.Place(root, 0f, 0f, 1f, 1f);
            root.sprite = UI.MenuSprites.FrostVignette;
            root.type = UnityEngine.UI.Image.Type.Simple;
            root.color = new Color(1f, 1f, 1f, 0.65f);  // ölçüm: kenarda α≈0.62
            root.raycastTarget = false;

            var flakes = new GameObject("Flakes", typeof(RectTransform));
            flakes.transform.SetParent(root.transform, worldPositionStays: false);
            GameKit.UI.UiKit.Place(flakes.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            GameKit.FX.Juice.Run(DriftFlakes(flakes.GetComponent<RectTransform>()));

            return holder;
        }

        /// <summary>
        /// Buzlanma sürerken kenarlardan içeri süzülen kar taneleri.
        ///
        /// DERS (parçacık sistemine gerek yok): On kadar UI görselini elle
        /// hareket ettirmek, ekran uzayında çalışan bir ParticleSystem kurup
        /// onu Canvas sıralamasına sokmaktan hem ucuz hem basit. Parçacık
        /// sistemi binlerce parçacık için doğru araç; on tane için değil.
        /// </summary>
        static IEnumerator DriftFlakes(RectTransform holder)
        {
            const int Count = 12;
            var flakes = new RectTransform[Count];
            var speed = new float[Count];
            var sway = new float[Count];
            var phase = new float[Count];

            for (int i = 0; i < Count; i++)
            {
                var image = GameKit.UI.UiKit.CreatePanel(
                    "Flake", holder, new Color(0.78f, 0.95f, 1f, Random.Range(0.35f, 0.8f)));
                image.sprite = UI.MenuSprites.Snowflake;
                image.raycastTarget = false;

                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                float size = Random.Range(14f, 30f);
                rect.sizeDelta = new Vector2(size, size);

                // Kenarlara yakın doğuyorlar: orta alan oynanış alanı, oradaki
                // hareket bloklarla karışır.
                float side = Random.value < 0.5f ? -1f : 1f;
                rect.anchoredPosition = new Vector2(
                    side * Random.Range(320f, 520f), Random.Range(-800f, 900f));

                flakes[i] = rect;
                speed[i] = Random.Range(28f, 62f);
                sway[i] = Random.Range(10f, 26f);
                phase[i] = Random.Range(0f, Mathf.PI * 2f);
            }

            while (holder != null)
            {
                float dt = Time.unscaledDeltaTime;   // sayaç donuk, efekt akıyor
                for (int i = 0; i < Count; i++)
                {
                    var rect = flakes[i];
                    if (rect == null) continue;

                    phase[i] += dt * 1.4f;
                    Vector2 at = rect.anchoredPosition;
                    at.y -= speed[i] * dt;
                    at.x += Mathf.Cos(phase[i]) * sway[i] * dt;
                    if (at.y < -900f) at.y = 900f;    // alttan çıkan üstten girer
                    rect.anchoredPosition = at;
                    rect.Rotate(0f, 0f, 22f * dt);
                }
                yield return null;
            }
        }
    }
}
