/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TileData.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Serializable data structure for tiles used in JSON serialization                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;

namespace Engine.Core.Rooms.Tiles
{
    /// <summary>
    /// Serializable data structure for tiles used in JSON serialization.
    /// This class represents a tile's data in a format that can be easily serialized to/from JSON.
    /// </summary>
    public class TileData
    {
        /// <summary>
        /// X coordinate of the tile in the tilemap grid
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Y coordinate of the tile in the tilemap grid
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Index of the tile in the tileset
        /// </summary>
        public int TileIndex { get; set; }

        /// <summary>
        /// Whether this tile is collidable
        /// </summary>
        public bool IsCollidable { get; set; }

        /// <summary>
        /// Height of the tile (for 3D tilemaps)
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Source rectangle in the tileset texture
        /// </summary>
        public Rectangle Frame { get; set; }

        /// <summary>
        /// Destination rectangle for rendering
        /// </summary>
        public Rectangle Destination { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TileData()
        {
            Frame = new Rectangle();
            Destination = new Rectangle();
        }

        /// <summary>
        /// Create TileData from a Tile object and its position
        /// </summary>
        /// <param name="tile">The tile to convert</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public TileData(Tile tile, int x, int y)
        {
            X = x;
            Y = y;
            TileIndex = tile.TileIndex;
            IsCollidable = tile.IsCollidable;
            Height = tile.Height;
            Frame = tile.Frame;
            Destination = tile.Destination;
        }

        /// <summary>
        /// Convert this TileData back to a Tile object
        /// </summary>
        /// <returns>A new Tile object with this data</returns>
        public Tile ToTile()
        {
            return new Tile(TileIndex)
            {
                IsCollidable = IsCollidable,
                Height = Height,
                Frame = Frame,
                Destination = Destination
            };
        }
    }
}