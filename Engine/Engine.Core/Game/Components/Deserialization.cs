/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Deserialization.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;
using System.Text.Json;
using System.Text.Json.Serialization;
using MonoGame.Extended.Serialization.Json;
using System.Reflection;

namespace Editor.AssetManagement
{
    public class ComponentListJsonConverter : JsonConverter<List<IComponent>>
    {
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
                var component = (IComponent)JsonSerializer.Deserialize(json, concreteType, options);
                components.Add(component);
            }

            return components;
        }

        public override void Write(Utf8JsonWriter writer, List<IComponent> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var component in value)
            {
                JsonSerializer.Serialize(writer, component, component.GetType(), options);
            }
            writer.WriteEndArray();
        }

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

