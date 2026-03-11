/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ProjectItem.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor;
using EarthEngineEditor.Windows;
using Engine.Core;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Rooms;
using Engine.Core.Scripting;
using GameRuntime;
using System.IO;
using System.Text.Json;

/// <summary>
/// Define a 2D or 3D project type
/// </summary>
public enum ProjectType
{
    Project2D,
    Project3D
}

namespace Editor.AssetManagement
{
    /// <summary>
    /// Represents the Earth Engine project, containing settings and assets.
    /// </summary>
    public class EarthProject
    {
        public string optionsPath = string.Empty;
        public GameOptions settings;
        public ProjectType ProjectType { get; set; } = ProjectType.Project2D;
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Give ourselves quick paths
        /// </summary>
        public EarthProject()
        {
            optionsPath = Path.Combine(ProjectSettings.ProjectDirectory, "game_options.json");
            settings = new GameOptions();
        }

        /// <summary>
        /// Save the project
        /// </summary>
        public void Save()
        {
            // Project settings
            settings.StartScene = SceneViewWindow.Instance.scene == null ? settings.StartScene : SceneViewWindow.Instance.scene.FilePath;
            settings.Title = Name;

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(optionsPath, json);

            // Editor settings
            EditorApp.Instance?._settings?.Save();
            TilemapManager.Save();
        }

        /// <summary>
        /// Load all project settings
        /// </summary>
        public void Load()
        {
            // Load per project assets
            EditorApp.Instance.runtime.LoadAssets();

            // Load static tilemaps
            TilemapManager.Load(Path.Combine(ProjectSettings.ProjectDirectory,"Tilemaps","tilemaps.json"));

            // Load scripts in project if possible
            ScriptCompiler.CompileAndLoadScripts(ProjectSettings.ProjectDirectory, out var scriptManager);

            // Load any game options
            if (File.Exists(optionsPath))
            {
                try
                {
                    string json = File.ReadAllText(optionsPath);

                    settings = JsonSerializer.Deserialize<GameOptions>(json);
                    Name = settings.Title == null ? "Empty" : settings.Title;

                    SceneAsset? sceneAsset = Asset.Get<SceneAsset>(Path.GetFileName(settings.StartScene));

                    if (sceneAsset != null)
                    {
                        SceneManager.EnterScene(sceneAsset);
                        InspectorWindow.Instance.Inspect(sceneAsset);
                    }

                    EditorApp.Instance.runtime.gameOptions = settings;

                    // Initialize EngineContext from loaded settings
                    EngineContext.UnitsPerPixel = settings.UnitsPerPixel;
                    Console.WriteLine("Last Scene: " + settings.StartScene);
                }
                catch
                {
                    throw new Exception($"ERROR: Loading Project Options {optionsPath} failed");
                }

            }
            else
            {
                Console.WriteLine("No project settings file found at: " + optionsPath);

                // Initialize with default values for new projects
                EngineContext.UnitsPerPixel = settings.UnitsPerPixel; // Uses default value of 1f
            }
        }


        /// <summary>
        /// Populates the scene with default objects (camera, etc.)
        /// </summary>
        public void PopulateScene()
        {
            // Type based 
            switch (ProjectType)
            {
                case ProjectType.Project2D:
                    GameObject camera2D = new GameObject("Camera");
                    RuntimeManager.Instance.scene.objects.Add(camera2D);
                    break;

                case ProjectType.Project3D:
                    GameObject camera3D = new GameObject("Camera");
                    camera3D.AddComponent<Camera3DController>();
                    RuntimeManager.Instance.scene.objects.Add(camera3D);
                    break;
            }
        }
    }
}

