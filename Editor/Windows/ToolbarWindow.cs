/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ToolbarWindow.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.Windows;
using Engine.Core;
using Engine.Core.Systems;
using Engine.Core.Graphics;
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
            // Toggle grid and wireframe with Ctrl
            if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
            {
                if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.G))
                {
                    ToggleGrid();
                }
                
                // Toggle wireframe
                if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.W))
                {
                    EngineContext.Wireframe = !EngineContext.Wireframe;
                    ToggleWireframe();
                }
            }

            // Grid size adjustment (when grid is visible)
            if (Grid3D.Instance.Visible)
            {
                if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.OemCloseBrackets))
                {
                    Grid3D.Instance.UpdateGridSize(Grid3D.Instance.GridSize + 4f, EngineContext.Current.GraphicsDevice);
                }
                if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.OemOpenBrackets))
                {
                    Grid3D.Instance.UpdateGridSize(Math.Max(4f, Grid3D.Instance.GridSize - 4f), EngineContext.Current.GraphicsDevice);
                }
            }

            // Run game
            if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.F5))
            {
                EditorApp.Instance.LaunchGame();
            }
        }

        /// <summary>
        /// Turn the grid on/off
        /// </summary>
        private void ToggleGrid()
        {
            showGrid = !showGrid;
        }

        /// <summary>
        /// Turn wireframe mode on/off
        /// </summary>
        private void ToggleWireframe()
        {
            // Set global wireframe state in EngineContext - MeshRenderer will check this
            Console.WriteLine($"[Wireframe] Global wireframe mode: {(EngineContext.Wireframe ? "ON" : "OFF")}");
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
            {
                EditorApp.Instance.LaunchGame();
            }

            ImGui.SameLine();

            // Grid Toggle
            string gridIcon = "\uf00a";
            Microsoft.Xna.Framework.Color gridColor = showGrid ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Gray;

            if (ImGuiRenderer.IconButton("grid", gridIcon, gridColor))
            {
                showGrid = !showGrid;
                ToggleGrid();
            }

            ImGui.SameLine();

            // Wireframe Toggle
            string wireframeIcon = "\uf545"; // Cube outline icon
            Microsoft.Xna.Framework.Color wireframeColor = EngineContext.Wireframe ? Microsoft.Xna.Framework.Color.White : Microsoft.Xna.Framework.Color.Gray;

            if (ImGuiRenderer.IconButton("wireframe", wireframeIcon, wireframeColor))
            {
                EngineContext.Wireframe = !EngineContext.Wireframe;
                ToggleWireframe();
            }

            ImGui.SameLine();

            // Camera Details
            Vector2 cameraPosition = new Vector2(Input.mouseWorldPosition.X, Input.mouseWorldPosition.Y);
            cameraPosition.Round();
            ImGui.Text($"{Camera.Main.Position} FOV DEG: {Camera3D.Main.FieldOfViewDegrees} Far: {Camera3D.Main.FarPlane} Near: {Camera3D.Main.NearPlane}" );

            ImGui.End();
        }

        public bool IsVisible => showToolbarWindow;
        public void SetVisible(bool visible) => showToolbarWindow = visible;
    }
} 
