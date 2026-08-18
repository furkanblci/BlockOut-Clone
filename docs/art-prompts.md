# Görsel Üretim Rehberi — Block Out! Clone

Bu dosya, oyunun arayüz görsellerini dışarıda üretirken kullanılacak **prompt listesidir**.
Renkler ve biçim kuralları referans oyunun karelerinden **ölçülerek** çıkarıldı, göz kararı değil.

> Üretilen dosyaları `Assets/_Project/Art/UI/` altına at. İsimlendirme her promptun
> başlığında yazıyor; kodun beklediği ad budur.

---

## 0. Önce şunu bil: stil DNA'sı

Referans oyunun arayüzü **3B render edilmiş, parlak plastik/oyuncak** estetiğinde.
Vektörel düz (flat) ikon **değil**; yumuşak stüdyo ışığıyla render edilmiş küçük objeler.

| Kural | Değer |
|---|---|
| Işık | Sol üstten tek yumuşak anahtar ışık, sağ altta yumuşak gölge |
| Yüzey | Parlak plastik; üstte belirgin bir ışık lekesi (specular highlight) |
| Kenar | Kalın, bol yuvarlatılmış; keskin köşe yok |
| Kontur | 3B ikonlarda **kontur yok** (gölgeyle okunur). Yalnız YAZI koyu konturlu |
| Kamera | Hafif 3/4 açı ya da tam önden, geniş açı değil |
| Doygunluk | Yüksek — şeker rengi palet |

### Ölçülmüş palet (referans karelerden)

```
Arka plan (koyu)      #1A173A      oyun ekranı zemini
Arka plan (menü)      #3B1B64      menü zemini
Tahta çerçevesi       #5A52C8      periwinkle mor
Sekme çubuğu          #3F2FCD      mavi-mor
Kart moru             #9F13CB      mağaza kartı gövdesi
Başlık şeridi         #8A17C5      koyu magenta-mor
Kart kremi            #FCEFE8      kart içeriği zemini
Yeşil (CTA üst)       #2DCC0C      ana buton yüzü
Yeşil (CTA alt kenar) #18A714      butonun alt kalınlığı
Jeton altını          #F0C000      coin
Kalp kırmızısı        #EC412D      can
```

### Her prompta eklenecek ORTAK SON EK

Aşağıdaki metni **her** promptun sonuna aynen ekle. Stil tutarlılığı üretimden değil,
bu ortak son ekten gelir — parça parça üretsen bile takım gibi durur.

```
mobile casual puzzle game UI asset, 3D rendered glossy plastic toy style,
soft studio lighting from top-left, subtle ambient occlusion, strong specular
highlight on top surface, thick rounded edges, no outlines, highly saturated
candy colors, centered single object, plain transparent background,
no text, no letters, no numbers, no drop shadow on background,
square 1024x1024, clean edges for game asset cutout
```

### Teknik çıktı şartları

- **1024×1024**, PNG
- Tek nesne, ortalanmış, kenarlara ~%8 boşluk — **nesne kenara değmesin**
- Üstüne **yazı/rakam bastırma** — sayılar oyunda TMP ile yazılıyor
- Gölgeyi arka plana bastırma (kod kendi gölgesini koyuyor)

> **Saydamlık artık şart değil.** Gemini saydam veremiyorsa uğraşma: düz bir
> zeminle üret, dosyayı `art_raw/` klasörüne olduğu gibi at, arka planı
> `tools/cutout.py` temizliyor. Ayrıntı: [art_raw/README.md](../art_raw/README.md).
> Tek şart, nesnenin görüntünün kenarına değmemesi.

---

## 1. Öncelik A — bunlar olmadan hiçbir ekran düzelmiyor

### 1.1 `btn_green.png` — ana eylem butonu (9-slice)
```
A chunky rounded rectangle game button, bright grass green face (#2DCC0C) with a
darker green bottom rim (#18A714) giving it 3D thickness, glossy highlight band
across the upper half, very rounded corners (radius about 18% of height),
wide horizontal shape 3:1 ratio, front view, flat-on camera
```
+ ortak son ek. **Not:** 9-slice yapılacağı için kenarlar bozulmadan gerilebilmeli;
merkez alan düz olmalı, desen olmamalı.

### 1.2 `btn_purple.png` — ikincil buton
Aynı prompt, renkleri değiştir:
`bright violet face (#6B4FE0) with darker violet bottom rim (#4B33B8)`

### 1.3 `btn_red.png` — yıkıcı eylem (Hesabımı Sil)
`warm red face (#EC412D) with darker red bottom rim (#B62718)`

### 1.4 `panel_card.png` — mağaza/menü kartı (9-slice)
```
A rounded rectangle UI card for a mobile game, cream ivory face (#FCEFE8) with a
thin warm beige inner border, sitting on a magenta purple base (#9F13CB) that
shows as a thick bottom band, soft inner shadow at the top edge, very rounded
corners, front view
```

### 1.5 `panel_dark.png` — koyu panel / kapsül (9-slice)
```
A rounded rectangle dark UI panel, deep indigo navy face (#241F52), subtle
lighter rim light along the top edge, soft inset shadow, very rounded corners,
semi glossy, front view
```

### 1.6 `frame_board.png` — tahta çerçevesi (9-slice)
```
A thick rounded picture frame for a puzzle board, periwinkle purple (#5A52C8)
plastic, soft rounded bevel on the outer edge, slightly darker inner groove,
top-left highlight, hollow center, front view
```

