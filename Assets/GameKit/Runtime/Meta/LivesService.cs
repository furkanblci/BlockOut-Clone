using System;

namespace GameKit.Meta
{
    /// <summary>
    /// Can sisteminin kayıtta tuttuğu iki alan. Oyunun kayıt sınıfı bunu uygular;
    /// servis kaydın geri kalanını bilmez.
    /// </summary>
    public interface ILivesState
    {
        int Lives { get; set; }

        /// <summary>Bir sonraki canın dolacağı an (UTC, ISO-8601). Boş = sayaç yok.</summary>
        string NextLifeAtUtc { get; set; }
    }

    /// <summary>
    /// Can sayısı ve zamanla dolumu.
    ///
    /// DERS (asıl zorluk: oyun kapalıyken geçen zaman): Bir sayacı her karede
    /// azaltmak kolaydır ama oyuncu uygulamayı kapattığında o sayaç durur — oysa
    /// canlar dolmaya devam etmeli. Çözüm sayacı değil, BİR SONRAKİ CANIN
    /// DOLACAĞI ANI saklamaktır. Uygulama açıldığında "şu an o anı geçmiş mi, kaç
    /// tane geçmiş" diye sorarız; 5 saat kapalı kaldıysa tek hesapta 5 canın
    /// hepsi verilir.
    ///
    /// DERS (saat kurcalama): Cihaz saati oyuncunun elindedir. Tam koruma sunucu
    /// ister. İstemcide yapabileceğimiz şey saatin GERİ alındığını fark etmektir:
    /// "bir sonraki can" anı şu andan bir dolum süresinden fazla ilerideyse saat
    /// oynanmıştır — sayacı bugüne çekeriz. İleri almayı engelleyemeyiz, ama en
    /// azından geri alarak durumu bozmasına izin vermeyiz.
    /// </summary>
    public sealed class LivesService
    {
        readonly ILivesState _state;
        readonly Action _persist;
        readonly Func<DateTime> _utcNow;

        public int MaxLives { get; }
        public TimeSpan RefillInterval { get; }

        public event Action<int> Changed;

        public LivesService(ILivesState state, Action persist, int maxLives,
            TimeSpan refillInterval, Func<DateTime> utcNow = null)
        {
            _state = state;
            _persist = persist ?? (() => { });
            MaxLives = Math.Max(1, maxLives);
            RefillInterval = refillInterval <= TimeSpan.Zero ? TimeSpan.FromMinutes(30) : refillInterval;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);

            // İlk açılışta can dolu başlar (kayıtta negatif duruyorsa).
            if (_state.Lives < 0)
            {
                _state.Lives = MaxLives;
                _state.NextLifeAtUtc = "";
            }
        }

        public int Current => Math.Min(_state.Lives, MaxLives);
        public bool IsFull => Current >= MaxLives;
        public bool HasLife => Current > 0;

        /// <summary>Bir sonraki cana kalan süre; can doluysa TimeSpan.Zero.</summary>
        public TimeSpan TimeToNextLife
        {
            get
            {
                if (IsFull) return TimeSpan.Zero;
                var next = ParseUtc(_state.NextLifeAtUtc);
                if (next == null) return RefillInterval;
                var remaining = next.Value - _utcNow();
                return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
            }
        }

        /// <summary>
        /// Geçen zamanı cana çevirir. Uygulama açılışında, öne geldiğinde ve
        /// ekranda geri sayım gösterilirken periyodik çağrılmalı.
        /// </summary>
        public void Refresh()
        {
            if (_state.Lives >= MaxLives)
            {
                if (!string.IsNullOrEmpty(_state.NextLifeAtUtc))
                {
                    _state.NextLifeAtUtc = "";
                    _persist();
                }
                return;
            }

            var now = _utcNow();
            var next = ParseUtc(_state.NextLifeAtUtc);

            // Sayaç hiç kurulmamış (ya da bozuk): şimdiden başlat.
            if (next == null)
            {
                _state.NextLifeAtUtc = FormatUtc(now + RefillInterval);
                _persist();
                Changed?.Invoke(Current);
                return;
            }

            // Saat geri alınmış: hedef, olabilecek en uzak andan da ileride.
            if (next.Value - now > RefillInterval)
            {
                _state.NextLifeAtUtc = FormatUtc(now + RefillInterval);
                _persist();
                Changed?.Invoke(Current);
                return;
            }

            if (now < next.Value) return;   // henüz dolmadı

            // Kaç tam aralık geçtiyse o kadar can. Kapalı geçen saatler de dahil.
            long elapsedTicks = (now - next.Value).Ticks;
            int granted = 1 + (int)(elapsedTicks / RefillInterval.Ticks);
            int before = _state.Lives;
            _state.Lives = Math.Min(MaxLives, _state.Lives + granted);

            _state.NextLifeAtUtc = _state.Lives >= MaxLives
                ? ""
                // Kalan artık süreyi ATMIYORUZ: sonraki can, son dolumun üstüne
                // bir aralık eklenerek hesaplanır; yoksa her açılışta oyuncu
                // birkaç saniye kaybederdi.
                : FormatUtc(next.Value + TimeSpan.FromTicks(RefillInterval.Ticks * granted));

            _persist();
            if (_state.Lives != before) Changed?.Invoke(Current);
        }

        /// <summary>Bir can harcar. Can yoksa false ve hiçbir şey değişmez.</summary>
        public bool TrySpend()
        {
            Refresh();
            if (_state.Lives <= 0) return false;

            bool wasFull = _state.Lives >= MaxLives;
            _state.Lives--;

            // Doluyken harcadıysak geri sayım ŞİMDİ başlar. Doluyken sayaç
            // işlemez; yoksa oyuncu hiç oynamadan can biriktirirdi.
            if (wasFull) _state.NextLifeAtUtc = FormatUtc(_utcNow() + RefillInterval);

            _persist();
            Changed?.Invoke(Current);
            return true;
        }

        /// <summary>Can ekler (ödül, reklam, satın alma). Üst sınırı aşmaz.</summary>
        public void Grant(int amount)
        {
            if (amount <= 0) return;
            int before = _state.Lives;
            _state.Lives = Math.Min(MaxLives, _state.Lives + amount);
            if (_state.Lives >= MaxLives) _state.NextLifeAtUtc = "";
            _persist();
            if (_state.Lives != before) Changed?.Invoke(Current);
        }

        static DateTime? ParseUtc(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            if (!DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal |
                    System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
                return null;
            return parsed;
        }

        static string FormatUtc(DateTime utc) =>
            utc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
    }
}
