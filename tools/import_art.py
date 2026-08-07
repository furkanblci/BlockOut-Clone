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
    # Bunları GÖZLE bulmak zor — koyu bir zemine koyana kadar fark edilmiyor;
    # satranç zemine bindirilirse üstteki satrançla karışıyor. Kontrol
    # yöntemi: çıktıyı düz koyu bir renge bindir.
    "icon_gear":      dict(holes=True),   # dişlinin göbeği
    "icon_clock":     dict(holes=True),   # iki zilin arası
    "icon_globe":     dict(holes=True),   # halka ile küre arası
    "icon_lock":      dict(holes=True),   # kilit dilinin içi
    "icon_shop":      dict(holes=True),   # tezgâhın arkası
    "icon_trophy":    dict(holes=True),   # kulpların içi
    "frame_board":    dict(holes=True),
    "banner_region":  dict(holes=True, size=640),

    # Konfeti parçaları tek tek küçük; varsayılan kırıntı eşiği onları atardı.
    "confetti_sheet": dict(size=1024, min_part=0.00005),

    # Tam ekran menü zemini: kesilecek bir arka planı yok, yalnız küçültülür.
    "bg_menu":        dict(key=False, size=1920),

    # --- İkinci parti (krem/sıcak gri zeminli) ---
    # Bu partinin zemini gri satranç değil KREM. Kroması ölçüldü: krem 13,
    # sıcak gri 24 — ilk partiye göre çok daha yüksek. neutral 18 bırakılsaydı
    # zeminin kendisi "renkli" sayılıp hiç silinmezdi.
    # Karakterlerin altındaki gri zemin gölgesi ortak ayarda kalıyordu ve köy
    # zemininin üstünde gri bir şerit gibi sırıtıyordu. Tolerans 95 onu da alıyor;
    # karakterler yüksek kromalı olduğu için (turkuaz/turuncu/yeşil) risk yok.
    "home_characters": dict(neutral=38, tolerance=95, size=1024),
    "avatar_player":   dict(neutral=26, size=1024, feather=1.2),
    "check_green":     dict(neutral=26, size=512),
    "region_1":        dict(neutral=26, size=1024),
    "region_2":        dict(neutral=26, size=1024),
    "region_3":        dict(neutral=26, size=1024),
    "region_4":        dict(neutral=26, size=1024),
    # Şeridin ortası oyuk: kapalı delik, kenardan taşma oraya ulaşamaz.
    "ribbon_reward":   dict(neutral=26, size=1024, holes=True),

    # --- Üçüncü parti (açık gri zeminli) ---
    # Çerçeve ve portre 1024'te tutuluyor: 512'ye küçülünce çerçevenin ince
    # kenarı iki-üç piksele düşüyor ve ekranda tırtıklı görünüyordu.
    "frame_avatar":    dict(neutral=20, size=1024, holes=True, min_part=0.004, feather=1.2),   # ortası oyuk
    # Kenar yumuşatma artırıldı: 512'ye küçülünce kesim kenarında
    # tırtıklanma görünüyordu.
    "btn_square":      dict(neutral=20, size=512, feather=1.4),
    # El: zemin KOYU LACIVERT olduğu için beyaz eldivenle karışmıyor.
    # İlk sürümde beyaz eldiven BEYAZ satranç zemindeydi ve kesim
    # matematiksel olarak imkânsızdı — nesne rengi zemin renklerinden
    # biriyle aynıysa taşma zeminden nesneye geçer.
    "icon_hand":       dict(neutral=0, tolerance=70, size=512),
    # Açılış görseli tam ekran: kesilecek arka planı yok.
    "splash_art":      dict(key=False, size=1920),
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
