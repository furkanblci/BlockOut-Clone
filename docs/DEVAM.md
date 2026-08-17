# Devam Notu — yeni oturuma nasıl devam edilir

Yeni bir sohbet açtığında Claude'a şunu yaz:

> **`docs/DEVAM.md` dosyasını oku ve kaldığımız yerden devam et.**

Bu dosya her oturum sonunda güncellenir. Aşağısı 2026-08-10 itibarıyla geçerli.

---

## 2026-08-17 OTURUM SONU — BUILD ALINABİLİR DURUMDA

Ayrıntılı kayıt: **`docs/APK-BULGULARI.md`** (21 kullanıcı bulgusu + 2 yeni).

**Bu oturumda kapanan maddeler:** 4 (günlük ödül), 7 (ana ekran), 9 (Yolculuk),
13 (Ayarlar), 16 (Liderlik), 21 (Profil), 22 (ödül şeridi), 23 (alt sekme
çubuğu). Konsol **0 hata**, play modundan çıkıldı, asset'ler kaydedildi.

**Geriye kalan üç şey:**
1. **Uygulama ikonu (1. madde) — SENDEN BİR DOSYA BEKLİYOR.** `art_raw/icon_app.png`
   (kare, ≥512px). Gelmeden Android ikon yuvaları bağlanamaz. **Build alınabilir
   ama ikon Unity'nin varsayılanı olur.**
2. **Koleksiyon görseli (5. madde)** — açık kitap + albüm sekme ikonu
   (`docs/art-prompts.md` §11). Yerine şimdilik sandık duruyor.
3. **Mağaza (12. madde) — tente şerit kontrastı ve harf kalınlığı.** Tek kalan
   kod işi; bu oturumda sıra gelmedi.

**Karar bekleyen ekonomi sorunu:** `LivesService.Grant` üst sınırı aşmıyor.
Canı doluyken can ödülü alan oyuncu kalbi KAYBEDİYOR (5 → 5 ölçüldü). Günlük
ödülün 5. ve 7. günü, ödüllü reklam ve mağazadan can alımı — üçü de aynı
yoldan geçiyor. Çözümü taşan canı saklayacak bir alan; ekonomiye dokunduğu
için tek başıma değiştirmedim.

**Bu oturumda öğrenilen iki büyük ders:**
- **Yakalama yöntemi hatayı GİZLİYORDU.** Aşağıdaki "Yakalama/doğrulama
  notları" bölümündeki `Capture()` uyarısı artık GEÇERSİZ — tam tersi doğru.
- **`UiSprites.Circle` düz bir `Image` olarak kutuya GERİLİR.** Kutu kare
  olmadığı anda daire elipse döner. Projede DÖRT yerde vardı (kapatma
  çarpısı, kalem rozeti, günlük ödül/Continue çarpıları, mağaza noktaları).
  Hepsi `UiKit.CreateIcon`'a alındı — o en-boy oranını korur.
- **Ölçerken KATMANI da ayır.** Alt çubuğun üst kenarında 30 piksel içinde
  dört ayrı renk var; tek yatay örnekleme üçünü birbirine karıştırıp yanlış
  "gövde rengi" verdi. Dikey tarama ayırdı.

---

## Proje nedir

Unity 6.3 URP Mobile ile **Block Out! – Color Sort Puzzle** (Grand Games)
klonu. Portfolyo için; GitHub'da yayınlanacak, mağazaya konmayacak. Reklam ve
mağaza **test modunda** çalışacak (gerçek para yok) ama akışların tamamı
gerçek olacak.

**Hedef: kusursuz birebir benzerlik ve çok iyi bir his.** "Çalışıyor" yetmez.

---

## Referans kaynağı — ÖNEMLİ

> **DÜZELTME (2026-08-16):** Aşağıdaki `Downloads` yolu EKSİK. Arayüz için
> asıl klasör:
> `C:/Users/CPN12/OneDrive/Masaüstü/Block Out! Videos/`
> İçinde `Block Out! menus,powerups,vs.mp4` (menüler + yardımcılar) ve
> **`Game over .mp4`** (kaybetme paneli — henüz hiç kıyaslanmadı) var.
> İkisi de 384x832; oynanış videoları 592x1280. **Her iki referansın da
> en-boy oranı ~2.165**, bizim yakalama 1080x1920 = 1.78 — yani referansın
> ölçekleme modelini (yükseklik mi genişlik mi kilitli) videodan ayırt etmek
> MÜMKÜN DEĞİL, iki video da aynı cihaz oranında. Dikey konumlar yükseklik
> oranı olarak doğrudan aktarılabilir (en-boydan bağımsız), yatayda ise
> mevcut kural (`referans_piksel / referans_genişlik`) korunuyor.

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

**Mağaza (2026-08-10 yeniden kuruldu)** — referanstan ölçülerek birebir:
çizgili tente + festonlu kenar, "Özel Teklifler / Paketler / Jetonlar" bölümleri
(her birinin kendi zemin rengi içerikle KAYIYOR), 5 paket kartı + köşe kurdelesi,
3×2 jeton ızgarası, geri yükle. Renkler ve yükseklikler
`Block Out! menus,powerups,vs.mp4` 08-14. saniyelerden piksel örneklenerek alındı.
Görseller geldi, kesildi ve `UiSkin`'e bağlandı: `coin_pile_1..6`,
`pack_1..5`, `icon_noads`, `icon_infinite` (52 görsel, `check_art.py` temiz).

**Mağaza BİTTİ** — canlı oyunda sekmeden açılıp doğrulandı:
- Jeton yığınları HER KUTUDA AYNI BOYUTTA. Önce kademeli büyütülmüştü;
  referans karesinde altı yığının da genişliği 226-229 piksel çıktı, artan tek
  şey jeton yoğunluğu (%21 → %35).
- "Özel Teklifler" artık GERÇEK bir taşıyıcı (`OfferCarousel`): üç sayfa,
  parmakla kayar, en yakın sayfaya oturur, noktalar sayfayı gösterir. Dikey
  jestler dıştaki listeye elle aktarılıyor — yoksa kart üstünde aşağı kaydırmak
  sayfayı dondururdu. **İkinci ve üçüncü teklifin içeriği UYDURMA**; referansta
  üç nokta görünüyor ama yalnız birincisi kaydedilmiş.
- İndirim rozeti flama biçiminde (`MenuSprites.Pennant`).
- Kaydırma alanı sekme çubuğunun ALTINA uzatıldı; kabuk ekranları 0.105'te
  bitirdiği için çubuğun üstünde ana ekranın manzarası sızıyordu.

**Yolculuk (2026-08-10 yeniden kuruldu)** — referanstan ölçülerek
(`menus,powerups,vs.mp4` 24-42. saniyeler):
- Zemin ölçüldü: DÜZ koyu lacivert `#171C4E`. Eskiden büyük yumuşak daireler +
  %10 yıldızlar vardı, ekran sisli duruyordu. Yıldızlar %5.5'e kısıldı,
  daireler kaldırıldı.
- Kilometre taşı artık TEK kapsül (solda Seviye/N, sağda ödül), eskiden iki
  ayrı parçaydı. Renk `#4C3BE2` — eskisi neredeyse siyahtı.
- Boru ekran genişliğinin %3.0'ı, bölme çizgili, sol tarafında ışık şeridi.
  Boru kapsüllerin ÜSTÜNDEN, bölge çemberlerinin ALTINDAN geçer — bu yüzden
  kurulum sırası: kapsüller → boru → bölgeler.
- Bölge çemberi artık gerçek DAİRE + parlak halka (`MenuSprites.Ring`);
  eskiden elipsti. Aralık etiketi üst kenara, yeşil düğme içine biniyor.
- "Üst"/"Alt" borunun uçlarında, sağ kenarda değil.
- **İlerleme çubuğu ve "sonraki ödüle N bölüm" satırı KALDIRILDI** — referansta
  yok. Yerine referanstaki mevcut seviye işareti (pembe rozet) kondu.
- Ödüller ONU DA referanstan okundu: 10, 15, 25, 30, 45, 50, 60, 75, 80, 90
  (24./31./40./41./42. saniyeler). "d" dakika, "s" saat.
- **BEŞİNCİ BÖLGE bulundu:** "Buz Kurtarma" (sv 71-100). İlk kuruluşta
  atlanmıştı çünkü videoda yalnız KİLİTLİ hâliyle görünüyor.
- **Boru bir İLERLEME GÖSTERGESİ.** Oyuncunun seviyesine kadar ışıklı, ötesi
  koyu (`#121137`). İlk kuruluşta baştan sona parlaktı — elimdeki karelerin
  hepsi oyuncunun ulaştığı bölgelerdeydi, yukarı kaydırılan kareye bakınca
  ortaya çıktı. Bizim eklediğimiz ayrı ilerleme çubuğu tam da bu yüzden
  fazlalıkmış.
- **Ulaşılmamış satır SOLDURULMUYOR.** 75/80/90 satırları alınmışlarla birebir
  aynı; tek fark yeşil tikin olmaması. Eski kod onları soluklaştırıyordu.
- Kilitli bölge: görselin üstüne açık gri-mavi daire perdesi + asma kilit +
  "Seviye N" etiketi; yeşil düğme gizli. Doygunluk düşürmek materyal işi
  olduğu için perde yöntemi kullanıldı.
- Eksik görsel: `region_5` (buz temalı çember) ve altın `icon_lock`
  — bkz. `docs/art-prompts.md` §8.

`ShopSprites` → **`MenuSprites`** olarak yeniden adlandırıldı: kapsül, ∞ ve
halka artık iki ekranda birden kullanılıyor, isim yanıltıcı olmuştu.

**Kalan dört menü ekranı (2026-08-10) — referanstan yeniden kuruldu**
Ortak kabuk `MenuPage.cs`'e alındı (degrade başlık bandı, koyu gövde, kırmızı
çarpı, kapsül düğme). Beş ekran ayrı ayrı kurulduğu için beşi de birbirinden
biraz farklıydı ve hiçbiri referansa uymuyordu.
- **Settings** (kare 54-56): tek açık mor kart, dört satır, her satırda
  [Off|On] İKİLİ anahtar (etkin olan yeşil). Eskiden tek yazı vardı.
  Zil simgesi yok — `UiSprites`'ta çizilmedi, o satır simgesiz.
- **Profile** (kare 57-60): kimlik kartı (avatar + kalem rozeti + ad + Level),
  "General Stats", 2×4 koyu kutu. Simgeler kutunun ÜST KENARINA biniyor.
  Sekiz sayaçtan yalnız "First Try Wins" gerçek; kalanı "-" (referans da
  kazanılmamışı "-" gösteriyor). Kalem bir yazı DEĞİL, eğik kapsül.
- **Collection** (kare 43-45): referansta ekranın TAMAMI bir görsel +
  "Unlock Collection at Level 95!". Eski tahmini ızgara kaldırıldı.
- **Leaderboard** (kare 16-22): üç sekme tek yuvada, geri sayım rozeti,
  podyum 2-1-3 (ortadaki yüksek), sıralı satırlar, en altta oyuncunun
  KENDİ satırı yeşil ve sabit.

**Arayüz dili İNGİLİZCE'ye geçti** (2026-08-10, kullanıcı kararı). Tasarım
referansla birebir; yalnız metinler İngilizce, fiyatlar USD. Kod yorumları
Türkçe kalıyor.

