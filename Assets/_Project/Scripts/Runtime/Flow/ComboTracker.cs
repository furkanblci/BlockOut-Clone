using System;
using UnityEngine;

namespace BlockOut.Runtime.Flow
{
    /// <summary>
    /// Arka arkaya emilimleri sayar ve çarpan üretir.
    ///
    /// DERS (combo neyi ödüllendirir): Bulmacada tek doğru hamle zaten
    /// ödüllendiriliyor — blok gidiyor. Combo, HIZLI ve ARDIŞIK doğru hamleyi
    /// ödüllendirir; yani oyuncunun tahtayı önceden okuyup sırayı planlamasını.
    /// Bu, aynı bölümü ikinci kez oynamaya değer kılan şey: ilk seferde çözersin,
    /// ikinci seferde AKICI çözersin.
    ///
    /// DERS (pencere kısa olmalı): Süre uzun olursa combo kendiliğinden oluşur
    /// ve hiçbir şey ifade etmez. 2.2 saniye, düşünmeden yapılan ardışık
    /// hamleleri yakalıyor; duraklayıp planlayan oyuncu zinciri kaybediyor.
    /// </summary>
    public sealed class ComboTracker
    {
        /// <summary>İki emilim arasındaki en uzun süre.</summary>
        public const float Window = 2.2f;

        /// <summary>Bu sayıdan itibaren combo "var" sayılır ve ekranda görünür.</summary>
        public const int MinimumChain = 2;

        float _lastAbsorbTime;

        /// <summary>Şu anki zincir uzunluğu.</summary>
        public int Chain { get; private set; }

        /// <summary>Bu bölümdeki en uzun zincir — bölüm sonunda gösterilir.</summary>
        public int BestChain { get; private set; }

        /// <summary>Zincir değiştiğinde haber verir (uzunluk, yeni rekor mu).</summary>
        public event Action<int, bool> Changed;

        public void Reset()
        {
            Chain = 0;
            BestChain = 0;
            _lastAbsorbTime = -999f;
        }

        /// <summary>Bir blok emildi. Zinciri uzatır ya da sıfırdan başlatır.</summary>
        public void NoteAbsorb(float now)
        {
            Chain = now - _lastAbsorbTime <= Window ? Chain + 1 : 1;
            _lastAbsorbTime = now;

            bool record = false;
            if (Chain > BestChain)
            {
                BestChain = Chain;
                record = Chain >= MinimumChain;
            }

            Changed?.Invoke(Chain, record);
        }

        /// <summary>
        /// Pencere doldu mu diye bakar; dolduysa zinciri düşürür.
        /// Her karede çağrılır — zincirin bitişi bir OLAY değil, zamanın
        /// geçmesiyle olur; o yüzden yoklamak gerekiyor.
        /// </summary>
        public void Tick(float now)
        {
            if (Chain == 0 || now - _lastAbsorbTime <= Window) return;
            Chain = 0;
            Changed?.Invoke(0, false);
        }

        /// <summary>
        /// Zincirin ödül çarpanı. 1-2 halka çarpan vermez; asıl ödül 3'ten
        /// sonra başlıyor ki combo bir BAŞARI olsun, otomatik bir ikramiye değil.
        /// </summary>
        public static float Multiplier(int chain)
        {
            if (chain < 3) return 1f;
            return 1f + Mathf.Min(chain - 2, 5) * 0.1f;     // en fazla 1.5x
        }
    }
}
