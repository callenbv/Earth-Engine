using Engine.Core.Game.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Editor.AssetManagement
{
    public class ComponentJsonConverter : JsonConverter<IComponent>
    {
        public override IComponent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            if (!doc.RootElement.TryGetProperty("type", out var typeProp))
                throw new JsonException("Component missing 'type' field");

            string typeName = typeProp.GetString();
            if (!ComponentRegistry.Types.TryGetValue(typeName, out var concreteType))
                throw new JsonException($"Unknown component type: {typeName}");

            string json = doc.RootElement.GetRawText();
            return (IComponent)JsonSerializer.Deserialize(json, concreteType, options);
        }

        public override void Write(Utf8JsonWriter writer, IComponent value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }

}
