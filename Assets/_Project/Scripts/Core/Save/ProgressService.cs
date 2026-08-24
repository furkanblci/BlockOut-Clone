using System;
using GameKit.Save;

namespace BlockOut.Core.Save
{
    /// <summary>
    /// Bölüm ilerlemesi ve coin — bu oyunun kuralları.
    ///
    /// DERS (kural nerede yaşar?): "Bölüm bitince ne kadar coin verilir",
    /// "sonraki bölüm ne zaman açılır" gibi kurallar UI'da ya da GameSession'da
    /// dağınık durursa, ikinci bir giriş noktası (Journey haritasından tekrar
    /// oynama, bölüm sonu ekranı) eklendiğinde sessizce ayrışırlar. Tek servis =
    /// tek kural.
    ///
    /// Bu sınıf KİTE TAŞINMADI: ödül kademeleri ve "PERFECT" kavramı bu oyuna
    /// ait tasarım kararları. Kite yalnızca kayıt yönetimi ve can dolumu gibi
    /// her oyunda AYNI olan şeyler gidiyor.
    /// </summary>
    public sealed class ProgressService
    {
        readonly SaveService<SaveData> _save;

        /// <summary>Bölüm bitirme ödülü — videodaki PERFECT ekranında görülen değerler.</summary>
        public int CoinsPerClear = 20;
        public int CoinsPerFirstClear = 50;
        public int CoinsPerPerfect = 100;

        public event Action<int> CoinsChanged;
        public event Action<int> UnlockedChanged;

        public ProgressService(SaveService<SaveData> save) => _save = save;

        public int Coins => _save.Data.Coins;

        /// <summary>Açılan en yüksek bölüm sırası (0 tabanlı).</summary>
        public int HighestUnlockedIndex => _save.Data.HighestUnlockedIndex;

        public bool IsUnlocked(int levelIndex) => levelIndex <= _save.Data.HighestUnlockedIndex;

        public LevelRecord Record(string levelId)
        {
            if (!_save.Data.Levels.TryGetValue(levelId, out var record))
            {
                record = new LevelRecord();
                _save.Data.Levels[levelId] = record;
            }
            return record;
        }

        /// <summary>
        /// Kaydı OKUR; yoksa <c>null</c> döner ve HİÇBİR ŞEY YARATMAZ.
        ///
        /// DERS (aynı tuzak iki kez): <see cref="Record"/> bulamadığını yaratıp
        /// kayda yazıyor. Liderlik ekranı puan hesaplarken bütün bölümler için
        /// onu çağırıyordu — yani her tazelemede kayıt dosyasına oynanmamış
        /// her bölüm için boş bir satır ekliyordu. Sadece OKUYACAK olan
        /// buradan geçmeli.
        /// </summary>
        public LevelRecord Peek(string levelId) =>
            !string.IsNullOrEmpty(levelId)
            && _save.Data.Levels.TryGetValue(levelId, out var record) ? record : null;

        /// <summary>
        /// Bu bölüm ŞU AN temizlenseydi kaç jeton verirdi? Hiçbir şeye
        /// dokunmaz. Kaybetme paneli bunu "kaçırdığın ödül" olarak gösteriyor.
        ///
        /// DERS (okuma kayıt OLUŞTURMAMALI): <see cref="Record"/> aradığını
        /// bulamazsa yenisini yaratıp kayda YAZIYOR. Sırf ödülü göstermek için
        /// onu çağırmak, oynanmamış her bölüm için kayıt dosyasında boş bir
        /// satır açardı — panel bir şeyi değiştirmeden okumalı.
        /// </summary>
        public int PreviewReward(string levelId)
        {
            bool cleared = !string.IsNullOrEmpty(levelId)
                        && _save.Data.Levels.TryGetValue(levelId, out var record)
                        && record.Cleared;
            return cleared ? CoinsPerClear : CoinsPerFirstClear;
        }

        /// <summary>Bölüme girildiğinde — deneme sayacı istatistik ve zorluk ayarı için.</summary>
        public void NoteAttempt(string levelId)
        {
            Record(levelId).Attempts++;
            _save.Save();
        }

