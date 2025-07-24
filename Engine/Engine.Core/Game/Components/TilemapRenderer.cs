using Engine.Core.Data;
using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Text.Json.Serialization;

namespace Engine.Core.Game.Components
{
    [ComponentCategory("Tiles")]
    public class TilemapRenderer : ObjectComponent
    {
        public override string Name => "Tilemap Renderer";
        public int Width = 100;
        public int Height = 100;
        public string TexturePath = string.Empty;
        public string Title { get; set; } = "Tilemap Layer";
        public int TileSize { get; private set; } = 16;
        [JsonIgnore]
        public Tile[,] Tiles { get; set; } = new Tile[100,100];
        [JsonIgnore]
        public Texture2D? Texture { get; set; }
        public IntPtr TexturePtr { get; set; }

        public TilemapRenderer()
        {

        }

        public TilemapRenderer(int width, int height, string texture)
        {
            Width = width;
            Height = height;
            Texture = TextureLibrary.Instance.Get(texture);
            TexturePath = texture;
            Tiles = new Tile[width, height];
        }

        public void SetTile(int x, int y, int index)
        {
            if (index < 0)
                Tiles[x,y] = null;
                else
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
                            Position + new Vector2(x * TileSize, y * TileSize),
                            source,
                            Color.White);

                    }
                }
            }
        }
    }
}