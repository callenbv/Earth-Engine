using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Media;
using System.Text.Json;

namespace Editor
{
    /// <summary>
    /// Interaction logic for WelcomeWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        private List<ProjectManager.ProjectTemplate> availableTemplates;
        private ProjectManager.ProjectTemplate selectedTemplate;

        public WelcomeWindow()
        {
            InitializeComponent();
            InitializeTemplates();
            LoadRecentProjects();
            SetDefaultProjectLocation();
        }

        private void InitializeTemplates()
        {
            availableTemplates = ProjectManager.GetAvailableTemplates();
            TemplateComboBox.ItemsSource = availableTemplates;
            TemplateComboBox.DisplayMemberPath = "Name";
            TemplateComboBox.SelectedIndex = 0;
        }

        private void SetDefaultProjectLocation()
        {
            var defaultLocation = Path.Combine(ProjectManager.ProjectsDirectory, ProjectNameTextBox.Text);
            ProjectLocationTextBox.Text = defaultLocation;
        }

        private void LoadRecentProjects()
        {
            var recentProjects = ProjectManager.GetRecentProjects();
            // Remove any projects whose directory no longer exists
            var validProjects = recentProjects.Where(p => Directory.Exists(p.Path)).ToList();
            if (validProjects.Count != recentProjects.Count)
            {
                // Save the cleaned list
                File.WriteAllText(ProjectManager.ProjectsDirectory + "/RecentProjects.json", JsonSerializer.Serialize(validProjects, new JsonSerializerOptions { WriteIndented = true }));
            }
            RecentProjectsPanel.Children.Clear();
            foreach (var project in validProjects)
            {
                var projectButton = CreateProjectButtonControl(project);
                RecentProjectsPanel.Children.Add(projectButton);
            }
        }

        private System.Windows.Controls.Button CreateProjectButtonControl(ProjectManager.ProjectInfo project)
        {
            var button = new System.Windows.Controls.Button
            {
                Style = (Style)FindResource("ProjectButtonStyle"),
                Margin = new Thickness(0, 0, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var icon = new TextBlock
            {
                Text = "📁",
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var textPanel = new StackPanel();
            var nameText = new TextBlock
            {
                Text = project.Name,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var pathText = new TextBlock
            {
                Text = project.Path,
                FontSize = 11,
                Foreground = (Brush)FindResource("SecondaryTextBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            
            textPanel.Children.Add(nameText);
            textPanel.Children.Add(pathText);
            
            stackPanel.Children.Add(icon);
            stackPanel.Children.Add(textPanel);
            
            button.Content = stackPanel;
            button.Tag = project;
            button.Click += (s, e) => OpenProject(project);
            
            return button;
        }

        private void OpenProject(ProjectManager.ProjectInfo project)
        {
            if (!Directory.Exists(project.Path))
            {
                System.Windows.MessageBox.Show($"Project directory not found: {project.Path}", "Project Not Found", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LoadRecentProjects(); // Refresh the list
                return;
            }

            OpenProjectInEditor(project.Path);
        }

        private void OpenProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Open Earth Engine Project",
                Filter = $"Earth Engine Projects (*{ProjectManager.ProjectExtension})|*{ProjectManager.ProjectExtension}|All Files (*.*)|*.*",
                InitialDirectory = ProjectManager.ProjectsDirectory
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var projectInfo = ProjectManager.LoadProject(openFileDialog.FileName);
                if (projectInfo != null)
                {
                    ProjectManager.AddToRecentProjects(projectInfo);
                    OpenProjectInEditor(projectInfo.Path);
                }
                else
                {
                    System.Windows.MessageBox.Show("Invalid project file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BrowseLocationButton_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select project location",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var selectedPath = folderDialog.SelectedPath;
                var projectName = ProjectNameTextBox.Text;
                var projectPath = Path.Combine(selectedPath, projectName);
                ProjectLocationTextBox.Text = projectPath;
            }
        }

        private void TemplateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplateComboBox.SelectedItem is ProjectManager.ProjectTemplate template)
            {
                selectedTemplate = template;
                TemplateDescriptionText.Text = template.Description;
            }
        }

        private void CreateProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var projectName = ProjectNameTextBox.Text.Trim();
            var projectPath = ProjectLocationTextBox.Text.Trim();

            if (string.IsNullOrEmpty(projectName))
            {
                System.Windows.MessageBox.Show("Please enter a project name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(projectPath))
            {
                System.Windows.MessageBox.Show("Please select a project location.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Directory.Exists(projectPath))
            {
                var result = System.Windows.MessageBox.Show($"The directory '{projectPath}' already exists. Do you want to use it anyway?", 
                    "Directory Exists", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            var templateName = selectedTemplate?.Name ?? "Empty";
            
            try
            {
                var success = ProjectManager.CreateProject(projectName, projectPath, templateName);
                if (success)
                {
                    // Copy Engine.Core.dll and related files to project root
                    var solutionRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
                    var engineCoreSource = Path.Combine(solutionRoot, "Engine", "Engine.Core", "bin", "Debug", "net8.0", "Engine.Core.dll");
                    var engineCoreDest = Path.Combine(projectPath, "Engine.Core.dll");
                    if (File.Exists(engineCoreSource))
                        File.Copy(engineCoreSource, engineCoreDest, true);
                    else
                        MessageBox.Show($"Engine.Core.dll not found at {engineCoreSource}");
                        
                    var engineCoreXml = engineCoreSource.Replace(".dll", ".xml");
                    var engineCorePdb = engineCoreSource.Replace(".dll", ".pdb");
                    if (File.Exists(engineCoreXml))
                        File.Copy(engineCoreXml, Path.Combine(projectPath, "Engine.Core.xml"), true);
                    if (File.Exists(engineCorePdb))
                        File.Copy(engineCorePdb, Path.Combine(projectPath, "Engine.Core.pdb"), true);
                    // Generate the scripts .csproj file for Visual Studio support
                    var mainWindow = System.Windows.Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    if (mainWindow != null)
                    {
                        mainWindow.GenerateScriptsCsproj(projectPath);
                    }
                    else
                    {
                        new MainWindow(projectPath).GenerateScriptsCsproj(projectPath);
                    }
                    System.Windows.MessageBox.Show($"Project '{projectName}' created successfully!", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    OpenProjectInEditor(projectPath);
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to create project. Please check the location and try again.", 
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred while creating the project: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenProjectInEditor(string projectPath)
        {
            try
            {
                var mainWindow = new MainWindow(projectPath);
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open project: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProjectNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update the project location when the name changes
            var projectName = ProjectNameTextBox.Text;
            var currentLocation = ProjectLocationTextBox.Text;
            
            if (!string.IsNullOrEmpty(currentLocation))
            {
                var directory = Path.GetDirectoryName(currentLocation);
                if (!string.IsNullOrEmpty(directory))
                {
                    var newLocation = Path.Combine(directory, projectName);
                    ProjectLocationTextBox.Text = newLocation;
                }
            }
        }
    }
} 