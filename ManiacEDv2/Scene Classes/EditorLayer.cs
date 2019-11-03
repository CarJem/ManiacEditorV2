using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSDKv5;
using SharpDX.Direct3D9;
using System.Drawing;
using System.Drawing.Imaging;

namespace ManiacEDv2
{
    public class EditorLayer
    {
        private SceneLayer Layer;
        private EditorScene Parent;
        private ChunkVBO[][] ChunkMap;
        public string Name { get => Layer.Name; }

        public ushort Width { get => Layer.Width; }
        public ushort Height { get => Layer.Height; }

        public int LayerWidth { get => ChunksWidth * TILE_SIZE * TILES_CHUNK_SIZE; }
        public int LayerHeight { get => ChunksHeight * TILE_SIZE * TILES_CHUNK_SIZE; }

        public int ChunksWidth { get; set; }
        public int ChunksHeight { get; set; }

        const int TILES_CHUNK_SIZE = 16;

        const int TILE_SIZE = 16;

        public EditorLayer(SceneLayer _layer, ref EditorScene parent)
        {
            Layer = _layer;
            Parent = parent;
            InitiallizeChunkMap();
        }

        private void InitiallizeChunkMap()
        {
            ChunksWidth = DivideRoundUp(Width, TILES_CHUNK_SIZE);
            ChunksHeight = DivideRoundUp(Height, TILES_CHUNK_SIZE);

            ChunkMap = new ChunkVBO[ChunksHeight][];
            for (int i = 0; i < ChunkMap.Length; ++i)
            {
                ChunkMap[i] = new ChunkVBO[ChunksWidth];
            }

        }

        static int DivideRoundUp(int number, int by)
        {
            return (number + by - 1) / by;
        }


        public void DrawLayer(DevicePanel d)
        {
            Rectangle screen = d.GetScreen();
            int pos_x = screen.X;
            int pos_y = screen.Y;
            int width = screen.Width;
            int height = screen.Height;

            if (pos_x >= 0 && pos_y >= 0 && width >= 0 && height >= 0)
            {
                int start_x = pos_x / (TILES_CHUNK_SIZE * TILE_SIZE);
                int end_x = Math.Min(DivideRoundUp(pos_x + width, TILES_CHUNK_SIZE * TILE_SIZE), ChunkMap[0].Length);
                int start_y = pos_y / (TILES_CHUNK_SIZE * TILE_SIZE);
                int end_y = Math.Min(DivideRoundUp(pos_y + height, TILES_CHUNK_SIZE * TILE_SIZE), ChunkMap.Length);

                for (int y = start_y; y < end_y; ++y)
                {
                    for (int x = start_x; x < end_x; ++x)
                    {
                        if (d.IsObjectOnScreen(x * 256, y * 256, 256, 256))
                        {
                            Rectangle rect = GetTilesChunkArea(x, y);
                            d.DrawBitmap(GetChunk(d, x, y), rect.X * TILE_SIZE, rect.Y * TILE_SIZE, rect.Width * TILE_SIZE, rect.Height * TILE_SIZE, false, 0xFF);
                        }
                        else DisposeChunk(x, y);
                    }
                }
                DisposeUnusedChunks();
            }


        }

        public void DisposeAllChunks()
        {
            for (int y = 0; y < ChunksHeight; y++)
            {
                for (int x = 0; x < ChunksWidth; x++)
                {
                    if (ChunkMap[y][x] != null)
                    {
                        ChunkMap[y][x].Dispose();
                        ChunkMap[y][x] = null;
                    }
                }
            }
        }


        private void DisposeUnusedChunks()
        {
            for (int y = 0; y < ChunksHeight; y++)
            {
                for (int x = 0; x < ChunksWidth; x++)
                {
                    if (ChunkMap[y][x] != null && ChunkMap[y][x].HasBeenRendered)
                    {
                        ChunkMap[y][x].HasBeenRendered = false;
                    }
                    else if (ChunkMap[y][x] != null)
                    {
                        ChunkMap[y][x].Dispose();
                        ChunkMap[y][x] = null;
                    }
                }
            }
        }

        private void DisposeChunk(int x, int y)
        {
            if (ChunkMap[y][x] != null)
            {
                ChunkMap[y][x].Dispose();
                ChunkMap[y][x] = null;
            }
        }

