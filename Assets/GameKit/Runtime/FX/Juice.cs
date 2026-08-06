using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.FX
{
    /// <summary>
    /// Küçük hareket kütüphanesi: ölçek vuruşu, sallanma, yumuşak geçiş.
    ///
    /// DERS (neden DOTween değil): Bir tween paketi bu proje için doğru olurdu
    /// eğer yüzlerce farklı hareket olsaydı. Burada beş çeşit hareket var ve
    /// hepsi tek satırlık eğrilerle yazılabiliyor. Bir bağımlılık eklemenin
    /// bedeli yalnız indirme değil: sürüm çakışması, lisans, derleme süresi ve
    /// "bu paket ne yapıyor" sorusunun her yeni kişiye anlatılması. Elli satırla
    /// çözülen bir şey için bunu ödemek pahalı.
    ///
    /// DERS (yay/geri sekme eğrisi): Doğrusal bir ölçek büyümesi "mekanik"
    /// hissettirir. Hedefi AŞIP geri dönen bir eğri (overshoot) canlı hissettirir
    /// — casual oyunların dokunma geri bildiriminin tamamı bu numaraya dayanır.
    /// </summary>
    public static class Juice
    {
        // ---------------------------------------------------------------- eğriler

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
        /// Neden gerekli: hareketi başlatan nesne (yok edilen bir blok, kapanan
        /// bir panel) coroutine bitmeden ölebilir; MonoBehaviour ölünce
        /// coroutine sessizce durur ve hareket yarıda kalır. Ayrı bir taşıyıcı
        /// bunu ortadan kaldırır.
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

        /// <summary>Bir hareketi başlatır. Dönen tanıtıcıyla iptal edilebilir.</summary>
        public static Coroutine Run(IEnumerator routine) => Runner.Instance.StartCoroutine(routine);

        public static void Stop(Coroutine handle)
        {
            if (handle != null) Runner.Instance.StopCoroutine(handle);
        }

        // ---------------------------------------------------------------- hareketler

        /// <summary>
        /// Belirtilen süre boyunca 0→1 ilerler ve her karede geri çağırır.
        /// Ölçeklenmemiş zaman kullanır: oyun duraklatıldığında bile arayüz
        /// hareketleri akmaya devam etsin (duraklat menüsü donmuş görünmesin).
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
        public static IEnumerator PunchScale(Transform target, float amount = 0.18f,
            float duration = 0.28f)
        {
            if (target == null) yield break;
            Vector3 baseScale = target.localScale;

            yield return Tween(duration, t =>
            {
                if (target == null) return;
                // sin(pi*t) tek tepe verir: 0'dan çıkar, ortada zirve, 0'a döner.
                float pulse = Mathf.Sin(t * Mathf.PI) * amount;
                target.localScale = baseScale * (1f + pulse);
            });

            if (target != null) target.localScale = baseScale;
        }

        /// <summary>Sıfırdan hedefe yaylanarak açılır — panel/kart girişi.</summary>
        public static IEnumerator PopIn(Transform target, float duration = 0.32f,
            float delay = 0f)
        {
            if (target == null) yield break;
            Vector3 baseScale = target.localScale;
            target.localScale = Vector3.zero;

            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            yield return Tween(duration, t =>
            {
                if (target != null) target.localScale = baseScale * t;
            }, t => EaseOutBack(t));

            if (target != null) target.localScale = baseScale;
        }

        /// <summary>Sallanma: kırılma/çarpma anlarında ağırlık hissi verir.</summary>
        public static IEnumerator Shake(Transform target, float strength = 0.18f,
            float duration = 0.25f)
        {
            if (target == null) yield break;
            Vector3 origin = target.localPosition;

            yield return Tween(duration, t =>
            {
                if (target == null) return;
                // Şiddet zamanla söner; yoksa sarsıntı aniden kesiliyormuş gibi durur.
                float fade = 1f - t;
                target.localPosition = origin + (Vector3)UnityEngine.Random.insideUnitCircle
                    * (strength * fade);
            });

            if (target != null) target.localPosition = origin;
        }

        /// <summary>Bir noktadan diğerine yay çizerek uçar — "jeton sayaca gitti".</summary>
        public static IEnumerator FlyTo(Transform target, Vector3 from, Vector3 to,
            float arc = 90f, float duration = 0.55f, Action onArrive = null)
        {
            if (target == null) yield break;

            yield return Tween(duration, t =>
            {
                if (target == null) return;
                var point = Vector3.LerpUnclamped(from, to, EaseInCubic(t));
                // Yay: yolun ortasında en yüksek, iki uçta sıfır.
                point.y += Mathf.Sin(t * Mathf.PI) * arc;
                target.position = point;
            });

            onArrive?.Invoke();
        }

        // ---------------------------------------------------------------- yardımcı

        static readonly Dictionary<int, Coroutine> _byKey = new Dictionary<int, Coroutine>();

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
    }
}
