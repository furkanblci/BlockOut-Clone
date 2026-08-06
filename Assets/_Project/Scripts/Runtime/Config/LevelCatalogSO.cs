using UnityEngine;

namespace BlockOut.Runtime.Config
{
    /// <summary>
    /// Oynanış sırası: hangi bölüm kaçıncı.
    ///
    /// DERS (neden GameSession'daki diziden ayrıldı?): Bölüm sırası artık iki
    /// yerden soruluyor — Gameplay "sıradaki hangisi", Home ekranı "kaç bölüm
    /// var, hangileri açık". Liste sahnedeki bir bileşenin içinde kalsaydı Home
    /// sahnesinden erişilemezdi. Veriyi asset'e taşımak, onu sahnelerden
    /// bağımsız kılıyor.
    ///
    /// İçerik ProjectSetupTool tarafından Levels klasörü taranarak doldurulur;
    /// yeni bölüm eklemek için burayı elle düzenlemek gerekmez.
    /// </summary>
    [CreateAssetMenu(menuName = "Block Out/Level Catalog", fileName = "LevelCatalog")]
    public sealed class LevelCatalogSO : ScriptableObject
    {
        [Tooltip("Oynanış sırası. ProjectSetupTool Levels klasöründen doldurur.")]
        public TextAsset[] levels = new TextAsset[0];

        public int Count => levels != null ? levels.Length : 0;

        public TextAsset AssetAt(int index) =>
            Count == 0 ? null : levels[Mathf.Clamp(index, 0, Count - 1)];

        /// <summary>Kayıt anahtarı olarak kullanılan bölüm adı (level_003 gibi).</summary>
        public string IdAt(int index)
        {
            var asset = AssetAt(index);
            return asset != null ? asset.name : "";
        }
    }

    /// <summary>
    /// Katalogu her sahneden erişilebilir kılan tembel erişimci.
    ///
    /// DERS (Resources klasörü): Genelde kaçınılır — içindeki HER ŞEY derlemeye
    /// girer ve açılışta bellekte tutulur. Ama tek, küçük ve "her sahnede lazım"
    /// bir yapılandırma için doğru araçtır: Addressables kurmak bu iş için fazla
    /// makine olurdu.
    /// </summary>
    public static class LevelCatalog
    {
        public const string ResourcePath = "LevelCatalog";

        static LevelCatalogSO _asset;

        public static LevelCatalogSO Asset =>
            _asset != null ? _asset : (_asset = Resources.Load<LevelCatalogSO>(ResourcePath));

        public static int Count => Asset != null ? Asset.Count : 0;
        public static TextAsset AssetAt(int index) => Asset != null ? Asset.AssetAt(index) : null;
        public static string IdAt(int index) => Asset != null ? Asset.IdAt(index) : "";

        // Zorluk, bölümün JSON'undan hesaplanır; ana ekran her karede sorabilsin
        // diye sonuç önbelleğe alınır. Aksi hâlde saniyede bir tam bölüm
        // ayrıştırmak gerekirdi.
        static readonly System.Collections.Generic.Dictionary<int, BlockOut.Core.LevelDifficulty>
            _difficulty = new System.Collections.Generic.Dictionary<int, BlockOut.Core.LevelDifficulty>();

        public static BlockOut.Core.LevelDifficulty DifficultyAt(int index)
        {
            if (_difficulty.TryGetValue(index, out var cached)) return cached;

            var result = BlockOut.Core.LevelDifficulty.Normal;
            var text = AssetAt(index);
            if (text != null)
            {
                try
                {
                    var data = BlockOut.Runtime.Level.LevelLoader.Parse(text.text);
                    result = BlockOut.Core.LevelDifficultyRule.Of(BlockOut.Core.LevelModel.Build(data));
                }
                catch (System.Exception error)
                {
                    Debug.LogWarning($"[LevelCatalog] {index + 1}. bölümün zorluğu okunamadı: {error.Message}");
                }
            }

            _difficulty[index] = result;
            return result;
        }
    }
}
