using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RSDKv5;
using SharpDX.Direct3D9;
using System.Drawing;
using System.Drawing.Imaging;
using Point = System.Drawing.Point;
using ManiacEDv2.Actions;

namespace ManiacEDv2
{
    public class EditorLayer
    {
        #region Variables
        private SceneLayer Layer;
        private EditorScene Parent;
        private ChunkVBO[][] ChunkMap;

        public PointsMap SelectedTiles;
        public PointsMap TempSelectionTiles;
        public PointsMap TempSelectionDeselectTiles;

        public List<IAction> Actions = new List<IAction>();

        #region Layer Properties
        public string Name { get => Layer.Name; }
        public ushort Width { get => Layer.Width; }
        public ushort Height { get => Layer.Height; }
        public int LayerWidth { get => ChunksWidth * TILE_SIZE * TILES_CHUNK_SIZE; }
        public int LayerHeight { get => ChunksHeight * TILE_SIZE * TILES_CHUNK_SIZE; }
        public int ChunksWidth { get; set; }
        public int ChunksHeight { get; set; }

        public byte DrawingOrder { get => Layer.DrawingOrder; }
        #endregion

        #region Constants
        const int TILES_CHUNK_SIZE = 16;
        const int TILE_SIZE = 16;
        #endregion
        #endregion

        public EditorLayer(SceneLayer _layer, ref EditorScene parent)
        {
            Layer = _layer;
            Parent = parent;


            InitiallizeSelectionMaps();
            InitiallizeChunkMap();
        }

