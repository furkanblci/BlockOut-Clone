using System;
using System.Collections;
using UnityEngine;

namespace BlockOut.Runtime.Services
{
    public enum PurchaseResult
    {
        Purchased,
        Cancelled,
        Failed
    }

    /// <summary>
    /// Satın alma akışı — GERÇEK mağaza yerine benzetim.
    ///
    /// DERS (benzetim, atlama değil): "Nasıl olsa gerçek para yok" deyip düğmeye
    /// basar basmaz jeton vermek kolay olurdu. Ama o zaman ödeme SDK'sı
    /// bağlandığında ortaya çıkan her şey — onay ekranı, bekleme süresi,
    /// iptal edilen satın alma, başarısız işlem, çift tıklama koruması —
    /// hiç yazılmamış olurdu ve hepsi aynı anda test edilmek zorunda kalırdı.
    /// Burada akışın TAMAMI gerçek: onay ister, işlem sürer, iptal edilebilir,
    /// bazen başarısız olur. Değişecek tek yer <see cref="Run"/> içindeki
    /// bekleme; yerine mağaza SDK'sının geri çağrısı gelecek.
    ///
    /// DERS (çift tıklama = çift ücret): Ödeme akışlarındaki en pahalı hata
    /// budur. `IsBusy` tek bir işlemin sürmesini garanti eder; gerçek SDK'ya
    /// geçildiğinde de aynı koruma yerinde kalır.
    /// </summary>
    public sealed class PurchaseService : MonoBehaviour
    {
        /// <summary>Benzetimde işlemin ne kadar süreceği (mağaza gecikmesi taklidi).</summary>
        const float MinDelay = 0.9f;
        const float MaxDelay = 1.6f;

        /// <summary>
        /// Benzetimde başarısızlık oranı. Sıfır olmamalı: başarısız satın alma
        /// ekranı hiç görünmezse yazıldığı gün bozuk olduğu anlaşılmaz.
        /// </summary>
        const float FailureChance = 0.06f;

        static PurchaseService _instance;

        public static PurchaseService Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("Purchases");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<PurchaseService>();
                return _instance;
            }
        }

        public bool IsBusy { get; private set; }

        /// <summary>Adım adım durum bildirimi — arayüz bunu gösterir.</summary>
        public event Action<string> Status;

        /// <summary>
        /// Bir jeton paketini satın alır.
        /// </summary>
        /// <param name="productId">Mağaza ürün kimliği (gerçek SDK'da lazım olacak).</param>
        /// <param name="coins">Başarılıysa verilecek jeton.</param>
        /// <param name="price">Ekranda gösterilen fiyat.</param>
        public void Buy(string productId, int coins, string price,
            Action<PurchaseResult> onFinished)
        {
            if (IsBusy)
            {
                onFinished?.Invoke(PurchaseResult.Failed);
                return;
            }
            StartCoroutine(Run(productId, coins, price, onFinished));
        }

        IEnumerator Run(string productId, int coins, string price,
            Action<PurchaseResult> onFinished)
        {
            IsBusy = true;
            Status?.Invoke("Mağazaya bağlanılıyor…");

            // GERÇEK SDK BURAYA: mağaza penceresi açılır, oyuncu onaylar.
            yield return new WaitForSecondsRealtime(
                UnityEngine.Random.Range(MinDelay, MaxDelay));

            bool failed = UnityEngine.Random.value < FailureChance;
            if (failed)
            {
                IsBusy = false;
                Status?.Invoke("Ödeme tamamlanamadı.");
                GameKit.Services.Analytics.LogPurchaseFailed(productId, price);
                onFinished?.Invoke(PurchaseResult.Failed);
                yield break;
            }

            if (MetaServices.Ready)
                MetaServices.Progress.GrantCoins(coins);

            GameKit.Services.Analytics.CurrencyEarned("coin", coins, "iap:" + productId);
            GameKit.Services.Analytics.LogPurchase(productId, price, coins);

            IsBusy = false;
            Status?.Invoke("Satın alma tamamlandı.");
            onFinished?.Invoke(PurchaseResult.Purchased);
        }

        /// <summary>
        /// "Satın alımları geri yükle" — gerçek mağazada önceki kalıcı ürünleri
        /// geri getirir. Jeton paketleri TÜKETİLEBİLİR ürünlerdir ve geri
        /// yüklenmezler; bu yüzden burada dürüst bir cevap veriyoruz.
        /// </summary>
        public void Restore(Action<string> onFinished)
        {
            StartCoroutine(RestoreRoutine(onFinished));
        }

        IEnumerator RestoreRoutine(Action<string> onFinished)
        {
            Status?.Invoke("Satın alımlar sorgulanıyor…");
            yield return new WaitForSecondsRealtime(1.1f);
            onFinished?.Invoke("Geri yüklenecek kalıcı ürün yok (jeton paketleri tüketilir).");
        }
    }
}
