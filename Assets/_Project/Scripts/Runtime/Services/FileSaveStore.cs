using System.IO;
using System.Text;
using BlockOut.Core.Save;
using UnityEngine;

namespace BlockOut.Runtime.Services
{
    /// <summary>
    /// Kaydı diske ATOMİK yazar.
    ///
    /// DERS (neden doğrudan üstüne yazmıyoruz?): `File.WriteAllText` dosyayı önce
    /// SIFIRLAR sonra doldurur. Telefon o iki adımın arasında uygulamayı öldürürse
    /// (kullanıcı kapattı, sistem belleği geri aldı, pil bitti) geriye 0 baytlık
    /// bir dosya kalır — oyuncunun tüm ilerlemesi gider.
    ///
    /// Doğrusu üç adım:
    ///   1) Yeni içeriği GEÇİCİ dosyaya yaz ve diske indiğinden emin ol (Flush).
    ///   2) Mevcut kaydı .bak'a taşı (yedek).
    ///   3) Geçici dosyayı asıl adına taşı — taşıma işletim sisteminde tek adımdır.
    /// Hangi adımda ölürsek ölelim, elimizde ya eski ya yeni TAM bir kayıt kalır.
    ///
    /// DERS (persistentDataPath): Android'de uygulamaya özel, kaldırılınca silinen,
    /// yedeklenebilen dizin. `Application.dataPath` salt okunurdur; `streamingAssets`
    /// paketin içidir. Oyuncu verisi için tek doğru yer burasıdır.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        readonly string _path;
        readonly string _backupPath;
        readonly string _tempPath;

        public FileSaveStore(string fileName = "save.json")
        {
            _path = Path.Combine(Application.persistentDataPath, fileName);
            _backupPath = _path + ".bak";
            _tempPath = _path + ".tmp";
        }

        /// <summary>Teşhis için: kaydın tam yolu (konsola basılır).
        /// NOT: "Path" adı System.IO.Path'i gölgeleyeceği için FullPath.</summary>
        public string FullPath => _path;

        public bool TryRead(out string text) => TryReadFile(_path, out text);

        public bool TryReadBackup(out string text) => TryReadFile(_backupPath, out text);

        static bool TryReadFile(string path, out string text)
        {
            text = null;
            try
            {
                if (!File.Exists(path)) return false;
                text = File.ReadAllText(path, Encoding.UTF8);
                return true;
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[Save] Okunamadı ({path}): {e.Message}");
                return false;
            }
            catch (System.UnauthorizedAccessException e)
            {
                Debug.LogWarning($"[Save] Erişim yok ({path}): {e.Message}");
                return false;
            }
        }

        public void Write(string text)
        {
            try
            {
                // 1) Geçici dosyaya yaz ve diske indiğinden emin ol.
                using (var stream = new FileStream(_tempPath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(text);
                    writer.Flush();
                    stream.Flush(true);
                }

                // 2) Mevcut kaydı yedekle.
                if (File.Exists(_path))
                {
                    if (File.Exists(_backupPath)) File.Delete(_backupPath);
                    File.Move(_path, _backupPath);
                }

                // 3) Geçiciyi asıl ada taşı — bu adım atomiktir.
                File.Move(_tempPath, _path);
            }
            catch (IOException e)
            {
                Debug.LogError($"[Save] Yazılamadı: {e.Message}");
            }
            catch (System.UnauthorizedAccessException e)
            {
                Debug.LogError($"[Save] Yazma izni yok: {e.Message}");
            }
        }

        public void Delete()
        {
            TryDelete(_path);
            TryDelete(_backupPath);
            TryDelete(_tempPath);
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException e) { Debug.LogWarning($"[Save] Silinemedi ({path}): {e.Message}"); }
        }
    }
}
