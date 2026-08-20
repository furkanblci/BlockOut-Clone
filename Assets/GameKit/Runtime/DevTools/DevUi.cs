using System.Collections.Generic;
using UnityEngine;

namespace GameKit.DevTools
{
    /// <summary>
    /// Konsolun rengi: Unity konsolu gibi SİYAH–KOYU GRİ, tek vurgu rengi.
    ///
    /// DERS (araç oyunun görüntüsünü ÇALMAMALI): Konsol oyunun üstünde durur;
    /// renkli, parlak bir panel gözü kendine çeker ve altındaki oyunun rengini
    /// yanlış gösterir (yarı saydam bir mor panelin arkasındaki mavi bloğun
    /// tonunu ölçemezsin). Nötr gri bir yüzey hem oyunun renklerini bozmaz hem
    /// de "bu oyun değil, alet" der.
    ///
    /// DERS (vurgu rengi az olmalı): İlk siyah sürümde neon yeşil başlıklar,
    /// mavi çipler ve yeşil düğmeler vardı; her şey vurguluyken hiçbir şey
    /// vurgulanmıyor demektir. Palet artık üç griden ibaret; renk yalnız DURUM
    /// taşıyor — seçili (mavi), iyi (yeşil), uyarı (sarı), hata (kırmızı).
    /// </summary>
    public static class DevInk
    {
        public static readonly Color Panel   = new Color(0.106f, 0.106f, 0.110f, 0.976f); // #1B1B1C
        public static readonly Color Card    = new Color(0.145f, 0.145f, 0.153f, 1f);     // #252527
        public static readonly Color CardAlt = new Color(0.180f, 0.180f, 0.188f, 1f);     // #2E2E30
        public static readonly Color Line    = new Color(0.259f, 0.259f, 0.271f, 1f);     // #424245
        public static readonly Color Text    = new Color(0.831f, 0.831f, 0.839f, 1f);     // #D4D4D6
        public static readonly Color Muted   = new Color(0.529f, 0.533f, 0.549f, 1f);     // #87888C
        public static readonly Color Accent  = new Color(0.298f, 0.557f, 0.855f, 1f);     // seçili · #4C8EDA
        public static readonly Color Accent2 = new Color(0.478f, 0.729f, 0.435f, 1f);     // iyi · #7ABA6F
        public static readonly Color Warn    = new Color(0.878f, 0.702f, 0.255f, 1f);     // #E0B341
        public static readonly Color Danger  = new Color(0.878f, 0.357f, 0.357f, 1f);     // #E05B5B
    }

    /// <summary>Konsolun IMGUI biçemleri. Bir kez kurulur, ekran genişliği değişince yenilenir.</summary>
    public sealed class DevStyles
    {
        public GUIStyle Title, Muted, Small, SmallRight, Head, Body, BodyRight, Wrap, WrapSmall,
                        Btn, BtnAccent, BtnDanger, BtnArmed, Tab, TabOn, Chip, ChipOn,
                        Field, Card, Row, RowOn;
    }

    /// <summary>
    /// Konsolun çizim araç kutusu: biçemler + tekrar eden parçalar (kart,
    /// başlık, etiket-değer satırı, çip, ölçü çubuğu, arama alanı).
    ///
    /// DERS (kit oyunu tanımaz): Bu sınıfın içinde tek bir oyun kavramı yok —
    /// ne bölüm, ne can, ne jeton. Yalnız "düğme, çip, satır" bilir. Sayfalar
    /// (oyunun kendi sekmeleri) bu kutuyu kullanır; böylece aynı konsol bir
    /// sonraki oyunda da hiçbir değişiklik olmadan çalışır.
    ///
    /// DERS (IMGUI'da biçem her karede KURULMAZ): OnGUI saniyede onlarca kez
    /// çalışır; içinde `new GUIStyle` yazmak her karede çöp üretir ve aracın
    /// kendisi ölçtüğü kare hızını bozar.
    /// </summary>
    public sealed class DevUi
    {
        /// <summary>Ölçek birimi: 540 piksel genişlikte 1, 1080'de 2.</summary>
        public float U { get; private set; } = 1f;

        public DevStyles S { get; private set; }

        int _builtWidth;
        string _armedKey;
        float _armedUntil;

        static readonly Dictionary<int, Texture2D> Textures = new Dictionary<int, Texture2D>();

        public void Refresh()
        {
            if (S != null && _builtWidth == Screen.width) return;
            _builtWidth = Screen.width;
            U = Mathf.Max(1f, Screen.width / 540f);
            Build();
        }

