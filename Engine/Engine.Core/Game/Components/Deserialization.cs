using Engine.Core.Game.Components;
using Microsoft.Xna.Framework;
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
                    Console.WriteLine("Component missing 'type' field");

                var typeName = typeProp.GetString();
                if (!ComponentRegistry.Types.TryGetValue(typeName, out var concreteType))
                    Console.WriteLine($"Unknown component type: {typeName}");

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

    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            float x = 0, y = 0;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Vector2(x, y);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read();
                    switch (propertyName)
                    {
                        case "X":
                        case "x":
                            x = reader.GetSingle();
                            break;
                        case "Y":
                        case "y":
                            y = reader.GetSingle();
                            break;
                    }
                }
            }

            throw new JsonException("Unexpected end of Vector2");
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteEndObject();
        }
    }
}
