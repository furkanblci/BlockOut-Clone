using System;
using GameKit.Meta;
using GameKit.Save;

namespace GameKit.Meta
{
    /// <summary>Günlük ödülün bir günü.</summary>
    public readonly struct DailyGift
    {
        public readonly int Day;
        public readonly int Coins;
        public readonly int Lives;
        public readonly string PowerUp;

        public DailyGift(int day, int coins, int lives = 0, string powerUp = null)
        {
            Day = day;
            Coins = coins;
            Lives = lives;
            PowerUp = powerUp;
        }
    }

    /// <summary>
    /// Günlük ödül: art arda giriş yapan oyuncuya yükselen hediyeler.
    ///
    /// DERS (elde tutmanın en ucuz aracı): Yeni bir bölüm yapmak saatler alır ve
    /// oyuncuyu bir kez oyalar. Günlük ödül bir günlük iş ve oyuncuya YARIN
    /// dönmek için sebep verir — üstelik oyunun kendisine hiç dokunmadan.
    /// Casual oyunların neredeyse tamamında bu yüzden var.
    ///
    /// DERS (ZİNCİR ödülü, toplam değil): Ödüller "kaç gün oynadın"a değil
    /// "kaç gün ÜST ÜSTE oynadın"a bakıyor. Bir gün atlayınca zincir başa
    /// dönüyor. Kaybedilecek bir şey olması, dönmek için tek başına sebeptir.
    ///
    /// DERS (tarih değil GÜN karşılaştırılır): İki zaman damgası arasındaki
    /// farka bakmak yanlış — 23:59'da alıp 00:01'de tekrar almak mümkün olurdu.
    /// Karşılaştırma TAKVİM GÜNÜ üzerinden yapılıyor.
    /// </summary>
    public sealed class DailyRewardService
    {
        /// <summary>
        /// Yedi günlük varsayılan zincir. Hediye edilen tüketilebilirler
        /// <see cref="GameKit.Meta.IGameHost.Consumables"/> listesinden
        /// SIRAYLA seçilir — 3., 6. ve 7. gün sırasıyla 1., 2. ve 3. öğeyi verir.
        ///
        /// DERS (varsayılan olsun ama son söz oyunun olsun): Burası eskiden
        /// oyunun güçlendirici enum'unu doğrudan adıyla yazıyordu, yani bu
        /// servis o oyuna aitti. Şimdi eğri (50→80→120→…→500) SDK'da, öğe
        /// seçimi oyunda. Farklı bir eğri isteyen oyun <see cref="Week"/>
        /// yerine kendi dizisini <see cref="SetWeek"/> ile verir.
        /// </summary>
        static DailyGift[] _week;
        static bool _builtWhileUnbound;

        public static DailyGift[] Week
        {
            get
            {
                // DERS (statik ilklendirmenin sırası GARANTİ DEĞİLDİR): Bu dizi
                // eskiden alan ilklendiricisiydi. Sınıfa ilk erişen kim olursa
                // diziyi O AN kuruyordu — ve bu, oyunun GameHost.Bind çağrısından
                // ÖNCE olabiliyor. Sonuç: hediyeler sessizce boş kalırdı, üstelik
                // hata da vermezdi. Tembel kurulum + "bağsız kurulduysa yeniden
                // kur" bayrağı, sıradan bağımsız hâle getiriyor.
                if (_week == null || (_builtWhileUnbound && GameHost.IsBound))
                {
                    _week = BuildDefaultWeek();
                    _builtWhileUnbound = !GameHost.IsBound;
                }
                return _week;
            }
        }

        static DailyGift[] BuildDefaultWeek()
        {
            var items = GameHost.Current.Consumables;
            string Pick(int i) => i < items.Count ? items[i].Id : null;

            return new[]
            {
                new DailyGift(1, 50),
                new DailyGift(2, 80),
                new DailyGift(3, 120, powerUp: Pick(0)),
                new DailyGift(4, 160),
                new DailyGift(5, 200, lives: 1),
                new DailyGift(6, 260, powerUp: Pick(1)),
                new DailyGift(7, 500, lives: 2, powerUp: Pick(2)),
            };
        }

        /// <summary>
        /// Zinciri oyuna göre değiştirir. <see cref="GameHost.Bind"/>
        /// çağrıldıktan SONRA çağrılmalı; parametresiz çağrı varsayılan eğriyi
        /// bağlı host'un öğeleriyle yeniden kurar.
        /// </summary>
        public static void SetWeek(DailyGift[] week = null)
        {
            _week = week ?? BuildDefaultWeek();
            _builtWhileUnbound = false;
        }

        readonly SaveService<SaveData> _save;
        readonly Func<DateTime> _now;

        public DailyRewardService(SaveService<SaveData> save, Func<DateTime> now = null)
        {
            _save = save;
            _now = now ?? (() => DateTime.UtcNow);
        }

        /// <summary>Bugün alınabilecek gün (1-7).</summary>
        public int PendingDay
        {
            get
            {
                var data = _save.Data;
                if (!Available) return Math.Clamp(data.DailyStreak, 1, Week.Length);

                bool broken = !IsYesterday(data.DailyLastClaimUtc);
                int next = broken ? 1 : data.DailyStreak + 1;
                return next > Week.Length ? 1 : next;      // hafta başa sarar
            }
        }

        /// <summary>Bugün ödül alınabilir mi?</summary>
        public bool Available => !IsToday(_save.Data.DailyLastClaimUtc);

        /// <summary>Zincirin şu anki uzunluğu (ekranda gösterilir).</summary>
        public int Streak => _save.Data.DailyStreak;

        /// <summary>Ödülü verir. Alınamıyorsa false döner.</summary>
        public bool Claim(ProgressService progress, GameKit.Meta.LivesService lives)
        {
            if (!Available) return false;

            int day = PendingDay;
            var gift = Week[day - 1];

            progress?.GrantCoins(gift.Coins);
            if (gift.Lives > 0) lives?.Grant(gift.Lives);
            if (!string.IsNullOrEmpty(gift.PowerUp) && progress != null)
                progress.SetPowerUpCount(gift.PowerUp, progress.PowerUpCount(gift.PowerUp) + 1);

            _save.Mutate(data =>
            {
                data.DailyStreak = day;
                data.DailyLastClaimUtc = SaveService<SaveData>.FormatUtc(_now());
            });

            GameKit.Services.Analytics.CurrencyEarned("coin", gift.Coins, "daily_" + day);
            return true;
        }

        bool IsToday(string stamp) => SameDay(stamp, _now());
        bool IsYesterday(string stamp) => SameDay(stamp, _now().AddDays(-1));

        static bool SameDay(string stamp, DateTime day)
        {
            var parsed = SaveService<SaveData>.ParseUtc(stamp);
            if (parsed == null) return false;
            return parsed.Value.Date == day.Date;
        }
    }
}
