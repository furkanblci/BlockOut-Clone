# Ses Listesi — ne lazım, nereden bulunur

> **DURUM (2026-08-20): SESLER GELDİ VE BAĞLANDI.** 31 anahtarın tamamı dolu,
> müzik çalıyor. Aşağıdaki liste artık bir ALIŞVERİŞ listesi değil, hangi
> anahtarın ne işe yaradığının kaydı — ses değiştirmek isteyen buraya bakar.
>
> Ham dosyalar `audio_raw/`, üretim `python tools/import_audio.py`.
> Hangi kaynaktan hangi anahtarın çıktığı: `audio_raw/README.md`.
> Bulunamayan sesler (kapı, tutma, kaybetme, menü müziği) için ne yapıldığı
> da orada yazıyor.

Bu liste tahmin değil: oyundaki **gerçek olaylardan** çıkarıldı
(`BoardEvents`, `PowerUpSystem`, `ComboTracker`, arayüz düğmeleri).

Şu an her ses `SfxSynth` ile kodla üretiliyor — çalışıyor ama "oyuncak" gibi.
Gerçek dosyalar geldiğinde koda DOKUNMAK GEREKMİYOR: `AudioService` her sesi
önce `AudioSkin`den arıyor, bulamazsa sentezlenmiş hâline düşüyor.

---

## Nereye koyacaksın

```
Assets/_Project/Audio/
  SFX/     ← kısa efektler (.wav tercih)
  Music/   ← döngüler (.ogg tercih)
```

Klasörler hazır, **yükleyici de yazıldı**. Dosyayı klasöre attığın an Unity onu
doğru ayarlarla içeri alıyor ve `AudioSkin` kendini yeniliyor — elle hiçbir adım
yok. Kütüphaneyi elle tazelemek istersen: `Tools > Block Out > Ses Kütüphanesini Yenile`.

**Dosya adı = aşağıdaki anahtar.** Örneğin `SFX/absorb.wav`, `Music/gameplay.ogg`.
Bir dosya yoksa o ses sentezlenmiş hâline düşer — yani eksik dosya oyunu bozmaz,
tek tek ekleyebilirsin.

**Format:**
- Efektler: **WAV, 44.1 kHz**, tepe seviye ~-6 dB, baştaki sessizlik kırpılmış
  (gecikme hissi oradan gelir). Mono'ya çevirmeyi içe aktarıcı kendi yapıyor.
