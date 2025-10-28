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
        virtual void BeginUpdate(GameTime gameTime) { }
        virtual void Draw(SpriteBatch spriteBatch) { }
        virtual void DrawUI(SpriteBatch spriteBatch) { }
        virtual void Destroy() { }
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
        /// Mark this component as UI
        /// </summary>
        public virtual bool IsUI => false;

        /// <summary>
        /// If this is active
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Unique identifier for the component. This is used to identify the component in the editor and in the game
        /// </summary>
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
        public Vector3 Position
        {
            get
            {
                var pos = Owner?.GetComponent<Transform>()?.Position ?? Vector3.Zero;
                return pos;
            }
            set
            {
                var transform = Owner?.GetComponent<Transform>();
                if (transform != null)
                    transform.Position = value;
            }
        }

        /// <summary>
        /// Old position of the object in the world. This is used to store the previous position of the object for various purposes, such as animations or physics calculations.
        /// </summary>
        [JsonIgnore]
        [HideInInspector]
        public Vector3 OldPosition
        {
            get
            {
                var pos = Owner?.GetComponent<Transform>()?.OldPosition ?? Vector3.Zero;
                return pos;
            }
            set
            {
                var transform = Owner?.GetComponent<Transform>();
                if (transform != null)
                    transform.OldPosition = value;
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
        public Vector3 Scale
        {
            get
            {
                var scale = Owner?.GetComponent<Transform>()?.Scale ?? Vector3.One;
                return scale;
            }
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
        /// Indicates whether the component should be updated in the editor
        /// </summary>
        [JsonIgnore]
        [HideInInspector]
        public virtual bool UpdateInEditor { get; set; } = false;

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
        /// BeginUpdate is called at the start of the update cycle. This method can be used to perform any setup or preparation before the main update logic is executed.
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void BeginUpdate(GameTime gameTime) { }

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

        /// <summary>
        /// Called when this collider collides with another collider.
        /// </summary>
        /// <param name="other"></param>
        public virtual void OnCollision(Collider2D other)
        {
        }

        /// <summary>
        /// Called when this collider enters a trigger with another collider.
        /// </summary>
        /// <param name="other"></param>
        public virtual void OnCollisionTrigger(Collider2D other)
        {
        }
    }
}

