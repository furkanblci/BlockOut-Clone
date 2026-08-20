using System.Collections.Generic;
using UnityEngine;

namespace GameKit.DevTools
{
    /// <summary>
    /// Uygulamanın log akışını CİHAZDA biriktiren halka tampon.
    ///
    /// DERS (cihazda `Debug.LogError` HİÇBİR YERE gitmez): 2026-08-17 APK
    /// testinde bir bölüm açılmadı ve sebebi hiçbir yerde görünmedi — telefonda
    /// konsol yok, logcat için kablo gerekiyor. Hata mesajı yazılmıştı ama
    /// OKUNAMIYORDU. Uygulamanın kendi log akışını yakalayıp ekranda
    /// gösterebilmek, kablosuz teşhisin tamamıdır.
    ///
    /// DERS (aracın kendi işleri de bir olaydır): Konsoldan verilen jeton,
    /// atlanan bölüm, sıfırlanan kayıt — hepsi buraya "İŞLEM" olarak düşüyor.
    /// Böylece "önce ne yaptım da bu hata çıktı?" sorusunun cevabı, hatanın
    /// hemen üstündeki satırlarda duruyor.
    /// </summary>
    public static class DevLog
    {
        public enum Kind { Action, Log, Warning, Error }

        public sealed class Entry
        {
            public Kind Kind;
            public string Time;
            public string Message;
            public string Stack;
            public int Repeat = 1;
        }

        public const int Max = 400;

        static readonly List<Entry> Items = new List<Entry>(Max);
        static bool _installed;

        public static IReadOnlyList<Entry> Entries => Items;
        public static int Errors { get; private set; }

        /// <summary>Alt şeritte gösterilen son işlem.</summary>
        public static string LastAction { get; private set; } = "";

        /// <summary>Kayıt duraklatıldı mı? (Akış çok hızlıysa okumak için.)</summary>
        public static bool Paused;

        /// <summary>Yeni satır geldi — otomatik kaydırma bunu okur ve sıfırlar.</summary>
        public static bool Dirty;

        /// <summary>Unity loglarını dinlemeye başlar. Birden çok çağrı zararsız.</summary>
        public static void Install()
        {
            if (_installed) return;
            _installed = true;
            Application.logMessageReceived += OnUnityLog;
        }

        public static void Uninstall()
        {
            if (!_installed) return;
            _installed = false;
            Application.logMessageReceived -= OnUnityLog;
        }

        static void OnUnityLog(string message, string stack, LogType type)
        {
            if (Paused) return;

            Kind kind = type == LogType.Warning ? Kind.Warning
                      : type == LogType.Log ? Kind.Log
                      : Kind.Error;

            Add(kind, message, stack);
        }

        /// <summary>Konsolun kendi yaptığı işi akışa yazar.</summary>
        public static void Note(string message)
        {
            LastAction = message;
            Add(Kind.Action, message, "");
        }

        public static void Add(Kind kind, string message, string stack)
        {
            if (string.IsNullOrEmpty(message)) return;

            // Aynı mesaj arka arkaya gelirse (her karede atan bir istisna gibi)
            // listeyi doldurmak yerine sayacı artır: 400 satırın hepsi aynı
            // metin olursa akış okunamaz hâle gelir.
            if (Items.Count > 0)
            {
                var last = Items[Items.Count - 1];
                if (last.Kind == kind && last.Message == message)
                {
                    last.Repeat++;
                    Dirty = true;
                    return;
                }
            }

            Items.Add(new Entry
            {
                Kind = kind,
                Time = System.DateTime.Now.ToString("HH:mm:ss"),
                Message = message,
                Stack = stack
            });

            if (kind == Kind.Error) Errors++;

            if (Items.Count > Max)
            {
                if (Items[0].Kind == Kind.Error) Errors--;
                Items.RemoveAt(0);
            }

            Dirty = true;
        }

        public static void Clear()
        {
            Items.Clear();
            Errors = 0;
            Dirty = true;
        }
    }
}
