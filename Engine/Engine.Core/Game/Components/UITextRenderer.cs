/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         UITextRenderer.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Data;

namespace Engine.Core.Game.Components
{
    [ComponentCategory("Graphics")]
    public class UITextRenderer : TextRenderer
    {
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

            Vector2 finalPos = Position;

            if (Centered)
            {
                // Center the text around the object's position
                finalPos -= textSize * Scale * 0.5f;
            }

            spriteBatch.DrawString(
                currentFont,
                Text,
                finalPos,
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
