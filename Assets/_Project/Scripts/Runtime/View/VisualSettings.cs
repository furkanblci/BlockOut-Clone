using BlockOut.Runtime.Config;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// Görsel ayarların çalışma anındaki tek erişim noktası.
    ///
    /// DERS (neden statik bir tutucu?): Ayar nesnesini her view'ın kurucusuna
    /// tek tek geçirmek çok sayıda imza değişikliği demek. Görünüm ayarı
    /// oyun boyunca TEK ve GLOBAL olduğu için burada statik tutmak makul bir
    /// takas. Aynı şeyi oyun MANTIĞI için yapmazdık — orada global durum
    /// test edilebilirliği bozar; ama görsel ayar test edilen bir şey değil.
    /// </summary>
    public static class VisualSettings
    {
        public static BlockVisualConfigSO Current { get; private set; }

        /// <summary>Ayarı uygular: mesh önbelleği ve malzeme değerleri tazelenir.</summary>
        public static void Apply(BlockVisualConfigSO config)
        {
            Current = config;
            BrickMeshBuilder.ClearCache();
            ViewKit.ClearCache();
        }

        public static float Get(float value, float fallback) => Current != null ? value : fallback;

        /// <summary>Malzeme parlaklık değerlerini ayardan yazar.</summary>
        public static void PushToMaterial(Material material)
        {
            if (material == null || Current == null) return;
            if (!material.HasProperty("_Specular")) return;

            material.SetVector("_LightDir", Current.lightDirection.normalized);
            material.SetFloat("_Ambient", Current.ambient);
            material.SetFloat("_Specular", Current.specular);
            material.SetFloat("_Gloss", Current.gloss);
            material.SetFloat("_RimStrength", Current.rim);
            material.SetFloat("_Saturation", Current.saturation);
        }
    }
}
