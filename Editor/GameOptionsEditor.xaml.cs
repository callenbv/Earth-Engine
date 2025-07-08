using Engine.Core.Game;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Editor
{
    public partial class GameOptionsEditor : UserControl
    {
        private readonly string assetsRoot;
        private readonly string roomsDir;
        private readonly string gameOptionsPath;

        public GameOptionsEditor(string assetsRoot)
        {
            InitializeComponent();
            this.assetsRoot = assetsRoot;
            roomsDir = Path.Combine(assetsRoot, "Rooms");
            gameOptionsPath = Path.Combine(assetsRoot, "game_options.json");
            LoadRooms();
            LoadGameOptions();
        }

        public void LoadRooms()
        {
            StartRoomComboBox.Items.Clear();
            if (Directory.Exists(roomsDir))
            {
                foreach (var file in Directory.GetFiles(roomsDir, "*.room"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    StartRoomComboBox.Items.Add(name);
                }
            }
        }

        public void LoadGameOptions()
        {
            if (File.Exists(gameOptionsPath))
            {
                try
                {
                    var json = File.ReadAllText(gameOptionsPath);
                    var options = JsonSerializer.Deserialize<GameOptions>(json);
                    if (options != null)
                    {
                        GameTitleTextBox.Text = options.title;
                        WindowWidthTextBox.Text = options.windowWidth.ToString();
                        WindowHeightTextBox.Text = options.windowHeight.ToString();
                        StartRoomComboBox.SelectedItem = options.defaultRoom;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load game options: {ex.Message}");
                }
            }
        }

        private void StartRoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This will be saved when SaveGameOptions is clicked
        }

        private void SaveGameOptions_Click(object sender, RoutedEventArgs e)
        {
            SaveGameOptions();
        }

        public void SaveGameOptions()
        {
            try
            {
                var options = new GameOptions
                {
                    title = GameTitleTextBox.Text,
                    windowWidth = int.Parse(WindowWidthTextBox.Text),
                    windowHeight = int.Parse(WindowHeightTextBox.Text),
                    defaultRoom = StartRoomComboBox.SelectedItem?.ToString() ?? ""
                };
                var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(gameOptionsPath, json);
                // Silent save for Ctrl+S, no popup
                LoadRooms();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save game options: {ex.Message}");
            }
        }
    }
} 