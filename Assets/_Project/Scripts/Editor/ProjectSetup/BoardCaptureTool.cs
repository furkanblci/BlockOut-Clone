using System.IO;
using UnityEngine;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Level;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Bir bölümün TAHTASINI oynatma kipine girmeden PNG olarak yazar.
    ///
    /// NEDEN VAR: Kapı, perde, üreteç ve blok silüetleri altı turdur yalnız
    /// APK'de görülebiliyordu; her düzeltme "sanırım doğru" ile
    /// kapatılıyordu. 5. turda tarif çıkarıldı ama her oturumda yeniden
    /// yazıldı — bu dosya onu kalıcılaştırıyor.
    ///
    /// DERS (doğrulanamayan kod üçüncü kez bozulur): Bu projenin en pahalı
    /// dersi. Bir görsel düzeltmenin maliyeti onu YAZMAK değil, GÖRMEK.
    ///
    /// Kamera <c>GameSession.FitCamera</c>'nın kopyası: 80° eğim, 27° FOV,
    /// mesafe ikili aramayla. Kopya olması bilinçli — oturum kurmadan aynı
    /// kadrajı üretmenin başka yolu yok; sayılar değişirse ikisi birlikte
    /// değişmeli.
    /// </summary>
    public static class BoardCaptureTool
    {
        /// <summary>
        /// <paramref name="levelPath"/>: `Assets/_Project/Levels/level_004.json`.
        /// <paramref name="tilt"/>: kamera eğimi (varsayılan oyununkiyle aynı).
        /// Döndürdüğü yol geçici klasördeki PNG.
        /// </summary>
        public static string Capture(string levelPath, string fileName,
            int width = 1080, int height = 1920, float tilt = 80f, float fov = 27f)
        {
            var level = LevelModel.Build(LevelLoader.Parse(File.ReadAllText(levelPath)));
            var board = level.Board;
            var space = new BoardSpace(board.Width, board.Height);
            // Palet `Resources` altında DEĞİL (oyun onu sahneden alıyor);
            // doğrulama aracı asset yolundan yüklüyor.
            var palette = UnityEditor.AssetDatabase.LoadAssetAtPath<ColorPaletteSO>(
                "Assets/_Project/ScriptableObjects/ColorPalette.asset");

            var host = new GameObject("BoardCapture")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            var camGo = new GameObject("BoardCaptureCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;

            var cfg = BlockOut.Runtime.View.VisualSettings.Current;
            cam.backgroundColor = cfg != null
                ? cfg.backgroundOuter : new Color(0.07f, 0.05f, 0.16f);
            cam.fieldOfView = fov;

            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = texture;
            cam.aspect = (float)width / height;

            BoardBuilder.Build(host.transform, level, space, palette);

            Fit(cam, board.Width, board.Height, tilt);
            cam.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            string directory = Path.Combine(Path.GetTempPath(), "blockout-ui");
            Directory.CreateDirectory(directory);
            if (string.IsNullOrEmpty(Path.GetExtension(fileName))) fileName += ".png";
            string path = Path.Combine(directory, fileName);
            File.WriteAllBytes(path, image.EncodeToPNG());

            cam.targetTexture = null;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(image);
            texture.Release();

            Debug.Log($"[BoardCapture] {path}");
            return path;
        }

        /// <summary>`GameSession.FitCamera`'nın doğrulama kopyası.</summary>
        static void Fit(Camera cam, int boardWidth, int boardHeight, float tilt)
        {
            var rotation = Quaternion.Euler(tilt, 0f, 0f);
            Vector3 forward = rotation * Vector3.forward;

            float hw = boardWidth * 0.5f + 0.9f;
            float hh = boardHeight * 0.5f + 0.9f;
            var corners = new[]
            {
                new Vector3(-hw, 0f, -hh), new Vector3(hw, 0f, -hh),
                new Vector3(-hw, 0f,  hh), new Vector3(hw, 0f,  hh),
                new Vector3(-hw, 0.7f, -hh), new Vector3(hw, 0.7f, -hh),
                new Vector3(-hw, 0.7f,  hh), new Vector3(hw, 0.7f,  hh)
            };

            float near = 6f, far = 80f;
            for (int i = 0; i < 18; i++)
            {
                float mid = (near + far) * 0.5f;
                cam.transform.SetPositionAndRotation(-forward * mid, rotation);
                if (AllVisible(cam, corners)) far = mid;
                else near = mid;
            }
            cam.transform.SetPositionAndRotation(-forward * far, rotation);
        }

        static bool AllVisible(Camera cam, Vector3[] points)
        {
            foreach (var p in points)
            {
                var v = cam.WorldToViewportPoint(p);
                if (v.z < 0f || v.x < 0.04f || v.x > 0.96f || v.y < 0.05f || v.y > 0.90f)
                    return false;
            }
            return true;
        }
    }
}
