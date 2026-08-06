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

- **1024×1024**, PNG, **saydam arka plan**
- Tek nesne, ortalanmış, kenarlara ~%8 boşluk
- Üstüne **yazı/rakam bastırma** — sayılar oyunda TMP ile yazılıyor
- Gölgeyi arka plana bastırma (kod kendi gölgesini koyuyor)

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
