using System.Collections.Generic;
using GameKit.Editor;
using GameKit.UI;
using UnityEditor;
using UnityEngine;

namespace GameKit.Editor.UiDesign
{
    /// <summary>
    /// ARAYÜZ TASARIM PENCERESİ — oyunun bütün ekranlarını ve panellerini tek
    /// yerde açar, elle düzenler, düzeltmeleri oyuna işler.
    ///
    /// NEDEN VAR (aracın var oluş gerekçesi): Bu projenin arayüzü bilerek
    /// prefab'sız kuruluyor; ölçülerin hangi referans karesinden geldiği kodun
    /// yanındaki yorumda duruyor ve prefab YAML'ı birleştirmede çakışıyor
    /// (bkz. <see cref="UiKit"/>). Bedeli görsel düzenleme yapamamaktı: bir
    /// yazıyı 20 piksel kaydırmak "kodu düzenle → derle → play → o ekrana git"
    /// döngüsü demekti ve o ekranların çoğuna ancak oyun oynayarak varılıyordu.
    /// Bu pencere bedeli ödüyor, gerekçeyi bozmadan: kod hâlâ TEK KAYNAK, elle
    /// yapılan şey onun üstüne binen bir FARK listesi. Kod bir ölçüyü
    /// değiştirdiğinde elle dokunulmamış her şey yeni değeri kendiliğinden alır.
    ///
    /// DERS (araç MİMARİSİ — bölüm editöründen alınan kabuk): Üst şerit, sekme
    /// çubuğu ve durum çubuğu her sekmede AYNI durur; değişen yalnız ortadaki
    /// gövdedir. Bu düzen <see cref="LevelEditorWindow"/>'dan geliyor ve teması
    /// da oradan (<see cref="EditorSkin"/>) — iki araç aynı oyunun içinde
    /// iki ayrı görünüme sahip olmamalı.
    ///
    /// Sekmeler:
    ///   • Paneller — panelleri OYUNU BAŞLATMADAN kurar, düzenler (asıl akış)
    ///   • Canlı    — play modunda GERÇEK ekranı, gerçek verisiyle düzenler
    ///   • Kayıtlı  — kaydedilmiş bütün düzeltmeler + SAHİPSİZ kalanlar
    ///   • Kılavuz  — nasıl çalıştığı, sınırları, eksik paneller
    ///
    /// Sınıf parçalara bölünmüştür: bu dosya (kabuk, sekmeler, durum),
    /// <c>.Panels</c> (panel sekmesi ve denetçi), <c>.Live</c> (play modu akışı),
    /// <c>.Saved</c> (kayıt listesi ve kılavuz).
    /// </summary>
    public sealed partial class UiDesignWindow : EditorWindow
    {
        enum Tab { Panels, Live, Saved, Guide }

        static readonly (Tab Id, string Label, string Tip)[] TabInfo =
        {
            (Tab.Panels, "Paneller", "Bütün ekranları oyunu başlatmadan kur ve düzenle"),
            (Tab.Live,   "Canlı",    "Play modunda gerçek ekranı gerçek verisiyle düzenle"),
            (Tab.Saved,  "Kayıtlı",  "Kaydedilmiş düzeltmeler ve sahipsiz kalanlar"),
            (Tab.Guide,  "Kılavuz",  "Nasıl çalışır, neyi düzenleyemez")
        };

        Tab _tab = Tab.Panels;
        string _status = "Soldan bir panel seç.";

        [MenuItem("Tools/Block Out/Arayüz Tasarımı %#u")]
        static void Open()
        {
            var window = GetWindow<UiDesignWindow>("Arayüz Tasarımı");
            window.minSize = new Vector2(900f, 560f);
        }

        void OnEnable()
        {
            // Tezgâh sahnede yaşayan geçici nesneler kuruyor; derleme ve play
            // geçişleri onları arkada bırakmasın (bkz. UiPanelStage sınıf notu).
            AssemblyReloadEvents.beforeAssemblyReload += DropStage;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= DropStage;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            DropStage();
        }

        void OnPlayModeChanged(PlayModeStateChange change) => DropStage();

