/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Runtime.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core;
using Engine.Core.Audio;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Rooms;
using Engine.Core.Scripting;
using Engine.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

namespace GameRuntime
{
    /// <summary>
    /// Main runtime class for the game engine, responsible for initializing and running the game.
    /// </summary>
    public class Runtime : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private RuntimeManager runtimeManager;
        private GameOptions gameOptions;

        /// <summary>
        /// Constructs a new Runtime instance with the specified project path.
        /// </summary>
        /// <param name="projectPath"></param>
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

        /// <summary>
        /// Initializes the game, setting up graphics, audio, and loading the default scene.
        /// </summary>
        protected override void Initialize()
        {
            // Set up our main spritebatch
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Set up the runtime manager and load the default scene
            runtimeManager = new RuntimeManager(this);
            runtimeManager.graphicsManager = _graphics;
            runtimeManager.gameOptions = gameOptions;
            gameOptions.Load("game_options.json");
            runtimeManager.Initialize();

            // Game settings based on options 
            Window.AllowUserResizing = gameOptions.CanResizeWindow;
            Window.Title = gameOptions.Title;
            IsFixedTimeStep = gameOptions.FixedTimestep;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / gameOptions.TargetFPS);
            _graphics.IsFullScreen = gameOptions.Fullscreen;
            _graphics.SynchronizeWithVerticalRetrace = gameOptions.VerticalSync;
            _graphics.PreferMultiSampling = false;
            _graphics.PreferredBackBufferWidth = gameOptions.WindowWidth;
            _graphics.PreferredBackBufferHeight = gameOptions.WindowHeight;
            _graphics.HardwareModeSwitch = false;

            // Load the compiled scripts
            ScriptCompiler.LoadScripts();

            // Load our default scene
            runtimeManager.scene = Room.Load(gameOptions.StartScene);
            runtimeManager.scene.Initialize();

            // Load static tilemaps
            TilemapManager.Load(Path.Combine(EnginePaths.ProjectBase, "Tilemaps", "tilemaps.json"));

            // Apply any graphics changes
            _graphics.ApplyChanges();

            // Engine is now running
            EngineContext.Running = true;

            base.Initialize();
        }

        /// <summary>
        /// Updates the game state, including input handling and runtime updates.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
#if DEBUG
            if (Keyboard.GetState().IsKeyDown(Keys.Escape) || Input.IsButtonDown(VirtualButton.Start))
                Exit();
#endif
            runtimeManager.Update(gameTime);
            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the game scene, rendering the current room and any overlays or UI elements.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Draw(GameTime gameTime)
        {
            runtimeManager.Draw(_spriteBatch);
            base.Draw(gameTime);
        }
    }
} 

