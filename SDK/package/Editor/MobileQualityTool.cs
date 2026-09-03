using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameKit.Editor
{
    /// <summary>
    /// Plandaki mobil optimizasyon listesini URP asset'ine uygular.
    ///
    /// DERS (ayarı elle tıklama, kodla yaz): Render ayarları asset içinde
    /// gömülü sayılardır; kim ne zaman neden değiştirdi asla belli olmaz. Kodla
    /// yazınca her değerin YANINDA gerekçesi durur ve yeni bir makinede ya da
    /// yeni bir kalite seviyesinde tek komutla tekrarlanır.
    ///
    /// DERS (ölç, sonra kapat): Buradaki her madde bedava kazanç değil; her biri
    /// bir şeyden vazgeçiyor. Gerekçeler bu oyuna özgü — başka bir projede
    /// gölgeyi kapatmak felaket olurdu.
    /// </summary>
    public static class MobileQualityTool
    {
        [MenuItem("Tools/Block Out/Mobil Kalite Ayarlarını Uygula")]
        public static void Apply()
        {
            var asset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset
                        ?? GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (asset == null)
            {
                Debug.LogError("[Kalite] Aktif URP asset'i bulunamadı.");
                return;
            }

            var so = new SerializedObject(asset);

            // HDR: geniş dinamik aralık, mobilde daha büyük render hedefi ve daha
            // fazla bant genişliği demek. Bu oyunun paleti düz ve parlak; parlama
            // (bloom) da yok. Kapatmak görsel olarak hiçbir şey kaybettirmiyor.
            Set(so, "m_SupportsHDR", false);

            // GÖLGE: tahtadaki gölgeler zaten SAHTE — blok altına konan koyu quad
            // ve mesh'e pişirilmiş vertex AO. Gerçek gölge açık kaldığında GPU
            // ayrıca bir gölge haritası pass'i çiziyor ve hiçbir piksel değişmiyor.
            Set(so, "m_MainLightShadowsSupported", false);
            Set(so, "m_AdditionalLightShadowsSupported", false);
            Set(so, "m_ShadowDistance", 0f);

            // Kamera opaque/depth kopyaları: kırılma/derinlik efekti kullanmıyoruz.
            // Açık kalsalardı her karede tam ekran bir kopyalama daha olurdu.
            Set(so, "m_RequireOpaqueTexture", false);
            Set(so, "m_RequireDepthTexture", false);

            // SRP Batcher: 8 paylaşımlı materyalle çizim çağrılarını birleştiren
            // ana kazanç. Kapalıysa her blok ayrı çağrı olurdu.
            Set(so, "m_UseSRPBatcher", true);

            // MSAA: bloklar düz renkli ve kenarları keskin; 2x kenar kırıklığını
            // belirgin azaltıyor ve mobil GPU'larda kutucuk belleğinde ucuz.
            // Cihazda ölçülüp gerekirse 1'e çekilecek.
            Set(so, "m_MSAA", 2);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Kalite] {asset.name}: HDR kapalı, gölge kapalı, opaque/depth kapalı, " +
                      $"MSAA 2x, SRP Batcher açık.");
        }

        /// <summary>
        /// URP asset alanlarının çoğu salt okunur özellik; değiştirmenin yolu
        /// SerializedObject. Alan adı sürümle değişirse sessiz geçmesin diye
        /// bulunamayanı uyarıyoruz.
        /// </summary>
        static void Set(SerializedObject so, string field, bool value)
        {
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[Kalite] Alan yok: {field}"); return; }
            prop.boolValue = value;
        }

        static void Set(SerializedObject so, string field, int value)
        {
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[Kalite] Alan yok: {field}"); return; }
            prop.intValue = value;
        }

        static void Set(SerializedObject so, string field, float value)
        {
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[Kalite] Alan yok: {field}"); return; }
            prop.floatValue = value;
        }
    }
}
