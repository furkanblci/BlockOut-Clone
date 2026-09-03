using GameKit.Meta;
using GameKit.Services;
using GameKit.Save;
using UnityEngine;

namespace GameKit.Editor.UiDesign
{
    /// <summary>
    /// Düzenleyici kipinde panellerin ihtiyaç duyduğu SAHTE OYUNCU VERİSİ.
    ///
    /// NEDEN GEREKLİ: Ekranların çoğu kurulurken kayıttan okuyor (jeton, can,
    /// açık bölüm, seçili arka plan). <see cref="MetaServices"/> ise
    /// <c>RuntimeInitializeOnLoadMethod</c> ile YALNIZ play modunda kuruluyor;
    /// düzenleyicide <c>Ready</c> false ve panel ya boş sayılarla kuruluyor ya
    /// da kurulurken patlıyor.
    ///
    /// DERS (aracın oyuncunun kaydına DOKUNMAMASI şart): İlk akla gelen çözüm
    /// gerçek <c>FileSaveStore</c>'u kurmak. O an araç oyuncunun kaydını
    /// okuyor, panel "kaydet" derse ÜSTÜNE yazıyor olurdu — bir tasarım aracı
    /// asla oyun verisi değiştirmemeli. Bu yüzden bellekteki depo kullanılıyor:
    /// diske hiçbir şey yazılmıyor, veriyi biz seçiyoruz ve seçtiğimiz için de
    /// önizleme HER SEFERİNDE aynı çıkıyor — ölçüm ancak böyle tekrarlanabilir.
    ///
    /// Play moduna girildiğinde motor kendi <c>Initialize</c>'ını çalıştırıp
    /// gerçek depoyla yeniden kuruyor; bu kat orada devreye hiç girmiyor.
    /// </summary>
    public static class UiPanelFixture
    {
        static bool _composed;

        /// <summary>Sahte veri kuruldu mu? (Durum çubuğunda gösteriliyor.)</summary>
        public static bool Active => _composed && !Application.isPlaying;

        /// <summary>Jeton — dört haneli, çünkü üst barın en dar hâli budur.</summary>
        public const int Coins = 1490;

        /// <summary>Açık bölüm — Yolculuk ekranının orta bölgesine denk gelsin.</summary>
        public const int UnlockedIndex = 19;

        /// <summary>
        /// Servisleri sahte veriyle kurar. Play modunda hiçbir şey yapmaz.
        /// Oturum başına bir kez yeter ama tekrar çağrılması zararsız.
        /// </summary>
        public static void Ensure(bool force = false)
        {
            if (Application.isPlaying) return;
            if (_composed && !force && MetaServices.Ready) return;

            MetaServices.Compose(new MemorySaveStore());

            var data = MetaServices.Save.Data;
            data.PlayerName = "Player";
            data.Coins = Coins;
            data.HighestUnlockedIndex = UnlockedIndex;
            data.LivesField = 4;
            data.DailyStreak = 3;

            // Yardımcıların ÜÇÜ DE dolu: rozetli hâl ile fiyatlı hâl farklı
            // yerleşimler ve panel önizlemesi ikisini de gösterebilmeli.
            data.PowerUps["hint"] = 3;
            data.PowerUps["rocket"] = 1;
            data.PowerUps["ufo"] = 2;

            MetaServices.Lives.Refresh();
            _composed = true;
        }

        /// <summary>Sahte veriyi baştan kurar — panel "kirlendiğinde" işe yarar.</summary>
        public static void Reset() => Ensure(force: true);
    }
}
