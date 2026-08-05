using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit.UI
{
    /// <summary>
    /// Arayüzü PREFAB'sız, kodla kuran yardımcılar — 3B tarafındaki ViewKit'in
    /// arayüz karşılığı.
    ///
    /// DERS (neden prefab değil?): Prefab ve sahne dosyaları YAML'dır; iki kişi
    /// aynı ekrana dokunduğunda git birleştirmesi neredeyse her zaman çakışır ve
    /// çözmesi acı vericidir. Ekranı kodla kurmak hem gözden geçirilebilir bir
    /// diff verir hem de "bu düğme neden burada" sorusunun cevabını yorumda
    /// tutar. Bedeli: görsel düzenleme yok. Bu proje için doğru takas — düzen
    /// zaten referans oyundan sabit.
    ///
    /// DERS (CanvasScaler): Mobil arayüzün en sık hatası piksel cinsinden düzen
    /// kurmaktır; aynı arayüz 720p telefonda dev, tablette minik görünür.
    /// ScaleWithScreenSize + referans çözünürlük, tüm ölçüleri "referans piksel"
    /// cinsine çevirir. matchWidthOrHeight=1 (yükseklik) dikey oyunlarda
    /// doğrudur: ekran ne kadar dar olursa olsun içerik dikeyde aynı kalır.
    ///
    /// NOT (font): Şimdilik yerleşik LegacyRuntime fontu. Cila turunda TMP'ye
    /// geçilecek; düzen kodu aynen kalacağı için o değişiklik yüzeyseldir.
    /// </summary>
    public static class UiKit
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        static Font _font;
        public static Font Font =>
            _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        // Referans oyunun paleti.
        public static readonly Color Background = new Color(0.13f, 0.10f, 0.28f);
        public static readonly Color Panel      = new Color(0.29f, 0.25f, 0.72f);
        public static readonly Color PanelDark  = new Color(0.20f, 0.17f, 0.52f);
        public static readonly Color Accent     = new Color(0.35f, 0.82f, 0.36f);
        public static readonly Color Coin       = new Color(1f, 0.82f, 0.28f);
        public static readonly Color Life       = new Color(0.95f, 0.35f, 0.45f);
        public static readonly Color Locked     = new Color(0.30f, 0.28f, 0.42f);
        public static readonly Color Ink        = new Color(1f, 0.98f, 0.94f);

        /// <summary>Ekranın kök canvas'ı: ölçekleyici + girdi yakalayıcı hazır.</summary>
        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            EnsureEventSystem();
            return canvas;
        }

        /// <summary>
        /// Dokunmanın işlenmesi için sahnede bir EventSystem şart; yoksa hiçbir
        /// düğme çalışmaz ve sebebi de görünmez. Bu yüzden canvas kurulurken
        /// sessizce garanti ediyoruz.
        ///
        /// DERS (iki girdi sistemi bir arada olmaz): Bu proje YENİ Input System
        /// paketini kullanıyor. uGUI'nin varsayılan bileşeni olan
        /// StandaloneInputModule ise ESKİ `UnityEngine.Input` sınıfını okur ve
        /// her karede `InvalidOperationException` atar — sonuç: hiçbir düğme
        /// tıklanmaz, üstelik hata yığını arayüzü değil girdi paketini işaret
        /// ettiği için sebebi geç anlaşılır. Doğru bileşen
        /// InputSystemUIInputModule.
        /// </summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

            var go = new GameObject("EventSystem",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            Object.DontDestroyOnLoad(go);
        }

        /// <summary>
        /// Çentik/köşe payını dışarıda bırakan güvenli alan kabı.
        /// DERS: Modern telefonlarda ekranın üst şeridi kamera çentiğinin,
        /// alt şeridi de sistem çubuğunun altında kalır. Screen.safeArea bunu
        /// piksel olarak verir; içeriği bu dikdörtgene sıkıştırmazsak coin
        /// göstergesi çentiğin altında kaybolur.
        /// </summary>
        public static RectTransform CreateSafeArea(Canvas canvas)
        {
            var rect = CreateRect("SafeArea", canvas.transform);
            var area = Screen.safeArea;
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>Düz renkli dikdörtgen — arka plan gibi köşesi önemsiz yerler için.</summary>
        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>
        /// Yuvarlak köşeli panel — referans oyunun her yüzeyi böyle.
        ///
        /// DERS (9-dilim): Sprite'ın köşe payı sabit kalır, ortası esner. Tek
        /// 64×64 doku hem küçük bir rozette hem tam ekran bir panelde bozulmadan
        /// çalışır; her boyut için ayrı görsel üretmeye gerek kalmaz.
        /// </summary>
        public static Image CreateRoundedPanel(string name, Transform parent, Color color)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UiSprites.RoundedPanel;
            image.type = Image.Type.Sliced;
            // DERS (9-dilim ölçeği ters çalışır): `pixelsPerUnitMultiplier`
            // birim başına piksel sayısını ÇARPAR — yani büyük değer köşeyi
            // KÜÇÜLTÜR. Önce 2.4 verilmişti ve 20 piksellik köşe payı ekranda
            // ~8 piksele düşüp yuvarlaklık kaybolmuştu. 0.5 tam tersini yapıp
            // payı ~40 piksele çıkarıyor.
            image.pixelsPerUnitMultiplier = 0.5f;
            image.color = color;
            return image;
        }

        /// <summary>
        /// Yazı.
        ///
        /// DERS (neden TMP?): Yerleşik `Text`, harfleri bir bitmap atlasına
        /// çizer; büyütünce bulanıklaşır ve her punto için atlas şişer. TMP ise
        /// İŞARETLİ MESAFE ALANI (SDF) kullanır: harfin kenarına olan mesafeyi
        /// saklar, bu yüzden her boyutta keskin kalır ve kontur/gölge gibi
        /// efektler bedavaya gelir. Mobilde tek atlas + keskin yazı demek.
        /// </summary>
        public static TextMeshProUGUI CreateLabel(string name, Transform parent, string text,
            int fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = align;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;   // yazı dokunmayı yutmasın
            return label;
        }

        /// <summary>Başlık yazısı: kalın, konturlu — referanstaki gibi çıkıntılı.</summary>
        public static TextMeshProUGUI CreateTitle(string name, Transform parent, string text,
            int fontSize, Color color, Color outline)
        {
            var label = CreateLabel(name, parent, text, fontSize, color);
            label.fontStyle = FontStyles.Bold;
            label.outlineWidth = 0.22f;
            label.outlineColor = outline;
            return label;
        }

        /// <summary>
        /// Etiketli düğme; tıklama davranışı çağıran tarafından bağlanır.
        ///
        /// DERS (düğme HİSSİ): Rengi biraz değiştirmek "basıldı" hissi vermez.
        /// Referans oyunda düğme basınca hafifçe KÜÇÜLÜR, bırakınca hedefi
        /// aşarak geri gelir. Bu 0.1 saniyelik hareket, arayüzü "canlı"
        /// gösteren şeyin ta kendisi — <see cref="UiButtonFeel"/>.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, string text,
            int fontSize, Color background, Color ink)
        {
            // Kap: hem gölgeyi hem yüzü taşır, basma animasyonu ikisini birden
            // ölçekler. Gölge yüzün ÇOCUĞU olsaydı onunla birlikte kayardı ve
            // kalınlık hissi kaybolurdu.
            var root = CreateRect(name, parent);

            // Alt gölge: yüzün biraz altında duran koyu kopya. Düğmeye fiziksel
            // bir kalınlık verir — referans oyunun tüm düğmeleri böyle.
            var shadow = CreateRoundedPanel("Shadow", root,
                Color.Lerp(background, Color.black, 0.42f));
            Place(shadow, 0f, 0f, 1f, 1f);
            shadow.rectTransform.offsetMin = new Vector2(0f, -10f);
            shadow.rectTransform.offsetMax = new Vector2(0f, -10f);
            shadow.raycastTarget = false;

            var face = CreateRoundedPanel("Face", root, background);
            Place(face, 0f, 0f, 1f, 1f);

            // Button KÖKE takılıyor ama hedef grafiği yüz. Böylece çağıran
            // `Place(button, ...)` dediğinde kap yerleşiyor (gölge dahil), yine
            // de renk geçişi yüze uygulanıyor. Dokunma olayları çocuktaki
            // grafikten köke KABARARAK (bubbling) ulaşır.
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.ColorTint;

            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.10f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.16f);
            colors.disabledColor = Color.Lerp(background, new Color(0.5f, 0.5f, 0.55f), 0.7f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            var label = CreateLabel("Label", face.transform, text, fontSize, ink);
            label.fontStyle = FontStyles.Bold;

            root.gameObject.AddComponent<UiButtonFeel>();
            return button;
        }

        /// <summary>Dikdörtgeni ebeveyninde ORANLA konumlandırır (0-1 aralığı).</summary>
        public static void Place(RectTransform rect,
            float minX, float minY, float maxX, float maxY, float padding = 0f)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        public static void Place(Component component,
            float minX, float minY, float maxX, float maxY, float padding = 0f) =>
            Place((RectTransform)component.transform, minX, minY, maxX, maxY, padding);
    }
}
