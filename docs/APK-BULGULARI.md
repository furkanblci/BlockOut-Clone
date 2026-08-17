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

### [x] 18. Bölüm açılmıyor — GERÇEK SEBEP CİHAZDAN GELDİ: MOTOR KODU KIRPMA
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

---

#### İKİNCİ TUR (2026-08-17 akşam) — HÂLÂ AÇILMIYOR: İKİ AYRI SEBEP BULUNDU

Kullanıcı: *"buildden test ettim leveller gelmiyor"*. `link.xml` sorunu
çözmemiş. Kod okunarak iki gerçek kusur bulundu; **ikisi de bu belirtiyi
birebir üretiyor** ve ikincisi birincinin görülmesini de engelliyordu.

**1. `Dictionary<(int x, int y), int>` — IL2CPP'de bir mayın.**
`LevelLoader.Validate` blok çakışmasını demet (ValueTuple) anahtarlı bir
sözlükte tutuyordu. Editörde kusursuz çalışır. Android/IL2CPP'de ise sözlük
ilk kullanımda `EqualityComparer<ValueTuple<int,int>>.Default` ister; o
karşılaştırıcı YANSIMAYLA üretilir ve ahead-of-time derlemede o örneklem
yoksa `ExecutionEngineException` fırlar. `link.xml` bunu ÇÖZMEZ — mesele
kırpma değil, KOD ÜRETİMİ. Anahtar `y * width + x` tek sayısına çevrildi:
yansıma yok, kutulama yok, üstelik daha hızlı. Bütün proje tarandı, bu
desenden başka örnek yok.

**2. Doğrulamadan sonrasının TAMAMI korumasızdı — asıl körlük buydu.**
`BuildAndStart` yalnız `Parse`'ı try/catch içine almıştı. `Validate`,
`LevelModel.Build`, `BoardBuilder.Build`, `ObstacleSystem`, `GateSystem`,
`DragController`, `PowerUpSystem` — hepsi dışarıdaydı. Oradan fırlayan bir
hata metodu yarıda kesiyor, **`LoadFailure` hiç yazılmıyor** ve oyuncu boş
bir tahtaya hiçbir açıklama olmadan bakıyor. Yani birinci maddedeki hata
tam olarak "sessiz boş ekran" olarak görünürdü. Kurulumun tamamı tek bir
try/catch'e alındı; hata mesajına TÜR ADI da ekleniyor
(`ExecutionEngineException` → IL2CPP/AOT, `NullReferenceException` →
bağlanmamış alan).

**DERS (yalnız ŞÜPHELENDİĞİN satırı korumak, körlüğü taşımaktır):** İlk turda
"parse patlıyor olmalı" diye düşünülüp yalnız o satır korundu. Hata başka
yerden geldi ve ağ orada değildi.

**3. Hata mesajı SIĞMAYAN bir yere yazılıyordu.** `LoadFailure` yardımcı
çubuğunun üstündeki ipucu satırına basılıyordu: 28 punto, **tek satır ve
`NoWrap`**. İki satırlık bir mesaj ekrandan taşıp okunmaz oluyordu — yani
mekanizma vardı ama görünürlük yoktu. Ekranın ortasında, sarmalı açık,
otomatik küçülen bir hata kartı eklendi (`GameplayScreen.BuildFailurePanel`).

**YAN DÜZELTME:** `SpendLifeForAttempt()` kurulumun BAŞINDA çağrılıyordu;
kurulum ortada patlarsa oyuncu hiç oynamadığı bölüm için can kaybediyordu.
Zaten kodun kendi yorumu "can oynanmaya başlarken harcanır" diyordu —
çağrı tahta ayağa kalktıktan sonraya alındı.

**Doğrulama (editör):**
- 50 bölümün hepsi yeni anahtarla parse + validate + model kurulumu:
  **50/50 temiz, 0 hata** — regresyon yok.
- Hata kartı kasten bozuk bir bölümle (rows 3 / height 4) denendi: kart
  ekranın ortasında çıktı ve metnin tamamı okundu
  ("Level doğrulanamadı / rows sayısı (3) height (4) ile uyuşmuyor").

> ⚠️ **Hâlâ cihazda doğrulanacak.** Editörde kırpma ve AOT yok; 1. maddenin
> gerçekten sebep olup olmadığını yalnız APK söyler. Bölüm yine açılmazsa
> artık ekranda kırmızı bir kart ve TÜR ADI olacak — o ad teşhisi tek adımda
> bitirir.

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

### [x] 7. Ana ekran — BİTTİ (kalan iki iş 23. maddede kapandı)

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

#### 7. MADDEDE KALAN — 23. maddede KAPANDI
"Sekme ikonları referansta bizimkilerden büyük ve seçili kartın tonu
çubuktan daha belirgin ayrışıyor" diye bırakılmıştı. İkisi de 23. maddede
çözüldü — ve orada yukarıdaki iki ölçünün de **yanlış** olduğu çıktı:
çubuğun gövdesi `#4F3BD8` değil `#5140E4`, seçili kart çarpanı 1.73 değil
**1.42**. Buradaki `#5340EA` "ışık şeridi" aslında gövdenin kendisiydi;
gerçek ışık şeridi çok daha parlak (`#7771F9`). Tek bir yatay örnekleme
üç katmanı birbirine karıştırmış; dikey tarama ayırdı.

**DERS (renk örneklerken KATMANI da ayır):** bir yüzeyden tek nokta almak,
o noktanın hangi katmana denk geldiğini bilmiyorsan ölçüm değil tahmindir.
Plastik bir yüzeyin üst kenarında 30 piksel içinde dört ayrı renk var.

Ayrıca 22. maddedeki ödül şeridi de bu ekranın parçası; o da kapandı.
**Ana ekran tamam.**

