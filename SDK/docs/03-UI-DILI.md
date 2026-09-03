# Arayüz dili — referansı ÖLÇEREK çıkarmak

Klon yaparken en büyük hata "referansa bakıp benzerini yapmaya çalışmak"tır.
Sonuç her zaman "biraz benziyor ama ucuz duruyor" olur ve nedeni bulunamaz.

**Doğru yöntem: ölç.** Bu proje bütün ekranlarını referans karelerini
**piksel piksel ölçerek** kurdu, göz kararıyla değil.

---

## 1. Kaynağı en yüksek çözünürlükte al

| Kaynak | Nasıl |
|---|---|
| App Store görselleri | Apple CDN'den `0x0ss.png` dönüşümüyle **kayıpsız PNG** (1320×2868) |
| Google Play APK | `apktool` / asset çıkarma — aynı stüdyonun Android oyunu varsa **arayüz kitini** görürsün |
| Video | `06-REFERANS-CIKARIMI.md` |

> **WhatsApp/ekran görüntüsü JPEG kullanma.** Bu projede önce 946×2048 JPEG'ler
> ölçüldü; sıkıştırma artefaktları kenar konumlarını 2-3 piksel kaydırıyordu ve
> ölçümler tutarsız çıkıyordu.

**Aynı stüdyonun başka oyununu bulmak büyük kazanç.** Block Out! iOS'a özel ve
paketleri şifreli; ama aynı stüdyonun Android oyunundan 5.773 varlık çıkarıldı
ve **font ailesi, 9-slice kenar payları, gölge yoğunlukları** oradan öğrenildi.

---

## 2. Ne ölçülür

Her ekran için sırayla:

1. **Yüzdelik dikdörtgenler.** Her kutunun sol/alt/sağ/üst kenarını ekran
   genişliğine bölerek yaz. SDK'daki `UiKit.Place(rect, 0.045f, 0.230f, 0.955f, 0.788f)`
   tam bu sayıları alıyor. Piksel değil yüzde: farklı en-boy oranlarında tutar.
2. **Renkler — pipetle, tahminle değil.** `numpy` ile bölgenin ortalamasını al.
   Koyu bölgelerde **gözle bakma** (`02-TUZAKLAR.md` §4).
3. **Köşe yarıçapı.** Köşeden köşegen boyunca ilerleyip rengin değiştiği
   noktayı bul; yarıçap ≈ o mesafe.
4. **Gölge.** Kaydırma miktarı ve koyuluk. Bu projede neredeyse her düğmede
   aynı numara var: **bir tık aşağı kaydırılmış koyu kopya** — kalınlık hissi
   oradan geliyor.
5. **Yazı boyutu.** Büyük harf yüksekliğini ölç, yazı tipi boyutuna çevir.
6. **Aralık ritmi.** Kartlar arası boşluk genelde tek bir birimin katıdır;
   birimi bul, hepsini ona oturt.

---

## 3. Font — en büyük tek görsel kazanç

Bu projede fontun değişmesi, bütün diğer düzeltmelerin toplamından daha çok
fark yarattı. Casual mobil oyunların dili: **ağır, yuvarlak, geniş.**

- `Baloo 2 ExtraBold`, `Fredoka One` (Google Fonts, OFL — ticari güvenli)
- SDK'daki `FontSetupTool` TTF'den TMP asset'i üretip varsayılan yapar

**Tuzak:** TMP kaynak TTF'i saklamaz, yalnız SDF atlası tutar. Bir APK'dan
çıkardığın fontu geri elde edemezsin — yalnız hangi font olduğunu teşhis
edebilirsin.

---

## 4. Stil DNA'sını tek paragrafa indir

Ölçümlerden sonra referansın stilini **yazıya dök**. Bu projeninki:

> 3B render edilmiş parlak plastik/oyuncak estetiği. Vektörel düz ikon DEĞİL.
> Sol üstten tek yumuşak anahtar ışık, sağ altta yumuşak gölge. Yüzeyde
> belirgin bir specular leke. Kalın, bol yuvarlatılmış kenar; keskin köşe yok.

Bu paragraf sonra **görsel üretim promptlarının başına** konur ve her ikon
aynı aileden çıkar. Ayrıntı: `04-GORSEL-PIPELINE.md`.

---

## 5. Kimlik katmanını AYIR — klon ≠ kopya

Portfolyo için yapılan bir klonda hedef **%85-95 benzerlik**, %100 değil.

| Referanstan alınabilir (tür standardı) | Senin olmalı (kimlik) |
|---|---|
| Düzen, oran, akış | Renk vurguları |
| Ekran sırası, sekme sayısı | İkon çizimleri |
| Etkileşim ritmi | Seçili sekme muamelesi |
| Bilgi hiyerarşisi | Düğme gradyanları, logo |

> Referans stüdyonun asset'lerini birebir kullanmak portfolyoda **kötü**
> izlenim yaratır. Amaç "klon yapabiliyorum" demek, "kopyaladım" demek değil.
> Özellikle göze çarpan yerler (alt sekme çubuğu, logo, wordmark) birebir
> olmamalı.

---

## 6. Taşan yazıyı araçla avla

Ölçerek kurulmuş bir arayüz bile başka dilde/başka sayıda taşar. SDK'daki
`UiOverflowAudit` bir ekran ağacındaki **bütün** etiketleri ölçüp kabına
sığmayanları listeler.

**Yaşanmış:** mağazada dört haneli bakiye jetonun altında kalıyordu — gözle
fark edilmesi için tam o bakiyeye sahip olmak gerekiyordu. Araç bunu ilk
taramada buldu.
