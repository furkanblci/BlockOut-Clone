# Tuzaklar — bu projede kanla öğrenilenler

> **SDK'nın en pahalı dosyası bu.** Aşağıdaki her madde, Block Out! klonunda
> gerçekten saatler yakmış bir hatanın kaydı. Kod yeniden yazılabilir; bu
> liste yeniden yazılamaz — yalnız yeniden yaşanarak öğrenilir.
>
> Yeni projeye başlarken **önce bunu oku.** Bir hata ararken de önce buraya bak:
> yaşadığın şey büyük ihtimalle burada yazıyor.

Ortak tema, hepsinde aynı: **ölçüm aracının kendi yalanı, ölçtüğü şeyin
hatasından daha tehlikelidir.**

---

## A. Doğrulama tuzakları — "değişiklik işe yaramadı" sandığın anlar

### 1. Oynatma modunda betikler DERLENMEZ

Unity oynatma (play) modundayken kaydedilen C# dosyaları derlenmez. Bu haldeyken
alınan **her** ekran görüntüsü ve **her** sayısal ölçüm, değişiklikten ÖNCEKİ
derlemeye aittir. Ölçüm doğru çalışır — ölçtüğü şey eskidir.

**Belirti:** "Düzeltmeyi yaptım ama hiçbir etkisi yok."
**Bu, sahte bir olumsuz sonuçtur ve seni yanlış teşhise götürür.**

**Kural:** ölçmeden önce `EditorApplication.isPlaying` yazdır. Doğruysa çık,
`AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate)` +
`CompilationPipeline.RequestScriptCompilation()` çağır, konsolu hatasız gör,
sonra ölç.

### 2. Bayat derleme — düzenle, tazele, AYRI komutta ölç

Bir düzenleyici komutu (MCP `RunCommand` gibi) kendi parçacığını **o anki**
derlemeye karşı derler. Dosyayı düzenledikten hemen sonra ölçüm komutu
çağırırsan değişiklikten önceki kodu ölçersin.

**Yaşanmış:** perde parıltısının alfası üç kez değiştirildi, üç kez "hiçbir şey
değişmedi" ölçüldü. Kod doğruydu; ölçülen derleme eskiydi.

**Kural:** düzenle → **yalnız** `AssetDatabase.Refresh(ForceUpdate)` yapan bir
komut → **ayrı** bir komutta ölç.

**İkinci tuzak, aynı aileden:** komut yalnız KENDİ parçacığını derler. PROJE
derlemesi hatalıysa komut yine "başarıyla çalıştı" der ve eski derlemeyle
koşar. Pratik hile: doğrulama komutunda **yeni eklediğin sembole dokun**
(`MenuSprites.Sparkle != null` gibi) — derleme bayatsa komut sessizce geçmek
yerine "does not contain a definition" diye patlar.

### 3. Statik önbellek, kaynağı değişince geçersizdir

`UiSprites` / `MenuSprites` / materyal önbellekleri statik. `ClearCache()`
çağrılmazsa renk veya alfa değişikliği domain reload'a kadar **hiç görünmez**.

Aynı ders asset tarafında da geçerli: `UiSkin` arama tablosu bir kez kurulup
ScriptableObject örneğinde saklanıyor ve o örnek düzenleme ile oyun modu
arasında yaşamaya devam ediyor. Yeni sprite'lar eklendiğinde tablo eskisini
tuttu ve yeni görseller "yok" göründü — dosyalar yerindeyken.

**Önbelleği besleyen veri değiştiğinde önbelleği düşürmek, yazılmaması en
pahalı iki satırdan biri.**

### 4. Ekran görüntüsü KARANLIK bölgede yalan söyler

Bir PNG'yi görüntüleyerek incelerken karanlık bölgeler olduğundan **açık**
görünür (görüntüleyici parlaklık eğrisi uyguluyor).

**Yaşanmış:** tahtanın koyu lacivert ızgara çizgileri ekranda açık gri çıktı;
"ızgara materyali bozuk" diye bir saat arandı. Piksel piksel tarandığında 90'ın
üstünde tek bir nötr piksel yoktu — çizgiler gerçekte `#0A0A22` civarındaydı.

