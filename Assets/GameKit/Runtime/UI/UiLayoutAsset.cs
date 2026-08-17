using System.Collections.Generic;
using UnityEngine;

namespace GameKit.UI
{
    /// <summary>
    /// Kodun ürettiği bir arayüz öğesinin ELLE DEĞİŞTİRİLMİŞ hâli.
    /// Yalnız gerçekten değişen alanlar taşınır (bayraklar).
    /// </summary>
    [System.Serializable]
    public struct UiOverride
    {
        /// <summary>Ekran kökünden itibaren hiyerarşi yolu: "HomeScreen/Play/Ribbon".</summary>
        public string path;

        public bool hasRect;
        public Vector2 anchorMin, anchorMax, offsetMin, offsetMax, pivot;

        public bool hasScale;
        public Vector3 scale;

        public bool hasColor;
        public Color color;

        public bool hasFont;
        public float fontSize;

        public bool hasActive;
        public bool active;
    }

    /// <summary>
    /// Elle yapılan arayüz düzeltmelerinin tek kaydı.
    /// `Resources/UiLayout.asset` olarak durur; editördeki Tasarım penceresi yazar.
    ///
    /// DERS (ScriptableObject KENDİ ADIYLA AYNI DOSYADA olmak zorunda): Bu sınıf
    /// önce <see cref="UiTweak"/> ile aynı dosyadaydı — kod sorunsuz derlendi
    /// ama `AssetDatabase.CreateAsset` "No script asset for UiLayoutAsset"
    /// uyarısı verip SCRIPT REFERANSI BOŞ bir varlık üretti. O varlık diske
    /// yazılıyor, `Resources.Load` onu buluyor gibi görünüyor, ama içindeki
    /// veri hiçbir zaman geri okunamıyor. Uyarı seviyesinde kaldığı için de
    /// kolayca gözden kaçıyor.
    /// </summary>
    public sealed class UiLayoutAsset : ScriptableObject
    {
        [SerializeField] UiOverride[] entries = new UiOverride[0];

        public UiOverride[] Entries => entries;

        public void SetEntries(UiOverride[] value)
        {
            entries = value ?? new UiOverride[0];
            _lookup = null;
        }

        Dictionary<string, UiOverride> _lookup;

        public bool TryGet(string path, out UiOverride result)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, UiOverride>(entries.Length);
                foreach (var e in entries)
                    if (!string.IsNullOrEmpty(e.path)) _lookup[e.path] = e;
            }
            return _lookup.TryGetValue(path, out result);
        }
    }
}
