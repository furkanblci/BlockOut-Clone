# Devam Notu — yeni oturuma nasıl devam edilir

Yeni bir sohbet açtığında Claude'a şunu yaz:

> **`docs/DEVAM.md` dosyasını oku ve kaldığımız yerden devam et.**

Bu dosya her oturum sonunda güncellenir. Aşağısı 2026-08-10 itibarıyla geçerli.

---

## Proje nedir

Unity 6.3 URP Mobile ile **Block Out! – Color Sort Puzzle** (Grand Games)
klonu. Portfolyo için; GitHub'da yayınlanacak, mağazaya konmayacak. Reklam ve
mağaza **test modunda** çalışacak (gerçek para yok) ama akışların tamamı
gerçek olacak.

**Hedef: kusursuz birebir benzerlik ve çok iyi bir his.** "Çalışıyor" yetmez.

---

## Referans kaynağı — ÖNEMLİ

Referans videolar `C:/Users/CPN12/Downloads/` altında:

- `Block Out Color Sort Puzzle Levels 1-20 Walkthrough.mp4` ← **arayüzün tamamı burada**
- `Block Out! - Color Sort Puzzle Level 21-22-...-30 Solution Walkthrough.mp4`
- `Block Out! - Color Sort Puzzle Level 31-...-40 Solution Walkthrough.mp4`
- `Block Out! Level 41-...-50 Solution Walkthrough.mp4`

> `anamenu.mp4` ve `BlattodaMenuAnimasyon.mp4` **başka bir projeye ait**, Block
> Out değil. Karıştırma.

**Yöntem hatası olarak öğrenildi:** Bu videolar uzun süre yalnız *bölüm
çıkarmak* için kullanıldı; arayüz ve his için hiç taranmadı. Kullanıcının
"birebir değil" eleştirisinin kaynağı buydu. Bir şeyin referansta nasıl
göründüğünü merak ettiğinde **kullanıcıya sorma, videodan çıkar.**

ffmpeg sistemde yok; taşınabilir olanı şöyle bulunur:

```bash
python -m pip install imageio-ffmpeg          # bir kez
FF=$(python -c "import imageio_ffmpeg;print(imageio_ffmpeg.get_ffmpeg_exe())")

# Her 6 saniyede bir kare (genel tarama)
"$FF" -loglevel error -i "<video>" -vf "fps=1/6,scale=296:-1" out/w%03d.png

# Belirli bir anı tam çözünürlükte
"$FF" -loglevel error -ss 00:02:30 -i "<video>" -frames:v 1 out/kare.png
```

---

## Şu an ne bitmiş durumda

**Oynanış çekirdeği** — 50 bölüm, hepsi çözücüyle doğrulanmış. Polyomino
bloklar, renk katmanları, buz (blok + kapı), perdeler, kapı renk kuyrukları,
yönlü bloklar, blok üreteçleri, çok bölgeli tahtalar.

**Menü** — ana ekran, Yolculuk, Mağaza, Liderlik, Koleksiyon, Ayarlar.
Kullanıcı ana ekranı onayladı.

**Oyun içi HUD** — referans videodan ölçülerek yeniden kuruldu (iki satır:
jeton/bölüm, sonra yeniden başlat/süre/duraklat). Yardımcılar yeşil kare +
kırmızı adet rozeti.

**Sistemler** — combo, günlük ödül, sahte IAP, ödüllü reklam, yerel analitik,
gizli geliştirici menüsü (F8 / sol üst köşeye 5 dokunuş), editör bölüm
tarayıcı (`Tools > Block Out > Bölüm Tarayıcı`, `Ctrl+Shift+L`).

**Görsel boru hattı** — `art_raw/` içine ham PNG at, `python tools/import_art.py`
işler, `python tools/check_art.py` kesim hasarını tarar (şu an 39/39 temiz).

---

## SIRADAKİ İŞLER (öncelik sırasıyla)

### 1. Referanstan çıkan, henüz yapılmayanlar
- **PERFECT paneli** — referansta yıldız YOK: "PERFECT!" + "Level N" + jeton
  yığını görseli + yeşil "Continue" + sağ üstte kırmızı X. Bizimki krem kart +
  üç yıldız, yani tamamen farklı bir tasarım. Referans kare: `1-20` videosu,
  02:30 ve 03:18.
- **"Ice Door! New Item Unlocked!" paneli** — yeni mekanik tanıtımı. Bizde yok.
  Referans kare: `1-20` videosu, ~01:06.
- **Duraklat paneli** — referansta Sounds/Musics/Haptics anahtarları +
  yeşil Resume + kırmızı Quit. Bizimki henüz karşılaştırılmadı.
  Referans kare: `1-20` videosu, 00:00.
- **21-50 videoları hiç taranmadı** — orada görülmemiş mekanikler olabilir.

### 2. Ses (kullanıcı bunu bekliyor)
Şu an her şey kodla sentezleniyor (`SfxSynth`), müzik yok. İki yol:
ücretsiz lisanslı kütüphane (Kenney / freesound CC0) → `Assets/_Project/Audio/`,
ya da mevcut sentezi katmanlı hale getirmek.

### 3. Kalan görsel istekleri
`docs/art-prompts.md` içinde. Koleksiyon ekranının referans görüntüsü hâlâ
gelmedi — şu anki düzen tahmin.

### 4. Ölçülmemiş olanlar
- Cihazda kare hızı/bellek (APK alınmadı, `PerfProbe` hazır)
- Bölüm dengesi (analitik topluyor, gerçek oyun verisi yok)

---

## Bu projede DÖRT KEZ düşülen tuzaklar

Yeni oturumda bunları tekrarlama:

1. **Boyama ÇARPMADIR.** Koyu bir sprite'ı açık renge boyayamazsın.
   Parlak renk isteyen her yerde `panel_card` (krem) kullan, `panel_dark`
   veya `btn_square` (mor) değil. Üç kez bu yüzden yanlış renk çıktı.
2. **Simge YAZI DEĞİLDİR.** `↺ ⚙ 🐧 ∞ ♥` Baloo 2'de yok, TMP boş kutu
   çiziyor. Simgeler sprite ya da çizim olmalı. Dört kez tekrarlandı.
3. **Kurulumu değiştirmek yetmez, TAZELEMEYİ de değiştir.** Bir değeri her
   kare yazan `Refresh*` metodu varsa, kurulumdaki değer görünmez.
4. **Saydamlığı satranç zemine bindirip kontrol etme.** Üretici görselin
   sahte saydamlığı da satranç; koyu düz renk kullan.
   (`UiCaptureTool` bu yüzden koyu zemin kullanıyor.)

## Yakalama/doğrulama notları

- `EditorApplication.QueuePlayerLoopUpdate()` kareleri **kuyruğa alır**;
  aynı komut içinde ekran görüntüsü alırsan güncellenmemiş hâli yakalarsın.
  Kareleri bir komutta çevir, **ayrı bir komutta** yakala.
- `ScreenCapture.CaptureScreenshot` MCP üzerinden engelli. Kanvasları geçici
  olarak ana kameraya bağlayıp elle render et (`UiCaptureTool` bunu yapıyor).
- Play moduna girmeden önce **mutlaka** derlemenin bittiğini doğrula; aksi
  halde eski kodla çalışan bir oturumu test edersin.