**Kural:** koyu bir yüzeyin rengi/kontrastı söz konusuysa **önce `numpy` ile
ölç.** Görüntüye yalnız GEOMETRİ için güven (konum, boyut, şekil, hizalama).

### 5. Saydamlığı ASLA dama tahtası üstünde doğrulama

Görsel üreteçleri "saydam" çıktıyı gri dama tahtası olarak **piksellere
pişirir**. Kesilmiş görseli dama tahtası önizlemesi üstüne koyarsan, artık
kalan sahte dama tahtası gerçek saydamlıktan ayırt edilemez.

**Yaşanmış:** beş ikonun (saat, küre, kilit, mağaza, kupa) kapalı iç delikleri
pişmiş dama tahtasıyla doluydu ve bir saat boyunca görünmedi; ancak oyunda koyu
sekme çubuğuna konunca ortaya çıktı.

**Kural:** sonucu **düz koyu bir renk** üstüne bindir — `(35,30,70)` gibi.
Sayısal kontrol de olur: nötre yakın (kroma ≤ 12), orta parlaklıkta (40–155),
görüntünün %0,1'inden büyük bölgeler oluşturan opak pikseller neredeyse kesin
artık dama tahtasıdır.

### 6. Yakalanan kare MAGENTA çıkıyorsa suçlu kod değil

Düzenleyicide bir shader **varyantı** henüz derlenmemişse Unity o nesneyi
magenta "bekliyor" rengiyle çizer ve `Camera.Render()` derlemeyi beklemez.

**Ölçüldü:** aynı sahne art arda üç kez yakalandı, üçünde de **tam 18.476**
magenta piksel — yani geçici değil, takılı kalmış bir durum.

**Çözüm:** SDK'daki `EditorCapture.EnsureSynchronousShaders()` — yakalayan her
aracın statik kurucusundan **ve** her `Render()` öncesinden çağır. Ayarı geri
açma: kapatıp aynı çağrının sonunda geri açmak bir sonraki yakalamayı yeniden
bozuyor.

### 7. Odaklanmamış editör KARE İŞLETMEZ

MCP ile oynatma modunu sürerken editör odakta değilse `Time.frameCount` 1'de
kalır; `Awake`/`Start`/`Update` hiç koşmaz. Sahneyi yüklersin, nesnelerini
bulamazsın ve "hata var" sanırsın. Yok — henüz hiçbir şey çalışmadı.
`PlayerSettings.runInBackground = true` bunu **çözmez**.

**Çözüm:** `EditorApplication.Step()` — ama **çağıran komutun içinde etki
etmez.** Adımlar kuyruğa girer ve `Execute` döndükten sonra işlenir. Yani "30
kare ilerlet, sonra yakala" tek komutta yazılırsa adımlardan ÖNCEKİ kareyi
yakalarsın. **Her zaman böl: bir komut eylem yapar, sonraki gözlem.**

### 8. Düğmeyi `onClick.Invoke()` ile test etme

`Invoke` ışın taramasını (raycast) atlar ve **parmağın asla ulaşamayacağı**
düğmeler için "çalışıyor" der. Bu, kaynak projede üç ayrı düzeltmeyi kandırdı.

Gerçek dokunuşu benzet:

```csharp
var corners = new Vector3[4]; rect.GetWorldCorners(corners);
var data = new PointerEventData(EventSystem.current)
    { position = (corners[0] + corners[2]) * 0.5f };
var hits = new List<RaycastResult>();
EventSystem.current.RaycastAll(data, hits);
var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
data.pointerPress = target;
ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
```

---

## B. Unity ve C# tuzakları

### 9. `Mathf.SmoothStep` GLSL'in `smoothstep`'i DEĞİLDİR

Unity'de `Mathf.SmoothStep(from, to, t)` iki **değer** arasında yumuşatılmış
interpolasyon yapar. GLSL'in `smoothstep(edge0, edge1, x)`'i ise x'i eşikler.

Yani `1f - Mathf.SmoothStep(63f, 64f, distance)` yumuşak kenarlı bir maske
değildir; `1 - ~64` = büyük bir negatif sayıdır ve bütün doku alfa 0 çıkar
(ya da işaret bir çarpımda dönerse alfa 1).

