/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ParticleEmitter.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.CustomMath;
using Engine.Core.Data;
using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json.Serialization;

namespace Engine.Core.Game.Components
{
    /// <summary>
    /// Represents the shape of a particle. This can be extended to include different shapes like Circle, Square, etc.
    /// </summary>
    public enum ParticleShape
    {
        Square, // Default square shape
        Circle, // Circle shape
        Disc    // Disc shape with fading alpha
    }

    /// <summary>
    /// Represents a 2D particle emitter that emits particles in a specified direction with a defined velocity and lifetime.
    /// </summary>
    [ComponentCategory("Particles")]
    public class ParticleEmitter : ObjectComponent
    {
        public override string Name => "Particle Emitter 2D";
        public override bool UpdateInEditor => true;

        /// <summary>
        /// Whether to emit particles in a burst mode. If true, particles will be emitted immediately upon creation.
        /// </summary>
        public bool Burst { get; set; } = false;

        /// <summary>
        /// Number of particles to emit in burst mode. This is only used if Burst is true.
        /// </summary>
        public int BurstCount { get; set; } = 10;

        /// <summary>
        /// Properties for controlling the particle emission rate, lifetime, speed, direction, color, offset, and size.
        /// </summary>
        [SliderEditor(0, 100)]
        public float SpawnRate { get; set; } = 1f;

        /// <summary>
        /// Lifetime of the particle in miliseconds.
        /// </summary>
        [SliderEditor(0, 600)]
        public float Lifetime { get; set; } = 60f;

        /// <summary>
        /// Speed and direction of the particles emitted by this emitter.
        /// </summary>
        [SliderEditor(0, 100)]
        public float Speed { get; set; } = 100f;

        /// <summary>
        /// Direction of emission in degrees.
        /// </summary>
        [SliderEditor(0, 360)]
        public float Direction { get; set; } = 0f;

        /// <summary>
        /// Direction wiggle in degrees. This adds randomness to the direction of emitted particles.
        /// </summary>
        [SliderEditor(0, 180)]
        public float DirectionWiggle { get; set; } = 0f;

        /// <summary>
        /// The shape of the particle, which determines its texture and appearance.
        /// </summary>
        public ParticleShape Shape
        {
            get => shape_;
            set
            {
                shape_ = value;
                switch (shape_)
                {
                    case ParticleShape.Square:
                        texture = GraphicsLibrary.SquareTexture;
                        break;
                    case ParticleShape.Circle:
                        texture = GraphicsLibrary.CircleTexture;
                        break;
                    case ParticleShape.Disc:
                        texture = GraphicsLibrary.DiscTexture;
                        break;
                    default:
                        texture = GraphicsLibrary.PixelTexture; // Default to pixel if unknown shape
                        break;
                }
            }
        }
        private ParticleShape shape_;

        /// <summary>
        /// Texture of the particle. Allows for sprite based particles or changes based on shape
        /// </summary>
        [JsonIgnore]
        public Texture2D Texture
        {
            get => texture;
            set
            {
                texture = value;

                if (texture != null)
                {
                    TexturePath = texture.Name;
                }
            }
        }
        private Texture2D texture = GraphicsLibrary.SquareTexture;

        /// <summary>
        /// Texture path to store reference to texture
        /// </summary>
        [HideInInspector]
        public string TexturePath { get; set; } = string.Empty;

        /// <summary>
        /// Color of the particles emitted by this emitter.
        /// </summary>
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// Offset from the emitter position where particles will be emitted.
        /// </summary>
        public Vector3 Offset { get; set; } = Vector3.Zero;

        /// <summary>
        /// Size of the particles emitted by this emitter. This is used to define the size of the particle texture.
        /// </summary>
        [SliderEditor(0, 32)]
        public float ParticleSize { get; set; } = 1f;

