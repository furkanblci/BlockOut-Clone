"""Ham ses dosyalarını oyunun beklediği kliplere çevirir.

`import_art.py`nin ses kardeşi. Ham dosyalar `audio_raw/` altında durur; bu
betik onları KESER, SEVİYELER ve doğru anahtar adıyla
`Assets/_Project/Audio/{SFX,Music}` içine yazar.

NEDEN GEREKLİ — indirilen ses dosyası oyuna hazır DEĞİLDİR:

1. BAŞTAKİ SESSİZLİK. Ölçüldü: `curtain.mp3` 479 ms, `bubble pop2.mp3`
   130 ms, `coin drop.mp3` 115 ms sessizlikle başlıyor. Oyunda bu doğrudan
   GECİKME demek — parmağını kaldırıyorsun, ses yarım saniye sonra geliyor
   ve oyun "tepki vermiyor" hissi bırakıyor. Dosya kısa diye sorun yok
   sanmak yanıltıcı: sorun uzunlukta değil, sesin NEREDE başladığında.

2. UZUN KUYRUK. `pop.mp3` 1,20 saniye ama ses 0,09'da bitiyor; gerisi
   sessizlik. `ice cracking.mp3` ise 6,2 saniyelik bir PERFORMANS — içinde
   12 ayrı çatlama var. Oyun her hamlede altı saniyelik bir dosya çalamaz;
   içinden tek bir çatlağı kesmek gerekiyor.

3. SEVİYE FARKI. Tepe değerleri -0,3 dB ile -19,3 dB arasında değişiyor.
   Aynı kodla çalınan iki ses arasında 19 dB fark, biri duyulmaz diğeri
   bağırır demek. Hepsi aynı tepeye getiriliyor; DENGE koddaki tek tek
   ses seviyelerinden veriliyor (bkz. AudioService).

DERS (bir ses dosyası bir SESİN kendisi değildir): İçindeki sesin nerede
başladığı, nerede bittiği ve ne kadar yüksek olduğu ayrı ayrı ölçülüp
düzeltilmezse, "sesler kötü" hissinin kaynağı sesin kendisi sanılır.

Kullanım:
    python tools/import_audio.py
    (sonra Unity'de: Tools > Block Out > Ses Kütüphanesini Yenile)
"""

import os
import subprocess
import sys

RAW = "audio_raw"
SFX_OUT = "Assets/_Project/Audio/SFX"
MUSIC_OUT = "Assets/_Project/Audio/Music"

SR = 44100
# Tepe hedefi: -3 dBFS. Sıfıra dayamak mp3 çözümünde kırpma riski yaratıyor,
# çok düşürmek ise kodda ses seviyesini 1'in üstüne çıkarmayı gerektirirdi.
PEAK_DB = -3.0


def ffmpeg():
    """Taşınabilir ffmpeg — sistemde kurulu olmasına güvenmiyoruz."""
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except ImportError:
        sys.exit("imageio-ffmpeg gerekli:  python -m pip install imageio-ffmpeg")


