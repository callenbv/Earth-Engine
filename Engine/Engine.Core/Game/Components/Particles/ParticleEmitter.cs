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
        /// Texture used for rendering the particles. This is set based on the ParticleShape and can be changed dynamically.
        /// </summary>
        private Texture2D texture = GraphicsLibrary.SquareTexture;

        /// <summary>
        /// Color of the particles emitted by this emitter.
        /// </summary>
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// Offset from the emitter position where particles will be emitted.
        /// </summary>
        public Vector2 Offset { get; set; } = Vector2.Zero;

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
        /// Whether or not to show the emitter's bounds
        /// </summary>
        public bool ShowBounds { get; set; } = true;

        /// <summary>
        /// Size of the emitter area from which particles will be emitted. This defines the rectangular area in which particles can spawn.
        /// </summary>
        public Vector2 EmitterSize { get; set; } = new Vector2(32, 32);

        /// <summary>
        /// List of particles currently emitted by this emitter. This will hold all active particles that are being updated and drawn.
        /// </summary>
        private List<Particle> particles = new List<Particle>();

        /// <summary>
        /// Elapsed time since the last particle emission. This is used to control the emission rate based on SpawnRate.
        /// </summary>
        private float ElapsedTime = 0f;

        /// <summary>
        /// Update the particle emitter.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            if (!Enabled)
                return; // If the emitter is not enabled, skip updating

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

                if (ElapsedTime > (60f / SpawnRate) * dt)
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
            Vector2 FinalPosition = Position + Offset + new Vector2(ERandom.Range(0, EmitterSize.X),
                                                ERandom.Range(0, EmitterSize.Y));

            float finalDirection = Direction + ERandom.Range(-DirectionWiggle, DirectionWiggle);
            float scaleFalloff = ParticleSize / texture.Width;

            Particle particle = new Particle
            {
                Position = FinalPosition,
                Velocity = new Vector2((float)Math.Cos(MathHelper.ToRadians(finalDirection)) * Speed,
                                       (float)Math.Sin(MathHelper.ToRadians(finalDirection)) * Speed),
                Lifetime = Lifetime,
                Age = 0f,
                Color = Color, // Default color
                Scale = scaleFalloff, // Default scale
                Depth = 1f,
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
            if (!Enabled)
                return;

            // Draw the particles
            foreach (var particle in particles)
            {
                particle.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Draw bounds in the UI
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void DrawUI(SpriteBatch spriteBatch)
        {
            // Draw the bounding box of the emitter as an outline
            DebugDraw(spriteBatch);
        }

        /// <summary>
        /// Draw debug bounds
        /// </summary>
        /// <param name="spriteBatch"></param>
        private void DebugDraw(SpriteBatch spriteBatch)
        {
            if (!ShowBounds || EngineContext.Running)
                return;

            Vector2 topLeft = Position + Offset;
            Vector2 bottomRight = Position + Offset;
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
