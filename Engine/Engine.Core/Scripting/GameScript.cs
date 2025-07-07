using Engine.Core.Game;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core
{
    public class GameScript
    {
        public GameObject? Owner { get; private set; }

        public static Engine.Core.Camera Camera => Engine.Core.Camera.Main;
        public static GraphicsDevice? GraphicsDevice { get; set; }
        public static object? RoomManager { get; set; }

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

        protected float lightRadius = 0f;
        protected float lightIntensity = 0f;
        protected string lightColor = "Orange";

        public void SetLight(float radius, float intensity, string color)
        {
            lightRadius = radius;
            lightIntensity = intensity;
            lightColor = color;
        }
        
        public void SetLightIntensity(float intensity)
        {
            lightIntensity = intensity;
        }
    }
} 