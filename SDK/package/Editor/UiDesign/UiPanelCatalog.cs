using System;
using System.Collections.Generic;
using GameKit.Screens;
using UnityEngine;

namespace GameKit.Editor.UiDesign
{
    /// <summary>
    /// OYUNDAKİ BÜTÜN EKRAN VE PANELLERİN KAYIT DEFTERİ.
    ///
    /// NEDEN VAR: Bu projenin arayüzü kodla kuruluyor ve ekranların çoğu ancak
    /// oyun o noktaya geldiğinde doğuyor — sonuç kartını görmek için bölümü
    /// bitirmek, günlük ödülü görmek için günü değiştirmek gerekiyordu. Her
    /// panelin "beni oturum olmadan kur" girişini TEK BİR LİSTEDE toplamak,
    /// hepsini aynı anda, tek tıkla erişilebilir kılıyor.
    ///
    /// DERS (araç, aradığın şeyi ARAMAMALI): Bu girişlerin çoğu (
    /// <c>CreateResultPreview</c>, <c>CreateHudPreview</c>, ...) zaten aylardır
    /// koddaydı; her kullanışta elle tek seferlik bir düzenleyici komutu
    /// yazılıyordu. Yetenek vardı, ERİŞİM yoktu — ve bir yeteneğe her seferinde
    /// yeniden erişmek, o yeteneğin olmamasıyla neredeyse aynı maliyeti taşıyor.
    ///
    /// ANAHTAR MESELESİ (bu dosyanın en kritik yeri): Kaydedilen düzeltmeler
    /// hiyerarşi YOLUNA göre saklanıyor. Önizleme farklı adlı bir kök altında
    /// kurulduğu için, yolun çalışma anındakiyle aynı çıkması ancak DOĞRU
    /// ANAHTARI vermekle mümkün. Örnek: üst şerit çalışma anında
    /// <c>GameplayScreen/Hud/...</c> altında yaşıyor ama önizlemesi doğrudan
    /// kendi köküne kuruluyor — bu yüzden anahtarı "GameplayScreen/Hud".
    /// Anahtar yanlışsa araç hata vermez, sadece kaydettiğin hiçbir şey oyunda
    /// görünmez (bkz. <c>UiTweakRoot</c> içindeki aynı ders).
    /// </summary>
    public static class UiPanelCatalog
    {
        /// <summary>
        /// Bir artboard: adı, hangi grupta durduğu, nasıl kurulduğu.
        /// </summary>
        public sealed class Entry
        {
            /// <summary>Kalıcı kimlik — seçim bu kimlikle hatırlanıyor.</summary>
            public string Id;

            /// <summary>Sol listedeki başlık.</summary>
            public string Group;

            /// <summary>Görünen ad.</summary>
            public string Label;

            /// <summary>
            /// Düzeltme yollarının başına gelen anahtar. <c>null</c> ise panel
            /// kendini işaretliyor (kurulumunda <c>UiTweak.Mark</c> çağırıyor).
            /// </summary>
            public string Key;

            /// <summary>
            /// Paneli kurar. Parametre: kurulacağı kap. Dönüş: anahtarla
            /// işaretlenecek kök — panel kendini işaretliyorsa <c>null</c>.
            /// İkinci parametre varyant (aynı panelin farklı hâlleri).
            /// </summary>
            public Func<Transform, int, Transform> Build;

            /// <summary>
            /// Menü sayfası mı? Menü sayfaları çalışma anında sekme çubuğunun
            /// üstündeki 0,105–1 çerçevesine kuruluyor; önizleme de aynı
            /// çerçeveyi kurmazsa sayfa %10 uzun görünür ve ölçüler yalan söyler.
            /// </summary>
            public bool ContentFrame;

            /// <summary>Kaç farklı hâli var (varsayılan 1).</summary>
            public int Variants = 1;

            /// <summary>Varyant adları (boşsa "1, 2, 3…").</summary>
            public string[] VariantNames;

