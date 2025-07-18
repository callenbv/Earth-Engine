using EarthEngineEditor.Windows;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Systems.Rooms;
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
        public void Load(string path)
        {
            scene = new Room();

            // Deserialize our scene
            string name = Path.GetFileName(path);
            scene.Name = name;

            try
            {
                string json = File.ReadAllText(path);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new ComponentListJsonConverter() },
                    ReferenceHandler = ReferenceHandler.Preserve,
                };
                options.Converters.Add(new Vector2JsonConverter());
                options.Converters.Add(new ColorJsonConverter());


                scene = JsonSerializer.Deserialize<Room>(json, options);
                if (scene != null)
                {
                    Console.WriteLine($"Loaded scene: {scene.Name}");
                    foreach (var obj in scene.objects)
                    {
                        foreach (var component in obj.components)
                        {
                            if (component is ObjectComponent comp)
                            {
                                comp.Owner = obj;
                                comp.Create();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load scene: {ex.Message}");
            }
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

        public void Open()
        {
            SceneViewWindow.Instance.scene = scene;
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
