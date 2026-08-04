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

            ProjectSetupTool.EnsureBuildScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("[Android] Ayarlar uygulandı: portre, IL2CPP+ARM64, minSdk 25, " +
                      "Vulkan/GLES3, paket " + PackageName);
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

            ApplySettings();
            Directory.CreateDirectory(BuildDir);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetupTool.ScenePath },
                locationPathName = Path.Combine(BuildDir, "BlockOut.apk"),
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[Android] Derleme sonucu: {report.summary.result}, " +
                      $"süre {report.summary.totalTime.TotalSeconds:0} sn, " +
                      $"çıktı {options.locationPathName}");
        }
    }
}
