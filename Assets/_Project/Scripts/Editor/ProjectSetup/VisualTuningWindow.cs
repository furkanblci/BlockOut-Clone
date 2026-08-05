using BlockOut.Runtime.Config;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.View;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Görünüm ayarlarını CANLI denemek için kaydırıcı paneli.
    ///
    /// DERS (tuning aracı): Sanat yönü tartışarak değil, deneyerek bulunur.
    /// Her denemede kod değiştirip derleme beklemek döngüyü dakikalara çıkarır;
    /// kaydırıcıyı çekip anında görmek saniyeye indirir. Stüdyolarda bu döngü
    /// hızı, sonucun kalitesini doğrudan belirler.
    ///
    /// Pencere ayarları <see cref="BlockVisualConfigSO"/>'ya yazar, sonra
    /// mesh önbelleğini temizleyip tahtayı yeniden kurdurur.
    /// </summary>
    public sealed class VisualTuningWindow : EditorWindow
    {
        BlockVisualConfigSO _config;
        Vector2 _scroll;
        bool _autoApply = true;

        [MenuItem("Tools/Block Out/Görünüm Ayarları")]
        public static void Open()
        {
            var window = GetWindow<VisualTuningWindow>("Görünüm Ayarları");
            window.minSize = new Vector2(340, 480);
        }

        void OnEnable() => _config = ProjectSetupTool.LoadVisualConfig();

        void OnGUI()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "BlockVisualConfig asset'i bulunamadı. Tools > Block Out > " +
                    "Kurulumu Şimdi Çalıştır komutunu çalıştır.", MessageType.Warning);
                if (GUILayout.Button("Kurulumu çalıştır"))
                {
                    ProjectSetupTool.RunSetupNow();
                    _config = ProjectSetupTool.LoadVisualConfig();
                }
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _autoApply = GUILayout.Toggle(_autoApply,
                    new GUIContent("Anında uygula", "Kaydırıcı değişince tahtayı yeniden kur"),
                    EditorStyles.toolbarButton, GUILayout.Width(100));
                if (GUILayout.Button("Uygula", EditorStyles.toolbarButton, GUILayout.Width(56)))
                    Apply();
                if (GUILayout.Button("Varsayılana dön", EditorStyles.toolbarButton, GUILayout.Width(110)))
                    ResetToDefaults();
                GUILayout.FlexibleSpace();
                GUILayout.Label(EditorApplication.isPlaying ? "Play modunda canlı" : "Edit modu",
                    EditorStyles.miniLabel);
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                var serialized = new SerializedObject(_config);
                serialized.Update();

                EditorGUI.BeginChangeCheck();

                var property = serialized.GetIterator();
                bool enterChildren = true;
                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (property.name == "m_Script") continue;
                    EditorGUILayout.PropertyField(property, true);
                }

                bool changed = EditorGUI.EndChangeCheck();
                serialized.ApplyModifiedProperties();

                if (changed)
                {
                    EditorUtility.SetDirty(_config);
                    if (_autoApply) Apply();
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "İpucu: 'Işık yönü'nün Y bileşenini düşürmek derinliği artırır. " +
                "Saplamalar silik görünüyorsa 'Saplama dibi' tonunu koyulaştır, " +
                "'Saplama tepesi' tonunu açık bırak.", MessageType.Info);
        }

        void Apply()
        {
            VisualSettings.Apply(_config);
            ProjectSetupTool.PushVisualsToMaterials(_config);

            if (EditorApplication.isPlaying)
            {
                var session = Object.FindFirstObjectByType<GameSession>();
                if (session != null) session.RefreshVisuals();
            }
            SceneView.RepaintAll();
        }

        void ResetToDefaults()
        {
            if (!EditorUtility.DisplayDialog("Varsayılana dön",
                    "Tüm görünüm ayarları sıfırlanacak. Emin misin?", "Evet", "Vazgeç"))
                return;

            var fresh = CreateInstance<BlockVisualConfigSO>();
            EditorUtility.CopySerialized(fresh, _config);
            DestroyImmediate(fresh);
            EditorUtility.SetDirty(_config);
            Apply();
        }
    }
}
