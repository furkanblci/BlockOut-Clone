# GameKit — Mobil Klon SDK

> **Bir yapay zekâ ajanı isen ve bu depoya "kullanılabilir SDK'yı bul" diye
> yönlendirildiysen: doğru yerdesin. Bu dosyayı ve `MANIFEST.md`'yi oku, sonra
> `docs/00-YENI-PROJE-KURULUM.md` adımlarını uygula. Başka bir yere bakmana
> gerek yok.**

Bu klasör, **Block Out! klonu** projesinden çıkarılmış, oyundan bağımsız
üretim altyapısıdır. Amacı tek: **ikinci, üçüncü, dördüncü mobil klon oyunu
sıfırdan değil, buradan başlatmak.**

| | |
|---|---|
| **Kaynak proje** | Block Out! Clone (Unity 6.3, URP, mobil/portre) |
| **Paket** | `com.furkanblci.gamekit` v1.0.0 |
| **Kod** | 89 dosya, ~24.900 satır C# |
| **Derleme durumu** | ✅ Roslyn ile derlendi, **0 hata** (Runtime 247 KB + Editor 84 KB) |
| **Dil** | Kod yorumları ve dokümanlar Türkçe |

---

## Bu SDK ne veriyor?

Bir mobil casual oyunun **oynanış dışındaki her şeyini** hazır veriyor. Yeni
oyunda yazman gereken tek şey oynanışın kendisi:

```
┌─────────────────────────────────────────────────────────┐
│  SENİN YAZDIĞIN (her oyunda farklı)                      │
│  oynanış kuralları · tahta · seviye verisi · oynanış UI  │
├─────────────────────────────────────────────────────────┤
│  SDK'NIN VERDİĞİ (her oyunda aynı)                       │
│                                                          │
│  Kabuk      AppRoot · AppRouter · BootSplash · hata katı │
│  Meta       kayıt · ilerleme · can · günlük ödül · satın │
│  Ekranlar   mağaza · sıralama · profil · yolculuk ·      │
│             koleksiyon · ayarlar · isim · teklif · reklam│
│  Arayüz     UiKit · UiSprites · UiTweak · 9-slice · font │
│  Servis     ses · titreşim · reklam · analitik · perf    │
│  Araçlar    canlı UI düzenleyici · Android derleme ·     │
│             ikon · sprite içe aktarma · gizli dev konsol │
└─────────────────────────────────────────────────────────┘
```

### Öne çıkan üç parça

**1. Kodla kurulan arayüz + canlı düzenleyici.**
Ekranların hiçbiri prefab değil; hepsi koddan doğuyor. `Ctrl+Shift+U` ile
açılan **Arayüz Tasarımı** penceresi her paneli oyun oturumu olmadan kurup
gösteriyor, elle sürükleyip bırakabiliyorsun ve **yalnızca farkları** bir
asset'e kaydediyor. Prefab çakışması yok, kod da bozulmuyor.

**2. Prosedürel sprite üreticisi.**
`UiSprites` + `MenuSprites` (3.000 satır) yuvarlak panel, kapsül, halka,
flama, rozet, parıltı gibi arayüz parçalarını **çalışma anında çiziyor**.
Yeni oyun için sıfır görsel asset ile çalışan bir arayüzün olur; gerçek
görseller sonra üstüne gelir.

**3. Miras yorumları.**
Kodun içinde ~450 adet `DERS (...)` bloğu var. Her biri bu projede
**gerçekten yaşanmış** bir hatanın kaydı: neden böyle yazıldı, alternatifi
neden yanlıştı. Bunlar dolgu değil — SDK'nın en pahalı parçası.
En kritik olanlar `docs/02-TUZAKLAR.md` içinde de toplandı.

---

## Klasör haritası

```
SDK/
├── README.md                  ← buradasın
├── MANIFEST.md                  modül modül ne var, neye bağlı
├── package/                     Unity paketi — KOPYALANACAK KISIM
│   ├── package.json
│   ├── Runtime/                 GameKit.Runtime.asmdef
│   │   ├── App/                 tek sahneli uygulama kabuğu
│   │   ├── Meta/                kayıt, ilerleme, can, ödül, satın alma
│   │   ├── Screens/             meta ekranlar
│   │   ├── Services/            ses, titreşim, reklam, analitik
│   │   ├── UI/                  arayüz araç takımı
│   │   ├── FX/ Flow/ Save/ DevTools/
│   └── Editor/                  GameKit.Editor.asmdef
│       ├── UiDesign/            canlı arayüz düzenleyici
│       └── ProjectSetup/        derleme, ikon, font, sprite araçları
├── docs/                        SÜREÇ MİRASI — kod kadar önemli
├── tools/                       Python: görsel kesme, ses/görsel içe aktarma
└── templates/                   oyuna özel örnekler, .gitignore, CI
```

