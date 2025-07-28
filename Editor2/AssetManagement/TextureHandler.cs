/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TextureHandler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor;
using Engine.Core.Data;
using Engine.Core.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Numerics;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Handles loading, saving, and rendering prefabs in the editor.
    /// </summary>
    public class TextureHandler : IAssetHandler
    {
        private Texture2D? texture = null;
        private float displayScale = 2f;
        private IntPtr? icon = IntPtr.Zero;

        /// <summary>
        /// Loads a texture from the specified path and binds it for rendering in the editor.
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            string texturePath = Path.GetFileName(path);
            texturePath = Path.GetFileNameWithoutExtension(texturePath);
            texture = TextureLibrary.Instance.Get(Path.GetFileName(texturePath));

            if (texture != null)
            {
                icon = ImGuiRenderer.Instance.BindTexture(texture);
            }
        }

        public void Save(string path)
        {
            // Nothing to save
        }

        /// <summary>
        /// Renders the prefab's components in the editor UI.
        /// </summary>
        public void Render()
        {
            if (icon != IntPtr.Zero && icon != null && texture != null)
            {
                float width = texture.Width * displayScale;
                float height = texture.Height * displayScale;
                width = Math.Clamp(width, 1, 256);
                height = Math.Clamp(height, 1, 256);
                Vector2 size = new Vector2(width, height);

                ImGui.Image(icon.Value, size);
            }
        }
    }
}