**Oyun içi HUD** — referans videodan ölçülerek yeniden kuruldu (iki satır:
jeton/bölüm, sonra yeniden başlat/süre/duraklat). Yardımcılar yeşil kare +
kırmızı adet rozeti.

**Sistemler** — combo, günlük ödül, sahte IAP, ödüllü reklam, yerel analitik,
gizli geliştirici menüsü (F8 / sol üst köşeye 5 dokunuş), editör bölüm
tarayıcı (`Tools > Block Out > Bölüm Tarayıcı`, `Ctrl+Shift+L`).

**Görsel boru hattı** — `art_raw/` içine ham PNG at, `python tools/import_art.py`
işler, `python tools/check_art.py` kesim hasarını tarar (şu an 39/39 temiz).

---

## Level editörü YENİDEN KURULDU — sekmeli kabuk (2026-08-17)

Referans: kullanıcının kendi **Ant Level Editor**'ü
(`OneDrive/Masaüstü/Block Out! Videos/level editör örnek.mp4`, 32 sn).
Amaç yalnız bu oyun değil: bu kabuk **sonraki oyunlarda da kullanılacak şablon**.

**Tarz:** tek `EditorWindow` + IMGUI, üç katmanlı sabit kabuk (üst şerit ·
sekme çubuğu · durum çubuğu) ve sekmeye göre değişen tek gövde. Oyuna özel olan
YALNIZCA Tahta sekmesinin içi; kalan yedi sekme herhangi bir ızgara-tabanlı
bölüm setiyle çalışır.

Dosyalar (`Assets/_Project/Scripts/Editor/LevelEditor/`):

| dosya | işi |
|---|---|
| `LevelEditorWindow.cs` | kabuk: durum, sekme yönlendirme, geri alma, dosya, durum çubuğu |
| `.TopBar.cs` | Yeni/Aç/Önceki/Sonraki/Undo/Redo/Kaydet/Play/Önbellek + palet noktaları |
| `.Board.cs` | üç kolon (denetçi · tuval · tarayıcı) + cetveller + alt kapı şeridi |
| `.Panels.cs` | sol denetçi: araçlar, fırça, seçili nesne |
| `.Tabs.cs` | Pano · Galeri · Doğrula · Çözüm · Araçlar · Referans · Kılavuz |
| `.Input.cs` | fare/klavye, seçim, düzenleme |
| `LevelLibrary.cs` | setin tek kaynağı: sıra (katalogdan), durum, minyatür önbelleği |
| `LevelThumbnail.cs` | `LevelData` → `Texture2D` (galeri kartları) |

**Yeni kazanımlar**
- **Galeri:** 50 bölüm minyatür olarak, durum renkli çerçeveyle. Sağ tık menüsü
  (aç / play / doğrula / çoğalt / sil).
- **Pano:** sağlık çubuğu, sayaç kartları, "dikkat gerekenler" listesi, zorluk ve
  tahta boyutu dağılımı.
- **Doğrula:** bütün seti tek düğmeyle doğrula (iptal edilebilir ilerleme çubuğu),
  sıralanabilir tablo.
- **Çözüm:** hamle listesi + tuval yan yana; satıra tıkla, tahtada o ana kadarki
  hamleler.
- **Kapı renk KUYRUĞU artık düzenlenebilir.** Şemada (`gates[].colors`) baştan
  beri vardı ama editör yalnız 0. rengi yazabiliyordu — bu yüzden 50 bölümde hiç
  kullanılmadı. Alt şeritteki kapı kutusundan renk ekle/çıkar/değiştir.
- **Palet noktaları:** tahtada kullanılan ama **kapısı olmayan** renk kırmızı
  çerçeveyle işaretlenir (= çözülemez bölüm), çözücüyü beklemeden.
- **Oynanış sırası** tarayıcıdan ▲▼ ile değiştirilebiliyor (`LevelCatalog` yazılır).
- Cetveller ızgaranın **dört** kenarında; Kırp (`TrimBoard`) boş kenarları atıp
  bloğu/kapıyı/perdeyi birlikte kaydırıyor.

**Referansın teknolojisi UI Toolkit, IMGUI DEĞİL** (karelerden büyütülerek
doğrulandı: mavi halkalı yuvarlak slider topuzu, düz tek renkli yuvarlak köşeli
düğmeler). Bizimki IMGUI kalıyor — ızgara tuvali için doğru araç o. Referansın
ferah görünümü `LevelEditorSkin.cs` ile elle üretiliyor: BEYAZ yuvarlak köşeli
doku + `GUI.backgroundColor` çarpımı (boyama çarpma olduğu için kaynak beyaz
olmalı), harf aralıklı başlıklar (IMGUI'de tracking yok, harfler tek tek
konumlandırılıyor), kart blokları ve saç teli ayraçlar.

**ÜRETEÇLER ARTIK EDİTÖRDE** — 19 üreteç 12 bölümde vardı ve editör onları hiç
düzenleyemiyordu (Perde aracı yalnız `curtain` üretiyordu). Eklenenler: 7. araç
"Üreteç" (kenara tıkla), tuvalde makine kutusu + sıradaki bloğun rengi, alt
şeritte sıra kutuları (renk/şekil/sıra/silme menüsü), denetçide kenar + şerit +
satır satır sıra düzenleme, minyatürde makine noktası. Şema:
`{type:"generator", side:"N|E|S|W", x, y, queue:[blok…]}` — x/y yalnız KENAR
BOYUNCA konumu taşır (N/S'de x, E/W'de y anlamlı, diğeri 0).

**BULUNAN GERÇEK HATA: `DrawColorGrid` 8 renk varsayıyordu.** Satırı
`i % 4 == 3` olunca kapatıyordu; palet 10 renge çıkınca (mor + camgöbeği) son
satır HİÇ kapanmadı ve Blok aracı seçiliyken editör her karede
"Invalid GUILayout state ... Verify that all layout Begin/End calls match"
bastı. **DERS: açılan layout grubunu eleman sayısının bölünebilirliğine
güvenerek kapatma, BAYRAKLA kapat.** Aynı desen `DrawSizePalette`'te de vardı
(18 ön ayar 6'ya bölündüğü için şimdilik tutuyordu) — o da düzeltildi.

**RENK PALETİ REFERANSTAN ÖLÇÜLDÜ** (`level editör örnek.mp4` 5. sn, 736×480 ham
kare, piksel örnekleme). Referans **iki tonlu neredeyse siyah** kullanıyor:

| rol | renk |
|---|---|
| ana zemin | `#100F12` (16,15,18) |
| yükseltilmiş yüzey (üst şerit, şeritler, metin alanı) | `#1B1A1D` (27,26,29) |
| nötr düğme | `#201F22` (32,31,34) |
| seçili düğme (mavi) | `#2E3D5C` — lacivert, parlak mavi DEĞİL |
| altın düğme | `#75601F` — pirinç |
| yeşil birincil | `#356A30` — orman yeşili |

**DERS (şıklık düşük yüzey kontrastından gelir):** İki zemin tonu arasındaki
fark yalnız 11 birim; vurgular da kısılmış. Böylece ekrandaki EN DOYGUN şey
daima içerik (renkli bloklar) oluyor. Unity'nin varsayılan `#383838` grisi tam
tersi: zemin bağırır, içerik onunla yarışır. Ayrıca renkler hafif MOR'a çalıyor
(mavi > kırmızı > yeşil, 2-3 birim) — "soğuk siyah" hissini o veriyor.
**DERS (dolgu rengi ≠ yazı rengi):** Aynı kısılmış renk yazıda okunmaz; skinde
`Accent/Positive/Warning/Danger` (dolgu) ve `*Bright` (yazı/çizgi) ayrı tanımlı.
**Pencerenin TAMAMI `OnGUI` başında boyanıyor** — boyanmayan her boşlukta
Unity'nin grisi sızıyordu. `EditorStyles.toolbar` da terk edildi
(`LevelEditorSkin.BarScope`), o doku griyi geri getiriyor.

**MEKANİK ALGILAMA + ZORLUK EĞRİSİ** (`LevelMechanics.cs`): 10 mekanik veriden
hesaplanıyor (katman, buz blok, buz kapı, kapı kuyruğu, perde, üreteç, yönlü,
polyomino, duvar, şekilli tahta). Galeri kartlarında rozet, galeride mekaniğe
göre süzgeç, Pano'da **tanıtım sırası** (her mekanik ilk hangi bölümde) ve
**zorluk eğrisi** (hamle sayısı çubuk grafiği, renk zorluktan geliyor).

Araç ilk çalıştırmada şunları çıkardı (2026-08-17):
- **İç duvar 0 bölümde kullanılıyor.** Şema ve araç destekliyor, içerik yok.
- **Kapı renk kuyruğu yalnız 1 bölümde** (29) — editör düzenleyemediği için.
- **Yönlü blok yalnız 1 bölümde** (50).
- Tanıtım sırası: polyomino 2 · buz kapı 6 · şekilli tahta 8 · buz blok 10 ·
  katman 15 · perde 20 · kuyruk 29 · üreteç 35 · yönlü 50.
- Doğrulama: **0 bozuk, 2 temiz, 48 uyarılı.** Uyarı dağılımı: 42× "açılışta
  tek hamle", 35× "süre dar olabilir", 6× "çok dar (zorunlu hamle)",
  **3× BUZ BÜTÇESİ** (o kapıların buzu hiç kırılamıyor — kapı fiilen dekor),
  2× "süre çok bol", 1× "çok serbest". En uzun çözüm level_038 (42 hamle).
  → Sıradaki denge işi bu listedir. "Açılışta tek hamle" 42/50 çıktığı için
  ölçütün kendisi de fazla katı olabilir; önce ona bakılmalı.

**TUVAL ARTIK OYUNUN TUĞLASINI ÇİZİYOR.** Editör düz renk dikdörtgen boyuyordu;
oyunda bloklar **LEGO benzeri kabartmalı tuğlalar** (`BrickMeshBuilder`: mesh +
kendi ışıklandırmalı materyal + temas gölgesi). Tuval artık aynı ölçüleri
`BlockVisualConfig.asset`'ten okuyup çiziyor: kenar payı (`brickInset`, yalnız
DIŞ kenarlara — bitişik hücreler birleşir), pah bandı (`brickChamfer`), üst yüz
ve yan yüz tonları (`toneFaceTop/toneBodySide`), **hücre başına `studsPerCell`²
saplama** (`studRadius`), temas gölgesi (`shadowOffset/shadowOpacity`). Zemin de
oyunun `floorColorA/B`'si + `frameColor` çerçevesi oldu (eskiden lavanta satranç
deseniydi). Minyatürler de aynı renkleri kullanıyor.
**DERS:** Editör oyunun görsel dilini paylaşmazsa tasarımcı kafasında sürekli
çeviri yapar ve o çeviri her seferinde biraz yanlış olur.

