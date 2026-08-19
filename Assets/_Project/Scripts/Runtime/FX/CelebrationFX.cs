using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.FX
{
    /// <summary>
    /// Kutlama parçacıkları: havai fişek ve konfeti. Arayüz uzayında çalışır.
    ///
    /// DERS (konfeti ile havai fişek FARKLI ŞEYLER anlatır): Havai fişek
    /// merkezden DIŞA patlar; enerjisi dışa doğrudur ve "bir şey oluyor" der.
    /// Konfeti yukarıdan DÜŞER; kutlama biter, kağıtlar yere iner ve "başardın"
    /// der. Referansın kazanma dizilimi ikisini de kullanıyor: fişekler arkada
    /// patlarken konfeti önde düşüyor. Bölüm geçişinde ise yalnız fişek var —
    /// orası bir bitiş değil, başlangıç anı.
    ///
    /// DERS (neden ayrı bir dosya?): Bu iki efekt <see cref="UI.LevelIntro"/>
    /// içinde yazılmıştı ve kazanma ekranı da aynısına ihtiyaç duydu. İkinci
    /// kopyayı çıkarmak yerine buraya taşındı: fişek eğrisi bir gün
    /// değiştiğinde iki yerde birden düzeltmek, er geç bir yerin unutulması
    /// demektir.
    ///
    /// Her şey <c>unscaledDeltaTime</c> ile akıyor — kutlama anında oyun
    /// zamanı çoktan durmuş oluyor.
    /// </summary>
    public static class CelebrationFX
    {
        /// <summary>
        /// Konfeti/fişek renkleri — REFERANSTAN ÖRNEKLENDİ VE AĞIRLIKLANDI
        /// (7. tur, R60).
        ///
        /// Kullanıcı: "Perfect Coin ekranı ve oyun kazanma BLOCKOUT
        /// ekranındaki konfeti efektleri revize edilip daha iyi hale
        /// getirilecek."
        ///
        /// ÖLÇÜM (`…Levels 1-20 Walkthrough.mp4` 11:26, BLOCKOUT kutlaması;
        /// logo bölgesi dışlanıp bağlı bileşenler ayrıştırıldı, 192 parça):
        ///   yeşil  (75,177,129)  62 parça   %32
        ///   mavi   (61, 65,213)  49         %26
        ///   sarı   (230,165, 26) 25         %13
        ///   kırmızı(192, 11, 35) 24         %13
        ///   beyaz  (239,234,220) 15         %8
        ///   pembe  (216, 68,156)  6         %3
        ///
        /// Bizim palet YEDİ RENGİ EŞİT olasılıkla dağıtıyordu ve tonları
        /// pastel-parlaktı (mercan, camgöbeği, lavanta). Referansın konfetisi
        /// hem daha DOYGUN hem de yeşil-mavi ağırlıklı; eşit dağılım ekrana
        /// "gökkuşağı" veriyor, referanstaki ise oyunun kendi renk kimliğini
        /// taşıyor.
        ///
        /// DERS (bir palet yalnız renklerden ibaret değil, ORANLARDAN da
        /// ibarettir): Doğru renkleri eşit dağıtmak yanlış bir görüntü
        /// üretiyor. Ağırlık, dizide tekrar ederek veriliyor — rastgele
        /// seçimin kendisi değişmiyor.
        /// </summary>
        public static readonly Color[] Palette =
        {
            new Color(0.294f, 0.694f, 0.506f),  // yeşil  ×4
            new Color(0.294f, 0.694f, 0.506f),
            new Color(0.294f, 0.694f, 0.506f),
            new Color(0.294f, 0.694f, 0.506f),
            new Color(0.239f, 0.255f, 0.835f),  // mavi   ×4
            new Color(0.239f, 0.255f, 0.835f),
            new Color(0.239f, 0.255f, 0.835f),
            new Color(0.239f, 0.255f, 0.835f),
            new Color(0.902f, 0.647f, 0.102f),  // sarı   ×2
            new Color(0.902f, 0.647f, 0.102f),
            new Color(0.753f, 0.043f, 0.137f),  // kırmızı ×2
            new Color(0.753f, 0.043f, 0.137f),
            new Color(0.937f, 0.918f, 0.863f),  // beyaz  ×1
            new Color(0.847f, 0.267f, 0.612f),  // pembe  ×1
        };

        // ---- Havai fişek ---------------------------------------------------

        /// <summary>
        /// Merkezden dışa halka çizen kıvılcım patlaması.
        ///
        /// <paramref name="anchor"/> ekranın merkezine göre oran (-0.5..0.5).
        /// </summary>
        public static void Explode(RectTransform root, Vector2 anchor, Color color,
                                   int sparks = 30, float minSpeed = 900f,
                                   float maxSpeed = 1700f)
        {
            for (int i = 0; i < sparks; i++)
            {
                // DÜZ DİKDÖRTGEN, 9-dilim değil.
                //
                // DERS (9-dilim kenar payı dikdörtgenden büyük olamaz): Kıvılcım
                // önce `CreateRoundedPanel` + `SetSliceScale(0.10f)` ile
                // kuruluyordu; uçları yuvarlak olsun diye. Ama 0.10 çarpanı
                // sprite'ın 18 piksellik kenar payını 180 piksele çıkarıyor ve
                // dikdörtgen 30x7. Kenar payları dikdörtgenin kendisinden
                // kat kat büyük olunca Unity bozuk bir ağ üretiyor: nesneler
                // sahnede duruyor, doğru ölçekte ve alfada — ama ekrana hiçbir
                // şey çizilmiyor. Bu yüzden "60 kıvılcım var" diyen sayaçla
                // bomboş bir ekran yan yana durabiliyordu.
                // HER KIVILCIM KENDİ RENGİNİ ALIYOR (4. tur, J38).
                //
                // Patlama tek renkti ve ışınlar eşit uzunlukta olduğu için
                // ekranda "havai fişek" değil PUSULA GÜLÜ çıkıyordu — sarı,
                // düz, uzun çizgilerden oluşan bir yıldız. Referansta bir
                // patlamanın içinde birden çok renk var ve ışınlar farklı
                // boylarda.
                //
                // Verilen renk yine baskın: kıvılcımların üçte biri onu
                // kullanıyor, kalanı paletten. Böylece patlamanın bir kimliği
                // oluyor ama tekdüze olmuyor.
                var tint = Random.value < 0.34f
                    ? color
                    : Palette[Random.Range(0, Palette.Length)];
                var spark = UiKit.CreatePanel("Spark", root, tint);
                spark.raycastTarget = false;

                var rect = spark.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f + anchor.x, 0.5f + anchor.y);
                // PIVOT SOL UÇTA: iz merkezden DIŞA uzasın diye. Ortadan
                // büyütmek izi iki yöne birden uzatır ve patlamanın içi dolar.
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(30f, 7f);
                rect.anchoredPosition = Vector2.zero;

                // Kıvılcımlar halka boyunca EŞİT dağılır, rastgele değil:
                // rastgele açılar kümeleşip patlamayı tek yöne kaydırıyor.
                float angle = (i / (float)sparks) * Mathf.PI * 2f
                              + Random.Range(-0.06f, 0.06f);
                float speed = Random.Range(minSpeed, maxSpeed);
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

                rect.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
                GameKit.FX.Juice.Run(SparkFly(rect, velocity));
            }
        }

        /// <summary>
        /// Kıvılcım: dışa fırlar, yavaşlar, düşerek söner.
        ///
        /// DERS (havai fişek KOPUK NOKTALARDAN değil İZLERDEN oluşur):
        /// İlk yazımda her kıvılcım sabit boyda küçük bir çizgiydi ve hepsi
        /// aynı yarıçapa gidiyordu; ekranda patlama değil bir SAAT KADRANI
        /// çıkıyordu — eşit aralıklı, eşit boyda çizgilerden oluşan bir halka.
        /// Referansta ise her ışın merkezden başlayıp dışa doğru UZUYOR.
        /// Düzeltme iki parça: pivot sol uca alındı (iz dışa doğru uzasın) ve
        /// uzunluk kat edilen yola bağlandı. Böylece hızlı giden uzun, yavaş
        /// giden kısa bir iz bırakıyor ve halka kendiliğinden bozuluyor.
        /// </summary>
        static IEnumerator SparkFly(RectTransform piece, Vector2 velocity)
        {
            var position = Vector2.zero;
            var image = piece.GetComponent<Image>();
            Color color = image.color;
            const float life = 0.95f;
            const float baseWidth = 30f;

            for (float t = 0f; t < life; t += Time.unscaledDeltaTime)
            {
                if (piece == null) yield break;

                // Hava sürtünmesi: patlama hızlı başlar, hızla yavaşlar.
                velocity *= 1f - 3.2f * Time.unscaledDeltaTime;
                velocity.y -= 900f * Time.unscaledDeltaTime;
                position += velocity * Time.unscaledDeltaTime;

                // KUYRUK MERKEZDE KALIR: dikdörtgen patlama noktasında duruyor,
                // yalnız boyu ve açısı değişiyor. Pivot sol uçta olduğu için
                // uzayan taraf dışarısı; ışın merkezden başlayıp başın olduğu
                // yere kadar çiziliyor.
                float k = t / life;

                // İZİN BOYU SINIRLI (4. tur, J38).
                //
                // Uzunluk doğrudan kat edilen yola bağlıydı ve hiçbir tavanı
                // yoktu: 2500 birim/sn ile fırlayan bir kıvılcım ekranın
                // yarısını geçen düz bir çizgiye dönüşüyordu. Referansta
                // ışınlar KISA ve kalın; patlamayı patlama yapan şey ışının
                // uzunluğu değil, sayısı ve dağılımı.
                //
                // DERS (orantı, TAVANSIZ bırakılırsa ölçeği yutar): "Uzunluk
                // yolla orantılı olsun" doğru bir fikirdi; eksik olan, o
                // orantının nerede duracağıydı.
                float reach = Mathf.Min(position.magnitude, baseWidth * 7f);
                piece.localRotation = Quaternion.Euler(
                    0f, 0f, Mathf.Atan2(position.y, position.x) * Mathf.Rad2Deg);
                piece.localScale = new Vector3(
                    Mathf.Max(1f, reach / baseWidth), 1f - k * 0.55f, 1f);

                color.a = 1f - k * k;                 // sonlara doğru hızla söner
                image.color = color;
                yield return null;
            }

            if (piece != null) Object.Destroy(piece.gameObject);
        }

        /// <summary>
        /// Sırayla patlayan fişek gösterisi.
        ///
        /// Patlamalar arasına gecikme konuyor: aynı anda üç patlama tek bir
        /// gürültü lekesi olur, sırayla gelen üç patlama gösteri olur.
        /// Konumlar SABİT bir listeden geliyor, rastgele değil — rastgele
        /// noktalar bazen üst üste düşüp ekranın yarısını boş bırakıyordu.
        /// </summary>
        public static IEnumerator Show(RectTransform root, int bursts,
                                       float interval, int sparks = 18)
        {
            // ORTA ŞERİT BOŞ: logo ekranın dikey ortasında duruyor ve oraya
            // patlayan bir fişek harflerin üstünü kapatıyor. Referansta da
            // patlamalar logonun ÜSTÜNDE ve ALTINDA; hiçbiri onun üzerinde
            // değil. Konumlar |y| >= 0.20 seçildi.
            var spots = new[]
            {
                new Vector2(-0.24f,  0.26f), new Vector2( 0.26f,  0.21f),
                new Vector2(-0.06f, -0.24f), new Vector2( 0.30f,  0.34f),
                new Vector2(-0.30f, -0.32f), new Vector2( 0.08f,  0.36f),
                new Vector2(-0.14f,  0.34f), new Vector2( 0.20f, -0.28f),
                new Vector2(-0.28f, -0.20f), new Vector2( 0.14f, -0.36f),
            };

            for (int burst = 0; burst < bursts; burst++)
            {
                if (root == null) yield break;
                Explode(root, spots[burst % spots.Length],
                        Palette[burst % Palette.Length], sparks);
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        /// <summary>
        /// Ekranın ALTINDAN yukarı fırlayan beyaz roket izleri; tepede
        /// patlarlar (4. tur, J38).
        ///
        /// REFERANS (`Levels 1-20` yürüyüşü, 32-34. saniyeler): logo
        /// tamamlandıktan sonra ekranın alt kenarından yukarı doğru ince
        /// BEYAZ çizgiler çıkıyor ve ucunda renkli patlama oluyor. Bizde
        /// yalnız havada beliren patlamalar vardı; patlamanın NEREDEN geldiği
        /// görünmüyordu.
        ///
        /// DERS (bir olayın sebebini göstermek, sonucunu göstermekten
        /// önemlidir): Havada beliren patlama "bir şey patladı" der; yükselen
        /// iz "biri fişek attı" der. İkincisi sahneye bir fail ekliyor ve
        /// kutlamayı biri tarafından YAPILMIŞ bir şeye çeviriyor.
        /// </summary>
        public static IEnumerator Rockets(RectTransform root, int count, float interval)
        {
            float[] lanes = { -0.34f, 0.28f, -0.12f, 0.38f, 0.05f, -0.40f, 0.20f, -0.24f };

            for (int i = 0; i < count; i++)
            {
                if (root == null) yield break;
                float lane = lanes[i % lanes.Length];
                float apex = Random.Range(0.10f, 0.34f);
                GameKit.FX.Juice.Run(RocketFly(root, lane, apex,
                    Palette[(i * 3) % Palette.Length]));
                yield return new WaitForSecondsRealtime(interval);
            }
        }

        static IEnumerator RocketFly(RectTransform root, float lane, float apex, Color color)
        {
            var trail = UiKit.CreatePanel("Rocket", root, new Color(1f, 1f, 1f, 0.9f));
            trail.raycastTarget = false;

            var rect = trail.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);          // aşağı ucu sabit
            rect.sizeDelta = new Vector2(5f, 10f);

            float startY = -1020f;
            float endY = apex * 1900f;
            const float rise = 0.42f;

            for (float t = 0f; t < rise; t += Time.unscaledDeltaTime)
            {
                if (rect == null) yield break;
                float k = t / rise;
                float y = Mathf.Lerp(startY, endY, k * (2f - k));   // yavaşlayarak
                rect.anchoredPosition = new Vector2(lane * 1080f, y);
                // İz yükselirken uzuyor, tepeye yaklaşınca kısalıyor: roketin
                // yavaşladığı bilgisi boydan okunuyor.
                rect.sizeDelta = new Vector2(5f, Mathf.Lerp(220f, 40f, k));
                var c = trail.color;
                c.a = 0.9f * (1f - k * 0.6f);
                trail.color = c;
                yield return null;
            }

            if (root != null) Explode(root, new Vector2(lane, apex), color, 42);
            if (trail != null) Object.Destroy(trail.gameObject);
        }

        // ---- Konfeti -------------------------------------------------------

        /// <summary>
        /// Ekranın üstünden dökülüp savrularak düşen kağıt parçaları.
        ///
        /// Referans ölçümü (`menus,powerups,vs.mp4` 01:58, 384x832 kare):
        /// parçaların yatay uzunluğu ortalama 4.4 piksel, medyan 4 — yani
        /// ekran genişliğinin ~%1'i. Renkler oyunun blok paletinin tamamı.
        ///
        /// DERS (hepsi aynı anda doğmaz): Bütün konfetiyi tek karede
        /// yaratmak, ekranı yatay bir şerit hâlinde geçen tek bir dalga
        /// üretiyor. Doğumları süreye yaymak yağmura benzetiyor.
        /// </summary>
        public static IEnumerator Rain(RectTransform root, int count, float duration)
        {
            // KARE BAŞINA BİR DEĞİL, KARE BAŞINA PAY.
            //
            // DERS (`yield` en az bir kare bekler): Önce her parça için
            // `WaitForSecondsRealtime(duration / count)` yazıyordum. 300 parça
            // 1,2 saniyeye yayılınca aralık 4 milisaniye çıkıyor — ama bir
            // coroutine bir karede en fazla bir kez ilerleyebilir. 60 fps'te
            // 1,2 saniye 72 kare demek, yani ekrana 300 değil 72 konfeti
            // düşüyordu ve fark ölçülene kadar görünmedi. Süreye yayılan bir
            // üretim, kare başına DÜŞEN PAY olarak yazılmalı.
            // İLK PARTİ EKRANA YAYILARAK DOĞAR, tepeden değil.
            //
            // Referansta kutlama başladığı anda konfeti ekranın TAMAMINDA var.
            // Hepsini tepeden yağdırınca ilk saniye boyunca yalnız üst üçte
            // bir doluyor; oyuncu kutlamanın "kurulmasını" izliyor. Üçte biri
            // baştan dağıtılınca perde açılır açılmaz ekran dolu görünüyor.
            int prefill = count / 3;
            for (int i = 0; i < prefill; i++) Spawn(root, spread: true);

            float budget = 0f;
            int made = prefill;
            float perSecond = (count - prefill) / Mathf.Max(0.01f, duration);

            while (made < count)
            {
                if (root == null) yield break;

                // Borç YEREL sayaçla tutuluyor, kökteki çocuklar sayılarak
                // değil: konfeti ömrü doluyor ve ölüyor, çocuk sayısı düşünce
                // "az doğurmuşum" sanılıp fazladan üretilirdi.
                budget += perSecond * Time.unscaledDeltaTime;
                while (made < count && budget >= 1f)
                {
                    Spawn(root, spread: false);
                    budget -= 1f;
                    made++;
                }

                yield return null;
            }
        }

        static void Spawn(RectTransform root, bool spread)
        {
            var piece = UiKit.CreatePanel("Confetti", root,
                Palette[Random.Range(0, Palette.Length)]);
            piece.raycastTarget = false;

            var rect = piece.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // PARÇALAR İKİ KAT BÜYÜDÜ — YENİDEN ÖLÇÜLDÜ (7. tur, R60).
            //
            // Eski ölçüm `menus,powerups,vs.mp4`ten (384×832) alınmıştı ve
            // kenarı ekranın %1,07'si buluyordu. Ama kullanıcının şikâyet
            // ettiği ekran BLOCKOUT kutlaması; o kare 592×1280 ve orada
            // bağlı bileşenler ayrıştırıldığında:
            //   genişlik ortancası 12 piksel  → ekranın **%2,03'ü**
            //   genişlik %90'lık   18 piksel  → **%3,04**
            //   yükseklik ortancası 10 piksel (genişliğin %83'ü)
            // Yani parçalar bizimkinin İKİ KATI. Küçük konfeti uzaktan
            // "gürültü" gibi okunuyor; kutlama hissini veren şey parçanın
            // TANINABİLİR olması.
            //
            // DERS (ölçümün alındığı kare, ölçümün kendisi kadar önemli):
            // İki farklı kutlama sahnesi iki farklı konfeti kullanıyor.
            // "Referanstan ölçtük" demek, doğru referanstan ölçtüğümüz
            // anlamına gelmiyor.
            //
            // 1080 birimlik tuvalde: ortanca 22, %90'lık 33 birim.
            float size = Random.Range(13f, 34f);
            rect.sizeDelta = new Vector2(size, size * Random.Range(0.55f, 1.05f));
            // `spread` ilk parti: ekranın her yerinde. Değilse tepeden.
            rect.anchoredPosition = new Vector2(
                Random.Range(-560f, 560f),
                spread ? Random.Range(-900f, 1000f) : Random.Range(1000f, 1400f));
            rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            GameKit.FX.Juice.Run(Flutter(rect));
        }

        /// <summary>
        /// Tek bir konfeti: düşerken savrulur ve döner.
        ///
        /// DERS (kağıt taş gibi düşmez): Sabit hızla dümdüz inen bir kare
        /// "konfeti" değil "piksel" gibi okunuyor. İki ekleme yetiyor:
        /// yanal salınım (hava direnci) ve eksen etrafında dönerken yassılaşma
        /// (parça yan döndüğünde inceliyor).
        /// </summary>
        static IEnumerator Flutter(RectTransform piece)
        {
            float fall = Random.Range(420f, 760f);
            float sway = Random.Range(40f, 130f);
            float swaySpeed = Random.Range(1.6f, 3.4f);
            float spin = Random.Range(-260f, 260f);
            float flip = Random.Range(3f, 7f);
            float phase = Random.Range(0f, Mathf.PI * 2f);
            float angle = piece.localRotation.eulerAngles.z;

            var position = piece.anchoredPosition;
            var image = piece.GetComponent<Image>();
            Color color = image.color;

            const float life = 3.2f;
            for (float t = 0f; t < life; t += Time.unscaledDeltaTime)
            {
                if (piece == null) yield break;
                float dt = Time.unscaledDeltaTime;

                phase += swaySpeed * dt;
                position.y -= fall * dt;
                position.x += Mathf.Cos(phase) * sway * dt;
                angle += spin * dt;

                piece.anchoredPosition = position;
                piece.localRotation = Quaternion.Euler(0f, 0f, angle);
                // Yan dönerken incelme: genişlik bir kosinüsle daralıyor.
                piece.localScale = new Vector3(
                    Mathf.Abs(Mathf.Cos(t * flip + phase)) * 0.8f + 0.2f, 1f, 1f);

                if (position.y < -1000f) break;
                yield return null;
            }

            if (piece != null)
            {
                color.a = 0f;
                image.color = color;
                Object.Destroy(piece.gameObject);
            }
        }
    }
}
