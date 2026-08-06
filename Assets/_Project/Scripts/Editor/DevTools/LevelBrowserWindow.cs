using BlockOut.Runtime.Config;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.Services;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.DevTools
{
    /// <summary>
    /// Bölüm tarayıcı: her bölümü tek tıkla test etmek için editör penceresi.
    ///
    /// DERS (test yolu kısa olmalı): "Şu bölümü dene" işi Play'e bas → ana
    /// ekranı geç → Yolculuk'u aç → yukarı kaydır → düğüme bas şeklindeyse,
    /// 50 bölümlük bir oyunda kimse 47. bölümü tekrar tekrar denemez. Bir
    /// tıkla o bölüme girmek, bölüm dengelemesini fiilen mümkün kılan şeydir.
    ///
    /// Pencere Play modunda da, dışında da çalışır: Play'de oturuma "o bölüme
    /// git" der, dışındayken hedef bölümü kaydedip Play'i başlatır.
    /// </summary>
    public sealed class LevelBrowserWindow : EditorWindow
    {
        const string PendingKey = "BlockOut.PendingTestLevel";

        Vector2 _scroll;
        string _filter = "";

        [MenuItem("Tools/Block Out/Bölüm Tarayıcı %#l")]
        public static void Open() =>
            GetWindow<LevelBrowserWindow>("Bölümler").minSize = new Vector2(300f, 400f);

        /// <summary>
        /// Play başlarken bekleyen bir test bölümü varsa ona atlar.
        ///
        /// Neden EditorPrefs: Play'e girmek domain reload tetikler ve statik
        /// alanlar sıfırlanır. İstek, reload'dan SAĞ ÇIKAN bir yerde durmalı.
        /// </summary>
        [InitializeOnLoadMethod]
        static void HookPlayMode()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;

                int pending = EditorPrefs.GetInt(PendingKey, -1);
                if (pending < 0) return;
                EditorPrefs.DeleteKey(PendingKey);

                // Bir kare beklenir: AppRoot ve servisler Awake/Start'ta kurulur.
                EditorApplication.delayCall += () =>
                {
                    if (!Application.isPlaying) return;
                    AppRouter.PlayLevel(pending);
                };
            };
        }

        void OnGUI()
        {
            int count = LevelCatalog.Count;
            if (count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Bölüm kataloğu boş. Kurulum aracı çalıştı mı?", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Ana Ekran", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                if (Application.isPlaying) AppRouter.GoHome();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                Application.isPlaying ? "Play modunda — tıkla, anında geç"
                                      : "Tıklayınca Play başlar ve o bölüme girer",
                EditorStyles.miniLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int perRow = Mathf.Max(1, Mathf.FloorToInt((position.width - 24f) / 62f));
            int drawn = 0;
            bool rowOpen = false;

            for (int i = 0; i < count; i++)
            {
                string label = (i + 1).ToString();
                if (!string.IsNullOrEmpty(_filter) && !label.Contains(_filter)) continue;

                if (drawn % perRow == 0)
                {
                    if (rowOpen) EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    rowOpen = true;
                }
                drawn++;

                DrawLevelButton(i, label);
            }
            if (rowOpen) EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Bölümü yeniden başlat", GUILayout.Height(24f)))
                    Object.FindFirstObjectByType<GameSession>()?.Restart();
            }
        }

        void DrawLevelButton(int index, string label)
        {
            bool isCurrent = false;
            if (Application.isPlaying)
            {
                var session = Object.FindFirstObjectByType<GameSession>();
                isCurrent = session != null && session.LevelIndex == index;
            }

            var background = GUI.backgroundColor;
            if (isCurrent) GUI.backgroundColor = new Color(0.45f, 1f, 0.55f);
            else if (Application.isPlaying && MetaServices.Ready &&
                     !MetaServices.Progress.IsUnlocked(index))
                GUI.backgroundColor = new Color(0.72f, 0.72f, 0.78f);

            if (GUILayout.Button(label, GUILayout.Width(56f), GUILayout.Height(30f)))
                Launch(index);

            GUI.backgroundColor = background;
        }

        static void Launch(int index)
        {
            if (Application.isPlaying)
            {
                var session = Object.FindFirstObjectByType<GameSession>();
                if (session != null && AppRoot.Current != null && AppRoot.Current.InGame)
                    session.GoToLevel(index);
                else
                    AppRouter.PlayLevel(index);
                return;
            }

            EditorPrefs.SetInt(PendingKey, index);
            EditorApplication.isPlaying = true;
        }
    }
}