            /// <summary>Bu önizlemenin NEYİ göstermediği — dürüstlük notu.</summary>
            public string Note;
        }

        static List<Entry> _entries;

        /// <summary>
        /// Oyunun kendi ekranlarını deftere eklediği kanca. Bir düzenleyici
        /// başlangıç sınıfında doldurulur:
        ///
        /// <code>
        /// [InitializeOnLoadMethod]
        /// static void RegisterPanels() =&gt; UiPanelCatalog.GameEntries = list =&gt;
        /// {
        ///     list.Add(new UiPanelCatalog.Entry { Id = "gameplay.hud", ... });
        /// };
        /// </code>
        ///
        /// DERS (kite oyunun ekranları yazılamaz — ama yer AÇILABİLİR): Bu
        /// defter bir zamanlar oynanış ekranlarını da içeriyordu ve kitin
        /// düzenleyici tarafını o oyuna bağlayan tek şey oydu. Girişleri
        /// çıkarmak yetmezdi: araç, oyunun ekranlarını gösteremezse en çok
        /// ihtiyaç duyulan yerde işe yaramaz. Kanca ikisini de çözüyor —
        /// meta ekranlar hazır gelir, oynanış ekranlarını oyun ekler.
        /// </summary>
        public static Action<List<Entry>> GameEntries;

        public static IReadOnlyList<Entry> All => _entries ?? (_entries = BuildAll());

        /// <summary>Kanca sonradan bağlanırsa defteri yeniden kurdurur.</summary>
        public static void Invalidate() => _entries = null;

        static List<Entry> BuildAll()
        {
            var list = Build();
            GameEntries?.Invoke(list);
            return list;
        }

        public static Entry Get(string id)
        {
            foreach (var entry in All) if (entry.Id == id) return entry;
            return null;
        }

        static List<Entry> Build() => new List<Entry>
        {
            // -------------------------------------------------------- menü
            new Entry
            {
                Id = "menu.journey", Group = "Menü", Label = "Yolculuk",
                ContentFrame = true,
                Build = (parent, v) =>
                {
                    var root = JourneyScreen.Build(parent);
                    Refresh(() => root.GetComponent<JourneyScreen>().Refresh());
                    return null;
                }
            },
            new Entry
            {
                Id = "menu.store", Group = "Menü", Label = "Mağaza",
                ContentFrame = true,
                Build = (parent, v) =>
                {
                    var root = StoreScreen.Build(parent);
                    Refresh(() => root.GetComponent<StoreScreen>().RefreshCoins());
                    return null;
                }
            },
            new Entry
            {
                Id = "menu.settings", Group = "Menü", Label = "Ayarlar",
                ContentFrame = true,
                Build = (parent, v) =>
                {
                    var screen = SettingsScreen.Build(parent);
                    Refresh(() => screen.Refresh());
                    return null;
                }
            },
            new Entry
            {
                Id = "menu.profile", Group = "Menü", Label = "Profil",
                ContentFrame = true,
                Build = (parent, v) =>
                {
                    var root = ProfileScreen.Build(parent);
                    Refresh(() => root.GetComponent<ProfileScreen>().Refresh());
                    return null;
                }
            },
            new Entry
            {
                Id = "menu.collection", Group = "Menü", Label = "Koleksiyon",
                ContentFrame = true,
                Build = (parent, v) =>
                {
                    var root = CollectionScreen.Build(parent);
                    Refresh(() => root.GetComponent<CollectionScreen>().Refresh());
                    return null;
                }
            },
            new Entry
            {
                Id = "menu.leaderboard", Group = "Menü", Label = "Liderlik",
                ContentFrame = true,
                Build = (parent, v) =>
                {
                    var root = LeaderboardScreen.Build(parent);
                    Refresh(() => root.GetComponent<LeaderboardScreen>().Refresh());
                    return null;
                }
            },
            new Entry
            {
                Id = "menu.tabbar", Group = "Menü", Label = "Sekme çubuğu",
                Key = "MenuShell",
                Variants = 5,
                VariantNames = new[] { "Shop", "Leaderboard", "Home", "Journey", "Collection" },
                Build = (parent, v) => MenuShell
                    .CreateTabBarPreview(parent, TabKeys[Mathf.Clamp(v, 0, TabKeys.Length - 1)])
                    .transform
            },

            // ----------------------------------------------------- paneller
            new Entry
            {
                Id = "panel.daily", Group = "Paneller", Label = "Günlük ödül",
                Build = (parent, v) =>
                {
                    var panel = DailyRewardPanel.Build(parent);
                    // Kurulum panelini KAPALI bırakıyor (oyunda gün dönmeden
                    // açılmasın diye); önizlemede açmak zorundayız.
                    panel.gameObject.SetActive(true);
                    return null;
                }
            },
            new Entry
            {
                Id = "panel.name", Group = "Paneller", Label = "İsim düzenleme",
                Build = (parent, v) =>
                {
                    var panel = NamePanel.Build(parent, null);
                    panel.gameObject.SetActive(true);
                    return null;
                }
            }
        };

