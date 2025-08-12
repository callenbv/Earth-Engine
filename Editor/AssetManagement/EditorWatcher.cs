/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         EditorWatcher.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Graphics;
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

            watcher = new FileSystemWatcher
            {
                Path = projectPath,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            watcher.Changed += (sender, e) =>
            {
                // Only reload if 500ms passed since last trigger
                var now = DateTime.Now;
                if ((now - lastTriggerTime).TotalMilliseconds < 500)
                    return;
                lastTriggerTime = now;

                Task.Delay(200).ContinueWith(_ =>
                {
                    string ext = Path.GetExtension(e.FullPath);

                    switch (ext)
                    {
                        case ".cs":
                            OnScriptDllChanged(sender, e);
                            break;

                        case ".png":
                            OnTextureChanged(sender, e);
                            break;
                    }
                });
            };
        }

        /// <summary>
        /// Handles the Changed event of the texture watcher and reloads textures if a texture was changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTextureChanged(object sender, FileSystemEventArgs e)
        {
            TextureLibrary.Instance.LoadTextures();
        }

        /// <summary>
        /// Handles the Changed event of the script DLL watcher and compiles scripts if code was changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnScriptDllChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("[EditorWatcher] Detected script DLL change. Reloading...");
            if (ScriptCompiler.CompileAndLoadScripts(projectPath, out var scriptManager))
                Console.WriteLine("[EditorWatcher] Reload succeeded.");
            else
                Console.WriteLine("[EditorWatcher] Reload failed.");
        }
    }
}

