# APK Bulguları — 5. Tur (2026-08-19)

Kullanıcının ikinci geri bildirim turundan çıkan 17 madde. Her maddede
**NE YAPILDI**, **ÖLÇÜM** ve **NASIL DOĞRULANDI** var.

**Durum: 17 kapalı.**

Referans kaynakları (bu turda kullanılanlar):
- `Downloads/Block Out Color Sort Puzzle Levels 1-20 Walkthrough.mp4`
- `Downloads/Block Out! Level 41-…-50 Solution Walkthrough.mp4`
- `OneDrive/Masaüstü/Block Out! Videos/Game over .mp4` ← **kaybetme akışının
  tamamı burada; Türkçe yerelleştirilmiş sürüm**
- `OneDrive/Masaüstü/Block Out! Videos/Block Out! menus,powerups,vs.mp4`

---

## Bu turun genel dersleri

1. **Bir kapı, kapattığı şeyin TAMAMINI kaplamalı.** Kapının derinliği
   duvar bandının %94'üydü ve bar bandın ORTASINA konuyordu; kâğıt üzerinde
   "bandı dolduruyor" görünüyordu. Ama çerçevenin iç pahı barın altında
   kalıyor ve ekranda 33 piksellik mor bir şerit bırakıyordu — oyuncu oraya
   blok sokabildiğini sanıyordu. Geometriyi "ortala ve biraz küçült" diye
   kurmak kenar ayrıntılarını hesaba katmıyor; doğrusu iki UÇTAN tanımlamak.
2. **Saydamlık, nesnenin KENDİ içini de gösterir.** "Yavaşça kaybolsun"
   denince ilk akla gelen alfayı indirmek. Ama saydam materyal derinliğe
   yazmaz; prizmanın üst kapağı, pahı ve yan duvarı üst üste harmanlanıp
   mesh'in iç kenarlarını UZUN ÇİZGİLER olarak gösteriyor. Hedef renk
   arkadaki yüzeyin rengiyse, opak yürütmek ekranda aynı pikselleri üretir
   ve çizgi bırakmaz.
3. **Bir kısıtın nerede saklandığını bilmek.** "Tahtalar dikdörtgendir"
   kuralı hiçbir yerde yazmıyordu; tek bir yardımcı fonksiyonun
   (`RoundedRect`) içinde saklıydı. Veri modeli çoktan serbestti. Bir
   özelliği eklemek çoğu zaman yeni kod yazmak değil, eski bir varsayımın
   nerede kaldığını bulmak.
4. **Bir olayın DUYULMASI görülmesi demek değil.** Buz sayacı azalınca ses
   ve titreşim vardı, GÖRSEL hiçbir şey yoktu. Olay "yapıldı" sayılmıştı;
   oysa sessiz oynayan biri için o hamle hiç gerçekleşmemiş gibiydi.
5. **Küçültmek ile kırpmak farklı şeylerdir.** İç katman bloğun kendi
   mesh'i %58 ölçekle konunca saplamalar da küçülüp dıştakilerle kesişti;
   saplamalar atılınca bu kez düz bir renk lekesi oldu. Doğrusu üçüncüsü:
   ızgara sabit, GÖVDE küçük, kenara sığmayan saplama çizilmiyor.
6. **Varlığın yapısı, animasyonun sınırını çizer.** "BLOCKOUT" logosu TEK
   bir PNG; harflere UV dikdörtgenleriyle bölünüyor. Dilim sıfırdan
   büyütülünce ekranda görünen şey harf değil KÜÇÜLTÜLMÜŞ BİR DİKDÖRTGEN.
   Animasyon eğrisini düzeltmeye çalışmak yanlış uçtan tutmaktı.
7. **Bir özellik "olmalı" diye eklenmez.** Ödüllü reklam düğmesi mobil
   bulmacalarda o kadar yaygın ki referansa bakılmadan eklenmişti.
   Referansın para modeli başka: oyuncuya reklam değil, biriktirdiği
   jetonu harcama seçeneği sunuyor.
