/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         UITextRenderer.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Graphics;
using System;
using Engine.Core.Data;

namespace Engine.Core.Game.Components
{
    [ComponentCategory("Graphics")]
    public class UITextRenderer : TextRenderer
    {
        /// <summary>
        /// If this is tethered to the object or not
        /// </summary>
        new public Vector2 Position;
        public override string Name => "UI Text Renderer";
        public override bool UpdateInEditor => true;

        /// <summary>
        /// Does not inherit draw from text renderer in world
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {

        }
        
        /// <summary>
        /// Draw our text in the UI
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void DrawUI(SpriteBatch spriteBatch)
        {
            if (!Visible || string.IsNullOrEmpty(Text) || currentFont == null || Owner == null)
                return;

            if (Centered)
            {
                // Center the text around the object's position
                Position -= textSize * Scale * 0.5f;
            }

            // Use UI transform matrix for screen-space rendering
            var uiMatrix = Camera.Main.GetUIViewMatrix(
                Camera.Main.ViewportWidth, 
                Camera.Main.ViewportHeight
            );

            spriteBatch.DrawString(
                currentFont,
                Text,
                Position,
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
