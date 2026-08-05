using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Android duman testi için hazırlık ve derleme.
    ///
    /// DERS (mobil ayarlar neden kodla?): Player Settings elle tıklanınca
    /// kimse hangi değerin neden seçildiğini hatırlamaz ve makineler arasında
    /// kayar. Ayarları koda almak onları hem belgelenmiş hem tekrarlanabilir
    /// yapar — plandaki mobil optimizasyon listesinin ilk adımı budur.
    /// </summary>
    public static class AndroidBuildTool
    {
        const string PackageName = "com.furkanblci.blockoutclone";
        const string BuildDir = "Builds";

        [MenuItem("Tools/Block Out/Android: Ayarları Uygula")]
        public static void ApplySettings()
        {
            PlayerSettings.companyName = "furkanblci";
            PlayerSettings.productName = "Block Out Clone";

            // Dikey telefon oyunu: yalnızca portre, otomatik döndürme kapalı.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, PackageName);

            // IL2CPP + ARM64: Google Play'in 64-bit zorunluluğu ve daha hızlı kod.
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Unity 6'nın desteklediği en düşük seviye 25 (Android 7.1).
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

            // Vulkan önce, GLES3 yedek (plandaki grafik hattı).
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });

            // ASTC: modern Android GPU'larının hepsinde donanım desteği var ve
            // ETC2'ye göre aynı boyutta belirgin daha iyi kalite verir.
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            // Oyun fizik kullanmıyor; sabit adım döngüsünü seyrelterek boşa
            // harcanan CPU'yu kesiyoruz (varsayılan 0.02 = saniyede 50 kez).
            Time.fixedDeltaTime = 1f / 30f;

            // Sürüm: her derlemede kod artar — cihazda hangi yapıyı test
            // ettiğini bilmek profillemenin ilk şartıdır.
            PlayerSettings.Android.bundleVersionCode =
                Mathf.Max(1, PlayerSettings.Android.bundleVersionCode + 1);

            MobileQualityTool.Apply();
            ProjectSetupTool.EnsureBuildScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[Android] Ayarlar uygulandı: portre, IL2CPP+ARM64, minSdk 25, " +
                      $"Vulkan/GLES3, ASTC, versionCode {PlayerSettings.Android.bundleVersionCode}, " +
                      "paket " + PackageName);
        }

        [MenuItem("Tools/Block Out/Android: APK Derle")]
        public static void BuildApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUtility.DisplayDialog("Platform Android değil",
                        "Önce File > Build Settings'ten Android'e geçmen gerekiyor " +
                        "(ilk geçiş tüm asset'leri yeniden içe aktarır, birkaç dakika sürer).",
                        "Şimdi geç", "Vazgeç"))
                    return;

                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    NamedBuildTarget.Android, BuildTarget.Android);
                Debug.Log("[Android] Platform değiştiriliyor — bitince menüyü tekrar çalıştır.");
                return;
            }

            Build(development: true);
        }

        [MenuItem("Tools/Block Out/Android: APK Derle (release)")]
        public static void BuildApkRelease() => Build(development: false);

        static void Build(bool development)
        {
            ApplySettings();
            Directory.CreateDirectory(BuildDir);

            // HATA (M6'da bulundu): burada eskiden yalnızca Gameplay sahnesi
            // vardı. M5'te Boot ve Home eklenince APK doğrudan oynanışta açılır,
            // ana ekran hiç görünmez oldu. Derleme listesi TEK doğruluk kaynağı;
            // sahneyi elle saymak ikinci bir liste yaratmak demektir.
            var scenes = EnabledBuildScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[Android] Derleme listesinde açık sahne yok. " +
                               "Tools > Block Out > Kurulumu Şimdi Çalıştır komutunu koştur.");
                return;
            }

            string suffix = development ? "-dev" : "";
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(BuildDir, $"BlockOut{suffix}.apk"),
                target = BuildTarget.Android,
                // Development derlemesi profiler'a bağlanabilmek için gerekli,
                // AMA kare hızını düşürür: son ölçüm hep release'te yapılır.
                options = development
                    ? BuildOptions.Development | BuildOptions.ConnectWithProfiler
                    : BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Android] Derleme ({(development ? "development" : "release")}): " +
                      $"{report.summary.result}, süre {report.summary.totalTime.TotalSeconds:0} sn, " +
                      $"boyut {report.summary.totalSize / (1024 * 1024)} MB, " +
                      $"{scenes.Length} sahne, çıktı {options.locationPathName}");
        }

        static string[] EnabledBuildScenes()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.enabled) list.Add(scene.path);
            return list.ToArray();
        }
    }
}
