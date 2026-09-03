# Yeni proje kurulumu — adım adım

Hedef: boş bir Unity projesinden, **çalışan menü kabuğu + meta ekranlar** olan
bir iskelete 30-45 dakikada varmak. Oynanışı ondan sonra yazarsın.

---

## 0. Ön koşullar

- Unity **6000.0** veya üstü (kaynak proje 6000.3.10f1)
- **URP** şablonu, **Mobile / Portrait**
- Git deposu kurulmuş olmalı

---

## 1. Paketi kopyala

```
SDK/package/  →  <YeniProje>/Packages/com.furkanblci.gamekit/
```

> **Neden `Packages/` altına, `Assets/` altına değil?** Gömülü paket (embedded
> package) olarak durunca kit ile oyun kodu arasındaki sınır **fiziksel** olur:
> oyun koduna yanlışlıkla kite ait bir dosya eklemek zorlaşır ve bir sonraki
> oyunda "hangisi kitindi?" sorusu hiç sorulmaz.

`.meta` dosyaları bilerek çıkarıldı; Unity yenilerini üretecek.

## 2. Bağımlılıkları ekle

`Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.unity.ugui": "2.0.0",
    "com.unity.inputsystem": "1.18.0",
    "com.unity.nuget.newtonsoft-json": "3.2.1",
    "com.unity.render-pipelines.universal": "17.3.0",
    "com.kyrylokuzyk.primetween": "1.4.11"
  },
  "scopedRegistries": [
    {
      "name": "npm",
      "url": "https://registry.npmjs.org/",
      "scopes": [ "com.kyrylokuzyk" ]
    }
  ]
}
```

Unity'yi aç, derlemenin temiz geçtiğini gör. **Bu noktada durup konsola bak** —
buradan sonraki her adım bunun üstüne kuruluyor.

## 3. Kendi assembly'ni kur

`Assets/_Project/Scripts/Runtime/<Oyun>.Runtime.asmdef`:

```json
{
  "name": "Oyun.Runtime",
  "rootNamespace": "Oyun.Runtime",
  "references": [ "GameKit.Runtime", "Unity.InputSystem", "Unity.TextMeshPro", "PrimeTween.Runtime" ]
}
```

Düzenleyici tarafı için ayrıca `Oyun.Editor` (referansları: `Oyun.Runtime`,
`GameKit.Runtime`, `GameKit.Editor`, `includePlatforms: ["Editor"]`).

---

## 4. Üç arayüzü bağla — SDK'nın oyunu tanıması

### 4a. `IGameHost` — bölümler ve tüketilebilirler

```csharp
using System.Collections.Generic;
using GameKit.Meta;
using UnityEngine;

public sealed class OyunHost : IGameHost
{
    public int LevelCount => 50;

    public string LevelIdAt(int index) => $"level_{index + 1:000}";

    public IReadOnlyList<ConsumableDef> Consumables { get; } = new[]
    {
        //              kimlik      görünen ad   Art'taki sprite adı
        new ConsumableDef("hammer",  "Çekiç",    Art.Rocket),
        new ConsumableDef("shuffle", "Karıştır", Art.Clock),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bind() => GameHost.Bind(new OyunHost());
}
```

### 4b. `IGameplayHost` — oynanış oturumu

```csharp
public sealed class GameSession : MonoBehaviour, GameKit.App.IGameplayHost
{
    public void PlayLevel(int levelIndex) { /* bölümü kur */ }
    public void StopLevel()               { /* tahtayı sök */ }
}
```

Sahnedeki `AppRoot` bileşeninin **Gameplay Host** alanına sürükle.

### 4c. `IHomeScreen` — ana ekran

Ana ekran SDK'da yok (her oyunda farklı). Kabuğun ona dokunabilmesi için:

```csharp
public sealed class HomeScreen : MonoBehaviour, GameKit.Screens.IHomeScreen
{
    void Awake()      => GameKit.Screens.Home.Bind(this);
    void OnDestroy()  => GameKit.Screens.Home.Unbind(this);

    public void Refresh() { /* jeton, can, ilerleme */ }
    public void Slide(float x, float y, float seconds) { /* kaydır */ }
}
```

---

## 5. Sahne yapısı

Tek sahne (`Main`) + isteğe bağlı küçük bir `Boot`:

```
Main
├── AppRoot                    ← AppRoot bileşeni
│   ├── Persistent             (kod kuruyor: ses, titreşim, hata katı)
│   ├── MenuRoot               ← AppRoot.menuRoot
│   │   ├── HomeScreen
│   │   └── MenuCanvas         ← MenuShell burada
│   └── GameRoot               ← AppRoot.gameRoot (kapalı başlar)
│       ├── GameSession        ← AppRoot.gameplayHost
│       └── Camera
└── EventSystem
```

Neden tek sahne olduğu: `01-MIMARI.md` §1.

---

## 6. Kayıt verisini genişlet

`SaveData` casual bir oyunun ortak alanlarını taşıyor (jeton, can, ayarlar,
bölüm kayıtları, tüketilebilir sayaçları). Oyununa özel alan eklerken:

