
namespace Engine.Core.Game
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class GameObjectReferenceConverter : JsonConverter<GameObject>
    {
        public override GameObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Guid id = Guid.Parse(reader.GetString()!);

            var scene = EngineContext.Current.Scene;
            if (scene == null) return null;

            return scene.objects.FirstOrDefault(o => o.ID == id);
        }

        public override void Write(Utf8JsonWriter writer, GameObject value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ID.ToString());
        }
    }

}
