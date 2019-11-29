using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace ManiacEDv2.Interfaces
{
    public class Control
    {
        #region Core
        public static bool LeftMouseDown = false;
        public static bool RightMouseDown = false;
        public static bool Dragging = false;

        public static Point MousePosition { get; set; } = new Point();
        public static Point LastMousePosition { get; set; } = new Point();
        public static Point ClickPoint { get; set; } = new Point();
        public static Point LastRegionStart { get; set; } = new Point();
        public static Point LastRegionEnd { get; set; } = new Point();
        public static Point RegionStart { get; set; } = new Point();
        public static Point RegionEnd { get; set; } = new Point();
        public static Point LastSelectRegionStart { get; set; } = new Point();
        public static Point LastSelectRegionEnd { get; set; } = new Point();

        public static Point ZoomedMousePosition
        {
            get => new Point((int)(MousePosition.X / Editor.Zoom), (int)(MousePosition.Y / Editor.Zoom));
        }
        public static Point ZoomedLastMousePosition
        {
            get => new Point((int)(LastMousePosition.X / Editor.Zoom), (int)(LastMousePosition.Y / Editor.Zoom));
        }
        public static Point ZoomedClickPoint
        {
            get => new Point((int)(ClickPoint.X / Editor.Zoom), (int)(ClickPoint.Y / Editor.Zoom));
        }
        public static Point ZoomedLastRegionStart
        {
            get => new Point((int)(LastRegionStart.X / Editor.Zoom), (int)(LastRegionStart.Y / Editor.Zoom));
        }
        public static Point ZoomedLastRegionEnd
        {
            get => new Point((int)(LastRegionEnd.X / Editor.Zoom), (int)(LastRegionEnd.Y / Editor.Zoom));
        }
        public static Point ZoomedRegionStart
        {
            get => new Point((int)(RegionStart.X / Editor.Zoom), (int)(RegionStart.Y / Editor.Zoom));
        }
        public static Point ZoomedRegionEnd
        {
            get => new Point((int)(RegionEnd.X / Editor.Zoom), (int)(RegionEnd.Y / Editor.Zoom));
        }
        public static Point ZoomedLastSelectRegionStart
        {
            get => new Point((int)(LastSelectRegionStart.X / Editor.Zoom), (int)(LastSelectRegionStart.Y / Editor.Zoom));
        }
        public static Point ZoomedLastSelectRegionEnd
        {
            get => new Point((int)(LastSelectRegionEnd.X / Editor.Zoom), (int)(LastSelectRegionEnd.Y / Editor.Zoom));
        }


        public static bool InMiddleOfDrag = false;
        public static bool InMiddleOfDragSingle = false;
        public static bool InMiddleOfSelection = false;
        public static bool SelectionMade = false;
        public static bool SingleSelectionMade = false;
        public static bool DragHasBeenStarted = false;
        public static bool NotADrag = false;


        public static bool CtrlPressed()
        {
            return System.Windows.Forms.Control.ModifierKeys.HasFlag(System.Windows.Forms.Keys.Control);
        }
        public static bool ShiftPressed()
        {
            return System.Windows.Forms.Control.ModifierKeys.HasFlag(System.Windows.Forms.Keys.Shift);
        }

        public static void MouseUp(ref Editor ed, MouseEventArgs e)
        {
            ed.UpdateClickLabels();

            if (e.Button == MouseButtons.Left)
            {
                LeftMouseDown = false;
            }
            if (e.Button == MouseButtons.Right)
            {
                RightMouseDown = false;
            }

            LastRegionStart = RegionStart;
            LastRegionEnd = RegionEnd;

            RegionStart = new Point();
            RegionEnd = new Point();

            if (e.Location == ClickPoint) MouseUp_Click(ref ed, e);
            else MouseUp_NoClick(ref ed, e);


        }

        public static void MouseUp_Click(ref Editor ed, MouseEventArgs e)
        {
            GraphicPanel_MouseClick(ref ed, e);
        }

        public static void MouseUp_NoClick(ref Editor ed, MouseEventArgs e)
        {
            GraphicPanel_MouseUp(ref ed, e);
        }

        public static void MouseDown(ref Editor ed, MouseEventArgs e)
        {
            RegionEnd = new Point();
            if (e.Button == MouseButtons.Left)
            {
                LeftMouseDown = true;
            }
            if (e.Button == MouseButtons.Right)
            {
                RightMouseDown = true;
            }
            if (e.Button == MouseButtons.Middle)
            {

            }

            ClickPoint = e.Location;
            RegionStart = e.Location;

            ed.UpdateClickLabels();
            GraphicPanel_MouseDown(ref ed, e);
        }

        public static void MouseMove(ref Editor ed, MouseEventArgs e)
        {
            if (LeftMouseDown || RightMouseDown)
            {
                Dragging = true;
            }
            else
            {
                Dragging = false;
            }

            if (Dragging)
            {
                RegionEnd = e.Location;
            }

            GraphicPanel_MouseMove(ref ed, e);
            ed.UpdateClickLabels();
        }

        private void KeyDown(ref Editor ed, KeyEventArgs e)
        {

        }

        private void KeyUp(ref Editor ed, KeyEventArgs e)
        {

        }

        #endregion

        #region Editor

        public static void GraphicPanel_MouseUp(ref Editor sender, MouseEventArgs e)
        {
            EndActivities(ref sender);
            Interfaces.Layer.EndDrag(ref sender.EditorScene, sender.EditorScene.EditLayer);
        }
        public static void EndActivities(ref Editor ed)
        {
            if (InMiddleOfDrag) InMiddleOfDrag = false;
            if (InMiddleOfDragSingle) InMiddleOfDragSingle = false;
            if (InMiddleOfSelection)
            {
                InMiddleOfSelection = false;
                SingleSelectionMade = false;
                SelectionMade = true;
                Interfaces.Layer.SelectRegion(ref ed.EditorScene, ed.EditorScene.EditLayer, Interfaces.Control.ZoomedLastRegionStart, Interfaces.Control.ZoomedLastRegionEnd, ShiftPressed() || CtrlPressed(), CtrlPressed());
                if (ed.EditorScene.EditLayer != null) ed.EditorScene.EditLayer.EndTempSelection();
            }
        }
        public static void GraphicPanel_MouseClick(ref Editor sender, MouseEventArgs e)
        {

            EndActivities(ref sender);
            bool hasPointBeenSelected = sender.EditorScene.EditLayer?.IsPointSelected(ZoomedLastMousePosition) ?? false;
            if (e.Button == MouseButtons.Left)
            {
                if (SelectionMade && !hasPointBeenSelected && !InMiddleOfDragSingle)
                {
                    SelectionMade = false;
                    SingleSelectionMade = false;
                    Interfaces.Layer.Deselect(ref sender.EditorScene, sender.EditorScene.EditLayer);
                }
                else
                {
                    // Start dragging the single selected tile
                    if (!(sender.EditorScene.EditLayer?.Dragging ?? false))
                    {
                        Interfaces.Layer.Select(ref sender.EditorScene, sender.EditorScene.EditLayer, ZoomedMousePosition, ShiftPressed() || CtrlPressed(), CtrlPressed());
                        Interfaces.Layer.StartDrag(ref sender.EditorScene, sender.EditorScene.EditLayer);
                        SingleSelectionMade = true;
                        InMiddleOfDragSingle = true;
                    }

                    var clicked_point = new Point((int)(e.X / Editor.Zoom), (int)(e.Y / Editor.Zoom));
                    bool selectedNow = sender.EditorScene.EditLayer?.IsPointSelected(clicked_point) ?? false;
                    if (!selectedNow)
                    {
                        InMiddleOfDragSingle = false;
                        SingleSelectionMade = false;
                        Interfaces.Layer.EndDrag(ref sender.EditorScene, sender.EditorScene.EditLayer);
                        Interfaces.Layer.Deselect(ref sender.EditorScene, sender.EditorScene.EditLayer);
                    }

                }
            }
        }
        public static void GraphicPanel_MouseDown(ref Editor sender, MouseEventArgs e)
        {
            var clicked_point = new Point((int)(e.X / Editor.Zoom), (int)(e.Y / Editor.Zoom));
            bool hasPointBeenSelected = sender.EditorScene.EditLayer?.IsPointSelected(clicked_point) ?? false;
            bool isTileAtPoint = sender.EditorScene.EditLayer?.IsTileAtPoint(clicked_point) ?? false;
            bool isSelectTool = sender.SelectToolButton.IsChecked.Value;

            if (e.Button == MouseButtons.Left)
            {
                bool selectionNotMade = !SelectionMade && (hasPointBeenSelected || isTileAtPoint);
                bool selectionMadeButTileNotSelected = SelectionMade && (!hasPointBeenSelected && isTileAtPoint);
                if (!isSelectTool && (selectionNotMade || selectionMadeButTileNotSelected))
                {

                    // Start dragging the single selected tile
                    if (sender.EditorScene.EditLayer.Dragging)
                    {
                        InMiddleOfDragSingle = false;
                        SelectionMade = false;
                        SingleSelectionMade = false;
                        Interfaces.Layer.EndDrag(ref sender.EditorScene, sender.EditorScene.EditLayer);
                        Interfaces.Layer.Deselect(ref sender.EditorScene, sender.EditorScene.EditLayer);
                    }
                    Interfaces.Layer.Select(ref sender.EditorScene, sender.EditorScene.EditLayer, ZoomedMousePosition, ShiftPressed() || CtrlPressed(), CtrlPressed());
                    Interfaces.Layer.StartDrag(ref sender.EditorScene, sender.EditorScene.EditLayer);
                    InMiddleOfDragSingle = true;
                    SingleSelectionMade = true;

                }

            }
        }
        public static void GraphicPanel_MouseMove_EdgeScroll(ref Editor sender, Point e)
        {
            System.Windows.Point position = new System.Windows.Point(sender.HScrollBar.Value, sender.VScrollBar.Value);
            double ScreenMaxX = position.X + sender.ViewPanel.ActualWidth;
            double ScreenMaxY = position.Y + sender.ViewPanel.ActualHeight;
            double ScreenMinX = position.X;
            double ScreenMinY = position.Y;

            double x = position.X;
            double y = position.Y;

            if (e.X > ScreenMaxX)
            {
                x += (e.X - ScreenMaxX) / 10;
            }
            else if (e.X < ScreenMinX)
            {
                x += (e.X - ScreenMinX) / 10;
            }
            if (e.Y > ScreenMaxY)
            {
                y += (e.Y - ScreenMaxY) / 10;
            }
            else if (e.Y < ScreenMinY)
            {
                y += (e.Y - ScreenMinY) / 10;
            }

            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x > sender.HScrollBar.Maximum) x = sender.HScrollBar.Maximum;
            if (y > sender.VScrollBar.Maximum) y = sender.VScrollBar.Maximum;

            if (x != position.X || y != position.Y)
            {
                sender.VScrollBar.Value = y;
                sender.HScrollBar.Value = x;

                sender.GraphicsHostControl.GraphicPanel.Render();

            }

        }
        public static void GraphicPanel_MouseMove(ref Editor sender, MouseEventArgs e)
        {
            LastMousePosition = MousePosition;
            MousePosition = e.Location;
            sender.UpdatePositionLabel();

            if (LeftMouseDown)
            {
                if (!InMiddleOfDragSingle) MouseMove_SelectMoveRegionAdjust(ref sender);
                else MouseMove_SingleSelectMoveRegionAdjust(ref sender);
            }

            if (Dragging)
            {
                GraphicPanel_MouseMove_EdgeScroll(ref sender, e.Location);
            }

            sender.UpdateClickLabels();
            sender.GraphicsHostControl.GraphicPanel.Render();
        }
        public static void MouseMove_SelectMoveRegionAdjust(ref Editor ed, bool chunkAlign = false)
        {
            bool PointSelected = ed.EditorScene.EditLayer?.IsPointSelected(ZoomedMousePosition) ?? false;

            if (SelectionMade && (PointSelected || InMiddleOfDrag))
            {
                // Start Dragging the Tiles
                if (!ed.EditorScene.EditLayer.Dragging) Interfaces.Layer.StartDrag(ref ed.EditorScene, ed.EditorScene.EditLayer);
                InMiddleOfDrag = true;
                Interfaces.Layer.MoveSelection(ref ed.EditorScene, ed.EditorScene.EditLayer, ZoomedLastMousePosition, ZoomedMousePosition, CtrlPressed(), chunkAlign);
            }
            else if (!SelectionMade)
            {
                // Start Selection for Drag
                InMiddleOfSelection = true;
                SingleSelectionMade = false;
                Interfaces.Layer.EndDrag(ref ed.EditorScene, ed.EditorScene.EditLayer);
                Interfaces.Layer.TempSelectRegion(ref ed.EditorScene, ed.EditorScene.EditLayer, Interfaces.Control.ZoomedRegionStart, Interfaces.Control.ZoomedRegionEnd, ShiftPressed() || CtrlPressed(), CtrlPressed());
            }
            else if ((SelectionMade && (!PointSelected || ed.SelectToolButton.IsChecked.Value) && !InMiddleOfDrag))
            {
                // Start/Continue Selection for Drag
                SelectionMade = false;
                SingleSelectionMade = false;
                InMiddleOfSelection = true;
                Interfaces.Layer.EndDrag(ref ed.EditorScene, ed.EditorScene.EditLayer);
                Interfaces.Layer.TempSelectRegion(ref ed.EditorScene, ed.EditorScene.EditLayer, Interfaces.Control.ZoomedRegionStart, Interfaces.Control.ZoomedRegionEnd, ShiftPressed() || CtrlPressed(), CtrlPressed());
            }

            Interfaces.UndoRedo.UpdateUndoRedoButtons(ref ed);

        }

        public static void MouseMove_SingleSelectMoveRegionAdjust(ref Editor ed, bool chunkAlign = false)
        {
            Interfaces.Layer.MoveSelection(ref ed.EditorScene, ed.EditorScene.EditLayer, ZoomedLastMousePosition, ZoomedMousePosition, CtrlPressed(), chunkAlign);        
        }

        #endregion
    }
}
