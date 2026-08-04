using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// Game view'a 1080x1920 "Phone Portrait" boyutu ekleyip seçer.
    ///
    /// DERS (editor reflection): Unity, Game view boyut listesi için resmi API
    /// sunmaz — UnityEditor.GameViewSizes internal'dır. Editör araçlarında bu
    /// tür reflection kabul görür (stüdyolarda yaygın), ama sürüm geçişinde
    /// kırılabilir; o yüzden her adım try/catch içinde ve başarısızsa elle
    /// yapılacak adım loglanır. OYUN kodunda reflection'a asla başvurmayız.
    /// </summary>
    public static class GameViewUtility
    {
        const int PhoneWidth = 1080;
        const int PhoneHeight = 1920;
        const string SizeLabel = "Phone Portrait";

        [MenuItem("Tools/Block Out/Game View'ı Telefon Boyutuna Ayarla (1080x1920)")]
        public static void SetPhonePortrait()
        {
            try
            {
                var asm = typeof(UnityEditor.Editor).Assembly;
                var sizesType = asm.GetType("UnityEditor.GameViewSizes");
                var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instance = singletonType
                    .GetProperty("instance", BindingFlags.Public | BindingFlags.Static)
                    .GetValue(null);

                // Aktif platform grubunu al (Editor'de Standalone/Android hangisiyse).
                var groupTypeProp = sizesType.GetProperty("currentGroupType",
                    BindingFlags.Public | BindingFlags.Instance);
                var groupType = groupTypeProp.GetValue(instance);
                var group = sizesType
                    .GetMethod("GetGroup", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(instance, new[] { groupType });

                int index = FindOrAddSize(asm, group);
                if (index < 0)
                {
                    Debug.LogWarning("[GameView] Boyut eklenemedi — Game view sağ üstteki " +
                                     "boyut menüsünden elle 1080x1920 ekleyebilirsin.");
                    return;
                }

                // Açık Game view penceresinde seç.
                var gameViewType = asm.GetType("UnityEditor.GameView");
                var window = EditorWindow.GetWindow(gameViewType);
                gameViewType
                    .GetProperty("selectedSizeIndex",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .SetValue(window, index, null);
                window.Repaint();

                Debug.Log($"[GameView] '{SizeLabel}' ({PhoneWidth}x{PhoneHeight}) seçildi.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameView] Otomatik ayar başarısız (" + e.GetType().Name +
                                 ") — Game view boyut menüsünden elle 1080x1920 seç. " +
                                 "Unity sürümü internal API'yi değiştirmiş olabilir.");
            }
        }

        /// <summary>Grupta 1080x1920 var mı bakar, yoksa ekler; toplam listedeki index'ini döndürür.</summary>
        static int FindOrAddSize(Assembly asm, object group)
        {
            var groupClass = asm.GetType("UnityEditor.GameViewSizeGroup");
            var getTotalCount = groupClass.GetMethod("GetTotalCount");
            var getGameViewSize = groupClass.GetMethod("GetGameViewSize");
            var sizeClass = asm.GetType("UnityEditor.GameViewSize");
            var widthProp = sizeClass.GetProperty("width");
            var heightProp = sizeClass.GetProperty("height");

            int total = (int)getTotalCount.Invoke(group, null);
            for (int i = 0; i < total; i++)
            {
                var size = getGameViewSize.Invoke(group, new object[] { i });
                if ((int)widthProp.GetValue(size) == PhoneWidth &&
                    (int)heightProp.GetValue(size) == PhoneHeight)
                    return i;
            }

            // Yok — özel boyut olarak ekle.
            var sizeTypeEnum = asm.GetType("UnityEditor.GameViewSizeType");
            var ctor = sizeClass.GetConstructor(new[]
                { sizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            var newSize = ctor.Invoke(new object[]
                { Enum.ToObject(sizeTypeEnum, 1 /* FixedResolution */),
                  PhoneWidth, PhoneHeight, SizeLabel });
            groupClass.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });

            return (int)getTotalCount.Invoke(group, null) - 1;
        }
    }
}
