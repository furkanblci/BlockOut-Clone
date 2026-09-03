using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PT = PrimeTween;

namespace GameKit.FX
{
    /// <summary>
    /// Küçük hareket kütüphanesi: ölçek vuruşu, sallanma, yumuşak geçiş.
    ///
    /// DERS (neden artık PrimeTween): Bu dosya başlangıçta elle yazılmış
    /// coroutine'lerle çalışıyordu ve gerekçesi şuydu — "beş çeşit hareket var,
    /// bağımlılık eklemeye değmez." Proje büyüdü: sekiz dosyada otuz dört
    /// çağrı, on bir dosyada otuz bir coroutine oldu. İki şey değişti:
    ///   1. Her coroutine bir nesne ayırır. Blok animasyonlarının yoğun olduğu
    ///      anlarda bu, mobilde çöp toplayıcı tırtıklanmasına dönüşür.
    ///      PrimeTween struct tabanlıdır, çalışırken ayırma yapmaz.
    ///   2. Zincirli zamanlama (önce kart, sonra yıldızlar, sonra ödül) elle
    ///      iç içe coroutine yazmayı gerektiriyordu. Sequence bunu tek ifadeye
    ///      indiriyor.
    /// Kütüphane doğrudan çağrı yerlerine serpilmedi; bu sınıf cephe olarak
    /// kaldı. Böylece bir gün kütüphane değişirse tek dosya değişir.
    ///
    /// DERS (yay/geri sekme eğrisi): Doğrusal bir ölçek büyümesi "mekanik"
    /// hissettirir. Hedefi AŞIP geri dönen bir eğri (overshoot) canlı hissettirir
    /// — casual oyunların dokunma geri bildiriminin tamamı bu numaraya dayanır.
    ///
    /// DERS (ölçeklenmemiş zaman): Buradaki her hareket useUnscaledTime ile
    /// çalışır. Oyun duraklatıldığında Time.timeScale sıfırlanır; ölçekli zamana
    /// bağlı bir tween duraklat menüsünü donmuş gösterir.
    /// </summary>
    public static class Juice
    {
        /// <summary>Panel/kart girişlerinde kullanılan geri sekme şiddeti.</summary>
        const float PopOvershoot = 1.35f;

        // ---------------------------------------------------------------- kurulum

        /// <summary>
        /// Havuz boyutunu önden ayırır.
        ///
        /// Neden gerekli: PrimeTween tween'leri bir dizide tutar ve dizi dolunca
        /// büyütür — büyütme anı tek seferlik bir ayırmadır. Oyunun en hareketli
        /// anında olmasın diye baştan yeterince yer açıyoruz.
        ///
        /// DERS (BeforeSceneLoad'da DEĞİL): İlk hali BeforeSceneLoad idi ve
        /// "PrimeTweenManager is not created yet" istisnası attı. PrimeTween
        /// yöneticisini kendisi de BeforeSceneLoad'da kuruyor; aynı aşamadaki iki
        /// geri çağırmanın sırası tanımsızdır. AfterSceneLoad yöneticinin var
        /// olduğunu garanti eder ve hâlâ ilk tween'den önce çalışır.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Configure()
        {
            PT.PrimeTweenConfig.SetTweensCapacity(256);
            // Hedefi yok edilmiş tween uyarısı bizde gürültü: panel kapanırken
            // üstündeki hareket hâlâ canlı olabiliyor, bu beklenen bir durum.
            PT.PrimeTweenConfig.warnIfTargetDestroyed = false;
        }

        // ---------------------------------------------------------------- eğriler

        // Bu dört fonksiyon hâlâ elle yazılmış coroutine'ler tarafından doğrudan
        // kullanılıyor (LevelIntro, TutorialOverlay). PrimeTween'in kendi eğrileri
        // tween'lerin içindedir; dışarıdan bir t değeri değerlendirmek için bunlar
        // gerekli.

        /// <summary>Hedefi aşıp geri dönen yumuşama. c1/c3 klasik "back" katsayıları.</summary>
        public static float EaseOutBack(float t, float overshoot = 1.70158f)
        {
            float c3 = overshoot + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + overshoot * p * p;
        }

        public static float EaseOutCubic(float t)
        {
            float p = 1f - t;
            return 1f - p * p * p;
        }

        public static float EaseInCubic(float t) => t * t * t;

        /// <summary>Sönümlenen yay: kısa, hızlı, birkaç kez sekerek duruyor.</summary>
        public static float Spring(float t, float frequency = 3.2f, float decay = 6f)
        {
            if (t >= 1f) return 1f;
            return 1f - Mathf.Exp(-decay * t) * Mathf.Cos(frequency * Mathf.PI * t);
        }

        // ---------------------------------------------------------------- koşucu

