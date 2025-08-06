using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ParticleEmitter.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

namespace Engine.Core.Game.Components
{
    /// <summary>
    /// Represents a single particle in the game, with properties for position, velocity, rotation, lifetime, color, scale, and texture.
    /// </summary>
    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;
        public float AngularVelocity;
        public float Lifetime;
        public float Age;
        public Color Color;
        public float Scale;
        private float FinalScale;
        public bool IsAlive => Age < Lifetime/60f;
        public Texture2D? Texture = GraphicsLibrary.SquareTexture;
        public float Depth;
        private float PercentDone => Age / (Lifetime / 60f); // Lifetime in seconds, Age in seconds

        /// <summary>
        /// Represents a single particle in the game, with properties for position, velocity, lifetime, and texture.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Age += dt;

            // Update position based on velocity
            Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update rotation based on angular velocity
            Rotation += AngularVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Scale down over lifetime
            FinalScale = Scale * (1f - PercentDone);
        }

        /// <summary>
        /// Draws the particle using the provided SpriteBatch.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Primitive textures
            spriteBatch.Draw(Texture, Position, null, Color, Rotation, new Vector2(0.5f,0.5f), FinalScale, SpriteEffects.None, Depth);
        }
    }
} 
