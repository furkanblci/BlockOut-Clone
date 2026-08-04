using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Referans görsel katmanı: videodan alınmış bir kareyi ızgaranın ARKASINA
    /// koyup üzerinden bölüm çizmeyi sağlar.
    ///
    /// DERS (içerik hattı): Orijinal bir oyunu incelerken "bakıp tahmin ederek"
    /// yeniden yapmak hata üretir. Referansı doğrudan araca gömmek, tasarımcının
    /// hücre hücre karşılaştırma yapmasını sağlar — stüdyolarda konsept
    /// görsellerini editöre bindirmek standart bir tekniktir.
    ///
    /// ffmpeg yolu EditorPrefs'te tutulur (kişiye özel, repoya girmez).
    /// </summary>
    [Serializable]
    public sealed class LevelReferenceOverlay
    {
        const string FfmpegKey = "BlockOut.FfmpegPath";
        const string VideoKey = "BlockOut.ReferenceVideo";

        [SerializeField] string _imagePath;
        [SerializeField] public bool Visible = true;
        [SerializeField] public float Opacity = 0.5f;
        [SerializeField] public Vector2 Offset;
        [SerializeField] public float Scale = 1f;
        [SerializeField] public string Timestamp = "00:02:30";
        [SerializeField] public bool Behind = true;

        [NonSerialized] Texture2D _texture;

        public string ImagePath => _imagePath;
        public bool HasImage => Texture != null;

        public static string FfmpegPath
        {
            get => EditorPrefs.GetString(FfmpegKey, "");
            set => EditorPrefs.SetString(FfmpegKey, value);
        }

        public static string VideoPath
        {
            get => EditorPrefs.GetString(VideoKey, "");
            set => EditorPrefs.SetString(VideoKey, value);
        }

        /// <summary>Doku, domain reload sonrası ilk erişimde diskten tazelenir.</summary>
        public Texture2D Texture
        {
            get
            {
                if (_texture == null && !string.IsNullOrEmpty(_imagePath) && File.Exists(_imagePath))
                    LoadTexture(_imagePath);
                return _texture;
            }
        }

        public void SetImage(string absolutePath)
        {
            _imagePath = absolutePath;
            _texture = null;
            LoadTexture(absolutePath);
        }

        public void Clear()
        {
            _imagePath = null;
            _texture = null;
        }

        void LoadTexture(string path)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (texture.LoadImage(bytes)) _texture = texture;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Referans] Görsel yüklenemedi: " + e.Message);
            }
        }

        /// <summary>Tahtanın üstüne (ya da altına) referansı çizer.</summary>
        public void Draw(Rect boardRect)
        {
            var texture = Texture;
            if (!Visible || texture == null) return;

            var rect = new Rect(
                boardRect.x + Offset.x - boardRect.width * (Scale - 1f) * 0.5f,
                boardRect.y + Offset.y - boardRect.height * (Scale - 1f) * 0.5f,
                boardRect.width * Scale,
                boardRect.height * Scale);

            var previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(Opacity));
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        /// <summary>
        /// Videodan tek kare çıkarır (ffmpeg -ss ... -frames:v 1) ve referans yapar.
        /// Çıktı geçici klasöre yazılır; projeye asset olarak girmez.
        /// </summary>
        public bool ExtractFrame(out string error)
        {
            error = null;
            string ffmpeg = FfmpegPath, video = VideoPath;

            if (string.IsNullOrEmpty(ffmpeg) || !File.Exists(ffmpeg))
            { error = "ffmpeg yolu ayarlı değil."; return false; }
            if (string.IsNullOrEmpty(video) || !File.Exists(video))
            { error = "Video yolu ayarlı değil."; return false; }

            string outDir = Path.Combine(Path.GetTempPath(), "BlockOutRef");
            Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "frame_" + Timestamp.Replace(':', '-') + ".png");

            try
            {
                var info = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    // -ss girişten ÖNCE: hızlı arama (keyframe'e atlar, kare kare taramaz)
                    Arguments = $"-hide_banner -loglevel error -y -ss {Timestamp} " +
                                $"-i \"{video}\" -frames:v 1 \"{outPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(info))
                {
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit(20000);
                    if (!File.Exists(outPath))
                    {
                        error = string.IsNullOrWhiteSpace(stderr) ? "ffmpeg kare üretmedi." : stderr.Trim();
                        return false;
                    }
                }

                SetImage(outPath);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }
    }
}