        /// <summary>
        /// Coroutine'leri çalıştıracak kalıcı taşıyıcı.
        ///
        /// Neden hâlâ gerekli: PrimeTween özellik animasyonlarını devraldı ama
        /// projede tween'e indirgenemeyen çok aşamalı yordamlar var (kıvılcım
        /// üretimi, roket dalışı, konfeti fiziği). Onlar coroutine olarak kalıyor.
        /// Hareketi başlatan nesne (yok edilen bir blok, kapanan bir panel)
        /// coroutine bitmeden ölebilir; MonoBehaviour ölünce coroutine sessizce
        /// durur ve hareket yarıda kalır. Ayrı bir taşıyıcı bunu ortadan kaldırır.
        /// </summary>
        sealed class Runner : MonoBehaviour
        {
            static Runner _instance;

            public static Runner Instance
            {
                get
                {
                    if (_instance != null) return _instance;
                    var go = new GameObject("JuiceRunner") { hideFlags = HideFlags.HideInHierarchy };
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Runner>();
                    return _instance;
                }
            }
        }

        /// <summary>Bir yordamı başlatır. Dönen tanıtıcıyla iptal edilebilir.</summary>
        public static Coroutine Run(IEnumerator routine) => Runner.Instance.StartCoroutine(routine);

        /// <summary>
        /// Hazır bir tween'i olduğu gibi geçirir.
        ///
        /// Neden var: çağrı yerleri `Juice.Run(Juice.PopIn(...))` biçiminde
        /// yazılmıştı. PrimeTween tween'leri oluşturuldukları anda zaten çalışmaya
        /// başlar, ayrıca başlatılmaları gerekmez. Bu geçirgen aşırı yükleme
        /// sayesinde otuz dört çağrı yerini değiştirmek gerekmedi.
        /// </summary>
        public static PT.Tween Run(PT.Tween tween) => tween;

        public static PT.Sequence Run(PT.Sequence sequence) => sequence;

        public static void Stop(Coroutine handle)
        {
            if (handle != null) Runner.Instance.StopCoroutine(handle);
        }

        public static void Stop(PT.Tween tween)
        {
            if (tween.isAlive) tween.Stop();
        }

        public static void Stop(PT.Sequence sequence)
        {
            if (sequence.isAlive) sequence.Stop();
        }

        /// <summary>Bir nesne üzerindeki tüm hareketleri keser.</summary>
        public static void StopAll(UnityEngine.Object target)
        {
            if (target != null) PT.Tween.StopAll(target);
        }

        // ---------------------------------------------------------------- hareketler

        /// <summary>
        /// Belirtilen süre boyunca 0→1 ilerler ve her karede geri çağırır.
        ///
        /// Elle yazılmış yordamların içinden `yield return` ile beklenebilsin diye
        /// coroutine olarak kaldı. Yeni kod yazarken bunun yerine PrimeTween'in
        /// kendi tween'lerini tercih et.
        /// </summary>
        public static IEnumerator Tween(float duration, Action<float> step,
            Func<float, float> ease = null, Action onDone = null)
        {
            if (duration <= 0f)
            {
                step?.Invoke(1f);
                onDone?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                step?.Invoke(ease != null ? ease(t) : t);
                yield return null;
            }

            step?.Invoke(1f);
            onDone?.Invoke();
        }

        /// <summary>Ölçek vuruşu: hedef boyutun üstüne çıkıp geri oturur.</summary>
        public static PT.Tween PunchScale(Transform target, float amount = 0.18f,
            float duration = 0.28f)
        {
            if (target == null) return default;

            // Şiddet mutlak birimdir; taban ölçekle çarparak "yüzde" davranışını
            // koruyoruz — yoksa 0.5 ölçekli bir rozet çok fazla zıplar.
            //
            // DERS (dizideki tween'ler ANINDA kurulur, sırayla DEĞİL): Taban
            // olarak target.localScale okunuyordu. CardEntrance içinde önce
            // PopIn geliyor ve ölçeği sıfırlıyor; ama Sequence'in bütün
            // tween'leri kurulum anında oluşuyor, yani buraya sıfır ölçek
            // geliyordu ve PrimeTween "Shake's strength is (0,0,0)" diye
            // hata veriyordu. Hatırlanan taban ölçek bu tuzağı kapatıyor.
            Vector3 strength = ResolveBaseScale(target) * amount;
            if (strength.sqrMagnitude < 1e-6f) return default;

            // Düşük frekans = tek güçlü darbe. Yüksek frekans titremeye benzer.
            return PT.Tween.PunchScale(target, strength, duration,
                frequency: 3.2f, useUnscaledTime: true);
        }

