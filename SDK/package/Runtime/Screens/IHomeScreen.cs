namespace GameKit.Screens
{
    /// <summary>
    /// Oyunun ana ekranının, menü kabuğuna verdiği söz.
    ///
    /// DERS (kabuk ana ekranı TANIMAMALI): Menü kabuğu, günlük ödül paneli ve
    /// profil ekranı ana ekrana üç şey için dokunuyordu: kaydır, tazele, üstünde
    /// dur. Üçü de doğrudan <c>HomeScreen.Instance</c> yazıyordu — yani kitin en
    /// çok kullanılan üç parçası, o oyunun 1300 satırlık ana ekranına
    /// çivilenmişti. Buradaki iki metot, o üç çağrının GERÇEKTEN ihtiyaç duyduğu
    /// her şey.
    ///
    /// DERS (arayüzü ihtiyaçtan türet, sınıftan değil): Bu arayüz "bir ana ekran
    /// neler yapar" diye düşünülerek yazılmadı — çağrı yerleri sayılarak
    /// yazıldı. Öbür yol, ana ekranın bütün genel metotlarını arayüze kopyalamaya
    /// ve yeni oyunda hiç kullanılmayan sekiz metodu doldurmaya çıkardı.
    /// </summary>
    public interface IHomeScreen
    {
        /// <summary>Jeton, can ve ilerleme göstergelerini kayıttan yeniden okur.</summary>
        void Refresh();

        /// <summary>
        /// Ana ekranı kaydırır — menü kabuğu bir alt sayfaya geçerken arkadaki
        /// manzarayı birlikte götürmek için kullanır.
        /// </summary>
        void Slide(float x, float y, float seconds);
    }

    /// <summary>
    /// Sahnedeki ana ekranın adresi. Oyunun ana ekranı <c>Awake</c>'inde
    /// <see cref="Bind"/>, <c>OnDestroy</c>'unda <see cref="Unbind"/> çağırır.
    ///
    /// Bağlı değilse bütün çağrılar sessizce hiçbir şey yapar: kit, ana ekranı
    /// olmayan bir projede de (ör. yalnız mağaza ekranını denerken) çalışır.
    /// </summary>
    public static class Home
    {
        public static IHomeScreen Current { get; private set; }

        public static void Bind(IHomeScreen screen) => Current = screen;

        public static void Unbind(IHomeScreen screen)
        {
            if (ReferenceEquals(Current, screen)) Current = null;
        }
    }
}
