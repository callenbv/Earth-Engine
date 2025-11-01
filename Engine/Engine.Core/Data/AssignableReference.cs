/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         AssignableReference.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Handles serialization and deserialization of IAssignable references                
/// -----------------------------------------------------------------------------

using Engine.Core.Game.Components;
using Engine.Core.Game;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Core;

namespace Engine.Core.Data
{
    /// <summary>
    /// Custom JSON converter for serializing and deserializing IAssignable references.
    /// Handles different identification methods for different types:
    /// - IComponent (ObjectComponent): uses ID (int)
    /// - GameObject: uses ID (Guid)
    /// - Asset: uses Path (string)
    /// </summary>
    public class AssignableReferenceConverter : JsonConverter<IAssignable>
    {
        public override IAssignable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Handle null
            if (reader.TokenType == JsonTokenType.Null)
            {
                reader.Read(); // Consume the null token
                return null;
            }

            // Use JsonDocument to parse the value - it can handle any JSON type
            using var doc = JsonDocument.ParseValue(ref reader);
            var element = doc.RootElement;

            // Handle number (Component ID - backward compatible)
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int componentId))
            {
                return FindComponentById(componentId);
            }

            // Handle string (GameObject GUID or Asset Path - backward compatible)
            if (element.ValueKind == JsonValueKind.String)
            {
                string strValue = element.GetString() ?? string.Empty;
                // Try as GameObject GUID
                if (Guid.TryParse(strValue, out Guid gameObjectId))
                {
                    return FindGameObjectById(gameObjectId);
                }
                // Try as Asset Path
                var asset = FindAssetByPath(strValue);
                if (asset != null)
                {
                    return asset;
                }
                return null;
            }

            // Handle object (new format with type information)
            if (element.ValueKind == JsonValueKind.Object)
            {
                // Check if it has a type property
                if (element.TryGetProperty("type", out var typeProp))
                {
                    string assignableType = typeProp.GetString() ?? string.Empty;

                    // Handle Component reference
                    if (assignableType == "Component" || assignableType.Contains("IComponent"))
                    {
                        if (element.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out int id))
                        {
                            return FindComponentById(id);
                        }
                    }
                    // Handle GameObject reference
                    else if (assignableType == "GameObject" || assignableType.Contains("GameObject"))
                    {
                        if (element.TryGetProperty("id", out var guidProp))
                        {
                            string guidStr = guidProp.GetString() ?? string.Empty;
                            if (Guid.TryParse(guidStr, out Guid guid))
                            {
                                return FindGameObjectById(guid);
                            }
                        }
                    }
                    // Handle Asset reference
                    else if (assignableType == "Asset" || assignableType.Contains("Asset"))
                    {
                        if (element.TryGetProperty("path", out var pathProp))
                        {
                            string path = pathProp.GetString() ?? string.Empty;
                            return FindAssetByPath(path);
                        }
                    }
                }
                
                // Fallback: try to infer from structure (no type property)
                if (element.TryGetProperty("id", out var idProp2))
                {
                    // Try as Component ID (int)
                    if (idProp2.ValueKind == JsonValueKind.Number && idProp2.TryGetInt32(out int componentId2))
                    {
                        return FindComponentById(componentId2);
                    }
                    // Try as GameObject GUID (string)
                    if (idProp2.ValueKind == JsonValueKind.String)
                    {
                        string guidStr = idProp2.GetString() ?? string.Empty;
                        if (Guid.TryParse(guidStr, out Guid gameObjectId2))
                        {
                            return FindGameObjectById(gameObjectId2);
                        }
                    }
                }
                
                // Check for Asset path
                if (element.TryGetProperty("path", out var pathProp2))
                {
                    string path = pathProp2.GetString() ?? string.Empty;
                    return FindAssetByPath(path);
                }
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, IAssignable value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            // Handle Component (ObjectComponent) - write as number for backward compatibility
            if (value is IComponent component && component is ObjectComponent objComp)
            {
                writer.WriteNumberValue(objComp.ID);
                return;
            }
            
            // Handle GameObject - write as string (GUID) for backward compatibility
            if (value is GameObject gameObject)
            {
                writer.WriteStringValue(gameObject.ID.ToString());
                return;
            }
            
            // Handle Asset (Editor.AssetManagement.Asset) - use reflection to avoid dependency
            try
            {
                var assetType = value.GetType();
                if (assetType.Name == "Asset" && assetType.Namespace == "Editor.AssetManagement")
                {
                    var pathProperty = assetType.GetProperty("Path");
                    if (pathProperty != null)
                    {
                        var path = pathProperty.GetValue(value)?.ToString() ?? string.Empty;
                        writer.WriteStartObject();
                        writer.WriteString("type", "Asset");
                        writer.WriteString("path", path);
                        writer.WriteEndObject();
                        return;
                    }
                }
            }
            catch
            {
                // Reflection failed - fall through to default
            }

            // For other IAssignable types, write as object with full type info
            writer.WriteStartObject();
            writer.WriteString("type", value.GetType().FullName ?? value.GetType().Name);
            writer.WriteEndObject();
        }

        private IComponent? FindComponentById(int id)
        {
            var scene = EngineContext.Current.Scene;
            if (scene == null) return null;

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

        private GameObject? FindGameObjectById(Guid id)
        {
            var scene = EngineContext.Current.Scene;
            if (scene == null) return null;

            return scene.objects.FirstOrDefault(o => o.ID == id);
        }

        private IAssignable? FindAssetByPath(string path)
        {
            // Use reflection to avoid hard dependency on Editor assembly
            try
            {
                var projectWindowType = Type.GetType("EarthEngineEditor.Windows.ProjectWindow, Editor");
                if (projectWindowType != null)
                {
                    var instanceProperty = projectWindowType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (instanceProperty != null)
                    {
                        var instance = instanceProperty.GetValue(null);
                        if (instance != null)
                        {
                            var getMethod = projectWindowType.GetMethod("Get", new[] { typeof(string) });
                            if (getMethod != null)
                            {
                                var asset = getMethod.Invoke(instance, new object[] { path });
                                if (asset is IAssignable assignableAsset)
                                {
                                    return assignableAsset;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[AssignableReferenceConverter] Failed to find Asset by path '{path}': {ex.Message}");
            }

            return null;
        }
    }
}

