using System.Collections.Generic;
using BlockOut.Core;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Tahta dışındaki sekmeler: Pano, Galeri, Doğrula, Çözüm, Araçlar,
    /// Referans, Kılavuz.
    ///
    /// DERS (setin bütününü gören araç): Tek bölümü düzenlemek işin yarısıdır;
    /// diğer yarısı 50 bölümün BİRLİKTE nasıl durduğudur — zorluk artıyor mu,
    /// aynı tahta tekrar mı ediyor, kaç bölüm bozuk. Bu sorular tek bölüme
    /// bakarak cevaplanamaz, o yüzden her birinin kendi ekranı var.
    /// </summary>
    public sealed partial class LevelEditorWindow
    {
        // ================= PANO =================

        void DrawDashboardTab()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_dashboardScroll))
            {
                _dashboardScroll = scroll.scrollPosition;

                LevelLibrary.Tally(out int ok, out int warning, out int broken, out int unknown);
                int total = LevelLibrary.Count;

                SectionHeader("SETİN DURUMU", $"{total} bölüm · Levels klasöründen okundu");
                DrawHealthBar(ok, warning, broken, unknown, total);

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawCountCard("Sağlam", ok, LevelLibrary.Status.Ok);
                    DrawCountCard("Uyarılı", warning, LevelLibrary.Status.Warning);
                    DrawCountCard("Bozuk", broken, LevelLibrary.Status.Broken);
                    DrawCountCard("Bilinmiyor", unknown, LevelLibrary.Status.Unknown);
                }

                EditorGUILayout.Space(6);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (LevelEditorSkin.Button("Bütün seti doğrula", LevelEditorSkin.Accent,
                            0f, 26f, "Çözücüyü 50 bölümde koştur"))
                    {
                        LevelLibrary.ValidateAll(_palette, _config);
                        Say("Set doğrulandı");
                    }
                    if (LevelEditorSkin.Button("Listeyi diskten tazele", LevelEditorSkin.Neutral, 0f, 26f))
                    {
                        LevelLibrary.Invalidate();
                        Say("Liste tazelendi");
                    }
                    if (LevelEditorSkin.Button("Katalogu yeniden kur", LevelEditorSkin.Neutral, 0f, 26f))
                    {
                        ProjectSetup.ProjectSetupTool.EnsureLevelCatalog();
                        LevelLibrary.Invalidate();
                        Say("Oynanış sırası katalogdan tazelendi");
                    }
                }

                SectionHeader("DİKKAT GEREKENLER",
                    "Bozuk ve uyarılı bölümler. Satıra tıkla — editörde açılır.");

                bool any = false;
                foreach (var entry in LevelLibrary.Entries)
                {
                    if (entry.Status != LevelLibrary.Status.Broken &&
                        entry.Status != LevelLibrary.Status.Warning) continue;
                    any = true;
                    DrawProblemRow(entry);
                }
                if (!any)
                    EditorGUILayout.LabelField(
                        unknown == total
                            ? "Henüz doğrulama yapılmadı — yukarıdaki düğmeye bas."
                            : "Sorunlu bölüm yok.",
                        LevelEditorSkin.RowLabel);

                SectionHeader("ZORLUK EĞRİSİ",
                    "Oynanış sırasına göre hamle sayısı. Çubuğa tıkla — bölüm açılır. " +
                    "Doğrulanmamış bölümler boş görünür. Aranan şey düzgün bir tırmanış: " +
                    "ani sıçrama oyuncuyu duvara çarptırır, düzlük sıkıcıdır.");
                DrawDifficultyCurve();

                SectionHeader("MEKANİK TANITIM SIRASI",
                    "Her mekanik ilk hangi bölümde çıkıyor. Oyun yeni mekaniği ilk " +
                    "görüldüğü bölümde tanıtıyor (NewItemPanel), yani bu sıra oyuncunun " +
                    "öğrenme sırasıdır.");
                DrawMechanicIntroOrder();

                SectionHeader("TASARIM DAĞILIMI", "Zorluk ve tahta boyutu — sette denge var mı?");
                DrawDistributions();
            }
        }

        void DrawHealthBar(int ok, int warning, int broken, int unknown, int total)
        {
            var bar = GUILayoutUtility.GetRect(0, 12, GUILayout.ExpandWidth(true));
            LevelEditorSkin.RoundedRect(bar, LevelEditorSkin.Neutral, LevelEditorSkin.Round4);
            if (total == 0) return;

            float x = bar.x;
            void Segment(int count, LevelLibrary.Status status)
            {
                if (count == 0) return;
                float w = bar.width * count / total;
                LevelEditorSkin.Fill(new Rect(x, bar.y, w, bar.height), LevelLibrary.ColorFor(status));
                x += w;
            }

            Segment(ok, LevelLibrary.Status.Ok);
            Segment(warning, LevelLibrary.Status.Warning);
            Segment(broken, LevelLibrary.Status.Broken);
            Segment(unknown, LevelLibrary.Status.Unknown);
        }

        static void DrawCountCard(string label, int count, LevelLibrary.Status status)
        {
            using (LevelEditorSkin.CardScope(GUILayout.Height(62)))
            {
                var numberStyle = new GUIStyle(LevelEditorSkin.Metric);
                numberStyle.normal.textColor = LevelLibrary.ColorFor(status);
                GUILayout.Label(count.ToString(), numberStyle, GUILayout.Height(28));

                var labelStyle = new GUIStyle(LevelEditorSkin.SectionBody)
                { alignment = TextAnchor.MiddleCenter, wordWrap = false };
                GUILayout.Label(label.ToUpperInvariant(), labelStyle);
            }
        }

        void DrawProblemRow(LevelLibrary.Entry entry)
        {
            var row = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            if (row.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(row, new Color(1f, 1f, 1f, 0.05f));

            StatusDot(new Rect(row.x + 4, row.y + 6, 8, 8), entry.Status);
            GUI.Label(new Rect(row.x + 18, row.y + 2, 120, 16), entry.Name, LevelEditorSkin.RowLabel);
            GUI.Label(new Rect(row.x + 140, row.y + 2, row.width - 146, 16),
                entry.Summary, LevelEditorSkin.RowLabel);

            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                RequestOpen(entry.Path);
                RequestTab(Tab.Board);
            }
        }

        /// <summary>
        /// Hamle sayısı çubuk grafiği, oynanış sırasında.
        ///
        /// DERS (eğriyi GÖRMEDEN dengeleyemezsin): "24. bölüm zor" tek bir
        /// veridir; zorluğun bölüm bölüm nasıl tırmandığı ancak elli sayıyı yan
        /// yana koyunca görünür. Bu yüzden grafik listeden daha iyidir — göz
        /// sıçramayı okur, sayı okumaz.
        /// </summary>
        void DrawDifficultyCurve()
        {
            var list = LevelLibrary.Entries;
            if (list.Count == 0) return;

            const float height = 108f;
            var area = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            LevelEditorSkin.RoundedRect(area, LevelEditorSkin.Card);

            var plot = new Rect(area.x + 8f, area.y + 8f, area.width - 16f, area.height - 26f);

            int peak = 1;
            foreach (var entry in list) peak = Mathf.Max(peak, entry.Moves);

            // Yatay kılavuz çizgileri — yükseklik okunabilir olsun.
            for (int i = 1; i <= 3; i++)
            {
                float y = plot.yMax - plot.height * i / 4f;
                LevelEditorSkin.Fill(new Rect(plot.x, y, plot.width, 1f),
                    new Color(1f, 1f, 1f, 0.045f));
            }

            float slot = plot.width / list.Count;
            float barWidth = Mathf.Max(2f, slot - 2f);
            var mouse = Event.current.mousePosition;

            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                float x = plot.x + i * slot;
                var slotRect = new Rect(x, plot.y, slot, plot.height);

                if (entry.Moves <= 0)
                {
                    // Doğrulanmamış: yalnız zeminde bir iz bırak.
                    LevelEditorSkin.Fill(new Rect(x, plot.yMax - 2f, barWidth, 2f),
                        LevelEditorSkin.TextDim);
                }
                else
                {
                    float ratio = (float)entry.Moves / peak;
                    float barHeight = Mathf.Max(2f, plot.height * ratio);
                    var bar = new Rect(x, plot.yMax - barHeight, barWidth, barHeight);

                    // Renk ZORLUKTAN gelir, hamle sayısından değil — böylece
                    // "hard işaretli ama kısa" bölümler gözle yakalanır.
                    var tint = entry.Difficulty == "superhard" ? LevelEditorSkin.DangerBright
                        : entry.Difficulty == "hard" ? LevelEditorSkin.WarningBright
                        : LevelEditorSkin.AccentBright;

                    if (entry.Path == _path) tint = Color.white;
                    LevelEditorSkin.Fill(bar, tint);
                }

                if (slotRect.Contains(mouse))
                {
                    LevelEditorSkin.Fill(slotRect, new Color(1f, 1f, 1f, 0.06f));
                    GUI.Label(new Rect(area.x + 8f, area.yMax - 17f, area.width - 16f, 14f),
                        $"{i + 1}. {entry.Name} · {entry.Moves} hamle · {entry.Difficulty} · " +
                        $"{entry.Width}×{entry.Height}",
                        LevelEditorSkin.RowLabel);

                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                    {
                        RequestOpen(entry.Path);
                        RequestTab(Tab.Board);
                        Event.current.Use();
                    }
                }
            }

            GUI.Label(new Rect(area.xMax - 96f, area.y + 4f, 90f, 14f),
                $"en yüksek {peak}",
                new GUIStyle(LevelEditorSkin.RowLabel) { alignment = TextAnchor.MiddleRight });
        }

        /// <summary>Her mekaniğin ilk göründüğü bölüm — öğrenme sırası.</summary>
        void DrawMechanicIntroOrder()
        {
            var list = LevelLibrary.Entries;

            foreach (var (flag, badge, label, tint) in LevelMechanics.All)
            {
                int first = -1;
                int count = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    if ((list[i].Mechanics & flag) == 0) continue;
                    if (first < 0) first = i;
                    count++;
                }

                var row = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
                if (row.Contains(Event.current.mousePosition))
                    LevelEditorSkin.Fill(row, new Color(1f, 1f, 1f, 0.04f));

                var badgeBox = new Rect(row.x + 4f, row.y + 4f, 16f, 12f);
                LevelEditorSkin.RoundedRect(badgeBox,
                    new Color(tint.r, tint.g, tint.b, 0.22f), LevelEditorSkin.Round4);
                var badgeStyle = new GUIStyle(LevelEditorSkin.RowLabel)
                { alignment = TextAnchor.MiddleCenter, fontSize = 8 };
                badgeStyle.normal.textColor = tint;
                GUI.Label(badgeBox, badge, badgeStyle);

                GUI.Label(new Rect(row.x + 26f, row.y + 2f, 150f, 16f), label,
                    LevelEditorSkin.RowLabel);

                if (first < 0)
                {
                    var dim = new GUIStyle(LevelEditorSkin.RowLabel);
                    dim.normal.textColor = LevelEditorSkin.TextDim;
                    GUI.Label(new Rect(row.x + 180f, row.y + 2f, 220f, 16f),
                        "sette hiç kullanılmıyor", dim);
                    continue;
                }

                GUI.Label(new Rect(row.x + 180f, row.y + 2f, 260f, 16f),
                    $"ilk: {first + 1}. bölüm ({list[first].Name})  ·  {count} bölümde",
                    LevelEditorSkin.Value);

                if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                {
                    RequestOpen(list[first].Path);
                    RequestTab(Tab.Board);
                }
            }
        }

        void DrawDistributions()
        {
            var difficulty = new Dictionary<string, int>();
            var sizes = new Dictionary<string, int>();

            foreach (var entry in LevelLibrary.Entries)
            {
                if (entry.Data == null) continue;
                string diff = entry.Difficulty;
                difficulty[diff] = difficulty.TryGetValue(diff, out int d) ? d + 1 : 1;
                string size = $"{entry.Width}×{entry.Height}";
                sizes[size] = sizes.TryGetValue(size, out int s) ? s + 1 : 1;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (LevelEditorSkin.CardScope())
                {
                    EditorGUILayout.LabelField("Zorluk", LevelEditorSkin.Value);
                    foreach (var name in Difficulties)
                        EditorGUILayout.LabelField(
                            $"{name}: {(difficulty.TryGetValue(name, out int c) ? c : 0)}",
                            LevelEditorSkin.RowLabel);
                }

                using (LevelEditorSkin.CardScope())
                {
                    EditorGUILayout.LabelField("Tahta boyutu", LevelEditorSkin.Value);
                    foreach (var pair in sizes)
                        EditorGUILayout.LabelField($"{pair.Key}: {pair.Value}", LevelEditorSkin.RowLabel);
                }
            }
        }

        // ================= GALERİ =================

        void DrawGalleryTab()
        {
            using (LevelEditorSkin.BarScope())
            {
                _gallerySearch = EditorGUILayout.TextField(_gallerySearch,
                    EditorStyles.toolbarSearchField, GUILayout.Width(180));

                _galleryStatusFilter = EditorGUILayout.Popup(_galleryStatusFilter,
                    new[] { "hepsi", "sağlam", "uyarılı", "bozuk", "doğrulanmadı" },
                    EditorStyles.toolbarPopup, GUILayout.Width(110));

                _galleryMechanicFilter = EditorGUILayout.Popup(_galleryMechanicFilter,
                    MechanicFilterNames, EditorStyles.toolbarPopup, GUILayout.Width(150));

                GUILayout.FlexibleSpace();
                GUILayout.Label("Boyut", LevelEditorSkin.RowLabel, GUILayout.Width(38));
                _galleryScale = GUILayout.HorizontalSlider(_galleryScale, 0.6f, 2.4f, GUILayout.Width(110));

                if (LevelEditorSkin.BarButton("Tazele", 54f))
                { LevelLibrary.Invalidate(); Say("Galeri tazelendi"); }

                if (LevelEditorSkin.BarButton("Hepsini doğrula", 104f))
                { LevelLibrary.ValidateAll(_palette, _config); Say("Set doğrulandı"); }
            }

            float cardWidth = 118f * _galleryScale;
            float cardHeight = 168f * _galleryScale;

            using (var scroll = new EditorGUILayout.ScrollViewScope(_galleryScroll))
            {
                _galleryScroll = scroll.scrollPosition;

                // Kaç kart bir satıra sığar: pencere genişliğinden hesaplanır,
                // sabit bir kolon sayısı dar pencerede taşar, geniş pencerede
                // boş yer bırakır.
                int perRow = Mathf.Max(1, Mathf.FloorToInt(
                    (position.width - LibraryPad) / (cardWidth + 6f)));
                int drawn = 0;
                bool rowOpen = false;

                var list = LevelLibrary.Entries;
                for (int i = 0; i < list.Count; i++)
                {
                    var entry = list[i];
                    if (!Matches(entry, _gallerySearch)) continue;
                    if (!PassesStatusFilter(entry)) continue;
                    if (!PassesMechanicFilter(entry)) continue;

                    if (drawn % perRow == 0)
                    {
                        if (rowOpen) EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        rowOpen = true;
                    }
                    drawn++;

                    DrawGalleryCard(entry, i, cardWidth, cardHeight);
                }
                if (rowOpen) { GUILayout.FlexibleSpace(); EditorGUILayout.EndHorizontal(); }

                if (drawn == 0)
                    EditorGUILayout.LabelField("Süzgece uyan bölüm yok.", LevelEditorSkin.RowLabel);
            }
        }

        const float LibraryPad = 40f;

        /// <summary>Süzgeç açılır listesi: "hepsi" + her mekanik.</summary>
        static string[] _mechanicFilterNames;

        static string[] MechanicFilterNames
        {
            get
            {
                if (_mechanicFilterNames != null) return _mechanicFilterNames;

                _mechanicFilterNames = new string[LevelMechanics.All.Length + 1];
                _mechanicFilterNames[0] = "mekanik: hepsi";
                for (int i = 0; i < LevelMechanics.All.Length; i++)
                    _mechanicFilterNames[i + 1] = LevelMechanics.All[i].Label;
                return _mechanicFilterNames;
            }
        }

        bool PassesMechanicFilter(LevelLibrary.Entry entry)
        {
            if (_galleryMechanicFilter <= 0) return true;
            var flag = LevelMechanics.All[_galleryMechanicFilter - 1].Flag;
            return (entry.Mechanics & flag) != 0;
        }

        /// <summary>
        /// Kartın alt kenarındaki mekanik rozetleri. Galeride "hangi bölümde ne
        /// var" sorusunu bölümü açmadan cevaplar.
        /// </summary>
        static void DrawMechanicBadges(Rect row, Mechanic mechanics)
        {
            float x = row.x;
            var style = new GUIStyle(LevelEditorSkin.RowLabel)
            { alignment = TextAnchor.MiddleCenter, fontSize = 8 };

            foreach (var (flag, badge, label, tint) in LevelMechanics.All)
            {
                if ((mechanics & flag) == 0) continue;
                float width = badge.Length > 1 ? 15f : 11f;
                if (x + width > row.xMax) return;

                var box = new Rect(x, row.y, width, row.height);
                LevelEditorSkin.RoundedRect(box,
                    new Color(tint.r, tint.g, tint.b, 0.22f), LevelEditorSkin.Round4);
                style.normal.textColor = tint;
                GUI.Label(box, new GUIContent(badge, label), style);
                x += width + 2f;
            }
        }

        bool PassesStatusFilter(LevelLibrary.Entry entry)
        {
            switch (_galleryStatusFilter)
            {
                case 1: return entry.Status == LevelLibrary.Status.Ok;
                case 2: return entry.Status == LevelLibrary.Status.Warning;
                case 3: return entry.Status == LevelLibrary.Status.Broken;
                case 4: return entry.Status == LevelLibrary.Status.Unknown;
                default: return true;
            }
        }

        /// <summary>
        /// Tek bölüm kartı: minyatür + kimlik + ölçüler. Çerçeve rengi durum
        /// göstergesidir — referans editörde de kartlar renkli çerçeveyle
        /// işaretlenmiş; bir bakışta "hangi bölüm bozuk" sorusunu cevaplıyor.
        /// </summary>
        void DrawGalleryCard(LevelLibrary.Entry entry, int order, float width, float height)
        {
            var card = GUILayoutUtility.GetRect(width, height,
                GUILayout.Width(width), GUILayout.Height(height));
            bool current = entry.Path == _path;
            bool hover = card.Contains(Event.current.mousePosition);

            EditorGUI.DrawRect(card, new Color(0.10f, 0.10f, 0.13f));
            LevelCanvasDrawer.Outline(card,
                current ? Color.white : entry.StatusColor, current ? 3f : 2f);
            if (hover) EditorGUI.DrawRect(card, new Color(1f, 1f, 1f, 0.04f));

            var thumbArea = new Rect(card.x + 5, card.y + 5, card.width - 10, card.height - 58);
            DrawMechanicBadges(new Rect(card.x + 6, card.yMax - 51, card.width - 12, 11),
                entry.Mechanics);
            var texture = LevelLibrary.Thumbnail(entry, _palette);
            if (texture != null) GUI.DrawTexture(thumbArea, texture, ScaleMode.ScaleToFit);
            else LevelCanvasDrawer.Label(thumbArea, "okunamadı",
                LevelLibrary.ColorFor(LevelLibrary.Status.Broken), 10);

            var nameStyle = new GUIStyle(LevelEditorSkin.Value) { alignment = TextAnchor.MiddleCenter };
            nameStyle.normal.textColor = current ? Color.white : new Color(0.85f, 0.85f, 0.9f);
            GUI.Label(new Rect(card.x, card.yMax - 38, card.width, 14),
                $"{order + 1}. {entry.Name}", nameStyle);

            var metaStyle = new GUIStyle(LevelEditorSkin.RowLabel) { alignment = TextAnchor.MiddleCenter };
            metaStyle.normal.textColor = new Color(0.55f, 0.55f, 0.62f);
            GUI.Label(new Rect(card.x, card.yMax - 25, card.width, 12),
                entry.Data != null
                    ? $"{entry.Width}×{entry.Height} · {entry.BlockCount} blok · {entry.GateCount} kapı"
                    : "bozuk dosya",
                metaStyle);
            GUI.Label(new Rect(card.x, card.yMax - 14, card.width, 12),
                entry.Data != null ? $"{entry.Difficulty} · {entry.Summary}" : entry.Summary, metaStyle);

            var e = Event.current;
            if (e.type == EventType.MouseDown && card.Contains(e.mousePosition))
            {
                if (e.button == 1) { ShowGalleryContextMenu(entry); e.Use(); }
                else if (!current)
                {
                    RequestOpen(entry.Path);
                    RequestTab(Tab.Board);
                    e.Use();
                }
            }
        }

        void ShowGalleryContextMenu(LevelLibrary.Entry entry)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Tahtada aç"), false, () =>
            { RequestTab(Tab.Board); RequestOpen(entry.Path); });

            menu.AddItem(new GUIContent("Play test"), false, () =>
            {
                if (entry.Data != null) LevelEditorIO.PlayTest(entry.Data);
            });

            menu.AddItem(new GUIContent("Doğrula"), false, () =>
            { LevelLibrary.Validate(entry, _palette, _config); Repaint(); });

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Çoğalt"), false, () =>
            { LevelLibrary.Duplicate(entry.Path); Say("Kopya eklendi"); });

            menu.AddItem(new GUIContent("Sil…"), false, () => DeleteLevel(entry.Path));
            menu.ShowAsContext();
        }

        // ================= DOĞRULA =================

        void DrawValidateTab()
        {
            using (LevelEditorSkin.BarScope())
            {
                if (LevelEditorSkin.BarButton("Bütün seti doğrula", 120f, null, LevelEditorSkin.Accent))
                { LevelLibrary.ValidateAll(_palette, _config); Say("Set doğrulandı"); }

                GUILayout.Label("Sırala", LevelEditorSkin.RowLabel, GUILayout.Width(38));
                _validateSort = EditorGUILayout.Popup(_validateSort,
                    new[] { "oynanış sırası", "duruma göre", "hamle sayısına göre" },
                    EditorStyles.toolbarPopup, GUILayout.Width(150));

                GUILayout.FlexibleSpace();
                LevelLibrary.Tally(out int ok, out int warning, out int broken, out int unknown);
                GUILayout.Label($"{ok} sağlam · {warning} uyarılı · {broken} bozuk · {unknown} bilinmiyor",
                    LevelEditorSkin.RowLabel);
            }

            var rows = new List<LevelLibrary.Entry>(LevelLibrary.Entries);
            switch (_validateSort)
            {
                case 1: rows.Sort((a, b) => b.Status.CompareTo(a.Status)); break;
                case 2: rows.Sort((a, b) => b.Moves.CompareTo(a.Moves)); break;
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_validateScroll))
            {
                _validateScroll = scroll.scrollPosition;
                foreach (var entry in rows) DrawValidateRow(entry);
            }

            DrawCurrentReport();
        }

        void DrawValidateRow(LevelLibrary.Entry entry)
        {
            var row = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            bool current = entry.Path == _path;

            if (current) EditorGUI.DrawRect(row, new Color(0.28f, 0.36f, 0.52f, 0.5f));
            else if (row.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(row, new Color(1f, 1f, 1f, 0.04f));

            StatusDot(new Rect(row.x + 6, row.y + 7, 9, 9), entry.Status);

            GUI.Label(new Rect(row.x + 22, row.y + 3, 130, 16), entry.Name, LevelEditorSkin.RowLabel);
            GUI.Label(new Rect(row.x + 152, row.y + 3, 70, 16),
                entry.Data != null ? $"{entry.Width}×{entry.Height}" : "—", LevelEditorSkin.RowLabel);
            GUI.Label(new Rect(row.x + 222, row.y + 3, 60, 16),
                $"{entry.BlockCount} blok", LevelEditorSkin.RowLabel);
            GUI.Label(new Rect(row.x + 282, row.y + 3, 60, 16),
                $"{entry.GateCount} kapı", LevelEditorSkin.RowLabel);
            GUI.Label(new Rect(row.x + 342, row.y + 3, 70, 16),
                entry.Moves > 0 ? entry.Moves + " hamle" : "—", LevelEditorSkin.RowLabel);
            GUI.Label(new Rect(row.x + 412, row.y + 3, 70, 16),
                entry.Difficulty, LevelEditorSkin.RowLabel);

            var summaryStyle = new GUIStyle(LevelEditorSkin.RowLabel);
            summaryStyle.normal.textColor = entry.StatusColor;
            GUI.Label(new Rect(row.x + 486, row.y + 3, row.width - 492, 16),
                entry.Summary, summaryStyle);

            if (GUI.Button(row, GUIContent.none, GUIStyle.none) && !current)
                RequestOpen(entry.Path);
        }

        /// <summary>Açık bölümün ayrıntılı raporu — hata ve uyarı metinleri.</summary>
        void DrawCurrentReport()
        {
            if (_report == null) return;

            using (LevelEditorSkin.CardScope(GUILayout.Height(140)))
            {
                var style = new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = _report.Ok
                    ? LevelLibrary.ColorFor(LevelLibrary.Status.Ok)
                    : LevelLibrary.ColorFor(LevelLibrary.Status.Broken);

                EditorGUILayout.LabelField(_report.Ok
                    ? $"✓ {_data.Id} oynanabilir — {_report.Solution.Moves.Count} hamle"
                    : $"⚠ {_data.Id} sorunlu", style);

                var messages = new List<string>(_report.AllMessages);
                if (messages.Count == 0)
                {
                    EditorGUILayout.LabelField("Uyarı yok.", LevelEditorSkin.RowLabel);
                }
                else
                {
                    using (var scroll = new EditorGUILayout.ScrollViewScope(_reportScroll))
                    {
                        _reportScroll = scroll.scrollPosition;
                        foreach (var line in messages)
                            EditorGUILayout.LabelField("• " + line, LevelEditorSkin.SectionBody);
                    }
                }
            }
        }

        // ================= ÇÖZÜM =================

        /// <summary>
        /// Çözümü hamle hamle oynatır.
        ///
        /// DERS (bölümü ANLAMAK ölçmekten farklıdır): "24 hamlede çözülüyor"
        /// bir sayıdır; hangi hamlenin zorunlu, nerede oyuncunun tek seçeneği
        /// kaldığı ancak sırayı izleyerek görülür. Zorluk ayarı burada yapılır.
        /// </summary>
        void DrawSolutionTab()
        {
            var solution = _report?.Solution;

            using (LevelEditorSkin.BarScope())
            {
                if (LevelEditorSkin.BarButton("Yeniden çöz", 84f))
                { _validationStale = true; Say("Çözücü yeniden koşuyor"); }

                _showSolution = LevelEditorSkin.BarToggle(_showSolution, "Rozetleri göster", 112f);

                using (new EditorGUI.DisabledScope(solution == null || solution.Moves.Count == 0))
                {
                    if (LevelEditorSkin.BarButton("⏮", 28f))
                        _playbackStep = -1;
                    if (LevelEditorSkin.BarButton("◀", 28f))
                        _playbackStep = Mathf.Max(-1, _playbackStep - 1);
                    if (LevelEditorSkin.BarButton("▶", 28f))
                        _playbackStep = Mathf.Min((solution?.Moves.Count ?? 1) - 1, _playbackStep + 1);

                    int count = solution?.Moves.Count ?? 0;
                    _playbackStep = Mathf.RoundToInt(GUILayout.HorizontalSlider(
                        _playbackStep, -1, Mathf.Max(0, count - 1)));
                    GUILayout.Label(_playbackStep < 0 ? "tümü" : $"{_playbackStep + 1}/{count}",
                        LevelEditorSkin.RowLabel, GUILayout.Width(56));
                }
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
                using (var scroll = new EditorGUILayout.ScrollViewScope(_solutionScroll))
                {
                    _solutionScroll = scroll.scrollPosition;

                    if (solution == null)
                    {
                        LevelEditorSkin.Note("Bu bölüm henüz doğrulanmadı.");
                    }
                    else if (!solution.Solved)
                    {
                        LevelEditorSkin.Note(
                            "Çözücü bu bölümü çözemedi. Kalan blok: " + solution.RemainingBlocks,
                            LevelEditorSkin.NoteKind.Danger);
                    }
                    else
                    {
                        SectionHeader("ÖLÇÜMLER");
                        Metric("Hamle", solution.Moves.Count.ToString());
                        Metric("Açılış seçeneği", solution.InitialOptions.ToString());
                        Metric("Zorunlu hamle", $"{solution.ForcedSteps}/{solution.Moves.Count}");
                        Metric("Ortalama seçenek", solution.AverageOptions.ToString("0.0"));
                        Metric("Karıştırma adımı", solution.ShuffleSteps.ToString());
                        Metric("Tahmini süre", $"~{_report.EstimatedSeconds} sn");
                        Metric("Verilen süre", _data.TimeSeconds + " sn");

                        if (_report.EstimatedSeconds > _data.TimeSeconds)
                            LevelEditorSkin.Note(
                                "Verilen süre tahminin altında — bölüm süreden dolayı kaybedilebilir.",
                                LevelEditorSkin.NoteKind.Warning);

                        SectionHeader("RENK DAĞILIMI");
                        DrawColorSummary();

                        SectionHeader("HAMLE LİSTESİ", "Satıra tıkla — o ana kadarki hamleler tuvalde.");
                        for (int i = 0; i < solution.Moves.Count; i++) DrawMoveRow(solution.Moves[i], i);
                    }
                }

                using (new EditorGUILayout.VerticalScope())
                    DrawCanvas(interactive: false);
            }
        }

        static void Metric(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, LevelEditorSkin.RowLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField(value, LevelEditorSkin.Value);
            }
        }

        void DrawMoveRow(LevelSolver.Move move, int index)
        {
            var row = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            bool current = _playbackStep == index;

            if (current) EditorGUI.DrawRect(row, new Color(0.30f, 0.40f, 0.56f, 0.6f));
            else if (row.Contains(Event.current.mousePosition))
                EditorGUI.DrawRect(row, new Color(1f, 1f, 1f, 0.04f));

            LevelCanvasDrawer.Fill(new Rect(row.x + 2, row.y + 4, 10, 10), ColorOf(move.Color));
            GUI.Label(new Rect(row.x + 16, row.y + 1, row.width - 20, 16),
                $"{index + 1}. ({move.X},{move.Y}) {move.Outcome} · {move.Options} seçenek",
                LevelEditorSkin.RowLabel);

            if (GUI.Button(row, GUIContent.none, GUIStyle.none))
            {
                _playbackStep = index;
                _showSolution = true;
                Repaint();
            }
        }

        // ================= ARAÇLAR =================

        void DrawToolsTab()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_toolsScroll))
            {
                _toolsScroll = scroll.scrollPosition;

                SectionHeader("TAHTA",
                    "Izgara ölçüsü. Küçültmek dışarıda kalan blokları SİLMEZ — " +
                    "doğrulama onları taşma olarak işaretler.");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    int w = EditorGUILayout.IntField("Genişlik", _data.Board.Width, GUILayout.Width(160));
                    int h = EditorGUILayout.IntField("Yükseklik", _data.Board.Height, GUILayout.Width(160));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Record();
                        ResizeBoard(Mathf.Clamp(w, 3, 16), Mathf.Clamp(h, 3, 20));
                        AfterChange();
                    }
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Satır", GUILayout.Width(70)))
                        Resize(0, 1);
                    if (GUILayout.Button("− Satır", GUILayout.Width(70)))
                        Resize(0, -1);
                    if (GUILayout.Button("+ Kolon", GUILayout.Width(70)))
                        Resize(1, 0);
                    if (GUILayout.Button("− Kolon", GUILayout.Width(70)))
                        Resize(-1, 0);
                    if (GUILayout.Button(new GUIContent("Kırp",
                            "Kenarlardaki tamamen boş satır/kolonları at"), GUILayout.Width(70)))
                        TrimBoard();
                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Tüm hücreleri aç", GUILayout.Width(130))) FillBoard('X');
                    if (GUILayout.Button("Tüm hücreleri kapat", GUILayout.Width(130))) FillBoard('.');
                    GUILayout.FlexibleSpace();
                }

                SectionHeader("DÖNÜŞÜMLER",
                    "Bölümün tamamını aynalar/döndürür — varyant üretmenin en hızlı yolu. " +
                    "Bloklar, kapılar, duvarlar ve perdeler birlikte taşınır.");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("⇄ Yatay aynala", GUILayout.Height(26)))
                        Transform(LevelTransform.MirrorHorizontal, "Yatay aynalandı");
                    if (GUILayout.Button("⇅ Dikey aynala", GUILayout.Height(26)))
                        Transform(LevelTransform.MirrorVertical, "Dikey aynalandı");
                    if (GUILayout.Button("⟳ 180° döndür", GUILayout.Height(26)))
                        Transform(LevelTransform.Rotate180, "180° döndürüldü");
                }

                SectionHeader("SÜRE VE ZORLUK",
                    "Süre elle yazılırsa tahminden kopar. Çözücünün ölçtüğü süreyi " +
                    "doğrudan uygulamak bu kopmayı engeller.");

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(_report == null || !_report.Ok))
                        if (GUILayout.Button(_report != null && _report.Ok
                                ? $"Süreyi tahmine ayarla ({_report.EstimatedSeconds} sn)"
                                : "Süreyi tahmine ayarla", GUILayout.Height(24)))
                        {
                            Record();
                            _data.TimeSeconds = _report.EstimatedSeconds;
                            AfterChange();
                            Say("Süre " + _data.TimeSeconds + " sn olarak ayarlandı");
                        }

                    int diff = Mathf.Max(0, System.Array.IndexOf(Difficulties, _data.Difficulty));
                    EditorGUI.BeginChangeCheck();
                    diff = EditorGUILayout.Popup(diff, Difficulties, GUILayout.Width(120));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Record();
                        _data.Difficulty = Difficulties[diff];
                        AfterChange();
                    }
                    GUILayout.FlexibleSpace();
                }

                SectionHeader("SET İŞLEMLERİ");
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Bu bölümü çoğalt", GUILayout.Height(24)))
                    {
                        if (_path == null) Say("Önce kaydet");
                        else
                        {
                            string source = _path;
                            EditorApplication.delayCall += () =>
                            {
                                if (this == null || !ConfirmDiscard()) return;
                                LoadFrom(LevelLibrary.Duplicate(source));
                            };
                        }
                    }
                    if (GUILayout.Button("Katalogu yeniden kur", GUILayout.Height(24)))
                    {
                        ProjectSetup.ProjectSetupTool.EnsureLevelCatalog();
                        LevelLibrary.Invalidate();
                        Say("Oynanış sırası tazelendi");
                    }
                    GUILayout.FlexibleSpace();
                }
            }
        }

        void Resize(int dw, int dh)
        {
            Record();
            ResizeBoard(Mathf.Clamp(_data.Board.Width + dw, 3, 16),
                        Mathf.Clamp(_data.Board.Height + dh, 3, 20));
            AfterChange();
        }

        void FillBoard(char value)
        {
            Record();
            for (int y = 0; y < _data.Board.Rows.Count; y++)
                _data.Board.Rows[y] = new string(value, _data.Board.Width);
            AfterChange();
        }

        /// <summary>Kenarlardaki tamamen kapalı satır/kolonları atar ve içeriği kaydırır.</summary>
        void TrimBoard()
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = 0; y < _data.Board.Height; y++)
                for (int x = 0; x < _data.Board.Width; x++)
                {
                    if (!Playable(x, y)) continue;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }

            if (maxX < 0) { Say("Tahtada açık hücre yok"); return; }
            if (minX == 0 && minY == 0 && maxX == _data.Board.Width - 1 && maxY == _data.Board.Height - 1)
            { Say("Kırpılacak boş kenar yok"); return; }

            Record();

            var rows = new List<string>();
            for (int y = minY; y <= maxY; y++)
                rows.Add(_data.Board.Rows[y].Substring(minX, maxX - minX + 1));

            _data.Board.Rows = rows;
            _data.Board.Width = maxX - minX + 1;
            _data.Board.Height = maxY - minY + 1;

            // Izgara kaydığı için tahtadaki HER ŞEY aynı kadar kaydırılmalı;
            // yoksa bloklar tahtanın dışında kalır.
            foreach (var block in _data.Blocks) { block.X -= minX; block.Y -= minY; }
            foreach (var gate in _data.Gates) { gate.X -= minX; gate.Y -= minY; }
            foreach (var wall in _data.Board.Walls) { wall.X -= minX; wall.Y -= minY; }
            foreach (var obstacle in _data.Obstacles)
            {
                LevelEditorIO.SetInt(obstacle, "x", LevelEditorIO.GetInt(obstacle, "x") - minX);
                LevelEditorIO.SetInt(obstacle, "y", LevelEditorIO.GetInt(obstacle, "y") - minY);

                var contents = LevelEditorIO.GetContents(obstacle);
                if (contents.Count == 0) continue;
                foreach (var hidden in contents) { hidden.X -= minX; hidden.Y -= minY; }
                LevelEditorIO.SetContents(obstacle, contents);
            }

            _selections.Clear();
            AfterChange();
            Say($"Kırpıldı — {_data.Board.Width}×{_data.Board.Height}");
        }

        void Transform(System.Action<LevelData> operation, string message)
        {
            Record();
            operation(_data);
            _selections.Clear();
            AfterChange();
            Say(message);
        }

        // ================= REFERANS =================

        void DrawReferenceTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(360)))
                {
                    SectionHeader("BİNDİRME",
                        "Referans kareyi tuvalin altına/üstüne koyar. Bölümü videodan " +
                        "birebir çıkarmanın yolu: kareyi bindir, ölçeği hücrelere " +
                        "oturtana kadar ayarla, sonra üstünden çiz.");

                    _reference.Visible = EditorGUILayout.ToggleLeft("Tuvalde göster", _reference.Visible);
                    _reference.Behind = EditorGUILayout.ToggleLeft(
                        new GUIContent("Izgaranın arkasında",
                            "Kapalıysa görsel ızgaranın ÜSTÜNE çizilir — hizalarken kullanışlı"),
                        _reference.Behind);

                    if (GUILayout.Button("Görsel seç…", GUILayout.Height(24)))
                    {
                        string path = EditorUtility.OpenFilePanel("Referans görsel", "", "png,jpg,jpeg");
                        if (!string.IsNullOrEmpty(path)) { _reference.SetImage(path); Say("Görsel yüklendi"); }
                    }

                    _reference.Opacity = EditorGUILayout.Slider("Opaklık", _reference.Opacity, 0f, 1f);
                    _reference.Scale = EditorGUILayout.Slider("Ölçek", _reference.Scale, 0.2f, 3f);
                    _reference.Offset = EditorGUILayout.Vector2Field("Kaydırma", _reference.Offset);

                    SectionHeader("VİDEODAN KARE",
                        "ffmpeg'i bir kez seç, sonra zaman damgası yazıp kare al. " +
                        "Referans oynanış videoları OneDrive/Masaüstü/Block Out! Videos altında.");

                    DrawPathRow("ffmpeg", LevelReferenceOverlay.FfmpegPath, "exe",
                        path => LevelReferenceOverlay.FfmpegPath = path);
                    DrawPathRow("Video", LevelReferenceOverlay.VideoPath, "mp4,mov,mkv",
                        path => LevelReferenceOverlay.VideoPath = path);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _reference.Timestamp = EditorGUILayout.TextField("Zaman", _reference.Timestamp);
                        if (GUILayout.Button("Kare al", GUILayout.Width(74)))
                        {
                            if (_reference.ExtractFrame(out string error))
                            { _reference.Visible = true; Say("Kare alındı: " + _reference.Timestamp); }
                            else EditorUtility.DisplayDialog("Kare alınamadı", error, "Tamam");
                        }
                    }

                    LevelEditorSkin.Note(
                        "Zaman biçimi: 00:02:34 ya da 154 (saniye). Bölüm başlangıcı için " +
                        "sahne geçişine ~3-4 saniye ekle — referansta kamera içeri zumlarken " +
                        "ızgara okunmuyor.");
                }

                using (new EditorGUILayout.VerticalScope())
                    DrawCanvas(interactive: false);
            }
        }

        static void DrawPathRow(string label, string current, string extensions,
            System.Action<string> onPick)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(56));
                EditorGUILayout.LabelField(
                    string.IsNullOrEmpty(current) ? "(ayarlı değil)" : System.IO.Path.GetFileName(current),
                    LevelEditorSkin.RowLabel);
                if (GUILayout.Button("Seç…", GUILayout.Width(52)))
                {
                    string path = EditorUtility.OpenFilePanel(label, "", extensions);
                    if (!string.IsNullOrEmpty(path)) onPick(path);
                }
            }
        }

        // ================= KILAVUZ =================

        static readonly (string Keys, string Action)[] Shortcuts =
        {
            ("1 – 7", "Araç seç: Seç · Şekil · Blok · Kapı · Duvar · Perde · Üreteç"),
            ("Sol tık", "Aracın işlemini uygula"),
            ("Sağ tık", "Sil (blok, kapı, duvar, perde)"),
            ("Sürükle", "Şekil boya · kutu seçim · nesne taşı"),
            ("Alt + sürükle", "Seçimi kopyalayarak taşı"),
            ("Ctrl + tık", "Seçime ekle / çıkar"),
            ("Ok tuşları", "Seçimi 1 hücre kaydır"),
            ("R / F", "Seçimi döndür / aynala"),
            ("Del", "Seçimi sil"),
            ("Ctrl + Z / Y", "Geri al / ileri al"),
            ("Ctrl + C / V", "Kopyala / yapıştır"),
            ("Ctrl + D", "Çoğalt"),
            ("Ctrl + A", "Bütün blokları seç"),
            ("Ctrl + S", "Kaydet"),
            ("Ctrl + PageUp/Down", "Önceki / sonraki bölüm"),
            ("Ctrl + Tab", "Sonraki sekme"),
            ("Tekerlek", "Yakınlaştır (imleç altındaki nokta sabit kalır)"),
            ("Orta tuş", "Tuvali kaydır"),
            ("Esc", "Seçimi bırak")
        };

        static readonly (string Title, string Body)[] Mechanics =
        {
            ("Kapı genişliği bloğu sınırlar",
             "Bir blok ancak kendi renginde ve EN AZ kendi genişliği kadar uzun bir " +
             "kapıdan çıkabilir. 3 hücre geniş blok için 2 hücrelik kapı yetmez."),
            ("Buz sayacı bir BÜTÇEDİR",
             "'N blok çıkmadan kırılmaz' diyen sayaç, kendisinden önce ulaşılabilir " +
             "çıkış sayısından büyükse buz hiç kırılmaz ve bölüm ölür. Doğrula sekmesi " +
             "bunu sebebiyle birlikte söylüyor."),
            ("Perde sayacı emilimleri sayar",
             "Katman soyulması da bir emilimdir. Perdenin ardındaki buz ancak perde " +
             "açıldıktan sonra saymaya başlar."),
            ("Sıkışık bölge hareket etmez",
             "Tamamen dolu bir bölgede hiçbir blok kayamaz. Her renk için en az bir " +
             "boş koridor bırak."),
            ("Renk kuyruğu tahtada görünmez",
             "Kapının ikinci ve sonraki renkleri tuvalde küçük karelerdir; sırayı " +
             "düzenlemek için alt şeritteki kapı kutusunu kullan."),
            ("Kapısı olmayan renk = ölü bölüm",
             "Üst şeritteki palet noktaları bunu gösterir: tahtada kullanılan ama " +
             "kapısı olmayan renk kırmızı çerçeveyle işaretlenir."),
            ("Üreteç sırası da 'oyunda olan renk'tir",
             "Makinenin sırasındaki bloklar tahtaya girecek demektir; o renklerin de " +
             "kapısı olmalı. Boş sıralı üreteç hiç çalışmaz ve bölümü kilitler."),
            ("Tuval oyunun dilini konuşur",
             "Bloklar oyundaki gibi kabartmalı tuğla çizilir; ölçüler " +
             "BlockVisualConfig'ten okunur. Kesin sonucu 3D Önizleme sekmesinde gör — " +
             "orada bölüm oyunun KENDİ kodu (BoardBuilder) ile kuruluyor.")
        };

        void DrawGuideTab()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_guideScroll))
            {
                _guideScroll = scroll.scrollPosition;

                SectionHeader("KISAYOLLAR");
                foreach (var (keys, action) in Shortcuts)
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(keys, LevelEditorSkin.Value, GUILayout.Width(150));
                        EditorGUILayout.LabelField(action, LevelEditorSkin.RowLabel);
                    }

                SectionHeader("BÖLÜM YAZARKEN DÜŞÜLEN TUZAKLAR",
                    "Bunlar bu projede fiilen yaşanmış hatalar; doğrulama araçları " +
                    "büyük ölçüde bu listeden doğdu.");

                foreach (var (title, body) in Mechanics)
                    using (LevelEditorSkin.CardScope())
                    {
                        EditorGUILayout.LabelField(title, LevelEditorSkin.Value);
                        EditorGUILayout.LabelField(body, LevelEditorSkin.SectionBody);
                    }

                SectionHeader("SEKMELER NE İŞE YARAR");
                foreach (var info in TabInfo)
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(info.Label, LevelEditorSkin.Value,
                            GUILayout.Width(150));
                        EditorGUILayout.LabelField(info.Tip, LevelEditorSkin.RowLabel);
                    }
            }
        }
    }
}
