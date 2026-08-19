# 7. TUR — kullanıcının 26 maddesi (M45 … V70)

Tek doğruluk kaynağı bu dosya. Her maddede **ne istendi**, **referans ölçümü**,
**bulunan sebep**, **ne yapıldı** ve **nasıl doğrulandı** var.

Referans kaynakları:
- `C:/Users/CPN12/Downloads/Block Out Color Sort Puzzle Levels 1-20 Walkthrough.mp4`
- `C:/Users/CPN12/Downloads/Block Out! Level 41-…-50 Solution Walkthrough.mp4`

Doğrulama araçları (hepsi düzenleyici kipinde, oyunu başlatmadan çalışır):
- `UiCaptureTool.Capture(ad, w, h, kanvas)` — arayüz ekranı PNG
- **`BoardCaptureTool.Capture(bölümYolu, ad)` — YENİ**, tahtayı PNG'ye çeker
- **`UiOverflowAudit.Report(ad, kök)` — YENİ**, kutusuna sığmayan yazıları listeler
- **`MenuShell.CreateTabBarPreview(kanvas, sekme)` — YENİ**
- **`GameplayScreen.CreatePausePreview(kanvas)` — YENİ**
- **`GameplayScreen.CreateHudPreview(kanvas)` — YENİ**
- `GameplayScreen.CreateResultPreview(...)` (6. turdan)
- `StoreScreen.Build(kanvas)` (6. turdan)

---

## M. METİN TAŞMA

### M45 — Daily Reward "See you tomorrow" taşıyor ✔
Ödül alındıktan sonra düğme yazısı "CLAIM" → "SEE YOU TOMORROW" oluyor ve
kapsülden taşıyordu. M46'daki yapısal düzeltme bunu kendiliğinden çözdü.
**Doğrulama:** panel düzenleyicide kurulup etiket elle "SEE YOU TOMORROW"
yapıldı; taşma denetimi 0 döndü, ekran görüntüsünde yazı düğmenin içinde.

### M46 — Hiçbir yazı taşmamalı (KRİTİK) ✔
**Sebep:** `UiKit.CreateLabel` her etiketi `NoWrap + TextOverflowModes.Overflow`
ile kuruyor — yani sığmayan yazı sessizce kutusunun dışına çıkıyor. Bu bilinçli
bir seçimdi (`Truncate` 4. turda bir satırı TAMAMEN silmişti) ama bedeli
ödenmemişti.

**Çözüm — yapısal, ekran ekran değil:** yeni `GameKit.UI.UiTextFit`. Her
etikete `CreateLabel` içinde otomatik takılıyor; yazı kutusundan genişse
puntoyu küçültüyor (taban %45). Yalnız GENİŞLİK — bu arayüzde birçok etiket
bilerek dar bir şeride konup dikeyde ortalanıyor, yükseklikten sıkıştırmak
onları yarı yarıya küçültürdü.

Yan bulgu: alt sekme çubuğunun "Leaderboard" etiketinde TMP'nin kendi
`enableAutoSizing`'i açıktı ve `Truncate` ile birlikte çalışıyordu; denetim
kutu 203 birim / gerek 262 birim ölçtü, yani yazı sığmadan çiziliyordu.
İkisi de kaldırıldı, iş `UiTextFit`'e verildi.

**Doğrulama:** yeni `UiOverflowAudit` ile 12 ekran durumu tarandı —
Mağaza, Liderlik, Yolculuk, Profil, Koleksiyon, Ayarlar, iki sekme çubuğu
durumu, Duraklat, PERFECT, BAŞARISIZ, Günlük Ödül. **Toplam taşan yazı: 0.**

---

## N. MAĞAZA

### N47 — Branda altındaki mavi zemin + gölge kaydırınca geliyor ✔
Levha altı turdur `root` altındaydı, yani kaydırma alanının DIŞINDA. Kaydırmayan
bir tuvalde görünür sonucu yok; parmak listeyi ittiği anda kartların üstünde
asılı kalıyor. `BuildAwningShade` içeriğin SONUNA taşındı: kardeş sırası
sayesinde hâlâ kartların üstünde ama artık onlarla birlikte kayıp gidiyor.
**Doğrulama:** içerik 700 birim kaydırılıp yakalandı — levha ve gölgesi yok,
kartlar doğrudan festona değiyor.

