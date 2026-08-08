using System.Collections;
using BlockOut.Core;
using BlockOut.Runtime.Board;
using UnityEngine;

namespace BlockOut.Runtime.FX
{
    /// <summary>
    /// Yardımcıların görsel karşılığı: roket izi, UFO ışını, dondurma perdesi.
    ///
    /// DERS (yardımcı PAHALI, o yüzden GÖSTERİŞLİ olmalı): Bu üç yardımcı ya
    /// jetonla alınıyor ya reklam izlenerek kazanılıyor. Oyuncu bedelini ödediği
    /// bir şeyin işe yaradığını GÖRMELİ. Blok sessizce kaybolursa "para boşa
    /// gitti" hissi doğar — oyuncu bir daha almaz. Efektin süresi kısa (yarım
    /// saniye) ama varlığı, o bedelin karşılığını verdiğini söyleyen şey.
    ///
    /// DERS (efekt oyunu BEKLETMEZ): Hepsi coroutine ve kendi başına akıyor;
    /// model tarafı hiçbir şey beklemiyor. Efekti oynanışın önüne geçirmek —
    /// "animasyon bitene kadar oynayamazsın" — casual oyunda en hızlı sıkma yolu.
    /// </summary>
    public static class PowerUpFX
    {
        /// <summary>
        /// Roket: hedefin üstüne yukarıdan dalıp çarpar.
        ///
        /// Yönü YUKARIDAN AŞAĞI: yatay gelen bir roket tahtadaki diğer blokların
        /// arkasından geçmek zorunda kalır ve yarısı görünmez.
        /// </summary>
        public static void Rocket(Transform parent, Vector3 target, Color tint)
        {
            var rocket = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rocket.name = "RocketFX";
            rocket.transform.SetParent(parent, worldPositionStays: false);
            Object.Destroy(rocket.GetComponent<Collider>());

            var renderer = rocket.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = View.ViewKit.FrostShard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            rocket.transform.localScale = new Vector3(0.22f, 0.55f, 0.22f);
            GameKit.FX.Juice.Run(Dive(rocket.transform, target));
        }

        static IEnumerator Dive(Transform rocket, Vector3 target)
        {
            Vector3 start = target + Vector3.up * 7f;
            const float duration = 0.26f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (rocket == null) yield break;
                float k = t / duration;
                rocket.position = Vector3.Lerp(start, target, k * k);   // hızlanarak
                rocket.Rotate(0f, 720f * Time.deltaTime, 0f);
                yield return null;
            }

            if (rocket != null) Object.Destroy(rocket.gameObject);
        }

        /// <summary>
        /// UFO: tahtanın üstünde beliren geniş bir ışık sütunu.
        ///
        /// UFO bir RENGİN tamamını siliyor; tek bir bloğa değil tahtanın
        /// geneline ait bir olay. Bu yüzden efekt de tek noktada değil,
        /// silinen her bloğun üstünde aynı anda parlıyor.
        /// </summary>
        public static void Beam(Transform parent, Vector3 target, Color tint)
        {
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "BeamFX";
            beam.transform.SetParent(parent, worldPositionStays: false);
            Object.Destroy(beam.GetComponent<Collider>());

            var renderer = beam.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = View.ViewKit.Translucent(
                new Color(tint.r, tint.g, tint.b, 0.55f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            beam.transform.position = target + Vector3.up * 2.4f;
            GameKit.FX.Juice.Run(Collapse(beam.transform, renderer));
        }

        static IEnumerator Collapse(Transform beam, MeshRenderer renderer)
        {
            const float duration = 0.42f;
            var material = renderer.material;          // örnek: kendi alfası olsun
            Color color = material.color;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                if (beam == null) yield break;
                float k = t / duration;
                // Genişlik daralır, yükseklik korunur: ışın "emiyor" gibi okunur.
                float width = Mathf.Lerp(0.55f, 0.04f, k);
                beam.localScale = new Vector3(width, 2.4f, width);

                color.a = 0.55f * (1f - k);
                material.color = color;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                yield return null;
            }

            if (beam != null) Object.Destroy(beam.gameObject);
        }

        /// <summary>
        /// Çalar saat: ekranın kenarlarına buzlu bir çerçeve. Süre donduğu
        /// sürece durur, bittiğinde söner.
        ///
        /// Kenarlarda duruyor çünkü tahtayı kaplayan bir perde oyunu okumayı
        /// zorlaştırır. "Bir şey değişti" bilgisi çevreden de verilebilir.
        /// </summary>
        public static GameObject FreezeVignette(Transform parent)
        {
            var holder = new GameObject("FreezeVignette");
            holder.transform.SetParent(parent, worldPositionStays: false);

            var canvas = GameKit.UI.UiKit.CreateCanvas("FreezeCanvas");
            canvas.transform.SetParent(holder.transform, worldPositionStays: false);
            canvas.sortingOrder = 4;                   // HUD'ın ALTINDA

            var root = GameKit.UI.UiKit.CreateSafeArea(canvas);
            var frost = new Color(0.62f, 0.87f, 1f, 0.30f);

            // Dört kenar şeridi; ortası tamamen açık kalıyor.
            var top = GameKit.UI.UiKit.CreatePanel("Top", root, frost);
            GameKit.UI.UiKit.Place(top, 0f, 0.90f, 1f, 1f);
            var bottom = GameKit.UI.UiKit.CreatePanel("Bottom", root, frost);
            GameKit.UI.UiKit.Place(bottom, 0f, 0f, 1f, 0.10f);
            var left = GameKit.UI.UiKit.CreatePanel("Left", root, frost);
            GameKit.UI.UiKit.Place(left, 0f, 0.10f, 0.05f, 0.90f);
            var right = GameKit.UI.UiKit.CreatePanel("Right", root, frost);
            GameKit.UI.UiKit.Place(right, 0.95f, 0.10f, 1f, 0.90f);

            foreach (var panel in new[] { top, bottom, left, right })
                panel.raycastTarget = false;

            return holder;
        }
    }
}
