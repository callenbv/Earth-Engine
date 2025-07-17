using EarthEngineEditor.Windows;
using Engine.Core.Data;
using Engine.Core.Graphics;
using Engine.Core.Systems.Rooms;
using GameRuntime;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Editor.AssetManagement
{

    public class ProjectSettingsData
    { 
        public string? LastScene {  get; set; } = string.Empty;
        public string? GameName {  get; set; } = string.Empty;
    }

    public class EarthProject
    {
        public string optionsPath = string.Empty;

        public ProjectSettingsData settings;

        /// <summary>
        /// Give ourselves quick paths
        /// </summary>
        public EarthProject()
        {
            optionsPath = Path.Combine(ProjectSettings.ProjectDirectory, "game_options.json");
            settings = new ProjectSettingsData();
        }

        /// <summary>
        /// Save the project
        /// </summary>
        public void Save()
        {
            settings.LastScene = SceneViewWindow.Instance.scene?.Name;
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(optionsPath, json);
        }

        /// <summary>
        /// Load all project settings
        /// </summary>
        public void Load()
        {
            TextureLibrary.Instance.LoadTextures();

            if (File.Exists(optionsPath))
            {
                string json = File.ReadAllText(optionsPath);
                settings = JsonSerializer.Deserialize<ProjectSettingsData>(json);

                Asset scene = Asset.Get(settings.LastScene);

                if (scene != null)
                {
                    scene.Open();
                }

                Console.WriteLine("Last Scene: " + settings.LastScene);
            }
            else
            {
                Console.WriteLine("No project settings file found at: " + optionsPath);
            }
        }
    }
}
