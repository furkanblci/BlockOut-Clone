using BlockOut.Core;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Services;
using GameKit.DevTools;
using UnityEngine;

namespace BlockOut.Runtime.DevTools
{
    /// <summary>
    /// KAYIT sekmesi: cüzdan, can, envanter ve ilerleme — oku ve düzenle.
    ///
    /// DERS (kaydı GÖRMEDEN düzenleme): Eski sürüm yalnız "+1000 jeton" ve
    /// "canları doldur" düğmeleri sunuyordu; kaydın o an ne içerdiği (kaç bölüm
    /// geçilmiş, kaç deneme yapılmış, sınırsız can hakkı sürüyor mu) hiçbir
    /// yerde görünmüyordu. Bir durumu ÜRETMEK için önce onu OKUYABİLMEK
    /// gerekir — yoksa test ettiğin şeyin hangi durumdan başladığını bilemezsin.
    ///
    /// DERS (yazma yollarını PAYLAŞ): Buradaki düğmeler kayda elle dokunmuyor,
    /// oyunun kendi servislerini (ProgressService, LivesService) çağırıyor.
    /// Konsoldan verilen 1000 jeton, mağazadan alınan 1000 jetonla aynı kod
    /// yolundan geçiyor: konsolda çalışıp oyunda çalışmayan bir durum üretmek
    /// mümkün olmuyor.
    /// </summary>
    public sealed class DevSavePage : DevPage
    {
        public override string Title => "KAYIT";

        Vector2 _scroll;

