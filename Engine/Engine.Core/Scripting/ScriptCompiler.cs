/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         ScriptCompiler.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Runtime.InteropServices;
using Engine.Core.Game.Components;
using GameRuntime;
using System.Reflection;

namespace Engine.Core.Scripting
{
    public class CompilationResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; } = new();
        public byte[]? CompiledAssembly { get; set; }
    }

    public static class ScriptCompiler
    {
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

            var syntaxTrees = scriptFiles.Select(file =>
                CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file)
            ).ToList();

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

        private static IEnumerable<MetadataReference> ResolveReferences()
        {
            var refs = new List<MetadataReference>();

            string? tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (!string.IsNullOrWhiteSpace(tpa))
            {
                foreach (var path in tpa.Split(Path.PathSeparator))
                {
                    // Only add relevant system/standard assemblies
                    if (path.Contains("System.") || path.Contains("Microsoft.") || path.Contains("netstandard"))
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
        public static bool CompileAndLoadScripts(string projectPath, out ScriptManager scriptManager)
        {
            scriptManager = null;

            string outputDll = Path.Combine(projectPath, "Build", "CompiledScripts.dll");

            // Step 1: Compile
            var compileResult = CompileAllScriptsInAssets(projectPath, outputDll);
            if (!compileResult.Success)
            {
                Console.WriteLine("[ScriptCompiler] Compilation failed!");
                foreach (var error in compileResult.Errors)
                    Console.WriteLine($"  - {error}");
                return false;
            }

            Console.WriteLine("[ScriptCompiler] Compilation succeeded.");

            // Step 2: Load DLL from memory
            try
            {
                byte[] assemblyBytes = File.ReadAllBytes(outputDll);
                Assembly scriptAssembly = Assembly.Load(assemblyBytes);

                // Step 3: Create script manager
                scriptManager = new ScriptManager(scriptAssembly);
                EngineContext.Current.ScriptManager = scriptManager;

                // Step 4: Register component types
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
    }
}

