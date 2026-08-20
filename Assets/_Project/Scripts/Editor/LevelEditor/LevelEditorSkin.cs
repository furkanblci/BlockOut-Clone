using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Aracın görsel dili: renkler, yuvarlak köşeli düğmeler, kart blokları,
    /// harf aralıklı bölüm başlıkları.
    ///
    /// DERS (neden ayrı bir skin katmanı): Unity'nin varsayılan
    /// <see cref="EditorStyles"/> takımı "ayar penceresi" için tasarlanmıştır —
    /// sıkışık, 10 punto, her şey aynı ağırlıkta. İçerik ÜRETİLEN bir araçta
    /// gün boyu bakılan şey o pencere; hiyerarşi (neyin başlık, neyin açıklama,
    /// neyin birincil eylem olduğu) görünür olmalı. Bu dosya tek bir yerde o
    /// hiyerarşiyi tanımlar; sekmeler yalnız onu çağırır.
    ///
    /// DERS (BOYAMA ÇARPMADIR — bu projede altı kez düşülen tuzak): Dokular
    /// BEYAZ üretilir, renk <c>GUI.backgroundColor</c> ile çarpılarak verilir.
    /// Beyaz kaynak (1,1,1) olduğu için çarpım tam olarak istenen rengi verir.
    /// Kaynak koyu ya da baskılı gölgeli olsaydı boyama onu kirletirdi.
    /// </summary>
    public static class LevelEditorSkin
    {
        // ---------------- palet ----------------
        // Referans editörün karelerinden PİKSEL ÖRNEKLENEREK alındı
        // (level editör örnek.mp4, 5. saniye, 736×480 ham kare).
        //
        // DERS (şıklık düşük yüzey kontrastından gelir): Referans yalnız İKİ
        // zemin tonu kullanıyor — #100F12 ana zemin, #1B1A1D yükseltilmiş
        // yüzey. Aradaki fark 11 birim; neredeyse görünmez ama yüzeyleri
        // ayırmaya yetiyor. Vurgu renkleri de kısılmış: mavi lacivert, sarı
        // pirinç, yeşil orman yeşili. Böylece ekrandaki EN DOYGUN şey daima
        // içerik (renkli bloklar) oluyor. Unity'nin varsayılan #383838 grisi
        // bunun tam tersi: zemin bağırıyor, içerik onunla yarışıyor.
        //
        // Renkler hafif MOR'a çalıyor (mavi > kırmızı > yeşil); referansta bu
        // fark 2-3 birim ve ekranı "soğuk siyah" yapan şey o.

        /// <summary>Ana zemin — pencerenin tamamı bununla boyanır.</summary>
        public static readonly Color Window      = new Color(0.063f, 0.059f, 0.071f); // #100F12

        /// <summary>Yükseltilmiş yüzey: üst şerit, şeritler, metin alanları.</summary>
        public static readonly Color Bar         = new Color(0.106f, 0.102f, 0.114f); // #1B1A1D
        public static readonly Color Card        = new Color(0.106f, 0.102f, 0.114f); // #1B1A1D
        public static readonly Color Field       = new Color(0.106f, 0.102f, 0.114f); // #1B1A1D

        /// <summary>Tuvalin oyulmuş zemini — zeminden bir tık koyu.</summary>
        public static readonly Color Inset       = new Color(0.047f, 0.043f, 0.055f); // #0C0B0E

        public static readonly Color Hairline    = new Color(0.145f, 0.141f, 0.153f);

        /// <summary>Nötr düğme dolgusu — zeminden iki tık açık.</summary>
        public static readonly Color Neutral     = new Color(0.125f, 0.122f, 0.133f); // #201F22

        public static readonly Color Text        = new Color(0.878f, 0.878f, 0.910f);
        public static readonly Color TextMuted   = new Color(0.541f, 0.541f, 0.588f);
        public static readonly Color TextDim     = new Color(0.376f, 0.376f, 0.427f);

        // Düğme dolguları: kısılmış (referans ölçümü).
        public static readonly Color Accent      = new Color(0.180f, 0.239f, 0.361f); // #2E3D5C
        public static readonly Color Positive    = new Color(0.208f, 0.416f, 0.188f); // #356A30
        public static readonly Color Warning     = new Color(0.459f, 0.361f, 0.133f); // #75601F
        public static readonly Color Danger      = new Color(0.400f, 0.176f, 0.176f);

        // Vurgu ÇİZGİLERİ ve yazı: dolgudan daha parlak olmalı, yoksa okunmaz.
        // DERS: aynı rengi hem dolgu hem yazı için kullanmak, birinde okunaksız
        // ötekinde bağıran bir sonuç verir. İkisi ayrı tanımlanır.
        public static readonly Color AccentBright   = new Color(0.400f, 0.588f, 0.902f);
        public static readonly Color PositiveBright = new Color(0.400f, 0.780f, 0.427f);
        public static readonly Color WarningBright  = new Color(0.878f, 0.694f, 0.294f);
        public static readonly Color DangerBright   = new Color(0.902f, 0.416f, 0.400f);

        // ---------------- dokular ----------------
        // Domain reload dokuları yok eder; her erişimde null kontrolü şart.

        static Texture2D _round6, _round4, _round3, _pixel, _disc;

        /// <summary>1×1 beyaz doku — düz dolgular ve saç teli çizgiler için.</summary>
        public static Texture2D Pixel => _pixel != null ? _pixel : (_pixel = MakePixel());

        public static Texture2D Round3 => _round3 != null ? _round3 : (_round3 = MakeRounded(3));
        public static Texture2D Round4 => _round4 != null ? _round4 : (_round4 = MakeRounded(4));

        /// <summary>Kenarı yumuşatılmış BEYAZ daire — tuğla saplamaları için.</summary>
        public static Texture2D Disc => _disc != null ? _disc : (_disc = MakeDisc(32));

        static Texture2D MakeDisc(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = size * 0.5f;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - radius, dy = y + 0.5f - radius;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Daire çizer (saplama, durum noktası...).</summary>
        public static void DrawDisc(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Disc, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }
        public static Texture2D Round6 => _round6 != null ? _round6 : (_round6 = MakeRounded(6));

        static Texture2D MakePixel()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Yuvarlak köşeli BEYAZ doku; 9 dilime bölünerek her boyuta gerilir.
        /// Kenar yumuşatma elle yapılır.
        ///
        /// DERS (<c>Mathf.SmoothStep</c> bir eşik DEĞİLDİR): Bu projede alfa
        /// maskeleri tam bu yüzden bir kez tamamen görünmez çıktı — Unity'nin
        /// SmoothStep'i iki DEĞER arasında yumuşatılmış lerp yapar, GLSL'deki
        /// eşik uygulayan smoothstep değildir. Burada 1 piksellik doğrusal
        /// rampa elle yazılıyor.
        /// </summary>
        static Texture2D MakeRounded(int radius)
        {
            int size = radius * 2 + 2;                       // ortada 2 piksel gerilecek alan
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    // Köşe merkezine uzaklık: dikdörtgenin içindeki noktalar için 0.
                    float dx = Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius), 0f);
                    float dy = Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius), 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // ---------------- stiller ----------------

        static GUIStyle _sectionTitle, _sectionBody, _rowLabel, _pill, _cardBox, _value, _metric;

        /// <summary>Bölüm başlığı — büyük harf, açık renk, 13 punto.</summary>
        public static GUIStyle SectionTitle => _sectionTitle ?? (_sectionTitle = new GUIStyle
        {
            font = EditorStyles.boldLabel.font,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Text }
        });

        /// <summary>Başlık altındaki açıklama paragrafı — soluk, sarmalı.</summary>
        public static GUIStyle SectionBody => _sectionBody ?? (_sectionBody = new GUIStyle
        {
            font = EditorStyles.miniLabel.font,
            fontSize = 10,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = TextMuted },
            padding = new RectOffset(0, 0, 2, 4)
        });

        public static GUIStyle RowLabel => _rowLabel ?? (_rowLabel = new GUIStyle
        {
            font = EditorStyles.label.font,
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = TextMuted }
        });

        public static GUIStyle Value => _value ?? (_value = new GUIStyle
        {
            font = EditorStyles.boldLabel.font,
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Text }
        });

        /// <summary>Kart içindeki büyük sayı (Pano sayaçları).</summary>
        public static GUIStyle Metric => _metric ?? (_metric = new GUIStyle
        {
            font = EditorStyles.boldLabel.font,
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Text }
        });

        /// <summary>Yuvarlak köşeli düğme gövdesi; rengi backgroundColor verir.</summary>
        public static GUIStyle Pill
        {
            get
            {
                if (_pill == null || _pill.normal.background == null)
                    _pill = new GUIStyle
                    {
                        font = EditorStyles.miniLabel.font,
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter,
                        border = new RectOffset(6, 6, 6, 6),
                        padding = new RectOffset(8, 8, 3, 3),
                        normal = { background = Round6, textColor = Text },
                        hover = { background = Round6, textColor = Color.white },
                        active = { background = Round6, textColor = Color.white }
                    };
                return _pill;
            }
        }

        static GUIStyle _barStyle;

        /// <summary>
        /// Düz şerit zemini. <see cref="EditorStyles.toolbar"/> yerine bu
        /// kullanılır — Unity'nin şerit dokusu klasik griyi getiriyor ve
        /// referansın iki tonlu siyahını bozuyor.
        /// </summary>
        public static GUIStyle BarStyle
        {
            get
            {
                if (_barStyle == null || _barStyle.normal.background == null)
                    _barStyle = new GUIStyle
                    {
                        padding = new RectOffset(6, 6, 3, 3),
                        margin = new RectOffset(0, 0, 0, 0),
                        fixedHeight = 0f,
                        normal = { background = Pixel }
                    };
                return _barStyle;
            }
        }

        /// <summary>
        /// Yatay şerit kapsamı: zemini kendi boyar.
        /// <c>using (LevelEditorSkin.BarScope()) { ... }</c>
        /// </summary>
        public static BarScopeHandle BarScope(params GUILayoutOption[] options) =>
            new BarScopeHandle(Bar, options);

        public static BarScopeHandle BarScope(Color color, params GUILayoutOption[] options) =>
            new BarScopeHandle(color, options);

        public readonly struct BarScopeHandle : System.IDisposable
        {
            public BarScopeHandle(Color color, GUILayoutOption[] options)
            {
                var previous = GUI.backgroundColor;
                GUI.backgroundColor = color;
                EditorGUILayout.BeginHorizontal(BarStyle, options);
                GUI.backgroundColor = previous;
            }

            public void Dispose() => EditorGUILayout.EndHorizontal();
        }

        public static GUIStyle CardBox
        {
            get
            {
                if (_cardBox == null || _cardBox.normal.background == null)
                    _cardBox = new GUIStyle
                    {
                        border = new RectOffset(6, 6, 6, 6),
                        padding = new RectOffset(10, 10, 8, 8),
                        margin = new RectOffset(0, 0, 3, 3),
                        normal = { background = Round6 }
                    };
                return _cardBox;
            }
        }

        // ---------------- çizim yardımcıları ----------------

        public static void Fill(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Pixel);
            GUI.color = previous;
        }

        /// <summary>Yuvarlak köşeli dolgu; istenirse 1 piksel kenarlıkla.</summary>
        public static void RoundedRect(Rect rect, Color fill, Texture2D shape = null,
            Color? border = null)
        {
            var texture = shape != null ? shape : Round6;
            var previous = GUI.color;

            if (border.HasValue)
            {
                GUI.color = border.Value;
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
                rect = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
            }

            GUI.color = fill;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        /// <summary>
        /// Harf aralıklı (tracked) metin çizer.
        ///
        /// IMGUI'de harf aralığı ayarı YOKTUR; referans editörün büyük harfli
        /// başlıklarındaki ferahlığı almak için harfler tek tek konumlandırılıyor.
        /// Başlıklar kısa olduğu için maliyeti önemsiz.
        /// </summary>
        public static void TrackedLabel(Rect rect, string text, GUIStyle style, float tracking = 1.6f)
        {
            if (string.IsNullOrEmpty(text)) return;

            float x = rect.x;
            for (int i = 0; i < text.Length; i++)
            {
                var glyph = new GUIContent(text[i].ToString());
                float width = style.CalcSize(glyph).x;
                GUI.Label(new Rect(x, rect.y, width + 1f, rect.height), glyph, style);
                x += width + tracking;
                if (x > rect.xMax) return;
            }
        }

        /// <summary>Bölüm başlığı + isteğe bağlı açıklama + altına saç teli.</summary>
        public static void SectionHeader(string title, string body = null)
        {
            GUILayout.Space(8f);
            var row = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
            TrackedLabel(row, title.ToUpperInvariant(), SectionTitle);

            var line = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            Fill(line, Hairline);

            if (!string.IsNullOrEmpty(body))
            {
                GUILayout.Space(3f);
                GUILayout.Label(body, SectionBody);
            }
            GUILayout.Space(4f);
        }

        /// <summary>
        /// Yuvarlak köşeli düğme. <paramref name="tint"/> gövde rengi,
        /// <paramref name="active"/> seçili hâli (vurgu rengi + beyaz yazı).
        /// </summary>
        public static bool Button(string label, Color tint, float width = 0f, float height = 22f,
            string tooltip = null, bool active = false)
        {
            var options = width > 0f
                ? new[] { GUILayout.Width(width), GUILayout.Height(height) }
                : new[] { GUILayout.Height(height) };

            var content = new GUIContent(label, tooltip);
            var style = Pill;
            var previousText = style.normal.textColor;
            var previousBackground = GUI.backgroundColor;

            GUI.backgroundColor = active ? Accent : tint;
            style.normal.textColor = active ? Color.white : Text;
            bool clicked = GUILayout.Button(content, style, options);

            style.normal.textColor = previousText;
            GUI.backgroundColor = previousBackground;
            return clicked;
        }

        // ---------------- giriş alanları ----------------

        static GUIStyle _fieldStyle, _popupStyle, _sliderTrack, _sliderThumb;

        /// <summary>
        /// Yuvarlak köşeli giriş alanı.
        ///
        /// DERS (Unity'nin alanları da BOYANABİLİR): Sayı/metin alanını sıfırdan
        /// yazmak, imleç–seçim–kopyala davranışını da yazmak demektir; gereksiz
        /// ve hataya açık. <c>EditorGUI.IntField</c> bir GUIStyle kabul ediyor —
        /// DAVRANIŞ Unity'de kalıyor, yalnız GÖRÜNÜM bizim oluyor. Aracın geri
        /// kalanı yuvarlak ve düzken alanların Unity'nin köşeli gri kutuları
        /// olarak kalması, tasarımdaki son yamaydı.
        /// </summary>
        public static GUIStyle FieldStyle
        {
            get
            {
                if (_fieldStyle == null || _fieldStyle.normal.background == null)
                    _fieldStyle = new GUIStyle
                    {
                        font = EditorStyles.label.font,
                        fontSize = 11,
                        alignment = TextAnchor.MiddleLeft,
                        border = new RectOffset(4, 4, 4, 4),
                        padding = new RectOffset(6, 6, 2, 2),
                        normal = { background = Round4, textColor = Text },
                        focused = { background = Round4, textColor = Color.white },
                        hover = { background = Round4, textColor = Text }
                    };
                return _fieldStyle;
            }
        }

        public static GUIStyle PopupStyle
        {
            get
            {
                if (_popupStyle == null || _popupStyle.normal.background == null)
                    _popupStyle = new GUIStyle(FieldStyle);
                return _popupStyle;
            }
        }

        public static GUIStyle SliderTrack
        {
            get
            {
                if (_sliderTrack == null || _sliderTrack.normal.background == null)
                    _sliderTrack = new GUIStyle
                    {
                        fixedHeight = 4f,
                        margin = new RectOffset(0, 0, 8, 8),
                        border = new RectOffset(2, 2, 2, 2),
                        normal = { background = Round3 }
                    };
                return _sliderTrack;
            }
        }

        public static GUIStyle SliderThumb
        {
            get
            {
                if (_sliderThumb == null || _sliderThumb.normal.background == null)
                    _sliderThumb = new GUIStyle
                    {
                        fixedWidth = 12f,
                        fixedHeight = 12f,
                        normal = { background = Disc },
                        active = { background = Disc }
                    };
                return _sliderThumb;
            }
        }

        /// <summary>Etiketi solda, alanı sağda duran satır çerçevesi.</summary>
        static Rect BeginRow(string label, float labelWidth, float height = 20f)
        {
            var row = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(label))
                GUI.Label(new Rect(row.x, row.y, labelWidth, row.height), label, RowLabel);
            return new Rect(row.x + labelWidth, row.y,
                Mathf.Max(24f, row.width - labelWidth), row.height);
        }

        public static int IntRow(string label, int value, float labelWidth = 108f)
        {
            var rect = BeginRow(label, labelWidth);
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = Bar;
            int result = EditorGUI.IntField(rect, value, FieldStyle);
            GUI.backgroundColor = previous;
            return result;
        }

        public static string TextRow(string label, string value, float labelWidth = 108f)
        {
            var rect = BeginRow(label, labelWidth);
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = Bar;
            string result = EditorGUI.TextField(rect, value ?? string.Empty, FieldStyle);
            GUI.backgroundColor = previous;
            return result;
        }

        public static int PopupRow(string label, int index, string[] options, float labelWidth = 108f)
        {
            var rect = BeginRow(label, labelWidth);
            var previous = GUI.backgroundColor;
            GUI.backgroundColor = Bar;
            int result = EditorGUI.Popup(rect, index, options, PopupStyle);
            GUI.backgroundColor = previous;
            return result;
        }

        /// <summary>Tamsayı kaydırıcı + sağda okunur değer.</summary>
        public static int SliderRow(string label, int value, int min, int max, float labelWidth = 108f)
        {
            var rect = BeginRow(label, labelWidth);
            var track = new Rect(rect.x, rect.y, Mathf.Max(24f, rect.width - 40f), rect.height);

            var previous = GUI.backgroundColor;
            GUI.backgroundColor = Neutral;
            float raw = GUI.HorizontalSlider(track, value, min, max, SliderTrack, SliderThumb);
            GUI.backgroundColor = previous;

            int result = Mathf.Clamp(Mathf.RoundToInt(raw), min, max);
            GUI.Label(new Rect(rect.xMax - 36f, rect.y, 36f, rect.height), result.ToString(), Value);
            return result;
        }

        public enum NoteKind { Info, Warning, Danger }

        /// <summary>
        /// <see cref="EditorGUILayout.HelpBox"/> yerine geçen not kutusu:
        /// yuvarlak zemin, solda renkli şerit, soluk sarmalı yazı. Simge YOK —
        /// Unity'nin simgeleri bu paletle uyumsuz duruyor, rengi zaten şerit
        /// taşıyor.
        ///
        /// DERS (yükseklik ÖLÇÜLÜR, tahmin edilmez): Sarmalı yazının kaç satır
        /// tutacağı pencere genişliğine bağlı. Sabit yükseklik vermek dar
        /// pencerede yazıyı kırpar. <c>GUILayoutUtility.GetRect(içerik, stil)</c>
        /// gereken yüksekliği yerleşim motoruna hesaplatıyor.
        /// </summary>
        public static void Note(string text, NoteKind kind = NoteKind.Info)
        {
            var accent = kind == NoteKind.Danger ? DangerBright
                : kind == NoteKind.Warning ? WarningBright
                : AccentBright;

            var style = new GUIStyle(SectionBody) { padding = new RectOffset(10, 10, 6, 6) };
            var content = new GUIContent(text);
            var box = GUILayoutUtility.GetRect(content, style, GUILayout.ExpandWidth(true));

            RoundedRect(box, Card, Round4);
            Fill(new Rect(box.x, box.y + 3f, 2f, box.height - 6f), accent);
            GUI.Label(box, content, style);
            GUILayout.Space(2f);
        }

        /// <summary>Şerit içi düğme — şeritlere sığan alçak yükseklik.</summary>
        public static bool BarButton(string label, float width = 0f, string tooltip = null,
            Color? tint = null) =>
            Button(label, tint ?? Neutral, width, 19f, tooltip);

        /// <summary>
        /// Şerit içi aç/kapa. Açıkken vurgu rengiyle dolar.
        /// Unity'nin <c>GUILayout.Toggle</c>'ı bir stil bekler ve o stilin
        /// dokusu klasik griyi getirir; bu yüzden düğme olarak kuruluyor.
        /// </summary>
        public static bool BarToggle(bool value, string label, float width = 0f,
            string tooltip = null)
        {
            if (Button(label, Neutral, width, 19f, tooltip, value)) return !value;
            return value;
        }

        /// <summary>Manuel yerleşim için dikdörtgen alan alan sürüm.</summary>
        public static bool Button(Rect rect, string label, Color tint,
            string tooltip = null, bool active = false, bool enabled = true)
        {
            var style = Pill;
            var previousText = style.normal.textColor;
            var previousBackground = GUI.backgroundColor;

            GUI.backgroundColor = active ? Accent : tint;
            style.normal.textColor = active ? Color.white : Text;

            bool clicked;
            using (new EditorGUI.DisabledScope(!enabled))
                clicked = GUI.Button(rect, new GUIContent(label, tooltip), style);

            style.normal.textColor = previousText;
            GUI.backgroundColor = previousBackground;
            return clicked;
        }

        /// <summary>
        /// Kart kapsamı: yuvarlak köşeli zemin + kenarlık, içine dikey yerleşim.
        /// <c>using (LevelEditorSkin.CardScope()) { ... }</c> biçiminde kullanılır.
        /// </summary>
        public static CardScopeHandle CardScope(params GUILayoutOption[] options) =>
            new CardScopeHandle(options);

        public readonly struct CardScopeHandle : System.IDisposable
        {
            public CardScopeHandle(GUILayoutOption[] options)
            {
                // DERS (zemini içerikten SONRA çizemezsin): Kartın yüksekliği
                // ancak içerik çizildikten sonra bilinir, ama o noktada zemini
                // çizmek içeriğin ÜSTÜNÜ kapatır. IMGUI'nin çözümü, grubun
                // kendi stil zeminini kullanmaktır — zemini GUILayout çiziyor,
                // biz yalnız rengini veriyoruz (beyaz doku × backgroundColor).
                var previous = GUI.backgroundColor;
                GUI.backgroundColor = Card;
                EditorGUILayout.BeginVertical(CardBox, options);
                GUI.backgroundColor = previous;
            }

            public void Dispose() => EditorGUILayout.EndVertical();
        }
    }
}
