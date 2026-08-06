using UnityEngine;

namespace BlockOut.Runtime.Flow
{
    /// <summary>
    /// Tek sahnelik uygulamanın kökü: menü ile oynanış arasındaki geçişi
    /// SAHNE YÜKLEMEDEN yapar; iki kökü açıp kapatır.
    ///
    /// DERS (neden tek sahne?): Casual bulmaca oyunlarının sektör standardı
    /// küçük bir <c>Boot</c> + her şeyi barındıran tek bir <c>Main</c>
    /// sahnesidir. Sebebi pratik: mobilde sahne yüklemek gözle görülür bir
    /// takılma yaratır, oysa "Oyna"ya basınca oyun ANINDA başlamalıdır. Üstelik
    /// üst bar (jeton/can), ses ve titreşim servisleri ekranlar arasında
    /// ORTAKTIR; her sahnede yeniden kurmak hem israf hem de "ayarlar ekranı ses
    /// servisini bulamıyor" gibi sinsi hataların kaynağıdır.
    ///
    /// DERS (kalıcı kök): Servisler <see cref="PersistentRoot"/> altında yaşar
    /// ve hiç kapanmaz. Menü kökü kapanınca müzik susmaz, oyun kökü kapanınca
    /// kayıt servisi kaybolmaz.
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        [SerializeField, Tooltip("Ana ekran ve menü ekranlarının kökü.")]
        GameObject menuRoot;

        [SerializeField, Tooltip("Tahta, kamera ve GameSession'ın kökü.")]
        GameObject gameRoot;

        [SerializeField] GameSession session;

        /// <summary>Sahnede kurulu olan kök; AppRouter buradan geçer.</summary>
        public static AppRoot Current { get; private set; }

        /// <summary>Hiç kapanmayan servis kökü (ses, titreşim, efektler).</summary>
        public Transform PersistentRoot { get; private set; }

        public bool InGame { get; private set; }

        void Awake()
        {
            Current = this;

            var persistent = new GameObject("Persistent");
            persistent.transform.SetParent(transform, worldPositionStays: false);
            PersistentRoot = persistent.transform;

            // Menüyle açılırız; oynanış kökü ilk "Oyna"da uyanır.
            if (gameRoot != null) gameRoot.SetActive(false);
            if (menuRoot != null) menuRoot.SetActive(true);

            // Geliştirici menüsü UYGULAMA AÇILIŞINDA kurulur, bölüme girince
            // değil. Önce yalnız GameSession kuruyordu; sonuç olarak "bölüme
            // atla" aracını kullanmak için önce bir bölüme girmek gerekiyordu —
            // aracın işini yapmasını istediğin ilk an olan ana ekranda yoktu.
            // Yayın yapısında bu çağrı boş bir metoda gider.
            DevTools.DevMenu.Ensure();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public void ShowMenu()
        {
            InGame = false;
            if (session != null) session.StopLevel();
            if (gameRoot != null) gameRoot.SetActive(false);
            if (menuRoot != null) menuRoot.SetActive(true);
        }

        public void PlayLevel(int levelIndex)
        {
            InGame = true;
            if (menuRoot != null) menuRoot.SetActive(false);
            if (gameRoot != null) gameRoot.SetActive(true);
            if (session != null) session.PlayLevel(levelIndex);
        }
    }
}
