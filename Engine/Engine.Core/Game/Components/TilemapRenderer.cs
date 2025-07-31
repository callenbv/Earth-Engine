/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TilemapRenderer.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json.Serialization;

namespace Engine.Core.Game.Components
{
    /// <summary>
    /// Represents a tilemap renderer component that can be attached to a GameObject.
    /// </summary>
    [ComponentCategory("Tiles")]
    public class TilemapRenderer : ObjectComponent
    {
        public override string Name => "Tilemap Renderer";

        /// <summary>
        /// Width of the tilemap in tiles. Default is 100x100 tiles.
        /// </summary>
        public int Width = 100;

        /// <summary>
        /// Height of the tilemap in tiles. Default is 100x100 tiles.
        /// </summary>
        public int Height = 100;

        /// <summary>
        /// Floor level of the tilemap, used for rendering order. Default is 100.
        /// </summary>
        public int FloorLevel = 1;

        /// <summary>
        /// Path to the texture used for the tilemap. This is set automatically when the texture is assigned.
        /// </summary>
        public string TexturePath = string.Empty;

        /// <summary>
        /// Title of the tilemap layer, used for identification in the editor.
        /// </summary>
        public string Title { get; set; } = "Tilemap Layer";

        /// <summary>
        /// Size of each tile in pixels. Default is 16x16 pixels.
        /// </summary>
        public int TileSize = 16;

        /// <summary>
        /// Depth of the tilemap layer, used for rendering order. Default is 0.
        /// </summary>
        public float Depth = 0;

        /// <summary>
        /// Visibility of the tilemap layer. If false, the layer will not be rendered.
        /// </summary>
        public bool Visible = true;

        /// <summary>
        /// CollisionEnabled indicates whether the tilemap layer should handle collisions. If true, the layer will check for collisions with tiles.
        /// </summary>
        public bool CollisionEnabled = false;

        /// <summary>
        /// Array of tiles in the tilemap. Each tile is represented by a Tile object, which contains its index in the tileset and other properties.
        /// </summary>
        [JsonIgnore]
        public Tile[,] Tiles { get; set; } = new Tile[100,100];

        /// <summary>
        /// Texture used for the tilemap. This is set automatically when the texture is assigned.
        /// </summary>
        [JsonIgnore]
        public Texture2D? Texture
        {
            get => texture_;
            set
            {
                texture_ = value;

                if (texture_ != null && !string.IsNullOrEmpty(texture_.Name))
                {
                    TexturePath = texture_.Name;
                    TexturePtr = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Real texture
        /// </summary>
        [JsonIgnore]
        private Texture2D? texture_;

        /// <summary>
        /// Pointer to the texture used for the tilemap. This is used for rendering the tileset preview in the editor.
        /// </summary>
        public IntPtr TexturePtr { get; set; }

        /// <summary>
        /// Tint color applied to the tilemap when rendering. This can be used to change the color of the tiles without modifying the texture.
        /// </summary>
        public Color Tint { get; set; } = Color.White;

        /// <summary>
        /// Offset for the tilemap position, used to adjust the rendering position of the tilemap in the world.
        /// </summary>
        public System.Numerics.Vector2 Offset = System.Numerics.Vector2.Zero;

        /// <summary>
        /// Default constructor for TilemapRenderer, initializes with default values.
        /// </summary>
        public TilemapRenderer()
        {

        }

        /// <summary>
        /// Create a new TilemapRenderer with specified width, height, and texture path.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="texture"></param>
        public TilemapRenderer(int width, int height, string texture)
        {
            Width = width;
            Height = height;
            Texture = TextureLibrary.Instance.Get(texture);
            TexturePath = texture;
            Tiles = new Tile[width, height];
        }

        /// <summary>
        /// Set the tile at the specified position to the given index.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="index"></param>
        public void SetTile(int x, int y, int index)
        {
            if (index < -1)
            {
                Tiles[x, y] = null;
            }
            else
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    Tile tile = new Tile(index);
                    Tiles[x, y] = tile;
                }
            }
        }

        /// <summary>
        /// Set the collision property of the tile at the specified position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="collidable"></param>
        public void SetCollision(int x, int y, bool collidable)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                if (Tiles[x, y] == null)
                    Tiles[x, y] = new Tile(-1);

                Tiles[x, y].IsCollidable = collidable;
            }
        }

        /// <summary>
        /// Set the stair property of the tile at the specified position. If collidable is true, the tile will be treated as a stair tile.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="collidable"></param>
        public void SetStair(int x, int y, bool stair)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                if (Tiles[x, y] == null)
                    Tiles[x, y] = new Tile(-1);

                Tiles[x, y].IsStair = stair;
                Tiles[x, y].IsCollidable = false;
            }
        }

        /// <summary>
        /// Check if the specified tile coordinates are valid within the tilemap dimensions.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsValidTile(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        /// <summary>
        /// Get the tile at the specified position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Tile? GetTile(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                return Tiles[x, y];

            return null;
        }

        /// <summary>
        /// Toggle the visibility of the tilemap layer. If the layer is currently visible, it will be hidden, and vice versa.
        /// </summary>
        public void ToggleVisibility()
        {
            Visible = !Visible;
        }

        /// <summary>
        /// Render the tilemap layer using the provided SpriteBatch. This method draws each tile in the layer based on its index in the tileset texture.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible)
                return;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var tile = Tiles[x, y];
                    if (tile != null && Texture != null)
                    {
                        // Draw the tile at the correct position
                        int tilesPerRow = Texture.Width / TileSize;

                        int index = tile.TileIndex;
                        int col = index % tilesPerRow;        // x in tiles
                        int row = index / tilesPerRow;        // y in tiles

                        var source = new Rectangle(
                            col * TileSize,
                            row * TileSize,
                            TileSize,
                            TileSize);

                        Tint = Color.White;
                        
                        if (!EngineContext.Running)
                        {
                            if (tile.IsCollidable)
                            {
                                Tint = Color.Red;
                            }
                            else if (tile.IsStair)
                            {
                                Tint = Color.Blue;
                            }
                        }
                        else
                        {
                            if (tile.TileIndex < 0)
                                continue;
                        }

                        spriteBatch.Draw(
                            Texture,
                            Position + Offset + new System.Numerics.Vector2(x * TileSize, y * TileSize),
                            source,
                            Tint,
                            0f,
                            Microsoft.Xna.Framework.Vector2.Zero,
                            1f,
                            SpriteEffects.None,
                            Depth
                            );
                    }
                }
            }
        }

        /// <summary>
        /// Check if a tile at the specified coordinates is solid (collidable).
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsSolidAtTile(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height || Tiles[x,y] == null)
                return false;

            return Tiles[x, y].IsCollidable;
        }

        /// <summary>
        /// Check if a tile at the specified world position is solid (collidable).
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public bool IsSolidAtWorld(Vector2 worldPos)
        {
            int x = (int)(worldPos.X / TileSize);
            int y = (int)(worldPos.Y / TileSize);
            return IsSolidAtTile(x, y);
        }
    }
}
