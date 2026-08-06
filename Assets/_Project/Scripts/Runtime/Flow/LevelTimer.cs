using System;

namespace BlockOut.Runtime.Flow
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
