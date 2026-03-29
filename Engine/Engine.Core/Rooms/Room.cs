/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Room.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.AssetManagement;
using Engine.Core.CustomMath;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Serialization.Json;
using System;
using System.Text.Json;

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
            if (EngineContext.UIOnly)
                return;

            try
            {
                foreach (var obj in objects)
                {
                    if (obj.Parent != null)
                        continue;

                    if (!obj.Active)
                        continue;

                    DrawHierarchy(obj, spriteBatch);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rendering scene: {ex.Message}");
            }
        }

        /// <summary>
        /// Render 3D components before the 2D sprite batch.
        /// </summary>
        public void Render3D(GraphicsDevice graphicsDevice, Matrix view, Matrix projection)
        {
        }

        /// <summary>
        /// Render UI elements in the scene
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void RenderUI(SpriteBatch spriteBatch)
        {
            Camera.Main.DrawUI(spriteBatch);

            try
            {
                foreach (var obj in objects)
                {
                    if (obj.Parent != null)
                        continue;

                    // Ignore deactive objects
                    if (!obj.Active)
                        continue;

                    DrawUIHierarchy(obj, spriteBatch);
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

            // Sort gameobjects by rendering order
            objects.Sort((a,b) => a.RenderOrder.CompareTo(b.RenderOrder));

            try
            {
                foreach (var obj in objects)
                {
                    // Destroyed objects defer their destruction
                    if (obj.IsDestroyed)
                        destroyedObjects.Add(obj);

                    if (obj.Parent != null)
                        continue;

                    // Ignore deactive objects
                    if (!obj.Active)
                        continue;

                    UpdateHierarchy(obj, gameTime, destroyedObjects);
                }

                CollisionSystem.Update(gameTime);

                // Remove destroyed objects
                foreach (GameObject obj in destroyedObjects)
                {
                    obj.SetParent(null);
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
            scene.FilePath = fullPath;

            try
            {
                string json = File.ReadAllText(fullPath);
                var sceneData = DeserializeRoomJson(json);

                if (sceneData != null)
                {
                    scene = sceneData;
                    scene.Name = name;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load scene: {ex.Message}");
            }

            return scene;
        }

        /// <summary>
        /// Loads a room relative to itself
        /// </summary>
        public void Load()
        {
            Load(FilePath);
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

        public static List<GameObject> DuplicateHierarchy(GameObject root)
        {
            var sourceObjects = GetHierarchyObjects(root);
            var tempRoom = new Room
            {
                Name = "Clipboard",
                objects = sourceObjects
            };

            string json = JsonSerializer.Serialize(tempRoom, CreateRoomSerializationOptions(writeIndented: false));
            Room? cloneRoom = DeserializeRoomJson(json);

            if (cloneRoom == null)
                return new List<GameObject>();

            RefreshObjectIds(cloneRoom.objects);
            InitializeComponents(cloneRoom.objects, createComponents: true);

            return cloneRoom.objects;
        }

        private static List<GameObject> GetHierarchyObjects(GameObject root)
        {
            List<GameObject> objects = new();

            void Collect(GameObject current)
            {
                objects.Add(current);
                foreach (var child in current.children)
                {
                    Collect(child);
                }
            }

            Collect(root);
            return objects;
        }

        private static JsonSerializerOptions CreateRoomSerializationOptions(bool writeIndented)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                WriteIndented = writeIndented,
                Converters =
                {
                    new ComponentListJsonConverter(),
                    new Vector2JsonConverter(),
                    new Vector3JsonConverter(),
                    new ColorJsonConverter()
                }
            };

            return options;
        }

        private static Room? DeserializeRoomJson(string json)
        {
            var scene = JsonSerializer.Deserialize<Room>(json, CreateRoomSerializationOptions(writeIndented: false));
            if (scene == null)
                return null;

            InitializeComponents(scene.objects, createComponents: false);
            GameReferenceResolver.Resolve(scene.objects);
            ComponentReferenceResolver.Resolve(scene.objects);
            RebuildHierarchy(scene.objects);
            return scene;
        }

        private static void InitializeComponents(List<GameObject> objects, bool createComponents)
        {
            Console.WriteLine($"[Room] Setting up {objects.Count} objects");
            foreach (var obj in objects)
            {
                foreach (var component in obj.components)
                {
                    try
                    {
                        if (component is ObjectComponent comp)
                        {
                            comp.Owner = obj;
                            comp.Initialize();

                            if (createComponents)
                                comp.Create();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Room] Error setting up component {component.GetType().Name} on {obj.Name}: {ex.Message}");
                        Console.Error.WriteLine($"[Room] Stack trace: {ex.StackTrace}");
                    }
                }
            }
        }

        private static void RefreshObjectIds(List<GameObject> objects)
        {
            foreach (var obj in objects)
            {
                obj.ID = Guid.NewGuid();
            }

            foreach (var obj in objects)
            {
                obj.ParentID = obj.Parent?.ID;

                foreach (var component in obj.components)
                {
                    if (component is ObjectComponent objectComponent)
                    {
                        objectComponent.ID = ERandom.Range(0, 9999999);
                    }
                }
            }
        }

        private static void RebuildHierarchy(List<GameObject> objects)
        {
            var parentLookup = objects.ToDictionary(obj => obj, obj => obj.ParentID);

            foreach (var obj in objects)
            {
                obj.children.Clear();
                obj.SetParent(null);
            }

            var byId = objects.ToDictionary(o => o.ID, o => o);
            foreach (var obj in objects)
            {
                var parentId = parentLookup[obj];
                if (parentId.HasValue && byId.TryGetValue(parentId.Value, out var parent))
                {
                    obj.SetParent(parent);
                }
            }
        }

        private static void UpdateHierarchy(GameObject obj, GameTime gameTime, List<GameObject> destroyedObjects)
        {
            obj.Update(gameTime);

            foreach (var child in obj.children)
            {
                if (child.IsDestroyed)
                    destroyedObjects.Add(child);

                if (!child.Active)
                    continue;

                UpdateHierarchy(child, gameTime, destroyedObjects);
            }
        }

        private static void DrawHierarchy(GameObject obj, SpriteBatch spriteBatch)
        {
            obj.Draw(spriteBatch);

            foreach (var child in obj.children)
            {
                if (!child.Active)
                    continue;

                DrawHierarchy(child, spriteBatch);
            }
        }

        private static void DrawUIHierarchy(GameObject obj, SpriteBatch spriteBatch)
        {
            obj.DrawUI(spriteBatch);

            foreach (var child in obj.children)
            {
                if (!child.Active)
                    continue;

                DrawUIHierarchy(child, spriteBatch);
            }
        }
    }
}