        /// <summary>
        /// Whether the emitter is enabled or not. If false, no particles will be emitted.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Visibility of particles
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Determines depth based on tilemaps
        /// </summary>
        public int Height { get; set; } = 0;

        /// <summary>
        /// Whether or not to show the emitter's bounds
        /// </summary>
        public bool ShowBounds { get; set; } = true;

        /// <summary>
        /// Size of the emitter area from which particles will be emitted. This defines the 3D box in which particles can spawn.
        /// </summary>
        public Vector3 EmitterSize { get; set; } = new Vector3(32, 32, 32);

        /// <summary>
        /// List of particles currently emitted by this emitter. This will hold all active particles that are being updated and drawn.
        /// </summary>
        private List<Particle> particles = new List<Particle>();

        /// <summary>
        /// Elapsed time since the last particle emission. This is used to control the emission rate based on SpawnRate.
        /// </summary>
        private float ElapsedTime = 0f;

        /// <summary>
        /// Effect for 3D particle rendering
        /// </summary>
        [JsonIgnore]
        private Effect? _particleEffect;

        /// <summary>
        /// Set the texture
        /// </summary>
        public override void Initialize()
        {
            // Use default texture if TexturePath is null or empty
            if (string.IsNullOrEmpty(TexturePath))
            {
                // Preserve the existing Shape setting instead of forcing it to Square
                // This ensures the shape set in the editor is maintained during gameplay
                Console.WriteLine($"[ParticleEmitter] Using {Shape} texture for {Owner?.Name ?? "Unknown"} (TexturePath was null/empty)");
            }
            else
            {
                try
                {
                    Texture = TextureLibrary.Instance.Get(TexturePath);
                    Console.WriteLine($"[ParticleEmitter] Loaded texture '{TexturePath}' for {Owner?.Name ?? "Unknown"}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ParticleEmitter] Failed to load texture '{TexturePath}' for {Owner?.Name ?? "Unknown"}: {ex.Message}");
                    // Preserve the existing Shape setting instead of forcing it to Square
                    Console.WriteLine($"[ParticleEmitter] Using {Shape} texture as fallback for {Owner?.Name ?? "Unknown"}");
                }
            }
        }

        /// <summary>
        /// Update the particle emitter.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            foreach (var particle in particles)
            {
                particle.Update(gameTime);
            }

            // Remove dead particles
            particles.RemoveAll(p => !p.IsAlive);

            // Emit new particles based on spawn rate
            if (Burst)
            {
                for (int i = 0; i < BurstCount; i++)
                {
                    EmitParticle();
                }
                Burst = false; // Reset burst after emitting
            }
            else
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                ElapsedTime += dt;

