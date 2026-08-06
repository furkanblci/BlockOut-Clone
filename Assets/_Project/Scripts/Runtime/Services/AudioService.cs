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
        AudioClip _click, _coin, _refuse, _star;
        AudioClip _absorb, _peel, _iceCrack, _curtain, _win, _lose;

        static AudioService _instance;

        public bool Muted
        {
            get => _player != null && _player.Muted;
            set { if (_player != null) _player.Muted = value; }
        }

        /// <summary>
        /// Ayarlar ekranı sahnedeki örneği bilmez; sesi buradan kapatır.
        /// Örnek yoksa (menü sahnesinde ses servisi kurulmamışsa) sessizce
        /// geçilir — ayar zaten kayda yazıldı, oyun sahnesi onu okuyacak.
        /// </summary>
        public static void SetMuted(bool muted)
        {
            if (_instance != null) _instance.Muted = muted;
        }

        public static AudioService Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, worldPositionStays: false);
            return go.AddComponent<AudioService>();
        }

        void Awake()
        {
            _instance = this;
            _player = SfxPlayer.Create(transform);

            // Yer tutucu palet: gerçek ses tasarımı geldiğinde DEĞİŞECEK TEK YER.
            _absorb   = SfxSynth.Pop(660f, 0.16f);
            _peel     = SfxSynth.Pop(440f, 0.12f);
            _iceCrack = SfxSynth.Noise(0.18f, 2600f);
            _curtain  = SfxSynth.Arpeggio(new[] { 523f, 659f, 784f }, 0.09f);
            _win      = SfxSynth.Arpeggio(new[] { 523f, 659f, 784f, 1046f }, 0.11f);
            _lose     = SfxSynth.Arpeggio(new[] { 440f, 349f, 262f }, 0.16f);

            // Arayüz sesleri: dokunmanın duyulması, oyunun "cevap veriyor"
            // hissinin yarısıdır. Görsel geri bildirim tek başına yetmiyor —
            // parmak ekranı kapattığı için gözün gördüğü şeyi çoğu zaman
            // parmağın kendisi örtüyor.
            _click    = SfxSynth.Click(880f);
            _coin     = SfxSynth.Coin();
            _refuse   = SfxSynth.Pop(180f, 0.10f);
            _star     = SfxSynth.Arpeggio(new[] { 784f, 988f, 1319f }, 0.08f);

            // Arayüz düğmelerinin tık sesi buradan geçer; GameKit ses
            // servisini tanımadığı için bağlantıyı oyun tarafı kuruyor.
            GameKit.UI.UiButtonFeel.Clicked = Click;
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

        // Arayüzün her yerinden çağrılabilsin diye statik: her ekranın
        // AudioService referansı taşıması gereksiz bir bağ olurdu.
        public static void Click()  => _instance?._player.Play(_instance._click, 0.35f);
        public static void Coin()   => _instance?._player.Play(_instance._coin, 0.55f);
        public static void Refuse() => _instance?._player.Play(_instance._refuse, 0.45f);
        public static void Star()   => _instance?._player.Play(_instance._star, 0.6f);
    }
}
