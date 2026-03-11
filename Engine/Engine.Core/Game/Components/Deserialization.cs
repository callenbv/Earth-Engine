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
using Engine.Core.Data;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Handles resolving references to Components by their unique IDs during deserialization.
    /// </summary>
    public static class ComponentReferenceResolver
    {
        private static readonly List<(object Target, FieldInfo Field, int ID)> _pending = new();

        /// <summary>
        /// Registers a pending reference to a Component by its ID.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <param name="id"></param>
        public static void Register(object target, FieldInfo field, int id)
        {
            _pending.Add((target, field, id));
            Console.WriteLine($"[ComponentReferenceResolver] Registered pending Component reference: {target.GetType().Name}.{field.Name} -> ID {id}");
        }

        /// <summary>
        /// Resolves all pending Component references by matching IDs with actual Components in the scene.
        /// </summary>
        /// <param name="objects"></param>
        public static void Resolve(List<GameObject> objects)
        {          
            Console.WriteLine($"[ComponentReferenceResolver] Resolving {_pending.Count} pending Component references...");
            
            foreach (var (target, field, id) in _pending)
            {          
                var match = FindComponentById(objects, id);
                
                if (match != null)
                {
                    field.SetValue(target, match);
                    Console.WriteLine($"[ComponentReferenceResolver] ✓ Resolved Component ID {id} for {target.GetType().Name}.{field.Name}");
                }
                else
                {
                    Console.Error.WriteLine($"[ComponentReferenceResolver] ✗ Component ID {id} not found for {target.GetType().Name}.{field.Name}");
                    
                    // Debug: List all available Component IDs
                    var allIds = new List<int>();
                    foreach (var gameObject in objects)
                    {
                        foreach (var component in gameObject.components)
                        {
                            if (component is ObjectComponent objComp)
                            {
                                allIds.Add(objComp.ID);
                            }
                        }
                    }
                    Console.Error.WriteLine($"[ComponentReferenceResolver] Available Component IDs: {string.Join(", ", allIds)}");
                }
            }

            _pending.Clear();
        }

        /// <summary>
        /// Finds a component by its ID in the given list of GameObjects.
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IComponent? FindComponentById(List<GameObject> objects, int id)
        {
            // Search through all GameObjects and their components to find the component with this ID
            foreach (var gameObject in objects)
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
    }

    /// <summary>
    /// Handles resolving references to GameObjects by their unique IDs during deserialization.
    /// </summary>
    public static class GameReferenceResolver
    {
        private static readonly List<(object Target, FieldInfo Field, Guid ID)> _pending = new();

        /// <summary>
        /// Registers a pending reference to a GameObject by its ID.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <param name="id"></param>
        public static void Register(object target, FieldInfo field, Guid id)
        {
            _pending.Add((target, field, id));
        }

        /// <summary>
        /// Resolves all pending GameObject references by matching IDs with actual GameObjects in the scene.
        /// </summary>
        /// <param name="objects"></param>
        public static void Resolve(List<GameObject> objects)
        {          
            foreach (var (target, field, id) in _pending)
            {          
                var match = objects.FirstOrDefault(o => o.ID == id);
                if (match != null)
                {
                    field.SetValue(target, match);
                }
                else
                {
                    Console.Error.WriteLine($"[ReferenceResolver] GameObject ID {id} not found for {target.GetType().Name}.{field.Name}");
                    Console.Error.WriteLine($"[ReferenceResolver] Available IDs: {string.Join(", ", objects.Select(o => o.ID))}");
                }
            }

            _pending.Clear();
        }
    }

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
        public override List<IComponent> Read(
    ref Utf8JsonReader reader,
    Type typeToConvert,
    JsonSerializerOptions options)
        {
            var components = new List<IComponent>();

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected start of components array.");

            // Move into the array
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                // Parse single component object
                using var doc = JsonDocument.ParseValue(ref reader);
                var element = doc.RootElement;

                // ----- Determine component type -----
                if (!element.TryGetProperty("type", out var typeProp))
                {
                    Console.Error.WriteLine("[ComponentListJsonConverter] Component missing 'type'");
                    continue;
                }

                var typeName = typeProp.GetString();
                if (!ComponentRegistry.Components.TryGetValue(typeName, out var info))
                {
                    Console.Error.WriteLine($"[ComponentListJsonConverter] Unknown component type: {typeName}");
                    continue;
                }

                var concreteType = info.Type;
                var componentObj = Activator.CreateInstance(concreteType);

                if (componentObj is not IComponent component)
                    continue;

                // ===== Deserialize fields =====
                foreach (var field in concreteType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!element.TryGetProperty(field.Name, out var fieldElement))
                        continue;

                    try
                    {
                        // GameObject reference
                        if (field.FieldType == typeof(GameObject))
                        {
                            if (Guid.TryParse(fieldElement.GetString(), out Guid id))
                                GameReferenceResolver.Register(componentObj, field, id);
                            continue;
                        }

                        // Component reference
                        if (typeof(IComponent).IsAssignableFrom(field.FieldType))
                        {
                            if (fieldElement.ValueKind == JsonValueKind.Number &&
                                fieldElement.TryGetInt32(out int id))
                            {
                                ComponentReferenceResolver.Register(componentObj, field, id);
                            }
                            continue;
                        }

                        // SceneAsset
                        if (field.FieldType == typeof(SceneAsset))
                        {
                            var val = JsonSerializer.Deserialize<SceneAsset>(
                                fieldElement.GetRawText(),
                                new JsonSerializerOptions { Converters = { new SceneAssetConverter() } }
                            );
                            field.SetValue(componentObj, val);
                            continue;
                        }

                        // IAssignable
                        if (typeof(IAssignable).IsAssignableFrom(field.FieldType))
                        {
                            var val = JsonSerializer.Deserialize<IAssignable>(
                                fieldElement.GetRawText(),
                                new JsonSerializerOptions { Converters = { new AssignableReferenceConverter() } }
                            );
                            field.SetValue(componentObj, val);
                            continue;
                        }

                        // Regular type
                        var fieldValue = JsonSerializer.Deserialize(
                            fieldElement.GetRawText(),
                            field.FieldType,
                            options
                        );
                        field.SetValue(componentObj, fieldValue);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(
                            $"[Deserializer] Failed to load field {field.Name} on {concreteType.Name}: {ex.Message}"
                        );
                    }
                }

                // ===== Deserialize properties =====
                foreach (var prop in concreteType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead || !prop.CanWrite)
                        continue;
                    if (prop.GetIndexParameters().Length > 0)
                        continue;
                    if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;
                    if (!element.TryGetProperty(prop.Name, out var propElement))
                        continue;

                    try
                    {
                        object? value;

                        // GameObject prop
                        if (prop.PropertyType == typeof(GameObject))
                        {
                            if (Guid.TryParse(propElement.GetString(), out Guid id))
                                value = EngineContext.Current.Scene?.objects.FirstOrDefault(o => o.ID == id);
                            else
                                value = null;

                            prop.SetValue(componentObj, value);
                            continue;
                        }

                        // Component prop
                        if (typeof(IComponent).IsAssignableFrom(prop.PropertyType))
                        {
                            if (propElement.ValueKind == JsonValueKind.Number &&
                                propElement.TryGetInt32(out int id))
                            {
                                value = ComponentReferenceResolver.FindComponentById(
                                    EngineContext.Current.Scene?.objects ?? new(),
                                    id
                                );
                            }
                            else value = null;

                            prop.SetValue(componentObj, value);
                            continue;
                        }

                        // SceneAsset prop
                        if (prop.PropertyType == typeof(SceneAsset))
                        {
                            value = JsonSerializer.Deserialize<SceneAsset>(
                                propElement.GetRawText(),
                                new JsonSerializerOptions { Converters = { new SceneAssetConverter() } }
                            );
                            prop.SetValue(componentObj, value);
                            continue;
                        }

                        // IAssignable prop
                        if (typeof(IAssignable).IsAssignableFrom(prop.PropertyType))
                        {
                            value = JsonSerializer.Deserialize<IAssignable>(
                                propElement.GetRawText(),
                                new JsonSerializerOptions { Converters = { new AssignableReferenceConverter() } }
                            );
                            prop.SetValue(componentObj, value);
                            continue;
                        }

                        // Regular property
                        value = JsonSerializer.Deserialize(
                            propElement.GetRawText(),
                            prop.PropertyType,
                            options
                        );
                        prop.SetValue(componentObj, value);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(
                            $"[Deserializer] Failed to load property {prop.Name} on {concreteType.Name}: {ex.Message}"
                        );
                    }
                }

                components.Add(component);
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
                    else if (fieldValue is IComponent comp && comp is ObjectComponent objComp)
                        writer.WriteNumberValue(objComp.ID);
                    else if (fieldValue is SceneAsset sceneAsset)
                        JsonSerializer.Serialize(writer, sceneAsset, typeof(SceneAsset), new JsonSerializerOptions { Converters = { new SceneAssetConverter() } });
                    else if (fieldValue is IAssignable assignable)
                        JsonSerializer.Serialize(writer, assignable, typeof(IAssignable), new JsonSerializerOptions { Converters = { new AssignableReferenceConverter() } });
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
                    else if (propValue is IComponent comp && comp is ObjectComponent objComp)
                        writer.WriteNumberValue(objComp.ID);
                    else if (propValue is SceneAsset sceneAsset)
                        JsonSerializer.Serialize(writer, sceneAsset, typeof(SceneAsset), new JsonSerializerOptions { Converters = { new SceneAssetConverter() } });
                    else if (propValue is IAssignable assignable)
                        JsonSerializer.Serialize(writer, assignable, typeof(IAssignable), new JsonSerializerOptions { Converters = { new AssignableReferenceConverter() } });
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

    /// <summary>
    /// Custom JSON converter for Microsoft.Xna.Framework.Vector3
    /// </summary>
    public class Vector3JsonConverter : JsonConverter<Microsoft.Xna.Framework.Vector3>
    {
        public override Microsoft.Xna.Framework.Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                // Handle string format (e.g., "1,2,3" or "1 2 3" or "1,2" for Vector2 compatibility)
                if (reader.TokenType == JsonTokenType.String)
                {
                    string value = reader.GetString() ?? "";
                    //Console.WriteLine($"[Vector3JsonConverter] Parsing string: '{value}'");
                    
                    var parts = value.Split(new char[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (parts.Length >= 2)
                    {
                        if (float.TryParse(parts[0], out float x) &&
                            float.TryParse(parts[1], out float y))
                        {
                            // Default Z to 0 if not provided (Vector2 compatibility)
                            float z = 0f;
                            if (parts.Length >= 3 && float.TryParse(parts[2], out float parsedZ))
                            {
                                z = parsedZ;
                            }
                            
                            //Console.WriteLine($"[Vector3JsonConverter] Successfully parsed: ({x}, {y}, {z})");
                            return new Microsoft.Xna.Framework.Vector3(x, y, z);
                        }
                    }
                    
                    //Console.WriteLine($"[Vector3JsonConverter] Failed to parse string: '{value}'");
                    return Microsoft.Xna.Framework.Vector3.Zero; // Return zero instead of throwing
                }
                
                // Handle object format (e.g., {"X": 1, "Y": 2, "Z": 3})
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    float x = 0, y = 0, z = 0;

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndObject)
                            break;

                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            string propertyName = reader.GetString() ?? "";
                            reader.Read();

                            switch (propertyName.ToLowerInvariant())
                            {
                                case "x":
                                    x = reader.GetSingle();
                                    break;
                                case "y":
                                    y = reader.GetSingle();
                                    break;
                                case "z":
                                    z = reader.GetSingle();
                                    break;
                            }
                        }
                    }

                    //Console.WriteLine($"[Vector3JsonConverter] Successfully parsed object: ({x}, {y}, {z})");
                    return new Microsoft.Xna.Framework.Vector3(x, y, z);
                }

                // Handle null or unexpected tokens
                if (reader.TokenType == JsonTokenType.Null)
                {
                    //Console.WriteLine("[Vector3JsonConverter] Received null, returning Vector3.Zero");
                    return Microsoft.Xna.Framework.Vector3.Zero;
                }

                Console.Error.WriteLine($"[Vector3JsonConverter] Unexpected token type: {reader.TokenType}, returning Vector3.Zero");
                return Microsoft.Xna.Framework.Vector3.Zero; // Return zero instead of throwing
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Vector3JsonConverter] Exception: {ex.Message}");
                return Microsoft.Xna.Framework.Vector3.Zero; // Return zero instead of throwing
            }
        }

        /// <summary>
        /// Write to the console line, a custom value
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        public override void Write(Utf8JsonWriter writer, Microsoft.Xna.Framework.Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteNumber("Z", value.Z);
            writer.WriteEndObject();
        }
    }
}

