using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockOut.Runtime.Input
{
    /// <summary>
    /// Mouse (Editor/PC) ve dokunmatik ekranı (Android/iOS) TEK arayüz altında
    /// birleştiren giriş servisi. Oyun kodu asla "mouse mu, parmak mı?" diye
    /// sormaz; sadece bu üç olaya abone olur.
    ///
    /// DERS (new Input System): Pointer.current, aktif işaretçiyi (mouse VEYA
    /// birincil dokunuş) soyutlar. Eski Input.mousePosition / Input.touches
    /// ikiliğine kıyasla tek kod yolu bırakır — PC'de geliştir, cihazda aynen
    /// çalışır. Update içinde "polling" yapıyoruz çünkü sürükleme zaten kare
    /// başına örneklenen sürekli bir eylemdir.
    /// </summary>
    public sealed class PointerInputService : MonoBehaviour
    {
        public event System.Action<Vector2> PointerDown;   // basıldığı kare
        public event System.Action<Vector2> PointerHeld;   // basılı tutulan her kare
        public event System.Action<Vector2> PointerUp;     // bırakıldığı kare

        [SerializeField, Tooltip("M0 doğrulaması için olayları Console'a yaz. M1'de kapatılacak.")]
        bool logEvents = true;

        public bool IsDown { get; private set; }
        public Vector2 Position { get; private set; }

        /// <summary>
        /// Üstte tam ekran bir katman (geliştirici konsolu) varken girdi
        /// oynanışa GEÇMEZ.
        ///
        /// DERS (IMGUI hiçbir şeyi engellemez): OnGUI ile çizilen bir panel
        /// yalnızca ÜSTE ÇİZER; altındaki oyun dünyası dokunuşları almaya
        /// devam eder. Konsol açıkken panele basan parmak, aynı anda arkadaki
        /// bloğu da sürüklüyordu — yani test aracının kendisi test edilen
        /// durumu bozuyordu. Kilit statik: girdiyi okuyan tek yer burası
        /// olduğu için tek kapı yeterli.
        /// </summary>
        public static bool Blocked;

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null) return; // ne mouse ne dokunmatik var (olağandışı)

            if (Blocked)
            {
                // Kilit AÇILDIĞI anda parmak basılıysa sürüklemeyi düzgün
                // bitir; yoksa blok "yapışık" kalır ve konsol kapanınca
                // kaldığı yerden sürüklenmeye devam eder.
                if (IsDown)
                {
                    IsDown = false;
                    PointerUp?.Invoke(Position);
                }
                return;
            }

            Position = pointer.position.ReadValue();
            bool pressed = pointer.press.isPressed;

            if (pressed && !IsDown)
            {
                IsDown = true;
                PointerDown?.Invoke(Position);
                if (logEvents) Debug.Log($"[Input] Down  {Position}");
            }
            else if (pressed) // ve zaten basılıydı
            {
                PointerHeld?.Invoke(Position);
            }
            else if (IsDown) // bırakıldı
            {
                IsDown = false;
                PointerUp?.Invoke(Position);
                if (logEvents) Debug.Log($"[Input] Up    {Position}");
            }
        }
    }
}
