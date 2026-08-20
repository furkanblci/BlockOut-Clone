# audio_raw — ham ses dosyaları

`art_raw/` ile aynı mantık: indirilen dosyalar BURADA durur, oyuna giren
kliplerse `tools/import_audio.py` tarafından üretilir. Ham dosyayı doğrudan
`Assets/_Project/Audio` altına atmak yanlış — sebebi aşağıda.

```
python tools/import_audio.py
```
sonra Unity açıkken kendiliğinden içeri alınır (gerekirse
`Tools > Block Out > Ses Kütüphanesini Yenile`).

## Neden ham dosya doğrudan kullanılamıyor

Bu klasördeki 17 dosya ölçüldü. Üç sorun çıktı:

| sorun | örnek | sonucu |
|---|---|---|
| baştaki sessizlik | `curtain.mp3` 479 ms, `bubble pop2.mp3` 130 ms | oyunda GECİKME; "tepki vermiyor" hissi |
| uzun kuyruk / çoklu olay | `pop.mp3` 1,20 sn ama ses 0,09'da bitiyor; `ice cracking.mp3` 6,2 sn içinde 12 ayrı çatlama | her hamlede 6 saniyelik dosya çalınamaz |
| seviye farkı | tepe değerleri −0,3 dB … −19,3 dB | biri bağırıyor, diğeri duyulmuyor |

Betik her sesi kesiyor, gerekiyorsa perdesini/hızını değiştiriyor, iki uca
sönüş koyuyor (kesim tıkını önlemek için) ve hepsini −3 dBFS tepeye
getiriyor. Denge, koddaki tek tek ses seviyelerinden veriliyor
(`AudioService`).

## Eşleme

Bir kaynak birden çok anahtarı besleyebiliyor — kesim noktası ve perde
farklı olduğu için ekranda ayrı sesler gibi duyuluyorlar.

| kaynak | beslediği anahtarlar |
|---|---|
| `pop.mp3` | absorb · gate_done · combo_1…5 |
| `bubble2.mp3` | peel · curtain_tick · click |
| `bublble.mp3` | pick_up · refuse |
| `bubble pop.mp3` | block_spawn |
| `bubble pop2.mp3` | drop |
| `ice cracking.mp3` | ice_crack (3,85 sn'deki en güçlü çatlama) · ice_tick (1,69 sn'deki küçük olan) |
| `curtain.mp3` | curtain_open · panel_open · panel_close (ters) |
| `ticking.mp3` | timer_warning (tek tik) · power_clock (yavaşlatılmış tikler) |
| `sparkle.mp3` | gate_advance · star |
| `coin drop.mp3` | coin |
| `rocket.mp3` | power_rocket |
| `ufo.mp3` | power_ufo |
| `level finished.mp3` | win |
| `tithuh-level-up…mp3` | reward_claim · **lose** (ters + pes) |
| `purchasesuccess.mp3` | purchase |
| `featureunlocked.mp3` | unlock |
| `bg music.mp3` | music/gameplay (menüde de aynı parça) |

## Bulunamayan sesler ne oldu

- **Kapı sesi yoktu** → `gate_advance` için `sparkle`: kapının rengi
  değişiyor, parıltı bunu anlatıyor. `gate_done` için pes bir `pop`.
- **Tutma sesi yoktu** → `pick_up` için `bublble` (kullanıcının önerisi).
- **Kaybetme sesi yoktu** → seviye atlama sesinin TERSİ, bir tık pes.
  Yükselen bir ezginin tersi alçalan bir ezgidir.
- **Menü müziği yoktu** → tek parça hem menüde hem oyunda. `AudioService`
  eksik anahtarı oynanış parçasına düşürüyor; parça değişmediği için
  menü ↔ oyun geçişinde müzik baştan başlamıyor, akmaya devam ediyor.

## Yeni ses eklemek

`tools/import_audio.py` içindeki `SFX` tablosuna bir satır ekle:
`(anahtar, kaynak dosya, başlangıç sn, süre sn, perde çarpanı, ters mi)`.
Kesim noktasını gözle seçme — dalga biçimini ölç; betiğin başındaki
açıklamada nasıl ölçüldüğü yazıyor.