        void Build()
        {
            int fTitle = Size(17), fHead = Size(14), fBody = Size(12), fSmall = Size(10.5f);

            S = new DevStyles
            {
                Title = Label(fTitle, DevInk.Accent, FontStyle.Bold, TextAnchor.MiddleLeft),
                Muted = Label(fBody, DevInk.Muted, FontStyle.Normal, TextAnchor.MiddleLeft),
                Small = Label(fSmall, DevInk.Muted, FontStyle.Normal, TextAnchor.MiddleLeft),
                SmallRight = Label(fSmall, DevInk.Muted, FontStyle.Normal, TextAnchor.MiddleRight),
                Head = Label(fHead, DevInk.Text, FontStyle.Bold, TextAnchor.MiddleLeft),
                Body = Label(fBody, DevInk.Text, FontStyle.Normal, TextAnchor.MiddleLeft),
                BodyRight = Label(fBody, DevInk.Text, FontStyle.Normal, TextAnchor.MiddleRight)
            };

            // Uzun metinler satır kırar: kırpılan bir hata mesajı, olmayan bir
            // hata mesajıdır.
            S.Wrap = Label(fBody, DevInk.Text, FontStyle.Normal, TextAnchor.UpperLeft);
            S.Wrap.wordWrap = true;
            S.Wrap.clipping = TextClipping.Overflow;
            S.WrapSmall = Label(fSmall, DevInk.Muted, FontStyle.Normal, TextAnchor.UpperLeft);
            S.WrapSmall.wordWrap = true;
            S.WrapSmall.clipping = TextClipping.Overflow;

            S.Btn = Button(DevInk.CardAlt, DevInk.Text, fBody);
            S.BtnAccent = Button(new Color(0.161f, 0.290f, 0.435f), Color.white, fBody);
            S.BtnDanger = Button(new Color(0.278f, 0.153f, 0.153f), DevInk.Danger, fBody);
            S.BtnArmed = Button(new Color(0.541f, 0.184f, 0.184f), Color.white, fBody);

            // Sekme yazısı gövdeden küçük: panel ekranın yarısı kadarken altı
            // sekme yan yana sığmalı, yoksa etiketler kırpılır ve hangi
            // sekmede olduğun okunamaz.
            int fTab = Size(9.5f);
            S.Tab = Button(DevInk.Card, DevInk.Muted, fTab);
            S.Tab.padding = new RectOffset(Px(2), Px(2), Px(3), Px(3));
            S.TabOn = Button(new Color(0.259f, 0.263f, 0.278f), Color.white, fTab);
            S.TabOn.padding = S.Tab.padding;
            S.TabOn.fontStyle = FontStyle.Bold;

            S.Chip = Button(DevInk.Card, DevInk.Muted, fSmall);
            S.Chip.padding = new RectOffset(Px(6), Px(6), Px(3), Px(3));
            S.ChipOn = Button(DevInk.Accent, Color.white, fSmall);
            S.ChipOn.padding = S.Chip.padding;
            S.ChipOn.fontStyle = FontStyle.Bold;

            S.Field = new GUIStyle(GUI.skin.textField)
            {
                fontSize = Size(14),
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(Px(10), Px(10), Px(4), Px(4)),
                margin = new RectOffset(0, 0, 0, 0)
            };
            S.Field.normal.background = Solid(new Color(0.012f, 0.012f, 0.016f));
            S.Field.focused.background = S.Field.normal.background;
            S.Field.normal.textColor = DevInk.Text;
            S.Field.focused.textColor = DevInk.Text;

            S.Card = new GUIStyle
            {
                padding = new RectOffset(Px(9), Px(9), Px(6), Px(6)),
                margin = new RectOffset(0, 0, 0, Px(5))
            };
            S.Card.normal.background = Solid(DevInk.Card);

            S.Row = Button(DevInk.Card, DevInk.Text, fBody);
            S.Row.alignment = TextAnchor.MiddleLeft;
            S.RowOn = Button(new Color(0.161f, 0.290f, 0.435f), Color.white, fBody);
            S.RowOn.alignment = TextAnchor.MiddleLeft;
        }

        public int Size(float baseSize) => Mathf.RoundToInt(baseSize * U);
        public int Px(float value) => Mathf.RoundToInt(value * U);

