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

        /// <summary>Bloğun kapıdan tamamen geçmesi kaç saniye sürer.</summary>
        public static float Duration(int depthCells) =>
            Mathf.Max(PerCell, depthCells * PerCell);
    }
}
