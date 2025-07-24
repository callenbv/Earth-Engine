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
        new public Vector2 Position { get; set; }
        new public float Rotation { get; set; }
        new public float Scale { get; set; } = 1f;
        public override string Name => "Transform";
    }
}

