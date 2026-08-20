using BlockOut.Core;
using BlockOut.Runtime.Flow;
using GameKit.DevTools;
using UnityEngine;

namespace BlockOut.Runtime.DevTools
{
    /// <summary>
    /// OYUN sekmesi: oynanan bölümü canlı izle ve SONUCU zorla.
    ///
    /// DERS (test kapısı gerçek yolu KULLANMALI): Kazanma panelini görmek için
    /// paneli elle açan bir kestirme yazmak cazip; ama o zaman test ettiğin şey
    /// gerçek akış olmaz — ödül hesabı, PERFECT ölçüsü, can iadesi ve bölüm
    /// açılışı çalışmadan panel açılır ve "doğru görünen ama yanlış veriyle
    /// dolu" bir ekranı onaylarsın. Buradaki düğmeler oturumun NORMAL bitiş
    /// yolunu çağırıyor; tek fark tetiğin nereden çekildiği.
    ///
    /// DERS (üç yıldızın üçünü de görebilmelisin): Panelin tek bir hâlini
    /// görmek yetmez — 1 yıldız, 2 yıldız ve PERFECT farklı rozet ve ödül
    /// gösterir. Yıldız kalan süreden hesaplandığı için düğmeler süreyi
    /// ayarlayıp aynı kuralı çalıştırıyor; kural değişirse konsol
    /// kendiliğinden uyar (bkz. GameSession.DebugForceWin).
    /// </summary>
    public sealed class DevPlayPage : DevPage
    {
        public override string Title => "OYUN";

        Vector2 _scroll;
        readonly int[] _colorTally = new int[10];

        static bool InGame =>
            DevMenu.Session != null && AppRoot.Current != null && AppRoot.Current.InGame;

        public override void Draw()
        {
            var session = DevMenu.Session;
            if (!InGame)
            {
                Empty("Şu anda bir bölüm oynanmıyor.");
                GUILayout.BeginHorizontal();
                if (BtnAccent("SON OYNANAN BÖLÜM", 38f))
                    DevLevelsPage.Jump(Mathf.Max(0, AppRouter.LastPlayedLevelIndex));
                if (Btn("1. BÖLÜM", 38f)) DevLevelsPage.Jump(0);
                GUILayout.EndHorizontal();
                Paragraph("Bölüm arama ve atlama için BÖLÜM sekmesine geç.");
                return;
            }

            _scroll = ScrollBegin(_scroll);

            DrawHeader(session);
            DrawOutcome(session);
            DrawTime(session);
            DrawBoard(session);
            DrawFlow(session);

            ScrollEnd();
        }

        void DrawHeader(GameSession session)
        {
            var timer = session.Timer;
            CardBegin();

            Kv("Bölüm", "#" + (session.LevelIndex + 1) + "/" + session.LevelCount + " · " +
                        DevLevelsPage.DifficultyName(session.Difficulty) + " ×" +
                        LevelDifficultyRule.RewardMultiplier(session.Difficulty));
            Kv("Durum", session.State + (DevMenu.PausedByConsole ? " (konsol)" : ""));

            // Süre çubuğu: sayıdan daha hızlı okunur, uyarı eşiği renkle görünür.
            var bar = GUILayoutUtility.GetRect(0f, 10f * U, GUILayout.ExpandWidth(true));
            float ratio = timer.Total > 0 ? timer.Remaining / timer.Total : 0f;
            Meter(bar, ratio, ratio <= 0.25f ? DevInk.Danger
                            : ratio <= 0.5f ? DevInk.Warn : DevInk.Accent);

            Kv("Süre", Clock(timer.Remaining) + " / " + Clock(timer.Total) +
                       (timer.Running ? "" : "  (durdu)") +
                       (session.PowerUps != null && session.PowerUps.IsTimeFrozen
                            ? "  ❄ " + Mathf.CeilToInt(session.PowerUps.FreezeRemaining) + "sn"
                            : ""));
            Kv("Ödül", session.State == GameState.Won
                ? session.LastStars + "★ · " + session.LastReward + " jeton" +
                  (session.LastPerfect ? " · PERFECT" : "")
                : session.PendingReward + " jeton (beklenen)");

            if (!string.IsNullOrEmpty(session.LoadFailure))
                GUILayout.Label(Tint("YÜKLEME HATASI: " + session.LoadFailure, DevInk.Danger),
                                S.Wrap, GUILayout.ExpandWidth(true));

            CardEnd();
        }

