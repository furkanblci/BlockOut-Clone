using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>
    /// 3D Önizleme sekmesi ve tuvalin gerçek görsel katmanı.
    ///
    /// Bölümü kuran ve render eden iş <see cref="LevelBoardPreview"/>'da;
    /// burada yalnız arayüz ve kamera denetimleri var. İki sekme AYNI kurulmuş
    /// tahtayı paylaşır — aynı anda ikisi görünmediği için bu hem doğru hem
    /// yarı maliyet.
    /// </summary>
    public sealed partial class LevelEditorWindow
    {
        LevelBoardPreview _board;

        [SerializeField] float _previewPitch = 80f;
        [SerializeField] float _previewYaw;
        [SerializeField] float _previewZoom = 1f;
        [SerializeField] bool _previewGameCamera = true;

        LevelBoardPreview Board => _board ?? (_board = new LevelBoardPreview());

        void CleanupPreview()
        {
            _board?.Dispose();
            _board = null;
        }

        // ---------------- tuvalin gerçek görseli ----------------

        /// <summary>
        /// Tuvale oyunun render'ını basar. Başarılıysa true — çağıran o zaman
        /// 2B tuğla çizimini atlar.
        ///
        /// Hizalama sözleşmesi: kamera tahtayı <paramref name="marginCells"/>
        /// kadar paylı kapsıyor; doku da tahta dikdörtgeni AYNI oranda
        /// büyütülerek basılır. Pay iki tarafta aynı olmazsa görüntü ızgaradan
        /// kayar ve tıklama yanlış hücreye düşer.
        /// </summary>
        bool DrawRealBoard()
        {
            if (_palette == null) return false;
            if (!Board.Ensure(_data, _palette, _path, _revision))
            {
                if (!string.IsNullOrEmpty(Board.Error)) _realBoardError = Board.Error;
                return false;
            }
            _realBoardError = null;
            if (Event.current.type != EventType.Repaint) return true;

            var rect = _canvas.BoardRect;
            // Pay PİKSEL cinsinden sınırlanır: yakınlaştırmada 1 hücrelik pay
            // tuval alanının dışına taşıp komşu arayüzün üstüne bulaşırdı.
            float marginPixels = Mathf.Min(_canvas.CellSize, 26f);
            float marginCells = marginPixels / Mathf.Max(1f, _canvas.CellSize);

            var target = new Rect(
                rect.x - marginPixels, rect.y - marginPixels,
                rect.width + marginPixels * 2f, rect.height + marginPixels * 2f);

            var texture = Board.RenderTopDown(target,
                _data.Board.Width, _data.Board.Height, marginCells);
            if (texture == null) return false;

            GUI.DrawTexture(target, texture, ScaleMode.StretchToFill, false);
            return true;
        }

        string _realBoardError;

        // ---------------- 3D Önizleme sekmesi ----------------

        void DrawPreviewTab()
        {
            using (LevelEditorSkin.BarScope())
            {
                if (LevelEditorSkin.BarButton("Yeniden kur", 90f,
                        "Bölümü sıfırdan kurar (materyal ya da görsel ayar değiştiyse)"))
                {
                    Board.Release();
                    Say("Önizleme yeniden kuruldu");
                }

                _previewGameCamera = LevelEditorSkin.BarToggle(_previewGameCamera,
                    "Oyun kamerası", 106f,
                    "Açık: oyunun kadrajı (80° eğim, 27° FOV). Kapalı: serbest yörünge.");

                using (new EditorGUI.DisabledScope(_previewGameCamera))
                {
                    GUILayout.Label("Eğim", LevelEditorSkin.RowLabel, GUILayout.Width(32));
                    _previewPitch = GUILayout.HorizontalSlider(_previewPitch, 20f, 89.5f,
                        GUILayout.Width(84));
                    GUILayout.Label("Dönüş", LevelEditorSkin.RowLabel, GUILayout.Width(40));
                    _previewYaw = GUILayout.HorizontalSlider(_previewYaw, -180f, 180f,
                        GUILayout.Width(84));
                }

                GUILayout.Label("Uzaklık", LevelEditorSkin.RowLabel, GUILayout.Width(48));
                _previewZoom = GUILayout.HorizontalSlider(_previewZoom, 0.55f, 2.2f,
                    GUILayout.Width(84));

                if (LevelEditorSkin.BarButton("Sıfırla", 58f))
                {
                    _previewPitch = 80f; _previewYaw = 0f; _previewZoom = 1f;
                    _previewGameCamera = true;
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label("sürükle = döndür · tekerlek = uzaklık", LevelEditorSkin.RowLabel);
            }

            var area = GUILayoutUtility.GetRect(200f, 200f,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (_palette == null)
            {
                LevelEditorSkin.Fill(area, LevelEditorSkin.Inset);
                LevelCanvasDrawer.Label(area, "ColorPalette.asset bulunamadı.",
                    LevelEditorSkin.DangerBright, 12);
                return;
            }

            bool ready = Board.Ensure(_data, _palette, _path, _revision);
            HandlePreviewInput(area);

            if (Event.current.type == EventType.Repaint)
            {
                var texture = ready
                    ? Board.RenderPerspective(area, _data.Board.Width, _data.Board.Height,
                        _previewGameCamera, _previewPitch, _previewYaw, _previewZoom)
                    : null;

                if (texture != null) GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, false);
                else
                {
                    LevelEditorSkin.Fill(area, LevelEditorSkin.Inset);
                    LevelCanvasDrawer.Label(area,
                        "Önizleme kurulamadı: " + (Board.Error ?? "bilinmeyen sebep"),
                        LevelEditorSkin.DangerBright, 12);
                }
            }

            if (ready) DrawPreviewLegend(area);
        }

        void HandlePreviewInput(Rect area)
        {
            var e = Event.current;
            if (!area.Contains(e.mousePosition)) return;

            if (e.type == EventType.ScrollWheel)
            {
                _previewZoom = Mathf.Clamp(_previewZoom * (1f + e.delta.y * 0.05f), 0.55f, 2.2f);
                e.Use(); Repaint();
                return;
            }

            if (e.type == EventType.MouseDrag && (e.button == 0 || e.button == 2))
            {
                // Sürüklemek serbest yörüngeye geçmek demektir; kullanıcı
                // döndürmeye çalışırken kamera kilitli kalırsa araç bozuk görünür.
                _previewGameCamera = false;
                _previewYaw = Mathf.Repeat(_previewYaw + e.delta.x * 0.5f + 180f, 360f) - 180f;
                _previewPitch = Mathf.Clamp(_previewPitch - e.delta.y * 0.4f, 20f, 89.5f);
                e.Use(); Repaint();
            }
        }

        void DrawPreviewLegend(Rect area)
        {
            var box = new Rect(area.x + 10f, area.y + 10f, 200f, 48f);
            LevelEditorSkin.RoundedRect(box, new Color(0f, 0f, 0f, 0.55f), LevelEditorSkin.Round6);

            var style = new GUIStyle(LevelEditorSkin.RowLabel) { fontSize = 10 };
            style.normal.textColor = LevelEditorSkin.Text;
            GUI.Label(new Rect(box.x + 8f, box.y + 4f, box.width - 16f, 16f),
                _previewGameCamera
                    ? "Oyun kamerası — oyuncunun gördüğü"
                    : $"Serbest · {_previewPitch:0}° / {_previewYaw:0}°",
                style);

            style.normal.textColor = LevelEditorSkin.TextMuted;
            GUI.Label(new Rect(box.x + 8f, box.y + 20f, box.width - 16f, 16f),
                $"{_data.Board.Width}×{_data.Board.Height} · {_data.Blocks.Count} blok · " +
                $"%{_previewZoom * 100f:0}", style);
            GUI.Label(new Rect(box.x + 8f, box.y + 33f, box.width - 16f, 14f),
                "BoardBuilder — oyunun kendi kodu", style);
        }
    }
}
