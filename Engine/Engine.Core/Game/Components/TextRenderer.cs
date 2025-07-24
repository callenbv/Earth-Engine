/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TextRenderer.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Renders text to the screen in world coordinates.          
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Graphics;
using Engine.Core.Data;

namespace Engine.Core.Game.Components
{
    /// <summary>
    /// A component for rendering text in the game world.
    /// </summary>
    [ComponentCategory("Graphics")]
    public class TextRenderer : ObjectComponent
    {
        public override string Name => "Text Renderer";
        public string Text { get; set; } = "";
        public Color Color { get; set; } = Color.White;
        new public float Scale { get; set; } = 1.0f;
        new public float Rotation { get; set; } = 0.0f;
        public float Depth { get; set; } = 1.0f;
        public bool Visible { get; set; } = true;
        public bool Centered { get; set; } = false;

        public string FontName = "Default";
        public Vector2 Origin = Vector2.Zero;
        public SpriteEffects Effects = SpriteEffects.None;
        public Vector2 Offset = Vector2.Zero;
        protected SpriteFont? currentFont;
        protected Vector2 textSize;

        /// <summary>
        /// Initialize the text renderer component.
        /// </summary>
        public override void Create()
        {
            if (currentFont == null)
            {
                LoadFont();
            }
        }

        /// <summary>
        /// Update the text size based on the current font and text content.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible || string.IsNullOrEmpty(Text) || currentFont == null || Owner == null)
                return;

            Vector2 position = Owner.Position + Offset;
            
            if (Centered)
            {
                // Center the text around the object's position
                position -= textSize * Scale * 0.5f;
            }

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

        /// <summary>
        /// Set the text content and update size calculations
        /// </summary>
        /// <param name="text">Text to display</param>
        public void SetText(string text)
        {
            Text = text;
            UpdateTextSize();
        }

        /// <summary>
        /// Set the font to use
        /// </summary>
        /// <param name="fontName">Name of the font</param>
        public void SetFont(string fontName)
        {
            FontName = fontName;
            LoadFont();
            UpdateTextSize();
        }

        /// <summary>
        /// Set text color
        /// </summary>
        /// <param name="color">Color to use</param>
        public void SetColor(Color color)
        {
            Color = color;
        }

        /// <summary>
        /// Set text scale
        /// </summary>
        /// <param name="scale">Scale factor</param>
        public void SetScale(float scale)
        {
            Scale = scale;
            UpdateTextSize();
        }

        /// <summary>
        /// Get the current text size
        /// </summary>
        /// <returns>Size of the text</returns>
        public Vector2 GetTextSize()
        {
            return textSize * Scale;
        }

        /// <summary>
        /// Load the current font
        /// </summary>
        private void LoadFont()
        {
            currentFont = FontLibrary.Main.Get(FontName);
            if (currentFont == null)
            {
                Console.WriteLine($"[TextRenderer] Font '{FontName}' not found, using fallback");
                currentFont = FontLibrary.Main.Get("Default");
            }
        }

        /// <summary>
        /// Update the cached text size
        /// </summary>
        private void UpdateTextSize()
        {
            if (currentFont != null && !string.IsNullOrEmpty(Text))
            {
                textSize = currentFont.MeasureString(Text);
            }
            else
            {
                textSize = Vector2.Zero;
            }
        }

        /// <summary>
        /// Center the text origin
        /// </summary>
        public void CenterOrigin()
        {
            if (currentFont != null && !string.IsNullOrEmpty(Text))
            {
                Origin = textSize * 0.5f;
            }
        }

        /// <summary>
        /// Set the text to be centered around the object's position
        /// </summary>
        /// <param name="centered">Whether to center the text</param>
        public void SetCentered(bool centered)
        {
            Centered = centered;
        }
    }
} 
