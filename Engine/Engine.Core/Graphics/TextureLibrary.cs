/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TextureLibrary.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Data;
using Microsoft.Xna.Framework;

namespace Engine.Core.Graphics
{
    public class TextureLibrary
    {
        public Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        public GraphicsDevice? graphicsDevice = null;
        public static TextureLibrary Instance; 
        private Texture2D defaultTexture;

        public TextureLibrary()
        {
            Instance = this;
        }

        /// <summary>
        /// Loads all textures within project
        /// </summary>
        /// <param name="graphicsDevice_"></param>
        /// <param name="searchPattern"></param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void LoadTextures(string searchPattern = "*.png")
        {
            try
            {
                // Look for Assets/Sprites relative to the EXE location
                string spriteDir = EnginePaths.AssetsBase;

                foreach (var file in Directory.GetFiles(spriteDir, searchPattern, SearchOption.AllDirectories))
                {
                    try
                    {
                        using var fs = File.OpenRead(file);
                        var tex = Texture2D.FromStream(graphicsDevice, fs);
                        tex.Name = Path.GetFileNameWithoutExtension(file);
                        textures[tex.Name] = tex;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Console.WriteLine($"Texture not found at {spriteDir}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            defaultTexture = new Texture2D(graphicsDevice, 16, 16);
            Color[] pixels = new Color[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.White;

            defaultTexture.SetData(pixels);
        }

        /// <summary>
        /// Retrieve a previously loaded texture by name (file name without extension).
        /// </summary>
        public Texture2D Get(string name)
        {
            try
            {
                if (textures.TryGetValue(name, out var tex))
                    return tex;
            }
            catch (System.Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }

            return defaultTexture;
        }
    }
}

