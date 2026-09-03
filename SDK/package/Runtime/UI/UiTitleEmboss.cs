using TMPro;
using UnityEngine;

namespace GameKit.UI
{
    /// <summary>
    /// Başlığa referanstaki KABARTMA görünümünü verir: harflerin dibinde
    /// ince koyu bir kenar, onun dışında KALIN ve PARLAK bir mor hale.
    ///
    /// NEDEN VAR (12. tur, G1 + G4): Kullanıcı bunu üç kez istedi —
    /// *"başlığın outlinesi aynı renk morda ve kalın, orjinal oyunda böyle;
    /// bunun için shader mı yazarsın üst üste text mi koyarsın hangi
    /// yöntemle yaparsın bilmiyorum ama bu olsun"* ve *"o kabartmalı text
    /// görünümü nerede varsa kullanalım"*.
    ///
    /// ÖLÇÜM (`_Reference/frames/m_051.jpg`, "Profil" başlığı):
    ///     bandın rengi        (66, 39, 196)
    ///     halenin en parlağı  (98, 71, 228)   -> BANTTAN AÇIK
    ///     bizim konturumuz    (50, 33, 146)   -> BANTTAN KOYU
    /// Yani yön yanlıştı: referans harfin çevresine IŞIK koyuyor, biz gölge
    /// koyuyorduk. Bu yüzden bizimki "ince karanlık bir çizgi", referanstaki
    /// "kalın parlak bir hale" gibi okunuyordu.
    ///
    /// NEDEN İKİ METİN: TMP'de bir materyalin TEK konturu var. İç koyu kenar
    /// ile dış parlak haleyi aynı anda vermek için arkaya ikinci bir kopya
    /// konuyor: kopya şişirilip (FaceDilate) kalın konturla çiziliyor, yani
    /// harflerin ŞİŞMAN bir siluetine dönüşüyor; öndeki asıl yazı kendi
    /// rengini ve ince koyu kenarını koruyor.
    ///
    /// Kopya KARDEŞ, çocuk değil: uGUI çocukları ebeveynden SONRA çizer,
    /// yani çocuk olsaydı halenin harfleri örtmesi gerekirdi. Kardeş olup
    /// bir alt sıraya konunca arkada kalıyor.
    /// </summary>
    /// <remarks>
    /// <c>ExecuteAlways</c> bilerek: doğrulama yakalamaları düzenleyici
    /// kipinde alınıyor (<see cref="UiTextFit"/> ile aynı gerekçe). Bileşen
    /// yalnız oynatma kipinde çalışsaydı, yakalamalarda hale hiç çizilmez ve
    /// "düzeldi" diye ölçtüğümüz kare düzeltilmemiş hâli gösterirdi.
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [DisallowMultipleComponent]
    public sealed class UiTitleEmboss : MonoBehaviour
    {
        /// <summary>Arkadaki kalın hale — banttan AÇIK olmalı.</summary>
        public Color Halo = new Color(0.384f, 0.278f, 0.894f);

        /// <summary>Halenin kontur genişliği (SDF, 0..1).</summary>
        public float HaloWidth = 0.30f;

        /// <summary>Haleyi şişiren miktar (SDF payı içinde kalan kısım).</summary>
        public float HaloDilate = 0.22f;

        /// <summary>
        /// Halenin BİRİM cinsinden kalınlığı — kaydırılmış kopyalarla verilir.
        ///
        /// NEDEN KOPYA (12. tur): TMP'de kontur ve şişirme, font atlasına
        /// basılmış DOLGU PAYIYLA sınırlı. `Baloo2 SDF` payı 9 piksel ve
        /// kontur + şişirme bu payı PAYLAŞIYOR; referansın istediği kalınlık
        /// (kapak yüksekliğinin ~%20'si) o paya sığmıyor. Değerleri artırmak
        /// bir yerden sonra hiçbir şey değiştirmiyordu — sessizce
        /// kırpılıyordu.
        ///
        /// Kaydırılmış kopyalar bu sınırı tamamen atlıyor: aynı yazı, hale
        /// renginde, çember üzerinde N yöne `HaloThickness` kadar kaydırılıp
        /// çiziliyor. Birleşimleri harfin etrafında tam olarak o kalınlıkta
        /// bir bant bırakıyor. Yazı tipini yeniden üretmek gerekmiyor.
        /// </summary>
        public float HaloThickness = 9f;

        /// <summary>Kaç yöne kopyalanacak. 8 yön, köşelerde de dolu bir bant verir.</summary>
        public int HaloSteps = 8;

        /// <summary>
        /// Halenin DIŞINDAKİ koyu halka ve aşağı düşen gölge.
        /// Referansta üstte ( 47, 23,161), altta (30,14,102) ölçüldü.
        /// </summary>
        public Color Shadow = new Color(0.137f, 0.071f, 0.420f);

        /// <summary>Koyu halkanın kontur genişliği.</summary>
        public float ShadowWidth = 0.26f;

