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

- [ ] **1. Açılış ekranı tam ekranı kaplamıyor.**
  > "Splash screen tamamen ekranı kaplamıyor; altında ve üstünde boşluklar var."

- [ ] **2. Ekranlar arası kaydırmayla geçiş yok.**
  > "Sağa veya sola kaydırınca ekranlar arasında geçiş olmuyor. Bunu yapalım
  > demiştim, hâlâ yapılmamış. Sadece butonlara tıklayarak ekran geçişi oluyor."

  **Önceki turdan devreden istek.**

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

- [ ] **7. Kaydırmada arka plan boşluğu (overscroll).**
  > "Basılı tutup aşağı ya da yukarı kaldırdığımızda arka plandaki zemin
  > çıkıyor ve boşluğu görebiliyoruz. Orijinal oyunda arka plan o kadar
  > hareket etmiyor ve hiçbir boşluk yok, dolu. Hep kaysa da boşluk
  > görünmüyor."

  Mağazada görüldü ama kaydırılan her ekranı ilgilendiriyor.

---

## C — YENİ PANEL AKIŞLARI (oyun içi)

- [ ] **39. Yeniden başlat → ONAY paneli.**
  > "bizde tıklayınca direkt oyun yeniden başlıyor. Öyle olmaması lazım.
  > Direkt hangi seviyede olduğumuzu üstünde yazan bir panel olacak.
  > Panelin yan çaprazında kapatma işareti olacak. Panelin içinde '1 can
  > kaybedeceksiniz' olacak. Ortada kırık bir kalp olacak. O kalp büyüyüp
  > küçülecek. Altında da '1 can kaybedeceksiniz' texti olacak. 'Tekrar Dene'
  > butonunu da altta olacak şekilde yapmamız lazım. Kapatırsak direkt oyuna
  > devam edeceğiz."

- [ ] **40. Tekrar Dene → BAŞARISIZ paneli.**
  > "Tekrar Dene dersek bu sefer başarısız paneli çıkacak. Üstte 'Başarısız'
  > yazısı olacak. Hemen altında kırık kalp olacak. Yine kapatma işareti
  > olacak. Hangi seviye olduğu ve zorluğu yazacak. Altında kaç para
  > kaybettiğimiz vs. olacak. Yeniden Dene butonuna yeniden basarsak da
  > kalbi kaybedeceğiz."

  Referans: **`Game over .mp4`** — bu video hiç kıyaslanmadı, kaynak burada.

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

- [ ] **6.** > "Restore Purch kısmı en aşağıya inildiğinde direkt buton olarak
  çıkıyor. Orijinalde böyle ama bizde gözükmüyor, kaydırdığımızda gözüküyor."

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
- [ ] **21.** > "Şu an sandık alakasız duruyor."
- [ ] **22.** > "Menü ikonu da alakasız duruyor."
- [ ] **23.** > "Collection kısmının dikdörtgeninin yine çerçevesi, dış kenarı,
  gölgesi vs. yok. Kötü duruyor."

### D4 — Yolculuk (Journey)

- [ ] **25.** > "Journey kısmında direkt ilk yuvarlağın ortasına Play yerine
  tikli buton koymalıyız."
- [ ] **26.** > "O 'Lv1-20' yazan yerin arka planı da büyük ve kötü gözüküyor.
  Bunu da ayarlamalıyız."
- [ ] **27.** > "Mission Prep kısmının da yine kaplaması olmalı."
- [ ] **28.** > "Journey'deki level dikdörtgenlerinin dış çizgisi biraz daha
  koyu, gölgeli olmalı. Orijinal oyundan bak."
- [ ] **29.** > "Kilitli olan kısımlar daha koyu renkte gözükmeli. Şu an beyaz."

### D5 — Ayarlar (Settings)

- [ ] **31.** > "Ayarlarda Delete My Account dikdörtgen şeklinde değil. Arka
  planı kötü duruyor ve çok aşağıda taşmış."
- [ ] **32.** > "Support, Terms, Privacy butonlarının koyu gölgesi yok.
  Orijinaldekine benzetilsin."
- [ ] **33.** > "Yine Notifications taşmış."
- [ ] **34.** > "Off / On butonları çok oval. Orijinalinde gölgeli, parlak, şık,
  dikdörtgene benzer tasarımlar mevcut."

### D6 — Ana ekran (Menü)

- [ ] **36.** > "Menüdeki paraya tıklarsak direkt ikona veya para yazısına
  mağazaya yönlendirsin, geçişli bir şekilde."
- [ ] **37.** > "Cana tıkladığımızda artıya değil, direkt cana ve 'dolu' yazan
  kısma, örneğin popup gibi olsun. Orası küçülüp büyüsün, minik hareket
  ediyor gibi olsun. Yani orijinal oyunda var."

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

- [ ] **51.** > "Orijinal oyunda hangi bloku tutuyorsak / basılı tutuyorsak onun
  etrafında beyaz bir outline oluyor."
- [ ] **53.** > "Kapıya giren blokun partikülünü iyi ayarlamak lazım. Tam girdiği
  yöne doğru partikül çıkıyor. Orijinal oyuna bakalım, yine aynı şekilde yapalım."
- [ ] **55.** > "Genel olarak oyun içi modellerin de detaylıca incelenmesi
  gerekiyor. Özellikle obstacle'ların ve kapıların üzerine sayı geldiğinde ne
  olduğu vs. detaylıca incelenip orijinal oyundaki hale benzemesi gerekiyor.
  **Fakat düzgün gözüken, benim ayarladığım küpleri de bozmayalım.**"
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

- [ ] **54. Buz bloğu.**
  > "Buz bloğu inanılmaz kötü duruyor. Bunun için yine yapay zekâya bir şey
  > yaptırabiliriz. Daha iyi hale getirilmeli."

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
