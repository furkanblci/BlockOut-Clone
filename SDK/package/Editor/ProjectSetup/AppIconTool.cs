using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace GameKit.Editor.Setup
{
    /// <summary>
    /// Uygulama ikonunu projeye alır ve Android ikon yuvalarına bağlar
    /// (4. tur, A1).
    ///
    /// Kullanıcı: "Oyunun app ikonu yok. İkon tasarlanacak ve eklenecek."
    ///
    /// İkon `tools/` altındaki betikle üretiliyor ve `art_raw/` içinde üç
    /// dosya olarak duruyor:
    ///   icon_app.png     — tam ikon (eski/yuvarlak yuvalar)
    ///   icon_app_bg.png  — uyarlanabilir ikonun ARKA katmanı
    ///   icon_app_fg.png  — uyarlanabilir ikonun ÖN katmanı (saydam zemin)
    ///
    /// DERS (Android ikonu tek bir resim değildir): Android 8'den beri ikon
    /// İKİ KATMAN: sistem ikisini kendi maskesiyle (daire, kare, damla…)
    /// kırpıyor ve aralarında paralaks uyguluyor. Tek bir kare resim
    /// verilirse cihaz onu beyaz bir kutunun içine oturtuyor ve oyun
    /// "yüklenmemiş uygulama" gibi görünüyor. Ön katmandaki içerik görselin
    /// ORTA %66'sında kalmalı; dışarısı her cihazda kırpılabilir.
    ///
    /// DERS (ikon ayarı ELLE yapılırsa kaybolur): Player Settings'te tıklanan
    /// her ayar, projeyi klonlayan ya da ayarları sıfırlayan ilk kişide yok
    /// olur. Kod hâline getirmek onu hem belgelenmiş hem tekrarlanabilir
    /// yapıyor — `AndroidBuildTool.ApplySettings` de aynı gerekçeyle var ve
    /// bu araç oradan da çağrılıyor.
    /// </summary>
    public static class AppIconTool
    {
        const string RawDir = "art_raw";
        const string IconDir = "Assets/_Project/Art/Icons";

        static readonly string[] Files = { "icon_app", "icon_app_bg", "icon_app_fg" };

        [MenuItem("Tools/Block Out/İkonu Uygula")]
        public static void Apply()
        {
            if (!Import()) return;

            var full = Load("icon_app");
            var background = Load("icon_app_bg");
            var foreground = Load("icon_app_fg");
            if (full == null)
            {
                Debug.LogError("[İkon] icon_app.png bulunamadı.");
                return;
            }

            // DERS (Unity 6'da ikon API si iki isim uzayina yayilmis):
            // `AndroidPlatformIconKind.Adaptive` bir `AndroidPlatformIconKind`
            // gibi görünüyor ama gerçek tipi `UnityEditor.PlatformIconKind`.
            // İmzayı Android tipine göre yazmak derlemeyi kırıyor; ayrıca
            // `GetPlatformIcons` `NamedBuildTarget` değil `BuildTargetGroup`
            // istiyor. İkisi de yalnız deneyerek görülüyor.
            SetKind(AndroidPlatformIconKind.Legacy, full, null);
            SetKind(AndroidPlatformIconKind.Round, full, null);
            if (background != null && foreground != null)
                SetKind(AndroidPlatformIconKind.Adaptive, background, foreground);

            // Editörde görünen "varsayılan" ikon da aynı olsun: derleme
            // listesinde olmayan platformlar buradan besleniyor.
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown,
                new[] { full }, IconKind.Any);

            AssetDatabase.SaveAssets();
            Debug.Log("[İkon] Android ikon yuvaları bağlandı (legacy, round, adaptive).");
        }

        /// <summary>
        /// Bir ikon türünün BÜTÜN boylarını aynı görselle doldurur.
        ///
        /// Unity her tür için birden çok çözünürlük ister (48…512). Aynı
        /// kaynağı vermek doğru: içe aktarıcı her boyu kendi ölçekliyor ve
        /// kaynak 1024 piksel olduğu için küçültme temiz çıkıyor. Boyları
        /// elle üretmek, birinde bir düzeltmeyi unutmanın kesin yolu.
        /// </summary>
        static void SetKind(PlatformIconKind kind, Texture2D primary, Texture2D secondary)
        {
            var icons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);
            foreach (var icon in icons)
            {
                if (secondary != null && icon.maxLayerCount >= 2)
                    icon.SetTextures(primary, secondary);
                else
                    icon.SetTexture(primary, 0);
            }
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, kind, icons);
        }

        /// <summary>Ham PNG'leri projeye kopyalar ve içe aktarma ayarlarını yazar.</summary>
        static bool Import()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            Directory.CreateDirectory(IconDir);

            bool any = false;
            foreach (string name in Files)
            {
                string source = Path.Combine(projectRoot, RawDir, name + ".png");
                if (!File.Exists(source))
                {
                    Debug.LogWarning("[İkon] Kaynak yok: " + source);
                    continue;
                }

                string destination = Path.Combine(IconDir, name + ".png");
                File.Copy(source, destination, overwrite: true);
                AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);

                var importer = AssetImporter.GetAtPath(destination) as TextureImporter;
                if (importer != null)
                {
                    // İKON DOKUSU SIKIŞTIRILMAZ.
                    //
                    // DERS (sıkıştırma ikonu kirletir): ASTC/ETC bloklu
                    // sıkıştırma düz renk alanlarında bant, keskin kenarlarda
                    // hâle üretir. İkon ekranda 48 piksel görünse bile
                    // kaynağı temiz olmalı; üstelik ikonlar derlemeye doku
                    // olarak değil, PNG olarak kopyalanıyor.
                    importer.textureType = TextureImporterType.Default;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
                any = true;
            }
            return any;
        }

        static Texture2D Load(string name) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(IconDir, name + ".png"));
    }
}
