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
    [UnityEditor.InitializeOnLoad]
    public static class BoardCaptureTool
    {
        /// <summary>
        /// Her domain reload'dan SONRA eşzamansız shader derlemesini kapatır.
        ///
        /// Neden statik kurucuda: ayarı yakalamanın İÇİNDE kapatmak işe
        /// yaramıyor — Unity onu bir sonraki düzenleyici karesinde dikkate
        /// alıyor, yani aynı çağrıdaki `Camera.Render()` yine magenta çiziyor.
        /// Ölçüldü: ayrı bir çağrıda kapatınca magenta piksel 18 476 → 0.
        /// Domain reload ayarı varsayılana (açık) döndürdüğü için de her
        /// derlemeden sonra yeniden kapatılması gerekiyor.
        /// </summary>
        static BoardCaptureTool() => EnsureSynchronousShaders();

        /// <summary>
        /// <paramref name="levelPath"/>: `Assets/_Project/Levels/level_004.json`.
        /// <paramref name="tilt"/>: kamera eğimi (varsayılan oyununkiyle aynı).
        /// Döndürdüğü yol geçici klasördeki PNG.
        /// </summary>
        /// <param name="tweak">
        /// Tahta kurulduktan SONRA, kare alınmadan ÖNCE çağrılır. Yalnız bir
        /// olay sırasında görünen şeyleri (kapı ağzı ışığı, buz kırılması,
        /// vurgulanmış blok) elle açıp yakalayabilmek için — bunlar oynatma
        /// kipi olmadan hiç görülemezdi.
        /// </param>
        public static string Capture(string levelPath, string fileName,
            int width = 1080, int height = 1920, float tilt = 80f, float fov = 27f,
            System.Action<Transform> tweak = null)
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
            tweak?.Invoke(host.transform);

            Fit(cam, board.Width, board.Height, tilt);

            EnsureSynchronousShaders();
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

        /// <summary>
        /// EŞZAMANSIZ SHADER DERLEMESİNİ KAPATIR — ve bir daha AÇMAZ.
        ///
        /// BULUNAN HATA: Bir domain reload'dan sonraki ilk yakalamalarda
        /// tahtanın ızgara çizgileri MAGENTA (212,0,212) çıkıyordu. Materyal
        /// doğruydu (`Universal Render Pipeline/Unlit`, renk 0,0,0,0.34,
        /// kuyruk 2990) ve konsolda tek hata yoktu. Sebep: Unity düzenleyicide
        /// bir shader VARYANTI henüz derlenmemişse o nesneyi magenta "bekliyor"
        /// rengiyle çiziyor; `Camera.Render()` derlemeyi beklemiyor.
        ///
        /// ÖLÇÜLDÜ: aynı sahne art arda üç kez yakalandı, üçünde de tam
        /// 18 476 magenta piksel — yani geçici değil, TAKILI kalmış bir durum.
        /// Ayar ayrı bir çağrıda kapatılınca 0'a düştü.
        ///
        /// Ayar geri AÇILMIYOR: kapatıp aynı çağrının sonunda geri açmak bir
        /// sonraki yakalamayı yeniden bozuyor (ilk kare yine varyantı bekliyor).
        /// Bu bir doğrulama aracı; senkron derleme birkaç yüz milisaniye
        /// yavaşlatır, karşılığında kare her zaman doğru.
        ///
        /// DERS (doğrulama aracının kendi yalanı en tehlikelisidir): Kare
        /// "ızgara bozuk, blokların üstüne çiziliyor" diyordu ve oyunda öyle
        /// bir hata yok. Ölçüm aracı, ölçtüğü şeyin durumunu değil KENDİ
        /// durumunu gösterebiliyorsa önce onu sabitlemek gerekir.
        /// </summary>
        public static void EnsureSynchronousShaders()
        {
            if (UnityEditor.ShaderUtil.allowAsyncCompilation)
                UnityEditor.ShaderUtil.allowAsyncCompilation = false;
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
