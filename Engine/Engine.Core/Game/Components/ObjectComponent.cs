/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ObjectComponent.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.CustomMath;
using Engine.Core.Data;
using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json.Serialization;

namespace Engine.Core.Game.Components
{
    /// <summary>
    /// Base interface for all components in the game engine.
    /// </summary>
    public interface IComponent 
    {
        string Name { get; }
        virtual void Update(GameTime gameTime) { }
        virtual void Draw(SpriteBatch spriteBatch) { }
        virtual void DrawUI(SpriteBatch spriteBatch) { }
        virtual void Create() { }

        [EditorOnlyAttribute]
        virtual void Initialize() { }

        /// <summary>
        /// Get the unique identifier for the component.
        /// </summary>
        /// <returns></returns>
        public int GetID() { return (this as ObjectComponent)?.ID ?? 0; }
    }

    public abstract class ObjectComponent : IComponent
    {
        /// <summary>
        /// Name of the component. This is used to identify the component in the editor and in the game.
        /// </summary>
        public virtual string Name => GetType().Name;

        /// <summary>
        /// Unique identifier for the component. This is used to identify the component in the editor and in the game
        /// </summary>
        [JsonIgnore]
        [HideInInspector]
        public int ID { get; set; } = ERandom.Range(0, 9999999);

        /// <summary>
        /// Type of the component. This is used to identify the component in the editor and in the game.
        /// </summary>
        public string type => GetType().Name;

        /// <summary>
        /// Position of the object in the world. This is used to position the object in the world.
        /// </summary>
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

        /// <summary>
        /// Rotation of the object in degrees. This is used to rotate the object in the world.
        /// </summary>
        [JsonIgnore]
        [HideInInspector]
        public float Rotation
        {
            get => Owner?.GetComponent<Transform>()?.Rotation ?? 0f;
            set
            {
                var transform = Owner?.GetComponent<Transform>();
                if (transform != null)
                    transform.Rotation = value;
            }
        }

        /// <summary>
        /// Scale of the object. This is used to scale the object in the world.
        /// </summary>
        [JsonIgnore]
        [HideInInspector]
        public float Scale
        {
            get => Owner?.GetComponent<Transform>()?.Scale ?? 1f;
            set
            {
                var transform = Owner?.GetComponent<Transform>();
                if (transform != null)
                    transform.Scale = value;
            }
        }

        /// <summary>
        /// The GameObject that this component is attached to. This is used to access the GameObject's properties and methods.
        /// </summary>
        [JsonIgnore]
        [HideInInspector]
        public GameObject? Owner { get; set; }

        /// <summary>
        /// Graphics device used for rendering. This is used to access the graphics device for rendering.
        /// </summary>
        [JsonIgnore]
        public static GraphicsDevice? GraphicsDevice => TextureLibrary.Instance.graphicsDevice;

        /// <summary>
        /// Delta time since the last frame.
        /// </summary>
        [JsonIgnore]
        [HideInInspector]
        public float dt => EngineContext.DeltaTime; // Retrieve the delta time from any component

        /// <summary>
        /// Initialize the component. This method is called when the component is created and should be used to set up any initial state or resources.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Create the component. This method is called when the component is added to a GameObject and should be used to set up any initial state or resources.
        /// </summary>
        public virtual void Create() { }

        /// <summary>
        /// Update the component. This method is called every frame and should be used to update the component's state.
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Update(GameTime gameTime) { }

        /// <summary>
        /// Draw the component. This method is called every frame and should be used to render the component to the screen.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public virtual void Draw(SpriteBatch spriteBatch) { }

        /// <summary>
        /// Draw the component's UI. This method is called every frame and should be used to render the component's UI to the screen.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public virtual void DrawUI(SpriteBatch spriteBatch) { }

        /// <summary>
        /// Handle the click event for the component. This method is called when the component is clicked.
        /// </summary>
        public virtual void OnClick() { }

        /// <summary>
        /// Destroy the component. This method is called when the component is removed from a GameObject.
        /// </summary>
        public virtual void Destroy() { }
    }
}

