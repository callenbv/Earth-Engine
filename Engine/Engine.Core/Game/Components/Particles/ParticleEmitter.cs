/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ParticleEmitter.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.CustomMath;
using Engine.Core.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Game.Components
{
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
        public bool Burst { get; set; } = false; // If true, emit particles immediately on creation

        /// <summary>
        /// Number of particles to emit in burst mode. This is only used if Burst is true.
        /// </summary>
        public int BurstCount { get; set; } = 10; // Number of particles to emit in burst

        /// <summary>
        /// Properties for controlling the particle emission rate, lifetime, speed, direction, color, offset, and size.
        /// </summary>
        [SliderEditor(0, 100)]
        public float SpawnRate { get; set; } = 1f; // Particles per second

        /// <summary>
        /// Lifetime of the particlle
        /// </summary>
        [SliderEditor(0, 60)]
        public float Lifetime { get; set; } = 2f; // Lifetime of each particle in seconds

        /// <summary>
        /// Speed and direction of the particles emitted by this emitter.
        /// </summary>
        [SliderEditor(0, 100)]
        public float Speed { get; set; } = 100f; // Speed of particles in pixels per second

        /// <summary>
        /// Direction of emission in degrees.
        /// </summary>
        [SliderEditor(0, 360)]
        public float Direction { get; set; } = 0f; // Angle of emission in degrees
        public float DirectionWiggle { get; set; } = 0f; // Angle of emission wiggle in degrees, can be used to add randomness to the direction

        /// <summary>
        /// Color of the particles emitted by this emitter.
        /// </summary>
        public Color Color { get; set; } = Color.White; // Color of emitted particles

        /// <summary>
        /// Offset from the emitter position where particles will be emitted.
        /// </summary>
        public Vector2 Offset { get; set; } = Vector2.Zero; // Offset from emitter position

        /// <summary>
        /// Size of the particles emitted by this emitter.
        /// </summary>
        [SliderEditor(1,10)]
        public float ParticleSize { get; set; } = 1f; // Scale of emitted particles

        /// <summary>
        /// Whether the emitter is enabled or not. If false, no particles will be emitted.
        /// </summary>
        public bool Enabled { get; set; } = true; // Whether the emitter is enabled or not

        /// <summary>
        /// Size of the emitter area from which particles will be emitted. This defines the rectangular area in which particles can spawn.
        /// </summary>
        public Vector2 EmitterSize { get; set; } = new Vector2(32, 32); // Size of the emitter area

        private List<Particle> particles = new List<Particle>(); // List of particles currently emitted by this emitter
        private float ElapsedTime = 0f; // Time elapsed since last particle emission

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

                if (ElapsedTime > (60f/SpawnRate)*dt)
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

            Particle particle = new Particle
            {
                Position = FinalPosition,
                Velocity = new Vector2((float)Math.Cos(MathHelper.ToRadians(finalDirection)) * Speed,
                                       (float)Math.Sin(MathHelper.ToRadians(finalDirection)) * Speed),
                Lifetime = Lifetime,
                Age = 0f,
                Color = Color, // Default color
                Scale = ParticleSize, // Default scale
                Depth = 1f,
                Texture = null // Assign a texture if needed
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
                return; // If the emitter is not enabled, skip updating

            foreach (var particle in particles)
            {
                particle.Draw(spriteBatch);
            }
        }
    }
} 
