using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameKit.Screens
{
    /// <summary>
    /// "Özel Teklifler" bölümündeki yatay taşıyıcı: sayfalar parmakla
    /// kaydırılır, bırakılınca en yakın sayfaya oturur, alttaki noktalar hangi
    /// sayfada olunduğunu gösterir.
    ///
    /// DERS (nokta göstergesi bir SÖZDÜR): İlk hâlde ekranda üç nokta vardı ama
    /// tek bir teklif duruyordu — referansta üç nokta olduğu için konmuşlardı.
    /// Oyuncu o noktaları görüp kaydırmayı dener ve hiçbir şey olmaz; arayüz bir
    /// kez yalan söylediğinde geri kalanına da güvenilmez. Ya gösterge kalkacak
    /// ya taşıyıcı gerçek olacak.
    ///
    /// DERS (iç içe kaydırma, dikeyi YUTAR): Bu taşıyıcı, dikey kaydırılan
    /// mağaza listesinin İÇİNDE duruyor. Olay sistemi sürüklemeyi, arayan
    /// nesneden yukarı doğru ilk bulduğu işleyiciye verir ve orada bırakır —
    /// yani teklif kartına parmak koyup aşağı kaydırmak istediğinde olay içteki
    /// taşıyıcıya gider, dıştaki liste hiç haberdar olmaz ve sayfa DONAR.
    /// Bu yüzden burada jestin yönü ölçülüyor: yatay ise taşıyıcı işler, dikey
    /// ise olay elle dıştaki listeye AKTARILIYOR.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public sealed class OfferCarousel : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Yön kararı için gereken en küçük parmak yolu (birim).</summary>
        const float AxisThreshold = 12f;
        const float SnapLerp = 14f;

        /// <summary>Fiske hızının hedefe katkı katsayısı (saniye cinsinden pay).</summary>
        const float FlickWeight = 0.16f;

        ScrollRect _scroll, _outer;
        RectTransform _viewport, _content;
        RectTransform[] _pages;
        Image[] _dots;

        bool _deciding, _vertical;
        float _lastWidth = -1f;
        int _page;

        /// <summary>Sürükleme boyunca yumuşatılan parmak hızı (birim/saniye).</summary>
        float _velocity;

        static readonly Color DotOn  = new Color(1f, 0.78f, 0.16f);
        static readonly Color DotOff = new Color(0.62f, 0.35f, 0.16f);

        public void Bind(ScrollRect outer, RectTransform[] pages, Image[] dots)
        {
            _scroll = GetComponent<ScrollRect>();
            // Kaydırma matematiğini bu bileşen yapıyor; ScrollRect yalnız
            // kabuk olarak duruyor, kendi sürükleme işleyişi kapalı.
            _scroll.horizontal = false;
            _scroll.vertical = false;

            _outer = outer;
            _viewport = (RectTransform)transform;
            _content = _scroll.content;
            _pages = pages;
            _dots = dots;

            // İlk yerleşim BURADA yapılır, ilk Update'te değil.
            //
            // DERS (ilk kare çalışmayabilir): Yerleşim yalnız Update'e
            // bağlanmıştı. Sayfalar varsayılan çapalarıyla (0,0)-(1,1) kaldı,
            // taşıyıcı şeridinin genişliği de 0 olduğu için üç kart da SIFIR
            // GENİŞLİKTE çizildi — ekranda yalnız taşan yazı parçaları göründü.
            // Kurulumun doğruluğu "bir kare geçmiş olmasına" bağlıysa, o kare
            // geçmediğinde ekran bozuk kalır; kurulum kendi kendine yeterli
            // olmalı. (Odaklanmamış editörde oyun döngüsünün tıklamaması bunu
            // görünür kıldı, ama sebep test ortamı değil tasarımdı.)
            Refresh();
        }

        void OnEnable() => Refresh();

        // ---- Sürükleme -----------------------------------------------------

        public void OnBeginDrag(PointerEventData eventData)
        {
            _deciding = true;
            _vertical = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_pages == null) return;

            if (_deciding)
            {
                var moved = eventData.position - eventData.pressPosition;
                if (moved.magnitude < AxisThreshold) return;   // yön daha belli değil

                _deciding = false;
                _vertical = Mathf.Abs(moved.y) > Mathf.Abs(moved.x);

                if (_vertical && _outer != null)
                {
                    // Dıştaki liste jesti baştan almalı, yoksa parmak ilk
                    // hareketi kadar zıplar.
                    ExecuteEvents.Execute(_outer.gameObject, eventData,
                        ExecuteEvents.beginDragHandler);
                }
            }

            if (_vertical)
            {
                if (_outer != null)
                    ExecuteEvents.Execute(_outer.gameObject, eventData,
                        ExecuteEvents.dragHandler);
                return;
            }

            var position = _content.anchoredPosition;
            position.x += eventData.delta.x / Scale();
            _content.anchoredPosition = position;

            // Parmağın hızını YUMUŞATARAK biriktir (bkz. OnEndDrag'deki ders).
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            float instant = eventData.delta.x / Scale() / dt;
            _velocity = Mathf.Lerp(_velocity, instant, 0.55f);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_vertical)
            {
                if (_outer != null)
                    ExecuteEvents.Execute(_outer.gameObject, eventData,
                        ExecuteEvents.endDragHandler);
                _deciding = false;
                _vertical = false;
                return;
            }

            _deciding = false;

            // DERS (son karenin delta'sı HIZ DEĞİLDİR): Burada fiske hesabı
            // `eventData.delta.x` ile yapılıyordu — yani YALNIZ son karede
            // parmağın gittiği yol. Gerçek bir dokunuşta parmak kaldırılmadan
            // hemen önce yavaşlar, çoğu zaman son kare deltası ~0'dır. Sonuç:
            // fiske katkısı hep sıfır çıkıyor ve YARIM SAYFADAN AZ her kaydırma
            // geri dönüyordu. Telefonda başparmakla yapılan normal bir kaydırma
            // 1080 genişlikte 200-300 piksel; yarım sayfa 540. Yani kullanıcı
            // kaydırıyor, sayfa geri dönüyor ve "kaydıramıyorum, sabit kalmış"
            // diye görünüyordu (10. APK bulgusu).
            //
            // Editörde fark edilmemesinin sebebi de bu: fare ile yapılan test
            // sürüklemesi kesintisiz ve hızlıdır, bırakma anında delta hâlâ
            // büyüktür. Girdi cihazı, hatayı gizleyen şeydi.
            //
            // Artık hız OnDrag boyunca yumuşatılarak biriktiriliyor; ayrıca
            // MESAFE eşiği de var: sayfanın çeyreği kadar kaydırmak yeter.
            float width = Mathf.Max(1f, _viewport.rect.width);
            float here = -_content.anchoredPosition.x / width;

            float flick = -_velocity / width * FlickWeight;
            float target = here + flick;

            // Mesafe eşiği: en yakın sayfaya yuvarlamak yerine, çeyrek sayfayı
            // geçen bir kaydırma komşu sayfaya taşısın.
            float fromPage = target - _page;
            int step = Mathf.Abs(fromPage) >= 0.25f ? (int)Mathf.Sign(fromPage) : 0;

            _page = Mathf.Clamp(_page + step, 0, _pages.Length - 1);
            _velocity = 0f;
        }

        /// <summary>Ekran pikselinden kanvas birimine oran.</summary>
        float Scale()
        {
            var canvas = _viewport.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.scaleFactor : 1f;
        }

        // ---- Yerleşim ve oturma ---------------------------------------------

        void Update()
        {
            if (_pages == null || _pages.Length == 0) return;

            float width = _viewport.rect.width;
            if (width <= 0f) return;

            if (!Mathf.Approximately(width, _lastWidth))
            {
                _lastWidth = width;
                Layout(width);
            }

            if (_deciding || _vertical)
            {
                // Jest bize ait değil; gösterge yerinde kalsın.
            }
            else
            {
                float target = -_page * width;
                var position = _content.anchoredPosition;
                position.x = Mathf.Lerp(position.x, target, Time.unscaledDeltaTime * SnapLerp);
                if (Mathf.Abs(position.x - target) < 0.5f) position.x = target;
                _content.anchoredPosition = position;
            }

            int shown = Mathf.Clamp(Mathf.RoundToInt(-_content.anchoredPosition.x / width),
                                    0, _dots.Length - 1);
            for (int i = 0; i < _dots.Length; i++)
                if (_dots[i] != null) _dots[i].color = i == shown ? DotOn : DotOff;
        }

        /// <summary>
        /// Görünüm alanının ölçüsü değiştiğinde Unity'nin kendisi çağırır —
        /// ekran döndüğünde, güvenli alan güncellendiğinde ve DÜZENLEYİCİ
        /// KİPİNDE. Sonuncusu önemli: edit modunda <c>Update</c> hiç
        /// çalışmadığı için, yerleşimi yalnız oraya bağlarsak ekran görüntüsü
        /// alırken üç sayfa da üst üste yığılı çıkar.
        /// </summary>
        void OnRectTransformDimensionsChange() => Refresh();

        /// <summary>Yerleşimi şimdi uygular; genişlik hazır değilse sessizce çıkar.</summary>
        public void Refresh()
        {
            if (_pages == null || _viewport == null) return;
            float width = _viewport.rect.width;
            if (width <= 0f) return;

            _lastWidth = width;
            Layout(width);
        }

        /// <summary>
        /// Sayfaları görünüm genişliğine göre yan yana dizer.
        ///
        /// DERS (genişlik KURULUMDA bilinmez): Kanvas ölçekleyici yüksekliğe
        /// kilitli olduğu için genişlik cihaz oranıyla değişiyor, üstelik güvenli
        /// alan da payını alıyor. Sayfaları kurulumda sabit piksele oturtmak dar
        /// bir telefonda kartları yarım bırakırdı.
        /// </summary>
        void Layout(float width)
        {
            _content.sizeDelta = new Vector2(width * _pages.Length, 0f);

            for (int i = 0; i < _pages.Length; i++)
            {
                var page = _pages[i];
                page.anchorMin = new Vector2(0f, 0f);
                page.anchorMax = new Vector2(0f, 1f);
                page.pivot = new Vector2(0.5f, 0.5f);
                page.sizeDelta = new Vector2(width, 0f);
                page.anchoredPosition = new Vector2((i + 0.5f) * width, 0f);
            }

            _content.anchoredPosition = new Vector2(-_page * width, 0f);
        }
    }
}
