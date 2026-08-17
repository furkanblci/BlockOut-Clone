using System;
using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Core.Save;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.Flow
{
    /// <summary>
    /// Yardımcıların (çalar saat / roket / UFO) satın alınması, hedef seçilmesi
    /// ve etkilerinin uygulanması.
    ///
    /// DERS (iki aşamalı yardımcı): Roket ve UFO hedef ister. Jeton düğmeye
    /// basınca harcanır ama karşılığı ENVANTERE girer; yardımcı ancak hedef
    /// seçilince TÜKENİR. Böylece yanlışlıkla dokunan oyuncu iptal edip
    /// hakkını geri alır — jetonun buhar olması, oyunu bıraktıran türden bir
    /// haksızlık hissi yaratırdı.
    ///
    /// DERS (zincir tekrar kullanımı): Roket/UFO bloğu SİLERKEN kapıdan çıkmış
    /// gibi davranır — buz ve perde sayaçları ilerler. Ayrı bir "silme" yolu
    /// yazmak, bu sayaçların bir gün unutulacağı ikinci bir kod yolu demekti.
    /// </summary>
    public sealed class PowerUpSystem
    {
        readonly LevelModel _level;
        readonly BoardViews _views;
        readonly GateSystem _gates;
        readonly ObstacleSystem _obstacles;
        readonly ProgressService _progress;
        readonly BoardSpace _space;
        readonly bool _hasSpace;
        readonly Dictionary<PowerUpKind, int> _local = new Dictionary<PowerUpKind, int>();

        /// <summary>Hedef bekleyen yardımcı; yoksa null.</summary>
        public PowerUpKind? Pending { get; private set; }

        /// <summary>Süre dondurma bitene kadar kalan saniye (0 ise donmuş değil).</summary>
        public float FreezeRemaining { get; private set; }

        public bool IsTimeFrozen => FreezeRemaining > 0f;

        public event Action Changed;
        public event Action<string> Message;

        /// <summary>
        /// Bir yardımcı GERÇEKTEN devreye girdiğinde yayınlanır — seçilince
        /// değil, etkisi uygulanınca.
        ///
        /// DERS (sistem sesi TANIMAZ): Buraya doğrudan `AudioService.PowerUp()`
        /// yazmak iki satır tasarruf ederdi ama oynanış sistemini ses servisine
        /// bağlardı. Bu projedeki ayrım net: sistem "ne oldu"yu yayınlar, hangi
        /// olayın hangi sesi çıkardığına <see cref="Services.AudioService"/>
        /// karar verir. Roket sesini değiştirmek için oynanış koduna dokunmak
        /// gerekmesin.
        /// </summary>
        public event Action<PowerUpKind> Used;

        /// <summary>
        /// Yardımcı alınamadı — elde yok ve jeton da yetmiyor.
        ///
        /// DERS (reddi de bir OLAY olarak yayınla): Bu durum önce yalnız
        /// `Message` ile bir yazıya dönüşüyordu; yazı 2.5 saniye görünüp
        /// kayboluyordu, ses yoktu, hangi düğmenin reddedildiği de belli
        /// değildi. Oyuncunun gördüğü şey "bastım, bir şey olmadı" idi.
        /// Kind'ı taşıyan ayrı bir olay, arayüzün TAM O DÜĞMEYİ
        /// işaretlemesini mümkün kılıyor — mesaj metnini ayrıştırmadan.
        /// </summary>
        public event Action<PowerUpKind> Refused;

        public PowerUpSystem(
            LevelModel level, BoardViews views, GateSystem gates,
            ObstacleSystem obstacles, ProgressService progress, BoardSpace space = default)
        {
            _level = level;
            _views = views;
            _gates = gates;
            _obstacles = obstacles;
            _progress = progress;
            // BoardSpace bir DEĞER TİPİ (readonly struct); null olamaz.
            // Testler onu vermediğinde `default` gelir ve efektler atlanır.
            _space = space;
            _hasSpace = space.Width > 0 && space.Height > 0;
        }

        /// <summary>
        /// Envanter KAYITTA yaşar; kayıt servisi yoksa (testler, görselsiz
        /// doğrulama) oturum içi sözlüğe düşer. İki yol da aynı API'den
        /// okunur ki çağıran taraf hangisinin geçerli olduğunu bilmek
        /// zorunda kalmasın.
        /// </summary>
        public int Owned(PowerUpKind kind) =>
            _progress != null ? _progress.PowerUpCount(Id(kind))
                              : (_local.TryGetValue(kind, out int n) ? n : 0);

        public void Grant(PowerUpKind kind, int count = 1) => SetOwned(kind, Owned(kind) + count);

        void SetOwned(PowerUpKind kind, int count)
        {
            if (_progress != null) _progress.SetPowerUpCount(Id(kind), count);
            else _local[kind] = Mathf.Max(0, count);
        }

        static string Id(PowerUpKind kind) => kind.ToString().ToLowerInvariant();

        public void Tick(float deltaTime)
        {
            if (FreezeRemaining <= 0f) return;
            FreezeRemaining = Mathf.Max(0f, FreezeRemaining - deltaTime);
            if (FreezeRemaining <= 0f) Changed?.Invoke();
        }

        /// <summary>
        /// Alt çubuktaki düğmeye basıldı. Elde varsa doğrudan, yoksa jetonla
        /// satın alarak kullanır. Hedef isteyen yardımcılarda seçim moduna geçer.
        /// </summary>
        public bool Use(PowerUpKind kind)
        {
            if (Pending == kind) { Cancel(); return false; }   // aynı düğme = iptal

            if (Owned(kind) <= 0)
            {
                int price = PowerUpInfo.Price(kind);
                if (_progress == null || !_progress.TrySpendCoins(price))
                {
                    Message?.Invoke("Not enough coins.");
                    Refused?.Invoke(kind);
                    return false;
                }
                Grant(kind);
            }

            if (kind == PowerUpKind.Clock)
            {
                Consume(kind);
                FreezeRemaining = PowerUpInfo.ClockFreezeSeconds;
                Message?.Invoke("Time frozen!");
                Used?.Invoke(kind);
                Changed?.Invoke();
                return true;
            }

            // Hedef bekleniyor. Jeton harcandıysa karşılığı ENVANTERE girdi;
            // iptal edilirse yardımcı elde kalır, yalnız kullanım geri alınır.
            Pending = kind;
            _lastUsed = kind;                      // efekt hangisi olacak
            GameKit.Services.Analytics.LogPowerUpUsed(kind.ToString(), 0);
            Message?.Invoke(PowerUpInfo.Prompt(kind));
            Changed?.Invoke();
            return true;
        }

        /// <summary>Efektin hangi yardımcıya ait olduğunu bilmek için.</summary>
        PowerUpKind _lastUsed = PowerUpKind.Clock;

        /// <summary>Seçim modundan çıkar; yardımcı harcanmadığı için elde kalır.</summary>

        public void Cancel()
        {
            if (Pending == null) return;
            Message?.Invoke("");
            Pending = null;
            Changed?.Invoke();
        }

        /// <summary>
        /// Oyuncu tahtada bir bloğa dokundu. Bekleyen yardımcı varsa uygulanır
        /// ve true döner (dokunuş sürüklemeye dönüşmez).
        /// </summary>
        public bool HandleBlockTap(BlockModel block)
        {
            if (Pending == null || block == null) return false;

            var kind = Pending.Value;
            Pending = null;
            Consume(kind);
            Message?.Invoke("");

            Used?.Invoke(kind);

            if (kind == PowerUpKind.Rocket)
                Remove(block);
            else
                RemoveColor(block.CurrentColor);

            Changed?.Invoke();
            return true;
        }

        void Consume(PowerUpKind kind) => SetOwned(kind, Owned(kind) - 1);

        /// <summary>Rengi eşleşen TÜM blokları siler (UFO). Donmuşlar dahil.</summary>
        void RemoveColor(BlockColor color)
        {
            var doomed = new List<BlockModel>();
            foreach (var b in _level.Blocks)
                if (b.CurrentColor == color) doomed.Add(b);

            foreach (var b in doomed) Remove(b);
        }

        /// <summary>
        /// Bloğu tahtadan siler ve ÇIKIŞ ZİNCİRİNİ tetikler: buzlar erir,
        /// perdeler sayar, üreteçler yer bulursa blok iter, kapılar tazelenir.
        /// </summary>
        void Remove(BlockModel block)
        {
            _level.RemoveBlock(block);

            if (_views.Blocks.TryGetValue(block, out var view))
            {
                _views.Blocks.Remove(block);

                // Yardımcının GÖRSEL imzası: oyuncu bedelini ödediği şeyin işe
                // yaradığını görmeli. Blok sessizce kaybolursa "para boşa gitti"
                // hissi doğar ve yardımcı bir daha satın alınmaz.
                if (_hasSpace)
                {
                    Vector3 at = _space.RectCenterToWorld(
                        block.Position, block.W, block.H, 0.3f);
                    if (_lastUsed == PowerUpKind.Rocket)
                        FX.PowerUpFX.Rocket(view.transform.parent, at, Color.white);
                    else if (_lastUsed == PowerUpKind.Ufo)
                        FX.PowerUpFX.Beam(view.transform.parent, at,
                            new Color(0.62f, 0.45f, 1f));
                }

                view.PlayVanish();
            }

            _obstacles.NotifyBlockExit();
            _gates.RecomputeGateStates();

            // Son bloğu yardımcı sildiyse bölüm de bitmeli. Bu çağrı olmadan
            // tahta boşalıyor ama zafer hiç tetiklenmiyordu.
            _gates.CheckCleared();
        }
    }
}
