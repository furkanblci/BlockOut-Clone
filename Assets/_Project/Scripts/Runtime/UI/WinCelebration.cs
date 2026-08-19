using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Bölüm kazanıldığında ödül kartından ÖNCE oynayan kutlama.
    ///
    /// REFERANS (`menus,powerups,vs.mp4` 01:56–02:01, 4 fps'te çıkarıldı):
    /// dizilim dört adım. (1) Tahta boşalır, ekran siyaha döner. (2) "BLOCK
    /// OUT!" logosu siyahın üstünde yaylanarak oturur — önce "BLOCK", hemen
    /// ardından "OUT!". (3) Yaklaşık 4,5 saniye boyunca arkada havai fişekler
    /// patlar, önde yoğun renkli konfeti düşer; logo ortada durur. (4) Ancak
    /// bundan sonra "MÜKEMMEL!" ödül kartı açılır.
    ///
    /// Bizde 2. ve 3. adım hiç yoktu: tahta boşalıyor ve kart doğrudan
    /// açılıyordu. Kullanıcının "kazanma ekranında çıkan efektler" dediği şey
    /// tam olarak bu eksik iki adım (2. tur, 50. madde).
    ///
    /// DERS (kutlama REFERANSTAN KISA olmalı): Referans burada 4,5 saniye
    /// harcıyor. Elli bölüm oynayan biri için bu, elli kez beklemek demek.
    /// Bizim süremiz 2,2 saniye ve DOKUNULUNCA KESİLİYOR — gösteriyi izlemek
    /// isteyen izler, acelesi olan geçer. Atlanamayan kutlama, kutlama değil
    /// vergidir.
    /// </summary>
    public sealed class WinCelebration : MonoBehaviour
    {
        const float FadeIn = 0.22f;
        const float Show = 2.20f;
        const float FadeOut = 0.30f;

        Canvas _canvas;
        RectTransform _root;
        RectTransform _logo;
        Image _curtain;
        TextMeshProUGUI _logoTop, _logoBottom;
        Image _logoImage;
        Coroutine _running;
        bool _skip;

        public static WinCelebration Create(Transform parent)
        {
            var holder = new GameObject("WinCelebration");
            holder.transform.SetParent(parent, worldPositionStays: false);
            var celebration = holder.AddComponent<WinCelebration>();
            celebration.Build();
            return celebration;
        }

        void Build()
        {
            _canvas = UiKit.CreateCanvas("WinCanvas");
            _canvas.transform.SetParent(transform, worldPositionStays: false);
            _canvas.sortingOrder = 210;          // sonuç kartının da üstünde

            _root = UiKit.CreateRect("Root", _canvas.transform);
            UiKit.Place(_root, 0f, 0f, 1f, 1f);

            // Referansta zemin TAM SİYAH, oyunun moru değil: fişeklerin
            // parlaklığı ancak siyahın üstünde okunuyor. Mor zeminde aynı
            // kıvılcımlar soluk kalırdı.
            _curtain = UiKit.CreatePanel("Curtain", _root, new Color(0f, 0f, 0f, 1f));
            _curtain.raycastTarget = true;       // dokunuş kutlamayı keser

            var skip = _curtain.gameObject.AddComponent<Button>();
            skip.transition = Selectable.Transition.None;
            skip.onClick.AddListener(() => _skip = true);

            // GERÇEK LOGO GÖRSELİ (49. madde).
            //
            // ÖLÇÜM (`menus,powerups,vs.mp4` kazanma dizilimi, 384x832 kare):
            // logonun mor halesi x 54..321 — yani ekran genişliğinin %69,5'i;
            // dikey merkezi 0.496, tam ortada.
            //
            // DERS (harf biçimi TARİF EDİLEMEZ): Logo önce iki TMP satırıyla
            // kuruluyordu, sonra yapay zekâya "aynı tarzda" ürettirildi;
            // ikisi de referansa benzemedi. Bir markanın harfleri onun
            // kimliği — "kalın, yuvarlak, altın" diye tarif edilince ortaya
            // hep başka bir şey çıkıyor. Doğru yol referans karesinden
            // kesmekti; medyan alma hilesi de arkadaki konfetiyi sildi.
            // Dikdörtgen, görselin en-boy oranından DAHA UZUN tutuluyor.
            //
            // DERS (`preserveAspect` hangi kenara sığdırır?): Önce 0.695 x 0.20
            // yazdım — genişlik doğru olsun diye. Ölçtüğümde logo 0.589 çıktı:
            // dikdörtgen (1.95) görselden (1.66) daha YASSI olduğu için
            // `preserveAspect` YÜKSEKLİĞE sığdırdı ve genişlik kendiliğinden
            // küçüldü. İstenen kenarın bağlayıcı olması için diğer kenar bol
            // bırakılmalı.
            _logo = UiKit.CreateRect("Logo", _root);
            UiKit.Place(_logo, 0.1525f, 0.366f, 0.8475f, 0.626f);   // %69,5 genişlik

            var art = UiSkin.Get(Art.GameLogo);
            if (art != null && art.texture != null)
            {
                BuildLetters(art);
            }
            else if (art != null)
            {
                _logoImage = UiKit.CreateIcon("Mark", _logo, art);
                UiKit.Place(_logoImage, 0f, 0f, 1f, 1f);
                _logoImage.preserveAspect = true;
            }
            else
            {
                // Görsel yoksa eski iki satırlık yazıya düşülür; ekran boş
                // kalmasın diye duruyor, tercih edilen yol değil.
                _logoTop = UiKit.CreateTitle("Top", _logo, "BLOCK", 96,
                    new Color(0.98f, 0.31f, 0.29f), new Color(0.24f, 0.06f, 0.30f));
                UiKit.Place(_logoTop, 0f, 0.50f, 1f, 1f);

                _logoBottom = UiKit.CreateTitle("Bottom", _logo, "OUT!", 96,
                    new Color(1f, 0.79f, 0.13f), new Color(0.24f, 0.06f, 0.30f));
                UiKit.Place(_logoBottom, 0f, 0f, 1f, 0.50f);
            }

            _root.gameObject.SetActive(false);
        }

        readonly System.Collections.Generic.List<RectTransform> _letters =
            new System.Collections.Generic.List<RectTransform>();

        /// <summary>
        /// Logoyu HARF HARF gelebilecek dilimlere böler (4. tur, J37).
        ///
        /// Kullanıcı: "'BLOCKOUT' yazısı animasyonu — harf harf geliyor,
        /// animasyonu incelenip birebir aynısı yapılacak."
        ///
        /// REFERANS ÇÖZÜMLENDİ (`Levels 1-20` yürüyüşü 28-34. saniyeler,
        /// 15 fps'te 90 kare): ekran siyaha döndükten sonra logo TEK PARÇA
        /// gelmiyor. Önce küçücük bir "B" beliriyor, sonra "BL", "BLO",
        /// "BLOCK"… her harf soldan sağa ekleniyor ve grup büyüdükçe ölçek de
        /// büyüyor. "BLOCK" tamamlanınca ikinci satır "OUT!" aynı biçimde
        /// diziliyor. Ancak logo tamamlandıktan SONRA konfeti ve fişek
        /// başlıyor — bizde ikisi aynı anda patlıyordu.
        ///
        /// DERS (bir görseli parçalamadan da harflere ayırabilirsin):
        /// Logo tek bir PNG ve harfler ortak bir mor konturla birbirine
        /// bağlı; makasla kesmek konturu bozardı. `RawImage.uvRect` görselin
        /// bir DİLİMİNİ gösteriyor, dilimin arayüz dikdörtgeni de aynı orana
        /// yerleştiriliyor: dilimlerin hepsi görününce sonuç piksel piksel
        /// bütün logonun aynısı. Yani "kesme" işi çalışma anında, kayıpsız.
        ///
        /// ÖLÇÜM (`art_raw/logo_game.png`, 662×399; harf pikselleri mor
        /// konturdan doygunlukla ayrıldı): iki satırın arası y=205'te
        /// (profil orada 456'dan 39'a düşüyor). "BLOCK" x 14-645,
        /// "OUT!" x 41-572.
        /// </summary>
        void BuildLetters(Sprite art)
        {
            var texture = art.texture;
            var region = art.textureRect;
            float tw = texture.width, th = texture.height;

            // Sprite atlasa girerse textureRect kayar; UV'yi ondan üretmek
            // her iki durumda da doğru kalıyor.
            float baseU = region.x / tw, baseV = region.y / th;
            float spanU = region.width / tw, spanV = region.height / th;

            // Satır ayrımı: görselin ALTTAN oranı. y=205/399 üstten,
            // yani alttan 1 - 0.514 = 0.486.
            const float LineSplit = 0.486f;

            // Harf sınırları (görselin genişliğine oran). Eşit bölmek yerine
            // ölçülen harf kutularına göre: "O" tuğla harfi geniş, "!" dar.
            float[] blockCuts = { 0f, 0.185f, 0.320f, 0.520f, 0.700f, 1f };
            float[] outCuts = { 0f, 0.235f, 0.435f, 0.640f, 1f };

            AddRow(blockCuts, LineSplit, 1f);
            AddRow(outCuts, 0f, LineSplit);

            void AddRow(float[] cuts, float v0, float v1)
            {
                for (int i = 0; i + 1 < cuts.Length; i++)
                {
                    float u0 = cuts[i], u1 = cuts[i + 1];

                    var slice = UiKit.CreateRect($"Letter_{_letters.Count}", _logo);
                    slice.anchorMin = new Vector2(u0, v0);
                    slice.anchorMax = new Vector2(u1, v1);
                    slice.offsetMin = Vector2.zero;
                    slice.offsetMax = Vector2.zero;

                    var raw = slice.gameObject.AddComponent<RawImage>();
                    raw.texture = texture;
                    raw.raycastTarget = false;
                    raw.uvRect = new Rect(
                        baseU + u0 * spanU, baseV + v0 * spanV,
                        (u1 - u0) * spanU, (v1 - v0) * spanV);

                    slice.localScale = Vector3.zero;
                    _letters.Add(slice);
                }
            }
        }

        /// <summary>
        /// Dilimleri soldan sağa, önce üst satır olmak üzere sırayla getirir.
        ///
        /// Grup ölçeği de büyüyor: referansta logo küçük bir "B" olarak
        /// başlayıp harfler eklendikçe irileşiyor. Yalnız dilimleri açmak
        /// "harfler belirdi" der; ölçeğin büyümesi "logo KURULUYOR" der.
        /// </summary>
        IEnumerator RevealLetters()
        {
            const float step = 0.075f;
            const float pop = 0.20f;

            _logo.localScale = Vector3.one * 0.42f;
            float total = _letters.Count * step + pop;

            for (int i = 0; i < _letters.Count; i++)
            {
                GameKit.FX.Juice.Run(PopLetter(_letters[i]));

                for (float t = 0f; t < step && !_skip; t += Time.unscaledDeltaTime)
                {
                    float progress = Mathf.Clamp01((i * step + t) / total);
                    _logo.localScale = Vector3.one *
                        Mathf.Lerp(0.42f, 1f, GameKit.FX.Juice.EaseOutBack(progress, 1.1f));
                    yield return null;
                }
                if (_skip) break;
            }

            foreach (var letter in _letters) if (letter != null) letter.localScale = Vector3.one;
            _logo.localScale = Vector3.one;
        }

        static IEnumerator PopLetter(RectTransform letter)
        {
            const float duration = 0.20f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (letter == null) yield break;
                float k = GameKit.FX.Juice.EaseOutBack(t / duration, 3.0f);
                letter.localScale = Vector3.one * k;
                yield return null;
            }
            if (letter != null) letter.localScale = Vector3.one;
        }

        /// <summary>Kutlamayı oynatır; bittiğinde <paramref name="done"/> çağrılır.</summary>
        public void Play(System.Action done)
        {
            if (_running != null) GameKit.FX.Juice.Stop(_running);
            _running = GameKit.FX.Juice.Run(Routine(done));
        }

        IEnumerator Routine(System.Action done)
        {
            _skip = false;
            _root.gameObject.SetActive(true);
            SetAlpha(0f);

            yield return GameKit.FX.Juice.Tween(FadeIn, SetAlpha);

            // LOGO ÖNCE KURULUR, KUTLAMA SONRA BAŞLAR (4. tur, J37/J38).
            //
            // DERS (sıra bir anlatımdır): Konfeti ve fişek eskiden logoyla
            // AYNI anda başlıyordu; ekran ilk kareden itibaren dolu olduğu
            // için logonun kurulduğu görülmüyordu. Referansta dizilim net:
            // önce harfler siyahın üstünde tek tek diziliyor, logo
            // tamamlandığında ekran patlıyor. Aynı öğeler, farklı sıra,
            // bambaşka bir his.
            // DERS (yedek yol da BİR YOLDUR — çökmemeli): Buradaki üçüncü dal
            // "görsel hiç yoksa yazıyla göster" diyordu ve `_logoTop`u null
            // KONTROL ETMEDEN kullanıyordu. Bir kez o dala düşünce
            // NullReferenceException coroutine'i öldürdü; `done` hiç
            // çağrılmadı ve PERFECT kartı ASLA AÇILMADI. Yani küçük bir
            // yedek yol hatası, oyunun bitiş akışını tamamen durdurdu.
            //
            // Kutlama bir SÜS; hiçbir koşulda oyunun akışını kesmemeli.
            // Bu yüzden her dal null'a dayanıklı ve hiçbiri "hiç bitmeme"
            // ihtimali taşımıyor.
            if (_letters.Count > 0)
            {
                yield return RevealLetters();
            }
            else if (_logoImage != null)
            {
                GameKit.FX.Juice.Run(DropIn(_logo, 0f));
            }
            else
            {
                if (_logoTop != null) GameKit.FX.Juice.Run(DropIn(_logoTop.rectTransform, 0f));
                if (_logoBottom != null)
                    GameKit.FX.Juice.Run(DropIn(_logoBottom.rectTransform, 0.10f));
            }

            // Tamamlanan logoya tek bir vuruş: "işte bu" anı.
            GameKit.FX.Juice.Run(Punch(_logo));

            // ROKETLER: referansta konfetiden ÖNCE ekranın altından yukarı
            // beyaz izler fırlıyor, patlamalar onların ucunda oluyor.
            GameKit.FX.Juice.Run(FX.CelebrationFX.Rockets(_root, count: 6, interval: 0.22f));

            // Fişek ARKADA, konfeti ÖNDE: ikisi de aynı kökte yaşıyor ama
            // fişekler önce yaratıldığı için çizim sırasında altta kalıyor.
            // Patlama aralığı kıvılcım ömründen (0,95 sn) KISA: bir patlama
            // sönmeden diğeri başlasın ki ekran hiç boş kalmasın. Referansta
            // her karede en az iki patlama birden var.
            // Referansta her patlama bizimkinden çok daha KALABALIK ve
            // patlamalar üst üste biniyor; 30 ışınlı seyrek bir çelenk
            // "havai fişek" değil "pusula gülü" gibi okunuyordu.
            GameKit.FX.Juice.Run(FX.CelebrationFX.Show(
                _root, bursts: 10, interval: 0.20f, sparks: 48));

            // KONFETİ SAYISI SAYILDI, tahmin edilmedi.
            //
            // Referans karelerinde logo bölgesi dışlanıp bağlı bileşenler
            // sayıldı: aynı anda ORTALAMA 276 parça var (161-432 arası).
            // Parça alanı medyanı 17 piksel², yani kenarı ~4,1 piksel —
            // 384 genişlikteki karede ekranın %1,07'si.
            //
            // İlk denemede 70, sonra 190 yazmıştım; ikisi de "kutlama" değil
            // "birkaç kağıt düştü" gibi okunuyordu. Bir yoğunluğu gözle
            // ayarlamak yerine saymak, üç denemeyi tek ölçüme indiriyor.
            GameKit.FX.Juice.Run(FX.CelebrationFX.Rain(
                _root, count: 300, duration: Show * 0.55f));

            Services.AudioService.Star();

            for (float t = 0f; t < Show && !_skip; t += Time.unscaledDeltaTime)
                yield return null;

            yield return GameKit.FX.Juice.Tween(FadeOut, t => SetAlpha(1f - t));

            _root.gameObject.SetActive(false);
            _running = null;
            done?.Invoke();
        }

        /// <summary>
        /// Perdeyi ve logoyu birlikte söndürür.
        ///
        /// DERS (kıvılcımlar bu alfaya KATILMAZ): Alfa yalnız perde ve logo
        /// üstünde geziniyor; fişek ve konfeti kendi ömürlerini kendileri
        /// söndürüyor. Hepsini tek alfaya bağlamak, her karede onlarca
        /// görselin rengini yeniden yazmak demekti — üstelik yeni doğan bir
        /// kıvılcım o an sönmüş olarak başlardı.
        /// </summary>
        void SetAlpha(float alpha)
        {
            var color = _curtain.color;
            color.a = alpha;
            _curtain.color = color;

            if (_logoImage != null)
            {
                var ic = _logoImage.color;
                ic.a = alpha;
                _logoImage.color = ic;
            }
            SetTextAlpha(_logoTop, alpha);
            SetTextAlpha(_logoBottom, alpha);
        }

        static void SetTextAlpha(TextMeshProUGUI text, float alpha)
        {
            if (text == null) return;
            var c = text.color;
            c.a = alpha;
            text.color = c;
        }

        /// <summary>Tamamlanan logoya kısa bir ölçek vuruşu.</summary>
        static IEnumerator Punch(RectTransform target)
        {
            const float duration = 0.26f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (target == null) yield break;
                float k = t / duration;
                target.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        /// <summary>Satırın yukarıdan düşüp yaylanarak oturması.</summary>
        static IEnumerator DropIn(RectTransform line, float delay)
        {
            if (line == null) yield break;

            Vector2 target = line.anchoredPosition;
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            const float duration = 0.34f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                if (line == null) yield break;
                float k = GameKit.FX.Juice.EaseOutBack(t / duration, 2.4f);
                line.anchoredPosition = target + new Vector2(0f, (1f - k) * 420f);
                line.localScale = Vector3.one * (0.65f + 0.35f * k);
                yield return null;
            }

            line.anchoredPosition = target;
            line.localScale = Vector3.one;
        }
    }
}
