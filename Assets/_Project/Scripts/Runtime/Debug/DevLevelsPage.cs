using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.Services;
using GameKit.DevTools;
using UnityEngine;

namespace BlockOut.Runtime.DevTools
{
    /// <summary>
    /// BÖLÜM sekmesi: ara, süz, sırala, atla.
    ///
    /// DERS (liste taşarsa araç işe yaramaz): İlk bölüm seçici 50 bölümü tek
    /// ızgaraya yığıyordu; son satırlar ekranın altından taşıyor ve
    /// SEÇİLEMİYORDU — yani aracın var oluş sebebi olan "son bölümü test et"
    /// işi tam da yapılamıyordu. İkinci sürüm kaydırma ekledi ama bu sefer de
    /// "buzlu bölüm hangisiydi?" sorusunun cevabı 50 kutuyu tek tek açmaktan
    /// geçiyordu.
    ///
    /// DERS (aramak, gezinmekten hızlıdır): Elli öğeyi gözle taramak yaklaşık
    /// on saniye; yazmak bir saniye. Liste artık bir ARAMA sonucu: numara,
    /// renk ("mavi"), mekanik ("perde"), zorluk ("çok zor") ya da dosya adı
    /// yazılabiliyor. Süzgeçler aynı işi tersinden yapıyor: aradığın şeyin
    /// adını bilmiyorsan çiplerle daraltıyorsun.
    /// </summary>
    public sealed class DevLevelsPage : DevPage
    {
        public override string Title => "BÖLÜM";

        string _query = "";
        bool _keypad;
        bool _fCleared, _fUncleared, _fLocked, _fHard;
        int _sort;                       // 0 sıra · 1 zorluk · 2 geçme oranı
        static readonly string[] SortLabels = { "SIRA", "ZORLUK", "ORAN" };

        Vector2 _scroll;
        int _expanded = -1;

        readonly List<DevLevelInfo> _view = new List<DevLevelInfo>();
        string _viewKey = " ";

        public override void OnOpen() => DevLevelIndex.Ensure();

        public override void Draw()
        {
            GUILayout.BeginHorizontal();
            _query = Field(_query, "ara: 12 · mavi · perde", 32f);
            if (Button("123", _keypad ? S.BtnAccent : S.Btn, 32f, 50f)) _keypad = !_keypad;
            if (Btn("TEMİZLE", 32f, 80f)) { _query = ""; _expanded = -1; }
            GUILayout.EndHorizontal();

            // DERS (cihazda klavyeye güvenme): IMGUI alanı Android'de ekran
            // klavyesini açar ama bazı cihazlarda gecikir ya da hiç açılmaz.
            // Numara tuş takımı, aracın en çok kullanılan işini (bölüm numarası
            // yaz, git) klavyeden bağımsız kılıyor.
            if (_keypad) DrawKeypad();

            // DERS (çip yerine yazı): İlk sürümde on çip vardı — altısı mekanik
            // süzgeciydi. Hepsi zaten arama kutusuyla yapılabiliyor ("buz",
            // "perde", "makine"), yani ekranın üçte biri, yazarak yapılabilen
            // bir işi tekrar sunuyordu. Kalan üç çip yazıyla ARANAMAYAN şeyler:
            // oyuncu kaydına ve kilit durumuna bakanlar.
            GUILayout.BeginHorizontal();
            if (Chip("GEÇİLMEDİ", _fUncleared)) { _fUncleared = !_fUncleared; if (_fUncleared) _fCleared = false; }
            if (Chip("GEÇİLDİ", _fCleared)) { _fCleared = !_fCleared; if (_fCleared) _fUncleared = false; }
            if (Chip("KİLİTLİ", _fLocked)) _fLocked = !_fLocked;
            if (Chip("ZOR+", _fHard)) _fHard = !_fHard;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            for (int i = 0; i < SortLabels.Length; i++)
                if (Chip(SortLabels[i], _sort == i)) _sort = i;

            RebuildView();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Tint(DevLevelIndex.Ready
                    ? _view.Count + "/" + DevLevelIndex.All.Count
                    : DevLevelIndex.Built + "/" + DevLevelIndex.Total + "…",
                DevInk.Muted), S.SmallRight, GUILayout.Height(24f * U), GUILayout.Width(70f * U));
            GUILayout.EndHorizontal();

            if (_view.Count == 0)
            {
                Empty(DevLevelIndex.Ready ? "Eşleşen bölüm yok." : "Bölümler okunuyor…");
                return;
            }

            _scroll = ScrollBegin(_scroll);
            for (int i = 0; i < _view.Count; i++) DrawRow(_view[i]);
            ScrollEnd();
        }

