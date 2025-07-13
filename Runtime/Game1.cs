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
using Engine.Core.Systems.Graphics;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace GameRuntime
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private ScriptManager _scriptManager;
        private RuntimeManager runtimeManager;
        private GameObjectManager objectManager;
        private Lighting2D _lighting;
        private int _lastWidth, _lastHeight;
        private RenderTarget2D _sceneRenderTarget;
        private GameOptions _gameOptions;
        
        // Internal rendering resolution for smooth subpixel movement
        private const int INTERNAL_WIDTH = 1920;
        private const int INTERNAL_HEIGHT = 1080;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            // Point ContentManager to the centralized Content directory
            var earthEngineDir = FindEarthEngineDirectory();
            var contentPath = Path.Combine(earthEngineDir, "Content", "bin", "Windows");
            Content.RootDirectory = contentPath;
            IsMouseVisible = true;
        }
        
        /// <summary>
        /// Find the Earth-Engine directory by searching for the Content folder
        /// </summary>
        private string FindEarthEngineDirectory()
        {
            var current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            
            // Search up the directory tree for the Content folder
            while (current != null)
            {
                var contentDir = Path.Combine(current.FullName, "Content");
                if (Directory.Exists(contentDir))
                {
                    Console.WriteLine($"[Game1] Found Earth-Engine directory: {current.FullName}");
                    return current.FullName;
                }
                current = current.Parent;
            }
            
            // Fallback to the original path calculation
            var fallbackPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Content", "bin", "Windows"));
            Console.WriteLine($"[Game1] Using fallback path: {fallbackPath}");
            return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(fallbackPath))));
        }

        protected override void Initialize()
        {
            _scriptManager = new ScriptManager();
            _scriptManager.LoadScripts();
            objectManager = new GameObjectManager();
            runtimeManager = new RuntimeManager(_scriptManager);

            // Get the loaded game options from RuntimeManager
            _gameOptions = GameOptions.Main;
            
            Input.gameInstance = this;
            Input.graphicsManager = _graphics;
            TextureLibrary.Main.LoadTextures(_graphics.GraphicsDevice);
            
            // Initialize font system
            FontLibrary.Main.Initialize(_graphics.GraphicsDevice, Content);
            FontLibrary.Main.LoadFonts();

            // Use the loaded game options for window configuration
            Window.Title = _gameOptions.title;
            _graphics.PreferredBackBufferWidth = _gameOptions.windowWidth;
            _graphics.PreferredBackBufferHeight = _gameOptions.windowHeight;
            _graphics.ApplyChanges();

            // Set window icon if specified (SDL2)
            if (!string.IsNullOrEmpty(_gameOptions.icon) && File.Exists(_gameOptions.icon))
            {
                try
                {
                    SDL2IconHelper.SetWindowIcon(_gameOptions.icon);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set SDL2 window icon: {ex.Message}");
                }
            }

            // Set up camera target viewport sizes (this defines the "base" resolution for game logic)
            Camera.Main.SetTargetViewportSize(384, 216);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            // Set GraphicsDevice and RoomManager for scripts to use
            Engine.Core.GameScript.GraphicsDevice = GraphicsDevice;
            Engine.Core.GameScript.RoomManager = runtimeManager;
            
            // Load the default room with ContentManager
            runtimeManager.Initialize(GraphicsDevice, Content);

            // Initialize lighting system with internal resolution for smooth rendering
            _lighting = new Lighting2D(GraphicsDevice, INTERNAL_WIDTH, INTERNAL_HEIGHT);
            _lastWidth = INTERNAL_WIDTH;
            _lastHeight = INTERNAL_HEIGHT;

            // Create scene render target with internal resolution for smooth subpixel movement
            _sceneRenderTarget = new RenderTarget2D(GraphicsDevice, INTERNAL_WIDTH, INTERNAL_HEIGHT);
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
            runtimeManager.Update(gameTime);

            // Resize lighting and scene render target if needed (only if internal resolution changes)
            if (INTERNAL_WIDTH != _lastWidth || INTERNAL_HEIGHT != _lastHeight)
            {
                _lighting.Resize(INTERNAL_WIDTH, INTERNAL_HEIGHT);
                _sceneRenderTarget?.Dispose();
                _sceneRenderTarget = new RenderTarget2D(GraphicsDevice, INTERNAL_WIDTH, INTERNAL_HEIGHT);
                _lastWidth = INTERNAL_WIDTH;
                _lastHeight = INTERNAL_HEIGHT;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var viewport = GraphicsDevice.Viewport;

            // Draw scene to render target (high internal resolution for smooth subpixel movement)
            GraphicsDevice.SetRenderTarget(_sceneRenderTarget);
            GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);

            // Draw world with camera transform at internal resolution
            _spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, 
                Camera.Main.GetViewMatrix(INTERNAL_WIDTH, INTERNAL_HEIGHT));
            runtimeManager.Draw(_spriteBatch);
            _spriteBatch.End();

            // Update the lightmap (draw lights to lightmap render target)
            _lighting.Draw(_spriteBatch);

            // Draw scene to backbuffer (scale from internal resolution to window)
            GraphicsDevice.SetRenderTarget(null);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp);
            
            // Calculate scale to fit the internal render target in the window while maintaining aspect ratio
            float scaleX = (float)viewport.Width / INTERNAL_WIDTH;
            float scaleY = (float)viewport.Height / INTERNAL_HEIGHT;
            float scale = Math.Min(scaleX, scaleY);
            
            // Calculate position to center the render target
            float scaledWidth = INTERNAL_WIDTH * scale;
            float scaledHeight = INTERNAL_HEIGHT * scale;
            float offsetX = (viewport.Width - scaledWidth) * 0.5f;
            float offsetY = (viewport.Height - scaledHeight) * 0.5f;
            
            // Draw the render target with linear filtering for smooth scaling
            _spriteBatch.Draw(_sceneRenderTarget, new Vector2(offsetX, offsetY), null, Microsoft.Xna.Framework.Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            _spriteBatch.End();

            // Draw lighting overlay (multiply blend) with same scaling
            _spriteBatch.Begin(SpriteSortMode.Immediate, Lighting2D.MultiplyBlend);
            _spriteBatch.Draw(_lighting.GetLightmap(), new Vector2(offsetX, offsetY), null, Microsoft.Xna.Framework.Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            _spriteBatch.End();

            // Draw UI elements directly to screen (no separate render target for now)
            _spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Camera.Main.GetUIViewMatrix(viewport.Width, viewport.Height));
            runtimeManager.DrawUI(_spriteBatch);
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

public static class SDL2IconHelper
{
    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SDL_GL_GetCurrentWindow();

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_SetWindowIcon(IntPtr window, IntPtr surface);

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SDL_CreateRGBSurfaceFrom(
        IntPtr pixels, int width, int height, int depth, int pitch,
        uint Rmask, uint Gmask, uint Bmask, uint Amask);

    [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_FreeSurface(IntPtr surface);

    public static void SetWindowIcon(string iconPath)
    {
        if (!File.Exists(iconPath)) return;

        using (var icon = new Icon(iconPath))
        using (var bmp = icon.ToBitmap())
        {
            var data = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                IntPtr sdlWindow = SDL_GL_GetCurrentWindow();
                if (sdlWindow == IntPtr.Zero) return;

                IntPtr surface = SDL_CreateRGBSurfaceFrom(
                    data.Scan0, bmp.Width, bmp.Height, 32, data.Stride,
                    0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000);

                if (surface != IntPtr.Zero)
                {
                    SDL_SetWindowIcon(sdlWindow, surface);
                    SDL_FreeSurface(surface);
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }
    }
} 