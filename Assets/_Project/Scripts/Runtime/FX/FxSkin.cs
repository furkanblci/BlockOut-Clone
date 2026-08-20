using System.Collections.Generic;
using UnityEngine;

namespace BlockOut.Runtime.FX
{
    /// <summary>
    /// Kullandığımız parçacık efektlerinin tek kayıt yeri.
    ///
    /// <see cref="UI.UiSkin"/> ve `AudioSkin` ile aynı kalıp ve aynı
    /// gerekçe: efektler `Assets/Epic Toon FX/` altında duruyor, orası bir
    /// Resources klasörü DEĞİL, dolayısıyla çalışma anında bulunamazlar.
    ///
    /// Paketi olduğu gibi bırakmak ÖNEMLİ: dosyaları taşımak paketin bir
    /// sonraki sürümünü içeri almayı imkânsızlaştırır ve neyi bizim
    /// değiştirdiğimiz kaybolur. Bunun yerine Resources'a yalnız bu küçük
    /// asset konuyor; o da paketten SEÇTİĞİMİZ prefab'lara referans veriyor.
    /// Derlemeye giren tek şey seçtiklerimiz oluyor — paketin geri kalanı
    /// (yüzlerce savaş efekti) APK'ye hiç girmiyor.
    ///
    /// DERS (bir paketi "kullanmak" onu SAHİPLENMEK değildir): İlk akla
    /// gelen çözüm kullandığımız prefab'ları kendi klasörümüze kopyalamak.
    /// O an işe yarar, altı ay sonra pakette bir düzeltme çıktığında iki
    /// kopya arasında hangisinin güncel olduğu bilinemez.
    /// </summary>
    public sealed class FxSkin : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string name;
            public GameObject prefab;
        }

        [SerializeField] Entry[] entries = new Entry[0];

        public Entry[] Entries => entries;

        public void SetEntries(Entry[] value)
        {
            entries = value;
            _lookup = null;          // önbellek, kaynağı değişince geçersiz
        }

        static FxSkin _current;
        static bool _searched;
        Dictionary<string, GameObject> _lookup;

        public static FxSkin Current
        {
            get
            {
                if (!_searched)
                {
                    _searched = true;
                    _current = Resources.Load<FxSkin>("FxSkin");
                    if (_current == null)
                        Debug.LogWarning("[FxSkin] Resources/FxSkin.asset yok — kutlama " +
                                         "kendi çizdiği efektlere düşecek.");
                }
                return _current;
            }
        }

        /// <summary>Adıyla prefab getirir; bulunamazsa null döner, patlamaz.</summary>
        public static GameObject Get(string effectName)
        {
            var skin = Current;
            if (skin == null) return null;

            if (skin._lookup == null)
            {
                skin._lookup = new Dictionary<string, GameObject>(skin.entries.Length);
                foreach (var entry in skin.entries)
                    if (!string.IsNullOrEmpty(entry.name) && entry.prefab != null)
                        skin._lookup[entry.name] = entry.prefab;
            }
            return skin._lookup.TryGetValue(effectName, out var prefab) ? prefab : null;
        }
    }

    /// <summary>
    /// Efekt adları. Dizeyi çağrı yerine yazmak yerine buradan kullan:
    /// yazım hatası derleme hatasına dönüşür, sessiz boşluğa değil.
    /// </summary>
    public static class Fx
    {
        // --- Kutlama ---
        public const string ConfettiBlast   = "ConfettiBlastRainbow";
        public const string ConfettiShower  = "ConfettiShowerRainbow";
        public const string ConfettiUp      = "ConfettiDirectionalRainbow";

        /// <summary>
        /// Havai fişek renkleri — kutlama boyunca sırayla atılıyor.
        /// KÜME OLMAYAN sürümler: doğdukları yerde anında patlıyorlar, yani
        /// referansın ölçülen patlama noktalarına birebir konabiliyorlar.
        /// </summary>
        public static readonly string[] Fireworks =
        {
            "FireworkYellow", "FireworkBlue", "FireworkPurple",
            "FireworkGreen", "FireworkRed",
        };

        // --- Oynanış ---
        /// <summary>Kapıdan blok geçerken kapının ağzında.</summary>
        public const string GateAbsorb = "SparkleExplosionYellow";
        /// <summary>Buz kırılması.</summary>
        public const string IceShatter = "FrostExplosion";
        /// <summary>Ödül kartındaki jeton patlaması.</summary>
        public const string CoinBlast = "GoldCoinBlast";
        /// <summary>Güç kullanımı (roket/UFO çarpması).</summary>
        public const string PowerHit = "ExplosionNovaSmallFire";
    }
}
