/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         RuntimeManager.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.IO;
using Engine.Core.Game;
using Engine.Core;
using Engine.Core.Data;
using System.Diagnostics;
using Engine.Core.Graphics;
using Engine.Core.Rooms;
using Engine.Core.Scripting;
using Engine.Core.Audio;
using Engine.Core.Systems;
using Engine.Core.Game.Components;

namespace GameRuntime
{
    /// <summary>
    /// Manages the runtime environment for the game, including loading assets, managing scenes, and rendering.
    /// </summary>
    public class RuntimeManager
    {
        public GameOptions gameOptions;
        public GraphicsDeviceManager graphicsManager;
        private Lighting _lighting;
        private RenderTarget2D _sceneRenderTarget;
        public ContentManager contentManager;
        private GraphicsDevice _graphicsDevice;
        private TextureLibrary textureLibrary;
        public AudioManager audioManager = new AudioManager();
        public Room? scene;
        private int _lastWidth, _lastHeight;
        private Game game;
        public static RuntimeManager Instance { get; private set; }
        public int _cameraLogFrames = 0;

        /// <summary>
        /// Initialize the runtime manager
        /// </summary>
        /// <param name="game_"></param>
        public RuntimeManager(Game game_)
        {
            Instance = this;
            game = game_;
            _graphicsDevice = game_.GraphicsDevice;
            _lighting = new Lighting(_graphicsDevice, EngineContext.InternalWidth, EngineContext.InternalHeight);
            _lastWidth = EngineContext.InternalWidth;
            _lastHeight = EngineContext.InternalHeight;
            _sceneRenderTarget = new RenderTarget2D(
                _graphicsDevice,
                EngineContext.InternalWidth,
                EngineContext.InternalHeight,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24
            );
        }

        /// <summary>
        /// Initialize the runtime manager
        /// </summary>
        /// <param name="device"></param>
        /// <param name="contentManager"></param>
        public void Initialize()
        {
            InitializeSystems();
            LoadAssets();
        }

        /// <summary>
        /// Initialize all systems in the runtime manager
        /// </summary>
        public void InitializeSystems()
        {
            contentManager = new ContentManager(game.Services, EnginePaths.SHARED_CONTENT_PATH);
            Input.gameInstance = game;
            Input.graphicsManager = graphicsManager;
            Input.Initialize();
            CollisionSystem.Initialize();
            GraphicsLibrary.graphicsDevice = _graphicsDevice;
            GraphicsLibrary.Initialize();
            EngineContext.Current.GraphicsDevice = _graphicsDevice;
            Grid3D.Instance.Initialize(_graphicsDevice);
        }

        /// <summary>
        /// Load all assets required for the runtime
        /// </summary>
        public void LoadAssets()
        {
            FontLibrary.Main.Initialize(_graphicsDevice, contentManager);
            FontLibrary.Main.LoadFonts();
            TextureLibrary textureLibrary = new TextureLibrary();
            textureLibrary.graphicsDevice = _graphicsDevice;
            textureLibrary.LoadTextures();
            MeshLibrary.LoadAll();
            audioManager.Initialize();
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
            Camera.Main.graphicsDevice = _graphicsDevice;

            if (scene != null)
            {
                scene.Update(gameTime);
                EngineContext.Current.Scene = scene;
            }
            CollisionSystem.Update(gameTime);

            EngineContext.DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Resize lighting and scene render target if needed (only if internal resolution changes)
            if (EngineContext.InternalWidth != _lastWidth || EngineContext.InternalHeight != _lastHeight)
            {
                _lighting.Resize(EngineContext.InternalWidth, EngineContext.InternalHeight);
                _sceneRenderTarget?.Dispose();
                _sceneRenderTarget = new RenderTarget2D(
                    _graphicsDevice,
                    EngineContext.InternalWidth,
                    EngineContext.InternalHeight,
                    false,
                    SurfaceFormat.Color,
                    DepthFormat.Depth24
                );
                _lastWidth = EngineContext.InternalWidth;
                _lastHeight = EngineContext.InternalHeight;
                Console.WriteLine($"Resized to {_lastWidth},{_lastHeight}");
            }

        }

        /// <summary>
        /// Draw everything
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (scene == null)
                return;

            var viewport = _graphicsDevice.Viewport;

            // Draw scene to render target (high internal resolution for smooth subpixel movement)
            _graphicsDevice.SetRenderTarget(_sceneRenderTarget);
            _graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Microsoft.Xna.Framework.Color.CornflowerBlue, 1f, 0);

            Matrix view3D = GameCamera.GetViewMatrix();
            Matrix proj3D = GameCamera.GetProjectionMatrix(EngineContext.InternalWidth, EngineContext.InternalHeight);

            // Draw the sprites depth sorted
            _graphicsDevice.DepthStencilState = DepthStencilState.None;
            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Camera.Main.GetViewMatrix(EngineContext.InternalWidth, EngineContext.InternalHeight));
            TilemapManager.Render(spriteBatch);
            scene.Render(spriteBatch);
            spriteBatch.End();

            // 3D pass (render on top of 2D content in the scene target)
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;
            _graphicsDevice.BlendState = BlendState.Opaque;

            // Render 3D grid (editor only)
            if (!EngineContext.Running)
            {
                Grid3D.Instance.Draw(_graphicsDevice, view3D, proj3D);
            }
            
            scene.Render(spriteBatch);

            // Update the lightmap (independent light buffer)
            _lighting.Draw(scene,spriteBatch);

            // Draw scene to backbuffer (scale from internal resolution to window)
            _graphicsDevice.SetRenderTarget(null);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp);

            // Calculate scale to fit the internal render target in the window while maintaining aspect ratio
            float scaleX = (float)viewport.Width / EngineContext.InternalWidth;
            float scaleY = (float)viewport.Height / EngineContext.InternalHeight;
            float scale = Math.Min(scaleX, scaleY);

            // Calculate position to center the render target
            float scaledWidth = EngineContext.InternalWidth * scale;
            float scaledHeight = EngineContext.InternalHeight * scale;
            float offsetX = (viewport.Width - scaledWidth) * 0.5f;
            float offsetY = (viewport.Height - scaledHeight) * 0.5f;

            // Draw the render target with linear filtering for smooth scaling
            spriteBatch.Draw(_sceneRenderTarget, new Vector2(offsetX, offsetY), null, Microsoft.Xna.Framework.Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.End();

            // Draw lighting overlay (multiply blend) with same scaling
            spriteBatch.Begin(SpriteSortMode.Immediate, Lighting.MultiplyBlend);
            spriteBatch.Draw(_lighting.GetLightmap(), new Vector2(offsetX, offsetY), null, Microsoft.Xna.Framework.Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.End();

            // Draw UI elements directly to screen (no separate render target for now)
            spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Camera.Main.GetUIViewMatrix(EngineContext.InternalWidth, EngineContext.InternalHeight));
            scene.RenderUI(spriteBatch);
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
                Console.Error.WriteLine($"[RuntimeManager] Cannot find runtime exe at: {runtimeExePath}");
                return;
            }

            if (!Directory.Exists(projectPath))
            {
                Console.Error.WriteLine($"[RuntimeManager] Project path does not exist: {projectPath}");
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
