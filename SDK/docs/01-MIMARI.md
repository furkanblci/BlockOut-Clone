# Mimari — neden böyle kuruldu

Bu dosya SDK'nın taşıdığı **kararları** ve gerekçelerini anlatır. Karar
gerekçesini bilmeden kodu değiştirmek, aynı hatayı ikinci kez yapmanın en hızlı
yoludur.

---

## 1. Tek sahne — `Boot` + `Main`

Menü ve oynanış **aynı sahnede** iki köktür; geçiş sahne yüklemeden, kökler
açılıp kapanarak yapılır.

**Neden:** Casual bulmaca oyunlarının sektör standardı budur. Üç sebebi var:

1. **Mobilde sahne yüklemek gözle görülür bir takılma yaratır.** "Oyna"ya
   basınca oyun ANINDA başlamalı.
2. **Üst bar, ses, titreşim, kayıt ekranlar arasında ORTAKTIR.** Her sahnede
   yeniden kurmak hem israf, hem de "ayarlar ekranı ses servisini bulamıyor"
   gibi sinsi hataların kaynağı.
3. **Kalıcı kök** (`AppRoot.PersistentRoot`) hiç kapanmaz: menü kökü kapanınca
   müzik susmaz, oyun kökü kapanınca kayıt servisi kaybolmaz.

**Yaşanmış hata:** ses ve titreşim eskiden oynanış kurulumunda doğuyordu.
Oynanış kökü kapalı başladığı için `Start` hiç koşmuyor, yani oyuncu **ilk
bölüme girene kadar** sahnede hiç `AudioService` olmuyordu — ana ekranın,
mağazanın, ayarların bütün düğmeleri sessiz basılıyordu ve Ayarlar'daki "Sesler"
anahtarı uygulanacak bir örnek bulamadığı için sessizce hiçbir şey yapmıyordu.

> **Menü kabuğu da oynanış kadar oyundur.** Servis "oyun başlayınca değil,
> uygulama açılınca" kurulur.

---

## 2. Arayüz PREFAB değil, KOD

Ekranların hiçbiri prefab değil; hepsi `UiKit` çağrılarıyla koddan doğuyor.

**Neden:**

| Prefab | Kod |
|---|---|
| İkili dosya — diff okunmaz | Metin — diff okunur, PR'da incelenir |
| Birleştirme çakışması cehennemi | Normal metin çakışması |
| "Şu değer nereden geliyor?" → sahnede ara | Aramaya gerek yok, kod orada |
| Ölçü değiştirmek = elle sürükleme | Ölçü değiştirmek = sayı değiştirme |
| Referanstan ölçüp uygulamak zor | Ölçtüğün sayıyı doğrudan yazarsın |

Bu proje referans oyunun ekran görüntülerini **piksel piksel ölçerek**
kuruldu (bkz. `03-UI-DILI.md`). Ölçülen sayıyı koda yazmak, sahnede sürükleyip
yaklaştırmaktan hem daha hızlı hem de tekrarlanabilir.

**Bedeli ve çözümü:** kodla kurulan arayüzü "gözle ayarlamak" zordur. Bu yüzden
`Arayüz Tasarımı` penceresi (`Ctrl+Shift+U`) var — bkz. §4.

---

## 3. Statik cephe, örnek gövde

`AppRouter`, `AudioService`, `MetaServices` gibi giriş noktaları **statiktir**;
işi yapan gerçek nesneler değil.

**Neden:** çağıran taraflar (ana ekran düğmesi, HUD, bitiş kartı) bir referans
taşımak zorunda kalmasın. Ama mantık test edilebilir bir bileşende dursun.

**Kaçınılan şey:** servislerin KENDİLERİNİN statik olması. Sınıflar normal,
bağımlılıklarını kurucudan alıyor; yalnız **besteci** statik
(`MetaServices.Compose`). Böylece testte sahte depo ve sahte saatle kendi
örneğini kurabiliyorsun.

**Neden `[RuntimeInitializeOnLoadMethod]`:** Unity'de tek örnek kurmanın üç
yolu var — (a) her sahneye koyulan MonoBehaviour singleton: sahne sırasına
bağımlı, kırılgan; (b) ScriptableObject: asset'e yazma riski; (c) ilk kareden
önce kurulan saf C# nesneleri. (c) seçildi: sahneye bağlı değil, Play'e
basıldığı anda hazır.

---

## 4. `UiTweak` — elle düzeltmeleri FARK olarak sakla

Kodla kurulan arayüzü elle ayarlamak isteyince iki kötü seçenek var: ya kodu
her ayarda yeniden yaz, ya da arayüzü prefab'a çevir.

Üçüncü yol: ekran koddan kurulur, sonra kaydedilmiş **farklar** uygulanır.

```
kod kurar  →  UiTweak.Apply(root, "StoreScreen")  →  ekran
                        ↑
               Resources/UiLayout.asset
               (yalnız DEĞİŞEN düğümler)
```

- Kayıtlı düzeltme yoksa (yayın hâli) `ApplyAll` ilk satırda döner — sıfıra
  yakın maliyet.
- Fark **hiyerarşi yoluna** göre saklanır: `"HomeScreen/Play/Ribbon"`.
- Kodda o düğümü yeniden adlandırırsan düzeltme sessizce düşer. **Anahtar
  yanlışsa araç hata vermez, sadece kaydettiğin hiçbir şey görünmez.** Bu,
  `UiPanelCatalog`'un en kritik yeri.