        /// <summary>Sıfırdan hedefe yaylanarak açılır — panel/kart girişi.</summary>
        public static PT.Tween PopIn(Transform target, float duration = 0.32f,
            float delay = 0f)
        {
            if (target == null) return default;

            Vector3 baseScale = ResolveBaseScale(target);
            target.localScale = Vector3.zero;

            return PT.Tween.Scale(target, Vector3.zero, baseScale, duration,
                PT.Easing.Overshoot(PopOvershoot),
                startDelay: delay, useUnscaledTime: true);
        }

        /// <summary>
        /// Bir arayüz öğesini yatayda kaydırır — sekmeler arası ekran geçişi.
        ///
        /// DERS (geçiş YÖN taşır): Ekranı anında değiştirmek "başka bir yere
        /// ışınlandım" der; yandan kaydırmak "yan sekmeye geçtim" der. Yön
        /// sekme sırasından geldiği için oyuncu nerede olduğunu takip
        /// edebiliyor. Referans oyunda ölçülen süre ~170 ms.
        ///
        /// `useUnscaledTime` şart: menü açıkken oyun zamanı durmuş olabilir.
        /// </summary>
        public static PT.Tween SlideX(RectTransform target, float fromX, float toX,
            float duration = 0.17f, Action onDone = null)
        {
            if (target == null) return default;

            var position = target.anchoredPosition;
            target.anchoredPosition = new Vector2(fromX, position.y);

            var tween = PT.Tween.UIAnchoredPositionX(target, fromX, toX, duration,
                PT.Ease.OutQuad, useUnscaledTime: true);

            return onDone != null ? tween.OnComplete(onDone) : tween;
        }

        /// <summary>
        /// <see cref="SlideX"/>'in dikey kardeşi.
        ///
        /// Eklenme gerekçesi (7. tur, S61): duraklatılan oyunda süre
        /// göstergesi ekranın üstünden çıkıp geri iniyor. Yatay kaydırma
        /// sekme geçişleri için vardı; dikeyi yoktu ve her çağrı yeri kendi
        /// tween'ini yazmak zorundaydı.
        /// </summary>
        public static PT.Tween SlideY(RectTransform target, float fromY, float toY,
            float duration = 0.17f, PT.Ease ease = PT.Ease.OutQuad, Action onDone = null)
        {
            if (target == null) return default;

            var position = target.anchoredPosition;
            target.anchoredPosition = new Vector2(position.x, fromY);

            var tween = PT.Tween.UIAnchoredPositionY(target, fromY, toY, duration,
                ease, useUnscaledTime: true);

            return onDone != null ? tween.OnComplete(onDone) : tween;
        }

        /// <summary>Hedefi sıfıra büzerek kapatır — panel çıkışı.</summary>
        public static PT.Tween PopOut(Transform target, float duration = 0.20f,
            Action onDone = null)
        {
            if (target == null) return default;

            var tween = PT.Tween.Scale(target, target.localScale, Vector3.zero, duration,
                PT.Ease.InBack, useUnscaledTime: true);

            return onDone != null ? tween.OnComplete(onDone) : tween;
        }

        /// <summary>Sallanma: kırılma/çarpma anlarında ağırlık hissi verir.</summary>
        public static PT.Tween Shake(Transform target, float strength = 0.18f,
            float duration = 0.25f)
        {
            if (target == null) return default;

            // Z ekseninde sallamıyoruz: arayüz düzlemsel, derinlikte sarsıntı
            // perspektifsiz kamerada görünmez ama sıralamayı bozabilir.
            return PT.Tween.ShakeLocalPosition(target,
                new Vector3(strength, strength, 0f), duration,
                frequency: 14f, useUnscaledTime: true);
        }

        /// <summary>Bir noktadan diğerine yay çizerek uçar — "jeton sayaca gitti".</summary>
        public static PT.Tween FlyTo(Transform target, Vector3 from, Vector3 to,
            float arc = 90f, float duration = 0.55f, Action onArrive = null)
        {
            if (target == null) return default;

            var tween = PT.Tween.Custom(0f, 1f, duration, t =>
            {
                if (target == null) return;
                var point = Vector3.LerpUnclamped(from, to, EaseInCubic(t));
                // Yay: yolun ortasında en yüksek, iki uçta sıfır.
                point.y += Mathf.Sin(t * Mathf.PI) * arc;
                target.position = point;
            }, PT.Ease.Linear, useUnscaledTime: true);

            return onArrive != null ? tween.OnComplete(onArrive) : tween;
        }

        // ---------------------------------------------------------------- yeni hareketler

        /// <summary>
        /// Sonsuz nefes alma: ana ekrandaki durağan öğeleri canlı tutar.
        ///
        /// Yoyo döngüsü kullanıyor — ileri gidip geri dönüyor, böylece başa
        /// zıplama olmuyor. Sonsuz döngü için cycles = -1.
        /// </summary>
        public static PT.Tween Breathe(Transform target, float amount = 0.04f,
            float period = 1.6f)
        {
            if (target == null) return default;

            Vector3 baseScale = ResolveBaseScale(target);

            return PT.Tween.Scale(target, baseScale, baseScale * (1f + amount),
                period * 0.5f, PT.Ease.InOutSine,
                cycles: -1, cycleMode: PT.CycleMode.Yoyo, useUnscaledTime: true);
        }

