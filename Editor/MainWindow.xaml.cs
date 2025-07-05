using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Engine.Core;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Threading;
using System.Management;

namespace Editor
{
    public partial class MainWindow : Window
    {
        private readonly string assetsRoot;
        private readonly Dictionary<string, string> assetTypeFolders = new()
        {
            { "Scripts", "Assets/Scripts" },
            { "Objects", "Assets/Objects" },
            { "Sprites", "Assets/Sprites" }
        };
        private TreeViewItem _rightClickItem;
        private string _inspectorObjectPath;
        private dynamic _inspectorObjectData;
        // Store last-inspected object path/data for drag-and-drop
        private string _lastInspectedObjectPath;
        private dynamic _lastInspectedObjectData;
        private bool isDraggingScript = false;

        public MainWindow()
        {
            InitializeComponent();
            assetsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets");
            LoadAssetTree();
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                PlayButton_Click(sender, e);
            }
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveAll();
                e.Handled = true;
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var solutionRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            try
            {
                // Recompile scripts before running the game
                var compiler = new Engine.Core.ScriptCompiler();
                var scriptsDir = Path.Combine(assetsRoot, "Scripts");
                var result = compiler.CompileScripts(scriptsDir);
                var dllPath = Path.Combine(scriptsDir, "GameScripts.dll");
                if (!result.Success)
                {
                    MessageBox.Show($"Script compilation failed:\n{string.Join("\n", result.Errors)}", "Script Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    //MessageBox.Show("Scripts compiled successfully!", "Script Compilation", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Copy DLL to Runtime's Scripts directory
                try
                {
                    var runtimeScriptsDir = Path.Combine(solutionRoot, "Runtime", "bin", "Debug", "net8.0-windows", "Scripts");
                    Directory.CreateDirectory(runtimeScriptsDir);
                    var destDllPath = Path.Combine(runtimeScriptsDir, "GameScripts.dll");
                    File.Copy(dllPath, destDllPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy GameScripts.dll to runtime: {ex.Message}", "Copy Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Save the current room if RoomEditor is open
                foreach (TabItem tab in ((TabControl)((DockPanel)Content).Children[0]).Items)
                {
                    if (tab.Content is UserControl uc && uc is RoomEditor re)
                    {
                        re.SaveRoom();
                        break;
                    }
                }
                // Go up from Editor/bin/Debug/net8.0-windows to solution root, then to Runtime/bin/Debug/net8.0-windows
                var runtimePath = Path.Combine(solutionRoot, "Runtime", "bin", "Debug", "net8.0-windows", "GameRuntime.exe");
                if (!File.Exists(runtimePath))
                {
                    MessageBox.Show($"Game runtime not found at: {runtimePath}\nPlease build the runtime project first.",
                        "Runtime Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = runtimePath,
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(runtimePath)
                    }
                };
                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred:\n{ex}", "Editor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAssetTree()
        {
            AssetTreeView.Items.Clear();
            foreach (var type in assetTypeFolders.Keys)
            {
                var absPath = Path.Combine(assetsRoot, type);
                var typeNode = new TreeViewItem { Header = type, Tag = absPath };
                LoadAssetFolder(typeNode, absPath);
                AssetTreeView.Items.Add(typeNode);
            }
        }

        private void LoadAssetFolder(TreeViewItem parent, string folderPath)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Add folders
            foreach (var dir in Directory.GetDirectories(folderPath))
            {
                var dirName = Path.GetFileName(dir);
                var dirNode = new TreeViewItem { Header = dirName, Tag = dir };
                LoadAssetFolder(dirNode, dir);
                parent.Items.Add(dirNode);
            }
            // Add files
            foreach (var file in Directory.GetFiles(folderPath))
            {
                var fileName = Path.GetFileName(file);
                var fileNode = new TreeViewItem { Header = fileName, Tag = file };
                parent.Items.Add(fileNode);
            }
        }

        private void AssetTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (isDraggingScript) return;
            var selected = AssetTreeView.SelectedItem as TreeViewItem;
            if (selected != null && File.Exists(selected.Tag.ToString()))
            {
                var ext = Path.GetExtension(selected.Tag.ToString()).ToLower();
                if (ext == ".cs")
                {
                    // Do not update inspector for script selection
                    return;
                }
                ShowInspector(selected.Tag.ToString());
            }
            else
            {
                InspectorPanel.Children.Clear();
            }
        }

        private void ShowInspector(string assetPath)
        {
            InspectorPanel.Children.Clear();
            var ext = Path.GetExtension(assetPath).ToLower();
            if (ext == ".eo")
            {
                _inspectorObjectPath = assetPath;
                var json = File.ReadAllText(assetPath);
                _inspectorObjectData = System.Text.Json.JsonSerializer.Deserialize<EarthObject>(json);
                _lastInspectedObjectPath = _inspectorObjectPath;
                _lastInspectedObjectData = _inspectorObjectData;
                // Assigned sprite preview (if any)
                if (!string.IsNullOrWhiteSpace(_inspectorObjectData.sprite))
                {
                    var spritePath = Path.Combine(assetsRoot, "Sprites", _inspectorObjectData.sprite);
                    if (File.Exists(spritePath))
                    {
                        var bmp = new System.Windows.Media.Imaging.BitmapImage(new Uri(spritePath));
                        var spriteImg = new Image
                        {
                            Source = bmp,
                            Width = bmp.PixelWidth,
                            Height = bmp.PixelHeight,
                            Stretch = System.Windows.Media.Stretch.None,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 10)
                        };
                        System.Windows.Media.RenderOptions.SetBitmapScalingMode(spriteImg, System.Windows.Media.BitmapScalingMode.NearestNeighbor);
                        InspectorPanel.Children.Add(spriteImg);
                    }
                }
                // Name
                var nameBox = new TextBox { Text = _inspectorObjectData.name, Margin = new Thickness(0, 0, 0, 10) };
                nameBox.LostFocus += (s, e) => { _inspectorObjectData.name = nameBox.Text; SaveInspectorObject(); };
                InspectorPanel.Children.Add(new TextBlock { Text = "Name:", FontWeight = FontWeights.Bold });
                InspectorPanel.Children.Add(nameBox);
                // Sprite
                InspectorPanel.Children.Add(new TextBlock { Text = "Sprite:", FontWeight = FontWeights.Bold });
                var spriteList = GetAllSprites();
                var spriteCombo = new ComboBox { ItemsSource = spriteList, SelectedItem = _inspectorObjectData.sprite, Margin = new Thickness(0, 0, 0, 10) };
                spriteCombo.SelectionChanged += (s, e) => { _inspectorObjectData.sprite = spriteCombo.SelectedItem?.ToString() ?? ""; SaveInspectorObject(); ShowInspector(assetPath); };
                InspectorPanel.Children.Add(spriteCombo);
                // Scripts
                InspectorPanel.Children.Add(new TextBlock { Text = "Scripts:", FontWeight = FontWeights.Bold });
                var scriptListPanel = new StackPanel { AllowDrop = true };
                var textBrush = (Brush)Application.Current.Resources["TextBrush"];
                foreach (var script in _inspectorObjectData.scripts)
                {
                    var sp = new StackPanel { Orientation = Orientation.Horizontal };
                    var scriptText = new TextBlock
                    {
                        Text = script,
                        Foreground = (Brush)Application.Current.Resources["TextBrush"] ?? Brushes.White,
                        Margin = new Thickness(0, 0, 5, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    sp.Children.Add(scriptText);
                    var removeBtn = new Button { Content = "Remove", Tag = script, Margin = new Thickness(5, 0, 0, 0), Style = (Style)Application.Current.Resources["DangerButton"] };
                    removeBtn.Click += (s, e) => { _inspectorObjectData.scripts.Remove(script); SaveInspectorObject(); ShowInspector(assetPath); };
                    sp.Children.Add(removeBtn);
                    scriptListPanel.Children.Add(sp);
                }
                scriptListPanel.Drop += (s, e) => InspectorPanel_Drop(s, e, assetPath);
                InspectorPanel.Children.Add(scriptListPanel);
                InspectorPanel.Children.Add(new TextBlock { Text = "Drag scripts here to add.", FontStyle = FontStyles.Italic, Foreground = Brushes.Gray });
            }
            else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            {
                // Sprite preview with nearest neighbor scaling, not warped, and show dimensions
                var bmp = new System.Windows.Media.Imaging.BitmapImage(new Uri(assetPath));
                var img = new Image
                {
                    Source = bmp,
                    Width = bmp.PixelWidth,
                    Height = bmp.PixelHeight,
                    Stretch = System.Windows.Media.Stretch.None,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                System.Windows.Media.RenderOptions.SetBitmapScalingMode(img, System.Windows.Media.BitmapScalingMode.NearestNeighbor);
                InspectorPanel.Children.Add(img);
                InspectorPanel.Children.Add(new TextBlock { Text = Path.GetFileName(assetPath), FontWeight = FontWeights.Bold });
                InspectorPanel.Children.Add(new TextBlock { Text = $"Dimensions: {bmp.PixelWidth} x {bmp.PixelHeight}", Foreground = Brushes.Gray });
            }
            else
            {
                // Default: show file info
                var fileInfo = new FileInfo(assetPath);
                InspectorPanel.Children.Add(new TextBlock { Text = $"Name: {fileInfo.Name}", FontWeight = FontWeights.Bold });
                InspectorPanel.Children.Add(new TextBlock { Text = $"Type: {fileInfo.Extension}" });
                InspectorPanel.Children.Add(new TextBlock { Text = $"Path: {assetPath}" });
                InspectorPanel.Children.Add(new TextBlock { Text = $"Size: {fileInfo.Length} bytes" });
                InspectorPanel.Children.Add(new TextBlock { Text = $"Last Modified: {fileInfo.LastWriteTime}" });
            }
        }

        private void InspectorPanel_Drop(object sender, DragEventArgs e)
        {
            // Always use last-inspected object for drag-and-drop
            InspectorPanel_Drop(sender, e, _lastInspectedObjectPath);
        }
        private void InspectorPanel_Drop(object sender, DragEventArgs e, string objectPath)
        {
            if (string.IsNullOrEmpty(objectPath)) return;
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var scriptPath = e.Data.GetData(DataFormats.StringFormat) as string;
                if (Path.GetExtension(scriptPath).ToLower() == ".cs")
                {
                    var scriptName = Path.GetFileName(scriptPath);
                    // Use last-inspected object data
                    var json = File.ReadAllText(objectPath);
                    var obj = System.Text.Json.JsonSerializer.Deserialize<EarthObject>(json);
                    if (!obj.scripts.Contains(scriptName))
                    {
                        obj.scripts.Add(scriptName);
                        var newJson = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(objectPath, newJson);
                        ShowInspector(objectPath);
                    }
                }
            }
        }
        private void SaveInspectorObject()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_inspectorObjectData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_inspectorObjectPath, json);
        }

        private void SaveAll()
        {
            try
            {
                // Save the currently inspected object if any
                if (!string.IsNullOrEmpty(_inspectorObjectPath) && _inspectorObjectData != null)
                {
                    SaveInspectorObject();
                }

                // Save the current room if RoomEditor is open
                foreach (TabItem tab in ((TabControl)((DockPanel)Content).Children[0]).Items)
                {
                    if (tab.Content is UserControl uc && uc is RoomEditor re)
                    {
                        re.SaveRoom();
                        break;
                    }
                }

                // Save game options if GameOptionsEditor is open
                foreach (TabItem tab in ((TabControl)((DockPanel)Content).Children[0]).Items)
                {
                    if (tab.Content is UserControl uc && uc is GameOptionsEditor goe)
                    {
                        goe.SaveGameOptions();
                        break;
                    }
                }

                // Show a brief save confirmation (optional)
                // MessageBox.Show("All changes saved!", "Save Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private List<string> GetAllSprites()
        {
            var spritesDir = Path.Combine(assetsRoot, "Sprites");
            if (!Directory.Exists(spritesDir)) return new List<string>();
            return Directory.GetFiles(spritesDir).Select(Path.GetFileName).ToList();
        }
        private void AssetTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var treeView = sender as TreeView;
                var item = treeView?.SelectedItem as TreeViewItem;
                if (item != null && File.Exists(item.Tag?.ToString()) && Path.GetExtension(item.Tag.ToString()).ToLower() == ".cs")
                {
                    isDraggingScript = true;
                    DragDrop.DoDragDrop(treeView, item.Tag.ToString(), DragDropEffects.Copy);
                    isDraggingScript = false;
                }
            }
        }
        private void AssetTreeView_Drop(object sender, DragEventArgs e)
        {
            // Optionally handle drop to move files/folders in the tree
        }

        private void AssetTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selected = AssetTreeView.SelectedItem as TreeViewItem;
            if (selected != null && File.Exists(selected.Tag.ToString()))
            {
                var ext = Path.GetExtension(selected.Tag.ToString()).ToLower();
                if (ext == ".cs")
                {
                    OpenInVisualStudio();
                }
                // Optionally handle other asset types
            }
        }

        private void OpenInVisualStudio()
        {
            try
            {
                // Find the solution root and .sln file
                var solutionRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
                var slnFile = Directory.GetFiles(solutionRoot, "*.sln").FirstOrDefault();
                if (slnFile == null)
                {
                    MessageBox.Show("Could not find the solution file (.sln) in the project root.");
                    return;
                }
                // Path to Visual Studio (Community 2022 by default)
                var vsPath = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe";
                if (!File.Exists(vsPath))
                {
                    MessageBox.Show("Could not find Visual Studio at:\n" + vsPath + "\nPlease update the path in the code.");
                    return;
                }
                // Check if Visual Studio is already running with this solution
                var processes = Process.GetProcessesByName("devenv");
                bool solutionOpen = false;
                foreach (var proc in processes)
                {
                    try
                    {
                        var cmdLine = GetCommandLine(proc);
                        if (cmdLine != null && cmdLine.Contains(Path.GetFileName(slnFile)))
                        {
                            solutionOpen = true;
                            break;
                        }
                    }
                    catch { /* Ignore access denied */ }
                }
                if (!solutionOpen)
                {
                    var psiSln = new ProcessStartInfo
                    {
                        FileName = vsPath,
                        Arguments = $"\"{slnFile}\"",
                        UseShellExecute = true,
                        WorkingDirectory = solutionRoot
                    };
                    Process.Start(psiSln);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open in Visual Studio: {ex.Message}");
            }
        }

        // Helper to get command line of a process (requires System.Management)
        private string GetCommandLine(Process process)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
                {
                    foreach (var @object in searcher.Get())
                    {
                        return @object["CommandLine"]?.ToString();
                    }
                }
            }
            catch { }
            return null;
        }

