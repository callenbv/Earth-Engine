using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace GameRuntime
{
    public class ScriptManager
    {
        private List<object> _scriptInstances = new List<object>();
        private string _scriptsDirectory;
        private Assembly _scriptAssembly;

        public ScriptManager()
        {
            _scriptsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
            LoadScriptAssembly();
        }

        private void LoadScriptAssembly()
        {
            var dllPath = Path.Combine(_scriptsDirectory, "GameScripts.dll");
            if (File.Exists(dllPath))
            {
                try
                {
                    var lastWrite = File.GetLastWriteTime(dllPath);
                    Console.WriteLine($"[ScriptManager] Loading DLL: {dllPath} (LastWrite: {lastWrite})");
                    _scriptAssembly = Assembly.LoadFrom(dllPath);
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
            LoadScriptAssembly(); // Always reload the assembly in case it changed
        }

        public object CreateScriptInstance(string scriptPath)
        {
            // Not used in new system
            return null;
        }

        public Engine.Core.GameScript CreateScriptInstanceByName(string scriptName)
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

    // Default script class for demonstration
    public class DefaultGameScript
    {
        public void Update(GameTime gameTime)
        {
            // Default update logic
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Default drawing logic
        }
    }
} 