---

## 2. Öncelik B — ikonlar

Hepsi aynı ortak son ekle. Her biri tek başına, saydam zeminde.

| Dosya | Prompt gövdesi |
|---|---|
| `icon_coin.png` | `A shiny gold game coin, thick disc seen from a slight 3/4 angle, embossed star symbol on the face, warm gold (#F0C000) with bright rim light and a white specular streak` |
| `icon_heart.png` | `A glossy red game heart, plump rounded 3D shape, bright red (#EC412D) with a soft pink highlight on the upper left, slight bottom darkening` |
| `icon_gear.png` | `A chunky settings gear wheel, glossy violet plastic (#6B4FE0), six thick rounded teeth, round hole in the center, slight 3/4 tilt` |
| `icon_star.png` | `A plump five-pointed game star, glossy golden yellow (#FFCE2B), rounded thick points, bright highlight top-left` |
| `icon_lock.png` | `A closed padlock, glossy grey-blue body with a silver shackle, chunky rounded proportions, small keyhole` |
| `icon_chest.png` | `A treasure chest slightly open with gold coins spilling out, red-and-gold wooden chest with metal bands, glossy toy render` |
| `icon_clock.png` | `A cartoon alarm clock with two bells on top, glossy cyan blue body (#3FB8E8), white face, pink and orange accents, slight 3/4 angle` |
| `icon_rocket.png` | `A small cartoon rocket, red nose cone, blue and white body, glossy toy plastic, flame at the bottom, diagonal 3/4 pose pointing up-right` |
| `icon_ufo.png` | `A cartoon flying saucer, purple metallic dome over a lavender disc body, glowing blue orb on top, small round lights along the rim, 3/4 angle` |
| `icon_plus.png` | `A round green button with a thick white plus sign, bright green (#2DCC0C) glossy sphere-like face with darker green rim` |
| `icon_trophy.png` | `A golden trophy cup with two handles on a base, glossy gold, slight 3/4 angle` |
| `icon_shop.png` | `A small market stall with a pink and white striped awning and a wooden counter, glossy toy render, front view` |
| `icon_home.png` | `A small cartoon building with a pink dome roof and golden trim, glossy toy render, front view` |
| `icon_globe.png` | `A pink and gold globe on a small stand, glossy toy render, slight 3/4 angle` |

---

## 2.5. ŞU AN GEREKEN LİSTE (2026-08-07)

Arayüzün tamamı kuruldu. Aşağıdakiler ekranlarda fiilen eksik; her birinin
yerinde şu an geçici bir çözüm duruyor.

**Ortak son ek** (her promptun sonuna aynen ekle):

```
mobile casual puzzle game UI asset, 3D rendered glossy plastic toy style,
soft studio lighting from top-left, subtle ambient occlusion, strong specular
highlight on top surface, thick rounded edges, no outlines, highly saturated
candy colors, centered single object, plain flat background,
no text, no letters, no numbers, clean edges for game asset cutout
```

Saydamlık gerekmiyor; `tools/cutout.py` hallediyor. **Tek şart: nesne
görüntünün kenarına değmesin.**

---

### 1. `home_characters.png` — ana ekranın ortasındaki karakterler
**En büyük görsel eksik.** Ana ekranın ortası şu an boş manzara; referansta
orada karakterler duruyor.

```
Three cheerful original cartoon creature characters standing together in a
group, full body, front view, friendly poses, one waving, one holding a
wrapped gift box, one jumping with arms up, chunky rounded proportions,
soft matte skin with glossy highlights, dungarees and simple overalls in
teal orange and lime, big expressive eyes, no hats, standing on flat ground
```
**1536×1024, yatay.** Karakterleri referanstan kopyalama — kendi karakterlerimiz olsun.

---

### 2. `region_1.png` … `region_4.png` — Yolculuk bölge daireleri
Şu an menü zemini kırpılıp kullanılıyor ve **kare duruyor**. Dairesel
çizilirse maskeye hiç gerek kalmaz.

Ortak gövde:
```
A circular vignette illustration of <SAHNE>, painted 3D toy diorama style,
composition fully inside a circle, content fading softly to the circle edge,
viewed from a slightly high angle, rich saturated colors, no characters
```

`<SAHNE>` yerine sırayla:
1. `a candy colored village square at sunset with rounded toy houses`
2. `a purple night sky with glowing stars and floating platforms`
3. `a snowy icy hill with igloos and frozen ponds`
4. `a golden mountain peak above the clouds`

**Her biri 1024×1024, kare çerçeve içinde DAİRE.**

---

### 3. `avatar_player.png` — üst bardaki profil resmi
Şu an oyuncunun baş harfi yazıyor.

```
A friendly original cartoon creature portrait bust, centered, facing the
camera, big round eyes, cheerful smile, teal and orange color scheme,
shoulders visible at the bottom edge, glossy toy plastic style
```
**512×512, kare.**

---

### 4. `check_green.png` — yeşil onay tiki
Yolculukta alınmış ödüllerin yanında. Şu an yeşile boyanmış yıldız var.

```
A thick rounded green checkmark symbol, glossy plastic, slight 3/4 tilt,
bright grass green face with a darker green bottom rim giving 3D thickness
```
**512×512.**

---

### 5. `ribbon_reward.png` — "Ödüller x2" şeridi (9-slice)
Oyna düğmesinin üstüne binen turuncu etiket. Şu an koyu panel turuncuya
boyanıyor.