        /// <summary>Koyu halkayı şişiren miktar — haleden BÜYÜK olmalı.</summary>
        public float ShadowDilate = 0.15f;

        /// <summary>Gölgenin aşağı kayması (birim). Referansta alt kenar üstten kalın.</summary>
        public float ShadowDrop = 4f;

        /// <summary>Harfin dibindeki ince koyu kenar. Referansta YOK: 0 bırakılır.</summary>
        public Color Inner = new Color(0.094f, 0.063f, 0.278f);

        /// <summary>
        /// İç kenarın genişliği — İNCE KALMALI.
        ///
        /// DERS (TMP konturu İÇERİ büyür): `_OutlineWidth` harfi
        /// büyütmüyor, harfin KENARINDAN İÇERİ doğru yiyor. 0,20 denendi ve
        /// harflerin yüzü tamamen koyulaştı — ekranda beyaz kalmadı.
        /// Kalınlık konturdan değil, arkadaki ŞİŞİRİLMİŞ katmanlardan
        /// gelmeli.
        /// </summary>
        public float InnerWidth = 0.06f;

        /// <summary>
        /// Öndeki harfleri şişiren miktar. Referansın yazı tipi bizimkinden
        /// KALIN; aynı puntoda bizim harfler cılız kalıyordu. Kontur
        /// kalınlaştırmak aynı şey değil — o harfi değil çevresini büyütür.
        /// </summary>
        public float InnerDilate = 0.06f;

        const string HaloSuffix = "_Halo";
        const string ShadowSuffix = "_Shadow";

        TextMeshProUGUI _front;
        TextMeshProUGUI[] _halo;        // parlak hale: kaydirilmis kopyalar
        RectTransform[] _haloRect;
        TextMeshProUGUI _shadow;        // koyu halka + asagi golge
        RectTransform _frontRect, _shadowRect;

        void OnEnable()
        {
            Cache();
            Sync();
        }

        void OnDisable()
        {
            // Hale ARTIK GEREKMİYORSA kalmasın: bileşen kapatılınca ekranda
            // sahipsiz bir siluet bırakmak, hatanın en sinsi türü.
            if (_halo != null)
                foreach (var h in _halo) if (h != null) h.gameObject.SetActive(false);
            if (_shadow != null) _shadow.gameObject.SetActive(false);
        }

        void Cache()
        {
            if (_front == null) _front = GetComponent<TextMeshProUGUI>();
            if (_frontRect == null) _frontRect = transform as RectTransform;
        }