        private void AssetTreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _rightClickItem = null;
            var treeView = sender as TreeView;
            var element = e.OriginalSource as DependencyObject;
            while (element != null && !(element is TreeViewItem))
                element = VisualTreeHelper.GetParent(element);
            if (element is TreeViewItem item)
            {
                item.IsSelected = true;
                _rightClickItem = item;
                ShowContextMenuForItem(item, e);
                e.Handled = true;
            }
        }

        private void ShowContextMenuForItem(TreeViewItem item, MouseButtonEventArgs e)
        {
            var menu = new ContextMenu();
            // Always allow new folder
            var newFolder = new MenuItem { Header = "New Folder" };
            newFolder.Click += NewFolder_Click;
            menu.Items.Add(newFolder);

            // Contextual asset creation
            string tag = item.Tag?.ToString() ?? "";
            if (tag.Contains("Scripts"))
            {
                var newScript = new MenuItem { Header = "New Script" };
                newScript.Click += NewScript_Click;
                menu.Items.Add(newScript);
            }
            else if (tag.Contains("Objects"))
            {
                var newObject = new MenuItem { Header = "New Object" };
                newObject.Click += NewObject_Click;
                menu.Items.Add(newObject);
            }
            else if (tag.Contains("Sprites"))
            {
                var newSprite = new MenuItem { Header = "Import Sprite..." };
                newSprite.Click += ImportSprite_Click;
                menu.Items.Add(newSprite);
            }

            menu.Items.Add(new Separator());
            var rename = new MenuItem { Header = "Rename" };
            rename.Click += RenameAsset_Click;
            menu.Items.Add(rename);
            var del = new MenuItem { Header = "Delete" };
            del.Click += DeleteAsset_Click;
            menu.Items.Add(del);

            item.ContextMenu = menu;
            menu.IsOpen = true;
        }

