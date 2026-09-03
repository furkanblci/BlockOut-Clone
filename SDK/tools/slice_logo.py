"""
`logo_game.png` görselini HARFLERİNE böler.

    python tools/slice_logo.py

Neden gerekli: Kazanma kutlamasında referans logoyu harf harf kuruyor —
önce küçücük bir "B" beliriyor, 0,10 saniye sonra "L", sonra tuğla "O",
"C", "K" ve en son alt satır "OUT!". Her harf sıfırdan büyüyüp son boyunu
AŞIYOR ve geri oturuyor. Bunun ön koşulu HARF BAŞINA AYRI GÖRSEL.

## Neden "kes ve dağıt" yetmiyor (ilk sürümün dersi)

İlk sürüm logoyu bir BÖLÜNTÜYE çeviriyordu: her piksel tek bir harfe
gidiyordu. Birleşik görüntü kusursuz çıkıyordu (fark: 0 piksel) ama
harfler TEK BAŞINA kötü görünüyordu:

- Bir harfin mor zemini, komşu harfin piksellerini İÇEREMEZ — çünkü onlar
  komşuya ait. Sonuç: her harfin zemininde komşusu şeklinde bir ısırık.
- Komşular arasındaki sınır mesafeye göre çizildiği için TIRTIKLI.
  Kullanıcı: "çok kesik kesik, fazla alınmış yerler, bazı yerler eksik
  alınmış."

Referanstaki harflerin her birinin KENDİ kapalı zemini var ve üst üste
biniyorlar; yani onlar bir bölüntü değil, ÜST ÜSTE BİNEN KATMANLAR.

## Bu sürümün yaptığı: iki katman

Her harf için İKİ görsel üretiliyor:

1. **Zemin** (`logo_b_back`): harfin çekirdeğinden yumuşakça şişirilerek
   elde edilmiş KAPALI mor blob. Komşu harflerin düştüğü yerler de mor —
   o mor, komşunun renginden değil, mor zeminin YAYILMASINDAN üretiliyor
   (aşağıdaki difüzyon notu).
2. **Harf** (`logo_b`): harfin kendisi ve kendi 3B gölgesi.

Oyunda önce BÜTÜN zeminler, sonra BÜTÜN harfler çiziliyor. Böylece:

- Tek başına gelen harfin kapalı bir zemini var (ısırık yok).
- Zeminler üst üste binse de hepsi aynı moru taşıdığı için fark etmiyor.
- Harf katmanı en üstte olduğu için birleşik görüntü aslından ayırt
  edilemiyor; betik bunu sayarak doğruluyor.

DERS (bölüntü mü, katman mı?): "Parçalar birleşince aslını versin" şartı
tek başına yetmiyor; parçanın TEK BAŞINA da doğru görünmesi gerekiyorsa
bölüntü yanlış araçtır. Üst üste binmeye izin vermek, hem birleşimi hem
tekil görünümü aynı anda doğru yapıyor.
"""

import pathlib
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(pathlib.Path(__file__).parent))
from cutout import dewisp                                    # noqa: E402

ROOT = pathlib.Path(__file__).parent.parent
SRC = ROOT / "art_raw" / "logo_game.png"
OUT = ROOT / "Assets" / "_Project" / "Art" / "UI"

# Sıra ÇİZİM SIRASI: soldaki harf sağdakinin ALTINDA kalıyor.
PARTS = ["logo_b", "logo_l", "logo_o", "logo_c", "logo_k", "logo_out"]

# İki satırın arası (kaynak görselde piksel). Dikey opaklık profili burada
# 456'dan 39'a düşüyor — yani "BLOCK" bitiyor, "OUT!" başlıyor.
LINE_SPLIT = 200

# Zeminin harften ne kadar dışarı taştığı.
#
# 34 ölçülerek seçildi: mor pikselin çekirdeğe uzaklığı medyanda 13, %90'ı
# 37'nin altında. Daha büyüğü harfin zeminini komşusunun yerine taşırıyor
# (tek başına gelen "B", "L"nin duracağı yere kadar uzanıyor); daha küçüğü
# zeminin dış kenarında boşluk bırakıyor.
GROW = 34

