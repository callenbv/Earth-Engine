using Engine.Core.Game.Components;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                    throw new JsonException("Component missing 'type' field");

                var typeName = typeProp.GetString();
                if (!ComponentRegistry.Types.TryGetValue(typeName, out var concreteType))
                    throw new JsonException($"Unknown component type: {typeName}");

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
    }

}
