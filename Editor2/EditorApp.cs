using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ImGuiNET;
using EarthEngineEditor.Windows;
using System.IO;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;
using XnaButtonState = Microsoft.Xna.Framework.Input.ButtonState;
using XnaColor = Microsoft.Xna.Framework.Color;
using Engine.Core.Graphics;
using GameRuntime;
using Engine.Core.Data;
using Engine.Core.Rooms;
using Engine.Core.Game;
using Editor.Windows;
using Editor.AssetManagement;
using Engine.Core;

namespace EarthEngineEditor
{
    public class EditorApp : Game
    {
        private GraphicsDeviceManager _graphics;
        private ImGuiRenderer? _imGuiRenderer;
        private ConsoleWindow _consoleWindow;
        public WindowManager _windowManager;
        private EditorSettings? _settings;
        private TextureLibrary? textureLibrary;
        private Room? scene;
        private SpriteBatch spriteBatch;
        public RuntimeManager runtime;
        public EditorOverlay editorOverlay;
        public EditorWatcher fileWatcher;
        public bool gameFocused = false;

        public static EditorApp Instance { get; private set; }

        public EditorApp()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Instance = this;
        }

        protected override void Initialize()
        {
            // Load settings
            spriteBatch = new SpriteBatch(GraphicsDevice);
            _imGuiRenderer = new ImGuiRenderer(this);
            _consoleWindow = new ConsoleWindow();
            _windowManager = new WindowManager(this,_consoleWindow);
            editorOverlay = new EditorOverlay(GraphicsDevice);

            // Enable docking
            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            runtime = new RuntimeManager(this);
            runtime.graphicsManager = _graphics;
            EngineContext.Paused = true;

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

        protected override void Update(GameTime gameTime)
        {
            _windowManager?.Update(gameTime);
            runtime.Update(gameTime);
            editorOverlay.Update(gameTime);
            _windowManager?.UpdatePerformance(gameTime.ElapsedGameTime.TotalMilliseconds);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(XnaColor.CornflowerBlue);

            _imGuiRenderer?.BeforeLayout(gameTime);

            runtime.Draw(spriteBatch);
            editorOverlay.DrawEnd(spriteBatch);

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
            ImGui.PopStyleVar(3);
            ImGui.PopStyleColor();

            Vector2 mouse = ImGui.GetMousePos();
            Vector2 min = viewport.Pos;
            Vector2 max = viewport.Pos + viewport.Size;

            bool isHoveringGameArea = mouse.X >= min.X && mouse.X <= max.X &&
                                      mouse.Y >= min.Y && mouse.Y <= max.Y;

            bool isInputFree = !ImGui.IsAnyItemActive() && !ImGui.IsAnyItemFocused() && !Input.IsKeyDown(XnaKeys.LeftControl);
            gameFocused = isHoveringGameArea && isInputFree;

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