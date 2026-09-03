using UnityEngine;

namespace GameKit.DevTools
{
    /// <summary>
    /// Konsolun bir sekmesi.
    ///
    /// DERS (kitin genişletme noktası): Konsolun kabuğu (gizli açılış, panel,
    /// boyut, tema, log, cihaz künyesi) her oyunda AYNI; içerik ise her oyunda
    /// FARKLI — birinde bölüm listesi, diğerinde dalga seçici. Kabuğu kite,
    /// içeriği oyuna bırakmak bu ayrımın kod karşılığıdır: yeni oyun kitten bir
    /// satırla konsolu kurar, kendi sayfalarını kaydeder ve işi biter.
    ///
    /// Sayfa yazarken tek kural: çizim <see cref="Ui"/> üzerinden yapılır.
    /// Doğrudan GUI çağırmak da serbest ama biçemler oradan alınmalı, yoksa
    /// sayfa konsolun temasından kopar.
    /// </summary>
    public abstract class DevPage
    {
        /// <summary>Sekme etiketi. Dar ekranda okunabilsin diye KISA olmalı.</summary>
        public abstract string Title { get; }

        /// <summary>Sekmenin yanında görünen küçük sayı (ör. hata adedi). Yoksa null.</summary>
        public virtual string Badge => null;

        /// <summary>Konsol bu sayfayı çizmeden önce doldurur.</summary>
        public DevUi Ui { get; internal set; }

        /// <summary>Konsol açıldığında ya da sekmeye geçildiğinde çağrılır.</summary>
        public virtual void OnOpen() { }

        public abstract void Draw();

        // ---- kısayollar: sayfa kodu "Ui.Ui.Ui" demeden okunsun ----

        protected float U => Ui.U;
        protected DevStyles S => Ui.S;

        protected bool Btn(string label, float height = 0f, float width = 0f) =>
            Ui.Btn(label, height, width);

        protected bool BtnAccent(string label, float height = 0f, float width = 0f) =>
            Ui.BtnAccent(label, height, width);

        protected bool Button(string label, GUIStyle style, float height = 0f, float width = 0f) =>
            Ui.Button(label, style, height, width);

        protected bool Chip(string label, bool on, float width = 0f) => Ui.Chip(label, on, width);
        protected bool Danger(string key, string label, float height = 0f) =>
            Ui.Danger(key, label, height);

        protected void Section(string title) => Ui.Section(title);
        protected void Divider() => Ui.Divider();
        protected void Kv(string key, string value) => Ui.Kv(key, value);
        protected void CardBegin() => Ui.CardBegin();
        protected void CardEnd() => Ui.CardEnd();
        protected void Paragraph(string text) => Ui.Paragraph(text);
        protected void Empty(string message) => Ui.Empty(message);
        protected void Meter(Rect rect, float ratio, Color color) => Ui.Meter(rect, ratio, color);
        protected string Field(string value, string placeholder, float height = 34f) =>
            Ui.Field(value, placeholder, height);

        protected Vector2 ScrollBegin(Vector2 scroll) => Ui.ScrollBegin(scroll);
        protected void ScrollEnd() => Ui.ScrollEnd();

        protected static string Tint(string text, Color color) => DevUi.Tint(text, color);
        protected static string Bold(string text) => DevUi.Bold(text);
        protected static void Fill(Rect rect, Color color) => DevUi.Fill(rect, color);
        protected static string Clock(float seconds) => DevUi.Clock(seconds);

        /// <summary>Yapılan işi alt şeride ve LOG akışına yazar.</summary>
        protected static void Note(string message) => DevLog.Note(message);
    }
}
