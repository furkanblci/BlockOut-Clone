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

            if (_tween.isAlive) _tween.Stop();
            _tween = PT.Tween.Scale(_rect, PressedScale, PressDuration,
                PT.Ease.OutQuad, useUnscaledTime: true);

            Clicked?.Invoke();
        }

        /// <summary>
        /// Dokunma sesini bağlamak için. GameKit ses servisini TANIMAZ —
        /// bilse arayüz kütüphanesi oyunun ses sistemine bağımlı olurdu ve
        /// başka bir projede kullanılamazdı.
        /// </summary>
        public static System.Action Clicked;

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            if (!_pressed) return;
            _pressed = false;

            if (_tween.isAlive) _tween.Stop();
            // Overshoot eğrisi 1'i aşıp geri döner; ayrıca bir "aşma hedefi"
            // vermeye gerek yok, eğrinin kendisi zıplamayı üretiyor.
            _tween = PT.Tween.Scale(_rect, 1f, ReleaseDuration,
                PT.Easing.Overshoot(ReleaseOvershoot), useUnscaledTime: true);
        }
    }
}
