using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace Engine.Core.Game.Components
{
    public class SpriteData
    {
        public Texture2D? texture;
        public string Name { get; set; } = string.Empty; // Name of the sprite, can be used for identification
        public int frameWidth { get; set; } = 0; // 0 means use full image
        public int frameHeight { get; set; } = 0; // 0 means use full image
        public int frameCount { get; set; } = 1;
        public int frameSpeed { get; set; } = 1;
        public bool animated { get; set; } = false;
        public float depth = 0;
        public bool tiled = true; // if this sprite should be tiled

        public SpriteEffects spriteEffect = SpriteEffects.None;
        public Vector2 origin;

        private Rectangle spriteBox = new Rectangle();
        private int frame = 0;
        private float frameTimer = 0;

        /// <summary>
        /// Draw the sprite if valid
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        public void Draw(SpriteBatch spriteBatch, Vector2 position, float rotation, float scale)
        {
            if (texture !=  null)
            {
                 origin = new Vector2(frameWidth / 2, frameHeight / 2);
                 spriteBatch.Draw(texture, position, spriteBox, Color.White, rotation, origin, scale, spriteEffect, depth);
            }
        }

        /// <summary>
        /// Advance the animation
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (texture == null)
                return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (animated)
            {
                frameTimer += frameSpeed * dt;

                // Advance the frame
                if (frameTimer >= 1)
                {
                    frame++;
                    spriteBox.Width = frameWidth;
                    spriteBox.X = frame * frameWidth;
                    spriteBox.Y = 0;

                    if (frame >= frameCount-1)
                    {
                        frame = 0;
                    }

                    frameTimer = 0;
                }
            }
            else
            {
                spriteBox.Width = texture.Width;
            }

            // Ensure we stay within sprite bounds
            int fh = frameHeight > 0 ? frameHeight : texture.Height;
            spriteBox.Height = Math.Min(fh, texture.Height);
        }
    }
}