        TextMeshProUGUI MakeLayer(string suffix)
        {
            string wanted = name + suffix;
            foreach (Transform sib in transform.parent)
            {
                if (sib.name != wanted) continue;
                var f = sib.GetComponent<TextMeshProUGUI>();
                if (f != null) return f;
            }
            var go = new GameObject(wanted, typeof(RectTransform));
            go.transform.SetParent(transform.parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.raycastTarget = false;
            return t;
        }

        void EnsureBack()
        {
            if (_front == null || transform.parent == null) return;

            int n = Mathf.Max(4, HaloSteps);
            if (_halo == null || _halo.Length != n)
            {
                _halo = new TextMeshProUGUI[n];
                _haloRect = new RectTransform[n];
            }
            for (int k = 0; k < n; k++)
            {
                if (_halo[k] == null) _halo[k] = MakeLayer(HaloSuffix + k);
                _haloRect[k] = _halo[k].rectTransform;
            }
            if (_shadow == null) _shadow = MakeLayer(ShadowSuffix);
            _shadowRect = _shadow.rectTransform;

            // KARDEŞ SIRASI = ÇİZİM SIRASI, ve sıra REFERANSTAN gelir.
            //
            // Ölçüm (m_051, "Profil", x=27 dikey kesit) harften DIŞA doğru:
            //     beyaz harf -> KOYU halka -> PARLAK hale -> bant
            // Yani koyu olan İÇERİDE, parlak olan DIŞARIDA. Bir denemede
            // tersini kurdum (koyu en dışta, en çok şişirilmiş) ve ekranda
            // harfleri yutan koyu bir blok çıktı.
            //
            // SIRALAMA IDEMPOTENT OLMALI. İlk sürüm her karede
            // `SetSiblingIndex(front.GetSiblingIndex())` çağırıyordu; bir
            // öğeyi kendi indeksine taşımak KOMŞULARI kaydırdığı için sıra
            // her karede bir adım dönüyordu ve öndeki beyaz yazı bir kare
            // sonra en alta düşüyordu — ekranda hiç beyaz kalmıyordu.
            //
            // DERS (her karede çalışan kod, IDEMPOTENT olmak zorundadır):
            // "Doğru sırayı kur" ile "sıra doğru değilse düzelt" aynı şey
            // değil. Birincisi kararlı bir durum üretmiyor.
            for (int k = 0; k < n; k++)
                if (_haloRect[k].GetSiblingIndex() >= _frontRect.GetSiblingIndex())
                    _haloRect[k].SetSiblingIndex(_frontRect.GetSiblingIndex());
            if (_shadowRect.GetSiblingIndex() >= _frontRect.GetSiblingIndex())
                _shadowRect.SetSiblingIndex(_frontRect.GetSiblingIndex());
            for (int k = 0; k < n; k++)
                if (_haloRect[k].GetSiblingIndex() > _shadowRect.GetSiblingIndex())
                    _haloRect[k].SetSiblingIndex(_shadowRect.GetSiblingIndex());
        }

        void LateUpdate() => Sync();

        public void Sync()
        {
            Cache();
            if (_front == null) return;
            EnsureBack();
            if (_halo == null || _shadow == null) return;

            int n = _halo.Length;
            for (int k = 0; k < n; k++)
            {
                float a = Mathf.PI * 2f * k / n;
                var off = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * HaloThickness;
                CopyTo(_halo[k], _haloRect[k], Halo, HaloWidth, HaloDilate, off);
            }
            CopyTo(_shadow, _shadowRect, Shadow, ShadowWidth, ShadowDilate,
                   new Vector2(0f, -ShadowDrop));

            var fm = _front.fontMaterial;
            fm.SetColor(ShaderUtilities.ID_OutlineColor, Inner);
            fm.SetFloat(ShaderUtilities.ID_OutlineWidth, InnerWidth);
            fm.SetFloat(ShaderUtilities.ID_FaceDilate, InnerDilate);
        }

        /// <summary>Bir arka katmanı öndekiyle eşitler.</summary>
        void CopyTo(TextMeshProUGUI layer, RectTransform rect, Color color,
            float outlineWidth, float dilate, Vector2 offset)
        {
            if (!layer.gameObject.activeSelf) layer.gameObject.SetActive(true);

            rect.anchorMin = _frontRect.anchorMin;
            rect.anchorMax = _frontRect.anchorMax;
            rect.pivot = _frontRect.pivot;
            // GÖLGE AŞAĞI KAYAR: referansta harfin ALT kenarındaki koyuluk
            // üst kenardakinden kalın — yani bu bir halka değil, aşağı
            // düşmüş bir gölge. Kutuyu kaydırmak, ayrı bir gölge sprite'ı
            // eklemekten ucuz ve yazı değişince kendiliğinden takip ediyor.
            rect.offsetMin = _frontRect.offsetMin + offset;
            rect.offsetMax = _frontRect.offsetMax + offset;
            rect.localScale = _frontRect.localScale;
            rect.localRotation = _frontRect.localRotation;

            if (!ReferenceEquals(layer.text, _front.text)) layer.text = _front.text;
            if (!Mathf.Approximately(layer.fontSize, _front.fontSize))
                layer.fontSize = _front.fontSize;

            layer.font = _front.font;

            // MATERYAL ÖNDEKİNDEN TÜRETİLİYOR. Yalnız `font` kopyalanınca
            // katman, font varlığının VARSAYILAN materyalini alıyor; TMP'de
            // konturun çizilebileceği en büyük genişlik atlasa basılı DOLGU
            // PAYIYLA sınırlı ve o materyalde pay yetmediği için kontur
            // sessizce kırpılıyordu — ekranda tek bir renkli piksel yoktu.
            //
            // DERS (doğru ayar, YANLIŞ materyalde işe yaramaz): Değerleri
            // sorgulayıp "hepsi doğru" görmek yetmedi; hangi materyale
            // yazıldığını sormak gerekiyordu.
            if (layer.fontSharedMaterial != _front.fontSharedMaterial)
                layer.fontSharedMaterial = _front.fontSharedMaterial;

            layer.fontStyle = _front.fontStyle;
            layer.alignment = _front.alignment;
            layer.textWrappingMode = _front.textWrappingMode;
            layer.overflowMode = _front.overflowMode;
            layer.characterSpacing = _front.characterSpacing;
            layer.wordSpacing = _front.wordSpacing;
            layer.enableVertexGradient = false;
            layer.color = color;

            var m = layer.fontMaterial;
            m.SetColor(ShaderUtilities.ID_OutlineColor, color);
            m.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            m.SetFloat(ShaderUtilities.ID_FaceDilate, dilate);
        }

        /// <summary>
        /// Etikete kabartmayı ekler (ya da varsa ayarlarını günceller).
        /// </summary>
        public static UiTitleEmboss Apply(TextMeshProUGUI label, Color halo,
            Color shadow, float haloThickness = 9f, float shadowDrop = 4f)
        {
            if (label == null) return null;
            var e = label.GetComponent<UiTitleEmboss>();
            if (e == null) e = label.gameObject.AddComponent<UiTitleEmboss>();
            e.Halo = halo;
            e.Shadow = shadow;
            e.HaloThickness = haloThickness;
            e.ShadowDrop = shadowDrop;
            e.enabled = true;
            e.Sync();
            return e;
        }
    }
}
