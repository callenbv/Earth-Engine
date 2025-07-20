using Engine.Core;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Rooms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Xml.Linq;

namespace GameRuntime
{
    public class Runtime : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private RuntimeManager runtimeManager;
        private GameOptions gameOptions;

        public Runtime(string projectPath)
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Get the paths to the project directory and assets
            EnginePaths.ProjectBase = projectPath;
            EnginePaths.AssetsBase = Path.Combine(projectPath, "Assets");
        }

        protected override void Initialize()
        {
            // Set up the runtime manager and load the default scene
            gameOptions = new GameOptions();
            runtimeManager = new RuntimeManager(this, null);
            runtimeManager.graphicsManager = _graphics;
            runtimeManager.gameOptions = gameOptions;
            runtimeManager.Initialize();
            runtimeManager.scene = Room.Load(gameOptions.LastScene);

            // Set up our main spritebatch
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            runtimeManager.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            runtimeManager.Draw(_spriteBatch);
            base.Draw(gameTime);
        }
    }

    /// <summary>
    /// Robust program class that lets us launch runtimes with a given project path
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            string? projectPath = null;

            // 1. Check for CLI argument
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--project")
                {
                    projectPath = args[i + 1];
                    break;
                }
            }

            projectPath ??= FindProjectRoot();
            Console.WriteLine($"Project root set to {projectPath}");
#if DEBUG
            if (projectPath == null)
            {
                Console.WriteLine("No project specified. Drag your project folder here:");
                projectPath = Console.ReadLine()?.Trim('"');
            }
#endif
            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                Console.WriteLine("Invalid or missing project path.");
                return;
            }

            using var game = new Runtime(projectPath);
            game.Run();
        }

        public static string? FindProjectRoot()
        {
            string dir = Directory.GetCurrentDirectory();

            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "project.earthproj")))
                    return dir;

                dir = Directory.GetParent(dir)?.FullName;
            }

            return null;
        }

    }
} 
