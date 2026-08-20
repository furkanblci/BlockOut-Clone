using System.Collections.Generic;
using BlockOut.Runtime.Services;
using GameKit.DevTools;
using UnityEngine;

namespace BlockOut.Runtime.DevTools
{
    /// <summary>
    /// VERİ sekmesi: bölüm bölüm deneme/geçme/kayıp tablosu.
    ///
    /// DERS (veriyi GÖREBİLDİĞİN yerde tut): Analitik bir dosyaya yazılıp kimse
    /// bakmazsa yoktur. Cihazda, beş dokunuşla açılan bir ekranda durursa test
    /// eden kişi oyunu bıraktığı anda "27. bölümde herkes takılmış" bilgisini
    /// sana getirebilir.
    ///
    /// DERS (tablo, düz metinden okunaklıdır): Önceki sürüm bu veriyi tek bir
    /// <c>Report()</c> metnine döküyordu; sıralayamıyor, süzemiyor ve oranı
    /// gözle karşılaştıramıyordun. Aynı sayılar sütunlara ve bir orana dönüşünce
    /// "hangi bölüm tıkanıyor" sorusu bakışta cevaplanıyor.
    /// </summary>
    public sealed class DevDataPage : DevPage
    {
        public override string Title => "VERİ";

        Vector2 _scroll;
        int _sort;                        // 0 sıra · 1 geçme oranı · 2 deneme
        bool _onlyPlayed = true;
        float _refreshAt;

        static readonly string[] SortLabels = { "SIRA", "ORAN", "DENEME" };
        readonly List<LocalAnalytics.LevelStat> _stats = new List<LocalAnalytics.LevelStat>();

        public override void OnOpen() => _refreshAt = 0f;

        public override void Draw()
        {
            var analytics = AppBootstrap.Analytics;
            if (analytics == null) { Empty("Analitik sağlayıcı hazır değil."); return; }

            // Konsol açıkken oyun duraklı; veri saniyede bir tazelense yeter.
            if (Time.unscaledTime >= _refreshAt)
            {
                _refreshAt = Time.unscaledTime + 1f;
                Rebuild(analytics);
            }

            int starts = 0, clears = 0, fails = 0;
            foreach (var stat in _stats)
            {
                starts += stat.Starts;
                clears += stat.Clears;
                fails += stat.Fails;
            }

            CardBegin();
            Kv("Toplam", starts + " başlangıç · " + clears + " geçme · " + fails + " kayıp");
            Kv("Genel geçme oranı",
               starts > 0 ? "%" + Mathf.RoundToInt(100f * clears / starts) : "—");
            Kv("Oynanan bölüm", _stats.Count + " farklı bölüm");
            CardEnd();

            GUILayout.BeginHorizontal();
            for (int i = 0; i < SortLabels.Length; i++)
                if (Chip(SortLabels[i], _sort == i)) { _sort = i; Rebuild(analytics); }
            if (Chip("OYNANMIŞ", _onlyPlayed))
            {
                _onlyPlayed = !_onlyPlayed;
                Rebuild(analytics);
            }
            GUILayout.EndHorizontal();

            if (_stats.Count == 0)
            {
                Empty("Henüz veri yok. Birkaç bölüm oyna.");
                DrawActions(analytics);
                return;
            }

            var head = GUILayoutUtility.GetRect(0f, 18f * U, GUILayout.ExpandWidth(true));
            Columns(head, Tint("BÖL", DevInk.Muted), Tint("BAŞLA", DevInk.Muted),
                    Tint("GEÇ", DevInk.Muted), Tint("KAYIP", DevInk.Muted),
                    Tint("ORAN", DevInk.Muted), S.Small);

            _scroll = ScrollBegin(_scroll);

            foreach (var stat in _stats)
            {
                var row = GUILayoutUtility.GetRect(0f, 30f * U, GUILayout.ExpandWidth(true));
                Fill(new Rect(row.x, row.y, row.width, row.height - 2f * U),
                     stat.Level % 2 == 0 ? DevInk.Card : DevInk.CardAlt);

                float rate = stat.ClearRate;
                Color rateColor = stat.Starts == 0 ? DevInk.Muted
                                : rate < 0.34f ? DevInk.Danger
                                : rate < 0.67f ? DevInk.Warn : DevInk.Accent;

                // Oran çubuğu satırın altında ince bir şerit: sayıyı okumadan
                // da "burası kırmızı" görünsün.
                Meter(new Rect(row.x, row.yMax - 5f * U, row.width, 3f * U), rate, rateColor);

                Columns(new Rect(row.x, row.y, row.width, row.height - 6f * U),
                        Bold(stat.Level.ToString()), stat.Starts.ToString(),
                        stat.Clears.ToString(), stat.Fails.ToString(),
                        Tint(stat.Starts > 0 ? "%" + Mathf.RoundToInt(rate * 100f) : "—", rateColor),
                        S.Body);
            }

            ScrollEnd();
            DrawActions(analytics);
        }

        void DrawActions(LocalAnalytics analytics)
        {
            Section("sayaçlar");
            foreach (var pair in analytics.Counters) Kv(pair.Key, pair.Value.ToString());

            GUILayout.BeginHorizontal();
            if (Btn("DİSKE YAZ", 34f)) { analytics.Save(); Note("analitik diske yazıldı"); }
            if (Danger("analytics", "VERİYİ SİL", 34f))
            {
                analytics.Reset();
                _stats.Clear();
                Note("analitik sıfırlandı");
            }
            GUILayout.EndHorizontal();
        }

        void Rebuild(LocalAnalytics analytics)
        {
            _stats.Clear();
            foreach (var stat in analytics.Levels.Values)
            {
                if (_onlyPlayed && stat.Starts == 0) continue;
                _stats.Add(stat);
            }

            _stats.Sort((a, b) =>
            {
                switch (_sort)
                {
                    case 1:
                        int byRate = a.ClearRate.CompareTo(b.ClearRate);
                        return byRate != 0 ? byRate : b.Starts.CompareTo(a.Starts);
                    case 2:
                        int byStarts = b.Starts.CompareTo(a.Starts);
                        return byStarts != 0 ? byStarts : a.Level.CompareTo(b.Level);
                    default:
                        return a.Level.CompareTo(b.Level);
                }
            });
        }

        /// <summary>Beş sütunlu tablo satırı — sütun genişlikleri tek yerde.</summary>
        void Columns(Rect rect, string c0, string c1, string c2, string c3, string c4,
                     GUIStyle style)
        {
            float pad = 7f * U;
            float x = rect.x + pad;
            float width = rect.width - pad * 2f;
            float[] weights = { 0.18f, 0.20f, 0.18f, 0.18f, 0.26f };
            string[] cells = { c0, c1, c2, c3, c4 };

            for (int i = 0; i < cells.Length; i++)
            {
                float w = width * weights[i];
                GUI.Label(new Rect(x, rect.y, w, rect.height), cells[i], style);
                x += w;
            }
        }
    }
}
