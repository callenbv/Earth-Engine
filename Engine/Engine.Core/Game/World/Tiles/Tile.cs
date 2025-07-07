using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core
{
    public class Tile
    {
        public int TileIndex { get; set; } // Index in the tileset
        public bool IsCollidable { get; set; }

        public Tile(int tileIndex)
        {
            TileIndex = tileIndex;
        }
    }
} 