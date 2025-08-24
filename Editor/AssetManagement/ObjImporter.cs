/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ObjImporter.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>      Simple .obj + .mtl -> .mesh/.mat converter for editor imports.          
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using System.Globalization;
using System.Text.Json;
using System.IO;
using EarthEngineEditor.Windows;

namespace Editor.AssetManagement
{
    public static class ObjImporter
    {
        public static void ConvertObjToMeshAndMaterial(string objFullPath, string? targetFolder = null)
        {
            try
            {
                if (!File.Exists(objFullPath)) return;

                string assetsDir = ProjectSettings.AssetsDirectory;
                string fileNameNoExt = Path.GetFileNameWithoutExtension(objFullPath);
                
                // Use specified target folder, or default to "Models" for backward compatibility
                if (string.IsNullOrEmpty(targetFolder))
                    targetFolder = Path.Combine(assetsDir, "Models");
                
                Directory.CreateDirectory(targetFolder);

                string meshOut = Path.Combine(targetFolder, fileNameNoExt + ".mesh");
                string matOut = Path.Combine(targetFolder, fileNameNoExt + ".mat");

                var mesh = ParseObj(objFullPath, out string? mtlPath);
                if (!string.IsNullOrEmpty(mtlPath))
                {
                    string resolvedMtlPath = Path.IsPathRooted(mtlPath) ? mtlPath : Path.Combine(Path.GetDirectoryName(objFullPath)!, mtlPath);
                    var mat = ParseMtl(resolvedMtlPath);
                    WriteJson(matOut, mat);
                    mesh.MaterialPath = ProjectSettings.NormalizePath(Path.GetRelativePath(assetsDir, matOut));
                }

                WriteJson(meshOut, mesh);

                Console.WriteLine($"[OBJ] Converted {objFullPath} -> {meshOut} / {matOut}");
                
                // Reload mesh library to include new assets
                Engine.Core.Graphics.MeshLibrary.LoadAll();
                
                ProjectWindow.Instance.RefreshItems();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OBJ] Failed to convert: {ex.Message}");
            }
        }

        // TODO: Implement FBX importer using AssimpNet if available. For now, leave hook ready.
        public static void ConvertFbxToMeshAndMaterial(string fbxFullPath)
        {
            Console.WriteLine("[FBX] Import not yet implemented. Consider adding AssimpNet dependency.");
        }

