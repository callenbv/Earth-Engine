using Engine.Core.Game.Components;
using Engine.Core.Scripting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Editor.AssetManagement
{
    public class EditorWatcher
    {
        private readonly FileSystemWatcher watcher;
        private readonly string projectPath;
        private DateTime lastTriggerTime = DateTime.MinValue;

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
