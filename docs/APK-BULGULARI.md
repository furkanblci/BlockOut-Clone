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

## TÜM OYUN DÜĞME TARAMASI (2026-08-17)

9. maddede bulunan "dinleyicisi var ama dokunuş ulaşmıyor" hatası yeni bir
tarama yöntemi doğurdu ve o tarama **altı ölü düğme daha** buldu:

| düğme | durum |
|---|---|
| Yolculuk "Üst" / "Alt" | hiç çalışmamış |
| Yolculuk bölge oynat düğmeleri (5 bölge) | **"düzeltildi" sanılıyordu** |
| Mağaza "Restore Purchases" | hiç çalışmamış |

**En öğretici olanı bölge düğmeleri:** Daha önceki bir oturumda "onClick
bağlanmamış" diye bulunup düzeltilmiş ve play modunda `onClick.Invoke()` ile
doğrulanmıştı. Ama **`Invoke` raycast'i ATLAR** — hedef grafik hâlâ
`raycastTarget = false` olduğu için düğme gerçekte basılamıyordu.
Doğrulama yöntemi, hatayı görmeyi imkânsız kılmıştı.

**Tarama yöntemi (tekrarlanabilir):** Bir düğme, alt ağacında `raycastTarget`
açık HİÇBİR grafik yoksa ulaşılamazdır. Sahnedeki bütün `Button`'ları
(kapalı ekranlar dahil) bu kurala sokmak yeterli — play modunda tıklamaya
bile gerek yok.

**Kalıcı çözüm:** `UiKit.MakeClickable(...)` eklendi. Yüzeyin raycast'ini
açıyor, `Button` + `UiButtonFeel` ekliyor, dinleyiciyi bağlıyor. Üç çağrı
yeri ona geçirildi; burada raycast'i açmayı unutmak mümkün değil.

**Sonuç: 76 düğme, ULAŞILAMAYAN 0, HİSSİZ 0.**

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

### [ ] 1. Uygulama ikonu yok — SENDEN BİR DOSYA BEKLİYOR
Karar: **orijinal oyunun ikonu** kullanılacak (kullanıcı seçimi, 2026-08-17).
Bende o dosya yok; referans videolardan çıkarılabilecek en büyük hâli ~60
piksel, ikon için 512 gerekiyor.

**Yapman gereken:** oyunun mağaza ikonunu `art_raw/icon_app.png` olarak koy
(kare, en az 512×512). Gerisini ben hallederim: `ProjectSettings` Android
ikon yuvaları + adaptive icon ön/arka katmanı.

### [x] 3. Grand Games açılışı — KURULDU
`BootSplash.cs`. Bordo degrade zemin (köşe `#81001F`, dikey orta `#C50133` —
referanstan örneklendi), ortada beyaz küçük harf kelime işareti,
X 0.235-0.762 · Y(alttan) 0.453-0.549. 1.4 saniye durup yumuşakça sönüyor.

> Kullanıcı birebir "grand" yazmasını seçti (portfolyo/marka uyarısı yapıldı,
> karar tekrarlandı).

