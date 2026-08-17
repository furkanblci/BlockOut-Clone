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
    /// DERS (kaba bir motorla ince iş yapılmaz): İlk hâl `Handheld.Vibrate()`
    /// çağırıyordu. O çağrı Android'de SÜRESİ AYARLANAMAYAN ~500 ms'lik bir
    /// buzz üretir ve şiddet ayrımı yapmaz. Bu yüzden `Threshold` Medium'da
    /// tutuluyordu — her dokunuşta yarım saniye titreyen bir oyun kullanılamaz.
    /// Ama o eşik asıl istenen şeyi de imkânsız kılıyordu: arayüz
    /// dokunuşlarının HAFİF bir tık vermesini (2026-08-17, 15. APK bulgusu).
    ///
    /// Artık Android'de `Vibrator` doğrudan çağrılıyor ve şiddet gerçek bir
    /// SÜRE + GENLİK oluyor (Light 12 ms, Medium 25 ms, Heavy 45 ms). Eşik
    /// Light'a inebildi; her düğme rahatsız etmeden tık veriyor.
    ///
    /// iOS'ta karşılığı yok: `Handheld.Vibrate()` orada da uzun bir buzz.
    /// Bu yüzden iOS'ta yalnız Heavy titriyor — hafif tık için Taptic Engine
    /// eklentisi gerekir, o gelirse değişecek tek yer yine burası.
    /// </summary>
    public sealed class Haptics : MonoBehaviour
    {
        /// <summary>Oyuncunun ayarı. Kapalıyken hiçbir çağrı iş yapmaz.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Bu şiddetin altındaki titreşimler yok sayılır. Artık Light da
        /// gerçekten hafif olduğu için varsayılan en alta çekildi.
        /// </summary>
        public HapticStrength Threshold { get; set; } = HapticStrength.Light;

        public static Haptics Create(Transform parent = null)
        {
            var go = new GameObject("Haptics");
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
            return go.AddComponent<Haptics>();
        }

        /// <summary>Şiddetin süre (ms) ve genlik (0-255) karşılığı.</summary>
        static void Shape(HapticStrength strength, out int ms, out int amplitude)
        {
            switch (strength)
            {
                case HapticStrength.Light:  ms = 12; amplitude = 60;  break;
                case HapticStrength.Medium: ms = 25; amplitude = 140; break;
                default:                    ms = 45; amplitude = 255; break;
            }
        }

        public void Play(HapticStrength strength = HapticStrength.Medium)
        {
            if (!Enabled || strength < Threshold) return;

#if UNITY_ANDROID && !UNITY_EDITOR
            Shape(strength, out int ms, out int amplitude);
            Vibrate(ms, amplitude);
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS'ta kısa tık üretemiyoruz; hafifleri hiç çalmamak, hepsini
            // yarım saniyelik buzz'a çevirmekten iyidir.
            if (strength == HapticStrength.Heavy) Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject _vibrator;
        static bool _looked;
        static int _sdk;

        /// <summary>
        /// Android titreşim servisi — bir kez bulunup saklanıyor. Her dokunuşta
        /// `AndroidJavaObject` kurmak JNI üzerinden pahalıdır.
        /// </summary>
        static AndroidJavaObject Vibrator
        {
            get
            {
                if (_looked) return _vibrator;
                _looked = true;
                try
                {
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        _sdk = version.GetStatic<int>("SDK_INT");

                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                catch (System.Exception error)
                {
                    Debug.LogWarning("[Haptics] Titreşim servisi alınamadı: " + error.Message);
                    _vibrator = null;
                }
                return _vibrator;
            }
        }

        static void Vibrate(int milliseconds, int amplitude)
        {
            var vibrator = Vibrator;
            if (vibrator == null) return;

            try
            {
                // API 26+ genlik destekliyor; altında yalnız süre verilebiliyor.
                if (_sdk >= 26)
                {
                    using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var effect = effects.CallStatic<AndroidJavaObject>(
                               "createOneShot", (long)milliseconds, amplitude))
                        vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("[Haptics] Titreşim çalınamadı: " + error.Message);
            }
        }
#endif
    }
}
