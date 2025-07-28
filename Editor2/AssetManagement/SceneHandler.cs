/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         SceneHandler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor.Windows;
using Engine.Core.Data;
using Engine.Core.Rooms;
using GameRuntime;
using MonoGame.Extended.Serialization.Json;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Represents a folder structure for organizing GameObjects in a scene.
    /// </summary>
    public class SceneFolderSerializable
    {
        public string Name;
        public List<string> GameObjectNames = new();
        public List<SceneFolderSerializable> SubFolders = new();
    }

    /// <summary>
    /// Represents metadata for a scene, including folders and organization of GameObjects.
    /// </summary>
    public class SceneMetadata
    {
        public List<SceneFolderSerializable> Folders { get; set; } = new();
    }


    /// <summary>
    /// Handles loading, saving, and opening scenes in the editor.
    /// </summary>
    public class SceneHandler : IAssetHandler
    {
        private Room? scene;

        /// <summary>
        /// Load the scene from a room file
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {

        }


        /// <summary>
        /// Save the room data if it changed
        /// </summary>
        public void Save(string path)
        {
            scene = SceneViewWindow.Instance.scene;

            if (scene == null)
            {
                Console.WriteLine("No scene to save.");
                return;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    IncludeFields = true,
                    Converters = { new ComponentListJsonConverter() },
                    ReferenceHandler = ReferenceHandler.Preserve
                };
                options.Converters.Add(new Vector2JsonConverter());
                options.Converters.Add(new ColorJsonConverter());

                string json = JsonSerializer.Serialize(scene, options);
                File.WriteAllText(path, json);
                Console.WriteLine($"Scene saved: {scene.Name}");

                // Save folder metadata
                var metadata = new SceneMetadata();
                foreach (var folder in SceneViewWindow.Instance.rootFolder.SubFolders)
                {
                    metadata.Folders.Add(ToSerializable(folder));
                }

                string metaPath = path + ".meta";
                string metaJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true });
                File.WriteAllText(metaPath, metaJson);
                Console.WriteLine("Scene metadata saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save scene: {ex.Message}");
            }
        }

        /// <summary>
        /// Set the selected scene
        /// </summary>
        /// <param name="path"></param>
        public void Open(string path)
        {
            scene = Room.Load(path);
            SceneViewWindow.Instance.scene = scene;
            SceneViewWindow.Instance.scene.FilePath = path;
            RuntimeManager.Instance.scene = scene;

            SceneFolder root = new SceneFolder("Scene");
            string fullAssetPath = Path.Combine(ProjectSettings.AssetsDirectory, path);
            string relativePath = Path.GetRelativePath(ProjectSettings.AssetsDirectory, fullAssetPath);
            string metaPath = Path.Combine(ProjectSettings.AssetsDirectory, relativePath + ".meta");

            if (File.Exists(metaPath))
            {
                string metaJson = File.ReadAllText(metaPath);

                var options = new JsonSerializerOptions
                {
                    IncludeFields = true
                };
                var metadata = JsonSerializer.Deserialize<SceneMetadata>(metaJson, options);

                if (metadata != null)
                {
                    foreach (var sFolder in metadata.Folders)
                    {
                        var folder = FromSerializable(sFolder);
                        root.SubFolders.Add(folder);
                    }
                }
            }
            else
            {
                foreach (var obj in scene.objects)
                {
                    root.GameObjects.Add(obj);
                }
            }

            SceneViewWindow.Instance.rootFolder = root;
        }

        /// <summary>
        /// Converts a SceneFolder to a SceneFolderSerializable for serialization.
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        public static SceneFolderSerializable ToSerializable(SceneFolder folder)
        {
            return new SceneFolderSerializable
            {
                Name = folder.Name,
                GameObjectNames = folder.GameObjects.Select(go => go.Name).ToList(),
                SubFolders = folder.SubFolders.Select(ToSerializable).ToList()
            };
        }

        /// <summary>
        /// Creates a SceneFolder from a SceneFolderSerializable object.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public SceneFolder FromSerializable(SceneFolderSerializable s)
        {
            var folder = new SceneFolder(s.Name);
            folder.SubFolders = s.SubFolders.Select(FromSerializable).ToList();

            foreach (string name in s.GameObjectNames)
            {
                var go = scene.objects.FirstOrDefault(o => o.Name == name);
                if (go != null)
                    folder.GameObjects.Add(go);
            }

            return folder;
        }
    }
}

