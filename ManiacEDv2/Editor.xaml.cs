﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RSDKv5;
using System.Windows.Forms;
using SharpDX;
using System.Windows.Forms.Integration;
using System.Diagnostics;
using Binding = System.Windows.Data.Binding;


namespace ManiacEDv2
{
    /// <summary>
    /// Interaction logic for Editor.xaml
    /// </summary>
    public partial class Editor : Window
    {

        public System.Windows.Point MousePosition = new System.Windows.Point(0, 0);

        public EditorScene EditorScene;
        public EditorBackground EditorBackground;


        public PerformanceCounter ramCounter;
        //public PerformanceCounter cpuCounter;

        //private double CPU;
        private double RAM;
        //private double CPU_Max = 0;
        private double RAM_Max = 0;



        public GraphicsHostControl GraphicsHostControl;
        public WindowsFormsHost GraphicsFormHost;


        internal int BackgroundWidth => (EditorScene != null ? EditorScene.Layers.Max(sl => sl.Width) * 16 : 0);
        internal int BackgroundHeight => (EditorScene != null ? EditorScene.Layers.Max(sl => sl.Height) * 16 : 0);

        public int ZoomLevel { get; set; }
        public static double Zoom { get; set; } = 1;

        public bool ShowCollisionA = false;
        public bool ShowCollisionB = false;

        public DevicePanel DevicePanel { get => GraphicsHostControl.GraphicPanel; set => GraphicsHostControl.GraphicPanel = value; }

        public bool isScrollY = true;

        public Editor()
        {
            InitializeComponent();
            SetupRenderingSystem();
            SetupTimer();
            UpdateUI();
        }

        #region Initialization
        public void Run()
        {
            Show();
            Focus();
            GraphicsHostControl.Show();
            DevicePanel.Run();
        }

        public void SetupTimer()
        {
            var p = Process.GetCurrentProcess();
            ramCounter = new PerformanceCounter("Process", "Working Set", p.ProcessName);
            //cpuCounter = new PerformanceCounter("Process", "% Processor Time", p.ProcessName);


            //Initialize a timer
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            //Start the Timer
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            RAM = ramCounter.NextValue();
            //CPU = cpuCounter.NextValue();

            double ram_optimized = (RAM / 1024 / 1024);
            //double cpu_optimized = (CPU);

            //if (CPU > CPU_Max) CPU_Max = CPU;
            if (RAM > RAM_Max) RAM_Max = RAM;

            double ram_max_optimized = (RAM_Max / 1024 / 1024);
            //double cpu_max_optimized = (CPU_Max);

            _baseDataDirectoryLabel.Content = string.Format("Current Usage: {0} MB; Maximum Usage: {1} MB;", (int)ram_optimized, (int)ram_max_optimized);
        }

        private void SetupRenderingSystem()
        {
            // Create the interop host control.
            GraphicsFormHost = new WindowsFormsHost();

            // Create the GraphicsHostControl control.
            GraphicsHostControl = new GraphicsHostControl(this);
            GraphicsHostControl.Dock = DockStyle.Fill;

            // Assign the GraphicsHostControl control as the host control's child.
            GraphicsFormHost.Child = GraphicsHostControl;

            // Add the interop host control to the Grid
            // control's collection of child controls.
            this.ViewPanel.Children.Add(GraphicsFormHost);

            VScrollBar.Value = 0;
            HScrollBar.Value = 0;

            GraphicsHostControl.GraphicPanel.OnRender += OnRender;
        }

        #endregion

        private void GetSceneFiles(string scene)
        {
            EditorBackground = new EditorBackground(this);
            EditorScene = new EditorScene(scene, this);
        }


