using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace GameKit.Editor.Setup
{
    /// <summary>
    /// TAŞAN YAZI AVCISI — bir ekran ağacındaki bütün etiketleri ölçer ve
    /// kutusuna sığmayanları listeler.
    ///
    /// NEDEN VAR (7. tur, M46): Kullanıcının isteği "hiçbir panelde, hiçbir
    /// ekranda, hiçbir yazı taşmamalı". Bu, gözle taranacak bir şey değil:
    /// ekranlar onlarca panel, paneller yüzlerce etiket taşıyor ve taşma
    /// çoğu zaman bir-iki piksel — ekran görüntüsünde fark edilmez, cihazda
    /// gözü rahatsız eder. Üstelik metin duruma göre değişiyor ("CLAIM" →
    /// "SEE YOU TOMORROW"), yani bir kez bakmak yetmez.
    ///
    /// DERS (denetim, düzeltmenin kendisi kadar önemli): <see cref="GameKit.UI.UiTextFit"/>
    /// taşmayı ÖNLÜYOR ama bir tabanı var (%45). Tabana dayanan bir etiket
    /// hâlâ taşar ve bu, etiketin yanlış kutuya konduğunun işaretidir —
    /// yani düzeltilmesi gereken gerçek bir tasarım hatası. Bu araç tam
    /// olarak o kalanları gösteriyor.
    /// </summary>
    public static class UiOverflowAudit
    {
        /// <summary>Sığmayan tek bir etiket.</summary>
        public readonly struct Hit
        {
            public readonly string Path;
            public readonly string Text;
            public readonly float BoxWidth, NeedWidth, FontSize;

            public Hit(string path, string text, float box, float need, float font)
            {
                Path = path; Text = text; BoxWidth = box; NeedWidth = need; FontSize = font;
            }

            public float Overflow => NeedWidth - BoxWidth;
            public override string ToString() =>
                $"{Path} | \"{Text}\" | kutu {BoxWidth:0} < gerek {NeedWidth:0} " +
                $"(+{Overflow:0}) | punto {FontSize:0.#}";
        }

        /// <summary>
        /// <paramref name="root"/> altındaki bütün TMP etiketlerini ölçer.
        ///
        /// <paramref name="slack"/>: kaç birim taşmaya göz yumulacağı. Yazı
        /// kutusunun kenarına DEĞMESİ taşma değildir; SDF konturu ve gölge
        /// zaten bir-iki birim yer kaplar.
        /// </summary>
        public static List<Hit> Scan(Transform root, float slack = 2f)
        {
            var hits = new List<Hit>();
            if (root == null) return hits;

            // Ölçümden ÖNCE düzenin kurulmuş olması şart: kutu genişliği
            // sıfırken her yazı "taşıyor" görünür.
            Canvas.ForceUpdateCanvases();

            foreach (var fit in root.GetComponentsInChildren<GameKit.UI.UiTextFit>(true))
                fit.FitNow();
            Canvas.ForceUpdateCanvases();

            foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (string.IsNullOrEmpty(label.text)) continue;
                if (!IsVisible(label, root)) continue;

                var rect = label.rectTransform.rect;
                if (rect.width <= 1f) continue;

                float need = label.GetPreferredValues(label.text,
                    Mathf.Infinity, Mathf.Infinity).x;
                if (need <= rect.width + slack) continue;

                hits.Add(new Hit(Path(label.transform, root), label.text,
                    rect.width, need, label.fontSize));
            }

            hits.Sort((a, b) => b.Overflow.CompareTo(a.Overflow));
            return hits;
        }

        /// <summary>Kapalı bir dalın içindeki yazı ekranda yok — taşamaz.</summary>
        static bool IsVisible(Component label, Transform root)
        {
            for (var t = label.transform; t != null && t != root.parent; t = t.parent)
                if (!t.gameObject.activeSelf) return false;
            return true;
        }

        static string Path(Transform node, Transform root)
        {
            var parts = new List<string>(8);
            for (var t = node; t != null && t != root; t = t.parent) parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        /// <summary>Konsola tek parça rapor basar; taşma sayısını döndürür.</summary>
        public static int Report(string screen, Transform root, float slack = 2f)
        {
            var hits = Scan(root, slack);
            var text = new StringBuilder();
            text.AppendLine($"[TaşmaDenetimi] {screen}: {hits.Count} taşan yazı");
            foreach (var hit in hits) text.AppendLine("  " + hit);
            if (hits.Count == 0) Debug.Log(text.ToString());
            else Debug.LogWarning(text.ToString());
            return hits.Count;
        }
    }
}