        private Rectangle GetTilesChunkArea(int x, int y)
        {
            int y_start = y * TILES_CHUNK_SIZE;
            int y_end = Math.Min((y + 1) * TILES_CHUNK_SIZE, Height);

            int x_start = x * TILES_CHUNK_SIZE;
            int x_end = Math.Min((x + 1) * TILES_CHUNK_SIZE, Width);


            return new Rectangle(x_start, y_start, x_end - x_start, y_end - y_start);
        }


        public Texture GetChunk(DevicePanel d, int x, int y)
        {
            bool isSelected = isChunkSelected(x, y);
            if (ChunkMap[y][x] != null && ChunkMap[y][x].IsReady && !ChunkMap[y][x].HasBeenSelectedPrior && !isSelected)
            {
                ChunkMap[y][x].HasBeenRendered = true;
                return ChunkMap[y][x].Texture;
            }
            else
            {
                if (ChunkMap[y][x] != null && ChunkMap[y][x].HasBeenSelectedPrior) DisposeChunk(x, y);
                else if (ChunkMap[y][x] != null) DisposeChunk(x, y);
                Rectangle rect = GetTilesChunkArea(x, y);

                Bitmap bmp2 = new Bitmap(rect.Width * TILE_SIZE, rect.Height * TILE_SIZE, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var squareSize = (bmp2.Width > bmp2.Height ? bmp2.Width : bmp2.Height);
                int factor = 32;
                int newSize = (int)Math.Round((squareSize / (double)factor), MidpointRounding.AwayFromZero) * factor;
                if (newSize == 0) newSize = factor;
                while (newSize < squareSize) newSize += factor;

                Bitmap bmp = new Bitmap(newSize, newSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                bool hasBeenSelected = false;

                using (bmp)
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        for (int ty = rect.Y; ty < rect.Y + rect.Height; ++ty)
                        {
                            for (int tx = rect.X; tx < rect.X + rect.Width; ++tx)
                            {
                                Point point = new Point(tx, ty);
                                bool tileSelected = isTileSelected(tx, ty);
                                DrawTile(g, Layer.Tiles[ty][tx], tx - rect.X, ty - rect.Y, tileSelected);
                                if (tileSelected) hasBeenSelected = true;

                            }
                        }
                    }
                    ChunkMap[y][x] = new ChunkVBO();
                    ChunkMap[y][x].Texture = TextureCreator.FromBitmap(d._device, bmp);
                    ChunkMap[y][x].IsReady = true;
                    ChunkMap[y][x].HasBeenRendered = true;
                    ChunkMap[y][x].HasBeenSelectedPrior = hasBeenSelected;
                }

                bmp.Dispose();
                bmp2.Dispose();
                bmp = null;
                bmp2 = null;

                return ChunkMap[y][x].Texture;
            }



            
        }

        private bool isChunkSelected(int _x, int _y)
        {
            Rectangle rect = GetTilesChunkArea(_x, _y);

            int x = rect.X * TILE_SIZE;
            int y = rect.Y * TILE_SIZE;
            int x2 = rect.Right * TILE_SIZE;
            int y2 = rect.Bottom * TILE_SIZE;

            int mouse_x = (int)Parent.Parent.MousePosition.X;
            int mouse_y = (int)Parent.Parent.MousePosition.Y;

            if (mouse_x >= x && mouse_x <= x2 && mouse_y >= y && mouse_y <= y2)
            {
                //System.Diagnostics.Debug.Print(string.Format("Chunk {0},{1} Selected", _x, _y));
                return true;
            }
            else return false;

        }

        private bool isTileSelected(int _x, int _y)
        {
            int x = _x * TILE_SIZE;
            int y = _y * TILE_SIZE;
            int x2 = _x * TILE_SIZE + TILE_SIZE - 1;
            int y2 = _y * TILE_SIZE + TILE_SIZE - 1;

            int mouse_x = (int)Parent.Parent.MousePosition.X;
            int mouse_y = (int)Parent.Parent.MousePosition.Y;

            if (mouse_x >= x && mouse_x <= x2 && mouse_y >= y && mouse_y <= y2)
            {
                System.Diagnostics.Debug.Print(string.Format("Tile {0},{1} Selected", _x, _y));
                return true;
            }
            else return false;

        }

