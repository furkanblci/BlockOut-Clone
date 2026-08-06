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


def flood_mask(rgb: np.ndarray, seeds: np.ndarray,
               tolerance: float, local: float) -> np.ndarray:
    """
    Kenarlardan yayılan, arka plana benzeyen bağlı bölge. True = arka plan.

    İki kabul ölçütü var:
      1. Piksel bir arka plan tohumuna yakınsa (düz ya da satranç zemin).
      2. Ya da GELDİĞİ komşudan çok az farklıysa (degrade zemin).

    İkincisi degradeyi çözer: zemin piksel başına 1-2 birim değişir, `local`
    eşiği bunu rahat geçer. Nesnenin siluetiyse kenar yumuşatmayla bile piksel
    başına onlarca birim sıçrar — taşma oradan içeri giremez.
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


def process(path: pathlib.Path, out_dir: pathlib.Path, tolerance: float,
            trim: bool, size: int, feather: float, local: float) -> str:
    image = Image.open(path).convert("RGBA")
    rgb = np.array(image)[:, :, :3]

    seeds = background_seeds(rgb)
    mask = flood_mask(rgb, seeds, tolerance, local)

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
        # Kareye oturt: en uzun kenar hedefin %84'ü olsun, kalanı boşluk.
        target = int(size * 0.84)
        scale = target / max(result.width, result.height)
        result = result.resize(
            (max(1, round(result.width * scale)), max(1, round(result.height * scale))),
            Image.LANCZOS)

        canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        canvas.paste(result, ((size - result.width) // 2, (size - result.height) // 2), result)
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
    parser.add_argument("--size", type=int, default=1024, help="Kare çıktı boyutu; 0 = dokunma")
    parser.add_argument("--feather", type=float, default=0.8, help="Kenar yumuşatma yarıçapı")
    parser.add_argument("--local", type=float, default=8.0,
                        help="Degrade zemin için komşu toleransı; 0 = kapat")
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
            print(process(path, out_dir, args.tolerance, not args.no_trim,
                          args.size, args.feather, args.local))
        except Exception as error:                      # tek dosya patlarsa parti durmasın
            print(f"{path.name}: HATA {error}")

    return 0


if __name__ == "__main__":
    sys.exit(main())
