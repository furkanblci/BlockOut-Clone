using UnityEngine;

namespace GameKit.UI
{
    /// <summary>
    /// Arayüz sprite'larını KOD İLE üretir — hiçbir görsel asset gerekmez.
    ///
    /// DERS (neden prosedürel sprite?): Yuvarlak köşeli panel, mobil casual
    /// arayüzün temel taşıdır ve normalde bir tasarımcının PNG'sini bekler.
    /// Küçük bir doku üretip 9-DİLİM (9-slice) kenar payı vermek, aynı sprite'ı
    /// her boyutta bozulmadan esnetmeyi sağlar: köşeler sabit kalır, kenarlar
    /// uzar. Böylece tek 64×64 doku, ekranın her yerinde kullanılabilir.
    ///
    /// DERS (yumuşak kenar = anti-aliasing): Yarıçapı keskin bir eşikle kesersek
    /// köşeler tırtıklı olur. Kenara olan mesafeyi 1-2 piksellik bir bantta
    /// yumuşatmak (smoothstep) kenarları temizler — GPU'da MSAA'ya gerek kalmaz.
    /// </summary>
    public static class UiSprites
    {
        static Sprite _roundedPanel;
        static Sprite _roundedOutline;
        static Sprite _circle;

        const int Size = 64;
        const float Radius = 18f;      // piksel; 9-dilim payı bunun biraz üstü
        const int Border = 20;

        /// <summary>Dolu, yuvarlak köşeli panel (9-dilim).</summary>
        public static Sprite RoundedPanel =>
            _roundedPanel != null ? _roundedPanel
                : (_roundedPanel = Build("UiRounded", filled: true, outlineWidth: 0f));

        /// <summary>Yalnız çerçeve — seçili/vurgulu durum için.</summary>
        public static Sprite RoundedOutline =>
            _roundedOutline != null ? _roundedOutline
                : (_roundedOutline = Build("UiRoundedOutline", filled: false, outlineWidth: 5f));

        /// <summary>Tam daire — rozet, ikon zemini, ilerleme noktası.</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle != null) return _circle;

                var tex = NewTexture("UiCircle");
                var pixels = new Color32[Size * Size];
                float half = Size * 0.5f;
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                                   new Vector2(half, half));
                        float a = 1f - Mathf.SmoothStep(half - 1.5f, half, d);
                        pixels[y * Size + x] = new Color(1f, 1f, 1f, a);
                    }
                tex.SetPixels32(pixels);
                tex.Apply(false, true);

                _circle = Sprite.Create(tex, new Rect(0, 0, Size, Size),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                return _circle;
            }
        }

        static Sprite Build(string name, bool filled, float outlineWidth)
        {
            var tex = NewTexture(name);
            var pixels = new Color32[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float d = RoundedRectDistance(x + 0.5f, y + 0.5f);

                    // d < 0 içeride, d > 0 dışarıda. Kenarda 1.5 piksellik yumuşama.
                    float alpha = 1f - Mathf.SmoothStep(-0.75f, 0.75f, d);

                    if (!filled)
                    {
                        // Çerçeve: dış kenardan `outlineWidth` kadar içerisi boş.
                        float inner = 1f - Mathf.SmoothStep(-0.75f, 0.75f, d + outlineWidth);
                        alpha = Mathf.Clamp01(alpha - inner);
                    }

                    pixels[y * Size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            // 9-dilim: köşe payı yarıçaptan biraz büyük olmalı, yoksa esnerken
            // yuvarlaklık bozulur.
            return Sprite.Create(tex, new Rect(0, 0, Size, Size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(Border, Border, Border, Border));
        }

        /// <summary>
        /// Yuvarlak dikdörtgene işaretli mesafe. Negatif = içeride.
        /// Köşelerde yarıçaplı bir daireye, kenarlarda düz çizgiye indirgenir.
        /// </summary>
        static float RoundedRectDistance(float x, float y)
        {
            float halfW = Size * 0.5f, halfH = Size * 0.5f;
            float dx = Mathf.Abs(x - halfW) - (halfW - Radius);
            float dy = Mathf.Abs(y - halfH) - (halfH - Radius);

            float outside = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            return outside + inside - Radius;
        }

        static Texture2D NewTexture(string name) => new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = name,
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        /// <summary>Görsel ayarlar değişince yeniden üretilsin.</summary>
        public static void ClearCache()
        {
            _roundedPanel = null;
            _roundedOutline = null;
            _circle = null;
        }
    }
}
