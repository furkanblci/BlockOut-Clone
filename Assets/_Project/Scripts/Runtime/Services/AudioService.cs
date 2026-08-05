using BlockOut.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace BlockOut.Runtime.Services
{
    /// <summary>
    /// Tahta olaylarını sese çevirir.
    ///
    /// DERS (object pooling — DOĞRU yeri): Aynı anda birden çok efekt çalabilmek
    /// için birden çok AudioSource gerekir; her ses için yenisini yaratıp yok
    /// etmek klasik bir GC hatasıdır. <see cref="ObjectPool{T}"/> tam buraya
    /// uyar: kaynak ödünç alınır, ses bitince havuza döner. Unity'nin kendi
    /// havuz sınıfını kullanıyoruz — kendi havuzunu yazmak, bakımı olan ama
    /// katma değeri olmayan koddur.
    ///
    /// DERS (ses asset'i olmadan ses): Klipler <see cref="SfxSynth"/> ile
    /// çalışma anında sentezlenir. Yer tutucu ses üretmenin en hızlı yolu budur;
    /// gerçek ses tasarımı geldiğinde yalnızca klip kaynağı değişir.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        const int MaxConcurrent = 8;

        ObjectPool<AudioSource> _sources;
        BoardEvents _events;

        AudioClip _absorb, _peel, _iceCrack, _curtain, _win, _lose;

        public bool Muted { get; set; }

        public static AudioService Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, worldPositionStays: false);
            return go.AddComponent<AudioService>();
        }

        void Awake()
        {
            _absorb = SfxSynth.Pop(660f, 0.16f);
            _peel = SfxSynth.Pop(440f, 0.12f);
            _iceCrack = SfxSynth.Noise(0.18f, 2600f);
            _curtain = SfxSynth.Arpeggio(new[] { 523f, 659f, 784f }, 0.09f);
            _win = SfxSynth.Arpeggio(new[] { 523f, 659f, 784f, 1046f }, 0.11f);
            _lose = SfxSynth.Arpeggio(new[] { 440f, 349f, 262f }, 0.16f);

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
            var go = new GameObject("Sfx");
            go.transform.SetParent(transform, worldPositionStays: false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D: bulmaca tahtası sabit, mesafe hissine gerek yok
            return source;
        }

        public void Bind(BoardEvents events)
        {
            Unbind();
            _events = events;
            _events.BlockAbsorbed += (b, g) => Play(_absorb, 0.55f);
            _events.LayerPeeled += (b, g) => Play(_peel, 0.45f);
            _events.IceShattered += b => Play(_iceCrack, 0.5f);
            _events.GateIceShattered += g => Play(_iceCrack, 0.5f);
            _events.CurtainOpened += c => Play(_curtain, 0.6f);
            _events.BoardCleared += () => Play(_win, 0.7f);
        }

        void Unbind() => _events = null;

        public void PlayLose() => Play(_lose, 0.6f);

        void Play(AudioClip clip, float volume)
        {
            if (Muted || clip == null) return;

            var source = _sources.Get();
            source.clip = clip;
            source.volume = volume;
            source.pitch = Random.Range(0.94f, 1.06f); // tekdüzelik kırılsın
            source.Play();
            StartCoroutine(ReleaseWhenDone(source, clip.length));
        }

        System.Collections.IEnumerator ReleaseWhenDone(AudioSource source, float length)
        {
            yield return new WaitForSeconds(length + 0.05f);
            source.Stop();
            _sources.Release(source);
        }
    }

    /// <summary>Yer tutucu ses efektlerini çalışma anında sentezler (asset gerekmez).</summary>
    public static class SfxSynth
    {
        const int SampleRate = 44100;

        /// <summary>Kısa, düşen perdeli "pop" — blok emilme sesi.</summary>
        public static AudioClip Pop(float frequency, float duration)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)count;
                float freq = Mathf.Lerp(frequency, frequency * 0.55f, progress);
                float envelope = Mathf.Exp(-6f * progress);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.6f;
            }
            return FromData("pop", data);
        }

        /// <summary>Filtrelenmiş gürültü — buz kırılma sesi.</summary>
        public static AudioClip Noise(float duration, float brightness)
        {
            int count = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[count];
            float previous = 0f;
            float smoothing = Mathf.Clamp01(brightness / SampleRate);

            for (int i = 0; i < count; i++)
            {
                float progress = i / (float)count;
                float white = Random.Range(-1f, 1f);
                previous = Mathf.Lerp(previous, white, smoothing * 12f);
                float envelope = Mathf.Exp(-9f * progress);
                data[i] = previous * envelope * 0.5f;
            }
            return FromData("noise", data);
        }

        /// <summary>Yükselen/alçalan nota dizisi — kazanma, kaybetme, perde açılışı.</summary>
        public static AudioClip Arpeggio(float[] frequencies, float noteDuration)
        {
            int perNote = Mathf.CeilToInt(SampleRate * noteDuration);
            var data = new float[perNote * frequencies.Length];

            for (int n = 0; n < frequencies.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = i / (float)SampleRate;
                    float progress = i / (float)perNote;
                    float envelope = Mathf.Sin(Mathf.PI * progress) * 0.55f;
                    data[n * perNote + i] =
                        Mathf.Sin(2f * Mathf.PI * frequencies[n] * t) * envelope;
                }
            }
            return FromData("arp", data);
        }

        static AudioClip FromData(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
