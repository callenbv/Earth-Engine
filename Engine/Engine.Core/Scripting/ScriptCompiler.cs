/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ScriptCompiler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Engine.Core.Game.Components;
using GameRuntime;
using System.Reflection;
using Engine.Core.Data;

namespace Engine.Core.Scripting
{
    /// <summary>
    /// Represents the result of a script compilation process.
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// Indicates whether the compilation was successful or not.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// A list of errors encountered during the compilation process.
        /// </summary>
        public List<string> Errors { get; } = new();

        /// <summary>
        /// The compiled assembly as a byte array. This can be used to load the assembly into the application.
        /// </summary>
        public byte[]? CompiledAssembly { get; set; }
    }

    /// <summary>
    /// Compiles C# scripts found in the Assets directory of the project.
    /// </summary>
    public static class ScriptCompiler
    {
        /// <summary>
        /// Compiles all C# scripts found in the Assets directory of the specified project.
        /// </summary>
        /// <param name="projectDir"></param>
        /// <param name="outputDllPath"></param>
        /// <returns></returns>
        public static CompilationResult CompileAllScriptsInAssets(string projectDir, string outputDllPath)
        {
            var result = new CompilationResult();

            string assetsPath = Path.Combine(projectDir, "Assets");
            if (!Directory.Exists(assetsPath))
            {
                result.Success = true; // No assets directory = nothing to compile
                return result;
            }

            string[] scriptFiles = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);
            if (scriptFiles.Length == 0)
            {
                result.Success = true;
                return result;
            }

            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);

            var defaultUsings = string.Join("\n", new[]
            {
                "using System;"
            }) + "\n\n";

            var syntaxTrees = scriptFiles.Select(file =>
            {
                string userCode = File.ReadAllText(file);
                string fullCode = defaultUsings + userCode;

                return CSharpSyntaxTree.ParseText(
                    fullCode,
                    parseOptions,
                    path: file
                );
            }).ToList();

            var references = ResolveReferences();

            var compilation = CSharpCompilation.Create(
                "CompiledScripts",
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                result.Errors.AddRange(emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString()));
                result.Success = false;
                return result;
            }

            result.CompiledAssembly = ms.ToArray();
            result.Success = true;

            Directory.CreateDirectory(Path.GetDirectoryName(outputDllPath)!);
            File.WriteAllBytes(outputDllPath, result.CompiledAssembly);

            return result;
        }

        /// <summary>
        /// Resolves references to necessary assemblies for script compilation.
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<MetadataReference> ResolveReferences()
        {
            var refs = new List<MetadataReference>();

            string? tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (!string.IsNullOrWhiteSpace(tpa))
            {
                Console.WriteLine($"TRUSTED_PLATFORM_ASSEMBLIES = {tpa}");

                foreach (var path in tpa.Split(Path.PathSeparator))
                {
                    refs.Add(MetadataReference.CreateFromFile(path));
                }
            }

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            foreach (var localDll in new[] { "Engine.Core.dll", "MonoGame.Framework.dll" })
            {
                string full = Path.Combine(baseDir, localDll);
                if (File.Exists(full))
                    refs.Add(MetadataReference.CreateFromFile(full));
            }

            return refs;
        }

        /// <summary>
        /// Compiles and loads scripts 
        /// </summary>
        /// <param name="projectPath"></param>
        /// <param name="scriptManager"></param>
        /// <returns></returns>
        public static bool CompileAndLoadScripts(string projectPath, out ScriptManager? scriptManager)
        {
            scriptManager = null;

            string outputDll = Path.Combine(projectPath, "Build", "CompiledScripts.dll");

            // Compile the scripts
            var compileResult = CompileAllScriptsInAssets(projectPath, outputDll);
            if (!compileResult.Success)
            {
                Console.WriteLine("[ScriptCompiler] Compilation failed!");
                foreach (var error in compileResult.Errors)
                    Console.WriteLine($"  - {error}");
                return false;
            }

            Console.WriteLine("[ScriptCompiler] Compilation succeeded.");

            // Load the assembly
            try
            {
                byte[] assemblyBytes = File.ReadAllBytes(outputDll);
                Assembly scriptAssembly = Assembly.Load(assemblyBytes);

                scriptManager = new ScriptManager(scriptAssembly);
                EngineContext.Current.ScriptManager = scriptManager;

                ComponentRegistry.RefreshAll();
                foreach (var type in scriptAssembly.GetTypes())
                {
                    if (!type.IsAbstract && typeof(ObjectComponent).IsAssignableFrom(type))
                    {
                        ComponentRegistry.Register(type.Name, type);
                        Console.WriteLine($"[ComponentRegistry] Registered: {type.Name}");
                    }
                }

                Console.WriteLine("[ScriptLoader] ScriptManager initialized.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ScriptLoader] Failed to load scripts: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads the compiled scripts from the Build directory and updates the ComponentRegistry.
        /// </summary>
        /// <returns></returns>
        public static bool LoadScripts()
        {
            string scriptDllPath = Path.Combine(EnginePaths.ProjectBase, "Build", "CompiledScripts.dll");
            bool success;

            if (File.Exists(scriptDllPath))
            {
                Console.WriteLine($"[Runtime] Loading CompiledScripts.dll from: {scriptDllPath}");

                byte[] bytes = File.ReadAllBytes(scriptDllPath);
                Assembly asm = Assembly.Load(bytes);

                EngineContext.Current.ScriptManager = new ScriptManager(asm);
                ComponentRegistry.RegisterAllComponents();

                Console.WriteLine("[Runtime] ScriptManager initialized and components registered.");
                success = true;
            }
            else
            {
                Console.WriteLine("[Runtime] CompiledScripts.dll not found!");
                success = false;
            }

#if DEBUG
            foreach (var kvp in ComponentRegistry.Components)
                Console.WriteLine($"[Runtime] Registered: {kvp.Key}");
            string engineCorePath = Path.Combine(AppContext.BaseDirectory, "Engine.Core.dll");
            string buildPath = Path.Combine(EnginePaths.ProjectBase, "Build");
            string destPath = Path.Combine(buildPath, "Engine.Core.dll");

            if (File.Exists(engineCorePath))
            {
                Directory.CreateDirectory(buildPath); // Ensure Build exists
                File.Copy(engineCorePath, destPath, overwrite: true);
                Console.WriteLine($"[Launcher] Copied Engine.Core.dll to {destPath}");
            }
            else
            {
                Console.WriteLine("[Launcher] Engine.Core.dll not found at: " + engineCorePath);
            }
#endif

            return success;
        }
    }
}

