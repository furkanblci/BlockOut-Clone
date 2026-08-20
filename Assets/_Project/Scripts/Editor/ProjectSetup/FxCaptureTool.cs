using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Bir parçacık prefab'ını OYNATMA KİPİ OLMADAN kareye alır.
    ///
    /// Neden gerekli: Epic Toon FX gibi bir pakette yüzlerce efekt var ve
    /// hangisinin bizim oyuna uyduğu ancak GÖRÜLEREK anlaşılıyor. Adına
    /// bakarak seçmek ("ConfettiBlastRainbow kulağa doğru geliyor") bu
    /// projede daha önce üç kez yanlış çıktı. Bu araç seçimi ölçülebilir
    /// hâle getiriyor: efekt belirli anlarda dondurulup PNG'ye yazılıyor.
    ///
    /// İKİ TUZAK — ikisi de burada karşılandı:
    ///
    /// 1. YUMUŞAK PARÇACIK. Paketteki 105 materyalin 64'ünde "Soft Particles"
    ///    açık; bu özellik kamera derinlik dokusu ister. Bizim URP asset'inde
    ///    (`Mobile_RPAsset`) derinlik dokusu KAPALI, dolayısıyla o parçacıklar
    ///    ekranda HİÇ görünmüyor. Paketin kendi belgesi de bunu yazıyor.
    ///    Araç yakalamadan önce anahtarı kapatıyor.
    ///
    /// 2. SİMÜLASYONU KÖKTEN ÇAĞIR. `Simulate` her çocuk için ayrı ayrı
    ///    çağrılırsa alt sistemlerin gecikmeleri sıfırlanıyor ve çok
    ///    parçalı efektler (havai fişek: fırlatma + patlama + kıvılcım)
    ///    boş görünüyor. İlk denememde fişek bomboş çıktı ve "efekt kötü"
    ///    diye eleyecektim; kötü olan çağrıydı.
    ///
    /// DERS (aracın yalanı, varlığın kusurundan ayırt edilemez): Bir
    /// doğrulama aracı yanlış kurulduğunda ürettiği görüntü de inandırıcı
    /// olur. "Efekt boş" ile "araç boş gösteriyor" arasındaki farkı ancak
    /// beklentiyle karşılaştırarak anlarsın.
    /// </summary>
    public static class FxCaptureTool
    {
        const string Root = "Assets/Epic Toon FX/Prefabs/";

        public static string OutputDir =>
            Path.Combine(Path.GetTempPath(), "blockout-fx");

        /// <summary>
        /// <paramref name="relative"/>: `Environment/Confetti/Blast/ConfettiBlastRainbow`
        /// gibi, paket köküne göre uzantısız yol.
        /// </summary>
        public static string[] Capture(string relative, float[] times,
            float viewSize = 3.5f, int size = 700, Color? background = null)
        {
            string path = Root + relative + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[FxCapture] yok: {path}");
                return new string[0];
            }

            Directory.CreateDirectory(OutputDir);

            var camGo = new GameObject("FxCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = viewSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background ?? new Color(0.04f, 0.03f, 0.10f, 1f);
            cam.transform.position = new Vector3(0f, 0f, -30f);

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.transform.position = Vector3.zero;
            DisableSoftParticles(go);

            // Kök sistem: alt sistemleri O sürüklüyor.
            var root = go.GetComponent<ParticleSystem>();
            if (root == null) root = go.GetComponentInChildren<ParticleSystem>(true);

            var rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;

            var files = new string[times.Length];
            for (int i = 0; i < times.Length; i++)
            {
                if (root != null) root.Simulate(times[i], withChildren: true, restart: true);
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                files[i] = Path.Combine(OutputDir,
                    $"{prefab.name}_{Mathf.RoundToInt(times[i] * 100)}.png");
                File.WriteAllBytes(files[i], tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(camGo);
            return files;
        }

        /// <summary>
        /// Yumuşak parçacık anahtarını kapatır — bkz. sınıf açıklaması.
        /// Materyaller PAYLAŞILDIĞI için bu kalıcı bir değişiklik; oyunda da
        /// kapalı olmaları GEREKİYOR, dolayısıyla yan etki değil amaç.
        /// </summary>
        public static void DisableSoftParticles(GameObject go)
        {
            foreach (var renderer in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material == null) continue;
                if (material.IsKeywordEnabled("_SOFTPARTICLES_ON"))
                {
                    material.DisableKeyword("_SOFTPARTICLES_ON");
                    EditorUtility.SetDirty(material);
                }
            }
        }
    }
}
