using Engine.Core.Game;
using Engine.Core.Game.Components;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Editor.AssetManagement
{
    public class PrefabHandler : IAssetHandler
    {
        private GameObject? _prefab;
        private string filter = string.Empty;

        public void Load(string path)
        {
            string json = File.ReadAllText(path);
            _prefab = GameObject.Deserialize(json);
        }

        public void Open()
        {

        }

        public void Render()
        {
            if (_prefab == null) return;

            // Show each component
            foreach (var comp in _prefab.components)
            {
                bool selected = ImGui.TreeNodeEx(comp.Name);

                if (selected)
                {
                    var type = comp.GetType();
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    // Draw fields
                    foreach (var field in fields)
                    {
                        var value = field.GetValue(comp);
                        DrawField(field.Name, value, newValue => field.SetValue(comp, newValue));
                    }

                    // Draw properties (optional)
                    foreach (var prop in properties)
                    {
                        if (!prop.CanRead || !prop.CanWrite) continue;
                        var value = prop.GetValue(comp);
                        DrawField(prop.Name, value, newValue => prop.SetValue(comp, newValue));
                    }

                    ImGui.TreePop();
                }
            }

            // Allow for adding new component
            if (ImGui.Button("Add Component"))
            {
                ImGui.OpenPopup("AddComponentPopup");
            }

            if (ImGui.BeginPopup("AddComponentPopup"))
            {
                // Input filter
                ImGui.InputText("Filter", ref filter, 64);

                // Iterate all registered components
                foreach (var kvp in ComponentRegistry.Types)
                {
                    string name = kvp.Key;

                    // Skip if doesn't match filter
                    if (!string.IsNullOrEmpty(filter) && !name.ToLower().Contains(filter.ToLower()))
                        continue;

                    // Show selectable item
                    if (ImGui.Selectable(name))
                    {
                        // Add the component
                        var component = (ObjectComponent)Activator.CreateInstance(kvp.Value);
                        _prefab.AddComponent(component); // Adjust to your method
                        ImGui.CloseCurrentPopup();
                    }
                }

                ImGui.EndPopup();
            }
        }

        void DrawField(string name, object value, Action<object> setValue)
        {
            if (value == null) return;

            // Start two-column layout
            ImGui.Columns(2, null, false);

            // Measure label width and pad it slightly
            float labelWidth = ImGui.CalcTextSize(name).X + ImGui.GetStyle().ItemSpacing.X * 2;
            ImGui.SetColumnWidth(0, labelWidth);

            // Draw label, vertically aligned
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(name);
            ImGui.NextColumn();

            // Set input field to fill remaining width
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);

            if (value is int i)
            {
                int v = i;
                if (ImGui.InputInt($"##{name}", ref v))
                    setValue(v);
            }
            else if (value is float f)
            {
                float v = f;
                if (ImGui.InputFloat($"##{name}", ref v))
                    setValue(v);
            }
            else if (value is bool b)
            {
                bool v = b;
                if (ImGui.Checkbox($"##{name}", ref v))
                    setValue(v);
            }
            else if (value is string s)
            {
                byte[] buffer = new byte[256];
                Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
                if (ImGui.InputText($"##{name}", buffer, (uint)buffer.Length))
                    setValue(Encoding.UTF8.GetString(buffer).TrimEnd('\0'));
            }
            else
            {
                ImGui.Text($"(unsupported type: {value.GetType().Name})");
            }

            ImGui.PopItemWidth();
            ImGui.NextColumn(); // Move to next row
            ImGui.Columns(1);
        }

        public void Unload()
        {
            _prefab = null;
        }
    }
}
