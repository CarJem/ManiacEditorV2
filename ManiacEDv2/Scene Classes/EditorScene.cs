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

        public Scene Scene;
        public List<EditorLayer> Layers = new List<EditorLayer>();

        public EditorLayer LowDetails
        {
            get => Layers.FirstOrDefault(el => el.Name.Replace("\0", "").Equals("FG Lower") || el.Name.Replace("\0", "").Equals("Low Details") || el.Name.Replace("\0", "").Equals("FG Supa Low"));
        }
        public EditorLayer ForegroundLow
        {
            get => Layers.FirstOrDefault(el => el.Name.Replace("\0", "").Equals("FG Low") || el.Name.Replace("\0", "").Equals("Playfield"));
        }

        public EditorLayer Scratch
        {
            get => Layers.FirstOrDefault(el => el.Name.Replace("\0", "").Equals("Scratch"));
        }

        public EditorLayer Move
        {
            get => Layers.FirstOrDefault(el => el.Name.Replace("\0", "").Equals("Move"));
        }


        public EditorLayer HighDetails
        {
            get => Layers.FirstOrDefault(el => el.Name.Replace("\0", "").Equals("FG Higher") || el.Name.Replace("\0", "").Equals("High Details") || el.Name.Replace("\0", "").Equals("FG Overlay") || el.Name.Replace("\0", "").Equals("FG Supa High"));
        }


        public EditorLayer ForegroundHigh
        {
            get => Layers.FirstOrDefault(el => el.Name.Replace("\0", "").Equals("FG High") || el.Name.Replace("\0", "").Equals("Ring Count"));
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
        }

        public void RefreshChunks()
        {
            foreach (var layer in Layers)
            {
                layer.DisposeAllChunks();
            }
        }

    }
}
