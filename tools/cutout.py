"""
Üretilen görsellerin arka planını temizler (Gemini vb. saydam veremediğinde).

Kullanım:
    python tools/cutout.py <girdi klasörü veya dosya> [-o çıktı klasörü]
                           [--tolerance 40] [--no-trim] [--size 1024]

Neden KENARDAN TAŞMA (flood fill), tek tek renk eşleme değil:
    "Arka planla aynı renkteki her pikseli sil" demek, yeşil butonun içindeki
    beyaz artı işaretini de silmek demektir. Bunun yerine yalnız GÖRÜNTÜNÜN
    KENARINDAN başlayıp birbirine değen benzer pikseller silinir; nesnenin
    içinde kalan aynı renkli bölgeler korunur.

Kenar yumuşatma:
    Sert bir maske, oyunda testere dişli kenar demek. Maske bir piksel
    bulanıklaştırılıp alfaya yediriliyor; ayrıca kenardaki pikselden arka plan
    rengi geri çekiliyor (renk saçağı / color fringe temizliği).
"""

import argparse
import pathlib
import sys
from collections import deque

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

Image.MAX_IMAGE_PIXELS = None       # üreticiler 2048+ veriyor, PIL'in DoS uyarısı bizi ilgilendirmiyor


def background_seeds(rgb: np.ndarray, count: int = 3) -> np.ndarray:
    """
    Kenar şeridinden arka plan renklerini çıkarır — TEK renk değil, en fazla üç.

    Neden birden çok: Gemini sık sık ya sahte bir "saydamlık satrancı" (iki
    gri kare) çiziyor ya da hafif degradeli bir zemin veriyor. Tek renge
    bakan bir taşma bunların ikisinde de yarı yolda duruyor. Kenar pikselleri
    kabaca gruplanıp her grubun ortalaması ayrı bir tohum yapılıyor.
    """
    h, w, _ = rgb.shape
    band = max(2, min(h, w) // 100)
    edges = np.concatenate([
        rgb[:band].reshape(-1, 3),
        rgb[-band:].reshape(-1, 3),
        rgb[:, :band].reshape(-1, 3),
        rgb[:, -band:].reshape(-1, 3),
    ]).astype(np.float32)

    seeds = []
    remaining = edges
    for _ in range(count):
        if len(remaining) < len(edges) * 0.06:      # kalanı gürültü, bırak
            break
        # En kalabalık renk kovası: 16'lık ızgarada en çok tekrar eden.
        buckets, counts = np.unique((remaining // 16).astype(np.int16), axis=0, return_counts=True)
        peak = buckets[counts.argmax()] * 16 + 8
        near = np.linalg.norm(remaining - peak, axis=1) <= 24
        seeds.append(remaining[near].mean(axis=0))
        remaining = remaining[~near]

    return np.array(seeds if seeds else [np.median(edges, axis=0)], dtype=np.float32)


def distance_to_seeds(rgb: np.ndarray, seeds: np.ndarray) -> np.ndarray:
    """Her pikselin EN YAKIN arka plan tohumuna uzaklığı."""
    diff = rgb.astype(np.float32)[:, :, None, :] - seeds[None, None, :, :]
    return np.linalg.norm(diff, axis=3).min(axis=2)


def background_test(rgb: np.ndarray, seeds: np.ndarray,
                    tolerance: float, neutral: float) -> np.ndarray:
    """
    Bir piksel arka plan rengi sayılır mı?

    `neutral` verilirse ikinci bir şart aranır: pikselin KROMASI (en yüksek ve
    en düşük kanal arasındaki fark) küçük olmalı. Stüdyo fonları gri tonlamadır
    — kroması sıfıra yakındır. Gri gövdeli bir kilit ise gri-MAVİdir; parlaklığı
    zeminle örtüşse bile kroması onu ele verir. Bu şart olmadan kilidi zeminden
    ayırmanın tek yolu toleransı düşürmek, o da zeminin yarısını bırakmak olur.
    """
    close = distance_to_seeds(rgb, seeds) <= tolerance
    if neutral > 0:
        channels = rgb.astype(np.int16)
        chroma = channels.max(axis=2) - channels.min(axis=2)
        close &= chroma <= neutral
    return close


def region_masks(rgb: np.ndarray, seeds: np.ndarray, tolerance: float,
                 neutral: float = 0.0, min_hole: float = 0.004):
    """
    Arka plan rengindeki bölgeleri tek seferde etiketler ve ikiye ayırır:
    görüntünün kenarına DEĞEN bölgeler (arka plan) ve değmeyip yeterince
    büyük olanlar (nesnenin içindeki delikler).

    Neden etiketleme: 2048x2048 bir görselde saf Python taşması dört milyon
    piksel gezer, dakikalar sürer. `label` aynı işi C tarafında yapar.

    Alan eşiği önemli: nesnenin üstündeki minik bir gri leke de arka plan
    rengine yakın olabilir ama delik sayılıp delinmemeli.
    """
    close = background_test(rgb, seeds, tolerance, neutral)
    labels, count = ndimage.label(close)
    if count == 0:
        empty = np.zeros(rgb.shape[:2], dtype=bool)
        return empty, empty

    touching = np.unique(np.concatenate([
        labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    touching = touching[touching > 0]

    background = np.isin(labels, touching)

    inner = close & ~background
    areas = np.bincount(labels[inner].ravel(), minlength=count + 1)
    big = np.nonzero(areas >= min_hole * rgb.shape[0] * rgb.shape[1])[0]
    holes = np.isin(labels, big[big > 0]) & ~background

    return background, holes


def despeckle(mask: np.ndarray, min_part: float) -> np.ndarray:
    """
    Arka plan maskesine, nesneden kopuk kalmış ufak adacıkları da katar.

    Satranç zeminin kareleri arasındaki kenar yumuşatma çizgileri iki grinin
    ara tonudur ve hiçbir tohuma yeterince yakın olmaz; geriye binlerce ufak
    kırıntı kalır. Tolerans yükseltmek çoğunu siler ama gri nesneleri de
    (kilit gibi) yemeye başlar. Bağımsız ve küçük her parçayı atmak, tolerans
    yükseltmeden aynı işi görür ve nesneye dokunmaz.
    """
    if min_part <= 0:
        return mask

    labels, count = ndimage.label(~mask)
    if count <= 1:
        return mask

    areas = np.bincount(labels.ravel(), minlength=count + 1)
    areas[0] = 0
    keep = areas >= min_part * mask.size
    keep[areas.argmax()] = True          # asıl nesne her hâlükârda kalsın
    return mask | ~keep[labels]


def flood_mask(rgb: np.ndarray, seeds: np.ndarray,
               tolerance: float, local: float) -> np.ndarray:
    """
    YAVAŞ yol (`--slow`): kenarlardan piksel piksel yayılan taşma.

    İki kabul ölçütü var:
      1. Piksel bir arka plan tohumuna yakınsa (düz ya da satranç zemin).
      2. Ya da GELDİĞİ komşudan çok az farklıysa (degrade zemin).

    İkincisi degradeyi çözer: zemin piksel başına 1-2 birim değişir, `local`
    eşiği bunu rahat geçer. Nesnenin siluetiyse kenar yumuşatmayla bile piksel
    başına onlarca birim sıçrar — taşma oradan içeri giremez. Etiketleme bu
    komşuluk ölçütünü ifade edemediği için degrade zeminlerde tek çare budur;
    büyük görsellerde yavaş olduğundan varsayılan değil.
    """
    h, w, _ = rgb.shape
    flat = rgb.astype(np.float32)
    close = distance_to_seeds(rgb, seeds) <= tolerance

    mask = np.zeros((h, w), dtype=bool)
    queue = deque()

    def push(y, x, from_color=None):
        if not (0 <= y < h and 0 <= x < w) or mask[y, x]:
            return
        if not close[y, x]:
            if from_color is None or local <= 0:
                return
            if np.linalg.norm(flat[y, x] - from_color) > local:
                return
        mask[y, x] = True
        queue.append((y, x))

    for x in range(w):
        push(0, x); push(h - 1, x)
    for y in range(h):
        push(y, 0); push(y, w - 1)

    while queue:
        y, x = queue.popleft()
        color = flat[y, x]
        push(y - 1, x, color); push(y + 1, x, color)
        push(y, x - 1, color); push(y, x + 1, color)

    return mask


def unfringe(rgb: np.ndarray, alpha: np.ndarray, seeds: np.ndarray) -> np.ndarray:
    """
    Yarı saydam kenar piksellerinden arka plan rengini geri çeker.

    Kenar pikseli aslında (nesne * a + arkaplan * (1-a)) karışımıdır; alfayı
    yazıp rengi olduğu gibi bırakmak, nesnenin çevresinde arka plan renginde
    ince bir hale bırakır. Karışımı tersine çeviriyoruz. Her piksel için
    kendi en yakın tohumu kullanılıyor — satranç zeminde tek ortalama renk
    kenarı gri yapardı.
    """
    diff = rgb.astype(np.float32)[:, :, None, :] - seeds[None, None, :, :]
    nearest = seeds[np.linalg.norm(diff, axis=3).argmin(axis=2)]

    a = np.clip(alpha, 1e-3, 1.0)[..., None]
    out = (rgb.astype(np.float32) - nearest * (1.0 - a)) / a
    return np.clip(out, 0, 255)


DEFAULTS = dict(tolerance=40.0, trim=True, size=512, feather=0.8, local=8.0,
                slow=False, holes=False, square=False, min_part=0.0005,
                neutral=0.0, key=True)


def process(path: pathlib.Path, out_dir: pathlib.Path, **overrides) -> str:
    """Tek dosyayı işler. Ayarlar DEFAULTS üstüne yazılır."""
    opt = dict(DEFAULTS, **overrides)
    tolerance, trim, size = opt["tolerance"], opt["trim"], opt["size"]
    feather, local, slow = opt["feather"], opt["local"], opt["slow"]
    holes, square, min_part = opt["holes"], opt["square"], opt["min_part"]
    neutral = opt["neutral"]

    image = Image.open(path).convert("RGBA")

    # Arka plan görselleri (menü zemini gibi) kesilmez, yalnız küçültülür.
    if not opt["key"]:
        result = image
        if size > 0 and max(result.size) > size:
            scale = size / max(result.size)
            result = result.resize((round(result.width * scale), round(result.height * scale)),
                                   Image.LANCZOS)
        out_dir.mkdir(parents=True, exist_ok=True)
        result.save(out_dir / (path.stem + ".png"))
        return f"{path.name} -> {path.stem}.png  (kesilmedi, {result.width}x{result.height})"

    rgb = np.array(image)[:, :, :3]

    seeds = background_seeds(rgb)
    if slow:
        mask = flood_mask(rgb, seeds, tolerance, local)
    else:
        mask, inner = region_masks(rgb, seeds, tolerance, neutral)
        if holes:
            mask |= inner

    mask = despeckle(mask, min_part)
    covered = mask.mean()
    if covered < 0.02:
        return f"{path.name}: arka plan bulunamadı (kenarlar nesneye değiyor olabilir) — atlandı"
    if covered > 0.98:
        return f"{path.name}: neredeyse tamamı arka plan sayıldı — tolerance'ı düşür"

    alpha = Image.fromarray(((~mask) * 255).astype(np.uint8))
    if feather > 0:
        alpha = alpha.filter(ImageFilter.GaussianBlur(feather))
    alpha_f = np.array(alpha).astype(np.float32) / 255.0

    rgb_clean = unfringe(rgb, alpha_f, seeds)
    result = Image.fromarray(
        np.dstack([rgb_clean.astype(np.uint8), (alpha_f * 255).astype(np.uint8)]), "RGBA")

    if trim:
        box = result.getbbox()
        if box:
            result = result.crop(box)

    if size > 0:
        # En uzun kenar `size` olacak şekilde, EN-BOY KORUNARAK küçültülür.
        # Kareye zorlamak 9-slice butonu ezerdi; Unity sprite'ın kendi
        # oranını zaten koruyor, dolayısıyla dolgu yalnız atlas israfı olur.
        scale = size / max(result.width, result.height)
        if scale < 1.0:
            result = result.resize(
                (max(1, round(result.width * scale)), max(1, round(result.height * scale))),
                Image.LANCZOS)

        if square:
            side = max(result.width, result.height)
            canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
            canvas.paste(result, ((side - result.width) // 2, (side - result.height) // 2), result)
            result = canvas

    out_dir.mkdir(parents=True, exist_ok=True)
    destination = out_dir / (path.stem + ".png")
    result.save(destination)
    palette = " ".join(f"#{int(s[0]):02X}{int(s[1]):02X}{int(s[2]):02X}" for s in seeds)
    return f"{path.name} -> {destination.name}  (arka plan %{covered * 100:.0f}, zemin {palette})"


def main() -> int:
    parser = argparse.ArgumentParser(description="Görsellerin arka planını temizler.")
    parser.add_argument("source", help="Dosya ya da klasör")
    parser.add_argument("-o", "--out", default=None, help="Çıktı klasörü (varsayılan: <kaynak>/cutout)")
    parser.add_argument("--tolerance", type=float, default=40.0, help="Renk toleransı (varsayılan 40)")
    parser.add_argument("--no-trim", action="store_true", help="Nesneyi kırpıp ortalama")
    parser.add_argument("--size", type=int, default=512, help="En uzun kenar; 0 = dokunma")
    parser.add_argument("--feather", type=float, default=0.8, help="Kenar yumuşatma yarıçapı")
    parser.add_argument("--local", type=float, default=8.0,
                        help="--slow ile: degrade zemin için komşu toleransı; 0 = kapat")
    parser.add_argument("--slow", action="store_true",
                        help="Degrade zeminler için piksel piksel taşma (yavaş)")
    parser.add_argument("--holes", action="store_true",
                        help="Nesnenin içindeki kapalı boşlukları da temizle (çerçeve, afiş)")
    parser.add_argument("--square", action="store_true", help="Çıktıyı kareye tamamla")
    parser.add_argument("--no-key", action="store_true",
                        help="Arka planı kesme, yalnız küçült (tam ekran zeminler için)")
    parser.add_argument("--neutral", type=float, default=0.0,
                        help="Zemin gri tonlamaysa: yalnız kroması bundan küçük pikseller silinir")
    parser.add_argument("--min-part", type=float, default=0.0005,
                        help="Bundan küçük kopuk parçalar atılır (alan oranı); 0 = kapat")
    args = parser.parse_args()

    source = pathlib.Path(args.source)
    files = ([source] if source.is_file()
             else sorted(p for p in source.iterdir()
                         if p.suffix.lower() in (".png", ".jpg", ".jpeg", ".webp")))
    if not files:
        print("Görsel bulunamadı:", source)
        return 1

    out_dir = pathlib.Path(args.out) if args.out else (
        source.parent if source.is_file() else source) / "cutout"

    for path in files:
        try:
            print(process(path, out_dir, tolerance=args.tolerance, trim=not args.no_trim,
                          size=args.size, feather=args.feather, local=args.local,
                          holes=args.holes, square=args.square, slow=args.slow,
                          min_part=args.min_part, neutral=args.neutral, key=not args.no_key))
        except Exception as error:                      # tek dosya patlarsa parti durmasın
            print(f"{path.name}: HATA {error}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