        private static void WriteJson<T>(string path, T data)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(path, json);
        }

        private static MeshData ParseObj(string path, out string? mtlLib)
        {
            mtlLib = null;
            List<Vector3> positions = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> indices = new();

            var positionOut = new List<float>();
            var normalOut = new List<float>();
            var uvOut = new List<float>();

            // Map unique vertex tuple to final index
            Dictionary<(int pi,int ti,int ni), int> vertexMap = new();

            var culture = CultureInfo.InvariantCulture;
            foreach (var raw in File.ReadLines(path))
            {
                string line = raw.Trim();
                if (line.StartsWith("#") || line.Length == 0) continue;

                if (line.StartsWith("mtllib "))
                {
                    mtlLib = line.Substring(7).Trim();
                }
                else if (line.StartsWith("v "))
                {
                    var parts = SplitParts(line, 4);
                    positions.Add(new Vector3(
                        float.Parse(parts[1], culture),
                        float.Parse(parts[2], culture),
                        float.Parse(parts[3], culture)));
                }
                else if (line.StartsWith("vn "))
                {
                    var parts = SplitParts(line, 4);
                    normals.Add(new Vector3(
                        float.Parse(parts[1], culture),
                        float.Parse(parts[2], culture),
                        float.Parse(parts[3], culture)));
                }
                else if (line.StartsWith("vt "))
                {
                    var parts = SplitParts(line, 3);
                    uvs.Add(new Vector2(
                        float.Parse(parts[1], culture),
                        1f - float.Parse(parts[2], culture)));
                }
                else if (line.StartsWith("f "))
                {
                    var parts = line.Substring(2).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 3) continue;

                    int[] face = new int[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        var tuple = parts[i].Split('/');
                        int pi = ParseIndex(tuple, 0, positions.Count);
                        int ti = ParseIndex(tuple, 1, uvs.Count);
                        int ni = ParseIndex(tuple, 2, normals.Count);

                        var key = (pi, ti, ni);
                        if (!vertexMap.TryGetValue(key, out int finalIndex))
                        {
                            var p = positions[pi];
                            positionOut.Add(p.X); positionOut.Add(p.Y); positionOut.Add(p.Z);

                            if (ni >= 0)
                            {
                                var n = normals[ni];
                                normalOut.Add(n.X); normalOut.Add(n.Y); normalOut.Add(n.Z);
                            }
                            else { normalOut.Add(0); normalOut.Add(1); normalOut.Add(0); }

                            if (ti >= 0)
                            {
                                var t = uvs[ti];
                                uvOut.Add(t.X); uvOut.Add(t.Y);
                            }
                            else { uvOut.Add(0); uvOut.Add(0); }

                            finalIndex = vertexMap[key] = (positionOut.Count / 3) - 1;
                        }
                        face[i] = finalIndex;
                    }

                    for (int i = 1; i < face.Length - 1; i++)
                    {
                        indices.Add(face[0]);
                        indices.Add(face[i]);
                        indices.Add(face[i + 1]);
                    }
                }
            }

            var mesh = new MeshData
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Positions = positionOut.ToArray(),
                Normals = normalOut.ToArray(),
                UV0 = uvOut.ToArray(),
                Indices = indices.ToArray()
            };

            // Compute bounds
            if (mesh.Positions.Length >= 3)
            {
                float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
                float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;
                for (int i = 0; i < mesh.Positions.Length; i += 3)
                {
                    float x = mesh.Positions[i+0];
                    float y = mesh.Positions[i+1];
                    float z = mesh.Positions[i+2];
                    if (x < minX) minX = x; if (y < minY) minY = y; if (z < minZ) minZ = z;
                    if (x > maxX) maxX = x; if (y > maxY) maxY = y; if (z > maxZ) maxZ = z;
                }
                mesh.Bounds = new BoundingBox { MinX = minX, MinY = minY, MinZ = minZ, MaxX = maxX, MaxY = maxY, MaxZ = maxZ };
            }

            return mesh;
        }

        private static MaterialData ParseMtl(string path)
        {
            var mat = new MaterialData();
            if (!File.Exists(path)) return mat;

            foreach (var raw in File.ReadLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (line.StartsWith("newmtl "))
                {
                    mat.Name = line.Substring(7).Trim();
                }
                else if (line.StartsWith("Kd "))
                {
                    var parts = SplitParts(line, 4);
                    mat.AlbedoColor = new float[]
                    {
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture),
                        1f
                    };
                }
                else if (line.StartsWith("Ns "))
                {
                    float ns = float.Parse(line.Substring(3), CultureInfo.InvariantCulture);
                    mat.Specular = ns / 1000f;
                }
                else if (line.StartsWith("map_Kd "))
                {
                    mat.AlbedoTexture = CopyIntoAssets(path, line.Substring(7).Trim());
                }
                else if (line.StartsWith("map_Bump ") || line.StartsWith("bump "))
                {
                    string tex = line.Contains(' ')? line[(line.IndexOf(' ')+1)..].Trim() : string.Empty;
                    mat.NormalTexture = CopyIntoAssets(path, tex);
                }
            }

            return mat;
        }

        private static string? CopyIntoAssets(string mtlPath, string textureRelative)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(textureRelative)) return null;
                string src = Path.Combine(Path.GetDirectoryName(mtlPath)!, textureRelative);
                if (!File.Exists(src)) return null;
                string destFolder = Path.Combine(ProjectSettings.AssetsDirectory, "Textures");
                Directory.CreateDirectory(destFolder);
                string dest = Path.Combine(destFolder, Path.GetFileName(src));
                File.Copy(src, dest, overwrite: true);
                return ProjectSettings.NormalizePath(Path.GetRelativePath(ProjectSettings.AssetsDirectory, dest));
            }
            catch { return null; }
        }

        private static string[] SplitParts(string line, int expected)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < expected) throw new FormatException($"Invalid line: {line}");
            return parts;
        }

        private static int ParseIndex(string[] tuple, int idx, int count)
        {
            if (idx >= tuple.Length || string.IsNullOrEmpty(tuple[idx])) return -1;
            int i = int.Parse(tuple[idx], CultureInfo.InvariantCulture);
            if (i < 0) i = count + i; else i = i - 1;
            return i;
        }

        private readonly struct Vector2
        {
            public readonly float X;
            public readonly float Y;
            public Vector2(float x, float y) { X = x; Y = y; }
        }

        private readonly struct Vector3
        {
            public readonly float X;
            public readonly float Y;
            public readonly float Z;
            public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
        }
    }
}


