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

            // GÖRSEL ALANI BÜYÜTÜLDÜ (4. tur, L44).
            //
            // Kullanıcı: "'New Feature' açılma ekranı çok detaysız,
            // orijinaliyle aynı değil. Açılan modeller detaylı gösterilecek."
            //
            // Alan ekranın %24 × %12'siydi: o kutuya sığan her şey birkaç
            // renkli dikdörtgenden ibaret kalmak zorundaydı. Referansta
            // tanıtılan nesne ekranın ortasında ve İRİ; oyuncu onu tahtada
            // göreceği hâliyle tanıyor.
            //
            // DERS (ayrıntı yer ister): "Daha detaylı çiz" demek çoğu zaman
            // önce "daha büyük çiz" demektir. Küçük bir kutuda eklenen her
            // ayrıntı gürültüye dönüşür; büyütmeden yapılan her ekleme
            // durumu kötüleştirirdi.
            var art = UiKit.CreateRect("Art", root);
            UiKit.Place(art, 0.255f, 0.398f, 0.745f, 0.648f);
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
                case Item.IceBlock:  BuildIce(holder, "3", IceBlue, IceLight); break;
                case Item.IceGate:   BuildIceGate(holder); break;
                case Item.Curtain:   BuildCurtain(holder); break;
                case Item.Layered:   BuildLayered(holder); break;
                case Item.ColorQueue: BuildColorGate(holder); break;
                case Item.Directional: BuildDirectional(holder); break;
                default:             BuildMachine(holder); break;
            }
        }

        /// <summary>Kabartmalı tuğla: koyu kenar + gövde + üst ışık + saplamalar.</summary>
        static RectTransform Brick(Transform parent, string name, Color body,
            float x0, float y0, float x1, float y1, int studCols = 2, int studRows = 2)
        {
            var root = UiKit.CreateRect(name, parent);
            UiKit.Place(root, x0, y0, x1, y1);

            var edge = UiKit.CreateSlicedPanel("Edge", root,
                UiSprites.RoundedPanel, Dim(body, 0.48f));
            UiKit.Place(edge, 0f, 0f, 1f, 1f);
            edge.raycastTarget = false;

            var face = UiKit.CreateSlicedPanel("Face", root, UiSprites.RoundedPanel, body);
            UiKit.Place(face, 0.04f, 0.14f, 0.96f, 0.96f);
            face.raycastTarget = false;

            // Saplamalar: tahtadaki tuğlanın imzası. Onlarsız her şey
            // "renkli kutu" olarak okunuyor.
            for (int i = 0; i < studCols; i++)
                for (int j = 0; j < studRows; j++)
                {
                    float u = (i + 0.5f) / studCols, v = (j + 0.5f) / studRows;
                    float r = 0.5f / Mathf.Max(studCols, studRows) * 0.62f;

                    var stud = UiKit.CreateIcon($"Stud_{i}_{j}", face.transform,
                        UiSprites.Circle, Lift(body, 0.22f));
                    UiKit.Place(stud, u - r, v - r * 1.15f, u + r, v + r * 0.85f);
                    stud.raycastTarget = false;
                }
            return root;
        }

        static Color Dim(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);

        static Color Lift(Color c, float k) => Color.Lerp(c, Color.white, k);

        /// <summary>Buz kalıbı: camgöbeği gövde, kırağı paneli ve sayaç.</summary>
        static void BuildIce(RectTransform holder, string count, Color body, Color light)
        {
            var rim = UiKit.CreateSlicedPanel("Rim", holder, UiSprites.RoundedPanel,
                Dim(body, 0.55f));
            UiKit.Place(rim, 0.28f, 0.06f, 0.72f, 0.94f);

            var ice = UiKit.CreateSlicedPanel("Ice", rim.transform, UiSprites.RoundedPanel, body);
            UiKit.Place(ice, 0f, 0f, 1f, 1f, padding: 8f);

            var frost = UiKit.CreateSlicedPanel("Frost", ice.transform,
                UiSprites.RoundedPanel, new Color(light.r, light.g, light.b, 0.75f));
            UiKit.Place(frost, 0.16f, 0.16f, 0.84f, 0.84f);

            var label = UiKit.CreateTitle("Count", ice.transform, count, 60,
                new Color(1f, 1f, 1f), Dim(body, 0.42f));
            UiKit.Place(label, 0f, 0.02f, 1f, 0.98f);
        }

        /// <summary>Buzlu kapı: SOLUK buz barı + sayaç (blok buzundan ayrı ton).</summary>
        static void BuildIceGate(RectTransform holder)
        {
            var frame = UiKit.CreateSlicedPanel("Frame", holder, UiSprites.RoundedPanel,
                new Color(0.259f, 0.224f, 0.643f));
            UiKit.Place(frame, 0.06f, 0.30f, 0.94f, 0.70f);

            var bar = UiKit.CreateSlicedPanel("Bar", holder, UiSprites.RoundedPanel,
                new Color(0.700f, 0.900f, 0.980f));
            UiKit.Place(bar, 0.28f, 0.34f, 0.72f, 0.78f);

            var label = UiKit.CreateTitle("Count", bar.transform, "5", 54,
                new Color(0.10f, 0.30f, 0.45f), new Color(1f, 1f, 1f));
            UiKit.Place(label, 0f, 0.02f, 1f, 0.98f);
        }

        /// <summary>Renk kuyruğu: iki renkli kapı barı + krem ok.</summary>
        static void BuildColorGate(RectTransform holder)
        {
            var frame = UiKit.CreateSlicedPanel("Frame", holder, UiSprites.RoundedPanel,
                new Color(0.259f, 0.224f, 0.643f));
            UiKit.Place(frame, 0.06f, 0.30f, 0.94f, 0.70f);

            var first = UiKit.CreateSlicedPanel("A", holder, UiSprites.RoundedPanel,
                new Color(0.900f, 0.150f, 0.200f));
            UiKit.Place(first, 0.20f, 0.34f, 0.50f, 0.78f);

            var second = UiKit.CreateSlicedPanel("B", holder, UiSprites.RoundedPanel,
                new Color(0.150f, 0.450f, 0.950f));
            UiKit.Place(second, 0.52f, 0.34f, 0.82f, 0.78f);

            var mark = UiKit.CreateIcon("Arrow", first.transform, UiSprites.Triangle,
                new Color(1f, 0.98f, 0.92f));
            UiKit.Place(mark, 0.30f, 0.18f, 0.70f, 0.82f);
        }

        /// <summary>Perde: altın çerçeve + lavanta panel + tırtıllar + rozet.</summary>
        static void BuildCurtain(RectTransform holder)
        {
            var frame = UiKit.CreateSlicedPanel("Frame", holder, UiSprites.RoundedPanel,
                new Color(0.937f, 0.647f, 0.106f));
            UiKit.Place(frame, 0.16f, 0.06f, 0.84f, 0.94f);

            var panel = UiKit.CreateSlicedPanel("Panel", frame.transform,
                UiSprites.RoundedPanel, new Color(0.278f, 0.220f, 0.706f));
            UiKit.Place(panel, 0f, 0f, 1f, 1f, padding: 10f);

            // Yüzeydeki yatay tırtıllar: perdeyi "levha" değil "malzeme" yapan
            // şey (tahtadaki perdeyle aynı desen).
            for (int i = 1; i <= 5; i++)
            {
                float y = i / 6f;
                var slat = UiKit.CreatePanel($"Slat_{i}", panel.transform,
                    new Color(0f, 0f, 0f, 0.16f));
                UiKit.Place(slat, 0.06f, y - 0.012f, 0.94f, y + 0.012f);
                slat.raycastTarget = false;
            }

            var badgeRim = UiKit.CreateIcon("BadgeRim", panel.transform, UiSprites.Circle,
                new Color(0.937f, 0.647f, 0.106f));
            UiKit.Place(badgeRim, 0.26f, 0.34f, 0.74f, 0.66f);

            var badge = UiKit.CreateIcon("Badge", badgeRim.transform, UiSprites.Circle,
                new Color(0.192f, 0.129f, 0.400f));
            UiKit.Place(badge, 0.10f, 0.10f, 0.90f, 0.90f);

            var count = UiKit.CreateTitle("Count", badge.transform, "3", 48, Ink, TitleShadow);
            UiKit.Place(count, 0f, 0f, 1f, 1f);
        }

        /// <summary>Katmanlı blok: dış tuğla + içine gömülü ikinci renk.</summary>
        static void BuildLayered(RectTransform holder)
        {
            var outer = Brick(holder, "Outer", new Color(1f, 0.750f, 0.100f),
                0.20f, 0.10f, 0.80f, 0.90f, 2, 2);

            var rim = UiKit.CreateSlicedPanel("InnerRim", outer, UiSprites.RoundedPanel,
                new Color(1f, 0.880f, 0.520f));
            UiKit.Place(rim, 0.20f, 0.24f, 0.80f, 0.76f);

            var inner = UiKit.CreateSlicedPanel("Inner", rim.transform,
                UiSprites.RoundedPanel, new Color(0.200f, 0.750f, 0.250f));
            UiKit.Place(inner, 0f, 0f, 1f, 1f, padding: 7f);
        }

        /// <summary>Yönlü blok: saplamasız karo + kendi renginde çift yönlü ok.</summary>
        static void BuildDirectional(RectTransform holder)
        {
            var body = new Color(0.150f, 0.450f, 0.950f);

            var edge = UiKit.CreateSlicedPanel("Edge", holder, UiSprites.RoundedPanel,
                Dim(body, 0.48f));
            UiKit.Place(edge, 0.20f, 0.10f, 0.80f, 0.90f);

            var face = UiKit.CreateSlicedPanel("Face", edge.transform,
                UiSprites.RoundedPanel, body);
            UiKit.Place(face, 0f, 0f, 1f, 1f, padding: 9f);

            // Oyunda ok bloğun KENDİ renginin açığı, koyu bir oluk hattıyla.
            var groove = UiKit.CreatePanel("Groove", face.transform, Dim(body, 0.50f));
            UiKit.Place(groove, 0.10f, 0.40f, 0.90f, 0.60f);
            var shaft = UiKit.CreatePanel("Shaft", face.transform, Lift(body, 0.40f));
            UiKit.Place(shaft, 0.14f, 0.435f, 0.86f, 0.565f);

            for (int i = 0; i < 2; i++)
            {
                bool left = i == 0;
                var headEdge = UiKit.CreateIcon($"HeadEdge_{i}", face.transform,
                    UiSprites.Triangle, Dim(body, 0.50f));
                UiKit.Place(headEdge, left ? 0.05f : 0.72f, 0.26f,
                                      left ? 0.33f : 1.00f, 0.74f);
                headEdge.rectTransform.localRotation =
                    Quaternion.Euler(0f, 0f, left ? 90f : -90f);

                var head = UiKit.CreateIcon($"Head_{i}", face.transform,
                    UiSprites.Triangle, Lift(body, 0.40f));
                UiKit.Place(head, left ? 0.08f : 0.74f, 0.30f,
                                  left ? 0.31f : 0.97f, 0.70f);
                head.rectTransform.localRotation =
                    Quaternion.Euler(0f, 0f, left ? 90f : -90f);
            }
        }

        /// <summary>
        /// Makine: gövde + sayaç başı + lamba + penceresinde sıradaki tuğla.
        ///
        /// Tahtadaki makinenin (bkz. <see cref="View.GeneratorView"/>) aynı
        /// dört parçası. Oyuncu burada gördüğü şeyi tahtada birebir tanısın
        /// diye parçalar aynı sırayla ve aynı renklerle diziliyor.
        /// </summary>
        static void BuildMachine(RectTransform holder)
        {
            var shellDark = new Color(0.435f, 0.129f, 0.514f);
            var shell = new Color(0.706f, 0.235f, 0.780f);

            var edge = UiKit.CreateSlicedPanel("Edge", holder, UiSprites.RoundedPanel, shellDark);
            UiKit.Place(edge, 0.22f, 0.04f, 0.78f, 0.96f);

            var face = UiKit.CreateSlicedPanel("Face", edge.transform,
                UiSprites.RoundedPanel, shell);
            UiKit.Place(face, 0f, 0f, 1f, 1f, padding: 10f);

            // Sayaç başı: üstte koyu plaka + rakam.
            var head = UiKit.CreateSlicedPanel("Head", face.transform,
                UiSprites.RoundedPanel, new Color(0.243f, 0.145f, 0.353f));
            UiKit.Place(head, 0.10f, 0.74f, 0.72f, 0.95f);

            var count = UiKit.CreateTitle("Count", head.transform, "6", 44, Ink, TitleShadow);
            UiKit.Place(count, 0f, 0f, 1f, 1f);

            // Lamba: referansta başın yanında küçük bir ışık.
            var lamp = UiKit.CreateIcon("Lamp", face.transform, UiSprites.Circle,
                new Color(0.937f, 0.180f, 0.180f));
            UiKit.Place(lamp, 0.76f, 0.78f, 0.94f, 0.92f);

            // Pencere: koyu oyuk + içinde sıradaki bloğun tuğlası.
            var well = UiKit.CreateSlicedPanel("Well", face.transform,
                UiSprites.RoundedPanel, new Color(0.106f, 0.086f, 0.290f));
            UiKit.Place(well, 0.10f, 0.16f, 0.90f, 0.68f);

            Brick(well.transform, "Next", new Color(0.150f, 0.450f, 0.950f),
                0.14f, 0.16f, 0.86f, 0.84f, 2, 2);

            // Ayaklar: makineyi zemine oturtan iki kısa çıkıntı.
            for (int i = 0; i < 2; i++)
            {
                var foot = UiKit.CreateSlicedPanel($"Foot_{i}", holder,
                    UiSprites.RoundedPanel, shellDark);
                float x0 = i == 0 ? 0.30f : 0.58f;
                UiKit.Place(foot, x0, -0.03f, x0 + 0.12f, 0.07f);
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
