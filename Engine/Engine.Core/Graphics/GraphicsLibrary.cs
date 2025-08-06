/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         GraphicsLibrary.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Graphics library for common drawing calls and textures          
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Core.Graphics
{
    /// <summary>
    /// A static class that provides graphics-related functionality, such as creating a pixel texture.
    /// </summary>
    public static class GraphicsLibrary
    {
        public static Texture2D PixelTexture = null!;
        public static Texture2D SquareTexture = null!;
        public static Texture2D CircleTexture = null!;
        public static Texture2D DiscTexture = null!;
        public static GraphicsDevice graphicsDevice = null!;

        /// <summary>
        /// Initializes the graphics library, creating a 1x1 pixel texture.
        /// </summary>
        public static void Initialize()
        {
            LoadTextures();
        }

        /// <summary>
        /// Loads the textures used in the graphics library, including primitives
        /// </summary>
        private static void LoadTextures()
        {
            // Setup base pixel texture
            PixelTexture = new Texture2D(graphicsDevice, 1, 1);
            var pixels = new Color[1];
            pixels[0] = Color.White;
            PixelTexture.SetData(pixels);

            // Setup square texture
            SquareTexture = new Texture2D(graphicsDevice, 64, 64);
            Color[] squarePixels = new Color[64 * 64];
            for (int i = 0; i < squarePixels.Length; i++)
            {
                squarePixels[i] = Color.White; // Fill the square texture with white pixels
            }
            SquareTexture.SetData(squarePixels);

            // Setup circle texture
            CircleTexture = new Texture2D(graphicsDevice, 64, 64);
            Color[] circlePixels = new Color[64 * 64];

            int size = 64;
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - radius + 0.5f;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    int index = y * size + x;
                    circlePixels[index] = dist <= radius ? Color.White : Color.Transparent;
                }
            }

            CircleTexture.SetData(circlePixels);


            // Setup the disc texture, fading alpha as it goes outwards
            DiscTexture = new Texture2D(graphicsDevice, 64, 64);
            Color[] discPixels = new Color[64 * 64];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius + 0.5f;
                    float dy = y - radius + 0.5f;
                    float dist = MathF.Sqrt(dx * dx + dy * dy) / radius;
                    dist = Math.Clamp(dist, 0f, 1f);

                    int index = y * size + x;
                    byte alpha = (byte)(255 * (1f - dist));
                    discPixels[index] = Color.FromNonPremultiplied(255, 255, 255, alpha);
                }
            }

            DiscTexture.SetData(discPixels);
        }
    }
}

