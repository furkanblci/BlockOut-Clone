using System.IO;
using System.Text;
using UnityEngine;

namespace GameKit.Save
{
    /// <summary>
    /// Kaydın nereye yazıldığını soyutlar.
    ///
    /// DERS (neden arayüz?): Dosya sistemi yavaş, platforma bağlı ve testte yan
    /// etkilidir. Servisi "bir metin oku / bir metin yaz" seviyesinde soyutlayınca
    /// aynı kod hem cihazda dosyaya hem testte belleğe çalışır — ve "bozuk kayıt"
    /// senaryosunu test etmek tek satır olur.
    /// </summary>
    public interface ISaveStore
    {
        bool TryRead(out string text);
        bool TryReadBackup(out string text);
        void Write(string text);
        void Delete();
    }

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
    /// DERS (persistentDataPath): Uygulamaya özel, kaldırılınca silinen,
    /// yedeklenebilen dizin. `Application.dataPath` salt okunurdur, streamingAssets
    /// paketin içidir. Oyuncu verisi için tek doğru yer burasıdır.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        readonly string _path;
        readonly string _backupPath;
        readonly string _tempPath;

        public FileSaveStore(string fileName = "save.json")
        {
            _path = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            _backupPath = _path + ".bak";
            _tempPath = _path + ".tmp";
        }

        /// <summary>Teşhis için tam yol. "Path" adı System.IO.Path'i gölgelerdi.</summary>
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
                using (var stream = new FileStream(_tempPath, FileMode.Create, FileAccess.Write))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(text);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(_path))
                {
                    if (File.Exists(_backupPath)) File.Delete(_backupPath);
                    File.Move(_path, _backupPath);
                }

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

    /// <summary>Test ve editör araçları için bellekte duran depo.</summary>
    public sealed class MemorySaveStore : ISaveStore
    {
        string _main, _backup;

        public bool TryRead(out string text) { text = _main; return _main != null; }
        public bool TryReadBackup(out string text) { text = _backup; return _backup != null; }
        public void Write(string text) { _backup = _main; _main = text; }
        public void Delete() { _main = null; _backup = null; }

        /// <summary>Testte bozuk kayıt senaryosu kurmak için.</summary>
        public void Corrupt(string garbage = "{ bozuk ") => _main = garbage;
    }
}
