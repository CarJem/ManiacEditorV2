using System;
using System.Drawing;
using RSDKv5Color = RSDKv5.Color;

namespace ManiacEDv2
{
    public class EditorBackground : IDrawable
    {

        RSDKv5Color BGColor1 { get => GetColor(true); }
        RSDKv5Color BGColor2 { get => GetColor(false); }

        public const int ENTITY_NAME_BOX_WIDTH = 20;
        public const int ENTITY_NAME_BOX_HEIGHT = 20;
        public const int ENTITY_NAME_BOX_HALF_WIDTH = ENTITY_NAME_BOX_WIDTH / 2;
        public const int ENTITY_NAME_BOX_HALF_HEIGHT = ENTITY_NAME_BOX_HEIGHT / 2;
        public const int TILES_CHUNK_SIZE = 16;
        public const int TILE_SIZE = 16;
        public const int BOX_SIZE = 8;
        public const int TILE_BOX_SIZE = 1;
        public const int x128_CHUNK_SIZE = 128;

        public Editor Parent;

        public EditorBackground(Editor _parent)
        {
            Parent = _parent;
        }


        static int DivideRoundUp(int number, int by)
        {
            return (number + by - 1) / by;
        }


        private RSDKv5Color GetColor(bool primaryColor = true)
        {
            if (Parent != null && Parent.EditorScene != null)
            {
                if (primaryColor) return Parent.EditorScene.Scene.EditorMetadata.BackgroundColor1;
                else return Parent.EditorScene.Scene.EditorMetadata.BackgroundColor2;
            }
            else return RSDKv5Color.EMPTY;
        }

        public void Draw(Graphics g)
        {

        }


        public static void SetBackgroundColors(EditorScene scene)
        {
            
        }

        public void Draw(DevicePanel d)
        {
            Rectangle screen = d.GetScreen();

            Color color1 = Color.FromArgb(BGColor1.A, BGColor1.R, BGColor1.G, BGColor1.B);
            Color color2 = Color.FromArgb(BGColor2.A, BGColor2.R, BGColor2.G, BGColor2.B);

            int start_x = screen.X / (BOX_SIZE * TILE_SIZE);
            int end_x = Math.Min(DivideRoundUp(screen.X + screen.Width, BOX_SIZE * TILE_SIZE), Parent.BackgroundWidth);
            int start_y = screen.Y / (BOX_SIZE * TILE_SIZE);
            int end_y = Math.Min(DivideRoundUp(screen.Y + screen.Height, BOX_SIZE * TILE_SIZE), Parent.BackgroundHeight);

            // Draw with first color everything
            d.DrawRectangle(screen.X, screen.Y, screen.X + screen.Width, screen.Y + screen.Height, color1);

            if (color2.A != 0)
            {
                for (int y = start_y; y < end_y; ++y)
                {
                    for (int x = start_x; x < end_x; ++x)
                    {
                        if ((x + y) % 2 == 1) d.DrawRectangle(x * BOX_SIZE * TILE_SIZE, y * BOX_SIZE * TILE_SIZE, (x + 1) * BOX_SIZE * TILE_SIZE, (y + 1) * BOX_SIZE * TILE_SIZE, color2);
                    }
                }
            }
        }

        public void DrawEdit(DevicePanel d)
        {
            Rectangle screen = d.GetScreen();

            Color color1 = Color.FromArgb(30, BGColor1.R, BGColor1.G, BGColor1.B);
            Color color2 = Color.FromArgb(30, BGColor2.R, BGColor2.G, BGColor2.B);

            int start_x = screen.X / (BOX_SIZE * TILE_SIZE);
            int end_x = Math.Min(DivideRoundUp(screen.X + screen.Width, BOX_SIZE * TILE_SIZE), Parent.BackgroundWidth);
            int start_y = screen.Y / (BOX_SIZE * TILE_SIZE);
            int end_y = Math.Min(DivideRoundUp(screen.Y + screen.Height, BOX_SIZE * TILE_SIZE), Parent.BackgroundHeight);

            // Draw with first color everything
            d.DrawRectangle(screen.X, screen.Y, screen.X + screen.Width, screen.Y + screen.Height, color1);

            if (color2.A != 0)
            {
                for (int y = start_y; y < end_y; ++y)
                {
                    for (int x = start_x; x < end_x; ++x)
                    {
                        if ((x + y) % 2 == 1) d.DrawRectangle(x * BOX_SIZE * TILE_SIZE, y * BOX_SIZE * TILE_SIZE, (x + 1) * BOX_SIZE * TILE_SIZE, (y + 1) * BOX_SIZE * TILE_SIZE, color2);
                    }
                }
            }
        }

        public void DrawGrid(DevicePanel d)
        {
            int GridSize = 16;
            Rectangle screen = d.GetScreen();

            Color GridColor = Color.FromArgb(0,0,0);

            int start_x = screen.X / (TILE_BOX_SIZE * GridSize);
            int end_x = Math.Min(DivideRoundUp(screen.X + screen.Width, TILE_BOX_SIZE * GridSize), Parent.BackgroundWidth);
            int start_y = screen.Y / (TILE_BOX_SIZE * GridSize);
            int end_y = Math.Min(DivideRoundUp(screen.Y + screen.Height, TILE_BOX_SIZE * GridSize), Parent.BackgroundHeight);


            for (int y = start_y; y < end_y; ++y)
            {
                for (int x = start_x; x < end_x; ++x)
                {
                    d.DrawLine(x * GridSize, y * GridSize, x * GridSize + GridSize, y * GridSize, GridColor);
                    d.DrawLine(x * GridSize, y * GridSize, x * GridSize, y * GridSize + GridSize, GridColor);
                    d.DrawLine(x * GridSize + GridSize, y * GridSize + GridSize, x * GridSize + GridSize, y * GridSize, GridColor);
                    d.DrawLine(x * GridSize + GridSize, y * GridSize + GridSize, x * GridSize, y * GridSize + GridSize, GridColor);
                }
            }
        }


    }
}
