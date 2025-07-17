using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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
        public string type => GetType().Name;

        [JsonIgnore]
        public GameObject? Owner { get; set; }  

        [JsonIgnore]
        public static GraphicsDevice? GraphicsDevice { get; set; }


        public virtual void Create() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
        public virtual void DrawUI(SpriteBatch spriteBatch) { }
        public virtual void OnClick() { }
        public virtual void Destroy() { }
    }
}