1. Alanı ekle
2. `CurrentVersion`'ı artır
3. `Upgrade`'e eski sürümden yeni sürüme geçişi yaz

> **Bunu atlama.** Kayıt formatını sürümlemeden değiştirirsen, test cihazındaki
> eski kayıt sessizce bozulur ve sebebini kayıt kodunda aramazsın.

---

## 7. Sesleri bağla

SDK ortak sesleri (arayüz, kazan/kaybet, jeton) hazır getiriyor ve **dosya
yoksa sentezliyor** — yani ses toplamadan önce de oyun sessiz kalmıyor.

Oyununa özel sesler:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void SesleriEkle() =>
    GameKit.Services.AudioService.PaletteExtender = audio =>
    {
        audio.Register("blok_patlat", () => SfxSynth.Pop(660f, 0.16f));
        audio.Register("power_hammer", () => SfxSynth.Noise(0.28f, 1400f));
    };
```

`power_<kimlik>` adı özel: `AudioService.PowerUp("hammer")` onu arar,
bulamazsa ortak sese düşer.

---

## 8. Geliştirici konsolunu bağla

```csharp
GameKit.App.AppRoot.DevMenuInstaller = () => MyDevMenu.Ensure();
```

`AppRoot.Awake` içinden çağrılır — yani konsol **uygulama açılışında** kurulur,
ilk bölüme girişte değil. (Sebebi: `02-TUZAKLAR.md` §14.)

## 9. Kurulum adımlarını bootstrap'a ekle

```csharp
[InitializeOnLoadMethod]
static void Kurulum()
{
    GameKit.Editor.Setup.ProjectBootstrap.Add("bölüm kataloğu", EnsureLevelCatalog);
    GameKit.Editor.Setup.ProjectBootstrap.Add("oynanış sahnesi", EnsureGameplayScene);
}
```

Her domain reload'da eksikler tamamlanır — yeni katılan kişi repoyu çeker,
Unity'yi açar, her şey hazırdır. README'de elle kurulum adımı bırakma.

## 10. Panellerini tasarım penceresine ekle

```csharp
[InitializeOnLoadMethod]
static void Panelleri() =>
    GameKit.Editor.UiDesign.UiPanelCatalog.GameEntries = list =>
        list.Add(new GameKit.Editor.UiDesign.UiPanelCatalog.Entry
        {
            Id = "gameplay.hud", Group = "Oynanış", Label = "Üst şerit",
            Key = "GameplayScreen/Hud",
            Build = (parent, v) => GameplayScreen.CreateHudPreview(parent).transform
        });
```

Örnekler: `templates/gameplay/UiPanelCatalog.GameplayEntries.txt`.

## 11. Android ayarları

```csharp
GameKit.Editor.Setup.AndroidBuildTool.PackageName = "com.sirket.oyun";
GameKit.Editor.Setup.AndroidBuildTool.EnsureBuildScenes = () => { /* sahne listesi */ };
```

> **Paket adını ilk derlemeden ÖNCE doğru yaz.** Play Console'da bir kez
> yayımlandıktan sonra asla değiştirilemez.

---

## Kontrol listesi

- [ ] Paket `Packages/` altında, konsol temiz
- [ ] Bağımlılıklar + `scopedRegistries` eklendi
- [ ] Kendi asmdef'lerin `GameKit.Runtime`'a bakıyor
- [ ] `GameHost.Bind` çağrılıyor
- [ ] `AppRoot` sahnede, üç alanı da dolu
- [ ] `Home.Bind` ana ekranın `Awake`'inde
- [ ] `MenuShell` menü kanvasında — sekmeler açılıyor
- [ ] Mağaza / sıralama / profil / ayarlar açılıyor (boş olabilir, açılmalı)
- [ ] `SaveData` sürümlendi
- [ ] Gizli konsol açılıyor (5 dokunuş / F8)
- [ ] Android paket adı yazıldı
- [ ] `templates/gitignore.txt` ve `gitattributes.txt` kopyalandı
- [ ] Yayın öncesi: `DeviceErrorOverlay.Enabled = false`

---

## İlk çalıştırmada beklenenler

Bunlar **hata değil**, host bağlanmadığı için normaldir:

| Görünen | Sebep |
|---|---|
| Sıralama listesi boş | `LevelCount` 0 — host bağlanmamış |
| Mağaza yardımcı paketi hiçbir şey vermiyor | `Consumables` boş |
| Günlük ödülde 3./6./7. gün yardımcısız | aynı sebep; `DailyRewardService.SetWeek()` ile tazele |
| Menü kaydırınca arka plan kaymıyor | `Home.Bind` çağrılmamış |

Hiçbiri istisna atmaz — SDK bağlanmamış hâlde de ayakta kalacak şekilde
yazıldı (`02-TUZAKLAR.md`'nin ruhu: sessiz çökme, gürültülü çökmeden kötüdür,
ama **hiç çökmemek** en iyisidir).
