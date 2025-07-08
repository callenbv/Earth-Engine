using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Game;

namespace Engine.Core.Systems.Graphics
{
    public class LightSource
    {
        public Vector2 Position;
        public float Radius;
        public Color Color = Color.White;
        public float Intensity = 1f;
    }

    public class Lighting2D
    {
        public List<LightSource> Lights = new();

        private GraphicsDevice graphicsDevice;
        private RenderTarget2D lightmap;
        private Texture2D whitePixel;
        private Texture2D? softCircleTexture;
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

        private void EnsureSoftCircleTexture(int diameter)
        {
            if (graphicsDevice == null)
            {
                Console.WriteLine("[Lighting2D] GraphicsDevice is null, cannot create soft circle texture");
                return;
            }
            
            if (diameter <= 0)
            {
                Console.WriteLine("[Lighting2D] Invalid diameter for soft circle texture: " + diameter);
                return;
            }
            
            if (softCircleTexture != null && !softCircleTexture.IsDisposed && softCircleTexture.Width == diameter)
                return;
                
            softCircleTexture?.Dispose();
            softCircleTexture = new Texture2D(graphicsDevice, diameter, diameter);
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
        /// Get all lights from game objects and populate the lighting system
        /// </summary>
        /// <param name="lighting">The lighting system to populate</param>
        public void GetLights()
        {
            Lights.Clear();

            foreach (var gameObj in GameObjectManager.Main.GetAllObjects())
            {
                foreach (var script in gameObj.scriptInstances)
                {
                    if (script is PointLight light)
                    {
                        if (light.lightRadius > 0 && light.lightIntensity > 0)
                        {
                            var pointLight = new LightSource
                            {
                                Position = gameObj.position,
                                Radius = light.lightRadius,
                                Color = light.lightColor,
                                Intensity = light.lightIntensity
                            };

                            Lights.Add(pointLight);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draw the lighting layer
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Get the lights in the scene
            GetLights();

            // Safety check
            if (spriteBatch == null || graphicsDevice == null || lightmap == null)
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
            foreach (var light in Lights)
            {
                if (light == null) continue;
                int diameter = Math.Max(1, (int)(light.Radius * 2));
                EnsureSoftCircleTexture(diameter);
                if (softCircleTexture != null)
                {
                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
                    spriteBatch.Draw(
                        softCircleTexture,
                        light.Position,
                        null,
                        light.Color * light.Intensity,
                        0f,
                        new Vector2(diameter / 2f, diameter / 2f),
                        1f,
                        SpriteEffects.None,
                        0f);
                    spriteBatch.End();
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