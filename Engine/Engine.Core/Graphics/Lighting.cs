/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Lighting2D.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Game;
using Engine.Core.Rooms;

namespace Engine.Core.Graphics
{
    public class Lighting
    {
        private GraphicsDevice graphicsDevice;
        private RenderTarget2D lightmap;
        private Texture2D whitePixel;
        private int width, height;
        public static Lighting Instance { get; private set; } = null!;
        public Color AmbientLightColor { get; set; } = Color.White;
        public float AmbientLightIntensity { get; set; } = 0.1f;
        public bool Enabled { get; set; } = false;
        public int Granularity { get; set; } = 10;
        public float Wind { get; set; } = 1f;

        /// <summary>
        /// Blend state for soft circle lights (additive blending).
        /// </summary>
        public static readonly BlendState MultiplyBlend = new BlendState
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.Zero,
            AlphaBlendFunction = BlendFunction.Add
        };

        /// <summary>
        /// Blend state for glowing additive effects (e.g., lights, particles).
        /// </summary>
        public static readonly BlendState AlphaAdditiveBlend = new BlendState
        {
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.Add,

            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.Zero,
            AlphaBlendFunction = BlendFunction.Add
        };

        /// <summary>
        /// Blend state for additive blending, useful for effects like fire or explosions.
        /// </summary>
        public static readonly BlendState AdditiveBlend = new BlendState
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.Add,

            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add
        };

        /// <summary>
        /// Initialize the lighting system with a graphics device and dimensions for the lightmap.
        /// </summary>
        /// <param name="gd"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public Lighting(GraphicsDevice gd, int width, int height)
        {
            graphicsDevice = gd;
            this.width = width;
            this.height = height;
            lightmap = new RenderTarget2D(gd, width, height);
            whitePixel = new Texture2D(gd, 1, 1);
            whitePixel.SetData(new[] { Color.White });
            Instance = this;
        }

        /// <summary>
        /// Resize the lightmap to new dimensions.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Resize(int width, int height)
        {
            this.width = width;
            this.height = height;
            lightmap?.Dispose();
            lightmap = new RenderTarget2D(graphicsDevice, width, height);
        }

        /// <summary>
        /// Draw the lighting layer
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(Room scene, SpriteBatch spriteBatch)
        {
            // Safety check
            if (spriteBatch == null || scene == null || graphicsDevice == null || lightmap == null)
            {
                return;
            }

            // Draw the lightmap (no shadow mask, no shader)
            graphicsDevice.SetRenderTarget(lightmap);
            graphicsDevice.Clear(Color.Black);

            // Draw ambient light first (gray background so scene isn't completely black)
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(whitePixel, new Rectangle(0, 0, width, height), AmbientLightColor);
            spriteBatch.End();

            // Draw all lights as soft circles (additive blending)
            if (Enabled)
            {
                foreach (var gameObj in scene.objects)
                {
                    foreach (var component in gameObj.components)
                    {
                        if (component is PointLight light)
                        {
                            if (light.lightRadius > 0 && light.lightIntensity > 0)
                            {
                                light.DrawLight(spriteBatch);
                            }
                        }
                    }
                }
            }

            graphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Get lightmap to draw it
        /// </summary>
        /// <returns></returns>
        public RenderTarget2D GetLightmap()
        {
            return lightmap;
        }
    }
}
