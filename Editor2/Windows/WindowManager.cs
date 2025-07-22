using Editor.AssetManagement;
using Engine.Core;
using Engine.Core.Data;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using System.IO;

namespace EarthEngineEditor.Windows
{
    public class WindowManager
    {
        private readonly SceneViewWindow _sceneView;
        private readonly InspectorWindow _inspector;
        private readonly ProjectWindow _project;
        private readonly AboutWindow _about;
        private readonly PerformanceWindow _performance;
        private readonly ConsoleWindow _console;
        private readonly ToolbarWindow toolbar;
        private EarthProject project;
        private EditorApp game;
        public List<string> recentProjects = new List<string>();
        private bool _openSettingsPopup = false;
        private bool openBuildPopup = false;
        private static string exportPath = "";
        private static int selectedTargetIndex = 0;
        private static readonly string[] targets = new[] { "linux-x64", "win-x64"};

        public WindowManager(EditorApp game_, ConsoleWindow console)
        {
            _sceneView = new SceneViewWindow();
            _inspector = new InspectorWindow();
            _project = new ProjectWindow();
            _about = new AboutWindow();
            _performance = new PerformanceWindow();
            toolbar = new ToolbarWindow();
            _console = console;
            game = game_;
            Load();
        }

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
            toolbar.Render();
        }

        /// <summary>
        /// Load all recent projects
        /// </summary>
        public void Load()
        {
            var files = File.ReadAllLines(ProjectSettings.RecentProjects).ToList();
            recentProjects = files;
        }

        /// <summary>
        /// Returns the last opened project, useful for a lot of things
        /// </summary>
        /// <returns></returns>
        public string? GetLastProject()
        {
            string? recentProject = recentProjects.FirstOrDefault();

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
                        CreateNewProject();
                    }
                    if (ImGui.MenuItem("Open Project"))
                    {
                        SelectProject();
                    }
                    if (ImGui.MenuItem("Save Project"))
                    {
                        SaveProject();
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
                    if (ImGui.MenuItem("Settings"))
                    {
                        _openSettingsPopup = true;
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

                // Export the game
                if (ImGui.BeginPopupModal("Export Game", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    EditorApp.Instance.gameFocused = false;

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
                if (ImGui.BeginPopupModal("Game Settings", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    EditorApp.Instance.gameFocused = false;

                    var settings = game.runtime.gameOptions;

                    string title = settings.Title ?? "";
                    if (ImGui.InputText("Game Title", ref title, 100))
                        settings.Title = title;

                    int width = settings.WindowWidth;
                    if (ImGui.InputInt("Window Width", ref width))
                        settings.WindowWidth = Math.Max(100, width);

                    int height = settings.WindowHeight;
                    if (ImGui.InputInt("Window Height", ref height))
                        settings.WindowHeight = Math.Max(100, height);

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

                ImGui.EndMainMenuBar();
            }
        }

        /// <summary>
        /// Called when we create a new project
        /// </summary>
        public void CreateNewProject()
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select the folder where you want to create the project";
                folderBrowserDialog.ShowNewFolderButton = true;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    var projectPath = folderBrowserDialog.SelectedPath;
                    var projectName = "NewProject";
                    var projectFolder = Path.Combine(projectPath, projectName);
                    var projectFile = Path.Combine(projectFolder, $"{projectName}.earthproj");
                    var assetsFolder = Path.Combine(projectFolder, "Assets");
                    var csprojFile = Path.Combine(projectFolder, $"{projectName}.csproj");
                    try
                    {
                        // Create project structure
                        Directory.CreateDirectory(projectFolder);
                        Directory.CreateDirectory(assetsFolder);

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

                        File.WriteAllText(csprojFile, csprojContent);
                        File.WriteAllText(projectFile, projectContent);

                        OpenProject(projectFolder);
                        project.settings.GameName = projectName;

                        Console.WriteLine($"Created new project: {projectFile}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating project: {ex.Message}");
                    }
                }
            }
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
        public void OpenProject(string? projectFilePath)
        {
            if (projectFilePath == null)
                return;

            var projectDirectory = Path.GetDirectoryName(projectFilePath);
            var assetsDirectory = Path.Combine(projectDirectory, "Assets");

            // Set project directories
            ProjectSettings.ProjectDirectory = ProjectSettings.NormalizePath(projectDirectory);
            ProjectSettings.AssetsDirectory = ProjectSettings.NormalizePath(assetsDirectory);
            ProjectSettings.AbsoluteProjectPath = projectDirectory;
            ProjectSettings.AbsoluteAssetsPath = assetsDirectory;

            EnginePaths.AssetsBase = ProjectSettings.AssetsDirectory;

            // Update the project window
            _project.SetProjectPath(projectDirectory);
               
            // Load project settings
            project = new EarthProject();
            project.Load();
            EditorApp.Instance.fileWatcher = new EditorWatcher(projectDirectory);

            // Load existing projects (or create empty list)
            var data = File.Exists(ProjectSettings.RecentProjects)
                ? File.ReadAllLines(ProjectSettings.RecentProjects).ToList()
                : new List<string>();

            // Normalize path and remove duplicates
            data.RemoveAll(p => string.Equals(p, projectFilePath, StringComparison.OrdinalIgnoreCase));

            // Insert the new project at the top
            data.Insert(0, projectFilePath);

            // Limit to 10 recent entries
            if (data.Count > 10)
                data = data.Take(10).ToList();

            // Write updated list back to file
            File.WriteAllLines(ProjectSettings.RecentProjects, data);

            Console.WriteLine($"Opened project {projectFilePath}");
            Console.WriteLine($"Project Directory: {ProjectSettings.ProjectDirectory}");
            Console.WriteLine($"Assets Directory: {ProjectSettings.AssetsDirectory}");
        }

        /// <summary>
        /// Saves all assets and game options
        /// </summary>
        public void SaveProject()
        {
            _project.Save();
            project?.Save();
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