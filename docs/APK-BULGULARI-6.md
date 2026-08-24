# 12. TUR — kullanıcının ekran ekran bulguları (2026-08-23)

Kullanıcı ekran görüntüleriyle birlikte madde madde bildiriyor. **Bu tur
henüz TAMAMLANMADI**: kullanıcı şimdilik yalnız **mağaza** kısmını verdi,
diğer ekranlar sonra eklenecek ("market kısmı bu kadar, bunları not al, ben
tekrar söyliycem yapman gerekenleri").

Kullanıcının talebi: *"bak ben tüm gördüğün sorunları yazıcam sana hepsini
düzeltip çözücez; ilk olarak bunların hepsini maddele, sonrasında baştan
sona hepsini tek tek yapıcaz."*

## Yöntem (önceki turlarla aynı)

- Madde bitince kutusu `[x]` işaretlenir, altına **NE YAPILDI** ve
  **NASIL DOĞRULANDI** yazılır (ölçüm varsa sayısıyla).
- Yarım bırakılan madde işaretlenmez.
- Yanlış çıkan eski ölçüm silinmez, düzeltilir ve neden yanlış olduğu yazılır.
- **Ölçmeden değiştirme.** Aynı ölçüm kodu hem referansa hem bize uygulanır.

## Referans durumu

Kullanıcı bu turda **doğrudan orijinal oyundan ekran görüntüsü** paylaştı
(jeton kapsülü, "Shop"/"Mağaza" tentesi, Özel Teklifler kartı, Packs şeridi,
jeton paketleri). Bunlar sohbetteki görsellerdir; depodaki karşılıkları:

- `_Reference/store/` — App Store'dan kayıpsız PNG (en iyi kaynak)
- `_Reference/extracted2/` — Magic Sort'tan 5773 varlık (hazır tente,
  şerit, bayrak, kâğıt doku aranacak ilk yer)
- `_Reference/frames/` — 52 kare

**Kural hatırlatması:** portfolyo kararı gereği birebir kopya değil, çok
yakın uyarlama ([[portfolio-not-clone]]). Kullanıcı bu turda ayrıca
"hazır halini de alabiliyorsan al" dedi — yani mağaza kaplamasında hazır
varlık kullanmak serbest.

---

# 0. UYGULAMA İKONU

- [x] **U1.** Uygulama ikonu orijinal oyundan zaten alınmıştı; doğrudan
      kurulacak.
      > *"uygulama ikonu orjinal oyundan aldık zaten onu koyalım direkt"*

      Not: `AppIconTool.cs` var ve `PlayerSettings.SetPlatformIcons`
      kullanıyor (obsolete uyarıları veriyor, çalışıyor). Kaynak dosyanın
      hangisi olduğu bulunacak.

      **13. TURDA DOĞRULANDI — ZATEN YERİNDEYMİŞ.** Kaynak
      `Assets/_Project/Art/Icons/icon_app.png` (1024x1024) ve uyarlanabilir
      ikon için `icon_app_bg` / `icon_app_fg`.

      `AppIconTool.Apply()` çalıştırılıp Android yuvaları tek tek sayıldı:
      **Adaptive (API 26) 6/6, Round (API 25) 6/6, Legacy 6/6** — hepsi
      doluydu ve araç hiçbir şeyi değiştirmedi (`ProjectSettings.asset`
      farkı yalnız derleme numarası).

      **Kendi hatam, not düşülüyor:** Önce "Android yuvaları boş" dedim.
      `m_BuildTargetPlatformIcons` altında gördüğüm boş `m_Textures: []`
      bloğu **iPhone**'a aitmiş; Android bloğu daha aşağıdaydı ve doluydu.
      Uzun bir YAML'de bir alanı okurken hangi platformun altında olduğunu
      da okumak gerekiyor.

---

# MAĞAZA

## 1. Jeton kapsülü (üstteki "gold" göstergesi)

- [x] **M1.** Kapsülün arka planı bizde **çok düz**, dış konturu **çok
      siyah**. Orijinaldeki hâline getirilecek; Grand Games oyunundan
      aldığımız arka plan işimize yararsa kullanılacak.
      > *"görsel o gold kısmının arkaplanı — bunun için grand gamesin
      > oyunundan aldığımız arkaplan işimize yararsa kullanalım; bizdeki
      > çok düz ve dış rengi siyah kontürü çok, onu düzeltelim birebir
      > orjinal hale getirelim o kısımı düzeltelim"*

      Alt kalemler:
      - [x] **M1a** — kapsül dolgusu düz; orijinaldeki doku/gradyan verilecek.
      - [x] **M1b** — dış kontur fazla siyah ve fazla kalın; incelt/renklendir.

      **13. TURDA KAPATILDI.** Referansta kapsülün konturu HİÇ YOK; hacmi
      veren şey alttaki sıcak pembe-kahve dudak. Bizdeki (35,19,9) siyaha
      çalan kahve kenar kaldırıldı, üç katman (gölge+kenar+yüz) tek
      prosedürel dokuya indi. Kapsül 158x45 -> 179x58 (referans 176x60),
      jeton 56 -> 83 birim, rakam puntosu 40 -> 52.
      Ayrıntı: `docs/DEVAM.md`, 13. tur.

## 2. Tente (awning)

- [x] **M2a.** Orijinal tentenin alt kenarına doğru **gölge şeklinde bir
      çizgi** var; bizde yok.
      > *"orjinal oyundaki tente çok daha güzel, alt kısmına doğru bir gölge
      > şeklinde çizgisi var"*

- [x] **M2b.** Tentenin **ucu aşağıya düşmüş** gibi duruyor, bu derinlik
      katıyor. Bizde bu yok.
      > *"tentenin ucu aşağıya düşmüş gibi gözüküyor bu da derinlik katıyor
      > bunu da istiyorum"*

- [x] **M2c.** Gerekirse tentenin **hazır varlığı** kullanılabilir.
      > *"bunun hazır halini de alabiliyorsan al gerekirse"*

- [x] **M2d.** Tentedeki **yazının yeri ve boyutu** yanlış: bizde yukarı
      kaçıyor. Orijinalde doğru konumda.
      > *"tentede textin olduğu yer de çok doğru, bizde yukarıda kaçıyor
      > text; o textin yeri boyutu da iyi ayarlanmalı"*

      **M2a-M2d 13. TURDA KAPATILDI.** Asıl fark yapısaldı: referansta
      tentenin alt kenarı DÜZ — festonun çentiklerini koyu lacivert bir
      levha dolduruyor ve yaylar onun üstünde kumaşın sarkması olarak
      okunuyor. Bizde çentikler saydamdı, kahverengi duvar görünüyordu.
      Levha DOKUYA çizildi (ayrı panel olarak 8. ve 11. turlarda iki kez
      eklenip kaldırılmıştı).

      Ölçümler: yay derinliği 18 -> 41 br (referans 41), kumaş altı payı
      11 -> 32 (29), tente yüksekliği %13,9 -> %15,3 (%15,4), başlık
      103 -> 84 br (84), başlık merkezi %57,9 -> %41,5 (%43,2).

      M2c (hazır varlık) gerekmedi. Ayrıntı: `docs/DEVAM.md`, 13. tur.

- [x] **M2e.** Tentenin hemen altındaki **mavilik**: sabit bir mavi arka
      plan. **STATİK** olacak — aşağı kaydırınca içerikle birlikte
      gelmeyecek. Yalnız **"Mağaza" başlığı ile "Özel Teklifler" arasındaki
      alanda** kalacak. Daha önce eklenmişti ve sorun çıkarmıştı; bu sefer
      bu kısıtla eklenecek.
      > *"tentenin hemen altında mavilik var, o mavilik sabit bir mavi
      > arkaplan ama aşağıya kaydırdığımızda tente ile birlikte gelecek bir
      > şey değil, static yani. daha önce eklemiştik ama sorun yaratmıştı,
      > yine eklemeni istiyorum ama sadece mağaza ile özel teklifler
      > arasındaki yerde kalsın, aşağıya kaydırınca bizle gelmesin"*

      **13. TURDA M2a/M2b İLE BİRLİKTE KAPANDI.** İstenen "mavilik" tam
      olarak festonun çentiklerini dolduran koyu lacivert levha. Üç şart da
      sağlanıyor:
      - **statik**: levha tentenin DOKUSUNUN içinde, tente de `root`
        altında (kaydırma alanının dışında) — içerikle gelmesi mümkün değil.
      - **sınırlı**: doku tentenin dibinde bittiği için levha oradan aşağı
        taşamıyor. Ölçüm: levhadan sonra doğrudan turuncu kurdele geliyor
        (referansta da öyle; arada yalnız 4 birimlik bir şerit kalıyor).
      - **daha önce sorun çıkarmıştı**: çünkü AYRI PANELDİ. 8. ve 11.
        turlarda öyle eklenip iki kez kaldırılmıştı; dokuya çizilince
        taşma ve sürüklenme sorunu ortadan kalkıyor.

