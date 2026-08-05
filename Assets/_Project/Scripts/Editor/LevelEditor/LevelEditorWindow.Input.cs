using System.Collections.Generic;
using BlockOut.Core;
using UnityEditor;
using UnityEngine;

namespace BlockOut.Editor.LevelEditor
{
    /// <summary>Level editörünün etkileşim katmanı: klavye, fare, seçim ve düzenleme işlemleri.</summary>
    public sealed partial class LevelEditorWindow
    {
        Vector2Int _lastHoverCell;

        // ---------------- klavye ----------------

        void HandleShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;
            bool ctrl = e.control || e.command;

            if (ctrl && e.keyCode == KeyCode.Z) { if (e.shift) Redo(); else Undo(); e.Use(); return; }
            if (ctrl && e.keyCode == KeyCode.Y) { Redo(); e.Use(); return; }
            if (ctrl && e.keyCode == KeyCode.S)
            {
                if (_path != null) SaveTo(_path);
                else { string path = LevelEditorIO.AskSavePath(_data.Id); if (path != null) SaveTo(path); }
                e.Use(); return;
            }
            if (ctrl && e.keyCode == KeyCode.C) { CopySelection(); e.Use(); return; }
            if (ctrl && e.keyCode == KeyCode.V) { PasteClipboard(); e.Use(); return; }
            if (ctrl && e.keyCode == KeyCode.D) { DuplicateSelection(); e.Use(); return; }
            if (ctrl && e.keyCode == KeyCode.A) { SelectAllBlocks(); e.Use(); return; }

            if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
            { DeleteSelection(); e.Use(); return; }
            if (e.keyCode == KeyCode.R) { RotateSelection(); e.Use(); return; }
            if (e.keyCode == KeyCode.F) { FlipSelection(); e.Use(); return; }
            if (e.keyCode == KeyCode.Escape) { _selections.Clear(); Repaint(); e.Use(); return; }

            // Ok tuşlarıyla 1 hücre kaydırma — hassas hizalama için.
            int dx = e.keyCode == KeyCode.RightArrow ? 1 : e.keyCode == KeyCode.LeftArrow ? -1 : 0;
            int dy = e.keyCode == KeyCode.DownArrow ? 1 : e.keyCode == KeyCode.UpArrow ? -1 : 0;
            if ((dx != 0 || dy != 0) && _selections.Count > 0)
            {
                Record();
                NudgeSelection(dx, dy);
                AfterChange();
                e.Use();
                return;
            }

