using EarthEngineEditor;
using EarthEngineEditor.Windows;
using Editor.AssetManagement;
using Editor.Windows.ImGuiWrappers;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editor.Windows.Homepage
{
    /// <summary>
    /// Homepage for making new projects, managing recent projects
    /// </summary>
    public class Homepage
    {
        public bool Active = false;
        public EButton OpenProjectButton;
        public EButton NewProjectButton;
        public EButton Help;
        public EDropdown<string> RecentProjects;
        public List<EWidget> MainContainer = new List<EWidget>();

        float containerWidth = 400f;
        float containerHeight = 48f;

        /// <summary>
        /// Initialize homepage buttons
        /// </summary>
        public Homepage()
        {
            OpenProjectButton = new EButton("Open Project", new System.Numerics.Vector2(containerWidth, containerHeight));
            NewProjectButton = new EButton("Create New Project", new System.Numerics.Vector2(containerWidth, containerHeight));
            Help = new EButton("Help", new System.Numerics.Vector2(containerWidth, containerHeight));
            RecentProjects = new EDropdown<string>("Recent Projects", new System.Numerics.Vector2(containerWidth,containerHeight));

            // Bind functions
            OpenProjectButton.Bind(FileActions.SelectProject);

            // Add to the main container
            MainContainer.Add(OpenProjectButton);
            MainContainer.Add(NewProjectButton);
            MainContainer.Add(RecentProjects);
        }

        /// <summary>
        /// If we are connected to internet
        /// </summary>
        public bool ConnectedToInternet = false;

        /// <summary>
        /// If we need an update (runtime or IDE is out of date)
        /// </summary>
        public bool NeedsUpdate = false;

        /// <summary>
        /// Renders the homepage
        /// </summary>
        public void Render()
        {
            if (!Active)
                return;

            ImGui.Begin("Homepage",ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse);
            ImGui.SetCursorPosY(ImGui.GetWindowHeight()/4);
            RecentProjects.Items = EditorApp.Instance._windowManager.recentProjects;

            foreach (var widget in MainContainer)
            {
                ImGuiRenderer.CenterDrawing(containerWidth);
                widget.Draw();
            }

            ImGui.End();
        }
    }
}
