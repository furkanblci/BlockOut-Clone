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
        /// <summary>Referanstaki konfeti/fişek renkleri: oyunun blok paleti.</summary>
        public static readonly Color[] Palette =
        {
            new Color(1f, 0.85f, 0.20f),   // altın
            new Color(0.98f, 0.31f, 0.38f),// mercan
            new Color(0.35f, 0.78f, 1f),   // camgöbeği
            new Color(0.55f, 1f, 0.45f),   // yeşil
            new Color(0.85f, 0.55f, 1f),   // mor
            new Color(1f, 0.45f, 0.78f),   // pembe
            new Color(1f, 1f, 1f),         // beyaz
        };

        // ---- Havai fişek ---------------------------------------------------

        /// <summary>
        /// Merkezden dışa halka çizen kıvılcım patlaması.
        ///
        /// <paramref name="anchor"/> ekranın merkezine göre oran (-0.5..0.5).
        /// </summary>
        public static void Explode(RectTransform root, Vector2 anchor, Color color,
                                   int sparks = 30, float minSpeed = 1400f,
                                   float maxSpeed = 2500f)
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
                var spark = UiKit.CreatePanel("Spark", root, color);
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
                float reach = position.magnitude;
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

            // ÖLÇÜM: referans karelerde parça alanı medyanı 17 piksel², yani
            // kenarı ~4,1 piksel — 384 genişlikteki karede ekranın %1,07'si.
            // Bizim tuval 1080 geniş; oran korunuyor, piksel değeri değil.
            float size = Random.Range(9f, 16f);
            rect.sizeDelta = new Vector2(size, size * Random.Range(0.5f, 1.1f));
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
