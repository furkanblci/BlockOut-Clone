using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameKit.Editor.Setup
{
    /// <summary>
    /// Kendi kendini kuran proje (self-bootstrapping project).
    ///
    /// DERS: [InitializeOnLoadMethod], Unity her "domain reload"da (proje
    /// açılışı, kod derlemesi, Play'den dönüş) bu metodu çağırır. Stüdyolarda
    /// bu desen "yeni katılan kişi repoyu çeker, Unity'yi açar, HER ŞEY hazır"
    /// deneyimi için kullanılır — README'de elle kurulum adımı bırakılmaz.
    ///
    /// Kurulum adımları idempotent olduğu için her reload'da çalışması
    /// zararsızdır: eksik yoksa hiçbir şey yapmaz.
    ///
    /// DERS (adımlar KİTE yazılamaz, sıra yazılabilir): Hangi asset'in
    /// kurulacağı oyuna göre değişir (bölüm kataloğu, blok materyalleri,
    /// oynanış sahnesi). Değişmeyen şey ÇERÇEVE: reload'da tetikle, ilk boş
    /// kareye ertele, Play'e girerken dokunma, yapılanı tek satırda raporla.
    /// Kit çerçeveyi verir; adımları oyun <see cref="Steps"/> ile ekler.
    /// </summary>
    public static class ProjectBootstrap
    {
        /// <summary>
        /// Bir kurulum adımı: eksiği tamamlar, bir şey YAPTIYSA true döner.
        /// </summary>
        public readonly struct Step
        {
            /// <summary>Raporda görünecek ad, ör. "blok materyalleri".</summary>
            public readonly string Label;

            /// <summary>Eksiği tamamlar; değişiklik yaptıysa true.</summary>
            public readonly Func<bool> Run;

            public Step(string label, Func<bool> run)
            {
                Label = label;
                Run = run;
            }
        }

        static readonly List<Step> _steps = new List<Step>();

        /// <summary>
        /// Oyunun kendi kurulum adımları. Bir düzenleyici sınıfında
        /// <c>[InitializeOnLoadMethod]</c> içinden eklenir:
        ///
        /// <code>
        /// ProjectBootstrap.Add("bölüm kataloğu", MySetup.EnsureLevelCatalog);
        /// </code>
        /// </summary>
        public static void Add(string label, Func<bool> run)
        {
            if (run == null) return;
            _steps.Add(new Step(label, run));
        }

        /// <summary>Kitin kendi adımları — oyununkilerden ÖNCE koşar.</summary>
        static IEnumerable<Step> KitSteps()
        {
            yield return new Step("UI sprite ayarları", UiSpriteImporter.EnsureImported);
            yield return new Step("TMP fontu", FontSetupTool.EnsureFontAsset);
        }

        [InitializeOnLoadMethod]
        static void OnDomainReload()
        {
            // Asset oluşturmak import/serileştirme ortasında güvenli değildir;
            // delayCall işi ilk boş editör karesine erteler.
            EditorApplication.delayCall += RunIfNeeded;
        }

        static void RunIfNeeded()
        {
            // Play moduna girerken dokunma — oyun başlatmayı geciktirmeyelim.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var done = new List<string>();

            foreach (var step in KitSteps()) RunStep(step, done);
            for (int i = 0; i < _steps.Count; i++) RunStep(_steps[i], done);

            if (done.Count > 0)
                Debug.Log("[Bootstrap] Eksik kurulum otomatik tamamlandı: " +
                          string.Join(", ", done) +
                          " — hiçbir menüye tıklamana gerek yok.");
        }

        /// <summary>
        /// Tek adımı koşar. Bir adımın patlaması ÖTEKİLERİ DURDURMAZ.
        ///
        /// DERS (kurulum zinciri en zayıf halkasında kopmamalı): Adımlar
        /// eskiden art arda çağrılıyordu; ortadaki biri istisna atınca
        /// sonrakiler hiç koşmuyor ve konsoldaki tek hata, kurulumun YARIM
        /// kaldığını değil yalnız o adımın bozuk olduğunu söylüyordu.
        /// </summary>
        static void RunStep(Step step, List<string> done)
        {
            try
            {
                if (step.Run()) done.Add(step.Label);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Bootstrap] '{step.Label}' adımı başarısız: {e.Message}");
            }
        }
    }
}
