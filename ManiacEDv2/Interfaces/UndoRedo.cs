using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ManiacEDv2.Actions;

namespace ManiacEDv2.Interfaces
{
    public static class UndoRedo
    {
        //Undo + Redo
        public static Stack<IAction> UndoStack = new Stack<IAction>(); //Undo Actions Stack
        public static Stack<IAction> RedoStack = new Stack<IAction>(); //Redo Actions Stack

        public static void EditorUndo(ref Editor Instance)
        {
            if (UndoStack.Count > 0)
            {
                //if (IsTilesEdit())
                //{
                    // Deselect to apply the changes
                    Interfaces.Layer.Deselect(ref Instance.EditorScene, Instance.EditorScene.ForegroundHigh);
                //}
                //else if (IsEntitiesEdit())
                //{
                //    if (UndoStack.Peek() is ActionAddDeleteEntities)
                //    {
                        // deselect only if delete/create
                //        Deselect();
                //    }
                //}
                IAction act = UndoStack.Pop();
                act.Undo();
                RedoStack.Push(act.Redo());
                //if (IsEntitiesEdit() && IsSelected())
                //{
                    // We need to update the properties of the selected entity
                //    EntitiesToolbar.UpdateCurrentEntityProperites();
                //}
            }
            Instance.GraphicsHostControl.GraphicPanel.Render();
            UpdateUndoRedoButtons(ref Instance);
        }
        public static void EditorRedo(ref Editor Instance)
        {
            if (RedoStack.Count > 0)
            {
                IAction act = RedoStack.Pop();
                act.Undo();
                UndoStack.Push(act.Redo());
                /*
                if (IsEntitiesEdit() && IsSelected())
                {
                    // We need to update the properties of the selected entity
                    EntitiesToolbar.UpdateCurrentEntityProperites();
                }*/
            }
            Instance.GraphicsHostControl.GraphicPanel.Render();
            UpdateUndoRedoButtons(ref Instance);
        }

        public static void UpdateLayerStack(ref Editor Instance)
        {
            List<IAction> actions = Instance.EditorScene.ForegroundHigh?.Actions;
            if (actions.Count > 0) RedoStack.Clear();
            while (actions.Count > 0)
            {
                bool create_new = false;
                if (UndoStack.Count == 0 || !(UndoStack.Peek() is ActionsGroup))
                {
                    create_new = true;
                }
                else
                {
                    create_new = (UndoStack.Peek() as ActionsGroup).IsClosed;
                }
                if (create_new)
                {
                    UndoStack.Push(new ActionsGroup());
                }
                (UndoStack.Peek() as ActionsGroup).AddAction(actions[0]);
                actions.RemoveAt(0);
            }
        }

        #region UI Refresh Methods
        public static void UpdateUndoRedoButtons(ref Editor Instance, bool enabled = true)
        {
            //Instance.UndoButton.IsEnabled = enabled && Interfaces.UndoRedo.UndoStack.Count > 0;
            //Instance.RedoButton.IsEnabled = enabled && Interfaces.UndoRedo.RedoStack.Count > 0;

            //Instance.undoToolStripMenuItem.IsEnabled = enabled && Interfaces.UndoRedo.UndoStack.Count > 0;
            //Instance.redoToolStripMenuItem.IsEnabled = enabled && Interfaces.UndoRedo.RedoStack.Count > 0;

            Instance.UndoButton.IsEnabled = Interfaces.UndoRedo.UndoStack.Count > 0;
            Instance.RedoButton.IsEnabled = Interfaces.UndoRedo.RedoStack.Count > 0;

            Instance.undoToolStripMenuItem.IsEnabled = Interfaces.UndoRedo.UndoStack.Count > 0;
            Instance.redoToolStripMenuItem.IsEnabled = Interfaces.UndoRedo.RedoStack.Count > 0;

            UpdateTooltipForStacks(Instance.UndoButton, UndoStack);
            UpdateTooltipForStacks(Instance.RedoButton, RedoStack);
            UpdateTextBlockForStacks(Instance.UndoMenuItemInfo, UndoStack);
            UpdateTextBlockForStacks(Instance.RedoMenuItemInfo, RedoStack);
        }
        private static void UpdateTextBlockForStacks(System.Windows.Controls.TextBlock tsb, Stack<IAction> actionStack)
        {
            if (actionStack?.Count > 0)
            {
                IAction action = actionStack.Peek();
                tsb.Visibility = System.Windows.Visibility.Visible;
                tsb.Text = string.Format("({0})", action.Description);
            }
            else
            {
                tsb.Visibility = System.Windows.Visibility.Collapsed;
                tsb.Text = string.Empty;
            }
        }
        private static void UpdateTooltipForStacks(System.Windows.Controls.Button tsb, Stack<IAction> actionStack)
        {
            if (actionStack?.Count > 0)
            {
                IAction action = actionStack.Peek();
                System.Windows.Controls.ToolTip tooltip = new System.Windows.Controls.ToolTip { Content = string.Format(tsb.Tag.ToString(), action.Description + " ") };
                tsb.ToolTip = tooltip;
            }
            else
            {
                System.Windows.Controls.ToolTip tooltip = new System.Windows.Controls.ToolTip { Content = string.Format(tsb.Tag.ToString(), string.Empty) };
                tsb.ToolTip = tooltip;
            }
        }

        #endregion


    }
}
