using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Engine.Core.Game.Components;
using Engine.Core.Game;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;
using System.Reflection;
using Engine.Core.Graphics;
using Newtonsoft.Json.Linq;

namespace Engine.Core
{
    public class CompilationResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public byte[]? AssemblyBytes { get; set; }
    }

    public class ScriptCompiler
    {
        public CompilationResult CompileScripts(string scriptsDirectory, string outputDllPath)
        {
            var result = new CompilationResult();
            
            try
            {
                if (!Directory.Exists(scriptsDirectory))
                {
                    result.Success = true; // No scripts to compile
                    return result;
                }

                var scriptFiles = Directory.GetFiles(scriptsDirectory, "*.cs", SearchOption.AllDirectories);
                if (scriptFiles.Length == 0)
                {
                    result.Success = true; // No scripts to compile
                    return result;
                }

                var syntaxTrees = new List<SyntaxTree>();
                
                foreach (var scriptFile in scriptFiles)
                {
                    try
                    {
                        var sourceCode = File.ReadAllText(scriptFile);
                        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
                        syntaxTrees.Add(syntaxTree);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error parsing {Path.GetFileName(scriptFile)}: {ex.Message}");
                    }
                }

                if (result.Errors.Any())
                {
                    result.Success = false;
                    return result;
                }

                // Create compilation
                var compilation = CSharpCompilation.Create(
                    "GameScripts",
                    syntaxTrees,
                    GetReferences(),
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                );

                // Compile
                using var ms = new MemoryStream();
                var emitResult = compilation.Emit(ms);

                if (!emitResult.Success)
                {
                    result.Success = false;
                    result.Errors.AddRange(emitResult.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.GetMessage()));
                }
                else
                {
                    result.Success = true;
                    result.AssemblyBytes = ms.ToArray();
                    Directory.CreateDirectory(Path.GetDirectoryName(outputDllPath));
                    File.WriteAllBytes(outputDllPath, result.AssemblyBytes);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Compilation failed: {ex.Message}");
            }

            return result;
        }

        public (bool Success, List<string> Errors) CompileScriptsWithProgress(string scriptsDir, string outputDll, Action<int, int, string> progressCallback)
        {
            var errors = new List<string>();
            var scriptFiles = Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories);
            int total = scriptFiles.Length;
            int current = 0;
            foreach (var file in scriptFiles)
            {
                current++;
                progressCallback?.Invoke(current, total, file);
                // Optionally, you could compile each script individually here, or just simulate progress
                // For now, just wait a tiny bit to simulate work
                System.Threading.Thread.Sleep(30);
            }
            // Call the real compile method at the end
            var result = CompileScripts(scriptsDir, outputDll);
            return (result.Success, result.Errors);
        }

        private static IEnumerable<MetadataReference> GetReferences()
        {
            var refs = new List<MetadataReference>();

            string? tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            if (!string.IsNullOrEmpty(tpa))
            {
                foreach (var path in tpa.Split(Path.PathSeparator))
                {
                    // You can filter here if you want only a subset
                    refs.Add(MetadataReference.CreateFromFile(path));
                }
            }
            else
            {
                // Fallback: manually add a few key assemblies
                string rtDir = RuntimeEnvironment.GetRuntimeDirectory();
                string[] essentials =
                {
                    "System.Private.CoreLib.dll",
                    "System.Runtime.dll",
                    "System.Console.dll",
                    "System.Collections.dll",
                    "System.Linq.dll",
                    "netstandard.dll"
                };

                foreach (var dll in essentials)
                {
                    string full = Path.Combine(rtDir, dll);
                    if (File.Exists(full))
                        refs.Add(MetadataReference.CreateFromFile(full));
                }
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            foreach (var localDll in new[]
                     {
                 "Engine.Core.dll",
                 "MonoGame.Framework.dll"
             })
            {
                string full = Path.Combine(baseDir, localDll);
                if (File.Exists(full))
                    refs.Add(MetadataReference.CreateFromFile(full));
            }

            return refs;
        }

        /// <summary>
        /// Load texture and attach scripts to a GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject to configure</param>
        /// <param name="objectName">Name of the object definition</param>
        /// <param name="content">ContentManager for loading textures</param>
        /// <param name="scriptManager">ScriptManager for creating scripts</param>
        /// <param name="graphicsDevice">GraphicsDevice for loading textures</param>
        public static void LoadTextureAndScripts(GameObject gameObject, string objectName, string assetsRoot, ContentManager content, object scriptManager, GraphicsDevice graphicsDevice)
        {
            try
            {
                // Get the object definition
                var objDef = Engine.Core.Game.GameObjectRegistry.Get(objectName);

                // Load texture
                if (!string.IsNullOrEmpty(objDef.Sprite))
                {
                    try
                    {
                        // Look for texture in the Sprites directory relative to the assets root
                        var spritePath = Path.Combine(assetsRoot, "Sprites", objDef.Sprite);
                        if (File.Exists(spritePath))
                        {
                            // Load texture directly from file using GraphicsDevice
                            SpriteData sprite = new SpriteData();
                            gameObject.sprite = sprite;
                            gameObject.sprite.texture = Texture2D.FromFile(graphicsDevice, spritePath);

                            // Try to load sprite definition from .sprite file
                            var spriteDefPath = Path.Combine(assetsRoot, "Sprites", Path.GetFileNameWithoutExtension(objDef.Sprite) + ".sprite");
                            if (File.Exists(spriteDefPath))
                            {
                                try
                                {
                                    var spriteJson = File.ReadAllText(spriteDefPath);
                                    var spriteDef = JsonSerializer.Deserialize<SpriteData>(spriteJson);

                                    if (spriteDef != null)
                                    {
                                        gameObject.sprite.frameWidth = spriteDef.frameWidth;
                                        gameObject.sprite.frameHeight = spriteDef.frameHeight;
                                        gameObject.sprite.frameCount = spriteDef.frameCount;
                                        gameObject.sprite.frameSpeed = spriteDef.frameSpeed;
                                        gameObject.sprite.animated = spriteDef.animated;

                                        Console.WriteLine($"Loaded sprite definition for {objectName}: {spriteDef.frameWidth}x{spriteDef.frameHeight}, {spriteDef.frameCount} frames, animated: {spriteDef.animated}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to load sprite definition for {objectName}: {ex.Message}");
                                }
                            }
                            else
                            {
                                // Fallback: set default values if no .sprite file exists
                                gameObject.sprite.frameWidth = gameObject.sprite.texture.Width;
                                gameObject.sprite.frameHeight = gameObject.sprite.texture.Height;
                                gameObject.sprite.frameCount = 1;
                                gameObject.sprite.frameSpeed = 1;
                                gameObject.sprite.animated = false;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Texture file not found: {spritePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load texture '{objDef.Sprite}' for {objectName}: {ex.Message}");
                    }
                }

                // Attach scripts
                if (objDef.Components != null)
                {

                    foreach (var scriptName in objDef.Components.Values<string>())
                    {
                        try
                        {
                            // Use reflection to call CreateComponentInstanceByName on the scriptManager
                            var createMethod = scriptManager.GetType().GetMethod("CreateComponentInstanceByName");
                            if (createMethod != null)
                            {
                                var component = createMethod.Invoke(scriptManager, new object[] { scriptName }) as ObjectComponent;
                                if (component != null)
                                {
                                    // Set properties as before
                                    var scriptPropsObj = gameObject.componentProperties;
                                    var componentType = component.GetType();

                                    if (scriptPropsObj.TryGetValue(scriptName, out var props))
                                    {
                                        foreach (var prop in props)
                                        {
                                            var field = componentType.GetField(prop.Key, BindingFlags.Public | BindingFlags.Instance);
                                            if (field != null)
                                            {
                                                try
                                                {
                                                    object value;
                                                    
                                                    // Handle special types that are stored as JSON objects
                                                    if (field.FieldType == typeof(Microsoft.Xna.Framework.Color))
                                                    {
                                                        // Color is stored as JSON object with R, G, B, A properties
                                                        if (prop.Value is JObject colorObj)
                                                        {
                                                            byte r = colorObj["R"]?.Value<byte>() ?? 255;
                                                            byte g = colorObj["G"]?.Value<byte>() ?? 255;
                                                            byte b = colorObj["B"]?.Value<byte>() ?? 255;
                                                            byte a = colorObj["A"]?.Value<byte>() ?? 255;
                                                            value = new Microsoft.Xna.Framework.Color(r, g, b, a);
                                                        }
                                                        else
                                                        {
                                                            value = Convert.ChangeType(prop.Value, field.FieldType);
                                                        }
                                                    }
                                                    else if (field.FieldType == typeof(Microsoft.Xna.Framework.Vector2))
                                                    {
                                                        // Vector2 is stored as JSON object with X, Y properties
                                                        if (prop.Value is JObject vectorObj)
                                                        {
                                                            float x = vectorObj["X"]?.Value<float>() ?? 0f;
                                                            float y = vectorObj["Y"]?.Value<float>() ?? 0f;
                                                            value = new Microsoft.Xna.Framework.Vector2(x, y);
                                                        }
                                                        else
                                                        {
                                                            value = Convert.ChangeType(prop.Value, field.FieldType);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // For other types, use the standard conversion
                                                        value = Convert.ChangeType(prop.Value, field.FieldType);
                                                    }
                                                    
                                                    field.SetValue(component, value);
                                                    Console.WriteLine($"Set {scriptName}.{prop.Key} = {value}");
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine($"Failed to set {scriptName}.{prop.Key}: {ex.Message}");
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine($"Field '{prop.Key}' not found on component '{scriptName}'");
                                            }
                                        }
                                    }

                                    // Add the component to the game object
                                    gameObject.AddComponent(component);
                                    // If it's a GameScript, also add to scriptInstances
                                    if (component is GameScript gs)
                                        gameObject.scriptInstances.Add(gs);

                                    Console.WriteLine($"Attached component '{scriptName}' to {objectName}");
                                }
                                else
                                {
                                    Console.WriteLine($"Failed to create component instance '{scriptName}' for {objectName}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"ScriptManager does not have CreateComponentInstanceByName method");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error attaching component '{scriptName}' to {objectName}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading texture and scripts for {objectName}: {ex.Message}");
            }
        }
    }
} 