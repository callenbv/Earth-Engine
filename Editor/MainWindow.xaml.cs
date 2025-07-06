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
using System.Windows.Media.Imaging;
using System.Text.Json;

namespace Editor
{
    public partial class MainWindow : Window
    {
        private readonly string assetsRoot;
        private readonly Dictionary<string, string> assetTypeFolders = new()
        {
            { "Scripts", "Scripts" },
            { "Objects", "Objects" },
            { "Sprites", "Sprites" },
            { "Rooms", "Rooms" }
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
                // Get DLL from Editor/bin/Scripts
                var editorBinScripts = Path.GetFullPath(Path.Combine(scriptsDir, "..", "..", "bin", "Scripts"));
                var dllPath = Path.Combine(editorBinScripts, "GameScripts.dll");
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
            // Add files (show all except .dll)
            foreach (var file in Directory.GetFiles(folderPath))
            {
                if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;
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
                if (ext == ".room")
                {
                    var tabControl = (TabControl)((DockPanel)Content).Children[0];
                    foreach (TabItem tab in tabControl.Items)
                    {
                        if (tab.Content is RoomEditor re)
                        {
                            re.LoadRoom(selected.Tag.ToString());
                            tabControl.SelectedItem = tab;
                            break;
                        }
                    }
                    return; // Prevent ShowInspector for .room files
                }
                else if (ext == ".cs")
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
                AnimatedSpriteDisplay animatedSprite = null;
                if (!string.IsNullOrWhiteSpace(_inspectorObjectData.sprite))
                {
                    var spritePath = Path.Combine(assetsRoot, "Sprites", _inspectorObjectData.sprite);
                    if (File.Exists(spritePath))
                    {
                        // Load sprite data if it exists
                        var spriteDataPath = Path.ChangeExtension(spritePath, ".sprite");
                        SpriteData spriteData = null;
                        if (File.Exists(spriteDataPath))
                        {
                            try
                            {
                                var spriteJson = File.ReadAllText(spriteDataPath);
                                spriteData = JsonSerializer.Deserialize<SpriteData>(spriteJson);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load sprite data: {ex.Message}");
                            }
                        }
                        animatedSprite = new AnimatedSpriteDisplay();
                        animatedSprite.LoadSprite(spritePath, spriteData);
                        animatedSprite.Margin = new Thickness(0, 0, 0, 10);
                        InspectorPanel.Children.Add(animatedSprite);
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
                var spriteCombo = new ComboBox { ItemsSource = spriteList, SelectedItem = _inspectorObjectData.sprite, Margin = new Thickness(0, 0, 0, 10), Width = 180 };
                spriteCombo.SelectionChanged += (s, e) => {
                    var selected = spriteCombo.SelectedItem?.ToString() ?? "";
                    _inspectorObjectData.sprite = !string.IsNullOrEmpty(selected) ? selected : "";
                    SaveInspectorObject();
                    // Update the image in-place if possible
                    if (animatedSprite != null && !string.IsNullOrEmpty(selected))
                    {
                        var newSpritePath = Path.Combine(assetsRoot, "Sprites", selected);
                        if (File.Exists(newSpritePath))
                        {
                            var newSpriteDataPath = Path.ChangeExtension(newSpritePath, ".sprite");
                            SpriteData newSpriteData = null;
                            if (File.Exists(newSpriteDataPath))
                            {
                                try
                                {
                                    var spriteJson = File.ReadAllText(newSpriteDataPath);
                                    newSpriteData = JsonSerializer.Deserialize<SpriteData>(spriteJson);
                                }
                                catch { }
                            }
                            animatedSprite.LoadSprite(newSpritePath, newSpriteData);
                        }
                    }
                    else
                    {
                        ShowInspector(assetPath);
                    }
                };
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
                // Load or create sprite data
                var spriteDataPath = Path.ChangeExtension(assetPath, ".sprite");
                SpriteData spriteData = null;
                
                if (File.Exists(spriteDataPath))
                {
                    try
                    {
                        var spriteJson = File.ReadAllText(spriteDataPath);
                        spriteData = JsonSerializer.Deserialize<SpriteData>(spriteJson);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load sprite data: {ex.Message}");
                    }
                }
                
                if (spriteData == null)
                {
                    // Create default sprite data
                    byte[] imageBytes = File.ReadAllBytes(assetPath);
                    BitmapImage bmp = new BitmapImage();
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                        bmp.Freeze();
                    }
                    
                    spriteData = new SpriteData
                    {
                        name = Path.GetFileNameWithoutExtension(assetPath),
                        frameWidth = bmp.PixelWidth,
                        frameHeight = bmp.PixelHeight,
                        frameCount = 1,
                        frameSpeed = 1.0,
                        animated = false
                    };
                }

                // Animated sprite preview
                var animatedSprite = new AnimatedSpriteDisplay();
                animatedSprite.LoadSprite(assetPath, spriteData);
                animatedSprite.Margin = new Thickness(0, 0, 0, 10);
                InspectorPanel.Children.Add(animatedSprite);
                
                InspectorPanel.Children.Add(new TextBlock { Text = Path.GetFileName(assetPath), FontWeight = FontWeights.Bold });
                InspectorPanel.Children.Add(new TextBlock { Text = $"Dimensions: {spriteData.frameWidth} x {spriteData.frameHeight}", Foreground = Brushes.Gray });
                
                // Animation controls
                InspectorPanel.Children.Add(new TextBlock { Text = "Animation Settings:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5) });
                
                // Animated checkbox
                var animatedCheck = new CheckBox { Content = "Animated", IsChecked = spriteData.animated, Margin = new Thickness(0, 0, 0, 5) };
                animatedCheck.Checked += (s, e) => { spriteData.animated = true; SaveSpriteData(spriteDataPath, spriteData); animatedSprite.SetAnimationProperties(spriteData); };
                animatedCheck.Unchecked += (s, e) => { spriteData.animated = false; SaveSpriteData(spriteDataPath, spriteData); animatedSprite.SetAnimationProperties(spriteData); };
                InspectorPanel.Children.Add(animatedCheck);
                
                // Frame count
                InspectorPanel.Children.Add(new TextBlock { Text = "Frame Count:" });
                var frameCountBox = new TextBox { Text = spriteData.frameCount.ToString(), Margin = new Thickness(0, 0, 0, 5) };
                frameCountBox.LostFocus += (s, e) => { 
                    if (int.TryParse(frameCountBox.Text, out int count) && count > 0) 
                    { 
                        spriteData.frameCount = count; 
                        SaveSpriteData(spriteDataPath, spriteData); 
                        animatedSprite.SetAnimationProperties(spriteData); 
                    } 
                };
                InspectorPanel.Children.Add(frameCountBox);
                
                // Frame speed
                InspectorPanel.Children.Add(new TextBlock { Text = "Frame Speed (FPS):" });
                var frameSpeedBox = new TextBox { Text = spriteData.frameSpeed.ToString(), Margin = new Thickness(0, 0, 0, 5) };
                frameSpeedBox.LostFocus += (s, e) => { 
                    if (double.TryParse(frameSpeedBox.Text, out double speed) && speed > 0) 
                    { 
                        spriteData.frameSpeed = speed; 
                        SaveSpriteData(spriteDataPath, spriteData); 
                        animatedSprite.SetAnimationProperties(spriteData); 
                    } 
                };
                InspectorPanel.Children.Add(frameSpeedBox);
                
                // Frame width
                InspectorPanel.Children.Add(new TextBlock { Text = "Frame Width (0 = full width):" });
                var frameWidthBox = new TextBox { Text = spriteData.frameWidth.ToString(), Margin = new Thickness(0, 0, 0, 5) };
                frameWidthBox.LostFocus += (s, e) => { 
                    if (int.TryParse(frameWidthBox.Text, out int width) && width >= 0) 
                    { 
                        spriteData.frameWidth = width; 
                        SaveSpriteData(spriteDataPath, spriteData); 
                        animatedSprite.SetAnimationProperties(spriteData); 
                    } 
                };
                InspectorPanel.Children.Add(frameWidthBox);
                
                // Frame height
                InspectorPanel.Children.Add(new TextBlock { Text = "Frame Height (0 = full height):" });
                var frameHeightBox = new TextBox { Text = spriteData.frameHeight.ToString(), Margin = new Thickness(0, 0, 0, 5) };
                frameHeightBox.LostFocus += (s, e) => { 
                    if (int.TryParse(frameHeightBox.Text, out int height) && height >= 0) 
                    { 
                        spriteData.frameHeight = height; 
                        SaveSpriteData(spriteDataPath, spriteData); 
                        animatedSprite.SetAnimationProperties(spriteData); 
                    } 
                };
                InspectorPanel.Children.Add(frameHeightBox);
                
                var loadSpriteBtn = new Button { Content = "Load New Sprite...", Margin = new Thickness(0, 10, 0, 10) };
                loadSpriteBtn.Click += (s, e) => {
                    var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" };
                    if (dlg.ShowDialog() == true)
                    {
                        try
                        {
                            var spritesDir = Path.Combine(assetsRoot, "Sprites");
                            var destPath = Path.Combine(spritesDir, Path.GetFileName(dlg.FileName));
                            File.Copy(dlg.FileName, destPath, overwrite: true);
                            
                            // Refresh the asset tree to show the new sprite
                            LoadAssetTree();
                            
                            // Refresh the inspector to show the updated sprite
                            ShowInspector(destPath);
                            
                            MessageBox.Show($"Sprite '{Path.GetFileName(dlg.FileName)}' loaded successfully!", "Sprite Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to load sprite: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                };
                InspectorPanel.Children.Add(loadSpriteBtn);
                var deleteSpriteBtn = new Button { Content = "Delete Sprite", Margin = new Thickness(0, 0, 0, 10), Style = (Style)Application.Current.Resources["DangerButton"] };
                deleteSpriteBtn.Click += (s, e) => {
                    if (MessageBox.Show($"Delete this sprite? This cannot be undone.", "Delete Sprite", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            File.Delete(assetPath);
                            if (File.Exists(spriteDataPath))
                                File.Delete(spriteDataPath);
                            // Optionally, refresh the asset tree or inspector
                            InspectorPanel.Children.Clear();
                            MessageBox.Show("Sprite deleted.");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to delete sprite: {ex.Message}");
                        }
                    }
                };
                InspectorPanel.Children.Add(deleteSpriteBtn);
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
        
        private void SaveSpriteData(string spriteDataPath, SpriteData spriteData)
        {
            try
            {
                var json = JsonSerializer.Serialize(spriteData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(spriteDataPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save sprite data: {ex.Message}");
            }
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
            var sprites = new List<string>();
            var spritesDir = Path.Combine(assetsRoot, "Sprites");
            if (Directory.Exists(spritesDir))
            {
                foreach (var file in Directory.GetFiles(spritesDir))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                    {
                        sprites.Add(Path.GetFileName(file));
                    }
                }
            }
            return sprites;
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
            var contextMenu = new ContextMenu();
            // Always allow new folder
            var newFolder = new MenuItem { Header = "New Folder" };
            newFolder.Click += NewFolder_Click;
            contextMenu.Items.Add(newFolder);

            // Contextual asset creation
            string tag = item.Tag?.ToString() ?? "";
            if (tag.Contains("Scripts"))
            {
                var newScript = new MenuItem { Header = "New Script" };
                newScript.Click += NewScript_Click;
                contextMenu.Items.Add(newScript);
            }
            else if (tag.Contains("Objects"))
            {
                var newObject = new MenuItem { Header = "New Object" };
                newObject.Click += NewObject_Click;
                contextMenu.Items.Add(newObject);
            }
            else if (tag.Contains("Sprites"))
            {
                var newSprite = new MenuItem { Header = "Import Sprite..." };
                newSprite.Click += ImportSprite_Click;
                contextMenu.Items.Add(newSprite);
            }

            contextMenu.Items.Add(new Separator());
            var rename = new MenuItem { Header = "Rename" };
            rename.Click += RenameAsset_Click;
            contextMenu.Items.Add(rename);
            var del = new MenuItem { Header = "Delete" };
            del.Click += DeleteAsset_Click;
            contextMenu.Items.Add(del);

            // Add 'New Room' to asset tree context menu
            var newRoomMenuItem = new MenuItem { Header = "New Room" };
            newRoomMenuItem.Click += (s, args) => {
                string roomName = PromptDialog("Enter room name:", "New Room");
                if (!string.IsNullOrWhiteSpace(roomName))
                {
                    var roomsDir = Path.Combine(assetsRoot, "Rooms");
                    Directory.CreateDirectory(roomsDir);
                    var roomPath = Path.Combine(roomsDir, roomName + ".room");
                    if (!File.Exists(roomPath))
                    {
                        var defaultRoomJson = "{\n  \"name\": \"" + roomName + "\",\n  \"objects\": []\n}";
                        File.WriteAllText(roomPath, defaultRoomJson);
                        LoadAssetTree();
                    }
                    else
                    {
                        MessageBox.Show("A room with that name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };
            contextMenu.Items.Add(newRoomMenuItem);

            item.ContextMenu = contextMenu;
            contextMenu.IsOpen = true;
            e.Handled = true;
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
        
        public class SpriteData
        {
            public string name { get; set; }
            public int frameWidth { get; set; } = 0; // 0 means use full image
            public int frameHeight { get; set; } = 0; // 0 means use full image
            public int frameCount { get; set; } = 1;
            public double frameSpeed { get; set; } = 1.0; // frames per second
            public bool animated { get; set; } = false;
        }
    }
} 