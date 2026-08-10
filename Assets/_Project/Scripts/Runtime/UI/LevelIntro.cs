using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Bölümler arasındaki geçiş: siyah perde, oyun logosu ve konfeti.
    ///
    /// DERS (geçiş oyunun NEFES ALDIĞI yerdir): Bölüm bitip yenisi anında
    /// açıldığında oyuncu iki tahtayı birbirine karıştırıyor — hangi bloğu
    /// nerede bıraktığını hatırlıyor ama tahta değişmiş oluyor. Kısa bir perde
    /// eski tahtayı gözden siliyor ve yeni bölüme temiz bir sayfayla
    /// başlatıyor. Referans oyun tam olarak bunu yapıyor.
    ///
    /// DERS (geçiş KISA olmalı): Bir saniyeyi geçen her geçiş, ellinci kez
    /// görüldüğünde işkenceye döner. Buradaki toplam süre 0.9 saniye ve
    /// dokunulunca hemen kesiliyor — oyuncu beklemek zorunda değil.
    /// </summary>
    public sealed class LevelIntro : MonoBehaviour
    {
        const float FadeIn = 0.18f;
        const float Hold = 0.45f;
        const float FadeOut = 0.28f;

        static LevelIntro _instance;

        Canvas _canvas;
        RectTransform _root;
        Image _curtain;
        RectTransform _logo;
        Coroutine _running;

        public static LevelIntro Ensure(Transform parent)
        {
            if (_instance != null) return _instance;

            var holder = new GameObject("LevelIntro");
            holder.transform.SetParent(parent, worldPositionStays: false);
            _instance = holder.AddComponent<LevelIntro>();
            _instance.Build();
            return _instance;
        }

        void Build()
        {
            _canvas = UiKit.CreateCanvas("IntroCanvas");
            _canvas.transform.SetParent(transform, worldPositionStays: false);
            _canvas.sortingOrder = 200;              // HUD'ın da üstünde

            _root = UiKit.CreateRect("Root", _canvas.transform);
            UiKit.Place(_root, 0f, 0f, 1f, 1f);

            _curtain = UiKit.CreatePanel("Curtain", _root, new Color(0.02f, 0.01f, 0.06f, 1f));
            _curtain.raycastTarget = true;

            // Logo: kendi görseli yok, yazıyla kuruluyor. İki satır ve iki renk
            // referanstaki dizilimi veriyor.
            _logo = UiKit.CreateRect("Logo", _root);
            UiKit.Place(_logo, 0.10f, 0.42f, 0.90f, 0.62f);

            var top = UiKit.CreateTitle("Top", _logo, "BLOCK", 92,
                new Color(0.98f, 0.31f, 0.29f), new Color(0.24f, 0.06f, 0.30f));
            UiKit.Place(top, 0f, 0.50f, 1f, 1f);

            var bottom = UiKit.CreateTitle("Bottom", _logo, "OUT!", 92,
                new Color(1f, 0.79f, 0.13f), new Color(0.24f, 0.06f, 0.30f));
            UiKit.Place(bottom, 0f, 0f, 1f, 0.50f);

            _root.gameObject.SetActive(false);
        }

        /// <summary>Perdeyi oynatır; ortasında <paramref name="swap"/> çağrılır.</summary>
        public void Play(System.Action swap)
        {
            if (_running != null) GameKit.FX.Juice.Stop(_running);
            _running = GameKit.FX.Juice.Run(Routine(swap));
        }

        IEnumerator Routine(System.Action swap)
        {
            _root.gameObject.SetActive(true);
            _logo.localScale = Vector3.zero;

            yield return GameKit.FX.Juice.Tween(FadeIn, t => SetAlpha(t));

            // Tahta perdenin ARKASINDA değişir: oyuncu iki tahtayı birlikte
            // görmediği için karışıklık olmuyor.
            swap?.Invoke();

            GameKit.FX.Juice.Run(GameKit.FX.Juice.PopIn(_logo, 0.30f));
            BurstConfetti();
            Services.AudioService.Star();

            yield return new WaitForSecondsRealtime(Hold);
            yield return GameKit.FX.Juice.Tween(FadeOut, t => SetAlpha(1f - t));

            _root.gameObject.SetActive(false);
            _running = null;
        }

        void SetAlpha(float alpha)
        {
            var color = _curtain.color;
            color.a = alpha;
            _curtain.color = color;

            foreach (var text in _logo.GetComponentsInChildren<TextMeshProUGUI>())
            {
                var c = text.color;
                c.a = alpha;
                text.color = c;
            }
        }

        /// <summary>Perdenin üstüne renkli kağıt parçaları saçar.</summary>
        void BurstConfetti()
        {
            var colors = new[]
            {
                new Color(0.18f, 0.80f, 0.05f), new Color(1f, 0.78f, 0.10f),
                new Color(0.95f, 0.30f, 0.45f), new Color(0.25f, 0.62f, 0.98f),
                new Color(0.66f, 0.35f, 0.92f), new Color(1f, 1f, 1f),
            };

            for (int i = 0; i < 44; i++)
            {
                var piece = UiKit.CreateRoundedPanel($"Confetti_{i}", _root,
                    colors[i % colors.Length]);
                piece.pixelsPerUnitMultiplier = 0.4f;
                piece.raycastTarget = false;

                var rect = piece.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(Random.Range(14f, 28f), Random.Range(20f, 38f));
                rect.anchoredPosition = new Vector2(Random.Range(-120f, 120f), -60f);

                float angle = Random.Range(35f, 145f) * Mathf.Deg2Rad;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))
                               * Random.Range(900f, 1900f);
                GameKit.FX.Juice.Run(Fly(rect, velocity, Random.Range(-540f, 540f)));
            }
        }

        static IEnumerator Fly(RectTransform piece, Vector2 velocity, float spin)
        {
            var position = piece.anchoredPosition;
            float angle = 0f;

            for (float t = 0f; t < 1.4f; t += Time.unscaledDeltaTime)
            {
                if (piece == null) yield break;
                velocity.y -= 2400f * Time.unscaledDeltaTime;
                position += velocity * Time.unscaledDeltaTime;
                angle += spin * Time.unscaledDeltaTime;
                piece.anchoredPosition = position;
                piece.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            if (piece != null) Destroy(piece.gameObject);
        }
    }
}
