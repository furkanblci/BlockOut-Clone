using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GameKit.Screens
{
    /// <summary>
    /// Alt sekme çubuğundaki ekranlar arasında YATAY KAYDIRMAYLA geçiş.
    ///
    /// DERS (neden `IDragHandler` DEĞİL?): İlk akla gelen çözüm tam ekran
    /// görünmez bir yüzeye sürükleme işleyicisi takmak. Bu projede çalışmaz:
    /// Mağaza, Liderlik ve Yolculuk ekranlarının her birinde dikey bir
    /// <see cref="UnityEngine.UI.ScrollRect"/> var ve ScrollRect sürükleme
    /// olaylarını YUTAR — üstünde başlayan bir jest asla arkadaki yüzeye
    /// ulaşmaz. Yani kaydırma yalnız ekranın boş kalan köşelerinde çalışırdı;
    /// oyuncunun parmağını nereye koyacağını bilmesi gereken bir jest,
    /// jest değildir.
    ///
    /// Bunun yerine işaretçi doğrudan Input System'den örnekleniyor
    /// (<see cref="PointerInputService"/> ile aynı yöntem). Olay ağacından
    /// bağımsız olduğu için ScrollRect'in yuttuğu jestleri de görüyoruz;
    /// dikey kaydırmanın bozulmaması ise EKSEN BASKINLIĞI kuralıyla
    /// sağlanıyor: yatay yol, dikey yolun en az iki katı olmalı.
    ///
    /// DERS (yatay kaydıran bir widget'ın üstünde bu jest KAPALI olmalı):
    /// Mağazadaki "Özel Teklifler" taşıyıcısı kendisi yatay kayıyor. Orada
    /// başlayan bir jest hem sayfayı hem ekranı değiştirirse oyuncu ne
    /// olduğunu anlamaz. Basma anında bir raycast yapılıp jestin bir
    /// <see cref="OfferCarousel"/> üstünde başlayıp başlamadığına bakılıyor;
    /// başladıysa o jest baştan iptal.
    /// </summary>
    [RequireComponent(typeof(MenuShell))]
    public sealed class MenuSwipeNavigator : MonoBehaviour
    {
        /// <summary>
        /// Geçiş için gereken en kısa yatay yol — EKRAN GENİŞLİĞİNİN ORANI.
        ///
        /// Piksel vermek yanlış olurdu: 720p bir telefonda rahat bir kaydırma,
        /// 1440p bir tablette parmağın zar zor kıpırdaması demek. %12, kazara
        /// dokunuşları eleyecek kadar uzun, tek başparmakla rahat yapılacak
        /// kadar kısa.
        /// </summary>
        const float MinTravelShare = 0.12f;

        /// <summary>Yatay yol, dikey yolun kaç katı olmalı (eksen baskınlığı).</summary>
        const float AxisDominance = 2f;

        /// <summary>
        /// Jestin en uzun süresi. Bundan yavaş bir hareket kaydırma değil,
        /// "parmağını dayayıp gezdirme"dir; ekranı değiştirmemeli.
        /// </summary>
        const float MaxDuration = 0.9f;

        MenuShell _shell;
        Vector2 _start;
        float _startTime;
        bool _tracking;

        static readonly List<RaycastResult> Hits = new List<RaycastResult>();

        void Awake() => _shell = GetComponent<MenuShell>();

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            Vector2 position = pointer.position.ReadValue();
            bool pressed = pointer.press.isPressed;

            if (pressed && !_tracking)
            {
                _tracking = !StartedOverHorizontalWidget(position);
                _start = position;
                _startTime = Time.unscaledTime;
                return;
            }

            if (pressed || !_tracking) return;

            // Bırakıldı.
            _tracking = false;

            if (TryResolveSwipe(position - _start, Time.unscaledTime - _startTime,
                                Screen.width, out int direction))
                _shell.StepTab(direction);
        }

        /// <summary>
        /// Jest bir sekme geçişi mi? SAF karar — hiçbir duruma dokunmaz.
        ///
        /// DERS (test edilemeyen mantık doğrulanmamış mantıktır): Bu üç kural
        /// önce `Update`'in içinde duruyordu ve doğrulamanın tek yolu gerçek
        /// bir parmak hareketi üretmekti. Editörde sahte bir işaretçi olayı
        /// kuyruğa atmak mümkün ama basma ile bırakma arasında KARE geçmesi
        /// gerekiyor; tek bir komutun içinde bu yapılamıyor ve aradan geçen
        /// gerçek saniyeler <see cref="MaxDuration"/> sınırına takılıyor.
        /// Kararı saf bir metoda çıkarmak, kuralların her birini tek tek ve
        /// anında sınanabilir yapıyor; geriye kalan tesisat (bas/bırak takibi)
        /// gözle okunacak kadar küçük.
        ///
        /// <paramref name="direction"/>: +1 sağdaki sekme, -1 soldaki.
        /// </summary>
        public static bool TryResolveSwipe(Vector2 travel, float duration,
                                           float screenWidth, out int direction)
        {
            direction = 0;
            if (duration > MaxDuration) return false;
            if (Mathf.Abs(travel.x) < screenWidth * MinTravelShare) return false;
            if (Mathf.Abs(travel.x) < Mathf.Abs(travel.y) * AxisDominance) return false;

            // Parmak SOLA giderse içerik sola kayar, yani SAĞDAKİ sekme gelir.
            // Bu, kaydırılan şeyin ekranın kendisi olduğu hissini verir; ters
            // yön "düğmeye bastım" gibi okunur ve yön öğrenilemez.
            direction = travel.x < 0f ? 1 : -1;
            return true;
        }

        /// <summary>
        /// Jest, kendisi yatay kayan bir widget'ın üstünde mi başladı?
        /// EventSystem yoksa (kurulum sırası) güvenli tarafta kalıp "hayır"
        /// diyoruz — jestin hiç çalışmaması, yanlış çalışmasından iyidir ama
        /// burada yokluk geçici bir durum, kalıcı bir kapanma değil.
        /// </summary>
        static bool StartedOverHorizontalWidget(Vector2 screenPosition)
        {
            var events = EventSystem.current;
            if (events == null) return false;

            var data = new PointerEventData(events) { position = screenPosition };
            Hits.Clear();
            events.RaycastAll(data, Hits);

            foreach (var hit in Hits)
                if (hit.gameObject != null &&
                    hit.gameObject.GetComponentInParent<OfferCarousel>() != null)
                    return true;

            return false;
        }
    }
}
