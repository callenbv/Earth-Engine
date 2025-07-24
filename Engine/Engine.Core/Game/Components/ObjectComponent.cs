using Engine.Core.Data;
using Engine.Core.Graphics;
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
        virtual void Update(GameTime gameTime) { }
        virtual void Draw(SpriteBatch spriteBatch) { }
        virtual void DrawUI(SpriteBatch spriteBatch) { }
        virtual void Create() { }

        [EditorOnlyAttribute]
        virtual void Initialize() { }
    }

    public abstract class ObjectComponent : IComponent
    {
        public virtual string Name => GetType().Name;
        public string type => GetType().Name;

        [JsonIgnore]
        [HideInInspector]
        public Vector2 Position
        {
            get => Owner?.GetComponent<Transform>()?.Position ?? Vector2.Zero;
            set
            {
                var transform = Owner?.GetComponent<Transform>();
                if (transform != null)
                    transform.Position = value;
            }
        }

        [JsonIgnore]
        [HideInInspector]
        public GameObject? Owner { get; set; }  

        [JsonIgnore]
        public static GraphicsDevice? GraphicsDevice => TextureLibrary.Instance.graphicsDevice;
        [JsonIgnore]
        [HideInInspector]
        public float dt => EngineContext.DeltaTime; // Retrieve the delta time from any component

        public virtual void Initialize() { }
        public virtual void Create() { }
        public virtual void Update(GameTime gameTime) { }
        public virtual void Draw(SpriteBatch spriteBatch) { }
        public virtual void DrawUI(SpriteBatch spriteBatch) { }
        public virtual void OnClick() { }
        public virtual void Destroy() { }
    }
}