# (anahtar, kaynak, başlangıç sn, süre sn, perde çarpanı, ters mi)
#
# Perde çarpanı hem PERDEYİ hem HIZI değiştiriyor (asetrate). Bilinçli:
# gerçek dünyada da büyük bir nesne hem daha pes hem daha yavaş ses çıkarır;
# perdeyi hızdan ayırmak (rubberband) burada yapay duruyor.
#
# Kesim noktaları dalga biçimi ÖLÇÜLEREK seçildi — 10 ms'lik RMS penceresiyle
# her dosyanın başlangıcı, tepesi ve sönüş noktası bulundu.
SFX = [
    # --- oynanış çekirdeği ---
    # En sık duyulan ses: kısa, parlak, kuyruksuz olmalı.
    ("absorb",        "pop.mp3",                   0.020, 0.22, 1.00, False),
    # Soyulma emilmenin küçük kardeşi: aynı aile, bir tık tiz ve kısa.
    ("peel",          "bubble2.mp3",               0.030, 0.13, 1.15, False),
    # 6,2 sn'lik kayıttaki EN GÜÇLÜ çatlama 3,85'te (0 dB).
    ("ice_crack",     "ice cracking.mp3",          3.780, 0.62, 1.00, False),
    ("curtain_open",  "curtain.mp3",               0.470, 0.75, 1.00, False),
    ("win",           "level finished.mp3",        0.055, 1.35, 1.00, False),
    # KAYBETME SESİ YOKTU. Seviye atlama sesinin TERSİ, bir tık pes:
    # yükselen bir ezginin tersi alçalan bir ezgidir — "power-down".
    ("lose",          "tithuh-level-up-523624.mp3", 0.030, 0.95, 0.88, True),

    # --- olay vardı ses yoktu ---
    # Kullanıcı: tutma sesi bulunamadı, bubble pop kullanılsın.
    ("pick_up",       "bublble.mp3",               0.045, 0.10, 1.10, False),
    ("drop",          "bubble pop2.mp3",           0.115, 0.17, 0.92, False),
    # Aynı kayıttan DAHA KÜÇÜK bir çatlama (1,69 sn, -7,1 dB):
    # her hamlede duyulacak, kırılma anından zayıf kalmalı.
    ("ice_tick",      "ice cracking.mp3",          1.655, 0.26, 1.05, False),
    ("curtain_tick",  "bubble2.mp3",               0.030, 0.11, 0.75, False),
    # Kapı sesi de yoktu: renk değişimi bir "parıltı" ile anlatılıyor.
    ("gate_advance",  "sparkle.mp3",               0.200, 0.38, 1.10, False),
    ("gate_done",     "pop.mp3",                   0.020, 0.26, 0.75, False),
    ("block_spawn",   "bubble pop.mp3",            0.060, 0.24, 0.80, False),
    ("refuse",        "bublble.mp3",               0.045, 0.11, 0.65, False),
    # Tik tak kaydında bir tik: 0,58'deki güçlü olan (-1,8 dB).
    ("timer_warning", "ticking.mp3",               0.545, 0.14, 1.00, False),

    # --- yardımcılar ---
    # Süre donuyor: aynı tikler ama YAVAŞ ve pes — zamanın ağırlaşması.
    ("power_clock",   "ticking.mp3",               0.545, 1.10, 0.70, False),
    ("power_rocket",  "rocket.mp3",                0.130, 1.10, 1.00, False),
    ("power_ufo",     "ufo.mp3",                   0.040, 1.10, 1.00, False),

    # --- arayüz ---
    ("click",         "bubble2.mp3",               0.030, 0.08, 1.30, False),
    ("coin",          "coin drop.mp3",             0.100, 0.62, 1.00, False),
    ("star",          "sparkle.mp3",               0.200, 0.48, 1.00, False),
    # Panelin açılışı bir kumaş kayması: perdenin ilk anı.
    ("panel_open",    "curtain.mp3",               0.470, 0.32, 1.05, False),
    ("panel_close",   "curtain.mp3",               0.470, 0.26, 0.85, True),
    ("purchase",      "purchasesuccess.mp3",       0.020, 1.05, 1.00, False),
    ("reward_claim",  "tithuh-level-up-523624.mp3", 0.020, 1.10, 1.00, False),
    ("unlock",        "featureunlocked.mp3",       0.170, 2.30, 1.00, False),
]

# Kombo: tek kaynak, beş kademe. Yarım ses aralıklarla tizleşiyor —
# oyuncu zincirin uzadığını SAYMADAN duyuyor.
COMBO_SOURCE = ("pop.mp3", 0.020, 0.20)
COMBO_STEPS = [1.00, 1.09, 1.19, 1.30, 1.42]


def run(args):
    result = subprocess.run(args, capture_output=True)
    if result.returncode != 0:
        sys.stderr.write(result.stderr.decode("utf-8", "replace")[-2000:])
        raise SystemExit(f"ffmpeg hatası: {' '.join(args[:6])} …")


def probe(path):
    """Dosyanın GERÇEK süresini ve tepe değerini döndürür."""
    result = subprocess.run(
        [FF, "-hide_banner", "-i", path, "-af", "volumedetect", "-f", "null", "-"],
        capture_output=True)
    text = result.stderr.decode("utf-8", "replace")
    peak, dur = None, None
    for line in text.splitlines():
        if "max_volume:" in line:
            peak = float(line.split("max_volume:")[1].strip().split(" ")[0])
        if "Duration:" in line:
            hms = line.split("Duration:")[1].split(",")[0].strip()
            h, m, s = hms.split(":")
            dur = int(h) * 3600 + int(m) * 60 + float(s)
    return dur, peak


def finish(path):
    """İkinci geçiş: GERÇEK süreye göre sönüş + tepe seviyeleme.

    DERS (beklenen süre ile GERÇEK süre aynı şey değil): İlk sürümde sönüş,
    istenen kesim uzunluğundan hesaplanıyordu. Ama kaynak dosya o kadar uzun
    değilse ffmpeg elindeki kadarını yazıyor — `bublble.mp3` 0,10 saniye, biz
    0,045'ten itibaren 0,10 saniye istiyoruz, elimize 0,055 geçiyor. Sönüş
    dosyanın SONUNDAN öteye düşüyor, yani hiç uygulanmıyor ve ses tam
    tepesindeyken kesiliyor: hoparlörde TIK.

    Ölçüp uygulamak, hesaplayıp uygulamaktan farklı — ve burada tek doğru
    olan ölçmek.
    """
    dur, peak = probe(path)
    if dur is None:
        return
    fade = min(0.025, dur * 0.25)
    gain = 0.0 if peak is None else PEAK_DB - peak

    tmp = path + ".tmp.wav"
    chain = (f"afade=t=in:st=0:d={min(0.003, dur * 0.05):.4f},"
             f"afade=t=out:st={max(0.0, dur - fade):.4f}:d={fade:.4f},"
             f"volume={gain:.2f}dB")
    run([FF, "-v", "error", "-y", "-i", path, "-af", chain,
         "-ar", str(SR), "-c:a", "pcm_s16le", tmp])
    os.replace(tmp, path)