## 3. Şerit başlıklar ve indirim bayrağı

- [x] **M3a.** "Special Offers" / "Packs" gibi **şerit başlıklar** daha iyi
      hale getirilecek: panel görünümü, dış çizgi, boyut hesaplanacak.
      > *"special offers bu şerit şeklinde başlıkları da daha iyi hale
      > getirmeliyiz; o panellerin görünümü, dış çizgisi, boyutu filan
      > hesaplanıp daha iyi hale getirilmeli"*

      **13. TURDA KAPATILDI.** Referansın dikey kesiti altı bantlı; kritik
      olan altınla turuncu ARASINDAKİ koyu kırmızı iç kenar (95,0,0) —
      kullanıcının "dış çizgisi" dediği şey o. Bizde altın doğrudan
      turuncuya değiyordu, ikisi tek yüzey gibi okunuyordu. Gövde de
      gradyanlı ((255,136,62) -> (205,49,0)) ve uçların yarıçapı tam
      kapsül değil, yüksekliğin %30'u. 9-dilimli tek prosedürel sprite.

- [x] **M3b.** **"%90 OFF" indirim bayrağının modeli çok kötü.** Oyundan
      hazır alınabilir veya aynı dilde üretilebilir.
      > *"%90 off indirim yazan o bayrağın modeli de çok kötü, yine hazır
      > oyundan alınabilir veya aynı dilde üretilebilir; çok kötü gözüküyor,
      > onu güncelleyelim"*

      **13. TURDA KAPATILDI.** Sorun renk ya da boyut değil BİÇİMDİ:
      bizimki alt ucunda V çentiği olan, köşeden aşağı SARKAN bir flamaydı;
      referansta kartın köşesini 45 derece KESEN bir kurdele var
      (`menus,powerups,vs.mp4` 11,4. sn). Flama kartın dışına sarkıp
      "etiket" gibi duruyordu, kurdele kartın parçası oluyor.

      Referans karesi 384 px ve bandın dik kalınlığı orada ~20 px olduğu
      için kesmek bulanık olurdu; kullanıcının izin verdiği gibi "aynı
      dilde" yeniden çizildi. Ölçüm: kenar (140,10,70), gövde
      (230,40,130)->(185,20,100), merkez köşeden %12,8, kalınlık %5,2.

## 4. Paket kartı (Starter Pack / teklif kartı)

- [x] **M4a.** Booster **ikonlarının boyutu çok fazla**; orijinaldekiyle
      kıyaslanıp küçültülecek.
      > *"paket kısmında ikonların boyutu çok fazla, orjinaldekiyle
      > kıyaslanıp düzenlenmeli"*

- [x] **M4b.** İkonların altındaki **"1s" / "x1" yazılarının boyutu**
      ayarlanacak.

- [x] **M4c.** İkonların **alt kısımları kesilmiş gibi** görünüyor; net ve
      düzgün görünmeliler.
      > *"ikonların alt kısımları kesilmiş gibi gözüküyor onu da düzeltelim,
      > net düzgün bir şekilde gözüksünler"*

- [x] **M4d.** Jetonun **dış parlaması (glow) rahatsız edici**; düzeltilecek.
      > *"yine o goldun dış parlaması rahatsız edici gözüküyor düzeltilmeli"*

- [x] **M4e.** **"10 000" yazısının konumu ve görünümü** ayarlanacak.

      **M4a-M4e 13. TURDA KAPATILDI.** Ayrıntı: `docs/DEVAM.md`, 13. tur.
      Özet: booster kutusu 0,36 -> 0,295 (gözle "%35 büyük" sanmıştım,
      ölçüm %12 dedi) ve alt kenarı maskeden içeri alındı; "x1" puntosu
      32 -> 40; jeton sprite'larının İÇİNE pişmiş beyaz hale dokuz dosyada
      temizlendi; "2 000"un konumu zaten doğruydu, konturunun KALINLIĞI
      yanlıştı ve `CreateTitle` ortak materyal yüzünden verilen rengi
      yok sayıyordu.

- [x] **M4f.** Kartın **"Starter Pack" kısmına geçmeden önceki ara çizgi /
      görünüm geçişi** daha iyi hale getirilecek.
      > *"o panelin starter pack kısmına gelmeden o aradaki çizgi görünüm
      > geçişi daha iyi hale getirilmeli"*
      >
      > **YAPILDI (14. tur).** Kartlar düz levha yığınıydı: teklif kartında
      > koyu gövde + bant + sanat alanı, paket kartında bant + dudak + raf +
      > krem. Dikey kesit bunu net gösteriyordu — renk hiç değişmiyor, sonra
      > bir anda sıçrıyor. Referansın aynı kesiti sürekli ve sınırda ÜÇ öge
      > var: nesnelerin rafa düşen gölgesi (237,199,156), rafın ışık alan
      > burnu (250,228,204), rafın ön kenarı (211,148,87); bandın üstünde de
      > kendi koyu kenarı (102,17,134) ve hemen altında parlak şerit
      > (219,74,255). Yani referans sınırı yumuşatmıyor, ona KALINLIK
      > veriyor. İki kart da artık iki gradyan görselden ibaret
      > (`MenuSprites.PackFace/PackBand`, `OfferFace/OfferBand`); ara
      > paneller ve "dikiş görünmesin" diye konan 0.04 bindirme kalktı.

- [x] **M4g.** Kart **boyutsal olarak** orijinal oyundaki görünümle aynı
      oranlarda olacak.
      >
      > **YAPILDI (14. tur).** Referans kartın krem yüzü 869x278 piksel;
      > 946 piksellik görsel 1080 birimlik kanvasa 1.1416 ile ölçeklendiği
      > için 992x317 birim. Genişliğimiz zaten 1002 birimdi (%1 fark) ama
      > yüksekliğimiz 241 idi — %24 basık. Bant 144 -> 174. Kart içindeki
      > ögelerin kutuları da yüzün ORANI olarak ölçüldü: kese 0.061..0.813,
      > "2 000" 0.061..0.353, ad yazısı bandın 0.283..0.691'i, düğme
      > 0.598..0.990 x / 0.158..0.849 y. Ad puntosu 58 -> 74, fiyat 44 -> 50.

- [x] **M4h.** Aralarındaki **kahverengi arka planın deseni** orijinaldekine
      daha çok benzetilecek.
      > *"aralarındaki kahverengi arkaplanın deseni daha çok benzetilmeli,
      > orjinal oyundaki gibi olmalı"*
      >
      > **YAPILDI (14. tur).** Desenimiz `cos(x+y) + cos(x-y)` ile
      > çiziliyordu; toplama formülü gereği bu `2·cos(x)·cos(y)`ye eşit, yani
      > ÇARPANLARINA AYRILIYOR ve eksen hizalı KARE bir ızgara veriyor —
      > ekranda yuvarlak köşeli kareler çıkıyordu. Referans 45°'lik eşkenar
      > dörtgen kafes: yatay adım 222 piksel (= 253 birim), çizgi merkezi 59,
      > zemin medyanı 100, yüzeylerin en açığı 116 / en koyusu 70, çizginin
      > üst-sol yanında ince bir parlama. Kafes artık `x+y` ve `x-y`
      > doğrularına DİK MESAFEYLE kuruluyor. Zemin tonu da düzeltildi:
      > (80,24,16) -> (100,28,13). Ölçüm sonrası medyanımız (99,28,13),
      > adım 253 birim.

