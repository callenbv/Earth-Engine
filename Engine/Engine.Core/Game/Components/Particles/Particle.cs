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
        public Vector3 Position;
        public Vector3 Velocity;
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
            Position += Velocity * dt;

            // Update rotation based on angular velocity
            Rotation += AngularVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Scale down over lifetime
            FinalScale = Scale * (1f - PercentDone);
        }

        /// <summary>
        /// Draws the particle using the provided SpriteBatch (2D mode - uses X,Y only).
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Convert 3D position to 2D for legacy SpriteBatch rendering
            Vector2 pos2D = new Vector2(Position.X, Position.Y);
            spriteBatch.Draw(Texture, pos2D, null, Color, Rotation, new Vector2(0.5f,0.5f), FinalScale, SpriteEffects.None, Depth);
        }

        /// <summary>
        /// Draws the particle in 3D space using BasicEffect
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        /// <param name="effect"></param>
        public void Draw3D(GraphicsDevice graphicsDevice, Matrix view, Matrix projection, BasicEffect effect)
        {
            if (Texture == null) return;

            // Convert position to 3D world coordinates
            Vector3 worldPos = new Vector3(
                Position.X / Engine.Core.EngineContext.UnitsPerPixel,
                -Position.Y / Engine.Core.EngineContext.UnitsPerPixel,
                Position.Z / Engine.Core.EngineContext.UnitsPerPixel
            );

            // Calculate particle size in world units
            float particleSize = (Texture.Width * FinalScale) / Engine.Core.EngineContext.UnitsPerPixel;
            float halfSize = particleSize * 0.5f;

            // Create quad vertices
            var vertices = new VertexPositionColorTexture[4];
            var indices = new int[] { 0, 1, 2, 0, 2, 3 };

            // Create quad vertices (centered on particle position)
            vertices[0] = new VertexPositionColorTexture(new Vector3(-halfSize, halfSize, 0), Color, new Vector2(0, 0));
            vertices[1] = new VertexPositionColorTexture(new Vector3(halfSize, halfSize, 0), Color, new Vector2(1, 0));
            vertices[2] = new VertexPositionColorTexture(new Vector3(halfSize, -halfSize, 0), Color, new Vector2(1, 1));
            vertices[3] = new VertexPositionColorTexture(new Vector3(-halfSize, -halfSize, 0), Color, new Vector2(0, 1));

            // Apply rotation if needed
            if (Math.Abs(Rotation) > 0.001f)
            {
                Matrix rotationMatrix = Matrix.CreateRotationZ(Rotation);
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i].Position = Vector3.Transform(vertices[i].Position, rotationMatrix);
                }
            }

            // Create world matrix
            Matrix world = Matrix.CreateTranslation(worldPos);

            // Set effect properties
            effect.Texture = Texture;
            effect.World = world;
            effect.View = view;
            effect.Projection = projection;
            effect.TextureEnabled = true;
            effect.VertexColorEnabled = true;
            effect.LightingEnabled = false;

            // Render the particle
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
            }
        }
    }
} 
