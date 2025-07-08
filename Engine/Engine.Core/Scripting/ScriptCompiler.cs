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

        private static IEnumerable<MetadataReference> GetReferences()
        {
            var references = new List<MetadataReference>();

            // Add basic .NET references
            var assemblies = new[]
            {
                typeof(object).Assembly,
                typeof(Console).Assembly,
                typeof(System.Collections.Generic.List<>).Assembly,
                typeof(System.Linq.Enumerable).Assembly
            };

            foreach (var assembly in assemblies)
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }

            // Add System.Runtime.dll for Roslyn
            var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            var systemRuntimeDll = Path.Combine(runtimeDir, "System.Runtime.dll");
            if (File.Exists(systemRuntimeDll))
                references.Add(MetadataReference.CreateFromFile(systemRuntimeDll));

            // Add Engine.Core.dll and MonoGame.Framework.dll
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var engineCorePath = Path.Combine(baseDir, "Engine.Core.dll");
            var monogamePath = Path.Combine(baseDir, "MonoGame.Framework.dll");
            if (File.Exists(engineCorePath))
                references.Add(MetadataReference.CreateFromFile(engineCorePath));
            if (File.Exists(monogamePath))
                references.Add(MetadataReference.CreateFromFile(monogamePath));

            return references;
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
                            gameObject.sprite = new SpriteData();
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
                if (objDef.Scripts != null)
                {
                    foreach (var scriptName in objDef.Scripts.Values<string>())
                    {
                        try
                        {
                            // Use reflection to call CreateScriptInstanceByName on the scriptManager
                            var createMethod = scriptManager.GetType().GetMethod("CreateScriptInstanceByName");
                            if (createMethod != null)
                            {
                                var script = createMethod.Invoke(scriptManager, new object[] { scriptName }) as Engine.Core.GameScript;
                                if (script != null)
                                {
                                    script.Attach(gameObject);
                                    gameObject.scriptInstances.Add(script);
                                    Console.WriteLine($"Attached script '{scriptName}' to {objectName}");
                                }
                                else
                                {
                                    Console.WriteLine($"Failed to create script instance '{scriptName}' for {objectName}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"ScriptManager does not have CreateScriptInstanceByName method");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error attaching script '{scriptName}' to {objectName}: {ex.Message}");
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