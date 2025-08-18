/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ComponentRegistry.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using System.Reflection;

namespace Engine.Core.Game.Components
{
    public class ComponentInfo
    {
        public string Name;
        public Type Type;
        public string Category;
    }

    public static class ComponentRegistry
    {
        private static readonly Dictionary<string, ComponentInfo> _components = new();

        public static IReadOnlyDictionary<string, ComponentInfo> Components => _components;

        static ComponentRegistry()
        {
            RegisterAllComponents();
        }

        /// <summary>
        /// Register a new component
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public static void Register(string name, Type type)
        {
            var attr = type.GetCustomAttribute<ComponentCategoryAttribute>();
            string category;

            if (attr != null)
            {
                category = attr.Category;
            }
            else if (type.Assembly.GetName().Name == "CompiledScripts")
            {
                category = "Scripts"; // Auto-assign to Scripts category
            }
            else
            {
                // Skip uncategorized non-script components
                return;
            }

            _components[name] = new ComponentInfo
            {
                Name = name,
                Type = type,
                Category = category
            };
        }

        /// <summary>
        /// Clear the scripts only from the registry
        /// </summary>
        public static void ClearScriptComponentsOnly()
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in _components)
            {
                var asm = kvp.Value.Type.Assembly;
                if (asm.GetName().Name == "CompiledScripts")
                    keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove)
                _components.Remove(key);
        }

        /// <summary>
        /// Register all components (scripts as components, assembly classes)
        /// </summary>
        public static void RegisterAllComponents()
        {
            ClearScriptComponentsOnly();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    if (typeof(ObjectComponent).IsAssignableFrom(type) && type != typeof(GameScript))
                    {
                        Register(type.Name, type);
                    }
                }
            }
        }

        /// <summary>
        /// Refresh registry upon compilation
        /// </summary>
        public static void RefreshAll()
        {
            _components.Clear();
            RegisterAllComponents();
        }
    }
}

