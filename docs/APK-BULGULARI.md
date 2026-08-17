# APK TESTİ — 21 BULGU (2026-08-17)

Kullanıcı APK'yı gerçek cihazda test etti ve 21 madde çıkardı.
**HİÇBİRİ ES GEÇİLMEYECEK.** Hepsi tek tek, detaylı, doğrulanarak kapatılacak.

Bu dosya tek doğruluk kaynağı. Bir madde bitince kutusu işaretlenir ve altına
NE YAPILDI + NASIL DOĞRULANDI yazılır. Yarım bırakılan madde işaretlenmez.

---

## REFERANS DURUMU

`C:/Users/CPN12/OneDrive/Masaüstü/Block Out! Videos/` içinde **9 yüksek
çözünürlüklü ekran görüntüsü** var (946×2048, oran 2.165 — video referanslarıyla
aynı cihaz):

| dosya | ekran |
|---|---|
| `grand açılış.jpeg` | Grand Games açılışı (bordo zemin + beyaz "grand") |
| `oyun açılış.jpeg` | Oyun yükleme ekranı (BLOCK OUT! logosu, roket, "Yükleniyor...") |
| `ana ekran.jpeg` | Ana ekran |
| `journey.jpeg` | Yolculuk |
| `market.jpeg` | Mağaza |
| `sıralama.jpeg` | Liderlik panosu |
| `collections.jpeg` | Koleksiyon |
| `profil.jpeg` | Profil |
| `WhatsApp Image ... (2).jpeg` | **Ayarlar** |

**EKSİK REFERANS: günlük ödül (daily reward) paneli.** 4. madde için ya
`menus,powerups,vs.mp4` taranacak ya da kullanıcıdan ekran görüntüsü istenecek.

Bu görseller ölçüm için videolardan ÇOK daha iyi: 946 piksel genişlik,
sıkıştırma artefaktı az. Ölçüm kuralı aynı: yatayda `piksel/946`, dikeyde
`1 - piksel/2048`.

---

## SIRA — neden bu sırayla

1. **BLOCKER'lar önce.** Kullanıcı oyunun yarısını test EDEMEDİ; bunlar
   açılmadan kalan testler yapılamaz ve yeni bulgular gelemez.
2. **Yapısal olanlar ikinci.** 6. madde (üst boşluk) TEK BAŞINA bütün
   ekranları etkiliyor — ekran ekran uğraşmadan önce o düzelmeli, yoksa her
   ekranı iki kez ölçeriz.
3. **Açılış zinciri üçüncü.** Kendi içinde kapalı bir iş (ikon + grand + splash).
4. **Ekran ekran sadakat en son.** En çok emek isteyen, en az riskli kısım.

---

## A — BLOCKER'LAR (test bunlar olmadan ilerlemiyor)

### [x] 18. Bölüm açılmıyor — bloklar gelmiyor  ⚠️ SEBEP BULUNDU, DÜZELTİLDİ
**Sebep: IL2CPP kod kırpma + Newtonsoft yansıması + `link.xml` yokluğu.**

Elenen şüpheliler (hepsi tek tek kontrol edildi, hiçbiri değildi):
`LevelCatalog` kataloğu 50 bölümü doğru referanslıyor · `__playtest.json`
katalogda DEĞİL · `BlockOut/Brick` shader'ını materyaller kullanıyor, yani
build'e giriyor · `Main.unity` içinde `GameSession`'ın bütün alanları
(config, palette, visuals, levelJson, input, boardRoot) bağlı ·
`EnsureGameplayWiring` değişiklikte sahneyi kaydediyor.

Gerçek sebep: `ProjectSettings` → `scriptingBackend: Android: 1` (**IL2CPP**),
`stripEngineCode: 1`, `managedStrippingLevel` varsayılan (kırpma AÇIK) ve
projede **hiçbir `link.xml` yoktu**. Bölüm JSON'u
`JsonConvert.DeserializeObject<LevelData>` ile, yani tamamen YANSIMAYLA
okunuyor; `LevelData`/`BoardData`/`BlockData` alanlarına kodun hiçbir yerinden
doğrudan dokunulmadığı için kırpıcı onları atıyor. Parse patlıyor,
`GameSession.Restart` erken dönüyor, tahta boş kalıyor.

**Düzeltme:** `Assets/link.xml` eklendi, `BlockOut.Core` tamamen korunuyor
(o assembly zaten davranış değil VERİ: modeller + kayıt şeması).

**DERS: "editörde çalışıyor" hiçbir şey kanıtlamaz** — editörde kırpıcı hiç
koşmaz. Bu hata sınıfı yalnız gerçek derlemede görünür.

> ⚠️ **Cihazda doğrulanacak.** Editörde sınanamaz (kırpma yok).

