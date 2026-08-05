using BlockOut.Core.Save;

namespace BlockOut.Runtime.Services
{
    /// <summary>
    /// Kayıttaki ayarları ses/titreşim servislerine uygular ve tersini yazar.
    ///
    /// DERS (neden ayrı bir bağlayıcı?): AudioService "sesi kapat" bilir ama
    /// kaydı bilmez; SaveService kaydı bilir ama sesi bilmez. İkisini birbirine
    /// haberdar etmek yerine aralarına ince bir çevirmen koymak, her iki tarafı
    /// da yalnız kendi işiyle bırakıyor — ileride ses bir mikser'e taşındığında
    /// değişecek tek yer burası olacak.
    /// </summary>
    public static class SettingsBinder
    {
        /// <summary>Kayıttaki değerleri servislere basar (açılışta ve ayar değişince).</summary>
        public static void Apply(SettingsData settings, AudioService audio, HapticsService haptics)
        {
            if (settings == null) return;
            if (audio != null) audio.Muted = !settings.Sounds;
            if (haptics != null) haptics.Enabled = settings.Haptics;
            // Müzik henüz yok; anahtarı kayıtta duruyor, kaynağı M5'in ilerisinde.
        }

        /// <summary>Bir anahtarı değiştirir, diske yazar ve servislere uygular.</summary>
        public static void SetSounds(bool on, AudioService audio, HapticsService haptics)
        {
            if (!MetaServices.Ready) return;
            MetaServices.Save.Mutate(d => d.Settings.Sounds = on);
            Apply(MetaServices.Save.Data.Settings, audio, haptics);
        }

        public static void SetMusic(bool on, AudioService audio, HapticsService haptics)
        {
            if (!MetaServices.Ready) return;
            MetaServices.Save.Mutate(d => d.Settings.Music = on);
            Apply(MetaServices.Save.Data.Settings, audio, haptics);
        }

        public static void SetHaptics(bool on, AudioService audio, HapticsService haptics)
        {
            if (!MetaServices.Ready) return;
            MetaServices.Save.Mutate(d => d.Settings.Haptics = on);
            Apply(MetaServices.Save.Data.Settings, audio, haptics);
        }
    }
}
