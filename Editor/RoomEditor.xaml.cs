using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Editor
{
    public partial class RoomEditor : UserControl
    {
        private readonly string assetsRoot;
        private readonly string roomsDir;
        private readonly string gameOptionsPath;
        
        private string currentRoomName = "";
        private bool isDragging = false;
        private Point dragStart;
        private UIElement draggedElement;
        private Point originalPosition;
        private double currentZoom = 1.0;
        
        // Data classes
        public class EarthObject
        {
            public string name { get; set; }
            public string sprite { get; set; }
            public List<string> scripts { get; set; } = new List<string>();
        }
        
        public class RoomObject
        {
            public string objectPath { get; set; }
            public double x { get; set; }
            public double y { get; set; }
        }
        
        public class Room
        {
            public string name { get; set; }
            public string background { get; set; } = "";
            public bool backgroundTiled { get; set; } = false;
            public int width { get; set; } = 800;
            public int height { get; set; } = 600;
            public List<RoomObject> objects { get; set; } = new List<RoomObject>();
        }
        
        // 1. Add a field for tiled background toggle
        private bool showTiledBackground = true;
        
        public RoomEditor()
        {
            InitializeComponent();
            assetsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets");
            roomsDir = Path.Combine(assetsRoot, "Rooms");
            gameOptionsPath = Path.Combine(assetsRoot, "game_options.json");
            
            // Create directories if they don't exist
            Directory.CreateDirectory(roomsDir);
            
            LoadObjectList();
            LoadRooms();
            
            ObjectTreeView.PreviewMouseMove += ObjectTreeView_PreviewMouseMove;
            RoomCanvas.Drop += RoomCanvas_Drop;
            
            // Add Ctrl+S hotkey for saving
            this.PreviewKeyDown += RoomEditor_PreviewKeyDown;
            this.Focusable = true;
            
            // Add a checkbox to toggle the tiled background
            var tileCheckbox = new CheckBox { Content = "Show Tiled Background", IsChecked = showTiledBackground, Margin = new Thickness(5) };
            tileCheckbox.Checked += (s, e) => { showTiledBackground = true; RoomCanvas.InvalidateVisual(); };
            tileCheckbox.Unchecked += (s, e) => { showTiledBackground = false; RoomCanvas.InvalidateVisual(); };
            var parentPanel = this.Content as Panel;
            if (parentPanel != null) parentPanel.Children.Insert(0, tileCheckbox);

            // Add event handlers for room size controls
            RoomWidthTextBox.LostFocus += RoomSize_Changed;
            RoomHeightTextBox.LostFocus += RoomSize_Changed;

            // Populate background ComboBox with sprites after component is loaded
            this.Loaded += (s, e) => RefreshBackgroundComboBox();
        }
        
        private void LoadObjectList()
        {
            ObjectTreeView.Items.Clear();
            var objectsDir = Path.Combine(assetsRoot, "Objects");
            if (Directory.Exists(objectsDir))
            {
                LoadObjectsRecursively(objectsDir, null);
            }
        }
        
        private void LoadObjectsRecursively(string directory, TreeViewItem parentNode)
        {
            // Load objects from current directory
            foreach (var file in Directory.GetFiles(directory, "*.eo"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                
                // Create object header with icon
                var objectPanel = new StackPanel { Orientation = Orientation.Horizontal };
                var objectIcon = new TextBlock { Text = "🎮", FontSize = 14, Margin = new Thickness(0, 0, 5, 0) };
                var objectText = new TextBlock { Text = name };
                objectPanel.Children.Add(objectIcon);
                objectPanel.Children.Add(objectText);
                
                var item = new TreeViewItem { Header = objectPanel, Tag = file };
                
                if (parentNode != null)
                    parentNode.Items.Add(item);
                else
                    ObjectTreeView.Items.Add(item);
            }
            
            // Recursively load from subdirectories
            foreach (var subDir in Directory.GetDirectories(directory))
            {
                var folderName = Path.GetFileName(subDir);
                
                // Create folder header with icon
                var folderPanel = new StackPanel { Orientation = Orientation.Horizontal };
                var folderIcon = new TextBlock { Text = "📁", FontSize = 14, Margin = new Thickness(0, 0, 5, 0) };
                var folderText = new TextBlock { Text = folderName };
                folderPanel.Children.Add(folderIcon);
                folderPanel.Children.Add(folderText);
                
                var folderNode = new TreeViewItem { Header = folderPanel };
                
                if (parentNode != null)
                    parentNode.Items.Add(folderNode);
                else
                    ObjectTreeView.Items.Add(folderNode);
                
                LoadObjectsRecursively(subDir, folderNode);
            }
        }
        
        private void LoadRooms()
        {

        }
        
        public void LoadRoom(string roomName)
        {
            currentRoomName = roomName;
            RoomCanvas.Children.Clear();
            CanvasInfoText.Visibility = Visibility.Visible;
            
            var roomPath = Path.Combine(roomsDir, $"{roomName}.room");
            if (File.Exists(roomPath))
            {
                try
                {
                    var json = File.ReadAllText(roomPath);
                    var room = JsonSerializer.Deserialize<Room>(json);
                    if (room != null)
                    {
                        // Load background settings
                        if (BackgroundComboBox != null)
                            BackgroundComboBox.SelectedItem = room.background;
                        if (BackgroundTiledCheckBox != null)
                            BackgroundTiledCheckBox.IsChecked = room.backgroundTiled;
                        
                        // Load room size settings
                        if (RoomWidthTextBox != null)
                            RoomWidthTextBox.Text = room.width.ToString();
                        if (RoomHeightTextBox != null)
                            RoomHeightTextBox.Text = room.height.ToString();
                        
                        // Apply room size to canvas
                        ApplyRoomSize(room.width, room.height);
                        
                        foreach (var roomObj in room.objects)
                        {
                            AddObjectToCanvas(roomObj.objectPath, roomObj.x, roomObj.y);
                        }
                        CanvasInfoText.Visibility = room.objects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load room: {ex.Message}");
                }
            }
            else
            {
                // New room - clear background settings and set default size
                if (BackgroundComboBox != null)
                    BackgroundComboBox.SelectedItem = null;
                if (BackgroundTiledCheckBox != null)
                    BackgroundTiledCheckBox.IsChecked = false;
                if (RoomWidthTextBox != null)
                    RoomWidthTextBox.Text = "800";
                if (RoomHeightTextBox != null)
                    RoomHeightTextBox.Text = "600";
                ApplyRoomSize(800, 600);
            }
        }
        
        public string GetCurrentRoomName()
        {
            return currentRoomName;
        }
        
        public void SaveRoom()
        {
            if (string.IsNullOrEmpty(currentRoomName))
                return;
                
            var room = new Room { name = currentRoomName };
            
            // Save background settings
            room.background = BackgroundComboBox?.SelectedItem as string ?? "";
            room.backgroundTiled = BackgroundTiledCheckBox?.IsChecked ?? false;
            
            // Save room size settings
            if (int.TryParse(RoomWidthTextBox?.Text ?? "800", out int width))
                room.width = width;
            if (int.TryParse(RoomHeightTextBox?.Text ?? "600", out int height))
                room.height = height;
            
            foreach (UIElement element in RoomCanvas.Children)
            {
                if (element is AnimatedSpriteDisplay animatedSprite && animatedSprite.Tag is string objectPath)
                {
                    room.objects.Add(new RoomObject
                    {
                        objectPath = objectPath,
                        x = Canvas.GetLeft(animatedSprite) + animatedSprite.Width / 2,
                        y = Canvas.GetTop(animatedSprite) + animatedSprite.Height / 2
                    });
                }
            }
            
            var roomPath = Path.Combine(roomsDir, $"{currentRoomName}.room");
            try
            {
                var json = JsonSerializer.Serialize(room, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(roomPath, json);
                // Silent save, no popup
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save room: {ex.Message}");
            }
        }
        
        private void ObjectTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var treeView = sender as TreeView;
                var item = treeView?.SelectedItem as TreeViewItem;
                if (item != null && item.Tag != null) // Only drag items with Tag (object files, not folders)
                {
                    DragDrop.DoDragDrop(treeView, item.Tag.ToString(), DragDropEffects.Copy);
                }
            }
        }
        
        private void RoomCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var assetPath = e.Data.GetData(DataFormats.StringFormat) as string;
                if (File.Exists(assetPath))
                {
                    var ext = Path.GetExtension(assetPath).ToLower();
                    if (ext == ".eo")
                    {
                        // Handle object files
                        var pos = e.GetPosition(RoomCanvas);
                        AddObjectToCanvas(assetPath, pos.X, pos.Y);
                        CanvasInfoText.Visibility = Visibility.Collapsed;
                    }
                    else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                    {
                        // Handle sprite files - create a temporary object
                        var pos = e.GetPosition(RoomCanvas);
                        AddSpriteToCanvas(assetPath, pos.X, pos.Y);
                        CanvasInfoText.Visibility = Visibility.Collapsed;
                    }
                    else if (ext == ".cs")
                    {
                        // Handle script files - could create a script-only object
                        MessageBox.Show("Scripts cannot be placed directly in rooms. Create an object and assign the script to it.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
        
        private void AddObjectToCanvas(string eoPath, double x, double y)
        {
            try
            {
                var json = File.ReadAllText(eoPath);
                var obj = JsonSerializer.Deserialize<EarthObject>(json);
                if (obj != null && !string.IsNullOrWhiteSpace(obj.sprite))
                {
                    var spritePath = Path.Combine(assetsRoot, "Sprites", obj.sprite);
                    if (File.Exists(spritePath))
                    {
                        // Load sprite data if it exists
                        var spriteDataPath = Path.ChangeExtension(spritePath, ".sprite");
                        MainWindow.SpriteData spriteData = null;
                        
                        if (File.Exists(spriteDataPath))
                        {
                            try
                            {
                                var spriteJson = File.ReadAllText(spriteDataPath);
                                spriteData = JsonSerializer.Deserialize<MainWindow.SpriteData>(spriteJson);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to load sprite data: {ex.Message}");
                            }
                        }
                        
                        // Load the image
                        var bmp = new BitmapImage(new Uri(spritePath));
                        
                        // Use frame dimensions if available
                        int displayWidth = spriteData?.frameWidth > 0 ? spriteData.frameWidth : bmp.PixelWidth;
                        int displayHeight = spriteData?.frameHeight > 0 ? spriteData.frameHeight : bmp.PixelHeight;
                        
                        var snappedX = Math.Round(x / 16.0) * 16;
                        var snappedY = Math.Round(y / 16.0) * 16;
                        
                        // Create animated sprite display
                        var animatedSprite = new AnimatedSpriteDisplay();
                        animatedSprite.LoadSprite(spritePath, spriteData);
                        animatedSprite.Width = displayWidth;
                        animatedSprite.Height = displayHeight;
                        animatedSprite.Tag = eoPath;
                        
                        Canvas.SetLeft(animatedSprite, snappedX - displayWidth / 2);
                        Canvas.SetTop(animatedSprite, snappedY - displayHeight / 2);
                        RoomCanvas.Children.Add(animatedSprite);
                        
                        // Add right-click delete functionality
                        animatedSprite.MouseRightButtonUp += (s, e) => {
                            if (MessageBox.Show($"Delete this object?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                            {
                                RoomCanvas.Children.Remove(animatedSprite);
                            }
                            e.Handled = true;
                        };
                        
                        // Add mouse events for dragging
                        animatedSprite.MouseLeftButtonDown += (s, e) => {
                            isDragging = true;
                            draggedElement = animatedSprite;
                            dragStart = e.GetPosition(RoomCanvas);
                            originalPosition = new Point(Canvas.GetLeft(animatedSprite), Canvas.GetTop(animatedSprite));
                            animatedSprite.CaptureMouse();
                            e.Handled = true;
                        };
                    }
                    else
                    {
                        MessageBox.Show($"Sprite file not found: {spritePath}");
                    }
                }
                else
                {
                    MessageBox.Show("Object has no assigned sprite.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add object to room: {ex.Message}");
            }
        }
        
        private void AddSpriteToCanvas(string spritePath, double x, double y)
        {
            try
            {
                if (File.Exists(spritePath))
                {
                    // Load sprite data if it exists
                    var spriteDataPath = Path.ChangeExtension(spritePath, ".sprite");
                    MainWindow.SpriteData spriteData = null;
                    
                    if (File.Exists(spriteDataPath))
                    {
                        try
                        {
                            var spriteJson = File.ReadAllText(spriteDataPath);
                            spriteData = JsonSerializer.Deserialize<MainWindow.SpriteData>(spriteJson);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to load sprite data: {ex.Message}");
                        }
                    }
                    
                    // Load the image
                    var bmp = new BitmapImage(new Uri(spritePath));
                    
                    // Use frame dimensions if available
                    int displayWidth = spriteData?.frameWidth > 0 ? spriteData.frameWidth : bmp.PixelWidth;
                    int displayHeight = spriteData?.frameHeight > 0 ? spriteData.frameHeight : bmp.PixelHeight;
                    
                    var snappedX = Math.Round(x / 16.0) * 16;
                    var snappedY = Math.Round(y / 16.0) * 16;
                    
                    // Create animated sprite display
                    var animatedSprite = new AnimatedSpriteDisplay();
                    animatedSprite.LoadSprite(spritePath, spriteData);
                    animatedSprite.Width = displayWidth;
                    animatedSprite.Height = displayHeight;
                    animatedSprite.Tag = spritePath; // Tag with sprite path for identification
                    
                    Canvas.SetLeft(animatedSprite, snappedX - displayWidth / 2);
                    Canvas.SetTop(animatedSprite, snappedY - displayHeight / 2);
                    RoomCanvas.Children.Add(animatedSprite);
                    
                    // Add right-click delete functionality
                    animatedSprite.MouseRightButtonUp += (s, e) => {
                        if (MessageBox.Show($"Delete this sprite?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            RoomCanvas.Children.Remove(animatedSprite);
                        }
                        e.Handled = true;
                    };
                    
                    // Add mouse events for dragging
                    animatedSprite.MouseLeftButtonDown += (s, e) => {
                        isDragging = true;
                        draggedElement = animatedSprite;
                        dragStart = e.GetPosition(RoomCanvas);
                        originalPosition = new Point(Canvas.GetLeft(animatedSprite), Canvas.GetTop(animatedSprite));
                        animatedSprite.CaptureMouse();
                        e.Handled = true;
                    };
                }
                else
                {
                    MessageBox.Show($"Sprite file not found: {spritePath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add sprite to room: {ex.Message}");
            }
        }
        
        // Mouse events for object movement
        private void RoomCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is AnimatedSpriteDisplay animatedSprite)
            {
                isDragging = true;
                draggedElement = animatedSprite;
                dragStart = e.GetPosition(RoomCanvas);
                originalPosition = new Point(Canvas.GetLeft(animatedSprite), Canvas.GetTop(animatedSprite));
                animatedSprite.CaptureMouse();
                e.Handled = true;
            }
        }
        
        private void RoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && draggedElement is AnimatedSpriteDisplay animatedSprite)
            {
                var mousePos = Mouse.GetPosition(RoomCanvas);
                var snappedX = Math.Round(mousePos.X / 16.0) * 16;
                var snappedY = Math.Round(mousePos.Y / 16.0) * 16;
                Canvas.SetLeft(animatedSprite, snappedX - animatedSprite.Width / 2);
                Canvas.SetTop(animatedSprite, snappedY - animatedSprite.Height / 2);
            }
        }
        
        private void RoomCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging && draggedElement != null)
            {
                isDragging = false;
                draggedElement.ReleaseMouseCapture();
                draggedElement = null;
            }
        }
        
        // Event handlers for UI controls
        private void RoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        
        private void NewRoom_Click(object sender, RoutedEventArgs e)
        {

        }
        
        private void SaveRoom_Click(object sender, RoutedEventArgs e)
        {
            SaveRoom();
        }
        
        private void DeleteRoom_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentRoomName))
                return;
                
            var result = MessageBox.Show($"Are you sure you want to delete room '{currentRoomName}'?", 
                                        "Confirm Delete", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                var roomPath = Path.Combine(roomsDir, $"{currentRoomName}.room");
                if (File.Exists(roomPath))
                {
                    File.Delete(roomPath);
                }
            }
        }
        
        private void RoomEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveRoom();
                e.Handled = true;
            }
        }
        
        private void BrowseBackground_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog 
            { 
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "Select Background Image"
            };
            
            if (dlg.ShowDialog() == true)
            {
                var spritesDir = Path.Combine(assetsRoot, "Sprites");
                var fileName = Path.GetFileName(dlg.FileName);
                var destPath = Path.Combine(spritesDir, fileName);
                
                try
                {
                    File.Copy(dlg.FileName, destPath, overwrite: true);
                    if (BackgroundComboBox != null)
                        BackgroundComboBox.Items.Add(fileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy background image: {ex.Message}");
                }
            }
        }
        
        private void ClearBackground_Click(object sender, RoutedEventArgs e)
        {
            if (BackgroundComboBox != null)
                BackgroundComboBox.SelectedItem = null;
            if (BackgroundTiledCheckBox != null)
                BackgroundTiledCheckBox.IsChecked = false;
        }
        
        private void RefreshBackground_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Manual refresh button clicked");
            RefreshBackgroundComboBox();
        }
        
        private void RefreshBackgroundComboBox()
        {
            try
            {
                var spritesDir = Path.Combine(assetsRoot, "Sprites");
                var currentSelection = BackgroundComboBox?.SelectedItem as string;
                
                if (BackgroundComboBox == null) return;
                
                BackgroundComboBox.Items.Clear();
                if (Directory.Exists(spritesDir))
                {
                    foreach (var file in Directory.GetFiles(spritesDir))
                    {
                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                        {
                            var name = Path.GetFileName(file);
                            BackgroundComboBox.Items.Add(name);
                        }
                    }
                }
                
                // Restore the previous selection if it still exists
                if (!string.IsNullOrEmpty(currentSelection) && BackgroundComboBox.Items.Contains(currentSelection))
                {
                    BackgroundComboBox.SelectedItem = currentSelection;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing background combo box: {ex.Message}");
            }
        }
        
        private void RoomSize_Changed(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(RoomWidthTextBox?.Text ?? "800", out int width) &&
                int.TryParse(RoomHeightTextBox?.Text ?? "600", out int height))
            {
                ApplyRoomSize(width, height);
                SaveRoom(); // Auto-save when size changes
            }
        }
        
        private void ApplyRoomSize(int width, int height)
        {
            // Set the canvas size
            RoomCanvas.Width = width;
            RoomCanvas.Height = height;
            
            // Update the grid overlay size
            if (GridOverlay != null)
            {
                GridOverlay.Width = width;
                GridOverlay.Height = height;
            }
            
            // Update the container size for zooming
            CanvasContainer.Width = width;
            CanvasContainer.Height = height;
        }
        
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ZoomSlider != null)
            {
                currentZoom = ZoomSlider.Value;
                ApplyZoom();
                UpdateZoomText();
            }
        }
        
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (ZoomSlider != null)
            {
                ZoomSlider.Value = Math.Min(ZoomSlider.Maximum, ZoomSlider.Value + 0.25);
            }
        }
        
        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (ZoomSlider != null)
            {
                ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, ZoomSlider.Value - 0.25);
            }
        }
        
        private void RoomCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Zoom with Ctrl+MouseWheel
                double zoomDelta = e.Delta > 0 ? 0.1 : -0.1;
                if (ZoomSlider != null)
                {
                    ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, 
                                               Math.Min(ZoomSlider.Maximum, ZoomSlider.Value + zoomDelta));
                }
                e.Handled = true;
            }
        }
        
        private void ApplyZoom()
        {
            if (CanvasContainer != null)
            {
                CanvasContainer.LayoutTransform = new ScaleTransform(currentZoom, currentZoom);
            }
        }
        
        private void UpdateZoomText()
        {
            if (ZoomText != null)
            {
                ZoomText.Text = $"{(currentZoom * 100):F0}%";
            }
        }
    }
} 