using System;
using System.Drawing;
using System.Windows.Forms;

namespace ManiacEDv2
{
    public partial class GraphicsHostControl : UserControl, IDrawArea
    {
        public DevicePanel GraphicPanel;
        private Editor Instance;

        private bool CtrlDown = false;


        public GraphicsHostControl(Editor instance)
        {
            Instance = instance;
            InitializeComponent();
            SetupGraphicsPanel();
        }

        public void SetupGraphicsPanel()
        {
            this.GraphicPanel = new DevicePanel();
            this.GraphicPanel.AllowDrop = true;
            this.GraphicPanel.AutoSize = true;
            this.GraphicPanel.DeviceBackColor = System.Drawing.Color.White;
            this.GraphicPanel.Location = new System.Drawing.Point(-1, 0);
            this.GraphicPanel.Margin = new System.Windows.Forms.Padding(0);
            this.GraphicPanel.Name = "GraphicPanel";
            this.GraphicPanel.Size = new System.Drawing.Size(643, 449);
            this.GraphicPanel.TabIndex = 10;
            this.viewPanel.Controls.Add(this.GraphicPanel);
            this.GraphicPanel.Width = SystemInformation.PrimaryMonitorSize.Width;
            this.GraphicPanel.Height = SystemInformation.PrimaryMonitorSize.Height;
            this.GraphicPanel.MouseWheel += GraphicPanel_MouseWheel;
            this.GraphicPanel.MouseMove += GraphicPanel_MouseMove;
            this.GraphicPanel.KeyDown += GraphicPanel_KeyDown;
            this.GraphicPanel.KeyUp += GraphicPanel_KeyUp;
            GraphicPanel.Init(this);
        }

        private void GraphicPanel_KeyUp(object sender, KeyEventArgs e)
        {
            if ((System.Windows.Forms.Control.ModifierKeys & Keys.Control) != 0) CtrlDown = true;
            else CtrlDown = false;
        }

        private void GraphicPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if ((System.Windows.Forms.Control.ModifierKeys & Keys.Control) != 0) CtrlDown = true;
            else CtrlDown = false;
        }

        private void GraphicPanel_MouseMove(object sender, MouseEventArgs e)
        {
            Instance.MousePosition = new System.Windows.Point((int)(e.X / Editor.Zoom), (int)(e.Y / Editor.Zoom));
            Instance.UpdatePositionLabel();
            GraphicPanel.Render();

        }

        private void GraphicPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            Instance.Scroll(e.Delta, new Point(0,0));
        }

        public double GetZoom()
        {
            return Editor.Zoom;
        }

        public new void Dispose()
        {
            this.GraphicPanel.Dispose();
            this.GraphicPanel = null;
            this.Controls.Clear();
            base.Dispose(true);
        }

        public Rectangle GetScreen()
        {
            //if (Settings.MySettings.EntityFreeCam) return new Rectangle(EditorInstance.StateModel.CustomX, EditorInstance.StateModel.CustomY, (int)EditorInstance.ViewPanelForm.ActualWidth, (int)EditorInstance.ViewPanelForm.ActualHeight);
            /*else*/ return new Rectangle((int)Instance.HScrollBar.Value, (int)Instance.VScrollBar.Value, (int)Instance.ViewPanel.ActualWidth, (int)Instance.ViewPanel.ActualHeight);
        }

        public void DisposeTextures()
        {
            // Make sure to dispose the textures of the extra layers too

            if (Instance.EditorScene != null && Instance.EditorScene.Layers != null)
            {
                foreach (var el in Instance.EditorScene.Layers)
                {
                    el.DisposeAllChunks();
                }
            }

        }

        private void EditorView_Load(object sender, EventArgs e)
        {

        }
    }
}
