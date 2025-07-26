/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         GameOptions.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Data;
using System.Text.Json;

namespace Engine.Core.Game
{
    /// <summary>
    /// Represents the game options for the Earth Engine game engine.
    /// </summary>
    public class GameOptions
    {
        public string? StartScene { get; set; } = string.Empty;
        public string? Title { get; set; } = string.Empty;
        public int WindowWidth { get; set; } = 1280;
        public int WindowHeight { get; set; } = 720;
        public int TargetResolutionWidth { get; set; } = 1920;
        public int TargetResolutionHeight { get; set; } = 1080;
        public int TargetFPS { get; set; } = 60;
        public bool Fullscreen { get; set; } = true;
        public bool VerticalSync { get; set; } = false;
        public bool FixedTimestep { get; set; } = false;
        public bool CanResizeWindow { get; set; } = false;

        private static GameOptions? Instance;
        public static GameOptions Main => Instance ??= new GameOptions();

        /// <summary>
        /// Initializes a new instance of the <see cref="GameOptions"/> class.
        /// </summary>
        public GameOptions()
        {
            Instance = this;
        }

        /// <summary>
        /// Loads the game options from a JSON file located at the specified path.
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path)
        {
            string optionsPath = Path.Combine(EnginePaths.ProjectBase, path);

            if (!File.Exists(optionsPath))
            {
                Console.WriteLine($"[GameOptions] Cannot find options file at: {optionsPath}");
                return;
            }

            string json = File.ReadAllText(optionsPath);

            var options = new JsonSerializerOptions
            {

            };

            var newOptions = JsonSerializer.Deserialize<GameOptions>(json, options);

            if (newOptions != null)
            {
                StartScene = newOptions.StartScene;
                Title = newOptions.Title;
                WindowWidth = newOptions.WindowWidth;
                WindowHeight = newOptions.WindowHeight;
                TargetResolutionHeight = newOptions.TargetResolutionHeight;
                TargetResolutionWidth = newOptions.TargetResolutionWidth;
                TargetResolutionWidth = newOptions.TargetResolutionWidth;
                Fullscreen = newOptions.Fullscreen;
                FixedTimestep = newOptions.FixedTimestep;
                VerticalSync = newOptions.VerticalSync;
                CanResizeWindow = newOptions.CanResizeWindow;
                TargetFPS = newOptions.TargetFPS;
                EngineContext.InternalWidth = TargetResolutionWidth;
                EngineContext.InternalHeight = TargetResolutionHeight;
                Console.WriteLine("[DEBUG] Loaded GameOptions:");
            }
        }
    }
}

