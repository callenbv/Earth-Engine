using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Systems.Graphics;
using System;

namespace Engine.Core.Systems.Graphics
{
    public class UITextRenderer
    {
        public string Text { get; set; } = "";
        public string FontName { get; set; } = "Default";
        public Color Color { get; set; } = Color.White;
        public float Scale { get; set; } = 1.0f;
        public float Rotation { get; set; } = 0.0f;
        public Vector2 Origin { get; set; } = Vector2.Zero;
        public SpriteEffects Effects { get; set; } = SpriteEffects.None;
        public float Depth { get; set; } = 0.0f;
        public bool Visible { get; set; } = true;
        public bool Centered { get; set; } = false;
        public Vector2 Position { get; set; } = Vector2.Zero;

        private SpriteFont? currentFont;
        private Vector2 textSize;

        public UITextRenderer()
        {
            LoadFont();
            UpdateTextSize();
        }

        public UITextRenderer(string text, string fontName = "Default")
        {
            Text = text;
            FontName = fontName;
            LoadFont();
            UpdateTextSize();
        }

        /// <summary>
        /// Draw the text in screen space
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch to draw with</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible || string.IsNullOrEmpty(Text) || currentFont == null)
                return;

            Vector2 drawPosition = Position;
            
            if (Centered)
            {
                // Center the text around the position
                drawPosition -= textSize * Scale * 0.5f;
            }

            spriteBatch.DrawString(
                currentFont,
                Text,
                drawPosition,
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
        /// Set the position in screen coordinates
        /// </summary>
        /// <param name="position">Screen position</param>
        public void SetPosition(Vector2 position)
        {
            Position = position;
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
                Console.WriteLine($"[UITextRenderer] Font '{FontName}' not found, using fallback");
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
        /// Set the text to be centered around the position
        /// </summary>
        /// <param name="centered">Whether to center the text</param>
        public void SetCentered(bool centered)
        {
            Centered = centered;
        }

        /// <summary>
        /// Create a text renderer for screen space text
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="position">Screen position</param>
        /// <param name="fontName">Font to use</param>
        /// <param name="color">Text color</param>
        /// <returns>UITextRenderer instance</returns>
        public static UITextRenderer Create(string text, Vector2 position, string fontName = "Default", Color? color = null)
        {
            var renderer = new UITextRenderer(text, fontName)
            {
                Position = position,
                Color = color ?? Color.White
            };
            return renderer;
        }
    }
} 