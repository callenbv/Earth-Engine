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
using Editor.Tiles;
using System.Threading.Tasks;

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
        private Room _room;
        private Point originalPosition;
        private double currentZoom = 1.0;
        private int _activeLayer = -1;
        private int _selectedTileId = -1;
        private bool _isPainting;
        private bool _isUpdatingTilesetPicker = false;
        private double _tilePaletteZoom = 1.0;
        private Button _selectedTileButton = null;
        private Dictionary<int, ImageSource> _tileCache = new Dictionary<int, ImageSource>();
        private Dictionary<string, UIElement> _visualTiles = new Dictionary<string, UIElement>();

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
            public int TileSize = 16;
            public List<TileLayer> Layers = new();
            public List<Tileset> Tilesets { get; } = new();

            public Tileset? GetTileset(int id) =>
                Tilesets.FirstOrDefault(t => t.Id == id);

            public void AddLayer(string name)
            {
                var layer = new TileLayer(width, height, name);
                Layers.Add(layer);
            }
            public Tile GetTile(int x, int y, int layerIndex)
            {
                return Layers[layerIndex].Tiles[x, y];
            }
            public void SetTile(int x, int y, int layerIndex, Tile tile)
            {
                Layers[layerIndex].Tiles[x, y] = tile;
            }
        }
        
        // 1. Add a field for tiled background toggle
        private bool showTiledBackground = true;
        
        public RoomEditor(string assetsRoot)
        {
            InitializeComponent();
            this.assetsRoot = assetsRoot;
            roomsDir = Path.Combine(assetsRoot, "Rooms");
            gameOptionsPath = Path.Combine(assetsRoot, "game_options.json");
            Directory.CreateDirectory(roomsDir);
            LoadObjectList();
            LoadRooms();
            ObjectTreeView.PreviewMouseMove += ObjectTreeView_PreviewMouseMove;
            RoomCanvas.Drop += RoomCanvas_Drop;
            this.PreviewKeyDown += RoomEditor_PreviewKeyDown;
            this.Focusable = true;
            var tileCheckbox = new CheckBox { Content = "Show Tiled Background", IsChecked = showTiledBackground, Margin = new Thickness(5) };
            tileCheckbox.Checked += (s, e) => { showTiledBackground = true; RoomCanvas.InvalidateVisual(); };
            tileCheckbox.Unchecked += (s, e) => { showTiledBackground = false; RoomCanvas.InvalidateVisual(); };
            var parentPanel = this.Content as Panel;
            if (parentPanel != null) parentPanel.Children.Insert(0, tileCheckbox);
            RoomWidthTextBox.LostFocus += RoomSize_Changed;
            RoomHeightTextBox.LostFocus += RoomSize_Changed;
            this.Loaded += (s, e) => { 
                RefreshBackgroundComboBox();
                RefreshTilesetPicker();
                if (RoomCanvas != null) Panel.SetZIndex(RoomCanvas, 0);
            };
        }

        public void LoadObjectList()
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
                        _room = room;
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
                        
                        // Initialize tile editor
                        InitTileEditor(room);
                        
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
            // Create a new empty room
            _room = new Room();
            currentRoomName = "";
            
            // Clear the canvas
            RoomCanvas.Children.Clear();
            CanvasInfoText.Visibility = Visibility.Visible;
            
            // Initialize the tile editor
            InitTileEditor(_room);
            
            // Reset UI controls
            if (BackgroundComboBox != null)
                BackgroundComboBox.SelectedItem = null;
            if (BackgroundTiledCheckBox != null)
                BackgroundTiledCheckBox.IsChecked = false;
            if (RoomWidthTextBox != null)
                RoomWidthTextBox.Text = "800";
            if (RoomHeightTextBox != null)
                RoomHeightTextBox.Text = "600";
            
            // Apply default room size
            ApplyRoomSize(800, 600);
            
            // Refresh tileset picker after room is created
            RefreshTilesetPicker();
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
        
        private void RefreshTileset_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Manual tileset refresh button clicked");
            RefreshTilesetPicker();
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

        private void RefreshTilesetPicker()
        {
            try
            {
                var tilesetsDir = Path.Combine(assetsRoot, "Sprites");
                var currentSelection = TilesetPicker?.SelectedItem as string;
                
                if (TilesetPicker == null) return;
                
                TilesetPicker.Items.Clear();
                
                if (Directory.Exists(tilesetsDir))
                {
                    foreach (var file in Directory.GetFiles(tilesetsDir))
                    {
                        var ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                        {
                            var name = Path.GetFileName(file);
                            TilesetPicker.Items.Add(name);
                        }
                    }
                }
                
                // Restore the previous selection if it still exists
                if (!string.IsNullOrEmpty(currentSelection) && TilesetPicker.Items.Contains(currentSelection))
                {
                    TilesetPicker.SelectedItem = currentSelection;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing tileset picker: {ex.Message}");
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

        private void InitTileEditor(Room room)
        {
            _room = room;
            
            // Pass the room to the canvas for tile rendering
            if (RoomCanvas != null)
            {
                RoomCanvas.Room = room;
            }
            
            RefreshTilesetPicker();
            RefreshLayerList();
            RefreshPalette();      // populate TilePalettePanel with buttons/tiles
        }

        /* ════════════════  LAYER UI ════════════════ */

        private void RefreshLayerList()
        {
            LayerListBox.ItemsSource = null;
            
            // Create a list of layer display items that include tileset info
            var layerDisplayItems = _room.Layers.Select(layer => new
            {
                Layer = layer,
                DisplayName = GetLayerDisplayName(layer)
            }).ToList();
            
            LayerListBox.ItemsSource = layerDisplayItems;
            LayerListBox.DisplayMemberPath = "DisplayName";
            
            if (_room.Layers.Count > 0)
            {
                _activeLayer = 0;
                LayerListBox.SelectedIndex = 0;
            }
        }
        
        private string GetLayerDisplayName(TileLayer layer)
        {
            var tileset = _room.GetTileset(layer.TilesetId);
            if (tileset != null)
            {
                var tilesetName = Path.GetFileNameWithoutExtension(tileset.FilePath);
                return $"{layer.Name} ({tilesetName})";
            }
            return $"{layer.Name} (No Tileset)";
        }

        private static string? AskForText(string title, string label, string defaultValue = "")
        {
            var win = new Window
            {
                Title = title,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                Owner = Application.Current.MainWindow
            };

            var txt = new TextBox { Text = defaultValue, MinWidth = 200, Margin = new Thickness(0, 4, 0, 4) };
            var ok = new Button { Content = "OK", IsDefault = true, Width = 70, Margin = new Thickness(4) };
            var cancel = new Button { Content = "Cancel", IsCancel = true, Width = 70, Margin = new Thickness(4) };

            ok.Click += (_, __) => win.DialogResult = true;

            var panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new TextBlock { Text = label });
            panel.Children.Add(txt);

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            btnRow.Children.Add(ok);
            btnRow.Children.Add(cancel);
            panel.Children.Add(btnRow);

            win.Content = panel;

            return win.ShowDialog() == true ? txt.Text.Trim() : null;
        }

        // ───── Add-layer handler ─────────────────────────────────────────────────
        private void AddLayer_Click(object sender, RoutedEventArgs e)
        {
            if (_room == null) return;

            string? name = AskForText("New Tile Layer", "Layer name:", $"Layer {_room.Layers.Count}");
            if (string.IsNullOrWhiteSpace(name)) return;

            _room.AddLayer(name);
            _activeLayer = _room.Layers.Count - 1;
            RefreshLayerList();
            RoomCanvas.InvalidateVisual();
        }
        private void LayerListBox_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            if (_isUpdatingTilesetPicker) return; // Prevent circular updates
            
            // Clear visual tiles when switching layers
            ClearVisualTiles();
            
            // Render existing tiles for the selected layer (asynchronously to prevent freezing)
            if (_activeLayer >= 0 && _room != null)
            {
                Task.Run(() => RenderExistingTiles());
            }
            
            _activeLayer = LayerListBox.SelectedIndex;
            if (_activeLayer >= 0)
            {
                var layer = _room.Layers[_activeLayer];
                var tileset = _room.GetTileset(layer.TilesetId);
                if (tileset != null)
                {
                    var tilesetName = Path.GetFileName(tileset.FilePath);
                    _isUpdatingTilesetPicker = true;
                    TilesetPicker.SelectedItem = tilesetName;
                    _isUpdatingTilesetPicker = false;
                }
                else
                {
                    // No tileset assigned to this layer yet
                    _isUpdatingTilesetPicker = true;
                    TilesetPicker.SelectedItem = null;
                    _isUpdatingTilesetPicker = false;
                }
            }
            else
            {
                // No layer selected
                _isUpdatingTilesetPicker = true;
                TilesetPicker.SelectedItem = null;
                _isUpdatingTilesetPicker = false;
            }
            RefreshPalette();                         // show tiles from that tileset
        }
        private void TilesetPicker_SelectionChanged(object? s, SelectionChangedEventArgs e)
        {
            if (_isUpdatingTilesetPicker) return;
            if (_activeLayer < 0 || TilesetPicker.SelectedItem is not string tilesetName) return;

            var tilesetPath = Path.Combine(assetsRoot, "Sprites", tilesetName);
            if (!File.Exists(tilesetPath)) return;

            var existingTileset = _room.Tilesets.FirstOrDefault(t => t.FilePath == tilesetPath);
            if (existingTileset == null)
            {
                var tilesetId = _room.Tilesets.Count > 0 ? _room.Tilesets.Max(t => t.Id) + 1 : 0;
                var newTileset = new Tileset(tilesetId, tilesetPath, 16, 16);
                _room.Tilesets.Add(newTileset);
                _room.Layers[_activeLayer].TilesetId = newTileset.Id;
            }
            else
            {
                _room.Layers[_activeLayer].TilesetId = existingTileset.Id;
            }

            _tileCache.Clear();

            _isUpdatingTilesetPicker = true;
            RefreshLayerList();
            LayerListBox.SelectedIndex = _activeLayer;
            _isUpdatingTilesetPicker = false;

            RefreshPalette();
        }

        private void RemoveLayer_Click(object sender, RoutedEventArgs e)
        {
            if (_activeLayer < 0) return;
            _room.Layers.RemoveAt(_activeLayer);
            RefreshLayerList();
            RoomCanvas.InvalidateVisual();
        }

        private void MoveLayerUp_Click(object sender, RoutedEventArgs e)
        {
            if (_activeLayer <= 0) return;
            var layer = _room.Layers[_activeLayer];
            _room.Layers.RemoveAt(_activeLayer);
            _room.Layers.Insert(--_activeLayer, layer);
            RefreshLayerList();
            LayerListBox.SelectedIndex = _activeLayer;
            RoomCanvas.InvalidateVisual();
        }

        private void MoveLayerDown_Click(object sender, RoutedEventArgs e)
        {
            if (_activeLayer < 0 || _activeLayer >= _room.Layers.Count - 1) return;
            var layer = _room.Layers[_activeLayer];
            _room.Layers.RemoveAt(_activeLayer);
            _room.Layers.Insert(++_activeLayer, layer);
            RefreshLayerList();
            LayerListBox.SelectedIndex = _activeLayer;
            RoomCanvas.InvalidateVisual();
        }

        /* ════════════════  TILE PALETTE UI ════════════════ */

        private void RefreshPalette()
        {
            TilePalettePanel.Children.Clear();
            _selectedTileButton = null;

            if (_activeLayer < 0) return;
            var layer = _room.Layers[_activeLayer];
            var tileset = _room.GetTileset(layer.TilesetId);
            if (tileset == null) return;

            // Limit the number of tiles to prevent freezing
            int maxTiles = 50; // Reasonable limit for performance
            int tileCount = 0;

            for (int ty = 0; ty < tileset.Rows && tileCount < maxTiles; ty++)
            {
                for (int tx = 0; tx < tileset.Columns && tileCount < maxTiles; tx++)
                {
                    int tileId = ty * tileset.Columns + tx;

                    try
                    {
                        // Create the actual tileset image
                        var img = new Image
                        {
                            Width = 32 * _tilePaletteZoom,
                            Height = 32 * _tilePaletteZoom,
                            Source = new CroppedBitmap(tileset.Atlas,
                                       new Int32Rect(tx * tileset.TileWidth, ty * tileset.TileHeight, 
                                                   tileset.TileWidth, tileset.TileHeight))
                        };

                        var btn = new Button
                        {
                            Tag = tileId,
                            Content = img,
                            Margin = new Thickness(1),
                            Padding = new Thickness(0),
                            Width = (34 * _tilePaletteZoom),
                            Height = (34 * _tilePaletteZoom),
                            BorderThickness = new Thickness(2),
                            BorderBrush = System.Windows.Media.Brushes.Transparent
                        };
                        btn.Click += TilePaletteButton_Click;
                        TilePalettePanel.Children.Add(btn);
                        tileCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error creating tile {tileId}: {ex.Message}");
                        // Fallback to colored rectangle if image fails
                        var rect = new System.Windows.Shapes.Rectangle
                        {
                            Fill = System.Windows.Media.Brushes.Gray,
                            Stroke = System.Windows.Media.Brushes.Black,
                            StrokeThickness = 1,
                            Width = 32 * _tilePaletteZoom,
                            Height = 32 * _tilePaletteZoom
                        };
                        
                        var fallbackBtn = new Button
                        {
                            Tag = tileId,
                            Content = rect,
                            Margin = new Thickness(1),
                            Padding = new Thickness(0),
                            Width = (34 * _tilePaletteZoom),
                            Height = (34 * _tilePaletteZoom),
                            BorderThickness = new Thickness(2),
                            BorderBrush = System.Windows.Media.Brushes.Transparent
                        };
                        fallbackBtn.Click += TilePaletteButton_Click;
                        TilePalettePanel.Children.Add(fallbackBtn);
                        tileCount++;
                    }
                }
            }
        }

        private void TilePaletteButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous selection
            if (_selectedTileButton != null)
            {
                _selectedTileButton.BorderBrush = System.Windows.Media.Brushes.Transparent;
            }
            
            // Set new selection
            _selectedTileButton = (Button)sender;
            _selectedTileButton.BorderBrush = System.Windows.Media.Brushes.Yellow;
            _selectedTileId = (int)((FrameworkElement)sender).Tag;
        }
        
        private void TilePalette_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Zoom the palette
                double zoomDelta = e.Delta > 0 ? 0.1 : -0.1;
                _tilePaletteZoom = Math.Max(0.5, Math.Min(3.0, _tilePaletteZoom + zoomDelta));
                RefreshPalette();
                e.Handled = true;
            }
            else
            {
                // Scroll the palette
                var scrollViewer = sender as ScrollViewer;
                if (scrollViewer != null)
                {
                    double scrollDelta = e.Delta > 0 ? -20 : 20;
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + scrollDelta);
                    e.Handled = true;
                }
            }
        }
        
        private void TilePaletteZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _tilePaletteZoom = Math.Min(3.0, _tilePaletteZoom + 0.2);
            RefreshPalette();
        }
        
        private void TilePaletteZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _tilePaletteZoom = Math.Max(0.5, _tilePaletteZoom - 0.2);
            RefreshPalette();
        }

        /* ════════════════  PAINTING HANDLERS ════════════════ */

        private void TilePaintCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_activeLayer < 0 || _selectedTileId < 0) return;

            _isPainting = true;
            PaintAt(e.GetPosition(TilePaintCanvas));
        }

        private void TilePaintCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPainting && e.LeftButton == MouseButtonState.Pressed)
                PaintAt(e.GetPosition(TilePaintCanvas));
        }

        private void TilePaintCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isPainting = false;
            ReleaseMouseCapture();
        }
        private void PaintAt(Point pos)
        {
            if (_room == null || _activeLayer < 0 || _selectedTileId < 0)
                return;

            int t = _room.TileSize;
            
            // Snap to grid
            int x = (int)Math.Floor(pos.X / (16 * currentZoom));
            int y = (int)Math.Floor(pos.Y / (16 * currentZoom));

            if (x < 0 || y < 0 || x >= _room.width / t || y >= _room.height / t)
                return;

            var layer = _room.Layers[_activeLayer];

            // Check if we're already painting the same tile at this position
            var existingTile = layer.Tiles[x, y];
            if (existingTile != null && existingTile.Index == _selectedTileId)
                return; // Skip if same tile already exists

            // Use default values to avoid UI element access
            int z = 0;
            bool collide = false;

            layer.Tiles[x, y] = new Tile(_selectedTileId, 0, z, collide);

            // Add visual tile directly without Dispatcher to test
            AddVisualTile(x, y, _selectedTileId);
        }
        
        private void AddVisualTile(int gridX, int gridY, int tileId)
        {
            if (_room == null || _activeLayer < 0) return;
            
            var layer = _room.Layers[_activeLayer];
            var tileset = _room.GetTileset(layer.TilesetId);
            if (tileset == null) return;
            
            // Create a unique key for this tile position
            string tileKey = $"{gridX},{gridY}";
            
            // Remove existing visual tile at this position if it exists
            if (_visualTiles.TryGetValue(tileKey, out var existingTile))
            {
                TilePaintCanvas.Children.Remove(existingTile);
                _visualTiles.Remove(tileKey);
            }
            
            try
            {
                // Get or create cached tile image
                ImageSource tileImage;
                if (!_tileCache.TryGetValue(tileId, out tileImage))
                {
                    // Calculate tile position in tileset
                    int tilesPerRow = tileset.Columns;
                    int tileX = (tileId % tilesPerRow) * tileset.TileWidth;
                    int tileY = (tileId / tilesPerRow) * tileset.TileHeight;
                    
                    // Create cropped bitmap for this tile (only once)
                    tileImage = new CroppedBitmap(tileset.Atlas, 
                        new Int32Rect(tileX, tileY, tileset.TileWidth, tileset.TileHeight));
                    
                    // Cache it for future use
                    _tileCache[tileId] = tileImage;
                }
                
                // Create image using cached source
                var img = new Image
                {
                    Source = tileImage,
                    Width = _room.TileSize * currentZoom,
                    Height = _room.TileSize * currentZoom
                };
                
                // Position the image
                Canvas.SetLeft(img, gridX * _room.TileSize * currentZoom);
                Canvas.SetTop(img, gridY * _room.TileSize * currentZoom);
                
                // Add to canvas and track it
                TilePaintCanvas.Children.Add(img);
                _visualTiles[tileKey] = img;
            }
            catch (Exception ex)
            {
                // Fallback: just add a colored rectangle if image creation fails
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Fill = System.Windows.Media.Brushes.Green,
                    Width = _room.TileSize * currentZoom,
                    Height = _room.TileSize * currentZoom
                };
                Canvas.SetLeft(rect, gridX * _room.TileSize * currentZoom);
                Canvas.SetTop(rect, gridY * _room.TileSize * currentZoom);
                TilePaintCanvas.Children.Add(rect);
                _visualTiles[tileKey] = rect;
            }
        }
        
        private void ClearVisualTiles()
        {
            TilePaintCanvas.Children.Clear();
            _visualTiles.Clear();
        }
        
        private void RenderExistingTiles()
        {

        }
    }
} 