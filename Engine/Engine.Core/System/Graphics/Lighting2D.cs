using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Content;
using Engine.Core.Game;
using System.Runtime.Serialization;

namespace GameRuntime
{
    public class LightSource
    {
        public Vector2 Position;
        public float Radius;
        public Color Color = Color.White;
        public float Intensity = 1f;
    }

    public class Occluder
    {
        public List<Vector2>? Vertices; // Polygon or line segment (2 points)
        public Vector2 Position; // Position of the occluder in world space
        public Texture2D? Sprite; // The sprite texture for pixel-perfect occlusion
    }

    public class Lighting2D
    {
        public List<LightSource> Lights = new();
        public List<Occluder> Occluders = new();

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

        // Alternative multiply blend that should work better
        public static readonly BlendState MultiplyBlendAlt = new BlendState
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendFunction.Add,
            AlphaSourceBlend = Blend.DestinationAlpha,
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

        public void EnsureWhitePixel()
        {
            if (whitePixel == null || whitePixel.IsDisposed)
            {
                Console.WriteLine("[Lighting2D] Recreating whitePixel texture!");
                whitePixel = new Texture2D(graphicsDevice, 1, 1);
                whitePixel.SetData(new[] { Color.White });
            }
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

        public void LoadContent(GraphicsDevice gd, ContentManager content, int width, int height)
        {
            // Load the shadow cast effect
            try
            {
                // shadowCastEffect = content.Load<Effect>("ShadowCast");
                // if (shadowCastEffect == null)
                //     Console.WriteLine("[Lighting2D] shadowCastEffect is null!");
                // else
                //     Console.WriteLine("[Lighting2D] shadowCastEffect loaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lighting2D] Failed to load shadowCastEffect: {ex.Message}");
                // shadowCastEffect = null;
            }
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
                // Check if the object has any scripts that emit light
                foreach (var script in gameObj.scriptInstances)
                {
                    if (script is Engine.Core.GameScript gameScript)
                    {
                        // Check if this script has lighting properties
                        var lightRadiusProperty = script.GetType().GetField("lightRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var lightIntensityProperty = script.GetType().GetField("lightIntensity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var lightColorProperty = script.GetType().GetField("lightColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (lightRadiusProperty != null && lightIntensityProperty != null && lightColorProperty != null)
                        {
                            var radius = (float)lightRadiusProperty.GetValue(script);
                            var intensity = (float)lightIntensityProperty.GetValue(script);
                            var colorName = (string)lightColorProperty.GetValue(script);

                            if (radius > 0 && intensity > 0)
                            {
                                // Parse color string to Color
                                var color = Color.White;
                                try
                                {
                                    var colorProperty = typeof(Microsoft.Xna.Framework.Color).GetProperty(colorName);
                                    if (colorProperty != null)
                                    {
                                        color = (Color)colorProperty.GetValue(null);
                                    }
                                }
                                catch
                                {
                                    // Default to white if color parsing fails
                                    color = Microsoft.Xna.Framework.Color.White;
                                }

                                var light = new LightSource
                                {
                                    Position = gameObj.position,
                                    Radius = radius,
                                    Color = color,
                                    Intensity = intensity
                                };

                                Lights.Add(light);
                            }
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

            // 1. Draw the lightmap (no shadow mask, no shader)
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
                        new Vector2(diameter / 2f, diameter / 2f), // Origin at center
                        1f,
                        SpriteEffects.None,
                        0f);
                    spriteBatch.End();
                }
            }

            graphicsDevice.SetRenderTarget(null);
        }

        public RenderTarget2D GetLightmap()
        {
            return lightmap;
        }
    }
} 