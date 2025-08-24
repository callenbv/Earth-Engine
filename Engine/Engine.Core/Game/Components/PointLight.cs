/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         PointLight.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.CustomMath;
using Engine.Core.Data;
using Engine.Core.Game.Components;
using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json.Serialization;

namespace Engine.Core.Game
{
    /// <summary>
    /// Represents a point light component that can be attached to a GameObject.
    /// </summary>
    [ComponentCategory("Graphics")]
    public class PointLight : ObjectComponent
    {
        public override string Name => "Point Light";
        public override bool UpdateInEditor => true;

        private Texture2D? softCircleTexture;
        private int diameter = 64;

        /// <summary>
        /// The radius of the light in pixels. This determines how far the light will reach.
        /// </summary>
        [SliderEditor(0f, 1000f)]
        public float lightRadius { get; set; } = 64f;
        [HideInInspector]
        public float finalRadius { get; set; } = 1f;

        /// <summary>
        /// The intensity of the light. This determines how bright the light will be.
        /// </summary>
        [SliderEditor(0f, 2f)]
        public float lightIntensity { get; set; } = 1f;
        [HideInInspector]
        public float finalIntensity { get; set; } = 1f;

        /// <summary>
        /// The color of the light. This determines the color of the light emitted by the point light.
        /// </summary>
        public Color lightColor { get; set; } = Color.White;

        /// <summary>
        /// Offset of the light from the GameObject's position.
        /// </summary>
        public Vector3 Offset { get; set; } = Vector3.Zero;

        /// <summary>
        /// The intensity of the flicker effect. This determines how much the light's intensity will vary when flickering is enabled.
        /// </summary>
        [SliderEditor(0f, 20f)]
        public float FlickerIntensity { get; set; } = 0f;

        /// <summary>
        /// The granularity of the light effect. This controls how blocky the light appears, with higher values resulting in smoother transitions.
        /// </summary>
        private int Granularity
        {
            get => granularity_;
            set 
            {
                granularity_ = Lighting.Instance.Granularity;
                Initialize();
            }
        }
        private int granularity_ = 10;

        /// <summary>
        /// Initializes the point light component, creating a soft circle texture for the light effect.
        /// </summary>
        public override void Initialize()
        {
            softCircleTexture = new Texture2D(GraphicsDevice, diameter, diameter);
            Color[] data = new Color[diameter * diameter];

            float r = diameter / 2f;

            int steps = granularity_;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy) / r;

                    // Snap to blocky levels
                    float stepped = (float)Math.Floor(dist * steps) / steps;
                    float alpha = 1f - MathHelper.Clamp(stepped, 0f, 1f);

                    data[y * diameter + x] = new Color(1f, 1f, 1f, alpha * alpha);
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
        /// Updates the point light component. This can be used to update the light's properties or behavior, such as flickering.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            // Apply flicker effect if enabled
            float flickerIntensity = FlickerIntensity * Lighting.Instance.Wind;
            finalRadius = lightRadius+MathF.Sin(gameTime.TotalGameTime.Milliseconds * dt) * flickerIntensity;
            finalIntensity = lightIntensity;

            // Apply granularity
            if (granularity_ != Lighting.Instance.Granularity)
            {
                Granularity = Lighting.Instance.Granularity;
            }
        }

        /// <summary>
        /// Draws the point light to the screen using a soft circle texture.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void DrawLight(SpriteBatch spriteBatch)
        {
            float depth = ((Owner.Height) * 10000f) / 100000f; // Adjust divisor to fit your world

            spriteBatch.Draw(
                GraphicsLibrary.DiscTexture,
                new Vector2(Position.X, Position.Y) + new Vector2(Offset.X, Offset.Y),
                null,
                lightColor * finalIntensity,
                0f,
                new Vector2(diameter / 2, diameter / 2),
                finalRadius / diameter,
                SpriteEffects.None,
                depth);
        }
    }
}

