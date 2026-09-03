using System;
using System.Collections;
using System.Text;
using GameKit.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace GameKit.Services
{
    /// <summary>
    /// Ödüllü reklam benzetimi: gerçek bir reklam ağı bağlanana kadar aynı
    /// SÖZLEŞMEYİ uygular.
    ///
    /// DERS (ödül geri çağrıyla verilir): En sık yapılan hata "reklamı
    /// gösterdim, ödülü hemen vereyim"dir. Oyuncu reklamı ilk saniyede
    /// kapatınca da ödül alır ve model çöker. Burada ödül YALNIZCA geri sayım
    /// bitip <see cref="RewardedResult.Completed"/> döndüğünde veriliyor;
    /// erken kapatma <see cref="RewardedResult.Skipped"/> üretiyor.
    ///
    /// DERS (atlama düğmesi GECİKMELİ görünür): Reklam açılır açılmaz kapatma
    /// düğmesi çıkarsa kimse izlemez. Sektörde standart, ödüllü reklamda
    /// atlamanın hiç olmaması ya da birkaç saniye sonra çıkmasıdır. Burada
    /// beş saniye sonra çıkıyor ve "ödülü alamazsın" diye açıkça yazıyor —
    /// oyuncuyu kandırmadan caydırmak doğru olanı.
    /// </summary>
    public sealed class FakeAdScreen : MonoBehaviour, IAdProvider
    {
        const float WatchSeconds = 8f;
        const float SkipAppearsAfter = 5f;

        static FakeAdScreen _instance;

        public static FakeAdScreen Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("Ads");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<FakeAdScreen>();
                return _instance;
            }
        }

        Canvas _canvas;
        RectTransform _root;
        TextMeshProUGUI _countdown, _placementLabel;
        Button _skip;
        Image _progressFill;

        /// <summary>Yuvanın içi ve dolgunun tabanı.</summary>
        static readonly Color TrackDark = new Color(0.129f, 0.110f, 0.325f);
        static readonly Color FillGreen = new Color(0.220f, 0.843f, 0.078f);

        /// <summary>
        /// Dolgunun genişliğini yuvanın oranına göre kurar. Kırpma yok:
        /// dolgu her zaman iki ucu yuvarlak bir nesne.
        /// </summary>
        void SetProgress(float amount)
        {
            if (_progressFill == null) return;
            var fill = _progressFill.rectTransform;
            if (fill.parent is not RectTransform track) return;

            float inner = Mathf.Max(0f, track.rect.width - 8f);
            float height = Mathf.Max(1f, track.rect.height - 8f);

            // Uçları yuvarlak olduğu için dolgu YÜKSEKLİĞİNDEN kısa olamaz;
            // sıfırda bile bir nokta kalır, bu da "başladı" demektir.
            float width = Mathf.Lerp(height, inner, Mathf.Clamp01(amount));
            fill.sizeDelta = new Vector2(width, fill.sizeDelta.y);
        }

        readonly StringBuilder _scratch = new StringBuilder(24);
        Action<RewardedResult> _pending;
        bool _showing;

        public bool IsRewardedReady => !_showing;

        public void Initialize() { /* gerçek SDK burada başlatılır */ }

        public void ShowInterstitial(string placement)
        {
            // Araya giren reklam ödül vermez; benzetimde aynı ekran, ödülsüz.
            ShowRewarded(placement, null);
        }

        public void ShowRewarded(string placement, Action<RewardedResult> onFinished)
        {
            if (_showing)
            {
                onFinished?.Invoke(RewardedResult.Unavailable);
                return;
            }

            EnsureUi();
            _pending = onFinished;
            _showing = true;
            _placementLabel.text = placement;
            _root.gameObject.SetActive(true);
            StartCoroutine(Playback());
        }

        IEnumerator Playback()
        {
            _skip.gameObject.SetActive(false);
            float elapsed = 0f;

            while (elapsed < WatchSeconds)
            {
                if (!_showing) yield break;         // atlandı

                elapsed += Time.unscaledDeltaTime;
                int left = Mathf.CeilToInt(WatchSeconds - elapsed);
                _countdown.text = _scratch.Clear().Append(left).Append(" sn").ToString();
                SetProgress(elapsed / WatchSeconds);

                if (elapsed >= SkipAppearsAfter && !_skip.gameObject.activeSelf)
                    _skip.gameObject.SetActive(true);

                yield return null;
            }

            Finish(RewardedResult.Completed);
        }

        void Finish(RewardedResult outcome)
        {
            if (!_showing) return;
            _showing = false;
            _root.gameObject.SetActive(false);

            var callback = _pending;
            _pending = null;
            Analytics.LogRewardedAd(_placementLabel.text, outcome);
            callback?.Invoke(outcome);
        }

        // ------------------------------------------------------------------ arayüz

        void EnsureUi()
        {
            if (_canvas != null) return;

            _canvas = UiKit.CreateCanvas("AdCanvas");
            _canvas.transform.SetParent(transform, worldPositionStays: false);
            _canvas.sortingOrder = 500;          // her şeyin üstünde

            _root = UiKit.CreateRect("Ad", _canvas.transform);
            UiKit.Place(_root, 0f, 0f, 1f, 1f);

            UiKit.CreatePanel("Bg", _root, new Color(0.06f, 0.05f, 0.16f, 1f));

            var badge = UiKit.CreateLabel("Badge", _root, "REKLAM", 24,
                new Color(1f, 1f, 1f, 0.55f));
            UiKit.Place(badge, 0.04f, 0.93f, 0.40f, 0.97f);
            badge.alignment = TextAlignmentOptions.Left;

            _placementLabel = UiKit.CreateLabel("Placement", _root, "", 20,
                new Color(1f, 1f, 1f, 0.32f));
            UiKit.Place(_placementLabel, 0.60f, 0.93f, 0.96f, 0.97f);
            _placementLabel.alignment = TextAlignmentOptions.Right;

            // Sahte reklam gövdesi: oyunun kendi görselleriyle kurulmuş bir
            // yer tutucu. Boş siyah ekran, akışın çalıştığını göstermez.
            var art = UiKit.CreateIcon("Art", _root, Screens.UiSkin.Get(Screens.Art.Chest));
            UiKit.Place(art, 0.24f, 0.50f, 0.76f, 0.82f);

            var title = UiKit.CreateTitle("Title", _root, "Test Ad", 46,
                UiKit.Ink, new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(title, 0.08f, 0.40f, 0.92f, 0.49f);

            var body = UiKit.CreateLabel("Body", _root,
                "No real ad network connected.\nThe flow works exactly the same.", 26,
                new Color(1f, 1f, 1f, 0.7f));
            UiKit.Place(body, 0.08f, 0.31f, 0.92f, 0.40f);
            body.textWrappingMode = TextWrappingModes.Normal;

            // İLERLEME ÇUBUĞU (10. tur).
            //
            // Eskiden dolgu `Image.Type.Filled` ile kırpılıyordu. Kırpma
            // görüntüyü DÜZ BİR ÇİZGİYLE kesiyor: yuvarlak uçlu bir dokunun
            // sağ ucu her karede kare çıkıyor, üstelik dokunun sol köşesi
            // çubuğun tamamına gerildiği için solda ikinci bir açık blok
            // beliriyordu. Kullanıcının gönderdiği görüntüde ikisi de var.
            //
            // DERS (dolgu KIRPMAK DEĞİL, BÜYÜTMEKTİR): Bir ilerleme çubuğunun
            // dolgusu, uçları yuvarlak kalması gereken bir NESNEDİR. Onu
            // maskeyle kesmek yerine genişliğini değiştirmek hem doğru
            // görünüyor hem de tek satır.
            var track = UiKit.CreateRoundedPanel("Track", _root, TrackDark, 0.5f);
            UiKit.Place(track, 0.12f, 0.22f, 0.88f, 0.26f);

            _progressFill = UiKit.CreateRoundedPanel("Fill", track.transform,
                                                     Color.white, 0.5f);
            _progressFill.raycastTarget = false;
            // Oyunun her yerindeki ışık profili: üstte parlak, dipte koyu.
            _progressFill.gameObject.AddComponent<GameKit.UI.UiVerticalTint>()
                .Set(Screens.MenuPage.Brighten(FillGreen, 1.19f),
                     Screens.MenuPage.Darken(FillGreen, 0.78f));

            var fill = _progressFill.rectTransform;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = new Vector2(4f, 4f);
            fill.offsetMax = new Vector2(4f, -4f);
            SetProgress(0f);

            _countdown = UiKit.CreateTitle("Countdown", _root, "", 34,
                UiKit.Ink, new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(_countdown, 0.12f, 0.15f, 0.88f, 0.21f);

            // Oyunun standart düğmesi (8. tur): `btn_purple.png` görselini
            // kullanan SON yer burasıydı.
            _skip = Screens.MenuPage.PillButton("Skip", _root,
                "Skip (no reward)", new Color(0.522f, 0.110f, 0.988f), 24, null);
            UiKit.Place(_skip, 0.28f, 0.05f, 0.72f, 0.13f);
            _skip.onClick.AddListener(() => Finish(RewardedResult.Skipped));

            _root.gameObject.SetActive(false);
        }
    }
}
