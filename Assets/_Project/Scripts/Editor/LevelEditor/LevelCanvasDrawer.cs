using BlockOut.Core;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// Editör tuvalinin GEOMETRİSİ: ızgara yerleşimi, hücre/kenar dikdörtgenleri
    /// ve mouse konumunun hücreye/kenara çevrilmesi.
    ///
    /// DERS (IMGUI): OnGUI her olayda (Repaint, MouseDown, Layout...) baştan
    /// koşar; kalıcı durum tutulmaz, her karede yeniden çizilir. Bu yüzden
    /// "hangi piksel hangi hücre" hesabı tek yerde toplanır — çizim ve tıklama
    /// aynı dönüşümü kullanır, kayma olmaz.
    ///
    /// Hücre uzayı sözleşmesi runtime ile aynı: x sağa, y AŞAĞI artar.
    /// </summary>
    public sealed class LevelCanvasDrawer
    {
        public const float MinZoom = 0.4f, MaxZoom = 4f;

        public float CellSize { get; private set; }
        public Vector2 Origin { get; private set; }
        public Rect BoardRect { get; private set; }

        /// <summary>Tuvale sığdırılmış temel ölçeğin katı (1 = tam sığdır).</summary>
        public float Zoom = 1f;

        /// <summary>Piksel cinsinden kaydırma (orta tuş sürüklemesi).</summary>
        public Vector2 Pan;

        /// <summary>Verilen alana width×height ızgarayı ortalayarak yerleştirir.</summary>
        public void Layout(Rect area, int width, int height)
        {
            const float pad = 24f; // kapı barları tahta dışına taştığı için pay
            float fit = Mathf.Min((area.width - pad * 2f) / width, (area.height - pad * 2f) / height);
            CellSize = Mathf.Max(Mathf.Floor(fit * Mathf.Clamp(Zoom, MinZoom, MaxZoom)), 6f);

            float w = CellSize * width, h = CellSize * height;
            Origin = new Vector2(
                Mathf.Floor(area.x + (area.width - w) * 0.5f + Pan.x),
                Mathf.Floor(area.y + (area.height - h) * 0.5f + Pan.y));
            BoardRect = new Rect(Origin.x, Origin.y, w, h);
        }

        /// <summary>İmleç altındaki noktayı sabit tutarak yakınlaştırır (harita gibi).</summary>
        public void ZoomAt(Vector2 pivot, float delta)
        {
            float before = Zoom;
            Zoom = Mathf.Clamp(Zoom * (1f - delta * 0.06f), MinZoom, MaxZoom);
            if (Mathf.Approximately(before, Zoom)) return;

            // Pivotun ızgara üzerindeki yeri değişmesin diye kaydırmayı düzelt.
            Vector2 toPivot = pivot - (Origin + BoardRect.size * 0.5f);
            Pan -= toPivot * (Zoom / before - 1f);
        }

        public void ResetView()
        {
            Zoom = 1f;
            Pan = Vector2.zero;
        }

        public Rect RectFor(int x, int y, int w = 1, int h = 1) =>
            new Rect(Origin.x + x * CellSize, Origin.y + y * CellSize, w * CellSize, h * CellSize);

        /// <summary>Kapı barının dikdörtgeni (kenarın hemen dışında).</summary>
        public Rect GateRect(int x, int y, Side side, int length)
        {
            float t = CellSize * 0.22f;
            var cell = RectFor(x, y);
            switch (side)
            {
                case Side.North: return new Rect(cell.x, cell.y - t, CellSize * length, t);
                case Side.South: return new Rect(cell.x, cell.y + CellSize, CellSize * length, t);
                case Side.West:  return new Rect(cell.x - t, cell.y, t, CellSize * length);
                default:         return new Rect(cell.x + CellSize, cell.y, t, CellSize * length);
            }
        }

        /// <summary>Kenar çizgisinin dikdörtgeni (duvar çizimi için, hücre sınırında).</summary>
        public Rect EdgeRect(EdgeId edge)
        {
            float t = Mathf.Max(2f, CellSize * 0.12f);
            return edge.Horizontal
                ? new Rect(Origin.x + edge.X * CellSize, Origin.y + edge.Y * CellSize - t * 0.5f, CellSize, t)
                : new Rect(Origin.x + edge.X * CellSize - t * 0.5f, Origin.y + edge.Y * CellSize, t, CellSize);
        }

        /// <summary>Mouse konumundaki hücre. Izgara dışındaysa false.</summary>
        public bool TryCell(Vector2 mouse, int width, int height, out Vector2Int cell)
        {
            var f = (mouse - Origin) / CellSize;
            cell = new Vector2Int(Mathf.FloorToInt(f.x), Mathf.FloorToInt(f.y));
            return cell.x >= 0 && cell.y >= 0 && cell.x < width && cell.y < height;
        }

        /// <summary>
        /// Mouse'a en yakın hücre kenarı. Hücre içindeki kesirli konuma bakıp
        /// dört kenardan en yakınını seçer; ortaya yakınsa (hiçbiri baskın
        /// değilse) yine de en yakını verir — kenar araçları böylece "affedici" olur.
        /// </summary>
        public bool TryEdge(Vector2 mouse, int width, int height,
            out Vector2Int cell, out Side side)
        {
            side = Side.North;
            var f = (mouse - Origin) / CellSize;
            cell = new Vector2Int(Mathf.FloorToInt(f.x), Mathf.FloorToInt(f.y));
            if (cell.x < 0 || cell.y < 0 || cell.x >= width || cell.y >= height) return false;

            float fx = f.x - cell.x, fy = f.y - cell.y;
            float best = fy; side = Side.North;
            if (1f - fy < best) { best = 1f - fy; side = Side.South; }
            if (fx < best)      { best = fx;      side = Side.West; }
            if (1f - fx < best) { side = Side.East; }
            return true;
        }

        // ---------- çizim yardımcıları ----------

        public static void Fill(Rect r, Color c) => EditorGUI.DrawRect(r, c);

        public static void Outline(Rect r, Color c, float t = 1f)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - t, r.width, t), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, t, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - t, r.y, t, r.height), c);
        }

        /// <summary>Seçili nesnenin animasyonsuz "marching ants" benzeri vurgusu.</summary>
        public static void Highlight(Rect r, Color color)
        {
            Outline(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), color, 2f);
            Outline(r, Color.black, 1f);
        }

        public static void Label(Rect r, string text, Color color, int fontSize, FontStyle style = FontStyle.Bold)
        {
            var gs = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = fontSize,
                fontStyle = style
            };
            gs.normal.textColor = color;
            GUI.Label(r, text, gs);
        }
    }
}
