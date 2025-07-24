/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ScriptManager.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Linq;
using Engine.Core.Game.Components;

namespace GameRuntime
{
    public class ScriptManager
    {
        private readonly Assembly _scriptAssembly;

        public ScriptManager(Assembly scriptAssembly)
        {
            _scriptAssembly = scriptAssembly;
        }

        public ObjectComponent? CreateComponentInstanceByName(string typeName)
        {
            // Full type scan (or optimize with caching later)
            var type = _scriptAssembly.GetTypes().FirstOrDefault(t =>
                t.Name == typeName && typeof(ObjectComponent).IsAssignableFrom(t));

            if (type == null)
            {
                Console.WriteLine($"[ScriptManager] Type '{typeName}' not found.");
                return null;
            }

            try
            {
                return Activator.CreateInstance(type) as ObjectComponent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScriptManager] Failed to create '{typeName}': {ex.Message}");
                return null;
            }
        }
    }

}
