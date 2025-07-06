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
            
            ObjectListBox.PreviewMouseMove += ObjectListBox_PreviewMouseMove;
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

            // Populate background ComboBox with sprites after component is loaded
            this.Loaded += (s, e) => RefreshBackgroundComboBox();
        }
        
        private void LoadObjectList()
        {
            ObjectListBox.Items.Clear();
            var objectsDir = Path.Combine(assetsRoot, "Objects");
            if (Directory.Exists(objectsDir))
            {
                foreach (var file in Directory.GetFiles(objectsDir, "*.eo"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    ObjectListBox.Items.Add(new ListBoxItem { Content = name, Tag = file });
                }
            }
        }
        
        private void LoadRooms()
        {
            RoomComboBox.Items.Clear();
            
            if (Directory.Exists(roomsDir))
            {
                foreach (var file in Directory.GetFiles(roomsDir, "*.room"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    RoomComboBox.Items.Add(name);
                }
            }
            
            if (RoomComboBox.Items.Count > 0)
            {
                RoomComboBox.SelectedIndex = 0;
                currentRoomName = RoomComboBox.SelectedItem.ToString();
                LoadRoom(currentRoomName);
            }
        }
        
        public void LoadRoom(string roomName)
        {
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
                // New room - clear background settings
                if (BackgroundComboBox != null)
                    BackgroundComboBox.SelectedItem = null;
                if (BackgroundTiledCheckBox != null)
                    BackgroundTiledCheckBox.IsChecked = false;
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
        
        private void ObjectListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var listBox = sender as ListBox;
                var item = listBox?.SelectedItem as ListBoxItem;
                if (item != null)
                {
                    DragDrop.DoDragDrop(listBox, item.Tag.ToString(), DragDropEffects.Copy);
                }
            }
        }
        
        private void RoomCanvas_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var eoPath = e.Data.GetData(DataFormats.StringFormat) as string;
                if (File.Exists(eoPath) && Path.GetExtension(eoPath).ToLower() == ".eo")
                {
                    var pos = e.GetPosition(RoomCanvas);
                    AddObjectToCanvas(eoPath, pos.X, pos.Y);
                    CanvasInfoText.Visibility = Visibility.Collapsed;
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
            if (RoomComboBox.SelectedItem != null)
            {
                currentRoomName = RoomComboBox.SelectedItem.ToString();
                LoadRoom(currentRoomName);
            }
        }
        
        private void NewRoom_Click(object sender, RoutedEventArgs e)
        {
            var roomName = NewRoomTextBox.Text.Trim();
            if (string.IsNullOrEmpty(roomName))
            {
                MessageBox.Show("Please enter a room name.");
                return;
            }
            
            if (RoomComboBox.Items.Contains(roomName))
            {
                MessageBox.Show("A room with this name already exists.");
                return;
            }
            
            RoomComboBox.Items.Add(roomName);
            RoomComboBox.SelectedItem = roomName;
            currentRoomName = roomName;
            NewRoomTextBox.Text = "";
            LoadRoom(roomName);
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
                
                RoomComboBox.Items.Remove(currentRoomName);
                
                if (RoomComboBox.Items.Count > 0)
                {
                    RoomComboBox.SelectedIndex = 0;
                    currentRoomName = RoomComboBox.SelectedItem.ToString();
                    LoadRoom(currentRoomName);
                }
                else
                {
                    currentRoomName = "";
                    RoomCanvas.Children.Clear();
                    CanvasInfoText.Visibility = Visibility.Visible;
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
    }
} 