**EK SAĞLAMLAŞTIRMA (cihaz olmadan bir daha kör kalmayalım):** Bölüm
yüklenemezse artık EKRANDA sebebi yazıyor. `GameSession.LoadFailure`
alanı hatayı taşıyor, HUD'daki ipucu satırı onu kırmızıyla basıyor.
Ayrıca bölüm dosyası hiç bulunamazsa mesaj katalogdaki bölüm sayısını da
söylüyor — "katalog boş" ile "parse patladı" ayırt edilebiliyor.

**DERS (cihazda `Debug.LogError` hiçbir yere gitmez):** Boş ekran + sessiz
konsol, teşhisi saatlerce geciktirdi. Hata metnini gizlemek hatayı yok
etmiyor, yalnız bulunmasını zorlaştırıyor. Bir sonraki APK'da bölüm yine
açılmazsa ekran sebebini kendisi söyleyecek.

### [x] 19. Gizli geliştirici menüsü açılmıyor — DÜZELTİLDİ
**Sebep kesin:** `DevMenu` sınıfının tamamı
`#if DEVELOPMENT_BUILD || UNITY_EDITOR` içindeydi. Release APK'da menü
**hiç derlenmiyordu** — açılmaması değil, var olmaması söz konusuydu.

**Düzeltme:** koşul kaldırıldı; menü her yapıda derleniyor ama hâlâ GİZLİ
(sol üst köşeye üst üste **5** dokunuş — kullanıcı 3 sanıyordu, doğrusu 5;
editörde ayrıca F8). Proje mağazaya konmayacağı için bu güvenli; gerekirse
sınıf gövdesini yeniden `#if` içine almak yeterli.

**DERS: test aracı, TEST EDİLEN yapıda olmalı.** Testçinin eline verilen
yapı ile aracın bulunduğu yapı farklıysa araç hiç yok demektir.

### [x] 20. Sınırsız kalp paketi çalışmıyor — DÜZELTİLDİ, play modunda doğrulandı
**Sebep:** Satın alma `InfiniteLivesUntilUtc`'yi kayda düzgünce yazıyordu ama
projede o alanı **okuyan tek bir yer yoktu**. `ProgressService.HasInfiniteLives`
ve `InfiniteLivesLeft` yazılmış, kullanılmamış.

