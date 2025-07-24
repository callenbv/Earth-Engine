/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         GameObject.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Engine.Core.Game.Components;
using System.Security.AccessControl;
using Editor.AssetManagement;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Core.Data;
using System.Reflection;
using MonoGame.Extended.Serialization.Json;

namespace Engine.Core.Game
{
    public class GameObject : IComponentContainer
    {
        public string Name { get; set; } = string.Empty;
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
        public float scale = 1f;
        public float rotation;
        public List<GameObject> children = new List<GameObject>();

        [JsonConverter(typeof(ComponentListJsonConverter))]
        public List<IComponent> components { get; set; } = new();

        public bool IsDestroyed { get; private set; } = false;

        [JsonIgnore]
        [HideInInspector]
        public Sprite2D? Sprite
        {
            get => GetComponent<Sprite2D>();
        }

        public GameObject()
        {
            OnCreate();
        }
        public GameObject(string name)
        {
            Name = name;
            OnCreate();
        }

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
            component.Create();
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
            component.Create();
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
        public void AddComponent(ObjectComponent component)
        {
            component.Owner = this;
            components.Add(component);
            component.Create();
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
                    // Do not update scripts if the engine is paused
                    if (EngineContext.Paused && component is GameScript)
                    {
                        continue;
                    }

                    component.Update(gameTime);
                }
                catch (Exception e) 
                {
                    Console.WriteLine($"Error updating component for {Name}: {e.Message}");
                }
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

            return JsonSerializer.Deserialize<GameObjectDefinition>(json, options);
        }

        /// <summary>
        /// Calculate the depth value based on the object's feet position (bottom of sprite)
        /// </summary>
        /// <param name="gameObj">The GameObject to calculate depth for</param>
        /// <returns>Depth value (0 = front, 1 = back)</returns>
        public float GetDepth()
        {
            if (Sprite == null || Sprite.texture == null)
                return 0f;

            // Calculate the bottom Y position (feet) of the sprite
            float feetY = Position.Y + Sprite.frameHeight / 2;
            feetY /= 1000f;

            feetY = Math.Clamp(feetY, 0f, 1f);

            // Convert to depth value (0 = front, 1 = back)
            // You can adjust this calculation based on your game's coordinate system
            // For a typical top-down view, higher Y values should have higher depth (appear behind)
            return feetY; // Normalize to 0-1 range, adjust divisor as needed
        }
    }
} 
