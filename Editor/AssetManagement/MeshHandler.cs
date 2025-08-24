/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         MeshHandler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using ImGuiNET;
using System.IO;
using System.Text.Json;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Minimal viewer/serializer for .mesh files.
    /// </summary>
    public class MeshHandler : IAssetHandler
    {
        private MeshData? mesh;

        public void Load(string path)
        {
            if (!File.Exists(path)) return;
            string json = File.ReadAllText(path);
            mesh = JsonSerializer.Deserialize<MeshData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (mesh != null)
            {
                // Recompute bounds if missing/zero to aid debugging visibility
                bool zeroBounds = mesh.Bounds.HasValue &&
                                  mesh.Bounds.Value.MinX == 0 && mesh.Bounds.Value.MinY == 0 && mesh.Bounds.Value.MinZ == 0 &&
                                  mesh.Bounds.Value.MaxX == 0 && mesh.Bounds.Value.MaxY == 0 && mesh.Bounds.Value.MaxZ == 0;
                if (!mesh.Bounds.HasValue || zeroBounds)
                {
                    if (mesh.Positions != null && mesh.Positions.Length >= 3)
                    {
                        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
                        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
                        for (int i = 0; i < mesh.Positions.Length; i += 3)
                        {
                            float x = mesh.Positions[i + 0];
                            float y = mesh.Positions[i + 1];
                            float z = mesh.Positions[i + 2];
                            if (x < minX) minX = x; if (y < minY) minY = y; if (z < minZ) minZ = z;
                            if (x > maxX) maxX = x; if (y > maxY) maxY = y; if (z > maxZ) maxZ = z;
                        }
                        mesh.Bounds = new BoundingBox { MinX = minX, MinY = minY, MinZ = minZ, MaxX = maxX, MaxY = maxY, MaxZ = maxZ };
                    }
                }
            }
        }

        public void Save(string path)
        {
            if (mesh == null) return;
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(mesh, options);
            File.WriteAllText(path, json);
        }

        public void Render()
        {
            if (mesh == null) return;
            ImGui.Text($"Mesh: {mesh.Name}");
            ImGui.Text($"Vertices: {(mesh.Positions?.Length ?? 0) / 3}");
            ImGui.Text($"Indices: {mesh.Indices?.Length ?? 0}");
            if (mesh.Bounds != null)
            {
                ImGui.Text($"Bounds Min: {mesh.Bounds.Value.MinX}, {mesh.Bounds.Value.MinY}, {mesh.Bounds.Value.MinZ}");
                ImGui.Text($"Bounds Max: {mesh.Bounds.Value.MaxX}, {mesh.Bounds.Value.MaxY}, {mesh.Bounds.Value.MaxZ}");
            }
        }
    }
}


