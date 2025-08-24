/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         MaterialHandler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using Engine.Core.Graphics;
using ImGuiNET;
using System.IO;
using System.Text.Json;
using System.Reflection;
using System.Numerics;
using EarthEngineEditor;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Minimal viewer/serializer for .mat files.
    /// </summary>
    public class MaterialHandler : IAssetHandler
    {
        private MaterialData? material;

        private string? _currentMaterialPath;

        public void Load(string path)
        {
            if (!File.Exists(path)) 
            {
                Console.Error.WriteLine($"[MaterialHandler] File does not exist: {path}");
                return;
            }
            _currentMaterialPath = path;
            string json = File.ReadAllText(path);
            Console.WriteLine($"[MaterialHandler] Loading material from {path}, JSON content: {json}");
            material = JsonSerializer.Deserialize<MaterialData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Console.WriteLine($"[MaterialHandler] Material deserialized: {material != null}");
            if (material != null)
            {
                Console.WriteLine($"[MaterialHandler] Material name: {material.Name}, AlbedoColor: [{material.AlbedoColor[0]}, {material.AlbedoColor[1]}, {material.AlbedoColor[2]}, {material.AlbedoColor[3]}]");
                
                // Load the material into the MeshLibrary so it's available for editing
                string materialName = Path.GetFileNameWithoutExtension(path);
                MeshLibrary.LoadMaterial(materialName);
            }
        }

        public void Save(string path)
        {
            if (material == null) return;
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(material, options);
            File.WriteAllText(path, json);
        }

        private void SaveCurrentMaterial()
        {
            if (material == null || _currentMaterialPath == null) 
            {
                Console.Error.WriteLine($"[MaterialHandler] Cannot save - material or path is null");
                return;
            }
            
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(material, options);
                File.WriteAllText(_currentMaterialPath, json);
                
                // Trigger a reload in the MeshLibrary
                string materialName = Path.GetFileNameWithoutExtension(_currentMaterialPath);
                MeshLibrary.UpdateMaterial(materialName, material);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[MaterialHandler] Failed to auto-save material: {ex.Message}");
            }
        }

        public void Render()
        {
            if (material == null) return;

            // Get MaterialData type for reflection
            Type materialType = typeof(MaterialData);

            // Shader
            var shaderProperty = materialType.GetProperty("Shader")!;
            PrefabHandler.DrawField("Shader", material.Shader, typeof(string), 
                value => {
                    material.Shader = (string)value;
                    SaveCurrentMaterial();
                }, shaderProperty);

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
                    SaveCurrentMaterial();
                }, albedoProperty);

            // Metallic
            var metallicProperty = materialType.GetProperty("Metallic")!;
            PrefabHandler.DrawField("Metallic", material.Metallic, typeof(float), 
                value => {
                    material.Metallic = (float)value;
                    SaveCurrentMaterial();
                }, metallicProperty);

            // Roughness
            var roughnessProperty = materialType.GetProperty("Roughness")!;
            PrefabHandler.DrawField("Roughness", material.Roughness, typeof(float), 
                value => {
                    material.Roughness = (float)value;
                    SaveCurrentMaterial();
                }, roughnessProperty);

            // Specular
            var specularProperty = materialType.GetProperty("Specular")!;
            PrefabHandler.DrawField("Specular", material.Specular, typeof(float), 
                value => {
                    material.Specular = (float)value;
                    SaveCurrentMaterial();
                }, specularProperty);

            // Emissive Intensity
            var emissiveProperty = materialType.GetProperty("EmissiveIntensity")!;
            PrefabHandler.DrawField("Emissive Intensity", material.EmissiveIntensity, typeof(float), 
                value => {
                    material.EmissiveIntensity = (float)value;
                    SaveCurrentMaterial();
                }, emissiveProperty);

            ImGui.Separator();
            ImGui.Text("Texture Settings");
            ImGui.Spacing();

            // Texture Selection UI
            DrawTextureSelector("Albedo Texture", material.AlbedoTexture, 
                value => material.AlbedoTexture = value);
            
            // Albedo Tiling
            if (!string.IsNullOrEmpty(material.AlbedoTexture))
            {
                ImGui.Indent(20f);
                var albedoTilingXProperty = materialType.GetProperty("AlbedoTilingX")!;
                PrefabHandler.DrawField("Albedo Tiling X", material.AlbedoTilingX, typeof(float), 
                    value => {
                        material.AlbedoTilingX = (float)value;
                        SaveCurrentMaterial();
                    }, albedoTilingXProperty);
                
                var albedoTilingYProperty = materialType.GetProperty("AlbedoTilingY")!;
                PrefabHandler.DrawField("Albedo Tiling Y", material.AlbedoTilingY, typeof(float), 
                    value => {
                        material.AlbedoTilingY = (float)value;
                        SaveCurrentMaterial();
                    }, albedoTilingYProperty);
                ImGui.Unindent(20f);
            }
            
            DrawTextureSelector("Normal Texture", material.NormalTexture, 
                value => material.NormalTexture = value);
            
            // Normal Tiling
            if (!string.IsNullOrEmpty(material.NormalTexture))
            {
                ImGui.Indent(20f);
                var normalTilingXProperty = materialType.GetProperty("NormalTilingX")!;
                PrefabHandler.DrawField("Normal Tiling X", material.NormalTilingX, typeof(float), 
                    value => {
                        material.NormalTilingX = (float)value;
                        SaveCurrentMaterial();
                    }, normalTilingXProperty);
                
                var normalTilingYProperty = materialType.GetProperty("NormalTilingY")!;
                PrefabHandler.DrawField("Normal Tiling Y", material.NormalTilingY, typeof(float), 
                    value => {
                        material.NormalTilingY = (float)value;
                        SaveCurrentMaterial();
                    }, normalTilingYProperty);
                ImGui.Unindent(20f);
            }
            
            DrawTextureSelector("Metallic Roughness Texture", material.MetallicRoughnessTexture, 
                value => material.MetallicRoughnessTexture = value);
            
            // Metallic Roughness Tiling
            if (!string.IsNullOrEmpty(material.MetallicRoughnessTexture))
            {
                ImGui.Indent(20f);
                var metallicRoughnessTilingXProperty = materialType.GetProperty("MetallicRoughnessTilingX")!;
                PrefabHandler.DrawField("Metallic Roughness Tiling X", material.MetallicRoughnessTilingX, typeof(float), 
                    value => {
                        material.MetallicRoughnessTilingX = (float)value;
                        SaveCurrentMaterial();
                    }, metallicRoughnessTilingXProperty);
                
                var metallicRoughnessTilingYProperty = materialType.GetProperty("MetallicRoughnessTilingY")!;
                PrefabHandler.DrawField("Metallic Roughness Tiling Y", material.MetallicRoughnessTilingY, typeof(float), 
                    value => {
                        material.MetallicRoughnessTilingY = (float)value;
                        SaveCurrentMaterial();
                    }, metallicRoughnessTilingYProperty);
                ImGui.Unindent(20f);
            }
        }

        private void DrawTextureSelector(string label, string? currentPath, Action<string?> setValue)
        {
            // Get texture name from path for display
            string displayName = string.IsNullOrEmpty(currentPath) ? "(None)" : 
                Path.GetFileNameWithoutExtension(currentPath);
            
            if (ImGui.BeginCombo(label, displayName))
            {
                if (ImGui.Selectable("(None)", string.IsNullOrEmpty(currentPath)))
                {
                    setValue(null);
                    // Auto-save when texture is changed
                    SaveCurrentMaterial();
                }

                // Get all available textures
                if (TextureLibrary.Instance?.textures != null)
                {
                    foreach (var kv in TextureLibrary.Instance.textures)
                    {
                        string texName = kv.Key;
                        bool selected = currentPath != null && 
                            Path.GetFileNameWithoutExtension(currentPath) == texName;
                        
                        if (ImGui.Selectable(texName, selected))
                        {
                            // Store the full path for the texture
                            string texturePath = Path.Combine("Assets", "Textures", texName + ".png");
                            setValue(texturePath);
                            // Auto-save when texture is changed
                            SaveCurrentMaterial();
                        }
                    }
                }

                ImGui.EndCombo();
            }

            // Show texture preview if available
            if (!string.IsNullOrEmpty(currentPath))
            {
                string texName = Path.GetFileNameWithoutExtension(currentPath);
                if (TextureLibrary.Instance?.textures.TryGetValue(texName, out var texture) == true)
                {
                    float maxSize = 64f;
                    Vector2 originalSize = new(texture.Width, texture.Height);
                    Vector2 targetSize = originalSize;

                    if (originalSize.X > maxSize || originalSize.Y > maxSize)
                    {
                        float scaleX = maxSize / originalSize.X;
                        float scaleY = maxSize / originalSize.Y;
                        float scale = MathF.Min(scaleX, scaleY);
                        targetSize *= scale;
                    }

                    IntPtr imImage = ImGuiRenderer.Instance.BindTexture(texture);
                    ImGui.Image(imImage, targetSize, Vector2.Zero, Vector2.One);
                }
            }
        }
    }
}


