using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace Editor
{
    public class ProjectManager
    {
        public static readonly string ProjectExtension = ".earthproj";
        public static readonly string ProjectsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
            "EarthEngine");

        private static readonly string RecentProjectsFile = Path.Combine(ProjectsDirectory, "RecentProjects.json");

        public class ProjectInfo
        {
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
            public DateTime CreatedDate { get; set; } = DateTime.Now;
            public DateTime LastModified { get; set; } = DateTime.Now;
            public string Version { get; set; } = "1.0.0";
        }

        public class ProjectTemplate
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string TemplatePath { get; set; } = "";
        }

        public static List<ProjectTemplate> GetAvailableTemplates()
        {
            var templates = new List<ProjectTemplate>
            {
                new ProjectTemplate
                {
                    Name = "Empty Project",
                    Description = "A blank project with basic folder structure and player movement",
                    TemplatePath = "bin/Assets"
                },
                new ProjectTemplate
                {
                    Name = "2D Platformer",
                    Description = "A basic 2D platformer template with player movement and camera follow",
                    TemplatePath = "bin/Assets"
                },
                new ProjectTemplate
                {
                    Name = "Top-Down RPG",
                    Description = "A top-down RPG template with basic game objects and lighting",
                    TemplatePath = "bin/Assets"
                }
            };

            return templates;
        }

        public static bool CreateProject(string projectName, string projectPath, string templateName = "Empty")
        {
            try
            {
                // Create project directory
                Directory.CreateDirectory(projectPath);

                // Create project file
                var projectInfo = new ProjectInfo
                {
                    Name = projectName,
                    Path = projectPath,
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now
                };

                var projectFilePath = Path.Combine(projectPath, projectName + ProjectExtension);
                var projectJson = JsonSerializer.Serialize(projectInfo, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(projectFilePath, projectJson);

                // Create basic folder structure (empty)
                var folders = new[]
                {
                    "Assets",
                    "Assets/Scripts",
                    "Assets/Objects",
                    "Assets/Sprites",
                    "Assets/Rooms",
                    "Assets/Fonts",
                    "Assets/Tilemaps"
                };

                foreach (var folder in folders)
                {
                    Directory.CreateDirectory(Path.Combine(projectPath, folder));
                }

                // Do NOT copy any template files or assets

                // Do NOT copy any template files or assets

                AddToRecentProjects(projectInfo);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static ProjectInfo? LoadProject(string projectPath)
        {
            try
            {
                if (!File.Exists(projectPath))
                    return null;

                var projectJson = File.ReadAllText(projectPath);
                var projectInfo = JsonSerializer.Deserialize<ProjectInfo>(projectJson);
                
                if (projectInfo != null)
                {
                    projectInfo.LastModified = DateTime.Now;
                    // Update the project file with new last modified date
                    var updatedJson = JsonSerializer.Serialize(projectInfo, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(projectPath, updatedJson);
                }

                return projectInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void AddToRecentProjects(ProjectInfo project)
        {
            List<ProjectInfo> recent = GetRecentProjects();
            // Remove any existing entry for this path
            recent.RemoveAll(p => p.Path == project.Path);
            // Add to the top
            recent.Insert(0, project);
            // Keep only the 10 most recent
            if (recent.Count > 10) recent = recent.Take(10).ToList();
            // Save
            Directory.CreateDirectory(ProjectsDirectory);
            File.WriteAllText(RecentProjectsFile, JsonSerializer.Serialize(recent, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static List<ProjectInfo> GetRecentProjects()
        {
            var recentProjects = new List<ProjectInfo>();
            try
            {
                if (File.Exists(RecentProjectsFile))
                {
                    var json = File.ReadAllText(RecentProjectsFile);
                    var list = JsonSerializer.Deserialize<List<ProjectInfo>>(json);
                    if (list != null)
                        recentProjects = list;
                }
            }
            catch { }
            return recentProjects;
        }

        public static bool IsValidProjectPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return false;

            var projectFiles = Directory.GetFiles(path, "*" + ProjectExtension);
            return projectFiles.Length > 0;
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }
    }
} 