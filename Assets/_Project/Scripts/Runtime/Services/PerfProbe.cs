using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace BlockOut.Runtime.Services
{
    /// <summary>
    /// Cihazda kare hızını ve ÇÖP ÜRETİMİNİ ölçen ince bir sonda.
    ///
    /// DERS (neden kare hızı yetmez?): 60 fps gösteren bir oyun, saniyede 200 KB
    /// çöp üretiyorsa birkaç saniyede bir GC duraklaması yaşar — ortalama akıcı,
    /// deneyim tırtıklıdır. Bu yüzden asıl bakılacak sayı KARE BAŞINA AYIRMA'dır
    /// ve sürükleme döngüsünde hedefi SIFIR'dır.
    ///
    /// DERS (sondanın kendisi çöp üretmemeli): Ölçüm aracının kendisi ayırma
    /// yaparsa ölçtüğü şeyi bozar. Bu yüzden metin her karede değil saniyede bir
    /// kez ve önceden ayrılmış bir StringBuilder ile kuruluyor; sayılar da
    /// string birleştirme yerine Append ile yazılıyor.
    ///
    /// Cihazda ekrana basar; editörde de aynı şekilde çalışır.
    /// </summary>
    public sealed class PerfProbe : MonoBehaviour
    {
        const float SampleWindow = 1f;

        static PerfProbe _instance;

        /// <summary>Sondayı açıp kapatır (HUD'daki gizli düğme ya da menü).</summary>
        public static bool Visible
        {
            get => _instance != null && _instance.enabled;
            set
            {
                if (value && _instance == null)
                {
                    var go = new GameObject("PerfProbe");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PerfProbe>();
                }
                if (_instance != null) _instance.enabled = value;
            }
        }

        readonly StringBuilder _text = new StringBuilder(160);
        GUIStyle _style;

        /// <summary>
        /// "GC Allocated In Frame" sayacı — kare başına YÖNETİLEN ayırmanın tam
        /// değeri.
        ///
        /// DERS (doğru metriği seç): Önce `Profiler.GetTotalAllocatedMemoryLong`
        /// farkını kullanmıştım; o sayı yerel (native) belleği de kapsar ve
        /// arka planda mesh/tekstür yüklenince oynar — iyileştirme yaptığımız
        /// hâlde sayı ARTMIŞ gibi görünmüştü. ProfilerRecorder tam olarak
        /// istediğimizi sayıyor: bu karede kaç bayt yönetilen bellek ayrıldı.
        /// </summary>
        ProfilerRecorder _gcAlloc;

        int _frames;
        float _elapsed;
        long _bytesInWindow;

        float _fps;
        float _worstFrameMs;
        float _frameWorstInWindow;
        long _bytesPerFrame;
        int _gcCount, _gcAtWindowStart;

        void OnEnable()
        {
            _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            _gcAtWindowStart = System.GC.CollectionCount(0);
            _elapsed = 0f;
            _frames = 0;
            _bytesInWindow = 0;
            _frameWorstInWindow = 0f;
        }

        void OnDisable()
        {
            if (_gcAlloc.Valid) _gcAlloc.Dispose();
        }

        void Update()
        {
            float ms = Time.unscaledDeltaTime * 1000f;
            if (ms > _frameWorstInWindow) _frameWorstInWindow = ms;

            if (_gcAlloc.Valid) _bytesInWindow += _gcAlloc.LastValue;

            _frames++;
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < SampleWindow) return;

            _fps = _frames / _elapsed;
            _worstFrameMs = _frameWorstInWindow;
            _bytesPerFrame = _frames > 0 ? _bytesInWindow / _frames : 0;
            _gcCount = System.GC.CollectionCount(0) - _gcAtWindowStart;

            BuildText();

            _frames = 0;
            _elapsed = 0f;
            _bytesInWindow = 0;
            _frameWorstInWindow = 0f;
            _gcAtWindowStart = System.GC.CollectionCount(0);
        }

        void BuildText()
        {
            _text.Clear();
            _text.Append("fps ").Append(Mathf.RoundToInt(_fps));
            _text.Append("  en kotu ").Append(_worstFrameMs.ToString("0.0")).Append(" ms");
            _text.Append("\nkare basi ayirma ");
            if (_bytesPerFrame <= 0) _text.Append("0 B");
            else if (_bytesPerFrame < 1024) _text.Append(_bytesPerFrame).Append(" B");
            else _text.Append((_bytesPerFrame / 1024f).ToString("0.0")).Append(" KB");
            _text.Append("   GC ").Append(_gcCount).Append("/sn");
        }

        void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(Mathf.Min(Screen.width / 26f, 26f)),
                    alignment = TextAnchor.UpperLeft,
                    fontStyle = FontStyle.Bold
                };
            }

            // Yeşil = temiz, sarı = çöp üretiyor, kırmızı = kare hızı düşük.
            _style.normal.textColor =
                _fps < 50f ? new Color(1f, 0.4f, 0.35f)
                : _bytesPerFrame > 64 ? new Color(1f, 0.85f, 0.3f)
                : new Color(0.5f, 1f, 0.6f);

            float pad = Screen.width * 0.03f;
            GUI.Label(new Rect(pad, Screen.height * 0.12f, Screen.width - pad * 2f, 120f),
                _text.ToString(), _style);
        }
    }
}
