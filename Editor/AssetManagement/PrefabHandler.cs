/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         PrefabHandler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor;
using EarthEngineEditor.Windows;
using Engine.Core.CustomMath;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Graphics;
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Serialization.Json;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Handles loading, saving, and rendering prefabs in the editor.
    /// </summary>
    public class PrefabHandler : IAssetHandler
    {
        private GameObjectDefinition? _prefab;
        public static string filter = string.Empty;
        private static string newStringItem = string.Empty;

        /// <summary>
        /// Loads a prefab from a JSON file.
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            string json = File.ReadAllText(path);
            _prefab = GameObject.Deserialize(json);

            foreach (ObjectComponent comp in _prefab.components)
            {
                comp.Initialize();
            }
        }

        /// <summary>
        /// Saves the current prefab to a JSON file.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            // Do not save null prefab
            if (_prefab == null)
            {
                Console.WriteLine("No prefab loaded to save.");
                return;
            }

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

        /// <summary>
        /// Renders the prefab's components in the editor UI.
        /// </summary>
        public void Render()
        {
            if (_prefab == null) return;

            // Show each component
            foreach (var comp in _prefab.components)
            {
                bool selected = ImGui.TreeNodeEx($"{comp.Name}##{comp.GetID()}");

                if (selected)
                {
                    var type = comp.GetType();
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    // Draw fields
                    foreach (var field in fields)
                    {
                        var value = field.GetValue(comp);
                        DrawField(field.Name, value, field.FieldType, newValue => field.SetValue(comp, newValue),field, _prefab);
                    }

                    // Draw properties (optional)
                    foreach (var prop in properties)
                    {
                        if (!prop.CanRead || !prop.CanWrite) continue;
                        var value = prop.GetValue(comp);

                        DrawField(prop.Name, value, prop.PropertyType, newValue => prop.SetValue(comp, newValue),prop, _prefab);
                    }

                    ImGui.TreePop();

                    if (ImGuiRenderer.IconButton("Remove", "\uf1f8", Microsoft.Xna.Framework.Color.Red))
                    {
                        if (comp is ObjectComponent objectComponent)
                        {
                            _prefab.components.Remove(objectComponent);
                            break;
                        }
                    }
                }
            }

            // Draws the button to add a new component
            DrawEditableButtons(_prefab);
        }

        /// <summary>
        /// Draws the button to add a new component to the prefab.
        /// </summary>
        /// <param name="prefab"></param>
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
                                var instance = Activator.CreateInstance(comp.Type) as ObjectComponent;
                                prefab.AddComponent(instance);
                                ImGui.CloseCurrentPopup();
                            }
                        }
                        ImGui.EndMenu();
                    }
                }

                ImGui.EndPopup();
            }
        }

        /// <summary>
        /// Draws a field in the inspector with appropriate input controls based on the value type.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="expectedType"></param>
        /// <param name="setValue"></param>
        /// <param name="memberInfo"></param>
        public static void DrawField(string name, object? value, Type expectedType, Action<object> setValue, MemberInfo memberInfo, IComponentContainer? componentContainer = null)
        {
            // Check for custom attribute tag
            if (memberInfo.GetCustomAttribute<HideInInspectorAttribute>() != null)
                return;

            var sliderAttr = memberInfo.GetCustomAttribute<SliderEditorAttribute>();

            // Start two-column layout
            ImGui.Columns(2, null, false);

            // Measure label width and pad it slightly
            float minLabelWidth = 140f;
            float labelWidth = Math.Max(minLabelWidth, ImGui.CalcTextSize(name).X + 20f);
            ImGui.SetColumnWidth(0, labelWidth);

            // Draw label, vertically aligned
            ImGui.AlignTextToFramePadding();
            if (!string.IsNullOrEmpty(name))
            {
                name = char.ToUpper(name[0]) + name.Substring(1);
            }
            ImGui.Text(name);

            // Use the parsed XML comments as an ImGui tooltip
            // Note: I really like this
            if (ImGui.IsItemHovered())
            {
                string memberKey = Comments.GetXmlDocMemberKey(memberInfo);

                if (memberKey != null && Comments.propertyTooltips.TryGetValue(memberKey, out string tooltip))
                {
                    ImGui.SetTooltip(tooltip);
                }
            }

            ImGui.NextColumn();

            // Set input field to fill remaining width
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);

            if (value is int i)
            {
                int v = i;
                if (sliderAttr != null)
                {
                    if (ImGui.SliderInt($"##{name}", ref v, (int)sliderAttr.Min, (int)sliderAttr.Max))
                        setValue(v);
                }
                else
                {
                    if (ImGui.InputInt($"##{name}", ref v))
                        setValue(v);
                }
            }
            else if (value is float f)
            {
                float v = f;
                if (sliderAttr != null)
                {
                    if (ImGui.SliderFloat($"##{name}", ref v, sliderAttr.Min, sliderAttr.Max))
                        setValue(v);
                }
                else
                {
                    if (ImGui.InputFloat($"##{name}", ref v))
                        setValue(v);
                }
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

                if (sliderAttr != null)
                {
                    if (ImGui.SliderFloat2($"##{name}", ref input, 0f, 20f))
                    {
                        setValue(input.ToXna());
                    }
                }
                else
                {
                    if (ImGui.InputFloat2($"##{name}", ref input))
                    {
                        setValue(input.ToXna());
                    }
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

                if (value != null)
                {
                    Texture2D imTex = (Texture2D)value;
                    float maxSize = 128f;
                    Vector2 originalSize = new(imTex.Width * 4, imTex.Height * 4);
                    Vector2 targetSize = originalSize;

                    // Scale down if larger than maxSize
                    float scale = 1f;

                    IntPtr imImage = ImGuiRenderer.Instance.BindTexture(imTex);
                    Vector2 uv_1 = Vector2.Zero;
                    Vector2 uv_2 = Vector2.One;

                    // If there is an animation display
                    Sprite2D? sprite = componentContainer?.GetComponent<Sprite2D>();

                    if (sprite != null && sprite.texture != null)
                    {
                        sprite.Animate();
                        float ratio = sprite.frameWidth / (float)sprite.texture.Width;
                        float ux = sprite.frame * ratio;
                        uv_1.X = ux;
                        uv_2.X = ux + (ratio);
                        targetSize.X = sprite.frameWidth*4;
                        targetSize.Y = sprite.frameHeight*4;
                        originalSize = targetSize;
                    }

                    if (originalSize.X > maxSize || originalSize.Y > maxSize)
                    {
                        float scaleX = maxSize / originalSize.X;
                        float scaleY = maxSize / originalSize.Y;
                        scale = MathF.Min(scaleX, scaleY);
                        targetSize *= scale;
                    }

                    ImGui.Image(imImage, targetSize, uv_1, uv_2);
                }
            }
            else if (expectedType == typeof(GameObject))
            {
                GameObject obj = (GameObject)value;
                string label = obj != null ? obj.Name : "None";

                if (ImGui.Button(label))
                {
                    ImGui.OpenPopup("SelectGameObject");
                }

                if (ImGui.BeginPopup("SelectGameObject"))
                {
                    foreach (var sceneObj in SceneViewWindow.Instance.scene.objects)
                    {
                        if (ImGui.Selectable(sceneObj.Name))
                        {
                            Console.WriteLine($"Ref is {sceneObj.Name}, instance: {sceneObj.GetHashCode()}");
                            setValue(sceneObj); // update your serialized field
                        }
                    }

                    ImGui.EndPopup();
                }

                if (ImGui.BeginDragDropSource())
                {
                    unsafe
                    {
                        GCHandle handle = GCHandle.Alloc(obj);
                        ImGui.SetDragDropPayload("GAMEOBJECT_REF", (IntPtr)handle, sizeof(uint));
                        ImGui.Text(obj.Name);
                        ImGui.EndDragDropSource();
                    }
                }

                if (ImGui.BeginDragDropTarget())
                {
                    unsafe
                    {
                        var payload = ImGui.AcceptDragDropPayload("GAMEOBJECT_REF");
                        if (payload.NativePtr != null)
                        {
                            var handle = GCHandle.FromIntPtr(payload.Data);
                            value = (GameObject)handle.Target;
                            handle.Free();
                        }
                        ImGui.EndDragDropTarget();
                    }
                }
            }
            else if (typeof(IComponent).IsAssignableFrom(expectedType))
            {
                ObjectComponent comp = (ObjectComponent)value;
                string label = comp != null ? comp.Name : "None";

                if (ImGui.Button(label))
                {
                }
            }
            else if (typeof(List<string>) == expectedType)
            {
                var list = value as List<string>;
                if (list == null)
                {
                    list = new List<string>();
                }

                ImGui.Text("List (String):");
                int indexToRemove = -1;

                for (i = 0; i < list.Count; i++)
                {
                    ImGui.PushID(i);

                    ImGui.Text($"[{i}] {list[i]}");
                    ImGui.SameLine();
                    if (ImGui.Button("Remove"))
                    {
                        indexToRemove = i;
                    }

                    ImGui.PopID();
                }

                if (indexToRemove >= 0)
                {
                    list.RemoveAt(indexToRemove);
                }

                // Add new item
                ImGui.InputText("New Item", ref newStringItem, 128);
                if (ImGui.Button("Add"))
                {
                    if (!string.IsNullOrWhiteSpace(newStringItem))
                    {
                        list.Add(newStringItem);
                        newStringItem = string.Empty;
                    }
                }

                // Optional: assign back if needed
                if (value != list)
                    value = list;
            }
            else if (value != null && value.GetType().IsEnum)
            {
                var enumType = value.GetType();
                var names = Enum.GetNames(enumType);
                var values = Enum.GetValues(enumType);

                int currentIndex = -1;
                i = 0;

                foreach (var enumVal in values)
                {
                    if (Equals(enumVal, value))
                    {
                        currentIndex = i;
                        break;
                    }
                    i++;
                }

                string currentLabel = currentIndex >= 0 ? names[currentIndex] : $"Unknown ({Convert.ToInt32(value)})";

                if (ImGui.BeginCombo($"##{name}", currentLabel))
                {
                    for (int j = 0; j < names.Length; j++)
                    {
                        bool isSelected = j == currentIndex;
                        if (ImGui.Selectable(names[j], isSelected))
                        {
                            setValue(Enum.Parse(enumType, names[j]));
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
    }
}

