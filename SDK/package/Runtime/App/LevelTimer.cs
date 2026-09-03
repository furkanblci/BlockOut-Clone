using System;

namespace GameKit.App
{
    /// <summary>
    /// Geri sayım. Kendi Update'i yok — GameSession her kare Tick eder; böylece
    /// duraklatma (M5) "Tick çağırma" kadar basit olur ve sınıf testlenebilir kalır.
    /// </summary>
    public sealed class LevelTimer
    {
        public float Remaining { get; private set; }
        public bool Running { get; private set; }

        /// <summary>Bölüme verilen toplam süre — "ne kadarı kaldı" oranı için.</summary>
        public int Total { get; private set; }

        public event Action Expired;

        public void StartCountdown(int seconds)
        {
            Total = seconds;
            Remaining = seconds;
            Running = seconds > 0;
        }

        public void Stop() => Running = false;

        /// <summary>
        /// Süre ekler ve sayacı yeniden başlatır — "reklam izle, devam et".
        ///
        /// Toplam da artırılıyor: yıldız hesabı kalan/toplam oranına baktığı
        /// için, süre eklendiğinde toplamı sabit bırakmak oyuncuya hak
        /// etmediği bir PERFECT verirdi.
        /// </summary>
        public void AddTime(int seconds)
        {
            if (seconds <= 0) return;
            Remaining += seconds;
            Total += seconds;
            Running = true;
        }

        /// <summary>
        /// Kalan süreyi DOĞRUDAN yazar — yalnız geliştirici konsolu için.
        ///
        /// DERS (test kancası neden ayrı bir metot?): <see cref="AddTime"/>
        /// toplamı da büyütür (yıldız hesabı bozulmasın diye). Konsoldan
        /// "1 yıldızla kazandır" ya da "süreyi bitir" demek içinse toplamın
        /// SABİT kalması gerekir — yoksa üretilmek istenen sonucun kendisi
        /// kayar. İki farklı niyet, iki farklı metot.
        /// </summary>
        public void DebugSetRemaining(float seconds)
        {
            Remaining = (float)Math.Max(0.0, seconds);
            Running = Remaining > 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!Running) return;
            Remaining -= deltaTime;
            if (Remaining > 0f) return;

            Remaining = 0f;
            Running = false;
            Expired?.Invoke();
        }
    }
}
