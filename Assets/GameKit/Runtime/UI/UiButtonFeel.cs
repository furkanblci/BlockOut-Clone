using UnityEngine;
using UnityEngine.EventSystems;

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
    /// bunu "gerçek" sayar. Aşma miktarı küçük olmalı: %6 yeterli, %20 oyuncak
    /// gibi görünür.
    ///
    /// Coroutine yerine Update kullanıyoruz: düğme devre dışı bırakılıp yeniden
    /// açıldığında yarım kalmış bir coroutine ölçeği bozuk bırakabilirdi.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiButtonFeel : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float PressedScale = 0.94f;
        const float Overshoot = 1.06f;
        const float PressSpeed = 22f;
        const float ReleaseSpeed = 14f;

        RectTransform _rect;
        float _current = 1f;
        float _target = 1f;
        bool _releasing;

        void Awake() => _rect = (RectTransform)transform;

        void OnDisable()
        {
            // Devre dışı kalırken ölçeği geri ver; yoksa düğme küçük kalıp
            // bir daha düzelmez.
            _current = _target = 1f;
            _releasing = false;
            if (_rect != null) _rect.localScale = Vector3.one;
        }

        void Update()
        {
            float speed = _releasing ? ReleaseSpeed : PressSpeed;
            _current = Mathf.Lerp(_current, _target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));

            if (_releasing && Mathf.Abs(_current - _target) < 0.004f)
            {
                if (!Mathf.Approximately(_target, 1f))
                {
                    _target = 1f;      // aşmadan sonra 1'e otur
                }
                else
                {
                    _current = 1f;
                    _releasing = false;
                }
            }

            _rect.localScale = new Vector3(_current, _current, 1f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _target = PressedScale;
            _releasing = false;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            if (Mathf.Approximately(_target, 1f) && !_releasing) return;
            _target = Overshoot;
            _releasing = true;
        }
    }
}