        static readonly string[] TabKeys = { "store", "board", "home", "journey", "collection" };

        /// <summary>
        /// Ekranın "verini ekrana yaz" adımını KORUMALI çağırır.
        ///
        /// NEDEN AYRI: Ekranlar sayıları (jeton, can, açık bölüm, sıralama)
        /// kurulumda değil <c>OnEnable → Refresh</c> içinde yazıyor ve
        /// düzenleyici kipinde Unity o geri çağrıyı hiç çalıştırmıyor. Çağrı
        /// yapılmazsa önizleme kurulur ama BOŞ görünür — ana ekranda oynat
        /// düğmesinin yazısı, üst bardaki sayılar, liderlik satırları hep
        /// eksik çıkar; ölçmek isteyen kişi de "yazı kayboluyor" diye
        /// olmayan bir hata arar.
        ///
        /// DERS (yan iş, ASIL işi düşürmemeli): Tazeleme oturuma bağlı bir
        /// servise takılıp istisna atarsa panel kurulmuş sayılmaz ve elimizde
        /// hiçbir önizleme kalmazdı. Bu yüzden hata yutuluyor — ama sessizce
        /// değil, konsola uyarı olarak.
        /// </summary>
        static void Refresh(Action action)
        {
            try { action(); }
            catch (Exception exception)
            {
                Debug.LogWarning("[Arayüz Tasarımı] Tazeleme atlandı — " + exception.Message);
            }
        }

        /// <summary>
        /// Henüz listeye giremeyenler — kılavuz sekmesinde gösteriliyor.
        ///
        /// Sebepleri ayrı ayrı yazılı: "eksik" demek yetmiyor, EKSİKLİĞİN NE
        /// İSTEDİĞİNİ yazmayan bir liste kimseyi harekete geçirmiyor.
        /// </summary>
        public static readonly (string Label, string Reason)[] Missing =
        {
            ("Reklam ekranı (FakeAdScreen)",
             "Kurulumu kendi kanvasını `UiKit.CreateCanvas` ile yaratıyor; o çağrı " +
             "düzenleyicide `DontDestroyOnLoad` yüzünden patlıyor. Kanvası " +
             "ÇAĞIRANIN verdiği bir `BuildInto(canvas)` ayrımı gerekiyor."),
            ("Devam teklifi (ContinueOffer)",
             "Girişi var (`ContinueOffer.Build`) ama teklif durumu oturuma bağlı; " +
             "sahte bir teklif kurgusu yazılmalı."),
            ("Bölüm girişi (LevelIntro) ve kayıp paneli",
             "İkisi de oyun durumundan besleniyor; fikstür yazılmadı."),
            ("Tahta ve blokların kendisi",
             "3B sahne, kanvas değil — bölüm editörünün 3B Önizleme sekmesine ait.")
        };
    }
}
