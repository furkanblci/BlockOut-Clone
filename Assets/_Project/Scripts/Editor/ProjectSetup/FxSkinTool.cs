using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using BlockOut.Runtime.FX;

namespace BlockOut.Editor.ProjectSetup
{
    /// <summary>
    /// `Resources/FxSkin.asset`i Epic Toon FX paketinden doldurur.
    ///
    /// Liste ELLE yazılıyor, klasör taranmıyor — `UiSkinTool`'dan farkı bu.
    /// Sebebi: pakette 300'den fazla efekt var ve hepsini kaydetmek hepsini
    /// derlemeye sokardı. Burada yalnız SEÇTİKLERİMİZ duruyor; listeye bir
    /// satır eklemek, o efekti oyuna almanın tek adımı.
    /// </summary>
    public static class FxSkinTool
    {
        const string PackageRoot = "Assets/Epic Toon FX/Prefabs/";
        const string SkinPath = "Assets/_Project/Resources/FxSkin.asset";

        /// <summary>ad → paket içindeki yol (uzantısız).</summary>
        static readonly (string Name, string Path)[] Wanted =
        {
            (Fx.ConfettiBlast,  "Environment/Confetti/Blast/ConfettiBlastRainbow"),
            (Fx.ConfettiShower, "Environment/Confetti/Shower/ConfettiShowerRainbow"),
            (Fx.ConfettiUp,     "Environment/Confetti/Directional/ConfettiDirectionalRainbow"),

            // KÜME OLMAYAN sürümler: kümeli olan önce yukarı DÖRT roket
            // fırlatıp onları ayrı ayrı patlatıyor, yani patlamanın nereye
            // düşeceğini biz belirleyemiyoruz. Referansın patlama noktaları
            // kare kare ölçüldüğü için konumu birebir koyabilmek şart;
            // küme olmayan sürüm doğduğu yerde ANINDA patlıyor.
            ("FireworkYellow", "Environment/Firework/FireworkYellow"),
            ("FireworkBlue",   "Environment/Firework/FireworkBlue"),
            ("FireworkPurple", "Environment/Firework/FireworkPurple"),
            ("FireworkGreen",  "Environment/Firework/FireworkGreen"),
            ("FireworkRed",    "Environment/Firework/FireworkRed"),

            (Fx.GateAbsorb, "Combat/Explosions/SparkleExplosion/SparkleExplosionYellow"),
            (Fx.IceShatter, "Combat/Explosions/FrostExplosion/FrostExplosion"),
            (Fx.CoinBlast,  "Interactive/Money/Coins/GoldCoinBlast"),
            (Fx.PowerHit,   "Combat/Explosions/NovaSmallExplosion/ExplosionNovaSmallFire"),
        };

        [MenuItem("Tools/Block Out/Efekt Kütüphanesini Yenile")]
        public static void EnsureSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<FxSkin>(SkinPath);
            if (skin == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SkinPath));
                skin = ScriptableObject.CreateInstance<FxSkin>();
                AssetDatabase.CreateAsset(skin, SkinPath);
            }

            var entries = new List<FxSkin.Entry>();
            var missing = new List<string>();
            foreach (var (name, path) in Wanted)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    PackageRoot + path + ".prefab");
                if (prefab == null) { missing.Add(path); continue; }

                // YUMUŞAK PARÇACIK BURADA, BİR KEZ KAPATILIYOR.
                //
                // Bizim URP asset'inde derinlik dokusu kapalı; açık kalan bir
                // materyal ekranda hiç görünmüyor (paketin belgesi bunu
                // "invisible particles in URP" diye anlatıyor). Çalışma
                // anında kapatmak da mümkün ama materyal PAYLAŞILDIĞI için
                // ilk kullanımda bir kare geç kalıyordu; kurulumda kapatmak
                // sorunu tamamen kaldırıyor.
                FxCaptureTool.DisableSoftParticles(prefab);

                entries.Add(new FxSkin.Entry { name = name, prefab = prefab });
            }

            skin.SetEntries(entries.ToArray());
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Setup] FxSkin güncellendi: {entries.Count} efekt." +
                      (missing.Count > 0 ? $" EKSİK: {string.Join(", ", missing)}" : ""));
        }
    }
}
