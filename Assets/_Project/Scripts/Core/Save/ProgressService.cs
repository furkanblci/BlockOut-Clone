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

        public void GrantCoins(int amount)
        {
            if (amount <= 0) return;
            _save.Data.Coins += amount;
            _save.Save();
            CoinsChanged?.Invoke(_save.Data.Coins);
        }
    }
}
