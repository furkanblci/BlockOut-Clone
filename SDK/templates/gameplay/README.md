# Oyuna özel örnekler

Bu klasördeki `.txt` dosyaları **SDK'ya girmeyen ama deseni değerli olan**
Block Out! kodudur. Derlenmezler (uzantıları bilerek `.txt`), kopyalanıp
uyarlanmak için buradalar.

| Dosya | Desen | Neden SDK'da değil |
|---|---|---|
| `NewItemPanel.cs.txt` | **Yeni mekanik tanıtımı: kart değil SPOT IŞIĞI.** Tahta yerinde kalır, üstüne koyu perde iner, yalnız tanıtılan öğe aydınlatılır. Bir mekanik bir kez, bölüm başına en fazla bir tanıtım. | Seçim mantığı (`LevelHas`) ve çizimi tamamen Block Out mekaniklerine (buz, perde, katman, kapı sırası) bağlı |
| `TutorialOverlay.cs.txt` | Öğretici katman: ilk bölümde legal bir hamleyi bulup parmakla gösterir | Hamle bulma tamamen oynanışa ait |
| `AudioService.Bind.cs.txt` | Oynanış olaylarını sese bağlama + **çoklu olay tuzağı** (`PlayOnce` neden var) | Olay adları oyuna özel |
| `UiPanelCatalog.GameplayEntries.txt` | Oynanış panellerinin tasarım penceresi kayıtları + **anahtar meselesi** | Oyunun ekranlarına bakıyor |

## Nasıl kullanılır

1. Deseni oku — `DERS (...)` yorumları neden öyle yazıldığını anlatıyor.
2. Kendi oyunun için yeniden yaz; kopyala-yapıştır etme.
3. `UiPanelCatalog.GameplayEntries.txt` bir istisna: içeriği doğrudan
   `UiPanelCatalog.GameEntries` kancasına uyarlanabilir.

## `NewItemPanel` neden önemli

İlk tasarım refleksi bir tanıtım kartı yapmaktır: başlık, açıklama, kapat
düğmesi. Referans oyunlar öyle yapmıyor.

> Kart "şimdi sana bir şey anlatacağım" der ve oyuncuyu oyundan koparır;
> spot ışığı "şuna bak" der ve oyuncu hâlâ tahtaya bakıyordur. Üstelik
> tanıtılan şeyin **nerede yaşadığını** da bedavaya öğretir.

Bu tek karar, tanıtımların oynanabilirlik hissini bozmamasının sebebi.
