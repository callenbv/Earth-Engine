/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         EditorWatcher.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Scripting;
using System.IO;

namespace Editor.AssetManagement
{
    /// <summary>
    /// Watches for changes in the compiled script DLL and reloads scripts when modified.
    /// </summary>
    public class EditorWatcher
    {
        private readonly FileSystemWatcher watcher;
        private readonly string projectPath;
        private DateTime lastTriggerTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorWatcher"/> class.
        /// </summary>
        /// <param name="projectPath"></param>
        public EditorWatcher(string projectPath)
        {
            this.projectPath = projectPath;

            string buildPath = Path.Combine(projectPath, "Build");

            watcher = new FileSystemWatcher
            {
                Path = buildPath,
                Filter = "CompiledScripts.dll",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true,
            };

            watcher.Changed += OnScriptDllChanged;
        }

        /// <summary>
        /// Handles the Changed event of the script DLL watcher and compiles scripts if code was changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnScriptDllChanged(object sender, FileSystemEventArgs e)
        {
            // Simple debounce: only reload if 500ms passed since last trigger
            var now = DateTime.Now;
            if ((now - lastTriggerTime).TotalMilliseconds < 500)
                return;

            lastTriggerTime = now;

            Task.Delay(200).ContinueWith(_ =>
            {
                Console.WriteLine("[EditorWatcher] Detected script DLL change. Reloading...");
                if (ScriptCompiler.CompileAndLoadScripts(projectPath, out var scriptManager))
                    Console.WriteLine("[EditorWatcher] Reload succeeded.");
                else
                    Console.WriteLine("[EditorWatcher] Reload failed.");
            });
        }
    }
}

