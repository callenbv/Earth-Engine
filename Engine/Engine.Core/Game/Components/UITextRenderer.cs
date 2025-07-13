using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Systems.Graphics;
using System;

namespace Engine.Core.Game.Components
{
    public class UITextRenderer : TextRenderer
    {
        /// <summary>
        /// If this is tethered to the object or not
        /// </summary>
        public bool Tethered = true;
        public Vector2 UIPosition;

        public override void Create()
        {
            base.Create();
            Name = "UI Text Renderer";
        }

        /// <summary>
        /// Does not inherit draw from text renderer in world
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {

        }

        public override void DrawUI(SpriteBatch spriteBatch)
        {
            if (!Visible || string.IsNullOrEmpty(Text) || currentFont == null || Owner == null)
                return;

            Vector2 position = Owner.position + Offset;

            if (!Tethered)
            {
                position = UIPosition;
            }
            
            if (Centered)
            {
                // Center the text around the object's position
                position -= textSize * Scale * 0.5f;
            }

            // Use UI transform matrix for screen-space rendering
            var uiMatrix = Camera.Main.GetUIViewMatrix(
                Camera.Main.ViewportWidth, 
                Camera.Main.ViewportHeight
            );

            spriteBatch.DrawString(
                currentFont,
                Text,
                position,
                Color,
                Rotation,
                Origin,
                Scale,
                Effects,
                Depth
            );
        }
    }
} 