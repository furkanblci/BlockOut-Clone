using System;
using Newtonsoft.Json;

namespace BlockOut.Core.Save
{
    /// <summary>
    /// Kaydın nereye yazıldığını soyutlar.
    ///
    /// DERS (neden arayüz?): Dosya sistemi yavaş, platforma bağlı ve testte
    /// yan etkilidir. Servisi "bir metin oku / bir metin yaz" seviyesinde
    /// soyutlayınca aynı kod hem cihazda dosyaya, hem testte belleğe çalışır —
    /// ve bozuk kayıt senaryosunu test etmek tek satır olur.
    /// </summary>
    public interface ISaveStore
    {
        bool TryRead(out string text);
        bool TryReadBackup(out string text);
        void Write(string text);
        void Delete();
    }

    /// <summary>Kaydın nasıl yüklendiğinin sonucu — teşhis ve oyuncuya bilgi için.</summary>
    public enum SaveLoadOutcome
    {
        /// <summary>Kayıt yoktu; yeni oyuncu.</summary>
        New,
        /// <summary>Kayıt okundu.</summary>
        Loaded,
        /// <summary>Ana dosya bozuktu, yedekten kurtarıldı.</summary>
        RecoveredFromBackup,
        /// <summary>İkisi de bozuktu; sıfırdan başlandı.</summary>
        Corrupted,
        /// <summary>Kayıt daha yeni bir sürümden; DOKUNULMADI, salt okunur çalışılıyor.</summary>
        FromFuture
    }

    /// <summary>
    /// Oyuncu kaydını yükler, tutar ve yazar.
    ///
    /// DERS (kayıt bozulmasının iki sebebi): (1) yazma ortasında uygulamanın
    /// ölmesi — çözümü atomik yazma, ISaveStore'un işi; (2) şema değişikliği —
    /// çözümü sürümleme, SaveMigration'ın işi. Servis bu ikisini birleştirir ve
    /// ÜÇÜNCÜ bir güvenlik katmanı ekler: ne olursa olsun kullanılabilir bir
    /// SaveData döndürür. Oyun kayıt yüzünden asla açılmamazlık etmemeli.
    /// </summary>
    public sealed class SaveService
    {
        readonly ISaveStore _store;
        readonly Func<DateTime> _utcNow;

        public SaveData Data { get; private set; }
        public SaveLoadOutcome Outcome { get; private set; }

        /// <summary>Gelecekten gelen kayıt: diske YAZMAYI reddederiz.</summary>
        public bool ReadOnly => Outcome == SaveLoadOutcome.FromFuture;

        /// <summary>Kayıt değiştiğinde tetiklenir (UI tazelemesi için).</summary>
        public event Action<SaveData> Changed;

        public SaveService(ISaveStore store, Func<DateTime> utcNow = null)
        {
            _store = store;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            Data = new SaveData();
        }

        public void Load()
        {
            if (TryParse(_store.TryRead, out var data, out var outcome))
            {
                Data = data;
                Outcome = outcome;
            }
            else if (TryParse(_store.TryReadBackup, out var backup, out var backupOutcome))
            {
                Data = backup;
                // Yedekten geldiyse "kurtarıldı" deriz; ama yedek de gelecekten
                // geliyorsa o bilgi daha önemli, onu koruruz.
                Outcome = backupOutcome == SaveLoadOutcome.FromFuture
                    ? SaveLoadOutcome.FromFuture
                    : SaveLoadOutcome.RecoveredFromBackup;
            }
            else
            {
                Data = new SaveData();
                Outcome = SaveLoadOutcome.Corrupted;
            }

            Changed?.Invoke(Data);
        }

        delegate bool ReadAttempt(out string text);

        bool TryParse(ReadAttempt read, out SaveData data, out SaveLoadOutcome outcome)
        {
            data = null;
            outcome = SaveLoadOutcome.New;

            if (!read(out string text) || string.IsNullOrWhiteSpace(text))
            {
                // Dosya yoksa bu bir HATA değil, yeni oyuncudur.
                data = new SaveData();
                outcome = SaveLoadOutcome.New;
                return true;
            }

            SaveData parsed;
            try { parsed = JsonConvert.DeserializeObject<SaveData>(text); }
            catch (JsonException) { return false; }
            if (parsed == null) return false;

            if (SaveMigration.IsFromFuture(parsed))
            {
                data = parsed;
                outcome = SaveLoadOutcome.FromFuture;
                return true;
            }

            SaveMigration.Upgrade(parsed);
            Normalize(parsed);
            data = parsed;
            outcome = SaveLoadOutcome.Loaded;
            return true;
        }

        /// <summary>
        /// Elle kurcalanmış ya da yarım kalmış kayıtları savunmaya alır: null
        /// koleksiyon, negatif para, anlamsız bölüm sırası...
        /// </summary>
        static void Normalize(SaveData data)
        {
            if (data.Levels == null) data.Levels = new System.Collections.Generic.Dictionary<string, LevelRecord>();
            if (data.Settings == null) data.Settings = new SettingsData();
            if (data.PlayerName == null) data.PlayerName = "";
            if (data.Coins < 0) data.Coins = 0;
            if (data.HighestUnlockedIndex < 0) data.HighestUnlockedIndex = 0;
        }

        /// <summary>Değişikliği diske yazar. Gelecekten gelen kayıtta hiçbir şey yapmaz.</summary>
        public void Save()
        {
            if (ReadOnly) return;

            Data.SavedAtUtc = SaveMigration.FormatUtc(_utcNow());
            _store.Write(JsonConvert.SerializeObject(Data, Formatting.Indented));
            Changed?.Invoke(Data);
        }

        /// <summary>Değiştir + yaz + haber ver. Çağıranların üç adımı unutmasını engeller.</summary>
        public void Mutate(Action<SaveData> change)
        {
            change(Data);
            Save();
        }

        /// <summary>Bölüm kaydını getirir; yoksa oluşturur.</summary>
        public LevelRecord RecordFor(string levelId)
        {
            if (!Data.Levels.TryGetValue(levelId, out var record))
            {
                record = new LevelRecord();
                Data.Levels[levelId] = record;
            }
            return record;
        }

        /// <summary>Kaydı siler ve sıfırlar (ayarlar menüsündeki "ilerlemeyi sıfırla").</summary>
        public void Reset()
        {
            _store.Delete();
            Data = new SaveData();
            Outcome = SaveLoadOutcome.New;
            Changed?.Invoke(Data);
        }
    }
}
