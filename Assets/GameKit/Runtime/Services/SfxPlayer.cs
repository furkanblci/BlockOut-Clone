using UnityEngine;
using UnityEngine.Pool;

namespace GameKit.Services
{
    /// <summary>
    /// Ses efekti çalar — havuzlu, çakışmalı, oyundan bağımsız.
    ///
    /// DERS (object pooling'in DOĞRU yeri): Aynı anda birden çok efekt çalabilmek
    /// için birden çok AudioSource gerekir; her ses için yenisini yaratıp yok
    /// etmek klasik bir GC hatasıdır. Kaynak ödünç alınır, ses bitince havuza
    /// döner. Unity'nin kendi <see cref="ObjectPool{T}"/> sınıfını kullanıyoruz —
    /// kendi havuzunu yazmak, bakımı olan ama katma değeri olmayan koddur.
    ///
    /// DERS (kit oyunu tanımaz): Eski hâli oyunun tahta olaylarına abone oluyor
    /// ve "blok emildi" gibi kavramlar biliyordu. Şimdi yalnızca "şu klibi çal"
    /// biliyor; hangi olayın hangi sesi çıkardığına oyun karar veriyor.
    ///
    /// DERS (üst sınır neden var?): Aynı anda 30 ses çalarsa hem kulak tırmalar
    /// hem mobil ses karıştırıcısı tıkanır. Havuz sınırı doğal bir kısıtlayıcı.
    /// </summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        const int MaxConcurrent = 8;

        ObjectPool<AudioSource> _sources;

        /// <summary>Oyuncunun "sesler" ayarı.</summary>
        public bool Muted { get; set; }

        /// <summary>Tüm efektlere uygulanan genel ses düzeyi.</summary>
        public float MasterVolume { get; set; } = 1f;

        /// <summary>
        /// Perde savrulması. Aynı klip arka arkaya tıpatıp aynı çalarsa kulak
        /// bunu "makine sesi" olarak algılar; küçük bir rastgelelik canlılık verir.
        /// </summary>
        public Vector2 PitchJitter { get; set; } = new Vector2(0.94f, 1.06f);

        public static SfxPlayer Create(Transform parent = null)
        {
            var go = new GameObject("Sfx");
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
            return go.AddComponent<SfxPlayer>();
        }

        void Awake()
        {
            _sources = new ObjectPool<AudioSource>(
                createFunc: CreateSource,
                actionOnGet: s => s.gameObject.SetActive(true),
                actionOnRelease: s => s.gameObject.SetActive(false),
                actionOnDestroy: s => Destroy(s.gameObject),
                collectionCheck: false,
                defaultCapacity: 4,
                maxSize: MaxConcurrent);
        }

        AudioSource CreateSource()
        {
            var go = new GameObject("Source");
            go.transform.SetParent(transform, worldPositionStays: false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D: bulmaca ekranı sabit, mesafe hissine gerek yok
            return source;
        }

        public void Play(AudioClip clip, float volume = 1f)
        {
            if (Muted || clip == null || MasterVolume <= 0f) return;

            var source = _sources.Get();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume * MasterVolume);
            source.pitch = Random.Range(PitchJitter.x, PitchJitter.y);
            source.Play();
            StartCoroutine(ReleaseWhenDone(source, clip.length / Mathf.Max(0.01f, source.pitch)));
        }

        System.Collections.IEnumerator ReleaseWhenDone(AudioSource source, float length)
        {
            yield return new WaitForSeconds(length + 0.05f);
            source.Stop();
            source.clip = null;   // klibi tutmak gereksiz referans demek
            _sources.Release(source);
        }
    }
}
