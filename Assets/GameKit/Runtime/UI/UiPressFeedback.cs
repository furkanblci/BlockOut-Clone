using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameKit.UI
{
    /// <summary>
    /// Düğmeye BASILDIĞINI hissettiren geri bildirim: hafifçe küçülür ve
    /// koyulaşır; bırakılınca eski hâline döner.
    ///
    /// NEDEN VAR (12. tur, H7): Kullanıcı — *"butona tıkladığımızda klasik
    /// oyunlarda bulunan butonun arkaplanında bir koyuluk olmalı, butonun
    /// basıldığını hissettirmek için olan visual bizde yok; tıklıyoruz
    /// basılı tutuyoruz bir şey değişmiyor."*
    ///
    /// Sebep tek satırdı ve her düğmede tekrarlıyordu:
    ///     button.transition = Selectable.Transition.None;
    /// uGUI'nin kendi `ColorTint` geçişi bu projede bilerek kapatılmış —
    /// çünkü düğmeler tek bir `Image` değil, üst üste birkaç katman (gölge,
    /// koyu kenar, yüz, yazı) ve `ColorTint` yalnız HEDEF grafiği boyar,
    /// yani düğmenin bir katmanı kararır diğerleri kalır.
    ///
    /// Bu bileşen katmanların HEPSİNİ birden çarpıyor, o yüzden düğmenin
    /// şekli ne olursa olsun doğru çalışıyor — yuvarlak, hap, kare fark
    /// etmiyor ve ayrıca bir kaplama görseli gerekmiyor.
    ///
    /// DERS (kapatılan bir özellik, YERİNE KOYULMADIYSA eksiktir):
    /// `Transition.None` doğru bir karardı ama yarım kaldı; uGUI'nin
    /// yapamadığı şey kapatıldı, yerine bir şey konmadı ve düğmeler bir yıl
    /// boyunca tepkisiz kaldı.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiPressFeedback : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>Basılıyken ölçek. Referanstaki his: belirgin ama zıplatmayan.</summary>
        public float PressScale = 0.955f;

        /// <summary>
        /// Basılıyken renk çarpanı.
        ///
        /// 0,84 -> 0,92 (13. tur). Kullanıcı, sonuç düğmesinin arkasına
        /// denediğim koyu kenar için *"basılı tuttuğunda çıkan siyahlık öyle
        /// olacak ama ONUN İÇİN BİLE FAZLA"* dedi. O kenar kaldırıldı; aynı
        /// yargı basış geri bildiriminin kendisi için de geçerliydi.
        ///
        /// %16 koyulaşma parlak yeşil/kırmızı düğmelerde renk değişimi gibi
        /// okunuyor, "basıldı" gibi değil. %8 dokunuşu hissettirmeye yetiyor
        /// ve düğmenin kimliğini bozmuyor.
        ///
        /// DERS (geri bildirim FARK EDİLECEK kadar, DİKKAT ÇEKMEYECEK kadar):
        /// Basış efektinin işi düğmeyi değiştirmek değil, dokunuşun
        /// ulaştığını söylemek.
        /// </summary>
        public float PressDim = 0.92f;

        /// <summary>
        /// Geri bildirimin UYGULANDIĞI kök. Boşsa bileşenin kendi nesnesi.
        ///
        /// NEDEN VAR (13. tur, P5): Aç/kapa anahtarında dokunmayı yakalayan
        /// yüzey ile GÖRÜNEN parça aynı nesne değil — tıklama alanı yuvanın
        /// sabit yarısı, görünen gösterge onun üstünde gezinen çip. Geri
        /// bildirim yakalayan yüzeye uygulanınca çipin ALTINDA kalan bir
        /// kapsül küçülüyordu, yani ekranda hiçbir şey olmuyordu.
        ///
        /// DERS (geri bildirim, DOKUNULANA değil GÖRÜLENE uygulanır):
        /// İkisi çoğu düğmede aynı nesne olduğu için bu ayrım fark
        /// edilmiyor; ayrıştığı ilk kontrolde geri bildirim sessizce
        /// kayboluyor.
        /// </summary>
        public Transform Target;

        Transform Body => Target != null ? Target : transform;

        Graphic[] _graphics;
        Color[] _base;
        Vector3 _baseScale = Vector3.one;
        bool _held;

        void Awake() => _baseScale = Body.localScale;

        void OnDisable() => Release();

        void Cache()
        {
            if (_graphics != null) return;
            _graphics = Body.GetComponentsInChildren<Graphic>(true);
            _base = new Color[_graphics.Length];
            for (int i = 0; i < _graphics.Length; i++)
                _base[i] = _graphics[i] != null ? _graphics[i].color : Color.white;
        }

        public void OnPointerDown(PointerEventData e) => Press();
        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Press()
        {
            if (_held) return;
            var selectable = GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable()) return;

            Cache();
            _held = true;
            // Ölçek HER BASIŞTA taze okunuyor: düğmelerin bir kısmı giriş
            // animasyonuyla (PopIn) geliyor ve `Awake` anındaki ölçek 0
            // olabiliyor. O anki değeri taban almak, animasyon bitmeden
            // basıldığında düğmenin kaybolmasını önlüyor.
            _baseScale = Body.localScale;
            Body.localScale = _baseScale * PressScale;

            for (int i = 0; i < _graphics.Length; i++)
            {
                if (_graphics[i] == null) continue;
                var c = _base[i];
                _graphics[i].color = new Color(c.r * PressDim, c.g * PressDim, c.b * PressDim, c.a);
            }
        }

        void Release()
        {
            if (!_held) return;
            _held = false;
            Body.localScale = _baseScale;
            if (_graphics == null) return;
            for (int i = 0; i < _graphics.Length; i++)
                if (_graphics[i] != null) _graphics[i].color = _base[i];
        }

        /// <summary>Düğmeye geri bildirimi ekler (varsa dokunmaz).</summary>
        public static UiPressFeedback Attach(Selectable target)
        {
            if (target == null) return null;
            var go = target.gameObject;
            var f = go.GetComponent<UiPressFeedback>();
            if (f == null) f = go.AddComponent<UiPressFeedback>();
            return f;
        }
    }
}
