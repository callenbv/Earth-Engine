/// -----------------------------------------------------------------------------
/// <Project>      Earth Engine 
/// <File>         EditorApp.cs
/// <Author>       Callen Betts Virott 
/// <Copyright>    @2025 Callen Betts Virott. All rights reserved.
/// <Summary>                
/// -----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ImGuiNET;
using EarthEngineEditor.Windows;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using XnaColor = Microsoft.Xna.Framework.Color;
using GameRuntime;
using Engine.Core.Game;
using Editor.Windows;
using Editor.AssetManagement;
using Engine.Core;

namespace EarthEngineEditor
{
    /// <summary>
    /// EditorSelectionMode defines the selection modes available in the editor.
    /// </summary>
    public enum EditorSelectionMode
    {
        Tile,
        Object
    }

    /// <summary>
    /// EditorApp is the main class for the Earth Engine Editor application.
    /// </summary>
    public class EditorApp : Game
    {
        private GraphicsDeviceManager _graphics;
        public ImGuiRenderer? _imGuiRenderer;
        private ConsoleWindow _consoleWindow;
        public WindowManager _windowManager;
        public EditorSettings? _settings;
        private SpriteBatch spriteBatch;
        public RuntimeManager runtime;
        public EditorOverlay editorOverlay;
        public EditorWatcher fileWatcher;
        public bool gameFocused = false;
        public EditorSelectionMode selectionMode = EditorSelectionMode.Object;
        public static EditorApp Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the EditorApp class.
        /// </summary>
        public EditorApp()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Instance = this;
        }

        /// <summary>
        /// Initializes the game, loading settings and preparing the runtime environment.
        /// </summary>
        protected override void Initialize()
        {
            // Load settings
            _consoleWindow = new ConsoleWindow();
            spriteBatch = new SpriteBatch(GraphicsDevice);
            _imGuiRenderer = new ImGuiRenderer(this);
            _windowManager = new WindowManager(this,_consoleWindow);
            editorOverlay = new EditorOverlay(GraphicsDevice);

            // Enable docking
            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            runtime = new RuntimeManager(this);
            runtime.graphicsManager = _graphics;
            runtime.gameOptions = new GameOptions(); // We use default options until we load per-project
            EngineContext.Paused = true;
            EngineContext.SpriteBatch = spriteBatch;

            // Load default project for test
            _windowManager.OpenProject(_windowManager.GetLastProject());

            // Override game options
            _settings = EditorSettings.Load();

            _graphics.PreferredBackBufferWidth = _settings.WindowWidth;
            _graphics.PreferredBackBufferHeight = _settings.WindowHeight;
            _graphics.ApplyChanges();

            // Test console output
            Console.WriteLine("Earth Engine Editor initialized successfully!");
            Console.WriteLine($"Graphics Device: {GraphicsDevice.Adapter.Description}");
            Console.WriteLine($"Window Size: {_graphics.PreferredBackBufferWidth}x{_graphics.PreferredBackBufferHeight}");

            base.Initialize();
        }

        /// <summary>
        /// Updates the game state, including input handling and window management.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Update(GameTime gameTime)
        {
            bool isInputFree = !ImGui.IsAnyItemActive() &&
                !ImGui.IsAnyItemFocused() &&
                !Input.IsKeyDown(XnaKeys.LeftControl) &&
                IsActive &&
                !ImGui.GetIO().WantCaptureMouse;

            gameFocused = isInputFree;
            _windowManager?.Update(gameTime);
            runtime.Update(gameTime);
            editorOverlay.Update(gameTime);
            _windowManager?.UpdatePerformance(gameTime.ElapsedGameTime.TotalMilliseconds);

            int newWidth = GraphicsDevice.Viewport.Width;
            int newHeight = GraphicsDevice.Viewport.Height;

            if (EngineContext.InternalWidth != newWidth || EngineContext.InternalHeight != newHeight)
            {
                EngineContext.InternalWidth = newWidth;
                EngineContext.InternalHeight = newHeight;

                Camera.Main.ViewportWidth = newWidth;
                Camera.Main.ViewportHeight = newHeight;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Draws the game, rendering the ImGui interface and the runtime environment.
        /// </summary>
        /// <param name="gameTime"></param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(XnaColor.CornflowerBlue);

            _imGuiRenderer?.BeforeLayout(gameTime);

            runtime.Draw(spriteBatch);
            editorOverlay.DrawEnd(spriteBatch);
            _windowManager.tileEditor.DrawWorldPreview(spriteBatch);

            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);
            ImGui.SetNextWindowViewport(viewport.ID);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0, 0, 0, 0)); // Transparent background
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);

            ImGuiWindowFlags windowFlags =
                    ImGuiWindowFlags.NoDocking |
                    ImGuiWindowFlags.NoTitleBar |
                    ImGuiWindowFlags.NoCollapse |
                    ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoBringToFrontOnFocus |
                    ImGuiWindowFlags.NoNavFocus |
                    ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.MenuBar;

            ImGui.Begin("DockSpace", windowFlags);
            Vector2 mouse = ImGui.GetMousePos();

            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor();

            Vector2 min = viewport.Pos;
            Vector2 max = viewport.Pos + viewport.Size;

            ImGui.DockSpace(
                ImGui.GetID("DockSpace"),
                System.Numerics.Vector2.Zero,
                ImGuiDockNodeFlags.PassthruCentralNode  // ✅ Let background show through center
            );

            // Push the Roboto font if available
            if (_imGuiRenderer?.HasCustomFont == true)
            {
                ImGui.PushFont(_imGuiRenderer._robotoFont);
            }

            // Render menu bar
            _windowManager?.RenderMenuBar();

            // Render all windows
            _windowManager?.RenderAll();

            // Pop the font
            if (_imGuiRenderer?.HasCustomFont == true)
            {
                ImGui.PopFont();
            }

            ImGui.End();
            _imGuiRenderer?.AfterLayout();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Unloads the content of the game, saving settings and disposing resources.
        /// </summary>
        protected override void UnloadContent()
        {
            // Save settings before exiting
            if (_settings != null)
            {
                _settings.WindowWidth = _graphics.PreferredBackBufferWidth;
                _settings.WindowHeight = _graphics.PreferredBackBufferHeight;
                _settings.Save();
            }
            
            _consoleWindow?.Dispose();
            base.UnloadContent();
        }
    }
} 
