using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Threading;
using System.Management;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Engine.Core.Game.Components;
using Engine.Core.Data;
using System.Numerics;

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
        private const string ScriptTemplate =
@"using System;
using Engine.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class $CLASS$ : GameScript
{
    public override void Create()
    {
    }

    public override void Update(GameTime gameTime)
    {
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
    }

    public override void Destroy()
    {
    }
}";

        private TreeViewItem _rightClickItem;
        private string _inspectorObjectPath;
        private dynamic _inspectorObjectData;
        // Store last-inspected object path/data for drag-and-drop
        private string _lastInspectedObjectPath;
        private dynamic _lastInspectedObjectData;
        private bool isDraggingScript = false;
        private bool isDraggingAsset = false;
        private bool isHandlingRoomSelection = false;
        private FileSystemWatcher _assetsWatcher;
        private DateTime _lastReload = DateTime.MinValue;

        public MainWindow(string projectPath = null)
        {
            InitializeComponent();
            
            if (!string.IsNullOrEmpty(projectPath) && Directory.Exists(projectPath))
            {
                // Use the provided project path
                assetsRoot = Path.Combine(projectPath, "Assets");
                Title = $"Earth Engine - {Path.GetFileName(projectPath)}";
            }
            else
            {
                // Fallback to default assets path
                assetsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets");
            }
            
            // Find the TabControl and set RoomEditor and GameOptionsEditor
            var dockPanel = (DockPanel)Content;
            TabControl tabControl = null;
            foreach (var child in dockPanel.Children)
            {
                if (child is TabControl tc)
                {
                    tabControl = tc;
                    break;
                }
            }
            if (tabControl != null)
            {
                foreach (TabItem tab in tabControl.Items)
                {
                    if (tab.Header.ToString() == "Rooms")
                        tab.Content = new RoomEditor(assetsRoot);
                    else if (tab.Header.ToString() == "Game Options")
                        tab.Content = new GameOptionsEditor(assetsRoot);
                }
            }
            
            LoadAssetTree();
            this.KeyDown += MainWindow_KeyDown;

            // Set up FileSystemWatcher for assets
            _assetsWatcher = new FileSystemWatcher(assetsRoot)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            _assetsWatcher.Changed += OnAssetsChanged;
            _assetsWatcher.Created += OnAssetsChanged;
            _assetsWatcher.Deleted += OnAssetsChanged;
            _assetsWatcher.Renamed += OnAssetsChanged;
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

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 0. Read icon path from game_options.json
                var projectRoot = Path.GetDirectoryName(assetsRoot);
                var gameOptionsPath = Path.Combine(assetsRoot, "game_options.json");
                string iconPath = null;
                if (File.Exists(gameOptionsPath))
                {
                    try
                    {
                        var json = File.ReadAllText(gameOptionsPath);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("icon", out var iconProp))
                        {
                            iconPath = iconProp.GetString();
                        }
                    }
                    catch { /* ignore */ }
                }
                var solutionRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
                var runtimeDir = Path.Combine(solutionRoot, "Runtime");
                var runtimeIconPath = Path.Combine(runtimeDir, "icon.ico");
                var runtimeCsprojPath = Path.Combine(runtimeDir, "GameRuntime.csproj");
                string csprojText = File.ReadAllText(runtimeCsprojPath);
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    File.Copy(iconPath, runtimeIconPath, true);
                    // Add or update ApplicationIcon
                    if (csprojText.Contains("<ApplicationIcon>"))
                        csprojText = Regex.Replace(csprojText, "<ApplicationIcon>.*?</ApplicationIcon>", "<ApplicationIcon>icon.ico</ApplicationIcon>");
                    else
                        csprojText = csprojText.Replace("<PropertyGroup>", "<PropertyGroup>\n    <ApplicationIcon>icon.ico</ApplicationIcon>");
                }
                else
                {
                    // Remove ApplicationIcon if present
                    csprojText = Regex.Replace(csprojText, @"<ApplicationIcon>.*?</ApplicationIcon>\s*", "");
                    if (File.Exists(runtimeIconPath))
                        File.Delete(runtimeIconPath);
                }
                File.WriteAllText(runtimeCsprojPath, csprojText);

                // 1. Compile scripts as before, but show progress bar
                var compiler = new Engine.Core.ScriptCompiler();
                var scriptsDir = Path.Combine(assetsRoot, "Scripts");
                var projectBinDir = Path.Combine(Path.GetDirectoryName(assetsRoot), "bin");
                Directory.CreateDirectory(projectBinDir);

                // Get all script files
                var scriptFiles = Directory.GetFiles(scriptsDir, "*.cs", SearchOption.AllDirectories);
                ScriptCompileProgressBar.Visibility = Visibility.Visible;
                ScriptCompileProgressBar.Minimum = 0;
                ScriptCompileProgressBar.Maximum = scriptFiles.Length;
                ScriptCompileProgressBar.Value = 0;
                ScriptCompileStatus.Text = "Compiling scripts...";

                // Compile scripts one by one for progress
                int compiled = 0;
                var compileResult = await Task.Run(() => compiler.CompileScriptsWithProgress(scriptsDir, Path.Combine(projectBinDir, "Scripts", "GameScripts.dll"), (current, total, file) =>
                {
                    Dispatcher.Invoke(() => {
                        ScriptCompileProgressBar.Value = current;
                        ScriptCompileStatus.Text = $"Compiling {Path.GetFileName(file)} ({current}/{total})";
                    });
                }));
                ScriptCompileProgressBar.Value = scriptFiles.Length;
                ScriptCompileStatus.Text = "Script compilation complete.";
                await Task.Delay(500);
                ScriptCompileProgressBar.Visibility = Visibility.Collapsed;
                ScriptCompileStatus.Text = "";
                if (!compileResult.Success)
                {
                    System.Windows.MessageBox.Show($"Script compilation failed:\n{string.Join("\n", compileResult.Errors)}", "Script Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 2. Build content with MGCB (pre-launch step)
                ScriptCompileStatus.Text = "Building content...";
                var mgcbPath = Path.Combine(projectRoot, "Content.mgcb");
                var contentOutput = Path.Combine(projectBinDir, "Content");
                Directory.CreateDirectory(contentOutput);
                if (File.Exists(mgcbPath))
                {
                    var mgcbCmd = $"dotnet tool run mgcb -- /@:{mgcbPath} /outputDir:{contentOutput}";
                    var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c {mgcbCmd}")
                    {
                        WorkingDirectory = projectRoot,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    var proc = System.Diagnostics.Process.Start(psi);
                    proc.WaitForExit();
                }
                ScriptCompileStatus.Text = "Content build complete.";
                await Task.Delay(500);
                ScriptCompileStatus.Text = "";

                // 3. Copy runtime EXE and dependencies to project bin
                var runtimeBuildDir = Path.Combine(solutionRoot, "Runtime", "bin", "Debug", "net8.0-windows");
                var runtimeExe = Path.Combine(runtimeBuildDir, "GameRuntime.exe");
                if (!File.Exists(runtimeExe))
                {
                    System.Windows.MessageBox.Show($"Game runtime not found at: {runtimeExe}\nPlease build the runtime project first.", "Runtime Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // Copy all files and subdirectories from runtimeBuildDir to projectBinDir
                foreach (var file in Directory.GetFiles(runtimeBuildDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(runtimeBuildDir, file);
                    var dest = Path.Combine(projectBinDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.Copy(file, dest, true);
                }

                // 4. Copy scripts DLL to project bin/Scripts
                var projectBinScripts = Path.Combine(projectBinDir, "Scripts");
                Directory.CreateDirectory(projectBinScripts);
                var dllPath = Path.Combine(projectBinScripts, "GameScripts.dll");
                var result = compiler.CompileScripts(scriptsDir, dllPath);
                if (!result.Success)
                {
                    System.Windows.MessageBox.Show($"Script compilation failed:\n{string.Join("\n", result.Errors)}", "Script Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // Ensure Engine.Core.dll is present in bin directory
                var engineCoreSource = Path.Combine(solutionRoot, "Engine", "Engine.Core", "bin", "Debug", "net8.0", "Engine.Core.dll");
                var engineCoreDest = Path.Combine(projectBinDir, "Engine.Core.dll");
                if (File.Exists(engineCoreSource))
                {
                    File.Copy(engineCoreSource, engineCoreDest, true);
                }
                else
                {
                    MessageBox.Show("Engine.Core.dll not found. Please build the Engine.Core project.", "Missing DLL", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                // 5. Copy Assets to project bin/Assets (optional: or just reference directly)
                var projectBinAssets = Path.Combine(projectBinDir, "Assets");
                CopyDirectory(assetsRoot, projectBinAssets);

                // 6. Save the current room if RoomEditor is open (as before)
                var dockPanel = (DockPanel)Content;
                TabControl tabControl = null;
                foreach (var child in dockPanel.Children)
                {
                    if (child is TabControl tc)
                    {
                        tabControl = tc;
                        break;
                    }
                }
                if (tabControl != null)
                {
                    foreach (TabItem tab in tabControl.Items)
                    {
                        if (tab.Content is UserControl uc && uc is RoomEditor re)
                        {
                            re.SaveRoom();
                            break;
                        }
                    }
                }

                // 7. Launch the runtime from the project bin directory
                // Kill any running game processes before launching
                KillRunningGameProcesses();
                var projectRuntimeExe = Path.Combine(projectBinDir, "GameRuntime.exe");
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = projectRuntimeExe,
                        UseShellExecute = false,
                        WorkingDirectory = projectBinDir
                    }
                };
                process.Start();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An unexpected error occurred:\n{ex}", "Editor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAssetTree()
        {
            // Preserve selected tab index
            var dockPanel = (DockPanel)Content;
            TabControl tabControl = null;
            foreach (var child in dockPanel.Children)
            {
                if (child is TabControl tc)
                {
                    tabControl = tc;
                    break;
                }
            }
            if (tabControl == null)
            {
                // Handle error: TabControl not found
                return;
            }
            int selectedTabIndex = tabControl.SelectedIndex;

            // Store expanded state before clearing
            var expandedPaths = GetExpandedPaths();
            
            AssetTreeView.Items.Clear();
            foreach (var type in assetTypeFolders.Keys)
            {
                var absPath = Path.Combine(assetsRoot, type);
                var typeNode = new TreeViewItem { Header = type, Tag = absPath };
                LoadAssetFolder(typeNode, absPath);
                AssetTreeView.Items.Add(typeNode);
            }
            
            // Restore expanded state
            RestoreExpandedPaths(expandedPaths);

            // Restore selected tab index
            tabControl.SelectedIndex = selectedTabIndex;
        }

        private void LoadAssetFolder(TreeViewItem parent, string folderPath)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Always add all subfolders
            foreach (var dir in Directory.GetDirectories(folderPath))
            {
                var dirName = Path.GetFileName(dir);
                // Add folder icon
                var folderPanel = new StackPanel { Orientation = Orientation.Horizontal };
                var folderIcon = new TextBlock { Text = "📁", FontSize = 14, Margin = new Thickness(0, 0, 5, 0) };
                var folderText = new TextBlock { Text = dirName };
                folderPanel.Children.Add(folderIcon);
                folderPanel.Children.Add(folderText);
                var dirNode = new TreeViewItem { Header = folderPanel, Tag = dir };
                LoadAssetFolder(dirNode, dir);
                parent.Items.Add(dirNode);
            }
            // Add files (show all except .dll)
            var scriptsRoot = Path.Combine(assetsRoot, "Scripts");
            foreach (var file in Directory.GetFiles(folderPath))
            {
                if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;
                var fileName = Path.GetFileName(file);
                var ext = Path.GetExtension(fileName).ToLower();
                // Only show .room files in the Rooms folder
                if (ext == ".room" && !folderPath.EndsWith("Rooms"))
                    continue;
                // Only show .cs files if they are under the Scripts root
                if (ext == ".cs" && !file.StartsWith(scriptsRoot))
                    continue;
                // Add icons for file types
                StackPanel filePanel = new StackPanel { Orientation = Orientation.Horizontal };
                TextBlock icon = new TextBlock { FontSize = 14, Margin = new Thickness(0, 0, 5, 0) };
                if (ext == ".room")
                    icon.Text = "🎮";
                else if (ext == ".cs")
                    icon.Text = "📄";
                else
                    icon.Text = "📦";
                var fileText = new TextBlock { Text = fileName };
                filePanel.Children.Add(icon);
                filePanel.Children.Add(fileText);
                var fileNode = new TreeViewItem { Header = filePanel, Tag = file };
                parent.Items.Add(fileNode);
            }
        }

        private void AssetTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selected = AssetTreeView.SelectedItem as TreeViewItem;
            if (selected != null && selected.Tag != null && File.Exists(selected.Tag.ToString()))
            {
                var ext = Path.GetExtension(selected.Tag.ToString()).ToLower();
                if (ext == ".room")
                {
                    // Set flag to prevent MouseLeftButtonUp from interfering
                    isHandlingRoomSelection = true;
                    // Switch to Rooms tab and load the room
                    var dockPanel = (DockPanel)Content;
                    TabControl tabControl = null;
                    foreach (var child in dockPanel.Children)
                    {
                        if (child is TabControl tc)
                        {
                            tabControl = tc;
                            break;
                        }
                    }
                    if (tabControl == null)
                    {
                        // Handle error: TabControl not found
                        return;
                    }
                    TabItem roomsTab = null;
                    RoomEditor roomEditor = null;
                    // Find the Rooms tab and RoomEditor
                    foreach (TabItem tab in tabControl.Items)
                    {
                        if (tab.Header.ToString() == "Rooms")
                        {
                            roomsTab = tab;
                            roomEditor = tab.Content as RoomEditor;
                            break;
                        }
                    }
                    if (roomsTab != null && roomEditor != null)
                    {
                        // Extract just the room name from the file path
                        var roomName = Path.GetFileNameWithoutExtension(selected.Tag.ToString());
                        roomEditor.LoadRoom(roomName);
                        // Use SelectedIndex instead of SelectedItem for more reliable tab switching
                        tabControl.SelectedIndex = 1; // Rooms tab is at index 1
                        // Workaround: set again after a short delay to fight WPF focus issues
                        Dispatcher.InvokeAsync(() => { tabControl.SelectedIndex = 1; }, System.Windows.Threading.DispatcherPriority.Background);
                        // Force focus to the room editor to prevent tab switching back
                        roomEditor.Focus();
                    }
                    // Reset flag after a short delay to allow MouseLeftButtonUp to complete
                    Task.Delay(100).ContinueWith(_ => {
                        Dispatcher.Invoke(() => {
                            isHandlingRoomSelection = false;
                        });
                    });
                }
            }
        }

        /// <summary>
        /// Show the inspector for the selected asset
        /// </summary>
        /// <param name="assetPath"></param>
        private void ShowInspector(string assetPath)
        {
            InspectorPanel.Children.Clear();

            if (!File.Exists(assetPath))
                return;

            var ext = Path.GetExtension(assetPath).ToLower();
            if (ext == ".cs")
            {
                // Get class name from file name
                var className = Path.GetFileNameWithoutExtension(assetPath);

                // Path to the compiled scripts DLL
                var scriptsDllPath = Path.Combine(Path.GetDirectoryName(assetsRoot), "bin", "Scripts", "GameScripts.dll");
                if (File.Exists(scriptsDllPath))
                {
                    try
                    {
                        var dllBytes = File.ReadAllBytes(scriptsDllPath);
                        var assembly = System.Reflection.Assembly.Load(dllBytes);
                        // Find the type by name (namespace may be needed if present)
                        var type = assembly.GetType(className) ?? assembly.GetTypes().FirstOrDefault(t => t.Name == className);
                        if (type != null)
                        {
                            // Create an instance
                            _inspectorObjectData = Activator.CreateInstance(type);
                        }
                        else
                        {
                            _inspectorObjectData = null;
                            InspectorPanel.Children.Add(new TextBlock { Text = $"Class '{className}' not found in GameScripts.dll.", Foreground = Brushes.Red });
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _inspectorObjectData = null;
                        InspectorPanel.Children.Add(new TextBlock { Text = $"Error loading script: {ex.Message}", Foreground = Brushes.Red });
                        return;
                    }
                }
                else
                {
                    _inspectorObjectData = null;
                    InspectorPanel.Children.Add(new TextBlock { Text = $"GameScripts.dll not found. Please compile your scripts.", Foreground = Brushes.Red });
                    return;
                }

                // Now reflect on the instance as before
                var fields = _inspectorObjectData.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    var label = new TextBlock { Text = field.Name };
                    FrameworkElement editor = null;
                    if (field.FieldType == typeof(int) || field.FieldType == typeof(float) || field.FieldType == typeof(string))
                    {
                        var tb = new TextBox { Text = field.GetValue(_inspectorObjectData)?.ToString() ?? "" };
                        tb.LostFocus += (s, e) => {
                            object value = Convert.ChangeType(tb.Text, field.FieldType);
                            field.SetValue(_inspectorObjectData, value);
                        };
                        editor = tb;
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        var cb = new CheckBox { IsChecked = (bool?)field.GetValue(_inspectorObjectData) ?? false };
                        cb.Checked += (s, e) => field.SetValue(_inspectorObjectData, true);
                        cb.Unchecked += (s, e) => field.SetValue(_inspectorObjectData, false);
                        editor = cb;
                    }
                    else if (field.FieldType.IsEnum)
                    {
                        var combo = new ComboBox { ItemsSource = Enum.GetValues(field.FieldType) };
                        combo.SelectedItem = field.GetValue(_inspectorObjectData);
                        combo.SelectionChanged += (s, e) => field.SetValue(_inspectorObjectData, combo.SelectedItem);
                        editor = combo;
                    }
                    InspectorPanel.Children.Add(label);
                    InspectorPanel.Children.Add(editor);
                }
            }
            else if (ext == ".eo")
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

                foreach (var script in _inspectorObjectData.scripts)
                {
                    var sp = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 5, 0, 5) };
                    var scriptLabel = new TextBlock
                    {
                        Text = script,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.LightBlue
                    };
                    sp.Children.Add(scriptLabel);

                    // Load script type from DLL
                    var scriptsDllPath = Path.Combine(Path.GetDirectoryName(assetsRoot), "bin", "Scripts", "GameScripts.dll");
                    if (File.Exists(scriptsDllPath))
                    {
                        var dllBytes = File.ReadAllBytes(scriptsDllPath);
                        var assembly = System.Reflection.Assembly.Load(dllBytes);
                        var type = assembly.GetType(script) ?? assembly.GetTypes().FirstOrDefault(t => t.Name == script);
                        if (type != null)
                        {
                            // Get or create property dictionary
                            if (_inspectorObjectData.scriptProperties == null)
                                _inspectorObjectData.scriptProperties = new Dictionary<string, Dictionary<string, object>>();
                            if (!_inspectorObjectData.scriptProperties.ContainsKey(script))
                                _inspectorObjectData.scriptProperties[script] = new Dictionary<string, object>();
                            var propDict = _inspectorObjectData.scriptProperties[script];

                            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                            {
                                var label = new TextBlock { Text = field.Name };
                                FrameworkElement editor = null;
                                object value = propDict.ContainsKey(field.Name) ? propDict[field.Name] : field.GetValue(Activator.CreateInstance(type));
                                if (field.FieldType == typeof(int) || field.FieldType == typeof(float) || field.FieldType == typeof(string))
                                {
                                    var tb = new TextBox { Text = value?.ToString() ?? "" };
                                    tb.LostFocus += (s, e) =>
                                    {
                                        object newValue = Convert.ChangeType(tb.Text, field.FieldType);
                                        propDict[field.Name] = newValue;
                                        SaveInspectorObject();
                                    };
                                    editor = tb;
                                }
                                else if (field.FieldType == typeof(bool))
                                {
                                    var cb = new CheckBox { IsChecked = value != null && (bool)value };
                                    cb.Checked += (s, e) => { propDict[field.Name] = true; SaveInspectorObject(); };
                                    cb.Unchecked += (s, e) => { propDict[field.Name] = false; SaveInspectorObject(); };
                                    editor = cb;
                                }
                                else if (field.FieldType.IsEnum)
                                {
                                    var combo = new ComboBox { ItemsSource = Enum.GetValues(field.FieldType) };
                                    combo.SelectedItem = value;
                                    combo.SelectionChanged += (s, e) =>
                                    {
                                        propDict[field.Name] = combo.SelectedItem;
                                        SaveInspectorObject();
                                    };
                                    editor = combo;
                                }
                                else if (field.FieldType == typeof(Microsoft.Xna.Framework.Vector2))
                                {
                                    // Special handling for Vector2
                                    Microsoft.Xna.Framework.Vector2 vector;
                                    if (value is Microsoft.Xna.Framework.Vector2 v)
                                    {
                                        vector = v;
                                    }
                                    else if (value is JsonElement je && je.TryGetProperty("X", out var xElem) && je.TryGetProperty("Y", out var yElem))
                                    {
                                        vector = new Microsoft.Xna.Framework.Vector2(
                                            xElem.GetSingle(),
                                            yElem.GetSingle()
                                        );
                                    }
                                    else
                                    {
                                        vector = new Microsoft.Xna.Framework.Vector2();
                                    }
                                    var vectorPanel = new StackPanel { Orientation = Orientation.Horizontal };
                                    var xBox = new TextBox { Text = vector.X.ToString(), Width = 50, Margin = new Thickness(0, 0, 5, 0) };
                                    var yBox = new TextBox { Text = vector.Y.ToString(), Width = 50 };
                                    xBox.LostFocus += (s, e) =>
                                    {
                                        if (float.TryParse(xBox.Text, out float x))
                                        {
                                            vector.X = x;
                                            propDict[field.Name] = vector;
                                            SaveInspectorObject();
                                        }
                                    };
                                    yBox.LostFocus += (s, e) =>
                                    {
                                        if (float.TryParse(yBox.Text, out float y))
                                        {
                                            vector.Y = y;
                                            propDict[field.Name] = vector;
                                            SaveInspectorObject();
                                        }
                                    };
                                    vectorPanel.Children.Add(new TextBlock { Text = "X:", Margin = new Thickness(0, 0, 5, 0) });
                                    vectorPanel.Children.Add(xBox);
                                    vectorPanel.Children.Add(new TextBlock { Text = "Y:", Margin = new Thickness(10, 0, 5, 0) });
                                    vectorPanel.Children.Add(yBox);
                                    editor = vectorPanel;
                                }

                                // Add the editor if possible
                                if (editor != null)
                                {
                                    sp.Children.Add(label);
                                    sp.Children.Add(editor);
                                }
                            }
                        }
                    }

                    // Remove button
                    var removeBtn = new Button
                    {
                        Content = "\uE74D", // Trash icon
                        FontFamily = new FontFamily("Segoe MDL2 Assets"),
                        Tag = script,
                        Margin = new Thickness(5, 5, 0, 0),
                        Style = (Style)Application.Current.Resources["DangerButton"],
                        FontSize = 16,
                        Width = 32,
                        Height = 32,
                        Padding = new Thickness(0),
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    removeBtn.Click += (s, e) =>
                    {
                        _inspectorObjectData.scripts.Remove(script);
                        _inspectorObjectData.scriptProperties.Remove(script);
                        SaveInspectorObject();
                        ShowInspector(_inspectorObjectPath);
                    };
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
                        Name = Path.GetFileNameWithoutExtension(assetPath),
                        frameWidth = bmp.PixelWidth,
                        frameHeight = bmp.PixelHeight,
                        frameCount = 1,
                        frameSpeed = 1,
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
                        spriteData.frameSpeed = (int)speed; 
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
                    // Ensure objectPath is the correct path to the object file
                    if (!File.Exists(objectPath))
                    {
                        MessageBox.Show($"Object file not found: {objectPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // Get the relative path from the Scripts folder
                    var scriptsRoot = Path.Combine(assetsRoot, "Scripts");
                    string scriptName;
                    
                    if (scriptPath.StartsWith(scriptsRoot))
                    {
                        // Get just the class name (filename without extension), not the folder path
                        scriptName = Path.GetFileNameWithoutExtension(scriptPath);
                    }
                    else
                    {
                        // Fallback to just filename if not in Scripts folder
                        scriptName = Path.GetFileNameWithoutExtension(scriptPath);
                    }
                    
                    try
                    {
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
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to attach script: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var dockPanel = (DockPanel)Content;
                TabControl tabControl = null;
                foreach (var child in dockPanel.Children)
                {
                    if (child is TabControl tc)
                    {
                        tabControl = tc;
                        break;
                    }
                }
                if (tabControl == null)
                {
                    // Handle error: TabControl not found
                    return;
                }
                foreach (TabItem tab in tabControl.Items)
                {
                    if (tab.Content is UserControl uc && uc is RoomEditor re)
                    {
                        re.SaveRoom();
                        break;
                    }
                }

                // Save game options if GameOptionsEditor is open
                if (tabControl != null)
                {
                    foreach (TabItem tab in tabControl.Items)
                    {
                        if (tab.Content is UserControl uc && uc is GameOptionsEditor goe)
                        {
                            goe.SaveGameOptions();
                            break;
                        }
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
        private Point? dragStartPoint = null;
        
        private void AssetTreeView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                dragStartPoint = e.GetPosition(null);
            }
        }
        
        private void AssetTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Only start dragging if we've moved a minimum distance (to distinguish from clicks)
                if (dragStartPoint.HasValue)
                {
                    var distance = Math.Sqrt(Math.Pow(e.GetPosition(null).X - dragStartPoint.Value.X, 2) + 
                                           Math.Pow(e.GetPosition(null).Y - dragStartPoint.Value.Y, 2));
                    
                    if (distance > 5) // Minimum drag distance of 5 pixels
                    {
                        var treeView = sender as TreeView;
                        var item = treeView?.SelectedItem as TreeViewItem;
                        if (item != null && File.Exists(item.Tag?.ToString()))
                        {
                            // Make all assets draggable, not just scripts
                            isDraggingScript = true;
                            isDraggingAsset = true;
                            DragDrop.DoDragDrop(treeView, item.Tag.ToString(), DragDropEffects.Copy);
                            isDraggingScript = false;
                            isDraggingAsset = false;
                            dragStartPoint = null; // Reset drag start point
                        }
                    }
                }
            }
            else
            {
                dragStartPoint = null; // Reset when mouse button is released
            }
        }
        private void AssetTreeView_Drop(object sender, DragEventArgs e)
        {
            // Only process drops if we're actually dragging an asset
            if (!isDraggingAsset) return;
            
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var sourcePath = e.Data.GetData(DataFormats.StringFormat) as string;
                if (string.IsNullOrEmpty(sourcePath)) return;
                
                var targetItem = GetTreeViewItemAtPosition(e.GetPosition(AssetTreeView));
                if (targetItem == null) return;
                
                var targetPath = targetItem.Tag?.ToString();
                if (string.IsNullOrEmpty(targetPath)) return;
                
                try
                {
                    // If target is a directory, move the file into it
                    if (Directory.Exists(targetPath))
                    {
                        var fileName = Path.GetFileName(sourcePath);
                        var destPath = Path.Combine(targetPath, fileName);
                        
                        if (File.Exists(sourcePath))
                        {
                            File.Move(sourcePath, destPath);
                        }
                        else if (Directory.Exists(sourcePath))
                        {
                            // Move directory
                            var destDir = Path.Combine(targetPath, Path.GetFileName(sourcePath));
                            if (!Directory.Exists(destDir))
                            {
                                Directory.Move(sourcePath, destDir);
                                
                                // Update stored paths if the moved directory contains the currently inspected object
                                if (!string.IsNullOrEmpty(_lastInspectedObjectPath) && _lastInspectedObjectPath.StartsWith(sourcePath))
                                {
                                    var relativePath = _lastInspectedObjectPath.Substring(sourcePath.Length);
                                    _lastInspectedObjectPath = Path.Combine(destDir, relativePath.TrimStart('\\', '/'));
                                    _inspectorObjectPath = _lastInspectedObjectPath;
                                }
                            }
                        }
                        
                        // Update stored paths if the moved asset was the currently inspected object
                        if (_lastInspectedObjectPath == sourcePath)
                        {
                            _lastInspectedObjectPath = destPath;
                            _inspectorObjectPath = destPath;
                        }
                        
                        LoadAssetTree(); // Refresh the tree
                        e.Handled = true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to move asset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
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
        
        private void AssetTreeView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // If we're handling a room selection, don't interfere
            if (isHandlingRoomSelection)
            {
                return;
            }
            // Handle asset selection on mouse release (not drag)
            if (isDraggingScript) 
            {
                return;
            }
            var selected = AssetTreeView.SelectedItem as TreeViewItem;
            if (selected != null && selected.Tag != null && File.Exists(selected.Tag.ToString()))
            {
                var ext = Path.GetExtension(selected.Tag.ToString()).ToLower();
                if (ext == ".room")
                {
                    // Rooms are handled in SelectedItemChanged, so we don't need to do anything here
                    return;
                }
                else if (ext == ".cs")
                {
                    // Show script in inspector for editing
                    ShowInspector(selected.Tag.ToString());
                }
                else if (ext == ".eo" || ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                {
                    // Show other assets in inspector
                    ShowInspector(selected.Tag.ToString());
                }
            }
            // Check if we're currently on the Rooms tab and if so, don't do anything that might switch back
            var dockPanel = (DockPanel)Content;
            TabControl tabControl = null;
            foreach (var child in dockPanel.Children)
            {
                if (child is TabControl tc)
                {
                    tabControl = tc;
                    break;
                }
            }
            if (tabControl == null)
            {
                // Handle error: TabControl not found
                return;
            }
            if (tabControl.SelectedIndex == 1) // Rooms tab
            {
                return;
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
            var assetName = PromptDialog("Enter new script name:", "NewScript");

            if (!string.IsNullOrWhiteSpace(assetName))
            {
                var newAssetPath = Path.Combine(parentDir, assetName + ".cs");

                if (!File.Exists(newAssetPath))
                {
                    var classContent = ScriptTemplate.Replace("$CLASS$", assetName);
                    File.WriteAllText(newAssetPath, classContent);
                }

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
                var dockPanel = (DockPanel)Content;
                TabControl tabControl = null;
                foreach (var child in dockPanel.Children)
                {
                    if (child is TabControl tc)
                    {
                        tabControl = tc;
                        break;
                    }
                }
                if (tabControl == null)
                {
                    // Handle error: TabControl not found
                    return;
                }
                foreach (TabItem tab in tabControl.Items)
                {
                    if (tab.Content is UserControl uc && uc is RoomEditor re)
                    {
                        // Preserve scroll position before reloading
                        var treeView = re.GetType().GetField("ObjectTreeView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(re) as TreeView;
                        double scrollPosition = 0;
                        if (treeView != null)
                        {
                            var scrollViewer = GetScrollViewer(treeView);
                            if (scrollViewer != null)
                                scrollPosition = scrollViewer.VerticalOffset;
                        }
                        
                        // Reload the object list
                        re.GetType().GetMethod("LoadObjectList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(re, null);
                        
                        // Restore scroll position
                        if (treeView != null && scrollPosition > 0)
                        {
                            var scrollViewer = GetScrollViewer(treeView);
                            if (scrollViewer != null)
                                scrollViewer.ScrollToVerticalOffset(scrollPosition);
                        }
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
            var currentExt = Path.GetExtension(currentName);
            var newName = PromptDialog($"Enter new name:", "Rename", Path.GetFileNameWithoutExtension(currentName));
            if (!string.IsNullOrWhiteSpace(newName))
            {
                // If file, append extension if user omitted it
                if (isFile && !newName.EndsWith(currentExt, StringComparison.OrdinalIgnoreCase))
                    newName += currentExt;
                if (newName != currentName)
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

        // Hot reload functionality
        private void HotReloadScript(string scriptPath)
        {
            try
            {
                // Compile the single script
                var compiler = new Engine.Core.ScriptCompiler();
                var scriptsDir = Path.Combine(assetsRoot, "Scripts");
                var editorBinScripts = Path.GetFullPath(Path.Combine(scriptsDir, "..", "..", "bin", "Scripts"));
                Directory.CreateDirectory(editorBinScripts);
                var dllPath = Path.Combine(editorBinScripts, "GameScripts.dll");
                var result = compiler.CompileScripts(scriptsDir, dllPath);
                if (!result.Success)
                {
                    MessageBox.Show($"Script compilation failed:\n{string.Join("\n", result.Errors)}", "Hot Reload Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // Copy DLL to runtime if game is running
                var solutionRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
                var tempDllPath = Path.Combine(editorBinScripts, "GameScripts_temp.dll");
                File.Copy(dllPath, tempDllPath, true); // Always copy to temp before loading
                // Use tempDllPath for any reflection/hot reload logic here
                var runtimeScriptsDir = Path.Combine(solutionRoot, "Runtime", "bin", "Debug", "net8.0-windows", "Scripts");
                if (Directory.Exists(runtimeScriptsDir))
                {
                    var destDllPath = Path.Combine(runtimeScriptsDir, "GameScripts.dll");
                    File.Copy(dllPath, destDllPath, true);
                    Console.WriteLine($"[Hot Reload] Script {Path.GetFileName(scriptPath)} compiled and copied to runtime");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Hot Reload] Error: {ex.Message}");
            }
        }
        
        // Asset tree state management
        private HashSet<string> GetExpandedPaths()
        {
            var expandedPaths = new HashSet<string>();
            CollectExpandedPaths(AssetTreeView.Items, expandedPaths);
            return expandedPaths;
        }
        
        private void CollectExpandedPaths(ItemCollection items, HashSet<string> expandedPaths)
        {
            foreach (TreeViewItem item in items)
            {
                if (item.IsExpanded && item.Tag != null)
                {
                    expandedPaths.Add(item.Tag.ToString());
                }
                CollectExpandedPaths(item.Items, expandedPaths);
            }
        }
        
        private void RestoreExpandedPaths(HashSet<string> expandedPaths)
        {
            RestoreExpandedPathsRecursive(AssetTreeView.Items, expandedPaths);
        }
        
        private void RestoreExpandedPathsRecursive(ItemCollection items, HashSet<string> expandedPaths)
        {
            foreach (TreeViewItem item in items)
            {
                if (item.Tag != null && expandedPaths.Contains(item.Tag.ToString()))
                {
                    item.IsExpanded = true;
                }
                RestoreExpandedPathsRecursive(item.Items, expandedPaths);
            }
        }
        

        
        private TreeViewItem GetTreeViewItemAtPosition(Point position)
        {
            var element = AssetTreeView.InputHitTest(position) as DependencyObject;
            while (element != null && !(element is TreeViewItem))
            {
                element = VisualTreeHelper.GetParent(element);
            }
            return element as TreeViewItem;
        }

        private static ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer scrollViewer)
                return scrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private RichTextBox CreateSyntaxHighlightingEditor(string content)
        {
            var richTextBox = new RichTextBox
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Height = 400,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1)
            };
            
            // Set compact line height
            richTextBox.Document.LineHeight = 8;

            // Set the content with basic syntax highlighting
            SetBasicSyntaxHighlighting(richTextBox, content);

            return richTextBox;
        }

        private void SetBasicSyntaxHighlighting(RichTextBox richTextBox, string content)
        {
            var paragraph = new Paragraph();
            
            // C# syntax highlighting patterns
            var patterns = new Dictionary<string, Brush>
            {
                // Keywords
                { @"\b(using|namespace|class|public|private|protected|internal|static|void|int|float|double|string|bool|var|if|else|for|while|foreach|return|new|this|base|null|true|false|override|virtual|abstract|interface|enum|struct|try|catch|finally|throw|using|namespace|partial|sealed|readonly|const|out|ref|in|params|where|select|from|let|group|into|orderby|join|on|equals|by|ascending|descending)\b", new SolidColorBrush(Colors.LightBlue) },
                
                // Types
                { @"\b(GameScript|GameObject|Vector2|Vector3|Color|Rectangle|Texture2D|SpriteBatch|GameTime|Keys|Mouse|Input|Camera|RoomManager|ScriptManager|Engine\.Core)\b", new SolidColorBrush(Colors.LightGreen) },
                
                // Strings
                { @"""[^""]*""", new SolidColorBrush(Colors.LightCoral) },
                
                // Comments
                { @"//.*$", new SolidColorBrush(Colors.Gray) },
                { @"/\*.*?\*/", new SolidColorBrush(Colors.Gray) },
                
                // Numbers
                { @"\b\d+\.?\d*\b", new SolidColorBrush(Colors.LightYellow) },
                
                // Method calls
                { @"\b\w+(?=\()", new SolidColorBrush(Colors.LightCyan) }
            };

            // Apply syntax highlighting to the entire content
            var highlightedRuns = ApplySyntaxHighlighting(content, patterns);
            
            // Add all highlighted runs to the paragraph
            foreach (var run in highlightedRuns)
            {
                paragraph.Inlines.Add(run);
            }
            
            richTextBox.Document.Blocks.Clear();
            richTextBox.Document.Blocks.Add(paragraph);
        }

        private List<Inline> ApplySyntaxHighlighting(string content, Dictionary<string, Brush> patterns)
        {
            var result = new List<Inline>();
            var lines = content.Split('\n');
            
            foreach (var line in lines)
            {
                var lineRuns = new List<Inline>();
                var currentPosition = 0;
                var lineText = line;
                
                // Find all matches for this line
                var allMatches = new List<(int index, int length, Brush brush, string text)>();
                
                foreach (var pattern in patterns)
                {
                    var regex = new Regex(pattern.Key, RegexOptions.Compiled);
                    var matches = regex.Matches(lineText);
                    
                    foreach (Match match in matches)
                    {
                        allMatches.Add((match.Index, match.Length, pattern.Value, match.Value));
                    }
                }
                
                // Sort matches by position
                allMatches.Sort((a, b) => a.index.CompareTo(b.index));
                
                // Process matches in order
                foreach (var match in allMatches)
                {
                    // Add text before this match
                    if (match.index > currentPosition)
                    {
                        var beforeText = lineText.Substring(currentPosition, match.index - currentPosition);
                        if (!string.IsNullOrEmpty(beforeText))
                        {
                            lineRuns.Add(new Run(beforeText) { Foreground = new SolidColorBrush(Colors.White) });
                        }
                    }
                    
                    // Add the highlighted match
                    lineRuns.Add(new Run(match.text) { Foreground = match.brush });
                    currentPosition = match.index + match.length;
                }
                
                // Add remaining text after last match
                if (currentPosition < lineText.Length)
                {
                    var remainingText = lineText.Substring(currentPosition);
                    if (!string.IsNullOrEmpty(remainingText))
                    {
                        lineRuns.Add(new Run(remainingText) { Foreground = new SolidColorBrush(Colors.White) });
                    }
                }
                
                // If no highlighting was applied, add the original line
                if (lineRuns.Count == 0)
                {
                    lineRuns.Add(new Run(lineText) { Foreground = new SolidColorBrush(Colors.White) });
                }
                
                // Add all runs for this line
                result.AddRange(lineRuns);
                
                // Add line break (except for the last line)
                if (line != lines[lines.Length - 1])
                {
                    result.Add(new LineBreak());
                }
            }
            
            return result;
        }

        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Always create the destination directory
            if (!Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);

            // Copy all files
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            // Recursively copy subdirectories (even if empty)
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }

        private void HomeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var welcome = new WelcomeWindow();
            welcome.Show();
            this.Close();
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void KillRunningGameProcesses()
        {
            try
            {
                var processes = System.Diagnostics.Process.GetProcessesByName("GameRuntime");
                foreach (var proc in processes)
                {
                    proc.Kill();
                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to close running game processes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void GenerateScriptsCsproj(string projectRoot)
        {
            var projectName = Path.GetFileName(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var csprojPath = Path.Combine(projectRoot, $"{projectName}.csproj");
            var engineCoreSource = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Engine", "Engine.Core", "bin", "Debug", "net8.0", "Engine.Core.dll"));
            var engineCoreDest = Path.Combine(projectRoot, "Engine.Core.dll");
            if (File.Exists(engineCoreSource))
                File.Copy(engineCoreSource, engineCoreDest, true);
            var engineCoreXml = engineCoreSource.Replace(".dll", ".xml");
            var engineCorePdb = engineCoreSource.Replace(".dll", ".pdb");
            if (File.Exists(engineCoreXml))
                File.Copy(engineCoreXml, Path.Combine(projectRoot, "Engine.Core.xml"), true);
            if (File.Exists(engineCorePdb))
                File.Copy(engineCorePdb, Path.Combine(projectRoot, "Engine.Core.pdb"), true);

            // Load csproj template from file
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ProjectTemplates", "ScriptsProject.csproj.template");
            var csprojTemplate = File.ReadAllText(templatePath);
            var iconPath = "Assets/icon.ico";
            var csprojContent = csprojTemplate
                .Replace("${PROJECT_NAME}", projectName)
                .Replace("${ICON_PATH}", iconPath);
            File.WriteAllText(csprojPath, csprojContent);
        }

        private void OpenScriptsInVisualStudio(string projectRoot)
        {
            var projectName = Path.GetFileName(projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var csprojPath = Path.Combine(projectRoot, $"{projectName}.csproj");
            if (File.Exists(csprojPath))
            {
                // Try to find Visual Studio installation
                var vsPath = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe";
                if (!File.Exists(vsPath))
                {
                    // Try Professional edition
                    vsPath = @"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe";
                }
                if (!File.Exists(vsPath))
                {
                    // Try Enterprise edition
                    vsPath = @"C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe";
                }
                if (!File.Exists(vsPath))
                {
                    // Try 2019 Community
                    vsPath = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe";
                }
                if (!File.Exists(vsPath))
                {
                    // Fallback to default application (cmd start)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = $"/c start \"\" \"{csprojPath}\"",
                        UseShellExecute = false
                    });
                    return;
                }

                // Open the project file directly with Visual Studio
                Process.Start(new ProcessStartInfo
                {
                    FileName = vsPath,
                    Arguments = $"\"{csprojPath}\"",
                    UseShellExecute = true,
                    WorkingDirectory = projectRoot
                });
            }
            else
            {
                MessageBox.Show($"{projectName}.csproj not found. Please create a script first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenVSButton_Click(object sender, RoutedEventArgs e)
        {
            // Assume assetsRoot is always set to the current project's Assets folder
            var projectRoot = Path.GetDirectoryName(assetsRoot);
            OpenScriptsInVisualStudio(projectRoot);
        }

        private void OnAssetsChanged(object sender, FileSystemEventArgs e)
        {
            // Debounce: only reload if 200ms have passed since last reload
            if ((DateTime.Now - _lastReload).TotalMilliseconds < 200)
                return;
            _lastReload = DateTime.Now;

            // Check if a script file was deleted and recompile if needed
            if (e.ChangeType == WatcherChangeTypes.Deleted && e.FullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[FileWatcher] Script file deleted: {e.FullPath}");
                RecompileScripts();
            }

            Dispatcher.Invoke(() =>
            {
                LoadAssetTree();
                // Reload open editors/inspectors if their file was affected
                ReloadOpenEditors(e.FullPath);
            });
        }

        private void RecompileScripts()
        {
            try
            {
                Console.WriteLine("[FileWatcher] Recompiling scripts after file deletion...");
                var compiler = new Engine.Core.ScriptCompiler();
                var scriptsDir = Path.Combine(assetsRoot, "Scripts");
                var projectBinDir = Path.Combine(Path.GetDirectoryName(assetsRoot), "bin");
                Directory.CreateDirectory(projectBinDir);
                var projectBinScripts = Path.Combine(projectBinDir, "Scripts");
                Directory.CreateDirectory(projectBinScripts);
                var dllPath = Path.Combine(projectBinScripts, "GameScripts.dll");
                
                var result = compiler.CompileScripts(scriptsDir, dllPath);
                if (result.Success)
                {
                    Console.WriteLine("[FileWatcher] Scripts recompiled successfully after file deletion");
                }
                else
                {
                    Console.WriteLine($"[FileWatcher] Script recompilation failed: {string.Join(", ", result.Errors)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWatcher] Error recompiling scripts: {ex.Message}");
            }
        }

        private void ReloadOpenEditors(string changedPath)
        {
            // Find the TabControl
            var dockPanel = (DockPanel)Content;
            TabControl tabControl = null;
            foreach (var child in dockPanel.Children)
            {
                if (child is TabControl tc)
                {
                    tabControl = tc;
                    break;
                }
            }
            if (tabControl != null)
            {
                foreach (TabItem tab in tabControl.Items)
                {
                    if (tab.Content is RoomEditor roomEditor)
                    {
                        // If a room file changed, reload the current room
                        if (changedPath.EndsWith(".room", StringComparison.OrdinalIgnoreCase))
                        {
                            var currentRoom = roomEditor.GetCurrentRoomName();
                            if (!string.IsNullOrEmpty(currentRoom))
                                roomEditor.LoadRoom(currentRoom);
                        }
                        // Always reload the object list in case objects/scripts changed
                        roomEditor.LoadObjectList();
                    }
                    else if (tab.Content is GameOptionsEditor gameOptionsEditor)
                    {
                        // Reload rooms and game options if relevant files changed
                        if (changedPath.EndsWith("game_options.json", StringComparison.OrdinalIgnoreCase) ||
                            changedPath.EndsWith(".room", StringComparison.OrdinalIgnoreCase))
                        {
                            gameOptionsEditor.LoadRooms();
                            gameOptionsEditor.LoadGameOptions();
                        }
                    }
                }
            }
            // Optionally, reload inspector if it references the changed file
            if (!string.IsNullOrEmpty(_inspectorObjectPath) && changedPath == _inspectorObjectPath)
            {
                ShowInspector(_inspectorObjectPath);
            }
        }

        // Add this handler for F2 rename
        private void AssetTreeView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2)
            {
                // Only trigger if an item is selected
                var selected = AssetTreeView.SelectedItem as TreeViewItem;
                if (selected != null)
                {
                    RenameAsset_Click(selected, null);
                    e.Handled = true;
                }
            }
        }
    }
} 