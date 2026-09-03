using System.Collections.Generic;
using System.IO;
using System.Text;
using GameKit.Services;
using UnityEngine;

namespace GameKit.Services
{
    /// <summary>
    /// Olayları CİHAZDA biriktiren analitik sağlayıcısı.
    ///
    /// DERS (analitik neyin cevabıdır): Analitiği "sonra lazım olur" diye
    /// eklemek işe yaramaz; bir soruya cevap vermesi gerekir. Buradaki soru
    /// tek: HANGİ BÖLÜM ÇOK ZOR? Bunu bilmeden 50 bölümü dengelemek tahmin
    /// yürütmektir. Bu yüzden sağlayıcı ham olay listesi tutmuyor; bölüm bölüm
    /// deneme/geçme sayısını topluyor ve geçme oranını hesaplıyor.
    ///
    /// DERS (sunucu olmadan da analitik olur): Gerçek bir panoya ihtiyaç yok.
    /// Kendi cihazında ve test edenlerin cihazında biriken bu özet, bölüm
    /// dengelemesi için fazlasıyla yeterli — üstelik hiçbir gizlilik metni,
    /// izin ekranı ya da SDK gerektirmiyor. Firebase bağlandığında değişecek
    /// tek şey <see cref="IAnalyticsProvider"/>'ın uygulaması olacak.
    /// </summary>
    public sealed class LocalAnalytics : IAnalyticsProvider
    {
        [System.Serializable]
        public sealed class LevelStat
        {
            public int Level;
            public int Starts;
            public int Clears;
            public int Fails;
            public int AdContinues;

            /// <summary>Denemelerin yüzde kaçı geçmeyle bitti.</summary>
            public float ClearRate => Starts > 0 ? (float)Clears / Starts : 0f;
        }

        const string FileName = "analytics.json";

        readonly Dictionary<int, LevelStat> _levels = new Dictionary<int, LevelStat>();
        readonly Dictionary<string, int> _counters = new Dictionary<string, int>();
        string _path;
        bool _dirty;
        float _lastSave;

        public IReadOnlyDictionary<int, LevelStat> Levels => _levels;
        public IReadOnlyDictionary<string, int> Counters => _counters;

        public void Initialize()
        {
            _path = Path.Combine(Application.persistentDataPath, FileName);
            Load();
        }

        public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            Bump(name);

            int level = ReadInt(parameters, "level", 0);
            switch (name)
            {
                case "level_start":    Stat(level).Starts++; break;
                case "level_complete": Stat(level).Clears++; break;
                case "level_fail":     Stat(level).Fails++; break;
                case "rewarded_ad":
                    if (parameters != null &&
                        parameters.TryGetValue("placement", out var placement) &&
                        placement as string == "continue_level")
                        Bump("continue_offered");
                    break;
            }

            _dirty = true;

            // Her olayda diske yazmak mobilde pahalı; on saniyede bir yeter.
            // Uygulama kapanırken de bir kez yazılıyor (Flush).
            if (Time.realtimeSinceStartup - _lastSave > 10f) Save();
        }

        public void SetUserProperty(string key, string value) { /* özet için gereksiz */ }

        LevelStat Stat(int level)
        {
            if (_levels.TryGetValue(level, out var stat)) return stat;
            stat = new LevelStat { Level = level };
            _levels[level] = stat;
            return stat;
        }

        void Bump(string key) =>
            _counters[key] = _counters.TryGetValue(key, out int value) ? value + 1 : 1;

        static int ReadInt(IReadOnlyDictionary<string, object> parameters, string key, int fallback)
        {
            if (parameters == null || !parameters.TryGetValue(key, out var raw)) return fallback;
            return raw is int number ? number : fallback;
        }

        // ------------------------------------------------------------------ kalıcılık

        [System.Serializable]
        sealed class Payload
        {
            public List<LevelStat> levels = new List<LevelStat>();
            public List<string> counterKeys = new List<string>();
            public List<int> counterValues = new List<int>();
        }

        public void Save()
        {
            if (!_dirty || string.IsNullOrEmpty(_path)) return;
            _dirty = false;
            _lastSave = Time.realtimeSinceStartup;

            var payload = new Payload();
            foreach (var stat in _levels.Values) payload.levels.Add(stat);
            foreach (var pair in _counters)
            {
                payload.counterKeys.Add(pair.Key);
                payload.counterValues.Add(pair.Value);
            }

            try { File.WriteAllText(_path, JsonUtility.ToJson(payload)); }
            catch (System.Exception error) { Debug.LogWarning("[Analytics] yazılamadı: " + error.Message); }
        }

        void Load()
        {
            if (!File.Exists(_path)) return;
            try
            {
                var payload = JsonUtility.FromJson<Payload>(File.ReadAllText(_path));
                if (payload == null) return;

                foreach (var stat in payload.levels) _levels[stat.Level] = stat;
                for (int i = 0; i < payload.counterKeys.Count && i < payload.counterValues.Count; i++)
                    _counters[payload.counterKeys[i]] = payload.counterValues[i];
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("[Analytics] okunamadı: " + error.Message);
            }
        }

        public void Reset()
        {
            _levels.Clear();
            _counters.Clear();
            _dirty = true;
            Save();
        }

        /// <summary>Geliştirici menüsünün gösterdiği özet.</summary>
        public string Report(int topCount = 10)
        {
            if (_levels.Count == 0) return "No data yet. Play a few levels.";

            var ranked = new List<LevelStat>(_levels.Values);
            // En çok denenip en az geçilen bölüm en tepede: "burası tıkanıyor".
            ranked.Sort((a, b) =>
            {
                int byRate = a.ClearRate.CompareTo(b.ClearRate);
                return byRate != 0 ? byRate : b.Starts.CompareTo(a.Starts);
            });

            var text = new StringBuilder();
            text.Append("En zorlanılan bölümler\n");
            for (int i = 0; i < ranked.Count && i < topCount; i++)
            {
                var stat = ranked[i];
                if (stat.Starts == 0) continue;
                text.Append("  Level ").Append(stat.Level)
                    .Append(":  ").Append(stat.Clears).Append('/').Append(stat.Starts)
                    .Append("  (%").Append(Mathf.RoundToInt(stat.ClearRate * 100f)).Append(")\n");
            }

            text.Append("\nSayaçlar\n");
            foreach (var pair in _counters)
                text.Append("  ").Append(pair.Key).Append(": ").Append(pair.Value).Append('\n');

            return text.ToString();
        }
    }
}