**3D ÖNİZLEME SEKMESİ EKLENDİ** (`LevelEditorWindow.Preview.cs`). Referans
editördeki eksik tek sekme buydu. Bölümü **oyunun kendi kurulum koduyla**
(`BoardBuilder.Build`) `PreviewRenderUtility`'nin izole sahnesinde kuruyor;
kamera GameSession'ın kadrajını birebir tekrarlıyor (80° eğim, 27° FOV, köşeler
görünene kadar ikili arama). Sürükle = serbest yörünge, tekerlek = uzaklık,
"Oyun kamerası" düğmesi kadraja döner. `_previewSignature` (bölümün JSON'u)
değişmedikçe yeniden kurulmuyor. `OnDisable`/`OnDestroy`'da `Cleanup()` —
yoksa RenderTexture ve gizli sahne sızar.

**BULUNAN İKİNCİ GERÇEK HATA: kurulum kodu edit modunda `Destroy` çağırıyordu.**
`BoardBuilder` (duvar + ızgara çizgisi), `GateView` ve `CurtainView` (3 yer)
`CreatePrimitive`'in çarpıştırıcısını `Object.Destroy` ile siliyordu; edit
modunda bu GEÇERSİZ ve Unity her çağrı için hata basıyor (önizleme ilk
denemede 41 hata verdi). `ViewKit.StripCollider(GameObject)` eklendi, beşi de
ona bağlandı. **DERS: kurulum kodu bir gün editörden de çağrılabilir; yıkım
çağrıları `Application.isPlaying` ayrımı yapmalı.** (`GeneratorView` bunu zaten
yapıyordu — desen oradan alındı.)

Önizleme sayıyla doğrulandı: level_001/024/046/050 dördü de render edildi
(16-88 mesh, karenin %18-24'ü dolu, 133-281 ayrık renk), **0 hata**.
Dokuz sekmenin hepsi tek tek çizdirildi, konsol temiz.

**TUVAL ARTIK OYUNUN KENDİ RENDER'INI GÖSTERİYOR** (`LevelBoardPreview.cs`).
Tahta sekmesindeki "Gerçek görsel" anahtarı açıkken tahta `BoardBuilder.Build`
ile kurulup **tepeden ortografik** bir kamerayla render ediliyor; 2B tuğla
çizimi yalnız yedek yol olarak kaldı (anahtar kapalıyken).

**Neden 90° ortografik, oyundaki 80° perspektif değil:** perspektifte bloklar
kenara doğru KAYAR, hücre↔piksel bağı bozulur ve tıklama yanlış hücreye düşer.
Ortografikte 1 hücre = 1 dünya birimi olduğu için eşleme doğrusal kalıyor.
Oyuncunun gerçek açısı 3D Önizleme sekmesinde duruyor — ikisi birlikte hem doğru
düzenleme hem doğru kadraj veriyor. Hizalama sayıyla kanıtlandı: 4×4 sınav
tahtasında sol-üst hücre KIRMIZI, sağ-alt MAVI, diğer üç nokta zemin — beşi de
beklenen hücreye düştü (yarım hücre kayma YOK).

Perde içeriği gerçek render'da görünmez (oyunda da görünmemeli) ama düzenlenmesi
gerekiyor: `DrawHiddenContents` onları yalnız editörde soluk biçimde üstüne
bindiriyor. Izgara da ince bir bindirme olarak çiziliyor.

**BULUNAN ÜÇÜNCÜ (ve en önemli) HATA: TAHTANIN ZEMİNİ HİÇ ÇİZİLMİYORDU.**
`BoardBuilder.BuildFloorMesh` iki üçgeni TERS sarıyordu; zemin ön yüzü AŞAĞI
bakıyordu ve kamera tepeden baktığı için arka yüz eleme (backface culling) onu
tamamen eliyordu. Oyunda "zemin" sanılan şey arka plan + ızgara çizgileri +
çerçeveydi; **`floorColorA/B`, `floorLineDarken`, `floorDotSize` ayarlarının
hiçbir görsel etkisi yoktu.**
- Nasıl bulundu: 3D önizleme oyunun kendi koduyla render ettiği için, arka planı
  MAGENTA yapıp yalnız zemini çizdirdim — her piksel magenta çıktı.
- Kıyas noktası aynı dosyadaki gölge mesh'i: köşelerini ALTTAN üste sıralıyor ve
  doğru çalışıyor. Zemin köşelerini ÜSTTEN alta sıralıyor (`CornerToWorld`'de y
  arttıkça z AZALIR), dolayısıyla üçgen sırası da tersine dönmeliydi.
- **DERS (ters sarım sessizce kaybeder):** Yanlış sarılmış yüzey hata vermez,
  sadece görünmez olur. Aylarca fark edilmemesinin sebebi bu.
- Doğrulama: 5 bölümde (001/024/038/046/050) oynanabilir hücrelerin merkezleri
  tarandı — **boş kalan hücre 0**. Zemin hem tepeden hem oyun açısından görünüyor.

Ayrıca `BlockView`'daki kullanılmayan `SideInset` sabiti kaldırıldı (derleme
uyarısı basıyordu). Konsol şu an **0 hata, 0 uyarı**.

**İKİ IMGUI TUZAĞI — tekrarlama**
1. **Sekmeyi/aracı çizim sırasının ORTASINDA değiştirme.** OnGUI bir karede
   `Layout` → gerçek olay → `Repaint` diye üç kez koşar ve GUILayout yerleşimi
   `Layout` geçişinde ölçer. Tıklama anında gövdeyi değiştirirsen o geçiş farklı
   sayıda kontrol çizer → "Getting control N's position in a group with only M
   controls". Çözüm: `_pendingTab` / `_pendingTool` + `ApplyPendingMode()`, mod
   yalnız `Layout` geçişinin başında değişiyor.
2. **OnGUI'nin içinde modal diyalog açma.** `ConfirmDiscard()` bir dialog açar,
   olay döngüsünü keser, açık layout grupları kapanmaz. Bölüm açma/silme/çoğaltma
   `RequestOpen` / `DeleteLevel` ile `EditorApplication.delayCall`'a erteleniyor.

**Doğrulama (bu oturumda yapıldı):** 8 sekmenin hepsi gerçekten çizdirildi,
konsol **0 hata**. Galeri'nin çizildiği sayıyla kanıtlandı (önbellek sıfırlandı →
paint sonrası 50/50 minyatür). Kütüphane 50 bölümü okuyor, 50'si de katalog
sırasında, okunamayan yok.

**ELLE DENENMEDİ:** Araçlar sekmesindeki `Kırp`, `± Satır/Kolon`, tarayıcıdaki
▲▼ sıra değiştirme ve `Sil`. Üçü de yıkıcı; ilk kullanımda geri alma (Ctrl+Z)
elinin altında olsun.

**Sonraki adım (referansta var, bizde yok):** `3D Preview` sekmesi — bölümü
gerçek oyun görünümüyle Play'e girmeden göstermek. Referans editörde bu, izometrik
bir önizleme; bizde `BlockView` prefablarını edit modda bir `PreviewRenderUtility`
sahnesine kurmak gerekir.

---

## Hareket kütüphanesi: PrimeTween (2026-08-10'da geçildi)

Artık `com.kyrylokuzyk.primetween` 1.4.11 kurulu. **npm scoped registry ile** —
GitHub deposunda paket YOK, orada yalnız dokümantasyon var, git URL'i
"package manifest bulunamadı" ile düşer. `Packages/manifest.json` içinde
`scopedRegistries` bloğu bunun için duruyor.

`GameKit.FX.Juice` cephe olarak kaldı; içi PrimeTween'e taşındı. Çağrı yerleri
değişmedi çünkü `Run(PT.Tween)` geçirgen aşırı yüklemesi eklendi. Yeni yazarken
`Juice` üzerinden git, doğrudan `PrimeTween.Tween` çağırma — bir gün kütüphane
değişirse tek dosya değişsin.

Tuzak: `PrimeTweenConfig` ayarları **BeforeSceneLoad'da yapılamaz**,
PrimeTween'in yöneticisi de orada kuruluyor ve sıra tanımsız. `AfterSceneLoad`
kullan.

---

## Dil: arayuz tamamen INGILIZCE (2026-08-15)

Butun kullanici metinleri Ingilizceye cevrildi (~110 dize): HUD, menuler,
magaza, ayarlar, liderlik, profil, koleksiyon, gunluk odul, sahte reklam,
ogretici ve gizli gelistirici menusu. Referans oyun Ingilizce oldugu icin
panellerdeki metinler (PERFECT!, Continue, Pause, Resume, Quit, Hard Level)
zaten referansla ayni.

DOKUNULMAYANLAR (bilerek):
- **Kod yorumlari Turkce kaldi.** Onlar bu projenin ogretici belgesi; cevirmek
  degerini yok ederdi ve istenen "arayuz metni" degil.
- **Debug/konsol loglari Turkce kaldi** (AppRouter, BootLoader, GameSession,
  MetaServices). Oyuncuya gorunmuyorlar, gelistirici teshisi.

DIKKAT: Turkce dizeleri ararken **ozel karakter aramasi yetmez**. "Bildirimler",
"Haptik", "Kapat", "Oyna" gibi kelimelerde c/g/i/o/s/u yok; ilk tarama bunlari
kacirdi. Kelime listesiyle ikinci tur sart.

Magaza fiyatlari TL'den USD'ye cevrildi (2000 jeton = 4.99 dolar gibi standart
mobil basamaklar). Rakamlar UYDURMA, referanstan alinmadi.

---

## SIRADAKİ İŞLER (öncelik sırasıyla)

### 0. PERFECT paneli — BİTTİ, gözle doğrulandı
Panel referanstan ölçülerek yeniden yazıldı (`GameplayScreen`): mor kart, üst
kenara binen altın "PERFECT!" (mor konturlu), sağ üstte kırmızı **daire** çarpı,
"Level N", jeton tümseği, koyu sayı rozeti, yeşil "Continue". Yıldızlar ve
"Ana Ekran" düğmesi kaldırıldı — çarpı onun yerine geçti.

Referans kare: `1-20` videosu **02:52** (Level 8 PERFECT). Eskiden burada yazan
02:30/03:18 damgaları YANLIŞTI, ikisi de oynanışa denk geliyor.

Referanstan kalan farklar **kapatıldı**: ışık huzmesi eklendi
(`UiSprites.Burst`), jetonlar tümsek yerine dikey **istiflerden** kuruluyor,
reklam düğmesi baskılı gölgesi olmayan prosedürel panele geçti (panel_card'ın
baskılı gölgesi turuncuyla çarpılınca kırmızıya kayıyordu).

Gerçek `coin_pile` görseli hâlâ yok; `BuildCoinPile` icon_coin'lerden istif
kuruyor. Görsel gelirse orası tek bir Image'a iner, düzen değişmez.

### 0b. Doğrulama araçları (bu oturumda eklendi)
- Gizli geliştirici menüsüne (F8) **"Bölümü kazandır"** eklendi. Paneli elle
  açmıyor, gerçek bitiş yolunu (`OnBoardCleared`) çağırıyor — ödül, PERFECT
  ölçüsü ve can iadesi de çalışsın diye.
- `UiCaptureTool.CaptureOf<T>()` eklendi: yalnız o bileşenin kanvasını yakalar.
  Ayrıca dosyaya `.png` uzantısı artık yazılıyor (eskiden uzantısız kaydediyordu
  ve hiçbir görüntüleyici açmıyordu).

### 0c. `MetaServices` hazır olmama tuzağı — DİKKAT
`LastReward = 0` görürsen sebebi tekrar oynama değil: `NoteCleared` her zaman
en az 20 döndürür. 0 demek `MetaServices.Ready == false` demek, yani ödül, can
iadesi, bölüm açılışı ve kayıt **hiç yazılmamış**. Artık bu durumda
`GameSession` konsola hata basıyor.

