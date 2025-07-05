using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core
{
    public class GameScript
    {
        public GameObject Owner { get; private set; }

        public static Engine.Core.Camera Camera => Engine.Core.Camera.Main;
        public static GraphicsDevice GraphicsDevice { get; set; }
        public static object RoomManager { get; set; }

        public void Attach(GameObject owner)
        {
            Owner = owner;
            Create();
        }

        public virtual void Create() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
        public virtual void OnClick() { }
        public virtual void Destroy() { }
        
        // Spawn method that scripts can call
        protected GameObject SpawnObject(string objectName, Vector2 position)
        {
            if (RoomManager != null)
            {
                // Use reflection to call the RoomManager's SpawnObject method
                var spawnMethod = RoomManager.GetType().GetMethod("SpawnObject");
                if (spawnMethod != null)
                {
                    return spawnMethod.Invoke(RoomManager, new object[] { objectName, position }) as GameObject;
                }
            }
            return null;
        }
    }
} 