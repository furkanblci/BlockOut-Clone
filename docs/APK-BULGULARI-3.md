# APK Bulguları — 4. Tur (2026-08-19)

Kullanıcının yeni APK testinden çıkan 44 madde. Numaralar kullanıcının kendi
gruplamasını (A…L) korur. Her maddenin altında **NE YAPILDI** ve **NASIL
DOĞRULANDI** yazıyor — 2. turdaki düzenin aynısı.

**Durum: 43 kapalı / 1 kullanıcıda (A2).**

Referans kaynakları:
- `Downloads/Block Out Color Sort Puzzle Levels 1-20 Walkthrough.mp4`
- `Downloads/Block Out! Level 41-…-50 Solution Walkthrough.mp4`
- `OneDrive/Masaüstü/Block Out! Videos/` → `ana ekran.jpeg`, `journey.jpeg`,
  `sıralama.jpeg`, `market.jpeg`, `profil.jpeg`

---

## Bu turda çıkan dört genel ders

1. **Ekrandaki gri, dosyadaki gri değildir.** Tahtanın ızgara çizgileri
   görüntüleyicide açık gri görünüyordu ve bir saat "ızgara materyali bozuk"
   diye arandı. Piksel piksel ölçüldüğünde o bölgede 90'ın üstünde tek bir
   nötr piksel bile yoktu: görüntüleyici koyu laciverti aydınlatıyordu.
   **Karanlık bölgeleri gözle yargılama, say.**
2. **Kırpma "biraz eksik"i "hiç yok"a çevirir.** Alt sekme çubuğundaki seçili
   sekmenin adı hiç görünmüyordu. Kutu 32,83 birimdi, yazının en küçük satır
   yüksekliği 33,6; `overflowMode = Truncate` satırı sığdıramayınca TAMAMEN
   atıyordu (`text.bounds` extents = 0,0,0). Renk, alfa, sıra, materyal hepsi
   doğruydu — eksik olan 0,8 birimdi.
3. **Bir süsün hatası ana akışı kilitlememeli.** Kutlama coroutine'inde bir
   `NullReferenceException` çıkınca `done` geri çağrısı hiç çalışmadı ve
   PERFECT kartı ASLA açılmadı: oyuncu bölümü bitiriyor, ekranda siyah perde
   kalıyordu. Artık hem o dal null'a dayanıklı hem de bir güvenlik ağı var.
4. **"Dışarısı" nerede başlıyor?** Üreteç makinesi tahtanın kenarından
   hesaplanıp "dışarı" konuyordu; ama arada 0,52 hücrelik çerçeve var ve
   makine onun içine giriyordu. Bir nesneyi dışarı koyan kod, görünen kenarın
   kalınlığını da bilmek zorunda.

---

## A. GENEL / UI

