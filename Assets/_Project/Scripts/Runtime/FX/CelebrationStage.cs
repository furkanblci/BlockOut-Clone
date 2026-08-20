using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockOut.Runtime.FX
{
    /// <summary>
    /// Parçacık efektlerini ARAYÜZÜN İÇİNE taşıyan sahne.
    ///
    /// SORUN: Kutlama kanvası `ScreenSpaceOverlay`. Unity'de üst katman
    /// kanvaslar HER kameradan sonra çizilir — yani hiçbir `ParticleSystem`
    /// onların üstünde görünemez. Kutlamanın siyah perdesi de bir üst katman
    /// kanvas olduğu için, sahneye konan bir konfeti efekti perdenin
    /// ARKASINDA kalıp tamamen kaybolur.
    ///
    /// Denenip elenen üç yol:
    ///   • Kanvası `ScreenSpaceCamera` yapmak — o zaman HUD gibi öteki üst
    ///     katman kanvaslar kutlamanın üstüne çıkıyor.
    ///   • Kutlama sırasında öteki kanvasları gizlemek — akışa dokunuyor,
    ///     yan etkisi kestirilemez.
    ///   • Efekti dünyada bırakıp perdeyi saydamlaştırmak — perde zaten
    ///     efektin okunması için var.
    ///
    /// ÇÖZÜM: Efektler oyundan UZAKTA (bkz. <see cref="Origin"/>) kendi
    /// kameralarıyla bir <see cref="RenderTexture"/>'a çiziliyor; kutlama
    /// kanvasındaki bir `RawImage` o dokuyu gösteriyor. Böylece parçacıklar
    /// arayüz sırasına girmiş oluyor ve logonun ÖNÜNDE geçebiliyorlar —
    /// referansta da konfeti harflerin önünden geçiyor.
    ///
    /// DERS (uzak köşe, KATMANDAN ucuzdur): İlk tasarım efektlere ayrı bir
    /// katman verip oyun kamerasının maskesinden çıkarmaktı. O yol proje
    /// ayarlarını (TagManager) ve oyun kamerasını değiştirmeyi gerektiriyor;
    /// ikisi de bu sahneden habersiz başka yerlerde kurulan şeyler. Sahneyi
    /// oyunun görüş alanının dışına koymak aynı yalıtımı sıfır ayarla
    /// veriyor.
    /// </summary>
    public sealed class CelebrationStage : MonoBehaviour
    {
        /// <summary>
        /// Sahnenin dünyadaki yeri: oyun tahtası 0'ın çevresinde duruyor,
        /// burası ondan bir kilometre uzakta. Oyun kamerası buraya hiç
        /// bakmıyor, bizim kameramız da başka hiçbir şey görmüyor.
        /// </summary>
        static readonly Vector3 Origin = new Vector3(5000f, 5000f, 0f);

        /// <summary>
        /// Kameranın yarım yüksekliği (dünya birimi). Efektlerin ölçeği bu
        /// sayıya göre anlam kazanıyor: 5 birim = ekranın yarısı, yani
        /// 1 birim ≈ ekran yüksekliğinin %10'u.
        /// </summary>
        const float HalfHeight = 5f;

        /// <summary>
        /// Doku çözünürlüğünün ekrana oranı. Konfeti ve kıvılcım yumuşak
        /// kenarlı olduğu için yarım çözünürlük gözle ayırt edilmiyor ama
        /// dolgu maliyetini dörtte bire indiriyor — kutlama, telefonun en
        /// çok parçacık çizdiği an.
        /// </summary>
        const float TextureScale = 0.6f;

        Camera _camera;
        RenderTexture _texture;
        RawImage _surface;
        readonly List<GameObject> _live = new List<GameObject>();

        public RawImage Surface => _surface;

        /// <summary>
        /// <paramref name="parent"/> kutlama kanvasındaki bir dikdörtgen;
        /// yüzey onun ÇOCUĞU olarak kuruluyor, yani kardeş sırası
        /// parçacıkların logonun önünde mi arkasında mı olacağını belirliyor.
        /// </summary>
        public static CelebrationStage Create(RectTransform parent)
        {
            var holder = new GameObject("CelebrationStage");
            holder.transform.position = Origin;
            var stage = holder.AddComponent<CelebrationStage>();
            stage.Build(parent);
            return stage;
        }

        void Build(RectTransform parent)
        {
            int width = Mathf.Max(64, Mathf.RoundToInt(Screen.width * TextureScale));
            int height = Mathf.Max(64, Mathf.RoundToInt(Screen.height * TextureScale));

            // DERİNLİK TAMPONU ZORUNLU — "bedava optimizasyon" değilmiş.
            //
            // İlk sürüm dokuyu `depth: 0` ile kurdu: parçacıklar saydam ve
            // sıraya göre harmanlanıyor, derinlik testine ihtiyaçları yok.
            // Mantıklıydı ama URP'nin Render Graph'ı bunu KABUL ETMİYOR:
            //
            //   "the output Render Texture must have a depth buffer …
            //    the Depth Stencil Format property must be set to a value
            //    other than None"
            //
            // Sonuç bir karede otuz üç hata ve ekranda oyunun kendi hata
            // paneli. Kutlama çalışıyordu ama her karesinde boru hattı
            // kırılıyordu.
            //
            // DERS (bir kaynağın "gereksiz" parçasını kırpmadan önce ONU
            // KİMİN İSTEDİĞİNE bak): Derinlik tamponunu parçacıklar
            // kullanmıyor — ama dokuyu tüketen şey parçacıklar değil, render
            // hattı. Tasarrufu ölçmeden önce sözleşmeyi okumak gerekiyordu.
            _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "CelebrationRT",
                antiAliasing = 1
            };

            var camGo = new GameObject("StageCamera");
            camGo.transform.SetParent(transform, worldPositionStays: false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -40f);
            _camera = camGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = HalfHeight;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 200f;
            // SAYDAM TEMİZLEME: dokunun altında oyun görünecek, bu yüzden
            // zemin siyah değil BOŞ olmalı. Alfası sıfır bir renkle
            // temizlemek onu veriyor.
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.targetTexture = _texture;
            _camera.useOcclusionCulling = false;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;

            var go = new GameObject("Particles", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            _surface = go.AddComponent<RawImage>();
            _surface.texture = _texture;
            _surface.raycastTarget = false;
        }

        /// <summary>
        /// Efekti ekranın <paramref name="viewport"/> noktasında doğurur
        /// (0,0 sol alt — 1,1 sağ üst). <paramref name="scale"/> prefab'ın
        /// kendi boyunu çarpar.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector2 viewport, float scale = 1f,
                               float countScale = 1f, Color? tint = null,
                               string[] mute = null)
        {
            if (prefab == null) return null;

            var instance = Instantiate(prefab, transform);
            instance.transform.localPosition = ToLocal(viewport);
            instance.transform.localScale = Vector3.one * scale;

            // PARÇA SAYISI, BOYUTTAN AYRI AYARLANIYOR.
            //
            // `localScale` hem yayılımı hem parça boyunu çarpıyor. Referansın
            // konfetisi İNCE ama ÇOK: ekranın %5'ini kaplıyor, bizimki
            // %1'ini. Ölçeği beşe katlamak parçaları da beşe katlardı ve
            // konfeti kâğıt değil battaniye olurdu.
            //
            // DERS (yoğunluk ile boyut aynı düğmede olmasın — ikinci kez):
            // Aynı tuzağa yağmurun genişliğinde de düşülmüştü. Parçacık
            // sistemlerinde "daha çok" isteniyorsa aranan şey ölçek değil,
            // emisyon sayısı.
            if (!Mathf.Approximately(countScale, 1f))
            {
                foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var emission = ps.emission;
                    for (int i = 0; i < emission.burstCount; i++)
                    {
                        var burst = emission.GetBurst(i);
                        burst.count = Mathf.RoundToInt(burst.count.constantMax * countScale);
                        emission.SetBurst(i, burst);
                    }
                    var main = ps.main;
                    main.maxParticles = Mathf.RoundToInt(main.maxParticles * countScale);
                }
            }

            // YUMUŞAK PARÇACIK KAPALI OLMALI.
            //
            // Paketin 105 materyalinin 64'ünde "Soft Particles" açık ve o
            // özellik kamera derinlik dokusu istiyor. Bizim URP asset'inde
            // (`Mobile_RPAsset`) derinlik dokusu kapalı; açık kalan bir
            // materyal ekranda HİÇ görünmüyor. Paketin kendi belgesi de bu
            // durumu "invisible particles in URP" başlığıyla anlatıyor.
            //
            // DERS (bir varlığı içeri almak, onu ÇALIŞIR hâle getirmez):
            // Paket URP için hazırlanmış (372 materyalin hiçbirinde bozuk
            // shader yok) ama yine de projenin ayarlarıyla uyuşmayan bir
            // anahtar taşıyor. "Hazır paket" demek "senin projene hazır"
            // demek değil.
            foreach (var renderer in instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material != null) material.DisableKeyword("_SOFTPARTICLES_ON");
            }

            // İSTENMEYEN ALT SİSTEMLERİ SUSTURMAK.
            //
            // Paketin konfeti topu üç parçadan: kâğıtlar, `Clouds` (12 beyaz
            // duman yumağı) ve `Glow` (namlu parıltısı). Referansta konfeti
            // "görünmez bir yerden" fırlıyor — ağızda ne duman var ne parıltı.
            // Bizde alt kenarda kocaman beyaz yumaklar birikiyordu.
            //
            // Prefab'ı DEĞİŞTİRMEK yerine kopyada söndürülüyor: paket dosyası
            // olduğu gibi kalsın, bir sonraki sürüm sorunsuz gelsin.
            if (mute != null)
                foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
                    foreach (string name in mute)
                        if (ps.name == name) ps.gameObject.SetActive(false);

            // RENGİ DIŞARIDAN VERMEK.
            //
            // Paketin fişekleri renk renk (sarı, mavi, yeşil, mor, kırmızı)
            // ama referansın patlamaları ÖLÇÜLDÜ: ortalama (229,219,234),
            // doygunluk 0,13 — yani neredeyse beyaz, hafif leylak. Renkli
            // prefab'ları sırayla atmak ekranda "renkli lekeler" veriyordu,
            // referansın gümüş kıvılcım hissini değil.
            //
            // DERS (bir efektin RENGİ de ölçülebilir bir şeydir): "Havai
            // fişek renklidir" sezgisi doğru görünüyor ama bu oyunun
            // referansı öyle yapmamış — patlamalar beyaz, RENK konfetide.
            if (tint.HasValue)
            {
                foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = ps.main;
                    var start = main.startColor;
                    start.mode = ParticleSystemGradientMode.Color;
                    start.color = tint.Value;
                    main.startColor = start;
                }
            }

            _live.Add(instance);
            return instance;
        }

        /// <summary>Ekran oranını sahnenin yerel koordinatına çevirir.</summary>
        public Vector3 ToLocal(Vector2 viewport)
        {
            float aspect = _camera != null ? _camera.aspect : 0.5625f;
            return new Vector3(
                (viewport.x - 0.5f) * 2f * HalfHeight * aspect,
                (viewport.y - 0.5f) * 2f * HalfHeight,
                0f);
        }

        /// <summary>Doğmuş bütün efektleri siler (kutlama bitince).</summary>
        public void Clear()
        {
            foreach (var instance in _live)
                if (instance != null) Destroy(instance);
            _live.Clear();
        }

        /// <summary>
        /// Kamera ve doku kutlama BİTİNCE kapanıyor.
        ///
        /// Açık bırakmak, oyun boyunca her karede boş bir dokuya çizen
        /// ikinci bir kamera demekti — telefonun kare hızını kutlama
        /// olmadığı anlarda da düşüren, görünmez bir maliyet.
        /// </summary>
        public void SetActive(bool on)
        {
            if (_camera != null) _camera.enabled = on;
            if (_surface != null) _surface.enabled = on;
        }

        void OnDestroy()
        {
            Clear();
            if (_camera != null) _camera.targetTexture = null;
            if (_texture != null) _texture.Release();
        }
    }
}