- [x] **M4i.** Şerit başlıklarda **dış çizgi rengi, ait olduğu başlığın
      rengiyle aynı** olacak. Örnek: "Packs" krem dış çizgili ama sarı
      şeritle bağlanmış — aynı renkte bağlanmalı.
      > *"yine bu şerit başlıklarda dış çizgiler, hangi başlığın dış çizgisi
      > neyse onla aynı olmalı; örneğin packs kısmı krem rengi dış çizgisi
      > var ama sarıyla bağlanmış şerit, ondan bahsediyorum, aynı renkte
      > bağlanmalı"*
      >
      > **YAPILDI (14. tur).** Tek kurdele görselini iki bölümde de
      > kullanıyorduk. Referansta ikisi ayrı: Teklifler altın çerçeve +
      > turuncu gövde, Paketler KREM çerçeve + mor gövde — dil de kurdelenin
      > çerçevesiyle aynı renk. `ribbon_packs.png` (76x109) ve
      > `ribbon_packs_tail.png` `market_ref.png`ten kesildi; renk profili
      > yazı olmayan bir sütundan (x=150) alınıp KENAR MESAFESİNE göre
      > uygulandı (`y`'ye göre değil — yoksa çerçeve yuvarlak uçlarda
      > kesiliyor). Başlığın konturu da bölüme bağlandı: altında koyu kahve,
      > morda koyu mor (46,5,70). Ölçüm: yazı/kurdele %38,4 (ref %36,7),
      > dil rengi (230,196,159) vs ref (229,197,159).

## 5. Paket kutucuklarının zemini

- [x] **M5.** Paketlerin **beyaz zemini kâğıt dokulu** olacak, düz beyaz
      değil.
      > *"bu paketlerin arkaplanı beyaz kısmı, orada bi kağıt dokuluymuş
      > gibi, düz beyazdan ziyade öyle ya; onu yapabiliyorsak yapalım"*
      >
      > **YAPILDI (14. tur) — ama tanecikle değil.** İlk varsayım "kâğıt
      > gürültüsü ekle" idi; ölçüm bunu çürüttü. Referansın kreminde
      > satır/sütun gradyanı çıkarıldığında kalan sapma std **0,1** — yüzey
      > pürüzsüz, tanecik yok. Mavi kanalı ±8 birimlik pencereye gerip
      > baktım, ışın deseni de yok. Derinliği veren tek şey kesenin
      > arkasındaki YUMUŞAK HALE: yakında (255,248,240), uzakta
      > (248,239,224) — soğuk beyaz bir ışık. `MenuSprites.SoftGlow`
      > (karesi alınmış radyal sönüm) üç kart tipinin de sanatının arkasına
      > kondu. Ölçüm: mavi kanalda tepe 232 / taban 225 (referans 234/224).

## 6. Jeton görsellerindeki koyuluk

- [x] **M6.** Jetonlarda görsel olarak bir **koyuluk** var. Kullanıcı
      editörden baktı: sebebi **drop (gölge)**. O drop'un ne işe yaradığı
      anlaşılmıyor, kötü görünüyor, amaçlanan şey olmamış. Çözülecek.
      > *"goldlar kısmında görsel olarak görselde bi koyuluk var, baktım
      > editörden droptan dolayı; o drop ne işe yarıyor anlamadım ama kötü
      > gözüküyor, amaçlanan şey olmamış, onu da çözelim"*
      >
      > **YAPILDI (14. tur) — kullanıcı teşhisi birebir doğruydu.** Suçlu
      > `PriceButton`ın gölgesi. Metot `root` -> (`Drop`, `Body`) kuruyor ama
      > geriye `Body`yi döndürüyordu; çağıran da dönen şeyi yerleştiriyordu,
      > yani KÖK hiç yerleştirilmiyor ve varsayılan olarak ebeveynin tamamına
      > yayılıyordu. `Drop` kökün %99'u olduğu için düğmenin gölgesi jeton
      > kutucuğunun ve paket bandının BÜTÜNÜNÜ %30 siyahla yıkıyordu.
      > Kök artık metodun kendisi tarafından yerleştiriliyor. Ölçüm: jeton
      > kutucuğunun kremi (251,241,226) — hedeflenen `CardCream`in aynısı.

---

# GENEL — bütün ekranları ilgilendiren

## 7. Başlık konturu (TÜM başlıklı sahneler)

- [x] **G1.** Başlığın konturu **aynı renk morda ve KALIN** olacak —
      orijinal oyunda böyle. Yalnız sıralama değil, **yolculuk, koleksiyon
      ve başlığı olan her sahne** için geçerli. Yöntem serbest (shader,
      üst üste text, ne gerekiyorsa).
      > *"ilk olarak bu bütün başlıklı sahneler için geçerli, yolculuk
      > koleksiyon için de: başlığın outlinesi aynı renk morda ve kalın,
      > orjinal oyunda böyle. yani bunu istiyorum bizde de her başlıkta
      > olsun. bunun için shader mı yazarsın, üst üste text mi koyarsın,
      > hangi yöntemle yaparsın bilmiyorum ama bu olsun"*


      **NE YAPILDI:** İki ayrı hata vardı.

      1. **Konturun YÖNÜ yanlıştı.** Ölçüm (`m_051.jpg`, "Profil"):
             bandın rengi     (66, 39, 196)
             referans halesi  (98, 71, 228)   <- BANTTAN AÇIK
             bizim konturumuz (50, 33, 146)   <- BANTTAN KOYU
         Referans harfin çevresine IŞIK koyuyor, biz gölge koyuyorduk.
      2. **Bandımız fazla açıktı** (70,55,226) — yani referansın HALESİ
         kadar açık. Banttan açık bir hale çizmeye yer kalmıyordu.

      Bant ölçülen değere indirildi (66,39,196 / 60,35,179) ve
      `GameKit.UI.UiTitleEmboss` yazıldı: arkaya şişirilmiş bir kopya
      (kalın parlak hale) + öne ince koyu kenar. Kopya KARDEŞ olarak bir alt
      sıraya konuyor (uGUI çocukları ebeveynden sonra çizer). `MenuPage.Header`
      tek yerden uygulandığı için profil, sıralama, koleksiyon, ayarlar,
      yolculuk ve mağaza BİRDEN düzeldi.

      **ARA TUZAK:** İlk sürümde hale hiç görünmedi. Ayarların hepsi doğruydu
      (konturW 0,340, dilate 0,160) ama hale, font varlığının VARSAYILAN
      materyalini kullanıyordu; TMP'de kontur genişliği atlasa basılı dolgu
      payıyla sınırlı ve o materyalde pay yetmiyordu. `fontSharedMaterial`
      öndekinden türetilince çizildi.

      **NASIL DOĞRULANDI:**
          bant   bizim (65,37,191)  referans (66,39,196)
          hale   bizim (95,67,222)  referans (98,71,228)   44.777 mor piksel
      Görsel: `_Reference/notes/emboss_all.png` (üstte referans, altında
      dört ekranımız), `emboss_cmp4.png`.
## 8. Responsive (KRİTİK)

