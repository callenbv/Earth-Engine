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

namespace Editor.Windows.Inspector
{
    public static class InspectorUI
    {
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

        public static void DrawGameObject(IComponentContainer obj)
        {
            foreach (var comp in obj.components)
            {
                bool open = ImGui.TreeNodeEx(comp.Name);
                if (open)
                {
                    DrawComponent(comp);
                    ImGui.TreePop();
                }
            }
            PrefabHandler.DrawEditableButtons(obj);
        }
    }

}
