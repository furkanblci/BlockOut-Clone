using System.Collections.Generic;
using GameKit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit.Editor.UiDesign
{
    /// <summary>
    /// "Paneller" sekmesi: sol tarafta bütün ekranların listesi, ortada canlı
    /// önizleme, sağda seçili öğenin denetçisi.
    /// </summary>
    public sealed partial class UiDesignWindow
    {
        const float ListWidth      = 190f;
        const float InspectorWidth = 330f;

        UiPanelStage _stage;
        string _entryId = "gameplay.result.win";
        int _variant;
        Transform _selection;
        int _changed;

        Vector2 _listScroll, _treeScroll, _inspectorScroll;
        readonly HashSet<string> _expanded = new HashSet<string>();

        // ------------------------------------------------------------ yaşam

        void DropStage()
        {
            if (_stage == null) return;
            try { _stage.Dispose(); }
            finally { _stage = null; _selection = null; }
        }

        /// <summary>
        /// Seçili paneli baştan kurar.
        ///
        /// DERS (kuran her araç `try/finally` yazmalı): Kurulum ortasında bir
        /// istisna, sahnede yarım kalmış bir kanvas bırakır ve o kanvas
        /// kullanıcının menüsünü bozar — 14. turda tam olarak bu oldu. Eski
        /// tezgâh HER durumda kapatılıyor, yenisi ondan sonra kuruluyor.
        /// </summary>
        void Rebuild()
        {
            DropStage();

            var entry = UiPanelCatalog.Get(_entryId);
            if (entry == null) { _status = "Panel bulunamadı: " + _entryId; return; }

            _variant = Mathf.Clamp(_variant, 0, Mathf.Max(0, entry.Variants - 1));
            _stage = UiPanelStage.Build(entry, _variant);
            _selection = null;
            _expanded.Clear();

            RecountChanges();
            _status = _stage.Ok
                ? $"{entry.Label} kuruldu — {_stage.Baseline.Count} öğe."
                : $"{entry.Label} KURULAMADI — {_stage.Error}";
        }

        void RecountChanges()
        {
            _changed = 0;
            if (_stage == null || !_stage.Ok) return;

            var current = _stage.ReadCurrent();
            foreach (var pair in _stage.Baseline)
                if (current.TryGetValue(pair.Key, out var now) &&
                    UiTweakDiff.Differs(pair.Value, now)) _changed++;
        }

        void Touched()
        {
            _stage?.Render();
            RecountChanges();
            Repaint();
        }

        // ------------------------------------------------------------ çizim

        void DrawPanelsTab()
        {
            if (Application.isPlaying)
            {
                EditorSkin.Note(
                    "Play modundasın. Bu sekme oyunu BAŞLATMADAN çalışmak için var; " +
                    "play modunda gerçek ekranı düzenlemek istiyorsan \"Canlı\" sekmesine geç.",
                    EditorSkin.NoteKind.Warning);
            }

            // İlk açılışta panel kendiliğinden kurulsun — "önce bir düğmeye bas"
            // isteyen araç, her açılışta aynı düğmeye bastırır.
            // YALNIZ Layout karesinde: Repaint sırasında yerleşim değiştirmek
            // IMGUI'de "GUILayout mismatch" hatası verir.
            if (_stage == null && Event.current.type == EventType.Layout) Rebuild();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawEntryList();
                VerticalRule();
                DrawPreviewColumn();
                VerticalRule();
                DrawInspector();
            }
        }

        // ------------------------------------------------------------- liste

        void DrawEntryList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(ListWidth)))
            {
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

                string group = null;
                foreach (var entry in UiPanelCatalog.All)
                {
                    if (entry.Group != group)
                    {
                        group = entry.Group;
                        EditorSkin.SectionHeader(group);
                    }

                    bool active = entry.Id == _entryId;
                    var row = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));

                    if (active)
                        EditorSkin.RoundedRect(
                            new Rect(row.x + 2f, row.y + 1f, row.width - 6f, row.height - 2f),
                            new Color(EditorSkin.Accent.r, EditorSkin.Accent.g,
                                EditorSkin.Accent.b, 0.22f), EditorSkin.Round4);

                    if (GUI.Button(row, GUIContent.none, GUIStyle.none) && !active)
                    {
                        _entryId = entry.Id;
                        _variant = 0;
                        Rebuild();
                    }

                    GUI.Label(new Rect(row.x + 10f, row.y, row.width - 14f, row.height),
                        entry.Label,
                        new GUIStyle(EditorSkin.RowLabel)
                        {
                            fontSize = 11,
                            normal = { textColor = active
                                ? EditorSkin.Text : EditorSkin.TextMuted }
                        });
                }

                EditorGUILayout.EndScrollView();
            }
        }

        // ---------------------------------------------------------- önizleme

        void DrawPreviewColumn()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                var entry = UiPanelCatalog.Get(_entryId);
                DrawPreviewBar(entry);

                if (_stage == null)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Panel kurulmadı — \"Yeniden kur\"a bas.",
                        new GUIStyle(EditorSkin.SectionBody)
                        { alignment = TextAnchor.MiddleCenter });
                    GUILayout.FlexibleSpace();
                    return;
                }

                if (!_stage.Ok)
                {
                    EditorSkin.Note(
                        "Bu panel düzenleyicide kurulamadı:\n" + _stage.Error +
                        "\n\nSebebi genelde oturuma bağlı bir servis. Konsolda tam yığın var.",
                        EditorSkin.NoteKind.Danger);
                    GUILayout.FlexibleSpace();
                    return;
                }

                if (entry != null && !string.IsNullOrEmpty(entry.Note))
                    EditorSkin.Note(entry.Note, EditorSkin.NoteKind.Info);

                DrawTexture();
            }
        }

        void DrawPreviewBar(UiPanelCatalog.Entry entry)
        {
            var row = GUILayoutUtility.GetRect(0f, 26f, GUILayout.ExpandWidth(true));
            EditorSkin.Fill(row, EditorSkin.Bar);

            float x = row.x + 8f;

            if (entry != null && entry.Variants > 1)
            {
                var names = entry.VariantNames;
                var options = new string[entry.Variants];
                for (int i = 0; i < entry.Variants; i++)
                    options[i] = names != null && i < names.Length ? names[i] : (i + 1).ToString();

                var popup = new Rect(x, row.y + 3f, 180f, 20f);
                int picked = EditorGUI.Popup(popup, _variant, options, EditorSkin.PopupStyle);
                if (picked != _variant) { _variant = picked; Rebuild(); }
                x += 188f;
            }

            if (EditorSkin.Button(new Rect(x, row.y + 3f, 90f, 20f), "Yeniden kur",
                    EditorSkin.Neutral))
                Rebuild();
            x += 96f;

            using (new EditorGUI.DisabledScope(_stage == null || !_stage.Ok))
            {
                if (EditorSkin.Button(new Rect(x, row.y + 3f, 130f, 20f),
                        _changed > 0 ? $"Kaydet ({_changed})" : "Kaydet",
                        _changed > 0 ? EditorSkin.Positive : EditorSkin.Neutral))
                    SaveStage();
                x += 136f;

                if (EditorSkin.Button(new Rect(x, row.y + 3f, 120f, 20f),
                        "Paneli sıfırla", EditorSkin.Danger,
                        "Bu panelin BÜTÜN kayıtlı düzeltmelerini siler"))
                    ResetStage();
            }
        }

        void DrawTexture()
        {
            var area = GUILayoutUtility.GetRect(10f, 10f,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            float scale = Mathf.Min(area.width / UiPanelStage.Width,
                                    area.height / UiPanelStage.Height);
            if (scale <= 0f) return;

            float width = UiPanelStage.Width * scale;
            float height = UiPanelStage.Height * scale;
            var view = new Rect(area.x + (area.width - width) * 0.5f,
                                area.y + (area.height - height) * 0.5f, width, height);

            GUI.DrawTexture(view, _stage.Texture, ScaleMode.StretchToFill, false);

            // Seçim çerçevesi: dokuyu yeniden çizdirmeden, üstüne IMGUI ile.
            if (_selection != null && _stage.TryGetTextureRect(_selection, out var texRect))
            {
                var box = new Rect(
                    view.x + texRect.x * scale,
                    view.yMax - texRect.yMax * scale,
                    texRect.width * scale, texRect.height * scale);
                DrawOutline(box, EditorSkin.AccentBright);
            }

            var current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 &&
                view.Contains(current.mousePosition))
            {
                var point = new Vector2(
                    (current.mousePosition.x - view.x) / scale,
                    (view.yMax - current.mousePosition.y) / scale);

                var hit = _stage.Pick(point);
                if (hit != null) Select(hit);
                GUI.FocusControl(null);
                current.Use();
                Repaint();
            }
        }

        static void DrawOutline(Rect rect, Color color)
        {
            EditorSkin.Fill(new Rect(rect.x, rect.y, rect.width, 1f), color);
            EditorSkin.Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            EditorSkin.Fill(new Rect(rect.x, rect.y, 1f, rect.height), color);
            EditorSkin.Fill(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        void Select(Transform node)
        {
            _selection = node;

            // Seçilen öğe ağaçta GÖRÜNÜR olmalı: tuvalden seçilen bir şey
            // kapalı bir dalın içindeyse denetçi doğru değeri gösterir ama
            // ağaçta hiçbir yer vurgulanmaz ve "yanlış şeyi seçtim" sanılır.
            var walker = node;
            while (walker != null)
            {
                string path = _stage.PathOf(walker);
                if (path == null) break;
                _expanded.Add(path);
                walker = walker.parent;
            }
        }

        // ----------------------------------------------------------- denetçi

        void DrawInspector()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(InspectorWidth)))
            {
                if (_stage == null || !_stage.Ok)
                {
                    GUILayout.Label("—", EditorSkin.SectionBody);
                    return;
                }

                EditorSkin.SectionHeader("Ağaç");
                _treeScroll = EditorGUILayout.BeginScrollView(_treeScroll,
                    GUILayout.Height(Mathf.Max(120f, position.height * 0.34f)));
                foreach (var pair in _stage.Roots)
                    DrawTreeNode(pair.Root, pair.Key, 0);
                EditorGUILayout.EndScrollView();

                EditorSkin.SectionHeader("Öğe");
                _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
                DrawSelectionFields();
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawTreeNode(Transform node, string path, int depth)
        {
            if (node == null) return;

            bool expanded = _expanded.Contains(path);
            bool selected = node == _selection;
            bool hasChildren = node.childCount > 0;
            bool overridden = HasOverride(path);

            var row = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));

            if (selected)
                EditorSkin.Fill(row,
                    new Color(EditorSkin.Accent.r, EditorSkin.Accent.g,
                        EditorSkin.Accent.b, 0.30f));

            float indent = 6f + depth * 11f;

            if (hasChildren)
            {
                var arrow = new Rect(row.x + indent - 4f, row.y + 2f, 14f, 14f);
                if (GUI.Button(arrow, expanded ? "▾" : "▸",
                        new GUIStyle(EditorSkin.RowLabel)
                        { fontSize = 9, alignment = TextAnchor.MiddleCenter }))
                {
                    if (expanded) _expanded.Remove(path); else _expanded.Add(path);
                }
            }

            var label = new Rect(row.x + indent + 12f, row.y, row.width - indent - 30f, row.height);
            if (GUI.Button(label, GUIContent.none, GUIStyle.none)) Select(node);

            GUI.Label(label, node.name,
                new GUIStyle(EditorSkin.RowLabel)
                {
                    fontSize = 10,
                    normal = { textColor = selected ? EditorSkin.Text
                        : node.gameObject.activeSelf ? EditorSkin.TextMuted
                                                     : EditorSkin.TextDim }
                });

            // Düzeltilmiş öğeler sağda bir noktayla işaretleniyor: hangi
            // öğeye elle dokunulduğunu ağaca bakarak görmek, listeye bakarak
            // aramaktan hızlı.
            if (overridden)
                EditorSkin.DrawDisc(new Rect(row.xMax - 14f, row.y + 6f, 6f, 6f),
                    EditorSkin.WarningBright);

            if (!expanded) return;
            for (int i = 0; i < node.childCount; i++)
            {
                var child = node.GetChild(i);
                DrawTreeNode(child, path + "/" + child.name, depth + 1);
            }
        }

        bool HasOverride(string path)
        {
            if (_stage == null || !_stage.Baseline.TryGetValue(path, out var was)) return false;
            var node = FindByPath(path);
            return node != null && UiTweakDiff.Differs(was, UiTweakDiff.Read(node));
        }

        Transform FindByPath(string path)
        {
            foreach (var pair in _stage.Roots)
            {
                if (path == pair.Key) return pair.Root;
                if (!path.StartsWith(pair.Key + "/")) continue;

                var node = pair.Root;
                foreach (var part in path.Substring(pair.Key.Length + 1).Split('/'))
                {
                    node = node.Find(part);
                    if (node == null) return null;
                }
                return node;
            }
            return null;
        }

        void DrawSelectionFields()
        {
            if (_selection == null)
            {
                GUILayout.Label("Tuvalden ya da ağaçtan bir öğe seç.",
                    EditorSkin.SectionBody);
                return;
            }

            string path = _stage.PathOf(_selection);
            GUILayout.Label(path ?? _selection.name, new GUIStyle(EditorSkin.SectionBody)
            { wordWrap = true, fontSize = 10 });

            // İŞARETSİZ KÖKÜN ALTINDAKİ ÖĞE KAYDEDİLEMEZ. Bazı parçalar
            // (ana ekranın manzarası gibi) ekran kökünün DIŞINDA, doğrudan
            // kanvasa kuruluyor; onların bir düzeltme yolu yok. Burada
            // söylenmezse kullanıcı ayarı yapar, kaydeder ve hiçbir şeyin
            // değişmediğini görür — sistemin sessizce başarısız olduğu yer
            // tam olarak burası.
            if (path == null)
                EditorSkin.Note(
                    "Bu öğe işaretli bir ekran kökünün ALTINDA değil, o yüzden " +
                    "düzeltmesi kaydedilemez. Ayarlaman gerekiyorsa kurulumunda " +
                    "UiTweak.Mark çağıran bir köke taşınması gerekir.",
                    EditorSkin.NoteKind.Warning);

            var rect = _selection as RectTransform;
            EditorGUI.BeginChangeCheck();

            bool active = _selection.gameObject.activeSelf;
            active = EditorSkin.BarToggle(active, "Görünür", 90f);

            if (rect != null)
            {
                EditorSkin.SectionHeader("Yerleşim (%)",
                    "Kodun UiKit.Place ile verdiği dört sayı: sol, alt, sağ, üst.");

                var min = rect.anchorMin;
                var max = rect.anchorMax;
                min.x = FloatRow("Sol   x0", min.x);
                min.y = FloatRow("Alt   y0", min.y);
                max.x = FloatRow("Sağ   x1", max.x);
                max.y = FloatRow("Üst   y1", max.y);

                EditorSkin.SectionHeader("Kenar payı (piksel)",
                    "Çapaların üstüne binen sabit pay. UiKit.Place bunları sıfırlar.");

                var offsetMin = rect.offsetMin;
                var offsetMax = rect.offsetMax;
                offsetMin.x = FloatRow("Sol pay",  offsetMin.x);
                offsetMin.y = FloatRow("Alt pay",  offsetMin.y);
                offsetMax.x = FloatRow("Sağ pay",  offsetMax.x);
                offsetMax.y = FloatRow("Üst pay",  offsetMax.y);

                EditorSkin.SectionHeader("Eksen ve ölçek");
                var pivot = rect.pivot;
                pivot.x = FloatRow("Eksen x", pivot.x);
                pivot.y = FloatRow("Eksen y", pivot.y);

                float scale = FloatRow("Ölçek", _selection.localScale.x);

                if (EditorGUI.EndChangeCheck())
                {
                    // Undo YOK: tezgâhtaki nesneler HideAndDontSave, yani
                    // Unity'nin geri alma yığınına giremiyorlar. Geri dönüş
                    // yolu "Öğeyi sıfırla" ve "Yeniden kur".
                    rect.anchorMin = min; rect.anchorMax = max;
                    rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
                    rect.pivot = pivot;
                    _selection.localScale = new Vector3(scale, scale, scale);
                    if (_selection.gameObject.activeSelf != active)
                        _selection.gameObject.SetActive(active);
                    Touched();
                }
            }
            else if (EditorGUI.EndChangeCheck())
            {
                if (_selection.gameObject.activeSelf != active)
                    _selection.gameObject.SetActive(active);
                Touched();
            }

            DrawGraphicFields();
            DrawItemButtons(path);
        }

        void DrawGraphicFields()
        {
            var graphic = _selection.GetComponent<Graphic>();
            var text = _selection.GetComponent<TMPro.TextMeshProUGUI>();
            if (graphic == null && text == null) return;

            EditorSkin.SectionHeader("Görünüm");

            if (graphic != null)
            {
                EditorGUI.BeginChangeCheck();
                var color = ColorRow("Renk", graphic.color);
                if (EditorGUI.EndChangeCheck())
                {
                    graphic.color = color;
                    Touched();
                }
            }

            if (text != null)
            {
                EditorGUI.BeginChangeCheck();
                float size = FloatRow("Punto", text.fontSize);
                if (EditorGUI.EndChangeCheck())
                {
                    text.enableAutoSizing = false;
                    text.fontSize = size;
                    // Taşma korumasına yeni puntoyu bildir; yoksa bileşen bir
                    // sonraki karede eski tabanına döner (bkz. UiTweak.ApplyTo).
                    text.GetComponent<GameKit.UI.UiTextFit>()?.Rebase();
                    Touched();
                }

                GUILayout.Label("Yazı: " + Shorten(text.text),
                    new GUIStyle(EditorSkin.SectionBody) { fontSize = 10 });
            }
        }

        void DrawItemButtons(string path)
        {
            GUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (EditorSkin.Button("Öğeyi sıfırla", EditorSkin.Neutral, 110f, 20f,
                        "Bu öğeyi kodun verdiği hâle döndürür"))
                    ResetNode(path);

                if (EditorSkin.Button("Kod satırını kopyala", EditorSkin.Neutral, 150f, 20f,
                        "UiKit.Place satırı olarak panoya kopyalar — düzeltmeyi kaynağa " +
                        "geri katlamak için"))
                    CopyPlaceLine();
            }
        }

        /// <summary>
        /// Seçili öğenin yerleşimini KOD SATIRI olarak panoya yazar.
        ///
        /// NEDEN (aracın en önemli düğmesi olabilir): Bu projede ölçülerin NEDEN
        /// öyle olduğu kodun yanındaki yorumda duruyor — projenin en değerli
        /// tarafı bu. Düzeltmeler varlıkta biriktikçe gerçek koddan varlığa
        /// kayar ve yorumlar sessizce yalan söylemeye başlar. Bu düğme, bir
        /// ayarı beğendikten sonra onu kaynağa geri katlamayı iki saniyelik iş
        /// hâline getiriyor.
        /// </summary>
        void CopyPlaceLine()
        {
            if (_selection is not RectTransform rect) return;

            var min = rect.anchorMin; var max = rect.anchorMax;
            string line = $"UiKit.Place({rect.name.ToLowerInvariant()}, " +
                          $"{min.x:0.###}f, {min.y:0.###}f, {max.x:0.###}f, {max.y:0.###}f);";
            EditorGUIUtility.systemCopyBuffer = line;
            _status = "Panoya kopyalandı: " + line;
        }

        static string Shorten(string text)
        {
            if (string.IsNullOrEmpty(text)) return "(boş)";
            text = text.Replace("\n", " ");
            return text.Length <= 40 ? text : text.Substring(0, 38) + "…";
        }

        // ---------------------------------------------------------- eylemler

        void SaveStage()
        {
            if (_stage == null || !_stage.Ok) return;

            var baseline = new Dictionary<string, UiNodeSnapshot>();
            foreach (var pair in _stage.Baseline) baseline[pair.Key] = pair.Value;

            int written = UiTweakDiff.Save(baseline, _stage.ReadCurrent());
            _status = written == 0
                ? "Kaydedilecek fark yok."
                : $"{written} kayıt güncellendi → {UiTweakDiff.AssetPath}";
            RecountChanges();
        }

        void ResetNode(string path)
        {
            if (path == null || _stage == null) return;

            // Kayıttan silmek yetmez: tezgâhtaki nesne hâlâ düzeltilmiş hâlde
            // duruyor ve bir sonraki "Kaydet" onu geri yazardı. Nesne de taban
            // ölçüsüne döndürülüyor.
            UiTweakDiff.Clear(new[] { path });

            var node = FindByPath(path);
            if (node != null && _stage.Baseline.TryGetValue(path, out var was))
                ApplySnapshot(node, was);

            Touched();
            _status = "Öğe kodun verdiği hâle döndü: " + path;
        }

        void ResetStage()
        {
            if (_stage == null) return;

            var paths = new List<string>(_stage.Baseline.Keys);
            int removed = UiTweakDiff.Clear(paths);
            Rebuild();
            _status = removed == 0
                ? "Bu panelde kayıtlı düzeltme yoktu."
                : $"{removed} düzeltme silindi, panel kodun hâline döndü.";
        }

        static void ApplySnapshot(Transform node, in UiNodeSnapshot snap)
        {
            if (snap.isRect && node is RectTransform rect)
            {
                rect.anchorMin = snap.anchorMin; rect.anchorMax = snap.anchorMax;
                rect.offsetMin = snap.offsetMin; rect.offsetMax = snap.offsetMax;
                rect.pivot = snap.pivot;
            }

            node.localScale = snap.scale;

            if (snap.hasGraphic)
            {
                var graphic = node.GetComponent<Graphic>();
                if (graphic != null) graphic.color = snap.color;
            }

            if (snap.hasText)
            {
                var text = node.GetComponent<TMPro.TextMeshProUGUI>();
                if (text != null) text.fontSize = snap.fontSize;
            }

            if (node.gameObject.activeSelf != snap.active)
                node.gameObject.SetActive(snap.active);
        }

        // ------------------------------------------------------------ alanlar

        /// <summary>Etiket solda, sayı sağda duran satır — temanın alanıyla.</summary>
        static float FloatRow(string label, float value, float labelWidth = 84f)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, EditorSkin.RowLabel, GUILayout.Width(labelWidth));
                return EditorGUILayout.FloatField(value, EditorSkin.FieldStyle,
                    GUILayout.Height(18f));
            }
        }

        static Color ColorRow(string label, Color value, float labelWidth = 84f)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, EditorSkin.RowLabel, GUILayout.Width(labelWidth));
                return EditorGUILayout.ColorField(GUIContent.none, value,
                    showEyedropper: true, showAlpha: true, hdr: false,
                    GUILayout.Height(18f));
            }
        }
    }
}
