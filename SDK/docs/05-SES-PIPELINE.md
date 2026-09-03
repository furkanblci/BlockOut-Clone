# Ses pipeline

```
audio_raw/  →  import_audio.py  →  Audio/{SFX,Music}/  →  AudioSkin  →  AudioService
(ham dosya)    (kes, seviyele)     (anahtar adıyla)      (kayıt)       (palet + çalar)
```

---

## 1. İndirilen ses dosyası oyuna HAZIR DEĞİLDİR

Bu, en çok küçümsenen adım. `import_audio.py` üç şeyi düzeltiyor ve üçü de
ölçülerek bulundu:

### Baştaki sessizlik → gecikme

Ölçüldü: `curtain.mp3` **479 ms**, `bubble pop2.mp3` 130 ms, `coin drop.mp3`
115 ms sessizlikle başlıyor.

Oyunda bu doğrudan gecikme demek — parmağını kaldırıyorsun, ses yarım saniye
sonra geliyor ve oyun "tepki vermiyor" hissi bırakıyor.

> Dosya kısa diye sorun yok sanmak yanıltıcı: sorun uzunlukta değil, sesin
> **nerede başladığında.**

### Uzun kuyruk → çalınamaz dosya

`pop.mp3` 1,20 saniye ama ses 0,09'da bitiyor; gerisi sessizlik.
`ice cracking.mp3` ise 6,2 saniyelik bir **performans** — içinde 12 ayrı
çatlama var. Oyun her hamlede altı saniyelik dosya çalamaz; içinden tek bir
çatlağı kesmek gerekiyor.

### Seviye farkı → biri duyulmaz, diğeri bağırır

Tepe değerleri **-0,3 dB ile -19,3 dB** arasında değişiyordu. Hepsi aynı tepeye
getiriliyor; **denge** koddaki tek tek ses seviyelerinden veriliyor
(`AudioService`'teki `0.26f`, `0.55f` gibi ikinci parametreler).

> **DERS:** bir ses dosyası bir sesin kendisi değildir. Nerede başladığı,
> nerede bittiği ve ne kadar yüksek olduğu ayrı ayrı ölçülüp düzeltilmezse,
> "sesler kötü" hissinin kaynağı sesin kendisi sanılır.

---

## 2. Anahtarlar — dizeyi çağrı yerine yazma

`Sfx` sınıfındaki sabitleri kullan. Yazım hatası **derleme hatasına** dönüşür,
çalışma anında sessizliğe değil.

SDK'nın verdiği ortak anahtarlar:

| Grup | Anahtarlar |
|---|---|
| Oynanış | `win` `lose` `pick_up` `drop` `refuse` `timer_warning` |
| Yardımcı | `power_generic` (+ oyunun `power_<kimlik>` sesleri) |
| Arayüz | `click` `coin` `star` `panel_open` `panel_close` `purchase` `reward_claim` `unlock` |
| Kombo | `combo_1` … `combo_5` |
| Müzik | `menu` `gameplay` |

**Oyuna özel sesler kite yazılmaz.** Bu liste bir zamanlar `absorb`,
`ice_crack`, `curtain_open` gibi yalnız Block Out'ta anlamı olan adlar
taşıyordu; ikinci bir oyunda hiçbiri çalmıyor ama hepsi kodda duruyor ve "bu ne
işe yarıyordu?" sorusunu her okuyana bir kez sorduruyor.

Kendi seslerini kancadan ekle:

```csharp
AudioService.PaletteExtender = audio =>
{
    audio.Register("blok_patlat", () => SfxSynth.Pop(660f, 0.16f));
};
```

---

## 3. Dosya varsa dosya, yoksa sentez

Her ses önce `AudioSkin`'den aranır; yoksa `SfxSynth` ile kodla üretilir.
Böylece ses toplama işi parça parça ilerleyebilir — tek bir dosya eklendiği
anda devreye girer ve **eksik dosya oyunu sessiz bırakmaz.**

`SfxSynth` sözlüğü: `Pop`, `Click`, `Noise`, `Arpeggio`, `Coin`.

---

## 4. Çakışma sınırı — olay sayısı ≠ ses sayısı

Tek bir hamlede aynı olay birden çok kez yayınlanabilir. Beş özdeş klip aynı
anda çalınca genlik beş katına çıkar: kırpma + "çatırtı bombası".

`AudioService.PlayOnce(key, volume)` aynı anahtarı **60 ms** içinde bir
kereden fazla çalmaz. 60 ms neden: 60 fps'te dört kare — aynı hamlenin
olayları tek sese iner, oyuncunun ardışık iki hamlesi ise en hızlı hâlinde
bile 150 ms'den seyrek, yani hiçbir gerçek hamle sessiz kalmaz.

**Sentezlenmiş cılız bliplerle bu fark edilmez**; gerçek, tepe seviyeli
kliplerle ilk denemede duyulur. Ses dosyaları geldiği gün bu hatayı ara.

---

## 5. Müziği kim başlatıyor?

`AudioService.PlayMusic` yazılmış, ayarlara bağlanmış, duraklat panelindeki
anahtara takılmıştı — ama projede onu **çağıran tek bir satır yoktu.**

Sessizliğin sebebi "ses dosyası yok" sanıldığı için hiç aranmadı. SDK'da bu
çağrı artık `AppRouter.GoHome` / `PlayLevel` içinde.

> **DERS:** bir sistemin "hazır" olması, **bağlı** olması demek değil.
> Zincirin ilk halkasını ara: bunu kim çağırıyor?

Menü ve oynanış aynı parçayı paylaşıyorsa `PlayMusicInternal` erken çıkar —
geçişte müzik baştan başlamaz, akmaya devam eder.

---

## 6. Ayar anahtarları

`SettingsBinder` kayıt ile servis arasındaki çevirmen: `Sounds`, `Music`,
`Haptics`. Ayar değişince hem diske yazar hem servise uygular.

**Neden ayrı bir bağlayıcı:** `AudioService` "sesi kapat" bilir ama kaydı
bilmez; `SaveService` kaydı bilir ama sesi bilmez. Aralarına ince bir çevirmen
koymak her iki tarafı da yalnız kendi işiyle bırakıyor — ileride ses bir
mikser'e taşındığında değişecek tek yer orası olacak.
