#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace GameKit.Editor.Setup
{
    /// <summary>
    /// Üretilen Gradle projesinin manifestine `VIBRATE` iznini ekler.
    ///
    /// DERS (izin istemeyen çağrı sessizce hiçbir şey yapmaz): Oyunun bütün
    /// titreşimi `GameKit.Services.Haptics` üzerinden `android.os.Vibrator`
    /// servisini DOĞRUDAN çağırıyor. Bu doğru karar — `Handheld.Vibrate()`
    /// Android'de süresi ayarlanamayan yarım saniyelik bir buzz üretir —
    /// ama bir yan etkisi var: **Unity artık VIBRATE iznini kendiliğinden
    /// eklemiyor.**
    ///
    /// Unity o izni yalnızca derlemeye giren kodda `Handheld.Vibrate`
    /// referansı GÖRÜNCE ekler; bizim çağrımız JNI üzerinden dize adlarıyla
    /// yapıldığı için statik tarama onu göremez. İzin olmayınca
    /// `vibrator.vibrate(...)` bir `SecurityException` atar, bizim try/catch
    /// onu yutar ve cihazda **tek bir titreşim bile çalışmaz** — hata da
    /// görünmez (4. tur A4 bulgusu: "hiçbir haptik çalışmıyor").
    ///
    /// DERS (izni manifeste elle yazmak yerine derlemeye enjekte etmek):
    /// `Assets/Plugins/Android/AndroidManifest.xml` koymak da mümkün ama o
    /// dosya ANA manifesti KOMPLE devralır: activity, tema, intent-filter,
    /// Unity'nin meta-data'ları... Unity sürümü değişince o kopya bayatlar ve
    /// uygulama açılmaz. Tek bir satır için bütün manifesti sahiplenmek kötü
    /// bir takas. Bu kanca üretilen manifesti okuyup yalnız eksik izni
    /// ekliyor; geri kalan her şey Unity'nin ürettiği hâliyle kalıyor.
    /// </summary>
    public sealed class AndroidManifestPatcher : IPostGenerateGradleAndroidProject
    {
        /// <summary>Unity'nin kendi kancalarından sonra koşsun.</summary>
        public int callbackOrder => 100;

        const string AndroidNs = "http://schemas.android.com/apk/res/android";

        /// <summary>Manifeste eklenecek izinler.</summary>
        static readonly string[] Permissions =
        {
            "android.permission.VIBRATE"
        };

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("[Android] Manifest bulunamadı, izin eklenemedi: " + manifestPath);
                return;
            }

            var doc = new XmlDocument();
            doc.Load(manifestPath);

            var manifest = doc.DocumentElement;
            if (manifest == null) return;

            int added = 0;
            foreach (string permission in Permissions)
            {
                if (HasPermission(manifest, permission)) continue;

                var node = doc.CreateElement("uses-permission");
                var attribute = doc.CreateAttribute("android", "name", AndroidNs);
                attribute.Value = permission;
                node.Attributes.Append(attribute);

                // İzinler `<application>` ETİKETİNDEN ÖNCE gelmeli; Android
                // şemasında sıra serbest değil, aksi hâlde AAPT uyarı basar.
                var application = manifest.SelectSingleNode("application");
                if (application != null) manifest.InsertBefore(node, application);
                else manifest.AppendChild(node);
                added++;
            }

            if (added == 0) return;

            doc.Save(manifestPath);
            Debug.Log($"[Android] Manifeste {added} izin eklendi (VIBRATE dahil): {manifestPath}");
        }

        static bool HasPermission(XmlElement manifest, string permission)
        {
            foreach (XmlNode node in manifest.SelectNodes("uses-permission"))
            {
                var name = node.Attributes?["android:name"];
                if (name != null && name.Value == permission) return true;
            }
            return false;
        }
    }
}
#endif
