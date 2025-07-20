using EarthEngineEditor.Windows;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Rooms;
using GameRuntime;
using ImGuiNET;
using MonoGame.Extended.Serialization.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Editor.AssetManagement
{
    public class SceneHandler : IAssetHandler
    {
        private Room? scene;

        /// <summary>
        /// Load the scene from a room file
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            scene = Room.Load(path);
        }

        /// <summary>
        /// Save the room data if it changed
        /// </summary>
        public void Save(string path)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new ComponentListJsonConverter() },
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                options.Converters.Add(new Vector2JsonConverter());
                options.Converters.Add(new ColorJsonConverter());

                string json = JsonSerializer.Serialize(scene, options);
                File.WriteAllText(path, json);
                Console.WriteLine($"Scene saved: {scene.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save scene: {ex.Message}");
            }
        }

        public void Open(string path)
        {
            SceneViewWindow.Instance.scene = scene;
            SceneViewWindow.Instance.scene.FilePath = path;
            RuntimeManager.Instance.scene = scene;
        }

        public void Render()
        {
            
        }

        public void Unload()
        {
            scene = null;
        }
    }
}