```
A small horizontal ribbon banner label, glossy orange plastic face with a
darker orange bottom rim, rounded ends, flat hollow center, front view,
wide 4:1 ratio
```
**1024×256.** 9-slice yapılacağı için **ortası düz olmalı**, desen olmamalı.

---

### 6. `ad_creative.png` — sahte reklamın görseli (isteğe bağlı)
Test reklamı ekranında şu an sandık ikonu duruyor. Şart değil ama akış
daha inandırıcı görünür.

```
A colorful fake mobile game advertisement creative, glossy toy blocks
tumbling out of an open treasure chest, dynamic diagonal composition,
bright candy colors on a deep purple background
```
**1024×1024.**

---

## 3. Öncelik C — süsleme
## 3. Öncelik C — süsleme

### 3.1 `bg_menu.png` — menü arka planı (1024×2048, saydam DEĞİL)
```
A cheerful cartoon village square at sunset seen from a low camera, candy colored
buildings with rounded shapes, purple and pink sky with soft clouds, green grass
and a warm sand path, no characters, no text, soft painterly 3D render,
vertical mobile game background, blurred depth of field in the distance
```
> Karakter isteme — kendi karakterimizi sonra ekleyeceğiz.

### 3.2 `node_level.png` — yolculuk yolundaki bölüm düğümü
```
A round level node button for a map, glossy periwinkle purple disc with a thick
raised golden ring around it, slightly domed face, front view
```

### 3.3 `banner_region.png` — bölge afişi (9-slice)
```
A wide ribbon banner for a game map, glossy purple fabric with golden rope trim
and rounded ends, front view, hollow center for text
```

### 3.4 `confetti_sheet.png` — konfeti parçacıkları
```
A grid of small confetti pieces on transparent background, glossy plastic
rectangles and curled ribbons, in green, gold, pink, blue and purple,
scattered rotations, no overlap between pieces
```

---

## 4. Font — bunu üretmiyoruz, indiriyoruz

Referanstaki yazı **kalın, yuvarlak, geniş** bir sans-serif. En yakın ücretsiz
(OFL, ticari kullanıma uygun) eşleşmeler:

1. **Baloo 2 ExtraBold** — en yakını
2. **Fredoka One**
3. **Luckiest Guy** — daha oyuncu, başlıklar için

`Assets/_Project/Art/Fonts/` içine `.ttf` olarak at, gerisini ben hallederim
(TMP asset + gradyan + kalın koyu kontur + alt gölge materyali).

---

## 5. Bir uyarı

Butonlar, jetonlar, kalpler, sandıklar — bunlar casual oyun ortak dili, sorun yok.
**Karakterleri birebir kopyalama** (penguen, yeşil yaratık vs.). Onlar telifli
tasarım; portfolyoda "kopyaladı" değil "yapabiliyor" demek istiyoruz. Aynı
*stilde* kendi karakterimiz hem güvenli hem daha güçlü durur.

---

## 6. Üretim sırası önerisi

1. **Font** (indir) → en büyük tek kazanç
2. `btn_green`, `btn_purple`, `panel_card`, `panel_dark` → tüm ekranlar bunları kullanıyor
3. `icon_coin`, `icon_heart`, `icon_gear`, `icon_star`, `icon_lock` → üst bar ve bölüm düğümleri
4. `icon_clock`, `icon_rocket`, `icon_ufo` → yardımcı çubuğu
5. `frame_board` → oyun ekranı
6. Gerisi (arka plan, düğüm, afiş, konfeti)

İlk üç madde geldiği anda ekranları yeniden kurmaya başlayabilirim; hepsini
beklemeye gerek yok.

---

## 7. MAĞAZA görselleri (2026-08-10 eklendi)

Mağaza ekranı referanstan ölçülerek yeniden kuruldu
(`StoreScreen.cs` + `ShopSprites.cs`). Yerleşim, renkler ve yazılar bitti.
Eksik olan tek şey **11 resim**: kartlardaki altın yığınları ve paket kapları.
Gelmeyenlerin yerine şimdilik `icon_coin` / `icon_chest` konuyor — bu yüzden
beş paketin beşi de aynı sandığı gösteriyor.

> Hepsi ortak son ek ile üretilecek. Dosya adları kodun beklediği adlardır,
> `art_raw/` içine bu adlarla at.

### 7.1 Jeton yığınları — `coin_pile_1.png` … `coin_pile_6.png`

Jetonlar ızgarasındaki altı kutu. Tek iş: **büyüklük farkı okunsun**. Fiyatı
okumadan önce göz hangisinin daha çok verdiğini görmeli, o yüzden yığınlar
sayıca ve hacimce belirgin biçimde artmalı.

| Dosya | Karşılığı | İstenen |
|---|---|---|
| `coin_pile_1` | 1 000 | `a small neat pile of about 8 gold coins, one coin standing upright leaning on the pile` |
| `coin_pile_2` | 5 000 | `a modest pile of about 20 gold coins in two short stacks, a few loose coins in front` |
| `coin_pile_3` | 10 000 | `a wider pile of about 40 gold coins, three stacks of different heights` |
| `coin_pile_4` | 25 000 | `a large mound of gold coins, roughly 80 coins, several tall stacks rising from a spread base` |
| `coin_pile_5` | 50 000 | `a very large heap of gold coins, roughly 150 coins, tall central stack, coins spilling wide` |
| `coin_pile_6` | 100 000 | `a huge treasure mound of gold coins, hundreds of coins, tall peak in the middle, coins cascading down both sides` |