            if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha6)
            {
                _tool = (Tool)(e.keyCode - KeyCode.Alpha1);
                if (_tool != Tool.Select) _selections.Clear();
                e.Use();
            }
        }

        // ---------------- fare ----------------

        void HandleInput(Rect area)
        {
            var e = Event.current;
            if (!area.Contains(e.mousePosition)) return;

            if (_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var hover))
                _lastHoverCell = hover;

            // Tekerlek: yakınlaştır. Orta tuş: kaydır. Her araçta geçerli.
            if (e.type == EventType.ScrollWheel)
            {
                _canvas.ZoomAt(e.mousePosition, e.delta.y);
                StashState(); e.Use(); Repaint();
                return;
            }
            if (e.button == 2)
            {
                if (e.type == EventType.MouseDrag)
                {
                    _canvas.Pan += e.delta;
                    StashState(); e.Use(); Repaint();
                }
                return;
            }

            bool erase = e.button == 1;
            switch (e.type)
            {
                case EventType.MouseDown: OnMouseDown(e, erase); break;
                case EventType.MouseDrag: OnMouseDrag(e, erase); break;
                case EventType.MouseUp: OnMouseUp(e); break;
            }
        }

        void OnMouseDown(Event e, bool erase)
        {
            _strokeEdges.Clear();

            switch (_tool)
            {
                case Tool.Select:
                    if (erase) { ShowContextMenu(e); return; }
                    BeginSelectionDrag(e);
                    break;

                case Tool.Shape:
                    Record();
                    PaintShape(e, true);
                    break;

                case Tool.Blocks:
                    if (erase) EraseAt(e); else PlaceBlock(e);
                    break;

                case Tool.Gates:
                    PlaceGate(e, erase);
                    break;

                case Tool.Walls:
                    Record();
                    PaintWall(e, erase);
                    break;

                case Tool.Curtain:
                    if (erase) EraseCurtain(e);
                    else if (_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                    { _regionStart = cell; e.Use(); }
                    break;
            }
        }

        void OnMouseDrag(Event e, bool erase)
        {
            switch (_tool)
            {
                case Tool.Select:
                    if (_movingSelection) MoveSelection(e);
                    else if (_boxSelecting) { e.Use(); Repaint(); }
                    break;
                case Tool.Shape: PaintShape(e, false); break;
                case Tool.Walls: PaintWall(e, erase); break; // sürükleyerek duvar dizisi
                case Tool.Curtain: e.Use(); Repaint(); break;
            }
        }

        void OnMouseUp(Event e)
        {
            if (_boxSelecting && _regionStart.HasValue &&
                _canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var boxEnd))
            {
                SelectInRegion(RegionRect(_regionStart.Value, boxEnd), e.control || e.command);
                e.Use();
            }
            else if (_tool == Tool.Curtain && _regionStart.HasValue &&
                     _canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
            {
                var region = RegionRect(_regionStart.Value, cell);
                Record();
                _data.Obstacles.Add(LevelEditorIO.NewCurtain(
                    region.x, region.y, region.width, region.height, _curtainCount));
                AfterChange();
                e.Use();
            }

            _movingSelection = false;
            _boxSelecting = false;
            _regionStart = null;
        }

        // ---------------- seçim ----------------

        void BeginSelectionDrag(Event e)
        {
            bool additive = e.control || e.command;

            if (TryPick(e.mousePosition, out var picked))
            {
                if (additive) ToggleSelection(picked);
                else if (!IsSelected(picked)) { _selections.Clear(); _selections.Add(picked); }

                // Alt+sürükle: taşımadan önce kopyasını bırak (standart editör refleksi).
                if (e.alt)
                {
                    Record();
                    DuplicateSelection(offset: false);
                }

                _movingSelection = true;
                _moveRecorded = e.alt; // Alt yolunda kayıt zaten alındı
                _preMoveSnapshot = LevelEditorIO.ToJson(_data);

                _canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell);
                var block = BlockOf(Primary);
                if (block != null)
                    _dragGrabOffset = new Vector2Int(block.X - cell.x, block.Y - cell.y);
                else if (Primary.Kind == SelKind.Curtain && Primary.Index < _data.Obstacles.Count)
                {
                    var curtain = _data.Obstacles[Primary.Index];
                    _dragGrabOffset = new Vector2Int(
                        LevelEditorIO.GetInt(curtain, "x") - cell.x,
                        LevelEditorIO.GetInt(curtain, "y") - cell.y);
                }
            }
            else
            {
                // Boş alana basıldı: kutu seçim başlat.
                if (!additive) _selections.Clear();
                if (_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var start))
                {
                    _regionStart = start;
                    _boxSelecting = true;
                }
            }

            e.Use();
            Repaint();
        }

        bool IsSelected(Selection selection)
        {
            foreach (var s in _selections) if (s.Same(selection)) return true;
            return false;
        }

        void ToggleSelection(Selection selection)
        {
            for (int i = 0; i < _selections.Count; i++)
                if (_selections[i].Same(selection)) { _selections.RemoveAt(i); return; }
            _selections.Add(selection);
        }

        void SelectInRegion(RectInt region, bool additive)
        {
            if (!additive) _selections.Clear();

            for (int i = 0; i < _data.Blocks.Count; i++)
            {
                var block = _data.Blocks[i];
                bool overlaps = block.X < region.xMax && block.X + block.W > region.x &&
                                block.Y < region.yMax && block.Y + block.H > region.y;
                if (!overlaps) continue;

                var selection = new Selection { Kind = SelKind.Block, Index = i };
                if (!IsSelected(selection)) _selections.Add(selection);
            }
            Repaint();
        }

        void SelectAllBlocks()
        {
            _selections.Clear();
            for (int i = 0; i < _data.Blocks.Count; i++)
                _selections.Add(new Selection { Kind = SelKind.Block, Index = i });
            _tool = Tool.Select;
            Repaint();
        }

        bool TryPick(Vector2 mouse, out Selection selection)
        {
            selection = Selection.None;

            for (int i = 0; i < _data.Gates.Count; i++)
            {
                var gate = _data.Gates[i];
                if (!SideUtil.TryParse(gate.Side, out var side)) continue;
                if (_canvas.GateRect(gate.X, gate.Y, side, gate.Length).Contains(mouse))
                {
                    selection = new Selection { Kind = SelKind.Gate, Index = i };
                    return true;
                }
            }

            if (!_canvas.TryCell(mouse, _data.Board.Width, _data.Board.Height, out var cell))
                return false;

            for (int i = _data.Blocks.Count - 1; i >= 0; i--)
                if (Covers(_data.Blocks[i], cell))
                {
                    selection = new Selection { Kind = SelKind.Block, Index = i };
                    return true;
                }

            for (int i = 0; i < _data.Obstacles.Count; i++)
            {
                var obstacle = _data.Obstacles[i];
                if (obstacle.Type != "curtain" || !CurtainCovers(obstacle, cell)) continue;

                var contents = LevelEditorIO.GetContents(obstacle);
                for (int j = contents.Count - 1; j >= 0; j--)
                    if (Covers(contents[j], cell))
                    {
                        selection = new Selection { Kind = SelKind.Content, Index = i, Sub = j };
                        return true;
                    }

                selection = new Selection { Kind = SelKind.Curtain, Index = i };
                return true;
            }
            return false;
        }

        // Polyomino: L şeklinin boş köşesine tıklamak bloğu SEÇMEMELİ.
        static bool Covers(BlockData block, Vector2Int cell) =>
            BlockShape.Covers(block, cell.x, cell.y);

        bool CurtainCovers(ObstacleData curtain, Vector2Int cell)
        {
            int x = LevelEditorIO.GetInt(curtain, "x"), y = LevelEditorIO.GetInt(curtain, "y");
            int w = LevelEditorIO.GetInt(curtain, "w", 1), h = LevelEditorIO.GetInt(curtain, "h", 1);
            return cell.x >= x && cell.x < x + w && cell.y >= y && cell.y < y + h;
        }

        ObstacleData CurtainCovering(Vector2Int cell)
        {
            foreach (var obstacle in _data.Obstacles)
                if (obstacle.Type == "curtain" && CurtainCovers(obstacle, cell))
                    return obstacle;
            return null;
        }

        List<BlockData> SelectedBlocks()
        {
            var blocks = new List<BlockData>();
            foreach (var selection in _selections)
            {
                var block = BlockOf(selection);
                if (block != null) blocks.Add(block);
            }
            return blocks;
        }

        void ShowContextMenu(Event e)
        {
            if (TryPick(e.mousePosition, out var picked) && !IsSelected(picked))
            {
                _selections.Clear();
                _selections.Add(picked);
            }
            if (_selections.Count == 0) return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Çoğalt  Ctrl+D"), false, () => DuplicateSelection());
            menu.AddItem(new GUIContent("Kopyala  Ctrl+C"), false, CopySelection);
            menu.AddItem(new GUIContent("Döndür  R"), false, RotateSelection);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Sil  Del"), false, DeleteSelection);
            menu.ShowAsContext();
            e.Use();
        }

        // ---------------- düzenleme işlemleri ----------------

        void MoveSelection(Event e)
        {
            // Kapılar kenara oturur: taşırken imlecin altındaki kenar yeniden hesaplanır.
            if (Primary.Kind == SelKind.Gate)
            {
                if (!_canvas.TryEdge(e.mousePosition, _data.Board.Width, _data.Board.Height,
                        out var gateCell, out var gateSide)) return;

                var gate = _data.Gates[Primary.Index];
                string sideId = gateSide.ToId();
                if (gate.X == gateCell.x && gate.Y == gateCell.y && gate.Side == sideId) return;

                EnsureMoveRecorded();
                gate.X = gateCell.x; gate.Y = gateCell.y; gate.Side = sideId;
                AfterChange(); e.Use();
                return;
            }

            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;

            if (Primary.Kind == SelKind.Curtain)
            {
                var curtain = _data.Obstacles[Primary.Index];
                int w = LevelEditorIO.GetInt(curtain, "w", 1), h = LevelEditorIO.GetInt(curtain, "h", 1);
                int nx = Mathf.Clamp(cell.x + _dragGrabOffset.x, 0, _data.Board.Width - w);
                int ny = Mathf.Clamp(cell.y + _dragGrabOffset.y, 0, _data.Board.Height - h);
                if (nx == LevelEditorIO.GetInt(curtain, "x") && ny == LevelEditorIO.GetInt(curtain, "y"))
                    return;

                EnsureMoveRecorded();
                LevelEditorIO.SetInt(curtain, "x", nx);
                LevelEditorIO.SetInt(curtain, "y", ny);
                AfterChange(); e.Use();
                return;
            }

            var primaryBlock = BlockOf(Primary);
            if (primaryBlock == null) return;

            int targetX = Mathf.Clamp(cell.x + _dragGrabOffset.x, 0, _data.Board.Width - primaryBlock.W);
            int targetY = Mathf.Clamp(cell.y + _dragGrabOffset.y, 0, _data.Board.Height - primaryBlock.H);
            int dx = targetX - primaryBlock.X, dy = targetY - primaryBlock.Y;
            if (dx == 0 && dy == 0) return;

            EnsureMoveRecorded();
            NudgeSelection(dx, dy);
            AfterChange(); e.Use();
        }

        /// <summary>Seçili tüm blokları (tahta içinde kalacak şekilde) kaydırır.</summary>
        void NudgeSelection(int dx, int dy)
        {
            foreach (var selection in _selections)
            {
                if (selection.Kind == SelKind.Gate || selection.Kind == SelKind.Curtain)
                {
                    if (selection.Kind == SelKind.Curtain && selection.Index < _data.Obstacles.Count)
                    {
                        var curtain = _data.Obstacles[selection.Index];
                        int w = LevelEditorIO.GetInt(curtain, "w", 1);
                        int h = LevelEditorIO.GetInt(curtain, "h", 1);
                        LevelEditorIO.SetInt(curtain, "x", Mathf.Clamp(
                            LevelEditorIO.GetInt(curtain, "x") + dx, 0, _data.Board.Width - w));
                        LevelEditorIO.SetInt(curtain, "y", Mathf.Clamp(
                            LevelEditorIO.GetInt(curtain, "y") + dy, 0, _data.Board.Height - h));
                    }
                    continue;
                }

                var block = BlockOf(selection);
                if (block == null) continue;
                block.X = Mathf.Clamp(block.X + dx, 0, _data.Board.Width - block.W);
                block.Y = Mathf.Clamp(block.Y + dy, 0, _data.Board.Height - block.H);
                CommitContentEdit(selection, block);
            }
        }

        /// <summary>Sürükleme boyunca TEK geri alma kaydı tutulur (her karede değil).</summary>
        void EnsureMoveRecorded()
        {
            if (_moveRecorded) return;
            _moveRecorded = true;
            _undoStack.Add(_preMoveSnapshot ?? LevelEditorIO.ToJson(_data));
            if (_undoStack.Count > 60) _undoStack.RemoveAt(0);
            _redoStack.Clear();
        }

        void RotateSelection()
        {
            var blocks = SelectedBlocks();
            if (blocks.Count == 0) return;

            Record();
            foreach (var selection in _selections)
            {
                var block = BlockOf(selection);
                if (block == null) continue;
                // Dikdörtgende döndürmek = w/h takası; maskeli blokta gerçek 90° dönüş.
                BlockShape.RotateCw(block);
                block.X = Mathf.Clamp(block.X, 0, _data.Board.Width - block.W);
                block.Y = Mathf.Clamp(block.Y, 0, _data.Board.Height - block.H);
                CommitContentEdit(selection, block);
            }
            AfterChange();
        }

        /// <summary>Seçili blokları yatay aynalar — L ile J arasında geçiş (F).</summary>
        void FlipSelection()
        {
            var blocks = SelectedBlocks();
            if (blocks.Count == 0) return;

            Record();
            foreach (var selection in _selections)
            {
                var block = BlockOf(selection);
                if (block == null) continue;
                BlockShape.FlipHorizontal(block);
                CommitContentEdit(selection, block);
            }
            AfterChange();
        }

        void ApplyColorToSelection(BlockColor color, int layerIndex)
        {
            Record();
            foreach (var selection in _selections)
            {
                var block = BlockOf(selection);
                if (block == null || block.Layers.Count == 0) continue;
                block.Layers[Mathf.Clamp(layerIndex, 0, block.Layers.Count - 1)] = color.ToId();
                CommitContentEdit(selection, block);
            }
            AfterChange();
        }

        void DeleteSelection()
        {
            if (_selections.Count == 0) return;
            Record();

            // Dizinler kaymasın diye büyükten küçüğe sil.
            var ordered = new List<Selection>(_selections);
            ordered.Sort((a, b) => b.Index != a.Index ? b.Index.CompareTo(a.Index) : b.Sub.CompareTo(a.Sub));

            foreach (var selection in ordered)
            {
                switch (selection.Kind)
                {
                    case SelKind.Block:
                        if (selection.Index < _data.Blocks.Count) _data.Blocks.RemoveAt(selection.Index);
                        break;
                    case SelKind.Gate:
                        if (selection.Index < _data.Gates.Count) _data.Gates.RemoveAt(selection.Index);
                        break;
                    case SelKind.Curtain:
                        if (selection.Index < _data.Obstacles.Count) _data.Obstacles.RemoveAt(selection.Index);
                        break;
                    case SelKind.Content:
                        if (selection.Index < _data.Obstacles.Count)
                        {
                            var curtain = _data.Obstacles[selection.Index];
                            var contents = LevelEditorIO.GetContents(curtain);
                            if (selection.Sub < contents.Count) contents.RemoveAt(selection.Sub);
                            LevelEditorIO.SetContents(curtain, contents);
                        }
                        break;
                }
            }

            _selections.Clear();
            AfterChange();
        }

        void DuplicateSelection(bool offset = true)
        {
            var blocks = SelectedBlocks();
            if (blocks.Count == 0) return;
            if (offset) Record();

            var created = new List<Selection>();
            foreach (var source in blocks)
            {
                var copy = new BlockData
                {
                    X = Mathf.Clamp(source.X + (offset ? 1 : 0), 0, _data.Board.Width - source.W),
                    Y = Mathf.Clamp(source.Y + (offset ? 1 : 0), 0, _data.Board.Height - source.H),
                    W = source.W, H = source.H, Ice = source.Ice,
                    Cells = source.Cells == null ? null : new List<string>(source.Cells),
                    Layers = new List<string>(source.Layers)
                };
                _data.Blocks.Add(copy);
                created.Add(new Selection { Kind = SelKind.Block, Index = _data.Blocks.Count - 1 });
            }

            _selections.Clear();
            _selections.AddRange(created);
            AfterChange();
        }

        // ---------------- pano ----------------

        void CopySelection()
        {
            var blocks = SelectedBlocks();
            if (blocks.Count == 0) return;
            LevelEditorClipboard.Copy(blocks);
            ShowNotification(new GUIContent($"{blocks.Count} blok kopyalandı"));
        }

        void PasteClipboard()
        {
            var pasted = LevelEditorClipboard.Paste(_lastHoverCell.x, _lastHoverCell.y);
            AddPastedBlocks(pasted);
        }

        void StampAtCenter(string name)
        {
            var pasted = LevelEditorClipboard.LoadStamp(name, _lastHoverCell.x, _lastHoverCell.y);
            AddPastedBlocks(pasted);
        }

        void AddPastedBlocks(List<BlockData> pasted)
        {
            if (pasted == null || pasted.Count == 0) return;
            Record();

            var created = new List<Selection>();
            foreach (var block in pasted)
            {
                block.X = Mathf.Clamp(block.X, 0, _data.Board.Width - block.W);
                block.Y = Mathf.Clamp(block.Y, 0, _data.Board.Height - block.H);
                _data.Blocks.Add(block);
                created.Add(new Selection { Kind = SelKind.Block, Index = _data.Blocks.Count - 1 });
            }

            _tool = Tool.Select;
            _selections.Clear();
            _selections.AddRange(created);
            AfterChange();
        }

        // ---------------- araç eylemleri ----------------

        void PaintShape(Event e, bool isFirst)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;

            // Sürükleme boyunca TEK değer boyanır; yoksa hücreler yanıp söner.
            if (isFirst) _shapePaintValue = !Playable(cell.x, cell.y);

            var row = _data.Board.Rows[cell.y].ToCharArray();
            char want = _shapePaintValue ? 'X' : '.';
            if (row[cell.x] == want) { e.Use(); return; }

            row[cell.x] = want;
            _data.Board.Rows[cell.y] = new string(row);
            AfterChange(); e.Use();
        }

        void PlaceBlock(Event e)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;
            Record();

            var block = new BlockData
            {
                X = Mathf.Clamp(cell.x, 0, _data.Board.Width - _blockW),
                Y = Mathf.Clamp(cell.y, 0, _data.Board.Height - _blockH),
                W = _blockW, H = _blockH, Ice = _blockIce,
                Cells = BrushMask()
            };
            foreach (var layer in _layers) block.Layers.Add(layer.ToId());

            var curtain = CurtainCovering(cell);
            if (curtain != null)
            {
                var contents = LevelEditorIO.GetContents(curtain);
                contents.Add(block);
                LevelEditorIO.SetContents(curtain, contents);
            }
            else _data.Blocks.Add(block);

            AfterChange(); e.Use();
        }

        void EraseAt(Event e)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;

            for (int i = _data.Blocks.Count - 1; i >= 0; i--)
                if (Covers(_data.Blocks[i], cell))
                {
                    Record();
                    _data.Blocks.RemoveAt(i);
                    AfterChange(); e.Use();
                    return;
                }

            foreach (var obstacle in _data.Obstacles)
            {
                if (obstacle.Type != "curtain") continue;
                var contents = LevelEditorIO.GetContents(obstacle);
                for (int i = contents.Count - 1; i >= 0; i--)
                    if (Covers(contents[i], cell))
                    {
                        Record();
                        contents.RemoveAt(i);
                        LevelEditorIO.SetContents(obstacle, contents);
                        AfterChange(); e.Use();
                        return;
                    }
            }
        }

        void PlaceGate(Event e, bool erase)
        {
            if (!_canvas.TryEdge(e.mousePosition, _data.Board.Width, _data.Board.Height,
                    out var cell, out var side))
                return;

            string sideId = side.ToId();
            int existing = _data.Gates.FindIndex(g => g.X == cell.x && g.Y == cell.y && g.Side == sideId);
            Record();

            if (erase)
            {
                if (existing >= 0) { _data.Gates.RemoveAt(existing); AfterChange(); }
                e.Use();
                return;
            }

            if (existing >= 0) _data.Gates.RemoveAt(existing);
            var gate = new GateData
            {
                X = cell.x, Y = cell.y, Side = sideId, Length = _gateLength, Ice = _gateIce
            };
            gate.Colors.Add(_gateColor.ToId());
            _data.Gates.Add(gate);
            AfterChange(); e.Use();
        }

        void PaintWall(Event e, bool erase)
        {
            if (!_canvas.TryEdge(e.mousePosition, _data.Board.Width, _data.Board.Height,
                    out var cell, out var side))
                return;

            var target = EdgeId.OfCellSide(cell.x, cell.y, side);
            if (!_strokeEdges.Add(target)) { e.Use(); return; } // aynı kenarı bir kez işle

            int existing = _data.Board.Walls.FindIndex(w =>
                SideUtil.TryParse(w.Side, out var s) &&
                EdgeId.OfCellSide(w.X, w.Y, s).Equals(target));

            if (erase)
            {
                if (existing >= 0) { _data.Board.Walls.RemoveAt(existing); AfterChange(); }
            }
            else if (existing < 0)
            {
                _data.Board.Walls.Add(new WallData
                {
                    X = cell.x, Y = cell.y, Side = side.ToId(), Length = 1
                });
                AfterChange();
            }
            e.Use();
        }

        void EraseCurtain(Event e)
        {
            if (!_canvas.TryCell(e.mousePosition, _data.Board.Width, _data.Board.Height, out var cell))
                return;
            var curtain = CurtainCovering(cell);
            if (curtain == null) return;

            Record();
            _data.Obstacles.Remove(curtain);
            AfterChange(); e.Use();
        }
    }
}
