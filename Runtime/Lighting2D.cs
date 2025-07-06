using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Content;

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
        public List<Vector2> Vertices; // Polygon or line segment (2 points)
        public Vector2 Position; // Position of the occluder in world space
        public Texture2D Sprite; // The sprite texture for pixel-perfect occlusion
    }

    public class Lighting2D
    {
        public List<LightSource> Lights = new();
        public List<Occluder> Occluders = new();

        private GraphicsDevice graphicsDevice;
        private RenderTarget2D lightmap;
        private Texture2D whitePixel;
        private Texture2D softCircleTexture;
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

        public static List<Vector2> ComputeLitPolygon(LightSource light, List<Occluder> occluders, int rayCount = 256)
        {
            var points = new List<(float angle, Vector2 pt)>();
            var uniqueAngles = new HashSet<float>();

            // Cast rays to all occluder vertices (and a few extra angles for smoothness)
            foreach (var occ in occluders)
            {
                foreach (var v in occ.Vertices)
                {
                    float angle = (float)Math.Atan2(v.Y - light.Position.Y, v.X - light.Position.X);
                    for (int d = -1; d <= 1; d++) // Slightly offset angles for smoothness
                    {
                        float a = angle + d * 0.0001f;
                        if (uniqueAngles.Add(a))
                        {
                            var hitPoint = CastRay(light.Position, a, occ, light.Radius);
                            if (hitPoint.HasValue)
                            {
                                points.Add((a, hitPoint.Value));
                            }
                        }
                    }
                }
            }

            // Add some extra rays for smoothness
            for (int i = 0; i < rayCount; i++)
            {
                float angle = (i * 2 * MathHelper.Pi) / rayCount;
                if (uniqueAngles.Add(angle))
                {
                    var hitPoint = CastRay(light.Position, angle, occluders, light.Radius);
                    if (hitPoint.HasValue)
                    {
                        points.Add((angle, hitPoint.Value));
                    }
                }
            }

            // Sort by angle and create polygon
            points.Sort((a, b) => a.angle.CompareTo(b.angle));
            return points.Select(p => p.pt).ToList();
        }

        private static Vector2? CastRay(Vector2 origin, float angle, List<Occluder> occluders, float maxDistance)
        {
            Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 end = origin + direction * maxDistance;
            Vector2? closestHit = null;
            float closestDistance = maxDistance;

            foreach (var occluder in occluders)
            {
                var verts = occluder.Vertices;
                if (verts == null || verts.Count < 2) continue;
                for (int i = 0; i < verts.Count; i++)
                {
                    Vector2 a = verts[i];
                    Vector2 b = verts[(i + 1) % verts.Count];
                    var intersection = LineIntersectsLine(origin, end, a, b);
                    if (intersection.HasValue)
                    {
                        float distance = Vector2.Distance(origin, intersection.Value);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestHit = intersection.Value;
                            Console.WriteLine($"[Ray Debug] Ray from {origin} hit edge ({a}, {b}) at {intersection.Value}");
                        }
                    }
                }
            }
            return closestHit ?? end;
        }

        private static Vector2? CastRay(Vector2 origin, float angle, Occluder occluder, float maxDistance)
        {
            Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 end = origin + direction * maxDistance;
            Vector2? closestHit = null;
            float closestDistance = maxDistance;
            var verts = occluder.Vertices;
            if (verts == null || verts.Count < 2) return end;
            for (int i = 0; i < verts.Count; i++)
            {
                Vector2 a = verts[i];
                Vector2 b = verts[(i + 1) % verts.Count];
                var intersection = LineIntersectsLine(origin, end, a, b);
                if (intersection.HasValue)
                {
                    float distance = Vector2.Distance(origin, intersection.Value);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestHit = intersection.Value;
                    }
                }
            }
            return closestHit ?? end;
        }

        private static Vector2? LineIntersectsLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            Vector2 b = a2 - a1;
            Vector2 d = b2 - b1;
            float bDotDPerp = b.X * d.Y - b.Y * d.X;

            if (bDotDPerp == 0)
                return null;

            Vector2 c = b1 - a1;
            float t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
            if (t < 0 || t > 1)
                return null;

            float u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
            if (u < 0 || u > 1)
                return null;

            return a1 + t * b;
        }

        // --- Triangle Drawing Helper ---
        public static void DrawTriangle(SpriteBatch sb, Texture2D tex, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            // Simple triangle filling using multiple line draws
            // This is a basic approach - for better performance, use a custom shader
            
            // Calculate bounding box
            float minX = Math.Min(Math.Min(a.X, b.X), c.X);
            float maxX = Math.Max(Math.Max(a.X, b.X), c.X);
            float minY = Math.Min(Math.Min(a.Y, b.Y), c.Y);
            float maxY = Math.Max(Math.Max(a.Y, b.Y), c.Y);
            
            // Fill the triangle by drawing horizontal lines
            for (int y = (int)minY; y <= (int)maxY; y++)
            {
                var intersections = new List<float>();
                
                // Find intersections with each edge
                if (LineIntersectsHorizontal(a, b, y, out float x1)) intersections.Add(x1);
                if (LineIntersectsHorizontal(b, c, y, out float x2)) intersections.Add(x2);
                if (LineIntersectsHorizontal(c, a, y, out float x3)) intersections.Add(x3);
                
                if (intersections.Count >= 2)
                {
                    intersections.Sort();
                    float startX = intersections[0];
                    float endX = intersections[intersections.Count - 1];
                    
                    // Draw horizontal line segment
                    if (endX > startX)
                    {
                        sb.Draw(tex, new Vector2(startX, y), null, color, 0f, Vector2.Zero, 
                            new Vector2(endX - startX, 1f), SpriteEffects.None, 0f);
                    }
                }
            }
        }

        private static bool LineIntersectsHorizontal(Vector2 a, Vector2 b, float y, out float x)
        {
            x = 0;
            
            // Check if line crosses the horizontal line at y
            if ((a.Y <= y && b.Y >= y) || (a.Y >= y && b.Y <= y))
            {
                if (Math.Abs(b.Y - a.Y) > 0.001f) // Avoid division by zero
                {
                    float t = (y - a.Y) / (b.Y - a.Y);
                    x = a.X + t * (b.X - a.X);
                    return true;
                }
            }
            return false;
        }

        public RenderTarget2D GetLightmap()
        {
            return lightmap;
        }

        public static Texture2D GenerateSilhouette(GraphicsDevice gd, Texture2D source)
        {
            Color[] srcData = new Color[source.Width * source.Height];
            source.GetData(srcData);
            Color[] maskData = new Color[srcData.Length];
            for (int i = 0; i < srcData.Length; i++)
            {
                // If the source pixel is mostly opaque, make it white; else black
                maskData[i] = srcData[i].A > 32 ? Color.White : Color.Black;
            }
            Texture2D mask = new Texture2D(gd, source.Width, source.Height);
            mask.SetData(maskData);
            return mask;
        }
    }
} 