                if (ElapsedTime >= (1f / SpawnRate))
                {
                    for (int i = 0; i < 1; i++)
                    {
                        EmitParticle();
                    }
                    ElapsedTime = 0f;
                }
            }
        }

        /// <summary>
        /// Emit a new particle from the emitter.
        /// </summary>
        public void EmitParticle()
        {
            if (!Enabled)
                return;

            // Get Transform component for 3D positioning
            var transform = Owner.GetComponent<Transform>();
            Vector3 emitterPos = transform?.Position ?? new Vector3(Position.X, Position.Y, Position.Z);

            // Calculate the center of the emitter box and randomize within the box
            Vector3 boxCenter = emitterPos + Offset;
            Vector3 halfSize = EmitterSize * 0.5f;
            Vector3 finalPosition = boxCenter + new Vector3(
                ERandom.Range(-halfSize.X, halfSize.X),
                ERandom.Range(-halfSize.Y, halfSize.Y),
                ERandom.Range(-halfSize.Z, halfSize.Z)
            );

            float finalDirection = Direction + ERandom.Range(-DirectionWiggle, DirectionWiggle);
            float scaleFalloff = ParticleSize / texture.Width;

            float depth = Height + Owner.Height;

            if (Owner.GetComponent<Sprite2D>() != null)
            {
                depth = Owner.GetComponent<Sprite2D>().depth;
            }

            Particle particle = new Particle
            {
                Position = finalPosition,
                Velocity = new Vector3((float)Math.Cos(MathHelper.ToRadians(finalDirection)) * Speed,
                                       (float)Math.Sin(MathHelper.ToRadians(finalDirection)) * Speed,
                                       0f), // No Z velocity by default
                Lifetime = Lifetime,
                Age = 0f,
                Color = Color, // Default color
                Scale = scaleFalloff, // Default scale
                Depth = depth,
                Texture = texture // Assign a texture if needed
            };
            particles.Add(particle);
        }

        /// <summary>
        /// Draw the particle emitter and its particles using the provided SpriteBatch.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible || particles.Count == 0)
                return;

            foreach (var particle in particles)
            {
                particle.Draw(spriteBatch);
            }

            if (ShowBounds && !EngineContext.Running)
            {
                DebugDraw(spriteBatch);
            }
        }

        /// <summary>
        /// Draw particles in 3D space using BasicEffect
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        public void Draw3D(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
        {
            if (!Visible || particles.Count == 0)
                return;

            // Initialize effect if needed
            if (_particleEffect == null)
            {
                _particleEffect = new AlphaTestEffect(graphicsDevice)
                {
                    VertexColorEnabled = true,
                    AlphaFunction = CompareFunction.Greater,
                    ReferenceAlpha = 1 // Only discard completely transparent pixels (alpha = 0)
                };
            }

            // Set render states for alpha blending
            var originalBlendState = graphicsDevice.BlendState;
            var originalDepthStencilState = graphicsDevice.DepthStencilState;
            var originalRasterizerState = graphicsDevice.RasterizerState;

            try
            {
                graphicsDevice.BlendState = BlendState.AlphaBlend; // Add colors together for proper particle blending
                graphicsDevice.DepthStencilState = DepthStencilState.Default; // Use default depth testing for proper ordering
                graphicsDevice.RasterizerState = RasterizerState.CullNone;

                // Sort particles by depth (back to front) for proper alpha blending
                var sortedParticles = particles.OrderByDescending(p => p.Depth).ToList();

                // Draw all particles using the shared effect
                foreach (var particle in sortedParticles)
                {
                    particle.Draw3D(graphicsDevice, view, projection, _particleEffect);
                }
            }
            finally
            {
                // Restore render states
                graphicsDevice.BlendState = originalBlendState;
                graphicsDevice.DepthStencilState = originalDepthStencilState;
                graphicsDevice.RasterizerState = originalRasterizerState;
            }
        }

        /// <summary>
        /// Draw bounds in the UI
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void DrawUI(SpriteBatch spriteBatch)
        {

        }

        /// <summary>
        /// Draw debug bounds in 3D space
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        public void Draw3DBounds(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
        {
            if (!ShowBounds || EngineContext.Running)
                return;

            var transform = Owner.GetComponent<Transform>();
            if (transform == null) return;

            Vector3 emitterPos = transform.Position;
            Vector3 worldPos = new Vector3(
                emitterPos.X / EngineContext.UnitsPerPixel,
                -emitterPos.Y / EngineContext.UnitsPerPixel,
                emitterPos.Z / EngineContext.UnitsPerPixel
            );

            Vector3 offset3D = new Vector3(
                Offset.X / EngineContext.UnitsPerPixel,
                -Offset.Y / EngineContext.UnitsPerPixel,
                Offset.Z / EngineContext.UnitsPerPixel
            );

            Vector3 size3D = new Vector3(
                EmitterSize.X / EngineContext.UnitsPerPixel,
                EmitterSize.Y / EngineContext.UnitsPerPixel,
                EmitterSize.Z / EngineContext.UnitsPerPixel
            );

            // Draw 3D wireframe box
            DrawWireframeBox(graphicsDevice, view, projection, worldPos + offset3D, size3D, Color.Yellow);
        }

        /// <summary>
        /// Draw a wireframe box in 3D space
        /// </summary>
        private void DrawWireframeBox(GraphicsDevice graphicsDevice, Matrix view, Matrix projection, Vector3 position, Vector3 size, Color color)
        {
            // Create wireframe effect if needed
            if (_wireframeEffect == null)
            {
                _wireframeEffect = new BasicEffect(graphicsDevice)
                {
                    VertexColorEnabled = true,
                    LightingEnabled = false
                };
            }

            // Calculate box corners
            Vector3 halfSize = size * 0.5f;
            Vector3[] corners = new Vector3[8]
            {
                position + new Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z), // 0: bottom-left-back
                position + new Vector3( halfSize.X, -halfSize.Y, -halfSize.Z), // 1: bottom-right-back
                position + new Vector3( halfSize.X,  halfSize.Y, -halfSize.Z), // 2: top-right-back
                position + new Vector3(-halfSize.X,  halfSize.Y, -halfSize.Z), // 3: top-left-back
                position + new Vector3(-halfSize.X, -halfSize.Y,  halfSize.Z), // 4: bottom-left-front
                position + new Vector3( halfSize.X, -halfSize.Y,  halfSize.Z), // 5: bottom-right-front
                position + new Vector3( halfSize.X,  halfSize.Y,  halfSize.Z), // 6: top-right-front
                position + new Vector3(-halfSize.X,  halfSize.Y,  halfSize.Z)  // 7: top-left-front
            };

            // Define edges (pairs of corner indices)
            int[] edges = new int[24]
            {
                // Back face
                0, 1, 1, 2, 2, 3, 3, 0,
                // Front face
                4, 5, 5, 6, 6, 7, 7, 4,
                // Connecting edges
                0, 4, 1, 5, 2, 6, 3, 7
            };

            // Create vertices for all edges
            var vertices = new VertexPositionColor[edges.Length];
            for (int i = 0; i < edges.Length; i++)
            {
                vertices[i] = new VertexPositionColor(corners[edges[i]], color);
            }

            // Set up effect
            _wireframeEffect.World = Matrix.Identity;
            _wireframeEffect.View = view;
            _wireframeEffect.Projection = projection;

            // Draw wireframe
            foreach (EffectPass pass in _wireframeEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, vertices.Length / 2);
            }
        }

        /// <summary>
        /// BasicEffect for wireframe rendering
        /// </summary>
        [JsonIgnore]
        private BasicEffect? _wireframeEffect;

        /// <summary>
        /// Draw debug bounds (legacy 2D method - kept for compatibility)
        /// </summary>
        /// <param name="spriteBatch"></param>
        private void DebugDraw(SpriteBatch spriteBatch)
        {
            if (!ShowBounds || EngineContext.Running)
                return;

            Vector3 topLeft = Position + Offset;
            Rectangle boundingBox = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)EmitterSize.X, (int)EmitterSize.Y);
            Color yellow = Color.FromNonPremultiplied(255, 255, 0, 100);

            // Draw a rectangle outline of the bounds, NOT a pure rectangle
            spriteBatch.Draw(GraphicsLibrary.PixelTexture, new Rectangle(boundingBox.X, boundingBox.Y, boundingBox.Width, 1), yellow); // Top
            spriteBatch.Draw(GraphicsLibrary.PixelTexture, new Rectangle(boundingBox.X, boundingBox.Y + boundingBox.Height - 1, boundingBox.Width, 1), yellow); // Bottom
            spriteBatch.Draw(GraphicsLibrary.PixelTexture, new Rectangle(boundingBox.X, boundingBox.Y, 1, boundingBox.Height), yellow); // Left
            spriteBatch.Draw(GraphicsLibrary.PixelTexture, new Rectangle(boundingBox.X + boundingBox.Width - 1, boundingBox.Y, 1, boundingBox.Height), yellow); // Right
        }
    }
} 