Ortak: `bright yellow gold coins with a five-pointed star embossed on the face,
matching the game coin icon, seen from a low 3/4 angle, wider than tall (about 4:3)`
\+ ortak son ek.

**Dikkat:** Yığın 4:3 civarı olmalı, kare değil — kart alanı yatay. Ve yığının
üstüne **rakam yazdırma**, sayıyı oyun kendi yazıyor.

### 7.2 Paket kapları — `pack_1.png` … `pack_5.png`

Paketler listesindeki beş kart. Bunlar bir "kap + içinden taşan altın"
kompozisyonu; kap büyüdükçe ve zenginleştikçe paket değeri artıyor.

| Dosya | Paket | İstenen |
|---|---|---|
| `pack_1` | Tuğla Paketi (2 000) | `a small red cloth money pouch tied with a gold cord, a few gold coins spilling out of the top and lying beside it` |
| `pack_2` | Blok Paketi (5 000) | `a round red ceramic pot with a silver rim, filled to the brim with gold coins that spill down its side` |
| `pack_3` | Premium Paket (8 000) | `an open treasure chest with a red body and gold trim, pink gemstones on the lid, packed with gold coins overflowing at the front` |
| `pack_4` | Lüks Paket (20 000) | `a large ornate treasure chest with a purple velvet body and heavy gold corners, wide open, gold coins and a few purple gems pouring out` |
| `pack_5` | Nihai Paket (60 000) | `an enormous mound of gold coins burying a half-open royal chest, coins cascading forward, a couple of red and blue gems on top` |

Ortak: `seen from a low 3/4 angle, wider than tall (about 4:3), the container sits
on the left and the gold spills toward the right` + ortak son ek.

**Neden sola yaslı:** Kartın sağ yarısı ödül simgelerine ayrıldı (reklam-yok,
sınırsız can, üç yardımcı). Kap ortalanırsa simgelerin altına girer.

### 7.3 İsteğe bağlı — çizimle idare ediliyor

Bunlar şu an kodla çiziliyor (`ShopSprites.cs`) ve fena durmuyor. Referansa
tam oturması istenirse üretilebilir, **öncelik değil**:

- `icon_noads.png` — `a bold red prohibition sign, thick red ring with a diagonal
  bar from top-left to bottom-right, glossy 3D plastic`
- `icon_infinite.png` — `a glossy red heart with a white infinity symbol embossed
  across its center`

### 7.4 Bunları İSTEME

Bu ekranın geri kalanı görsel değil geometri; kod çiziyor ve her genişlikte
kusursuz çalışıyor. Üretici görsel istemek burada geriye adım olur:

- Mavi çizgili **tente** ve festonlu alt kenarı (`ShopSprites.Awning`)
- Bölüm başlıklarının **kapsül** şeridi (`ShopSprites.Capsule`)
- Yeşil fiyat düğmeleri, mavi geri-yükle düğmesi, köşe kurdelesi, taşıyıcı
  noktaları — hepsi kapsül/panel + renk

### 7.5 Durum — KAPANDI (2026-08-17 doğrulandı)

> Aşağıdaki üç eksik **giderilmiş**: `coin_pile_5` artık `coin_pile_3` ile
> aynı dosya değil, `pack_4` ile `pack_5` ayrı dosyalar ve `pack_5`'in sağ
> kenarında tek bir opak piksel yok (düz kesik gitmiş). Liste tarihsel kayıt
> olarak duruyor; yeni bir şey istenmiyor.

Gelen dosyalar işlendi ve `UiSkin`'e bağlandı. Eksikler:

| Dosya | Sorun |
|---|---|
| `coin_pile_5` | Hiç gelmedi — atılan dosya `coin_pile_3` ile **byte-byte aynı** |
| `pack_4` | Gelmedi; ham `pack_4` aslında `pack_5` tarifiydi |
| `pack_5` | Kurtarıldı ama **sağ kenarında düz kesik** var, yeniden üretilmeli |

**`pack_5`'te ne oldu:** Altın yığını görüntünün sol/sağ/alt kenarına
değiyordu. `cutout.py` taşmayı KENARDAN başlatır; kenardaki altın "zemin
tohumu" sayıldı ve yığının tamamı silindi. Kenar payı eklenip kesim tekrarlandı,
görsel kurtarıldı — ama padding, kenara değen sikkeleri düz bir çizgide kesiyor.

> **Bu yüzden "nesne kenara değmesin" şartı süs değil.** Prompt'a
> `object must not touch the image border` yazmak yetmiyorsa
> `plenty of empty margin around the object, object occupies only the middle 80% of the frame`
> ekle.

Eksik kademe kodda **bir alt kademeye** düşüyor (`StoreScreen.Tiered`): şu an
`coin_pile_5` yerine `_4`, `pack_4` yerine `pack_3` görünüyor. Ekran çalışıyor,
sadece iki kart komşusuyla aynı resmi paylaşıyor.

### 7.6 Kapandı (2026-08-10)

Eksik üç dosya geldi, kesildi, bağlandı. **Mağazanın görsel ihtiyacı bitti.**
`check_art.py`: 52 görsel, 0 hasarlı.

İkinci turda kenar payı şartı tuttu: `coin_pile_5`, `pack_4`, `pack_5`'in
üçünde de kenar şeridinde nesne yok, kesim ilk denemede temiz çıktı.

