/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Homepage.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2026 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor;
using Editor.AssetManagement;
using Editor.UI.Homepage;
using Editor.Windows.ImGuiWrappers;
using Engine.Core;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Graphics;
using ImGuiNET;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Editor.Windows.Homepage
{
    /// <summary>
    /// Homepage for making new projects, managing recent projects
    /// </summary>
    public class Homepage
    {
        public bool Active = true;
        public EButton OpenProjectButton;
        public EButton NewProjectButton;
        public EButton Help;
        public EDropdown<string> RecentProjects;
        public List<EWidget> MainContainer = new List<EWidget>();
        public List<EWidget> DemoProjects = new List<EWidget>();

        float containerWidth = 400f;
        float containerHeight = 48f;
        private string DemoProjectDirectory = "DemoProjects";

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
            // MainContainer.Add(RecentProjects);
        }

        public void Initialize()
        {
            // Load demo projects
            string DemoProjectDirectory = "../../../../DemoProjects";
            string EditorAssets = "../../../Content/Assets/Textures";

            DemoProjectDirectory = Path.GetFullPath(DemoProjectDirectory);

            if (Directory.Exists(DemoProjectDirectory))
            {
                var files = Directory.EnumerateFiles(DemoProjectDirectory, "*.earthproj", SearchOption.AllDirectories);

                // Recursively check for projects
                foreach (var f in files)
                {
                    // Find game options to get the project settings
                    string json = File.ReadAllText(f);

                    var projectData = JsonSerializer.Deserialize<EarthProject>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    );

                    // Create the demo project
                    if (projectData != null)
                    {
                        DemoProject project = new DemoProject();
                        project.ProjectPath = f;
                        project.ProjectName = projectData.Name;
                        DemoProjects.Add(project);
                    }
                }
            }

            // Load editor assets
            if (Directory.Exists(EditorAssets))
            {
                var files = Directory.EnumerateFiles(EditorAssets, "*.png", SearchOption.AllDirectories);

                // Recursively check for projects
                foreach (var f in files)
                {
                    TextureLibrary.Instance.LoadTexture(f);
                }
            }
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
            ImGui.SetCursorPosY(ImGui.GetWindowHeight()/8);
            RecentProjects.Items = EditorApp.Instance._windowManager.recentProjects;

            foreach (var widget in MainContainer)
            {
                ImGuiRenderer.CenterDrawing(containerWidth);
                widget.Draw();
            }

            ImGui.NewLine();
            ImGui.Text("DEMO PROJECTS");
            ImGui.Separator();

            foreach (var project in DemoProjects)
            {
                project.Draw();
            }

            ImGui.End();
        }
    }
}

