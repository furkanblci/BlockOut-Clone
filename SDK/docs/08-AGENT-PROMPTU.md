# Yeni projede ajana verilecek prompt

Bu dosya, **yeni bir klon oyuna başlarken** yapay zekâ ajanına vereceğin hazır
metinleri içerir. Kopyala, köşeli parantezleri doldur, yapıştır.

---

## A. Kurulum promptu — projeyi ayağa kaldır

> Yeni bir Unity mobil klon oyunu yapıyorum. Daha önce yaptığım bir projeden
> çıkardığım bir SDK var; onu inceleyip bu projeye kur.
>
> **SDK konumu:** `[C:/Projects/BlockOut! Clone/SDK]`
>
> Şu sırayla ilerle:
> 1. `SDK/README.md` ve `SDK/MANIFEST.md` dosyalarını oku — SDK'da ne olduğunu
>    öğren.
> 2. **`SDK/docs/02-TUZAKLAR.md` dosyasını oku ve buna uy.** O dosyadaki her
>    madde bu SDK'yı üreten projede saatler yakmış gerçek bir hatadır; özellikle
>    doğrulama tuzaklarını (A bölümü) çalışma biçimine yerleştir.
> 3. `SDK/docs/00-YENI-PROJE-KURULUM.md` adımlarını uygula.
> 4. Sonunda o dosyanın kontrol listesini tek tek doğrula ve bana **ölçümle**
>    rapor et — "yaptım" değil, "şu komutu çalıştırdım, şu çıktı geldi".
>
> **Oyun bilgileri:**
> - Ad: `[OYUN ADI]`
> - Paket adı: `[com.sirket.oyun]`
> - Tür: `[bulmaca / eşleştirme / …]`
> - Bölüm sayısı: `[50]`
> - Tüketilebilirler: `[çekiç, karıştır, ekstra hamle]`
>
> Oynanışı henüz yazma — önce menü kabuğu, meta ekranlar ve kayıt sistemi
> ayakta olsun, ben görüp onaylayayım.

---

## B. Devam promptu — SDK'yı tanıyan bir oturum aç

> `[SDK yolu]/SDK/docs/02-TUZAKLAR.md` ve `01-MIMARI.md` dosyalarını oku, sonra
> şu işe başla: `[iş]`
>
> Bu projede meta katman ve arayüz altyapısı GameKit SDK'dan geliyor
> (`Packages/com.furkanblci.gamekit/`). Oraya yeni bir şey yazmadan önce
> zaten var mı diye `MANIFEST.md`'ye bak.

---

## C. Kalıcı proje kuralları (`CLAUDE.md` içine)

Yeni projenin kök dizinine `CLAUDE.md` olarak koy:

```markdown
# Proje kuralları

## Altyapı
Meta katman, arayüz araç takımı ve düzenleyici araçları
`Packages/com.furkanblci.gamekit/` içindeki GameKit SDK'dan geliyor.
Yeni bir yardımcı yazmadan önce `SDK/MANIFEST.md`'ye bak — büyük ihtimalle var.

SDK'nın oyunla konuştuğu tek yer üç arayüz ve dört kancadır
(`SDK/docs/01-MIMARI.md` §8). Başka bir yerde SDK'ya oyuna özel tip sızdırma.

## Doğrulama — bunlar pazarlık konusu değil
- Ölçmeden önce `EditorApplication.isPlaying` kontrol et; oynatma modunda
  betikler DERLENMEZ ve ölçtüğün şey eski koddur.
- Düzenle → tazele → **AYRI komutta** ölç. Aynı komutta birleştirme.
- Koyu renkleri gözle yargılama, piksel ölç.
- Saydamlığı düz koyu zemin üstünde doğrula, dama tahtası üstünde değil.
- Düğmeyi ışın taramasıyla test et, `onClick.Invoke()` ile değil.
- Sayısal doğrulama > ekran görüntüsü. Görüntüyü son kontrol olarak kullan.

Tamamı: `SDK/docs/02-TUZAKLAR.md`

## Arayüz
- Arayüz PREFAB değil KODLA kurulur (`UiKit`).
- Ölçüler referanstan **ölçülerek** gelir, göz kararıyla değil.
- Elle düzeltmeler `Ctrl+Shift+U` penceresiyle yapılır ve fark olarak saklanır.
- Prosedürel maske yazarken `Mathf.SmoothStep` GLSL'in `smoothstep`'i DEĞİLDİR.

## Test araçları
Asla görünür düğme olmaz. Gizli hareket (5 dokunuş / F8) veya düzenleyici
penceresi. Tamamı `#if DEVELOPMENT_BUILD || UNITY_EDITOR` içinde.

## Git
`git add -A` YASAK — yol vererek aşamala. Bütün iş `feature/...` dalında,
`main`'e PR ile girer.

## Klon etiği
Hedef referansa %85-95 benzerlik, %100 DEĞİL. Düzen ve akış referanstan
alınabilir; renk vurguları, ikon çizimleri, logo ve düğme gradyanları bizim
olmalı.
```

---

## D. SDK'yı güncelleme promptu

Yeni oyunda SDK'da bir eksik bulursan, düzeltmeyi **kaynağa geri taşı**:

> `[SDK yolu]/SDK/package/` içindeki GameKit'te şu eksiği buldum: `[eksik]`
>
> Bu projede `Packages/com.furkanblci.gamekit/` altında şöyle düzelttim:
> `[düzeltme]`
>
> Aynı düzeltmeyi SDK kaynağına taşı, `MANIFEST.md`'yi güncelle ve gerekiyorsa
> `docs/02-TUZAKLAR.md`'ye yeni bir madde ekle. Değişikliği Roslyn ile
> derleyerek doğrula (yöntem `SDK/README.md` içinde anlatılıyor).

> **Bu adımı atlama.** SDK'nın değeri her oyunda biraz daha artmasından geliyor.
> Düzeltmeyi yalnız yeni projede yaparsan üçüncü oyunda aynı hatayı yeniden
> yaşarsın.

---

## E. SDK'nın kendini derlediğini doğrulama

SDK `Assets/` dışında durduğu için Unity onu derlemez. Değişiklikten sonra
Roslyn ile doğrula (Unity kendi derleyicisini getiriyor):

```bash
U="C:/Program Files/Unity/Hub/Editor/<sürüm>/Editor/Data"
"$U/NetCoreRuntime/dotnet.exe" "$U/DotNetSdkRoslyn/csc.dll" "@rt.rsp"
```

`rt.rsp` içeriği: `-target:library -nostdlib+ -langversion:9.0`, projenin
`.csproj` dosyasındaki bütün `HintPath` referansları (`-r:`), ve
`SDK/package/Runtime` altındaki bütün `.cs` dosyaları. Editör katmanı için
aynısı + üretilen `GameKit.Runtime.dll` referansı.
