/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Transform.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;

namespace Engine.Core.Game
{
    /// <summary>
    /// Represents a transform component that can be attached to a GameObject.
    /// </summary>
    [ComponentCategory("World")]
    public class Transform : ObjectComponent
    {
        public override string Name => "Transform";

        /// <summary>
        /// The position of the GameObject in the world. This is used to position the object in the world.
        /// </summary>
        new public Vector2 Position { get; set; }

        /// <summary>
        /// The old position of the GameObject in the world. This is used to track the previous position of the object for movement calculations.
        /// </summary>
        new public Vector2 OldPosition { get; set; }

        /// <summary>
        /// The rotation of the GameObject in degrees. This is used to rotate the object in the world.
        /// </summary>
        [SliderEditor(0f, 360f)]
        new public float Rotation { get; set; }

        /// <summary>
        /// The scale of the GameObject. This is used to scale the object in the world.
        /// </summary>
        new public Vector2 Scale { get; set; } = Vector2.One;

        /// <summary>
        /// Initializes a new instance of the Transform component with default values.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void BeginUpdate(GameTime gameTime)
        {
            // Update the old position to the current position before changing it
            OldPosition = Position;
            // Here you can add logic to update the position, rotation, or scale based on game logic
            // For example, you might want to move the object based on user input or physics
        }
    }
}