**Yaşanmış — iki sessiz hata:** mağaza sprite'larının bütün maskeleri
(kapsül, ∞ halkası, tente fistoları) görünmez çıktı; ve `UiSprites.cs` **aylarca**
her yuvarlak panelin arkasında %25 alfalı kare bir hayaletle yayınlandı —
"arayüz biraz kirli duruyor" diye okundu, hiç izi sürülmedi.

**Kural:** prosedürel sprite/maske kodunda yerel yardımcı kullan:

```csharp
static float Step(float e0, float e1, float x)
{
    float t = Mathf.Clamp01((x - e0) / Mathf.Max(1e-5f, e1 - e0));
    return t * t * (3f - 2f * t);
}
```

Bir prosedürel sprite tamamen saydam ya da tamamen opak çıkıyorsa **önce buna
bak** — dokuyu PNG'ye dök ve alfa min/max'ını oku, ekrandan tahmin etme.

### 10. `Start`'ların sırası TANIMSIZDIR

Elle yapılan arayüz düzeltmeleri doğrudan `Start` içinde uygulanıyordu ve hiç
uygulanmadı — üstelik hata da vermedi, çünkü ortada hata yoktu: ekranlar o an
henüz kurulmamıştı, gezilecek çocuk yoktu.

**Kural:** "benden sonra kurulan" bir şeyi beklemenin tek güvenli yolu **bir
kare geçirmek** (`yield return null`).

### 11. Bir sistemin "hazır" olması, BAĞLI olması demek değil

`AudioService.PlayMusic` yazılmış, ayarlara bağlanmış, duraklat panelindeki
anahtara takılmıştı — ama projede onu **çağıran tek bir satır yoktu.** Ses
dosyaları konsa bile müzik hiç çalmayacaktı; anahtar da hiçbir şeyi
açıp kapatmayacaktı. Sessizliğin sebebi "dosya yok" sanıldığı için aranmadı bile.

**Kural:** bir özelliği bitirdim demeden önce **zincirin ilk halkasını** ara:
bunu kim çağırıyor?

### 12. Olay sayısı ≠ ses sayısı

Tek bir hamlede aynı olay birden çok kez yayınlanabilir (tahtadaki beş buzlu
bloğun sayacı aynı karede beşer kez azalıyordu). Beş özdeş klip aynı anda
çalınca genlik beş katına çıkar: hem kırpma hem "çatırtı bombası".

