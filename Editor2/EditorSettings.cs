using System;
using System.IO;
using System.Text.Json;

namespace EarthEngineEditor
{
    public class EditorSettings
    {
        private static readonly string SettingsPath = "Settings\\settings.json";
        
        public bool ShowDemoWindow { get; set; } = false;
        public bool ShowMetricsWindow { get; set; } = false;
        public bool ShowAboutWindow { get; set; } = false;
        public bool ShowConsole { get; set; } = true;
        public int WindowWidth { get; set; } = 1280;
        public int WindowHeight { get; set; } = 720;
        public List<string> OpenPanels = new();
        public string? LastFocusedPanel = null;

        public static EditorSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<EditorSettings>(json) ?? new EditorSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load settings: {ex.Message}");
            }
            
            return new EditorSettings();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
} 