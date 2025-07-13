using Engine.Core.Game;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

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
                        IconPathTextBox.Text = options.icon ?? string.Empty;
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
                    defaultRoom = StartRoomComboBox.SelectedItem?.ToString() ?? "",
                    icon = IconPathTextBox.Text
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

        private void BrowseIcon_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Icon files (*.ico)|*.ico|All files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                IconPathTextBox.Text = dialog.FileName;
            }
        }
    }
} 