Sentezlenmiş cılız seslerle bu fark edilmez; gerçek, tepe seviyeli kliplerle
ilk denemede duyulur. SDK'daki `AudioService.PlayOnce` 60 ms'lik bir çakışma
sınırı koyuyor (60 fps'te dört kare — aynı hamlenin olayları tek sese iner,
gerçek ardışık hamleler en hızlı hâlinde bile 150 ms'den seyrek).

**Kural:** bir olaya ses bağlarken "kaç kere yayınlanıyor" sorusu, "hangi ses"
sorusu kadar önemli.

### 13. Kod stripping cihazda öldürür, editörde öldürmez

APK'da bölümler hiç yüklenmedi; editörde her şey çalışıyordu. Kök sebep
**motor kodu stripping**: shader'lar "Always Included" listesinde değildi ve
`CreatePrimitive`'in ihtiyaç duyduğu `MeshCollider` sınıfı atılmıştı.

**Kural:** cihazda çalışmayıp editörde çalışan her şeyde önce `link.xml` ve
"Always Included Shaders" listesine bak. Duman testini **gerçek cihazda** yap.

---

## C. Ürün ve süreç tuzakları

### 14. Test aracı ASLA görünür bir düğme olmaz

Oynanış HUD'ının sol üstünde duran bir "bölüme atla" düğmesi, testçilerin
oyunu gerçekten oynamamasına yol açtı — gerçek ilerleme hiç sınanmadı. Ve
yayın yapısına sızsaydı oyunu bozardı.

**Kural:** hata ayıklama aracı ya gizli bir harekete bağlanır (SDK'da: sol üst
köşeye 2 saniyede 5 dokunuş, editörde `F8`) ya da düzenleyici penceresinde
yaşar. Tamamı `#if DEVELOPMENT_BUILD || UNITY_EDITOR` içinde olur.

**Yan kural:** her liste **kaydırılabilir** olmalı. İlk sürüm 50 bölümü tek
ekrana yığdı; son satırlar taşıp seçilemez oldu — araç, var olma sebebi olan
tek işte başarısız oldu.

**Yan kural 2:** yayın servislerine hata ayıklama metodu ekleme. "Hepsini aç"
işlemi `ProgressService.UnlockAll` diye yeni bir metot değil, mevcut
`Save.Mutate` üzerinden yazılır.

### 15. Üç sekme, üç AYRI liste göstermeli

Sıralama ekranında tek bir veri dizisi vardı ve üç sekme de onu gösteriyordu;
tıklayınca yalnız sekmenin rengi değişiyordu. **Üç kapı açıp üçünü de aynı
odaya çıkarmak, düğmeyi hiç koymamaktan daha kötüdür** — oyuncu bir süre fark
aradıktan sonra oyunun bozuk olduğuna karar veriyor.

### 16. Sonradan doldurulacak her yazının KURULUMDA da varsayılanı olmalı

"CLAIM" etiketi yalnız `Refresh()` içinde yazılıyordu; panel `Refresh`'siz
açıldığında düğme **boş yeşil bir çubuk** olarak duruyordu.

### 17. Bir kutu KAÇ ödül gösteriyor?

Günlük ödül paneli her gün için tek simge çizip altına **her zaman** jeton
sayısını yazıyordu. 5. gün "kalp + 200" görünüyordu; 200 kalp değil jetondu,
kalp 1 taneydi. **Panel oyuncuya yanlış bilgi veriyordu.** Çözüm: bir günün
hediyeleri çip olarak yan yana dizilir, her çipin kendi simgesi ve kendi
sayısı olur.

### 18. Tanıtım KART değil, SPOT IŞIĞIDIR

Yeni bir mekaniği tanıtmanın refleks çözümü bir kart yapmaktır: başlık,
açıklama, kapat düğmesi. Referans oyun öyle yapmıyor — tahta yerinde kalıyor,
üstüne koyu bir perde iniyor ve **yalnızca tanıtılan öğe aydınlatılıyor.**

Fark önemli: kart "şimdi sana bir şey anlatacağım" der ve oyuncuyu oyundan
koparır; spot ışığı "şuna bak" der ve oyuncu hâlâ tahtaya bakıyordur. Üstelik
tanıtılan şeyin **nerede yaşadığını** da bedavaya öğretir.

Ek iki kural: bir mekanik **bir kez** tanıtılır (PlayerPrefs'e yaz), ve bir
bölümde **en fazla bir** tanıtım gösterilir.

---

## D. Çalışma alışkanlıkları

### 19. Ölçüm > ekran görüntüsü

Sayısal doğrulama (sahne ağacı dökümü, başsız benzetim, piksel ölçümü) hem
**daha ucuz** hem de kaynak projede daha çok hata yakaladı. Ekran görüntüsünü
son bir akıl sağlığı kontrolü olarak kullan, birincil döngü olarak değil.

### 20. Aynı ağaçta iki kişi çalışıyorsa `git add -A` YASAK

Kaynak projede bu, bir seferde karşı tarafın dokuz yeni + dört değişmiş
dosyasını yanlış commit'e süpürdü. Yol vererek aşamala:

```bash
git add Assets/_Project/Scripts/Runtime/UI/Foo.cs docs/BAR.md
```

Kazara olursa: `git reset --soft HEAD~1`, ardından `git restore --staged <yol>`
— çalışma ağacına dokunmadan düzeltir.

**Bayat hata tuzağı:** karşı taraf dosyasını kaydetmeden derlersen konsolda
ONUN yarım kodundan hatalar görünür. Satır numaraları kayıksa hata bayattır;
yeniden derletip bak, onun koduna dokunma.

### 21. Bulguları NUMARALI bir dosyaya dök

Kaynak projede altı tur cihaz testi yapıldı ve her tur `docs/APK-BULGULARI-N.md`
diye numaralı bir dosyaya yazıldı (toplam ~250 madde, hepsi kapatıldı).
Numara olmadan "şu düğme kötü" geri bildirimleri kaybolur; numarayla her biri
tek tek kapanır ve kapandığı doğrulanır. Ayrıntı: `07-IS-AKISI.md`.
