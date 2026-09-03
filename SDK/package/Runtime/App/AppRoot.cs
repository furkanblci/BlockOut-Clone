using UnityEngine;

namespace GameKit.App
{
    /// <summary>
    /// Oynanış kökünün SDK'ya verdiği söz. <see cref="AppRoot"/> oyunun
    /// oturum sınıfını tanımaz; yalnız "bölümü başlat" ve "bölümü durdur"
    /// bilir.
    ///
    /// DERS (arayüz, kabuğu içeriğinden ayırır): Eskiden <c>AppRoot</c>
    /// doğrudan <c>GameSession</c> tutuyordu — yani tek sahnelik kabuk, o
    /// oyunun oynanışına çivilenmişti. Oysa kabuğun bildiği tek şey "bir yerde
    /// bir oynanış var ve ona başla/dur diyebiliyorum". Arayüz tam olarak bu
    /// kadarını yazıya döküyor.
    /// </summary>
    public interface IGameplayHost
    {
        void PlayLevel(int levelIndex);
        void StopLevel();
    }

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

        [SerializeField, Tooltip("Tahta, kamera ve oynanış oturumunun kökü.")]
        GameObject gameRoot;

        [SerializeField, Tooltip("IGameplayHost uygulayan bileşen (oyunun oturum sınıfı).")]
        MonoBehaviour gameplayHost;

        IGameplayHost _gameplay;

        /// <summary>Sahnede kurulu olan kök; AppRouter buradan geçer.</summary>
        public static AppRoot Current { get; private set; }

        /// <summary>Hiç kapanmayan servis kökü (ses, titreşim, efektler).</summary>
        public Transform PersistentRoot { get; private set; }

        /// <summary>Ses servisi — menüde de, oynanışta da AYNI örnek.</summary>
        public GameKit.Services.AudioService Audio { get; private set; }

        /// <summary>Titreşim servisi — menüde de, oynanışta da AYNI örnek.</summary>
        public GameKit.Services.Haptics Haptics { get; private set; }

        public bool InGame { get; private set; }

        /// <summary>
        /// Oyunun kendi geliştirici menüsünü kurması için kanca.
        ///
        /// DERS (SDK oyunun hata ayıklama menüsünü bilemez): Menünün İÇERİĞİ
        /// oyuna özeldir — "bölüme atla", "tüm renkleri ver". Ama KURULMA ANI
        /// SDK'ya aittir: uygulama açılışında kurulmalı, ilk bölüme girişte
        /// değil (aşağıdaki nedene bak). Bu yüzden zamanlama burada, içerik
        /// oyunda.
        /// </summary>
        public static System.Action DevMenuInstaller;

        void Awake()
        {
            Current = this;
            _gameplay = gameplayHost as IGameplayHost;
            if (gameplayHost != null && _gameplay == null)
                Debug.LogError($"[AppRoot] {gameplayHost.GetType().Name} IGameplayHost uygulamıyor.");

            var persistent = new GameObject("Persistent");
            persistent.transform.SetParent(transform, worldPositionStays: false);
            PersistentRoot = persistent.transform;

            // HATA KATI EN ÖNCE: kendisinden sonra kurulan hiçbir şeyin ayakta
            // olmasına ihtiyaç duymuyor ve onların hatalarını yakalayabilmesi
            // için önce kurulmuş olması gerekiyor (bkz. DeviceErrorOverlay).
            DeviceErrorOverlay.Create(PersistentRoot);

            // Ses ve titreşim UYGULAMA AÇILIŞINDA kurulur.
            //
            // DERS (menü de oyunun parçası): Bu ikisi eskiden oynanış
            // kurulumunda doğuyordu. Oynanış kökü kapalı başladığı için
            // `Start` hiç koşmuyor, yani oyuncu İLK BÖLÜME GİRENE KADAR sahnede
            // hiç AudioService olmuyordu: ana ekranın, mağazanın, ayarların
            // bütün düğmeleri SESSİZ basılıyordu (`UiButtonFeel.Clicked`
            // bağlanmamış oluyor) ve Ayarlar'daki "Sesler" anahtarı
            // uygulanacak bir örnek bulamadığı için sessizce hiçbir şey
            // yapmıyordu. Servis "oyun başlayınca değil, uygulama açılınca"
            // kurulmalı — menü kabuğu da oynanış kadar oyundur.
            Audio = GameKit.Services.AudioService.Create(PersistentRoot);
            Haptics = GameKit.Services.Haptics.Create(PersistentRoot);

            if (GameKit.Meta.MetaServices.Ready)
                GameKit.Services.SettingsBinder.Apply(
                    GameKit.Meta.MetaServices.Save.Data.Settings, Audio, Haptics);

            // HER düğme dokunuşu hafif bir tık versin — tek yerden.
            //
            // DERS (haptiği düğme düğme eklemek imkânsız): Oyunda onlarca
            // düğme var; her birine tek tek titreşim yazmak hem unutulur hem
            // de eklenen her yeni düğmede yeniden unutulur. `UiButtonFeel`
            // zaten HEPSİNİN üstünde (basma animasyonu ondan geliyor), yani
            // tek doğru bağlama noktası orası. Ses de aynı sebeple oradan
            // bağlanıyor.
            GameKit.UI.UiButtonFeel.Pressed =
                () => Haptics?.Play(GameKit.Services.HapticStrength.Light);

            // Menüyle açılırız; oynanış kökü ilk "Oyna"da uyanır.
            if (gameRoot != null) gameRoot.SetActive(false);
            if (menuRoot != null) menuRoot.SetActive(true);

            // Geliştirici menüsü UYGULAMA AÇILIŞINDA kurulur, bölüme girince
            // değil. Önce yalnız oynanış oturumu kuruyordu; sonuç olarak
            // "bölüme atla" aracını kullanmak için önce bir bölüme girmek
            // gerekiyordu — aracın işini yapmasını istediğin ilk an olan ana
            // ekranda yoktu. Yayın yapısında bu kanca hiç bağlanmaz.
            DevMenuInstaller?.Invoke();
        }

        /// <summary>
        /// Elle yapılmış arayüz düzeltmelerini uygular — BİR KARE SONRA.
        ///
        /// DERS (Start yetmez, çünkü Start'ların sırası tanımsızdır): Bu çağrı
        /// önce doğrudan `Start` içindeydi ve düzeltmeler hiç uygulanmadı;
        /// üstelik hata da vermedi, çünkü ortada hata yoktu — ekranlar o an
        /// henüz kurulmamıştı ve gezilecek çocuk yoktu. Unity `Start`'ları
        /// belirli bir sırayla çağırmaz; "benden sonra kurulan" bir şeyi
        /// beklemenin tek güvenli yolu bir kare geçirmek.
        ///
        /// Kayıtlı düzeltme yoksa (yayın hâli) `ApplyAll` ilk satırda dönüyor:
        /// tek seferlik, sıfıra yakın maliyet. (bkz. GameKit.UI.UiTweak)
        /// </summary>
        System.Collections.IEnumerator Start()
        {
            yield return null;
            GameKit.UI.UiTweak.ApplyAll();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public void ShowMenu()
        {
            InGame = false;
            _gameplay?.StopLevel();
            if (gameRoot != null) gameRoot.SetActive(false);
            if (menuRoot != null) menuRoot.SetActive(true);
        }

        public void PlayLevel(int levelIndex)
        {
            InGame = true;
            if (menuRoot != null) menuRoot.SetActive(false);
            if (gameRoot != null) gameRoot.SetActive(true);
            _gameplay?.PlayLevel(levelIndex);
        }
    }
}
