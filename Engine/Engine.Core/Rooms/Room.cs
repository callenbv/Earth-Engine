/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Room.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Engine.Core.Game;
using Editor.AssetManagement;
using Engine.Core.Game.Components;
using System.Text.Json.Serialization;
using MonoGame.Extended.Serialization.Json;
using Engine.Core.Data;
using System.Reflection;
using Engine.Core.Systems;

namespace Engine.Core.Rooms
{
    /// <summary>
    /// Represents a room or scene in the game, containing GameObjects and their components.
    /// </summary>
    public class Room
    {
        /// <summary>
        /// Name of the room. This is used to identify the room in the editor and in the game.
        /// </summary>
        public string Name { get; set; } = "Room";

        /// <summary>
        /// File path of the room. This is used to load the room from disk.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// List of GameObjects in the room. Each GameObject can have multiple components.
        /// </summary>
        public List<GameObject> objects { get; set; } = new List<GameObject>();

        /// <summary>
        /// Render a scene
        /// </summary>
        public void Render(SpriteBatch spriteBatch)
        {
            try
            {
                foreach (var obj in objects)
                {
                    obj.Draw(spriteBatch);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering scene: {ex.Message}");
            }
        }

        /// <summary>
        /// Render UI elements in the scene
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void RenderUI(SpriteBatch spriteBatch)
        {
            try
            {
                foreach (var obj in objects)
                {
                    obj.DrawUI(spriteBatch);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering scene: {ex.Message}");
            }
        }

        /// <summary>
        /// Update a scene
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            List<GameObject> destroyedObjects = new List<GameObject>();

            try
            {
                foreach (var obj in objects)
                {
                    // Destroyed objects defer their destruction
                    if (obj.IsDestroyed)
                        destroyedObjects.Add(obj);

                    obj.Update(gameTime);
                }

                CollisionSystem.Update(gameTime);

                // Remove destroyed objects
                foreach (GameObject obj in destroyedObjects)
                {
                    objects.Remove(obj);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating scene: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a room given a path
        /// </summary>
        /// <param name="path"></param>
        public static Room Load(string path)
        {
            string fullPath = Path.Combine(EnginePaths.AssetsBase, path);
            Console.WriteLine($"Loading scene from: {fullPath}");

            Room scene = new Room();
            string name = Path.GetFileName(path);

            try
            {
                string json = File.ReadAllText(fullPath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    IncludeFields = true,
                    Converters =
            {
                new ComponentListJsonConverter(),
                new Vector2JsonConverter(),
                new ColorJsonConverter()
            }
                };

                var sceneData = JsonSerializer.Deserialize<Room>(json, options);

                if (sceneData != null)
                {
                    scene = sceneData;
                    scene.Name = name;

                    foreach (var obj in scene.objects)
                    {
                        foreach (var component in obj.components)
                        {
                            if (component is ObjectComponent comp)
                            {
                                comp.Owner = obj;
                                comp.Initialize();
                            }
                        }
                    }

                    GameReferenceResolver.Resolve(scene.objects);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load scene: {ex.Message}");
            }

            return scene;
        }

        /// <summary>
        /// Initialize the room and all of its objects & components
        /// </summary>
        public void Initialize()
        {
            foreach (var obj in objects)
            {
                foreach (var component in obj.components)
                {
                    Console.WriteLine($"Initializing component {component.Name} on object {obj.Name}");
                    component.Create();
                }
            }
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

