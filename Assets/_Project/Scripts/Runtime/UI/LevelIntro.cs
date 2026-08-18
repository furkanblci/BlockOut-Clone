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
            GameKit.FX.Juice.Run(Fireworks());
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

        /// <summary>
        /// Havai fişek: üç patlama, her biri merkezden dışa halka çizen
        /// kıvılcımlar.
        ///
        /// DERS (konfeti ile havai fişek farklı şeyler anlatır): Konfeti
        /// yukarıdan DÜŞER — kutlama biter, kağıtlar yere iner. Havai fişek
        /// merkezden DIŞA patlar; enerjisi dışa doğrudur ve "bir şey başlıyor"
        /// der. Bölüm geçişi bir bitiş değil bir başlangıç anı, o yüzden
        /// doğru olan patlama.
        ///
        /// Patlamalar arasına gecikme konuyor: aynı anda üç patlama tek bir
        /// gürültü lekesi olur, sırayla gelen üç patlama gösteri olur.
        /// </summary>
        IEnumerator Fireworks()
        {
            var palette = new[]
            {
                new Color(1f, 0.85f, 0.20f), new Color(0.98f, 0.31f, 0.38f),
                new Color(0.35f, 0.78f, 1f), new Color(0.55f, 1f, 0.45f),
                new Color(0.85f, 0.55f, 1f),
            };

            var spots = new[]
            {
                new Vector2(-0.24f,  0.20f),
                new Vector2( 0.26f,  0.05f),
                new Vector2(-0.06f, -0.22f),
            };

            for (int burst = 0; burst < spots.Length; burst++)
            {
                Explode(spots[burst], palette[burst % palette.Length]);
                yield return new WaitForSecondsRealtime(0.16f);
            }
        }

        void Explode(Vector2 anchor, Color color)
        {
            const int sparks = 18;

            for (int i = 0; i < sparks; i++)
            {
                var spark = UiKit.CreateRoundedPanel("Spark", _root, color);
                UiKit.SetSliceScale(spark, 0.10f);   // uçları yuvarlak çizgi
                spark.raycastTarget = false;

                var rect = spark.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f + anchor.x, 0.5f + anchor.y);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(26f, 9f);
                rect.anchoredPosition = Vector2.zero;

                // Kıvılcımlar halka boyunca EŞİT dağılır, rastgele değil:
                // rastgele açılar kümeleşip patlamayı tek yöne kaydırıyor.
                float angle = (i / (float)sparks) * Mathf.PI * 2f
                              + Random.Range(-0.08f, 0.08f);
                float speed = Random.Range(680f, 1050f);
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

                rect.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                GameKit.FX.Juice.Run(Spark(rect, velocity));
            }
        }

        /// <summary>Kıvılcım: dışa fırlar, yavaşlar, düşerek söner.</summary>
        static IEnumerator Spark(RectTransform piece, Vector2 velocity)
        {
            var position = Vector2.zero;
            var image = piece.GetComponent<Image>();
            Color color = image.color;
            const float life = 0.85f;

            for (float t = 0f; t < life; t += Time.unscaledDeltaTime)
            {
                if (piece == null) yield break;

                // Hava sürtünmesi: patlama hızlı başlar, hızla yavaşlar.
                velocity *= 1f - 3.2f * Time.unscaledDeltaTime;
                velocity.y -= 900f * Time.unscaledDeltaTime;
                position += velocity * Time.unscaledDeltaTime;

                piece.anchoredPosition = position;
                // Kıvılcım uzayıp incelir: hız yönünde iz bırakma hissi.
                float k = t / life;
                piece.localScale = new Vector3(1f + k * 0.8f, 1f - k * 0.7f, 1f);

                color.a = 1f - k * k;                 // sonlara doğru hızla söner
                image.color = color;
                yield return null;
            }

            if (piece != null) Destroy(piece.gameObject);
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
