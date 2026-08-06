using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Baloo 2 ExtraBold'dan TMP font asset'i üretir ve oyunun varsayılanı yapar.
    ///
    /// DERS (SDF font neden üretilir): TMP bir .ttf dosyasını doğrudan çizemez.
    /// Harflerin kenarına olan MESAFEYİ saklayan bir atlas üretmek gerekir; asıl
    /// asset budur. Bu yüzden fontu projeye atmak yetmez, bir kere "pişirilir".
    ///
    /// DERS (dinamik atlas): Atlas iki türlü doldurulur. STATİK modda hangi
    /// harflerin gireceğini önceden yazmak gerekir — Türkçe'de ç ğ ı İ ö ş ü
    /// unutulursa oyunda kutu çıkar ve bunu ancak o yazının göründüğü ekranda
    /// fark edersin. DİNAMİK modda TMP ihtiyaç duyduğu harfi çalışma anında
    /// atlasa ekler; hiçbir karakter listesi tutmaya gerek kalmaz. Bedeli ilk
    /// görünüşte küçük bir maliyet, kazancı "eksik glif" hatasının tamamen
    /// ortadan kalkması. Bu projede o hata üç kez yaşandı — dinamik doğru seçim.
    ///
    /// Yedek (fallback) font: Baloo 2'de olmayan bir işaret çıkarsa TMP
    /// LiberationSans'a düşer, kutu çizmez.
    /// </summary>
    public static class FontSetupTool
    {
        const string SourceTtf = "Assets/_Project/Art/Fonts/Baloo2-ExtraBold.ttf";
        const string FontAssetPath = "Assets/_Project/Art/Fonts/Baloo2 SDF.asset";
        const string TitleMaterialPath = "Assets/_Project/Resources/Fonts/Baloo2 SDF Title.mat";

        /// <summary>Font asset'i, başlık materyalini ve TMP varsayılanını garanti eder.</summary>
        public static bool EnsureFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing == null)
            {
                existing = CreateFontAsset();
                if (existing == null) return false;
            }

            bool changed = EnsureFallback(existing);
            changed |= EnsureTitleMaterial(existing);
            changed |= EnsureTmpDefault(existing);
            return changed;
        }

        static TMP_FontAsset CreateFontAsset()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceTtf);
            if (source == null)
            {
                Debug.LogWarning($"[Font] {SourceTtf} bulunamadı — TMP asset üretilemedi.");
                return null;
            }

            // 90pt örnekleme + 1024'lük atlas: başlık puntolarında bile keskin,
            // tek atlasa sığıyor. Dolgu (padding) 9, kalın kontur için pay bırakır.
            var asset = TMP_FontAsset.CreateFontAsset(
                source, samplingPointSize: 90, atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024, atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            if (asset == null)
            {
                Debug.LogError("[Font] TMP_FontAsset.CreateFontAsset başarısız.");
                return null;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(FontAssetPath));
            AssetDatabase.CreateAsset(asset, FontAssetPath);

            // Atlas dokusu ve materyal, font asset'inin İÇİNDE yaşamalı; ayrı
            // dosya olurlarsa font taşındığında bağ kopar.
            asset.atlasTextures[0].name = "Baloo2 SDF Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            AssetDatabase.SaveAssets();
            Debug.Log("[Font] Baloo2 SDF üretildi.");
            return asset;
        }

        static bool EnsureFallback(TMP_FontAsset asset)
        {
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (fallback == null || asset.fallbackFontAssetTable == null) return false;
            if (asset.fallbackFontAssetTable.Contains(fallback)) return false;

            asset.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(asset);
            return true;
        }

        /// <summary>
        /// Başlıklar için PAYLAŞILAN kontur materyali.
        ///
        /// DERS (materyal örneklemesi): `label.outlineWidth` yazmak, o etikete
        /// özel bir materyal KOPYASI yaratır. On başlık, on ayrı materyal ve on
        /// ayrı çizim çağrısı demektir. Aynı görünümü paylaşılan tek bir
        /// materyalle vermek, hepsini tek çağrıda toplar.
        /// </summary>
        static bool EnsureTitleMaterial(TMP_FontAsset asset)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(TitleMaterialPath) != null) return false;

            var material = new Material(asset.material);
            material.EnableKeyword("OUTLINE_ON");
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.12f, 0.07f, 0.25f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.9f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);

            Directory.CreateDirectory(Path.GetDirectoryName(TitleMaterialPath));
            AssetDatabase.CreateAsset(material, TitleMaterialPath);
            return true;
        }

        /// <summary>
        /// TMP'nin varsayılan fontunu değiştirir — böylece kod tarafında tek bir
        /// `label.font = ...` satırı yazmaya gerek kalmaz, mevcut bütün ekranlar
        /// yeni fontla açılır.
        /// </summary>
        static bool EnsureTmpDefault(TMP_FontAsset asset)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(
                "Assets/TextMesh Pro/Resources/TMP Settings.asset");
            if (settings == null) return false;

            var so = new SerializedObject(settings);
            var prop = so.FindProperty("m_defaultFontAsset");
            if (prop == null || prop.objectReferenceValue == asset) return false;

            prop.objectReferenceValue = asset;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[Font] TMP varsayılan fontu Baloo2 SDF olarak ayarlandı.");
            return true;
        }
    }
}
