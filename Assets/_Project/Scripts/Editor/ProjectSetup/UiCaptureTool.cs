using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Ekrandaki arayüzü PNG olarak diske yazar — tasarımı SAYIYLA değil GÖZLE
    /// doğrulamak için.
    ///
    /// DERS (neden ScreenCapture değil): `ScreenCapture.CaptureScreenshot`
    /// oyun penceresinin bir sonraki karesini bekler; odaklanmamış bir editörde
    /// o kare hiç gelmeyebilir. Burada kanvaslar geçici olarak bir kameraya
    /// bağlanıp ELDE render ediliyor — tek çağrıda, kare beklemeden sonuç.
    /// Bittiğinde kanvaslar eski kipine döner.
    ///
    /// DERS (saydamlığı neye bindirdiğin önemli): Kesilmiş bir görselin
    /// saydamlığını satranç zemine bindirip kontrol etmek, üretici görselin
    /// SAHTE saydamlık satrancını da aynı desende gösterdiği için işe yaramaz —
    /// beş ikonun içinde kalan satranç böyle gözden kaçtı. Bu yüzden burada
    /// düz, koyu bir zemin kullanılıyor.
    ///
    /// DERS (ne yakaladığını SEÇMEZSEN hepsini yakalarsın): İlk hali sahnedeki
    /// bütün overlay kanvasları aynı kameraya bağlıyordu. Menü kanvasları oyun
    /// sırasında yok olmuyor, yalnızca üstleri örtülüyor — hepsi tek karede
    /// üst üste bindi ve oyun içi sonuç panelini yakalamak isterken elime
    /// Mağaza ekranı geçti. Yakalama aracı "ekranda ne varsa" değil, "neyi
    /// doğrulamak istiyorsam onu" almalı.
    /// </summary>
    public static class UiCaptureTool
    {
        /// <summary>
        /// Belirli bir bileşeni barındıran kanvası yakalar.
        ///
        /// Asıl kullanım bu: <c>CaptureOf&lt;GameplayScreen&gt;("perfect")</c>
        /// demek, hangi kanvasın hangi ekrana ait olduğunu aramaktan kurtarıyor.
        /// </summary>
        public static string CaptureOf<T>(string fileName, int width = 1080, int height = 1920)
            where T : Component
        {
            var owner = Object.FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
            if (owner == null)
            {
                Debug.LogWarning($"[UiCapture] Sahnede {typeof(T).Name} yok.");
                return null;
            }

            // Ekran bileşenleri kanvasın ALTINDA olmak zorunda değil: bu projede
            // GameplayScreen "Game" nesnesinde duruyor ve kanvası kendisi kuruyor,
            // yani kanvas onun kardeşi/çocuğu olabiliyor. Üç yere de bakıyoruz.
            var canvas = owner.GetComponentInParent<Canvas>()
                      ?? owner.GetComponentInChildren<Canvas>();

            if (canvas == null)
            {
                // Son çare: en üstte çizilen açık overlay kanvas. Oyuncunun
                // gördüğü de odur.
                foreach (var candidate in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (!candidate.enabled || candidate.renderMode != RenderMode.ScreenSpaceOverlay)
                        continue;
                    if (canvas == null || candidate.sortingOrder > canvas.sortingOrder)
                        canvas = candidate;
                }
            }

            if (canvas == null)
            {
                Debug.LogWarning($"[UiCapture] {typeof(T).Name} için kanvas bulunamadı.");
                return null;
            }

            return Capture(fileName, width, height, canvas.rootCanvas);
        }

        /// <summary>
        /// <paramref name="only"/> verilirse yalnız o kanvas yakalanır, diğer
        /// overlay kanvaslar geçici olarak çizimden çıkarılır. Verilmezse eski
        /// davranış: ekrandaki her şey.
        /// </summary>
        public static string Capture(string fileName, int width = 1080, int height = 1920,
            Canvas only = null)
        {
            var canvases = new List<Canvas>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    canvases.Add(canvas);

            // İstenmeyen kanvasları KAPATMIYORUZ, yalnız Canvas bileşenini
            // devre dışı bırakıyoruz. GameObject'i kapatmak altındaki her
            // bileşenin OnDisable'ını tetikler ve oyun mantığına dokunur —
            // doğrulama aracının sahneyi değiştirmesi kabul edilemez.
            var mutedCanvases = new List<Canvas>();
            if (only != null)
            {
                foreach (var canvas in canvases)
                {
                    if (canvas == only || !canvas.enabled) continue;
                    canvas.enabled = false;
                    mutedCanvases.Add(canvas);
                }

                if (!canvases.Contains(only)) canvases.Add(only);
            }

            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var holder = new GameObject("UiCaptureCam");
            var camera = holder.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.03f, 0.12f, 1f);
            camera.targetTexture = texture;
            camera.transform.position = new Vector3(0f, 0f, -1000f);

            var targets = only != null ? new List<Canvas> { only } : canvases;
            foreach (var canvas in targets)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 50f;
            }

            Canvas.ForceUpdateCanvases();
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            string directory = Path.Combine(Path.GetTempPath(), "blockout-ui");
            Directory.CreateDirectory(directory);

            // Uzantıyı burada garantiliyoruz: çağıranlar "perfect_bizim" gibi
            // sade adlar veriyor ve uzantısız dosyayı hiçbir görüntüleyici
            // açmıyor — yakalama başarılı olduğu hâlde "bozuk" görünüyordu.
            if (string.IsNullOrEmpty(Path.GetExtension(fileName))) fileName += ".png";
            string path = Path.Combine(directory, fileName);
            File.WriteAllBytes(path, image.EncodeToPNG());

            // Geri alma: önce kip, sonra susturulanlar.
            foreach (var canvas in targets)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
            }
            foreach (var canvas in mutedCanvases) canvas.enabled = true;

            camera.targetTexture = null;
            Object.DestroyImmediate(holder);
            Object.DestroyImmediate(image);
            texture.Release();

            Debug.Log($"[UiCapture] {path}");
            return path;
        }
    }
}
