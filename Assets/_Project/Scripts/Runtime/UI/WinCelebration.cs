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
        Image _logoImage;
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

            // GERÇEK LOGO GÖRSELİ (49. madde).
            //
            // ÖLÇÜM (`menus,powerups,vs.mp4` kazanma dizilimi, 384x832 kare):
            // logonun mor halesi x 54..321 — yani ekran genişliğinin %69,5'i;
            // dikey merkezi 0.496, tam ortada.
            //
            // DERS (harf biçimi TARİF EDİLEMEZ): Logo önce iki TMP satırıyla
            // kuruluyordu, sonra yapay zekâya "aynı tarzda" ürettirildi;
            // ikisi de referansa benzemedi. Bir markanın harfleri onun
            // kimliği — "kalın, yuvarlak, altın" diye tarif edilince ortaya
            // hep başka bir şey çıkıyor. Doğru yol referans karesinden
            // kesmekti; medyan alma hilesi de arkadaki konfetiyi sildi.
            // Dikdörtgen, görselin en-boy oranından DAHA UZUN tutuluyor.
            //
            // DERS (`preserveAspect` hangi kenara sığdırır?): Önce 0.695 x 0.20
            // yazdım — genişlik doğru olsun diye. Ölçtüğümde logo 0.589 çıktı:
            // dikdörtgen (1.95) görselden (1.66) daha YASSI olduğu için
            // `preserveAspect` YÜKSEKLİĞE sığdırdı ve genişlik kendiliğinden
            // küçüldü. İstenen kenarın bağlayıcı olması için diğer kenar bol
            // bırakılmalı.
            _logo = UiKit.CreateRect("Logo", _root);
            UiKit.Place(_logo, 0.1525f, 0.366f, 0.8475f, 0.626f);   // %69,5 genişlik

            var art = UiSkin.Get(Art.GameLogo);
            if (art != null)
            {
                _logoImage = UiKit.CreateIcon("Mark", _logo, art);
                UiKit.Place(_logoImage, 0f, 0f, 1f, 1f);
                _logoImage.preserveAspect = true;
            }
            else
            {
                // Görsel yoksa eski iki satırlık yazıya düşülür; ekran boş
                // kalmasın diye duruyor, tercih edilen yol değil.
                _logoTop = UiKit.CreateTitle("Top", _logo, "BLOCK", 96,
                    new Color(0.98f, 0.31f, 0.29f), new Color(0.24f, 0.06f, 0.30f));
                UiKit.Place(_logoTop, 0f, 0.50f, 1f, 1f);

                _logoBottom = UiKit.CreateTitle("Bottom", _logo, "OUT!", 96,
                    new Color(1f, 0.79f, 0.13f), new Color(0.24f, 0.06f, 0.30f));
                UiKit.Place(_logoBottom, 0f, 0f, 1f, 0.50f);
            }

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

            // Referansta logo tek parça hâlinde yaylanarak oturuyor (kesilen
            // görsel zaten tek parça). Yazıya düşülen yolda ise iki satır
            // sırayla geliyor — orada hareketi olaya çeviren şey oydu.
            if (_logoImage != null)
            {
                GameKit.FX.Juice.Run(DropIn(_logo, 0f));
            }
            else
            {
                GameKit.FX.Juice.Run(DropIn(_logoTop.rectTransform, 0f));
                GameKit.FX.Juice.Run(DropIn(_logoBottom.rectTransform, 0.10f));
            }

            // Fişek ARKADA, konfeti ÖNDE: ikisi de aynı kökte yaşıyor ama
            // fişekler önce yaratıldığı için çizim sırasında altta kalıyor.
            // Patlama aralığı kıvılcım ömründen (0,95 sn) KISA: bir patlama
            // sönmeden diğeri başlasın ki ekran hiç boş kalmasın. Referansta
            // her karede en az iki patlama birden var.
            // Referansta her patlama bizimkinden çok daha KALABALIK ve
            // patlamalar üst üste biniyor; 30 ışınlı seyrek bir çelenk
            // "havai fişek" değil "pusula gülü" gibi okunuyordu.
            GameKit.FX.Juice.Run(FX.CelebrationFX.Show(
                _root, bursts: 10, interval: 0.20f, sparks: 48));

            // KONFETİ SAYISI SAYILDI, tahmin edilmedi.
            //
            // Referans karelerinde logo bölgesi dışlanıp bağlı bileşenler
            // sayıldı: aynı anda ORTALAMA 276 parça var (161-432 arası).
            // Parça alanı medyanı 17 piksel², yani kenarı ~4,1 piksel —
            // 384 genişlikteki karede ekranın %1,07'si.
            //
            // İlk denemede 70, sonra 190 yazmıştım; ikisi de "kutlama" değil
            // "birkaç kağıt düştü" gibi okunuyordu. Bir yoğunluğu gözle
            // ayarlamak yerine saymak, üç denemeyi tek ölçüme indiriyor.
            GameKit.FX.Juice.Run(FX.CelebrationFX.Rain(
                _root, count: 300, duration: Show * 0.55f));

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

            if (_logoImage != null)
            {
                var ic = _logoImage.color;
                ic.a = alpha;
                _logoImage.color = ic;
            }
            SetTextAlpha(_logoTop, alpha);
            SetTextAlpha(_logoBottom, alpha);
        }

        static void SetTextAlpha(TextMeshProUGUI text, float alpha)
        {
            if (text == null) return;
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
