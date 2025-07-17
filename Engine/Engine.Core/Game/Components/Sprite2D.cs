using Engine.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Engine.Core.Game.Components
{
    public class Sprite2D : ObjectComponent
    {
        private Texture2D? texture;
        public override string Name => "Sprite 2D";
        public int frameWidth { get; set; } = 16;
        public int frameHeight { get; set; } = 16;
        public int frameCount { get; set; } = 1;
        public int frameSpeed = 1;
        public bool animated = false;
        public float depth = 0;

        public SpriteEffects spriteEffect = SpriteEffects.None;
        public Vector2 origin;

        private Rectangle spriteBox = new Rectangle();
        private int frame = 0;
        private float frameTimer = 0;

        /// <summary>
        /// Initialize
        /// </summary>
        public override void Create()
        {
        }

        /// <summary>
        /// Swap the texture
        /// </summary>
        /// <param name="textureName"></param>
        public void Set(string textureName)
        {
            texture = TextureLibrary.Instance.Get(textureName);

            if (texture == null)
            {
                Console.WriteLine($"Failed to load texture {textureName}");
                return;
            }

            frameWidth = texture.Width;
            frameHeight = texture.Height;
            this.frameCount = texture.Width / frameWidth;

            if (frame >= frameCount)
            {
                frame = 0;
            }
        }

        /// <summary>
        /// Swap the texture
        /// </summary>
        /// <param name="textureName"></param>
        public void Set(string textureName, int frameWidth, int frameHeight)
        {
            texture = TextureLibrary.Instance.Get(textureName);
            this.frameWidth = frameWidth;
            this.frameHeight = frameHeight;
            this.frameCount = texture.Width / frameWidth;

            if (frame >= frameCount)
            {
                frame = 0;
            }
        }

        /// <summary>
        /// Draw the sprite if valid
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (texture !=  null)
            {
                 origin = new Vector2(frameWidth / 2, frameHeight / 2);
                 spriteBatch.Draw(texture, Owner.position, spriteBox, Color.White, Owner.rotation, origin, Owner.scale, spriteEffect, depth);
            }
        }

        /// <summary>
        /// Advance the animation
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            if (texture == null || frameCount <= 0)
                return;

            if (animated)
            {
                Animate(gameTime);
            }
            else
            {
                spriteBox.X = 0;
                spriteBox.Y = 0;
                spriteBox.Width = frameWidth > 0 ? frameWidth : texture.Width;
            }

            int fh = frameHeight > 0 ? frameHeight : texture.Height;
            spriteBox.Height = Math.Min(fh, texture.Height);
        }

        /// <summary>
        /// Animates the sprite
        /// </summary>
        /// <param name="gameTime"></param>
        public void Animate(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (frameCount > 1)
            {
                frameTimer += frameSpeed * dt;

                // Advance the frame
                if (frameTimer >= 1f)
                {
                    frame = (frame + 1) % frameCount;
                    frameTimer = 0f;
                }

                spriteBox.Width = frameWidth;
                spriteBox.X = frame * frameWidth;
                spriteBox.Y = 0;
            }
        }
    }
}
