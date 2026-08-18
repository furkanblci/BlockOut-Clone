using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BlockOut.Runtime.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.Flow
{
    /// <summary>
    /// Açılış zinciri: stüdyo ekranı → oyun yükleme ekranı → Main sahnesi.
    ///
    /// Referanslar (946×2048, `OneDrive/Masaüstü/Block Out! Videos/`):
    ///   • `grand açılış.jpeg` — bordo degrade zemin, ortada beyaz küçük harf
    ///     "grand". Zemin köşede #81001F, dikey ortada #C50133 — yani üstten
    ///     ve alttan koyulaşan bir degrade.
    ///   • `oyun açılış.jpeg` — gökyüzü/bulut illüstrasyonu, üstte blok harfli
    ///     "BLOCK OUT!" logosu, altta "Yükleniyor…".
    ///
    /// DERS (yükleme ekranı BOŞ ZAMANI DOLDURMAZ, beklentiyi yönetir): Bu
    /// ekranın işi bir ilerleme çubuğu göstermek değil; oyuncuya "açılıyor,
    /// donmadı" demek. Bu yüzden en az bir süre EKRANDA KALIYOR — sahne çok
    /// hızlı yüklenirse bile bir anlık titreme yerine sakin bir geçiş görünür.
    ///
    /// DERS (Boot sahnesi neden boş kalmalı): Ağır bir sahne açılışta
    /// yüklenirse marka ekranı bile gösterilemeden saniyeler geçer. Buradaki
    /// her şey KODLA kuruluyor; sahnede tek bir bileşen var.
    /// </summary>
    public sealed class BootSplash : MonoBehaviour
    {
        /// <summary>Stüdyo ekranının süresi.</summary>
        const float StudioSeconds = 1.4f;

        /// <summary>Yükleme ekranının EN AZ kalma süresi.</summary>
        const float LoadingSeconds = 1.2f;

        const float FadeSeconds = 0.30f;

        // Referans karesinden örneklenen bordo tonları.
        static readonly Color StudioEdge   = new Color(0.506f, 0f, 0.122f);      // #81001F
        static readonly Color StudioCenter = new Color(0.773f, 0.004f, 0.200f);  // #C50133
        static readonly Color Ink          = new Color(1f, 0.99f, 0.96f);

        CanvasGroup _studio, _loading;

        /// <summary>Zincir bitince çağrılır — Main sahnesini BootLoader yükler.</summary>
        public System.Action Finished;

        public static BootSplash Create(System.Action finished)
        {
            var go = new GameObject("BootSplash");

            // SAHNE GEÇİŞİNDE YAŞAMALI.
            //
            // DERS (yükleme ekranı, yükleme BAŞLARKEN ölmemeli): İlk hâlde
            // splash Boot sahnesinin nesnesiydi; `SceneRouter.Load(Main)`
            // çağrıldığı anda Boot boşaltılıyor ve ekran TAM DA işe yarayacağı
            // anda kayboluyordu. Oyuncu illüstrasyonu görüp sonra yükleme
            // takılmasını çıplak yaşıyordu. Perde, arkasındaki iş bitene kadar
            // durmalı.
            DontDestroyOnLoad(go);

            var splash = go.AddComponent<BootSplash>();
            splash.Finished = finished;
            splash.Build();
            return splash;
        }

        /// <summary>
        /// Yükleme bitti: perde yumuşakça açılıp kendini yok ediyor.
        /// Main sahnesi yüklendikten SONRA çağrılır.
        /// </summary>
        public void Dismiss() => GameKit.FX.Juice.Run(FadeOutAndDie());

        System.Collections.IEnumerator FadeOutAndDie()
        {
            // Yeni sahnenin ilk karesi çizilsin; hemen sönersek bir kare
            // boşluk görünür.
            yield return null;

            float t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                _loading.alpha = 1f - Mathf.Clamp01(t / FadeSeconds);
                yield return null;
            }
            Destroy(gameObject);
        }

        void Build()
        {
            var canvas = UiKit.CreateCanvas("SplashCanvas");
            canvas.transform.SetParent(transform, worldPositionStays: false);
            canvas.sortingOrder = 100;              // her şeyin üstünde

            _loading = BuildLoading(canvas.transform);
            _studio = BuildStudio(canvas.transform);   // en son kurulan en üstte
        }

        // ---------------------------------------------------------- stüdyo

        CanvasGroup BuildStudio(Transform canvas)
        {
            var page = UiKit.CreateRect("Studio", canvas);
            UiKit.Place(page, 0f, 0f, 1f, 1f);
            var group = page.gameObject.AddComponent<CanvasGroup>();

            // Zemin: koyu taban + ortada açılan bant. Referansta degrade dikey,
            // ortası parlak; iki yumuşak geçiş sprite'ı bunu tek renkten çok
            // daha yakın veriyor.
            var baseFill = UiKit.CreatePanel("Base", page, StudioEdge);
            baseFill.raycastTarget = false;

            var glow = UiKit.CreatePanel("Glow", page, StudioCenter);
            glow.raycastTarget = false;
            UiKit.Place(glow, 0f, 0.30f, 1f, 0.70f);

            var up = UiKit.CreateRect("FadeUp", page);
            var upImage = up.gameObject.AddComponent<Image>();
            upImage.sprite = MenuSprites.FadeDown;
            upImage.type = Image.Type.Sliced;
            upImage.color = StudioCenter;
            upImage.raycastTarget = false;
            UiKit.Place(up, 0f, 0.70f, 1f, 1f);

            var down = UiKit.CreateRect("FadeDown", page);
            var downImage = down.gameObject.AddComponent<Image>();
            downImage.sprite = MenuSprites.FadeDown;
            downImage.type = Image.Type.Sliced;
            downImage.color = StudioCenter;
            downImage.raycastTarget = false;
            UiKit.Place(down, 0f, 0f, 1f, 0.30f);
            down.localRotation = Quaternion.Euler(0f, 0f, 180f);

            // Kelime işareti. Referansta X 0.235-0.762, Y(alttan) 0.453-0.549.
            var mark = UiKit.CreateLabel("Wordmark", page, "grand", 150, Ink);
            mark.fontStyle = FontStyles.Normal;
            UiKit.Place(mark, 0.235f, 0.453f, 0.762f, 0.549f);

            return group;
        }

        // --------------------------------------------------------- yükleme

        CanvasGroup BuildLoading(Transform canvas)
        {
            var page = UiKit.CreateRect("Loading", canvas);
            UiKit.Place(page, 0f, 0f, 1f, 1f);
            var group = page.gameObject.AddComponent<CanvasGroup>();

            // Zemin sanatı EKRANI KAPLAR — sığmaz, KAPLAR.
            //
            // DERS (`preserveAspect` SIĞDIRIR, DOLDURMAZ): Burası
            // `CreateIcon` + `preserveAspect = true` kullanıyordu. O bayrak
            // görseli kutunun İÇİNE sığdırır (letterbox); görselin oranı
            // ekranınkinden farklı olduğu anda üstte ve altta şerit kalır.
            // Kod bunu biliyordu — arkasına gökyüzü renginde bir panel koyup
            // "şerit görünmesin" diye yorum düşülmüştü. Ama düz mavi bir şerit
            // de şerittir: kullanıcı "splash screen tamamen ekranı kaplamıyor,
            // altında ve üstünde boşluklar var" derken tam olarak onu gördü.
            //
            // Doğru araç `UiKit.CreateCover`: `AspectRatioFitter.EnvelopeParent`
            // ile görseli oranını koruyarak ebeveyni ÖRTECEK kadar büyütür,
            // fazlası ekran dışında kalır (fotoğraftaki "cover" davranışı).
            // Karakterler yine ezilmiyor, ama boşluk da kalmıyor.
            //
            // Gökyüzü paneli YİNE duruyor: görsel hiç yüklenemezse ekran
            // simsiyah kalmasın diye. Artık bir şeridi örtmek için değil,
            // yedek olarak.
            var sky = UiKit.CreatePanel("Sky", page, new Color(0.129f, 0.361f, 0.741f));
            sky.raycastTarget = false;

            var art = UiSkin.Get(Art.Splash);
            if (art != null)
            {
                var cover = UiKit.CreateCover("Art", page, art,
                    new Color(0.129f, 0.361f, 0.741f));
                cover.raycastTarget = false;
            }

            // Logo: gerçek blok harfli görselimiz yok, başlık malzemesiyle
            // yazılıyor. Görsel gelirse burası tek bir Image'a iner.
            var logo = UiKit.CreateTitle("Logo", page, "BLOCK OUT!", 150,
                new Color(1f, 0.847f, 0.180f), new Color(0.153f, 0.075f, 0f));
            UiKit.Place(logo, 0.08f, 0.775f, 0.92f, 0.885f);

            var status = UiKit.CreateTitle("Status", page, "Loading…", 54,
                Ink, new Color(0.10f, 0.18f, 0.36f));
            UiKit.Place(status, 0.10f, 0.100f, 0.90f, 0.155f);

            group.alpha = 1f;
            return group;
        }

        // ---------------------------------------------------------- akış

        void Start() => GameKit.FX.Juice.Run(Sequence());

        System.Collections.IEnumerator Sequence()
        {
            yield return new WaitForSecondsRealtime(StudioSeconds);

            // Stüdyo ekranı SÖNER, altındaki yükleme ekranı zaten hazır bekliyor.
            float t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                _studio.alpha = 1f - Mathf.Clamp01(t / FadeSeconds);
                yield return null;
            }
            _studio.gameObject.SetActive(false);

            yield return new WaitForSecondsRealtime(LoadingSeconds);

            Finished?.Invoke();
        }
    }
}
