# GameKit

Mobil casual oyunlar için yeniden kullanılabilir çekirdek. **Oyuna özgü hiçbir şey içermez** — burada "blok", "kapı", "bölüm" gibi kavramlar geçmez.

## Kural

> Bağımlılık **oyundan kite** doğru akar, tersi asla.

GameKit oyunu tanımaz. Örneğin `Haptics` yalnızca "titret" bilir; *hangi* olayda titreneceğine oyun karar verir. Bu kural bozulduğu an paket bir sonraki projede kullanılamaz hâle gelir.

Bir şeyi buraya taşımadan önce sor: **"Bunu Match-3 yapsam da kullanır mıyım?"** Cevap hayırsa oyunda kalmalı.

## İçindekiler

| Parça | Ne yapar |
|---|---|
| `Save/SaveStore` | `ISaveStore` + **atomik** dosya yazımı (geçici → yedek → taşı) + bellek deposu. |
| `Save/SaveService<T>` | Sürümlü kayıt yönetimi: bozuk dosya kurtarma, göç, gelecekten gelen kaydı koruma. |
| `Meta/LivesService` | Can + zamanla dolum. Oyun kapalıyken geçen süre, saat kurcalama koruması. |
| `Services/SfxPlayer` | Havuzlu ses çalar; çakışma sınırı, perde savrulması, kısma. |
| `Services/SfxSynth` | Asset'siz yer tutucu ses üretimi (pop, gürültü, arpej, coin, klik). |
| `Services/Haptics` | Titreşim; şiddet kademeleri ve eşik. Platform farkı `#if` ile ayrılmış. |
| `Services/Analytics` | `IAnalyticsProvider` + tipli olay girişi (level_start/complete/fail, para akışı). |
| `Services/Ads` | `IAdProvider` + ödüllü reklam sözleşmesi + araya giren reklamda sıklık sınırı. |
| `Services/PerfProbe` | Cihazda fps + **kare başına GC ayırması**. Profiler bağlamadan ölçüm. |
| `Flow/SceneRouter` | Sahne geçişi + aralarda taşınan tek parça niyet (okununca tüketilir). |
| `UI/UiKit` | Prefab'sız uGUI kurucuları, CanvasScaler ve safe-area doğru ayarlı. |
| `Editor/MobileQualityTool` | URP mobil kontrol listesi (HDR, gölge, opaque/depth, MSAA, SRP Batcher) — her ayarın yanında gerekçesi. |
| `Editor/GameViewUtility` | Game view'ı telefon oranına ayarlar. |

## Üçüncü parti SDK'lar

`Analytics` ve `Ads` şu an sahte sağlayıcılarla çalışıyor (konsola basar / ödülü hep verir). Gerçek ağ geldiğinde **tek yapılacak** `IAnalyticsProvider` / `IAdProvider` uygulaması yazıp `SetProvider` ile takmak. Oyun kodunda hiçbir çağrı değişmez.

Bu ayrım kritik: reklam ağı, oyunun ömrü boyunca **değişme ihtimali en yüksek** bağımlılıktır. Oyun kodu doğrudan ağın API'sini çağırırsa o çağrılar projeye yayılır ve geçiş haftalar alır.

**Ödüllü reklam sözleşmesi:** ödül, geri çağrı `Completed` derse verilir. "Gösterdim, hemen vereyim" demek en sık yapılan hata — oyuncu reklamı kapatınca da ödül alır ve model çöker.

## Sonraki projede kullanmak

Şu an klasör olarak duruyor; kopyalayıp yeni projeye atmak yeterli.

**Daha iyisi (ikinci klon başlarken yapılacak):** bu klasörü kendi git deposuna çıkar, sonra her projenin `Packages/manifest.json` dosyasına ekle:

```json
"com.furkanblci.gamekit": "https://github.com/furkanblci/gamekit.git"
```

Böylece bir projede yapılan iyileştirme diğerlerine de gelir. `package.json` bunun için hazır bekliyor.

> Not: Klasör `Packages/` altına doğrudan konursa Unity onu gömülü paket olarak tanır — ama bunu ancak **yeniden başlatınca** fark eder.
