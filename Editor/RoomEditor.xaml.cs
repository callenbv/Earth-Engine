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
            public List<RoomObject> objects { get; set; } = new List<RoomObject>();
        }
        
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
        
        private void LoadRoom(string roomName)
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
        }
        
        public void SaveRoom()
        {
            if (string.IsNullOrEmpty(currentRoomName))
                return;
                
            var room = new Room { name = currentRoomName };
            
            foreach (UIElement element in RoomCanvas.Children)
            {
                if (element is Image img && img.Tag is string objectPath)
                {
                    room.objects.Add(new RoomObject
                    {
                        objectPath = objectPath,
                        x = Canvas.GetLeft(img),
                        y = Canvas.GetTop(img)
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
                        var bmp = new BitmapImage(new Uri(spritePath));
                        var img = new Image
                        {
                            Source = bmp,
                            Width = bmp.PixelWidth,
                            Height = bmp.PixelHeight,
                            Stretch = Stretch.None,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Tag = eoPath
                        };
                        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                        Canvas.SetLeft(img, x - bmp.PixelWidth / 2);
                        Canvas.SetTop(img, y - bmp.PixelHeight / 2);
                        RoomCanvas.Children.Add(img);
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
            if (e.Source is Image img)
            {
                isDragging = true;
                draggedElement = img;
                dragStart = e.GetPosition(RoomCanvas);
                originalPosition = new Point(Canvas.GetLeft(img), Canvas.GetTop(img));
                img.CaptureMouse();
                e.Handled = true;
            }
        }
        
        private void RoomCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && draggedElement != null)
            {
                var currentPos = e.GetPosition(RoomCanvas);
                var delta = currentPos - dragStart;
                
                Canvas.SetLeft(draggedElement, originalPosition.X + delta.X);
                Canvas.SetTop(draggedElement, originalPosition.Y + delta.Y);
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
    }
} 