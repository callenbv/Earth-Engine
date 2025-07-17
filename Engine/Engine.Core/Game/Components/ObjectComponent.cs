using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Game.Components
{
    public interface IComponent 
    {
        string Name { get; }
        void Update(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch);
        void DrawUI(SpriteBatch spriteBatch);
    }

    public abstract class ObjectComponent : IComponent
    {
        public virtual string Name => "Component";
        public GameObject? Owner { get; set; }
        public static Engine.Core.Camera Camera => Engine.Core.Camera.Main;
        public static GraphicsDevice? GraphicsDevice { get; set; }
        public static object? RoomManager { get; set; }

        public virtual void Create() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
        public virtual void DrawUI(SpriteBatch spriteBatch) { }
        public virtual void OnClick() { }
        public virtual void Destroy() { }
    }
}