### A1 — Oyun ikonu yok ✅
**NE YAPILDI.** `tools/make_icon.py` oyunun kendi paletinden (ColorPalette.asset)
çark biçiminde dört kabartmalı tuğla çiziyor; `tools/make_icon_layers.py`
Android 8+ için ayrı ön/arka katman üretiyor (ön plan içeriği görselin orta
%66'sında — sistem maskesi dışarısını kırpıyor). `AppIconTool` üç PNG'yi
projeye alıyor, sıkıştırmayı kapatıyor ve legacy/round/adaptive yuvalarını
dolduruyor. `AndroidBuildTool.ApplySettings` de onu çağırıyor, yani ayar
sıfırlansa bile derlemede geri geliyor.

**NASIL DOĞRULANDI.** `PlayerSettings.GetPlatformIcons` üç tür için de
**6/6 yuva dolu** dedi.

### A2 — Metin ve panel boyutları
**KULLANICI YAPACAK.** Dokunulmadı.

### A3 — Ana sayfa → Mağaza geçişi ✅
**NE YAPILDI.** `MenuShell.ShowStepped` eklendi: hedef bir sekmeden uzaksa
aradaki sekmelere gerçekten uğruyor (ana ekran → sıralama → mağaza), her
durakta ekran kurulup kayıyor. Jeton sayacı, jeton simgesi, artı düğmesi ve
can panelindeki "mağazaya git" bağlantısının hepsi buna bağlandı.

**NASIL DOĞRULANDI.** Kod yolu: `ShowStepped` → `StepThrough` her adımda
`Show` çağırıyor ve `SlideSeconds * 0.72` bekliyor.

### A4 — Haptikler ÇALIŞMIYOR (KRİTİK) ✅
**NE YAPILDI.** Kök sebep: **Android manifestinde `VIBRATE` izni yok.** Oyunun
titreşimi `android.os.Vibrator`ü JNI ile dize adlarıyla çağırıyor; Unity izni
yalnız derlemeye giren kodda `Handheld.Vibrate` referansı GÖRÜNCE ekliyor,
bizim çağrımızı statik tarama göremiyor. İzin olmayınca `vibrate()` bir
`SecurityException` atıyor, kodun try/catch'i onu yutuyor ve **tek bir
titreşim bile çalışmıyor** — hata da görünmüyor.

`AndroidManifestPatcher` (IPostGenerateGradleAndroidProject) üretilen
manifeste izni ekliyor. Ana manifesti sahiplenmek yerine bu yol seçildi:
`Assets/Plugins/Android/AndroidManifest.xml` koymak activity, tema ve
Unity'nin meta-data'larını da devralmak demekti.

Ayrıca kapsam genişletildi: blok alma/bırakma, reddedilen dokunuş, emilme,
katman soyulması, buz sayacı, buz kırılması, perde açılması ve tahta
temizlenmesi. `GameKit.Services.Haptics.Active/Tap` statik kapısı eklendi ki
her çağrı yerine referans taşımak gerekmesin.

**NASIL DOĞRULANDI.** Kod derlendi; izin enjeksiyonu ancak gerçek bir Gradle
derlemesinde görülebilir. **Kullanıcının bir sonraki APK'sinde titreşim
gelmezse bakılacak ilk yer manifestin kendisi** (`unityLibrary/src/main/`).

### A5 — Can butonu ✅
**NE YAPILDI.** Can doluyken (ve sınırsız can hakkı yokken) panel artık
AÇILMIYOR; sayacın plakası ve kalp simgesi birlikte kısa bir büyü-küçül
yapıyor, ses ve hafif titreşim veriyor. Panelin söyleyecek bir şeyi yokken
açılması, oyuncuya bildiği şeyi söylemek için bir dokunuş daha yaptırmaktı.

### A6 — Alt menü ikonları ✅
**NE YAPILDI.** Seçili sekmenin ikonu kartın ortasına oturdu.
**ÖLÇÜM** (`ana ekran.jpeg`, 946×2048; düğme yüksekliği ekranın %9,5'i =
194,6 piksel): ikon merkezi düğme yerelinde 0.807, yazı 0.293. Eski değer
0.70-1.40 idi, yani ikonun tepesi kartın üst kenarına yapışıyordu.

**Yol üstünde bulunan ikinci hata:** seçili sekmenin ADI hiç
görünmüyordu (bkz. yukarıdaki 2. ders). Kutu 0.155-0.425'e büyütüldü,
`fontSizeMin` 24 → 20.

**NASIL DOĞRULANDI.** Beş menü ekranı yakalandı; "Home", "Journey",
"Leaderboard", "Shop" etiketleri ekranda.

---

## B. JOURNEY

### B7 — Tamamlanmış bölgede yeşil tik ✅
**NE YAPILDI.** Tik zaten çiziliyordu ama `check_green` PARLAK yeşil bir
tikti ve aynı parlaklıktaki yeşil düğmenin üstünde eriyip kayboluyordu.
Referansta (`journey.jpeg`) tik düğmeye OYULMUŞ gibi koyu yeşil. Tik rengi
`#14 5D 0B` yapıldı.

### B8 — Yeşil dikdörtgendeki sayı ✅
**NE YAPILDI.** Düğmede artık hiçbir durumda sayı yok (tamamlandıysa tik,
değilse "Play"). Ayrıca borunun üstündeki seviye rozeti KALDIRILDI:
referansta yok ve borunun ışıklı boyu zaten aynı bilgiyi veriyor — rozet
tam da okunması gereken sınırı örtüyordu.

### B9 — Kapsüldeki yazı koyu mor ✅
**ÖLÇÜM** (`journey.jpeg`, "Seviye" harflerinin gövde pikselleri):
**#1E1065** (30, 16, 101). Kapsül yüzeyi #5846E8 — yani yazı zeminden KOYU.
Bizdeki #BABDF2 ondan AÇIKTI. Etiket koyu, değer beyaz: hiyerarşi tek renk
kararıyla kuruluyor.

---

## C. PROFİL

### C10 — Düzenleme ikonu ✅
**NE YAPILDI.** Rozetin içinde 45° döndürülmüş beyaz bir KAPSÜL vardı; iki
ucu da yuvarlak olduğu için ekranda "beyaz bir oval" okunuyordu. Yeni
`GameKit.UI.UiSprites.Pencil` işaretli mesafe çizimi: düz kesimli gövde +
GERÇEK sivri uç. Silgi bandı denendi ve kaldırıldı — 40 piksellik bir
rozette üçüncü parça şekli anlaşılır kılmıyor, kirletiyor.

---

## D. SIRALAMA

### D11 — Sekme alanının arka planı ✅
**ÖLÇÜM** (`sıralama.jpeg`, sol kenardan dikey tarama):
y 280-430 → `#205DF3 → #276FF9`, y 440+ → yeşil çim. Yani sekmelerin
arkasındaki mavi ayrı bir şerit DEĞİL, sahne görselinin GÖKYÜZÜ. Bizde sahne
556'da başlıyor, üstüne elle düz mavi bir bant boyanıyordu.
Sahne artık başlığın hemen altından kürsü bandının altına kadar tek parça.

### D12 — Sıralama listesi arka planın altında ✅
**NE YAPILDI.** Sahne ekranın İLK çocuğu oldu; sekmeler, kürsüler, satırlar
ve sabit "You" satırı ondan sonra kuruluyor, yani hepsi kendiliğinden üstünde
çiziliyor.

**NASIL DOĞRULANDI.** Ekran yakalandı: gökyüzü sekmelerin arkasında, sekiz
satırın hepsi görünür.

---

## E. MAĞAZA

### E13 — "Best Value"/"Popular" şeritleri ✅
**NE YAPILDI.** Şerit tek düz pembe dikdörtgendi. Üç katman oldu: koyu dış
çizgi, pembe yüzey ve üst kenarda ince ışık. Kontur ayrı bir görsel
gerektirmiyor — aynı dikdörtgenin biraz büyük ve koyu kopyası.

### E14 — "%90 OFF" şeriti ✅
**NE YAPILDI.** Flamanın arkasına aynı flamanın koyu ve biraz büyük kopyası
kondu.

### E15 / E16 — Tentenin bitişi ve mavi→kahverengi geçişi ✅
**ÖLÇÜM** (`market.jpeg`, iki dikey tarama): feston ortasında y 250-316,
festonlar arasında y 290-316 → `#062387`; kahverengi duvar ancak y 390'dan
sonra. Yani tentenin arkasında koyu lacivert bir levha var ve feston
çentiklerinin içinden o görünüyor. Levha + altına yumuşak bir düşüş eklendi.

---

## F. OYUN İÇİ HUD

### F17 — Yeniden başlat / duraklat düğmeleri ✅
**ÖLÇÜM** (Levels 1-20, 00:12): dış kenar `#2A1D8C`, gövde `#5D46FC`,
simge `#D2C8FF`. Gövde ile simge arasındaki parlaklık farkı referansta 2,4
kat, bizdeki soluk leylakta 1,2 kat. Hazır `btn_square` sprite'ı bırakıldı,
düğme üç katman olarak kuruldu.

### F18 — Yardımcı ikonları ✅
**ÖLÇÜM** (aynı kare, düğmenin ortasından dikey tarama):
`#1C7521` ince üst kenar → `#46F037` üst alan → `#01AB00` alt gövde →
son ~10 pikselde `#005506 → #003B09` koyu bant. Bizde üç katman vardı ama
sıralaması TERSTİ (iç kuyu gövdeden koyuydu) ve aşağıda hiç koyu bant yoktu.

### F19 — Para birimi barı ✅
**NE YAPILDI.** Fiyat kapsülü sarıydı; sarı zemin üstündeki altın jeton
simgesini yutuyordu. Mor zemin (HUD'un kendi rengi) + %25 daha kalın kutu.

---

## G. KAPILAR

### G20 — Kapılar köşelerden taşıyor ✅
**NE YAPILDI.** Kapı `PrimitiveType.Cube` idi; köşeye dayanan bir kapı
çerçevenin yuvarlak köşesinin dışına sivri bir dilim olarak taşıyordu.
Kapı artık yuvarlak köşeli prizma (`PrismMeshBuilder`), uçları barın dar
kenarının yarısı kadar yuvarlak. Çerçeve köşe yarıçapı 0.6 → 0.32.

### G21 — Level 1 kapı konumu ✅
`level_001.json` kuzey kapısı x 0 → 1.

### G22 — Kapı kaybolma efekti ✅
**NE YAPILDI.** Kapı ghost rengine solup EKRANDA KALIYORDU; oyuncu onu hâlâ
kullanılabilir sanıyordu. Artık alfası 0'a inip renderer kapanıyor. Eskiden
kaçınılan "duvarda boşluk" korkusu yersizdi: `BoardFrameMeshBuilder` çerçeveyi
KESİNTİSİZ bir halka olarak örüyor, kapı saydamlaşınca altından o çıkıyor.

### G23 — Bloğun kapıya girme efekti ✅
**NE YAPILDI.** `GateView.PlayAbsorbFlash`: kapı beyaza doğru parlıyor ve
%9 şişiyor (0,26 sn). Emilmede ve katman soyulmasında `GateSystem` çağırıyor.
Blok tarafındaki beyaz parlama + iri kırıntı 3. turda yapılmıştı.

### G24 — Buzlu kapı modeli ✅
**NE YAPILDI.** Kapı buzu artık blok buzundan AYRI bir materyal
(`ViewKit.GateIce`, `#B3E6FA` — neredeyse beyaz); blok buzu doygun camgöbeği
kalıyor. İki farklı kural artık iki farklı görünüm.

### G25 — Buzlu kapı parçalanma efekti ✅
**NE YAPILDI.** `FXService.IceBurst` iki katmanlı: hızlı/küçük/kısa ömürlü
BEYAZ toz (kırılmanın anı) + iri/yavaş CAMGÖBEĞİ kristaller (kırılan madde).
Parça sayısı kırılan şeyin büyüklüğüne bağlı (18 + hücre×6). Buz kırılınca
kapı ayrıca bir kez parlıyor.

### G26 — Outline sorunu ✅
**NE YAPILDI.** Blok konturu için bkz. H29. Kapıya, barın bir tık büyütülmüş
KOYU kopyası eklendi: çerçeveyle kapı arasındaki sınır — özellikle
köşelerde — artık çiziliyor.

---

## H. BLOKLAR

### H27 — Aynı renkli komşu bloklar ayırt edilemiyor (KRİTİK) ✅
**KÖK SEBEP.** `BlockVisualConfig.asset` içinde `brickInset: 0` idi ve tuğla
gövdesi hücre hücre KESKİN kutulardan örülüyordu. Yan yana iki blok arasında
ne boşluk ne de kırılan bir hat kalıyordu.

**NE YAPILDI.** Yeni `BrickSilhouette`: hücre maskesinden kenar takibiyle
çevre çizgisi çıkarılıyor, gönye ile içeri kaydırılıyor, her 90° köşe
(dışbükey VE içbükey) yay ile yuvarlanıyor, üst yüz kulak kırpma ile
üçgenleniyor. `brickInset` 0.032, `brickCornerRadius` 0.16.
Temas gölgesi de silüetten üretiliyor ve dışa 0,035 taşıyor — iki komşu
bloğun koyu hatları birleşip tek net ayrım çizgisi veriyor.

### H28 — Blok 3D kenarları ✅
**NE YAPILDI.** Pah artık GEOMETRİ: `brickChamfer` 0.06 → 0.13 ve yan yüz
tonu (`toneBodySide`) 1.0 → 0.62, yani üst yüzden (0.8) KOYU. Eskiden yan
yüz üst yüzden parlaktı, yani bant bir ışık şeridi gibi okunuyordu.

### H29 — Seçim outline'ı komşuya taşıyor ✅
**KÖK SEBEP.** Kontur `localScale = 1.04` ile üretiliyordu; ölçek merkezden
çalıştığı için taşma bloğun BOYUYLA orantılıydı (2 hücrede 0,04 — 6 hücrede
0,12) ve bloklar birbirine değdiği için büyük bloklarda komşunun üstüne
biniyordu. Artık `GetOutlineShell` silüeti dışa SABİT `outlineWidth` (0.05)
kadar kaydırıyor; ölçek birde kalıyor.

### H30 — Level 15 iç içe bloklar ✅
**NASIL DOĞRULANDI.** 15. bölüm yakalandı: bloklarda dış renk çerçeve, ortada
gömülü iç renk paneli ve arasında açık kenar çizgisi görünüyor.

### H31 — Level 50 ok blokları ✅
**NE YAPILDI.** Eksen kısıtlı bloklar artık SAPLAMASIZ (referansta pürüzsüz
karo) ve ok bloğun KENDİ renginin açığı; altında aynı rengin koyusundan bir
oluk hattı var. Ok alçak bir prizma, düz levha değil. Ölçüler 50. bölümün
mavi 1×3 bloğundan: gövde kısa kenarın ~%26'sı, baş genişliği ~%62'si.

---

## I. LEVEL TASARIMLARI

### I32 / I33 / I34 — Boş alanlar görünüyor ✅
**KÖK SEBEP.** Kod zaten doğru şeyi yapıyordu — zemin yalnız oynanabilir
hücrelere örülüyordu. Ama oynanamaz bölgede kalan şey ARKA PLAN oluyordu:
ızgara çizgileri oradan geçmeye devam ediyor ve sınırına ince bir duvar
çubuğu düşüyordu. Referansta (41-50 yürüyüşü, 48-49. bölümler) o alanlar
DOLU: çerçeveyle aynı açık mor, aynı yükseklikte, yuvarlak köşeli bir kütle.

**NE YAPILDI.** `BoardBuilder.BuildDeadZones`: oynanamaz hücreler bağlı
bileşenlere ayrılıyor, her biri yuvarlak köşeli kabartma bir kütle olarak
doldurulıyor. Kenara dayanan bölgeler tahtanın dışına taşırılıp çerçevenin
dış yüzüne kırpılıyor — köşe yayı çerçevenin altında kalıyor.
Bölge çevresindeki eski duvar çubukları kaldırıldı (aynı sınırı iki kez
çizmek, oynanabilir alana 0,15 hücrelik ikinci bir raf taşıyordu).

**NASIL DOĞRULANDI.** 8. bölüm ("7" şekli), 9. bölüm ("H" şekli) ve 20.
bölüm yakalandı; boş alanların hepsi dolu, arka plan hiçbirinden görünmüyor.

### I35 — Level 10 buz görünümü ✅
**NE YAPILDI.** Buz kalıbının üstüne daha AÇIK bir kırağı paneli eklendi
(`ViewKit.IceFrost`) — tek renkli bir kalıp buz değil, boyanmış plastik.
Kapı buzu ile blok buzu artık farklı tonda (bkz. G24).

### I36 — Level 20 perde ✅
**NE YAPILDI.** Perde keskin köşeli iki küptü; tahtanın geri kalanı
yuvarlakken tek keskin nesneydi. Çerçeve ve panel `PrismMeshBuilder` ile
yuvarlak köşeli kuruldu.
**Kalkma animasyonu eklendi:** `Destroy(gameObject)` yerine perde 0,42 saniyede
yükseliyor, büyüyor ve saydamlaşıyor.
**Blokların gelişi:** perde kalktıktan sonra içerik blokları `PlayIntro` ile
sırayla yukarıdan düşüp yaylanarak oturuyor (0,30 + sıra×0,07 gecikme) —
bölüm açılışıyla aynı ritim.
**Buz parçalanması** G25 ile aynı iki katmanlı efekti kullanıyor.

---

## J. KAZANMA / ÖDÜL

### J37 — "BLOCKOUT" harf harf ✅
**REFERANS ÇÖZÜMLENDİ** (Levels 1-20, 28-34. saniyeler, 15 fps'te 90 kare):
ekran siyaha döndükten sonra önce küçücük bir "B" beliriyor, sonra "BL",
"BLO"… her harf soldan sağa ekleniyor ve grup büyüdükçe ölçek de büyüyor.

**NE YAPILDI.** Logo tek bir PNG ve harfler ortak mor konturla bağlı; makasla
kesmek konturu bozardı. `RawImage.uvRect` görselin bir dilimini gösteriyor ve
dilimin arayüz dikdörtgeni aynı orana yerleştiriliyor — dilimlerin hepsi
görününce sonuç piksel piksel bütün logonun aynısı.
**ÖLÇÜM** (`art_raw/logo_game.png`, harf pikselleri mor konturdan doygunlukla
ayrıldı): iki satırın arası y=205/399 (profil 456'dan 39'a düşüyor),
"BLOCK" x 14-645, "OUT!" x 41-572.

### J38 — Konfeti ✅
**NE YAPILDI.** Sıra düzeltildi: konfeti ve fişek artık logo tamamlandıktan
SONRA başlıyor (eskiden aynı anda başlıyordu ve logonun kurulduğu
görünmüyordu). Ekranın altından yukarı beyaz ROKET izleri eklendi; patlamalar
onların ucunda oluyor. Kıvılcımlar artık tek renk değil (üçte biri patlamanın
rengi, kalanı paletten) ve izin boyu tavanlandı — eskiden 2500 birim/sn ile
fırlayan bir kıvılcım ekranın yarısını geçen düz bir çizgiye dönüşüyor,
ortaya havai fişek değil PUSULA GÜLÜ çıkıyordu.

### J39 — Para kazanma paneli ✅
**NE YAPILDI.** Jeton yığınının arkasına ışın çelengi eklendi (16 kama,
biri kalın biri ince). Aynı jeton görseli, arkasında ışın olduğunda
"kazandığın şey", olmadığında "bir resim".

### J40 — Çift kazanma ekranı (BUG) ✅
**TEŞHİS.** Kazanma dizilimi şuydu: `WinCelebration` (gerçek logo görseli +
fişek) → PERFECT kartı → Continue → `LevelIntro` (YAZIYLA kurulmuş
"BLOCK"/"OUT!" + konfeti) → yeni bölüm. Yani aynı kutlama iki kez, ikincisi
eski biçimiyle görünüyordu. `LevelIntro` `WinCelebration`dan ÖNCE yazılmıştı
ve yenisi eklenirken eskisi kaldırılmadı.

**NE YAPILDI.** `LevelIntro` artık yalnız tahtayı perde arkasında değiştiriyor;
logo, fişek ve ses oradan kaldırıldı.

### J41 — Level 20 perfect ekranı ✅
**NE YAPILDI.** Kart rengi artık zorluktan geliyor: normal mor, zor şarap,
çok zor `#D8331F` (41-50 yürüyüşü 12:24'ten ölçüldü — 49. bölümün kartı
KIRMIZI ve başlığın altında "Super Hard" yazıyor). Başlık konturu da kartın
koyu tonundan türetiliyor; zorluk etiketi kartın içine, bölüm adının üstüne
eklendi.

**Yol üstünde bulunan hata:** kutlama coroutine'inde bir NullReference
çıkınca PERFECT kartı HİÇ AÇILMIYORDU (bkz. yukarıdaki 3. ders). Yedek dal
null'a dayanıklı hâle getirildi ve `ShowResultAfterBeat` içine bir güvenlik
ağı kondu.

**NASIL DOĞRULANDI.** 49. bölüm zorla kazandırıldı; kart şarap renginde,
ışın çelengi ve "100" ödülüyle açıldı.

---

## K. COMBO

### K42 — Combo yazısı ✅ (KALDIRILDI)
**KONTROL EDİLDİ.** 1-20 ve 41-50 yürüyüşlerinden çıkarılan ~140 karenin
tamamı tarandı (oynanış, kazanma, duraklama, ödül ekranları dahil). Referans
HUD'ında jeton, bölüm, yeniden başlat, süre, duraklat ve üç yardımcı var —
**combo göstergesi YOK.**

**NE YAPILDI.** Rozet kaldırıldı. `ComboTracker` yerinde bırakıldı: sesin
kademelenmesi ve bölüm sonu "en uzun zincir" istatistiği onu kullanıyor,
ikisi de ekranda kutu göstermeden çalışıyor.

---

## L. LEVEL 35 — MAKİNE

### L43 — Makine feature'ı ✅
**REFERANS ÇÖZÜMLENDİ** (41-50 yürüyüşü, 12:16 karesi, tam çözünürlükte
büyütüldü). Makine dört parça ve hepsi bilgi taşıyor: gövde, sayaç başı,
lamba, ve içinde SIRADAKİ BLOĞUN KENDİ ŞEKLİ duran pencere.

**NE YAPILDI.** Bizdeki hâli iki düz küptü (gövde + sıradaki rengin kutusu):
şekil bilgisi yoktu ve makinenin çalışıp çalışmadığı görünmüyordu.
- Gövde, sayaç başı, KIRMIZI/YEŞİL lamba, koyu pencere ve penceredeki gerçek
  silüet önizlemesi.
- Lamba `ObstacleSystem.RefreshGeneratorLamps` tarafından, itmeyi DENEYEN
  aynı `IsAreaFree` çağrısından besleniyor — iki ayrı hesap yapılsaydı biri
  güncellenip diğeri unutulurdu.
- Blok çıkarken makine dışa doğru geri tepiyor.
- Sıra bitince makine kendi parçalarına ayrılıp dağılıyor (yerçekimi + dönme
  + küçülme), kamera sarsılıyor ve titreşim veriyor.

**NASIL DOĞRULANDI.** 49. bölüm yakalandı ve büyütüldü: magenta gövde, "3"
sayacı, kırmızı lamba ve pencerede mavi tuğla görünüyor.

### L44 — "New Feature" açılma ekranı ✅
**NE YAPILDI.** Görsel alanı ekranın %24×%12'sinden %49×%25'ine büyütüldü —
o kutuya sığan her şey birkaç renkli dikdörtgen olmak zorundaydı. Her mekanik
için ayrı ve ayrıntılı çizim yazıldı: kabartmalı tuğla (saplamalarıyla),
buz kalıbı + sayaç, buzlu kapı, renk kuyruğu kapısı, altın çerçeveli perde
(tırtıllar + rozet), katmanlı blok, yönlü blok (oluk + çift ok) ve MAKİNE
(gövde + sayaç + lamba + penceresinde sıradaki tuğla). Yeni
`UiSprites.Triangle` ok başları için gerçek üçgen veriyor — bu projede
üçgen gereken her yerde 45° döndürülmüş kare kullanılıyordu.

---

## Açık kalanlar

- **A2** — kullanıcı yapacak.
- **Haptik cihazda doğrulanmadı.** Manifest enjeksiyonu ancak gerçek bir
  Gradle derlemesinde görülür.
- **27 bölümde süre yetmiyor** (3. turdan devrediyor, tasarım kararı).
- **`DeviceErrorOverlay.Enabled = true`** — yayına çıkarken `false`.
