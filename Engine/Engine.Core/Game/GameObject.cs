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

namespace Engine.Core.Game
{
    public class GameObject
    {
        public string Name { get; set; } = string.Empty;
        public SpriteData? sprite;
        public Vector2 position;
        public float scale = 1f;
        public float rotation;
        public List<object> scriptInstances = new List<object>();
        public List<GameObject> children = new List<GameObject>();

        [JsonConverter(typeof(ComponentListJsonConverter))]
        public List<IComponent> components { get; set; } = new();

        public bool IsDestroyed { get; private set; } = false;
               
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
            AddComponent<Transform>();
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
            return new GameObject("Empty");
        }

        /// <summary>
        /// Deserialize a game object
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static GameObject Deserialize(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            };
            options.Converters.Add(new ComponentListJsonConverter());

            return JsonSerializer.Deserialize<GameObject>(json, options);
        }
    }
} 