using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Art/UI altına düşen her görseli doğru ayarlarla içeri alır.
    ///
    /// DERS (import ayarı elle tıklanmaz): Bir sprite'ın Sprite olarak
    /// işaretlenmesi, mipmap'inin kapatılması, 9-slice kenar paylarının
    /// girilmesi — bunların hepsi .meta dosyasında durur. Elle yapılırsa
    /// yirmi dördüncü dosyada biri unutulur ve o tek dosya oyunda bulanık
    /// çıkar. AssetPostprocessor, kural olarak yazılır ve İÇERİ ALMA ANINDA
    /// uygulanır; dosyayı silip yeniden atsan bile ayarlar geri gelir.
    ///
    /// Kenar payları göz kararı değil: sprite'ın silueti ölçülüp kenarın
    /// hangi noktada dikleştiği bulundu (tools/import_art.py yanındaki
    /// ölçüm). Pay köşe yarıçapından küçük olursa buton gerildiğinde köşe
    /// ezilir; çok büyük olursa orta alan kalmaz ve sprite gerilemez.
    /// </summary>
    public sealed class UiSpriteImporter : AssetPostprocessor
    {
        const string UiFolder = "Assets/_Project/Art/UI/";

        // 9-slice kenar payları: (sol, alt, sağ, üst) — Unity'nin sırası budur.
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            // Butonlar: üst köşe yarıçapı ~85, alt kalınlık ~63.
            { "btn_green",  new Vector4(90, 68, 90, 88) },
            { "btn_purple", new Vector4(90, 68, 90, 88) },
            { "btn_red",    new Vector4(90, 68, 90, 88) },

            // Kartlar: köşe yarıçapı ~57.
            { "panel_card", new Vector4(60, 60, 60, 60) },
            { "panel_dark", new Vector4(60, 60, 60, 60) },

            // Çerçeve: et kalınlığı 70 ölçüldü; pay bundan BÜYÜK olmalı,
            // yoksa gerilince ortadaki boşluk değil duvarın kendisi gerilir.
            { "frame_board", new Vector4(78, 78, 78, 78) },

            // Ödül şeridi: uçları yuvarlak, ortası yazı için oyuk. Siluetten
            // ölçülen dikleşme noktaları sol 66 / sağ 86 / üst 62 / alt 101.
            { "ribbon_reward", new Vector4(92, 106, 92, 68) },

            // Avatar çerçevesi ve kare düğme kare oranda kullanılıyor ama
            // 9-dilim payı verilirse farklı boyutlarda da bozulmaz.
            { "frame_avatar", new Vector4(224, 220, 224, 224) },
            { "btn_square",   new Vector4(114, 110, 114, 116) },

            // Dördüncü parti; paylar siluetten ölçüldü.
            { "badge_reward", new Vector4(232, 268, 278, 266) },
            { "bar_tabs",     new Vector4(96, 16, 96, 86) },
            { "card_tab",     new Vector4(54, 70, 50, 68) },
        };

        // Varsayılan 512; bunlar ondan büyük olmalı.
        //
        // DERS (import boyutu sessizce kırpar): maxTextureSize sprite'ı
        // KÜÇÜLTÜR ve hiçbir uyarı vermez. 1024 genişliğindeki ödül şeridi
        // 512'ye inince yatay çözünürlüğün yarısını kaybediyordu; tam ekran
        // açılış görseli ise tanınmaz hâle gelirdi.
        static readonly Dictionary<string, int> MaxSize = new Dictionary<string, int>
        {
            { "bg_menu",         2048 },
            { "splash_art",      2048 },
            { "confetti_sheet",  1024 },
            { "ribbon_reward",   1024 },
            { "home_characters", 1024 },
            { "region_1", 1024 }, { "region_2", 1024 },
            { "region_3", 1024 }, { "region_4", 1024 },
            { "frame_avatar", 1024 }, { "avatar_player", 1024 },
            { "badge_reward", 1024 }, { "bar_tabs", 1024 },

            // LOGO PARÇALARI (12. tur, W1). Kutlamada logo ekranın %69,5'ini
            // kaplıyor, yani 1080 birimlik tuvalde ~750 birim. `logo_out`
            // dosyada 588 piksel ama 512'ye kırpılıyordu ve sonra 666 birime
            // büyütülüyordu — yumuşama ve merdivenlenme oradan geliyor.
            { "logo_game", 2048 }, { "logo_out", 2048 }, { "logo_out_back", 2048 },
            { "logo_b", 2048 }, { "logo_l", 2048 }, { "logo_o", 2048 },
            { "logo_c", 2048 }, { "logo_k", 2048 },
            { "logo_b_back", 2048 }, { "logo_l_back", 2048 },
            { "logo_o_back", 2048 }, { "logo_c_back", 2048 }, { "logo_k_back", 2048 },

            // "Süre Doldu" ekranının altın kronometresi: ekranın yarısına
            // yakınını kaplıyor ve baştan sona gradyan (12. tur, O1).
            { "icon_stopwatch", 1024 },

            // Mağaza tentesi (13. tur, M2): 946 piksel genişliğinde ve tam
            // ekran genişliğine geriliyor. Varsayılan 512'de yatay
            // çözünürlüğün yarısı gidiyor ve şerit kenarları yumuşuyor.
            { "awning_shop", 1024 },
        };

        /// <summary>
        /// SIKIŞTIRILMAYACAK görseller.
        ///
        /// NEDEN VAR (12. tur, W1 — kullanıcı: *"block out yazısı çok kesik
        /// kesik... daha iyi bir yöntem bul"*): Bütün UI sprite'ları Android'de
        /// ASTC_6x6 ile sıkıştırılıyordu. ASTC 6x6 blok tabanlı: her 6x6
        /// pikselde renk sayısını kısıtlıyor. Düz renkli ikonlarda bu
        /// görünmüyor ama LOGO baştan sona gradyan ve yumuşak kenar — orada
        /// blok sınırları basamak basamak çıkıyor.
        ///
        /// Ayrıca logo ekranın ortasında ve büyük duruyor, yani artefaktın
        /// en çok görüneceği yer.
        ///
        /// MALİYET: on üç parça RGBA32 olarak ~2,9 MB. Kutlama ekranının tek
        /// varlığı olduğu için kabul edilebilir; ASTC'de ~0,4 MB olurdu.
        ///
        /// DERS (sıkıştırma, İÇERİĞE göre seçilir): "Bütün UI'da ASTC" makul
        /// bir varsayılan ama istisnasız uygulanınca gradyanlı tek varlığı
        /// bozuyor. Bir kuralın doğru olması, istisnasının olmaması demek
        /// değil.
        static readonly HashSet<string> Uncompressed = new HashSet<string>
        {
            "logo_game", "logo_b", "logo_l", "logo_o", "logo_c", "logo_k", "logo_out",
            "logo_b_back", "logo_l_back", "logo_o_back", "logo_c_back",
            "logo_k_back", "logo_out_back",
            "icon_stopwatch",
            // Kapat çarpısı (13. tur, G3): küçük bir daire, kenarındaki
            // yumuşak alfa ASTC blok sınırlarına denk geliyor ve çember
            // tırtıklı çıkıyor.
            "icon_close",
            // Mağaza tentesi (13. tur, M2): baştan sona dikey gradyan;
            // ASTC bunu bantlıyor ve şeritler kademeli görünüyor.
            "awning_shop",
        };

        /// <summary>
        /// Ayarı tutmayan görselleri yeniden içeri aldırır.
        ///
        /// DERS (postprocessor geçmişe dönük çalışmaz): <see cref="OnPreprocessTexture"/>
        /// yalnız İÇERİ ALMA ANINDA çalışır. Görseller bu kural yazılmadan önce
        /// projeye girdiyse Unity onları "Default" doku olarak almıştır ve kuralı
        /// yazmak onları kendiliğinden düzeltmez — dosyaya sağ tıklayıp Reimport
        /// demek gerekir. Yirmi dört dosyada bu unutulur. Kurulum sırasında bir
        /// kez bakıp yanlış olanı yeniden aldırmak, kuralı geçmişe de uygular.
        /// </summary>
        public static bool EnsureImported()
        {
            bool changed = false;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiFolder.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;
                if (importer.textureType == TextureImporterType.Sprite) continue;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                changed = true;
            }

            if (changed) Debug.Log("[Setup] UI sprite ayarları uygulandı.");
            return changed;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(UiFolder)) return;

            var importer = (TextureImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;      // UI hep ekran çözünürlüğünde çizilir
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = MaxSize.TryGetValue(name, out int max) ? max : 512;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;   // 9-slice FullRect ister
            settings.spriteBorder = Borders.TryGetValue(name, out var border) ? border : Vector4.zero;
            importer.SetTextureSettings(settings);

            // Android'de ASTC: alfalı UI'da ETC2'den belirgin daha temiz.
            // İSTİSNA: gradyanlı varlıklar (bkz. `Uncompressed`).
            bool ham = Uncompressed.Contains(name);
            if (ham) importer.textureCompression = TextureImporterCompression.Uncompressed;

            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = importer.maxTextureSize,
                format = ham ? TextureImporterFormat.RGBA32 : TextureImporterFormat.ASTC_6x6,
                textureCompression = ham
                    ? TextureImporterCompression.Uncompressed
                    : TextureImporterCompression.Compressed,
            });
        }
    }
}