        void DrawOutcome(GameSession session)
        {
            Section("sonuç panelini zorla");

            // Etiketler KISA: panel ekranın yarısı kadarken "KAZAN 3★ PERFECT"
            // gibi bir yazı satırı içeriği panelden geniş yapıyor.
            GUILayout.BeginHorizontal();
            if (BtnAccent("KAZAN 3★", 40f)) ForceWin(session, 3, "3★ PERFECT");
            if (Btn("2★", 40f)) ForceWin(session, 2, "2★");
            if (Btn("1★", 40f)) ForceWin(session, 1, "1★");
            GUILayout.EndHorizontal();

            if (Button("KAYBETTİR", S.BtnDanger, 38f))
            {
                session.DebugForceLose();
                Note("bölüm kaybettirildi");
                DevConsole.SetVisible(false);
            }

            Paragraph("Gerçek bitiş yolu çağrılır (ödül, can, kayıt, analitik) ve konsol kapanır.");
        }

        static void ForceWin(GameSession session, int stars, string label)
        {
            session.DebugForceWin(stars);
            Note("bölüm kazandırıldı (" + label + ")");
            DevConsole.SetVisible(false);
        }

        void DrawTime(GameSession session)
        {
            Section("süre");

            GUILayout.BeginHorizontal();
            if (Btn("+30sn", 34f)) { session.Timer.AddTime(30); Note("+30 sn"); }
            if (Btn("TAM", 34f))
            {
                session.Timer.DebugSetRemaining(session.Timer.Total);
                Note("süre başa alındı");
            }
            if (Btn("10sn", 34f))
            {
                session.Timer.DebugSetRemaining(10f);
                Note("süre 10 sn'ye indirildi");
            }
            GUILayout.EndHorizontal();

            Paragraph("“+30sn” toplam süreyi de büyütür (reklamla devam yolu); diğer ikisi " +
                      "yalnız kalanı yazar, yıldız hesabı bozulmaz.");
        }

        /// <summary>
        /// Tahtanın CANLI dökümü: hangi renkten kaç blok kaldı, kapılar ne
        /// bekliyor, engeller ne durumda.
        ///
        /// DERS (bölüm dengelemesi tahmine bırakılmaz): "Bu bölüm neden
        /// bitmiyor?" sorusunun cevabı çoğu zaman "sarıdan iki blok kaldı ama
        /// sarı kapı buzlu" gibi somut bir şeydir. Ekrana bakıp saymak yerine
        /// aracın sayması, hem hızlı hem hatasız.
        /// </summary>
        void DrawBoard(GameSession session)
        {
            var level = session.ActiveLevel;
            Section("tahta");

            if (level == null) { Empty("Tahta kurulmadı."); return; }

            for (int i = 0; i < _colorTally.Length; i++) _colorTally[i] = 0;
            int frozen = 0, layered = 0, cells = 0;

            foreach (var block in level.Blocks)
            {
                _colorTally[(int)block.CurrentColor]++;
                if (block.IsFrozen) frozen++;
                if (block.Layers.Count > 1) layered++;
                cells += block.Cells.Count;
            }

            CardBegin();
            Kv("Kalan blok", level.Blocks.Count + " blok · " + cells + " hücre · " +
                             frozen + " buzlu · " + layered + " katmanlı");

            foreach (BlockColor color in System.Enum.GetValues(typeof(BlockColor)))
            {
                int count = _colorTally[(int)color];
                if (count == 0) continue;
                Kv(DevLevelIndex.ColorName(color), count + " blok");
            }

            int gateIndex = 0;
            foreach (var gate in level.Gates)
            {
                gateIndex++;
                Kv("Kapı " + gateIndex,
                   gate.Side + " · " + DevLevelIndex.ColorName(gate.ActiveColor) +
                   " · uzunluk " + gate.Length +
                   (gate.IsIced ? "  ❄ buz " + gate.IceCount : "") +
                   (gate.IsGhost ? "  (hayalet)" : ""));
            }

            foreach (var obstacle in level.Obstacles)
            {
                if (obstacle is CurtainModel curtain)
                    Kv("Perde", (curtain.IsOpen ? "açık" : "sayaç " + curtain.Count) +
                                " · içinde " + curtain.Contents.Count + " blok");
                else if (obstacle is GeneratorModel generator)
                    Kv("Makine", generator.Side + " · sırada " + generator.Remaining + " blok");
            }
            CardEnd();
        }

        void DrawFlow(GameSession session)
        {
            Section("akış");

            GUILayout.BeginHorizontal();
            if (Btn("YENİDEN", 36f))
            {
                session.Restart();
                Note("bölüm yeniden başlatıldı");
                DevConsole.SetVisible(false);
            }
            if (Btn("SONRAKİ", 36f) && session.HasNextLevel)
                DevLevelsPage.Jump(session.LevelIndex + 1);
            if (Btn("MENÜ", 36f))
            {
                DevConsole.SetVisible(false);
                AppRouter.GoHome();
                Note("menüye dönüldü");
            }
            GUILayout.EndHorizontal();
        }
    }
}
