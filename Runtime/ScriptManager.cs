using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Engine.Core;
using Engine.Core.Game;
using Microsoft.Xna.Framework.Content;
using Engine.Core.Game.Components;
using System.Text.Json;

namespace GameRuntime
{
    public class ScriptManager
    {
        private List<object> _scriptInstances = new List<object>();
        private string _scriptsDirectory;
        private Assembly _scriptAssembly;
        private DateTime _lastAssemblyWriteTime;
        public static ScriptManager Instance { get; private set; }

        /// <summary>
        /// Load the script assemblies 
        /// </summary>
        public ScriptManager()
        {
            Instance = this;
            _scriptsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
            LoadScriptAssembly();
        }

        /// <summary>
        /// Load the script assembly from dll
        /// </summary>
        private void LoadScriptAssembly()
        {
            var dllPath = Path.Combine(_scriptsDirectory, "GameScripts.dll");
            if (File.Exists(dllPath))
            {
                try
                {
                    var lastWrite = File.GetLastWriteTime(dllPath);
                    
                    // Check if assembly has changed (for hot reloading)
                    if (_scriptAssembly != null && lastWrite <= _lastAssemblyWriteTime)
                    {
                        return; // No change, keep existing assembly
                    }
                    
                    Console.WriteLine($"[ScriptManager] Loading DLL: {dllPath} (LastWrite: {lastWrite})");
                    _scriptAssembly = Assembly.LoadFrom(dllPath);
                    _lastAssemblyWriteTime = lastWrite;
                    
                    var types = _scriptAssembly.GetTypes();
                    Console.WriteLine($"[ScriptManager] Types in DLL:");
                    foreach (var t in types)
                        Console.WriteLine($"  - {t.FullName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load script assembly: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Script DLL not found: {dllPath}");
            }
        }

        public void LoadScripts()
        {
            _scriptInstances.Clear();
            LoadScriptAssembly(); // Check for changes and reload if needed
        }
        
        public void CheckForHotReload()
        {
            LoadScriptAssembly(); // This will only reload if the DLL has changed
        }

        public object CreateScriptInstance(string scriptPath)
        {
            // Not used in new system
            return null;
        }

        /// <summary>
        /// Create a script instance given a name
        /// </summary>
        /// <param name="scriptName"></param>
        /// <returns></returns>
        public GameScript CreateScriptInstanceByName(string scriptName)
        {
            // Create script instance by name (for room objects)
            try
            {
                if (_scriptAssembly == null)
                {
                    Console.WriteLine("Script assembly not loaded.");
                    return null;
                }
                // Remove .cs extension if present
                var typeName = scriptName.EndsWith(".cs") ? scriptName.Substring(0, scriptName.Length - 3) : scriptName;
                Console.WriteLine($"[ScriptManager] Attempting to instantiate script: {typeName}");
                // Search the loaded script assembly for the type
                var scriptType = _scriptAssembly.GetTypes()
                    .FirstOrDefault(t => t.Name == typeName && typeof(Engine.Core.GameScript).IsAssignableFrom(t));
                if (scriptType != null)
                {
                    Console.WriteLine($"[ScriptManager] Instantiating: {scriptType.FullName}");
                    return Activator.CreateInstance(scriptType) as Engine.Core.GameScript;
                }
                else
                {
                    Console.WriteLine($"[ScriptManager] Script type not found in DLL: {typeName}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating script instance: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Update the script by invoking it
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            foreach (var script in _scriptInstances)
            {
                try
                {
                    var updateMethod = script.GetType().GetMethod("Update");
                    updateMethod?.Invoke(script, new object[] { gameTime });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating script: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Draw the script if possible
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var script in _scriptInstances)
            {
                try
                {
                    var drawMethod = script.GetType().GetMethod("Draw");
                    drawMethod?.Invoke(script, new object[] { spriteBatch });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error drawing script: {ex.Message}");
                }
            }
        }
    }
} 