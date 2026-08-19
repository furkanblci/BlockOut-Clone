using TMPro;
using UnityEngine;

namespace GameKit.UI
{
    /// <summary>
    /// Yazıyı KUTUSUNUN İÇİNDE tutar: sığmıyorsa puntoyu küçültür.
    ///
    /// NEDEN VAR (7. tur, M46): Kullanıcının tek cümlelik isteği — "hiçbir
    /// panelde, hiçbir ekranda, hiçbir yazı taşmamalı". Bu ekran ekran
    /// dolaşılıp elle düzeltilecek bir liste değil, YAPISAL bir eksik:
    /// <see cref="UiKit.CreateLabel"/> her etiketi
    /// <c>NoWrap + TextOverflowModes.Overflow</c> ile kuruyor, yani sığmayan
    /// yazı sessizce kutusunun dışına taşıyor. Onlarca ekranda, dile göre
    /// (İngilizce'ye geçildi), sayıya göre (100 000 jeton) ve duruma göre
    /// ("CLAIM" → "SEE YOU TOMORROW") değişen yazılar var; hepsini tek tek
    /// ölçmek bir sonraki metin değişikliğinde yeniden bozulur.
    ///
    /// DERS (taşmayı ÖNLEMEK, taşanı kırpmaktan başkadır): İlk akla gelen
    /// <c>overflowMode = Truncate</c>. Bu projede bir kez denendi ve daha
    /// kötüsü oldu: kutu 32,83 birim, satır yüksekliği 33,6 çıkınca TMP
    /// satırın TAMAMINI atladı — sekme adı hiç görünmedi (4. tur dersi).
    /// Kırpmak "biraz eksik"i "hiç yok"a çevirir. Küçültmek ise en kötü
    /// ihtimalle yazıyı bir tık ufaltır; okunur kalır.
    ///
    /// DERS (yalnız GENİŞLİK): Yükseklikten de sıkıştırmak cazip ama yanlış.
    /// Bu arayüzde bir sürü etiket bilerek dar bir şeride konuyor ve
    /// yazının satır kutusu o şeridi taşıyor — günlük ödül çipindeki miktar,
    /// jeton kapsülündeki sayı, rozetlerin içindeki "x3". Onlar dikeyde
    /// ORTALANDIĞI için taşma görünmüyor, ama yükseklikten sıkıştırılsalardı
    /// hepsi yarı yarıya küçülürdü. Kullanıcının gördüğü taşma yatay taşma:
    /// yazı panelin kenarından dışarı çıkıyor.
    ///
    /// MALİYET: Kare başına iki karşılaştırma (metin referansı + kutu
    /// genişliği). Ölçüm ancak ikisinden biri değişince yapılıyor, yani
    /// sabit yazılar kurulumda bir kez ölçülüp bir daha dokunulmuyor.
    /// Kapalı ekranlarda <c>LateUpdate</c> hiç çalışmaz.
    /// </summary>
    /// <remarks>
    /// <c>ExecuteAlways</c> BİLEREK: doğrulama yakalamaları düzenleyici
    /// kipinde alınıyor (<c>StoreScreen.Build</c>, <c>CreateResultPreview</c>).
    /// Bileşen yalnız oynatma kipinde çalışsaydı, taşmayı önleyen kod
    /// yakalamalarda hiç koşmaz ve "düzeltildi" diye ölçtüğümüz kare
    /// düzeltilmemiş hâli gösterirdi — bu projede üç kez düşülen tuzağın
    /// (doğrulanamayan kod) tam olarak kendisi.
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(TextMeshProUGUI))]
    [DisallowMultipleComponent]
    public sealed class UiTextFit : MonoBehaviour
    {
        /// <summary>
        /// Puntonun inebileceği en düşük oran.
        ///
        /// Taban olmasa tek bir uzun kelime yazıyı okunmaz hâle getirebilir;
        /// %45'in altına inen bir etiket zaten yanlış kutuya konmuş demektir
        /// ve o zaman taşması görünür kalsın — hata gizlenmesin.
        /// </summary>
        public const float MinScale = 0.45f;

        /// <summary>Ölçüm gürültüsü payı: tam sınırda titremesin.</summary>
        const float Slack = 0.995f;

        TextMeshProUGUI _label;
        RectTransform _rect;

        /// <summary>Kurulumdaki punto — küçültme HER ZAMAN buradan hesaplanır.</summary>
        float _baseSize;
        bool _hasBase;

        string _lastText;
        float _lastWidth = -1f;

        /// <summary>
        /// Kutusunu bilerek taşan etiketler için kapatma yolu.
        /// (<see cref="UiKit.NoFit"/> bunu çağırıyor.)
        /// </summary>
        public void Release()
        {
            if (_hasBase && _label != null) _label.fontSize = _baseSize;
            enabled = false;
        }

        /// <summary>
        /// Kurulum puntosunu yeniden okur. Punto KOD ile değiştirildiğinde
        /// (düzen aracı, tema) çağrılmalı; yoksa bileşen eski tabanı korur.
        /// </summary>
        public void Rebase()
        {
            Cache();
            if (_label == null) return;
            _baseSize = _label.fontSize;
            _hasBase = true;
            _lastWidth = -1f;
        }

        void Cache()
        {
            if (_label == null) _label = GetComponent<TextMeshProUGUI>();
            if (_rect == null) _rect = transform as RectTransform;
        }

        void OnEnable()
        {
            Cache();
            if (!_hasBase && _label != null)
            {
                _baseSize = _label.fontSize;
                _hasBase = true;
            }
            _lastWidth = -1f;      // yeniden açılışta ölçümü tazele
        }

        void OnRectTransformDimensionsChange() => _lastWidth = -1f;

        /// <summary>
        /// Ölçümü HEMEN yap — kare beklemeden. Yakalama araçları
        /// (<c>Canvas.ForceUpdateCanvases</c> sonrası) bunu çağırıyor.
        /// </summary>
        public void FitNow() => LateUpdate();

        void LateUpdate()
        {
            Cache();
            if (_label == null || _rect == null) return;

            // TMP'nin KENDİ otomatik boyutlandırması açıksa karışma.
            //
            // DERS (iki ölçekleyici tek puntoyu paylaşamaz): İlk sürümde bu
            // kontrol yoktu ve alt sekme çubuğunun "Leaderboard" etiketi
            // taşımaya devam etti. Sebep: o etikette `enableAutoSizing`
            // açıktı, yani puntoyu TMP kendisi yazıyordu; biz `fontSize`'ı
            // taban değere geri koyuyorduk, TMP bir sonraki karede kendi
            // değerini yazıyordu ve ölçüm hep yanlış anı görüyordu.
            // Denetim aracı bunu tek satırda gösterdi (kutu 203 < gerek 262).
            if (_label.enableAutoSizing) return;

            float width = _rect.rect.width;
            if (width <= 1f) return;                     // düzen daha kurulmadı

            // Metin nesnesi aynı referanssa ve kutu kıpırdamadıysa iş yok.
            if (ReferenceEquals(_lastText, _label.text) &&
                Mathf.Approximately(_lastWidth, width)) return;

            Fit(width);
        }

        void Fit(float width)
        {
            if (!_hasBase)
            {
                _baseSize = _label.fontSize;
                _hasBase = true;
            }

            _lastText = _label.text;
            _lastWidth = width;

            if (string.IsNullOrEmpty(_lastText))
            {
                _label.fontSize = _baseSize;
                return;
            }

            // ÖLÇÜM HER ZAMAN TABAN PUNTODA. Küçültülmüş puntoda ölçüp yeniden
            // küçültmek, metin kısaldığında yazının bir daha büyümemesine yol
            // açar: sayaç "100 000"den "50"ye düşse bile etiket küçük kalırdı.
            if (!Mathf.Approximately(_label.fontSize, _baseSize))
                _label.fontSize = _baseSize;

            float preferred = _label.GetPreferredValues(_lastText,
                Mathf.Infinity, Mathf.Infinity).x;
            if (preferred <= width * Slack) return;      // zaten sığıyor

            float scale = Mathf.Max(MinScale, width * Slack / preferred);
            _label.fontSize = _baseSize * scale;
        }
    }
}
