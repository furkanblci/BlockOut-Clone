using System;
using UnityEngine;

namespace GameKit.Services
{
    /// <summary>Ödüllü reklamın sonucu — oyun buna göre ödül verir ya da vermez.</summary>
    public enum RewardedResult
    {
        /// <summary>Sonuna kadar izlendi; ödül HAK EDİLDİ.</summary>
        Completed,
        /// <summary>Oyuncu kapattı; ödül YOK.</summary>
        Skipped,
        /// <summary>Gösterilecek reklam yoktu ya da ağ hatası.</summary>
        Unavailable
    }

    /// <summary>
    /// Reklam sağlayıcısı. AppLovin MAX, ironSource, AdMob... hepsi bunu uygular.
    ///
    /// DERS (neden arayüz?): Reklam ağı, oyunun ömrü boyunca değişme ihtimali EN
    /// YÜKSEK bağımlılıktır — daha iyi ödeme yapan ağa geçilir, yayıncı kendi
    /// aracısını dayatır, ağ API'sini kırar. Oyun kodu `MaxSdk.ShowRewardedAd()`
    /// çağırırsa bu çağrılar projeye yayılır. Araya kendi arayüzünü koyarsan
    /// geçiş, tek bir sınıfı yeniden yazmaya iner.
    ///
    /// DERS (ödüllü reklam SÖZLEŞMESİ): Ödül, geri çağrı `Completed` derse verilir.
    /// "Reklamı gösterdim, ödülü hemen vereyim" demek en sık yapılan hatadır;
    /// oyuncu reklamı kapatınca da ödül alır ve para kazanma modeli çöker.
    /// </summary>
    public interface IAdProvider
    {
        void Initialize();

        /// <summary>Şu an gösterilebilecek ödüllü reklam var mı?</summary>
        bool IsRewardedReady { get; }

        /// <summary>Ödüllü reklamı gösterir; sonucu geri çağrıyla bildirir.</summary>
        void ShowRewarded(string placement, Action<RewardedResult> onFinished);

        /// <summary>Araya giren tam ekran reklam (bölüm sonu gibi).</summary>
        void ShowInterstitial(string placement);
    }

    /// <summary>
    /// Gerçek ağ bağlanana kadar kullanılan sağlayıcı: her zaman "izlendi" der.
    ///
    /// Geliştirme sırasında ödül akışını test etmeyi mümkün kılar; yayına
    /// çıkmadan önce gerçek sağlayıcıyla değiştirilmesi ZORUNLUDUR.
    /// </summary>
    public sealed class NullAdProvider : IAdProvider
    {
        public void Initialize() => Debug.Log("[Ads] sahte sağlayıcı aktif (reklam yok, ödül hep verilir)");

        public bool IsRewardedReady => true;

        public void ShowRewarded(string placement, Action<RewardedResult> onFinished)
        {
            Debug.Log($"[Ads] ödüllü reklam ({placement}) — sahte: tamamlandı sayılıyor");
            onFinished?.Invoke(RewardedResult.Completed);
        }

        public void ShowInterstitial(string placement) =>
            Debug.Log($"[Ads] araya giren reklam ({placement}) — sahte");
    }

    /// <summary>
    /// Oyunun reklam girişi.
    ///
    /// DERS (araya giren reklamda sıklık sınırı): Bölüm sonlarında reklam
    /// göstermek standarttır ama her bölümde göstermek oyuncuyu kaçırır. Sıklık
    /// kuralı burada, TEK yerde durur; çağıran taraf "göstersem mi" diye
    /// düşünmez, sadece "uygun an geldi" der.
    /// </summary>
    public static class Ads
    {
        static IAdProvider _provider = new NullAdProvider();

        /// <summary>İki araya giren reklam arasında en az bu kadar saniye geçmeli.</summary>
        public static float InterstitialCooldownSeconds = 90f;

        static float _lastInterstitialTime = float.NegativeInfinity;

        public static void SetProvider(IAdProvider provider)
        {
            _provider = provider ?? new NullAdProvider();
            _provider.Initialize();
        }

        public static bool IsRewardedReady => _provider.IsRewardedReady;

        public static void ShowRewarded(string placement, Action<RewardedResult> onFinished)
        {
            if (!_provider.IsRewardedReady)
            {
                onFinished?.Invoke(RewardedResult.Unavailable);
                return;
            }
            _provider.ShowRewarded(placement, onFinished);
        }

        /// <summary>Uygun an geldi; sıklık kuralı elverirse gösterir. Gösterdiyse true.</summary>
        public static bool TryShowInterstitial(string placement)
        {
            if (Time.unscaledTime - _lastInterstitialTime < InterstitialCooldownSeconds)
                return false;

            _lastInterstitialTime = Time.unscaledTime;
            _provider.ShowInterstitial(placement);
            return true;
        }
    }
}
