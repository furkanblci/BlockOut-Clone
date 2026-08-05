using System;
using System.Collections.Generic;
using BlockOut.Core;
using BlockOut.Runtime.Config;
using BlockOut.Runtime.Input;
using UnityEngine;

namespace BlockOut.Runtime.Board
{
    /// <summary>
    /// Girdi → model köprüsü: parmağın ekran konumunu tahta düzlemine indirir,
    /// blok seçer, her karede DragSolver'a taşıtır, bırakınca hücreye oturtur.
    ///
    /// DERS (neden MonoBehaviour değil?): Bu sınıfın Update'e ihtiyacı yok —
    /// PointerInputService'in olayları zaten kare başına gelir. Sahneye nesne
    /// eklemeden, bağımlılıkları kurucuda alan saf bir sınıf hem test edilebilir
    /// hem de yaşam döngüsü nettir (Dispose = abonelikleri bırak).
    /// </summary>
    public sealed class DragController : IDisposable
    {
        readonly PointerInputService _input;
        readonly Camera _camera;
        readonly LevelModel _level;
        readonly BoardViews _views;
        readonly BoardSpace _space;
        readonly GameConfigSO _config;
        readonly GateSystem _gates;
        readonly Func<bool> _canDrag;

        // Sürükleme başında BİR KEZ doldurulur; her kare yeniden taranmaz.
        readonly List<Aabb> _obstacles = new List<Aabb>(64);

        BlockModel _dragged;
        Vector2 _grabOffset; // blok köşesi ile tutma noktası arasındaki fark

        public DragController(
            PointerInputService input, Camera camera, LevelModel level, BoardViews views,
            BoardSpace space, GameConfigSO config, GateSystem gates, Func<bool> canDrag)
        {
            _input = input;
            _camera = camera;
            _level = level;
            _views = views;
            _space = space;
            _config = config;
            _gates = gates;
            _canDrag = canDrag;

            _input.PointerDown += OnPointerDown;
            _input.PointerHeld += OnPointerHeld;
            _input.PointerUp += OnPointerUp;
        }

        public void Dispose()
        {
            _input.PointerDown -= OnPointerDown;
            _input.PointerHeld -= OnPointerHeld;
            _input.PointerUp -= OnPointerUp;
        }

        /// <summary>
        /// Ekran noktasını y=0 tahta düzlemine ışınla indirir (fizik raycast'i
        /// DEĞİL — UnityEngine.Plane saf bir matematik yapısıdır).
        /// </summary>
        bool TryPointerToCell(Vector2 screenPos, out Vector2 cell)
        {
            var ray = _camera.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float dist))
            {
                cell = _space.WorldToCell(ray.GetPoint(dist));
                return true;
            }
            cell = default;
            return false;
        }

        void OnPointerDown(Vector2 screenPos)
        {
            if (_dragged != null || !_canDrag() || !TryPointerToCell(screenPos, out var cell))
                return;

            foreach (var block in _level.Blocks)
            {
                // Polyomino: sınırlayıcı kutu kabaca eler, sonra GERÇEK hücreye
                // bakılır — L şeklinin boş köşesine dokunmak bloğu tutmamalı.
                var bounds = block.Bounds;
                if (cell.x < bounds.MinX || cell.x > bounds.MaxX ||
                    cell.y < bounds.MinY || cell.y > bounds.MaxY) continue;
                if (!block.CoversCell(Mathf.FloorToInt(cell.x), Mathf.FloorToInt(cell.y)))
                    continue;

                if (block.IsFrozen) return; // buzlu blok kilitli — sallanma efekti M4'te

                _dragged = block;
                _grabOffset = block.Position - cell;
                _obstacles.Clear();
                _level.CollectObstacles(_obstacles, block);
                _views.Blocks[block].SetHighlight(true);
                return;
            }
        }

        void OnPointerHeld(Vector2 screenPos)
        {
            if (_dragged == null) return;
            if (!_canDrag()) { EndDrag(); return; } // süre sürükleme ortasında dolabilir

            if (!TryPointerToCell(screenPos, out var cell)) return;

            _dragged.Position = DragSolver.Solve(
                _dragged.Position, cell + _grabOffset, _dragged.Cells,
                _obstacles, _config.dragSubstep, _config.collisionEpsilon);
            _views.Blocks[_dragged].SyncFromModel();

            // Emilme VE katman soyulması sürükleme sırasında gerçekleşir;
            // ikisi de sürüklemeyi bitirir (video kuralı).
            if (_gates.ResolveContact(_dragged) != GateContactResult.None)
                _dragged = null;
        }

        void OnPointerUp(Vector2 screenPos)
        {
            if (_dragged != null) EndDrag();
        }

        void EndDrag()
        {
            var block = _dragged;
            _dragged = null;

            block.Position = DragSolver.SnapToGrid(
                block.Position, block.Cells, _obstacles, _config.collisionEpsilon);

            if (_views.Blocks.TryGetValue(block, out var view))
            {
                view.SetHighlight(false);
                view.SyncFromModel();
            }

            // Kapıya dayalı bırakıldıysa oturur oturmaz emilsin/soyulsun.
            _gates.ResolveContact(block);
        }
    }
}
