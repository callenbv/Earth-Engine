using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Reflection;
using Engine.Core;

namespace GameRuntime
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private ScriptManager _scriptManager;
        private RoomManager _roomManager;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _scriptManager = new ScriptManager();
            _scriptManager.LoadScripts();
            
            _roomManager = new RoomManager(_scriptManager);
            
            // Load game options and set window properties
            var gameOptions = _roomManager.GetGameOptions();
            Window.Title = gameOptions.title;
            _graphics.PreferredBackBufferWidth = gameOptions.windowWidth;
            _graphics.PreferredBackBufferHeight = gameOptions.windowHeight;
            _graphics.ApplyChanges();
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            // Set GraphicsDevice and RoomManager for scripts to use
            Engine.Core.GameScript.GraphicsDevice = GraphicsDevice;
            Engine.Core.GameScript.RoomManager = _roomManager;
            
            // Load the default room
            _roomManager.LoadDefaultRoom(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            Input.Update();
            Camera.Main.Update(gameTime);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Update viewport dimensions for coordinate conversion
            _roomManager.SetViewportDimensions(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            // Update room manager (which handles all game objects and their scripts)
            _roomManager.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            var viewport = GraphicsDevice.Viewport;
            _spriteBatch.Begin(transformMatrix: Engine.Core.Camera.Main.GetViewMatrix(viewport.Width, viewport.Height), samplerState: SamplerState.PointClamp);
            
            // Draw room manager (which handles all game objects and their sprites)
            _roomManager.Draw(_spriteBatch);
            
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