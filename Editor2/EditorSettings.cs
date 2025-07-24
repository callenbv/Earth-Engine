using Engine.Core;
using Engine.Core.Data;
using System;
using System.IO;
using System.Text.Json;

namespace EarthEngineEditor
{
    public class EditorSettings
    {
        private static readonly string SettingsPath = Path.Combine(
    AppContext.BaseDirectory, "settings.json");

        private static readonly string RuntimePath = "C:\\Users\\calle\\Desktop\\Earth Engine\\Runtime\\bin\\Debug\\net8.0\\GameRuntime.exe";
        public int WindowWidth { get; set; } = 1280;
        public int WindowHeight { get; set; } = 720;

        public static EditorSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    // Read the data and set editor properties (e.g,
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<EditorSettings>(json);
                    ProjectSettings.RuntimePath = RuntimePath;

                    return settings;
                }
                else
                {
                    File.Create(SettingsPath);
                    File.WriteAllText(SettingsPath, "{}");
                    return new EditorSettings();
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