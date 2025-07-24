using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core
{
    public class Tile
    {
        public int TileIndex { get; set; } // Index in the tileset
        public bool IsCollidable { get; set; }
        public int Height { get; set; } = 0;
        public Tile(int tileIndex)
        {
            TileIndex = tileIndex;
        }
        public Tile()
        { 
        }
    }
    public class TileLayer
    {
        public string Name = string.Empty;
        public Tile[,]? Tiles;
        public IntPtr TextureId;
        public int TileWidth;
        public int TileHeight;
        public bool Visible = true;
        public TileLayer(int width, int height)
        {
            Tiles = new Tile[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Tiles[x, y] = new Tile();
        }
    }
}