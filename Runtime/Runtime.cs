using Engine.Core;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Game.Components;
using Engine.Core.Rooms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace GameRuntime
{
    public class Runtime : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private RuntimeManager runtimeManager;
        private GameOptions gameOptions;
        private string projectPath = string.Empty;

        public Runtime(string projectPath)
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Get the paths to the project directory and assets
            EnginePaths.ProjectBase = projectPath;
            EnginePaths.AssetsBase = Path.Combine(projectPath, "Assets");

            // Construct early systems
            gameOptions = new GameOptions();
        }

        protected override void Initialize()
        {
            // Set up the runtime manager and load the default scene
            runtimeManager = new RuntimeManager(this);
            runtimeManager.graphicsManager = _graphics;
            runtimeManager.gameOptions = gameOptions;
            runtimeManager.Initialize();

            gameOptions.Load("game_options.json");
            Window.AllowUserResizing = true;
            Window.Title = gameOptions.Title;
            _graphics.PreferredBackBufferWidth = gameOptions.WindowWidth;
            _graphics.PreferredBackBufferHeight = gameOptions.WindowHeight;

            // Set up our main spritebatch
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            string scriptDllPath = Path.Combine(EnginePaths.ProjectBase, "Build", "CompiledScripts.dll");

            // Load the compiled scripts
            if (File.Exists(scriptDllPath))
            {
                Console.WriteLine($"[Runtime] Loading CompiledScripts.dll from: {scriptDllPath}");

                byte[] bytes = File.ReadAllBytes(scriptDllPath);
                Assembly asm = Assembly.Load(bytes);

                EngineContext.Current.ScriptManager = new ScriptManager(asm);
                ComponentRegistry.RegisterAllComponents();

                Console.WriteLine("[Runtime] ScriptManager initialized and components registered.");
            }
            else
            {
                Console.WriteLine("[Runtime] CompiledScripts.dll not found!");
            }

            // Debug: Copy the engine DLL into our folder so we can rapid test engine changes
#if DEBUG
            foreach (var kvp in ComponentRegistry.Components)
                Console.WriteLine($"[Runtime] Registered: {kvp.Key}");
            string engineCorePath = Path.Combine(AppContext.BaseDirectory, "Engine.Core.dll");
            string buildPath = Path.Combine(EnginePaths.ProjectBase, "Build");
            string destPath = Path.Combine(buildPath, "Engine.Core.dll");

            if (File.Exists(engineCorePath))
            {
                Directory.CreateDirectory(buildPath); // Ensure Build exists
                File.Copy(engineCorePath, destPath, overwrite: true);
                Console.WriteLine($"[Launcher] Copied Engine.Core.dll to {destPath}");
            }
            else
            {
                Console.WriteLine("[Launcher] Engine.Core.dll not found at: " + engineCorePath);
            }
#endif
            // Load our default scene
            runtimeManager.scene = Room.Load(gameOptions.LastScene);

            // Apply any graphics changes
            _graphics.ApplyChanges();

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

            projectPath ??= AppContext.BaseDirectory;
            Console.WriteLine($"Project root set to {projectPath}");

#if DEBUG
            if (projectPath == null)
            {
                Console.WriteLine("No project specified. Drag your project folder here:");
                projectPath = Console.ReadLine()?.Trim('"');
            }
#endif
            // We defaut to our current directory, which should be the case for release builds
            if (projectPath == null)
            {
                // Get the directory of the executable
                string exeDir = AppContext.BaseDirectory;

                // Ensure this is the actual folder (remove trailing slash just in case)
                exeDir = exeDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                Console.WriteLine("Invalid or missing project path.");
                return;
            }

            using var game = new Runtime(projectPath);
            game.Run();
        }
    }
} 
