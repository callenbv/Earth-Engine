/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Program.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using System;
using System.IO;

namespace GameRuntime
{
    /// <summary>
    /// Robust program class that lets us launch runtimes with a given project path
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            string? projectPath = null;

            // 1. Check for CLI argument
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--project")
                {
                    projectPath = args[i + 1];
                    break;
                }
            }

#if DEBUG
            if (projectPath == null)
            {
                Console.WriteLine("No project specified. Drag your project folder here:");
                projectPath = Console.ReadLine()?.Trim('"');
            }
#endif
            projectPath ??= AppContext.BaseDirectory;
            Console.WriteLine($"Project root set to {projectPath}");

            // We defaut to our current directory, which should be the case for release builds
            if (projectPath == null)
            {
                // Get the directory of the executable
                string exeDir = AppContext.BaseDirectory;

                // Ensure this is the actual folder (remove trailing slash just in case)
                exeDir = exeDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                Console.WriteLine("Invalid or missing project path.");
                return;
            }

            string fullPath = Path.Combine(projectPath);

            Console.WriteLine($"[DEBUG] fullPath = {fullPath}");
            Console.WriteLine($"[DEBUG] File.Exists = {File.Exists(fullPath)}");

            try
            {
                var fi = new FileInfo(fullPath);
                Console.WriteLine($"[DEBUG] FileInfo.Exists = {fi.Exists}");
                Console.WriteLine($"[DEBUG] FileInfo.IsReadOnly = {fi.IsReadOnly}");
                Console.WriteLine($"[DEBUG] Attributes = {fi.Attributes}");
            }
            catch (Exception infoEx)
            {
                Console.WriteLine($"[DEBUG] FileInfo error: {infoEx}");
            }
            EnginePaths.ProjectBase = projectPath;

            using var game = new Runtime(projectPath);
            game.Run();
        }
    }
}

