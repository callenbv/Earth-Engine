using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using Engine.Core.Game;
using Engine.Core.Data;
using Newtonsoft.Json;

namespace Engine.Core.Systems.Rooms
{
    public class Room
    {
        public string background { get; set; } = "";
        public string Name { get; set; } = "Room";
        public bool backgroundTiled { get; set; } = false;
        public int width { get; set; } = 800;
        public int height { get; set; } = 600;
        public List<GameObject> objects { get; set; } = new List<GameObject>();

        /// <summary>
        /// Render a scene
        /// </summary>
        public void Render(SpriteBatch spriteBatch)
        {
            foreach (var obj in objects)
            {
                obj.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Update a scene
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            foreach (var obj in objects)
            {
                obj.Update(gameTime);
            }
        }
    }
}
