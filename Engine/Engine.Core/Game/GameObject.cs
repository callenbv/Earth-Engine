/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         GameObject.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core.Game.Components;
using Editor.AssetManagement;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Core.Data;
using MonoGame.Extended.Serialization.Json;
using Engine.Core.CustomMath;

namespace Engine.Core.Game
{
    /// <summary>
    /// Represents a game object in the scene, which can have multiple components attached to it.
    /// </summary>
    public class GameObject : IComponentContainer, IInspectable
    {
        /// <summary>
        /// Name of the GameObject, used for identification and debugging.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Position of the GameObject in the game world.
        /// </summary>
        public Vector2 Position
        {
            get => GetComponent<Transform>()?.Position ?? Vector2.Zero;
            set
            {
                var transform = GetComponent<Transform>();
                if (transform != null)
                    transform.Position = value;
            }
        }

        /// <summary>
        /// Rotation of the GameObject in radians, affecting its orientation in the game world.
        /// </summary>
        public float Rotation
        {
            get => GetComponent<Transform>()?.Rotation ?? 0f;
            set
            {
                var transform = GetComponent<Transform>();
                if (transform != null)
                    transform.Rotation = value;
            }
        }

        /// <summary>
        /// Scale of the GameObject, affecting its size in the game world.
        /// </summary>
        public Vector2 Scale
        {
            get => GetComponent<Transform>()?.Scale ?? Vector2.One;
            set
            {
                var transform = GetComponent<Transform>();
                if (transform != null)
                    transform.Scale = value;
            }
        }

        /// <summary>
        /// List of child GameObjects that are part of this GameObject's hierarchy.
        /// </summary>
        public List<GameObject> children = new List<GameObject>();

        /// <summary>
        /// Unique identifier for the GameObject, used for serialization and identification.
        /// </summary>
        public Guid ID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Parent GameObject, if this GameObject is part of a hierarchy.
        /// </summary>
        public GameObject? Parent { get; set; } = null!; // Parent GameObject, if any. Initialized to null.

        /// <summary>
        /// List of components attached to this GameObject.
        /// </summary>

        [JsonConverter(typeof(ComponentListJsonConverter))]
        public List<IComponent> components { get; set; } = new();

        /// <summary>
        /// Indicates whether the GameObject has been destroyed.
        /// </summary>
        public bool IsDestroyed { get; private set; } = false;

        /// <summary>
        /// Gets the Sprite2D component if it exists, otherwise returns null.
        /// </summary>
        [JsonIgnore]
        [HideInInspector]
        public Sprite2D? Sprite
        {
            get => GetComponent<Sprite2D>();
        }

        /// <summary>
        /// Default constructor for GameObject, initializes with an empty name.
        /// </summary>
        public GameObject()
        {
            OnCreate();
        }

        /// <summary>
        /// Constructor for GameObject with a specified name.
        /// </summary>
        /// <param name="name"></param>
        public GameObject(string name)
        {
            Name = name;
            OnCreate();
        }

        /// <summary>
        /// Called when the GameObject is created.
        /// </summary>
        public void OnCreate()
        {

        }

        /// <summary>
        /// Creates a new component and attaches it
        /// </summary>
        /// <param name="component"></param>
        public T AddComponent<T>() where T : ObjectComponent, new()
        { 
            var component = new T
            {
                Owner = this
            };

            components.Add(component);
            component.Initialize();
            Console.WriteLine($"Added component {component.Name}");

            return component;
        }

        /// <summary>
        /// Add an Icomponent
        /// </summary>
        /// <param name="component"></param>
        public void AddComponent(IComponent component)
        {
            ((ObjectComponent)component).Owner = this;
            components.Add(component);
            component.Initialize();
            Console.WriteLine($"Added component {component.Name}");
        }

        /// <summary>
        /// Gets a component if possible
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? GetComponent<T>() where T : ObjectComponent
        {
            return components.OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Add an already existing component 
        /// </summary>
        /// <param name="comp"></param>
        public void AddComponent(ObjectComponent? component)
        {
            if (component == null)
            {
                Console.WriteLine($"Tried to add null component");
                return;
            }

            component.Owner = this;
            components.Add(component);
            component.Initialize();
        }

        /// <summary>
        /// Destroy method, accessible via scripts
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed) return;
            
