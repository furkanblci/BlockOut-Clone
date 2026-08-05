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
        bool _levelPickerOpen;

        // ---- çöp üretmeyen metin yolu ----
        // DERS (IMGUI sessizce ayırır): `GUI.Label(rect, "metin")` her çağrıda
        // geçici bir GUIContent üretir; OnGUI kare başına birkaç kez koştuğu için
        // bu, saniyede yüzlerce küçük ayırma demektir. M6 ölçümünde HUD tek başına
        // kare başına ~1.7 KB üretiyordu ve 120 karede 35 GC toplamasına yol
        // açıyordu. Çözüm: GUIContent'i BİR KEZ yaratıp yalnızca .text alanını,
        // o da yalnızca değer değiştiğinde güncellemek.
        readonly GUIContent _timerText = new GUIContent();
        readonly GUIContent _livesText = new GUIContent();
        readonly GUIContent _coinText = new GUIContent();
        readonly System.Text.StringBuilder _scratch = new System.Text.StringBuilder(48);

        int _shownSeconds = -1, _shownLevel = -1, _shownLives = -1, _shownCoins = -1;
        int _shownRefillSeconds = -1;

        static System.Text.StringBuilder Build(System.Text.StringBuilder sb)
        {
            sb.Clear();
            return sb;
        }

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

            // Metin yalnızca SANİYE ya da bölüm değişince kuruluyor; kalan ~59
            // karede hazır dize yeniden kullanılıyor.
            if (total != _shownSeconds || _session.DisplayNumber != _shownLevel)
            {
                _shownSeconds = total;
                _shownLevel = _session.DisplayNumber;
                _timerText.text = Build(_scratch)
                    .Append("Bölüm ").Append(_shownLevel).Append("   ")
                    .Append(total / 60).Append(':').Append((total % 60) / 10).Append(total % 10)
                    .ToString();
            }
            GUI.Label(new Rect(0, 8 * s, Screen.width, 40 * s), _timerText, _timerStyle);

            DrawMetaBar(s);
            DrawPerfToggle(s);
            DrawLevelPicker(s);
            if (_levelPickerOpen) return; // seçici açıkken altındaki ekranı çizme

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
            // Kazanç satırı — videodaki PERFECT ekranının coin'i.
            if (won && _session.LastReward > 0)
            {
                _timerStyle.fontSize = Mathf.RoundToInt(28 * s);
                _timerStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
                GUI.Label(new Rect(0, Screen.height * 0.42f, Screen.width, 40 * s),
                    $"+{_session.LastReward} coin", _timerStyle);
            }

            bool advance = won && _session.HasNextLevel;
            string label = advance ? "Sonraki Bölüm" : won ? "Tekrar Oyna" : "Tekrar Dene";
            if (GUI.Button(buttonRect, label, _buttonStyle))
            {
                if (advance) _session.NextLevel();
                else _session.Restart();
            }

            // Can bittiyse tekrar denemek anlamsız — oyuncuyu ana ekrana yollarız.
            var homeRect = new Rect(
                buttonRect.x, buttonRect.yMax + 14 * s, buttonRect.width, 48 * s);
            if (GUI.Button(homeRect, "Ana Ekran", _buttonStyle))
                AppRouter.GoHome();
        }

        /// <summary>
        /// Can / coin şeridi. Videodaki üst bar bunun cilalı hâli olacak;
        /// şimdilik meta servislerinin GERÇEKTEN işlediğini gözle görmek için.
        /// </summary>
        void DrawMetaBar(float s)
        {
            if (!Services.MetaServices.Ready) return;

            var lives = Services.MetaServices.Lives;
            var progress = Services.MetaServices.Progress;

            // Geri sayım her karede yeniden hesaplanmaz; saniyede bir yeter.
            if (Time.unscaledTime - _lastLivesRefresh > 1f)
            {
                _lastLivesRefresh = Time.unscaledTime;
                lives.Refresh();
            }

            // Can metni: sayı ya da geri sayımın SANİYESİ değiştiğinde kurulur.
            var refill = lives.TimeToNextLife;
            int refillSeconds = lives.IsFull ? -1 : Mathf.CeilToInt((float)refill.TotalSeconds);
            if (lives.Current != _shownLives || refillSeconds != _shownRefillSeconds)
            {
                _shownLives = lives.Current;
                _shownRefillSeconds = refillSeconds;

                var sb = Build(_scratch)
                    .Append("♥ ").Append(_shownLives).Append('/')
                    .Append(Services.MetaServices.MaxLives);
                if (refillSeconds >= 0)
                    sb.Append("  ").Append(refill.Minutes / 10).Append(refill.Minutes % 10)
                      .Append(':').Append(refill.Seconds / 10).Append(refill.Seconds % 10);
                _livesText.text = sb.ToString();
            }

            if (progress.Coins != _shownCoins)
            {
                _shownCoins = progress.Coins;
                _coinText.text = Build(_scratch).Append("◉ ").Append(_shownCoins).ToString();
            }

            _timerStyle.fontSize = Mathf.RoundToInt(22 * s);
            _timerStyle.normal.textColor = new Color(1f, 0.75f, 0.8f);
            GUI.Label(new Rect(0, 52 * s, Screen.width * 0.5f, 30 * s), _livesText, _timerStyle);

            _timerStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
            GUI.Label(new Rect(Screen.width * 0.5f, 52 * s, Screen.width * 0.5f, 30 * s),
                _coinText, _timerStyle);
        }

        float _lastLivesRefresh;

        /// <summary>
        /// Performans sondasını açıp kapatan küçük düğme. Cihazda profiler
        /// bağlamadan kare hızını ve çöp üretimini görmenin en hızlı yolu.
        /// </summary>
        static readonly GUIContent PerfOn = new GUIContent("fps ✓");
        static readonly GUIContent PerfOff = new GUIContent("fps");

        void DrawPerfToggle(float s)
        {
            var rect = new Rect(Screen.width - 74 * s, 8 * s, 66 * s, 34 * s);
            _buttonStyle.fontSize = Mathf.RoundToInt(18 * s);
            if (GUI.Button(rect, Services.PerfProbe.Visible ? PerfOn : PerfOff, _buttonStyle))
                Services.PerfProbe.Visible = !Services.PerfProbe.Visible;
        }

        /// <summary>
        /// Bölüm seçici — cihazda test ederken herhangi bir bölüme atlamak için.
        /// Editörde de, telefonda da aynı şekilde çalışır (dokunmatik dostu
        /// büyük düğmeler). M5'te gerçek Journey haritası bunun yerini alacak.
        /// </summary>
        void DrawLevelPicker(float s)
        {
            if (_session.LevelCount <= 1) return;

            var toggleRect = new Rect(8 * s, 8 * s, 48 * s, 40 * s);
            _buttonStyle.fontSize = Mathf.RoundToInt(24 * s);
            if (GUI.Button(toggleRect, _levelPickerOpen ? "×" : "☰", _buttonStyle))
                _levelPickerOpen = !_levelPickerOpen;

            if (!_levelPickerOpen) return;

            // Yarı saydam perde: altındaki oyuna dokunma geçmesin.
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

            const int perRow = 4;
            float cell = Mathf.Min(Screen.width / (perRow + 1f), 90f * s);
            float gridWidth = perRow * cell;
            int rows = Mathf.CeilToInt(_session.LevelCount / (float)perRow);
            float originX = (Screen.width - gridWidth) * 0.5f;
            float originY = Screen.height * 0.5f - rows * cell * 0.5f;

            _timerStyle.fontSize = Mathf.RoundToInt(26 * s);
            _timerStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(0, originY - 46 * s, Screen.width, 40 * s), "Bölüm Seç", _timerStyle);

            _buttonStyle.fontSize = Mathf.RoundToInt(28 * s);
            for (int i = 0; i < _session.LevelCount; i++)
            {
                var rect = new Rect(
                    originX + (i % perRow) * cell + 4f,
                    originY + (i / perRow) * cell + 4f,
                    cell - 8f, cell - 8f);

                bool current = i == _session.LevelIndex;
                var previous = GUI.color;
                if (current) GUI.color = new Color(0.5f, 1f, 0.6f);
                if (GUI.Button(rect, (i + 1).ToString(), _buttonStyle))
                {
                    _levelPickerOpen = false;
                    _session.GoToLevel(i);
                }
                GUI.color = previous;
            }
        }
    }
}
