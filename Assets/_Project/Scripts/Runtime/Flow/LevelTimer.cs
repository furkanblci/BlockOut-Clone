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

        public event Action Expired;

        public void StartCountdown(int seconds)
        {
            Remaining = seconds;
            Running = seconds > 0;
        }

        public void Stop() => Running = false;

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
