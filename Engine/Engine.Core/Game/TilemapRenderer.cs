using Engine.Core.Data;
using Engine.Core.Data.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json.Serialization;

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
        [HideInInspector]
        [JsonIgnore]
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
            }
        }
        private TextureData? texture_;

        /// <summary>
        /// Call when we want to update the tilemap (e.g, slice it into grid)
        /// </summary>
        public void Slice()
        {
        }

        /// <summary>
        /// Draw the tiles in the tilemap
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw the tiles in the map
            for (int x = 0; x < Tilemap.MapWidth; x++) 
            {
                for (int y = 0; y < Tilemap.MapHeight; y++)
                {
                    Tile tile = Tilemap.Tiles[x,y];

                    if (tile == null)
                        continue;

                    if (tile?.Texture?.texture == null)
                        continue;

                    Rectangle dest = new Rectangle(x*tile.CellSize, y* tile.CellSize, tile.CellSize, tile.CellSize);
                    spriteBatch.Draw(tile.Texture.texture, dest, tile.Frame, Color.White, 0f,Vector2.Zero,SpriteEffects.None,0f);
                }
            }
        }
    }
}
