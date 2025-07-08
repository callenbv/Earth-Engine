using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Game.Tiles
{
    public class TilemapRenderer
    {
        public List<TileMap> tileMaps = new List<TileMap>();

        /// <summary>
        /// Create a new dungeon
        /// </summary>
        public void Initialize()
        {

        }

        /// <summary>
        /// Draw all tilemaps
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var tileMap in tileMaps)
            {
                tileMap.Draw(spriteBatch, Vector2.Zero);
            }
        }
    }
}
