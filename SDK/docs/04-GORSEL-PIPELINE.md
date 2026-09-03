# Görsel pipeline — üretimden oyuna

Bu proje görsellerini dışarıda (Gemini vb.) üretip projeye aktardı. Zincirin
her halkasında öğrenilmiş bir şey var.

```
prompt  →  üreteç  →  art_raw/  →  cutout.py  →  Art/UI/  →  UiSpriteImporter  →  UiSkin
           (PNG)      (ham)        (saydamlık)   (temiz)     (içe aktarma)        (kayıt)
```

---

## 1. Prompt listesi — stil DNA'sı başta

Her promptun başına `03-UI-DILI.md` §4'teki stil paragrafını koy. Aksi hâlde
her ikon farklı bir dünyadan gelir ve arayüz dağınık durur.

**Prompt'un GARANTİ etmesi gereken tek şey:** nesne görüntünün kenarına
**değmemeli**. Saydamlık kesme kenardan taşarak çalışıyor; nesne kenara
değiyorsa taşma başlayamaz ve kesim tamamen başarısız olur.

Diğer kurallar:
- Tek nesne, ortalanmış, düz zemin
- Gölge nesnenin altında, kenara uzanmasın
- 1024×1024 kare

---

## 2. Saydamlık — `tools/cutout.py`

Üreteçlerin çoğu saydam PNG vermez. Bu araç arka planı keser.

```bash
python tools/cutout.py art_raw/ -o Assets/_Project/Art/UI/ --tolerance 40
```

**Neden kenardan taşma (flood fill), tek tek renk eşleme değil:** "arka planla
aynı renkteki her pikseli sil" demek, yeşil düğmenin içindeki beyaz artıyı da
silmek demektir. Yalnız görüntünün **kenarından** başlayıp birbirine değen
benzer pikseller siliniyor; nesnenin içinde kalan aynı renkli bölgeler
korunuyor.

**Üç zorluk ve çözümleri:**

| Sorun | Çözüm |
|---|---|
| Sahte saydamlık (gri dama tahtası piksele pişmiş) | Çok tohumlu taşma — kenar şeridinden **en fazla üç** arka plan rengi çıkarılıyor |
| Degradeli zemin | Komşu toleransı — mutlak renk yerine komşuyla fark |
| Testere dişli kenar | Maske bir piksel bulanıklaştırılıp alfaya yediriliyor; kenar pikselinden arka plan rengi geri çekiliyor (renk saçağı temizliği) |
| Kapalı iç delikler (saatin içi, kilit kulpu) | `--holes` bayrağı |

### Doğrulama — dama tahtası ÜSTÜNDE bakma

Kesim sonucunu **düz koyu bir renk** üstüne bindirerek kontrol et. Dama
tahtası önizlemesi üstünde, artık kalan sahte dama tahtası gerçek saydamlıktan
ayırt edilemez ve bu projede **beş ikonu bir saat boyunca sakladı.**

Sayısal kontrol: nötre yakın (kroma ≤ 12), orta parlaklıkta (40–155),
görüntünün %0,1'inden büyük bölge oluşturan opak pikseller ≈ artık dama tahtası.
`tools/check_art.py` bunu yapıyor.

---

## 3. İçe aktarma — `UiSpriteImporter`

`Art/UI/` altına düşen her görseli doğru ayarlarla alır (sprite tipi, filtre,
sıkıştırma, mipmap kapalı, pivot). Elle ayar yapmayı bırak — bir dosyayı
unuttuğunda sebebi haftalar sonra "şu ikon biraz bulanık" diye ortaya çıkar.

## 4. Kayıt defteri — `UiSkin`

**Resources tuzağı:** sprite'lar `Art/UI/` altında ve orası Resources klasörü
DEĞİL — çalışma anında `Resources.Load` ile bulunamazlar.

İlk akla gelen çözüm hepsini Resources'a taşımak; ama Resources'taki her şey,
kullanılsın kullanılmasın, derlemeye girer ve açılışta indekslenir.

**Yapılan:** Resources'a yalnızca küçük bir `UiSkin` asset'i konuyor.
Sprite'lara referans verdiği için hepsi derlemeye giriyor, klasör düzeni
bozulmuyor ve neyin kullanıldığı tek yerden görünüyor. Asset elle
doldurulmuyor — kurulum aracı klasörü tarayıp dosya adına göre yazıyor.

**Önbellek tuzağı:** `UiSkin` arama tablosu ScriptableObject örneğinde
saklanıyor ve düzenleme ile oyun modu arasında yaşamaya devam ediyor. Yeni
sprite eklendiğinde `SetEntries` tabloyu **sıfırlamazsa** yeni görseller "yok"
görünür. Bu projede avatar çerçevesi ve kare düğme, dosyalar yerindeyken yedek
çizime düştü.

---

## 5. Prosedürel yedek — asset beklemeden çalış

`UiSkin.Get` bulamazsa `UiSprites` / `MenuSprites` prosedürel çizime düşer.
Yani **görsel gelmeden önce de** arayüz çalışır ve doğru ölçülerdedir.

Bu, görsel üretimini bir engel olmaktan çıkarıyor: önce arayüzü kur ve ölç,
görselleri sonra tek tek yerine oturt.

> Prosedürel maske yazarken `Mathf.SmoothStep` tuzağına dikkat —
> `02-TUZAKLAR.md` §9.

---

## 6. Uygulama ikonu

```bash
python tools/make_icon.py            # ana ikon
python tools/make_icon_layers.py     # Android uyarlanabilir ikon katmanları
```

`AppIconTool` üretilen dosyaları projeye alıp Android ikon yuvalarına bağlar.

**Neden araçla:** elle bağlanan bir ikon, oyuncu ayarlarını sıfırlayan ilk
kişide kaybolur. İkon da ayarların parçasıdır ve kodla bağlanmalıdır.
