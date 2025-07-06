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
        
        // Force reload a texture
        protected void ReloadTexture(string spriteName)
        {
            if (RoomManager != null)
            {
                var loadTextureMethod = RoomManager.GetType().GetMethod("LoadTexture", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (loadTextureMethod != null)
                {
                    loadTextureMethod.Invoke(RoomManager, new object[] { spriteName, true });
                }
            }
        }
        
        // Lighting control methods
        protected void SetLight(bool enabled, float radius = 100f, float intensity = 1f, string color = "White")
        {
            // Use reflection to access AnimatedGameObject properties
            var animObjType = Owner.GetType();
            if (animObjType.Name == "AnimatedGameObject")
            {
                var emitsLightProp = animObjType.GetField("emitsLight");
                var lightRadiusProp = animObjType.GetField("lightRadius");
                var lightIntensityProp = animObjType.GetField("lightIntensity");
                var lightColorProp = animObjType.GetField("lightColor");
                
                if (emitsLightProp != null) emitsLightProp.SetValue(Owner, enabled);
                if (lightRadiusProp != null) lightRadiusProp.SetValue(Owner, radius);
                if (lightIntensityProp != null) lightIntensityProp.SetValue(Owner, intensity);
                if (lightColorProp != null) lightColorProp.SetValue(Owner, color);
            }
        }
        
        protected void SetLightRadius(float radius)
        {
            var animObjType = Owner.GetType();
            if (animObjType.Name == "AnimatedGameObject")
            {
                var lightRadiusProp = animObjType.GetField("lightRadius");
                if (lightRadiusProp != null) lightRadiusProp.SetValue(Owner, radius);
            }
        }
        
        protected void SetLightIntensity(float intensity)
        {
            var animObjType = Owner.GetType();
            if (animObjType.Name == "AnimatedGameObject")
            {
                var lightIntensityProp = animObjType.GetField("lightIntensity");
                if (lightIntensityProp != null) lightIntensityProp.SetValue(Owner, intensity);
            }
        }
        
        protected void SetLightColor(string color)
        {
            var animObjType = Owner.GetType();
            if (animObjType.Name == "AnimatedGameObject")
            {
                var lightColorProp = animObjType.GetField("lightColor");
                if (lightColorProp != null) lightColorProp.SetValue(Owner, color);
            }
        }
    }
} 