        GUIStyle Label(int fontSize, Color color, FontStyle style, TextAnchor anchor)
        {
            var result = new GUIStyle
            {
                fontSize = fontSize,
                fontStyle = style,
                alignment = anchor,
                richText = true,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            result.normal.textColor = color;
            return result;
        }

        GUIStyle Button(Color background, Color text, int fontSize)
        {
            var result = new GUIStyle
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(Px(5), Px(5), Px(3), Px(3)),
                margin = new RectOffset(Px(2), Px(2), Px(2), Px(2))
            };
            result.normal.background = Solid(background);
            result.normal.textColor = text;
            result.hover.background = Solid(Lift(background, 0.045f));
            result.hover.textColor = text;
            result.active.background = Solid(Lift(background, 0.12f));
            result.active.textColor = text;
            result.focused.background = result.normal.background;
            result.focused.textColor = text;
            return result;
        }

        static Color Lift(Color color, float amount) =>
            new Color(color.r + amount, color.g + amount, color.b + amount, color.a);

        /// <summary>Tek renk doku — biçem arka planları için. Renk başına bir kez.</summary>
        public static Texture2D Solid(Color color)
        {
            int key = ((Color32)color).GetHashCode();
            if (Textures.TryGetValue(key, out var cached) && cached != null) return cached;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            var pixels = new Color32[4];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            Textures[key] = texture;
            return texture;
        }

