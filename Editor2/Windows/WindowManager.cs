using Editor.AssetManagement;
using Engine.Core.Data;
using ImGuiNET;
using System;
using System.IO;
using System.Windows.Forms;

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

        public WindowManager(ConsoleWindow console)
        {
            _sceneView = new SceneViewWindow();
            _inspector = new InspectorWindow();
            _project = new ProjectWindow();
            _about = new AboutWindow();
            _performance = new PerformanceWindow();
            _console = console;
        }

        public void UpdatePerformance(double frameTime)
        {
            _performance.Update(frameTime);
        }

        public void RenderAll()
        {
            _sceneView.Render();
            _inspector.Render();
            _project.Render();
            _about.Render();
            _performance.Render();
            _console.Render();
        }

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
                        OpenProject();
                    }
                    if (ImGui.MenuItem("Save Project"))
                    {
                        SaveProject();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Exit"))
                    {
                        // TODO: Implement exit functionality
                    }
                    ImGui.EndMenu();
                }
                
                if (ImGui.BeginMenu("View"))
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
                    
                    ImGui.EndMenu();
                }
                
                ImGui.EndMainMenuBar();
            }
        }

        private void CreateNewProject()
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

                    try
                    {
                        // Create project structure
                        Directory.CreateDirectory(projectFolder);
                        Directory.CreateDirectory(assetsFolder);

                        // Create project file
                        var projectContent = $@"{{
  ""name"": ""{projectName}"",
  ""version"": ""1.0.0"",
  ""created"": ""{DateTime.Now:yyyy-MM-dd HH:mm:ss}""
}}";
                        File.WriteAllText(projectFile, projectContent);

                        // Set project directories
                        ProjectSettings.ProjectDirectory = ProjectSettings.NormalizePath(projectFolder);
                        ProjectSettings.AssetsDirectory = ProjectSettings.NormalizePath(assetsFolder);
                        ProjectSettings.AbsoluteProjectPath = projectFolder;
                        ProjectSettings.AbsoluteAssetsPath = assetsFolder;

                        // Set project path in project window
                        _project.SetProjectPath(projectFolder);

                        Console.WriteLine($"Created new project: {projectFile}");
                        Console.WriteLine($"Project Directory: {ProjectSettings.ProjectDirectory}");
                        Console.WriteLine($"Assets Directory: {ProjectSettings.AssetsDirectory}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating project: {ex.Message}");
                    }
                }
            }
        }

        public void OpenProject()
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
                            var projectDirectory = Path.GetDirectoryName(projectFile);
                            var assetsDirectory = Path.Combine(projectDirectory, "Assets");

                            // Set project directories
                            ProjectSettings.ProjectDirectory = ProjectSettings.NormalizePath(projectDirectory);
                            ProjectSettings.AssetsDirectory = ProjectSettings.NormalizePath(assetsDirectory);
                            ProjectSettings.AbsoluteProjectPath = projectDirectory;
                            ProjectSettings.AbsoluteAssetsPath = assetsDirectory;

                            // Set project path in project window
                            _project.SetProjectPath(projectDirectory);

                            Console.WriteLine($"Opened project: {projectFile}");
                            Console.WriteLine($"Project Directory: {ProjectSettings.ProjectDirectory}");
                            Console.WriteLine($"Assets Directory: {ProjectSettings.AssetsDirectory}");
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

        private void SaveProject()
        {
            // TODO: Implement project saving logic
            Console.WriteLine("Save Project - Not implemented yet");
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