### [x] 2. Oyun açılış/yükleme ekranı — KURULDU
`BootSplash.cs`'in ikinci aşaması. **`splash_art.png` zaten üretilmiş ama
koda hiç bağlanmamıştı** (`Art.Splash`'in tek kullanımı yoktu) — referansın
illüstrasyonuna çok yakın: gökyüzü, bulutlar, roketli üç karakter, uçuşan
bloklar. Üstte "BLOCK OUT!", altta "Loading…" (Y 0.100-0.155).

**DERS (yükleme ekranı, yükleme BAŞLARKEN ölmemeli):** İlk hâlde splash Boot
sahnesinin nesnesiydi; `SceneRouter.Load(Main)` çağrıldığı anda Boot
boşaltılıyor ve perde tam da işe yarayacağı anda kayboluyordu. Artık
`DontDestroyOnLoad` ile sahne geçişini aşıyor ve Main ayağa kalktıktan sonra
açılıyor.

**Kalan fark:** referansta logo BLOK HARFLİ bir görsel; bizde başlık
malzemesiyle yazılmış altın metin. Gerçek logo görseli gelirse tek bir
`Image`'a iner, düzen değişmez.

---

## D — EKRAN EKRAN SADAKAT

### [~] 7. Ana ekran — YAZI ÖLÇÜLERİ BİTTİ, iki iş kaldı

**Ölçüm yöntemi (`ana ekran.jpeg`, 946×2048):** yazının BÜYÜK HARF
yüksekliğini, içinde durduğu kabın yüksekliğine oranla.

| yazı | referans oranı | bizim (önce) | sonuç |
|---|---|---|---|
| jeton sayısı | 29/85 = **0.341** | 0.34 | **zaten doğruymuş** — dokunulmadı |
| kalp adedi | 37/85 = **0.435** | 0.32 | 34 → **46** punto |
| "Dolu" | 31/85 = **0.365** | 0.27 | 29 → **39** punto |
| "Seviye 54" | ekranın **%3.27**'si | %1.65 | 44 → **84** punto |
| "Zor Seviye" | — | — | 24 → **48** punto |

**DERS (göz "hepsi küçük" der, ölçüm hangisi olduğunu söyler):** Jeton
sayısı zaten doğru orandaydı; küçük olanlar kalp adedi ve "Dolu"ydu. Hepsini
birden büyütmek jeton sayısını yanlış yapardı.

**DERS (oranı NEYE göre ölçtüğün önemli):** "Seviye 54" için düğme-içi orana
bakmak yanıltıcı — bizim düğmemiz referanstan %50 daha yüksek. Doğru ölçü
EKRANA göre olan; oyuncunun gözü düğmeyi değil yazıyı okuyor.

**Zorluk ayrı bir rozet DEĞİL:** referansta düğmenin İÇİNDE ikinci satır.
Küçük olduğu için düğmeye ait görünmüyordu; büyüyünce yerine oturdu.

#### Bu iş sırasında KENDİ eklediğim bir hata yakalandı
8. bulgunun düzeltmesi (`Show` başındaki "aynı sekme = çık" koruması)
kurulumdaki `Show("home")` çağrısını da yutuyordu — `_active` zaten "home"
ile başlıyor. Sonuç: sekme görselleri hiç uygulanmıyor, **beş sekmenin de
kartı ve yazısı açık kalıyordu**, hepsi seçiliymiş gibi görünüyordu.
`_applied` bayrağı eklendi. **DERS: "durum değişmediyse çık" koruması,
durumun bir kez UYGULANDIĞINI da bilmek zorunda; yoksa "değişmedi" ile
"hiç yazılmadı" karışır.**

#### ÜST BAR TEK KAPSÜLE ÇEVRİLDİ
Referansın X oranları birebir alındı: avatar 0.058-0.201 · kapsül
0.303-0.866 · jeton ikonu 0.220-0.308 (kapsülün sol ucundan TAŞIYOR) ·
sayı 0.330-0.445 · artı 0.457-0.511 · kalp 0.529-0.628 · durum 0.640-0.762 ·
artı 0.772-0.827 · dişli 0.872-0.962. Kullanılmayan `Track` yardımcısı
kaldırıldı (61 satır).

**Ölçüm bir kez daha "dokunma" dedi:** kapsülün tonu bizde daha soluk
görünüyordu; ölçünce referansın zemini %46'ya indirdiği, bizim %50 alfamızın
zaten aynı yerde olduğu çıktı. Fark arkadaki manzaranın renginden geliyor —
kapsül doğru. (Jeton puntosunda olduğu gibi, gözün "yanlış" dediği şeyin
ölçüsü doğru çıkabiliyor.)

#### SEKME ÇUBUĞU REFERANSA ÇEKİLDİ
- **Renk:** gövde `#514E9C` → **`#4F3BD8`**, ışık şeridi → **`#5340EA`**
  (referanstan örneklendi). Eskisi belirgin biçimde daha soluktu —
  "alt menü arkaplanı güncellenecek" isteğinin ölçülebilir kısmı buydu.
- **Kenardan kenara düz:** çubuğun boyası artık YANLARDAN da 90 birim
  taşıyor. Görselin yuvarlak uçları ekran dışında kalıyor; eskiden iki yanda
  yuvarlanıp arkasındaki manzarayı gösteriyordu.
- **Yükseklik:** üst kenar %8.6 → **%9.9** (referans ölçüsü).
- **Dikey ayraçlar** eklendi (referansta her komşu sekme çifti arasında var).
- **Seçili kart** çubuğun üstüne daha çok çıkıyor: çarpan 1.56 → **1.73**
  (referansta kartın üstü ekranın altından %16.4'te).

#### 7. MADDEDE KALAN
Sekme ikonları referansta bizimkilerden büyük ve seçili kartın tonu çubuktan
daha belirgin ayrışıyor. İkinci tur cila; yapısal iş bitti.

### [ ] 7-eski. Ana ekran (özgün istek metni)
- Üstteki jeton ve kalp yazıları büyüyecek; kalp adedi ve "Dolu" yazısının
  puntosu artacak.
- Alttaki "Seviye N" düğmesinin yazısı çok küçük — referanstaki ölçüye çıkacak.
- Üstündeki zorluk ("Zor Seviye") kısmı daha iyi bir düğme/rozet olacak.
- Alt menü çubuğunun zemini ve SEÇİLİ sekme görselleri Block Out'a en yakın
  hâle getirilecek.
Referans: `ana ekran.jpeg`

### [~] 9. Yolculuk — KAYDIRMA DÜZELDİ + BİR ÖLÜ KONTROL DAHA

**Kaydırma neden çalışmıyordu:** `ScrollRect` viewport'ta kuruluydu ama
viewport'ta **raycast yakalayan hiçbir grafik yoktu** — yalnız `RectMask2D`.
uGUI olay sistemi sürükleme olaylarını ancak `raycastTarget` açık bir
GRAFİĞE çarpınca yollar. Sonuç: parmağını tesadüfen bir kapsülün üstüne
koyarsan kayıyordu, boş zemine koyarsan hiçbir şey olmuyordu.

Çözüm: görünmez ama dokunulabilir bir yüzey (`TouchCatcher`, alfa 0).

**DERS (yakalayıcıyı VIEWPORT'a koyma):** İlk denemede yüzeyi viewport'un
KENDİSİNE koydum. Kaydırma düzeldi ama "Üst"/"Alt" düğmeleri tıklanamaz
oldu — o düğmeler viewport'un çocuğu değil KARDEŞİ. uGUI'de kardeş sırası
çizim ve raycast sırasıdır. Yakalayıcı, kaydırılan İÇERİĞİN ilk çocuğu
olmalı: hem viewport'un kardeşlerinin altında kalır, hem de sonradan
eklenen bölge düğmeleri onun üstünde kalır.

#### BEŞİNCİ ÖLÜ KONTROL — "Üst" / "Alt" düğmeleri
Yakalayıcıyı düzeltirken çıktı: bu iki düğmenin `onClick`'i bağlıydı,
`Button` bileşeni yerindeydi, ekranda düğme gibi duruyordu — ama hedef
grafiği `MenuCapsule`'den geliyor ve o yardımcı `raycastTarget = false`
üretiyor. **Dokunuş düğmeye hiç ulaşmıyordu; hiçbir zaman çalışmamışlar.**

**DERS (dinleyici bağlamak YETMEZ, dokunuşun ULAŞMASI da gerekir):**
Önceki dört ölü kontrol dinleyicisizdi ve "onClick bağlı mı" taramasıyla
bulunmuştu. Bu ise dinleyicisi olup DUYAMAYAN bir düğme — o tarama bu türü
göremez. Bulan şey bir **raycast taraması** oldu: her düğmenin merkezine
sanal dokunuş atıp ilk çarpanın o düğme olup olmadığına bakmak.

**Doğrulama:** boş zeminden 9/9 noktada kaydırma çalışıyor, iki atlama
düğmesi de 2/2 tıklanabilir, kaydırma konumu gerçekten değişiyor.

#### BAŞLIK VE KAPSÜLLER — referanstan ölçüldü
- **Başlık "baskılı" oldu.** Punto 62 → **74** (referansta başlığın büyük harf
  yüksekliği ekranın %2.73'ü; 62 punto %2.32 veriyordu). Kontur `SetOutline`
  ile **kalın** (0.55) ve rengi referanstan örneklendi (`#342596`).
  **DERS (yedinci tuzak, yine):** `CreateTitle`'a verilen kontur rengi sessizce
  yok sayılıyor — bütün başlıklar tek materyali paylaşıyor. Başlık düz beyaz
  görünüyordu çünkü kontur hiç uygulanmamıştı.
- **Kilometre taşı kapsülü:** yüzey `#5846E8`, dış kenar `#4130B7`
  (`journey.jpeg`'ten). Eskisi hem yüzeyde hem kenarda daha koyuydu, kapsül
  zeminden yeterince ayrışmıyor ve "sade" duruyordu.

#### ÖLÇÜM BİR İSTEĞİ ÇÜRÜTTÜ
"Yuvarlak görseller biraz daha küçük olacak" isteği ölçülünce ters çıktı:
referans çemberin çapı ekran genişliğinin **%73.2'si**, bizimki **%66.7** —
bizimki zaten daha küçük. Fark çemberde değil **dikey yoğunlukta**: referans
aynı ekrana daha çok kilometre taşı sığdırıyor. Çemberi küçültmek yanlış
düzeltme olurdu; satır aralığı ayrı bir iş olarak duruyor.

#### 9. MADDEDE KALAN
Satır aralığı (dikey yoğunluk) ve ödül kapsülünün iç düzeni.

### [ ] 9-eski. Yolculuk (özgün istek metni)
- ~~Kaydırma çalışmıyor~~ — düzeldi (yukarı bak).
- Yuvarlak bölge görselleri biraz daha küçük olacak.
- Dikdörtgen ödül (kilometre taşı) kapsülleri sade kalmış — kendi UI'ımızı
  çizdirebiliriz.
- Üstteki "Yolculuk" başlığı baskılı/kabartmalı görünüme geçecek.
Referans: `journey.jpeg`

### [x] 10. Mağaza — Özel Teklifler kaydırılamıyor — DÜZELTİLDİ

**Taşıyıcı aslında bozuk değildi; BIRAKMA mantığı bozuktu.**

Sanal sürüklemeyle sınadım: 700 piksel çekince sayfa doğru şekilde
değişiyordu. Ama fiske (hızlı kaydırma) hesabı `eventData.delta.x` ile
yapılıyordu — yani **yalnız son karede** parmağın gittiği yol.

Gerçek bir dokunuşta parmak kaldırılmadan hemen önce YAVAŞLAR; son kare
deltası çoğu zaman ~0'dır. Yani fiske katkısı hep sıfır çıkıyor ve **yarım
sayfadan az her kaydırma geri dönüyordu**. Telefonda başparmakla yapılan
normal bir kaydırma 1080 genişlikte 200-300 piksel, yarım sayfa ise 540.
Kullanıcı kaydırıyor, sayfa geri dönüyor, "sabit kalmış, çalışmıyor"
görünüyor.

**DERS (girdi cihazı hatayı GİZLEYEBİLİR):** Editörde fark edilmemesinin
sebebi fare. Fareyle yapılan test sürüklemesi kesintisiz ve hızlıdır,
bırakma anında delta hâlâ büyüktür. Aynı kod, aynı ekran, farklı parmak —
farklı sonuç. "Editörde çalışıyor"un bir kez daha hiçbir şey kanıtlamadığı
yer.

**Düzeltme:** hız artık `OnDrag` boyunca yumuşatılarak biriktiriliyor
(son karenin deltası değil); ayrıca MESAFE eşiği eklendi — sayfanın çeyreği
kadar kaydırmak komşu sayfaya taşımaya yetiyor.

**Doğrulama:** telefon gibi (önce hızlı, sonu yavaşlayan) 275 piksellik bir
kaydırma artık 2. sayfaya geçiyor. Eski kodda bu geri dönerdi.

### [x] 11. Mağaza arka planı çok sade — KAPİTONE DOKU EKLENDİ

Ölçüm: bölüm zeminlerinin renkleri zaten doğruydu (bordo `#601B0C`,
mor `#3C1D66` — bizimkiler birkaç birim farkla aynı). Eksik olan **doku**.
Referansta zemin düz değil: eşkenar dörtgen bir kapitone deseni var ve her
yüzeyin ışığı biraz farklı. Bordo bölümde kırmızı kanal 62 ile 116 arasında
salınıyor (ortalamanın ±%28'i).

`MenuSprites.Quilt` eklendi — döşenebilir 128×128 prosedürel desen: iki
köşegen dalganın toplamı yüzeyleri, sıfır geçişleri de dikiş çizgilerini
veriyor.

**DERS (doku RENK DEĞİL, IŞIKTIR):** Deseni renkli çizip Image'ı boyamak,
boyama çarpma olduğu için deseni de renklendirirdi ve her bölümde farklı bir
ton çıkardı. Doku BEYAZ üstüne yalnız parlaklık farkı olarak çiziliyor;
bölüm rengi tint ile veriliyor. Tek doku üç bölümde de doğru çalışıyor.

### [~] 12. Mağaza başlığı ve üst bant — ÖLÇÜLDÜ ve DÜZELTİLDİ

Referanstan (`market.jpeg`) ölçülenler ve uygulananlar:

| öğe | referans | bizde (önce) |
|---|---|---|
| jeton ikonu | X 0.036-0.123 | 0.030-0.135 ✓ zaten doğru |
| jeton kapsülü | X **0.112-0.338** | 0.055-0.315 ✗ sola kaçmış |
| başlık merkezi | **0.524** | 0.575 ✗ sağa kaçmış |
| başlık büyük harf yüksekliği | ekranın **%3.37**'si | %3.15 ✗ küçük |

**Kapsül sola kaçtığı için jeton ikonu tamamen kapsülün ÜSTÜNDE kalıyordu;**
referansta jeton kapsülün SOL UCUNDAN taşıyor. Kapsül 0.112'ye çekildi,
başlık 0.524 merkezine oturtuldu, punto 84 → 90.

**AYNI TUZAK, İKİNCİ EKRAN:** Mağaza başlığında da kontur uygulanmıyordu —
`CreateTitle`'a verilen renk paylaşılan materyal yüzünden yok sayılıyor.
`SetOutline` ile referanstan örneklenen lacivert (`#0A0F55`) kalın kontur
eklendi. Yolculuk'ta da aynısı vardı; **başlık kuran her yerde bu kontrol
edilmeli.**

#### 12'DE KALAN
Tente şeritlerinin kontrastı ve harf kalınlığı (font ağırlığı) referanstan
biraz hafif. Panel köşe yarıçapları gözle kıyaslandı, belirgin fark yok.

### [x] 13. Ayarlar — alt menü çıkmamalı — DÜZELTİLDİ
### [x] 14. Ayarlar — "Hesabımı Sil" görünür oldu + başlık — DÜZELTİLDİ

İkisi de **tek bir sebepten** kaynaklanıyordu.

**Sebep:** Ayarlar ve Profil "tam ekran örtü sayfası" olsun diye
`SetAsLastSibling()` ile "çubuktan sonra çizilsin" diye işaretlenmişti.
Mantık doğruydu ama çubuk onların **kardeşi değil**: ekranlar `Content`'in,
çubuk ise `SafeArea`'nın çocuğu. Kardeş sırası yalnız aynı ebeveyn altında
anlam taşır — çubuk her ekranın üstünde kalmaya devam ediyordu ve
Ayarlar'ın en altındaki "Hesabımı Sil" düğmesi **onun arkasında
kayboluyordu**.

**DERS (kardeş sırası YETMEZ, kardeş OLMAK gerekir).**

**Düzeltme:** `MenuShell` artık tam ekran sayfalarda (`settings`, `profile`)
çubuğu, sekme düğmelerini ve ayraçları tamamen gizliyor.

**Kendi eklediğim küçük hata:** ayraç çizgileri de kökün çocuğu olduğu için
çubukla birlikte gizlenmiyordu; ekranın altında iki dikey çizgi kalıyordu.
Onlar da listeye bağlandı.

**Doğrulama:** Ayarlar'da çubuk kapalı, 5 sekme düğmesi kapalı, ayraç
kapalı, "Delete My Account" referanstaki yerinde görünüyor. Mağazaya
dönünce üçü de geri geliyor.

#### ORTAK BAŞLIK — dört ekran birden
`MenuPage.Header` punto 62 → **72** ve `SetOutline` ile kalın kontur
(`#322192`, referanstan). Ayarlar, Profil, Liderlik ve Koleksiyon aynı
başlığı kullandığı için tek düzeltme dördünü birden etkiledi.

**DERS (ALT UZANTI ölçüyü şişirir):** İlk ölçümde "Ayarlar" %3.47,
"Koleksiyon" %3.17 çıktı ve "başlıklar %50 küçük" gibi göründü. İkisinde de
'y' harfi var; beyaz piksel kutusu alt uzantıyı da sayıyor. Alt uzantısı
olmayan üç başlık (Yolculuk %2.73 · Liderlik %2.69 · Profil %2.78) gerçeği
söyledi. **Punto seçerken ölçülecek şey BÜYÜK HARF yüksekliğidir.**

#### 13'TE KALAN
Anahtarlar referansta daha küçük ve ince konturlu; kapat çarpısı daha küçük
ve başlığa daha yakın; kart kenarlığı daha belirgin.

### [~] 16. Liderlik — İLK ÜÇ ARTIK LİSTEDE DE VAR

**Sebep koddaydı ve gerekçesi yazılıydı:** satırlar kasten `Rivals[i + 3]`
ile başlıyordu, yorumu "podyumdaki yüz iki kez görünmesin" diyordu. Mantıklı
geliyor ama referans (`sıralama.jpeg`) tam tersini yapıyor: 1 Ella,
2 Fikret, 3 KOR podyumda DA listede DE var.

**DERS (podyum listenin yerini tutmaz):** Podyum bir KUTLAMA, liste bir
CETVEL. Cetvelden ilk üçü çıkarınca oyuncu "ben kaçıncıyım, önümde kim var"
sorusunu cevaplayamıyor; üstelik 4. sıradaki isim listenin başında görünüp
birinci sanılıyordu.

**Yan etki — uydurma sıra numaraları gitti.** Satırlarda 997-1000 yazıyordu
ama veri puana göre sıralı; o satırlar aslında 4-7. sıralardı. Ekrandaki
sayı ile verinin anlattığı şey birbirini tutmuyordu. Artık 1,2,3,4,5.

Satır sayısı 4 → **5** (referansta beş rakip satırı var).

**Doğrulama:** podyum aisha/Bet/zzz gösteriyor, liste de 1 aisha 980 ·
2 Bet 910 · 3 zzz 860 · 4 dotsang 800 · 5 player_2u1hw 760.

### [x] 17. Liderlik — sekme düğmelerinin yeri — DÜZELTİLDİ
Yuva X 0.075-0.925 (neredeyse tam genişlik) → **0.107-0.803** (referans
ölçüsü). Sağda kalan boşluğa referanstaki **"i" bilgi düğmesi** eklendi;
basınca puanların nasıl hesaplandığını söyleyen kısa bir açıklama çıkıyor
(düğme gibi duran şey bir şey yapmalı). Geri sayım rozeti de yuvanın sol alt
köşesine hizalandı.

#### 16'DA KALAN
Liste kaydırma, podyumun yeşil park zemini, ilk üç için madalya rozetleri ve
sağdaki "Puan" sütunu (bizde "Level" yazıyor).

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