**Ölçülen not — yığınları büyütme:** Üretici her yığını kareye sığdırıyor,
yani 8 jetonluk yığınla 300 jetonluk yığın aynı kutuyu dolduruyor. Bu bir
kusur DEĞİL: referans karesi ölçüldüğünde altı yığının da genişliği 226-229
piksel çıktı, artan tek şey jeton yoğunluğu (%21 → %35). Kodda yığınları
kademeli büyüten ölçek bu yüzden kaldırıldı.

---

## 8. YOLCULUK görselleri (2026-08-10)

Yolculuk ekranı referanstan yeniden kuruldu. İki görsel eksik.

### 8.1 `region_5.png` — "Buz Kurtarma" bölgesi (sv 71-100)

Referansta beş bölge var; "Buz Kurtarma" ilk kuruluşta atlanmıştı (videoda
yalnız kilitli hâliyle göründüğü için gözden kaçmış). Diğer dördü elimizde:
`region_1` köy, `region_2` uzay, `region_3` penguen/kar, `region_4` zirve.

```
A circular game region badge showing a snowy ice cavern scene: friendly cartoon creatures chipping frozen blocks out of a pale blue glacier wall, icicles hanging above, soft snow drifts, a warm lantern glow in the middle of the cold blues, mobile casual puzzle game key art, 3D rendered glossy plastic toy style, soft studio lighting from top-left, subtle ambient occlusion, thick rounded edges, highly saturated candy colors, the whole illustration is cropped inside a PERFECT CIRCLE that touches the edges of the square frame, no border, no ring, no text, no letters, no numbers, square 1024x1024
```

**Dikkat — bu görselde kural TERS:** Bölge görselleri daire biçiminde
kırpılmış olmalı ve kareyi doldurmalı; kesim aracına girmezler
(`cutout.py` çalıştırma, doğrudan `Assets/_Project/Art/UI/` altına at).
Diğer üçü de böyle üretildi.

### 8.2 `icon_lock.png` — ALTIN asma kilit (mevcut olan gümüş)

Kilitli bölgenin ortasındaki asma kilit referansta **altın sarısı**; elimizdeki
`icon_lock` gümüş. Kod şimdilik sıcak bir tona boyuyor ama **boyama çarpımdır**,
gümüşü altına çeviremez — sonuç mat bir hardal.

```
A chunky closed padlock in bright warm gold with an orange-amber body, a lighter gold shackle, a dark keyhole in the center, glossy 3D plastic toy style, front view, mobile casual puzzle game UI asset, soft studio lighting from top-left, subtle ambient occlusion, strong specular highlight on top surface, thick rounded edges, no outlines, highly saturated candy colors, single centered object on a plain solid light grey background, no text, no letters, no numbers, no drop shadow on the background, square 1024x1024, object must not touch the image border, clean edges for game asset cutout
```

Bu ikincisi `art_raw/` yoluna girer (kesim gerekir).

---

## 9. Kaybetme paneli (2026-08-16)

Referans: `OneDrive/Masaüstü/Block Out! Videos/Game over .mp4`, 18. saniye.

### 9.1 `icon_heart_broken.png` — KIRIK kalp

Kaybetme kartının üst kenarına binen rozet referansta **çatlamış** bir kalp.
Elimizde yalnız sağlam `icon_heart` var ve panel şu an onu kullanıyor —
"can kaybettin" mesajı sağlam kalple TERS anlam veriyor.

```
A chunky glossy red heart broken into two halves with a jagged lightning-bolt crack running down the middle, the two halves tilted slightly apart, deep crimson shading in the crack, glossy 3D plastic toy style, front view, mobile casual puzzle game UI asset, soft studio lighting from top-left, subtle ambient occlusion, strong specular highlight on the upper left of each half, thick rounded edges, no outlines, highly saturated candy colors, single centered object on a plain solid light grey background, no text, no letters, no numbers, no drop shadow on the background, square 1024x1024, object must not touch the image border, clean edges for game asset cutout
```

`art_raw/` yoluna girer (kesim gerekir).

### 9.2 İki panel KURULDU (2026-08-17) — bir görsel eksik kaldı

"Devam Et?" paneli yukarıdaki kırık kalbi kullanıyor, sorun yok.

"Süre Doldu" panelinde referansta **altın bir kronometre** var; biz süre
yardımcısının **yeşil çalar saatini** kullanıyoruz. Yanlış değil (oyuncu o
simgeyi zaten "süre" diye tanıyor) ama referansla aynı da değil.

**Boyayarak çözülemez:** boyama çarpmadır, yeşil bir görsel altına
çevrilemez — denendi, çamurlu bir yeşil çıktı ve boyama kaldırıldı.

## 10. `icon_stopwatch.png` — altın kronometre (isteğe bağlı)

```
A chunky golden stopwatch seen from the front, round gold case with a cream dial, a single bold red-orange pointer, small blue square markers at the quarter positions, a gold crown button on top and a small red push button on the upper right, glossy 3D plastic toy style, mobile casual puzzle game UI asset, soft studio lighting from top-left, subtle ambient occlusion, strong specular highlight on the upper left of the case, thick rounded edges, no outlines, highly saturated candy colors, single centered object on a plain solid light grey background, no text, no letters, no numbers, no drop shadow on the background, square 1024x1024, object must not touch the image border, clean edges for game asset cutout
```

