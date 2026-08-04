using UnityEngine;

namespace BlockOut.Runtime.Flow
{
    /// <summary>
    /// GEÇİCİ geliştirme HUD'ı: süre, bölüm numarası, kazan/kaybet ekranı.
    /// OnGUI kasıtlı bir kısayoldur — M1'in konusu oynanış çekirdeği; gerçek
    /// arayüz (TextMeshPro + Canvas) M4/M5'te bunu tamamen değiştirecek.
    /// GameSession tarafından çalışma anında eklenir; sahnede serileşmez.
    /// </summary>
    public sealed class GameplayHud : MonoBehaviour
    {
        GameSession _session;
        GUIStyle _timerStyle;
        GUIStyle _bannerStyle;
        GUIStyle _buttonStyle;

        public void Init(GameSession session) => _session = session;

        void OnGUI()
        {
            if (_session == null) return;

            // Stiller OnGUI içinde kurulmak zorunda (GUI.skin'e ancak burada erişilir).
            if (_timerStyle == null)
            {
                _timerStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
                _bannerStyle = new GUIStyle(_timerStyle);
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            }

            // Ölçek DAR kenara bağlı: yalnızca yüksekliğe bakmak dikey telefonda
            // (1080x1920) yazıyı ekran genişliğinden taşırıyordu.
            float s = Mathf.Min(Screen.width / 500f, Screen.height / 800f);

            // Üst bant: bölüm + süre
            _timerStyle.fontSize = Mathf.RoundToInt(34 * s);
            int total = Mathf.CeilToInt(_session.Timer.Remaining);
            bool warning = total <= _session.WarningSeconds;
            _timerStyle.normal.textColor = warning ? new Color(1f, 0.3f, 0.25f) : Color.white;
            GUI.Label(new Rect(0, 8 * s, Screen.width, 40 * s),
                $"Bölüm {_session.DisplayNumber}   {total / 60}:{total % 60:00}", _timerStyle);

            if (_session.State != GameState.Won && _session.State != GameState.Lost)
                return;

            // Sonuç ekranı
            bool won = _session.State == GameState.Won;
            _bannerStyle.fontSize = Mathf.RoundToInt(40 * s);
            _bannerStyle.wordWrap = true; // uzun başlık dar ekranda alt satıra insin
            _bannerStyle.normal.textColor = won ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.4f, 0.35f);
            float margin = Screen.width * 0.06f;
            GUI.Label(
                new Rect(margin, Screen.height * 0.32f, Screen.width - margin * 2f, 160 * s),
                won ? "BÖLÜM TAMAMLANDI!" : "SÜRE DOLDU", _bannerStyle);

            _buttonStyle.fontSize = Mathf.RoundToInt(26 * s);
            var buttonRect = new Rect(
                Screen.width * 0.5f - 110 * s, Screen.height * 0.48f, 220 * s, 56 * s);
            bool advance = won && _session.HasNextLevel;
            string label = advance ? "Sonraki Bölüm" : won ? "Tekrar Oyna" : "Tekrar Dene";
            if (GUI.Button(buttonRect, label, _buttonStyle))
            {
                if (advance) _session.NextLevel();
                else _session.Restart();
            }
        }
    }
}
