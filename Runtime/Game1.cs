using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Engine.Core;
using GameRuntime; // For Lighting2D

namespace GameRuntime
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private ScriptManager _scriptManager;
        private RoomManager _roomManager;
        private Lighting2D _lighting;
        private int _lastWidth, _lastHeight;
        private RenderTarget2D _sceneRenderTarget;

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

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Check for Ctrl+R to reset room
            if (Keyboard.GetState().IsKeyDown(Keys.R) && (Keyboard.GetState().IsKeyDown(Keys.LeftControl) || Keyboard.GetState().IsKeyDown(Keys.RightControl)))
            {
                ResetRoom();
            }

            // Update viewport dimensions for coordinate conversion
            _roomManager.SetViewportDimensions(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

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

            // Update lighting occluders and lights from room
            _lighting.Occluders = _roomManager.GetOccluders();
            
            // Get lights from room and add demo lights
            var roomLights = _roomManager.GetLights();
            _lighting.Lights.Clear();
            _lighting.Lights.AddRange(roomLights);

            base.Update(gameTime);
        }

        private void ResetRoom()
        {
            try
            {
                // Get the current room name
                var currentRoomName = _roomManager.GetCurrentRoomName();
                if (!string.IsNullOrEmpty(currentRoomName))
                {
                    // Reload the current room
                    _roomManager.LoadRoom(currentRoomName, GraphicsDevice);
                    Console.WriteLine($"Room '{currentRoomName}' reset!");
                }
                else
                {
                    // Load default room if no current room
                    _roomManager.LoadDefaultRoom(GraphicsDevice);
                    Console.WriteLine("Default room loaded!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to reset room: {ex.Message}");
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // Draw scene to render target
            GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
            GraphicsDevice.Clear(Color.CornflowerBlue);
            var viewport = GraphicsDevice.Viewport;
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Engine.Core.Camera.Main.GetViewMatrix(viewport.Width, viewport.Height));
            _roomManager.Draw(_spriteBatch);
            _spriteBatch.End();

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