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
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = importer.maxTextureSize,
                format = TextureImporterFormat.ASTC_6x6,
                textureCompression = TextureImporterCompression.Compressed,
            });
        }
    }
}