- [x] **G2.** Telefondan telefona arayüz boyutları bozulabiliyor. Her şey
      responsive yapılıp **her telefonda aynı görünüm** yakalanacak.
      > *"yine kritik bir şeyden bahsedicem: telefondan telefona uılar
      > boyutu bozulabiliyor, responsive yapıp her şeyi her telefonda aynı
      > görünümü de yakalayalım"*
      >
      > **YAPILDI (14. tur) — göz yerine ÖLÇÜMLE tarandı.**
      >
      > Kanvas YÜKSEKLİĞE kilitli (`matchWidthOrHeight = 1`). Bunun anlamı:
      > 1920 birimlik yükseklik her cihazda aynı, ama birim GENİŞLİĞİ ekran
      > oranıyla değişiyor — 16:9'da 1080, 19.5:9'luk bir telefonda yalnız
      > **886**, 4:3 tablette **1440**. Yani kırılma her zaman EN DAR ekranda
      > olur ve sabit birimle yazılmış her genişlik orada taşar.
      >
      > Altı ekran (mağaza, koleksiyon, sıralama, yolculuk, ayarlar, profil)
      > üç oranda kurulup iki ayrı testten geçirildi:
      >   1. **Yazı taşması:** her `TMP_Text` için `ForceMeshUpdate` sonrası
      >      ÇİZİLEN genişlik ile kutu genişliği. Sonuç: sıfır. (Kalan iki
      >      uyarı mağazadaki "20 000"/"60 000" — 224 birim çizilene karşı
      >      220 birimlik kutu; fark yazının KONTURUNUN kalınlığı, 2 birim.)
      >   2. **Oran kayması:** her ögenin genişliği ekran genişliğine bölünüp
      >      üç oran karşılaştırıldı. Oran %5'ten fazla kayıyorsa o öge sabit
      >      birimle yazılmış demektir.
      >
      > Bulunan tek GERÇEK kusur: yolculuk ekranının bölge başlığı 1040 birim
      > SABİTTİ ve 886 birimlik ekranda 154 birim taşıyordu (ekranın %117'si).
      > Oranlı yazıldı (0.02–0.98).
      >
      > Yolculuğun diğer 54 "sabit" ögesi (disk 580, halka 594, kilit 230,
      > etiket 380, eylem 280 birim) DOĞRU: onlar bir haritanın gerçek
      > nesneleri, tablette de telefonda da aynı büyüklükte olmalılar ve en
      > dar ekranda rahatça sığıyorlar. Sıralamanın 1197 birimlik `Scene`si de
      > kasıtlı — `CreateCover` ile kurulmuş bir kapak görseli ve arkasında
      > `RectMask2D` var.
      >
      > **Son durum:** en dar ekranda (886 birim) altı ekranın hiçbirinde
      > ekrandan geniş kutu ve taşan yazı yok.

## 9. Kapatma (X) düğmesi

- [x] **G3.** Klasik kapat X işareti orijinalden alınacak; bizdeki çok
      kötü. Grand Games varlığından çözülecek.
      > *"yine bu klasik kapat X işareti orjinal oyundan alınabilir,
      > bizdeki çok kötü, grand games assetinden çöz"*

---

# SIRALAMA (Leaderboard)

- [ ] **L1.** Sahnede **görülen her şey** güncellenecek. Hazır olanlar
      doğrudan kullanılacak, olmayanlar aynı dilde üretilip şu anki
      hâlinden çok daha iyi hâle getirilerek orijinaline benzetilecek.
      Kapsam: **avatar çerçeveleri, kürsü (podyum), sıralama satırları,
      Haftalık / Dünya / Ülke sekme paneli görünümü** — en ufak detayına
      kadar.
      > *"daha sonra sıralama kısmı sahnesinde gördüğün her şey güncellensin
      > ve hazır olanlar direkt kullanılsın, olmayanlar aynı dilde
      > üretilerek şuanki halinden çok daha iyi hale getirilip birebir
      > orjinal oyundakine benzetilsin. işte avatar çerçeveleri olur, kürsü
      > olur, sıralama kısmı, haftalık dünya ülke kısmı, paneli görünümü —
      > bunların hepsini en ufak detayına kadar daha temiz hale getirip
      > düzenlemeni istiyorum"*

      Alt kalemler (çalışırken bölünecek):
      - **L1a** avatar çerçeveleri
      - **L1b** kürsü / podyum
      - **L1c** sıralama satırları
      - **L1d** Haftalık / Dünya / Ülke sekme paneli

---

# ALT MENÜ (sekme çubuğu)

- [x] **N1.** Alt menü **daha iyi oldu, tamam** — ama **arka planı**
      orijinalin birebir aynısı alınabilir. Bizdekinin **dış kenarları /
      görünümü şu an kötü**.
      > *"menü kısmında alt menü daha iyi oldu okey, ama arkaplan orjinal
      > oyundakinin birebir aynısı alınabilir; onun bizdekinin dış kenarları
      > görünümü şuan kötü, birebir orjinalini alalım daha iyi hale gelsin"*
      >
      > **YAPILDI (14. tur).** Çubuğun üst dudağı DÖRT DÜZ ŞERİTTİ (koyu
      > kenar 15, ışık 4, oyuk 11, parlaklık 9 birim). Renkler doğruydu ama
      > aralarındaki sıçramalar telefonda çizgi olarak görünüyor — "dış
      > kenarları kötü" denen şey o basamaklar. Ayrıca toplam kalınlık 39
      > birimdi, referansta 30.
      >
      > 52 menü karesinden aynı durumu gösteren 8'i ortalandı (kareler arası
      > std 3,5 -> kodek gürültüsü kalmadı). Kare 443x960, kanvas 1920 birim:
      > 1 kare pikseli = 2 birim. Çubuğun üstü ekranın altından **186 birim**
      > (eskiden %10,35 = 199 varsayılmıştı). Ölçülen profil bir dokuya
      > gömüldü ve çubuğun görünen 186 biriminin tamamını bire bir kaplıyor.
      > **Doğrulama:** 186/182/176/168/162/156/120/80/40/4 birimlerinin
      > hepsinde referansla 1-2 birim fark.
      >
      > Seçili kart da tek görsele indi: dudağı KENAR MESAFESİNE göre
      > boyanıyor, böylece bant köşede de tam ölçülen kalınlıkta (iki
      > dikdörtgenle bu mümkün değildi — 7. ve 8. turlar tam bunun için
      > harcanmıştı). Kutusu yeniden ölçüldü: üstü 230 birim (bizde 260 idi),
      > genişliği slotun 1,524 katı (1,30 idi), altı ekranın 46 birim dışına
      > taşıyor (18 idi — yuvarlak alt köşeler görünüyordu).

- [x] **N2.** **Renk tonu:** orijinalde bir tık daha koyu mor var gibi;
      bizdeki fazla açık olabilir. Ölçülüp bakılacak.
      > *"ve renk olarak da sanki orjinal oyunda bir tık daha koyu mor var,
      > bizdeki ekstra açık olabilir, ona da bakalım"*
      >
      > **YAPILDI (14. tur) — kullanıcı haklıydı, fark YEŞİL kanalda.**
      > Çubuğun gövdesi bizde (81,64,228), referansta (73,44,219); seçili
      > kartın yüzü bizde (107,101,249), referansta (93,70,245). Yeşilin 20-30
      > birim fazla olması moru lavantaya çekiyor, göz de bunu "daha açık"
      > diye okuyor. Gövde ayrıca alta doğru (60,34,180)'e söner — bizde bu
      > sönüm hiç uygulanmıyordu (sönüm katmanı çubuğun ekran dışına taşan
      > payını kapsıyordu, görünen alanda etkisi sıfırdı).

---

# ANA SAYFA (kritik)

- [x] **H1.** Karakter **avatar çerçevesi** güncellenecek / değişecek.

- [x] **H2.** Jeton ve kalp için sağdaki **artı (+) ekleme düğmesi** kötü
      görünüyor; güncellenecek.

- [x] **H3.** **Ayarlar düğmesi** güncellenecek; ikonu Magic Sort'tan hazır
      alınabilir.
      > *"karakter avatar çerçevesi güncellenicek değişecek; gold ve kalp
      > için sağda bulunan artı ekleme işareti butonu kötü gözüküyor
      > güncellenicek; ayarlar butonu güncellenicek, ikonu yine hazır
      > alınabilir magic shorttan"*
      >
      > **YAPILDI (14. tur) — üçü tek dilde.** Üç düğme de referansta AYNI
      > profili paylaşıyor: dört kenarı dolanan ince koyu kenar, üstte dar bir
      > parlaklık, aşağı sönen gövde, altta kalın koyu bir kalınlık.
      > Bizimkiler düz renkli yuvarlak karelerdi — "kötü görünüyor" denen şey
      > hacmin yokluğu. `MenuSprites.PlastikKare` bir kez yazıldı, üç palet
      > verildi:
      >   * ayarlar: kenar (44,8,23), tepe (113,81,250), gövde
      >     (105,75,246)→(91,61,214)  — kesit y 68..108
      >   * artı: kenar (12,62,10), tepe (129,250,91), gövde
      >     (105,249,56)→(38,184,15)  — kesit y 75..100
      >   * avatar: gövde (102,74,226)→(122,96,240), iç rim (56,34,158)
      > Avatarın İÇ PANOSU açık yapıldı: referansta çerçevenin içi
      > çerçeveden AÇIK (orada portre bir sahne karesi). Koyu bırakınca bizim
      > saydam zeminli karakterimiz çerçeveye karışıyordu.