        private void NewScript_Click(object sender, RoutedEventArgs e)
        {
            var parentDir = GetTargetDir(_rightClickItem);
            var assetName = PromptDialog($"Enter new script name (without extension):", "New Script");
            if (!string.IsNullOrWhiteSpace(assetName))
            {
                var newAssetPath = Path.Combine(parentDir, assetName + ".cs");
                if (!File.Exists(newAssetPath))
                    File.WriteAllText(newAssetPath, "// New script\n");
                LoadAssetTreeAndExpandTo(newAssetPath);
            }
        }

        private void NewObject_Click(object sender, RoutedEventArgs e)
        {
            var parentDir = GetTargetDir(_rightClickItem);
            var assetName = PromptDialog($"Enter new object name (without extension):", "New Object");
            if (!string.IsNullOrWhiteSpace(assetName))
            {
                var newAssetPath = Path.Combine(parentDir, assetName + ".eo");
                if (!File.Exists(newAssetPath))
                {
                    // Basic EO template
                    File.WriteAllText(newAssetPath, "{\n  \"name\": \"" + assetName + "\",\n  \"sprite\": \"\",\n  \"scripts\": []\n}\n");
                }
                LoadAssetTreeAndExpandTo(newAssetPath);

                // Refresh RoomEditor object lists
                foreach (TabItem tab in ((TabControl)((DockPanel)Content).Children[0]).Items)
                {
                    if (tab.Content is UserControl uc && uc is RoomEditor re)
                    {
                        re.GetType().GetMethod("LoadObjectList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(re, null);
                    }
                }
            }
        }

        private void ImportSprite_Click(object sender, RoutedEventArgs e)
        {
            var parentDir = GetTargetDir(_rightClickItem);
            var dlg = new OpenFileDialog { Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" };
            if (dlg.ShowDialog() == true)
            {
                var destPath = Path.Combine(parentDir, Path.GetFileName(dlg.FileName));
                File.Copy(dlg.FileName, destPath, overwrite: true);
                LoadAssetTreeAndExpandTo(destPath);
            }
        }

        private string GetTargetDir(TreeViewItem item)
        {
            if (item != null && Directory.Exists(item.Tag?.ToString()))
                return item.Tag.ToString();
            else if (item != null && File.Exists(item.Tag?.ToString()))
                return Path.GetDirectoryName(item.Tag.ToString());
            else
                return assetsRoot;
        }

        private void LoadAssetTreeAndExpandTo(string path)
        {
            LoadAssetTree();
            ExpandToPath(path);
        }

        private void ExpandToPath(string path)
        {
            foreach (TreeViewItem root in AssetTreeView.Items)
            {
                if (ExpandToPathRecursive(root, path))
                {
                    root.IsExpanded = true;
                    break;
                }
            }
        }

        private bool ExpandToPathRecursive(TreeViewItem node, string path)
        {
            if (node.Tag?.ToString() == path)
            {
                node.IsSelected = true;
                node.BringIntoView();
                return true;
            }
            foreach (TreeViewItem child in node.Items)
            {
                if (ExpandToPathRecursive(child, path))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }
            return false;
        }

        private void NewFolder_Click(object sender, RoutedEventArgs e)
        {
            var parentDir = GetTargetDir(_rightClickItem);
            var folderName = PromptDialog("Enter new folder name:", "New Folder");
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                var newFolderPath = Path.Combine(parentDir, folderName);
                if (!Directory.Exists(newFolderPath))
                    Directory.CreateDirectory(newFolderPath);
                LoadAssetTreeAndExpandTo(newFolderPath);
            }
        }

        private void RenameAsset_Click(object sender, RoutedEventArgs e)
        {
            var selected = AssetTreeView.SelectedItem as TreeViewItem;
            if (selected == null) return;
            var path = selected.Tag?.ToString();
            if (string.IsNullOrEmpty(path)) return;
            var isDir = Directory.Exists(path);
            var isFile = File.Exists(path);
            var currentName = Path.GetFileName(path);
            var newName = PromptDialog($"Enter new name:", "Rename", currentName);
            if (!string.IsNullOrWhiteSpace(newName) && newName != currentName)
            {
                var parentDir = Path.GetDirectoryName(path);
                var newPath = Path.Combine(parentDir, newName);
                if (isDir)
                    Directory.Move(path, newPath);
                else if (isFile)
                    File.Move(path, newPath);
                LoadAssetTree();
            }
        }

        private void DeleteAsset_Click(object sender, RoutedEventArgs e)
        {
            var selected = AssetTreeView.SelectedItem as TreeViewItem;
            if (selected == null) return;
            var path = selected.Tag?.ToString();
            if (string.IsNullOrEmpty(path)) return;
            var isDir = Directory.Exists(path);
            var isFile = File.Exists(path);
            if (MessageBox.Show($"Are you sure you want to delete '{Path.GetFileName(path)}'?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                if (isDir)
                    Directory.Delete(path, true);
                else if (isFile)
                    File.Delete(path);
                LoadAssetTree();
            }
        }

        // Simple prompt dialog for input
        private string PromptDialog(string text, string caption, string defaultValue = "")
        {
            var inputDialog = new Window
            {
                Title = caption,
                Width = 350,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = this
            };
            var panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new TextBlock { Text = text });
            var textBox = new TextBox { Text = defaultValue, Margin = new Thickness(0, 10, 0, 10) };
            panel.Children.Add(textBox);
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "OK", Width = 70, Margin = new Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "Cancel", Width = 70 };
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(buttonPanel);
            inputDialog.Content = panel;
            string result = null;
            okButton.Click += (s, e) => { result = textBox.Text; inputDialog.DialogResult = true; inputDialog.Close(); };
            cancelButton.Click += (s, e) => { inputDialog.DialogResult = false; inputDialog.Close(); };
            inputDialog.ShowDialog();
            return result;
        }

        // EO object model
        public class EarthObject
        {
            public string name { get; set; }
            public string sprite { get; set; }
            public List<string> scripts { get; set; } = new List<string>();
        }
    }
} 