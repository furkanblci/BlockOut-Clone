"""
Kesilen görsellerde HASAR arar.

    python tools/check_art.py

Neden gerekli: `import_art.py` sessizce çalışır ve "oldu" der. Ama kesim
nesnenin kendisinden parça koparabilir — karakterin omzundan, ikonun
kenarından. Bu hata gözle ancak yakınlaştırıp bakınca görülür ve otuz altı
görselde her seferinde bakmak mümkün değil.

Ölçüt: ham görselde arka plan renginden BELİRGİN uzak (yani kesinlikle
nesneye ait) olduğu hâlde çıktıda saydam olan pikseller. Zeminin kendisi bu
ölçüte hiç girmez, dolayısıyla yanlış alarm üretmez. Yer gölgesi gibi
kasıtlı silinen alanlar için eşik payı bırakılır.
"""

import pathlib
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(pathlib.Path(__file__).parent))
import cutout                                              # noqa: E402
import import_art                                          # noqa: E402

Image.MAX_IMAGE_PIXELS = None

# Kesin nesne sayılmak için tohumdan en az bu kadar uzak olmalı.
#
# 45 denendi ve yanlış alarm yağdırdı: yumuşak gölgeler, karlı bir sahnenin
# beyazı, kesilerek biten bir portrenin alt kenarı — hepsi "kayıp" sayıldı.
# 70, zeminden AÇIKÇA farklı olan pikselleri seçiyor; bu eşiğin üstünde
# silinen bir piksel gerçekten hatadır.
CERTAIN = 70.0

# Bu kadar pikselden büyük bir kayıp bildirilir. Daha küçükleri kenar
# yumuşatmasının payıdır ve gözle görülmez.
REPORT = 400


def check(name: str) -> str:
    raw_path = import_art.RAW / f"{name}.png"
    cut_path = import_art.OUT / f"{name}.png"
    if not raw_path.exists() or not cut_path.exists():
        return f"{name}: dosya eksik"

    settings = dict(import_art.COMMON, **import_art.RECIPES.get(name, {}))
    if not settings.get("key", True):
        return f"{name}: kesilmiyor, atlandı"

    raw = np.array(Image.open(raw_path).convert("RGB"))
    seeds = cutout.background_seeds(raw)
    background, _ = cutout.region_masks(
        raw, seeds, settings["tolerance"],
        settings.get("neutral", 0.0), settings.get("bright", 0.0))

    certain = cutout.distance_to_seeds(raw, seeds) > CERTAIN
    lost = background & certain
    if not lost.any():
        return f"{name}: temiz"

    labels, count = ndimage.label(lost)
    areas = np.bincount(labels.ravel())[1:]
    big = np.nonzero(areas >= REPORT)[0]
    if len(big) == 0:
        return f"{name}: temiz"

    # Yer gölgesi hep en ALT şeritte olur; onu hasar saymıyoruz.
    height = raw.shape[0]
    real = []
    for i in big:
        ys = np.nonzero((labels == i + 1).any(axis=1))[0]
        if ys.min() > height * 0.88:      # yalnız alt %12'de → gölge
            continue
        real.append((int(areas[i]), int(ys.min()), int(ys.max())))

    if not real:
        return f"{name}: temiz (yalnız yer gölgesi silindi)"

    real.sort(reverse=True)
    parts = ", ".join(f"{a} px (y{y0}-{y1})" for a, y0, y1 in real[:3])
    return f"{name}: >>> HASAR — {parts}"


def main() -> int:
    names = sorted(p.stem for p in import_art.RAW.glob("*.png"))
    damaged = 0
    for name in names:
        line = check(name)
        print(line)
        if "HASAR" in line:
            damaged += 1

    print(f"\n{len(names)} görsel, {damaged} hasarlı.")
    return 1 if damaged else 0


if __name__ == "__main__":
    sys.exit(main())
