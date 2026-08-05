using System;

namespace BlockOut.Core.Save
{
    /// <summary>
    /// Can sayısı ve zamanla dolumu.
    ///
    /// DERS (asıl zorluk: oyun kapalıyken geçen zaman): Bir sayacı her karede
    /// azaltmak kolaydır ama oyuncu uygulamayı kapattığında o sayaç durur —
    /// oysa canlar dolmaya devam etmeli. Çözüm sayacı değil, BİR SONRAKİ CANIN
    /// DOLACAĞI ANI saklamaktır. Uygulama açıldığında "şu an o anı geçmiş mi,
    /// kaç tane geçmiş" diye sorarız; 5 saat kapalı kaldıysa tek hesapta 5
    /// canın hepsi verilir.
    ///
    /// DERS (saat kurcalama): Cihaz saati oyuncunun elindedir. Saati ileri alıp
    /// can kasmak kolay olurdu. Tam koruma sunucu ister; istemcide yapabileceğimiz
    /// şey saatin GERİ alındığını fark etmektir: "bir sonraki can" anı, şu andan
    /// bir dolum süresinden fazla ilerideyse saat oynanmıştır — sayacı bugüne
    /// çekeriz. İleri almayı engelleyemeyiz, ama en azından geri alarak durumu
    /// bozmasına izin vermeyiz.
    /// </summary>
    public sealed class LivesService
    {
        readonly SaveService _save;
        readonly Func<DateTime> _utcNow;

        public int MaxLives { get; }
        public TimeSpan RefillInterval { get; }

        /// <summary>Can sayısı değiştiğinde tetiklenir.</summary>
        public event Action<int> Changed;

        public LivesService(SaveService save, int maxLives, TimeSpan refillInterval,
            Func<DateTime> utcNow = null)
        {
            _save = save;
            MaxLives = Math.Max(1, maxLives);
            RefillInterval = refillInterval <= TimeSpan.Zero ? TimeSpan.FromMinutes(30) : refillInterval;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);

            // İlk açılışta can dolu başlar (kayıtta -1 duruyorsa).
            if (_save.Data.Lives < 0)
            {
                _save.Data.Lives = MaxLives;
                _save.Data.NextLifeAtUtc = "";
            }
        }

        public int Current => Math.Min(_save.Data.Lives, MaxLives);
        public bool IsFull => Current >= MaxLives;
        public bool HasLife => Current > 0;

        /// <summary>Bir sonraki cana kalan süre; can doluysa TimeSpan.Zero.</summary>
        public TimeSpan TimeToNextLife
        {
            get
            {
                if (IsFull) return TimeSpan.Zero;
                var next = SaveMigration.ParseUtc(_save.Data.NextLifeAtUtc);
                if (next == null) return RefillInterval;
                var remaining = next.Value - _utcNow();
                return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
            }
        }

        /// <summary>
        /// Geçen zamanı cana çevirir. Uygulama açılışında ve öne geldiğinde
        /// çağrılmalı; ayrıca ekranda geri sayım gösterilirken periyodik olarak.
        /// </summary>
        public void Refresh()
        {
            var data = _save.Data;
            if (data.Lives >= MaxLives)
            {
                if (!string.IsNullOrEmpty(data.NextLifeAtUtc))
                {
                    data.NextLifeAtUtc = "";
                    _save.Save();
                }
                return;
            }

            var now = _utcNow();
            var next = SaveMigration.ParseUtc(data.NextLifeAtUtc);

            // Sayaç hiç kurulmamış (ya da bozuk): şimdiden başlat.
            if (next == null)
            {
                data.NextLifeAtUtc = SaveMigration.FormatUtc(now + RefillInterval);
                _save.Save();
                Changed?.Invoke(Current);
                return;
            }

            // Saat geri alınmış: hedef, olabilecek en uzak andan da ileride.
            if (next.Value - now > RefillInterval)
            {
                data.NextLifeAtUtc = SaveMigration.FormatUtc(now + RefillInterval);
                _save.Save();
                Changed?.Invoke(Current);
                return;
            }

            if (now < next.Value) return;   // henüz dolmadı

            // Kaç tam aralık geçtiyse o kadar can. Kapalı geçen saatler de dahil.
            long elapsedTicks = (now - next.Value).Ticks;
            int granted = 1 + (int)(elapsedTicks / RefillInterval.Ticks);
            int before = data.Lives;
            data.Lives = Math.Min(MaxLives, data.Lives + granted);

            data.NextLifeAtUtc = data.Lives >= MaxLives
                ? ""
                // Kalan artık süreyi ATMIYORUZ: sonraki can, son dolumun üstüne
                // bir aralık eklenerek hesaplanır; yoksa her açılışta oyuncu
                // birkaç saniye kaybederdi.
                : SaveMigration.FormatUtc(next.Value + TimeSpan.FromTicks(RefillInterval.Ticks * granted));

            _save.Save();
            if (data.Lives != before) Changed?.Invoke(Current);
        }

        /// <summary>Bir can harcar (bölüme girerken). Can yoksa false.</summary>
        public bool TrySpend()
        {
            Refresh();
            var data = _save.Data;
            if (data.Lives <= 0) return false;

            bool wasFull = data.Lives >= MaxLives;
            data.Lives--;

            // Doluyken harcadıysak geri sayım ŞİMDİ başlar. Doluyken sayaç
            // işlemez; yoksa oyuncu hiç oynamadan can biriktirirdi.
            if (wasFull) data.NextLifeAtUtc = SaveMigration.FormatUtc(_utcNow() + RefillInterval);

            _save.Save();
            Changed?.Invoke(Current);
            return true;
        }

        /// <summary>Can ekler (ödül, reklam, satın alma). Üst sınırı aşabilir mi: hayır.</summary>
        public void Grant(int amount)
        {
            if (amount <= 0) return;
            var data = _save.Data;
            int before = data.Lives;
            data.Lives = Math.Min(MaxLives, data.Lives + amount);
            if (data.Lives >= MaxLives) data.NextLifeAtUtc = "";
            _save.Save();
            if (data.Lives != before) Changed?.Invoke(Current);
        }
    }
}
