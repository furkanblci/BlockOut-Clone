using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Bölüm kazanıldığında ödül kartından ÖNCE oynayan kutlama.
    ///
    /// REFERANS (`menus,powerups,vs.mp4` 01:56–02:01, 4 fps'te çıkarıldı):
    /// dizilim dört adım. (1) Tahta boşalır, ekran siyaha döner. (2) "BLOCK
    /// OUT!" logosu siyahın üstünde yaylanarak oturur — önce "BLOCK", hemen
    /// ardından "OUT!". (3) Yaklaşık 4,5 saniye boyunca arkada havai fişekler
    /// patlar, önde yoğun renkli konfeti düşer; logo ortada durur. (4) Ancak
    /// bundan sonra "MÜKEMMEL!" ödül kartı açılır.
    ///
    /// Bizde 2. ve 3. adım hiç yoktu: tahta boşalıyor ve kart doğrudan
    /// açılıyordu. Kullanıcının "kazanma ekranında çıkan efektler" dediği şey
    /// tam olarak bu eksik iki adım (2. tur, 50. madde).
    ///
    /// DERS (kutlama REFERANSTAN KISA olmalı): Referans burada 4,5 saniye
    /// harcıyor. Elli bölüm oynayan biri için bu, elli kez beklemek demek.
    /// Bizim süremiz 2,2 saniye ve DOKUNULUNCA KESİLİYOR — gösteriyi izlemek
    /// isteyen izler, acelesi olan geçer. Atlanamayan kutlama, kutlama değil
    /// vergidir.
    /// </summary>
    public sealed class WinCelebration : MonoBehaviour
    {
        const float FadeIn = 0.22f;
        const float Show = 2.20f;
        const float FadeOut = 0.30f;

        Canvas _canvas;
        RectTransform _root;
        RectTransform _logo;
        Image _curtain;
        TextMeshProUGUI _logoTop, _logoBottom;
        Coroutine _running;
        bool _skip;

        public static WinCelebration Create(Transform parent)
        {
            var holder = new GameObject("WinCelebration");
            holder.transform.SetParent(parent, worldPositionStays: false);
            var celebration = holder.AddComponent<WinCelebration>();
            celebration.Build();
            return celebration;
        }

        void Build()
        {
            _canvas = UiKit.CreateCanvas("WinCanvas");
            _canvas.transform.SetParent(transform, worldPositionStays: false);
            _canvas.sortingOrder = 210;          // sonuç kartının da üstünde

            _root = UiKit.CreateRect("Root", _canvas.transform);
            UiKit.Place(_root, 0f, 0f, 1f, 1f);

            // Referansta zemin TAM SİYAH, oyunun moru değil: fişeklerin
            // parlaklığı ancak siyahın üstünde okunuyor. Mor zeminde aynı
            // kıvılcımlar soluk kalırdı.
            _curtain = UiKit.CreatePanel("Curtain", _root, new Color(0f, 0f, 0f, 1f));
            _curtain.raycastTarget = true;       // dokunuş kutlamayı keser

            var skip = _curtain.gameObject.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(() => _skip = true);

            _logo = UiKit.CreateRect("Logo", _root);
            UiKit.Place(_logo, 0.10f, 0.44f, 0.90f, 0.64f);

            // Logo LevelIntro'daki ile aynı: iki satır, iki renk. Gerçek blok
            // harfli görsel gelene kadar başlık malzemesiyle kuruluyor
            // (49. madde bu görseli bekliyor).
            _logoTop = UiKit.CreateTitle("Top", _logo, "BLOCK", 96,
                new Color(0.98f, 0.31f, 0.29f), new Color(0.24f, 0.06f, 0.30f));
            UiKit.Place(_logoTop, 0f, 0.50f, 1f, 1f);

            _logoBottom = UiKit.CreateTitle("Bottom", _logo, "OUT!", 96,
                new Color(1f, 0.79f, 0.13f), new Color(0.24f, 0.06f, 0.30f));
            UiKit.Place(_logoBottom, 0f, 0f, 1f, 0.50f);

            _root.gameObject.SetActive(false);
        }

        /// <summary>Kutlamayı oynatır; bittiğinde <paramref name="done"/> çağrılır.</summary>
        public void Play(System.Action done)
        {
            if (_running != null) GameKit.FX.Juice.Stop(_running);
            _running = GameKit.FX.Juice.Run(Routine(done));
        }

        IEnumerator Routine(System.Action done)
        {
            _skip = false;
            _root.gameObject.SetActive(true);
            SetAlpha(0f);

            yield return GameKit.FX.Juice.Tween(FadeIn, SetAlpha);

            // Logo iki hamlede geliyor: tek parça büyümek temiz ama cansız,
            // sırayla düşen iki satır hareketi bir OLAYA çeviriyor.
            GameKit.FX.Juice.Run(DropIn(_logoTop.rectTransform, 0f));
            GameKit.FX.Juice.Run(DropIn(_logoBottom.rectTransform, 0.10f));

            // Fişek ARKADA, konfeti ÖNDE: ikisi de aynı kökte yaşıyor ama
            // fişekler önce yaratıldığı için çizim sırasında altta kalıyor.
            // Patlama aralığı kıvılcım ömründen (0,95 sn) KISA: bir patlama
            // sönmeden diğeri başlasın ki ekran hiç boş kalmasın. Referansta
            // her karede en az iki patlama birden var.
            GameKit.FX.Juice.Run(FX.CelebrationFX.Show(
                _root, bursts: 8, interval: 0.26f, sparks: 30));

            // Konfeti YOĞUN: 70 parça ekrana serpiştirilince "kutlama" değil
            // "birkaç kağıt düştü" gibi okunuyordu. Referansta her karede
            // yüzlerce parça var. Doğumlar süreye yayılıyor ki tek bir yatay
            // dalga hâlinde geçmesinler.
            GameKit.FX.Juice.Run(FX.CelebrationFX.Rain(
                _root, count: 190, duration: Show * 0.55f));

            Services.AudioService.Star();

            for (float t = 0f; t < Show && !_skip; t += Time.unscaledDeltaTime)
                yield return null;

            yield return GameKit.FX.Juice.Tween(FadeOut, t => SetAlpha(1f - t));

            _root.gameObject.SetActive(false);
            _running = null;
            done?.Invoke();
        }

        /// <summary>
        /// Perdeyi ve logoyu birlikte söndürür.
        ///
        /// DERS (kıvılcımlar bu alfaya KATILMAZ): Alfa yalnız perde ve logo
        /// üstünde geziniyor; fişek ve konfeti kendi ömürlerini kendileri
        /// söndürüyor. Hepsini tek alfaya bağlamak, her karede onlarca
        /// görselin rengini yeniden yazmak demekti — üstelik yeni doğan bir
        /// kıvılcım o an sönmüş olarak başlardı.
        /// </summary>
        void SetAlpha(float alpha)
        {
            var color = _curtain.color;
            color.a = alpha;
            _curtain.color = color;

            SetTextAlpha(_logoTop, alpha);
            SetTextAlpha(_logoBottom, alpha);
        }

        static void SetTextAlpha(TextMeshProUGUI text, float alpha)
        {
            var c = text.color;
            c.a = alpha;
            text.color = c;
        }

        /// <summary>Satırın yukarıdan düşüp yaylanarak oturması.</summary>
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