Sebebi: derleme bitmeden play moduna girmek. `RuntimeInitializeOnLoadMethod`
çalışmıyor, `MetaServices.Initialize` hiç koşmuyor ve HUD her karede
`RefreshPowerUps` içinde NullReference atıyor. **Çözüm:** play moduna girmeden
önce `isCompiling` VE `isUpdating` ikisinin de false olduğunu doğrula; şüphe
varsa play modundan çık, bekle, temiz gir.
- ~~**"Ice Blocks! New Item Unlocked!" paneli**~~ — **BİTTİ**, gözle doğrulandı.
  `NewItemPanel` (`Assets/_Project/Scripts/Runtime/UI/NewItemPanel.cs`).
  Yedi mekaniği tanıyor (katman, kapı renk kuyruğu, buz blok, buz kapı, perde,
  yönlü blok, üreteç); bölüm başına EN FAZLA BİR tanıtım gösteriyor ve
  görüldüğünü PlayerPrefs'e yazıyor (`BlockOut.ItemSeen.*`).
  Test için o anahtarları silmek yeterli.

  Aşağısı eski notun kendisi, ölçüler hâlâ geçerli:
  **Doğru referans karesi: `1-20` videosu 03:31** (eskiden burada yazan ~01:06
  yanlıştı). Panelin adı da "Ice Door" değil **"Ice Blocks!"**.

  ÖNEMLİ: Bu bir KART DEĞİL. Tahta yerinde kalıyor, üstüne koyu bir perde
  iniyor ve tanıtılan öğe spot ışığıyla aydınlatılıyor — arkasında sıcak sarı
  bir hüzme, çevresinde altın kıvılcımlar. Yerleşim (0-1, alttan):
  - "Ice Blocks!" beyaz balon yazı, mor kontur: y 0.758–0.820
  - "New Item Unlocked!" daha küçük beyaz yazı: y 0.707–0.730
  - Öğenin kendisi (büyük buz bloğu) ortada: x 0.385–0.622, y 0.461–0.582
  - Altta krem kapsül + mor çerçeve, içinde ipucu:
    "Clear blocks to break the ice!" — x 0.084–0.917, y 0.270–0.340
  - Kırmızı daire çarpı sağ üstte, HUD hizasında: x 0.845–0.946, y 0.867–0.910

  Muhtemelen `TutorialOverlay` bu işin doğru evi — önce ona bak, sıfırdan
  panel yazma.
- **Duraklat paneli** — referansta Sounds/Musics/Haptics anahtarları +
  yeşil Resume + kırmızı Quit. Referans kare: `1-20` videosu, 00:03.

  **BİTTİ, gözle doğrulandı.** Referanstan ölçülerek yeniden kuruldu: mor kart,
  üst kenara binen "Pause", sağ üstte kırmızı daire çarpı, üç ayar satırı
  (Sounds / Musics / Haptics) + Off/On anahtarları, ayraç, yeşil "Resume",
  kırmızı "Quit". "Yeniden Başla" KALDIRILDI — referansta yok, yeniden başlatma
  zaten HUD'daki geri düğmesinde.

  Anahtarlar `SettingsBinder` üzerinden kayda yazıyor; panel her açılışta
  kayıttan tazeleniyor (ayar Ayarlar ekranından da değişmiş olabilir).
  Her iki yarı da tıklanabilir — oyuncu istediği duruma DOĞRUDAN basıyor,
  tek düğmeli "değiştir" davranışı bir fazladan adım olurdu.

  Üç simge (hoparlör, nota, titreşim) elimizde yoktu, **çizim olarak üretildi**:
  `UiSprites.Speaker / MusicNote / Haptics`. İşaretli mesafe (SDF) ile
  çiziliyorlar — tek 128×128 dokudan her ölçekte temiz kenar, ve şekli
  değiştirmek birkaç sayı değiştirmekten ibaret.
- **21-30 videosu TARANDI.** Turuncu çerçeveli sayılı alanlar bizim PERDE
  mekaniğimiz — yeni değil. İki YENİ arayüz öğesi çıktı, ikisi de eklendi:
  - **"Hard Level" satırı** — sayacın hemen altında pembe yazı. `LevelData`
    zaten `difficulty` taşıyordu (13 hard + 5 superhard bölüm var); ikisi de
    satırı açıyor. Superhard'ın referansta ayrı bir yazısı var mı GÖRÜLMEDİ.
    Referans kare: 21-30 videosu, Level 24.
  - **Yardımcı fiyatı jeton kapsülünde** — elde hiç yokken kırmızı adet rozeti
    yerine, düğmenin altında jeton simgeli SARI kapsül. Adet ve fiyat biçimle
    ayrılıyor: "3" ile "300" aynı rozette olsa oyuncu ikisini karıştırır.
- **31-40 ve 41-50 videoları TARANDI. Yeni arayüz öğesi ya da yeni mekanik YOK.**
  Bütün referans videoları artık taranmış durumda. Cikanlar:
  - Tahtalar düzensiz siluetlere bürünüyor (elmas, ev, merdiven, S). Bunlar yeni
    mekanik değil — bölüm JSON'undaki `rows` maskesi zaten destekliyor.
  - Mor ve camgöbeği bloklar çıkıyor; paletimizde ikisi de var.
  - Perde sayaçları büyüyor (17, 20), kapı sayısı artıyor. Aynı mekanik.
  - "Hard Level" satırı burada da görünüyor — eklendi.
  - Yolculuk bölge tanıtım ekranları: Level 34/35 uzay-roket sahnesi,
    Level 44 kar-penguen sahnesi. **"Penguin Chase" adı referansta birebir
    böyle geçiyor** (bizim çevirimiz tuttu).

  Yani referanstan çıkarılacak arayüz işi KALMADI. Bundan sonrası ses, cihaz
  ölçümü ve denge.

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
4. **`Mathf.SmoothStep` GLSL'deki `smoothstep` DEĞİL.** Unity'ninki iki
   DEĞER arasında yumuşatılmış lerp yapar ve t'yi 0-1'e kırpar — eşik
   uygulamaz. `1 - Mathf.SmoothStep(63f, 64f, d)` bir maske değil, her yerde
   negatif bir sayıdır; alfa tamamen 0 çıkar. Mağaza kapsülleri, ∞ halkası ve
   tente festonu tam olarak bu yüzden hiç görünmedi. `UiSprites` de aynı hatayı
   taşıyordu: yuvarlak panellerin arkasında %25 saydamlıkta bir KARE hayalet
   vardı ve aylarca fark edilmedi. İkisi de düzeltildi (`static float Step(...)`).
5. **Saydamlığı satranç zemine bindirip kontrol etme.** Üretici görselin
   sahte saydamlığı da satranç; koyu düz renk kullan.
   (`UiCaptureTool` bu yüzden koyu zemin kullanıyor.)
6. **BASKILI GÖLGELİ SPRITE'I BOYAMA.** 1. maddenin devamı ve daha sinsi
   hâli: `panel_card` ve `btn_*` görsellerinin alt kenarında BASKILI 3B gölge
   var. Boyama çarpma olduğu için o gölge turuncuya boyayınca KIRMIZIYA, kreme
   boyayınca MAGENTAYA kayıyor ve düğmenin altında oyunun hiçbir yerinde
   olmayan bir renk şeridi bırakıyor. Kullanıcı bunu "bozuk, tarzla uyumsuz,
   orijinal oyunda yok" diye işaretledi — haklıydı, oyunda 15 yerde vardı.
   - Bir yüzeyi BOYAYACAKSAN baskılı gölgesi olmayanı kullan:
     `UiKit.CreateOutlinedBox` ya da `UiSprites.RoundedPanel`.
   - Düğme kuracaksan rengi boyama, ROLÜNÜN görselini seç:
     `UiKit.CreateSpriteButton(..., Art.ButtonGreen / ButtonPurple / ButtonRed)`.
   - `CreateTintedButton` artık yalnız baskılı gölgesi OLMAYAN sprite'larla.
7. **Paylaşılan başlık materyali tek tek kontur ayarı KABUL ETMEZ.**
   `CreateTitle`'a verdiğin kontur rengi sessizce yok sayılır — bütün başlıklar
   tek materyali paylaşıyor. Tek bir başlığa özel kontur istiyorsan
   `UiKit.SetOutline(label, renk, kalınlık)`.

## Device Simulator (2026-08-16 kuruldu)

`com.unity.device-simulator.devices` 1.0.1 kurulu — **82 cihaz profili**.
Açmak: `Window > General > Device Simulator`, ya da Game view sekmesinden
"Simulator".

Ne işe yarar: çentik/güvenli alan, gerçek en-boy oranları ve DPI. Bizim
arayüzümüz tamamen oransal kurulduğu için asıl sınav çentik.

**Kurulurken bir hata yakalandı:** `UiKit.CreateSafeArea` güvenli alanı
YALNIZCA kurulum anında okuyordu. Tek cihazda kusursuz çalışır ama simülatörde
cihaz değiştirince arayüz ESKİ çentiğe göre kalırdı — üstelik simülatör "test
ettim" hissi verdiği için yanlış sonuç doğru sanılırdı. Telefon döndüğünde de
aynı sorun. Artık `UiSafeArea` bileşeni ekran değiştikçe yeniden uyguluyor
(her kare değil, yalnız değişince).

Bu, DEVAM.md'deki 3. tuzağın ("kurulumu değiştirmek yetmez, TAZELEMEYİ de
değiştir") dördüncü tekrarıydı.

---

## Yakalama/doğrulama notları

- **~~`UiCaptureTool.Capture()` kadrajı bozuyor~~ — ARTIK GEÇERSİZ (2026-08-17).**
  Bu not bir zamanlar doğruydu ama şimdi `Capture(ad)` tam olarak 1080×1920
  ve doğru kadrajda çıkıyor (beş sekmenin beşi de görünüyor). **Üstelik ASIL
  KULLANILMASI GEREKEN O.** `CaptureOf<T>` yalnız o bileşenin KENDİ kanvasını
  çiziyor; alt sekme çubuğu `MenuCanvas`'ta olduğu için `CaptureOf<HomeScreen>`
  karesinde HİÇ görünmüyor, menü sayfalarının altındaki zemin de görünmüyor.
  Bu yüzden iki gerçek hata aylarca gizlendi: Liderlik'te sekmelerin arkasındaki
  siyah şerit ve podyumda birbirine giren kürsüler. İkisi de ilk tam ekran
  `Capture()` çağrısında bir anda ortaya çıktı.
  **Kural: kıyas ve denetim için `Capture(ad)`; yalnız tek bir paneli izole
  etmek istediğinde `CaptureOf<T>`.**
- **TAM KARE yakalama (tahta + zemin + HUD).** `UiCaptureTool` kendi
  kamerasını kurup YALNIZ kanvası çeker; tahta ve zemin o karede yoktur, bu
  yüzden referansla "ne kadar karardı" kıyası yapılamaz. Tam kare için ana
  kamerayı bir `RenderTexture`'a yönlendir, kanvası geçici olarak
  `ScreenSpaceCamera` + `planeDistance = 0.5` yap, `Camera.Render()` çağır,
  sonra hepsini geri al. (Yardımcı istemi kıyası bununla yapıldı.)
  Tuzak: bu yöntemde TMP bazı harfleri eksik çiziyor ("remove" → "emove");
  yakalama artefaktı, gerçek hata değil — kanvas yakalamasında metin doğru.
