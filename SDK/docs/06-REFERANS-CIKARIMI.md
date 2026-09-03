# Referans videodan içerik çıkarma

Klonlanacak oyunun oynanış videoları, hem **bölüm tasarımının** hem de
**arayüz akışının** en güvenilir kaynağı. Bu proje 50 bölümün 49'unu videodan
çıkardı.

Araç: taşınabilir **ffmpeg** (kurulum gerekmez, zip açıp `bin/` kullan).

---

## 1. Bölüm sınırlarını bul — sahne değişimi

```bash
ffmpeg -i video.mp4 -vf "select='gt(scene,0.35)',metadata=print:file=scenes.txt" -vsync vfr out_%04d.png
```

> **Tuzak:** komutu **çıktı klasörünü cwd yaparak** çalıştır. Windows'ta sürücü
> harfindeki iki nokta (`C:`) filtre dizgisini bozuyor.

Çıkan `scenes.txt` bölüm geçişlerinin zaman damgalarını verir.

## 2. Doğru kareyi seç — sahne başı DEĞİL

Bölüm başlangıcı = sahne zaman damgası **+ 3-4 saniye**. Oyunlarda bölüm
açılışında kameranın yakınlaştığı bir giriş var; daha erken kareler yakın
plandadır ve okunmaz.

## 3. Kareyi çıkar — TAM GENİŞLİK

```bash
ffmpeg -ss 00:01:23 -i video.mp4 -frames:v 1 \
  -vf "scale=1184:2560:flags=lanczos,crop=1184:1780:0:470" kare.png
```

> **En pahalı hata:** yatay kırpmak. Geniş tahtaların kenarları kesiliyor ve
> eksik olduğu ancak bölüm oynanmayacak hâle geldiğinde anlaşılıyor. **Yatayda
> tam genişlik al**, yalnız dikeyde kırp.

Ölçeklemede `lanczos` kullan — video sıkıştırması zaten kenarları yumuşatmış,
`bilinear` üstüne bir kat daha ekliyor.

## 4. Neyin sadık, neyin yaklaşım olduğunu YAZ

Videodan her şey okunmaz. Bu projede kayıt tutuldu:

| Sadık | Yaklaşım |
|---|---|
| Tahta boyutu ve şekli | Buzlu kapının ALTINDAKİ renk (görünmüyor) |
| Blok konumları ve şekilleri (L/T/J dâhil) | Ölçeklenmiş sayaç değerleri |
| Kapı yönü, konumu, uzunluğu | Bir bölümün orta bandı (özgün transkripsiyon tahtayı ikiye mühürlüyordu) |
| Katman ve sayaç varlığı | |

> Bu tabloyu tutmazsan, aylar sonra "burası neden farklı?" sorusuna cevap
> veremezsin ve doğru olanı yanlış sanıp bozarsın.

---

## 5. İçeriği DOĞRULA — çözülemeyen bölüm yayına giremez

Videodan çıkarılan bölüm oynanabilir olmayabilir. Bu projede:

- **Açgözlü çözücü** önce, sonra bütün tahta durumu üzerinde **en-iyi-önce**
  karıştırmalı arama (sezgisel: her bloğun kendi kapısına en kısa mesafesi)
- Her bölüm için **kanıt hamle listesi** üretiliyor
- CI her PR'da hepsini yeniden çözüyor (`templates/github-workflows/`)

### Yinelenen tuzak: bütçe tutmayan sayaç

Bir sayaç "N blok önce çıkmalı" der, ama o çıkışları besleyen havuz sonludur.
Sayaç, ondan önce ulaşılabilir çıkış sayısını aşarsa **buz hiç kırılmaz ve
bölüm ölür**.

Çözücü bunu bulur ama **saniyeler** sürer ve yalnız "çözemedim" der. Ayrı bir
denetleyici (bu projede `WarnUnreachableIce`) kademeyi benzetip **sebebiyle
birlikte milisaniyede** uyarıyor.

> **Genel kural:** "çözülemedi" bir teşhis değildir. İçerik doğrulayıcısı
> **neden** çözülemediğini söylemeli.

---

## 6. Arayüz akışını da çıkar

Menü videoları (mağaza, profil, ayarlar) aynı yöntemle işlenir ama amaç farklı:
burada aradığın **sıra ve geçiş**. Hangi ekran hangisini açıyor, geri nereye
dönüyor, hangi panel kendiliğinden geliyor.

Bu projede mağaza ekranı bu şekilde yeniden kuruldu: video karelerinden ölçüm
→ düzenleme kipinde yakalama → karşılaştır → düzelt döngüsü.

---

## 7. Maliyet uyarısı

Video analizi **pahalı**. Bu projede 15+ temas sayfası + zoom serisi tek
oturumda 5 saatlik kotayı bir saatte yaktı.

**Kural:** video analizini **tek seferde, minimum kareyle** toplu yap.
Rutin işte sayısal doğrulamayı tercih et (`02-TUZAKLAR.md` §19), ekran
görüntüsünü son kontrol olarak kullan.
