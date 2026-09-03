using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Meta
{
    /// <summary>
    /// Tüketilebilir bir öğe (power-up, güçlendirici, yardımcı) tanımı.
    ///
    /// SDK bu öğelerin NE YAPTIĞINI bilmez — yalnız kimliğini, adını ve
    /// ikonunu bilir. Mağaza kartını, günlük ödül hediyesini ve envanter
    /// rozetini çizmeye bu kadarı yeter.
    /// </summary>
    public readonly struct ConsumableDef
    {
        /// <summary>Kayıtta ve mağazada kullanılan kimlik — küçük harf, ör. "rocket".</summary>
        public readonly string Id;

        /// <summary>Oyuncuya gösterilen ad.</summary>
        public readonly string DisplayName;

        /// <summary><see cref="GameKit.Screens.Art"/> içindeki sprite adı.</summary>
        public readonly string IconName;

        public ConsumableDef(string id, string displayName, string iconName)
        {
            Id = id;
            DisplayName = displayName;
            IconName = iconName;
        }
    }

    /// <summary>
    /// SDK'nın oyundan ihtiyaç duyduğu HER ŞEY. Tek arayüz, tek bağlama noktası.
    ///
    /// DERS (neden böyle bir arayüz var?): Meta ekranları — mağaza, sıralama,
    /// yolculuk, günlük ödül — her oyunda aynıdır ama her oyunun bölüm listesi
    /// ve öğe seti farklıdır. Ekranların doğrudan <c>LevelCatalog</c> ya da
    /// <c>PowerUpKind</c> gibi OYUNA ÖZEL tiplere bakması, o ekranları o oyuna
    /// çivileyen tek şeydi. Aradaki ince arayüz o çiviyi söküyor: ekran "kaç
    /// bölüm var" diye sorar, cevabı kimin verdiğini bilmez.
    ///
    /// DERS (arayüzü KÜÇÜK tut): Buraya "oyunun her şeyi" konabilirdi. Konmadı —
    /// yalnız SDK ekranlarının GERÇEKTEN sorduğu şeyler var. Arayüz büyüdükçe
    /// yeni oyunda doldurulması gereken boşluk büyür ve SDK'yı kullanmak
    /// sıfırdan yazmaktan zahmetli hâle gelir.
    /// </summary>
    public interface IGameHost
    {
        /// <summary>Toplam bölüm sayısı (sıralama ve yolculuk ekranı için).</summary>
        int LevelCount { get; }

        /// <summary>Bölümün kayıt kimliği — ilerleme kayıtları bu anahtarla tutulur.</summary>
        string LevelIdAt(int index);

        /// <summary>Mağazada ve günlük ödülde görünen tüketilebilirler, gösterim sırasıyla.</summary>
        IReadOnlyList<ConsumableDef> Consumables { get; }
    }

    /// <summary>
    /// Oyunun kendini SDK'ya tanıttığı yer. Açılışta bir kez bağlanır.
    ///
    /// <code>
    /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    /// static void Bind() => GameHost.Bind(new MyGameHost());
    /// </code>
    ///
    /// DERS (bağlanmamışken de çalışmalı): Bağlanmadıysa <see cref="Current"/>
    /// boş bir uygulamaya düşer — sıfır bölüm, sıfır öğe. Ekranlar boş görünür
    /// ama HİÇBİRİ patlamaz. Bunun alternatifi olan <c>NullReferenceException</c>,
    /// yeni bir projede SDK'yı ilk kez çalıştıran kişinin karşısına oyunun
    /// açılmaması olarak çıkardı ve sebebi bulmak dakikalar alırdı.
    /// </summary>
    public static class GameHost
    {
        sealed class NullHost : IGameHost
        {
            static readonly ConsumableDef[] None = new ConsumableDef[0];
            public int LevelCount => 0;
            public string LevelIdAt(int index) => "level_" + (index + 1).ToString("000");
            public IReadOnlyList<ConsumableDef> Consumables => None;
        }

        static readonly IGameHost Fallback = new NullHost();

        /// <summary>Bağlı oyun; hiç bağlanmadıysa zararsız boş uygulama.</summary>
        public static IGameHost Current { get; private set; } = Fallback;

        /// <summary>Bağlı bir oyun var mı — ekranlar "içerik yok" durumunu ayırt edebilsin.</summary>
        public static bool IsBound => !ReferenceEquals(Current, Fallback);

        public static void Bind(IGameHost host) => Current = host ?? Fallback;

        /// <summary>Testler ve editör araçları için: bağlamayı geri al.</summary>
        public static void Unbind() => Current = Fallback;

        // ------------------------------------------------------------- kısayollar

        /// <summary>Kimliğe göre tüketilebilir tanımı; yoksa kimliğin kendisiyle bir vekil.</summary>
        public static ConsumableDef Consumable(string id)
        {
            var list = Current.Consumables;
            for (int i = 0; i < list.Count; i++)
                if (list[i].Id == id) return list[i];
            return new ConsumableDef(id, id, null);
        }
    }
}
