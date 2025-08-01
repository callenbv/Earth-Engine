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
        public static GraphicsDevice graphicsDevice = null!;

        /// <summary>
        /// Initializes the graphics library, creating a 1x1 pixel texture.
        /// </summary>
        public static void Initialize()
        {
            PixelTexture = new Texture2D(graphicsDevice, 1, 1);
            var pixels = new Color[1];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.White;

            PixelTexture.SetData(pixels);
        }
    }
}
