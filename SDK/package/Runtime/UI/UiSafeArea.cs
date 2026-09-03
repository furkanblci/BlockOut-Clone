using UnityEngine;

namespace GameKit.UI
{
    /// <summary>
    /// Güvenli alanı ekran her değiştiğinde yeniden uygular.
    ///
    /// DERS (kurulumu değiştirmek yetmez, TAZELEMEYİ de değiştir — bu projede
    /// dördüncü kez): <see cref="UiKit.CreateSafeArea"/> çentik payını YALNIZCA
    /// kurulum anında okuyordu. Tek bir cihazda test edildiği sürece kusursuz
    /// çalışır; ama:
    ///   - Device Simulator'da cihaz değiştirince arayüz ESKİ cihazın çentiğine
    ///     göre kalır — üstelik simülatör "test ettim" hissi verdiği için
    ///     yanlış sonuç doğru sanılır,
    ///   - telefon döndüğünde güvenli alan tamamen değişir,
    ///   - editörde Game view'ın boyutu değiştiğinde de öyle.
    ///
    /// DERS (her kare DEĞİL, değişince): Bunu Update içinde her kare hesaplamak
    /// kolaydı ama güvenli alan saatte bir kez bile değişmiyor. Son uygulanan
    /// değeri saklayıp yalnız farklıysa yazmak, aynı doğruluğu bedelsiz veriyor.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class UiSafeArea : MonoBehaviour
    {
        RectTransform _rect;
        Rect _lastArea;
        int _lastWidth, _lastHeight;

        void Awake() => _rect = (RectTransform)transform;

        void OnEnable() => Apply();

        void Update()
        {
            // Ekran ölçüsü de kontrol ediliyor: bazı cihazlarda safeArea aynı
            // kalıp çözünürlük değişebiliyor ve oran bozulur.
            if (Screen.safeArea == _lastArea &&
                Screen.width == _lastWidth &&
                Screen.height == _lastHeight) return;

            Apply();
        }

        void Apply()
        {
            if (_rect == null) _rect = transform as RectTransform;
            if (_rect == null) return;

            var area = Screen.safeArea;
            int width = Screen.width, height = Screen.height;
            if (width <= 0 || height <= 0) return;

            _lastArea = area;
            _lastWidth = width;
            _lastHeight = height;

            _rect.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
            _rect.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