8. **Oranı bozan tek sayı, her şeyi bozar.** Kaybetme kartı referanstan
   %19 uzundu; içindeki her şey kart-göreli yerleştiği için hepsi birden
   seyreliyordu. Kullanıcının "bozuk" dediği şey tek tek öğeler değil,
   aralarındaki boşluklardı. Önce kabı ölç, sonra içindekileri.

---

## A. KAPILAR

### A1 — Kapı duvarın TAMAMINI kaplamalı ✅
**Kullanıcı:** "Kapı komple o duvarın yerini almalı, altında bir duvar parçası
kalmamalı; ama oraya blok sokabiliyoruz, bu da oyunu mantıksızlaştırıyor."

**ÖLÇÜM — referans** (Levels 1-20, 00:12, üstteki kırmızı kapı): kapı
158×55 piksel, yanındaki duvar bandı 47 piksel. Kapının üst kenarı duvarın
üst kenarıyla aynı (1 piksel fark), ALT kenarı ise duvarın iç kenarından
**7 piksel İÇERİDE**. Hücre 79 piksel → taşma hücrenin %9'u.

**ÖLÇÜM — bizde:** kapı 74 piksel, duvar bandı 106 piksel; kapının alt
kenarı duvarın iç kenarından **33 piksel YUKARIDA**.

**NE YAPILDI.** `BarDepth` ve `OutwardOffset` iki UÇTAN tanımlandı:
dış kenar çerçevenin dış kenarı (+0,02 z-fighting payı), iç kenar oyun
alanının 0,09 hücre içerisi.

**NASIL DOĞRULANDI.** 2. bölümde kapı 121 piksel; alt kenarı duvarın iç
kenarından 12 piksel içeride; kapının altındaki 30 piksellik şeritte mor
YOK, doğrudan tahta zemini (27,27,76) başlıyor. 35. bölümde de aynı:
duvar (kapısız satır) x 73..162, kapı x 73..168 — dış kenar birebir aynı,
iç kenar 6 piksel içeride.

### A2 — Ok kapıdan taşıyor ✅
**Kullanıcı:** "Ok işareti kapıdan taşmış gibi gözüküyor; kapıyı tam
ortalasın, biraz daha küçük olsun."

**ÖLÇÜM** (aynı kare): ok 27×15 piksel, hücre 79 → genişlik hücrenin %34'ü,
DERİNLİK %19'u; oran 1,8 (BASIK). Bizde oran 1,05'ti (neredeyse eşkenar) ve
derinlik 0,44 hücre — kapının kendi derinliğinin neredeyse tamamı.

**DERS (bir şekli tek sayı ile küçültmek şeklini düzeltmez):** "Ok büyük"
denince ilk refleks `arrowSize`ı kısmak. Sorun boyut değil ORANDI;
küçültmek oku kapıya sığdırırdı ama yine eşkenar kalırdı.

**NASIL DOĞRULANDI.** Ok 54×29 piksel, hücre 160 → %34 × %18, oran 1,86.

### A3 — Sönerken uzun çizgiler ✅
**Kullanıcı:** "Bazen kapı kaybolurken üzerinde uzun çizgi işaretleri
görüyoruz."

**TEŞHİS.** Sönme `ViewKit.Translucent` kullanıyordu; o materyal derinliğe
YAZMAZ. Kapı yarı saydamken prizmanın üst kapağı, pahı ve yan duvarı ekranda
üst üste harmanlanıyor, iki kez boyanan yerler koyulaşıyor ve mesh'in iç
kenarları uzun çizgiler olarak görünüyordu.

**NE YAPILDI.** Sönme artık OPAK: renk çerçeve rengine yürüyor. Ölçülen ara
kare zaten kapı-çerçeve arası %51'lik düz bir karışımdı, yani ekranda aynı
pikseller — ama nesne kendi içini göstermiyor.

