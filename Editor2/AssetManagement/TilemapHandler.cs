/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TextureHandler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor;
using Engine.Core;
using Engine.Core.Data;
using Engine.Core.Game.Components;
using Engine.Core.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Numerics;
using System.Reflection;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Handles loading, saving, and rendering prefabs in the editor.
    /// </summary>
    public class TilemapHandler : IInspectable
    {
        public TilemapRenderer? layer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TilemapHandler"/> class with the specified layer.
        /// </summary>
        /// <param name="layer"></param>
        public TilemapHandler(TilemapRenderer layer)
        {
            this.layer = layer;
        }

        /// <summary>
        /// Renders the prefab's components in the editor UI.
        /// </summary>
        public void Render()
        {
            if (layer != null)
            {
                EngineContext.CurrentTilemap = layer;

                // Draw the editable fields
                string title = layer.Title;
                if (ImGui.InputText("Name", ref title, 16))
                    layer.Title = title;

                ImGui.InputFloat("Depth", ref layer.Depth);
                ImGui.InputInt("Floor Level", ref layer.FloorLevel);
                ImGui.InputFloat2("Offset", ref layer.Offset);

                var member = typeof(TilemapRenderer).GetMember("Texture", BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                PrefabHandler.DrawField(
                    "Texture",
                    layer.Texture,
                    typeof(Texture2D),
                    newVal => layer.Texture = (Texture2D)newVal,
                    member
                );
                if (ImGuiRenderer.IconButton("Delete Layer", ImGuiRenderer.TrashIcon, Microsoft.Xna.Framework.Color.Red))
                {
                    // Remove the layer
                    TilemapManager.layers.Remove(layer);
                }
            }
        }
    }
}

