/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         EditorSettings.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using System.IO;
using System.Text.Json;

namespace EarthEngineEditor
{
    /// <summary>
    /// Represents the settings for the Earth Engine Editor.
    /// </summary>
    public class EditorSettings
    {
        /// <summary>
        /// Path to the settings file.
        /// </summary>
        private static readonly string SettingsPath = Path.Combine(
        AppContext.BaseDirectory, "settings.json");

        /// <summary>
        /// Path to the runtime executable.
        /// </summary>
        private static readonly string RuntimePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "Earth-Engine", "Runtime", "bin", "Debug", "net8.0", "GameRuntime.exe");

        /// <summary>
        /// The window width of the editor.
        /// </summary>
        public int WindowWidth { get; set; } = 1280;

        /// <summary>
        /// The height of the editor window.
        /// </summary>
        public int WindowHeight { get; set; } = 720;

        /// <summary>
        /// Loads the editor settings from a JSON file.
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Saves the current editor settings to a JSON file.
        /// </summary>
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
