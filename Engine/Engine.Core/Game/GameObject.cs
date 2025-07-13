using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Engine.Core.Game.Components;
using System.ComponentModel;
using System.Security.AccessControl;

namespace Engine.Core.Game
{
    public class GameObject
    {
        public string? Name;
        public SpriteData? sprite;
        public Vector2 position;
        public float scale = 1f;
        public float rotation;
        public List<object> scriptInstances = new List<object>();
        public List<ObjectComponent> components = new List<ObjectComponent>();
        public Dictionary<string, Dictionary<string, object>> scriptProperties { get; set; } = new();

        public bool IsDestroyed { get; private set; } = false;
               
        public GameObject()
        {

        }
        public GameObject(string name)
        {
            Name = name;
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
            
            // Call Destroy on all attached scripts
            foreach (var script in scriptInstances)
            {
                if (script is GameScript gs)
                {
                    try
                    {
                        gs.Destroy();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error calling Destroy on script for {Name}: {ex.Message}");
                    }
                }
            }
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
                    component.Update(gameTime);
                }
                catch (Exception e) 
                {
                    Console.WriteLine($"Error updating component for {Name}: {e.Message}");
                }
            }

            if (sprite != null)
            {
                sprite.Update(gameTime);
            }
        }

        /// <summary>
        /// Draw the object and its attached scripts
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw the sprite if available
            if (sprite != null)
            {
                sprite.Draw(spriteBatch,position,rotation,scale);
            }

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
            // Try to instantiate the object
            try
            {
                var objDef = GameObjectRegistry.Get(defName);
                var go = new GameObject(objDef.Name);
                go.position = position;

                // Load default script properties
                if (objDef?.Scripts != null)
                {
                    foreach (var script in objDef.Scripts)
                    {
                        if (objDef.scriptProperties != null)
                        {
                            foreach (var kvp in objDef.scriptProperties)
                                go.scriptProperties[kvp.Key] = new Dictionary<string, object>(kvp.Value);
                        }
                    }
                }

                var assetsRoot = EngineContext.Current.AssetsRoot;
                var contentManager = EngineContext.Current.ContentManager;
                var scriptManager = EngineContext.Current.ScriptManager;
                var graphicsDevice = EngineContext.Current.GraphicsDevice;
                ScriptCompiler.LoadTextureAndScripts(go, defName, assetsRoot, contentManager, scriptManager, graphicsDevice);

                GameObjectManager.Main.gameObjects.Add(go);

                return go;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
            }

            // If failed, make an empty game object
            return new GameObject();
        }
    }
} 