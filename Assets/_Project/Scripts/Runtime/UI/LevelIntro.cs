using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UiKit = GameKit.UI.UiKit;

namespace BlockOut.Runtime.UI
{
    /// <summary>
    /// Bölümler arasındaki geçiş: siyah perde, oyun logosu ve konfeti.
    ///
    /// DERS (geçiş oyunun NEFES ALDIĞI yerdir): Bölüm bitip yenisi anında
    /// açıldığında oyuncu iki tahtayı birbirine karıştırıyor — hangi bloğu
    /// nerede bıraktığını hatırlıyor ama tahta değişmiş oluyor. Kısa bir perde
    /// eski tahtayı gözden siliyor ve yeni bölüme temiz bir sayfayla
    /// başlatıyor. Referans oyun tam olarak bunu yapıyor.
    ///
    /// DERS (geçiş KISA olmalı): Bir saniyeyi geçen her geçiş, ellinci kez
    /// görüldüğünde işkenceye döner. Buradaki toplam süre 0.9 saniye ve
    /// dokunulunca hemen kesiliyor — oyuncu beklemek zorunda değil.
    /// </summary>
    public sealed class LevelIntro : MonoBehaviour
    {
        const float FadeIn = 0.18f;
        const float Hold = 0.45f;
        const float FadeOut = 0.28f;

        static LevelIntro _instance;

        Canvas _canvas;
        RectTransform _root;
        Image _curtain;
        Coroutine _running;

        public static LevelIntro Ensure(Transform parent)
        {
            if (_instance != null) return _instance;

            var holder = new GameObject("LevelIntro");
            holder.transform.SetParent(parent, worldPositionStays: false);
            _instance = holder.AddComponent<LevelIntro>();
            _instance.Build();
            return _instance;
        }

        void Build()
        {
            _canvas = UiKit.CreateCanvas("IntroCanvas");
            _canvas.transform.SetParent(transform, worldPositionStays: false);
            _canvas.sortingOrder = 200;              // HUD'ın da üstünde

            _root = UiKit.CreateRect("Root", _canvas.transform);
            UiKit.Place(_root, 0f, 0f, 1f, 1f);

            _curtain = UiKit.CreatePanel("Curtain", _root, new Color(0.02f, 0.01f, 0.06f, 1f));
            _curtain.raycastTarget = true;

            // LOGO KALDIRILDI — ÇİFT KAZANMA EKRANI HATASI (4. tur, J40).
            //
            // Kullanıcı: "2 tane üst üste 'BLOCKOUT kazandın' ekranı çıkıyor,
            // biri eski versiyon."
            //
            // TEŞHİS: Kazanma dizilimi şuydu — `WinCelebration` (GERÇEK logo
            // görseli + fişek) → PERFECT kartı → Continue → `LevelIntro`
            // (YAZIYLA kurulmuş "BLOCK"/"OUT!" + konfeti) → yeni bölüm. Yani
            // oyuncu aynı kutlamayı iki kez, ikincisini de eski/çirkin
            // biçimiyle görüyordu.
            //
            // Bu sınıf `WinCelebration`'dan ÖNCE yazılmıştı ve o zaman tek
            // kutlama oydu; yenisi eklenirken eskisi kaldırılmadı.
            //
            // DERS (yeni bir şey eklerken ESKİSİNİ aramak, işin yarısıdır):
            // İki ekran ayrı dosyalarda, ayrı akışlarda yaşıyordu ve ikisi de
            // tek başına doğru çalışıyordu. Hata ancak ikisi ARDIŞIK
            // oynandığında görülüyor — yani hiçbir birim testinin
            // yakalayamayacağı, yalnız oynayarak bulunabilecek bir hata.
            //
            // Geriye kalan iş bu sınıfın ASIL işi: tahtayı perde arkasında
            // değiştirmek. Logo ve fişek oraya sonradan eklenmiş süstü.
            _root.gameObject.SetActive(false);
        }

        /// <summary>Perdeyi oynatır; ortasında <paramref name="swap"/> çağrılır.</summary>
        public void Play(System.Action swap)
        {
            if (_running != null) GameKit.FX.Juice.Stop(_running);
            _running = GameKit.FX.Juice.Run(Routine(swap));
        }

        IEnumerator Routine(System.Action swap)
        {
            _root.gameObject.SetActive(true);

            yield return GameKit.FX.Juice.Tween(FadeIn, t => SetAlpha(t));

            // Tahta perdenin ARKASINDA değişir: oyuncu iki tahtayı birlikte
            // görmediği için karışıklık olmuyor. Perdenin TEK işi bu; logo,
            // fişek ve ses buradan kaldırıldı (bkz. Build, J40).
            swap?.Invoke();

            yield return new WaitForSecondsRealtime(Hold);
            yield return GameKit.FX.Juice.Tween(FadeOut, t => SetAlpha(1f - t));

            _root.gameObject.SetActive(false);
            _running = null;
        }

        void SetAlpha(float alpha)
        {
            var color = _curtain.color;
            color.a = alpha;
            _curtain.color = color;
        }

    }
}
