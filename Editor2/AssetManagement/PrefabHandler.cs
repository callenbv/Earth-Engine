using EarthEngineEditor;
using Engine.Core;
using Engine.Core.CustomMath;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Graphics;
using Engine.Core.Rooms;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Screens;
using MonoGame.Extended.Serialization.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Editor.AssetManagement
{
    public class PrefabHandler : IAssetHandler
    {
        private GameObjectDefinition? _prefab;
        public static string filter = string.Empty;

        public void Load(string path)
        {
            string json = File.ReadAllText(path);
            _prefab = GameObject.Deserialize(json);
        }

        public void Open(string path)
        {

        }

        public void Save(string path)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new ComponentListJsonConverter() },
                WriteIndented = true,
                IncludeFields = true,
                ReferenceHandler = ReferenceHandler.Preserve
            };
            options.Converters.Add(new Vector2JsonConverter());
            options.Converters.Add(new ColorJsonConverter());

            string json = JsonSerializer.Serialize<GameObjectDefinition>(_prefab, options);
            File.WriteAllText(path, json);
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
                        DrawField(field.Name, value, field.FieldType, newValue => field.SetValue(comp, newValue),field);
                    }

                    // Draw properties (optional)
                    foreach (var prop in properties)
                    {
                        if (!prop.CanRead || !prop.CanWrite) continue;
                        var value = prop.GetValue(comp);

                        DrawField(prop.Name, value, prop.PropertyType, newValue => prop.SetValue(comp, newValue),prop);
                    }

                    ImGui.TreePop();
                }
            }

            // Draws the button to add a new component
            DrawEditableButtons(_prefab);
        }

        public static void DrawEditableButtons(IComponentContainer prefab)
        {
            // Allow for adding new component
            if (ImGui.Button("Add Component"))
            {
                ImGui.OpenPopup("AddComponentPopup");
            }

            if (ImGui.BeginPopup("AddComponentPopup"))
            {
                // Input filter
                ImGui.InputText("Filter", ref filter, 64);

                string currentCategory = null;

                foreach (var group in ComponentRegistry.Components.Values
                             .Where(c => string.IsNullOrEmpty(filter) || c.Name.ToLower().Contains(filter.ToLower()))
                             .GroupBy(c => c.Category)
                             .OrderBy(g => g.Key))
                {
                    if (ImGui.BeginMenu(group.Key))
                    {
                        foreach (var comp in group.OrderBy(c => c.Name))
                        {
                            if (ImGui.MenuItem(comp.Name))
                            {
                                var instance = (ObjectComponent)Activator.CreateInstance(comp.Type);
                                prefab.components.Add(instance);

                                // If actual game object, set owner
                                if (prefab is GameObject gameObject)
                                    instance.Owner = gameObject;

                                instance.Initialize();

                                ImGui.CloseCurrentPopup();
                            }
                        }
                        ImGui.EndMenu();
                    }
                }

                ImGui.EndPopup();
            }
        }

        public static void DrawField(string name, object value, Type expectedType, Action<object> setValue, MemberInfo memberInfo)
        {
            // Check for custom attribute tag
            if (memberInfo.GetCustomAttribute<HideInInspectorAttribute>() != null)
                return;

            // Start two-column layout
            ImGui.Columns(2, null, false);

            // Measure label width and pad it slightly
            float labelWidth = ImGui.CalcTextSize(name).X + ImGui.GetStyle().ItemSpacing.X * 2;
            ImGui.SetColumnWidth(0, labelWidth);

            // Draw label, vertically aligned
            ImGui.AlignTextToFramePadding();
            if (!string.IsNullOrEmpty(name))
            {
                name = char.ToUpper(name[0]) + name.Substring(1);
            }
            ImGui.Text(name);
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
            else if (value is Microsoft.Xna.Framework.Vector2 v)
            {
                System.Numerics.Vector2 input = v.ToNumerics();
                if (ImGui.InputFloat2($"##{name}", ref input))
                {
                    setValue(input.ToXna());
                }
            }
            else if (value is Microsoft.Xna.Framework.Color color)
            {
                System.Numerics.Vector4 colorVec = new(
                    color.R / 255f,
                    color.G / 255f,
                    color.B / 255f,
                    color.A / 255f
                );

                if (ImGui.ColorEdit4($"##{name}", ref colorVec))
                {
                    var newColor = new Microsoft.Xna.Framework.Color(
                        (byte)(colorVec.X * 255),
                        (byte)(colorVec.Y * 255),
                        (byte)(colorVec.Z * 255),
                        (byte)(colorVec.W * 255)
                    );

                    setValue(newColor);
                }
            }
            else if (expectedType == typeof(Texture2D))
            {
                string selectedName = value is Texture2D tex && tex.Name != null ? tex.Name : "(None)";
                if (ImGui.BeginCombo($"##{name}", selectedName))
                {
                    if (ImGui.Selectable("(None)", value == null))
                        setValue(null);

                    foreach (var kv in TextureLibrary.Instance.textures)
                    {
                        string texName = kv.Key;
                        Texture2D texValue = kv.Value;

                        if (ImGui.Selectable(texName, value == texValue))
                        {
                            texValue.Name = texName;
                            setValue(texValue);
                        }
                    }

                    ImGui.EndCombo();
                }
            }
            else
            {
                // ImGui.Text($"(unsupported type: {value.GetType().Name})");
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
