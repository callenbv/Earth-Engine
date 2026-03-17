/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         InspectorHelper.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.AssetManagement;
using Engine.Core.Game.Components;
using ImGuiNET;
using System.Reflection;
using Engine.Core.Data;
using EarthEngineEditor;

namespace Editor.Windows.Inspector
{
    /// <summary>
    /// InspectorUI provides methods to draw components and game objects in the inspector window.
    /// </summary>
    public static class InspectorUI
    {
        /// <summary>
        /// Draw a component in the inspector
        /// </summary>
        /// <param name="comp"></param>
        public static void DrawComponent(IComponent comp)
        {
            if (comp == null) return;

            var type = comp.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var value = field.GetValue(comp);
                PrefabHandler.DrawField(field.Name, value, field.FieldType, newValue => field.SetValue(comp, newValue),field);
            }

            foreach (var prop in properties)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                var value = prop.GetValue(comp);
                PrefabHandler.DrawField(prop.Name, value, prop.PropertyType, newValue => prop.SetValue(comp, newValue),prop);
            }
        }

        /// <summary>
        /// Give a GameObject, draw all its components in a tree view
        /// </summary>
        /// <param name="obj"></param>
        public static void DrawGameObject(IComponentContainer obj)
        {
            foreach (var comp in obj.components)
            {
                bool open = ImGui.TreeNodeEx($"{comp.Name}##{comp.GetID()}");

                if (open)
                {
                    DrawComponent(comp);
                    ImGui.TreePop();
                    if (ImGuiRenderer.IconButton("Remove", "\uf1f8", Microsoft.Xna.Framework.Color.Red))
                    {
                        if (comp is ObjectComponent objectComponent)
                        {
                            objectComponent.Owner?.components.Remove(objectComponent);
                            break;
                        }
                    }
                }
            }
            PrefabHandler.DrawEditableButtons(obj);
        }

        /// <summary>
        /// Exposes all fields of a class, any class
        /// </summary>
        /// <param name="comp"></param>
        public static void DrawClass(object comp)
        {
            if (comp == null) return;

            var type = comp.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var value = field.GetValue(comp);
                PrefabHandler.DrawField(field.Name, value, field.FieldType, newValue => field.SetValue(comp, newValue), field);
            }

            foreach (var prop in properties)
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                var value = prop.GetValue(comp);
                PrefabHandler.DrawField(prop.Name, value, prop.PropertyType, newValue => prop.SetValue(comp, newValue), prop);
            }
        }

        /// <summary>
        /// Draw the property field
        /// </summary>
        /// <param name="field"></param>
        public static void DrawField(object target, string memberName)
        {
            if (target == null || string.IsNullOrEmpty(memberName))
                return;

            var type = target.GetType();

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            // Try property first
            var prop = type.GetProperty(memberName, flags);
            if (prop != null && prop.CanRead && prop.CanWrite)
            {
                var value = prop.GetValue(target);

                PrefabHandler.DrawField(
                    prop.Name,
                    value,
                    prop.PropertyType,
                    newValue => prop.SetValue(target, newValue),
                    prop
                );

                return;
            }

            // Try field
            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                var value = field.GetValue(target);

                PrefabHandler.DrawField(
                    field.Name,
                    value,
                    field.FieldType,
                    newValue => field.SetValue(target, newValue),
                    field
                );
            }
        }
    }
}

