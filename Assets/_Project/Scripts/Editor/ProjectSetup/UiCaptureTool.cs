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
    /// </summary>
    public static class UiCaptureTool
    {
        public static string Capture(string fileName, int width = 1080, int height = 1920)
        {
            var canvases = new List<Canvas>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    canvases.Add(canvas);

            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var holder = new GameObject("UiCaptureCam");
            var camera = holder.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.03f, 0.12f, 1f);
            camera.targetTexture = texture;
            camera.transform.position = new Vector3(0f, 0f, -1000f);

            foreach (var canvas in canvases)
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
            string path = Path.Combine(directory, fileName);
            File.WriteAllBytes(path, image.EncodeToPNG());

            foreach (var canvas in canvases) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            camera.targetTexture = null;
            Object.DestroyImmediate(holder);
            Object.DestroyImmediate(image);
            texture.Release();

            Debug.Log($"[UiCapture] {path}");
            return path;
        }
    }
}
