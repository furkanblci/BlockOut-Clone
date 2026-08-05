using System;
using Newtonsoft.Json;

namespace GameKit.Save
{
    /// <summary>
    /// Sürümlenebilir kayıt gövdesi. Oyunun kayıt sınıfı bunu uygular; kit
    /// yalnızca sürüm numarasını bilir, içindeki alanları değil.
    /// </summary>
    public interface IVersionedSave
    {
        int Version { get; set; }
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
        /// <summary>Kayıt daha yeni bir sürümden; DOKUNULMADI, salt okunur.</summary>
        FromFuture
    }

    /// <summary>
    /// Oyuncu kaydını yükler, tutar ve yazar.
    ///
    /// DERS (kayıt bozulmasının iki sebebi): (1) yazma ortasında uygulamanın
    /// ölmesi — çözümü atomik yazma, <see cref="FileSaveStore"/>'un işi;
    /// (2) şema değişikliği — çözümü sürümleme, oyunun göç kodunun işi. Servis
    /// bu ikisini birleştirir ve ÜÇÜNCÜ bir güvenlik katmanı ekler: ne olursa
    /// olsun kullanılabilir bir kayıt döndürür. Oyun kayıt yüzünden asla
    /// açılmamazlık etmemeli.
    ///
    /// DERS (neden jenerik?): Kayıt İÇERİĞİ her oyunda farklıdır ama YÖNETİMİ
    /// aynıdır. İçeriği tip parametresine çıkarınca kit hiçbir oyunun alanlarını
    /// bilmek zorunda kalmıyor.
    /// </summary>
    public sealed class SaveService<T> where T : class, IVersionedSave, new()
    {
        readonly ISaveStore _store;
        readonly Func<DateTime> _utcNow;
        readonly int _currentVersion;
        readonly Action<T> _upgrade;
        readonly Action<T> _normalize;

        public T Data { get; private set; }
        public SaveLoadOutcome Outcome { get; private set; }

        /// <summary>Gelecekten gelen kayıt: diske YAZMAYI reddederiz.</summary>
        public bool ReadOnly => Outcome == SaveLoadOutcome.FromFuture;

        public event Action<T> Changed;

        /// <param name="currentVersion">Bu yapımın anladığı en yüksek şema sürümü.</param>
        /// <param name="upgrade">Eski kaydı güncel şemaya taşır (oyunun göç kodu).</param>
        /// <param name="normalize">Kurcalanmış değerleri savunmaya alır (negatif para vb.).</param>
        public SaveService(ISaveStore store, int currentVersion,
            Action<T> upgrade = null, Action<T> normalize = null, Func<DateTime> utcNow = null)
        {
            _store = store;
            _currentVersion = currentVersion;
            _upgrade = upgrade;
            _normalize = normalize;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            Data = new T { Version = currentVersion };
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
                Data = new T { Version = _currentVersion };
                Outcome = SaveLoadOutcome.Corrupted;
            }

            Changed?.Invoke(Data);
        }

        delegate bool ReadAttempt(out string text);

        bool TryParse(ReadAttempt read, out T data, out SaveLoadOutcome outcome)
        {
            data = null;
            outcome = SaveLoadOutcome.New;

            if (!read(out string text) || string.IsNullOrWhiteSpace(text))
            {
                // Dosya yoksa bu bir HATA değil, yeni oyuncudur.
                data = new T { Version = _currentVersion };
                outcome = SaveLoadOutcome.New;
                return true;
            }

            T parsed;
            try { parsed = JsonConvert.DeserializeObject<T>(text); }
            catch (JsonException) { return false; }
            if (parsed == null) return false;

            if (parsed.Version > _currentVersion)
            {
                data = parsed;
                outcome = SaveLoadOutcome.FromFuture;
                return true;
            }

            _upgrade?.Invoke(parsed);
            _normalize?.Invoke(parsed);
            data = parsed;
            outcome = SaveLoadOutcome.Loaded;
            return true;
        }

        /// <summary>Değişikliği diske yazar. Gelecekten gelen kayıtta hiçbir şey yapmaz.</summary>
        public void Save()
        {
            if (ReadOnly) return;
            _store.Write(JsonConvert.SerializeObject(Data, Formatting.Indented));
            Changed?.Invoke(Data);
        }

        /// <summary>Değiştir + yaz + haber ver. Çağıranların üç adımı unutmasını engeller.</summary>
        public void Mutate(Action<T> change)
        {
            change(Data);
            Save();
        }

        /// <summary>Kaydı siler ve sıfırlar ("ilerlemeyi sıfırla" için).</summary>
        public void Reset()
        {
            _store.Delete();
            Data = new T { Version = _currentVersion };
            Outcome = SaveLoadOutcome.New;
            Changed?.Invoke(Data);
        }

        /// <summary>Şu anki UTC — zamana bağlı servisler (can dolumu) bunu paylaşır.</summary>
        public DateTime UtcNow => _utcNow();

        /// <summary>ISO-8601 UTC metnini okur; bozuksa null.</summary>
        public static DateTime? ParseUtc(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            if (!DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal |
                    System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
                return null;
            return parsed;
        }

        /// <summary>DateTime'ı kayıt formatına çevirir (UTC, ISO-8601, kültürden bağımsız).</summary>
        public static string FormatUtc(DateTime utc) =>
            utc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
    }
}