- **MCP komutları play modu SIRASINDA domain reload tetikleyebiliyor.**
  Bir doğrulama komutu `MetaServices.Save.Data.Settings`'i sorunsuz okudu,
  iki komut sonra aynı satır NullReference attı: `MetaServices.Ready`
  **False** olmuştu. Play modundan çıkılmamıştı — araya giren derleme
  statikleri sıfırladı, `RuntimeInitializeOnLoadMethod` ise yalnız play
  BAŞLARKEN koşar. 0c maddesindeki tuzağın oturum ORTASINDA çıkan hâli.
  **Çözüm:** etkileşim testini TEK komutta yap (düğmeye `onClick.Invoke()`
  ile basmak kare gerektirmez, `Show()` de anında etkir). Her komutun başına
  `if (!MetaServices.Ready) { LogError; return; }` koy — yoksa sıfırlanmış
  statiklerle çalışan bir testin sonucunu doğru sanarsın.
- **`System.IO` kullanan MCP komutu reddediliyor** ("User interactions are not
  supported"). Kayıt dosyasını yedekleme/geri yükleme gibi işleri kabuktan yap.
  Aynı kapı `System.Reflection`'a da kapalı — özel alanları yansımayla
  okuyarak teşhis yapamazsın, davranışı dışarıdan gözlemle.
- **PLAY MODU AÇIKKEN KAYNAK DOSYASI DÜZENLEME.** Unity derleyip domain'i
  yeniden yükler; oyun çalışmaya devam eder ama C# çalışma-zamanı durumu
  SİLİNİR: `AddListener` ile bağlanmış bütün dinleyiciler gider,
  `[SerializeField]` olmayan özel alanlar null olur. Sahne hiyerarşisi
  yerinde durduğu için her şey normal GÖRÜNÜR — düğmeler oradadır, tıklarsın,
  hiçbir şey olmaz ve konsolda tek bir hata yoktur. Bir saat "panelim neden
  aşama değiştirmiyor" diye aranmasının sebebi buydu; kod doğruydu.
  **Kural: düzenlemeden önce play modundan çık, sonra temiz gir.**
  Teşhis işareti: bileşenin `Visible` gibi alan okuyan bir özelliği
  beklenmedik biçimde `False` dönüyorsa alanlar silinmiştir.
- **Yıkıcı bir akışı denemeden ÖNCE kaydı yedekle.** Hesap silme testi gerçek
  ilerlemeyi siler; `%LOCALAPPDATA%Low/furkanblci/Block Out Clone/` altındaki
  `save.json`, `save.json.bak`, `analytics.json` kopyalanıp sonra geri konuyor.
  Play modundan ÖNCE çık, sonra geri yükle — çalışan oyun çıkarken üstüne yazar.
- **`EditorApplication.Step()` play modunda ÇAĞIRMA.** MCP komutunun içinden
  çağrılınca "PlayerLoop internal function has been called recursively"
  hatası yağıyor. Onun yerine `Application.runInBackground = true` yap ve
  kareleri AYRI komutlara böl — komutlar arasında gerçek zaman geçtiği için
  kareler kendiliğinden ilerliyor.
- **Aynı komutta hem bölüme girip hem yardımcıyı kullanma.** Arada kare
  geçmediği için `Update`'te duran bağlanma çalışmaz ve istem hiç açılmaz;
  bu bir oyun hatası değil, testin kendi hatasıdır (bir kez düşüldü).
- `EditorApplication.QueuePlayerLoopUpdate()` kareleri **kuyruğa alır**;
  aynı komut içinde ekran görüntüsü alırsan güncellenmemiş hâli yakalarsın.
  Kareleri bir komutta çevir, **ayrı bir komutta** yakala.
- `ScreenCapture.CaptureScreenshot` MCP üzerinden engelli. Kanvasları geçici
  olarak ana kameraya bağlayıp elle render et (`UiCaptureTool` bunu yapıyor).
- Play moduna girmeden önce **mutlaka** derlemenin bittiğini doğrula; aksi
  halde eski kodla çalışan bir oturumu test edersin.

---

## GÖRSEL DENETİM — 2026-08-10, YARIM KALDI

Ekranlar referansla yan yana kıyaslanmaya başlandı. Yöntem: referans kareyi ve
bizim yakalamayı tek bir sayfaya yan yana koyup bakmak (`cmp_*.png`).
**Kıyas ölçüsü dikey yüzde DEĞİL, EKRAN GENİŞLİĞİNE oran olmalı** — referans
kare 384×832 (oran 2.17), bizim yakalama 1080×1920 (oran 1.78); dikey yüzdeler
doğrudan karşılaştırılamaz.

### Bulunan ve DÜZELTİLEN
- **Ayarlar/Profil tam ekran olmalıydı.** MenuShell ekranları 0.105–1 arasına
  koyuyor; referansta bu ikisi sekme çubuğunu da örten TAM EKRAN sayfalar.
  `MenuPage.Screen` kökü 210 birim aşağı taşıyor, MenuShell de bu ikisini
  çubuktan SONRA çiziyor (`SetAsLastSibling`).
- **Ayarlar çok küçüktü:** satır 132→196, kart/düğme/yazı ölçüleri referans
  oranına çekildi. Artık örtüşüyor.
- **Profil büyütüldü** (kutu 150→214, punto artırıldı) ama küçük önizleme
  üzerinden daha fazla ince ayar yapmak güvenilir değil. Bir sonraki oturumda
  yapılacak ilk iş: profil karesini TAM ÇÖZÜNÜRLÜKTE referansla yan yana koyup
  kutu yüksekliğini piksel ölçerek doğrulamak (göz kararı değil).

### AÇIK KALAN GÖRSEL EKSİKLER
1. ~~Zil simgesi~~ — **BİTTİ**. `UiSprites.Bell` eklendi (kubbe + etek + alt
   kenar + tokmak, işaretli mesafeyle). Ayarlar artık dört satırda da simgeli.
2. **Altın `icon_lock` gelmedi** — mevcut gümüş kilit sıcak tona boyanıyor,
   mat pirinç çıkıyor. Prompt `docs/art-prompts.md` §8.2'de.
3. **Podyumda üç oyuncu da aynı avatar** — `avatar_2/3/4` üretilmedi.
4. Ayarlar kartının referanstaki ince açık kenarlığı bizde yok (çok küçük fark).

### KIYASLANDI ve DÜZELTİLDİ
- **Liderlik** — ölçek büyütüldü; sıra rozeti kendi kapsülüne alındı, avatar
  kare çerçeveye, puan rozetine altın kenar eklendi. **Referansta 5 rakip
  satırı var, bizde 4** — referans ekran 9:19.5, bizimki 9:16; aynı ölçekte
  beşincisi sığmıyor, oranı korumak bir satırdan önemli sayıldı.
  İki tuzak: (a) `enableAutoSizing` tek başına çalışmaz, `overflowMode`
  kısıtlayıcı olmalı; (b) sığdırılmış yazı bile bitişik okunur, boşluk
  KUTUDAN gelmeli.
- **Yolculuk** — çember 646→812, kapsül genişliği %80→%90, seviye puntosu
  56→66, bölge adı 50→62, kilit/etiket/düğme büyütüldü. Referansla örtüşüyor.

- **Mağaza** — bütün kart yükseklikleri ~%30 büyütüldü (teklif kartı 388→506,
  paket kartı 380→495, jeton kutusu 354→452, bölüm şeridi 82→104) ve puntolar
  buna göre artırıldı. Referansla örtüşüyor.

### BEŞ MENÜ EKRANININ HEPSİNDE AYNI HATA ÇIKTI
Hepsi referanstan **%25-35 küçük** kurulmuştu. Sebep tek: ölçüler referans
karesinden alınırken dikey yüzdeyle kıyaslanmış. Referans kare 384×832
(oran 2.17), bizim yakalama 1080×1920 (1.78) — "ekranın %30'u" iki tarafta
farklı şey. **Doğru birim: EKRAN GENİŞLİĞİNE oran.** Kanvas ölçekleyici
yüksekliğe kilitli olduğu için genişlik sabit referans.
Yeni bir ekran kurarken ölçüyü şöyle çevir:
`birim = referans_piksel / referans_genişlik * 1080`

### OYUN İÇİ PANELLER — kısmen kıyaslandı
- **Duraklat paneli** — ✅ **referansa UYUYOR.** Kodu referansla satır satır
  karşılaştırıldı (`GameplayScreen.BuildPausePanel` + `BuildSettingRow`):
  koyu perde, mor kart, üst kenara binen "Pause", kırmızı çarpı, üç satır
  (Sounds/Musics/Haptics) ikili [Off|On] anahtarıyla, ayraç, yeşil Resume,
  kırmızı Quit. Referans kare: `1-20` videosu **00:02**.
- **PERFECT paneli** — daha önce bitmişti (bkz. yukarıdaki bölüm).
- **YARDIMCI İSTEMİ — BİTTİ, PLAY MODUNDA SAYIYLA DOĞRULANDI (2026-08-16).**

  **Önce ölü bir olay bulundu.** `GameplayScreen.Init` içindeki
  `if (_session.PowerUps != null)` HER ZAMAN false'tu: `EnsureServices` bu
  ekranı kurarken `PowerUps` henüz doğmamış oluyor (o nesne `Restart` içinde
  kuruluyor). Yani `PowerUps.Message` olayının **hiç dinleyicisi yoktu** —
  yeni istem paneli de, ondan önceki 2.5 saniyelik ipucu yazısı da hiçbir
  zaman çalışmamış. Üstüne `Restart` her çağrıldığında `PowerUps` YENİDEN
  kuruluyor, yani tek seferlik abonelik doğru anda kurulsa bile ilk yeniden
  başlatmada ölü nesneyi dinler kalırdı. İkisi de `BindPowerUps()` ile
  çözüldü: her karede örnek değişti mi diye bakıyor, değiştiyse yeniden
  abone oluyor.

  **Sonra karartmanın yanlış katmanda olduğu bulundu.** Perde HUD kanvasına
  seriliyordu; kanvas `ScreenSpaceOverlay` olduğu için TAHTAYI da karartıyordu.
  Referans kareleri (istemli 66-67. sn ↔ istemsiz 64/68/70. sn) tersini
  söylüyor:

  | bölge | normal | istemli | sonuç |
  |---|---|---|---|
  | boş zemin | 49.0 | 23.0 | %47'ye iner |
  | Seviye rozeti (tepe) | 255 | **43** | görünmez |
  | duraklat düğmesi (tepe) | 255 | **50** | görünmez |
  | **tahta** | 119.5 | 119.4 | **değişmez** |

  HUD'ın kalıntısı karartılmış zeminin tepe değeriyle (44) aynı — yani
  referans HUD'ı **soldurmuyor, tamamen gizliyor**. Başlığın sayaçla
  çakışması da bu yüzden referansta yok.

  **DERS (ortalama yalan söyler, tepe değer söylemez):** HUD'ın ortalaması
  49.3 → 23.4 düşüyor ve bu "yarı yarıya soluyor" gibi okunuyor; ilk düzeltme
  alfayı 0.48 yapmıştı. Ortalamanın çoğu zaten koyu olan zemin. Bir öğenin
  görünürlüğünü ölçerken TEPE değere bak.

  **DERS (katsayı ekranda gördüğün oran değildir):** Zemini %47'ye indirmek
  için 0.47 yazmak yanlış — çarpım doğrusal uzayda, ölçtüğümüz PNG sRGB.
  0.36 katsayısı ekranda 0.23 oldu; doğru değer (0.59) ölçerek geri çözüldü.

  Son hâl üç ayrı katmandan karartıyor: HUD `CanvasGroup` ile gizleniyor
  (`blocksRaycasts` de kapanıyor — görünmeyen duraklat düğmesine basılmasın),
  zemin `BackgroundView.SetDim` ile (paylaşılan malzemeyi boyamamak için
  `MaterialPropertyBlock`), yardımcı düğmeleri tek tek (seçilen parlak kalsın).

  Doğrulama sonucu — referans / bizim: zemin oranı 0.47 / **0.50**,
  HUD kalıntısı tepe 43 / **38**, ortalama 20.5 / **20.9**.
  Tahtaya raycast: **0 engel**. Kapanışta her şey geri alınıyor
  (alfa 1, raycast açık, panel kapalı, özellik bloğu temiz).

  Eski notun kendisi: Referansta (menüler videosu **67. saniye**)
  yardımcı seçilince EKRANIN TAMAMI değişiyor: tahta kararıyor, sol üstte
  BÜYÜK parlayan yardımcı görseli + kıvılcımlar, ortada beyaz mor konturlu
  başlık ("Roket"), altında yönerge ("Kaldırılacak bloğu seçin"), alttaki
  yardımcı düğmesi de parlıyor.
  **Bizde yalnız 2.5 saniyelik bir yazı var** (`GameplayScreen._hint`).
  Yapılacak: `PowerUpPrompt` diye ayrı bir örtü paneli; `PowerUps.Message`
  olayına yazı yerine (kind, prompt) geçirilip görsel de gösterilmeli.
- Kaybetme paneli hiç kıyaslanmadı — ama referansı bulundu:
  `OneDrive/Masaüstü/Block Out! Videos/Game over .mp4`.

### KAYBETME AKIŞI — kıyaslandı (2026-08-16)

Referans: `OneDrive/Masaüstü/Block Out! Videos/Game over .mp4` (24 sn, 384x832).
Kontakt sayfası: `fps=1, tile=8x4`.

**EN ÖNEMLİ BULGU: kaybetmek TEK panel değil, ÜÇ AŞAMALI bir akış.**
Bizde tek bir "TIME'S UP" kartı vardı; referans şunu yapıyor:

| # | panel | referans sn | içerik |
|---|---|---|---|
| 1 | **Süre Doldu** | 8-11 | altın kronometre, "+30 saniye", yeşil `Zaman Ekle 🪙900` |
| 2 | **Devam Et?** | 12-16 | kırık kalp, "1 can kaybedeceksiniz!", yeşil `Oyna 🪙900`; altında ayrı "Blok Bileti" promosyon kartı |
| 3 | **BAŞARISIZ** | 17-20 | mor kart, üste binen altın başlık, kırık kalp, "Zor" + "Seviye 54", jeton yığını, `60 ❌`, "Ödüller x3", yeşil `Yeniden Dene` |

Aradaki 4. ve 10. saniyelerde **Mağaza** açılıyor — yani jetonu yetmeyen
oyuncu bu panellerden doğrudan mağazaya gidebiliyor.

**3. AŞAMA (BAŞARISIZ) KURULDU ve play modunda doğrulandı.** Değişenler:
- Başlık `TIME'S UP` (kırmızı) → **`FAILED` (ALTIN)**. Referans kaybı RENKLE
  değil BİÇİMLE anlatıyor; kırmızı başlık bizim eklememizdi.
- Düğme kırmızı → **yeşil** (referansta "Yeniden Dene" yeşil).
- **DERS (kaçırılan ödülü GÖSTER, gizleme):** Eski kod kaybedince ödül alanını
  tamamen kaldırıyordu. Referans onu gösterip üstüne kırmızı çarpı atıyor.
  "Şunu kaçırdın" demek, "burada bir şey yok" demekten çok daha güçlü bir
  tekrar oynama sebebi. Bunun için `ProgressService.PreviewReward` eklendi.
- **DERS (okuma kayıt OLUŞTURMAMALI):** `Progress.Record(id)` aradığını
  bulamazsa yeni kayıt yaratıp kayda YAZIYOR. Ödülü sırf göstermek için onu
  çağırmak, oynanmamış her bölüm için kayıt dosyasında boş satır açardı.
  `PreviewReward` bu yüzden `TryGetValue` ile okuyor.
- Bölüm numarası artık kaybederken de var; zorluk etiketi panelde KISA
  ("Hard"), HUD'daki uzun hâli ("Hard Level") değil.

**AÇIK KALANLAR:**
1. ~~1. ve 2. aşama panelleri KURULMADI~~ — **KURULDU (2026-08-17),
   play modunda sayıyla doğrulandı.** Bkz. aşağıdaki bölüm.
2. ~~Kırık kalp görseli yok~~ — geldi (`icon_heart_broken`), iki panel de
   onu kullanıyor.
3. "Ödüller x3" etiketinin ne yaptığı referanstan ANLAŞILMADI — reklamla
   ödül katlama olabilir; şu an yalnız görsel olarak duruyor.

### KAYBETME AKIŞININ İLK İKİ AŞAMASI KURULDU (2026-08-17)

`Assets/_Project/Scripts/Runtime/UI/ContinueOffer.cs`. Süre bitince artık
FAILED kartı DOĞRUDAN açılmıyor; referanstaki iki teklif araya giriyor.

**Ölçüler referans karelerinden alındı** (`Game over .mp4`, 384×832):
1. aşama 8.6. saniye, 2. aşama 13.6. saniye. Yatayda `piksel/384`, dikeyde
`1 - piksel/832` (kabuk ekranlarındaki kuralın aynısı).

| | 1. aşama "Time's Up!" | 2. aşama "Continue?" |
|---|---|---|
| kart | **YOK** — içerik doğrudan perdenin üstünde | ekranı uçtan uca geçen mor sayfa, Y 0.298–0.792 |
| görsel | çalar saat, X 0.219–0.740 · Y 0.416–0.675 | kırık kalp, X 0.302–0.698 · Y 0.543–0.683 |
| yazı | "+30 SECONDS" Y 0.326–0.371 | "You'll lose 1 life!" Y 0.462–0.492 |
| düğme | "Add Time 🪙900" X 0.172–0.833 · Y 0.162–0.250 | "Play 🪙900" X 0.203–0.802 · Y 0.352–0.440 |
| çarpı | HUD hizasında Y 0.894–0.942 | panelin üst kenarında Y 0.757–0.803 |

2. aşama tek bir kart görseli DEĞİL, üst üste beş bant: kenar (`#2C0B5E`),
başlık (`#7035D3`), koyu oyuk (`#311265`), gövde (`#6122BC`), ayak (`#4E1B9C`).
Renkler referans karesinden piksel örneklenerek alındı; tek bir sprite bu
kademeyi veremezdi.

**Karartma referansta ÇOK daha koyu.** Yardımcı isteminde tahta %47'de
kalıyordu (okunması gerekiyordu); burada ölçüm **%8–10** çıktı — tahta
yalnızca bir hatıra. Bu yüzden perde neredeyse mat (alfa 0.94) ve HUD
tamamen gizleniyor (alfa 0).

**Jeton sayacı panelin KENDİSİNDE.** HUD gizlendiği için sayaç da onunla
kaybolurdu; referans tam bu yüzden sayacı panelin üstünde ayrıca çiziyor.
Fiyat gösteren bir ekran cüzdanı da göstermeli.

**Doğrulama (play modunda, sayıyla):**
- Süre bitti → 1. aşama açıldı, HUD alfa 0, FAILED kartı kapalı.
- 1. aşamada çarpı → 2. aşama; 2. aşamada çarpı → teklif kapandı, FAILED
  açıldı, HUD alfa 1'e döndü.
- 1320 jetonla "Add Time" → jeton **420**, durum **Playing**, kalan süre
  **30**, teklif kapandı, FAILED açılmadı.
- 420 jetonla (fiyat 900) → jeton değişmedi, durum Lost, panel AÇIK kaldı,
  FAILED açılmadı. İki aşamada da aynı.
- Reddedip yeniden başlat → teklif yeniden açılıyor.

**REFERANSTAN BİLEREK ALINMAYAN İKİ ŞEY:**
- 2. aşamanın altındaki **"Blok Bileti" promosyon kartı** kurulmadı: bizde
  öyle bir ürün yok, olmayanı satan bir kart uydurma olurdu.
- Referansta jetonu yetmeyen oyuncu bu panellerden **mağazaya gidebiliyor**
  (4. ve 10. saniyeler). Bizde mağaza menü kabuğunda ve menüye dönmek
  `AppRoot.ShowMenu → StopLevel` ile tahtayı söküyor — yani "devam et"
  imkânsız hâle gelirdi. Bunu yapmak için mağazanın oynanışın ÜSTÜNE açılan
  bir kip kazanması gerekir. Şimdilik jeton yetmeyince fiyat kapsülü
  sarsılıyor ve panel açık kalıyor.
- Referansta 1. aşamanın kronometresi **altın**, bizimki süre yardımcısının
  **yeşil** çalar saati. Boyayarak çözülemez (boyama çarpmadır, denendi);
  görsel isteği `docs/art-prompts.md` §10'da.

**Teklif deneme başına BİR KEZ** çıkıyor (bizim kararımız, referanstan
çıkarılamadı): jetonla devam edip yine kaybeden oyuncu doğrudan FAILED'e
gidiyor. Sonsuz "öde ve devam et" döngüsünü referans doğrulamadan kurmak
istemedik.

### GÖRSEL AKTARIMI — sessiz atlama hatası (2026-08-16)

**`tools/import_art.py` yalnız `*.png` topluyordu ve JPG'leri UYARI VERMEDEN
atlıyordu.** Sonuç: `avatar_2/3/4` 15 Ağustos'tan beri `art_raw/` içinde
duruyor ama oyuna hiç girmemişti; altın `icon_lock.jpg` de aynı gün gelmiş,
aynı şekilde kaybolmuştu. Kullanıcı "kilit zaten vardı" görüp (o ESKİ GÜMÜŞ
`icon_lock.png` idi) ikinci bir dosya (`icon_lock2.jpg`) üretmek zorunda
kalmıştı.

**DERS (sessizce atlamak hatadan beterdir):** Bir boru hattı işleyemediği
girdiyi görmezden gelirse, sorun aylarca "görsel gelmedi" diye yanlış yerde
aranır. Artık `*.jpg/*.jpeg` de okunuyor; alfa yokluğu sorun değil, kesim
zemini zaten kendisi siliyor.

**DERS (uzantıya değil TARİHE bak):** Aynı isim iki uzantıyla gelince ilk
kural "PNG kazansın" idi (alfası olabilir diye). Gerçek durum tersine döndü —
BAYAT gümüş PNG, yeni altın JPG'yi eziyordu. Artık `st_mtime` en yeni olan
kazanıyor. `icon_lock2` ise `SKIP` kümesinde: dosya silinmiyor (kullanıcının
ürettiğini silmek bizim işimiz değil), yalnız oyuna girmiyor.

Aktarılanlar: `icon_heart_broken`, altın `icon_lock`, `avatar_2..4`,
`avatar_6..9`. Kesimler koyu zeminde tek tek doğrulandı, hâle yok.
`check_art.py` bunları GÖRMÜYOR — sabit listeden çalışıyor, yeni dosyalar
listeye eklenmedikçe "temiz" demesi bir şey ifade etmiyor.

**Avatarlar bağlandı:** podyum ve rakip satırları artık farklı yüzler
kullanıyor (`UiSkin.Rival(index)`), oyuncunun kendi satırı `avatar_player`.
- **`avatar_6..9` KULLANILMIYOR:** kendi ALTIN ÇERÇEVESİYLE geliyorlar,
  bizim kare çerçevemizin içine konunca çift çerçeve oluyor.
- Elde 3 çerçevesiz rakip yüzü var, 7 slot (3 podyum + 4 satır) — tekrar
  kaçınılmaz. Tekilleştirmek için 4 tane daha ÇERÇEVESİZ avatar gerekir.

**Kaçan iki Türkçe metin bulundu ve çevrildi:** `DailyRewardPanel`'de
"80 JETON AL" → "CLAIM 80", `GameplayScreen`'de "+N jeton" → "+N".
İkisinde de ç/ğ/ı/ö/ş/ü yok — dosyanın yukarısındaki uyarı tam da bunu
söylüyordu, yine de kaçmışlar. Bir sonraki taramada kelime listesiyle git.

## MEKANİK DENETİM — başladı (2026-08-16)

Görsel kıyas bitti; bu bölüm "neye basınca ne oluyor" sorusunu izliyor.
Yöntem: referansı `fps=2` kontakt sayfasıyla tara, sonra kodda karşılığını ara.

### Yolculuk — referansta doğrulananlar (bizde ZATEN doğru)
- **"Üst"/"Alt" düğmeleri GÖRÜNTÜ ALANINA sabit**, içerikle kaymıyor —
  her karede aynı ekran konumunda duruyorlar. Bizde de öyle.
- Ekran açılınca **oyuncunun bulunduğu yere kaydırılmış** geliyor.
- Bölge adı çemberin DIŞINDA üstte; aralık etiketi ("sv 41 - 70") çemberin
  üst kenarına biniyor; pembe seviye rozeti tüpün üstünde.
- Üç bölge durumu: kilitli (gri + altın kilit + "Seviye N"), açık (yeşil
  düğme), tamamlanmış (yeşil tik).

### BULUNAN ÜÇ ÖLÜ KONTROL

**DERS (düğme gibi duran şey BİR ŞEY YAPMALI):** Üçü de ekranda parlak,
tıklanabilir ve "çalışıyormuş" gibi duruyordu. Basınca hiçbir şey olmayan
bir düğme, eksik bir özellikten beterdir — oyuncu oyunu bozuk sanır.
Tarama yöntemi basit ve tekrarlanabilir: her dosyada düğme üreten çağrı
sayısıyla `onClick.AddListener` sayısını karşılaştır.

1. **Yolculuk bölge düğmesi — DÜZELTİLDİ, play modunda doğrulandı.**
   Dosyadaki tek gerçek `Button` atlama düğmeleriydi; bölge çemberindeki
   yeşil düğmenin `onClick`'i HİÇ bağlanmamıştı. Artık
   `JourneyScreen.PlayRegion(from, to)`: oyuncunun ilerlemesi bölge
   aralığına KIRPILIYOR — içinde bulunduğu bölge "kaldığın yerden devam",
   tamamladığı bölge "o bölgenin son bölümünü tekrar oyna".
   Doğrulama: 5 bölgenin de Button'ı var; tıklayınca `InGame=True`,
   `LastPlayedLevelIndex=10` (seviye 11, 1-20 aralığına kırpılmış).

2. **Ayarlar "Delete My Account" — DÜZELTİLDİ ve 2026-08-17'de ELLE
   DENENDİ** (bkz. aşağıdaki "HESAP SİLME" bölümü). Düğme bile değildi,
   yalnız kapsül + yazıydı.
   Artık gerçek düğme ve **iki aşamalı**: ilk dokunuş kırmızıya dönüp
   "Tap again to erase everything" diyor, ikinci dokunuş `Save.Reset()`
   çağırıp ana ekrana dönüyor; 5 saniye içinde onaylanmazsa kendiliğinden
   geri alınıyor. Ayrı onay penceresi kurmadım — yıkıcı işlem için iki
   dokunuş yeterli koruma ve yeni bir pencere düzeni gerektirmiyor.

3. **Liderlik sekmeleri KOZMETİKTİ — DÜZELTİLDİ, play modunda doğrulandı.**
   `Weekly / World / Country` üçü de bağlıydı ama `_activeTab` yalnız sekme
   RENGİNİ değiştiriyordu; podyum ve satırlar üç sekmede de birebir aynıydı.
   Üç ayrı kapı açıp üçünü de aynı odaya çıkarmak, düğmeyi hiç koymamaktan
   daha kötü — oyuncu bir süre farkı arayıp oyunu bozuk sanıyor.

   Artık `Boards[3][8]` var (haftalık düşük puanlı, dünya en yüksek, ülke
   ortada) ve `Refresh` podyum adlarını + 4 satırın ad/puan/seviye
   yazılarını yeniden yazıyor. `BuildRowContent` artık puan yazısını da
   döndürüyor. Doğrulama (sekme sırasıyla podyum 1-2-3):
   `mira·kret·Bet` / `aisha·Bet·zzz` / `polat·nurhayat·kret`, satırlar da
   değişiyor, "You" satırı üçünde de en altta sabit.

4. **`LeaderboardScreen.Score()` kayıt dosyasını ŞİŞİRİYORDU — DÜZELTİLDİ.**
   Puanı hesaplarken BÜTÜN bölümler için `progress.Record(id)` çağırıyordu;
   o metot bulamadığını YARATIP kayda yazıyor. Yani liderlik ekranı her
   tazelemede oynanmamış her bölüm için kayda boş bir satır ekliyordu.
   `ProgressService.Peek(levelId)` eklendi (yoksa `null`, hiçbir şey
   yaratmaz) ve `Score` ona geçirildi.
   **Bu, `PreviewReward`'da düzeltilen tuzağın İKİNCİ örneği** — bu depoda
   `Record()` bir "getir ya da yarat"; yalnız okuyacaksan `Peek` kullan.

5. **Sekme geçişi ANLIKTI — DÜZELTİLDİ, play modunda doğrulandı.**
   2 kare/sn taramada ara kare görünmediği için "anlık" sanılmıştı; **30
   kare/sn** ile bakınca referansın kaydırdığı ortaya çıktı (35-37. sn,
   Yolculuk → Ana Ekran): yeni ekran yandan giriyor, eski ekran karşı
   yönden çıkıyor, süre **~170 ms** (~5 kare).

   `Juice.SlideX` eklendi (proje kuralı gereği doğrudan PrimeTween değil,
   cepheden) ve `MenuShell.SlideSwap` yönü SEKME SIRASINDAN hesaplıyor —
   sağdaki sekmeye geçerken yeni ekran sağdan giriyor. Çıkan ekran kayma
   bitince kapanıyor ve `anchoredPosition` sıfırlanıyor (yoksa bir dahaki
   açılışta ekran dışında kalırdı).

   Doğrulama: home→store→board→journey→home dizisi; kayma sırasında çıkan
   ekran açık kalıyor, kareler geçince HEPSİ kapanıp x=0'a dönüyor;
   tek geçişte hedef ekran aktif ve x=0.

   **Ana ekran da geçişe KATILDI** (aynı oturumda tamamlandı):
   `HomeScreen.Slide(fromX, toX, süre)` + `HomeScreen.Instance`.
   Manzara (`Background`, kanvasın çocuğu) ile arayüz (`SafeArea`) AYRI
   ebeveynlerde olduğu için İKİSİ birden kaydırılıyor — yalnız birini
   kaydırmak "arayüz kaydı ama dünya durdu" gibi görünürdü.
   Doğrulama: `home→journey→home→store→home` hızlı dizisinden sonra
   `Background x=0`, `SafeArea x=0`, bütün menü ekranları kapalı ve x=0 —
   kayma BİRİKMİYOR, ekran dışında takılan yok. Ana ekran görsel olarak da
   yerinde yakalandı.

### DÖRT ÖLÜ KONTROL DAHA (2026-08-17) — hepsi düzeltildi, play modunda sayıyla doğrulandı

Tarama yöntemi aynı: her dosyada düğme üreten çağrı sayısıyla
`onClick.AddListener` sayısını karşılaştır. Ama bu turda asıl av **sayımın
GÖRMEDİĞİ** yerden çıktı: düğmesi olan ama işi YARIM yapan kontroller.

**6. MENÜDE HİÇ SES YOKTU.** `AudioService` ve `Haptics`, `GameSession`'ın
kurulumunda doğuyordu. Oynanış kökü kapalı başladığı için `Start` hiç koşmuyor
— yani oyuncu **ilk bölüme girene kadar** sahnede AudioService YOKTU:
ana ekranın, mağazanın, ayarların bütün düğmeleri sessiz basılıyordu
(`UiButtonFeel.Clicked` bağlanmamış kalıyor). Üstelik Ayarlar'daki "Sounds"
anahtarı uygulanacak örnek bulamadığı için sessizce hiçbir şey yapmıyordu.
İkisi de `AppRoot.Awake`'e alındı; `GameSession` artık devralıyor, ikinci
örnek kurmuyor. Doğrulama (ana ekranda, hiç bölüme girmeden):
sahnede **1 AudioService, 1 Haptics**, `UiButtonFeel.Clicked` bağlı.

**DERS (menü de oyunun parçası):** Servis "oyun başlayınca" değil "uygulama
açılınca" kurulmalı. Cila servislerini oynanışın kurulumuna asmak, menüyü
sessiz bir maket hâline getiriyordu ve bu aylarca fark edilmedi çünkü test
hep bir bölüme girerek yapılıyordu.

**7. Ayarlar'daki Müzik ve Haptik anahtarları KAYDA yazıyor, SERVİSE
uygulamıyordu.** Oyuncu haptiği kapatıyor, telefon titremeye devam ediyordu;
ayar ancak uygulama yeniden açılınca tutuyordu. Duraklat panelindeki AYNI
anahtarlar ise `SettingsBinder` üzerinden gidiyordu. Ayarlar da oraya bağlandı.

**DERS (ayarı KAYDA yazmak, ayarı UYGULAMAK değildir):** Aynı ayarın iki
yazma yolu varsa biri er geç eksik kalır. Yazma + uygulama tek elden gitmeli.

**8. Aynı görünen anahtar iki ekranda FARKLI davranıyordu.** Ayarlar'da
yuvanın tamamı tek düğmeydi ve durumu TERS ÇEVİRİYORDU — yani yeşil "On"
yarısına, üstünde **On yazan yere** basmak ayarı KAPATIYORDU. Duraklat
panelinde ise iki yarı ayrı ayrı basılıyordu (doğrudan-durum). Ayarlar da
doğrudan-duruma geçti; iki yarının çakışan 0.04'lük bandı da temizlendi.
Doğrulama (Haptics satırı): açıkken On → açık kalıyor; Off → kayıt VE servis
kapanıyor; kapalıyken Off → kapalı kalıyor; On → ikisi de açılıyor.
Sounds için de `Audio.Muted` ters yönde doğru izliyor.

**9. Profil'deki yeşil kalem rozeti ÖLÜYDÜ** — altında `Button` bile yoktu.
Daha kötüsü: `SaveData.PlayerName` alanı kayıtta duruyor, ana ekranın avatar
baş harfini ve liderlik tablosundaki oyuncu satırını besliyordu, ama oyunda
o adı yazabileceğin **tek bir yer yoktu**; alan hep boş kalıyor, her yerde
"Player" / "You" görünüyordu. `NamePanel.cs` eklendi (koyu perde, mor kart,
üst kenara binen "Edit Name", `TMP_InputField`, yeşil Save, kırmızı çarpı).
Doğrulama: kaleme basınca panel açılıyor, alan kayıttaki adla doluyor,
boş ad REDDEDİLİYOR (panel açık kalıp alan sarsılıyor), "  Fikret  " →
kırpılıp `Fikret` olarak kaydediliyor, Profil kartı ve liderlik satırı
anında güncelleniyor.

> **Referanstan ÇIKARILAMADI:** Videoda oyuncunun adı "Fikret" ama kaleme
> hiç dokunulmuyor — bu pencerenin referanstaki hâli GÖRÜLMEDİ. Düzen
> ölçülmedi, projenin kendi menü dilinden kuruldu. Kare bulunursa ölçüler
> oradan düzeltilmeli.

**Ana ekrandaki avatar baş harfi bir HATA DEĞİL:** `_avatarInitial` yalnız
`avatar_player` görseli YOKSA kuruluyor; görsel geldiği için o etiket hiç
doğmuyor. Ad ana ekranda zaten gösterilmiyor.

### HESAP SİLME — elle denendi, ÜÇ GÜVENCE DE ÇALIŞIYOR (2026-08-17)
DEVAM'da "derleniyor ama ELLE DENENMEDİ" diye duran madde kapandı.
Kayıt dosyası önce yedeklendi, test sonrası geri yüklendi (bölüm 11, 1320 jeton).
- İlk dokunuş yalnız KURUYOR: yazı "Tap again to erase everything", zemin
  kırmızı, jeton 1320 — hiçbir şey silinmiyor.
- İkinci dokunuş gerçekten siliyor: jeton 1320→0, açık bölüm 11→1, ad boşalıyor,
  ana ekrana dönülüyor.
- **5 saniye zaman aşımı çalışıyor:** t=164.5'te kuruldu, t=180.8'de ekran hâlâ
  açıkken yazı kendiliğinden "Delete My Account"a dönmüştü.
- **Ekrandan çıkmak da iptal ediyor** (`OnDisable`): kurup ekranı kapatıp
  açınca yazı geri dönüyor ve sonraki TEK dokunuş silmiyor, yeniden kuruyor.

### OYNANIŞ İÇİ DENETİM — başladı (2026-08-17)

Bugüne kadar mekanik denetim hep MENÜLERİ taramıştı. Oynanış tarafına ilk
bakış: HUD'daki bütün düğmelerin (yeniden başlat, duraklat, üç yardımcı,
panel düğmeleri) dinleyicisi var, ölü kontrol YOK. Yardımcı sistemi de
sağlam çıktı — elde yoksa jetonla satın alıyor, aynı düğmeye ikinci basış
seçimi iptal ediyor.

