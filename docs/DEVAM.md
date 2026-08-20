# Devam Notu — yeni oturuma nasıl devam edilir

Yeni bir sohbet açtığında Claude'a şunu yaz:

> **`docs/DEVAM.md` dosyasını oku ve kaldığımız yerden devam et.**

Bu dosya her oturum sonunda güncellenir. Aşağısı 2026-08-10 itibarıyla geçerli.

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