`art_raw/` yoluna girer (kesim gerekir). Gelince `ContinueOffer.BuildStage1`
içindeki `Art.Clock` yerine yeni anahtar konur, düzen değişmez.

## 11. KOLEKSİYON görselleri (2026-08-17, **2026-08-18'de düzeltildi**)

> **ÖNEMLİ DÜZELTME (2026-08-18).** Bu bölümdeki iki istem `collections.jpeg`
> yeniden ve büyütülerek incelenince YANLIŞ çıktı. Eski istem kitabın
> çevresine "chunky toy building blocks" (oyuncak yapı taşları) koyuyordu;
> referansta oradaki nesneler yapı taşı DEĞİL, **turuncu köşe kapaklı,
> pembe kayış-tokalı sarılmış ALBÜM PAKETLERİ**. Sekme ikonu da "deep red
> cover" diye tarif edilmişti; referansta yüz **pembe-magenta**, çerçeve
> altın. Eski istemler kullanılsaydı ekrana referansla alakasız iki görsel
> girecekti.
>
> **DERS: bir istemi yazmadan önce referansa BÜYÜTEREK bak.** Küçük karede
> "renkli bloklar" gibi görünen şey, büyütünce tamamen başka bir nesne çıktı.

### 11.1 `collection_book.png` — açık kitap + albüm paketleri

Referans: `collections.jpeg`, ekranın ortası (y 620-1250 / 2048).
Sahnenin tarifi:

- Ortada **açık bir kitap**, dik duruyor. Kapağı magenta-pembe, köşelerinde
  turuncu metal kapaklar, sırtında pembe-mor bir kayış ve toka var.
  Sayfaları krem; sol sayfada kırmızı, sağ sayfada mavi bir yer imi şeridi.
  Sayfalarda üç küçük **çerçeveli koleksiyon kartı** duruyor (altın taçlı).
- Kitap **mor silindirik bir kaidenin** üstünde; kaidenin üst yüzü krem,
  gövdesi koyu mor, alt kenarı altın bir halka.
- Kaidenin iki yanında **üç sarılmış albüm paketi** yatıyor: solda mor
  çizgili, sağda mavi çizgili ve yeşil çizgili. Her birinin dört köşesinde
  turuncu kapaklar ve üstünde pembe bir kayış-toka.

```
An open storybook with a magenta pink cover and orange metal corner caps standing upright on a purple cylindrical pedestal with a cream top and a gold rim, its cream pages showing three small framed collectible cards with tiny gold crowns, a red bookmark ribbon on the left page and a blue one on the right, and three wrapped album boxes lying around the base in striped purple striped blue and striped green, each box with orange corner caps and a pink strap and buckle across its lid, glossy 3D plastic toy style, front view, mobile casual puzzle game UI asset, soft studio lighting from top-left, subtle ambient occlusion, strong specular highlight on the pages, thick rounded edges, no outlines, highly saturated candy colors, single centered arrangement on a plain solid light grey background, no text, no letters, no numbers, no drop shadow on the background, square 1024x1024, plenty of empty margin around the object, object occupies only the middle 80% of the frame, clean edges for game asset cutout
```

`art_raw/` yoluna girer (kesim gerekir). Gelince `CollectionScreen`'deki
`Art.Chest` anahtarı `collection_book`'a döner, düzen aynı kalır —
**56. maddedeki plaka ve yazı zaten referansa göre yerinde.**

### 11.2 `icon_album.png` — Koleksiyon SEKME ikonu

Referansta alt çubuktaki koleksiyon sekmesi, kitabın yanındaki albüm
paketlerinin küçük hâli: **altın yuvarlak köşeli çerçeve**, içinde
**pembe-magenta** bir yüz ve o yüzde **iki-iki dizilmiş dört küçük nokta**.
Sandıkla (`icon_chest`) hiç ilgisi yok — kullanıcının "menü ikonu da
alakasız duruyor" bulgusu.

```
A square album case icon with a thick rounded gold frame and a bright magenta pink face, showing four small round studs arranged in a two by two grid on the face, glossy 3D plastic toy style, front view, mobile casual puzzle game tab bar icon, soft studio lighting from top-left, subtle ambient occlusion, thick rounded edges, no outlines, highly saturated candy colors, single centered object on a plain solid light grey background, no text, no letters, no numbers, no drop shadow on the background, square 1024x1024, plenty of empty margin around the object, clean edges for game asset cutout
```

Gelince `MenuShell.Tabs` dizisindeki `Art.Chest` → `Art.Album` olur ve
`UiSkin`'e `icon_album` anahtarı eklenir.

---

## 12. ALT SEKME ÇUBUĞU (2026-08-17)

Kullanıcı isteği: *"alt menü için daha iyi orjinale benzer bir arkaplan
tasarımı ve seçili buton için de görsel lazım"*.

**Şu an bunlar KODLA çiziliyor** ve referanstan ölçülen renklerle
(`MenuShell.BuildTabBar`) makul görünüyor — yani bu iki görsel *zorunlu*
değil, kaliteyi yükseltir. Gelirse yalnız iki satır değişir.

### Neden eski görseller kullanılmıyor

