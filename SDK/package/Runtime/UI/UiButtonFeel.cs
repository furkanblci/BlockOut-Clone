using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PT = PrimeTween;

namespace GameKit.UI
{
    /// <summary>
    /// Düğmeye dokunma hissi: basınca küçülür, bırakınca hedefi hafifçe aşarak
    /// yerine oturur. Projedeki TEK basış bileşeni.
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
    ///
    /// DERS (İKİ bileşen aynı şeyi yaparsa, ikisi de yanlış olur): Bu dosyanın
    /// yanında bir de `UiPressFeedback` vardı ve o da ölçek veriyordu. İkisi
    /// aynı düğmeye takılıydı: 0,94 × 0,955 = 0,90. Yani kimsenin seçmediği bir
    /// değer ekrandaydı — iki doğru sayının çarpımı. Ölçek TEK bir yerden
    /// gelmeli; o dosya 15. turda silindi, renk koyulaştırması da onunla
    /// birlikte (ölçüm için aşağıya bak).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiButtonFeel : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>
        /// Basılıyken ölçek — REFERANSTAN ÖLÇÜLDÜ (15. tur).
        ///
        /// Kullanıcı: *"basılı tuttuğundaki görünüm istediğim gibi değil,
        /// detaylı incele, basılı tuttuğunda ne değişiyor?"*
        ///
        /// ÖLÇÜM (`Block Out Color Sort Puzzle Levels.mp4`, 592×1280, 59,47 fps):
        /// aynı düğmenin basılı ve serbest kareleri piksel piksel karşılaştırıldı.
        ///   • Ana ekranın "Level 10" düğmesi (t≈209,3 sn, 10 kare basılı):
        ///     serbest 281×110 px → basılı 273×107 px  = 0,972 / 0,973
        ///   • "PERFECT!" panelinin "Continue" düğmesi (t≈201 sn):
        ///     serbest 304×110 px → basılı 296×108 px  = 0,974 / 0,982
        ///   • Yatay merkez İKİSİNDE de aynı pikselde kaldı (295,5) — yani
        ///     düğme kaymıyor, MERKEZİNE doğru küçülüyor.
        ///   • Yüz rengi basılıyken de serbestken de (55,214,19); 3B dudak
        ///     ikisinde de (24,175,7). Yani KOYULAŞMA YOK, kalınlık da
        ///     kaybolmuyor. Değişen tek şey ölçek.
        ///
        /// Bizdeki eski değer 0,94 idi ve üstüne `UiPressFeedback`'in 0,955'i
        /// biniyordu: ekrandaki gerçek basış 0,90 — referansın ÜÇ KATI. Düğme
        /// "basılmış" değil "çekilmiş" görünüyordu.
        ///
        /// DERS (küçük fark, doğru fark): %3 az gibi durur ama ANINDA olduğu
        /// için göz onu yakalar. Büyük ölçek düşüşü dokunuşu değil, düğmenin
        /// kaçtığını anlatır.
        /// </summary>
        const float PressedScale = 0.97f;

        /// <summary>
        /// Basış süresi. ÖLÇÜM: referansta düğme TEK karede (≈17 ms) küçük
        /// hâline geçiyor — ara değer yok. 0,04 sn bunu yumuşatmadan verir;
        /// eski 0,07 sn'de parmak kalkarken animasyon hâlâ sürüyordu.
        /// </summary>
        const float PressDuration = 0.04f;

        const float ReleaseDuration = 0.30f;

        /// <summary>Bırakışta hedefi aşma şiddeti. 1.0 klasik "back" eğrisi.</summary>
        const float ReleaseOvershoot = 1.6f;

        /// <summary>
        /// Ölçeğin UYGULANDIĞI gövde. Boşsa bileşenin kendi nesnesi.
        ///
        /// NEDEN VAR (13. tur, P5 — eski `UiPressFeedback.Target`): Aç/kapa
        /// anahtarında dokunmayı yakalayan yüzey ile GÖRÜNEN parça aynı nesne
        /// değil: tıklama alanı yuvanın sabit yarısı, görünen gösterge onun
        /// üstünde gezinen çip. His yakalayan yüzeye uygulanınca çipin ALTINDA
        /// kalan bir kapsül küçülüyordu, yani ekranda hiçbir şey olmuyordu.
        ///
        /// DERS (his, DOKUNULANA değil GÖRÜLENE uygulanır): İkisi çoğu düğmede
        /// aynı nesne olduğu için bu ayrım fark edilmiyor; ayrıştığı ilk
        /// kontrolde his sessizce kayboluyor.
        /// </summary>
        public Transform Body;

