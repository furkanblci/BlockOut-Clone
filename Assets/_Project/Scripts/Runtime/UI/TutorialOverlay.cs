using System.Collections;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// İlk bölümde çıkan öğretici: bir bloktan kapısına doğru giden el.
    ///
    /// DERS (öğretici YAZI DEĞİL HAREKETTİR): "Blokları kendi renklerindeki
    /// kapıya sürükle" cümlesini kimse okumaz — mobil oyuncu ilk ekranda metin
    /// görürse geçer. Gösterilen bir el, aynı bilgiyi okumadan öğretir ve dili
    /// de gerektirmez. Tek satır yazı yalnızca DESTEK olarak duruyor.
    ///
    /// DERS (öğretici oyunu DURDURMAZ): Perde koyup "şuraya bas" demek yaygın
    /// ama sinir bozucu; oyuncu kendi keşfetmek isterse engellenmiş olur.
    /// Buradaki el dokunmayı ENGELLEMİYOR — oyuncu istediği an kendi
    /// oynayabilir, ilk sürükleme başlar başlamaz öğretici siliniyor.
    ///
    /// DERS (bir kez göster, bir daha gösterme): Gösterildiği kayda yazılıyor.
    /// Her açılışta tekrar eden öğretici, oyunu bilen oyuncuyu kaçırır.
    /// </summary>
    public sealed class TutorialOverlay : MonoBehaviour
    {
        const string SeenKey = "BlockOut.TutorialSeen";

        RectTransform _hand;
        TextMeshProUGUI _hint;
        Camera _camera;
        Coroutine _loop;

        /// <summary>Bölüm kurulduktan sonra çağrılır; gerekmiyorsa hiçbir şey yapmaz.</summary>
        public static void TryShow(GameSession session, LevelModel level, BoardSpace space)
        {
            if (session == null || level == null) return;

            // Yalnız ilk bölümde ve yalnız bir kez.
            if (session.LevelIndex != 0) return;
            if (PlayerPrefs.GetInt(SeenKey, 0) == 1) return;

            var target = FindDemoMove(level);
            if (target.block == null) return;

            var go = new GameObject("Tutorial");
            go.transform.SetParent(session.transform, worldPositionStays: false);
            var overlay = go.AddComponent<TutorialOverlay>();
            overlay.Begin(session, space, target.block, target.gate);
        }

        /// <summary>
        /// Gösterilecek hamleyi seçer: kapısıyla rengi uyuşan İLK blok.
        /// Karmaşık bir "en iyi hamle" aramaya gerek yok — birinci bölüm zaten
        /// tek adımlık ve amaç kuralı göstermek.
        /// </summary>
        static (BlockModel block, GateModel gate) FindDemoMove(LevelModel level)
        {
            foreach (var gate in level.Gates)
            {
                if (gate.IsGhost || gate.IsIced || gate.ColorQueue.Count == 0) continue;
                foreach (var block in level.Blocks)
                {
                    if (block.IsFrozen || block.Layers.Count == 0) continue;
                    if (block.CurrentColor == gate.ActiveColor) return (block, gate);
                }
            }
            return (null, null);
        }

        void Begin(GameSession session, BoardSpace space, BlockModel block, GateModel gate)
        {
            _camera = Camera.main;

            var canvas = UiKit.CreateCanvas("TutorialCanvas");
            canvas.transform.SetParent(transform, worldPositionStays: false);
            canvas.sortingOrder = 20;                    // HUD'ın üstünde
            var root = UiKit.CreateSafeArea(canvas);

            _hint = UiKit.CreateTitle("Hint", root,
                "Drag the block to its matching door", 30,
                new Color(1f, 0.98f, 0.94f), new Color(0.10f, 0.07f, 0.24f));
            UiKit.Place(_hint, 0.06f, 0.845f, 0.94f, 0.895f);
            _hint.textWrappingMode = TextWrappingModes.Normal;

            // İşaretçi: dokunmayı YUTMAZ, altındaki tahta tıklanabilir kalır.
            //
            // El görseli varsa o kullanılır. Yoksa dokunma HALKASI çiziliyor:
            // içi dolu bir daire ve etrafında saydam bir halka. Mobilde bu,
            // el kadar yaygın ve dil gerektirmeyen bir "buraya dokun" işareti.
            var handSprite = UiSkin.Get(Art.Hand);
            var holder = UiKit.CreateRect("Hand", root);
            holder.anchorMin = holder.anchorMax = new Vector2(0.5f, 0.5f);
            holder.sizeDelta = new Vector2(130f, 130f);
            _hand = holder;

            if (handSprite != null)
            {
                var hand = UiKit.CreateIcon("Glyph", holder, handSprite);
                UiKit.Place(hand, 0f, 0f, 1f, 1f);
            }
            else
            {
                var ring = UiKit.CreateRoundedPanel("Ring", holder,
                    new Color(1f, 1f, 1f, 0.30f));
                ring.pixelsPerUnitMultiplier = 0.05f;
                ring.raycastTarget = false;
                UiKit.Place(ring, 0f, 0f, 1f, 1f);

                var dot = UiKit.CreateRoundedPanel("Dot", holder,
                    new Color(1f, 1f, 1f, 0.85f));
                dot.pixelsPerUnitMultiplier = 0.05f;
                dot.raycastTarget = false;
                UiKit.Place(dot, 0.30f, 0.30f, 0.70f, 0.70f);
            }

            Vector3 from = space.RectCenterToWorld(block.Position, block.W, block.H, 0.4f);
            Vector3 to = GateWorld(space, gate);

            _loop = StartCoroutine(Demo(from, to));

            // İlk gerçek sürüklemede öğretici görevini tamamlar.
            session.PowerUps.Changed += Dismiss;
            _session = session;
        }

        GameSession _session;

        static Vector3 GateWorld(BoardSpace space, GateModel gate)
        {
            float center = (gate.SpanMin + gate.SpanMax) * 0.5f;
            return gate.EdgeHorizontal
                ? space.CornerToWorld(center, gate.EdgeCoord, 0.4f)
                : space.CornerToWorld(gate.EdgeCoord, center, 0.4f);
        }

        IEnumerator Demo(Vector3 from, Vector3 to)
        {
            while (true)
            {
                // Duraklat: parmak bloğun üstüne "iniyor".
                yield return Pulse(from, 0.45f);

                float duration = 1.1f;
                for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
                {
                    float k = GameKit.FX.Juice.EaseOutCubic(t / duration);
                    Place(Vector3.Lerp(from, to, k));
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.35f);
            }
        }

        IEnumerator Pulse(Vector3 at, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                Place(at);
                float scale = 1f + Mathf.Sin(t * 12f) * 0.08f;
                if (_hand != null) _hand.localScale = Vector3.one * scale;
                yield return null;
            }
            if (_hand != null) _hand.localScale = Vector3.one;
        }

        void Place(Vector3 world)
        {
            if (_hand == null || _camera == null) return;
            var screen = _camera.WorldToScreenPoint(world);
            // Ekran uzayı overlay canvas: konum doğrudan piksel.
            _hand.position = screen;
        }

        void Update()
        {
            // Oyuncu kendi oynamaya başladıysa öğretici çekilir.
            if (_session != null && _session.State == GameState.Playing &&
                UnityEngine.InputSystem.Pointer.current != null &&
                UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
                Dismiss();
        }

        void Dismiss()
        {
            if (_loop != null) StopCoroutine(_loop);
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            Destroy(gameObject);
        }
    }
}
