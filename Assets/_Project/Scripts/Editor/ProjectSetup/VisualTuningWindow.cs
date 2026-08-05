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
            DrawDiagnostics();
            EditorGUILayout.HelpBox(
                "İpucu: 'Işık yönü'nün Y bileşenini düşürmek derinliği artırır. " +
                "Saplamalar silik görünüyorsa 'Saplama dibi' tonunu koyulaştır, " +
                "'Saplama tepesi' tonunu açık bırak.", MessageType.Info);
        }

        /// <summary>
        /// Ayarlar arasındaki GEOMETRİK çelişkileri yakalar.
        ///
        /// DERS (aracın kendisi de kontrol etmeli): Kaydırıcılar birbirinden
        /// bağımsız görünür ama fiziksel olarak ilişkilidir. Bloklar çerçeveden
        /// yüksekse taşar; kullanıcı bunu "bug" sanır. Araç bu ilişkiyi
        /// açıkça söylerse ayar yapan kişi neyi düzelteceğini bilir.
        /// </summary>
        void DrawDiagnostics()
        {
            float blockTop = _config.brickHeight + _config.studHeight + _config.dragLift;
            if (blockTop > _config.frameHeight + 0.001f)
            {
                EditorGUILayout.HelpBox(
                    $"Bloklar çerçeveden yüksek: blok tepesi {blockTop:0.00}, " +
                    $"çerçeve {_config.frameHeight:0.00}. Bu yüzden sürüklenen blok " +
                    "çerçevenin üstünden taşıyor ve içinden geçiyormuş gibi görünüyor.\n" +
                    "Çözüm: 'Çerçeve yüksekliği'ni artır ya da 'Tuğla yüksekliği' / " +
                    "'Sürükleme kalkma'yı azalt.", MessageType.Warning);

                if (GUILayout.Button($"Çerçeveyi {blockTop + 0.06f:0.00} yap (otomatik düzelt)"))
                {
                    _config.frameHeight = blockTop + 0.06f;
                    EditorUtility.SetDirty(_config);
                    Apply();
                }
            }

            if (_config.frameCornerRadius > _config.frameThickness * 2.5f)
                EditorGUILayout.HelpBox(
                    "Köşe yarıçapı kalınlığa göre büyük — köşeler aşırı yuvarlak görünür " +
                    "ve kenardaki kapılar köşe kavisiyle çakışabilir.", MessageType.Info);
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