        Transform Target => Body != null ? Body : transform;

        PT.Tween _tween;
        bool _pressed;

        /// <summary>
        /// Basışta KÜÇÜLMEYEN katman: düğmenin dış gölgesi.
        ///
        /// NEDEN VAR (15. tur): Kullanıcı basışı fark edemedi. Ölçek
        /// referansla birebir aynıydı (0,97) ama bizde düğmenin BÜTÜN
        /// katmanları birlikte küçülüyordu — küçülmeyi ölçebileceğin sabit
        /// bir kenar yoktu, arkadan da arka plan çıkıyordu.
        ///
        /// Referansta düğmenin altında duran gölge YERİNDE KALIYOR; düğme
        /// içine gömülünce aradaki koyu boşluk büyüyor ve asıl görülen şey o.
        /// Gölge gövdenin çocuğu olduğu için ters ölçekleniyor (1/s): dünya
        /// ölçüsü sabit kalıyor.
        ///
        /// DERS (hareketi gösteren şey, DURAN referanstır).
        /// </summary>
        Transform _shadow;
        bool _shadowSearched;

        Transform Shadow
        {
            get
            {
                if (_shadowSearched) return _shadow;
                _shadowSearched = true;
                var body = Target;
                if (body != null) _shadow = body.Find("Shadow");
                return _shadow;
            }
        }

        /// <summary>Gövdeyi ölçekler, gölgeyi yerinde tutar.</summary>
        void Apply(float scale)
        {
            var body = Target;
            if (body == null) return;
            body.localScale = new Vector3(scale, scale, 1f);

            var shadow = Shadow;
            if (shadow != null)
            {
                float inverse = scale > 0.0001f ? 1f / scale : 1f;
                shadow.localScale = new Vector3(inverse, inverse, 1f);
            }
        }

        void OnDisable()
        {
            // Devre dışı kalırken ölçeği geri ver; yoksa düğme küçük kalıp
            // bir daha düzelmez.
            if (_tween.isAlive) _tween.Stop();
            _pressed = false;
            Apply(1f);
        }

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
            // Kapalı bir düğme basılmış gibi davranmamalı (eski
            // `UiPressFeedback`'ten devralınan denetim).
            var selectable = GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable()) return;

            _pressed = true;

            if (!Muted)
            {
                if (_tween.isAlive) _tween.Stop();
                float from = Target != null ? Target.localScale.x : 1f;
                _tween = PT.Tween.Custom(this, from, PressedScale, PressDuration,
                    (feel, value) => feel.Apply(value),
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

            var body = Target;
            if (body == null) return;

            // Susturulmuşken basışta hiç küçülmedik; geri getirilecek bir şey
            // de yok. Yine de bir tween varsa (susturma basıştan SONRA açıldıysa)
            // ölçeği düzeltmek gerekir, o yüzden koşul tween'e bakıyor.
            if (Muted && !_tween.isAlive)
            {
                Apply(1f);
                return;
            }

            if (_tween.isAlive) _tween.Stop();
            // Overshoot eğrisi 1'i aşıp geri döner; ayrıca bir "aşma hedefi"
            // vermeye gerek yok, eğrinin kendisi zıplamayı üretiyor.
            _tween = PT.Tween.Custom(this, body.localScale.x, 1f, ReleaseDuration,
                (feel, value) => feel.Apply(value),
                PT.Easing.Overshoot(ReleaseOvershoot), useUnscaledTime: true);
        }

        /// <summary>
        /// Düğmeye hissi ekler (varsa dokunmaz) ve gövdeyi bağlar.
        /// Eski <c>UiPressFeedback.Attach</c> çağrılarının yerini alır.
        /// </summary>
        public static UiButtonFeel Attach(Component target, Transform body = null)
        {
            if (target == null) return null;
            var go = target.gameObject;
            var feel = go.GetComponent<UiButtonFeel>();
            if (feel == null) feel = go.AddComponent<UiButtonFeel>();
            if (body != null) feel.Body = body;
            return feel;
        }
    }
}
