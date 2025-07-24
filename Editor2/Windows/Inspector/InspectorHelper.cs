using Editor.AssetManagement;
using Engine.Core.Game.Components;
using Engine.Core.Game;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Engine.Core.Data;
using EarthEngineEditor;

namespace Editor.Windows.Inspector
{
    public static class InspectorUI
    {
        /// <summary>
        /// Draw a component in the inspector
        /// </summary>
        /// <param name="comp"></param>
        public static void DrawComponent(IComponent comp)
        {
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
                bool open = ImGui.TreeNodeEx(comp.Name);
                if (open)
                {
                    DrawComponent(comp);
                    ImGui.TreePop();

                    if (ImGuiRenderer.IconButton("Remove", "\uf1f8", Microsoft.Xna.Framework.Color.Red))
                    {
                        ((ObjectComponent)comp).Owner.components.Remove(comp);
                        break;
                    }
                }
            }
            PrefabHandler.DrawEditableButtons(obj);
        }
    }

}
