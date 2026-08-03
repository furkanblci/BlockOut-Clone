using System;
using BlockOut.Core;
using UnityEngine;

namespace BlockOut.Runtime.Config
{
    /// <summary>
    /// BlockColor -> görsel karşılıkları (materyal, UI rengi, partikül rengi).
    /// Oyunun tüm renk görünümü TEK asset'ten yönetilir; palet değişikliği
    /// kod değişikliği gerektirmez.
    ///
    /// DERS: Bu, "veri odaklı tasarım"ın en küçük örneği. GateView, BlockView,
    /// partikül sistemleri renk enum'unu buraya sorar; hiçbir view kendi
    /// içinde Color(1,0,0) gibi sihirli değer taşımaz.
    /// </summary>
    [CreateAssetMenu(menuName = "Block Out/Color Palette", fileName = "ColorPalette")]
    public sealed class ColorPaletteSO : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public BlockColor color;

            [Tooltip("Blok mesh'inin paylaşımlı materyali. 8 renk = 8 paylaşımlı materyal (SRP Batcher dostu; MaterialPropertyBlock KULLANMIYORUZ).")]
            public Material blockMaterial;

            [Tooltip("Kapı barının tonu ve UI göstergeleri için düz renk.")]
            public Color uiColor = Color.white;

            [Tooltip("Kapıdan çıkışta patlayan tuğla kırıntısı partiküllerinin rengi.")]
            public Color particleColor = Color.white;
        }

        [SerializeField] Entry[] entries = Array.Empty<Entry>();

        // Enum değeriyle O(1) erişim için önbellek. Lazy kurulur çünkü
        // OnEnable sırası SO'larda güvenilmezdir.
        //
        // DERS (domain reload tuzağı): Unity, script yeniden yüklenirken
        // [SerializeField] OLMAYAN private alanları bile anlık görüntüye alır
        // ve null dizileri BOŞ diziye normalize eder. [NonSerialized] "bu
        // alana hiç dokunma" der; uzunluk kontrolü de ikinci emniyet kemeri.
        [NonSerialized] Entry[] _lookup;

        public Entry Get(BlockColor color)
        {
            if (_lookup == null || _lookup.Length == 0) BuildLookup();
            var entry = _lookup[(int)color];
            if (entry == null)
                Debug.LogError($"[ColorPalette] '{color}' için palet girdisi yok! {name} asset'ini kontrol et.", this);
            return entry;
        }

        void BuildLookup()
        {
            int count = Enum.GetValues(typeof(BlockColor)).Length;
            _lookup = new Entry[count];
            foreach (var e in entries)
                _lookup[(int)e.color] = e;
        }

#if UNITY_EDITOR
        /// <summary>Kurulum aracı varsayılan 8 rengi doldururken kullanır.</summary>
        public void EditorSetEntries(Entry[] newEntries)
        {
            entries = newEntries;
            _lookup = null;
        }

        /// <summary>Kurulum aracı materyal bağlarken girdilere doğrudan erişir.</summary>
        public Entry[] EditorEntries => entries;
#endif
    }
}