### N48 — %90 bayrağındaki yazı ortalanmalı ✔
`MenuSprites.Pennant` 160×200 ve alt kenarında 46 piksellik bir V çentiği var;
dolu gövde 0.23-1.00, ortası 0.615. Yazı bloğu 0.60'taydı ve arkadaki kontur
katmanı flamayı 0.045 daha uzatıyor. Blok 0.06 aşağı çekildi.

### N49 — Starter Pack yerleşimi ✔
`art` paneli bandın üstüne kart yüksekliğinin %4'ü kadar biniyor (yuvarlak
köşelerde dikiş kalmasın diye) = 20 birim, bandın %14'ü. Ad ve fiyat düğmesi
bandın TAMAMINA göre ortalandığı için ikisi de o payın yarısı kadar yukarıda
duruyordu. Ortalama artık GÖRÜNEN yüksekliğe göre; ad ayrıca 0.05 → 0.032'ye
çekildi.

### N50/N51 — Bölüm başlığı şeritleri ✔
Eski dil iki katmanlıydı ve iç rengi BÖLÜM RENGİNİN koyusuydu (Paketler'de mor,
Teklifler'de kızıl). Referansta dil tek renk, kapsülün altın konturuyla aynı.
Dil artık: tek katman, altın, %68 yerine %52 yükseklikte (daha ince) ve İKİ
UÇTA — sağdaki aralık (0.930-1.045) aynalanarak sola taşındı.

### N52 — Taşıyıcı noktaları ✔
Yarıçap 0.011 → 0.015 (çap 23,8 → 32,4 birim), aralık 0.035 → 0.044. Dikeyde
kutu 24,8 birimdi, çap 23,8 — `CreateIcon` en-boy koruduğu için nokta 1 birim
yukarıda kalıyordu; kutu çapa eşitlendi.

### N53 — Jeton kutularının alt kırmızı alanı ✔
**Ölçüldü (kendi yakalamamız):** kremin dibi 592, kutunun dibi 710 → taban 118
piksel; yeşil düğme 90 piksel. Kırmızıdan geriye düğmenin çevresinde 8-13
piksellik bir çerçeve kalıyordu. Taban 132 → **176** birim; düğme 90 birimde
SABİT bırakıldı (aksi hâlde "çok büyük görünmesin" isteği karşılanmazdı) ve
kutunun altına gölge eklendi.

---

## O. LIDERLIK

### O54 — Kaydırmada üst maskeleme ✔
**Sebep:** görüntü alanı 1006'da başlıyordu, park sahnesi ise 1042'de bitiyor —
listenin ilk 36 birimi ÇİMİN ÜSTÜNDEYDİ ve satırlar çimenli bir görselin
ortasında kesiliyordu. Kesim hiçbir kenara denk gelmediği için "yanlışlıkla
kırpılmış" gibi okunuyordu.

Yeni yapı (kürsüden aşağı):
1. sahne 1042'de biter,
2. **26 birim düz, çerçevesiz mor çizgi** (ekranın gövde rengi),
3. **7 birimlik açık mor çerçeve** (satır genişliğinde),
4. kaydırma alanı.

İkisi de görüntü alanından SONRA kuruluyor; kaydırılan satırlar onların ardına
girip kayboluyor.
**Doğrulama:** liste 190 birim kaydırılmış hâlde yakalandı.

---

## P. ANA EKRAN / ALT MENÜ

### P55 — Ana ekrandaki level yazıları ✔
`btn_*` görselinin alt 9-dilim payı 68 piksel; 201,6 birimlik düğmede alt %34
3B DUDAK, yani yazı yüzü 0.34-1.00 ve ortası 0.67. İki yazının bloğu
0.355-0.94 idi, ortası 0.6475 — %2,3 aşağıda; üstelik iki kutu 0.58-0.60
arasında ÜST ÜSTE biniyordu. Blok 0.375-0.955'e çekildi. Taşma tarafını
M46 yapısal olarak kapattı.

### P56 — Seçili sekmenin köşelerinde piksel bozulması ✔
İKİ ayrı sebep bulundu:

1. **Çözünürlük.** Kart 263×277 birim ve köşe yarıçapı 44 birim, ama
   `UiSprites`'ın yuvarlak panel dokusundaki yay 18 piksel — yay ekrana
   **2,4 kat büyütülerek** çiziliyordu; 1,5 piksellik yumuşatma bandı 3,6
   birime çıkıyor, dokunun basamakları görünür oluyordu.
   Doku 128'e, yay 36 piksele, dilim payı 40 piksele çıkarıldı — VE sprite'ın
   piksel/birim oranı 100 → 200. Unity payı `pay × 100 / (ppu × çarpan)` ile
   birime çevirdiğinden 40×100/(200×m) = 20/m, yani **eski değerin aynısı**:
   projedeki kırk kadar elle ölçülmüş `SetSliceScale` değeri ve `UiCornerFit`
   hesabı olduğu gibi geçerli kaldı. Değişen tek şey köşenin keskinliği
   (büyütme 2,4× → 1,2×).
2. **Eş merkezlilik.** Kartın koyu bandı her yerde 20 birim; öyleyse iç yüzeyin
   yarıçapı dıştan TAM 20 eksik olmalı. Dış 44,4 iken iç 40 idi — olması
   gereken 24,4. İç köşe 15,6 birim fazla yuvarlaktı ve köşegen boyunca bandı
   yiyordu: kenarda 20 birim olan bant köşede 8-9 birime iniyordu.
   Yeni `UiKit.SliceScaleFor(yarıçap)` ile ikisi tek sayıdan türetiliyor.

**Doğrulama:** yeni `MenuShell.CreateTabBarPreview` ile önce/sonra 4× büyütülmüş
köşe kırpması.

---

## Q. PROFİL

### Q57 — Avatar isim değiştirme ikonu ✔
Rozet tek düz yeşil daireydi; ekranın her yüzeyinin (kart, ad levhası,
istatistik kutuları) koyu bir kenarı varken o yoktu. Ayrıca eski tasarımdan
kalma, yeri yanlış küçük bir koyu kapsül ("Tip") rozetin sol ortasında bir
LEKE olarak duruyordu.

Yeni rozet: gölge → koyu yeşil bilezik → parlak yüz → yumuşak üst ışık
(`Radial`). Kalem üç katman: koyu kontur, beyaz gövde ve **grafit uç** — yeni
`UiSprites.PencilTip`, `PencilDistance`'ın yalnız uç üçgeni, aynı koordinat
sisteminde üretildiği için gövdenin ucuna kendiliğinden oturuyor. Tek renk
beyaz kalem bu boyutta TİK gibi okunuyordu; şekli tanınır kılan şey ikinci renk.

### Q58 — Seviye dikey ayraç çizgisi ✔
Beyazın %30'u, kartın açık mor yüzeyinde kayboluyordu. Ayraç KOYU tarafa
geçti (`#372A7A`, %85 alfa) ve bir tık kalınlaştı.

---

## R. ÖDÜL / KAZANMA

### R59 — Perfect Coin paneli ✔
**Ölçüm** (`Levels 1-20` 11:31, 592×1280):
| öğe | referans (ekran oranı) | bizde (önce) |
|---|---|---|
| kart | x %5,4-94,3 · y %25,9-79,1 | %4,4-95,4 · %25,8-76,7 |
| "PERFECT!" | y %77,7-84,0 (kartın üstüne BİNER) | %77,2-85,6 (kartın üstünde asılı) |
| "Level 20" | AÇIK MOR PLAKA içinde | plakasız düz yazı |
| "20" | koyu mor YUVARLAK DİKDÖRTGEN | siyaha yakın ELİPS |

Renkler örneklendi: kart yüzü (65,49,192) — bizimkiyle birebir aynı; "Level 20"
plakası yüz (82,63,247)/kenar (45,32,145); ödül plakası yüz (34,21,100)/kenar
(56,41,120).

Ödül plakası `panel_dark` sprite'ının `BadgeDark` ile boyanmış hâliydi. Boyama
ÇARPMA olduğu için koyu bir sprite'ı koyu bir renkle çarpmak siyah veriyor —
projede altıncı kez düşülen tuzak. Prosedürel yuvarlak panel rengi birebir
veriyor.

### R60 — Konfeti revizyonu ✔
**Ölçüm** (`Levels 1-20` 11:26, BLOCKOUT kutlaması; logo bölgesi dışlanıp
bağlı bileşenler ayrıştırıldı, 192 parça):
- genişlik ortancası **12 piksel = ekranın %2,03'ü**, %90'lık dilim 18 px (%3,04)
- yükseklik ortancası 10 piksel (genişliğin %83'ü)
- renk dağılımı: yeşil %32 · mavi %26 · sarı %13 · kırmızı %13 · beyaz %8 · pembe %3

Bizde parçalar %0,83-1,48 arasıydı — **yarı boy**. Eski ölçüm
`menus,powerups,vs.mp4`ten (384×832, BAŞKA bir kutlama sahnesi) alınmıştı;
"referanstan ölçtük" demek doğru referanstan ölçtüğümüz anlamına gelmiyor.
Palet de yedi rengi EŞİT dağıtıyordu ve tonları pastel-parlaktı; artık
referansın altı rengi, ölçülen oranlarda (dizide tekrar ederek).

---

## S. DURAKLAT

### S61 — Sayaç animasyonu ✔
**Doğrulama referansta:** duraklat karesinde (00:04) yeniden başlat ve duraklat
düğmeleri duruyor ama **SÜRE HAPI YOK** — yani gizlenmesi bir süs değil,
referansın davranışı.
Yeni `Juice.SlideY` + `GameplayScreen.SlideTimerAway`: duraklatınca 210 birim
yukarı (0,26 sn, `InCubic`), devam edince geri (0,16 sn, `OutBack`). Dönüş
bilerek daha kısa. `SetActive(false)` de gizlerdi ama oyuncu nereye gittiğini
bilmezdi; yukarı kayınca geri geleceği de anlaşılıyor.

### S62 — Quit butonu taşmış ✔
Kart 1031 birim; `panel_card` görselinin alt dudağı 108 birim, yani iç yüzey
kart-göreli 0.121'de bitiyor. Quit 0.076'da başlıyordu — dudağın **46 birim
İÇİNE** giriyordu. Yeni yerleşim: ayraç 0.512-0.519, Resume 0.330-0.487,
Quit 0.140-0.297. Üçü tek bütçeyi paylaştığı için hepsi birlikte kaydı.

### S63 — Kapat (X) daha koyu ve gölgeli ✔
Ölçüm: dairenin üst kenarı `#FF282A`, alt/yan kenarları `#6E0000`-`#960000`;
çarpı saf beyaz değil krem `#F4E7D6`. Bizimki tek düz `#E5252E` daireydi.
Yeni: gölge → koyu kırmızı bilezik → parlak yüz → krem çarpı.

### S64 — Ses/Müzik/Haptik ikonları ✔
Ölçüm: simgelerin en koyu pikselleri **(26,19,74) = #1A134A**; kartın yüzü
(65,49,192). Yani simgeler yüzeyden KOYU tarafa ayrılıyor. Bizde beyazdılar ve
etiketle aynı katmanda okunuyorlardı.

---

## T. KAPI YERLEŞİMİ

### T65 — Üst kapıların altında duvar payı ✔
**Sebep PARALAKS, yerleşim değil.** Kamera 80° eğimli (dikeyden 10°); yerden
`h` yükseklikteki bir yüzey ekranda `h·tan(10°) = 0,176h` kadar kuzeye kaymış
görünür. Çerçevemiz 0,80 hücre yüksekliğinde:
- çerçevenin üst yüzü 0,141 hücre yukarı kayıyor,
- kapı (0,82) 0,145 kayıyor.

Kapı 6. turda prizma olmaktan çıkıp düz PLAKA olmuştu, yani yan yüzü yok;
altındaki pah bandı açıkta kalıyor. **Ölçüldü: 11 piksel, hücre 122 piksel =
0,09 hücre.** `InwardOverhang` 0,09 → **0,235**; 0,235 − 0,145 = 0,09 hücre
görünür taşma, referansta ölçülen değerin aynısı.

### T66 — Köşe kapıları ve radius ✔
**Bulunan hata:** `BoardFrameMeshBuilder`'ın hücre-listesi yolu dış halkayı
`cornerRadius + thickness` ile üretiyordu; aynı dosyadaki DİKDÖRTGEN yol ise
aynı sayıyı DIŞ yarıçap kabul ediyor. İki yol aynı ayarı iki farklı şey
sanıyordu. Ayardaki 0,32 ekranda **0,84 hücrelik** bir yay demekti.

**Ölçüm (referans, hücre 72,5 piksel):** çerçevenin dış köşe yayı ~14 piksel =
**0,19 hücre** — bizimkinin 4,4'te biri. Köşeye dayanan 2 hücrelik bir kapı,
duvarın çoktan kıvrılıp gitmiş olduğu bir yere düz bir plaka olarak oturuyordu.
Artık `cornerRadius` her iki yolda da DIŞ yarıçap; iç yarıçap ondan türüyor.

---

## U. OYUN İÇİ HUD

**Ölçüm** (`Levels 1-20` 01:20, 592×1280; Baloo 2'de büyük harf ≈ puntonun
%67,2'si — mağaza başlığından ölçüldü):

| yazı | referans büyük harf | ekran oranı | gereken punto | bizde |
|---|---|---|---|---|
| "1040" | 19 px | %1,484 | 42 | 34 |
| "Level 6" | 19 px | %1,484 | 42 | 28 |
| "01:52" | 26 px | %2,031 | 58 | 42 |

### U67 — Süre göstergesi ✔
- **Simge mor ve içi boş.** Referanstaki kadran 30×27 piksellik, İÇİ BOŞ,
  tek renk mor bir halka + iki akrep; en parlak pikselleri **(92,83,180)**
  (aynı karede duraklat çubukları #DED5FF çıktı, yani koyuluk karenin
  tonlamasından değil). Bizde 3B mavi-turkuaz bir ÇALAR SAAT vardı — aynı
  görsel mağazada yardımcı simgesi olarak da kullanılıyor. Yeni
  `UiSprites.ClockFace`.
- **Punto 42 → 58.**
- **Hap referansa göre küçüldü:** x %36,3-64,2 (bizde %30-70), yükseklik 76
  birim (bizde 111). Hap düğmelerin ARASINDA ve onlardan alçak; referansta
  düğmeler 72 piksel, hap 51 piksel.
- Dakika iki haneli ("01:14"), sayaç kıpırdamadan sayıyor.

### U68 — Seviye göstergesi ✔
Punto 28 → 42 (jeton yazısı da 34 → 42; referansta ikisi AYNI puntoda).

---

## V. GÖRSEL İYİLEŞTİRME

### V69 — Perde parıltıları ✔
Üç ayrı hata:
1. **Dağılım.** Konumlar düz `Range(-half, half)` ile atılıyordu; düzgün
   rastgelelik kümelenir. Artık katmanlı (stratified) örnekleme: yüzey
   ızgaraya bölünüyor, her hücreye bir parıltı + hücre içinde sapma. Sayaç
   rozetinin bulunduğu hücreler atlanıyor.
2. **Parlaklık.** Referansta zemin (63,43,182) → parıltı (72,51,204); bizde
   ekranda ORTALAMA (145,141,206), tepe (249,248,248) — beyaz. Sebep: parıltı
   bir KÜP idi, saydam bir küpün ön ve arka yüzleri üst üste harmanlanıyordu
   ve kümelenen parıltılar birbirine biniyordu; %10'luk alfa ekranda %44'e
   çıkıyordu. Tek yüzlü levha + alfa 0.03. **Ölçüldü:** yeni tepe (78,69,189).
3. **Biçim.** 45° döndürülmüş küp bir EŞKENAR DÖRTGEN verir; referanstaki
   içbükey kollu DÖRT UÇLU YILDIZ (iç yarıçap dışın %22'si).

### V70 — Level 50 ok blokları ✔
**Ok mesh'i köşe rengi yazmıyordu.** `BlockOut/Brick` gölgelendirici albedo'yu
`_BaseColor * IN.color` diye hesaplıyor; renk verilmeyen bir mesh'te Unity
beyaz veriyor, yani ok TAM parlaklıkta ve TEK TONDA çiziliyordu. Bloğun
gövdesi ise pişmiş tonlar taşıyor (yüz 0.72, yan 0.86, saplama tepesi 1.0).
**Aynı hata 6. turda KAPIDA çıkmıştı** — o zaman kapı için not düşülmüş ama
aynı gölgelendiriciyi kullanan diğer mesh'ler taranmamıştı.

Yapılanlar: pahlı prizma (taban geniş → eğik omuz → içeri çekik kapak), köşe
renkleri (dip 0.50, omuz 1.00, kapak 0.88), köşeler yuvarlatıldı, baş genişliği
0.205 → 0.235 (yuvarlatma + pah uçlardan pay yiyor).

İki tuzak yol boyunca çıktı:
- **Yay yarıçapı komşu kenarın yarısına eşit olunca iki yay AYNI noktayı
  üretiyor.** Sıfır uzunluklu kenarın dış normali sıfıra bölme demek; offset
  NaN üretip halkayı kendi içine kıvırıyordu.
- **Kaydırılmış halkanın kendi üçgenlemesi çöküyor.** İçbükey omuz çentiği
  0,12 birim derin; içeri kaydırma onu kapatıyor, kulak kırpma kulak
  bulamayınca kalanı YELPAZE ile dolduruyor ve ok sağ ucundan sola genişleyen
  bir KAMA olarak çiziliyordu (ölçüldü: parlak bant solda 49, sağda 25 piksel;
  mesh ise tam simetrikti). Kapak artık TABANIN üçgenlemesini kullanıyor —
  iki halkanın köşe sayısı ve sırası aynı olduğu için üçgen listesi ikisinde
  de geçerli.

---

## Bu turun genel dersleri

1. **Taşmayı önlemek, taşanı kırpmaktan başkadır.** `Truncate` eşiği bir birim
   aşılınca satırın TAMAMINI siler; küçültmek en kötü ihtimalle yazıyı bir tık
   ufaltır.
2. **Bir malzemenin beklediği veriyi vermezsen sessizce düzleşir** — ikinci
   kez. Bir tuzağı bulunca onu yalnız bulunduğu yerde değil, AYNI SÖZLEŞMEYİ
   paylaşan her yerde aramak gerekiyor.
3. **Bir kutunun ortası, GÖRÜNEN kutunun ortasıdır.** Aynı hata bu turda üç
   yerde çıktı: mağaza teklif bandı (N49), OYNA düğmesi (P55), duraklat
   kartının dudağı (S62).
4. **Aynı ayarı iki yerde iki farklı anlamda kullanma.** Çerçeve köşe
   yarıçapı iki kod yolunda iki farklı şey demekti; ikisi de kendi içinde
   tutarlıydı, hata ancak yan yana konunca çıktı (T66).
5. **Sayı doğru olabilir, ekran yanlış olabilir.** Perde parıltısında alfa
   0.10 yazıyordu, ekranda ölçülen etkin alfa 0.44'tü — farkı üreten şey
   geometriydi (V69). Bir görsel değeri, ürettiği PİKSELİ ölçerek doğrula.
6. **Ölçümün alındığı kare, ölçümün kendisi kadar önemli.** Konfeti boyu
   başka bir kutlama sahnesinden ölçülmüştü ve iki katı hatalıydı (R60).
7. **Üç boyutlu bir sahnede "üst üste", ekranda üst üste demek değildir.**
   Kapının iç kenarı dünyada duvarı örtüyordu; ekranda örtmüyordu (T65).
8. **Doğrulanamayan kod bozuk kalır.** Bu turda beş yeni önizleme/denetim
   aracı eklendi; P56'daki köşe bozulması altı tur boyunca ölçülmemişti çünkü
   sekme çubuğu yalnız oynatma kipinde kuruluyordu.

## Uyarı — MCP ile çalışırken

`Unity_RunCommand` kendi parçacığını O ANKİ derlemeye karşı derliyor.
Kaynak dosyayı düzenledikten hemen sonra çağrılan komut **eski kodu** ölçebilir:
bu turda perde parıltısı üç kez "değişmedi" göründü. Kural: düzenlemeden sonra
ÖNCE yalnız `AssetDatabase.Refresh` yapan bir komut, SONRA ölçen komut.
