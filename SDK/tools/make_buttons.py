"""
Arayüz düğmelerini Grand Games reçetesiyle prosedürel üretir.

    python tools/make_buttons.py [cikti_klasoru]

Neden prosedürel: düğme bir illüstrasyon değil, geometrik bir kaplama. Gradyan
durakları, kontur kalınlığı ve köşe yarıçapı sayı olarak durursa referansa
bakarak tek tek ayarlanabilir; ham görselden kesilirse ayarlanamaz.

Reçete Block Out! App Store görsellerinden (1320x2868 kayipsiz PNG) dikey
tarama ile ölçüldü; ölçümler docs/UI-DILI.md'de. Yapı üç renkte de aynı:

    dış açık halka -> kalın koyu kontur -> üstte parlak bant ->
    ortada en koyu -> altta hafif geri parlama -> alt koyu bant

Bizim eski düğmelerimizdeki büyük beyaz spekülar süpürme referansta YOK;
ayrışmanın en görünür sebebi oydu, bu yüzden reçetede yer almıyor.
"""
import sys, pathlib
from PIL import Image

SS = 4                                   # kenar yumuşatma için üst örnekleme

# --- ölçülen değerler --------------------------------------------------------
RIM     = (0x38, 0x33, 0x77)             # zeminden açık dış halka
OUTLINE = (0x12, 0x11, 0x33)             # kalın koyu kontur

# (konum, renk) — konum yüzün üstünden altına 0..1
FACES = {
    "purple": [(0.00,(0x5B,0x42,0xD1)), (0.12,(0xA7,0x8A,0xFF)),
               (0.16,(0x7B,0x68,0xFF)), (0.36,(0x62,0x50,0xFE)),
               (0.61,(0x59,0x45,0xF6)), (0.87,(0x67,0x51,0xFB)),
               (0.90,(0x3A,0x2B,0xAA)), (1.00,(0x29,0x1C,0x88))],
    "red":    [(0.00,(0x95,0x28,0x1B)), (0.13,(0xFE,0x77,0x5A)),
               (0.16,(0xED,0x35,0x2B)), (0.64,(0xD8,0x19,0x17)),
               (0.87,(0xE4,0x3C,0x32)), (0.89,(0x7C,0x00,0x01)),
               (1.00,(0x3D,0x02,0x06))],
    # Yeşil: booster kapsülü gömme halkalı, düğmeyle aynı yapıda değil.
    # Bu yüzden kırmızının profili yeşil tona taşındı; tepe ve ana renk
    # kapsülden ölçüldü (#C4FF67 tepe, #3BC32B ana).
    "green":  [(0.00,(0x02,0x6B,0x02)), (0.12,(0xC4,0xFF,0x67)),
               (0.16,(0x54,0xF8,0x3F)), (0.64,(0x3B,0xC3,0x2B)),
               (0.87,(0x48,0xE0,0x17)), (0.89,(0x0D,0x5A,0x06)),
               (1.00,(0x06,0x33,0x03))],
}

# Yatay iç gölge: sol/sağ kenarda yüz koyulaşıyor (yatay taramada ~15 px rampa).
SIDE_DARK  = 0.62                        # kenardaki çarpan
SIDE_WIDTH = 0.13                        # yarı genişliğin oranı


def lerp(a, b, t): return tuple(round(x + (y - x) * t) for x, y in zip(a, b))


def sample(stops, t):
    t = min(max(t, 0.0), 1.0)
    for i in range(len(stops) - 1):
        p0, c0 = stops[i]; p1, c1 = stops[i + 1]
        if t <= p1:
            span = p1 - p0
            return c0 if span <= 0 else lerp(c0, c1, (t - p0) / span)
    return stops[-1][1]


def sdf(px, py, hw, hh, r, n):
    """Yuvarlak dikdörtgenin işaretli mesafesi. n=2 daire köşe, n=4 squircle."""
    qx = max(abs(px) - (hw - r), 0.0)
    qy = max(abs(py) - (hh - r), 0.0)
    return (qx ** n + qy ** n) ** (1.0 / n) - r


def build(w, h, radius, stops, outline_px, rim_px, squircle=4.0):
    W, H = w * SS, h * SS
    hw, hh = W / 2.0, H / 2.0
    r  = radius * SS
    ot = outline_px * SS
    rt = rim_px * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    px = img.load()

    # yüzün dikey aralığı: konturun içi
    top, bot = -hh + ot, hh - ot
    face_h = bot - top

    for y in range(H):
        fy = y + 0.5 - hh
        t = (fy - top) / face_h
        col = sample(stops, t)
        for x in range(W):
            fx = x + 0.5 - hw
            d = sdf(fx, fy, hw - rt, hh - rt, r, squircle)
            if d > 0:                                   # dış halka bölgesi
                if d <= rt: px[x, y] = RIM + (255,)
                continue
            if d > -ot:                                 # kontur
                px[x, y] = OUTLINE + (255,)
                continue
            # yüz + yatay iç gölge
            edge = (-d - ot) / (min(hw, hh) * SIDE_WIDTH)
            k = SIDE_DARK + (1.0 - SIDE_DARK) * min(edge, 1.0)
            px[x, y] = tuple(round(c * k) for c in col) + (255,)

    return img.resize((w, h), Image.LANCZOS)


def main():
    if len(sys.argv) < 2:
        sys.exit(
            "Cikti klasoru ver. UYARI: btn_*.png su an OLU varlik: "
            "UiSkin.Art.Button* sabitleri hicbir yerden cagrilmiyor, "
            "arayuz UiSprites.RoundedPanel kullaniyor. Assets/ altina "
            "yazmadan once bkz. docs/DEVAM.md 'C' maddesi.")
    out = pathlib.Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)
    # kapsül düğmeler: 9-dilim kenarı {90,68,90,88} olan 512x246 ile uyumlu
    for name, stops in FACES.items():
        im = build(512, 246, radius=88, stops=stops, outline_px=8, rim_px=4)
        p = out / f"btn_{name}.png"; im.save(p); print(f"  {p}  {im.size}")
    # kare ikon düğmesi
    im = build(512, 512, radius=150, stops=FACES["purple"],
               outline_px=15, rim_px=7)
    p = out / "btn_square.png"; im.save(p); print(f"  {p}  {im.size}")


if __name__ == "__main__":
    main()
