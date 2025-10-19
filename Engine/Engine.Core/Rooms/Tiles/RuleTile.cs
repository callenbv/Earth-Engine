
using Microsoft.Xna.Framework;

namespace Engine.Core.Rooms.Tiles
{
    /// <summary>
    /// Lets us define whether or not a tile is facing a certain direction
    /// Defines the directions a tile is facing (orthogonal)
    /// </summary>
    public enum TileDirection
    { 
        Up,
        Down,
        Left,
        Right
    }

    /// <summary>
    /// Defines a rule tile class that has autotiling rules
    /// </summary>
    public class RuleTile : Tile
    {
        /// <summary>
        /// The autotile pattern to use for this rule tile
        /// </summary>
        public AutotilePattern Pattern { get; set; } = AutotilePattern.ThreeByThree;

        /// <summary>
        /// Base frame rectangle (top-left tile in the autotile set)
        /// </summary>
        public Rectangle BaseFrame { get; set; } = new Rectangle();

        public RuleTile()
        {
            BaseFrame = new Rectangle();
        }

        /// <summary>
        /// Set the frame
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public RuleTile(int x, int y, int width, int height)
        {
            Frame.X = x;
            Frame.Y = y;
            Frame.Width = width;
            Frame.Height = height;
        }

        /// <summary>
        /// These are the directions the tile is facing. They define how the tile is autotiled
        /// </summary>
        public Dictionary<TileDirection, bool> Directions = new Dictionary<TileDirection, bool>();

        /// <summary>
        /// Given adjacent cells, autotile this tile with O(n)
        /// </summary>
        /// <param name="tilemap">The tilemap containing this tile</param>
        /// <param name="x">X coordinate of this tile</param>
        /// <param name="y">Y coordinate of this tile</param>
        public void AutoTile(Tilemap tilemap, int x, int y)
        {
            // Get neighboring tiles
            var neighbors = tilemap.GetNeighboringTiles(x, y);
            
            // Check which directions have tiles of the same type
            Directions[TileDirection.Up] = HasSameTileType(neighbors[TileDirection.Up]);
            Directions[TileDirection.Down] = HasSameTileType(neighbors[TileDirection.Down]);
            Directions[TileDirection.Left] = HasSameTileType(neighbors[TileDirection.Left]);
            Directions[TileDirection.Right] = HasSameTileType(neighbors[TileDirection.Right]);
            
            // Update the frame based on the autotile pattern
            UpdateFrameFromDirections();
        }

        /// <summary>
        /// Check if a neighboring tile is of the same type as this rule tile
        /// </summary>
        /// <param name="neighbor">The neighboring tile to check</param>
        /// <returns>True if the neighbor is of the same type</returns>
        private bool HasSameTileType(Tile? neighbor)
        {
            if (neighbor == null)
                return false;
                
            // Check if the neighbor is the same type of rule tile
            // You can customize this logic based on your needs
            return neighbor is RuleTile && neighbor.TileIndex == this.TileIndex;
        }

        /// <summary>
        /// Update the frame based on the current directions using the selected pattern
        /// </summary>
        private void UpdateFrameFromDirections()
        {
            // Use the pattern system to calculate the correct frame
            Frame = AutotilePatterns.CalculateFrame(Pattern, Directions, BaseFrame);
        }

        /// <summary>
        /// Set the base frame for this rule tile (the top-left tile in the autotile set)
        /// </summary>
        /// <param name="x">X coordinate of the base tile in the tileset</param>
        /// <param name="y">Y coordinate of the base tile in the tileset</param>
        /// <param name="width">Width of each tile</param>
        /// <param name="height">Height of each tile</param>
        public void SetBaseFrame(int x, int y, int width, int height)
        {
            BaseFrame = new Rectangle(x, y, width, height);
            Frame = BaseFrame; // Set initial frame to base frame
        }

        /// <summary>
        /// Set the autotile pattern for this rule tile
        /// </summary>
        /// <param name="pattern">The autotile pattern to use</param>
        public void SetPattern(AutotilePattern pattern)
        {
            Pattern = pattern;
        }
    }
}
