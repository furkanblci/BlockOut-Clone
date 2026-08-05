using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Services
{
    /// <summary>
    /// Analitik sağlayıcısı. GameAnalytics, Firebase, Adjust... hepsi bunu uygular.
    ///
    /// DERS (neden kendi arayüzün olmalı?): Sektörde analitik ve reklam SDK'ları
    /// SIK DEĞİŞİR — ağ daha iyi ödeme yapan bir rakiple takas edilir, sağlayıcı
    /// API'sini kırar, yayıncı kendi SDK'sını dayatır. Oyun kodu doğrudan
    /// `GameAnalytics.NewDesignEvent(...)` çağırırsa bu çağrılar projeye yayılır
    /// ve sökülmesi haftalar alır. Araya kendi ince arayüzünü koyarsan değişecek
    /// tek yer o arayüzün UYGULAMASI olur.
    ///
    /// DERS (olay isimlendirme): Sonradan pişman olmamanın yolu olayları
    /// "isim + sözlük" olarak değil, KODLA ÇAĞRILAN metotlar olarak tanımlamaktır
    /// (<see cref="Analytics"/>). Böylece yazım hatası derleme hatası olur,
    /// panoda "level_complet" diye bir kayıp olay birikmez.
    /// </summary>
    public interface IAnalyticsProvider
    {
        void Initialize();
        void LogEvent(string name, IReadOnlyDictionary<string, object> parameters);
        void SetUserProperty(string key, string value);
    }

    /// <summary>Sağlayıcı bağlanana kadar olayları konsola basar.</summary>
    public sealed class DebugAnalyticsProvider : IAnalyticsProvider
    {
        public void Initialize() => Debug.Log("[Analytics] hata ayıklama sağlayıcısı aktif");

        public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                Debug.Log($"[Analytics] {name}");
                return;
            }

            var sb = new System.Text.StringBuilder(name).Append(" {");
            bool first = true;
            foreach (var pair in parameters)
            {
                if (!first) sb.Append(", ");
                sb.Append(pair.Key).Append('=').Append(pair.Value);
                first = false;
            }
            Debug.Log("[Analytics] " + sb.Append('}'));
        }

        public void SetUserProperty(string key, string value) =>
            Debug.Log($"[Analytics] kullanıcı özelliği {key}={value}");
    }

    /// <summary>
    /// Oyunun analitik girişi. Metotlar TİPLİ — yazım hatası derleme hatasıdır.
    ///
    /// DERS (hangi olaylar gerçekten lazım?): Casual bir oyunda ilk günden
    /// izlenmesi gereken çekirdek küme küçüktür: bölüm başladı / bitti /
    /// başarısız, para kazanıldı / harcandı, reklam gösterildi. Bunlar
    /// tutundurma (retention) ve zorluk eğrisini okumaya yeter. Yüzlerce olay
    /// eklemek analiz etmeyi zorlaştırır, kolaylaştırmaz.
    /// </summary>
    public static class Analytics
    {
        static IAnalyticsProvider _provider = new DebugAnalyticsProvider();
        static readonly Dictionary<string, object> Scratch = new Dictionary<string, object>(8);

        public static void SetProvider(IAnalyticsProvider provider)
        {
            _provider = provider ?? new DebugAnalyticsProvider();
            _provider.Initialize();
        }

        public static void LevelStarted(int levelIndex, int attempt)
        {
            Scratch.Clear();
            Scratch["level"] = levelIndex + 1;
            Scratch["attempt"] = attempt;
            _provider.LogEvent("level_start", Scratch);
        }

        public static void LevelCompleted(int levelIndex, int attempt, int secondsLeft, bool perfect)
        {
            Scratch.Clear();
            Scratch["level"] = levelIndex + 1;
            Scratch["attempt"] = attempt;
            Scratch["seconds_left"] = secondsLeft;
            Scratch["perfect"] = perfect;
            _provider.LogEvent("level_complete", Scratch);
        }

        public static void LevelFailed(int levelIndex, int attempt, string reason)
        {
            Scratch.Clear();
            Scratch["level"] = levelIndex + 1;
            Scratch["attempt"] = attempt;
            Scratch["reason"] = reason;
            _provider.LogEvent("level_fail", Scratch);
        }

        public static void CurrencyEarned(string currency, int amount, string source)
        {
            Scratch.Clear();
            Scratch["currency"] = currency;
            Scratch["amount"] = amount;
            Scratch["source"] = source;
            _provider.LogEvent("currency_earn", Scratch);
        }

        public static void CurrencySpent(string currency, int amount, string sink)
        {
            Scratch.Clear();
            Scratch["currency"] = currency;
            Scratch["amount"] = amount;
            Scratch["sink"] = sink;
            _provider.LogEvent("currency_spend", Scratch);
        }
    }
}