# Zeminin kenarını yumuşatan bulanıklık ve eşiği. Şişirmenin ham çıktısı
# harfin biçimini birebir izliyor (tırtıklı); bulanık + eşik onu yuvarlatıyor.
SMOOTH_SIGMA = 7.0
SMOOTH_LEVEL = 0.42

# Mor rengin boşluğa yayılma yarıçapı (difüzyon).
DIFFUSE_SIGMA = 18.0

# ZEMİNİN SİLUETTEN TAŞMA PAYI.
#
# Zemin önce tam olarak özgün siluete kırpılıyordu. Sonuç: logonun kenarına
# değen harflerde (özellikle tuğla "O"nun ÜST kenarı) mor kalmıyor ve harf
# tek başına geldiğinde düz kesilmiş görünüyordu. Kullanıcı: "üst kısım hâlâ
# kesiliyor."
#
# Referanstaki harflerin her birinin dört yanında da mor var; onlar özgün
# siluete kırpılmamış, ÜST ÜSTE BİNEREK siluete dönüşüyorlar.
#
# 10 piksel, 662 genişlikte %1,5: tek başına gelen harfe belirgin bir kenar
# veriyor, birleşik logonun dış hattını ise gözle fark edilmeyecek kadar
# kalınlaştırıyor.
#
# DERS (kırpma sınırı, hem birleşimi hem parçayı bağlar): Siluete tam
# kırpmak birleşimi kusursuz yapıyor ama parçayı sakatlıyor. İkisi aynı
# sayıyla ayarlanıyor; hangisinin daha çok göründüğüne bakıp seçmek gerek —
# burada parça, kelimenin kurulduğu bir saniye boyunca ekranın ortasında.
SILHOUETTE_BLEED = 10


def hue_sat_val(rgb):
    """0-255 RGB dizisinden (hue derece, doygunluk, parlaklık) üretir."""
    a = rgb.astype(float) / 255.0
    mx = a.max(2)
    mn = a.min(2)
    d = mx - mn + 1e-9
    r, g, b = a[:, :, 0], a[:, :, 1], a[:, :, 2]
    h = np.zeros_like(mx)
    i = mx == r
    h[i] = ((g - b)[i] / d[i]) % 6
    i = (mx == g) & (mx != r)
    h[i] = ((b - r)[i] / d[i]) + 2
    i = (mx == b) & (mx != r) & (mx != g)
    h[i] = ((r - g)[i] / d[i]) + 4
    return h * 60.0, d / (mx + 1e-9), mx


def clean_blobs(mask, floor=400, share=0.06):
    """Serpintileri atar, harfin GERÇEK parçalarını tutar.

    İki uçta da hata yapıldı, ikisi de ekranda görünüyordu:

    - "yalnız en büyük parça": ünlem işareti "OUT!" gövdesinden AYRI bir
      leke olduğu için elendi, en yakın çekirdek olan "K"ya düştü.
    - "400 pikselden büyük her parça": "L" harfinin ayağı altındaki koyu
      kırmızı gölge (601 piksel) "B" sanıldı.

    Ölçülen oranlar ayrımı net veriyor: ünlem noktası gövdenin %7,7'si,
    yabancı gölge ise %4,5'i. Eşik ikisinin arasında.
    """
    labels, count = ndimage.label(mask)
    if count == 0:
        return mask
    sizes = ndimage.sum(mask, labels, range(1, count + 1))
    limit = max(floor, share * sizes.max())
    keep = [i + 1 for i, size in enumerate(sizes) if size >= limit]
    return np.isin(labels, keep) if keep else mask


def disk(radius):
    r = int(np.ceil(radius))
    return np.hypot(*np.mgrid[-r:r + 1, -r:r + 1]) <= radius


