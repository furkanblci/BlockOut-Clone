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
    /// NOT (font): Yazılar TMP'nin VARSAYILAN fontundan gelir; proje kurulumu
    /// onu Baloo 2 ExtraBold'a çevirir (FontSetupTool). Böylece burada tek bir
    /// `label.font = ...` satırı yok — font değişirse bütün ekranlar birlikte
    /// değişir.
    /// </summary>
    public static class UiKit
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        static Font _font;
        public static Font Font =>
            _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        // Başlıkların paylaştığı kontur+gölge materyali. Etikete tek tek
        // `outlineWidth` yazmak her birine ayrı materyal kopyası çıkarır;
        // paylaşılan materyal hepsini tek çizim çağrısında toplar.
        static Material _titleMaterial;
        static bool _titleMaterialSearched;
        public static Material TitleMaterial
        {
            get
            {
                if (!_titleMaterialSearched)
                {
                    _titleMaterialSearched = true;
                    _titleMaterial = Resources.Load<Material>("Fonts/Baloo2 SDF Title");
                }
                return _titleMaterial;
            }
        }

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

            // Payı bileşen uyguluyor ve ekran değiştikçe yeniden uyguluyor;
            // burada bir kez hesaplamak cihaz değişince/telefon dönünce
            // arayüzü eski çentiğe göre bırakıyordu (bkz. UiSafeArea).
            rect.gameObject.AddComponent<UiSafeArea>();
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
        /// Tam ekran arka plan görseli — ekranı KAPLAR, gerekirse taşar.
        ///
        /// DERS (kapla, esnetme): Arka planı dört köşeye yapıştırmak en kolayı
        /// ama telefon oranı görselin oranından farklı olduğu anda görüntü ezilir
        /// — 20:9 bir ekranda evler incelir. AspectRatioFitter'ın EnvelopeParent
        /// kipi görseli oranını koruyarak ebeveyni ÖRTECEK kadar büyütür; fazlası
        /// ekran dışında kalır. Fotoğraftaki "cover" davranışının aynısı.
        /// </summary>
        public static Image CreateCover(string name, Transform parent, Sprite sprite, Color fallback)
        {
            if (sprite == null) return CreatePanel(name, parent, fallback);

            var rect = CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;

            var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            return image;
        }

        /// <summary>
        /// Üretilmiş bir sprite'la 9-dilim panel. Sprite null ise prosedürel
        /// yuvarlak panele düşer — böylece görsel gelmeden de ekran kurulabilir.
        /// </summary>
        public static Image CreateSlicedPanel(string name, Transform parent, Sprite sprite,
            Color? tint = null)
        {
            if (sprite == null) return CreateRoundedPanel(name, parent, tint ?? Color.white);

            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            // Kenar payı olan sprite'lar 9-dilim, olmayanlar (ikon) düz çizilir.
            image.type = sprite.border == Vector4.zero ? Image.Type.Simple : Image.Type.Sliced;
            image.color = tint ?? Color.white;

            // 9-dilim kutu kenar paylarından alçak/dar olabilir; o zaman paylar
            // çakışıp görseli ezer. Bu bileşen payları kutuya göre küçültüyor.
            if (image.type == Image.Type.Sliced)
                image.gameObject.AddComponent<UiSliceFit>();

            return image;
        }

        /// <summary>
        /// İkon. En-boy oranı korunur — kare olmayan bir alana konsa bile ezilmez.
        /// </summary>
        public static Image CreateIcon(string name, Transform parent, Sprite sprite,
            Color? tint = null)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = tint ?? Color.white;
            return image;
        }

        /// <summary>
        /// İKON düğmesi: tıklanabilir, ama görseli en-boy oranını KORUR.
        ///
        /// DERS (düğme yüzeyi ile ikon farklı şeylerdir): Normal düğmenin yüzü
        /// 9-dilim bir panel olduğu için serbestçe gerilir; bu doğrudur.
        /// Ama yüzeyin kendisi bir İKON olduğunda (yeşil artı düğmesi gibi)
        /// aynı gerdirme onu ovale çevirir. Yuvarlak çizilmiş bir artı düğmesi
        /// ekranda yumurtaya dönüyordu. İkon düğmesi ayrı bir yardımcı olmalı.
        /// </summary>
        public static Button CreateIconButton(string name, Transform parent, Sprite sprite,
            Color? tint = null)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = tint ?? Color.white;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            rect.gameObject.AddComponent<UiButtonFeel>();
            return button;
        }

        /// <summary>
        /// Görselli düğme: 3B butonun kendi gölgesi var, kod ayrıca gölge koymaz.
        ///
        /// DERS (görsel gelince kod SADELEŞİR): Prosedürel düğme, kalınlık
        /// hissini vermek için ikinci bir koyu kopya çiziyordu. Butonun 3B alt
        /// kenarı zaten görselin içinde olduğu için o kopya artık fazlalık —
        /// hem bir Image hem bir çizim çağrısı eksiliyor. Renk geçişi de
        /// kaldırıldı: sprite boyandığında parlaklık lekesi de boyanır ve
        /// plastik görünüm bozulur; basma hissi <see cref="UiButtonFeel"/>
        /// ölçeğinden geliyor.
        /// </summary>
        public static Button CreateSpriteButton(string name, Transform parent, Sprite sprite,
            string text, int fontSize, Color ink)
        {
            var root = CreateRect(name, parent);

            var face = CreateSlicedPanel("Face", root, sprite);
            Place(face, 0f, 0f, 1f, 1f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;

            // Etiket metin BOŞ OLSA DA kurulur — aynı sebeple CreateTintedButton'da
            // da öyle: çağıranlar düğmeyi boş kurup yazısını sonra
            // GetComponentInChildren ile bulup yazıyor. Etiket hiç yaratılmazsa
            // o arama null döner ve ekran yarı kurulmuş hâlde kalır.
            var label = CreateLabel("Label", face.transform, text ?? "", fontSize, ink);
            // Yazı butonun YÜZÜNE oturmalı; alt kalınlık payı bırakılmazsa
            // aşağı kaymış görünür.
            Place(label, 0.06f, 0.18f, 0.94f, 0.94f);
            if (TitleMaterial != null) label.fontSharedMaterial = TitleMaterial;

            root.gameObject.AddComponent<UiButtonFeel>();
            return button;
        }

        /// <summary>
        /// KISA düğmeler için: verilen sprite RENGE BOYANARAK kullanılır.
        ///
        /// DERS (9-dilim payı düğmenin boyunu belirler): Buton görselleri
        /// 3:1 orana göre çizildi ve 9-dilim payları büyük (üstte 88, altta 68
        /// piksel). Yüksekliği 140 pikselin altına inen bir düğmede bu iki pay
        /// toplamı alanın tamamını yiyor; Unity payları orantılı kırpıyor ve
        /// plastik yüzey eziliyor. Mağazadaki "300 J" ve ayarlardaki "Değiştir"
        /// düğmeleri böyle bozulmuştu. Payları her kenarda eşit ve küçük olan
        /// panel görselini boyamak, aynı dilde kalıp bu sorunu ortadan kaldırır.
        ///
        /// DERS (boyama ÇARPMADIR): Taban görsel koyu olursa boyanmış renk de
        /// koyu çıkar — koyu lacivert bir paneli yeşile boyamak "yeşil" değil
        /// "koyu yeşil" verir. Bu yüzden taban KREM kart görselidir: krem ≈ beyaz
        /// olduğu için çarpım rengin kendisini bırakır, kartın alt bandı da
        /// koyulaşarak bedava bir 3B kalınlık verir.
        /// </summary>
        /// <summary>
        /// Düz, çerçeveli kutu: dolgu + ince kenarlık. 3B dudak YOK.
        ///
        /// DERS (baskılı gölgeyi BOYAYAMAZSIN): Bu projede `panel_card` uzun
        /// süre her şeyin zemini olarak kullanıldı — kart, düğme, ipucu kutusu.
        /// O sprite'ın alt kenarında BASKILI bir 3B gölge var. Krem üstünde
        /// doğru duruyor; ama boyama çarpma olduğu için turuncuya boyayınca
        /// gölge kırmızıya, kreme boyayınca magentaya kayıyor ve kutunun altında
        /// oyunun hiçbir yerinde olmayan bir renk şeridi beliriyor.
        ///
        /// Kural: bir yüzeyi BOYAYACAKSAN baskılı gölgesi olmayanı kullan.
        /// Baskılı gölgeli sprite'lar (`panel_card`, `btn_*`) yalnız KENDİ
        /// renkleriyle, boyanmadan kullanılmalı.
        /// </summary>
        public static Image CreateOutlinedBox(string name, Transform parent,
            Color fill, Color border, float borderInset = 0f)
        {
            var box = CreateSlicedPanel(name, parent, UiSprites.RoundedPanel, fill);

            var ring = CreateSlicedPanel("Border", box.transform,
                UiSprites.RoundedOutline, border);
            ring.raycastTarget = false;
            Place(ring, borderInset, borderInset, 1f - borderInset, 1f - borderInset);

            return box;
        }

        /// <summary>
        /// Bir başlığa KENDİNE ÖZEL kontur verir.
        ///
        /// DERS (paylaşılan materyal tek tek ayar KABUL ETMEZ): <see cref="CreateTitle"/>
        /// bütün başlıklara ortak bir TMP materyali veriyor — bir atlas, bir
        /// çizim çağrısı, mobilde doğru karar. Ama o materyale yazılan kontur
        /// rengi TÜM başlıkları birden değiştirir; tek bir etikete mor kontur
        /// vermek istediğinde CreateTitle'a verdiğin renk sessizce yok sayılır.
        /// `fontMaterial`'e dokunmak o etikete özel bir kopya üretir: bir çizim
        /// çağrısı daha, ama yalnız gerçekten farklı olması gereken başlıklarda.
        /// Bu yüzden ayrı bir metot — varsayılan davranış hâlâ paylaşılan
        /// materyal, ayrışmak bilinçli bir tercih.
        /// </summary>
        public static void SetOutline(TextMeshProUGUI label, Color color, float width = 0.30f)
        {
            if (label == null) return;
            var material = label.fontMaterial;
            material.SetColor(ShaderUtilities.ID_OutlineColor, color);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
        }

        public static Button CreateTintedButton(string name, Transform parent, Sprite sprite,
            Color tint, string text, int fontSize, Color ink)
        {
            var root = CreateRect(name, parent);

            var face = CreateSlicedPanel("Face", root, sprite, tint);
            Place(face, 0f, 0f, 1f, 1f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.ColorTint;

            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(Color.white, Color.black, 0.06f);
            colors.pressedColor = Color.Lerp(Color.white, Color.black, 0.18f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.65f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            // Etiket metin BOŞ OLSA DA kurulur.
            //
            // DERS (sonradan doldurulacak alan var olmalı): Boş metinde etiketi
            // atlamak "gereksiz nesne yaratma" gibi görünüyordu. Ama çağıranlar
            // düğmeyi boş kurup yazısını sonra yazıyor
            // (`GetComponentInChildren<TextMeshProUGUI>()` ile bulup). Etiket hiç
            // yaratılmayınca o arama null döndü ve sonuç ekranı her açılışında
            // NullReferenceException attı — panel yarı kurulmuş hâlde kaldı,
            // kaybedince bile yıldızlar ekranda durdu. Bir Text bileşeninin
            // maliyeti, bu sınıf hatasının maliyetinin yanında yok.
            var label = CreateLabel("Label", face.transform, text ?? "", fontSize, ink);
            Place(label, 0.05f, 0.06f, 0.95f, 0.94f);
            label.fontStyle = FontStyles.Bold;

            root.gameObject.AddComponent<UiButtonFeel>();
            return button;
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
            label.textWrappingMode = TextWrappingModes.NoWrap;
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

            if (TitleMaterial != null)
            {
                label.fontSharedMaterial = TitleMaterial;
            }
            else
            {
                // Materyal henüz üretilmemişse (ilk açılış) eski yola düş.
                label.outlineWidth = 0.22f;
                label.outlineColor = outline;
            }
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