- [ ] **H4.** Ana sayfadaki **arka plan görseli**: her şey bitince
      düğmelerle **renk uyumu** açısından bir görsel çözülecek.
      (SONA BIRAKILIYOR — diğer maddeler bitmeden bakılmayacak.)
      > *"ana sayfadaki arkaplan görsele, butonlar, her şey bitince çok daha
      > yakışacak renk uyum olarak bi görsel çözülecek"*

- [x] **H5.** **Oyna düğmesi zorluğa göre renk değiştirecek** (değişmiyorsa
      eklenecek). Orijinalde zor seviyede mor düğme var; bizdeki renk
      bilinmiyor, tespit edilecek.
      > *"buton kısmında zorluğa göre buton değişmiyorsa değişecek; örneğin
      > zor seviyede mor buton var orjinal oyunda, bizde hangi renk
      > bilmiyorum"*
      >
      > **ZATEN VARDI, DOĞRULANDI (14. tur).** `ApplyDifficulty` düğmeyi
      > zorluğa göre boyuyor: normal YEŞİL, Hard MOR, SuperHard KIRMIZI.
      > Sorunun cevabı: bizim moru `PlayPurple` = **(133,28,252)**.
      > Referansla karşılaştırmak için 52 menü karesinden mor düğmeli 5'i ve
      > yeşil düğmeli 4'ü ayrı ayrı ortalandı:
      >   * referans mor: tepe (146,45,242), gövde (111,4,242), dip (98,0,218)
      >   * referans yeşil: tepe (86,255,39), gövde (66,243,25), dip (32,200,10)
      > Bizim mor referansın tepesi ile gövdesi arasında — fark JPEG
      > sıkıştırmasının gürültüsü mertebesinde, değişiklik gerekmedi.

- [x] **H6.** Düğmenin **görünümüne ve boyutuna** dikkat edilecek.
      >
      > **ÖLÇÜLDÜ, DEĞİŞİKLİK GEREKMEDİ (14. tur).** Aynı maske iki tarafa:
      >     referans  gen %49,9  yük %18,1 (genişliğe oranla)  en/boy 2,76
      >     bizim     gen %52,0  yük %17,3                      en/boy 3,05
      > Fark %4 mertebesinde. (Bu ölçüm bir kez YANLIŞ çıktı: yeşil eşiği
      > ana ekranın ÇİMENİNİ de yakalayınca düğme %82,5 genişlik verdi.
      > Doygunluk eşiği daraltılınca gerçek sayı çıktı — bkz. DEVAM.md.)

- [x] **H7.** **BASILI DURUM GÖRSELİ YOK.** Klasik oyunlarda düğmeye
      basınca arkasında bir koyuluk olur, basıldığını hissettirir. Bizde
      tıklayıp basılı tutunca hiçbir şey değişmiyor. Eklenecek.
      > *"ayrıca butona tıkladığımızda klasik oyunlarda bulunan butonun
      > arkaplanında bir koyuluk olmalı, butonun basıldığını hissettirmek
      > için olan visual bizde yok; tıklıyoruz basılı tutuyoruz bir şey
      > değişmiyor, onu da ekleyelim"*


      **NE YAPILDI:** Düğmelerin çoğu `Selectable.Transition.None` ile
      kuruluyordu — hiçbir basılı geri bildirimi yoktu. Kalanlar `ColorTint`
      kullanıyordu ama o da yalnız HEDEF grafiği boyuyor; bu projenin
      düğmeleri çok katmanlı (gölge + koyu kenar + yüz + yazı) olduğu için
      bir katman kararıp diğerleri kalıyordu.

      `GameKit.UI.UiPressFeedback` yazıldı: basılınca ölçek %95,5'e iner ve
      TÜM alt grafiklerin rengi 0,84 ile çarpılır; bırakılınca geri döner.
      Şekil bağımsız çalışıyor (yuvarlak, hap, kare fark etmiyor) ve ayrı
      bir kaplama görseli gerekmiyor.

      **NASIL DOĞRULANDI:** Resume düğmesine kodla basıldı —
          once     : olcek 1,000  renk (8,32,3)
          BASILI   : olcek 0,955  renk (7,27,3)
          birakinca: olcek 1,000  renk (8,32,3)
      On ekranda kapsam taraması: **73/74 düğme**. Tek istisna ana ekrandaki
      görünmez `Hit` dokunma alanı — görseli olmadığı için kararacak bir şeyi
      yok, ölçeklenmesi de jeton şeridini oynatırdı.

      *Ders: kapatılan bir özellik, YERİNE KOYULMADIYSA eksiktir.
      `Transition.None` doğru bir karardı ama yarım kaldı.*

      Not: `P5`'in "basılı tutunca hareket etsin" kısmı da bununla kapandı.
---

# AYARLAR

- [x] **A1.** On/Off anahtarı: **ON iken yeşil, OFF iken KIRMIZI** düğme
      olacak. (Şu an OFF tarafı nötr mor.)

- [x] **A2.** "on / off" **yazıları fazla koyu** görünüyor. Orijinal rengin
      biraz daha düşük opaklıklı hâli gibi olmalı; daha çok benzetilecek.
      > *"ayarlar kısmında on off var ya, on iken yeşil buton, off olduğunda
      > kırmızı renk buton olmalı; ve o on off yazıları fazla koyu
      > gözüküyor, ona da dikkat edelim, orjinal rengin transpanı biraz daha
      > kısık hali gibi düşün, onu daha çok benzetelim"*

---

# KOLEKSİYON