**Bir "yarım" geri bildirim bulundu ve düzeltildi.** Jetonu yetmeyen oyuncu
bir yardımcıya bastığında tek geri bildirim, yardımcı çubuğunun üstünde
beliren 28 puntoluk bir satırdı ("Not enough coins.", 2.5 sn). Oyuncunun
gözü o an bastığı DÜĞMEDE; ekranın başka yerinde beliren küçük bir yazıyı
çoğu zaman görmüyor ve "bastım, bozuk" diye okuyor.

`PowerUpSystem` artık ayrı bir `Refused(PowerUpKind)` olayı yayınlıyor;
`GameplayScreen` buna red sesi + titreşim + **reddedilen düğmenin sarı fiyat
kapsülünü sarsma** ile karşılık veriyor. Reddi reddedilen şeyin ÜSTÜNDE
göstermek gerekiyordu; sarsılan şeyin FİYAT olması sebebi de anlatıyor.

Doğrulama (play modunda, jeton 0): UFO'ya (elde 0, fiyat 1200) basınca
`Refused` **tam 1 kez**, kind=**Ufo**, `Pending` null, jeton değişmedi.
Elde roketi OLAN oyuncu roketе basınca `Refused` **0 kez**, `Pending=Rocket` —
yani red yalnız gerçekten reddedilende çalışıyor.

