/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         UITextRenderer.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;
using System.Text;

namespace Engine.Core.Game.Components
{
    [ComponentCategory("UI")]
    public class UITextRenderer : TextRenderer
    {
        public override string Name => "UI Text Renderer";
        public override bool UpdateInEditor => true;
        public override bool IsUI => true;

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
            if (!Visible || string.IsNullOrEmpty(Text) || bitFont == null || Owner == null)
                return;

            Vector2 finalPos = new Vector2(Position.X, Position.Y);

            if (Centered)
            {
                // Center the text around the object's position
                finalPos -= textSize * new Vector2(Scale.X, Scale.Y) * 0.5f;
            }

            spriteBatch.DrawString(
                bitFont,
                Text,
                finalPos,
                Color,
                Rotation,
                Origin,
                new Vector2(Scale.X, Scale.Y),
                Effects,
                Depth
            );
        }
    }
} 
