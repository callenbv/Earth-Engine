using Engine.Core.Data;
using Engine.Core.Scripting;
using GameRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void RefreshAll()
        {
            _components.Clear();
            RegisterAllComponents();
        }
    }
}