        public void OnRender(object sender, DeviceEventArgs e)
        {
            if (EditorScene != null)
            {
                if (EditorBackground != null) EditorBackground.Draw(DevicePanel);
                if (EditorScene.LowDetails != null) EditorScene.LowDetails.DrawLayer(DevicePanel);
                if (EditorScene.ForegroundLow != null) EditorScene.ForegroundLow.DrawLayer(DevicePanel);
                if (EditorScene.ForegroundHigh != null) EditorScene.ForegroundHigh.DrawLayer(DevicePanel);
                if (EditorScene.HighDetails != null) EditorScene.HighDetails.DrawLayer(DevicePanel);
            }

        }


        #region Scene Controls
        private void UpdateScrollView()
        {
            if (this.VScrollBar.ActualHeight != double.NaN && this.VScrollBar.ActualWidth != double.NaN)
            {
                this.VScrollBar.LargeChange = this.VScrollBar.ActualHeight;
                this.VScrollBar.SmallChange = this.VScrollBar.ActualHeight / 8;

                this.HScrollBar.LargeChange = this.HScrollBar.ActualWidth;
                this.HScrollBar.SmallChange = this.HScrollBar.ActualWidth / 8;
            }


            if (this.VScrollBar.Track.ViewportSize != this.BackgroundHeight) this.VScrollBar.Track.ViewportSize = this.BackgroundHeight;
            if (this.HScrollBar.Track.ViewportSize != this.BackgroundWidth) this.HScrollBar.Track.ViewportSize = this.BackgroundWidth;
            if (this.EditorScene != null) VScrollBar.Maximum = (EditorScene.Layers.Max(r => r.Height) * 16 * Zoom) - GraphicsFormHost.ActualHeight;
            if (this.EditorScene != null) HScrollBar.Maximum = (EditorScene.Layers.Max(r => r.Width) * 16 * Zoom) - GraphicsFormHost.ActualWidth;
            DevicePanel.Render();
        }

        private void VScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            isScrollY = true;
            UpdateScrollView();

        }