- Müzik: **OGG, stereo**, kusursuz döngü (loop point'te tık olmamalı).
- Unity ayarları (efekt: `Decompress on Load` + PCM, müzik: `Streaming` + Vorbis)
  içe aktarıcı tarafından otomatik uygulanıyor.

---

## 1. Oynanış — ZORUNLU (şu an sentetik)

| Anahtar | Ne zaman çalar | Karakter | Süre |
|---|---|---|---|
| `absorb` | Blok kapıdan içeri girer | Tatmin edici "şlop/pop", yumuşak | 0.15–0.25 sn |
| `peel` | Bloğun üst renk katmanı soyulur | `absorb`ün daha ince, kısa kardeşi | 0.10–0.18 sn |
| `ice_crack` | Buz kırılır (blok ya da kapı) | Cam/kristal çatlaması, parlak | 0.20–0.35 sn |
| `curtain_open` | Perde açılır | Kumaş kayması + küçük çıngırak | 0.30–0.50 sn |
| `win` | Bölüm tamamlanır | Yükselen neşeli 3-4 nota | 0.8–1.5 sn |
| `lose` | Süre biter | Alçalan, yumuşak hayal kırıklığı (sert değil) | 0.8–1.2 sn |

## 2. Oynanış — EKSİK (olay var, ses yok)

Bunlar şu an sessiz. Referans oyunda hepsinin sesi var.

| Anahtar | Ne zaman çalar | Karakter | Süre |
|---|---|---|---|
| `pick_up` | Bloğa dokunulup kaldırılır | Çok kısa, hafif "tık/çıt" | 0.05–0.10 sn |
| `drop` | Blok yerine oturur | Tok, kısa "tak" | 0.08–0.15 sn |
| `ice_tick` | Buz sayacı bir azalır | Küçük "çıt", `ice_crack`ten hafif | 0.08–0.12 sn |
| `curtain_tick` | Perde sayacı bir azalır | Boğuk kısa vuruş | 0.08–0.12 sn |
| `gate_advance` | Kapı sıradaki renge geçer | Kısa "şving", renk değişimi hissi | 0.15–0.25 sn |
| `gate_done` | Kapı tamamlanıp kapanır | Kilitlenme, tok kapanış | 0.20–0.30 sn |
| `block_spawn` | Üreteç yeni blok iter | Mekanik itiş | 0.15–0.25 sn |
| `refuse` | Donmuş/kımıldamayan bloğa dokunulur | Alçak, kısa "tuh" — SERT DEĞİL | 0.10–0.15 sn |
| `timer_warning` | Son 10 saniye, saniyede bir | Alçak nabız/tik | 0.10 sn |

## 3. Kombo — 5 varyasyon

Arka arkaya emilimde zincir uzuyor. Tek ses tekrarlanırsa monoton olur;
referans oyunlarda **her adımda tiz yükselir** ve oyuncu "iyi gidiyorum" der.

| Anahtar | Not |
|---|---|
| `combo_1` … `combo_5` | Aynı sesin 5 kademe tizleşen hâli. Tek dosya verip kodda pitch kaydırabilirim de — o zaman yalnız `combo_1` yeter. |

## 4. Yardımcılar — 3 ses

| Anahtar | Ne zaman | Karakter |
|---|---|---|
| `power_clock` | Çalar saat: süre donar | Zamanın yavaşladığı hissi, hafif tersine |
| `power_rocket` | Roket: blok patlatılır | Fırlatma + küçük patlama |
| `power_ufo` | UFO: bir renk süpürülür | Işın/emme, elektronik |

## 5. Arayüz

| Anahtar | Ne zaman | Karakter | Süre |
|---|---|---|---|
| `click` | Her düğme dokunuşu | Çok kısa, yumuşak tık | 0.04–0.08 sn |
| `coin` | Jeton kazanılır | Metalik parlak çıngırak | 0.20–0.40 sn |
| `star` | Kutlama parçacığı | Parlak yükselen çıngırak | 0.20–0.35 sn |
| `panel_open` | Panel açılır (PERFECT, duraklat, tanıtım) | Yumuşak "vuuf" | 0.15–0.25 sn |
| `panel_close` | Panel kapanır | `panel_open`un tersi, daha kısa | 0.10–0.20 sn |
| `purchase` | Satın alma tamamlanır | Zengin, tatmin edici onay | 0.6–1.0 sn |
| `reward_claim` | Günlük ödül alınır | Hediye açılışı, neşeli | 0.5–0.9 sn |
| `unlock` | "New Item Unlocked!" paneli açılır | Görkemli açılış | 0.8–1.2 sn |

## 6. Müzik — 2 döngü

| Anahtar | Nerede | Karakter |
|---|---|---|
| `music/menu` | Ana ekran ve menüler | Sakin, neşeli, dikkat dağıtmayan |
| `music/gameplay` | Bölüm oynanırken | Hafif ritmik, TEKRARI FARK EDİLMEYEN — oyuncu bir bölümde 3 dakika kalıyor |

> Müzik oynatıcı **yazıldı ve ayara bağlandı**: duraklat panelindeki "Musics"
> anahtarı ve Ayarlar ekranı artık gerçekten müziği açıp kapatıyor. Dosya
> koyduğun an çalmaya başlar.
>
> Müzik SENTEZLENMİYOR: kötü bir döngü, sessizlikten daha rahatsız edici.

---

## Nereden bulursun

**Önce buraya bak — CC0, hesap gerekmez, kalite yüksek:**

- **Kenney.nl** (`kenney.nl/assets` → Audio) — Bu proje için birinci adres.
  `Interface Sounds`, `UI Audio`, `Digital Audio`, `Impact Sounds` paketleri
  arayüz + oynanış efektlerinin neredeyse tamamını karşılar. Tamamı CC0,
  atıf bile gerekmez, hepsi tek zip.
- **FreePD.com** — CC0 müzik. `music/menu` ve `music/gameplay` buradan çıkar.
- **OpenGameArt.org** — filtreyi **CC0**'a al. Karışık kalite, ama ücretsiz.

**Sonra:**

- **Pixabay Audio** (`pixabay.com/sound-effects`) — telifsiz, atıf gerekmez,
  hem efekt hem müzik. Arama: "pop", "coin", "glass break", "whoosh".
- **Mixkit** (`mixkit.co/free-sound-effects`) — ücretsiz, atıfsız, düzenli
  kategoriler ("Game" bölümü tam bize göre).
- **freesound.org** — devasa arşiv ama **lisansa dikkat**: filtreyi CC0'a al,
  yoksa atıf zorunlu dosyalar karışır. Kayıt gerekir.
- **ZapSplat** — geniş ve kaliteli, ücretsiz katmanda **atıf zorunlu**, üyelik ister.
- **Sonniss GDC Bundle** — yılda bir yayınlanan dev telifsiz paket. Çoğu sinematik
  foley, bu oyuna birebir uymayabilir ama bedava ve kalitesi yüksek.

**Kendin üretmek istersen (bu oyun için gerçekten iyi seçenek):**

- **jsfxr / Bfxr / ChipTone** — tarayıcıda çalışan arcade ses üreteçleri.
  "Pickup/Coin", "Hit", "Explosion" gibi hazır şablonları var, rastgele üretip
  beğendiğini WAV indiriyorsun. Çıktı CC0. `pick_up`, `drop`, `click`,
  `ice_tick`, `combo_*` gibi kısa şeyler için Kenney'den bile hızlı sonuç verir.

---

## Lisans notu

Proje GitHub'da yayınlanacak. **CC0'ı tercih et** — atıf dosyası tutmak
gerekmez. CC-BY kullanırsan (ZapSplat, bazı freesound dosyaları) depoya bir
`CREDITS.md` koymak ZORUNLU; yoksa lisansı ihlal etmiş olursun.

Ticari mağazaya çıkmayacak olsak da GitHub yayını "dağıtım" sayılır.

---

## Sıra önerisi

Hepsini birden toplamak yorucu. Etkiye göre sıralarsan:

1. `click`, `absorb`, `drop`, `pick_up` — oyunun %90'ı bu dörtte hissedilir
2. `coin`, `win`, `ice_crack`
3. `music/gameplay`
4. Gerisi

İlk dördü koyduğun anda oyun bambaşka hissettirir. Yükleyici hazır — sen
ekledikçe otomatik devreye girerler, benden bir şey beklemene gerek yok.
