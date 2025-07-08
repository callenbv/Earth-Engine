using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Engine.Core.Game
{
    public class GameObjectManager
    {
        public List<GameObject> gameObjects = new List<GameObject>();
        public List<TileMap> tileMaps = new List<TileMap>();

        private static GameObjectManager? Instance;
        public static GameObjectManager Main => Instance ??= new GameObjectManager();

        public GameObjectManager()
        {
            Instance = this;
        }

        /// <summary>
        /// Updates and draws all objects in the list
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="spriteBatch"></param>
        public void Update(GameTime gameTime)
        {
            foreach (var gameObj in gameObjects)
            {
                if (!gameObj.IsDestroyed)
                {
                    gameObj.Update(gameTime);
                }
            }

            gameObjects.RemoveAll(obj => obj.IsDestroyed);
        }

        /// <summary>
        /// Render all objects sorted by depth (Y coordinate at feet)
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Get all non-destroyed objects and sort them by depth
            var objectsToDraw = gameObjects
                .Where(obj => !obj.IsDestroyed)
                .ToList();

            // Update depth values and draw
            foreach (var gameObj in objectsToDraw)
            {
                // Update the sprite's depth based on feet position
                if (gameObj.sprite != null)
                {
                    gameObj.sprite.depth = GetObjectDepth(gameObj);
                }
                
                gameObj.Draw(spriteBatch);
            }

            foreach (var tilemap in tileMaps)
            {
                tilemap.Draw(spriteBatch, Vector2.Zero);
            }
        }

        /// <summary>
        /// Calculate the depth value based on the object's feet position (bottom of sprite)
        /// </summary>
        /// <param name="gameObj">The GameObject to calculate depth for</param>
        /// <returns>Depth value (0 = front, 1 = back)</returns>
        private float GetObjectDepth(GameObject gameObj)
        {
            if (gameObj.sprite == null || gameObj.sprite.texture == null)
                return 0f;

            // Calculate the bottom Y position (feet) of the sprite
            float feetY = gameObj.position.Y+gameObj.sprite.texture.Height/2;

            // Convert to depth value (0 = front, 1 = back)
            // You can adjust this calculation based on your game's coordinate system
            // For a typical top-down view, higher Y values should have higher depth (appear behind)
            return feetY / 1000f; // Normalize to 0-1 range, adjust divisor as needed
        }

        /// <summary>
        /// Get all managed GameObjects
        /// </summary>
        /// <returns>List of all GameObjects</returns>
        public List<GameObject> GetAllObjects()
        {
            return gameObjects.Where(obj => !obj.IsDestroyed).ToList();
        }

        /// <summary>
        /// Find a GameObject by name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <returns>The GameObject if found, null otherwise</returns>
        public GameObject FindByName(string name)
        {
            return gameObjects.FirstOrDefault(obj => !obj.IsDestroyed && obj.Name == name);
        }

        /// <summary>
        /// Remove all GameObjects
        /// </summary>
        public void Clear()
        {
            foreach (var obj in gameObjects)
            {
                obj.Destroy();
            }
            gameObjects.Clear();
        }
    }
}
