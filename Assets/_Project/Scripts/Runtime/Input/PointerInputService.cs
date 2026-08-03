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

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null) return; // ne mouse ne dokunmatik var (olağandışı)

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