            IsDestroyed = true;
        }

        /// <summary>
        /// Update object
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            foreach (var component in components)
            {
                try
                {
                    // Check for no owner
                    if (component is ObjectComponent objComp)
                    {
                        if (objComp.Owner == null)
                            continue;

                        // Do not update scripts if the engine is paused
                        if (!EngineContext.Running && !objComp.UpdateInEditor)
                        {
                            continue;
                        }
                    }

                    component.Update(gameTime);
                }
                catch (Exception e) 
                {
                    Console.WriteLine($"Error updating component for {Name}: {e.Message}");
                }
            }

            foreach (var obj in children)
            {
                obj.Update(gameTime);
            }
        }

        /// <summary>
        /// Draw the object and its attached scripts
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw components
            foreach (var component in components)
            {
                try
                {
                    component.Draw(spriteBatch);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing script for {Name}: {ex.Message}");
                }
            }

            foreach (var obj in children)
            {
                obj.Draw(spriteBatch);
            }
        }

        /// <summary>
        /// Draw the object and its attached scripts
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void DrawUI(SpriteBatch spriteBatch)
        {
            // Draw components
            foreach (var component in components)
            {
                try
                {
                    component.DrawUI(spriteBatch);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing UI script for {Name}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Instantiate game object from definition
        /// </summary>
        /// <param name="defName">Name of the object definition to instantiate</param>
        /// <param name="position">Position to place the GameObject</param>
        /// <returns>Instantiated GameObject</returns>
        public static GameObject Instantiate(string defName, Vector2 position)
        {
            // Check that the prefab exists
            string path = Path.Combine(ProjectSettings.AssetsDirectory, defName);
            if (!File.Exists(path))
            {
                Console.WriteLine($"[Instantiate] Prefab not found: {path}");
                return new GameObject("Missing");
            }

            try
            {
                // Deserialize game object
                string json = File.ReadAllText(path);

                var options = new JsonSerializerOptions
                {
                    Converters = { new ComponentListJsonConverter() },
                    PropertyNameCaseInsensitive = true
                };

                options.Converters.Add(new Vector2JsonConverter());
                options.Converters.Add(new ColorJsonConverter());

                var def = JsonSerializer.Deserialize<GameObjectDefinition>(json, options);
                if (def == null)
                {
                    Console.WriteLine($"[Instantiate] Failed to deserialize prefab: {defName}");
                    return new GameObject("Error");
                }

                // Attach components
                GameObject obj = new GameObject(def.Name);
                foreach (var comp in def.components)
                    obj.AddComponent(comp);

                // Set transform position
                var transform = obj.GetComponent<Transform>();
                if (transform != null)
                    transform.Position = position;

                // Give an empty visual
                var sprite = obj.GetComponent<Sprite2D>();
                if (sprite != null)
                    sprite.Initialize();

                // Add to the scene
                string objectName = Path.GetFileNameWithoutExtension(defName);
                obj.Name = $"{objectName}{EngineContext.Current.Scene?.objects.Count}";
                EngineContext.Current.Scene?.objects.Add(obj);
                return obj;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Instantiate] Error loading prefab '{defName}': {ex.Message}");
                return new GameObject("Error");
            }
        }

        /// <summary>
        /// Check if this GameObject is a descendant of another GameObject
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsDescendantOf(GameObject other)
        {
            var current = this.Parent;
            while (current != null)
            {
                if (current == other) return true;
                current = current.Parent;
            }
            return false;
        }

        /// <summary>
        /// Deserialize a game object
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static GameObjectDefinition Deserialize(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
                ReferenceHandler = ReferenceHandler.Preserve
            };
            options.Converters.Add(new ComponentListJsonConverter());
            options.Converters.Add(new Vector2JsonConverter());
            options.Converters.Add(new ColorJsonConverter());

            GameObjectDefinition gameObject = new GameObjectDefinition();
            var gameObjectData = JsonSerializer.Deserialize<GameObjectDefinition>(json, options);

            if (gameObjectData != null)
            {
                gameObject = gameObjectData;
            }

            return gameObject;
        }
    }
} 
