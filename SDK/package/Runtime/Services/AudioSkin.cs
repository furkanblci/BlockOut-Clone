using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Services
{
    /// <summary>
    /// Ses dosyalarının tek kayıt yeri — <see cref="GameKit.Screens.UiSkin"/>'in ses kardeşi.
    ///
    /// DERS (Resources tuzağı, ikinci kez): Klipler `Audio/` altında duruyor ve
    /// orası bir Resources klasörü DEĞİL — çalışma anında `Resources.Load` ile
    /// bulunamazlar. Hepsini Resources'a taşımak ilk akla gelen çözüm ama oradaki
    /// her şey, kullanılsın kullanılmasın, derlemeye girer ve açılışta indekslenir.
    /// Bunun yerine Resources'a YALNIZCA bu küçük asset konuyor; kliplere referans
    /// verdiği için hepsi derlemeye girer, klasör düzeni bozulmaz.
    ///
    /// DERS (eksik dosya oyunu BOZMAMALI): Ses toplama işi uzun sürer ve parça
    /// parça ilerler. <see cref="Get"/> bulamazsa null döner; çağıran taraf
    /// sentezlenmiş sese düşer. Böylece tek bir dosya bile eklendiği anda
    /// devreye girer, geri kalanı beklemez.
    /// </summary>
    public sealed class AudioSkin : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string name;
            public AudioClip clip;
        }

        [SerializeField] Entry[] entries = new Entry[0];

        public Entry[] Entries => entries;

        /// <summary>Kurulum aracı listeyi yeniler. Arama tablosu MUTLAKA sıfırlanır.</summary>
        public void SetEntries(Entry[] value)
        {
            entries = value;
            _lookup = null;
        }

        static AudioSkin _current;
        static bool _searched;
        Dictionary<string, AudioClip> _lookup;

        /// <summary>Resources'taki tek ses asset'i; yoksa null (oyun yine çalışır).</summary>
        public static AudioSkin Current
        {
            get
            {
                if (!_searched)
                {
                    _searched = true;
                    _current = Resources.Load<AudioSkin>("AudioSkin");
                }
                return _current;
            }
        }

        /// <summary>Adıyla klip getirir; bulunamazsa null döner, patlamaz.</summary>
        public static AudioClip Get(string clipName)
        {
            var skin = Current;
            if (skin == null || string.IsNullOrEmpty(clipName)) return null;

            if (skin._lookup == null)
            {
                skin._lookup = new Dictionary<string, AudioClip>(skin.entries.Length);
                foreach (var entry in skin.entries)
                    if (!string.IsNullOrEmpty(entry.name) && entry.clip != null)
                        skin._lookup[entry.name] = entry.clip;
            }

            return skin._lookup.TryGetValue(clipName, out var clip) ? clip : null;
        }

        /// <summary>Editör aracı yeni klip eklediğinde önbellek düşsün.</summary>
        public static void ClearCache()
        {
            _current = null;
            _searched = false;
        }
    }

    /// <summary>
    /// Ses dosyası adları. Dizeyi çağrı yerine yazmak yerine buradan kullan:
    /// yazım hatası derleme hatasına dönüşür, çalışma anında sessizliğe değil.
    ///
    /// Karşılıkları ve nereden bulunacağı: `docs/audio-brief.md`.
    /// </summary>
    public static class Sfx
    {
        // --- her oyunda olan oynanış sesleri ---
        public const string Win          = "win";
        public const string Lose         = "lose";
        public const string PickUp       = "pick_up";
        public const string Drop         = "drop";
        public const string Refuse       = "refuse";
        public const string TimerWarning = "timer_warning";

        // --- yardımcılar ---
        //
        // Tüketilebilirin kendi sesi `power_<kimlik>` adıyla aranır
        // (bkz. AudioService.PowerUp). Bulunamazsa aşağıdaki ortak ses çalar,
        // yani yeni bir öğe eklemek ses servisini açmayı GEREKTİRMEZ.
        public const string PowerGeneric = "power_generic";

        // OYUNA ÖZEL SESLER BURAYA YAZILMAZ.
        //
        // DERS (sözlük kite girerse kit kirlenir): Bu liste bir zamanlar
        // `absorb`, `ice_crack`, `curtain_open`, `gate_advance` gibi yalnız
        // Block Out'ta anlamı olan adlar taşıyordu. İkinci bir oyunda bunların
        // hiçbiri çalmıyor ama hepsi kodda duruyor ve "bu ne işe yarıyordu?"
        // sorusunu her okuyana bir kez sorduruyordu. Oyuna özel adlar oyunun
        // kendi `Sfx` sınıfında yaşamalı; kit yalnız ortak olanı bilir.

        // --- arayüz ---
        public const string Click        = "click";
        public const string Coin         = "coin";
        public const string Star         = "star";
        public const string PanelOpen    = "panel_open";
        public const string PanelClose   = "panel_close";
        public const string Purchase     = "purchase";
        public const string RewardClaim  = "reward_claim";
        public const string Unlock       = "unlock";

        /// <summary>Kombo kademesi (1-5). Dosya yoksa perde kaydırmayla üretilir.</summary>
        public static string Combo(int step) => "combo_" + Mathf.Clamp(step, 1, 5);

        // --- müzik ---
        public const string MusicMenu     = "menu";
        public const string MusicGameplay = "gameplay";
    }
}
