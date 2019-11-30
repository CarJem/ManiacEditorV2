using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManiacEDv2.Interfaces
{
    public class Layer
    {
        public static void SetSelection(ref System.Drawing.Point Pos, System.Drawing.Point NewPos, int Zoom)
        {
            Pos = new System.Drawing.Point(NewPos.X / (int)Zoom, NewPos.Y / (int)Zoom);
        }

        public static void Select(ref EditorScene scene, EditorLayer layer, System.Drawing.Point pos, bool addSelection = false, bool deselectIfSelected = false)
        {
            if (layer != null)
            {
                int index = scene.Layers.IndexOf(layer);
                scene.Layers[index].Select(pos, addSelection, deselectIfSelected);
            }
        }


        public static void StartDrag(ref EditorScene scene, EditorLayer layer)
        {
            if (layer != null)
            {
                int index = scene.Layers.IndexOf(layer);
                scene.Layers[index].StartDrag();
            }
        }

        public static void EndDrag(ref EditorScene scene, EditorLayer layer)
        {
            if (layer != null)
            {
                int index = scene.Layers.IndexOf(layer);
                scene.Layers[index].EndDrag();
            }
        }

        public static void MoveSelection(ref EditorScene scene, EditorLayer layer, System.Drawing.Point OldPos, System.Drawing.Point NewPos, bool duplicate = false, bool chunkAlign = false)
        {
            if (layer != null)
            {
                int index = scene.Layers.IndexOf(layer);
                scene.Layers[index].MoveSelected(OldPos, NewPos, duplicate, chunkAlign);
            }
        }

        public static void TempSelectRegion(ref EditorScene scene, EditorLayer layer, System.Drawing.Point SelectionXY_1, System.Drawing.Point SelectionXY_2, bool addSelection = false, bool deselectIfSelected = false)
        {
            if (layer != null)
            {
                int index = scene.Layers.IndexOf(layer);
                int x1 = SelectionXY_1.X, x2 = SelectionXY_2.X;
                int y1 = SelectionXY_1.Y, y2 = SelectionXY_2.Y;

                if (x1 != x2 && y1 != y2)
                {
                    if (x1 > x2)
                    {
                        x1 = SelectionXY_2.X;
                        x2 = SelectionXY_1.X;
                    }
                    if (y1 > y2)
                    {
                        y1 = SelectionXY_2.Y;
                        y2 = SelectionXY_1.Y;
                    }
                }

                scene.Parent.UpdateSelectedTilesLabels();
                scene.Layers[index].TempSelection(new System.Drawing.Rectangle(x1, y1, x2 - x1, y2 - y1), deselectIfSelected);
                scene.Layers[index].InvalidateChunks();

            }
        }

        public static void SelectRegion(ref EditorScene scene, EditorLayer layer, System.Drawing.Point SelectionXY_1, System.Drawing.Point SelectionXY_2, bool addSelection = false, bool deselectIfSelected = false)
        {
            if (layer != null)
            {
                int index = scene.Layers.IndexOf(layer);
                int x1 = SelectionXY_1.X, x2 = SelectionXY_2.X;
                int y1 = SelectionXY_1.Y, y2 = SelectionXY_2.Y;

                if (x1 != x2 && y1 != y2)
                {
                    if (x1 > x2)
                    {
                        x1 = SelectionXY_2.X;
                        x2 = SelectionXY_1.X;
                    }
                    if (y1 > y2)
                    {
                        y1 = SelectionXY_2.Y;
                        y2 = SelectionXY_1.Y;
                    }
                }


                scene.Layers[index].Select(new System.Drawing.Rectangle(x1, y1, x2 - x1, y2 - y1), addSelection, deselectIfSelected);
            }
        }
        public static void Deselect(ref EditorScene scene, EditorLayer layer)
        {
            if (layer != null)
            {
                int index = scene.Layers.IndexOf(layer);
                bool wasMoved = scene.Layers[index].SelectionMoved;
                scene.Layers[index].Deselect();
                if (wasMoved)
                {
                    UndoRedo.UpdateLayerStack(ref scene.Parent);
                    UndoRedo.UpdateUndoRedoButtons(ref scene.Parent);
                }
            }
        }


        private static void PlaySound()
        {
            using (var soundPlayer = new System.Media.SoundPlayer(@"c:\Windows\Media\chimes.wav"))
            {
                soundPlayer.Play(); // can also use soundPlayer.PlaySync()
            }
        }
    }
}
