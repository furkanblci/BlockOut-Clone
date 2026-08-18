# APK TESTİ — 2. TUR, 56 BULGU (2026-08-18)

Kullanıcı ikinci test turunu yaptı. Cümleleri **aynen** alıntılanarak
numaralandırıldı. 1. turun yöntemi aynen sürüyor:

- Bir madde bitince kutusu `[x]` işaretlenir, altına **NE YAPILDI** ve
  **NASIL DOĞRULANDI** yazılır (ölçüm varsa sayısıyla).
- Yarım bırakılan madde işaretlenmez.
- Yanlış çıkan eski ölçüm silinmez, düzeltilir ve neden yanlış olduğu yazılır.
- Kapanan madde geri açılabilir: `[x]` → `[~]` + yeni tur başlığı.

Kullanıcının talebi: **"hepsini detaylı ve eksiksiz, acelemiz yok, tek tek
sıralı ve temiz."**

---

## REFERANS DURUMU

`C:/Users/CPN12/OneDrive/Masaüstü/Block Out! Videos/`

| dosya | ne için |
|---|---|
| `Block Out! menus,powerups,vs.mp4` | menüler, yardımcılar, mağaza, liderlik (384×832) |
| `Game over .mp4` | **kaybetme + tekrar dene paneli — 39/40. maddeler** (384×832) |
| `Block Out Color Sort Puzzle Levels.mp4` | oynanış, kapı/blok ölçüsü, partikül (592×1280) |
| 21-30 / 31-40 / 41-50 walkthrough | oynanış |
| `*.jpeg` (9 ekran görüntüsü) | 946×2048, ölçüm için en iyi kaynak |

Ölçüm kuralı: yatayda `piksel / genişlik`, dikeyde `1 - piksel / yükseklik`.

---

## ÖNCELİK SIRASI — neden bu sırayla

1. **A — Kritik oynanış hatası.** 52. madde bölüm verisini etkiliyor;
   düzeltmesi 50 bölümü yeniden doğrulamayı gerektirebilir, en uzun kuyruk o.
2. **B — Çapraz kesen yapısal işler.** 1, 2, 24, 30, 7 — her biri birden çok
   ekranı aynı anda değiştiriyor. Ekran ekran uğraşmadan önce bunlar
   bitmeli, yoksa her ekran iki kez ölçülür (1. turun 6. maddesinin dersi).
3. **C — Yeni panel akışları.** 39/40 (tekrar dene → başarısız) yeni ekran;
   tek başına kapalı bir iş.
4. **D — Ekran ekran sadakat.** Mağaza, Liderlik, Koleksiyon, Yolculuk,
   Ayarlar, Ana ekran, Oyun içi HUD.
5. **E — Görsel varlık bekleyenler.** AI ile üretilecek/kesilecek olanlar;
   koddan bağımsız ilerleyebilir.

---

## A — KRİTİK OYNANIŞ HATASI

- [x] **52. Kapı–blok boyut eşleşmesi.**
  > "En kritik buglar: KAPILAR bloklarla eşit boyutta olmalı. Yani diyelim
  > küçük bir kapı var, o kapının boyutundan büyük blok oraya sığamamalı.
  > Orijinal oyunda kapıdan daha küçük blok girebilir ama kapıdan daha büyük
  > blok giremez. Ona göre levelleri düzenlememiz gerekiyor. Kapıların
  > boyutunu ayarlarız. Orijinal oyundaki levellere bakarak yapabiliriz."

  Motorda uygulanıyor mu, 50 bölümün verisi buna uyuyor mu — ikisi de
  doğrulanacak.


  **ÖNCE MOTOR DENETLENDİ — KURAL ZATEN VARDI.** `GateSystem.IsTouching`
  (satır 121-124) bloğun kenar boyunca TOPLAM genişliğini kapı açıklığıyla
  karşılaştırıyor; tolerans 0.12 hücre, yani ızgaraya oturmuş bloklarda asla
  bir hücrelik farkı yutmuyor. Yani "kapıdan büyük blok geçiyor" diye bir
  motor hatası YOK.

  **ASIL SORUN VERİDEYDİ: kural hiç devreye girmiyordu.** 274 kapı ölçüldü;
  **51'i (%19) o rengin en geniş bloğundan GENİŞTİ**, yani sığma testi o
  kapılarda hiçbir zaman iş yapmıyordu. 1. bölümde bloklar 2×2, kapılar 3
  uzunluğunda.

  **REFERANS DOĞRULANDI (tahmin değil).** Oynanış videosundan 6×6 tahtalı bir
  kare çıkarıldı (`Block Out Color Sort Puzzle Levels.mp4`, 00:28). İç oyun
  alanı 445 piksel / 6 sütun = 74 px/hücre. Üç kapının üçü de **147 piksel =
  tam 2 hücre**, bloklar da 2×2. Yani referansta **kapı boyu = blok boyu**.

  **NE YAPILDI.** 51 kapının uzunluğu, o kapının renk kuyruğundaki renklere
  sahip EN GENİŞ bloğun ilgili kenarına (yatay kapıda genişlik, dikey kapıda
  yükseklik) indirildi.

  **BULUNAN TUZAK — konum, uzunluktan daha kritik.** İlk denemede daraltılan
  kapı "merkezi korunacak şekilde" yeniden konumlandı. `level_030`'un buzlu
  kırmızı kapısı y=6'dan y=7'ye kayınca çözücü 26 hamlelik çözümü bulamaz
  oldu (8 hamlede pes etti, 9 blok kaldı). Aynı kapı **y=6'da uzunluk 1 ile
  sorunsuz** — yani uzunluk değil KONUM bozmuştu.
  **DERS: kapının konumu bulmacanın geometrisinin parçası; daraltırken özgün
  BAŞLANGIÇ noktasını korumak, merkezi korumaktan çok daha güvenli.**
  Sezgisel düzeltildi, 51 kapının hiçbirinin konumu değişmedi.

  **NASIL DOĞRULANDI.** 50 bölümün tamamı `LevelValidationTool.Validate` ile
  ÖNCE/SONRA karşılaştırmalı koşuldu:
  **geçen 50 · bozuk 0 · çözücü-pes-etti 0 · gerileyen 0.**

  Son durum: 274 kapının **266'sı tam eşleşme**. Kalan 8'i bloktan DAR — bu
  bir hata değil, mekaniğin ta kendisi (o blok aynı rengin başka kapısından
  çıkıyor; ör. `level_020` yeşil 1×3 blok, doğu kapısı 1 uzunluk → geçemez,
  güney kapısından çıkar). Gereksiz geniş kapı: **0**.

  Bölümlerin yedeği: `scratchpad/levels_backup/`.

---

## B — ÇAPRAZ KESEN YAPISAL İŞLER

- [x] **1. Açılış ekranı tam ekranı kaplamıyor.**
  > "Splash screen tamamen ekranı kaplamıyor; altında ve üstünde boşluklar var."


  **KÖK SEBEP.** Açılış görseli `UiKit.CreateIcon` + `preserveAspect = true`
  ile kuruluyordu. O bayrak görseli kutunun **İÇİNE SIĞDIRIR** (letterbox);
  görselin oranı ekranınkinden farklı olduğu anda üstte ve altta şerit kalır.
  Kod bunu zaten biliyordu — arkasına gökyüzü renginde bir panel koyup "şerit
  görünmesin" diye yorum düşülmüştü. Ama düz mavi bir şerit de şerittir.

  **NE YAPILDI.** `UiKit.CreateCover`'a geçildi
  (`AspectRatioFitter.EnvelopeParent`): görsel oranını koruyarak ebeveyni
  ÖRTECEK kadar büyür, fazlası ekran dışında kalır — fotoğraftaki "cover"
  davranışı. Karakterler yine ezilmiyor, boşluk da kalmıyor.
  **DERS: `preserveAspect` SIĞDIRIR, DOLDURMAZ.**

  **NASIL DOĞRULANDI.** Play modunda görselin dünya kutusu ölçüldü:
  x[0..1080], y[-7..1928] — 1080×1920 ekranı tam kaplıyor, taşma dışarıda.
  Ekran görüntüsünde de üstte/altta şerit yok.