### A4 — Aydınlanma çok zayıf ✅
**Kullanıcı:** "Blok soktuğumuzdaki o kapının aydınlanması, ışık saçması
bizdeki çok zayıf kalmış."

**NE YAPILDI.** Parlama yalnız kapının kendi pikselleriydi. Artık kapının
büyütülmüş KATKILI (additive) kopyası bir hale yayıyor; hale hem parlıyor
hem genişliyor. Süre 0,26 → 0,34 saniye.

**DERS (parlama ALANLA orantılı okunur):** Bir vurgunun gücü sadece
kontrastından değil, kapladığı alandan gelir. Katkılı harmanlama seçildi
çünkü alfa harmanlama sönerken arkadaki yüzeyi KARARTIR — ışık öyle
davranmaz.

---

## B. TAHTA

### B5 — Kullanılmayan alanlar kesilmeli ✅
**Kullanıcı:** "Bazı levellerde boşluk olmalı, kullanılmayan kısımlar
kesilmeli şeklinde söylediğim mantığı yine yapamamışsın, hatta ekstra
doldurmuşsun oraları. Örneğin level 8'de sol alt kısmın olmaması gerekiyor."

**TEŞHİS.** Veri modeli ZATEN maskeliydi (`BoardModel.IsPlayable`; level 8'in
JSON'unda `"...XXX"` satırları vardı). İki şey yanlıştı:
1. Çerçeve `RoundedRect` ile üretiliyordu — her tahta dikdörtgen bir duvarla
   çevriliyordu.
2. Oynanamaz her hücre `BuildDeadZones` ile çerçeve renginde KABARIK bir
   kütle olarak çiziliyordu — yani "kesilmiş" değil "doldurulmuş".

**NE YAPILDI.** Çerçeve artık `BrickSilhouette` ile maskenin silüetini
izliyor (blok silüetini üreten aynı kod; dört halka aynı çağrının farklı
`inset`/`radius` değerleriyle üretildiği için nokta sıraları eşleşiyor).
Kenara dayanan boşluklar hiç çizilmiyor; yalnız İÇ DELİKLER kütle kalıyor.
Izgara da maskeyi izliyor: bir çizgi parçası ancak iki yanı da oynanabilir
sınırlarda çiziliyor, perçin ancak dört hücre de oynanabilirse.

**NASIL DOĞRULANDI.** 8. bölümün kesik bölgesinde (x 100-450, y 900-1500)
arka plandan farklı piksel **1127 / 210000 = %0,5** — o da yalnız kenar
pikselleri. Hayalet ızgara yok.

### B6 — Blok içinde blok ✅
**Kullanıcı:** "Blok içinde blok feature de düzgün değil, hâlâ düz renk kare
var blok yerine."

**ÖLÇÜM** (Levels 1-20, 07:04, 15. bölüm): dıştaki pembe blok 140×142
piksel, içindeki mavi katman 98×94 → **%70 × %66**. İç katman SAPLAMALI;
dış blok yalnız KENAR saplamalarını gösteriyor.

**NE YAPILDI.** İç katman saplamalı bir blok mesh'i; dış blok onun kapladığı
bölgede saplama basmıyor (yeni `suppressStudsInside`). Oran %58 → %68.
İç katman rengi %86'ya kısılıyordu ("tozlu" görünüyordu) — tam doygunluğa
alındı. Açık hat beyaza %55 yerine %42.

**Yol boyunca bulunan iki hata:**
- `Build`'de `Mathf.Max(0f, grow)` negatif büyütmeyi sessizce yutuyordu;
  iç blok hiç küçülmüyor, dış bloğu tamamen örtüyordu.
- Hat ve panel aynı yükseklikte dolu levhalar olduğu için üst yüzleri
  çakışıp yatay şeritler üretiyordu; hat 0,008 aşağı alındı.
  **Doğrulama:** panel bölgesinde ani satır renk sıçraması 5/229.

### B7 — Buz her hamlede çatlamalı ✅
**Kullanıcı:** "Buz kırılıyorken her hamle yaptığımızda buz parçalanma
efekti gelsin demiştim, çalışmıyor şu anda."

**BULUNAN SEBEP.** `IceDecremented` olayına yalnız titreşim ve ses bağlıydı;
GÖRSEL hiçbir şey yoktu. Parçacıklar sadece buz TAMAMEN kırılınca
(`IceShattered`) çıkıyordu.

**NE YAPILDI.** Her azalmada küçük bir çatlama patlaması (5+2n kırıntı +
3 beyaz kıvılcım, 0,10 sarsıntı) ve buz kabuğu bir an titreyip beyazlıyor.
Kırılma anı ayrışıyor: orada iki katmanlı büyük patlama ve 0,30 sarsıntı.

### B8 — 50. bölümün yön blokları ✅
**Kullanıcı:** "Seviye 50'deki ok bloklar daha iyi ama orijinaldeki tarzda
değil, hâlâ biraz daha benzetilebilir."

**ÖLÇÜM.** En iyi kare 41-50 yürüyüşünün 12:32'sindeki **"New Item
Unlocked!"** paneli — yön bloğu orada tek başına ve büyük. Blok 145×144
piksel, ok 60×130:

| ölçü | referans | bizde (önce) |
|---|---|---|
| baş genişliği | %41 | %52 (çok iri) |
| gövde kalınlığı | %23 | %21 (doğru) |
| ok boyu | %90 | %56 (çok kısa) |

Tek bir ölçek çarpanı ikisini birden düzeltemezdi; baş ile boy ayrı
ayarlandı. Okun DOLGUSU da referansta blok rengiyle neredeyse aynı; beyaza
%34 karıştırmak oku "yapıştırılmış açık bir çıkartma" gibi gösteriyordu →
%10.

---

## C. KUTLAMA VE SONUÇ EKRANLARI

### C9 — "BLOCKOUT" yazısı yapboz gibi geliyor ✅
**Kullanıcı:** "Bölüm geçince çıkan BLOCKOUT yazısı yapboz gibi parça parça
geliyor, kastettiğim bu değildi; orijinal oyundakini incele, birebir aynısını
yap, gelme sırası da önemli."

**SEBEP.** Logo TEK bir görsel ve harflere UV dikdörtgenleriyle bölünüyor.
Dilim sıfırdan büyütülünce ekranda görünen şey harf değil KÜÇÜLTÜLMÜŞ BİR
DİKDÖRTGEN: içinde harfin ve mor zeminin birer parçası, iki yanında dümdüz
kesik kenarlar.

**REFERANS ÇÖZÜMLENDİ** (01:31 kutlaması, 15 fps'te 135 kare): siyah perde →
harfler soldan sağa **0,07 saniye arayla** tek tek → logo büyüyerek son
boyunu aşıyor → oturuyor → roketler → patlamalar → konfeti. Her harfin KENDİ
mor zemini var, kesik kenar yok.

**NE YAPILDI.** Tek parça görselle birebir yapmak mümkün değil; kesik kenar
GÖRÜNMEZ kılındı: dilim %86'dan başlıyor (bir karede bile fark edilmiyor) ve
asıl geliş ALFA ile oluyor. Logo 0,42 → 1,12 → 1,00.

**HAVAİ FİŞEK.** Referansta her patlamanın altında bir roket izi var ve
ekranda aynı anda ÜÇ iz birden yükseliyor. Roket 6 → 11, aralık 0,22 → 0,13;
gökte kendiliğinden beliren patlama 10 → 4.

### C10 — PERFECT kartındaki para ✅
**Kullanıcı:** "Level sonu perfect para gelme yeri de daha çok benzetilsin,
şu an iyi durmuyor."

**TEŞHİS.** Işık huzmesi 16 ışınlı TAM DAİREYDİ ve alfa 0,20'ydi; mor
panelin üstünde GRİ bir çark gibi duruyordu.

**ÖLÇÜM** (Levels 1-20, 02:53, 8. bölümün PERFECT kartı): ışınlar yığının
üstünden yukarı açılan bir YELPAZE; sayısı az, rengi panelin morundan bir
tık açık. Kart dolgusu (65,49,192) — bizimki (77,62,237) idi, belirgin
biçimde parlak ve doygun.

**NE YAPILDI.** 9 ışın, 118° yelpaze, alfa 0,11; kart moru ölçülen değere
çekildi.

**DERS (bir vurgunun görevi dikkat çekmek değil YÖNLENDİRMEK):** Tam daire
ışın bakışı merkeze değil çevreye dağıtır. Yelpaze yukarı bakar, yani
"buradan yukarı bir şey çıkıyor" der.

### C11 — "Watch Ad · Double Reward" ✅ (KALDIRILDI)
**Kullanıcı:** "Watch ad double reward orijinal oyunda var mı, yoksa kaldır;
varsa buton ve yeri kötü, bozuk duruyor."

**REFERANS TARANDI** (`Game over .mp4` tamamı, 2 fps'te 50 kare): kaybetme
akışı üç panel —
1. **"Süre Doldu"**: kırmızı başlık, çalar saat görseli, "+30 saniye", yeşil
   düğme **"Zaman Ekle 🪙900"**.
2. **"Devam Et?"**: kırık kalp, "1 can kaybedeceksiniz!", yeşil düğme
   **"Oyna 🪙900"**.
3. **"BAŞARISIZ"**: jeton yığını + üstünde kırmızı çarpı, "Ödüller x3"
   rozeti, yeşil **"Yeniden Dene"**.

PERFECT kartında da yalnız "Continue" var. **Hiçbirinde reklam düğmesi
YOK**; devam etmenin bedeli JETON.

**NE YAPILDI.** Reklam düğmesi tamamen gizlendi.

### C12 — BAŞARISIZ ekranının ölçeği ✅
**Kullanıcı:** "Yine failed ekranı sıkıntılı, orijinaliyle çok benzer değil,
bozuk gözüküyor; scale, yapı, tarz olarak birebir benzetilmesi gerekiyor."

**ÖLÇÜM** (`Game over .mp4` 18. saniye; 384×832 kare, ekran oranına
normalize, y aşağıdan yukarı):

| öğe | x | y |
|---|---|---|
| panel | 0.049..0.951 | 0.254..0.700 |
| yeşil düğme | 0.266..0.734 | 0.308..0.389 |
| sayı hapı | 0.344..0.711 | 0.422..0.483 |
| "Ödüller x3" | 0.370..0.628 | 0.393..0.418 |
| "Seviye 54" | — | 0.608..0.644 |

Bizim kartımız 0.255..0.786 idi: referanstan **%19 daha uzun**. Panel
dolgusu da ölçüldü: **(95,32,186)** — kazanma panelinin morundan (65,49,192)
BAŞKA bir renk. "Ödüller x3" rozeti koyu lacivert değil TURUNCU hap, yazısı
beyaz.

---

## D. YARDIMCILAR (POWER-UP)

### D13 — Adet rozeti ✅
**Kullanıcı:** "Powerupların adet göstergesi daha küçük, koyu gölgeli şık bir
kırmızı ile gösteriliyor, değiştirilmesi lazım."

**ÖLÇÜM** (Levels 1-20, 02:32, sol yardımcı düğmesi): düğme 112×89 piksel,
rozet 36×34 → düğme genişliğinin **%31'i**. Rozetin sağ kenarı düğmenin sağ
kenarının bir tık içinde (x %64..%95,5); gövdesinin üçte ikisi düğmenin ALT
kenarının altında. Renkler: gövde `#DC1612`, kenar `#7C0200` (altta
kalınlaşan koyu gölge), rakam saf beyaz, üst kenarda ince bir mercan
parlaması `#FF5E40`.

Bizdeki rozet düğmenin **yarısı** kadardı ve sağa taşıyordu.

### D14 — Fiyat kapsülü ✅
**Kullanıcı:** "Powerupların para birimi yazdığı kısmın da güncellenmesi
lazım."

**ÖLÇÜM** (`Game over .mp4` 2. saniye, üç yardımcının fiyatı): hap PARLAK
mavi-mor `#5A45F1`, rakam ÇOK KOYU lacivert `#131046` — yani açık zemin
üstünde koyu yazı. Jeton hapın SOL ucunda ve haptan BÜYÜK, dışına taşıyor.

Bizimki koyu hap + açık yazıydı, yani tam tersi; koyu HUD'un içinde
kayboluyordu.

---

## E. MAĞAZA

### E15 — Brandanın altındaki mavi alan ✅
**Kullanıcı:** "Mavi brandanın altındaki mavi yer aşağıya kaydırınca bizimle
beraber geliyor; orijinalde o yer sabit."

**ÖLÇÜM.** İki referans karesi (menüler videosu 10. ve 12. saniye)
farklandı: içeriğin kaydığı ilk satır ekranın **%13,5'i**. Bizde **%16,4**.

**TEŞHİS.** Bant zaten kaydırılmıyordu — "sabit mi" sorusunun cevabı
doğruydu; yanlış olan KALINLIĞIYDI. Festonlar bittikten sonra 46 birimlik
DÜZ bir lacivert bant daha vardı ve içerik ancak ondan sonra başlıyordu.
Referansta öyle bir bant yok: festonun kenarı koyulaşıyor ve hemen içerik
geliyor.

**DERS (sabit bir katman da yanlış YERDE olabilir):** Kullanıcı hareketi
değil, o bandın varlığını görüyordu.

**NASIL DOĞRULANDI.** İki kaydırma konumu edit modunda yakalandı; ilk
değişen satır 274/1920 = **%14,3**.

### E16 — Bölüm şeritleri ✅
**Kullanıcı:** "Packs ve special offers kısmının şeritleri var, bitiş sınırı
gibi orijinalinde; onu da yapalım."

**ÖLÇÜM** (menüler videosu 07. saniye): başlık düz bir kapsül değil
KURDELE — sağ ucundan, kapsülün dışına taşan küçük bir dil ve onun koyu
gölgesi. Kitap ayracı gibi.

### E17 — Jeton kapsülü, paket yazıları, gölgeler ✅
**Kullanıcı:** "Gold yerinin arka planı, dış çizgisi daha fazla ve gölgeli
gözükmeli, ayrıca ufak bir radiusu daha fazla; bununla beraber coin ikonu o
arka planının başlangıç noktasında." / "Paket yazıları biraz daha solda,
dikdörtgenin başlangıcında." / "Yeşil butonların bir tık daha fazla gölgesi
var, mor kutuların da alt kısmında gölgeler daha belirgin."

**NE YAPILDI.**
- Jeton kapsülü üç katmana çıktı (gölge + koyu kenar + krem yüz); jeton
  kapsülün sol ucuna alındı ve ondan büyük.
- Paket adı: kutu zaten soldaydı ama YAZI ortalıydı; kısa adlar ortaya
  kaçıyor, uzun adlar sola dayanıyordu. Hizalama sola alındı.
  **DERS:** Bir etiketi sola taşımak yetmez; içindeki yazının da sola yaslı
  olması gerekir, yoksa konum yalnız en uzun metin için doğru olur.
- Yeşil düğmeye dış gölge; koyu yeşil taban %16 → %22.
- Mor karta dış gölge + bandın alt kenarına koyu şerit.

---

## Açık kalanlar

- **A2 (4. tur)** — yazı/panel boyutları: kullanıcının kendi işi.
- Titreşim gerçek cihazda doğrulanmadı (manifest izni eklendi, APK gerekiyor).
- 27 bölümün tasarımı referanstan değil, yaklaşık.
- Müzik yok.
- `DeviceErrorOverlay.Enabled` yayından önce `false` yapılacak.
