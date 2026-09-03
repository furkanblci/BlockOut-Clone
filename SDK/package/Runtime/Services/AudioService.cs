using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Services
{
    /// <summary>
    /// Oyunun ses masası: paleti kurar, arayüz seslerini statik olarak sunar,
    /// müziği yönetir. OYUNA ÖZEL eşlemeler burada DEĞİL — onlar
    /// <see cref="PaletteExtender"/> kancasından katılır.
    ///
    /// DERS (ne kite gider, ne oyunda kalır?): Sesin nasıl çalınacağı (havuz,
    /// çakışma sınırı, perde savrulması, kısma) her oyunda aynıdır → GameKit'teki
    /// <see cref="SfxPlayer"/>. Hangi olayın hangi sesi çıkardığı ise oyunun
    /// tasarım kararıdır → oyunun kendi dosyası. Bu sınıf ikisinin arasındaki
    /// masa: her iki oyunda da aynı kalan kısım.
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
            Register(Sfx.Win,          () => SfxSynth.Arpeggio(new[] { 523f, 659f, 784f, 1046f }, 0.11f));
            Register(Sfx.Lose,         () => SfxSynth.Arpeggio(new[] { 440f, 349f, 262f }, 0.16f));

            Register(Sfx.PickUp,       () => SfxSynth.Click(1200f));
            Register(Sfx.Drop,         () => SfxSynth.Pop(300f, 0.09f));
            Register(Sfx.Refuse,       () => SfxSynth.Pop(180f, 0.10f));
            Register(Sfx.TimerWarning, () => SfxSynth.Pop(520f, 0.08f));

            Register(Sfx.PowerGeneric, () => SfxSynth.Arpeggio(new[] { 1319f, 988f, 784f, 659f }, 0.07f));

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

            // Oyunun kendi oynanış sesleri buradan katılır.
            //
            // DERS (uzatma noktası, kalıtım değil): Bunu yapmanın "doğru"
            // görünen yolu sınıfı `virtual BuildPalette` ile açmak ve oyunda
            // türetmek olurdu. Ama o zaman oyun tarafında bir AudioService
            // ALT SINIFI olur ve <c>AppRoot</c>'un hangi türü kuracağı bir
            // ayara dönüşür. Tek bir statik kanca, aynı işi kurulum sırasını
            // hiç değiştirmeden yapıyor.
            PaletteExtender?.Invoke(this);
        }

        /// <summary>
        /// Oyunun kendi seslerini kaydettiği kanca — <c>AppRoot</c> uyanmadan
        /// ÖNCE atanmalı (ör. <c>[RuntimeInitializeOnLoadMethod]</c> içinde).
        /// </summary>
        public static System.Action<AudioService> PaletteExtender;

        /// <summary>
        /// Bir ses adı kaydeder: dosya varsa dosya, yoksa sentezlenmiş yer tutucu.
        /// Oyun tarafı <see cref="PaletteExtender"/> içinden çağırır.
        /// </summary>
        public void Register(string key, System.Func<AudioClip> synthesize)
        {
            var clip = AudioSkin.Get(key);
            _clips[key] = clip != null ? clip : synthesize();
        }

        AudioClip Clip(string key) => _clips.TryGetValue(key, out var c) ? c : null;

        readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();

        /// <summary>
        /// Aynı anahtarı 60 ms içinde bir kereden fazla çalmaz.
        ///
        /// 60 ms neden: 60 fps'te dört kare. Aynı hamlenin ürettiği olaylar
        /// hep aynı karede geliyor, yani hepsi tek sese iniyor; oyuncunun
        /// ARDIŞIK iki hamlesi ise en hızlı hâlinde bile 150 ms'den seyrek,
        /// yani hiçbir gerçek hamle sessiz kalmıyor.
        /// </summary>
        public void PlayOnce(string key, float volume)
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

        /// <summary>Bu ad için kayıtlı bir ses var mı — yedeğe düşmek isteyenler için.</summary>
        static bool HasClip(string key)
            => _instance != null && _instance.Clip(key) != null;

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

        /// <summary>
        /// Tüketilebilir kullanım sesi. Kimlik <c>IGameHost.Consumables</c>
        /// içindeki <c>Id</c>'dir.
        ///
        /// DERS (enum yerine kimlik): Burası eskiden oyunun <c>PowerUpKind</c>
        /// enum'unu alıyordu — yani ses servisi, o oyunun güçlendirici setini
        /// bilmek zorundaydı. Kimlik dizgisiyle çalışınca servis "hangi ses
        /// bankasında bu ad var" sorusuna indirgeniyor; yeni bir oyunda yeni bir
        /// öğe eklemek için ses servisini AÇMAK GEREKMİYOR, tek yapılacak
        /// <c>power_&lt;kimlik&gt;</c> adında bir ses kaydetmek.
        /// </summary>
        public static void PowerUp(string consumableId)
        {
            string key = "power_" + (consumableId ?? "").ToLowerInvariant();
            PlayStatic(HasClip(key) ? key : Sfx.PowerGeneric, 0.55f);
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
