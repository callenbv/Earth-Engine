/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         DataModels.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.Core.Data;

namespace Engine.Core.Data
{
    public class EarthObject
    {
        public double x { get; set; }
        public double y { get; set; }
        public string? name { get; set; }
        public string? objectPath { get; set; }
        public string? sprite { get; set; }
  
        public List<string> components { get; set; } = new List<string>();
        public Dictionary<string, Dictionary<string, object>> componentProperties { get; set; } = new();
    }

    /// <summary>
    /// Serializable mesh container for runtime consumption.
    /// Tangents/bitangents optional for now.
    /// </summary>
    public class MeshData
    {
        public string Name { get; set; } = string.Empty;
        public float[] Positions { get; set; } = Array.Empty<float>(); // xyz per vertex
        public float[] Normals { get; set; } = Array.Empty<float>();   // xyz per vertex
        public float[] UV0 { get; set; } = Array.Empty<float>();       // uv per vertex
        public int[] Indices { get; set; } = Array.Empty<int>();       // triangle indices
        public BoundingBox? Bounds { get; set; }
        public List<MeshSubmesh> Submeshes { get; set; } = new();
        public string? MaterialPath { get; set; }
    }

    public class MeshSubmesh
    {
        public string Name { get; set; } = string.Empty;
        public int StartIndex { get; set; }
        public int IndexCount { get; set; }
        public string? MaterialPath { get; set; }
    }

    public class MaterialData
    {
        public string Name { get; set; } = "Material";
        
        [HideInInspector]
        public string? AlbedoTexture { get; set; }
        [HideInInspector]
        public string? NormalTexture { get; set; }
        [HideInInspector]
        public string? MetallicRoughnessTexture { get; set; }
        
        // Editor-friendly texture properties (not serialized)
        [HideInInspector]
        public string? AlbedoTextureName { get; set; }
        [HideInInspector]
        public string? NormalTextureName { get; set; }
        [HideInInspector]
        public string? MetallicRoughnessTextureName { get; set; }
        
        public float[] AlbedoColor { get; set; } = new float[] { 1f, 1f, 1f, 1f };
        public float Metallic { get; set; } = 0f;
        public float Roughness { get; set; } = 1f;
        public float Specular { get; set; } = 0.5f;
        public float EmissiveIntensity { get; set; } = 0f;
        public string Shader { get; set; } = "Standard";
        
        // UV Tiling properties
        public float AlbedoTilingX { get; set; } = 1f;
        public float AlbedoTilingY { get; set; } = 1f;
        public float NormalTilingX { get; set; } = 1f;
        public float NormalTilingY { get; set; } = 1f;
        public float MetallicRoughnessTilingX { get; set; } = 1f;
        public float MetallicRoughnessTilingY { get; set; } = 1f;
    }

    public struct BoundingBox
    {
        public float MinX { get; set; }
        public float MinY { get; set; }
        public float MinZ { get; set; }
        public float MaxX { get; set; }
        public float MaxY { get; set; }
        public float MaxZ { get; set; }
    }
}

