using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using UnityEngine;

namespace BlockOut.Runtime.View
{
    /// <summary>
    /// M2 geçici görsel gereçleri: çalışma anında üretilen paylaşımlı
    /// materyaller (buz, perde, ghost) ve sayaç yazıları. Hepsi placeholder —
    /// M4'te asset tabanlı gerçek görsellerle değiştirilecek.
    ///
    /// Statik önbellek bilinçli: materyaller sahne yeniden kurulunca da aynı
    /// kalır (paylaşım = SRP Batcher dostu), domain reload'da sıfırlanır.
    /// </summary>
    public static class ViewKit
    {
        static Material _ice;
        static Material _curtainPanel;
        static Material _curtainFrame;
        static Font _counterFont;
        static Material _counterMaterial;
        static readonly Dictionary<BlockColor, Material> _ghosts =
            new Dictionary<BlockColor, Material>();

        /// <summary>
        /// Buz bloğu — OPAK.
        ///
        /// DERS (referansı doğru okumak, yarı saydamlıkla boğuşmaktan iyidir):
        /// Buz kabuğu önce yarı saydam yapılmıştı; blok buzun içinden görünsün
        /// isteniyordu. Ama iki sorun çıktı: (1) saydam nesneler derinlik yazmaz
        /// ve sıralamaya bağımlıdır, kamera bu oyunda çok geride durduğu için
        /// buz sürekli bloğun arkasına düşüyordu; (2) referans oyunda zaten
        /// ALTTAKİ RENK GÖRÜNMÜYOR — donmuş blok düz bir buz kalıbı, renk ancak
        /// buz kırılınca ortaya çıkıyor (video kuralı: "buz rengi gizler").
        ///
        /// Opak yapmak hem referansa uyuyor hem de bütün sıralama problemini
        /// ortadan kaldırıyor. Bazen doğru çözüm, yanlış soruyu sormayı bırakmak.
        /// </summary>
        public static Material Ice
        {
            get
            {
                if (_ice == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit");
                    _ice = new Material(shader) { name = "Ice_TEMP" };

                    var color = new Color(0.60f, 0.85f, 0.99f);
                    if (_ice.HasProperty("_BaseColor")) _ice.SetColor("_BaseColor", color);
                    _ice.color = color;
                    // Buz parlak ve pürüzsüz: ışığı toplayınca "cam" hissi veriyor.
                    if (_ice.HasProperty("_Smoothness")) _ice.SetFloat("_Smoothness", 0.75f);
                    if (_ice.HasProperty("_Metallic")) _ice.SetFloat("_Metallic", 0f);
                }
                return _ice;
            }
        }

        /// <summary>
        /// URP için doğru kurulmuş yarı saydam materyal.
        ///
        /// DERS (doğru hat, doğru shader): Buz eskiden `Sprites/Default` ile
        /// çiziliyordu — o YERLEŞİK (built-in) render hattının shader'ı. URP'de
        /// bir şeyler çiziyor ama saydamlık/derinlik davranışı garanti değil.
        /// URP'de saydamlık, shader'ın kendisiyle değil ANAHTAR KELİMELERLE
        /// açılır: `_Surface=1`, `_SURFACE_TYPE_TRANSPARENT` ve harmanlama
        /// modu elle kurulmalı; yalnız `color.a` düşürmek yetmez, materyal
        /// yine opak hattında çizilir.
        /// </summary>
        static Material CreateTransparent(string name, Color color, int queue)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default"); // en kötü ihtimalde

