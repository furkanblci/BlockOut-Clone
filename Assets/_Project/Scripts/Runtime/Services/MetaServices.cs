using System;
using BlockOut.Core.Save;
using UnityEngine;

namespace BlockOut.Runtime.Services
{
    /// <summary>
    /// Meta servislerinin tek erişim noktası: kayıt, ilerleme, can.
    ///
    /// DERS (neden statik bir erişim noktası?): Bu servisler oyunun TAMAMINDA
    /// tek örnek olmalı — iki SaveService aynı dosyaya yazsaydı biri diğerini
    /// ezerdi. Sahneler arası geçişte de yaşamalılar. Bunun Unity'deki üç yolu
    /// var: (a) her sahneye koyulan MonoBehaviour singleton — sahne sırasına
    /// bağımlı, kırılgan; (b) ScriptableObject — asset'e yazma riski;
    /// (c) `[RuntimeInitializeOnLoadMethod]` ile ilk kareden önce kurulan saf
    /// C# nesneleri. (c)'yi seçtik: sahneye bağlı değil, Play'e basıldığı anda
    /// hazır, test edilebilir.
    ///
    /// DERS (yine de statik = küresel durum): Kaçınılan şey servislerin
    /// KENDİLERİNİN statik olması. Sınıflar normal, bağımlılıkları kurucudan
    /// alıyor; yalnız bu BESTECİ statik. Böylece testte istediğin sahte depo ve
    /// sahte saatle kendi örneğini kurabiliyorsun.
    /// </summary>
    public static class MetaServices
    {
        public static SaveService Save { get; private set; }
        public static ProgressService Progress { get; private set; }
        public static LivesService Lives { get; private set; }

        public static bool Ready => Save != null;

        /// <summary>Videodaki üst bar: 5 can, 30 dakikada bir dolum.</summary>
        public const int MaxLives = 5;
        public static readonly TimeSpan RefillInterval = TimeSpan.FromMinutes(30);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            var store = new FileSaveStore();
            Compose(store);

            if (Save.Outcome == SaveLoadOutcome.RecoveredFromBackup)
                Debug.LogWarning("[Save] Ana kayıt bozuktu, yedekten kurtarıldı.");
            else if (Save.Outcome == SaveLoadOutcome.Corrupted)
                Debug.LogWarning("[Save] Kayıt okunamadı, sıfırdan başlanıyor.");
            else if (Save.Outcome == SaveLoadOutcome.FromFuture)
                Debug.LogWarning("[Save] Kayıt daha yeni bir sürümden — salt okunur çalışılıyor.");

            Debug.Log($"[Save] {store.FullPath} ({Save.Outcome}) — " +
                      $"can {Lives.Current}/{MaxLives}, coin {Progress.Coins}, " +
                      $"açık bölüm {Progress.HighestUnlockedIndex + 1}");
        }

        /// <summary>Testlerin ve editör araçlarının kendi deposunu verebilmesi için.</summary>
        public static void Compose(ISaveStore store, Func<DateTime> utcNow = null)
        {
            Save = new SaveService(store, utcNow);
            Save.Load();
            Progress = new ProgressService(Save);
            Lives = new LivesService(Save, MaxLives, RefillInterval, utcNow);
            Lives.Refresh();
        }
    }
}
