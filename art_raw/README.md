# Ham görseller — buraya at

Gemini (ya da başka bir üretici) sana **arka planlı** bir PNG verdiğinde, dosyayı
düzeltmeye uğraşmadan olduğu gibi bu klasöre at. Arka planı ben temizliyorum.

## Çalıştırma

```bash
python tools/cutout.py art_raw -o "Assets/_Project/Art/UI"
```

Bu kadar. Klasördeki her PNG/JPG/WEBP işlenir, saydam ve 1024×1024 olarak
`Assets/_Project/Art/UI/` altına yazılır. Unity klasörü kendiliğinden yeniden
tarar (`Ctrl+R`).

## İsimlendirme

Çıktı dosyası, ham dosyayla **aynı adı** alır. Bu yüzden ham dosyayı
`docs/art-prompts.md` içinde geçen adla kaydet:

```
art_raw/btn_green.png   ->   Assets/_Project/Art/UI/btn_green.png
```

`Gemini_Generated_Image_kx82p.png` gibi bir adla atarsan çıktı da öyle olur ve
kod onu bulamaz.

## İş görmezse

Araç ne yaptığını her dosya için yazar: arka planın yüzdesi ve bulduğu zemin
renkleri. Buna bakarak ayar yapılır.

| Belirti | Sebep | Çözüm |
|---|---|---|
| `arka plan bulunamadı` | Nesne görüntünün kenarına değiyor, taşma başlayamıyor | Görseli yeniden üret, kenarda boşluk iste |
| Nesnenin **içi** delinmiş | Zemin rengi nesnenin içinde de var ve o bölge dışarıya bağlı | `--tolerance 22` |
| Zeminin bir kısmı kalmış | Zemin çok alacalı | `--tolerance 60` |
| Kenarda arka plan halkası | Yumuşatma dar kalmış | `--feather 1.4` |
| Nesne yenmiş, yalnız ortası kalmış | Yumuşak degrade nesnenin içine sızmış | `--local 0` |

Tek dosya üstünde denemek için doğrudan onu ver:

```bash
python tools/cutout.py art_raw/btn_green.png --tolerance 22
```

## Nasıl çalışıyor (kısaca)

Naif yöntem "arka planla aynı renkteki her pikseli sil" der; bu, yeşil butonun
içindeki beyaz artıyı da siler. Araç bunun yerine **görüntünün kenarından
başlayıp içeri doğru yayılır** ve yalnız birbirine değen benzer pikselleri
siler — nesnenin içinde kalan aynı renkli bölgelere ulaşamaz, dolayısıyla
onları bozmaz.

İki ek ayrıntı: zemin tek renk kabul edilmez (sahte saydamlık satrancı için
üç ayrı renk tohumu çıkarılır), ve kenardaki yarı saydam piksellerden zemin
rengi geri çekilir — yoksa nesnenin çevresinde ince bir beyaz hale kalır.
