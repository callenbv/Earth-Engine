/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         Runtime.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Engine.Core.Audio;
using Engine.Core.Data;
using Engine.Core.Game;
using Engine.Core.Rooms;
using Engine.Core.Scripting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
        public AudioManager audioManager = new AudioManager();

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
            runtimeManager.Initialize();

            gameOptions.Load("game_options.json");

            // Game settings based on options 
            Window.AllowUserResizing = true;
            Window.Title = gameOptions.Title;
            _graphics.SynchronizeWithVerticalRetrace = false;
            _graphics.PreferMultiSampling = false;
            IsFixedTimeStep = false;
            _graphics.PreferredBackBufferWidth = gameOptions.WindowWidth;
            _graphics.PreferredBackBufferHeight = gameOptions.WindowHeight;

            // Load the compiled scripts
            ScriptCompiler.LoadScripts();

            // Load our default scene
            runtimeManager.scene = Room.Load(gameOptions.LastScene);
            runtimeManager.scene.Initialize();

            // Set up and load audio
            audioManager.Initialize();

            // Load static tilemaps
            TilemapManager.Load(Path.Combine(EnginePaths.ProjectBase, "Tilemaps", "tilemaps.json"));

            // Apply any graphics changes
            _graphics.ApplyChanges();

            base.Initialize();
        }

        /// <summary>
        /// Updates the game state, including input handling and runtime updates.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
#if DEBUG
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
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

