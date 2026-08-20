using UnityEngine;

namespace GameKit.DevTools
{
    /// <summary>
    /// LOG sekmesi: cihaz üstünde Unity konsolu (<see cref="DevLog"/> akışı).
    /// Kitin içinde gelir — her oyunun ihtiyacı aynı.
    /// </summary>
    public sealed class DevLogPage : DevPage
    {
        public override string Title => "LOG";

        public override string Badge => DevLog.Errors > 0 ? DevLog.Errors.ToString() : null;

        bool _showAction = true, _showLog = true, _showWarning = true, _showError = true;
        bool _follow = true;
        string _query = "";
        Vector2 _scroll;
        int _expanded = -1;

        public override void Draw()
        {
            GUILayout.BeginHorizontal();
            _query = Field(_query, "log içinde ara…", 30f);
            if (Btn("TEMİZLE", 30f, 84f)) _query = "";
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Chip("İŞLEM", _showAction)) _showAction = !_showAction;
            if (Chip("LOG", _showLog)) _showLog = !_showLog;
            if (Chip("UYARI", _showWarning)) _showWarning = !_showWarning;
            if (Chip("HATA" + (DevLog.Errors > 0 ? " " + DevLog.Errors : ""), _showError))
                _showError = !_showError;
            if (Chip(_follow ? "TAKİP ✓" : "TAKİP", _follow)) _follow = !_follow;
            if (Chip(DevLog.Paused ? "DURDU" : "KAYIT ✓", !DevLog.Paused))
                DevLog.Paused = !DevLog.Paused;
            GUILayout.EndHorizontal();

            GUILayout.Space(3f * U);

            // Yeni satır geldiyse ve takip açıksa sona kaydır: telefonu elinde
            // tutan kişi listeyi elle sürüklemek zorunda kalmasın.
            if (DevLog.Dirty && _follow) { _scroll.y = float.MaxValue; DevLog.Dirty = false; }

            _scroll = ScrollBegin(_scroll);

            string needle = _query.ToLowerInvariant();
            int shown = 0;

            for (int i = 0; i < DevLog.Entries.Count; i++)
            {
                var entry = DevLog.Entries[i];
                if (!Visible(entry.Kind)) continue;
                if (needle.Length > 0 && !entry.Message.ToLowerInvariant().Contains(needle)) continue;

                shown++;
                DrawEntry(i, entry);
            }

            if (shown == 0) Empty("Süzgece uyan kayıt yok.");
            ScrollEnd();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Tint(DevLog.Entries.Count + " kayıt · " + DevLog.Errors + " hata",
                                 DevInk.Muted), S.Small, GUILayout.Height(26f * U));
            GUILayout.FlexibleSpace();
            if (Btn("AKIŞI TEMİZLE", 28f, 130f))
            {
                DevLog.Clear();
                _expanded = -1;
                Note("log akışı temizlendi");
            }
            GUILayout.EndHorizontal();
        }

        bool Visible(DevLog.Kind kind)
        {
            switch (kind)
            {
                case DevLog.Kind.Action: return _showAction;
                case DevLog.Kind.Warning: return _showWarning;
                case DevLog.Kind.Error: return _showError;
                default: return _showLog;
            }
        }

        void DrawEntry(int index, DevLog.Entry entry)
        {
            bool expanded = _expanded == index;

            var row = GUILayoutUtility.GetRect(0f, 22f * U, GUILayout.ExpandWidth(true));
            if (GUI.Button(row, GUIContent.none, S.Row)) _expanded = expanded ? -1 : index;

            Color color = entry.Kind == DevLog.Kind.Error ? DevInk.Danger
                        : entry.Kind == DevLog.Kind.Warning ? DevInk.Warn
                        : entry.Kind == DevLog.Kind.Action ? DevInk.Accent
                        : DevInk.Text;

            Fill(new Rect(row.x, row.y, 3f * U, row.height), color);

            float pad = 7f * U;
            GUI.Label(new Rect(row.x + pad, row.y, 58f * U, row.height),
                      Tint(entry.Time, DevInk.Muted), S.Small);

            string text = entry.Message.Replace("\n", " ");
            if (entry.Repeat > 1) text = "(" + entry.Repeat + "x) " + text;

            GUI.Label(new Rect(row.x + pad + 60f * U, row.y,
                               row.width - pad * 2f - 60f * U, row.height),
                      Tint(text, color), S.Small);

            if (!expanded) return;

            CardBegin();
            GUILayout.Label(entry.Message, S.Wrap, GUILayout.ExpandWidth(true));
            if (!string.IsNullOrEmpty(entry.Stack))
                GUILayout.Label(entry.Stack, S.WrapSmall, GUILayout.ExpandWidth(true));
            CardEnd();
        }
    }
}
