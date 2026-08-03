using UnityEngine;

namespace BlockOut.Runtime.Board
{
    /// <summary>
    /// Hücre uzayı ↔ dünya uzayı dönüşümü. Tek gerçeklik kaynağı — hiçbir view
    /// kendi başına "0.5 ekle, z'yi ters çevir" hesabı yapmaz.
    ///
    /// Sözleşme: hücre uzayında x sağa, y AŞAĞI artar (JSON rows sırası);
    /// dünyada tahta merkezi orijindedir, +X sağa, +Z KUZEYE (ekranda yukarı).
    /// Bu yüzden y → Z dönüşümü işaret değiştirir.
    /// </summary>
    public readonly struct BoardSpace
    {
        public readonly int Width;
        public readonly int Height;

        public BoardSpace(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>Köşe-uzayı noktasını dünya konumuna çevirir (y = dünya yüksekliği).</summary>
        public Vector3 CornerToWorld(float cellX, float cellY, float worldY = 0f) =>
            new Vector3(cellX - Width * 0.5f, worldY, Height * 0.5f - cellY);

        /// <summary>Min-köşesi ve boyutu verilen hücre dikdörtgeninin MERKEZİNİ dünyaya çevirir.</summary>
        public Vector3 RectCenterToWorld(Vector2 minCorner, float w, float h, float worldY = 0f) =>
            CornerToWorld(minCorner.x + w * 0.5f, minCorner.y + h * 0.5f, worldY);

        /// <summary>Dünya noktasını (y yok sayılır) hücre uzayına çevirir.</summary>
        public Vector2 WorldToCell(Vector3 world) =>
            new Vector2(world.x + Width * 0.5f, Height * 0.5f - world.z);
    }
}