**Tuzak:** `Haptics.Threshold` varsayılan olarak `Medium`'dur ve altındaki
her şeyi sessizce yutar. `HapticStrength.Light` yazmak "titreşim ekledim"
sanıp hiç titretmemek olurdu.

### BÖLÜM ORTASINDA ÇIKIŞ — CİDDİ SIZINTI BULUNDU ve DÜZELTİLDİ (2026-08-17)

**Yardımcı istemi açıkken menüye dönen oyuncunun BİR SONRAKİ BÖLÜMÜ sakat
başlıyordu.** Ölçüm (yeniden girilen bölümde):

| | önce | sonra |
|---|---|---|
| HUD alfası | **0** (görünmez VE tıklanamaz) | 1 |
| yardımcı istemi paneli | **açık**, yeni tahtanın üstünde | kapalı |
| zemin karartması | **duruyor** | temiz |
| `PowerUps.Pending` | **Rocket** (asılı kalmış) | null |

**DERS (doğru temizlik, YANLIŞ kanca):** Bu temizliğin bir kısmı zaten
`GameplayScreen.OnDestroy` içindeydi ve yorumunda aynen şu yazıyordu:
*"Zemin karartması KALICI: istem açıkken bölüm bırakılırsa zemin karanlık
kalırdı."* Yani sorun GÖRÜLMÜŞ, çözüm yazılmış — ama hiç çalışmamış. Tek
sahnelik yapıda bu ekran **hiç yok edilmiyor**; `AppRoot.ShowMenu` yalnızca
oynanış kökünü kapatıyor, `OnDestroy` ancak uygulama kapanırken koşuyor.
Doğru kanca `OnDisable`. Bir temizliği yazmak yetmez, **çalıştığı anı da
doğrulamak** gerekir.

