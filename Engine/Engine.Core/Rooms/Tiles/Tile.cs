/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Tile.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core
{
    /// <summary>
    /// Represents a single tile in a tile layer, including its index in the tileset, collision properties, and height.
    /// </summary>
    public class Tile
    {
        public int TileIndex { get; set; } // Index in the tileset
        public bool IsCollidable { get; set; }
        public int Height { get; set; } = 0;
        public bool IsStair { get; set; } = false; // Indicates if the tile is a stair tile

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
