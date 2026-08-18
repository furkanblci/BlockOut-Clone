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
        TMPro.TextMeshProUGUI _logoTop, _logoBottom;

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

            _logoTop = UiKit.CreateTitle("Top", _logo, "BLOCK", 96,
                new Color(0.98f, 0.31f, 0.29f), new Color(0.24f, 0.06f, 0.30f));
            UiKit.Place(_logoTop, 0f, 0.50f, 1f, 1f);

            _logoBottom = UiKit.CreateTitle("Bottom", _logo, "OUT!", 96,
                new Color(1f, 0.79f, 0.13f), new Color(0.24f, 0.06f, 0.30f));
            UiKit.Place(_logoBottom, 0f, 0f, 1f, 0.50f);

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

            // DERS (logo BİR PARÇA değil, iki hamledir): Tek bir PopIn ile
            // logo bir bütün olarak büyüyordu — temiz ama cansız. İki satırı
            // sırayla göndermek (üst gelir, hemen ardından alt) hareketi bir
            // OLAYA çeviriyor; göz ikinci satırı beklerken yakalanıyor.
            _logo.localScale = Vector3.one;
            GameKit.FX.Juice.Run(DropIn(_logoTop.rectTransform, 0f));
            GameKit.FX.Juice.Run(DropIn(_logoBottom.rectTransform, 0.10f));
            // Fişek gösterisi FX.CelebrationFX içinde; kazanma ekranı da
            // aynısını kullanıyor (2. tur, 50. madde).
            GameKit.FX.Juice.Run(FX.CelebrationFX.Show(_root, bursts: 3, interval: 0.16f));
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

        /// <summary>
        /// Satırın yukarıdan düşüp yaylanarak oturması.
        /// </summary>
        static IEnumerator DropIn(RectTransform line, float delay)
        {
            if (line == null) yield break;

            Vector2 target = line.anchoredPosition;
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            const float duration = 0.34f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (line == null) yield break;
                float k = GameKit.FX.Juice.EaseOutBack(t / duration, 2.4f);
                line.anchoredPosition = target + new Vector2(0f, (1f - k) * 420f);
                line.localScale = Vector3.one * (0.65f + 0.35f * k);
                yield return null;
            }

            line.anchoredPosition = target;
            line.localScale = Vector3.one;
        }

    }
}
