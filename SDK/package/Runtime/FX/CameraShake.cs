using UnityEngine;

namespace GameKit.FX
{
    /// <summary>
    /// Kamera sarsıntısı — kırılma ve çarpma anlarına ağırlık verir.
    ///
    /// DERS (sarsıntı EKLENİR, atanmaz): Birden çok olay aynı karede sarsıntı
    /// isteyebilir (iki buz aynı anda kırılır). Şiddeti atamak, ikinci olayın
    /// birincinin üstüne yazması ve toplam etkinin küçülmesi demektir. Şiddeti
    /// TOPLAMAK ve tavanla sınırlamak, çok olayın birikerek daha güçlü —ama
    /// kontrolden çıkmayan— bir sarsıntı vermesini sağlar.
    ///
    /// DERS (kamerayı DOĞRUDAN oynatma): Sarsıntı, kameranın kendi konumuna
    /// yazarsa; kadraj hesabı yapan kod (tahtayı ekrana sığdıran kısım) o
    /// bozuk konumu okur ve kadraj kayar. Bu yüzden sarsıntı bir OFSET olarak
    /// tutuluyor ve her karede temel konumun üstüne ekleniyor; temel konum
    /// kimseye görünmeden kalıyor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraShake : MonoBehaviour
    {
        const float MaxTrauma = 1f;
        const float Decay = 2.4f;        // saniyede sönme hızı
        const float MaxOffset = 0.42f;   // dünya birimi
        const float Frequency = 26f;

        static CameraShake _instance;

        float _trauma;
        Vector3 _base;
        float _seed;

        public static CameraShake Ensure(Camera camera)
        {
            if (camera == null) return null;
            var shake = camera.GetComponent<CameraShake>();
            if (shake == null) shake = camera.gameObject.AddComponent<CameraShake>();
            _instance = shake;
            return shake;
        }

        /// <summary>Sarsıntı ekler. 0.2 hafif, 0.5 belirgin, 1.0 sert.</summary>
        public static void Add(float amount)
        {
            if (_instance != null) _instance.AddLocal(amount);
        }

        public void AddLocal(float amount) =>
            _trauma = Mathf.Min(MaxTrauma, _trauma + Mathf.Max(0f, amount));

        void Awake()
        {
            _base = transform.localPosition;
            _seed = Random.value * 100f;
        }

        void LateUpdate()
        {
            // Kadraj kodu kamerayı taşımış olabilir; sarsıntı yokken temel
            // konumu ondan öğren.
            if (_trauma <= 0f)
            {
                _base = transform.localPosition;
                return;
            }

            _trauma = Mathf.Max(0f, _trauma - Decay * Time.unscaledDeltaTime);

            // Şiddetin KARESİ kullanılıyor: küçük sarsıntılar iyice hafif kalsın,
            // büyükler belirgin olsun. Doğrusal kullanmak her şeyi orta şiddette
            // ve tekdüze gösteriyor.
            float strength = _trauma * _trauma * MaxOffset;
            float time = Time.unscaledTime * Frequency;

            // Perlin gürültüsü rastgeleden daha iyi: art arda gelen değerler
            // birbirine yakın olduğu için sarsıntı titreme değil SALLANMA olur.
            float x = (Mathf.PerlinNoise(_seed, time) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(_seed + 17f, time) - 0.5f) * 2f;

            transform.localPosition = _base + new Vector3(x, y, 0f) * strength;
        }
    }
}
