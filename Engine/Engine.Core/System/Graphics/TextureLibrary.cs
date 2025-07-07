using Engine.Core.Game.Components;
using Engine.Core.Game;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.Graphics
{
    public class TextureLibrary
    {
        public Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private static TextureLibrary? _main;
        GraphicsDevice? graphicsDevice = null;
        public static TextureLibrary Main => _main ??= new TextureLibrary();

        public void LoadTextures(GraphicsDevice graphicsDevice_, string searchPattern = "*.png")
        {
            if (graphicsDevice == null)
                graphicsDevice = graphicsDevice_;          // remember for hot-reload if needed

            var baseDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                        "..", "..", "..", "..",
                                                        "Editor", "bin", "Assets"));
            var spriteDir = Path.Combine(baseDir, "Sprites");
            if (!Directory.Exists(spriteDir))
                throw new DirectoryNotFoundException($"Sprite directory not found: {spriteDir}");

            foreach (var file in Directory.GetFiles(spriteDir, searchPattern, SearchOption.TopDirectoryOnly))
            {
                using var fs = File.OpenRead(file);
                var tex = Texture2D.FromStream(graphicsDevice, fs);
                tex.Name = Path.GetFileNameWithoutExtension(file);
                textures[tex.Name] = tex;
            }
        }

        /// <summary>
        /// Retrieve a previously loaded texture by name (file name without extension).
        /// </summary>
        public Texture2D Get(string name)
        {
            if (textures.TryGetValue(name, out var tex))
                return tex;

            throw new KeyNotFoundException($"Texture '{name}' not found. Make sure LoadTextures() was called and the file exists.");
        }
    }
}
