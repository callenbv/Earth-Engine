/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Asset.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor.Windows;
using Engine.Core.Data;
using System.IO;
using System.Text.Json;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Represents the type of asset in the project.
    /// </summary>
    public enum AssetType { Texture, Scene, Data, Script, Audio, Prefab, Mesh, Material, RuleTile, Unknown }

    /// <summary>
    /// Represents an asset in the project.
    /// </summary>
    public class Asset : IInspectable
    {
        public AssetType Type = AssetType.Unknown;
        public string Name = string.Empty;
        public string Path = string.Empty;
        public string FullPath = string.Empty;
        public bool Folder = false;
        private IAssetHandler? _handler;
        public string FileIcon = "\uf15b";
        private DateTime _lastModified;

        /// <summary>
        /// Opens a new asset instance in the editor if applicable
        /// </summary>
        public void Open()
        {
            EnsureLoaded();
            _handler?.Open(Path);
        }

        /// <summary>
        /// Saves the asset to its path and any changes made to it
        /// </summary>
        public void Save()
        {
            string absPath = System.IO.Path.Combine(ProjectSettings.AssetsDirectory, Path);
            absPath = System.IO.Path.GetFullPath(absPath);
            EnsureLoaded();
            _handler?.Save(absPath);
        }

        /// <summary>
        /// Deletes the asset from the project and file system.
        /// </summary>
        /// <param name="filePath"></param>
        public void Delete(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Console.WriteLine($"Deleted {Name} at {filePath}");
            }
        }

        /// <summary>
        /// Ensures the asset is loaded and ready for use.
        /// </summary>
        public void EnsureLoaded()
        {
            string absPath = System.IO.Path.Combine(ProjectSettings.AssetsDirectory, Path);
            absPath = System.IO.Path.GetFullPath(absPath);

            if (!File.Exists(absPath)) return;

            var modified = File.GetLastWriteTimeUtc(absPath);

            if (_handler != null)
                return;

            _lastModified = modified;

            _handler = Type switch
            {
                AssetType.Prefab => new PrefabHandler(),
                AssetType.Scene => new SceneHandler(),
                AssetType.Texture => new TextureHandler(),
                AssetType.Audio => new AudioHandler(),
                AssetType.Mesh => new MeshHandler(),
                AssetType.Material => new MaterialHandler(),
                AssetType.RuleTile => new RuleTileHandler(),
                _ => null
            };

            try
            {
                _handler?.Load(absPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the icon for the given asset type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetIconForType(AssetType type)
        {
            return type switch
            {
                AssetType.Texture => "\uf1c5", // Image file icon
                AssetType.Scene => "\uf279", // Project diagram / sitemap
                AssetType.Script => "\uf1c9", // Code file
                AssetType.Data => "\uf1c0", // Database / data
                AssetType.Audio => "\uf001", // Music note
                AssetType.Prefab => "\uf1b2", // Cube / 3D object
                AssetType.Mesh => "\uf1b2", // Reuse cube icon
                AssetType.RuleTile => "\uf1b2", // Reuse cube icon
                AssetType.Material => "\uf53f", // Fill drip (palette-like)
                AssetType.Unknown => "\uf15b", // Generic file
                _ => "\uf15b", // Fallback
            };
        }

        /// <summary>
        /// Gets the asset type based on the file extension of the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static AssetType GetAssetTypeFromExtension(string path)
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();

            return ext switch
            {
                ".png" or ".jpg" or ".jpeg" => AssetType.Texture,
                ".room" => AssetType.Scene,
                ".cs" => AssetType.Script,
                ".json" => AssetType.Data,
                ".wav" or ".ogg" or ".mp3" => AssetType.Audio,
                ".prefab" or ".eo" => AssetType.Prefab,
                ".mesh" => AssetType.Mesh,
                ".tile" => AssetType.RuleTile,
                ".mat" => AssetType.Material,
                _ => AssetType.Unknown
            };
        }

        /// <summary>
        /// Gets an asset by its path from the project window.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Asset Get(string path)
        {
            return ProjectWindow.Instance.Get(path);
        }

        /// <summary>
        /// Gets the file extension associated with the given asset type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetExtensionFromType(AssetType type)
        {
            return type switch
            {
                AssetType.Texture => ".png",
                AssetType.Scene => ".room",
                AssetType.Script => ".cs",
                AssetType.Data => ".json",
                AssetType.Prefab => ".eo",
                AssetType.Mesh => ".mesh",
                AssetType.RuleTile => ".tile",
                AssetType.Material => ".mat",
                _ => ".asset"
            };
        }

        /// <summary>
        /// Generates a template for the given asset type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public static string GenerateTemplateForAssetType(AssetType type, string assetName = "NewAsset")
        {
            string baseDir = AppContext.BaseDirectory;
            string templatePath = System.IO.Path.Combine(baseDir, "Templates", $"{type}.txt");

            if (File.Exists(templatePath))
            {
                string template = File.ReadAllText(templatePath);

                if (type == AssetType.Script)
                    template = template.Replace("{CLASS_NAME}", assetName);

                return template;
            }

            // Fallbacks only for non-script types
            return type switch
            {
                AssetType.Prefab => "{\n  \"name\": \"NewPrefab\",\n  \"components\": []\n}",
                AssetType.Scene => "{\n  \"entities\": []\n}",
                AssetType.Data => "{\n  \"key\": \"value\"\n}",
                AssetType.Material => "{\n  \"Name\": \"" + assetName + "\",\n  \"AlbedoColor\": [1.0, 1.0, 1.0, 1.0],\n  \"Metallic\": 0.0,\n  \"Roughness\": 1.0,\n  \"Specular\": 0.5,\n  \"EmissiveIntensity\": 0.0,\n  \"Shader\": \"Standard\",\n  \"AlbedoTilingX\": 1.0,\n  \"AlbedoTilingY\": 1.0,\n  \"NormalTilingX\": 1.0,\n  \"NormalTilingY\": 1.0,\n  \"MetallicRoughnessTilingX\": 1.0,\n  \"MetallicRoughnessTilingY\": 1.0\n}",
                _ => ""
            };
        }

        /// <summary>
        /// Renders the asset in the editor for inspection or editing.
        /// </summary>
        public void Render()
        {
            EnsureLoaded();
            _handler?.Render();
        }
    }
}

