using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core
{
    public class GameScript
    {
        public GameObject Owner { get; private set; }

        public static Engine.Core.Camera Camera => Engine.Core.Camera.Main;

        public void Attach(GameObject owner)
        {
            Owner = owner;
            Create();
        }

        public virtual void Create() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
    }
} 