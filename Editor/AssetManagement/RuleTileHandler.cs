/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         RuleTileHandler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>
/// Editor handler for creating and managing RuleTiles with multiple TileRules.
/// -----------------------------------------------------------------------------

using EarthEngineEditor;
using Editor.Windows.Inspector;
using Engine.Core.Data;
using Engine.Core.Data.Graphics;
using Engine.Core.Game.Components;
using Engine.Core.Rooms.Tiles;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Serialization.Json;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Handles loading, saving, and rendering RuleTiles in the editor.
    /// </summary>
    public class RuleTileHandler : IAssetHandler
    {
        /// <summary>
        /// The tilemap this rule tile is used with.
        /// </summary>
        [JsonIgnore]
        public Tilemap? Tilemap;

        /// <summary>
        /// The RuleTile being edited.
        /// </summary>
        [HideInInspector]
        public RuleTile Tile = new RuleTile();

        public void Open(string path)
        {
        }

        /// <summary>
        /// Reconstruct rule tile from data
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            string fullPath = Path.Combine(EnginePaths.AssetsBase, path);

            if (!File.Exists(fullPath))
                return;

            path = fullPath;

            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                Converters = { new JsonStringEnumConverter() }
            };

            try
            {
                string json = File.ReadAllText(path);
                Tile = JsonSerializer.Deserialize<RuleTile>(json, options) ?? new RuleTile();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load RuleTile from {path}: {ex.Message}");
                Tile = new RuleTile();
            }
        }

        /// <summary>
        /// Save rule tile data
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true,
                Converters = { new ComponentListJsonConverter() },
            };

            string json = JsonSerializer.Serialize(Tile, options);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Draws the full editor for the RuleTile.
        /// </summary>
        public void Render()
        {
            if (Tilemap != null)
            {
                Tilemap.Tile = Tile;
            }

            InspectorUI.DrawClass(this);

            int ruleIndex = 0;
            foreach (var rule in Tile.Rules)
            {
                bool header = ImGui.CollapsingHeader($"Rule {ruleIndex++}");

                if (header)
                {
                    DrawRuleGrid(rule);

                    if (ImGuiRenderer.IconButton($"Delete##{ruleIndex}", "\uf1f8", Microsoft.Xna.Framework.Color.Red))
                    {
                        Tile.Rules.Remove(rule);
                        break;
                    }

                    ImGui.Separator();
                }
            }

            ImGui.NewLine();
            if (ImGui.Button("Add Rule"))
            {
                Tile.Rules.Add(new TileRule());
            }
        }

        /// <summary>
        /// Draws the 3x3 grid for a rule’s pattern.
        /// </summary>
        /// <summary>
        /// Draws the 3x3 grid for a rule’s pattern with tri-state buttons.
        /// Left-click = cycle forward (Any → This → NotThis → Any)
        /// Right-click = directly set to NotThis.
        /// </summary>
        private void DrawRuleGrid(TileRule rule)
        {
            float buttonSize = 24f;

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    TileDirection dir = (TileDirection)(y * 3 + x);
                    if (!rule.Conditions.ContainsKey(dir))
                        rule.Conditions[dir] = NeighborCondition.Any;

                    var state = rule.Conditions[dir];

                    // Color mapping
                    Vector4 baseColor = state switch
                    {
                        NeighborCondition.This => new Vector4(0.2f, 0.7f, 0.2f, 1f),      // Green
                        NeighborCondition.NotThis => new Vector4(0.8f, 0.2f, 0.2f, 1f),   // Red
                        _ => new Vector4(0.5f, 0.5f, 0.5f, 1f)                            // Gray (Any)
                    };

                    Vector4 hoverColor = new Vector4(baseColor.X + 0.1f, baseColor.Y + 0.1f, baseColor.Z + 0.1f, 1f);
                    Vector4 pressedColor = new Vector4(baseColor.X - 0.1f, baseColor.Y - 0.1f, baseColor.Z - 0.1f, 1f);

                    ImGui.PushID($"{dir}_{rule.GetHashCode()}");
                    ImGui.PushStyleColor(ImGuiCol.Button, baseColor);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverColor);
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, pressedColor);

                    string label = dir == TileDirection.Center ? "##C" : $"##{dir}";

                    // Draw the button
                    if (ImGui.Button(label, new Vector2(buttonSize, buttonSize)))
                    {
                        if (dir != TileDirection.Center)
                        {
                            // Left-click cycles state
                            rule.Conditions[dir] = (NeighborCondition)(((int)state + 1) % 3);
                        }
                    }

                    // Detect right-click → set to NotThis
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        if (dir != TileDirection.Center)
                            rule.Conditions[dir] = NeighborCondition.NotThis;
                    }

                    ImGui.PopStyleColor(3);
                    ImGui.PopID();

                    if (x < 2)
                        ImGui.SameLine();
                }
            }

            SelectTileImage(rule);
        }


        /// <summary>
        /// Selects which tile frame should be the output for the rule.
        /// </summary>
        private unsafe void SelectTileImage(TileRule rule)
        {
            if (Tilemap?.texture?.texture == null)
                return;

            Texture2D tex = Tilemap.texture.texture;
            var frames = GetTileFrames(Tilemap);
            if (frames.Count == 0)
                return;

            int selected = rule.SelectedFrameIndex;
            if (selected >= frames.Count)
                selected = 0;

            string previewLabel = $"Frame {selected}";
            ImGui.Text("Output Tile:");
            ImGui.SameLine();

            if (ImGui.BeginCombo($"##TileImage_{rule.GetHashCode()}", previewLabel))
            {
                for (int i = 0; i < frames.Count; i++)
                {
                    var rect = frames[i];
                    bool isSelected = (i == selected);

                    Vector2 uv0 = new Vector2(rect.X / (float)tex.Width, rect.Y / (float)tex.Height);
                    Vector2 uv1 = new Vector2((rect.X + rect.Width) / (float)tex.Width, (rect.Y + rect.Height) / (float)tex.Height);

                    ImGui.PushID(i);
                    ImGui.Image(ImGuiRenderer.Instance.BindTexture(tex), new Vector2(32, 32), uv0, uv1);
                    ImGui.SameLine();

                    if (ImGui.Selectable($"Frame {i}", isSelected))
                        rule.SelectedFrameIndex = i;

                    ImGui.PopID();
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            // Draw preview outside combo
            var previewRect = frames[rule.SelectedFrameIndex];
            Vector2 uvp0 = new Vector2(previewRect.X / (float)tex.Width, previewRect.Y / (float)tex.Height);
            Vector2 uvp1 = new Vector2((previewRect.X + previewRect.Width) / (float)tex.Width, (previewRect.Y + previewRect.Height) / (float)tex.Height);

            ImGui.Text("Preview:");
            ImGui.Image(ImGuiRenderer.Instance.BindTexture(tex), new Vector2(48, 48), uvp0, uvp1);
        }

        /// <summary>
        /// Returns all tile frame rectangles for the tileset.
        /// </summary>
        private List<Rectangle> GetTileFrames(Tilemap map)
        {
            var frames = new List<Rectangle>();
            if (map.texture?.texture == null)
                return frames;

            Texture2D tex = map.texture.texture;
            int texWidth = tex.Width;
            int texHeight = tex.Height;
            int cell = map.CellSize;

            int cols = texWidth / cell;
            int rows = texHeight / cell;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                    frames.Add(new Rectangle(x * cell, y * cell, cell, cell));
            }

            return frames;
        }
    }
}
