using BlockOut.Core;
using BlockOut.Runtime.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PT = PrimeTween;
using UiKit = GameKit.UI.UiKit;
using UiSprites = GameKit.UI.UiSprites;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Yeni mekanik tanıtımı: "Ice Blocks! — New Item Unlocked!"
    ///
    /// DERS (tanıtım KART DEĞİL, SPOT IŞIĞIDIR): İlk tasarım refleksi bunu bir
    /// kart yapmak — başlık, açıklama, kapat düğmesi. Referans öyle yapmıyor:
    /// tahta yerinde kalıyor, üstüne koyu bir perde iniyor ve YALNIZCA tanıtılan
    /// öğe aydınlatılıyor. Fark önemli, çünkü kart "şimdi sana bir şey
    /// anlatacağım" der ve oyuncuyu oyundan koparır; spot ışığı "şuna bak" der
    /// ve oyuncu hâlâ tahtaya bakıyordur. Tanıtılan şeyin nerede yaşadığını da
    /// bedavaya öğretmiş olur.
    ///
    /// DERS (bir mekanik bir kez tanıtılır): Görüldüğü PlayerPrefs'e yazılıyor.
    /// Aynı mekaniği her karşılaşmada tanıtmak, oyunu bilen oyuncuyu her bölümde
    /// bir kez durdurmak demek.
    ///
    /// DERS (bölüm başına EN FAZLA BİR tanıtım): Bir bölümde iki yeni mekanik
    /// birden çıkabiliyor. İkisini arka arkaya göstermek oyuncuyu iki kez
    /// durdurur ve ikisi de akılda kalmaz. Sırayla, bölüm bölüm tanıtılıyorlar.
    /// </summary>
    public sealed class NewItemPanel : MonoBehaviour
    {
        /// <summary>Tanıtılabilecek mekanikler — tanıtım sırasıyla.</summary>
        enum Item { Layered, ColorQueue, IceBlock, IceGate, Curtain, Directional, Generator }

        static readonly Color Scrim       = new Color(0.020f, 0.012f, 0.060f, 0.88f);
        static readonly Color Ink         = new Color(1f, 0.98f, 0.94f);
        static readonly Color TitleShadow = new Color(0.290f, 0.160f, 0.620f);
        static readonly Color HintCream   = new Color(1f, 0.925f, 0.820f);
        static readonly Color HintInk     = new Color(0.290f, 0.160f, 0.620f);
        static readonly Color CloseRed    = new Color(0.898f, 0.196f, 0.235f);
        static readonly Color GlowWarm    = new Color(1f, 0.86f, 0.45f, 0.55f);
        static readonly Color IceBlue     = new Color(0.259f, 0.671f, 0.980f);
        static readonly Color IceLight    = new Color(0.596f, 0.882f, 1f);

        GameSession _session;
        Canvas _canvas;

        /// <summary>Bölüm kurulduktan sonra çağrılır; gerekmiyorsa hiçbir şey yapmaz.</summary>
        public static void TryShow(GameSession session, LevelModel level)
        {
            if (session == null || level == null) return;
            if (!TryPickItem(level, out var item)) return;

            var go = new GameObject("NewItemPanel");
            go.transform.SetParent(session.transform, worldPositionStays: false);
            go.AddComponent<NewItemPanel>().Begin(session, item);
        }

        // ---------------------------------------------------------------- seçim

        static string SeenKey(Item item) => "BlockOut.ItemSeen." + item;

        /// <summary>
        /// Bu bölümde bulunan, henüz tanıtılmamış İLK mekaniği seçer.
        /// Sıra enum'daki sıra: önce en temel olan.
        /// </summary>
        static bool TryPickItem(LevelModel level, out Item item)
        {
            item = default;
            for (int i = 0; i <= (int)Item.Generator; i++)
            {
                var candidate = (Item)i;
                if (PlayerPrefs.GetInt(SeenKey(candidate), 0) == 1) continue;
                if (!LevelHas(level, candidate)) continue;
                item = candidate;
                return true;
            }
            return false;
        }

        static bool LevelHas(LevelModel level, Item item)
        {
            switch (item)
            {
                case Item.Layered:
                    foreach (var b in level.Blocks) if (b.Layers.Count > 1) return true;
                    return false;

                case Item.ColorQueue:
                    foreach (var g in level.Gates) if (g.ColorQueue.Count > 1) return true;
                    return false;

                case Item.IceBlock:
                    foreach (var b in level.Blocks) if (b.IsFrozen) return true;
                    return false;

                case Item.IceGate:
                    foreach (var g in level.Gates) if (g.IsIced) return true;
                    return false;

                case Item.Curtain:
                    foreach (var o in level.Obstacles) if (o is CurtainModel) return true;
                    return false;

                case Item.Directional:
                    foreach (var b in level.Blocks) if (b.Axis != MoveAxis.Free) return true;
                    return false;

                case Item.Generator:
                    foreach (var o in level.Obstacles) if (o is GeneratorModel) return true;
                    return false;
            }
            return false;
        }

        static (string title, string hint) TextFor(Item item)
        {
            switch (item)
            {
                case Item.Layered:     return ("Color Layers!", "Clear the top color to reveal the next!");
                case Item.ColorQueue:  return ("Color Gates!",  "Gates change color as blocks pass through!");
                case Item.IceBlock:    return ("Ice Blocks!",   "Clear blocks to break the ice!");
                case Item.IceGate:     return ("Ice Doors!",    "Clear blocks to melt the door!");
                case Item.Curtain:     return ("Curtains!",     "Clear blocks to open the curtain!");
                case Item.Directional: return ("Locked Blocks!", "These blocks slide on one axis only!");
                default:               return ("Block Machines!", "New blocks arrive when space frees up!");
            }
        }

        // ---------------------------------------------------------------- kurulum

        void Begin(GameSession session, Item item)
        {
            _session = session;

            // Sayaç yalnız Playing durumunda işliyor; duraklatmak onu durdurur.
            // Duraklat KARTI ayrı bir bayrakla açıldığı için burada görünmüyor.
            _session.SetPaused(true);

            _canvas = UiKit.CreateCanvas("NewItemCanvas");
            _canvas.transform.SetParent(transform, worldPositionStays: false);
            _canvas.sortingOrder = 25;                  // öğreticinin de üstünde
            var root = UiKit.CreateSafeArea(_canvas);

            // Perde tahtayı karartıyor ve altındaki dokunuşu yutuyor.
            var scrim = UiKit.CreatePanel("Scrim", root, Scrim);
            scrim.raycastTarget = true;

            var (title, hint) = TextFor(item);

            var titleLabel = UiKit.CreateTitle("Title", root, title, 84, Ink, TitleShadow);
            UiKit.Place(titleLabel, 0.06f, 0.758f, 0.94f, 0.820f);
            titleLabel.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.SetOutline(titleLabel, TitleShadow);

            var subtitle = UiKit.CreateTitle("Subtitle", root, "New Item Unlocked!", 40,
                Ink, TitleShadow);
            UiKit.Place(subtitle, 0.06f, 0.700f, 0.94f, 0.740f);
            UiKit.SetOutline(subtitle, TitleShadow);

            // Spot ışığı: öğenin arkasında, öğeden ÖNCE ekleniyor ki arkada kalsın.
            var glow = UiKit.CreateIcon("Glow", root, UiSprites.Burst, GlowWarm);
            glow.raycastTarget = false;
            UiKit.Place(glow, 0.06f, 0.360f, 0.94f, 0.690f);

            var art = UiKit.CreateRect("Art", root);
            UiKit.Place(art, 0.385f, 0.461f, 0.622f, 0.582f);
            BuildArt(art, item);

            var sparkles = BuildSparkles(root);

            // İpucu: DÜZ krem kutu, ince mor çerçeve, mor yazı. Referansta
            // 3B dudak yok; panel_card ile kurulunca baskılı gölgesi kreme
            // boyanınca magentaya kayıyor ve oyunun hiçbir yerinde olmayan bir
            // şerit bırakıyordu.
            var hintCard = UiKit.CreateOutlinedBox("Hint", root, HintCream, HintInk);
            UiKit.Place(hintCard, 0.084f, 0.270f, 0.917f, 0.340f);

            var hintLabel = UiKit.CreateLabel("Text", hintCard.transform, hint, 34, HintInk);
            hintLabel.fontStyle = FontStyles.Bold;
            hintLabel.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Place(hintLabel, 0.05f, 0.08f, 0.95f, 0.92f);

            var close = UiKit.CreateIconButton("Close", root, UiSprites.Circle, CloseRed);
            UiKit.Place(close, 0.845f, 0.867f, 0.946f, 0.910f);
            close.onClick.AddListener(Dismiss);

            var crossMark = UiKit.CreateIcon("Mark", close.transform, UiSprites.Cross, Ink);
            crossMark.raycastTarget = false;
            UiKit.Place(crossMark, 0.24f, 0.24f, 0.76f, 0.76f);

            PlayEntrance(titleLabel.transform, subtitle.transform, glow.transform,
                art, hintCard.transform, close.transform, sparkles);

            // Tanıtımın kendi sesi var: bu panel "bir şey açıldı" diyor,
            // sıradan bir panel açılışından daha görkemli olmalı.
            Services.AudioService.Unlock();

            PlayerPrefs.SetInt(SeenKey(item), 1);
            PlayerPrefs.Save();
        }

        // ---------------------------------------------------------------- görsel

        /// <summary>
        /// Tanıtılan öğenin temsili görseli.
        ///
        /// DERS (temsil, KOPYA olmak zorunda değil): Tahtadaki gerçek nesneyi
        /// buraya taşımak (aynı mesh, aynı kabuk) cazip ama pahalı: o nesneler
        /// 3B ve arayüz kanvası 2B. Oyuncunun tanıması için siluet ve renk
        /// yeterli — buz mavi ve parlak, perde koyu ve sayılı. Bire bir aynı
        /// olmaları değil, AYNI ŞEY olarak okunmaları gerekiyor.
        /// </summary>
        void BuildArt(RectTransform holder, Item item)
        {
            switch (item)
            {
                case Item.IceBlock:
                case Item.IceGate:
                    // Referanstaki buz: açık mavi gövde, ÜST YARIDA geniş bir
                    // camsı şerit ve etrafında daha açık ince bir kenar.
                    // Küçük bir açık kare "buz" değil "içinde nokta olan kutu"
                    // okunuyordu — parlaklık geniş ve kenara yakın olmalı.
                    var rim = UiKit.CreateSlicedPanel("Rim", holder,
                        UiSprites.RoundedPanel, IceLight);
                    UiKit.Place(rim, 0f, 0f, 1f, 1f);

                    var ice = UiKit.CreateSlicedPanel("Ice", holder,
                        UiSprites.RoundedPanel, IceBlue);
                    UiKit.Place(ice, 0.055f, 0.055f, 0.945f, 0.945f);

                    var shine = UiKit.CreateSlicedPanel("Shine", holder,
                        UiSprites.RoundedPanel,
                        new Color(IceLight.r, IceLight.g, IceLight.b, 0.55f));
                    UiKit.Place(shine, 0.14f, 0.52f, 0.86f, 0.88f);
                    break;

                case Item.Curtain:
                    var curtain = UiKit.CreateIcon("Curtain", holder, UiSprites.RoundedPanel,
                        new Color(0.180f, 0.145f, 0.420f));
                    UiKit.Place(curtain, 0f, 0f, 1f, 1f);
                    var count = UiKit.CreateTitle("Count", holder, "3", 52, Ink, TitleShadow);
                    UiKit.Place(count, 0f, 0f, 1f, 1f);
                    break;

                case Item.Layered:
                    // İki katman: alttaki geniş, üstteki dar ve kaymış —
                    // "birinin altında bir tane daha var" siluetı.
                    var under = UiKit.CreateIcon("Under", holder, UiSprites.RoundedPanel,
                        new Color(0.180f, 0.800f, 0.290f));
                    UiKit.Place(under, 0.10f, 0f, 1f, 0.82f);
                    var over = UiKit.CreateIcon("Over", holder, UiSprites.RoundedPanel,
                        new Color(0.950f, 0.290f, 0.310f));
                    UiKit.Place(over, 0f, 0.18f, 0.90f, 1f);
                    break;

                default:
                    var plain = UiKit.CreateIcon("Item", holder, UiSprites.RoundedPanel,
                        new Color(0.980f, 0.760f, 0.180f));
                    UiKit.Place(plain, 0f, 0f, 1f, 1f);
                    break;
            }
        }

        /// <summary>Öğenin çevresine saçılan altın kıvılcımlar.</summary>
        RectTransform[] BuildSparkles(Transform root)
        {
            var star = UiSkin.Get(Art.Star);

            // Elle yazılmış konumlar: rastgele saçılım her açılışta başka
            // görünür ve arayüz "oturmamış" hissettirir.
            // Küçük tutuluyorlar: kıvılcım büyüdüğü anda "yıldız ikonu" olarak
            // okunuyor ve tanıtılan öğeyle yarışıyor. Amaç parıltı, süs değil.
            var spots = new[]
            {
                (0.315f, 0.632f, 0.015f), (0.690f, 0.618f, 0.013f),
                (0.272f, 0.523f, 0.011f), (0.735f, 0.538f, 0.016f),
                (0.330f, 0.432f, 0.013f), (0.668f, 0.425f, 0.011f),
                (0.500f, 0.668f, 0.012f), (0.500f, 0.392f, 0.014f),
            };

            var result = new RectTransform[spots.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                var (cx, cy, r) = spots[i];
                var piece = star != null
                    ? UiKit.CreateIcon($"Spark_{i}", root, star, new Color(1f, 0.88f, 0.42f))
                    : UiKit.CreateIcon($"Spark_{i}", root, UiSprites.Circle, new Color(1f, 0.88f, 0.42f));
                piece.raycastTarget = false;
                UiKit.Place(piece, cx - r, cy - r * 1.8f, cx + r, cy + r * 1.8f);
                result[i] = piece.rectTransform;
            }
            return result;
        }

        // ---------------------------------------------------------------- hareket

        /// <summary>
        /// Giriş: perde açılır, başlık iner, öğe yaylanır, kıvılcımlar sönüp yanar.
        ///
        /// DERS (sıra ANLATIR): Hepsi aynı anda gelirse ekran "açıldı" der.
        /// Başlık → öğe → ipucu sırası ise bir cümle kurar: "yeni bir şey var,
        /// şu, şöyle çalışıyor." Toplam yarım saniye, ama okunuşu tamamen farklı.
        /// </summary>
        void PlayEntrance(Transform title, Transform subtitle, Transform glow,
            Transform art, Transform hint, Transform close, RectTransform[] sparkles)
        {
            GameKit.FX.Juice.PopIn(title, 0.34f);
            GameKit.FX.Juice.PopIn(subtitle, 0.30f, 0.08f);
            GameKit.FX.Juice.PopIn(art, 0.42f, 0.14f);
            GameKit.FX.Juice.PopIn(hint, 0.32f, 0.30f);
            GameKit.FX.Juice.PopIn(close, 0.26f, 0.38f);

            // Işık huzmesi dönerek yaşıyor: sabit bir hüzme çıkartma gibi durur.
            PT.Tween.EulerAngles(glow, Vector3.zero, new Vector3(0f, 0f, 360f),
                24f, PT.Ease.Linear, cycles: -1, useUnscaledTime: true);

            for (int i = 0; i < sparkles.Length; i++)
            {
                if (sparkles[i] == null) continue;
                sparkles[i].localScale = Vector3.zero;

                // Her kıvılcım farklı gecikmeyle sönüp yanıyor; aynı anda
                // yanıp sönerlerse yanıp sönen tek bir ışık gibi okunur.
                PT.Tween.Scale(sparkles[i], 0f, 1f, 0.9f, PT.Ease.InOutSine,
                    cycles: -1, cycleMode: PT.CycleMode.Yoyo,
                    startDelay: 0.25f + i * 0.11f, useUnscaledTime: true);
            }
        }

        void Dismiss()
        {
            if (_session != null) _session.SetPaused(false);
            Destroy(gameObject);
        }
    }
}
