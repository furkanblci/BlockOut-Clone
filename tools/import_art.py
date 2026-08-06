"""
Ham görselleri (art_raw/) oyunun kullandığı sprite'lara çevirir.

    python tools/import_art.py

Neden ayrı bir sürücü: 24 görselin hepsi aynı ayarla işlenmiyor. Dişlinin
ortasındaki delik delinmeli, menü zemini hiç kesilmemeli, konfeti sayfasındaki
küçük parçalar kırıntı sanılıp atılmamalı. Bu bilgi komut satırı geçmişinde
kalırsa altı ay sonra kimse hangi görselin neden farklı olduğunu bilemez.
Tablo hâlinde burada durursa yeniden üretmek tek komut.

Ortak ayarların gerekçesi:
    tolerance 60  Zemin satranç deseni; iki gri arasındaki kenar yumuşatma
                  tonları 47 birim uzakta kalıyor, 40 onları bırakıyordu.
    neutral 18    Zemin gri tonlama (kroma <= 14 ölçüldü), nesneler renkli.
                  Bu şart olmadan tolerance 60 kapının koyu kahvesini ve
                  kupanın ahşap kaidesini de siliyordu.
"""

import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent))
from cutout import process                                    # noqa: E402

RAW = pathlib.Path(__file__).parent.parent / "art_raw"
OUT = pathlib.Path(__file__).parent.parent / "Assets/_Project/Art/UI"

COMMON = dict(tolerance=60, neutral=18, size=512)

# Yalnız ortaktan SAPAN dosyalar yazılır; gerisi COMMON ile işlenir.
RECIPES = {
    # Ortası delik: kenardan taşma oraya ulaşamaz, ayrıca delinmeli.
    "icon_gear":      dict(holes=True),
    "frame_board":    dict(holes=True),
    "banner_region":  dict(holes=True, size=640),

    # Konfeti parçaları tek tek küçük; varsayılan kırıntı eşiği onları atardı.
    "confetti_sheet": dict(size=1024, min_part=0.00005),

    # Tam ekran menü zemini: kesilecek bir arka planı yok, yalnız küçültülür.
    "bg_menu":        dict(key=False, size=1920),
}


def main() -> int:
    files = sorted(p for p in RAW.rglob("*.png") if p.parent.name != "cutout")
    if not files:
        print("art_raw/ boş.")
        return 1

    for path in files:
        settings = dict(COMMON, **RECIPES.get(path.stem, {}))
        try:
            print(process(path, OUT, **settings))
        except Exception as error:
            print(f"{path.name}: HATA {error}")

    print(f"\n{len(files)} görsel -> {OUT}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
