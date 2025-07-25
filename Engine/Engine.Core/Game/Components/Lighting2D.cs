/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         PointLight.cs
/// <Author>       Callen Betts Virott  
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                 
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Game.Components;
using Engine.Core.Graphics;
using Microsoft.Xna.Framework;

namespace Engine.Core.Game
{
    /// <summary>
    /// Represents a point light component that can be attached to a GameObject.
    /// </summary>
    [ComponentCategory("Graphics")]
    public class Lighting2D : ObjectComponent
    {
        public override string Name => "Lighting 2D";

        /// <summary>
        /// The color of the ambient light in the scene. This is used to simulate global illumination.
        /// </summary>
        public Color AmbientLightColor { get; set; } = Color.Gray;

        /// <summary>
        /// Indicates whether the lighting system is enabled. If false, no lighting effects will be applied.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The grain size for the lighting system. This can be used to control the blockiness of the lighting effects.
        /// </summary>
        [SliderEditor(1,100)]
        public int Granularity { get; set; } = 10;

        /// <summary>
        /// The wind effect on the lighting system. This can be used to simulate dynamic lighting changes, such as flickering lights.
        /// </summary>
        [SliderEditor(0,10)]
        public float Wind { get; set; } = 1f;

        /// <summary>
        /// Update method that is called every frame to update the lighting settings.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (Enabled)
            {
                Lighting.Instance.Enabled = Enabled;
                Lighting.Instance.Granularity = Granularity;
                Lighting.Instance.AmbientLightColor = AmbientLightColor;
                Lighting.Instance.Wind = Wind;
            }
            else
            {
                Lighting.Instance.AmbientLightColor = Color.White;
            }
        }
    }
}

