using Engine.Core.Data.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Rooms.Tiles
{
    /// <summary>
    /// Defines a tilemap class to hold the texture and tiles within a world
    /// </summary>
    public class TilemapRenderer
    {
        /// <summary>
        /// The tilemap reference 
        /// </summary>
        public Tilemap Tilemap { get; set; }

        /// <summary>
        /// Sets up the render with a tilemap
        /// </summary>
        public TilemapRenderer(Tilemap tilemap)
        {
            Tilemap = tilemap;
        }

        /// <summary>
        /// Default constructor - this should never be called!
        /// </summary>
        public TilemapRenderer()
        {
            Tilemap = new Tilemap();
        }

        /// <summary>
        /// Source texture of the tilemap. Slices the texture whenever it is set
        /// </summary>
        public TextureData? texture
        {
            get => texture_;
            set
            {
                texture_ = value;
                texture?.Slice(Tilemap.CellSize, Tilemap.CellSize);
            }
        }
        private TextureData? texture_;

        /// <summary>
        /// Call when we want to update the tilemap (e.g, slice it into grid)
        /// </summary>
        public void Slice()
        {
            texture?.Slice(Tilemap.CellSize,Tilemap.CellSize);
        }

        /// <summary>
        /// Draw the tiles in the tilemap
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Bad texture, no draw
            if (texture == null)
                return;

            // Draw the tiles in the map
            for (int x = 0; x < Tilemap.MapWidth; x++) 
            {
                for (int y = 0; y < Tilemap.MapHeight; y++)
                {
                    Tile tile = Tilemap.Tiles[x,y];

                    if (tile == null)
                        continue;

                    Rectangle dest = new Rectangle(x*Tilemap.CellSize, y*Tilemap.CellSize, Tilemap.CellSize, Tilemap.CellSize);
                    spriteBatch.Draw(texture.texture, dest, tile.Frame, texture.Color);
                }
            }
        }
    }
}