`Refresh` bunu kurtaramaz: durum geçişini görmesi için bir kare koşması
lazım, oysa kök aynı karede kapanıyor.

Artık `GameplayScreen.OnDisable` bütün geçici durumu sıfırlıyor (istem,
duraklat, sonuç, teklif panelleri; HUD ve yardımcı düğmelerinin alfası;
zemin karartması; `_shownState`, `_offerShown`, ipucu). `GameSession.StopLevel`
de bekleyen yardımcıyı iptal ediyor. Doğrulama: çıkışta dördü de temiz,
yeniden girilen bölümde HUD alfa 1, üç yardımcı düğmesi alfa 1 ve raycast
açık, dört panel de kapalı, `Pending` null.

**Yan bulgu — isim çakışması:** HUD'daki duraklat DÜĞMESİ de duraklat PANELİ
de "Pause" adını taşıyordu ve düğme hiyerarşide önce geliyordu. İsimle arayan
her teşhis "panel açık kalmış" diye yanlış okuyor (bu oturumda bir kez
düşüldü). Panel `PausePanel` olarak yeniden adlandırıldı.

### KALAN İKİ SENARYO — kodda zaten karşılanıyor

- **Sürükleme sırasında duraklatma / süre dolması.** `DragController`
  `() => State == GameState.Playing` yüklemini iki yerde birden okuyor:
  `OnPointerDown` sürüklemeyi hiç başlatmıyor, `OnPointerHeld` ise devam eden
  sürüklemeyi `EndDrag()` ile temiz bırakıyor
  (`DragController.cs:129`, yorumu da bunu söylüyor). Blok elde asılı kalmıyor.
- **Süre bitmişken yardımcı kullanmak.** Teklif paneli açılırken
  `SetOfferDim(true)` üç yardımcı düğmesinin `blocksRaycasts`'ini kapatıyor,
  yani basılamıyorlar. Hedef seçimi de mümkün değil: `BlockTapped` yolu aynı
  `_canDrag()` yükleminden geçiyor ve durum `Lost`.

### AÇIK KALAN (küçük)
Ayarlar'daki **Support / Terms / Privacy** düğmeleri `example.com` adreslerini
açıyor. Ölü değiller ama gittikleri yer yok. Portfolyo için doğrusu GitHub
depo sayfası olabilir — karar kullanıcının.

### MAĞAZA TAŞIYICISI — kontrol edildi, SORUN YOK
Referansta 8 saniye boyunca (7-15. sn, 4 kare/sn) teklif kartı ilk sayfada
kaldı, noktalar hiç değişmedi: **kendiliğinden dönmüyor.** Bizimki de
dönmüyor — burası zaten doğru, değişiklik gerekmedi.

### VİDEODAN ÇIKARILAMAYANLAR (mekanik)
Aşağıdakiler için referansta kimsenin o şeye DOKUNDUĞU bir an yok; video
bakarak cevaplanamaz, ancak oyunun kendisi kurulup denenirse bilinir:
- Kilitli bölgeye dokunmak bir şey yapıyor mu (bizde düğme gizli, hiçbir şey).
- Kilometre taşı kapsülleri tıklanabilir mi (bizde değil; bilgi taşıyorlar).

### HENÜZ BAKILMADI (mekanik)
- Oynanış İÇİNDEKİ mekanik denetim hiç yapılmadı: yardımcıların iptali,
  sürükleme sırasında duraklatma, süre bitince yardımcı kullanmak, bölüm
  ortasında ana ekrana dönüp geri gelmek.
- Mağazanın oynanışın ÜSTÜNE açılan bir kipi yok; referansta kaybetme
  panellerinden mağazaya gidilebiliyor (bkz. yukarıdaki bölüm).

### AÇIK MODEL SORUSU — panel EN-BOY oranı
Referans cihaz 2.165, bizim yakalama 1.78. Kart genişliği ekran ORANI olarak
verildiği için bizim kart göreli olarak daha geniş/bodur çıkıyor ve üstte
boşluk varmış gibi duruyor. Kart-göreli ölçüldüğünde yerleşim referansla
ÖRTÜŞÜYOR — yani hata yerleşimde değil, yakalama oranında. Yine de
referansın kendi ölçekleme modeli (yükseklik mi genişlik mi kilitli)
bilinmiyor; iki referans video da aynı oranda olduğu için videodan
çıkarılamıyor. Gerçek cihazda (2.0-2.2) bakılıp karar verilmeli.

### HENÜZ KIYASLANMAMIŞ (sıradaki iş)
- Oyun içi panellerden duraklat, PERFECT, yardımcı istemi ve BAŞARISIZ **BİTTİ**.
- **MEKANİK kıyas hiç yapılmadı**: Yolculuk neye basınca kayıyor, bölge
  çemberine dokunulunca ne oluyor, kilitli bölge tıklanabilir mi, sekmeler
  arası geçiş animasyonu var mı, mağaza taşıyıcısı otomatik dönüyor mu…

Referans kareler `C:/Users/CPN12/AppData/Local/Temp/claude/.../scratchpad/shop/j/`
altında; kaybolursa `menus,powerups,vs.mp4` içinden şu saniyelerden çıkarılır:
Mağaza 8-14 · Liderlik 16-22 · Yolculuk 24-42 · Koleksiyon 43-45 ·
Ayarlar 54-56 · Profil 57-60.
