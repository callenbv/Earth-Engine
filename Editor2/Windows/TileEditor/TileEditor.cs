using Editor.AssetManagement;
using Engine.Core;
using Engine.Core.Game.Components;
using Engine.Core.Rooms.Tiles;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Windows.TileEditor
{
    public class TileEditorWindow
    {
        private bool show = true;
        private TilemapRenderer? selectedLayer;

        public void Render()
        {
            if (!show) return;

            ImGui.Begin("Tile Editor", ref show);

            bool tree = ImGui.TreeNodeEx("Tile Layers", ImGuiTreeNodeFlags.DefaultOpen);

            if (tree)
            {
                foreach (var layer in TilemapManager.layers)
                {
                    if (ImGui.TreeNodeEx(layer.Title))
                    {
                        // Draw the editable fields
                        selectedLayer = layer; // Set the selected layer for painting
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }

            if (ImGui.Button("Add Layer"))
            {
                // Create a new layer with default values
                var newLayer = new TilemapRenderer(100, 100, "CoraglowCityTileset");
                newLayer.Title = $"Layer {TilemapManager.layers.Count + 1}";
                TilemapManager.layers.Add(newLayer);
            }

            ImGui.Separator();

            // Paint on the selected layer
            if (selectedLayer != null)
            {
                ImGui.Text(selectedLayer.Title);
                if (Input.IsMouseDown())
                {
                    var mousePos = Input.mouseWorldPosition;
                    int tileX = (int)(mousePos.X / selectedLayer.TileSize);
                    int tileY = (int)(mousePos.Y / selectedLayer.TileSize);
                    if (tileX >= 0 && tileX < selectedLayer.Width && tileY >= 0 && tileY < selectedLayer.Height)
                    {
                        selectedLayer.SetTile(tileX, tileY, 5); // Test grass tile
                    }
                }
            }

            ImGui.End();
        }


        public bool IsVisible => show;
        public void SetVisible(bool visible) => show = visible;
    }
}
