using EarthEngineEditor;
using EarthEngineEditor.Windows;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Graphics;
using Engine.Core.Scripting;
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

    public class EarthProject
    {
        public string optionsPath = string.Empty;

        public GameOptions settings;

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
            settings.LastScene = SceneViewWindow.Instance.scene?.FilePath;
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(optionsPath, json);

            // Editor settings
            EditorApp.Instance?._settings?.Save();
        }

        /// <summary>
        /// Load all project settings
        /// </summary>
        public void Load()
        {
            // Load per project assets
            EditorApp.Instance.runtime.Initialize();

            // Load scripts in project if possible
            ScriptCompiler.CompileAndLoadScripts(ProjectSettings.ProjectDirectory, out var scriptManager);

            // Load any game options
            if (File.Exists(optionsPath))
            {
                string json = File.ReadAllText(optionsPath);
                settings = JsonSerializer.Deserialize<GameOptions>(json);

                Asset scene = Asset.Get(Path.GetFileName(settings.LastScene));
                InspectorWindow.Instance.Inspect(scene);
                ProjectSettings.RuntimePath = settings.RuntimePath ?? string.Empty;

                if (scene != null)
                {
                    scene.Open();
                }

                EditorApp.Instance.runtime.gameOptions = settings;
                Console.WriteLine("Last Scene: " + settings.LastScene);
            }
            else
            {
                Console.WriteLine("No project settings file found at: " + optionsPath);
            }
        }
    }
}