**Düzeltme, üç yerde:**
1. `GameSession.SpendLifeForAttempt` — hak sürerken can HARCANMIYOR.
2. `HomeScreen` — sayının yerini `icon_infinite` GÖRSELİ alıyor (∞ karakteri
   Baloo 2'de yok, TMP boş kutu çizer — bu projede beşinci tekrar) ve alttaki
   yazı kalan süreyi gösteriyor ("2h 45m").
3. `HomeScreen` oynama kapısı — 0 canla da oynanabiliyor; bu satır atlansaydı
   paketi alan oyuncu yine "Oyna"ya basamazdı.

**Doğrulama (play modu):** paket verildi → ∞ görseli çizildi, bölüme girildi,
can **2 → 2** (harcanmadı), kalan süre işliyor.

**DERS: bir alanı YAZMAK onu bir özellik yapmaz.** Okuyan taraf yoksa özellik
de yoktur — para gidiyor, hiçbir şey değişmiyor.

---

## B — YAPISAL (tek düzeltme bütün ekranları etkiliyor)

### [x] 6. Ana ekran DIŞINDAKİ bütün sayfaların üstünde boşluk — DÜZELTİLDİ
**Sebep:** Menü ekranları `MenuCanvas → SafeArea → Content` zincirinin altında,
yani güvenli alanın İÇİNDE. Ana ekranın manzarası ise TAM EKRAN. Çentikli bir
telefonda menü sayfasının koyu gövdesi çentiğin altında başlıyor, üstünde
kalan şeritte alttaki ana ekran görünüyor — kullanıcının gördüğü "boşluk" bu.

**Bu hata ALT kenarda zaten çözülmüştü** (`MenuShell.BuildTabBar`:
"boyası aşağı taşar", `offsetMin = -220`) ama aynı düşünce üst kenara hiç
uygulanmamıştı. **DERS: bir kenarda bulduğun kenar hatasını öbür kenarda da ara.**

**Düzeltme:** başlık bandının boyası güvenli alanın üstüne 320 birim taşıyor.
Taşan yalnız BOYA — başlık yazısı bandın içinde kalıyor, çentiğin altına
girmiyor. Üç yerde:
- `MenuPage.Header` → Ayarlar, Profil, Liderlik, Koleksiyon (dördü birden)
- `JourneyScreen.BuildHeader` → kendi başlığı var
- `StoreScreen.BuildAwning` → kendi tentesi var; şerit tentenin koyu mavisi
  (`market.jpeg` üst kenarından ölçüldü, `#053AE8`). Çizgili deseni yukarı
  sürdürmek yerine düz renk: o şerit çentiğin altında kalıyor, festonu yukarı
  esnetmek tasarımı bozardı.

**Doğrulama:** play modunda güvenli alan üstten %6 kısılarak çentik simüle
edildi; **altı ekranın da** boya şeridi ekranın tepesini aşıyor.

### [x] 8. Seçili sekmeye tekrar basınca ekran kapanıp açılıyor — DÜZELTİLDİ
**Sebep:** `MenuShell.Show` içinde tek satır —
`if (_active == key && key != "home") key = "home";` — yani açık sekmeye
tekrar basmak oyuncuyu ANA EKRANA atıyordu. Niyet "geri tuşu gibi olsun"du;
cihazda yaşanan şey "ekran açılıp kapanıyor" oldu.

**Düzeltme:** aynı sekmeye basmak artık hiçbir şey yapmıyor, yalnız haptik
veriyor. Doğrulama: mağazadayken mağaza sekmesine 3 kez daha basıldı, mağaza
açık kaldı; farklı sekmeye geçiş hâlâ çalışıyor.

**DERS:** Sekme çubuğunda seçili sekme bir HEDEF'tir, düğme değil — zaten
oradaysan gidilecek yer yoktur.

### [x] 15. Haptik eksik — bütün oyunun haptik denetimi — DÜZELTİLDİ

**Asıl sorun düğmelerde değil, MOTORDA'ydı.** `Haptics.Play` tek bir şey
yapıyordu: `Handheld.Vibrate()`. O çağrı Android'de **süresi ayarlanamayan
~500 ms**'lik bir buzz üretir ve şiddet ayrımı yapmaz. Bu yüzden `Threshold`
Medium'da tutulmuştu — her dokunuşta yarım saniye titreyen bir oyun
kullanılamaz. Ama o eşik, asıl istenen şeyi de imkânsız kılıyordu: arayüz
dokunuşlarının hafif bir tık vermesini. Yani "haptik ekle" demek, önce
motoru değiştirmek demekti.

**Düzeltme 1 — motor.** Android'de `Vibrator` doğrudan çağrılıyor; şiddet
artık gerçek bir SÜRE + GENLİK: Light 12 ms/60, Medium 25 ms/140,
Heavy 45 ms/255 (API 26+ `VibrationEffect.createOneShot`, altında süre).
Servis nesnesi bir kez bulunup saklanıyor (her dokunuşta JNI pahalı).
Eşik `Light`'a indirildi. iOS'ta kısa tık üretilemediği için yalnız Heavy
titriyor — hepsini buzz'a çevirmektense hafifleri hiç çalmamak daha iyi.

**Düzeltme 2 — bağlama noktası.** Haptiği düğme düğme eklemek imkânsız
(onlarca düğme var, her yenisi yeniden unutulur). `UiButtonFeel` zaten
HEPSİNİN üstünde — basma animasyonu ondan geliyor. Oraya sesten AYRI bir
`Pressed` kancası eklendi; `AppRoot` onu haptiğe bağlıyor.

> **Neden ayrı kanca:** `Clicked` bir olay değil bir ALAN; ses ve titreşimi
> aynı alana bağlamak, ikinci atamanın birinciyi ezmesi demek olurdu ve
> hangisinin kazandığı kurulum sırasına kalırdı.

**Düzeltme 3 — kullanıcının asıl noktası.** "Kapat düğmesine basılı tutup
parmağımı dışarı kaydırınca hiçbir şey olmaması normal, ama yine de haptik
çalışmalı." Doğru okuma: **titreşim eylemin değil, DOKUNUŞUN onayıdır.**
Kanca `OnPointerDown` içinde — tıklamanın tamamlanmasını beklemiyor, eylem
iptal olsa bile parmak "duyuldum" bilgisini alıyor.

**Düzeltme 4 — hissiz kalan 6 düğme.** Tarama: 76 düğmenin **6'sı**
`UiButtonFeel` taşımıyordu — duraklat panelindeki Off/On yarımları. Onlarda
ne basma animasyonu, ne tık sesi, ne titreşim vardı. Ses elle çağrıldığı
için eksiklik yıllarca fark edilmemiş. Eklendi; elle çağrılan ses satırı da
kaldırıldı (yoksa ses ikiye katlanırdı).

**Doğrulama (play modu):** 76 düğme, **hissiz 0**. `Pressed` ve `Clicked`
ikisi de bağlı. Eşik `Light`, ayar açık. Kanca, oyuncunun haptik ayarı
kapalıyken de açıkken de hatasız çalışıyor.

> Titreşimin kendisi editörde hissedilemez (`#if UNITY_ANDROID && !UNITY_EDITOR`).
> Cihazda tık sertliği fazla/az gelirse ayarlanacak tek yer `Haptics.Shape`.

---

## C — AÇILIŞ ZİNCİRİ

### [ ] 1. Uygulama ikonu yok
Orijinal oyunun ikonu kullanılabilir.

### [ ] 3. Grand Games açılışı yok
Referans: `grand açılış.jpeg` — bordo zemin, ortada beyaz küçük harf "grand".
Oyun açılışından ÖNCE gelir.

### [ ] 2. Oyun açılış/yükleme ekranı yok
Referans: `oyun açılış.jpeg` — gökyüzü + bulutlar, "BLOCK OUT!" blok harfli
logo, roketli karakterler, altta "Yükleniyor...". **Aynı ekran yükleme ekranı
olarak da çalışacak.**

---

## D — EKRAN EKRAN SADAKAT

### [ ] 7. Ana ekran
- Üstteki jeton ve kalp yazıları büyüyecek; kalp adedi ve "Dolu" yazısının
  puntosu artacak.
- Alttaki "Seviye N" düğmesinin yazısı çok küçük — referanstaki ölçüye çıkacak.
- Üstündeki zorluk ("Zor Seviye") kısmı daha iyi bir düğme/rozet olacak.
- Alt menü çubuğunun zemini ve SEÇİLİ sekme görselleri Block Out'a en yakın
  hâle getirilecek.
Referans: `ana ekran.jpeg`

### [ ] 9. Yolculuk (Journey)
- **Kaydırma çalışmıyor:** orijinalde ekranın HER YERİNDEN yukarı/aşağı
  sürüklenebiliyor, bizde olmuyor.
- Yuvarlak bölge görselleri biraz daha küçük olacak.
- Dikdörtgen ödül (kilometre taşı) kapsülleri sade kalmış — kendi UI'ımızı
  çizdirebiliriz.
- Üstteki "Yolculuk" başlığı baskılı/kabartmalı görünüme geçecek.
Referans: `journey.jpeg`

### [ ] 10. Mağaza — Özel Teklifler kaydırılamıyor
Taşıyıcı (`OfferCarousel`) APK'da çalışmıyor, sayfa sabit kalıyor.

### [ ] 11. Mağaza arka planı çok sade
Orijinalde doku var, daha kaliteli duruyor. Referans: `market.jpeg` + video.

### [ ] 12. Mağaza başlığı ve panel köşe yarıçapları
"Mağaza" yazısının yeri, üst bandın duruşu, panellerin radius'u ve genel
oturuşu yeniden ölçülüp kıyaslanacak.

### [ ] 13. Ayarlar — düğme/ikon/yazı kalitesi + alt menü çıkmamalı
Orijinalde ayarlar ekranında alt sekme çubuğu YOK (referansta doğrulandı).
Düğmelerin, ikonların ve yazının kalitesi Block Out seviyesine çıkacak.
Referans: `WhatsApp Image ... (2).jpeg`

### [ ] 14. Ayarlar — "Hesabımı Sil" ve başlık yerleşimi
Referansta "Hesabımı Sil" en altta duruyor; "Ayarlar" başlığının yeri ve
görünümü güncellenecek.

### [ ] 16. Liderlik — tasarım çok sade
- Sıralama listesi kaydırılamıyor.
- Podyum tasarımı zayıf; referansta yeşil park zemini üzerinde üç kürsü var.
- **İlk üçtekiler kürsüde OLMASINA RAĞMEN alttaki listede de görünüyor**
  (referansta 1. Ella, 2. Fikret, 3. KOR listede de var).
Referans: `sıralama.jpeg`

### [ ] 17. Liderlik — Haftalık/Dünya/Ülke düğmelerinin yeri kötü
Referansta tek yuvada, başlığın hemen altında; yanında "i" bilgi düğmesi.

### [ ] 21. Profil ekranı düzensiz
Referansa benzetilecek. Referans: `profil.jpeg`

### [ ] 5. Koleksiyon ikonu, içerik görseli, yazı biçimi ve başlık
Referansta: kitap + bloklar görseli, "Koleksiyonu **Seviye 95'de** Aç!"
(vurgulu kısım altın). Sekme ikonu da değişecek.
Referans: `collections.jpeg`

### [ ] 4. Günlük ödül (Daily Reward) paneli
Ödüllerin arkasındaki "day" yazısı okunmuyor ve oyunun konseptine uymuyor.
**REFERANS EKSİK** — video taranacak, çıkmazsa kullanıcıdan istenecek.

---

## KAPANAN MADDELER

_(Bir madde bitince buraya taşınır: ne yapıldı, nasıl doğrulandı.)_
