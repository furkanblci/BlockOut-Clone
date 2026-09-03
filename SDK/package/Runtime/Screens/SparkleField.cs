using UnityEngine;
using UnityEngine.UI;

namespace GameKit.Screens
{
    /// <summary>
    /// Ödül yığınının çevresinde parlayıp sönen küçük yıldızlar.
    ///
    /// SAYILDI (`…Levels 1-20 Walkthrough.mp4` 05:19, PERFECT kartı,
    /// 59,47 fps): yığının çevresinde aynı anda ORTALAMA 31 minik beyaz
    /// yıldız var, ama sayı kareden kareye 4 ile 32 arasında geziniyor.
    /// Yani sabit bir süs değil — sürekli SÖNÜP YANIYORLAR. Alan medyanı
    /// 7-10 piksel², yani kenarı ~3 piksel; 592 genişlikte ekranın %0,5'i.
    ///
    /// Bizde dört tane BÜYÜK ve HAREKETSİZ yıldız vardı. Sayı da boyut da
    /// yanlıştı, ama asıl fark hareketti: sabit dört yıldız "resmin bir
    /// parçası" gibi okunuyor, sönüp yanan otuz tanesi "ödül parlıyor"
    /// diyor. Aynı varlık, iki farklı cümle.
    ///
    /// DERS (rastgelelik gerekmiyor): Her yıldızın kendi evresi ve kendi
    /// hızı var; ikisi de kurulumda bir kez, konumundan TÜRETİLİYOR. Yani
    /// kart her açılışta aynı görünüyor (tasarım kararlı kalıyor) ama göz
    /// deseni yakalayamıyor, çünkü hızlar birbirinin katı değil.
    /// </summary>
    public sealed class SparkleField : MonoBehaviour
    {
        struct Star
        {
            public Graphic Graphic;
            public RectTransform Rect;
            public float Phase;
            public float Speed;
            public float Scale;
        }

        Star[] _stars;

        /// <summary>Bir yıldızın tam bir yanıp sönme çevrimi (saniye).</summary>
        const float BaseCycle = 1.35f;

        public void Adopt(Graphic[] graphics, float[] scales)
        {
            _stars = new Star[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
            {
                // Evre ve hız altın orandan üretiliyor: ardışık yıldızların
                // evreleri hiçbir zaman aynı yere düşmüyor, yani hepsi bir
                // arada yanıp sönmüyor.
                float golden = i * 0.6180339f;
                _stars[i] = new Star
                {
                    Graphic = graphics[i],
                    Rect = graphics[i].rectTransform,
                    Phase = golden - Mathf.Floor(golden),
                    Speed = 1f / (BaseCycle * (0.72f + 0.56f * ((i * 7 % 5) / 4f))),
                    Scale = scales[i]
                };
            }

            // İLK KARE HEMEN YAZILIYOR.
            //
            // `OnEnable` bileşen eklenir eklenmez çalışıyor — yani `Adopt`
            // çağrılmadan ÖNCE; o an yıldız listesi boş olduğu için hiçbiri
            // yazılmıyordu. Oyunda `Update` bir sonraki karede düzeltiyor ama
            // düzenleme modundaki yakalamalarda `Update` hiç çalışmıyor ve
            // kart yıldızsız görünüyordu — "yıldızları unutmuşum" diye
            // yarım saat aradım.
            Tick(0f);
        }

        void OnEnable() => Tick(0f);

        void Update() => Tick(Time.unscaledTime);

        void Tick(float time)
        {
            if (_stars == null) return;
            for (int i = 0; i < _stars.Length; i++)
            {
                var star = _stars[i];
                if (star.Graphic == null) continue;

                float k = star.Phase + time * star.Speed;
                k -= Mathf.Floor(k);

                // Yanma kısa, sönük duruş uzun: eğri çevrimin yalnız ilk
                // %55'inde tepe yapıyor, gerisinde sıfırda kalıyor. Referansta
                // da yıldızların çoğu her an SÖNÜK — hepsi hep yanıyor
                // olsaydı ekran beyaz bir bulut olurdu.
                float glow = k < 0.55f ? Mathf.Sin(k / 0.55f * Mathf.PI) : 0f;

                var color = star.Graphic.color;
                color.a = glow;
                star.Graphic.color = color;

                if (star.Rect != null)
                    star.Rect.localScale = Vector3.one * (star.Scale * (0.35f + 0.65f * glow));
            }
        }
    }
}