### [arşiv] 7-eski. Ana ekran (özgün istek metni)
- Üstteki jeton ve kalp yazıları büyüyecek; kalp adedi ve "Dolu" yazısının
  puntosu artacak.
- Alttaki "Seviye N" düğmesinin yazısı çok küçük — referanstaki ölçüye çıkacak.
- Üstündeki zorluk ("Zor Seviye") kısmı daha iyi bir düğme/rozet olacak.
- Alt menü çubuğunun zemini ve SEÇİLİ sekme görselleri Block Out'a en yakın
  hâle getirilecek.
Referans: `ana ekran.jpeg`

### [x] 9. Yolculuk — BİTTİ

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

#### 9 — İKİNCİ TUR (2026-08-17): DAİRE %40 BÜYÜKTÜ, ATLAMA DÜĞMELERİ GİTTİ

**Bölge dairesi ölçüldü:** referansta ~600 piksel, yani ekran yüksekliğinin
%29'u → bizim tuvalde **580 birim**. Bizimki **812**'ydi (%42), yani %40
büyük. `RegionH` de 1010 → 760, kilometre adımı 268 → **250** (referans).

**DERS (oranı HANGİ KENARA göre alacaksın?):** Referans telefon 946×2048
(en/boy 0.462), bizim tuval 1080×1920 (0.5625) — onların ekranı BİZDEN DAR.
Daire orada genişliğin %63'ü; aynı fiziksel boyut bizde %54 eder.
`CanvasScaler` YÜKSEKLİĞE eşlendiği için (match=1) doğru referans yükseklik
oranıdır. Genişlik oranını kopyalamak nesneyi fiziksel olarak büyütür — bu
ekranda tam olarak bu olmuş.

**"Üst"/"Alt" atlama düğmeleri kaldırıldı.** Gerekçeleri "referans onları
borunun ucuna koyuyor ve orası boş" idi; ama bizim rayımız daha uzun (beş
bölge) ve o iki nokta hiçbir kaydırma konumunda boş kalmıyor. Tam ekran
yakalamada göründü: "Top" bölgenin `lv 21-40` etiketini, "Alt" da alttaki
bölgenin etiketini ve sekme kartını örtüyor — ikisi de okunmaz oluyordu.
Üç farklı konum denendi; ekran kenardan kenara dolu olduğu için çakışmayan
yer yok.

İşlev kaybolmadı: ekran zaten açılışta oyuncunun bulunduğu kilometre taşını
ortalıyor (`_centeredOnce` + `LateUpdate`), gerisi normal kaydırma.

**DERS (içeriğin üstüne binen kontrol, olmayan kontrolden kötüdür):** Bir
düğmeyi ekranda tutmak için altındaki bilgiyi okunmaz yapmak takas değil,
zarar.

#### DAİRE/ELİPS TARAMASI (21. maddedeki hatanın peşinden)
Profildeki elips hatasından sonra bütün proje `UiSprites.Circle` için
tarandı ve üç yer daha çıktı: **günlük ödül** ve **"Continue?"** panellerinin
kapatma çarpıları, bir de **mağaza taşıyıcı noktaları**. Üçü de düz `Image`
kullanıyordu, yani kutu kare olmadığı anda elipse dönüyorlardı. Hepsi
`UiKit.CreateIcon`'a alındı. Yolculuk'un kilitli bölge maskesi zaten kare
kutudaydı (`DiscSize × DiscSize`) — dokunulmadı.

### [arşiv] 9-eski. Yolculuk (özgün istek metni)
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

### [x] 12. Mağaza başlığı ve üst bant — BİTTİ (tente kontrastı aşağıda, 12. madde ikinci turu)

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

#### 12'DE KALAN — kapandı
Tente kontrastı ölçülüp düzeltildi (bu dosyanın sonundaki 12. madde ikinci
turu). Harf kalınlığı için ölçüm bir fark göstermedi; iki taraf da ExtraBold.

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

#### 13 — İKİNCİ TUR (2026-08-17): KÜÇÜK OLAN ANAHTAR DEĞİL, SATIRDI

Ölçüm (`WhatsApp Image ... (2).jpeg`, 946×2048): kart y 387-1031 → bizim
tuvalde **üst 363, yükseklik 604**, X 0.034-0.966; satır adımı 157 piksel →
**147 birim**. Bizimki 316 / 856 / 196'ydı — kart %40, satırlar %33 fazla
yüksekti.

**DERS (yanlış olan şey, şikâyet edilen şey olmayabilir):** "Anahtarlar
referansta daha küçük" diye not almıştım. Anahtarın SATIR İÇİNDEKİ oranı
zaten doğruymuş; büyük olan onu taşıyan satırdı. Bir öğe büyük görünüyorsa
önce kabını ölç.

Bunlar da düzeldi: kart kenarlığı (referansta karttan koyu ince şerit),
kart rengi #8A84F6 → **#8C7DFE**, anahtar yuvası → **#342B7E**,
Koşullar/Gizlilik yüksekliği 130 → **148** (referansta yeşil "Destek" ile
aynı boyda), ve menü zemini **#171C4E → #1E1858** (bizimki G kanalı R'den
büyük olduğu için yeşile kaçıyordu; referansın her menü zemini mora çalıyor).

Kapat çarpısı 21. maddede düzeldi (elips → daire, referans konumu).

### [x] 16. Liderlik — BİTTİ (kalan yalnız süsleme görselleri)

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

#### LİSTE KAYDIRILABİLİR OLDU
Ekranda beş satır görünüyordu ve o kadarı SABİTTİ — elimizde sekiz rakip
olmasına rağmen kalan üçüne ulaşmanın yolu yoktu.

