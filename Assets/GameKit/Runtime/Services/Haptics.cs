using UnityEngine;

namespace GameKit.Services
{
    /// <summary>Titreşimin şiddeti. Oyun "ne oldu"yu söyler, servis nasıl titreyeceğine karar verir.</summary>
    public enum HapticStrength
    {
        /// <summary>Hafif dokunuş — seçim, tutma, küçük onay.</summary>
        Light,
        /// <summary>Orta — bir şey yerine oturdu, bir engel kırıldı.</summary>
        Medium,
        /// <summary>Ağır — bölüm bitti, büyük ödül.</summary>
        Heavy
    }

    /// <summary>
    /// Dokunsal geri bildirim.
    ///
    /// DERS (yeniden kullanılabilirlik = bağımlılığı tersine çevirmek): Bu servisin
    /// ilk hâli oyunun `BoardEvents`'ine abone oluyordu — yani titreşim kodu
    /// "buz kırıldı" diye bir kavramı biliyordu ve başka hiçbir oyunda
    /// kullanılamazdı. Şimdi servis yalnızca "titret" biliyor; HANGİ olayda
    /// titreneceğine oyun karar veriyor. Bağımlılık oyundan kite doğru akıyor,
    /// tersi değil.
    ///
    /// DERS (platform farkı): Editörde ve PC'de titreşim yoktur; kod her
    /// platformda DERLENMELİ ama yalnızca mobilde iş yapmalıdır. `#if` ile
    /// ayırmak, çalışma anında platform sorgulamaktan hem hızlı hem temizdir.
    ///
    /// NOT: `Handheld.Vibrate` süresi ayarlanamaz (Android'de ~500 ms) ve
    /// şiddet ayrımı yapamaz. Şiddet kademeleri şimdilik "titret / titretme"
    /// eşiğine dönüşüyor; ileride yerel eklenti gelirse DEĞİŞECEK TEK YER burası.
    /// </summary>
    public sealed class Haptics : MonoBehaviour
    {
        /// <summary>Oyuncunun ayarı. Kapalıyken hiçbir çağrı iş yapmaz.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Bu şiddetin altındaki titreşimler yok sayılır. Sürekli titreyen bir
        /// oyun rahatsız eder; varsayılan olarak yalnızca kayda değer anlar.
        /// </summary>
        public HapticStrength Threshold { get; set; } = HapticStrength.Medium;

        public static Haptics Create(Transform parent = null)
        {
            var go = new GameObject("Haptics");
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
            return go.AddComponent<Haptics>();
        }

        public void Play(HapticStrength strength = HapticStrength.Medium)
        {
            if (!Enabled || strength < Threshold) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
