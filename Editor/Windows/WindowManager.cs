/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         WindowManager.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Editor.AssetManagement;
using Editor.Windows.TileEditor;
using Engine.Core;
using Engine.Core.Data;
using Engine.Core.Rooms;
using Engine.Core.Scripting;
using GameRuntime;
using ImGuiNET;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace EarthEngineEditor.Windows
{
    /// <summary>
    /// Manages all editor windows and their interactions, including project management, game settings, and build/export functionality.
    /// </summary>
    public class WindowManager
    {
        private readonly SceneViewWindow _sceneView;
        private readonly InspectorWindow _inspector;
        private readonly ProjectWindow _project;
        private readonly AboutWindow _about;
        private readonly PerformanceWindow _performance;
        private readonly ConsoleWindow _console;
        private readonly ToolbarWindow toolbar;
        public TileEditorWindow tileEditor;
        private EarthProject project;
        private EditorApp game;
        public List<string> recentProjects = new List<string>();
        private bool _openSettingsPopup = false;
        private bool openBuildPopup = false;
        private bool openEditorSettings = false;
        private bool showNewProject = false;
        private static string exportPath = "";
        private static int selectedTargetIndex = 0;
        private string projectName = String.Empty;
        private static readonly string[] targets = new[] { "linux-arm64", "linux-x64", "win-x64"};

        /// <summary>
        /// WindowManager constructor initializes all editor windows and sets up the project.
        /// </summary>
        /// <param name="game_"></param>
        /// <param name="console"></param>
        public WindowManager(EditorApp game_, ConsoleWindow console)
        {
            _sceneView = new SceneViewWindow();
            _inspector = new InspectorWindow();
            _project = new ProjectWindow();
            project = new EarthProject();
            _about = new AboutWindow();
            _performance = new PerformanceWindow();
            toolbar = new ToolbarWindow();
            tileEditor = new TileEditorWindow();
            _console = console;
            game = game_;
            Load();
        }

        /// <summary>
        /// Update the performance metrics based on the frame time
        /// </summary>
        /// <param name="frameTime"></param>
        public void UpdatePerformance(double frameTime)
        {
            _performance.Update(frameTime);
        }

        /// <summary>
        /// Render all windows in ImGui
        /// </summary>
        public void RenderAll()
        {
            _sceneView.Render();
            _inspector.Render();
            _project.Render();
            _about.Render();
            _performance.Render();
            _console.Render();
            tileEditor.Render();
            toolbar.Render();
        }

        /// <summary>
        /// Load all recent projects
        /// </summary>
        public void Load()
        {
            if (!File.Exists(ProjectSettings.RecentProjects))
            {
                File.Create(ProjectSettings.RecentProjects);
            }
            else
            {
                var files = File.ReadAllLines(ProjectSettings.RecentProjects).ToList();
                recentProjects = files;
            }
        }

        /// <summary>
        /// Returns the last opened project, useful for a lot of things
        /// </summary>
        /// <returns></returns>
        public string? GetLastProject()
        {
            string? recentProject = recentProjects.FirstOrDefault();

            if (!File.Exists(recentProject))
            {
                recentProject = null;
            }

            return recentProject;
        }

        /// <summary>
        /// Check for hotkeys
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Save project
            if (Input.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl))
            {
                if (Input.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.S))
                {
                    SaveProject();
                }
            }
        }

        /// <summary>
        /// Copy directory to a source destination
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="destinationDir"></param>
        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destinationDir));
            }

            foreach (var filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string destPath = filePath.Replace(sourceDir, destinationDir);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                File.Copy(filePath, destPath, overwrite: true);
            }
        }

        /// <summary>
        /// Render the top menu bar (file, etc.)
        /// </summary>
        public void RenderMenuBar()
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New Project"))
                    {
                        showNewProject = true;
                    }

                    if (ImGui.MenuItem("Open Project"))
                    {
                        SelectProject();
                    }
                    if (ImGui.MenuItem("Save Project"))
                    {
                        SaveProject();
                    }
                    if (ImGui.MenuItem("Show In File Explorer"))
                    {
                        if (Directory.Exists(ProjectSettings.AbsoluteProjectPath))
                        {
                            Process.Start("explorer.exe", $"\"{ProjectSettings.AbsoluteProjectPath}\"");
                        }
                    }
                    if (ImGui.MenuItem("Open Visual Studio"))
                    {
                        if (Directory.Exists(ProjectSettings.AbsoluteProjectPath))
                        {
                            var csprojPath = Directory.GetFiles(ProjectSettings.AbsoluteProjectPath, "*.csproj").FirstOrDefault();

                            if (csprojPath != null)
                            {
                                ProcessStartInfo psi = new()
                                {
                                    FileName = csprojPath,
                                    UseShellExecute = true // needed to open with default associated app (Visual Studio)
                                };

                                Process.Start(psi);
                            }
                            else
                            {
                                Console.WriteLine("[DEBUG] No .csproj file found in: " + ProjectSettings.ProjectDirectory);
                            }
                        }
                    }
                    if (ImGui.MenuItem("Exit"))
                    {
                        EditorApp.Instance.Exit();
                    }
                    ImGui.EndMenu();
                }
                
                if (ImGui.BeginMenu("Window"))
                {
                    bool sceneVisible = _sceneView.IsVisible;
                    if (ImGui.MenuItem("Scene View", null, ref sceneVisible))
                        _sceneView.SetVisible(sceneVisible);
                    
                    bool inspectorVisible = _inspector.IsVisible;
                    if (ImGui.MenuItem("Inspector", null, ref inspectorVisible))
                        _inspector.SetVisible(inspectorVisible);
                    
                    bool projectVisible = _project.IsVisible;
                    if (ImGui.MenuItem("Project", null, ref projectVisible))
                        _project.SetVisible(projectVisible);
                    
                    bool consoleVisible = _console.IsVisible;
                    if (ImGui.MenuItem("Console", null, ref consoleVisible))
                        _console.SetVisible(consoleVisible);
                    
                    bool performanceVisible = _performance.IsVisible;
                    if (ImGui.MenuItem("Performance", null, ref performanceVisible))
                        _performance.SetVisible(performanceVisible);
                    
                    bool aboutVisible = _about.IsVisible;
                    if (ImGui.MenuItem("About", null, ref aboutVisible))
                        _about.SetVisible(aboutVisible);

                    bool toolbarVisible = toolbar.IsVisible;
                    if (ImGui.MenuItem("Toolbar", null, ref toolbarVisible))
                        toolbar.SetVisible(toolbarVisible);

                    bool tileVisible = tileEditor.IsVisible;
                    if (ImGui.MenuItem("Tile Editor", null, ref tileVisible))
                        toolbar.SetVisible(tileVisible);

                    if (ImGui.MenuItem("Reset"))
                    {
                        _sceneView.SetVisible(true);
                        _inspector.SetVisible(true);
                        _project.SetVisible(true);
                        _console.SetVisible(true);
                        _performance.SetVisible(true);
                        _about.SetVisible(false);
                        toolbar.SetVisible(true);
                    }

                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Project"))
                {
                    if (ImGui.MenuItem("Game Settings"))
                    {
                        _openSettingsPopup = true;
                    }
                    if (ImGui.MenuItem("Editor Settings"))
                    {
                        openEditorSettings = true;
                    }
                    if (ImGui.MenuItem("Audio Settings"))
                    {
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Build"))
                {
                    if (ImGui.MenuItem("Export"))
                    {
                        openBuildPopup = true;
                    }
                    ImGui.EndMenu();
                }
                
                if (_openSettingsPopup)
                    ImGui.OpenPopup("Game Settings");

                if (openBuildPopup)
                    ImGui.OpenPopup("Export Game");

                if (openEditorSettings)
                    ImGui.OpenPopup("Editor Settings");

                // Change editor settings
                if (ImGui.BeginPopupModal("Editor Settings", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    int width = EditorApp.Instance._settings.WindowWidth;
                    if (ImGui.InputInt("Window Width", ref width))
                        EditorApp.Instance._settings.WindowWidth = width;

                    int height = EditorApp.Instance._settings.WindowHeight;
                    if (ImGui.InputInt("Window Height", ref height))
                        EditorApp.Instance._settings.WindowHeight = height;

                    bool editor = EditorApp.Instance._settings.PlayInEditor;
                    if (ImGui.Checkbox("Play In Editor", ref editor))
                        EditorApp.Instance._settings.PlayInEditor = editor;
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("If the editor launches a new instance of the runtime on play");

                    bool restartOnPlay = EditorApp.Instance._settings.RestartOnPlay;
                    if (ImGui.Checkbox("Live Edit", ref restartOnPlay))
                        EditorApp.Instance._settings.RestartOnPlay = restartOnPlay;
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("[WARNING] Scene will not reset when the game stops, and will save the modified state of the game. Use with caution");

                    if (ImGui.Button("Save"))
                    {
                        project.Save();
                        EditorSettings.Load();
                        ImGui.CloseCurrentPopup();
                        openEditorSettings = false;
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                        openEditorSettings = false;
                    }
                    ImGui.EndPopup();
                }

                // Export the game
                if (ImGui.BeginPopupModal("Export Game", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    EditorApp.Instance.gameFocused = false;

                    ImGui.Text("Select target platform:");
                    ImGui.Combo("##TargetPlatform", ref selectedTargetIndex, targets, targets.Length);

                    ImGui.InputText("Export Path", ref exportPath, 512);
                    ImGui.SameLine();
                    if (ImGui.Button("Browse"))
                    {
                        using var dialog = new FolderBrowserDialog();
                        dialog.Description = "Choose export folder";
                        dialog.UseDescriptionForTitle = true;

                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            exportPath = dialog.SelectedPath;
                        }
                    }

                    if (ImGui.Button("Export") && !string.IsNullOrWhiteSpace(exportPath))
                    {
                        ImGui.CloseCurrentPopup();
                        openBuildPopup = false;

                        string target = targets[selectedTargetIndex];
                        string projectDir = ProjectSettings.ProjectDirectory;
                        string runtimePath = Path.GetFullPath(Path.Combine(ProjectSettings.RuntimePath, "..", "..", "..", ".."));
                        string runtimeCsproj = Path.Combine(runtimePath, "GameRuntime.csproj");

                        Directory.CreateDirectory(exportPath);
                        string extraProps = "";
                        if (target.StartsWith("win"))
                        {
                            extraProps = "-p:UseWindowsForms=true -p:UseWPF=true";
                        }

                        var psi = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"publish \"{runtimeCsproj}\" -c Release -r {target} --self-contained true {extraProps} -o \"{exportPath}\"",
                            WorkingDirectory = runtimePath,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        try
                        {
                            using var process = Process.Start(psi);
                            string output = process.StandardOutput.ReadToEnd();
                            string error = process.StandardError.ReadToEnd();
                            process.WaitForExit();

                            // Copy over assets and game options to the export directory
                            try
                            {
                                // Assets
                                string exportAssets = Path.Combine(exportPath, "Assets");
                                CopyDirectory(ProjectSettings.AssetsDirectory, exportAssets);

                                // Tilemaps
                                string exportTilemap = Path.Combine(exportPath, "Tilemaps");
                                string tilemapDir = Path.Combine(ProjectSettings.ProjectDirectory, "Tilemaps");
                                CopyDirectory(tilemapDir, exportTilemap);

                                // Game Options
                                string optionsFileDest = Path.Combine(exportPath, Path.GetFileName(project.optionsPath));
                                File.Copy(project.optionsPath, optionsFileDest, overwrite: true);

                                // Compiled DLLs (Build Folder)
                                CopyDirectory(ProjectSettings.BuildPath, Path.Combine(exportPath, "Build"));

                                // Success
                                Console.WriteLine("[Export] Copied data to build output.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Export ERROR] Failed to copy assets: {ex.Message}");
                            }

                            // Create ZIP archive
                            try
                            {
                                string zipFileName = $"{game.runtime.gameOptions.Title}.zip"; // target might be "win-x64", etc.
                                string zipPath = Path.Combine(Path.GetDirectoryName(exportPath)!, zipFileName);

                                if (File.Exists(zipPath))
                                    File.Delete(zipPath); // Overwrite existing zip if present

                                ZipFile.CreateFromDirectory(exportPath, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
                                Console.WriteLine($"[Export] Zipped build to: {zipPath}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Export ERROR] Failed to zip folder: {ex.Message}");
                            }

                            Console.WriteLine($"[Export] Build for {target} complete:\n{output}");
                            if (!string.IsNullOrWhiteSpace(error))
                                Console.WriteLine($"[Export ERROR] {error}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Export ERROR] Failed to build: {ex.Message}");
                        }
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                        openBuildPopup = false;
                    }

                    ImGui.EndPopup();
                }

                // Edit our game options
                var settings = game.runtime.gameOptions;

                if (ImGui.BeginPopupModal("Game Settings", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    EditorApp.Instance.gameFocused = false;

                    string title = settings.Title ?? "";
                    if (ImGui.InputText("Game Title", ref title, 100))
                        settings.Title = title;

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Name of the game window");

                    ImGui.NewLine();

                    int width = settings.WindowWidth;
                    if (ImGui.InputInt("Window Width", ref width))
                        settings.WindowWidth = Math.Max(384, width);

                    int height = settings.WindowHeight;
                    if (ImGui.InputInt("Window Height", ref height))
                        settings.WindowHeight = Math.Max(216, height);

                    int internalWidth = settings.TargetResolutionWidth;
                    if (ImGui.InputInt("Target Render Width", ref internalWidth))
                        settings.TargetResolutionWidth = Math.Max(384, internalWidth);

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("The internal width used for rendering. Higher is more detailed");

                    int internalHeight = settings.TargetResolutionHeight;
                    if (ImGui.InputInt("Target Render Height", ref internalHeight))
                        settings.TargetResolutionHeight = Math.Max(216, internalHeight);

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("The internal height used for rendering. Higher is more detailed");

                    ImGui.NewLine();

                    bool fullscreen = settings.Fullscreen;
                    if (ImGui.Checkbox("Start in Fullscreen", ref fullscreen))
                        settings.Fullscreen = fullscreen;

                    bool canResize = settings.CanResizeWindow;
                    if (ImGui.Checkbox("Can re-size Window", ref canResize))
                        settings.CanResizeWindow = canResize;

                    bool vsync = settings.VerticalSync;
                    if (ImGui.Checkbox("Vsync", ref vsync))
                        settings.VerticalSync = vsync;

                    bool fixedTimestep = settings.FixedTimestep;
                    if (ImGui.Checkbox("Lock FPS", ref fixedTimestep))
                        settings.FixedTimestep = fixedTimestep;
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Caps the target frame rate to the target FPS (60 by default)");

                    int targetFPS = settings.TargetFPS;
                    if (ImGui.InputInt("Target FPS", ref targetFPS))
                        settings.TargetFPS = Math.Clamp(settings.TargetFPS, 30, 144);
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("The FPS the game will try to run at");

                    ImGui.NewLine();
                    
                    // Units Per Pixel setting
                    ImGui.Separator();
                    ImGui.Text("3D Rendering Settings");
                    
                    float unitsPerPixel = EngineContext.UnitsPerPixel;
                    if (ImGui.SliderFloat("Units Per Pixel", ref unitsPerPixel, 0.1f, 10f, "%.2f"))
                    {
                        EngineContext.UnitsPerPixel = unitsPerPixel;
                        // Also update the game options so it gets saved
                        game.runtime.gameOptions.UnitsPerPixel = unitsPerPixel;
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Controls the scale conversion from 2D pixels to 3D world units.\nHigher values = smaller 3D objects relative to 2D coordinates.");
                    }

                    ImGui.NewLine();

                    if (ImGui.Button("Save"))
                    {
                        project.Save();
                        ImGui.CloseCurrentPopup();
                        _openSettingsPopup = false;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                        _openSettingsPopup = false;
                    }

                    ImGui.EndPopup();
                }

                // Create a new project
                if (showNewProject)
                    ImGui.OpenPopup("Create New Project");

                if (ImGui.BeginPopupModal("Create New Project", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("Project Name");
                    ImGui.InputText("##ProjectTitle", ref projectName, 20);

                    if (ImGui.MenuItem("2D Game"))
                    {
                        if (CreateNewProject(projectName, ProjectType.Project2D))
                        {
                            showNewProject = false;
                        }
                    }
                    if (ImGui.MenuItem("3D Game"))
                    {
                        if (CreateNewProject(projectName, ProjectType.Project3D))
                        {
                            showNewProject = false;
                        }
                    }
                    ImGui.NewLine();

                    if (ImGui.Button("Cancel"))
                    {
                        ImGui.CloseCurrentPopup();
                        showNewProject = false;
                    }
                    ImGui.EndPopup();
                }

                ImGui.EndMainMenuBar();
            }
        }

        /// <summary>
        /// Called when we create a new project
        /// </summary>
        public bool CreateNewProject(string name, ProjectType projectType = ProjectType.Project2D)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select the folder where you want to create the project";
                folderBrowserDialog.ShowNewFolderButton = true;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    var projectPath = folderBrowserDialog.SelectedPath;
                    var projectName = name;
                    var projectFolder = Path.Combine(projectPath, projectName);
                    var buildFolder = Path.Combine(projectFolder, "Build");
                    var tilemapFolder = Path.Combine(projectFolder, "Tilemaps");
                    var projectFile = Path.Combine(projectFolder, $"{projectName}.earthproj");
                    var assetsFolder = Path.Combine(projectFolder, "Assets");
                    var csprojFile = Path.Combine(projectFolder, $"{projectName}.csproj");
                    try
                    {
                        // Create project structure
                        Directory.CreateDirectory(projectFolder);
                        Directory.CreateDirectory(assetsFolder);
                        Directory.CreateDirectory(buildFolder);
                        Directory.CreateDirectory(tilemapFolder);

                        // Create project file
                        var projectContent = $@"{{
                          ""name"": ""{projectName}"",
                          ""scene"": """",
                          ""version"": ""1.0.0"",
                          ""created"": ""{DateTime.Now:yyyy-MM-dd HH:mm:ss}""
                        }}";

                        var csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include=""..\\..\\Engine\\Engine.Core\\Engine.Core.csproj"" />
    <Reference Include=""Engine.Core"">
      <HintPath>Engine.Core.dll</HintPath>
      <Private>true</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include=""MonoGame.Framework.DesktopGL"" Version=""3.8.3"" />
    <PackageReference Include=""ImGui.NET"" Version=""1.91.6.1"" />
  </ItemGroup>

</Project>";

                        // Create the csproj and project file
                        File.WriteAllText(csprojFile, csprojContent);
                        File.WriteAllText(projectFile, projectContent);

                        Console.WriteLine($"Created new project: {projectFile}");

                        // Open the newly created project
                        CloseProject();
                        project.settings.Title = projectName;
                        project.Name = projectName;
                        project.ProjectType = projectType;
                        OpenProject(projectFile);

                        // Create a default scene file
                        var roomData = $@"";
                        string defaultScene = Path.Combine(assetsFolder, "Scene.room");
                        File.WriteAllText(defaultScene, roomData);
                        ProjectWindow.Instance.RefreshItems();
                        ProjectWindow.Instance.Get("Scene.room").Open();

                        // Create a default camera object in the scene
                        project.PopulateScene();

                        // Save project
                        project.Save();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating project: {ex.Message}");
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Close and shutdown the project
        /// </summary>
        public void CloseProject()
        {

        }

        /// <summary>
        /// Select a project to open
        /// </summary>
        public void SelectProject()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Earth Engine Project (*.earthproj)|*.earthproj|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var projectFile = openFileDialog.FileName;
                    if (File.Exists(projectFile))
                    {
                        try
                        {
                            OpenProject(projectFile);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Error opening project:\n{ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            Console.WriteLine($"Error opening project: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Project file not found: {projectFile}");
                    }
                }
            }
        }

        /// <summary>
        /// Opens the project given the filepath. This sets our engine paths and reloads the project
        /// </summary>
        /// <param name="projectFilePath"></param>
        public void OpenProject(string? projectFile)
        {
            if (projectFile == null)
                return;

            string projectDirectory = Path.GetDirectoryName(projectFile);
            var assetsDirectory = Path.Combine(projectDirectory, "Assets");

            // Set project directories
            ProjectSettings.ProjectDirectory = ProjectSettings.NormalizePath(projectDirectory);
            ProjectSettings.AssetsDirectory = ProjectSettings.NormalizePath(assetsDirectory);
            ProjectSettings.AbsoluteProjectPath = projectDirectory;
            ProjectSettings.AbsoluteAssetsPath = assetsDirectory;
            ProjectSettings.BuildPath = Path.Combine(ProjectSettings.ProjectDirectory, "Build");

            EnginePaths.AssetsBase = ProjectSettings.AssetsDirectory;

            // Update the project window
            _project.RefreshItems();
               
            // Load project settings
            project = new EarthProject();
            project.Load();
            EditorApp.Instance.fileWatcher = new EditorWatcher(projectDirectory);

            // Load existing projects (or create empty list)
            var data = File.Exists(ProjectSettings.RecentProjects)
                ? File.ReadAllLines(ProjectSettings.RecentProjects).ToList()
                : new List<string>();

            // Normalize path and remove duplicates
            data.RemoveAll(p => string.Equals(p, projectFile, StringComparison.OrdinalIgnoreCase));

            // Insert the new project at the top
            data.Insert(0, projectFile);

            // Limit to 10 recent entries
            if (data.Count > 10)
                data = data.Take(10).ToList();

            // Write updated list back to file
            File.WriteAllLines(ProjectSettings.RecentProjects, data);
            EditorApp.Instance.currentProject = project;
            EditorApp.Instance.Window.Title = project.settings.Title;

            Console.WriteLine($"Opened project {projectDirectory}");
            Console.WriteLine($"Project Directory: {ProjectSettings.ProjectDirectory}");
            Console.WriteLine($"Assets Directory: {ProjectSettings.AssetsDirectory}");
        }

        /// <summary>
        /// Saves all assets and game options
        /// </summary>
        public void SaveProject()
        {
            if (!EngineContext.Running)
            {
                _project.Save();
                project?.Save();
            }
        }

        public bool GetSceneViewVisible() => _sceneView.IsVisible;
        public void SetSceneViewVisible(bool visible) => _sceneView.SetVisible(visible);
        
        public bool GetInspectorVisible() => _inspector.IsVisible;
        public void SetInspectorVisible(bool visible) => _inspector.SetVisible(visible);
        
        public bool GetProjectVisible() => _project.IsVisible;
        public void SetProjectVisible(bool visible) => _project.SetVisible(visible);
        
        public bool GetConsoleVisible() => _console.IsVisible;
        public void SetConsoleVisible(bool visible) => _console.SetVisible(visible);
        
        public bool GetPerformanceVisible() => _performance.IsVisible;
        public void SetPerformanceVisible(bool visible) => _performance.SetVisible(visible);
        
        public bool GetAboutVisible() => _about.IsVisible;
        public void SetAboutVisible(bool visible) => _about.SetVisible(visible);
    }
} 