**DERS (liste, sığdığı kadarından ibaret değildir):** Sıralama listesi doğası
gereği kaydırılır; sığan kadarını gösterip gerisini atmak, listeyi bir
CETVEL olmaktan çıkarıp vitrine çevirir.

Yolculuk'ta öğrenilen kural burada baştan uygulandı: görünmez dokunuş yüzeyi
**içeriğin ilk çocuğu** (viewport'a konsa kardeş öğeleri örterdi).

**DERS (kaydırılabilir alan, ekranın SERBEST kısmı kadardır):** İlk kurulumda
görüntü alanını beş satır yüksekliğinde yaptım ve sabit "You" satırı sekme
çubuğunun ARKASINA düştü. Çubuk ekranın alt %9.9'unu kaplıyor; kaydırma alanı
sabit satır + çubuk payı DÜŞÜLDÜKTEN sonra kalan yere kadar uzayabilir.

**Doğrulama:** 8 satır, içerik 1456 > görüntü 522, boş zeminden sürükleme
ScrollRect'e ulaşıyor, "You" satırı çubuğun üstünde sabit duruyor.

#### PODYUM ARTIK BİR SAHNE + İLK ÜÇ MADALYALI

**Park zemini:** kürsüler düz mavi bir dikdörtgenin üstünde duruyordu.
**DERS (podyum bir SAHNEDİR):** Düz zemin kürsüleri "arayüz öğesi" gibi
gösteriyor; çim şeridi ve birkaç ağaç onları bir YERE koyuyor, kutlama hissi
oradan geliyor. Ağaçlar kendi görselleri olmadan kuruldu — yuvarlak panelden
iki daire (koyu taç + açık taç) ve ince bir gövde.

İki küçük hata yolda düzeltildi:
- İlk denemede dört ağaç kondu, ikisi kürsülerin arkasında kayboldu
  (kürsüler 0.065-0.935 arasını kaplıyor). **Görünmeyen bir süs, olmayan bir
  süstür** — ağaçlar iki kenara alındı ve büyütüldü.
- Kürsüler bandın en altından başlıyordu, çim tamamen arkalarında kalıyordu.
  Artık çimin ÜSTÜNE oturuyorlar.

**Madalya rozetleri:** sekiz satırın rozeti de aynı koyu mordu; liste bir
numaralandırmadan ibaret kalıyordu. İlk üç artık altın/gümüş/bronz ve
üstündeki yazı koyu (altın üstünde beyaz okunmuyor).
**DERS (sıra numarası bir DEĞER taşır):** Renk burada süs değil, bilgi —
göz listeye bakar bakmaz zirveyi buluyor.

#### 16 — İKİNCİ TUR (2026-08-17): ÖDÜL, PUAN, PODYUM, ZEMİN

**Kırmızı rozet PUAN değil ÖDÜL'dü.** Bizde o rozet rakibin puanını
gösteriyor ve SEKİZ satırın hepsinde duruyordu. Referansta aynı yerde bir
jeton yığını + kırmızı kapsül var ve içindeki sayı haftalık ÖDÜL
(2000/1000/500), bu yüzden yalnız ilk üçte. Puan ise sağdaki **"Puan"**
sütununda — bizde orada **"Level"** yazıyordu. Aynı görsel iki farklı
bilgiyi anlatınca liste yanlış okunuyordu: 4. sıradaki oyuncu "ödülüm yok"
değil "puanım yok" gibi görünüyordu.

**Ölü kod bir sorunun iziydi.** Sınıfta `Score(level, progress)` diye bir
yardımcı var — bitirilen bölüm, yıldız ve mükemmel geçişlerden puan
hesaplıyor. **Hiçbir yerden çağrılmıyordu**, çünkü ekran onun yerine seviye
numarasını yazıyordu. Sütun "Puan" olunca hesap da canlandı (oyuncunun
gerçek puanı: 286, play modunda doğrulandı). Rakip verisindeki `level`
alanı artık kullanılmadığı için diziden çıkarıldı — gösterilmeyen veri,
sonradan "acaba nerede kullanılıyor" diye aranan veridir.

**Podyumda kürsüler BİRLEŞİKTİ.** Üçü de 0.155 yarım genişlikteydi ve
merkezleri 0.22/0.50/0.78'di; yani komşular **0.03 kadar üst üste
biniyordu**. Aynı krem rengi paylaştıkları için ekranda üç kürsü değil tek
bir krem kütle görünüyordu — "podyum tasarımları çok zayıf" şikâyetinin
ölçülebilir kısmı buydu. Referansta kürsüler arasında boşluk var ve
ortadaki belirgin biçimde GENİŞ (0.34-0.66; yanlar 0.107-0.325 ve
0.671-0.905). Genişlik de bir sıralama işareti. Her kürsüye kendi kenarlığı
verildi, kapaklar elips olmaktan çıktı (yarıçap kutu yüksekliğinin yarısını
geçince yuvarlak panel elipse döner), madalya `CreateIcon` ile daire kaldı,
ad puntosu 24 → 34 (referansta cap yüksekliği ekranın %1.27'si).

**Sekmelerin arkası MAVİ olmalıydı.** Menü kabuğu her sayfaya aynı koyu
gövdeyi veriyor; Koleksiyon'da doğru ama Liderlik'te referans zemini ikiye
bölüyor: sekme bandı parlak mavi (**#205DF3** üstte, **#246CF4** altta),
liste alanı koyu lacivert (#1B215B). Tek renk verince sekme şeridi ekranda
siyah bir delik gibi kalıyordu.

**DERS (yakalama yöntemi hatayı GİZLİYORDU):** Bu ekran defalarca
`UiCaptureTool.CaptureOf<LeaderboardScreen>` ile doğrulandı ve siyah şerit
hiç görünmedi — o metot yalnız bileşenin KENDİ kanvasını çiziyor. Tam ekran
`UiCaptureTool.Capture(...)` ilk çağrıldığında hem siyah şerit hem birleşik
kürsüler bir anda ortaya çıktı. Bu projede "doğrulama yöntemi hatayı
gizliyor" tuzağının kaçıncı tekrarı olduğunu artık saymıyorum.

#### 16'DA KALAN (yalnız GÖRSEL)
Referansta kürsüler oymalı sütunlar, avatarlar altın çerçeveli ve isim
levhaları süslü. Bunlar görsel işi; yapı ve bilgi tarafı bitti.

### [x] 21. Profil ekranı düzensiz — BİTTİ

**1. Zemin yanlıştı, kutular değil.** Referansta menü sayfalarının zemini
her ekranda AYNI DEĞİL: Ayarlar/Liderlik/Koleksiyon koyu lacivert
(`#1E1856` civarı) ama **Profil belirgin biçimde daha açık bir mor
(`#302488`)**. Biz ortak kabuğa tek bir koyu zemin vermiştik; Profil'in
KOYU istatistik kutuları o zeminden hiç ayrışmıyor, ekran "dağınık"
okunuyordu. Kutu rengimiz zaten doğruydu — yanlış olan arkasıydı.
`MenuPage.Screen` artık isteğe bağlı zemin rengi alıyor.

**2. Aralık, ikonun taşmasını hesaba katmıyordu.** İkon kutunun üst
kenarından %34 taşıyor (tasarım böyle, referans da öyle) ama satır aralığı
30 birimdi. Sonraki satırın taşan ikonu, önceki kutunun altındaki DEĞER
yazısının üstüne biniyordu — ekranda "0" rakamı roketin üstünde yüzüyor gibi
duruyordu.

**DERS (bir öğe taşıyorsa komşusunun payı da o kadar artmalı).**

Aralığı açmak ızgarayı ekrandan taşırdı; **satır adımı referanstan ölçüldü**
(ızgara Y 0.053-0.536 arasında dört satır → adım ekranın %12.1'i = 232
birim) ve kutu ile aralık birlikte ayarlandı: 214+30 → **152+80**.

#### 21 — İKİNCİ TUR (2026-08-17): HER ŞEY DİKEY TARAMAYLA ÖLÇÜLDÜ

İlk turdaki ölçüler GÖZLE yapılmıştı. Kutu dolgusunun koyu olduğunu bilerek
yapılan dikey tarama gerçek sayıları verdi ve ilk turun neredeyse tamamının
%6 küçük olduğu çıktı:

| ölçü | eski | referans (ölçüldü) |
|---|---|---|
| kart üst / yükseklik | 330 / 340 | **387 / 271** |
| kart X | 0.055-0.945 | 0.058-0.941 |
| kutu yüksekliği | 152 | **161** |
| satır adımı | 232 | **255** |
| ızgara üstü | 902 | **891** |
| "Seviye" puntosu | 44 | **52** |
| seviye sayısı | 64 | **74** |
| kart rengi | #8A84F6 | **#A79BFD** |
| kutu dolgusu | #211C53 | **#1D1450** |

**Kutulara kenarlık geldi.** Referansta 4 pikselik bir ara ton var
(**#2A1E74**): zeminden koyu, dolgudan açık. O şerit olmadan koyu kutular
açık mor zeminde "kesilmiş delik" gibi duruyordu.

**Ad kapsülü zaten avatarın altındaydı** — ilk turun notu yanlıştı. Ölçüm
doğruladı: referansta plaka X 0.119-0.590, avatar 0.211-0.492, yani plaka
avatarın iki yanından da taşıyor ve ikisi tek bir kimlik bloğu okunuyor.

#### DAİRE/ELİPS HATASI — BU PROJEDE ÜÇÜNCÜ KEZ (kullanıcı yakaladı)
*"profildeki şu kısımı düzelt çarpı işareti de çok basık gibi"*.

Kapatma çarpısı 162×125 birimlik bir kutuya konuyordu ve `UiSprites.Circle`
düz bir `Image` olarak kutuya GERİLİYORDU → elips. Aynısı kalem rozetinde
de vardı (121×105). Referansta çarpı 104×105 piksel, yani tam daire.

**DERS:** `Image` sprite'ı kutuya gerer, `UiKit.CreateIcon` en-boy oranını
KORUR. Daire çizen her yerde ikincisi kullanılmalı. Aynı hata daha önce
yeşil artı düğmesinde ve liderlik madalyasında da çıktı — üçünde de sebep
"kutu kare sanılıyordu" idi. Doğru bileşen seçilirse hata hiç doğmuyor.

Kalem de düzeldi: 53×17'lik kapsül ekranda BEYAZ BİR OVAL okunuyordu;
62×11 + koyu uç ile kalem gibi duruyor.

**`badge_reward` "Blok Ligi" ikonundan kaldırıldı:** o görsel içi boş
turuncu bir çerçeve (ortasında mor pencere) ve tek başına konunca
"yüklenmemiş ikon" gibi duruyor. Yerine küre kondu. (Aynı görsel ana
ekranın ödül şeridinde de yanlış kullanılıyordu — 22. madde.)

### [~] 5. Koleksiyon — YAZI ve BAŞLIK düzeltildi, GÖRSEL bekliyor

**Başlık** ortak `MenuPage.Header` düzeltmesiyle zaten düzeldi (punto + kontur).

**Yazı ölçüldü:** referansta cümle ekranın neredeyse tamamını kaplıyor
(X 0.044-0.957), büyük harf yüksekliği ekranın **%2.69**'u, Y 0.271-0.299.
Bizdeki 40 punto **%1.5** veriyordu — yarı yarıya küçüktü ve ekranın
ortasında kaybolmuş bir alt yazı gibi duruyordu. 40 → **72**.

**Seviye kısmı ALTIN oldu** (`#FCC21E`, referanstan örneklendi). Referansta
cümlenin tamamı beyaz değil: "Koleksiyonu **Seviye 95'de** Aç!" — ortadaki
koşul vurgulu. Tek renkte yazınca cümle bir duyuru gibi okunuyor;
vurgulanınca oyuncunun aradığı SAYI öne çıkıyor. TMP zengin metniyle.

**İngilizce metin Türkçesinden uzun** olduğu için 72 puntoda sağdan taşıyordu;
otomatik küçültme eklendi (72 üst sınır, 46 alt sınır).
**DERS (yine):** `enableAutoSizing` tek başına çalışmaz — `overflowMode`
kısıtlayıcı olmazsa TMP küçültmek yerine kutudan taşırır.

#### 5'TE KALAN — GÖRSEL İSTEĞİ
Referansta ortadaki görsel **açık bir kitap + çevresinde bloklar**; bizde
sandık duruyor. Sekme ikonu da referansta kırmızı-altın çerçeveli bir
"albüm". İki istem `docs/art-prompts.md` §11'e yazıldı. Gelince yalnız
anahtar değişecek, düzen aynı kalacak.

### [x] 4. Günlük ödül (Daily Reward) paneli — YENİDEN KURULDU
Kullanıcı kararı (2026-08-17): *"günlük ödül referans görüntüsü çok farklı
oyundan ama onu kullanmayalım... oyunun genel görsel ui tarzına yakın daha
güncel hale getirelim yeter"*. Yani **referanssız**, oyunun kendi diliyle.

Eski panelin somut kusurları (yakalandı, `daily_now.png`):
- Krem `panel_card` zemini oyunun mor diline yabancıydı.
- "Day 1" **18 punto ve %65 saydam beyaz**, koyu lacivert kutunun üstünde
  neredeyse görünmüyordu.
- Bir gün için TEK simge çiziliyor, altına **her zaman jeton sayısı**
  yazılıyordu. 5. gün "kalp + 200" görünüyordu; 200 kalp değil jetondu,
  kalp 1 taneydi. Panel **yanlış bilgi veriyordu**.
- "CLAIM" yazısı yalnız `Refresh` içinde yazılıyordu; Refresh'siz açılışta
  düğme boş yeşil bir çubuktu.

Yeni panel (`DailyRewardPanel.cs` baştan yazıldı):
- Koyu kenarlı mor kart + üst kenarına binen kapsül başlık + koyu oyuk —
  aynı parçalar Yolculuk, Mağaza ve "Continue?" panelinde de var.
- Bir günün hediyeleri **çip** olarak yan yana: her çipin kendi simgesi ve
  kendi sayısı (`120` / `x1`). 7. gün üç hediye taşıdığı için kutusu **iki
  hücre geniş** — haftanın büyük ödülü boyuyla anlatılıyor.
- Gün etiketi 18 → **30 punto**, kendi koyu kapsülünün üstünde; bugünkü
  günde kapsül altın, yazı koyu mürekkep. Perdeden SONRA kuruluyor, yani
  alınmış günün kutusu kararsa da gün numarası okunur kalıyor.
- Alınmış günün tiki **simgenin sağ üst köşesinde**: ortadaydı ve jetonu
  tamamen örtüyordu; alt köşede miktarı ("50" → "5C") kesiyordu.
- Bugünün kutusunda altın kenar + ışık huzmesi + %5 ölçek; 7. gün
  alınmadığı sürece soluk bir parıltı taşıyor.

**BULUNAN GERÇEK HATA — `PendingDay` iki farklı şey söylüyor.** Ödül
beklerken bugün alınacak günü verir; ödül ALINDIKTAN sonra `DailyStreak`'e
düşer, yani az önce alınan günü verir. "alınmış = gün < pending" kuralı
ikinci durumda yanlıştı: CLAIM'e basınca 3. gün hâlâ altın "bugün" kutusu
olarak kalıyor, tik hiç gelmiyordu. `Available` da sorularak düzeltildi.

Doğrulama (play modu, **gerçek raycast** ile — `onClick.Invoke()` değil):
- 3. gün: jeton 1320 → 1440 (+120), Clock 0 → 1, streak 3.
- 5. gün: jeton 1440 → 1640 (+200), streak 5.
- 2. gün: alındıktan sonra gün 1 ve 2 tik=True, altın vurgu yok, ölçek 1.00,
  alt başlık "2 day streak! Come back tomorrow.", düğme "SEE YOU TOMORROW".
- Ana ekran sayacı panel açıkken tazeleniyor (1320 → 1440 üst çubukta).

**AÇIK KALAN (bu maddeye ait değil, EKONOMİ kararı):** 5. ve 7. günün can
hediyesi `LivesService.Grant` ile veriliyor ve o metot üst sınırı aşmıyor.
Canı doluyken ödülü alan oyuncu kalbi **kaybediyor** (5 → 5 ölçüldü). Aynı
şey reklam ödülü ve mağazadan can satın alma için de geçerli — yani
çözümü tek panelde değil, taşan canı saklayacak bir alanla olmalı.

### [x] 22. Ana ekran "Rewards x2" şeridi — YENİDEN KURULDU
Kullanıcı (2026-08-17): *"şu butonun da değişmesi şart çok kötü gözüküyor
referansla alakası yok ve yazı okunmuyor"*.

Sebep **görsel seçimiydi**: şerit `badge_reward.png` kullanıyordu ve o
görsel KARE bir çerçeve — turuncu kenarlık, ortasında MOR bir pencere.
1.9:1 bir şeride gerdirilince kenarlık inceliyor, ortadaki mor pencere de
yazının zemini oluyordu; 26 puntoluk beyaz yazı açık mor üstünde
kayboluyordu.

Referans (`ana ekran.jpeg`, 946×2048) ölçüldü: plaka x 338-606 / y 1482-1542,
düğme x 250-700 / y 1536-1720. Yani plaka düğme genişliğinin **%59.5**'i ve
düğmenin üstünden düğme yüksekliğinin **%29**'u kadar çıkıyor; yazının büyük
harf yüksekliği ekranın **%1.46**'sı (26 punto %0.95 veriyordu).

Yapılan: dolu turuncu plaka (kenar `#5B2A08`, yüzey `#FBA40A`, üstte ışık),
**koyu kahve yazı + krem kontur**, "x2" çarpanı `<size=76%>` ile daha küçük,
punto 26 → 40 (taşarsa 28'e kadar küçülüyor). Şerit artık düğme kökünün
**ilk kardeşi**: alt kenarı düğmenin arkasında kayboluyor — referansta da
öyle, "yapıştırılmış" değil "takılmış" duruyor.

### [x] 23. Alt sekme çubuğu ve seçili sekme — REFERANS RENGİNE ÇEKİLDİ
Kullanıcı (2026-08-17): *"alt menü için daha iyi orjinale benzer bir
arkaplan tasarımı ve seçili buton için de görsel lazım"*.

Sebep yine görsel: `bar_tabs.png` ve `card_tab.png` **mor** (#5B1FB8
civarı), referanstaki çubuk **mavi-mor** (#5140E4). Boyama çarpma olduğu
için moru maviye çevirmek mümkün değil.

Dikey tarama referansın plastik dudağını katman katman verdi:
`#2D1B87` koyu dış kenar · `#5C4BD8` ince ışık · `#231578` koyu oyuk ·
`#7771F9` üst parlaklık · `#5140E4` gövde. Dördü ince şerit olarak kuruldu.

Seçili kart da prosedürel: yüzey `#6B65F9`, kenarlık `#291B8C` (20 birim).
İki ölçü hatası düzeldi:
- **Kart slottan GENİŞ olmalı.** Referansta kart ekranın %28.3'ü, bir slot
  %20 — yani kart slotunun 1.42 katı ve komşulara taşıyor. Bizde kart
  slotun İÇİNE (0.05-0.95) sığdırılmıştı, referansın yarısı kadar genişti.
- **Kartın üst kenarı** ekranın altından %13.48'te (bizde %16.4 idi; kart
  bir baş boyu uzundu). Alt kenarı da artık ekranın dışına taşıyor —
  görünen yuvarlak alt köşe kartı "çubuğun üstüne konmuş ayrı bir kutu"
  gösteriyordu.
- Sekme yazısı 24 → 40 punto (referansta büyük harf yüksekliği ekranın
  %1.71'i), ikonlar büyütüldü.

`preserveAspect` dersi: ikon kutuyu DOLDURMAZ, kutuya SIĞAR. İkonu
genişletmek hiçbir şey değiştirmiyordu; sınırlayan kenar yükseklikti.

Görsel isteği (zorunlu değil, kaliteyi yükseltir) `docs/art-prompts.md`
§12'ye yazıldı — doğru renkte `bar_tabs` ve `card_tab`.

---

## KAPANAN MADDELER

_(Bir madde bitince buraya taşınır: ne yapıldı, nasıl doğrulandı.)_

---

### [x] 24. Cihazda hata görünmüyor — HATA KATI EKLENDİ
`Assets/_Project/Scripts/Runtime/Flow/DeviceErrorOverlay.cs`

18. maddenin iki turu da aynı şeye takıldı: **hatayı bildirecek mekanizma
vardı ama hep BİR EKRANA bağlıydı.** Hata o ekran kurulmadan önce olursa
(Awake, menü kurulumu, ölü bir düğme) kimse görmüyordu; telefonda konsol yok,
logcat için kablo gerekiyor, yani her sessiz hata bir APK turu kaybettiriyor.

Yeni kat `Application.logMessageReceivedThreaded`'a bağlanıyor —
`Debug.LogError`, `Debug.LogException` ve **yakalanmamış istisnaların
hepsini** kodun hiçbir yerine dokunmadan topluyor. `AppRoot.Awake`'te, her
şeyden ÖNCE ve kendi kanvasında (sortingOrder 32000) kuruluyor.

**DERS (teşhis aracı, teşhis edilecek şeyden BAĞIMSIZ olmalı):** Bir ekranın
hatasını o ekrana yazdırırsan, ekran hiç kurulamadığında elin boş kalır.

İki incelik:
- **Panel tam opak.** Yarı saydam bir teşhis penceresinin arkasından manzara
  sızıyor ve asıl işi olan okunurluk düşüyordu.
- **Yalnız DAHA ÖNCE GÖRÜLMEMİŞ hata paneli açar.** `Update` içinden gelen
  bir NullReference her kare tekrarlar; ilk tasarımda panel KAPAT'a basılsa
  bile bir sonraki karede geri geliyordu, yani oyun kilitlenmiş gibi
  oluyordu. Tekrarlar artık yalnız sayaca yazılıyor.
  **DERS: kapatılamayan bir teşhis penceresi, teşhis aracı değil engeldir.**

Doğrulama: play modunda kasten bir `LogError` ve bir
`ExecutionEngineException` fırlatıldı; ikisi de yığın satırlarıyla birlikte
ekranda göründü.

> **YAYINA ÇIKARKEN:** `DeviceErrorOverlay.Enabled` → `false`.

### [x] 12. Mağaza tente kontrastı — ÖLÇÜLDÜ ve DÜZELTİLDİ
İlk örnekleme koyu #0356FB / açık #0084FC vermişti. Ekrandaki SONUÇ ölçülünce
(#034CDF ve #0075E0) iki sorun çıktı: açık şerit referanstakinden belirgin
biçimde KOYU ve ikisi de MORA kaçıyordu. Referansın gerçek çifti **#0066DC**
ve **#01A1F5** — açık şerit çok daha camgöbeği (G kanalı 117 değil 161).
Parlaklık oranı bizde **1.32**, referansta **1.43**.

**DERS (kaynağı değil SONUCU ölç):** Sprite'ın taban rengi ekranda göründüğü
renk değil — üstüne dikey parlaklık rampası biniyor. Kıyaslanacak şey
ekrandaki piksel; taban renk ondan geri hesaplanmalı (÷0.945).

Feston derinliği de düzeltildi (%11.5 → **%14**) ve tente yüksekliği 263 →
**268** birim. İlk feston ölçümüm %23 demişti; o rakam tentenin dışındaki bir
mavi pikselden kirlenmişti — **periyot içinde ölçmek** doğrusu.

Tente altındaki çentik şeridi de tentenin üst satırının ortalamasına çekildi
(#053AE8 → **#016FC3**); çentiksiz bir telefonda ikisi arasında görünür bir
dikiş kalıyordu.

**Kalan:** "harf kalınlığı biraz hafif" notu için ölçüm bir fark göstermedi
(iki taraf da ExtraBold); değiştirilmedi.

### [x] 25. Aynı sayı iki ekranda iki türlü yazılıyordu
Mağaza `Amount()` ile "1 720" yazıyordu, ana ekranın üst çubuğu düz
`ToString()` ile "1720". Ortak biçim `MenuPage.Amount`'a alındı (referans
binlik ayıracı olarak BOŞLUK kullanıyor — `market.jpeg` paket tutarları).
Biçim `InvariantCulture` üzerinden gidiyor: `ToString("N0")` tek başına
cihazın diline bağlı ve Türkçe bir telefonda "1.720" verir.

### [x] 26. Canı doluyken alınan can ödülü KAYBOLUYORDU — düzeltildi
Bu, 4. maddeyi yaparken bulunmuş ve "ekonomiye dokunuyor" diye açık
bırakılmıştı. Kullanıcı "eksik ne varsa yap" deyince yapıldı.

`LivesService.Grant` üst sınırı aşan kısmı **sessizce atıyordu**. Canı
doluyken günlük ödülün 5. veya 7. gününü alan, "can kazan" reklamını izleyen
ya da mağazadan can satın alan oyuncu, kendisine SÖZ VERİLEN canı hiç
görmüyordu — üstelik hiçbir uyarı da yoktu (5 → 5 ölçülmüştü).

"Sınırı aşma" kuralı doğru; yanlış olan aşan kısmı yok saymaktı. Artık fazlası
`ILivesState.BankedLives`'da **bekliyor** ve oyuncu can harcadıkça
(`TrySpend` → `DrainBank`) geri veriliyor. Denge sayılarının hiçbiri
değişmedi; yalnız kayıp durdu.

Eski kayıtlarla uyumlu: `bankedLives` alanı yoksa Newtonsoft 0 ile dolduruyor,
sürüm yükseltmeye gerek yok.

**Doğrulama (play modu):** can 5/5 · banka 0 → doluyken +2 verildi
(can 5, banka 2) → üç bölüm oynandı → can 4, banka 0.
**5 + 2 ödül − 3 harcama = 4** ✓ kayıp yok.

**DERS (sınır koymak, aşanı ATMAK demek değildir):** İlk ölçümde bunu bir
"tasarım tercihi" sanmıştım. Oyuncuya bir şey söz verip vermemek tercih
değil, hatadır — kural sınırı korumalı ama sözü de tutmalı.

---

### [x] 27. ARAYÜZ TASARIM SİSTEMİ — panelleri elle düzenleme
Kullanıcı isteği: *"uiları bütün panelleri tasarımları ben elimle
değiştirebileyim revize edebileyim... profesyonel olsun optimizasyonu
etkileyecek bir şey de olmasın"*.

**Prefab'a çevirmedik** — bu projenin arayüzü bilerek kodla kuruluyor ve
ölçülerin hangi referans karesinden, hangi piksel oranından geldiği kodun
yanındaki yorumda duruyor (bkz. `UiKit` başındaki ders). Prefab'a çevirmek
o gerekçelerin tamamını çöpe atardı ve prefab YAML'ı birleştirmede çakışır.

Bunun yerine **fark (override) katmanı**: kod hâlâ tek kaynak, elle yapılan
değişiklik onun ÜSTÜNE binen bir liste. Kod bir ölçüyü değiştirdiğinde elle
dokunulmamış her şey yeni değeri kendiliğinden alır — donmuş bir kopya
kalmaz.

**Kullanım** (`Tools > Block Out > Arayüz Tasarımı`, `Ctrl+Shift+U`):
1. Play moduna gir, düzenleyeceğin ekranı aç
2. **Referans al**
3. Hiyerarşide/Sahnede normal Unity araçlarıyla oynat (konum, boyut, renk,
   punto, aç/kapa)
4. **Değişiklikleri kaydet** → yalnız FARKLAR
   `Resources/UiLayout.asset`'e yazılır

Ekran başına "Sıfırla" düğmesi kodun ürettiği hâle döndürür.

**Optimizasyon:** çalışma anında hiçbir karede iş yok. Ekran kurulduktan
sonra bir kez hiyerarşi geziliyor; kayıtlı düzeltme yoksa `ApplyAll` ilk
satırda dönüyor. Tasarım penceresi editör kodu, derlemeye hiç girmiyor.
Ekranlardaki işaret (`UiTweakRoot`) tek bir string alan taşıyor.

**İKİ GERÇEK HATA — ikisi de sınavda çıktı, ikisi de SESSİZ:**

1. **ScriptableObject kendi adıyla aynı dosyada olmak zorunda.**
   `UiLayoutAsset` önce `UiTweak.cs` içindeydi; kod sorunsuz derlendi ama
   `AssetDatabase.CreateAsset` "No script asset for UiLayoutAsset" **uyarısı**
   verip script referansı BOŞ bir varlık üretti. Varlık diske yazılıyor,
   `Resources.Load` onu buluyor gibi görünüyor, ama veri geri okunamıyor.
   Uyarı seviyesinde kaldığı için kolayca gözden kaçıyor.

2. **`Start` yetmez, çünkü `Start`'ların sırası tanımsızdır.** Uygulama
   çağrısı önce doğrudan `AppRoot.Start`'taydı ve düzeltmeler hiç
   uygulanmadı — üstelik hata da vermedi, çünkü ortada hata yoktu: ekranlar
   o an henüz kurulmamıştı ve gezilecek çocuk yoktu. "Benden sonra kurulan"
   bir şeyi beklemenin tek güvenli yolu bir kare geçirmek.

**Doğrulama (uçtan uca):** 7 ekran işaretli bulundu; ana ekrandaki Oyna
düğmesi "elle" 0.158 → 0.188'e taşındı, kaydedildi, **play yeniden
başlatıldı** ve değer 0.188 olarak geldi. Sınav düzeltmesi sonra silindi,
varlık boş bırakıldı.

---

### 18 — ÜÇÜNCÜ TUR (2026-08-17 gece): CİHAZ CEVABI VERDİ

**Hata katı işini yaptı.** Kullanıcı build'i aldı ve ekrandaki kart iki satırla
sebebi söyledi — teşhis tek turda bitti:

```
Can't add component because class 'MeshCollider' doesn't exist!
    UnityEngine.GameObject:CreatePrimitive(PrimitiveType)

[GameSession] Level kurulamadı: ArgumentNullException:
Value cannot be null. Parameter name: shader
```

**Sebep Newtonsoft DEĞİLMİŞ.** İki ayrı MOTOR KODU KIRPMA sorunu:

#### 1. Shader'lar derlemeye hiç girmiyordu
Bütün görsel katman materyalleri `Shader.Find(...)` ile kuruyor. Bir shader
ne "Always Included Shaders" listesindeyse ne de gönderilen bir materyalden
referanslıysa, **derlemede yoktur** — `Shader.Find` null döner ve
`new Material(null)` `ArgumentNullException` atar.

Ölçüldü: kodun aradığı beş shader'dan **dördü listede yoktu**. Eklendi:
`Universal Render Pipeline/Lit`, `.../Unlit`, `.../Particles/Unlit`,
`BlockOut/Brick`. (`Sprites/Default` zaten vardı.)

**DERS (`Shader.Find` editörde her zaman çalışır):** Editörde bütün shader'lar
yüklüdür; `Shader.Find` orada hiç null dönmez. Bu çağrının derlemede
çalışması, shader'ın derlemeye GİRDİĞİNİ ayrıca garanti etmene bağlı.

#### 2. `MeshCollider` sınıfı kırpılmıştı
`GameObject.CreatePrimitive` nesneye **her zaman** bir çarpıştırıcı ekler.
Bu oyunda fizik yok, o yüzden kod onu hemen siliyordu — ama Android
derlemesinde `stripEngineCode` açık ve fizik modülünü *gerçekten kullanan*
kod olmadığı için Unity `MeshCollider` sınıfını atıyor. Nesne HİÇ
kurulamıyor, tahta boş kalıyor.

Çözüm modülü zorla korumak değil, ona hiç dokunmamak oldu:
`ViewKit.CreateShape(PrimitiveType, ad)` — `MeshFilter` + `MeshRenderer` +
Unity'nin yerleşik ağı, çarpıştırıcı yok. 12 çağrı yeri geçirildi,
`StripCollider` ve altı `GetComponent<Collider>()` çağrısı silindi.
`FXService` küp ağını almak için nesne yaratıp siliyordu; artık doğrudan
yerleşik ağı okuyor.

**DERS (kullanmadığın şeyi İSTEME):** Kırpıcı "kimse kullanmıyorsa at" diye
çalışır. Kodun *geçici olarak* dokunduğu her modül, o modülü derlemede tutmak
zorunda kalmak ya da orada patlamak demektir. İstemediğin bir bileşeni
ekleyip silmek, olmadığı ortamda çökme sebebine dönüşüyor.

**Doğrulama (editör):** 1., 25. ve 50. bölüm — üçü de `Playing`, yükleme
hatası yok; sahnede **0 çarpıştırıcı**, **0 boş ağ**, **0 boş
materyal/shader**. 1. bölümde JSON 2 blok + 2 kapı diyor, sahnede 2 blok
meshi + 4 kapı meshi (çubuk + ok) var.

> ⚠️ Cihazda tekrar doğrulanacak — editörde kırpma yok. Ama bu sefer sebep
> TAHMİN değil, cihazın kendi söylediği şey.

**Önceki iki turun düzeltmeleri yerinde kalıyor** (IL2CPP demet anahtarı,
korumasız kurulum, görünür hata kartı): hiçbiri bu hatanın sebebi değildi
ama üçü de gerçek kusurdu ve hata katının o mesajı gösterebilmesi ikinci
turdaki try/catch genişletmesi sayesinde oldu.