        public void DrawTile(Graphics g, ushort tile, int x, int y, bool isSelected = false)
        {
            if (tile != 0xffff)
            {
                ushort TileIndex = (ushort)(tile & 0x3ff);
                int TileIndexInt = (int)TileIndex;
                bool flipX = ((tile >> 10) & 1) == 1;
                bool flipY = ((tile >> 11) & 1) == 1;
                bool SolidTopA = ((tile >> 12) & 1) == 1;
                bool SolidLrbA = ((tile >> 13) & 1) == 1;
                bool SolidTopB = ((tile >> 14) & 1) == 1;
                bool SolidLrbB = ((tile >> 15) & 1) == 1;

                System.Drawing.Color AllSolid = System.Drawing.Color.White;
                System.Drawing.Color LRDSolid = System.Drawing.Color.Yellow;
                System.Drawing.Color TopOnlySolid = System.Drawing.Color.Red;

                g.DrawImage(Parent.EditorTiles.Image.GetBitmap(new Rectangle(0, TileIndex * TILE_SIZE, TILE_SIZE, TILE_SIZE), flipX, flipY), new Rectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE));

                if (Parent.Parent.ShowCollisionA)
                {
                    if (SolidLrbA || SolidTopA)
                    {
                        if (SolidTopA && SolidLrbA) DrawCollision(true, AllSolid, flipX, flipY);
                        if (SolidTopA && !SolidLrbA) DrawCollision(true, TopOnlySolid, flipX, flipY);
                        if (SolidLrbA && !SolidTopA) DrawCollision(true, LRDSolid, flipX, flipY);
                    }
                }
                if (Parent.Parent.ShowCollisionB)
                {
                    if (SolidLrbB || SolidTopB)
                    {
                        if (SolidTopB && SolidLrbB) DrawCollision(false, AllSolid, flipX, flipY);
                        if (SolidTopB && !SolidLrbB) DrawCollision(false, TopOnlySolid, flipX, flipY);
                        if (SolidLrbB && !SolidTopB) DrawCollision(false, LRDSolid, flipX, flipY);
                    }
                }
            }

            if (isSelected)
            {
                g.DrawRectangle(Pens.Red, new Rectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE - 1, TILE_SIZE - 1));
            }

            void DrawCollision(bool drawA, System.Drawing.Color colour, bool flipX, bool flipY)
            {
                //create some image attributes
                ImageAttributes attributes = new ImageAttributes();

                float[][] colourMatrixElements =
                {
                    new float[] { colour.R / 255.0f, 0, 0, 0, 0 },
                    new float[] { 0, colour.G / 255.0f, 0, 0, 0 },
                    new float[] { 0, 0, colour.B / 255.0f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                };

                //set the color matrix attribute
                attributes.SetColorMatrix(new ColorMatrix(colourMatrixElements));


                int _x = 0;
                int _y = 0;
                int _width = TILE_SIZE;
                int _height = TILE_SIZE;

                Rectangle dest = new Rectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE);

                if (drawA) g.DrawImage(Parent.EditorTiles.CollisionMaskA.GetBitmap(new Rectangle(0, (tile & 0x3ff) * TILE_SIZE, TILE_SIZE, TILE_SIZE), flipX, flipY), dest, _x, _y, _width, _height, GraphicsUnit.Pixel, attributes);
                else g.DrawImage(Parent.EditorTiles.CollisionMaskB.GetBitmap(new Rectangle(0, (tile & 0x3ff) * TILE_SIZE, TILE_SIZE, TILE_SIZE), flipX, flipY), dest, _x, _y, _width, _height, GraphicsUnit.Pixel, attributes);

                attributes.Dispose();
                attributes = null;

                colourMatrixElements = null;
            }


        }


        public class ChunkVBO
        {
            public bool IsReady = false;
            public SharpDX.Direct3D9.Texture Texture;
            public bool HasBeenRendered = false;
            public bool HasBeenSelectedPrior = false;

            public void Dispose()
            {
                if (this.Texture != null)
                {
                    this.Texture.Dispose();
                    this.Texture = null;
                }
                this.IsReady = false;
                this.HasBeenSelectedPrior = false;
            }
        }
    }
}
