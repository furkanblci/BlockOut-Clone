using BlockOut.Core;
using GameKit.Services;
using UnityEngine;

namespace BlockOut.Runtime.Services
{
    /// <summary>
    /// Tahta olaylarını sese çevirir — ses PALETİ ve EŞLEMESİ bu oyuna aittir.
    ///
    /// DERS (ne kite gider, ne oyunda kalır?): Sesin nasıl çalınacağı (havuz,
    /// çakışma sınırı, perde savrulması, kısma) her oyunda aynıdır → GameKit'teki
    /// <see cref="SfxPlayer"/>. Hangi olayın hangi sesi çıkardığı ise bu oyunun
    /// tasarım kararıdır → burası. Ayrımı böyle çekince kit ikinci projede
    /// olduğu gibi çalışıyor, bu dosya ise atılıyor.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        SfxPlayer _player;
        AudioClip _absorb, _peel, _iceCrack, _curtain, _win, _lose;

        public bool Muted
        {
            get => _player != null && _player.Muted;
            set { if (_player != null) _player.Muted = value; }
        }

        public static AudioService Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, worldPositionStays: false);
            return go.AddComponent<AudioService>();
        }

        void Awake()
        {
            _player = SfxPlayer.Create(transform);

            // Yer tutucu palet: gerçek ses tasarımı geldiğinde DEĞİŞECEK TEK YER.
            _absorb   = SfxSynth.Pop(660f, 0.16f);
            _peel     = SfxSynth.Pop(440f, 0.12f);
            _iceCrack = SfxSynth.Noise(0.18f, 2600f);
            _curtain  = SfxSynth.Arpeggio(new[] { 523f, 659f, 784f }, 0.09f);
            _win      = SfxSynth.Arpeggio(new[] { 523f, 659f, 784f, 1046f }, 0.11f);
            _lose     = SfxSynth.Arpeggio(new[] { 440f, 349f, 262f }, 0.16f);
        }

        public void Bind(BoardEvents events)
        {
            if (events == null) return;
            events.BlockAbsorbed    += (b, g) => _player.Play(_absorb, 0.55f);
            events.LayerPeeled      += (b, g) => _player.Play(_peel, 0.45f);
            events.IceShattered     += b => _player.Play(_iceCrack, 0.5f);
            events.GateIceShattered += g => _player.Play(_iceCrack, 0.5f);
            events.CurtainOpened    += c => _player.Play(_curtain, 0.6f);
            events.BoardCleared     += () => _player.Play(_win, 0.7f);
        }

        public void PlayLose() => _player.Play(_lose, 0.6f);
    }
}