        /// <summary>
        /// Bölüm bitti. Ödülü hesaplar, rekoru günceller, sonraki bölümü açar.
        /// Kazanılan coin döndürülür ki bitiş ekranı onu gösterebilsin.
        /// </summary>
        public int NoteCleared(string levelId, int levelIndex, int remainingSeconds, bool perfect)
        {
            var record = Record(levelId);
            bool firstClear = !record.Cleared;

            record.Cleared = true;
            if (remainingSeconds > record.BestRemainingSeconds)
                record.BestRemainingSeconds = remainingSeconds;
            if (perfect) record.Perfect = true;

            int reward = firstClear ? CoinsPerFirstClear : CoinsPerClear;
            if (perfect) reward = Math.Max(reward, CoinsPerPerfect);
            _save.Data.Coins += reward;

            int unlocked = _save.Data.HighestUnlockedIndex;
            if (levelIndex + 1 > unlocked) _save.Data.HighestUnlockedIndex = levelIndex + 1;

            _save.Save();

            CoinsChanged?.Invoke(_save.Data.Coins);
            if (_save.Data.HighestUnlockedIndex != unlocked)
                UnlockedChanged?.Invoke(_save.Data.HighestUnlockedIndex);
            return reward;
        }

        /// <summary>Coin harcar; yetmiyorsa false ve hiçbir şey değişmez.</summary>
        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || _save.Data.Coins < amount) return false;
            _save.Data.Coins -= amount;
            _save.Save();
            CoinsChanged?.Invoke(_save.Data.Coins);
            return true;
        }

        /// <summary>
        /// İlk denemede bitirilen bölüm sayısı — profildeki "İlk Denemede
        /// Kazanıldı" sayacı. Kayıtta ayrı bir alan tutmak yerine bölüm
        /// kayıtlarından türetilir: tek doğruluk kaynağı, senkron kaçağı yok.
        /// </summary>
        public int FirstTryClears
        {
            get
            {
                int count = 0;
                foreach (var record in _save.Data.Levels.Values)
                    if (record.Cleared && record.Attempts <= 1) count++;
                return count;
            }
        }

        /// <summary>Elde kaç adet var? (Yardımcı envanteri kayıtta yaşar.)</summary>
        public int PowerUpCount(string id) =>
            _save.Data.PowerUps.TryGetValue(id, out int n) ? n : 0;

        /// <summary>Envanteri yazar ve kaydeder; negatif değer sıfıra kırpılır.</summary>
        public void SetPowerUpCount(string id, int count)
        {
            _save.Data.PowerUps[id] = Math.Max(0, count);
            _save.Save();
        }

        public void GrantCoins(int amount)
        {
            if (amount <= 0) return;
            _save.Data.Coins += amount;
            _save.Save();
            CoinsChanged?.Invoke(_save.Data.Coins);
        }

        /// <summary>Mağaza paketi alındıysa reklam gösterilmez.</summary>
        public bool NoAds => _save.Data.NoAds;

        /// <summary>
        /// Sınırsız can hakkı sürüyor mu?
        ///
        /// DERS (hakkı BİTİŞ ANI olarak sakla, kalan süre olarak değil): Kalan
        /// süreyi saklarsan onu her açılışta azaltman gerekir ve oyun kapalıyken
        /// zaman durur — oyuncu 3 saatlik hakkı günlerce kullanır. Bitiş anı
        /// yazmak bu sınıf hatayı imkânsız kılar.
        /// </summary>
        public bool HasInfiniteLives => InfiniteLivesLeft > TimeSpan.Zero;

        /// <summary>Sınırsız can hakkından kalan süre; hak yoksa sıfır.</summary>
        public TimeSpan InfiniteLivesLeft
        {
            get
            {
                var text = _save.Data.InfiniteLivesUntilUtc;
                if (string.IsNullOrEmpty(text)) return TimeSpan.Zero;
                if (!DateTime.TryParse(text, null,
                        System.Globalization.DateTimeStyles.RoundtripKind, out var until))
                    return TimeSpan.Zero;

                var left = until - DateTime.UtcNow;
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Paketin verdiklerini işler. Süre EKLENİR: elinde 2 saat varken
        /// 6 saatlik paket alan oyuncu 8 saat almalı, 6 saate düşmemeli.
        /// </summary>
        public void GrantPackage(int coins, int infiniteLifeHours, bool noAds)
        {
            if (coins > 0) _save.Data.Coins += coins;
            if (noAds) _save.Data.NoAds = true;

            if (infiniteLifeHours > 0)
                UzatSinirsizCan(TimeSpan.FromHours(infiniteLifeHours));

            _save.Save();
            if (coins > 0) CoinsChanged?.Invoke(_save.Data.Coins);
        }

        /// <summary>
        /// Sınırsız can süresini uzatır. Süre EKLENİR (bkz. GrantPackage).
        ///
        /// SAAT DEĞİL TimeSpan (14. tur, J1): Yolculuk ödülleri arasında
        /// "30 dakika" var; `int` saat parametresiyle bu ifade edilemiyordu
        /// ve 30 dakikalık ödül sessizce 0 saat oluyordu.
        /// </summary>
        public void UzatSinirsizCan(TimeSpan sure)
        {
            if (sure <= TimeSpan.Zero) return;
            var from = DateTime.UtcNow + InfiniteLivesLeft;
            _save.Data.InfiniteLivesUntilUtc = (from + sure).ToString("o");
        }

        // ---- Arka plan ödülü (J1) -------------------------------------------

        /// <summary>Ana ekranda kullanılan arka plan; 0 = varsayılan manzara.</summary>
        public int Background => _save.Data.Background;

        /// <summary>Arka plan değişti — ana ekran ve kabuk bunu dinliyor.</summary>
        public event Action<int> BackgroundChanged;

        /// <summary>
        /// Bir bölgenin arka planı açık mı? Bölge TAMAMLANDIYSA açıktır.
        /// Ayrı bir liste tutulmuyor: tek kaynak ilerleme.
        /// </summary>
        public bool IsBackgroundUnlocked(int lastLevelOfRegion) =>
            HighestUnlockedIndex + 1 > lastLevelOfRegion;

        /// <summary>Arka planı seçer. Açık değilse hiçbir şey yapmaz.</summary>
        public bool SelectBackground(int index, int lastLevelOfRegion)
        {
            if (index != 0 && !IsBackgroundUnlocked(lastLevelOfRegion)) return false;
            if (_save.Data.Background == index) return false;

            _save.Data.Background = index;
            _save.Save();
            BackgroundChanged?.Invoke(index);
            return true;
        }

        // ---- Yolculuk kilometre taşları (J1) --------------------------------

        /// <summary>Bu seviyenin yolculuk ödülü daha önce alındı mı?</summary>
        public bool JourneyClaimed(int level) =>
            _save.Data.JourneyClaimed != null && _save.Data.JourneyClaimed.Contains(level);

        /// <summary>
        /// Yolculuk ödülünü verir ve BİR KEZ alındığını kaydeder.
        /// Zaten alınmışsa hiçbir şey yapmaz ve <c>false</c> döner.
        /// </summary>
        public bool ClaimJourney(int level, int coins, TimeSpan infiniteLives,
                                 string powerUpId, int powerUpCount)
        {
            if (JourneyClaimed(level)) return false;

            if (coins > 0)
            {
                _save.Data.Coins += coins;
                CoinsChanged?.Invoke(_save.Data.Coins);
            }
            UzatSinirsizCan(infiniteLives);
            if (!string.IsNullOrEmpty(powerUpId) && powerUpCount > 0)
                SetPowerUpCount(powerUpId, PowerUpCount(powerUpId) + powerUpCount);

            _save.Data.JourneyClaimed ??= new System.Collections.Generic.List<int>();
            _save.Data.JourneyClaimed.Add(level);
            _save.Save();
            return true;
        }
    }
}