        void DrawKeypad()
        {
            GUILayout.BeginHorizontal();
            for (int digit = 1; digit <= 5; digit++)
                if (Btn(digit.ToString(), 30f)) _query += digit.ToString();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            for (int digit = 6; digit <= 9; digit++)
                if (Btn(digit.ToString(), 30f)) _query += digit.ToString();
            if (Btn("0", 30f)) _query += "0";
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (Btn("← SİL", 30f) && _query.Length > 0)
                _query = _query.Substring(0, _query.Length - 1);
            // Tek eşleşme varsa doğrudan oraya atla: "12 yaz, git" iki dokunuş.
            if (BtnAccent("GİT", 30f) && _view.Count > 0) Jump(_view[0].Index);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Görünen listeyi yalnız GİRDİ DEĞİŞİNCE yeniden kurar.
        ///
        /// DERS (OnGUI saniyede 60 kez çalışır): Süzme ve sıralamayı her karede
        /// yapmak 50 öğede sorun olmaz ama alışkanlık olarak yanlıştır — aynı
        /// kod 500 bölümde aracı yavaşlatır. Girdilerden bir anahtar üretip
        /// değişmediyse işi atlamak, önbelleğin en ucuz biçimi.
        /// </summary>
        void RebuildView()
        {
            string key = _query + "|" + _fCleared + _fUncleared + _fLocked + _fHard + "|" +
                         _sort + "|" + DevLevelIndex.Built + "|" +
                         (MetaServices.Ready ? MetaServices.Progress.HighestUnlockedIndex : -1);
            if (key == _viewKey) return;
            _viewKey = key;

            _view.Clear();
            var tokens = _query.ToLowerInvariant().Split(' ');

            foreach (var info in DevLevelIndex.All)
            {
                if (_fHard && info.Difficulty == LevelDifficulty.Normal) continue;

                var record = MetaServices.Ready ? MetaServices.Progress.Peek(info.Id) : null;
                bool cleared = record != null && record.Cleared;
                if (_fCleared && !cleared) continue;
                if (_fUncleared && cleared) continue;
                if (_fLocked && MetaServices.Ready &&
                    MetaServices.Progress.IsUnlocked(info.Index)) continue;

                bool matches = true;
                foreach (var token in tokens)
                {
                    if (token.Length == 0) continue;
                    if (info.Haystack.Contains(token)) continue;
                    matches = false;
                    break;
                }
                if (matches) _view.Add(info);
            }

            _view.Sort(Compare);
        }

        int Compare(DevLevelInfo a, DevLevelInfo b)
        {
            switch (_sort)
            {
                case 1:
                    int byScore = b.Score.CompareTo(a.Score);
                    return byScore != 0 ? byScore : a.Index.CompareTo(b.Index);
                case 2:
                    // En düşük geçme oranı en üstte: "burası tıkanıyor".
                    int byRate = ClearRate(a).CompareTo(ClearRate(b));
                    return byRate != 0 ? byRate : a.Index.CompareTo(b.Index);
                default:
                    return a.Index.CompareTo(b.Index);
            }
        }

        static float ClearRate(DevLevelInfo info)
        {
            var analytics = AppBootstrap.Analytics;
            if (analytics == null) return 2f;   // veri yoksa en sona
            return analytics.Levels.TryGetValue(info.Number, out var stat) && stat.Starts > 0
                ? stat.ClearRate
                : 2f;
        }

        // ------------------------------------------------------------------ satır

        void DrawRow(DevLevelInfo info)
        {
            var session = DevMenu.Session;
            bool current = session != null && AppRoot.Current != null &&
                           AppRoot.Current.InGame && session.LevelIndex == info.Index;
            var record = MetaServices.Ready ? MetaServices.Progress.Peek(info.Id) : null;
            bool unlocked = !MetaServices.Ready || MetaServices.Progress.IsUnlocked(info.Index);

            var row = GUILayoutUtility.GetRect(0f, 42f * U, GUILayout.ExpandWidth(true));
            float detailWidth = 30f * U;

            var main = new Rect(row.x, row.y, row.width - detailWidth - 2f * U, row.height - 3f * U);
            if (GUI.Button(main, GUIContent.none, current ? S.RowOn : S.Row)) Jump(info.Index);

            // Sol kenarda zorluk şeridi: göz, listeyi okumadan da tarayabilsin.
            Fill(new Rect(main.x, main.y, 3f * U, main.height), DifficultyColor(info.Difficulty));

            // DERS (dar panelde SABİT genişlik tutmaz): Kolonlar piksel yerine
            // satırın ORANI ile bölünüyor; panel ister ekranın yarısı olsun
            // ister tamamı, aynı düzen okunur kalıyor.
            float pad = 7f * U;
            float x = main.x + pad;
            float numberWidth = 34f * U;
            float statusWidth = main.width * 0.28f;
            float textWidth = main.width - numberWidth - statusWidth - pad * 2f;

            GUI.Label(new Rect(x, main.y, numberWidth, main.height),
                      Bold(Tint(info.Number.ToString(), unlocked ? DevInk.Text : DevInk.Muted)),
                      S.Head);
            x += numberWidth;

            string title = info.Error != null
                ? Tint("HATA: " + info.Error, DevInk.Danger)
                : info.Width + "x" + info.Height + " · " + info.Blocks + " blok · " +
                  info.ColorCount + " renk";

            GUI.Label(new Rect(x, main.y + 2f * U, textWidth, 19f * U), title, S.Body);
            GUI.Label(new Rect(x, main.y + 20f * U, textWidth, 17f * U),
                      string.IsNullOrEmpty(info.Mechanics)
                          ? Tint("—", DevInk.Muted)
                          : Tint(info.Mechanics, DevInk.Warn), S.Small);

            // Sağ kolon: oyuncu kaydı — geçildi mi, kaç denemede.
            string status;
            if (current) status = Tint("● AÇIK", DevInk.Accent2);
            else if (record != null && record.Cleared)
                status = Tint(record.Perfect ? "✓ PERFECT" : "✓ geçildi", DevInk.Accent2) +
                         "\n" + Tint(record.Attempts + " deneme", DevInk.Muted);
            else if (!unlocked) status = Tint("kilitli", DevInk.Muted);
            else if (record != null) status = Tint(record.Attempts + " deneme", DevInk.Warn);
            else status = Tint("oynanmadı", DevInk.Muted);

            GUI.Label(new Rect(main.xMax - statusWidth - pad, main.y, statusWidth, main.height),
                      status, S.SmallRight);

            var detail = new Rect(row.xMax - detailWidth, row.y, detailWidth, row.height - 3f * U);
            if (GUI.Button(detail, _expanded == info.Index ? "▲" : "▼", S.Btn))
                _expanded = _expanded == info.Index ? -1 : info.Index;

            if (_expanded == info.Index) DrawDetail(info, record);
        }

        void DrawDetail(DevLevelInfo info, Core.Save.LevelRecord record)
        {
            CardBegin();

            Kv("Dosya", info.Id);
            Kv("Zorluk", DifficultyName(info.Difficulty) + "  (puan " + info.Score + ")");
            Kv("Renkler", string.IsNullOrEmpty(info.Colors) ? "—" : info.Colors);
            Kv("Bloklar", info.Blocks + " blok · " + info.Layered + " katmanlı · " +
                          info.Polyomino + " şekilli · " + info.Ice + " buzlu · " +
                          info.AxisLocked + " eksen kilitli");
            Kv("Engeller", info.Curtains + " perde · " + info.Generators + " makine (" +
                           info.Queued + " sırada) · " + info.GateIce + " buzlu kapı");
            Kv("Kayıt", record != null
                ? (record.Cleared ? "geçildi" : "geçilmedi") + " · " + record.Attempts +
                  " deneme · en iyi " + record.BestRemainingSeconds + "sn kaldı" +
                  (record.Perfect ? " · PERFECT" : "")
                : "yok");

            var analytics = AppBootstrap.Analytics;
            if (analytics != null && analytics.Levels.TryGetValue(info.Number, out var stat))
                Kv("Analitik", stat.Starts + " başlangıç · " + stat.Clears + " geçme · " +
                               stat.Fails + " kayıp · %" +
                               Mathf.RoundToInt(stat.ClearRate * 100f) + " oran");

            GUILayout.BeginHorizontal();
            if (BtnAccent("OYNA", 32f)) Jump(info.Index);
            if (Btn("BURAYA KADAR AÇ", 32f) && MetaServices.Ready)
            {
                MetaServices.Save.Mutate(data =>
                    data.HighestUnlockedIndex = Mathf.Max(data.HighestUnlockedIndex, info.Index));
                _viewKey = " ";
                Note("bölüm " + info.Number + "'e kadar açıldı");
            }
            if (Btn("KAYDI SİL", 32f) && MetaServices.Ready)
            {
                MetaServices.Save.Mutate(data => data.Levels.Remove(info.Id));
                _viewKey = " ";
                Note(info.Id + " kaydı silindi");
            }
            GUILayout.EndHorizontal();

            CardEnd();
        }

        static Color DifficultyColor(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard: return DevInk.Warn;
                case LevelDifficulty.SuperHard: return DevInk.Danger;
                default: return DevInk.Accent2;
            }
        }

        public static string DifficultyName(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Hard: return "Zor";
                case LevelDifficulty.SuperHard: return "Çok Zor";
                default: return "Normal";
            }
        }

        /// <summary>
        /// Seçilen bölüme atlar ve konsolu kapatır.
        ///
        /// Oyun içindeysek oturuma söylüyoruz (sahne kurulumu korunur); menüden
        /// geliyorsak yönlendirici oynanışı açıyor. İki yol da NORMAL giriş
        /// yolları — konsola özel bir bölüm yükleyici yazmak, gerçekte hiç
        /// çalışmayan ikinci bir kod yolunu test etmek olurdu.
        /// </summary>
        public static void Jump(int index)
        {
            DevConsole.SetVisible(false);

            var session = DevMenu.Session;
            if (session != null && AppRoot.Current != null && AppRoot.Current.InGame)
                session.GoToLevel(index);
            else
                AppRouter.PlayLevel(index);

            Note("bölüm " + (index + 1) + " açıldı");
        }
    }
}
