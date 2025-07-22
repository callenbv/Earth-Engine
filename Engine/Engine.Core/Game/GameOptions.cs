using Engine.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Engine.Core.Game
{
    public class GameOptions
    {
        public string? LastScene { get; set; } = string.Empty;
        public string? Title { get; set; } = string.Empty;
        public string? RuntimePath { get; set; } = string.Empty;
        public int WindowWidth { get; set; } = 1280;
        public int WindowHeight { get; set; } = 720;

        private static GameOptions? Instance;
        public static GameOptions Main => Instance ??= new GameOptions();

        public GameOptions()
        {
            Instance = this;
        }

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
                LastScene = newOptions.LastScene;
                Title = newOptions.Title;
                RuntimePath = newOptions.RuntimePath;
                WindowWidth = newOptions.WindowWidth;
                WindowHeight = newOptions.WindowHeight;
                Console.WriteLine("[DEBUG] Loaded GameOptions:");
            }
        }
    }
}
