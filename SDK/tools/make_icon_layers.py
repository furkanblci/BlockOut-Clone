"""Uyarlanabilir (adaptive) ikon katmanlarini uretir.

Android 8+ ikonu IKI katman ister: arka plan ve on plan. Sistem ikisini
kendi maskesiyle (daire, kare, damla...) kirpiyor, bu yuzden on plandaki
icerik gorselin ORTA %66'sinda kalmali; disarisi her cihazda kirpilabilir.

Ayni kaynagi kullanip yalniz olcegi degistirmek, uc dosyanin da birbirinden
kaymasini imkansiz kiliyor.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw, ImageFilter

import make_icon as base

OUT = base.OUT
SIZE = base.SIZE
S = base.S


def build_layers():
    size = SIZE * S

    # --- arka plan katmani -------------------------------------------------
    bg = base.background(size).convert("RGB")
    bg.resize((SIZE, SIZE), Image.LANCZOS).save(os.path.join(OUT, "icon_app_bg.png"))

    # --- on plan katmani ---------------------------------------------------
    fg = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(fg)

    red = (230, 38, 51)
    blue = (38, 115, 242)
    yellow = (255, 191, 26)
    green = (51, 191, 64)

    # Guvenli alan: on plan icerigi gorselin orta %66'sinda kalmali.
    cell = size * 0.155
    gap = cell * 0.10
    span = cell * 3 + gap * 2
    left = size * 0.5 - span * 0.5
    top = size * 0.5 - span * 0.5

    shadow = Image.new("L", (size, size), 0)
    sd = ImageDraw.Draw(shadow)
    sd.rounded_rectangle(
        [left - cell * 0.10, top + cell * 0.35,
         left + span + cell * 0.10, top + span + cell * 0.35],
        int(cell * 0.3), fill=130)
    shadow = shadow.filter(ImageFilter.GaussianBlur(cell * 0.20))
    fg = Image.composite(Image.new("RGBA", (size, size), (10, 6, 40, 170)), fg, shadow)
    draw = ImageDraw.Draw(fg)

    base.brick(draw, left, top, cell * 2 + gap, cell, red, cell)
    base.brick(draw, left + cell + gap, top + cell + gap, cell, cell * 2 + gap, blue, cell)
    base.brick(draw, left, top + cell + gap, cell, cell * 2 + gap, yellow, cell)
    base.brick(draw, left + cell + gap, top + cell * 2 + gap * 2,
               cell * 2 + gap, cell, green, cell)

    fg.resize((SIZE, SIZE), Image.LANCZOS).save(os.path.join(OUT, "icon_app_fg.png"))
    print("yazildi: icon_app_bg.png, icon_app_fg.png")


build_layers()
