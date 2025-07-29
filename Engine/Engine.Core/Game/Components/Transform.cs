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
        /// The rotation of the GameObject in degrees. This is used to rotate the object in the world.
        /// </summary>
        [SliderEditor(0f, 360f)]
        new public float Rotation { get; set; }

        /// <summary>
        /// The scale of the GameObject. This is used to scale the object in the world.
        /// </summary>
        [SliderEditor(0f, 100f)]
        new public Vector2 Scale { get; set; } = Vector2.One;
    }
}

