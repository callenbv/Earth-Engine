/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         SceneAsset.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Represents a reference to a Scene asset that works in both editor and runtime                
/// -----------------------------------------------------------------------------

using System.Text.Json.Serialization;
using System.Text.Json;
using Engine.Core.Rooms;

namespace Engine.Core.Data
{
    /// <summary>
    /// Represents a reference to a Scene asset. 
    /// In the editor, this can be assigned from Scene assets.
    /// At runtime, this stores the path and can load the Room.
    /// </summary>
    public class SceneAsset : IAssignable
    {
        /// <summary>
        /// The path to the scene asset (relative to Assets directory)
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new SceneAsset reference
        /// </summary>
        public SceneAsset()
        {
        }

        /// <summary>
        /// Creates a SceneAsset from a path
        /// </summary>
        public SceneAsset(string path)
        {
            Path = path ?? string.Empty;
        }

        /// <summary>
        /// Loads the Room from this scene asset
        /// </summary>
        public Room? LoadRoom()
        {
            if (string.IsNullOrEmpty(Path))
                return null;

            try
            {
                string fullPath = System.IO.Path.Combine(EnginePaths.AssetsBase, Path);
                fullPath = System.IO.Path.GetFullPath(fullPath);
                return Room.Load(fullPath);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"[SceneAsset] Failed to load scene from path '{Path}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the display name for this scene asset
        /// </summary>
        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(Path))
                return "None";
            
            return System.IO.Path.GetFileNameWithoutExtension(Path);
        }
    }

    /// <summary>
    /// JSON converter for SceneAsset that serializes as a string path
    /// </summary>
    public class SceneAssetConverter : JsonConverter<SceneAsset>
    {
        public override SceneAsset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new SceneAsset();
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                string path = reader.GetString() ?? string.Empty;
                return new SceneAsset(path);
            }

            // Try reading as object with path property
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var element = doc.RootElement;
                
                if (element.TryGetProperty("path", out var pathProp))
                {
                    string path = pathProp.GetString() ?? string.Empty;
                    return new SceneAsset(path);
                }
            }

            return new SceneAsset();
        }

        public override void Write(Utf8JsonWriter writer, SceneAsset value, JsonSerializerOptions options)
        {
            if (value == null || string.IsNullOrEmpty(value.Path))
            {
                writer.WriteNullValue();
                return;
            }

            // Write as simple string for compactness
            writer.WriteStringValue(value.Path);
        }
    }
}

