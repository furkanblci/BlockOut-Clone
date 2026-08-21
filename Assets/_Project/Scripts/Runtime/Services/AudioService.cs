using System.Collections.Generic;
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
    ///
    /// DERS (dosya varsa dosya, yoksa sentez): Her ses önce
    /// <see cref="AudioSkin"/>'den aranıyor; bulunamazsa kodla üretilmiş
    /// yer tutucuya düşüyor. Böylece ses toplama işi parça parça ilerleyebiliyor
    /// — tek bir dosya eklendiği anda devreye girer, hiçbir şey beklemez ve
    /// eksik dosya oyunu SESSİZ bırakmaz.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        SfxPlayer _player;
        AudioSource _music;

        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>(32);

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

        /// <summary>Müzik anahtarı. Kapatınca kaynak durur, açınca kaldığı parça başlar.</summary>
        public static void SetMusicEnabled(bool on)
        {
            if (_instance == null) return;
            _instance._musicEnabled = on;

            if (!on) _instance._music.Stop();
            else if (!string.IsNullOrEmpty(_instance._musicKey))
                _instance.PlayMusicInternal(_instance._musicKey);
        }

        bool _musicEnabled = true;
        string _musicKey;

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

            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.volume = 0.35f;      // müzik efektlerin ARKASINDA durmalı

            BuildPalette();

            // Arayüz düğmelerinin tık sesi buradan geçer; GameKit ses
            // servisini tanımadığı için bağlantıyı oyun tarafı kuruyor.
            GameKit.UI.UiButtonFeel.Clicked = Click;
        }

        /// <summary>
        /// Ses paleti: her anahtar için önce dosya, yoksa sentez.
        ///
        /// Sentezlenmiş sesler YER TUTUCU. Gerçek dosyalar
        /// `Assets/_Project/Audio/sfx/&lt;anahtar&gt;.wav` olarak eklenince
        /// kendiliğinden devreye girerler; burada hiçbir şey değişmez.
        /// </summary>
        void BuildPalette()
        {
            Register(Sfx.Absorb,       () => SfxSynth.Pop(660f, 0.16f));
            Register(Sfx.Peel,         () => SfxSynth.Pop(440f, 0.12f));
            Register(Sfx.IceCrack,     () => SfxSynth.Noise(0.18f, 2600f));
            Register(Sfx.CurtainOpen,  () => SfxSynth.Arpeggio(new[] { 523f, 659f, 784f }, 0.09f));
            Register(Sfx.Win,          () => SfxSynth.Arpeggio(new[] { 523f, 659f, 784f, 1046f }, 0.11f));
            Register(Sfx.Lose,         () => SfxSynth.Arpeggio(new[] { 440f, 349f, 262f }, 0.16f));

            Register(Sfx.PickUp,       () => SfxSynth.Click(1200f));
            Register(Sfx.Drop,         () => SfxSynth.Pop(300f, 0.09f));
            Register(Sfx.IceTick,      () => SfxSynth.Noise(0.07f, 3200f));
            Register(Sfx.CurtainTick,  () => SfxSynth.Pop(240f, 0.08f));
            Register(Sfx.GateAdvance,  () => SfxSynth.Arpeggio(new[] { 587f, 740f }, 0.07f));
            Register(Sfx.GateDone,     () => SfxSynth.Pop(392f, 0.20f));
            Register(Sfx.BlockSpawn,   () => SfxSynth.Pop(220f, 0.14f));
            Register(Sfx.Refuse,       () => SfxSynth.Pop(180f, 0.10f));
            Register(Sfx.TimerWarning, () => SfxSynth.Pop(520f, 0.08f));

            Register(Sfx.PowerClock,   () => SfxSynth.Arpeggio(new[] { 880f, 740f, 622f }, 0.10f));
            Register(Sfx.PowerRocket,  () => SfxSynth.Noise(0.28f, 1400f));
            Register(Sfx.PowerUfo,     () => SfxSynth.Arpeggio(new[] { 1319f, 988f, 784f, 659f }, 0.07f));

            Register(Sfx.Click,        () => SfxSynth.Click(880f));
            Register(Sfx.Coin,         () => SfxSynth.Coin());
            Register(Sfx.Star,         () => SfxSynth.Arpeggio(new[] { 784f, 988f, 1319f }, 0.08f));
            Register(Sfx.PanelOpen,    () => SfxSynth.Noise(0.16f, 900f));
            Register(Sfx.PanelClose,   () => SfxSynth.Noise(0.12f, 600f));
            Register(Sfx.Purchase,     () => SfxSynth.Arpeggio(new[] { 523f, 659f, 784f, 1046f, 1319f }, 0.10f));
            Register(Sfx.RewardClaim,  () => SfxSynth.Arpeggio(new[] { 659f, 784f, 1046f }, 0.12f));
            Register(Sfx.Unlock,       () => SfxSynth.Arpeggio(new[] { 392f, 523f, 659f, 784f, 1046f }, 0.13f));

            // Kombo: dosya yoksa tek sesin perdesi kademeli yükselir.
            for (int step = 1; step <= 5; step++)
            {
                int captured = step;
                Register(Sfx.Combo(captured),
                    () => SfxSynth.Pop(660f * Mathf.Pow(1.12f, captured - 1), 0.14f));
            }
        }

        void Register(string key, System.Func<AudioClip> synthesize)
        {
            var clip = AudioSkin.Get(key);
            _clips[key] = clip != null ? clip : synthesize();
        }

        AudioClip Clip(string key) => _clips.TryGetValue(key, out var c) ? c : null;

        // ---------------------------------------------------------------- olaylar

        /// <summary>
        /// Tahta olaylarını sese bağlar. Her bölümde yeni bir olay merkezi
        /// doğduğu için her seferinde çağrılır.
        /// </summary>
        public void Bind(BoardEvents events)
        {
            if (events == null) return;

            events.BlockAbsorbed    += (b, g) => _player.Play(Clip(Sfx.Absorb), 0.45f);
            events.LayerPeeled      += (b, g) => _player.Play(Clip(Sfx.Peel), 0.38f);
            events.IceShattered     += b => _player.Play(Clip(Sfx.IceCrack), 0.55f);
            events.GateIceShattered += g => _player.Play(Clip(Sfx.IceCrack), 0.55f);
            events.CurtainOpened    += c => _player.Play(Clip(Sfx.CurtainOpen), 0.55f);
            events.BoardCleared     += _ => _player.Play(Clip(Sfx.Win), 0.7f);

            // Buraya kadarı zaten vardı. Aşağısı SESSİZDİ — olay yayınlanıyordu
            // ama kimse dinlemiyordu. Referans oyunda hepsinin sesi var; sayaç
            // tıklamaları özellikle önemli, çünkü oyuncu "ilerliyor muyum"
            // sorusunu onlarla cevaplıyor.
            // BU DÖRT OLAY TEK HAMLEDE BİRDEN ÇOK KEZ YAYINLANIYOR.
            //
            // Her emilimde tahtadaki BÜTÜN buzlu blokların sayacı bir azalıyor
            // — beş buzlu blok varsa `IceDecremented` aynı karede beş kez
            // yayınlanıyor. Beş özdeş klip aynı anda çalınca genlik beş katına
            // çıkıyor: hem kırpma hem "çatırtı bombası". Aynı şey perde
            // sayaçları ve aynı anda ghost olan kapılar için de geçerli.
            //
            // DERS (olay sayısı = ses sayısı DEĞİLDİR): Sentezlenmiş cılız
            // bliplerle bu fark edilmiyordu; gerçek, tepe seviyeli kliplerle
            // ilk denemede duyulur hâle geliyor. Bir olaya ses bağlarken
            // "kaç kere yayınlanıyor" sorusu, "hangi ses" sorusu kadar önemli.
            events.IceDecremented     += b => PlayOnce(Sfx.IceTick, 0.30f);
            events.GateIceDecremented += g => PlayOnce(Sfx.IceTick, 0.30f);
            events.CurtainDecremented += c => PlayOnce(Sfx.CurtainTick, 0.30f);
            events.GateAdvanced       += g => PlayOnce(Sfx.GateAdvance, 0.35f);
            events.GateGhosted        += g => PlayOnce(Sfx.GateDone, 0.45f);
            events.BlockSpawned       += b => PlayOnce(Sfx.BlockSpawn, 0.35f);
        }

        /// <summary>Aynı sesin üst üste yığılmasını engelleyen son çalma anları.</summary>
        readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();

        /// <summary>
        /// Aynı anahtarı 60 ms içinde bir kereden fazla çalmaz.
        ///
        /// 60 ms neden: 60 fps'te dört kare. Aynı hamlenin ürettiği olaylar
        /// hep aynı karede geliyor, yani hepsi tek sese iniyor; oyuncunun
        /// ARDIŞIK iki hamlesi ise en hızlı hâlinde bile 150 ms'den seyrek,
        /// yani hiçbir gerçek hamle sessiz kalmıyor.
        /// </summary>
        void PlayOnce(string key, float volume)
        {
            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(key, out float last) && now - last < 0.06f) return;
            _lastPlayed[key] = now;
            _player.Play(Clip(key), volume);
        }

        public void PlayLose() => _player.Play(Clip(Sfx.Lose), 0.55f);

        // ---------------------------------------------------------------- statik erişim

        // Arayüzün her yerinden çağrılabilsin diye statik: her ekranın
        // AudioService referansı taşıması gereksiz bir bağ olurdu.
        static void PlayStatic(string key, float volume)
            => _instance?._player.Play(_instance.Clip(key), volume);

        public static void Click()       => PlayStatic(Sfx.Click, 0.26f);
        public static void Coin()        => PlayStatic(Sfx.Coin, 0.50f);
        public static void Refuse()      => PlayStatic(Sfx.Refuse, 0.38f);
        public static void Star()        => PlayStatic(Sfx.Star, 0.45f);
        public static void PickUp()      => PlayStatic(Sfx.PickUp, 0.30f);
        public static void Drop()        => PlayStatic(Sfx.Drop, 0.34f);
        public static void PanelOpen()   => PlayStatic(Sfx.PanelOpen, 0.34f);
        public static void PanelClose()  => PlayStatic(Sfx.PanelClose, 0.30f);
        public static void Purchase()    => PlayStatic(Sfx.Purchase, 0.65f);
        public static void RewardClaim() => PlayStatic(Sfx.RewardClaim, 0.60f);
        public static void Unlock()      => PlayStatic(Sfx.Unlock, 0.65f);
        public static void TimerWarning()=> PlayStatic(Sfx.TimerWarning, 0.28f);

        /// <summary>Zincir uzadıkça tizleşen kutlama.</summary>
        public static void Combo(int chain) => PlayStatic(Sfx.Combo(chain), 0.50f);

        public static void PowerUp(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Clock:  PlayStatic(Sfx.PowerClock, 0.55f); break;
                case PowerUpKind.Rocket: PlayStatic(Sfx.PowerRocket, 0.60f); break;
                default:                 PlayStatic(Sfx.PowerUfo, 0.55f); break;
            }
        }

        // ---------------------------------------------------------------- müzik

        /// <summary>
        /// Döngü müziğini değiştirir. Aynı parça zaten çalıyorsa dokunmaz —
        /// menüler arasında gezerken müziğin baştan başlaması en sık yapılan
        /// hatalardan biri.
        /// </summary>
        public static void PlayMusic(string key)
        {
            if (_instance == null) return;
            _instance._musicKey = key;
            if (_instance._musicEnabled) _instance.PlayMusicInternal(key);
        }

        void PlayMusicInternal(string key)
        {
            var clip = AudioSkin.Get(key);

            // MENÜ PARÇASI YOKSA OYNANIŞ PARÇASINA DÜŞ.
            //
            // Elimizde tek bir arka plan parçası var. Aynı dosyayı iki ayrı
            // anahtarla iki kez koymak, derlemeye bir megabaytı boşuna
            // eklerdi. Menü ile oynanış arasında geçerken parça DEĞİŞMEDİĞİ
            // için aşağıdaki "zaten çalıyor" kontrolü devreye giriyor ve
            // müzik kesintisiz akıyor — istenen davranış da bu.
            if (clip == null && key != Sfx.MusicGameplay)
                clip = AudioSkin.Get(Sfx.MusicGameplay);

            // Müzik SENTEZLENMİYOR: kötü bir döngü, sessizlikten daha rahatsız
            // edici. Dosya yoksa müzik yok.
            if (clip == null) { _music.Stop(); return; }
            if (_music.clip == clip && _music.isPlaying) return;

            _music.clip = clip;
            _music.Play();
        }
    }
}
