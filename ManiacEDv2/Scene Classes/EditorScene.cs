using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using RSDKv5;
using System.Drawing;

namespace ManiacEDv2
{
    public class EditorScene
    {

        public EditorTiles EditorTiles;
        public Editor Parent;

        Func<EditorLayer, bool> FGHighFunc = new Func<EditorLayer, bool>(el => el.Name.Replace("\0", "").Equals("FG High") || el.Name.Replace("\0", "").Equals("Ring Count"));
        Func<EditorLayer, bool> FGLowFunc = new Func<EditorLayer, bool>(el => el.Name.Replace("\0", "").Equals("FG Low") || el.Name.Replace("\0", "").Equals("Playfield"));
        Func<EditorLayer, bool> ScratchFunc = new Func<EditorLayer, bool>(el => el.Name.Replace("\0", "").Equals("Scratch"));
        Func<EditorLayer, bool> LowDetailsFunc = new Func<EditorLayer, bool>(el => el.Name.Replace("\0", "").Equals("FG Lower") || el.Name.Replace("\0", "").Equals("Low Details") || el.Name.Replace("\0", "").Equals("FG Supa Low"));
        Func<EditorLayer, bool> HighDetailsFunc = new Func<EditorLayer, bool>(el => el.Name.Replace("\0", "").Equals("FG Higher") || el.Name.Replace("\0", "").Equals("High Details") || el.Name.Replace("\0", "").Equals("FG Overlay") || el.Name.Replace("\0", "").Equals("FG Supa High"));
        Func<EditorLayer, bool> MoveFunc = new Func<EditorLayer, bool>(el => el.Name.Replace("\0", "").Equals("Move"));

        public Scene Scene;
        public List<EditorLayer> Layers = new List<EditorLayer>();

        public List<EditLayerToggleButton> ViewLayerButtons;
        public List<EditLayerToggleButton> EditLayerButtons;


        public EditorLayer EditLayer { get; set; }

        public EditorLayer LowDetails
        {
            get => Layers.FirstOrDefault(LowDetailsFunc);
            //set => SetPrimaryLayer(value, LowDetailsFunc);
        }
        public EditorLayer ForegroundLow
        {
            get => Layers.FirstOrDefault(FGLowFunc);
            //set => SetPrimaryLayer(value, FGLowFunc);
        }

        public EditorLayer Scratch
        {
            get => Layers.FirstOrDefault(ScratchFunc);
            //set => SetPrimaryLayer(value, ScratchFunc);
        }

        public EditorLayer Move
        {
            get => Layers.FirstOrDefault(MoveFunc);
            //set => SetPrimaryLayer(value, MoveFunc);
        }


        public EditorLayer HighDetails
        {
            get => Layers.FirstOrDefault(HighDetailsFunc);
            //set => SetPrimaryLayer(value, HighDetailsFunc);
        }


        public EditorLayer ForegroundHigh
        {
            get => Layers.FirstOrDefault(FGHighFunc);
            //set => SetPrimaryLayer(value, FGHighFunc);
        }

        private void SetPrimaryLayer(EditorLayer value, Func<EditorLayer, bool> func)
        {
            Layers[Layers.IndexOf(Layers.FirstOrDefault(func))] = value;
        }

        public EditorScene(string filePath, Editor _parent)
        {
            Parent = _parent;
            EditorScene self = this;
            EditorTiles = new EditorTiles(System.IO.Path.GetDirectoryName(filePath));
            Scene = new Scene(filePath);
            foreach (var layer in Scene.Layers)
            {
                Layers.Add(new EditorLayer(layer, ref self));
            }
            GenerateLayerButtons();
            UpdateLayerButtons();
        }

        #region View/Edit Layer Buttons

        private void GenerateLayerButtons()
        {
            if (ViewLayerButtons != null && EditLayerButtons != null)
            {
                ViewLayerButtons.Clear();
                EditLayerButtons.Clear();
            }
            else
            {
                ViewLayerButtons = new List<EditLayerToggleButton>();
                EditLayerButtons = new List<EditLayerToggleButton>();
            }


            // VIEW LAYERS
            foreach (var layer in Layers)
            {
                ViewLayerButtons.Add(GenerateLayerToggleButton(layer, true));
            }

            // EDIT LAYERS
            foreach (var layer in Layers)
            {
                EditLayerButtons.Add(GenerateLayerToggleButton(layer));
            }
        }

        private void UpdateLayerButtons()
        {
            int viewIndex = Parent.LayerToolbar.Items.IndexOf(Parent.ViewLayerListStart) + 1;

            // VIEW LAYERS
            foreach (var layer in ViewLayerButtons)
            {
                Parent.LayerToolbar.Items.Insert(viewIndex, layer);
            }


            int editIndex = Parent.LayerToolbar.Items.IndexOf(Parent.EditLayerListStart) + 1;

            // EDIT LAYERS
            foreach (var layer in EditLayerButtons)
            {
                Parent.LayerToolbar.Items.Insert(editIndex, layer);
            }
        }


        private EditLayerToggleButton GenerateLayerToggleButton(EditorLayer layer, bool viewMode = false)
        {
            if (viewMode)
            {
                EditLayerToggleButton button = new EditLayerToggleButton();
                button.DualSelect = false;
                button.Text = layer.Name;
                button.LayerName = layer.Name;
                button.TextForeground = System.Windows.Media.Brushes.White;
                button.IsEnabled = true;
                return button;
            }
            else
            {
                EditLayerToggleButton button = new EditLayerToggleButton();
                button.DualSelect = false;
                button.Text = layer.Name;
                button.LayerName = layer.Name;
                button.TextForeground = System.Windows.Media.Brushes.Red;
                button.Click += LayerEditButton_Click;
                button.IsEnabled = true;
                return button;
            }
        }

        private void LayerEditButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if ((sender as EditLayerToggleButton).IsCheckedN.Value)
            {
                foreach (var item in EditLayerButtons)
                {
                    if (!item.Equals((sender as EditLayerToggleButton))) item.IsCheckedN = false;
                }
                if (EditLayer != null) CloseEditLayer();
                EditLayer = this.Layers.Where(x => x.Name == (sender as EditLayerToggleButton).LayerName).FirstOrDefault();
            }
            else
            {
                if (EditLayer != null) CloseEditLayer();
                EditLayer = null;
            }
        }

        private void CloseEditLayer()
        {
            var self = this;
            Interfaces.Layer.Deselect(ref self, EditLayer);
        }

        #endregion

        public void RefreshChunks()
        {
            foreach (var layer in Layers)
            {
                layer.InvalidateChunks();
            }
        }

    }
}
