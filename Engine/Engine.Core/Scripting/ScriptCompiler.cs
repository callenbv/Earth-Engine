using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        public CompilationResult CompileScripts(string scriptsDirectory)
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

                    // Write DLL to Editor/bin/Scripts
                    var editorBin = Path.GetFullPath(Path.Combine(scriptsDirectory, "..", "..", "bin", "Scripts"));
                    Directory.CreateDirectory(editorBin);
                    var dllPath = Path.Combine(editorBin, "GameScripts.dll");
                    File.WriteAllBytes(dllPath, result.AssemblyBytes);
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
    }
} 