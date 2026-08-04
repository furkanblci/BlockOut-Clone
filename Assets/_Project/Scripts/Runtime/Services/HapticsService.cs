using BlockOut.Core;
using UnityEngine;

namespace BlockOut.Runtime.Services
{
    /// <summary>
    /// Dokunsal geri bildirim. Tahta olaylarına abone olur, cihazda titreşir.
    ///
    /// DERS (platform farkı): Editörde ve PC'de titreşim yoktur; kod her
    /// platformda derlenmeli ama yalnızca mobilde iş yapmalıdır. Bunu
    /// #if UNITY_ANDROID ile ayırıyoruz — çalışma anında platform sorgusu
    /// yapmak yerine DERLEME anında ilgisiz kodu tamamen dışarıda bırakmak
    /// hem daha hızlı hem daha temizdir.
    ///
    /// Not: Handheld.Vibrate süresi ayarlanamaz (Android'de ~500 ms). Gerçek
    /// ince titreşim için M5'te yerel eklenti ya da hazır paket kullanılabilir;
    /// şimdilik yalnızca ÖNEMLİ anlarda titretiyoruz ki rahatsız etmesin.
    /// </summary>
    public sealed class HapticsService : MonoBehaviour
    {
        BoardEvents _events;

        public bool Enabled { get; set; } = true;

        public static HapticsService Create(Transform parent)
        {
            var go = new GameObject("Haptics");
            go.transform.SetParent(parent, worldPositionStays: false);
            return go.AddComponent<HapticsService>();
        }

        public void Bind(BoardEvents events)
        {
            _events = events;
            // Her blok çıkışında titretmek yorucu olur; yalnızca buz kırılması
            // ve bölüm bitişi gibi "kazanım" anlarında geri bildirim veriyoruz.
            _events.IceShattered += _ => Pulse();
            _events.GateIceShattered += _ => Pulse();
            _events.CurtainOpened += _ => Pulse();
            _events.BoardCleared += Pulse;
        }

        public void Pulse()
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
