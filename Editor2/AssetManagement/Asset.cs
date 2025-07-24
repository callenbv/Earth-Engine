using EarthEngineEditor.Windows;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Editor.AssetManagement
{
    public enum AssetType { Texture, Scene, Data, Script, Audio, Prefab, Unknown }
    public class Asset : IInspectable
    {
        public AssetType Type = AssetType.Unknown;
        public string Name = string.Empty;
        public string Path = string.Empty;
        public bool Folder = false;
        private IAssetHandler? _handler;
        private DateTime _lastModified;

        public void Open()
        {
            EnsureLoaded();
            _handler?.Open(Path);
        }

        public void Save()
        {
            string absPath = System.IO.Path.Combine(ProjectSettings.AssetsDirectory, Path);
            absPath = System.IO.Path.GetFullPath(absPath);
            _handler?.Save(absPath);
        }

        public void Delete(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Console.WriteLine($"Deleted {Name} at {filePath}");
            }
        }
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
                _ => AssetType.Unknown
            };
        }

        public static Asset Get(string path)
        {
            return ProjectWindow.Instance.Get(path);
        }
        public static string GetExtensionFromType(AssetType type)
        {
            return type switch
            {
                AssetType.Texture => ".png",
                AssetType.Scene => ".room",
                AssetType.Script => ".cs",
                AssetType.Data => ".json",
                AssetType.Prefab => ".eo",
                _ => ".asset"
            };
        }
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
                _ => ""
            };
        }

        public void Render()
        {
            EnsureLoaded();
            _handler?.Render();
        }
    }
}
