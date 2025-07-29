/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Deserialization.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Game.Components;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Engine.Core.Game;
using Engine.Core;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Custom JSON converter for serializing and deserializing lists of IComponent.
    /// </summary>
    public class ComponentListJsonConverter : JsonConverter<List<IComponent>>
    {
        /// <summary>
        /// Reads a JSON array and converts it to a list of IComponent.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="typeToConvert"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public override List<IComponent> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var components = new List<IComponent>();

            using var doc = JsonDocument.ParseValue(ref reader);
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (!element.TryGetProperty("type", out var typeProp))
                    Console.WriteLine("Component missing 'type' field");

                var typeName = typeProp.GetString();
                if (!ComponentRegistry.Components.TryGetValue(typeName, out var info))
                {
                    Console.WriteLine($"Unknown component type: {typeName}");
                    continue;
                }
                var concreteType = info.Type;

                var json = element.GetRawText();
                var component = Activator.CreateInstance(concreteType);
                var fields = concreteType.GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (element.TryGetProperty(field.Name, out var fieldElement))
                    {
                        try
                        {
                            object? value = null;

                            if (field.FieldType == typeof(GameObject))
                            {
                                var id = Guid.Parse(fieldElement.GetString() ?? string.Empty);
                                value = EngineContext.Current.Scene?.objects.FirstOrDefault(o => o.ID == id);
                            }
                            else
                            {
                                value = JsonSerializer.Deserialize(fieldElement.GetRawText(), field.FieldType, options);
                            }

                            field.SetValue(component, value);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Deserializer] Failed to load {field.Name} on {concreteType.Name}: {ex.Message}");
                        }
                    }
                }

                if (component is IComponent comp)
                {
                    components.Add(comp);
                }
            }

            return components;
        }

        /// <summary>
        /// Writes a list of IComponent to JSON.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, List<IComponent> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var component in value)
            {
                writer.WriteStartObject();
                writer.WriteString("type", component.GetType().Name);

                Type type = component.GetType();

                // Serialize public instance fields
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    writer.WritePropertyName(field.Name);
                    var fieldValue = field.GetValue(component);

                    if (fieldValue is GameObject go)
                        writer.WriteStringValue(go.ID.ToString());
                    else
                        JsonSerializer.Serialize(writer, fieldValue, field.FieldType, options);
                }

                // Serialize public instance properties with getters & setters
                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead || !prop.CanWrite) continue;
                    if (prop.GetIndexParameters().Length > 0) continue; // skip indexers
                    if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;

                    writer.WritePropertyName(prop.Name);
                    var propValue = prop.GetValue(component);

                    if (propValue is GameObject go)
                        writer.WriteStringValue(go.ID.ToString());
                    else
                        JsonSerializer.Serialize(writer, propValue, prop.PropertyType, options);
                }

                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        /// <summary>
        /// Checks if the converter can convert the specified type.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="props"></param>
        public static void ApplyComponentProperties(object component, JsonElement props)
        {
            var type = component.GetType();

            foreach (var prop in props.EnumerateObject())
            {
                var field = type.GetField(prop.Name, BindingFlags.Public | BindingFlags.Instance);
                if (field == null || field.IsInitOnly) continue;

                try
                {
                    var value = JsonSerializer.Deserialize(prop.Value.GetRawText(), field.FieldType);
                    field.SetValue(component, value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Deserializer] Failed to set {type.Name}.{prop.Name}: {ex.Message}");
                }
            }
        }
    }
}