- [x] **K1.** Görselin **dış kısımlarında beyazlıklar** var; kötü görünüyor,
      **kesilmiş gibi** duruyor. Düzeltilecek.
      > *"koleksiyon kısmında zaten çok bir şey yok ama görselin dış
      > kısımlarından beyazlıklar var, o da kötü gözüküyor, çok kesilmiş
      > duruyor; ona dikkat edelim düzeltelim"*

      Not: bu, `collection_book.png`'in matlanmasından kalan hâle olabilir —
      hafızadaki "saydamlık doğrulama tuzağı" (koyu zeminde bak, damalıda
      değil) burada birebir geçerli.

      > **YAPILDI (14. tur) — ve tahmin doğruydu.** Matlama alfayı doğru
      > çıkarıyor ama yarı saydam kenar piksellerinde ORİJİNALDE pişirilmiş
      > açık zeminin rengi kalıyor; koyu bir zeminde o kalıntı açık gri bir
      > hale olarak görülüyor. `collection_book.png`in yumuşak kenarında
      > beyaza yakın piksel oranı **%12,4**'tü.
      >
      > Çözüm kenarı SİLMEK değil, kenarın RENGİNİ içerden doldurmak: alfa
      > aynen korunuyor (silüet bozulmuyor), yalnız RGB en yakın OPAK
      > komşudan yayılıyor. Sonra oran **%3,3**.
      >
      > Bütün `Art/UI` klasörü aynı ölçütle tarandı (kenar ortalaması içten
      > 12 birimden fazla açıksa VE beyaza yakın oran %6'yı geçiyorsa) ve 20
      > varlıkta aynı kusur bulundu. Kullanımda olan 16'sı düzeltildi:
      > `collection_book`, `avatar_2..9`, `icon_lock`, `region_1..4`,
      > `podium_gold/silver/bronze`, `frame_board`. (Kalan dördü —
      > `frame_avatar`, `btn_square`, `bar_tabs`, `card_tab` — bu turda
      > prosedürel görsellerle değiştirildiği için artık kullanılmıyor.)
      > `logo_*.png` taramanın dışında bırakıldı.
      >
      > Yan fayda: L1a'nın avatar çerçeveleri ve L1b'nin kürsüleri de aynı
      > düzeltmeyi aldı.

---

# BÖLÜM SONU → MENÜ GEÇİŞİ

- [ ] **E1.** Görev bitip menüye geçince **toplanan paralar birikip jeton
      sayacına UÇARAK gelmeli**. Referansta var, kesin yapılacak.
      > *"şeyi de ekleyelim: görev bittikten sonra menüye geçince o toplanan
      > paralar birikip gold kısmına geliyor ya, referansta vardır onun
      > orjinali, onu kesin yapalım — yoksa, goldun gelme animasyonu"*

---

# YOLCULUK (Journey)

- [ ] **J1.** **ÖNCE MANTIK ÇALIŞSIN.** Belirli seviyelere gelince kalp,
      jeton, arka plan görseli ödülü veriyor; **bunların hepsi açılabilmeli,
      kullanılabilmeli**. Sorunluysa düzeltilecek.
      > *"journey kısmını da bi toparlayalım inceden. ilk olarak mantık
      > olarak çalışsın: belirli levellere gelince kalp coin arkaplan
      > görseli veriyor ya, bunların hepsini açabilelim kullanabilelim
      > çalışsın yani; o sorunluysa onu düzeltelim"*

- [ ] **J2.** Orijinale göre eksikler detaylıca incelenip kapatılacak.

- [x] **J3.** Geçilen bölüm için çıkan **tikler çok kötü**; Grand'ın
      tikleri kullanılacak.
      > *"en basitinden geçtiğimiz bölüm için çıkan tikler çok kötü bizde,
      > grandin tiklerini kullanalım"*
      >
      > **YAPILDI (14. tur).** Bizimki `check_green.png`di: kalın yan duvarı,
      > geniş spekülar parlaması ve altında gölgesi olan PLASTİK BİR NESNE
      > (512x461, en/boy 1,11). Referansın tiki DÜZ — kalem darbesi gibi,
      > yalnız hafif bir dikey gradyan ve altında ince koyu yeşil kontur.
      > Fark "iyi/kötü" değil DİL farkı: oyunun geri kalanı düz işaretler
      > kullanıyor, bizim tik tek başına 3B duruyordu.
      >
      > ÖLÇÜM (`m_010.jpg`, y 294..329 / x 384..431):
      > kutu 48x36 piksel = 96x72 birim (en/boy **1,33**), üst (105,233,94),
      > orta (66,230,39), alt (54,217,28), kontur (10,113,4).
      > Biçim iki kalın yuvarlak uçlu çizgi parçasının birleşimi olarak
      > `MenuSprites.Tick`te çizildi. Sonuç: en/boy **1,34**, ekran
      > genişliğinin **%11,0**'i (referans %10,8).

- [ ] **J4.** Sahnenin geneli cilalanacak.

---

# OYUN İÇİ — DURAKLAT (Pause)

- [x] **P1.** **BUG:** Oyunu durdurunca süre yukarı doğru gidip kayboluyordu
      (animasyon). **Başka bir telefonda süre yukarı gidiyor ama ekrandan
      kaybolmuyor, yukarıda takılı kalıyor.** Muhtemelen responsive
      olmamasından ([[G2]]); kötü bir bug, çözülecek.
      > *"oyun içi kısma geldiğimizde oyunu durdurduğumda süre yukarı doğru
      > gidiyor geliyordu ya animasyonda; bunu başka bir telefonda denedim,
      > süre yukarı gidiyor ama ekrandan kaybolmuyor yukarıda duruyordu. bu
      > responsive olmamasından kaynaklı muhtemelen ama kötü bir bug, bunu
      > çöz"*


      **NE YAPILDI:** `TimerHideRise` SABİT 210 birimdi. Hap `SafeArea`nın
      çocuğu; çentiği büyük telefonda güvenli alan HUD'u aşağı ittiği için
      hap daha alçaktan yola çıkıyor ve 210 birim ekranı terk etmeye
      yetmiyor. Mesafe artık `RiseToClearTop()` ile ölçülüyor (hapın alt
      kenarını kanvasın üst kenarına taşıyacak kadar + 32 birim pay); sabit
      yalnız alt sınır olarak kaldı.

      **NASIL DOĞRULANDI:** Çentik simüle edilip gereken mesafe ölçüldü —
      eski sabit HİÇBİR durumda yetmiyormuş:

          centik   0: gereken 249  eski 210 -> YETMIYOR (39 birim ekranda)
          centik  90: gereken 329  eski 210 -> YETMIYOR
          centik 150: gereken 382  eski 210 -> YETMIYOR
          centik 220: gereken 444  eski 210 -> YETMIYOR

      *Ders: sabit bir mesafe, DEĞİŞKEN bir başlangıçtan işe yaramaz.
      Ekran dışına çıkmak isteyen her animasyon mesafeyi ekranın
      kendisinden okumalı.*
- [x] **P2.** Duraklatınca **arka plan bir tık daha koyulaşmalı**.


      **NE YAPILDI:** Duraklat perdesi %82 -> %92.
      **NASIL DOĞRULANDI:** Sonuç paneli (%94) ile aynı ailede ama bir tık
      açık — duraklatta oyuncu tahtaya dönecek, sonuçta dönmeyecek.
- [x] **P3.** **EN KRİTİKLERDEN BİRİ:** "Pause" yazısı **panele bağlı
      değil**. Panel açılıyor, oynuyor, hareket ediyor ama Pause yazısı
      ondan ÖNCE geliyor ve panelle alakası yok. Düzeltilecek.
      > *"ve EN KRİTİK şeylerden biri: pause yazısı panele bağlı değil;
      > panel açılıyor oynuyor hareket ediyor ama pause texti ondan önce
      > geliyor ve alakası yok, bunu düzelt"*


      **NE YAPILDI:** `Title` ve `Close` `_pausePanel`in çocuğuydu, oysa giriş
      animasyonu (`Juice.CardEntrance`) yalnız `_pauseCard`a uygulanıyor —
      bu yüzden yazı kart büyürken ilk karede son yerinde beliriyordu.
      İkisi de karta bağlandı, kutuları ekran-göreliden kart-göreliye
      çevrildi (ekrandaki yer birebir korundu).

      **NASIL DOĞRULANDI:** Kartın ölçeği %50'ye indirildi; yazı ve çarpı
      ONUNLA birlikte küçüldü —
          Title %76,2..83,2 -> %64,4..67,9
          Close %73,2..77,8 -> %62,9..65,2
      Ölçek 1'e dönünce ikisi de eski yerine döndü. Ebeveyn artık 'Card'.
- [x] **P4.** "Pause" yazısı **tasarım olarak** da düzeltilecek: bahsedilen
      **kabartma/kontur** yapısında olacak, konumu da iyi ayarlanacak.


      **NE YAPILDI:** `SetOutline` yerine `UiTitleEmboss` (aşağıda G1).
      **NASIL DOĞRULANDI:** `_Reference/notes/pause_title.png` — arkada kalın
      parlak hale, önde ince koyu kenar.
- [x] **P5.** Duraklat panelinde **düğmelerin boyutu ve görünümü,
      ikonların boyutu, kapat düğmesinin yeri**, ve **üzerine basılı
      tutunca hareket etmesi** — hepsine dikkat edilecek.
      > *"yine duraklat kısmında butonların boyutuna görünümüne ikonların
      > boyutuna kapat butonunun yerine, üzerine basılı tuttuğumuzda hareket
      > etmesine vs dikkat edelim, bunların hepsini yapıcaz"*

      **13. TURDA KAPATILDI — SONRA GERÇEK REFERANSLA YENİDEN YAPILDI.**

      İlk turda "duraklat panelinin referansı yok" deyip Ayarlar ekranını
      vekil almıştım. Panel `Game over .mp4`ün **1. saniyesinde** varmış
      (`_Reference/notes/durak_avg.png`, t=1,0-1,55 arası 34 kare). Vekil
      %89'a varan hata verdi; hepsi gerçek kaynakla yeniden ölçüldü:
      başlık 55→106 birim, kapat 65→89, anahtar çipi 50→79, simge 61→75,
      etiket puntosu 40→55, düğmeler referans yerine indi.

      Ayrıca ölü bir kısıt bulundu: kod "kartın iç yüzeyi 0,121'de bitiyor"
      diyordu, o sınır 8. turda çıkarılan `panel_card` görselinden kalmaydı;
      gerçek değer 0,019 ve düğmeler için 100 birim boş yer vardı.

      İlk turda (vekille) yapılanlar:
      - **simge boyutları** eşitlendi (50/64/34 -> 62/61/61 birim); sorun
        kutuda değil sprite'ların farklı saydam dolgusundaydı
      - **etiketler ortalandı** (referansta dört etiketin merkezi aynı,
        başlangıçları farklı — yani ortalı, sola dayalı değil)
      - **anahtar yuvası** referans ölçüsüne döndü: eski kod çipi yuvadan
        taşırıyordu, ölçüm çipin yuvanın İÇİNDE olduğunu gösterdi
      - **basılı tutunca hareket** düzeltildi: geri bildirim çipin altındaki
        görünmez yüzeye takılıydı, artık kontrolün tamamına uygulanıyor
      - **kapat düğmesi** yeri zaten referansla birebirdi (12. turda
        karta bağlanmıştı), dokunulmadı

      Ayrıntı ve dersler: `docs/DEVAM.md`, 13. tur.

---

# "CAN KAYBEDECEKSİNİZ" ONAY PANELİ

- [x] **R1.** Yeniden oyna deyince çıkan **"can kaybedeceksiniz" paneli çok
      kötü**: panelin boyutu, dış kısmı yok. Orijinal referanstakine göre
      düzeltilecek, yazısı da hallolacak.
      > *"yeniden oyna diyince 'can kaybediceksiniz' kısmı, o panel de çok
      > kötü; panelin boyutu, dış kısmı vs yok, onu ayarlamamız gerekiyor.
      > orjinal referanstakiyle o paneli düzelt, textini filan hallet"*

      **KISMEN:** Perde %72 -> %90 (en açık perdeydi, tahtanın %28'i
      sızıyordu). Panelin boyutu, dış kısmı ve yazısı HENÜZ YAPILMADI.


      **NE YAPILDI:** Panel TEK düz bant + 9 birimlik ışık şeridiydi — "dış
      kısmı yok" denen şey buydu: kenarı, kademesi, ayağı olmayan bir
      dikdörtgen. `ContinueOffer` aşama 2 ile AYNI çerçeveye alındı (beş
      bant: Rim/Header/Well/Body/Foot) çünkü ikisi aynı soruyu soruyor —
      kırık kalp, "1 can kaybedeceksin", yeşil onay. Aşama 2'nin bantları
      `Game over .mp4` 18. saniyeden ÖLÇÜLMÜŞTÜ; bu panelinkiler
      ölçülmemişti, yani ölçülü olanı çoğaltmak doğru yön.
      Yazı da aşama 2 ile aynı: "You'll lose 1 life!", 40 punto.
      Perde ayrıca %72 -> %90.

      **NASIL DOĞRULANDI:** `CreateRetryPreview` eklendi (önizlemesi yoktu);
      iki panel yan yana yakalandı — `_Reference/notes/retry_vs_stage2.png`.
      Bantlar, başlık hizası, kalp, uyarı ve düğme birebir örtüşüyor.

      *Ders: iki ekran aynı cümleyi kuruyorsa aynı ağızdan kursun.*
---

# GENEL (devam)

- [x] **G4.** **KABARTMALI YAZI GÖRÜNÜMÜ NEREDE VARSA KULLANILACAK.**
      Kullanıcı bunu üçüncü kez söylüyor; tek tek ekran saymak yerine
      genel kural olarak alınacak.
      > *"bak aynı şeyi söylüyor gibi oluyorum ama o kabartmalı text
      > görünümü nerede varsa kullanalım"*

      Not: [[G1]] (başlık konturu) ile aynı ailedendir; ikisi tek bir
      yazı-stili altyapısıyla çözülmeli.

      **NE YAPILDI:** G1 ile aynı altyapı (`GameKit.UI.UiTitleEmboss`).
      Uygulandığı yerler: `MenuPage.Header` (profil, sıralama, koleksiyon,
      ayarlar), `JourneyScreen` ("Journey"), `GameplayScreen` ("Pause").

      **MAĞAZAYA UYGULANMADI** — kullanıcı kararı: *"shop kısmında o outline
      kabartma yok, onu kaldır shop kısmından."* Denendi, geri alındı;
      "Shop" kendi ince konturunda kaldı.

---

# BÖLÜM BAŞARISIZ (Level Failed) EKRANI

- [x] **F1.** Ekran **çok kötü, patlamış**. **ÇOK ACİL.**
      > *"level failed ekranı da çok kötü patlamış, bunu da düzeltmemiz
      > lazım çok acil"*

      Ekran görüntüsünde görülen: kartın ARKASINDA hizasız koyu bir bant
      duruyor, kırık kalp o bandın üstüne biniyor, kart ile başlık arası
      kopuk.


      **NE YAPILDI:** Aynı kök sebep O1 ile ortak. Sonuç panelinin perdesi
      %84'tü, yani tahtanın %16'sı sızıyordu; kartın üstündeki "hizasız koyu
      bant" ve içindeki kırmızı dikdörtgen bir arayüz öğesi değil, sızan
      TAHTAydı. Perde %94'e çıkarıldı (`ContinueOffer` ile aynı değer).

      **NASIL DOĞRULANDI:** Sızma %16 -> %6, yani 2,7 kat azaldı. Aynı
      sahneyi kapatan iki panel artık aynı opaklıkta.
- [x] **F2.** **Alt kısım (booster şeridi):** power-up'ların arka planı,
      düğmeleri, **ikon boyutu**, altındaki **para göstergesi** — toparlanıp
      düzgün hâle getirilecek.
      > *"yine alt kısım poweruplar arkaplanı, butonlar, ikon boyutu,
      > altındaki para kısmı göstergesi vs onları da toparla düzgün hale
      > getir"*

      **13. TURDA KAPATILDI.** `CreatePowerUpPreview` eklenip şerit tek
      başına yakalandı; referans `Game over .mp4` 2,3-3,1 sn (48 kare
      ortalandı). Düğme 157x137 -> 194x138 (referansınki KAREYE YAKIN
      DEĞİL, yatay), şerit aralığı %23,9-%73,8 -> %18,1-%81,8, fiyat hapı
      80 -> 43 birim, ikon küçültülüp kuyunun içine alındı ve yeşilin
      yukarıdan aşağı düşüşü (%15 -> ~%47) iki tonlu kuyuyla verildi.
      Ayrıntı: `docs/DEVAM.md`, 13. tur.

---

# BÖLÜM GEÇME KUTLAMASI (Win Celebration)

- [x] **W1.** **KRİTİK:** Leveli geçince çıkan **"BLOCK OUT!" yazısı çok
      kesik kesik**. Orijinali nasıl alınabiliyorsa alınacak — videodan
      alınacak, kırpılacak, olmazsa başka bir çözüm bulunacak; ama bu kadar
      kötü görünmeyecek. **Çok önemli.**
      > *"leveli geçince çıkan block out yazısı çok kesik kesik ... block
      > out yazısının o kesikliğini çözmeni istiyorum; orjinalini nasıl
      > alabiliyorsan al, videodan al, kırp, olmadı bir çözüm bul ama bu
      > kadar kötü gözükmesin, o kısım çok önemli"*


      **NE YAPILDI — sebep dilimleme betiğindeydi.** Logo altı harf PNG'sinden
      kuruluyor (`tools/slice_logo.py`). Ölçüldü:

          logo_game (bütün logo)  yarı-saydam piksel 4562  (%2,4)
          logo_l                                       0   (%0,0)
          logo_c                                       0   (%0,0)
          logo_out                                     9   (%0,0)

      Yani harflerin kenarında TEK BİR ara ton yoktu — merdiven basamağı.
      Sebep tek satır: `letter[own] = data[own]`; `own` boolean olduğu için
      her piksel ya tamamen kopyalanıyor ya tamamen atılıyordu ve `own`
      yüksek alfa istediği için dış kenardaki yumuşak tüy hiçbir harfe
      girmiyordu.

      **İKİ ADIMDA ÇÖZÜLDÜ (ilk denemede yan etki çıktı):**
      1. Maske genişletilip bulanıklaştırıldı ve ağırlık olarak kullanıldı →
         harf kenarları yumuşadı (%0,0 -> %8-10).
      2. Ama zeminlerde DİKİŞ çıktı: iki komşu zeminin paylaştığı sınırda iki
         yarı-saydam kenar üst üste binince ince koyu çizgi kalıyor
         (452 piksel). Zeminlerin alfası artık BİRLEŞİM siluetinin
         yumuşatılmış hâlinden alınıyor; parça o alfayı yalnız kendi alanında
         kullanıyor, iç sınırlar ikili kalıyor.

      **NASIL DOĞRULANDI:**

          harfler   %0,0  ->  %8,4 / %9,9 / %6,0 / %9,7 / %9,6 / %8,9
          zeminler                %1,0-1,3 (yalnız dış siluet)
          dikiş     452 piksel (%0,243)  ->  41 piksel (%0,022)

      Görsel: `_Reference/notes/logo_check3.png`. On üç parça `Uncompressed`
      sprite olarak yeniden içe alındı (sıkıştırma kenar yumuşatmasını
      bozuyor).

      *Ders: yumuşaklık BÜTÜNE aittir, parçaya değil. Bir siluet parçalara
      bölünüyorsa dış kenarın yumuşaklığı bölünmeden ÖNCE hesaplanmalı; her
      parçaya ayrı uygulanınca iç sınırlarda olmayan bir boşluk icat
      ediliyor.*

      NOT: Kullanıcının "kesik kesik" ifadesi ANİMASYON anlamına da
      gelebilir (harf harf giriş). Kenar sorunu ölçülüp giderildi; giriş
      animasyonunun ritmi W2 ile birlikte ayrıca bakılacak.
- [ ] **W2.** **Konfeti / havai fişek sırası ve uyumu** orijinaldekiyle
      aynı değil; düzeltilecek.

- [ ] **W3.** **Üst üste tıklayınca hızlandırılıp atlanabilmeli** (skip) —
      her oyunda olan özellik.
      > *"ve tabi üst üste tıkladığımızda hızlandırıp skipleyebilelim, her
      > oyunda olan özellik"*

---

# "TIME'S UP!" DEVAM TEKLİFİ PANELİ

- [x] **O1.** Panel **patlamış**: saat ikonu kutusuna sığmıyor, panelin
      dışına taşıyor ve arkasındaki tahta karoları görünüyor; üstte hizasız
      kırmızı bir blok duruyor. Çok kötü görünüyor.
      > *"bak bu kısmı da patlamış, ne kadar kötü gözüküyor ikon burada,
      > bu kısmı da çöz"*

      Ekran görüntüsünden okunanlar (ölçülecek):
      - [x] **O1a** saat ikonu panelin kutusundan taşıyor / kırpılıyor
      - [x] **O1b** panelin arkasında tahta karoları ve kırmızı blok görünüyor
      - [x] **O1c** "Time's Up!" başlığı ve "+30 seconds" yazısının konumu/boyutu
      - [x] **O1d** "Add Time" düğmesi ve jeton göstergesi

      **13. TURDA KAPATILDI.** Altı öge referansla yan yana ölçüldü
      (`_Reference/notes/sw_avg.png`). Jeton sayacı grubu %3,5 yukarıdaydı,
      jeton %33 küçüktü, hap kısaydı, "+30 SECONDS" tamamı büyük harfti ve
      düğmenin iç yazısı düğmeyi dolduruyordu (%95,7, referans %81,1).
      Hepsi ölçülüp düzeltildi; kalan farklar ekran oranı ve dil artefaktı.
      Ayrıntı ve dersler: `docs/DEVAM.md`, 13. tur.


      **NE YAPILDI:** `BuildStage1`'de HİÇ PANEL YOKTU — başlık, saat, yazı ve
      düğme doğrudan perdenin üstüne konuyordu. Perde %94 mat olduğu için
      arkadaki TAHTA görünüyordu; kullanıcının "panel" sandığı koyu
      dikdörtgen tahtanın kendisi, içindeki kırmızı blok da gerçek bir
      bloktu. Aşama 2'nin bant yapısı (Rim/Header/Well/Body/Foot) birebir
      uygulandı; jeton sayacı, çarpı, başlık, saat, yazı ve düğme aşama 2
      ile aynı hizaya alındı. Saate `preserveAspect` eklendi.

      **NASIL DOĞRULANDI:** İki aşama yan yana yakalandı
      (`_Reference/notes/offer_stages.png`) — panel bantları opak, arkadan
      hiçbir şey sızmıyor, iki sayfa aynı çerçeveyi paylaşıyor.
---

# EN SONDA — QA

- [ ] **Q1.** Her şey bittikten sonra **bütün bölümler baştan sona
      oynanacak**. Sıkıntı var mı kontrol edilecek — **çalışmayan /
      açılmayan bazı bölümler vardı**, hepsi düzeltilecek.
      > *"tüm her şeyi bitirdikten sonra bunların hepsini bitirirsen QA test
      > yap, bütün levelleri baştan sona oyna kontrol et sıkıntı var mı;
      > çünkü çalışmayan bazı leveller vardı, açılmayan bazı leveller,
      > onların hepsini de düzelt"*

      Not: `GECE-PLANI.md` 16. maddesiyle aynı iş. Çözücü (`LevelValidationTool`)
      zaten koşturuldu ama **gerçek oynanış hiç sürülmedi**; "açılmayan
      bölüm" tam olarak çözücünün yakalayamayacağı türden bir hata.

---


---

## SONRAYA BIRAKILANLAR

- [ ] **G5.** Kabartmalı başlıkların **boyutu ve konumu** bazı ekranlarda
      ayarlanacak (kullanıcı özellikle **Pause**'u işaret etti).
      > *"bazı yerlerde boyutu değişecek, mesela pause'da konumu boyutu
      > değişecek ama bu sonranın işi, şuanda güzel oldu devam edebiliriz"*

---

## DURUM

**Toplam 59 madde** (55 + O1a–O1d).

### Kullanıcının çalışma talimatı (2026-08-23)

> *"bak daha benim aklıma gelmeyen sorunlar da olabilir veya senin
> çalışırken farkedeceğin problemler — onları da çöz. amaç artık bu klon
> projeyi sorunsuz eksiksiz bir şekilde tamamlamak ve oldukça benzetilmiş
> orjinal oyunun halini yapabilmek. o yüzden bu yazdığımız hiçbir şeyi es
> geçmemeni ve eksiksiz yapmanı istiyorum; yani yaptığın her şeyi kontrol
> et, doğrula, oldu mu diye kendin bak ve tikle kendi listende. çünkü ben
> dışarıda olucam ve remote bir şekilde telefondan takip edicem."*

Yani:
1. **Listede olmayan ama çalışırken görülen sorunlar da çözülecek** ve
   buraya YENİ MADDE olarak eklenecek.
2. Her madde **kendim doğrulayıp** tikleyeceğim; kullanıcı uzaktan takip
   ediyor, ölçüm ve kanıt madde altına yazılacak.
3. Bilgisayar ve Unity açık kalacak.

### Aciliyet (kullanıcının kendi vurguları)

| Öncelik | Madde |
|---|---|
| ÇOK ACİL | **F1** level failed ekranı patlamış · **O1** Time's Up paneli patlamış |
| EN KRİTİK | **P3** Pause yazısı panele bağlı değil |
| KRİTİK | **G2** responsive · **W1** BLOCK OUT! yazısı kesik |
| KESİN | **E1** jeton uçma animasyonu |

### Çalışma sırası (kendi kararım — patlamış olanlar önce)

1. F1/F2 — bölüm başarısız ekranı
2. O1 — Time's Up paneli
3. P1–P5 — duraklat (P3 en kritik)
4. R1 — can onay paneli
5. G1/G4 — yazı stili altyapısı (tek seferde bütün başlıklar)
6. W1–W3 — kutlama
7. E1 — jeton uçma
8. M* — mağaza (19 madde)
9. L*, N*, H*, A*, K*, J* — ekran ekran
10. G2 — responsive (geniş dokunuş, düzen oturduktan sonra)
11. G3, U1 — varlık işleri
12. Q1 — QA, en sonda
