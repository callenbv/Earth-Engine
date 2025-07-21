using Editor.Windows;
using Engine.Core;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace EarthEngineEditor.Windows
{
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

            // Grid Toggle
            if (ImGui.Checkbox("Grid", ref showGrid))
            {
                ToggleGrid();
            }

            ImGui.SameLine();

            // Play Button
            if (ImGui.Button("Play"))
            {
                EditorApp.Instance.runtime.Launch();
            }
            ImGui.SameLine();

            // Camera Details
            Vector2 cameraPosition = Camera.Main.Position;
            cameraPosition.Round();
            ImGui.Text($"{cameraPosition} Zoom: {Camera.Main.Zoom}x");

            ImGui.End();
        }

        public bool IsVisible => showToolbarWindow;
        public void SetVisible(bool visible) => showToolbarWindow = visible;
    }
} 