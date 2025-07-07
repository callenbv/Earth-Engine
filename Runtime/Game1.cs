using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Engine.Core;
using GameRuntime;
using System.Runtime.Serialization;
using Engine.Core.Game;
using Engine.Core.Game.Tiles;
using Engine.Core.Graphics;

namespace GameRuntime
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private ScriptManager _scriptManager;
        private RuntimeManager _roomManager;
        private GameObjectManager objectManager;
        private Lighting2D _lighting;
        private int _lastWidth, _lastHeight;
        private RenderTarget2D _sceneRenderTarget;
        private TilemapRenderer _mapRenderer;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            // Point ContentManager to the Editor's Assets directory where sprites are located
            var assetsPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Editor", "bin", "Assets"));
            Content.RootDirectory = assetsPath;
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _scriptManager = new ScriptManager();
            _scriptManager.LoadScripts();
            objectManager = new GameObjectManager();
            _roomManager = new RuntimeManager(_scriptManager);
            Input.gameInstance = this;
            Input.graphicsManager = _graphics;
            TextureLibrary.Main.LoadTextures(_graphics.GraphicsDevice);

            _mapRenderer = new TilemapRenderer();

            // Load game options and set window properties
            var gameOptions = _roomManager.GetGameOptions();
            Window.Title = gameOptions.title;
            _graphics.PreferredBackBufferWidth = gameOptions.windowWidth;
            _graphics.PreferredBackBufferHeight = gameOptions.windowHeight;
            _graphics.ApplyChanges();
            _mapRenderer.Initialize();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            // Set GraphicsDevice and RoomManager for scripts to use
            Engine.Core.GameScript.GraphicsDevice = GraphicsDevice;
            Engine.Core.GameScript.RoomManager = _roomManager;
            
            // Load the default room with ContentManager
            _roomManager.LoadDefaultRoom(GraphicsDevice, Content);

            // Initialize lighting system
            _lighting = new Lighting2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _lastWidth = GraphicsDevice.Viewport.Width;
            _lastHeight = GraphicsDevice.Viewport.Height;

            // Create scene render target
            _sceneRenderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        }

        protected override void Update(GameTime gameTime)
        {
            Input.Update();
            Camera.Main.Update(gameTime);

            // Update viewport dimensions for coordinate conversion
            Camera.Main.SetViewportSize(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            // Check for hot reload every few frames
            if (gameTime.TotalGameTime.TotalMilliseconds % 500 < 16) // Check every ~500ms
            {
                _scriptManager.CheckForHotReload();
            }

            // Update room manager (which handles all game objects and their scripts)
            _roomManager.Update(gameTime);

            // Resize lighting and scene render target if needed
            if (GraphicsDevice.Viewport.Width != _lastWidth || GraphicsDevice.Viewport.Height != _lastHeight)
            {
                _lighting.Resize(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                _sceneRenderTarget?.Dispose();
                _sceneRenderTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                _lastWidth = GraphicsDevice.Viewport.Width;
                _lastHeight = GraphicsDevice.Viewport.Height;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Draw scene to render target
            GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);
            var viewport = GraphicsDevice.Viewport;

            _spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Engine.Core.Camera.Main.GetViewMatrixPixel(viewport.Width, viewport.Height));
            _mapRenderer.Draw(_spriteBatch);
            _spriteBatch.End();

            _spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Engine.Core.Camera.Main.GetViewMatrix(viewport.Width, viewport.Height));
            _roomManager.Draw(_spriteBatch);
            _spriteBatch.End();


            // Get lights from room and populate lighting system
            _roomManager.GetLights(_lighting);

            // Update the lightmap (draw lights to lightmap render target)
            _lighting.Draw(_spriteBatch);

            // Draw scene to backbuffer
            GraphicsDevice.SetRenderTarget(null);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            _spriteBatch.Draw(_sceneRenderTarget, Vector2.Zero, Color.White);
            _spriteBatch.End();

            // Draw lighting overlay (multiply blend) with camera transform
            _spriteBatch.Begin(SpriteSortMode.Immediate, Lighting2D.MultiplyBlend, transformMatrix: Engine.Core.Camera.Main.GetViewMatrix(viewport.Width, viewport.Height));
            _spriteBatch.Draw(_lighting.GetLightmap(), Vector2.Zero, Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
                game.Run();
        }
    }
} 