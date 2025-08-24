/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         MeshLibrary.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Runtime registry for mesh and material assets (.mesh/.mat).          
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using System.Text.Json;

namespace Engine.Core.Graphics
{
    public static class MeshLibrary
    {
        public static readonly Dictionary<string, MeshData> Meshes = new();
        public static readonly Dictionary<string, MaterialData> Materials = new();
        private static readonly Dictionary<string, DateTime> MaterialLastModified = new(); // Track when materials were last modified
        
        // Event to notify when a material is reloaded
        public static event Action<string>? MaterialReloaded;

        public static void LoadAll()
        {
            try
            {
                string root = EnginePaths.AssetsBase;
                foreach (var file in Directory.GetFiles(root, "*.mesh", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var json = File.ReadAllText(file);
                    var data = JsonSerializer.Deserialize<MeshData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (data != null)
                    {
                        EnsureBounds(data);
                        Meshes[name] = data;
                    }
                }

                foreach (var file in Directory.GetFiles(root, "*.mat", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var json = File.ReadAllText(file);
                    var data = JsonSerializer.Deserialize<MaterialData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (data != null) 
                    {
                        Materials[name] = data;
                        MaterialLastModified[name] = File.GetLastWriteTimeUtc(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[MeshLibrary] {ex.Message}");
            }

            // Log what was loaded for debugging
            LogLoadedAssets();
        }

        public static MeshData? GetMesh(string name)
        {
            Meshes.TryGetValue(name, out var m); return m;
        }

        public static MaterialData? GetMaterial(string name)
        {
            Materials.TryGetValue(name, out var m); return m;
        }

        /// <summary>
        /// Reloads a specific material from disk
        /// </summary>
        public static void ReloadMaterial(string name)
        {
            try
            {
                string root = EnginePaths.AssetsBase;
                string materialPath = Path.Combine(root, name + ".mat");
                
                if (File.Exists(materialPath))
                {
                    var json = File.ReadAllText(materialPath);
                    var data = JsonSerializer.Deserialize<MaterialData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (data != null)
                    {
                        Materials[name] = data;
                        MaterialLastModified[name] = DateTime.UtcNow; // Update the modification timestamp
                        MaterialReloaded?.Invoke(name); // Notify listeners
                    }
                    else
                    {
                        Console.Error.WriteLine($"[MeshLibrary] Failed to deserialize material '{name}'");
                    }
                }
                else
                {
                    Console.Error.WriteLine($"[MeshLibrary] Material file not found: {materialPath}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[MeshLibrary] Error reloading material '{name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Updates a material directly in memory without reloading from disk
        /// </summary>
        public static void UpdateMaterial(string name, MaterialData newData)
        {
            if (Materials.ContainsKey(name))
            {
                Materials[name] = newData;
                MaterialLastModified[name] = DateTime.UtcNow;
                MaterialReloaded?.Invoke(name); // Notify listeners
            }
            else
            {
                // Add the material to memory if it doesn't exist
                Materials[name] = newData;
                MaterialLastModified[name] = DateTime.UtcNow;
                Console.WriteLine($"[MeshLibrary] Added new material '{name}' to memory");
                MaterialReloaded?.Invoke(name); // Notify listeners
            }
        }

        /// <summary>
        /// Loads a specific material from disk and adds it to memory
        /// </summary>
        public static void LoadMaterial(string name)
        {
            try
            {
                string root = EnginePaths.AssetsBase;
                string materialPath = Path.Combine(root, name + ".mat");
                
                if (File.Exists(materialPath))
                {
                    var json = File.ReadAllText(materialPath);
                    var data = JsonSerializer.Deserialize<MaterialData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (data != null)
                    {
                        Materials[name] = data;
                        MaterialLastModified[name] = File.GetLastWriteTimeUtc(materialPath);
                        Console.WriteLine($"[MeshLibrary] Loaded material '{name}' from disk");
                    }
                    else
                    {
                        Console.Error.WriteLine($"[MeshLibrary] Failed to deserialize material '{name}'");
                    }
                }
                else
                {
                    Console.Error.WriteLine($"[MeshLibrary] Material file not found: {materialPath}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[MeshLibrary] Error loading material '{name}': {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the last modification time of a material
        /// </summary>
        public static DateTime GetMaterialLastModified(string name)
        {
            return MaterialLastModified.TryGetValue(name, out var time) ? time : DateTime.MinValue;
        }

        /// <summary>
        /// Debug method to list all loaded meshes and materials
        /// </summary>
        public static void LogLoadedAssets()
        {
            Console.WriteLine($"[MeshLibrary] Loaded {Meshes.Count} meshes:");
            foreach (var mesh in Meshes.Keys)
                Console.WriteLine($"  - {mesh}");
            
            Console.WriteLine($"[MeshLibrary] Loaded {Materials.Count} materials:");
            foreach (var material in Materials.Keys)
                Console.WriteLine($"  - {material}");
        }

        private static void EnsureBounds(MeshData data)
        {
            if (data == null || data.Positions == null || data.Positions.Length < 3)
                return;

            bool hasBounds = data.Bounds.HasValue && !(data.Bounds.Value.MinX == 0f && data.Bounds.Value.MinY == 0f && data.Bounds.Value.MinZ == 0f &&
                                                       data.Bounds.Value.MaxX == 0f && data.Bounds.Value.MaxY == 0f && data.Bounds.Value.MaxZ == 0f);
            if (hasBounds) return;

            float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
            for (int i = 0; i < data.Positions.Length; i += 3)
            {
                float x = data.Positions[i + 0];
                float y = data.Positions[i + 1];
                float z = data.Positions[i + 2];
                if (x < minX) minX = x; if (y < minY) minY = y; if (z < minZ) minZ = z;
                if (x > maxX) maxX = x; if (y > maxY) maxY = y; if (z > maxZ) maxZ = z;
            }
            data.Bounds = new BoundingBox { MinX = minX, MinY = minY, MinZ = minZ, MaxX = maxX, MaxY = maxY, MaxZ = maxZ };
        }
    }
}