def cut(source, key, start, dur, pitch, reverse, out_dir):
    src = os.path.join(RAW, source)
    if not os.path.exists(src):
        print(f"  ATLANDI (kaynak yok): {source}")
        return False

    dst = os.path.join(out_dir, key + ".wav")
    chain = []
    if abs(pitch - 1.0) > 0.001:
        # Perde ve HIZ birlikte değişiyor (bkz. tablodaki not).
        chain.append(f"asetrate={int(SR * pitch)}")
        chain.append(f"aresample={SR}")
    if reverse:
        chain.append("areverse")
    chain.append("volume=0dB")

    run([FF, "-v", "error", "-y", "-ss", f"{start:.4f}", "-t", f"{dur:.4f}",
         "-i", src, "-af", ",".join(chain),
         "-ar", str(SR), "-ac", "1", "-c:a", "pcm_s16le", dst])
    finish(dst)
    return True


def music():
    """Arka plan müziği: sonundaki solma kesilir, döngü dikişi gizlenir.

    ÖLÇÜLDÜ: parça 54,9 saniye ve SON 3 SANİYEDE SÖNÜYOR (0,1 sn'lik RMS
    0,035 → 0,011). Ham hâliyle döngüye sokulursa oyuncu her 55 saniyede
    bir müziğin kaybolup aniden geri gelmesini duyar — döngü olduğu
    anlaşılır ve rahatsız eder.

    Çözüm: solan kuyruk atılıyor, sonra parçanın BAŞI kendi SONUNA
    çapraz geçişle bindiriliyor (`acrossfade`). Sonuç, sonu başına
    kusursuz bağlanan bir parça.

    Ayrıca parça sessiz: genel RMS 0,044 (-27 dB). `loudnorm` ile -16 LUFS'a
    çekiliyor; oyundaki 0,35'lik müzik seviyesi ancak o zaman anlamlı.
    """
    src = os.path.join(RAW, "bg music.mp3")
    if not os.path.exists(src):
        print("  ATLANDI: bg music.mp3 yok")
        return

    body = 51.5     # solmanın başladığı yerden önce
    xfade = 2.0     # döngü dikişinin uzunluğu

    tmp_head = os.path.join(MUSIC_OUT, "_head.wav")
    tmp_rest = os.path.join(MUSIC_OUT, "_rest.wav")
    run([FF, "-v", "error", "-y", "-t", f"{xfade}", "-i", src,
         "-ar", str(SR), "-ac", "2", "-c:a", "pcm_s16le", tmp_head])
    run([FF, "-v", "error", "-y", "-ss", f"{xfade}", "-t", f"{body - xfade}",
         "-i", src, "-ar", str(SR), "-ac", "2", "-c:a", "pcm_s16le", tmp_rest])

    dst = os.path.join(MUSIC_OUT, "gameplay.ogg")
    run([FF, "-v", "error", "-y", "-i", tmp_rest, "-i", tmp_head,
         "-filter_complex",
         f"[0][1]acrossfade=d={xfade}:c1=tri:c2=tri[x];"
         f"[x]loudnorm=I=-16:TP=-1.5:LRA=11[out]",
         "-map", "[out]", "-ar", str(SR), "-ac", "2",
         "-c:a", "libvorbis", "-q:a", "5", dst])

    os.remove(tmp_head)
    os.remove(tmp_rest)
    print(f"  müzik: {os.path.basename(dst)}")


if __name__ == "__main__":
    FF = ffmpeg()
    os.makedirs(SFX_OUT, exist_ok=True)
    os.makedirs(MUSIC_OUT, exist_ok=True)

    made = 0
    for key, source, start, dur, pitch, reverse in SFX:
        if cut(source, key, start, dur, pitch, reverse, SFX_OUT):
            made += 1
            print(f"  {key:15s} <- {source}")

    src, start, dur = COMBO_SOURCE
    for i, pitch in enumerate(COMBO_STEPS, start=1):
        if cut(src, f"combo_{i}", start, dur, pitch, False, SFX_OUT):
            made += 1
    print(f"  combo_1..5     <- {src}")

    music()
    print(f"\n{made} efekt yazıldı → {SFX_OUT}")
    print("Unity'de: Tools > Block Out > Ses Kütüphanesini Yenile")
