/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Tile.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Rooms.Tiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core
{
    /// <summary>
    /// Represents a single tile in a tile layer, including its index in the tileset, collision properties, and height.
    /// </summary>
    public class Tile
    {
        public int TileIndex { get; set; }
        public bool IsCollidable { get; set; }
        public int Height { get; set; } = 0;

        /// <summary>
        /// This is the frame relative to the tile texture we want to source from
        /// </summary>
        public Rectangle Frame = new Rectangle();

        /// <summary>
        /// Destination render
        /// </summary>
        public Rectangle Destination = new Rectangle();

        /// <summary>
        /// Create a new Tile with a specified tile index.
        /// </summary>
        /// <param name="tileIndex"></param>
        public Tile(int tileIndex)
        {
            TileIndex = tileIndex;
        }

        /// <summary>
        /// Default constructor for Tile, initializes with default values.
        /// </summary>
        public Tile()
        {
        }

        /// <summary>
        /// Base autotile method
        /// </summary>
        public virtual void AutoTile(TilemapRenderer renderer)
        {

        }
    }

    /// <summary>
    /// Represents a layer of tiles in a tilemap, including the name, tile dimensions, visibility, and an array of tiles.
    /// </summary>
    public class TileLayer
    {
        public string Name = string.Empty;
        public Tile[,]? Tiles;
        public IntPtr TextureId;
        public int TileWidth;
        public int TileHeight;
        public bool Visible = true;

        /// <summary>
        /// Create a new TileLayer with specified width and height.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public TileLayer(int width, int height)
        {
            Tiles = new Tile[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Tiles[x, y] = new Tile();
        }
    }
}