        public override void Draw()
        {
            if (!MetaServices.Ready)
            {
                Empty("Meta servisleri hazır değil — kayıt okunamıyor.");
                return;
            }

            var progress = MetaServices.Progress;
            var lives = MetaServices.Lives;
            var data = MetaServices.Save.Data;

            _scroll = ScrollBegin(_scroll);

            // DERS (kart bir ÖZET, döküm değil): İlk sürüm oyuncu adını, kimlik
            // kodunu, günlük ödül serisini de basıyordu — hiçbiri test sırasında
            // bir karar değiştirmiyor. Kartta yalnız bir sonraki hamleyi
            // belirleyen dört sayı kaldı.
            CardBegin();
            Kv("Jeton", progress.Coins.ToString("N0"));
            // Değer sütunu dar: uzun metin SOLDAN kırpılır ve "…aki: 4dk" gibi
            // anlamsız bir kalıntı bırakır. Uzun bilgi ikinci bir satıra iner.
            Kv("Can", lives.Current + " / " + MetaServices.MaxLives +
                      (lives.IsFull ? "" : " · " + Span(lives.TimeToNextLife)));
            if (progress.HasInfiniteLives)
                Kv("Sınırsız can", Span(progress.InfiniteLivesLeft));
            Kv("Açık bölüm", (progress.HighestUnlockedIndex + 1) + " / " + LevelCatalog.Count);
            Kv("Kayıt", MetaServices.Save.Outcome +
                        (MetaServices.Save.ReadOnly ? " (SALT OKUNUR)" : "") +
                        (progress.NoAds ? " · reklamsız" : ""));
            CardEnd();

            Section("jeton");
            GUILayout.BeginHorizontal();
            if (Btn("+1.000", 34f)) { progress.GrantCoins(1000); Note("+1.000 jeton"); }
            if (Btn("+10.000", 34f)) { progress.GrantCoins(10000); Note("+10.000 jeton"); }
            if (Danger("coins", "SIFIRLA", 34f))
            {
                MetaServices.Save.Mutate(save => save.Coins = 0);
                Note("jeton sıfırlandı");
            }
            GUILayout.EndHorizontal();

            Section("can ve haklar");
            GUILayout.BeginHorizontal();
            if (Btn("CAN DOLDUR", 34f))
            {
                lives.Grant(MetaServices.MaxLives);
                Note("canlar dolduruldu");
            }
            if (Btn("-1 CAN", 34f)) { lives.TrySpend(); Note("bir can harcandı"); }
            if (Btn("CANI BİTİR", 34f))
            {
                // "Canın bitti" ekranını test etmenin tek dürüst yolu: gerçekten bitirmek.
                for (int i = 0; i < MetaServices.MaxLives + 2 && lives.Current > 0; i++)
                    lives.TrySpend();
                Note("canlar tüketildi");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Btn("∞ CAN 1s", 34f))
            {
                progress.GrantPackage(0, 1, false);
                Note("1 saatlik sınırsız can");
            }
            if (Btn("∞ BİTİR", 34f))
            {
                MetaServices.Save.Mutate(save => save.InfiniteLivesUntilUtc = "");
                Note("sınırsız can hakkı kaldırıldı");
            }
            if (Btn(progress.NoAds ? "REKLAMSIZ ✓" : "REKLAMSIZ", 34f))
            {
                bool value = !progress.NoAds;
                MetaServices.Save.Mutate(save => save.NoAds = value);
                Note("reklamsız hak " + (value ? "açıldı" : "kapatıldı"));
            }
            GUILayout.EndHorizontal();

            Section("yardımcı envanteri");
            foreach (PowerUpKind kind in System.Enum.GetValues(typeof(PowerUpKind)))
            {
                string id = kind.ToString();
                int count = progress.PowerUpCount(id);

                GUILayout.BeginHorizontal();
                GUILayout.Label(PowerUpInfo.Label(kind) + "  " +
                                Bold(Tint(count.ToString(), DevInk.Accent2)),
                                S.Body, GUILayout.Height(30f * U), GUILayout.Width(110f * U));
                if (Btn("0", 30f)) { progress.SetPowerUpCount(id, 0); Note(id + " sıfırlandı"); }
                if (Btn("-1", 30f)) { progress.SetPowerUpCount(id, count - 1); Note(id + " -1"); }
                if (Btn("+1", 30f)) { progress.SetPowerUpCount(id, count + 1); Note(id + " +1"); }
                if (Btn("+5", 30f)) { progress.SetPowerUpCount(id, count + 5); Note(id + " +5"); }
                GUILayout.EndHorizontal();
            }

            Section("ilerleme");
            int cleared = 0, perfect = 0, attempts = 0;
            foreach (var record in data.Levels.Values)
            {
                if (record.Cleared) cleared++;
                if (record.Perfect) perfect++;
                attempts += record.Attempts;
            }

            CardBegin();
            Kv("Geçilen", cleared + " bölüm · " + perfect + " PERFECT · " +
                          progress.FirstTryClears + " ilk denemede");
            Kv("Toplam deneme", attempts + " deneme · " + data.Levels.Count + " kayıt satırı");
            CardEnd();

            GUILayout.BeginHorizontal();
            if (Btn("HEPSİNİ AÇ", 36f))
            {
                int last = Mathf.Max(0, LevelCatalog.Count - 1);
                // Doğrudan kayda yazılıyor: ProgressService'e yalnız hata ayıklama
                // için bir "hepsini aç" metodu eklemek, yayına giden koda test
                // kapısı açmak olurdu.
                MetaServices.Save.Mutate(save => save.HighestUnlockedIndex = last);
                Note("tüm bölümler açıldı");
            }
            if (Danger("lock", "KİLİTLE", 36f))
            {
                MetaServices.Save.Mutate(save => save.HighestUnlockedIndex = 0);
                Note("ilerleme kilidi başa alındı");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Danger("records", "KAYITLARI SİL", 36f))
            {
                MetaServices.Save.Mutate(save => save.Levels.Clear());
                Note("bölüm kayıtları silindi");
            }
            if (Danger("wipe", "KAYDI SIFIRLA", 36f))
            {
                MetaServices.Save.Reset();
                Note("kayıt sıfırlandı");
            }
            GUILayout.EndHorizontal();

            ScrollEnd();
        }

        static string Span(System.TimeSpan span)
        {
            if (span <= System.TimeSpan.Zero) return "0sn";
            if (span.TotalHours >= 1d) return (int)span.TotalHours + "sa " + span.Minutes + "dk";
            if (span.TotalMinutes >= 1d) return span.Minutes + "dk " + span.Seconds + "sn";
            return span.Seconds + "sn";
        }
    }
}