        public static void Fill(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        /// <summary>Zengin metin rengi — satır içinde vurgu için.</summary>
        public static string Tint(string text, Color color) =>
            "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";

        public static string Bold(string text) => "<b>" + text + "</b>";

        /// <summary>
        /// Türkçe büyük harf: "yardımcı" → "YARDIMCI", "ilerleme" → "İLERLEME".
        ///
        /// DERS (kültüre duyarlı harf dönüşümü): <c>ToUpperInvariant</c> Türkçe
        /// noktasız ı'yı ve noktalı i'yi tanımaz; başlıklar "YARDıMCı" diye
        /// çıkıyordu. Kültür nesnesine (tr-TR) güvenmek yerine iki harfi elle
        /// eşlemek, kısıtlı çalışma zamanlarında (IL2CPP + invariant
        /// globalization) da aynı sonucu verir.
        /// </summary>
        public static string Upper(string text) =>
            text.Replace('ı', 'I').Replace('i', 'İ').ToUpperInvariant();

        // ------------------------------------------------------------------ parçalar

        public bool Button(string label, GUIStyle style, float height = 0f, float width = 0f)
        {
            var options = new List<GUILayoutOption>(2)
            {
                GUILayout.Height(height > 0f ? height * U : 34f * U)
            };
            if (width > 0f) options.Add(GUILayout.Width(width * U));
            return GUILayout.Button(label, style, options.ToArray());
        }

        public bool Btn(string label, float height = 0f, float width = 0f) =>
            Button(label, S.Btn, height, width);

        public bool BtnAccent(string label, float height = 0f, float width = 0f) =>
            Button(label, S.BtnAccent, height, width);

        public bool Chip(string label, bool on, float width = 0f)
        {
            var options = new List<GUILayoutOption>(2) { GUILayout.Height(26f * U) };
            if (width > 0f) options.Add(GUILayout.Width(width * U));
            return GUILayout.Button(label, on ? S.ChipOn : S.Chip, options.ToArray());
        }

        /// <summary>Bölüm başlığı: büyük harf etiket + ince çizgi.</summary>
        public void Section(string title)
        {
            GUILayout.Space(5f * U);
            GUILayout.Label(Tint(Upper(title), DevInk.Accent2), S.Small,
                            GUILayout.Height(16f * U));
            Divider();
        }

        public void Divider()
        {
            var rect = GUILayoutUtility.GetRect(0f, 1f * U, GUILayout.ExpandWidth(true));
            Fill(rect, DevInk.Line);
            GUILayout.Space(3f * U);
        }

        /// <summary>Etiket solda, değer sağda — okuma tablosunun tek satırı.</summary>
        public void Kv(string key, string value)
        {
            var rect = GUILayoutUtility.GetRect(0f, 22f * U, GUILayout.ExpandWidth(true));
            GUI.Label(new Rect(rect.x, rect.y, rect.width * 0.42f, rect.height), key, S.Small);
            GUI.Label(new Rect(rect.x + rect.width * 0.42f, rect.y, rect.width * 0.58f, rect.height),
                      value, S.BodyRight);
        }

        /// <summary>Oran çubuğu: sayının yanında GÖRÜNEN bir uzunluk.</summary>
        public void Meter(Rect rect, float ratio, Color color)
        {
            Fill(rect, DevInk.CardAlt);
            float width = Mathf.Clamp01(ratio) * rect.width;
            if (width > 0f) Fill(new Rect(rect.x, rect.y, width, rect.height), color);
        }

        public void CardBegin() => GUILayout.BeginVertical(S.Card);
        public void CardEnd() => GUILayout.EndVertical();

        /// <summary>
        /// Kaydırma alanı — YATAY kaydırma kapalı.
        ///
        /// DERS (tek bir uzun satır bütün düzeni kaydırır): Satır kırmayan tek
        /// bir açıklama metni, IMGUI'nin içerik genişliğini o satıra göre
        /// hesaplayıp yatay çubuk eklemesine ve sağdaki BÜTÜN değerlerin ekran
        /// dışına taşmasına yol açıyordu. Yatay çubuğu <c>GUIStyle.none</c> ile
        /// kapatmak taşan içeriği kaydırmak yerine kırpar; düzen sabit kalır.
        /// </summary>
        public Vector2 ScrollBegin(Vector2 scroll)
        {
            var result = GUILayout.BeginScrollView(scroll, false, true,
                                                   GUIStyle.none, GUI.skin.verticalScrollbar,
                                                   GUILayout.ExpandHeight(true));

            // DERS (içerik genişliğini SABİTLE): Kaydırma alanı, içindeki en
            // geniş öğeye göre genişler. Tek bir uzun düğme satırı bile içeriği
            // panelden geniş yapıyor, sağa hizalı bütün değerler görünmez
            // alana kayıyordu. İçeriği panel genişliğinde bir sütuna kilitlemek
            // bu sınıf hatayı tümden kapatıyor.
            GUILayout.BeginVertical(GUILayout.Width(Mathf.Max(40f, AreaWidth - 16f * U)));
            return result;
        }

        public void ScrollEnd()
        {
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        /// <summary>Sayfanın çizildiği alanın genişliği; konsol her karede yazar.</summary>
        public float AreaWidth { get; internal set; }

        /// <summary>Satır kıran açıklama metni.</summary>
        public void Paragraph(string text)
        {
            GUILayout.Label(text, S.WrapSmall, GUILayout.ExpandWidth(true));
            GUILayout.Space(3f * U);
        }

        /// <summary>Boş durum metni — "veri yok" da bir bilgidir.</summary>
        public void Empty(string message)
        {
            GUILayout.Space(14f * U);
            GUILayout.Label(message, S.Wrap, GUILayout.ExpandWidth(true));
            GUILayout.Space(6f * U);
        }

        /// <summary>Arama/metin alanı; boşken ipucu metnini kendi üstüne çizer.</summary>
        public string Field(string value, string placeholder, float height = 34f)
        {
            string result = GUILayout.TextField(value, S.Field, GUILayout.Height(height * U));

            if (result.Length == 0 && Event.current.type == EventType.Repaint)
            {
                var rect = GUILayoutUtility.GetLastRect();
                GUI.Label(new Rect(rect.x + 11f * U, rect.y, rect.width, rect.height),
                          Tint(placeholder, DevInk.Muted), S.Body);
            }
            return result;
        }

        /// <summary>
        /// Yıkıcı düğmeler için iki adımlı onay: ilk dokunuş SORAR, ikincisi
        /// yapar. Beş saniye içinde onaylanmazsa iptal olur.
        ///
        /// DERS (geri alınamayan işe tek dokunuş yetmez): "KAYDI SIFIRLA" tek
        /// dokunuşla çalışıyordu; saatlerce biriken test ilerlemesi parmağın
        /// kaymasıyla gidebiliyordu. Onay adımı aracı yavaşlatmaz, GÜVENİLİR
        /// yapar — güvenmediğin bir aracı zaten kullanmazsın.
        /// </summary>
        public bool Danger(string key, string label, float height = 0f)
        {
            bool armed = _armedKey == key && Time.unscaledTime <= _armedUntil;
            if (!Button(armed ? "EMİN MİSİN? TEKRAR DOKUN" : label,
                        armed ? S.BtnArmed : S.BtnDanger, height)) return false;

            if (armed) { _armedKey = null; return true; }

            _armedKey = key;
            _armedUntil = Time.unscaledTime + 5f;
            return false;
        }

        /// <summary>Konsol kapanınca bekleyen onayı düşür.</summary>
        public void ClearArmed() => _armedKey = null;

        /// <summary>m:ss biçimi — süre gösteren her sayfa aynı biçimi kullansın.</summary>
        public static string Clock(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = Mathf.CeilToInt(seconds);
            return (total / 60) + ":" + (total % 60).ToString("00");
        }
    }
}
