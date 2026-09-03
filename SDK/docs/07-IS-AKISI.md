# İş akışı — stüdyo alışkanlıkları

Bu proje bilerek **gerçek bir mobil oyun stüdyosu gibi** yürütüldü. Aşağıdakiler
kaynak projede işe yaramış, tekrarlanabilir alışkanlıklar.

---

## 1. Dal ve PR düzeni

```
main
 └── feature/m3-store-screen
 └── feature/m6-performance
```

- Bütün iş `feature/...` dalında, `main`'e **PR ile** girer
- Tek kişilik projede bile PR aç: **kendi kodunu inceleme** alışkanlığı kurar
  ve değişikliğin tamamını bir arada görmenin başka yolu yok
- Commit'ler dar kapsamlı ve açıklayıcı; issue kapatan commit `Closes #N` yazar
- Milestone'lar M0…M6 diye numaralanır, her biri issue'lara bölünür

## 2. `git add -A` YASAK

Aynı ağaçta iki kişi (ya da bir kişi + bir ajan) çalışıyorsa bu komut karşı
tarafın dosyalarını senin commit'ine süpürür. Kaynak projede bir seferde
**dokuz yeni + dört değişmiş** dosya yanlış commit'e girdi.

```bash
git add Assets/_Project/Scripts/Runtime/UI/Foo.cs docs/BAR.md   # yol vererek
```

Kazara olursa: `git reset --soft HEAD~1` → `git restore --staged <yol>`.
Çalışma ağacına dokunmaz.

**Oturum başında sahipliği netleştir.** Kaynak projede kimin neyi tuttuğu
sabit değildi, her gün değişti. Kendi alanın dışındaki dosyaları okuma-değiştirme;
oradaki hataları da düzeltme, yalnız bildir.

## 3. Cihaz testi ve NUMARALI bulgu dosyaları

Gün sonunda gerçek cihazda duman testi. Bulunan her şey **numaralı** bir
dosyaya yazılır:

```
docs/APK-BULGULARI.md      →  21 madde
docs/APK-BULGULARI-2.md    →  56 madde
...
docs/APK-BULGULARI-6.md    →  62 madde
```

Kaynak projede altı tur yapıldı, ~250 madde, hepsi kapatıldı.

**Neden numara:** "şu düğme kötü" biçimindeki geri bildirim numarasız kaybolur.
Numarayla her madde tek tek kapanır, kapandığı **doğrulanır** ve tur sonunda
"56/56" diye sayılabilir.

**Biçim:** her madde bir kutucuk (`- [ ]`), altında ne yapıldığı ve ölçüm.
Kapanan madde silinmez, tiklenir — sonraki turda "bu zaten çözülmüştü" demek
için kayıt lazım.

## 4. İçeriği de test et

Kod nasıl test ediliyorsa içerik de test edilir. `templates/github-workflows/`
altındaki CI her PR'da bütün bölümleri şema **ve oynanabilirlik** açısından
denetler — **çözülemeyen bölüm `main`'e giremez.**

Unity batch modu lisans ister; secret tanımlı değilse iş atlanır ve PR bloke
olmaz (kurulum adımları dosyanın başında yazıyor).

## 5. Önce ölç, sonra optimize et

Profillemeden optimize etme. `PerfProbe` cihazda kare hızını **ve çöp
üretimini** ölçer — casual mobilde takılmanın en sık sebebi düşük ortalama fps
değil, GC duraklamalarıdır.

## 6. Doğrulama kültürü

`02-TUZAKLAR.md` bölüm A'nın tamamı bu başlık altında. Özet:

- Ölçmeden önce **doğru derlemeyi** ölçtüğünden emin ol
- Sayısal doğrulama > ekran görüntüsü (hem ucuz hem daha çok hata buluyor)
- Ekran görüntüsüne yalnız **geometri** için güven
- Düğmeyi ışın taramasıyla test et, `Invoke` ile değil

## 7. Maliyet farkındalığı (yapay zekâ ile çalışırken)

Kaynak projede 5 saatlik kota bir saatte tükendi. Sebepleri sırasıyla:

1. En ağır modeli rutin işte kullanmak
2. Ağır görüntü analizi (bir oturumda 15+ temas sayfası, ekran görüntüsü serisi)
3. Tek uzun oturumda iki milestone birden — her araç çağrısı büyüyen bağlamı
   yeniden işliyor

**Uygula:**
- Rutin uygulama işinde (kod, commit, seviye verisi) orta seviye model yeter;
  ağır modeli çetrefil mimari/hata ayıklama için sakla
- Sayısal doğrulamayı tercih et — **hem ucuz hem daha etkili**
- Milestone başına **yeni oturum** aç; devir notunu bir dosyaya yaz
- Video/görsel analizini tek toplu geçişte yap

## 8. Devir notu — oturumlar arası süreklilik

Kaynak projede `docs/DEVAM.md` her oturum sonunda güncellendi ve yeni oturum
"bu dosyayı oku, kaldığımız yerden devam et" diye başladı.

İçinde ne olmalı:
- **Tek cümlelik durum** (en üstte)
- Açık maddelerin sayımı, dosya dosya
- Bu oturumda öğrenilen dersler (tekrar etmesin diye)
- Paralel çalışılan alanlar — dokunulmayacak yerler
- Yayın öncesi kontrol listesi

## 9. Yayın öncesi kontrol listesi

Kaynak projeden devralınan maddeler:

- [ ] `DeviceErrorOverlay.Enabled = false`
- [ ] Hata ayıklama menüsü yalnız `#if DEVELOPMENT_BUILD || UNITY_EDITOR` içinde
- [ ] Android paket adı doğru (**yayımlandıktan sonra değiştirilemez**)
- [ ] `versionCode` artırıldı
- [ ] Haptik **gerçek cihazda** doğrulandı (editörde doğrulanamaz)
- [ ] `link.xml` + "Always Included Shaders" — stripping cihazda öldürür
- [ ] Gerçek cihazda duman testi: her ekran açılıyor, her bölüm yükleniyor
- [ ] Kayıt göçü test edildi (eski sürüm kayıtla aç)
