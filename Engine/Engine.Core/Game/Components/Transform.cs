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
        new public Vector3 Position { get; set; }

        /// <summary>
        /// The old position of the GameObject in the world. This is used to track the previous position of the object for movement calculations.
        /// </summary>
        [HideInInspector]
        new public Vector3 OldPosition
        {
            get => _oldPosition;
            set => _oldPosition = value;
        }
        private Vector3 _oldPosition;

        /// <summary>
        /// The rotation of the GameObject in degrees. This is used to rotate the object in the world.
        /// </summary>
        [SliderEditor(0f, 360f)]
        new public float Rotation { get; set; }

        /// <summary>
        /// The scale of the GameObject. This is used to scale the object in the world.
        /// </summary>
        new public Vector3 Scale { get; set; } = Vector3.One;

    }
}

