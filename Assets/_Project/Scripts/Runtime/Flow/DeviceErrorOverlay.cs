using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.Flow
{
    /// <summary>
    /// Cihazda konsol yerine geçen kat: yakalanmamış her hatayı EKRANA basar.
    ///
    /// NEDEN VAR: 2026-08-17 APK testinde "bölümler gelmiyor" denildi ve sebep
    /// günlerce bulunamadı. Oyunun içinde hatayı bildirecek mekanizmalar vardı
    /// (<see cref="GameSession.LoadFailure"/>) ama hepsi BİR EKRANA bağlıydı;
    /// hata o ekran kurulmadan önce olursa kimse görmüyordu. Telefonda konsol
    /// yok, logcat için kablo gerekiyor — yani her sessiz hata bir tur daha
    /// kaybettiriyor.
    ///
    /// DERS (teşhis aracı, teşhis edilecek şeyden BAĞIMSIZ olmalı): Bir ekranın
    /// hatasını o ekrana yazdırırsan, ekran hiç kurulamadığında elin boş kalır.
    /// Bu kat uygulama açılışında, her şeyden önce ve kendi kanvasında kurulur;
    /// altındaki hiçbir sistemin ayakta olmasına ihtiyaç duymaz.
    ///
    /// DERS (`Debug.LogError` bir yere GİTMEZ): Unity'nin `logMessageReceived`
    /// olayı `Debug.LogError`, `Debug.LogException` ve YAKALANMAMIŞ istisnaların
    /// hepsini veriyor — kodun hiçbir yerine dokunmadan tamamını toplayabiliyoruz.
    ///
    /// YAYINA ÇIKARKEN <see cref="Enabled"/> false yapılacak.
    /// </summary>
    public sealed class DeviceErrorOverlay : MonoBehaviour
    {
        /// <summary>Test sürümünde AÇIK. Yayına çıkarken kapatılacak.</summary>
        public const bool Enabled = true;

        /// <summary>Ekranda tutulacak en fazla hata; gerisi sayaçta.</summary>
        const int MaxShown = 6;

        // Olay BAŞKA BİR İŞ PARÇACIĞINDAN gelebilir; Unity arayüzüne oradan
        // dokunmak yasak. Mesajlar kuyruğa alınıp `Update` içinde işleniyor.
        readonly Queue<string> _pending = new Queue<string>();
        readonly List<string> _shown = new List<string>();
        readonly object _lock = new object();

        RectTransform _root;
        TextMeshProUGUI _text;
        int _total;

        public static DeviceErrorOverlay Create(Transform parent)
        {
            if (!Enabled) return null;

            var go = new GameObject("DeviceErrorOverlay");
            go.transform.SetParent(parent, worldPositionStays: false);
            var overlay = go.AddComponent<DeviceErrorOverlay>();

            // KENDİ KANVASI, EN ÜST SIRA: oyunun hiçbir kanvası kurulmasa da
            // görünmeli, kurulduysa da hepsinin üstünde kalmalı.
            var canvas = UiKit.CreateCanvas("ErrorCanvas");
            canvas.transform.SetParent(go.transform, worldPositionStays: false);
            canvas.sortingOrder = 32000;

            overlay._root = UiKit.CreateRect("Panel", canvas.transform);
            UiKit.Place(overlay._root, 0.02f, 0.28f, 0.98f, 0.86f);

            // TAM OPAK: teşhis metninin arkasından manzara sızarsa okunurluk
            // düşer ve asıl işini yapamaz.
            var back = UiKit.CreateRoundedPanel("Back", overlay._root,
                new Color(0.10f, 0.015f, 0.035f, 1f));
            UiKit.SetSliceScale(back, 0.4f);
            back.raycastTarget = true;
            UiKit.Place(back, 0f, 0f, 1f, 1f);

            var title = UiKit.CreateLabel("Title", overlay._root, "HATA", 44,
                new Color(1f, 0.45f, 0.42f));
            UiKit.Place(title, 0.04f, 0.905f, 0.96f, 0.985f);

            overlay._text = UiKit.CreateLabel("Body", overlay._root, "", 26,
                new Color(1f, 0.90f, 0.88f), TextAlignmentOptions.TopLeft);
            overlay._text.textWrappingMode = TextWrappingModes.Normal;
            overlay._text.overflowMode = TextOverflowModes.Truncate;
            UiKit.Place(overlay._text, 0.045f, 0.115f, 0.955f, 0.895f);

            // Kapatma: teşhis aracı testin ÖNÜNÜ KESMEMELİ. Okuyup kapatınca
            // oyun kaldığı yerden devam ediyor; yeni hata gelirse geri açılır.
            var close = UiKit.CreateRect("Close", overlay._root);
            UiKit.Place(close, 0.34f, 0.015f, 0.66f, 0.105f);
            var closeFace = UiKit.CreateRoundedPanel("Face", close,
                new Color(0.60f, 0.10f, 0.14f));
            UiKit.SetSliceScale(closeFace, 0.9f);
            UiKit.Place(closeFace, 0f, 0f, 1f, 1f);
            var closeLabel = UiKit.CreateLabel("Label", closeFace.transform, "KAPAT", 30,
                new Color(1f, 0.94f, 0.92f));
            UiKit.Place(closeLabel, 0.05f, 0.05f, 0.95f, 0.95f);
            UiKit.MakeClickable(close.gameObject, closeFace, overlay.Hide);

            overlay._root.gameObject.SetActive(false);
            Application.logMessageReceivedThreaded += overlay.OnLog;
            return overlay;
        }

        void OnDestroy() => Application.logMessageReceivedThreaded -= OnLog;

        void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            // Yığının ilk satırı çoğu zaman hatanın nerede olduğunu tek başına
            // söylüyor; tamamını basmak ekranı doldurup asıl mesajı kaçırtıyor.
            string where = FirstFrame(stackTrace);
            lock (_lock)
                _pending.Enqueue(string.IsNullOrEmpty(where)
                    ? condition
                    : condition + "\n    " + where);
        }

        static string FirstFrame(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return null;
            int end = stackTrace.IndexOf('\n');
            return end > 0 ? stackTrace.Substring(0, end).Trim() : stackTrace.Trim();
        }

        void Update()
        {
            // İki ayrı bayrak.
            //
            // DERS (kapatılamayan bir teşhis penceresi, teşhis aracı değil
            // engeldir): İlk hâlde her mesaj "değişti" sayılıyordu ve panel
            // yeniden açılıyordu. Ama `Update` içinden gelen bir
            // NullReference HER KARE tekrarlar; oyuncu KAPAT'a bassa da panel
            // bir sonraki karede geri gelir ve oyun kilitlenmiş gibi olur.
            // Panel yalnız DAHA ÖNCE GÖRÜLMEMİŞ bir hata gelince açılıyor;
            // tekrarlar sayaca yazılıyor.
            bool fresh = false, counted = false;

            while (true)
            {
                string message;
                lock (_lock)
                {
                    if (_pending.Count == 0) break;
                    message = _pending.Dequeue();
                }

                _total++;
                counted = true;
                if (_shown.Contains(message)) continue;

                if (_shown.Count >= MaxShown) _shown.RemoveAt(0);
                _shown.Add(message);
                fresh = true;
            }

            if (!counted) return;

            _text.text = string.Join("\n\n", _shown) +
                         (_total > _shown.Count ? $"\n\n(toplam {_total} hata)" : "");
            if (fresh && !_root.gameObject.activeSelf) _root.gameObject.SetActive(true);
        }

        void Hide() => _root.gameObject.SetActive(false);
    }
}
