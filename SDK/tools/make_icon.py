"""Uygulama ikonunu ureten betik (4. tur, A1).

Ikon oyunun kendi gorsel dilinden cikiyor: mor zemin uzerinde LEGO benzeri
kabartmali tuglalar. Renkler `ColorPalette.asset` icindeki gercek blok
renkleri; boylece ikon ile oyun ayni paleti konusuyor.

Neden kodla: ikon tek bir dosya degil, ALTI boy (48..512) ve her boyda
kenarlarin temiz kalmasi gerekiyor. 4x supersampling ile tek kaynaktan
uretmek, her boyu elle cizmekten hem hizli hem tutarli.
"""
import io
import math
import os

from PIL import Image, ImageDraw, ImageFilter

OUT = r"c:\Projects\BlockOut! Clone\art_raw"
S = 4                      # supersampling
SIZE = 1024


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def background(size):
    """Dikey degrade + merkezde hafif aydinlanma."""
    top = (86, 62, 214)
    bottom = (36, 24, 104)
    img = Image.new("RGB", (size, size))
    d = ImageDraw.Draw(img)
    for y in range(size):
        d.line([(0, y), (size, y)], fill=lerp(top, bottom, y / size))

    # Merkez isigi: radyal beyaz maske, cok hafif.
    glow = Image.new("L", (size, size), 0)
    gd = ImageDraw.Draw(glow)
    r = int(size * 0.46)
    gd.ellipse([size // 2 - r, size // 2 - r, size // 2 + r, size // 2 + r], fill=70)
    glow = glow.filter(ImageFilter.GaussianBlur(size * 0.12))
    img = Image.composite(Image.new("RGB", (size, size), (150, 130, 255)), img, glow)
    return img


def brick(draw, x, y, w, h, color, cell):
    """Tek tugla: govde + koyu yan yuz + kabartmalar + acik ust kenar."""
    radius = int(cell * 0.20)
    side = int(cell * 0.17)          # yan yuz yuksekligi

    dark = tuple(int(c * 0.55) for c in color)
    light = tuple(min(255, int(c * 1.0 + 46)) for c in color)

    # Yan yuz (govdenin altinda kalan koyu bant)
    draw.rounded_rectangle([x, y + side, x + w, y + h + side], radius, fill=dark)
    # Ust yuz
    draw.rounded_rectangle([x, y, x + w, y + h], radius, fill=color)
    # Ust kenarda ince isik
    draw.rounded_rectangle(
        [x + int(cell * 0.09), y + int(cell * 0.07),
         x + w - int(cell * 0.09), y + int(cell * 0.30)],
        int(cell * 0.12), fill=light)

    # Kabartmalar: hucre basina 2x2
    cols = max(1, round(w / cell)) * 2
    rows = max(1, round(h / cell)) * 2
    stepx = w / cols
    stepy = h / rows
    sr = cell * 0.155
    for i in range(cols):
        for j in range(rows):
            cx = x + stepx * (i + 0.5)
            cy = y + stepy * (j + 0.5)
            draw.ellipse([cx - sr, cy - sr + cell * 0.045,
                          cx + sr, cy + sr + cell * 0.045], fill=dark)
            draw.ellipse([cx - sr, cy - sr, cx + sr, cy + sr], fill=color)
            draw.ellipse([cx - sr * 0.62, cy - sr * 0.72,
                          cx + sr * 0.62, cy - sr * 0.05], fill=light)


def build():
    size = SIZE * S
    img = background(size).convert("RGBA")
    draw = ImageDraw.Draw(img)

    # Paletten gercek blok renkleri (ColorPalette.asset)
    red = (230, 38, 51)
    blue = (38, 115, 242)
    yellow = (255, 191, 26)
    green = (51, 191, 64)

    cell = size * 0.235
    gap = cell * 0.10
    # Kompozisyon (3 hucre + 2 bosluk) kare; tam ortalanmasi icin sinirlayici
    # kutunun yarisi kadar geri aliniyor. Ilk denemede 2 hucrelik bir kutuya
    # gore ortalanmisti ve dizilim sola/yukari kaymisti.
    span = cell * 3 + gap * 2
    left = size * 0.5 - span * 0.5
    top = size * 0.5 - span * 0.5 - size * 0.015

    # Golge: dort tuglanin altinda tek yumusak leke.
    shadow = Image.new("L", (size, size), 0)
    sd = ImageDraw.Draw(shadow)
    sd.rounded_rectangle(
        [left - cell * 0.10, top + cell * 0.35,
         left + cell * 2 + gap + cell * 0.10, top + cell * 2 + gap + cell * 0.55],
        int(cell * 0.3), fill=150)
    shadow = shadow.filter(ImageFilter.GaussianBlur(cell * 0.18))
    img = Image.composite(Image.new("RGBA", (size, size), (10, 6, 40, 255)), img, shadow)
    draw = ImageDraw.Draw(img)

    # Cark seklinde dizilim: her tugla 1x2 ya da 2x1, ortada bosluk yok.
    brick(draw, left, top, cell * 2 + gap, cell, red, cell)
    brick(draw, left + cell + gap, top + cell + gap, cell, cell * 2 + gap, blue, cell)
    brick(draw, left, top + cell + gap, cell, cell * 2 + gap, yellow, cell)
    brick(draw, left + cell + gap, top + cell * 2 + gap * 2,
          cell * 2 + gap, cell, green, cell)

    img = img.convert("RGB").resize((SIZE, SIZE), Image.LANCZOS)
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, "icon_app.png")
    img.save(path)
    print("yazildi:", path, img.size)


build()
