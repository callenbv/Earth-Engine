/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         TileEditor.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor;
using EarthEngineEditor.Windows;
using Editor.AssetManagement;
using Editor.Windows.Inspector;
using Engine.Core;
using Engine.Core.Data;
using Engine.Core.Game.Components;
using Engine.Core.Rooms.Tiles;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Editor.Windows.TileEditor
{
    /// <summary>
    /// Represents the mode of the tile editor, which can be used to paint, erase, or select tiles.
    /// </summary>
    public enum TileEditorMode
    {
        Paint,
        Erase,
        Collision,
        Stair,
        Select
    }

    /// <summary>
    /// Represents the Tile Editor window in the editor, allowing users to create and edit tile layers.
    /// </summary>
    public class TileEditorWindow : IInspectable
    {
        private bool show = true;
        public int BrushSize = 1;
        public Tilemap? SelectedTilemap;

        TileEditorMode mode = TileEditorMode.Paint;
        public int tileX = 0;
        public int tileY = 0;

        /// <summary>
        /// Tile editor draw
        /// </summary>
        public void Draw()
        {
            if (ImGui.Begin("Tile Editor", ref show))
            {
                EditorApp.Instance.selectionMode = EditorSelectionMode.Tile;
                DrawEditor();
            }
            ImGui.Separator();
            ImGui.End();
        }

        /// <summary>
        /// Draws the inspector fields
        /// </summary>
        public void Render()
        {
            InspectorUI.DrawComponent(SelectedTilemap);
        }

        /// <summary>
        /// Draw the tile editor
        /// </summary>
        public void DrawEditor()
        {
            List<Tilemap> Tilemaps = new List<Tilemap>();
            Tilemaps.Clear();

            foreach (var gameObj in EngineContext.Current.Scene.objects)
            {
                foreach (var component in gameObj.components)
                {
                    if (component is Tilemap tilemap)
                    {
                        Tilemaps.Add(tilemap);
                    }
                }
            }

            // Draw the editor
            foreach (var map in Tilemaps)
            {
                // Select the tilemap
                bool selected = ImGui.Selectable($"{map.Name}##{map.GetHashCode()}");

                if (selected)
                {
                    InspectorWindow.Instance.Inspect(this);
                    SelectedTilemap = map;
                }
            }

            // Add more tilemaps
            if (ImGui.Button("Add Tilemap Layer"))
            {
                Tilemaps.Add(new Tilemap());
            }

            // No tilemap selected
            if (SelectedTilemap == null)
                return;
        }

        /// <summary>
        /// Handle the logic for painting tiles
        /// </summary>
        public void HandleTilepainting(SpriteBatch spriteBatch)
        {
            // Game is not focused
            if (EditorApp.Instance.selectionMode != EditorSelectionMode.Tile || !EditorApp.Instance.gameFocused || SelectedTilemap == null)
                return;

            // Paint tiles
            Vector3 mousePos = Input.mouseWorldPosition.ToNumerics();

            // Snap the mouse to the nearest tile cell
            mousePos.X = (int)Math.Floor(mousePos.X / SelectedTilemap.CellSize) * SelectedTilemap.CellSize;
            mousePos.Y = (int)Math.Floor(mousePos.Y / SelectedTilemap.CellSize) * SelectedTilemap.CellSize;

            // Grid coordinates (tile index)
            Vector3 gridPos = new Vector3(
                (int)(mousePos.X / SelectedTilemap.CellSize),
                (int)(mousePos.Y / SelectedTilemap.CellSize),
                0);

            // Place tile on click
            if (Input.IsMouseDown())
            {
                // Create a *copy* of the RuleTile asset
                RuleTile newTile = new RuleTile
                {
                    Rules = new List<TileRule>(SelectedTilemap.Tile.Rules.Select(r =>
                    {
                        // Deep-copy each rule (so instances don’t share references)
                        TileRule copy = new TileRule();
                        foreach (var kvp in r.Conditions)
                            copy.Conditions[kvp.Key] = kvp.Value;
                        copy.SelectedFrameIndex = r.SelectedFrameIndex;
                        return copy;
                    })),
                    DefaultFrameIndex = SelectedTilemap.Tile.DefaultFrameIndex,
                    TileIndex = SelectedTilemap.Tile.TileIndex,
                    Frame = SelectedTilemap.Tile.Frame
                };

                // Place the new tile into the map grid
                SelectedTilemap.SetTile(newTile, (int)gridPos.X, (int)gridPos.Y);
            }

            // Draw the preview overlay
            if (SelectedTilemap?.texture?.texture != null)
            {
                Microsoft.Xna.Framework.Rectangle tilePreview = new Microsoft.Xna.Framework.Rectangle(
                    (int)mousePos.X,
                    (int)mousePos.Y,
                    SelectedTilemap.CellSize,
                    SelectedTilemap.CellSize);

                // Draw whichever frame you want to preview (currently top-left)
                Microsoft.Xna.Framework.Rectangle sourceFrame = new Microsoft.Xna.Framework.Rectangle(
                    0,
                    0,
                    SelectedTilemap.CellSize,
                    SelectedTilemap.CellSize);

                spriteBatch.Draw(
                    SelectedTilemap.texture.texture,
                    tilePreview,
                    sourceFrame,
                    Microsoft.Xna.Framework.Color.Green * 0.5f); // semi-transparent overlay
            }


            // Erase tiles
            if (Input.IsMouseDown(Engine.Core.Systems.Button.Right))
            {
                SelectedTilemap.SetTile(null, gridPos);
            }
        }

        public bool IsVisible => show;
        public void SetVisible(bool visible) => show = visible;
    }
}