---

## Hızlı başlangıç

1. `SDK/package/` klasörünü yeni projede `Packages/com.furkanblci.gamekit/`
   altına kopyala.
2. `Packages/manifest.json`'a bağımlılıkları ekle (bkz. aşağı).
3. Oyununu SDK'ya tanıt — **tek dosya**:

```csharp
using System.Collections.Generic;
using GameKit.Meta;
using UnityEngine;

public sealed class MyGameHost : IGameHost
{
    public int LevelCount => 50;
    public string LevelIdAt(int i) => $"level_{i + 1:000}";

    public IReadOnlyList<ConsumableDef> Consumables { get; } = new[]
    {
        new ConsumableDef("hammer", "Çekiç",  "icon_hammer"),
        new ConsumableDef("shuffle", "Karıştır", "icon_shuffle"),
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bind() => GameHost.Bind(new MyGameHost());
}
```

4. Oynanış oturumuna `IGameplayHost` uygula, `AppRoot`'un alanına sürükle.
5. Ana ekranına `IHomeScreen` uygula, `Home.Bind(this)` çağır.

Tamamı ve kontrol listesi: **`docs/00-YENI-PROJE-KURULUM.md`**.

### Bağımlılıklar

`Packages/manifest.json`:

```json
"com.unity.ugui": "2.0.0",
"com.unity.inputsystem": "1.18.0",
"com.unity.nuget.newtonsoft-json": "3.2.1",
"com.unity.render-pipelines.universal": "17.3.0",
"com.kyrylokuzyk.primetween": "1.4.11"
```

PrimeTween npm kayıt defterinden gelir — `scopedRegistries` girişi de gerekli
(örneği `docs/00-YENI-PROJE-KURULUM.md` içinde).

---

## Dokümanlar

| Dosya | İçerik |
|---|---|
| `docs/00-YENI-PROJE-KURULUM.md` | Sıfırdan kurulum, adım adım kontrol listesi |
| `docs/01-MIMARI.md` | Neden tek sahne, neden kodla UI, katman sınırları |
| `docs/02-TUZAKLAR.md` | **Bu projede kanla öğrenilen 21 tuzak** — en değerli dosya |
| `docs/03-UI-DILI.md` | Referans oyunu ölçerek arayüz dili çıkarma yöntemi |
| `docs/04-GORSEL-PIPELINE.md` | Görsel üretimi, saydamlık kesme, 9-slice, ikon |
| `docs/05-SES-PIPELINE.md` | Ses anahtarları, sentez yedeği, içe aktarma |
| `docs/06-REFERANS-CIKARIMI.md` | Videodan bölüm/ekran çıkarma (ffmpeg) |
| `docs/07-IS-AKISI.md` | Dal/PR düzeni, APK bulgu turları, doğrulama kültürü |
| `docs/08-AGENT-PROMPTU.md` | **Yeni projede ajana verilecek hazır prompt** |

---

## Sınırlar — dürüst olalım

- **Bu bir anlık görüntü.** Kaynak proje geliştikçe buradaki kopya geride
  kalır. Ana projede yapılan iyileştirmeler buraya elle taşınmalı.
- **Paket bu depoda derlenmiyor.** `SDK/` klasörü `Assets/` dışında olduğu
  için Unity onu görmez (bilerek — aynı sınıflar iki kez tanımlanmasın).
  Derlendiği Roslyn ile ayrıca doğrulandı; ilk gerçek test yeni projede olur.
- **Oynanış yok.** Tahta, blok, seviye çözücü, oynanış ekranı SDK'da yok ve
  olmamalı — onlar her oyunda farklı.
- **Görsel asset yok.** Sprite'lar prosedürel; PNG'ler oyunun kendi
  `Art/UI/` klasöründen gelir.