`bar_tabs.png` ve `card_tab.png` zaten var ama **yanlış renkte**: ikisi de
mor (#5B1FB8 civarı), referanstaki çubuk ise mavi-mor (#5140E4, ölçüldü).
Boyama çarpma olduğu için moru maviye çevirmek mümkün değil — çarpım her
zaman daha koyu ve daha mor kalır. Yeni görseller **doğru renkte** üretilmeli.

### 12.1 `bar_tabs.png` — çubuk zemini (9-dilim)

Referanstan ölçülen dikey katmanlar (üstten alta):
`#2D1B87` koyu dış kenar (15 birim) · `#5C4BD8` ince ışık (4) ·
`#231578` koyu oyuk (11) · `#7771F9` üst parlaklık (9) · `#5140E4` gövde.

```
A horizontal glossy 3D plastic bar in blue-violet color hex 5140E4, seen straight from the front, with a bright thin highlight strip along its top edge in lighter blue-violet hex 7771F9 and a darker recessed groove line just above it, flat uniform surface with no pattern, sharp horizontal top edge that runs the full width, mobile casual puzzle game bottom navigation bar, soft studio lighting from top, subtle ambient occlusion, thick rounded plastic feel, no outlines, plain solid light grey background, no text, no letters, no numbers, no icons, wide banner 1024x256, the bar fills the full width edge to edge
```

9-dilim payı: üst 40 px, alt 8 px, yanlar 8 px (yanlarda desen yok, düz uzar).

### 12.2 `card_tab.png` — seçili sekme kartı (9-dilim)

Referanstan ölçüldü: yüzey `#6B65F9`, kenarlık `#291B8C` (18 px kalınlık),
üst köşeler geniş yuvarlak, alt kenar ekranın dışına taşıdığı için düz.

```
A vertical rounded rectangle button in light blue-violet color hex 6B65F9 with a thick dark blue-violet border hex 291B8C, glossy 3D plastic toy style, front view, softly rounded top corners and a flat bottom edge, a soft light reflection gathered in the upper half of the face, empty face with nothing on it, mobile casual puzzle game selected tab card, soft studio lighting from top-left, subtle ambient occlusion, no outlines, plain solid light grey background, no text, no letters, no numbers, no icons, portrait 512x640, plenty of empty margin around the object, clean edges for game asset cutout
```

9-dilim payı: her kenarda ~48 px (köşe yarıçapı kadar).

### 12.3 Gelince ne değişir

`MenuShell.BuildTabBar` içinde çubuğun `CreatePanel` çağrısı
`CreateSlicedPanel(..., UiSkin.Get(Art.TabBar))` olur ve dört `TopStrip`
satırı silinir; kart tarafında `Rim`/`Face`/`Sheen` üçlüsü tek bir
`CreateSlicedPanel(..., UiSkin.Get(Art.TabCard))` olur. Ölçüler (kartın
slotun 1.42 katı olması, üst kenarın ekranın altından %13.48'te bitmesi)
aynen kalır — onlar görselden değil referanstan geliyor.

---

## 13. 2. TUR — BEKLEYEN GÖRSELLER (2026-08-18)

Hepsi `art_raw/` klasörüne ham PNG olarak atılır, sonra
`python tools/import_art.py` işler ve `python tools/check_art.py` kesim
hasarını tarar. Dosya adları ÖNEMLİ: `UiSkin` anahtarları o adlardan geliyor.

**Ortak kural — her istemin sonuna eklenmiş olan kısım neden orada:**
`plain solid light grey background` + `no drop shadow on the background`
kesimi kolaylaştırıyor; `plenty of empty margin` kırpma payı bırakıyor;
`no text, no letters, no numbers` üreticinin kendiliğinden yazı eklemesini
engelliyor (bu projede üç kez oldu ve görselleri kullanılamaz yaptı).

### 13.1 `podium_gold.png` · `podium_silver.png` · `podium_bronze.png` — 12. madde

Referans: `sıralama.jpeg`, kürsü bölgesi. Üç ayrı kaide: krem/bej silindirik
gövde, dikey oluklar, üstünde MOR kadife minder, altın/gümüş/bronz metal
kuşaklar. Birinci daha geniş ve yüksek, minderinde halat işlemesi var.

```
A tall cylindrical award pedestal with a cream ivory body and vertical fluted grooves, a rounded purple velvet cushion on top, and a polished gold metal band around its base and rim, glossy 3D plastic toy style, front view, mobile casual puzzle game leaderboard podium, soft studio lighting from top-left, subtle ambient occlusion, thick rounded edges, no outlines, highly saturated candy colors, single centered object on a plain solid light grey background, no text, no letters, no numbers, no drop shadow on the background, square 1024x1024, plenty of empty margin around the object, clean edges for game asset cutout
```

Gümüş ve bronz için `gold` yerine `silver` / `bronze` yazıp aynı istemi
kullan; gövde ve minder aynı kalsın, yalnız metal kuşak değişsin.

### 13.2 `board_scene.png` — Liderlik arka planı, 14. madde

Referans: `sıralama.jpeg` üst yarısı. Şu an ağaçlar prosedürel
(`MenuSprites.Foliage`) — kabul edilebilir ama referanstaki yaprak kümeleri
çok daha zengin.

```
A wide cartoon park scene seen from the front, with a bright blue sky gradient at the top, two lush leafy trees with rounded clustered foliage at the left and right edges, a trimmed green hedge row across the middle, and a smooth green grass lawn at the bottom, the center of the image left empty and uncluttered, glossy 3D plastic toy style, mobile casual puzzle game background art, soft studio lighting from top, subtle ambient occlusion, no outlines, highly saturated candy colors, no text, no letters, no numbers, no characters, no people, wide banner 1536x1024, clean flat composition suitable for a UI backdrop
```

Bu görsel KESİLMEZ (arka plan), doğrudan `UiKit.CreateCover` ile kullanılır.

### 13.3 `medal_gold.png` · `medal_silver.png` · `medal_bronze.png` — 16. madde

Şu an prosedürel (`MenuSprites.Sunburst` + daire). Gerçek görsel gelirse
`LeaderboardScreen`'deki çelenk/bilezik/yüzey üçlüsü tek `Image`'a iner.

```
A round award medal with a thick scalloped gold rim shaped like laurel petals, a smooth polished gold face in the center, and a bright specular highlight in the upper left, glossy 3D plastic toy style, front view, mobile casual puzzle game rank medal, soft studio lighting from top-left, subtle ambient occlusion, thick rounded edges, no outlines, highly saturated candy colors, single centered object on a plain solid light grey background, no text, no letters, no numbers, no drop shadow on the background, square 512x512, plenty of empty margin around the object, clean edges for game asset cutout
```

### 13.4 `frame_gold.png` · `frame_silver.png` · `frame_bronze.png` — 17. madde

Referansta podyumdaki üç avatarın çerçevesi oymalı ve sıraya göre farklı
metalde; birincininkinin tepesinde kırmızı bir taş var.

```
An ornate square picture frame with thick gold scrollwork corners and a hollow empty center, glossy 3D plastic toy style, front view, mobile casual puzzle game avatar frame, soft studio lighting from top-left, subtle ambient occlusion, thick rounded edges, no outlines, highly saturated candy colors, single centered object on a plain solid light grey background, no text, no letters, no numbers, no drop shadow on the background, square 512x512, plenty of empty margin around the object, transparent hollow middle, clean edges for game asset cutout
```

Altın olanın tepesine `with a small red gem set at the top center of the frame`
eklenir.

### 13.5 `logo_game.png` — 49. madde (BLOCK OUT! yazısı)

> **NOT (marka).** Referanstaki logo Grand Games'in tescilli markası. Proje
> GitHub'da yayınlanacağı için birebir kopyalamak yerine **aynı dilde ama
> kendi çizimimiz** olan bir yazı istiyoruz: aynı blok-harf hissi, aynı altın
> renk, aynı kalın kontur. Kullanıcı yine de birebir isterse
> `oyun açılış.jpeg`'ten kesmek tek satırlık bir değişiklik.

```
The words BLOCK OUT written as chunky three dimensional toy building blocks, thick golden yellow letters with a bright warm gradient, a thick dark navy outline around every letter and a soft drop shadow beneath, playful rounded bubble lettering, slight upward arc, an exclamation mark at the end, glossy 3D plastic toy style, front view, mobile casual puzzle game logo, soft studio lighting from top-left, no background scenery, plain solid light grey background, no extra words, wide banner 1536x768, plenty of empty margin around the lettering, clean edges for game asset cutout
```

### 13.6 `logo_studio.png` — 35. madde (açılış stüdyo yazısı)

> Aynı marka notu geçerli: referanstaki "grand" kelimesi stüdyonun kendi
> markası. Kendi projemiz için nötr bir kelime işareti üretiyoruz; metni sen
> belirle (ör. kendi takma adın), istemdeki `STUDIO` yerine onu yaz.

```
The word STUDIO written in soft rounded lowercase lettering, pure white, thin elegant strokes, wide letter spacing, centered on a deep burgundy red background with a soft radial glow behind the word, minimal clean studio splash screen, no other elements, no icons, no shapes, wide banner 1536x768, plenty of empty margin around the lettering
```

### 13.7 `icon_restart.png` — 38. madde

Referansta üst bardaki yeniden başlat düğmesinin içinde açık lavanta renkli,
saat yönünün TERSİNE dönen dairesel bir ok var (ölçülen renk `#CFC4FF`).
Şu an prosedürel çiziliyor (halka + döndürülmüş kare).

```
A circular arrow icon curving counter clockwise with a single arrowhead, thick rounded stroke, soft light lavender color, glossy 3D plastic toy style, front view, mobile casual puzzle game restart button icon, soft studio lighting from top-left, subtle ambient occlusion, thick rounded edges, no outlines, single centered object on a plain solid light grey background, no text, no letters, no numbers, no drop shadow on the background, square 512x512, plenty of empty margin around the object, clean edges for game asset cutout
```

### 13.8 Gelince ne değişir

| dosya | bağlanacağı yer |
|---|---|
| `podium_*` | `LeaderboardScreen.BuildPodium` — üç katmanlı prosedürel kürsü tek `Image`'a iner |
| `board_scene` | `LeaderboardScreen.BuildPodium` — gökyüzü + çim + ağaç çizimleri silinir |
| `medal_*` | aynı yerde çelenk/bilezik/yüzey üçlüsü tek `Image` olur |
| `frame_*` | podyum avatarlarındaki `Art.AvatarFrame` sıraya göre seçilir |
| `logo_game` | `BootSplash.BuildLoading` — `CreateTitle("Logo", …)` yerine `CreateIcon` |
| `logo_studio` | `BootSplash.BuildStudio` — `CreateLabel("Wordmark", …)` yerine `CreateIcon` |
| `icon_restart` | `GameplayScreen.BuildRestartGlyph` — çizim yerine tek `CreateIcon` |

Düzenler DEĞİŞMEZ: ölçüler görselden değil referanstan geliyor.
