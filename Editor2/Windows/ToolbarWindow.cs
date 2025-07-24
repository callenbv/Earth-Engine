/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ToolbarWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.Windows;
using Engine.Core;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace EarthEngineEditor.Windows
{
    /// <summary>
    /// Represents the Toolbar window in the editor, providing controls for game actions such as running the game and toggling the grid.
    /// </summary>
    public class ToolbarWindow
    {
        private bool showToolbarWindow = true;
        public bool showGrid = true;
        public static ToolbarWindow Instance;

        public ToolbarWindow()
        {
            Instance = this;
        }

        /// <summary>
        /// Handle hotkeys
        /// </summary>
        private void HandleHotkeys()
        {
            // Toggle grid
            if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
            {
                if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.G))
                {
                    showGrid = !showGrid;
                    ToggleGrid();
                }
            }

            // Run game
            if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F5))
            {
                EditorApp.Instance._windowManager.SaveProject();
                EditorApp.Instance.runtime.Launch();
            }
        }

        /// <summary>
        /// Turn the grid on/off
        /// </summary>
        private void ToggleGrid()
        {
            EditorOverlay.Instance.showGrid = showGrid;
        }

        /// <summary>
        ///  Render the toolbar
        /// </summary>
        public void Render()
        {
            HandleHotkeys();
            if (!showToolbarWindow) return;

            ImGui.Begin("Game", ref showToolbarWindow);

            // Play button
            if (ImGuiRenderer.IconButton("play", "\uf04b", Microsoft.Xna.Framework.Color.White))
                EditorApp.Instance.runtime.Launch();

            ImGui.SameLine();

            // Grid Toggle
            string gridIcon = showGrid ? "\uf00a" : "\uf00a";
            Microsoft.Xna.Framework.Color gridColor = showGrid ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Gray;

            if (ImGuiRenderer.IconButton("grid", gridIcon, gridColor))
            {
                showGrid = !showGrid;
                ToggleGrid();
            }

            ImGui.SameLine();

            // Camera Details
            Vector2 cameraPosition = Input.mouseWorldPosition;
            cameraPosition.Round();
            ImGui.Text($"{cameraPosition} Zoom: {Camera.Main.Zoom}x");

            ImGui.End();
        }

        public bool IsVisible => showToolbarWindow;
        public void SetVisible(bool visible) => showToolbarWindow = visible;
    }
} 
