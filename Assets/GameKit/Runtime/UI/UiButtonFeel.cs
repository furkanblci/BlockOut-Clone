using UnityEngine;
using UnityEngine.EventSystems;
using PT = PrimeTween;

namespace GameKit.UI
{
    /// <summary>
    /// Düğmeye dokunma hissi: basınca küçülür, bırakınca hedefi hafifçe aşarak
    /// yerine oturur.
    ///
    /// DERS (juice nedir, neden bedava değil?): Arayüzün "canlı" hissettirmesi
    /// büyük efektlerden değil, 100 milisaniyelik küçük tepkilerden gelir.
    /// Oyuncu parmağını değdirdiğinde ekranda BİR ŞEY olmalı — yoksa dokunuşun
    /// kaydedilip kaydedilmediğinden emin olamaz. Bu belirsizlik, oyuncunun aynı
    /// düğmeye iki kez basmasının ve arayüzü "tepkisiz" bulmasının sebebidir.
    ///
    /// DERS (neden geri zıplama?): Doğrusal geri dönüş mekanik durur. Hedefi
    /// biraz aşıp geri gelmek (overshoot) fiziksel bir yay gibi okunur ve beyin
    /// bunu "gerçek" sayar. Aşma miktarı küçük olmalı: %8 yeterli, %20 oyuncak
    /// gibi görünür.
    ///
    /// DERS (neden artık Update değil): Eskiden burada her karede üstel yumuşatma
    /// yapan bir Update vardı. Ekranda otuz düğme varsa otuz Update çağrısı
    /// demekti — hiçbiri iş yapmasa bile. PrimeTween tween'i yalnızca canlıyken
    /// işler; düğme boştayken sıfır maliyet. Basma ve bırakma eğrilerini de
    /// ayrı ayrı seçebiliyoruz: basış sert ve hızlı, bırakış yaylı.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiButtonFeel : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float PressedScale = 0.94f;
        const float PressDuration = 0.07f;
        const float ReleaseDuration = 0.30f;

        /// <summary>Bırakışta hedefi aşma şiddeti. 1.0 klasik "back" eğrisi.</summary>
        const float ReleaseOvershoot = 1.6f;

        RectTransform _rect;
        PT.Tween _tween;
        bool _pressed;

        /// <summary>
        /// Açıkken HAREKET yok, ses ve titreşim var.
        ///
        /// DERS (bir hedefte olmak, oraya gidebilmek demek değildir): Alt sekme
        /// çubuğunda AÇIK olan sekmeye tekrar basmak hiçbir yere götürmez —
        /// <c>MenuShell.Show</c> zaten erken çıkıyor. Ama düğme yine de
        /// küçülüp büyüyordu, çünkü his bileşeni gezinmeden habersiz. Oyuncu
        /// aynı sekmeye üst üste basıp ekranı "oynatabiliyordu" ve bu, olmayan
        /// bir eylemin olmuş gibi görünmesi demek. Referans oyunda o sekme
        /// kımıldamıyor; dokunuşun alındığını yalnız titreşim söylüyor.
        ///
        /// Bileşeni tamamen kapatmak yanlış olurdu: <c>Clicked</c>/<c>Pressed</c>
        /// kancaları da susardı ve dokunuş HİÇ duyulmazdı.
        /// </summary>
        public bool Muted { get; set; }

        void Awake() => _rect = (RectTransform)transform;

        void OnDisable()
        {
            // Devre dışı kalırken ölçeği geri ver; yoksa düğme küçük kalıp
            // bir daha düzelmez.
            if (_tween.isAlive) _tween.Stop();
            _pressed = false;
            if (_rect != null) _rect.localScale = Vector3.one;
        }

        /// <summary>
        /// Ses, dokunma anında çalar — bırakma anında değil.
        ///
        /// DERS (geri bildirim GECİKMESİZ olmalı): Sesi tıklama tamamlanınca
        /// çalmak doğru gibi görünür ama parmak kalkana kadar geçen 100-200
        /// milisaniye "gecikmeli" hissettirir. Basma anında çalmak dokunuşu
        /// anında onaylar; eylem sonra iptal olsa bile oyuncu "duyuldum" bilir.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;

            if (!Muted)
            {
                if (_tween.isAlive) _tween.Stop();
                _tween = PT.Tween.Scale(_rect, PressedScale, PressDuration,
                    PT.Ease.OutQuad, useUnscaledTime: true);
            }

            Clicked?.Invoke();
            Pressed?.Invoke();
        }

        /// <summary>
        /// Dokunma sesini bağlamak için. GameKit ses servisini TANIMAZ —
        /// bilse arayüz kütüphanesi oyunun ses sistemine bağımlı olurdu ve
        /// başka bir projede kullanılamazdı.
        /// </summary>
        public static System.Action Clicked;

        /// <summary>
        /// Dokunma titreşimini bağlamak için — sesten AYRI bir kanca.
        ///
        /// DERS (iki dinleyici, tek atama = biri sessizce kaybolur): Ses ve
        /// titreşimi tek bir `Clicked` alanına bağlamak cazipti; ama bu bir
        /// OLAY değil, bir ALAN — ikinci atama birincisini eziyor ve hangisinin
        /// kazandığı kurulum sırasına kalıyor. Ayrı kanca, iki sistemin
        /// birbirinden habersiz yaşamasını sağlıyor.
        ///
        /// DERS (haptik BIRAKMADA değil BASMADA): Kullanıcı APK testinde şunu
        /// söyledi — "kapat düğmesine basılı tutup parmağımı dışarı kaydırınca
        /// hiçbir şey olmaması normal, ama yine de haptik çalışmalı".
        /// Doğru okuma: titreşim eylemin DEĞİL, dokunuşun onayıdır. Eylem
        /// iptal olsa bile parmak "duyuldum" bilgisini almalı. Bu yüzden
        /// kanca `OnPointerDown` içinde, tıklama tamamlanmasını beklemiyor.
        /// </summary>
        public static System.Action Pressed;

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            if (!_pressed) return;
            _pressed = false;

            // Susturulmuşken basışta hiç küçülmedik; geri getirilecek bir şey
            // de yok. Yine de bir tween varsa (susturma basıştan SONRA açıldıysa)
            // ölçeği düzeltmek gerekir, o yüzden koşul tween'e bakıyor.
            if (Muted && !_tween.isAlive)
            {
                _rect.localScale = Vector3.one;
                return;
            }

            if (_tween.isAlive) _tween.Stop();
            // Overshoot eğrisi 1'i aşıp geri döner; ayrıca bir "aşma hedefi"
            // vermeye gerek yok, eğrinin kendisi zıplamayı üretiyor.
            _tween = PT.Tween.Scale(_rect, 1f, ReleaseDuration,
                PT.Easing.Overshoot(ReleaseOvershoot), useUnscaledTime: true);
        }
    }
}
