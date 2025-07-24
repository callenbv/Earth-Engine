/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         FontLibrary.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Engine.Core.Data;

namespace Engine.Core.Graphics
{
    public class FontLibrary
    {
        public Dictionary<string, SpriteFont> fonts = new Dictionary<string, SpriteFont>();
        private static FontLibrary? _main;
        private GraphicsDevice? graphicsDevice = null;
        private ContentManager? contentManager = null;

        public static FontLibrary Main => _main ??= new FontLibrary();

        /// <summary>
        /// Initialize the font library with graphics device and content manager
        /// </summary>
        /// <param name="graphicsDevice_">Graphics device for font rendering</param>
        /// <param name="contentManager_">Content manager for loading fonts</param>
        public void Initialize(GraphicsDevice graphicsDevice_, ContentManager contentManager_)
        {
            graphicsDevice = graphicsDevice_;

            Console.WriteLine($"[FontLibrary] Creating ContentManager pointing to: {EnginePaths.SHARED_CONTENT_PATH}");
            contentManager = contentManager_;
        }

        /// <summary>
        /// Loads all fonts from the Fonts directory
        /// </summary>
        /// <param name="searchPattern">File pattern to search for (default: "*.spritefont")</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void LoadFonts(string searchPattern = "*.spritefont")
        {
            try
            {
                if (contentManager == null)
                {
                    Console.WriteLine("[FontLibrary] ContentManager not initialized. Call Initialize() first.");
                    return;
                }

                // Load fonts from the centralized Content directory
                var fontNames = new[] { "Default", "UI" };

                foreach (var fontName in fontNames)
                {
                    try
                    {
                        Console.WriteLine($"[FontLibrary] Attempting to load font: {fontName}");

                        var font = contentManager.Load<SpriteFont>(Path.Combine("Assets", "Fonts", fontName));
                        fonts[fontName] = font;
                        Console.WriteLine($"[FontLibrary] Successfully loaded font: {fontName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FontLibrary] Failed to load font {fontName}: {ex.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[FontLibrary] Error loading fonts: {e.Message}");
            }
        }

        /// <summary>
        /// Load a specific font by name
        /// </summary>
        /// <param name="fontName">Name of the font to load</param>
        /// <returns>True if font was loaded successfully</returns>
        public bool LoadFont(string fontName)
        {
            try
            {
                if (contentManager == null)
                {
                    Console.WriteLine("[FontLibrary] ContentManager not initialized. Call Initialize() first.");
                    return false;
                }

                var font = contentManager.Load<SpriteFont>(Path.Combine("Assets", "Fonts", fontName));
                fonts[fontName] = font;
                Console.WriteLine($"[FontLibrary] Loaded font: {fontName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FontLibrary] Failed to load font {fontName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get a font by name
        /// </summary>
        /// <param name="name">Name of the font</param>
        /// <returns>The SpriteFont, or null if not found</returns>
        public SpriteFont? Get(string name)
        {
            if (fonts.TryGetValue(name, out var font))
                return font;

            Console.WriteLine($"[FontLibrary] Font '{name}' not found. Make sure LoadFonts() was called and the font exists.");
            return null;
        }

        /// <summary>
        /// Get a font by name, with fallback to default
        /// </summary>
        /// <param name="name">Name of the font</param>
        /// <param name="fallbackName">Fallback font name</param>
        /// <returns>The SpriteFont, or fallback font</returns>
        public SpriteFont Get(string name, string fallbackName)
        {
            var font = Get(name);
            if (font != null)
                return font;

            return Get(fallbackName) ?? CreateDefaultFont();
        }



        /// <summary>
        /// Create a default font if no fonts are available
        /// </summary>
        /// <returns>A basic SpriteFont</returns>
        private SpriteFont? CreateDefaultFont()
        {
            if (graphicsDevice == null)
            {
                Console.WriteLine("[FontLibrary] GraphicsDevice is null, cannot create default font");
                return null;
            }

            // Create a simple 1x1 white texture for the default font
            var texture = new Texture2D(graphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });

            // This is a fallback - in practice, you should always have at least one font loaded
            Console.WriteLine("[FontLibrary] Created fallback font");
            return null; // We'll need to implement proper fallback font creation
        }

        /// <summary>
        /// Check if a font exists
        /// </summary>
        /// <param name="name">Name of the font</param>
        /// <returns>True if font exists</returns>
        public bool HasFont(string name)
        {
            return fonts.ContainsKey(name);
        }

        /// <summary>
        /// Get all loaded font names
        /// </summary>
        /// <returns>List of font names</returns>
        public List<string> GetFontNames()
        {
            return new List<string>(fonts.Keys);
        }
    }
}