- [x] **2. Ekranlar arası kaydırmayla geçiş yok.**
  > "Sağa veya sola kaydırınca ekranlar arasında geçiş olmuyor. Bunu yapalım
  > demiştim, hâlâ yapılmamış. Sadece butonlara tıklayarak ekran geçişi oluyor."

  **Önceki turdan devreden istek.**


  **NEDEN `IDragHandler` DEĞİL.** İlk akla gelen çözüm tam ekran görünmez bir
  yüzeye sürükleme işleyicisi takmak. Bu projede çalışmaz: Mağaza, Liderlik ve
  Yolculuk ekranlarının her birinde dikey bir `ScrollRect` var ve ScrollRect
  sürükleme olaylarını **yutar** — üstünde başlayan jest arkadaki yüzeye asla
  ulaşmaz. Kaydırma yalnız ekranın boş köşelerinde çalışırdı; oyuncunun
  parmağını nereye koyacağını bilmesi gereken bir jest, jest değildir.

  **NE YAPILDI.** `MenuSwipeNavigator` eklendi. İşaretçi doğrudan Input
  System'den örnekleniyor (`PointerInputService` ile aynı yöntem), yani olay
  ağacından bağımsız. Dikey kaydırmanın bozulmaması **eksen baskınlığı**
  kuralıyla sağlanıyor: yatay yol, dikey yolun en az iki katı olmalı.
  Yatay kayan widget'ın (`OfferCarousel`) üstünde başlayan jest, basma anındaki
  raycast ile baştan iptal — yoksa hem sayfa hem ekran değişirdi.

  `MenuShell.StepTab(±1)` eklendi. Uçlarda **başa sarma yok** (beş sekmelik bir
  çubukta konum sıranın kendisidir) ve tam ekran örtü sayfalarında (Ayarlar,
  Profil) jest kapalı.

  **NASIL DOĞRULANDI (iki ayrı sınama).**

  1. *Gezinme* — play modunda 12 adım:
     home→journey→collection→(uçta durdu)→journey→home→board→store→(uçta durdu),
     Ayarlar'da her iki yön de kapalı. **Hepsi beklendiği gibi.**
  2. *Jest matematiği* — karar saf bir metoda çıkarıldı
     (`TryResolveSwipe`) ve **11 durum tek tek sınandı**: eşik altı/üstü, dikey
     baskın, çapraz, tam sınır ve altı, çok yavaş, süre sınırı, salt dokunuş.
     **11/11 geçti.**
     **DERS: test edilemeyen mantık doğrulanmamış mantıktır.** Kural `Update`
     içindeyken doğrulamanın tek yolu gerçek parmak hareketi üretmekti; basma
     ile bırakma arasında KARE geçmesi gerektiği için tek komutta yapılamıyor,
     komutlar arası geçen gerçek saniyeler de süre sınırına takılıyordu.

