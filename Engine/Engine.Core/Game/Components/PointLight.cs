/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         PointLight.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Game
{
    /// <summary>
    /// Represents a point light component that can be attached to a GameObject.
    /// </summary>
    [ComponentCategory("Graphics")]
    public class PointLight : ObjectComponent
    {
        public override string Name => "Point Light";
        private Texture2D? softCircleTexture;
        private int diameter = 64;

        /// <summary>
        /// The radius of the light in pixels. This determines how far the light will reach.
        /// </summary>
        public float lightRadius { get; set; } = 64f;

        /// <summary>
        /// The intensity of the light. This determines how bright the light will be.
        /// </summary>
        public float lightIntensity { get; set; } = 1f;

        /// <summary>
        /// The color of the light. This determines the color of the light emitted by the point light.
        /// </summary>
        public Color lightColor { get; set; } = Color.White;

        /// <summary>
        /// Offset of the light from the GameObject's position.
        /// </summary>
        public Vector2 Offset { get; set; } = Vector2.Zero;

        /// <summary>
        /// Initializes the point light component, creating a soft circle texture for the light effect.
        /// </summary>
        public override void Initialize()
        {
            softCircleTexture = new Texture2D(GraphicsDevice, diameter, diameter);
            Color[] data = new Color[diameter * diameter];

            float r = diameter / 2f;
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float dist = (float)System.Math.Sqrt(dx * dx + dy * dy) / r;
                    float alpha = 1f - MathHelper.Clamp(dist, 0f, 1f);
                    data[y * diameter + x] = new Color(1f, 1f, 1f, alpha * alpha); // Soft falloff
                }
            }
            softCircleTexture.SetData(data);
        }

        /// <summary>
        /// Sets the light properties for the point light.
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="intensity"></param>
        /// <param name="color"></param>
        public void SetLight(float radius, float intensity, Color color)
        {
            lightRadius = radius;
            lightIntensity = intensity;
            lightColor = color;
        }

        /// <summary>
        /// Sets the light radius for the point light.
        /// </summary>
        /// <param name="intensity"></param>
        public void SetLightIntensity(float intensity)
        {
            lightIntensity = intensity;
        }

        /// <summary>
        /// Draws the point light to the screen using a soft circle texture.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void DrawLight(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, null, null, null, Camera.Main.GetViewMatrix(EngineContext.InternalWidth, EngineContext.InternalHeight));
            spriteBatch.Draw(
                softCircleTexture,
                Position + Offset,
                null,
                lightColor * lightIntensity,
                0f,
                new Vector2(diameter / 2, diameter / 2),
                lightRadius/ diameter,
                SpriteEffects.None,
                0f);
            spriteBatch.End();
        }
    }
}

