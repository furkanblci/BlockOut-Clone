using System.Collections.Generic;
using System.IO;
using BlockOut.Runtime.Services;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Audio klasörünü tarayıp <see cref="AudioSkin"/> asset'ini doldurur.
    /// <see cref="UiSkinTool"/>'un ses kardeşi; aynı gerekçeyle var.
    ///
    /// Klasör düzeni (bkz. `docs/audio-brief.md`):
    ///   Assets/_Project/Audio/sfx/absorb.wav      → anahtar "absorb"
    ///   Assets/_Project/Audio/music/gameplay.ogg  → anahtar "gameplay"
    ///
    /// DERS (anahtar DOSYA ADIDIR): Alternatif, her sesi asset üzerinde bir
    /// alana elle sürüklemekti — otuz ses, otuz unutma fırsatı. Dosya adını
    /// anahtar yapmak, "sesi klasöre at, biter" demeyi mümkün kılıyor.
    /// </summary>
    public static class AudioSkinTool
    {
        const string SourceFolder = "Assets/_Project/Audio";
        const string SkinPath = "Assets/_Project/Resources/AudioSkin.asset";

        [MenuItem("Tools/Block Out/Ses Kütüphanesini Yenile")]
        public static void Refresh() => EnsureSkin(log: true);

        public static bool EnsureSkin(bool log = false)
        {
            // Klasör yoksa oluştur: kullanıcı sesleri buraya atacak.
            if (!Directory.Exists(SourceFolder))
            {
                Directory.CreateDirectory(Path.Combine(SourceFolder, "sfx"));
                Directory.CreateDirectory(Path.Combine(SourceFolder, "music"));
                AssetDatabase.Refresh();
            }

            var skin = AssetDatabase.LoadAssetAtPath<AudioSkin>(SkinPath);
            bool created = skin == null;
            if (created)
            {
                skin = ScriptableObject.CreateInstance<AudioSkin>();
                Directory.CreateDirectory(Path.GetDirectoryName(SkinPath));
                AssetDatabase.CreateAsset(skin, SkinPath);
            }

            var entries = new List<AudioSkin.Entry>();
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SourceFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    entries.Add(new AudioSkin.Entry
                    {
                        name = Path.GetFileNameWithoutExtension(path),
                        clip = clip
                    });
            }
            entries.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            if (!created && SameAs(skin.Entries, entries))
            {
                if (log) Debug.Log($"[Setup] AudioSkin zaten güncel: {entries.Count} klip.");
                return false;
            }

            skin.SetEntries(entries.ToArray());
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            AudioSkin.ClearCache();

            Debug.Log($"[Setup] AudioSkin güncellendi: {entries.Count} klip.");
            return true;
        }

        static bool SameAs(AudioSkin.Entry[] existing, List<AudioSkin.Entry> fresh)
        {
            if (existing == null || existing.Length != fresh.Count) return false;
            for (int i = 0; i < fresh.Count; i++)
                if (existing[i].name != fresh[i].name || existing[i].clip != fresh[i].clip)
                    return false;
            return true;
        }
    }

    /// <summary>
    /// Audio klasörüne düşen her klibi doğru ayarlarla içeri alır ve kütüphaneyi
    /// tazeler — böylece dosyayı klasöre atmak tek adım oluyor.
    ///
    /// DERS (efekt ile müzik AYNI ayarı istemez): Kısa efektler bellekte açık
    /// durmalı (`DecompressOnLoad`) yoksa her çalışta çözme gecikmesi olur —
    /// dokunma sesinde 30 ms bile fark edilir. Müzik ise birkaç megabayt;
    /// belleğe açmak savurganlık, `Streaming` doğrusu.
    /// </summary>
    public sealed class AudioClipImporter : AssetPostprocessor
    {
        const string AudioFolder = "Assets/_Project/Audio/";

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(AudioFolder)) return;

            var importer = (AudioImporter)assetImporter;

            // Klasör adı büyük/küçük harf duyarsız karşılaştırılıyor: Unity
            // klasörü "Music" diye oluşturdu, kod "/music/" arıyordu ve müzik
            // dosyaları sessizce EFEKT ayarlarıyla içeri giriyordu — birkaç
            // megabaytlık parça belleğe açılırdı ve kimse fark etmezdi.
            bool isMusic = assetPath.IndexOf("/music/", System.StringComparison.OrdinalIgnoreCase) >= 0;

            var settings = importer.defaultSampleSettings;
            settings.loadType = isMusic
                ? AudioClipLoadType.Streaming
                : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = isMusic
                ? AudioCompressionFormat.Vorbis
                : AudioCompressionFormat.PCM;
            settings.quality = isMusic ? 0.7f : 1f;

            // preloadAudioData artık örnek ayarlarının içinde (platform başına).
            settings.preloadAudioData = !isMusic;

            importer.defaultSampleSettings = settings;
            importer.forceToMono = !isMusic;      // efektler mono: yer ve karışım
            importer.loadInBackground = isMusic;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted,
            string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
                if (path.StartsWith(AudioFolder)) { AudioSkinTool.EnsureSkin(); return; }

            foreach (var path in deleted)
                if (path.StartsWith(AudioFolder)) { AudioSkinTool.EnsureSkin(); return; }
        }
    }
}
