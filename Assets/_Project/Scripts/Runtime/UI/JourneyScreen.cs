using System.Collections.Generic;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Flow;
using BlockOut.Runtime.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Yolculuk: yılankavi bir yol üzerinde bütün bölümler, aralarında bölge
    /// afişleri ve kilometre taşı ödülleri. Bölüm seçimi BURADA yapılır.
    ///
    /// DERS (bir liste yolculuğa nasıl döner): Ana ekrandaki 5x10'luk ızgara
    /// aynı bilgiyi daha az yerde gösteriyordu — ve tam da bu yüzden kötüydü.
    /// Izgara "elli görev" hissi verir; kıvrılan bir yol "nereye geldim"
    /// hissi verir. Aradaki fark oyunun kendisinde değil, ilerlemenin
    /// OKUNABİLİRLİĞİNDE: yolda önündeki düğüm bir sonraki adımdır, ızgarada
    /// ise sadece 27 numaralı kutudur.
    ///
    /// DERS (kaydırma alanı = maske + içerik): ScrollRect tek başına bir şey
    /// kırpmaz; kırpmayı üstündeki RectMask2D yapar. Maskesiz bir ScrollRect,
    /// içeriğini ekranın dışına taşırıp başlığın ve sekme çubuğunun üstüne
    /// çizer — kaydırma "çalışır" ama görüntü bozulur.
    /// </summary>
    public sealed class JourneyScreen : MonoBehaviour
    {
        /// <summary>Kilometre taşı: hangi seviyede ne verilir.</summary>
        static readonly (int level, string reward)[] Milestones =
        {
            (10, "50 jeton"),
            (15, "sınırsız can · 30 dk"),
            (25, "Yardımcı x1"),
            (45, "100 jeton"),
            (50, "sınırsız can · 1 sa"),
            (60, "Yardımcı x1"),
            (80, "250 jeton"),
            (90, "sınırsız can · 1 gün")
        };

        static readonly (string name, int from, int to)[] Regions =
        {
            ("Görev Hazırlığı", 1, 20),
            ("Yıldız Yolculuğu", 21, 40),
            ("Penguen Kovalamacası", 41, 70),
            ("Zafer Tırmanışı", 101, 150)
        };

        // Referans çözünürlükte (1080x1920) piksel ölçüleri.
        const float StepY = 190f;      // iki bölüm arası dikey mesafe
        const float NodeSize = 150f;
        const float Amplitude = 210f;  // yolun sağa sola savrulma genliği
        const float BottomPad = 300f;   // ilk bölge afişi buraya sığar
        const float TopPad = 260f;

        // Yolun noktaları iki düğüm arasının şu kesirlerine konur. 0.25 ve 0.75
        // denenmişti; düğümün yarıçapı (~65px) adımın (190px) üçte birine yakın
        // olduğu için o noktalar düğümün ALTINDA kalıp görünmüyordu.
        static readonly float[] DotSteps = { 0.35f, 0.5f, 0.65f };

        readonly List<(Button button, TextMeshProUGUI label, Image face, Image badge, int index)> _nodes =
            new List<(Button, TextMeshProUGUI, Image, Image, int)>();

        TextMeshProUGUI _progressLabel;
        ScrollRect _scroll;
        RectTransform _content;
        bool _built;
        bool _centeredOnce;

        // Her bölümün yoldaki dikey konumu. Sabit bir çarpım yerine dizi
        // tutuluyor çünkü bölge başlarına afiş için FAZLADAN boşluk açılıyor;
        // aksi hâlde afiş iki düğümün üstüne biniyordu.
        float[] _levelY;

        public static RectTransform Build(Transform parent)
        {
            var root = MenuShell.Screen(parent, "JourneyScreen");
            var screen = root.gameObject.AddComponent<JourneyScreen>();

            screen.BuildScrollArea(root);
            screen.BuildPath();

            // Başlık ve ilerleme şeridi yolun ÜSTÜNDE durur: kaydırma sırasında
            // "kaçıncı bölümdeyim" bilgisi ekrandan çıkmasın.
            MenuShell.Header(root, "Yolculuk");
            screen._progressLabel = UiKit.CreateLabel("Progress", root, "", 32,
                new Color(1f, 1f, 1f, 0.85f));
            UiKit.Place(screen._progressLabel, 0.04f, 0.875f, 0.96f, 0.918f);

            screen._built = true;
            return root;
        }

        void BuildScrollArea(Transform root)
        {
            var viewport = UiKit.CreateRect("Viewport", root);
            UiKit.Place(viewport, 0f, 0f, 1f, 0.87f);
            viewport.gameObject.AddComponent<RectMask2D>();

            _content = UiKit.CreateRect("Path", viewport);
            _content.anchorMin = new Vector2(0f, 0f);
            _content.anchorMax = new Vector2(1f, 0f);
            _content.pivot = new Vector2(0.5f, 0f);

            LayOutLevels();
            _content.sizeDelta = new Vector2(0f, _levelY[_levelY.Length - 1] + TopPad);
            _content.anchoredPosition = Vector2.zero;

            _scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.content = _content;
            _scroll.viewport = viewport;
            _scroll.horizontal = false;
            _scroll.movementType = ScrollRect.MovementType.Elastic;
            _scroll.elasticity = 0.08f;
            _scroll.scrollSensitivity = 40f;
            _scroll.inertia = true;
            _scroll.decelerationRate = 0.12f;
        }

        /// <summary>Bölüm konumlarını hesaplar; bölge başlarına afiş boşluğu açar.</summary>
        void LayOutLevels()
        {
            int count = Mathf.Max(1, LevelCatalog.Count);
            _levelY = new float[count];

            float y = BottomPad;
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    y += StepY;
                    if (StartsRegion(i + 1)) y += StepY * 0.85f;   // afiş için pay
                }
                _levelY[i] = y;
            }
        }

        static bool StartsRegion(int level)
        {
            foreach (var (_, from, _) in Regions)
                if (from == level) return true;
            return false;
        }

        void BuildPath()
        {
            int count = LevelCatalog.Count;
            if (count == 0)
            {
                UiKit.CreateLabel("Empty", _content, "Bölüm bulunamadı", 36, UiKit.Ink);
                return;
            }

            AddRegionBanners(count);       // önce kurulur ki düğümlerin ARKASINDA kalsın

            // Yolun kendisi: düğümler arasına serpilmiş noktalar. Tek parça bir
            // çizgi yerine nokta dizisi kullanmak, kıvrımı ayrı bir mesh
            // üretmeden veriyor.
            for (int i = 0; i < count - 1; i++)
                foreach (float t in DotSteps)
                {
                    // Yuvarlak panel sprite'ı kullanılıyor: UiSprites.Circle bu
                    // bağlamda çizilmedi (sprite, doku, boyut ve konum doğru
                    // olduğu hâlde piksel gelmedi), panel sprite'ı ise projenin
                    // her yerinde çalışıyor. Kenar payı çarpanı küçültülünce
                    // 9-dilim köşeleri tamamen yuvarlanıp daireye dönüyor.
                    var dot = UiKit.CreateRoundedPanel($"Dot_{i}_{t}", _content,
                        new Color(1f, 0.94f, 0.78f, 0.85f));
                    dot.raycastTarget = false;
                    dot.pixelsPerUnitMultiplier = 0.2f;
                    Anchor(dot.rectTransform,
                        Mathf.Lerp(PathX(i), PathX(i + 1), t),
                        Mathf.Lerp(_levelY[i], _levelY[i + 1], t), 28f, 28f);
                }

            for (int i = 0; i < count; i++)
            {
                int level = i + 1;

                var button = UiKit.CreateSpriteButton($"Node_{level}", _content,
                    UiSkin.Get(Art.LevelNode), null, 0, UiKit.Ink);
                Anchor(button.GetComponent<RectTransform>(), PathX(i), _levelY[i], NodeSize, NodeSize);

                var label = UiKit.CreateTitle("Num", button.transform, level.ToString(), 48,
                    UiKit.Ink, new Color(0.16f, 0.11f, 0.36f));
                UiKit.Place(label, 0f, 0.06f, 1f, 0.94f);

                // Rozet: bitirilmişse yıldız, kilitliyse asma kilit. Düğümün
                // sağ üstüne, hafifçe taşarak oturur.
                var badge = UiKit.CreateIcon("Badge", button.transform, null);
                UiKit.Place(badge, 0.52f, 0.52f, 1.16f, 1.16f);

                int index = i;
                button.onClick.AddListener(() => Play(index));
                _nodes.Add((button, label, button.targetGraphic as Image, badge, index));

                AddSideCard(level, i);
            }
        }

        /// <summary>Kilometre taşı ödülü — yolun boş kalan tarafına asılır.</summary>
        void AddSideCard(int level, int i)
        {
            string reward = null;
            foreach (var (milestoneLevel, text) in Milestones)
                if (milestoneLevel == level) reward = text;
            if (reward == null) return;

            // Düğüm solda ise kart sağda: ikisi üst üste binmesin.
            float side = PathX(i) < 0f ? 255f : -255f;

            // Kart 9-dilim payları her kenarda 60px; 130 yükseklikte üst ve alt
            // pay (120) neredeyse tüm alanı yiyip ortayı eziyordu. 200 vererek
            // esneyecek gerçek bir orta alan bırakılıyor.
            var card = UiKit.CreateSlicedPanel($"Reward_{level}", _content,
                UiSkin.Get(Art.PanelCard));
            Anchor(card.rectTransform, side, _levelY[i], 380f, 210f);

            var chest = UiKit.CreateIcon("Chest", card.transform, UiSkin.Get(Art.Chest));
            UiKit.Place(chest, 0.06f, 0.36f, 0.44f, 0.92f);

            // Kart krem renkli; üstüne beyaz yazı okunmaz.
            var caption = UiKit.CreateLabel("Text", card.transform, reward, 22,
                new Color(0.30f, 0.16f, 0.05f));
            UiKit.Place(caption, 0.10f, 0.14f, 0.90f, 0.42f);
            caption.textWrappingMode = TextWrappingModes.Normal;

            var levelTag = UiKit.CreateLabel("Level", card.transform, $"sv {level}", 22,
                new Color(0.55f, 0.30f, 0.10f));
            UiKit.Place(levelTag, 0.46f, 0.55f, 0.94f, 0.88f);
        }

        void AddRegionBanners(int count)
        {
            foreach (var (name, from, to) in Regions)
            {
                if (from > count) continue;               // henüz o kadar bölüm yok

                // Afiş, bölge başındaki düğümün ALTINDA açılan boşluğa oturur.
                // İlk bölge için altında düğüm yok; onu da alt payın içine al.
                float y = from == 1
                    ? _levelY[0] - StepY * 0.72f
                    : (_levelY[from - 1] + _levelY[from - 2]) * 0.5f;

                var banner = UiKit.CreateSlicedPanel($"Region_{from}", _content,
                    UiSkin.Get(Art.RegionBanner));
                Anchor(banner.rectTransform, 0f, y, 660f, 155f);

                var label = UiKit.CreateTitle("Name", banner.transform, name, 34,
                    UiKit.Ink, new Color(0.20f, 0.10f, 0.32f));
                UiKit.Place(label, 0.22f, 0.34f, 0.78f, 0.72f);

                var range = UiKit.CreateLabel("Range", banner.transform, $"{from}-{to}", 22,
                    new Color(1f, 1f, 1f, 0.75f));
                UiKit.Place(range, 0.22f, 0.16f, 0.78f, 0.36f);
            }
        }

        static float PathX(float step) => Mathf.Sin(step * 0.85f) * Amplitude;

        /// <summary>Kaydırma içeriğinde MUTLAK yerleştirme (oran değil piksel).</summary>
        static void Anchor(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
        }

        void OnEnable()
        {
            Refresh();
            _centeredOnce = false;      // ekran her açılışında güncel bölüme dön
        }

        void LateUpdate()
        {
            // Ortalama, yerleşim hesaplandıktan SONRA yapılmalı; ilk karede
            // içerik yüksekliği henüz kesinleşmemiş oluyor.
            if (_centeredOnce || !_built || _scroll == null) return;
            _centeredOnce = true;
            CenterOnCurrent();
        }

        void CenterOnCurrent()
        {
            if (!MetaServices.Ready || LevelCatalog.Count == 0) return;

            int current = Mathf.Clamp(MetaServices.Progress.HighestUnlockedIndex,
                0, LevelCatalog.Count - 1);
            float viewport = _scroll.viewport.rect.height;
            float span = Mathf.Max(1f, _content.rect.height - viewport);

            // Güncel bölüm ekranın alt üçte birine gelsin: önündeki yol görünsün.
            float target = _levelY[current] - viewport * 0.35f;
            _scroll.verticalNormalizedPosition = Mathf.Clamp01(target / span);
        }

        public void Refresh()
        {
            if (!_built || !MetaServices.Ready) return;

            var progress = MetaServices.Progress;
            int reached = progress.HighestUnlockedIndex + 1;

            int next = int.MaxValue;
            foreach (var (level, _) in Milestones)
                if (level > reached && level < next) next = level;

            _progressLabel.text = next == int.MaxValue
                ? $"Seviye {reached} · tüm ödüller alındı"
                : $"Seviye {reached} · sonraki ödüle {next - reached} bölüm";

            bool hasLife = MetaServices.Lives.HasLife;

            foreach (var (button, label, face, badge, index) in _nodes)
            {
                bool unlocked = progress.IsUnlocked(index);
                var record = progress.Record(LevelCatalog.IdAt(index));
                bool current = index == progress.HighestUnlockedIndex;

                button.interactable = unlocked && hasLife;

                if (face != null)
                    face.color = unlocked ? Color.white : new Color(0.45f, 0.44f, 0.52f);

                // Kilitli bölümün numarası da görünsün: "kaçıncı bölüme
                // bakıyorum" sorusu kilitliyken de sorulur.
                label.text = (index + 1).ToString();
                label.color = unlocked ? UiKit.Ink : new Color(1f, 1f, 1f, 0.45f);

                if (badge != null)
                {
                    badge.sprite = !unlocked ? UiSkin.Get(Art.Lock)
                        : record.Perfect ? UiSkin.Get(Art.Star)
                        : record.Cleared ? UiSkin.Get(Art.Star)
                        : null;
                    badge.color = record.Perfect ? Color.white : new Color(0.85f, 0.85f, 0.9f);
                    badge.enabled = badge.sprite != null;
                }

                // Sıradaki bölüm biraz büyük dursun — gözün gideceği yer belli olsun.
                button.transform.localScale = Vector3.one * (current ? 1.22f : 1f);
            }
        }

        static void Play(int index)
        {
            if (MetaServices.Ready && !MetaServices.Lives.HasLife) return;
            AppRouter.PlayLevel(index);
        }
    }
}
