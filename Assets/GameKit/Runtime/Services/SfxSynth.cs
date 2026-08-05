using UnityEngine;

namespace GameKit.Services
{
    /// <summary>
    /// Yer tutucu ses efektlerini çalışma anında sentezler — asset gerekmez.
    ///
    /// DERS (neden değerli?): Bir prototipte ses, en son gelen ve en çok gecikeni
    /// olur; oysa sessiz bir oyun "his" açısından değerlendirilemez. Birkaç satır
    /// matematikle üretilen yer tutucu sesler, oyunu ilk günden duyulabilir yapar.
    /// Gerçek ses tasarımı geldiğinde değişen tek şey klibin kaynağı olur —
    /// çalma, havuzlama ve ayar kodu aynen kalır.
    ///
    /// DERS (zarf = karakterin kendisi): Ham bir sinüs dalgası "biiip" diye
    /// duyulur ve rahatsız eder. Sesi tanınır kılan şey ZARFTIR: nasıl başlayıp
    /// nasıl söndüğü. Aşağıdaki üssel sönümler bu yüzden var.
    /// </summary>
    public static class SfxSynth
    {
        const int SampleRate = 44100;

        /// <summary>Kısa, düşen perdeli "pop" — bir şey alındı/yerleşti.</summary>
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

        /// <summary>Filtrelenmiş gürültü — kırılma, çatlama, patlama.</summary>
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

        /// <summary>Nota dizisi — kazanma, kaybetme, ödül açılışı.</summary>
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

        /// <summary>Arayüz tıklaması — çok kısa, yüksek, yumuşak.</summary>
        public static AudioClip Click(float frequency = 900f) => Pop(frequency, 0.05f);

        /// <summary>Para/ödül sesi — hızlı yükselen üçlü.</summary>
        public static AudioClip Coin() =>
            Arpeggio(new[] { 880f, 1174f, 1568f }, 0.05f);

        static AudioClip FromData(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
