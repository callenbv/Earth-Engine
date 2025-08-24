/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         MaterialHandler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using ImGuiNET;
using System.IO;
using System.Text.Json;
using System.Reflection;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Minimal viewer/serializer for .mat files.
    /// </summary>
    public class MaterialHandler : IAssetHandler
    {
        private MaterialData? material;

        public void Load(string path)
        {
            if (!File.Exists(path)) return;
            string json = File.ReadAllText(path);
            material = JsonSerializer.Deserialize<MaterialData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public void Save(string path)
        {
            if (material == null) return;
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(material, options);
            File.WriteAllText(path, json);
        }

        public void Render()
        {
            if (material == null) return;

            // Get MaterialData type for reflection
            Type materialType = typeof(MaterialData);

            // Shader
            var shaderProperty = materialType.GetProperty("Shader")!;
            PrefabHandler.DrawField("Shader", material.Shader, typeof(string), 
                value => material.Shader = (string)value, shaderProperty);

            // Albedo Color (convert float array to Color for editor)
            var albedoProperty = materialType.GetProperty("AlbedoColor")!;
            var albedoColor = new Microsoft.Xna.Framework.Color(
                material.AlbedoColor[0], 
                material.AlbedoColor[1], 
                material.AlbedoColor[2], 
                material.AlbedoColor[3]);
            
            PrefabHandler.DrawField("Albedo Color", albedoColor, typeof(Microsoft.Xna.Framework.Color), 
                value => {
                    var color = (Microsoft.Xna.Framework.Color)value;
                    material.AlbedoColor[0] = color.R / 255f;
                    material.AlbedoColor[1] = color.G / 255f;
                    material.AlbedoColor[2] = color.B / 255f;
                    material.AlbedoColor[3] = color.A / 255f;
                }, albedoProperty);

            // Metallic
            var metallicProperty = materialType.GetProperty("Metallic")!;
            PrefabHandler.DrawField("Metallic", material.Metallic, typeof(float), 
                value => material.Metallic = (float)value, metallicProperty);

            // Roughness
            var roughnessProperty = materialType.GetProperty("Roughness")!;
            PrefabHandler.DrawField("Roughness", material.Roughness, typeof(float), 
                value => material.Roughness = (float)value, roughnessProperty);

            // Specular
            var specularProperty = materialType.GetProperty("Specular")!;
            PrefabHandler.DrawField("Specular", material.Specular, typeof(float), 
                value => material.Specular = (float)value, specularProperty);

            // Emissive Intensity
            var emissiveProperty = materialType.GetProperty("EmissiveIntensity")!;
            PrefabHandler.DrawField("Emissive Intensity", material.EmissiveIntensity, typeof(float), 
                value => material.EmissiveIntensity = (float)value, emissiveProperty);

            // Albedo Texture
            var albedoTexProperty = materialType.GetProperty("AlbedoTexture")!;
            PrefabHandler.DrawField("Albedo Texture", material.AlbedoTexture ?? "", typeof(string), 
                value => material.AlbedoTexture = string.IsNullOrEmpty((string)value) ? null : (string)value, albedoTexProperty);

            // Normal Texture
            var normalTexProperty = materialType.GetProperty("NormalTexture")!;
            PrefabHandler.DrawField("Normal Texture", material.NormalTexture ?? "", typeof(string), 
                value => material.NormalTexture = string.IsNullOrEmpty((string)value) ? null : (string)value, normalTexProperty);

            // Metallic Roughness Texture
            var metallicRoughnessTexProperty = materialType.GetProperty("MetallicRoughnessTexture")!;
            PrefabHandler.DrawField("Metallic Roughness Texture", material.MetallicRoughnessTexture ?? "", typeof(string), 
                value => material.MetallicRoughnessTexture = string.IsNullOrEmpty((string)value) ? null : (string)value, metallicRoughnessTexProperty);
        }
    }
}