        /// <summary>Saydamlık geçişi. Kart/perde açılıp kapanmaları için.</summary>
        public static PT.Tween Fade(CanvasGroup group, float to, float duration = 0.22f,
            float delay = 0f)
        {
            if (group == null) return default;

            return PT.Tween.Alpha(group, group.alpha, to, duration,
                PT.Ease.OutQuad, startDelay: delay, useUnscaledTime: true);
        }

        /// <summary>
        /// Kart girişi: yaylanarak açılır, sonra oturma vuruşu yer.
        ///
        /// Tek başına PopIn "belirdi" der; arkasına gelen küçük vuruş "yere kondu"
        /// der. Sonuç panellerinin ağırlık kazanması bu ikinci darbeye bağlı.
        /// </summary>
        public static PT.Sequence CardEntrance(Transform target, float delay = 0f)
        {
            if (target == null) return default;

            var sequence = PT.Sequence.Create(useUnscaledTime: true);
            // ChainDelay(0) sıfır süreli tween uyarısı üretir; sadece gerçekten
            // gecikme istendiğinde ekliyoruz.
            if (delay > 0f) sequence.ChainDelay(delay);

            return sequence
                .Chain(PopIn(target, 0.34f))
                .Chain(PunchScale(target, 0.06f, 0.18f));
        }

        /// <summary>
        /// Bir grup öğeyi sırayla açar — yıldızlar, ödül satırları, liste kartları.
        ///
        /// Neden sırayla: hepsi aynı anda açılırsa göz nereye bakacağını bilemez.
        /// Aradaki küçük gecikme bakışı soldan sağa sürükler.
        /// </summary>
        public static PT.Sequence PopInStaggered(float gap, params Transform[] targets)
        {
            var sequence = PT.Sequence.Create(useUnscaledTime: true);
            if (targets == null) return sequence;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                sequence.Insert(i * gap, PopIn(targets[i], 0.30f));
            }

            return sequence;
        }

        // ---------------------------------------------------------------- yardımcı

        static readonly Dictionary<int, Coroutine> _byKey = new Dictionary<int, Coroutine>();
        static readonly Dictionary<int, PT.Tween> _tweenByKey = new Dictionary<int, PT.Tween>();
        static readonly Dictionary<int, PT.Sequence> _sequenceByKey = new Dictionary<int, PT.Sequence>();
        static readonly Dictionary<int, Vector3> _baseScaleByKey = new Dictionary<int, Vector3>();

        /// <summary>
        /// Bir nesnenin "gerçek" ölçeğini hatırlar.
        ///
        /// Neden gerekli: PopIn önce ölçeği sıfırlıyor. Panel kapanıp hemen
        /// yeniden açılırsa ikinci çağrı sıfırı taban ölçek sanır ve öğe bir daha
        /// görünmez. İlk görülen sıfırdan farklı ölçeği saklayıp sonra onu
        /// kullanıyoruz.
        /// </summary>
        static Vector3 ResolveBaseScale(Transform target)
        {
            int key = target.GetInstanceID();
            Vector3 current = target.localScale;

            if (current.sqrMagnitude > 0.0001f)
            {
                _baseScaleByKey[key] = current;
                return current;
            }

            return _baseScaleByKey.TryGetValue(key, out var remembered)
                ? remembered
                : Vector3.one;
        }

        /// <summary>
        /// Aynı nesne için önceki hareketi iptal edip yenisini başlatır.
        ///
        /// Neden gerekli: oyuncu bir düğmeye hızlıca üç kez basarsa üç vuruş
        /// aynı anda ölçeği yazar ve nesne titrer ya da yanlış boyutta kalır.
        /// </summary>
        public static void Replace(UnityEngine.Object owner, IEnumerator routine)
        {
            if (owner == null) return;
            int key = owner.GetInstanceID();

            if (_byKey.TryGetValue(key, out var running) && running != null)
                Stop(running);

            _byKey[key] = Run(routine);
        }

        public static void Replace(UnityEngine.Object owner, PT.Tween tween)
        {
            if (owner == null) return;
            int key = owner.GetInstanceID();

            if (_tweenByKey.TryGetValue(key, out var running) && running.isAlive)
                running.Stop();

            _tweenByKey[key] = tween;
        }

        public static void Replace(UnityEngine.Object owner, PT.Sequence sequence)
        {
            if (owner == null) return;
            int key = owner.GetInstanceID();

            if (_sequenceByKey.TryGetValue(key, out var running) && running.isAlive)
                running.Stop();

            _sequenceByKey[key] = sequence;
        }
    }
}