        #region Initilization
        private void InitiallizeSelectionMaps()
        {
            SelectedTiles = new PointsMap(Width, Height);
            TempSelectionTiles = new PointsMap(Width, Height);
            TempSelectionDeselectTiles = new PointsMap(Width, Height);
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
        #endregion

        #region Dragging

        public bool FirstDrag;
        public bool Dragging;
        public bool SelectionMoved;
        public bool isDragOver;

        public void StartDrag()
        {
            FirstDrag = true;
            Dragging = true;
            InvalidateChunks();
        }
        public void EndDrag()
        {
            Dragging = false;
            InvalidateChunks();
        }
        public void PreDragOver(Point point, ushort value)
        {
            Deselect();
            isDragOver = true;
            DragOver(point, value);
            InvalidateChunks();
            
        }
        public void DragOver(Point point, ushort value)
        {
            SelectedTiles.Clear();
            SelectedTiles.Values.Clear();
            point = new Point(point.X / TILE_SIZE, point.Y / TILE_SIZE);
            SelectedTiles.Add(new Tile(point, Layer.Tiles[point.Y][point.X]));
            SelectedTiles.Values[point] = value;
            InvalidateChunks();
            
        }
        public void PostDragOver(bool remove)
        {
            if (isDragOver)
            {
                if (remove)
                {
                    SelectedTiles.Clear();
                    SelectedTiles.Values.Clear();
                    
                }
                isDragOver = false;
                InvalidateChunks();
                
            }
        }
        #endregion

        #region Manipulate Selected
        public void MoveSelectedQuonta(Point change)
        {
            MoveSelected(Point.Empty, new Point(change.X * TILE_SIZE, change.Y * TILE_SIZE), false);
        }
        public void DeleteSelected()
        {
            bool removedSomething = SelectedTiles.Count > 0;
            foreach (Point p in SelectedTiles.PopAll())
            {
                // Remove only tiles that not moved, because we already removed the moved tiles
                if (!SelectedTiles.Values.ContainsKey(p))
                {
                    RemoveTile(p);
                }
            }
            if (removedSomething)
            {
                Actions.Add(new ActionsGroupCloseMarker());
            }

            SelectedTiles.Values.Clear();
            

        }
        private void DetachSelected()
        {
            foreach (Tile point in SelectedTiles.GetAllTiles())
            {
                if (!SelectedTiles.Values.ContainsKey(point.Point))
                {
                    // Not moved yet
                    SelectedTiles.Values[point.Point] = Layer.Tiles[point.Y][point.X];
                    RemoveTile(point.Point);
                    InvalidateChunks();
                    
                }
            }
        }
        public void MoveSelected(Point oldPos, Point newPos, bool duplicate, bool chunkAlign = false)
        {
            oldPos = new Point(oldPos.X / TILE_SIZE, oldPos.Y / TILE_SIZE);
            newPos = new Point(newPos.X / TILE_SIZE, newPos.Y / TILE_SIZE);
            if (oldPos != newPos)
            {
                if (FirstDrag) FirstDragMove();
                else Standard();
            }

            void FirstDragMove()
            {
                FirstDrag = false;
                List<Tile> newPoints = new List<Tile>(SelectedTiles.Count);
                foreach (Tile tile in SelectedTiles.PopAllTiles())
                {
                    Tile tileWithNewPosition = new Tile(new Point(tile.X + (newPos.X - oldPos.X), tile.Y + (newPos.Y - oldPos.Y)), tile.Value);
                    newPoints.Add(tileWithNewPosition);
                    if (!duplicate) RemoveTile(tile.Point);
                }
                if (duplicate)
                {
                    Deselect();
                    // Create new actions group
                    Actions.Add(new ActionDummy());
                    
                }
                SelectionMoved = true;
                SelectedTiles.Values.Clear();
                SelectedTiles.AddPoints(newPoints);
                InvalidateChunks();
                
            }

            void Standard()
            {
                List<Tile> newPoints = new List<Tile>(SelectedTiles.Count);
                foreach (Tile point in SelectedTiles.PopAllTiles())
                {
                    Tile newPoint = new Tile(new Point(point.X + (newPos.X - oldPos.X), point.Y + (newPos.Y - oldPos.Y)), point.Value);
                    newPoints.Add(newPoint);
                }
                SelectedTiles.Values.Clear();
                SelectedTiles.AddPoints(newPoints);
                InvalidateChunks();
                
            }
        }
        public void FlipPropertySelected(FlipDirection direction, bool flipIndividually = false)
        {
            DetachSelected();
            List<Point> points = new List<Point>(SelectedTiles.Values.Keys);

            if (points.Count == 0) return;

            if (points.Count == 1 || flipIndividually)
            {
                FlipIndividualySelectedTiles(direction, points);
                return;
            }

            IEnumerable<int> monoCoordinates;

            if (direction == FlipDirection.Horizontal)
            {
                monoCoordinates = points.Select(p => p.X);
            }
            else
            {
                monoCoordinates = points.Select(p => p.Y);
            }

            int min = monoCoordinates.Min();
            int max = monoCoordinates.Max();
            int diff = max - min;

            if (diff == 0)
            {
                FlipIndividualySelectedTiles(direction, points);
            }
            else
            {
                FlipSelectedTilesGroup(direction, points, min, max);
            }
        }
        private void FlipIndividualySelectedTiles(FlipDirection direction, IEnumerable<Point> points)
        {
            foreach (Point point in points)
            {
                SelectedTiles.Values[point] ^= (ushort)direction;
            }
        }
        private void FlipSelectedTilesGroup(FlipDirection direction, IEnumerable<Point> points, int min, int max)
        {
            Dictionary<Point, ushort> workingTiles = new Dictionary<Point, ushort>();
            foreach (Point point in points)
            {
                ushort tileValue = SelectedTiles.Values[point];
                Point newPoint;

                if (direction == FlipDirection.Horizontal)
                {
                    int fromLeft = point.X - min;
                    int fromRight = max - point.X;

                    int newX = fromLeft < fromRight ? max - fromLeft : min + fromRight;
                    newPoint = new Point(newX, point.Y);
                }
                else
                {
                    int fromBottom = point.Y - min;
                    int fromTop = max - point.Y;

                    int newY = fromBottom < fromTop ? max - fromBottom : min + fromTop;
                    newPoint = new Point(point.X, newY);
                }

                workingTiles.Add(newPoint, tileValue ^= (ushort)direction);
            }

            SelectedTiles.Clear();
            SelectedTiles.Values.Clear();
            SelectedTiles.AddPoints(workingTiles.Select(wt => new Tile(wt.Key, wt.Value)).ToList());
        }
        public void SetPropertySelected(int bit, bool state)
        {
            DetachSelected();

            List<Point> points = new List<Point>(SelectedTiles.Values.Keys);
            foreach (Point point in points)
            {
                if (state)
                    SelectedTiles.Values[point] |= (ushort)(1 << bit);
                else
                    SelectedTiles.Values[point] &= (ushort)(~(1 << bit));
            }
        }

        #endregion

        #region Selection/Movement

        public void Select(Rectangle area, bool addSelection = false, bool deselectIfSelected = false)
        {
            if (!addSelection) Deselect();
            for (int y = Math.Max(area.Y / TILE_SIZE, 0); y < Math.Min(DivideRoundUp(area.Y + area.Height, TILE_SIZE), Layer.Height); ++y)
            {
                for (int x = Math.Max(area.X / TILE_SIZE, 0); x < Math.Min(DivideRoundUp(area.X + area.Width, TILE_SIZE), Layer.Width); ++x)
                {
                    if (addSelection || deselectIfSelected)
                    {
                        Point p = new Point(x, y);
                        if (SelectedTiles.Contains(p))
                        {
                            if (deselectIfSelected)
                            {
                                // Deselect
                                DeselectPoint(p);
                            }
                            // Don't add already selected tile, or if it was just deslected
                            continue;
                        }

                    }
                    if (Layer.Tiles[y][x] != 0xffff)
                    {
                        SelectedTiles.Add(new Tile(new Point(x, y), Layer.Tiles[y][x]));
                        
                    }
                    /*
                    else if (Layer.Tiles[y][x] == 0xffff && EditorInstance.UIModes.CopyAir)
                    {
                        SelectedTiles.Add(new Point(x, y));
                        RefreshTileCount();
                    }*/
                }
            }
            InvalidateChunks();
        }
        public void Select(Point point, bool addSelection = false, bool deselectIfSelected = false)
        {
            if (!addSelection) Deselect();
            point = new Point(point.X / TILE_SIZE, point.Y / TILE_SIZE);
            //EditorInstance.StateModel.SelectedTileX = point.X;
            //EditorInstance.StateModel.SelectedTileY = point.Y;
            if (point.X >= 0 && point.Y >= 0 && point.X < this.Layer.Tiles[0].Length && point.Y < this.Layer.Tiles.Length)
            {
                if (deselectIfSelected && SelectedTiles.Contains(point))
                {
                    // Deselect
                    DeselectPoint(point);
                    
                }
                else if (this.Layer.Tiles[point.Y][point.X] != 0xffff /*|| EditorInstance.UIModes.CopyAir*/)
                {
                    // Just add the point
                    SelectedTiles.Add(new Tile(point, this.Layer.Tiles[point.Y][point.X]));
                    
                }
            }
        }
        public void TempSelection(Rectangle area, bool deselectIfSelected)
        {
            ResetValues();
            bool TempSelectionDeselect = deselectIfSelected;
            for (int y = Math.Max(area.Y / TILE_SIZE, 0); y < Math.Min(DivideRoundUp(area.Y + area.Height, TILE_SIZE), Layer.Height); ++y)
            {
                for (int x = Math.Max(area.X / TILE_SIZE, 0); x < Math.Min(DivideRoundUp(area.X + area.Width, TILE_SIZE), Layer.Width); ++x)
                {
                    var tile = new Tile(new Point(x, y), Layer.Tiles[y][x]);
                    TempSelectionTiles.Add(tile);
                    if (SelectedTiles.Contains(new Point(x, y)))
                    {
                        if (SelectedTiles.Contains(tile.Point) && TempSelectionTiles.Contains(tile.Point))
                        {
                            TempSelectionDeselectTiles.Add(tile);
                        }
                    }
                }
            }
            InvalidateChunks();
        }
        public void SelectAll()
        {
            for (int y = 0; y < Layer.Tiles.Length; y += 1)
            {
                for (int x = 0; x < Layer.Tiles[y].Length; x += 1)
                {
                    if (Layer.Tiles[y][x] != 0xffff)
                    {
                        var point = new Point(x, y);
                        SelectedTiles.Add(new Tile(point, this.Layer.Tiles[point.Y][point.X]));
                        
                    }
                    /*
                    else if (Layer.Tiles[y][x] == 0xffff && EditorInstance.UIModes.CopyAir)
                    {
                        var point = new Point(x, y);
                        SelectedTiles.Add(new Tile(point, this.Layer.Tiles[point.Y][point.X]));
                        
                    }*/

                }
            }
            
        }
        public void Deselect()
        {
            bool addActions = SelectionMoved;
            bool hasTiles = SelectedTiles.Values.Count > 0;
            foreach (KeyValuePair<Point, ushort> point in SelectedTiles.Values)
            {
                // ignore out of bounds
                if (point.Key.X < 0 || point.Key.Y < 0 || point.Key.Y >= Layer.Height || point.Key.X >= Layer.Width) continue;
                SetTile(point.Key, point.Value, addActions);
            }
            if (hasTiles && addActions)
                Actions.Add(new ActionsGroupCloseMarker());


            SelectionMoved = false;
            SelectedTiles.Clear();
            SelectedTiles.Values.Clear();
        }

        public void ResetValues()
        {
            TempSelectionTiles.Clear();
            TempSelectionTiles.Values.Clear();
            TempSelectionDeselectTiles.Clear();
            TempSelectionDeselectTiles.Values.Clear();
        }
        public void EndTempSelection()
        {
            SelectedTiles.VerifyAll();
            TempSelectionTiles.Clear();
            TempSelectionTiles.Values.Clear();
            TempSelectionDeselectTiles.Clear();
            TempSelectionDeselectTiles.Values.Clear();
        }
        private void DeselectPoint(Point p)
        {
            if (SelectedTiles.Values.ContainsKey(p))
            {
                // Or else it wasn't moved at all
                SetTile(p, SelectedTiles.Values[p]);
                SelectedTiles.Values.Remove(p);
            }
            else SelectedTiles.Remove(p);

            if (TempSelectionTiles.Values.ContainsKey(p))
            {
                // Or else it wasn't moved at all
                SetTile(p, SelectedTiles.Values[p]);
                TempSelectionTiles.Values.Remove(p);
            }
            else TempSelectionTiles.Remove(p);

            if (TempSelectionDeselectTiles.Values.ContainsKey(p))
            {
                // Or else it wasn't moved at all
                SetTile(p, TempSelectionDeselectTiles.Values[p]);
                TempSelectionDeselectTiles.Values.Remove(p);
            }
            else TempSelectionDeselectTiles.Remove(p);

        }

        #endregion

        #region Drawing
        public void DrawLayer(DevicePanel d)
        {
            int Transperncy;

            if (Parent.EditLayer != null && (Parent.EditLayer != this))
                Transperncy = 0x32;
            /*else if (EditorInstance.EditEntities.IsCheckedAll && EditorInstance.EditLayerA == null && EditorInstance.UIModes.ApplyEditEntitiesTransparency)
                Transperncy = 0x32;*/
            else
                Transperncy = 0xFF;


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
                            d.DrawBitmap(GetChunk(d, x, y), rect.X * TILE_SIZE, rect.Y * TILE_SIZE, rect.Width * TILE_SIZE, rect.Height * TILE_SIZE, false, Transperncy);
                        }
                        else InvalidateChunk(x, y);
                    }
                }
                DisposeUnusedChunks();
            }


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
                if (ChunkMap[y][x] != null && ChunkMap[y][x].HasBeenSelectedPrior) InvalidateChunk(x, y);
                else if (ChunkMap[y][x] != null) InvalidateChunk(x, y);
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
                                ushort tile = GetTileToDraw(point);
                                bool tileSelected = (SelectedTiles.Values.ContainsKey(point) || TempSelectionTiles.Values.ContainsKey(point)) && !TempSelectionDeselectTiles.Values.ContainsKey(point);
                                if(tile != 0xFFFF) DrawTile(g, tile, tx - rect.X, ty - rect.Y, tileSelected);
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
        private Rectangle GetTilesChunkArea(int x, int y)
        {
            int y_start = y * TILES_CHUNK_SIZE;
            int y_end = Math.Min((y + 1) * TILES_CHUNK_SIZE, Height);

            int x_start = x * TILES_CHUNK_SIZE;
            int x_end = Math.Min((x + 1) * TILES_CHUNK_SIZE, Width);


            return new Rectangle(x_start, y_start, x_end - x_start, y_end - y_start);
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

                g.DrawImage(Parent.EditorTiles.Image.GetBitmap(new Rectangle(0, TileIndex * TILE_SIZE, TILE_SIZE, TILE_SIZE), flipX, flipY, isSelected), new Rectangle(x * TILE_SIZE, y * TILE_SIZE, TILE_SIZE, TILE_SIZE));

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
        private ushort GetTileToDraw(Point source)
        {
            if (SelectedTiles.Values.ContainsKey(source)) return SelectedTiles.Values[source];
            else if (TempSelectionTiles.Values.ContainsKey(source)) return TempSelectionTiles.Values[source];
            else return Layer.Tiles[source.Y][source.X];
        }


        #endregion

        #region Chunk Invalidation/Disposal
        private void InvalidateChunkFromTilePosition(Point point)
        {
            var chunkPoint = GetDrawingChunkCoordinates(point.X, point.Y);
            if (!(chunkPoint.X >= ChunksWidth || chunkPoint.Y >= ChunksHeight || chunkPoint.Y < 0 || chunkPoint.X < 0))
            {
                if (ChunkMap[chunkPoint.Y][chunkPoint.X] != null) ChunkMap[chunkPoint.Y][chunkPoint.X].HasBeenSelectedPrior = true;
            }

            Point GetDrawingChunkCoordinates(int x, int y)
            {
                Point ChunkCoordinate = new Point();
                if (x != 0) ChunkCoordinate.X = x / 16;
                else ChunkCoordinate.X = 0;
                if (y != 0) ChunkCoordinate.Y = y / 16;
                else ChunkCoordinate.Y = 0;

                return ChunkCoordinate;
            }
        }
        public void InvalidateChunks()
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
        private void InvalidateChunk(int x, int y)
        {
            if (ChunkMap[y][x] != null)
            {
                ChunkMap[y][x].Dispose();
                ChunkMap[y][x] = null;
            }
        }

        #endregion

        #region Custom Classes
        public class Tile
        {
            public Point Point;
            public int X { get => Point.X; set => Point.X = value; }
            public int Y { get => Point.Y; set => Point.Y = value; }

            public ushort Value;

            public bool HasBeenValidated = false;

            public Tile(Point _point, ushort _tile)
            {
                Point = _point;
                Value = _tile;
            }
        }

        public class PointsMap
        {
            HashSet<Tile>[][] PointsChunks;
            HashSet<Tile> OutOfBoundsPoints = new HashSet<Tile>();
            public Dictionary<Point, ushort> Values = new Dictionary<Point, ushort>();
            public int Count = 0;

            public PointsMap(int width, int height)
            {
                PointsChunks = new HashSet<Tile>[DivideRoundUp(height, TILES_CHUNK_SIZE)][];
                for (int i = 0; i < PointsChunks.Length; ++i)
                {
                    PointsChunks[i] = new HashSet<Tile>[DivideRoundUp(width, TILES_CHUNK_SIZE)];
                    for (int j = 0; j < PointsChunks[i].Length; ++j)
                        PointsChunks[i][j] = new HashSet<Tile>();
                }
            }

            public bool isOutOfBounds(Tile tile)
            {
                return OutOfBoundsPoints.Contains(tile);
            }

            public void Add(Tile Tile)
            {

                HashSet<Tile> h;
                if (Tile.Y < 0 || Tile.X < 0 || Tile.Y / TILES_CHUNK_SIZE >= PointsChunks.Length || Tile.X / TILES_CHUNK_SIZE >= PointsChunks[0].Length)
                    h = OutOfBoundsPoints;
                else
                    h = PointsChunks[Tile.Y / TILES_CHUNK_SIZE][Tile.X / TILES_CHUNK_SIZE];
                Count -= h.Count;
                h.Add(Tile);
                Count += h.Count;

                Values.Add(Tile.Point, Tile.Value);
            }

            public void Remove(Tile Tile)
            {
                HashSet<Tile> h;
                if (Tile.Y < 0 || Tile.X < 0 || Tile.Y / TILES_CHUNK_SIZE >= PointsChunks.Length || Tile.X / TILES_CHUNK_SIZE >= PointsChunks[0].Length)
                    h = OutOfBoundsPoints;
                else
                    h = PointsChunks[Tile.Y / TILES_CHUNK_SIZE][Tile.X / TILES_CHUNK_SIZE];
                Count -= h.Count;
                h.Remove(Tile);
                Count += h.Count;

                Values.Remove(Tile.Point);
            }

            public void Remove(Point Tile)
            {
                HashSet<Tile> h;
                if (Tile.Y < 0 || Tile.X < 0 || Tile.Y / TILES_CHUNK_SIZE >= PointsChunks.Length || Tile.X / TILES_CHUNK_SIZE >= PointsChunks[0].Length)
                    h = OutOfBoundsPoints;
                else
                    h = PointsChunks[Tile.Y / TILES_CHUNK_SIZE][Tile.X / TILES_CHUNK_SIZE];
                Count -= h.Count;
                h.RemoveWhere(x => x.Point == Tile);
                Count += h.Count;

                Values.Remove(Values.Where(x => x.Key == Tile).FirstOrDefault().Key);
            }

            public bool Contains(Tile Tile)
            {
                if (Tile.Y < 0 || Tile.X < 0 || Tile.Y / TILES_CHUNK_SIZE >= PointsChunks.Length || Tile.X / TILES_CHUNK_SIZE >= PointsChunks[0].Length)
                    return OutOfBoundsPoints.Any(x => x.Point == Tile.Point);
                else
                    return PointsChunks[Tile.Y / TILES_CHUNK_SIZE][Tile.X / TILES_CHUNK_SIZE].Any(x => x.Point == Tile.Point);
            }

            public Tile GetTile(Point point)
            {
                if (point.Y < 0 || point.X < 0 || point.Y / TILES_CHUNK_SIZE >= PointsChunks.Length || point.X / TILES_CHUNK_SIZE >= PointsChunks[0].Length)
                    return OutOfBoundsPoints.Where(x => x.Point == point).FirstOrDefault();
                else
                    return PointsChunks[point.Y / TILES_CHUNK_SIZE][point.X / TILES_CHUNK_SIZE].Where(x => x.Point == point).FirstOrDefault();
            }

            public bool Contains(Point point)
            {
                if (point.Y < 0 || point.X < 0 || point.Y / TILES_CHUNK_SIZE >= PointsChunks.Length || point.X / TILES_CHUNK_SIZE >= PointsChunks[0].Length)
                    return OutOfBoundsPoints.Any(x => x.Point == point);
                else
                    return PointsChunks[point.Y / TILES_CHUNK_SIZE][point.X / TILES_CHUNK_SIZE].Any(x => x.Point == point);
            }

            public bool IsChunkUsed(int x, int y)
            {
                return PointsChunks[y][x].Count > 0;
            }

            public void Clear()
            {
                for (int i = 0; i < PointsChunks.Length; ++i)
                    for (int j = 0; j < PointsChunks[i].Length; ++j)
                        PointsChunks[i][j].Clear();
                OutOfBoundsPoints.Clear();
                Count = 0;
            }

            public bool IsTileVerified(Point point)
            {
                if (point.Y < 0 || point.X < 0 || point.Y / TILES_CHUNK_SIZE >= PointsChunks.Length || point.X / TILES_CHUNK_SIZE >= PointsChunks[0].Length)
                    return OutOfBoundsPoints.Where(x => x.Point == point).FirstOrDefault().HasBeenValidated;
                else
                    return PointsChunks[point.Y / TILES_CHUNK_SIZE][point.X / TILES_CHUNK_SIZE].Where(x => x.Point == point).FirstOrDefault().HasBeenValidated;
            }

            public void VerifyAll()
            {
                for (int i = 0; i < PointsChunks.Length; ++i)
                    for (int j = 0; j < PointsChunks[i].Length; ++j)
                        for (int k = 0; k < PointsChunks[i][j].Count; ++k)
                            PointsChunks[i][j].ElementAt(k).HasBeenValidated = true;
            }

            public HashSet<Tile> GetChunkPoint(int x, int y)
            {
                return PointsChunks[y][x];
            }

            public List<Point> PopAll()
            {
                List<Point> points = GetAll();
                Clear();
                return points;
            }

            public List<Tile> PopAllTiles()
            {
                List<Tile> points = GetAllTiles();
                Clear();
                return points;
            }

            public List<Point> GetAll()
            {
                List<Point> points = new List<Point>(Count);
                for (int i = 0; i < PointsChunks.Length; ++i)
                    for (int j = 0; j < PointsChunks[i].Length; ++j)
                    {
                        points.AddRange(PointsChunks[i][j].Select(x => x.Point).ToList());
                    }

                points.AddRange(OutOfBoundsPoints.Select(x => x.Point).ToList());
                return points;
            }

            public List<Tile> GetAllTiles()
            {
                List<Tile> points = new List<Tile>(Count);
                for (int i = 0; i < PointsChunks.Length; ++i)
                    for (int j = 0; j < PointsChunks[i].Length; ++j)
                    {
                        points.AddRange(PointsChunks[i][j].ToList());
                    }

                points.AddRange(OutOfBoundsPoints.ToList());
                return points;
            }

            public void AddPoints(List<Tile> points)
            {
                points.ForEach(Tile => Add(Tile));
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

        #endregion

        #region Tile Manipulation

        private void SetTile(Point point, ushort value, bool addAction = true)
        {
            if (addAction)
                Actions.Add(new ActionChangeTile((x, y) => SetTile(x, y, false), point, Layer.Tiles[point.Y][point.X], value));
            Layer.Tiles[point.Y][point.X] = value;
            InvalidateChunk(point.X / TILES_CHUNK_SIZE, point.Y / TILES_CHUNK_SIZE);
        }
        private void RemoveTile(Point point)
        {
            SetTile(point, 0xffff);
            
        }

        #endregion

        #region Information Requests
        private ushort GetTile(Point point)
        {
            return Layer.Tiles[point.Y][point.X];
        }
        public bool IsTileAtPoint(Point zoomPoint)
        {
            Point point = new Point(zoomPoint.X / TILES_CHUNK_SIZE, zoomPoint.Y / TILES_CHUNK_SIZE);
            return Layer.Tiles[point.Y][point.X] != 0xffff;
        }
        private bool isChunkSelected(int _x, int _y)
        {
            Rectangle rect = GetTilesChunkArea(_x, _y);

            int x = rect.X * TILE_SIZE;
            int y = rect.Y * TILE_SIZE;
            int x2 = rect.Right * TILE_SIZE;
            int y2 = rect.Bottom * TILE_SIZE;

            int mouse_x = (int)Interfaces.Control.MousePosition.X;
            int mouse_y = (int)Interfaces.Control.MousePosition.Y;

            if (mouse_x >= x && mouse_x <= x2 && mouse_y >= y && mouse_y <= y2)
            {
                //System.Diagnostics.Debug.Print(string.Format("Chunk {0},{1} Selected", _x, _y));
                return true;
            }
            else return false;

        }
        public bool IsPointSelected(Point point)
        {
            return SelectedTiles.Contains(new Point(point.X / TILE_SIZE, point.Y / TILE_SIZE));
        }
        #endregion

        #region Misc
        static int DivideRoundUp(int number, int by)
        {
            return (number + by - 1) / by;
        }
        #endregion
    }
}