        private void HScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            isScrollY = false;
            UpdateScrollView();
        }

        private void DevicePanel_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Scroll(e.Delta, new System.Drawing.Point(0,0));
        }

        public void UpdatePositionLabel()
        {
            positionLabel.Content = string.Format("X: {0}, Y:{1}, Zoom Level: {2}", MousePosition.X, MousePosition.Y, ZoomLevel);
        }

        private void UpdateZoomLevel(int zoom_level, System.Drawing.Point zoom_point)
        {  
            if (zoom_level > 5) zoom_level = 5;
            else if (zoom_level < -5) zoom_level = -5;

            ZoomLevel = zoom_level;

            double old_zoom = Zoom;

            switch (ZoomLevel)
            {
                case 5: Zoom = 4; break;
                case 4: Zoom = 3; break;
                case 3: Zoom = 2; break;
                case 2: Zoom = 3 / 2.0; break;
                case 1: Zoom = 5 / 4.0; break;
                case 0: Zoom = 1; break;
                case -1: Zoom = 2 / 3.0; break;
                case -2: Zoom = 1 / 2.0; break;
                case -3: Zoom = 1 / 3.0; break;
                case -4: Zoom = 1 / 4.0; break;
                case -5: Zoom = 1 / 8.0; break;
            }

            int oldShiftX = (int)this.HScrollBar.Value;
            int oldShiftY = (int)this.VScrollBar.Value;

            this.HScrollBar.Value = (int)((zoom_point.X + oldShiftX) / old_zoom * Zoom - zoom_point.X);
            this.HScrollBar.Value = (int)Math.Min((HScrollBar.Maximum), Math.Max(0, this.HScrollBar.Value));

            this.VScrollBar.Value = (int)((zoom_point.Y + oldShiftY) / old_zoom * Zoom - zoom_point.Y);
            this.VScrollBar.Value = (int)Math.Min((VScrollBar.Maximum), Math.Max(0, this.VScrollBar.Value));
        }

        public void Scroll(int Delta, System.Drawing.Point scroll_point)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (Delta > 0)
                {
                    UpdateZoomLevel(ZoomLevel + 1, scroll_point);
                }
                else if (Delta < 0)
                {
                    UpdateZoomLevel(ZoomLevel - 1, scroll_point);
                }

                UpdatePositionLabel();
                UpdateScrollView();
            }
            else
            {
                if (Delta > 0)
                {
                    if (isScrollY) VScrollBar.Value = VScrollBar.Value - 16;
                    else HScrollBar.Value = HScrollBar.Value - 16;
                }
                else if (Delta < 0)
                {
                    if (isScrollY) VScrollBar.Value = VScrollBar.Value + 16;
                    else HScrollBar.Value = HScrollBar.Value + 16;
                }

                UpdateScrollView();
            }
        }

        public void ResizeGraphicPanel(int width = 0, int height = 0)
        {
            DevicePanel.Width = width;
            DevicePanel.Height = height;

            DevicePanel.ResetDevice();

            DevicePanel.DrawWidth = Math.Min((int)HScrollBar.Maximum, DevicePanel.Width);
            DevicePanel.DrawHeight = Math.Min((int)VScrollBar.Maximum, DevicePanel.Height);

            UpdateScrollView();
        }

        private void EditorViewWPF_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeGraphicPanel(DevicePanel.Width, DevicePanel.Height);
        }

        #endregion

        private void EditorViewWPF_Closed(object sender, EventArgs e)
        {
            GraphicsHostControl.Dispose();
            GraphicsFormHost.Child.Dispose();
            System.Windows.Application.Current.Shutdown();
        }


        #region UI Updating Methods

        private void UpdateUI()
        {
            editToolStripMenuItem.IsEnabled = false;
            viewToolStripMenuItem.IsEnabled = false;
            toolsToolStripMenuItem1.IsEnabled = false;
            sceneToolStripMenuItem.IsEnabled = false;
            sceneToolStripMenuItem1.IsEnabled = false;
            toolsToolStripMenuItem.IsEnabled = false;
            helpToolStripMenuItem.IsEnabled = false;

            LayerToolbar.IsEnabled = false;
            foreach (var control in MainToolbarButtons.Items)
            {
                if (control is System.Windows.Controls.Button)
                {
                    (control as System.Windows.Controls.Button).IsEnabled = false;
                }
                if (control is System.Windows.Controls.Primitives.ToggleButton && !(control.Equals(ShowCollisionAButton) || control.Equals(ShowCollisionBButton)))
                {
                    (control as System.Windows.Controls.Primitives.ToggleButton).IsEnabled = false;
                }

            }
            LeftToolbarToolbox.IsEnabled = false;
            ToolbarLeft.Width = new GridLength(15);

            StatusBar1.IsEnabled = false;
            StatusBar2.IsEnabled = false;
        }

        private void EnterDemoMode()
        {
            OpenMenuItem.IsEnabled = false;
        }


        #endregion


        #region File Tab Menu Items

        private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string scene = @"D:\Users\CarJem\Documents\Mania Modding\mods\LHPZ - Dev\Data\Stages\HPZ\Scene1.bin";
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "Scene File | *.bin",
                InitialDirectory = System.IO.Path.GetDirectoryName(scene)
            };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GetSceneFiles(ofd.FileName);
                EnterDemoMode();
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Tile Collision Methods

        private void UpdateCollisionProprtiesAndControls(bool isPathA = true)
        {
            if (isPathA)
            {
                if (ShowCollisionAButton.IsChecked.Value) ShowCollisionA = true;
                else ShowCollisionA = false;

                if (EditorScene != null) EditorScene.RefreshChunks();
            }
            else
            {
                if (ShowCollisionBButton.IsChecked.Value) ShowCollisionB = true;
                else ShowCollisionB = false;

                if (EditorScene != null) EditorScene.RefreshChunks();
            }
        }

        private void ShowCollisionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender.Equals(ShowCollisionAButton))
            {
                UpdateCollisionProprtiesAndControls(true);
            }
            else
            {
                UpdateCollisionProprtiesAndControls(false);
            }

        }

        #endregion
    }


}
