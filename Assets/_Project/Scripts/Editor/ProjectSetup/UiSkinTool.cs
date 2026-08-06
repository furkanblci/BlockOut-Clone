using System.Collections.Generic;
using System.IO;
using BlockOut.Runtime.UI;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Art/UI klasörünü tarayıp <see cref="UiSkin"/> asset'ini doldurur.
    ///
    /// DERS (elle sürükle-bırak envanteri): Yirmi dört sprite'ı bir asset'in
    /// alanlarına elle sürüklemek yirmi dört fırsat demek — biri unutulur ve o
    /// ikon oyunda görünmez. Klasörü tarayan bir adım, hem ilk kurulumu hem de
    /// sonradan eklenen her dosyayı halleder. "Kaynak klasör" tek doğru;
    /// asset yalnızca onun türevi.
    /// </summary>
    public static class UiSkinTool
    {
        const string SourceFolder = "Assets/_Project/Art/UI";
        const string SkinPath = "Assets/_Project/Resources/UiSkin.asset";

        public static bool EnsureSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(SkinPath);
            bool created = skin == null;
            if (created)
            {
                skin = ScriptableObject.CreateInstance<UiSkin>();
                Directory.CreateDirectory(Path.GetDirectoryName(SkinPath));
                AssetDatabase.CreateAsset(skin, SkinPath);
            }

            var entries = new List<UiSkin.Entry>();
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { SourceFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    entries.Add(new UiSkin.Entry
                    {
                        name = Path.GetFileNameWithoutExtension(path),
                        sprite = sprite
                    });
            }
            entries.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            if (!created && SameAs(skin.Entries, entries)) return false;

            skin.SetEntries(entries.ToArray());
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] UiSkin güncellendi: {entries.Count} sprite.");
            return true;
        }

        static bool SameAs(UiSkin.Entry[] existing, List<UiSkin.Entry> fresh)
        {
            if (existing == null || existing.Length != fresh.Count) return false;
            for (int i = 0; i < fresh.Count; i++)
                if (existing[i].name != fresh[i].name || existing[i].sprite != fresh[i].sprite)
                    return false;
            return true;
        }
    }
}