def find_cores(data, opaque):
    """Her harfin doygun gövdesi. Sarı üç yerde geçtiği için konumla ayrılır."""
    hue, sat, val = hue_sat_val(data[:, :, :3])
    strong = opaque & (sat > 0.45) & (val > 0.45)
    height, width = opaque.shape
    yy, xx = np.mgrid[0:height, 0:width]
    upper = yy < LINE_SPLIT

    cores = {
        "logo_b": strong & ((hue > 335) | (hue < 16)) & upper & (xx < 260),
        # "L" ile tuğla "O" ARASINDAKİ SINIR DİK DEĞİL EĞİK.
        #
        # Tuğla döndürülmüş: sol kenarı (255,5)'ten (232,160)'a iniyor.
        # Dikey bir kesim (x < 252) tuğlanın sol alt köşesini "L"ye
        # veriyordu ve L tek başına gelirken sağ üstünde ilgisiz bir sarı
        # kırıntı taşıyordu. Sınır o kenarın kendisi: x = 255 − 0,144·y.
        "logo_l": strong & (hue >= 16) & (hue < 70) & upper & (xx < 255 - 0.144 * yy),
        "logo_o": strong & (hue >= 16) & (hue < 70) & upper & (xx >= 255 - 0.144 * yy),
        "logo_c": strong & (hue >= 70) & (hue < 165) & upper,
        "logo_k": strong & (hue >= 165) & (hue < 230) & upper,
        "logo_out": strong & (hue >= 16) & (hue < 70) & ~upper,
    }
    return {k: clean_blobs(v) for k, v in cores.items()}


def diffuse(data, purple):
    """
    Mor zeminin rengini, mor OLMAYAN yerlere yumuşakça yayar.

    Neden gerekiyor: Harflerin düştüğü yerleri de morla doldurmak gerek,
    yoksa zeminde ısırık kalır. İlk denemede o pikseller "en yakın mor
    pikselin rengi" ile dolduruldu; sonuç IŞIN IŞIN çıktı — büyük bir
    boşluğun ortasındaki her piksel farklı bir kenardan renk çekiyor ve
    aralarında keskin sınırlar oluşuyor.

    Normalleştirilmiş bulanıklık (ağırlıklı ortalama) sürekli bir alan
    üretiyor: her piksel, çevresindeki BÜTÜN mor piksellerin uzaklıkla
    ağırlıklandırılmış ortalaması. Morun üstten açık alttan koyu geçişi de
    böylece korunuyor.

    DERS (boşluk doldurmak bir ENTERPOLASYON işidir): "En yakını kopyala"
    ucuz ama süreksiz; aynı işi ağırlıklı ortalamayla yapmak tek satır
    farkla pürüzsüz sonuç veriyor.
    """
    source = data[:, :, :3].astype(float)
    mask = purple.astype(float)
    weight = ndimage.gaussian_filter(mask, DIFFUSE_SIGMA)
    channels = [ndimage.gaussian_filter(source[:, :, i] * mask, DIFFUSE_SIGMA)
                / np.maximum(weight, 1e-6) for i in range(3)]
    return np.clip(np.dstack(channels), 0, 255).astype(np.uint8)


def blend(canvas, layer):
    """Katmanı tuvale alfa harmanıyla basar (Unity'nin yaptığının aynısı)."""
    a = layer[:, :, 3:4].astype(float) / 255.0
    canvas[:, :, :3] = (layer[:, :, :3] * a + canvas[:, :, :3] * (1 - a)).astype(np.uint8)
    canvas[:, :, 3] = np.maximum(canvas[:, :, 3], layer[:, :, 3])


