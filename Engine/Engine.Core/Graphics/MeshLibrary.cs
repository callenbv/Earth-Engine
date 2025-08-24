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
                    if (data != null) Materials[name] = data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MeshLibrary] {ex.Message}");
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


