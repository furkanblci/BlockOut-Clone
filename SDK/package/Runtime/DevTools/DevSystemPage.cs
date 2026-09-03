using GameKit.Services;
using UnityEngine;

namespace GameKit.DevTools
{
    /// <summary>
    /// SİSTEM sekmesi: cihaz künyesi, kare hızı araçları, zaman ölçeği ve
    /// konsolun kendi ayarları. Kitin içinde gelir — oyuna özel hiçbir şey yok.
    ///
    /// DERS (hata raporu cihaz künyesiyle başlar): "Bende kasıyor" cümlesi tek
    /// başına işe yaramaz; hangi cihaz, hangi çözünürlük, hangi sürüm, ne kadar
    /// bellek? Bu sekmenin ekran görüntüsü bir hata raporunun ilk yarısıdır.
    ///
    /// DERS (zaman ölçeği bir HATA AYIKLAMA aracıdır): Bir animasyonun
    /// ortasında ne olduğunu 60 fps'te göz göremez. Oyunu dörtte bir hızda
    /// çalıştırmak, kare kare video incelemeden çok daha hızlı bir yol.
    /// </summary>
    public sealed class DevSystemPage : DevPage
    {
        public override string Title => "SİSTEM";

        static readonly float[] TimeScales = { 0.25f, 0.5f, 1f, 2f };
        Vector2 _scroll;

        public override void Draw()
        {
            _scroll = ScrollBegin(_scroll);

            // Bir hata raporunun ilk yarısı: sürüm, cihaz, ekran, bellek.
            // Gerisi (işlemci modeli, güvenli alan dikdörtgeni) merak
            // giderir ama hiçbir kararı değiştirmez.
            CardBegin();
            Kv("Sürüm", Application.version + " · Unity " + Application.unityVersion +
                        (Debug.isDebugBuild ? " · dev" : ""));
            Kv("Cihaz", SystemInfo.deviceModel);
            Kv("Ekran", Screen.width + "x" + Screen.height + " @" + Screen.dpi.ToString("0") + "dpi");
            Kv("Bellek", SystemInfo.systemMemorySize + " MB · " + SystemInfo.graphicsDeviceName);
            Kv("Kare", Application.targetFrameRate + " fps hedef · vSync " +
                       QualitySettings.vSyncCount);
            CardEnd();

            Section("araçlar");
            GUILayout.BeginHorizontal();
            if (Btn(PerfProbe.Visible ? "FPS SAYACI ✓" : "FPS SAYACI", 34f))
            {
                PerfProbe.Visible = !PerfProbe.Visible;
                Note("fps sayacı " + (PerfProbe.Visible ? "açıldı" : "kapatıldı"));
            }
            if (Btn("KÜNYEYİ LOGA YAZ", 34f))
            {
                Debug.Log("[Device] " + SystemInfo.deviceModel + " · " + SystemInfo.operatingSystem +
                          " · " + Screen.width + "x" + Screen.height + " @" + Screen.dpi + "dpi · " +
                          SystemInfo.systemMemorySize + "MB · " + SystemInfo.graphicsDeviceName);
                Note("cihaz künyesi loga yazıldı");
            }
            GUILayout.EndHorizontal();

            Section("zaman ölçeği");
            GUILayout.BeginHorizontal();
            foreach (float scale in TimeScales)
            {
                bool active = Mathf.Approximately(Time.timeScale, scale);
                if (!Button("x" + scale.ToString("0.##"), active ? S.BtnAccent : S.Btn, 34f)) continue;
                Time.timeScale = scale;
                Note("zaman ölçeği x" + scale.ToString("0.##"));
            }
            GUILayout.EndHorizontal();
            Paragraph("Yavaşlatma yalnız görsel inceleme içindir; oyunun süre sayacı da yavaşlar. " +
                      "Konsolu kapatmadan önce x1'e döndür.");

            Section("konsol");
            GUILayout.BeginHorizontal();
            if (Btn(DevConsole.PauseWhileOpen ? "AÇIKKEN DURAKLAT ✓" : "AÇIKKEN DURAKLAT", 34f))
            {
                DevConsole.PauseWhileOpen = !DevConsole.PauseWhileOpen;
                // Ayar ANINDA geçerli olsun; bir sonraki açılışa ertelemek şaşırtıcı olurdu.
                if (DevConsole.Instance != null) DevConsole.Instance.ApplyPausePreference();
                Note("açıkken duraklatma " + (DevConsole.PauseWhileOpen ? "açık" : "kapalı"));
            }
            if (Btn("BOYUT", 34f) && DevConsole.Instance != null)
                DevConsole.Instance.CycleSize();
            if (Btn("KÖŞE", 34f) && DevConsole.Instance != null)
                DevConsole.Instance.CycleCorner();
            GUILayout.EndHorizontal();

            Paragraph("Panel S/M/L boyutlarında ve dört köşeden birine yaslanır; başlıktaki " +
                      "“—” onu köşedeki DEV rozetine indirir — o hâldeyken oyun tamamen " +
                      "görünür ve oynanır. Tercih cihazda saklanır.");

            Section("gizli tetik");
            Paragraph("Ekranın SOL ya da SAĞ ÜST köşesine 2 saniye içinde 5 kez dokun. " +
                      "Editörde F8 açar/kapatır, Esc kapatır. Görünür bir düğme yok: " +
                      "testçi oyunu oynasın, aracı arayan bulsun.");

            ScrollEnd();
        }
    }
}
