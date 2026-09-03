using System.Collections.Generic;
using GameKit.Editor;
using GameKit.UI;
using UnityEditor;
using UnityEngine;

namespace GameKit.Editor.UiDesign
{
    /// <summary>
    /// "Canlı" sekmesi: play modunda GERÇEK ekranı, gerçek verisiyle düzenleme.
    ///
    /// NEDEN DURUYOR (Paneller sekmesi varken): İkisi farklı soruları
    /// cevaplıyor. Tezgâh sahte veriyle, tek panelle, hızlı ve tekrarlanabilir
    /// çalışıyor — ölçü işi için doğru yer. Ama "jetonu 12.480 olan bir
    /// oyuncuda üst bar taşıyor mu", "bu panel gerçekten o ekranın üstünde mi
    /// duruyor" gibi sorular ancak oyun akarken cevaplanır. Akış eski hâliyle
    /// korunuyor: referans al → Hierarchy'de oyna → kaydet.
    /// </summary>
    public sealed partial class UiDesignWindow
    {
        readonly Dictionary<string, UiNodeSnapshot> _liveBaseline =
            new Dictionary<string, UiNodeSnapshot>();

        string _liveStatus = "Play moduna girip \"Referans al\" ile başla.";

        void DrawLiveTab()
        {
            EditorSkin.SectionHeader("Canlı düzenleme",
                "1) Play moduna gir ve düzenleyeceğin ekranı aç  ·  2) Referans al  ·  " +
                "3) Hierarchy'de öğeleri sürükle/renklerini değiştir  ·  4) Kaydet");

            if (!Application.isPlaying)
                EditorSkin.Note(
                    "Play modunda değilsin. Ekranlar yalnız orada gerçek verisiyle kurulu " +
                    "olduğu için bu akış da orada çalışıyor. Oyunu başlatmadan düzenlemek " +
                    "istiyorsan \"Paneller\" sekmesi tam bunun için var.",
                    EditorSkin.NoteKind.Info);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (EditorSkin.Button("1 · Referans al", EditorSkin.Neutral, 140f, 24f))
                    TakeLiveBaseline();

                using (new EditorGUI.DisabledScope(_liveBaseline.Count == 0))
                    if (EditorSkin.Button("2 · Değişiklikleri kaydet",
                            EditorSkin.Positive, 190f, 24f))
                        SaveLiveChanges();

                if (EditorSkin.Button("Yeniden uygula", EditorSkin.Neutral, 120f, 24f,
                        "Kayıtlı düzeltmeleri sahnedeki bütün ekranlara yeniden uygular"))
                {
                    UiTweak.Invalidate();
                    UiTweak.ApplyAll();
                    _liveStatus = "Kayıtlı düzeltmeler sahneye yeniden uygulandı.";
                }
            }

            GUILayout.Space(6f);
            GUILayout.Label(_liveStatus, EditorSkin.SectionBody);

            if (Application.isPlaying)
            {
                EditorSkin.SectionHeader("Sahnedeki işaretli ekranlar");
                int found = 0;
                foreach (var root in FindLiveRoots())
                {
                    found++;
                    GUILayout.Label("· " + root.Key + "   (" + root.name + ")",
                        new GUIStyle(EditorSkin.RowLabel) { fontSize = 11 });
                }
                if (found == 0)
                    EditorSkin.Note(
                        "İşaretli ekran bulunamadı. Ekran kurulumu UiTweak.Mark çağırıyor mu?",
                        EditorSkin.NoteKind.Warning);
            }
        }

        static IEnumerable<UiTweakRoot> FindLiveRoots()
        {
            foreach (var root in Object.FindObjectsByType<UiTweakRoot>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(root.Key)) yield return root;
        }

        void TakeLiveBaseline()
        {
            _liveBaseline.Clear();
            int screens = 0;

            foreach (var root in FindLiveRoots())
            {
                screens++;
                UiTweakDiff.Collect(root.transform, root.Key, _liveBaseline);
            }

            _liveStatus = screens == 0
                ? "İşaretli ekran bulunamadı. (Ekran kurulumu UiTweak.Mark çağırıyor mu?)"
                : $"Referans alındı: {screens} ekran, {_liveBaseline.Count} öğe. Şimdi düzenle.";
            _status = _liveStatus;
            Repaint();
        }

        void SaveLiveChanges()
        {
            var current = new Dictionary<string, UiNodeSnapshot>();
            foreach (var root in FindLiveRoots())
                UiTweakDiff.Collect(root.transform, root.Key, current);

            int written = UiTweakDiff.Save(_liveBaseline, current);
            _liveStatus = written == 0
                ? "Kaydedilecek fark yok."
                : $"{written} kayıt güncellendi → {UiTweakDiff.AssetPath}";
            _status = _liveStatus;
        }
    }
}
