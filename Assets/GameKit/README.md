# GameKit

Mobil casual oyunlar için yeniden kullanılabilir çekirdek. **Oyuna özgü hiçbir şey içermez** — burada "blok", "kapı", "bölüm" gibi kavramlar geçmez.

## Kural

> Bağımlılık **oyundan kite** doğru akar, tersi asla.

GameKit oyunu tanımaz. Örneğin `Haptics` yalnızca "titret" bilir; *hangi* olayda titreneceğine oyun karar verir. Bu kural bozulduğu an paket bir sonraki projede kullanılamaz hâle gelir.

Bir şeyi buraya taşımadan önce sor: **"Bunu Match-3 yapsam da kullanır mıyım?"** Cevap hayırsa oyunda kalmalı.

## İçindekiler

| Parça | Ne yapar |
|---|---|
| `Services/Haptics` | Titreşim; şiddet kademeleri ve eşik. Platform farkı `#if` ile ayrılmış. |
| `Services/PerfProbe` | Cihazda fps + **kare başına GC ayırması**. Profiler bağlamadan ölçüm. |
| `UI/UiKit` | Prefab'sız uGUI kurucuları, CanvasScaler ve safe-area doğru ayarlı. |
| `Editor/MobileQualityTool` | URP mobil kontrol listesi (HDR, gölge, opaque/depth, MSAA, SRP Batcher) — her ayarın yanında gerekçesi. |
| `Editor/GameViewUtility` | Game view'ı telefon oranına ayarlar. |

## Sonraki projede kullanmak

Şu an klasör olarak duruyor; kopyalayıp yeni projeye atmak yeterli.

**Daha iyisi (ikinci klon başlarken yapılacak):** bu klasörü kendi git deposuna çıkar, sonra her projenin `Packages/manifest.json` dosyasına ekle:

```json
"com.furkanblci.gamekit": "https://github.com/furkanblci/gamekit.git"
```

Böylece bir projede yapılan iyileştirme diğerlerine de gelir. `package.json` bunun için hazır bekliyor.

> Not: Klasör `Packages/` altına doğrudan konursa Unity onu gömülü paket olarak tanır — ama bunu ancak **yeniden başlatınca** fark eder.
