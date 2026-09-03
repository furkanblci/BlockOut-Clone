# MANIFEST — modül modül ne var

Bu dosya SDK'nın içindekiler listesi. Yeni bir projede **neyi alacağına**
karar verirken buraya bak. Her satırda: dosya, ne işe yaradığı, oyundan ne
istediği.

**Okuma anahtarı**

| İşaret | Anlamı |
|---|---|
| 🟢 | Hazır çalışır, hiçbir şey bağlamana gerek yok |
| 🔵 | `IGameHost` / `IGameplayHost` / `IHomeScreen` bağlanınca çalışır |
| 🟡 | Görsel/metin oyununa göre düzenlenmeli (yapı hazır, içerik senin) |

---

## 1. Kabuk — `Runtime/App/` (903 satır)

Tek sahneli uygulama mimarisi. Menü ve oynanış aynı sahnede iki kök; geçiş
sahne yüklemeden yapılıyor.

| Dosya | Ne yapar | Bağ |
|---|---|---|
| `AppRoot.cs` | Uygulama kökü: kalıcı servis kökü, menü/oynanış kökleri, ses+titreşim kurulumu, tek noktadan haptik | 🔵 `IGameplayHost` |
| `AppRouter.cs` | Ekranlar arası geçişin tek adresi; müziği de o değiştirir | 🟢 |
| `AppBootstrap.cs` | İlk kareden önce: 60 fps, vSync kapalı, ekran uyumaz, analitik açık | 🟢 |
| `BootLoader.cs` | Boot sahnesi → Main geçişi | 🟢 |
| `BootSplash.cs` | Stüdyo ekranı → yükleme ekranı zinciri | 🟡 logo/ad |
| `DeviceErrorOverlay.cs` | **Cihazda konsol yerine geçen kat** — yakalanmamış her hatayı ekrana basar | 🟢 |
| `ComboTracker.cs` | Ardışık olayları sayıp çarpan üretir | 🟢 |
| `LevelTimer.cs` | Geri sayım (kendi `Update`'i yok, dışarıdan `Tick`) | 🟢 |

> **Yayın öncesi:** `DeviceErrorOverlay.Enabled` test için `true` bırakılır,
> yayında `false` yapılır. Kaynak projede bu unutulmasın diye çıkış kontrol
> listesine yazılmıştı — `docs/07-IS-AKISI.md`.

---

## 2. Meta katman — `Runtime/Meta/` (1.181 satır)

Kayıt, ilerleme, can, ödül, satın alma. Casual bir oyunun para/ilerleme
iskeleti.

| Dosya | Ne yapar | Bağ |
|---|---|---|
| `GameHost.cs` | **SDK'nın oyuna açılan tek kapısı.** `IGameHost`, `ConsumableDef`, boş yedek uygulama | — kapının kendisi |
| `MetaServices.cs` | Servislerin bestecisi: kayıt, ilerleme, can, günlük ödül. `[RuntimeInitializeOnLoadMethod]` ile ilk kareden önce hazır | 🟢 |
| `SaveData.cs` | Sürümlü kayıt DTO'su + göç (`Upgrade`) + normalleştirme | 🟡 alan ekle |
| `ProgressService.cs` | Bölüm kilidi, yıldız, jeton, tüketilebilir sayacı | 🟡 kural |
| `LivesService.cs` | 5 can, 30 dakikada bir dolum, zaman damgasıyla | 🟢 |
| `DailyRewardService.cs` | Yedi günlük **zincir** ödülü; takvim günü karşılaştırması | 🔵 öğeler host'tan |
| `PurchaseService.cs` | Satın alma akışı benzetimi (gerçek IAP takılana kadar) | 🟢 |

### `IGameHost` — doldurman gereken tek arayüz

```csharp
int LevelCount { get; }                              // sıralama, yolculuk
string LevelIdAt(int index);                         // ilerleme kayıt anahtarı
IReadOnlyList<ConsumableDef> Consumables { get; }    // mağaza, günlük ödül
```

Bağlamazsan patlamaz: boş bir yedek uygulamaya düşer (0 bölüm, 0 öğe) ve
ekranlar boş ama çalışır hâlde açılır.

---

## 3. Meta ekranlar — `Runtime/Screens/` (11.022 satır)

Hepsi **koddan** kuruluyor, prefab yok. Hepsi `MenuPage` ortak kabuğunu
paylaşıyor: degrade başlık bandı, koyu gövde, yuvarlak kart dili.

| Dosya | Ekran | Bağ |
|---|---|---|
| `MenuShell.cs` | Alt sekme çubuğu + açtığı ekranlar; kaydırmalı geçiş | 🔵 `IHomeScreen` |
| `MenuPage.cs` | Ortak sayfa kabuğu: başlık bandı, kapsül düğme, kart | 🟢 |
| `MenuSprites.cs` | **1.673 satır prosedürel sprite**: flama, rozet, kapsül, parıltı, madalya | 🟢 |
| `UiSkin.cs` | Sprite kayıt defteri + `Art` ad sabitleri | 🟢 |
| `StoreScreen.cs` | Mağaza: jeton paketleri, teklif kartları, şerit flamaları | 🔵 |
| `LeaderboardScreen.cs` | Sıralama: üç sekme (haftalık/dünya/ülke), üç AYRI liste | 🔵 |
| `JourneyScreen.cs` | Yolculuk: bölüm patikası, ilerleme | 🔵 |
| `ProfileScreen.cs` | Profil: avatar, istatistik, ad değiştirme | 🔵 |
| `CollectionScreen.cs` | Koleksiyon ızgarası | 🟡 içerik |
| `SettingsScreen.cs` | Ayarlar: ses/müzik/titreşim anahtarları, oyuncu kimliği | 🟢 |
| `DailyRewardPanel.cs` | Günlük ödül: yedi kutu, hediye çipleri | 🔵 |
| `ContinueOffer.cs` | Kaybetme akışı: "devam et?" teklifi | 🟢 |
| `OfferCarousel.cs` | Yatay sayfalı teklif taşıyıcısı | 🟢 |
| `NamePanel.cs` | Ad değiştirme penceresi | 🟢 |
| `SparkleField.cs` · `PillTint.cs` · `MenuSwipeNavigator.cs` | Küçük yardımcılar | 🟢 |
| `IHomeScreen.cs` | Ana ekranın kabuğa verdiği söz (`Refresh`, `Slide`) | — arayüz |

> **Ana ekran SDK'da yok** — o her oyunda tamamen farklı. Kabuk ona
> `IHomeScreen` üzerinden dokunuyor.

---

## 4. Arayüz araç takımı — `Runtime/UI/` (3.727 satır)

Kaynak projenin en çok kullanılan ve en olgun parçası.

| Dosya | Ne yapar |
|---|---|
| `UiKit.cs` | Kodla arayüz kurmanın sözlüğü: `CreatePanel`, `CreateTitle`, `CreateIcon`, `Place`, `SetSliceScale` |
| `UiSprites.cs` | **1.355 satır prosedürel doku**: yuvarlak panel, kapsül, halka, gölge, degrade |
| `UiTweak.cs` + `UiTweakRoot.cs` + `UiLayoutAsset.cs` | Elle yapılan düzeltmeleri **yalnız fark olarak** saklayan katman |
| `UiButtonFeel.cs` | Basma animasyonu + tek noktadan tık sesi/haptik kancası |
| `UiSafeArea.cs` | Çentik/ada güvenli alanı |
| `UiTextFit.cs` · `UiSliceFit.cs` · `UiCornerFit.cs` | Taşan yazı, 9-slice ve köşe uyumu |
| `UiTitleEmboss.cs` · `UiVerticalTint.cs` | Kabartmalı başlık, dikey degrade |
| `UiRingLayout.cs` · `UiScrollOvershoot.cs` | Halka dizilim, kaydırma esnemesi |

---

## 5. Servisler — `Runtime/Services/` (1.605 satır)

| Dosya | Ne yapar | Bağ |
|---|---|---|
| `AudioService.cs` | Ses masası: palet, statik arayüz sesleri, müzik, 60 ms çakışma sınırı | 🔵 `PaletteExtender` |
| `AudioSkin.cs` | Ses kayıt defteri + `Sfx` ad sabitleri | 🟢 |
| `SfxPlayer.cs` · `SfxSynth.cs` | Havuzlu çalar + **asset gerektirmeyen ses sentezi** | 🟢 |
| `Haptics.cs` | Titreşim (hafif/orta/güçlü), ayardan kapanabilir | 🟢 |
| `Ads.cs` | Reklam soyutlaması + boş sağlayıcı | 🟢 |
| `FakeAdScreen.cs` | Ödüllü reklam benzetimi (gerçek ağ takılana kadar) | 🟢 |
| `Analytics.cs` · `LocalAnalytics.cs` | Sağlayıcı arayüzü + cihazda biriktiren uygulama | 🟢 |
| `PerfProbe.cs` | Kare hızı **ve çöp üretimi** sondası | 🟢 |
| `SettingsBinder.cs` | Kayıt ↔ ses/titreşim çevirmeni | 🟢 |

---

## 6. Kayıt ve altyapı

| Dosya | Ne yapar |
|---|---|
| `Runtime/Save/SaveService.cs` | Sürümlü kayıt: **yedekten kurtarma**, bozuk kayıt, gelecekten gelen kayıt |
| `Runtime/Save/SaveStore.cs` | Dosya deposu + testler için bellek deposu |
| `Runtime/Flow/SceneRouter.cs` | Sahne var mı kontrolü, güvenli yükleme |
| `Runtime/FX/Juice.cs` | Zıplama, sarsılma, yanıp sönme — arayüz canlandırma sözlüğü |
| `Runtime/FX/CameraShake.cs` | Kamera sarsıntısı |

---

## 7. Gizli geliştirici konsolu — `Runtime/DevTools/` (1.405 satır)

**Oyuncuya görünmez, yayın yapısında yok.** Sol üst köşeye 2 saniyede 5
dokunuş veya editörde `F8`.

| Dosya | Ne yapar |
|---|---|
| `DevConsole.cs` | Konsolun kendisi, sayfa yönetimi, açılış hareketi |
| `DevPage.cs` | Sayfa temel sınıfı — oyun kendi sayfasını ekler |
| `DevUi.cs` | IMGUI çizim sözlüğü (düğme, kaydırıcı, liste) |
| `DevLog.cs` · `DevLogPage.cs` | Cihazda log görüntüleyici |
| `DevSystemPage.cs` | Cihaz bilgisi, bellek, kare hızı |

> **Kural:** Test aracı asla görünür bir düğme olmaz. Kaynak projede oynanış
> HUD'ında duran bir "bölüme atla" düğmesi, testçilerin oyunu gerçekten
> oynamamasına yol açtı. Ayrıntı: `docs/02-TUZAKLAR.md` §14.

---

## 8. Düzenleyici araçları — `Editor/` (4.112 satır)

### Arayüz Tasarım Penceresi (`Editor/UiDesign/`, 2.104 satır) — **en değerli araç**

`Ctrl+Shift+U`. Her paneli oyun oturumu olmadan kurar, gerçek boyutta gösterir,
elle sürükletir ve **yalnız farkı** `Resources/UiLayout.asset` içine yazar.

| Dosya | Ne yapar |
|---|---|
| `UiDesignWindow.*.cs` | Pencere: Paneller / Canlı / Kayıtlı / Kılavuz sekmeleri |
| `UiPanelCatalog.cs` | Panellerin kayıt defteri — oyun kendi panellerini `GameEntries` ile ekler |
| `UiPanelStage.cs` | Paneli sahneye dokunmadan kurup dokuya çizen sahne |
| `UiPanelFixture.cs` | Düzenleyici kipi için sahte oyuncu verisi |
| `UiTweakDiff.cs` | Kurulan ağaç ile şimdiki ağacın farkı |

### Kurulum ve derleme (`Editor/ProjectSetup/`)

| Dosya | Ne yapar |
|---|---|
| `ProjectBootstrap.cs` | **Kendi kendini kuran proje**: her domain reload'da eksikleri tamamlar; oyun adımlarını `Add()` ile ekler |
| `AndroidBuildTool.cs` | Portre, IL2CPP+ARM64, minSdk, ASTC, versionCode artırma, APK derleme |
| `AndroidManifestPatcher.cs` | Gradle manifestine `VIBRATE` izni |
| `AppIconTool.cs` | İkonu projeye alıp Android yuvalarına bağlar |
| `FontSetupTool.cs` | TTF'den TMP font asset'i üretir, varsayılan yapar |
| `UiSpriteImporter.cs` | `Art/UI` altına düşen her görseli doğru ayarlarla alır |
| `UiOverflowAudit.cs` | **Taşan yazı avcısı** — ağaçtaki bütün etiketleri ölçer |
| `EditorCapture.cs` | Yakalamadan önce shader'ları senkron derlet (magenta tuzağı) |
| `FxCaptureTool.cs` | Parçacık prefab'ını oynatma kipi olmadan kareye alır |
| `MobileQualityTool.cs` · `GameViewUtility.cs` | URP mobil ayarları, 1080×1920 game view |
| `EditorSkin.cs` | IMGUI için ferah/yuvarlak görünüm (düzenleyici pencerelerinde) |

---

## 9. Python araçları — `tools/`

| Dosya | Ne yapar |
|---|---|
| `cutout.py` | **Saydamlık kesme** — üreteçlerin sahte saydamlığını (dama tahtası) temizler, iç delikleri de açar |
| `import_art.py` | Ham görselleri projeye doğru adlarla aktarır |
| `import_audio.py` | Ham sesleri oyunun beklediği kliplere çevirir |
| `check_art.py` | Görsel doğrulama (alfa, kenar, boyut) |
| `make_icon.py` · `make_icon_layers.py` | Uygulama ikonu ve uyarlanabilir ikon katmanları |
| `make_buttons.py` · `slice_logo.py` | Düğme varyantları, logo dilimleme |

---

## 10. Şablonlar — `templates/`

SDK'ya girmeyen ama **desen olarak değerli** oyun kodu:

| Dosya | Neden burada |
|---|---|
| `gameplay/TutorialOverlay.cs.txt` | Öğretici katman — seçim mantığı oynanışa bağlı |
| `gameplay/NewItemPanel.cs.txt` | "Yeni mekanik" tanıtımı — **spot ışığı deseni**, kart değil |
| `gameplay/AudioService.Bind.cs.txt` | Oynanış olaylarını sese bağlama + çoklu olay tuzağı |
| `gameplay/UiPanelCatalog.GameplayEntries.txt` | Oynanış panellerinin katalog girişleri |
| `gitignore.txt` · `gitattributes.txt` | Unity için doğru ignore + LFS ayarları |
| `github-workflows/validate-levels.yml` | **İçeriği de test et**: her PR'da bölümleri doğrulayan CI |

---

## Bağımlılık zinciri

```
GameKit.Editor  ──►  GameKit.Runtime
                     ├─► Unity.InputSystem
                     ├─► Unity.TextMeshPro
                     ├─► PrimeTween.Runtime      (Juice, UiButtonFeel)
                     └─► Newtonsoft.Json.dll     (SaveData)
```

PrimeTween'i kullanmak istemiyorsan yalnız iki dosya etkilenir
(`FX/Juice.cs`, `UI/UiButtonFeel.cs`) — ikisi de tween çağrılarını tek yerde
topluyor, başka bir kütüphaneye çevirmek yarım saatlik iş.
