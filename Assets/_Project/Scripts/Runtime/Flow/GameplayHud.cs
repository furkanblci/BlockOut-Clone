using UnityEngine;
using BlockOut.Core;
using GameKit.Services;

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
        GUIStyle _hintStyle;

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

        string _powerMessage = "";

        public void Init(GameSession session)
        {
            _session = session;
            if (_session.PowerUps != null)
                _session.PowerUps.Message += text => _powerMessage = text;
        }

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
                _hintStyle = new GUIStyle(_timerStyle) { fontStyle = FontStyle.Normal };
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
            DrawPowerUpBar(s);
            DrawPerfToggle(s);

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
        /// <summary>
        /// Alt yardımcı çubuğu: çalar saat / roket / UFO. Elde varsa adet,
        /// yoksa jeton fiyatı yazar — referans oyunun rozet mantığı.
        ///
        /// DERS (durumu düğmenin üstünde göster): Oyuncu "bu bana kaça mal
        /// olacak" sorusunu düğmeye basmadan görebilmeli; fiyatı gizleyip
        /// basınca almak, oyuncunun kendini kandırılmış hissetmesinin en kısa
        /// yolu.
        /// </summary>
        void DrawPowerUpBar(float s)
        {
            var power = _session.PowerUps;
            if (power == null) return;

            float w = 92f * s, h = 56f * s, gap = 12f * s;
            float total = w * 3f + gap * 2f;
            float x = (Screen.width - total) * 0.5f;
            float y = Screen.height - h - 16f * s;

            _hintStyle.fontSize = Mathf.RoundToInt(15 * s);

            // Yönerge ve donmuş süre uyarısı düğmelerin üstünde durur.
            if (!string.IsNullOrEmpty(_powerMessage))
                GUI.Label(new Rect(0, y - 26f * s, Screen.width, 22f * s), _powerMessage, _hintStyle);
            else if (power.IsTimeFrozen)
                GUI.Label(new Rect(0, y - 26f * s, Screen.width, 22f * s),
                    "Süre donduruldu: " + Mathf.CeilToInt(power.FreezeRemaining) + " sn", _hintStyle);

            for (int i = 0; i < 3; i++)
            {
                var kind = (PowerUpKind)i;
                int owned = power.Owned(kind);
                string caption = PowerUpInfo.Label(kind) + "\n" +
                                 (owned > 0 ? "x" + owned : PowerUpInfo.Price(kind) + " J");

                var rect = new Rect(x + i * (w + gap), y, w, h);
                var previous = GUI.backgroundColor;
                if (power.Pending == kind) GUI.backgroundColor = new Color(0.55f, 0.95f, 0.6f);
                if (GUI.Button(rect, caption)) power.Use(kind);
                GUI.backgroundColor = previous;
            }
        }

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
                    .Append("Can ").Append(_shownLives).Append('/')
                    .Append(Services.MetaServices.MaxLives);
                if (refillSeconds >= 0)
                    sb.Append("  ").Append(refill.Minutes / 10).Append(refill.Minutes % 10)
                      .Append(':').Append(refill.Seconds / 10).Append(refill.Seconds % 10);
                _livesText.text = sb.ToString();
            }

            if (progress.Coins != _shownCoins)
            {
                _shownCoins = progress.Coins;
                _coinText.text = Build(_scratch).Append("J ").Append(_shownCoins).ToString();
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
            if (GUI.Button(rect, PerfProbe.Visible ? PerfOn : PerfOff, _buttonStyle))
                PerfProbe.Visible = !PerfProbe.Visible;
        }

    }
}
