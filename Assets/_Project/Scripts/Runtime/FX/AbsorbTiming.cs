using UnityEngine;

namespace BlockOut.Runtime.FX
{
    /// <summary>
    /// Kapıdan geçişin ZAMAN ÇİZELGESİ — üç ayrı yerde okunan TEK kaynak.
    ///
    /// Emilim aynı anda üç yerde canlanıyor: blok kayıyor
    /// (<c>BlockView.PlayAbsorb</c>), kapı geri tepiyor
    /// (<c>GateView.PlayAbsorbFlash</c>) ve parçacıklar akıyor
    /// (<c>FXService.AbsorbRoutine</c>). Üçü de birbirini görmüyor.
    ///
    /// DERS (senkron, ORTAK BİR NESNEDEN değil ORTAK BİR SAYIDAN gelir):
    /// İlk tasarım üçünü tek bir "koreografi" nesnesine bağlamaktı; o zaman
    /// FX katmanının blok ve kapı görünümlerini tanıması, yani M1'de özenle
    /// kurulan gevşek bağlılığın çözülmesi gerekiyordu. Oysa üç canlandırma
    /// aynı karede başlıyor ve aynı süreyi kullanıyorsa zaten senkronlar.
    /// Paylaşılması gereken şey nesne değil, SÜRE.
    ///
    /// Bütün sayılar `Levels` videosunun 59,47 fps ham karelerinden ölçüldü;
    /// hangi karenin neye karşılık geldiği <c>FXService.OnBlockAbsorbed</c>
    /// başında tablo hâlinde yazılı.
    /// </summary>
    public static class AbsorbTiming
    {
        /// <summary>
        /// Hücre başına yutulma süresi — ÖLÇÜM: 2 hücrelik blok 14 karede
        /// (0,235 sn) yutuldu.
        ///
        /// Süre bloğun DERİNLİĞİNE bağlı, sabit değil: kapı 1 hücrelik bir
        /// bloğu 2 hücrelik bir bloktan daha çabuk yutuyor. Sabit süre
        /// kullanmak, küçük bloğu ağır çekim, uzun bloğu ise aceleci
        /// gösteriyordu.
        /// </summary>
        public const float PerCell = 0.118f;

        /// <summary>
        /// Blok kaymaya başlamadan önceki ışık — kullanıcı: "daha girmeden
        /// ışık parçaları da geliyor."
        ///
        /// Referansta ilk ışınlar blok kapıya 1,5 hücre uzaktayken (temastan
        /// 0,185 sn önce) beliriyor. Bizim oyunda blok komşu hücreye
        /// oturduğu anda emilim tetikleniyor — yani "yaklaşma" diye bir faz
        /// yok. O bekleyiş burada bir ön-yükleme olarak yaşıyor.
        /// </summary>
        public const float PreRoll = 0.09f;

        /// <summary>İlk küp temastan ne kadar sonra — ÖLÇÜM: 4 kare.</summary>
        public const float CubeDelay = 0.067f;

        /// <summary>
        /// Blok bittikten sonra küplerin sürdüğü süre — ÖLÇÜM: emisyon kare
        /// 1756'da bitti, blok 1746'da bitmişti.
        /// </summary>
        public const float CubeTail = 0.10f;

        /// <summary>Hücre başına küp — ÖLÇÜM: 4 hücrelik blokta ~60 küp.</summary>
        public const int CubesPerCell = 14;

        /// <summary>Ağız ışığının sönme süresi.</summary>
        public const float MouthFade = 0.17f;

        /// <summary>
        /// Rengi tükenen kapı, blok TAMAMEN geçtikten ne kadar sonra solmaya
        /// başlar.
        ///
        /// ÖLÇÜM — iki farklı renkte aynı sayı çıktı:
        ///   turuncu kuzey kapısı: blok kare 1644'te bitti, sönme 1673'te
        ///                         başladı → 29 kare
        ///   mavi güney kapısı:    blok kare 1746'da bitti, sönme 1775'te
        ///                         başladı → 29 kare
        /// 29 / 59,47 = 0,49 sn.
        ///
        /// Kullanıcı: "kapı bir anda kayboluyor, orjinalinde fade gibi
        /// kayboluyor, anlık değil."
        ///
        /// TEŞHİS: Sönme zaten vardı (0,375 sn) ama `RecomputeGateStates`
        /// emilimle AYNI karede çağrılıyordu. Yani kapı, blok daha içeri
        /// girerken solmaya başlıyor ve emilim efekti biterken çoktan
        /// gitmiş oluyordu — ekranda ayrı bir "kapı kapandı" anı hiç
        /// oluşmuyordu.
        ///
        /// DERS (bir geçişin görünmesi için ÖNCESİNİN de görünmesi gerekir):
        /// Solmanın süresi doğruydu; sorun ne kadar sürdüğü değil, NE ZAMAN
        /// başladığıydı. Başka bir hareketin içine gömülen geçiş, süresi ne
        /// olursa olsun "bir anda oldu" diye okunuyor.
        /// </summary>
        /// KISALDI (kullanıcı: "bir tık hızlandırabiliriz, çok yavaş
        /// olmuş"). Referansın 0,49'u ölçümdü ama orada bekleme sırasında
        /// hâlâ küpler uçuşuyor; bizim küp bulutumuz daha erken duruyor ve
        /// arada ölü bir bekleme kalıyordu.
        public const float GhostDelay = 0.26f;

        /// <summary>
        /// Kapının solma süresi — ÖLÇÜM: turuncu kapı kare 1673'te (181,128,62)
        /// iken 1696'da çerçevenin (67,57,163) rengine indi = 23 kare = 0,387 sn.
        /// Kullanıcı isteğiyle 0,26'ya çekildi; eğri aynı kaldı.
        /// </summary>
        public const float GhostFade = 0.26f;

        /// <summary>
        /// Solma eğrisi: başta hızlı, sonunda yavaş.
        ///
        /// ÖLÇÜM (turuncu kapı, R kanalı, 12 ara kare) `1 − (1−t)²` eğrisini
        /// ±0,01 içinde izliyor: t=0,25'te %44, t=0,50'de %73, t=0,75'te %93.
        ///
        /// DERS (kaç örnekle ölçtüğünü not et): Bu eğri daha önce 8 fps'te
        /// çıkarılmış ÜÇ kareden okunmuş ve "orta kare tam yarıda, yani
        /// doğrusal" diye yazılmıştı. Üç örnek, aradaki karenin gerçekte
        /// nerede olduğunu söyleyemez — 59,47 fps'te on iki örnek alınınca
        /// ortanın %73'te olduğu görüldü. Ölçüm yanlış değildi, ÇÖZÜNÜRLÜĞÜ
        /// yetersizdi.
        /// </summary>
        public static float GhostCurve(float t)
        {
            t = Mathf.Clamp01(t);
            float rest = 1f - t;
            return 1f - rest * rest;
        }

        /// <summary>Bloğun kapıdan tamamen geçmesi kaç saniye sürer.</summary>
        public static float Duration(int depthCells) =>
            Mathf.Max(PerCell, depthCells * PerCell);
    }
}
