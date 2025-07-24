/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TileEditor.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor;
using Engine.Core;
using Engine.Core.Game.Components;
using ImGuiNET;
using System.Numerics;

namespace Editor.Windows.TileEditor
{
    /// <summary>
    /// Represents the mode of the tile editor, which can be used to paint, erase, or select tiles.
    /// </summary>
    public enum TileEditorMode
    {
        Paint,
        Erase,
        Select
    }

    /// <summary>
    /// Represents the Tile Editor window in the editor, allowing users to create and edit tile layers.
    /// </summary>
    public class TileEditorWindow
    {
        private bool show = true;
        private bool open = false;
        private float previewScale = 1;
        private int selectedTileIndex = 1;
        private TilemapRenderer? selectedLayer;
        public int TileSize = 16;
        public int BrushSize = 1;
        TileEditorMode mode = TileEditorMode.Paint;

        /// <summary>
        /// Renders the tile editor panel
        /// </summary>
        public void Render()
        {
            if (!show) return;

            if(ImGui.Begin("Tile Editor", ref show))
                EditorApp.Instance.selectionMode = EditorSelectionMode.Tile;

            bool tree = ImGui.TreeNodeEx("Tile Layers", ImGuiTreeNodeFlags.DefaultOpen);
            open = false;

            if (tree)
            {
                open = true;
                foreach (var layer in TilemapManager.layers)
                {
                    if (ImGui.TreeNodeEx(layer.Title))
                    {
                        // Draw the editable fields
                        selectedLayer = layer; // Set the selected layer for painting
                        ImGui.SliderInt("Width", ref layer.Width, 50,200);
                        ImGui.SliderInt("Height", ref layer.Height, 50,200);
                        ImGui.InputFloat2("Offset", ref layer.Offset);
                        ImGui.InputFloat("Depth", ref layer.Depth);
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
            if (selectedLayer != null && EditorApp.Instance.selectionMode == EditorSelectionMode.Tile)
            {
                // Draw data for each tileset
                ImGui.Text(selectedLayer.Title);

                bool clicked;

                CircularSelectable("tile", "T", TileEditorMode.Paint, mode, out clicked);
                if (clicked) mode = TileEditorMode.Paint;

                ImGui.SameLine();
                CircularSelectable("erase", "X", TileEditorMode.Erase, mode, out clicked);
                if (clicked) mode = TileEditorMode.Erase;

                ImGui.SliderInt("Brush Size", ref BrushSize, 1, 10);

                // Tileset preview
                if (selectedLayer.Texture != null)
                {
                    int tileSize = selectedLayer.TileSize;
                    int texWidth = selectedLayer.Texture.Width;
                    int texHeight = selectedLayer.Texture.Height;

                    int tilesX = texWidth / tileSize;
                    int tilesY = texHeight / tileSize;

                    Vector2 scaledTileSize = new Vector2(tileSize * previewScale);

                    // Show full tileset image
                    Vector2 imageSize = new Vector2(texWidth * previewScale, texHeight * previewScale);

                    if (selectedLayer.TexturePtr == IntPtr.Zero)
                        selectedLayer.TexturePtr = ImGuiRenderer.Instance.BindTexture(selectedLayer.Texture);

                    ImGui.Image(selectedLayer.TexturePtr, imageSize);

                    // Draw overlay grid of invisible buttons
                    Vector2 imagePos = ImGui.GetItemRectMin(); // top-left corner of the image
                    var drawList = ImGui.GetWindowDrawList();

                    for (int y = 0; y < tilesY; y++)
                    {
                        for (int x = 0; x < tilesX; x++)
                        {
                            int index = y * tilesX + x;

                            Vector2 min = imagePos + new Vector2(x * scaledTileSize.X, y * scaledTileSize.Y);
                            Vector2 max = min + scaledTileSize;

                            ImGui.SetCursorScreenPos(min);
                            ImGui.PushID(index);

                            if (ImGui.InvisibleButton("tile", scaledTileSize))
                            {
                                selectedTileIndex = index;
                            }

                            // Draw outline if selected
                            if (index == selectedTileIndex)
                            {
                                drawList.AddRect(min, max, ImGui.ColorConvertFloat4ToU32(new Vector4(255,0,0,1)), 0f, ImDrawFlags.None, 2.0f);
                            }

                            ImGui.PopID();
                        }
                    }
                }

                // Now paint with the selected tile index
                if (!ImGui.GetIO().WantCaptureMouse && EditorApp.Instance.gameFocused)
                {
                    var mousePos = Input.mouseWorldPosition;
                    int tileX = (int)((mousePos.X - selectedLayer.Offset.X) / selectedLayer.TileSize);
                    int tileY = (int)((mousePos.Y - selectedLayer.Offset.Y) / selectedLayer.TileSize);

                    if (Input.IsMouseDown())
                    {
                        if (tileX >= 0 && tileX < selectedLayer.Width && tileY >= 0 && tileY < selectedLayer.Height)
                        {
                            for (int dx = -BrushSize / 2; dx <= BrushSize / 2; dx++)
                            {
                                for (int dy = -BrushSize / 2; dy <= BrushSize / 2; dy++)
                                {
                                    int px = tileX + dx;
                                    int py = tileY + dy;

                                    if (px >= 0 && px < selectedLayer.Width &&
                                        py >= 0 && py < selectedLayer.Height)
                                    {

                                        if (mode == TileEditorMode.Paint)
                                        {
                                            selectedLayer.SetTile(px, py, selectedTileIndex);
                                        }
                                        else if (mode == TileEditorMode.Erase)
                                        {
                                            selectedLayer.SetTile(px, py, -1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            ImGui.End();
        }

        /// <summary>
        /// Draws a circular selectable button with an icon/text inside it.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="icon"></param>
        /// <param name="mode"></param>
        /// <param name="selectedMode"></param>
        /// <param name="clicked"></param>
        /// <returns></returns>
        private bool CircularSelectable(string id, string icon, TileEditorMode mode, TileEditorMode selectedMode, out bool clicked)
        {
            ImGui.PushID(id);

            float size = 36f;
            var drawList = ImGui.GetWindowDrawList();
            var cursor = ImGui.GetCursorScreenPos();
            var center = cursor + new Vector2(size / 2f);
            var color = mode == selectedMode ? new Vector4(0.4f, 0.7f, 1.0f, 1.0f) : new Vector4(0.2f, 0.2f, 0.2f, 1.0f);

            ImGui.InvisibleButton("##btn", new Vector2(size));
            bool hovered = ImGui.IsItemHovered();
            clicked = ImGui.IsItemClicked();

            drawList.AddCircleFilled(center, size / 2f, ImGui.ColorConvertFloat4ToU32(color), 20);

            // Icon/text
            var textSize = ImGui.CalcTextSize(icon);
            var textPos = center - textSize / 2;
            drawList.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), icon);

            ImGui.PopID();
            return hovered;
        }

        public bool IsVisible => show;
        public void SetVisible(bool visible) => show = visible;
    }
}