            var mat = new Material(shader) { name = name + "_TEMP" };

            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);                 // 0 opak, 1 saydam
                mat.SetFloat("_Blend", 0f);                   // alfa harmanlama
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0f);                  // saydam derinlik yazmaz
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.SetShaderPassEnabled("ShadowCaster", false);
                mat.SetShaderPassEnabled("DepthOnly", false);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            mat.renderQueue = queue;
            return mat;
        }

        static Material _floor;
        static Texture2D _floorTexture;

        /// <summary>
        /// Zemin: TEK quad + döşenen prosedürel doku (hücre + ızgara çizgisi +
        /// kesişim noktası).
        ///
        /// DERS (çizim çağrısı = maliyet): Önceden her hücre için ayrı quad
        /// üretiyorduk — 6×8 tahtada 48 nesne, 48 çizim çağrısı ve komşu
        /// quad'ların kenarlarında z-fighting (titreyen çizgiler). Tek quad
        /// üstüne döşenen doku hem 1 çizim çağrısı hem de kusursuz kenar.
        /// </summary>
        public static Material FloorMaterial(BlockOut.Runtime.Config.BlockVisualConfigSO cfg)
        {
            if (_floor != null) return _floor;

            _floor = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = "Floor",
                mainTexture = BuildFloorTexture(cfg)
            };
            _floor.SetColor("_BaseColor", Color.white);
            _floor.mainTexture.wrapMode = TextureWrapMode.Repeat;
            return _floor;
        }

        static Texture2D BuildFloorTexture(BlockOut.Runtime.Config.BlockVisualConfigSO cfg)
        {
            const int size = 128;
            Color cell = cfg != null ? cfg.floorColorA : new Color(0.17f, 0.15f, 0.31f);
            // Çizgi/nokta renkleri hücre renginden TÜRETİLİR: zemin rengini
            // değiştirdiğinde kontrast kendiliğinden korunur.
            float lineDarken = cfg != null ? cfg.floorLineDarken : 0.6f;
            float dotDarken = cfg != null ? cfg.floorDotDarken : 0.4f;
            Color line = new Color(cell.r * lineDarken, cell.g * lineDarken, cell.b * lineDarken, 1f);
            Color dot = new Color(cell.r * dotDarken, cell.g * dotDarken, cell.b * dotDarken, 1f);
            float lineWidth = cfg != null ? cfg.floorLineWidth : 0.045f;
            float dotSize = cfg != null ? cfg.floorDotSize : 0.09f;

            _floorTexture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;

                    // Kenara olan mesafe: hücre sınırında çizgi.
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));

                    // Hücrenin ORTASI hafif aydınlık: koyu zeminlerde bile
                    // hücreler tek tek okunur (yalnızca çizgiye güvenmek,
                    // karanlık renklerde yetersiz kalıyordu).
                    float centerLift = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge / 0.35f));
                    Color cellShade = Color.Lerp(
                        new Color(cell.r * 0.86f, cell.g * 0.86f, cell.b * 0.86f, 1f),
                        new Color(cell.r * 1.10f, cell.g * 1.10f, cell.b * 1.10f, 1f),
                        centerLift);

                    Color color = edge < lineWidth ? line : cellShade;

                    // Köşelerdeki nokta (döşenince kesişimlerde birleşir).
                    float dx = Mathf.Min(u, 1f - u);
                    float dy = Mathf.Min(v, 1f - v);
                    if (Mathf.Sqrt(dx * dx + dy * dy) < dotSize) color = dot;

                    pixels[y * size + x] = color;
                }
            }
            _floorTexture.SetPixels32(pixels);
            _floorTexture.Apply(true, false);
            return _floorTexture;
        }

        static Material _arrowGhost;

        /// <summary>Rengi tükenmiş kapının soluk oku (kapı gizlenmez, solar).</summary>
        public static Material ArrowGhostMaterial
        {
            get
            {
                if (_arrowGhost == null)
                {
                    var shader = Shader.Find("BlockOut/Brick")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit");
                    _arrowGhost = new Material(shader) { name = "GateArrowGhost" };
                    _arrowGhost.SetColor("_BaseColor", new Color(0.42f, 0.40f, 0.50f));
                }
                return _arrowGhost;
            }
        }

        static Material _shadow;
        static Texture2D _shadowTexture;

        /// <summary>
        /// Blokların altındaki yumuşak temas gölgesi.
        ///
        /// DERS (derinlik ipuçları): Tepeden bakan bir kamerada tüm üst yüzler
        /// ışığa aynı açıyla durur, bu yüzden sahne DÜZ görünür. Gerçek gölge
        /// hesaplamak mobilde pahalıdır; blokların altına yumuşak bir leke
        /// koymak ise neredeyse bedavadır ve "nesne zeminin ÜSTÜNDE duruyor"
        /// bilgisini tek başına verir. Oyun grafiklerinde buna blob shadow denir.
        /// </summary>
        public static Material ShadowMaterial
        {
            get
            {
                if (_shadow == null)
                {
                    _shadow = new Material(Shader.Find("Sprites/Default")) { name = "BlobShadow" };
                    _shadow.mainTexture = ShadowTexture;
                }
                return _shadow;
            }
        }

        static Texture2D ShadowTexture
        {
            get
            {
                if (_shadowTexture != null) return _shadowTexture;

                // Kenarları yumuşak, köşeleri yuvarlatılmış dikdörtgen leke.
                const int size = 64;
                _shadowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp
                };

                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Merkezden kenara doğru yumuşak düşüş (superellipse).
                        float nx = (x + 0.5f) / size * 2f - 1f;
                        float ny = (y + 0.5f) / size * 2f - 1f;
                        float d = Mathf.Pow(Mathf.Abs(nx), 4f) + Mathf.Pow(Mathf.Abs(ny), 4f);
                        float alpha = Mathf.Clamp01(1f - Mathf.Pow(d, 0.75f));
                        alpha = alpha * alpha;
                        pixels[y * size + x] = new Color32(0, 0, 0, (byte)(alpha * 255));
                    }
                }
                _shadowTexture.SetPixels32(pixels);
                _shadowTexture.Apply(false, true);
                return _shadowTexture;
            }
        }

        /// <summary>Ayar değişince üretilen materyaller yeniden kurulsun.</summary>
        public static void ClearCache()
        {
            _ice = null;
            _counterMaterial = null;
            _curtainPanel = null;
            _curtainFrame = null;
            _arrow = null;
            _arrowGhost = null;
            _particle = null;
            _floor = null;
            _floorTexture = null;
            _ghosts.Clear();
        }

        static Material _arrow;

        /// <summary>
        /// Kapı okları: tuğla shader'ı kullanılır ki ok da speküler parlaklık
        /// alsın ve kabartma kenarları belirginleşsin (unlit materyal düz
        /// beyaz bir leke bırakıyordu).
        /// </summary>
        public static Material ArrowMaterial
        {
            get
            {
                if (_arrow == null)
                {
                    var shader = Shader.Find("BlockOut/Brick")
                                 ?? Shader.Find("Universal Render Pipeline/Unlit");
                    _arrow = new Material(shader) { name = "GateArrow" };
                    _arrow.SetColor("_BaseColor", new Color(1f, 0.99f, 0.96f));
                }
                return _arrow;
            }
        }

        static Material _particle;

        /// <summary>
        /// Parçacık materyali: vertex rengini olduğu gibi gösteren unlit yol.
        /// ParticleSystem her parçacığın rengini vertex rengiyle taşıdığı için
        /// tek materyal tüm renkler için yeterlidir.
        /// </summary>
        public static Material ParticleMaterial
        {
            get
            {
                if (_particle == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                 ?? Shader.Find("Sprites/Default");
                    _particle = new Material(shader) { name = "Crumb_TEMP" };
                }
                return _particle;
            }
        }

        public static Material CurtainPanel
        {
            get
            {
                if (_curtainPanel == null)
                    _curtainPanel = MakeLit("CurtainPanel_TEMP", new Color(0.22f, 0.16f, 0.38f));
                return _curtainPanel;
            }
        }

        public static Material CurtainFrame
        {
            get
            {
                if (_curtainFrame == null)
                    _curtainFrame = MakeLit("CurtainFrame_TEMP", new Color(0.85f, 0.65f, 0.2f));
                return _curtainFrame;
            }
        }

        /// <summary>
        /// Rengi tükenen kapının kapalı hali.
        ///
        /// Önceden renk açık griye doğru karıştırılıyordu ve pastel/solgun
        /// görünüyordu — "bozuk" hissi veriyordu. Referansta kapı SÖNÜK
        /// görünür: aynı renk ama koyu ve doygunluğu düşük. Karartmak
        /// "devre dışı" mesajını çok daha net veriyor.
        /// </summary>
        public static Material GhostFor(ColorPaletteSO palette, BlockColor color)
        {
            if (_ghosts.TryGetValue(color, out var mat)) return mat;

            var entry = palette.Get(color);
            Color baseColor = entry != null ? entry.uiColor : Color.gray;

            float grey = baseColor.r * 0.299f + baseColor.g * 0.587f + baseColor.b * 0.114f;
            Color desaturated = Color.Lerp(new Color(grey, grey, grey), baseColor, 0.45f);
            Color dimmed = desaturated * 0.38f;
            dimmed.a = 1f;

            mat = MakeLit($"Ghost_{color}", dimmed);
            _ghosts[color] = mat;
            return mat;
        }

        static Font CounterFont
        {
            get
            {
                if (_counterFont == null)
                    _counterFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _counterFont;
            }
        }

        /// <summary>
        /// Zemine yatık, yukarı bakan sayaç yazısı (buz/perde sayaçları).
        /// TextMesh geçici çözüm — TMP entegrasyonu M4'te. Ana nesnenin ÇOCUĞU
        /// yapılmaz: blokların eşit olmayan ölçeği yazıyı da eziyordu; bunun
        /// yerine dünya konumuna bağımsız yerleştirilir.
        /// </summary>
        public static TextMesh CreateCounter(Transform parent, Vector3 worldPos, int value)
        {
            var go = new GameObject("Counter");
            go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.SetPositionAndRotation(worldPos, Quaternion.Euler(90f, 0f, 0f));
            go.transform.localScale = Vector3.one;

            var text = go.AddComponent<TextMesh>();
            text.font = CounterFont;
            // Yüksek fontSize + küçük characterSize = keskin kenar. Etkin boyut
            // ikisinin çarpımıdır; hücre genişliği 1 birim olduğu için ~0.05
            // referanstaki iriliği veriyor.
            text.fontSize = 96;
            text.characterSize = 0.05f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.text = value.ToString();

            // DERS (yazı da derinlik testine girer): Sayaç buz kalıbının ÜSTÜNDE
            // duruyor ama font materyali varsayılan sırada çizilince blok/buz
            // yüzeyiyle çakışıp soluk kalıyordu. Materyali kopyalayıp sırasını
            // yukarı çekmek sayacı her zaman en üstte tutar — yazı bir arayüz
            // öğesi gibi davranmalı, sahnenin bir parçası gibi değil.
            var renderer = go.GetComponent<MeshRenderer>();
            if (_counterMaterial == null && CounterFont.material != null)
            {
                _counterMaterial = new Material(CounterFont.material) { name = "Counter_TEMP" };
                _counterMaterial.renderQueue = 4000;
            }
            renderer.sharedMaterial = _counterMaterial != null ? _counterMaterial : CounterFont.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Koyu lacivert zeminde krem: referanstaki sayaçlar da açık renk.
            text.color = new Color(0.13f, 0.20f, 0.38f);
            return text;
        }

        static Material MakeLit(string name, Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            mat.SetColor("_BaseColor", color);
            return mat;
        }
    }
}
