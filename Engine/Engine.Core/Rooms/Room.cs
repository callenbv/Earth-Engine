using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Engine.Core.Game;
using Editor.AssetManagement;
using Engine.Core.Game.Components;
using System.Text.Json.Serialization;
using MonoGame.Extended.Serialization.Json;
using Engine.Core.Data;
using System.IO;

namespace Engine.Core.Rooms
{
    public class Room
    {
        public string Name { get; set; } = "Room";
        public string FilePath { get; set; } = string.Empty;
        public List<GameObject> objects { get; set; } = new List<GameObject>();

        /// <summary>
        /// Render a scene
        /// </summary>
        public void Render(SpriteBatch spriteBatch)
        {
            foreach (var obj in objects)
            {
                obj.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Update a scene
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            foreach (var obj in objects)
            {
                obj.Update(gameTime);
            }
        }

        /// <summary>
        /// Creates a room given a path
        /// </summary>
        /// <param name="path"></param>
        public static Room? Load(string path)
        {
            // Deserialize our scene
            string fullPath = Path.Combine(EnginePaths.ProjectBase, "Assets", path);
            Console.WriteLine($"Loading scene from: {fullPath}");

            Room scene = new Room();
            string name = Path.GetFileName(path);

            try
            {
                string json = File.ReadAllText(fullPath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new ComponentListJsonConverter() },
                    IncludeFields = true,
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
                    scene.Name = name;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load scene: {ex.Message}");
            }

            return scene;
        }

        /// <summary>
        /// Find a GameObject by name
        /// </summary>
        /// <param name="name">Name to search for</param>
        /// <returns>The GameObject if found, null otherwise</returns>
        public GameObject? FindByName(string name)
        {
            return objects.FirstOrDefault(obj => !obj.IsDestroyed && obj.Name == name);
        }
    }
}