**İşaretle + uygula, birlikte.** `AppRoot` tek seferlik bir `ApplyAll` çağırıyor;
o çağrıdan SONRA doğan paneller yalnız işaretlenirse düzeltmeleri hiç
uygulanmaz. Sonradan doğan panel hem `Mark` hem `Apply` çağırmalı.

---

## 5. Sprite'lar PROSEDÜREL

`UiSprites` (1.355 satır) ve `MenuSprites` (1.673 satır) arayüz parçalarını
çalışma anında çiziyor: yuvarlak panel, kapsül, halka, gölge, flama, rozet.

**Neden:**
- Yeni projede **sıfır görsel asset ile** çalışan bir arayüzün olur; gerçek
  görseller sonra üstüne gelir.
- Renk ve yarıçap birer parametre — sanatçıya gitmeden denenir.
- 9-slice ayarı, mipmap, sıkıştırma derdi yok.

**Tuzağı:** prosedürel maske kodunda `Mathf.SmoothStep` **GLSL'in
`smoothstep`'i değildir** ve sessizce her şeyi saydam yapar. Bu SDK aylarca bu
hatayı taşıdı. Ayrıntı: `02-TUZAKLAR.md` §9.

---

## 6. Ses: dosya varsa dosya, yoksa sentez

Her ses önce `AudioSkin`'den aranır; bulunamazsa `SfxSynth` ile kodla üretilmiş
yer tutucuya düşülür.

**Neden:** ses toplama işi uzun sürer ve parça parça ilerler. Tek bir dosya
eklendiği anda devreye girer, geri kalanını beklemez ve **eksik dosya oyunu
sessiz bırakmaz.**

Aynı desen görsellerde de var (`UiSkin.Get` bulamazsa prosedürel çizime düşer).

> **Genel ilke:** eksik içerik oyunu bozmamalı, yalnız fakirleştirmeli.

---

## 7. Kayıt sürümlüdür ve kendini kurtarır

`SaveService<T>`:
- **Sürüm numarası** taşır; eski sürüm `Upgrade` ile yükseltilir
- Ana dosya bozuksa **yedekten** kurtarır
- Gelecekten gelen kayıt (daha yeni sürüm) **salt okunur** çalıştırılır —
  üstüne yazıp kullanıcının ilerlemesini yok etmez
- `ISaveStore` soyutlaması sayesinde testte bellek deposu kullanılır

`SaveLoadOutcome` her açılışta loglanır; hangi yoldan gelindiği görünür.

---

## 8. Katman sınırları — SDK oyunu nereden tanır?

Tam **üç** nokta. Başka hiçbir yerde SDK oyunun tipine bakmaz:

| Arayüz | Kim uygular | Ne için |
|---|---|---|
| `IGameHost` | oyunun bir sınıfı | bölüm sayısı/kimliği, tüketilebilir listesi |
| `IGameplayHost` | oynanış oturumu | bölümü başlat / durdur |
| `IHomeScreen` | oyunun ana ekranı | tazele / kaydır |

Artı üç **kanca** (arayüz değil, delege):

| Kanca | Ne için |
|---|---|
| `AppRoot.DevMenuInstaller` | oyunun hata ayıklama menüsü |
| `AudioService.PaletteExtender` | oyunun kendi sesleri |
| `UiPanelCatalog.GameEntries` | oyunun kendi panelleri (düzenleyici) |
| `AndroidBuildTool.EnsureBuildScenes` | derleme sahne listesi |

**Neden bazıları arayüz, bazıları kanca?** Arayüz, birden çok üyesi olan ve
"bir şeyin ne olduğunu" tanımlayan sözleşmeler için. Kanca, tek bir işi olan
ve "ne zaman" sorusunun cevabı SDK'ya ait olan yerler için. Kancayı arayüze
çevirmek, yeni oyunda doldurulacak boş metot sayısını artırırdı.

**Arayüzü küçük tut.** Buraya "oyunun her şeyi" konabilirdi. Konmadı — yalnız
SDK ekranlarının GERÇEKTEN sorduğu şeyler var. Arayüz büyüdükçe yeni oyunda
doldurulması gereken boşluk büyür ve SDK'yı kullanmak sıfırdan yazmaktan
zahmetli hâle gelir.

---

## 9. Bağlanmamışken de çalışmalı

`GameHost.Current` hiç bağlanmadıysa boş bir uygulamaya düşer — sıfır bölüm,
sıfır öğe. Ekranlar boş görünür ama **hiçbiri patlamaz**.

**Neden:** alternatifi olan `NullReferenceException`, yeni bir projede SDK'yı
ilk kez çalıştıran kişinin karşısına "oyun açılmıyor" olarak çıkardı ve sebebi
bulmak dakikalar alırdı. Aynı gerekçe `Home.Current`, `DevMenuInstaller`,
`PaletteExtender` için de geçerli.

---

## 10. Hata katı en önce kurulur

`DeviceErrorOverlay` cihazda konsol yerine geçer: yakalanmamış her hatayı
ekrana basar. `AppRoot.Awake`'te **ilk** o kurulur — kendisinden sonra kurulan
hiçbir şeye ihtiyacı yok ve onların hatalarını yakalayabilmesi için önce
ayakta olması gerekiyor.

Yayın öncesi `Enabled = false` yapılır.
