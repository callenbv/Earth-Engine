using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Content;
using Engine.Core.Game;

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
        //public void DrawShadowsFor(Light light, SpriteBatch spriteBatch)
        //{
        //    float maxDist = light.Radius * 2f;
        //    Color shadowColor = new Color(0, 0, 0, 200);

        //    // BasicEffect setup omitted for brevity…

        //    foreach (var oc in occluders)
        //    {
        //        // oc is AnimatedGameObject or something with silhouetteEdges
        //        foreach (var (localA, localB) in oc.silhouetteEdges)
        //        {
        //            // transform to world:
        //            var A = Vector2.Transform(localA, oc.WorldTransform);
        //            var B = Vector2.Transform(localB, oc.WorldTransform);

        //            // only cast from edges facing away from the light
        //            var edge = B - A;
        //            var normal = new Vector2(-edge.Y, edge.X);
        //            var mid = (A + B) * 0.5f;
        //            if (Vector2.Dot(normal, mid - light.Position) <= 0)
        //                continue;

        //            // extrude to "infinity"
        //            var dirA = Vector2.Normalize(A - light.Position);
        //            var dirB = Vector2.Normalize(B - light.Position);
        //            var farA = A + dirA * maxDist;
        //            var farB = B + dirB * maxDist;

        //            // build two triangles for the quad
        //            var verts = new[] {
        //                new VertexPositionColor(new Vector3(A,0),  shadowColor),
        //                new VertexPositionColor(new Vector3(B,0),  shadowColor),
        //                new VertexPositionColor(new Vector3(farB,0), shadowColor),

        //                new VertexPositionColor(new Vector3(A,0),  shadowColor),
        //                new VertexPositionColor(new Vector3(farB,0), shadowColor),
        //                new VertexPositionColor(new Vector3(farA,0), shadowColor),
        //            };

        //            // set your MultiplyBlend, RasterizerState, etc.
        //            graphicsDevice.BlendState = Lighting2D.MultiplyBlend;
        //            graphicsDevice.RasterizerState = RasterizerState.CullNone;
        //            graphicsDevice.DepthStencilState = DepthStencilState.None;

        //            // draw verts with your BasicEffect
        //            effect.CurrentTechnique.Passes[0].Apply();
        //            graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, verts, 0, 2);
        //        }
        //    }
        //}

        //public List<LightSource> GetLights()
        //{
        //    var lights = new List<LightSource>();

        //    foreach (var gameObj in gameObjects)
        //    {
        //        if (gameObj is AnimatedGameObject animObj && animObj.emitsLight)
        //        {
        //            var light = animObj.CreateLightSource();
        //            if (light != null)
        //            {
        //                lights.Add(light);
        //            }
        //        }
        //    }

        //    return lights;
        //}

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

        public void Draw(SpriteBatch spriteBatch)
        {
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

        public void DebugSaveLightmap()
        {
            // Save the lightmap to a file for inspection
            try
            {
                Color[] data = new Color[width * height];
                lightmap.GetData(data);
                
                // Check if the lightmap has any non-black pixels
                bool hasNonBlack = false;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i].R > 0 || data[i].G > 0 || data[i].B > 0 || data[i].A > 0)
                    {
                        hasNonBlack = true;
                        break;
                    }
                }
                
                Console.WriteLine($"[DEBUG] Lightmap size: {width}x{height}");
                Console.WriteLine($"[DEBUG] Lightmap has non-black pixels: {hasNonBlack}");
                Console.WriteLine($"[DEBUG] First few pixels: {data[0]}, {data[1]}, {data[2]}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error reading lightmap: {ex.Message}");
            }
        }
        public RenderTarget2D GetLightmap()
        {
            return lightmap;
        }
    }
} 