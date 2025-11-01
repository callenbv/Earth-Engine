/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         SceneHandler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using EarthEngineEditor;
using EarthEngineEditor.Windows;
using Engine.Core.Data;
using Engine.Core.Game;
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
            try
            {
                if (File.Exists(path))
                {
                    scene = Room.Load(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load scene from {path}: {ex.Message}");
            }
        }


        /// <summary>
        /// Save the room data if it changed
        /// </summary>
        public void Save(string path)
        {
            // Normalize paths for comparison
            string normalizedPath = Path.GetFullPath(path);
            
            // Check if this path matches the currently open scene
            Room? sceneToSave = null;
            
            // If the path matches the currently open scene, use that (so changes are saved)
            if (SceneViewWindow.Instance.scene != null)
            {
                string currentScenePath = Path.Combine(ProjectSettings.AssetsDirectory, SceneViewWindow.Instance.scene.FilePath);
                currentScenePath = Path.GetFullPath(currentScenePath);
                
                if (normalizedPath.Equals(currentScenePath, StringComparison.OrdinalIgnoreCase))
                {
                    sceneToSave = SceneViewWindow.Instance.scene;
                }
            }
            
            // Otherwise, if we don't have a scene loaded for this path, load it first
            if (sceneToSave == null)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        sceneToSave = Room.Load(path);
                        scene = sceneToSave; // Store it for next time
                    }
                    else
                    {
                        // Create a new empty scene if the file doesn't exist
                        sceneToSave = new Room
                        {
                            Name = Path.GetFileNameWithoutExtension(path),
                            FilePath = Path.GetRelativePath(ProjectSettings.AssetsDirectory, path)
                        };
                        scene = sceneToSave;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load scene from {path} for saving: {ex.Message}");
                    return;
                }
            }

            if (sceneToSave == null)
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
                };
                options.Converters.Add(new Vector2JsonConverter());
                options.Converters.Add(new Vector3JsonConverter());
                options.Converters.Add(new ColorJsonConverter());

                string json = JsonSerializer.Serialize(sceneToSave, options);
                File.WriteAllText(path, json);
                Console.WriteLine($"Scene saved: {sceneToSave.Name} to {path}");

                // Save folder metadata only if this is the currently open scene
                if (SceneViewWindow.Instance.scene != null && 
                    SceneViewWindow.Instance.scene.FilePath == path)
                {
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
            // Load the scene
            scene = Room.Load(path);
            SceneViewWindow.Instance.scene = scene;
            SceneViewWindow.Instance.scene.FilePath = path;
            RuntimeManager.Instance.scene = scene;

            // Clear the old folder structure completely - create a fresh root
            SceneFolder root = new SceneFolder("Root");
            
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
                        // Clean up any invalid references
                        CleanupFolderGameObjects(folder, scene.objects);
                        root.SubFolders.Add(folder);
                    }
                }
            }
            
            // Set the new root folder (this replaces the old one completely)
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

            // Only add GameObjects that exist in the current scene
            foreach (string name in s.GameObjectNames)
            {
                var go = scene.objects.FirstOrDefault(o => o.Name == name);
                if (go != null)
                    folder.GameObjects.Add(go);
            }

            return folder;
        }
        
        /// <summary>
        /// Recursively clears all GameObjects from folders that no longer exist in the scene
        /// </summary>
        private void CleanupFolderGameObjects(SceneFolder folder, List<GameObject> validObjects)
        {
            // Remove objects that are no longer in the scene
            folder.GameObjects.RemoveAll(obj => !validObjects.Contains(obj));
            
            // Recursively clean subfolders
            foreach (var subFolder in folder.SubFolders)
            {
                CleanupFolderGameObjects(subFolder, validObjects);
            }
        }
    }
}