        void OnGUI()
        {
            // Pencerenin TAMAMINI boya: boyanmayan her boşlukta Unity'nin
            // klasik grisi sızar ve temanın iki tonlu siyahını bozar.
            EditorSkin.Fill(new Rect(0f, 0f, position.width, position.height),
                EditorSkin.Window);

            DrawTopBar();
            DrawTabBar();

            switch (_tab)
            {
                case Tab.Panels: DrawPanelsTab(); break;
                case Tab.Live:   DrawLiveTab();   break;
                case Tab.Saved:  DrawSavedTab();  break;
                case Tab.Guide:  DrawGuideTab();  break;
            }

            DrawStatusBar();
        }

        // ------------------------------------------------------------- kabuk

        void DrawTopBar()
        {
            var row = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true));
            EditorSkin.Fill(row, EditorSkin.Bar);
            EditorSkin.Fill(new Rect(row.x, row.yMax - 1f, row.width, 1f),
                EditorSkin.Hairline);

            var title = new Rect(row.x + 12f, row.y, 200f, row.height);
            EditorSkin.TrackedLabel(title, "ARAYÜZ TASARIMI",
                EditorSkin.SectionTitle);

            // Sağ uçta varlığın durumu: kaç düzeltme kayıtlı olduğunu görmeden
            // "acaba kaydoldu mu" sorusu her seferinde yeniden soruluyor.
            var asset = UiTweakDiff.Find();
            int saved = asset != null && asset.Entries != null ? asset.Entries.Length : 0;
            var info = new Rect(row.xMax - 260f, row.y, 250f, row.height);
            GUI.Label(info, saved == 0 ? "kayıtlı düzeltme yok"
                                       : saved + " kayıtlı düzeltme",
                new GUIStyle(EditorSkin.RowLabel)
                { alignment = TextAnchor.MiddleRight, fontSize = 11 });
        }

        void DrawTabBar()
        {
            var row = GUILayoutUtility.GetRect(0f, 32f, GUILayout.ExpandWidth(true));
            EditorSkin.Fill(row, EditorSkin.Window);
            EditorSkin.Fill(new Rect(row.x, row.yMax - 1f, row.width, 1f),
                EditorSkin.Hairline);

            float x = row.x + 10f;
            var style = new GUIStyle(EditorSkin.RowLabel)
            { alignment = TextAnchor.MiddleCenter, fontSize = 11 };

            foreach (var info in TabInfo)
            {
                var content = new GUIContent(info.Label, info.Tip);
                float width = style.CalcSize(content).x + 26f;
                var slot = new Rect(x, row.y, width, row.height);
                bool active = _tab == info.Id;

                if (active)
                    EditorSkin.RoundedRect(
                        new Rect(slot.x + 3f, slot.y + 4f, slot.width - 6f, slot.height - 9f),
                        new Color(EditorSkin.Accent.r, EditorSkin.Accent.g,
                            EditorSkin.Accent.b, 0.16f), EditorSkin.Round6);

                if (GUI.Button(slot, GUIContent.none, GUIStyle.none) && !active)
                {
                    _tab = info.Id;
                    GUI.FocusControl(null);
                }

                style.normal.textColor = active
                    ? EditorSkin.Text
                    : EditorSkin.TextMuted;
                GUI.Label(slot, content, style);
                x += width;
            }
        }

        void DrawStatusBar()
        {
            var row = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            EditorSkin.Fill(row, EditorSkin.Bar);
            EditorSkin.Fill(new Rect(row.x, row.y, row.width, 1f),
                EditorSkin.Hairline);

            GUI.Label(new Rect(row.x + 10f, row.y, row.width - 220f, row.height), _status,
                new GUIStyle(EditorSkin.RowLabel) { fontSize = 11 });

            string fixture = Application.isPlaying
                ? "play modu — gerçek kayıt"
                : UiPanelFixture.Active ? "sahte kayıt (diske yazılmaz)" : "kayıt kurulmadı";

            GUI.Label(new Rect(row.xMax - 220f, row.y, 210f, row.height), fixture,
                new GUIStyle(EditorSkin.RowLabel)
                { alignment = TextAnchor.MiddleRight, fontSize = 10,
                  normal = { textColor = EditorSkin.TextDim } });
        }

        // ------------------------------------------------------- ortak yardım

        /// <summary>Dikey saç teli — sütunları ayıran çizgi.</summary>
        static void VerticalRule()
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f,
                GUILayout.Width(1f), GUILayout.ExpandHeight(true));
            EditorSkin.Fill(rect, EditorSkin.Hairline);
        }
    }
}
