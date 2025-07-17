using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Game;
using Engine.Core.Systems.Rooms;

namespace Engine.Core.Systems.Graphics
{
    public class Lighting2D
    {
        private GraphicsDevice graphicsDevice;
        private RenderTarget2D lightmap;
        private Texture2D whitePixel;
        private int width, height;

        public static readonly BlendState MultiplyBlend = new BlendState
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.Zero,
            AlphaBlendFunction = BlendFunction.Add
        };

        public Lighting2D(GraphicsDevice gd, int width, int height)
        {
            graphicsDevice = gd;
            this.width = width;
            this.height = height;
            lightmap = new RenderTarget2D(gd, width, height);
            whitePixel = new Texture2D(gd, 1, 1);
            whitePixel.SetData(new[] { Color.White });
        }
     
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
                Console.WriteLine("[Lighting2D] Critical components are null, skipping draw");
                return;
            }

            // Draw the lightmap (no shadow mask, no shader)
            graphicsDevice.SetRenderTarget(lightmap);
            graphicsDevice.Clear(Color.Black);

            // Draw ambient light first (gray background so scene isn't completely black)
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(whitePixel, new Rectangle(0, 0, width, height), Color.Gray);
            spriteBatch.End();

            // Draw all lights as soft circles (additive blending)
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