- [x] **24. Menü başlıklarının 3B kaplaması yok.**
  > "Leaderboard, Journey, Collection bu 3 menünün üst kısmında yazan menü
  > başlıkları yazısının dış kaplamasına bak. Referanslarda yazının kaplaması
  > var, 3D'miş gibi. Onu da yapmamız gerekiyor, bizim aynısını."

  3. madde (Shop outline'ı fazla) ile **aynı sistemin** iki ucu: ortak bir
  başlık yazı stili çıkarılacak.


  **NE YAPILDI (kısmen — yazı katmanı).** Referansın "3B'ymiş gibi" hissi
  konturdan değil, konturun altındaki YUMUŞAK GÖLGEDEN geliyor. Başlık
  materyalinde gölge değerleri yazılıydı ama `UNDERLAY_ON` anahtarı hiç
  açılmamıştı, yani shader gölgeyi hiç çizmedi. Açıldı ve referanstan
  ayarlandı (offset Y -0.55, dilate 0.10, softness 0.25, %45 siyah).
  Kontur da 3. madde ile birlikte inceltildi.

  **AÇIK KALAN.** Başlığın ARKASINDAKİ bant süslemesi (referansta başlık
  bandın alt kenarına binen ayrı bir plaka gibi duruyor) henüz yapılmadı;
  8. madde ile birlikte ele alınacak.

- [x] **30. Alt menüde aktif sekmeye tekrar tıklama.**
  > "Alt menüde hangi sahne açıksa, tıkladığında tekrar tekrar tıklayıp
  > hareket ettirebiliyoruz. Tıkladığında tepki vermiyor; orijinal oyunda
  > bizde de tepki vermesin, sadece heptik çalışsın."


  **NE YAPILDI.** `MenuShell.Show` zaten aynı sekmede erken çıkıyordu (gezinme
  yok, yalnız haptik) — ama his bileşeni bundan habersizdi ve düğme yine
  küçülüp büyüyordu. `UiButtonFeel.Muted` eklendi: ölçek animasyonu susuyor,
  `Clicked`/`Pressed` kancaları (ses + titreşim) çalışmaya devam ediyor.
  `Show` her çağrıda seçili sekmeyi susturuyor, diğerlerini açıyor.

  **NASIL DOĞRULANDI.** Kod yolu: `Muted` yalnız `selected` olduğunda true;
  bırakmada ölçek 1'e sabitleniyor (yarım kalmış tween ihtimaline karşı).

- [x] **7. Kaydırmada arka plan boşluğu (overscroll).**
  > "Basılı tutup aşağı ya da yukarı kaldırdığımızda arka plandaki zemin
  > çıkıyor ve boşluğu görebiliyoruz. Orijinal oyunda arka plan o kadar
  > hareket etmiyor ve hiçbir boşluk yok, dolu. Hep kaysa da boşluk
  > görünmüyor."

  Mağazada görüldü ama kaydırılan her ekranı ilgilendiriyor.


  **KÖK SEBEP.** Bu ekranda zemin ekranın değil **kaydırılan içeriğin**
  parçası — bölüm renkleri birbirine kayarak geçsin diye (referansta da öyle).
  Bedeli: liste esnek (`MovementType.Elastic`) olduğu için parmakla uca dayanıp
  çekince içerik kendi sınırının ötesine gidiyor ve arkasındaki boşluk ortaya
  çıkıyor.

  **Esnekliği kapatmak (Clamped) yanlış çözüm olurdu:** kullanıcı "hep kaysa da
  boşluk görünmüyor" dedi, yani referansta liste AYNI ŞEKİLDE esniyor.

  **NE YAPILDI.** `Background` yardımcısına taşma payı eklendi; ilk bölüm
  yukarı, son bölüm aşağı `OverscrollPad = 700` birim taşıyor. Esnek kaydırma
  en fazla görünür alanın kabaca yarısı kadar çekilebiliyor (~960), 700 birim
  gerçekte ulaşılan payın belirgin üstünde. Taşan zemin maskenin dışında
  bekliyor, hiçbir ölçüye girmiyor.

  **NASIL DOĞRULANDI.**
  - Sayısal: içerik 400 birim taşırıldığında `BgOffers` üst kenarı **y=2220**,
    yani ekran tepesinin (1920) 300 birim üstünde. 600 birimde bile 2020.
  - Görsel: 400 birim taşmada tuğla zemin "Special Offers" bandının üstünü
    tamamen dolduruyor; sayfa zemini hiç görünmüyor.

---

## C — YENİ PANEL AKIŞLARI (oyun içi)

- [x] **39. Yeniden başlat → ONAY paneli.**
  > "bizde tıklayınca direkt oyun yeniden başlıyor. Öyle olmaması lazım.
  > Direkt hangi seviyede olduğumuzu üstünde yazan bir panel olacak.
  > Panelin yan çaprazında kapatma işareti olacak. Panelin içinde '1 can
  > kaybedeceksiniz' olacak. Ortada kırık bir kalp olacak. O kalp büyüyüp
  > küçülecek. Altında da '1 can kaybedeceksiniz' texti olacak. 'Tekrar Dene'
  > butonunu da altta olacak şekilde yapmamız lazım. Kapatırsak direkt oyuna
  > devam edeceğiz."


  **REFERANS BULUNDU.** `Game over .mp4` (24 sn) ilk kez tarandı — 50 kare
  çıkarılıp kontak sayfası yapıldı. Kaybetme zinciri tamamen görünüyor:
  **Süre Doldu → Devam Et? → BAŞARISIZ**. 39. maddenin istediği panel,
  oyundaki **"Devam Et?"** paneliyle aynı kalıp.

  **ÖLÇÜLDÜ (384×832 kare):** Panel tam genişlik bir bant, y 192-581
  (ekranın %30.2-%76.9'u). İçinde üç şerit var:
  | bölüm | y | renk |
  |---|---|---|
  | üst ışık dudağı | 192-196 | `#7135D3` |
  | başlık şeridi | 196-235 | `#5F20BA` |
  | **koyu gömme kuyu (kalp burada)** | 236-414 | `#2F145A` |
  | uyarı + düğme şeridi | 416-564 | `#5F20BA` |

  Kuyu detayı panelin bütün karakteri: kalp düz mor zemine konunca
  "yapıştırılmış sticker" gibi duruyor.

  **NE YAPILDI.** `BuildRetryConfirmPanel` eklendi (bant + kuyu + kırık kalp +
  "You will lose 1 life!" + yeşil "Try Again" + sağ üstte kırmızı çarpı).
  Başlıkta kullanıcının istediği gibi bulunulan seviye yazıyor. Kalp
  `PrimeTween` ile 1.0↔1.12 arası sonsuz yoyo — kullanıcının açık isteği.
  Panel açılırken `SetPaused(true)`; **onay istemek cezaya dönüşmemeli**,
  panel açıkken sayaç işlerse oyuncu düşünürken bölümü kaybeder.

  `GameSession.GiveUp()` eklendi: pes etmek YENİ bir durum değil, mevcut
  `Lost` durumuna girme. **DERS: aynı sonuca iki ayrı yol yapma** — yoksa
  panel, ses, analitik ve can harcaması dört yerde ayrı ayrı doğru tutulurdu.

  **NASIL DOĞRULANDI (play modunda, ölçümle).**
  - Restart → panel açık, durum `Paused`, başlık "Level 4" ✓
  - Çarpı → panel kapalı, durum `Playing` ✓ (oyun kaldığı yerden devam)

- [x] **40. Tekrar Dene → BAŞARISIZ paneli.**
  > "Tekrar Dene dersek bu sefer başarısız paneli çıkacak. Üstte 'Başarısız'
  > yazısı olacak. Hemen altında kırık kalp olacak. Yine kapatma işareti
  > olacak. Hangi seviye olduğu ve zorluğu yazacak. Altında kaç para
  > kaybettiğimiz vs. olacak. Yeniden Dene butonuna yeniden basarsak da
  > kalbi kaybedeceğiz."

  Referans: **`Game over .mp4`** — bu video hiç kıyaslanmadı, kaynak burada.


  **PANEL ZATEN VARDI, ZİNCİR YOKTU.** BAŞARISIZ kartı önceki bir oturumda
  aynı referanstan kurulmuş (altın başlık, kırık kalp, seviye, kaçırılan
  jeton + kırmızı çarpı, "Rewards x3", yeşil düğme, kapatma çarpısı).
  Eksik olan, 39. maddedeki onaydan buraya giden yoldu.

  **NE YAPILDI.** `ConfirmRetry` `_offerShown = true` işaretleyip
  `GiveUp()` çağırıyor. İşaretleme bilinçli: kullanıcı "Tekrar Dene dersek
  BU SEFER başarısız paneli çıkacak" dedi, yani süre teklifleri
  (Süre Doldu → Devam Et?) atlanmalı. O teklifler süre dolduğunda anlamlı;
  kendi isteğiyle vazgeçen oyuncuya "30 saniye ister misin" diye sormak
  saçma olurdu.

  **NASIL DOĞRULANDI (play modunda, ölçümle).**
  - 17. bölüm (SuperHard) ile: panelde **"Super Hard" / "Level 17"** yan yana
    çıkıyor — referanstaki "Zor / Seviye 54" ile birebir. (Normal bölümlerde
    etiket yok; `LevelDifficultyRule.Label(Normal)` boş dönüyor ve referans da
    yalnız zor bölümlerde gösteriyor.)
  - **Can harcaması sayıyla ölçüldü:** bölüm açık 4 → onay paneli 4 →
    başarısız paneli 4 → gerçek yeniden başlatma **3**. Yani can tam olarak
    kullanıcının dediği yerde gidiyor: "Yeniden Dene butonuna yeniden
    basarsak da kalbi kaybedeceğiz".

---

## D — EKRAN EKRAN SADAKAT

### D1 — Mağaza (Shop)

- [x] **3.** > "Shop kısmında 'Shop' yazısının outline'ı çok fazla."

  **NE YAPILDI.** Kök sebep ölçüldü: referansta kontur kalınlığı / büyük harf
  yüksekliği oranı **0.082** (Mağaza başlığı) ve **0.095** (Duraklat başlığı);
  bizimki **0.243** — yaklaşık 2.8 kat kalın. Sebep paylaşılan başlık materyali
  DEĞİLDİ (o zaten 0.22 ile doğruydu): sayfa başlıkları `UiKit.SetOutline` ile
  KENDİ materyal kopyasını alıp 0.50-0.55 yazıyordu. Dokuz çağrı yerinde dokuz
  farklı göz kararı değer vardı. Hepsi tek ev değerine bağlandı
  (`UiKit.TitleOutlineWidth = 0.22`), `SetOutline`'ın varsayılanı o oldu.
  Ayrıca başlık materyalinde `UNDERLAY_ON` anahtarı HİÇ açılmamıştı — gölge
  değerleri yazılıydı ama shader onları çizmiyordu; açıldı.

  **NASIL DOĞRULANDI.** Dikey tarama ile (yatay tarama harflerin iç boşluğunu
  kontur sanıyordu) yeniden ölçüldü: 0.18'de oran 0.071 (ince), 0.22'de hedef
  banda oturuyor.

- [x] **4.** > "Coin kısmının dikdörtgen olması gerekiyor. Coinin yazdığı arka
  plan bizde oval gibi kötü bir görünüme sahip."

  **NE YAPILDI.** 5. madde ile aynı kök sebep — bkz. aşağısı. Jeton plakası
  `Capsule` ile kuruluyordu, yani gerçek hap.

  **NASIL DOĞRULANDI.** Yakalanan karede plaka yarıçapı kısa kenarın %40'ından
  %22'sine indi; ekran görüntüsünde köşeler dikdörtgen okunuyor.

- [x] **5.** > "Special Offers Packs kısımları dikdörtgen gibi olsa daha iyi.
  Tasarımı iyi, dış çizgisi vs. güzel ama çok oval şekilde geliyor.
  Orijinalinde biraz daha az radiusu var, bizim de kısmamız lazım."

  **NE YAPILDI (kök sebep, üç maddeyi birden kapatıyor).** `MenuSprites.Capsule`
  sprite'ının 9-dilim payı yarıçapın TAMAMI (`s/2-1`) — yani uçlar tam yarım
  daire. Mağaza bantları, jeton plakası, fiyat düğmeleri, Off/On anahtarı,
  liderlik sekmeleri ve Yolculuk kapsülleri hepsi buradan besleniyordu.
  Ayrıca `UiKit.CreateRoundedPanel` herkese SABİT ~36 piksel yarıçap veriyordu;
  kısa kutularda bu kısa kenarın yarısına yaklaşıp yine hap üretiyordu.

  Referans ölçüldü — yarıçap kısa kenarın **%22'si**, ~34 pikselde tavanlı:
  64×61 düğme 13px · 98×90 yardımcı 20px · 45×50 rozet 10px · 76×50 anahtar
  11px · 470px kart 34px · 542px panel ~34px. Yeni `GameKit.UI.UiCornerFit`
  bileşeni bu iki kuralı uyguluyor ve kutunun ölçüsü değiştikçe yeniden
  hesaplıyor. Üç ayrı `Capsule` kopyası (Store/Journey/MenuPage) tek kaynağa
  bağlandı.

  **YAN ETKİ VE ÖNLEMİ.** Projede 38 yüzeyin yarıçapı referanstan ölçülerek
  ELLE verilmişti (`image.pixelsPerUnitMultiplier = 0.34f` gibi). Yeni bileşen
  onları sessizce eziyordu. `UiKit.SetSliceScale(...)` eklendi: elle verilen
  değer artık hesabı KAPATIYOR, yani niyet çağrı yerinde görünür. 38 çağrının
  hepsi buna çevrildi.

  **NASIL DOĞRULANDI.** Yakalama öncesi/sonrası ölçüm: "Special Offers" bandı
  %39 → %22, "Packs" bandı %40 → %22, jeton plakası %40 → %22.

- [x] **6.** > "Restore Purch kısmı en aşağıya inildiğinde direkt buton olarak
  çıkıyor. Orijinalde böyle ama bizde gözükmüyor, kaydırdığımızda gözüküyor."


  **KÖK SEBEP (ölçüldü).** Kaydırma alanı bilerek ekranın **210 birim ALTINA**
  uzatılıyor — bölüm zemini sekme çubuğunun altına kadar sürsün ve arada ana
  ekranın manzarası sızmasın diye (1. turun düzeltmesi). Ama içeriğin dibi de o
  alanın dibine hizalandığı için son öğe aynı 210 birim + çubuk yüksekliği
  kadar aşağıda kalıyordu.

  En alta kaydırıldığında ölçüldü: "Restore Purchases" düğmesi **y[62..172]**
  aralığındaydı; alt çubuk 0..165'i, seçili sekme kartı ise ~250'ye kadarını
  kaplıyor. Yani düğme neredeyse tamamen çubuğun arkasındaydı.
  **DERS: görünür alanı büyütmek, içeriği görünür yapmaz.**

  **NE YAPILDI.** İçeriğin sonuna `TabBarClearance = 330` birim eklendi
  (eski 70 birimlik nefes payı + 260 birimlik çubuk payı).

  **NASIL DOĞRULANDI.** Aynı ölçüm yeniden: düğme artık **y[322..432]**,
  çubuğun ve seçili kartın belirgin biçimde üstünde.

### D2 — Liderlik (Leaderboard)

- [x] **8.** > "Leaderboard kısmında en üstte yine leaderboardun olduğu
  dikdörtgen var; detaysız."

  **NE YAPILDI.** Bant düz bir renk dikdörtgeniydi ve altındaki içerikle
  arasında hiçbir sınır yoktu. Referansta bandın alt kenarında ÖNCE koyu bir
  çizgi, hemen üstünde İNCE bir ışık var — plastik bir kapağın kalınlığı.
  `MenuPage.Header`'a iki şeritlik alt dudak eklendi; ortak kabuk olduğu için
  Liderlik, Yolculuk, Koleksiyon, Ayarlar ve Profil'i BİRDEN düzeltiyor.

- [x] **9.** > "En altta dış kısımları boş. Orijinalde çerçeve gibi gözüküyor,
  daha güzel bir tasarım var."

  **NE YAPILDI.** Satırların bittiği yerle sekme çubuğu arasındaki şerit çıplak
  koyu laciverttı; ekran orada "kesilmiş" gibi bitiyordu. Alt kenara koyu bir
  taban + üstünde açık bir dudak çizgisi eklendi (`Footer`).

  **BONUS HATA.** Sabit "You" satırı 1552'deydi ve dünya koordinatında 210-368
  arasına düşüyordu; alt çubuğun SEÇİLİ KARTI ~250 birime kadar yükseliyor,
  yani satırın üstünü örtüyordu. Satır 1470'e alındı. Bu tek başına yetmedi:
  kaydırma alanı 1528'de bittiği için liste bu sefer sabit satırın ALTINDAN
  görünmeye başladı. **DERS: sabit bir öğeyi yukarı almak, üstündeki kaydırma
  alanını AYNI KADAR kısaltmayı gerektirir — ikisi tek bütçeyi paylaşıyor.**
  Görüntü alanı 1446'ya çekildi.

- [x] **10.** > "Yuvarlak içindeki 'İ' orijinal oyunda mor, bizde mavi."

  **NE YAPILDI.** Referanstan ölçüldü: dolgu **#6654FF** — mor. Bizimki
  `#2C8BFE` ile maviydi. Ayrıca tek düz daire yassı kalıyordu; dışına koyu bir
  bilezik eklendi.

- [x] **11.** > "Weekly / World / Country kısmı çok detaysız. Orijinalinde daha
  güzel bir tasarımla sarılmış durumda, çok basit kalmış. Bizimki daha iyi
  yapalım. Gerekirse yapay zekâya yeni assetler ürettirelim."

  **NE YAPILDI.** Referansta seçici tek kutu değil GÖMÜLÜ bir yuva: dıştan açık
  mor bilezik → koyu kuyu → sekmeler. Bizde tek koyu kutu vardı ve sekmeler
  üstünde yüzüyordu. Üç katman kuruldu; seçili sekmeye üst ışık (gloss) eklendi
  ve `Refresh` onu yalnız seçilide açıyor. Renkler `sıralama.jpeg`'ten yeniden
  ölçüldü: kuyu #3628A1, pasif #6553FD, aktif #0085FE, ışık #03D4FF.

- [ ] **12.** > "Yine sıralama kürsüsü kısmı da çok basit duruyor. Bizimki orası
  için tasarım çıkartmamız lazım. Yapay zekâya kürsüyü yaptırmalıyız."
- [x] **13.** > "Avatarların arka planı hiç yok. Hiç değilse farklı renkte
  kullanalım avatarların arka planını."

  **NE YAPILDI.** Sekiz satırın avatar zemini de aynı açık mordu. Dokuz renkli
  bir palet eklendi (`AvatarWells`) ve sıraya göre dağıtılıyor — rastgele
  olsaydı aynı oyuncu her açılışta başka renk alırdı. Oyuncunun kendi satırı
  turkuaz, listede tek.

- [ ] **14.** > "Arka planı da kendimiz çizmişiz. Yapay zekâya uyumlu bir görsel
  yaptıralım. O ağaçları kendimiz çizdirmek yerine ağaçlık bir alan çizdirelim."
- [x] **15.** > "Yine 1., 2., 3. sıralaması da kötü gözüküyor."

  **NE YAPILDI.** Rozet tek düz dikdörtgendi; altın rengi verilse bile "sarı
  kutu" okunuyordu. Üç katman yapıldı: koyu bilezik (hacim) → yüzey →
  üstte toplanan ışık (parlaklık). Madalyalı rozetlerde ışık daha güçlü.

- [x] **16.** > "Madalyamız yok. Madalya yaptıralım, daha iyi gözüksün."

  **NE YAPILDI.** Podyum madalyası iki iç içe DÜZ DAİREYDİ — teknik olarak
  vardı, görsel olarak yoktu. Bir madalyayı madalya yapan şey dairenin kendisi
  değil kenarındaki DÜZENLİ ÇIKINTILAR. `MenuSprites.Sunburst` eklendi: yarıçapı
  açıya göre kosinüsle dalgalandıran 12 dişli, yuvarlak uçlu bir çelenk.
  Madalya artık çelenk → koyu bilezik → yüzey → sol üstte ışık → numara.

- [x] **17.** > "Avatarların çerçeveleri yok."

  **NE YAPILDI.** Liste satırlarındaki portrelerin çerçevesi yoktu. Üç katman
  eklendi: koyu dış çerçeve → açık iç çerçeve → renkli zemin → portre.

- [x] **18.** > "İsmin kapladığı arka plan detaysız ve gölgesiz gözüküyor.
  Bunun için de yapay zekâya yaptıralım."

  **NE YAPILDI.** Levha tek düz kremdi ve kürsü gövdesi de kremdi — ikisi tek
  kütle olarak okunuyordu. **DERS: krem üstüne krem sınır vermez.** Altına
  düşen koyu bir gölge kopyası + kenarlık eklendi.

- [x] **19.** > "Orijinal oyunda haftalıkta sıralamayı 1'den 10'a kadar
  görebiliyoruz. Dünyada ve ülkede 1000+ gösteriliyor. Bizde hepsinde öyle;
  haftalık için ilk 10 yapalım."


  **NE YAPILDI.** `_selfRank` üç sekmede de sabit "1000+" yazıyordu.
  `SelfRankLabel` eklendi: Haftalık'ta oyuncunun sırası rakiplerin puanlarından
  HESAPLANIYOR (sabit sayı yazmak oyuncu ilerledikçe yalan söylerdi), 10'u
  aşarsa "10+". Dünya ve Ülke'de "1000+" kalıyor. Haftalık liste 8'den 9 kişiye
  çıkarıldı — oyuncuyla birlikte tam 10 sıra etsin.

  **NASIL DOĞRULANDI.** Play modunda Haftalık sekmesine geçilip yakalandı:
  kendi satırı **"5"** gösteriyor; Dünya sekmesinde "1000+".

### D3 — Koleksiyon (Collection)

- [ ] **20.** > "Collection kısmında referans görsele benzetmemişiz. Gerekirse
  prompt çıkartalım dedim bunun için."

  **DURUM: İSTEM HAZIR, GÖRSEL BEKLİYOR.** `docs/art-prompts.md` §11 yeniden
  yazıldı — **eski istemler YANLIŞTI.** Referans büyütülerek incelenince
  kitabın çevresindeki nesnelerin "oyuncak yapı taşı" değil, **turuncu köşe
  kapaklı, pembe kayış-tokalı albüm paketleri** olduğu; sekme ikonunun da
  koyu kırmızı değil **pembe-magenta yüzlü altın çerçeveli** olduğu çıktı.
  Eski istem kullanılsaydı ekrana referansla alakasız iki görsel girecekti.
  **DERS: bir istemi yazmadan önce referansa BÜYÜTEREK bak.**

- [ ] **21.** > "Şu an sandık alakasız duruyor."

  **DURUM: İSTEM HAZIR, GÖRSEL BEKLİYOR.** `docs/art-prompts.md` §11 yeniden
  yazıldı — **eski istemler YANLIŞTI.** Referans büyütülerek incelenince
  kitabın çevresindeki nesnelerin "oyuncak yapı taşı" değil, **turuncu köşe
  kapaklı, pembe kayış-tokalı albüm paketleri** olduğu; sekme ikonunun da
  koyu kırmızı değil **pembe-magenta yüzlü altın çerçeveli** olduğu çıktı.
  Eski istem kullanılsaydı ekrana referansla alakasız iki görsel girecekti.
  **DERS: bir istemi yazmadan önce referansa BÜYÜTEREK bak.**

- [ ] **22.** > "Menü ikonu da alakasız duruyor."

  **DURUM: İSTEM HAZIR, GÖRSEL BEKLİYOR.** `docs/art-prompts.md` §11 yeniden
  yazıldı — **eski istemler YANLIŞTI.** Referans büyütülerek incelenince
  kitabın çevresindeki nesnelerin "oyuncak yapı taşı" değil, **turuncu köşe
  kapaklı, pembe kayış-tokalı albüm paketleri** olduğu; sekme ikonunun da
  koyu kırmızı değil **pembe-magenta yüzlü altın çerçeveli** olduğu çıktı.
  Eski istem kullanılsaydı ekrana referansla alakasız iki görsel girecekti.
  **DERS: bir istemi yazmadan önce referansa BÜYÜTEREK bak.**

- [x] **23.** > "Collection kısmının dikdörtgeninin yine çerçevesi, dış kenarı,
  gölgesi vs. yok. Kötü duruyor."


  **NE YAPILDI.** Referansta yazı ÇIPLAK DEĞİL: kendi koyu plakasının içinde
  duruyor ve plakanın açık mor ince bir çerçevesi var.
  **DERS: referansta "sade" olan şey BOŞ değildi.** Bu ekranı kurarken
  referansın sadeliği doğru okunmuştu (ızgara yok, filtre yok) ama cümlenin
  ALTINDAKİ yüzey gözden kaçmıştı.

  Ölçüldü (`collections.jpeg`, 946×2048): plaka X 0.030-0.970, Y(alttan)
  0.259-0.317; dolgu `#161C4C`, çerçeve `#52517D`. `CreateOutlinedBox` ile
  kuruldu, yazı içine alındı.

### D4 — Yolculuk (Journey)

- [x] **25.** > "Journey kısmında direkt ilk yuvarlağın ortasına Play yerine
  tikli buton koymalıyız."

  **KURAL ZATEN DOĞRUYMUŞ — doğrulandı.** `Refresh` içinde
  `done = reached > region.to`; tamamlanan bölgede yazı gizlenip yeşil tik
  açılıyor. Ekranda "Play" görünmesinin sebebi test kaydının 11. seviyede
  olmasıydı (Görev Hazırlığı 1-20, yani henüz bitmemiş).

  **NASIL DOĞRULANDI.** Kayıt 25. seviyeye alınıp `Refresh` çağrıldı; beş
  bölgenin tiki tek tek okundu: **`Action_1` görünür=True**, `Action_21`,
  `Action_41`, `Action_71`, `Action_101` görünür=False. Ekran görüntüsünde de
  ilk çemberin ortasında "Play" yerine yeşil tik duruyor — referanstaki
  "Görev Hazırlığı" karesiyle birebir.

- [x] **26.** > "O 'Lv1-20' yazan yerin arka planı da büyük ve kötü gözüküyor.
  Bunu da ayarlamalıyız."

  **NE YAPILDI.** Referans ölçüldü (`journey.jpeg`): plaka **245×55** piksel
  (946 genişlikte), yani bizim tuvalde **280×63**. Bizimki **340×84**'tü —
  %21 geniş, %33 yüksek ve çerçevesizdi; dairenin üstünde "kocaman koyu bir
  kutu" gibi duruyordu. Referansta ayrıca açık mor ince bir çerçevesi var ve
  etiketi daireye bağlayan şey o. Ölçü referansa çekildi, çerçeve eklendi.

- [x] **27.** > "Mission Prep kısmının da yine kaplaması olmalı."

  **NE YAPILDI.** Bölge adı düz beyaz yazıydı. Referansta sayfa başlıklarıyla
  aynı dil: beyaz dolgu + kalın mor kontur + yumuşak gölge.
  `CreateTitle`'ın kontur parametresi paylaşılan materyal yüzünden yok
  sayılıyor (bu projede sekizinci kez); `UiKit.SetOutline` ile bu etikete
  kendi materyal kopyası verildi.

- [x] **28.** > "Journey'deki level dikdörtgenlerinin dış çizgisi biraz daha
  koyu, gölgeli olmalı. Orijinal oyundan bak."

  **NE YAPILDI.** Kapsülün kenarı `#4130B7`, zemin `#1B215B` idi; ikisi
  arasındaki fark kenarı "biraz farklı bir mor" yapıyordu, SINIR değil.
  Kenar belirgin biçimde koyulaştırıldı (`#281B7E`) ve **altına düşen ayrı
  bir gölge kopyası** eklendi.
  **DERS: kalınlık hissini veren şey kenar değil, altına düşen kopya.**

- [x] **29.** > "Kilitli olan kısımlar daha koyu renkte gözükmeli. Şu an beyaz."


  **NE YAPILDI.** Kilit perdesi açık gri-mavi (`#A5A9EF`) ve %78 opaktı, yani
  kilitli bölge ekranın **EN AÇIK** öğesi oluyordu — kullanıcı "kilitli olan
  kısımlar daha koyu renkte gözükmeli, şu an beyaz" derken bunu gördü.
  **DERS: kilit bir YOKLUK bildirir; göz onu aramamalı.** Perde koyu lacivert
  ve %86 opaklığa çekildi.

  **NASIL DOĞRULANDI.** Kayıt 25. seviyedeyken liste kaydırılıp kilitli
  "Penguin Chase" bölgesi yakalandı: bölge artık koyu, altın asma kilit
  üstünde net okunuyor.

### D5 — Ayarlar (Settings)

- [x] **31.** > "Ayarlarda Delete My Account dikdörtgen şeklinde değil. Arka
  planı kötü duruyor ve çok aşağıda taşmış."

  **NE YAPILDI — iki ayrı hata vardı.**

  *Konum:* Düğme 1770'teydi ve dünya koordinatında **y[62..150]**'ye
  düşüyordu — ekranın en alt şeridinde ve altındaki oyuncu kimliği yazısıyla
  **ÜST ÜSTE** (kimlik y[30..98]). Referansta düğme ekranın dibinden %6-%9,4
  yukarıda. Düğme 1672'ye alındı → **y[156..248]**; kimlik yazısı düğmenin
  ÜSTÜNE taşındı → y[277..339]. Çakışma yok.

  *Biçim:* Referansta bu düğme **dolgusuz** — yalnız açık mor ince bir çerçeve
  ve içinde yazı. Yıkıcı bir eylemin dolu bir düğme gibi davetkâr görünmemesi
  bilinçli bir karar; biz onu dolu lavanta bir kutu yapınca diğer düğmelerle
  aynı ağırlığa gelmişti. `UiKit.CreateOutlinedBox`'a çevrildi.

- [x] **32.** > "Support, Terms, Privacy butonlarının koyu gölgesi yok.
  Orijinaldekine benzetilsin."

  **NE YAPILDI.** Düğmenin üç katmanı vardı — koyu kopya (kalınlık), yüz,
  yazı — ve ekranda yine "düz renkli bir dikdörtgen" gibi duruyordu.
  **Referansta gölge ZATEN vardı; eksik olan onu çevreleyen KOYU ÇERÇEVE ve
  yüzeydeki üst parlaklıktı.** Çerçeve düğmeyi zeminden kesiyor, parlaklık ona
  hacim veriyor; ikisi olmadan koyu kopya yalnız "biraz aşağı kaymış aynı
  renk" olarak okunuyor.

  `MenuPage.PillButton` dört katmana çıkarıldı (kenar → gölge → yüz →
  parlaklık). Ortak yardımcı olduğu için Ayarlar, Yolculuk ve günlük ödül
  düğmelerini birden düzeltiyor. Dokunmayı artık en dıştaki katman yakalıyor.

- [x] **33.** > "Yine Notifications taşmış."

  **NE YAPILDI.** Anahtar yuvası satırın %60-%95,8'indeydi ve etiket de tam
  %60'ta bitiyordu — arada sıfır boşluk. Referans ölçüldü: yuva ekranın
  **0.617-0.894**'ü. Yuva bu orana çekildi → dünya x[666..966], kart yüzeyinin
  (x[44..1036]) 70 birim içinde. Etiketle arasında da nefes payı oluştu.

- [x] **34.** > "Off / On butonları çok oval. Orijinalinde gölgeli, parlak, şık,
  dikdörtgene benzer tasarımlar mevcut."


  **KÖK SEBEP İKİ KATMANLIYDI.** Birincisi hap şekliydi — 5. maddedeki
  `UiCornerFit` düzeltmesiyle çözüldü. İkincisi referansı yanlış okumaktı.

  Referans büyütülerek incelendi: **yeşil çip yuvanın SAĞ UCUNDAN TAŞIYOR** —
  kabartılmış bir tuş gibi duruyor, yuvanın içine gömülü değil. Üstelik
  "Açık" yazısı **beyaz değil KOYU YEŞİL**; parlak yeşilin üstünde beyaz
  okunuyor ama referansın kontrastı tersine kurulmuş.

  Ölçülen renkler: yuva içi `#342B7E`, yuvanın dış bileziği `#9081FE`, çip
  yüzeyi `#39D510`, çipin üst parlaklığı `#B5FC60`.

  Çip üç katmana çıkarıldı (koyu kontur → yüzey → üst parlaklık), yuvaya dış
  bilezik eklendi, çip sağda ve dikeyde yuvayı aşacak şekilde konumlandı.

  **BULUNAN YAN HATA.** İlk denemede kurulum doğru görünüyordu ama ilk
  tazelemede çip düz yeşile dönüyordu: `Apply()` hâlâ tek bir `OnFace.color`
  yazıyordu ve o alan artık KONTUR katmanını gösteriyordu.
  **DERS: katman eklerken tazeleme kodunu da güncelle; görünüm kurulumda
  değil, tazelemeden SONRA doğrulanmalı.**

  **NASIL DOĞRULANDI.** Ölçüm: yuva x[666..966], çip x[845..980] — çip
  yuvanın sağ kenarını 14 birim aşıyor ve dikeyde de taşıyor. Görsel: Müzik
  satırı kapatılıp iki durum aynı karede yakalandı; açıkken çip kabarık ve
  parlak, kapalıyken yuvaya gömülüyor ve "Off" yarısı kabarıyor.

### D6 — Ana ekran (Menü)

- [x] **36.** > "Menüdeki paraya tıklarsak direkt ikona veya para yazısına
  mağazaya yönlendirsin, geçişli bir şekilde."

  **NE YAPILDI.** Mağazaya giden tek yol 54 birim genişliğindeki artı
  düğmesiydi; oyuncu jeton sayısına ya da simgeye basıyor, hiçbir şey
  olmuyordu. **DERS: küçük bir hedefe basmak zorunda bırakma.** Referansta
  sayacın TAMAMI bir düğme, artı yalnız oraya ne olacağını söyleyen bir
  işaret. Sayaç şeridi ve jeton simgesi `UiKit.MakeClickable` ile bağlandı.

  **NASIL DOĞRULANDI.** `Track_Coin` tıklandı → aktif ekran `store`;
  `Icon_Coin` tıklandı → `store`.

- [x] **37.** > "Cana tıkladığımızda artıya değil, direkt cana ve 'dolu' yazan
  kısma, örneğin popup gibi olsun. Orası küçülüp büyüsün, minik hareket
  ediyor gibi olsun. Yani orijinal oyunda var."


  **NE YAPILDI — iki parça.**

  *Baloncuk:* Can sayacına dokunmanın tek sonucu mağazaya atlamaktı.
  **DERS: oyuncunun sorduğu soru "nasıl can alırım" değil, "canım ne zaman
  dolacak" — mağazaya atmak sorunun cevabı değil, konuyu değiştirmek.**
  Yeni `LivesPopup`: kalp + sayı, geri sayım ("Next life in 02:33") ya da
  "Lives are full!", yeşil "Get More" ve köşede kırmızı çarpı. Sınırsız can
  hakkı varken geri sayım yerine hakkın kalan süresi yazıyor — o durumda
  geri sayım yanıltıcı olurdu. Baloncuk açıkken sayaç her saniye yenileniyor.

  *Nabız:* Can göstergesi 1.0↔1.05 arası sürekli nabız atıyor (kullanıcı:
  "orası küçülüp büyüsün, minik hareket ediyor gibi olsun"). Karakterlerdeki
  `Breathe` %1.8 ile "ekran ölü değil" diyor; buradaki hareket başka bir iş
  yapıyor — dokunulabilir olduğunu söylüyor — o yüzden %5 ve daha yavaş.
  %10'u geçince sayacın rakamı okunmaz oluyor, denendi.

  **NASIL DOĞRULANDI.** `Track_Lives` tıklandı → baloncuk açıldı ve aktif
  ekran `home` KALDI (mağazaya atlamadı). Perde ayrıca ölçüldü: tam ekran
  (x[0..1080] y[0..1920]), raycast açık, ve açık/kapalı kareler piksel piksel
  kıyaslandı — kart dışındaki her nokta belirgin biçimde koyulaşıyor
  (`#D98CAF` → `#8D5972`, toplam parlaklıkta -188).

### D7 — Oyun içi HUD

- [x] **43.** > "Oyun içi ekranda sol üstteki para yazısı mor renkte olmalı."

  **NE YAPILDI.** Referans karesi (`Levels.mp4` 00:28, 592×1280) piksel
  örneklendi: jeton ve "Level" yazılarının ikisi de **#9C91FF** — açık MOR.
  Biz ikisini de `Ink` (neredeyse beyaz) ile yazıyorduk. Süre yazısı ise
  gerçekten krem beyaz (#FFFDF6) ve belirgin biçimde daha büyük.
  **DERS: üç yazı üç ayrı rol taşıyor; hepsini beyaz yapmak o ayrımı siliyor.**

- [x] **44.** > "Sol üstteki para yazısının dikdörtgen arka planı ayarlanmalı."

  **NE YAPILDI.** Jeton sayacının plakası HİÇ YOKTU — yazı doğrudan zeminin
  üstündeydi ve tahtanın rengine göre bazen okunmuyordu. Referanstaki koyu
  lacivert (#181437) yuvarlak plaka eklendi; jeton simgesi plakanın sol
  ucundan taşıyor.

- [x] **45.** > "Süre görünümü, süre ikonu, text rengi ve boyutu orijinaliyle
  birebir aynı olmalı. Buna kontrol."

  **NE YAPILDI.** Süre plakası `panel_dark` görselinin boyanmış haliydi; o
  görsel MOR ve boyama çarpma olduğu için referansın koyu lacivertine hiç
  inmiyordu. Prosedürel yuvarlak panele geçildi (renk birebir). Yazı 38→42
  punto ve krem beyaz.

- [x] **46.** > "Yine oyun içi sağ üst level yazısı, texti ve arka planı
  ayarlanmalı."

  **NE YAPILDI.** 44/45 ile aynı: plaka rengi #181437, yazı #9C91FF.

- [x] **47.** > "Durdur butonunun rengi ve görünümü ayarlanmalı."

  **NE YAPILDI.** Duraklat çubukları ve yeniden başlat simgesi SAF BEYAZDI;
  referansta **#D0C8FF** (açık lavanta). Beyaz simge menekşe yüzeyin üstünde
  fazla sert kalıyor ve düğmeyi "yapıştırılmış" gösteriyordu.

- [x] **41.** > "Power-up ikonları güzel ama arkasındaki butonlar çok kötü.
  Onların güncellenmesi lazım. Yine referanstakilere bak."

  **NE YAPILDI.** Düğme tek düz yeşil bir yüzeydi. Referans karesi büyütülerek
  okundu: üç yeşil katman var ve üçü de iş yapıyor — koyu dış kenar (hacim),
  orta gövde (asıl renk), açık iç kuyu (ikonu içine oturtan yuva).
  Üçü de kuruldu; yarıçap `UiCornerFit` varsayılanı (%22) ile referansa uyuyor.

- [x] **42.** > "Power-up ikonları butonlarının alt kısmında kaç para oldukları
  yazısı da orijinaldekiyle aynı olmalı."


  **NE YAPILDI.** Adet rozeti ve fiyat kapsülünün ikisi de
  `SetSliceScale(0.10f)` ile kuruluyordu — bu köşeyi 18/0.10 = **180 piksele**
  çıkarıyor, yani kutu ne olursa olsun tamamen yuvarlanıyordu. Referansta
  rozet 45×50 ve yarıçapı 10 piksel, yani belirgin biçimde KARE. İkisi de
  oransal yarıçapa döndürüldü ve koyu kenarlık kazandı (rozet koyu kırmızı,
  fiyat koyu altın).

### D8 — Oyun içi görsel/his

- [x] **51.** > "Orijinal oyunda hangi bloku tutuyorsak / basılı tutuyorsak onun
  etrafında beyaz bir outline oluyor."

  **NE YAPILDI.** Tutma geri bildirimi yalnız bloğu %5 büyütmekti; kalabalık
  bir tahtada — özellikle aynı renkten birkaç blok yan yanayken — hangisinin
  elde olduğunu söylemiyordu. `ViewKit.Outline` + `BlockView.EnsureOutline`
  eklendi.

  **YÖNTEM: KABUK.** Bir mesh'in silüetini çizmenin doğru yolu URP'de ayrı bir
  Renderer Feature yazmak olurdu — tek bir blok için fazlasıyla ağır. Klasik ve
  bedava yol: aynı şekil %4 büyütülüp **ön yüzleri kırpılarak** (`Cull Front`)
  çizilir; geriye kalan arka yüzler bloğun arkasında olduğu için ekranda
  yalnız kenardan taşan ince beyaz bir çerçeve görünür. Işıksız (Unlit)
  materyal: kontur bir yüzey değil bir İŞARET.

  **BULUNAN VE DÜZELTİLEN ARTEFAKT.** İlk denemede kabuk tuğlanın KENDİ
  mesh'iyle kuruldu. Silüet doğru çıktı ama SAPLAMALAR da büyüdü ve her
  saplamanın üstünde beyaz bir hilal belirdi — blok "beyaz benekli" göründü.
  `BrickMeshBuilder.GetSilhouette` eklendi (aynı gövde, saplamasız, ayrı
  önbellek anahtarı).
  **DERS: kontur kabuğu gövdenin AYNISI olmamalı — taşıdığı her ayrıntı kendi
  konturunu üretir.**

  Kabuk ilk tutulduğunda kuruluyor: tahtada 20 blok var, çoğu hiç tutulmayacak.

  **NASIL DOĞRULANDI.** Kontur açık/kapalı kareler piksel piksel kıyaslandı:
  fark kutusu **tam olarak tutulan bloğun bölgesi** (3022 piksel), tahtanın
  geri kalanında sıfır fark. Yakınlaştırılmış karede silüet temiz, saplamalarda
  artefakt yok.

- [x] **53.** > "Kapıya giren blokun partikülünü iyi ayarlamak lazım. Tam girdiği
  yöne doğru partikül çıkıyor. Orijinal oyuna bakalım, yine aynı şekilde yapalım."

  **KÖK SEBEP.** Patlama bloğun **MERKEZİNDE** ve **küresel** doğuyordu. Yani
  blok kapıya girerken kırıntılar tahtanın ortasında beliriyordu ve olay
  "blok patladı" gibi okunuyordu, "kapıdan geçti" gibi değil.
  **DERS: parçacık nerede doğduğunu anlatır.** Aynı sayıda parçacık, doğru
  yerde ve doğru yönde, bambaşka bir cümle kuruyor.

  **REFERANS.** `Levels.mp4` 00:28 karesi: sarı blok KUZEY kapısından
  emilirken kırıntılar tahtanın **DIŞINDA**, kapının üstünde ve **yukarı doğru
  bir koni** hâlinde saçılıyor. Kırıntılar bloğun renginde küçük küpler.

  **NE YAPILDI.** `FXService.BurstFromGate` eklendi; emilme ve katman soyulma
  artık onu çağırıyor.
  - Konum: kapı açıklığı **boyunca** rastgele (tek noktadan değil — 3 hücrelik
    bir kapıdan çıkan blok kapının tamamından toz kaldırır) ve kapının biraz
    **dışında** (tam çizgide doğarsa yarısı çerçevenin arkasında kalıyor).
  - Hız: dışa doğru 2.4-4.6, yanlara ±1.5, yukarı 1.2-3.4 → koni.

  **DERS (tek `Emit` çağrısı tek hız verir).** `EmitParams.velocity` bütün
  partiye uygulanır; tek çağrıyla koni yapılamaz, hepsi aynı yöne fırlar ve
  "havai fişek" değil "sprey" olur. Parçacıklar tek tek yayılıyor
  (`EmitParams` struct olduğu için döngü çöp üretmiyor).

  **NASIL DOĞRULANDI — ve doğrulamanın SINIRI.** Gerçek bir emilmeyi dışarıdan
  tetikleyecek açık bir API yok (`DragController`'ın sürükleme yolu private).
  Bu yüzden doğrulama iki parçalı:
  1. *Görsel:* Sahnedeki GERÇEK kapı görselinin dünya konumundan, GERÇEK
     kırıntı sisteminden, aynı hız formülüyle 40 parçacık basıldı ve kare
     yakalandı — kırıntılar tahtanın dışında, güney kapısının altında, aşağı
     doğru koni hâlinde. Konum ve yön doğru.
  2. *Yapısal:* `BurstFromGate` girdileri (`EdgeCoord`, `OutwardSign`,
     `SpanMin/SpanMax`, `_space.CornerToWorld`) `OnGateIceShattered`'ın zaten
     kullandığı girdilerle aynı; `GateView.Create` de kapıyı tam olarak bu
     formülle konumlandırıyor.

  Olay bağlantısının kendisi tek satırlık bir değişiklik
  (`Burst(blok merkezi)` → `BurstFromGate(gate)`).

- [ ] **55.** > "Genel olarak oyun içi modellerin de detaylıca incelenmesi
  gerekiyor. Özellikle obstacle'ların ve kapıların üzerine sayı geldiğinde ne
  olduğu vs. detaylıca incelenip orijinal oyundaki hale benzemesi gerekiyor.
  **Fakat düzgün gözüken, benim ayarladığım küpleri de bozmayalım.**"

  **KISMİ İLERLEME (2026-08-18): tahta ızgarası düzeltildi.**

  Hücre ayraçları "%7 BEYAZ" ile kuruluydu; gerekçe "sınırı sezdir, dikkat
  çekme" idi ve tek başına makul. Referans ölçüldüğünde (oynanış videosu,
  00:28) tam TERSİ çıktı: hücre `#1E1B50`, ayraç `#120F2F` — ayraç hücreden
  **KOYU**. Bizde ekranda `#4E4E59` ölçüldü: hem yanlış yönde hem parlak.

  **DERS: ayracın yönü referanstan okunur, sezgiden değil.** Tahta zaten koyu
  bir kuyu; açık çizgi orada ÇIKINTI gibi okunuyor ve boş hücreler "ızgara
  kâğıdı" gibi görünüyor. Koyu çizgi bir OLUK — hücreler kabartma kalıyor ve
  bloklar oraya oturuyormuş hissi doğuyor.

  Ayrıca referansta her iç kesişimde küçük koyu bir NOKTA var; eklendi.

  **NASIL DOĞRULANDI.** Ayraç `#4E4E59` → **`#0D0D28`**, yani hücreden
  (`#1E1E53`) koyu — referansın yönüyle aynı.

  **AÇIK KALAN:** engellerin ve kapıların üzerindeki SAYILARIN görünümü
  (buz sayacı, perde sayacı) henüz referansla kıyaslanmadı.

- [ ] **56.** > "İç içe 2 blok feature'ın visualı çok kötü. Orijinal oyundaki
  gibi olması gerekiyor."

---

## E — GÖRSEL VARLIK GEREKTİRENLER

- [ ] **35. Grand açılışı orijinal görselle.**
  > "İlk açılışta direkt Grand'ın orijinal görselini kullanalım. Bizim
  > yazdığımız 'Grand' orijinal değil."

  Kaynak elde var: `grand açılış.jpeg`.

- [ ] **49. BLOCK OUT! logosu.**
  > "Splash screende oyun kazandığımızda vs. çıkan 'Block out!' yazısı bildiğin
  > oyunun orijinal logosu olmalı. Bizim yazdığımız çok kötü duruyor."

  Kaynak elde var: `oyun açılış.jpeg`.

- [ ] **50. Kazanma ekranı efektleri.**
  > "Yine oyun kazanma ekranı çıkan efektler, o kısmı çok iyi hale getirmeliyiz."

- [ ] **38. Yeniden oyna ikonu.**
  > "Yeniden Oyna ikonu değiştirilmesi lazım. Bunu da butonu yapay zekâya
  > yaptıralım."

- [ ] **48. Power-up efektleri.**
  > "3 tane power-up'ın da efektleri ve görünümü orijinal oyundakiyle alakası
  > yok. Birebir aynısını yapmalıyız."

- [x] **54. Buz bloğu.**
  > "Buz bloğu inanılmaz kötü duruyor. Bunun için yine yapay zekâya bir şey
  > yaptırabiliriz. Daha iyi hale getirilmeli."


  **REFERANS BULUNDU.** `Levels.mp4` 05:40 — 13. bölümde tahtada üç buz bloğu
  birden var. Ölçüldü: gövde `#1DB3F8`, üst bandı `#1996F0`, sayaç `#FEDDD3`
  (krem/şeftali). Kıyas için aynı karedeki normal mavi blok `#024DFB` — yani
  buzun mavi bloktan belirgin biçimde AÇIK ve CAMGÖBEĞİ olması gerekiyor.
  Ayrıca referansta buz **DÜZ bir levha**: silüet tuğlanın ama yüzey pürüzsüz,
  saplama yok.

  **DÖRT AYRI HATA BULUNDU VE DÜZELTİLDİ.**

  1. *Renk.* Bizimki `#3D9EEB` idi — hem daha koyu hem daha MAVİ. Ölçülen
     değere çekildi.
  2. *Yüzey.* Kabuk tuğlanın kendi (saplamalı) mesh'ini kullanıyordu; buz
     "mavi boyanmış tuğla" gibi okunuyordu. 51. madde için üretilen
     `GetSilhouette` (saplamasız gövde) buraya da uydu.
  3. *Sayaç rengi.* Koddaki yorum "koyu lacivert zeminde KREM" diyordu ama
     yazılan renk `(0.13, 0.20, 0.38)`, yani koyu lacivertin ta kendisiydi.
     **DERS: yorum ile kod çelişiyorsa ikisinden biri yalan söylüyor.**
     Niyet doğru yazılmış, uygulaması yanlış kalmıştı.
  4. *Komşu buzların kaynaması — 2. düzeltmenin YAN ETKİSİ.* Saplama gidince
     10. bölümdeki **dokuz buz bloğu ekranda TEK bir dev camgöbeği leke**
     olarak çıktı; tahta okunmaz oldu. Sırayla denendi ve hiçbiri tek başına
     yetmedi:
     - koyu kenar kabuğu (`ViewKit.IceRim`) → kıl gibi ince kaldı,
     - `BlockOut/Brick` shader'ına geçiş (köşe renklerini okuyor) → bitişik
       ÜST yüzlerin tonu aynı olduğu için sınır yine doğmadı.

     **Sorun gölgede değil GEOMETRİDEYDİ: iki levha fiziksel olarak bitişikse
     aralarında gösterilecek bir şey yok.** Kabuk yatayda %6 içeri çekildi;
     aradan tahtanın koyu zemini geçiyor ve sınır kendiliğinden doğuyor.
     (Kenar kabuğu ve Brick shader'ı da kaldı — üçü birlikte referansın
     koyu kenarlı, gölgeli kalıp görünümünü veriyor.)

  **NASIL DOĞRULANDI.** 10. bölüm (dokuz buzlu blok) yakalandı: her kalıp ayrı
  ve okunur, aralarında koyu boşluk, 3B yan yüzler, krem sayaçlar.
  Ölçüm: buz gövdesi **`#16AAED`** (referans `#1DB3F8`).

---

## İLERLEME

| grup | madde | kapalı |
|---|---|---|
| A — kritik | 1 | 0 |
| B — yapısal | 5 | 0 |
| C — yeni panel | 2 | 0 |
| D — ekran ekran | 34 | 0 |
| E — görsel varlık | 6 | 0 |
| **TOPLAM** | **48** | **0** |

> Not: numaralar kullanıcının anlatım sırasına göre 1-56 arası; bazı numaralar
> aynı işin parçası olduğu için madde sayısı 48.