def main():
    if not SRC.exists():
        print(f"HATA: {SRC} yok.")
        return 1

    data = np.array(Image.open(SRC).convert("RGBA"))

    # KESİM ARTIĞI ÖNCE TEMİZLENİR.
    #
    # Ham logonun alt kenarının iki yanında, zeminin nesneye karışmış hâli
    # kalmış: donuk-açık, pembemsi tüyler. Kutlamada logo ekranın ortasında
    # ve büyük durduğu için bunlar gözle görülüyordu.
    #
    # `logo_game` normal görsel boru hattına GİRMİYOR: dosya zaten alfalı
    # geldiği için `cutout.process` onu "arka plan bulunamadı" diye atlıyor.
    # Temizlik burada yapılıyor ve TEMİZLENMİŞ tam logo da Assets'e
    # yazılıyor — hem harfler hem bütün logo aynı kaynaktan geliyor.
    alpha = dewisp(data[:, :, :3].astype(int), data[:, :, 3].astype(np.float32) / 255.0)
    removed = int(((data[:, :, 3] > 30) & (alpha <= 0.12)).sum())
    data[:, :, 3] = np.clip(alpha * 255.0, 0, 255).astype(np.uint8)
    print(f"kesim artığı temizlendi: {removed} piksel")
    Image.fromarray(data).save(OUT / "logo_game.png")

    # TUVALİ GENİŞLET.
    #
    # Logo kaynak görselin kenarlarına DEĞİYOR (opak kutu 0..661 × 0..398).
    # Zeminler siluetten taştığı için kenara değen harflerde taşma tuvalin
    # dışına düşüyor ve tam da düzeltmek istediğimiz yerde (tuğlanın üstü)
    # yeniden kesiliyordu. Önce pay eklenip her şey o boşlukta hesaplanıyor,
    # dikdörtgenler en sonda ÖZGÜN tuvale göre yeniden ifade ediliyor —
    # yani oyunda logonun boyu ve yeri değişmiyor.
    pad = SILHOUETTE_BLEED + 4
    source_height, source_width = data.shape[:2]
    data = np.pad(data, ((pad, pad), (pad, pad), (0, 0)))

    height, width = data.shape[:2]
    opaque = data[:, :, 3] > 96

    cores = find_cores(data, opaque)
    every_core = np.zeros((height, width), bool)
    for mask in cores.values():
        every_core |= mask

    # MOR TESTİNDE `kırmızı > yeşil` ŞARTI: onsuz camgöbeği "K" de mor
    # sayılıyor. Mor zemin (110,60,200) — kırmızı yeşilden fazla; "K"
    # (60,170,240) ise değil. İkisinde de mavi baskın, fark İKİNCİ kanalda.
    red = data[:, :, 0].astype(int)
    green = data[:, :, 1].astype(int)
    blue = data[:, :, 2].astype(int)
    purple = (opaque & ~every_core
              & (blue > red * 1.15) & (blue > green * 1.40) & (red > green))
    if not purple.any():
        print("HATA: mor kontur bölgesi bulunamadı.")
        return 1

    fill = diffuse(data, purple)

    # Harfin KENDİ gölgesi en yakın çekirdeğe göre paylaştırılıyor. Bu
    # bölüntü yalnız mor OLMAYAN pikseller için geçerli ve hepsi harflerin
    # dibinde durduğu için tırtıklı bir sınır ortaya çıkmıyor.
    seeds = np.zeros((height, width), np.int32)
    for index, name in enumerate(PARTS, start=1):
        seeds[cores[name]] = index
    _, (near_y, near_x) = ndimage.distance_transform_edt(seeds == 0, return_indices=True)
    owner = seeds[near_y, near_x]

    # Çekirdeklerden UZAK kalan uçlar (blobun alt eteği, sol/sağ lobları)
    # şişirmeyle yakalanamıyor; onlar en yakın harfe veriliyor.
    reach = ndimage.distance_transform_edt(~every_core)
    outskirts = opaque & (reach > GROW)
    grow_disk = disk(GROW)

    # Zeminlerin kırpıldığı alan: siluetin kendisi DEĞİL, biraz şişirilmişi.
    bleed_area = ndimage.binary_dilation(opaque, structure=disk(SILHOUETTE_BLEED))

    # HARF KATMANI: mor OLMAYAN pikseller en yakın çekirdeğe gidiyor, ama
    # ÇEKİRDEĞE DEĞMEYEN parçalar komşuya devrediliyor.
    #
    # "L" harfinin ayağının alt köşesi, öklit uzaklığına göre tuğlanın iri
    # çekirdeğine daha yakın düşüyordu; tuğla tek başına gelirken sol altında
    # ilgisiz bir sarı kırıntı taşıyordu. Parça sahibinin gövdesine hiç
    # değmiyorsa ona ait değildir.
    #
    # Devretmek ŞART, atmak değil: bu pikselleri hiçbir harf çizmezse zemin
    # onların yerine sentetik mor basıyor ve birleşik logo bozuluyor.
    owns = [opaque & ~purple & (owner == i) for i in range(1, len(PARTS) + 1)]
    for i, own in enumerate(owns):
        labels, count = ndimage.label(own)
        if count == 0:
            continue
        touching = set(np.unique(labels[ndimage.binary_dilation(cores[PARTS[i]])]))
        for piece in range(1, count + 1):
            if piece in touching:
                continue
            stray = labels == piece
            near = ndimage.binary_dilation(stray)
            best, target = 0, -1
            for j, other in enumerate(owns):
                if j == i:
                    continue
                overlap = int((near & (other | cores[PARTS[j]])).sum())
                if overlap > best:
                    best, target = overlap, j
            # Komşusu YOKSA yerinde bırak: sahipsiz kalan piksel, zemin
            # tarafından sentetik morla doldurulur ve logo bozulurdu.
            # (İlk sürümde bu şart yoktu ve komşusuz parçalar sırayla ilk
            # harfe düşüyordu — "B" harfinin kutusu logonun tamamına yayıldı.)
            if target < 0:
                continue
            owns[i] = owns[i] & ~stray
            owns[target] = owns[target] | stray

    print(f"{SRC.name}: {width}x{height}")
    print()

    # Zeminler İKİ GEÇİŞTE kuruluyor. Birinci geçiş yumuşatmayı yapıyor;
    # yumuşatma tanım gereği kenardan bir tık içeri çekiyor ve silüetin
    # uçlarında birkaç düzine piksel hiçbir zeminin içinde kalmıyor. İkinci
    # geçiş o artakalanı en yakın harfe ekliyor.
    #
    # DERS (yumuşatma her zaman biraz KIRPAR): Bir maskeyi bulanıklaştırıp
    # eşiklemek, dışbükey yerlerde daraltır. Kapsamı garanti etmek istiyorsan
    # yumuşatmadan SONRA bir onarım geçişi gerekiyor.
    shapes = []
    covered = np.zeros((height, width), bool)
    for index, name in enumerate(PARTS, start=1):
        blob = (ndimage.binary_dilation(cores[name], structure=grow_disk)
                | (outskirts & (owner == index)))
        blob = (ndimage.gaussian_filter(blob.astype(float), SMOOTH_SIGMA)
                > SMOOTH_LEVEL)
        # Kapama (şişir + aşındır): bulanıklık+eşik iç bükey yerlerde küçük
        # çentikler bırakıyor. Tuğlanın üst kenarında bunlardan biri siyah
        # bir ısırık olarak görünüyordu.
        blob = ndimage.binary_closing(blob, structure=disk(16)) & bleed_area
        shapes.append(blob)
        covered |= blob

    orphans = opaque & ~covered
    if orphans.any():
        for index in range(len(PARTS)):
            shapes[index] |= orphans & (owner == index + 1)
        covered |= orphans

    # Bütün zeminlerin BİRLEŞİMİ ve onun yumuşatılmış dış kenarı.
    # Parçalara bölmeden ÖNCE hesaplanıyor — gerekçe `back[:, :, 3]`
    # satırındaki notta.
    union = np.zeros(opaque.shape, bool)
    for shape in shapes:
        union |= shape
    silhouette_soft = np.clip(ndimage.gaussian_filter(
        union.astype(np.float32), 0.8) * 255.0, 0, 255).astype(np.uint8)

    layers = []
    for index, name in enumerate(PARTS, start=1):
        blob = shapes[index - 1]

        # Zeminde GERÇEK renk kalacak yerler: mor pikseller ve harfin KENDİ
        # gölgesi. Komşunun harfi ve gölgesi difüzyonla doldurulur — yoksa
        # tek başına gelen "L"nin sağında tuğlanın gölgesi asılı kalıyordu.
        real = purple | (opaque & ~purple & (owner == index))

        back = np.zeros_like(data)
        back[blob, 0:3] = fill[blob]
        keep = blob & real
        back[keep, 0:3] = data[keep, 0:3]
        # Siluetin İÇİNDE özgün alfa (dış kenarın yumuşaklığı korunuyor),
        # DIŞINDA tam opak — orada özgün alfa zaten sıfır.
        # ZEMİNİN ALFASI BİRLİK SİLUETİNDEN GELİYOR (12. tur, W1).
        #
        # Her zeminin kenarını ayrı ayrı yumuşatmak DİKİŞ üretiyordu: iki
        # komşu zeminin paylaştığı sınırda iki yarı-saydam kenar üst üste
        # binince ortada ince koyu bir çizgi kalıyor (ölçüldü: 452 piksel).
        #
        # Doğrusu, yumuşaklığı tek tek parçalardan değil BÜTÜNÜN dış
        # kenarından almak: `silhouette_soft` bütün zeminlerin birleşiminin
        # yumuşatılmış alfası. Parça o alfayı yalnız KENDİ alanında
        # kullanıyor; iç sınırlar ikili kalıyor ve parçalar boşluksuz
        # döşeniyor.
        #
        # DERS (yumuşaklık BÜTÜNE aittir, parçaya değil): Bir siluet
        # parçalara bölünüyorsa, dış kenarın yumuşaklığı bölünmeden ÖNCE
        # hesaplanmalı; her parçaya ayrı ayrı uygulanınca iç sınırlarda
        # olmayan bir boşluk icat ediliyor.
        back[:, :, 3] = np.where(blob, silhouette_soft, 0)

        # HARF, İKİLİ MASKEYLE DEĞİL YUMUŞAK AĞIRLIKLA KESİLİYOR (12. tur, W1).
        #
        # Eskiden tek satırdı: `letter[own] = data[own]`. `own` boolean
        # olduğu için her piksel ya tamamen kopyalanıyor ya tamamen
        # sıfırlanıyordu — yani kesim İKİLİ. Sonuç ölçüldü:
        #     logo_game (bütün logo)  yarı-saydam piksel 4562  (%2,4)
        #     logo_l                                       0   (%0,0)
        #     logo_c                                       0   (%0,0)
        #     logo_out                                     9   (%0,0)
        # Kullanıcının "block out yazısı çok kesik kesik" dediği şey buydu:
        # harflerin kenarında tek bir ara ton yok, merdiven basamağı gibi.
        #
        # Maske önce 3x3 genişletiliyor (özgün görselin yumuşak dış tüyünü
        # de kapsasın diye), sonra hafifçe bulanıklaştırılıp AĞIRLIK olarak
        # kullanılıyor ve ÖZGÜN ALFAYLA ÇARPILIYOR. Böylece:
        #   - logonun dış kenarındaki özgün yumuşaklık korunuyor,
        #   - iki harfin arasındaki kesim de sert kalmıyor.
        #
        # DERS (maske bir SEÇİM değil, bir AĞIRLIKTIR): "Bu piksel bu harfin
        # mi" sorusunun cevabı evet/hayır olduğunda kenar kaybolur. Aynı
        # soruyu "ne kadarı" diye sormak tek satır fark ediyor.
        letter = np.zeros_like(data)
        own = owns[index - 1]
        # SAHİPLİK KENARA KADAR GENİŞLETİLİYOR, ALFA BULANIKLAŞTIRILMIYOR.
        #
        # `own` maskesi `strong`dan türüyor ve `strong` yüksek alfa istiyor;
        # yani logonun DIŞ KENARINDAKİ yarı-saydam tüy hiçbir harfin sahipliğine
        # girmiyor ve tamamen atılıyordu. Sonuç: harflerde tek bir ara ton yok.
        #     logo_game %2,4 yarı-saydam  ->  logo_l %0,0
        #
        # İlk düzeltmede maskeyi bulanıklaştırıp alfayla çarptım; dış kenar
        # düzeldi ama İKİ HARFİN ARASINDAKİ sınır da yumuşadı ve orada iki
        # yarı-saydam kenar üst üste binince ince koyu DİKİŞLER çıktı.
        #
        # Doğrusu: sahipliği 1 piksel genişletmek ve alfayı ÖZGÜN hâliyle
        # almak. Dış kenarda `data`nın kendi yumuşaklığı geliyor; iç sınırda
        # komşu maskeler 1 piksel BİNİŞİYOR, yani boşluk da kalmıyor dikiş de.
        #
        # DERS (yumuşatma, yalnız DIŞ kenara aittir): Bir siluet parçalara
        # bölünüyorsa iç sınırlar sert kalmalı — orada zaten komşu var. Her
        # kenarı birden yumuşatmak, olmayan bir boşluğu görünür kılıyor.
        grown = ndimage.maximum_filter(own.astype(np.float32), size=3)
        weight = np.clip(ndimage.gaussian_filter(grown, 0.7), 0.0, 1.0)
        letter[:, :, 0:3] = data[:, :, 0:3]
        letter[:, :, 3] = np.clip(
            np.maximum(data[:, :, 3], 255) * weight, 0, 255).astype(np.uint8)

        layers.append((name, back, blob, letter, own))

    missing = int((opaque & ~covered).sum())
    if missing:
        print(f"UYARI: hiçbir zeminin kapsamadığı {missing} opak piksel var.")

    # Kutu, zemin ile harfin BİRLEŞİMİ: iki katman aynı dikdörtgeni
    # paylaşmalı ki oyunda tek ölçekle birlikte büyüsünler.
    print("        // ad          x0      y0      x1      y1")
    rects = []
    for name, back, blob, letter, own in layers:
        keep = blob | own
        ys, xs = np.nonzero(keep)
        x0, x1 = int(xs.min()), int(xs.max()) + 1
        y0, y1 = int(ys.min()), int(ys.max()) + 1

        Image.fromarray(back[y0:y1, x0:x1]).save(OUT / f"{name}_back.png")
        Image.fromarray(letter[y0:y1, x0:x1]).save(OUT / f"{name}.png")

        # Unity'nin arayüz dikdörtgeni SOL ALTTAN ölçüyor, görsel ise
        # SOL ÜSTTEN; y ekseni bu yüzden çevriliyor.
        # Özgün tuvale çevir: pay çıkarılıyor, oran özgün boyuta göre.
        # 0'ın altına veya 1'in üstüne taşan değerler normal — zemin
        # logonun dış hattından biraz dışarı çıkıyor.
        rects.append((name,
                      (x0 - pad) / source_width,
                      1.0 - (y1 - pad) / source_height,
                      (x1 - pad) / source_width,
                      1.0 - (y0 - pad) / source_height))
        print("        // %-9s %6.4f  %6.4f  %6.4f  %6.4f   (%dx%d px)"
              % (name, rects[-1][1], rects[-1][2], rects[-1][3], rects[-1][4],
                 x1 - x0, y1 - y0))

    print()
    print("C# tablosu:")
    for name, a, b, c, e in rects:
        print('            new LetterRect("%s", %.4ff, %.4ff, %.4ff, %.4ff),'
              % (name, a, b, c, e))

    # BİRLEŞTİRME DOĞRULAMASI: önce bütün zeminler, sonra bütün harfler —
    # oyundaki çizim sırasının aynısı.
    #
    # Doğrulama YALNIZ tam opak piksellerde: silüetin dış kenarındaki yarı
    # saydam şerit burada hep "farklı" çıkıyor, ama bu kusur değil ÖLÇÜM
    # YÖNTEMİNİN kendi hatası — katmanlar boş (siyah) bir tuvale harmanlanıyor.
    canvas = np.zeros_like(data)
    for _, back, _, _, _ in layers:
        blend(canvas, back)
    for _, _, _, letter, _ in layers:
        blend(canvas, letter)

    diff = np.abs(canvas[:, :, :3].astype(int) - data[:, :, :3].astype(int))
    solid = data[:, :, 3] > 250
    bad = int(((diff.max(2) > 12) & solid).sum())
    added = int(((canvas[:, :, 3] > 128) & (data[:, :, 3] <= 128)).sum())
    print()
    print(f"birleştirme farkı (tam opak): {bad} piksel "
          f"({bad / max(1, solid.sum()) * 100:.3f}%)")
    print(f"siluetin dışına eklenen mor: {added} piksel "
          f"(kasıtlı — bkz. SILHOUETTE_BLEED)")

    Image.fromarray(canvas).save(ROOT / "art_raw" / "logo_parts_check.png")
    return 0


if __name__ == "__main__":
    sys.exit(main())
