# Devam Notu — yeni oturuma nasıl devam edilir

Yeni bir sohbet açtığında Claude'a şunu yaz:

> **`docs/DEVAM.md` dosyasını oku ve kaldığımız yerden devam et.**

Bu dosya her oturum sonunda güncellenir. Aşağısı 2026-08-10 itibarıyla geçerli.

---

## 2026-08-20 (11) — SON KALANLAR: HUD, ÜST BAR, SEKME ÇUBUĞU, İLERLEME ÇUBUĞU

Kullanıcının listesi: Restore Purchases, Journey'nin oynat düğmeleri,
oyun içi düğmeler, reklam ekranının çubuğu, alt menü — ve ayrıca üst
bardaki yeşil "+" ile dişli.

### Reçeteye geçenler

| yer | önce | sonra |
|-----|------|-------|
| `StoreScreen` Restore Purchases | kendi 2 katmanı | reçete |
| `JourneyScreen` Play (×5 bölge) | kendi 2 katmanı | reçete |
| `GameplayScreen` HUD geri/duraklat | kendi 2 katmanı, degradesiz | reçete, kare |
| Üst bar yeşil "+" (×2) | komple YUVARLAK PNG | reçete, kare |
| Üst bar dişli | `btn_square.png` | reçete, kare |

**Kare düğmelerin köşe oranı ölçüldü** (`hud_ref_top.png`): 138×136
kutuda yarıçap **42**, yani %30,9 — kapsül düğmenin %35,4'ünden biraz
daha az yuvarlak. Dikey tarama profilin AYNI olduğunu gösterdi: pah
(114,94,255), kaymak (144,126,255), yüz (95,72,255) → (74,54,255) yani
**×0,78 — kapsül düğmedeki `FaceBottom` ile birebir aynı sayı**.

`icon_plus.png` komple yuvarlak yeşil bir düğme görseliydi, referansta
ise yuvarlak KARE. Artı artık `UiSprites.Plus` ile prosedürel çiziliyor;
çarpının 45° döndürülmüşü DEĞİL, ayrı bir çizim (döndürülen kolların
uçları eğik kalıp ızgaraya oturmuyor).

### İlerleme çubuğu

Dolgu `Image.Type.Filled` ile kırpılıyordu. Kırpma görüntüyü DÜZ BİR
ÇİZGİYLE kesiyor: yuvarlak uçlu dokunun sağ ucu her karede kare çıkıyor,
üstelik dokunun sol köşesi çubuğun tamamına gerildiği için solda ikinci
bir açık blok beliriyordu — kullanıcının gönderdiği görüntüde ikisi de
görünüyor. Artık dolgu KIRPILMIYOR, GENİŞLİYOR.

**DERS (dolgu kırpmak değil, büyütmektir):** Bir ilerleme çubuğunun
dolgusu, uçları yuvarlak kalması gereken bir NESNEDİR.

### Sekme çubuğu — ölçüm neyi düzeltmek gerektiğini söyledi

İlk içgüdü "kart küçük, çubuk kalın" idi. Ölçüm başka şey dedi:

| ölçü | referans | önce | sonra |
|------|----------|------|-------|
| ikon boyu / çubuk | 0,588 · 0,531 | **0,437** | 0,593 |
| kart genişliği / ekran | 0,273 | 0,256 | 0,272 |
| kartın çubuk üstünde kalan payı | 0,313 | 0,301 | değişmedi |
| seçili ikon (düğme birimi) | 0,417–1,199 | 0,42–1,19 | değişmedi |

Yani kart ve yükseklik ZATEN doğruydu (7. turun ölçümü yerindeydi);
çubuk boş görünüyordu çünkü **ikonlar dörtte bir küçüktü**.

**DERS (bir ekranın "boş" görünmesi çoğu zaman boşluk değil KÜÇÜKLÜK
sorunudur):** Çubuğu inceltmek de aynı görüntüyü verirdi ama referanstan
uzaklaştırırdı.

### Eş merkezli köşe artık TÜRETİLİYOR

İç katmanların köşe oranları elle yazılmıştı (0,346 · 0,305 · 0,285).
Doğru oldukları sürece sorun yoktu — ta ki kare düğmeler için farklı bir
dış oran (%30,9) gerekene kadar. `Concentric(dış, pay)` formülü elle
yazılan üç sayıyı da yeniden üretiyor (0,344 · 0,296 · 0,285).

**DERS:** Elle yazılmış bir sayı, türetilmesi gereken bir şeyin yerine
geçtiğinde ikinci kullanımda sessizce yanlış olur.

### Doğrulama

Oynatma modunda: üst bar ("+" ve dişli), HUD (geri/duraklat), sekme
çubuğu (ölçüldü: ikon 0,593 · kart 0,272), reklam çubuğu (yuvarlak uçlu,
kırpılmamış). Journey düğmeleri yapı olarak ölçüldü: kutu 280×96,
yarıçap/boy **0,354**, paylar 0,033 · 0,142 · 0,160 — hepsi birebir.

### TUZAK TEKRARLADI

Yenile → hemen oynatmaya geç → yakala dizisi yine BAYAT kare verdi:
sekme ikonları ölçümde 87 piksel çıktı, oysa kod 118 diyordu. Sahnedeki
`RectTransform`'u loglayınca 164×118,6 göründü. Ekran görüntüsü
gecikebilir, SAYI gecikmez — şüphede kalınca kutuyu logla.

---

## 2026-08-20 (10) — DÜĞME TURU KAPANDI: HEPSİ GÖZLE DOĞRULANDI

8. ve 9. turda "gözle doğrulanmadı" diye bırakılan her ekran oynatma
modunda gezildi. Bir şey daha çıktı.

### BULUNAN: BEŞİNCİ BİR DÜĞME UYGULAMASI

Mağazanın yeşil fiyat düğmesi (`StoreScreen.PriceButton`) 8. turdaki
taramada gözden kaçmıştı — kendi dört katmanını kuruyor, kendi köşe
yarıçapını kullanıyordu. Oyunun en çok bakılan ikinci ekranında,
yanındaki her şeyden farklı duruyordu. Reçeteye alındı; altına taşan
yumuşak gölge KORUNDU (5. turda ölçülmüştü, referansta da var).

### DOĞRULANANLAR

| ekran | ne bakıldı | sonuç |
|-------|-----------|-------|
| Mağaza | teklif kartının degradesi | köşeyi kesmiyor |
| Mağaza | `$1.99` fiyat düğmesi | reçeteye geçti |
| Liderlik | seçili sekme, sıra rozeti | degrade silüete uyuyor |
| Ana ekran | WATCH AD düğmesi | reçetede |
| Günlük ödül | CLAIM | reçetede |
| Devam teklifi | Play / Add Time | reçetede |
| Kayıp paneli | Try Again | reçetede |
| Duraklat | Resume / Quit / anahtarlar | referansla örtüşüyor |

Son ikisi (sahte reklamın Skip düğmesi ve ana ekranın ödül şeridi)
zamanlamaya bağlı olduğu için ekran görüntüsü yerine YAPI ölçüldü:

* **Skip** — kutu 475×153,6, yarıçap/boy **0,354** (hedef 0,354). Katman
  payları: kabuk 0,033 · kaymak 0,142 · yüz 0,160 — üçü de ölçülen
  sabitlerle birebir.
* **Ödül şeridi** — yüzün degradesi (0,996 0,788 0,235) → (0,984 0,643
  0,039) ve **çocuk sayısı 0**: dikdörtgen yama gerçekten kalkmış,
  geçiş yüzeyin kendisinde.

### DERS

**Zamanlamaya bağlı bir ekranı yakalayamıyorsan yapıyı ölç.** Sahte
reklamın düğmesi geri sayımla açılıyor, ödül şeridi yalnız zor
bölümlerde çıkıyor; ikisini de doğru anda yakalamak için üç tur harcandı
ve üçü de ıskaladı. Katman paylarını ve yarıçabı okumak on saniye sürdü
ve aynı soruya daha kesin cevap verdi — "doğru görünüyor mu" değil,
"ölçüler tuttu mu".

### Kalan bilinçli istisnalar

* `GameplayScreen._adButton` — 5. turda kaldırılmıştı, her yerde
  `SetActive(false)`. Kod duruyor ama ekrana hiç çıkmıyor.
* Üst bardaki kare simge düğmeleri, HUD'un kare düğmeleri, güç
  düğmeleri ve sekme çubuğu — bunlar kapsül değil, kendi ölçülmüş
  aileleri var.

---

## 2026-08-20 (9) — DÜĞME TURUNUN DEVAMI: ANAHTAR VE DEGRADE YAMALARI

Kullanıcı: *"aynı şekilde düzenlemen gereken başka bir yer kaldı mı?"*
Tarandı; iki kalıp daha çıktı ve ikisi de kapatıldı.

### 1. AÇ/KAPA ANAHTARI DA İKİ KEZ YAZILMIŞTI

Düğmelerdeki hikâyenin birebir aynısı:

* `SettingsScreen` — referanstan ölçülmüş: çip yuvayı taşıyor, üç
  katmanlı, "On" yazısı koyu yeşil.
* `GameplayScreen` (duraklat paneli) — düz iki yarım, beyaz "On" yazısı,
  kabartma yok.

ÖLÇÜM (`pause_00-00-04.png`, y=380 yatay tarama) yeşil çipin profilinin
**düğmeninkiyle aynı** olduğunu gösterdi: koyu kontur (0,72,4), parlak
bilezik (75,211,48), yüz (40,191,13). Yani çip küçük bir düğme —
`MenuPage.PillBody` zaten onu veriyor, ayrı bir reçeteye gerek yok.

Yazı renkleri de ölçüldü: açıkken "On" **koyu yeşil** (28,64,25),
kapalıyken "Off" **leylak** (71,57,208). Referansın kontrastı tersine
kurulu — parlak yeşilin üstüne beyaz değil, kendinden koyu yazı.

Artık tek bir `MenuPage.Switch` var; iki ekran da onu çağırıyor.

### 2. DİKDÖRTGEN DEGRADE YAMALARI (5 yer)

`MenuSprites.FadeDown` + `Image.Type.Sliced` = **kenarlıksız, yani düpedüz
gerilmiş bir dikdörtgen**. Yuvarlak bir yüzeyin üstüne konduğunda köşeyi
düz bir çizgiyle kesiyor. Beş yerde vardı:

| yer | yüzey |
|-----|-------|
| `SettingsScreen` | anahtarın yeşil çipi |
| `StoreScreen` | teklif kartının turuncu görseli |
| `LeaderboardScreen` | seçili sekme |
| `LeaderboardScreen` | sıra rozeti |
| `HomeScreen` | "Ödüller x3" şeridi |

Beşi de `UiVerticalTint`'e çevrildi: geçiş artık ayrı bir katman değil,
yüzeyin **kendi köşe noktalarının rengi**. Silüet neyse geçiş de o; üstelik
beş çizim çağrısı da eksildi.

Bir incelik: köşe rengi **çarpar**, üstüne bindiremez. Bu yüzden yüzeyin
`color`'ı beyaza alınıp gerçek renkler geçişe taşındı, alfayla karıştırma
da (`Color.Lerp`) elle yapıldı. Çalışma anında rengi değişen yerlerde
(sekme seçimi, sıra rozeti) artık `image.color` değil `tint.Set(...)`
çağrılıyor.

### 3. SON `btn_*.png` KULLANICISI

`FakeAdScreen`'in "Skip (no reward)" düğmesi `btn_purple.png` kullanan son
yerdi; o da reçeteye geçti. Oyunda artık **görselden gelen tek bir düğme
kalmadı**.

### DERS

**"Aynı işi yapan iki kod" bir kez düzeltilince bitmiyor.** Düğmeleri
birleştirdikten sonra aynı soruyu bir kez daha sormak, anahtarı ve beş
degrade yamasını çıkardı. Bir kalıbı gördükten sonra onu ARAMAK gerekiyor
— düzeltilen örnek, aranacak şeyin tarifi oluyor.

### Doğrulama

Gerçek duraklat paneli oynatma modunda yakalandı: çip yuvayı taşıyor,
kontur/bilezik/yüz profili düğmeyle aynı, "On" yazısı koyu yeşil.
Referansla örtüşüyor.

**Gözle doğrulanmadı** (derlemesi temiz): mağaza teklif kartı, liderlik
sekmeleri ve sıra rozeti, ana ekranın ödül şeridi, sahte reklam ekranı.

### DİKKAT — `Builds/BlockOut.apk` BAYAT

Depodaki APK 20 Ağustos 21:11'de, bu turun ORTASINDA üretildi (önceki
turun `delayCall` kuyruğu alan yenilemesinden sonra bir kez daha
ateşlendi). Düğme ve anahtar değişikliklerinin bir bölümünü içermiyor —
teste vermeden önce yeniden derle.

---

## 2026-08-20 (8) — DÜĞMELER TEK REÇETEYE İNDİ

İstek: *"genel olarak herkes butonlara laf etti — ana menüdeki oynama
butonu, settingse basınca panellerde çıkan tekrar oyna butonu, bulunan
bütün butonlar… piksel sorunu olduğunu ve tarz olarak alakasız kaldığını
söylediler. Bunu da birebir referans oyundan inceleyerek daha iyi hale
getiremez miyiz?"*

### Asıl teşhis: tek bir düğme değil, DÖRT AYRI DÜĞME vardı

Oyunda aynı işi yapan dört ayrı uygulama vardı:

1. `MenuPage.PillButton` — menülerin düğmesi (koddan, üç katman)
2. `GameplayScreen.CreateGreenButton` — sonuç kartının düğmesi (koddan,
   dört katman, başka oranlarla)
3. `btn_green/red/purple.png` — duraklat, kayıp, günlük ödül, teklif ve
   ana ekran düğmeleri (görselden)
4. Ana ekranın OYNA düğmesi — zorluğa göre üç PNG arasında geçiş

"Tarz olarak alakasız" geri bildiriminin sebebi bunlardan birinin kötü
olması değil, **aynı olmamalarıydı**. Dördü de tek bir reçeteye indi.

### Ölçüm (`pause_00-00-04.png`, referansın duraklat paneli, 592×1280)

Resume düğmesi **280×113**. Dikey tarama (x=200) ve yatay tarama (y=700):

| katman | kalınlık | ölçülen renk | taban çarpanı |
|--------|----------|--------------|---------------|
| dış çizgi | 3 px | (0,84,0) | ×0,15 |
| üst pah | 8 px | (46,244,8) | ×1,19 |
| açık kaymak | 6 px | (168,253,86) | beyaza %50 |
| yüz üstü | — | (56,215,20) | ×1,00 |
| yüz altı | — | (16,170,3) | ×0,78 |
| yan duvar | 14 px | (13,130,19) | ×0,64 |
| dip eteği | 14 px | (0,85,15)→(5,49,8) | ×0,42→×0,24 |

Kırmızı Quit düğmesi aynı taramayla aynı oranları verdi. Yani tek bir
**ışık profili** var, renk onun altına giriyor. RGB'yi çarpmak tonu ve
doygunluğu koruyup yalnız parlaklığı değiştirdiği için (HSV'de V×k),
profil bir renk çarpanı olarak yazılabiliyor.

Köşe yarıçapı **40 piksel = kısa kenarın %35,4'ü**. Bizim ev oranımız
%22, tavanımız 34 birimdi.

Yazı: harf yüksekliği düğme yüksekliğinin **%28,3'ü**, genişliği
**%58,6'sı**. Bizimki %24,3 ve %63,1 — hem küçük hem yayvan.

### Yapılanlar

* **`UiVerticalTint`** (yeni): degradeyi ayrı bir katman olarak değil,
  grafiğin kendi köşe noktalarının rengi olarak yazıyor. Eskiden yüzün
  üstüne açık, altına koyu birer DİKDÖRTGEN konuyordu; ikisi de yuvarlak
  köşeyi takip etmiyor ve düğmenin köşelerinde dik kenarlar bırakıyordu.
  Bildirilen "piksel bozukluğu"nun bir bölümü buydu.
* **`UiRingLayout`** (yeni): dört katmanı kutunun yüksekliğinin oranı
  kadar, DÖRT KENARDA DA AYNI PİKSEL içeri çekiyor; yazının puntosunu da
  yükseklikten türetiyor.
* **`PillTint`** (yeni): düğmenin rengini tek çağrıyla değiştiriyor. Ana
  ekranın zorluk rengi artık üç PNG yerine bir `Color`.
* **`MenuPage.PillButton` / `PillBody`**: ölçülen reçete. Otuzdan fazla
  çağrı yeri değişmeden yeni görünüşü aldı.
* Yuvarlak panel sprite'ı **2× daha** büyüdü (256×256, yay 72, ppu 400).
  `PanelRadius/ppu = 0,18` ve `PanelBorder/ppu = 0,20` oranları
  korunduğu için kırk kadar elle ölçülmüş `SetSliceScale` değeri ve
  `UiCornerFit`'in hesabı olduğu gibi geçerli kaldı.

### ÜÇ TUZAK, ÜÇÜ DE ZAMAN YAKTI

**1. Ebeveynin ölçüsünü okuyan bileşen.** Kenar payı ilk hâlde her
katmanın üstündeydi ve ebeveyninin yüksekliğini okuyordu. Düğme
kurulurken kök kutu henüz yerleştirilmemiş oluyor ve kanvas boyunda
(1920) görünüyor; pay 0,027 × 1920 = 52 piksel çıkıyordu. Kök sonradan
206'ya inince hesap bir daha yapılmıyor, iki iç katman kutunun dışına
taşıp yok oluyordu (ölçüm: kök 511×206 iken kabuk 408×102, yüz
−102×−408). Ekranda kocaman siyah bir hap kalıyordu.

**2. `ILayoutSelfController` de kurtarmadı.** Sebebi `LayoutRebuilder`'ın
kaynağında yazıyor: *bir kutuda hiç denetleyici yoksa bütün alt ağaç
atlanıyor.* Düğmenin kökünde denetleyici yoktu. Çözüm hesabı KÖKE almak
oldu — `UiCornerFit`'in yıllardır kullandığı mekanizma.

**3. ARAÇ YALAN SÖYLÜYORDU.** Düzeltmeden sonra bile yakalanan görüntüde
düğme bir ELİPS'ti. Sırayla köşe oranı, sprite ve yerleşim suçlandı;
hiçbiri bozuk değildi. `UiCornerFit`'te **`[ExecuteAlways]` yoktu**:
oynatma modunda `OnRectTransformDimensionsChange` ateşlendiği için
yarıçap doğruydu, düzenleyicide ise hesap yalnız kurulum anındaki
(yerleştirilmemiş) ölçüyle yapılıyordu — 1080 × %35 = 382 birim.
**Oyun doğruydu, ölçen düzenek bozuktu.**

### DERSLER

* **Aynı işi yapan iki kod, er geç iki farklı tasarım olur.** Dördü de
  aynı gün ölçülmüştü; zamanla köşe oranları, puntolar ve katman sayısı
  ayrıştı. Kullanıcıya "alakasız" diye ulaşan şey buydu.
* **Kabartma, üst üste konan yamalarla yapılmaz.** Degrade yüzeyin
  kendisinin özelliğidir; ayrı bir dikdörtgen olarak konursa silüeti
  takip etmez.
* **Bir halkanın kalınlığı çapraya bağlı olamaz.** Çıpa oranı eni ve
  boyu ayrı ayrı vurur; referansın kullandığı şey bir oran değil bir
  KALINLIK.
* **Bir ölçüm beklentiden saparsa ilk soru "kod mu yanlış?" değil,
  "ölçtüğüm şey gerçekten çalışan şey mi?" olmalı.**
* **Renk değiştirmek için görsel değiştirmek pahalı bir alışkanlıktır.**
  Işık profili çarpanla türetilince renk yalnızca bir `Color` oluyor.

### Doğrulama

Yeni düğme referansla yan yana ölçüldü: yarıçap 72,9 birim / 206 =
**%35,4** (referans %35,4); yazı yüksekliği **%28,2** (referans %28,3);
yazı genişliği **%58,4** (referans %58,6). Gerçek duraklat paneli
oynatma modunda yakalandı ve referansla örtüşüyor.

`btn_green.png` / `btn_red.png` / `btn_purple.png` dosyaları DURUYOR ama
artık kullanılmıyor. Ölçüldü: 512×246, köşesi referanstan belirgin daha
köşeli ve parlaklığı sol üste doğru asimetrik olarak pişirilmiş — dokuz
dilimle esnetilince ışık lekesi de esniyor.

**Gözle doğrulanmadı** (derlemesi temiz, aynı API): günlük ödül "CLAIM",
devam teklifi "Buy", ana ekranın reklam düğmesi, kayıp panelinin "Try
Again" düğmesi.

---

## 2026-08-20 (7) — KUTLAMA DİZİLİMİ REFERANSTAN BİREBİR ÇIKARILDI

İstek: *"o konfetilerin havai fişeklerin patladığı yeri iyi dikkatlice
incele referanstakine bak sıralama birebir aynı olmalı konumları filan
tamamen aynı olmalı"*.

### Nasıl ölçüldü

`ffmpeg -vsync 0` ile kutlama sahnesi ham karelere ayrıldı (59,47 fps,
592×1280). Sıfır anı ilk harfin belirdiği kare. Her karede:

* **Patlamalar**: beyaz kıvılcım maskesi bağlı bileşenlere ayrıldı,
  2500 pikselden büyük *yeni* bir bileşen bir patlama sayıldı, ağırlık
  merkezi ekran oranına çevrildi.
* **Konfeti**: doygun ve parlak piksellerin dikey dağılımı (tepe, orta,
  dip ve toplam oran) izlendi; logo bandı (0,30–0,55) dışlandı.

### Çıkan çizelge — `WinCelebration.ReferenceBursts`

Sekiz patlama, (an, x, y):

| # | an (sn) | x | y |
|---|---------|-----|-----|
| 1 | 1,614 | 0,806 | 0,679 |
| 2 | 1,799 | 0,152 | 0,677 |
| 3 | 2,001 | 0,479 | 0,804 |
| 4 | 2,119 | 0,686 | 0,805 |
| 5 | 2,371 | 0,699 | 0,352 |
| 6 | 2,489 | 0,692 | 0,753 |
| 7 | 2,623 | 0,339 | 0,635 |
| 8 | 2,741 | 0,866 | 0,693 |

Ara süreler 0,118 ile 0,252 arasında ve düzenli **değil** — bu yüzden
sabit bir aralık yerine tablo tutuluyor. Roket izi patlamadan 0,33 sn
önce çıkıyor (`RocketRise`).

### Beş bulgu, beşi de ölçümle yakalandı

1. **Konfeti yukarıdan değil AŞAĞIDAN geliyor.** t=0,98'de renkli
   piksellerin %99,8'i alt yarıda; 0,27 sn sonra tepe ekranın üstünde.
   Bizde yağmur yukarıdan dökülüyordu, yani dizilim ters başlıyordu.
2. **Kümeli fişek prefab'ı kullanılamaz.** `FireworkYellowCluster`
   yukarı dört alt-roket atıp onları kendi hesapladığı yerlerde
   patlatıyor; ölçülen noktalara koyamıyoruz. Küme olmayan sürüm
   doğduğu yerde anında patlıyor — `FxSkinTool` ona çevrildi.
3. **Patlamalar RENKLİ DEĞİL.** Ölçüm: ortalama (229,219,234),
   doygunluk 0,13–0,28 — neredeyse beyaz, hafif leylak. Beş rengi
   sırayla atmak ekranda "renkli lekeler" veriyordu. `BurstTint` ile
   tek prefab, tek renk. Renk konfetide kalıyor.
4. **Aynı yoğunluk, yanlış dağılım.** Ölçek 1,15'te toplam oran
   referansla eşitti (%4,8'e %5,0) ama hepsi dipte bir duvar hâlinde
   duruyordu; parçalar yukarı çıkamıyordu. `localScale` parçacıkta
   HIZI da çarptığı için ölçek 2,2'ye çıkarıldı, sayı o oranda
   düşürüldü.
5. **Toplar sırayla ateşlenmeli.** Referansta oran %1,5 → %3,3 → %6,6
   diye tırmanıyor; hepsini tek karede atınca bizde t=1,15'te zaten
   %8,8 vardı. `ConfettiStep = 0,085` ile yedi top dalga hâlinde.

Ek olarak paketin konfeti topundaki `Clouds` (12 beyaz duman yumağı) ve
`Glow` (namlu parıltısı) alt sistemleri susturuldu — referansta ağızda
ne duman var ne parıltı, bizde alt kenarda yumaklar birikiyordu.
Prefab'a dokunulmuyor, kopyada söndürülüyor (`CelebrationStage.Spawn`
`mute` parametresi).

### Son ölçüm (bizim / referans, konfeti oranı)

| an | biz | referans |
|----|-----|----------|
| 1,46 | %3,6 | %2,8 |
| 1,72 | %4,9 | %1,5 |
| 2,05 | %4,0 | %3,3 |
| 2,56 | %5,8 | %6,6 |
| 3,00 | %8,1 | %5,8 |

Patlama konumları: bizim 0,805/0,690 · 0,150/0,686 · 0,479/0,805 ·
0,692/0,751 — referans 0,806/0,679 · 0,152/0,677 · 0,479/0,804 ·
0,692/0,753.

### DERSLER

* **Bir efektin YÖNÜ, yoğunluğundan çok şey anlatır.** Yukarıdan
  dökülen konfeti "kutlama sürüyor" der; aşağıdan fırlayan konfeti
  "AZ ÖNCE bir şey oldu" der. İkisi de aynı kâğıt parçaları.
* **Bir efektin RENGİ de ölçülebilir bir şeydir.** "Havai fişek
  renklidir" sezgisi doğru görünüyor ama bu oyunun referansı öyle
  yapmamış.
* **"Ekranın %5'i konfeti" tek başına yetmez.** O %5'in NEREDE olduğu
  ve ZAMAN İÇİNDE nasıl değiştiği de ölçülmeli.
* **Bir paketi kullanmak onu sahiplenmek değildir.** Seçilen
  prefab'lar `Resources/FxSkin.asset` üzerinden referansla tutuluyor,
  kopyalanmıyor; istemediğimiz alt sistemler kopyada söndürülüyor.

### DİKKAT — Epic Toon FX git'te DEĞİL

`Assets/Epic Toon FX/` 571 MB ve depoya alınmadı (LFS yok). Temiz bir
klonda kutlama, `FxSkin` boş döndüğü için kendi çizdiğimiz eski
efektlere düşer — çöker değil, sadece daha sade olur. Paketi Asset
Store'dan yeniden içeri alıp `Tools/Block Out/Efekt Kütüphanesini
Yenile` çalıştırmak yeterli.

---

## 2026-08-20 (6) — EPIC TOON FX: KUTLAMA PARÇACIKLARA GEÇTİ

Kullanıcı pakete Epic Toon FX ekledi; en önemli istek "BLOCK OUT geldikten
sonra konfetilerin patlaması, havai fişek — o sekans".

### Paket durumu (ölçüldü, tahmin değil)

- 372 materyalin **301'i zaten URP shader'ı** kullanıyor, **bozuk shader 0**.
  Yani paket URP'ye hazır geldi; `Upgrade/` klasöründeki yükseltme paketi
  bu projede gerekmedi.
- **105 materyalin 64'ünde "Soft Particles" AÇIK.** Bizim URP asset'inde
  (`Mobile_RPAsset`) derinlik dokusu KAPALI — o materyaller ekranda HİÇ
  görünmüyor. Paketin kendi belgesi bunu "invisible particles in URP"
  başlığıyla anlatıyor. Anahtar kurulumda (`FxSkinTool`) bir kez kapatılıyor.

*DERS: Bir varlığı içeri almak, onu çalışır hâle getirmez.*

### Parçacıklar üst katman kanvasın üstüne çizilemiyor

Kutlama kanvası `ScreenSpaceOverlay` ve Unity'de üst katman kanvaslar HER
kameradan sonra çizilir. Sahneye konan bir konfeti, kutlamanın siyah
perdesinin ARKASINDA kalıp tamamen kayboluyor.

Elenen üç yol: kanvası `ScreenSpaceCamera` yapmak (HUD üste çıkıyor),
kutlama sırasında öteki kanvasları gizlemek (akışa dokunuyor), perdeyi
saydamlaştırmak (perde zaten efekt okunsun diye var).

**`CelebrationStage`**: efektler oyundan 5000 birim uzakta kendi
kameralarıyla bir `RenderTexture`'a çiziliyor, kutlama kanvasındaki bir
`RawImage` o dokuyu gösteriyor. Böylece parçacıklar arayüz sırasına girip
logonun ÖNÜNDEN geçebiliyor — referansta da konfeti harflerin üstünden
akıyor. Uzak köşe, ayrı bir katman açıp oyun kamerasının maskesini
değiştirmekten ucuz: proje ayarlarına hiç dokunulmuyor.

### Üç hata, üçü de ölçümle bulundu

1. **`ParticleSystem.Simulate` alt yayıcıları çalıştırmıyor.** Düzenleme
   kipinde çekilen ilk karelerde havai fişekler BOMBOŞ çıktı ve "efekt kötü"
   diye elenecekti. Kötü olan çağrıydı: `Simulate` her çocuk için ayrı ayrı
   çağrılırsa alt sistemlerin gecikmeleri sıfırlanıyor. Çok aşamalı efektler
   ancak OYNATMA KİPİNDE değerlendirilebiliyor.
2. **RenderTexture'ın derinlik tamponu ZORUNLU.** Dokuyu `depth: 0` ile
   kurmak "bedava optimizasyon" gibi görünüyordu (parçacıklar derinlik testi
   kullanmıyor) ama URP'nin Render Graph'ı reddediyor: karede 33 hata ve
   ekranda oyunun kendi hata paneli. *Bir kaynağın "gereksiz" parçasını
   kırpmadan önce onu kimin istediğine bak.*
3. **Sahne nesnesi sızıyordu.** `CelebrationStage` kök bir nesne (dünyada
   uzakta durması gerektiği için çocuk olamıyor), dolayısıyla kutlama yok
   edilince otomatik ölmüyordu. `WinCelebration.OnDestroy` artık onu
   kapatıyor. *Bir nesneyi bilerek hiyerarşinin dışına koyduysan, ömrünü de
   sen taşırsın.*

### Dizilim (oyun modunda kare kare doğrulandı)

- Konfeti yağmuru **üç sütundan** (0,2 / 0,5 / 0,8). Tek kaynak ortada dar
  bir şerit bırakıyordu; ölçeği büyütmek parçaları da büyütüyor, oysa
  referansta konfeti İNCE. *Kaplama ile boyut aynı düğmede olmasın: çözüm
  ölçek değil, daha çok kaynak.*
- İki alt köşeden yukarı patlama.
- Altı havai fişek, 0,42 saniye arayla, beş farklı renkte. Doğum noktaları
  0,48–0,66 arasında: ilk denemede 0,70–0,90'daydılar ve patlamalar en üst
  şeride sıkışıyordu, çünkü prefab kendi içinde bir de yukarı fırlatıyor.

Eski elle çizilen konfeti/fişek/roket kodu SİLİNMEDİ: `FxSkin` boş dönerse
(paket projeden çıkarılırsa) devreye giriyor.

### Paketten alınmayanlar ve NEDEN

- **Kapıdan blok geçme efekti**: bizimki referanstan sayılarak ayarlandı
  (kapı ağzından çıkan 12-16 renkli kırıntı). Paketin `SparkleExplosion`u
  beyaz-sarı kıvılcım — referansa BENZEMİYOR. Daha "zengin" olması onu daha
  doğru yapmıyor.
- Savaş efektlerinin tamamı (mermi, el bombası, lazer, kan): bulmaca
  oyununda karşılığı yok.

Aday olarak duruyor, henüz bağlanmadı: `FrostExplosion` (buz kırılması) ve
`GoldCoinBlast` (PERFECT kartındaki jeton patlaması). İkisi de arayüz
tarafında olduğu için `CelebrationStage` benzeri bir kap gerektiriyor.

### Sesler — kısa liste

79 sesin çoğu nişancı oyunu için. Bizim ses setimiz kullanıcının kendi
seçtiği dosyalardan ayarlandı ve tutarlı; yalnız `audio_raw/README.md`de
"bulunamadı, yerine şu kondu" diye işaretlenenler için pakette daha iyi
karşılık var:

| bizdeki | şu an | paketten aday |
|---|---|---|
| `gate_advance` | sparkle (ikame) | `etfx_explosion_sparkle2` |
| `ice_crack` / `ice_tick` | 6,2 sn'lik dosyadan kesim | `etfx_explosion_frost` / `snow` |
| `power_rocket` | rocket.mp3 | `etfx_shoot_rocket` + `etfx_explosion_rocket` |
| `power_ufo` | ufo.mp3 | `etfx_shoot_energy02` |
| `block_spawn` | bubble pop | `etfx_spawn` |

Ses öznel ve kullanıcının kulağıyla ayarlanmıştı; bu yüzden DEĞİŞTİRİLMEDİ,
onay bekliyor.

---

## 2026-08-20 (5) — KAPI YÜKSEKLİĞİ, LOGO ZEMİNLERİ, TENTE GÖLGESİ

### Kapı bloğun üstünde kalıyordu — sebep PAY değil YÜKSEKLİK

Kullanıcı: "üst kısımdaki kapı kırmızı bloğun üstünde kalmış, böyle şeyler
hiçbir levelde olmamalı."

Gerçek mesh sınırları ölçüldü (14. bölüm):

    kapı plakası   y 0,820 … 0,824      (frameHeight + 0,02)
    blok gövdesi   y 0     … 0,800
    blok çıtçıtı           … 1,050

Plaka, bloğun GÖVDESİ ile ÇITÇITLARININ ARASINDA kalıyordu. Komşu bloğun
çıtçıtları plakanın üstüne taşıyor ve kapının iç kenarını örtüyordu; ekranda
kapı kısalmış, blok da doğrudan kapıya girmiş gibi görünüyordu.

Bu, 4., 6. ve 7. turda pay değerinin neden hep bir kenarı düzeltip
ötekini bozduğunu da açıklıyor: **farklı yükseklikteki iki yüzey
paralakstan farklı etkilenir**, yani aralarındaki mesafe kenara göre
değişir. Plaka blokların tepesine çıkarılınca ikisi aynı kadar kayıyor ve
pay dört kenarda da aynı olabiliyor:

    BarHeight       = brickHeight + studHeight + 0,02
    InwardOverhang  = 0,09 + brickInset          (kenar ayrımı YOK)

Ölçüldü: kapının ekrandaki iç kenarı bloğun üst kenarını 0,086 hücre
örtüyor (hedef 0,09) ve kapı artık bloğun ÜSTÜNDE çiziliyor.

**DERS:** Bir kusuru dört kez düzeltiyorsan, düzelttiğin şey kusur
değildir. Kenara göre değişen bir düzeltmeye ihtiyaç duyman, ölçtüğün
büyüklüğün yanlış olduğunun işaretidir.

### BoardCaptureTool görsel ayarları uygulamıyordu

Aynı komut iki kez çalıştırılınca farklı sayı veriyordu: blok tepesi bir
seferinde 1,05, öbür seferinde 0,515. Sebep, `VisualSettings.Current`in
statik olması — araç onu kurmuyordu, geçmiş bir işlemden dolu kalmışsa
doğru, domain reload'dan sonra boşsa yedek değerler kullanılıyordu.
Araç artık `BlockVisualConfig`i kendisi uyguluyor.

*Statik durum, doğrulama aracının en büyük tuzağı: araç ölçtüğü sistemi
kendi kurmuyorsa, ölçtüğünün oyundaki hâli olduğunu garanti edemez.*

### Logo zeminleri siluetten taşıyor

Kullanıcı: "üst kısım hâlâ kesiliyor… daha ince işçilik istiyorum."

Zeminler özgün siluete TAM kırpılıyordu. Logonun kenarına değen harflerde
(özellikle tuğla "O"nun üstünde) mor kalmıyor, harf tek başına gelince düz
kesilmiş görünüyordu. Referanstaki harflerin dört yanında da mor var —
onlar siluete kırpılmamış, üst üste binerek siluete dönüşüyorlar.

Üç değişiklik:
1. `SILHOUETTE_BLEED = 10` — zemin siluetten 10 piksel taşıyor.
2. Tuval önce PED'leniyor: logo kaynak görselin kenarlarına değdiği için
   taşma tuvalin dışına düşüyor ve tam düzeltmek istediğimiz yerde
   yeniden kesiliyordu. Dikdörtgenler en sonda özgün tuvale çevriliyor,
   yani oyunda logonun yeri ve boyu değişmiyor.
3. Zemin maskesine 16 piksellik KAPAMA: bulanık+eşik iç bükey yerlerde
   çentik bırakıyordu (tuğlanın üstünde siyah bir ısırık).

Ayrıca harf katmanında çekirdeğine DEĞMEYEN parçalar komşuya devrediliyor
("L"nin ayak köşesi öklit uzaklığına göre tuğlaya düşüyordu). Devretmek
şart, atmak değil — sahipsiz piksel zemin tarafından sentetik morla
doldurulur ve logo bozulur.

Birleştirme farkı hâlâ **tam opak piksellerde 0**; siluetin dışına
kasıtlı olarak ~12 bin piksel mor ekleniyor (gözle fark edilmiyor).

### Tentenin gölgesi kaldırıldı

34 birimlik koyu düşüş, levhanın alt kenarını yumuşatmak için konmuştu.
Ama levha zaten tentenin ARKASINDA; görünen tek yeri festonun çentikleri.
Gölge ise tentenin altından taşıp turuncu "Special Offers" kurdelesinin
üstüne düşüyordu — yumuşattığı kenar görünmüyor, kendisi görünüyordu.

*Bir öğe neyi çözdüğünü gösterebilmeli: yumuşattığı kesimin görünmediği
bir yerde duran katman, yalnız kendi varlığını gösterir.*

---

## 2026-08-20 (4) — LOGO DİLİMLEME YÖNTEMİ DEĞİŞTİ, TENTE GÖLGESİ GİTTİ

Kullanıcı kutlamayı kare kare inceleyip iki şey söyledi: "block out
kısmında üst kısım hâlâ kesiliyor… çok kesik kesik, fazla alınmış yerler,
bazı yerler eksik alınmış" ve mağazadaki tentenin gölgesi.

### Bölüntü YANLIŞ ARAÇMIŞ

Dilimleyici logoyu bir BÖLÜNTÜYE çeviriyordu: her piksel tek bir harfe.
Birleşik görüntü kusursuzdu (0 piksel fark) ama harfler tek başına
bozuktu ve sebebi yapısaldı:

- Bir harfin mor zemini komşusunun piksellerini İÇEREMEZ (onlar komşuya
  ait) → her zeminde komşusu şeklinde bir ısırık.
- Komşular arası sınır mesafeye göre çizildiği için TIRTIKLI.

Referanstaki harflerin her birinin KENDİ kapalı zemini var ve üst üste
biniyorlar. Yani onlar bir bölüntü değil, ÜST ÜSTE BİNEN KATMANLAR.

Yeni yöntem: harf başına İKİ görsel. `logo_b_back` (çekirdekten 34 piksel
şişirilip yumuşatılmış kapalı mor blob) ve `logo_b` (harfin kendisi +
kendi gölgesi). Oyunda önce BÜTÜN zeminler, sonra BÜTÜN harfler çiziliyor
(`Frame/Backs` ve `Frame/Letters`). Zemin ile harf AYNI dikdörtgeni ve
pivotu paylaşıyor, böylece tek çarpanla birlikte ölçekleniyorlar.

Birleştirme farkı yine **tam opak piksellerde 0**.

Yol boyunca üç ara adım:
- Halo yalnız "mor" piksellerden kurulunca deliklendi — harflerin koyu
  gölgeleri mor testine girmiyor.
- Boşluklar "en yakın mor pikselin rengi" ile doldurulunca IŞIN IŞIN
  çıktı. Normalleştirilmiş bulanıklık (ağırlıklı ortalama) sürekli bir
  alan veriyor. *Boşluk doldurmak bir enterpolasyon işidir.*
- Yumuşatma (bulanık + eşik) dışbükey yerlerde daraltıyor ve silüetin
  uçlarında 80 piksel açıkta kalıyordu; ikinci bir onarım geçişi eklendi.

Ayrıca "L" ile tuğla "O" arasındaki sınır dikeyden EĞİK'e çevrildi
(x = 255 − 0,144·y): tuğla döndürülmüş olduğu için dikey kesim onun sol
alt köşesini L'ye veriyordu.

### Tentenin gölgesi kaldırıldı

34 birimlik koyu düşüş, levhanın alt kenarını yumuşatmak için konmuştu.
Ama levha zaten tentenin ARKASINDA; görünen tek yeri festonun çentikleri.
Gölge ise tentenin altından taşıp turuncu "Special Offers" kurdelesinin
üstüne düşüyordu — yani yumuşattığı kenar görünmüyor, kendisi görünüyordu.

*DERS: bir öğe neyi çözdüğünü gösterebilmeli. "Sert kesimi yumuşatsın"
diye eklenen bir katman, yumuşattığı kesimin görünmediği bir yerde
duruyorsa yalnız kendi varlığını gösteriyordur.*

---

## 2026-08-20 (3) — LOGO TEMİZLİĞİ, TEKRAR HATASI, SEKME KÖŞESİ, MAĞAZA TAŞMASI

Kullanıcının bir turda verdiği dokuz maddeydi. Üçü kritikti (logo), altısı
menü/araç tarafı.

### Logo: üç ayrı kusur, üçü de dilimleyicide çözüldü

1. **Kenardaki beyazlıklar.** Ham `logo_game.png`in alt kenarının iki
   yanında kesimden kalan soluk pembe tüyler vardı (ölçüldü: 437 piksel,
   en büyük leke 174). `cutout.dewisp` yazıldı: donuk-açık VE nesnenin
   gövdesinin İÇİNDE OLMAYAN pikselleri siliyor. İkinci şart olmasa beyaz
   ikonlar ve gümüş kupa da silinirdi — aynı rengin kusur mu tasarım mı
   olduğunu belirleyen şey, nesnenin içinde mi dışında mı durduğu.

   Not: `logo_game` normal görsel boru hattına GİRMİYOR (zaten alfalı
   geldiği için `cutout.process` onu atlıyor), o yüzden temizlik
   `slice_logo.py` içinde yapılıyor ve temizlenmiş tam logo da Assets'e
   yazılıyor.

2. **"OUT!" üstten çok fazla mor getiriyordu.** İki satır arasındaki mor,
   saf mesafeye göre bölüşülüyordu ve "OUT!"un harfleri iri olduğu için
   epey yukarısı ona düşüyordu; parça 1,42 kata şişince o kütle "BLOCK"
   satırını tamamen yutuyordu. Bölüştürmeye TEK YÖNLÜ kısıt kondu: satır
   çizgisinin üstünde "OUT!" aday değil, altında üst satırın harfleri hâlâ
   aday. (İki yönlü kısıtta "B" tek başına gelirken altından düz kesilmiş
   görünüyordu.)

3. **"K" harfinin mavisi mor sanılıyordu.** Halkanın mor testi
   `mavi > kırmızı·1,15 && mavi > yeşil·1,40` idi; camgöbeği K (60,170,240)
   ikinci şartı 238'e karşı 240 ile kıl payı geçiyordu. `kırmızı > yeşil`
   eklendi. Menekşe ile camgöbeğini ayıran şey mavi değil, İKİNCİ kanal.

Birleştirme doğrulaması hâlâ **tam opak piksellerde 0 fark**.

### İkinci bölümde animasyon oynamıyordu

Kullanıcı: "bir kere yaptıktan sonra bir sonraki bölüme geçince logo direkt
hazır geliyor."

`WinCelebration` bir kez kurulup her bölümde yeniden kullanılıyor. Önceki
kutlama bittiğinde harflerin ölçeği 1'de kalıyordu; `RevealLetters`
sıfırlamayı harfin kendi rutinine bırakıyor ve o rutin ilk karesini ancak
sırası gelince çalıştırıyor. Sonuç: ilk yarım saniye bütün harfler tam boyda
duruyor, sonra tek tek "yeniden" beliriyorlardı.

**Bu turun ikinci aynı hatası** — PERFECT kartında da başlık ve düğme
gecikmeden önce tam boyda görünüyordu. Ortak kural: *gecikmeli bir
animasyonun BAŞLANGIÇ DURUMU gecikemez.*

### Seçili sekmenin köşesi — altı turdur yanlış katman düzeltiliyormuş

Kullanıcı altı turdur "seçili butonun köşeleri bozuk" diyor. 7. turda kartın
dış ve iç yüzeyinin yarıçapları eş merkezli hâle getirilmişti; o düzeltme
doğruydu ama **kusur orada değildi**. Ekrandan ölçünce görüldü:

    Face  : yuvarlak (ppuM 0,53 → köşe ~75 birim)
    Sheen : `FadeDown`, KENARLIKSIZ bir gradyan

Kenarlığı olmayan bir sprite `Sliced` çizilince dokuz dilim diye bir şey
kalmıyor — düpedüz gerilmiş bir dikdörtgen oluyor. Yani yuvarlak kartın
üstünde KARE köşeli bir ışık duruyor ve köşede kartın dışına taşıyordu.

Çözüm ışığı küçültmek değil, kartın şekline KIRPMAK: yüz artık bir `Mask`.
Ayrıca karta gölge eklendi (karttan 7 birim geniş, 6 birim aşağıda — aynı
boyda olsaydı tamamen kartın arkasında kalırdı) ve sekme ayırıcıları
beyaz-üstüne-alfadan koyu mora çevrildi.

**DERS:** Bir kusuru altı tur boyunca kovalıyorsan, düzelttiğin şeyin
GERÇEKTEN o kusur olduğunu ekrandan doğrulamak gerekiyor.

### Mağaza: taşma sınırlandı, "gölge bugu" çözüldü

Tentenin arkasındaki koyu lacivert levha ve altındaki 34 birimlik gölge
`content` altındaydı, yani içerikle birlikte KAYIYORDU. Aşağı esnetildiğinde
levha tentenin altından çıkıp koca bir mavi bant, gölge de ayrı bir şerit
hâline geliyordu. İkisi de `root`a taşındı ve tenteden ÖNCE kuruluyor: artık
kıpırdamıyorlar.

Taşmanın kendisi için `UiScrollOvershoot` yazıldı (GameKit). Unity'nin esnek
kipinde taşma mesafesi AYARLANAMAZ — `elasticity` yalnız geri dönüş süresini
verir, açılma miktarını motorun içine gömülü sabit bir katsayı belirler.
Bileşen `LateUpdate`te içerik konumunu kırpıyor (ScrollRect konumu kendi
`LateUpdate`inde yazdığı için `Update` işe yaramazdı). Sınır 160 birim.

Buna bağlı olarak bölüm zeminlerinin taşma payı 700'den 260'a indi. O 700,
taşmanın ne kadar olacağı bilinmediği için seçilmişti; taşma sınırlanınca
gerek kalmadı. *Bilinmeyeni bol payla kapatmak, bilinmezliği korur.*

### DEV konsolu

Liste telefonda kaydırılamıyordu: IMGUI'nin `ScrollView`i içerikten
sürüklenemez ve Unity'nin varsayılan çubuğu 15 piksel — bu ölçü çözünürlükle
BÜYÜMÜYOR. İki şey yapıldı: konsol kendi `GUISkin`ini kurup çubuğu ve
tutamağı 26 birime çıkardı (çubuk parametreden, TUTAMAK ise her zaman
`GUI.skin.verticalScrollbarThumb`tan geldiği için ikisi ayrı ayrı
ayarlanmalı), ve içerik parmakla sürüklenebilir hâle geldi. Sürükleme
kaydırma sayıldığı anda bırakma olayı yutuluyor — yoksa kaydırmaya çalışan
parmak, kaldırdığı satırın düğmesine basmış oluyordu.

Panel ölçüleri de %5 büyütüldü.

### Ayar anahtarları — SORUN YOK (ölçüldü)

"Çalışmıyor" duyumu doğrulanamadı. Oyun modunda dördü de tek tek test
edildi; her biri kendi anahtarını yazıyor, çapraz etki yok:

    Sounds  OFF→false ON→true
    Music   OFF→false ON→true
    Haptics OFF→false ON→true
    Notif   OFF→false ON→true

Işın testi de düğmelerin en üstteki hedef olduğunu gösteriyor. Tek gözlem:
sayfa geçiş animasyonu sürerken (`Show` çağrıldığı KARE) ışınlar hedefi
bulamıyor — geçiş bitince normale dönüyor. Kullanıcı geçişin ortasında
dokunduysa gördüğü şey bu olabilir.

### Sanat dosyalarında "piksel bozulması" taraması

Bütün `Art/UI` sprite'ları tarandı. Kopuk leke yalnız iki dosyada var
(`badge_reward`, `icon_hand`) ve ikisi de tasarımın parçası görünüyor.
"Donuk-açık artık" ölçütü ise gümüş kupayı, beyaz ikonları ve fotoğraf
avatarlarını da yakaladığı için tek başına kullanılamaz. Yani dosyalarda
yaygın bir bozulma YOK; kullanıcının duyduğu şey ekrandaki çizim kusurları
(sekme köşesi, mağaza gölgesi) olmalı — ikisi de bu turda kapandı.

---

## 2026-08-20 (2) — KAZANMA DİZİLİMİ: LOGO HARF HARF, PERFECT KARTI YENİDEN

Kullanıcı: *"leveli kazanınca block out yazısının gelişinde ve oradaki
efektlerin çıkışında problem var… ve sonrasındaki perfect yazısı gold
gelişi o panel… bu 2 şeyi çok iyi hale getirmeliyiz, bunlar sürekli
gördüğümüz şeyler ve çok benzer kalitede olmak zorunda."*

Referans videodan **59,47 fps'lik ham kareler** çıkarıldı (`ffmpeg -vsync 0`,
592×1280). Videodaki on kutlamanın ikisi bağımsız ölçüldü ve aynı sayıları
verdi.

### 1. Logo artık HARF HARF geliyor — çünkü varlık üretildi

Bu, 4., 5. ve 6. turda üç kez denenip üç kez terk edilmişti. Teşhis her
seferinde doğruydu: logo TEK bir PNG, harfler ORTAK bir mor konturla bağlı;
bir dilimi küçültünce ekranda harf değil KÜÇÜLTÜLMÜŞ BİR DİKDÖRTGEN
görünüyordu. Yanlış olan son adımdı — "harf başına görsel yok" denip
durulmuştu, oysa görsel ÜRETİLEBİLİRDİ.

**`tools/slice_logo.py` (yeni)** logoyu altı parçaya bölüyor:

1. Harfler renklerinden bulunuyor (çekirdek).
2. Kalan her opak piksel EN YAKIN çekirdeğe veriliyor (Voronoi) — mor kontur
   ve gölgeler kendiliğinden sahiplerine dağılıyor.
3. Her parçaya kendi mor konturu geri büyütülüyor.
4. Parçalar 2 piksel bindiriliyor.

Betik sonucu SAYARAK doğruluyor: **tam opak piksellerde birleştirme farkı 0.**
Yani altı parça üst üste konunca sonuç aslının aynısı.

Üç tuzak yol boyunca çıktı ve üçü de betiğin içinde yazılı:
- "yalnız en büyük bağlı parça" → ünlem işareti elenip "K"ya düştü.
- halka rengini "harf olmayan en yakın piksel"den almak → tuğla "O"nun
  halkası kendi koyu gölgesini komşusunun üstüne bastı; ekranda tuğlanın
  solundan üçgen bir dilim kesilmiş gibi duruyordu.
- parçaları tam bitişik koymak → ondalıklı piksel konumunda aradan siyah
  sızdı (tek sütun 35 birim koyu).

**Ölçülen zamanlama** (`WinCelebration`):

| an | olay |
|---|---|
| 0,000 | B belirir; her harf 0 → 1,40 kat → 1 (0,38 sn) |
| 0,105 | L (aralık her harfte aynı) |
| 0,210 | tuğla O |
| 0,315 | C |
| 0,420 | K |
| 0,525 | "OUT!" — 0 → **1,42 kat** → 1 (0,72 sn) |
| 0,940 | konfeti + fişek başlar |

"OUT!" tepe noktasında "BLOCK" satırını tamamen örtüyor; referansta da öyle.
Kalan beş harf o sırada kıpırdamıyor (kırmızı B'nin piksel sayısı sabit).

**Yakalanan hata:** tepe oranı önce 1,65 ölçülmüştü. O sayı "altın piksel
sayısı en yüksek SATIRIN genişliği"nden geliyordu ve kelime büyüdükçe o satır
harfin başka yerine denk düştüğü için şişiyordu. Kutu ölçülünce 1,42. Fark
masum değildi: 1,65 ile bizim "OUT!" ekranın %103'üne çıkıp kesilecekti.

Logo bittikten sonraki tüm-logo vuruşu KALDIRILDI — vurgu ikiye bölünüyordu.

### 2. PERFECT kartı

**Kart ölçeklenerek gelmiyor, ANINDA orada.** Referansta 313. karede yok,
314'te tam boyunda. Canlanan şey içindekiler:

| an | olay |
|---|---|
| 0,000 | ödül yığını **2,00 kattan** küçülür (0,135 sn) |
| 0,151 | "PERFECT!" kartın üst kenarının altındaki bir noktadan yukarı açılır (0,25 sn) |
| 0,252 | "Continue" 0 → 1,09 → 1 (0,27 sn) |
| 0,639 | ödül sayısı plakası 0 → 1,24 → 1 (0,15 sn) |

Başlığın dönme noktası da ÖLÇÜLDÜ: üst ve alt kenarın ayrı eğrileri
çözülünce y = 314 çıkıyor, yani başlığın altı. Ortadan ölçeklenen bir başlık
"belirdi" der; buradaki hareket "kart onu yukarı itti" der.

### Kartta düzeltilen dört şey

1. **Koyu kenar ekranda hiç görünmüyormuş.** 7. turda ölçülüp yazılan
   18 piksellik kenar, `RefreshResult` içindeki
   `_resultCard.color = difficultyTint` tarafından her açılışta eziliyordu ve
   normal bölümlerde o renk tam olarak kartın yüzüyle aynıydı. Artık zorluk
   rengi YÜZE gidiyor, kenar bantları ondan TÜRETİLİYOR (`TintCard`).
2. **Kenar tek bant değil dört.** Ölçülen kesit: dış kenar → koyu oluk →
   iç kenar → parlak iç çizgi → yüz.
3. **"Level N" plakası kaldırıldı — referansta yok.** 7. turdaki ölçüm tek
   bir yatay kesite bakmış, harflerin konturunun birleştiği satırı plaka
   sanmıştı. Yoğun tarama boşlukları gösteriyor.
4. **Yeşil düğme hap değil, yuvarlak köşeli dikdörtgen** — prosedürel
   kuruldu (kenar + yüz + dip + parlaklık).

### "PERFECT!" dizgisi

Aynı genişliğe ölçeklenip yan yana konunca referansın harfleri belirgin daha
uzun ve kalın çıkıyordu: **referansın yazı tipi Baloo2'den DAR.** Aynı
yüksekliğe getirince kelime taşıyor. Çözüm: harf aralığı kısıldı (−16),
gövde SDF ile şişirildi (`_FaceDilate` 0,12) ve kalan fark yatay ölçekle
kapatıldı (0,70). Sonuç: genişlik %56,6 (hedef %56,2), yükseklik %7,97
(hedef %8,05), tam ortalı.

Altın artık DÜZ DEĞİL: tepede (254,211,7), dipte (250,152,0). `color` ile
`colorGradient` TMP'de ÇARPILDIĞI için taban renk beyaza çekildi — altın
kalsaydı geçişin açık ucu da turuncuya dönerdi.

### Yıldızlar

Referansta yığının çevresinde aynı anda ortalama 31 minik yıldız var ve
sayı 4 ile 32 arasında geziniyor — yani sönüp yanıyorlar. Bizde dört tane
büyük ve HAREKETSİZ yıldız vardı. `SparkleField` (yeni) 40 ölçülmüş konumu
altın orandan türeyen evrelerle yakıp söndürüyor; ışın çelengi de geri geldi
(`UiSprites.Burst` — kodda anlatılıyordu ama ekranda yoktu).

Kart konfetisi KALDIRILDI: referansın kartında yok, bir önceki adımda zaten
konfeti var. Gürültüden sonra sakin kart ödülü öne çıkarıyor.

### Bordo Grand Games açılış ekranı kaldırıldı

Kullanıcının isteği. `BuildStudio` silinmedi, yalnız kurulmuyor ve
`Sequence` `_studio` null olduğunda dalı atlıyor — böylece o ekranın
1,7 saniyesi de akıştan düşüyor (yalnız alfayı sıfırlamak, görünmez bir
gecikme bırakırdı).

### DOĞRULANMAYAN TEK ŞEY

Zamanlamalar düzenleme modunda ÇALIŞMAZ (tween yok). Ölçümler referanstan,
yerleşim ve ara kareler Unity'de yakalandı; ama akan hareketi APK'de ya da
oynatma modunda GÖRMEK gerekiyor.

---

## 2026-08-20 — SESLER BAĞLANDI

Kullanıcı ham ses dosyalarını yükledi. **31 anahtarın tamamı dolu, müzik
çalıyor.** Ayrıntı: `audio_raw/README.md` (eşleme tablosu) ve
`tools/import_audio.py` (kesim noktaları + gerekçeler).

### Ham dosya oyuna hazır DEĞİLDİR — üç sorun ölçüldü

| sorun | örnek | sonucu |
|---|---|---|
| baştaki sessizlik | `curtain.mp3` 479 ms | oyunda GECİKME |
| uzun kuyruk / çoklu olay | `ice cracking.mp3` 6,2 sn'de 12 çatlama | her hamlede çalınamaz |
| seviye farkı | −0,3 dB … −19,3 dB | biri bağırıyor, diğeri duyulmuyor |

`tools/import_audio.py` (yeni, `import_art.py`nin kardeşi) kesiyor,
perde/hız ayarlıyor, iki uca sönüş koyuyor ve −3 dBFS'e getiriyor.
Ham dosyalar `audio_raw/` altında — `art_raw/` ile aynı düzen.

### ÜÇ GERÇEK HATA ÇIKTI

1. **Müziği kimse başlatmıyordu.** `AudioService.PlayMusic` yazılmış,
   ayarlara ve duraklat anahtarına bağlanmıştı ama projede onu ÇAĞIRAN tek
   satır yoktu. Dosya konsa bile müzik hiç çalmayacaktı.
   *DERS: bir sistemin "hazır" olması BAĞLI olması demek değil.*
2. **Aynı ses üst üste yığılıyordu.** Her emilimde tahtadaki BÜTÜN buzlu
   blokların sayacı azalıyor; beş buzlu blokta `IceDecremented` aynı karede
   beş kez yayınlanıyor ve beş özdeş klip toplanıyordu. Sentezlenmiş cılız
   bliplerle duyulmuyordu, gerçek kliplerle ilk denemede duyuldu.
   Yeni `PlayOnce`: aynı anahtar 60 ms içinde bir kez.
   *DERS: olay sayısı = ses sayısı DEĞİLDİR.*
3. **Müzik döngüsü dikişliydi.** Parça son 3 saniyede SÖNÜYOR; ham hâliyle
   döngüye girse her 55 saniyede bir kaybolup geri gelirdi. Solan kuyruk
   kesildi, parçanın başı sonuna çapraz geçişle bindirildi (bas RMS 0,195 /
   son 0,166 — dikiş duyulmuyor). Ayrıca sessizdi (−27 dB), −16 LUFS'a çekildi.

### Bulunamayan sesler ne oldu

- kapı → `gate_advance` için `sparkle`, `gate_done` için pes `pop`
- tutma → `pick_up` için `bubble` (kullanıcının önerisi)
- kaybetme → seviye atlama sesinin TERSİ, bir tık pes
- menü müziği → tek parça; eksik anahtar oynanış parçasına düşüyor, parça
  değişmediği için menü ↔ oyun geçişinde müzik baştan başlamıyor

### Ayrıca

Ana ekranda kalp ve jeton sayaçlarının koyu plakası ikonların KUTUSUNUN
içinden başlıyordu; kalp dikdörtgen olmadığı için (ortada daralıyor)
arada zemin görünüyordu. Plakalar artık ikonun ORTASINDAN başlıyor.

### SONRAKİ ADIM: PAKETLEME

Kullanıcı "artık bu oyunu paketleyelim" dedi. Yayından önce:
- `DeviceErrorOverlay.cs:33` → `Enabled = false`
- APK alınıp ses dengesi CİHAZDA dinlenmeli (seviyeler ölçüyle ayarlandı,
  kulakla değil)
- 7. turun APK'de görülmesi gerekenleri (kapı ışığı, buz, roket/UFO,
  reklam sonrası düğme) aynı derlemede kontrol et

---

## 2026-08-20 — 7. TURUN EKİ KAPANDI (7/7)

Kullanıcının aynı gün gelen ikinci listesi. Ayrıntılar
`docs/APK-BULGULARI-5.md` içindeki **"7. TUR — EK"** bölümünde.

1. **KRİTİK: alt kapılar blokların üstünü örtüyordu.** T65'in yan etkisi —
   `InwardOverhang` dört kenara birden uygulanmıştı, oysa paralaks üst ve alt
   kenarda TERS yönde çalışıyor. Pay artık kenara göre: üst 0,235, alt −0,055,
   yanlar 0,09.
2. **Kapı ağzı ışığı eklendi.** 6. turun ölçümü doğruydu ama yanlış yere
   bakmıştı: ışık kapıda değil, kapı ile bloğun TEMAS ÇİZGİSİNDE.
3. **PERFECT paneli** — `panel_card` sprite'ının içindeki ikinci çerçeve
   gitti (kart prosedürel), hale sıcak altın oldu, parıltılar eklendi.
4. **Buz çatlaması her hücrede** — tek merkez yerine hücre hücre; kırıntı
   sayısı 3×2 blokta 11 → 42.
5. **"CAN YOK" bugu** — durumu boyayan kodun geri dönüşü yoktu; reklamdan
   sonra düğme yalan söylüyordu. Yazı ayrıca Türkçeydi.
6. **Roket ve UFO** artık gerçekten roket ve UFO (eskiden beyaz bir küp ve
   bir ışık sütunu).
7. **Ses listesi** zaten hazırdı: `docs/audio-brief.md`.

### İKİ YENİ ARAÇ TUZAĞI — ikisi de zaman yaktı

**1. Yakalama aracı MAGENTA çiziyordu.** Unity düzenleyicide derlenmemiş bir
shader varyantı magenta "bekliyor" rengiyle çiziliyor ve `Camera.Render()`
onu beklemiyor. Ölçüldü: aynı sahne üç kez yakalandı, üçünde de tam 18 476
magenta piksel — yani geçici değil takılı kalmış bir durum, ve olmayan bir
"ızgara bugu" olarak teşhis edildi. `ShaderUtil.allowAsyncCompilation = false`
AYRI bir çağrıda yapılmalı; aynı çağrının içinde kapatmak işe yaramıyor.
`BoardCaptureTool` artık `[InitializeOnLoad]` ile her domain reload sonrası
kapatıyor.

**2. `Unity_RunCommand` "başarılı" dese de proje derlenmemiş olabilir.**
Komut yalnız KENDİ parçacığını derliyor. `GameplayScreen` iki satırlık bir
isim alanı hatasıyla derlenmedi ve üç yakalama boyunca hiçbir değişiklik
görünmedi; komutlar hep "Command executed successfully" dedi.
**Kural: her düzenlemeden sonra `Unity_GetConsoleLogs`.** Pratik hile:
doğrulama komutunda YENİ eklenen sembole dokun (`MenuSprites.Sparkle != null`
gibi) — derleme bayatsa komut sessizce geçmek yerine patlar.

### APK'DE GÖRÜLMESİ GEREKENLER (bu ekten)

- Kapı ağzı ışığının ZAMANLAMASI (şeridin kendisi karede doğrulandı).
- Buz çatlamasının şiddeti (sayılar doğrulandı, hareket değil).
- Roket uçuşu ve UFO inişi (sprite'lar karede doğrulandı, hareket değil).
- "CAN YOK" düzeltmesi: reklam izlenip düğmenin geri döndüğü görülmeli.

---

## 2026-08-19 — 7. TUR KAPANDI (26/26)

**Tek doğruluk kaynağı: `docs/APK-BULGULARI-5.md`.** Kullanıcının 26 maddesi
(M45 … V70) oraya döküldü; her maddede referans ÖLÇÜMÜ, tespit edilen sebep,
yapılan iş ve doğrulama var.

### Kapatılanlar — başlıklar

- **M45/M46 — taşma.** `GameKit.UI.UiTextFit`: `UiKit.CreateLabel` artık her
  etikete takıyor, sığmayan yazının puntosunu küçültüyor (yalnız genişlik,
  taban %45). Ekran ekran düzeltme değil, YAPISAL çözüm.
- **N47-N53 — mağaza.** Branda levhası içeriğe taşındı (kaydırınca gidiyor),
  kurdele dilleri iki uçta/altın/ince, jeton kutularının kırmızı tabanı
  132 → 176 birim + gölge, taşıyıcı noktaları büyüdü, %90 flaması ve Starter
  Pack yerleşimi ortalandı.
- **O54 — liderlik.** Kesim artık bir KENARA denk geliyor: kürsünün altında
  26 birim düz mor çizgi, altında 7 birimlik açık mor çerçeve, sonra liste.
- **P55/P56 — ana ekran + sekme çubuğu.** OYNA düğmesindeki yazı çifti
  düğmenin GÖRÜNEN yüzüne ortalandı; seçili sekme kartının köşe bozulması
  iki sebepten (doku çözünürlüğü + eş merkezli olmayan yarıçaplar) düzeldi.
- **Q57/Q58 — profil.** Kalem rozeti üç katman + gölge, kalem üç renk
  (kontur/gövde/grafit uç); ayraç çizgisi koyu tarafa geçti.
- **R59/R60 — ödül.** "Level 20" kendi açık mor plakasına girdi, ödül sayısı
  siyah elipsten koyu mor yuvarlak dikdörtgene döndü, kart referans ölçüsüne
  çekildi; konfeti İKİ KAT büyüdü ve palet referans oranlarına ağırlıklandı.
- **S61-S64 — duraklat.** Süre hapı duraklatınca yukarı kayıyor (referansta da
  yok), Quit kartın dudağından çıktı, çarpı gölge+koyu bilezik aldı, ayar
  simgeleri koyu mor oldu.
- **T65/T66 — kapılar.** Üst kapının altındaki duvar payı PARALAKS kaynaklıydı
  (`InwardOverhang` 0,09 → 0,235); çerçevenin dış köşe yarıçapı yanlış kod
  yolundan 0,84 hücre çıkıyordu, referansta 0,19.
- **U67/U68 — HUD.** Saat simgesi mor ve içi boş (`UiSprites.ClockFace`),
  süre 42 → 58 punto, seviye ve jeton 42 punto, süre hapı referans ölçüsünde.
- **V69/V70 — perde ve ok blokları.** Parıltılar katmanlı örneklemeyle eşit
  dağıldı, dört uçlu yıldız oldu ve beyazlıktan kurtuldu; ok blokları köşe
  rengi kazanıp gerçek bir kabartmaya döndü.

### YENİ DOĞRULAMA ARAÇLARI (sonraki oturum bunları kullansın)

| araç | işi |
|---|---|
| `BoardCaptureTool.Capture(bölümYolu, ad)` | tahtayı oynatma kipine girmeden PNG'ye çeker |
| `UiOverflowAudit.Report(ad, kök)` | kutusuna sığmayan yazıları listeler, sayı döndürür |
| `MenuShell.CreateTabBarPreview(kanvas, sekme)` | alt sekme çubuğu, oturumsuz |
| `GameplayScreen.CreatePausePreview(kanvas)` | duraklat paneli, oturumsuz |
| `GameplayScreen.CreateHudPreview(kanvas)` | üst şerit, oturumsuz |

Hepsi `UNITY_EDITOR` altında; kanvası ÇAĞIRAN vermeli
(`UiKit.CreateCanvas` edit modunda `DontDestroyOnLoad` yüzünden patlıyor).

### TUZAK — MCP komutu ESKİ DERLEMEYİ ölçebilir

`Unity_RunCommand` kendi parçacığını O ANKİ derlemeye karşı derliyor. Kaynak
dosyayı düzenledikten hemen sonra çağrılan ölçüm komutu, değişiklikten ÖNCEKİ
kodu ölçebiliyor — bu turda perde parıltısı üç kez "değişmedi" göründü.
**Kural: düzenlemeden sonra ÖNCE yalnız `AssetDatabase.Refresh` yapan bir
komut, SONRA ölçen komut.** (5. turdaki "oynatma modunda derlemez" dersinin
edit modundaki kardeşi.)

Ayrıca: `ViewKit.ClearCache()` ve `UiSprites.ClearCache()` çağrılmazsa statik
materyal/sprite önbelleği domain reload'a kadar bayat kalıyor.

### AÇIK KALANLAR (6. turdan devam)

1. **SES VE MÜZİK HİÇ YOK** — `Resources/AudioSkin.asset` boş, projede tek
   ses dosyası yok. Kullanıcıya soruldu, cevap bekleniyor.
2. Titreşim gerçek cihazda doğrulanmadı.
3. 27 bölümün tasarımı referanstan değil, yaklaşık.
4. `DeviceErrorOverlay.cs:33` → `Enabled = true`, yayından önce `false`.
5. **S61'in animasyonu APK'de görülmeli** — tween'ler oynatma kipi istiyor;
   yerleşim ve kod yolu doğrulandı, hareketin kendisi doğrulanmadı.
6. R60'ın konfetisi de aynı sebeple yalnız sayı olarak doğrulandı.

---

## 2026-08-19 — 6. TUR KAPANDI (8/8)

Kullanıcının üçüncü geri bildirim turu. Sekiz madde de kapandı; ayrıntılı
ölçümler commit mesajlarında (`976a079`, `601c48b`).

### Kapatılanlar

1. **Kapı görseli baştan kuruldu.** Kullanıcı bizimkini ve olması gerekeni
   yan yana gönderdi. Referansın dikey kesiti KESİNTİSİZ bir degrade;
   bizimki üç düz bant + sert basamaklardı, çünkü bar mesh'i **hiç köşe
   rengi yazmıyordu** (`BlockOut/Brick` onu çarpan olarak kullanıyor).
   Üç düzeltme denemesi yetmedi; doğru cevap yapıyı kurmaktı: kapı bir
   prizma değil, **düz iki katlı plaka** — çepeçevre ince koyu kenar
   (%2,5 hücre), içinde dikey degradeli parlak yüz. Okun etrafına da
   referanstaki koyu halka eklendi.
2. **Beyaz parlama kaldırıldı.** 20 fps'te 280 kare: blok kapıya girerken
   kapı HİÇ değişmiyor. Bizdeki beyaza patlama + %14 büyüme + üç katlık
   hale, 4. turda ölçülmeden eklenmişti. Asıl olan blok parçalarının iri
   olması (hücrenin %13-18'i); sayı 38 → 15.
3. **Sönmedeki bozulma:** okun koyu halkası boyanmıyor, son karede ok
   biçimli koyu bir leke bırakıyordu.
4. **PERFECT / FAILED kartları ölçüldü** ve — önemlisi — **editörde
   görülebilir hâle geldi.** Yerleşim `ApplyResultLayout`'a çıkarıldı,
   `GameplayScreen.CreateResultPreview` (yalnız `UNITY_EDITOR`) eklendi.
   Puntolarımız referansın kabaca yarısıydı (88/58/46/44 → 132/84/76/66);
   Continue kartın %76'sıydı, referansta %55.
5. **Jeton yığını:** `Art/UI/coin_pile_1..5.png` zaten projede duruyordu
   (mağaza kullanıyor). Ödül kartı on tek jetonu elle istifliyordu.
6. **Işık yansıması:** referansta ışın YOK, yumuşak hale var. Yeni
   `UiSprites.Radial`.
7. **BLOCKOUT harf harf gelişi terk edildi.** Ön koşulu harf başına ayrı
   görsel; elimizde tek PNG var. Logo tek parça, ölçek+alfa ile oturuyor.
8. **Mağaza:** festonun altındaki bant tamamen kaldırıldı (46 → 0).

### Bu turun dersleri

- **Bir malzemenin beklediği veriyi vermezsen sessizce düzleşir.** Kapı,
  bloklarla aynı shader'ı kullanıyordu ama aynı veriyi (köşe rengi)
  vermiyordu.
- **Biçimi taklit etmek yerine YAPIYI kur.** Üç tur boyunca referansa
  benzemeyen bir gövdeyi gölgelendirerek benzetmeye çalıştım.
- **Bir tepki EKLEMEK, tepkiyi iyileştirmek değildir.** "Olay iki taraflı
  olmalı" mantıklı bir cümle olduğu için sorgulanmadı.
- **Ölçmek saymaktan ibaret değil — NE OLMADIĞINI da görmek.** "Işınlar
  kaç tane" diye ölçtüm; sorulacak ilk soru "ışın var mı" idi.
- **İkinci kez aynı duvara çarpınca duvarı kabul et.** Harf animasyonu
  için gereken varlık yok; kötü taklit yerine sade ve temiz çözüm.
- **Elde olanı aramak, üretmekten ucuzdur.** "Bu varlık yok" varsayımı
  bir kez kurulunca üstüne kod yazılıyor ve varsayım kodun içinde donuyor.
- **Vector3 → Vector2 sessiz bir veri kaybıdır.** Ok halkası tek çizgiye
  inmişti; derleyici hata vermez, çalışma anında patlamaz, mesh boş çıkar.
- **Doğrulanamayan kod üçüncü kez bozulur.** Sonuç kartı üç turdur
  düzeltiliyordu çünkü yalnız APK'de görülebiliyordu.

### Yeni araçlar (sonraki oturum bunları kullansın)

- `GameplayScreen.CreateResultPreview(canvasTransform, won, reward,
  levelName, title)` — oturum olmadan sonuç kartını kurar. Kanvası
  ÇAĞIRAN vermeli (`UiKit.CreateCanvas` edit modunda `DontDestroyOnLoad`
  yüzünden patlıyor) ve kök RectTransform olmalı.
- `StoreScreen.Build(canvasTransform)` — mağaza edit modunda kurulabiliyor.
- Tahta yakalama: boş sahne nesnesi + `LevelLoader.Parse` →
  `LevelModel.Build` → `BoardBuilder.Build` + kapalı kamera (80° eğim,
  27° FOV, ikili aramayla mesafe) + `RenderTexture`.

### AÇIK KALANLAR (öncelik sırasıyla)

1. **SES VE MÜZİK HİÇ YOK.** `Resources/AudioSkin.asset` → `entries: []`,
   projede tek bir `.wav/.mp3/.ogg` yok. `AudioService` tamamen bağlı ama
   çalacak klip yok — oyun baştan sona SESSİZ. Kullanıcıya soruldu,
   cevabı beklendi.
2. **Titreşim gerçek cihazda doğrulanmadı** (manifest izni eklendi).
3. **27 bölümün tasarımı referanstan değil, yaklaşık.**
4. **`DeviceErrorOverlay.cs:33` → `Enabled = true`**, yayından önce
   `false` yapılacak.
5. **A2 (4. tur)** — yazı/panel boyutları: kullanıcının kendi işi.
6. Animasyonlar (yığın gelişi, sönme eğrisi, parçalanma, kutlama akışı)
   edit modunda oynatılamıyor; ancak APK'de görülebilir.

---

## 2026-08-19 — 5. TUR KAPANDI (17/17)

**Tek doğruluk kaynağı: `docs/APK-BULGULARI-4.md`.** Kullanıcının ikinci
geri bildirim turundaki 17 madde oraya döküldü; her maddede referans ÖLÇÜMÜ,
tespit edilen sebep ve doğrulama sayıları var.

Bu turda kapatılanlar, kabaca:
- **Kapılar:** duvarı tamamen kaplıyor (altında mor şerit kalmıyor), ok
  referans oranına (%34 × %19, basık) çekildi, sönerken uzun çizgiler
  gitti (saydam → opak), yutma parlaması katkılı haleyle güçlendi.
- **Tahta:** kullanılmayan bölgeler artık gerçekten KESİLİYOR — çerçeve
  maskenin silüetini izliyor, ızgara da öyle; kenara dayanan boşluklar hiç
  çizilmiyor.
- **Blok içinde blok:** iç katman artık saplamalı gerçek bir blok.
- **Buz:** her hamlede çatlama efekti (eskiden yalnız ses ve titreşim vardı).
- **Yön blokları:** baş/boy oranı "New Item Unlocked!" karesinden ölçüldü.
- **Kutlama:** harfler yapboz gibi değil, alfa ile geliyor; roket/patlama
  sayıları sayıldı.
- **PERFECT / BAŞARISIZ:** ışın yelpazesi, ölçülen panel renkleri ve
  BAŞARISIZ kartının ölçeği.
- **Reklam düğmesi kaldırıldı** — referansta yok, devam etme jetonla.
- **Yardımcılar:** adet rozeti %50 → %31, fiyat kapsülü ters renkteydi.
- **Mağaza:** kurdele şeritleri, jeton kapsülü, sola yaslı paket adları,
  gölgeler, sabit başlığın kalınlığı.

### Bu turun en pahalı dersi

**Unity oynatma modundayken kaydedilen betikler DERLENMEZ.** Doğrulama
turunun bir kısmı, değişikliklerden ÖNCEKİ derlemeyi ölçmüştü: ölçüm doğru
çalışıyordu, ölçtüğü şey yanlıştı. Ölçmeden önce `EditorApplication.isPlaying`
kontrol et. Tahta yakalamaları oynatma modu gerektirmiyor — boş bir sahne
nesnesi altında `LevelLoader.Parse` → `LevelModel.Build` → `BoardBuilder.Build`
+ kapalı bir kamera + `RenderTexture` ile edit modunda kurulabiliyor
(kamera `FitCamera`'nın kopyası: 80° eğim, 27° FOV, ikili aramayla mesafe).
Arayüz ekranları da aynı yolla yakalanabiliyor: `StoreScreen.Build(canvas)`.

---

## 2026-08-19 — 4. TUR KAPANDI (43/44)

**Tek doğruluk kaynağı: `docs/APK-BULGULARI-3.md`.** Kullanıcının 44 maddesi
oraya numaralı biçimde döküldü; her kapanan maddenin altında NE YAPILDI +
NASIL DOĞRULANDI ve ölçümler var.

**Durum: 43 kapalı / 1 kullanıcıda (A2 — metin ve panel boyutlarını kullanıcı
kendisi düzenleyecek).**

### Bu turun dört genel dersi

1. **Ekrandaki gri, dosyadaki gri değildir.** Tahtanın ızgara çizgileri
   görüntüleyicide açık gri görünüyordu ve bir saat "ızgara materyali bozuk"
   diye arandı. Piksel piksel ölçüldüğünde o bölgede 90'ın üstünde tek bir
   nötr piksel bile yoktu. **Karanlık bölgeleri gözle yargılama, say.**
2. **Kırpma "biraz eksik"i "hiç yok"a çevirir.** Alt sekme çubuğundaki seçili
   sekmenin adı hiç görünmüyordu: kutu 32,83 birim, yazının en küçük satır
   yüksekliği 33,6 ve `overflowMode = Truncate` satırı TAMAMEN atıyordu
   (`text.bounds` extents = 0,0,0). Renk, alfa, sıra, materyal hepsi doğruydu.
3. **Bir süsün hatası ana akışı kilitlememeli.** Kutlama coroutine'inde bir
   NullReference çıkınca `done` geri çağrısı hiç çalışmadı ve PERFECT kartı
   ASLA açılmadı — oyuncu bölümü bitiriyor, ekranda siyah perde kalıyordu.
4. **"Dışarısı" nerede başlıyor?** Üreteç makinesi tahtanın kenarından
   hesaplanıp "dışarı" konuyordu; arada 0,52 hücrelik çerçeve var ve makine
   onun içine girip kapının arkasında kalıyordu.

### En büyük iş: blok silüetleri (H27/H28/H29)

`brickInset: 0` ve keskin köşeli, hücre hücre örülen gövde yüzünden yan yana
duran aynı renkten iki blok TEK KÜTLE görünüyordu. Yeni `BrickSilhouette`
polyomino'nun çevre çizgisini kenar takibiyle çıkarıyor, gönye ile kaydırıyor,
her 90° köşeyi (dışbükey ve içbükey) yay ile yuvarlıyor ve üst yüzü kulak
kırpma ile üçgenliyor. Tutma konturu artık ölçek değil SABİT kalınlık.

Yeni ortak araç: **`PrismMeshBuilder`** (yuvarlak köşeli, pahlı alçak prizma).
Kapı barı, üreteç parçaları ve perde aynı biçimi paylaşıyor.

### Haptik — cihazda doğrulanmadı

Kök sebep bulundu: **Android manifestinde `VIBRATE` izni yoktu.** Oyun
`android.os.Vibrator`ü JNI ile çağırdığı için Unity izni kendiliğinden
eklemiyor; izin olmayınca `SecurityException` try/catch içinde yutuluyor ve
tek bir titreşim bile çalışmıyor. `AndroidManifestPatcher` üretilen manifeste
izni ekliyor.

> **SONRAKİ OTURUMDA İLK BAKILACAK YER:** Kullanıcının yeni APK'sinde titreşim
> hâlâ yoksa, derlemenin ürettiği `unityLibrary/src/main/AndroidManifest.xml`
> dosyasında `<uses-permission android:name="android.permission.VIBRATE"/>`
> satırının olup olmadığına bak.

### Doğrulama yöntemi — iki yeni tuzak

1. **`UiCaptureTool.Capture` 3B TAHTA İÇİN KULLANILMAZ.** O araç ORTOGRAFİK
   bir kamera kuruyor ve sahnedeki her şeyi onunla çiziyor; tahta yandan
   bakılmış ince bir şerit olarak çıkıyor. Tahta kareleri için `Camera.main`
   kendi `RenderTexture`ına render edilmeli (bkz. bu oturumun komutları).
2. **`EditorApplication.Step()` çağrıldığı komutun İÇİNDE etki etmez** (bu
   ders 3. turda da yazılmıştı, yine unutuldu). Adımlayan komut ile gözleyen
   komut AYRI olmalı. Ayrıca adımlarken `Time.unscaledDeltaTime` çok küçük
   kalıyor: `Show = 2.2f` gibi biriken süreler stepli modda neredeyse hiç
   ilerlemiyor, bu yüzden kutlama "bitmiyor" gibi görünüyor — gerçek oyunda
   sorun yok.

Ayrıca: **statik alanlar RunCommand'ın derlemesinde bayat kalabiliyor.**
`MenuShell.Instance` ve `AppRoot.Current` play modunda `null` dönüyordu; aynı
nesneler `Object.FindFirstObjectByType<T>()` ile bulunuyor. Doğrulamada statik
kapı değil, sahneden arama kullan.

### Yeni dosyalar

| dosya | işi |
|---|---|
| `Runtime/View/BrickSilhouette.cs` | polyomino çevre çizgisi + yuvarlatma + kulak kırpma |
| `Runtime/View/PrismMeshBuilder.cs` | yuvarlak köşeli alçak prizma (kapı, makine, perde) |
| `Editor/ProjectSetup/AndroidManifestPatcher.cs` | manifeste VIBRATE izni |
| `Editor/ProjectSetup/AppIconTool.cs` | ikonu projeye alıp Android yuvalarına bağlar |
| `tools/make_icon.py`, `tools/make_icon_layers.py` | ikon ve uyarlanabilir katmanları |

### Açık kalanlar

1. **A2** — metin/panel boyutları, kullanıcı yapacak.
2. **Haptik cihazda doğrulanmadı** (yukarı bak).
3. **27 bölümde süre yetmiyor** — 3. turdan devrediyor, tasarım kararı.
   Tam tablo: `APK-BULGULARI-2.md` → "3. TUR" → bölüm D.
4. **`DeviceErrorOverlay.Enabled = true`** (`Runtime/Flow/DeviceErrorOverlay.cs`
   satır 33) — yayına çıkarken `false`.
5. **Ses**: her şey `SfxSynth` ile sentezleniyor, müzik yok.

### Uyarılar (değişmedi)

- **Level editörüne DOKUNMA** (`Scripts/Editor/LevelEditor/`) ve
  `Runtime/Debug/`, `GameKit/Runtime/DevTools/` — kullanıcının paralel işi.
- **`git add -A` KULLANMA.** Yalnız kendi dosyalarını tek tek stage'le.

---

## 2026-08-18 — GELİŞTİRİCİ KONSOLU YENİDEN YAZILDI + KİTE TAŞINDI

Eski gizli "dev menu" (4 sekme, tek ızgarada 50 bölüm, arama yok) yerini
altı sekmeli bir KONSOLA bıraktı.

**Kabuk artık GameKit'te**, çünkü sonraki oyunlarda da kullanılacak:
`Assets/GameKit/Runtime/DevTools/` → `DevConsole` (gizli açılış, siyah panel,
S/M/L boyut, üst/alt yaslama, rozete indirme, sekme çubuğu),
`DevUi` (tema + parçalar), `DevPage` (genişletme noktası), `DevLog` (log
akışı), hazır `DevLogPage` + `DevSystemPage`. Kurulum örneği
`Assets/GameKit/README.md` içinde.

**Oyuna özel sekmeler** `Assets/_Project/Scripts/Runtime/Debug/` altında:
`DevMenu.cs` (kurulum + `IDevConsoleHost`: duraklatma / girdi kilidi / başlık
bağlamı), `DevLevelsPage`, `DevPlayPage`, `DevSavePage`, `DevDataPage`,
`DevLevelIndex`.

**Panel ekranı KAPLAMIYOR (SRDebugger tarzı):** varsayılan S = ekranın sol üst
köşesine yaslı, ~%56 genişlik × %46 yükseklik. Başlıktaki düğmeler: S/M/L boyut,
köşe değiştir (sol üst → sağ üst → sağ alt → sol alt), "—" ile köşedeki DEV
rozetine indir, "✕" kapat. Rozetteyken oyun akıyor ve oynanıyor (doğrulandı:
sayaç 176.7 → 175.9), panel açılınca tekrar duruyor. Tercih PlayerPrefs'te.

**Tema:** Unity konsolu tonları — siyah/koyu gri yüzeyler, tek vurgu rengi
(seçili = mavi, iyi = yeşil, uyarı = sarı, hata = kırmızı). Neon yeşil/mavi
karışımı ilk sürümden vazgeçildi: her şey vurguluyken hiçbir şey vurgulanmıyor.

**Sadeleştirme:** mekanik süzgeç çipleri (arama kutusu zaten yapıyor), oyuncu
kimliği/günlük ödül satırları, yardımcı verme düğmelerinin OYUN'daki kopyası,
"analitiği loga dök", cihaz künyesindeki işlemci/güvenli alan satırları ve alt
şeritteki tetik ipucu kaldırıldı.

**Dar panelde düzen dersi:** IMGUI kaydırma alanı içindeki en geniş öğeye göre
genişler; tek bir uzun düğme satırı bile sağa hizalı bütün değerleri görünmez
alana kaydırıyordu. `DevUi.ScrollBegin` artık içeriği panel genişliğinde bir
sütuna kilitliyor (`AreaWidth`), etiketler de kısaltıldı.

- **BÖLÜM**: arama kutusu (numara / renk / mekanik / zorluk, TR+EN),
  numara tuş takımı, süzgeç çipleri (geçildi, kilitli, zor+, buz, perde,
  makine, katman, şekil, eksen), sıralama (sıra/zorluk/deneme/oran) ve satır
  başına açılan detay (kayıt + analitik + "buraya kadar aç", "kaydı sil").
  Dizin 50 bölümü bir kez ayrıştırıp önbelleğe alıyor (`DevLevelIndex`,
  kareye yayılmış eşyordam).
- **OYUN**: canlı bölüm künyesi + **sonuç panelini zorla**: KAZAN 1★ / 2★ /
  3★ PERFECT / olduğu gibi ve KAYBETTİR. Yıldız, süre ayarlanıp oturumun
  GERÇEK bitiş yolu çağrılarak üretiliyor (`GameSession.DebugForceWin(stars)`,
  `DebugForceLose()`). Ayrıca canlı tahta dökümü: renk başına kalan blok,
  kapı yön/renk/buz sayacı, perde ve makine kuyruğu.
- **KAYIT**: cüzdan/can/∞ can/reklamsız/envanter/ilerleme okuma + düzenleme.
  Yıkıcı düğmeler iki adımlı onay istiyor.
- **VERİ**: bölüm bölüm başlangıç/geçme/kayıp tablosu, oran çubuğu, sıralama.
- **LOG**: cihaz üstü Unity konsolu (Application.logMessageReceived) +
  konsolda yapılan her işlem "İŞLEM" satırı olarak aynı akışta.
- **SİSTEM**: cihaz künyesi, FPS sondası, zaman ölçeği (x0.25…x2), konsol
  ayarları, gizli tetik açıklaması.

Tetik artık **iki üst köşeden biri** (5 dokunuş / 2 sn), editörde F8 + Esc.
Panel açıkken oyun DURUYOR, oynanış girdisi (`PointerInputService.Blocked`)
ve uGUI olayları kilitli — eskiden panele basan parmak arkadaki bloğu
sürüklüyordu.

Doğrulama: play mode'da altı sekme de çizdirildi (hata yok), duraklatma
sayaç üzerinden ölçüldü (180.00 sn sabit), 2★ zorlaması `LastStars=2,
LastReward=20` verdi ve PERFECT paneli açıldı, KAYBETTİR `Lost + 0:00` verdi,
rozet–panel geçişi duraklat/kilit durumlarıyla birlikte ölçüldü.

---

## 2026-08-18 — 2. TUR BAŞLADI (56 madde)

**Tek doğruluk kaynağı: `docs/APK-BULGULARI-2.md`.** Kullanıcının ikinci test
turundaki 56 maddesi oraya numaralı biçimde döküldü; her kapanan maddenin
altında NE YAPILDI + NASIL DOĞRULANDI yazıyor.

**Durum: 23 kapalı / 33 açık.**

Kapananlar: 3, 4, 5, 24, 30 (köşe yarıçapı + başlık konturu) · 8, 9, 10, 11,
13, 15, 16, 17, 18, 19 (Liderlik) · 52 (KRİTİK: kapı–blok boyutu) ·
41, 42, 43, 44, 45, 46, 47 (oyun içi HUD + yardımcı düğmeleri).

### Bu turda çıkan üç genel ders

1. **Yarıçap sabit piksel değil ORANDIR.** Referansta her yüzeyde yarıçap kısa
   kenarın **%22'si**, ~34 pikselde tavanlı. Bizde sabit ~36 piksel vardı ve
   kısa kutularda hap üretiyordu. `GameKit.UI.UiCornerFit` bunu uyguluyor.
   Elle yarıçap veren yerler `UiKit.SetSliceScale` ile hesabı KAPATIYOR.
2. **Bir sayıyı on yere elle yazarsan on ayrı sayı olur.** Başlık konturu
   dokuz çağrı yerinde 0.22-0.55 arası yazılmıştı; referansta tek oran var.
   `UiKit.TitleOutlineWidth` ev değeri oldu.
3. **Kapının KONUMU bulmacanın geometrisidir.** Kapı daraltırken "merkezi
   koru" sezgiseli level_030'u çözülemez hâle getirdi; "özgün başlangıcı
   koru" 51 kapının hepsinde sorunsuz.

### Doğrulama döngüsü (tekrarlanabilir)

Unity MCP ile: play moduna gir → `MenuShell.Instance.Show(key)` →
ekranı yerine oturt → `UiCaptureTool.CaptureOf<T>(ad)` → PNG'yi oku/ölç.

> **TUZAK:** Ekranı yerine oturturken `anchoredPosition = Vector2.zero`
> YAZMA. Menü ekranlarının kökü asimetrik offset'li (tabanı 210 birim aşağı
> taşıyor); anchoredPosition'ı sıfırlamak dikdörtgeni ORTALAR ve ekranı 105
> birim yukarı kaydırır — başlık ekranın üstünden taşıp kırpılır. Yalnız X
> sıfırlanmalı (oyunun kendi `SlideSwap`'i de öyle yapıyor). Bu tuzak bir
> oturum içinde "oyunda hata var" sanılmasına yol açtı; hata araçtaydı.

---

## 2026-08-17 OTURUM SONU — BLOCKER ÇÖZÜLDÜ, YENİ TUR BEKLİYOR

Ayrıntılı kayıt: **`docs/APK-BULGULARI.md`** (21 kullanıcı bulgusu + 6 yeni,
27 maddenin 26'sı kapalı).

### Durum: build çalışıyor

**18. madde ÇÖZÜLDÜ** — bölümlerin gelmeme sebebi ne Newtonsoft ne AOT'muş,
**motor kodu kırpma**ymış. Cihazdaki hata kartı sebebi iki satırla söyledi:

```
Can't add component because class 'MeshCollider' doesn't exist!
ArgumentNullException: Value cannot be null. Parameter name: shader
```

1. **Shader'lar derlemeye girmiyordu.** Görsel katmanın tamamı
   `Shader.Find(...)` ile materyal kuruyor; bir shader "Always Included
   Shaders"da değilse ve gönderilen bir materyalden referanslı değilse
   derlemede YOKTUR. Aranan beş shader'dan dördü listede yoktu; eklendi.
2. **`MeshCollider` kırpılmıştı.** `CreatePrimitive` her zaman çarpıştırıcı
   ekler; oyunda fizik yok, kod onu hemen siliyordu — ama kırpıcı sınıfı
   tamamen atınca nesne HİÇ kurulamıyordu. `ViewKit.CreateShape` ile 12 çağrı
   yeri fiziğe hiç dokunmayacak şekilde geçirildi.

### Bu oturumda kapanan maddeler
4 (günlük ödül) · 7 (ana ekran) · 9 (Yolculuk) · 12 (Mağaza tentesi) ·
13 (Ayarlar) · 16 (Liderlik) · 18 (bölüm yükleme) · 21 (Profil) ·
22 (ödül şeridi) · 23 (alt sekme çubuğu) · 24 (cihaz hata katı) ·
25 (sayı biçimi) · 26 (kaybolan can ödülü) · 27 (arayüz tasarım sistemi).

---

## SONRAKİ OTURUM — ~90 MADDELİK YENİ GERİ BİLDİRİM

Kullanıcı yeni bir test turu yaptı ve **yaklaşık 90 madde** geri bildirim
topladı. Hepsi ayrı bir oturumda ele alınacak (2026-08-17 gecesi kararı).

**Bu oturuma başlarken yapılacak ilk iş:** maddeleri kullanıcıdan alıp
`docs/APK-BULGULARI.md`'nin yaptığı gibi tek bir dosyaya numaralı biçimde
dökmek ve öncelik gruplarına ayırmak. 21 maddelik ilk turda bu yöntem
işe yaradı: her madde kapanırken NE yapıldığı ve NASIL doğrulandığı yazıldı,
böylece hiçbiri iki kez açılmadı.

**Önceki turdan devreden, hâlâ açık üç şey:**

1. **Uygulama ikonu (1. madde) — KULLANICIDAN DOSYA BEKLİYOR.**
   `art_raw/icon_app.png` (kare, ≥512px). Gelmeden Android ikon yuvaları
   bağlanamaz; build alınır ama ikon Unity'nin varsayılanı olur.
2. **Koleksiyon görseli (5. madde)** — açık kitap + albüm sekme ikonu
   (istemler `docs/art-prompts.md` §11). Şimdilik yerinde sandık duruyor.
3. **Ses** — her şey `SfxSynth` ile kodla sentezleniyor, **müzik hiç yok.**
   Sahnedeki eksik `AudioListener` bu oturumda eklendi (APK tamamen sessiz
   çıkıyordu), yani artık duyuluyor; ama duyulan şey sentetik efektlerden
   ibaret. Ya ücretsiz lisanslı kütüphane (Kenney / freesound CC0) ya da
   mevcut sentezin katmanlandırılması.

**Denge (ölçüldü, dokunulmadı):** editördeki doğrulama aracı 50 bölümde
0 bozuk / 2 temiz / 48 uyarılı diyor. İçinde gerçek bir sorun var:
**3 bölümde buz bütçesi tutmuyor** — o kapıların buzu hiç kırılamıyor, kapı
fiilen dekor. Ayrıca 42/50'de "açılışta tek hamle" uyarısı var; oran o kadar
yüksek ki önce ÖLÇÜTÜN kendisine bakmak lazım.

**Yayına çıkarken kapatılacak:** `DeviceErrorOverlay.Enabled` → `false`
(test sürümünde açık; yakalanmamış her hatayı ekrana basıyor).

---

## BU OTURUMDA ÖĞRENİLEN, TEKRARLANMAMASI GEREKENLER

1. **`Shader.Find` editörde HER ZAMAN çalışır.** Orada bütün shader'lar
   yüklüdür ve null dönmez. Derlemede çalışması, shader'ın derlemeye
   GİRDİĞİNİ ayrıca garanti etmene bağlı (Always Included Shaders).
2. **Kullanmadığın modüle DOKUNMA.** Kırpıcı "kimse kullanmıyorsa at" diye
   çalışır; kodun geçici olarak dokunduğu her modül ya derlemede tutulmak
   zorunda kalır ya da orada patlar. `CreatePrimitive` + hemen silinen
   çarpıştırıcı tam olarak bu tuzaktı.
3. **Yakalama yöntemi hatayı GİZLEYEBİLİR.** `CaptureOf<T>` yalnız o
   bileşenin kanvasını çiziyor; alt sekme çubuğu ve sayfa zemini o karede
   yok. İki gerçek hata (Liderlik'te siyah şerit, birleşik kürsüler) ilk tam
   ekran `Capture()` çağrısında bir anda ortaya çıktı. **Kıyas için
   `Capture(ad)` kullan.**
4. **`UiSprites.Circle` düz bir `Image` olarak kutuya GERİLİR** → kutu kare
   değilse daire elips olur. Projede dört yerde vardı. `UiKit.CreateIcon`
   en-boy oranını korur; daire çizen her yerde o kullanılmalı.
5. **Ölçerken KATMANI ayır.** Plastik bir yüzeyin üst kenarında 30 piksel
   içinde dört ayrı renk olabiliyor; tek yatay örnekleme üçünü birbirine
   karıştırıp yanlış "gövde rengi" verdi. Dikey tarama ayırdı.
6. **Kaynağı değil SONUCU ölç.** Sprite'ın taban rengi ekranda göründüğü
   renk değil (üstüne parlaklık rampası biniyor). Referansla kıyaslanacak
   şey ekrandaki piksel; taban renk ondan geri hesaplanmalı.
7. **`Start`'ların sırası TANIMSIZDIR.** "Benden sonra kurulan" bir şeyi
   beklemenin tek güvenli yolu bir kare geçirmek.
8. **ScriptableObject kendi adıyla aynı dosyada olmak zorunda.** Değilse
   `CreateAsset` yalnız UYARI verip script referansı boş bir varlık üretir;
   varlık diske yazılır, `Resources.Load` bulur gibi görünür, veri hiç geri
   okunamaz.
9. **`EditorApplication.Step()` çağrıldığı komutun İÇİNDE etki etmez.**
   Adımlar komut dönünce işleniyor; "adımla ve yakala" tek komutta ESKİ
   kareyi yakalar. Bir komut hareket ettirir, SONRAKİ komut gözlemler.
10. **Cihazda `Debug.LogError` hiçbir yere gitmez.** Teşhis aracı, teşhis
    edilecek şeyden BAĞIMSIZ olmalı — bir ekranın hatasını o ekrana
    yazdırırsan, ekran hiç kurulamadığında elin boş kalır.
    (`DeviceErrorOverlay` bu yüzden var ve blocker'ı tek turda çözdü.)

---

## ARAYÜZ TASARIM SİSTEMİ (2026-08-17 eklendi)

`Tools > Block Out > Arayüz Tasarımı` (`Ctrl+Shift+U`) — panelleri ELLE
düzenlemek için. Prefab'a çevrilmedi (ölçü gerekçeleri kod yorumlarında
duruyor); bunun yerine FARK katmanı:

1. Play moduna gir, ekranı aç → **Referans al**
2. Hiyerarşide normal Unity araçlarıyla oynat (konum, boyut, renk, punto)
3. **Değişiklikleri kaydet** → yalnız farklar `Resources/UiLayout.asset`'e

Kod hâlâ tek kaynak; elle dokunulmamış her şey kod değişince yeni değeri
kendiliğinden alır. Ekran başına "Sıfırla" var. Çalışma anında karede sıfır
iş: ekran kurulduktan sonra bir kez hiyerarşi geziliyor, kayıtlı düzeltme
yoksa ilk satırda dönülüyor.

**Kapsam dışı:** yeni öğe ekleme ve hiyerarşi değiştirme hâlâ kod işi.

---

## Proje nedir

Unity 6.3 URP Mobile ile **Block Out! – Color Sort Puzzle** (Grand Games)
klonu. Portfolyo için; GitHub'da yayınlanacak, mağazaya konmayacak. Reklam ve
mağaza **test modunda** çalışacak (gerçek para yok) ama akışların tamamı
gerçek olacak.

**Hedef: kusursuz birebir benzerlik ve çok iyi bir his.** "Çalışıyor" yetmez.

---

## Referans kaynağı — ÖNEMLİ

> **DÜZELTME (2026-08-16):** Aşağıdaki `Downloads` yolu EKSİK. Arayüz için
> asıl klasör:
> `C:/Users/CPN12/OneDrive/Masaüstü/Block Out! Videos/`
> İçinde `Block Out! menus,powerups,vs.mp4` (menüler + yardımcılar) ve
> **`Game over .mp4`** (kaybetme paneli — henüz hiç kıyaslanmadı) var.
> İkisi de 384x832; oynanış videoları 592x1280. **Her iki referansın da
> en-boy oranı ~2.165**, bizim yakalama 1080x1920 = 1.78 — yani referansın
> ölçekleme modelini (yükseklik mi genişlik mi kilitli) videodan ayırt etmek
> MÜMKÜN DEĞİL, iki video da aynı cihaz oranında. Dikey konumlar yükseklik
> oranı olarak doğrudan aktarılabilir (en-boydan bağımsız), yatayda ise
> mevcut kural (`referans_piksel / referans_genişlik`) korunuyor.

Referans videolar `C:/Users/CPN12/Downloads/` altında:

- `Block Out Color Sort Puzzle Levels 1-20 Walkthrough.mp4` ← **arayüzün tamamı burada**
- `Block Out! - Color Sort Puzzle Level 21-22-...-30 Solution Walkthrough.mp4`
- `Block Out! - Color Sort Puzzle Level 31-...-40 Solution Walkthrough.mp4`
- `Block Out! Level 41-...-50 Solution Walkthrough.mp4`

> `anamenu.mp4` ve `BlattodaMenuAnimasyon.mp4` **başka bir projeye ait**, Block
> Out değil. Karıştırma.

**Yöntem hatası olarak öğrenildi:** Bu videolar uzun süre yalnız *bölüm
çıkarmak* için kullanıldı; arayüz ve his için hiç taranmadı. Kullanıcının
"birebir değil" eleştirisinin kaynağı buydu. Bir şeyin referansta nasıl
göründüğünü merak ettiğinde **kullanıcıya sorma, videodan çıkar.**

ffmpeg sistemde yok; taşınabilir olanı şöyle bulunur:

```bash
python -m pip install imageio-ffmpeg          # bir kez
FF=$(python -c "import imageio_ffmpeg;print(imageio_ffmpeg.get_ffmpeg_exe())")

# Her 6 saniyede bir kare (genel tarama)
"$FF" -loglevel error -i "<video>" -vf "fps=1/6,scale=296:-1" out/w%03d.png

# Belirli bir anı tam çözünürlükte
"$FF" -loglevel error -ss 00:02:30 -i "<video>" -frames:v 1 out/kare.png
```

---

## Şu an ne bitmiş durumda

**Oynanış çekirdeği** — 50 bölüm, hepsi çözücüyle doğrulanmış. Polyomino
bloklar, renk katmanları, buz (blok + kapı), perdeler, kapı renk kuyrukları,
yönlü bloklar, blok üreteçleri, çok bölgeli tahtalar.

**Menü** — ana ekran, Yolculuk, Mağaza, Liderlik, Koleksiyon, Ayarlar.
Kullanıcı ana ekranı onayladı.

**Mağaza (2026-08-10 yeniden kuruldu)** — referanstan ölçülerek birebir:
çizgili tente + festonlu kenar, "Özel Teklifler / Paketler / Jetonlar" bölümleri
(her birinin kendi zemin rengi içerikle KAYIYOR), 5 paket kartı + köşe kurdelesi,
3×2 jeton ızgarası, geri yükle. Renkler ve yükseklikler
`Block Out! menus,powerups,vs.mp4` 08-14. saniyelerden piksel örneklenerek alındı.
Görseller geldi, kesildi ve `UiSkin`'e bağlandı: `coin_pile_1..6`,
`pack_1..5`, `icon_noads`, `icon_infinite` (52 görsel, `check_art.py` temiz).

**Mağaza BİTTİ** — canlı oyunda sekmeden açılıp doğrulandı:
- Jeton yığınları HER KUTUDA AYNI BOYUTTA. Önce kademeli büyütülmüştü;
  referans karesinde altı yığının da genişliği 226-229 piksel çıktı, artan tek
  şey jeton yoğunluğu (%21 → %35).
- "Özel Teklifler" artık GERÇEK bir taşıyıcı (`OfferCarousel`): üç sayfa,
  parmakla kayar, en yakın sayfaya oturur, noktalar sayfayı gösterir. Dikey
  jestler dıştaki listeye elle aktarılıyor — yoksa kart üstünde aşağı kaydırmak
  sayfayı dondururdu. **İkinci ve üçüncü teklifin içeriği UYDURMA**; referansta
  üç nokta görünüyor ama yalnız birincisi kaydedilmiş.
- İndirim rozeti flama biçiminde (`MenuSprites.Pennant`).
- Kaydırma alanı sekme çubuğunun ALTINA uzatıldı; kabuk ekranları 0.105'te
  bitirdiği için çubuğun üstünde ana ekranın manzarası sızıyordu.

**Yolculuk (2026-08-10 yeniden kuruldu)** — referanstan ölçülerek
(`menus,powerups,vs.mp4` 24-42. saniyeler):
- Zemin ölçüldü: DÜZ koyu lacivert `#171C4E`. Eskiden büyük yumuşak daireler +
  %10 yıldızlar vardı, ekran sisli duruyordu. Yıldızlar %5.5'e kısıldı,
  daireler kaldırıldı.
- Kilometre taşı artık TEK kapsül (solda Seviye/N, sağda ödül), eskiden iki
  ayrı parçaydı. Renk `#4C3BE2` — eskisi neredeyse siyahtı.
- Boru ekran genişliğinin %3.0'ı, bölme çizgili, sol tarafında ışık şeridi.
  Boru kapsüllerin ÜSTÜNDEN, bölge çemberlerinin ALTINDAN geçer — bu yüzden
  kurulum sırası: kapsüller → boru → bölgeler.
- Bölge çemberi artık gerçek DAİRE + parlak halka (`MenuSprites.Ring`);
  eskiden elipsti. Aralık etiketi üst kenara, yeşil düğme içine biniyor.
- "Üst"/"Alt" borunun uçlarında, sağ kenarda değil.
- **İlerleme çubuğu ve "sonraki ödüle N bölüm" satırı KALDIRILDI** — referansta
  yok. Yerine referanstaki mevcut seviye işareti (pembe rozet) kondu.
- Ödüller ONU DA referanstan okundu: 10, 15, 25, 30, 45, 50, 60, 75, 80, 90
  (24./31./40./41./42. saniyeler). "d" dakika, "s" saat.
- **BEŞİNCİ BÖLGE bulundu:** "Buz Kurtarma" (sv 71-100). İlk kuruluşta
  atlanmıştı çünkü videoda yalnız KİLİTLİ hâliyle görünüyor.
- **Boru bir İLERLEME GÖSTERGESİ.** Oyuncunun seviyesine kadar ışıklı, ötesi
  koyu (`#121137`). İlk kuruluşta baştan sona parlaktı — elimdeki karelerin
  hepsi oyuncunun ulaştığı bölgelerdeydi, yukarı kaydırılan kareye bakınca
  ortaya çıktı. Bizim eklediğimiz ayrı ilerleme çubuğu tam da bu yüzden
  fazlalıkmış.
- **Ulaşılmamış satır SOLDURULMUYOR.** 75/80/90 satırları alınmışlarla birebir
  aynı; tek fark yeşil tikin olmaması. Eski kod onları soluklaştırıyordu.
- Kilitli bölge: görselin üstüne açık gri-mavi daire perdesi + asma kilit +
  "Seviye N" etiketi; yeşil düğme gizli. Doygunluk düşürmek materyal işi
  olduğu için perde yöntemi kullanıldı.
- Eksik görsel: `region_5` (buz temalı çember) ve altın `icon_lock`
  — bkz. `docs/art-prompts.md` §8.

`ShopSprites` → **`MenuSprites`** olarak yeniden adlandırıldı: kapsül, ∞ ve
halka artık iki ekranda birden kullanılıyor, isim yanıltıcı olmuştu.

**Kalan dört menü ekranı (2026-08-10) — referanstan yeniden kuruldu**
Ortak kabuk `MenuPage.cs`'e alındı (degrade başlık bandı, koyu gövde, kırmızı
çarpı, kapsül düğme). Beş ekran ayrı ayrı kurulduğu için beşi de birbirinden
biraz farklıydı ve hiçbiri referansa uymuyordu.
- **Settings** (kare 54-56): tek açık mor kart, dört satır, her satırda
  [Off|On] İKİLİ anahtar (etkin olan yeşil). Eskiden tek yazı vardı.
  Zil simgesi yok — `UiSprites`'ta çizilmedi, o satır simgesiz.
- **Profile** (kare 57-60): kimlik kartı (avatar + kalem rozeti + ad + Level),
  "General Stats", 2×4 koyu kutu. Simgeler kutunun ÜST KENARINA biniyor.
  Sekiz sayaçtan yalnız "First Try Wins" gerçek; kalanı "-" (referans da
  kazanılmamışı "-" gösteriyor). Kalem bir yazı DEĞİL, eğik kapsül.
- **Collection** (kare 43-45): referansta ekranın TAMAMI bir görsel +
  "Unlock Collection at Level 95!". Eski tahmini ızgara kaldırıldı.
- **Leaderboard** (kare 16-22): üç sekme tek yuvada, geri sayım rozeti,
  podyum 2-1-3 (ortadaki yüksek), sıralı satırlar, en altta oyuncunun
  KENDİ satırı yeşil ve sabit.

**Arayüz dili İNGİLİZCE'ye geçti** (2026-08-10, kullanıcı kararı). Tasarım
referansla birebir; yalnız metinler İngilizce, fiyatlar USD. Kod yorumları
Türkçe kalıyor.

**Oyun içi HUD** — referans videodan ölçülerek yeniden kuruldu (iki satır:
jeton/bölüm, sonra yeniden başlat/süre/duraklat). Yardımcılar yeşil kare +
kırmızı adet rozeti.

**Sistemler** — combo, günlük ödül, sahte IAP, ödüllü reklam, yerel analitik,
gizli geliştirici menüsü (F8 / sol üst köşeye 5 dokunuş), editör bölüm
tarayıcı (`Tools > Block Out > Bölüm Tarayıcı`, `Ctrl+Shift+L`).

**Görsel boru hattı** — `art_raw/` içine ham PNG at, `python tools/import_art.py`
işler, `python tools/check_art.py` kesim hasarını tarar (şu an 39/39 temiz).

---

## Level editörü YENİDEN KURULDU — sekmeli kabuk (2026-08-17)

Referans: kullanıcının kendi **Ant Level Editor**'ü
(`OneDrive/Masaüstü/Block Out! Videos/level editör örnek.mp4`, 32 sn).
Amaç yalnız bu oyun değil: bu kabuk **sonraki oyunlarda da kullanılacak şablon**.

**Tarz:** tek `EditorWindow` + IMGUI, üç katmanlı sabit kabuk (üst şerit ·
sekme çubuğu · durum çubuğu) ve sekmeye göre değişen tek gövde. Oyuna özel olan
YALNIZCA Tahta sekmesinin içi; kalan yedi sekme herhangi bir ızgara-tabanlı
bölüm setiyle çalışır.

Dosyalar (`Assets/_Project/Scripts/Editor/LevelEditor/`):

| dosya | işi |
|---|---|
| `LevelEditorWindow.cs` | kabuk: durum, sekme yönlendirme, geri alma, dosya, durum çubuğu |
| `.TopBar.cs` | Yeni/Aç/Önceki/Sonraki/Undo/Redo/Kaydet/Play/Önbellek + palet noktaları |
| `.Board.cs` | üç kolon (denetçi · tuval · tarayıcı) + cetveller + alt kapı şeridi |
| `.Panels.cs` | sol denetçi: araçlar, fırça, seçili nesne |
| `.Tabs.cs` | Pano · Galeri · Doğrula · Çözüm · Araçlar · Referans · Kılavuz |
| `.Input.cs` | fare/klavye, seçim, düzenleme |
| `LevelLibrary.cs` | setin tek kaynağı: sıra (katalogdan), durum, minyatür önbelleği |
| `LevelThumbnail.cs` | `LevelData` → `Texture2D` (galeri kartları) |

**Yeni kazanımlar**
- **Galeri:** 50 bölüm minyatür olarak, durum renkli çerçeveyle. Sağ tık menüsü
  (aç / play / doğrula / çoğalt / sil).
- **Pano:** sağlık çubuğu, sayaç kartları, "dikkat gerekenler" listesi, zorluk ve
  tahta boyutu dağılımı.
- **Doğrula:** bütün seti tek düğmeyle doğrula (iptal edilebilir ilerleme çubuğu),
  sıralanabilir tablo.
- **Çözüm:** hamle listesi + tuval yan yana; satıra tıkla, tahtada o ana kadarki
  hamleler.
- **Kapı renk KUYRUĞU artık düzenlenebilir.** Şemada (`gates[].colors`) baştan
  beri vardı ama editör yalnız 0. rengi yazabiliyordu — bu yüzden 50 bölümde hiç
  kullanılmadı. Alt şeritteki kapı kutusundan renk ekle/çıkar/değiştir.
- **Palet noktaları:** tahtada kullanılan ama **kapısı olmayan** renk kırmızı
  çerçeveyle işaretlenir (= çözülemez bölüm), çözücüyü beklemeden.
- **Oynanış sırası** tarayıcıdan ▲▼ ile değiştirilebiliyor (`LevelCatalog` yazılır).
- Cetveller ızgaranın **dört** kenarında; Kırp (`TrimBoard`) boş kenarları atıp
  bloğu/kapıyı/perdeyi birlikte kaydırıyor.

**Referansın teknolojisi UI Toolkit, IMGUI DEĞİL** (karelerden büyütülerek
doğrulandı: mavi halkalı yuvarlak slider topuzu, düz tek renkli yuvarlak köşeli
düğmeler). Bizimki IMGUI kalıyor — ızgara tuvali için doğru araç o. Referansın
ferah görünümü `LevelEditorSkin.cs` ile elle üretiliyor: BEYAZ yuvarlak köşeli
doku + `GUI.backgroundColor` çarpımı (boyama çarpma olduğu için kaynak beyaz
olmalı), harf aralıklı başlıklar (IMGUI'de tracking yok, harfler tek tek
konumlandırılıyor), kart blokları ve saç teli ayraçlar.

**ÜRETEÇLER ARTIK EDİTÖRDE** — 19 üreteç 12 bölümde vardı ve editör onları hiç
düzenleyemiyordu (Perde aracı yalnız `curtain` üretiyordu). Eklenenler: 7. araç
"Üreteç" (kenara tıkla), tuvalde makine kutusu + sıradaki bloğun rengi, alt
şeritte sıra kutuları (renk/şekil/sıra/silme menüsü), denetçide kenar + şerit +
satır satır sıra düzenleme, minyatürde makine noktası. Şema:
`{type:"generator", side:"N|E|S|W", x, y, queue:[blok…]}` — x/y yalnız KENAR
BOYUNCA konumu taşır (N/S'de x, E/W'de y anlamlı, diğeri 0).

**BULUNAN GERÇEK HATA: `DrawColorGrid` 8 renk varsayıyordu.** Satırı
`i % 4 == 3` olunca kapatıyordu; palet 10 renge çıkınca (mor + camgöbeği) son
satır HİÇ kapanmadı ve Blok aracı seçiliyken editör her karede
"Invalid GUILayout state ... Verify that all layout Begin/End calls match"
bastı. **DERS: açılan layout grubunu eleman sayısının bölünebilirliğine
güvenerek kapatma, BAYRAKLA kapat.** Aynı desen `DrawSizePalette`'te de vardı
(18 ön ayar 6'ya bölündüğü için şimdilik tutuyordu) — o da düzeltildi.

**RENK PALETİ REFERANSTAN ÖLÇÜLDÜ** (`level editör örnek.mp4` 5. sn, 736×480 ham
kare, piksel örnekleme). Referans **iki tonlu neredeyse siyah** kullanıyor:

| rol | renk |
|---|---|
| ana zemin | `#100F12` (16,15,18) |
| yükseltilmiş yüzey (üst şerit, şeritler, metin alanı) | `#1B1A1D` (27,26,29) |
| nötr düğme | `#201F22` (32,31,34) |
| seçili düğme (mavi) | `#2E3D5C` — lacivert, parlak mavi DEĞİL |
| altın düğme | `#75601F` — pirinç |
| yeşil birincil | `#356A30` — orman yeşili |

**DERS (şıklık düşük yüzey kontrastından gelir):** İki zemin tonu arasındaki
fark yalnız 11 birim; vurgular da kısılmış. Böylece ekrandaki EN DOYGUN şey
daima içerik (renkli bloklar) oluyor. Unity'nin varsayılan `#383838` grisi tam
tersi: zemin bağırır, içerik onunla yarışır. Ayrıca renkler hafif MOR'a çalıyor
(mavi > kırmızı > yeşil, 2-3 birim) — "soğuk siyah" hissini o veriyor.
**DERS (dolgu rengi ≠ yazı rengi):** Aynı kısılmış renk yazıda okunmaz; skinde
`Accent/Positive/Warning/Danger` (dolgu) ve `*Bright` (yazı/çizgi) ayrı tanımlı.
**Pencerenin TAMAMI `OnGUI` başında boyanıyor** — boyanmayan her boşlukta
Unity'nin grisi sızıyordu. `EditorStyles.toolbar` da terk edildi
(`LevelEditorSkin.BarScope`), o doku griyi geri getiriyor.

**MEKANİK ALGILAMA + ZORLUK EĞRİSİ** (`LevelMechanics.cs`): 10 mekanik veriden
hesaplanıyor (katman, buz blok, buz kapı, kapı kuyruğu, perde, üreteç, yönlü,
polyomino, duvar, şekilli tahta). Galeri kartlarında rozet, galeride mekaniğe
göre süzgeç, Pano'da **tanıtım sırası** (her mekanik ilk hangi bölümde) ve
**zorluk eğrisi** (hamle sayısı çubuk grafiği, renk zorluktan geliyor).

Araç ilk çalıştırmada şunları çıkardı (2026-08-17):
- **İç duvar 0 bölümde kullanılıyor.** Şema ve araç destekliyor, içerik yok.
- **Kapı renk kuyruğu yalnız 1 bölümde** (29) — editör düzenleyemediği için.
- **Yönlü blok yalnız 1 bölümde** (50).
- Tanıtım sırası: polyomino 2 · buz kapı 6 · şekilli tahta 8 · buz blok 10 ·
  katman 15 · perde 20 · kuyruk 29 · üreteç 35 · yönlü 50.
- Doğrulama: **0 bozuk, 2 temiz, 48 uyarılı.** Uyarı dağılımı: 42× "açılışta
  tek hamle", 35× "süre dar olabilir", 6× "çok dar (zorunlu hamle)",
  **3× BUZ BÜTÇESİ** (o kapıların buzu hiç kırılamıyor — kapı fiilen dekor),
  2× "süre çok bol", 1× "çok serbest". En uzun çözüm level_038 (42 hamle).
  → Sıradaki denge işi bu listedir. "Açılışta tek hamle" 42/50 çıktığı için
  ölçütün kendisi de fazla katı olabilir; önce ona bakılmalı.

**TUVAL ARTIK OYUNUN TUĞLASINI ÇİZİYOR.** Editör düz renk dikdörtgen boyuyordu;
oyunda bloklar **LEGO benzeri kabartmalı tuğlalar** (`BrickMeshBuilder`: mesh +
kendi ışıklandırmalı materyal + temas gölgesi). Tuval artık aynı ölçüleri
`BlockVisualConfig.asset`'ten okuyup çiziyor: kenar payı (`brickInset`, yalnız
DIŞ kenarlara — bitişik hücreler birleşir), pah bandı (`brickChamfer`), üst yüz
ve yan yüz tonları (`toneFaceTop/toneBodySide`), **hücre başına `studsPerCell`²
saplama** (`studRadius`), temas gölgesi (`shadowOffset/shadowOpacity`). Zemin de
oyunun `floorColorA/B`'si + `frameColor` çerçevesi oldu (eskiden lavanta satranç
deseniydi). Minyatürler de aynı renkleri kullanıyor.
**DERS:** Editör oyunun görsel dilini paylaşmazsa tasarımcı kafasında sürekli
çeviri yapar ve o çeviri her seferinde biraz yanlış olur.

**3D ÖNİZLEME SEKMESİ EKLENDİ** (`LevelEditorWindow.Preview.cs`). Referans
editördeki eksik tek sekme buydu. Bölümü **oyunun kendi kurulum koduyla**
(`BoardBuilder.Build`) `PreviewRenderUtility`'nin izole sahnesinde kuruyor;
kamera GameSession'ın kadrajını birebir tekrarlıyor (80° eğim, 27° FOV, köşeler
görünene kadar ikili arama). Sürükle = serbest yörünge, tekerlek = uzaklık,
"Oyun kamerası" düğmesi kadraja döner. `_previewSignature` (bölümün JSON'u)
değişmedikçe yeniden kurulmuyor. `OnDisable`/`OnDestroy`'da `Cleanup()` —
yoksa RenderTexture ve gizli sahne sızar.

**BULUNAN İKİNCİ GERÇEK HATA: kurulum kodu edit modunda `Destroy` çağırıyordu.**
`BoardBuilder` (duvar + ızgara çizgisi), `GateView` ve `CurtainView` (3 yer)
`CreatePrimitive`'in çarpıştırıcısını `Object.Destroy` ile siliyordu; edit
modunda bu GEÇERSİZ ve Unity her çağrı için hata basıyor (önizleme ilk
denemede 41 hata verdi). `ViewKit.StripCollider(GameObject)` eklendi, beşi de
ona bağlandı. **DERS: kurulum kodu bir gün editörden de çağrılabilir; yıkım
çağrıları `Application.isPlaying` ayrımı yapmalı.** (`GeneratorView` bunu zaten
yapıyordu — desen oradan alındı.)

Önizleme sayıyla doğrulandı: level_001/024/046/050 dördü de render edildi
(16-88 mesh, karenin %18-24'ü dolu, 133-281 ayrık renk), **0 hata**.
Dokuz sekmenin hepsi tek tek çizdirildi, konsol temiz.

**TUVAL ARTIK OYUNUN KENDİ RENDER'INI GÖSTERİYOR** (`LevelBoardPreview.cs`).
Tahta sekmesindeki "Gerçek görsel" anahtarı açıkken tahta `BoardBuilder.Build`
ile kurulup **tepeden ortografik** bir kamerayla render ediliyor; 2B tuğla
çizimi yalnız yedek yol olarak kaldı (anahtar kapalıyken).

**Neden 90° ortografik, oyundaki 80° perspektif değil:** perspektifte bloklar
kenara doğru KAYAR, hücre↔piksel bağı bozulur ve tıklama yanlış hücreye düşer.
Ortografikte 1 hücre = 1 dünya birimi olduğu için eşleme doğrusal kalıyor.
Oyuncunun gerçek açısı 3D Önizleme sekmesinde duruyor — ikisi birlikte hem doğru
düzenleme hem doğru kadraj veriyor. Hizalama sayıyla kanıtlandı: 4×4 sınav
tahtasında sol-üst hücre KIRMIZI, sağ-alt MAVI, diğer üç nokta zemin — beşi de
beklenen hücreye düştü (yarım hücre kayma YOK).

Perde içeriği gerçek render'da görünmez (oyunda da görünmemeli) ama düzenlenmesi
gerekiyor: `DrawHiddenContents` onları yalnız editörde soluk biçimde üstüne
bindiriyor. Izgara da ince bir bindirme olarak çiziliyor.

**BULUNAN ÜÇÜNCÜ (ve en önemli) HATA: TAHTANIN ZEMİNİ HİÇ ÇİZİLMİYORDU.**
`BoardBuilder.BuildFloorMesh` iki üçgeni TERS sarıyordu; zemin ön yüzü AŞAĞI
bakıyordu ve kamera tepeden baktığı için arka yüz eleme (backface culling) onu
tamamen eliyordu. Oyunda "zemin" sanılan şey arka plan + ızgara çizgileri +
çerçeveydi; **`floorColorA/B`, `floorLineDarken`, `floorDotSize` ayarlarının
hiçbir görsel etkisi yoktu.**
- Nasıl bulundu: 3D önizleme oyunun kendi koduyla render ettiği için, arka planı
  MAGENTA yapıp yalnız zemini çizdirdim — her piksel magenta çıktı.
- Kıyas noktası aynı dosyadaki gölge mesh'i: köşelerini ALTTAN üste sıralıyor ve
  doğru çalışıyor. Zemin köşelerini ÜSTTEN alta sıralıyor (`CornerToWorld`'de y
  arttıkça z AZALIR), dolayısıyla üçgen sırası da tersine dönmeliydi.
- **DERS (ters sarım sessizce kaybeder):** Yanlış sarılmış yüzey hata vermez,
  sadece görünmez olur. Aylarca fark edilmemesinin sebebi bu.
- Doğrulama: 5 bölümde (001/024/038/046/050) oynanabilir hücrelerin merkezleri
  tarandı — **boş kalan hücre 0**. Zemin hem tepeden hem oyun açısından görünüyor.

Ayrıca `BlockView`'daki kullanılmayan `SideInset` sabiti kaldırıldı (derleme
uyarısı basıyordu). Konsol şu an **0 hata, 0 uyarı**.

**DENETİM TURU — 8 GERÇEK HATA BULUNDU VE KAPATILDI (2026-08-17)**

Editör baştan sona denetlendi. Hepsi sessiz hatalardı: hiçbiri konsola bir şey
basmıyordu, bu yüzden test etmeden fark edilmeleri mümkün değildi.

1. **Bayat seçim indeksi — SESSİZ VERİ BOZULMASI.** Seçim "Blocks listesinin 3.
   elemanı" gibi bir İNDEKS tutuyor. Sağ tıkla silme yollarının hiçbiri
   (`EraseAt` blok, `EraseAt` perde içeriği, `PlaceGate` silme, `PlaceGate`
   üzerine koyma, `EraseCurtain`) seçimi temizlemiyordu; liste kayınca denetçi
   hiç hata vermeden BAŞKA nesneyi düzenliyordu. Hepsi artık `DropSelection()`
   çağırıyor. **DERS: indeksle tutulan seçim, liste değişince yalan söyler.**
2. **Üreteç denetçisi tür kontrolü yoktu.** Perde ve üreteç aynı `obstacles`
   listesinde. Seçim bir perdeye denk gelirse üreteç denetçisi perdenin üstünde
   çalışıyor ve "sıraya ekle" perdeye `queue` alanı yazıyordu. Artık
   `Type != "generator"` ise seçim bırakılıyor; `ShowQueueMenu` de indeks/tür
   sınırlarını doğruluyor (`IndexOf` -1 dönerse tıklamada istisna atıyordu).
3. **Çözüm ve Referans sekmelerinde tuval DÜZENLENEBİLİYORDU.** Aynı
   `DrawCanvas` üç sekmede kullanılıyor ve `HandleInput` her yerde koşuyordu —
   Çözüm sekmesinde dalgın bir sürükleme bloğu oynatıp incelenen çözümü
   geçersiz kılıyordu. `DrawCanvas(interactive)` eklendi; o iki sekme salt
   okunur ve tıklayınca durum çubuğunda sebebini söylüyor.
   **DERS: görünüm paylaşılabilir, yetki paylaşılamaz.**
4. **`Playable()` kısa satırda çöküyordu.** `Rows[y][x]` uzunluk kontrolsüzdü;
   elle düzenlenmiş bir JSON'da kısa satır editörü her karede
   IndexOutOfRange'e sokardı. Artık kapalı hücre sayılıyor.
5. **Önizleme her karede bölümün TAM JSON'unu üretiyordu** — imza olarak. Layout,
   olay, Repaint ve her fare hareketi için ayrı ayrı. `_revision` sayacına
   geçildi (`Ensure(data, palette, key, revision)`).
6. **Çoğaltılan bölüm katalogda yer almıyordu** → oyunda açılmıyor, listede
   sırasız görünüyordu. `LevelLibrary.Register` eklendi; çoğaltma artık sete
   gerçekten ekliyor. **DERS: yarım kalan işlem, olmayan işlemden kötüdür.**
7. **Sıra değiştirmek bölüm NUMARASINI güncellemiyordu.** Oynanış sırası
   katalogda, oyuncuya gösterilen numara ise her JSON'un `displayNumber`
   alanında (GameplayScreen onu okuyor). ▲▼ ile taşımak 7. sıradaki bölümün
   ekranda "Level 12" demesine yol açardı. `RenumberFromCatalog` eklendi;
   yalnız DEĞİŞEN dosyaları yazıyor.
8. **Araç kısayolu 1–6'da kalmıştı**, 7. araç (Üreteç) eklendiği hâlde. Sınır
   artık `ToolInfo.Length`'ten okunuyor. Ayrıca `FromJson` perde içeriğini
   normalleştirirken ÜRETEÇ SIRASINI atlıyordu (üreteç şemaya sonradan
   eklenmiş, döngü güncellenmemişti) ve `AssetDatabase.GetAssetPath(int)`
   Unity 6.3'te obsolete olmuştu — ikisi de düzeltildi.

**DOĞRULAMA (sayıyla)**
- **Kaydetme veri kaybettirmiyor:** 50 bölüm gidiş-dönüş geçirildi
  (`FromJson → ToJson → FromJson → ToJson`). İkinci kayıtta **sürüklenme 0**.
  Blok/kapı/engel/perde içeriği/üreteç sırası/kapı renk kuyruğu/duvar/buz
  toplamı/eksen/maske sayıları ham dosyalardan BAĞIMSIZ olarak (Python)
  yeniden hesaplanıp karşılaştırıldı — **50/50 birebir eşleşti**.
- **Fonksiyonel tarama:** 50 bölümde model kurma 0 hata, önizleme kurma 0 hata,
  tepeden render'da boş hücre 0, minyatür 0 hata. Önbellek testi (aynı
  revizyonu tekrar istemek yeniden kurmamalı) geçti.
- **Dokuz sekmenin hepsi** canlı pencerede çizdirildi: **0 hata, 0 uyarı.**

**İKİNCİ DENETİM TURU — 5 HATA DAHA + TASARIM BORCU KAPANDI (2026-08-17)**

1. **Sıra değiştirmek açık bölümün numarasını bayat bırakıyordu.** `Reorder`
   dosyaların `displayNumber` alanını yeniliyor ama pencere bölümün KENDİ
   kopyasını tutuyor; bir sonraki kaydetme eski numarayı geri yazıp düzeltmeyi
   sessizce iptal ederdi. `SyncDisplayNumberFromDisk()` eklendi — dosyanın
   tamamını okumuyor (kaydedilmemiş düzenlemeler gitmesin), yalnız o alanı
   eşitliyor. **DERS: bellekteki kopya bayatlar.**
2. **Silmek numaraları kaydırmıyordu.** 8. bölüm silinince 9-50 arası bölümler
   eski numaralarıyla kalıyor, oyunda "Level 9" yazan bölüm 8. sırada
   oynanıyordu. `Delete` artık `RenumberFromCatalog` çağırıyor.
3. **`FromJson` üreteç sırasını normalleştirmiyordu** — perde içeriği geçiyordu,
   üreteç sırası atlanmıştı (üreteç şemaya sonradan eklenmiş, döngü
   güncellenmemiş). Elle yazılmış bir sırada `cells` ile `w/h` çelişirse blok
   yanlış boyutta doğardı.
4. **`AssetDatabase.GetAssetPath(int)`** Unity 6.3'te obsolete olmuş; nesne
   üzerinden giden aşırı yüklemeye geçildi (konsol uyarısı temizlendi).
5. **Ölü kod temizlendi:** `Round12`, `CardBorder`, `HairlineRow`,
   `Selection.IsNone`, `Entry.ObstacleCount` — hiçbiri kullanılmıyordu.

**TASARIM BORCU KAPANDI.** Kalan tek yama Unity'nin varsayılan widget'larıydı.
`LevelEditorSkin`'e eklendi ve her yerde kullanıldı:
- `FieldStyle` / `PopupStyle` / `SliderTrack` / `SliderThumb` — yuvarlak köşeli,
  düz, koyu giriş alanları. **DERS: Unity'nin alanlarını sıfırdan yazmak imleç,
  seçim ve kopyala davranışını da yazmak demektir. `EditorGUI.IntField` bir
  GUIStyle kabul ediyor — DAVRANIŞ Unity'de kalıyor, yalnız GÖRÜNÜM bizim.**
- `IntRow` / `TextRow` / `PopupRow` / `SliderRow` — etiket solda, alan sağda.
- `Note(text, kind)` — `EditorGUILayout.HelpBox` yerine: yuvarlak zemin, solda
  renkli şerit, soluk sarmalı yazı. Yükseklik `GetRect(içerik, stil)` ile
  ÖLÇÜLÜYOR; sabit yükseklik dar pencerede yazıyı kırpardı.
- 8 HelpBox, 5 `helpBox` kutusu ve ~65 varsayılan etiket skine geçirildi.
  Editör dosyalarında kalan `EditorStyles` kullanımı: **0** (yalnız skinin
  kendi font referansları).

**DOĞRULAMA:** derleme **0 hata / 0 uyarı**; katalog tutarlılığı salt-okunur
denetlendi (50 bölüm, numara uyuşmazlığı 0, liste sırası sapması 0), yani
yeniden numaralama bugün no-op — kod veriyi bozmuyor.

**ELLE DENENECEK (pencere kapalıydı, açıp kullanıcının işini bölmedim):**
yeni skinli alanların/kaydırıcıların ve not kutularının canlı çizimi. Editörü
açtığında bir tuhaflık görürsen söyle.

**İKİ IMGUI TUZAĞI — tekrarlama**
1. **Sekmeyi/aracı çizim sırasının ORTASINDA değiştirme.** OnGUI bir karede
   `Layout` → gerçek olay → `Repaint` diye üç kez koşar ve GUILayout yerleşimi
   `Layout` geçişinde ölçer. Tıklama anında gövdeyi değiştirirsen o geçiş farklı
   sayıda kontrol çizer → "Getting control N's position in a group with only M
   controls". Çözüm: `_pendingTab` / `_pendingTool` + `ApplyPendingMode()`, mod
   yalnız `Layout` geçişinin başında değişiyor.
2. **OnGUI'nin içinde modal diyalog açma.** `ConfirmDiscard()` bir dialog açar,
   olay döngüsünü keser, açık layout grupları kapanmaz. Bölüm açma/silme/çoğaltma
   `RequestOpen` / `DeleteLevel` ile `EditorApplication.delayCall`'a erteleniyor.

**Doğrulama (bu oturumda yapıldı):** 8 sekmenin hepsi gerçekten çizdirildi,
konsol **0 hata**. Galeri'nin çizildiği sayıyla kanıtlandı (önbellek sıfırlandı →
paint sonrası 50/50 minyatür). Kütüphane 50 bölümü okuyor, 50'si de katalog
sırasında, okunamayan yok.

**ELLE DENENMEDİ:** Araçlar sekmesindeki `Kırp`, `± Satır/Kolon`, tarayıcıdaki
▲▼ sıra değiştirme ve `Sil`. Üçü de yıkıcı; ilk kullanımda geri alma (Ctrl+Z)
elinin altında olsun.

**Sonraki adım (referansta var, bizde yok):** `3D Preview` sekmesi — bölümü
gerçek oyun görünümüyle Play'e girmeden göstermek. Referans editörde bu, izometrik
bir önizleme; bizde `BlockView` prefablarını edit modda bir `PreviewRenderUtility`
sahnesine kurmak gerekir.

---

## Hareket kütüphanesi: PrimeTween (2026-08-10'da geçildi)

Artık `com.kyrylokuzyk.primetween` 1.4.11 kurulu. **npm scoped registry ile** —
GitHub deposunda paket YOK, orada yalnız dokümantasyon var, git URL'i
"package manifest bulunamadı" ile düşer. `Packages/manifest.json` içinde
`scopedRegistries` bloğu bunun için duruyor.

`GameKit.FX.Juice` cephe olarak kaldı; içi PrimeTween'e taşındı. Çağrı yerleri
değişmedi çünkü `Run(PT.Tween)` geçirgen aşırı yüklemesi eklendi. Yeni yazarken
`Juice` üzerinden git, doğrudan `PrimeTween.Tween` çağırma — bir gün kütüphane
değişirse tek dosya değişsin.

Tuzak: `PrimeTweenConfig` ayarları **BeforeSceneLoad'da yapılamaz**,
PrimeTween'in yöneticisi de orada kuruluyor ve sıra tanımsız. `AfterSceneLoad`
kullan.

---

## Dil: arayuz tamamen INGILIZCE (2026-08-15)

Butun kullanici metinleri Ingilizceye cevrildi (~110 dize): HUD, menuler,
magaza, ayarlar, liderlik, profil, koleksiyon, gunluk odul, sahte reklam,
ogretici ve gizli gelistirici menusu. Referans oyun Ingilizce oldugu icin
panellerdeki metinler (PERFECT!, Continue, Pause, Resume, Quit, Hard Level)
zaten referansla ayni.

DOKUNULMAYANLAR (bilerek):
- **Kod yorumlari Turkce kaldi.** Onlar bu projenin ogretici belgesi; cevirmek
  degerini yok ederdi ve istenen "arayuz metni" degil.
- **Debug/konsol loglari Turkce kaldi** (AppRouter, BootLoader, GameSession,
  MetaServices). Oyuncuya gorunmuyorlar, gelistirici teshisi.

DIKKAT: Turkce dizeleri ararken **ozel karakter aramasi yetmez**. "Bildirimler",
"Haptik", "Kapat", "Oyna" gibi kelimelerde c/g/i/o/s/u yok; ilk tarama bunlari
kacirdi. Kelime listesiyle ikinci tur sart.

Magaza fiyatlari TL'den USD'ye cevrildi (2000 jeton = 4.99 dolar gibi standart
mobil basamaklar). Rakamlar UYDURMA, referanstan alinmadi.

---

## SIRADAKİ İŞLER (öncelik sırasıyla)

### 0. PERFECT paneli — BİTTİ, gözle doğrulandı
Panel referanstan ölçülerek yeniden yazıldı (`GameplayScreen`): mor kart, üst
kenara binen altın "PERFECT!" (mor konturlu), sağ üstte kırmızı **daire** çarpı,
"Level N", jeton tümseği, koyu sayı rozeti, yeşil "Continue". Yıldızlar ve
"Ana Ekran" düğmesi kaldırıldı — çarpı onun yerine geçti.

Referans kare: `1-20` videosu **02:52** (Level 8 PERFECT). Eskiden burada yazan
02:30/03:18 damgaları YANLIŞTI, ikisi de oynanışa denk geliyor.

Referanstan kalan farklar **kapatıldı**: ışık huzmesi eklendi
(`UiSprites.Burst`), jetonlar tümsek yerine dikey **istiflerden** kuruluyor,
reklam düğmesi baskılı gölgesi olmayan prosedürel panele geçti (panel_card'ın
baskılı gölgesi turuncuyla çarpılınca kırmızıya kayıyordu).

Gerçek `coin_pile` görseli hâlâ yok; `BuildCoinPile` icon_coin'lerden istif
kuruyor. Görsel gelirse orası tek bir Image'a iner, düzen değişmez.

### 0b. Doğrulama araçları (bu oturumda eklendi)
- Gizli geliştirici menüsüne (F8) **"Bölümü kazandır"** eklendi. Paneli elle
  açmıyor, gerçek bitiş yolunu (`OnBoardCleared`) çağırıyor — ödül, PERFECT
  ölçüsü ve can iadesi de çalışsın diye.
- `UiCaptureTool.CaptureOf<T>()` eklendi: yalnız o bileşenin kanvasını yakalar.
  Ayrıca dosyaya `.png` uzantısı artık yazılıyor (eskiden uzantısız kaydediyordu
  ve hiçbir görüntüleyici açmıyordu).

### 0c. `MetaServices` hazır olmama tuzağı — DİKKAT
`LastReward = 0` görürsen sebebi tekrar oynama değil: `NoteCleared` her zaman
en az 20 döndürür. 0 demek `MetaServices.Ready == false` demek, yani ödül, can
iadesi, bölüm açılışı ve kayıt **hiç yazılmamış**. Artık bu durumda
`GameSession` konsola hata basıyor.

Sebebi: derleme bitmeden play moduna girmek. `RuntimeInitializeOnLoadMethod`
çalışmıyor, `MetaServices.Initialize` hiç koşmuyor ve HUD her karede
`RefreshPowerUps` içinde NullReference atıyor. **Çözüm:** play moduna girmeden
önce `isCompiling` VE `isUpdating` ikisinin de false olduğunu doğrula; şüphe
varsa play modundan çık, bekle, temiz gir.
- ~~**"Ice Blocks! New Item Unlocked!" paneli**~~ — **BİTTİ**, gözle doğrulandı.
  `NewItemPanel` (`Assets/_Project/Scripts/Runtime/UI/NewItemPanel.cs`).
  Yedi mekaniği tanıyor (katman, kapı renk kuyruğu, buz blok, buz kapı, perde,
  yönlü blok, üreteç); bölüm başına EN FAZLA BİR tanıtım gösteriyor ve
  görüldüğünü PlayerPrefs'e yazıyor (`BlockOut.ItemSeen.*`).
  Test için o anahtarları silmek yeterli.

  Aşağısı eski notun kendisi, ölçüler hâlâ geçerli:
  **Doğru referans karesi: `1-20` videosu 03:31** (eskiden burada yazan ~01:06
  yanlıştı). Panelin adı da "Ice Door" değil **"Ice Blocks!"**.

  ÖNEMLİ: Bu bir KART DEĞİL. Tahta yerinde kalıyor, üstüne koyu bir perde
  iniyor ve tanıtılan öğe spot ışığıyla aydınlatılıyor — arkasında sıcak sarı
  bir hüzme, çevresinde altın kıvılcımlar. Yerleşim (0-1, alttan):
  - "Ice Blocks!" beyaz balon yazı, mor kontur: y 0.758–0.820
  - "New Item Unlocked!" daha küçük beyaz yazı: y 0.707–0.730
  - Öğenin kendisi (büyük buz bloğu) ortada: x 0.385–0.622, y 0.461–0.582
  - Altta krem kapsül + mor çerçeve, içinde ipucu:
    "Clear blocks to break the ice!" — x 0.084–0.917, y 0.270–0.340
  - Kırmızı daire çarpı sağ üstte, HUD hizasında: x 0.845–0.946, y 0.867–0.910

  Muhtemelen `TutorialOverlay` bu işin doğru evi — önce ona bak, sıfırdan
  panel yazma.
- **Duraklat paneli** — referansta Sounds/Musics/Haptics anahtarları +
  yeşil Resume + kırmızı Quit. Referans kare: `1-20` videosu, 00:03.

  **BİTTİ, gözle doğrulandı.** Referanstan ölçülerek yeniden kuruldu: mor kart,
  üst kenara binen "Pause", sağ üstte kırmızı daire çarpı, üç ayar satırı
  (Sounds / Musics / Haptics) + Off/On anahtarları, ayraç, yeşil "Resume",
  kırmızı "Quit". "Yeniden Başla" KALDIRILDI — referansta yok, yeniden başlatma
  zaten HUD'daki geri düğmesinde.

  Anahtarlar `SettingsBinder` üzerinden kayda yazıyor; panel her açılışta
  kayıttan tazeleniyor (ayar Ayarlar ekranından da değişmiş olabilir).
  Her iki yarı da tıklanabilir — oyuncu istediği duruma DOĞRUDAN basıyor,
  tek düğmeli "değiştir" davranışı bir fazladan adım olurdu.

  Üç simge (hoparlör, nota, titreşim) elimizde yoktu, **çizim olarak üretildi**:
  `UiSprites.Speaker / MusicNote / Haptics`. İşaretli mesafe (SDF) ile
  çiziliyorlar — tek 128×128 dokudan her ölçekte temiz kenar, ve şekli
  değiştirmek birkaç sayı değiştirmekten ibaret.
- **21-30 videosu TARANDI.** Turuncu çerçeveli sayılı alanlar bizim PERDE
  mekaniğimiz — yeni değil. İki YENİ arayüz öğesi çıktı, ikisi de eklendi:
  - **"Hard Level" satırı** — sayacın hemen altında pembe yazı. `LevelData`
    zaten `difficulty` taşıyordu (13 hard + 5 superhard bölüm var); ikisi de
    satırı açıyor. Superhard'ın referansta ayrı bir yazısı var mı GÖRÜLMEDİ.
    Referans kare: 21-30 videosu, Level 24.
  - **Yardımcı fiyatı jeton kapsülünde** — elde hiç yokken kırmızı adet rozeti
    yerine, düğmenin altında jeton simgeli SARI kapsül. Adet ve fiyat biçimle
    ayrılıyor: "3" ile "300" aynı rozette olsa oyuncu ikisini karıştırır.
- **31-40 ve 41-50 videoları TARANDI. Yeni arayüz öğesi ya da yeni mekanik YOK.**
  Bütün referans videoları artık taranmış durumda. Cikanlar:
  - Tahtalar düzensiz siluetlere bürünüyor (elmas, ev, merdiven, S). Bunlar yeni
    mekanik değil — bölüm JSON'undaki `rows` maskesi zaten destekliyor.
  - Mor ve camgöbeği bloklar çıkıyor; paletimizde ikisi de var.
  - Perde sayaçları büyüyor (17, 20), kapı sayısı artıyor. Aynı mekanik.
  - "Hard Level" satırı burada da görünüyor — eklendi.
  - Yolculuk bölge tanıtım ekranları: Level 34/35 uzay-roket sahnesi,
    Level 44 kar-penguen sahnesi. **"Penguin Chase" adı referansta birebir
    böyle geçiyor** (bizim çevirimiz tuttu).

  Yani referanstan çıkarılacak arayüz işi KALMADI. Bundan sonrası ses, cihaz
  ölçümü ve denge.

### 2. Ses (kullanıcı bunu bekliyor)
Şu an her şey kodla sentezleniyor (`SfxSynth`), müzik yok. İki yol:
ücretsiz lisanslı kütüphane (Kenney / freesound CC0) → `Assets/_Project/Audio/`,
ya da mevcut sentezi katmanlı hale getirmek.

### 3. Kalan görsel istekleri
`docs/art-prompts.md` içinde. Koleksiyon ekranının referans görüntüsü hâlâ
gelmedi — şu anki düzen tahmin.

### 4. Ölçülmemiş olanlar
- Cihazda kare hızı/bellek (APK alınmadı, `PerfProbe` hazır)
- Bölüm dengesi (analitik topluyor, gerçek oyun verisi yok)

---

## Bu projede DÖRT KEZ düşülen tuzaklar

Yeni oturumda bunları tekrarlama:

1. **Boyama ÇARPMADIR.** Koyu bir sprite'ı açık renge boyayamazsın.
   Parlak renk isteyen her yerde `panel_card` (krem) kullan, `panel_dark`
   veya `btn_square` (mor) değil. Üç kez bu yüzden yanlış renk çıktı.
2. **Simge YAZI DEĞİLDİR.** `↺ ⚙ 🐧 ∞ ♥` Baloo 2'de yok, TMP boş kutu
   çiziyor. Simgeler sprite ya da çizim olmalı. Dört kez tekrarlandı.
3. **Kurulumu değiştirmek yetmez, TAZELEMEYİ de değiştir.** Bir değeri her
   kare yazan `Refresh*` metodu varsa, kurulumdaki değer görünmez.
4. **`Mathf.SmoothStep` GLSL'deki `smoothstep` DEĞİL.** Unity'ninki iki
   DEĞER arasında yumuşatılmış lerp yapar ve t'yi 0-1'e kırpar — eşik
   uygulamaz. `1 - Mathf.SmoothStep(63f, 64f, d)` bir maske değil, her yerde
   negatif bir sayıdır; alfa tamamen 0 çıkar. Mağaza kapsülleri, ∞ halkası ve
   tente festonu tam olarak bu yüzden hiç görünmedi. `UiSprites` de aynı hatayı
   taşıyordu: yuvarlak panellerin arkasında %25 saydamlıkta bir KARE hayalet
   vardı ve aylarca fark edilmedi. İkisi de düzeltildi (`static float Step(...)`).
5. **Saydamlığı satranç zemine bindirip kontrol etme.** Üretici görselin
   sahte saydamlığı da satranç; koyu düz renk kullan.
   (`UiCaptureTool` bu yüzden koyu zemin kullanıyor.)
6. **BASKILI GÖLGELİ SPRITE'I BOYAMA.** 1. maddenin devamı ve daha sinsi
   hâli: `panel_card` ve `btn_*` görsellerinin alt kenarında BASKILI 3B gölge
   var. Boyama çarpma olduğu için o gölge turuncuya boyayınca KIRMIZIYA, kreme
   boyayınca MAGENTAYA kayıyor ve düğmenin altında oyunun hiçbir yerinde
   olmayan bir renk şeridi bırakıyor. Kullanıcı bunu "bozuk, tarzla uyumsuz,
   orijinal oyunda yok" diye işaretledi — haklıydı, oyunda 15 yerde vardı.
   - Bir yüzeyi BOYAYACAKSAN baskılı gölgesi olmayanı kullan:
     `UiKit.CreateOutlinedBox` ya da `UiSprites.RoundedPanel`.
   - Düğme kuracaksan rengi boyama, ROLÜNÜN görselini seç:
     `UiKit.CreateSpriteButton(..., Art.ButtonGreen / ButtonPurple / ButtonRed)`.
   - `CreateTintedButton` artık yalnız baskılı gölgesi OLMAYAN sprite'larla.
7. **Paylaşılan başlık materyali tek tek kontur ayarı KABUL ETMEZ.**
   `CreateTitle`'a verdiğin kontur rengi sessizce yok sayılır — bütün başlıklar
   tek materyali paylaşıyor. Tek bir başlığa özel kontur istiyorsan
   `UiKit.SetOutline(label, renk, kalınlık)`.

## Device Simulator (2026-08-16 kuruldu)

`com.unity.device-simulator.devices` 1.0.1 kurulu — **82 cihaz profili**.
Açmak: `Window > General > Device Simulator`, ya da Game view sekmesinden
"Simulator".

Ne işe yarar: çentik/güvenli alan, gerçek en-boy oranları ve DPI. Bizim
arayüzümüz tamamen oransal kurulduğu için asıl sınav çentik.

**Kurulurken bir hata yakalandı:** `UiKit.CreateSafeArea` güvenli alanı
YALNIZCA kurulum anında okuyordu. Tek cihazda kusursuz çalışır ama simülatörde
cihaz değiştirince arayüz ESKİ çentiğe göre kalırdı — üstelik simülatör "test
ettim" hissi verdiği için yanlış sonuç doğru sanılırdı. Telefon döndüğünde de
aynı sorun. Artık `UiSafeArea` bileşeni ekran değiştikçe yeniden uyguluyor
(her kare değil, yalnız değişince).

Bu, DEVAM.md'deki 3. tuzağın ("kurulumu değiştirmek yetmez, TAZELEMEYİ de
değiştir") dördüncü tekrarıydı.

---

## Yakalama/doğrulama notları

- **~~`UiCaptureTool.Capture()` kadrajı bozuyor~~ — ARTIK GEÇERSİZ (2026-08-17).**
  Bu not bir zamanlar doğruydu ama şimdi `Capture(ad)` tam olarak 1080×1920
  ve doğru kadrajda çıkıyor (beş sekmenin beşi de görünüyor). **Üstelik ASIL
  KULLANILMASI GEREKEN O.** `CaptureOf<T>` yalnız o bileşenin KENDİ kanvasını
  çiziyor; alt sekme çubuğu `MenuCanvas`'ta olduğu için `CaptureOf<HomeScreen>`
  karesinde HİÇ görünmüyor, menü sayfalarının altındaki zemin de görünmüyor.
  Bu yüzden iki gerçek hata aylarca gizlendi: Liderlik'te sekmelerin arkasındaki
  siyah şerit ve podyumda birbirine giren kürsüler. İkisi de ilk tam ekran
  `Capture()` çağrısında bir anda ortaya çıktı.
  **Kural: kıyas ve denetim için `Capture(ad)`; yalnız tek bir paneli izole
  etmek istediğinde `CaptureOf<T>`.**
- **TAM KARE yakalama (tahta + zemin + HUD).** `UiCaptureTool` kendi
  kamerasını kurup YALNIZ kanvası çeker; tahta ve zemin o karede yoktur, bu
  yüzden referansla "ne kadar karardı" kıyası yapılamaz. Tam kare için ana
  kamerayı bir `RenderTexture`'a yönlendir, kanvası geçici olarak
  `ScreenSpaceCamera` + `planeDistance = 0.5` yap, `Camera.Render()` çağır,
  sonra hepsini geri al. (Yardımcı istemi kıyası bununla yapıldı.)
  Tuzak: bu yöntemde TMP bazı harfleri eksik çiziyor ("remove" → "emove");
  yakalama artefaktı, gerçek hata değil — kanvas yakalamasında metin doğru.
- **MCP komutları play modu SIRASINDA domain reload tetikleyebiliyor.**
  Bir doğrulama komutu `MetaServices.Save.Data.Settings`'i sorunsuz okudu,
  iki komut sonra aynı satır NullReference attı: `MetaServices.Ready`
  **False** olmuştu. Play modundan çıkılmamıştı — araya giren derleme
  statikleri sıfırladı, `RuntimeInitializeOnLoadMethod` ise yalnız play
  BAŞLARKEN koşar. 0c maddesindeki tuzağın oturum ORTASINDA çıkan hâli.
  **Çözüm:** etkileşim testini TEK komutta yap (düğmeye `onClick.Invoke()`
  ile basmak kare gerektirmez, `Show()` de anında etkir). Her komutun başına
  `if (!MetaServices.Ready) { LogError; return; }` koy — yoksa sıfırlanmış
  statiklerle çalışan bir testin sonucunu doğru sanarsın.
- **`System.IO` kullanan MCP komutu reddediliyor** ("User interactions are not
  supported"). Kayıt dosyasını yedekleme/geri yükleme gibi işleri kabuktan yap.
  Aynı kapı `System.Reflection`'a da kapalı — özel alanları yansımayla
  okuyarak teşhis yapamazsın, davranışı dışarıdan gözlemle.
- **PLAY MODU AÇIKKEN KAYNAK DOSYASI DÜZENLEME.** Unity derleyip domain'i
  yeniden yükler; oyun çalışmaya devam eder ama C# çalışma-zamanı durumu
  SİLİNİR: `AddListener` ile bağlanmış bütün dinleyiciler gider,
  `[SerializeField]` olmayan özel alanlar null olur. Sahne hiyerarşisi
  yerinde durduğu için her şey normal GÖRÜNÜR — düğmeler oradadır, tıklarsın,
  hiçbir şey olmaz ve konsolda tek bir hata yoktur. Bir saat "panelim neden
  aşama değiştirmiyor" diye aranmasının sebebi buydu; kod doğruydu.
  **Kural: düzenlemeden önce play modundan çık, sonra temiz gir.**
  Teşhis işareti: bileşenin `Visible` gibi alan okuyan bir özelliği
  beklenmedik biçimde `False` dönüyorsa alanlar silinmiştir.
- **Yıkıcı bir akışı denemeden ÖNCE kaydı yedekle.** Hesap silme testi gerçek
  ilerlemeyi siler; `%LOCALAPPDATA%Low/furkanblci/Block Out Clone/` altındaki
  `save.json`, `save.json.bak`, `analytics.json` kopyalanıp sonra geri konuyor.
  Play modundan ÖNCE çık, sonra geri yükle — çalışan oyun çıkarken üstüne yazar.
- **`EditorApplication.Step()` play modunda ÇAĞIRMA.** MCP komutunun içinden
  çağrılınca "PlayerLoop internal function has been called recursively"
  hatası yağıyor. Onun yerine `Application.runInBackground = true` yap ve
  kareleri AYRI komutlara böl — komutlar arasında gerçek zaman geçtiği için
  kareler kendiliğinden ilerliyor.
- **Aynı komutta hem bölüme girip hem yardımcıyı kullanma.** Arada kare
  geçmediği için `Update`'te duran bağlanma çalışmaz ve istem hiç açılmaz;
  bu bir oyun hatası değil, testin kendi hatasıdır (bir kez düşüldü).
- `EditorApplication.QueuePlayerLoopUpdate()` kareleri **kuyruğa alır**;
  aynı komut içinde ekran görüntüsü alırsan güncellenmemiş hâli yakalarsın.
  Kareleri bir komutta çevir, **ayrı bir komutta** yakala.
- `ScreenCapture.CaptureScreenshot` MCP üzerinden engelli. Kanvasları geçici
  olarak ana kameraya bağlayıp elle render et (`UiCaptureTool` bunu yapıyor).
- Play moduna girmeden önce **mutlaka** derlemenin bittiğini doğrula; aksi
  halde eski kodla çalışan bir oturumu test edersin.

---

## GÖRSEL DENETİM — 2026-08-10, YARIM KALDI

Ekranlar referansla yan yana kıyaslanmaya başlandı. Yöntem: referans kareyi ve
bizim yakalamayı tek bir sayfaya yan yana koyup bakmak (`cmp_*.png`).
**Kıyas ölçüsü dikey yüzde DEĞİL, EKRAN GENİŞLİĞİNE oran olmalı** — referans
kare 384×832 (oran 2.17), bizim yakalama 1080×1920 (oran 1.78); dikey yüzdeler
doğrudan karşılaştırılamaz.

### Bulunan ve DÜZELTİLEN
- **Ayarlar/Profil tam ekran olmalıydı.** MenuShell ekranları 0.105–1 arasına
  koyuyor; referansta bu ikisi sekme çubuğunu da örten TAM EKRAN sayfalar.
  `MenuPage.Screen` kökü 210 birim aşağı taşıyor, MenuShell de bu ikisini
  çubuktan SONRA çiziyor (`SetAsLastSibling`).
- **Ayarlar çok küçüktü:** satır 132→196, kart/düğme/yazı ölçüleri referans
  oranına çekildi. Artık örtüşüyor.
- **Profil büyütüldü** (kutu 150→214, punto artırıldı) ama küçük önizleme
  üzerinden daha fazla ince ayar yapmak güvenilir değil. Bir sonraki oturumda
  yapılacak ilk iş: profil karesini TAM ÇÖZÜNÜRLÜKTE referansla yan yana koyup
  kutu yüksekliğini piksel ölçerek doğrulamak (göz kararı değil).

### AÇIK KALAN GÖRSEL EKSİKLER
1. ~~Zil simgesi~~ — **BİTTİ**. `UiSprites.Bell` eklendi (kubbe + etek + alt
   kenar + tokmak, işaretli mesafeyle). Ayarlar artık dört satırda da simgeli.
2. **Altın `icon_lock` gelmedi** — mevcut gümüş kilit sıcak tona boyanıyor,
   mat pirinç çıkıyor. Prompt `docs/art-prompts.md` §8.2'de.
3. **Podyumda üç oyuncu da aynı avatar** — `avatar_2/3/4` üretilmedi.
4. Ayarlar kartının referanstaki ince açık kenarlığı bizde yok (çok küçük fark).

### KIYASLANDI ve DÜZELTİLDİ
- **Liderlik** — ölçek büyütüldü; sıra rozeti kendi kapsülüne alındı, avatar
  kare çerçeveye, puan rozetine altın kenar eklendi. **Referansta 5 rakip
  satırı var, bizde 4** — referans ekran 9:19.5, bizimki 9:16; aynı ölçekte
  beşincisi sığmıyor, oranı korumak bir satırdan önemli sayıldı.
  İki tuzak: (a) `enableAutoSizing` tek başına çalışmaz, `overflowMode`
  kısıtlayıcı olmalı; (b) sığdırılmış yazı bile bitişik okunur, boşluk
  KUTUDAN gelmeli.
- **Yolculuk** — çember 646→812, kapsül genişliği %80→%90, seviye puntosu
  56→66, bölge adı 50→62, kilit/etiket/düğme büyütüldü. Referansla örtüşüyor.

- **Mağaza** — bütün kart yükseklikleri ~%30 büyütüldü (teklif kartı 388→506,
  paket kartı 380→495, jeton kutusu 354→452, bölüm şeridi 82→104) ve puntolar
  buna göre artırıldı. Referansla örtüşüyor.

### BEŞ MENÜ EKRANININ HEPSİNDE AYNI HATA ÇIKTI
Hepsi referanstan **%25-35 küçük** kurulmuştu. Sebep tek: ölçüler referans
karesinden alınırken dikey yüzdeyle kıyaslanmış. Referans kare 384×832
(oran 2.17), bizim yakalama 1080×1920 (1.78) — "ekranın %30'u" iki tarafta
farklı şey. **Doğru birim: EKRAN GENİŞLİĞİNE oran.** Kanvas ölçekleyici
yüksekliğe kilitli olduğu için genişlik sabit referans.
Yeni bir ekran kurarken ölçüyü şöyle çevir:
`birim = referans_piksel / referans_genişlik * 1080`

### OYUN İÇİ PANELLER — kısmen kıyaslandı
- **Duraklat paneli** — ✅ **referansa UYUYOR.** Kodu referansla satır satır
  karşılaştırıldı (`GameplayScreen.BuildPausePanel` + `BuildSettingRow`):
  koyu perde, mor kart, üst kenara binen "Pause", kırmızı çarpı, üç satır
  (Sounds/Musics/Haptics) ikili [Off|On] anahtarıyla, ayraç, yeşil Resume,
  kırmızı Quit. Referans kare: `1-20` videosu **00:02**.
- **PERFECT paneli** — daha önce bitmişti (bkz. yukarıdaki bölüm).
- **YARDIMCI İSTEMİ — BİTTİ, PLAY MODUNDA SAYIYLA DOĞRULANDI (2026-08-16).**

  **Önce ölü bir olay bulundu.** `GameplayScreen.Init` içindeki
  `if (_session.PowerUps != null)` HER ZAMAN false'tu: `EnsureServices` bu
  ekranı kurarken `PowerUps` henüz doğmamış oluyor (o nesne `Restart` içinde
  kuruluyor). Yani `PowerUps.Message` olayının **hiç dinleyicisi yoktu** —
  yeni istem paneli de, ondan önceki 2.5 saniyelik ipucu yazısı da hiçbir
  zaman çalışmamış. Üstüne `Restart` her çağrıldığında `PowerUps` YENİDEN
  kuruluyor, yani tek seferlik abonelik doğru anda kurulsa bile ilk yeniden
  başlatmada ölü nesneyi dinler kalırdı. İkisi de `BindPowerUps()` ile
  çözüldü: her karede örnek değişti mi diye bakıyor, değiştiyse yeniden
  abone oluyor.

  **Sonra karartmanın yanlış katmanda olduğu bulundu.** Perde HUD kanvasına
  seriliyordu; kanvas `ScreenSpaceOverlay` olduğu için TAHTAYI da karartıyordu.
  Referans kareleri (istemli 66-67. sn ↔ istemsiz 64/68/70. sn) tersini
  söylüyor:

  | bölge | normal | istemli | sonuç |
  |---|---|---|---|
  | boş zemin | 49.0 | 23.0 | %47'ye iner |
  | Seviye rozeti (tepe) | 255 | **43** | görünmez |
  | duraklat düğmesi (tepe) | 255 | **50** | görünmez |
  | **tahta** | 119.5 | 119.4 | **değişmez** |

  HUD'ın kalıntısı karartılmış zeminin tepe değeriyle (44) aynı — yani
  referans HUD'ı **soldurmuyor, tamamen gizliyor**. Başlığın sayaçla
  çakışması da bu yüzden referansta yok.

  **DERS (ortalama yalan söyler, tepe değer söylemez):** HUD'ın ortalaması
  49.3 → 23.4 düşüyor ve bu "yarı yarıya soluyor" gibi okunuyor; ilk düzeltme
  alfayı 0.48 yapmıştı. Ortalamanın çoğu zaten koyu olan zemin. Bir öğenin
  görünürlüğünü ölçerken TEPE değere bak.

  **DERS (katsayı ekranda gördüğün oran değildir):** Zemini %47'ye indirmek
  için 0.47 yazmak yanlış — çarpım doğrusal uzayda, ölçtüğümüz PNG sRGB.
  0.36 katsayısı ekranda 0.23 oldu; doğru değer (0.59) ölçerek geri çözüldü.

  Son hâl üç ayrı katmandan karartıyor: HUD `CanvasGroup` ile gizleniyor
  (`blocksRaycasts` de kapanıyor — görünmeyen duraklat düğmesine basılmasın),
  zemin `BackgroundView.SetDim` ile (paylaşılan malzemeyi boyamamak için
  `MaterialPropertyBlock`), yardımcı düğmeleri tek tek (seçilen parlak kalsın).

  Doğrulama sonucu — referans / bizim: zemin oranı 0.47 / **0.50**,
  HUD kalıntısı tepe 43 / **38**, ortalama 20.5 / **20.9**.
  Tahtaya raycast: **0 engel**. Kapanışta her şey geri alınıyor
  (alfa 1, raycast açık, panel kapalı, özellik bloğu temiz).

  Eski notun kendisi: Referansta (menüler videosu **67. saniye**)
  yardımcı seçilince EKRANIN TAMAMI değişiyor: tahta kararıyor, sol üstte
  BÜYÜK parlayan yardımcı görseli + kıvılcımlar, ortada beyaz mor konturlu
  başlık ("Roket"), altında yönerge ("Kaldırılacak bloğu seçin"), alttaki
  yardımcı düğmesi de parlıyor.
  **Bizde yalnız 2.5 saniyelik bir yazı var** (`GameplayScreen._hint`).
  Yapılacak: `PowerUpPrompt` diye ayrı bir örtü paneli; `PowerUps.Message`
  olayına yazı yerine (kind, prompt) geçirilip görsel de gösterilmeli.
- Kaybetme paneli hiç kıyaslanmadı — ama referansı bulundu:
  `OneDrive/Masaüstü/Block Out! Videos/Game over .mp4`.

### KAYBETME AKIŞI — kıyaslandı (2026-08-16)

Referans: `OneDrive/Masaüstü/Block Out! Videos/Game over .mp4` (24 sn, 384x832).
Kontakt sayfası: `fps=1, tile=8x4`.

**EN ÖNEMLİ BULGU: kaybetmek TEK panel değil, ÜÇ AŞAMALI bir akış.**
Bizde tek bir "TIME'S UP" kartı vardı; referans şunu yapıyor:

| # | panel | referans sn | içerik |
|---|---|---|---|
| 1 | **Süre Doldu** | 8-11 | altın kronometre, "+30 saniye", yeşil `Zaman Ekle 🪙900` |
| 2 | **Devam Et?** | 12-16 | kırık kalp, "1 can kaybedeceksiniz!", yeşil `Oyna 🪙900`; altında ayrı "Blok Bileti" promosyon kartı |
| 3 | **BAŞARISIZ** | 17-20 | mor kart, üste binen altın başlık, kırık kalp, "Zor" + "Seviye 54", jeton yığını, `60 ❌`, "Ödüller x3", yeşil `Yeniden Dene` |

Aradaki 4. ve 10. saniyelerde **Mağaza** açılıyor — yani jetonu yetmeyen
oyuncu bu panellerden doğrudan mağazaya gidebiliyor.

**3. AŞAMA (BAŞARISIZ) KURULDU ve play modunda doğrulandı.** Değişenler:
- Başlık `TIME'S UP` (kırmızı) → **`FAILED` (ALTIN)**. Referans kaybı RENKLE
  değil BİÇİMLE anlatıyor; kırmızı başlık bizim eklememizdi.
- Düğme kırmızı → **yeşil** (referansta "Yeniden Dene" yeşil).
- **DERS (kaçırılan ödülü GÖSTER, gizleme):** Eski kod kaybedince ödül alanını
  tamamen kaldırıyordu. Referans onu gösterip üstüne kırmızı çarpı atıyor.
  "Şunu kaçırdın" demek, "burada bir şey yok" demekten çok daha güçlü bir
  tekrar oynama sebebi. Bunun için `ProgressService.PreviewReward` eklendi.
- **DERS (okuma kayıt OLUŞTURMAMALI):** `Progress.Record(id)` aradığını
  bulamazsa yeni kayıt yaratıp kayda YAZIYOR. Ödülü sırf göstermek için onu
  çağırmak, oynanmamış her bölüm için kayıt dosyasında boş satır açardı.
  `PreviewReward` bu yüzden `TryGetValue` ile okuyor.
- Bölüm numarası artık kaybederken de var; zorluk etiketi panelde KISA
  ("Hard"), HUD'daki uzun hâli ("Hard Level") değil.

**AÇIK KALANLAR:**
1. ~~1. ve 2. aşama panelleri KURULMADI~~ — **KURULDU (2026-08-17),
   play modunda sayıyla doğrulandı.** Bkz. aşağıdaki bölüm.
2. ~~Kırık kalp görseli yok~~ — geldi (`icon_heart_broken`), iki panel de
   onu kullanıyor.
3. "Ödüller x3" etiketinin ne yaptığı referanstan ANLAŞILMADI — reklamla
   ödül katlama olabilir; şu an yalnız görsel olarak duruyor.

### KAYBETME AKIŞININ İLK İKİ AŞAMASI KURULDU (2026-08-17)

`Assets/_Project/Scripts/Runtime/UI/ContinueOffer.cs`. Süre bitince artık
FAILED kartı DOĞRUDAN açılmıyor; referanstaki iki teklif araya giriyor.

**Ölçüler referans karelerinden alındı** (`Game over .mp4`, 384×832):
1. aşama 8.6. saniye, 2. aşama 13.6. saniye. Yatayda `piksel/384`, dikeyde
`1 - piksel/832` (kabuk ekranlarındaki kuralın aynısı).

| | 1. aşama "Time's Up!" | 2. aşama "Continue?" |
|---|---|---|
| kart | **YOK** — içerik doğrudan perdenin üstünde | ekranı uçtan uca geçen mor sayfa, Y 0.298–0.792 |
| görsel | çalar saat, X 0.219–0.740 · Y 0.416–0.675 | kırık kalp, X 0.302–0.698 · Y 0.543–0.683 |
| yazı | "+30 SECONDS" Y 0.326–0.371 | "You'll lose 1 life!" Y 0.462–0.492 |
| düğme | "Add Time 🪙900" X 0.172–0.833 · Y 0.162–0.250 | "Play 🪙900" X 0.203–0.802 · Y 0.352–0.440 |
| çarpı | HUD hizasında Y 0.894–0.942 | panelin üst kenarında Y 0.757–0.803 |

2. aşama tek bir kart görseli DEĞİL, üst üste beş bant: kenar (`#2C0B5E`),
başlık (`#7035D3`), koyu oyuk (`#311265`), gövde (`#6122BC`), ayak (`#4E1B9C`).
Renkler referans karesinden piksel örneklenerek alındı; tek bir sprite bu
kademeyi veremezdi.

**Karartma referansta ÇOK daha koyu.** Yardımcı isteminde tahta %47'de
kalıyordu (okunması gerekiyordu); burada ölçüm **%8–10** çıktı — tahta
yalnızca bir hatıra. Bu yüzden perde neredeyse mat (alfa 0.94) ve HUD
tamamen gizleniyor (alfa 0).

**Jeton sayacı panelin KENDİSİNDE.** HUD gizlendiği için sayaç da onunla
kaybolurdu; referans tam bu yüzden sayacı panelin üstünde ayrıca çiziyor.
Fiyat gösteren bir ekran cüzdanı da göstermeli.

**Doğrulama (play modunda, sayıyla):**
- Süre bitti → 1. aşama açıldı, HUD alfa 0, FAILED kartı kapalı.
- 1. aşamada çarpı → 2. aşama; 2. aşamada çarpı → teklif kapandı, FAILED
  açıldı, HUD alfa 1'e döndü.
- 1320 jetonla "Add Time" → jeton **420**, durum **Playing**, kalan süre
  **30**, teklif kapandı, FAILED açılmadı.
- 420 jetonla (fiyat 900) → jeton değişmedi, durum Lost, panel AÇIK kaldı,
  FAILED açılmadı. İki aşamada da aynı.
- Reddedip yeniden başlat → teklif yeniden açılıyor.

**REFERANSTAN BİLEREK ALINMAYAN İKİ ŞEY:**
- 2. aşamanın altındaki **"Blok Bileti" promosyon kartı** kurulmadı: bizde
  öyle bir ürün yok, olmayanı satan bir kart uydurma olurdu.
- Referansta jetonu yetmeyen oyuncu bu panellerden **mağazaya gidebiliyor**
  (4. ve 10. saniyeler). Bizde mağaza menü kabuğunda ve menüye dönmek
  `AppRoot.ShowMenu → StopLevel` ile tahtayı söküyor — yani "devam et"
  imkânsız hâle gelirdi. Bunu yapmak için mağazanın oynanışın ÜSTÜNE açılan
  bir kip kazanması gerekir. Şimdilik jeton yetmeyince fiyat kapsülü
  sarsılıyor ve panel açık kalıyor.
- Referansta 1. aşamanın kronometresi **altın**, bizimki süre yardımcısının
  **yeşil** çalar saati. Boyayarak çözülemez (boyama çarpmadır, denendi);
  görsel isteği `docs/art-prompts.md` §10'da.

**Teklif deneme başına BİR KEZ** çıkıyor (bizim kararımız, referanstan
çıkarılamadı): jetonla devam edip yine kaybeden oyuncu doğrudan FAILED'e
gidiyor. Sonsuz "öde ve devam et" döngüsünü referans doğrulamadan kurmak
istemedik.

### GÖRSEL AKTARIMI — sessiz atlama hatası (2026-08-16)

**`tools/import_art.py` yalnız `*.png` topluyordu ve JPG'leri UYARI VERMEDEN
atlıyordu.** Sonuç: `avatar_2/3/4` 15 Ağustos'tan beri `art_raw/` içinde
duruyor ama oyuna hiç girmemişti; altın `icon_lock.jpg` de aynı gün gelmiş,
aynı şekilde kaybolmuştu. Kullanıcı "kilit zaten vardı" görüp (o ESKİ GÜMÜŞ
`icon_lock.png` idi) ikinci bir dosya (`icon_lock2.jpg`) üretmek zorunda
kalmıştı.

**DERS (sessizce atlamak hatadan beterdir):** Bir boru hattı işleyemediği
girdiyi görmezden gelirse, sorun aylarca "görsel gelmedi" diye yanlış yerde
aranır. Artık `*.jpg/*.jpeg` de okunuyor; alfa yokluğu sorun değil, kesim
zemini zaten kendisi siliyor.

**DERS (uzantıya değil TARİHE bak):** Aynı isim iki uzantıyla gelince ilk
kural "PNG kazansın" idi (alfası olabilir diye). Gerçek durum tersine döndü —
BAYAT gümüş PNG, yeni altın JPG'yi eziyordu. Artık `st_mtime` en yeni olan
kazanıyor. `icon_lock2` ise `SKIP` kümesinde: dosya silinmiyor (kullanıcının
ürettiğini silmek bizim işimiz değil), yalnız oyuna girmiyor.

Aktarılanlar: `icon_heart_broken`, altın `icon_lock`, `avatar_2..4`,
`avatar_6..9`. Kesimler koyu zeminde tek tek doğrulandı, hâle yok.
`check_art.py` bunları GÖRMÜYOR — sabit listeden çalışıyor, yeni dosyalar
listeye eklenmedikçe "temiz" demesi bir şey ifade etmiyor.

**Avatarlar bağlandı:** podyum ve rakip satırları artık farklı yüzler
kullanıyor (`UiSkin.Rival(index)`), oyuncunun kendi satırı `avatar_player`.
- **`avatar_6..9` KULLANILMIYOR:** kendi ALTIN ÇERÇEVESİYLE geliyorlar,
  bizim kare çerçevemizin içine konunca çift çerçeve oluyor.
- Elde 3 çerçevesiz rakip yüzü var, 7 slot (3 podyum + 4 satır) — tekrar
  kaçınılmaz. Tekilleştirmek için 4 tane daha ÇERÇEVESİZ avatar gerekir.

**Kaçan iki Türkçe metin bulundu ve çevrildi:** `DailyRewardPanel`'de
"80 JETON AL" → "CLAIM 80", `GameplayScreen`'de "+N jeton" → "+N".
İkisinde de ç/ğ/ı/ö/ş/ü yok — dosyanın yukarısındaki uyarı tam da bunu
söylüyordu, yine de kaçmışlar. Bir sonraki taramada kelime listesiyle git.

## MEKANİK DENETİM — başladı (2026-08-16)

Görsel kıyas bitti; bu bölüm "neye basınca ne oluyor" sorusunu izliyor.
Yöntem: referansı `fps=2` kontakt sayfasıyla tara, sonra kodda karşılığını ara.

### Yolculuk — referansta doğrulananlar (bizde ZATEN doğru)
- **"Üst"/"Alt" düğmeleri GÖRÜNTÜ ALANINA sabit**, içerikle kaymıyor —
  her karede aynı ekran konumunda duruyorlar. Bizde de öyle.
- Ekran açılınca **oyuncunun bulunduğu yere kaydırılmış** geliyor.
- Bölge adı çemberin DIŞINDA üstte; aralık etiketi ("sv 41 - 70") çemberin
  üst kenarına biniyor; pembe seviye rozeti tüpün üstünde.
- Üç bölge durumu: kilitli (gri + altın kilit + "Seviye N"), açık (yeşil
  düğme), tamamlanmış (yeşil tik).

### BULUNAN ÜÇ ÖLÜ KONTROL

**DERS (düğme gibi duran şey BİR ŞEY YAPMALI):** Üçü de ekranda parlak,
tıklanabilir ve "çalışıyormuş" gibi duruyordu. Basınca hiçbir şey olmayan
bir düğme, eksik bir özellikten beterdir — oyuncu oyunu bozuk sanır.
Tarama yöntemi basit ve tekrarlanabilir: her dosyada düğme üreten çağrı
sayısıyla `onClick.AddListener` sayısını karşılaştır.

1. **Yolculuk bölge düğmesi — DÜZELTİLDİ, play modunda doğrulandı.**
   Dosyadaki tek gerçek `Button` atlama düğmeleriydi; bölge çemberindeki
   yeşil düğmenin `onClick`'i HİÇ bağlanmamıştı. Artık
   `JourneyScreen.PlayRegion(from, to)`: oyuncunun ilerlemesi bölge
   aralığına KIRPILIYOR — içinde bulunduğu bölge "kaldığın yerden devam",
   tamamladığı bölge "o bölgenin son bölümünü tekrar oyna".
   Doğrulama: 5 bölgenin de Button'ı var; tıklayınca `InGame=True`,
   `LastPlayedLevelIndex=10` (seviye 11, 1-20 aralığına kırpılmış).

2. **Ayarlar "Delete My Account" — DÜZELTİLDİ ve 2026-08-17'de ELLE
   DENENDİ** (bkz. aşağıdaki "HESAP SİLME" bölümü). Düğme bile değildi,
   yalnız kapsül + yazıydı.
   Artık gerçek düğme ve **iki aşamalı**: ilk dokunuş kırmızıya dönüp
   "Tap again to erase everything" diyor, ikinci dokunuş `Save.Reset()`
   çağırıp ana ekrana dönüyor; 5 saniye içinde onaylanmazsa kendiliğinden
   geri alınıyor. Ayrı onay penceresi kurmadım — yıkıcı işlem için iki
   dokunuş yeterli koruma ve yeni bir pencere düzeni gerektirmiyor.

3. **Liderlik sekmeleri KOZMETİKTİ — DÜZELTİLDİ, play modunda doğrulandı.**
   `Weekly / World / Country` üçü de bağlıydı ama `_activeTab` yalnız sekme
   RENGİNİ değiştiriyordu; podyum ve satırlar üç sekmede de birebir aynıydı.
   Üç ayrı kapı açıp üçünü de aynı odaya çıkarmak, düğmeyi hiç koymamaktan
   daha kötü — oyuncu bir süre farkı arayıp oyunu bozuk sanıyor.

   Artık `Boards[3][8]` var (haftalık düşük puanlı, dünya en yüksek, ülke
   ortada) ve `Refresh` podyum adlarını + 4 satırın ad/puan/seviye
   yazılarını yeniden yazıyor. `BuildRowContent` artık puan yazısını da
   döndürüyor. Doğrulama (sekme sırasıyla podyum 1-2-3):
   `mira·kret·Bet` / `aisha·Bet·zzz` / `polat·nurhayat·kret`, satırlar da
   değişiyor, "You" satırı üçünde de en altta sabit.

4. **`LeaderboardScreen.Score()` kayıt dosyasını ŞİŞİRİYORDU — DÜZELTİLDİ.**
   Puanı hesaplarken BÜTÜN bölümler için `progress.Record(id)` çağırıyordu;
   o metot bulamadığını YARATIP kayda yazıyor. Yani liderlik ekranı her
   tazelemede oynanmamış her bölüm için kayda boş bir satır ekliyordu.
   `ProgressService.Peek(levelId)` eklendi (yoksa `null`, hiçbir şey
   yaratmaz) ve `Score` ona geçirildi.
   **Bu, `PreviewReward`'da düzeltilen tuzağın İKİNCİ örneği** — bu depoda
   `Record()` bir "getir ya da yarat"; yalnız okuyacaksan `Peek` kullan.

5. **Sekme geçişi ANLIKTI — DÜZELTİLDİ, play modunda doğrulandı.**
   2 kare/sn taramada ara kare görünmediği için "anlık" sanılmıştı; **30
   kare/sn** ile bakınca referansın kaydırdığı ortaya çıktı (35-37. sn,
   Yolculuk → Ana Ekran): yeni ekran yandan giriyor, eski ekran karşı
   yönden çıkıyor, süre **~170 ms** (~5 kare).

   `Juice.SlideX` eklendi (proje kuralı gereği doğrudan PrimeTween değil,
   cepheden) ve `MenuShell.SlideSwap` yönü SEKME SIRASINDAN hesaplıyor —
   sağdaki sekmeye geçerken yeni ekran sağdan giriyor. Çıkan ekran kayma
   bitince kapanıyor ve `anchoredPosition` sıfırlanıyor (yoksa bir dahaki
   açılışta ekran dışında kalırdı).

   Doğrulama: home→store→board→journey→home dizisi; kayma sırasında çıkan
   ekran açık kalıyor, kareler geçince HEPSİ kapanıp x=0'a dönüyor;
   tek geçişte hedef ekran aktif ve x=0.

   **Ana ekran da geçişe KATILDI** (aynı oturumda tamamlandı):
   `HomeScreen.Slide(fromX, toX, süre)` + `HomeScreen.Instance`.
   Manzara (`Background`, kanvasın çocuğu) ile arayüz (`SafeArea`) AYRI
   ebeveynlerde olduğu için İKİSİ birden kaydırılıyor — yalnız birini
   kaydırmak "arayüz kaydı ama dünya durdu" gibi görünürdü.
   Doğrulama: `home→journey→home→store→home` hızlı dizisinden sonra
   `Background x=0`, `SafeArea x=0`, bütün menü ekranları kapalı ve x=0 —
   kayma BİRİKMİYOR, ekran dışında takılan yok. Ana ekran görsel olarak da
   yerinde yakalandı.

### DÖRT ÖLÜ KONTROL DAHA (2026-08-17) — hepsi düzeltildi, play modunda sayıyla doğrulandı

Tarama yöntemi aynı: her dosyada düğme üreten çağrı sayısıyla
`onClick.AddListener` sayısını karşılaştır. Ama bu turda asıl av **sayımın
GÖRMEDİĞİ** yerden çıktı: düğmesi olan ama işi YARIM yapan kontroller.

**6. MENÜDE HİÇ SES YOKTU.** `AudioService` ve `Haptics`, `GameSession`'ın
kurulumunda doğuyordu. Oynanış kökü kapalı başladığı için `Start` hiç koşmuyor
— yani oyuncu **ilk bölüme girene kadar** sahnede AudioService YOKTU:
ana ekranın, mağazanın, ayarların bütün düğmeleri sessiz basılıyordu
(`UiButtonFeel.Clicked` bağlanmamış kalıyor). Üstelik Ayarlar'daki "Sounds"
anahtarı uygulanacak örnek bulamadığı için sessizce hiçbir şey yapmıyordu.
İkisi de `AppRoot.Awake`'e alındı; `GameSession` artık devralıyor, ikinci
örnek kurmuyor. Doğrulama (ana ekranda, hiç bölüme girmeden):
sahnede **1 AudioService, 1 Haptics**, `UiButtonFeel.Clicked` bağlı.

**DERS (menü de oyunun parçası):** Servis "oyun başlayınca" değil "uygulama
açılınca" kurulmalı. Cila servislerini oynanışın kurulumuna asmak, menüyü
sessiz bir maket hâline getiriyordu ve bu aylarca fark edilmedi çünkü test
hep bir bölüme girerek yapılıyordu.

**7. Ayarlar'daki Müzik ve Haptik anahtarları KAYDA yazıyor, SERVİSE
uygulamıyordu.** Oyuncu haptiği kapatıyor, telefon titremeye devam ediyordu;
ayar ancak uygulama yeniden açılınca tutuyordu. Duraklat panelindeki AYNI
anahtarlar ise `SettingsBinder` üzerinden gidiyordu. Ayarlar da oraya bağlandı.

**DERS (ayarı KAYDA yazmak, ayarı UYGULAMAK değildir):** Aynı ayarın iki
yazma yolu varsa biri er geç eksik kalır. Yazma + uygulama tek elden gitmeli.

**8. Aynı görünen anahtar iki ekranda FARKLI davranıyordu.** Ayarlar'da
yuvanın tamamı tek düğmeydi ve durumu TERS ÇEVİRİYORDU — yani yeşil "On"
yarısına, üstünde **On yazan yere** basmak ayarı KAPATIYORDU. Duraklat
panelinde ise iki yarı ayrı ayrı basılıyordu (doğrudan-durum). Ayarlar da
doğrudan-duruma geçti; iki yarının çakışan 0.04'lük bandı da temizlendi.
Doğrulama (Haptics satırı): açıkken On → açık kalıyor; Off → kayıt VE servis
kapanıyor; kapalıyken Off → kapalı kalıyor; On → ikisi de açılıyor.
Sounds için de `Audio.Muted` ters yönde doğru izliyor.

**9. Profil'deki yeşil kalem rozeti ÖLÜYDÜ** — altında `Button` bile yoktu.
Daha kötüsü: `SaveData.PlayerName` alanı kayıtta duruyor, ana ekranın avatar
baş harfini ve liderlik tablosundaki oyuncu satırını besliyordu, ama oyunda
o adı yazabileceğin **tek bir yer yoktu**; alan hep boş kalıyor, her yerde
"Player" / "You" görünüyordu. `NamePanel.cs` eklendi (koyu perde, mor kart,
üst kenara binen "Edit Name", `TMP_InputField`, yeşil Save, kırmızı çarpı).
Doğrulama: kaleme basınca panel açılıyor, alan kayıttaki adla doluyor,
boş ad REDDEDİLİYOR (panel açık kalıp alan sarsılıyor), "  Fikret  " →
kırpılıp `Fikret` olarak kaydediliyor, Profil kartı ve liderlik satırı
anında güncelleniyor.

> **Referanstan ÇIKARILAMADI:** Videoda oyuncunun adı "Fikret" ama kaleme
> hiç dokunulmuyor — bu pencerenin referanstaki hâli GÖRÜLMEDİ. Düzen
> ölçülmedi, projenin kendi menü dilinden kuruldu. Kare bulunursa ölçüler
> oradan düzeltilmeli.

**Ana ekrandaki avatar baş harfi bir HATA DEĞİL:** `_avatarInitial` yalnız
`avatar_player` görseli YOKSA kuruluyor; görsel geldiği için o etiket hiç
doğmuyor. Ad ana ekranda zaten gösterilmiyor.

### HESAP SİLME — elle denendi, ÜÇ GÜVENCE DE ÇALIŞIYOR (2026-08-17)
DEVAM'da "derleniyor ama ELLE DENENMEDİ" diye duran madde kapandı.
Kayıt dosyası önce yedeklendi, test sonrası geri yüklendi (bölüm 11, 1320 jeton).
- İlk dokunuş yalnız KURUYOR: yazı "Tap again to erase everything", zemin
  kırmızı, jeton 1320 — hiçbir şey silinmiyor.
- İkinci dokunuş gerçekten siliyor: jeton 1320→0, açık bölüm 11→1, ad boşalıyor,
  ana ekrana dönülüyor.
- **5 saniye zaman aşımı çalışıyor:** t=164.5'te kuruldu, t=180.8'de ekran hâlâ
  açıkken yazı kendiliğinden "Delete My Account"a dönmüştü.
- **Ekrandan çıkmak da iptal ediyor** (`OnDisable`): kurup ekranı kapatıp
  açınca yazı geri dönüyor ve sonraki TEK dokunuş silmiyor, yeniden kuruyor.

### OYNANIŞ İÇİ DENETİM — başladı (2026-08-17)

Bugüne kadar mekanik denetim hep MENÜLERİ taramıştı. Oynanış tarafına ilk
bakış: HUD'daki bütün düğmelerin (yeniden başlat, duraklat, üç yardımcı,
panel düğmeleri) dinleyicisi var, ölü kontrol YOK. Yardımcı sistemi de
sağlam çıktı — elde yoksa jetonla satın alıyor, aynı düğmeye ikinci basış
seçimi iptal ediyor.

**Bir "yarım" geri bildirim bulundu ve düzeltildi.** Jetonu yetmeyen oyuncu
bir yardımcıya bastığında tek geri bildirim, yardımcı çubuğunun üstünde
beliren 28 puntoluk bir satırdı ("Not enough coins.", 2.5 sn). Oyuncunun
gözü o an bastığı DÜĞMEDE; ekranın başka yerinde beliren küçük bir yazıyı
çoğu zaman görmüyor ve "bastım, bozuk" diye okuyor.

`PowerUpSystem` artık ayrı bir `Refused(PowerUpKind)` olayı yayınlıyor;
`GameplayScreen` buna red sesi + titreşim + **reddedilen düğmenin sarı fiyat
kapsülünü sarsma** ile karşılık veriyor. Reddi reddedilen şeyin ÜSTÜNDE
göstermek gerekiyordu; sarsılan şeyin FİYAT olması sebebi de anlatıyor.

Doğrulama (play modunda, jeton 0): UFO'ya (elde 0, fiyat 1200) basınca
`Refused` **tam 1 kez**, kind=**Ufo**, `Pending` null, jeton değişmedi.
Elde roketi OLAN oyuncu roketе basınca `Refused` **0 kez**, `Pending=Rocket` —
yani red yalnız gerçekten reddedilende çalışıyor.

**Tuzak:** `Haptics.Threshold` varsayılan olarak `Medium`'dur ve altındaki
her şeyi sessizce yutar. `HapticStrength.Light` yazmak "titreşim ekledim"
sanıp hiç titretmemek olurdu.

### BÖLÜM ORTASINDA ÇIKIŞ — CİDDİ SIZINTI BULUNDU ve DÜZELTİLDİ (2026-08-17)

**Yardımcı istemi açıkken menüye dönen oyuncunun BİR SONRAKİ BÖLÜMÜ sakat
başlıyordu.** Ölçüm (yeniden girilen bölümde):

| | önce | sonra |
|---|---|---|
| HUD alfası | **0** (görünmez VE tıklanamaz) | 1 |
| yardımcı istemi paneli | **açık**, yeni tahtanın üstünde | kapalı |
| zemin karartması | **duruyor** | temiz |
| `PowerUps.Pending` | **Rocket** (asılı kalmış) | null |

**DERS (doğru temizlik, YANLIŞ kanca):** Bu temizliğin bir kısmı zaten
`GameplayScreen.OnDestroy` içindeydi ve yorumunda aynen şu yazıyordu:
*"Zemin karartması KALICI: istem açıkken bölüm bırakılırsa zemin karanlık
kalırdı."* Yani sorun GÖRÜLMÜŞ, çözüm yazılmış — ama hiç çalışmamış. Tek
sahnelik yapıda bu ekran **hiç yok edilmiyor**; `AppRoot.ShowMenu` yalnızca
oynanış kökünü kapatıyor, `OnDestroy` ancak uygulama kapanırken koşuyor.
Doğru kanca `OnDisable`. Bir temizliği yazmak yetmez, **çalıştığı anı da
doğrulamak** gerekir.

`Refresh` bunu kurtaramaz: durum geçişini görmesi için bir kare koşması
lazım, oysa kök aynı karede kapanıyor.

Artık `GameplayScreen.OnDisable` bütün geçici durumu sıfırlıyor (istem,
duraklat, sonuç, teklif panelleri; HUD ve yardımcı düğmelerinin alfası;
zemin karartması; `_shownState`, `_offerShown`, ipucu). `GameSession.StopLevel`
de bekleyen yardımcıyı iptal ediyor. Doğrulama: çıkışta dördü de temiz,
yeniden girilen bölümde HUD alfa 1, üç yardımcı düğmesi alfa 1 ve raycast
açık, dört panel de kapalı, `Pending` null.

**Yan bulgu — isim çakışması:** HUD'daki duraklat DÜĞMESİ de duraklat PANELİ
de "Pause" adını taşıyordu ve düğme hiyerarşide önce geliyordu. İsimle arayan
her teşhis "panel açık kalmış" diye yanlış okuyor (bu oturumda bir kez
düşüldü). Panel `PausePanel` olarak yeniden adlandırıldı.

### KALAN İKİ SENARYO — kodda zaten karşılanıyor

- **Sürükleme sırasında duraklatma / süre dolması.** `DragController`
  `() => State == GameState.Playing` yüklemini iki yerde birden okuyor:
  `OnPointerDown` sürüklemeyi hiç başlatmıyor, `OnPointerHeld` ise devam eden
  sürüklemeyi `EndDrag()` ile temiz bırakıyor
  (`DragController.cs:129`, yorumu da bunu söylüyor). Blok elde asılı kalmıyor.
- **Süre bitmişken yardımcı kullanmak.** Teklif paneli açılırken
  `SetOfferDim(true)` üç yardımcı düğmesinin `blocksRaycasts`'ini kapatıyor,
  yani basılamıyorlar. Hedef seçimi de mümkün değil: `BlockTapped` yolu aynı
  `_canDrag()` yükleminden geçiyor ve durum `Lost`.

### AÇIK KALAN (küçük)
Ayarlar'daki **Support / Terms / Privacy** düğmeleri `example.com` adreslerini
açıyor. Ölü değiller ama gittikleri yer yok. Portfolyo için doğrusu GitHub
depo sayfası olabilir — karar kullanıcının.

### MAĞAZA TAŞIYICISI — kontrol edildi, SORUN YOK
Referansta 8 saniye boyunca (7-15. sn, 4 kare/sn) teklif kartı ilk sayfada
kaldı, noktalar hiç değişmedi: **kendiliğinden dönmüyor.** Bizimki de
dönmüyor — burası zaten doğru, değişiklik gerekmedi.

### VİDEODAN ÇIKARILAMAYANLAR (mekanik)
Aşağıdakiler için referansta kimsenin o şeye DOKUNDUĞU bir an yok; video
bakarak cevaplanamaz, ancak oyunun kendisi kurulup denenirse bilinir:
- Kilitli bölgeye dokunmak bir şey yapıyor mu (bizde düğme gizli, hiçbir şey).
- Kilometre taşı kapsülleri tıklanabilir mi (bizde değil; bilgi taşıyorlar).

### HENÜZ BAKILMADI (mekanik)
- Oynanış İÇİNDEKİ mekanik denetim hiç yapılmadı: yardımcıların iptali,
  sürükleme sırasında duraklatma, süre bitince yardımcı kullanmak, bölüm
  ortasında ana ekrana dönüp geri gelmek.
- Mağazanın oynanışın ÜSTÜNE açılan bir kipi yok; referansta kaybetme
  panellerinden mağazaya gidilebiliyor (bkz. yukarıdaki bölüm).

### AÇIK MODEL SORUSU — panel EN-BOY oranı
Referans cihaz 2.165, bizim yakalama 1.78. Kart genişliği ekran ORANI olarak
verildiği için bizim kart göreli olarak daha geniş/bodur çıkıyor ve üstte
boşluk varmış gibi duruyor. Kart-göreli ölçüldüğünde yerleşim referansla
ÖRTÜŞÜYOR — yani hata yerleşimde değil, yakalama oranında. Yine de
referansın kendi ölçekleme modeli (yükseklik mi genişlik mi kilitli)
bilinmiyor; iki referans video da aynı oranda olduğu için videodan
çıkarılamıyor. Gerçek cihazda (2.0-2.2) bakılıp karar verilmeli.

### HENÜZ KIYASLANMAMIŞ (sıradaki iş)
- Oyun içi panellerden duraklat, PERFECT, yardımcı istemi ve BAŞARISIZ **BİTTİ**.
- **MEKANİK kıyas hiç yapılmadı**: Yolculuk neye basınca kayıyor, bölge
  çemberine dokunulunca ne oluyor, kilitli bölge tıklanabilir mi, sekmeler
  arası geçiş animasyonu var mı, mağaza taşıyıcısı otomatik dönüyor mu…

Referans kareler `C:/Users/CPN12/AppData/Local/Temp/claude/.../scratchpad/shop/j/`
altında; kaybolursa `menus,powerups,vs.mp4` içinden şu saniyelerden çıkarılır:
Mağaza 8-14 · Liderlik 16-22 · Yolculuk 24-42 · Koleksiyon 43-45 ·
Ayarlar 54-56 · Profil 57-60.

---

## 2026-08-18 (akşam) — 2. TUR KAPANDI (56/56), 3. TUR BAŞLADI

### Durum

`docs/APK-BULGULARI-2.md` — **56/56 madde kapalı.** Aynı dosyanın SONUNDA
"3. TUR" bölümü var; oradan devam edilecek.

Son commitler: `1d05035` (denetim), `12ee4c8` (3. tur A/B/C),
`203e2da` (2. tur kapanış), `c0c56a2` (logolar + kutlama).

### Kullanıcı ne yapıyor

Yeni bir APK aldı ve test ediyor; **çok sayıda not çıkardı, yarın sabah
hepsi tek tek yapılacak.** Yeni oturumda ilk iş: kullanıcının listesini al,
`docs/APK-BULGULARI-3.md` diye numaralandırılmış bir dosya aç ve
2. turdaki düzeni birebir uygula (her madde: NE YAPILDI + NASIL DOĞRULANDI,
ölçümler referans karelerden).

### Yarın sabah devralınacak AÇIK İŞLER

1. **27 bölümde süre yetmiyor** (karar kullanıcının, ölçüm hazır).
   Çözücünün bulduğu çözüm verilen süreye sığmıyor. En kötüleri:
   `level_038` 351/180 (**171 sn eksik**), `041` 295/180, `049` 271/180,
   `030` 223/150, `026` ve `034` 247/180, `047` 239/180.
   Tam tablo: `APK-BULGULARI-2.md` → "3. TUR" → bölüm D.
   Süreler TASARIM kararı olduğu için değiştirilmedi.

2. **`DeviceErrorOverlay.Enabled = true`** — `Runtime/Flow/DeviceErrorOverlay.cs`
   satır 33. Yayına çıkarken `false`. Şu an bilerek açık (test için).

3. **Kürsü görseli referansla birebir değil.** Referansta minder parlak
   magenta ve puf gibi; gövdenin ön yüzünde ismin asıldığı SARKAN BİR BAYRAK
   var ve madalya ondan sarkıyor. Bizimkinde minder koyu mor kadife, bayrak
   yok — ad levhası kapsül olarak minderin üstüne biniyor. İstenirse istem
   yeniden yazılmalı: *parlak magenta puf minder + ön yüzde sarkan bayrak*.

4. **Emme kırıntısı ekranda doğrulanmadı.** Boyu (0.18-0.42) ve sayısı
   (30 + alan×8) referanstan ölçülüp koda yazıldı ama gerçek bir emilme
   yasal bir hamle gerektirdiği için editörde tetiklenemedi. Kullanıcının
   test ettiği APK'de ilk kez görülecek — oranlar tutmazsa ölçüme göre
   yeniden ayarlanacak (ölçüm: parça kenarı hücrenin %40-70'i).

### Bu oturumda yapılan 3. tur işleri (bitti)

- **A · Kapı açıklığından %25 dar çiziliyordu.** `GateView.ResolveSpan`
  köşeye dayanan ucu 0.45 hücre içeri alıyordu; 2 hücrelik kapı 1.5 hücre
  görünüyordu. Bar 1.50 → 1.960. Bölüm verisinde ihlal YOK (50 bölüm
  tarandı), sorun tamamen çizimdeydi.
- **B · Kapı artık sönerek ghost'a dönüyor** (0,34 sn, renk+alfa birlikte).
- **C · Emme efekti**: blok beyaz konturla parlıyor, kırıntı irileşti.

### Denetim aracında düzeltilen hata

`LevelValidationTool.WarnUnreachableIce` üreteç kuyruklarını ve katman
soyulmalarını saymıyordu; `level_037` için üç sahte "buz hiç kırılmaz"
uyarısı veriyordu. Düzeltildi, sahte uyarı 3 → 0. **Bölüm verisi temiz:**
683 blok, 16 polyomino, çözücü 50/50, 0 hata.

### Uyarılar (değişmedi)

- **Level editörüne DOKUNMA** (`Scripts/Editor/LevelEditor/`) ve
  `Runtime/Debug/`, `GameKit/Runtime/DevTools/` — kullanıcının paralel işi.
- **`git add -A` KULLANMA.** Yalnız kendi dosyalarını tek tek stage'le.
- Kullanıcının dosyaları yarım kaldığında derleme kırılabiliyor; bu
  oturumda dört kez oldu. Beklemek yeterli, düzeltmeye kalkışma.

---

# Grand Games arayüz dili turu (2026-08-22, gece)

## Ne yapıldı

**1. Referans malzemesi çıkarıldı.** Block Out! iOS'a özel ve App Store paketleri
FairPlay ile şifreli — oyunun kendi dosyalarına erişilemiyor, Android sürümü de
yok. Bunun yerine aynı stüdyonun Android'de yayınlanan oyunu **Magic Sort!**
(`com.grandgames.magicsort` 0.0.3011) çıkarıldı: 5773 varlık (5092 sprite,
668 doku, 13 font).

Araçlar: `_Reference/rip.py` (tek dosya), `_Reference/rip2.py` (klasör bütün
olarak — akış verisi ancak böyle çözülüyor; tek tek yüklemek 105 dosya
verirken bütün yüklemek 5773 verdi), `_Reference/pull.sh` (adb, kullanılmadı).

`_Reference/` gitignore'lu. Telifli; oradan bakıp kendi sürümümüzü üretiriz.

**2. Ölçümler `docs/UI-DILI.md`'ye yazıldı.** Font (Freight Sans Black — ama
Block Out! onu KULLANMIYOR), renkler, biçim reçeteleri, atlas haritası.

**3. Kök sebep bulundu.** `UiSprites.RoundedPanel` saf beyaz bir maske
(`new Color(1f,1f,1f,alpha)`); `CreateRoundedPanel` onu tek renkle boyuyor.
Yani bütün arayüzümüz DÜZ renkli yuvarlak dikdörtgen. Referansta hiçbir düğme
düz değil: kalın koyu kontur + üstte parlak bant + ortada koyu + altta koyu
bant. Ayrışmanın kök sebebi bu.

**4. Reçete koda geçirildi.**
- `UiSprites.ButtonFace` — ölçülen dikey parlaklık profilini RGB'ye pişiren
  yeni 9-dilim sprite. Renkle boyanınca çarpma gradyanı üretiyor.
- `UiKit.CreateBeveledButton` — kontur (ayrı katman, çünkü rengi ana renkten
  bağımsız: üç renkte de `#121133`) + pahlı yüz.
- Mevcut `RoundedPanel`'e DOKUNULMADI; kırk kadar `SetSliceScale` çağrısı
  olduğu gibi geçerli.

Görsel doğrulama: aynı matematik Python'da çalıştırılıp referansla yan yana
konuldu (`_Reference/notes/cmp_bevel.png`). Yapı tutuyor.

## Sıradaki — karar bekleyen

**A. `UiCornerFit.HouseShare` %22 → %35.** Referans ölçümü `UiSprites.cs`'teki
yorumda ZATEN yazılı ("kısa kenarın %35'i… bizim ev oranımız %22") ama sabit
değiştirilmemiş. Önizlemede %35 referansa belirgin biçimde daha yakın çıktı.
Tek satır ama `UiCornerFit` kullanan her yüzeyi etkiliyor — Unity'de ekran
ekran bakmadan değiştirmedim.

**B. `CreateBeveledButton` çağrı yerlerine bağlanmalı.** Şu an eklendi ama
hiçbir yerden çağrılmıyor. Duraklat paneli ve mağaza düğmeleriyle başlanmalı.

**C. `btn_purple/green/red/square.png` ÖLÜ.** `UiSkin.Art.Button*` sabitleri
hiçbir yerden kullanılmıyor. Ya silinmeli ya bağlanmalı.

**D. Block Out!'un fontu teşhis edilmedi.** Freight Sans Black değil; yuvarlak
uçlu ağır bir sans. `_Reference/store/` içindeki 1320 px görsellerden
çıkarılmalı.

### Aynı gece — A ve D maddeleri kapandı

**A kapandı.** `HouseShare` %22 → **%35**, `MaxRadius` 34 → **36**. Gerekçe
`UiCornerFit.cs`'te uzun uzun yazılı. Özet: eski %22 değeri WhatsApp'tan geçmiş
946 px JPEG'lerdeki KÜÇÜK elemanlardan ölçülmüştü; yeni ölçüm 1320 px kayıpsız
PNG'den ve dayanıklı yöntemle (sınır kutusu köşesine en yakın piksel uzaklığı =
r(√2−1)). Düğme yüzleri %33-35 çıktı. Kanvas birimine çevrilince yarıçap 35-37;
yani tavan zaten doğruydu ama %22 ile TAVAN HİÇ DEVREYE GİRMİYORDU.

**D kapandı — ama olumsuz sonuçla.** Block Out!'un fontu teşhis EDİLEMEDİ. On bir
aday ölçülüp elendi; hiçbiri referansın "düz ayaklı 1"ini vermiyor. Muhtemelen
ticari bir font. En yakın ücretsiz karşılık Baloo2 / BalooPaaji2 ExtraBold
(en/boy 3.145 vs referans 3.111). Ayrıntı ve tablo `docs/UI-DILI.md`'de.
Adaylar `_Reference/fonts/` altında indirilmiş halde duruyor.

**Hâlâ açık: B ve C.** `CreateBeveledButton` hiçbir yerden çağrılmıyor;
`btn_*.png` hâlâ ölü.

**HİÇBİRİ UNITY'DE DERLENMEDİ.** Üç dosya değişti — `UiCornerFit.cs`,
`UiKit.cs`, `UiSprites.cs`. Ayraç dengesi HEAD'e karşı denetlendi (tutuyor) ama
bu derleme garantisi değil. İlk iş Unity'yi açıp konsola bakmak.

Bu oturumda DEĞİŞTİRİLENLER (başkasınınkilere dokunulmadı):
`.gitignore`, `UiCornerFit.cs`, `UiKit.cs`, `UiSprites.cs`, `docs/DEVAM.md`,
`docs/UI-DILI.md` (yeni), `tools/make_buttons.py` (yeni).

### Park edilmiş seçenek: Car Match

`com.grandgames.carmatch` ("Car Match - Traffic Puzzle") — Grand Games'in
Android'deki ikinci oyunu. İNDİRİLMEDİ, gerekirse bakılacak.

**Tek gerçek getirisi font adı.** TMP atlasları font adını dosya adında taşıyor
(`Freight-SansBlack SDF Atlas` gibi). Car Match'te "düz ayaklı 1"i olan bir font
çıkarsa Block Out!'un fontu adıyla teşhis edilmiş olur.

Magic Sort'un üç fontuna da bakıldı — Freight Sans Black (rakam atlası net
okunuyor: `9527 684 031x`, "1"de ayak YOK), FF Meta Pro Black, gribley-big.
Hiçbiri tutmuyor.

İkincil getiri: iki örnek, bir şeyin "Grand Games ev stili" mi yoksa "o oyunun
kendi sanat yönetimi" mi olduğunu ayırt etmeyi sağlar. Tek örnekle ayırt
edilemiyor.

**Getirisi OLMAYAN yer:** düğme/panel/kapsül/renk reçeteleri. Onlar Block Out!'un
kendi 1320 px görsellerinden doğrudan ölçüldü; kardeş oyundan çıkarım yapmak
daha kötü bir kaynak olur.

Boru hattı hazır: paket `_Reference/apk/` altına atılır, önce `bin/Data` diske
açılır, sonra `_Reference/rip2.py` klasörü BÜTÜN olarak yükler (tek tek yüklemek
akış verisini çözemiyor).

### DÜZELTME — aynı gece, kod değişiklikleri GERİ ALINDI

Unity'de derleme temiz çıktı (hata yok, uyarıların hiçbiri bu turdan değil).
Ama düzenleyici kipinde yakalama alıp ölçünce iki hata bulundu, ikisi de bende.

**1. `HouseShare` %35 değişikliği YANLIŞTI — kapsam hatası. Geri alındı (%22).**
Düğmeleri ölçüp düğme-olmayanlara uygulamışım:
- Düğmeler bu sabiti kullanmıyor; kendi sabitleri var:
  `MenuPage.ButtonCornerShare = 0.354`. Zaten doğruydular.
- `HouseShare` 73 `CreateRoundedPanel` çağrısının 66'sını, yani panel/kart/kuyu
  yüzeylerini besliyor. Onların referanstaki oranı DÜŞÜK: booster kapsülü
  189×182 px'te 45 px = %24. Eski %20-22 ölçümleriyle tutuyor.

Gerekçe `UiCornerFit.cs`'e ders olarak yazıldı. Ölçüm doğruydu, kaynak iyiydi,
yöntem sağlamdı — eksik olan tek şey "bu sabite hangi yüzeyler bağlı" kontrolü.

**2. `UiSprites.ButtonFace` + `UiKit.CreateBeveledButton` GEREKSİZDİ. Silindi.**
`MenuPage.PillButton` zaten tam bir pahlı düğme kuruyor: dış çizgi + kabuk
(üstte ışık, dipte etek) + kaymak halkası + yüz, hepsi ölçülmüş sabitlerle
(`ShellTop 1.19`, `ShellBottom 0.22`, `RimBottom 0.50`, `FaceBottom 0.78`,
`OutlineTone 0.15`). Projede var olanı ikinci kez yazmışım — `UiSkin`,
`UiSprites`, `UiKit`'e bakıp `MenuPage`'e bakmamışım.

`tools/make_buttons.py` da bu yüzden ölü; siliniebilir.

**3. Yakalama aracının zemin rengi ölçümü kirletiyor.**
`UiCaptureTool` kamerayı `Color(0.05, 0.03, 0.12)` = `#0D081F` ile temizliyor.
Bir ara "zeminimiz referanstan çok koyu" diye ölçtüm — aracın kendi rengini
ölçmüşüm. Oyunun zemini `UiKit.Background = #211A47` ve referansın
`#1B1940`–`#262157` aralığının tam içinde. Zemin sorunu YOK.

### GERÇEKTEN AÇIK KALAN TEK ŞEY: düğmenin ışık profili

Bizim Resume düğmesi ile referans düğmesi, ikisi de kendi orta tonuna
normalize edilerek ölçüldü (bizimki düzenleyici yakalamasından, referans
1320 px kayıpsız PNG'den):

| konum | referans | bizim | fark |
|---|---|---|---|
| %0 yüzün üst kenarı | 0.69 | 1.20 | **+0.51** |
| %13 tepe | 1.18 | 1.28 | +0.10 |
| %16 | 1.10 | 1.17 | +0.07 |
| %64 orta | 1.00 | 1.18 | +0.18 |
| %87 | 1.06 | 0.93 | −0.13 |
| %89 | 0.57 | 0.91 | **+0.34** |
| %100 dip | 0.28 | 0.34 | +0.06 |

Tepe ve dip TONLARI tutuyor. İki yapısal fark var:

1. **Üst dudak yok.** Referansın yüzü üstte KOYU başlıyor (0.69) ve %13'te
   tepeye çıkıyor. Bizde dudak yok, doğrudan parlak başlıyoruz (1.20).
2. **Dip eteği fazla ince.** Referansta koyulaşma %89'da başlıyor; bizde
   ~%97'ye kadar inmiyor.

Bunlar `MenuPage`'in halka payları (`FaceInset`, `RimInset`, `ShellInset`) ve
tonlarıyla (`RimBottom`, `FaceBottom`) ayarlanır. DEĞİŞTİRİLMEDİ — bu turda bir
kere kapsam hatası yapıldığı için körlemesine ikinci bir dokunuş yapılmadı.

**Doğrulama döngüsü artık var:** `UiCaptureTool` ile düzenleyici kipinde
yakala → yeşil pikselleri izole et → dikey profili orta tona normalize et →
yukarıdaki tabloyla karşılaştır. Değişiklik profili referansa yaklaştırıyorsa
iyileşme, yaklaştırmıyorsa geri al.

### Doğrulanmış iki düzeltme (mağaza jeton sayacı)

Yöntem her ikisinde de aynı: değiştir → `UiCaptureTool` ile düzenleyici kipinde
yakala → aynı ölçümü referansa ve bize uygula → sayıyla karşılaştır.

**1. `StoreScreen.PillX1` 0.352 → 0.296.**
Kapsülün sağ kenarı. Koddaki eski yorum referans için 0.338 diyordu; aynı
`market.jpeg` üzerinde krem kapsülün BİTİŞİK KOŞUSU taranınca 0.288 çıktı.
Referansta üç haneli "142" varken bile o kadar, bizimki tek haneli "0" ile
0.344'e uzuyordu.
Doğrulama: yakalamada sağ kenar 0.344 → **0.288**, referansla farkı 0.056 → **0.000**.

**2. `Cocoa` → `CoinInk`, #532A0D → #1B255C.**
Jeton sayacının rakam rengi KAHVERENGİYDİ, referansta LACİVERT. `market.jpeg`
üzerinde "142" rakamlarının koyu piksellerinin ortancası #1B255C.
Doğrulama: yakalamada rakam rengi #532A0D → **#1C265B** (hedef #1B255C).
Kapsam kontrol edildi: `Cocoa` projede yalnız bu tek yerde kullanılıyordu.

### Mağaza ekranının tamamı karşılaştırıldı

Bizimki referansa ÇOK YAKIN: aynı tente, aynı tuğla duvar, aynı kart yapısı,
aynı bölüm başlıkları, aynı renk ailesi. Kalan farklar iki grupta:

**İllüstrasyon (kod değil, sanat):** referansın teklif kartında zengin bir
hazine sandığı var, bizde düz altın yığını. Kart içeriği de farklı (avatar +
kalp sayısı). Hissedilen "ayrışma"nın büyük kısmı burada.

**Kalan küçük kaplama farkları (ölçülmedi, DEĞİŞTİRİLMEDİ):**
- Bizim jeton kapsülünde kalın koyu kontur var, referansta YOK. Ama koddaki
  yoruma göre bunu kullanıcı bilerek istemiş ("dış çizgisi daha fazla ve
  gölgeli gözükmeli") — karar kullanıcının.
- Rakamlarımız kapsül boyuna göre referanstakinden küçük.
- Jetonun yıldızı referansta daha parlak ve konturu daha güçlü.

### Düğmelerin ışık profili — ölçüldü, DEĞİŞTİRİLMEDİ

Bizim kırmızı Quit ile referansın kırmızı duraklat düğmesi, ikisi de kendi
orta tonuna normalize edilip 21 noktada karşılaştırıldı. Bir önceki turda
bulunduğu sanılan "üst dudak farkı" YOK — o, iki tarafta kenarı farklı ele
almaktan doğan bir artefaktmış (aynı yöntemle: referans %0'da 1.12, bizim 1.17).

Kalan gerçek fark tek: referansın yüzü ortada koyulaşıp **dipte geri parlıyor**
(%75-90'da ~1.02), sonra keskin etek geliyor. Bizde o sekme ışığı yok, tek
yönlü koyulaşıyoruz. Fark 0.1 mertebesinde — yani düğmelerimiz referansa yakın.
`MenuPage`'in halka tonlarıyla ayarlanabilir; yapılmadı.

## SANAT DEĞİŞİMİ BAŞLADI (aynı gece)

Kullanıcı kararı: çıkarılan Grand Games varlıkları doğrudan kullanılacak;
karşılığı olmayan için aynı dilde yenisi üretilecek. Telif riski bir kez
söylendi, kullanıcı üç kez teyit etti.

### Değişen varlıklar

**Jeton paketleri — `coin_pile_1..6`.** Havuzda mağazanın KENDİ paket serisi
bulundu: `shop_coin_002..006` (180×144 → 301×237) artı `coin_pile` (162×89).
Altı kademe: yığın → büyük yığın → sandık → kasa → taşan kasa.
Bizim eskiler mat, düşük kontrastlı, ışıksızdı.

**`icon_noads`** ← `icon_noads` (129×122). Kalın "ADS" + kırmızı çizgi.
**`icon_chest`** ← `Chest` (453×358). Açık, mücevherli, jeton dökülen sandık.

### DEĞİŞTİRİLMEYENLER — ad eşleşmesi yanıltıcı

`Rocket`, `Ufo`, `Heart` Magic Sort'ta İLLÜSTRASYON DEĞİL, **flama rozeti**.
Bizim mevcut 3B roket/UFO/kalp görsellerimiz o iş için daha iyi. `Cup_Gold` da
bizimkinden zengin değil. Yani ikonlarımız topluca kötü değil — kötü olan
jeton paketleriydi.

**Ders:** havuzda ada göre arayıp körlemesine almak yanlış sonuç verir; her
adayı koyu zemine bindirip GÖRMEK gerekiyor. Altı adayın dördü elendi.

### Yolda çıkan hata ve düzeltmesi

Yeni `icon_noads`'ın İÇİNDE "ADS" yazısı var; `StoreScreen.RewardIcons` ise
ayrıca kod tarafından "ADS" etiketi çiziyordu → ekranda "AADSS".
Aynı dosyada bu ders zaten yazılıydı (sonsuz kalp için: "hazır görsel geldiğinde
parçaları ÜST ÜSTE KOYMA") ama NoAds'a uygulanmamıştı. Artık uygulandı: skin
görseli varsa yalnız görsel, yoksa görsel + kod yazısı.

### Sırada

Paneller ve kart zeminleri, sekme çubuğu, rozet/çerçeveler, yolculuk düğüm
görselleri, liderlik podyumu. Havuzda `HomeSpriteAtlas` ve `MetaSpriteAtlas`
tam bir arayüz kiti taşıyor.

## MAĞAZA DETAYLI ELDEN GEÇİRME (2026-08-22)

Yöntem her maddede aynı: değiştir → düzenleyici kipinde yakala → AYNI ölçüm
kodunu hem referansa hem bize uygula → sayıyla karşılaştır.

### Ölçülüp düzeltilenler

**1. Paket kartı fazla uzundu.** `PackCreamH` 330 → **290**.
Referans kart 885×268 → en/boy 3.30; bizimki 1002×346 → 2.90.
Yardımcı ikon şeridinin yüksekliği İKİSİNDE DE 0.448 çıkmıştı — yani ikonlar
karta göre doğru orandaydı, kartın kendisi fazla yüksekti ve fazladan dikey
boşluk her şeyi genişliğe kıyasla küçültüyordu.
**Sonuç: en/boy 2.90 → 3.27** (hedef 3.30).

**2. ADS rozeti fazla iriydi.** Yükseklik kart boyunun referansta 0.440'ı,
bizde 0.538'i; genişlik 0.141'e karşı 0.184.
Kutu `(0.556, 0.44, 0.740, 1.00)` → **`(0.578, 0.542, 0.719, 1.00)`**.
X ekseni referansta ölçülen 0.574-0.715 aralığına oturdu.

**3. "3s" yanlış yerdeydi.** Kalbin sağına kaçmış, UFO'nun yanında uçuşuyordu.
Referansta sonsuz kalbin TAM ALTINDA ve onunla aynı eksende.
`(0.845, 0.44, 0.955, 0.62)` → **`(0.762, 0.50, 0.873, 0.68)`**.

**4. Yardımcı ikonlar biraz küçüktü.** Kutu ±0.070 → **±0.080**,
dikey 0.14-0.48 → **0.09-0.48**. Dikey sıralama netleşti: kalp → "3s" → yardımcılar.

**5. Jeton sayacı** (bir önceki turdan): `PillX1` 0.352 → 0.296,
rakam rengi `#532A0D` (kahve) → `#1B255C` (lacivert).

### Sanat değişimi

**Jeton paketleri.** Havuzda mağazanın kendi serisi bulundu: `shop_coin_002..006`
artı `coin_pile`. Tırmanış: yığın → büyük yığın → sandık → kasa → taşan kasa.

**KRİTİK: yığınlar NORMALLEŞTİRİLDİ.** Kodda ölçülmüş bir kural yazılıydı —
"referansta altı yığının da genişliği 226-229 px, hepsi kutuyu aynı dolduruyor;
artan tek şey jeton yoğunluğu". İlk içe alma bunu bozdu: `coin_pile` 162×89
(en/boy 1.82) diğerlerinden yassı olduğu için kutusunda küçücük kalıyor ve
miktar yazısı boş kremin üstüne düşüyordu.
Çözüm: altısı da **420×323 ortak tuvale, aynı genişlikte, ALTA YASLI** konarak
yeniden üretildi. Artık kutuları eşit dolduruyorlar, yazı jetonların üstünde.

**`icon_chest`** ← `Chest` (açık, mücevherli sandık). **`icon_noads`** ← kalın "ADS".

**DEĞİŞTİRİLMEYENLER:** `Rocket`, `Ufo`, `Heart` Magic Sort'ta illüstrasyon
değil FLAMA ROZETİ; bizim 3B görsellerimiz daha iyi. `Cup_Gold` da değil.
Altı adayın dördü elendi. Ada göre körlemesine almak yanlış sonuç veriyor.

### Yolda çıkan hata

Yeni `icon_noads`'ın içinde "ADS" yazısı var; `RewardIcons` ayrıca kod tarafından
"ADS" çiziyordu → ekranda "AADSS". Aynı dosyada bu ders sonsuz kalp için zaten
yazılıydı ("hazır görsel geldiğinde parçaları ÜST ÜSTE KOYMA") ama NoAds'a
uygulanmamıştı. Artık uygulandı: skin görseli varsa yalnız görsel.

### Mağazada kalan, henüz dokunulmayanlar

- Kutucuklarda miktar yazısı kremin üstüne denk geldiği yerde hâlâ düşük kontrast
- "90% OFF" ve "Popular" flamaları düz renk; havuzda daha iyi flama sanatı olabilir
- Teklif kartının (Starter Pack) zemini düz turuncu gradyan
- Referansın teklif kartında avatar + kalp sayısı var, bizde yok

### Power-up'lar Block Out!'un KENDİSİNDEN alındı

Block Out! iOS'a özel ve paketi şifreli — dosyalarına erişemiyoruz. Ama elimizde
App Store'dan indirilen 1320×2868 KAYIPSIZ ekran görüntüsü var ve booster'lar
orada düz yeşil kapsülün üstünde duruyor. Üçü de oradan kesildi:

Yöntem (`_Reference/blockout_icons/`):
1. Booster şeridi bulundu (y %73.5-%83.5, üç kapsül x aralığı 266 px'de bir)
2. Zemin adayı = yeşilimsi VEYA çok koyu pikseller
3. KENARDAN taşma (flood fill) ile bağlı zemin silindi — eşikle silmek koyu
   yeşil konturu bırakıyordu
4. Kalan opak parçalardan yalnız MERKEZE BAĞLI olanı tutuldu — bu, alttaki
   fiyat hapını ("300", "1200") ve üstteki kapsül yayını temizledi
5. Doğrulama KOYU DÜZ ZEMİNDE yapıldı (satranç deseninde değil — bkz. bellek
   notu "transparency verification trap")

`icon_rocket` 117×116, `icon_ufo` 142×113, `icon_clock` 114×121.

**BEDEL:** eski ikonlarımız 512 px'ti, bunlar ~117 px. Kaynak ekran görüntüsü
olduğu için daha fazlası yok. Mağazada ~160 birimde çiziliyorlar, yani hafif
yumuşaklar. Tasarım kazancı (gövde detayı, altın halka, hacim) bunu karşılıyor
ama yüksek DPI cihazda fark edilebilir.

iPad görselinde kapsüller DAHA KÜÇÜK (190 px vs 201 px) — iPhone görseli daha
yüksek çözünürlük veriyor, o kullanıldı.

### pack_1..5 DEĞİŞTİRİLMEDİ

Havuzdaki paket adaylarının çoğunda Magic Sort'a özel mor booster ikonları
gömülü. Bizim kırmızı çanta / kavanoz / mor sandık / kırmızı sandık / büyük
hazine serimiz daha temiz ve zaten Block Out! diline uygun.

### KAPLAMA için ÖNEMLİ AYRIM

Magic Sort'un düğme ve panel çerçeveleri **ALTIN KONTURLU** (`bg_bttn_green_frame`,
`bg_popup_offer_frame`, `bg_popup_frame` — hepsinde kalın altın halka).
Block Out!'un dili ise **KOYU İNDİGO KONTURLU** (#121133, ölçüldü).
Magic Sort'un kaplamasını olduğu gibi almak oyunu Block Out!'a değil Magic
Sort'a benzetir. Bu yüzden kaplama havuzdan ALINMAYACAK, Block Out!'un ölçülmüş
diliyle üretilecek. İllüstrasyonda durum tersi: orada havuz zengin ve dil uyuyor.

### Tente ve başlık — ölçülüp düzeltildi

Üçü de "üstünde başlık/kapsül OLMAYAN sütunlardan" (ekranın sağ %74-%99'u)
ölçüldü; ortadan ölçmek beyaz başlığı ve krem kapsülü örneğe katıyordu.

**1. Tente yüksekliği 268 → 360.** Eski değer "ekran YÜKSEKLİĞİNİN %13.96'sı"
diye hesaplanmıştı. Ama kanvas GENİŞLİKLE ölçekleniyor (`matchWidthOrHeight = 0`),
yani oran genişliğe göre alınmalı: referans 315/946 = **%33.3**, bizimki
268/1080 = %24.8. **Sonuç: %33.1.**

**2. Feston derinliği %14 → %21.6.** Referans 68/315 = %21.6, bizim 37/266 =
%13.9 — kumaş "asılı" değil "kesilmiş şerit" gibi duruyordu. **Sonuç: %21.6.**
İkisi kendi içinde tutarlı: 360 × (1−0.216) = 282, referansın sığ noktasının
birim karşılığı da 282.

**3. Başlık puntosu 90 → 128.** Referans başlık 90/315 = **%28.6**, bizim
72/357 = %20.2. Kutu da 0.32 → 0.40'a açıldı yoksa `UiTextFit` puntoyu geri
kısıyordu. **Sonuç: %28.9.**

**4. Koyu şerit açıldı.** #0068E1 → **#0374F6** (referans #0375F8).
Not: gözle "kontrastımız düşük" sanmıştım; ölçünce TERSİ çıktı — bizim oran
1.25, referans 1.11. Sorun kontrast değil, koyu şeridin fazla koyu olmasıydı.
Açık şerit zaten tutuyordu. **Sonuç: oran 1.13.**

### Bu turun dersi

Bu turda dört kez gözle yargılayıp ölçünce yanıldım (zemin rengi, kapsül
genişliği, üst dudak, şerit kontrastı). Hepsinde ortak hata: **elle kutu seçmek
ya da örneği kontrol etmeden bakmak.** İşe yarayan tek yöntem: aynı ölçüm kodunu
hem referansa hem bize uygulamak, ve örnek alınan bölgede üst üste binen öge
olmadığından emin olmak.

### Bölüm başlığı şeridi (gece turu, 1. iterasyon)

`PillH` 104 → **122**.

Ölçüm zor çıktı, üç deneme gerekti — kaydı önemli çünkü aynı tuzak başka
ekranlarda da çıkacak:
1. Kırmızı maske denendi → tuğla duvarı ve turuncu teklif kartını da yakaladı.
2. "Sıcak renk" bitişik koşusu denendi → başlığın YÜZÜNÜN İÇİNDEKİ koyu bant
   (#5F0000) koşuyu kırdı, 10 px'lik saçma sonuç verdi.
3. Çalışan yöntem: **altın kenar satırlarını kümelemek.** Başlığın üst ve alt
   altın kenarı iki ayrı satır kümesi; aradaki mesafe gerçek yükseklik.
       referans: kümeler 318-323 ve 418-424 -> 107 px = genişliğin %11.3'ü
       bizim   : kümeler 364-373 ve 458-467 -> 104 px = %9.63
   0.113 × 1080 = 122.

**Doğrulama:** yeniden yakalandı, yeni değer **%11.30** (hedef %11.3).

**MarginX DEĞİŞTİRİLMEDİ.** Başlık genişliği referansta %88.7, bizde %90.4
ölçüldü — ama ölçüm köşe yarıçapının daralttığı satırdan alındığı için bu fark
güvenilir değil; üstelik `MarginX` kartlarla paylaşılıyor ve kartlarda ölçüm
zaten tutuyor (referans %93.6, bizim %92.8). Paylaşılan bir sabiti güvenilmez
bir ölçüm için değiştirmek, bu turda bir kez yapılan kapsam hatasının aynısı
olurdu.

### Gece turu, 2. iterasyon — mağaza fiyat düğmesi ve başlık yazısı

**Fiyat düğmesi oranı.** Yeşil dolgusu ölçüldü:
    referans 263×86 -> ekranın %27.8'i geniş, %9.09'u yüksek, en/boy 3.06
    bizim    319×83 -> %29.5 geniş, %7.69 yüksek, en/boy 3.84
Hem fazla geniş hem fazla alçaktı. Kutu: X 0.63-0.96 → 0.649-0.96 (×0.94),
Y bandın %18-80'i → %12.4-85.8 (yükseklik ×1.18, dikey MERKEZ korunarak).
**Doğrulama: %27.6 / %9.07 / en-boy 3.04.**

**Bölüm başlığı yazısı 56 → 70 punto.** Yazı ŞERİDE oranlandı:
    referans 52 px / 107 px şerit = %48.6
    bizim    48 px / 122 px şerit = %39.3
**Doğrulama: %49.2.**

DERS: şerit bu turda 104'ten 122'ye çıkarılmıştı ama yazı sabit puntoda
kaldığı için oransal olarak KÜÇÜLDÜ. Bir kutuyu büyütürken içindeki yazının
puntosu da birlikte düşünülmeli; yoksa bir düzeltme başka bir bozulma üretiyor.

### Mağazada kalan (ölçüldü, henüz yapılmadı)

- **Teklif kartı — en büyük görsel fark.** Referansta altın çerçeveli zengin bir
  kart: hazine sandığı, "Blok Bileti" altın plakası, avatar, kalp sayısı, kordonla
  bağlı altın fiyat plakası, yeşil "Aktifleştir" düğmesi. Bizde düz turuncu
  gradyan + jeton yığını. Bu bir İÇERİK farkı da (avatar/kalp bizde yok).
- Paket adı yazısı: referans bandın %50.9'u; bizimki ölçülemedi (yerleşim
  kaydığı için pencere ıskaladı), sonraki turda alınacak.

### Gece turu, 3. iterasyon — ana ekran ilk kez ölçülebilir hale geldi

**`HomeScreen.CreatePreview` + `BuildInto` eklendi.** Ana ekran kendini
`Start()` içinde kuruyordu ve `UiKit.CreateCanvas` bir EventSystem kurup
`DontDestroyOnLoad` çağırdığı için düzenleyici kipinde PATLIYORDU. `BuildUi`
ikiye ayrıldı: kanvas kurma ayrı, içeriği kurma (`BuildInto(Canvas)`) ayrı.
Artık diğer menü ekranlarıyla aynı sözleşmede ve yakalanabiliyor.
`Refresh()` çağrılmıyor (MetaServices düzenleyici kipinde ayakta değil), bu
yüzden önizlemede jeton/can SAYILARI boş — yerleşim ve görseller doğru.

**Üst bar DOĞRU ÇIKTI.** Gözle "kapsüller aşırı geniş" sanmıştım; yeşil "+"
düğmelerinin konumu ölçülünce:
    referans 0.456-0.512 ve 0.772-0.827
    bizim    0.459-0.509 ve 0.774-0.825
Kapsüller boş göründüğü için geniş sanmışım — sayılar önizlemede yok.
(Bu turda beşinci kez gözle yanılıp ölçümle düzeldim.)

**OYNA düğmesi rengi — DEĞİŞTİRİLMEDİ, karar gerekçesi:**
Referansta düğme MOR (#361473), bizde YEŞİL (#34C414); kod koşulsuz
`MenuPage.Green` kullanıyor. Ama referanstaki bölüm "Zor Seviye" ve üstünde
"Ödüller ×3" flaması var — ikisi de zor-bölüm göstergesi. Ayrıca oyun HUD
referansımızda (`iphone_2_challengeyourself.png`, "Super Hard") geri-al ve
duraklat düğmeleri mordan KIRMIZIYA dönüyor: Block Out! kaplama rengini
zorluğa göre değiştiriyor.

Elimizdeki TEK ana ekran referansı zor bölümü gösteriyor; normal bölümde
düğmenin ne renk olduğunu bilmiyoruz. Koşulsuz mora çevirmek, normal durumu
bozma riski taşıyor. **Kullanıcıya sorulacak** (uyandığında): oynat düğmesi
normal bölümde yeşil mi kalmalı, zor bölümde mora mı dönmeli?

**Ana ekranda eksik olanlar (referansta var, bizde YOK):**
- Sol kenarda iki arkadaş avatarı ("Bitti" / "Katıl" etiketli)
- Sağ üstte yıldız rozeti + "15g 0s" etkinlik geri sayımı
- Sol ortada can/kalp geri sayım widget'ı ("15:25")
Bunlar İÇERİK/ÖZELLİK eksiği, kaplama ayarı değil — eklenmesi ayrı bir iş.

### Gece turu, 4. iterasyon — yolculuk seviye kapsülleri

Mor bantlar TAM KOLON taramasıyla bulundu (sabit pencere ıskalıyordu):

| | referans | önce | sonra |
|---|---|---|---|
| kapsül genişliği | %81.0 | %87.4 | **%80.9** |
| yüksekliği (ekran genişliğine oran) | %18.5 | %14.1 | **%18.4** |
| en/boy | 4.38 | 6.21 | **4.39** |
| satır aralığı | %29.9 | %23.1 | **%29.9** |

`PillH` 175 → **230**, `RowHeight` 250 → **323**, `PillX0/X1` 0.05-0.95 → **0.0835-0.9165**.

İki not:
- `PillH` 175 birimken ekranda 152 px çiziliyordu (kapsülün iç payı, oran 0.869);
  hedef sabit o payı telafi ederek hesaplandı.
- Genişlikte önce doğrudan %81 verildi, ekranda %78.7 çıktı — aynı iç pay.
  2.3 puan telafi edilip %83.3 sabiti kondu, ekranda %80.9 oldu.
- Kapsül büyürken satır aralığı da büyütüldü; yoksa kapsüller birbirine
  yapışırdı (mağazada aynı tuzağa bir kez düşülmüştü).

**Görsel doğrulama yapıldı**, taşma yok.

### Yolculukta kalan — KÖŞE YARIÇAPI (sonraki iterasyon)

Ölçüm (X aralığı kapsülle sınırlandırılarak; ilk deneme %81 gibi imkânsız bir
değer verdi çünkü maske kapsül dışını da kapmıştı):
    referans yarıçap = kısa kenarın **%39**'u
    bizim            = **%13**

Sebep: kapsüller `MenuCapsule` -> `UiCornerFit.HouseShare` (%22) kullanıyor ama
`MaxRadius = 34` birim TAVANI devreye giriyor: 193 px'lik kapsülde %22 = 42
birim isteniyor, tavan 34'e kırpıyor, iç paylarla birlikte ekranda %13 kalıyor.
%39 için tavanın bu çağrı yerinde kaldırılması gerek (`MenuPage.Capsule`'ün
imzasına bakılacak). Tavanı GENEL olarak yükseltmek yanlış olur — o değer
panel ve kartlar için ölçülmüştü.

### Gece turu, 5. iterasyon — yolculuk BİTTİ

**Köşe yarıçapı %13 → %38** (referans %39).
`MenuPage.Capsule` ve `JourneyScreen.MenuCapsule` isteğe bağlı bir `maxRadius`
parametresi aldı (varsayılan eskisi, yani başka hiçbir çağrı yeri etkilenmedi).
Kilometre taşı kapsülleri `MilestoneCornerShare = 0.42` + `NoRadiusCap` ile
kuruluyor. **Genel `MaxRadius` sabitine DOKUNULMADI** — o değer panel ve kartlar
için ölçülmüştü; bu turda bir kez yapılan kapsam hatasının tekrarı olurdu.

**Yazı puntoları 66 → 75 ve 38 → 43.** Sayı bloğu kapsüle oranlandı:
referans %22.5, bizim %19.7 → ×1.14. **Doğrulama: %22.3.**
(Kapsül 175'ten 230 birime çıkınca sabit punto oransal olarak küçülmüştü —
mağazadaki "şerit büyüdü, yazı sabit kaldı" tuzağının aynısı. Bu sefer aynı
turda yakalandı.)

**Yolculuk ekranının ölçülen bütün boyutları referansla eşleşti:**
genişlik %80.9/%81.0, yükseklik %18.4/%18.5, en-boy 4.39/4.38,
satır aralığı %29.9/%29.9, köşe %38/%39, yazı %22.3/%22.5.
Görsel doğrulama da yapıldı.

**Yolculukta kalan (ölçülmedi, görsel izlenim — sonraki taramada bakılacak):**
referansın kapsülünde daha belirgin bir açık kenar ışığı ve dipte 3B dudak var;
bizimki daha düz. Bu MenuPage'in halka tonlarına bağlı, ölçüm gerektirir.

### Gece turu, 6. iterasyon — sıralama satırları

`rowH` 158 → **179**, `gap` 24 → **20**.

Ölçüm: satır zeminleri, avatar/rozet/jeton hapının kesmediği temiz bir sütundan
(x = genişliğin %32'si) dikey taramayla bulundu. İlk denemede satır bandı
tespiti parçalanmıştı — içerik bandı kesiyordu.

    referans: satır genişliğin %17.0'i, pitch %18.4
    bizim   : %14.6 / %16.9
    sonra   : %16.6 / %18.4   (pitch BİREBİR)

**ÖNEMLİ METODOLOJİ NOTU:** Dikey KONUM karşılaştırılmadı. Referans ekranı
946×2048 (en/boy 2.165), bizimki 1080×1920 (1.778). Farklı en/boy oranında
"ekranın %X'i aşağıda" aynı şeyi anlatmıyor; kanvas GENİŞLİKLE ölçeklendiği
için yalnız genişliğe oranlanan ölçüler karşılaştırılabilir. Bu yüzden
"referansta 5 satır sığıyor, bizde 3" gözlemi bir KUSUR DEĞİL — referans
ekranı fiziksel olarak daha uzun.

### Sıralamada kalan (sonraki iterasyon)

**Sekme seçici ters.** Referansta seçili sekme MAVİ DOLGULU, diğer ikisi
dolgusuz (yalnız yazı). Bizde ÜÇÜ DE beyaz dolgulu. Ölçülen renkler:
    referans seçili #0F6DC4, seçilmemiş #604FC2 / #644DC2 (kapsayıcıyla aynı)
    bizim    üçü de #3CB8F8 / #3CB8F7 / #39B5F7

### Gece turu, 7. iterasyon — sıralama sekme seçici: KUSUR DEĞİLMİŞ

Önceki turda "sekme seçici ters, bizde üçü de beyaz" diye not almıştım.
Kodu okuyunca çıktı: sekmeler `Color.white` ile KURULUYOR ama seçili/seçilmemiş
rengini `Refresh()` uyguluyor (`TabActive` / `TabIdle`). Önizleme `Refresh()`
çağırmadığı için hepsi beyaz kalıyor — ana ekrandaki boş sayılarla aynı tür
önizleme yan etkisi.

**Yapı DOĞRU.** Referansta da seçili sekme mavi, diğerleri kapsayıcıyla aynı
renkte (y=300 yatay taraması: mavi 0.181-0.384, mor 0.392-0.819) — bizim
`Refresh()`'imiz tam bunu yapıyor.

**AÇIK KALAN (ölçülemedi):** sekme renklerinin TON'u. Sabitlerimiz
`TabActive #0085FE`, `TabIdle #6553FD`; referanstan tek satır örneği
`#0E5BB3` / `#624DC2` verdi, yani bizimkiler daha parlak olabilir. Ama
ortalama almaya çalışınca pencere sekmelerin arkasındaki koyu kuyuya denk
geldi ve üç sekme de aynı çıktı — ölçüm güvenilir değil. Sekme bandının y
aralığı önce kesin tespit edilmeli. Tek satır örneğine dayanıp renk
değiştirmedim.

**ÖNİZLEME UYARISI (tekrar tekrar tuzağa düşmemek için):**
`Refresh()` çağrılmayan önizlemelerde şunlar YANLIŞ görünür ve KUSUR SANILMAMALI:
- HomeScreen: jeton/can sayıları boş, oynat düğmesi yazısı yok
- LeaderboardScreen: üç sekme de beyaz
- Genel kural: renk/metin durum'a bağlıysa önizlemede varsayılan kalır.
  Ölçmeden önce "bunu Refresh mi ayarlıyor?" diye koda bakılmalı.

### Gece turu, 8. iterasyon — koleksiyon illüstrasyonu

Zemin (koyu mor) maskelenip illüstrasyonun sınır kutusu bulundu:
    referans 799×686 px / 946 -> ekran genişliğinin **%84.5**'i
    bizim    648×574 px / 1080 -> **%60.0**

Kutu X 0.20-0.80 idi ve `preserveAspect` GENİŞLİKLE sınırlıyordu, yani çizilen
boy doğrudan kutunun genişliği. Kutu 0.0775-0.9225'e açıldı ve DİKEY de açıldı
(0.32 -> 0.42) — yoksa bu sefer yükseklik kırpar, görsel yine küçük kalırdı.
Merkez korundu (0.56).

**Doğrulama: %84.4** (aynı pencereyle ölçüldü — ilk denemede pencereyi
değiştirip referansta alt bandı da kapmış, %97.5 gibi yanlış bir değer almıştım;
iki tarafı AYNI pencereyle ölçmek şart).
**Görsel doğrulama yapıldı**, banner kutusuyla çakışma yok.

### Koleksiyonda kalan

- Başlık yazısı ("Collection") referanstakinden ("Koleksiyon") küçük ve konturu
  daha ince. Ölçülmedi.
- Alttaki banner önizlemede BOŞ — metin duruma bağlı, `Refresh()` çağrılmıyor.
  Kusur değil (bkz. 7. iterasyondaki önizleme uyarısı).

## MAĞAZA — sanat değişimi ve tente REGRESYON DÜZELTMESİ (2026-08-22, sabah)

### Tente regresyonu — kullanıcı haklıydı

Kullanıcı "tente daha kötü oldu" dedi. Doğruydu: feston derinliğini %14'ten
%21.6'ya çıkarmıştım ve bu bir REGRESYONDU.

Aynı büyüklüğü ÜÇ farklı pencereyle ölçtüm, üç farklı sonuç aldım:
    1) x 0.70-0.98, y sınırsız  -> %21.6  (YANLIŞ, maske tenteden aşağı taştı)
    2) x 0.55-0.99, y sınırsız  -> saçma (derinlik 221 px)
    3) x 0.55-0.99, y<400 SINIRLI -> **%10.3** (DOĞRU, görselle uyuşuyor)

Kenarı büyütüp GÖZLE bakınca fark hemen görüldü: referansın festonları geniş
ve SIĞ; bizimkiler parmak gibi sarkıyordu.
`depth` %21.6 -> **%10.3**, `AwningH` 360 -> **400** (referansın en derin
noktası 351 px / 946 = genişliğin %37.1'i; önceki 315 px ölçümü festonun
dibini kesiyordu). Görsel doğrulama yapıldı, festonlar artık referanstaki gibi.

**DERS:** ölçüm sonucu görselle çelişiyorsa ölçüme değil GÖZE güven, sonra
ölçümü düzelt. Bir sabiti değiştirmeden önce ölçümün kendisi doğrulanmalı.

### Sanat değişimi

**`pack_1..5` YENİLENDİ** — kullanıcı haklı olarak "jeton yığınlarını
yenileyip paketleri eski bıraktın" dedi. Yeni set, tırmanan ve tutarlı:
    pack_1 <- booster_rewards_coin  (yıldızlı jeton yığını)
    pack_2 <- CoinBag               (kırmızı çanta)
    pack_3 <- reward_coin_red       (taşan kâse)
    pack_4 <- Chest                 (mücevherli açık sandık)
    pack_5 <- cl_ays_coinpile       (jeton dağı)
Hepsi 460×377 ortak tuvale, aynı genişlikte, alta yaslı normalleştirildi.
ELENENLER: `Coins` ve `chest_offer_mp_shop` (Magic Sort'un mor booster'ları
gömülü), `Chest_1/2` ve `Safe_Front` (parça), `chest_search`/`sj_gold_chest`
(kaideli). Hepsi krem zeminde GÖRÜLEREK elendi.

**Kalp ikonları yenilendi** — havuzdakilerin KOYU KONTURU var, Block Out!'un
dili tam bu; bizimkiler konturuz ve düz parlaktı:
    icon_infinite    <- booster_rewards_limitless_life (248×224)
    icon_heart       <- heart-Multiple (224×212)
    icon_heart_broken<- broken_heart (332×260)

### Teklif kartına ALTIN ÇERÇEVE

Referansta özel teklif kartını paket kartlarından ayıran şey kalın altın
çerçeve; bizde hiç yoktu. ÖLÇÜM: sol kenardaki altın koşu 10 px / 946 =
genişliğin %1.06'sı -> 1080'de **11 birim**.
Kartın üç katmanı (Body/Band/Art) çerçeve kadar içeri alınmış bir `Inner`
kabına bağlandı — `Place`'in padding'i her yönden eşit içeri aldığı için
doğrudan uygulamak bandın DİKEYİNİ de kısaltırdı.
Görsel doğrulama yapıldı.

### Mağazada kalan

- Teklif kartı içeriği: referansta hazine sandığı + "Blok Bileti" plakası +
  avatar + kalp sayısı + kordonlu fiyat plakası. Bizde jeton yığını + booster.
  İÇERİK farkı (avatar/kalp bizde hiç yok).
- "90% OFF" ve "Popular" flamaları düz renk, konturu yok.
- Paket kartında yardımcı ikonların "x1" etiketleri ikonlara biniyor.

## OYUN HUD'U — kayıpsız referansla ilk ölçüm (2026-08-22)

Kaynak: `_Reference/store/iphone_1_cleartheboard.png` (1320×2868, App Store,
SIKIŞTIRMASIZ). Önceki HUD ölçümleri 592×1280 video karesinden yapılmıştı.

### Jeton sayacı

    referans jeton X 0.092-0.146 (genişliğin %5.5'i), yükseklik %4.5
    bizim    jeton X 0.039-0.121 (%8.2), yükseklik %7.3
    referans sayı yüksekliği %2.8  /  bizim %2.3

Jetonumuz ~1.5 kat büyüktü ve fazla soldaydı; referansta jeton plakanın SOL
UCUNA BİNİYOR, dışında durmuyor.
`coinPlate` 0.075-0.365 -> **0.105-0.300**, `coin` 0.035-0.125 ->
**0.089-0.149** (Y de daraltıldı), sayı puntosu 42 -> **51**.

**Doğrulama:** jeton X **0.096-0.143** (hedef 0.092-0.146), sayı yüksekliği
**%2.8** (hedef %2.8).
Genişlikte %0.9 fark kaldı: bizim jeton sprite'ı referanstakinden daha yuvarlak,
`preserveAspect` genişlik ve yüksekliği birden tutturamıyor. Yükseklik
önceliklendirildi.

### Süre sayacı

    referans yazı yüksekliği %3.8, çekirdek renk #FFFAEB (AA'sız medyan)
    bizim    %3.1, #FFFDF6 (daha SOĞUK)

Punto 58 -> **73**, `HudClockInk` -> **#FFFAEB**.
Ayrıca `Refresh()` sayacı `Ink` ile EZİYORDU; artık `HudClockInk` kullanıyor,
yoksa ölçülen renk oyunda geri kaybolurdu.

**Doğrulama:** yükseklik **%3.9** (hedef %3.8), renk **#FFFAEB** BİREBİR.

**AÇIK KALAN — font kimliği:** aynı "02:30" metninde referans %14.5 geniş,
bizimki %18.2. Yükseklik eşit olduğuna göre bizim rakamlar daha GENİŞ; bu
karakter aralığıyla kapatılamaz, glif genişliği farkı. Block Out!'un fontu
teşhis edilemediği için (bkz. UI-DILI.md) bu fark kalıcı.

**Not:** süre biçimi zaten iki hanelidir (`02:30`); önizlemeye elle "2:30"
verdiğim için tek haneli görünmüştü — kusur değil.

### HUD'da kalan

- Geri-al ikonu: referansta uçlu kavisli ok, bizde tam daire ok. Görsel fark.
- Geri-al/duraklat düğmelerinin pah ve dış konturu referansta daha belirgin.
- "Lv 72" kapsülü ölçülmedi.

## KULLANICI BULGULARI — 2026-08-22 sabah (ekran görüntüleriyle)

- [x] **A. Booster ikonları ALTTAN KESİK — DÜZELTİLDİ.** Paket kartında roket/çalar saat/UFO
      alt kısımları yok. Maskeden (RectMask2D) mi, yerleşimden mi belirlenecek.
      REGRESYON İHTİMALİ: bu turda ikon kutusu 0.14-0.48'den 0.09-0.48'e
      indirilmişti.
- [x] **B. Jeton dağı KALDIRILDI, en üst kademe SANDIK oldu.** Aynı tarzda bir SANDIK
      konacak; alakasız durmayacak. (`pack_5` = `cl_ays_coinpile` idi.)
- [x] **C. Jeton ikonu DEĞİŞTİRİLDİ.** yeni varlıkla güncellenecek (mağaza üst barındaki).
- [ ] **D. "Best Value" bayrağı/flaması** güncellenecek.

### A/B/C ne yapıldı (2026-08-22)

**A — kesilme.** Doğrulandı: roketin alevi, saatin kaidesi ve UFO'nun altı
kartın alt maskesine (RectMask2D) taşıp kesiliyordu. İKİ sebep vardı:
1. Kutuyu bu turda 0.14 -> 0.09'a indirmiştim (REGRESYON, benim hatam).
2. `UiKit.CreateIcon` `preserveAspect` kurmuyor — ikon kutuyu doldurmak için
   GERİLİYORDU, yani dikeyde kutudan taşabiliyordu.
Kutu 0.14-0.50'ye alındı, `preserveAspect` açıldı, etiket 0.03-0.20'ye indi.
Yakalamayla doğrulandı: üç ikon da tam görünüyor.

**B — jeton dağı.** Havuzdaki DAHA BÜYÜK sandıkların hepsinde Magic Sort'un mor
booster karoları gömülü (`chest_offer_mp_shop`, `chest_offer_magicpass`,
`chest_info`) — alınırsa "alakasız" durur. Çözüm: set yeniden sıralandı,
dağ tamamen çıkarıldı, EN ÜST KADEME temiz `Chest` oldu:
    pack_1 booster_rewards_coin -> pack_2 Coin10000 -> pack_3 CoinBag
    -> pack_4 reward_coin_red -> pack_5 **Chest**

**C — jeton ikonu.** Bizimki AÇILI/3B bir jetondu (479×512, solda kalınlığı
görünüyor); Block Out!'unki DÜZ KARŞIDAN, koyu rimli, ortada yıldız.
`Coin_brownoutline` (87×88) tam o duruşta. Değiştirildi ve HUD + mağaza
yakalamasıyla doğrulandı — referansla neredeyse birebir.
Çözünürlük 479 -> 87 düştü; kaynak havuzda daha büyüğü yok.

## ⚠ ORTAM TIKANMASI — Unity odak almadan C# DERLEMİYOR (2026-08-22 11:50)

**Belirti:** `ribbon_reward.png` yeni görselle değişti, `UiSkin.Get(Art.Ribbon)`
sprite'ı DÖNDÜRÜYOR (186×204 diye loglandı), `StoreScreen.Ribbon` içindeki yeni
dal kodda DURUYOR — ama ekranda hâlâ eski prosedürel flama çiziliyordu.

**Kök sebep:**
    kaynak   StoreScreen.cs        22.08.2026 11:50:05
    assembly BlockOut.Runtime.dll  22.08.2026 11:30:28   -> BAYAT

**Denenen ve İŞE YARAMAYAN yollar (hepsi MCP `Unity_RunCommand` içinden):**
- `AssetDatabase.Refresh()`
- `AssetDatabase.Refresh(ForceSynchronousImport | ForceUpdate)`
- `AssetDatabase.ImportAsset(<dosya>.cs, ForceUpdate)`
- `CompilationPipeline.RequestScriptCompilation()`
- `EditorUtility.RequestScriptReload()`
Hepsi başarıyla döndü, `isCompiling` hep False kaldı, dll tarihi değişmedi.

**ÇÖZÜM: Unity penceresine bir kez tıklamak (odak vermek) ya da Ctrl+R.**
Odak alınca Unity beklemedeki derlemeyi yapıyor.

**Bu turda etkilenen TEK dosya `StoreScreen.cs`'ti** (flama değişikliği).
Diğer bütün dosyalar 11:30'dan önce derlenmişti, yani önceki doğrulamalar
GEÇERLİ. Kontrol edildi:
    GameplayScreen 11:19, MenuSprites 10:51, CollectionScreen 05:11,
    LeaderboardScreen 04:58, JourneyScreen 04:50, MenuPage 04:47, HomeScreen 04:28

**BUNDAN SONRA HER C# DEĞİŞİKLİĞİNDEN SONRA ŞU KONTROL YAPILACAK:**
    File.GetLastWriteTime("Library/ScriptAssemblies/BlockOut.Runtime.dll")
    < File.GetLastWriteTime(<değiştirilen .cs>)
Bayatsa ölçüm YAPILMAYACAK — eski kod ölçülür ve yanlış sonuç çıkar.
Bu turda tam olarak o oldu: flamayı üç kez "hâlâ eski" diye ölçtüm.

## 82 VARLIK TARAMASI — 1. parti: 22 ikon (2026-08-22)

Hepsi koyu zeminde tek tabloda görüldü (`_Reference/notes/all_icons.png`).

**Zaten Block Out! dilinde (bu turlarda yenilenmişti):**
chest, clock, coin, heart, heart_broken, infinite, noads, rocket, ufo

**HAVUZ ADAYLARI DENENDİ VE ELENDİ — bizimkiler daha iyi:**
| bizim | havuz adayı | neden elendi |
|---|---|---|
| star | `3x3_Star` | pembe DENİZYILDIZI, yıldız değil |
| star | `pto_star_04` | parıltı efekti, ikon değil |
| trophy | `cl_lb_goldcup` | **KEDİ KULAKLARI** var (Magic Sort'un kedi teması) |
| lock | `icon_lock_journey` | kilit değil, "kilitli profil" rozeti |
| globe | `planet03` | küre değil, halkalı gezegen |
| gear | `icon_tab_settings` | altın çiçek-dişli; bizim mor dişli Block Out!'un moruna daha yakın |

**DERS: havuz her zaman daha iyi değil.** Altı adayın altısı da elendi. Ada
göre alıp koysaydım altısı da bozulma olurdu. Her aday zeminde GÖRÜLMELİ.

**`icon_restart` DEĞİŞTİ — Block Out!'un KENDİ glifi.**
Havuzda geri-al ikonu yok (`back` eşleşmeleri arka plan katmanları). Kullanıcının
"olmayanı aynı dilde üret" talimatı geçerliydi ama daha iyisi bulundu: glif
kayıpsız referansta düz mor düğmenin üstünde duruyor, oradan kesildi.
Yöntem: düğmenin İÇ yüzü kırpıldı, yüz parlaklığından ±eşik ile glif gövdesi
(açık) VE konturu (koyu) seçildi, merkeze bağlı bileşen tutuldu.
`icon_restart` 512×512 (düz lavanta daire ok) -> **79×86** (Block Out!'un
uçlu kavisli oku, koyu konturuyla).
Yakalamayla doğrulandı: HUD referansa belirgin yaklaştı.

**Hâlâ zayıf, havuzda karşılığı yok (Block Out! dilinde üretilecek):**
album, badge_reward, hand, plus

## 82 VARLIK TARAMASI — 2. parti: ikon dışı 22 varlık (2026-08-22)

Hepsi koyu zeminde görüldü (`_Reference/notes/other_assets.png`).

**Dile uygun, dokunulmadı:** banner_region, bar_tabs, bg_menu, board_scene,
card_tab, collection_book, confetti_sheet, frame_avatar, frame_board,
home_characters, node_level, panel_card, panel_dark, podium_bronze/silver/gold,
ribbon_reward (yeni), splash_art.

**SORUNLU: `btn_green` / `btn_purple` / `btn_red` / `btn_square`**
Parlak "şekerleme" düğmeleri — üst yarılarında büyük beyaz spekülar süpürme var,
Block Out!'un MAT dilinin tam tersi. AMA bunlar **ÖLÜ VARLIK**:
`UiSkin.Art.Button*` sabitleri hiçbir yerden çağrılmıyor, arayüz düğmeleri
`MenuPage.PillButton` ile prosedürel kuruluyor. Silmedim (sahne/prefab
referansı olabilir), not edildi.

### YENİ KAYNAK BULUNDU: `iphone_6_splashstore03png.png`

1320×2868 KAYIPSIZ — Block Out!'un kendi açılış görseli: "BLOCK OUT!" logosu
(renkli LEGO harfler + mor konturlu turuncu "OUT!") ve roketteki üç karakter.

**`splash_art` karşılaştırıldı:** Block Out!'unki belirgin daha zengin —
tulum/ayakkabı detayı, perçinli ve lombozlu roket, dinamik pozlar, daha iyi
ışık. Bizimki daha basit ama kabul edilebilir.

**HENÜZ ALINMADI, sebebi:** zemin düz renk DEĞİL — bulutlu gradyan gökyüzü ve
karakterler bulutlarla örtüşüyor. Booster/geri-al glifinde işe yarayan
"düz zeminden flood fill" yöntemi burada çalışmaz; kötü bir kesim mevcut
halden daha kötü olurdu. Yapılacaksa ayrı ve dikkatli bir iş.

**LOGO DOSYALARINA DOKUNULMADI.** `logo_b/c/k/l/o/out(.png)` depoda YENİ ve
izlenmiyor — başkası logo üzerinde çalışıyor olabilir (bkz. bellek notu
"paralel çalışma ve sahiplik"). Referanstaki yüksek çözünürlüklü logo
kullanılabilir ama sahiplik netleşmeden dokunulmayacak.

## VİDEO REFERANSLARI ÇIKARILDI + ALTIN ÇERÇEVE GERİ ALINDI (2026-08-22)

### ffmpeg kuruldu, video karesi çıkarılabiliyor

Sistemde ffmpeg yoktu; `imageio-ffmpeg` paketi kendi ffmpeg'ini getiriyor:
    _Reference/.venv/Scripts/python.exe -c "import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())"

`Block Out! menus,powerups,vs.mp4` 3 saniyede bir taranarak 52 kare çıkarıldı
(`_Reference/frames/`, kontak sayfası `_Reference/notes/frames_sheet.png`).
İçinde REFERANSI OLMAYAN ekranlar bulundu: Ayarlar (019), Profil (020/051),
"MÜKEMMEL" sonuç paneli (041), Yıldız Patlaması etkinliği (042/043),
Gökyüzü Atlayışı (045), mağazanın Jetonlar bölümü (005).

**AMA: video yerel çözünürlüğü 384×832.** Elimizdeki EN KÖTÜ kaynak
(WhatsApp JPEG 946, App Store PNG 1320). Ölçüm için değil, YERLEŞİM ve RENK
için kullanılmalı. Başka referansı olmayan ekranlarda tek seçenek.

### ALTIN ÇERÇEVE GERİ ALINDI — fazla genelleme

`market.jpeg`'te teklif kartının çevresi kalın altın çerçeveydi ve bunu BÜTÜN
teklif kartlarına uygulamıştım. **YANLIŞTI.** Videodan çıkan karede
"Başlangıç Paketi" (bizim Starter Pack'in ta kendisi) düz turuncu, çerçevesiz.

`market.jpeg`'teki çerçeveli kart **"Blok Bileti" adlı PREMIUM paketti**.
Yani altın çerçeve teklif kartının değil, o özel paketin muamelesi.

`GoldRim` katmanı ve `GoldFrame`/`OfferGold` sabitleri silindi; `Inner` kabı
KORUNDU (padding 0 ile), çünkü katman yapısını sadeleştiriyor.

**DERS (tek örnekten kural çıkarma):** bir referans karesinde görülen muameleyi
"bu kart tipinin dili" sanmak, o karenin hangi İÇERİĞİ gösterdiğine
bakmamaktan geliyor. İkinci bir örnek görmeden genelleme yapılmamalı.
Bu turda aynı hatanın iki örneği oldu: tente derinliği (üç farklı pencere, üç
farklı sonuç) ve altın çerçeve (tek karta bakıp bütün tipe uygulamak).

## DERLEME HATASI VE DOĞRULAMA (2026-08-22 12:33)

Kullanıcı Unity'ye tıkladı, derleme çalıştı ve **KIRILDI**:

    StoreScreen.cs(1079,21): error CS0136: A local or parameter named 'label'
    cannot be declared in this scope ...

Flama kodumda `var label` kullanmıştım; aynı metotta prosedürel yedeğin `label`ı
var ve C# iç içe kapsamda aynı adı kabul etmiyor. `ribbonText` olarak
yeniden adlandırıldı.

**DERS (ayraç denetimi yetmiyor):** Bu turlarda C# değişikliklerinden sonra
`{`/`}` dengesini HEAD'e karşı kontrol ediyordum. O denetim SÖZDİZİMİ
dengesini görüyor ama KAPSAM/ad çakışması gibi anlamsal hataları görmüyor.
Tek güvenilir kontrol Unity konsolu — ve o da ancak Unity derledikten sonra.
Bundan sonra derleme tamamlandığında MUTLAKA `Unity_GetConsoleLogs` ile
hata kontrolü yapılacak.

### Doğrulanan iki değişiklik

**1. "Best Value" / "Popular" flaması** — artık havuzun altın kenarlı köşe
flaması (`ribbon_popular` -> `ribbon_reward.png`). Eski düz pembe + koyu rim
şerit gitti. Görsel doğrulama yapıldı.

**2. Teklif kartının altın çerçevesi KALKTI** — `GoldRim` nesnesi artık
kurulmuyor (kodla doğrulandı: "GoldRim hala var mi: False"), kart düz turuncu
ve referanstaki "Başlangıç Paketi" ile aynı. Görsel doğrulama yapıldı.

## AYARLAR EKRANI (2026-08-22)

Yapı referansla BİREBİR: başlık + kırmızı X, lavanta kart içinde dört satır
(zil/hoparlör/nota/titreşim + Off/On anahtarı), yeşil Support, yan yana iki
mavi düğme, altta soluk "Delete My Account".

**Ölçüm (mavi maskesi, iki tarafa AYNI kod; referans WhatsApp 946×2048):**

| | referans | önce | sonra |
|---|---|---|---|
| mavi düğme yüksekliği | %13.6 | %11.0 | **%13.8** |
| toplam genişlik | %82.0 | %81.7 | %81.3 |
| sol düğme X | 0.089-0.463 | 0.091-0.468 | — |
| sağ düğme X | 0.535-0.909 | 0.531-0.907 | — |

`Legal` satırı 148 -> **184** birim. (148 birim ekranda %11.0 çiziliyordu;
hap düğmenin iç payı oranı 0.80, hedef 0.136×1080/0.80 = 184.)
Derleme doğrulandı (assembly 12:43:00 > kaynak 12:42:39), konsolda hata YOK.

**GÖZLE YANILDIM, ÖLÇÜM DÜZELTTİ (bu turlarda kaçıncı kez):**
"Bizim Terms/Privacy düğmeleri daha geniş ve kenarlara itilmiş" sanmıştım.
Ölçüm: genişlik ve konum ZATEN neredeyse birebir tutuyordu; tek sorun
yükseklikti. Genişliğe dokunsaydım çalışan bir şeyi bozacaktım.

### Ayarlar'da ölçülemeyen

- Başlık yazısı: referans %5.71, bizim %9.35 ÇIKTI ama bu ölçüm KİRLİ —
  referansın başlığında kalın mor kontur var ve beyaz maskem yalnız çekirdeği
  görüyor, bizimkinde kontur ince olduğu için daha çoğu beyaz sayılıyor.
  Kontur kalınlığı farkı ölçümü bozuyor; dokunulmadı.
- Yeşil "Support" düğmesi: referans JPEG'inin yeşili maskeyi geçmiyor
  (120×14 gibi saçma sonuç). Ayrı bir maske gerekiyor; dokunulmadı.

## PROFİL — ölçüldü, DÜZELTİLMEDİ (kullanıcı önceliği değiştirdi)

Yapı referansla eşleşiyor (başlık + X, avatar kartı + seviye, "Genel
İstatistikler" ayracı, iki sütun kutucuk). Boş isim ve "Level 1" ÖNİZLEME
yan etkisi, kusur değil.

**ÖLÇÜM (sol sütundan dikey tarama, iki tarafa AYNI kod):**

| | referans | bizim |
|---|---|---|
| kutucuk yüksekliği | %8.4 | **%14.7** |
| dikey aralık | %10.4 | **%23.6** |
| aynı alana sığan | 8 | 4 |

Kutucuklarımız yaklaşık **%75 fazla uzun**. Tek başına en büyük profil kusuru.
SONRAKİ TURDA YAPILACAK.

**Ayrıca içerik farkı:** referansta 8 istatistiğin 8'inde FARKLI ikon var
(madalya, nilüfer, damalı bayrak, hedef, yıldız, kupa, roket, ufo); bizde
kupa/ufo/roket/küre/yıldız tekrar ediyor. Havuzda karşılık aranmalı.

## ALT MENÜ (SEKME ÇUBUĞU) — kullanıcı önceliği (2026-08-22)

### Yerleşim ZATEN DOĞRUYMUŞ

- Çubuk yüksekliği: `0.1035` (ekran YÜKSEKLİĞİNİN oranı). İkon yüksekliği
  ölçüldü — referans %18.5, bizim %18.7 → **tutuyor**, yani sabit doğru.
- Seçili kart: kod yorumunda zaten ölçülmüş (referans %27.3, bizim %25.6,
  yayılım 1.30 slot, yükseklik çarpanı 1.42).
- İlk ölçümümde "kart %18.6 vs %11.9" çıkmıştı — o kartı DEĞİL ikon+etiketi
  ölçüyordu ve fark yalnız kelime uzunluğundan ("Ana Ekran" vs "Home").

### İKONLAR BLOCK OUT!'UN KENDİSİNDEN KESİLDİ

Sekme çubuğu düz mor zemin (#5140E4) olduğu için booster/geri-al glifinde
işe yarayan yöntem burada da çalıştı: beş slot ayrı ayrı kırpıldı, mor+koyu
zemin kenardan taşma (flood fill) ile silindi, merkeze bağlı bileşen tutuldu.

    icon_shop       512×508 -> 97×94    (pembe tenteli magenta dükkân)
    icon_trophy     449×512 -> 112×106  (altın kupa + PEMBE YILDIZ — bizde yıldız YOKTU)
    icon_home       471×512 -> 176×112  (pembe çatı, ALTIN duvarlar; bizimki krem/soluktu)
    icon_globe      382×512 -> 98×120   (doygun magenta küre)
    icon_album      511×512 -> 126×101  (altın çerçeveli kare + pembe noktalar)

**Doğrulama:** yakalandı ve referansla yan yana konuldu — beşi de eşleşiyor.
Eski ikonlar belirgin SOLUKTU; asıl fark boyut değil DOYGUNLUKTU.

Çözünürlük 512 -> ~100 düştü (kaynak 946 px ekran görüntüsü). Sekme
ikonları ekranda ~110 birimde çiziliyor, yani 1:1'e yakın.

## İKON ÇÖZÜNÜRLÜK/KALİTE SORUNU — çözüldü (2026-08-22)

Kullanıcı sekme ikonlarında "çözünürlük problemi" bildirdi. **İKİ ayrı sebep
vardı ve ikisi de çözünürlük DEĞİLDİ:**

### 1. ASTC blok sıkıştırma
Bütün arayüz dokuları `ASTC_6x6` ile sıkıştırılıyordu. ~100 px'lik ikonlarda
bu format yıkıcı: kupanın yıldızı, evin pencereleri gibi ince detaylar
bloklaşıyor. Eski 512 px'liklerde aynı ayar vardı ama beş kat çözünürlükte
fark edilmiyordu.
**256 px altındaki 24 doku `Uncompressed` yapıldı.**
(Bu `logo_*.png` dosyalarının .meta'sını da değiştirdi — sanata dokunulmadı,
yalnız içe alma ayarı; başkası logo üzerinde çalışıyorsa haberdar olmalı.)

### 2. İKİLİ ALFA — asıl sebep
İlk kesme yöntemim alfayı 0 veya 255 yapıyordu: kenar yumuşatması yok ve
JPEG'in zeminle karışmış kenar pikselleri kalıyor -> **koyu tırtıklı hale**.

**Çözüm: yumuşak matleme.**
    alpha = clamp((|piksel - zemin| - 28) / (70 - 28), 0, 1)
    renk  = zemin + (piksel - zemin) / alpha        (un-premultiply)
    sonra alpha < 0.18 olan pikseller TAMAMEN saydam yapıldı
(o düşük alfalı kenar pikselleri kaynak zeminin rengini taşıyıp soluk bir
DİKDÖRTGEN hale bırakıyordu — evde açıkça görülüyordu)

### Yolda çıkan iki tuzak

**Rozet silme tenteyi yedi.** Dükkân ikonundaki kırmızı "1" bildirim rozetini
renk testiyle silmeye çalıştım; tente de pembe-magenta olduğu için o da gitti.
Çözüm: rozeti renkle değil KONUMLA sil (sağ üst çeyrek kutusu).

**Ev yanlış kareden kesildi.** `ana ekran.jpeg`'te Ana Ekran SEÇİLİ, yani ev
kartın üstünde ve zemin rengi farklı. `market.jpeg`'te Mağaza seçili olduğu
için ev DÜZ çubukta — oradan kesildi. Ayrıca seçili sekme geniş olduğu için
diğer sekmeler KAYIYOR; slot aritmetiği tutmadı, ev pembe çatısından
bileşen seçilerek bulundu.

**Doğrulama:** sekme çubuğu referansla yan yana konuldu — beş ikon da eşleşiyor,
hale yok, kenarlar temiz.

## ANA EKRAN — ölçüldü, yerleşim DOĞRU çıktı (2026-08-22)

**Üst bar ölçümleri (iki tarafa AYNI kod):**

| | referans | bizim |
|---|---|---|
| yeşil "+" #1 | X %45.6-51.2, gen %5.6 | X %45.9-50.9, gen %5.0 |
| yeşil "+" #2 | X %77.2-82.7, gen %5.5 | X %77.4-82.5, gen %5.1 |
| yükseklik | %4.9 | %5.2 |

Konumlar 0.3 puan içinde. Gözle "bizimkiler daha küçük ve köşeli" sanmıştım —
yine yanıldım, fark 0.5 puan. DOKUNULMADI.
Jeton ikonu bu turlarda değiştirildiği için artık referansla birebir.

**Ana ekranın gerçek eksikleri — hiçbiri ölçüyle çözülecek türden DEĞİL:**

1. **ARKA PLAN (en büyük fark).** Referans: LEGO tuğlalı, dolu bir köy —
   binalar, yollar, tezgâhlar. Bizimki: seyrek bir gün batımı sahnesi.
   REFERANSTAN KESİLEMEZ: üstünde arayüz ve karakterler var, temiz bir
   arka plan çıkarmak mümkün değil. Yeni sanat gerektiriyor.
2. **Eksik yan widget'lar:** arkadaş avatarları ("Bitti"/"Katıl"), yıldız
   rozeti + "15g 0s" geri sayımı, can geri sayımı ("15:25"). ÖZELLİK eksiği.
3. **Can sayısı kalbin İÇİNDE olmalı** (referansta kalbin ortasında "5");
   bizde kalp düz, sayı ayrı. Yerleşim değişikliği.
4. **Oynat düğmesi rengi** — referans mor, bizde yeşil; ama referanstaki bölüm
   "Zor Seviye" ve Block Out! rengi zorluğa göre değiştiriyor. Normal durumu
   bilmediğimiz için DOKUNULMADI (bkz. 3. iterasyon).

## PROFİL KUTUCUKLARI — düzeltildi (2026-08-22)

`boxH` 161 -> 99, `gapY` 94 -> 8.

**Ölçüm üç denemede oturdu — ilk ikisi YANLIŞTI:**
1. Parlaklık eşiği (x=%14): "referans %8.4, bizim %14.7" dedi. Bu değerlerle
   boxH'yi 92'ye indirdim ve GÖRSEL BOZULDU — ikonlar etiketlere bindi.
2. Ortanca eşiği (x=%25): 29-50 px değerler verdi; bunlar kutucuk değil YAZI
   SATIRLARIYDI.
3. **Çalışan yöntem: kutucuk zemin RENGİNİ eşleştirmek.** Referansta ekran
   zemini `#302488`, kutucuk zemini `#1E1652`; bizde `#322288` / `#1C164F`.
   Renk eşleşmesiyle:
       referans yükseklik %8.1, dikey aralık %9.9
       bizim (boxH 92)     %7.6 / %10.4
   0.081×1080 = 88 px çizim -> boxH 99 (oran ~0.90); aralık 107 -> gapY 8.

**Doğrulama: %8.2 / %9.9** (hedef %8.1 / %9.9).

**DERS:** eşik tabanlı ölçüm bu ekranda üç kez yanlış sonuç verdi çünkü
kutucuk zemini ile ekran zemini arasındaki parlaklık farkı, yazı ile zemin
arasındaki farktan KÜÇÜK. Zemin rengini örnekleyip RENK EŞLEŞTİRMEK tek
güvenilir yol oldu.

**HÂLÂ FARKLI (dürüst not):** kutu yüksekliği ve aralığı artık eşleşiyor ama
referansın kutucuklarında İÇ BOŞLUK daha dengeli — etiket bir satır, değer
altında rahat duruyor; bizde değer alt kenara sıkışmış ve ikon üst kenara
fazla biniyor. Bu kutu ölçüsü değil İÇ YERLEŞİM meselesi, ayrıca ölçülmeli.

**Ayrıca:** referansta 8 istatistiğin 8'inde FARKLI ikon var; bizde
kupa/ufo/roket/küre/yıldız tekrar ediyor. Havuzda karşılık aranmalı.

### Sonuç paneli (PERFECT kartı) — 9. tur

**Kaynak:** `_Reference/frames/m_041.jpg` (video karesi, **384×832 ham** — layout ve
renk için kullanılabilir, ince ölçüm için değil).

**Ölçüm (dış çerçeve, mor maskesi, genişliğe oranla):**

| | genişlik | yükseklik | en/boy |
|---|---|---|---|
| referans | %93,5 | %134,8 | 0,69 |
| bizim (önce) | %90,4 | %121,9 | 0,74 |
| bizim (sonra) | %93,4 | %134,8 | **0,69** |

Kart genişliğine göre ALÇAKTI. `GameplayScreen.cs`:
- `WinCardX0/X1` 0,049–0,953 → **0,033–0,968**
- `WinCardY0/Y1` 0,259–0,803 → **0,121–0,879** (ekran merkezine oturtuldu)

**Maske tuzağı:** ilk ölçümüm dar mor aralığı kullanınca kartın DIŞ koyu
çerçevesini değil İÇ panelini ölçtü ve %79,3 dedi; sabit ise %90,4'tü.
Aralık genişletilince sabitle birebir tuttu. *Ölçüm sabitle çelişiyorsa önce
maskeyi sorgula.*

**Doğrulanan, dokunulmayanlar:**
- başlık konumu: kart üst kenarına uzaklık referans +%8,8 / bizim +%8,2 ✔
- kapatma düğmesi: kart köşesine göre fark ~%1,5 ✔

**ÖLÇÜLEMEDİ (dokunulmadı):**
- başlık harf yüksekliği — video sıkıştırması sarı gövdeyi inceltiyor
  (referans %8,1 çıkıyor, gözle ~%13; genişlik/harf ise 6,68 vs 7,08 ile yakın)
- "Seviye 53" yazısı — beyaz eşiği referansta griye kaçan yazının yalnız
  birkaç pikselini yakaladı (%4,7 genişlik = anlamsız)
- seviye adının PLAKASI: 8. turda üç kesitle ölçülüp "plaka değil, kalın
  kontur" diye kaldırılmış. 384px kaynakta bu ayrım yapılamaz — geri
  eklemedim.

**Yorum düzeltmesi:** "PERFECT! kartın DIŞINDA duruyor" notu kart büyüyünce
kodla çelişir hâle geldi; ölçümle birlikte güncellendi. *Yanlış yorum,
yanlış koddan uzun yaşar.*

### Duraklat paneli — 9. tur

**Referans yok.** `_Reference/frames/` içindeki 52 karenin hepsi Magic Sort
videosundan (443×960) ve duraklat panelini içermiyor. Bu yüzden referans
ölçümü DEĞİL, **iç tutarlılık** ölçütü kullanıldı: duraklat kartı sonuç
kartıyla aynı yüzeyi taşımalı.

**Bulgu:** duraklat kartı hâlâ `panel_card` sprite'ını kullanıyordu — sonuç
kartının 8. turda tam da bu yüzden terk ettiği görsel. Ekranda ikisi de
duruyordu: içinde basılı ikinci çerçeve ve gövdeden kopuk alt dudak.

**Ölçüm (kart orta yüksekliği, kenar rengi):**

| | genişlik | kenar rengi |
|---|---|---|
| sonuç kartı | %93,4 | (49, 33,165) |
| duraklat (önce) | %92,6 | (46, **2**,146) |
| duraklat (sonra) | %93,4 | (49, 33,165) |

Aynı oyunda iki ayrı mor vardı ve fark sabitten değil, `CardPurple` ile
boyanmasına rağmen sprite'ın kendi baskısından geliyordu.

`GameplayScreen.cs` — `_pauseCard` artık `CreateRoundedPanel` + dört bant
(Groove/Inner/Line/Face), sonuç kartıyla birebir aynı profil; X sınırları
`WinCardX0/X1`e bağlandı.

**DERS (bir "bu varlığı kullanma" kararı, TÜM çağrı yerlerini kapsamalı):**
8. turda sprite yalnız sonuç kartından kaldırılmıştı; duraklat paneli aynı
hatayı bir tur daha taşıdı.

**Yanılıp ölçümle düzeltildi:** küçültülmüş önizlemede satır etiketleri koyu
göründü, "kontrast sorunu" sandım. Ölçüm: en açık piksel **(255,250,240)** —
etiketler zaten `Ink`, yani krem beyaz. Dokunulmadı. Simge rengi `PauseGlyph`
de S64'te ölçülmüş bir karar, korundu.

### Profil — sayaç ızgarası — 9. tur

**Not:** `_Reference/refs/profile.png` YANLIŞ ADLANDIRILMIŞ — içeriği profil
değil, ana ekran. Profil referansımız (`profil.jpeg`, 946×2048) artık diskte
yok; ölçümler kendi yakalamamız üzerinden ve koddaki hedef oranlara karşı
yapıldı.

**1) Türetme hatası (kanıtlandı).** Kod `boxH 99` değerini "89 px çizim →
oran ~0,90" varsayımıyla türetmişti. Aynı notun birkaç satır yukarısı oranı
0,99 diyordu; ölçüm ise **1,00** verdi:

| | kutucuk | adım |
|---|---|---|
| hedef (referans) | %8,2 | %9,9 |
| önce | %9,17 | %9,91 |
| sonra | **%8,24** | %9,91 |

`boxH 99 → 89`, `gapY 8 → 18`. Adım zaten doğruydu; fazlalık kutucuktaydı.

**DERS (ölçekleme oranını VARSAYMA, ölç):** birim→piksel oranı bu tuvalde
1,00; iki turda 0,99 ve 0,90 diye tahmin edildi, ikincisi kutucuğu %11
şişirdi.

**2) Değerler görünmüyordu.** Aralık 8 birim, ikon taşması 30 birim → her
satırın ikonu bir üstteki kutunun değerini örtüyordu (ilk üç satırda değer
hiç görünmüyordu). `gapY` 18'e çıkınca örtme bitti.

**3) Değer dibe yapışıktı ve puntosu geçersizdi.** 50 punto, kutu-göreli
0,04-0,40 = 32 birimlik kutu → `UiTextFit` her açılışta küçültüyordu, yani
yazılan punto ekranda hiç geçerli olmadı. **34 punto, 0,11-0,41** yapıldı.
*Kutuya sığmayan punto, yazılmamış puntodur.*

**4) İkon çeşitliliği — denendi, geri alındı.** `First Try Wins` için
`Art.Check` denendi (Trophy tekrarını kırıyordu). Anlamı doğruydu ama ekranda
sırıttı: diğer yedi ikon parlak/hacimli, `check_green` düz vektör. Geri
alındı. Kalan tekrarların çoğu adın kendisinden geliyor ve doğru
(Rocket Race→roket, Max Ufo Climb→UFO). *Anlamlı tekrar, üslubu bozan
çeşitlilikten iyidir.*

### Ana ekran + alt menü — 9. tur

Alt menü `MenuShell.CreateTabBarPreview` ile birlikte yakalandı; sekmeler,
seçili kart ve ikonlar referansla uyumlu, dokunulmadı. (Referanstaki mağaza
sekmesinde kırmızı "1" bildirim rozeti var; bizde karşılığı olan veri yok,
*sahte sayı sahte oyundur* gereği eklenmedi.)

**1) Oyna düğmesinin üstünde boş turuncu şerit.** `_rewardRibbon`'ı yalnız
`Refresh` açıp kapatıyor (`multiplier > 1`) ama kurulum onu AÇIK bırakıyordu —
ilk karede ve Refresh'in hiç gelmediği her durumda içi boş bir ödül şeridi
duruyordu. Kurulumda `SetActive(false)`.
*Varsayılan durum, yayın durumudur: boş bir ödül şeridi olmayan bir ödülü
vaat ediyor.*

**2) Kaynak çubukları solgundu.** Çubuk yarı saydam (alfa 0,50) ve ekrandaki
sonucu ARKA PLAN belirliyor; referansın arkasında koyu bir köy, bizim
arkamızda açık bir gökyüzü var.

| çubuğun içi (y=85, x=0,32) | R | G | B |
|---|---|---|---|
| referans | 64 | 28 | 98 |
| önce | 78 | 56 | 129 |
| sonra (alfa 0,72) | **60** | **43** | **101** |

*Yarı saydam bir rengi sabitinden yargılama — referanstan alınacak olan
sabit değil, ekranda okunan sonuçtur.*

**3) Oyna düğmesi %19 küçüktü.**

| | genişlik | yükseklik | en/boy | çubuk boşluğu |
|---|---|---|---|---|
| referans | %52,9 | %18,8 | 2,82 | %13,8 |
| önce | %42,8 | %15,0 | 2,85 | %12,8 |
| sonra | **%52,8** | **%18,9** | **2,79** | **%13,5** |

En/boy zaten birebirdi; yalnız ölçek küçüktü. Düğme AŞAĞI değil yukarı ve
yanlara büyütüldü: koddaki "düğme %17'den başlamalı" kuralı mutlak bir sayı,
ölçülen şey ise düğme ile sekme çubuğu arasındaki paydı ve o zaten doğruydu.
*Korunması gereken sayının kendisi değil, iki öğe arasındaki nefes payı.*
Yazı puntosuna dokunulmadı — referans ölçüsü düğmeye değil EKRANA göre
alınmıştı.

**Yanılıp ölçümle düzeltildim (iki kez):**
- "Üst barda tek uzun bant var, referansta iki ayrı plaka" dedim. Kod zaten
  iki ayrı çubuk kuruyor (0,262-0,511 ve 0,574-0,825) ve tam bu hataya karşı
  bir ders notu taşıyor: gevşek eşik ikonların gölgesini de koyu sayıp iki
  çubuğu tek parça gösteriyor. Benim maskem de aynısını yaptı.
- Çubuk rengini y=38'de ölçüp "değişiklik ekrana yansımadı" sandım; o hiza
  çubuğun kendi aralığının (y 46-123) dışındaydı.

**Yapılmayanlar (karşılığı olan veri yok):** sol kenardaki üç etkinlik rozeti
(Bitti / Katıl / geri sayım) ve sağ üstteki ödül rozeti. Bunlar bizde olmayan
etkinliklere ait.

### Günlük ödül paneli — 9. tur

Yakalandı (`Build` paneli kapalı kuruyor, önizleme için elle açıldı). Yapı
sağlam: kapsül başlık kartın üst kenarına biniyor, 7 gün kutucuğu, CLAIM
düğmesi oyunun standart düğmesi. **Referansı yok** — 52 karenin hiçbirinde
günlük ödül paneli geçmiyor. ÖLÇÜLEMEDİ, dokunulmadı.

### Kare arşivi tarandı — GERÇEK REFERANSLAR BULUNDU

`_Reference/frames/` içindeki 52 kare tek bir kontak sayfasına dizildi
(`_Reference/notes/frames_sheet.png`). Bunlar Magic Sort değil, **Block Out!'un
kendi kareleri**. İçerdikleri:

| ekran | kareler |
|---|---|
| mağaza | m_001–m_005 |
| liderlik | m_006, m_007 |
| yolculuk | m_009–m_014, m_047 |
| koleksiyon | m_015 |
| ayarlar | m_019 |
| **profil** | **m_020, m_051** |
| oyun tahtası | m_022–m_039 |
| sonuç paneli | m_041 |
| Yıldız Patlaması etkinliği | m_042, m_043 |
| Gökyüzü Atlayışı etkinliği | m_045 |
| ana ekran | m_044, m_046, m_048–m_050 |

*Ders: elde duran kaynaklar taranmadan "referans yok" denmemeli.* Bu turda
profil için iki kez "referans yok" deyip varsayımla ilerlemiştim.

### Profil ızgarası — gerçek referansla yeniden (aynı tur, üç düzeltme)

**1) Kutucuk ölçüsü — iki yanlış, sonra doğru.**

| | kutucuk | adım |
|---|---|---|
| kodun eski hedefi (kaynağı silinmiş) | %8,2 | %9,9 |
| YATAY taramayla "referans" | %7,90 | %10,61 |
| **DİKEY taramayla gerçek referans** | **%18,5** | **%28,9** |

Yatay tarama kutucuğun İÇİNDEKİ ikon/etiket/değer tarafından bölünen
şeritleri ölçüyordu. Doğrusu: içeriğin uzanmadığı bir sütunda dikey tarama.
Referansın kutucuğu bizimkinin **iki buçuk katıymış** — gözle zaten öyle
görünüyordu, sayı iki tur boyunca gözü yanılttı.

Birebir oran uygulanamıyor: referans karesi 443×960 (2,167), bizim tuval
1080×1920 (1,778); genişliğe göre ölçeklenen ızgara 1136 birim tutup ekranın
dışına taşıyor. Referansın ORANI korundu, ölçeği bütçeye sığdırıldı:
`boxH 170`, `gapY 96` → kod oranı 170/266 = **0,639**, referans
18,74/29,35 = **0,639**.

**2) Değer/etiket hiyerarşisi tersti.**

| | değer | etiket | oran |
|---|---|---|---|
| referans | %4,29 | %2,26 | **1,90** |
| önce | %2,26 | %2,41 | 0,94 |
| sonra | %3,70 | %1,94 | **1,90** |

Bizde etiket değerden büyüktü — oyuncunun aradığı sayı, o sayının ne olduğunu
söyleyen yazının altında eziliyordu. Değer 34→68 punto, etiket 30→25.

**3) "Fit küçültüyor" varsayımı yanlıştı.** 62 punto %3,33, 55 punto %2,96
verdi — oran 1,125, punto oranı 1,127. Punto doğrudan ekrana gidiyor.
*Bir varsayımı tek ölçümle sınama; iki nokta doğrusal ilişkiyi gösterir.*

### Ayarlar — gerçek referansla yeniden (9. tur, `m_019`)

Bu ekran daha önce "bitti" sayılmıştı; ölçüleri artık diskte olmayan
kaynaklara dayanıyordu.

**Yöntem düzeltmesi:** oranların hepsi GENİŞLİĞE bölündü, çünkü
`CanvasScaler.matchWidthOrHeight = 0`. İki ekranı aynı YÜKSEKLİĞE ölçekleyip
yan yana koyunca satır adımları birebir aynı çıkıyordu (64 piksel) — fark
yalnız genişliğe oranla görünüyor. *Karşılaştırmayı, düzenin ölçeklendiği
eksene göre yap; yanlış eksende hazırlanmış bir görsel gerçek farkı gizler.*

| | referans | önce | sonra |
|---|---|---|---|
| satır adımı | %16,70 | %13,61 | **%16,67** |
| kart yüksekliği | %70,40 | %54,40 | **%70,40** |
| kart genişliği | %93,7 | %91,9 | (dokunulmadı) |
| anahtar yüksekliği | %8,80 | %8,06 | (dokunulmadı) |
| Support en/boy | 2,77 | 4,37 | **2,67** |
| Support üst kenar | %122,6 | %118,1 | **%123,2** |
| Terms/Privacy | %82,6 / %13,8 | %81,9 / %13,8 | (zaten birebir) |
| Terms üst kenar | %149,2 | %136,0 | **%149,7** |

`rowH 147→180`, `cardH 604→776`, Support `132→185` offset / `156→227`
yükseklik / `0,24-0,76 → 0,265-0,735`, Legal `324→440`.

**GÖZ YAKALADI, ÖLÇÜM YAKALAYAMADI:** bütün oranlar birebir tuttuktan sonra
ekranda "Delete My Account" Terms/Privacy düğmelerinin ÜSTÜNDE duruyordu.
Sebep: Support ve Legal karta göre konumlanıyor, Delete ise MUTLAK (1672).
Legal aşağı kayınca tam Delete'in içine girdi. Delete 1672→1790.
*Karışık yerleşimde göreli bir öğeyi oynatmak, mutlak komşusunun üstüne
biner ve hiçbir oran ölçümü bunu yakalamaz.*

**Sığmayan kısım:** referansın alt bölümü (Support üstünden Delete altına)
genişliğe oranla %88 = bizde 950 birim; kartın altı 1139'da bittiği için
2274'e, ekranın 354 birim dışına taşıyor. Support ve Terms referans yerinde
bırakıldı, fazlalık Terms↔Delete arasındaki büyük boşluktan kısıldı.

**Ölçülüp dokunulmayan:** kart rengi referans (134,109,252), bizim
(140,125,254) — bizimki az doygun. Renk `MenuPage` üzerinden geliyor ve
başka ekranları da etkiler; tek başına değiştirmek riskli, not edildi.

### Liderlik panosu — gerçek referansla (9. tur, `m_006`)

**Önizleme yöntemi genişledi:** `LeaderboardScreen.Refresh()` düzenleyici
kipinde çağrılabiliyor. Çağrılmadan bakınca sekmelerin üçü de beyaz, geri
sayım rozeti boş bir mavi kapsül görünüyor — ikisi de artefakt, gerçek kusur
değil. *Bir ekranı yargılamadan önce tazelemesini çağırmayı dene.*

**Düzeltilen — sekme yuvası ve "i" düğmesi.** Eski ölçüler artık diskte
olmayan `sıralama.jpeg`ten geliyordu. Yuvanın orta yüksekliğinden yatay kesit:

| | yuva | "i" düğmesi |
|---|---|---|
| referans | %9,3–%86,0 (gen %76,7) | %90,7–%97,7 (gen %7,0) |
| önce | %10,7–%80,0 (gen %69,3) | %84,4–%92,3 |
| sonra | ...–%86,7 | **%90,3–%97,5 (gen %7,3)** |

`slotRow 0,107-0,803 → 0,093-0,860`, `info 0,836-0,936 → 0,907-0,977`.
Sabitlerle birebir örtüştüğü için ölçüme güvenildi.

**ÖLÇÜLDÜ, DOKUNULMADI — liste alanı (aspect kısıtı).** Referansta liste
ekranın %44'ünü kaplıyor, bizde %19. Ama referansın liste alanı genişliğe
oranla %96 ve bizim tuvalde 1063→2099 birim tutuyor; ekran 1920'de bitiyor,
üstelik altta sekme çubuğu var. Mevcut düzen (liste 1075-1446, "You" 1470,
alt menü ~1720) bu bütçeyi zaten paylaşıyor. Zorlamak "You" satırını sekme
çubuğunun altına iterdi.

**ÖLÇÜLEMEDİ:** satır yüksekliği/adımı — satırlar çok katmanlı (rozet, avatar
çerçevesi, ödül plakası) ve renk maskesi katmanları satır sanıyor. Satır
zemin renkleri uyumlu çıktı: referans (112,89,229), bizim (108,91,232).

**KESİNLEŞTİRİLEMEDİ — podyum/liste tekrarı.** Bizde podyumdaki üç oyuncu
listenin ilk üç satırında yeniden görünüyor (`board[0]`'dan başlıyor).
Referans karesinde liste 6. sıradan başlıyor ama bu kaydırılmış bir durum
olabilir (m_007'de dünya sıralaması 998-1001 arasını gösteriyor, yani liste
oyuncunun bulunduğu yere kayıyor). Liste 1'den mi 4'ten mi başlıyor
ayırt edilemedi — dokunulmadı.

**Yapısal fark (tasarım kararı, korundu):** referansta oyuncu listenin
İÇİNDE vurgulanıyor; bizde ayrı sabit bir "You" satırı var. Bizimki yaygın
ve savunulabilir bir desen.

### Koleksiyon — gerçek referansla (9. tur, `m_015`)

`CollectionScreen.Refresh()` de düzenleyici kipinde çağrılabiliyor; çağrılmadan
bakınca "Unlock Collection at Level 95!" plakası boş bir kutu görünüyor.

**Sanat boyutu zaten doğruymuş.** Kod hedefi %84,5; benim ölçümüm referansı
**%84,9** buldu. Dokunulmadı.

**Düzeltilen — arka plan gradyanı.** Referansın arka planı dikey gradyanlı,
bizimki düz:

| hiza | referans | önce | sonra |
|---|---|---|---|
| y%16 | (12,13,43) | (28,22,88) | **(16,16,49)** |
| y%35 | (20,18,57) | (28,22,88) | **(19,19,56)** |
| y%55 | (20,16,53) | (28,22,88) | **(22,22,62)** |
| y%75 | (30,28,77) | (28,22,88) | **(24,24,68)** |
| y%88 | (27,25,74) | (28,22,88) | **(26,26,72)** |

**Gradyan `MenuPage`E EKLENMEDİ — üç ekran ölçüldü:**

| ekran | arka plan |
|---|---|
| ayarlar (m_019) | düz (28,19,76) |
| profil (m_051) | düz (42,25,119) — bizimki zaten birebir |
| koleksiyon (m_015) | **gradyanlı** |

Yani bu kabuğun değil bu sayfanın kararı: ortadaki kitap illüstrasyonunu
taşıyan bir sahne ışığı. Ortak koda konsaydı doğru olan iki ekranı bozardı.
*Ortak koda taşımadan önce üç örnek ölç — bir ekranda görülen şey "oyunun
dili" olmayabilir, o sayfaya ait bir vurgu da olabilir.*

## ⚠ YÖNTEM HATASI VE DÜZELTMESİ — 9. tur (ÖNEMLİ)

Bu turda "tüm oranları GENİŞLİĞE böl" diye bir kural benimsedim ve
`SettingsScreen`e "çünkü `matchWidthOrHeight = 0`" diye yazdım. **Yanlış.**

`Assets/GameKit/Runtime/UI/UiKit.cs:76` → **`scaler.matchWidthOrHeight = 1f`**
(YÜKSEKLİK). Kanvas her cihazda 1920 birim yüksek; genişliği cihaza göre
değişiyor.

**Doğru kural — her eksen kendi kenarına bölünür:**
- **X** ölçüleri `Place` ile normalize → ekran **GENİŞLİĞİNE** oranla
- **Y** ölçüleri (`MenuPage.Row`un `top`/`height`i, birim cinsinden) → ekran
  **YÜKSEKLİĞİNE** oranla

**Doğrulama yolu:** yakalamayı referansın EN-BOYUNDA al. Referans 443×960 =
0,4615 → **886×1920** ve `matchWidthOrHeight = 1`. O zaman iki eksen aynı
ölçeğe gelir ve fark ortaya çıkar. (1080×1920 ile yakalamak aspect'i
1,778'e çekiyor ve dikey farkı gizliyor.)

### Ayarlar — gerileme geri alındı

| | referans | bozuk (bu tur) | düzeltme |
|---|---|---|---|
| satır adımı (yüksekliğe) | %7,71 | %9,38 | **%7,71** |
| kart yüksekliği | %32,50 | %39,58 | **%31,67** |

`rowH 180→148`, `cardH 776→624`, Support offset `185→99` / yükseklik
`227→186`, Legal offset `440→335` / yükseklik `184→151`, Delete `1790→1804`.
Support'un X daralması (0,52→0,47) DOĞRUYDU, korundu.

### Aynı hatadan etkilenen, SONRAKİ TURDA kontrol edilecekler

- **Sonuç kartı** `WinCardY0/Y1 = 0.121-0.879` (1455 birim). Referans
  yüksekliği 597 px / 960 = %62,2 → hedef **1194 birim** = 0,189-0,811.
  Genişlik (0,033-0,968 = %93,5) doğru.
- **Ana ekran oyna düğmesi** `0.158-0.290` (253 birim). Referans 83 px / 960
  = %8,65 → hedef **166 birim**. Genişlik (%52,8) doğru.
- **Profil ızgarası** `boxH 170 / gapY 96` (adım 266). Referans 82 px ve
  128 px / 960 → hedef **164 / 92** (adım 256). Sapma %4, sınırda.
- **Profil değer 68 punto** → ölçülen 40 birim harf; hedef 38. Sapma %5, kabul.

**Etkilenmeyenler (yalnız X veya renk):** liderlik sekme yuvası ve "i"
düğmesi, koleksiyon gradyanı, duraklat kartı, ana ekran çubuk alfası.

### Eksen hatasından etkilenenler düzeltildi (9. tur, devamı)

Hepsi referansın en-boyunda (886×1920, `matchWidthOrHeight = 1`) yakalandı;
X oranları genişliğe, Y oranları yüksekliğe bölündü.

**Sonuç kartı** (`GameplayScreen.WinCardY0/Y1`)

| | genişlik | yükseklik | en/boy |
|---|---|---|---|
| referans | %93,5 | %62,19 | 0,69 |
| önce (0,121-0,879) | %93,6 | %75,83 | 0,57 |
| sonra (0,203-0,797) | %93,6 | **%63,59** | **0,68** |

Genişlik zaten birebirdi, kart yalnız dikeyde şişmişti. İlk düzeltme
(0,189-0,811) %65,00 verdi; aradaki fark kartın kenar bantlarından geliyor,
aralık ölçülen orana göre küçültüldü.

**Ana ekran oyna düğmesi** (`HomeScreen`)

| | genişlik | yükseklik | en/boy |
|---|---|---|---|
| referans | %52,9 | %8,65 | 2,82 |
| önce (…0,290) | %52,6 | %10,62 | 2,28 |
| sonra (…0,265) | %52,8 | **%8,65** | **2,82** |

Birebir. Alt kenar 0,158 sabit tutuldu — sekme çubuğuyla arasındaki pay daha
önce ölçülüp doğrulanmıştı (%13,5 / %13,8).

**Profil ızgarası — ÖLÇÜLDÜ, DOKUNULMADI.** Doğru eksende zaten uyumlu:

| | kutucuk | adım |
|---|---|---|
| referans | %8,65 | %13,54 |
| bizim (170/96) | %8,54 | %13,85 |

Sapma %1-2; hesapladığım "hedef 164/92" düzeltmesi gereksizdi.

Böylece 9. turdaki eksen hatasının bilinen bütün etkileri giderildi.

### Yolculuk — eksen düzeltmesi (9. tur, `m_009`)

`JourneyScreen`in daire notu eksen konusunda ZATEN DOĞRUYDU (`DiscSize`
yükseklik oranından türetilmiş) ve ölçümde referansla birebir çıktı: daire
çapı %68 / %68. Ama seviye kapsülü ölçüleri "yüksekliği GENİŞLİĞİN %18,5'i"
ve "satır aralığı GENİŞLİĞİN %29,9'u" diye hesaplanmıştı.

| | referans | önce | sonra |
|---|---|---|---|
| kapsül yüksekliği (yüksekliğe) | %8,33 | %10,16 | **%8,49** |
| satır adımı | %13,96 | %16,82 | %13,96 (sabit) |

`PillH 230→189`, `RowHeight 323→268`. Kapsüller arası boşluk artık
268-160 = 108, referansta da aynı. **Genişliğe dokunulmadı** — `PillX0/X1`
(%83,3 sabit → %81,0 çizim) X ekseninde ve zaten doğruydu.

### Kodda yanlış eksen yorumları işaretlendi

Üç yerde "matchWidthOrHeight = 0" yazıyordu; gerçeği `UiKit.cs:76` →
**`= 1`**. Sonraki turları yanıltmasınlar diye hepsi düzeltildi:

- `ProfileScreen.cs:341` — açıklama yanlıştı ama sonuç doğru çıkmış; not
  eklendi (doğru eksende teyit: kutucuk %8,54 / adım %13,85).
- `SettingsScreen.cs:142` — zaten geri alınan bölümün gerekçesi, işaretlendi.
- **`StoreScreen.cs:83` — ciddi.** Tente yüksekliği 268'den 360'a, sonra
  400'e çıkarılırken gerekçe "kanvas GENİŞLİKLE ölçekleniyor" idi. Yanlış:
  ESKİ değer ("ekran yüksekliğinin %13,96'sı" = 268) doğru eksendeydi.
  Doğru hedef ~295 birim (315 px / 2048 = %15,38). Aynı dosyanın 33. satırı
  zaten `= 1` diyor — **dosya kendi içinde çelişiyormuş.** Mağaza turunda
  `m_001`-`m_005` ile ölçülüp düzeltilecek.

*Ders: bir dosyada aynı gerçeğe dair iki çelişkili yorum varsa, biri kesin
yanlıştır ve ona dayanan her sayı şüphelidir.*

### Mağaza — eksen düzeltmesi (9. tur, `m_001`/`m_003`)

İki ölçü de yanlış eksene dayanıyordu ve **her ikisi de ilk değerine geri
döndü** — yani aradaki "düzeltmeler" tamamen gerilemeymiş.

| | referans | önce | sonra |
|---|---|---|---|
| tente (yüksekliğe) | %13,96 | %20,73 | **%13,91** |
| bölüm başlığı | %4,38 | %5,10 | **%4,38** |

- `AwningH 400 → 268` (268→360→400 diye şişirilmişti; **268 doğruymuş**)
- `PillH 122 → 105` (104'tü; **104 doğruymuş**)

Koddaki "referansta tentenin EN DERİN noktası 351 px" iddiası da tutmuyor:
tüm sütunlar tarandığında sol kenar %13,75, en derin nokta %13,96 —
tente neredeyse düz, feston sığ.

**DERS (bir sayı iki kez "düzeltildiyse" ilk hâlini kontrol et):** Bu
dosyada iki ölçü de üç kez değişip başladığı yere döndü. Her turda ölçüm
yapıldı ama hep aynı yanlış kenara bölündü; yanlış yöntem tekrar edilince
"doğrulanmış" gibi göründü.

### ÖLÇÜLDÜ, SONRAKİ TURA — teklif kartı sağa kaymış

Gerçek cihaz en-boyunda (886×1920) teklif kartının fiyat düğmesi ekran
kenarında kesiliyor:

| | sol | sağ | merkez |
|---|---|---|---|
| referans | %3,2 | %97,1 | %50,2 |
| bizim | %4,4 | %99,9 | **%52,2** |

Kart genişliği yakın (%95,5 / %93,9) ama 2 puan sağa kaymış ve sağ kenara
dayanmış; içindeki fiyat düğmesi bu yüzden kesiliyor. `OfferCarousel`in
başlangıç kaydırma konumu ya da kart içi yerleşimi incelenecek.

### Teklif kartı taşması — ARAŞTIRILDI, KÖK NEDEN BULUNAMADI

**Kartın kendisi doğru.** `MarginX = 0,036` → kart %3,6-%96,4, merkez %50.
Referans ölçümü **%3,8-%96,6, merkez %50,2** — birebir aynı. Karusel mantığı
da doğru: her sayfa viewport genişliğinde, `pivot 0,5` ile ortalı.

**Ama sağ kenarda kartın DIŞINDA öğeler var.** 886×1920'de x %97-%99 arası
hâlâ dolu:

| hiza | x%96 | x%97 | x%98 | x%99 |
|---|---|---|---|---|
| y%24 | (251,180,1) | (251,180,0) | (247,177,16) | (211,150,45) |
| y%27 | (254,247,212) | (252,246,210) | (254,246,204) | (250,59,41) |
| y%30 | (255,255,255) | (218,219,222) | (175,175,185) | (219,146,30) |

Kart %96,4'te bittiği hâlde altın, krem ve kırmızı sürüyor; ekranda kalp
rozeti kenarda kesik görünüyor.

**Elenen açıklamalar:**
- Önizleme artefaktı değil — `LayoutRebuilder.ForceRebuildLayoutImmediate`
  sonrası ölçüm birebir aynı (%4,4-%99,9).
- `RewardIcons`ın x sınırları kart içinde kalıyor (en sağdaki öğe 0,890 →
  ekranda %86,2).
- Ölçüm hizası kart içinde (kart y 431-937, ölçüm y 461).

Kart dışına taşan katman henüz saptanmadı (`inner`/`body`/gölge adayları).
Sonraki turda `Place` çağrıları hiyerarşi sırasıyla izlenecek. *Ölçüm sabitle
çelişince maskeyi sorguladım ve sabit haklı çıktı — fazlalık başka bir
öğeden geliyor.*

### Teklif kartı "taşması" — YANLIŞ ALARM, kök neden yakalama aracında

`GetWorldCorners` ile `Page0`ın tüm alt ağacı ölçüldü (piksel değil GEOMETRİ):

```
Card %3,6..96,4
  Inner/Body/Band/Art  %3,6..96,4
    Name %6,6..59,3      Buy %63,0..92,7
    Pile %9,2..46,3      Amount %18,4..55,6
    InfiniteHeart %72,7..86,2   Span %74,3..84,6
    Helper0 %52,4..67,3  Helper1 %64,1..79,0  Helper2 %78,1..93,0
    Count0 %59,4..69,6   Count1 %71,1..81,3   Count2 %85,1..95,3
    Badge %5,9..19,8     Rim %4,5..21,2
```

**Hiçbir öğe kartın %96,4 sınırını aşmıyor.** Kart da referansla birebir
(%3,6-%96,4 ↔ %3,8-%96,6). Yani kartta taşma YOK.

**Kök neden: yakalama aracının ölçekleme davranışı.** `UiCaptureTool` kanvası
`ScreenSpaceCamera`ya alıp bir `RenderTexture`a render ediyor, ama
`CanvasScaler` ekran boyutunu kullanmaya devam ediyor — hiyerarşi taramasında
kanvas **1080 birim** çıktı, istenen 886 değil. Dolayısıyla 886×1920
yakalamada YATAY oranlar güvenilir değil.

**Yöntem notu (bundan sonrası için):**
- **Y ölçümleri 886×1920 yakalamada GÜVENİLİR** — dikey ölçek doğru; ayarlar
  satır adımı %7,71 ↔ %7,71 birebir tuttu, yolculuk/mağaza düzeltmeleri de
  bu eksende doğrulandı.
- **X ölçümleri için piksel yerine GEOMETRİ kullan** (`GetWorldCorners`,
  kanvas köşelerine normalize). Piksel maskesi yatay ölçekleme artefaktından
  etkileniyor.

*Ders: bir ölçüm aracının kendisi de ölçülmelidir. Piksel taraması üç kez
üst üste "kart taşıyor" dedi; kesin geometri tek seferde "taşmıyor" dedi.
Aracın varsayımını sınamadan üç tur boyunca yanlış yerde kusur arandı.*

### Mağaza paket kartı — eksen düzeltmesi (9. tur)

`PackCreamH`/`PackBandH` "en/boy" ile hesaplanmıştı; en-boy iki farklı ekran
oranında aynı şeyi anlatmıyor (genişlik X'e, yükseklik Y'ye oranlı).

| | referans | önce | sonra |
|---|---|---|---|
| krem (yüksekliğe) | %13,02 | %15,68 | **%13,12** |
| mor bant | %6,77 | %5,78 | **%6,77** |

`PackCreamH 290 → 241`, `PackBandH 165 → 144`.

**Bant dört denemede oturdu ve süreç öğreticiydi:** tek orandan tahmin iki kez
şaştı (0,673 → 0,829), çünkü bandın ölçülen yüksekliği kartlar arası
boşluktan etkileniyor. İki nokta doğrusal varsayıldı (193→%8,33, 157→%7,45)
ve 129 tahmin edildi; o da şaştı (%5,99). Üç nokta ilişkinin doğrusal
olmadığını gösterdi, ara değer (144) tam tutturdu.
*Ders: iki nokta bir doğru verir ama doğrunun doğru olduğunu kanıtlamaz.*

### ⚠ YAKALAMA ARACI SAĞDAN KIRPIYOR (önceki bulgunun açıklaması)

Görselde bizim mağaza sağdan kesik: teklif kartının "$1.99" ve paket
kartının "$4.99" düğmeleri kırpılmış. Sebep artık net:

- `CanvasScaler` ekran boyutuna göre kanvası **1080×1920 birim** yapıyor
  (en-boy 0,5625).
- `UiCaptureTool` ise **886×1920** bir `RenderTexture`a render ediyor
  (en-boy 0,4615).
- Ortografik kamera dikeyi kapladığı için yatayda kanvasın yalnız
  886/1080 = **%82'si** görüntüye giriyor → sağ kenar KIRPILIYOR.

**Bu yüzden:**
- **Y ölçümleri 886 yakalamada geçerli** (dikey tam) — bu turdaki bütün
  dikey düzeltmeler bu eksende doğrulandı.
- **X ölçümü ve GÖRSEL karşılaştırma için 886 kullanılamaz.** X için
  `GetWorldCorners` geometrisi, görsel için 1080×1920 yakalama gerekir.

Bu, "teklif kartı taşıyor" yanlış alarmının da tam açıklaması: kart taşmıyor,
görüntü kırpılıyordu.

### Mağaza teklif kartı — eksen düzeltmesi (9. tur)

Kart sınırı sıcak-renk bloğu olarak ölçüldü (`m_001`, oranlar yüksekliğe):

| | referans | önce | sonra |
|---|---|---|---|
| teklif kartı | %20,62 | %16,77 | **%20,68** |

Beklentimin TERSİNE bizim kart **küçüktü**. Üç parça aynı oranla büyütüldü;
bant/toplam payı zaten doğruydu (referans 64/225 = 0,284, bizim 140/506 =
0,277):
`OfferArtH 340→418`, `OfferBandH 140→172`, `OfferLipH 26→32`.

**Kırpma bulgusu görselle de doğrulandı:** aynı sahne 1080×1920'de
yakalandığında teklif kartının "$1.99" düğmesi TAM görünüyor. Önceki
turlarda "kesik" görünen şey gerçekten yakalama kırpmasıydı; kartta kusur
yoktu.

Mağazanın durumu (hepsi doğru eksende, referansla birebir):

| ölçü | değer | referans ↔ bizim |
|---|---|---|
| `AwningH` | 268 | %13,96 ↔ %13,91 |
| `PillH` | 105 | %4,38 ↔ %4,38 |
| `PackCreamH` | 241 | %13,02 ↔ %13,12 |
| `PackBandH` | 144 | %6,77 ↔ %6,77 |
| teklif kartı | 622 | %20,62 ↔ %20,68 |

**Kalan (ölçülmedi):** `TileCreamH 320`, `DotsH 62`, `PillGap 54`,
`PackGap 64`.

## 10. TUR — KİMLİK AYRIŞTIRMA (portfolyo kararı)

Kullanıcı (2026-08-23): oyun ticari yayınlanmayacak ama **portfolyo ve
LinkedIn'de** paylaşılacak. Hedef **%85-95 benzerlik, %100 DEĞİL**. Ayrıca
"daha hızlı ilerleyelim" dendi — bu turdan itibaren sabit sabit ölçüp
doğrulamak yerine TOPLU geçiş yapılıyor.

### Yapılanlar

**1. Renk kimliği ayrıştırıldı.** Alt menü rengi birebir Block Out!'un
ölçülmüş `#5140E4`'üydü. Tüm mor ailesine tutarlı **+14° hue kayması**
(doygunluk ×1,06) uygulandı:
- `MenuShell`: 8 renk (çubuk, ayraç, dört üst şerit, seçili kart, gölge)
  → çubuk `#5140E4` → **`#7036E3`** (mavi-mor → mor)
- `MenuPage`: 12 renk (Body, HeaderTop/Low, Panel, PanelDeep, InkSoft,
  InkDark, TitleEdge, BodyProfile, Switch*)
- Yeşil/kırmızı/mavi **işlevsel** renklere DOKUNULMADI.

**2. İkonlar farklılaştırıldı** (`_Reference/art_backup/` altında yedek var):

| ikon | müdahale | orijinal↔bizim |
|---|---|---|
| home/shop/trophy/globe/album | pembe→mor **+ altın→bakır** | 0,977-0,984 |
| rocket | ayna + hue +0,30 | **0,648** |
| ufo | ayna + hue +0,22 | **0,741** |
| restart | ayna (dönüş yönü tersine) | **0,780** |
| clock | hue +0,28 | 0,890 |
| chest | ayna + gövde moru (altınlar korundu) | 0,859 |
| infinite/noads/ribbon/coin/heart | hafif hue kayması | — |

Sekme setinin kosinüs benzerliği yüksek kalıyor çünkü ölçüt siluete
duyarlı; **palet gözle açıkça farklı**: referans "altın + pembe", bizimki
"bakır + mor". Siluet (ev, kupa, küre) tür jeneriği sayıldı.

**3. ⚠ `logo_studio.png` KALDIRILDI — başka bir şirketin tescilli markasıydı.**
Denetim, dosyanın Grand Games'in "grand" wordmark'ı olduğunu buldu
(`_Reference/extracted2/Texture2D/grand_logo.png` ile harf harf aynı). Bu bir
benzerlik meselesi değil; portfolyoda duramaz. Yerine oyunun tuğla
motifinden türeyen **tarafsız bir stüdyo işareti** çizildi (iki blok,
bakır+mor). Stüdyo adı kullanıcıdan alınınca yazı eklenecek.

### Bölüm QA (1-50) — çalıştırıldı, gerçek hata bulundu ve düzeltildi

`LevelValidationTool.ValidateAll()`: **hata yok**, ama 27 bölümde tahmini
çözüm süresi verilen süreyi AŞIYORDU (en kötüsü `level_038`: ~351 sn için
180 sn) — bunlar ortalama bir oyuncu için bitirilemez.

**35 bölümün süresi** `tahmin × 1,25` (15 sn'ye yuvarlı) olacak şekilde
güncellendi. Örnekler: 038 → 450, 041 → 375, 022 → 360, 049 → 345,
026/034 → 315, 030/037 → 285.

`level_001`/`level_002`'deki "süre çok bol" uyarısı **kasıtlı** (doğrulayıcının
kendi notu: öğretici bölümlerde baskı olmamalı), dokunulmadı.

### AÇIK KALAN — kullanıcı kararı gerekiyor

- **Oyunun adı:** `logo_game.png` + `logo_b/l/o/c/k/o/u/t` harfleri
  "BLOCK OUT!" yazıyor. Harfler bizim çizimimiz (denetim doğruladı) ama
  kelime markası gerçek oyunun adı. Portfolyoda kalan en büyük marka kalemi.
- **Stüdyo adı:** tarafsız işaret kondu, ad bekleniyor.

### Kullanıcı kararları (10. tur)

- **Oyunun adı "BLOCK OUT!" KALIYOR.** Portfolyoda "klon çalışması" diye
  açıkça beyan edilecek. Renkli oyun logosuna DOKUNULMADI.
- **Stüdyo logosu kullanılmıyor.** Grand Games wordmark'ı kaldırıldı,
  yerinde tarafsız blok işareti duruyor.

### Kendi hatam: ilk ikon reçetesi fazla agresifti

`icon_clock` +0,28, `icon_rocket` +0,30, `icon_ufo` +0,22 tam hue döndürmesi
aldı; ekranda saat **magenta-yeşil** çıktı, roket kırmızıdan yeşile döndü.
Kullanıcı "çok ufak değişiklikler" demişti — bu ondan fazlasıydı ve kalite
kaybıydı. Üçü de yedekten alınıp **sekme setiyle aynı nazik reçeteye**
çekildi (pembe→mor, altın→bakır, asimetrik olanlarda ayna).

*Ders (farklılaştırma ≠ bozma): bir görseli tanınmaz yapmak kolay, tanınır
ama BAŞKA yapmak zor. Tek tek hue döndürmek yerine bütün sete uygulanan
tutarlı bir palet reçetesi hem daha az müdahale hem daha çok kimlik veriyor.*

**Not (önizleme):** `ContinueOffer.Show()` PopIn animasyonu kuruyor;
düzenleyici kipinde ilk kare `localScale = 0` olduğu için panel BOŞ çıkıyor.
Yakalamadan önce tüm `RectTransform`ların ölçeğini 1'e almak gerekiyor.

**Ölçülmedi/dokunulmadı:** `NewItemPanel` önizlemesi — `Begin`/`Item` private,
sahte `LevelModel` ve `PlayerPrefs` temizliği gerektiriyor. `LevelIntro` zaten
panel değil, tam ekran siyah perde.

### İkon reçetesi üç denemede oturdu (10. tur)

Kullanıcı geri bildirimi: *"kalp ve coin ikonu aynı kalsaydı, rengi kötü
olmuş"* ve *"ikonlarla menü renk uyumu bir garip olmuş, çok boğdu renk beni;
aynısını kullanmayalım dedim diye de kötü gözükmesin."* Haklıydı.

| deneme | reçete | sonuç |
|---|---|---|
| 1 | her ikona ayrı ayrı tam hue döndürme (+0,22…+0,30) | saat magenta-yeşil, roket yeşil — **bozuk** |
| 2 | pembe→mor **+ altın→bakır** | uyumlu değil: turuncu ikonlar mor çubukla **neredeyse zıt renk**, boğucu |
| 3 | pembe→**turkuaz**, altın KORUNUYOR | uyumlu ve ayrışık ✔ |

**Nihai reçete:** `hue ∈ [0,78–0,99]` (pembe/magenta) → **−0,42** (turkuaz),
doygunluk ×0,88. Altın hiç ellenmiyor. Asimetrik olanlarda ayna
(rocket, ufo, restart, chest). Sonuç: referans "altın + pembe", bizimki
**"altın + turkuaz"**.

**`icon_coin`, `icon_heart`, `icon_heart_broken` ORİJİNALİNE DÖNDÜ** —
kullanıcı isteği; jenerik görseller, dokunmaya değmez.

**Kabuk paleti sakinleştirildi:** ilk geçişte hue +14° / doygunluk ×1,06
uygulanmıştı ve ekranı boğuyordu. Orijinal değerlerden yeniden hesaplandı:
**hue +10°, doygunluk ×0,90**. Çubuk `#5140E4` → **`#7850E3`**.

*Ders (farklılaştırma bir PALET kararıdır, tek tek filtre değil): İkonları
teker teker döndürmek hem kaliteyi bozdu hem uyumu. Doğru çözüm, bütün sete
uygulanan tek bir renk çifti kuralı — "sıcak vurgu sabit, soğuk vurgu
değişir". Zıt renge kaçmak (bakır↔mor) ayrışma sağlar ama göz yorar;
komşu-tamamlayıcı bir çift (altın+turkuaz) hem ayrışır hem dinlendirir.*

### Renk denemesi GERİ ALINDI — ayrışma biçimden yapılıyor (10. tur)

Kullanıcı: *"renk aynı kalsın, bu renk çok bozuldu; ikonu değiştirebiliyorsak
biraz değiştirelim."* Doğru karar — üç renk denemesi de ya bozuk ya boğucuydu.

**Tamamen geri alındı:** `MenuShell` (8 renk), `MenuPage` (12 renk) ve bütün
ikonlar ORİJİNAL renklerine döndü. Referans paleti aynen kullanılıyor.

**Yerine: biçim farklılaştırması (renge hiç dokunmadan, kalite kaybı sıfır).**
Aynalama, asimetrik ikonlarda gerçek fark yaratıyor:

| ikon | orijinal↔bizim | durum |
|---|---|---|
| rocket | 0,641 | ayrışık |
| ufo | 0,761 | ayrışık |
| restart | 0,782 | ayrışık (dönüş yönü ters) |
| clock | 0,840 | ayrışık |
| chest | 0,858 | ayrışık |
| globe | 0,889 | ayrışık (kıtalar/ayak ters) |
| shop | 0,933 | az farklı |
| home | 0,990 | az farklı |
| **trophy** | **1,000** | **AYNI** |
| **album** | **1,000** | **AYNI** |

**Denendi ve GERİ ALINDI:** kupanın yıldızını ve albümün iç desenini tuğla
motifiyle değiştirmek. PIL ile üstüne şekil basmak ekranda leke gibi durdu —
kullanıcının "kötü de gözükmesin" kuralını çiğniyordu.

*Ders (filtre yeniden tasarım yapamaz): Hue kaydırma, ayna ve döndürme bir
görselin KİMLİĞİNİ değil yalnız sunumunu değiştirir. Simetrik bir ikonu
gerçekten farklılaştırmak için onu yeniden ÇİZMEK gerekiyor; üstüne şekil
basmak her seferinde amatör duruyor.*

**Açık kalan:** `icon_trophy` ve `icon_album` hâlâ birebir. Gerçekten
değiştirmek için yeniden çizim şart (proje zaten `MenuSprites` ile bir sürü
UI'yi prosedürel çiziyor, aynı yolla yapılabilir).

### Sekme ikonları YENİDEN ÇİZİLDİ (10. tur)

Kullanıcı: *"çizelim menüdeki ikonları, hepsini baştan çizelim ama çok benzer
olsun; zaten aynayla olacak iş değil, fakat aynı dilde olsun."*

**Referansın dili örneklenip kurallaştırıldı** (`_Reference/draw_icons.py`):

| kural | değer |
|---|---|
| kontur | koyu çivit `#202080`, siluetin tamamını sarıyor |
| altın | `#C87800` dip → `#F0A800` gövde → `#FFD242` ışık |
| pembe | `#B00058` dip → `#E82880` gövde → `#FF78B2` ışık |
| krem | `#F0C8A0` / `#FFF0D8` |
| form | her parça üç tonlu: dip dudak + gövde + üst ışık |

Beş ikon (`shop`, `trophy`, `home`, `globe`, `album`) bu kurallarla
**sıfırdan çizildi**; 5× süperörnekleme + LANCZOS indirgeme, çıktı orijinalin
**3 katı çözünürlükte** (eski 106-136 px → 318-408 px, ekranda daha net).

**İki iterasyon gerekti:**
1. İlk çizim düz kaldı — koni kâse, üçgen çatı, rastgele lekeler.
2. Düzeltme: kosinüs eğrili kâse (`cup()`), yuvarlatılmış çatı tepesi, elle
   tanımlı kıta poligonları, albüme kitap sırtı + üç fotoğraf kartı.

**Ayna** asimetrik olanlarda kaldı: `rocket`, `ufo`, `restart`, `clock`,
`chest` — bunlar zaten ayrışıktı (0,64-0,86), yeniden çizim gerekmedi.

*Ders (aynı dil ≠ aynı çizim): Bir görsel dilini taklit etmek, o dilin
KURALLARINI çıkarmakla olur — palet, kontur kalınlığı, tonlama sırası. Kurallar
yazıldıktan sonra yeni form çizmek mekanik bir iş; kural çıkarılmadan çizilen
ilk sürüm "aynı ailedenmiş" gibi durmuyor.*

### İkon kalitesi: piksel bozulması BULUNDU ve düzeltildi (10. tur)

Kullanıcı *"piksel bozulmaları var gibi, emin değilim"* dedi — **haklıydı.**

**Kök neden: `ImageFilter.MaxFilter`.** Konturu onunla büyütüyordum; kare
çekirdekli bir işlem olduğu için (a) köşeleri yuvarlak değil KÖŞELİ
büyütüyor, (b) hiç ara ton bırakmıyor. Sonuç: jilet gibi, mekanik bir kenar.

**Çözüm — `finish()` yeniden yazıldı:**
1. **Yuvarlak genişletme:** `GaussianBlur` → eşik → hafif `GaussianBlur`.
   Kare çekirdek yerine dairesel yayılım, üstelik kenar yumuşatma geri geliyor.
2. **İç gölge:** `alpha − blur(alpha)` kenar bandını veriyor; içeri doğru
   koyulaşan bu bant referansın derinlik hissinin kaynağı.
3. **Üst kenar ışığı:** alfayı aşağı kaydırıp farkını alarak siluetin üst
   kenarına ince parlaklık.

**Ölçüm (kenar yumuşaklığı, yüksek = yumuşak):**

| ikon | referans | bizim |
|---|---|---|
| trophy | 0,28 | 0,49 |
| globe | 0,27 | 0,50 |

Kaynakta referanstan yumuşağız ama ikonlarımız **3 kat çözünürlükte**, yani
ekranda bu keskinliğe dönüşüyor.

**Ayrıca: boş kenar kırpıldı.** Çizimlerin çevresinde saydam pay kalmıştı
(ör. `shop` yüksekliğin yalnız %87'sini dolduruyordu, referans %100).
Kırpılınca ikonlar yuvalarını referanstaki gibi dolduruyor.

**Kalan fark (dürüst):** referansın yüzeylerinde elle boyanmış doku var
(kiremit gölgeleri, yüzey lekeleri); bizde düz gradyan. Prosedürel çizim
bunu birebir vermiyor — ama portfolyo amacı için bu ayrım işimize yarıyor:
aynı dil, ayrı el.

### Prosedürel çizim bir üst seviyeye taşındı (10. tur, v5)

Kullanıcı: *"home'da içindeki çizgiler kötü gözüküyor"* ve *"prosedürel çizim
ile bir üst seviyeye taşımak mümkün değil mi?"* — ikisi de yapıldı.

**1. Tuğla derz çizgileri KALDIRILDI.** Düz çizgiler küçük boyutta kirli
görünüyordu. Yerine gerçek **ışık modeli** kondu.

**2. Motora dört yeni araç eklendi** (`_Reference/draw_icons.py`):

| araç | ne yapıyor |
|---|---|
| `mgrad` / `paint_m` | **çok duraklı** gradyan (2 renk yerine 4) |
| `contact_shadow` | bir formun ALTINDAKİ yüzeye düşürdüğü yumuşak gölge |
| `spec` | yumuşak spekülar parlama (formun sol üstü) |
| `inset` | oyuk hissi — üst kenardan içeri düşen gölge |

Gölge ve parlama katmanları **alfayla çarpılıyor**, yoksa siluetin dışına
taşıyorlar. Derinlik hissini veren şey bu: çatı duvara gölge düşürüyor,
pencere camı oyuk duruyor, yüzeylerde ışık kayıyor.

**3. Ev'in detay yoğunluğu artırıldı** (5 iterasyon): iki ana pencere
(çapraz bölmeli), alınlık penceresi, kule penceresi, çerçeveli kapı +
basamak, çatı sırtında süs topları, iki tepe finiali.

**4. Aynı ışık modeli diğer dördüne de uygulandı** — mağazada tentenin
gövdeye gölgesi, kupada kâse ve taban parlaması, kürede cam parlaması +
kıtaların temas gölgesi, albümde kapak parlaması.

**5. Boş kenarlar kırpıldı** — ikonlar yuvalarını referanstaki gibi
dolduruyor.

*Ders (prosedürel çizimin tavanı sanılandan yüksek): "PIL ile çizim düz
kalır" doğru değil — düz kalmasının sebebi ışık modelinin olmamasıydı.
Gradyan + temas gölgesi + spekülar + oyuk gölgesi eklenince aynı kod
boyanmış görünüyor. Eksik olan araç sayısıydı, ortam değil.*

### Ev ikonu simetrik hâle getirildi (10. tur, v6)

Kullanıcı: *"sağ tarafta kule gibi yeri var ya, aynısından solda da olsun."*

Kompozisyon yeniden kuruldu: **iki kule kanatlarda, ana gövde ortada.**
Kule çizimi `tower(cx)` diye tek bir yardımcıya alındı — gövde, çatı, tepe
topu, spekülar ve temas gölgesi birlikte; iki kez çağrılıyor (0,155 ve
0,845). Böylece ikisi birebir simetrik ve ileride konum değiştirmek tek
sayıyla oluyor.

Yerleşim: kuleler önce çizilip ana gövde önlerine geliyor; kule gövdeleri
duvarla kaynaşıyor, çatıları ayrı kütle olarak okunuyor. Kapı, alınlık
penceresi ve çatı süsleri merkeze hizalandı; pencere sayısı dörde çıktı
(her kulede bir, ana gövdede iki).

### İkonlar bir seviye daha (10. tur, v7) — radyal gölgelendirme + hotspot

Kullanıcı: *"son olarak ikonları bir tık seviye daha yükseltebilme şansımız
varsa yapalım."* Vardı; iki araç daha eklendi:

| araç | ne yapıyor |
|---|---|
| `rgrad` / `paint_r` | **radyal** gradyan — küre/kubbe formları için |
| `hotspot` | küçük, keskin parlak nokta (cam/metal hissi) |

**Küre yeniden yazıldı.** En zayıf halka oydu: bir küreyi dikey gradyan
satamaz, çünkü ışık bir NOKTADAN gelir, yukarıdan aşağıya bir şerit olarak
değil. Artık sol üstten radyal gölgelendirme, meridyen halkasında metal
gradyanı, iki cam parlaması ve altta sıcak bounce ışığı var.

**Hotspot** kupanın kâsesine ve tabanına, mağaza tentesine, albüm kapağına
ve ev çatısına kondu — küçük ama "boyanmış" hissini veren detay.

*Ders (her forma kendi ışığı): Dikey gradyan düz yüzeyler için doğru, küresel
formlar için yanlış. Tek bir gradyan tipiyle bütün ikonları boyamak, kürenin
neden hep "düz" göründüğünü açıklıyordu.*

### Mağazanın kalan Y ölçüleri (10. tur)

Jeton ızgarası `m_004`'ten ölçüldü. Çapa olarak **yeşil fiyat düğmeleri**
kullanıldı (iki satır, güvenilir); krem alan kutucuğun sol kenarına yakın
sütundan (jeton görselinin uzanmadığı yer) alındı.

| ölçü | eski | yeni | gerekçe |
|---|---|---|---|
| `TileCreamH` | 320 | **236** | referans 118 px |
| `TileBaseH` | 176 | **122** | referans 61 px |
| `TileGap` | 50 | **36** | referans 18 px |
| `DotsH` | 62 | **70** | nokta çapı 14 px = 28 birim; satır 28/0,40 |

Kutucuk adımı: referans **%20,52** ↔ bizim **%20,52** (birebir). Eskisi
320+176+50 = 546 birim, yani %39 fazla uzundu.

**`TileBaseH` yine ilk değerine döndü.** "132 → 176" büyütmesi de yanlış
eksene dayanıyordu; referans 122 diyor. Kullanıcının vaktiyle "kırmızı kısım
küçük kalmış" bulgusu gerçekti ama sebebi taban değil, KREM ALANIN fazla
uzun olmasıydı — oran zaten doğruydu (referans 1,93 / bizde 1,82).

### NewItemPanel önizlenebilir hâle getirildi (10. tur)

Panel düzenleyicide yakalanamıyordu: tek girişi `TryShow` ve üç yan etkisi
vardı — `GameSession` duraklatma, `UiKit.CreateCanvas` (`DontDestroyOnLoad`
düzenleyicide patlar) ve `PlayerPrefs` (bir kez önizlemek mekaniği "görüldü"
işaretliyor, panel bir daha açılmıyor).

`Begin` ikiye ayrıldı:
- **`BuildContent(root, item, animate)`** — yalnız görsel kurulum
- **`CreatePreview(parent, variant)`** — yan etkisiz, `public static`

`animate: false` giriş animasyonunu atlıyor; `PlayEntrance` ölçekleri 0'dan
başlattığı için düzenleyicide ilk kare boş çıkıyordu. **Yedi varyantın
(Layered, ColorQueue, IceBlock, IceGate, Curtain, Directional, Generator)
hepsi artık yakalanabiliyor.**

**Yerleşim düzeltmesi:** içerik 0,27-0,91 arasındaydı, optik merkezi 0,59 —
panel tepeye yapışıktı ve alt %27 boştu. Blok 0,07 aşağı alındı; kapat
düğmesi köşede bırakıldı.

*Ders (önizleme, yan etkisi olmayan bir yol ister): Bu panel dokuz tur
boyunca "zor" diye atlanmıştı. Sorun karmaşıklık değil, tek girişin oyun
durumunu değiştirmesiydi. Kurulumu yan etkiden ayırmak hem önizlemeyi
mümkün kıldı hem `Begin`i okunur yaptı.*

---

## Ana ekran üst şeridi — "kalp üst üste bindi" (11. tur)

Kullanıcı bulgusu: bilerek çok jeton alındığında (33 340) kalp, jeton
kapsülünün artı düğmesine biniyor gibi görünüyor.

### Teşhis — üç yanlış hipotez, sonra ölçüm

1. *"Yazı kutusundan taşıp kalbin altına giriyor"* — **yanlış.** `Value_Coin`
   `CreateTitle` ile kuruluyor, o da `CreateLabel`'i çağırıyor ve `UiTextFit`
   HER etikete orada ekleniyor. Sığdırma çalışıyordu: punto 40 → 37,2,
   çizilen 126 birim / kutu 124.
2. *"`Status_Heart` `CreateLabel` ile kurulduğu için sığdırma yok"* —
   **yanlış**, aynı sebeple. (`CreateLabel`'de eklendiğini okuyana kadar
   `CreateTitle`'a özel sandım.)
3. *"Dar telefonda kutu daralıyor ama punto sabit"* — **yanlış.** Bütün
   kutular genişliğin YÜZDESİ, sığdırma da kutuya göre ölçüyor; oran sabit
   kaldığı için hiçbir genişlikte kutular çakışmıyor.

Asıl sebep, `GetWorldCorners` dökümünü **boşluklarla birlikte** yazdırınca
göründü:

```
%  5,8..% 20,1  Avatar
% 22,0..% 30,8  Icon_Coin      <- boşluk %1,9
% 45,7..% 51,1  Plus_Coin
% 52,9..% 62,8  Icon_Heart     <- boşluk %1,8 = 19 birim
% 87,2..% 96,2  Gear           <- boşluk %4,5  (BOŞ DURUYOR)
```

Kalbin nabzı (`Tween.Scale ... 1.05f`) tepe noktasında kutuyu %5 büyütüyor:
sol kenar %52,65'e iniyor, boşluk **17 birime** düşüyor. Kalp PNG'sinin
yumuşak gölgesiyle birlikte düğmeye yapışık okunuyor. Yani çakışma
geometrik değil, **optik** — ve sağda dişliye kadar %4,5 boşluk boştaydı.

### Düzeltme

| Öge | Eski | Yeni |
|---|---|---|
| `Value_Coin` | 0,330–0,445 (124 birim) | **0,312–0,452** (151 birim) |
| `Icon_Heart` | 0,529–0,628 | **0,544–0,643** |
| `Track_Lives` | 0,574–0,825 | **0,589–0,840** |
| `Status_Heart` | 0,640–0,762 | **0,655–0,777** |
| `Plus_Heart` | 0,772–0,827 | **0,787–0,842** |

Can grubunun tamamı %1,5 sağa, boşta duran dişli payına kaydırıldı. Jeton
kutusu da simgeyle arasındaki ölü alana genişletildi.

### Doğrulama (33 340 jeton + sınırsız can, nabız tepesi dâhil)

```
% 31,9..% 44,5  Value_Coin (punto 40,0 — TAM PUNTO, cizilen 136/kutu 151)
% 54,2..% 64,5  Icon_Heart (nabiz tepesi %105)  <- bosluk 33 birim (%3,1)
% 87,2..% 96,2  Gear                            <- bosluk 32 birim (%3,0)
```

Çarpışma noktasındaki boşluk **17 → 33 birim**; jeton sayısı artık altı
hanede küçülmüyor (kutu yedi haneye kadar tam puntoda yetiyor). Konsol
temiz (0 hata). Karşılaştırma: `_Reference/notes/topbar_before_after.png`.

*Ders (yüzdesel düzende çakışma aramak yanlış soru): Bütün kutular
genişliğin yüzdesiyse iki kutu HİÇBİR ekranda çakışmaz — o yüzden "hangi
çözünürlükte taşıyor" diye aramak üç tur boşa gitti. Kullanıcının gördüğü
şey çakışma değil YETERSİZ BOŞLUKTU. Doğru araç, kutuların yerini değil
ARALARINDAKİ boşlukları sıralayan bir döküm: %4,5'lik ölü alan ile
%1,8'lik sıkışma yan yana yazılınca çözüm kendini gösterdi. Bir de
animasyonu ölçüme kat — nabız atan bir öge duran hâliyle ölçülürse %5
yalan söyler.*

---

## Sınırsız can: iki kalp üst üste (11. tur)

Kullanıcı bulgusu: mağazadan süreli sınırsız can paketi alındığında "normal
kalp ikonunun üstünde sınırsız kalp ikonu kaldı, üst üste bindiler".

### Sebep

`icon_infinite.png` (248×224) yalnız ∞ işareti DEĞİL — **içinde ∞ olan tam
bir kalp**. Önceki kod onu `Icon_Heart`'ın çocuğu olarak ayrı bir `Image`
nesnesiyle ÜSTÜNE koyuyordu:

```csharp
_livesInfinity = UiKit.CreateIcon("Infinite", _livesLabel.transform.parent, …);
badge.offsetMin = number.offsetMin;   // SAYININ kutusu: %4 alt, %8 üst girinti
```

Kutusunu sayının kutusundan kopyaladığı için üstteki kalp alttakinden küçük
kalıyordu ve sade kalbin kenarları çepeçevre dışarı taşıyordu — iki kalp iç
içe görünüyordu.

### Düzeltme

İkinci nesne komple kaldırıldı; tek kalbin **sprite'ı** değişiyor:

```csharp
_heartIcon = heart;  _heartNormal = heart.sprite;
_heartInfinite = UiSkin.Get(Art.Infinite) ?? MenuSprites.Infinity;
…
_heartIcon.sprite = infinite ? _heartInfinite : _heartNormal;
```

İki sprite'ın en-boy oranı zaten yakın (1,057 / 1,107), ikisi de aynı kutuya
geriliyor. Nabız (`Tween.Scale`), dokunma alanı ve ölçüler tek nesnede
kaldığı için hiçbiri ayrıca güncellenmedi.

**Can baloncuğunda aynı tutarsızlık** vardı: altta "Unlimited for 11h 59m"
yazarken kalbin üstünde `lives.Current` (çoğu zaman "0") duruyordu. Aynı
kural uygulandı — sprite değişiyor, sayı gizleniyor.

### Doğrulama

```
NORMAL  : kalp sprite 'icon_heart',    kalp altindaki Image sayisi 0
SINIRSIZ: kalp sprite 'icon_infinite', kalp altindaki Image sayisi 0
```

Konsol temiz (0 hata). Görsel: `_Reference/notes/heart_states.png`.

*Ders (iki DURUM aynı yeri kaplıyorsa ikinci nesne değil, tek nesnenin
sprite'ı): Üst üste koymak "gizle/göster" ile çözülüyormuş gibi durur ama
iki nesnenin kutusu, oranı ve animasyonu ayrı ayrı doğru tutulmak zorunda —
burada kutu yanlış kaynaktan kopyalanmıştı ve hata tam olarak oradan çıktı.
Bir de varsayımı doğrula: "infinite" adlı bir sprite'ın yalnız ∞ işareti
olduğunu sandım; açıp bakınca tam bir kalp çıktı.*

---

## Sonuç panelleri — "Rewards x3" rozeti siyahtı (11. tur)

### Önce: ölçüm aracının kendisi yalan söyledi

Panelleri yakalarken şu satır vardı (önceki turlardan, `ContinueOffer`
önizlemesi için eklenmişti — giriş animasyonu ölçekleri 0'da bırakıyordu):

```csharp
foreach (var rt in canvas.GetComponentsInChildren<RectTransform>(true))
    rt.localScale = Vector3.one;      // HEPSİNİ birden
```

Bu satır başlığın **bilerek** konmuş `TitleCondense = 0.70` yatay
sıkıştırmasını yok ediyor. Üstelik `foreach (…UiTextFit…) f.FitNow()` da
`Release()` edilmiş bileşende çalışıyor (`FitNow` doğrudan `LateUpdate`
çağırıyor, `enabled`e bakmıyor) ve `NoFit` işaretli başlığın puntosunu
250'den 133,7'ye indiriyordu.

Sonuç: iki sahte "hata" raporladım — başlık kapatma düğmesinin altında ve
kart kenarından taşıyor. İkisi de aracın ürettiği yapaylıktı. Düzeltilmiş
ölçüm (yalnız SIFIR ölçekler onarılıyor):

```
'PERFECT!' punto 250 -> % 21,4..% 78,6  tamam   (kapatma % 86,7'de)
'FAILED'   punto 160 -> % 36,4..% 63,6  tamam
```

*Ders (ölçüm aracı da bir kod parçasıdır ve o da bozulur): "Önizlemede
ölçekleri sıfırla" kuralı bir panelde doğruydu, hepsinde değil. Toptan
uygulanan bir onarım, bilerek konmuş her ayarı da siler. Bir düzeltme
eklerken "hangi durumda gerekliydi" diye daraltmak gerekiyor —
`localScale.x < 0.02f` koşulu tam olarak bunu yapıyor.*

### Asıl hata: tint bir boya değil, ÇARPAN

Kaybetme panelindeki "Rewards x3" rozeti turuncu olmalıydı; kodda da öyle
yazıyordu. Render'dan ölçüldü:

```
RewardsTag ortanca (49, 26, 6)      <- neredeyse siyah kahve
TagOrange  hedef   (246, 157, 33)
```

Sebep: rozet `Art.PanelDark` sprite'ıyla kuruluyor ve `TagOrange` ona tint
olarak veriliyordu. uGUI tint'i sprite'la **çarpar**; koyu sprite × turuncu
= koyu kahve. Yorumun "koyu rozet yeşil düğmenin üstünde ikinci bir gölge
gibi duruyordu" diye kaldırmak istediği şey ekranda hâlâ duruyordu.

**Düzeltme:** kartın kendisi gibi prosedürel kuruldu — `CreateRoundedPanel`
beyaz bir maske olduğu için tint birebir işliyor, `0.5f` köşe oranı da tam
hap veriyor.

```
RewardsTag ortanca (246, 157, 34)   <- hedefle aynı
```

Konsol temiz (0 hata). Görsel: `_Reference/notes/tag_after.png`.

*Ders (sabit tanımlamak, ekranda görmek değildir — ikinci kez): 8. turda
kartın koyu kenarı `RefreshResult` tarafından eziliyordu, bu turda rozetin
turuncusu sprite tarafından koyultuluyor. İkisinde de kaynak kod doğruydu.
Rengi HER ZAMAN render'dan piksel örnekleyerek doğrula.*

### Kırık kalp hiçbir şeye değmiyordu

`BuildFailExtras` yorumu "kırık kalp: başlıkla kartın kesiştiği yere biner"
diyordu; kod ise kalbi tamamen kartın DIŞINA koyuyordu.

```
FailHeart  y % 70,0..% 75,2      (görünür alt kenar % 70,8)
Card       y % 25,4..% 70,0
Title      y % 75,6..% 83,6
```

Kalbin kutusu kartın üstünde bitiyor ama sprite'ın kendi saydam payıyla
(alt %2,3) görünür kenar %70,8'de kalıyor: karla arası 13 birim, başlıkla
arası 38 birim. Yani kalp karanlık perdede asılıydı — ne başlığın ne kartın
parçası.

**Düzeltme:** %1,5 aşağı (`0.700..0.752` → `0.685..0.737`). Alt üçte biri
kartın üst kenarına biniyor; kalp `_resultPanel`in SONRA kurulan kardeşi
olduğu için kartın üstünde çiziliyor. Kapatma düğmesi kartın köşesine nasıl
biniyorsa kalp de üst kenarına öyle biniyor — aynı dil. Kartın içindeki
zorluk etiketi %67,8'de başlıyor, kalbin yeni alt kenarı %68,5: 13 birim pay.

Görsel: `_Reference/notes/heart_ba.png`, `_Reference/notes/result_final.png`.

**DOKUNULMADI — kaynağı yok:** Kazanma ve kaybetme kartları farklı ölçüde
(%93,5×59,4'e karşı %90,2×44,6) ve "FAILED" 160 punto ile ekranın yalnız
%27'sini kaplarken "PERFECT!" 250 punto ile %57'sini kaplıyor. Kod bunların
`Game over .mp4` 18. saniyeden ölçüldüğünü söylüyor ama o video artık
depoda yok (`_Reference` altında hiç .mp4 kalmamış) ve 52 karelik arşivde
de kaybetme ekranı bulunmuyor — yalnız kazanma var (kare 041). Ölçülmüş bir
kararı, kaynağını doğrulayamadan bozmak yanlış olur; kaybetme ekranının
referansı bulunursa buradan devam edilmeli.

---

## Duraklat paneli (11. tur)

### Değiştirilmedi: koyu ayar simgeleri DOĞRU

İlk bakışta hoparlör/nota/titreşim simgeleri mor zeminde "kaybolmuş" gibi
duruyor. Ölçüldü:

```
kart yuzu     (64, 50, 192)
Icon_0 koyu   (28, 22, 73)     -> kontrast ~1,9:1
Label beyaz   (255,250,240)    -> kontrast ~3,5:1
```

Kontrast gerçekten düşük — ama **böyle olması isteniyor**. `PauseGlyph`
(S64) yorumunda duruyor: kullanıcının kendi isteği ("Ses / Müzik / Haptik
ikonları koyu mor olacak, bizde beyaz") ve referanstan ölçülen değer
(26,19,74); bizimki (28,22,73), yani neredeyse birebir. Gerekçe de yazılı:
beyaz zaten yazının rengi, simgeyi de beyaz yapınca satır tek bir uzun
kelime gibi okunuyor.

*Ders (düşük kontrast HER ZAMAN hata değildir): Erişilebilirlik sezgisi
"her şeyi zemine göre parlat" der; hiyerarşi ise bazı ögelerin geri
çekilmesini ister. Bir değeri "yanlış" ilan etmeden önce onu koyan yorumu
oku — burada değiştirseydim kullanıcının açıkça istediği şeyi geri almış
olacaktım.*

### Düzeltildi: "Musics:" → "Music:"

Aynı ayar iki ekranda iki farklı adla duruyordu:

```
SettingsScreen.cs:222      "Music"      <- doğru
GameplayScreen.cs:1549     "Musics:"    <- yanlış
```

"music" İngilizce'de sayılamayan bir isim, çoğulu yok. Muhtemelen Türkçe
"Müzikler"den birebir çevrilmiş; yanındaki "Sounds" çoğul doğru olduğu için
bu da çoğullanmış. Doğrulandı:

```
Label_0: 'Sounds:'   Label_1: 'Music:'   Label_2: 'Haptics:'
```

*Ders (aynı kontrolün adı da tek olmalı): Düzen ve renk için "iki ekranda
aynı görünsün" kuralını uyguluyoruz ama METİN için uygulamamışız. Kopya da
arayüzün parçası.*

---

## Profil istatistik ikonları: 5 → 8 (11. tur)

Referansın profil ekranında (`_Reference/frames/m_051.jpg`) sekiz kutucuğun
sekizinde de farklı bir simge var. Bizde beş simge sekiz kutuya
dağıtılmıştı: kupa×2, roket×2, UFO×2, küre, yıldız.

### 9. turun kararı neden değişti

O tur tekrarları **bilerek** korumuştu ve gerekçesi sağlamdı:

> `badge_reward` BURAYA UYMUYOR … Referanstaki amblem elimizde yok; küre
> "lig" fikrine en yakın olan. … `Art.Check` denendi ve geri alındı: diğer
> yedi ikon parlak, hacimli oyun ikonu, `check_green` düz bir vektör tik.

Yani karar doğruydu ama bir **varsayıma** dayanıyordu: "elimizdeki simgeler
bunlar". Bu turda o kısıt kaldırıldı — dört simge `_Reference/
draw_stat_icons.py` ile çizildi. Motor `draw_icons.py`den geliyor, yani
palet ve bitiriş (kalın lacivert kontur, çok duraklı gradyan, tepe ışığı,
temas gölgesi) menü simgeleriyle **aynı**.

| Kutucuk | Eski | Yeni |
|---|---|---|
| First Try Wins | kupa ⟲ | **icon_medal** (kurdeleli "1") |
| Sky Jump Wins | mavi UFO ⟲ | **icon_saucer** (pembe tabak) |
| Streak Race Wins | roket ⟲ | **icon_flag** (damalı bayrak) |
| Block League Wins | küre | **icon_league** (ışınlı rozet + taş) |

Anlamı adından gelen dört tekrar korundu: roket → Rocket Race, UFO → Max
Ufo Climb, kupa → Weekly Cup, yıldız → Star Blast.

```
Stat_0 'icon_medal'   Stat_4 'icon_star'
Stat_1 'icon_saucer'  Stat_5 'icon_trophy'
Stat_2 'icon_flag'    Stat_6 'icon_rocket'
Stat_3 'icon_league'  Stat_7 'icon_ufo'
-> 8 kutucuk, 8 FARKLI ikon        UiSkin: 82 -> 86 sprite
```

### Çizimde üç deneme

İlk turda madalya tuttu, diğer üçü tutmadı ve sebepleri öğretici:

- **Bayrak** direksizdi, iki ucu topuzlu bir "kemik" siluetiydi ve lapa gibi
  çıktı. *Bir bayrağı bayrak yapan şey kumaş değil, kumaşın BİR YERE bağlı
  olması* — altın direk eklenince siluet okundu. Dama da sekiz sütundan
  dörde indi; simge boyutunda sekiz sütun gri bir dokuya dönüşüyordu.
- **Lig amblemi** altı YUVARLAK yapraklıydı ve papatyaya benziyordu. Rozet
  hissi yuvarlaklıktan değil, uzun-kısa değişen SİVRİ uçlardan geliyor.
- **Tabak** koyu bordo bir yastığın üstünde krem bir kubbeydi. İki hata:
  gövde `PINK_D`nin de altına inen bir tona kayıyordu (ekranda kahve) ve
  kubbe ALTINDI, gövdeyle yarışıyordu. Tek renk ailesi + üç kademe + altın
  kenar bandı çözdü.

*Ders (bir kararı yeniden aç, ama SEBEBİ değişince): Bu tabloyu "referansta
8 farklı var" diye değiştirmek 9. turda yanlıştı, bugün de yanlış olurdu.
Değişimi haklı kılan şey referans değil, KISITIN KALKMIŞ olması. Eski yorum
silinmedi — hangi gerekçenin ne zaman düştüğü, kararın kendisinden değerli.*

## Kutucuk iç yerleşimi: bekleyen not ESKİMİŞ

"Profil kutucuklarında değer alt kenara sıkışmış" notu listede duruyordu.
Kutu ölçüleri bunu doğrular gibiydi (üst 26, ara 3, alt 13 birim) ama
**çizilen harfler** ölçülünce tablo değişti:

```
Face 160 birim (tabandan)
Label  CIZILEN 101..124   -> ust bosluk 36
Value  CIZILEN  25.. 79   -> ara 22, alt bosluk 25
```

Değer dibe yapışmamış. Kod da bunu söylüyor: "DEĞER DİPTEN KALDIRILDI VE
KUTUYA SIĞDIRILDI (9. tur)". Not, o düzeltmeden önce yazılmış. Dokunulmadı.

*Ders (kutu ≠ yazı, üçüncü kez): TMP'nin kutusu yazının etrafında bol
duruyor; dikey dengeyi kutulardan okumak bu projede üçüncü kez yanlış
sonuç verdi. `textInfo.characterInfo[i].topLeft/bottomLeft` gerçeği
söylüyor.*

## Genel tarama: taşan yazı yok

On bir ekran kurulup her etiketin ÇİZİLEN genişliği kutusuyla
karşılaştırıldı (ölçek dâhil, %4 kontur payı):

```
Home, Sonuc-Kazan, Sonuc-Kaybet, Duraklat, Profil, GunlukOdul,
Magaza, Ayarlar, Yolculuk, Siralama, Koleksiyon
--- 284 etiket tarandi, 0 tanesi tasiyor ---
```

Hiçbir ekran kurulumda hata da vermedi. Konsol temiz (0 hata).
Görseller: `_Reference/notes/stat_icons2.png`, `profile_after.png`,
`daily_now.png`.

---

## İkonlar 2. deneme: BİÇİM referanstan (11. tur)

Kullanıcı: *"ikonlar çok içime sinmedi, oyunun ana dili ile uyuşması lazım;
aşırı benzerini veya aynısını da alabilirsin, ufak detayları alırsak çok
ufak değiştirirsek problem olmaz."*

İlk denemede simgeler "aynı dilde ama serbest yorum" diye çizilmişti ve
**nesnenin kendisi yanlıştı**. Referans karesi (`m_051.jpg`) 6× büyütülünce
görüldü:

| Simge | İlk denemede çizdiğim | Referansta gerçekte |
|---|---|---|
| madalya | küresel, radyal gradyanlı | önden görünen **düz jeton** |
| pembe | uçan daire (kubbe + ışıklar) | **şişme iniş halkası** |
| bayrak | direkli, dalgalı bayrak | **direksiz flama**, üst köşelerinde kemik topuz |
| amblem | tek ışınlı rozet | **iç içe halkalar** + yanlarda alev kanat |

Renkler de tahminle konmuştu; referanstan ölçülünce ikisi kaydı:

```
pembe  benim (228, 34,122) PINK   ->  referans (200, 24,200)  MAG
teal   benim ( 22,176,190)        ->  referans ( 34,158,132)  TEAL2
altın        (216,144, 24)   bayrak (216,216,216)/(24,24,24)
```

Üç rötuş turu gerekti ve üçü de aynı cinsten hataydı:

- **Bayrak** ilk sürümde "kurtarmak" için direk eklenmişti — yanlış teşhis.
  Sorun topuzlar değil, topuzların kumaşla aynı hamurdan görünmesiydi.
  Ayrıca 120 yoğunluklu gölge bandı beyaz kareleri GRİLEŞTİRİYORDU; damalı
  bir yüzeyde gölge, deseni okunmaz yapan ilk şey. Bant kaldırıldı.
- **Halka** deliği 0,40'tayken simge simite dönüşüyordu → 0,27; mercan
  çentik magentanın üstünden başlayınca dile benziyordu → altına alındı.
- **Amblem** kanatları DÜZ dörtgendi, ok gibi duruyordu. Referansta kanat
  halkayı SARIYOR; iki YAY arasında örülen yaprağa çevrildi, sayısı da
  3'ten 2'ye indi (küçük yapraklar papatya etkisi yapıyordu).

Karşılaştırma: `_Reference/notes/icons_vs_ref3.png` (üst referans, alt bizim).

*Ders (üslup taklidi, BİÇİM taklidi değildir): "Aynı görsel dilde çiz"
talimatını palet + kontur + gradyan diye okudum ve nesneyi serbest
yorumladım. Oysa bir ızgarada simgeyi tanıdık yapan şey işçilik değil
SİLUET — uçan daire ile şişme halka aynı paletle çizilse de aynı şeyi
anlatmıyor. Referans varken siluet ölçülür, uydurulmaz.*

### preserveAspect eklendi (görünür etkisi olmadı ama kalmalı)

`UiKit.CreateIcon` preserveAspect kurmuyor, yani sprite kutusuna geriliyor.
Sekiz simge de kabaca kare olduğu sürece görünmüyordu; portre madalya
(318×378) ve yatay flama (372×324) gelince risk gerçek oldu. `ProfileScreen`
kutucuk simgelerinde `preserveAspect = true`.

Ölçüm: kutu hepsinde 154×109, ayar canlı (`preserveAspect=True` sekizinde
de). Render değişmedi — sprite'lar zaten kutuya yakın oturuyor — ama ayar
bir sonraki farklı oranlı varlıkta bozulmayı engelliyor.

*Ders (bayat derleme, dördüncü kez): Düzenledim → refresh → yakaladım ve
"değişmedi" sandım; ayrı bir komutta bileşeni sorgulayınca ayarın canlı
olduğu görüldü. Ölçmeden önce DEĞERİ sorgula, render'a güvenme.*

*Ders (kesik sanılan şey kesik değildi): İlk bakışta simgelerin kutucuk üst
kenarında "kesildiğini" yazdım. Mevcut kupa ve yıldızla yan yana konunca
hepsinin aynı hizada bittiği görüldü — kutu orada bitiyor, kırpılma yok.
Yeni bir varlık geldiğinde göz, farkı hep YENİ olanın kusuru sanıyor.*

---

## Kabartmalı başlık — üç deneme, üç ders (12. tur, G1+G4)

Kullanıcı ilk sonucu beğenmedi: *"o bahsettiğim kabartma yazısı kesinlikle
istediğim gibi olmadı, orjinali ile kıyasladığın zaman bizimki çok zayıf...
gerekirse shader da yazabilirsin."*

### Referansın gerçek yapısı

`m_051.jpg`, "Profil", x=27 dikey kesiti — harften DIŞA doğru:

```
beyaz harf
KOYU halka   ~3px   (47,23,161)
PARLAK hale  ~5px   tepe (98,71,228)     <- kapak yüksekliğinin ~%20'si
bant                (66,39,196)
ALTTA ayrıca koyu gölge 2-3px (30,14,102) — aşağı kaydırılmış
```

Yani KOYU içeride, PARLAK dışarıda, ve ayrıca aşağı düşen bir gölge var.
Üç katman. Bizde iki vardı ve ikisi de inceydi.

### Üç yanlış deneme

1. **Sırayı ters kurdum** — koyu katmanı en dışa, en çok şişirilmiş hâlde
   koydum. Ekranda harfleri yutan koyu bir blok çıktı. Ölçüm sırayı
   söylüyordu, ben okumadan kurdum.

2. **Konturu kalınlaştırarak çözmeye çalıştım** (`InnerWidth = 0.20`).
   Harflerin yüzü tamamen koyulaştı.
   *Ders: TMP'de `_OutlineWidth` harfi BÜYÜTMÜYOR, kenarından İÇERİ
   doğru yiyor. Kalınlık konturdan değil, arkadaki şişirilmiş
   katmanlardan gelmeli.*

3. **Sıralama kodu idempotent değildi.** Her karede
   `SetSiblingIndex(front.GetSiblingIndex())` çağırıyordum; bir öğeyi kendi
   indeksine taşımak komşuları kaydırdığı için sıra her karede bir adım
   dönüyor ve öndeki beyaz yazı bir kare sonra en alta düşüyordu.
   *Ders: her karede çalışan kod idempotent olmak zorunda. "Doğru sırayı
   kur" ile "sıra doğru değilse düzelt" aynı şey değil.*

### Asıl duvar: font atlasının dolgu payı

Değerleri artırmak bir yerden sonra hiçbir şey değiştirmiyordu. Sebep:

```
Baloo2 SDF   padding 9   atlas 1024x1024   SDFAA
```

TMP'de kontur ve şişirme bu 9 pikseli PAYLAŞIYOR. Referansın istediği
kalınlık (kapak yüksekliğinin ~%20'si, bizim 72 puntoda ~10 birim) o paya
sığmıyor ve sessizce kırpılıyordu.

**Çözüm:** hale artık şişirmeyle değil **kaydırılmış kopyalarla** çiziliyor.
Aynı yazı, hale renginde, çember üzerinde 8 yöne `HaloThickness` kadar
kaydırılıp çiziliyor; birleşimleri harfin etrafında tam olarak o kalınlıkta
bir bant bırakıyor. Yazı tipini yeniden üretmek gerekmedi.

*Ders (bir sınıra dayandığında, sınırı değil YÖNTEMİ değiştir): "Değeri
artır" üç tur boyunca işe yaramadı çünkü sorun değerde değil, değerin
sığdığı bütçedeydi. Font varlığını yeniden üretmek de bir seçenekti ama
paylaşılan bir varlığı değiştirmek yerine, o bütçeye hiç dokunmayan bir
çizim yolu seçildi.*

### Doğrulama

```
bant   bizim (65,37,191)   referans (66,39,196)
hale   bizim (95,67,222)   referans (98,71,228)
kardes sirasi: Title_Halo0..7 -> Title_Shadow -> Title   (kararli)
```

Uygulandığı yerler: `MenuPage.Header` (profil, sıralama, koleksiyon,
ayarlar), `StoreScreen` ("Shop", mavi hale), `JourneyScreen`, `GameplayScreen`
("Pause"). Görseller: `_Reference/notes/emboss4.png`,
`emboss_final.png`, `emboss_store_journey.png`.

---

## BLOCK OUT! logosunun "kesik kesik" olması — üç katmanlı sebep (12. tur, W1)

Kullanıcı iki kez bildirdi, ikincisinde: *"knock out logosu da hala kesik
biçimde geliyor, daha iyi bir yöntem bul."* Üç ayrı sebep vardı ve üçü de
ölçülerek bulundu.

### 1. Dilimleme betiği kenar yumuşatmasını yok ediyordu

```
logo_game (bütün logo)  yarı-saydam piksel 4562  (%2,4)
logo_l                                       0   (%0,0)
logo_c                                       0   (%0,0)
```

Tek satır: `letter[own] = data[own]`. `own` boolean olduğu için piksel ya
tamamen kopyalanıyor ya atılıyor; üstelik `own` yüksek alfa istediği için
dış kenardaki yumuşak tüy hiçbir harfe girmiyordu.

İlk düzeltme (maskeyi bulanıklaştırıp ağırlık olarak kullanmak) harfleri
düzeltti ama **zeminlerde dikiş** üretti: iki komşu zeminin paylaştığı
sınırda iki yarı-saydam kenar üst üste binince ince koyu çizgi kalıyor
(452 piksel). Zeminlerin alfası artık BİRLEŞİM siluetinin yumuşatılmış
hâlinden alınıyor.

    harfler   %0,0  ->  %8,4 / %9,9 / %6,0 / %9,7 / %9,6 / %8,9
    dikiş     452 piksel (%0,243)  ->  41 piksel (%0,022)

### 2. İçe alırken 512'ye kırpılıyordu

```
logo_game  dosya 662x399  ->  512x309
logo_out   dosya 588x230  ->  512x200
```

Ekranda `logo_out` 667 birime çiziliyor: 512'den 667'ye %30 büyütme.

### 3. ASIL SEBEP: Android'de ASTC_6x6 ile SIKIŞTIRILIYORDU

```
Android   max 512   format ASTC_6x6
```

ASTC 6x6 blok tabanlı — her 6x6 pikselde renk sayısını kısıtlıyor. Düz
renkli ikonlarda görünmez ama logo baştan sona gradyan ve yumuşak kenar;
orada blok sınırları basamak basamak çıkıyor. Kullanıcının telefonda
gördüğü şey buydu.

**API ve meta düzenlemesi TUTMADI.** Ne `SetPlatformTextureSettings` ne de
`.meta` dosyasını elle değiştirmek işe yaradı; her içe almada eski hâline
dönüyordu. Sebep: `UiSpriteImporter` bir `AssetPostprocessor` ve
`OnPreprocessTexture` içinde bütün UI sprite'larına 512 + ASTC_6x6
dayatıyor. Düzeltme oraya yazıldı (`MaxSize` + yeni `Uncompressed` kümesi).

    13/13 tam boyut, 13/13 sikistirmasiz (RGBA32), 3,5 MB
    logo_out 588 -> 667 birim  (1,13x; onceki 512 -> 667 = 1,30x)

*Ders (ayarın TUTMADIĞINI görünce, ayarı değil SAHİBİNİ ara): Üç ayrı yolla
(importer API, platform ayarı, meta dosyası) aynı değeri yazdım ve üçü de
sessizce geri alındı. "Yazdım ama olmuyor" bir postprocessor'ün varlığının
en net işareti. Değeri tekrar tekrar yazmak yerine "bunu kim eziyor" diye
sormak gerekiyordu.*

*Ders (sıkıştırma İÇERİĞE göre seçilir): "Bütün UI'da ASTC" makul bir
varsayılan ama istisnasız uygulanınca gradyanlı tek varlığı bozuyor. Bir
kuralın doğru olması, istisnasının olmaması demek değil.*

### Önizleme kurarken düşülen iki tuzak

- **Kök düz `Transform` olamaz.** İlk önizlemede `new GameObject(...)`
  kullandım, yakalama BOMBOŞ çıktı — arayüz çocukları çapalarını üst
  DİKDÖRTGENE göre çözüyor ve düz Transform altında yerleşim sessizce
  çöküyor. Bu tuzak `CreateResultPreview`de zaten belgelenmişti.
- **`Build` sonunda kök kapatılıyor** (`_root.SetActive(false)`), çünkü
  kutlama ancak kazanınca açılıyor. Önizleme onu açmak zorunda.

## Onay paneli: ölçülmüş düzeni bozmak (12. tur, R1)

İlk denemede paneli `ContinueOffer` aşama 2'nin çerçevesine taşıdım.
Gerekçem "ikisi aynı soruyu soruyor"du; kullanıcı reddetti.

Hatam: o panelin ölçüleri ZATEN REFERANSTAN alınmıştı — yorumlarda açıkça
yazıyordu ("referansta y 236-414", "referansta y=183", "#7135D3, 4 piksel").
Ölçülmüş bir düzeni başka bir ekranın ölçüsüyle değiştirdim; iki panel
benzeşti ama ikisi de referanstan uzaklaştı.

Ölçüler geri getirildi; kullanıcının ASIL şikâyeti olan "dış kısmı yok"
için yalnız kenar katmanları eklendi (üstte pervaz, altta ayak).

*Ders (ölçülmüş olanı TUTARLILIK adına bozma): "İki ekran aynı şeyi
söylüyorsa aynı görünsün" makul bir ilke ama ölçümün önüne geçemez. Bir
düzeni değiştirmeden önce sor: bu değerler nereden geldi? Yorumda kaynak
yazıyorsa, o kaynak benim benzetme isteğimden ağır basar.*

---

## ⚠ REFERANS VİDEOLARI KAYIP DEĞİLMİŞ (12. tur)

İki tur boyunca "`Game over .mp4` depoda yok, doğrulanamıyor" diye yazdım ve
buna dayanarak iki karar verdim. **Yanlıştı.** Videolar duruyor:

    C:\Users\CPN12\OneDrive\Masaüstü\Block Out! Videos\
        Game over .mp4                      (kaybetme akışının tamamı)
        Block Out! menus,powerups,vs.mp4    (menüler + kutlama)
        Block Out Color Sort Puzzle Levels.mp4
        ... 21-30, 31-40, 41-50 ...

Ben yalnız `_Reference/` altına bakmıştım. `docs/DEVAM.md` ve
`project-status` hafızası yolu zaten yazıyordu.

*Ders (arama yerini genişletmeden "yok" deme): "Depoda .mp4 yok" doğru bir
gözlemdi ama "referans yok" yanlış bir sonuçtu. Bir kaynağın bulunmaması,
onun var olmadığı anlamına gelmiyor — hele proje notu nerede olduğunu
söylüyorsa.*

Portatif ffmpeg: `_Reference/.venv` içine `imageio-ffmpeg` kuruldu.

## O1 — "Süre Doldu": panel EKLEMEK hataydı

Kullanıcı *"bak bu kısmı da patlamış, ne kadar kötü gözüküyor ikon burada"*
dediğinde, arkadan sızan tahtayı panel eksikliği sandım ve aşama 2'nin
bantlarını aşama 1'e de kurdum. Referans (8. saniye, tam çözünürlük) bunu
çürüttü: **kart YOK**, içerik doğrudan koyu perdenin üstünde ve tahta
hafifçe görünüyor — sızma kusur değil, tasarım. Eski ölçülerimiz de zaten
tutuyormuş:

```
öge      bizim (eski)   referans
saat     0,416-0,675    0,405-0,675
düğme    0,162-0,250    0,159-0,249
başlık   0,742-0,805    ~0,778
çarpı    0,894-0,942    ~0,916
```

Panel geri alındı. Kullanıcının gördüğü kusur İKONDU: referansta **altın
kronometre**, bizde süre yardımcısının yeşil çalar saati kutusuna gerilmiş
hâlde. `_Reference/draw_stopwatch.py` yazıldı (üç tur rötuş):

```
halka ortanca  bizim (246,186,18)   referans (251,195,0)
```

Kutu da yeniden ölçüldü: 0,229-0,742 × 0,399-0,681 (eskisi 46 birim alçaktı
ve `preserveAspect` yüzünden kronometreyi küçültüyordu).

*Ders (şikâyetin işaret ettiği yere değil, SEBEBİNE bak): "Panel patlamış"
cümlesini panelin kendisiyle ilgili sandım ve olmayan bir paneli inşa ettim.
Kullanıcı kusuru gördüğü yerden tarif eder; hangi öğenin kusurlu olduğunu
ölçüm söyler.*

## W2 — konfeti 0,39 saniye geç başlıyordu

Kutlama 20 fps'te 120 kareye ayrıldı (t=0 siyaha geçiş):

```
t(sn)   konfeti(alt)%   fişek(üst)%   logo%
0,15        0,1            0,9         25,4
0,55        3,2            4,0         49,6   <- konfeti BAŞLIYOR
1,15        5,1            3,4          —
1,75        8,7            6,5          —     <- konfeti doyuyor
2,15        8,7           10,2          —     <- fişek tepesi
```

`ShowStarts` 0,94 -> **0,55**. Referansta konfeti logo HENÜZ OTURURKEN
başlıyor; bizde logo tamamen yerleştikten sonra, arada ölü bir an bırakarak.
Fişek tablosu (`ReferenceBursts`, 1,614-2,741) ölçülen 1,75-2,15 penceresiyle
örtüştüğü için ona dokunulmadı.

*Ders (bir kutlamada üst üste binme, sıralamadan iyidir): İki olayı arka
arkaya dizmek doğal geliyor ama referans onları BİNDİRİYOR ve iki hareket
birbirini itiyor.*

## W3 — atlamak ile hızlandırmak aynı istek değil

Tek dokunuş doğrudan sona atlıyordu; "biraz hızlansın" diyen oyuncu
kutlamayı hiç göremiyordu. İki kademe: ilk dokunuş `_speed = 3,2`, ikincisi
`_skip`. Bütün bekleme döngüleri ve konfeti saati çarpanı kullanıyor.

    normal   0,94 + 2,50 = ~3,4 sn
    hizli    3,4 / 3,2   = ~1,1 sn
    ikinci dokunus -> aninda

Üç statik yardımcı metot örnek alanına erişemediği için onlar normal hızda
kalıyor (kısa animasyonlar; asıl bekleme boşlukları hızlanıyor).

### O1 devamı — kronometre ÇİZİLMEDİ, REFERANSTAN ÇIKARILDI

Prosedürel çizim üç rötuş turundan sonra bile referansın doygunluğunu ve
3B kabartmasını yakalayamadı. Kullanıcı: *"orjinal kronometreyi kullan,
soldaki referans, oradakini kullan; yazıyı buton boyutunu da aynı yap...
soldakiyle birebir aynı yap."*

**GÜRÜLTÜ ORTALAMAYLA SİLİNDİ.** Kaynak yalnız 384x832 (`Game over .mp4`;
yürüyüş videoları 592x1280 ama orada "Süre Doldu" ekranı hiç yok — oyuncu
süreyi hiç doldurmuyor). Tek kareyi kesmek video sıkıştırma gürültüsünü de
getirirdi. Sahne duruk olduğu için 7,2-9,4 sn arası **132 kare** çıkarılıp
ortalandı:

    kare sayisi 132   ortalama kare farki 0,02   en buyuk 1,01

Yani sahne tamamen sabit; ortalama gürültüyü sıfıra yakın indiriyor ve
efektif çözünürlüğü artırıyor. Sonra yumuşak matleme + en büyük bağlı
bileşen ile kesildi (217x225), 3x LANCZOS ile 651x675'e çıkarıldı,
`Uncompressed` alındı.

*Ders (duran bir sahnede kare ORTALAMAK, çözünürlük kazandırır): Tek kareden
kesmek video kodeğinin blok gürültüsünü de kesiyor. Aynı sahnenin yüz karesi
aynı görüntünün yüz farklı gürültülü ölçümü demek; ortalamaları gerçeğe çok
daha yakın.*

**YAZI VE DÜĞME DE ÖLÇÜLDÜ** (aynı ortalanmış kare):

```
                 referans   önce    sonra
düğme yazısı      %2,28     %1,61   %2,34
başlık dolgusu    %4,33     %3,65   %4,58
başlık konturu    %6,37     %4,06   %7,08
```

Kontur beş kat inceydi. Tek `SetOutline` bunu veremiyor (TMP konturu harfin
kenarından İÇERİ büyür, kalınlaştırınca dolgu kararır — menü başlıklarında
öğrenilen ders); `UiTitleEmboss` kırmızı haleyle uygulandı.
Punto: başlık 82->98, düğme yazısı 46->66, "+30 SECONDS" 56->110.

Görsel: `_Reference/notes/timesup_cmp4.png` (sol referans, sağ bizim).

NOT: `_Reference/draw_stopwatch.py` duruyor ama ARTIK KULLANILMIYOR —
içindeki ölçümler (renkler, biçim tarifi) belge değeri taşıdığı için
silinmedi.

### O1 son adım — düğme BÜTÜN olarak ölçeklendi

Kullanıcı: *"butonu ve texti ayarla, boyutunu ölçüsünü, butonu
referanstakine benzet."* Ölçüm:

```
                referans   önce     sonra
düğme           255x74     703x139  700x209
en-boy oranı    3,45       5,06     3,35
yazı yüksekliği %25,7      %22,5    %24,4
yazı genişliği  %80,4      %76,5    %82,3
```

**Neden yüzde kopyalamak yanlıştı:** referans ekranı 384x832 (oran 0,46),
bizimki 1080x1920 (0,56). Aynı yüzde genişlik bizde fiziksel olarak daha
geniş bir düğme demek; yükseklik yüzdesi sabit kalınca düğme yassılaşıyor
(5,06). Doğrusu düğmeyi bütün olarak ölçeklemek: genişlik oranı korunup
ölçek 717/255 = 2,81 çıkıyor, yükseklik de 74 x 2,81 = 208 birim oluyor.
Kutu bundan biraz büyük çünkü `PillBody` altına gölge koyuyor (yeşil,
kutunun %82'si).

**Punto yine sessizce küçültülüyordu.** 89 yazıldı, ekranda **63** çıktı:
`UiTextFit` "Add Time"i 299 birimlik kutuya sığdırmak için indiriyordu
(çizilen 302). Etiket kutusu 0,47 -> 0,555 genişletilince yazılan punto
gerçekten ekrana gitti (ekranda 75).

*Ders (iki ekranın ORANI farklıysa yüzde kopyalanamaz): Bir öğenin kendi
en-boy oranı, ekrandaki yüzdesinden daha çok şey söylüyor.*

*Ders ("kutuya sığmayan punto, yazılmamış puntodur" — bu turda ÜÇÜNCÜ kez):
Punto değiştirdikten sonra EKRANDAKİ değeri sorgula; yazılan değere güvenme.*

### O1 — jeton kapsülü ve kapat düğmesi

Kullanıcı: *"referans görselin kapat butonu daha iyi, bir de solda coin
gözüküyor bizde de gözüksün."*

**Jeton kapsülü yoktu.** Bizde yalnız çıplak bir jeton simgesi ve KREM
renkli bir sayı vardı; sayı koyu perdenin üstünde zar zor okunuyordu.
Ölçüm (y=56 yatay kesiti):

```
jeton      x %3,9..%12,0
krem hap   x %12,5..%28,1   dolgu (251,238,234)
rakamlar   hapın içinde     (33,32,59) KOYU LACİVERT
```

Krem hap eklendi (jetonun altından başlıyor ki jeton sol ucuna binsin),
yazı rengi krem -> koyu lacivert, punto 34 -> 48.

**Kapat düğmesinin kırmızısı çok koyuydu.** Ölçüm (y=67 kesiti):

```
             referans      bizim (önce)
koyu halka   (163, 0, 0)   (107, 13, 23)   <- siyaha kaçıyordu
parlak yüz   (245,45,50)   (218, 37, 46)
çarpı        (253,248,240) saf beyaz
```

Referansın halkası kırmızının KOYU tonu, siyaha kaçanı değil.

**Doğrulama:**

```
              referans   bizim
kapat halkası %7,1       %7,8
hap genişliği %17,2      %17,7
sayı / hap    %52        %52
```

*Ders (yuvarlak bir şeklin içini ölçerken KÖŞELERİ dışla): İlk ölçümde sayı
"hapın %98'i" çıktı; maske hapın yuvarlak köşelerinden sızan koyu zemini de
saymıştı. Ortadaki %55'lik şeritten ölçünce gerçek değer (%37) göründü.*

### O1 — kapsül yakın plandan yeniden ölçüldü

Kullanıcı: *"bizim coin arkaplanı coin ile birleşik değil, o arkaplan kısmı
basit kalmış, bir de sayı yazan font fazla kalın"* + *"times up dış kontürü
kırmızı fazla keskin, kesilmiş gibi."*

Referans 8x büyütülüp (`_Reference/notes/ref_coinpill.png`) dikey/yatay kesit
alındı. Dört ayrı kusur çıktı:

**1. Hap jetondan AYRI duruyordu.** Hap %9,8'de başlıyor, jeton %12,0'de
bitiyordu — yan yana iki parça. Referansta hap jetonun ARKASINDAN geçiyor.
Sol uç %9,8 -> %6,2.

**2. Hapın altındaki ŞEFTALİ DUDAK yoktu** (ve ilk çözümüm de yanlıştı —
bkz. aşağıdaki not). Dikey kesit (x=80):

```
y43-50  krem   (247,240,236)
y51-62  rakam  ( 51, 48, 80)
y63-67  krem
y68-69  ŞEFTALİ (255,202,183)   <- hap yüksekliğinin ~%7'si
```

Bu şerit olmadan hap düz bir krem dikdörtgen gibi duruyordu — "basit kalmış"
denen şey buydu. İlk denemede 0,009 verdim (hapın %28'i) ve kalın pembe bir
bant çıktı; 0,0022'ye indirildi.

**3. Sayı fazla kalındı** çünkü `CreateTitle`a dolgu rengiyle AYNI konturu
vermişim. Kontur harfin kenarından dışa büyüyor, yani rakamlar şişiyor.
`CreateLabel` + `FontStyles.Bold` ile kontursuz kuruldu.

**4. Hap fazla uzundu.** Referans 78x25 (en-boy 3,1), bizimki 237x61 (3,9).
Sağ uç %28,1 -> %23,7.

**5. Başlığın konturu keskindi.** Hale 8 yöne kaydırılmış kopyadan
oluşuyor; sekiz kopyanın birleşimi çember yerine SEKİZGEN veriyor ve
kenarda tarak dişi bırakıyor. Kalınlık 19 birim olduğu için komşu kopyalar
arası kiriş ~15 birim. Yön sayısı 24'e çıkarıldı (kiriş ~5) ve kopyaların
kendi şişmesi kalanı kapatıyor. Menü başlıkları varsayılan 8'de kalıyor —
oradaki hale ince olduğu için tarak izi görünmüyor.

**Doğrulama:**

```
                referans   bizim
hap+jeton oranı 2,64       2,70
sayı / hap      %52        %52
hap genişliği   %17,2      %17,7 -> %14,3 (kısaltıldıktan sonra)
```

*Ders (kaydırılmış kopyayla hale çizmenin bedeli çokgenleşmedir): Kalınlık
arttıkça aynı yön sayısı daha büyük kirişler bırakıyor. Kalınlığı
artırıyorsan yön sayısını da artır — ikisi bağımlı.*

### O1 — dudak DÜZ bant olamaz

Kullanıcı ilk çözümü de reddetti: *"yaptığın beyaz arkaplanın alt kısmında
uzunluk kalmış, kötü duruyor."*

Dudağı ince ve DÜZ bir dikdörtgen olarak koymuştum. Yüksekliği 4 birim
olunca yuvarlaklığı da 2 birime düşüyor (köşe oranı kısa kenarın yarısı),
yani neredeyse köşeli bir çubuk oluyor ve hapın yuvarlak köşelerinin DIŞINA
taşıyor — altta iki yandan çıkan düz bir uç bırakıyor.

**Doğrusu:** hapın TAM KOPYASINI şeftali renginde, birkaç birim aşağı
kaydırıp ARKASINA koymak. Üstünü krem hap örtüyor, yalnız alt kenarı
görünüyor ve silueti birebir izliyor. Sıra: dudak -> hap -> yazı -> jeton.

*Ders (bir kenar şeridi, ŞEKLİN kendisinden türetilmeli): Yuvarlak bir forma
düz bir şerit eklemek, formun dışına taşan bir uç bırakır. Aynı şekli
kaydırmak hem daha ucuz hem şekil ne olursa olsun doğru — kart, hap, daire
fark etmiyor. Bu projede düğmelerin "üç katman" kuralı da tam olarak bu.*

---

## 13. tur — "Süre Doldu" ekranı: eleman eleman referansa oturtuldu

**Kullanıcı:** *"Coini de ortala yukarıda kalmış referansa bakıp sorunları çöz
her şeyi bana söyletme"*

Tek bir öğe düzeltmek yerine ekranın **altı ögesinin tamamı** referansla yan
yana ölçüldü (`_Reference/notes/sw_avg.png`, 132 karenin ortalaması, 384x832).
Tablo çıkınca kullanıcının işaret ettiği "coin yukarıda" tek bir kusur değil,
**üç ayrı kusurun** ortak belirtisi çıktı.

### Ölçüm (oran = ekran yüzdesi, y ALTTAN)

| öge | referans | ÖNCE | SONRA |
|---|---|---|---|
| jeton | y 0,913–0,951 | y 0,952–0,982 | y 0,910–0,954 |
| jeton genişlik | 0,080 | 0,054 | 0,079 |
| hap | x 0,109–0,281 | x 0,100–0,236 | x 0,116–0,280 |
| düğme alt kenar | 0,162 | 0,171 | **0,162** |
| yazı / düğme yükseklik | %26,0 | %29,9 | **%25,4** |
| yazı / düğme genişlik | %81,1 | %95,7 | **%84,9** |

### Düzeltilen dört şey

1. **Jeton sayacı grubu %3,5 yukarıdaydı ve jeton %33 küçüktü.**
   `BuildCoinReadout`un `y0/y1` parametresi artık JETONUN değil **HAPIN**
   aralığı; jeton bu aralıktan türetiliyor (yükseklik = hapın 2,03 katı,
   merkez hapın %0,18 altında) — ikisi referansta böyle bağlı.

2. **Jeton kutuya ortalıydı ama GÖRÜNEN altın ortalı değildi.**
   `icon_coin.png`in saydam dolgusu asimetrik; `preserveAspect` sprite'ı
   kutuya ortalasa da altın içerik 10 birim yukarıda kalıyordu. Merkez
   ölçülen fark kadar (−0,0058) daha aşağı itildi.

3. **"+30 SECONDS" tamamı büyük harfti.** Ölçüm bunu ele verdi: bizimki
   referanstan hem %19 DAHA GENİŞ hem %24 DAHA KISA çıkıyordu. Aynı yazı
   aynı puntoda iki yönde birden sapamaz. Kırpılıp yan yana konunca görüldü:
   referansta "+30 saniye" küçük harf. Büyük harf hem geniş hem alt
   uzantısız — tam olarak "geniş ama kısa" kutuyu veriyor.
   Küçük harfe çevrildikten sonra ortak olan "+30" rakamları ölçüldü:
   referans 48x48 birim, bizde 51x49 — %4 fark. Kalan toplam genişlik farkı
   yalnızca "seconds" kelimesinin "saniye"den uzun olmasından.

4. **Düğmenin iç yazısı düğmeyi dolduruyordu.** Punto 89 → 77.
   Eski hedef "punto x kapak oranı" tahmininden hesaplanmıştı ve %16 saptı.

### Sapma sanılıp SAPMA OLMAYAN üç ölçüm

`kapat`, `saat` ve `miktar` genişlik farkı bayrağı kaldırdı ama üçü de
gerçek kusur değil:

- **kapat** ve **saat**: dikeyde birebir (dy 0,000 ve 0,001). Genişlik
  yüzdesi farkı ekran ORANINDAN geliyor — referans 384x832 (≈9:19,5), bizim
  1080x1920 (16:9). Dairesel bir nesne iki ekranda aynı anda hem genişlik
  hem yükseklik yüzdesini tutturamaz.
- **miktar**: dil farkı ("seconds" > "saniye").

### Dersler

- **Bir grubu ölçerken ÖGE ÖGE karşılaştır.** "Jeton yukarıda kalmış" tek
  öğe sorunu gibi duruyordu; altı ögenin kutusu referansla yan yana
  yazılınca grubun tamamının kaydığı, jetonun küçüklüğü ve hapın kısalığı
  aynı anda göründü. Tek tek bakmak, her turda bir kusur bulup ötekini
  kaçırmak demek.
- **İki yönde birden sapan ölçüm, ölçümü değil VARSAYIMI yalanlar.** Bir
  kutu referanstan hem daha geniş hem daha kısa çıkıyorsa yerleşimi
  kurcalamadan önce iki görüntüyü yan yana KIRP — karşılaştırdığın şeyler
  aynı şey olmayabilir. Burada biri büyük harf biri küçük harfti.
- **Ekran yüzdesi farklı oranlı ekranlar arasında taşınmaz.** Sapma
  bayrağını kaldıran her ölçümü, yükseklik-normalize birime çevirip bir daha
  bak; oran artefaktı gerçek kusurdan bu şekilde ayrılıyor.
- **Kutuyu değil GÖRÜNENİ hizala.** `preserveAspect` sprite'ı kutuya ortalar
  ama sprite'in saydam dolgusu dengesizse görünen şekil ortalanmaz.
- **Hedef bir ORANSA, oranı PUNTODAN değil ÇİZİLENDEN hesapla.**
  `UiTextFit`, kontur kalınlığı ve harf aralığı araya giriyor.

### Araç hatası (kendi hatam, not düşülüyor)

`BuildCoinReadout`u toplu bir Python betiğiyle değiştirirken dilim
sınırlarını ters aldım (`BuildCoinReadout` dosyada aşama 2'den SONRA
geliyor, ama kesme noktası olarak aşama 2'nin başlığını kullandım) ve 129
satırlık bir blok ÇOĞALDI. Dosya yine de derlendi çünkü çoğalan blok tam
metotlardan oluşuyordu — sessizce iki `BuildStage2` ve iki
`BuildCoinReadout` oluştu. Süslü parantez sayımıyla ve metot listesiyle
yakalandı.
**Ders:** kaynak dosyada toplu dilimleme yapma; `Edit` ile tek tek değiştir
(zaten kayıtlı bir tercih). Yapılacaksa kesme noktalarının SIRASINI doğrula
— `a < b` varsayımı dosyanın gerçek düzeniyle uyuşmayabilir.

---

## 13. tur (devam) — A1, A2 ve P5: aç/kapa anahtarı ile ayar satırları

Duraklat panelinin referansı elimizdeki videoların hiçbirinde yok. Ama
`Block Out! menus,powerups,vs.mp4`in **54. saniyesindeki "Ayarlar" ekranı**
BİREBİR aynı satır kurgusunu taşıyor (simge · etiket · ikili anahtar) ve o
ekran hem A1/A2'nin hem P5'in kaynağı oldu. 53,6-55,4 sn arası **109 kare
ortalanarak** `_Reference/notes/ayarlar_avg.png` üretildi.

### A1 — kapalıyken KIRMIZI düğme

Kullanıcı: *"ON iken yeşil, OFF iken KIRMIZI düğme."* Referansta kapalı bir
satır yok (dört ayar da açık), yani bu bilinçli bir sapma.

Çip artık yalnız renk değil **YER** de değiştiriyor: açıkken sağda yeşil,
kapalıyken solda kırmızı. Kırmızı bir düğmeyi sağda, "On" yazısının üstünde
bırakmak yazının söylediğinin tersini gösterirdi.

Bu, dokunma kurgusunu da değiştirdi. Eskiden sağ yarının dokunma yüzeyi
ÇİPİN dış konturuydu; çip sola gidince sağ yarı ölü alan kalırdı. Şimdi iki
yarı da yuvanın içinde **sabit** birer yüzey, çip ve yazılar üstlerinde ve
hiçbiri ışın hedefi değil.

> **Ders:** hareket eden bir parçayı dokunma hedefi yapma. Tıklama alanı
> kontrolün sabit yarısına aittir; görsel gösterge onun üstünde gezinen ayrı
> bir katmandır.

### A2 — "on/off yazıları fazla koyu"

| | eski | ölçülen referans |
|---|---|---|
| sönük yarının yazısı | (71,57,208) | **(124,112,226)** |
| yuva içi | (52,43,126) | **(39,30,105)** |
| çip yeşili (gövde) | (40,191,13) | **(52,169,20)** |
| çip yazısı | (28,64,25) | **(5,67,1)** |

Eski değerler tek bir JPEG karesinden alınmıştı.

> **Ders:** koyu zemindeki ince yazıyı tek kareden ölçme. Sıkıştırma en çok
> düşük kontrastlı ince ayrıntıyı bozar; kare ortalaması bu yazıyı 53 birim
> açığa çıkardı.

### Ölçüm bir de eski bir "gerçeği" çürüttü

Kodda "çip yuvanın sağ ucundan TAŞIYOR — referanstaki kabartma" yazıyordu ve
çip 0,60..1,065'e konmuştu. Ortalanmış karede anahtar satırının yatay kesiti:

```
yuva (ray)  x %63,8..%92,2   genislik %28,4
yesil cip   x %78,9..%91,7   genislik %12,8
```

Çip yuvanın **içinde** ve sağ ucuna dayalı (fark %0,5). Dikeyde de içeride:
yuva %74,28..%79,33, çip %75,0..%79,1 — yuva yüksekliğinin %81'i.
Yeni duraklar: sağ %53..%100, sol %0..%47, dikey %9,5..%90,5.

> **Ders:** tek karelik bir JPEG'e dayanan "ölçüm" bir tahmindir. Çipin
> parlak kenarı zemine taşınca dışarı çıkmış gibi görünüyordu.

### P5 — satır düzeni

**Etiketler ortalıymış, sola dayalı değil.** Referansta dört etiketin
başlangıcı farklı (%25,5 / %31,5 / %31,8 / %30,7) ama merkezleri aynı
(%40,6). Bizde dördü de %25,0'ten başlıyordu.
Düzeltme sonrası bizim merkezler: %40,7-%40,8.

> **Ders:** hizayı başlangıçtan değil DEĞİŞİMDEN oku. Tek bir satıra bakmak
> sola dayalı ile ortalı arasındaki farkı göstermez.

**Simge boyutları tutarsızdı.** Dördü de aynı kutuyu kullanıyordu ama
çizilen yükseklikler farklıydı — sebep kutu değil, sprite'ların birbirinden
farklı saydam dolgusu (`preserveAspect` opak içeriği değil dosyanın
tamamını sığdırıyor). Aynı tuzak "Süre Doldu" jetonunda da çıkmıştı.

| | ayarlar önce | ayarlar sonra | referans |
|---|---|---|---|
| zil | 73 | **76** | 76 |
| hoparlör | 64 | **68** | 69 |
| nota | 80 | **69** | 72 |
| titreşim | 42 | **70** | 72 |

Duraklat panelinde de aynı örüntü (50 / 64 / 34 birim) — ekran değil dosya
sorunu olduğunun kanıtı. Hedef 61 birim (referansın satır adımına oranı
%48); sonuç **62 / 61 / 61**.

> **Ders:** aynı kutu, aynı boyut demek değil. Bir sprite kümesini tek kutuya
> koyup "hepsi eşit" saymak ancak dolguları eşitse doğrudur.

**Basılı tutunca hareket etme.** Geri bildirim, tıklanan yüzeye
takılıydı — çipin ALTINDA kalan bir kapsül küçülüyordu, yani ekranda hiçbir
şey olmuyordu. `UiPressFeedback`a `Target` alanı eklendi; anahtarda geri
bildirim artık kontrolün TAMAMINA uygulanıyor.

> **Ders:** geri bildirim, dokunulana değil GÖRÜLENE uygulanır. İkisi çoğu
> düğmede aynı nesne olduğu için ayrım fark edilmiyor; ayrıştığı ilk
> kontrolde geri bildirim sessizce kayboluyor.

### Ölçüm aracının kendi hatası

Simgeleri "dolu satır" arayarak bölütlüyordum; zilin gövdesiyle tokmağı
arasındaki boşluk onu ikiye bölüp yalnız üst parçayı ölçtürdü (53 birim) ve
çarpanı 1,30 hesapladım — sonuç 95 birim, hedefin %25 üstü. Kartı dört EŞİT
dilime bölünce gerçek taban 73 çıktı, çarpan 1,04 oldu.

> **Ders:** ölçüm bölütlemesi, ölçtüğün şeyi bozabilir. Bölütlemeyi verinin
> kendisinden değil BİLİNEN düzenden (dört eşit satır) türet.

---

## 13. tur (devam) — G3: kapat çarpısı orijinalden alındı

Kullanıcı: *"Klasik kapat X işareti orijinalden alınacak; bizdeki çok kötü."*

Bizimki üç prosedürel katmandı (koyu halka + kırmızı yüz + ayrı çarpı) ve
referansın hacmini vermiyordu.

**Kaynak seçimi:** video kareleri 384x832; çarpı orada 42 piksel. Ama
OneDrive'daki ekran görüntüleri **946x2048**, yani 2,5 kat büyük — çarpı
orada 120 piksel. Kesme `WhatsApp Image 2026-08-17 ... (2).jpeg` üzerinden
yapıldı.

**Kesme yöntemi:** düğme bir DAİRE olduğu için alfa eşikten değil
**yarıçaptan** kuruldu; kenardaki karışım da bilinen panel moruyla çözüldü
(`gozlenen = a*renk + (1-a)*zemin`).

Yarıçaplar yatay kesitle ölçüldü: kırmızı 0-53, koyu indigo halka **54-60**,
panel 61+. İlk denemede dış yarıçapı 66 alıp panelin morunu da içeri
almıştım — çıktının halkası leylak oluyordu.

**GÖZ YANILDI, ÖLÇÜM DOĞRU SÖYLEDİ.** İkinci kesimden sonra da halka bana
"parlak mavi" göründü ve tekrar düzeltmeye kalkacaktım. Çıktının piksellerini
ölçünce halka **(37,17,94) → (43,31,122)**, kaynak **(37,18,97) → (51,38,144)**
çıktı: birebir. Koyu indigo, kırmızının yanında maviye kaçık okunuyor.

> **Ders:** bu projedeki "gözle değil piksel ölçerek yargıla" kuralı yalnız
> karanlık bölgeler için değil, KOMŞU renklerin birbirini itmesi için de
> geçerli.

### Altı yer, tek bileşen

Çarpı altı ayrı yerde ayrı ayrı kuruluyordu: menü başlığı, duraklat, sonuç,
yeniden dene, günlük ödül, yeni eşya. Önce `MenuPage.CloseGlyph` /
`MenuPage.CloseArt` altında toplandı, sonra hepsi ona bağlandı. Görsel
yoksa eski prosedürel çizime düşülüyor.

> **Ders:** tekrar eden bir görsel parçayı ilk KOPYALARKEN değil, ilk
> DEĞİŞTİRİRKEN pahalıya alıyorsun.

### Boy: iki farklı en-boydaki ekranı ORTAK BİR ÜÇÜNCÜ ŞEYE oranla

Görsel kendi koyu halkasını taşıyor (kırmızı, görselin %79,6'sı), yani aynı
kutuda görünen kırmızı %20 küçüldü. Ne kadar büyütmeli?

```
ekran GENISLIGINE gore   x1,51
ekran YUKSEKLIGINE gore  x1,17
```

İkisi de yanlış — çelişki referansın 384x832 (en-boy 0,462), bizim tuvalin
1080x1920 (0,5625) olmasından. İki tasarımda da olan ve boyu tutan bir öge
ortak cetvel oldu: **başlık bandı** (referans %18,6, bizde %19,3 — neredeyse
aynı).

```
carpi / bant yuksekligi:  referans %25,2   bizim %20,8   ->  x1,21
```

Sonuç: %24,8 (referans %25,2), yatay merkez %86,25 (referans %86,20).

`CreateIconButton` kullanan dört yerde (sonuç, yeniden dene, yeni eşya, ana
ekran can kutusu) kutu 1/0,796 = **×1,256** büyütüldü; oralarda eski kurguda
kutuyu düz kırmızı bir daire dolduruyordu, yani görünen kırmızıyı korumak
için halkanın payı eklendi.

---

## 13. tur (devam) — P5 YENİDEN: duraklat panelinin GERÇEK referansı vardı

**Kendi hatamı düzeltiyorum.** P5'i kapatırken "duraklat panelinin referansı
elimizdeki videoların hiçbirinde yok" deyip `menus,powerups,vs.mp4`teki
**Ayarlar** ekranını vekil almıştım (aynı satır kurgusu: simge · etiket ·
ikili anahtar). Sonra `Game over .mp4`ün kare temas sayfasını çıkarınca panel
**1. saniyede** karşıma çıktı.

Vekil yanlış ölçüler verdi — iki ekran aynı bileşeni kullanıyor ama aynı
BOYUTTA kullanmıyor:

| öge | vekille kurduğum | GERÇEK referans | düzeltme |
|---|---|---|---|
| anahtar çipi | 61 birim | **75** | ×1,50 |
| satır simgesi | 61 birim | **74** | ×1,20 |
| satır etiketi | punto 40 | **punto 55** | ×1,37 |
| başlık | 55 birim | **104** | ×1,89 (punto 88 → 166) |
| kapat çarpısı | 65 birim | **88** | ×1,35 |

> **Ders:** vekil referans, referans değildir. Elimde olmadığını sandığım bir
> ekran için "aynı bileşeni kullanan başka bir ekran" makul göründü ve
> ölçüler %89'a varan farkla yanlış çıktı. Daha önemlisi: kaynağın YOK
> olduğuna karar vermeden önce eldeki her videonun kare temas sayfasını
> çıkarmak 30 saniye sürüyor. Bu oturumda aynı hatayı ikinci kez yaptım
> (referans videolarının OneDrive'da olduğunu daha önce de kaçırmıştım).

### Kart ölçüsü: kendi ölçümüme de aldandım

Renk maskesiyle kart sınırlarını ölçünce "bizim kart %9,5 dar ve %16 kısa"
çıktı. Kartı büyütmeye başlamadan önce yatay ve dikey KESİT aldım:

```
             dis kenar        yuz baslangici
referans     x %3,1           x %9,4        y %79,8 / %25,7
bizim        x %3,3           x %8,9        y %79,5 / %25,8
```

Kart zaten birebir doğruymuş. Maske iki görüntüde FARKLI bantları
yakalıyordu (bizde dört bant var, referansta üç).

> **Ders:** bir kutuyu renk maskesiyle ölçerken, maskenin iki görüntüde de
> AYNI katmanı yakaladığını doğrula. Kesit almak bunu bir bakışta gösteriyor;
> maske sessizce yanlış cevap veriyor.

### Ölü bir kısıt bulundu

Düğmeler yukarı sıkışmıştı çünkü kodda "kartın iç yüzeyi kart-göreli
0,121'de bitiyor" diye bir sınır vardı. O sınır `panel_card` GÖRSELİNİN kalın
alt dudağından geliyordu; 8. turda kart görselden çıkarılıp
`CreateRoundedPanel` bantlarına çevrilmiş ama sayı kalmıştı. Dikey kesit: iç
yüz artık **0,019**'da bitiyor, yani 100 birim boş yer vardı.

> **Ders:** bir kısıt, dayandığı şey kalkınca da yaşamaya devam eder. Bir
> varlığı çıkarırken ONA GÖRE konmuş sayıları da tara.

### Sonuç (hepsi ölçüldü)

```
             referans        bizim
baslik        104 br          106 br
baslik alt    %77,3           %77,3
kapat cap      88 br           89 br
kapat merkez  (%91,7,%75,6)   (%91,3,%75,5)
cip           74/76/76 br     78/79/79 br
simge         -/76/74 br      78/75/75 br
etiket        37/37/46 br     35/38/47 br
Surdur        %41,8..%49,6    %41,9..%49,5
Cikis         %30,5..%38,2    %30,6..%38,4
```

---

## 13. tur (devam) — F1: başarısız ekranı gerçek referansla yeniden kuruldu

F1 daha önce "kapatıldı" sayılmıştı ama yapılan tek şey PERDEYİ koyulaştırmaktı
(sızan tahta). Panelin kendisi hiç ölçülmemişti. Referans:
`Game over .mp4` 17,4-19,8 sn, **144 kare ortalandı**
(`_Reference/notes/basarisiz_avg.png`).

### Kartın boyu: bir önceki tur onu BOZMUŞ

Kodda "referans y 0,254..0,700" yazıyordu ve o ölçüme dayanarak kart
0,255..0,786'dan 0,700'e **kısaltılmıştı**. Yani "düzeltilen" şey aslında
bozulmuş. Doğru değer 0,257..0,789 — yani eski hâli.

Kartın üst kenarı **satır genişliği profiliyle** bulundu:

```
y %84,6..%83,4   genislik 0,20 -> 0,63 -> 0,22   <- BASLIGIN MOR HALESI
y %78,9..%72,7   genislik 0,42 -> 0,93           <- kartin yuvarlak kosesi
y %72,7 ve alti  genislik 0,93                   <- kartin tam genisligi
```

Dikey kesit burada YETMEDİ: x=%16'da kesince başlığın halesini kartın üstü
sanıp %83,9 okumuştum.

> **Ders:** bir kabın sınırını KESİTLE değil PROFİLLE bul. Tek bir sütun, o
> sütunda ne varsa onu gösterir — başlık, hale, gölge. Satır genişliği
> profili kabın gerçek biçimini veriyor ve üstündeki yazıyı kendiliğinden
> ayırıyor (dar bant = yazı, geniş bant = kap).

### Ölçüm maskesi ikinci kez yanılttı

"60" hapını renk maskesiyle 557x219 birim ölçtüm; maske kartın gölgesini ve
düğmenin koyu konturunu da içine almıştı. İki görüntünün ORTA BANDINI kırpıp
yan yana koyunca gerçek çıktı: referans **230x94**, bizim **395x142** — hap
kartı bir uçtan diğerine geçiyordu.

> **Ders:** maske şüpheli bir sayı verdiğinde KIRP. Renk maskesi "bulduğu her
> şeyi" tek kutuya topluyor ve komşu koyu öğeler sessizce içeri giriyor.

### Sayılar tuttuğu hâlde görsel yanlış olabilir

Kırık kalbi referansla birebir aynı yere koydum (y %72,8..%82,1 vs
%73,1..%82,3) ve ekranda başlığın ORTASINI kapattı. Sebep: referansın başlığı
"BAŞARISIZ" 720 birim geniş, bizim "FAILED" 375. Aynı örtüşme kısa kelimede
farklı görünüyor. Kalp başlığın alt kenarına indirildi.

> **Ders:** referanstan alınan bir ÖRTÜŞME, metin uzunluğu değişince aynı
> görünmez. Sayı tuttuğu hâlde ekrana bakmak gerekiyor.

### Sonuç

```
                referans        bizim
kart y          %25,7..%78,9    %25,4..%78,7
baslik          106 br          103 br
kirik kalp      183x178 br      239x179 br   (bizim gorsel daha genis)
"60" hapi       230x94 br       ~235x95 br
jeton yigini    470x367 br      ~490x345 br
Try Again       565x166 br      564x164 br
```

Ayrıca "Rewards x3" rozeti hapın İÇİNDEN çıkarılıp referanstaki yerine —
yeşil düğmenin üst kenarına — alındı, ve `preserveAspect` varsayımım
yüzünden ilk denemede %22 büyük çıkan kalp ölçülerek kapatıldı.

> **Ders:** `preserveAspect` sprite'ı kutunun TAMAMINA sığdırıyor; "kutunun
> içinde bir miktar boşluk kalır" varsayımı yanlış. Kutuyu hedef çizim
> kutusuna eşitlemek doğrudan doğru sonucu vermiyor, ölçüp kapatmak gerek.

---

## 13. tur (devam) — Failed ekranı: kullanıcının altı maddesi

Kullanıcı: *"Failed yazısı boyutu dış çizgisi referanstaki gibi olmalı /
kalp dış mor çizgisi yok alakasız duruyor / seviye yazısının kaplaması yok /
o 60 yazısı ve arkaplanı goldu kaplıyor, biraz altta kalmalı, arkaplan biraz
transparan olmalı / goldların boyutu da yanlış / rewards paneli de çok kötü
eksik"*

Referans yakın plandan incelendi (`_Reference/notes/ref_ust.png` ve
`ref_orta.png`, 4-5x büyütme) ve altı maddenin altısı da doğrulandı:

1. **Başlığın mor halesi.** Referansta "BAŞARISIZ" harflerinin çevresinde
   KALIN parlak mor bir şerit var: hale (146,74,255), kartın yüzü
   (95,30,185) — hale karttan belirgin AÇIK. Bizde ince bir kontur vardı.
   `SetOutline` bunu veremiyor (TMP konturu atlas dolgusuna takılıyor ve
   harfin yüzünü yiyor); 12. turda menü başlıkları için kurulan
   `UiTitleEmboss` buraya da uygulandı.

2. **Kalbin mor konturu.** Referansta kırık kalp bir çıkartma gibi mor
   şeritle çevrili. Kalbin BÜYÜTÜLMÜŞ ve mora boyanmış kopyası arkaya
   çizildi. İlk denemede %9 verdim, ekranda görünmedi; %18 oldu.

3. **Bölüm adının kaplaması.** Aynı hale "Seviye 54"te de var.

4. **"60" hapı.** Referansta hap jetonların ALT UCUNA biniyor, ortasına
   değil; zemini de saydam (ölçülen (45,20,90), kartın yüzü (95,30,185) —
   opak bir lacivert bu değeri veremez). Alfa önce 0,80 verildi, jetonların
   üstünde altın sızdı ve "60" okunmadı; 0,92 oldu.
   *"Biraz saydam", "içi görünen" demek değil.*

5. **Jetonların boyutu.** Yeniden ölçüldü.

6. **Rewards rozeti.** Referansta tek düz turuncu şerit değil: kalın koyu
   dış kontur + turuncu iç yüz + iki renkli yazı. Bizdeki tek katman yeşil
   düğmenin üstünde eriyordu. İki katmana çıkarıldı, punto 24 → 34.

### Üç ders

- **Kabartma katmanları, metin SONRADAN atanıyorsa tazelenmeli.**
  `UiTitleEmboss` haleyi `LateUpdate`te eşitliyor. Bu panelin yazısı
  kurulumda BOŞ, gösterilirken atanıyor — hale ilk karede boş metni
  kopyalıyordu ve düzenleyici yakalamasında `LateUpdate` hiç çalışmadığı
  için hale HİÇ görünmüyordu (ölçüm: 27x18 birim, olması gereken 759x159).
  Artık metnin atandığı yerde `Sync()` çağrılıyor.

- **Hangi ekseni eşitleyeceğine ÇAKIŞMA karar verir.** Kalbin yüksekliğini
  referansla birebir tutturunca (178 birim) genişlik 239 çıkıyordu
  (referans 183) — bizim görselimiz daha basık. Fazla genişlik "FAILED"
  yazısının ortasını kapatıyordu, yani çakışma YATAY; genişliğe göre
  eşitlendi.

- **Aynı ölçüm aracı aynı ögede tekrar tekrar yanılıyorsa, aracı değiştir.**
  "60" hapını renk maskesiyle üç kez ölçtüm, üçünde de yanlış (557, 430,
  ...). Hap koyu zemin üstünde koyu bir öge; maske onu komşularından
  ayıramıyor. Kırpıp bakmak tek güvenilir yoldu ve bunu üç turda öğrendim.

---

## 13. tur (devam) — F2: alt yardımcı (booster) şeridi

Kullanıcı: *"yine alt kısım poweruplar arkaplanı, butonlar, ikon boyutu,
altındaki para kısmı göstergesi vs onları da toparla düzgün hale getir"*

Bu şerit oynanış sırasında ekranın en altında duruyor ve tek başına
yakalanamıyordu — `GameplayScreen.CreatePowerUpPreview` eklendi.
Referans: `Game over .mp4` 2,3-3,1 sn, 48 kare ortalandı
(`_Reference/notes/oyun_avg.png`).

### Ölçüm (iki şerit AYNI GENİŞLİĞE ölçeklenip alt alta konarak)

| | referans | bizim (önce) | sonra |
|---|---|---|---|
| düğme | 194x138 br | 157x137 | ~194x138 |
| şerit aralığı | x %18,1..%81,8 | %23,9..%73,8 | %18,1..%81,8 |
| fiyat hapı yük. | 43 br | 80 br | ~43 br |
| yeşilin düşüşü | 216→115 (%47) | 236→200 (%15) | iki tonlu kuyu |

Düğme referansta kareye yakın değil, **YATAY** (en-boy 1,41); bizimki
neredeyse kareydi (1,05).

**İkon**, düğmeyi tamamen dolduruyor hatta üstünden taşıyordu; referansta
açık yeşil kuyunun içinde her yanında yeşil pay var. Kuyu ikonu "oturtan"
şey — ikon onu kaplayınca düğme düz bir ikon lekesine dönüyor.

**Yeşilin düşüşü** en görünür farktı. Elimizde dikey gradyan sprite'ı yok;
düşüş iki tonlu kuyuyla verildi (altta orta ton, üstte parlak kuyu).
138 birimlik bir düğmede iki basamak sürekli bir geçiş gibi okunuyor —
referansın kendi dikey kesiti de zaten basamaklı.

> **Ders (ölçüm aracını ÖGE ÖGE seçmek gerekiyor):** Bu turda renk maskesi
> üç ayrı ögede yanılttı ("60" hapı üç kez, booster fiyat hapı bir kez).
> Hepsi ortak bir özelliği paylaşıyor: **koyu zemin üstünde koyu öge**.
> Maske onları komşularından ayıramıyor. Böyle ögelerde iki görüntüyü aynı
> genişliğe ölçekleyip alt alta koymak saniyeler sürüyor ve doğru cevabı
> tek bakışta veriyor.

---

## 13. tur (devam) — Try Again paneli: yakın plan farkları

Kullanıcı: *"try again paneline geri dön, orada hâlâ içime sinmeyen kısımlar
var"*. Panelin İKİ BÖLGESİ referansla aynı ölçekte kırpılıp alt alta konuldu
(`fail_baslik.png`, `fail_dugme.png`) ve dört fark çıktı:

1. **Başlığın iç konturu MOR değil KOYU TURUNCU.** Referansta altın harflerle
   mor hale ARASINDA ince, koyu turuncu bir kenar var (ölçüm (139,45,0)) ve
   harfleri haleden ayıran şey o. Mor gölge verince altın doğrudan moru
   sınırlıyor ve yazı "yapıştırılmış" duruyordu.

2. **Harflerin kendi dikey gradyanı yok.** Referansta altın düz değil: tepede
   açık sarı (255,245,107), dipte turuncu (251,163,7). TMP'nin köşe gradyanı
   bunu ayrı bir katman olmadan veriyor.

3. **"Try Again" tek kelime gibi okunuyordu.** `PillButton` harf aralığını
   -16 veriyor (referanstan ölçülmüştü) ve bu KELİME BOŞLUĞUNU da kapatıyor.
   > **Ders:** negatif harf aralığı kelime boşluğunu da yer. Tek kelimelik
   > etiketlerde fark edilmiyor; ilk iki kelimelik etikette ortaya çıkıyor.

4. **Düğmenin dış kenarı çok açık.** `PillBody` dış çizgiyi düğmenin KENDİ
   renginin %15'i olarak kuruyor; yeşil için bu (13,34,0), yani koyu YEŞİL.
   Referansın kaybetme kartındaki düğmesinde ölçüm (7,6,23) — neredeyse
   siyah.

   İlginç olan: duraklat panelindeki AYNI aileden düğmenin konturu referansta
   kendi renginin koyusu ((59,22,60) kırmızı için). Yani bu bir kural değil,
   BU EKRANA ait bir karar — düğme parlak mor bir kartın üstünde duruyor ve
   renkli bir kontur orada zemine karışıyor.
   > **Ders:** aynı bileşen, farklı zeminde farklı ayar isteyebilir. Ortak
   > reçeteyi bozmadan yalnız o çağrı yerinde geçersiz kılmak doğru olan;
   > `PillBody`yi değiştirmek oyundaki otuz düğmeyi birden etkilerdi.

   Kalınlık üç denemede oturdu (0,010 → 0,005 → 0,002):
   > **Ders:** bir kenarı KALINLAŞTIRMAKLA ÇERÇEVE eklemek aynı şey değil.
   > Arkaya konan daha büyük bir panel, kendi köşe yarıçapı da büyüdüğü için
   > köşelerde şişiyor ve düğmenin etrafında ikinci bir hat gibi görünüyor.

Ayrıca "Rewards x3" rozetinin koyu konturu 5 → 7 birime çıkarıldı (5'te
ekranda neredeyse görünmüyordu) ve rozet biraz uzatıldı.

### Koyu kenar denemesi GERİ ALINDI + basış geri bildirimi yumuşadı

Kullanıcı: *"butonun o arkasındaki siyahlık kötü; basılı tuttuğunda çıkan
siyahlık öyle olacak ama onun için bile fazla."*

Bir önceki adımda referansın kaybetme kartındaki düğme kenarını ölçüp
((7,6,23), neredeyse siyah) düğmenin arkasına koyu bir panel koymuştum.
Ölçüm doğruydu ama sonuç yanlıştı: eklenen koyuluk DURAĞAN hâlde bir basış
geri bildirimi gibi okunuyor, düğme hep basılıymış gibi duruyordu.

> **Ders (bir DURUMA ait görsel dili durağan hâle taşıma):** Koyulaşma bu
> oyunda "basılı" demek (`UiPressFeedback`). Aynı sinyali dinlenme hâlinde
> kullanmak, basış geri bildirimini de anlamsızlaştırıyor — iki hâl
> birbirine benziyor. Ölçüm doğruydu ama ölçtüğüm şeyin ARAYÜZDEKİ ANLAMI
> yanlıştı.

Panel tamamen kaldırıldı, kenar `PillBody`nin kendi reçetesinde bırakıldı.

Aynı yargı basış efektinin kendisi için de geçerliydi: `PressDim` 0,84 → 0,92.
%16 koyulaşma parlak yeşil/kırmızı düğmelerde renk değişimi gibi okunuyor,
"basıldı" gibi değil. Ölçüldü: dinlenme (88,191,64) → basılı (81,178,56),
çarpan **0,918**.

> **Ders:** geri bildirim FARK EDİLECEK kadar, DİKKAT ÇEKMEYECEK kadar
> olmalı. Basış efektinin işi düğmeyi değiştirmek değil, dokunuşun
> ulaştığını söylemek.

### Rewards rozeti — ve "renkler çok basit duruyor" teşhisi

Kullanıcı: *"rewards kısmını da güncelle, detaylı incele"*, ardından
*"benzedi ama dil olarak sanki bir eksiklik var, renkler olarak bizimki çok
basit duruyor."*

Rozeti 5 kat büyütüp yan yana koyunca beş fark çıktı:

1. **Şekil** kapsül değil, yuvarlak köşeli dikdörtgen (yarıçap ~%30).
2. **Kontur** koyu KAHVE (72,13,0) — bizde koyu lacivertti, turuncuyla
   akraba olmayan bir renk; rozet "yapıştırılmış" duruyordu.
3. **Dolgu** düz değil: tepede (255,180,34), dipte (238,138,8).
4. **Yazı tek parça değil**: "Ödüller" KOYU KAHVE ve konturusuz, "x3" BEYAZ
   ve koyu konturlu. Bizde ikisi de beyazdı, yani "x3" hiç öne çıkmıyordu.
5. Yazı rozeti neredeyse **kenardan kenara** dolduruyor (yazı %36,5..%64,1,
   rozet %37,0..%63,3).

> **Ders (bir rozetin "x3"ü rozetin kendisi kadar önemli):** Referans
> çarpanı ayrı renkte ve konturlu yazarak onu ikinci bir rozet gibi
> gösteriyor; tek renkte yazınca oyuncu yalnız "ödüller" kelimesini okuyor
> ve KAÇ KAT olduğunu kaçırıyor.

**Kullanıcının asıl teşhisi daha genel:** üç düz panelle (kontur + dolgu +
üst bant) gradyanı taklit ettim ve "benzedi ama basit" oldu. Referansta
HİÇBİR yüzey tek ton değil. `UiSprites.RewardTag` konturu, turuncu rampayı
ve üst iç parlaklığı TEK dokuya çiziyor — aynı çözüm bu turda
`UiSprites.PowerPad` için de gerekmişti.

> **Ders (bu turda İKİNCİ kez):** sürekli bir geçiş isteniyorsa panel
> eklemek çözüm değil, ÇİZİM gerekiyor. Düz renk panelleri iki-üç ton için
> doğru araç; ötesinde her yeni panel hem basamak ekliyor hem de sonucu
> "sade" bırakıyor. Bundan sonra bu tür bir yüzey istendiğinde doğrudan
> doku üretmek daha kısa yol.

---

## 13. tur (devam) — M1: mağazanın jeton kapsülü

Kullanıcı: *"bizdeki çok düz ve dış rengi siyah, kontürü çok; onu düzeltelim
birebir orijinal hâle getirelim."*

### Önce YANLIŞ ölçtüm — ve yanlış ölçüm "makul" göründü

Kapsülün dikey kesitini x=%36'da aldım ve şunu okudum: koyu lacivert kenar
(4,5,70), soğuk beyaz dolgu (245,248,255), üstten alta sönen bir gradyan.
Bu değerlerle `CoinPad` dokusunu kurdum.

Kapsül **%11,9..%28,2** arasındaymış — yani o sütun kapsülün DIŞINDAYDI ve
okuduğum şey tentenin bandıydı.

> **Ders (bu turda İKİNCİ kez):** bir kesit almadan önce ÖĞENİN NEREDE
> olduğunu ölç. Yanlış sütundan alınan kesit yine de makul sayılar veriyor
> ve insan onları öğenin kendisi sanıyor. (Aynı hatayı `basarisiz_avg`de
> başlığın halesini kartın üstü sanarak da yapmıştım.)

### Temiz sütundan (x=%26,5, rakamların sağı) gerçek yapı

```
ust kenar   yumusak gecis, KOYU HALKA YOK
govde       (254,243,237)  SICAK KREM, DUZ
alt %8      (255,249,246)  bir tik acik
en alt      (233,166,149) -> (125,91,89)   SICAK DUDAK
```

Yani referansta kapsülün konturu **hiç yok**; hacmi veren şey alttaki sıcak
dudak. Bizde ise üç katman vardı: gölge + **(35,19,9) siyaha çalan kahve
kenar** + krem yüz. Kullanıcının *"dış rengi siyah, kontürü çok"* dediği şey
tam olarak o kenardı.

### Sonuç

| | önce | sonra | referans |
|---|---|---|---|
| kapsül | 158x45 br | **179x58** | 176x60 |
| kenar | (35,19,9) kalın | **yok** | yok |
| dolgu | (255,249,236) düz | sıcak krem + alt dudak | (254,243,237) |
| jeton | 56 br | **83 br** | 83 br |
| rakam | punto 40 | **52** | ~%60 kapsül boyu |

Üç katman tek prosedürel dokuya indi (`UiSprites.CoinPad`) — bu turda aynı
çözümün dördüncü kullanımı (`PowerPad`, `RewardTag`, `CoinPad`).

---

## 13. tur (devam) — M2: tente

### M2a/M2b — asıl fark: referansın alt kenarı DÜZ

Kullanıcı iki şey söylemişti: *"alt kısmına doğru bir gölge şeklinde çizgisi
var"* ve *"tentenin ucu aşağıya düşmüş gibi gözüküyor, bu derinlik katıyor"*.
İkisi de tek bir yapısal farkın sonucuymuş:

Referansta tentenin alt kenarı **düz bir çizgi**. Festonun çentiklerini koyu
lacivert bir levha dolduruyor ve yaylar o levhanın üstünde kumaşın sarkması
olarak okunuyor. Bizde çentikler SAYDAMDI, arkadaki kahverengi duvar
görünüyordu ve alt kenar taraklı bir siluetti — "sarkan kumaş" değil
"dalgalı kenar" gibi.

Levha DOKUYA çizildi, ayrı bir panel olarak değil. Bu bilerek: aynı levha
**8. ve 11. turlarda ayrı panel olarak eklenip iki kez kaldırılmıştı**, çünkü
ayrı panel tentenin altından taşıyor, kaydırmada sürükleniyor ve turuncu
şeridin üstüne gölge düşürüyordu.

> **Ders (bir öğe iki kez kaldırıldıysa, üçüncüsünde YERİNİ değiştir):**
> Levhanın kendisi doğruydu; yanlış olan ayrı bir katman olmasıydı. Dokunun
> içinde tam olarak festonun bittiği yerde bitiyor, taşamıyor.

### Ölçümler

| | önce | sonra | referans |
|---|---|---|---|
| yay derinliği | 18 br | **41** | 41 |
| kumaş altı ↔ tente dibi | 11 br | **32** | 29 |
| tente yüksekliği | %13,9 | **%15,3** | %15,4 |
| başlık kapak boyu | 103 br | **84** | 84 |
| başlık merkezi (tente içinde) | %57,9 | **%41,5** | %43,2 |

**9. turdaki geri alma açıklandı:** o turda derinlik %21,6'ya çıkarılmış,
kullanıcı *"tente daha kötü oldu"* demiş ve %10,3'e dönülmüştü. O sırada
çentikler saydamdı — derin yaylar kahverengi duvarın önünde uzun parmaklar
gibi sarkıyordu.

> **Ders (bir sayı tek başına değil, KOMŞUSUYLA birlikte yanlıştı):**
> Derinlik hep doğruydu; eksik olan altındaki levhaydı. Yalnız derinliği
> değiştirip geri almak iki turluk bir döngüye mal oldu.

### M2d — tente yüksekliği yorumla çelişiyordu

`AwningH = 268` idi. Kodun kendi yorumu doğru hedefi zaten yazmış:
*"referans tente 315 px / 2048 = %15,38 -> 1920x0,1538"* = **295**. Ama
konan sayı 268 ve gerekçesi bir alt satırda: *"tente 315 px -> GENİŞLİĞİN
%33,3'ü, bizimki 268/1080"*.

> **Ders (bu projede tekrarlayan tuzak):** kanvas YÜKSEKLİĞE göre
> ölçekleniyor; bir ölçüyü genişliğe oranlamak sessizce %10 hata veriyor.
> Yorumda doğru sayı dururken yanlışının yazılmış olması ayrı bir uyarı —
> hesabı yapıp sabiti güncellememek kolay.
