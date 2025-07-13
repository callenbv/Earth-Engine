using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Engine.Core.Game.Components
{
    public class TilemapRenderer : ObjectComponent
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileSize { get; private set; } = 16;
        public Tile[,] Tiles { get; private set; }
        public Texture2D? Texture { get; set; }

        public TilemapRenderer(int width, int height, string texture)
        {
            Width = width;
            Height = height;
            Texture = TextureLibrary.Main.Get(texture);
            Tiles = new Tile[width, height];
        }

        public void SetTile(int x, int y, int index)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                Tiles[x, y] = new Tile(index);
        }

        public Tile? GetTile(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                return Tiles[x, y];

            return null;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
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

                        spriteBatch.Draw(
                            Texture,
                            Owner.position + new Vector2(x * TileSize, y * TileSize),
                            source,
                            Color.White);

                    }
                }
            }
        }
    }
}