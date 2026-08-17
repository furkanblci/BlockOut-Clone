using System;
using System.Collections;
using System.Text;
using GameKit.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.Services
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
                _progressFill.fillAmount = Mathf.Clamp01(elapsed / WatchSeconds);

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
            var art = UiKit.CreateIcon("Art", _root, UI.UiSkin.Get(UI.Art.Chest));
            UiKit.Place(art, 0.24f, 0.50f, 0.76f, 0.82f);

            var title = UiKit.CreateTitle("Title", _root, "Test Ad", 46,
                UiKit.Ink, new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(title, 0.08f, 0.40f, 0.92f, 0.49f);

            var body = UiKit.CreateLabel("Body", _root,
                "No real ad network connected.\nThe flow works exactly the same.", 26,
                new Color(1f, 1f, 1f, 0.7f));
            UiKit.Place(body, 0.08f, 0.31f, 0.92f, 0.40f);
            body.textWrappingMode = TextWrappingModes.Normal;

            // İlerleme çubuğu
            var track = UiKit.CreateSlicedPanel("Track", _root,
                UI.UiSkin.Get(UI.Art.PanelDark));
            UiKit.Place(track, 0.12f, 0.22f, 0.88f, 0.26f);

            _progressFill = UiKit.CreateSlicedPanel("Fill", track.transform,
                UI.UiSkin.Get(UI.Art.PanelCard), new Color(0.30f, 0.88f, 0.36f));
            UiKit.Place(_progressFill, 0.01f, 0.12f, 0.99f, 0.88f);
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillOrigin = 0;
            _progressFill.fillAmount = 0f;

            _countdown = UiKit.CreateTitle("Countdown", _root, "", 34,
                UiKit.Ink, new Color(0.12f, 0.09f, 0.30f));
            UiKit.Place(_countdown, 0.12f, 0.15f, 0.88f, 0.21f);

            _skip = UiKit.CreateSpriteButton("Skip", _root,
                UI.UiSkin.Get(UI.Art.ButtonPurple), "Skip (no reward)", 24, UiKit.Ink);
            UiKit.Place(_skip, 0.28f, 0.05f, 0.72f, 0.13f);
            _skip.onClick.AddListener(() => Finish(RewardedResult.Skipped));

            _root.gameObject.SetActive(false);
        }
    }
}
