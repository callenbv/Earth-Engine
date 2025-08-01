
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
        public bool IsAlive => Age < Lifetime;
        public Texture2D? Texture = null!;
        public float Depth;

        /// <summary>
        /// Represents a single particle in the game, with properties for position, velocity, lifetime, and texture.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            Age += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update position based on velocity
            Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update rotation based on angular velocity
            Rotation += AngularVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update color and scale based on age
            float lifeRatio = Age / Lifetime;
            Color = Color.Lerp(Color, Color.Transparent, lifeRatio);
            Scale = MathHelper.Lerp(Scale, 0f, lifeRatio); // Scale down over lifetime
        }

        /// <summary>
        /// Draws the particle using the provided SpriteBatch.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (Texture != null)
            {
                spriteBatch.Draw(Texture, Position, null, Color, Rotation, new Vector2(Texture.Width / 2f, Texture.Height / 2f), Scale, SpriteEffects.None, 0f);
            }
            else
            {
                // Draw a particle as a simple colored rectangle if no texture is assigned
                spriteBatch.Draw(GraphicsLibrary.PixelTexture, Position, null, Color, Rotation, new Vector2(0.5f,0.5f), Scale, SpriteEffects.None, Depth);
            }
        }
    }
} 
