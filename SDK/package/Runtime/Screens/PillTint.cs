using GameKit.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit.Screens
{
    /// <summary>
    /// Kabartmalı düğmenin RENGİNİ tek çağrıyla değiştirir.
    ///
    /// Düğme dört katmandan oluşuyor ve her katmanın tonu tek bir taban
    /// renginin çarpanı (bkz. <see cref="MenuPage.PillButton"/>). Rengi
    /// değiştirmek dolayısıyla dört katmanın altı ayrı tonunu birden yeniden
    /// hesaplamak demek — bunu çağrı yerlerine bırakmak, ışık profilinin
    /// oyunun içinde birden fazla yerde yazılı olması demekti.
    ///
    /// DERS (renk değiştirmek için GÖRSEL değiştirmek pahalı bir alışkanlık):
    /// Ana ekrandaki OYNA düğmesi zorluğa göre yeşil/mor/kırmızı oluyordu ve
    /// bunu üç ayrı PNG arasında geçiş yaparak sağlıyordu. Üç dosya, üç kez
    /// içe aktarma ayarı, üç kez atlas yeri — ve üçü aynı ışığa sahip olsun
    /// diye üçünün de aynı elden çıkmış olması gerekiyordu. Işık profili
    /// çarpanla türetilince renk yalnızca bir <see cref="Color"/> oluyor.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PillTint : MonoBehaviour
    {
        [SerializeField] Image outline;
        [SerializeField] UiVerticalTint shell;
        [SerializeField] UiVerticalTint rim;
        [SerializeField] UiVerticalTint face;
        [SerializeField] TextMeshProUGUI label;

        Color _color = Color.white;

        /// <summary>Kurulumda katmanları bağlar.</summary>
        public void Bind(Image outlineImage, UiVerticalTint shellTint,
                         UiVerticalTint rimTint, UiVerticalTint faceTint)
        {
            outline = outlineImage;
            shell = shellTint;
            rim = rimTint;
            face = faceTint;
        }

        /// <summary>Yazıyı da kaydeder; konturu düğmenin rengini takip etsin diye.</summary>
        public void BindLabel(TextMeshProUGUI text) => label = text;

        /// <summary>Düğmenin tabanı. Altı ton buradan türetiliyor.</summary>
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                // Yazının konturu ile düğmenin kenarı AYRI tonlar (15. tur):
                // kontur koyu kalmalı, kenar yumuşak olmalı.
                var dark = MenuPage.Darken(value, MenuPage.OutlineTone);

                if (outline != null) outline.color = MenuPage.Darken(value, MenuPage.EdgeTone);
                shell?.Set(MenuPage.Brighten(value, MenuPage.ShellTop),
                           MenuPage.Darken(value, MenuPage.ShellBottom));
                rim?.Set(UnityEngine.Color.Lerp(value, UnityEngine.Color.white, 0.5f),
                         MenuPage.Darken(value, MenuPage.RimBottom));
                face?.Set(value, MenuPage.Darken(value, MenuPage.FaceBottom));

                if (label != null) UiKit.SetOutline(label, dark, 0.24f);
            }
        }
    }
}
