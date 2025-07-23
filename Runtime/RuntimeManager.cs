using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.IO;
using System.Text.Json;
using Engine.Core.Game;
using Engine.Core;
using System.Drawing;
using System.Reflection.Metadata;
using Engine.Core.Data;
using System.Threading;
using System.Diagnostics;
using Engine.Core.Graphics;
using Engine.Core.Rooms;
using System.Runtime;
using Engine.Core.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;

namespace GameRuntime
{
    public class RuntimeManager
    {
        private string assetsRoot;
        private string roomsDir;
        private string gameOptionsPath;
        public GameOptions gameOptions;
        public GraphicsDeviceManager graphicsManager;
        private Lighting2D _lighting;
        private RenderTarget2D _sceneRenderTarget;
        public ContentManager contentManager;
        private GraphicsDevice _graphicsDevice;
        private TextureLibrary textureLibrary;
        public Room? scene;
        private int _lastWidth, _lastHeight;
        private Game game;
        public static RuntimeManager Instance { get; private set; }

        private const int INTERNAL_WIDTH = 1920;
        private const int INTERNAL_HEIGHT = 1080;
    
        public RuntimeManager(Game game_)
        {
            assetsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
            roomsDir = Path.Combine(assetsRoot, "Rooms");
            gameOptionsPath = Path.Combine(assetsRoot, "game_options.json");
            Instance = this;
            game = game_;
            _graphicsDevice = game_.GraphicsDevice;
            _lighting = new Lighting2D(_graphicsDevice, INTERNAL_WIDTH, INTERNAL_HEIGHT);
            _lastWidth = INTERNAL_WIDTH;
            _lastHeight = INTERNAL_HEIGHT;
            _sceneRenderTarget = new RenderTarget2D(_graphicsDevice, INTERNAL_WIDTH, INTERNAL_HEIGHT);
        }

        /// <summary>
        /// Initialize the runtime manager
        /// </summary>
        /// <param name="device"></param>
        /// <param name="contentManager"></param>
        public void Initialize()
        {
            contentManager = new ContentManager(game.Services, EnginePaths.SHARED_CONTENT_PATH);

            Input.gameInstance = game;
            Input.graphicsManager = graphicsManager;
            FontLibrary.Main.Initialize(_graphicsDevice,contentManager);
            FontLibrary.Main.LoadFonts();
            TextureLibrary textureLibrary = new TextureLibrary();
            textureLibrary.graphicsDevice = _graphicsDevice;
            textureLibrary.LoadTextures();

            Camera.Main.SetTargetViewportSize(384, 216);  
            graphicsManager.ApplyChanges();
        }

        /// <summary>
        /// Update everything
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            Input.Update();
            Camera.Main.Update(gameTime);
            Camera.Main.SetViewportSize(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);

            Camera.Main.ViewportHeight = graphicsManager.GraphicsDevice.Viewport.Height;
            Camera.Main.ViewportWidth = graphicsManager.GraphicsDevice.Viewport.Width;

            if (scene != null)
            {
                scene.Update(gameTime);
                EngineContext.Current.Scene = scene;
            }

            // Resize lighting and scene render target if needed (only if internal resolution changes)
            if (INTERNAL_WIDTH != _lastWidth || INTERNAL_HEIGHT != _lastHeight)
            {
                _lighting.Resize(INTERNAL_WIDTH, INTERNAL_HEIGHT);
                _sceneRenderTarget?.Dispose();
                _sceneRenderTarget = new RenderTarget2D(_graphicsDevice, INTERNAL_WIDTH, INTERNAL_HEIGHT);
                _lastWidth = INTERNAL_WIDTH;
                _lastHeight = INTERNAL_HEIGHT;
            }
        }

        /// <summary>
        /// Draw everything
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            var viewport = _graphicsDevice.Viewport;

            // Draw scene to render target (high internal resolution for smooth subpixel movement)
            _graphicsDevice.SetRenderTarget(_sceneRenderTarget);
            _graphicsDevice.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);

            // Draw world with camera transform at internal resolution
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Camera.Main.GetViewMatrix(INTERNAL_WIDTH, INTERNAL_HEIGHT));
            if (scene != null)
            {
                scene.Render(spriteBatch);
            }
            spriteBatch.End();

            // Update the lightmap
            _lighting.Draw(scene,spriteBatch);

            // Draw scene to backbuffer (scale from internal resolution to window)
            _graphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp);

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
            spriteBatch.Draw(_sceneRenderTarget, new Vector2(offsetX, offsetY), null, Microsoft.Xna.Framework.Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.End();

            // Draw lighting overlay (multiply blend) with same scaling
            spriteBatch.Begin(SpriteSortMode.Immediate, Lighting2D.MultiplyBlend);
            spriteBatch.Draw(_lighting.GetLightmap(), new Vector2(offsetX, offsetY), null, Microsoft.Xna.Framework.Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.End();

            // Draw UI elements directly to screen (no separate render target for now)
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null, Camera.Main.GetUIViewMatrix(viewport.Width, viewport.Height));
            //runtimeManager.DrawUI(_spriteBatch);
            spriteBatch.End();
        }

        /// <summary>
        /// Launches an instance of the runtime, starting the game
        /// </summary>
        public void Launch()
        {
            string projectPath = ProjectSettings.ProjectDirectory;
            string runtimeExePath = ProjectSettings.RuntimePath;

            if (!File.Exists(runtimeExePath))
            {
                Console.WriteLine($"[RuntimeManager] Cannot find runtime exe at: {runtimeExePath}");
                return;
            }

            if (!Directory.Exists(projectPath))
            {
                Console.WriteLine($"[RuntimeManager] Project path does not exist: {projectPath}");
                return;
            }

            ScriptManager scriptManager;
            ScriptCompiler.CompileAndLoadScripts(projectPath, out scriptManager);

            // Start the process if successful
            var psi = new ProcessStartInfo
            {
                FileName = runtimeExePath,
                Arguments = $"--project \"{projectPath}\"",
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(runtimeExePath)
            };

            Process.Start(psi);
            Console.WriteLine($"[RuntimeManager] Launched runtime with project: {projectPath}");
        }
    }
} 