/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ComponentReference.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Handles serialization and deserialization of Component references                
/// -----------------------------------------------------------------------------

using Engine.Core.Game.Components;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Core;

namespace Engine.Core.Game.Components
{
    /// <summary>
    /// Custom JSON converter for serializing and deserializing Component references by their ID.
    /// </summary>
    public class ComponentReferenceConverter : JsonConverter<IComponent>
    {
        public override IComponent? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            int id = reader.GetInt32();

            var scene = EngineContext.Current.Scene;
            if (scene == null) return null;

            // Search through all GameObjects and their components to find the component with this ID
            foreach (var gameObject in scene.objects)
            {
                foreach (var component in gameObject.components)
                {
                    if (component is ObjectComponent objComp && objComp.ID == id)
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, IComponent value, JsonSerializerOptions options)
        {
            if (value is ObjectComponent objComp)
            {
                writer.WriteNumberValue(objComp.ID);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
